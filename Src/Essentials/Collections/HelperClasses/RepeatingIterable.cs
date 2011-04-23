/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:47 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace Loyc.Collections
{
	/// <summary>A sequence that simply repeats the same value a specified number 
	/// of times, returned from <see cref="LCExt.Repeat{T}"/>.</summary>
	public class RepeatedValueList<T> : ListSourceBase<T>
	{
		int _count;
		T _value;

		public RepeatedValueList(T value, int count)
		{
			_count = count;
			_value = value;
		}
		public override Iterator<T> GetIterator()
		{
			return Iterator.Repeat(_value, _count);
		}

		public override T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_count)
				return _value;
			fail = true;
			return default(T);
		}

		public override int Count
		{
			get { return _count; }
		}
	}
}
