using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>A compact auto-enlarging list that efficiently supports
	/// supports insertions at the beginning or end of the list.
	/// </summary>
	[Serializable()]
	public class DList<T> : IListEx<T>, IDeque<T>
	{
		protected InternalDList<T> _deque = InternalDList<T>.Empty;

		public DList(int capacity)     { Capacity = capacity; }
		public DList(IIterable<T>   items) { PushLast(items); }
		public DList(ISource<T>     items) { PushLast(items); }
		public DList(ICollection<T> items) { PushLast(items); }
		public DList(IEnumerable<T> items) { PushLast(items); }
		public DList() { }

		private void CheckPopCount(int amount)
		{
			if (amount < 0)
	 			throw new InvalidOperationException(string.Format("Can't pop a negative number of elements ({0})", amount));
			if (amount > _deque.Count)
	 			throw new InvalidOperationException(string.Format("Can't pop more elements than Deque<{0}> contains ({1}>{2})", typeof(T).Name, amount, Count));
		}

		public int IndexOf(T item)
		{
			return _deque.IndexOf(item);
		}

		public void PushLast(ICollection<T> items)
		{
			_deque.PushLast(items);
		}
		public void PushLast(IEnumerable<T> items)
		{
			_deque.PushLast(items);
		}
		public void PushLast(ISource<T> items)
		{
			_deque.PushLast(items);
		}
		public void PushLast(IIterable<T> items)
		{
			_deque.PushLast(items);
		}

		public void PushLast(T item)
		{
			_deque.PushLast(item);
		}
		
		public void PushFirst(T item)
		{
			_deque.PushFirst(item);
		}

		public void PopLast(int amount)
		{
			CheckPopCount(amount);
			_deque.PopLast(amount);
		}

		public void PopFirst(int amount)
		{
			CheckPopCount(amount);
			_deque.PopFirst(amount);
		}

		public int Capacity
		{
			get { return _deque.Capacity; }
			set {
				if (value < _deque.Count)
					throw new ArgumentOutOfRangeException(string.Format("Capacity is too small ({0}<{1})", value, Count));
				_deque.Capacity = value;
			}
		}

		public void Insert(int index, T item)
		{
			CheckInsertIndex(index);
			_deque.Insert(index, item);
		}

		public void InsertRange(int index, ICollection<T> items)
		{
			CheckInsertIndex(index);
			_deque.InsertRange(index, items);
		}

		public void InsertRange(int index, ISource<T> items)
		{
			CheckInsertIndex(index);
			_deque.InsertRange(index, items);
		}
		
		void CheckInsertIndex(int index)
		{
			if ((uint)index > (uint)_deque.Count)
				throw new IndexOutOfRangeException(string.Format("Invalid index in Deque<{0}> ({1}∉[0,{2}])", typeof(T).Name, index, Count));
		}

		public void RemoveAt(int index)
		{
			CheckRemoveIndex(index, 1);
			_deque.RemoveAt(index);
		}
		public void RemoveRange(int index, int amount)
		{
			if (amount < 0)
				throw new ArgumentOutOfRangeException("amount");
			CheckRemoveIndex(index, amount);
			_deque.RemoveRange(index, amount);
		}
		void CheckRemoveIndex(int index, int amount)
		{
			if ((uint)index > (uint)_deque.Count || (uint)(index + amount) > (uint)_deque.Count)
				throw new IndexOutOfRangeException(string.Format("Invalid removal range in Deque<{0}> ([{1},{2})⊈[0,{3}))", typeof(T).Name, index, index + amount, Count));
		}

		public T this[int index]
		{
			[DebuggerStepThrough]
			get {
				CheckIndex(index);
				return _deque[index];
			}
			[DebuggerStepThrough]
			set {
				CheckIndex(index);
				_deque[index] = value;
			}
		}
		private void CheckIndex(int index)
		{
			if ((uint)index >= (uint)_deque.Count)
				throw new IndexOutOfRangeException(string.Format("Invalid index in Deque<{0}> ({1}∉[0,{2}))", typeof(T).Name, index, Count));
		}

		public bool TrySet(int index, T value)
		{
			return _deque.TrySet(index, value);
		}
		public T TryGet(int index, ref bool fail)
		{
			return _deque.TryGet(index, ref fail);
		}

		/// <summary>An alias for PushLast().</summary>
		public void Add(T item)
		{
			_deque.PushLast(item);
		}

		public void Clear()
		{
			_deque.Clear();
		}

		public bool Contains(T item)
		{
			return _deque.IndexOf(item) > -1;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null || array.Length < _deque.Count)
				throw new ArgumentOutOfRangeException("array");
			if (arrayIndex < 0 || array.Length - arrayIndex < _deque.Count)
				throw new ArgumentOutOfRangeException("arrayIndex");
			_deque.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _deque.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return _deque.Remove(item);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _deque.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _deque.GetEnumerator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			return _deque.GetEnumerator();
		}

		public Iterator<T> GetIterator()
		{
			return _deque.GetIterator();
		}

		#region IDeque<T>

		public T TryPopFirst(ref bool isEmpty)
		{
			return _deque.TryPopFirst(ref isEmpty);
		}
		public T TryPeekFirst(ref bool isEmpty)
		{
			return _deque.TryPeekFirst(ref isEmpty);
		}
		public T TryPopLast(ref bool isEmpty)
		{
			return _deque.TryPopLast(ref isEmpty);
		}
		public T TryPeekLast(ref bool isEmpty)
		{
			return _deque.TryPeekLast(ref isEmpty);
		}

		public T First
		{
			get { return _deque.First; }
			set { _deque.First = value; }
		}
		public T Last
		{
			get { return _deque.Last; }
			set { _deque.Last = value; }
		}
		public bool IsEmpty
		{
			get { return _deque.IsEmpty; }
		}

		#endregion

		public int BinarySearch(T k, Comparer<T> comp)
		{
			return _deque.BinarySearch(k, comp);
		}
		public int BinarySearch<K>(K k, Func<T, K, int> comp)
		{
			return _deque.BinarySearch(k, comp);
		}
	}

	[Serializable()]
	public class Deque : DList<object>, System.Collections.IList
	{
		public bool IsFixedSize
		{
			get { return false; }
		}
		public void CopyTo(Array array, int arrayIndex)
		{
			if (array == null || array.Length < Count)
				throw new ArgumentOutOfRangeException("array");
			if (arrayIndex < 0 || array.Length - arrayIndex < Count)
				throw new ArgumentOutOfRangeException("arrayIndex");
			
			foreach(object obj in this)
				array.SetValue(obj, arrayIndex++);
		}
		public bool IsSynchronized
		{
			get { return false; }
		}
		public object SyncRoot
		{
			get { return this; }
		}
		public new void Remove(object obj)
		{
			base.Remove(obj);
		}
		public new int Add(object obj)
		{
			base.Add(obj);
			return Count - 1;
		}
	}
}
