using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Utilities;
using MiniTestRunner.TestDomain;

namespace MiniTestRunner
{
	[Serializable]
	public class TaskRowModel : RowModel
	{
		protected string _name;
		protected TestNodeType _type;
		protected List<IRowModel> _children = new List<IRowModel>();
		protected ITaskEx _task;

		public TaskRowModel(string name, TestNodeType type, ITaskEx task)
		{
			_name = name;
			_type = type;
			_task = task;
			new CrossDomainPropertyChangeHandler(task, TaskPropertyChanged);
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

	/// <summary>Allows a non-MarshalByRefObject to subscribe to PropertyChanged on 
	/// an object in another AppDomain. The CLR may pretend to allow us to subscribe 
	/// to the event directly, but when the event fires, it goes to a useless COPY 
	/// of the subscriber that was silently created in the other AppDomain.</summary>
	class CrossDomainPropertyChangeHandler : MarshalByRefObject
	{
		PropertyChangedDelegate _handler;
		public CrossDomainPropertyChangeHandler(IPropertyChanged target, PropertyChangedDelegate handler)
		{
			_handler = handler;
			target.PropertyChanged += (sender, prop) => _handler(sender, prop);
		}
		public override object InitializeLifetimeService()
		{
			// This AppDomain will keep this object alive forever. Or maybe this
			// object can die when the other AppDomain dies. TODO: find out.
			return null;
		}
	}
}
