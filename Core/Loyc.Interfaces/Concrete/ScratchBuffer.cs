using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loyc.Threading
{
	/// <summary>Holds a single Value that is associated with the thread that
	/// assigned it.</summary>
	/// <remarks>
	/// ScratchBuffer is typically used as a static variable to hold a temporary
	/// object used for operations that are done frequently and require a 
	/// temporary memory space--but only during the operation, not afterward.
	/// <para/>
	/// For example, CPTrie may require a temporary byte array during searches.
	/// Re-creating the byte array for every search might cause a too much time
	/// to be spent garbage-collecting. On the other hand, if CPTrie keeps a 
	/// reference to the temporary buffer in itself, what if a program contains 
	/// many instances of CPTrie? Each one would have its own separate temporary
	/// buffer, wasting memory. The buffer can't be a straightforward global
	/// variable, either, in case two threads need a scratch buffer at once.
	/// ScratchBuffer, then, exists to prevent two threads from using the same 
	/// buffer.
	/// <para/>
	/// ScratchBuffer is designed with the assumption that creating a scratch
	/// buffer is fast, but re-using an existing buffer is faster. Since 
	/// creating a scratch buffer is cheap already, this class is worthless 
	/// unless it is even cheaper. Therefore, it does not hold a buffer for 
	/// each thread, since managing multiple buffers would be too expensive; 
	/// and volatile variable access is used instead of locking.
	/// <para/>
	/// ScratchBuffer originally returned null if the scratch buffer had not 
	/// been initialized or was associated with a different thread, requiring
	/// the caller to create a new buffer manually. Now there is a new constructor 
	/// that takes a factory function, which is called automatically by the Value
	/// property if the scratch buffer is null or belongs to another thread. If
	/// you use this constructor, then you do longer have to worry about Value 
	/// returning null.
	/// <example>
	/// static ScratchBuffer&lt;byte[]&gt; _buf = 
	///    new ScratchBuffer&lt;byte[]&gt;(() => new byte[40]);
	///
	/// // A method called a million times that needs a scratch buffer each time
	/// void FrequentOperation()
	/// {
	///		byte[] buf = _buf.Value;
	///     
	///     // do something here involving the buffer ...
	/// }
	/// </example>
	/// Arguably it is better to use a [ThreadStatic] variable is instead of 
	/// ScratchBuffer, but FWIW [ThreadStatic] is not available on the .NET
	/// Compact Framework.
	/// </remarks>
	public struct ScratchBuffer<T> : IHasValue<T> where T : class
	{
		volatile int _threadID;
		volatile T _buffer;
		Func<T> _factory;

		public ScratchBuffer(Func<T> factory) { _threadID = 0; _buffer = default(T); _factory = factory; }

		/// <summary>Please see the documentation of <see cref="ScratchBuffer{T}"/> itself.</summary>
		public T Value
		{
			get {
				T buffer = _buffer;
				if (_threadID == Thread.CurrentThread.ManagedThreadId)
					return buffer;
				return CallFactory();
			}
			set {
				_threadID = int.MinValue;
				_buffer = value;
				_threadID = Thread.CurrentThread.ManagedThreadId;
			}
		}
		private T CallFactory()
		{
			// By putting this code (which I hope is called infrequently) in a 
			// separate method, I hope that the hot path (Value) will be inlined
			if (_factory == null)
				return null;
			T newT = _factory();
			Value = newT;
			return newT;
		}
	}
}
