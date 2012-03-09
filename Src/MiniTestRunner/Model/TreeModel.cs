using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using Loyc.Collections;
using Loyc.Essentials;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using MiniTestRunner.TestDomain;
using UpdateControls.Collections;

namespace MiniTestRunner
{
	/// <summary>
	/// Holds a tree of testing tasks and manages the running of the tasks.
	/// </summary>
	public class TreeModel
	{
		TaskRunner _runner;
		public OptionsModel Options { get; private set; }
		
		IndependentList<RowModel> _roots = new IndependentList<RowModel>();
		public IList<RowModel> Roots
		{
			get { return _roots; }
		}

		public TreeModel(TaskRunner runner, OptionsModel options)
		{
			_runner = runner;
			//_runner.TaskComplete += new Action<IRowModel>(Runner_TaskComplete);
			Options = options;
		}

		public void BeginOpenAssemblies(string[] filenames, bool partialTrust)
		{
			var newRoots = new List<RowModel>();

			for (int i = 0; i < filenames.Length; i++)
			{
				string fn = filenames[i];
				string baseFolder = Path.GetFullPath(Path.Combine(fn, ".."));
				var task = AppDomainStarter.Start<AssemblyScanTask>(baseFolder, Path.GetFileName(fn), new object[] { fn, baseFolder }, partialTrust);
				var root = new TaskRowModel(Path.GetFileName(fn), TestNodeType.Assembly, task, false);
				
				newRoots.Add(root);
				_roots.Add(root);
			}
			_runner.AddTasks(newRoots.Select(row => row.Task));
			_runner.AutoStartTasks();
		}

		/*void Runner_TaskComplete(IRowModel row)
		{
			if (row is AssemblyScanTask || (row is UnitTestTask && (row as UnitTestTask).IsTestSet))
			{
				var suites = new List<IRowModel>();
				FindSuites(row, suites);
				_runner.AddTasks(suites, false);
			}
		}

		private void FindSuites(IRowModel row, List<IRowModel> suites)
		{
			foreach (var child in row.Children)
			{
				if (child.Children.Count > 0)
					FindSuites(child, suites);
				else if ((child is UnitTestTask) && (child as UnitTestTask).IsTestSet)
					suites.Add(child);
			}
		}*/

		internal void RemoveAllAssemblies()
		{
			for (int i = Roots.Count - 1; i >= 0; i--)
				if (Roots[i].Type == TestNodeType.Assembly)
					Roots.RemoveAt(i);
		}

		internal void Clear()
		{
			Roots.Clear();
		}

		public void StartTesting()
		{
			var queue = new List<RowModel>();
			FindTasksToRun(Roots, queue);
			_runner.AddTasks(queue.Select(row => row.Task));
			_runner.AutoStartTasks();
		}
		private void FindTasksToRun(IEnumerable<RowModel> list, List<RowModel> queue)
		{
			foreach (var row in list)
			{
				FindTasksToRun(row.Children, queue);
				if (row.Task != null && row.Status == TestStatus.NotRun)
					queue.Add(row);
			}
		}
	}

}
