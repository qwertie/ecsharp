using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;

namespace Loyc.Collections
{
	/// <summary>Helper struct: enumerates through a forward range (<see cref="IFRange{T}"/>).</summary>
	public struct RangeEnumerator<T> : IEnumerator<T>
	{
		IFRange<T> _range;
		T _current;
		public RangeEnumerator(IFRange<T> range) { _range = range.Clone(); _current = default(T); }

		public bool MoveNext() { bool empty; _current = _range.PopFirst(out empty); return !empty; }
		public T Current { get { return _current; } }

		object System.Collections.IEnumerator.Current { get { return Current; } }
		void IDisposable.Dispose() { }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
	}

	/// <summary>Helper struct: enumerates through a forward range (<see cref="IFRange{T}"/>), 
	/// calling the range methods through R instead of through <see cref="IFRange{T}"/>.
	/// </summary>
	public struct RangeEnumerator<R, T> : IEnumerator<T> where R : IFRange<T>, ICloneable<R>
	{
		R _range;
		T _current;
		public RangeEnumerator(R range) { _range = R_Clone<R>(range); _current = default(T); }

		public bool MoveNext() { bool empty; _current = _range.PopFirst(out empty); return !empty; }
		public T Current { get { return _current; } }

		object System.Collections.IEnumerator.Current { get { return Current; } }
		void IDisposable.Dispose() { }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }

		/// <summary>Since R implements IFRange{T} which includes ICloneable{IFRange{T}},
		/// we cannot invoke ICloneable{R}.Clone because the compiler complains that 
		/// Clone() is ambiguous. I used to think it was necessary to cast the range to <see 
		/// cref="ICloneable{R}"/> just to clone it; if R is a value type then it is
		/// boxed, hurting performance. But then I thought of doing this.</summary>
		static R_ R_Clone<R_>(R_ r) where R_ : ICloneable<R_> { return r.Clone(); }
	}
}
