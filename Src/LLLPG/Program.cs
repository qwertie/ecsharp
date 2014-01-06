using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Ecs.Parser;
using Loyc.Syntax.Les;
using Loyc.Collections;
using Loyc.Utilities;
using System.Reflection;
using Loyc.Syntax;
using System.Diagnostics;

namespace Loyc.LLParserGenerator
{
	public class LLLPG // Avoid name collision with 'Program' class in LinqPad
	{
		public static void Main(params string[] args)
		{
			if (args.Length != 0) {
				BMultiMap<string,string> options = new BMultiMap<string,string>();
				Dictionary<string, Pair<string, string>> KnownOptions = LEL.Compiler.KnownOptions;

				var argList = args.ToList();
				UG.ProcessCommandLineArguments(argList, options, "", LEL.Compiler.ShortOptions, LEL.Compiler.TwoArgOptions);
				if (!options.ContainsKey("nologo"))
					Console.WriteLine("LLLPG/LeMP macro compiler (pre-alpha)");

				string _;
				if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _)) {
					LEL.Compiler.ShowHelp(KnownOptions);
					return;
				}

				Symbol minSeverity = MessageSink.Note;
				#if DEBUG
				minSeverity = MessageSink.Debug;
				#endif
				var filter = new SeverityMessageFilter(MessageSink.Console, minSeverity);

				LEL.Compiler c = LEL.Compiler.ProcessArguments(argList, options, filter, typeof(LEL.Prelude.Macros));
				LEL.Compiler.WarnAboutUnknownOptions(options, MessageSink.Console, KnownOptions);
				if (c != null) {
					c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LEL.Prelude"));
					c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLParserGenerator"));
					c.AddMacros(Assembly.GetExecutingAssembly());
					using (LNode.PushPrinter(Ecs.EcsNodePrinter.PrintPlainCSharp))
						c.Run();
				}
			} else {
				Tests();
				Ecs.Program.Main(args); // do EC# tests
			}
		}

		/// <summary>Run macro processor for LLLPG on the specified input, with the
		/// specified command-line option map, returning the result as a string.</summary>
		public static string QuickRun(string input, int maxExpand = 0xFFFF, params Assembly[] macroAssemblies)
		{
			var source = new StringCharSourceFile(input, "");
			var c = new LEL.TestCompiler(MessageSink.Trace, source);
			c.Parallel = false;
			c.MaxExpansions = maxExpand;
			c.AddMacros(Assembly.GetExecutingAssembly());
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LEL.Prelude"));
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLParserGenerator"));
			foreach (var assembly in macroAssemblies)
				c.AddMacros(assembly);
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.Printer))
				c.Run();
			return c.Output.ToString();
		}

		static void Tests()
		{
			Console.WriteLine("Running tests...");

			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );

			RunTests.Run(new IntSetTests());
			RunTests.Run(new LNodeTests());
			RunTests.Run(new Loyc.Syntax.Les.LesLexerTests());
			RunTests.Run(new Loyc.Syntax.Les.LesParserTests());
			RunTests.Run(new LEL.MacroProcessorTests());
			RunTests.Run(new LlpgCoreTests());
			RunTests.Run(new LlpgParserTests());
			RunTests.Run(new LlpgGeneralTests());

			Console.WriteLine("******************************");
			Console.WriteLine("**** Generating LES lexer ****");
			string code = new LesLexerGenerator().GenerateLexerCode().Print();
			code = string.Format(@"using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using Loyc;
				using Loyc.LLParserGenerator;
				using Loyc.Syntax;
				using Loyc.Syntax.Lexing;

				namespace Loyc.Syntax.Les
				{{
					using TT = TokenType;

					{0}
				}}".Replace("\t\t\t\t", ""),
				code.Replace("\n","\r\n\t"));
			File.WriteAllText("LesLexerGenerated.cs", code, Encoding.UTF8);
			Console.WriteLine("**** Done.                ****");
			Console.WriteLine("******************************");

			Console.WriteLine("******************************");
			Console.WriteLine("**** Generating LES parser ***");
			code = new LesParserGenerator().GenerateParserCode().Print();
			code = string.Format(@"using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using Loyc;
				using Loyc.LLParserGenerator;
				using Loyc.Collections;
				using Loyc.Syntax;
				using Loyc.Syntax.Lexing;

				namespace Loyc.Syntax.Les
				{{
					using TT = TokenType;
					using S = CodeSymbols;
					using P = LesPrecedence;

					{0}
				}}".Replace("\t\t\t\t", ""),
				code.Replace("\n","\r\n\t"));
			File.WriteAllText("LesParserGenerated.cs", code, Encoding.UTF8);
			Console.WriteLine("**** Done.                ****");
			Console.WriteLine("******************************");

			RunTests.Run(new Loyc.Syntax.Lexing.TokensToTreeTests());
			RunTests.Run(new Loyc.Syntax.Les.LesPrinterTests());
		}
	}
}
