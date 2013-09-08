using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Util.UI;

namespace BoxDiagrams
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
		}

		public static ToolStripDropDown NewPopup(Control contents)
		{
			var strip = new ToolStripDropDown();
			var host = new ToolStripControlHost(contents);
			strip.Items.Add(host);
			host.Margin = new Padding(0);
			return strip;
		}

		private void menuNew_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Not implemented");;
		}
		private void menuSave_Click(object sender, EventArgs e) { Save(); }

		private string _filename;

		[Command(null, "Save diagram")] public void Save()
		{
			if (string.IsNullOrEmpty(_filename))
				SaveAs();
			else
				Save(_filename);
		}

		[Command(null, "Save diagram in a new file")] private void SaveAs()
		{
			var dlg = new SaveFileDialog { Filter = "Baadia diagrams (*.badia)|*.badia|Show all files (*.*)|*.*" };
			if (dlg.ShowDialog() == DialogResult.OK)
				Save(dlg.FileName);
		}

		private void Save(string filename)
		{
			TryCatchWithMessageBox(() => {
				_diagramCtrl.Save(filename);
				_filename = filename;
			}, "Unable to save " + filename);
		}

		private Exception TryCatchWithMessageBox(Action act, string prefix = null, string title = null)
		{
			try {
				act();
				return null;
			} catch (Exception ex) {
				string msg = ex.Message;
				if (prefix != null)
					msg = prefix + "\n\n" + msg;
				MessageBox.Show(msg, string.Format("{0} ({1})", title ?? "Error", ex.GetType().Name));
				return ex;
			}
		}

		private void menuOpen_Click(object sender, EventArgs e)
		{
			var dlg = new OpenFileDialog { Filter = "Baadia diagrams (*.badia)|*.badia|Show all files (*.*)|*.*" };
			if (dlg.ShowDialog() == DialogResult.OK)
				Load(dlg.FileName);
		}
		new void Load(string filename)
		{
			TryCatchWithMessageBox(() => {
				_diagramCtrl.Load(filename);
				_filename = filename;
			}, "Unable to load " + filename);
		}

		private void menuSaveAs_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Not implemented");
		}

		private void menuExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void menuCut_Click(object sender, EventArgs e)
		{
			_diagramCtrl.Cut();
		}

		private void menuCopy_Click(object sender, EventArgs e)
		{
			_diagramCtrl.Copy();
		}

		private void menuPaste_Click(object sender, EventArgs e)
		{
			_diagramCtrl.Paste();
		}

		private void menuDelete_Click(object sender, EventArgs e)
		{
			_diagramCtrl.DeleteSelected();
		}

		private void menuDuplicate_Click(object sender, EventArgs e)
		{
			_diagramCtrl.DuplicateSelected();
		}

		private void menuSelectAll_Click(object sender, EventArgs e)
		{
			_diagramCtrl.SelectAll();
		}

		private void menuClearText_Click(object sender, EventArgs e)
		{
			_diagramCtrl.ClearText();
		}
	}
	public class CustomComboBox : ComboBox
	{
		protected override void OnDropDown(EventArgs e)
		{
			var p = MainForm.NewPopup(new System.Windows.Forms.TextBox { Text = "hi" });
			p.Show(PointToScreen(new Point(0, Height)));
		}
	}
}
