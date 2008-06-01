using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using System.Diagnostics;

namespace Loyc.Runtime
{
	/// <summary>Creates and controls a thread, and fills in a gap in the
	/// .NET framework by propagating thread-local variables from parent
	/// to child threads, and by providing a ThreadStarting event.</summary>
	/// <remarks>
	/// This class is a decorator for the Thread class and thus a 
	/// drop-in replacement, except that only the most common methods and
	/// properties (both static and non-static) are provided.
	/// 
	/// A child thread inherits a thread-local value from a parent thread
	/// only if ForkThread.AllocateDataSlot, ForkThread.AllocateNamedDataSlot
	/// or ForkThread.GetNamedDataSlot was called to create the variable.
	/// Sadly, there is no way to provide inheritance for variables marked by
	/// [ThreadStatic].
	/// 
	/// TODO: rewrite ThreadState property for .NET compact framework.
	/// </remarks>
	public class ThreadEx
	{
		protected Thread _parent; // set by Start()
		protected Thread _thread; // underlying thread
		protected ThreadStart _ts1;
		protected ParameterizedThreadStart _ts2;
		protected List<object> _copiedSlots;
		protected object _startLock = new object();

		protected static List<LocalDataStoreSlot> _slots = new List<LocalDataStoreSlot>();
		
		/// <summary>
		/// This event is called in the context of a newly-started thread, provided
		/// that the thread is started by the Start() method of this class (rather
		/// than Thread.Start()).
		/// </summary>
		/// <remarks>The Start() method blocks until this </remarks>
		public static event EventHandler<ThreadStartEventArgs> ThreadStarting;

		public ThreadEx(ParameterizedThreadStart start)
			{ _thread = new Thread(ThreadStart); _ts2 = start; }
		public ThreadEx(ThreadStart start)
			{ _thread = new Thread(ThreadStart); _ts1 = start; }
		public ThreadEx(ParameterizedThreadStart start, int maxStackSize)
			{ _thread = new Thread(ThreadStart, maxStackSize); _ts2 = start; }
		public ThreadEx(ThreadStart start, int maxStackSize)
			{ _thread = new Thread(ThreadStart, maxStackSize); _ts1 = start; }		

		/// <summary>
		/// Causes the operating system to change the state of the current instance to
		/// System.Threading.ThreadState.Running.
		/// </summary>
		public void Start() { Start(null); }
		/// <summary>
		/// Causes the operating system to change the state of the current instance to
		/// System.Threading.ThreadState.Running. Start() does not return until the
		/// ThreadStarted event is handled.
		/// </summary><remarks>
		/// Once the thread terminates, it cannot be restarted with another call to Start.
		/// </remarks>
		public virtual void Start(object obj)
		{
			if (_startLock == null)
				throw new ThreadStateException("The thread has already been started.");
			lock(_startLock) {
				if (_parent != null)
					throw new ThreadStateException("The thread has already been started.");

				_parent = Thread.CurrentThread;

				lock(_slots) {
					// Make a copy of all known thread-local variables in _slots
					_copiedSlots = new List<object>(_slots.Count);
					for (int i = 0; i < _slots.Count; i++)
						_copiedSlots.Add(Thread.GetData(_slots[i]));

					_thread.Start(obj);

					while(_copiedSlots != null)
						Thread.Sleep(0);
				}
				
				while(_startLock != null)
					Thread.Sleep(0);
			}
		}

		protected virtual void ThreadStart(object obj)
		{
			Debug.Assert(_thread == Thread.CurrentThread);

			// Inherit thread-local variables from parent
			for (int i = 0; i < _slots.Count; i++)
				Thread.SetData(_slots[i], _copiedSlots[i]);

			// allow parent thread to release lock on _slots so that 
			// ThreadStarting event handlers can use it
			_copiedSlots = null; 

			// Note that Start() is still running in the parent thread
			if (ThreadStarting != null)
				ThreadStarting(this, new ThreadStartEventArgs(_parent, this));

			_startLock = null; // allow parent thread to return from Start()

			if (_ts2 != null)
				_ts2(obj);
			else
				_ts1();
		}

