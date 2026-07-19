using System.Text.Json;
using System.Text.Json.Serialization;

namespace Benchmark
{
	public enum JobStatus { Queued, Running, Completed, Failed, Canceled }

	/// <summary>One sub-benchmark that failed during an otherwise-successful run (e.g. a
	/// single serializer that threw or whose round-trip didn't match), so it was
	/// excluded from the charts. Surfaced prominently in the UI so a silently-missing
	/// series doesn't go unnoticed.</summary>
	public class SubBenchmarkFailure
	{
		/// <summary>The serializer/series that failed, e.g. "protobuf-net".</summary>
		public string Series { get; set; } = "";
		/// <summary>The case/parameter it failed on, e.g. "100 entries".</summary>
		public string Case { get; set; } = "";
		/// <summary>Short, one-line reason (exception summary or mismatch description).</summary>
		public string Reason { get; set; } = "";
		/// <summary>Full details when available — typically an exception's ToString()
		/// (message + stack trace). Null for a validation mismatch with no exception.</summary>
		public string? Details { get; set; }
	}

	/// <summary>Result of one benchmark run: measurements, chart configs, and log output.</summary>
	public class BenchmarkRunResult
	{
		public string Path { get; set; } = "";
		public DateTime StartedUtc { get; set; }
		public double ElapsedSeconds { get; set; }
		public string? Error { get; set; }
		public List<string> LogLines { get; set; } = new();
		public List<EzDataPoint> Points { get; set; } = new();
		public Dictionary<string, GraphModel> Graphs { get; set; } = new();
		/// <summary>Sub-benchmarks that failed but did not abort the whole run.</summary>
		public List<SubBenchmarkFailure> Failures { get; set; } = new();

		[JsonIgnore] public double ProgressFraction { get; set; }
		[JsonIgnore] public string? ProgressNote { get; set; }

		/// <summary>Deep-copies the collections so the UI/persistence can read a
		/// stable snapshot while the benchmark thread keeps appending.</summary>
		public BenchmarkRunResult Snapshot()
		{
			lock (this)
				return new BenchmarkRunResult {
					Path = Path, StartedUtc = StartedUtc, ElapsedSeconds = ElapsedSeconds, Error = Error,
					LogLines = new List<string>(LogLines),
					Points = new List<EzDataPoint>(Points),
					Graphs = new Dictionary<string, GraphModel>(Graphs),
					Failures = new List<SubBenchmarkFailure>(Failures),
					ProgressFraction = ProgressFraction, ProgressNote = ProgressNote,
				};
		}
	}

	public class BenchmarkJob
	{
		public BenchmarkJob(BenchmarkLeaf leaf) => Leaf = leaf;
		public Guid Id { get; } = Guid.NewGuid();
		public BenchmarkLeaf Leaf { get; }
		public JobStatus Status { get; set; } = JobStatus.Queued;
		public BenchmarkRunResult Result { get; } = new();
	}

	/// <summary>Singleton service that owns the benchmark queue. Benchmarks are
	/// executed strictly one at a time on a dedicated high-priority worker thread
	/// (running two at once would corrupt both measurements). Completed results
	/// are kept per benchmark path and persisted to disk as JSON.</summary>
	public class BenchmarkQueueService : IDisposable
	{
		public BenchmarkQueueService(BenchmarkRegistry registry, string resultsDirectory)
		{
			Registry = registry;
			_resultsDir = resultsDirectory;
			Directory.CreateDirectory(_resultsDir);
			LoadPersistedResults();

			_worker = new Thread(WorkerLoop) {
				Name = "Benchmark runner", IsBackground = true, Priority = ThreadPriority.AboveNormal
			};
			_worker.Start();
		}

		public BenchmarkRegistry Registry { get; }
		readonly string _resultsDir;
		readonly Thread _worker;
		readonly object _lock = new();
		readonly List<BenchmarkJob> _jobs = new();          // queue + history for this session
		readonly Dictionary<string, BenchmarkRunResult> _latestResults = new(); // by benchmark path
		readonly SemaphoreSlim _workAvailable = new(0);
		CancellationTokenSource? _currentJobCts;
		BenchmarkJob? _currentJob;
		volatile bool _disposed;

		/// <summary>Raised (on a worker thread) whenever the queue or a running job changes.</summary>
		public event Action? Changed;
		void NotifyChanged() => Changed?.Invoke();

		public IReadOnlyList<BenchmarkJob> Jobs { get { lock (_lock) return _jobs.ToList(); } }
		public BenchmarkJob? CurrentJob => _currentJob;

		public BenchmarkRunResult? FindLatestResult(string path)
		{
			lock (_lock) {
				// Prefer a live/completed job result from this session over a persisted one
				var job = _jobs.LastOrDefault(j => j.Leaf.Path == path
					&& j.Status is JobStatus.Running or JobStatus.Completed);
				if (job != null)
					return job.Result;
				return _latestResults.TryGetValue(path, out var r) ? r : null;
			}
		}

		public int QueuedCount { get { lock (_lock) return _jobs.Count(j => j.Status == JobStatus.Queued); } }

		/// <summary>Adds benchmarks to the queue (skipping any already queued).</summary>
		public int Enqueue(IEnumerable<string> paths)
		{
			int added = 0;
			lock (_lock) {
				var alreadyQueued = _jobs.Where(j => j.Status == JobStatus.Queued).Select(j => j.Leaf.Path).ToHashSet();
				foreach (var path in paths) {
					var leaf = Registry.Find(path);
					if (leaf != null && alreadyQueued.Add(path)) {
						_jobs.Add(new BenchmarkJob(leaf));
						added++;
					}
				}
			}
			for (int i = 0; i < added; i++)
				_workAvailable.Release();
			if (added > 0)
				NotifyChanged();
			return added;
		}

