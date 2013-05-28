using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using Ecs.Parser;

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
			File.WriteAllText(             "EcsLexerGenerated.cs", code, Encoding.UTF8);
			//File.WriteAllText("../../Parser/EcsLexerGenerated.cs", code, Encoding.UTF8);
			//Console.WriteLine(code);
			Console.WriteLine("**** Done.                ****");
			Console.WriteLine("******************************");

			Ecs.Program.Main(args); // do EC# tests
		}
	}
}
