namespace Benchmark.Serialization
{
	/// <summary>Which half of a round trip is being measured.</summary>
	public enum SerializationOp { Serialize, Deserialize }

	/// <summary>Base class so differently-typed scenarios can share one benchmark leaf.</summary>
	public abstract class ScenarioBase
	{
		public string Name = "";
		public string Description = "";
		/// <summary>Graph-id prefix; several scenarios can share one graph group so
		/// their cases become extra categories on the same charts.</summary>
		public string GraphGroup = "";
		public string XAxisTitle = "";
		/// <summary>Milliseconds each timing trial should last (see MicroBench).</summary>
		public double TargetTrialMs = 25;
		public int Trials = 6;

		public abstract int CaseCount { get; }
		public abstract int AdapterCount { get; }
		public abstract void RunCase(BenchmarkContext ctx, int caseIndex, SerializationOp op,
			Action<string> progressNote);

		// All serialization charts are grouped, stacked bar charts: the X axis is the
		// set of case labels; each serializer emits a "write" and a "read" series that
		// share a group (so they stack into one round-trip bar per category). Values
		// are normalized per item so the Y axis stays flat as the payload grows.
		protected GraphModel MakeTimeGraph() => new GraphModel(GraphGroup + ": round-trip time") {
			Title = GraphGroup + ": round-trip time (write + read)",
			XAxisTitle = XAxisTitle,
			YAxisTitle = "Nanoseconds per item",
			YUnit = "ns",
			Stacked = true,
			Order = 0,
		};
		protected GraphModel MakeAllocGraph() => new GraphModel(GraphGroup + ": allocations") {
			Title = GraphGroup + ": allocations (write + read)",
			XAxisTitle = XAxisTitle,
			YAxisTitle = "Bytes per item",
			YUnit = "B",
			Stacked = true,
			Order = 10,
		};
		protected GraphModel MakeSizeGraph() => new GraphModel(GraphGroup + ": payload size") {
			Title = GraphGroup + ": payload size",
			XAxisTitle = XAxisTitle,
			YAxisTitle = "Bytes per item",
			YUnit = "B",
			Order = 20,
		};
	}

	/// <summary>A serialization benchmark scenario: one data model (T), a set of test
	/// cases (payload sizes/shapes each with an item count for per-item normalization),
	/// and the serializers to compare. Produces timing, allocation, and payload-size
	/// measurements for each combination.</summary>
	public class Scenario<T> : ScenarioBase
	{
		public Scenario(string name, params (string label, int items, Func<T> data)[] cases)
		{
			Name = GraphGroup = name;
			Cases = cases;
		}

		/// <summary>Each case: a category label, the number of items it holds (the
		/// divisor for per-item metrics — use 1 for a single indivisible object), and
		/// a factory for the data.</summary>
		public (string Label, int Items, Func<T> Data)[] Cases;
		public List<SerializerAdapter<T>> Adapters = new();
		/// <summary>Compares original and round-tripped data; returns an error
		/// message, or null if they match. Timing is skipped when validation fails —
		/// a serializer that produces wrong results has no meaningful speed.</summary>
		public Func<T, T?, string?>? Validate;

		public override int CaseCount => Cases.Length;
		public override int AdapterCount => Adapters.Count;

