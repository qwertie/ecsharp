using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Loyc.Math;

namespace Loyc.Collections
{
	public static partial class ListExt
	{
		public static IRange<T> AsRange<T>(this IListSource<T> list)
		{
			var range = (list as IRange<T>);
			if (range != null)
				return range;
			return new Slice_<T>(list, 0, int.MaxValue); // boxed
		}
		[Obsolete("The object is already a range; the AsRange() method is a no-op.")]
		public static IRange<T> AsRange<T>(this IRange<T> list)
		{
			return list;
		}
	}

	/// <summary>Adapter: a random-access range for a slice of an 
	/// <see cref="IListSource{T}"/>.</summary>
	/// <typeparam name="T">Item type in the list</typeparam>
	/// <remarks>
	/// This type was supposed to be called simply <c>Slice</c>, but this was not
	/// allowed because in plain C#, "CS0542: member names cannot be the same as 
	/// their enclosing type" and of course, this type contains the Slice() method
	/// from IListSource.
	/// </remarks>
	public struct Slice_<T> : IRange<T>, ICloneable<Slice_<T>>, IIsEmpty
	{
		[Obsolete("I doubt anyone is using this.")]
		public static readonly Slice_<T> Empty = new Slice_<T>();

		IListSource<T> _list;
		int _start, _count;
		
		/// <summary>Initializes a slice.</summary>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as 'start' and 'count' are zero or above. 
		/// <ul>
		/// <li>If 'start' is above the original Count, the Count of the new slice 
		/// is set to zero.</li>
		/// <li>if (start + count) is above the original Count, the Count of the new
		/// slice is reduced to <c>list.Count - start</c>.</li>
		/// </ul>
		/// </remarks>
		public Slice_(IListSource<T> list, int start, int count = int.MaxValue)
		{
			_list = list;
			_start = start;
			_count = CheckParam.ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(start, count, _list.Count);
		}
		public Slice_(IListSource<T> list)
		{
			_list = list;
			_start = 0;
			_count = list.Count;
		}

		public int Count
		{
			get { return _count; }
		}
		public bool IsEmpty
		{
			get { return _count == 0; }
		}
		public T First
		{
			get { return this[0]; }
		}
		public T Last
		{
			get { return this[_count - 1]; }
		}

		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("empty")]
		public T PopFirst(out bool empty)
		{
			if (_count != 0) {
				empty = false;
				_count--;
				return _list[_start++];
			}
			empty = true;
			return default(T);
		}
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("empty")]
		public T PopLast(out bool empty)
		{
			if (_count != 0) {
				empty = false;
				_count--;
				return _list[_start + _count];
			}
			empty = true;
			return default(T);
		}

		IFRange<T> ICloneable<IFRange<T>>.Clone() { return Clone(); }
		IBRange<T> ICloneable<IBRange<T>>.Clone() { return Clone(); }
		IRange<T>  ICloneable<IRange<T>> .Clone() { return Clone(); }
		public Slice_<T> Clone() { return this; }

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public RangeEnumerator<Slice_<T>,T> GetEnumerator()
		{
			return new RangeEnumerator<Slice_<T>, T>(this);
		}

		public T this[int index]
		{
			get { 
				if ((uint)index < (uint)_count)
					return _list[_start + index];
				throw new ArgumentOutOfRangeException("index");
			}
		}
		public T this[int index, T defaultValue]
		{
			get {
				if ((uint)index < (uint)_count) {
					bool fail;
					var r = _list.TryGet(_start + index, out fail);
					return fail ? defaultValue : r!;
				}
				return defaultValue;
			}
		}
		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_count)
				return _list.TryGet(_start + index, out fail);
			fail = true;
			return default(T);
		}

		IListSource<T> IListSource<T>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<T> Slice(int start, int count = int.MaxValue)
		{
			var slice = new Slice_<T>();
			slice._list = this._list;
			slice._start = this._start + start;
			slice._count = CheckParam.ThrowIfStartOrCountAreBelowZeroAndLimitCountIfNecessary(start, count, this._count);
			return slice;
		}
	}
}
