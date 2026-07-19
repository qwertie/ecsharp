#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Math;
using Loyc;
using Loyc.Collections.Impl;
using System.Reflection;

namespace Benchmark
{
	/// <summary>The classic Loyc collection benchmarks (AList, DList, SparseAList,
	/// BDictionary, MMap, ...), ported from the old OxyPlot/WinForms chart app; data
	/// points now flow to the web UI's charts through <see cref="BenchmarkContext"/>.</summary>
	class ListBenchmarks
	{
		IAdd<EzDataPoint> _graph;
		Predicate<EzDataPoint> _where;
		BenchmarkContext _ctx;

		public bool TestALists = true;
		public bool TestDLists = true;
		public bool TestOther = true;
		int _count;
		int _seed = Environment.TickCount;

		Dictionary<object, Action<GraphModel>> _graphConfiguration = new Dictionary<object, Action<GraphModel>>();
		HashSet<object> _configuredGraphs = new HashSet<object>();

		void Add(EzDataPoint dp)
		{
			if (_where == null || _where(dp)) {
				ConfigureGraphOnce(dp.GraphId);
				_graph.Add(dp);
			}
		}

		// Replaces EzChartForm.InitDefaultModel: sets up a graph's axes the first
		// time a data point is sent to it.
		void ConfigureGraphOnce(object graphId)
		{
			if (_configuredGraphs.Add(graphId)) {
				var m = new GraphModel(graphId.ToString()) { Title = graphId.ToString() };
				if (_graphConfiguration.TryGetValue(graphId, out var action))
					action(m);
				else
					AddStandardAxes(m, string.Format("Milliseconds to perform {0:n0} iterations", StdIterations), yMinimum: 0);
				_ctx.ConfigureGraph(m);
			}
		}

		public void Run(BenchmarkContext ctx, int maxListSize = 1000000, Predicate<EzDataPoint> where = null)
		{
			Benchmarker b = new Benchmarker(1);
			_ctx = ctx;
			_graph = ctx;
			_where = where;

			_r = new Random(_seed);
			ctx.Progress(0, "Measuring collection memory use");
			RunListSizeBenchmarks("Bytes used per list item", "Bytes used per 8-byte item", true);
			RunListSizeBenchmarks("Bytes used per list", "Total heap bytes", false);
			RunDictionarySizeBenchmarks("Bytes per dictionary pair", "Bytes used per 16-byte item", true);
			RunDictionarySizeBenchmarks("Bytes per dictionary", "Total heap bytes", false);

			int[] sizes = { 30, 100, 300, 1000, 3000, 10000, 30000, 100000, 300000, 1000000 };
			var sizesToRun = sizes.Where(s => s <= maxListSize).ToList();
			for (int i = 0; i < sizesToRun.Count; i++) {
				ctx.Progress((double)i / sizesToRun.Count, $"List size {sizesToRun[i]:n0}");
				Run(b, sizesToRun[i]);
			}
			ctx.Progress(1);
		}

		private void AddStandardAxes(GraphModel m, string yAxisLabel, int? yMinimum = null, int? yMaximum = null)
		{
			m.XAxisTitle = "List size";
			m.XLogScale = true;
			m.YAxisTitle = yAxisLabel;
			m.YMin = yMinimum ?? -1;
			if (yMaximum != null)
				m.YMax = yMaximum.Value;
		}

