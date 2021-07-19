using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A variety of empty lists.</summary>
	public class Empty<T>
	{
		public static readonly T[] Array = System.Array.Empty<T>();
		public static readonly IListAndListSource<T> List = new Repeated<T>(default(T)!, 0);
		public static readonly List<T>.Enumerator Enumerator = new List<T>().GetEnumerator();
		public static readonly InternalList.Scanner<T> Scanner = new InternalList.Scanner<T>();
	}
}
