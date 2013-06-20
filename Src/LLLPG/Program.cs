using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Ecs.Parser;
using Loyc.Syntax.Les;

namespace Loyc.LLParserGenerator
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Running tests...");
			RunTests.Run(new IntSetTests());
			RunTests.Run(new LlpgTests());

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
				using Loyc.LLParserGenerator;
				using Loyc.Syntax;
				using Loyc;

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
				using Loyc.LLParserGenerator;
				using Loyc.Collections;
				using Loyc.Syntax;
				using Loyc;

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

			RunTests.Run(new Loyc.Syntax.Les.TokensToTreeTests());
			RunTests.Run(new Loyc.Syntax.Les.LesLexerTests());

			Ecs.Program.Main(args); // do EC# tests
		}
	}
}
