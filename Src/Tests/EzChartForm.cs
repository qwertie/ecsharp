using Loyc;
using Loyc.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Benchmark
{
	#if DotNet35 // OxyPlot and ConcurrentDictionary not available in .NET 3.5
	
	public partial class EzChartForm : Form, IAdd<EzDataPoint>
	{
		public EzChartForm(bool example) { InitializeComponent(); }
		public void Add(EzDataPoint point) { } // DUMMY so that project still compiles
		protected override void OnVisibleChanged(EventArgs e) { if (Visible) Close(); }
		private void btnSaveCurrent_Click(object sender, EventArgs e) {}
		private void btnSaveAll_Click(object sender, EventArgs e) {}
	}
	
	#else
	
	using OxyPlot;
	using OxyPlot.Axes;
	using OxyPlot.Series;
	using OxyPlot.WindowsForms;

	/// <summary>Uses the OxyPlot library to produce a series of graphs (one tab 
	/// page per graph) from a set of <see cref="EzDataPoint"/> objects and optional 
	/// <c>OxyPlot.PlotModel</c> objects.</summary>
	/// <remarks>This is super easy to use, just add a series of DataPoint objects
	/// in any order by calling Add() on any thread. The form is refreshed 
	/// automatically as data is added. The data series and X axis are always sorted.
	/// Consider adding PlotModel objects to customize the appearance of the graph(s). .NET 4+ required.</remarks>
	public partial class EzChartForm : Form, IAdd<EzDataPoint>
	{
		public EzChartForm(bool example = false)
		{
			InitializeComponent();

			for (int i = 0; i <= 11; i++)
			{
				Add("Example", "Series A", ((char)('A' + i)).ToString(), i);
				Add("Example", "Series B", i, i * i / 10.0);
			}
		}

		public void Add(object graphId, string series, object parameter, double value)
		{
			Add(new EzDataPoint { GraphId = graphId, Series = series, Parameter = parameter, Value = value });
		}

		// Can be called from any thread
		public void AddGraph(object graphId, PlotModel model)
		{
			Graphs[graphId] = Pair.Create(model, false); // false = no auto-gridlines
			AutoUpdateGraphs();
		}
		// Can be called from any thread
		public void Add(EzDataPoint point)
		{
			var set = GraphData.GetOrAdd(point.GraphId, id => new MSet<EzDataPoint>());
			lock (set)
				set.Add(point, true);
			AutoUpdateGraphs();
		}

		ConcurrentDictionary<object, Pair<PlotModel, bool>> Graphs = new ConcurrentDictionary<object, Pair<PlotModel, bool>>();
		ConcurrentDictionary<object, MSet<EzDataPoint>> GraphData = new ConcurrentDictionary<object, MSet<EzDataPoint>>();
		int _needUpdate;

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			UpdateGraphs();
		}

		void AutoUpdateGraphs()
		{
			if (IsHandleCreated && Interlocked.Exchange(ref _needUpdate, 1) == 0)
				BeginInvoke(new Action(() => {
					_needUpdate = 0;
					UpdateGraphs();
				}));
		}

		void UpdateGraphs()
		{
			// Create/update models
			foreach (var pair in GraphData) {
				var graphAnd = Graphs.GetOrAdd(pair.Key, graphId => Pair.Create(new PlotModel(graphId.ToString()), true));
				PlotModel graph = graphAnd.A;
				bool autoConfig = graphAnd.B;
				lock (pair.Value)
					UpdateGraph(graph, pair.Value, autoConfig);
			}
			// Create/update tabs
			foreach (var pair in Graphs) {
				var page = _tabs.TabPages.Cast<ChartPage>().FirstOrDefault(p => object.Equals(p.GraphId, pair.Key));
				if (page == null) {
					page = new ChartPage(pair.Key, pair.Value.A, pair.Value.A.Title.Left(25));
					_tabs.TabPages.Add(page);
				}
				page.Model = pair.Value.A;
			}
		}
		private void UpdateGraph(PlotModel model, MSet<EzDataPoint> points, bool autoConfig)
		{
			model.Series.Clear();
			var allSeries = new BMultiMap<string, EzDataPoint>();
			foreach (var dp in points)
				allSeries.Add(dp.Series, dp);

			if (autoConfig) {
				// TODO
			}

			// Add text labels to axis if the data uses text Parameters
			CategoryAxis cAxis = null;
			if (points.Any(dp => dp.Parameter is string)) {
				var headings = new SortedSet<string>(points.Select(dp => dp.Parameter as string).WhereNotNull());
				cAxis = model.Axes.OfType<CategoryAxis>().SingleOrDefault();
				if (cAxis == null)
					model.Axes.Add(cAxis = new CategoryAxis());
				foreach (var text in headings)
					cAxis.Labels.Add(text);
				cAxis.Key = "CategoryAxis";
			}
			
			int iSeries = 0;
			foreach (var series in allSeries.Values) {
				if (series.Any(p => p.Parameter is string)) {
					var plotSeries = new ColumnSeries();
					plotSeries.XAxisKey = cAxis.Key;
					foreach (var dp in series)
						plotSeries.Items.Add(new ColumnItem(dp.Value, cAxis.Labels.IndexOf(dp.Parameter)));
					model.Series.Add(plotSeries);
				} else {
					var plotSeries = new LineSeries();
					if (cAxis != null) plotSeries.XAxisKey = cAxis.Key;
					// There are 8 marker types starting at 1, excluding None (0)
					plotSeries.MarkerType = (MarkerType)((iSeries & 7) + 1);
					plotSeries.MarkerSize = 4;
					plotSeries.MarkerFill = plotSeries.Color; 
					foreach (var dp in series)
						plotSeries.Points.Add(new OxyPlot.DataPoint(Convert.ToDouble(dp.Parameter), dp.Value));
					model.Series.Add(plotSeries);
				}
			}
		}

		private void btnSaveCurrent_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		private void btnSaveAll_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}
	}

	class ChartPage : TabPage
	{
		public PlotView Plot;
		public object GraphId;
		public PlotModel Model { get { return Plot.Model; } set { Plot.Model = value; } }
		
		public ChartPage(object graphId, PlotModel model, string tabTitle)
		{
			GraphId = graphId;
			Text = tabTitle;
			Plot = new PlotView { 
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
				Location = new Point(),
				Size = ClientSize,
				Model = model,
				BackColor = Color.White
			};
			Controls.Add(Plot);
		}
	}
	#endif

	/// <summary>A data point used with <see cref="EzChartForm"/>.</summary>
	public class EzDataPoint : IEquatable<EzDataPoint>, IComparable<EzDataPoint>
	{
		public object GraphId = ""; // Usually a string or Symbol
		public string Series;
		public object Parameter; // X
		public double Value;    // Y or bar size

		public override bool Equals(object obj)
		{
			return obj is EzDataPoint && Equals(obj as EzDataPoint);
		}
		// this definition allows a DataPoint in a set to be updated with a new result
		public bool Equals(EzDataPoint other)
		{
			return Series == other.Series
				&& object.Equals(GraphId, other.GraphId)
				&& object.Equals(Parameter, other.Parameter);
		}
		public override int GetHashCode()
		{
			return ((GraphId ?? "").GetHashCode() + Series.GetHashCode()) ^ (Parameter ?? "").GetHashCode();
		}
		public int CompareTo(EzDataPoint other)
		{
			var comp = System.Collections.Comparer.Default;
			int c;
			if ((c = comp.Compare(GraphId, other.GraphId)) == 0)
				if ((c = Series.CompareTo(other.Series)) == 0)
					c = comp.Compare(Parameter, other.Parameter);
			return c;
		}
	}
}
