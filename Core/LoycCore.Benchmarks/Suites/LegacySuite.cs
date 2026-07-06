using Loyc;             // GoInterfaceBenchmark
using Loyc.Collections;  // CPTrieBenchmark

namespace Benchmark
{
	/// <summary>Registers the benchmarks carried over from the old console/OxyPlot
	/// app. The collection benchmarks produce charts; the rest are console-style
	/// benchmarks whose output is captured into the result log.</summary>
	public static class LegacySuite
	{
		public static void Register(BenchmarkRegistry registry)
		{
			registry.Add("Loyc.Collections/List benchmarks (quick, sizes ≤ 30k)",
				ctx => ConsoleCapture.Run(ctx, () => new ListBenchmarks().Run(ctx, maxListSize: 30_000)),
				"AList, SparseAList, IndexedAList, DList, InternalList vs List, LinkedList and " +
				"friends: insert/remove/scan/memory-use benchmarks at several list sizes. " +
				"This variant stops at 30,000 items.");
			registry.Add("Loyc.Collections/List benchmarks (full, sizes ≤ 1M)",
				ctx => ConsoleCapture.Run(ctx, () => new ListBenchmarks().Run(ctx, maxListSize: 1_000_000)),
				"The complete list benchmark suite up to one million items — takes a few minutes.");
			registry.Add("Loyc.Collections/AList benchmarks only",
				ctx => ConsoleCapture.Run(ctx, () =>
					new ListBenchmarks { TestDLists = false, TestOther = false }.Run(ctx, maxListSize: 1_000_000)),
				"The A-list family only (like the old app's 'AList benchmarks' menu item).");

			var words = Serialization.SampleData.Words;
			AddConsole(registry, "Loyc.Collections/Hashtrees (InternalSet) vs HashSet & Dictionary",
				() => Benchmarks.BenchmarkSets(words),
				"The old 'Hashtrees vs HashSet/Dictionary' console benchmark — takes a few minutes.");

			AddConsole(registry, "Loyc.Essentials/Thread-local storage", Benchmarks.ThreadLocalStorage,
				"ThreadStatic vs Thread.GetData vs ThreadLocalVariable.");
			AddConsole(registry, "Loyc.Essentials/IEnumerator vs Iterator", Benchmarks.EnumeratorVsIterator,
				"Iteration interface overhead comparison.");

			AddConsole(registry, "Loyc.Math/Convex hull", Benchmarks.ConvexHull,
				"Loyc.Math convex-hull computation speed.");

			AddConsole(registry, "Loyc.Utilities/CPTrie (strings)", () => CPTrieBenchmark.BenchmarkStrings(words),
				"String keys: CPTrie vs SortedDictionary vs Dictionary — takes several minutes " +
				"(iteration counts are fixed inside Loyc.Utilities and were not reduced).");
			AddConsole(registry, "Loyc.Utilities/CPTrie (integers)", CPTrieBenchmark.BenchmarkInts,
				"Integer keys: CPTrie vs SortedDictionary vs Dictionary.");
			AddConsole(registry, "Loyc.Utilities/GoInterface", GoInterfaceBenchmark.DoBenchmark,
				"Dynamic interface wrapper (GoInterface) call overhead.");

			AddConsole(registry, ".NET runtime/LINQ vs for-loop", Benchmarks.LinqVsForLoop,
				"Cost of LINQ-to-objects compared with a plain loop.");
			AddConsole(registry, ".NET runtime/Byte array access", Benchmarks.ByteArrayAccess,
				"Ways of reading a byte array: indexer, BitConverter, unsafe pointers.");
		}

		static void AddConsole(BenchmarkRegistry registry, string path, Action action, string description)
			=> registry.Add(path, ctx => ConsoleCapture.Run(ctx, action), description);
	}
}
