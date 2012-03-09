using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Loyc.Collections;
using System.Reflection;
using MiniTestRunner.TestDomain;
using UpdateControls.Fields;
using System.Runtime.Serialization;

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
	/// Another reason that ITestRowModel should not be a MBRO is that it uses
	/// Update Controls, which does not work across AppDomain boundaries. The task,
	/// therefore, uses <see cref="IPropertyChanged"/> instead of Update Controls
	/// (but not INotifyPropertyChanged, which can't be used across an AppDomain 
	/// boundary). This also means that changing a row's priority requires RowModel
	/// to notify the task manually.
	/// <para/>
	/// RowModels do not aggregate properties of children, such as Status and 
	/// RunTime. The ViewModel (<see cref="RowVM"/>) does that instead.
	/// </remarks>
	[Serializable]
	public abstract class RowModel
	{
		public RowModel()
		{
		}

		protected IndependentS<RowModel> _parent = new IndependentS<RowModel>("Parent", null);
		public RowModel Parent
		{
			get { return _parent.Value; }
		}

		IndependentS<int> _BasePriority = new IndependentS<int>("BasePriority", 0);
		public int BasePriority
		{
			get { return _BasePriority.Value; }
			set { _BasePriority.Value = value; PropagatePriority(); }
		}
		void PropagatePriority()
		{
			// Manually notify associated tasks that their priority changed
			if (Task != null)
				Task.Priority = Priority;
			Children.OfType<RowModel>().ForEach(m => m.PropagatePriority());
		}

		public int InheritedPriority
		{
			get { return _parent.Value.Priority; }
		}

		[NonSerialized()]
		protected Dependent<int> _priority;
		public int Priority
		{
			get {
				// Don't init in constructor, because it's bypassed in deserialization
				if (_priority == null)
					_priority = new Dependent<int>("Priority", () => BasePriority + InheritedPriority);
				return _priority.Value;
			}
		}

		public abstract string Name { get; }
		public abstract TestNodeType Type { get; }
		public abstract IList<RowModel> Children { get; }

		public virtual ITestTask Task { get { return null; } }

		public abstract string Summary { get; }
		public virtual TestStatus Status { get { return TestStatus.None; } }
		public virtual DateTime LastRunAt { get { return DateTime.MinValue; } }
		public virtual TimeSpan RunTime { get { return TimeSpan.Zero; } }
	}

	/// <summary>A serializable Independent.</summary>
	[Serializable]
	public class IndependentS<T> : Independent<T>, ISerializable
	{
		public IndependentS() { }
		public IndependentS(T value) : base(value) { }
		public IndependentS(string name, T value) : base(name, value) { }

		protected IndependentS(SerializationInfo info, StreamingContext context)
			: base(info.GetString("Name"), (T)info.GetValue("Value", typeof(T)))
		{
		}
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Value", _value);
			info.AddValue("Name", _name);
		}
	}
}
