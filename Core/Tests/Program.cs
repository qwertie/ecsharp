using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Loyc.Essentials.Tests;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Collections.Tests;
using Loyc.Utilities.Tests;
using Loyc.Math;
using Loyc.Syntax;
using Loyc.Geometry;
using Loyc.Syntax.Lexing;
using Loyc.Syntax.Les;
using Loyc.Syntax.Tests;

namespace Loyc.Tests
{
	public class RunCoreTests
	{
		static int _seed = Environment.TickCount;

		public static readonly VList<Pair<string, Func<int>>> Menu = new VList<Pair<string, Func<int>>>()
		{
			new Pair<string,Func<int>>("Run unit tests of Loyc.Essentials.dll",  Loyc_Essentials),
			new Pair<string,Func<int>>("Run unit tests of Loyc.Math.dll",        Loyc_Math),
			new Pair<string,Func<int>>("Run unit tests of Loyc.Collections.dll", Loyc_Collections),
			new Pair<string,Func<int>>("Run unit tests of Loyc.Syntax.dll",      Loyc_Syntax),
			new Pair<string,Func<int>>("Run unit tests of Loyc.Utilities.dll",   Loyc_Utilities),
		};

		public static void Main(string[] args)
		{
			if (RunMenu(Menu, args.Length > 0 ? args[0].GetEnumerator() : null) > 0)
				// Let the outside world know that something went wrong (e.g. Travis CI)
				Environment.ExitCode = 1;
		}

		private static IEnumerator<char> ConsoleChars()
		{
			for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Escape
				&& k.Key != ConsoleKey.Enter;)
				yield return k.KeyChar;
		}

