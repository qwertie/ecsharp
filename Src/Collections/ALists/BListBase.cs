namespace Loyc.Collections
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using Loyc.Collections.Impl;
	using Loyc.Essentials;
	using Loyc.Math;

	// Turns out that BList and BDictionary have very little code they can share. 
	// So there's not much point in having a common base.
	[Serializable]
	//[DebuggerTypeProxy(typeof(ListSourceDebugView<>)), DebuggerDisplay("Count = {Count}")]
	public abstract class BListBase<K, T> : AListBase<K, T>
	{
		#region Constructors

		protected BListBase() 
			: this(AListLeaf<K, T>.DefaultMaxNodeSize, AListInnerBase<K, T>.DefaultMaxNodeSize) { }
		protected BListBase(int maxLeafSize)
			: this(maxLeafSize, AListInnerBase<K, T>.DefaultMaxNodeSize) { }
		protected BListBase(int maxLeafSize, int maxInnerSize)
			: this(Comparer<K>.Default.Compare, maxLeafSize, maxInnerSize) { }
		protected BListBase(Comparison<K> compareKeys)
			: this(compareKeys, AListLeaf<K, T>.DefaultMaxNodeSize, AListInnerBase<K, T>.DefaultMaxNodeSize) { }
		protected BListBase(Comparison<K> compareKeys, int maxLeafSize)
			: this(compareKeys, maxLeafSize, AListInnerBase<K, T>.DefaultMaxNodeSize) { }
		
		protected BListBase(Comparison<K> compareKeys, int maxLeafSize, int maxInnerSize)
			: base(maxLeafSize, maxInnerSize) { _compareKeys = compareKeys; }
		protected BListBase(BListBase<K, T> items, bool keepListChangingHandlers) 
			: base(items, keepListChangingHandlers) { _compareKeys = items._compareKeys; }
		protected BListBase(BListBase<K, T> original, AListNode<K, T> section) 
			: base(original, section) { _compareKeys = original._compareKeys; }

		#endregion

		protected Comparison<K> _compareKeys;

		protected override AListLeaf<K, T> NewRootLeaf()
		{
			return new BListLeaf<K, T>(_maxLeafSize);
		}

		protected abstract override AListInnerBase<K, T> SplitRoot(AListNode<K, T> left, AListNode<K, T> right);
	}

}
