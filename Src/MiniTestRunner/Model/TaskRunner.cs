using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Collections.Linq;

namespace MiniTestRunner
{
	public interface ITask
	{
		/// <summary>Runs the task.</summary>
		/// <returns>Returns null, or a list of tasks to add to the task queue.</returns>
		IEnumerable<ITask> RunOnCurrentThread();
		/// <summary>Returns true if the task can start right now, false if it was 
		/// already started earlier and does not want to be started again.</summary>
		/// <remarks>A task that has run once can be run again if RunOnCurrentThread
		/// does not set IsCompleted to true and it is placed in the task queue again.
		/// </remarks>
		bool IsPending { get; }
		/// <summary>Task priority (higher priority tasks generally run first).</summary>
		int Priority { get; }
		/// <summary>Maximum number of threads that can run at the same time while 
		/// this task is running. TaskRunner will treat zero (and lower) as 1.</summary>
		int MaxThreads { get; }
		/// <summary>Returns a list of tasks that must be completed before this one.
		/// Lower-priority prerequisites effectively have their priority boosted to this task's
		/// 
		/// The method is given a list of tasks that are already running, and if one
		/// or more running tasks cannot be executed concurrently with this task,
		/// this task can return one of them to block this task from starting.</summary>
		IEnumerable<ITask> Prerequisites(IEnumerable<ITask> concurrentTasks);
		/// <summary>Sends an abort command to the task. The task can either stop
		/// itself in whatever way it knows how, or it can call taskThread.Abort().
		/// This method should return immediately without waiting for the thread to
		/// stop.</summary>
		void Abort(Thread taskThread);
	}

	/// <summary>
	/// A general-purpose class for running a prioritized list of tasks that implement 
	/// <see cref="ITask"/>, on multiple threads. Each task may specify prerequisites
	/// and exclude other tasks from running concurrently.
	/// </summary>
	/// <remarks>
	/// If an exception occurs while running a task, the exception is added to a map
	/// that associates the exception with that task. Call ErrorFor(task) to get the
	/// exception, if any.
	/// </remarks>
	public class TaskRunner
	{
		// A simple linked list
		protected class Link<T>
		{
			public T Value;
			public Link<T> Next;

			static readonly EqualityComparer<T> Comp = EqualityComparer<T>.Default;
			public bool Contains(T item)
			{
				for (var link = this; link != null; link = link.Next)
					if (Comp.Equals(item, link.Value))
						return true;
				return false;
			}
		}
		class TaskThread
		{
			public ITask Task { get; set; }
			public ThreadEx Thread { get; private set; }
			public TaskRunner Runner { get; private set; }

			public TaskThread(ITask initialTask, TaskRunner runner) 
			{
				Task = initialTask;
				Thread = new ThreadEx(TestThread);
				Runner = runner;
				Thread.Start();
			}
			public void TestThread()
			{
				while (Task != null) {
					try {
						Task.RunOnCurrentThread();
					} catch(Exception ex) {
						Runner._errors[Task] = ex;
					}
					Task = null;
					Runner.AutoStartTasks();
				}
			}
		}

		// List of unstarted tasks, highest priority first
		static Func<ITask, ITask, int> HiPriorityFirst = (a, b) => b.Priority.CompareTo(a.Priority);
		List<ITask> _q = new List<ITask>();
		List<TaskThread> _threads = new List<TaskThread>();
		Dictionary<ITask, Exception> _errors = new Dictionary<ITask, Exception>();
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public IEnumerable<ITask> RunningTasks()
		{
			return from t in _threads let task = t.Task 
			       where task != null select task;
		}

		public Exception ErrorFor(ITask task)
		{
			return _errors.TryGetValue(task, null);
		}
		public IDictionary<ITask, Exception> Errors
		{
			get { return _errors; }
		}

