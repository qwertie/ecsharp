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
		public static readonly VList<Pair<string, Action>> Menu = new VList<Pair<string, Action>>()
		{
			new Pair<string,Action>("Run unit tests of Loyc.Essentials.dll",  Loyc_Essentials),
			new Pair<string,Action>("Run unit tests of Loyc.Collections.dll", Loyc_Collections),
			new Pair<string,Action>("Run unit tests of Loyc.Syntax.dll",      Loyc_Syntax),
			new Pair<string,Action>("Run unit tests of Loyc.Utilities.dll",   Loyc_Utilities),
		};

		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );
			RunMenu(Menu);
		}

		public static void RunMenu(IList<Pair<string, Action>> menu)
		{
			for (;;) {
				Console.WriteLine();
				Console.WriteLine("What do you want to do? (Esc to quit)");
				for (int i = 0; i < menu.Count; i++)
					Console.WriteLine(ParseHelpers.HexDigitChar(i+1) + ". " + menu[i].Key);
				Console.WriteLine("Space. Run all tests");

				ConsoleKeyInfo k;
				Console.WriteLine((k = Console.ReadKey(true)).KeyChar);
				if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter)
					break;
				else if (k.KeyChar == ' ') {
					for (int i = 0; i < menu.Count; i++) {
						Console.WriteLine();
						ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i+1, menu[i].Key);
						menu[i].Value();
					}
				} else {
					int i = ParseHelpers.HexDigitValue(k.KeyChar);
					if (i > 0 && i <= menu.Count)
						menu[i - 1].Value();
				}
			}
		}

		public static void Loyc_Essentials()
		{
			MiniTest.RunTests.Run(new ListExtTests());
			MiniTest.RunTests.Run(new MathExTests());
			MiniTest.RunTests.Run(new UStringTests());
			MiniTest.RunTests.Run(new StringExtTests());
			MiniTest.RunTests.Run(new HashTagsTests());
			MiniTest.RunTests.Run(new LocalizeTests());
			MiniTest.RunTests.Run(new SymbolTests());
			MiniTest.RunTests.Run(new ThreadExTests());
			MiniTest.RunTests.Run(new ListTests<InternalList<int>>(false, delegate(int n) { var l = InternalList<int>.Empty; l.Resize(n); return l; }));
			MiniTest.RunTests.Run(new ListRangeTests<InternalList<int>>(false, delegate() { return InternalList<int>.Empty; }));
			MiniTest.RunTests.Run(new ListTests<DList<int>>(false, delegate(int n) { var l = new DList<int>(); l.Resize(n); return l; }));
			MiniTest.RunTests.Run(new DequeTests<DList<int>>(delegate() { return new DList<int>(); }));
			MiniTest.RunTests.Run(new ListRangeTests<DList<int>>(false, delegate() { return new DList<int>(); }));
			MiniTest.RunTests.Run(new GTests());
			MiniTest.RunTests.Run(new ParseHelpersTests());
		}
		public static void Loyc_Collections()
		{
			MiniTest.RunTests.Run(new CPTrieTests());
			MiniTest.RunTests.Run(new SimpleCacheTests());
			MiniTest.RunTests.Run(new InvertibleSetTests());
			// Test with small node sizes as well as the standard node size,
			// including the minimum size of 3 (the most problematic size).
			int seed = 237588399;
			MiniTest.RunTests.Run(new AListTests(false, seed, 8, 8));
			MiniTest.RunTests.Run(new BListTests(false, seed, 3, 3));
			MiniTest.RunTests.Run(new BDictionaryTests(false, seed, 6, 6));
			MiniTest.RunTests.Run(new SparseAListTests(false, seed, 10, 10));
			MiniTest.RunTests.Run(new DequeTests<AList<int>>(delegate() { return new AList<int>(); }));
			MiniTest.RunTests.Run(new DequeTests<SparseAList<int>>(delegate() { return new SparseAList<int>(); }));
			MiniTest.RunTests.Run(new DictionaryTests<BDictionary<object, object>>(true, true));
			MiniTest.RunTests.Run(new ListTests<AList<int>>(false, delegate(int n) { var l = new AList<int>(); l.Resize(n); return l; }));
			MiniTest.RunTests.Run(new ListRangeTests<AList<int>>(false, delegate() { return new AList<int>(); }, 12345));
			MiniTest.RunTests.Run(new ListTests<SparseAList<int>>(false, delegate(int n) { var l = new SparseAList<int>(); l.Resize(n); return l; }, 12345));
			MiniTest.RunTests.Run(new ListRangeTests<SparseAList<int>>(false, delegate() { return new SparseAList<int>(); }, 12345));
			MiniTest.RunTests.Run(new WListTests());
			MiniTest.RunTests.Run(new FWListTests());
			MiniTest.RunTests.Run(new VListTests());
			MiniTest.RunTests.Run(new FVListTests());
			MiniTest.RunTests.Run(new MapTests());
			MiniTest.RunTests.Run(new SparseAListTests(true, seed, 8, 4));
			MiniTest.RunTests.Run(new SparseAListTests());
			MiniTest.RunTests.Run(new AListTests());
			MiniTest.RunTests.Run(new BListTests());
			MiniTest.RunTests.Run(new BDictionaryTests());
			MiniTest.RunTests.Run(new MSetTests()); // derived from MutableSetTests<MSet<STI>, STI>
			MiniTest.RunTests.Run(new SymbolSetTests()); // derived from MutableSetTests<MSet<Symbol>, Symbol>
			MiniTest.RunTests.Run(new ImmSetTests()); // tests for Set<T>
			MiniTest.RunTests.Run(new MapTests()); // derived from DictionaryTests<MMap<object, object>>
			MiniTest.RunTests.Run(new KeylessHashtableTests());
		}
		public static void Loyc_Syntax()
		{
			MiniTest.RunTests.Run(new TokenTests());
			MiniTest.RunTests.Run(new LesLexerTests());
			MiniTest.RunTests.Run(new LesParserTests());
			MiniTest.RunTests.Run(new LesPrinterTests());
			MiniTest.RunTests.Run(new TokensToTreeTests());
			MiniTest.RunTests.Run(new StreamCharSourceTests());
			MiniTest.RunTests.Run(new LexerSourceTests_Calculator());
			MiniTest.RunTests.Run(new ParserSourceTests_Calculator());
			MiniTest.RunTests.Run(new IndentTokenGeneratorTests());
		}
		public static void Loyc_Utilities()
		{
			MiniTest.RunTests.Run(new LineMathTests());
			MiniTest.RunTests.Run(new PointMathTests());
			MiniTest.RunTests.Run(new Loyc.LLParserGenerator.IntSetTests());
			MiniTest.RunTests.Run(new TagsInWListTests());
			MiniTest.RunTests.Run(new UGTests());
			MiniTest.RunTests.Run(new GoInterfaceTests());
		}
	}
}
