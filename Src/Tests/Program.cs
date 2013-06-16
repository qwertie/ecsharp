using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Loyc.Collections;
using Loyc.Essentials;
using Loyc.Utilities;
using NUnit.Framework;
using Tests.Resources;
using Loyc.Math;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Syntax;

namespace Loyc.Tests
{
	class Program
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Running tests on stable code...");
			RunTests.Run(new SimpleCacheTests());
			RunTests.Run(new HashTagsTests());
			RunTests.Run(new StringCharSourceTests());
			RunTests.Run(new ThreadExTests());
			RunTests.Run(new ExtraTagsInWListTests());
			RunTests.Run(new LocalizeTests());
			RunTests.Run(new CPTrieTests());
			RunTests.Run(new ListTests<InternalList<int>>(false, delegate(int n) { var l = InternalList<int>.Empty; l.Resize(n); return l; }));
			RunTests.Run(new ListRangeTests<InternalList<int>>(false, delegate() { return InternalList<int>.Empty; }));
			RunTests.Run(new ListTests<DList<int>>(false, delegate(int n) { var l = new DList<int>(); l.Resize(n); return l; }));
			RunTests.Run(new DequeTests<DList<int>>(delegate() { return new DList<int>(); }));
			RunTests.Run(new ListRangeTests<DList<int>>(false, delegate() { return new DList<int>(); }));
			RunTests.Run(new ListTests<AList<int>>(false, delegate(int n) { var l = new AList<int>(); l.Resize(n); return l; }));
			RunTests.Run(new ListRangeTests<AList<int>>(false, delegate() { return new AList<int>(); }, 12345));
			RunTests.Run(new MSetTests());
			RunTests.Run(new ImmSetTests());
			RunTests.Run(new SymbolSetTests());
			RunTests.Run(new RWListTests()); 
			RunTests.Run(new WListTests());
			RunTests.Run(new RVListTests());
			RunTests.Run(new VListTests());

			//LogTest3.Main(args);

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
					RunTests.Run(new MapTests());
					RunTests.Run(new GTests());
					RunTests.Run(new GoInterfaceTests());
					// Test with small node sizes as well as the standard node size,
					// including the minimum size of 3 (the most problematic size).
					RunTests.Run(new AListTests());
					RunTests.Run(new BListTests());
					RunTests.Run(new AListTests(false, 0, 8, 8));
					RunTests.Run(new BListTests(false, 0, 3, 3));
					RunTests.Run(new BDictionaryTests(false, 0, 6, 6));
					RunTests.Run(new BDictionaryTests());

					RunTests.Run(new SymbolTests());
				} else if (k.KeyChar == '2') {
					RunTests.Run(new Loyc.Syntax.Les.TokensToTreeTests());
					RunTests.Run(new KeylessHashtableTests());
				} else if (k.KeyChar == '9') {
					Benchmarks();
				} else if (k.KeyChar == 'z' || k.KeyChar == 'Z') {
					foreach (EncodingInfo inf in Encoding.GetEncodings())
						Console.WriteLine("{0} {1}: {2}", inf.CodePage, inf.Name, inf.DisplayName);
				}
			}
		}

		private static void Benchmarks()
		{
			// Obtain the word list
			string wordList = Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" }, 
			                                StringSplitOptions.RemoveEmptyEntries);

			Benchmark.LinqVsForLoop();
			//Benchmark.CountOnes();
			Benchmark.BenchmarkSets(words);
			Benchmark.ThreadLocalStorage();
			Benchmark.EnumeratorVsIterator();
			GoInterfaceBenchmark.DoBenchmark();
			CPTrieBenchmark.BenchmarkStrings(words);
			CPTrieBenchmark.BenchmarkInts();
			Benchmark.ByteArrayAccess();
		}

		// By examining disassembly of this method in the debugger, I learned that 
		// the .NET inliner (x64) is too dumb to take into account the cost of 
		// calling a method in its inlining decision. .NET will not inline the last 
		// three methods, even though I only add a single additional instruction as 
		// I add a single additional parameter. So it seems the inlining decision is 
		// based only on the cost of the method body; .NET ignores the cost *savings*
		// of not having to shuffle registers or stack space around when it inlines 
		// a method.
		//
		//private static long InliningTest(long a, long b, long c, long d, long e, long f, long g, long h)
		//{
		//    long total = 0;
		//    total += Foo(a, b, c);
		//    total += Foo(c, d, e, f);
		//    total += Foo(d, e, f, g, h);
		//    total += Foo(f, g, h, a, b, c);
		//    total += Foo(h, a, b, c, d, e, f);
		//    total += Foo(a, b, c, d, e, f, g, h);
		//    total += Foo(a, c, e, g, b, d, f);
		//    total += Foo(a, b, c, d, e, f);
		//    total += Foo(a, b, c, d);
		//    total += Foo(a, b, c);
		//    return total;
		//}
		//private static long Add(long a, long b)
		//{
		//    return a + b;
		//}
		//private static long Foo(long a, long b, long c)
		//{
		//    return a * b * c + a + b + c;
		//}
		//private static long Foo(long a, long b, long c, long d)
		//{
		//    return a * b * c * d + a + b + c;
		//}
		//private static long Foo(long a, long b, long c, long d, long e)
		//{
		//    return a * b * c * d * e + a + b + c + d + e;
		//}
		//private static long Foo(long a, long b, long c, long d, long e, long f)
		//{
		//    return a * b * c * d * e * f + a + b + c + d;
		//}
		//private static long Foo(long a, long b, long c, long d, long e, long f, long g)
		//{
		//    return a * b * c * d * e * f * g + a + b + c + d;
		//}
		//private static long Foo(long a, long b, long c, long d, long e, long f, long g, long h)
		//{
		//    return a * b * c * d * e * f * g * h + a + b + c + d;
		//}
	}
}