		int _maxThreads = 2;
		public int MaxThreads
		{
			get { return _maxThreads; }
			set {
				CheckParam.Range("MaxThreads", value, 1, 256);
				_maxThreads = value;
			}
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AutoStartTasks()
		{
			RemoveDeadThreads();
			var removeList = new HashSet<ITask>();
			try {
				int blockPriority = int.MinValue;
				foreach (ITask task in _q)
				{
					if (task.Priority < blockPriority)
						break;
					var result = TryStart(task, task, null, ref blockPriority);
					if (result == Stop)
						break;
					if (result == Handled || result == Error)
						removeList.Add(task);
				}
			} finally {
				_q.RemoveAll(t => removeList.Contains(t));
			}
		}

		// Return values of TryStart
		static Symbol Handled = GSymbol.Get("Handled"); // Task started or already completed
		static Symbol Defer = GSymbol.Get("Defer");     // Task can't start now; blockPriority may have increased
		static Symbol Stop = GSymbol.Get("Stop");       // MaxThreads reached; no more tasks can start right now
		static Symbol Error = GSymbol.Get("Error");     // Exception occurred and was added to _errors

		private Symbol TryStart(ITask root, ITask task, Link<ITask> cycleDetection, ref int blockPriority)
		{
			if (!task.IsPending)
				return Handled;

			var running = RunningTasks().ToList();
			if (running.Count >= MaxThreads)
				return Stop;

			// Get list of prerequisites
			IEnumerable<ITask> preqs;
			try {
				preqs = task.Prerequisites(running);
			} catch(Exception ex) {
				AddError(root, task, ex);
				return Error;
			}

			// Try to run the prerequisites
			bool hasPreqs = false;
			if (preqs != null)
				foreach (var preq in preqs.Where(t => t.IsPending))
				{
					hasPreqs = true;
					if (!running.Contains(preq))
					{
						cycleDetection = new Link<ITask> { Value = task, Next = cycleDetection };
						if (cycleDetection != null && cycleDetection.Contains(preq)) {
							CycleDetected(root, preq);
							return Error;
						}
						var subresult = TryStart(root, preq, cycleDetection, ref blockPriority);
						if (subresult == Stop || subresult == Error)
							return subresult;
					}
				}

			// If there were no prerequisites, try to start the task
			if (hasPreqs)
				return Defer;
			else if (running.Count >= Math.Max(task.MaxThreads, 1)) {
				blockPriority = Math.Max(blockPriority, task.Priority);
				return Defer;
			} else {
				Start(task);
				return Handled;
			}
		}

		protected void Start(ITask task)
		{
			Debug.Assert(_threads.All(t => t.Thread.IsAlive));
			Debug.Assert(_threads.Count <= Math.Min(task.MaxThreads, MaxThreads));
			
			var thread = _threads.FirstOrDefault(t => t.Task == null);
			if (thread == null)
				_threads.Add(thread = new TaskThread(task, this));
			else
				thread.Task = task;
		}

		protected virtual void CycleDetected(ITask root, ITask startOfCycle)
		{
			AddError(root, startOfCycle, new ApplicationException("Cycle detected in prerequisites for this task"));
		}
		private void AddError(ITask root, ITask errorTask, Exception ex)
		{
			try {
				ex.Data["ErrorTask"] = errorTask;
			} catch(ArgumentException) { } // errorTask is not Serializable
			_errors[root] = ex;
		}
		private void RemoveDeadThreads()
		{
			_threads.RemoveAll(thread => !thread.Thread.IsAlive);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AddTasks(IEnumerable<ITask> tasks)
		{
			_q.AddRange(tasks);
			ReSortTasks();
		}

		public void ReSortTasks()
		{
			ListExt.StableSort(_q, (a, b) => b.Priority.CompareTo(a.Priority));
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void RemoveTasks(ICollection<ITask> set, bool abortIfRunning)
		{
			_q.RemoveAll(t => set.Contains(t));
			if (abortIfRunning)
				AbortIfRunning(set);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AbortIfRunning(IEnumerable<ITask> set)
		{
			foreach (var task in set)
				foreach (var thread in _threads)
					if (task == thread.Task)
						try {
							task.Abort(thread.Thread.Thread);
						} catch (Exception ex) {
							_errors[task] = ex;
						}
		}
	}


	/*public class TaskRunner
	{
		public TaskRunner()
		{
			MaxThreads = 1;
		}

		/// <summary>List of unstarted tasks. Last task is highest priority.</summary>
		List<IRowModel> TaskList = new List<IRowModel>();

		class PriorityComparer : Comparer<IRowModel>
		{
			public new static PriorityComparer Default = new PriorityComparer();
			public override int Compare(IRowModel x, IRowModel y)
			{
				int c = x.TotalPriority().CompareTo(y.TotalPriority());
				if (c != 0)
					return c;
				c = x.ListOrder.CompareTo(y.ListOrder);
				Debug.Assert(c != 0 || x == y);
				return c;
			}
		}

		public int MaxThreads { get; set; }

		class TaskThread
		{
			public IRowModel Row { get; private set; }
			public bool IsComplete { get; private set; }
			public ThreadEx Thread { get; private set; }
			Action<IRowModel> _onComplete;

			public TaskThread(IRowModel row, Action<IRowModel> onComplete) 
			{
				Thread = new ThreadEx(TestThread);
				Row = row;
				_onComplete = onComplete;
			}
			public void TestThread()
			{
				System.Threading.Thread.Sleep(500); // TEMP
				Row.Task.RunOnCurrentThread();
				IsComplete = true;
				_onComplete(Row);
			}
		}

		List<TaskThread> Threads = new List<TaskThread>();

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AutoStartTasks()
		{
			AutoRemoveThreads();

			if (Threads.Count < MaxThreads && TaskList.Count != 0)
			{
				int topPriority = TaskList[TaskList.Count - 1].TotalPriority();
				for (int i = TaskList.Count - 1; ; i--)
				{
					if (i < 0 || TaskList[i].TotalPriority() < topPriority)
						return;

					if (Threads.Count < TaskList[i].Task.ThreadLimit)
					{
						var tt = new TaskThread(TaskList[i], OnTaskComplete);
						Threads.Add(tt);
						TaskList.RemoveAt(i);
						tt.Thread.Start();
					}
				}
			}
		}

		public event Action<IRowModel> TaskComplete; // runs on worker thread

		private void OnTaskComplete(IRowModel row)
		{
			if (TaskComplete != null)
				TaskComplete(row);
			AutoStartTasks();
		}

		private void AutoRemoveThreads()
		{
			for (int i = 0; i < Threads.Count; i++)
			{
				if (Threads[i].IsComplete || Threads[i].Thread.ThreadState == System.Threading.ThreadState.Stopped)
				{
					Debug.Assert(Threads[i].Row.Status != TestStatus.Running);
					Threads.RemoveAt(i);
				}
			}
		}

		public int _nextTaskOrder;

		[MethodImpl(MethodImplOptions.Synchronized)]
		public void AddTasks(IEnumerable<IRowModel> tests, bool removeDuplicates)
		{
			Debug.Assert(tests.All(t => t.Task != null));
			
			HashSet<IRowModel> uniqueSet = null;
			if (removeDuplicates)
			{
				uniqueSet = new HashSet<IRowModel>(tests);
				foreach (var duplicate in TaskList.Where(d => uniqueSet.Contains(d)))
					uniqueSet.Remove(duplicate);
			}

			int boundary = TaskList.Count;
			foreach (var newRow in tests.Where(t => uniqueSet == null || uniqueSet.Contains(t)))
				TaskList.Add(newRow);
			
			for (int i = 0; i < TaskList.Count; i++)
				TaskList[i].ListOrder = i;
			TaskList.Sort(PriorityComparer.Default);
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void RemoveTasks(ICollection<IRowModel> set)
		{
			TaskList.RemoveAll(t => set.Contains(t));
		}
		
		public void RemoveTasksAndAbortThreads(ICollection<IRowModel> set, int waitMsBeforeAbort)
		{
			RemoveTasks(set);
			AbortThreads(set, waitMsBeforeAbort);
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void AbortThreads(ICollection<IRowModel> set, int waitMsBeforeAbort)
		{
			SimpleTimer timer = new SimpleTimer();
			while (timer.Millisec < waitMsBeforeAbort && Threads.Any(t => set.Contains(t.Row))) {
				AutoRemoveThreads();
				Thread.Sleep(1);
			}

			foreach (var thread in Threads.Where(t => set.Contains(t.Row) && t.Row.Task.CanAbort))
				thread.Thread.Abort();
			
			Thread.Sleep(0);
			AutoRemoveThreads(); // remove stopped tasks
		}
	}*/
}
