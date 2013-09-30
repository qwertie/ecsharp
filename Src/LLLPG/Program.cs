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

namespace Loyc.LLParserGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 0) {
				BMultiMap<string,string> options = new BMultiMap<string,string>();
				Dictionary<string, Pair<string, string>> KnownOptions = LEL.Compiler.KnownOptions;

				var argList = args.ToList();
				UG.ProcessCommandLineArguments(argList, options, "", LEL.Compiler.ShortOptions, LEL.Compiler.TwoArgOptions);
				if (!options.ContainsKey("nologo"))
					Console.WriteLine("LLLPG/Micro-LEL macro compiler (pre-alpha)");

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

				LEL.Compiler c = LEL.Compiler.ProcessArguments(argList, options, filter, typeof(Macros));
				if (c != null) {
					LEL.Compiler.WarnAboutUnknownOptions(options, MessageSink.Console, KnownOptions);

					c.AddMacros(Assembly.GetExecutingAssembly());
					c.Run();
				}
			}
			
			Tests();
			Ecs.Program.Main(args); // do EC# tests
		}
		static void Tests()
		{
			Console.WriteLine("Running tests...");
			RunTests.Run(new IntSetTests());
			RunTests.Run(new LEL.MacroProcessorTests());
			RunTests.Run(new LlpgTests());
			RunTests.Run(new LlpgParserTests());

			Console.WriteLine("******************************");
			Console.WriteLine("**** Generating EC# lexer ****");
			string code = new EcsLexerGenerator().GenerateLexerCode().Print();
			code = string.Format(@"using System;
				using System.Collections.Generic;
				using System.Linq;
				using System.Text;
				using Loyc.LLParserGenerator;
				using Loyc.Syntax;
				using Loyc;

				namespace Ecs.Parser
				{{
					using TT = TokenType;

					{0}
				}}".Replace("\t\t\t\t", ""),
				code.Replace("\n","\r\n\t"));
			File.WriteAllText("EcsLexerGenerated.cs", code, Encoding.UTF8);
			Console.WriteLine("**** Done.                ****");
			Console.WriteLine("******************************");

			Console.WriteLine("******************************");
			Console.WriteLine("**** Generating LES lexer ****");
			code = new LesLexerGenerator().GenerateLexerCode().Print();
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
			RunTests.Run(new Loyc.Syntax.Les.LesParserTests());
			RunTests.Run(new Loyc.Syntax.Les.LesLexerTests());
			RunTests.Run(new Loyc.Syntax.Les.LesPrinterTests());
		}
	}
}
