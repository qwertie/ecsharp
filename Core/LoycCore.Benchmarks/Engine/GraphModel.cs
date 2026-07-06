namespace Benchmark
{
	/// <summary>Display settings for one chart (all <see cref="EzDataPoint"/>s that
	/// share a GraphId). This replaces the OxyPlot PlotModel configuration hooks in
	/// the old app with a small, chart-library-agnostic model.</summary>
	public class GraphModel
	{
		public GraphModel(string id) => Id = id;

		public string Id { get; set; }
		public string Title { get; set; } = "";
		public string XAxisTitle { get; set; } = "";
		public string YAxisTitle { get; set; } = "";
		/// <summary>Y-axis unit suffix shown in tooltips/tables, e.g. "µs" or "bytes".</summary>
		public string YUnit { get; set; } = "";
		public bool YLogScale { get; set; }
		/// <summary>True if this is a grouped, stacked bar chart: each entity (e.g.
		/// serializer) gets one bar per category, split into stacked segments (e.g.
		/// write + read) that share a group. Series in the same group are stacked;
		/// different groups sit side by side. See <see cref="EzDataPoint"/>.</summary>
		public bool Stacked { get; set; }
		/// <summary>True if numeric X values are log-spaced (e.g. list sizes 30, 100,
		/// 300 …). The chart then places the X values as evenly-spaced categories,
		/// which is equivalent to a log-scale X axis for log-spaced data.</summary>
		public bool XLogScale { get; set; }
		public double? YMin { get; set; }
		public double? YMax { get; set; }
		/// <summary>Sort order of graphs within a benchmark's result page.</summary>
		public int Order { get; set; }
	}
}
