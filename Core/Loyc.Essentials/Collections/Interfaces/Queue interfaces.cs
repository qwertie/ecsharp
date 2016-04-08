using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Represents a collection that accepts a sequence of items.</summary>
	#if DotNet4 || DotNet4_5
	public interface IPush<in T>
	#else
	public interface IPush<T>
	#endif
	{
		void Push(T item);
	}
	
	/// <summary>Represents a collection that produces a sequence of items, and can
	/// return the next item without popping it (the Peek operation).</summary>
	/// <remarks>Push/Pop methods that throw an exception on failure, and
	/// TryPush/TryPop methods that don't require a "ref" argument, are
	/// available as extension methods.</remarks>
	#if DotNet4 || DotNet4_5
	public interface IPop<out T>
	#else
	public interface IPop<T>
	#endif
	{
		T TryPop(out bool isEmpty);
		T TryPeek(out bool isEmpty);
		bool IsEmpty { get; }
	}
	
	public static partial class LCInterfaces
	{
		public static T Pop<T>(this IPop<T> c)
		{
			bool isEmpty;
			T next = c.TryPop(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next;
		}
		public static T Peek<T>(this IPop<T> c)
		{
			bool isEmpty;
			T next = c.TryPeek(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next;
		}
		public static bool TryPop<T>(this IPop<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPop(out isEmpty);
			return !isEmpty;
		}
		public static bool TryPeek<T>(this IPop<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPeek(out isEmpty);
			return !isEmpty;
		}
		public static T TryPop<T>(this IPop<T> c)
		{
			bool isEmpty;
			return c.TryPop(out isEmpty);
		}
		public static T TryPeek<T>(this IPop<T> c)
		{
			bool isEmpty;
			return c.TryPeek(out isEmpty);
		}
		public static T TryPop<T>(this IPop<T> c, T defaultValue)
		{
			bool isEmpty;
			T value = c.TryPop(out isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPeek<T>(this IPop<T> c, T defaultValue)
		{
			bool isEmpty;
			T value = c.TryPeek(out isEmpty);
			return isEmpty ? defaultValue : value;
		}
	}
	
	/// <summary>Represents a FIFO (first-in-first-out) queue.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IQueue<T> : IPush<T>, IPop<T>, ICount
	{
	}
	
	/// <summary>Represents a LIFO (last-in-first-out) stack.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IStack<T> : IPush<T>, IPop<T>, ICount
	{
	}
	
	/// <summary>Represents a double-ended queue that allows items to be added or
	/// removed at the beginning or end.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IDeque<T> : IIsEmpty, ICount
	{
		void PushFirst(T item);
		void PushLast(T item);
		Maybe<T> TryPopFirst();
		Maybe<T> TryPeekFirst();
		Maybe<T> TryPopLast();
		Maybe<T> TryPeekLast();

		/// <summary>Gets the first item in the deque.</summary>
		/// <exception cref="InvalidOperationException"></exception>
		T First { get; set; }
		T Last { get; set; }
	}

	public static partial class LCInterfaces
	{
		public static T PopFirst<T>(this IDeque<T> c)
		{
			var next = c.TryPopFirst();
			if (!next.HasValue)
				throw new InvalidOperationException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next.Value;
		}
		public static T PopLast<T>(this IDeque<T> c)
		{
			var next = c.TryPopLast();
			if (!next.HasValue)
				throw new InvalidOperationException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next.Value;
		}
		public static T PeekFirst<T>(this IDeque<T> c)
		{
			var next = c.TryPeekFirst();
			if (!next.HasValue)
				throw new InvalidOperationException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next.Value;
		}
		public static T PeekLast<T>(this IDeque<T> c)
		{
			var next = c.TryPeekLast();
			if (!next.HasValue)
				throw new InvalidOperationException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next.Value;
		}
	}
}