		void RunListSizeBenchmarks(string id, string yAxis, bool perItem)
		{
			_graphConfiguration[id] = m => AddStandardAxes(m, yAxis, 0, yMaximum: perItem ? 60 : (int?)null);
			int limit = perItem ? 9999 : 133;
			for (int size = 1; size <= limit; size += Math.Max(1, size / 8))
			{
				double factor = perItem ? 1.0 / size : 1.0;
				
				var list = AddAtEnd(new List<long>(), size);
				var lList = AddAtEnd(new LinkedList<long>(), size);
				Add(new EzDataPoint { GraphId = id, Series = "List<long>", Parameter = size, Value = CountSizeInBytes(list, 8) * factor });
				Add(new EzDataPoint { GraphId = id, Series = "LinkedList<long>", Parameter = size, Value = CountSizeInBytes(lList, 8) * factor });

				if (TestALists)
				{
					var alist = AddAtEnd(new AList<long>(), size);
					var alistR = AddAtRandom(new AList<long>(), size);
					Add(new EzDataPoint { GraphId = id, Series = "AList<long> (sequential fill)", Parameter = size, Value = alist.CountSizeInBytes(8) * factor });
					//Add(new EzDataPoint { GraphId = id, Series = "AList<long> (random fill)", Parameter = size, Value = alistR.CountSizeInBytes(8) * factor });
					var salist = AddAtEnd(new SparseAList<long>(), size);
					var salistR = AddAtRandom(new SparseAList<long>(), size);
					Add(new EzDataPoint { GraphId = id, Series = "SparseAList<long> (sequential fill)", Parameter = size, Value = salist.CountSizeInBytes(8) * factor });
					//Add(new EzDataPoint { GraphId = id, Series = "SparseAList<long> (random fill)", Parameter = size, Value = salistR.CountSizeInBytes(8) * factor });
				}
				if (TestOther)
				{
					var ialist = AddAtEnd(new IndexedAList<long>(), size);
					var ialistR = AddAtRandom(new IndexedAList<long>(), size);
					Add(new EzDataPoint { GraphId = id, Series = "IndexedAList<long> (sequential fill)", Parameter = size, Value = ialist.CountSizeInBytes(8) * factor });
					//Add(new EzDataPoint { GraphId = id, Series = "IndexedAList<long> (random fill)", Parameter = size, Value = ialistR.CountSizeInBytes(8) * factor });
				}
			}
		}

		void RunDictionarySizeBenchmarks(string id, string yAxis, bool perItem)
		{
			_graphConfiguration[id] = m => AddStandardAxes(m, yAxis, 0, yMaximum: perItem ? 100 : (int?)null);
			int limit = perItem ? 9999 : 133;
			for (int size = 1; size <= limit; size += Math.Max(1, size / 8))
			{
				double factor = perItem ? 1.0 / size : 1.0;

				var dict = FillDictionary(new Dictionary<long, long>(), size, false);
				var dictR = FillDictionary(new Dictionary<long, long>(), size, true);
				//Add(new EzDataPoint { GraphId = id, Series = "Dictionary<long,long> (sequential fill)", Parameter = size, Value = CountSizeInBytes(dict, 16) * factor });
				Add(new EzDataPoint { GraphId = id, Series = "Dictionary<long,long>", Parameter = size, Value = CountSizeInBytes(dictR, 16) * factor });
				
				var sdictR = FillDictionary(new SortedDictionary<long, long>(), size, true);
				Add(new EzDataPoint { GraphId = id, Series = "SortedDictionary<long,long>", Parameter = size, Value = CountSizeInBytes(sdictR, 16) * factor });
				
				if (TestALists)
				{
					var bdict = FillDictionary(new BDictionary<long,long>(), size, false);
					var bdictR = FillDictionary(new BDictionary<long,long>(), size, true);
					//Add(new EzDataPoint { GraphId = id, Series = "BDictionary<long,long> (sequential fill)", Parameter = size, Value = bdict.CountSizeInBytes(16, 8) * factor });
					Add(new EzDataPoint { GraphId = id, Series = "BDictionary<long,long> (random fill)", Parameter = size, Value = bdictR.CountSizeInBytes(16, 8) * factor });
				}

				if (TestOther)
				{
					var map = FillDictionary(new MMap<long,long>(), size, false);
					var mapR = FillDictionary(new MMap<long,long>(), size, true);
					Add(new EzDataPoint { GraphId = id, Series = "MMap<long,long> (sequential fill)", Parameter = size, Value = map.CountMemory(16) * factor });
					Add(new EzDataPoint { GraphId = id, Series = "MMap<long,long> (random fill)", Parameter = size, Value = mapR.CountMemory(16) * factor });
				}
			}
		}

		public static long CountSizeInBytes<T>(List<T> list, int sizeOfT)
		{
			// fields: _items, _size, _version, _syncRoot
			return 4 * IntPtr.Size + 8 + (list.Capacity == 0 ? 0 : 3 * IntPtr.Size + (long)sizeOfT * list.Capacity);
		}

