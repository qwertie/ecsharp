/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/12/2011
 * Time: 8:03 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <inheritdoc cref="NegList{T}.NegList"/>
		public static NegList<T> AsNegList<T>(this IList<T> list, int zeroOffset)
		{
			return new NegList<T>(list, zeroOffset);
		}
		/// <inheritdoc cref="NegList{T}.NegList"/>
		public static NegList<T> AsNegList<T>(this IListAndListSource<T> list, int zeroOffset)
		{
			return new NegList<T>(list, zeroOffset);
		}
	}

	/// <summary>
	/// Adapter: provides a view of an <see cref="IList{T}"/> in which the Count is the same, but the 
	/// minimum index is not necessarily zero. Returned from <see cref="LCExt.AsNegList{T}(IList{T},int)"/>.
	/// </summary>
	/// <remarks>This wrapper is a structure in order to offer high performance in 
	/// certain scenarios.</remarks>
	[Serializable]
	public struct NegList<T> : INegArray<T>, IEquatable<NegList<T>>
	{
		public static readonly NegList<T> Empty = new NegList<T>(EmptyList<T>.Value, 0);

		/// <summary>Gets the list that was passed to the constructor of this instance.</summary>
		public IList<T> OriginalList { get { return _list; } }
		private IList<T> _list;
		
		/// <summary>Returns the offset added to indexes in the original list, which equals -Min.</summary>
		/// <remarks>The 0th item in this list the same as OriginalList[Offset].</remarks>
		/// <remarks>
		/// WARNING: this is a value type. Calling the setter may have unexpected
		/// consequences for people unfamiliar with the .NET type system, because 
		/// it is easy to make copies accidentally, and changing the Offset in a copy
		/// does not change the Offset in the original.
		/// </remarks>
		public int Offset { get { return _offset; } set { _offset = value; } }
		private int _offset;

		/// <summary>Initializes a NegListSource wrapper.</summary>
		/// <param name="list">A list to wrap (must not be null).</param>
		/// <param name="zeroOffset">An index into the original list. this[0] will refer to that index.</param>
		/// <remarks>The zeroOffset can be any integer, but if it is not in the range 0 to list.Count-1, this[0] will not be valid.</remarks>
		public NegList(IList<T> list, int zeroOffset)
		{
			if (list == null)
				throw new ArgumentNullException("list");
			_list = list;
			_offset = zeroOffset;
		}
		
		/// <summary>Returns the total number of items in the list (same as OriginalList.Count).</summary>
		public int Count { get { return _list.Count; } }
		/// <summary>Returns the minimum valid index.</summary>
		public int Min { get { return -_offset; } }
		/// <summary>Returns the maximum valid index, which is Min + OriginalList.Count - 1.</summary>
		public int Max { get { return ~_offset + _list.Count; } }
		/// <summary>Gets the value of the list at the specified index. In terms 
		/// of the original list, this is OriginalList[index + Offset]</summary>
		/// <param name="index">An index in the range Min to Max.</param>
		/// <exception cref="ArgumentOutOfRangeException">The index provided is not 
		/// valid in this list.</exception>
		public T this[int index]
		{
			get { return _list[_offset + index]; }
			set { _list[_offset + index] = value; }
		}
		/// <inheritdoc/>
		public T TryGet(int index, out bool fail)
		{
			index += _offset;
			fail = false;
			if ((uint)index < (uint)_list.Count)
				return _list[index];
			fail = true;
			return default(T);
		}
		/// <inheritdoc/>
		public bool TrySet(int index, T value)
		{
			index += _offset;
			if ((uint)index < (uint)_list.Count) {
				_list[index] = value;
				return true;
			}
			return false;
		}
		
		/// <summary>Returns a sub-range of this list.</summary>
		public IRange<T> Slice(int start, int count = int.MaxValue)
		{
			return _list.Slice(_offset + start, count);
		}

		public IEnumerator<T> GetEnumerator() { return _list.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _list.GetEnumerator(); }
		
		public bool Equals(NegList<T> rhs)
		{
			return rhs._offset == _offset && (rhs._list == _list || object.Equals(rhs._list, _list));
		}
		public static bool operator ==(NegList<T> a, NegList<T> b) { return a.Equals(b); }
		public static bool operator !=(NegList<T> a, NegList<T> b) { return !a.Equals(b); }
		/// <inheritdoc cref="Loyc.WrapperBase{T}.Equals"/>
		public override bool Equals(object obj)
		{
			if (obj is NegList<T>)
				return Equals((NegList<T>)obj);
			return false;
		}
		public override int GetHashCode()
		{
			return _list.GetHashCode() ^ _offset.GetHashCode();
		}
		/// <summary>Returns ToString() of the wrapped list.</summary>
		public override string ToString()
		{
			return _list.ToString();
		}
	}
}
