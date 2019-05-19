using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	/// <summary>A variation of IReadOnlyCollection that provides the Contains() and 
	/// CopyTo() methods from ICollection.</summary>
	/// <remarks>
	/// Implementing this interface suggests that the collection supports accelerated
	/// membership tests (int O(1) or O(log(Count)) time), since you would only need to
	/// implement IReadOnlyCollection if it doesn't.
	/// <para/>
	/// The name of this collection fits a pattern: just as IListSource is a variation on
	/// IReadOnlyList with additional functionality, this interface is a variation on
	/// IReadOnlyCollection with additional functionality. The word "source" means "data
	/// comes out"; it is the opposite of a "sink" which means "data goes in".
	/// </remarks>
	public interface ICollectionSource<T> : IReadOnlyCollection<T>
	{
		/// <summary>Returns true if and only if the collection contains the specified 
		/// item.</summary>
		/// <param name="item">Data/object whose presence you want to check for. The
		/// collection decides how to test for equality, but it's most common to use
		/// <see cref="EqualityComparer{T}.Default"/>.</param>
		bool Contains(T item);

		/// <summary>Copies the elements of the collection to an Array, starting at a 
		/// particular array index.</summary>
		/// <remarks>It's usually more convenient to call the ToArray() extension method, 
		/// which calls this method for you.
		/// <para/>
		/// This method exists for performance reasons (the collection itself can often 
		/// copy data out faster than an enumerator can).
		/// </remarks>
		/// <exception cref="ArgumentNullException">array is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">arrayIndex is negative.</exception>
		/// <exception cref="ArgumentException">The number of elements in the source 
		/// collection is greater than the available space from arrayIndex to the end of 
		/// the destination array.</exception>
		void CopyTo(T[] array, int arrayIndex);
	}

	/// <summary>Extension methods for ICollection, IReadOnlyCollection and ICollectionSource.</summary>
	public static partial class LCExt
	{
		/// <summary>Converts the collection to an array.</summary>
		public static T[] ToArray<T>(this ICollectionSource<T> list)
		{
			T[] array = new T[list.Count];
			list.CopyTo(array, 0);
			return array;
		}
	}
}
