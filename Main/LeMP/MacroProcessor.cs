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

/// <summary>The lexical macro processor. Main classes: <see cref="LeMP.Compiler"/> and <see cref="LeMP.MacroProcessor"/>.</summary>
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
	/// statements, and { braces } (for scoping the #import statements). The
	/// macro processor should be configured with any needed macros like this:
	/// <code>
	///   var MP = new MacroProcessor(prelude, sink);
	///   MP.AddMacros(typeof(LeMP.Prelude.Macros).Assembly);
	///   MP.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
	/// </code>
	/// In order for the input code to have access to macros, two steps are 
	/// necessary: you have to add the macro classes with <see cref="AddMacros"/>
	/// and then you have to import the namespace that contains the class(es).
	/// Higher-level code (e.g. <see cref="Compiler"/>) can define "always-open"
	/// namespaces by adding entries to PreOpenedNamespaces, and the code being 
	/// processed can open additional namespaces with a #importMacros(Namespace) 
	/// statement (in LES, "import macros Namespace" can be used as a synonym if 
	/// PreOpenedNamespaces contains LeMP.Prelude).
	/// <para/>
	/// MacroProcessor is not aware of any distinction between "statements"
	/// and "expressions"; it will run macros no matter where they are located,
	/// whether as standalone statements, attributes, or arguments to functions.
	 /// <para/>
	/// MacroProcessor's main responsibilities are to keep track of a table of 
	/// registered macros (call <see cref="AddMacros"/> to register more), to
	/// keep track of which namespaces are open (namespaces can be imported by
	/// <c>#import</c>, or by <c>import</c> which is defined in the LES prelude);
	/// to scan the input for macros to call; and to control the printout of 
	/// messages.
	/// <para/>
	/// This class processes a batch of files at once. Call either
	/// <see cref="ProcessSynchronously"/> or <see cref="ProcessParallel"/>.
	/// Parallelizing on a file-by-file basis is easy; each source file is completely 
	/// independent, since no semantic analysis is being done. 
	/// <para/>
	/// TODO: add method for processing an LNode instead of a list of source files.
	/// </remarks>
	public partial class MacroProcessor
	{
		IMessageSink _sink;
		public IMessageSink Sink { get { return _sink; } set { _sink = value; } }
		public int MaxExpansions = 255;

		[ThreadStatic]
		internal static MacroProcessor _current;
		/// <summary>Returns the <c>MacroProcessor</c> running on the current thread, or null if none.</summary>
		public static MacroProcessor Current { get { return _current; } }

		public MacroProcessor(Type prelude, IMessageSink sink)
		{
			_sink = sink;
			if (prelude != null)
				AddMacros(prelude);
			AbortTimeout = TimeSpan.FromSeconds(30);
		}

		MMap<Symbol, List<MacroInfo>> _macros = new MMap<Symbol, List<MacroInfo>>();
		internal MMap<Symbol, List<MacroInfo>> Macros { get { return _macros; } }

		public MSet<Symbol> PreOpenedNamespaces = new MSet<Symbol>();

		#region Adding macros from types (AddMacros())

		public bool AddMacros(Type type)
		{
			var ns = GSymbol.Get(type.Namespace);
			bool any = false;
			foreach (var info in GetMacros(type, ns, _sink)) {
				any = true;
				AddMacro(_macros, info);
			}
			return any;
		}

		public bool AddMacros(Assembly assembly, bool writeToSink = true)
		{
			bool any = false;
			foreach (Type type in assembly.GetExportedTypes()) {
				if (!type.IsGenericTypeDefinition &&
					type.GetCustomAttributes(typeof(ContainsMacrosAttribute), true).Any())
				{
					if (writeToSink && Sink.IsEnabled(Severity.Verbose))
						Sink.Write(Severity.Verbose, assembly.GetName().Name, "Adding macros in type '{0}'", type);
					any = AddMacros(type) || any;
				}
			}
			if (!any && writeToSink)
				Sink.Write(Severity.Warning, assembly, "No macros found");
			return any;
		}

		internal static void AddMacro(MMap<Symbol, List<MacroInfo>> macros, MacroInfo info)
		{
			List<MacroInfo> cases;
			if (!macros.TryGetValue(info.Name, out cases)) {
				macros[info.Name] = cases = new List<MacroInfo>();
				cases.Add(info);
			} else {
				if (!cases.Any(existing => existing.Macro == info.Macro))
					cases.Add(info);
			}
		}

		internal static IEnumerable<MacroInfo> GetMacros(Type type, Symbol @namespace, IMessageSink sink, object instance = null)
		{
			var flags = BindingFlags.Public | BindingFlags.Static;
			if (instance != null)
				flags |= BindingFlags.Instance;
			foreach(var method in type.GetMethods(flags)) {
				foreach (LexicalMacroAttribute attr in method.GetCustomAttributes(typeof(LexicalMacroAttribute), false)) {
					var @delegate = AsDelegate(method, sink, instance);
					if (@delegate != null) {
						if (attr.Names == null || attr.Names.Length == 0)
							yield return new MacroInfo(@namespace, GSymbol.Get(method.Name), @delegate, attr);
						else
							foreach (string name in attr.Names)
								yield return new MacroInfo(@namespace, GSymbol.Get(name), @delegate, attr);
					}
				}
			}
		}
		static LexicalMacro AsDelegate(MethodInfo method, IMessageSink sink, object instance)
		{
			try {
				return (LexicalMacro)Delegate.CreateDelegate(typeof(LexicalMacro), method.IsStatic ? null : instance, method);
			} catch (Exception e) {
				sink.Write(Severity.Note, method.DeclaringType, "Macro '{0}' is uncallable: {1}", method.Name, e.Message);
				return null;
			}
		}

		#endregion

		public RVList<LNode> ProcessSynchronously(LNode stmt)
		{
			return ProcessSynchronously(new RVList<LNode>(stmt));
		}
		public RVList<LNode> ProcessSynchronously(RVList<LNode> stmts)
		{
			return new MacroProcessorTask(this).ProcessRoot(stmts);
		}

		#region Batch processing: ProcessSynchronously, ProcessParallel, ProcessAsync

		// TimeSpan.Zero or TimeSpan.MaxValue mean 'infinite' and prevent spawning a new thread
		public TimeSpan AbortTimeout { get; set; }

		/// <summary>Processes source files one at a time (may be easier for debugging).</summary>
		public void ProcessSynchronously(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			foreach (var io in sourceFiles)
				new MacroProcessorTask(this).ProcessFileWithThreadAbort(io, onProcessed, AbortTimeout);
		}
		
		#if DotNet3 || DotNet2 // Parallel mode requires .NET 4 Tasks
		public void ProcessParallel(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			ProcessSynchronously(sourceFiles, onProcessed);
		}
		#else

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
						return new MacroProcessorTask(this).ProcessFileWithThreadAbort(io, onProcessed, AbortTimeout);
				});
			}
			return tasks;
		}

		#endif

		#endregion
	}

	public class MacroInfo : IComparable<MacroInfo>
	{
		public MacroInfo(Symbol @namespace, Symbol name, LexicalMacro macro, LexicalMacroAttribute info)
		{
			NamespaceSym = @namespace; Name = name; Macro = macro; Info = info;
			Mode = info.Mode;
			if ((Mode & MacroMode.PriorityMask) == 0)
				Mode |= MacroMode.NormalPriority;
		}
		public Symbol NamespaceSym { get; private set; }
		public Symbol Name         { get; private set; }
		public LexicalMacro Macro  { get; private set; }
		public LexicalMacroAttribute Info { get; private set; }
		public MacroMode Mode      { get; private set; }

		/// <summary>Compare priorities of two macros.</summary>
		public int CompareTo(MacroInfo other)
		{
			return (Mode & MacroMode.PriorityMask).CompareTo(other.Mode & MacroMode.PriorityMask);
		}
	}

}
