using System;

namespace Benchmark
{
	/// <summary>A single measurement produced by a benchmark. This type keeps the
	/// shape it had in the old OxyPlot-based app (GraphId/Series/Parameter/Value)
	/// so that legacy benchmarks port over unchanged; the web UI groups points by
	/// GraphId to build one chart per graph.</summary>
	/// <remarks>If <see cref="Parameter"/> is a string, the chart uses a category
	/// (bar chart) X axis; if it is numeric, the chart uses a line series.
	/// Two points with the same (GraphId, Series, Parameter) are considered the
	/// same point; adding a duplicate overwrites the earlier value.</remarks>
	public class EzDataPoint : IEquatable<EzDataPoint>, IComparable<EzDataPoint>
	{
		public object GraphId = ""; // Usually a string or Symbol
		public string Series = "";
		public object? Parameter; // X
		public double Value;      // Y or bar size

		public override bool Equals(object? obj) => obj is EzDataPoint p && Equals(p);
		// This definition allows a data point in a set to be updated with a new result
		public bool Equals(EzDataPoint? other)
		{
			return other != null
				&& Series == other.Series
				&& object.Equals(GraphId, other.GraphId)
				&& object.Equals(Parameter, other.Parameter);
		}
		public override int GetHashCode()
			=> ((GraphId ?? "").GetHashCode() + Series.GetHashCode()) ^ (Parameter ?? "").GetHashCode();

		public EzDataPoint Clone() => (EzDataPoint)MemberwiseClone();

		public int CompareTo(EzDataPoint? other)
		{
			if (other == null) return 1;
			var comp = System.Collections.Comparer.Default;
			int c;
			if ((c = string.CompareOrdinal(GraphId?.ToString(), other.GraphId?.ToString())) == 0)
				if ((c = string.CompareOrdinal(Series, other.Series)) == 0)
					c = CompareParams(Parameter, other.Parameter);
			return c;
		}
		static int CompareParams(object? a, object? b)
		{
			if (a is string sa)
				return b is string sb ? string.CompareOrdinal(sa, sb) : 1;
			if (b is string) return -1;
			if (a == null) return b == null ? 0 : -1;
			if (b == null) return 1;
			return Convert.ToDouble(a).CompareTo(Convert.ToDouble(b));
		}
	}
}
