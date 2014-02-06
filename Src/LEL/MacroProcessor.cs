using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Loyc.Utilities;
using LeMP.Prelude;
using Loyc.Collections;
using Loyc;
using Loyc.Collections.Impl;
using Loyc.Syntax;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Threading;
using Loyc.Threading;
using System.Threading.Tasks;

namespace LeMP
{
	using S = CodeSymbols;
	using System.Diagnostics;

	/// <summary>
	/// For LeMP: an input file plus per-file options (input and output language) and output code.
	/// </summary>
	public class InputOutput
	{
		public InputOutput(ICharSource text, string fileName, IParsingService input = null, LNodePrinter outPrinter = null, string outFileName = null)
		{
			Text = text; FileName = fileName ?? ""; InputLang = input; OutPrinter = outPrinter; OutFileName = outFileName;
		}
		public readonly ICharSource Text;
		public readonly string FileName;
		public IParsingService InputLang;
		public LNodePrinter OutPrinter;
		public string OutFileName;
		public RVList<LNode> Output;
		public override string ToString()
		{
			return FileName;
		}
	}

	/// <summary>
	/// Encapsulates the LeMP engine, a simple LISP-style macro processor, 
	/// suitable for running LLLPG and other lexical macros.
	/// </summary>
	/// <remarks>
	/// MacroProcessor itself only cares about to #import/#importMacros/#unimportMacros 
	/// statements, and { braces } (for scoping the #import statements). It is 
	/// recommended to add the <see cref="Prelude"/> class when processing LES 
	/// code, as well as any other types that might contain macros you want to use.
	/// MacroProcessor is not aware of any distinction between "statements"
	/// and "expressions"; it will run macros no matter where they are located,
	/// whether as standalone statements, attributes, or arguments to functions.
	/// <para/>
	/// MacroProcessor's main responsibilities are to keep track of a table of 
	/// registered macros (call <see cref="AddMacros"/> to register more), to
	/// keep track of which namespaces are open (namespaces can be imported by
	/// <c>#import</c>, or by <c>import</c> which is defined in the prelude; see
	/// also <see cref="PreOpenedNamespaces"/>); to scan the input for macros to
	/// call; and to control the printout of messages.
	/// <para/>
	/// This class processes a batch of files at once. Call either
	/// <see cref="ProcessSynchronously"/> or <see cref="ProcessParallel"/>.
	/// Parallelizing on a file-by-file basis is easy; each source file is completely 
	/// independent, since no semantic analysis is being done. 
	/// <para/>
	/// TODO: add method for processing an LNode instead of a list of source files.
	/// </remarks>
	public class MacroProcessor
	{
		IMessageSink _sink;
		public IMessageSink Sink { get { return _sink; } set { _sink = value; } }
		public int MaxExpansions = 0xFFFF;
	
		public MacroProcessor(Type prelude, IMessageSink sink)
		{
			_sink = sink;
			if (prelude != null)
				AddMacros(prelude);
			AbortTimeout = TimeSpan.FromSeconds(30);
		}

		public class MacroInfo
		{
			public MacroInfo(Symbol @namespace, Symbol name, SimpleMacro macro, MacroMode mode)
				{ Namespace = @namespace; Name = name; Macro = macro; Mode = mode; }
			public Symbol Namespace;
			public Symbol Name;
			public SimpleMacro Macro;
			public MacroMode Mode;
		}

		MMap<Symbol, List<MacroInfo>> _macros = new MMap<Symbol, List<MacroInfo>>();

		public MSet<Symbol> PreOpenedNamespaces = new MSet<Symbol>();

		#region Adding macros from types

		public bool AddMacros(Type type)
		{
			var ns = GSymbol.Get(type.Namespace);
			bool any = false;
			foreach (var info in GetMacros(type, ns)) {
				any = true;
				AddMacro(_macros, info);
			}
			return any;
		}
		static void AddMacro(MMap<Symbol, List<MacroInfo>> macros, MacroInfo info)
		{
			List<MacroInfo> cases;
			if (!macros.TryGetValue(info.Name, out cases))
				macros[info.Name] = cases = new List<MacroInfo>();
			cases.Add(info);
		}

