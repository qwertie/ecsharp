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

namespace Loyc.Tests
{
	class Program
	{
		public static PoorMansLinq<T> Linq<T>(IEnumerable<T> source)
		{
			return new PoorMansLinq<T>(source);
		}

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

			

			Deque<int> list = new Deque<int>(Enumerable.Range(-5, 1000));
			var odds = Iterable.CountForever(3, 2);
			var primes = from p in list
						 where p >= 2 && (p == 2 || (p & 1) == 1)
						 where !odds.TakeWhile(n => n * n <= p).Any(n => p % n == 0)
						 select p;
			Console.WriteLine(string.Join(", ", primes.Select(p => p.ToString()).ToArray()));
			for(;;) {

                //var d1 = new Deque<Uri>(new[] { new Uri("http://1"), new Uri("http://2"), new Uri("http://3") });
				//var d2 = new Deque<string>(new [] { "1","2","3" });
				//var both = d1.Concat<object>(d2);


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
			List<AstNode> tokens = Linq(lexFilter).ToList();
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

        public static IEnumerator<int> Counter1(int limit)
        {
            for (int i = 0; i < limit; i++)
                yield return i;
        }
        public static Iterator<int> Counter2(int limit)
        {
            int i = -1;
            return (ref bool ended) =>
            {
                if (++i < limit)
                    return i;
                ended = true;
                return 0;
            };
        }
        static void EnumeratorVsIterator()
		{
			SimpleTimer t = new SimpleTimer();
			int total1 = 0, total2 = 0, total2b = 0;
            const int Limit = 333333333;

			for (int i = 0; i < 3; i++)
			{
				Console.Write("IEnumerator<int>...  ");
				t.Restart();
                var b1 = Counter1(Limit);
				int current;
				while (b1.MoveNext())
					current = b1.Current;
				total1 += t.Millisec;
				Console.WriteLine("{0} seconds", t.Millisec * 0.001);

				Console.Write("Iterator<int>...     ");
				t.Restart();
                var b2 = Counter2(Limit);
				bool ended = false;
				do
					current = b2(ref ended);
				while (!ended);
				total2 += t.Millisec;
				Console.WriteLine("{0} seconds", t.Millisec * 0.001);

				Console.Write("Iterator.MoveNext... ");
				t.Restart();
				b2 = Counter2(Limit);
				while (b2.MoveNext(out current)) { }
				total2b += t.Millisec;
				Console.WriteLine("{0} seconds", t.Millisec * 0.001);
			}
			Console.WriteLine("On average, IEnumerator consumes {0:0.0}% as much time as Iterator.", total1 * 100.0 / total2);
			Console.WriteLine("However, Iterator.MoveNext needs {0:0.0}% as much time as Iterator used directly.", total2b * 100.0 / total2);
			Console.WriteLine();
		}

		private static void Benchmarks()
		{
			EnumeratorVsIterator();

			// Obtain the word list
			string wordList = Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" }, 
			                                StringSplitOptions.RemoveEmptyEntries);

			GoInterfaceBenchmark.DoBenchmark();
			CPTrieBenchmark.BenchmarkStrings(words);
			CPTrieBenchmark.BenchmarkInts();
			Benchmark.CountOnes();
			Benchmark.ByteArrayAccess();
			Benchmark.ThreadLocalStorage();
		}
	}
}
