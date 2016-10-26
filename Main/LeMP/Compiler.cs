using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Loyc.MiniTest;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Threading;
using Loyc.Ecs;
using LeMP.Prelude;
using System.Threading;

namespace LeMP
{
	/// <summary>A class that helps you invoke <see cref="MacroProcessor"/> on
	/// on a set of source files, given a set of command-line options.</summary>
	/// <remarks>
	/// This class helps you process command-line options (see <see cref="ProcessArguments(IList{string}, bool, bool, IList{string})"/>), 
	/// complete <see cref="InputOutput"/> objects based on those options (see 
	/// <see cref="CompleteInputOutputOptions"/>), and add macros from Assemblies 
	/// (<see cref="AddMacros"/> and <see cref="AddStdMacros"/>). 
	/// </remarks>
	public class Compiler
	{
		public static InvertibleSet<string> TwoArgOptions = new InvertibleSet<string>(new[] { "macros" });
		public static Dictionary<char, string> ShortOptions = new Dictionary<char,string>()
			{ {'o',"out"}, {'m',"macros"}, {'p',"preserve-comments"}, {'e',"editor"} };

		public static MMap<string, Pair<string, string>> KnownOptions = new MMap<string, Pair<string, string>>()
		{
 			{ "help",      Pair.Create("", "show this screen") },
 			{ "macros",    Pair.Create("filename.dll", "load macros from given assembly") },
 			{ "max-expand",Pair.Create("N", "stop expanding macros after N nested or iterated expansions.") },
 			{ "verbose",   Pair.Create("", "Print extra status messages (e.g. discovered Types, list output files).") },
 			{ "parallel",  Pair.Create("", "Process all files in parallel (this is the default)") },
			{ "noparallel",Pair.Create("", "Process all files in sequence") },
			{ "inlang",    Pair.Create("name", "Set input language: --inlang=ecs for Enhanced C#, --inlang=les for LES") },
			{ "outext",    Pair.Create("name", "Set output extension and optional suffix:\n  .ecs (Enhanced C#), .cs (C#), .les (LES)\n"+
			               "This can include a suffix before the extension, e.g. --outext=.output.cs\n"+
			               "If --outlang is not used, output language is chosen by file extension.") },
			{ "outlang",   Pair.Create("name", "Set output language independently of file extension") },
			{ "forcelang", Pair.Create("", "Specifies that --inlang overrides the input file extension.\n"+
			               "Without this option, known file extensions override --inlang.") },
 			{ "timeout",   Pair.Create("N", "Aborts the processing thread(s) after this many seconds (0=never)") },
			{ "nostdmacros", Pair.Create("", "Don't scan LeMP.StdMacros.dll or pre-import LeMP and LeMP.Prelude") },
			{ "set",       Pair.Create("key=literal", "Associate a value with a key (use #get(key) to read it back)") },
			{ "snippet",   Pair.Create("key=code", "Associate code with a key (use #get(key) to read it back)") },
			{ "preserve-comments", Pair.Create("bool", "Preserve comments and newlines (where supported)\n  Default value: true") },
		};

		#region Main()

		[STAThread] // Required by ICSharpCode.TextEditor
		public static void Main(string[] args)
		{
			if (!args.Contains("--nologo"))
				Console.WriteLine("LeMP macro compiler ({0})", typeof(Compiler).Assembly.GetName().Version.ToString());
			if (args.Contains("--editor")) {
				Console.WriteLine("Starting editor...");
				System.Windows.Forms.Application.EnableVisualStyles();
				System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
				System.Windows.Forms.Application.Run(new TextEditor.LempDemoForm());
				return;
			}

			KnownOptions["editor"] = Pair.Create("", "Show built-in text editor");

			Severity minSeverity = Severity.Note;
			#if DEBUG
			minSeverity = Severity.Debug;
			#endif
			var filter = new SeverityMessageFilter(MessageSink.Console, minSeverity);

			Compiler c = new Compiler(filter, typeof(BuiltinMacros));

			var argList = args.ToList();
			var options = c.ProcessArguments(argList, false, true);
			if (argList.Count == 0) {
				filter.Write(Severity.Error, null, "No input files provided, stopping.");
				return;
			}
			if (!MaybeShowHelp(options, KnownOptions))
			{
				WarnAboutUnknownOptions(options, filter, 
					KnownOptions.With("nologo", Pair.Create("", "")));
				using (LNode.PushPrinter(EcsNodePrinter.PrintPlainCSharp))
					c.Run();
			}
		}

