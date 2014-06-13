/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 9:03 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <summary>Converts any IList{T} object to IListSource{T}.</summary>
		/// <remarks>This method is named "AsListSource" and not "ToListSource" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence.</remarks>
		public static IListSource<T> AsListSource<T>(this IList<T> c)
		{
			if (c == null)
				return null;
			var listS = c as IListSource<T>;
			if (listS != null)
				return listS;
			return new ListAsListSource<T>(c);
		}
	}
	/// <summary>
	/// Helper type returned from <see cref="LCExt.AsListSource{T}"/>.
	/// </summary>
	/// <summary>A read-only wrapper that implements ICollection and ISource.</summary>
	[Serializable]
	public sealed class ListAsListSource<T> : WrapperBase<IList<T>>, IListAndListSource<T>
	{
		public ListAsListSource(IList<T> obj) : base(obj) { }

		public int Count
		{
			get { return _obj.Count; }
		}
		public bool Contains(T item)
		{
			return _obj.Contains(item);
		}
		public T this[int index]
		{
			get { return _obj[index]; }
			set { throw new NotSupportedException("Collection is read-only."); }
		}
		public T this[int index, T defaultValue]
		{
			get {
				if ((uint)index >= (uint)_obj.Count)
					return defaultValue;
				else
					return _obj[index];
			}
		}
		public T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_obj.Count) {
				fail = false;
				return _obj[index];
			}
			fail = true;
			return default(T);
		}
		public int IndexOf(T item)
		{
			return _obj.IndexOf(item);
		}
		IRange<T> IListSource<T>.Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}
		public Slice_<T> Slice(int start, int count)
		{
			return new Slice_<T>(this, start, count);
		}

		#region IList<T> Members

		public void Insert(int index, T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void Add(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void Clear()
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			_obj.CopyTo(array, arrayIndex);
		}
		public bool IsReadOnly
		{
			get { return true; }
		}
		public bool Remove(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public IEnumerator<T> GetEnumerator()
		{
			return _obj.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return (_obj as System.Collections.IEnumerable).GetEnumerator();
		}

		#endregion
	}
}
