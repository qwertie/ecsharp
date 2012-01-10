using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;
using Loyc.Essentials;
using System.Diagnostics;

namespace MiniTestRunner
{
	public class TaskRunner
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
	}
}
