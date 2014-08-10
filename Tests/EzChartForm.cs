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
	#if DotNet35 
	
	// OxyPlot and ConcurrentDictionary not available in .NET 3.5. 
	// DUMMY IMPLEMENTATION SO THAT THE TESTS PROJECT STILL COMPILES
	public partial class EzChartForm : Form, IAdd<EzDataPoint>
	{
		public EzChartForm(bool example) { InitializeComponent(); }
		public void Add(EzDataPoint point) { } // DUMMY so that project still compiles
		protected override void OnVisibleChanged(EventArgs e) { if (Visible) Close(); }
		private void btnSaveCurrent_Click(object sender, EventArgs e) {}
		private void btnSaveAll_Click(object sender, EventArgs e) {}
		public static EzChartForm StartOnNewThread(bool example = false)
		{
			EzChartForm form = new EzChartForm(example);
			new Thread(() => System.Windows.Forms.Application.Run(form)).Start();
			return form;
		}
	}
	
	#else
	
	using OxyPlot;
	using OxyPlot.Axes;
	using OxyPlot.Series;
	using OxyPlot.WindowsForms;
	
	using System.IO;
	using System.Net;

	/// <summary>Uses the OxyPlot library to produce a series of graphs (one tab 
	/// page per graph) from a set of <see cref="EzDataPoint"/> objects and optional 
	/// <c>OxyPlot.PlotModel</c> objects.</summary>
	/// <remarks>This is super easy to use, just add a series of DataPoint objects
	/// in any order by calling Add() on any thread. The form is refreshed 
	/// automatically as data is added. The data series and X axis are always sorted.
	/// Consider adding PlotModel objects to customize the appearance of the graph(s). .NET 4+ required.</remarks>
	public partial class EzChartForm : Form, IAdd<EzDataPoint>
	{
		public static EzChartForm StartOnNewThread(bool example = false)
		{
			EzChartForm form = new EzChartForm(example);
			Thread thread = new Thread(() => System.Windows.Forms.Application.Run(form));
			thread.SetApartmentState(ApartmentState.STA); // required by SaveFileDialog
			thread.Start();
			return form;
		}

		public EzChartForm(bool example = false)
		{
			InitializeComponent();

			for (int i = 0; i <= 11; i++)
			{
				AddGraph("Example", new PlotModel() {
					Title = "Example!",
					LegendPosition = LegendPosition.TopLeft,
					LegendPlacement = LegendPlacement.Inside,
				});
				Add("Example", "Series A", ((char)('A' + i)).ToString(), i);
				Add("Example", "Series B", i, i * i / 10.0);
				InitDefaultModel = (id, model) => {
					model.LegendPosition = LegendPosition.TopLeft;
				};
			}
		}

		/// <summary>A method that is called to initialize an auto-created model.
		/// The object parameter is the graph ID for which the model was created.</summary>
		public Action<object, PlotModel> InitDefaultModel { get; set; }

		public void Add(object graphId, string series, object parameter, double value)
		{
			Add(new EzDataPoint { GraphId = graphId, Series = series, Parameter = parameter, Value = value });
		}

		// Can be called from any thread
		public void AddGraph(object graphId, PlotModel model)
		{
			Graphs[graphId] = model;
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

		ConcurrentDictionary<object, PlotModel> Graphs = new ConcurrentDictionary<object, PlotModel>();
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
				var graph = Graphs.GetOrAdd(pair.Key, graphId => { 
					var m = new PlotModel { Title = graphId.ToString() };
					if (InitDefaultModel != null) InitDefaultModel(graphId, m);
					return m;
				});
				lock (pair.Value)
					UpdateGraph(graph, pair.Value);
			}
			// Create/update tabs
			foreach (var pair in Graphs) {
				var page = _tabs.TabPages.Cast<ChartPage>().FirstOrDefault(p => object.Equals(p.GraphId, pair.Key));
				if (page == null) {
					page = new ChartPage(pair.Key, pair.Value, pair.Value.Title.Left(25));
					_tabs.TabPages.Add(page);
				}
				page.Model = pair.Value;
			}
		}
		private void UpdateGraph(PlotModel model, MSet<EzDataPoint> points)
		{
			model.Series.Clear();
			var allSeries = new BMultiMap<string, EzDataPoint>();
			foreach (var dp in points)
				allSeries.Add(dp.Series, dp);

			// Add text labels to axis if the data uses text Parameters
			CategoryAxis cAxis = null;
			if (points.Any(dp => dp.Parameter is string)) {
				var headings = new SortedSet<string>(points.Select(dp => dp.Parameter as string).WhereNotNull());
				cAxis = model.Axes.OfType<CategoryAxis>().SingleOrDefault();
				if (cAxis == null)
					model.Axes.Add(cAxis = new CategoryAxis { 
						MajorGridlineStyle = LineStyle.Dot,
						Position = AxisPosition.Bottom
					});
				cAxis.Labels.Clear();
				foreach (var text in headings)
					cAxis.Labels.Add(text);
				cAxis.Key = "CategoryAxis";
			}
			
			int iSeries = 0;
			foreach (var series in allSeries.Values) {
				if (series.Any(p => p.Parameter is string)) {
					var plotSeries = new ColumnSeries { Title = series.First().Series };
					plotSeries.XAxisKey = cAxis.Key;
					foreach (var dp in series)
						plotSeries.Items.Add(new ColumnItem(dp.Value, cAxis.Labels.IndexOf(dp.Parameter)));
					model.Series.Add(plotSeries);
				} else {
					var plotSeries = new LineSeries { Title = series.First().Series };
					if (cAxis != null) plotSeries.XAxisKey = cAxis.Key;
					// There are 7 marker types starting at 1, excluding None (0)
					plotSeries.MarkerType = (MarkerType)((iSeries % 7) + 1);
					plotSeries.MarkerSize = 4;
					plotSeries.MarkerFill = plotSeries.Color; 
					foreach (var dp in series)
						plotSeries.Points.Add(new OxyPlot.DataPoint(Convert.ToDouble(dp.Parameter), dp.Value));
					model.Series.Add(plotSeries);
				}
				iSeries++;
			}
		}

		SaveFileDialog _saveDialog = new SaveFileDialog();

		private void btnSaveCurrent_Click(object sender, EventArgs e)
		{
			var page = (ChartPage)_tabs.SelectedTab;
			if (page != null) {
				_saveDialog.Title = "Save single image";
				_saveDialog.Filter = "PNG image (*.png)|*.png|All Files (*.*)|*.*";
				_saveDialog.FileName = G.MakeValidFileName(page.GraphId.ToString() + ".png");
				if (_saveDialog.ShowDialog() == DialogResult.OK) {
					string filename = _saveDialog.FileName;
					PngExporter.Export(page.Model, filename, page.Plot.Width, page.Plot.Height, Brushes.White);
				}
			}
		}

		private void btnSaveAll_Click(object sender, EventArgs e)
		{
			_saveDialog.Filter = "HTML file (*.html)|*.html|All Files (*.*)|*.*";
			_saveDialog.FileName = "ViewGraphs.html";
			_saveDialog.Title = "Choose folder in which to save PNGs";
			if (_saveDialog.ShowDialog() == DialogResult.OK) {
				string filename = _saveDialog.FileName;
				
				string folder = Path.GetDirectoryName(_saveDialog.FileName);
				using (var w = new StreamWriter(_saveDialog.FileName))
				{
					w.WriteLine("<html>");
					w.WriteLine("<head>");
					w.WriteLine("<title>Graphs ({0})</title>", DateTime.Now);
					w.WriteLine("</head>");
					w.WriteLine("<body>");
					foreach (ChartPage page in _tabs.TabPages) {
						string imgName = Path.Combine(folder, G.MakeValidFileName(page.GraphId.ToString().Replace(' ','-') + ".png"));
						w.WriteLine("<p>{0}</p>", page.GraphId);
						w.WriteLine("<img src=\"{0}\"/><br/>", imgName);
						PngExporter.Export(page.Model, imgName, page.Plot.Width, page.Plot.Height, Brushes.White);
					}
					w.WriteLine("</body>");
					w.WriteLine("</html>");
				}
			}
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
	public class EzDataPoint : IEquatable<EzDataPoint>, IComparable<EzDataPoint>, ICloneable<EzDataPoint>
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
		public EzDataPoint Clone() { return (EzDataPoint)MemberwiseClone(); }
	}
}
