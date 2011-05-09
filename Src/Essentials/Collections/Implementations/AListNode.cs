using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections.Impl
{
	/// <summary>
	/// Base class for nodes in an <see cref="AList{T}"/>. These nodes basically 
	/// form an in-memory B+tree, so there are two types: leaf and inner nodes.
	/// </summary>
	/// <remarks>
	/// Indexes that are passed to methods such as Index, this[] and RemoveAt are
	/// not range-checked except by assertion. The caller (AList) is expected to 
	/// ensure indexes are valid.
	/// <para/>
	/// At the root node level, indexes have the same meaning as they do in AList
	/// itself. However, below the root node, each node has a "base index" that 
	/// is subtracted from any index passed to the node. For example, if the root 
	/// node has two leaf children, and the left one has 20 items, then the right
	/// child's base index is 20. When accessing item 23, the subindex 3 is passed 
	/// to the right child. Note that the right child is not aware of its own base
	/// index (the parent node manages the base index); as far as each node is 
	/// concerned, it manages a collection of items numbered 0 to TotalCount-1.
	/// <para/>
	/// Indexes are expressed with a uint so that nodes are capable of holding up 
	/// to uint.MaxValue-1 elements. AList itself doesn't support sizes over 
	/// int.MaxValue, since it assumes indexes are signed. It should be possible 
	/// to support oversize lists in 64-bit machines by writing a derived class 
	/// based on "uint" or "long" indexes; 32-bit processes, generally, don't 
	/// have enough address space to even hold int.MaxValue bytes.
	/// </remarks>
	[Serializable]
	public abstract class AListNode<T>
	{
		/// <summary>Inserts an item at the specified index.</summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		/// <returns>Returns null if the insert completed normally. If the node 
		/// split, a pair of replacement nodes are returned in a new AListInner 
		/// object, which is a temporary object unless it becomes the new root 
		/// node.</returns>
		public abstract AListInner<T> Insert(uint index, T item);
		/// <summary>Inserts a list of items at the specified index. This method
		/// may not insert all items at once, so there is a sourceIndex parameter 
		/// which points to the next item to be inserted. When sourceIndex reaches
		/// source.Count, the insertion is complete.</summary>
		/// <param name="index">The index at which to insert the contents of 
		/// source. Important: if sourceIndex > 0, insertion of the remaining 
		/// items starts at [index + sourceIndex].</param>
		/// <returns>Returns non-null on split, as explained in the other overload.</returns>
		public abstract AListInner<T> Insert(uint index, IListSource<T> source, ref int sourceIndex);
		/// <summary>Gets the total number of (T) items in this node and all children</summary>
		public abstract uint TotalCount { get; }
		/// <summary>Gets the number of items (slots) used this node only.</summary>
		public abstract int LocalCount { get; }
		/// <summary>Returns true if the node is full and is a leaf node.</summary>
		public abstract bool IsFullLeaf { get; }
		/// <summary>Gets or sets an item at the specified sub-index.</summary>
		public abstract T this[uint index] { get; set; }
		/// <summary>Removes an item at the specified index</summary>
		/// <returns>If the result is Underflow, it means that the node size has 
		/// dropped below its normal range. Unless this is the root node, the 
		/// parent will shift items from siblings, or discard the node and 
		/// redistribute its children among existing nodes. In case of the root 
		/// node, it is only discarded if it is an inner node with a single child
		/// (the child becomes the new root node).</returns>
		public abstract RemoveResult RemoveAt(uint index);
		public enum RemoveResult { OK, Underflow };

		internal abstract void TakeFromRight(AListNode<T> child);
		internal abstract void TakeFromLeft(AListNode<T> child);

		public abstract bool IsFrozen { get; }
		public abstract void Freeze();

		public abstract int CapacityLeft { get; }
	}
}
