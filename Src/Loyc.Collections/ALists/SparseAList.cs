using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics;
using System.Collections.Specialized;

namespace Loyc.Collections
{
	/// <summary>A sparse A-List that implements <see cref="ISparseList{T}"/>.</summary>
	/// <remarks>
	/// The sparse A-List is implemented similarly to a normal A-List; the main 
	/// difference is that leaf nodes have a list of (int, T) pairs rather than
	/// a list of T values. The integers represent the relative index of each T
	/// value, as an offset from the beginning of the node. This allows an 
	/// arbitrary amount of empty space to exist between each T value in the
	/// list, making it sparse.
	/// <para/>
	/// <c>SparseAList</c> is a precise sparse list, meaning that you can rely on 
	/// it to keep track of which indexes are "set" and which are "empty" (the 
	/// <see cref="IsSet"/> method tells you which).
	/// </remarks>
	public class SparseAList<T> : AListBase<T>, ISparseList<T>, IListEx<T>, IListRangeMethods<T>, ICloneable<SparseAList<T>>
	{
		#region Constructors

		public SparseAList() { }
		public SparseAList(IEnumerable<T> items) { InsertRange(0, items); }
		public SparseAList(IListSource<T> items) { InsertRange(0, items); }
		public SparseAList(ISparseListSource<T> items) { InsertRange(0, items); }
		public SparseAList(int maxLeafSize) : base(maxLeafSize) { }
		public SparseAList(int maxLeafSize, int maxInnerSize) : base(maxLeafSize, maxInnerSize) { }
		public SparseAList(SparseAList<T> items, bool keepListChangingHandlers) : base(items, keepListChangingHandlers) { }
		protected SparseAList(AListBase<int, T> original, AListNode<int, T> section) : base(original, section) { }
		
		#endregion

		#region General supporting protected methods

		protected override AListNode<int, T> NewRootLeaf()
		{
			return new SparseAListLeaf<T>(_maxLeafSize);
		}
		
		#endregion

		void IAddRange<T>.AddRange(IReadOnlyCollection<T> source) { AddRange(source); }

		#region Insert, InsertRange methods (and DoSparseOperation)

		internal void DoSparseOperation(ref AListSparseOperation<T> op)
		{
			uint index = op.AbsoluteIndex;
			Debug.Assert((_freezeMode & 1) == 0);
			if ((uint)index > (uint)_count) {
				if (index < 0 || op.IsInsert)
					throw new ArgumentOutOfRangeException("index");
				return; // this clear operation has no effect
			}
			Debug.Assert(op.SourceCount > 0);
			Debug.Assert(op.SourceCount <= (int)(_count - index) || op.IsInsert);
			if (_listChanging != null) {
				if (op.Source == null)
					op.Source = op.WriteEmpty 
						? new EmptySpace<T>(op.SourceCount) 
						: op.Source ?? new Repeated<T>(op.Item, op.SourceCount);
				if (op.IsInsert)
					CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Add, (int)index, op.SourceCount, op.Source));
				else
					CallListChanging(new ListChangeInfo<T>(NotifyCollectionChangedAction.Replace, (int)index, 0, op.Source));
			}
			if (_root == null || _root.IsFrozen)
				AutoCreateOrCloneRoot();

			AListNode<int, T> splitLeft, splitRight;
			int sizeChange = 0;
			do {
				sizeChange += _root.DoSparseOperation(ref op, (int)index, out splitLeft, out splitRight);
				if (splitLeft != null)
					AutoSplit(splitLeft, splitRight);
			} while (op.SourceIndex < op.SourceCount);
			_count += (uint)sizeChange;
			Debug.Assert(sizeChange == (op.IsInsert ? op.SourceCount : 0));