		public static long CountSizeInBytes<T>(LinkedList<T> list, int sizeOfT)
		{
			// fields of LinkedList<T>:
			//   internal LinkedListNode<T> head;
			//   internal int count;
			//   internal int version;
			//   private object _syncRoot;
			//   private SerializationInfo siInfo;
			//
			// fields of LinkedListNode<T>:
			//   internal LinkedList<T> list;
			//   internal LinkedListNode<T> next;
			//   internal LinkedListNode<T> prev;
			//   internal T item;
			int nodeSize = 5 * IntPtr.Size + sizeOfT;
			return 5 * IntPtr.Size + 8 + (list.Count * nodeSize);
		}

		public static double CountSizeInBytes<K, V>(Dictionary<K, V> dict, int sizeOfPair)
		{
			// fields:
			//   private int[] buckets;
			//   private Dictionary<TKey, TValue>.Entry[] entries;
			//   private int count;
			//   private int version;
			//   private int freeList;
			//   private int freeCount;
			//   private IEqualityComparer<TKey> comparer;
			//   private Dictionary<TKey, TValue>.KeyCollection keys;
			//   private Dictionary<TKey, TValue>.ValueCollection values;
			//   private object _syncRoot;
			//
			// private struct Entry {
			//   public int hashCode;
			//   public int next;
			//   public TKey key;
			//   public TValue value;
			// }
			// Field names changed from "buckets"/"entries" (.NET Framework) to
			// "_buckets"/"_entries" (.NET Core+)
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
			var bucketsField = dict.GetType().GetField("_buckets", flags) ?? dict.GetType().GetField("buckets", flags);
			var entriesField = dict.GetType().GetField("_entries", flags) ?? dict.GetType().GetField("entries", flags);
			var buckets = (Array)bucketsField.GetValue(dict);
			var entries = (Array)entriesField.GetValue(dict);
			return 4 * 4 + 8 * IntPtr.Size + (buckets?.LongLength ?? 0) * 4 + (entries?.LongLength ?? 0) * (8 + sizeOfPair);
		}

		public static double CountSizeInBytes<K, V>(SortedDictionary<K, V> dict, int sizeOfPair)
		{
			// SortedDictionary<TKey, TValue>:
			//   private SortedDictionary<TKey, TValue>.KeyCollection keys;
			//   private SortedDictionary<TKey, TValue>.ValueCollection values;
			//   private TreeSet<KeyValuePair<TKey, TValue>> _set;
			// TreeSet<T> (actually SortedSet<T>)
			//   private SortedSet<T>.Node root;
			//   private IComparer<T> comparer;
			//   private int count;
			//   private int version;
			//   private object _syncRoot;
			//   private SerializationInfo siInfo;
			//   internal class Node {
			//     public bool IsRed; // usually consumes 4 or 8 bytes (optimistically assume 4)
			//     public T Item;
			//     public SortedSet<T>.Node Left;
			//     public SortedSet<T>.Node Right;
			//   }
			int sortedDictSize = 5 * IntPtr.Size;
			int treeSetSize = 6 * IntPtr.Size + 8;
			int nodeSize = 4 + sizeOfPair + 4 * IntPtr.Size;
			return sortedDictSize + treeSetSize + dict.Count * nodeSize;
		}

		List AddAtRandom<List>(List list, int count) where List : IList<long>
		{
			for (int i = 0; i < count; i++)
				list.Insert(_r.Next(i+1), i);
			return list;
		}

		static List AddAtEnd<List>(List list, int count) where List:ICollection<long>
		{
			for (int i = 0; i < count; i++)
				list.Add(i);
			return list;
		}

		Dict FillDictionary<Dict>(Dict list, int count, bool random) where Dict:ICollection<KeyValuePair<long,long>>
		{
			for (int i = 0; i < count; i++)
			{
				try {
					list.Add(new KeyValuePair<long, long>(random ? _r.Next(int.MaxValue) : i, i));
				} catch { i--; } // low probability of duplicate key
			}
			return list;
		}

		// Make a list of size _count for a benchmark. The listmaking time is excluded from the total time.
		private List MakeList<List>(List list, Benchmarker b, bool new_r = true) where List : ICollection<long>
		{
			b.PauseTimer();
			list = AddAtEnd(list, _count);
			if (new_r)
				_r = new Random(_seed);
			b.ResumeTimer();
			return list;
		}

