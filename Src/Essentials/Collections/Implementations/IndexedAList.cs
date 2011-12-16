using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.Collections.Linq;

namespace Loyc.Collections
{
	/*class IndexedAList<T> : AList<T>
	{
		public override IIterable<int> IndexesOf(T item, int minIndex, int maxIndex)
		{
			if (_observer != null)
				return new IteratorFactory<uint>(() =>
					((AListIndexerBase<T>)_observer).IndexesOf(item, (uint)minIndex, (uint)maxIndex).AsIterator())
					.Select(ui => (int)ui);
			else
				return base.IndexesOf(item, minIndex, maxIndex);
		}
		public override int IndexOf(T item)
		{
			if (_observer != null)
			{
				CheckCounts();
				return (int)((AListIndexerBase<T>)_observer).IndexOf(item);
			}
			return base.IndexOf(item);
		}
	}*/
}
