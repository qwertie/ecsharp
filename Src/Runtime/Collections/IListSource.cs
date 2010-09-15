// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	/// <summary>A read-only list indexed by an integer.</summary>
	/// <remarks>
	/// The term "source" means a read-only collection, as opposed to a "sink"
	/// which would be a write-only collection. The purpose of IListSource is to
	/// make it easier to implement a read-only list, by lifting IList's
	/// requirement to write implementations for Add(), Remove(), etc. A secondary
	/// purpose is, of course, to guarantee users don't mistakenly call those 
	/// methods on a read-only collection.
	/// <para/>
	/// I have often wanted to access the "next" or "previous" item in a list, e.g.
	/// during parsing, but it inconvenient if you have to worry about whether the 
	/// the current item is the first or last. In that case you must check whether
	/// the array index is valid, which is both inconvenient and wasteful, because
	/// the list class will check the array index also, and throw
	/// ArgumentOutOfRangeException in case of a problem. To solve these problems,
	/// IListSource introduces a second indexer. If the array index is
	/// out-of-range, the second indexer returns a default value.
	/// <para/>
	/// As IListSource is supposed to be a simpler alternative to IList, I didn't
	/// want to require implementers to implement more than two indexers. Ideally,
	/// this interface would have included a third indexer as a method:
	/// <code>
	///     bool TryGetValue(int, ref T);
	/// </code>
	/// The advantage of this would have been that it specifically informs the
	/// caller whether the index was valid or not. I would have then defined the 
	/// second indexer as an extension method, to lift the burden from
	/// implementers:
	/// <code>
	///     static T this[this IListSource&lt;T> list, int index, T defaultValue]
	///     {
	///         get {
	///             list.TryGetValue(index, ref defaultValue);
	///             return defaultValue;
	///         }
	///     }
	/// </code>
	/// I didn't use this approach because standard C# doesn't support extension 
	/// properties and extension indexers. So instead I provide TryGetValue as an
	/// extension method, but this is less efficient, because TryGetValue and the
	/// indexer it calls will both test the array bounds.
	/// </remarks>
	public interface IListSource<T> : ISource<T>
	{
		/// <summary>Gets the item at the specified index.</summary>
		/// <exception cref="ArgumentOutOfRangeException">The index was not valid
		/// in this list.</exception>
		/// <returns>The element at the specified index.</returns>
		T this[int index] { get; }

		/// <summary>Gets the item at the specified index.</summary>
		/// <returns>The element at the specified index, or defaultValue if the
		/// index is not valid.</returns>
		T this[int index, T defaultValue] { get; }

		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or -1 if it was not found.</summary>
		/// <remarks>
		/// Implementer could call Collections.IndexOf to help:
		/// <code>
		/// public int IndexOf(T item)
		/// {
		///     return Collections.IndexOf(this, item);
		/// }
		/// </code>
		/// IndexOf() is not provided as an extension method, just in case the
		/// source has some kind of fast lookup logic (e.g. binary search).
		/// </remarks>
		int IndexOf(T item);
	}

	public static partial class Collections
	{
		public static ListSourceFromList<T> ToListSource<T>(this IList<T> c)
			{ return new ListSourceFromList<T>(c); }
		public static ListFromListSource<T> ToList<T>(this IListSource<T> c)
			{ return new ListFromListSource<T>(c); }

		public static bool TryGetValue<T>(this IListSource<T> list, int index, ref T value)
		{
		    if ((uint)index < (uint)list.Count)
				return false;
		    value = list[index];
			return true;
		}
		public static int IndexOf<T>(IListSource<T> list, T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			int count = list.Count;
			for (int i = 0; i < count; i++)
				if (comparer.Equals(item, list[i]))
					return i;
			return -1;
		}
		public static int CopyTo<T>(this IListSource<T> c, T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			int count = c.Count;
			if (space < count) {
				if ((uint)arrayIndex >= (uint)count)
					throw new ArgumentOutOfRangeException("arrayIndex");
				else
					throw new ArgumentException(Localize.From("CopyTo: array is too small ({0} < {1})", space, count));
			}
			
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = c[i];
			
			return arrayIndex + count;
		}
		public static ListSourceSlice<T> Slice<T>(this IListSource<T> list, int start, int length)
		{
			return new ListSourceSlice<T>(list, start, length);
		}
	}

	/// <summary>This interface models the capabilities of an array: getting and
	/// setting elements by index, but not adding or removing elements.</summary>
	public interface IArray<T> : IListSource<T>
	{
		new T this[int index] { set; }
	}

	/// <summary>A read-only wrapper that implements ICollection and ISource.</summary>
	public sealed class ListSourceFromList<T> : AbstractWrapper<IList<T>>, IList<T>, IListSource<T>
	{
		public ListSourceFromList(IList<T> obj) : base(obj) { }

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
		public int IndexOf(T item)
		{
			return _obj.IndexOf(item);
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

	/// <summary>A read-only wrapper that implements IList(T) and IListSource(T).</summary>
	public sealed class ListFromListSource<T> : AbstractWrapper<IListSource<T>>, IList<T>, IListSource<T>
	{
		public ListFromListSource(IListSource<T> obj) : base(obj) { }

		#region IList<T> Members

		public int IndexOf(T item)
		{
			return _obj.IndexOf(item);
		}
		public void Insert(int index, T item)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("Collection is read-only.");
		}
		public T this[int index]
		{
			get { return _obj[index]; }
			set { throw new NotSupportedException("Collection is read-only."); }
		}
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
		public T this[int index, T defaultValue]
		{
			get { return _obj[index, defaultValue]; }
		}
	}
}
