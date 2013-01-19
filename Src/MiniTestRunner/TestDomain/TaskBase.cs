using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using Loyc.Collections;
using System.Security;

namespace MiniTestRunner.TestDomain
{
	abstract class TaskBase : PropertyChangedHelper, ITaskEx
	{
		TestStatus _status = TestStatus.NotRun;
		DateTime _lastRunAt;
		TimeSpan _runTime;
		string _summary;
		IList<RowModel> _children = EmptyList<RowModel>.Value;

		// Apparently we can't control remoting lifetime in partial-trust!??!?!
		// This method causes a TypeLoadException in partial trust.
		[SecurityCritical]
		public override object InitializeLifetimeService()
		{
			return null; // This object will exist as long as its AppDomain does
		}

		public TestStatus Status
		{
			get { return _status; }
			set { Set(ref _status, value, "Status"); }
		}
		public DateTime LastRunAt
		{ 
			get { return _lastRunAt; }
			set { Set(ref _lastRunAt, value, "LastRunAt"); }
		}
		public TimeSpan RunTime
		{ 
			get { return _runTime; }
			set { Set(ref _runTime, value, "RunTime"); }
		}

		protected int _priority;
		public virtual int Priority
		{
			get { return _priority; }
			set { _priority = value; }
		}
		public virtual int MaxThreads
		{
			get { return int.MaxValue; }
		}
		public virtual bool IsPending
		{
			get { return Status == TestStatus.NotRun || Status == TestStatus.Running; }
		}

		public virtual IEnumerable<ITask> RunOnCurrentThread()
		{
			try {
				Status = TestStatus.Running;
				LastRunAt = DateTime.Now;
				var timer = new Stopwatch();
				timer.Start();
				RunCore();
				RunTime = timer.Elapsed;
			} finally {
				Status = TestStatus.Inconclusive;
			}
			Status = TestStatus.Success;
			return null;
		}
		
		protected abstract void RunCore();

		public Stream OutputStream { get; set; }

		protected internal static T GetPropertyValue<T>(object obj, string propertyName, T failValue)
		{
			PropertyInfo propertyInfo;
			if (obj != null && (propertyInfo = obj.GetType().GetProperty(propertyName)) != null)
			{
				object result = propertyInfo.GetGetMethod().Invoke(obj, null);
				if (result is T)
					return (T)result;
			}
			return failValue;
		}

		public virtual IList<RowModel> Children
		{
			get { return _children; }
			set { Set(ref _children, value ?? EmptyList<RowModel>.Value, "Children"); }
		}

		public virtual string Summary
		{
			get { return _summary; }
			set { Set(ref _summary, value, "Summary"); }
		}

		public virtual void Abort(System.Threading.Thread thread)
		{
			throw new NotImplementedException();
		}
		public virtual IEnumerable<ITask> Prerequisites(IEnumerable<ITask> concurrentTasks)
		{
			return null;
		}
		public virtual AppDomain Domain 
		{ 
			get { return AppDomain.CurrentDomain; }
		}
	}
}