		/// <summary>
		/// Gets the currently running thread.
		/// </summary>
		public static Thread CurrentThread { get { return Thread.CurrentThread; } }
		/// <summary>
		/// Gets or sets a value indicating whether or not a thread is a background thread.
		/// </summary>
		public bool IsBackground { get { return _thread.IsBackground; } set { _thread.IsBackground = value; } }
		/// <summary>
		/// Gets a unique identifier for the current managed thread.
		/// </summary>
		public int ManagedThreadId { get { return _thread.ManagedThreadId; } }
		/// <summary>
		/// Gets or sets the name of the thread.
		/// </summary>
		public string Name { get { return _thread.Name; } set { _thread.Name = value; } }
		/// <summary>
		/// Gets or sets a value indicating the scheduling priority of a thread.
		/// </summary>
		public ThreadPriority Priority { get { return _thread.Priority; } set { _thread.Priority = value; } }
		/// <summary>
		/// Gets a value containing the states of the current thread.
		/// </summary>
		public System.Threading.ThreadState ThreadState { get { return _thread.ThreadState; } }
		/// <summary>
		/// Raises a System.Threading.ThreadAbortException in the thread on which it
		/// is invoked, to begin the process of terminating the thread while also providing
		/// exception information about the thread termination. Calling this method usually
		/// terminates the thread.
		/// </summary>
		public void Abort(object stateInfo) { _thread.Abort(stateInfo); }
		/// <summary>
		/// Allocates an unnamed data slot on all the threads. For better performance,
		/// use fields that are marked with the System.ThreadStaticAttribute attribute
		/// instead.
		/// </summary>
		public static LocalDataStoreSlot AllocateDataSlot() { 
			LocalDataStoreSlot slot = Thread.AllocateDataSlot();
			_slots.Add(slot);
			return slot;
		}
		/// <summary>
		/// Allocates a named data slot on all threads. For better performance, use fields
		/// that are marked with the System.ThreadStaticAttribute attribute instead.
		/// </summary>
		public static LocalDataStoreSlot AllocateNamedDataSlot(string name) {
			LocalDataStoreSlot slot = Thread.AllocateNamedDataSlot(name);
			_slots.Add(slot);
			return slot;
		}
		/// <summary>
		/// Eliminates the association between a name and a slot, for all threads in
		/// the process. For better performance, use fields that are marked with the
		/// System.ThreadStaticAttribute attribute instead.
		/// </summary>
		public static void FreeNamedDataSlot(string name) 
		{ 
			LocalDataStoreSlot slot = Thread.GetNamedDataSlot(name);
			int i = _slots.IndexOf(slot);
			if (i != -1)
				_slots.RemoveAt(i);
			Thread.FreeNamedDataSlot(name);
		}
		/// <summary>
		/// Retrieves the value from the specified slot on the current thread, within
		/// the current thread's current domain. For better performance, use fields that
		/// are marked with the System.ThreadStaticAttribute attribute instead.
		/// </summary>
		public static object GetData(LocalDataStoreSlot slot) { return Thread.GetData(slot); }
		/// <summary>
		/// Returns the current domain in which the current thread is running.
		/// </summary>
		public static AppDomain GetDomain() { return Thread.GetDomain(); }
		/// <summary>
		/// Looks up a named data slot. For better performance, use fields that are marked
		/// with the System.ThreadStaticAttribute attribute instead.
		/// </summary>
		public static LocalDataStoreSlot GetNamedDataSlot(string name) 
		{
			LocalDataStoreSlot slot = Thread.GetNamedDataSlot(name);
			if (!_slots.Contains(slot))
				_slots.Add(slot);
			return slot;
		}
		/// <summary>
		/// Returns a hash code for the current thread.
		/// </summary>
		public override int GetHashCode() { return _thread.GetHashCode(); }
		/// <summary>
		/// Blocks the calling thread until a thread terminates, while continuing to
		/// perform standard COM and SendMessage pumping.
		/// </summary>
		public void Join() { _thread.Join(); }
		/// <summary>
		/// Blocks the calling thread until a thread terminates or the specified time 
		/// elapses, while continuing to perform standard COM and SendMessage pumping. 
		/// </summary>
		public bool Join(int milliseconds) { return _thread.Join(milliseconds); }
		/// <summary>
		/// Sets the data in the specified slot on the currently running thread, for
		/// that thread's current domain. For better performance, use fields marked with
		/// the System.ThreadStaticAttribute attribute instead.
		/// </summary>
		public static void SetData(LocalDataStoreSlot slot, object data) { Thread.SetData(slot, data); }
		/// <summary>
		/// Suspends the current thread for a specified time.
		/// </summary>
		public static void Sleep(int millisecondsTimeout) { Thread.Sleep(millisecondsTimeout); }

