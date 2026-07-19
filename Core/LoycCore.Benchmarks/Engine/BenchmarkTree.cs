using Loyc.Collections;

namespace Benchmark
{
	/// <summary>A runnable benchmark (a leaf in the benchmark tree). Its
	/// <see cref="Path"/> ("A/B/C") defines where it appears in the hierarchy.</summary>
	public class BenchmarkLeaf
	{
		public BenchmarkLeaf(string path, Action<BenchmarkContext> run, string description = "",
			double? estimatedSeconds = null)
		{
			Path = path;
			Run = run;
			Description = description;
			EstimatedSeconds = estimatedSeconds;
		}

		/// <summary>Slash-separated location in the benchmark hierarchy, e.g.
		/// "Serialization/Calendar/Serialize". Must be unique.</summary>
		public string Path { get; }
		public string Name => Path.Substring(Path.LastIndexOf('/') + 1);
		public string Description { get; }
		/// <summary>Rough duration estimate shown in the UI, if known.</summary>
		public double? EstimatedSeconds { get; }
		public Action<BenchmarkContext> Run { get; }
	}

	/// <summary>A node in the benchmark tree built from leaf paths; used by the UI.</summary>
	public class BenchmarkTreeNode
	{
		public BenchmarkTreeNode(string path) => Path = path;
		public string Path { get; }
		public string Name => Path.Substring(Path.LastIndexOf('/') + 1);
		public BenchmarkLeaf? Leaf { get; set; }
		public List<BenchmarkTreeNode> Children { get; } = new();
		public bool IsLeaf => Leaf != null;

		public IEnumerable<BenchmarkLeaf> DescendantLeaves()
		{
			if (Leaf != null)
				yield return Leaf;
			foreach (var child in Children)
				foreach (var leaf in child.DescendantLeaves())
					yield return leaf;
		}
	}

	/// <summary>Registry of all benchmarks, organized into a tree by their paths.</summary>
	public class BenchmarkRegistry
	{
		readonly List<BenchmarkLeaf> _leaves = new();
		readonly Dictionary<string, BenchmarkLeaf> _byPath = new();
		BenchmarkTreeNode? _root;

		public void Add(BenchmarkLeaf leaf)
		{
			if (!_byPath.TryAdd(leaf.Path, leaf))
				throw new ArgumentException($"Duplicate benchmark path: {leaf.Path}");
			_leaves.Add(leaf);
			_root = null;
		}
		public void Add(string path, Action<BenchmarkContext> run, string description = "",
			double? estimatedSeconds = null)
			=> Add(new BenchmarkLeaf(path, run, description, estimatedSeconds));

		public IReadOnlyList<BenchmarkLeaf> Leaves => _leaves;
		public BenchmarkLeaf? Find(string path) => _byPath.TryGetValue(path, out var leaf) ? leaf : null;

		/// <summary>Root of the tree; its own Path is "" and its children are the top-level groups.</summary>
		public BenchmarkTreeNode Root => _root ??= BuildTree();

		BenchmarkTreeNode BuildTree()
		{
			var root = new BenchmarkTreeNode("");
			var nodes = new Dictionary<string, BenchmarkTreeNode> { [""] = root };

			BenchmarkTreeNode GetNode(string path)
			{
				if (nodes.TryGetValue(path, out var node))
					return node;
				int i = path.LastIndexOf('/');
				var parent = GetNode(i < 0 ? "" : path.Substring(0, i));
				node = new BenchmarkTreeNode(path);
				parent.Children.Add(node);
				nodes.Add(path, node);
				return node;
			}

			foreach (var leaf in _leaves)
				GetNode(leaf.Path).Leaf = leaf;
			return root;
		}
	}

	/// <summary>Passed to a running benchmark: cancellation, logging, progress
	/// reporting, and sinks for data points and graph configuration.</summary>
	public class BenchmarkContext : IAdd<EzDataPoint>
	{
		public BenchmarkContext(BenchmarkRunResult result, CancellationToken ct, Action? changed)
		{
			_result = result;
			CancellationToken = ct;
			_changed = changed;
		}

		readonly BenchmarkRunResult _result;
		readonly Action? _changed;
		public CancellationToken CancellationToken { get; }

		public void Log(string line)
		{
			lock (_result)
				_result.LogLines.Add(line);
			_changed?.Invoke();
		}

		/// <summary>Records a sub-benchmark failure that didn't abort the whole run (e.g.
		/// one serializer threw or produced a bad round-trip and was skipped). It is shown
		/// prominently in the UI — with <paramref name="details"/> (usually a stack trace)
		/// available on demand — and also written to the log. Thread-safe.</summary>
		public void ReportFailure(string series, string @case, string reason, string? details = null)
		{
			lock (_result)
				_result.Failures.Add(new SubBenchmarkFailure {
					Series = series, Case = @case, Reason = reason, Details = details,
				});
			Log($"  ✗ {series} [{@case}]: {reason}");
		}

		/// <summary>Adds or updates a measurement. Thread-safe.</summary>
		public void Add(EzDataPoint point)
		{
			lock (_result) {
				int i = _result.Points.IndexOf(point); // Equals ignores Value
				if (i >= 0)
					_result.Points[i] = point;
				else
					_result.Points.Add(point);
			}
			_changed?.Invoke();
		}

		public void ConfigureGraph(GraphModel model)
		{
			lock (_result)
				_result.Graphs[model.Id] = model;
			_changed?.Invoke();
		}

		/// <summary>Reports progress in [0, 1] with an optional status note. Also acts
		/// as a cancellation point: throws OperationCanceledException if the user has
		/// canceled the job.</summary>
		public void Progress(double fraction, string? note = null)
		{
			_result.ProgressFraction = Math.Clamp(fraction, 0, 1);
			if (note != null)
				_result.ProgressNote = note;
			_changed?.Invoke();
			CancellationToken.ThrowIfCancellationRequested();
		}
	}
}
