// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	/// <summary>
	/// Encapsulates GetIterator() and a Count property.
	/// </summary>
	/// <remarks>
	/// Member list:
	/// <code>
	/// public Iterator&lt;T> GetIterator();
	/// public int Count { get; }
	/// public IEnumerator&lt;T> GetEnumerator();
	/// System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator();
	/// </code>
	/// The term "source" means a read-only collection, as opposed to a "sink"
	/// which would be a write-only collection. The purpose of ISource is to make
	/// it easier to implement a read-only collection, by lifting the requirement
	/// to write implementations for Add(), Remove(), etc.
	/// <para/>
	/// Originally this interface had a Contains() method, just in case the source 
	/// had some kind of fast lookup logic (e.g. hashtable). However, this is 
	/// not allowed in C# 4 when T is marked as "out" (covariant), so Contains() 
	/// must be an extension method.
	/// </remarks>
	#if DotNet4
	public interface ISource<out T> : IIterable<T>, ICount
	#else
	public interface ISource<T> : IIterable<T>, ICount
	#endif
	{
	}

	public static partial class CollectionInterfaces
	{
		public static int CopyTo<T>(this ISource<T> c, T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			if (c.Count > space)
				throw new ArgumentException(Localize.From("CopyTo: array is too small ({0} < {1})", space, c.Count));
			return CopyTo((IIterable<T>)c, array, arrayIndex);
		}
	}

	public static partial class Collections
	{
		/// <summary>Converts any ICollection{T} object to ISource{T}.</summary>
		/// <remarks>This method is named "AsSource" and not "ToSource" because,
		/// in contrast to methods like ToArray(), and ToList() it does not make a 
		/// copy of the sequence.</remarks>
		public static ISource<T> AsSource<T>(this ICollection<T> c)
		{
			var list = c as ISource<T>;
			if (list != null)
				return list;
			return new CollectionAsSource<T>(c);
		}
		
		/// <summary>Converts any ISource{T} object to a read-only ICollection{T}.</summary>
		/// <remarks>This method is named "AsCollection" and not "ToCollection" 
		/// because, in contrast to methods like ToArray() and ToList(), it does not 
		/// make a copy of the sequence.</remarks>
		public static ICollection<T> AsCollection<T>(this ISource<T> c)
		{
			var list = c as ICollection<T>;
			if (list != null)
				return list;
			return new SourceAsCollection<T>(c);
		}
	}

	/// <summary>A read-only wrapper that implements ICollection(T) and ISource(T).</summary>
	public sealed class CollectionAsSource<T> : WrapperBase<ICollection<T>>, ICollection<T>, ISource<T>
	{
		public CollectionAsSource(ICollection<T> obj) : base(obj) { }

		public Iterator<T> GetIterator()
		{
			return _obj.GetEnumerator().AsIterator();
		}
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

	/// <summary>A read-only wrapper that implements ICollection(T) and ISource(T).</summary>
	public sealed class SourceAsCollection<T> : WrapperBase<ISource<T>>, ICollection<T>, ISource<T>
	{
		public SourceAsCollection(ISource<T> obj) : base(obj) { }

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
			CollectionInterfaces.CopyTo(_obj, array, arrayIndex);
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
			return _obj.GetIterator().ToEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public Iterator<T> GetIterator()
		{
			return _obj.GetIterator();
		}
	}
}
