using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc;
using Loyc.Syntax;

namespace Benchmark
{
	class Program
	{
        public static void Main()
		{
			// Obtain the word list
			string wordList = Benchmark.Resources.Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" },
											StringSplitOptions.RemoveEmptyEntries);

			RunMenu(new Pair<string, Action>[] {
				new Pair<string,Action>("Convex Hull", Benchmarks.ConvexHull),
				new Pair<string,Action>("Run unit tests of LeMP", Benchmarks.LinqVsForLoop),
				new Pair<string,Action>("Hashtrees (InternalSet) vs HashSet/Dictionary", () => Benchmarks.BenchmarkSets(words)),
				new Pair<string,Action>("Thread-local storage", Benchmarks.ThreadLocalStorage),
				new Pair<string,Action>("IEnumerator<T> vs Iterator<T>", Benchmarks.EnumeratorVsIterator),
				new Pair<string,Action>("GoInterface", GoInterfaceBenchmark.DoBenchmark),
				new Pair<string,Action>("CPTrie (strings)", () => CPTrieBenchmark.BenchmarkStrings(words)),
				new Pair<string,Action>("CPTrie (integers)", CPTrieBenchmark.BenchmarkInts),
				new Pair<string,Action>("Byte array access", Benchmarks.ByteArrayAccess),
				new Pair<string,Action>("List benchmarks (with chart Form)", 
					() => new ListBenchmarks().Run(EzChartForm.StartOnNewThread(true))),
			});
		}

		private static IEnumerator<char> ConsoleChars()
		{
			for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Escape
				&& k.Key != ConsoleKey.Enter;)
				yield return k.KeyChar;
		}

		public static void RunMenu(IList<Pair<string, Action>> menu, IEnumerator<char> input = null)
		{
			var reader = input ?? ConsoleChars();
			for (;;) {
				Console.WriteLine();
				Console.WriteLine("What do you want to run? (Esc to quit)");
				for (int i = 0; i < menu.Count; i++)
					Console.WriteLine(PrintHelpers.HexDigitChar(i+1) + ". " + menu[i].Key);
				Console.WriteLine("Space. Run all");

				if (!reader.MoveNext())
					break;

				char c = reader.Current;
				if (c == ' ') {
					for (int i = 0; i < menu.Count; i++)
						RunOne(menu, i);
				} else {
					int i = ParseHelpers.HexDigitValue(c);
					if (i > 0 && i <= menu.Count)
						RunOne(menu, i - 1);
				}
			}
		}

		private static void RunOne(IList<Pair<string, Action>> menu, int i)
		{
			Console.WriteLine();
			ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i + 1, menu[i].Key);
			menu[i].Value();
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
