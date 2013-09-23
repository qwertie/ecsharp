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

	public class MacroProcessor
	{
		IMessageSink _sink;
		public MacroProcessor(Type prelude, IMessageSink sink)
		{
			_sink = sink;
			if (prelude != null)
				AddMacros(prelude);
		}

		public bool AddMacros(Type type)
		{
			var ns = GSymbol.Get(type.Namespace);
			bool any = false;
			foreach (var pair in GetMacros(type)) {
				any = true;
				var name = pair.A;
				List<Pair<Symbol, SimpleMacro>> cases;
				if (!_macros.TryGetValue(name, out cases))
					_macros[name] = cases = new List<Pair<Symbol, SimpleMacro>>();
				cases.Add(Pair.Create(ns, pair.B));
			}
			return any;
		}

		Dictionary<Symbol, List<Pair<Symbol, SimpleMacro>>> _macros = new Dictionary<Symbol, List<Pair<Symbol, SimpleMacro>>>();

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

		public int GetApplicableMacros(ICollection<Symbol> openNamespaces, Symbol name, ICollection<SimpleMacro> found)
		{
			List<Pair<Symbol, SimpleMacro>> candidates;
			if (_macros.TryGetValue(name, out candidates)) {
				int count = 0;
				foreach (var pair in candidates) {
					if (openNamespaces.Contains(pair.A)) {
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

		/// <summary>Processes source files one at a time (may be easier for debugging).</summary>
		public Dictionary<ISourceFile, RVList<LNode>> ProcessSynchronously(IMessageSink sink, IReadOnlyList<ISourceFile> sourceFiles, Action<ISourceFile, RVList<LNode>> onProcessed = null)
		{
			var results = new Dictionary<ISourceFile, RVList<LNode>>();
			for (int i = 0; i < sourceFiles.Count; i++) {
				var file = sourceFiles[i];
				results[file] = ProcessFile(file, sink, onProcessed);
			}
			return results;
		}
		
		/// <summary>Processes source files in parallel. All files are fully 
		/// processed before the method returns.</summary>
		public Dictionary<ISourceFile, RVList<LNode>> ProcessParallel(IMessageSink sink, IReadOnlyList<ISourceFile> sourceFiles, Action<ISourceFile, RVList<LNode>> onProcessed)
		{
			Task<RVList<LNode>>[] tasks = ProcessAsync(sink, sourceFiles, onProcessed);
			var results = new Dictionary<ISourceFile, RVList<LNode>>();
			for (int i = 0; i < tasks.Length; i++)
				results[sourceFiles[i]] = tasks[i].Result;
			return results;
		}

		/// <summary>Processes source files asynchronously. The method returns immediately.</summary>
		public Task<RVList<LNode>>[] ProcessAsync(IMessageSink sink, IReadOnlyList<ISourceFile> sourceFiles, Action<ISourceFile, RVList<LNode>> onProcessed = null)
		{
			int parentThreadId = Thread.CurrentThread.ManagedThreadId;
			Task<RVList<LNode>>[] tasks = new Task<RVList<LNode>>[sourceFiles.Count];
			for (int i = 0; i < tasks.Length; i++)
			{
				var file = sourceFiles[i];
				tasks[i] = Task.Factory.StartNew<RVList<LNode>>(() => {
					using (ThreadEx.PropagateVariables(parentThreadId))
						return ProcessFile((ISourceFile)file, sink, onProcessed);
				});
			}
			return tasks;
		}

		public RVList<LNode> ProcessFile(ISourceFile file, IMessageSink sink, Action<ISourceFile, RVList<LNode>> onProcessed)
		{
			var lang = ParsingService.Current;
			var input = lang.Parse(file, sink);
			var results = ApplyMacros(input);
			if (onProcessed != null)
				onProcessed(file, results);
			return results;
		}
		public RVList<LNode> ApplyMacros(IListSource<LNode> input)
		{
			var results = new RVList<LNode>();
			foreach (var node in input) {
				var result = ApplyMacros(node);
				if (result.Calls(S.Splice))
					results.AddRange(result.Args);
				else
					results.Add(result);
			}
			return results;
		}
		public LNode ApplyMacros(LNode input)
		{
			// TODO
			return input;
		}
	}
}
