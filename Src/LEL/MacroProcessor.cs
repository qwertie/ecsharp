using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Loyc.Utilities;
using LEL.Prelude;
using Loyc.Collections;
using Loyc;
using Loyc.Collections.Impl;
using Loyc.Syntax;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Threading;
using Loyc.Threading;
using System.Threading.Tasks;

namespace LEL
{
	using S = CodeSymbols;
	using System.Diagnostics;

	public class MacroProcessor
	{
		IMessageSink _sink;
		public IMessageSink Sink { get { return _sink; } set { _sink = value; } }
		public int MaxExpansions = int.MaxValue;
		/// <summary>When a macro rejects an input, its messages will not be 
		/// printed unless the severity of the Type of the message is equal to this 
		/// threshold or above it. For example, with the default threshold of Warning,
		/// Warning and Error messages are printed but Debug messages are not.</summary>
		public Symbol MinSeverityOfRejectionMessages = MessageSink.Warning;
	
		public MacroProcessor(Type prelude, IMessageSink sink)
		{
			_sink = sink;
			if (prelude != null)
				AddMacros(prelude);
		}

		MMap<Symbol, List<Pair<Symbol, SimpleMacro>>> _macros = new MMap<Symbol, List<Pair<Symbol, SimpleMacro>>>();

		public MSet<Symbol> PreOpenedNamespaces = new MSet<Symbol>();

		#region Adding macros from types

		public bool AddMacros(Type type)
		{
			var ns = GSymbol.Get(type.Namespace);
			bool any = false;
			foreach (var pair in GetMacros(type)) {
				any = true;
				AddMacro(_macros, ns, pair.A, pair.B);
			}
			return any;
		}
		internal static void AddMacro(MMap<Symbol, List<Pair<Symbol, SimpleMacro>>> macros, Symbol @namespace, Symbol name, SimpleMacro macro)
		{
			List<Pair<Symbol, SimpleMacro>> cases;
			if (!macros.TryGetValue(name, out cases))
				macros[name] = cases = new List<Pair<Symbol, SimpleMacro>>();
			cases.Add(Pair.Create(@namespace, macro));
		}

		private IEnumerable<Pair<Symbol, SimpleMacro>> GetMacros(Type type)
		{
			foreach(var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
				foreach (SimpleMacroAttribute attr in method.GetCustomAttributes(typeof(SimpleMacroAttribute), false)) {
					var @delegate = AsDelegate(method);
					if (@delegate != null) {
						if (attr.Names == null || attr.Names.Length == 0)
							yield return new Pair<Symbol, SimpleMacro>(GSymbol.Get(method.Name), @delegate);
						else
							foreach (string name in attr.Names)
								yield return new Pair<Symbol, SimpleMacro>(GSymbol.Get(name), @delegate);
					}
				}
			}
		}
		SimpleMacro AsDelegate(MethodInfo method)
		{
			try {
				return (SimpleMacro)Delegate.CreateDelegate(typeof(SimpleMacro), method);
			} catch (Exception e) {
				_sink.Write(MessageSink.Note, method.DeclaringType, "Macro '{0}' is uncallable: {1}", method.Name, e.Message);
				return null;
			}
		}

		#endregion

		#region Batch processing: ProcessSynchronously, ProcessParallel, ProcessFile

		/// <summary>Processes source files one at a time (may be easier for debugging).</summary>
		public Dictionary<ISourceFile, RVList<LNode>> ProcessSynchronously(IReadOnlyList<ISourceFile> sourceFiles, Action<ISourceFile, RVList<LNode>> onProcessed = null)
		{
			var results = new Dictionary<ISourceFile, RVList<LNode>>();
			for (int i = 0; i < sourceFiles.Count; i++) {
				var file = sourceFiles[i];
				results[file] = new Task(this).ProcessFile(file, onProcessed);
			}
			return results;
		}
		
		/// <summary>Processes source files in parallel. All files are fully 
		/// processed before the method returns.</summary>
		public Dictionary<ISourceFile, RVList<LNode>> ProcessParallel(IReadOnlyList<ISourceFile> sourceFiles, Action<ISourceFile, RVList<LNode>> onProcessed)
		{
			Task<RVList<LNode>>[] tasks = ProcessAsync(sourceFiles, onProcessed);
			var results = new Dictionary<ISourceFile, RVList<LNode>>();
			for (int i = 0; i < tasks.Length; i++)
				results[sourceFiles[i]] = tasks[i].Result;
			return results;
		}

		/// <summary>Processes source files asynchronously. The method returns immediately.</summary>
		public Task<RVList<LNode>>[] ProcessAsync(IReadOnlyList<ISourceFile> sourceFiles, Action<ISourceFile, RVList<LNode>> onProcessed = null)
		{
			int parentThreadId = Thread.CurrentThread.ManagedThreadId;
			Task<RVList<LNode>>[] tasks = new Task<RVList<LNode>>[sourceFiles.Count];
			for (int i = 0; i < tasks.Length; i++)
			{
				var file = sourceFiles[i];
				tasks[i] = System.Threading.Tasks.Task.Factory.StartNew<RVList<LNode>>(() => {
					using (ThreadEx.PropagateVariables(parentThreadId))
						return new Task(this).ProcessFile(file, onProcessed);
				});
			}
			return tasks;
		}