		public void Run(Benchmarker b, int listCount)
		{
			_count = listCount;

			b.RunPublicBenchmarks(this, false, () =>
			{
				b.PrintResults(Console.Out);

				// Send results to _graph
				if (_graph != null) {
					foreach (var row in b.Results) {
						string rowName = row.Key;
						var result = row.Value;
						var pair1 = rowName.SplitAt(": ");
						var pair2 = pair1.B.SplitAt(": ");
						
						var graphName = pair2.A.ToString();
						if (graphName.StartsWith("Scan by") && graphName.EndsWith("x"))
							// avoid creating separate graphs for "Scan by ... 1000x" and "Scan by ... 333x"
							graphName = graphName.Left(graphName.LastIndexOf(' '));

						var dp = new EzDataPoint {
							Parameter = double.Parse(pair1.A.ToString()),
							GraphId = graphName,
							Series = pair2.B.ToString(),
							Value = result.Avg()
						};
						Add(dp);

						// Make a second version of the dictionary graph without SortedList
						if (dp.GraphId.ToString().StartsWith("Dictionary") && dp.Series != "SortedList") {
							dp = dp.Clone();
							dp.GraphId = dp.GraphId.ToString() + " (no SortedList)";
							Add(dp);
						}
						
						// Make a second version of the "@ random indexes" graphs for small lists
						if (dp.GraphId.ToString().Contains("random index") && (double)dp.Parameter < 10000) {
							dp = dp.Clone();
							dp.GraphId = dp.GraphId.ToString() + "\u200B"; // zero-width space just to change the graph ID
							Add(dp);
						}
					}
				}
			},
			string.Format("{0,8}: ", listCount), true);
		}

		const int StdIterations = 5000; // Number of random insert/remove ops to perform
		Random _r;

		[Benchmark("Insert at random indexes")]
		public object InsertRandom(Benchmarker b)
		{
			b.Run("List", () =>
			{
				var list = MakeList(new List<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.Insert(_r.Next(c + 1), i);
					if (c >= _count + 10) list.RemoveRange(list.Count - 20, 20);
				}
			});
			b.Run("InternalList", TestOther, () =>
			{
				var list = MakeList(InternalList<long>.Empty, b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.Insert(_r.Next(c + 1), i);
					if (c >= _count + 10) list.RemoveRange(list.Count - 20, 20);
				}
			});
			b.Run("DList", TestDLists, () =>
			{
				var list = MakeList(new DList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.Insert(_r.Next(c + 1), i);
					if (c >= _count + 10) list.RemoveRange(list.Count - 20, 20);
				}
			});
			b.Run("AList", TestALists, () =>
			{
				var list = MakeList(new AList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.Insert(_r.Next(c + 1), i);
					if (c >= _count + 10) list.RemoveRange(list.Count - 20, 20);
				}
			});
			return Benchmarker.DiscardResult;
		}

		[Benchmark("Insert at end", Trials = 20)]
		public object InsertSequentially(Benchmarker b)
		{
			var list2 = new DList<long>();
			AddAtEnd(list2, StdIterations);
			b.Run("List", () =>
			{
				var list = MakeList(new List<long>(), b);
				AddAtEnd(list, StdIterations);
			});
			// \u200B (zero-width space) changes the sort order so that colors
			// on the graph are consistent between different graphs
			b.Run("\u200BList.AddRange", () =>
			{
				var list = MakeList(new List<long>(), b);
				list.AddRange(list2);
			});
			b.Run("InternalList", TestOther, () =>
			{
				var list = MakeList(InternalList<long>.Empty, b);
				AddAtEnd(list, StdIterations);
			});
			b.Run("DList", TestDLists, () =>
			{
				var list = MakeList(new DList<long>(), b);
				AddAtEnd(list, StdIterations);
			});
			b.Run("AList", TestALists, () =>
			{
				var list = MakeList(new AList<long>(), b);
				AddAtEnd(list, StdIterations);
			});
			b.Run("\u200BAList.AddRange", TestALists, () =>
			{
				var list = MakeList(new AList<long>(), b);
				list.AddRange(list2);
			});
			return Benchmarker.DiscardResult;
		}

