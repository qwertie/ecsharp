// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	public class EmptyCollection<T> : IList<T>, IListSource<T>
	{
		public static readonly EmptyCollection<T> Default = new EmptyCollection<T>();

		public int IndexOf(T item)
		{
			return -1;
		}
		public void Insert(int index, T item)
		{
			ReadOnly();
		}
		private void ReadOnly()
		{
			throw new InvalidOperationException("Collection is read-only");
		}
		public void RemoveAt(int index)
		{
			ReadOnly();
		}
		public T this[int index]
		{
			get {
				throw new IndexOutOfRangeException();
			}
			set {
				throw new IndexOutOfRangeException();
			}
		}
		public T this[int index, T defaultValue]
		{
			get { return defaultValue; }
		}
		public void Add(T item)
		{
			ReadOnly();
		}
		public void Clear()
		{
		}
		public bool Contains(T item)
		{
			return false;
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
		}
		public int Count
		{
			get { return 0; }
		}
		public bool IsReadOnly
		{
			get { return true; }
		}
		public bool Remove(T item)
		{
			return false;
		}
		public IEnumerator<T> GetEnumerator()
		{
			return EmptyEnumerator<T>.Default;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return EmptyEnumerator<T>.Default;
		}
		public Iterator<T> GetIterator()
		{
			return Iterator_<T>.Empty;
		}
	}
}
