#region License, Terms and Author(s)
//
// Original Copyright (c) 2007-9 Atif Aziz, Joseph Albahari. All rights reserved.
// Modified to support IIterable<T> by David Piepgrass (April 2011)
//
//  Author(s):
//      Atif Aziz, http://www.raboof.com
//      David Piepgrass, http://loyc-etc.blogspot.com
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the New BSD License, a copy of which should have 
// been delivered along with this distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
#endregion

// $Id: Enumerable.cs 240 2010-10-19 21:49:03Z azizatif $

namespace Loyc.Collections.Linq
{
	#region Imports

	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using Loyc.Essentials;
	using Loyc.Collections.Impl;

	#endregion

	/// <summary>
	/// Provides a set of static (Shared in Visual Basic) methods for 
	/// querying objects that implement <see cref="IIterable{T}" />.
	/// </summary>
	public static partial class Iterable
	{
		public static RepeatedValueList<T> Single<T>(T value)
		{
			return new RepeatedValueList<T>(value, 1);
		}
		public static RepeatedValueList<T> Repeat<T>(T value, int count)
		{
			return new RepeatedValueList<T>(value, count);
		}
		public static IIterable<T> RepeatForever<T>(T value)
		{
			return new IteratorFactory<T>(delegate() { return Iterator.RepeatForever(value); });
		}
		public static IteratorEnumerator<T> GetEnumerator<T>(this IIterable<T> iterable)
		{
			return new IteratorEnumerator<T>(iterable.GetIterator());
		}
		public static IIterable<int> CountForever(int start, int step)
		{
			return new IteratorFactory<int>(() => Iterator.CountForever(start, step));
		}
		public static IIterable<long> CountForever(long start, long step)
		{
			return new IteratorFactory<long>(() => Iterator.CountForever(start, step));
		}
		public static IIterable<float> CountForever(float start, float step)
		{
			return new IteratorFactory<float>(() => Iterator.CountForever(start, step));
		}
		public static IIterable<double> CountForever(double start, double step)
		{
			return new IteratorFactory<double>(() => Iterator.CountForever(start, step));
		}

		/// <summary>
		/// Returns the input typed as <see cref="IIterable{T}"/>, which ensures 
		/// that you can call Linq-to-iterable methods without interference from
		/// the class that implements the IIterable interface.
		/// </summary>
		public static IIterable<T> AsIterable<T>(this IIterable<T> source)
		{
			return source;
		}

		public static IListSource<T> ToListSource<T>(this IIterable<T> source)
		{
			var ls = source as IListSource<T>;
			if (ls != null)
				return ls;
			return new DList<T>(source);
		}

		/// <summary>
		/// Returns an empty <see cref="IIterable{T}"/> that has the 
		/// specified type argument.
		/// </summary>
		public static IIterable<TResult> Empty<TResult>()
		{
			return EmptyList<TResult>.Value;
		}

		/// <summary>
		/// Converts the elements of an <see cref="IIterable{T}"/> to the 
		/// specified type.
		/// </summary>
		public static IIterable<TResult> Cast<T, TResult>(this IIterable<T> source) where TResult:T
		{
			CheckParam.IsNotNull("source", source);
			return new DoDownCast<T, TResult>(source);
		}

		class DoDownCast<T, TOut> : IterableBase<TOut> where TOut : T
		{
			protected IIterable<T> s;
			public DoDownCast(IIterable<T> source) { s = source; }
			public sealed override Iterator<TOut> GetIterator()
			{
				var it = s.GetIterator();
				return delegate(ref bool ended) { return (TOut)it(ref ended); };
			}
		}

		/// <summary>
		/// Filters the elements of an <see cref="IIterable{T}"/> based on a specified type.
		/// </summary>
		public static IIterable<TResult> OfType<T, TResult>(this IIterable<T> source) where TResult : T
		{
			CheckParam.IsNotNull("source", source);
			return new DoOfType<T, TResult>(source);
		}

		class DoOfType<T, TOut> : IterableBase<TOut> where TOut : T
		{
			protected IIterable<T> s;
			public DoOfType(IIterable<T> source) { s = source; }
			public sealed override Iterator<TOut> GetIterator()
			{
				var it = s.GetIterator();
				return delegate(ref bool ended) {
					for(;;) {
						T current = it(ref ended);
						if (ended)
							return default(TOut);
						if (current is TOut)
							return (TOut)current;
					}
				};
			}
		}

		/// <summary>
		/// Generates a sequence of integral numbers within a specified range.
		/// </summary>
		/// <param name="start">The value of the first integer in the sequence.</param>
		/// <param name="count">The number of sequential integers to generate.</param>
		public static IIterable<int> Range(int start, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", count, null);
			
			return new IteratorFactory<int>(() => Iterator.Range(start, count));
		}

		/// <summary>
		/// Filters a sequence of values based on a predicate.
		/// </summary>
		public static IIterable<T> Where<T>(this IIterable<T> source, Func<T, bool> predicate)
		{
			CheckParam.IsNotNull("predicate", predicate);
			return new DoWhere<T>(source, predicate);
		}

		class DoWhere<T> : IterableBase<T>
		{
			IIterable<T> s;
			Func<T,bool> p;
			public DoWhere(IIterable<T> source, Func<T,bool> predicate) { s = source; p = predicate; }
			public sealed override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				return delegate(ref bool ended) {
					T current;
					do {
						current = it(ref ended);
						if (ended)
							return default(T);
					} while(!p(current));
					return current;
				};
			}
		}

