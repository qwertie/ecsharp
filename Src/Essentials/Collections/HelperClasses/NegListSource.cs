/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/12/2011
 * Time: 7:15 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>
	/// Provides a view of an <see cref="IListSource{T}"/> in which the Count is the same, but the 
	/// minimum index is not necessarily zero. Returned from <see cref="LCExt.NegView{T}(IListSource{T},int)"/>.
	/// </summary>
	/// <remarks>
	/// This wrapper is a structure in order to offer high performance in certain 
	/// scenarios.
	/// <para/>
	/// Like ListSourceSlice, this structure provides a view of another list 
	/// starting at a certain offset. Unlike ListSourceSlice, however, this 
	/// structure allows the caller to access the entire original list, not just a 
	/// slice.
	/// </remarks>
	[Serializable]
	public struct NegListSource<T> : INegListSource<T>, IEquatable<NegListSource<T>>
	{
		public static readonly NegListSource<T> Empty = new NegListSource<T>(EmptyList<T>.Value, 0);

		/// <summary>Gets the list that was passed to the constructor of this instance.</summary>
		public IListSource<T> OriginalList { get { return _list; } }
		private IListSource<T> _list;
		
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
		public NegListSource(IListSource<T> list, int zeroOffset)
		{
			if (list == null)
				throw new ArgumentNullException("wrappedObject");
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
		}
		/// <inheritdoc/>
		public T TryGet(int index, ref bool fail)
		{
			return _list.TryGet(_offset + index, ref fail);
		}
		
		public IEnumerator<T> GetEnumerator() { return _list.GetEnumerator(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _list.GetEnumerator(); }
		public Iterator<T> GetIterator() { return _list.GetIterator(); }
		
		public bool Equals(NegListSource<T> rhs)
		{
			return rhs._offset == _offset && (rhs._list == _list || object.Equals(rhs._list, _list));
		}
		/// <inheritdoc cref="Loyc.Essentials.WrapperBase{T}.Equals"/>
		public override bool Equals(object obj)
		{
			if (obj is NegListSource<T>)
				return Equals((NegListSource<T>)obj);
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
