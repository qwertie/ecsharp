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
	class Program
	{
		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );

			Console.WriteLine("Running tests on stable code...");
			// Omitted: unit tests that throw & catch exceptions (those are below)

			// Loyc.Essentials
			RunTests.Run(new ListExtTests());
			RunTests.Run(new MathExTests());
			RunTests.Run(new UStringTests());
			RunTests.Run(new StringExtTests());
			RunTests.Run(new HashTagsTests());
			RunTests.Run(new LocalizeTests());
			RunTests.Run(new SymbolTests());
			RunTests.Run(new ThreadExTests());
			RunTests.Run(new ListTests<InternalList<int>>(false, delegate(int n) { var l = InternalList<int>.Empty; l.Resize(n); return l; }));
			RunTests.Run(new ListRangeTests<InternalList<int>>(false, delegate() { return InternalList<int>.Empty; }));
			RunTests.Run(new ListTests<DList<int>>(false, delegate(int n) { var l = new DList<int>(); l.Resize(n); return l; }));
			RunTests.Run(new DequeTests<DList<int>>(delegate() { return new DList<int>(); }));
			RunTests.Run(new ListRangeTests<DList<int>>(false, delegate() { return new DList<int>(); }));

			// Loyc.Collections
			RunTests.Run(new CPTrieTests());
			RunTests.Run(new SimpleCacheTests());
			RunTests.Run(new InvertibleSetTests());
			// Test with small node sizes as well as the standard node size,
			// including the minimum size of 3 (the most problematic size).
			int seed = 237588399;
			RunTests.Run(new AListTests(false, seed, 8, 8));
			RunTests.Run(new BListTests(false, seed, 3, 3));
			RunTests.Run(new BDictionaryTests(false, seed, 6, 6));
			RunTests.Run(new SparseAListTests(false, seed, 10, 10));
			RunTests.Run(new DequeTests<AList<int>>(delegate() { return new AList<int>(); }));
			RunTests.Run(new DequeTests<SparseAList<int>>(delegate() { return new SparseAList<int>(); }));
			RunTests.Run(new DictionaryTests<BDictionary<object, object>>(true, true));
			RunTests.Run(new ListTests<AList<int>>(false, delegate(int n) { var l = new AList<int>(); l.Resize(n); return l; }));
			RunTests.Run(new ListRangeTests<AList<int>>(false, delegate() { return new AList<int>(); }, 12345));
			RunTests.Run(new ListTests<SparseAList<int>>(false, delegate(int n) { var l = new SparseAList<int>(); l.Resize(n); return l; }, 12345));
			RunTests.Run(new ListRangeTests<SparseAList<int>>(false, delegate() { return new SparseAList<int>(); }, 12345));

			// Loyc.Syntax
			RunTests.Run(new TokenTests());
			RunTests.Run(new LesLexerTests());
			RunTests.Run(new LesParserTests());
			RunTests.Run(new LesPrinterTests());
			RunTests.Run(new TokensToTreeTests());
			RunTests.Run(new StreamCharSourceTests());
			RunTests.Run(new LexerSourceTests_Calculator());
			RunTests.Run(new ParserSourceTests_Calculator());
			RunTests.Run(new IndentTokenGeneratorTests());

			// Loyc.Utilities
			RunTests.Run(new LineMathTests());
			RunTests.Run(new PointMathTests());
			RunTests.Run(new Loyc.LLParserGenerator.IntSetTests());
			RunTests.Run(new TagsInWListTests());
			RunTests.Run(new UGTests());

			for(;;) {
				ConsoleKeyInfo k;

				Console.WriteLine();
				Console.WriteLine("What do you want to do?");
				Console.WriteLine("1. Run unit tests that expect exceptions");
				Console.WriteLine("2. Run unit tests on unstable code");
				Console.WriteLine("9. Benchmarks");
				Console.WriteLine("Z. List encodings");
				Console.WriteLine("Press ESC or ENTER to Quit");
				Console.WriteLine((k = Console.ReadKey(true)).KeyChar);
				if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter)
					break;
				else if (k.KeyChar == '1') {
					// Loyc.Essentials
					RunTests.Run(new GTests());
					
					// Loyc.Utilities
					RunTests.Run(new GoInterfaceTests());
					
					// Loyc.Collections
					RunTests.Run(new RWListTests()); 
					RunTests.Run(new WListTests());
					RunTests.Run(new RVListTests());
					RunTests.Run(new VListTests());
					RunTests.Run(new MapTests());
					RunTests.Run(new SparseAListTests(true, seed, 8, 4));
					RunTests.Run(new SparseAListTests());
					RunTests.Run(new AListTests());
					RunTests.Run(new BListTests());
					RunTests.Run(new BDictionaryTests());
					RunTests.Run(new MSetTests()); // derived from MutableSetTests<MSet<STI>, STI>
					RunTests.Run(new SymbolSetTests()); // derived from MutableSetTests<MSet<Symbol>, Symbol>
					RunTests.Run(new ImmSetTests()); // tests for Set<T>
					RunTests.Run(new MapTests()); // derived from DictionaryTests<MMap<object, object>>
				} else if (k.KeyChar == '2') {
					RunTests.Run(new KeylessHashtableTests());
				} else if (k.KeyChar == 'z' || k.KeyChar == 'Z') {
					foreach (EncodingInfo inf in Encoding.GetEncodings())
						Console.WriteLine("{0} {1}: {2}", inf.CodePage, inf.Name, inf.DisplayName);
				}
			}
		}
	}
}
