using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Loyc.Utilities;
using Loyc;

namespace Benchmark
{
	/// <summary>
	/// A simple benchmarking framework that produces a fairly nice result table.
	/// </summary>
	/// <remarks>
	/// This framework is designed mainly to measure the time that benchmarks
	/// require, but it can also report information supplied by the benchmarks
	/// themselves (see below).
	/// <para/>
	/// The main methods of interest are <see cref="RunAllBenchmarks"/>,
	/// <see cref="RunAllBenchmarksInConsole"/>, <see cref="MeasureAndRecord"/>
	/// and <see cref="PrintResults"/> (if you want to print the results to a file).
	/// <para/>
	/// Benchmarker is designed for relatively long-running benchmarks (100+
	/// milliseconds) because Environment.TickCount is used for measurements, which
	/// typically has a resolution of 15.6 ms under Windows. Also, benchmark methods 
	/// are invoked via reflection. Therefore, if you are testing a small operation,
	/// you should write a loop with many iterations (up to millions).
	/// <para/>
	/// Benchmarker normally runs several trials, in order to detect any "jitter" in
	/// the results. It normally shows the first-run time, average time, 
	/// minimum running time, maximum time, and standard deviation, but the min, max
	/// and standard deviation columns are not shown when running less than 3 trials.
	/// If the result table shows "Min" and "Max" values that are quite different, 
	/// and the "Std.Dev" (standard deviation) is high, there's probably something 
	/// weird going on. It is not unusual, however, that the first trial takes 
	/// slightly longer (as the .NET Framework JITs it on the first run).
	/// <para/>
	/// Benchmarker can run benchmarks either in collated (alphabetical) order or in
	/// a random order that mixes different trials together.
	/// <para/>
	/// Benchmark trials can return comments or other information. Benchmarker
	/// builds a list of unique objects returned from benchmarks (duplicate datums
	/// from the same benchmark, and null datums, are ignored). By default, 
	/// PrintResults() converts this user-defined data to strings, concatenates the 
	/// data for each benchmark, and prints it out as the last column. You can 
	/// customize the way that the data are aggregated into a string via the 
	/// userDataFormatter parameter. The <see cref="UserDataColumnName"/> attribute 
	/// controls the name of this final column. The final column will not be shown 
	/// if none of the benchmarks returned any (non-null) data.
	/// </remarks>
	public class Benchmarker
	{
		/// <summary>Whether to garbage-collect before each test.</summary>
		/// <remarks>Since the garbage collector can run at any time, garbage from 
		/// one benchmark may be cleaned up in a different benchmark, or not, causing
		/// inconsistency in the results. By garbage-collecting this effect is
		/// hopefully avoided; by default this option is true.</remarks>
		public bool DoGC = true;

		/// <summary>Number of times that RunAllBenchmarksInConsole or
		/// RunAllBenchmarks should run each benchmark method that doesn't specify
		/// how many trials to run.</summary>
		#if CompactFramework
		public int DefaultNumTrials = 2;
		// CE has ms-resolution timer and consistent measurements; no need for many trials
		#else
		public int DefaultNumTrials = 5;
		// Desktop Windows' GetTickCount() has 15ms resolution
		#endif

		/// <summary>If benchmarks return data, it will be displayed in the last
		/// column, which is given this column heading.</summary>
		public string UserDataColumnName = "Comment";
		
		public static readonly string DiscardResult = "(discard result)";
		
		enum Overhead { }
		public static object SubtractOverhead(int millisec)
		{
			return (Overhead)millisec;
		}

		protected Dictionary<string, int> _errors = new Dictionary<string, int>();
		protected string _activeBenchmark;
		// BTW: SortedList is very slow for insert/delete; you should usually use
		// SortedDictionary instead, but it is missing from the Compact Framework.
		protected SortedList<string, BenchmarkStatistic> _results = new SortedList<string, BenchmarkStatistic>();
		IEnumerable<KeyValuePair<string, BenchmarkStatistic>> Results
		{
			get { return _results; }
		}

