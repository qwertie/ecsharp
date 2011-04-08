// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;

namespace Loyc.Runtime
{
	/// <summary>A high-performance alternative to IEnumerable(of T).</summary>
	/// <remarks>
	/// IIterable is to Iterator what IEnumerable is to IEnumerator.
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

	public static partial class Collections
	{
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
			for (; ; )
			{
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

	public class IterableFromDelegate<T> : IIterable<T>, IEnumerable<T>
	{
		Func<Iterator<T>> _iterable;
		public IterableFromDelegate(Func<Iterator<T>> iterable)
		{
			_iterable = iterable;
		}
		public Iterator<T> GetIterator()
		{
 			return _iterable();
		}
		public IEnumerator<T> GetEnumerator()
		{
			return _iterable().ToEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _iterable().ToEnumerator();
		}
	}

	public class RepeatingIterable<T> : IIterable<T>
	{
		int _count;
		T _value;
		
		public RepeatingIterable(T value, int count)
		{
			_count = count;
			_value = value;
		}
		public Iterator<T> GetIterator()
		{
			return Iterator.Repeat(_value, _count);
		}
	}
	
	public static partial class Iterable
	{
		// NOTE! The fact that all these methods return IIterable and not the actual
		// class type (e.g. IterableFromDelegate) is deliberate. The reason is that
		// many classes such as IterableFromDelegate and EmptyCollection implement
		// both IEnumerable<T> and IIterable<T>. This causes Linq operations to be
		// ambiguous, as the compiler cannot choose between the IEnumerable and
		// IIterable version of the extension methods. We return IIterable in order
		// to resolve the ambiguity.

		public static IIterable<T> ToIterable<T>(this Func<Iterator<T>> iterable)
		{
			return new IterableFromDelegate<T>(iterable);
		}
		public static IIterable<T> Single<T>(T value)
		{
			return new RepeatingIterable<T>(value, 1);
		}
		public static IIterable<T> Repeat<T>(T value, int count)
		{
			return new RepeatingIterable<T>(value, count);
		}
		public static IIterable<T> RepeatForever<T>(T value)
		{
			return new IterableFromDelegate<T>(delegate() { return Iterator.RepeatForever(value); });
		}
	}
}