		public Thread Thread { get { return _thread; } }
		public Thread ParentThread { get { return _parent; } }

		public bool IsAlive { 
			get { 
				System.Threading.ThreadState t = ThreadState;
				return t != System.Threading.ThreadState.Stopped &&
				       t != System.Threading.ThreadState.Unstarted &&
				       t != System.Threading.ThreadState.Aborted;
			}
		}
	}

	public class ThreadStartEventArgs : EventArgs
	{
		public ThreadStartEventArgs(Thread parent, ThreadEx child) 
			{ ParentThread = parent; ChildThread = child; }
		public Thread ParentThread;
		public ThreadEx ChildThread;
	}

	/// <summary>A wrapper around a thread-local variable slot. Provides a more 
	/// convenient way to access thread-local data than using Thread.GetData 
	/// and Thread.SetData directly.</summary>
	/// <typeparam name="T">Type of variable to wrap</typeparam>
	/// <remarks>
	/// Variables of this type should always be static and they should not be 
	/// marked with the [ThreadStatic] attribute.
	/// 
	/// ThreadLocalVariable(of T) is less convenient than the [ThreadStatic]
	/// attribute, but ThreadLocalVariable's default constructor calls 
	/// ThreadEx.AllocateDataSlot so that the variable's value is inherited in 
	/// child threads. Also, [ThreadStatic] is not available in the Compact 
	/// Framework.
	/// </remarks>
	public class ThreadLocalVariable<T> 
	{
		protected LocalDataStoreSlot _slot;
		public ThreadLocalVariable() { 
			_slot = ThreadEx.AllocateDataSlot();
			if (default(T) != null)
				ThreadEx.SetData(_slot, default(T));
		}
		public ThreadLocalVariable(T initialValue) { 
			_slot = ThreadEx.AllocateDataSlot();
			ThreadEx.SetData(_slot, initialValue);
		}
		public ThreadLocalVariable(LocalDataStoreSlot slot) { 
			_slot = slot;
		}
		public T Value { 
			get { return (T)Thread.GetData(_slot); } 
			set { Thread.SetData(_slot, value); }
		}
	}

	[TestFixture]
	public class ThreadExTests
	{
		[Test]
		public void BasicChecks()
		{
			ThreadLocalVariable<int> threadVar = new ThreadLocalVariable<int>(123);
			Thread parent = Thread.CurrentThread;
			bool eventOccurred = false;
			bool valueOk = true, eventOk = true;
			bool stop = false;
			bool started = false;

			ThreadEx t = new ThreadEx(delegate(object o)
			{
				started = true;
				try
				{
					if ((int)o != 123 || threadVar.Value != 123)
						valueOk = false;
				}
				catch
				{
					valueOk = false;
				}
				while (!stop)
					ThreadEx.Sleep(0);
				started = false;
			});

			EventHandler<ThreadStartEventArgs> eh = null;
			ThreadEx.ThreadStarting += (eh = delegate(object o, ThreadStartEventArgs e)
			{
				eventOccurred = true;
				if (e.ChildThread != t || e.ParentThread != parent)
					eventOk = false;
				ThreadEx.ThreadStarting -= eh;

				// Allocating/removing slots in this handler shouldn't cause deadlock
				ThreadEx.AllocateNamedDataSlot("ThreadEx_foo");
				ThreadEx.FreeNamedDataSlot("ThreadEx_foo"); 
			});

			Assert.IsFalse(t.IsAlive);
			Assert.AreEqual(t.ThreadState, System.Threading.ThreadState.Unstarted);
			t.Start(123);
			Assert.IsTrue(t.IsAlive);
			Assert.IsTrue(eventOccurred);
			Assert.IsTrue(eventOk);
			while(!started)
				ThreadEx.Sleep(0);
			Assert.AreEqual(t.ThreadState, System.Threading.ThreadState.Running);
			stop = true;
			Assert.IsTrue(t.Join(5000));
			Assert.IsTrue(valueOk);
			Assert.IsFalse(started);
		}
	}
}