		public void MeasureAndRecord(string name, Action code)
		{
			MeasureAndRecord(name, () => { code(); return null; });
		}

		/// <summary>
		/// Measures and records the time required for a given piece of code to run.
		/// Supports nested measurements.
		/// </summary>
		/// <param name="name">Name of a benchmark or sub-benchmark.</param>
		/// <param name="code">Code to run.</param>
		/// <remarks>A benchmark method called by RunAllBenchmarks can call this
		/// method to run a sub-benchmark. A row on the results table will be created
		/// with both the name of the outer benchmark and the sub-benchmark. The
		/// outer benchmark's time will include time used by the sub-benchmark.</remarks>
		public void MeasureAndRecord(string name, Func<object> code)
		{
			var oldActive = _activeBenchmark;
			if (_activeBenchmark != null)
				_activeBenchmark += ": " + name;
			else
				_activeBenchmark = name;

			try {
				object userData;
				double time = Measure(code, out userData);
				if (userData as string != DiscardResult)
					Tally(_activeBenchmark, time, userData);
			}
			catch(Exception ex)
			{
				Type excType = ex.GetType();
				string msg = string.Format("{0}: {1}", ex.GetType().Name, ex.Message);
				while (ex.InnerException != null) {
					ex = ex.InnerException;
					msg += string.Format("\n  Inner {0}: {1} ", ex.GetType().Name, ex.Message);
					if (excType == typeof(TargetInvocationException))
						excType = ex.GetType();
				}

				int count;
				_errors.TryGetValue(msg, out count);
				_errors[msg] = count + 1;

				TallyError(_activeBenchmark, excType.Name);
			}
			finally
			{
				_activeBenchmark = oldActive;
			}
		}

		/// <summary>Runs a piece of code and returns the number of seconds it required.</summary>
		/// <remarks>Garbage-collects before the test if DoGC is true.</remarks>
		public double Measure(Func<object> code, out object userData)
		{
			// Give the test as good a chance as possible to avoid garbage collection
			if (DoGC)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
				GC.Collect();
			}

			SimpleTimer timer = new SimpleTimer();
			userData = code();
			double time = timer.Millisec / 1000.0;
			if (userData is Overhead)
			{
				time -= (int)(Overhead)userData / 1000.0;
				userData = null;
			}
			return time;
		}

		protected void TallyError(string name, string msg)
		{
			Tally(name, double.NaN, msg);
		}
		protected void Tally(string name, double seconds, object userData)
		{
			BenchmarkStatistic s;
			if (!_results.TryGetValue(name, out s))
				s = new BenchmarkStatistic();
			s.Add(seconds, userData);
			_results[name] = s;
		}

		/// <summary>Runs all public benchmark methods (static and nonstatic) in the
		/// specified object ('set') and records the results. Each method is run the 
		/// number of times indicated by the Trials attribute of BenchmarkAttribute 
		/// (if provided) or DefaultNumTrials.</summary>
		/// <param name="set">An object with methods having the [<see cref="BenchmarkAttribute"/>].</param>
		/// <param name="randomOrder">If true, the order in which the methods are run
		/// is randomized, different trials are even mixed together. If false, the
		/// methods are run in order, collated, sorted by method name.</param>
		/// <param name="postprocess">A method to run after each trial, if desired, 
		/// or null.</param>
		/// <remarks>Existing results are Clear()ed before running the benchmarks.</remarks>
		public void RunAllBenchmarks(object set, bool randomOrder, Action postprocess)
		{
			// Get a list of methods to run
			var methods = new List<KeyValuePair<BenchmarkAttribute, MethodInfo>>();
			BindingFlags publics = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
			foreach (MethodInfo method in set.GetType().GetMethods(publics).OrderBy(m => m.Name))
			{
				var attrs = method.GetCustomAttributes(typeof(BenchmarkAttribute), false);
				if (attrs.Length != 0)
					methods.Add(Pair((BenchmarkAttribute)attrs[0], method));
			}

			// Prepare the coallated order to do them in
			var order = new List<KeyValuePair<BenchmarkAttribute, MethodInfo>>();
			bool done = false;
			for (int j = 0; !done; j++) {
				done = true;
				for (int i = 0; i < methods.Count; i++) {
					int trials = methods[i].Key.Trials;
					if (trials < 0)
						trials = DefaultNumTrials;
					if (j < trials) {
						order.Add(methods[i]);
						done = false;
					}
				}
			}

			// Randomize if requested
			if (randomOrder) {
				Random r = new Random();
				for (int i = 0; i < order.Count; i++)
				{
					int j = r.Next(order.Count);
					var temp = order[i];
					order[i] = order[j];
					order[j] = temp;
				}
			}

			Clear();

			// Finally, do the benchmarks
			for (int i = 0; i < order.Count; i++)
			{
				var item = order[i];
				string name = item.Key.Name ?? item.Value.Name;
				MeasureAndRecord(name, () => item.Value.Invoke(set, null));
				if (postprocess != null)
					postprocess();
			}
		}