		[Benchmark("Change random elements", Trials = 10)]
		public object ChangeRandom(Benchmarker b)
		{
			b.Run("List", () =>
			{
				var list = MakeList(new List<long>(), b);
				int maxIndex = list.Count - 1;
				for (int iter = 0; iter < StdIterations; iter++) {
					int i = _r.Next(maxIndex);
					list[i++] = iter;
					list[i] = iter;
				}
			});
			b.Run("InternalList", TestOther, () =>
			{
				var list = MakeList(InternalList<long>.Empty, b);
				int maxIndex = list.Count - 1;
				for (int iter = 0; iter < StdIterations; iter++) {
					int i = _r.Next(maxIndex);
					list[i++] = iter;
					list[i] = iter;
				}
			});
			b.Run("DList", TestDLists, () =>
			{
				var list = MakeList(new DList<long>(), b);
				int maxIndex = list.Count - 1;
				for (int iter = 0; iter < StdIterations; iter++) {
					int i = _r.Next(maxIndex);
					list[i++] = iter;
					list[i] = iter;
				}
			});
			b.Run("AList", TestALists, () =>
			{
				var list = MakeList(new AList<long>(), b);
				int maxIndex = list.Count - 1;
				for (int iter = 0; iter < StdIterations; iter++) {
					int i = _r.Next(maxIndex);
					list[i++] = iter;
					list[i] = iter;
				}
			});
			return Benchmarker.DiscardResult;
		}


		[Benchmark("Remove at random indexes")]
		public object RemoveRandom(Benchmarker b)
		{
			var more = new long[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, };
			b.Run("List", () =>
			{
				var list = MakeList(new List<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.RemoveAt(_r.Next(c));
					if (c <= _count - 5) list.AddRange(more);
				}
			});
			b.Run("InternalList", TestOther, () =>
			{
				var list = MakeList(InternalList<long>.Empty, b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.RemoveAt(_r.Next(c));
					if (c <= _count - 5) list.AddRange((IList<long>)more);
				}
			});
			b.Run("DList", TestDLists, () =>
			{
				var list = MakeList(new DList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.RemoveAt(_r.Next(c));
					if (c <= _count - 5) list.AddRange((ICollection<long>)more);
				}
			});
			b.Run("AList", TestALists, () =>
			{
				var list = MakeList(new AList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.RemoveAt(_r.Next(c));
					if (c <= _count - 5) list.AddRange(more);
				}
			});
			return Benchmarker.DiscardResult;
		}

		[Benchmark("Scan by [index] repeatedly", Trials=3)]
		public object ScanIndexer(Benchmarker b)
		{
			int Cycles = Math.Max(MathEx.MulDiv(StdIterations, 100, _count), 1);
			int r = b.ActiveBenchmarkName.IndexOf("repeatedly");
			if (r > -1)
				b.ActiveBenchmarkName = b.ActiveBenchmarkName.Left(r) + Cycles + "x";

			_graphConfiguration[b.ActiveBenchmarkName] = m =>
				AddStandardAxes(m, string.Format("Milliseconds to perform {0:n0} iterations", StdIterations * 100));

			b.Run("List", b_ =>
			{
				var list = MakeList(new List<long>(), b_);
				long sum = 0;
				for (int c = 0; c < Cycles; c++) {
					sum = 0;
					for (int i = 0; i < list.Count; i++)
						sum += list[i];
				}
				return "Sum: " + sum;
			});
			b.Run("InternalList", TestOther, b_ =>
			{
				var list = MakeList(InternalList<long>.Empty, b_);
				long sum = 0;
				for (int c = 0; c < Cycles; c++) {
					sum = 0;
					for (int i = 0; i < list.Count; i++)
						sum += list[i];
				}
				return "Sum: " + sum;
			});
			b.Run("DList", TestDLists, b_ =>
			{
				var list = MakeList(new DList<long>(), b_);
				long sum = 0;
				for (int c = 0; c < Cycles; c++) {
					sum = 0;
					for (int i = 0; i < list.Count; i++)
						sum += list[i];
				}
				return "Sum: " + sum;
			});
			b.Run("\u200BInternalDList", TestDLists, b_ =>
			{
				b_.PauseTimer();
				var list = InternalDList<long>.Empty;
				for (int i = 0; i < _count; i++)
					list.Add(i);
				_r = new Random(_seed);
				b_.ResumeTimer();

				long sum = 0;
				for (int c = 0; c < Cycles; c++) {
					sum = 0;
					for (int i = 0; i < list.Count; i++)
						sum += list[i];
				}
				return "Sum: " + sum;
			});
			b.Run("AList", TestALists, b_ =>
			{
				var list = MakeList(new AList<long>(), b_);
				long sum = 0;
				for (int c = 0; c < Cycles; c++) {
					sum = 0;
					for (int i = 0; i < list.Count; i++)
						sum += list[i];
				}
				return "Sum: " + sum;
			});
			return Benchmarker.DiscardResult;
		}

