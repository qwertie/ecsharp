using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Loyc.BooStyle;
using Loyc.BooStyle.Tests;
using Loyc.Collections;
using Loyc.Collections.Linq;
using Loyc.CompilerCore;
using Loyc.CompilerCore.ExprNodes;
using Loyc.CompilerCore.ExprParsing;
using Loyc.Essentials;
using Loyc.Utilities;
using NUnit.Framework;
using Tests.Resources;
using Loyc.Math;
using Loyc.Collections.Impl;

namespace Loyc.Tests
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Running tests on stable code...");
			RunTests.Run(new SimpleCacheTests());
			RunTests.Run(new GTests());
			RunTests.Run(new HashTagsTests());
			RunTests.Run(new StringCharSourceTests());
			RunTests.Run(new StreamCharSourceTests(Encoding.Unicode, 256));
			RunTests.Run(new StreamCharSourceTests(Encoding.Unicode, 16));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 256));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 16));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 27));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF32, 64));
			RunTests.Run(new ThreadExTests());
			RunTests.Run(new ExtraTagsInWListTests());
			RunTests.Run(new LocalizeTests());
			RunTests.Run(new CPTrieTests());
			RunTests.Run(new GoInterfaceTests());
			RunTests.Run(new ListTests<DList<int>>(true, delegate(int n) { var l = new DList<int>(); l.Resize(n); return l; }));
			RunTests.Run(new DequeTests<DList<int>>(delegate() { return new DList<int>(); }));
			RunTests.Run(new ListTests<InternalList<int>>(false, delegate(int n) { var l = InternalList<int>.Empty; l.Resize(n); return l; }));

			for(;;) {
				ConsoleKeyInfo k;
				string s;
				Console.WriteLine();
				Console.WriteLine("What do you want to do?");
				Console.WriteLine("1. Run unit tests that expect exceptions");
				Console.WriteLine("2. Run unit tests on unstable code");
				Console.WriteLine("3. Try out BooLexer");
				Console.WriteLine("4. Try out BasicOneParser with standard operator set (not done)");
				Console.WriteLine("5. Parse LAIF");
				Console.WriteLine("9. Benchmarks");
				Console.WriteLine("Z. List encodings");
				Console.WriteLine("Press ESC or ENTER to Quit");
				Console.WriteLine((k = Console.ReadKey(true)).KeyChar);
				if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter)
					break;
				else if (k.KeyChar == '1') {
					RunTests.Run(new SymbolTests());
					RunTests.Run(new RWListTests()); 
					RunTests.Run(new WListTests());
					RunTests.Run(new RVListTests());
					RunTests.Run(new VListTests());
					RunTests.Run(new ParseTokenTests());
				} else if (k.KeyChar == '2') {
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), false));
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), true));
					RunTests.Run(new BooLexerCoreTest());
					RunTests.Run(new BooLexerTest());
					RunTests.Run(new EssentialTreeParserTests());
					RunTests.Run(new LaifParserTests());
					RunTests.Run(new ListTests<AList<int>>(true, delegate(int n) { var l = new AList<int>(); l.Resize(n); return l; }, 504864148));
				}
				else if (k.KeyChar == '3')
				{
					BooLanguage lang = new BooLanguage();
					Console.WriteLine("Boo Lexer: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						Lexer(lang, s);
				} else if (k.KeyChar == '4') {
					BooLanguage lang = new BooLanguage();
					Console.WriteLine("BasicOneParser: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						OneParserDemo(lang, s);
				} else if (k.KeyChar == '5') {
					Console.WriteLine("LaifParser: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						ParseLaif(s);
				} else if (k.KeyChar == '9') {
					Benchmarks();
				} else if (k.KeyChar == 'z' || k.KeyChar == 'Z') {
					foreach (EncodingInfo inf in Encoding.GetEncodings())
						Console.WriteLine("{0} {1}: {2}", inf.CodePage, inf.Name, inf.DisplayName);
				}
			}
		}

		static void Lexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(s, "Boo");
			BooLexer lexer = new BooLexer(input, lang.StandardKeywords, false);

			foreach (Loyc.CompilerCore.AstNode t in lexer) {
				System.Console.WriteLine("{0} <{1}>", t.NodeType, t.SourceText);
			}
		}
		static void TreeLexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(s, lang.LanguageName);
			IEnumerable<AstNode> lexer = new BooLexer(input, lang.StandardKeywords, false);
			EssentialTreeParser etp = new EssentialTreeParser();
			AstNode root = AstNode.New(SourceRange.Nowhere, GSymbol.Empty);
			etp.Parse(ref root, lexer); // May print errors
		}

		private static void OneParserDemo(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(s, lang.LanguageName);
			IEnumerable<AstNode> lexer = new BooLexer(input, lang.StandardKeywords, true);
			IEnumerable<AstNode> lexFilter = new VisibleTokenFilter<AstNode>(lexer);
			List<AstNode> tokens = lexFilter.ToList();
			EnumerableSource<AstNode> source = new EnumerableSource<AstNode>(tokens);
			int pos = 0;
			IOneParser<AstNode> parser = new BasicOneParser<AstNode>(OneParserTests.TestOps);
			OneOperatorMatch<AstNode> expr = parser.Parse(source, ref pos, false);
			System.Console.WriteLine("Parsed as: " + OneParserTests.BuildResult(expr));
		}

		private static void ParseLaif(string input)
		{
			LaifParser p = new LaifParser();
			RVList<AstNode> output = p.Parse(
				new StringCharSourceFile(input, "Laif"), SourceRange.Nowhere.Source);
			
			Console.WriteLine("TODO");
		}

		private static void Benchmarks()
		{
			// Obtain the word list
			string wordList = Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" }, 
			                                StringSplitOptions.RemoveEmptyEntries);

			Benchmark.CountOnes();
			Benchmark.EnumeratorVsIterator();
			GoInterfaceBenchmark.DoBenchmark();
			CPTrieBenchmark.BenchmarkStrings(words);
			CPTrieBenchmark.BenchmarkInts();
			Benchmark.ByteArrayAccess();
			Benchmark.ThreadLocalStorage();
		}
	}
}
