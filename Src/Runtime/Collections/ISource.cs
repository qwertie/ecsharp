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
	#if CSharp4
	public interface ISource<out T> : IIterable<T>
	#else
	public interface ISource<T> : IIterable<T>
	#endif
	{
		/// <summary>Returns the number of items provided by the iterator.</summary>
		int Count { get; }
	}

	public static partial class Collections
	{
		public static SourceFromCollection<T> ToSource<T>(this ICollection<T> c)
			{ return new SourceFromCollection<T>(c); }
		public static CollectionFromSource<T> ToCollection<T>(this ISource<T> c)
			{ return new CollectionFromSource<T>(c); }
	}

	/// <summary>A read-only wrapper that implements ICollection(T) and ISource(T).</summary>
	public sealed class SourceFromCollection<T> : WrapperBase<ICollection<T>>, ICollection<T>, ISource<T>
	{
		public SourceFromCollection(ICollection<T> obj) : base(obj) { }

		public Iterator<T> GetIterator()
		{
			return _obj.GetEnumerator().ToIterator();
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
	public sealed class CollectionFromSource<T> : WrapperBase<ISource<T>>, ICollection<T>, ISource<T>
	{
		public CollectionFromSource(ISource<T> obj) : base(obj) { }

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
			Collections.CopyTo(_obj, array, arrayIndex);
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
