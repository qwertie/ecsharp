namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;
	using System.Collections.Specialized;
	
	/// <summary>
	/// Leaf node of <see cref="BList{T}"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public class BListLeaf<K, T> : AListLeaf<K, T>
	{
		public BListLeaf(ushort maxNodeSize) : base(maxNodeSize) { }
		public BListLeaf(ushort maxNodeSize, InternalDList<T> list) : base(maxNodeSize, list) { }
		public BListLeaf(BListLeaf<K, T> frozen) : base(frozen) { }

		public override int DoSingleOperation(ref AListSingleOperation<K, T> op, out AListNode<K, T> splitLeft, out AListNode<K, T> splitRight)
		{
			T searchItem = op.Item;
			int index = _list.BinarySearch(op.Key, op.CompareToKey, op.LowerBound);
			if (op.Found = (index >= 0)) {
				op.Item = _list[index]; // save old value
				if (op.RequireExactMatch && (searchItem == null ? op.Item == null : !searchItem.Equals(op.Item)))
					op.Found = false;
			} else
				index = ~index;

			splitLeft = splitRight = null;
			op.BaseIndex += (uint)index;

			if (op.Mode == AListOperation.Retrieve)
				return 0;

			if (op.Mode >= AListOperation.Add)
			{
				// Possible operations: Add, AddOrReplace, AddIfNotPresent, AddOrThrow
				if (_list.Count >= _maxNodeSize && (op.Mode == AListOperation.Add || !op.Found))
				{
					op.BaseIndex -= (uint)index;
					op.Item = searchItem;
					return SplitAndAdd(ref op, out splitLeft, out splitRight);
				}

				if (op.Found && op.Mode != AListOperation.Add)
				{
					if (op.Mode == AListOperation.AddOrThrow)
						throw new KeyAlreadyExistsException();
					else if (op.Mode == AListOperation.AddIfNotPresent)
						return 0;
				} 
				else // add new item
				{
					if (HasListChanging(op.List))
						CallListChanging(op.List, new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, (int)op.BaseIndex, 1, Range.Single(searchItem)));

					if (index == _list.Count)
					{	// Highest key may change
						splitLeft = this;
						op.AggregateChanged |= 1;
						op.AggregateKey = GetKey(op.List, searchItem);
					}

					_list.AutoRaiseCapacity(1, _maxNodeSize);
					_list.Insert(index, searchItem);

					if (GetObserver(op.List) != null)
					{
						if ((op.AggregateChanged & 2) == 0)
							GetObserver(op.List).ItemAdded(searchItem, this);
					}
					return 1;
				}
				Debug.Assert(op.Mode == AListOperation.AddOrReplace);
			}
			else if (op.Found)
			{
				// Possible operations: ReplaceIfPresent, Remove
				if (op.Mode == AListOperation.Remove)
				{
					if (HasListChanging(op.List))
						CallListChanging(op.List, new ListChangeInfo<T>(NotifyCollectionChangedAction.Remove, (int)op.BaseIndex, -1, null));

					_list.RemoveAt(index);

					if (index == _list.Count)
					{	// Highest key may change
						splitLeft = this;
						if (_list.Count != 0) {
							op.AggregateChanged |= 1;
							op.AggregateKey = GetKey(op.List, _list.Last);
						}
					}
					else if (IsUndersized)
						splitLeft = this;

					if (GetObserver(op.List) != null)
					{
						Debug.Assert((op.AggregateChanged & 2) == 0);
						GetObserver(op.List).ItemRemoved(op.Item, this);
					}

					return -1;
				}
				Debug.Assert(op.Mode == AListOperation.ReplaceIfPresent);
			}
			else
			{
				Debug.Assert(op.Mode == AListOperation.Remove || op.Mode == AListOperation.ReplaceIfPresent);
				return 0; // can't remove/replace because item was not found.
			}

			// Fallthrough action: replace existing item
			Debug.Assert(op.Found);
			if (HasListChanging(op.List))
				CallListChanging(op.List, new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, (int)op.BaseIndex, 0, Range.Single(searchItem)));
			
			_list[index] = searchItem;

			if (index + 1 == _list.Count)
			{	// Highest key may change
				splitLeft = this;
				op.AggregateChanged |= 1;
				op.AggregateKey = GetKey(op.List, searchItem);
			}

			if (GetObserver(op.List) != null) {
				GetObserver(op.List).ItemRemoved(op.Item, this);
				GetObserver(op.List).ItemAdded(searchItem, this);
			}
			return 0;
		}

		/// <summary>Called by DoSingleOperation to split a full node, then retry the add operation.</summary>
		/// <remarks>Same arguments and return value as DoSingleOperation.</remarks>
		protected virtual int SplitAndAdd(ref AListSingleOperation<K, T> op, out AListNode<K, T> splitLeft, out AListNode<K, T> splitRight)
		{
			// Tell DoSingleOperation not to send notifications to the observer
			op.AggregateChanged |= 2;

			int divAt = _list.Count >> 1;
			var mid = _list[divAt];
			var left = new BListLeaf<K, T>(_maxNodeSize, _list.CopySection(0, divAt));
			var right = new BListLeaf<K, T>(_maxNodeSize, _list.CopySection(divAt, _list.Count - divAt));
			int sizeChange;
			if (op.CompareToKey(mid, op.Key) >= 0)
				sizeChange = left.DoSingleOperation(ref op, out splitLeft, out splitRight);
			else {
				op.BaseIndex += left.TotalCount;
				sizeChange = right.DoSingleOperation(ref op, out splitLeft, out splitRight);
			}

			op.AggregateChanged &= unchecked((byte)~2);

			// (splitLeft may be non-null, meaning that the highest key changed, which doesn't matter here.)
			Debug.Assert(splitRight == null);
			Debug.Assert(sizeChange == 1);
			splitLeft = left;
			splitRight = right;
			return sizeChange;
		}

		public override AListNode<K, T> DetachedClone()
		{
			_isFrozen = true;
			return new BListLeaf<K, T>(this);
		}

		public override AListNode<K, T> CopySection(uint index, uint count, AListBase<K,T> list)
		{
			Debug.Assert((int)(count | index) > 0);
			if (index == 0 && count == _list.Count)
				return DetachedClone();

			return new BListLeaf<K, T>(_maxNodeSize, _list.CopySection((int)index, (int)count));
		}

		public override bool RemoveAt(uint index, uint count, IAListTreeObserver<K, T> tob)
		{
			return base.RemoveAt(index, count, tob) || index == LocalCount;
		}
	}
}
