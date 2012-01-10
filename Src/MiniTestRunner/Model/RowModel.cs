using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Loyc.Collections;
using System.Reflection;
using MiniTestRunner.TestDomain;

namespace MiniTestRunner
{
	/// <summary>The model for a row of the test tree.</summary>
	/// <remarks>
	/// TestRowModel is not a MarshalByRefObject (MBRO), but the ITestTask normally 
	/// is. So when a TestRowModel is passed across AppDomain boundaries, it is 
	/// deep-copied-by-value, although the Task property is typically a MBRO and is
	/// left in its original AppDomain. This ensures that when the default domain
	/// calls ITestTaskEx.Children, and it returns a list of TestRowModels, the 
	/// children are copied to the default appdomain, so that when the domain is 
	/// unloaded, the row models continue to exist.
	/// <para/>
	/// Another reason that ITestRowModel should not be a MBRO is that it implements
	/// INotifyPropertyChanged, whose PropertyChanged event can't be marshaled 
	/// across an AppDomain boundary.
	/// </remarks>
	[Serializable]
	public abstract class RowModel : NPCHelper, IRowModel
	{
		protected int _priority;
		public virtual int Priority
		{
			get { return _priority; }
			set { Set(ref _priority, value, "Priority"); }
		}

		protected TestStatus _status;
		public virtual TestStatus Status
		{
			get { return _status; }
			set { Set(ref _status, value, "Status"); }
		}

		public abstract string Name { get; }

		public abstract string Summary { get; }

		public abstract IList<IRowModel> Children { get; }

		public abstract TestNodeType Type { get; }

		public int InheritedPriority { get; set; }
		public int ListOrder { get; set; }

		public abstract ITask Task { get; }
	}
}
