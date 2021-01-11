using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Encapsulates a Push(T) method.</summary>
	public interface IHasPush<in T>
	{
		void Push(T item);
	}

	[Obsolete("This was renamed to IHasPush for consistency with other interfaces")]
	public interface IPush<in T> : IHasPush<T> { }

	/// <summary>Encapsulates a First property (for sequences).</summary>
	public interface IHasFirst<out T> : IIsEmpty
	{
		/// <summary>Gets the first item in the deque.</summary>
		/// <exception cref="EmptySequenceException">The collection is empty.</exception>
		T First { get; }
	}

	/// <summary>Encapsulates a Last property (for sequences).</summary>
	public interface IHasLast<out T> : IIsEmpty
	{
		/// <summary>Gets the first item in the collection.</summary>
		/// <exception cref="EmptySequenceException">The collection is empty.</exception>
		T Last { get; }
	}

	/// <summary>Encapsulates a mutable First property.</summary>
	public interface IHasMFirst<T> : IHasFirst<T>
	{
		new T First { get; set; }
	}

	/// <summary>Encapsulates a mutable Last property.</summary>
	public interface IHasMLast<T> : IHasLast<T>
	{
		new T Last { get; set; }
	}

	/// <summary>Represents a collection that produces a sequence of items, and can
	/// return the next item without popping it (the Peek operation).</summary>
	/// <remarks>Push/Pop methods that throw an exception on failure, and
	/// TryPush/TryPop methods that don't require an "out" argument, are
	/// available as extension methods.</remarks>
	public interface ITryPop<out T> : IIsEmpty
	{
		T TryPop(out bool isEmpty);
		T TryPeek(out bool isEmpty);
	}
	[Obsolete("This was renamed to ITryPop")]
	public interface IPop<out T> : ITryPop<T> { }

	public static partial class LCInterfaces
	{
		public static bool TryPop<T>(this ITryPop<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPop(out isEmpty);
			return !isEmpty;
		}
		public static bool TryPeek<T>(this ITryPop<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPeek(out isEmpty);
			return !isEmpty;
		}
	}

	/// <summary>Represents a FIFO (first-in-first-out) queue (or a priority queue 
	/// if <see cref="IPriorityQueue{ThisAssembly}"/> is also implemented).</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IQueue<T> : IHasPush<T>, ITryPop<T>, ICount
	{
	}

	/// <summary>Represents a LIFO (last-in-first-out) stack.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IStack<T> : IHasPush<T>, ITryPop<T>, ICount
	{
	}

	/// <summary>Represents a priority queue, in which Pop() always returns the largest or smallest item.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IPriorityQueue<T> : IQueue<T>
	{
	}

	/// <summary>Represents a double-ended queue that allows items to be added or
	/// removed at the beginning or end.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IDeque<T> : IHasMFirst<T>, IHasMLast<T>, ICount
	{
		void PushFirst(T item);
		void PushLast(T item);
		Maybe<T> TryPopFirst();
		Maybe<T> TryPeekFirst();
		Maybe<T> TryPopLast();
		Maybe<T> TryPeekLast();
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
