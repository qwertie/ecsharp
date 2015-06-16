using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc;

namespace Benchmark
{
	class Program
	{
		public static void Main()
		{
			RunBenchmarks();
		}
		private static void RunBenchmarks()
		{
			new ListBenchmarks().Run(EzChartForm.StartOnNewThread(true));

			Benchmarks.ConvexHull();

			// Obtain the word list
			string wordList = Benchmark.Resources.Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" },
											StringSplitOptions.RemoveEmptyEntries);

			Benchmarks.LinqVsForLoop();
			//Benchmarks.CountOnes();
			Benchmarks.BenchmarkSets(words);
			Benchmarks.ThreadLocalStorage();
			Benchmarks.EnumeratorVsIterator();
			GoInterfaceBenchmark.DoBenchmark();
			CPTrieBenchmark.BenchmarkStrings(words);
			CPTrieBenchmark.BenchmarkInts();
			Benchmarks.ByteArrayAccess();
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
