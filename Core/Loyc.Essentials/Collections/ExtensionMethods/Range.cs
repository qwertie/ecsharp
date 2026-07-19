using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Loyc.Collections
{
	/// <summary>Extension/helper methods for ranges.</summary>
	public static partial class RangeExt
	{
		/// <summary>Advances by the specified number of elements.</summary>
		/// <param name="count">Number of items to remove from the beginning of the
		/// range. If count is higher than the number of items in the range, no
		/// exception is thrown but the return value will be less than this value.</param>
		/// <returns>Returns the number of items skipped.</returns>
		public static int Skip<R, T>(ref R range, int count) where R : IFRange<T>
		{
			bool fail;
			for (int i = 0; i < count; i++) {
				range.PopFirst(out fail);
				if (fail) return i;
			}
			return count;
		}
		public static int DropLast<R, T>(ref R range, int count) where R : IBRange<T>
		{
			bool fail;
			for (int i = 0; i < count; i++)
			{
				range.PopLast(out fail);
				if (fail) return i;
			}
			return count;
		}
		public static T PopFirst<R,T>(ref R range) where R:IFRange<T>
		{
			T? next = range.PopFirst(out bool fail);
			return fail ? throw new EmptySequenceException() : next!;
		}
		public static Maybe<T> TryPopFirst<R, T>(ref R range) where R : IFRange<T>
		{
			T? next = range.PopFirst(out bool fail);
			return fail ? default(Maybe<T>) : next!;
		}
		public static bool TryPopFirst<R, T>(ref R range, [MaybeNullWhen(false)] out T item) where R : IFRange<T>
		{
			item = range.PopFirst(out bool fail);
			return !fail;
		}
		public static T PopLast<R, T>(ref R range) where R : IBRange<T>
		{
			T? next = range.PopLast(out bool fail);
			return fail ? throw new EmptySequenceException() : next!;
		}
		public static Maybe<T> TryPopLast<R, T>(ref R range) where R : IBRange<T>
		{
			T? next = range.PopLast(out bool fail);
			return fail ? default(Maybe<T>) : next!;
		}
		public static bool TryPopLast<R, T>(ref R range, [MaybeNullWhen(false)] out T item) where R : IBRange<T>
		{
			item = range.PopLast(out bool fail);
			return !fail;
		}

		public static bool Contains<R,T>(this R range, IRangeEx<R,T> other) where R : IRangeEx<R,T>, ICloneable<R>
		{
			int r0 = range.SliceStart, o0 = other.SliceStart;
			return r0 <= o0 && r0 + range.Count >= o0 + other.Count
				&& object.Equals(range.InnerList, other.InnerList);
		}
		public static bool Overlaps<R, T>(this R range, IRangeEx<R, T> other) where R : IRangeEx<R,T>, ICloneable<R>
		{
			int r0 = range.SliceStart, r1 = r0 + range.Count;
			int o0 = other.SliceStart, o1 = o0 + other.Count;
			return r1 > o0 && o1 > r0
				&& object.Equals(range.InnerList, other.InnerList);
		}
	}
}
