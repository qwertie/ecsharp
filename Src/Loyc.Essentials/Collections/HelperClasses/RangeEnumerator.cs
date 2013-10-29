using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;

namespace Loyc.Collections
{
	/// <summary>Enumerates through a forward range (<see cref="IFRange{T}"/>).</summary>
	public struct RangeEnumerator<T> : IEnumerator<T>
	{
		IFRange<T> _range;
		T _current;
		public RangeEnumerator(IFRange<T> range) { _range = range.Clone(); _current = default(T); }

		public bool MoveNext() { bool empty; _current = _range.PopFront(out empty); return !empty; }
		public T Current { get { return _current; } }

		object System.Collections.IEnumerator.Current { get { return Current; } }
		void IDisposable.Dispose() { }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
	}

	/// <summary>Enumerates through a forward range (<see cref="IFRange{T}"/>), 
	/// calling the range methods through R instead of through <see cref="IFRange{T}"/>.
	/// </summary>
	/// <remarks>Although there is a ICloneable{R} constraint on R, it is currently
	/// worthless due to a limitation of C#. Since <see cref="IFRange{T}"/> already
	/// includes <c>ICloneable(IFRange(T))</c>, this structure cannot simply
	/// invoke Clone() directly because the compiler complains that Clone() is 
	/// ambiguous. Consequently it is necessary to cast the range to <see 
	/// cref="ICloneable{R}"/> just to clone it; if R is a value type then it is
	/// boxed, which defeats the entire performance advantage of calling 
	/// <c>ICloneable{R}.Clone()</c> instead of <c>ICloneable{IFRange{T}}.Clone</c>.
	/// <para/>
	/// Nevertheless, I have left the constraint in place, in the hope that EC# can 
	/// eventually eliminate this limitation thanks to its "using" cast. Once EC#
	/// compiles directly to CIL, <c>range(using ICloneable&lt;R>).Clone()</c> will 
	/// not perform boxing.
	/// </remarks>
	public struct RangeEnumerator<R, T> : IEnumerator<T> where R : IFRange<T>, ICloneable<R>
	{
		R _range;
		T _current;
		public RangeEnumerator(R range) { _range = ((ICloneable<R>)range).Clone(); _current = default(T); }

		public bool MoveNext() { bool empty; _current = _range.PopFront(out empty); return !empty; }
		public T Current { get { return _current; } }

		object System.Collections.IEnumerator.Current { get { return Current; } }
		void IDisposable.Dispose() { }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
	}
}
