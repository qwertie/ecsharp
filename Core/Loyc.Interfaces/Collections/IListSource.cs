// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A read-only list indexed by an integer.</summary>
	/// <remarks>
	/// Member list:
	/// <code>
	/// public IEnumerator&lt;T> GetEnumerator();  // inherited
	/// System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator(); // inherited
	/// public T this[int index] { get; }          // inherited
	/// public int Count { get; }                  // inherited
	/// public T TryGet(int index, out bool fail); // inherited
	/// IRange&lt;T> Slice(int start, int count = int.MaxValue);
	/// </code>
	/// The term "source" means a read-only collection, as opposed to a "sink" which
	/// is a write-only collection.
	/// <para/>
	/// IListSource was created before .NET's IReadOnlyList but was later retrofitted 
	/// so that IListSource implements IReadOnlyList. In addition, IListSource supports
	/// slices through its Slice() method, and it has <c>TryGet</c> methods to eliminate 
	/// the need to call <c>Count</c> before reading from the list.
	/// <para/>
	/// I have often wanted to access the "next" or "previous" item in a list, e.g.
	/// during parsing, but it inconvenient if you have to worry about whether the 
	/// the current item is the first or last. In that case you must check whether
	/// the array index is valid, which is both inconvenient and wasteful, because
	/// the list class itself will check the array index a second time, and then the 
	/// .NET runtime will check the index a third time when reading the internal 
	/// array. The <c>TryGet(index, defaultValue)</c> extension method can be used 
	/// to return a default value if the index is not valid, using only one 
	/// interface call.
	/// <para/>
	/// Design footnote: Ideally the return type of TryGet would be <see cref="Maybe{T}"/>,
	/// but that design would not allow T to be covariant (out T). Therefore, the 
	/// version of <c>TryGet</c> that returns <see cref="Maybe{T}"/> is an extension 
	/// method.
	/// <para/>
	/// Using <see cref="Impl.ListSourceBase{T}"/> as your base class can help you
	/// implement this interface more quickly.
	/// </remarks>
	public interface IListSource<out T> : IReadOnlyList<T>, ISource<T>, ITryGet<int, T>, IIndexed<int, T>
	{
		/// <summary>Returns a sub-range of this list.</summary>
		/// <param name="start">The new range will start at this index in the current
		/// list (this location will be index [0] in the new range).</param>
		/// <param name="count">The desired number of elements in the new range,
		/// or int.MaxValue to get all elements until the end of the list.</param>
		/// <returns>Returns a sub-range of this range.</returns>
		/// <exception cref="ArgumentException">The start index was below zero.</exception>
		/// <remarks>The (start, count) range is allowed to be invalid, as long
		/// as start is zero or above. 
		/// <ul>
		/// <li>If count is below zero, or if start is above the original Count, the
		/// Count of the new slice is set to zero.</li>
		/// <li>if (start + count) is above the original Count, the Count of the new
		/// slice is reduced to <c>this.Count - start</c>. Implementation note:
		/// do not compute (start + count) because it may overflow. Instead, test
		/// whether (count > this.Count - start).</li>
		/// </ul>
		/// Most collections should use the following implementation:
		/// <pre>
		/// IListSource&lt;T> IListSource&lt;T>.Slice(int start, int count) { return Slice(start, count); }
		/// public Slice_&lt;T> Slice(int start, int count) { return new Slice_&lt;T>(this, start, count); }
		/// </pre>
		/// </remarks>
		IListSource<T> Slice(int start, int count = int.MaxValue);
	}

	public static partial class LCInterfaces
	{
		/// <summary>Tries to get a value from the list at the specified index.</summary>
		/// <param name="index">The index to access. Valid indexes are between 0 and Count-1.</param>
		/// <param name="value">A variable that will be changed to the retrieved value. If the index is not valid, this variable is left unmodified.</param>
		/// <returns>True on success, or false if the index was not valid.</returns>
		[Obsolete("Please use another overload of TryGet; this one will be removed eventually")]
		public static bool TryGet<T>(this IListSource<T> list, int index, [AllowNull] ref T value)
		{
			bool fail;
			T result = list.TryGet(index, out fail)!;
			if (fail)
				return false;
			value = result;
			return true;
		}

		/// <summary>Uses list.TryGet(index) to find out if the specified index is valid.</summary>
		/// <returns>true if the specified index is valid, false if not.</returns>
		[Obsolete("Use TryGet(index).HasValue instead, or compare index to Count")]
		public static bool HasIndex<T>(this IListSource<T> list, int index)
		{
			bool fail;
			list.TryGet(index, out fail);
			return !fail;
		}

		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or null if it was not found.</returns>
		public static int? FirstIndexOf<T>(this IReadOnlyList<T> list, T item)
		{
			int count = list.Count;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			for (int i = 0; i < count; i++)
				if (comparer.Equals(item, list[i]))
					return i;
			return null;
		}

		/// <summary>Determines the index of a specific value.</summary>
		/// <returns>The index of the value, if found, or -1 if it was not found.</returns>
		public static int IndexOf<T>(this IReadOnlyList<T> list, T item) => FirstIndexOf(list, item) ?? -1;

		public static void CopyTo<T>(this IReadOnlyList<T> c, T[] array, int arrayIndex)
		{
			int space = array.Length - arrayIndex;
			int count = c.Count;
			if (space < count) {
				if ((uint)arrayIndex >= (uint)count)
					throw new ArgumentOutOfRangeException("arrayIndex");
				else
					throw new ArgumentException("CopyTo: array is too small ({0} < {1})".Localized(space, count));
			}
			
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = c[i];
		}

		/// <summary>Gets the lowest index at which a condition is true, or null if nowhere.</summary>
		public static int? FirstIndexWhere<T>(this IReadOnlyList<T> source, Func<T, bool> pred)
		{
			for (int i = 0, c = source.Count; i < c; i++)
				if (pred(source[i]))
					return i;
			return null;
		}
		/// <summary>Gets the lowest index at which a condition is true, or -1 if nowhere.</summary>
		[Obsolete("Please use FirstIndexWhere. This method will be changed later to return nullable int.")]
		public static int IndexWhere<T>(this IReadOnlyList<T> source, Func<T, bool> pred) => FirstIndexWhere(source, pred) ?? -1;

		/// <summary>Gets the highest index at which a condition is true, or null if nowhere.</summary>
		public static int? FinalIndexWhere<T>(this IReadOnlyList<T> source, Func<T, bool> pred)
		{
			for (int i = source.Count-1; i >= 0; i--)
				if (pred(source[i]))
					return i;
			return null;
		}
		/// <summary>Gets the highest index at which a condition is true, or -1 if nowhere.</summary>
		[Obsolete("Please use FinalIndexWhere. This method will be changed later to return nullable int.")]
		public static int LastIndexWhere<T>(this IReadOnlyList<T> source, Func<T, bool> pred) =>
			FinalIndexWhere(source, pred) ?? -1;
	}
}
