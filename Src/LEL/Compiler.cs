using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using LeMP.Prelude;
using Loyc;
using System.Threading.Tasks;
using Loyc.Threading;
using NUnit.Framework;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace LeMP
{
	/// <summary>A class that helps you invoke <see cref="MacroProcessor"/> on a 
	/// set of source files.</summary>
	/// <remarks>
	/// Helps you process command-line options (e.g. --outext=cs), complete 
	/// <see cref="InputOutput"/> objects based on those options (see <see 
	/// cref="CompleteInputOutputOptions"/>), and add macros from Assemblies 
	/// (<see cref="AddMacros"/>).
	/// </remarks>
	public class Compiler
	{
		/// <summary>A list of available syntaxes.</summary>
		public static HashSet<IParsingService> Languages = new HashSet<IParsingService> { 
			Loyc.Syntax.Les.LesLanguageService.Value,
			Ecs.Parser.EcsLanguageService.Value,
			Ecs.Parser.EcsLanguageService.WithPlainCSharpPrinter
		};

		#region Command-line interface

		public static Dictionary<char, string> ShortOptions = new Dictionary<char,string>()
			{ {'o',"out"}, {'m',"macros"} };

		public static void Main(string[] args)
		{
			BMultiMap<string,string> options = new BMultiMap<string,string>();

			var argList = args.ToList();
			UG.ProcessCommandLineArguments(argList, options, "", ShortOptions, TwoArgOptions);
			if (!options.ContainsKey("nologo"))
				Console.WriteLine("LeMP macro compiler (pre-alpha)");

			string _;
			if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _)) {
				ShowHelp(KnownOptions.OrderBy(p => p.Key));
				return;
			}

			Symbol minSeverity = MessageSink.Note;
			#if DEBUG
			minSeverity = MessageSink.Debug;
			#endif
			var filter = new SeverityMessageFilter(MessageSink.Console, minSeverity);

			Compiler c = ProcessArguments(argList, options, filter, typeof(Macros));
			Compiler.WarnAboutUnknownOptions(options, MessageSink.Console, KnownOptions);
			if (c != null) {
				c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
				using (LNode.PushPrinter(Ecs.EcsNodePrinter.PrintPlainCSharp))
					c.Run();
			} else if (args.Length == 0) {
				Console.WriteLine("Running unit tests...");
				RunTests.Run(new MacroProcessorTests());
			}
		}

		public static Compiler ProcessArguments(List<string> inputFiles, BMultiMap<string, string> options, SeverityMessageFilter sink, Type prelude)
		{
			if (inputFiles.Count == 0)
			{
				sink.Write(MessageSink.Error, null, "No input provided, stopping.");
				return null;
			}

			string value;
			if (options.TryGetValue("verbose", out value) && value != "false")
			{
				int sev;
				if ((sev = MessageSink.GetSeverity(GSymbol.GetIfExists(value))) > -1)
					sink.MinSeverity = sev;
				else
					sink.MinSeveritySymbol = MessageSink.Verbose;
			}

			var c = new Compiler(sink, prelude, inputFiles);
			return ProcessArguments(c, options) ? c : null;
		}
		public static bool ProcessArguments(Compiler c, BMultiMap<string, string> options)
		{
			IMessageSink sink = c.Sink;
			if (c.Files.Count == 0)
				return false;

			string value;
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
			foreach (var macroDll in options["macros-longname"])
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
			if (!options.ContainsKey("outlang") && c.OutExt != null && FileNameToLanguage(c.OutExt) == null)
				sink.Write(MessageSink.Error, "--outext", "No language was found for extension «{0}»", c.OutExt);
			double num;
			if (options.TryGetValue("timeout", out value)) {
				if (!double.TryParse(value, out num) || !(num >= 0))
					sink.Write(MessageSink.Error, "--timeout", "Invalid or missing timeout value", c.OutExt);
				else
					c.AbortTimeout = TimeSpan.FromSeconds(num);
			}

			return true;
		}
		static void ApplyLanguageOption(IMessageSink sink, string option, string value, ref IParsingService lang)
		{
			if (string.IsNullOrEmpty(value))
				sink.Write(MessageSink.Error, option, "Missing value");
			else {
				if (!value.StartsWith("."))
					value = "." + value;
				if ((lang = FileNameToLanguage(value)) == null)
					sink.Write(MessageSink.Error, option, "No language was found for extension «{0}»", value);
			}
		}
		
		static bool TryCatch(object context, IMessageSink sink, Action action)
		{
			try {
				action();
				return true;
			} catch (Exception ex) {
				sink.Write(MessageSink.Error, context, "{0} ({1})", ex.Message, ex.GetType().Name);
				return false;
			}
		}

		public static void WarnAboutUnknownOptions(BMultiMap<string, string> options, IMessageSink sink, IDictionary<string, Pair<string, string>> knownOptions)
		{
			foreach (var opt in options.Keys) {
				if (!knownOptions.ContainsKey(opt))
					sink.Write(MessageSink.Warning, "Command line", "Unrecognized option '--{0}'", opt);
			}
		}

		public static MMap<string, Pair<string, string>> KnownOptions = new MMap<string, Pair<string, string>>()
		{
 			{ "help",      Pair.Create("", "show this screen") },
 			{ "macros",    Pair.Create("filename.dll", "load macros from given assembly") },
 			{ "max-expand",Pair.Create("N", "stop expanding macros after N expansions.") },
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
 			{ "timeout",   Pair.Create("", "Aborts the processing thread(s) after this number of seconds\n(0=never, default=30)") },
		};
		public static InvertibleSet<string> TwoArgOptions = new InvertibleSet<string>(new[] { "macros" });

		public static void ShowHelp(IEnumerable<KeyValuePair<string, Pair<string, string>>> knownOptions)
		{
			var asm = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
 			Console.WriteLine("Usage: {0} <--options> <source-files>", asm.GetName().Name);
 			Console.WriteLine("Options available:");
			foreach (var kvp in knownOptions) {
				string helpInfo = kvp.Value.B.Replace("\n","\n    ");
				if (string.IsNullOrEmpty(kvp.Value.A))
		 			Console.WriteLine("  --{0}: {1}", kvp.Key, helpInfo);
				else
		 			Console.WriteLine("  --{0}={1}: {2}", kvp.Key, kvp.Value.A, helpInfo);
			}
			Console.WriteLine("");
		}

		#endregion

		public Compiler(IMessageSink sink, Type prelude = null)
		{
			MacroProcessor = new MacroProcessor(prelude, sink);
		}
		public Compiler(IMessageSink sink, Type prelude, IEnumerable<InputOutput> sourceFiles)
 			: this(sink, prelude) {
			Files = new List<InputOutput>(sourceFiles);
		}
		public Compiler(IMessageSink sink, Type prelude, IEnumerable<string> sourceFiles)
 			: this(sink, prelude) {
			Files = new List<InputOutput>(OpenSourceFiles(sink, sourceFiles));
		}

		public IMessageSink Sink {
			get { return MacroProcessor.Sink; } 
			set { MacroProcessor.Sink = value; }
		}

		public List<InputOutput> Files;
		public int MaxExpansions { get { return MacroProcessor.MaxExpansions; } set { MacroProcessor.MaxExpansions = value; } }
		public TimeSpan AbortTimeout { get { return MacroProcessor.AbortTimeout; } set { MacroProcessor.AbortTimeout = value; } }
		public bool Verbose { get { return Sink.IsEnabled(MessageSink.Verbose); } }
		public bool Parallel = true;
		public string IndentString = "\t";
		public string NewlineString = "\r\n";
		public MacroProcessor MacroProcessor; // the core LeMP engine
		public IParsingService InLang;  // null to choose by extension or use ParsingService.Current
		public LNodePrinter OutLang;    // null to use LNode.Printer
		public string OutExt;           // output extension and optional suffix (includes leading '.'); null for same ext
		public bool ForceInLang;        // InLang overrides input file extension
		
		/// <summary>Fills in all fields of <see cref="Files"/> that are still null,
		/// based on the command-line options.</summary>
		public void CompleteInputOutputOptions()
		{
			foreach (var file in Files) CompleteInputOutputOptions(file);
		}
		public void CompleteInputOutputOptions(InputOutput file)
		{
			if (file.InputLang == null) {
				var inLang = InLang ?? ParsingService.Current;
				if (!ForceInLang || InLang == null)
					inLang = FileNameToLanguage(file.FileName) ?? inLang;
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
					var lang = FileNameToLanguage(OutExt); 
					if (lang != null) outLang = lang.Printer;
				}
				file.OutPrinter = outLang ?? LNode.Printer;
			}
		}

		private int IndexOfExtension(string fn)
		{
			int dot = fn.LastIndexOf('.');
			if (dot == -1 || fn.IndexOf('/', dot) > -1 && fn.IndexOf('\\', dot) > -1)
				return fn.Length;
			return dot;
		}

		/// <summary>Finds a language service in ExtensionToLanguage() for the 
		/// specified file extension, or null if there is no match.</summary>
		public static IParsingService FileNameToLanguage(string fn)
		{
			return Languages.FirstOrDefault(lang => fn.EndsWith(lang.ToString()))
				?? Languages
				.Where(lang => lang.FileExtensions.Any(ext => ExtensionMatches(ext, fn)))
				.MinOrDefault(lang => lang.FileExtensions.IndexWhere(ext => ExtensionMatches(ext, fn)));
		}
		static bool ExtensionMatches(string ext, string fn)
		{
			return fn.Length > ext.Length && fn[fn.Length - ext.Length - 1] == '.' && fn.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
		}
		
		private static List<InputOutput> OpenSourceFiles(IMessageSink sink, IEnumerable<string> fileNames)
		{
			var openFiles = new List<InputOutput>();
			foreach (var filename in fileNames) {
				try {
					var text = File.ReadAllText(filename, Encoding.UTF8);
					openFiles.Add(new InputOutput(new StringSlice(text), filename));
				} catch (Exception ex) {
					sink.Write(MessageSink.Error, filename, ex.GetType().Name + ": " + ex.Message);
				}
			}
			return openFiles;
		}

		public bool AddMacros(Assembly assembly)
		{
			bool any = false;
			foreach (Type type in assembly.GetExportedTypes()) {
				if (!type.IsGenericTypeDefinition &&
					type.GetCustomAttributes(typeof(ContainsMacrosAttribute), true).Any())
				{
					if (Verbose)
						Sink.Write(MessageSink.Verbose, assembly.GetName().Name, "Adding macros in type '{0}'", type);
					any = MacroProcessor.AddMacros(type) || any;
				}
			}
			if (!any)
				Sink.Write(MessageSink.Warning, assembly, "No macros found");
			return any;
		}

		public void Run()
		{
			CompleteInputOutputOptions();
			if (Parallel)
				MacroProcessor.ProcessParallel(Files.AsListSource(), WriteOutput);
			else
				MacroProcessor.ProcessSynchronously(Files.AsListSource(), WriteOutput);
		}

		protected virtual void WriteOutput(InputOutput io)
		{
			if (Parallel)
				// attach to parent so that ProcessParallel does not exit before file is written
				Task.Factory.StartNew(() => WriteOutput2(io), TaskCreationOptions.AttachedToParent);
			else
				WriteOutput2(io);
		}

		private void WriteOutput2(InputOutput io)
		{
			Debug.Assert(io.FileName != io.OutFileName);

			Sink.Write(MessageSink.Verbose, io, "Writing output file: {0}", io.OutFileName);

			using (var stream = File.Open(io.OutFileName, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				var sb = new StringBuilder();
				foreach (LNode node in io.Output) {
					io.OutPrinter(node, sb, Sink, null, IndentString, NewlineString);
					writer.Write(sb.ToString());
					writer.Write(NewlineString);
					sb.Clear();
				}
			}
		}
	}
}