		#endregion

		#region Static methods

		public static void WarnAboutUnknownOptions(BMultiMap<string, string> options, IMessageSink sink, IDictionary<string, Pair<string, string>> knownOptions)
		{
			foreach (var opt in options.Keys) {
				if (!knownOptions.ContainsKey(opt))
					sink.Write(Severity.Warning, "Command line", "Unrecognized option '--{0}'", opt);
			}
		}

		public static bool MaybeShowHelp(ICollection<KeyValuePair<string, string>> options,
			ICollection<KeyValuePair<string, Pair<string, string>>> knownOptions,
			TextWriter @out = null)
		{
			if (options.Contains(Pair.Create("help", (string)null)) || options.Contains(Pair.Create("?", (string)null)))
			{
				ShowHelp(KnownOptions.OrderBy(p => p.Key), @out);
				return true;
			}
			return false;
		}

		public static void ShowHelp(IEnumerable<KeyValuePair<string, Pair<string, string>>> knownOptions, TextWriter @out = null, bool includeUsageLine = true)
		{
			@out = @out ?? Console.Out;
			if (includeUsageLine)
			{
				var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
				@out.WriteLine("Usage: {0} <--options> <source-files>", asm.GetName().Name);
			}
			@out.WriteLine("Options available:");
			foreach (var kvp in knownOptions.OrderBy(p => p.Key)) {
				string helpInfo = kvp.Value.B.Replace("\n","\n    ");
				if (string.IsNullOrEmpty(kvp.Value.A))
		 			@out.WriteLine("  --{0}: {1}", kvp.Key, helpInfo);
				else if (kvp.Value.A.Contains("="))
		 			@out.WriteLine("  --{0}:{1}: {2}", kvp.Key, kvp.Value.A, helpInfo);
				else
		 			@out.WriteLine("  --{0}={1}: {2}", kvp.Key, kvp.Value.A, helpInfo);
			}
			@out.WriteLine("");
			@out.Flush();
		}

		#endregion

		#region Constructor

		public Compiler(IMessageSink sink, Type prelude = null, bool registerEcsAndLes = true)
		{
			MacroProcessor = new MacroProcessor(prelude, sink);

			if (registerEcsAndLes) {
				ParsingService.Register(Loyc.Syntax.Les.Les2LanguageService.Value);
				ParsingService.Register(Loyc.Syntax.Les.Les3LanguageService.Value);
				ParsingService.Register(Loyc.Ecs.EcsLanguageService.WithPlainCSharpPrinter, new[] { "cs" });
				ParsingService.Register(Loyc.Ecs.EcsLanguageService.Value);
			}
		}

		public Compiler(IMessageSink sink, Type prelude, IEnumerable<InputOutput> sourceFiles)
 			: this(sink, prelude) {
			Files = new List<InputOutput>(sourceFiles);
		}

		#endregion

		#region Processing command-line arguments

		/// <summary>Processes command-line arguments to build a BMultiMap and 
		/// sends those options to the other overload of this method.</summary>
		/// <param name="args">Arg list from which to extract options. **NOTE**:
		/// discovered options are removed from the list. This parameter 
		/// cannot be an array.</param>
		/// <param name="warnAboutUnknownOptions">Whether this method should
		/// call <see cref="WarnAboutUnknownOptions"/> for you.</param>
		/// <param name="autoOpenInputFiles">Whether to open input files 
		/// for you by calling <see cref="OpenSourceFiles(IMessageSink, IEnumerable{string})"/>.
		/// </param>
		/// <param name="inputFiles">A list of input files to open if 
		/// autoOpenInputFiles is true. If this is null, The input files are 
		/// assumed to be those command-line arguments left over after the options 
		/// are removed.</param>
		/// <returns>The map of options (key-value pairs and, for options that 
		/// don't have a value, key-null pairs).</returns>
		/// <remarks>
		/// Note: If you get your command-line arguments as a single 
		/// string, use <see cref="G.SplitCommandLineArguments(string)"/> first 
		/// to split it into an array.
		/// <para/>
		/// This method doesn't check for --help. To implement --help, call 
		/// <see cref="MaybeShowHelp"/> on the return value.
		/// </remarks>
		public BMultiMap<string, string> ProcessArguments(IList<string> args, bool warnAboutUnknownOptions, bool autoOpenInputFiles, IList<string> inputFiles = null)
		{
			BMultiMap<string,string> options = new BMultiMap<string,string>();

			UG.ProcessCommandLineArguments(args, options, "", ShortOptions, TwoArgOptions);
			if (inputFiles == null && autoOpenInputFiles)
				inputFiles = args;
			if (!ProcessArguments(options, warnAboutUnknownOptions, inputFiles))
				return null;
			return options;
		}

