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
		public static bool MoveNext<T>(this Iterator<T> it, out T value)
		{
			bool ended = false;
			value = it(ref ended);
			return !ended;
		}

		#region Conversion from IEnumerator

		public static Iterator<T> From<T>(IEnumerator<T> e) { return e.AsIterator(); }
		public static IIterable<T> From<T>(IEnumerable<T> e) { return e.AsIterable(); }
		
		public static IteratorEnumerator<T> ToEnumerator<T>(this Iterator<T> it)
		{
			return new IteratorEnumerator<T>(it);
		}
		public static Iterator<T> AsIterator<T>(this IEnumerator<T> e)
		{
			return delegate(ref bool ended)
			{
				if (e.MoveNext())
					return e.Current;
				else
				{
					ended = true;
					return default(T);
				}
			};
		}

		#endregion

		#region Simple sequences

		public static Iterator<T> Empty<T>()
		{
			return EmptyIterator<T>.Value;
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
		public static Iterator<int> Range(int start, int count)
		{
			int upTo = start + count;
			return delegate(ref bool ended)
			{
				if (start < upTo)
					return start++;
				ended = true;
				return upTo;
			};
		}
		public static Iterator<long> Range(long start, long count)
		{
			long upTo = start + count;
			return delegate(ref bool ended)
			{
				if (start < upTo)
					return start++;
				ended = true;
				return upTo;
			};
		}
		public static Iterator<int> CountForever(int start, int step)
		{
			start -= step;
			return delegate(ref bool ended)
			{
				return start += step;
			};
		}
		public static Iterator<long> CountForever(long start, long step)
		{
			start -= step;
			return delegate(ref bool ended)
			{
				return start += step;
			};
		}
		public static Iterator<double> CountForever(double start, double step)
		{
			return delegate(ref bool ended)
			{
				double r = start;
				start += step;
				return r;
			};
		}
		public static Iterator<float> CountForever(float start, float step)
		{
			return delegate(ref bool ended)
			{
				float r = start;
				start += step;
				return r;
			};
		}

		#endregion
	}

	public static class EmptyIterator<T>
	{
		public static Iterator<T> Value = delegate(ref bool ended)
		{
			ended = true;
			return default(T);
		};
	}

	public struct IteratorEnumerator<T> : IEnumerator<T>
	{
		Iterator<T> _it;
		T _current;
		public IteratorEnumerator(Iterator<T> it) { _it = it; _current = default(T); }

		public T Current
		{
			get { return _current; }
		}
		public void Dispose()
		{
		}
		object System.Collections.IEnumerator.Current
		{
			get { return _current; }
		}
		public bool MoveNext()
		{
			bool ended = false;
			_current = _it(ref ended);
			return !ended;
		}
		public void Reset()
		{
			throw new NotSupportedException("An Iterator<T> cannot be reset.");
		}
	}
}