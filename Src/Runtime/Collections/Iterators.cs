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
	/// </remarks>
	#if CSharp4
	public delegate bool Iterator<out T>(out T current);
	#else
	public delegate bool Iterator<T>(out T current);
	#endif

	/// <summary>Helper methods for creating iterators and converting to/from
	/// enumerators. The underscore is needed to avoid a name collision with the 
	/// Iterator delegate.</summary>
	public static class Iterator_
	{
		public static Iterator<T> From<T>(IEnumerator<T> e) { return e.ToIterator(); }
		public static IIterable<T> From<T>(IEnumerable<T> e) { return e.ToIterable(); }
		
		public static Iterator<T> Empty<T>()
		{
			return delegate(out T current)
			{
				current = default(T);
				return false;
			};
		}
		public static Iterator<T> Single<T>(T value) { return Repeat(value, 1); }
		public static Iterator<T> Repeat<T>(T value, int count)
		{
			return delegate(out T current)
			{
				current = value;
				return --count > 0;
			};
		}
		public static Iterator<T> RepeatForever<T>(T value)
		{
			return delegate(out T current)
			{
				current = value;
				return true;
			};
		}
	}
	public static class Iterator_<T>
	{
		public static Iterator<T> Empty = delegate(out T current)
		{
			current = default(T);
			return false;
		};
	}

	public static partial class Collections
	{
		public static IEnumerator<T> ToEnumerator<T>(this Iterator<T> i)
		{
			T current;
			while (i(out current))
				yield return current;
		}
		public static Iterator<T> ToIterator<T>(this IEnumerator<T> e)
		{
			return delegate(out T current)
			{
				if (e.MoveNext()) {
					current = e.Current;
					return true;
				} else {
					current = default(T);
					return false;
				}
			};
		}
		public static IEnumerable<T> ToEnumerable<T>(this IIterable<T> list)
		{
			Iterator<T> i = list.GetIterator();
			T current;
			while (i(out current))
				yield return current;
		}
		public static IIterable<T> ToIterable<T>(this IEnumerable<T> list)
		{
			return new IterableFromEnumerable<T>(list);
		}
		public static bool Contains<T>(this IIterable<T> list, T item)
		{
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			T current;
			for (var it = list.GetIterator(); it(out current); )
				if (comparer.Equals(item, current))
					return true;
			return false;
		}
		public static int CopyTo<T>(this IIterable<T> c, T[] array, int arrayIndex)
		{
			for (var it = c.GetIterator(); it(out array[arrayIndex]); arrayIndex++) { }
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
	/// public Iterator<T> GetIterator()
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

	public class IterableFromEnumerable<T> : AbstractWrapper<IEnumerable<T>>, IIterable<T>, IEnumerable<T>
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