		/// <summary>Processes all standard command-line arguments from 
		/// <see cref="KnownOptions"/>, except --help.</summary>
		/// <param name="options">A set of options, presumably derived from command-
		/// line options using <see cref="UG.ProcessCommandLineArguments"/></param>
		/// <param name="warnAboutUnknownOptions">Whether to warn (to <see cref="Sink"/>) 
		/// about options not listed in <see cref="KnownOptions"/>.</param>
		/// <param name="inputFiles">Files to open with <see cref="OpenSourceFiles"/></param>
		/// <returns>true, unless inputFiles != null and all input files failed to open.</returns>
		/// <remarks>
		/// This method calls AddStdMacros() unless options includes "nostdmacros".
		/// </remarks>
		public bool ProcessArguments(BMultiMap<string, string> options, bool warnAboutUnknownOptions, IList<string> inputFiles = null)
		{
			Compiler c = this;
			string value;
			var filter = c.Sink as SeverityMessageFilter ?? new SeverityMessageFilter(c.Sink, Severity.Note);

			if (warnAboutUnknownOptions)
				WarnAboutUnknownOptions(options, Sink, KnownOptions);

			if (options.TryGetValue("verbose", out value))
			{
				if (value != "false") {
					try { // Enum.TryParse() does not exist before .NET 4 so use Enum.Parse
						filter.MinSeverity = (Severity)Enum.Parse(typeof(Severity), value);
					} catch (Exception) { // Docs say OverflowException, but that just sounds wrong
						filter.MinSeverity = Severity.Verbose;
					}
				}
			}
			
			IMessageSink sink = c.Sink = filter;
			
			if (options.TryGetValue("max-expand", out value))
				TryCatch("While parsing max-expand", sink, () => c.MaxExpansions = int.Parse(value));

			foreach (var macroDll in options["macros"])
			{
				Assembly assembly;
				TryCatch("While opening " + macroDll, sink, () =>
				{
					if (macroDll.Contains('\\') || macroDll.Contains('/')) {
						// Avoid "Absolute path information is required" exception
						string fullPath = Path.Combine(Environment.CurrentDirectory, macroDll);
						assembly = Assembly.LoadFile(fullPath);
					} else
						assembly = Assembly.LoadFrom(macroDll);
					c.AddMacros(assembly);
				});
			}
			foreach (string macroDll in options["macros-longname"])
			{
				Assembly assembly;
				TryCatch("While opening " + macroDll, sink, () =>
				{
					assembly = Assembly.Load(macroDll);
					c.AddMacros(assembly);
				});
			}
			if (options.TryGetValue("noparallel", out value) && (value == null || value == "true"))
				c.Parallel = false;
			if (options.TryGetValue("outext", out c.OutExt) && c.OutExt != null && !c.OutExt.StartsWith("."))
				c.OutExt = "." + c.OutExt;
			if (options.TryGetValue("inlang", out value)) {
				ApplyLanguageOption(sink, "--inlang", value, ref c.InLang);
			}
			if (options.TryGetValue("outlang", out value)) {
				IParsingService lang = null;
				ApplyLanguageOption(sink, "--outlang", value, ref lang);
				c.OutLang = lang.Printer ?? c.OutLang;
			}
			if (options.TryGetValue("forcelang", out value) && (value == null || value == "true"))
				c.ForceInLang = true;
			if (!options.ContainsKey("outlang") && c.OutExt != null && ParsingService.GetServiceForFileName(c.OutExt) == null)
				sink.Write(Severity.Error, "--outext", "No language was found for extension «{0}»", c.OutExt);
			double num;
			if (options.TryGetValue("timeout", out value)) {
				if (!double.TryParse(value, out num) || !(num >= 0))
					sink.Write(Severity.Error, "--timeout", "Invalid or missing timeout value", c.OutExt);
				else
					c.AbortTimeout = TimeSpan.FromSeconds(num);
			}
			foreach (string exprStr in options["set"])
				SetPropertyHelper(exprStr, quote: false);
			foreach (string exprStr in options["snippet"])
				SetPropertyHelper(exprStr, quote: true);

			if (!options.TryGetValue("nostdmacros", out value) && !options.TryGetValue("no-std-macros", out value))
				AddStdMacros();

			if (options.TryGetValue("preserve-comments", out value))
				PreserveComments = value == null || !value.ToString().ToLowerInvariant().IsOneOf("false", "0");

			if (inputFiles != null) {
				this.Files = new List<InputOutput>(OpenSourceFiles(Sink, inputFiles));
				if (inputFiles.Count != 0 && Files.Count == 0)
					return false;
			}

			return true;
		}

