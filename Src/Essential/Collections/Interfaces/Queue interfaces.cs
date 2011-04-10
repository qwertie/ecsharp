using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Runtime
{
	/// <summary>Represents a collection that accepts a sequence of items.</summary>
	public interface IPush<T>
	{
		void Push(T item);
	}
	
	/// <summary>Represents a collection that produces a sequence of items, and can
	/// return the next item without popping it (the Peek operation).</summary>
	/// <remarks>Push/Pop methods that throw an exception on failure, and
	/// TryPush/TryPop methods that don't require a "ref" argument, are
	/// available as extension methods.</remarks>
	public interface IPop<T>
	{
		T TryPop(ref bool isEmpty);
		T TryPeek(ref bool isEmpty);
		bool IsEmpty { get; }
	}
	public static partial class CollectionInterfaces
	{
		public static T Pop<T>(this IPop<T> c)
		{
			bool isEmpty = false;
			T next = c.TryPop(ref isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T Peek<T>(this IPop<T> c)
		{
			bool isEmpty = false;
			T next = c.TryPeek(ref isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static bool TryPop<T>(this IPop<T> c, out T value)
		{
			bool isEmpty = false;
			value = c.TryPop(ref isEmpty);
			return !isEmpty;
		}
		public static bool TryPeek<T>(this IPop<T> c, out T value)
		{
			bool isEmpty = false;
			value = c.TryPeek(ref isEmpty);
			return !isEmpty;
		}
		public static T TryPop<T>(this IPop<T> c)
		{
			bool isEmpty = false;
			return c.TryPop(ref isEmpty);
		}
		public static T TryPeek<T>(this IPop<T> c)
		{
			bool isEmpty = false;
			return c.TryPeek(ref isEmpty);
		}
		public static T TryPop<T>(this IPop<T> c, T defaultValue)
		{
			bool isEmpty = false;
			T value = c.TryPop(ref isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPeek<T>(this IPop<T> c, T defaultValue)
		{
			bool isEmpty = false;
			T value = c.TryPeek(ref isEmpty);
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
	public interface IDeque<T> : ICount
	{
		void PushFirst(T item);
		void PushLast(T item);
		T TryPopFirst(ref bool isEmpty);
		T TryPeekFirst(ref bool isEmpty);
		T TryPopLast(ref bool isEmpty);
		T TryPeekLast(ref bool isEmpty);

		T First { get; set; }
		T Last { get; set; }
		bool IsEmpty { get; }
	}

	public static partial class CollectionInterfaces
	{
		public static T PopFirst<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			T next = c.TryPopFirst(ref isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T PopLast<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			T next = c.TryPopLast(ref isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T PeekFirst<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			T next = c.TryPeekFirst(ref isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static T PeekLast<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			T next = c.TryPeekLast(ref isEmpty);
			if (isEmpty)
				throw new InvalidOperationException(string.Format("The {0} is empty", c.GetType().Name));
			return next;
		}
		public static bool TryPopFirst<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty = false;
			value = c.TryPopFirst(ref isEmpty);
			return !isEmpty;
		}
		public static bool TryPoLastp<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty = false;
			value = c.TryPopLast(ref isEmpty);
			return !isEmpty;
		}
		public static bool TryPeekFirst<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty = false;
			value = c.TryPeekFirst(ref isEmpty);
			return !isEmpty;
		}
		public static bool TryPeekLast<T>(this IDeque<T> c, out T value)
		{
			bool isEmpty = false;
			value = c.TryPeekLast(ref isEmpty);
			return !isEmpty;
		}
		public static T TryPopFirst<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			return c.TryPopFirst(ref isEmpty);
		}
		public static T TryPopLast<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			return c.TryPopLast(ref isEmpty);
		}
		public static T TryPeekFirst<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			return c.TryPeekFirst(ref isEmpty);
		}
		public static T TryPeekLast<T>(this IDeque<T> c)
		{
			bool isEmpty = false;
			return c.TryPeekLast(ref isEmpty);
		}
		public static T TryPopFirst<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty = false;
			T value = c.TryPopFirst(ref isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPopLast<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty = false;
			T value = c.TryPopLast(ref isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPeekFirst<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty = false;
			T value = c.TryPeekFirst(ref isEmpty);
			return isEmpty ? defaultValue : value;
		}
		public static T TryPeekLast<T>(this IDeque<T> c, T defaultValue)
		{
			bool isEmpty = false;
			T value = c.TryPeekLast(ref isEmpty);
			return isEmpty ? defaultValue : value;
		}
	}
}
