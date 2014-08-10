using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Loyc.Collections
{
	/// <summary>
	/// A simple wrapper around AList that includes an <see cref="AListIndexer{K,T}"/> 
	/// that can be used to find items relatively quickly in a large list. When an
	/// index is built and the list is large, it accelerates IndexOf(item), 
	/// Contains(item) and Remove(item).
	/// </summary>
	/// <remarks>
	/// The <see cref="IndexOf"/>, <see cref="Remove"/> and <see cref="Contains"/>
	/// methods are accelerated by the indexer, but please note that the indexer
	/// is expensive in terms of memory usage and CPU time. In total, once the 
	/// index has been built, IndexedAList typically uses about three times as
	/// much memory as a plain <see cref="AList{T}"/>. Moreover, changing the list 
	/// takes at least twice as much time, since the indexer must be updated to 
	/// reflect every change.
	/// <para/>
	/// An IndexedAList is indexed by default, but if necessary the index can be 
	/// disabled in the constructor or by settings the <see cref="IsIndexed"/> 
	/// property to false.
	/// </remarks>
	[Serializable]
	public class IndexedAList<T> : AList<T>
	{
		public IndexedAList() : this(true) { }
		public IndexedAList(bool createIndexNow) { if (createIndexNow) CreateIndex(); }
		public IndexedAList(IEnumerable<T> items) : base(items) { CreateIndex(); }
		public IndexedAList(IListSource<T> items) : base(items) { CreateIndex(); }
		public IndexedAList(int maxLeafSize) : base(maxLeafSize) { CreateIndex(); }
		public IndexedAList(int maxLeafSize, int maxInnerSize) : base(maxLeafSize, maxInnerSize) { CreateIndex(); }
		public IndexedAList(int maxLeafSize, int maxInnerSize, bool createIndexNow) { if (createIndexNow) CreateIndex(); }
		public IndexedAList(AList<T> items, bool keepListChangingHandlers) : this(items, keepListChangingHandlers, true) { }
		public IndexedAList(AList<T> items, bool keepListChangingHandlers, bool createIndexNow) : base(items, keepListChangingHandlers) 
		{
			if (createIndexNow)
				CreateIndex();
		}

		AListIndexer<int, T> _indexer;

		protected void CreateIndex()
		{
			Debug.Assert(_indexer == null);
			_indexer = new AListIndexer<int, T>();
			AddObserver(_indexer);
		}

		/// <summary>Finds an index of an item in the list.</summary>
		/// <param name="item">An item for which to search.</param>
		/// <returns>An index of the item. If the list contains duplicates of the 
		/// item, this method does not necessarily return the lowest index of the 
		/// item.</returns>
		/// <remarks>
		/// If IsIndexed is false, an index is created unless the list is short 
		/// (specifically, an index is created if the root node is not a leaf.)
		/// </remarks>
		public override int IndexOf(T item)
		{
			if (_indexer == null)
			{
				// Create the index, unless the root is a leaf.
				var leaf = _root as AListLeaf<int, T>;
				if (leaf != null)
					return leaf.IndexOf(item, 0);
				else
					CreateIndex();
			}
			return _indexer.IndexOfAny(item);
		}

		/// <summary>Returns a list of indexes at which the specified item can be found.</summary>
		/// <param name="item">Item to find in the list</param>
		/// <param name="sorted">Whether to sort the list of indexes before returning it.</param>
		/// <remarks>If IsIndexed is false, an index is created.</remarks>
		public List<int> IndexesOf(T item, bool sorted)
		{
			if (_indexer == null) CreateIndex();
			var list = _indexer.IndexesOf(item);
			if (sorted && list.Count > 1)
				list.Sort();
			return list;
		}

		/// <summary>Indicates whether the AList is indexed.</summary>
		/// <remarks>
		/// You can set this property to false to discard the index if it has been 
		/// built, or set it to true to create a new index if it has not yet been
		/// built (which takes O(N log N) where N is the <see cref="Count"/> of 
		/// this list).
		/// </remarks>
		public bool IsIndexed
		{
			get { return _indexer != null; }
			set {
				if (value) {
					if (_indexer == null)
						CreateIndex();
				} else {
					if (_indexer != null) {
						bool r = RemoveObserver(_indexer);
						Debug.Assert(r);
						_indexer = null;
					}
				}
			}
		}

		/// <inheritdoc cref="AListIndexer{K,T}.VerifyCorrectness"/>
		public void VerifyCorrectness()
		{
			if (_indexer != null)
				_indexer.VerifyCorrectness();
		}
	}
}
