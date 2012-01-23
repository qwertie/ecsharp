using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Utilities;
using MiniTestRunner.TestDomain;
using System.Runtime.Serialization;
using System.Runtime.Remoting;

namespace MiniTestRunner
{
	[Serializable]
	public class TaskRowModel : RowModel, IDeserializationCallback
	{
		protected string _name;
		protected TestNodeType _type;
		protected List<IRowModel> _children = new List<IRowModel>();
		protected ITaskEx _task;

		public TaskRowModel(string name, TestNodeType type, ITaskEx task, bool delaySubscribeToTask)
		{
			_name = name;
			_type = type;
			_task = task;

			// Normally the TaskRowModel and task are created in the same AppDomain
			// and then the TaskRowModel is sent back to the main AppDomain. In this
			// case, we should not subscribe to PropertyChanged until deserialization.
			if (!delaySubscribeToTask) Subscribe();
		}
		private void Subscribe()
		{
			if (RemotingServices.IsTransparentProxy(_task))
				new CrossDomainPropertyChangeHelper(_task, TaskPropertyChanged);
			else
				_task.PropertyChanged += TaskPropertyChanged;
		}
		public void OnDeserialization(object context)
		{
			if (RemotingServices.IsTransparentProxy(_task))
				Subscribe();
		}

		protected virtual void TaskPropertyChanged(object sender, string propertyName)
		{
			if (propertyName == "Children")
				_children = new List<IRowModel>(_task.Children);
			if (propertyName == "Children" || propertyName == "Status" || propertyName == "Summary")
				Changed(propertyName);
		}

		public override string Name
		{
			get { return _name; }
		}
		public override IList<IRowModel> Children
		{
			get { return _children; }
		}
		public override TestNodeType Type
		{
			get { return _type; }
		}
		public override ITask Task
		{
			get { return _task; }
		}
		public override TestStatus Status
		{
			get { return _task.Status; }
		}
		public override string Summary
		{
			get { return _task.Summary; }
		}
	}
}