		public override void RunCase(BenchmarkContext ctx, int caseIndex, SerializationOp op,
			Action<string> progressNote)
		{
			var (label, items, dataFactory) = Cases[caseIndex];
			T data = dataFactory();
			double divisor = Math.Max(items, 1);

			GraphModel timeGraph = MakeTimeGraph(), allocGraph = MakeAllocGraph(), sizeGraph = MakeSizeGraph();
			ctx.ConfigureGraph(timeGraph);
			ctx.ConfigureGraph(allocGraph);
			if (op == SerializationOp.Serialize)
				ctx.ConfigureGraph(sizeGraph);

			foreach (var adapter in Adapters) {
				ctx.CancellationToken.ThrowIfCancellationRequested();
				progressNote($"{Name} · {label} · {adapter.Name}");

				object? payload = ValidateRoundTrip(ctx, adapter, data, label);
				if (payload != null) {
					Action operation = op == SerializationOp.Serialize
						? () => adapter.Serialize(data)
						: () => adapter.Deserialize(payload);
					BenchStats stats = MicroBench.Measure(operation, ctx.CancellationToken, TargetTrialMs, Trials);

					int size = SerializerAdapter<T>.GetPayloadSize(payload);
					ctx.Log($"  {adapter.Name} [{label}] {op}: {stats}" +
						(op == SerializationOp.Serialize ? $", {size:n0} bytes" : ""));

					// Write and read are separate stacked segments of the same serializer's
					// bar. The median is charted — one GC-disturbed trial shouldn't show
					// up as a mysteriously tall bar.
					string series = op == SerializationOp.Serialize ? adapter.WriteLabel : adapter.ReadLabel;
					Emit(ctx, timeGraph.Id, series, label, stats.MedianUs * 1000 / divisor);
					Emit(ctx, allocGraph.Id, series, label, Math.Max(stats.AllocBytesPerOp, 0) / divisor);
					// Payload size is a property of the bytes, not of the operation
					if (op == SerializationOp.Serialize)
						Emit(ctx, sizeGraph.Id, adapter.Name, label, size / divisor);
				}
			}
		}

		/// <summary>Serializes and deserializes the data once, before any timing, to
		/// prove the adapter handles it correctly. Returns the serialized payload, or
		/// null (after logging the reason) if the adapter failed or the round-tripped
		/// data didn't match.</summary>
		object? ValidateRoundTrip(BenchmarkContext ctx, SerializerAdapter<T> adapter, T data, string label)
		{
			try {
				object payload = adapter.Serialize(data);
				var back = adapter.Deserialize(payload);
				string? error = Validate?.Invoke(data, back);
				if (error != null) {
					ctx.ReportFailure(adapter.Name, label, "round-trip mismatch: " + error);
					return null;
				}
				return payload;
			} catch (Exception ex) {
				ctx.ReportFailure(adapter.Name, label,
					$"{ex.GetType().Name}: {GetFirstLine(ex.Message)}", ex.ToString());
				return null;
			}
		}

		static void Emit(BenchmarkContext ctx, string graphId, string series, string caseLabel, double value)
			=> ctx.Add(new EzDataPoint { GraphId = graphId, Series = series, Parameter = caseLabel, Value = value });

		/// <summary>Truncates a (possibly multi-line) exception message to its first line.</summary>
		static string GetFirstLine(string s)
		{
			int i = s.IndexOf('\n');
			return i < 0 ? s : s.Substring(0, i).TrimEnd('\r') + " […]";
		}
	}

	/// <summary>Registers a single benchmark leaf for a group of scenarios that run
	/// together and share charts. The leaf runs the serialize pass then the
	/// deserialize pass over every scenario/case, round-trip-validating each
	/// serializer before timing it.</summary>
	public static class ScenarioRegistration
	{
		public static void AddScenarios(this BenchmarkRegistry registry, string path,
			string description, params ScenarioBase[] scenarios)
		{
			registry.Add(path, ctx => {
				var ops = new[] { SerializationOp.Serialize, SerializationOp.Deserialize };
				int totalUnits = ops.Length * scenarios.Sum(s => s.CaseCount * Math.Max(s.AdapterCount, 1));
				int unit = 0;
				foreach (var op in ops) {
					foreach (var scenario in scenarios) {
						ctx.Log($"=== {scenario.Name}: {op} ===");
						for (int i = 0; i < scenario.CaseCount; i++) {
							int unitsBefore = unit;
							scenario.RunCase(ctx, i, op,
								note => ctx.Progress((double)unitsBefore / totalUnits, note));
							unit += Math.Max(scenario.AdapterCount, 1);
							ctx.Progress((double)unit / totalUnits);
						}
					}
				}
			}, description);
		}
	}
}
