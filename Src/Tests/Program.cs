using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Loyc.BooStyle;
using Loyc.Runtime;
using Loyc.Utilities;
using Loyc.CompilerCore;
using Loyc.CompilerCore.ExprParsing;
using Loyc.CompilerCore.ExprNodes;
using System.Threading;

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
			RunTests.Run(new BasicOperatorDividerTests());
			RunTests.Run(new ExtraTagsInWListTests());
			RunTests.Run(new LocalizeTests());

			for(;;) {
				ConsoleKeyInfo k;
				string s;
				Console.WriteLine();
				Console.WriteLine("What do you want to do?");
				Console.WriteLine("1. Run unit tests that expect exceptions");
				Console.WriteLine("2. Run unit tests on unstable code");
				Console.WriteLine("3. Try out BooLexer");
				Console.WriteLine("4. Try out BasicOneParser with standard operator set (not done)");
				Console.WriteLine("4. Benchmarks");
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
				} else if (k.KeyChar == '2') {
					RunTests.Run(new BooLexerCoreTest());
					RunTests.Run(new BooLexerTest());
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), false));
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), true));
					RunTests.Run(new EssentialTreeParserTests());
				}
				else if (k.KeyChar == '3')
				{
					var lang = new BooLanguage();
					Console.WriteLine("Boo Lexer: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						Lexer(lang, s);
				} else if (k.KeyChar == '4') {
					var lang = new BooLanguage();
					Console.WriteLine("BasicOneParser: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						OneParserDemo(lang, s);
				} else if (k.KeyChar == '5') {
					Benchmarks();
				} else if (k.KeyChar == 'z' || k.KeyChar == 'Z') {
					foreach (EncodingInfo inf in Encoding.GetEncodings())
						Console.WriteLine("{0} {1}: {2}", inf.CodePage, inf.Name, inf.DisplayName);
				} 
			}
		}

		private static void Benchmarks()
		{
			Benchmark.ThreadLocalStorage();
		}

		/*static void ParseBug(string s)
		{
			System.Console.WriteLine(s);
			ANTLRStringStream input = new ANTLRStringStream(s);
			Lexer lexerBug = new Bug1Lexer(input);
			AstNode t;
			while ((t = lexerBug.NextToken()).NodeType != BooLexer.EOF) {
				System.Console.WriteLine("{0} <{1}>",
					BooTreeParser.tokenNames[t.NodeType], t.Text);
			}
			System.Console.WriteLine("");
		}*/
		static void Lexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(lang, s);
			BooLexer lexer = new BooLexer(input, lang.StandardKeywords, false);

			foreach (Loyc.CompilerCore.AstNode t in lexer) {
				System.Console.WriteLine("{0} <{1}>", t.NodeType, t.Value.ToString());
			}
		}
		static void TreeLexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(lang, s);
			IEnumerable<AstNode> lexer = new BooLexer(input, lang.StandardKeywords, false);
			EssentialTreeParser etp = new EssentialTreeParser();
			AstNode root = AstNode.New(SourceRange.Nowhere, Symbol.Empty);
			etp.Parse(ref root, lexer); // May print errors
		}

		private static void OneParserDemo(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(lang, s);
			IEnumerable<AstNode> lexer = new BooLexer(input, lang.StandardKeywords, true);
			IEnumerable<AstNode> lexFilter = new VisibleTokenFilter<AstNode>(lexer);
			List<AstNode> tokens = Linq(lexFilter).ToList();
			EnumerableSource<AstNode> source = new EnumerableSource<AstNode>(tokens);
			int pos = 0;
			IOneParser<AstNode> parser = new BasicOneParser<AstNode>(OneParserTests.TestOps);
			OneOperatorMatch<AstNode> expr = parser.Parse(source, ref pos, false, null);
			System.Console.WriteLine("Parsed as: " + OneParserTests.BuildResult(expr));
		}
	}

	class Benchmark
	{
		[ThreadStatic]
		static int _threadStatic;
		static LocalDataStoreSlot _tlSlot;
		static Dictionary<int, int> _dictById = new Dictionary<int,int>();
		static int _globalVariable = 0;

		public static void ThreadLocalStorage()
		{
			SimpleTimer t = new SimpleTimer();
			
			// Baseline comparison
			for (int i = 0; i < 10000000; i++)
				_globalVariable += i;
			int time0 = t.Restart();

			// ThreadStatic attribute
			t = new SimpleTimer();
			for (int i = 0; i < 10000000; i++)
				_threadStatic += i;
			int time1 = t.Restart();

			// Dictionary indexed by thread ID
			_dictById[Thread.CurrentThread.ManagedThreadId] = 0;
			for (int i = 0; i < 10000000; i++)
				_dictById[Thread.CurrentThread.ManagedThreadId] += i;
			int time2 = t.Restart();

			// Calling Thread.CurrentThread.ManagedThreadId
			for (int i = 0; i < 10000000; i++)
				_globalVariable += Thread.CurrentThread.ManagedThreadId;
			int time2a = t.Restart();

			// Thread Data Slot: slow, so extrapolate from 1/5 the work
			_tlSlot = Thread.AllocateDataSlot();
			Thread.SetData(_tlSlot, 0);
			t.Restart();
			for (int i = 0; i < 2000000; i++)
				Thread.SetData(_tlSlot, (int)Thread.GetData(_tlSlot) + i);
			int time3 = t.Restart() * 5;

			Console.WriteLine("Global: {0}ms, ThreadStatic: {1}ms, Dictionary: {2}ms ({3}ms getting ThreadId), Slot: ~{4}ms", 
				time0, time1, time2, time2a, time3);
		}
	}
}