		#endregion

		class Task
		{
			static readonly Symbol _importMacros = GSymbol.Get("#importMacros");

			public Task(MacroProcessor parent)
			{
				_macros = parent._macros.Clone();
				MacroProcessor.AddMacro(_macros, null, S.Import, OnImport);
				MacroProcessor.AddMacro(_macros, null, _importMacros, OnImportMacros);
				MacroProcessor.AddMacro(_macros, null, S.Braces, OnBraces);
				_parent = parent;
			}

			MacroProcessor _parent;
			IMessageSink _sink { get { return _parent._sink; } }
			int MaxExpansions { get { return _parent.MaxExpansions; } }
			MMap<Symbol, List<Pair<Symbol, SimpleMacro>>> _macros;

			class Scope : ICloneable<Scope>
			{
				public MSet<Symbol> OpenNamespaces;
				public Scope Clone()
				{
					return new Scope { OpenNamespaces = OpenNamespaces.Clone() };
				}
			}
			// null entries inherit parent scope (null means "no new stuff in this scope")
			InternalList<Scope> _scopes = InternalList<Scope>.Empty; 
			Scope _curScope; // current scope, or parent scope if inherited
			void AutoInitScope()
			{
				if (_scopes.Last == null)
					_curScope = _scopes.Last = _curScope.Clone();
			}
			
			public RVList<LNode> ProcessFile(ISourceFile file, Action<ISourceFile, RVList<LNode>> onProcessed)
			{
				var lang = ParsingService.Current;
				var input = lang.Parse(file, _sink);
				var inputRV = new RVList<LNode>(input);

				Debug.Assert(_scopes.Count == 0);
				_curScope = new Scope { OpenNamespaces = _parent.PreOpenedNamespaces.Clone() };
				_scopes.Add(_curScope);

				var results = ApplyMacrosToList(inputRV, MaxExpansions);
				if (onProcessed != null)
					onProcessed(file, results);
				return results;
			}

			#region Find macros by name: GetApplicableMacros

			public int GetApplicableMacros(ICollection<Symbol> openNamespaces, Symbol name, ICollection<SimpleMacro> found)
			{
				List<Pair<Symbol, SimpleMacro>> candidates;
				if (_macros.TryGetValue(name, out candidates)) {
					int count = 0;
					foreach (var pair in candidates) {
						if (openNamespaces.Contains(pair.A) || pair.A == null) {
							count++;
							found.Add(pair.B);
						}
					}
					return count;
				} else
					return 0;
			}
			public int GetApplicableMacros(Symbol @namespace, Symbol name, ICollection<SimpleMacro> found)
			{
				List<Pair<Symbol, SimpleMacro>> candidates;
				if (_macros.TryGetValue(name, out candidates)) {
					int count = 0;
					foreach (var pair in candidates) {
						if (pair.A == @namespace) {
							count++;
							found.Add(pair.B);
						}
					}
					return count;
				} else
					return 0;
			}

			#endregion

			#region Built-in commands
			// These aren't really macros, but they are installed like macros so that
			// no extra overhead is required to detect them.

			static readonly LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

			public LNode OnImportMacros(LNode node, IMessageSink sink)
			{
				OnImport(node, sink);
				return F.Call(S.Splice);
			}
			public LNode OnImport(LNode node, IMessageSink sink)
			{
				AutoInitScope();
				foreach (var arg in node.Args)
					_curScope.OpenNamespaces.Add(NamespaceToSymbol(arg));
				return null;
			}
			public LNode OnBraces(LNode node, IMessageSink sink)
			{
				_scopes.Add(null);
				return null;
			}
			private void PopScope()
			{
				_scopes.Pop();
				for (int i = _scopes.Count - 1; (_curScope = _scopes[i]) == null; i--) { }
			}

			#endregion
		
			#region Lower-level processing: ApplyMacros, etc.

			Symbol NamespaceToSymbol(LNode node)
			{
				return GSymbol.Get(node.ToString()); // quick & dirty
			}

			struct Result {
				public SimpleMacro Macro; 
				public LNode Node;
				public ListSlice<MessageHolder.Message> Msgs;
			}
			MessageHolder _messageHolder = new MessageHolder();

			// Optimization: these lists are re-used on each call to ApplyMacros.
			List<SimpleMacro> _foundMacros = new List<SimpleMacro>();
			List<Result> _results = new List<Result>();