		private IEnumerable<MacroInfo> GetMacros(Type type, Symbol @namespace)
		{
			foreach(var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)) {
				foreach (SimpleMacroAttribute attr in method.GetCustomAttributes(typeof(SimpleMacroAttribute), false)) {
					var @delegate = AsDelegate(method);
					if (@delegate != null) {
						if (attr.Names == null || attr.Names.Length == 0)
							yield return new MacroInfo(@namespace, GSymbol.Get(method.Name), @delegate, attr.Mode);
						else
							foreach (string name in attr.Names)
								yield return new MacroInfo(@namespace, GSymbol.Get(name), @delegate, attr.Mode);
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

		#region Batch processing: ProcessSynchronously, ProcessParallel, ProcessAsync

		// TimeSpan.Zero or TimeSpan.MaxValue mean 'infinite' and prevent spawning a new thread
		public TimeSpan AbortTimeout { get; set; }

		/// <summary>Processes source files one at a time (may be easier for debugging).</summary>
		public void ProcessSynchronously(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			foreach (var io in sourceFiles)
				new Task(this).ProcessFileWithThreadAbort(io, onProcessed, AbortTimeout);
		}
		
		/// <summary>Processes source files in parallel. All files are fully 
		/// processed before the method returns.</summary>
		public void ProcessParallel(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			Task<RVList<LNode>>[] tasks = ProcessAsync(sourceFiles, onProcessed);
			for (int i = 0; i < tasks.Length; i++)
				tasks[i].Wait();
		}

		/// <summary>Processes source files in parallel using .NET Tasks. The method returns immediately.</summary>
		public Task<RVList<LNode>>[] ProcessAsync(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			int parentThreadId = Thread.CurrentThread.ManagedThreadId;
			Task<RVList<LNode>>[] tasks = new Task<RVList<LNode>>[sourceFiles.Count];
			for (int i = 0; i < tasks.Length; i++)
			{
				var io = sourceFiles[i];
				tasks[i] = System.Threading.Tasks.Task.Factory.StartNew<RVList<LNode>>(() => {
					using (ThreadEx.PropagateVariables(parentThreadId))
						return new Task(this).ProcessFileWithThreadAbort(io, onProcessed, AbortTimeout);
				});
			}
			return tasks;
		}

		#endregion

		class Task
		{
			static readonly Symbol _importMacros = GSymbol.Get("#importMacros");
			static readonly Symbol _unimportMacros = GSymbol.Get("#unimportMacros");
			static readonly Symbol _noLexicalMacros = GSymbol.Get("#noLexicalMacros");
			
			public Task(MacroProcessor parent)
			{
				_macros = parent._macros.Clone();
				// Braces must be handled specially by ApplyMacros itself, but we need 
				// a macro method in order to get the special treatment, because as an 
				// optimization, we ignore all symbols that are not in the macro table.
				MacroProcessor.AddMacro(_macros, new MacroInfo(null, S.Braces,         OnBraces, MacroMode.Normal | MacroMode.Passive));
				MacroProcessor.AddMacro(_macros, new MacroInfo(null, S.Import,         OnImport, MacroMode.Normal | MacroMode.Passive));
				MacroProcessor.AddMacro(_macros, new MacroInfo(null, _importMacros,    OnImportMacros, MacroMode.Normal));
				MacroProcessor.AddMacro(_macros, new MacroInfo(null, _unimportMacros,  OnUnimportMacros, MacroMode.Normal | MacroMode.Passive));
				MacroProcessor.AddMacro(_macros, new MacroInfo(null, _noLexicalMacros, NoLexicalMacros, MacroMode.NoReprocessing));
				_parent = parent;
			}

			MacroProcessor _parent;
			IMessageSink _sink { get { return _parent._sink; } }
			int MaxExpansions { get { return _parent.MaxExpansions; } }
			MMap<Symbol, List<MacroInfo>> _macros;

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
			
			public RVList<LNode> ProcessFileWithThreadAbort(InputOutput io, Action<InputOutput> onProcessed, TimeSpan timeout)
			{
				if (timeout == TimeSpan.Zero || timeout == TimeSpan.MaxValue)
					return ProcessFile(io, onProcessed);
				else {
					Exception ex = null;
					var thread = new ThreadEx(() =>
					{
						try { ProcessFile(io, null); }
						catch (Exception e) { ex = e; }
					});
					thread.Start();
					if (thread.Join(timeout)) {
						onProcessed(io);
					} else {
						io.Output = new RVList<LNode>(F.Id("processing_thread_timed_out"));
						thread.Abort();
						thread.Join(timeout);
					}
					if (ex != null)
						throw ex;
					return io.Output;
				}
			}

			public RVList<LNode> ProcessFile(InputOutput io, Action<InputOutput> onProcessed)
			{
				using (ParsingService.PushCurrent(io.InputLang ?? ParsingService.Current)) {
					var input = ParsingService.Current.Parse(io.Text, io.FileName, _sink);
					var inputRV = new RVList<LNode>(input);

					Debug.Assert(_scopes.Count == 0);
					_curScope = new Scope { OpenNamespaces = _parent.PreOpenedNamespaces.Clone() };
					_scopes.Add(_curScope);

					io.Output = ApplyMacrosToList(inputRV, MaxExpansions);
					if (onProcessed != null)
						onProcessed(io);
					return io.Output;
				}
			}

			#region Find macros by name: GetApplicableMacros

			public int GetApplicableMacros(ICollection<Symbol> openNamespaces, Symbol name, ICollection<MacroInfo> found)
			{
				List<MacroInfo> candidates;
				if (_macros.TryGetValue(name, out candidates)) {
					int count = 0;
					foreach (var info in candidates) {
						if (openNamespaces.Contains(info.Namespace) || info.Namespace == null) {
							count++;
							found.Add(info);
						}
					}
					return count;
				} else
					return 0;
			}
			public int GetApplicableMacros(Symbol @namespace, Symbol name, ICollection<MacroInfo> found)
			{
				List<MacroInfo> candidates;
				if (_macros.TryGetValue(name, out candidates)) {
					int count = 0;
					foreach (var info in candidates) {
						if (info.Namespace == @namespace) {
							count++;
							found.Add(info);
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
			public LNode OnUnimportMacros(LNode node, IMessageSink sink)
			{
				AutoInitScope();
				foreach (var arg in node.Args) {
					var sym = NamespaceToSymbol(arg);
					if (!_curScope.OpenNamespaces.Remove(sym))
						sink.Write(MessageSink.Debug, arg, "Namespace not found to remove: {0}", sym);
				}
				return null;
			}
			public static LNode NoLexicalMacros(LNode node, IMessageSink sink)
			{
				if (!node.IsCall)
					return null;
				return node.WithTarget(S.Splice);
			}

			public LNode OnBraces(LNode node, IMessageSink sink)
			{
				_scopes.Add(null);
				return null; // ApplyMacros will take care of popping the scope
			}
			private void PopScope()
			{
				_scopes.Pop();
				for (int i = _scopes.Count - 1; (_curScope = _scopes[i]) == null; i--) { }
			}

			#endregion
		
			#region Lower-level processing: ApplyMacros, PrintMessages, etc.

			Symbol NamespaceToSymbol(LNode node)
			{
				return GSymbol.Get(node.Print(NodeStyle.Expression)); // quick & dirty
			}

			struct Result
			{
				public Result(MacroInfo macro, LNode node, ListSlice<MessageHolder.Message> msgs)
				{
					Macro = macro; Node = node; Msgs = msgs;
				}
				public MacroInfo Macro; 
				public LNode Node;
				public ListSlice<MessageHolder.Message> Msgs;
			}

			// Optimization: these lists are re-used on each call to ApplyMacros.
			MessageHolder _messageHolder = new MessageHolder();
			List<MacroInfo> _foundMacros = new List<MacroInfo>();
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

				if (_foundMacros.Count != 0)
					return ApplyMacrosFound(input, maxExpansions);
				else
					return ApplyMacrosToChildren(input, maxExpansions);
			}
			private LNode ApplyMacrosFound(LNode input, int maxExpansions)
			{
				List<MacroInfo> foundMacros = _foundMacros;
				List<Result> results = _results;
				MessageHolder messageHolder = _messageHolder;

				LNode preprocessed = null;
				results.Clear();
				int accepted = 0, acceptedIndex = -1;
				for (int i = 0; i < foundMacros.Count; i++)
				{
					var macro = foundMacros[i];
					var macroInput = input;
					if ((macro.Mode & MacroMode.ProcessChildrenBefore) != 0) {
						if (maxExpansions == 1)
							continue; // avoid expanding both this macro and its children
						if (preprocessed == null) {
							// _foundMacros, _results, and _messageHolder are re-used 
							// by callee, so we must make copies
							foundMacros = new List<MacroInfo>(foundMacros);
							results = new List<Result>(results);
							messageHolder = messageHolder.Clone();
							
							preprocessed = ApplyMacrosToChildren(input, maxExpansions) ?? input;
						}
						macroInput = preprocessed;
					}

					LNode output = null;
					int mhi = messageHolder.List.Count;
					try {
						output = macro.Macro(macroInput, messageHolder);
						if (output != null) { accepted++; acceptedIndex = i; }
					} catch (ThreadAbortException e) {
						_sink.Write(MessageSink.Error, input, "Macro-processing thread aborted in {0}", QualifiedName(macro.Macro.Method));
						_sink.Write(MessageSink.Detail, input, e.StackTrace);
						results.Add(new Result(macro, output, messageHolder.List.Slice(mhi, messageHolder.List.Count - mhi)));
						PrintMessages(results, input, accepted, MessageSink.Error);
						throw;
					} catch (Exception e) {
						messageHolder.Write(MessageSink.Error, input, "{0}: {1}", e.GetType().Name, e.Message);
						messageHolder.Write(MessageSink.Detail, input, e.StackTrace);
					}
					results.Add(new Result(macro, output, messageHolder.List.Slice(mhi, messageHolder.List.Count - mhi)));
				}

				PrintMessages(results, input, accepted,
					messageHolder.List.MaxOrDefault(msg => MessageSink.GetSeverity(msg.Type)).Type ?? MessageSink.Verbose);

				if (accepted >= 1) {
					var result = results[acceptedIndex];
					
					Debug.Assert(result.Node != null);
					if ((result.Macro.Mode & MacroMode.ProcessChildrenBefore) != 0)
						maxExpansions--;
					
					if ((result.Macro.Mode & MacroMode.Normal) != 0) {
						if (result.Node == input)
							return ApplyMacrosToChildren(result.Node, maxExpansions - 1) ?? result.Node;
						else
							return ApplyMacros(result.Node, maxExpansions - 1) ?? result.Node;
					} else if ((result.Macro.Mode & MacroMode.ProcessChildrenAfter) != 0) {
						return ApplyMacrosToChildren(result.Node, maxExpansions - 1) ?? result.Node;
					} else
						return result.Node;
				} else {
					// #{} needs special treatment
					if (input.Calls(S.Braces)) {
						try {
							return preprocessed ?? ApplyMacrosToChildren(input, maxExpansions);
						} finally {
							PopScope();
						}
					}
					return preprocessed ?? ApplyMacrosToChildren(input, maxExpansions);
				}
			}

			RVList<LNode> ApplyMacrosToList(RVList<LNode> list, int maxExpansions)
			{
				RVList<LNode> results = list;
				LNode result = null;
				int i, c;
				// Share as much of the original RVList as is left unchanged
				for (i = 0, c = list.Count; i < c; i++) {
					if ((result = ApplyMacros(list[i], maxExpansions)) != null || (result = list[i]).Calls(S.Splice)) {
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
						if (newTarget.Calls(S.Splice, 1))
							newTarget = newTarget.Args[0];
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
						results.Where(r => r.Node != null).Select(r => QualifiedName(r.Macro.Macro.Method)).Join(", "));

				bool macroStyleCall = input.BaseStyle == NodeStyle.Special;

				if (accepted > 0 || macroStyleCall || MessageSink.GetSeverity(maxSeverity) >= MessageSink.GetSeverity(MessageSink.Warning))
				{
					if (macroStyleCall && MessageSink.GetSeverity(maxSeverity) < MessageSink.GetSeverity(MessageSink.Warning))
						maxSeverity = MessageSink.Warning;
					var rejected = results.Where(r => r.Node == null && (r.Macro.Mode & MacroMode.Passive) == 0);
					if (accepted == 0 && macroStyleCall && _sink.IsEnabled(maxSeverity) && rejected.Any())
					{
						_sink.Write(maxSeverity, input, "{0} macro(s) saw the input and declined to process it: {1}", 
							results.Count, rejected.Select(r => QualifiedName(r.Macro.Macro.Method)).Join(", "));
					}
			
					foreach (var result in results)
					{
						bool printedLast = true;
						foreach(var msg in result.Msgs) {
							// Print all messages from macros that accepted the input. 
							// For rejecting macros, print warning/error messages, and 
							// other messages when macroStyleCall.
							if (_sink.IsEnabled(msg.Type) && (result.Node != null 
								|| (msg.Type == MessageSink.Detail && printedLast)
								|| MessageSink.GetSeverity(msg.Type) >= MessageSink.GetSeverity(MessageSink.Warning)
								|| macroStyleCall))
							{
								var msg2 = new MessageHolder.Message(msg.Type, msg.Context,
									QualifiedName(result.Macro.Macro.Method) + ": " + msg.Format, msg.Args);
								msg2.WriteTo(_sink);
								printedLast = true;
							} else
								printedLast = false;
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
