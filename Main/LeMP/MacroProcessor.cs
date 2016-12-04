using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Threading;
using LeMP.Prelude;

/// <summary>The lexical macro processor. Main classes: <see cref="LeMP.Compiler"/> and <see cref="LeMP.MacroProcessor"/>.</summary>
namespace LeMP
{
	/// <summary>
	/// For LeMP: an input file plus per-file options (input and output language) and output code.
	/// </summary>
	public class InputOutput
	{
		public InputOutput(ICharSource text, string fileName, IParsingService input = null, ILNodePrinter outPrinter = null, string outFileName = null)
		{
			Text = text; FileName = fileName ?? ""; InputLang = input; OutPrinter = outPrinter; OutFileName = outFileName;
		}
		public readonly ICharSource Text;
		public readonly string FileName;
		public IParsingService InputLang;
		public bool? PreserveComments; // null means unassigned (to use the Compiler default)
		public ParsingMode ParsingMode; // inputType argument when parsing with IParsingService.Parse
		public ILNodePrinter OutPrinter;
		public ILNodePrinterOptions OutOptions;
		public string OutFileName;
		public VList<LNode> Output;
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
	/// MacroProcessor itself only cares about a few nodes including 
	/// #importMacros and #unimportMacros, and { braces } (for scoping the 
	/// #import statements). The macro processor should be configured with any 
	/// needed macros like this:
	/// <code>
	///   var prelude = typeof(LeMP.Prelude.BuiltinMacros); // the default prelude
	///   var MP = new MacroProcessor(prelude, sink);
	///   MP.AddMacros(typeof(LeMP.StandardMacros).Assembly);
	///   MP.PreOpenedNamespaces.Add((Symbol) "LeMP.Prelude"); // already done for you
	///   MP.PreOpenedNamespaces.Add((Symbol) "LeMP");
	/// </code>
	/// In order for the input code to have access to macros, two steps are 
	/// necessary: you have to add the macro classes with <see cref="AddMacros"/>
	/// and then you have to import the namespace that contains the class(es).
	/// Higher-level code (e.g. <see cref="Compiler"/>) can define "always-open"
	/// namespaces by adding entries to PreOpenedNamespaces, and the code being 
	/// processed can open additional namespaces with a #importMacros(Namespace) 
	/// statement (in LES, "import_macros Namespace" can be used as a synonym if 
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

		/// <summary>Initializes MacroProcessor with default prelude.</summary>
		public MacroProcessor(IMessageSink sink) : this(sink, typeof(BuiltinMacros)) { }

		/// <summary>Initializes MacroProcessor.</summary>
		/// <param name="sink">The destination for warning and error messages. NOTE: 
		/// this class can process files in parallel. Consider using a thread-safe
		/// implementation of <see cref="IMessageSink"/>.</param>
		/// <param name="prelude">An initial type from which to add macros.
		/// Omit this parameter to use typeof(LeMP.Prelude.BuiltinMacros).</param>
		public MacroProcessor(IMessageSink sink, Type prelude)
		{
			_sink = sink;
			if (prelude != null)
				AddMacros(prelude);
			AbortTimeout = TimeSpan.FromSeconds(30);
			PreOpenedNamespaces.Add((Symbol)"LeMP.Prelude");
		}

		MMap<Symbol, VList<MacroInfo>> _macros = new MMap<Symbol, VList<MacroInfo>>();
		internal MMap<Symbol, VList<MacroInfo>> Macros { get { return _macros; } }

		/// <summary>Macros in these namespaces will be available without an explicit 
		/// import command (#importMacros). By default this list has one item: 
		/// @@LeMP.Prelude (i.e. (Symbol)"LeMP.Prelude")</summary>
		public ICollection<Symbol> PreOpenedNamespaces { get { return _preOpenedNamespaces; } }
		internal MSet<Symbol> _preOpenedNamespaces = new MSet<Symbol>();

		/// <summary>Default values of scoped properties.</summary>
		/// <remarks>Code being processed can look up a scoped property named "N" with 
		/// <c>#getScopedProperty("N")</c> in LESv2 or EC#. This map is empty by default.
		/// Scoped properties are "scoped" in the sense that setting a property with 
		/// <c>#setScopedProperty(keyLiteral, valueLiteral)</c> takes effect only until 
		/// the end of the braced block in which it appears.
		/// <para/>
		/// The @@#inputFolder and @@#inputFileName properties (note: @@ is EC# 
		/// syntax for <see cref="Symbol"/>) are not normally stored in this collection; 
		/// when you use <see cref="ProcessSynchronously"/> or <see cref="ProcessParallel"/>, 
		/// @@#inputFolder and @@#inputFileName are set according to the folder and 
		/// filename in <see cref="InputOutput.FileName"/>. However, @@#inputFolder 
		/// is not set if the filename has no folder component, so this collection 
		/// could be used to provide a value of @@#inputFolder in that case.
		/// </remarks>
		public MMap<object, object> DefaultScopedProperties = new MMap<object, object>();

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

		internal static void AddMacro(MMap<Symbol, VList<MacroInfo>> macros, MacroInfo info)
		{
			foreach (string name in info.Names) {
				var nameS = (Symbol)name;
				var cases = macros[nameS, VList<MacroInfo>.Empty];
				if (!cases.Any(existing => existing.Macro == info.Macro))
					macros[nameS] = cases.Add(info);
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
					if (@delegate != null)
						yield return new MacroInfo(@namespace, attr, @delegate);
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

		/// <summary>Processes a list of nodes directly on the current thread.</summary>
		/// <remarks>Note: <c>AbortTimeout</c> doesn't work when using this overload.</remarks>
		public VList<LNode> ProcessSynchronously(VList<LNode> stmts)
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
			Task<VList<LNode>>[] tasks = ProcessAsync(sourceFiles, onProcessed);
			for (int i = 0; i < tasks.Length; i++)
				tasks[i].Wait();
		}

		/// <summary>Processes source files in parallel using .NET Tasks. The method returns immediately.</summary>
		public Task<VList<LNode>>[] ProcessAsync(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			int parentThreadId = Thread.CurrentThread.ManagedThreadId;
			Task<VList<LNode>>[] tasks = new Task<VList<LNode>>[sourceFiles.Count];
			for (int i = 0; i < tasks.Length; i++)
			{
				var io = sourceFiles[i];
				tasks[i] = System.Threading.Tasks.Task.Factory.StartNew<VList<LNode>>(() => {
					using (ThreadEx.PropagateVariables(parentThreadId))
						return new MacroProcessorTask(this).ProcessFileWithThreadAbort(io, onProcessed, AbortTimeout);
				});
			}
			return tasks;
		}

		#endif

		#endregion

		[ThreadStatic]
		internal static int _nextTempCounter;
		
		/// <summary>Gets the next number to use as a suffix for temporary variables (without incrementing).</summary>
		public static int NextTempCounter { get { return Math.Max(10, _nextTempCounter); } }
		/// <summary>Gets the next number to use as a suffix for temporary variables, then increments it.</summary>
		/// <remarks>MacroProcessor currently starts this counter at 10 to avoid 
		/// collisions with names like tmp_2 and tmp_3 that might be names chosen 
		/// by a developer; tmp_10 is much less likely to collide.</remarks>
		public static int IncrementTempCounter() { _nextTempCounter = NextTempCounter; return _nextTempCounter++; }
	}
}
