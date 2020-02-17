using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;

namespace Loyc.Collections
{
	/// <summary>Helper struct: enumerates through a forward range (<see cref="IFRange{T}"/>).</summary>
	/// <seealso cref="RangeEnumerator{R, T}"/>
	[Obsolete("Not in use. If you are using this, please leave an issue at https://github.com/qwertie/ecsharp/ to have the deprecation cancelled.")]
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
}
