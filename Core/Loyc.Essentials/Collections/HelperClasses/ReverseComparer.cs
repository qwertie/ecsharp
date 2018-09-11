using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>Reverses the order used by an IComparer object.</summary>
	public struct ReverseComparer<T> : IComparer<T>
	{
		IComparer<T> _comparer;
		public ReverseComparer(IComparer<T> comparer)
		{
			_comparer = comparer;
		}
		public int Compare(T x, T y)
		{
			return _comparer.Compare(y, x);
		}
	}

	/// <summary>Reverses the order used by an IComparer object.</summary>
	public struct ReverseComparer<T, TComparer> : IComparer<T> where TComparer : IComparer<T>
	{
		TComparer _comparer;
		public ReverseComparer(TComparer comparer)
		{
			_comparer = comparer;
		}
		public int Compare(T x, T y)
		{
			return _comparer.Compare(y, x);
		}
	}
}
