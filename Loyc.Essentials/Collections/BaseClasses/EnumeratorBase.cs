using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Base class to help you implement the standard IEnumerator{T}
	/// interface. All you have to do is override MoveNext() and, when successful,
	/// set the Current property.</summary>
	public abstract class EnumeratorBase<T> : IEnumerator<T>
	{
		public abstract bool MoveNext();
		private T _current;
		public T Current
		{
			get { return _current; }
			protected set { _current = value; }
		}
		public void Dispose()
		{
		}
		object System.Collections.IEnumerator.Current
		{
			get { return Current; }
		}
		public void Reset()
		{
			throw new NotSupportedException();
		}
	}
}
