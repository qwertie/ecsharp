using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections.Impl
{
	/// <summary>Represents a list of default(T) values.</summary>
	/// <typeparam name="T"></typeparam>
	public struct EmptySpace<T> : IListSource<T>, IRange<T>
	{
		public static readonly EmptySpace<T> Empty = new EmptySpace<T>();

		int _count;

		public EmptySpace(int count) { _count = count; CheckParam.IsNotNegative("count", count); }

		public void Resize(int newCount)
		{
			CheckParam.IsNotNegative("count", newCount);
			_count = newCount;
		}

		public T TryGet(int index, out bool fail)
		{
			fail = (uint)index >= (uint)_count;
			return default(T);
		}

		public IRange<T> Slice(int start, int count = int.MaxValue)
		{
			CheckParam.IsNotNegative("start", start);
			if (count < 0) count = 0;
			if (count > _count - start)
				count = System.Math.Max(_count - start, 0);
			return new EmptySpace<T>(count);
		}

		public T this[int index]
		{
			get {
				CheckParam.ArgRange("index", index, 0, _count - 1);
				return default(T);
			}
		}

		public int Count
		{
			get { return _count; }
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new Repeated<T>(default(T), _count).GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

		public bool IsEmpty { get { return _count == 0; } }
		public T Back { get { return default(T); } }
		public T Front { get { return default(T); } }

		public T PopBack(out bool fail)
		{
			if (!(fail = _count == 0)) _count--;
			return default(T);
		}
		public T PopFront(out bool fail)
		{
			return PopBack(out fail);
		}

		IRange<T>  ICloneable<IRange<T>>.Clone()  { return new EmptySpace<T>(_count); }
		IFRange<T> ICloneable<IFRange<T>>.Clone() { return new EmptySpace<T>(_count); }
		IBRange<T> ICloneable<IBRange<T>>.Clone() { return new EmptySpace<T>(_count); }
	}
}
