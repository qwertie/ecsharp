using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Loyc.Collections;

namespace MiniTestRunner
{
	// WinForms-independent ViewModel code for a row of the tree
	partial class RowVM : NPCHelper
	{
		public readonly IRowModel Model;
		public readonly RowVM Parent;
		public List<RowVM> _children;

		public RowVM(IRowModel model, RowVM parent)
		{
			Parent = parent;
			Model = model;
			Model.PropertyChanged += Model_PropertyChanged;
			
			if (model.Children.Count > 0)
			{
				_children = new List<RowVM>();
				Synchronize(_children, model.Children, this);
			}
		}

		#region Change propagation from IRowModel => RowVM => parent RowVM

		void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Children")
			{
				if (Model.Children.Count == 0)
					_children = null;
				else {
					_children = _children ?? new List<RowVM>();
					Synchronize(_children, Model.Children, this);
				}
			}
			if (e.PropertyName == "Priority" || e.PropertyName == "Type" || e.PropertyName == "Status")
				Changed(e.PropertyName + "Icon");
			Changed(e.PropertyName);
		}

		protected override void Changed(string prop)
		{
			base.Changed(prop);
			if (Parent != null)
				Parent.ChildChanged(this, prop);
		}

		/// <summary>An event that is fired when a property changes on a child, 
		/// grandchild, etc. Thus, the view (TreeViewAdvTestModel) only has to 
		/// subscribe to PropertyChanged and ChildPropertyChanged on the root 
		/// nodes to learn about all changes in the tree.</summary>
		public event PropertyChangedEventHandler ChildPropertyChanged;

		protected void ChildChanged(RowVM sender, string prop)
		{
			if (ChildPropertyChanged != null)
				ChildPropertyChanged(sender, new PropertyChangedEventArgs(prop));
			if (Parent != null)
				Parent.ChildChanged(sender, prop);
		}

		/// <summary>Updates a list of viewmodels to match a list of models.</summary>
		/// <param name="parent">parent node assigned to any new viewmodels that are created.</param>
		internal static void Synchronize(List<RowVM> vms, IList<IRowModel> models, RowVM parent)
		{
			var map = new Dictionary<IRowModel, RowVM>();
			foreach (var vm in vms)
				map[vm.Model] = vm;

			vms.Clear();
			foreach (var model in models)
			{
				RowVM vm;
				if (map.TryGetValue(model, out vm))
					vms.Add(vm);
				else
					vms.Add(new RowVM(model, parent));
			}
		}

		#endregion

		public string Name
		{
			get { return Model.Name; }
		}

		public string RunTime
		{
			get {
				if (Model.Status == TestStatus.NotRun)
					return "";

				var task = Model.Task;
				TimeSpan time = task.RunTime;
				double sec = time.TotalSeconds;
				if (sec <= 9.9995)
					return string.Format("{0:0.000}s", sec);
				if (sec <= 99.995)
					return string.Format("{0:00.00}s", sec);
				if (sec <= 5999.95)
					return task.RunTime.ToString(@"m\:ss\.f");
				else
					return string.Format("{0}:{1}:{2}", (int)time.TotalHours, time.Minutes, time.Seconds);
			}
		}

		public TestNodeType Type
		{
			get { return Model.Type; }
		}
		public int Priority
		{
			get { return Model.BasePriority; }
		}
		public TestStatus Status
		{
			get { return Model.Status; }
		}
		public string Summary
		{
			get { return Model.Summary; }
		}

		public string LastRunAt
		{
			get {
				var task = Model.Task;
				if (task == null || task.LastRunAt == default(DateTime))
					return "";
				if (task.LastRunAt.Year == DateTime.Now.Year)
					return task.LastRunAt.ToString("ddd MMM dd HH:mm:ss");
				else
					return task.LastRunAt.ToString("yyyy-MM-dd HH:mm:ss");
			}
		}

		public IList<RowVM> Children
		{
			get { return _children ?? (IList<RowVM>)EmptyList<RowVM>.Value; }
		}
	}
}
