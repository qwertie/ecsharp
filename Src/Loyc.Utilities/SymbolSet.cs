using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Utilities;
using Loyc.Collections.Impl;

namespace Loyc
{
    /// <summary>
    /// A set of symbols.
    /// </summary><remarks>
	/// Stored with a small bloom filter, so that a membership test tends to be 
	/// fast if the item tested is not in the set. Thus, it is useful when the 
	/// items to be tested are most often not in the set.
	/// <para/>
    /// This class is designed for small sets of perhaps 5 to 20 items. Its
    /// efficiency decreases as its size increases, and is O(N) in in the limit for
    /// all operations. TODO: implement sorting at larger sizes to improve query
    /// performance of large sets. At small sizes, though (say, Count less than 10)
    /// I don't think binary search is any faster.
    /// </remarks>
	public class SymbolSet : ICollection<Symbol>
	{
		protected InternalList<Symbol> _list = InternalList<Symbol>.Empty;
		protected BloomFilterM64K2 _bloom;

		public SymbolSet() { }
		public SymbolSet(int capacity) { _list = new InternalList<Symbol>(capacity); }
		public SymbolSet(ICollection<Symbol> copy) { AddRange(copy); }
		public SymbolSet(params Symbol[] list)
		{
			for (int i = 0; i < list.Length; i++) 
				Add(list[i]);
		}
		public SymbolSet(params ICollection<Symbol>[] sets)
		{
			for (int i = 0; i < sets.Length; i++)
				AddRange(sets[i]);
		}

		/// <summary>
		/// Returns whether the bloom filter indicates that this set may contain
		/// the specified item. If this function returns false, the item is
		/// definitely not in the set.
		/// </summary>
		public bool MayContain(Symbol item)
		{
			return _bloom.MayContain(item);
		}

		private void RebuildBloom()
		{
			_bloom.Clear();
			for (int i = 0; i < _list.Count; i++)
				_bloom.Add(_list[i]);
		}

		public void AddRange(ICollection<Symbol> copy)
		{
			foreach (Symbol s in copy)
				Add(s);
		}

		#region ICollection<Symbol>

		public void Add(Symbol item)
		{
			if (!Contains(item)) {
				_bloom.Add(item);
				_list.Add(item);
			}
		}

		public void Clear()
		{
			_bloom.Clear();
			_list.Clear();
		}

		public bool Contains(Symbol item)
		{
			if (!_bloom.MayContain(item))
				return false;
			Debug.Assert(_list.Count != 0); // an empty bloom has no false positives
			return _list.Contains(item);
		}

		public void CopyTo(Symbol[] array, int arrayIndex)
		{
			_list.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _list.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

        public bool IsEmpty
        {
            get { return _bloom.IsEmpty; }
        }

		public bool Remove(Symbol item)
		{
			if (!_bloom.MayContain(item))
				return false;
			if (!_list.Remove(item))
				return false;
			RebuildBloom();
			return true;
		}

		#endregion

		#region IEnumerable<Symbol> Members

		public IEnumerator<Symbol> GetEnumerator()
		{
			return _list.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
}
