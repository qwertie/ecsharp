using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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
	}
}
