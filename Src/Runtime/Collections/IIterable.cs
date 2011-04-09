// This file is part of the Loyc project. Licence: LGPL
using System;
using System.Collections.Generic;

namespace Loyc.Runtime
{
	/// <summary>A high-performance alternative to IEnumerable(of T).</summary>
	/// <remarks>
	/// <see cref="IIterable{T}"/> is to <see cref="Iterator{T}"/> what 
	/// <see cref="IEnumerable{T}"/> is to <see cref="IEnumerator{T}"/>.
	/// <para/>
	/// If you already implement IEnumerable and want to add support for <see
	/// cref="IIterable{T}"/>, you can easily do so using the ToIterator extension 
	/// method:
	/// <code>
	/// public Iterator&lt;T> GetIterator()
	/// {
	///     return GetEnumerator().ToIterator();
	/// }
	/// </code>
	/// Otherwise, consider deriving your class from <see cref="IterableBase{T}"/>
	/// so that you don't have to implement the GetEnumerator() methods yourself.
	/// See also: <see cref="SourceBase{T}"/>, <see cref="ListSourceBase{T}"/>, 
	/// <see cref="ListExBase{T}"/> 
	/// </remarks>
	#if CSharp4
	public interface IIterable<out T> : IEnumerable<T>
	#else
	public interface IIterable<T> : IEnumerable<T>
	#endif
	{
		Iterator<T> GetIterator();
	}

	/*public struct IterableEnumerable<T> : IEnumerable<T>
	{
		IIterable<T> _source;
		public IterableEnumerable(IIterable<T> source)
		{
			_source = source;
		}
		public IteratorEnumerator<T> GetEnumerator()
		{
			return new IteratorEnumerator<T>(_source.GetIterator());
		}
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		System.Collections.IEnumerator  System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}*/

	public static partial class CollectionInterfaces
	{
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

	public static partial class Collections
	{
		/*public static IEnumerable<T> ToEnumerable<T>(this IIterable<T> list)
		{
			var listE = list as IEnumerable<T>;
			if (listE != null)
				return listE;
			return new IterableEnumerable<T>(list);
		}*/
		/// <summary>Converts any IEnumerable object to IIterable.</summary>
		/// <remarks>This method is named "AsIterable" and not "ToIterable" because,
		/// in contrast to methods like ToArray() and ToList(), it does not make a 
		/// copy of the sequence.</remarks>
		public static IIterable<T> AsIterable<T>(this IEnumerable<T> list)
		{
			var listI = list as IIterable<T>;
			if (listI != null)
				return listI;
			return new IterableFromEnumerable<T>(list);
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

	/// <summary>A helper class that makes it easier to return objects that
	/// implement IIterable.</summary>
	/// <remarks>
	/// The C# compiler makes it extraordinarily easy to create objects that
	/// implement <see cref="IEnumerable{T}"/>. For example:
	/// <code>
	/// public IEnumerable&lt;int> Digits()
	/// {
	///     for (int i = 0; 10 > i; i++)
	///         yield return i;
	/// }
	/// </code>
	/// In C# 1.0 you would have had to write two classes to accomplish the same
	/// thing (one implementing IEnumerable, one implementing IEnumerator.)
	/// <see cref="IIterable{T}"/> cannot provide the same convenience without
	/// a customized C# compiler. However, this class makes the task slightly
	/// easier. The simplest IIterable version of the above code is as follows:
	/// <code>
	/// public IIterable&lt;int> Digits()
	/// {
	///     return new IteratorFactory&lt;int>(() =>
	///     {
	///         int i = -1;
	///         return (ref bool ended) =>
	///         {
	///             if (++i >= 10)
	///                 ended = true;
	///             return i;
	///         };
	///     });
	/// }
	/// </code>
	/// Here, the outer lambda function effectively implements IIterable{T}, and the
	/// inner lambda function implements Iterator{T}.
	/// </remarks>
	public class IteratorFactory<T> : IterableBase<T>
	{
		Func<Iterator<T>> _iterable;
		public IteratorFactory(Func<Iterator<T>> iterable)
		{
			_iterable = iterable;
		}
		public override Iterator<T> GetIterator()
		{
 			return _iterable();
		}
	}

	/// <summary>A helper class for implementing <see cref="IIterable{T}"/> that
	/// contains GetEnumerator implementations.</summary>
	public abstract class IterableBase<T> : IIterable<T>
	{
		public abstract Iterator<T> GetIterator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetIterator().ToEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetIterator().ToEnumerator();
		}
		public IteratorEnumerator<T> GetEnumerator()
		{
			return GetIterator().ToEnumerator();
		}
	}

	/// <summary>A sequence that simply repeats the same value a specified number of times.</summary>
	public class RepeatingIterable<T> : IterableBase<T>
	{
		int _count;
		T _value;
		
		public RepeatingIterable(T value, int count)
		{
			_count = count;
			_value = value;
		}
		public override Iterator<T> GetIterator()
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
			return new IteratorFactory<T>(iterable);
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
			return new IteratorFactory<T>(delegate() { return Iterator.RepeatForever(value); });
		}
		public static IteratorEnumerator<T> GetEnumerator<T>(this IIterable<T> iterable)
		{
			return new IteratorEnumerator<T>(iterable.GetIterator());
		}
	}
}