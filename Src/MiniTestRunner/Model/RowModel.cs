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
	/// RowModel is not a MarshalByRefObject (MBRO), but the ITestTask normally 
	/// is. So when a TestRowModel is passed across AppDomain boundaries, it is 
	/// deep-copied-by-value, although the Task property is typically a MBRO and is
	/// left in its original AppDomain. This ensures that when the default domain
	/// calls ITestTaskEx.Children, and it returns a list of RowModels, the 
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
		protected int _basePriority, _inheritedPriority;
		public int BasePriority
		{
			get { return _basePriority; }
			set {
				if (Set(ref _basePriority, value, "BasePriority"))
					PropagatePriority();
			}
		}
		public int InheritedPriority
		{
			get { return _inheritedPriority; }
			set {
				if (Set(ref _inheritedPriority, value, "InheritedPriority"))
					PropagatePriority();
			}
		}

		private void PropagatePriority()
		{
			int prio = BasePriority + InheritedPriority;
			if (Task != null)
				Task.Priority = prio;
			foreach (var row in Children)
				row.InheritedPriority = prio;
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

		public abstract ITestTask Task { get; }
	}
}
