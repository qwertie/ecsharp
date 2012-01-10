using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Loyc.Math;

namespace MiniTestRunner
{
	public partial class TestingForm : Form
	{
		TaskTreeModel _model;
		OptionsModel Options { get { return _model.Options; } }
		TreeViewAdvModel _tvAdvModel;

		public TestingForm(TaskTreeModel model)
		{
			_model = model;

			InitializeComponent();

			_tvAdvModel = new TreeViewAdvModel(model);
			_tvAdvModel.RowChanged += row => _testTreeView.Invalidate();
			_testTreeView.Model = _tvAdvModel;
			
			Options.PropertyChanged += Options_PropertyChanged;
			Options_PropertyChanged(Options, null);
		}

		#region File menu

		private void menuOpenAssembly_Click(object sender, EventArgs e)
		{
			string[] filenames = ShowOpenAssembliesDialog();
			if (filenames != null) {
				_model.RemoveAllAssemblies();
				_model.BeginOpenAssemblies(filenames, Options.PartialTrust);
			}
		}
		private void menuAddAssembly_Click(object sender, EventArgs e)
		{
			_model.BeginOpenAssemblies(ShowOpenAssembliesDialog(), Options.PartialTrust);
		}
		private string[] ShowOpenAssembliesDialog()
		{
			return ShowOpenDialog(true, "Open Assembly", "Assemblies (*.dll;*.exe)|*.dll;*.exe");
		}
		private string[] ShowOpenDialog(bool multiselect, string title, string defaultFilter)
		{
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Multiselect = multiselect;
			dlg.ShowReadOnly = false;
			dlg.Title = title;
			dlg.Filter = defaultFilter + "|All files (*.*)|*.*";
			if (dlg.ShowDialog(this) == DialogResult.OK)
				return dlg.FileNames;
			else
				return null;
		}

		private void menuClearTree_Click(object sender, EventArgs e)
		{
			_model.Clear();
		}

		private void menuLoadProject_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		private void menuSaveProject_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		private void menuLoadResultSet_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		private void menuSaveResultSet_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		private void menuExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Options menu

		private void menuLoadLastProjectOnStartup_Click(object sender, EventArgs e)
		{
			Options.LoadLastProjectOnStartup = !Options.LoadLastProjectOnStartup;
			MessageBox.Show("TODO");
		}
		private void menuRunTestsOnLoad_Click(object sender, EventArgs e)
		{
			Options.RunTestsOnLoad = !Options.RunTestsOnLoad;
			MessageBox.Show("TODO");
		}
		private void menuRunTestsOnChange_Click(object sender, EventArgs e)
		{
			Options.RunTestsOnChange = !Options.RunTestsOnChange;
			MessageBox.Show("TODO");
		}
		private void menuAutoUnload_Click(object sender, EventArgs e)
		{
			Options.AutoUnload = !Options.AutoUnload;
			MessageBox.Show("TODO");
		}
		private void menuPartialTrust_Click(object sender, EventArgs e)
		{
			Options.PartialTrust = !Options.PartialTrust;
		}
		private void menuAlwaysOnTop_Click(object sender, EventArgs e)
		{
			Options.AlwaysOnTop = !Options.AlwaysOnTop;
		}

		private void menuResetAllPriorities_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}
		private void menuReEnableAllTests_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		int ExtractNumberAtStart(string text, int @default)
		{
			int num;
			if (!int.TryParse(text.Substring(0, text.IndexOf(' ') + 1), out num))
				return @default;
			return num;
		}

		private void menuMultithreading_Click(object sender, EventArgs e)
		{
			string text = (sender as ToolStripItem).Text;
			Options.ThreadLimit = ExtractNumberAtStart(text, 1);
		}

		private void btnSplitHorizontally_Click(object sender, EventArgs e)
		{
			Options.SplitHorizontally = !Options.SplitHorizontally;
		}

		private void menuHideOutputPane_Click(object sender, EventArgs e)
		{
			Options.OutputPaneCollapsed = !Options.OutputPaneCollapsed;
		}

		private void btnWordWrap_Click(object sender, EventArgs e)
		{
			Options.OutputWordWrap = !Options.OutputWordWrap;
		}

		void Options_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			menuLoadLastProjectOnStartup.Checked = Options.LoadLastProjectOnStartup;
			menuRunTestsOnLoad.Checked = Options.RunTestsOnLoad;
			menuRunTestsOnChange.Checked = Options.RunTestsOnChange;
			menuAutoUnload.Checked = Options.AutoUnload;
			menuPartialTrust.Checked = Options.PartialTrust;
			menuAlwaysOnTop.Checked = Options.AlwaysOnTop;
			SetAlwaysOnTop(this, null);

			foreach (ToolStripMenuItem child in menuMultithreading.DropDownItems)
				child.Checked = (Options.ThreadLimit == ExtractNumberAtStart(child.Text, 1));

			btnSplitHorizontally.Enabled = !Options.OutputPaneCollapsed;
			btnSplitHorizontally.Checked = Options.SplitHorizontally;
			SetOrientation(splitContainer, Options.SplitHorizontally);
			btnHideOutputPane.Checked = Options.OutputPaneCollapsed;
			menuHideOutputPane.Checked = Options.OutputPaneCollapsed;
			splitContainer.Panel2Collapsed = Options.OutputPaneCollapsed;
			btnOutputWordWrap.Checked = Options.OutputWordWrap;
		}

		private void SetOrientation(SplitContainer container, bool horizontal)
		{
			// The terminology here is reversed with respect to WinForms. To me, 
			// side-by-side panes (|__|__|) are "horizontally" oriented and stacked 
			// panes (|----|) are "vertically" oriented.
			// 
			// When we change the orientation of a SplitContainer, it doesn't split 
			// proportionally as it should, instead it simply keeps the "size" of the
			// first half constant (e.g. if the container was split vertically, then
			// when we switch to horizontal split, the first half's old height 
			// becomes the first half's new width.) This is not usually what one 
			// wants, instead we would like to keep the proportions equal (if the 
			// first half used 2/3 of the space before, it should use 2/3 
			// afterward.)
			var o = horizontal ? Orientation.Vertical : Orientation.Horizontal;
			if (container.Orientation == o)
				return;
			
			double frac = (double)container.SplitterDistance / (horizontal ? container.Height : container.Width);
			frac = MathEx.InRange(frac, 0.01, 0.99);
			container.Orientation = o;
			container.SplitterDistance = (int)(frac * (horizontal ? container.Width : container.Height));
		}

		private void SetAlwaysOnTop(object sender, EventArgs e)
		{
			bool top = Options.AlwaysOnTop && WindowState != FormWindowState.Maximized;
			//FormBorderStyle = top ? FormBorderStyle.SizableToolWindow : FormBorderStyle.Sizable;
			TopMost = top;
		}
 
		#endregion

		#region Items that are only on the toolbar

		private void txtFilter_TextChanged(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}
		private void btnCopy_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}
		private void btnRunTests_Click(object sender, EventArgs e)
		{
			_model.StartTesting();
		}
		private void btnStopTests_Click(object sender, EventArgs e)
		{
			MessageBox.Show("TODO");
		}

		#endregion
	}
}
