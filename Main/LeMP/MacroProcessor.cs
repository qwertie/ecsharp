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
using System.Diagnostics;
using Loyc.Collections.Impl;

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
		public readonly string FileName; // Should include the full path
		public IParsingService InputLang;
		public bool? PreserveComments; // null means unassigned (to use the Compiler default)
		public ParsingMode ParsingMode; // inputType argument when parsing with IParsingService.Parse
		public ILNodePrinter OutPrinter;
		public ILNodePrinterOptions OutOptions;
		public string OutFileName;
		public LNodeList Output;
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
	/// statement.
	/// <para/>
	/// MacroProcessor is not aware of any distinction between "statements"
	/// and "expressions"; it will run macros no matter where they are located,
	/// whether as standalone statements, attributes, or arguments to functions.
	/// <para/>
	/// MacroProcessor's main responsibilities are to keep track of a table of 
	/// registered macros (call <see cref="AddMacros"/> to register more), to
	/// keep track of which namespaces are open, to scan the input for macros to 
	/// call; and to control the printout of messages.
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
			PreOpenedNamespaces.Add(GSymbol.Empty);
			PreOpenedNamespaces.Add((Symbol)"LeMP.Prelude");
		}

		MMap<Symbol, InternalList<InternalMacroInfo>> _macros = new MMap<Symbol, InternalList<InternalMacroInfo>>();
		internal MMap<Symbol, InternalList<InternalMacroInfo>> Macros { get { return _macros; } }

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

		public int AddMacros(Type type)
		{
			int found = 0;
			foreach (var info in LeMP.MacroInfo.GetMacros(type, _sink)) {
				found++;
				AddMacro(_macros, info);
			}
			return found;
		}

		public int AddMacros(Assembly assembly, bool writeToSink = true)
		{
			int found = 0;
			foreach (Type type in assembly.GetExportedTypes()) {
				if (!type.IsGenericTypeDefinition &&
					type.GetCustomAttributes(typeof(ContainsMacrosAttribute), true).Any())
				{
					if (writeToSink && Sink.IsEnabled(Severity.Verbose))
						Sink.Write(Severity.Verbose, assembly.GetName().Name, "Adding macros in type '{0}'", type);
					found += AddMacros(type);
				}
			}
			if (found == 0 && writeToSink)
				Sink.Warning(assembly, "No macros found");
			return found;
		}

		static SymbolPool Wildcards = new SymbolPool();
		internal static Symbol MatchEveryIdentifier = Wildcards.Get(nameof(MatchEveryIdentifier));
		internal static Symbol MatchEveryCall = Wildcards.Get(nameof(MatchEveryCall));
		internal static Symbol MatchEveryLiteral = Wildcards.Get(nameof(MatchEveryLiteral));

		internal static void AddMacro(MMap<Symbol, InternalList<InternalMacroInfo>> macros, LeMP.MacroInfo newMacro)
		{
			if (newMacro?.Macro == null)
				return; // TODO: should we throw? log an error?
			
			foreach (string name_ in newMacro.Names)
				if (name_ != null)
					AddMacroByName(macros, (Symbol)name_, newMacro);

			if ((newMacro.Mode & MacroMode.MatchEveryCall) != 0)
				AddMacroByName(macros, MatchEveryCall, newMacro);
			if ((newMacro.Mode & MacroMode.MatchEveryIdentifier) != 0)
				AddMacroByName(macros, MatchEveryIdentifier, newMacro);
			if ((newMacro.Mode & MacroMode.MatchEveryLiteral) != 0)
				AddMacroByName(macros, MatchEveryLiteral, newMacro);
		}
		private static void AddMacroByName(MMap<Symbol, InternalList<InternalMacroInfo>> macros, Symbol name, MacroInfo newMacro)
		{
			var macrosWithThisName = macros[name, InternalList<InternalMacroInfo>.Empty];

			// Check if the same macro was added earlier, possibly in a different namespace
			foreach (var macro in macrosWithThisName)
			{
				if (macro.Macro.Equals(newMacro.Macro))
				{
					if (!macro.Namespaces.Contains(newMacro.Namespace))
						macro.Namespaces.Add(newMacro.Namespace);
					return;
				}
			}

			// It's a new macro, add it
			macrosWithThisName.Add(new InternalMacroInfo
			{
				Name = name,
				Attr = newMacro,
				Macro = newMacro.Macro,
				Namespaces = new InternalList<Symbol>(new Symbol[] { newMacro.Namespace })
			});
			macros[name] = macrosWithThisName;
		}

		#endregion

		/// <summary>Processes a list of nodes directly on the current thread.</summary>
		/// <remarks>Note: <c>AbortTimeout</c> doesn't work when using this overload.</remarks>
		public LNodeList ProcessSynchronously(LNodeList stmts)
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
		
		/// <summary>Processes source files in parallel. All files are fully 
		/// processed before the method returns.</summary>
		public void ProcessParallel(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			Task<LNodeList>[] tasks = ProcessAsync(sourceFiles, onProcessed);
			for (int i = 0; i < tasks.Length; i++)
				tasks[i].Wait();
		}

		/// <summary>Processes source files in parallel using .NET Tasks. The method returns immediately.</summary>
		public Task<LNodeList>[] ProcessAsync(IReadOnlyList<InputOutput> sourceFiles, Action<InputOutput> onProcessed = null)
		{
			int parentThreadId = Thread.CurrentThread.ManagedThreadId;
			Task<LNodeList>[] tasks = new Task<LNodeList>[sourceFiles.Count];
			for (int i = 0; i < tasks.Length; i++)
			{
				var io = sourceFiles[i];
				tasks[i] = System.Threading.Tasks.Task.Factory.StartNew<LNodeList>(() => {
					using (ThreadEx.PropagateVariables(parentThreadId))
						return new MacroProcessorTask(this).ProcessFileWithThreadAbort(io, onProcessed, AbortTimeout);
				});
			}
			return tasks;
		}

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

	/// <summary>A macro delegate with a list of namespaces and <see cref="LexicalMacroAttribute"/>.
	/// The difference between this and the public class <see cref="LeMP.MacroInfo"/>
	/// is that here the goal is to associate a list of namespaces with a a single 
	/// macro name (in case the macro is re-used in other namespaces) and the other 
	/// MacroInfo associates a single namespace with macro attributes that 
	/// potentially list multiple names.</summary>
	internal class InternalMacroInfo
	{
		public Symbol Name;
		public LexicalMacro Macro;
		public LexicalMacroAttribute Attr;
		public InternalList<Symbol> Namespaces = InternalList<Symbol>.Empty;
		public MacroMode Mode { get { return Attr.Mode; } }

		public static Comparison<InternalMacroInfo> CompareDescendingByPriority =
			(a, b) => b.Attr.Priority.CompareTo(a.Attr.Priority);
	}
}
