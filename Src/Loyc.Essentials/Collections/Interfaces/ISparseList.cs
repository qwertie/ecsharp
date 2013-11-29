﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Represents a read-only indexed list in which parts of the index 
	/// space may be unused or "clear".</summary>
	/// <remarks>
	/// This interface should be implemented by "sparse" data structures that are 
	/// optimized for lists that have large gaps in them; for example, a sparse 
	/// data structure with <c>Count == 1000000</c> might really contain only a 
	/// few elements, or it could even be completely empty. The <c>Count</c> of a 
	/// sparse list tells you the range of valid indexes that you are allowed to 
	/// read, but since any or all of the space might be empty, it only gives an 
	/// upper bound, not a lower bound, of the true size of the list.
	/// <para/>
	/// When you read <c>list[i]</c>, and <c>i</c> is within an empty area of the 
	/// sparse list, a default value is returned, which is normally <c>default(T)</c>.
	/// <para/>
	/// As an example, <c>SortedDictionary&lt;int, T></c> could be viewed as a sparse
	/// list. Assuming the dictionary has no negative integers, you could create a 
	/// wrapper around <c>SortedDictionary&lt;int, T></c> that implements this 
	/// interface as follows:
	/// <ul>
	/// <li><c>Count</c> returns <c>int.MaxValue</c>,</li>
	/// <li><see cref="NextHigher"/> and <see cref="NextLower"/> do a linear scan 
	/// to find the next higher or lower key that exists.</li>
	/// <li><see cref="IsSet(int)"/> returns the result of <see cref="SortedDictionary{K,V}.ContainsKey"/>, and</li>
	/// <li><see cref="IListSource{T}.TryGet(int, ref bool)"/> returns the value 
	/// retrieved by <see cref="SortedDictionary{K,V}.TryGetValue"/>, setting 'fail'
	/// only if the index is negative.</li>
	/// </ul>
	/// <c>SortedDictionary</c> is not a very useful example in practise, though,
	/// because it does not provide a way for <see cref="NextHigher"/> and
	/// <see cref="NextLower"/> to work efficiently, and it cannot efficiently 
	/// support the <see cref="ISparseList{T}"/> interface.
	/// </remarks>
	/// <seealso cref="LCExtensions.AsSparse"/>
	public interface ISparseListSource<T> : IListSource<T>
	{
		/// <summary>Gets the next higher index of a non-empty (cleared) element 
		/// of the list, or null if there are no higher indexes.</summary>
		/// <remarks>This method should skip over all indexes i for which 
		/// <c>IsSet(i)</c> returns false, and return the next index for which
		/// <c>IsSet(i)</c> returns true.
		/// <para/>
		/// This method should accept any integer as input, including invalid 
		/// indexes. For example, if the first existing index is 5, 
		/// NextHigher(-10) returns 5.
		/// </remarks>
		int? NextHigher(int index);
		
		/// <summary>Gets the next lower index of a non-empty (cleared) element 
		/// of the list, or null if there are no lower indexes.</summary>
		/// <remarks>This method should skip over all indexes i for which 
		/// <c>IsSet(i)</c> returns false, and return the next lower index for 
		/// which <c>IsSet(i)</c> returns true.
		/// <para/>
		/// This method should accept any integer as input, including invalid 
		/// indexes.
		/// </remarks>
		int? NextLower(int index);

		/// <summary>Determines whether a value exists at the specified index.</summary>
		/// <param name="index"></param>
		/// <returns>true if a value is assigned at the specified index, or false
		/// if index is part of an empty space, or is outside the range of indexes
		/// that exist.</returns>
		bool IsSet(int index);
	}

	public partial class LCInterfaces
	{
		/// <summary>Returns the non-cleared items in the sparse list, along with 
		/// their indexes, sorted by index.</summary>
		/// <remarks>
		/// The returned sequence should exactly match the set of indexes for which 
		/// <c>list.IsSet(Key)</c> returns true.</remarks>
		public static IEnumerable<KeyValuePair<int, T>> Items<T>(this ISparseListSource<T> list)
		{
			int i = -1;
			int? next;
			while ((next = list.NextHigher(i)) != null) {
				i = next.Value;
				yield return new KeyValuePair<int, T>(i, list[i]);
			}
		}
	}

	/// <summary>Represents a sparse list that allows insertion and removal of items
	/// and empty spaces. In a sparse list, some spaces can be "clear" meaning that 
	/// they have no value.</summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// Classes that implement this interface should be designed so that large empty 
	/// spaces in the list consume no memory, or very little memory. Furthermore,
	/// insertions and removals at random indexes should take no more than O(M) 
	/// amortized time to complete, where M is the number of elements actually set
	/// (non-empty spaces), and Count >= M. (Sparse lists do not have to keep track
	/// of M itself, though, so there is no property to retrieve this value).
	/// <para/>
	/// This interface has special methods for creating empty spaces: 
	/// <see cref="Clear(int, int)"/> and <see cref="InsertSpace(int, int)"/>.
	/// The implementation can treat all other insertions and changes as non-empty
	/// values; for example, given code like
	/// <code>
	/// ISparseList&lt;T> list = ...;
	/// foreach (int i in Enumerable.Range(1000))
	///     list.Add(default(T));
	/// </code>
	/// A sparse list is allowed to treat the last 1000 items as "clear" (saving 
	/// memory) or "set" (allocating memory) depending on how it is implemented. 
	/// Since the .NET framework does not allow fast bitwise comparisons of a 
	/// generic T (in other words, there is no fast, generic way to compare a T 
	/// value with default(T)), implementations will typically treat the last 1000 
	/// items as "set" and allocate memory for them.
	/// <para/>
	/// This interface has no method to insert another sparse list into the current 
	/// one, but <see cref="SparseListEx{T}"/> does.
	/// <para/>
	/// A sparse list is allowed to behave like <see cref="IAutoSizeArray{T}"/> when 
	/// setting an invalid non-negative index. When you set <c>this[i]</c> where 
	/// i >= 0, the <c>Count</c> may automatically increase to <c>i + 1</c> if 
	/// necessary, <i>or</i> the setter may throw <see cref="ArgumentOutOfRangeException"/>.
	/// Even if the setter succeeds, the getter of <c>this[i]</c> may still throw 
	/// <see cref="ArgumentOutOfRangeException"/> when <c>i</c> is an invalid index.
	/// <para/>
	/// The indexer (<c>this[i]</c>) returns a default value, typically 
	/// <c>default(T)</c>, when the specified index is valid but clear.
	/// <para/>
	/// Sparse list implementations do <i>not</i> have to perfectly track which 
	/// spaces are clear and which spaces are set; in particular, implementations
	/// are allowed to return <c>true</c> from <c>IsSet</c> for any valid index.
	/// <ul>
	/// <li>If the default value is <c>t</c> and you set <c>this[i] = t</c>, a 
	/// sparse list is allowed to return true or false from <c>IsSet(i)</c>.</li>
	/// <li>After you call <c>Clear(i)</c>, a sparse list is still allowed to 
	/// return true from <c>IsSet(i)</c>.</li>
	/// <li>After you set <c>this[i] = v</c> where <c>v</c> is not the default 
	/// value, <c>IsSet(i)</c> <i>must</i> return true.</li>
	/// </ul>
	/// The purpose of this freedom is to permit implementations that use arrays
	/// of T[] for regions that are mostly filled. For example, if 95 out of 100
	/// indexes in a certain region are filled, an implementation may decide it 
	/// is more efficient to use an array that discards information about the 
	/// empty spaces.
	/// <para/>
	/// On the other hand, some implementations are precise, and will always 
	/// report that <c>IsSet(i)==false</c> after you call <c>Clear(i)</c> or
	/// <c>InsertSpace(i)</c>.
	/// </remarks>
	public interface ISparseList<T> : ISparseListSource<T>, IListAndListSource<T>
	{
		void Clear(int index, int count = 1);
		void InsertSpace(int index, int count = 1);
	}
	
	/// <summary>A sparse list that supports additional methods including 
	/// <see cref="InsertRange(ISparseListSource{T})"/>.</summary>
	/// <seealso cref="ISparseList{T}"/>
	public interface ISparseListEx<T> : ISparseList<T>, IListEx<T>, IAutoSizeArray<T>
	{
		/// <summary>Inserts another sparse list into this one.</summary>
		void InsertRange(ISparseListSource<T> list);
	}
}
