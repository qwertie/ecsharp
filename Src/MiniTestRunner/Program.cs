using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MiniTestRunner
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			var model = new TaskTreeModel(new TaskRunner(), new OptionsModel());
			Application.Run(new TestingForm(model));
		}
	}
}
