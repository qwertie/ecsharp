/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 9:28 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace Loyc.Collections
{
	public static partial class LCExt
	{
		/// <summary>Adapter: treats any IReadOnlyCollection{T} as a read-only ICollection{T}.</summary>
		/// <remarks>This method is named "AsCollection" and not "ToCollection" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence, although it does create a new wrapper object.</remarks>
		public static ICollection<T> AsCollection<T>(this IReadOnlyCollection<T> c)
		{
			var list = c as ICollection<T>;
			if (list != null)
				return list;
			return new ReadOnlyAsCollection<T>(c);
		}
	}

	/// <summary>
	/// A read-only wrapper that implements ICollection(T) and ISource(T),
	/// returned from <see cref="LCExt.AsCollection{T}"/>
	/// </summary>
	[Serializable]
	public sealed class ReadOnlyAsCollection<T> : WrapperBase<IReadOnlyCollection<T>>, ICollectionAndReadOnly<T>
	{
		public ReadOnlyAsCollection(IReadOnlyCollection<T> obj) : base(obj) { }

		#region ICollection<T> Members

		public void Add(T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void Clear()
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public int Count
		{
			get { return _obj.Count; }
		}
		public bool Contains(T item)
		{
			return _obj.Contains(item);
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			ListExt.CopyTo(_obj, array, arrayIndex);
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
			return GetEnumerator();
		}

		#endregion
	}
}