		[Benchmark("Scan by IEnumerator repeatedly", Trials=3)]
		public object ScanIEnumerator(Benchmarker b)
		{
			int Cycles = Math.Max(MathEx.MulDiv(StdIterations, 100, _count), 1);
			int r = b.ActiveBenchmarkName.IndexOf("repeatedly");
			if (r > -1)
				b.ActiveBenchmarkName = b.ActiveBenchmarkName.Left(r) + Cycles + "x";

			_graphConfiguration[b.ActiveBenchmarkName] = m =>
				AddStandardAxes(m, string.Format("Milliseconds to perform {0:n0} iterations", StdIterations * 100));

			b.Run("List", b_ =>
			{
				var list = MakeList(new List<long>(), b_);
				double avg = 0;
				for (int c = 0; c < Cycles; c++)
					avg = list.Average();
				return "Avg: " + avg;
			});
			b.Run("InternalList", TestOther, b_ =>
			{
				var list = MakeList(InternalList<long>.Empty, b_);
				double avg = 0;
				for (int c = 0; c < Cycles; c++)
					avg = list.Average();
				return "Avg: " + avg;
			});
			b.Run("DList", TestDLists, b_ =>
			{
				var list = MakeList(new DList<long>(), b_);
				double avg = 0;
				for (int c = 0; c < Cycles; c++)
					avg = list.Average();
				return "Avg: " + avg;
			});
			b.Run("AList", TestALists, b_ =>
			{
				var list = MakeList(new AList<long>(), b_);
				double avg = 0;
				for (int c = 0; c < Cycles; c++)
					avg = list.Average();
				return "Avg: " + avg;
			});
			return Benchmarker.DiscardResult;
		}

		[Benchmark("Dictionary random add+remove", Trials = 3)]
		public object DictionaryBenchmarks(Benchmarker b)
		{
			int max;
			b.Run("SortedDictionary", () =>
			{
				var list = MakeDict(new SortedDictionary<long, int>(), b, out max);
				for (int i = 0; i < StdIterations; i++) {
					int k = _r.Next(max);
					list[k] = i;
					if (list.Count > _count)
						list.Remove(k);
				}
			});
			b.Run("BDictionary", TestALists, () =>
			{
				var list = MakeDict(new BDictionary<long, int>(), b, out max);
				for (int i = 0; i < StdIterations; i++) {
					int k = _r.Next(max);
					list[k] = i;
					if (list.Count > _count)
						list.Remove(k);
				}
			});
			// don't waste time running multiple trials for large SortedList
			if (_count <= 333333 && (_count < 10000 || b.CurrentTrialNumber == 1))
				b.Run("SortedList", () =>
				{
					var list = MakeDict(new SortedList<long, int>(), b, out max);
					for (int i = 0; i < StdIterations; i++) {
						int k = _r.Next(max);
						list[k] = i;
						if (list.Count > _count)
							list.Remove(k);
					}
				});
			b.Run("Dictionary", () =>
			{
				var list = MakeDict(new Dictionary<long, int>(), b, out max);
				for (int i = 0; i < StdIterations; i++) {
					int k = _r.Next(max);
					list[k] = i;
					if (list.Count > _count)
						list.Remove(k);
				}
			});
			b.Run("MMap", TestOther, () =>
			{
				var list = MakeDict(new MMap<long, int>(), b, out max);
				for (int i = 0; i < StdIterations; i++) {
					int k = _r.Next(max);
					list[k] = i;
					if (list.Count > _count)
						list.Remove(k);
				}
			});
			return Benchmarker.DiscardResult;
		}

		// Make a dictionary of size _count for a benchmark. Time used is excluded from the total time.
		private Dict MakeDict<Dict>(Dict list, Benchmarker b, out int max, bool new_r = true) where Dict : IDictionary<long,int>
		{
			b.PauseTimer();
			if (new_r)
				_r = new Random(_seed);
			int k = 0;
			for (int i = 0; i < _count; i++) {
				list.Add(k, i);
				k += _r.Next(50, 150);
			}
			b.ResumeTimer();
			max = k;
			return list;
		}
	}
}
