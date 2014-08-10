using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Math;
using Loyc;
using Loyc.Collections.Impl;
using System.Drawing;
#if !DotNet35
using OxyPlot;
using OxyPlot.Axes;
#endif

namespace Benchmark
{
	class ListBenchmarks
	{
		IAdd<EzDataPoint> _graph;

		public void Run(EzChartForm graph = null)
		{
			Benchmarker b = new Benchmarker(1);
			_graph = graph;
			#if !DotNet35
			graph.InitDefaultModel = (id, model) =>
			{
				model.LegendPosition = LegendPosition.TopLeft;
				model.PlotMargins = new OxyThickness(double.NaN, double.NaN, 12, double.NaN); // avoid cutting off "1000000" (NaN=autodetect)
				model.Axes.Add(new LogarithmicAxis {
					Position = AxisPosition.Bottom,
					Title = "List size",
					MajorGridlineStyle = LineStyle.Solid,
					MinorGridlineStyle = LineStyle.Dot
				});
				int X = id.ToString().StartsWith("Scan by") ? StdIterations * 100 : StdIterations;
				model.Axes.Add(new LinearAxis { 
					Position = AxisPosition.Left, 
					Title = string.Format("Milliseconds to perform {0:n0} iterations", X),
					MajorGridlineStyle = LineStyle.Solid,
					MinorGridlineStyle = LineStyle.Dot,
					Minimum = 0,
				});
			};
			#endif
			
			Run(b, 100);
			Run(b, 300);
			Run(b, 1000);
			Run(b, 3000);
			Run(b, 10000);
			Run(b, 30000);
			Run(b, 100000);
			Run(b, 300000);
			Run(b, 1000000);
		}

		int _count;
		int _seed = Environment.TickCount;

		public void Run(Benchmarker b, int listCount)
		{
			_count = listCount;
			
			int end = Console.CursorTop;
			b.RunPublicBenchmarks(this, false, () =>
			{
				int start = Console.CursorTop;
				b.PrintResults(Console.Out);
				end = Console.CursorTop;
				Console.CursorTop = start;

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
						_graph.Add(dp);

						// Make a second version of the dictionary graph without SortedList
						if (dp.GraphId.ToString().StartsWith("Dictionary") && dp.Series != "SortedList") {
							dp = dp.Clone();
							dp.GraphId = dp.GraphId.ToString() + " (no SortedList)";
							_graph.Add(dp);
						}
						
						// Make a second version of the "@ random indexes" graphs for small lists
						if (dp.GraphId.ToString().Contains("random index") && (double)dp.Parameter < 10000) {
							dp = dp.Clone();
							dp.GraphId = dp.GraphId.ToString() + " "; // extra space just to change the graph ID
							_graph.Add(dp);
						}
					}
				}
			},
			string.Format("{0,8}: ", listCount), true);
			Console.CursorTop = end;


		}

		const int StdIterations = 100000; // Number of random insert/remove ops to perform
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
					if (c >= _count + 5) list.RemoveRange(list.Count - 10, 10);
				}
			});
			b.Run("DList", () =>
			{
				var list = MakeList(new DList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.Insert(_r.Next(c + 1), i);
					if (c >= _count + 5) list.RemoveRange(list.Count - 10, 10);
				}
			});
			b.Run("AList", () =>
			{
				var list = MakeList(new AList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.Insert(_r.Next(c + 1), i);
					if (c >= _count + 5) list.RemoveRange(list.Count - 10, 10);
				}
			});
			return Benchmarker.DiscardResult;
		}

		[Benchmark("Insert at end", Trials=5)]
		public object InsertSequentially(Benchmarker b)
		{
			b.Run("List", () =>
			{
				var list = new List<long>();
				for (int i = 0; i < StdIterations; i++)
					list.Add(i);
			});
			b.Run("DList", () =>
			{
				var list = new DList<long>();
				for (int i = 0; i < StdIterations; i++)
					list.Add(i);
			});
			b.Run("AList", () =>
			{
				var list = new AList<long>();
				for (int i = 0; i < StdIterations; i++)
					list.Add(i);
			});
			return Benchmarker.DiscardResult;
		}

		// Make a list of size _count for a benchmark. The listmaking time is excluded from the total time.
		private List MakeList<List>(List list, Benchmarker b, bool new_r = true) where List : IList<long>
		{
			b.PauseTimer();
			for (int i = 0; i < _count; i++)
				list.Add(i);
			if (new_r)
				_r = new Random(_seed);
			b.ResumeTimer();
			return list;
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
			b.Run("DList", () =>
			{
				var list = MakeList(new DList<long>(), b);
				for (int i = 0; i < StdIterations; i++) {
					int c = list.Count;
					list.RemoveAt(_r.Next(c));
					if (c <= _count - 5) list.AddRange((ICollection<long>)more);
				}
			});
			b.Run("AList", () =>
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
			b.Run("DList", b_ =>
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
			b.Run("InternalDList", b_ =>
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
			b.Run("AList", b_ =>
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

			b.Run("List", b_ =>
			{
				var list = MakeList(new List<long>(), b_);
				double avg = 0;
				for (int c = 0; c < Cycles; c++)
					avg = list.Average();
				return "Avg: " + avg;
			});
			b.Run("DList", b_ =>
			{
				var list = MakeList(new DList<long>(), b_);
				double avg = 0;
				for (int c = 0; c < Cycles; c++)
					avg = list.Average();
				return "Avg: " + avg;
			});
			b.Run("AList", b_ =>
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
			b.Run("BDictionary", () =>
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
			if (_count < 10000 || b.CurrentTrialNumber == 1)
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
			b.Run("MMap", () =>
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
