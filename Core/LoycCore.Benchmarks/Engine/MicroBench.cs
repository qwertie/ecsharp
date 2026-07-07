using System.Diagnostics;

namespace Benchmark
{
	/// <summary>Statistics for one benchmarked operation.</summary>
	public record BenchStats(
		double MedianUs, double MeanUs, double MinUs, double MaxUs, double StdDevUs,
		double AllocBytesPerOp, int Trials, long OpsPerTrial)
	{
		public override string ToString() =>
			$"median {MedianUs:0.00###} µs/op (mean {MeanUs:0.00###}, min {MinUs:0.00###}, " +
			$"max {MaxUs:0.00###}, σ {StdDevUs:0.00###}, alloc {AllocBytesPerOp:n0} B/op, " +
			$"{Trials}×{OpsPerTrial:n0} ops)";
	}

	/// <summary>A small micro-benchmark harness: an unmeasured warm-up period,
	/// automatic inner-loop sizing so each trial runs long enough to time
	/// accurately, several trials, and per-operation allocation tracking.</summary>
	public static class MicroBench
	{
		/// <summary>Measures the time and allocations of <c>op</c>. Prefer
		/// <see cref="BenchStats.MedianUs"/> when charting: a single trial disturbed
		/// by the GC or the OS scheduler skews the mean but not the median.</summary>
		/// <param name="op">The operation to measure.</param>
		/// <param name="ct">Checked between trials; throws OperationCanceledException.</param>
		/// <param name="targetTrialMs">Inner-loop count is chosen so one trial lasts
		/// about this long.</param>
		/// <param name="trials">Number of measured trials (after 2 unrecorded trials).</param>
		/// <param name="warmupMs">Minimum duration of the unmeasured warm-up. This is
		/// deliberately much longer than a single trial: .NET's tiered JIT only promotes
		/// a hot method to fully-optimized (tier-1) code after it has been called many
		/// times, and the recompilation happens asynchronously on a background thread.
		/// A too-short warm-up therefore measures a mix of tier-0/instrumented code and
		/// makes heavily-generic call trees (like SyncLib's) look far slower than their
		/// steady state. Warming for ~1/4 s gives promotion time to land and then
		/// exercises the optimized code before timing begins.</param>
		public static BenchStats Measure(Action op, CancellationToken ct,
			double targetTrialMs = 50, int trials = 8, long maxOpsPerTrial = 10_000_000,
			double warmupMs = 250)
		{
			// Absorb one-time costs (JIT, static initialization) before estimating
			op();

			// Warm-up period, not measured: settle the tiered JIT (see warmupMs), and
			// bring caches, branch predictors and the allocator to steady state, while
			// estimating the cost of one op. The run-count cap is only a safety net for
			// pathologically cheap ops; the intent is to warm for the full warmupMs.
			var sw = Stopwatch.StartNew();
			long warmupRuns = 0;
			do {
				op();
				warmupRuns++;
			} while (sw.Elapsed.TotalMilliseconds < warmupMs && warmupRuns < 200_000_000);
			double estMs = Math.Max(sw.Elapsed.TotalMilliseconds / warmupRuns, 0.000_001);
			long opsPerTrial = (long)Math.Clamp(targetTrialMs / estMs, 1, maxOpsPerTrial);

			CleanGC();

			var timesUs = new double[trials];
			double allocPerOp = 0;
			for (int t = -2; t < trials; t++) { // two more unrecorded trials, then the real ones
				ct.ThrowIfCancellationRequested();
				long allocBefore = GC.GetAllocatedBytesForCurrentThread();
				sw.Restart();
				for (long i = 0; i < opsPerTrial; i++)
					op();
				sw.Stop();
				long allocAfter = GC.GetAllocatedBytesForCurrentThread();
				if (t >= 0) {
					timesUs[t] = sw.Elapsed.TotalMilliseconds * 1000 / opsPerTrial;
					allocPerOp += (double)(allocAfter - allocBefore) / opsPerTrial / trials;
				}
			}

			double mean = timesUs.Average();
			double variance = timesUs.Length > 1
				? timesUs.Sum(x => (x - mean) * (x - mean)) / (timesUs.Length - 1) : 0;
			return new BenchStats(GetMedian(timesUs), mean, timesUs.Min(), timesUs.Max(),
				Math.Sqrt(variance), allocPerOp, trials, opsPerTrial);
		}

		/// <summary>Garbage-collects so earlier benchmarks don't pay their GC debt
		/// in a later one.</summary>
		public static void CleanGC()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		static double GetMedian(double[] values)
		{
			var sorted = values.Order().ToArray();
			int mid = sorted.Length / 2;
			return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
		}
	}
}
