namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Diagnostics;

	/// <summary>Internal implementation class. Shared code of all BList internal nodes.</summary>
	[Serializable]
	internal abstract class BListInner<K, T> : AListInnerBase<K, T>
	{
		#region Constructors and boilerplate

		protected BListInner(BListInner<K, T> frozen) : base(frozen)
		{
			_highestKey = InternalList.CopyToNewArray(frozen._highestKey);
		}
		public BListInner(AListNode<K, T> left, AListNode<K, T> right, int maxNodeSize)
			: base(left, right, maxNodeSize)
		{
			_highestKey = new K[_children.Length-1];
			_highestKey[0] = GetHighestKey(left);
		}
		protected BListInner(BListInner<K, T> original, int localIndex, int localCount, uint baseIndex, int maxNodeSize) 
			: base(original, localIndex, localCount, baseIndex, maxNodeSize)
		{
			_highestKey = new K[_children.Length - 1];
			for (int i = 0; i < localCount - 1; i++)
				_highestKey[i] = original._highestKey[localIndex + i];
		}

		protected BListInner(BListInner<K, T> original, uint index, uint count, AListBase<K,T> list) : base(original, index, count, list)
		{
			// This constructor is called by CopySection
			GetHighestKeys();
		}

		protected abstract K GetKey(T item);
		protected K GetHighestKey(AListNode<K, T> node) { return GetKey(node.GetLastItem()); }

		#endregion

		/// <summary>Stores the highest key that applies to the node with the same index.</summary>
		protected K[] _highestKey;

		public override long CountSizeInBytes(int sizeOfT, int sizeOfK) =>
			base.CountSizeInBytes(sizeOfT, sizeOfK) + 4 * IntPtr.Size + _highestKey.Length * sizeOfK;

		private void GetHighestKeys()
		{
			_highestKey = new K[_children.Length - 1];
			for (int i = 0; i < _childCount - 1; i++)
				_highestKey[i] = GetHighestKey(_children[i].Node);
		}

		/// <summary>Performs a binary search for a key.</summary>
		/// <remarks>If the key matches one of the values of _highestKey, this
		/// method returns the index of the lowest node that contains that key so 
		/// that non-add operations work correctly. If we were concerned ONLY with 
		/// plain Add operations, it would be acceptable to return index i+1 
		/// when key equals _highestKey[i] (and perhaps preferable, because it
		/// guarantees that _highestKey[i] won't have to be updated).</remarks>
		public int BinarySearchK(K key, Func<K,K,int> compare)
		{
			int keyCount = _childCount - 1;
			K[] highestKey = _highestKey;
			Debug.Assert(keyCount <= _highestKey.Length);
			Debug.Assert(_highestKey.Length < 256);

			int i = 1;
			if (keyCount >= 4)
			{
				i = 7;
				if (keyCount >= 16)
				{
					i = 31;
					if (keyCount >= 64)
					{
						i = 127;
						i += (i < keyCount && compare(highestKey[i], key) < 0 ? 64 : -64);
						i += (i < keyCount && compare(highestKey[i], key) < 0 ? 32 : -32);
					}
					i += (i < keyCount && compare(highestKey[i], key) < 0 ? 16 : -16);
					i += (i < keyCount && compare(highestKey[i], key) < 0 ? 8 : -8);
				}
				i += (i < keyCount && compare(highestKey[i], key) < 0 ? 4 : -4);
				i += (i < keyCount && compare(highestKey[i], key) < 0 ? 2 : -2);
			}
			i += (i < keyCount && compare(highestKey[i], key) < 0 ? 1 : -1);
			if (i < keyCount && compare(highestKey[i], key) < 0)
				++i;
			return i;
		}

		internal override int DoSingleOperation(ref AListSingleOperation<K, T> op, out AListNode<K, T> splitLeft, out AListNode<K, T> splitRight)
		{
			Debug.Assert(!IsFrozen || op.Mode == AListOperation.Retrieve);

			AssertValid();
			var tob = GetObserver(op.List);
			int i = BinarySearchK(op.Key, op.CompareKeys);
			if (op.Mode != AListOperation.Retrieve)
			{
				AutoClone(ref _children[i].Node, this, tob);
				if (op.Mode >= AListOperation.Add && _children[i].Node.IsFullLeaf)
				{
					if (!PrepareToInsert(i, tob))
					{	// Items have been shifted out of _children[i] (to the left or right)
						if (i < _highestKey.Length)
							_highestKey[i] = GetHighestKey(_children[i].Node);
						if (i > 0)
							_highestKey[i - 1] = GetHighestKey(_children[i - 1].Node);
						i = BinarySearchK(op.Key, op.CompareKeys);
					}
				}
			}
			AssertValid();
			op.BaseIndex += _children[i].Index;
			int sizeChange = _children[i].Node.DoSingleOperation(ref op, out splitLeft, out splitRight);
			if (sizeChange != 0)
				AdjustIndexesAfter(i, sizeChange);

			// Handle child split / undersized / highest key changed
			if (splitLeft != null)
			{
				if (splitRight != null)
					splitLeft = HandleChildSplit(i, splitLeft, ref splitRight, tob);
				else {
					// Node is undersized and/or highest key changed
					bool flagParent = false;
					
					if (op.AggregateChanged != 0)
					{
						if (i < _childCount - 1) {
							_highestKey[i] = op.AggregateKey;
							op.AggregateChanged = 0;
						} else {
							// Update highest key in parent node instead
							flagParent = true;
						}
					}

					if (splitLeft.IsUndersized)
						flagParent |= HandleUndersized(i, tob);

					if (flagParent)
						splitLeft = this;
					else
						splitLeft = null;
				}
			}
			AssertValid();
			return sizeChange;
		}

		protected sealed override bool HandleUndersizedOrAggregateChanged(int i, IAListTreeObserver<K, T> tob)
		{
			// Child i is undersized or its highest key changed. Update _highestKey if possible.
			bool returnAggChg = i >= _childCount-1;
			if (!returnAggChg && _children[i].Node.TotalCount > 0)
				_highestKey[i] = GetHighestKey(_children[i].Node);

			return base.HandleUndersizedOrAggregateChanged(i, tob) | returnAggChg;
		}

		protected sealed override bool HandleUndersized(int i, IAListTreeObserver<K, T> tob)
		{
			if (_highestKey == null) // it's null if called from base constructor
				GetHighestKeys();

			bool amUndersized = base.HandleUndersized(i, tob);

			// Child [i] either borrowed items from siblings or was removed.
			// Plus, this method may have been called by RemoveAt().
			// So, update _highestKey[i-1], [i] and [i+1] if applicable.
			int jStop = Math.Min(_childCount - 1, i + 2);
			for (int j = Math.Max(i - 1, 0); j < jStop; j++)
				_highestKey[j] = GetHighestKey(_children[j].Node);
			AssertValid();

			return amUndersized;
		}

		protected new AListInnerBase<K, T> HandleChildSplit(int i, AListNode<K, T> splitLeft, ref AListNode<K, T> splitRight, IAListTreeObserver<K, T> tob)
		{
			// Update _highestKey. base.HandleChildSplit will call LLInsert
			// which will update _highestKey for the newly inserted right child,
			// but we must manually update the left child at _highestKey[i], 
			// unless i == _childCount-1.
			if (i < _childCount-1)
				_highestKey[i] = GetHighestKey(splitLeft);

			return base.HandleChildSplit(i, splitLeft, ref splitRight, tob);
		}

		protected override void LLInsert(int i, AListNode<K, T> child, uint indexAdjustment)
		{
			if (_childCount > 0)
			{
				// Keep _highestKey up-to-date
				K highest;
				int i2 = i;
				if (i == _childCount) {
					i2--;
					highest = GetHighestKey(_children[_childCount - 1].Node);
				} else
					highest = GetHighestKey(child);
				
				_highestKey = InternalList.Insert(i2, highest, _highestKey, _childCount - 1);
			}

			base.LLInsert(i, child, indexAdjustment);
		}

		protected override bool LLDelete(int i, bool adjustIndexesAfterI)
		{
			if (_childCount > 1)
				InternalList.RemoveAt(Math.Min(i, _childCount - 2), _highestKey, _childCount - 1);

			return (i == _childCount - 1) | base.LLDelete(i, adjustIndexesAfterI);
		}

		[Conditional("DEBUG")] //"not valid... [on] an override method"
		public new void AssertValid()
		{
			base.AssertValid();
			Debug.Assert(_highestKey.Length + 1 >= _childCount);

			if (typeof(T).TypeHandle.Value == typeof(K).TypeHandle.Value)
			{
				// Verify values of _highestKey
				for (int i = 0; i < _childCount - 1; i++)
				{
					var key = _highestKey[i];
					var expected = _children[i].Node.GetLastItem();
					Debug.Assert((key == null && expected == null) || key.Equals(expected));
				}
			}
		}
	}

	internal class BListInner<T> : BListInner<T, T>
	{
		#region Constructors and boilerplate

		protected BListInner(BListInner<T, T> frozen) : base(frozen) {}
		public BListInner(AListNode<T, T> left, AListNode<T, T> right, int maxNodeSize) : base(left, right, maxNodeSize) { }
		protected BListInner(BListInner<T, T> original, int localIndex, int localCount, uint baseIndex, int maxNodeSize) : base(original, localIndex, localCount, baseIndex, maxNodeSize) { }
		protected BListInner(BListInner<T, T> original, uint index, uint count, AListBase<T, T> list) : base(original, index, count, list) { }

		public override AListNode<T, T> DetachedClone()
		{
			return new BListInner<T>(this);
		}
		public override AListNode<T, T> CopySection(uint index, uint count, AListBase<T, T> list)
		{
			Debug.Assert(count > 0 && count <= TotalCount);
			if (index == 0 && count >= TotalCount)
				return DetachedClone();

			return new BListInner<T>(this, index, count, list);
		}
		protected override AListInnerBase<T, T> SplitAt(int divAt, out AListNode<T, T> right)
		{
			right = new BListInner<T>(this, divAt, LocalCount-divAt, _children[divAt].Index, MaxNodeSize);
			return new BListInner<T>(this, 0, divAt, 0, MaxNodeSize);
		}

		#endregion

		protected override T GetKey(T item) { return item; }
	}

	internal class BDictionaryInner<K, V> : BListInner<K, KeyValuePair<K, V>>
	{
		#region Constructors and boilerplate

		protected BDictionaryInner(BDictionaryInner<K, V> frozen) : base(frozen) { }
		public BDictionaryInner(AListNode<K, KeyValuePair<K, V>> left, AListNode<K, KeyValuePair<K, V>> right, int maxNodeSize) : base(left, right, maxNodeSize) { }
		protected BDictionaryInner(BDictionaryInner<K, V> original, int localIndex, int localCount, uint baseIndex, int maxNodeSize) : base(original, localIndex, localCount, baseIndex, maxNodeSize) { }
		protected BDictionaryInner(BDictionaryInner<K, V> original, uint index, uint count, AListBase<K, KeyValuePair<K, V>> list) : base(original, index, count, list) { }

		public override AListNode<K, KeyValuePair<K, V>> DetachedClone()
		{
			return new BDictionaryInner<K, V>(this);
		}
		public override AListNode<K, KeyValuePair<K, V>> CopySection(uint index, uint count, AListBase<K, KeyValuePair<K, V>> list)
		{
			Debug.Assert(count > 0 && count <= TotalCount);
			if (index == 0 && count >= TotalCount)
				return DetachedClone();

			return new BDictionaryInner<K, V>(this, index, count, list);
		}
		protected override AListInnerBase<K, KeyValuePair<K, V>> SplitAt(int divAt, out AListNode<K, KeyValuePair<K, V>> right)
		{
			right = new BDictionaryInner<K, V>(this, divAt, LocalCount-divAt, _children[divAt].Index, MaxNodeSize);
			return new BDictionaryInner<K, V>(this, 0, divAt, 0, MaxNodeSize);
		}

		#endregion

		protected override K GetKey(KeyValuePair<K, V> item) { return item.Key; }
	}
}