			public LNode ApplyMacros(LNode input, int maxExpansions)
			{
				if (maxExpansions <= 0)
					return null;
				// Find macros...
				_foundMacros.Clear();
				LNode target;
				if (input.HasSimpleHead()) {
					GetApplicableMacros(_curScope.OpenNamespaces, input.Name, _foundMacros);
				} else if ((target = input.Target).Calls(S.Dot, 2) && target.Args[1].IsId) {
					Symbol name = target.Args[1].Name, @namespace = NamespaceToSymbol(target.Args[0]);
					GetApplicableMacros(@namespace, name, _foundMacros);
				}

				if (_foundMacros.Count != 0) {
					_results.Clear();
					int accepted = 0, acceptedIndex = -1;
					for (int i = 0; i < _foundMacros.Count; i++) {
						var macro = _foundMacros[i];
						LNode result = null;
						int mhi = _messageHolder.List.Count;
						try {
							result = macro(input, _messageHolder);
							if (result != null) { accepted++; acceptedIndex = i; }
						} catch(Exception e) {
							_messageHolder.Write(MessageSink.Error, input, "Exception in {0}. {1}: {2}", 
								QualifiedName(macro.Method), e.GetType().Name, e.Message);
						}
						_results.Add(new Result { 
							Macro = macro, Node = result, 
							Msgs = _messageHolder.List.Slice(mhi, _messageHolder.List.Count - mhi)
						});
					}

					PrintMessages(_results, input, accepted,
						_messageHolder.List.MaxOrDefault(msg => MessageSink.GetSeverity(msg.Type)).Type ?? MessageSink.Verbose);

					if (accepted >= 1) {
						var result = _results[acceptedIndex];
						for (int i = 0; i < result.Msgs.Count; i++)
							result.Msgs[i].WriteTo(_sink);

						if (result.Node == input)
							return ApplyMacrosToChildren(result.Node, maxExpansions - 1) ?? result.Node;
						else
							return ApplyMacros(result.Node, maxExpansions - 1) ?? result.Node;
					} else if (input.Calls(S.Braces)) {
						try {
							return ApplyMacrosToChildren(input, maxExpansions);
						} finally {
							PopScope();
						}
					}
				}

				return ApplyMacrosToChildren(input, maxExpansions);
			}

			RVList<LNode> ApplyMacrosToList(RVList<LNode> list, int maxExpansions)
			{
				RVList<LNode> results = list;
				LNode result = null;
				int i, c;
				// Share as much of the original RVList as is left unchanged
				for (i = 0, c = list.Count; i < c; i++) {
					if ((result = ApplyMacros(list[i], maxExpansions)) != null) {
						results = list.WithoutLast(c - i);
						Add(ref results, result);
						break;
					}
				}
				// Prepare a modified list from now on
				for (i++; i < c; i++) {
					LNode input = list[i];
					if ((result = ApplyMacros(input, maxExpansions)) != null)
						Add(ref results, result);
					else
						results.Add(input);
				}
				return results;
			}
			private void Add(ref RVList<LNode> results, LNode result)
			{
				if (result.Calls(S.Splice))
					results.AddRange(result.Args);
				else
					results.Add(result);
			}
 
			LNode ApplyMacrosToChildren(LNode node, int maxExpansions)
			{
				if (maxExpansions <= 0)
					return null;

				bool changed = false;
				RVList<LNode> old;
				var newAttrs = ApplyMacrosToList(old = node.Attrs, maxExpansions);
				if (newAttrs != old) {
					node = node.WithAttrs(newAttrs);
					changed = true;
				}
				if (!node.HasSimpleHead()) {
					LNode target = node.Target, newTarget = ApplyMacros(target, maxExpansions);
					if (newTarget != null) {
						node = node.WithTarget(newTarget);
						changed = true;
					}
				}
				var newArgs = ApplyMacrosToList(old = node.Args, maxExpansions);
				if (newArgs != old) {
					node = node.WithArgs(newArgs);
					changed = true;
				}
				return changed ? node : null;
			}

			void PrintMessages(List<Result> results, LNode input, int accepted, Symbol maxSeverity)
			{
				if (accepted > 1)
					_sink.Write(MessageSink.Error, input, "Ambiguous macro call. {0} macros accepted the input: {1}", accepted,
						_results.Where(r => r.Node != null).Select(r => QualifiedName(r.Macro.Method)).Join(", "));

				if (accepted > 0 || MessageSink.GetSeverity(maxSeverity) >= MessageSink.GetSeverity(_parent.MinSeverityOfRejectionMessages))
				{
					if (accepted == 0 && results.Count > 1)
						_sink.Write(maxSeverity, input, "{0} macros saw the input and declined to process it.", results.Count);
			
					foreach (var result in results)
					{
						bool printedLast = true;
						foreach(var msg in result.Msgs) {
							// Print all messages from macros that accepted the input. 
							// For rejecting macros, only print messages above a threshold.
							if (result.Node != null || (msg.Type == MessageSink.Detail && printedLast)
								|| MessageSink.GetSeverity(msg.Type) >= MessageSink.GetSeverity(_parent.MinSeverityOfRejectionMessages))
							{
								if (_sink.IsEnabled(msg.Type))
								{
									var msg2 = new MessageHolder.Message(msg.Type, msg.Context, 
										QualifiedName(result.Macro.Method) + ": " + msg.Format, msg.Args);
									msg2.WriteTo(_sink);
									printedLast = true;
								}
							}
						}
					}
				}
			}

			private string QualifiedName(MethodInfo method)
			{
				return string.Format("{0}.{1}.{2}", method.DeclaringType.Namespace, method.DeclaringType.Name, method.Name);
			}

			#endregion
		}
	}

}