		/// <summary>Runs all public benchmark methods (static and nonstatic) in the
		/// specified object ('set') and prints results to the console. Each method 
		/// is run the number of times indicated by the Trials attribute of
		/// BenchmarkAttribute (if provided) or DefaultNumTrials.</summary>
		public void RunAllBenchmarksInConsole(object set, bool randomOrder)
		{
			#if CompactFramework
				// Console cursor cannot be controlled in the Compact Framework
				var t = new SimpleTimer();
				RunAllBenchmarks(set, randomOrder, () => {
					// Also, console printing is slow; avoid spending too much time on it
					if (t.ClearAfter(10000) != 0) { PrintResults(Console.Out); t.Restart(); }
				});
				PrintResults(Console.Out);
			#else
				int end = Console.CursorTop;
				RunAllBenchmarks(set, randomOrder, () =>
				{
					int start = Console.CursorTop;
					PrintResults(Console.Out);
					end = Console.CursorTop;
					Console.CursorTop = start;
				});
				Console.CursorTop = end;
			#endif
		}

		/// <summary>Deletes all benchmark results within this object.</summary>
		public void Clear()
		{
			_errors.Clear();
			_results.Clear();
		}

		/// <summary>Prints all recorded results to the specified stream.</summary>
		/// <param name="writer">An output stream.</param>
		public void PrintResults(TextWriter writer)
		{
			PrintResults(writer, "|", true, null);
		}
		/// <summary>Prints all recorded results to the specified stream.</summary>
		/// <param name="writer">An output stream.</param>
		/// <param name="separator">Separator between table columns.</param>
		/// <param name="addPadding">If true, columns are space-padded to equal width.</param>
		/// <param name="userDataFormatter">A function that converts a list of
		/// user-defined data to a string, or null for the default behavior, which
		/// converts the data to strings and concatenates them.</param>
		public void PrintResults(TextWriter writer, string separator, bool addPadding, Func<List<object>, string> userDataFormatter)
		{
			PrintResults(writer, Results.ToList(), separator, addPadding, userDataFormatter, UserDataColumnName);

			// Print errors
			if (_errors.Count > 0) {
				Console.WriteLine("                ");
				Console.WriteLine("Errors occurred:");
				foreach (var pair in _errors)
					Console.WriteLine("{0} x{1}", pair.Key, pair.Value);
			}
			
			writer.Flush();
		}

