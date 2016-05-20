using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Loyc.MiniTest;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.Tests;
using Loyc.Utilities;
using Loyc.Math;
using Loyc.Threading;
using Loyc.Syntax;
using Loyc.Geometry;
using Loyc.Syntax.Lexing;
using Loyc.Syntax.Les;
using Loyc.Syntax.Tests;

namespace Loyc.Tests
{
	public class RunCoreTests
	{
		public static readonly VList<Pair<string, Func<bool>>> Menu = new VList<Pair<string, Func<bool>>>()
		{
			new Pair<string,Func<bool>>("Run unit tests of Loyc.Essentials.dll",  Loyc_Essentials),
			new Pair<string,Func<bool>>("Run unit tests of Loyc.Collections.dll", Loyc_Collections),
			new Pair<string,Func<bool>>("Run unit tests of Loyc.Syntax.dll",	  Loyc_Syntax),
			new Pair<string,Func<bool>>("Run unit tests of Loyc.Utilities.dll",   Loyc_Utilities),
		};

		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );
            if (!RunMenu(Menu, args.Length > 0 ? args[0].GetEnumerator() : null))
				// Let the outside world know that something
				// went wrong by setting the exit code to
				// '1'. This is particularly useful for
				// automated tests (CI).
				Environment.ExitCode = 1;
		}

        private static IEnumerator<char> ConsoleChars()
        {
            for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Escape 
                && k.Key != ConsoleKey.Enter;)
                yield return k.KeyChar;
        }

        public static bool RunMenu(IList<Pair<string, Func<bool>>> menu, IEnumerator<char> input = null)
		{
            var reader = input ?? ConsoleChars();
			bool anyErrors = false;
			for (;;) {
				Console.WriteLine();
				Console.WriteLine("What do you want to do? (Esc to quit)");
				for (int i = 0; i < menu.Count; i++)
					Console.WriteLine(ParseHelpers.HexDigitChar(i+1) + ". " + menu[i].Key);
				Console.WriteLine("Space. Run all tests");

                if (!reader.MoveNext())
                    break;

                char c = reader.Current;
				if (c == ' ') {
					for (int i = 0; i < menu.Count; i++) {
						Console.WriteLine();
						ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i+1, menu[i].Key);
						anyErrors = !menu[i].Value() || anyErrors;
					}
				} else {
					int i = ParseHelpers.HexDigitValue(c);
					if (i > 0 && i <= menu.Count)
						anyErrors = !menu[i - 1].Value() || anyErrors;
				}
			}
			return !anyErrors;
		}

		public static bool Loyc_Essentials()
		{
			return MiniTest.RunTests.RunMany(
				new ListExtTests(),
				new MathExTests(),
				new UStringTests(),
				new StringExtTests(),
				new HashTagsTests(),
				new LocalizeTests(),
				new SymbolTests(),
				new ThreadExTests(),
				new ListTests<InternalList<int>>(false, delegate(int n) { var l = InternalList<int>.Empty; l.Resize(n); return l; }),
				new ListRangeTests<InternalList<int>>(false, delegate() { return InternalList<int>.Empty; }),
				new ListTests<DList<int>>(false, delegate(int n) { var l = new DList<int>(); l.Resize(n); return l; }),
				new DequeTests<DList<int>>(delegate() { return new DList<int>(); }),
				new ListRangeTests<DList<int>>(false, delegate() { return new DList<int>(); }),
				new GTests(),
				new ParseHelpersTests());
		}
		public static bool Loyc_Collections()
		{
			// Test with small node sizes as well as the standard node size,
			// including the minimum size of 3 (the most problematic size).
			int seed = 237588399;

			return MiniTest.RunTests.RunMany(
				new CPTrieTests(),
				new SimpleCacheTests(),
				new InvertibleSetTests(),
				new AListTests(false, seed, 8, 8),
				new BListTests(false, seed, 3, 3),
				new BDictionaryTests(false, seed, 6, 6),
				new SparseAListTests(false, seed, 10, 10),
				new DequeTests<AList<int>>(delegate() { return new AList<int>(); }),
				new DequeTests<SparseAList<int>>(delegate() { return new SparseAList<int>(); }),
				new DictionaryTests<BDictionary<object, object>>(true, true),
				new ListTests<AList<int>>(false, delegate(int n) { var l = new AList<int>(); l.Resize(n); return l; }),
				new ListRangeTests<AList<int>>(false, delegate() { return new AList<int>(); }, 12345),
				new ListTests<SparseAList<int>>(false, delegate(int n) { var l = new SparseAList<int>(); l.Resize(n); return l; }, 12345),
				new ListRangeTests<SparseAList<int>>(false, delegate() { return new SparseAList<int>(); }, 12345),
				new WListTests(),
				new FWListTests(),
				new VListTests(),
				new FVListTests(),
				new MapTests(),
				new SparseAListTests(true, seed, 8, 4),
				new SparseAListTests(),
				new AListTests(),
				new BListTests(),
				new BDictionaryTests(),
				new MSetTests(), // derived from MutableSetTests<MSet<STI>, STI>
				new SymbolSetTests(), // derived from MutableSetTests<MSet<Symbol>, Symbol>
				new ImmSetTests(), // tests for Set<T>
				new MapTests(), // derived from DictionaryTests<MMap<object, object>>
				new KeylessHashtableTests()
			);
		}
		public static bool Loyc_Syntax()
		{
			return MiniTest.RunTests.RunMany(
				new TokenTests(),
				new LesLexerTests(),
				new LesParserTests(),
				new LesPrinterTests(),
				new TokensToTreeTests(),
				new StreamCharSourceTests(),
				new LexerSourceTests_Calculator(),
				new ParserSourceTests_Calculator(),
				new IndentTokenGeneratorTests());
		}
		public static bool Loyc_Utilities()
		{
			return MiniTest.RunTests.RunMany(
				new LineMathTests(),
				new PointMathTests(),
				new Loyc.LLParserGenerator.IntSetTests(),
				new TagsInWListTests(),
				new UGTests(),
				new GoInterfaceTests());
		}
	}
}
