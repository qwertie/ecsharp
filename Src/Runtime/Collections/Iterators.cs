// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;

namespace Loyc.Runtime
{
	/// <summary>A high-performance alternative to IEnumerator(of T).</summary>
	/// <remarks>
	/// The normal IEnumerator interface is inefficient, in that one must call two
	/// interface methods on every iteration: MoveNext and Current. This is one of
	/// many small decisions that makes the .NET framework slightly slower than
	/// necessary, but we can work around it by defining this alternative.
	/// <para/>
	/// An enumerator e can be converted to an iterator using
	/// Iterator.ToIterator(e), and an enumerable to IIterable using
	/// Iterator.ToIterable(e). Likewise an iterator i can be converted to an
	/// enumerator using Iterator.ToEnumerator(i), and an iterable to enumerable
	/// using Iterator.ToEnumerable(i).
	/// <para/>
	/// Whether Iterator should be a delegate or interface is hard to decide, as
	/// each has advantages and disadvantages. A delegate call is slightly faster
	/// than an interface call, but sometimes an interface call can be avoided.
	/// Consider List(T).GetEnumerator, which does not return IEnumerator but
	/// rather List(T).Enumerator. Thus, when List(T) is directly used in a 
	/// foreach loop, no interface calls are necessary to call MoveNext() and
	/// Current. Also, an interface allows casting (if the object implements other
	/// interfaces), and the Reset() method. On the other hand, as a delegate,
	/// an Iterator would be easier to define in standard C#--a function can 
	/// return a lambda that implements the iterator.
	/// <para/>
	/// Originally this delegate was defined as bool Iterator(out T current),
	/// so that calling it was like calling MoveNext() except that you get the next
	/// value at the same time. Unfortunately, the CLR does not permit this 
	/// definition to be covariant: only return values can be covariant. Therefore 
	/// I had to change the argument into a return value. However, an extension
	/// method called MoveNext() allows you to call Iterator in the original way:
	/// <code>
	/// T current;
	/// for (Iterator&lt;T> it = list.GetIterator(); it.MoveNext(out current); )
	/// {
	///     ...
	/// }
	/// </code>
	/// Unfortunately, benchmarking shows that MoveNext() adds some overhead, which
	/// eliminates most of the speed advantage that Iterator has over IEnumerator.
	/// </remarks>
	#if CSharp4
	public delegate T Iterator<out T>(ref bool ended);
	#else
	public delegate T Iterator<T>(ref bool ended);
	// The .NET Framework 2.0 permits the first (covariant) definition, but only C#
	// version 4+ can parse it.
#endif

	/// <summary>Helper methods for creating iterators and converting to/from
	/// enumerators. The underscore is needed to avoid a name collision with the 
	/// Iterator delegate.</summary>
	public static class Iterator
	{
		public static Iterator<T> From<T>(IEnumerator<T> e) { return e.ToIterator(); }
		public static IIterable<T> From<T>(IEnumerable<T> e) { return e.ToIterable(); }
		
		public static Iterator<T> Empty<T>()
		{
			return Iterator_<T>.Empty;
		}
		public static Iterator<T> Single<T>(T value) { return Repeat(value, 1); }
		public static Iterator<T> Repeat<T>(T value, int count)
		{
			return delegate(ref bool ended)
			{
				if (--count < 0)
					ended = true;
				return value;
			};
		}
		public static Iterator<T> RepeatForever<T>(T value)
		{
			return delegate(ref bool ended)
			{
				return value;
			};
		}
		public static bool MoveNext<T>(this Iterator<T> it, out T value)
		{
			bool ended = false;
			value = it(ref ended);
			return !ended;
		}
	}
	public static class Iterator_<T>
	{
		public static Iterator<T> Empty = delegate(ref bool ended)
		{
			ended = true;
			return default(T);
		};
	}

	public static partial class Collections
	{
		public static IEnumerator<T> ToEnumerator<T>(this Iterator<T> i)
		{
			bool ended = false;
			for (T current = i(ref ended); !ended; current = i(ref ended))
				yield return current;
		}
		public static Iterator<T> ToIterator<T>(this IEnumerator<T> e)
		{
			return delegate(ref bool ended)
			{
				if (e.MoveNext())
					return e.Current;
				else {
					ended = true;
					return default(T);
				}
			};
		}

		public static IEnumerable<T> ToEnumerable<T>(this IIterable<T> list)
		{
			var listE = list as IEnumerable<T>;
			if (listE != null)
				return listE;
			return ToEnumerableCore(list);
		}
		internal static IEnumerable<T> ToEnumerableCore<T>(IIterable<T> list)
		{
			bool ended = false;
			Iterator<T> i = list.GetIterator();
			T current;
			for(;;) {
				current = i(ref ended);
				if (ended)
					yield break;
				yield return current;
			}
		}
		public static IIterable<T> ToIterable<T>(this IEnumerable<T> list)
		{
			var listI = list as IIterable<T>;
			if (listI != null)
				return listI;
			return new IterableFromEnumerable<T>(list);
		}
		
		/// <summary>Determines whether the source contains a specific value.</summary>
		/// <returns>true if an element that equals 'item' was found, false otherwise.</returns>
		/// <remarks>
		/// Contains() was originally a member of the ISource(T) interface, just in 
		/// case the source had some kind of fast lookup logic. However, this is
		/// not allowed in C# 4 when T is marked as "out" (covariant), so Contains()
		/// must be an extension method.
		/// </remarks>
		public static bool Contains<T>(this IIterable<T> list, T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			bool ended = false;
			var it = list.GetIterator();
			for (T current = it(ref ended); !ended; current = it(ref ended))
				if (comparer.Equals(item, current))
					return true;
			return false;
		}
		
		public static int CopyTo<T>(this IIterable<T> c, T[] array, int arrayIndex)
		{
			bool ended = false;
			var it = c.GetIterator();
			for (T current = it(ref ended); !ended; current = it(ref ended))
				array[arrayIndex++] = current;
			return arrayIndex;
		}
	}

	/// <summary>A high-performance alternative to IEnumerable(of T).</summary>
	/// <remarks>
	/// IIterable is to Iterator what IEnumerable is to IEnumerator.
	/// <para/>
	/// TODO: Implement LINQ-to-iterable
	/// <para/>
	/// If you already implement IEnumerable and want to add IIterable, you can use
	/// this implementation (taking advantage of the extension method ToIterator):
	/// <code>
	/// public Iterator&lt;T> GetIterator()
	/// {
	///     return GetEnumerator().ToIterator();
	/// }
	/// </code>
	/// </remarks>
	#if CSharp4
	public interface IIterable<out T>
	#else
	public interface IIterable<T>
	#endif
	{
		Iterator<T> GetIterator();
	}

	public class IterableFromEnumerable<T> : WrapperBase<IEnumerable<T>>, IIterable<T>, IEnumerable<T>
	{
		public IterableFromEnumerable(IEnumerable<T> list) : base(list) { }
		
		public Iterator<T> GetIterator()
		{
			return _obj.GetEnumerator().ToIterator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			return _obj.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _obj.GetEnumerator();
		}
	}
}