using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Loyc.Utilities;
using MiniTestRunner.TestDomain;
using System.Runtime.Serialization;
using System.Runtime.Remoting;
using Loyc.Collections.Impl;
using UpdateControls.Fields;
using UpdateControls;
using Loyc.Collections;

namespace MiniTestRunner
{
	[Serializable]
	public class TaskRowModel : RowModel, IDeserializationCallback
	{
		protected string _name;
		protected TestNodeType _type;
		protected ITaskEx _task;
		protected CrossDomainPropertyChangeHelper _subscribeHelper;

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
				_subscribeHelper = new CrossDomainPropertyChangeHelper(_task, TaskPropertyChanged);
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
				_children.Value = new List<RowModel>(_task.Children);
			if (propertyName == "Status")
				_status.Value = _task.Status;
			if (propertyName == "Summary")
				_summary.Value = _task.Summary;
		}

		public override string Name
		{
			get { return _name; }
		}

		protected IndependentS<IList<RowModel>> _children = new IndependentS<IList<RowModel>>("Children", EmptyList<RowModel>.Value);
		public override IList<RowModel> Children
		{
			get { return _children.Value; }
		}

		public override TestNodeType Type
		{
			get { return _type; }
		}
		public override ITestTask Task
		{
			get { return _task; }
		}

		IndependentS<string> _summary = new IndependentS<string>("Summary", "");
		public override string Summary
		{
			get { return _summary.Value; }
		}
		IndependentS<TestStatus> _status = new IndependentS<TestStatus>("Status", TestStatus.NotRun);
		public override TestStatus Status
		{
			get { return _status.Value; }
		}
	}
}
