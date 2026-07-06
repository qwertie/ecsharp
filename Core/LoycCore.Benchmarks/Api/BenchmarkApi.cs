using System.Text;

namespace Benchmark
{
	/// <summary>A small REST API so benchmarks can also be driven by scripts
	/// (curl) instead of the UI. Endpoints:
	/// <code>
	///   GET  /api/benchmarks             list all benchmark paths
	///   POST /api/queue  {"paths":[..]}  queue benchmarks ("*" queues everything)
	///   GET  /api/status                 queue/job status
	///   POST /api/cancel                 cancel the running job and clear the queue
	///   GET  /api/result?path=...        full result (measurements, graphs, log)
	///   GET  /api/result.csv?path=...    measurements as CSV
	/// </code></summary>
	public static class BenchmarkApi
	{
		public static void Map(WebApplication app)
		{
			app.MapGet("/api/benchmarks", (BenchmarkRegistry registry, BenchmarkQueueService queue) =>
				registry.Leaves.Select(leaf => new {
					path = leaf.Path,
					description = leaf.Description,
					hasResult = queue.FindLatestResult(leaf.Path) != null,
				}));

			app.MapPost("/api/queue",
				(QueueRequest request, BenchmarkRegistry registry, BenchmarkQueueService queue) => {
				var paths = request.Paths ?? new List<string>();
				if (paths.Contains("*"))
					paths = registry.Leaves.Select(l => l.Path).ToList();
				int added = queue.Enqueue(paths);
				return Results.Ok(new { queued = added });
			});

			app.MapGet("/api/status", (BenchmarkQueueService queue) =>
				queue.Jobs.Select(job => new {
					path = job.Leaf.Path,
					status = job.Status.ToString(),
					progress = job.Result.ProgressFraction,
					note = job.Result.ProgressNote,
					elapsedSeconds = job.Result.ElapsedSeconds,
					error = job.Result.Error,
				}));

			app.MapPost("/api/cancel", (BenchmarkQueueService queue) => {
				queue.CancelAll();
				return Results.Ok();
			});

			app.MapGet("/api/result", (string path, BenchmarkQueueService queue) =>
				queue.FindLatestResult(path) is BenchmarkRunResult result
					? Results.Ok(result.Snapshot()) : Results.NotFound());

			app.MapGet("/api/result.csv", (string path, BenchmarkQueueService queue) => {
				if (queue.FindLatestResult(path) is not BenchmarkRunResult result)
					return Results.NotFound();
				var snapshot = result.Snapshot();
				var csv = new StringBuilder("Graph,Series,Parameter,Value\n");
				foreach (var point in snapshot.Points)
					csv.Append(Quote(point.GraphId?.ToString())).Append(',')
					   .Append(Quote(point.Series)).Append(',')
					   .Append(Quote(point.Parameter?.ToString())).Append(',')
					   .Append(point.Value.ToString("R")).Append('\n');
				return Results.Text(csv.ToString(), "text/csv");
			});
		}

		record QueueRequest(List<string>? Paths);

		static string Quote(string? s)
		{
			s ??= "";
			return s.Contains(',') || s.Contains('"') || s.Contains('\n')
				? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
		}
	}
}