			++_version;
			CheckPoint();
		}

		public sealed override void Add(T item)
		{
			Insert(Count, item);
		}
		public void AddRange(IEnumerable<T> list)
		{
			InsertRange(Count, list);
		}

		public sealed override void Insert(int index, T item)
		{
			AutoThrow();
			DetectSizeOverflow(1);
			var op = new AListSparseOperation<T>((uint)index, true, false, 1, _observer) { Item = item };
			DoSparseOperation(ref op);
		}

		public void InsertRange(int index, SparseAList<T> source) { InsertRange(index, source, false); }
		public void InsertRange(int index, SparseAList<T> source, bool move) { base.InsertRange(index, source, move); }
		
		void IListRangeMethods<T>.InsertRange(int index, IReadOnlyCollection<T> source) { InsertRange(index, source); }
		public sealed override void InsertRange(int index, IEnumerable<T> list)
		{
			InsertRange(index, list as IListSource<T> ?? new InternalList<T>(list));
		}
		public sealed override void InsertRange(int index, IListSource<T> list)
		{
			AutoThrow();
			var count = list.Count;
			DetectSizeOverflow(count);
			if (count <= 0)
				return;
			var op = new AListSparseOperation<T>((uint)index, true, false, count, _observer) {
				Source = list,
				SparseSource = list as ISparseListSource<T>
			};
			DoSparseOperation(ref op);
		}
		public void InsertRange(int index, ISparseListSource<T> list)
		{
			AutoThrow();
			var count = list.Count;
			DetectSizeOverflow(count);
			if (count <= 0)
				return;
			var op = new AListSparseOperation<T>((uint)index, true, false, count, _observer) {
				Source = list,
				SparseSource = list
			};
			DoSparseOperation(ref op);
		}

		#endregion

		#region Features delegated to AListBase: Clone, CopySection, RemoveSection, Swap

		protected override void Clone(out AListBase<T> clone)
		{
			clone = Clone();
		}
		public new SparseAList<T> Clone()
		{
			return Clone(false);
		}
		public SparseAList<T> Clone(bool keepListChangingHandlers)
		{
			return new SparseAList<T>(this, keepListChangingHandlers);
		}
		public SparseAList<T> CopySection(int start, int subcount)
		{
			return new SparseAList<T>(this, CopySectionHelper(start, subcount));
		}
		protected override AListBase<T> cov_RemoveSection(int start, int count) { return RemoveSection(start, count); }
		public new SparseAList<T> RemoveSection(int start, int count)
		{
			if ((uint)count > _count - (uint)start)
				throw new ArgumentOutOfRangeException(count < 0 ? "count" : "start+count");
			
			var newList = new SparseAList<T>(this, CopySectionHelper(start, count));
			// bug fix: we must RemoveRange after creating the new list, because 
			// the section is expected to have the same height as the original tree 
			// during the constructor of the new list.
			RemoveRange(start, count);
			return newList;
		}
		/// <summary>Swaps the contents of two <see cref="SparseAList{T}"/>s in O(1) time.</summary>
		/// <remarks>Any observers are also swapped.</remarks>
		public void Swap(SparseAList<T> other)
		{
			base.SwapHelper(other, true);
		}

		#endregion

		#region Bonus features (Append, Prepend)

		/// <inheritdoc cref="AList{T}.Append(AList{T}, bool)"/>
		public virtual void Append(SparseAList<T> other) { Combine(other, false, true); }

		/// <inheritdoc cref="AList{T}.Append(AList{T}, bool)"/>
		public virtual void Append(SparseAList<T> other, bool move) { Combine(other, move, true); }

		/// <summary>Prepends an AList to this list in sublinear time.</summary>
		/// <param name="other">A list of items to be added to the front of this list (at index 0).</param>
		/// <inheritdoc cref="Append(SparseAList{T}, bool)"/>
		public virtual void Prepend(SparseAList<T> other) { Combine(other, false, false); }
		
		/// <summary>Prepends an AList to this list in sublinear time.</summary>
		/// <param name="other">A list of items to be added to the front of this list (at index 0).</param>
		/// <inheritdoc cref="Append(SparseAList{T}, bool)"/>
		public virtual void Prepend(SparseAList<T> other, bool move) { Combine(other, move, false); }

		#endregion

		public sealed override T this[int index]
		{
			get {
				return ((AListBase<int, T>)this)[index];
			}
			set {
				if ((_freezeMode & 1) != 0) // Frozen or FrozenForConcurrency, but not FrozenForListChanging
					AutoThrow();
				var op = new AListSparseOperation<T>((uint)index, false, false, 1, _observer) { Item = value };
				DoSparseOperation(ref op);
			}
		}

		public sealed override bool TrySet(int index, T value)
		{
			if (_freezeMode != 0)
			{
				if (_freezeMode == FrozenForConcurrency)
					AutoThrow();
				if (_freezeMode == Frozen)
					return false;
			}
			if ((uint)index >= (uint)Count)
				return false;
			var op = new AListSparseOperation<T>((uint)index, false, false, 1, _observer) { Item = value };
			DoSparseOperation(ref op);
			return true;
		}
		
		public void Clear(int index, int count = 1)
		{
			AutoThrow();
			if (count <= 0)
				return;
			if (count > (int)_count - index && index >= 0)
				count = (int)_count - index;
			var op = new AListSparseOperation<T>((uint)index, false, true, count, _observer);
			DoSparseOperation(ref op);
		}

		public void InsertSpace(int index, int count = 1)
		{
			AutoThrow();
			DetectSizeOverflow(count);
			if (count <= 0)
				return;
			var op = new AListSparseOperation<T>((uint)index, true, true, count, _observer);
			DoSparseOperation(ref op);
		}

		public bool IsSet(int index)
		{
			throw new NotImplementedException();
		}

		public int? NextHigher(int index)
		{
			throw new NotImplementedException();
		}

		public int? NextLower(int index)
		{
			throw new NotImplementedException();
		}

		public override int IndexOf(T item)
		{
			// TODO: make efficient by skipping empty slots
			return base.IndexOf(item);
		}
	}
}

