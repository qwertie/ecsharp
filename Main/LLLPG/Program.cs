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
using Loyc.Ecs.Parser;

namespace Loyc.LLParserGenerator
{
	/// <summary>Entry point of LLLPG.exe, with QuickRun() method to help invoke LLLPG programmatically.</summary>
	public class Program // Avoid name collision with 'Program' class in LinqPad
	{
		public static void Main(params string[] args)
		{
			MMap<string, Pair<string, string>> KnownOptions = LeMP.Compiler.KnownOptions;
			if (args.Length != 0) {
				Severity minSeverity = Severity.NoteDetail;
				#if DEBUG
				minSeverity = Severity.DebugDetail;
				#endif
				var filter = new SeverityMessageFilter(ConsoleMessageSink.Value, minSeverity - 1);

				LeMP.Compiler c = new LeMP.Compiler(filter, typeof(LeMP.Prelude.BuiltinMacros));
				var argList = args.ToList();
				var options = c.ProcessArguments(argList, false, true);
				if (!LeMP.Compiler.MaybeShowHelp(options, KnownOptions)) {
					LeMP.Compiler.WarnAboutUnknownOptions(options, ConsoleMessageSink.Value,
						KnownOptions.With("nologo", Pair.Create("","")));
					if (c.Files.Count == 0)
						ConsoleMessageSink.Value.Warning(null, "No files specified, stopping.");
					else {
						c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
						c.MacroProcessor.PreOpenedNamespaces.Add(Loyc.LLPG.Macros.MacroNamespace);
						c.AddMacros(typeof(LeMP.StandardMacros).Assembly);
						c.AddMacros(Assembly.GetExecutingAssembly());
						c.Run();
					}
				}
			} else {
				LeMP.Compiler.ShowHelp(KnownOptions.OrderBy(p => p.Key));
				Test_LLLPG();
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
			var c = new LeMP.TestCompiler(TraceMessageSink.Value, new UString(input));
			c.Parallel = false;
			c.MaxExpansions = maxExpand;
			c.AddMacros(Assembly.GetExecutingAssembly());
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			c.MacroProcessor.PreOpenedNamespaces.Add(Loyc.LLPG.Macros.MacroNamespace);
			foreach (var assembly in macroAssemblies)
				c.AddMacros(assembly);
			using (ParsingService.SetDefault(inputLang ?? ParsingService.Default))
			using (LNode.SetPrinter(Ecs.EcsLanguageService.Value))
				c.Run();
			return c.Output.ToString();
		}

		public static int Test_LLLPG()
		{
			Console.WriteLine("Running tests... (a small number of them are broken)");

			#if DotNet3 || DotNet4
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );
			#endif

			return RunTests.RunMany(
				new LlpgParserTests(),
				new LlpgGeneralTests(),
				new LlpgCoreTests(),
				new LlpgAutoValueSaverVisitorTests(),
				new LlpgTestLargerExamples(),
				new LlpgBugsAndSlugs());
		}
	}
}