		/// <summary>Adds standard macros from LeMP.StdMacros.dll, and adds the 
		/// namespaces LeMP and LeMP.Prelude to the pre-opened namespace list.</summary>
		/// <remarks>Note: prelude macros were already added by the constructor.</remarks>
		public void AddStdMacros()
		{
			MacroProcessor.AddMacros(typeof(global::LeMP.StandardMacros).Assembly);
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
		}

		bool SetPropertyHelper(string exprStr, bool quote)
		{
			LNode expr = (InLang ?? ParsingService.Current).ParseSingle(exprStr, Sink, ParsingMode.Expressions);
			if (expr.Calls(CodeSymbols.Assign, 2) && !expr[0].IsCall) {
				object key = expr[0].IsLiteral ? expr[0].Value : expr[0].Name;
				LNode valueN = expr[1];
				object value = valueN.Value;
				if (quote)
					value = valueN.Calls(CodeSymbols.Braces) ? valueN.Args.AsLNode(CodeSymbols.Splice) : valueN;
				if (!(value is NoValue)) {
					MacroProcessor.DefaultScopedProperties[key] = value;
					return true;
				}
			}
			if (quote)
				Sink.Write(Severity.Error, "Command line", "--snippet: syntax error. Expected `key=code` where `key` is a literal or identifier with which to associate a code snippet.");
			else
				Sink.Write(Severity.Error, "Command line", "--set: syntax error. Expected `key=value` where `key` is a literal or identifier with which to associate a value.");
			return false;
		}

		static void ApplyLanguageOption(IMessageSink sink, string option, string value, ref IParsingService lang)
		{
			if (string.IsNullOrEmpty(value))
				sink.Write(Severity.Error, option, "Missing value");
			else {
				if (!value.StartsWith("."))
					value = "." + value;
				if ((lang = ParsingService.GetServiceForFileName(value)) == null)
					sink.Write(Severity.Error, option, "No language was found for extension «{0}»", value);
			}
		}
		
		static bool TryCatch(object context, IMessageSink sink, Action action)
		{
			try {
				action();
				return true;
			} catch (Exception ex) {
				sink.Write(Severity.Error, context, "{0} ({1})", ex.Message, ex.GetType().Name);
				return false;
			}
		}

		#endregion

		public IMessageSink Sink {
			get { return MacroProcessor.Sink; } 
			set { MacroProcessor.Sink = value; }
		}

		public List<InputOutput> Files;
		public int MaxExpansions { get { return MacroProcessor.MaxExpansions; } set { MacroProcessor.MaxExpansions = value; } }
		public TimeSpan AbortTimeout { get { return MacroProcessor.AbortTimeout; } set { MacroProcessor.AbortTimeout = value; } }
		public bool Verbose { get { return Sink.IsEnabled(Severity.Verbose); } }
		public bool Parallel = true;
		public string IndentString = "\t";
		public string NewlineString = "\n";
		public MacroProcessor MacroProcessor; // the core LeMP engine
		public IParsingService InLang;  // null to choose by extension or use ParsingService.Current
		public bool PreserveComments = true; // whether to preserve comments by default, if supported by input and output lang
		public ParsingMode ParsingMode = ParsingMode.File;
		public LNodePrinter OutLang;    // null to use LNode.Printer
		public string OutExt;           // output extension and optional suffix (includes leading '.'); null for same ext
		public bool ForceInLang;        // InLang overrides input file extension

