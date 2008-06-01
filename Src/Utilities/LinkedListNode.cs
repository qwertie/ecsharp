using System;
using System.Collections.Generic;
using System.Text;
using D = System.Diagnostics.Debug;

namespace Mentor.Utilities
{
	/// <summary>
	/// This class can be used to form a linked list; note that there
	/// is no "master" class for managing the list. Each instance of this
	/// class has a Value inside of type T.
	/// </summary>
	/// <typeparam name="T">The type of value stored in each node</typeparam>
	/// <remarks>
	/// I wrote this class without realizing that the .NET Framework 2.0 has
	/// its own class by the same name! Oops! But I'm keeping this class 
	/// because it has a small performance advantage in not needing a master
	/// LinkedList<T> class.
	/// </remarks>
	class LinkedListNode<T> : IEnumerable<T>
	{
		protected LinkedListNode<T> _n, _p;
		protected T _v;

		public LinkedListNode(T value) { _v = value; }
		public LinkedListNode(T value, bool circular) { 
			_v = value; 
			if (circular) 
				_n = _p = this;
		}

		public T Value { get { return _v; } set { _v = value; } }
		public LinkedListNode<T> Next { get { return _n; } }
		public LinkedListNode<T> Previous { get { return _p; } }

		public LinkedListNode<T> Unlink()
		{
			LinkedListNode<T> result = _n;
			D.Assert((_p == this) == (_n == this));

			if (_n != null)
			{
				D.Assert(_n._p == this);
				_n._p = _p;
			}
			if (_p != null)
			{
				D.Assert(_p._n == this);
				_p._n = _n;
				_p = null;
			}
			_n = null;
			return result;
		}
		public void InsertAfter(LinkedListNode<T> other)
		{
			if (other == this || other == null)
				return; // no-op

			Unlink();
			_p = other;
			_n = other._n;
			_p._n = this;
			_n._p = this;
		}
		public void InsertBefore(LinkedListNode<T> other)
		{
			if (other == this || other == null)
				return; // no-op

			Unlink();
			_p = other._p;
			_n = other;
			_p._n = this;
			_n._p = this;
		}

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			LinkedListNode<T> node = this;
			do {
				yield return node.Value;
				node = node.Next;
			} while (node != null && node != this);
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