		/// <summary>Prints a list of results to the specified stream.</summary>
		/// <param name="writer">An output stream.</param>
		/// <param name="results">A list of benchmark results.</param>
		/// <param name="separator">Separator between table columns.</param>
		/// <param name="addPadding">If true, columns are space-padded to equal width.</param>
		/// <param name="userDataFormatter">A function that converts a list of
		/// user-defined data to a string, or null for the default behavior, which
		/// converts the data to strings and concatenates them.</param>
		/// <param name="userDataColumnName">Name of user-defined data column.</param>
		public static void PrintResults(TextWriter writer, List<KeyValuePair<string, BenchmarkStatistic>> results, string separator, bool addPadding, Func<List<object>, string> userDataFormatter, string userDataColumnName)
		{
			// Prepare a list of columns
			var columns = new List<ColInfo>();
			columns.Add(new ColInfo("Test name", p => p.Key));
			columns.Add(new ColInfo("#", p => p.Value.Count.ToString()));
			if (results.Any(p => p.Value.Count >= 2))
				columns.Add(new ColInfo("Average", p => p.Value.Avg().ToString("0.000")));
			columns.Add(new ColInfo("First", p => p.Value.First.ToString("0.000")));
			if (results.Any(p => p.Value.Count >= 3))
			{
				columns.Add(new ColInfo("Max", p => p.Value.Max.ToString("0.000")));
				columns.Add(new ColInfo("Min", p => p.Value.Min.ToString("0.000")));
				columns.Add(new ColInfo("Std.Dev", p => p.Value.StdDeviation().ToString("0.000")));
			}
			if (results.Any(p => p.Value.UserData != null)) {
				if (userDataFormatter == null)
					userDataFormatter = list => list == null ? "" : 
						string.Join(separator, (from item in list 
						                        where item != null 
						                        select item.ToString()).ToArray());

				columns.Add(new ColInfo(userDataColumnName, p => userDataFormatter(p.Value.UserData)));
			}

			if (addPadding)
				foreach (var col in columns)
					col.Width = Math.Max(col.ColName.Length, results.Max(p => col.Getter(p).Length));

			// Now print the results
			PrintRow(writer, columns, separator, col => col.ColName);
			foreach (var result in results)
				PrintRow(writer, columns, separator, col => col.Getter(result));
		}

		private static void PrintRow(TextWriter writer, List<ColInfo> columns, string separator, Func<ColInfo, string> getter)
		{
			for (int i = 0; i < columns.Count; i++)
			{
				if (i > 0)
					writer.Write(separator);
				var col = columns[i];
				string text = getter(col);
				
				bool hasLetters = text.Any(c => char.IsLetter(c));
				if (!hasLetters)
					text = text.PadLeft(col.Width);
				else if (i != columns.Count - 1)
					text = text.PadRight(col.Width);
				
				writer.Write(text);
			}
			writer.WriteLine();
		}

		class ColInfo
		{
			public ColInfo(string name, GetColumn getter) { ColName = name; Getter = getter; }
			public string ColName;
			public GetColumn Getter;
			public int Width;
		}

		delegate string GetColumn(KeyValuePair<string, BenchmarkStatistic> result);
		static KeyValuePair<K, V> Pair<K, V>(K k, V v) { return new KeyValuePair<K, V>(k, v); }
	}
	
	[AttributeUsage(AttributeTargets.Method)]
	public class BenchmarkAttribute : Attribute
	{
		public readonly string Name;
		public int Trials = -1;

		public BenchmarkAttribute() { }
		public BenchmarkAttribute(int defaultTrialCount) { Trials = defaultTrialCount; }
		public BenchmarkAttribute(string name) { Name = name; }
		public BenchmarkAttribute(string name, int defaultTrialCount) { Name = name; Trials = defaultTrialCount; }
	}

	public class BenchmarkStatistic : Statistic
	{
		public int Errors; // Number of trials that ended in an exception
		public double First;
		public double Last;
		public List<object> UserData;
		
		public new void Clear()
		{
			base.Clear();
			First = Last = Errors = 0;
			UserData = null;
		}
		public new void Add(double nextValue) { Add(nextValue, null); }
		public void Add(double nextValue, object userDatum)
		{
			Last = nextValue;
			if (Count == 0)
				First = nextValue;

			if (userDatum != null)
			{
				if (UserData == null)
					UserData = new List<object>();
				if (!UserData.Contains(userDatum))
					UserData.Add(userDatum);
			}

			if (double.IsNaN(nextValue) || double.IsInfinity(nextValue))
				Errors++;
			else
				base.Add(nextValue);
		}
	}
}
