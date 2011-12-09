namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Diagnostics;

	[Serializable]
	public class BListInner<K, T> : AListInnerBase<K, T>
	{
		#region Constructors and boilerplate

		protected BListInner(BListInner<K, T> frozen) : base(frozen)
		{
			_aggregateKey = InternalList.CopyToNewArray(frozen._aggregateKey);
		}
		public BListInner(AListNode<K, T> left, AListNode<K, T> right, Func<T,K> extractKey, int maxNodeSize) 
			: base(left, right, maxNodeSize)
		{
			_aggregateKey = new K[_children.Length-1];
			_aggregateKey[0] = extractKey(left.GetLastItem());
		}
		protected BListInner(BListInner<K, T> original, int localIndex, int localCount, uint baseIndex, int maxNodeSize) 
			: base(original, localIndex, localCount, baseIndex, maxNodeSize)
		{
			_aggregateKey = new K[_children.Length - 1];
			for (int i = 0; i < localCount-1; i++)
				_aggregateKey[i] = original._aggregateKey[localIndex + i];
		}
		protected BListInner(BListInner<K, T> original, uint index, uint count, AListBase<K,T> list) : base(original, index, count, list)
		{
			_aggregateKey = new K[_children.Length - 1];
			for (int i = 0; i < LocalCount-1; i++)
				_aggregateKey[i] = list.GetKey(_children[i].Node.GetLastItem());
		}

		public override AListNode<K, T> DetachedClone()
		{
			// TODO: Wait, why do we have to freeze this node? Isn't it enough to freeze the children?
			Freeze();
			return new BListInner<K, T>(this);
		}
		public override AListNode<K, T> CopySection(uint index, uint count, AListBase<K,T> list)
		{
			Debug.Assert(count > 0 && count <= TotalCount);
			if (index == 0 && count >= TotalCount)
				return DetachedClone();

			return new BListInner<K, T>(this, index, count, list);
		}
		protected override AListInnerBase<K, T> SplitAt(int divAt, out AListNode<K, T> right)
		{
			right = new BListInner<K, T>(this, divAt, LocalCount, _children[divAt].Index, MaxNodeSize);
			return new BListInner<K, T>(this, 0, divAt, 0, MaxNodeSize);
		}

		#endregion

		/// <summary>Stores the highest key that applies to the node with the same index.</summary>
		protected K[] _aggregateKey;

		/// <summary>Performs a binary search for a key.</summary>
		/// <remarks>If the key matches one of the values of _aggregateKey, this
		/// method returns the index of the lowest node that contains that key so 
		/// that non-add operations work correctly. If we were concerned ONLY with 
		/// plain Add operations, it would be acceptable to return index i+1 
		/// when key equals _aggregateKey[i] (and perhaps preferable, because it
		/// guarantees that _aggregateKey[i] won't have to be updated).</remarks>
		public int BinarySearchK(K key, Func<K,K,int> compare)
		{
			int keyCount = _childCount - 1;
			K[] highestKey = _aggregateKey;
			Debug.Assert(keyCount <= _aggregateKey.Length);
			Debug.Assert(_aggregateKey.Length < 256);

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

		public override int DoSingleOperation(ref AListSingleOperation<K, T> op, out AListNode<K, T> splitLeft, out AListNode<K, T> splitRight)
		{
			Debug.Assert(!IsFrozen || op.Mode == AListOperation.Retrieve);

			int i = BinarySearchK(op.Key, op.CompareKeys);
			var nob = GetObserver(op.List);
			if (op.Mode != AListOperation.Retrieve)
			{
				if (op.Mode >= AListOperation.Add)
					PrepareToInsert(i, nob);
				else
					AutoClone(ref _children[i].Node, this, nob);
			}
			AssertValid();

			int sizeChange = _children[i].Node.DoSingleOperation(ref op, out splitLeft, out splitRight);
			if (sizeChange != 0)
				AdjustIndexesAfter(i, sizeChange);

			// Handle child split / undersized / highest key changed
			if (splitLeft != null)
			{
				if (splitRight != null)
					splitLeft = HandleChildSplit(i, splitLeft, ref splitRight, nob);
				else {
					// Node is undersized and/or highest key changed
					bool flagParent = false;
					
					if (op.AggregateChanged)
					{
						if (i < _aggregateKey.Length) {
							_aggregateKey[i] = op.AggregateKey;
							op.AggregateChanged = false;
						} else {
							// Update highest key in parent node instead
							flagParent = true;
						}
					}
					
					if (splitLeft.IsUndersized)
						flagParent |= HandleUndersized(i, nob);

					if (flagParent)
						splitLeft = this;
					else
						splitLeft = null;
				}
			}
			return sizeChange;
		}

		// Return true if node changed
		/*public override bool DoStructuredOperation(ref StructuredAListOperation<K, T> op)
		{
			Debug.Assert(!IsFrozen);
			int i;
			if (op.SearchMode == AListSearchMode.Linear) {
				int min_i = int.MaxValue;
				int max_i = int.MinValue;
				for (i = 0; i < _aggregateKey.Length; i++) {
					int c = op.CompareKeys(op.SearchKey, _aggregateKey[i]);
					if (c == 0)
					{
						max_i = i;
						if (min_i > i)
							min_i = i;
						_children[i].Node.DoStructuredOperation(ref op);
					}
					if (c < 0)
						break;
				}
			} else {
				i = BinarySearchK(op.SearchKey, op.CompareKeys, (int)op.SearchMode & 1);
			}
		}*/
	}
}
