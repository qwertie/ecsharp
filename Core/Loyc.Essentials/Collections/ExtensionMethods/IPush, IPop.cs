using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Collections
{
	public static partial class IPopExt
	{
		public static T Pop<T>(this IPop<T> c)
		{
			bool isEmpty;
			T next = c.TryPop(out isEmpty);
			if (isEmpty)
				throw new EmptySequenceException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next;
		}
		public static T Peek<T>(this IPop<T> c)
		{
			bool isEmpty;
			T next = c.TryPeek(out isEmpty);
			if (isEmpty)
				throw new EmptySequenceException("The {0} is empty".Localized(MemoizedTypeName.Get(c.GetType())));
			return next;
		}
		[Obsolete("Please call a different overload. The return type on this method should be Maybe<T> in the future.")]
		public static T TryPop<T>(this IPop<T> c)
		{
			bool isEmpty;
			return c.TryPop(out isEmpty);
		}
		[Obsolete("Please call a different overload. The return type on this method should be Maybe<T> in the future.")]
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
}
