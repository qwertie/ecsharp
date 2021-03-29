using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Adapter: a <see cref="IBinumerator{T}"/> that swaps the MoveNext() and MovePrev() methods.</summary>
	public struct ReverseBinumerator<T> : IBinumerator<T>
	{
		IBinumerator<T> _e;
		public ReverseBinumerator(IBinumerator<T> realEnumerator)
		{
			_e = realEnumerator;
		}
		public bool MovePrev()
		{
			return _e.MoveNext();
		}
		public bool MoveNext()
		{
			return _e.MovePrev();
		}
		public T Current => _e.Current;
		void IDisposable.Dispose() { _e.Dispose(); }
		object? System.Collections.IEnumerator.Current => _e.Current;
		public void Reset() { _e.Reset(); }
	}
}