		/// <summary>Cancels the running job (if any); queued jobs continue afterward.</summary>
		public void CancelCurrent() => _currentJobCts?.Cancel();

		/// <summary>Removes all queued jobs, and optionally cancels the running one too.</summary>
		public void CancelAll(bool includingCurrent = true)
		{
			lock (_lock) {
				foreach (var job in _jobs.Where(j => j.Status == JobStatus.Queued))
					job.Status = JobStatus.Canceled;
			}
			if (includingCurrent)
				_currentJobCts?.Cancel();
			NotifyChanged();
		}

		/// <summary>Removes finished jobs from the session job list.</summary>
		public void ClearFinished()
		{
			lock (_lock)
				_jobs.RemoveAll(j => j.Status is JobStatus.Completed or JobStatus.Failed or JobStatus.Canceled);
			NotifyChanged();
		}

		void WorkerLoop()
		{
			while (!_disposed) {
				try {
					_workAvailable.Wait();
				} catch (ObjectDisposedException) {
					return;
				}
				BenchmarkJob? job;
				lock (_lock) {
					job = _jobs.FirstOrDefault(j => j.Status == JobStatus.Queued);
					if (job == null)
						continue; // canceled while queued
					job.Status = JobStatus.Running;
					_currentJob = job;
					_currentJobCts = new CancellationTokenSource();
				}
				RunJob(job, _currentJobCts!.Token);
				lock (_lock) {
					_currentJob = null;
					_currentJobCts.Dispose();
					_currentJobCts = null;
				}
				NotifyChanged();
			}
		}

		void RunJob(BenchmarkJob job, CancellationToken ct)
		{
			var result = job.Result;
			result.Path = job.Leaf.Path;
			result.StartedUtc = DateTime.UtcNow;
			var context = new BenchmarkContext(result, ct, NotifyChanged);
			var sw = System.Diagnostics.Stopwatch.StartNew();
			var oldPriority = Thread.CurrentThread.Priority;
			try {
				Thread.CurrentThread.Priority = ThreadPriority.Highest;
				NotifyChanged();
				job.Leaf.Run(context);
				job.Status = JobStatus.Completed;
			} catch (OperationCanceledException) {
				job.Status = JobStatus.Canceled;
				context.Log("*** Canceled by user ***");
			} catch (Exception ex) {
				job.Status = JobStatus.Failed;
				result.Error = ex.ToString();
				context.Log("*** FAILED: " + ex.Message + " ***");
			} finally {
				Thread.CurrentThread.Priority = oldPriority;
				sw.Stop();
				result.ElapsedSeconds = sw.Elapsed.TotalSeconds;
				result.ProgressFraction = 1;
				MicroBench.CleanGC();
			}

			if (job.Status == JobStatus.Completed) {
				var snapshot = result.Snapshot();
				lock (_lock)
					_latestResults[job.Leaf.Path] = snapshot;
				PersistResult(snapshot);
			}
		}

		#region Persistence (results survive app restarts)

		// IncludeFields: EzDataPoint stores its data in public fields
		static readonly JsonSerializerOptions _jsonOptions = new() {
			WriteIndented = true,
			IncludeFields = true,
			Converters = { new ObjectToJsonConverter() },
		};

		string GetResultFilePath(string path) => System.IO.Path.Combine(_resultsDir,
			Loyc.G.MakeValidFileName(path.Replace('/', '.')) + ".json");

		void PersistResult(BenchmarkRunResult result)
		{
			try {
				File.WriteAllText(GetResultFilePath(result.Path), JsonSerializer.Serialize(result, _jsonOptions));
			} catch (Exception ex) {
				Console.Error.WriteLine($"Failed to persist result for {result.Path}: {ex.Message}");
			}
		}

		void LoadPersistedResults()
		{
			foreach (var file in Directory.EnumerateFiles(_resultsDir, "*.json")) {
				try {
					var result = JsonSerializer.Deserialize<BenchmarkRunResult>(File.ReadAllText(file), _jsonOptions);
					if (result != null && Registry.Find(result.Path) != null)
						_latestResults[result.Path] = result;
				} catch (Exception ex) {
					Console.Error.WriteLine($"Ignoring unreadable result file {file}: {ex.Message}");
				}
			}
		}

		/// <summary>EzDataPoint.GraphId/Parameter are 'object'; serialize numbers as
		/// numbers and everything else as strings so results round-trip through JSON.</summary>
		class ObjectToJsonConverter : JsonConverter<object>
		{
			public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> reader.TokenType switch {
					JsonTokenType.Number => reader.GetDouble(),
					JsonTokenType.String => reader.GetString(),
					JsonTokenType.True => true,
					JsonTokenType.False => false,
					JsonTokenType.Null => null,
					_ => JsonDocument.ParseValue(ref reader).RootElement.ToString(),
				};
			public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
			{
				if (value is int or long or short or byte)
					writer.WriteNumberValue(Convert.ToInt64(value));
				else if (value is double or float or decimal)
					writer.WriteNumberValue(Convert.ToDouble(value));
				else if (value is bool b)
					writer.WriteBooleanValue(b);
				else
					writer.WriteStringValue(value.ToString());
			}
		}

		#endregion

		public void Dispose()
		{
			_disposed = true;
			_currentJobCts?.Cancel();
			_workAvailable.Dispose();
		}
	}
}