		public static int RunMenu(IList<Pair<string, Func<int>>> menu, IEnumerator<char>? input = null)
		{
			var reader = input ?? ConsoleChars();
			int errorCount = 0;
			for (;;) {
				Console.WriteLine();
				Console.WriteLine("What do you want to do? (Esc to quit)");
				for (int i = 0; i < menu.Count; i++)
					Console.WriteLine(PrintHelpers.HexDigitChar(i+1) + ". " + menu[i].Key);
				Console.WriteLine("Space. Run all tests");

				if (!reader.MoveNext())
					break;

				char c = reader.Current;
				if (c == ' ') {
					errorCount = 0;
					for (int i = 0; i < menu.Count; i++) {
						Console.WriteLine();
						ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i+1, menu[i].Key);
						errorCount += menu[i].Value();
					}
					if (errorCount == 0)
						ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, null,
							"All test(s) passed (except known failures, if any).");
					else
						ConsoleMessageSink.WriteColoredMessage(ConsoleColor.Red, null,
							"{0} test(s) unexpectedly failed in total.".Localized(errorCount));
				} else {
					int i = ParseHelpers.HexDigitValue(c);
					if (i > 0 && i <= menu.Count)
						errorCount += menu[i - 1].Value();
				}
			}
			return errorCount;
		}

		public static int Loyc_Essentials()
		{
			return MiniTest.RunTests.RunMany(
				new SyncJsonWriterTests(),
				new InternalListScannerTests(_seed),
				new ScannableEnumerableTests(_seed),
				new ScannableEnumerableTests(_seed, 200_000),
				new BufferedSequenceScannerTests(_seed),
				new MultiMapTests(_seed),
				new EitherTests(),
				new BaisTests(),
				new ListExtTests(),
				new UStringTests(),
				new StringExtTests(),
				new HashTagsTests(),
				new LocalizeTests(),
				new SymbolTests(),
				new ThreadExTests(),
				new CollectionWithChangeEventsTests(),
				new ListWrapperTests(_seed),
				new ListWithChangeEventsTests(_seed),
				new DictionaryWrapperTests(),
				new DictionaryWithChangeEventsTests(),
				new DictionaryWithChangeEventsTests2(),
				new ListTests<InternalList<int>>(false, delegate(int n) { var l = InternalList<int>.Empty; l.Resize(n); return l; }),
				new ListRangeTests<InternalList<int>>(false, delegate() { return InternalList<int>.Empty; }),
				new ListTests<DList<int>>(true, delegate(int n) { var l = new DList<int>(); l.Resize(n); return l; }),
				new DequeTests<DList<int>>(delegate() { return new DList<int>(); }),
				new ListRangeTests<DList<int>>(false, delegate() { return new DList<int>(); }),
				SelectDictionaryFromKeysTests.TestObjects[0],
				SelectDictionaryFromKeysTests.TestObjects[1],
				new GTests(),
				new PrintHelpersTests());
		}
		public static int Loyc_Collections()
		{
			return MiniTest.RunTests.RunMany(
				new InternalDArrayTests(_seed),
				new TypeDictionaryWithBaseTypeLookupsTests(),
				new ListSourceSliceTests(),
				new ListSliceTests(),
				new ROLSliceTests(),
				new HeapTests(_seed),
				new SimpleCacheTests(),
				new InvertibleSetTests(),
				// Test with small node sizes as well as the standard node size,
				// including the minimum size of 3 (the most problematic size).
				new AListTests(false, _seed, 8, 8),
				new AListTests(false, _seed, 3, 3),
				new BListTests(false, _seed, 8, 8),
				new BListTests(false, _seed, 3, 3),
				new BDictionaryTests(false, _seed, 6, 6),
				new SparseAListTests(false, _seed, 3, 3),
				new SparseAListTests(false, _seed, 9, 9),
				new BMultiMapTests(4),
				new DequeTests<DList<int>>(delegate() { return new DList<int>(); }),
				new DequeTests<AList<int>>(delegate() { return new AList<int>(); }),
				new DequeTests<SparseAList<int>>(delegate() { return new SparseAList<int>(); }),
				new DictionaryTests<BDictionary<object, object?>>(true, true),
				new ListTests<AList<int>>(false, delegate(int n) { var l = new AList<int>(); l.Resize(n); return l; }),
				new ListRangeTests<AList<int>>(false, delegate() { return new AList<int>(); }, 12345),
				new ListTests<SparseAList<int>>(false, delegate(int n) { var l = new SparseAList<int>(); l.Resize(n); return l; }, 12345),
				new ListRangeTests<SparseAList<int>>(false, delegate() { return new SparseAList<int>(); }, 12345),
				new WListTests(),
				new FWListTests(),
				new VListTests(),
				new FVListTests(),
				new MapTests(),
				new SparseAListTests(true, _seed, 8, 4),
				new SparseAListTests(),
				new AListTests(),
				new BListTests(),
				new BDictionaryTests(),
				new MSetTests(), // derived from MutableSetTests<MSet<STI>, STI>
				new SymbolSetTests(), // derived from MutableSetTests<MSet<Symbol>, Symbol>
				new ImmSetTests(), // tests for Set<T>
				new MapTests() // derived from DictionaryTests<MMap<object, object>>
			);
		}
		public static int Loyc_Math()
		{
			return MiniTest.RunTests.RunMany(
				new MathExTests(),
				new LineMathTests(),
				new PointMathTests());
		}
		public static int Loyc_Syntax()
		{
			return MiniTest.RunTests.RunMany(
				new TokenTests(),
				new IndexPositionMapperTests(),
				new LNodeRangeMapperTests(),
				new LNodePrinterHelperTests(),
				new IndentTokenGeneratorTests(),
				new StandardTriviaInjectorTests(),
				new LiteralHandlerTableTests(),
				new StandardLiteralHandlersTests(),
				new Les2LexerTests(),
				new Les2ParserTests(),
				new Les2PrinterTests(),
				new Les3LexerTests(),
				new Les3ParserTests(),
				new Les3PrinterTests(),
				new LNodeTests(),
				new MiscTests(),
				new TokensToTreeTests(),
				new StreamCharSourceTests(),
				new ParseHelpersTests(),
				new LexerSourceTests_Calculator(),
				new ParserSourceTests_Calculator());
		}
		public static int Loyc_Utilities()
		{
			return MiniTest.RunTests.RunMany(
				new AListSummaryTrackerTests(_seed),
				new BDictionarySummaryTrackerTests(_seed),
				new Loyc.LLParserGenerator.IntSetTests(),
				new TagsInWListTests(),
				new UGTests(),
				new GoInterfaceTests(),
				new CPTrieTests(),
				new KeylessHashtableTests());
		}
	}
}
