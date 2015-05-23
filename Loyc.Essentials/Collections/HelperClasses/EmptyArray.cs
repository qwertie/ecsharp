using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary><see cref="EmptyArray{T}.Value"/> lets you avoid allocating an empty array on the heap.</summary>
	public class EmptyArray<T>
	{
		public static readonly T[] Value = new T[0];
	}
}
