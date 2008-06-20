using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.BooStyle;
using Loyc.Runtime;
using Loyc.Utilities;
using Loyc.CompilerCore;
using Loyc.CompilerCore.ExprParsing;
using Loyc.CompilerCore.ExprNodes;
using Loyc.Compatibility.Linq;

namespace Loyc.BooStyle.Tests
{
	class Program
	{
		public static PoorMansLinq<T> Linq<T>(IEnumerable<T> source)
		{
			return new PoorMansLinq<T>(source);
		}
		public static void Main(string[] args)
		{
			string[] words = new string[] { "Pies", "Are", "Good", "In", "Lovely", "Apples" };
			// Pies Are Good
			Console.WriteLine(string.Join(" ", Linq(words).Take(3).ToArray()));
			// Apples Are Good In Lovely Pies
			Console.WriteLine(string.Join(" ", Linq(words).Sorted().ToArray()));

			int[] numbers = new int[] { 4, 95, 309, 357, 233, 2 };
			// 1000
			Console.WriteLine(Enumerable.Sum(numbers));
			// 666
			Console.WriteLine(Enumerable.Sum(Linq(numbers)
				.Where(delegate(int x) { return x > 300; })));

			Console.WriteLine("Running tests on stable code...");
			RunTests.Run(new GTests());
			RunTests.Run(new StringCharSourceTests());
			RunTests.Run(new SymbolTests());
			RunTests.Run(new ExtraAttributesTests());
			RunTests.Run(new RVListTests());
			RunTests.Run(new VListTests());

			for(;;) {
				ConsoleKeyInfo k;
				string s;
				Console.WriteLine();
				Console.WriteLine("What do you want to do?");
				Console.WriteLine("1. Run unit tests on unstable code");
				Console.WriteLine("2. Try out BooLexer");
				Console.WriteLine("3. Try out BasicOneParser with standard operator set");
				Console.WriteLine("Z. List encodings");
				Console.WriteLine("Press ESC or ENTER to Quit");
				Console.WriteLine((k = Console.ReadKey(true)).KeyChar);
				if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter)
					break;
				else if (k.KeyChar == '1') {
					RunTests.Run(new StreamCharSourceTests(Encoding.Unicode, 256));
					RunTests.Run(new StreamCharSourceTests(Encoding.Unicode, 16));
					RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 256));
					RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 16));
					RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 27));
					RunTests.Run(new StreamCharSourceTests(Encoding.UTF32, 64));
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), false));
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), true));
					RunTests.Run(new BooLexerTest());
				} else if (k.KeyChar == '2') {
					Console.WriteLine("Boo Lexer: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						Lexer(null, s);
				} else if (k.KeyChar == '3') {
					Console.WriteLine("BasicOneParser: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						OneParser(null, s);
				} else if (k.KeyChar == 'z' || k.KeyChar == 'Z') {
					foreach (EncodingInfo inf in Encoding.GetEncodings())
						Console.WriteLine("{0} {1}: {2}", inf.CodePage, inf.Name, inf.DisplayName);
				} 
			}
		}
		/*static void ParseBug(string s)
		{
			System.Console.WriteLine(s);
			ANTLRStringStream input = new ANTLRStringStream(s);
			Lexer lexerBug = new Bug1Lexer(input);
			AstNode t;
			while ((t = lexerBug.NextToken()).Type != BooLexer.EOF) {
				System.Console.WriteLine("{0} <{1}>",
					BooTreeParser.tokenNames[t.Type], t.Text);
			}
			System.Console.WriteLine("");
		}*/
		static void Lexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(lang, s);
			BooLexer lexer = new BooLexer(input, new Dictionary<string, Symbol>(), false);

			foreach (Loyc.CompilerCore.AstNode t in lexer) {
				System.Console.WriteLine("{0} <{1}>", t.NodeType, t.Text);
			}
		}
		private static void OneParser(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(lang, s);
			IEnumerable<AstNode> lexer = new BooLexer(input, new Dictionary<string, Symbol>(), false);
			IEnumerable<AstNode> filter = new VisibleTokenFilter<AstNode>(lexer);
			IOneParser<AstNode> parser = new BasicOneParser<AstNode>();

			foreach (Loyc.CompilerCore.AstNode t in lexer) {
				System.Console.WriteLine("{0} <{1}>", t.NodeType, t.Text);
			}
		}
	}
}
