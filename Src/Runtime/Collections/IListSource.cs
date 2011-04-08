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
	/// the list class will check the array index again, and then the .NET runtime
	/// will check the index a third time when reading the internal array. To make
	/// this more efficient, IListSource has a TryGet() method that does not throw
	/// on failure, but returns default(T).
	/// <para/>
	/// As IListSource is supposed to be a simpler alternative to IList, I didn't
	/// want to require implementers to implement more than two indexers. There are
	/// two additional TryGet extension methods, though:
	/// <code>
	///     bool TryGet(int index, ref T value);
	///     T TryGet(int, T defaultValue);
	/// </code>
	/// If T is defined as "out" (covariant) in C# 4, these methods are not allowed 
	/// in IListSource anyway and MUST be extension methods.
	/// <para/>
	/// Note that "value" is a "ref" rather than an "out" parameter, unlike
	/// Microsoft's own TryGetValue() implementations. Using ref parameter allows
	/// the caller to choose his own default value in case TryGet() returns false.
	/// </remarks>
	#if CSharp4
	public interface IListSource<out T> : ISource<T>
	#else
	public interface IListSource<T> : ISource<T>
	#endif
	{
		/// <summary>Gets the item at the specified index.</summary>
		/// <exception cref="ArgumentOutOfRangeException">The index was not valid
		/// in this list.</exception>
		/// <returns>The element at the specified index.</returns>
		T this[int index] { get; }

		/// <summary>Gets the item at the specified index, and does not throw an
		/// exception on failure.</summary>
		/// <param name="fail">A flag that is set on failure. To improve
		/// performance slightly, this flag is not cleared on success.</param>
		/// <returns>The element at the specified index, or default(T) if the index
		/// is not valid.</returns>
		/// <remarks>In my original design, the caller could provide a value to 
		/// return on failure, but this would not allow T to be marked as "out" in 
		/// C# 4. For the same reason, we cannot have a ref/out T parameter.
		/// Instead, the following extension methods are provided:
		/// <code>
		///     bool TryGet(int index, ref T value);
		///     T TryGet(int, T defaultValue);
		/// </code>
		/// </remarks>
		T TryGet(int index, ref bool fail);
	}

	public static partial class Collections
	{
		public static ListSourceFromList<T> ToListSource<T>(this IList<T> c)
			{ return new ListSourceFromList<T>(c); }
		public static ListFromListSource<T> ToList<T>(this IListSource<T> c)
			{ return new ListFromListSource<T>(c); }

		public static bool TryGet<T>(this IListSource<T> list, int index, ref T value)
		{
			bool fail = false;
			T result = list.TryGet(index, ref fail);
			if (fail)
				return false;
			value = result;
			return true;
		}
		public static T TryGet<T>(this IListSource<T> list, int index, T defaultValue)
		{
			bool fail = false;
			T result = list.TryGet(index, ref fail);
			if (fail)
				return defaultValue;
			else
				return result;
		}
		
		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or -1 if it was not found.</returns>
		/// <remarks>
		/// At first, this method was a member of IListSource itself, just in 
		/// case the source might have some kind of fast lookup logic (e.g. binary 
		/// search) or custom comparer. However, since the item to find is an "in" 
		/// argument, it would prevent IListSource from being marked covariant when
		/// I upgrade to C# 4.
		/// </remarks>
		public static int IndexOf<T>(this IListSource<T> list, T item)
		{
			int count = list.Count;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
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

		public static T[] ToArray<T>(this IListSource<T> c)
		{
			var array = new T[c.Count];
			for (int i = 0; i < array.Length; i++)
				array[i] = c[i];
			return array;
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
	public sealed class ListSourceFromList<T> : WrapperBase<IList<T>>, IList<T>, IListSource<T>
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
		public T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_obj.Count)
				return _obj[index];
			fail = true;
			return default(T);
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
	public sealed class ListFromListSource<T> : WrapperBase<IListSource<T>>, IList<T>, IListSource<T>
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
		public T TryGet(int index, ref bool fail)
		{
			return _obj.TryGet(index, ref fail);
		}
	}

	public class ReversedListSource<T> : IListSource<T>
	{
		IListSource<T> _list;
		public ReversedListSource(IListSource<T> list) { _list = list; }

		public T this[int index]
		{
			get { return _list[_list.Count - 1 - index]; }
		}
		public T TryGet(int index, ref bool fail)
		{
			return _list.TryGet(_list.Count - 1 - index, ref fail);
		}
		public int Count
		{
			get { return _list.Count; }
		}
		public Iterator<T> GetIterator()
		{
			int i = _list.Count;;
			return delegate(ref bool fail)
			{
				return TryGet(--i, ref fail);
			};
		}
	}
}