		/// <summary>
		/// Filters a sequence of values based on a predicate. 
		/// Each element's index is used in the logic of the predicate function.
		/// </summary>
		public static IIterable<T> Where<T>(this IIterable<T> source, Func<T, int, bool> predicate)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("predicate", predicate);
			return new DoWhere2<T>(source, predicate);
		}

		class DoWhere2<T> : IterableBase<T>
		{
			IIterable<T> s;
			Func<T, int, bool> _pred;
			public DoWhere2(IIterable<T> source, Func<T, int, bool> predicate) { s = source; _pred = predicate; }
			public sealed override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				var i = -1;
				return delegate(ref bool ended)
				{
					T current;
					do {
						current = it(ref ended);
						if (ended)
							return default(T);
						++i;
					} while (!_pred(current, i));
					return current;
				};
			}
		}

		/// <summary>
		/// Projects each element of a sequence into a new form.
		/// </summary>
		public static IIterable<TResult> Select<T, TResult>(this IIterable<T> source, Func<T, TResult> selector)
		{
			CheckParam.IsNotNull("selector", selector);

			return new DoSelect<T, TResult>(source, selector);
		}

		internal class DoSelect<T, TResult> : IterableBase<TResult>
		{
			protected IIterable<T> s;
			protected Func<T, TResult> _sel;
			public DoSelect(IIterable<T> source, Func<T, TResult> selector) { s = source; _sel = selector; }
			public sealed override Iterator<TResult> GetIterator()
			{
				var it = s.GetIterator();
				return delegate(ref bool ended)
				{
					T current = it(ref ended);
					if (ended) return default(TResult);
					return _sel(current);
				};
			}
		}

		/// <summary>
		/// Projects each element of a sequence into a new form by 
		/// incorporating the element's index.
		/// </summary>
		public static IIterable<TResult> Select<T, TResult>(this IIterable<T> source, Func<T, int, TResult> selector)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("selector", selector);
			return new DoSelect2<T, TResult>(source, selector);
		}

		class DoSelect2<T, TResult> : IterableBase<TResult>
		{
			protected IIterable<T> s;
			Func<T, int, TResult> sel;
			public DoSelect2(IIterable<T> source, Func<T, int, TResult> selector) { s = source; sel = selector; }
			public override Iterator<TResult> GetIterator()
			{
				var it = s.GetIterator();
				int i = -1;
				return delegate(ref bool ended)
				{
					T current = it(ref ended);
					if (ended) return default(TResult);
					++i;
					return sel(current, i);
				};
			}
		}

		public static IIterable<Pair<T, T>> AdjacentPairs<T>(this IIterable<T> source)
		{
			CheckParam.IsNotNull("source", source);

			return new IteratorFactoryWithState<IIterable<T>, Pair<T,T>>(source, s =>
			{
				// Get the first item of source
				Iterator<T> it = s.GetIterator();
				bool ended0 = false;
				T a, b = it(ref ended0);

				if (ended0)
					return EmptyIterator<Pair<T,T>>.Value;
				else {
					return delegate(ref bool ended)
					{
						a = b;
						b = it(ref ended);
						return new Pair<T,T>(a, b);
					};
				}
			});
		}

		/// <summary>
		/// Projects each element of a sequence to an <see cref="IIterable{T}" /> 
		/// and flattens the resulting sequences into one sequence.
		/// </summary>
		public static IIterable<TResult> SelectMany<T, TResult>(this IIterable<T> source, Func<T, IIterable<TResult>> selector)
		{
			CheckParam.IsNotNull("selector", selector);

			return Concat(Select(source, selector));
		}

		/// <summary>
		/// Projects each element of a sequence to an <see cref="IIterable{T}" />, 
		/// and flattens the resulting sequences into one sequence. The 
		/// index of each source element is used in the projected form of 
		/// that element.
		/// </summary>
		public static IIterable<TResult> SelectMany<T, TResult>(this IIterable<T> source, Func<T, int, IIterable<TResult>> selector)
		{
			CheckParam.IsNotNull("selector", selector);

			return Concat(Select(source, selector));
		}

		/// <summary>
		/// Concatenates any number of sequences.
		/// </summary>
		public static IIterable<T> Concat<T>(this IIterable<IIterable<T>> sets)
		{
			return new DoConcat<T>(sets);
		}
		
		class DoConcat<T> : IterableBase<T>
		{
			IIterable<IIterable<T>> s1;

			public DoConcat(IIterable<IIterable<T>> source) { s1 = source; }
			public override Iterator<T> GetIterator()
			{
				var i1 = s1.GetIterator();
				Iterator<T> i2 = null;

				return delegate(ref bool ended)
				{
					for (;;) {
						if (i2 != null)
						{
							bool ended2 = false;
							T current = i2(ref ended2);
							if (!ended2)
								return current;
							i2 = null;
						}

						var s2 = i1(ref ended);
						if (ended)
							return default(T);

						i2 = s2.GetIterator();
					}
				};
			}
		}

		/// <summary>
		/// Projects each element of a sequence to an <see cref="IIterable{T}" />, 
		/// flattens the resulting sequences into one sequence, and invokes 
		/// a result selector function on each element therein.
		/// </summary>
		public static IIterable<TResult> SelectMany<T, T2, TResult>(
			 this IIterable<T> source,
			 Func<T, IIterable<T2>> collectionSelector,
			 Func<T, T2, TResult> resultSelector)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("collectionSelector", collectionSelector);
			CheckParam.IsNotNull("resultSelector", resultSelector);

			Func<T, IIterable<Pair<T, T2>>> zip = t => collectionSelector(t).Select(t2 => Pair.Create(t, t2));
			return Concat(Select(source, zip)).Select(pair => resultSelector(pair.A, pair.B));
		}

		/// <summary>
		/// Projects each element of a sequence to an <see cref="IIterable{T}" />, 
		/// flattens the resulting sequences into one sequence, and invokes 
		/// a result selector function on each element therein. The index of 
		/// each source element is used in the intermediate projected form 
		/// of that element.
		/// </summary>
		public static IIterable<TResult> SelectMany<T, T2, TResult>(
			 this IIterable<T> source,
			 Func<T, int, IIterable<T2>> collectionSelector,
			 Func<T, T2, TResult> resultSelector)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("collectionSelector", collectionSelector);
			CheckParam.IsNotNull("resultSelector", resultSelector);

			int i = -1;
			Func<T, IIterable<Pair<T, T2>>> zip = (t => collectionSelector(t, ++i).Select(t2 => Pair.Create(t, t2)));
			return Concat(Select(source, zip)).Select(pair => resultSelector(pair.A, pair.B));
		}

		/// <summary>
		/// Returns elements from a sequence as long as a specified condition is true.
		/// </summary>
		public static IIterable<T> TakeWhile<T>(this IIterable<T> source, Func<T, bool> predicate)
		{
			CheckParam.IsNotNull("predicate", predicate);

			return new DoTakeWhile<T>(source, predicate);
		}

		class DoTakeWhile<T> : IterableBase<T>
		{
			IIterable<T> s;
			Func<T,bool> p;
			public DoTakeWhile(IIterable<T> source, Func<T,bool> predicate) { s = source; p = predicate; }
			public override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				bool stopped = false;
				return delegate(ref bool ended) {
                    if (!stopped)
                    {
                        T current = it(ref ended);
                        if (ended)
                            return default(T);
                        if (p(current))
                            return current;
                        stopped = true;
                    }
                    ended = true;
					return default(T);
				};
			}
		}

		/// <summary>
		/// Returns elements from a sequence as long as a specified condition is true.
		/// The element's index is used in the logic of the predicate function.
		/// </summary>
		public static IIterable<T> TakeWhile<T>(
			 this IIterable<T> source,
			 Func<T, int, bool> predicate)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("predicate", predicate);

			return new DoTakeWhile2<T>(source, predicate);
		}

		class DoTakeWhile2<T> : IterableBase<T>
		{
			IIterable<T> s;
			Func<T, int, bool> p;
			public DoTakeWhile2(IIterable<T> source, Func<T, int, bool> predicate) { s = source; p = predicate; }
			public override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				bool stopped = false;
				int i = -1;
				return delegate(ref bool ended) {
                    if (!stopped)
                    {
                        T current = it(ref ended);
                        if (ended)
                            return default(T);
                        if (p(current, ++i))
                            return current;
                        stopped = true;
                    }
                    ended = true;
					return default(T);
				};
			}
		}

		private static class Futures<T>
		{
			public static readonly Func<T> Default = () => default(T);
			public static readonly Func<T> Undefined = () => { throw new InvalidOperationException(); };
		}

		/// <summary>
		/// Base implementation of First operator.
		/// </summary>
		private static T FirstImpl<T>(this IIterable<T> source, Func<T> empty)
		{
			CheckParam.IsNotNull("source", source);
			Debug.Assert(empty != null);

			var it = source.GetIterator();
			bool ended = false;
			T first = it(ref ended);
			return ended ? empty() : first;
		}

		/// <summary>
		/// Returns the first element of a sequence.
		/// </summary>
		public static T First<T>(
			 this IIterable<T> source)
		{
			return source.FirstImpl(Futures<T>.Undefined);
		}

		/// <summary>
		/// Returns the first element in a sequence that satisfies a specified condition.
		/// </summary>
		public static T First<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return First(source.Where(predicate));
		}

		/// <summary>
		/// Returns the first element of a sequence, or a default value if 
		/// the sequence contains no elements.
		/// </summary>
		public static T FirstOrDefault<T>(
			 this IIterable<T> source)
		{
			return source.FirstImpl(Futures<T>.Default);
		}

		/// <summary>
		/// Returns the first element of the sequence that satisfies a 
		/// condition or a default value if no such element is found.
		/// </summary>
		public static T FirstOrDefault<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return FirstOrDefault(source.Where(predicate));
		}

		/// <summary>
		/// Base implementation of Last operator.
		/// </summary>
		private static T LastImpl<T>(
			 this IIterable<T> source,
			 Func<T> empty)
		{
			CheckParam.IsNotNull("source", source);

			var it = source.GetIterator();
			bool ended = false;
			T t = it(ref ended);
			if (ended)
				return empty();

			var listS = source as IListSource<T>;    // optimized case for lists
			if (listS != null)
				return listS[listS.Count - 1];
			var list = source as IList<T>;    // optimized case for lists
			if (list != null)
				return list[list.Count - 1];

			for(;;) {
				T next = it(ref ended);
				if (ended) return t;
				t = next;
			}
		}

		/// <summary>
		/// Returns the last element of a sequence.
		/// </summary>
		public static T Last<T>(
			 this IIterable<T> source)
		{
			return source.LastImpl(Futures<T>.Undefined);
		}

		/// <summary>
		/// Returns the last element of a sequence that satisfies a 
		/// specified condition.
		/// </summary>
		public static T Last<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return Last(source.Where(predicate));
		}

		/// <summary>
		/// Returns the last element of a sequence, or a default value if 
		/// the sequence contains no elements.
		/// </summary>
		public static T LastOrDefault<T>(
			 this IIterable<T> source)
		{
			return source.LastImpl(Futures<T>.Default);
		}

		/// <summary>
		/// Returns the last element of a sequence that satisfies a 
		/// condition or a default value if no such element is found.
		/// </summary>
		public static T LastOrDefault<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return LastOrDefault(source.Where(predicate));
		}

		/// <summary>
		/// Base implementation of Single operator.
		/// </summary>
		private static T SingleImpl<T>(
			 this IIterable<T> source,
			 Func<T> empty)
		{
			CheckParam.IsNotNull("source", source);

			var it = source.GetIterator();
			
			T single, next;
			if (it.MoveNext(out single))
			{
				if (it.MoveNext(out next))
					throw new InvalidOperationException("Single element expected");

				return single;
			}

			return empty();
		}

		/// <summary>
		/// Returns the only element of a sequence, and throws an exception 
		/// if there is not exactly one element in the sequence.
		/// </summary>
		public static T Single<T>(
			 this IIterable<T> source)
		{
			return source.SingleImpl(Futures<T>.Undefined);
		}

		/// <summary>
		/// Returns the only element of a sequence that satisfies a 
		/// specified condition, and throws an exception if more than one 
		/// such element exists.
		/// </summary>
		public static T Single<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return Single(source.Where(predicate));
		}

		/// <summary>
		/// Returns the only element of a sequence, or a default value if 
		/// the sequence is empty; this method throws an exception if there 
		/// is more than one element in the sequence.
		/// </summary>
		public static T SingleOrDefault<T>(
			 this IIterable<T> source)
		{
			return source.SingleImpl(Futures<T>.Default);
		}

		/// <summary>
		/// Returns the only element of a sequence that satisfies a 
		/// specified condition or a default value if no such element 
		/// exists; this method throws an exception if more than one element 
		/// satisfies the condition.
		/// </summary>
		public static T SingleOrDefault<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return SingleOrDefault(source.Where(predicate));
		}

		/// <summary>
		/// Returns the element at a specified index in a sequence.
		/// </summary>
		public static T ElementAt<T>(
			 this IIterable<T> source,
			 int index)
		{
			CheckParam.IsNotNull("source", source);

			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, null);

			var list = source as IListSource<T>;
			if (list != null)
				return list[index];

			try
			{
				return source.SkipWhile((item, i) => i < index).First();
			}
			catch (InvalidOperationException) // if thrown by First
			{
				throw new ArgumentOutOfRangeException("index", index, null);
			}
		}

		/// <summary>
		/// Returns the element at a specified index in a sequence or a 
		/// default value if the index is out of range.
		/// </summary>
		public static T ElementAtOrDefault<T>(
			 this IIterable<T> source,
			 int index)
		{
			CheckParam.IsNotNull("source", source);

			if (index < 0)
				return default(T);

			var list = source as IListSource<T>;
			if (list != null)
				return index < list.Count ? list[index] : default(T);

			return source.SkipWhile((item, i) => i < index).FirstOrDefault();
		}

		/// <summary>
		/// Inverts the order of the elements in a sequence.
		/// </summary>
		public static IListSource<T> Reverse<T>(this IIterable<T> source)
		{
			CheckParam.IsNotNull("source", source);

			return new ReversedListSource<T>(ToInternalList(source));
		}

		public static InternalList<T> ToInternalList<T>(IIterable<T> source)
		{
			var listS = source as IListSource<T>;
			if (listS != null)
				return new InternalList<T>(listS.ToArray(), listS.Count);

			var list = InternalList<T>.Empty;
			var it = source.GetIterator();
			for (bool ended = false;;)
			{
				T current = it(ref ended);
				if (ended)
					return list;
				list.Add(current);
			}
		}

		/// <summary>
		/// Returns a specified number of contiguous elements from the start 
		/// of a sequence.
		/// </summary>
		public static IIterable<T> Take<T>(this IIterable<T> source, int count)
		{
			return source.TakeWhile((item, i) => i < count);
		}

		/// <summary>
		/// Bypasses a specified number of elements in a sequence and then 
		/// returns the remaining elements.
		/// </summary>
		public static IIterable<T> Skip<T>(this IIterable<T> source, int count)
		{
			var list = source as IListSource<T>;
			if (list != null)
				return list.Slice(count, list.Count - count);

			return source.Where((item, i) => i >= count);
		}

		/// <summary>
		/// Bypasses elements in a sequence as long as a specified condition 
		/// is true and then returns the remaining elements.
		/// </summary>
		public static IIterable<T> SkipWhile<T>(this IIterable<T> source, Func<T, bool> predicate)
		{
			CheckParam.IsNotNull("predicate", predicate);

			return new DoSkipWhile<T>(source, predicate);
		}
		
		class DoSkipWhile<T> : IterableBase<T>
		{
			IIterable<T> s;
			Func<T,bool> p;
			public DoSkipWhile(IIterable<T> source, Func<T,bool> predicate) { s = source; p = predicate; }
			public override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				bool skip = true;
				return delegate(ref bool ended)
				{
					if (skip) {
						T current;
						do
							current = it(ref ended);
						while (!ended && p(current));
						skip = false;

						return current;
					}
					return it(ref ended);
				};
			}
		}

		/// <summary>
		/// Bypasses elements in a sequence as long as a specified condition 
		/// is true and then returns the remaining elements. The element's 
		/// index is used in the logic of the predicate function.
		/// </summary>
		public static IIterable<T> SkipWhile<T>(
			 this IIterable<T> source,
			 Func<T, int, bool> predicate)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("predicate", predicate);

			return new DoSkipWhile2<T>(source, predicate);
		}

		class DoSkipWhile2<T> : IterableBase<T>
		{
			IIterable<T> s;
			Func<T, int, bool> p;
			public DoSkipWhile2(IIterable<T> source, Func<T, int, bool> predicate) { s = source; p = predicate; }
			public override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				int i = -1;
				bool skip = true;
				return delegate(ref bool ended)
				{
					if (skip) {
						T current;
						do
							current = it(ref ended);
						while (!ended && p(current, ++i));
						skip = false;

						return current;
					}
					return it(ref ended);
				};
			}
		}

		/// <summary>
		/// Returns the number of elements in a sequence.
		/// </summary>
		public static int Count<T>(this IIterable<T> source)
		{
			CheckParam.IsNotNull("source", source);

			var source2 = source as ISource<T>;
			if (source2 != null)
				return source2.Count;
		
			var collection = source as ICollection;
			if (collection != null)
				return collection.Count;

			int count = 0;
			bool ended = false;
			var it = source.GetIterator();
			for (it(ref ended); !ended; it(ref ended))
				count++;
			return count;
		}

		/// <summary>
		/// Returns a number that represents how many elements in the 
		/// specified sequence satisfy a condition.
		/// </summary>
		public static int Count<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return Count(source.Where(predicate));
		}

		/// <summary>
		/// Returns an <see cref="Int64"/> that represents the total number 
		/// of elements in a sequence.
		/// </summary>
		public static long LongCount<T>(this IIterable<T> source)
		{
			CheckParam.IsNotNull("source", source);

			var array = source as Array;
			if (array != null)
				return array.Length;

			long count = 0;
			bool ended = false;
			var it = source.GetIterator();
			for (it(ref ended); !ended; it(ref ended))
				count++;
			return count;
		}

		/// <summary>
		/// Returns an <see cref="Int64"/> that represents how many elements 
		/// in a sequence satisfy a condition.
		/// </summary>
		public static long LongCount<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			return LongCount(source.Where(predicate));
		}

		/// <summary>
		/// Concatenates two sequences.
		/// </summary>
		public static IIterable<T> Concat<T>(
			 this IIterable<T> first,
			 IIterable<T> second)
		{
			CheckParam.IsNotNull("first", first);
			CheckParam.IsNotNull("second", second);

			return new DoConcat2<T>(first, second);
		}

		class DoConcat2<T> : IterableBase<T>
		{
			IIterable<T> s;
			IIterable<T> s2;

			public DoConcat2(IIterable<T> source, IIterable<T> source2) { s = source; s2 = source2; }
			public override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				return delegate(ref bool ended)
				{
					bool ended1 = false;
					T current = it(ref ended1);
					if (ended1)
					{
						if (s2 != null) {
							it = s2.GetIterator();
							s2 = null;
							current = it(ref ended);
						} else
							ended = true;
					}
					return current;
				};
			}
		}

		/// <summary>
		/// Creates a <see cref="List{T}"/> from an <see cref="IIterable{T}"/>.
		/// </summary>
		public static List<T> ToList<T>(
			 this IIterable<T> source)
		{
			CheckParam.IsNotNull("source", source);

			return new List<T>(source.AsEnumerable());
		}

		/// <summary>
		/// Creates an array from an <see cref="IIterable{T}"/>.
		/// </summary>
		public static T[] ToArray<T>(
			 this IIterable<T> source)
		{
			var list = ToInternalList(source);
			if (list.Count == list.InternalArray.Length)
				return list.InternalArray;
			else
				return list.ToArray();
		}

		/// <summary>
		/// Returns distinct elements from a sequence by using the default 
		/// equality comparer to compare values.
		/// </summary>
		public static IIterable<T> Distinct<T>(
			 this IIterable<T> source)
		{
			return Distinct(source, /* comparer */ null);
		}

		/// <summary>
		/// Returns distinct elements from a sequence by using a specified 
		/// <see cref="IEqualityComparer{T}"/> to compare values.
		/// </summary>
		public static IIterable<T> Distinct<T>(
			 this IIterable<T> source,
			 IEqualityComparer<T> comparer)
		{
			CheckParam.IsNotNull("source", source);

			return new IteratorFactory<T>(() => DistinctIterable(source, comparer));
		}

		private static Iterator<T> DistinctIterable<T>(IIterable<T> source, IEqualityComparer<T> comparer)
		{
			var set = new Dictionary<T, object>(comparer);
			var gotNull = false;

			var it = source.GetIterator();
			return delegate(ref bool ended)
			{
				for (;;) {
					T item = it(ref ended);
					if (ended)
						return default(T);

					if (item == null)
					{
						if (gotNull)
							continue;
						gotNull = true;
					}
					else
					{
						if (set.ContainsKey(item))
							continue;
						set.Add(item, null);
					}

					return item;
				}
			};
		}

		// REMOVED:
		// ToLookup() and GroupBy() methods have been removed. Calling ToLookup or 
        // GroupBy will automatically fall back on existing LINQ implementation for 
        // IEnumerable, provided that the user is "using System.Linq".

		/// <summary>
		/// Applies an accumulator function over a sequence.
		/// </summary>
		public static T Aggregate<T>(this IIterable<T> source, Func<T, T, T> func)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("func", func);

			var it = source.GetIterator();
			T total;
			if (!it.MoveNext(out total))
				throw new InvalidOperationException("Aggregate cannot operate on an empty sequence");

			for (;;) {
				bool ended = false;
				T next = it(ref ended);
				if (ended)
					return total;
				total = func(total, next);
			}
		}
		
		/// <summary>
		/// Applies an accumulator function over a sequence. The specified 
		/// seed value is used as the initial accumulator value.
		/// </summary>
		public static TAccumulate Aggregate<T, TAccumulate>(
			 this IIterable<T> source,
			 TAccumulate seed,
			 Func<TAccumulate, T, TAccumulate> func)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("func", func);

			var result = seed;

			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				T item = it(ref ended);
				if (ended) return result;
				result = func(result, item);
			}
		}

		/// <summary>
		/// Applies an accumulator function over a sequence. The specified 
		/// seed value is used as the initial accumulator value, and the 
		/// specified function is used to select the result value.
		/// </summary>

		public static TResult Aggregate<T, TAccumulate, TResult>(
			 this IIterable<T> source,
			 TAccumulate seed,
			 Func<TAccumulate, T, TAccumulate> func,
			 Func<TAccumulate, TResult> resultSelector)
		{
			CheckParam.IsNotNull("resultSelector", resultSelector);
			return resultSelector(Aggregate(source, seed, func));
		}

		/// <summary>
		/// Produces the set union of two sequences by using the default 
		/// equality comparer.
		/// </summary>

		public static IIterable<T> Union<T>(
			 this IIterable<T> first,
			 IIterable<T> second)
		{
			return Union(first, second, /* comparer */ null);
		}

		/// <summary>
		/// Produces the set union of two sequences by using a specified 
		/// <see cref="IEqualityComparer{T}" />.
		/// </summary>
		public static IIterable<T> Union<T>(
			 this IIterable<T> first,
			 IIterable<T> second,
			 IEqualityComparer<T> comparer)
		{
			return first.Concat(second).Distinct(comparer);
		}

		/// <summary>
		/// Returns the elements of the specified sequence or the type 
		/// parameter's default value in a singleton collection if the 
		/// sequence is empty.
		/// </summary>
		public static IIterable<T> DefaultIfEmpty<T>(
			 this IIterable<T> source)
		{
			return DefaultIfEmpty(source, default(T));
		}

		/// <summary>
		/// Returns the elements of the specified sequence or the specified 
		/// value in a singleton collection if the sequence is empty.
		/// </summary>
		public static IIterable<T> DefaultIfEmpty<T>(
			 this IIterable<T> source,
			 T defaultValue)
		{
			CheckParam.IsNotNull("source", source);

			return new DoDefaultIfEmpty<T>(source, Iterable.Single(defaultValue));
		}

		/// <summary>
		/// Returns the elements of the specified source sequence or, if that
		/// sequence is empty, the elements of the specified fallback sequence.
		/// </summary>
		public static IIterable<T> DefaultIfEmpty<T>(
			 this IIterable<T> source,
			 IIterable<T> fallback)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("fallback", fallback);

			return new DoDefaultIfEmpty<T>(source, fallback);
		}

		class DoDefaultIfEmpty<T> : IterableBase<T>
		{
			IIterable<T> s;
			IIterable<T> f;

			public DoDefaultIfEmpty(IIterable<T> source, IIterable<T> fallback) { s = source; f = fallback; }
			public override Iterator<T> GetIterator()
			{
				var it = s.GetIterator();
				return delegate(ref bool ended)
				{
					bool ended1 = false;
					T current = it(ref ended1);
					if (ended1)
					{
						if (f != null) {
							it = f.GetIterator();
							f = null;
							current = it(ref ended);
						} else
							ended = true;
					}
					f = null;
					return current;
				};
			}
		}

		/// <summary>
		/// Determines whether all elements of a sequence satisfy a condition.
		/// </summary>
		public static bool All<T>(
			 this IIterable<T> source,
			 Func<T, bool> predicate)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("predicate", predicate);

			var it = source.GetIterator();
			bool ended = false;
			for (;;) {
				T item = it(ref ended);
				if (ended)
					return true;
				if (!predicate(item))
					return false;
			}
		}

		/// <summary>
		/// Determines whether a sequence contains any elements.
		/// </summary>
		public static bool Any<T>(this IIterable<T> source)
		{
			return !Empty(source);
		}

		/// <summary>
		/// Determines whether a sequence contains any elements.
		/// </summary>
		public static bool Empty<T>(this IIterable<T> source)
		{
			CheckParam.IsNotNull("source", source);

			bool ended = false;
			source.GetIterator()(ref ended);
			return ended;
		}

		/// <summary>
		/// Determines whether any element of a sequence satisfies a 
		/// condition.
		/// </summary>
		public static bool Any<T>(this IIterable<T> source, Func<T, bool> predicate)
		{
			return Any(Where(source, predicate));
		}

		/// <summary>
		/// Determines whether a sequence contains a specified element by 
		/// using the default equality comparer.
		/// </summary>
		public static bool Contains<T>(
			 this IIterable<T> source,
			 T value)
		{
			return source.Contains(value, /* comparer */ null);
		}

		/// <summary>
		/// Determines whether a sequence contains a specified element by 
		/// using a specified <see cref="IEqualityComparer{T}" />.
		/// </summary>
		public static bool Contains<T>(
			 this IIterable<T> source,
			 T value,
			 IEqualityComparer<T> comparer)
		{
			CheckParam.IsNotNull("source", source);

			if (comparer == null)
			{
				var collection = source as ICollection<T>;
				if (collection != null)
					return collection.Contains(value);
			}

			comparer = comparer ?? EqualityComparer<T>.Default;
			return source.Any(item => comparer.Equals(item, value));
		}

		/// <summary>
		/// Determines whether two sequences are equal by comparing the 
		/// elements by using the default equality comparer for their type.
		/// </summary>
		public static bool SequenceEqual<T>(
			 this IIterable<T> first,
			 IIterable<T> second)
		{
			return first.SequenceEqual(second, /* comparer */ null);
		}

		/// <summary>
		/// Determines whether two sequences are equal by comparing their 
		/// elements by using a specified <see cref="IEqualityComparer{T}" />.
		/// </summary>
		public static bool SequenceEqual<T>(
			 this IIterable<T> first,
			 IIterable<T> second,
			 IEqualityComparer<T> comparer)
		{
			CheckParam.IsNotNull("first", first);
			CheckParam.IsNotNull("second", second);

			comparer = comparer ?? EqualityComparer<T>.Default;

			var it1 = first.GetIterator();
			var it2 = second.GetIterator();
			bool ended1 = false, ended2 = false;
			for (;;)
			{
				T current1 = it1(ref ended1);
				T current2 = it2(ref ended2);
				if (ended1 && ended2)
					return true;
				if (ended1 != ended2 || !comparer.Equals(current1, current2))
					return false;
			}
		}

		/// <summary>
		/// Base implementation for Min/Max operator.
		/// </summary>

		private static T MinMaxImpl<T>(
			 this IIterable<T> source,
			 Func<T, T, bool> lesser)
		{
			CheckParam.IsNotNull("source", source);
			Debug.Assert(lesser != null);

			return source.Aggregate((a, item) => lesser(a, item) ? a : item);
		}

		/// <summary>
		/// Base implementation for Min/Max operator for nullable types.
		/// </summary>

		private static T? MinMaxImpl<T>(
			 this IIterable<T?> source,
			 T? seed, Func<T?, T?, bool> lesser) where T : struct
		{
			CheckParam.IsNotNull("source", source);
			Debug.Assert(lesser != null);

			return source.Aggregate(seed, (a, item) => lesser(a, item) ? a : item);
			//  == MinMaxImpl(Repeat<T?>(null, 1).Concat(source), lesser);
		}

		/// <summary>
		/// Returns the minimum value in a generic sequence.
		/// </summary>

		public static T Min<T>(
			 this IIterable<T> source)
		{
			var comparer = Comparer<T>.Default;
			return source.MinMaxImpl((x, y) => comparer.Compare(x, y) < 0);
		}

		/// <summary>
		/// Invokes a transform function on each element of a generic 
		/// sequence and returns the minimum resulting value.
		/// </summary>

		public static TResult Min<T, TResult>(
			 this IIterable<T> source,
			 Func<T, TResult> selector)
		{
			return source.Select(selector).Min();
		}

		/// <summary>
		/// Returns the maximum value in a generic sequence.
		/// </summary>

		public static T Max<T>(
			 this IIterable<T> source)
		{
			var comparer = Comparer<T>.Default;
			return source.MinMaxImpl((x, y) => comparer.Compare(x, y) > 0);
		}

		/// <summary>
		/// Invokes a transform function on each element of a generic 
		/// sequence and returns the maximum resulting value.
		/// </summary>

		public static TResult Max<T, TResult>(
			 this IIterable<T> source,
			 Func<T, TResult> selector)
		{
			return source.Select(selector).Max();
		}

        // REMOVED:
        // OrderBy(), OrderByDescending() and ThenBy() methods have been removed 
        // because they return IOrderedEnumerable<T>, which currently has no 
        // IIterable equivalent. Calling OrderBy will automatically fall back on 
        // existing LINQ implementation for IEnumerable, if the user is "using 
        // System.Linq".

		class Lazy<T>
		{
			Func<T> _factory;
			T _value;

			public T Value
			{
				get { 
					if (_factory != null) {
                        _value = _factory();
                        _factory = null;
					}
					return _value;
				}
			}
			public bool IsValueCreated
			{
                get { return _factory == null; }
			}
			public Lazy()
			{
				_factory = () => (T) Activator.CreateInstance(typeof(T));
			}
			public Lazy(Func<T> valueFactory)
			{
				_factory = valueFactory;
			}
			public override string ToString()
			{
 				return Value.ToString();
			}
		}
		class LazyIterable<T> : IterableBase<T>
		{
			Lazy<IIterable<T>> s;
			public LazyIterable(Func<IIterable<T>> getter) { s = new Lazy<IIterable<T>>(getter); }
			public override Iterator<T> GetIterator() { return s.Value.GetIterator(); }
		}

		/// <summary>
		/// Base implementation for Intersect and Except operators.
		/// </summary>
		private static IIterable<T> IntersectExceptImpl<T>(
			 this IIterable<T> first,
			 IIterable<T> second,
			 IEqualityComparer<T> comparer,
			 bool flag)
		{
			CheckParam.IsNotNull("first", first);
			CheckParam.IsNotNull("second", second);

			var keys = InternalList<T>.Empty;
			var flags = new Dictionary<T, bool>(comparer);
			{
				T item;
				for (var it = first.Where(k => !flags.ContainsKey(k)).GetIterator(); it.MoveNext(out item); )
				{
					flags.Add(item, !flag);
					keys.Add(item);
				}

				for (var it = second.Where(flags.ContainsKey).GetIterator(); it.MoveNext(out item); )
					flags[item] = flag;
			}
			// As per docs, "the marked elements are yielded in the order in 
			// which they were collected."
			return keys.Where(item => flags[item]);
		}

		/// <summary>
		/// Produces the set intersection of two sequences by using the 
		/// default equality comparer to compare values.
		/// </summary>
		public static IIterable<T> Intersect<T>(
			 this IIterable<T> first,
			 IIterable<T> second)
		{
			return first.Intersect(second, /* comparer */ null);
		}

		/// <summary>
		/// Produces the set intersection of two sequences by using the 
		/// specified <see cref="IEqualityComparer{T}" /> to compare values.
		/// </summary>
		public static IIterable<T> Intersect<T>(
			 this IIterable<T> first,
			 IIterable<T> second,
			 IEqualityComparer<T> comparer)
		{
			return new LazyIterable<T>(() => IntersectExceptImpl(first, second, comparer, /* flag */ true));
		}

		/// <summary>
		/// Produces the set difference of two sequences by using the 
		/// default equality comparer to compare values.
		/// </summary>
		public static IIterable<T> Except<T>(
			 this IIterable<T> first,
			 IIterable<T> second)
		{
			return first.Except(second, /* comparer */ null);
		}

		/// <summary>
		/// Produces the set difference of two sequences by using the 
		/// specified <see cref="IEqualityComparer{T}" /> to compare values.
		/// </summary>

		public static IIterable<T> Except<T>(
			 this IIterable<T> first,
			 IIterable<T> second,
			 IEqualityComparer<T> comparer)
		{
			return IntersectExceptImpl(first, second, comparer, /* flag */ false);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TValue}" /> from an 
		/// <see cref="IIterable{T}" /> according to a specified key 
		/// selector function.
		/// </summary>

		public static Dictionary<TKey, T> ToDictionary<T, TKey>(
			 this IIterable<T> source,
			 Func<T, TKey> keySelector)
		{
			return source.ToDictionary(keySelector, /* comparer */ null);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TValue}" /> from an 
		/// <see cref="IIterable{T}" /> according to a specified key 
		/// selector function and key comparer.
		/// </summary>

		public static Dictionary<TKey, T> ToDictionary<T, TKey>(
			 this IIterable<T> source,
			 Func<T, TKey> keySelector,
			 IEqualityComparer<TKey> comparer)
		{
			return source.ToDictionary(keySelector, e => e);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TValue}" /> from an 
		/// <see cref="IIterable{T}" /> according to specified key 
		/// selector and element selector functions.
		/// </summary>

		public static Dictionary<TKey, TElement> ToDictionary<T, TKey, TElement>(
			 this IIterable<T> source,
			 Func<T, TKey> keySelector,
			 Func<T, TElement> elementSelector)
		{
			return source.ToDictionary(keySelector, elementSelector, /* comparer */ null);
		}

		/// <summary>
		/// Creates a <see cref="Dictionary{TKey,TValue}" /> from an 
		/// <see cref="IIterable{T}" /> according to a specified key 
		/// selector function, a comparer, and an element selector function.
		/// </summary>
		public static Dictionary<TKey, TElement> ToDictionary<T, TKey, TElement>(
			 this IIterable<T> source,
			 Func<T, TKey> keySelector,
			 Func<T, TElement> elementSelector,
			 IEqualityComparer<TKey> comparer)
		{
			CheckParam.IsNotNull("source", source);
			CheckParam.IsNotNull("keySelector", keySelector);
			CheckParam.IsNotNull("elementSelector", elementSelector);

			var dict = new Dictionary<TKey, TElement>(comparer);

			T item;
			for (var it = source.GetIterator(); it.MoveNext(out item); )
			{
				//
				// ToDictionary is meant to throw ArgumentNullException if
				// keySelector produces a key that is null and 
				// Argument exception if keySelector produces duplicate keys 
				// for two elements. Incidentally, the doucmentation for
				// IDictionary<TKey, TValue>.Add says that the Add method
				// throws the same exceptions under the same circumstances
				// so we don't need to do any additional checking or work
				// here and let the Add implementation do all the heavy
				// lifting.
				//

				dict.Add(keySelector(item), elementSelector(item));
			}

			return dict;
		}

		/// <summary>
		/// Correlates the elements of two sequences based on matching keys. 
		/// The default equality comparer is used to compare keys.
		/// </summary>
		public static IIterable<TResult> Join<TOuter, TInner, TKey, TResult>(
			 this IIterable<TOuter> outer,
			 IIterable<TInner> inner,
			 Func<TOuter, TKey> outerKeySelector,
			 Func<TInner, TKey> innerKeySelector,
			 Func<TOuter, TInner, TResult> resultSelector)
		{
			return outer.Join(inner, outerKeySelector, innerKeySelector, resultSelector, /* comparer */ null);
		}

		/// <summary>
		/// Correlates the elements of two sequences based on matching keys. 
		/// The default equality comparer is used to compare keys. A 
		/// specified <see cref="IEqualityComparer{T}" /> is used to compare keys.
		/// </summary>
		public static IIterable<TResult> Join<TOuter, TInner, TKey, TResult>(
			 this IIterable<TOuter> outer,
			 IIterable<TInner> inner,
			 Func<TOuter, TKey> outerKeySelector,
			 Func<TInner, TKey> innerKeySelector,
			 Func<TOuter, TInner, TResult> resultSelector,
			 IEqualityComparer<TKey> comparer)
		{
			CheckParam.IsNotNull("outer", outer);
			CheckParam.IsNotNull("inner", inner);
			CheckParam.IsNotNull("outerKeySelector", outerKeySelector);
			CheckParam.IsNotNull("innerKeySelector", innerKeySelector);
			CheckParam.IsNotNull("resultSelector", resultSelector);

			var lookup = inner.AsEnumerable().ToLookup(innerKeySelector, comparer);

			return outer.SelectMany(o => lookup[outerKeySelector(o)].AsIterable(), 
			                   (o, i) => resultSelector(o, i));
		}


		/// <summary>
		/// Correlates the elements of two sequences based on equality of 
		/// keys and groups the results. The default equality comparer is 
		/// used to compare keys.
		/// </summary>
		public static IIterable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
			 this IIterable<TOuter> outer,
			 IIterable<TInner> inner,
			 Func<TOuter, TKey> outerKeySelector,
			 Func<TInner, TKey> innerKeySelector,
			 Func<TOuter, IIterable<TInner>, TResult> resultSelector)
		{
			return outer.GroupJoin(inner, outerKeySelector, innerKeySelector, resultSelector, /* comparer */ null);
		}

		/// <summary>
		/// Correlates the elements of two sequences based on equality of 
		/// keys and groups the results. The default equality comparer is 
		/// used to compare keys. A specified <see cref="IEqualityComparer{T}" /> 
		/// is used to compare keys.
		/// </summary>
		public static IIterable<TResult> GroupJoin<TOuter, TInner, TKey, TResult>(
			 this IIterable<TOuter> outer,
			 IIterable<TInner> inner,
			 Func<TOuter, TKey> outerKeySelector,
			 Func<TInner, TKey> innerKeySelector,
			 Func<TOuter, IIterable<TInner>, TResult> resultSelector,
			 IEqualityComparer<TKey> comparer)
		{
			CheckParam.IsNotNull("outer", outer);
			CheckParam.IsNotNull("inner", inner);
			CheckParam.IsNotNull("outerKeySelector", outerKeySelector);
			CheckParam.IsNotNull("innerKeySelector", innerKeySelector);
			CheckParam.IsNotNull("resultSelector", resultSelector);

			var lookup = inner.AsEnumerable().ToLookup(innerKeySelector, comparer);
			return outer.Select(o => resultSelector(o, lookup[outerKeySelector(o)].AsIterable()));
		}
	}
}

