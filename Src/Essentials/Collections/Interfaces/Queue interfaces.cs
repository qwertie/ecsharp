using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Represents a collection that accepts a sequence of items.</summary>
	#if DotNet4
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
	#if DotNet4
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
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T Peek<T>(this IPop<T> c)
		{
			bool isEmpty;
			T next = c.TryPeek(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
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
		int Count { get; }
	}
	
	/// <summary>Represents a double-ended queue that allows items to be added or
	/// removed at the beginning or end.</summary>
	/// <typeparam name="T">Type of each element</typeparam>
	public interface IDeque<T>: ICount
	{
		int Count { get; }

		void PushFirst(T item);
		void PushLast(T item);
		T TryPopFirst(out bool isEmpty);
		T TryPeekFirst(out bool isEmpty);
		T TryPopLast(out bool isEmpty);
		T TryPeekLast(out bool isEmpty);

		/// <summary>Gets the first item in the deque.</summary>
		/// <exception cref="InvalidOperationException"></exception>
		T First { get; set; }
		T Last { get; set; }
		bool IsEmpty { get; }
	}

	public static partial class LCInterfaces
	{
		public static T PopFirst<T>(this IDeque<T> c)
		{
			bool isEmpty;
			T next = c.TryPopFirst(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T PopLast<T>(this IDeque<T> c)
		{
			bool isEmpty;
			T next = c.TryPopLast(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T PeekFirst<T>(this IDeque<T> c)
		{
			bool isEmpty;
			T next = c.TryPeekFirst(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T PeekLast<T>(this IDeque<T> c)
		{
			bool isEmpty;
			T next = c.TryPeekLast(out isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static bool TryPopFirst<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPopFirst(out isEmpty);
			return !isEmpty;
		}
		public static bool TryPopLast<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPopLast(out isEmpty);
			return !isEmpty;
		}
		public static bool TryPeekFirst<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPeekFirst(out isEmpty);
			return !isEmpty;
		}
		public static bool TryPeekLast<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty;
			value = c.TryPeekLast(out isEmpty);
			return !isEmpty;
		}
		public static T TryPopFirst<T>(this IDeque<T> c)
		{
			bool isEmpty;
			return c.TryPopFirst(out isEmpty);
		}
		public static T TryPopLast<T>(this IDeque<T> c)
		{
			bool isEmpty;
			return c.TryPopLast(out isEmpty);
		}
		public static T TryPeekFirst<T>(this IDeque<T> c)
		{
			bool isEmpty;
			return c.TryPeekFirst(out isEmpty);
		}
		public static T TryPeekLast<T>(this IDeque<T> c)
		{
			bool isEmpty;
			return c.TryPeekLast(out isEmpty);
		}
		public static T TryPopFirst<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty;
			T value = c.TryPopFirst(out isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPopLast<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty;
			T value = c.TryPopLast(out isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPeekFirst<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty;
			T value = c.TryPeekFirst(out isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPeekLast<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty;
			T value = c.TryPeekLast(out isEmpty);
			return isEmpty ? defaultValue : value;
		}
	}
}
