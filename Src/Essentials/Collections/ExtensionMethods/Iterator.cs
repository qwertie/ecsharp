/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:51 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>Helper methods for creating simple iterators.</summary>
	public static class Iterator
	{
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

		public static Iterator<TResult> UpCast<T, TResult>(this Iterator<T> it) where T : class, TResult
		{
			#if DotNet4
			return it;
			#else
			return delegate(ref bool ended) { return it(ref ended); };
			#endif
		}

		#endregion
	}
}
