using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Loyc.Runtime
{
	/// <summary>Creates and controls a thread, and fills in a gap in the
	/// .NET framework by propagating thread-local variables from parent
	/// to child threads, and by providing a ThreadStarting event.</summary>
	/// <remarks>
	/// This class is a decorator for the Thread class and thus a 
	/// drop-in replacement, except that only the most common methods and
	/// properties (both static and non-static) are provided.
	/// <para/>
	/// A child thread inherits a thread-local value from a parent thread
	/// only if ForkThread.AllocateDataSlot, ForkThread.AllocateNamedDataSlot
	/// or ForkThread.GetNamedDataSlot was called to create the variable.
	/// Sadly, there is no way to provide inheritance for variables marked by
	/// [ThreadStatic].
	/// <para/>
	/// TODO: rewrite ThreadState property for .NET compact framework.
	/// </remarks>
	public class ThreadEx
	{
		protected Thread _parent; // set by Start()
		protected Thread _thread; // underlying thread
		protected ThreadStart _ts1;
		protected ParameterizedThreadStart _ts2;
		protected int _startState = 0;
		
		protected internal static List<WeakReference<ThreadLocalVariableBase>> _TLVs = new List<WeakReference<ThreadLocalVariableBase>>();
		
		/// <summary>
		/// This event is called in the context of a newly-started thread, provided
		/// that the thread is started by the Start() method of this class (rather
		/// than Thread.Start()).
		/// </summary>
		/// <remarks>The Start() method blocks until this event completes.</remarks>
		public static event EventHandler<ThreadStartEventArgs> ThreadStarting;

		/// <summary>
		/// This event is called when a thread is stopping, if the thread is stopping
		/// gracefully and provided that it was started by the Start() method of this 
		/// class (rather than Thread.Start()).
		/// </summary>
		public static event EventHandler<ThreadStartEventArgs> ThreadStopping;

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
		/// Once the thread terminates, it CANNOT be restarted with another call to Start.
		/// </remarks>
		public virtual void Start(object parameter)
		{
			if (Interlocked.CompareExchange(ref _startState, 1, 0) != 0)
				throw new ThreadStateException("The thread has already been started.");

			Debug.Assert(_parent == null);
			_parent = Thread.CurrentThread;

			_thread.Start(parameter);
				
			while(_startState == 1)
				Thread.Sleep(0);
		}

		protected virtual void ThreadStart(object parameter)
		{
			Debug.Assert(_thread == Thread.CurrentThread);

			try {
				// Inherit thread-local variables from parent
				for (int i = 0; i < _TLVs.Count; i++) {
					ThreadLocalVariableBase v = _TLVs[i].Target;
					if (v != null)
						v.Propagate(_parent.ManagedThreadId, _thread.ManagedThreadId);
				}

				// Note that Start() is still running in the parent thread
				if (ThreadStarting != null)
					ThreadStarting(this, new ThreadStartEventArgs(_parent, this));

				_startState = 2; // allow parent thread to continue

				if (_ts2 != null)
					_ts2(parameter);
				else
					_ts1();
			} finally {
				_startState = 3; // ensure parent thread continues
				
				// Inherit notify thread-local variables of termination
				for (int i = 0; i < _TLVs.Count; i++) {
					ThreadLocalVariableBase v = _TLVs[i].Target;
					if (v != null)
						v.Terminate(_thread.ManagedThreadId);
				}

				if (ThreadStopping != null)
					ThreadStopping(this, new ThreadStartEventArgs(_parent, this));
			}
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
		/// Returns the current domain in which the current thread is running.
		/// </summary>
		public static AppDomain GetDomain() { return Thread.GetDomain(); }
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

		internal static void RegisterTLV(ThreadLocalVariableBase tlv)
		{
			lock(_TLVs) {
				for (int i = 0; i < _TLVs.Count; i++)
					if (!_TLVs[i].IsAlive) {
						_TLVs[i].Target = tlv;
						return;
					}
				_TLVs.Add(new WeakReference<ThreadLocalVariableBase>(tlv));
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

	public class WeakReference<T> : System.WeakReference
	{
		public WeakReference(T target) : base(target) { }
		public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection) { }
		#if !WindowsCE && !SmartPhone && !PocketPC
		protected WeakReference(SerializationInfo info, StreamingContext context) : base(info, context) {}
		#endif
		public new T Target
		{
			get { return (T)base.Target; }
			set { base.Target = value; }
		}
	}

	public abstract class ThreadLocalVariableBase
	{
		internal abstract void Propagate(int parentThreadId, int childThreadId);
		internal abstract void Terminate(int threadId);
	}

	/// <summary>Provides access to a thread-local variable through a dictionary 
	/// that maps thread IDs to values.</summary>
	/// <typeparam name="T">Type of variable to wrap</typeparam>
	/// <remarks>
	/// My benchmark shows that using a dictionary is slightly faster than the
	/// ThreadStatic attribute in the single-threaded case. The purpose of 
	/// this class is not better performance, however, but value propagation.
	/// When used with ThreadEx.Start() to start child threads, the value of
	/// each ThreadLocalVariable object is automatically copied from the parent 
	/// thread to the child thread.
	/// <para/>
	/// 
	/// Variables of this type should always be static and they should not be 
	/// marked with the [ThreadStatic] attribute.
	/// 
	/// ThreadLocalVariable(of T) is less convenient than the [ThreadStatic]
	/// attribute, but ThreadLocalVariable's default constructor calls 
	/// ThreadEx.AllocateDataSlot so that the variable's value is inherited in 
	/// child threads. Also, [ThreadStatic] is not available in the Compact 
	/// Framework.
	/// </remarks>
	public class ThreadLocalVariable<T> : ThreadLocalVariableBase
	{
		public delegate TResult Func<TArg0, TResult>(TArg0 arg0);

		protected Dictionary<int, T> _tls = new Dictionary<int,T>(5);
		Func<T,T> _propagator = delegate(T v) { return v; };

		public ThreadLocalVariable()
		{
			ThreadEx.RegisterTLV(this);
		}

		/// <summary>Constructs a ThreadLocalVariable.</summary>
		/// <param name="initialValue">Initial value on the current thread. 
		/// Does not affect other threads that are already running.</param>
		public ThreadLocalVariable(T initialValue) 
			: this(initialValue, null) {}

		/// <summary>Constructs a ThreadLocalVariable.</summary>
		/// <param name="initialValue">Initial value on the current thread. 
		/// Does not affect other threads that are already running.</param>
		/// <param name="propagator">A function that copies (and possibly 
		/// modifies) the Value from a parent thread when starting a new 
		/// thread.</param>
		public ThreadLocalVariable(T initialValue, Func<T,T> propagator)
		{
			Value = initialValue;
			if (propagator != null)
				_propagator = propagator;
			ThreadEx.RegisterTLV(this);
		}

		internal override void Propagate(int parentThreadId, int childThreadId)
		{
			T value;
			lock(_tls) {
				_tls.TryGetValue(parentThreadId, out value);
				_tls[childThreadId] = _propagator(value);
			}
		}
		internal override void Terminate(int threadId)
		{
			_tls.Remove(CurrentThreadId);
		}

		internal int CurrentThreadId 
		{
			get { return Thread.CurrentThread.ManagedThreadId; } 
		}

		public bool HasValue
		{
			get { lock(_tls) { return _tls.ContainsKey(CurrentThreadId); } }
		}

		public T Value { 
			get {
				T value;
				lock(_tls) {
					_tls.TryGetValue(CurrentThreadId, out value);
				}
				return value;
			}
			set {
				lock(_tls) {
					_tls[CurrentThreadId] = value;
				}
			}
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
			});

			Assert.IsFalse(t.IsAlive);
			Assert.AreEqual(System.Threading.ThreadState.Unstarted, t.ThreadState);
			t.Start(123);
			Assert.IsTrue(t.IsAlive);
			Assert.IsTrue(eventOccurred);
			Assert.IsTrue(eventOk);
			while(!started)
				ThreadEx.Sleep(0);
			Assert.AreEqual(System.Threading.ThreadState.Running, t.ThreadState);
			stop = true;
			Assert.IsTrue(t.Join(5000));
			Assert.IsTrue(valueOk);
			Assert.IsFalse(started);
		}
	}
}