// $Id: Enumerable.g.cs 215 2009-10-03 13:31:49Z azizatif $

namespace Loyc.Collections.Linq
{
	#region Imports

	using System;
	using System.Collections.Generic;
	using Loyc.Collections;
	using Loyc.Essentials;

	#endregion

	// This partial implementation was template-generated:
	// Sat, 03 Oct 2009 09:42:39 GMT

	public static partial class Iterable
	{
		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Int32" /> values.
		/// </summary>
		public static int Sum(this IIterable<int> source)
		{
			CheckParam.IsNotNull("source", source);

			int sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				int num = it(ref ended);
				if (ended)
					return sum;
				checked { sum += num; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Int32" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>
		public static int Sum<T>(this IIterable<T> source, Func<T, int> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Int32" /> values.
		/// </summary>
		public static double Average(this IIterable<int> source)
		{
			CheckParam.IsNotNull("source", source);

			long sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				int num = it(ref ended);
				if (ended)
					break;
				checked { sum += num; }
			}
			
			if (count == 0)
				throw new InvalidOperationException();
			return (double)sum / count;
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Int32" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static double Average<T>(this IIterable<T> source, Func<T, int> selector)
		{
			return source.Select(selector).Average();
		}


		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Int32" /> values.
		/// </summary>
		public static int? Sum(this IIterable<int?> source)
		{
			CheckParam.IsNotNull("source", source);

			int sum = 0;
			bool ended = false;
			for (var it = source.GetIterator();;)
			{
				int? num = it(ref ended);
				if (ended)
					return sum;
				if (num.HasValue)
					checked { sum += num.Value; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Int32" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>
		public static int? Sum<T>(this IIterable<T> source, Func<T, int?> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Int32" /> values.
		/// </summary>
		public static double? Average(
			 this IIterable<int?> source)
		{
			CheckParam.IsNotNull("source", source);

			long sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				int? num = it(ref ended);
				if (ended)
					break;
				if (num.HasValue)
					checked { sum += num.Value; }
			}
			
			if (count == 0)
				return null;
			return (double?)sum / count;
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Int32" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static double? Average<T>(this IIterable<T> source, Func<T, int?> selector)
		{
			return source.Select(selector).Average();
		}

		/// <summary>
		/// Returns the minimum value in a sequence of nullable 
		/// <see cref="System.Int32" /> values.
		/// </summary>

		public static int? Min(
			 this IIterable<int?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null), null, (min, x) => min < x);
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the minimum nullable <see cref="System.Int32" /> value.
		/// </summary>

		public static int? Min<T>(
			 this IIterable<T> source,
			 Func<T, int?> selector)
		{
			return source.Select(selector).Min();
		}

		/// <summary>
		/// Returns the maximum value in a sequence of nullable 
		/// <see cref="System.Int32" /> values.
		/// </summary>

		public static int? Max(
			 this IIterable<int?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null),
				 null, (max, x) => x == null || (max != null && x.Value < max.Value));
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the maximum nullable <see cref="System.Int32" /> value.
		/// </summary>

		public static int? Max<T>(
			 this IIterable<T> source,
			 Func<T, int?> selector)
		{
			return source.Select(selector).Max();
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Int64" /> values.
		/// </summary>
		public static long Sum(this IIterable<long> source)
		{
			CheckParam.IsNotNull("source", source);

			long sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				long num = it(ref ended);
				if (ended)
					return sum;
				checked { sum += num; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Int64" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>
		public static long Sum<T>(this IIterable<T> source, Func<T, long> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Int64" /> values.
		/// </summary>
		public static double Average(this IIterable<long> source)
		{
			CheckParam.IsNotNull("source", source);

			double sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				long num = it(ref ended);
				if (ended)
					break;
				sum += (double)num;
			}
			
			if (count == 0)
				throw new InvalidOperationException();
			return sum / (double)count;
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Int64" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static double Average<T>(this IIterable<T> source, Func<T, long> selector)
		{
			return source.Select(selector).Average();
		}


		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Int64" /> values.
		/// </summary>
		public static long? Sum(this IIterable<long?> source)
		{
			CheckParam.IsNotNull("source", source);

			long sum = 0;
			bool ended = false;
			for (var it = source.GetIterator();;)
			{
				long? num = it(ref ended);
				if (ended)
					return sum;
				if (num.HasValue)
					checked { sum += num.Value; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Int64" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>
		public static long? Sum<T>(this IIterable<T> source, Func<T, long?> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Int64" /> values.
		/// </summary>
		public static double? Average(this IIterable<long?> source)
		{
			CheckParam.IsNotNull("source", source);

			double sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				long? num = it(ref ended);
				if (ended)
					break;
				if (num.HasValue)
					sum += num.Value;
			}

			if (count == 0)
				return null;
			return sum / (double)count;
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Int64" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static double? Average<T>(this IIterable<T> source, Func<T, long?> selector)
		{
			return source.Select(selector).Average();
		}

		/// <summary>
		/// Returns the minimum value in a sequence of nullable 
		/// <see cref="System.Int64" /> values.
		/// </summary>
		public static long? Min(
			 this IIterable<long?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null), null, (min, x) => min < x);
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the minimum nullable <see cref="System.Int64" /> value.
		/// </summary>

		public static long? Min<T>(
			 this IIterable<T> source,
			 Func<T, long?> selector)
		{
			return source.Select(selector).Min();
		}

		/// <summary>
		/// Returns the maximum value in a sequence of nullable 
		/// <see cref="System.Int64" /> values.
		/// </summary>

		public static long? Max(
			 this IIterable<long?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null),
				 null, (max, x) => x == null || (max != null && x.Value < max.Value));
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the maximum nullable <see cref="System.Int64" /> value.
		/// </summary>

		public static long? Max<T>(
			 this IIterable<T> source,
			 Func<T, long?> selector)
		{
			return source.Select(selector).Max();
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Single" /> values.
		/// </summary>
		public static float Sum(this IIterable<float> source)
		{
			CheckParam.IsNotNull("source", source);

			float sum = 0;
			bool ended = false;
			for (var it = source.GetIterator();;)
			{
				float num = it(ref ended);
				if (ended)
					return sum;
				checked { sum += num; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Single" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>

		public static float Sum<T>(
			 this IIterable<T> source,
			 Func<T, float> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Single" /> values.
		/// </summary>
		public static float Average(this IIterable<float> source)
		{
			CheckParam.IsNotNull("source", source);

			float sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				float num = it(ref ended);
				if (ended)
					break;
				checked { sum += num; }
			}

			if (count == 0)
				throw new InvalidOperationException();
			return sum / (float)count;
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Single" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static float Average<T>(this IIterable<T> source, Func<T, float> selector)
		{
			return source.Select(selector).Average();
		}


		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Single" /> values.
		/// </summary>
		public static float? Sum(this IIterable<float?> source)
		{
			CheckParam.IsNotNull("source", source);

			float sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				float? num = it(ref ended);
				if (ended)
					return sum;
				if (num.HasValue)
					sum += num.Value;
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Single" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>
		public static float? Sum<T>(this IIterable<T> source, Func<T, float?> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Single" /> values.
		/// </summary>
		public static float? Average(
			 this IIterable<float?> source)
		{
			CheckParam.IsNotNull("source", source);

			float sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				float? num = it(ref ended);
				if (ended)
					break;
				if (num.HasValue)
					sum += num.Value;
			}

			if (count == 0)
				return null;
			return sum / (float)count;
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Single" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static float? Average<T>(this IIterable<T> source, Func<T, float?> selector)
		{
			return source.Select(selector).Average();
		}

		/// <summary>
		/// Returns the minimum value in a sequence of nullable 
		/// <see cref="System.Single" /> values.
		/// </summary>
		public static float? Min(
			 this IIterable<float?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null), null, (min, x) => min < x);
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the minimum nullable <see cref="System.Single" /> value.
		/// </summary>

		public static float? Min<T>(
			 this IIterable<T> source,
			 Func<T, float?> selector)
		{
			return source.Select(selector).Min();
		}

		/// <summary>
		/// Returns the maximum value in a sequence of nullable 
		/// <see cref="System.Single" /> values.
		/// </summary>

		public static float? Max(
			 this IIterable<float?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null),
				 null, (max, x) => x == null || (max != null && x.Value < max.Value));
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the maximum nullable <see cref="System.Single" /> value.
		/// </summary>

		public static float? Max<T>(
			 this IIterable<T> source,
			 Func<T, float?> selector)
		{
			return source.Select(selector).Max();
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Double" /> values.
		/// </summary>

		public static double Sum(this IIterable<double> source)
		{
			CheckParam.IsNotNull("source", source);

			double sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				double num = it(ref ended);
				if (ended)
					return sum;
				checked { sum += num; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Double" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>

		public static double Sum<T>(
			 this IIterable<T> source,
			 Func<T, double> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Double" /> values.
		/// </summary>
		public static double Average(this IIterable<double> source)
		{
			CheckParam.IsNotNull("source", source);

			double sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator();; count++)
			{
				double num = it(ref ended);
				if (ended)
					break;
				checked { sum += num; }
			}

			if (count == 0)
				throw new InvalidOperationException();
			return sum / (double)count;
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Double" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static double Average<T>(this IIterable<T> source, Func<T, double> selector)
		{
			return source.Select(selector).Average();
		}


		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Double" /> values.
		/// </summary>
		public static double? Sum(this IIterable<double?> source)
		{
			CheckParam.IsNotNull("source", source);

			double sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				double? num = it(ref ended);
				if (ended)
					return sum;
				if (num.HasValue)
					sum += num.Value;
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Double" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>

		public static double? Sum<T>(
			 this IIterable<T> source,
			 Func<T, double?> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Double" /> values.
		/// </summary>
		public static double? Average(this IIterable<double?> source)
		{
			CheckParam.IsNotNull("source", source);

			double sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				double? num = it(ref ended);
				if (ended)
					break;
				if (num.HasValue)
					sum += num.Value;
			}

			if (count == 0)
				return null;
			return sum / (double)count;
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Double" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static double? Average<T>(this IIterable<T> source, Func<T, double?> selector)
		{
			return source.Select(selector).Average();
		}

		/// <summary>
		/// Returns the minimum value in a sequence of nullable 
		/// <see cref="System.Double" /> values.
		/// </summary>

		public static double? Min(
			 this IIterable<double?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null), null, (min, x) => min < x);
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the minimum nullable <see cref="System.Double" /> value.
		/// </summary>

		public static double? Min<T>(
			 this IIterable<T> source,
			 Func<T, double?> selector)
		{
			return source.Select(selector).Min();
		}

		/// <summary>
		/// Returns the maximum value in a sequence of nullable 
		/// <see cref="System.Double" /> values.
		/// </summary>

		public static double? Max(
			 this IIterable<double?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null),
				 null, (max, x) => x == null || (max != null && x.Value < max.Value));
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the maximum nullable <see cref="System.Double" /> value.
		/// </summary>

		public static double? Max<T>(
			 this IIterable<T> source,
			 Func<T, double?> selector)
		{
			return source.Select(selector).Max();
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Decimal" /> values.
		/// </summary>
		public static decimal Sum(this IIterable<decimal> source)
		{
			CheckParam.IsNotNull("source", source);

			decimal sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				decimal num = it(ref ended);
				if (ended)
					return sum;
				checked { sum += num; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of nullable <see cref="System.Decimal" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>

		public static decimal Sum<T>(
			 this IIterable<T> source,
			 Func<T, decimal> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Decimal" /> values.
		/// </summary>
		public static decimal Average(this IIterable<decimal> source)
		{
			CheckParam.IsNotNull("source", source);

			decimal sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				decimal num = it(ref ended);
				if (ended)
					break;
				checked { sum += num; }
			}

			if (count == 0)
				throw new InvalidOperationException();
			return sum / (decimal)count;
		}

		/// <summary>
		/// Computes the average of a sequence of nullable <see cref="System.Decimal" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static decimal Average<T>(this IIterable<T> source, Func<T, decimal> selector)
		{
			return source.Select(selector).Average();
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Decimal" /> values.
		/// </summary>
		public static decimal? Sum(this IIterable<decimal?> source)
		{
			CheckParam.IsNotNull("source", source);

			decimal sum = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; )
			{
				decimal? num = it(ref ended);
				if (ended)
					return sum;
				if (num.HasValue)
					checked { sum += num.Value; }
			}
		}

		/// <summary>
		/// Computes the sum of a sequence of <see cref="System.Decimal" /> 
		/// values that are obtained by invoking a transform function on 
		/// each element of the input sequence.
		/// </summary>

		public static decimal? Sum<T>(
			 this IIterable<T> source,
			 Func<T, decimal?> selector)
		{
			return source.Select(selector).Sum();
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Decimal" /> values.
		/// </summary>
		public static decimal? Average(this IIterable<decimal?> source)
		{
			CheckParam.IsNotNull("source", source);

			decimal sum = 0;
			long count = 0;
			bool ended = false;
			for (var it = source.GetIterator(); ; count++)
			{
				decimal? num = it(ref ended);
				if (ended)
					break;
				if (num.HasValue)
					checked { sum += num.Value; }
			}

			if (count == 0)
				return null;
			return sum / (decimal)count;
		}

		/// <summary>
		/// Computes the average of a sequence of <see cref="System.Decimal" /> values 
		/// that are obtained by invoking a transform function on each 
		/// element of the input sequence.
		/// </summary>
		public static decimal? Average<T>(this IIterable<T> source, Func<T, decimal?> selector)
		{
			return source.Select(selector).Average();
		}

		/// <summary>
		/// Returns the minimum value in a sequence of nullable 
		/// <see cref="System.Decimal" /> values.
		/// </summary>

		public static decimal? Min(
			 this IIterable<decimal?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null), null, (min, x) => min < x);
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the minimum nullable <see cref="System.Decimal" /> value.
		/// </summary>

		public static decimal? Min<T>(
			 this IIterable<T> source,
			 Func<T, decimal?> selector)
		{
			return source.Select(selector).Min();
		}

		/// <summary>
		/// Returns the maximum value in a sequence of nullable 
		/// <see cref="System.Decimal" /> values.
		/// </summary>

		public static decimal? Max(
			 this IIterable<decimal?> source)
		{
			CheckParam.IsNotNull("source", source);

			return MinMaxImpl(source.Where(x => x != null),
				 null, (max, x) => x == null || (max != null && x.Value < max.Value));
		}

		/// <summary>
		/// Invokes a transform function on each element of a sequence and 
		/// returns the maximum nullable <see cref="System.Decimal" /> value.
		/// </summary>

		public static decimal? Max<T>(
			 this IIterable<T> source,
			 Func<T, decimal?> selector)
		{
			return source.Select(selector).Max();
		}
	}
}