		#region Other stuff

		/// <summary>Fills in all fields of <see cref="Files"/> that are still null,
		/// based on the command-line options. Calling this is optional, since Run()
		/// calls it anyway.</summary>
		public void CompleteInputOutputOptions()
		{
			foreach (var file in Files) CompleteInputOutputOptions(file);
		}
		public void CompleteInputOutputOptions(InputOutput file)
		{
			if (file.InputLang == null) {
				var inLang = InLang ?? ParsingService.Current;
				if (!ForceInLang || InLang == null)
					inLang = ParsingService.GetServiceForFileName(file.FileName) ?? inLang;
				file.InputLang = inLang;
			}
			if (file.OutFileName == null) {
				string inputFN = file.FileName;
				if (OutExt == null)
					file.OutFileName = inputFN;
				else {
					int dot = IndexOfExtension(inputFN);
					file.OutFileName = inputFN.Left(dot) + OutExt;
				}
				if (file.OutFileName == inputFN) {
					// e.g. input.cs => input.out.cs
					int dot = IndexOfExtension(inputFN);
					file.OutFileName = file.OutFileName.Insert(dot, ".out");
				}
			}
			if (file.OutPrinter == null) {
				var outLang = OutLang;
				if (outLang == null && OutExt != null) {
					var lang = ParsingService.GetServiceForFileName(OutExt); 
					if (lang != null) outLang = lang.Printer;
				}
				file.OutPrinter = outLang ?? LNode.Printer;
			}
			if (file.PreserveComments == null)
				file.PreserveComments = PreserveComments;
			if (file.ParsingMode == null)
				file.ParsingMode = ParsingMode;
		}

		private int IndexOfExtension(string fn)
		{
			int dot = fn.LastIndexOf('.');
			if (dot == -1 || fn.IndexOf('/', dot) > -1 && fn.IndexOf('\\', dot) > -1)
				return fn.Length;
			return dot;
		}

		/// <summary>Opens a set of source files by file name, and creates a text file for each.</summary>
		/// <param name="sink"></param>
		/// <param name="fileNames"></param>
		/// <returns></returns>
		public static List<InputOutput> OpenSourceFiles(IMessageSink sink, IEnumerable<string> fileNames)
		{
			var openFiles = new List<InputOutput>();
			foreach (var filename in fileNames) {
				try {
					var stream = File.OpenRead(filename);
					var text = File.ReadAllText(filename, Encoding.UTF8);
					var io = new InputOutput(new StreamCharSource(stream), filename);
					openFiles.Add(io);
				} catch (Exception ex) {
					sink.Write(Severity.Error, filename, ex.GetType().Name + ": " + ex.Message);
				}
			}
			return openFiles;
		}

		public bool AddMacros(Assembly assembly)
		{
			return MacroProcessor.AddMacros(assembly);
		}

		/// <summary>Runs the <see cref="MacroProcessor"/> on all input <see cref="Files"/>.</summary>
		public void Run()
		{
			CompleteInputOutputOptions();
			if (Parallel && Files.Count > 1)
				MacroProcessor.ProcessParallel(Files.AsListSource(), WriteOutput);
			else
				MacroProcessor.ProcessSynchronously(Files.AsListSource(), WriteOutput);
		}

		protected virtual void WriteOutput(InputOutput io)
		{
			Debug.Assert(io.FileName != io.OutFileName);

			Sink.Write(Severity.Verbose, io, "Writing output file: {0}", io.OutFileName);

			using (var stream = File.Open(io.OutFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				var sb = new StringBuilder();
				foreach (LNode node in io.Output) {
					io.OutPrinter(node, sb, Sink, null, IndentString, NewlineString);
					writer.Write(sb.ToString());
					writer.Write(NewlineString);
					sb.Length = 0; // Clear() is new in .NET 4
				}
			}
		}

		#endregion
	}
}
