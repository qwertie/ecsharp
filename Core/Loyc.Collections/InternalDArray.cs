using Loyc.Collections.Impl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	public struct InternalDArray<T> : IAutoNegArray<T>, ICloneable<InternalDArray<T>>
	{
		public static InternalDArray<T> Empty = new InternalDArray<T> { _list = InternalDList<T>.Empty };

		InternalDList<T> _list;
		int _startIndex;

		public T this[int index] {
			get {
				int mappedIndex = index - _startIndex;
				if ((uint)mappedIndex < (uint)_list.Count)
					return _list[mappedIndex];
				throw new IndexOutOfRangeException();
			}
			set {
				int mappedIndex = index - _startIndex;
				if ((uint)mappedIndex < (uint)_list.Count)
					_list[mappedIndex] = value;
				else if (mappedIndex < 0) {
					_startIndex += mappedIndex;
					if (mappedIndex == -1) {
						_list.PushFirst(value);
					} else {
						_list.InsertRange(0, new Repeated<T>(default(T), -mappedIndex));
						_list[0] = value;
					}
				} else {
					if (mappedIndex > _list.Count)
						_list.InsertRange(_list.Count, new Repeated<T>(default(T), mappedIndex - _list.Count));
					_list.PushLast(value);
				}
			}
		}
		
		public T this[int index, T defaultValue] {
			get {
				T value = TryGet(index, out bool fail);
				return fail ? defaultValue : value;
			}
		}

		public int Min => _startIndex;
		public int Max => _startIndex + _list.Count - 1;
		public int Count => _list.Count;

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		public InternalDList<T>.Enumerator GetEnumerator() => _list.GetEnumerator();

		public IListSource<T> Slice(int start, int count) => new NegListSlice<T>(this, start, count);
		
		public T TryGet(int index, T defaultValue) => this[index, defaultValue];
		public T TryGet(int index, out bool fail)
		{
			int mappedIndex = index - _startIndex;
			if (fail = (uint)mappedIndex >= (uint)_list.Count)
				return default(T);
			else
				return _list[mappedIndex];
		}

		public bool TrySet(int index, T value)
		{
			int mappedIndex = index - _startIndex;
			if ((uint)mappedIndex < (uint)_list.Count) {
				_list[mappedIndex] = value;
				return true;
			}
			return false;
		}

		public InternalDArray<T> Clone() => new InternalDArray<T> { 
			_list = _list.Clone(), _startIndex = _startIndex 
		};
	}
}
