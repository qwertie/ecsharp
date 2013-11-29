/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:47 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Loyc.Collections
{
	/// <summary>A sequence that simply repeats the same value a specified number 
	/// of times, returned from <see cref="Range.Repeat{T}"/>.</summary>
	[Serializable]
	public struct Repeated<T> : IListAndListSource<T>, IRange<T>
	{
		int _count;
		T _value;

		public Repeated(T value, int count)
		{
			_count = count;
			_value = value;
		}

		#region IListSource<T> Members

		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count)
				return _value;
			fail = true;
			return default(T);
		}

		public int Count
		{
			get { return _count; }
		}

		public T this[int index]
		{ 
			get {
				bool fail = false;
				T value = TryGet(index, ref fail);
				if (fail)
					CheckParam.ThrowIndexOutOfRange(index, _count);
				return value;
			}
		}
		
		public int IndexOf(T item)
		{
			return LCInterfaces.IndexOf(this, item);
		}

		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return Slice(start, count); 
		}
		public Slice_<T> Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count); 
		}

		#endregion

		#region IList<T> Members

		T IList<T>.this[int index]
		{
			get { return this[index]; }
			set { throw new ReadOnlyException(); }
		}
		void IList<T>.Insert(int index, T item)
		{
			throw new ReadOnlyException();
		}
		void IList<T>.RemoveAt(int index)
		{
			throw new ReadOnlyException();
		}
		
		#endregion

		#region ICollection<T> Members

		void ICollection<T>.Add(T item)
		{
			throw new ReadOnlyException();
		}
		void ICollection<T>.Clear()
		{
			throw new ReadOnlyException();
		}
		void ICollection<T>.CopyTo(T[] array, int arrayIndex)
		{
			LCInterfaces.CopyTo(this, array, arrayIndex);
		}
		bool ICollection<T>.IsReadOnly
		{
			get { return true; }
		}
		bool ICollection<T>.Remove(T item)
		{
			throw new ReadOnlyException();
		}
		public bool Contains(T item)
		{
			return Enumerable.Contains(this, item);
		}

		#endregion

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < _count; i++)
				yield return _value;
		}

		#region IRange<T>

		public bool IsEmpty
		{
			get { return _count <= 0; }
		}

		public T Back
		{
			get { return Front; }
		}
		public T PopBack(out bool fail)
		{
			return PopBack(out fail);
		}

		public T Front
		{
			get { 
				if (_count <= 0)
					throw new EmptySequenceException(); 
				return _value; 
			}
		}
		public T PopFront(out bool fail)
		{
			if (!(fail = _count <= 0)) {
				_count--;
				return _value;
			} else
				return default(T);
		}

		IFRange<T> ICloneable<IFRange<T>>.Clone() { return Clone(); }
		IBRange<T> ICloneable<IBRange<T>>.Clone() { return Clone(); }
		IRange<T> ICloneable<IRange<T>>.Clone() { return Clone(); }
		public Repeated<T> Clone()
		{
			return new Repeated<T>(_value, _count);
		}

		#endregion
	}
}
