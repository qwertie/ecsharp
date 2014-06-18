/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 9:27 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <summary>Converts any ICollection{T} object to IReadOnlyCollection{T}.</summary>
		/// <remarks>This method is named "AsReadOnly" and not "ToReadOnly" because,
		/// in contrast to methods like ToArray(), and ToList() it does not make a 
		/// copy of the sequence, although it does create a new wrapper object.</remarks>
		public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> c)
		{
			var list = c as IReadOnlyCollection<T>;
			if (list != null)
				return list;
			return new CollectionAsReadOnly<T>(c);
		}
	}

	/// <summary>A read-only wrapper that implements ICollection(T) and ISource(T), 
	/// returned from <see cref="LCExt.AsReadOnly{T}"/>.</summary>
	[Serializable]
	public sealed class CollectionAsReadOnly<T> : WrapperBase<ICollection<T>>, ICollectionAndReadOnly<T>
	{
		public CollectionAsReadOnly(ICollection<T> obj) : base(obj) { }

		public int Count
		{
			get { return _obj.Count; }
		}
		public bool Contains(T item)
		{
			return _obj.Contains(item);
		}

		#region ICollection<T> Members

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
