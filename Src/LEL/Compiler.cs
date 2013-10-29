using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Utilities;
using Loyc.Syntax;
using System.Reflection;
using Loyc.Collections;
using System.IO;
using LEL.Prelude;
using Loyc;
using System.Threading.Tasks;
using Loyc.Threading;
using NUnit.Framework;

namespace LEL
{
	/// <summary>A class that helps you invoke <see cref="MacroProcessor"/> on a 
	/// set of source files. Allows you to add macros from Assemblies (<see cref="AddMacros"/>).
	/// Also encapsulates a command-line interface in Main().</summary>
	public class Compiler
	{
		#region Command-line interface

		public static Dictionary<char, string> ShortOptions = new Dictionary<char,string>()
			{ {'o',"out"}, {'m',"macros"} };

		public static void Main(string[] args)
		{
			BMultiMap<string,string> options = new BMultiMap<string,string>();

			var argList = args.ToList();
			UG.ProcessCommandLineArguments(argList, options, "", ShortOptions, TwoArgOptions);
			if (!options.ContainsKey("nologo"))
				Console.WriteLine("Micro-LEL macro compiler (pre-alpha)");

			string _;
			if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _)) {
				ShowHelp(KnownOptions);
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
				c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LEL.Prelude"));
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

			var c = new Compiler(sink, inputFiles, prelude);
			return ProcessArguments(c, options) ? c : null;
		}
		public static bool ProcessArguments(Compiler c, BMultiMap<string, string> options)
		{
			IMessageSink sink = c.Sink;
			if (c.InputFiles.Count == 0)
				return false;

			string value;
			if (options.TryGetValue("max-expand", out value))
				TryCatch("While parsing max-expand", sink, () => c.MaxExpansions = int.Parse(value));

			foreach (var macroDll in options["macros"])
			{
				Assembly assembly = null;
				TryCatch("While opening " + macroDll, sink, () =>
				{
					if (macroDll.Contains('\\') || macroDll.Contains('/'))
						assembly = Assembly.LoadFile(macroDll);
					else
						assembly = Assembly.LoadFrom(macroDll);
				});
				c.AddMacros(assembly);
			}
			foreach (var macroDll in options["macros-longname"])
			{
				Assembly assembly = null;
				TryCatch("While opening " + macroDll, sink, () =>
				{
					assembly = Assembly.Load(macroDll);
					c.AddMacros(assembly);
				});
			}
			if (options.TryGetValue("noparallel", out value) && (value == null || value == "true"))
				c.Parallel = false;

			return true;
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

		public static void WarnAboutUnknownOptions(BMultiMap<string, string> options, IMessageSink sink, Dictionary<string, Pair<string, string>> knownOptions)
		{
			foreach (var opt in options.Keys) {
				if (!knownOptions.ContainsKey(opt))
					sink.Write(MessageSink.Warning, "Command line", "Unrecognized option '--{0}'", opt);
			}
		}

		public static Dictionary<string, Pair<string, string>> KnownOptions = new Dictionary<string, Pair<string, string>>()
		{
 			{ "help",      Pair.Create("", "show this screen") },
 			{ "macros",    Pair.Create("filename.dll", "load macros from given assembly\n(by default, just LEL 'prelude' macros are available)") },
 			{ "max-expand",Pair.Create("N", "stop expanding macros after N expansions.") },
 			{ "verbose",   Pair.Create("", "Print extra status messages (e.g. discovered Types, list output files).") },
 			{ "parallel",  Pair.Create("", "Process all files in parallel (this is the default)") },
			{ "noparallel",Pair.Create("", "Process all files in sequence") },
		};
		public static InvertibleSet<string> TwoArgOptions = new InvertibleSet<string>(new[] { "macros" });

		public static void ShowHelp(Dictionary<string, Pair<string, string>> knownOptions)
		{
 			Console.WriteLine("Usage: {0} <--options> <source-files>", Assembly.GetEntryAssembly().GetName().Name);
 			Console.WriteLine("Options available:");
			foreach (var kvp in knownOptions) {
				string helpInfo = kvp.Value.B.Replace("\n","\n    ");
				if (string.IsNullOrEmpty(kvp.Value.A))
		 			Console.WriteLine("  --{0}: {1}", kvp.Key, helpInfo);
				else
		 			Console.WriteLine("  --{0}={1}: {2}", kvp.Key, kvp.Value.A, helpInfo);
			}
			Console.WriteLine("");
			Console.WriteLine("Currently, the input format is always LES and the output syntax is always C#/EC#");
		}

		#endregion

		public Compiler(IMessageSink sink, IEnumerable<string> sourceFiles, Type prelude) : this(sink, OpenSourceFiles(sink, sourceFiles), prelude) { }
		public Compiler(IMessageSink sink, IEnumerable<ISourceFile> sourceFiles, Type prelude) 
		{ 
			InputFiles = new List<ISourceFile>(sourceFiles);
			MacroProcessor = new MacroProcessor(prelude, sink);
		}

		public IMessageSink Sink {
			get { return MacroProcessor.Sink; } 
			set { MacroProcessor.Sink = value; }
		}

		public List<ISourceFile> InputFiles;
		public int MaxExpansions { get { return MacroProcessor.MaxExpansions; } set { MacroProcessor.MaxExpansions = value; } }
		public bool Verbose { get { return Sink.IsEnabled(MessageSink.Verbose); } }
		public bool Parallel = true;
		public string IndentString = "\t";
		public string NewlineString = "\r\n";
		public MacroProcessor MacroProcessor;
		
		private static List<ISourceFile> OpenSourceFiles(IMessageSink sink, IEnumerable<string> fileNames)
		{
			var openFiles = new List<ISourceFile>();
			foreach (var filename in fileNames) {
				try {
					var text = File.ReadAllText(filename, Encoding.UTF8);
					openFiles.Add(new StringCharSourceFile(text, filename));
				} catch (Exception ex) {
					sink.Write(MessageSink.Error, filename, ex.GetType().Name + ": " + ex.Message);
				}
			}
			return openFiles;
		}

		public bool AddMacros(Assembly assembly)
		{
			bool any = false;
			foreach (Type type in assembly.GetTypes()) {
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
			if (Parallel)
				MacroProcessor.ProcessParallel(InputFiles.AsListSource(), WriteOutput);
			else
				MacroProcessor.ProcessSynchronously(InputFiles.AsListSource(), WriteOutput);
		}

		protected virtual void WriteOutput(ISourceFile file, RVList<LNode> results)
		{
			var printer = LNode.Printer;
			if (Parallel)
				// attach to parent so that ProcessParallel does not exit before file is written
				Task.Factory.StartNew(() => WriteOutput2(file, results, printer), TaskCreationOptions.AttachedToParent);
			else
				WriteOutput2(file, results, printer);
		}

		private void WriteOutput2(ISourceFile file, RVList<LNode> results, LNodePrinter printer)
		{
			var @out = Path.ChangeExtension(file.FileName, "cs");
			if (file.FileName == @out)
				@out += "2";

			Sink.Write(MessageSink.Verbose, file.FileName, "Writing output file: {0}", @out);

			using (var stream = File.Open(@out, FileMode.Create, FileAccess.Write, FileShare.Read))
			using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
				var sb = new StringBuilder();
				foreach (LNode node in results) {
					printer(node, sb, Sink, null, IndentString, NewlineString);
					writer.Write(sb.ToString());
					writer.Write(NewlineString);
					sb.Clear();
				}
			}
		}
	}
}
