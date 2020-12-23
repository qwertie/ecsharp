using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Combines <see cref="IReadOnlyCollection{T}"/> with related interfaces
	/// <see cref="ICount"/> and <see cref="IIsEmpty"/>.</summary>
	public interface ISource<out T> : IReadOnlyCollection<T>, ICount, IIsEmpty
	{
		/// <summary>Gets the number of items in the collection.</summary>
		/// <remarks>This property exists only to resolve the supposed "ambiguity" between 
		/// <see cref="IReadOnlyCollection{T}.Count"/> and <see cref="ICount.Count"/>.
		new int Count { get; }
	}

	/// <summary>Holds the Count property found in nearly all collection interfaces.</summary>
	public interface ICount
	{
		/// <summary>Gets the number of items in the collection.</summary>
		int Count { get; }
	}

	/// <summary>Holds the IsEmpty property that tells you if a collection is empty.</summary>
	public interface IIsEmpty
	{
		bool IsEmpty { get; }
	}

	/// <summary>
	/// This class contains extension methods that are provided as part of various 
	/// Loyc.Collections interfaces. For example, it provides methods such as IndexOf(),
	/// Contains() and CopyTo(), that the traditional <see cref="ICollection{T}"/> 
	/// and <see cref="IList{T}"/> interfaces require the author to write himself.
	/// </summary>
	/// <remarks>
	/// For covariant collections such as <see cref="IReadOnlyCollection{T}"/> and <see 
	/// cref="IListSource{T}"/>, the CLR actually prohibits methods such as 
	/// Contains(T) and IndexOf(T), because T is not allowed in "input" positions. 
	/// Therefore, these extension methods must be used to fill the gap. Even
	/// methods such as <i>bool TryGet(int, out T)</i> are prohibited, so TryGet()
	/// has the signature <i>T TryGet(ref bool failed)</i> instead, and extension
	/// methods provide the original version of the method in addition.
	/// </remarks>
	public static partial class LCInterfaces
	{
		/// <summary>Returns true if the collection contains any elements.</summary>
		public static bool Any(this ICount c) { return c.Count > 0; }
	}
}
