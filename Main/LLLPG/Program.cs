using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using Loyc.MiniTest;
using Loyc.Syntax.Les;
using Loyc.Collections;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Syntax.Tests;
using Ecs.Parser;

namespace Loyc.LLParserGenerator
{
	/// <summary>Entry point of LLLPG.exe, with QuickRun() method to help invoke LLLPG programmatically.</summary>
	public class LLLPG // Avoid name collision with 'Program' class in LinqPad
	{
		public static void Main(params string[] args)
		{
			IDictionary<string, Pair<string, string>> KnownOptions = LeMP.Compiler.KnownOptions;
			if (args.Length != 0) {
				BMultiMap<string,string> options = new BMultiMap<string,string>();

				var argList = args.ToList();
				UG.ProcessCommandLineArguments(argList, options, "", LeMP.Compiler.ShortOptions, LeMP.Compiler.TwoArgOptions);
				if (!options.ContainsKey("nologo"))
					Console.WriteLine("LLLPG/LeMP macro compiler (alpha)");

				string _;
				if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _) || args.Length == 0) {
					LeMP.Compiler.ShowHelp(KnownOptions.OrderBy(p => p.Key));
					return;
				}

				Severity minSeverity = Severity.Note;
				#if DEBUG
				minSeverity = Severity.Debug;
				#endif
				var filter = new SeverityMessageFilter(MessageSink.Console, minSeverity);

				LeMP.Compiler c = LeMP.Compiler.ProcessArguments(options, filter, typeof(LeMP.Prelude.Les.Macros), argList);
				LeMP.Compiler.WarnAboutUnknownOptions(options, MessageSink.Console, KnownOptions);
				if (c != null) {
					c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
					c.MacroProcessor.PreOpenedNamespaces.Add(Loyc.LLPG.Macros.MacroNamespace);
					c.AddMacros(Assembly.GetExecutingAssembly());
					c.AddMacros(typeof(LeMP.Prelude.Macros).Assembly);
					using (LNode.PushPrinter(Ecs.EcsNodePrinter.PrintPlainCSharp))
						c.Run();
				}
			} else {
				LeMP.Compiler.ShowHelp(KnownOptions.OrderBy(p => p.Key));
				Tests();
				Ecs.Program.Main(args); // do EC# tests
			}
		}

		/// <summary>Run macro processor for LLLPG on the specified input, with the
		/// specified command-line option map, returning the result as a string.</summary>
		public static string QuickRun(string input, params Assembly[] macroAssemblies)
		{
			return QuickRun(null, 0xFFFF, input, macroAssemblies);
		}
		public static string QuickRun(IParsingService inputLang, int maxExpand, string input, params Assembly[] macroAssemblies)
		{
			var c = new LeMP.TestCompiler(MessageSink.Trace, new UString(input));
			c.Parallel = false;
			c.MaxExpansions = maxExpand;
			c.AddMacros(Assembly.GetExecutingAssembly());
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
			c.MacroProcessor.PreOpenedNamespaces.Add(Loyc.LLPG.Macros.MacroNamespace);
			foreach (var assembly in macroAssemblies)
				c.AddMacros(assembly);
			using (ParsingService.PushCurrent(inputLang ?? ParsingService.Current))
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.Printer))
				c.Run();
			return c.Output.ToString();
		}

		static void Tests()
		{
			Console.WriteLine("Running tests... (a small number of them are broken)");

			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );

			RunTests.Run(new IntSetTests());
			//RunTests.Run(new LNodeTests());
			RunTests.Run(new LlpgParserTests());
			RunTests.Run(new LlpgGeneralTests());
			RunTests.Run(new Loyc.Syntax.Lexing.TokenTests());
			RunTests.Run(new Loyc.Syntax.Les.LesLexerTests());
			RunTests.Run(new Loyc.Syntax.Les.LesParserTests());
			RunTests.Run(new Loyc.Syntax.Les.LesPrinterTests());
			RunTests.Run(new LexerSourceTests_Calculator());
			RunTests.Run(new ParserSourceTests_Calculator());
			RunTests.Run(new LeMP.MacroProcessorTests());
			RunTests.Run(new LeMP.StandardMacroTests());
			RunTests.Run(new LlpgCoreTests());
			RunTests.Run(new LlpgAutoValueSaverVisitorTests());
			RunTests.Run(new LlpgTestLargerExamples());
			RunTests.Run(new LlpgBugsAndSlugs());
			RunTests.Run(new Loyc.Syntax.Lexing.TokensToTreeTests());
			RunTests.Run(new Loyc.Syntax.Les.LesPrinterTests());
		}
	}
}
