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
		/// split in half, the return value is the left side, and splitRight is
		/// set to the right side. If the node is frozen, it is cloned prior to
		/// the insert, and the clone is returned unless the node splits.</returns>
		public abstract AListNode<T> Insert(uint index, T item, out AListNode<T> splitRight);
		/// <summary>Inserts a list of items at the specified index. This method
		/// may not insert all items at once, so there is a sourceIndex parameter 
		/// which points to the next item to be inserted. When sourceIndex reaches
		/// source.Count, the insertion is complete.</summary>
		/// <param name="index">The index at which to insert the contents of 
		/// source. Important: if sourceIndex > 0, insertion of the remaining 
		/// items starts at [index + sourceIndex].</param>
		/// <returns>Returns non-null if the node is split or cloned, as explained 
		/// in the other overload.</returns>
		public abstract AListNode<T> Insert(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<T> splitRight);

		/// <summary>Gets the total number of (T) items in this node and all children</summary>
		public abstract uint TotalCount { get; }
		/// <summary>Gets the number of items (slots) used this node only.</summary>
		public abstract int LocalCount { get; }
		/// <summary>Returns true if the node is full and is a leaf node.</summary>
		public abstract bool IsFullLeaf { get; }
		/// <summary>Returns true if the node is undersized, meaning it would 
		/// prefer to have more immediate children.</summary>
		public abstract bool IsUndersized { get; }
		/// <summary>Gets an item at the specified sub-index.</summary>
		public abstract T this[uint index] { get; }
		/// <summary>Sets an item at the specified sub-index.</summary>
		/// <returns>Returns null if the node is not frozen, or a modified clone 
		/// if the node is frozen.</returns>
		public abstract AListNode<T> SetAt(uint index, T item);

		/// <summary>Removes an item at the specified index.</summary>
		/// <returns>Returns null in the usual case--that the item at the specified 
		/// index was removed successfully, and the node is not undersized. If the
		/// node is frozen then this method creates and returns an unfrozen 
		/// duplicate copy of the node (with the specified item removed); if 
		/// the node is undersized but not frozen, the node itself (this) is 
		/// returned.
		/// <para/>
		/// When the node is undersized, but is not the root node, the parent will 
		/// shift an item from a sibling, or discard the node and redistribute its 
		/// children among existing nodes. If it is the root node, it is only 
		/// discarded if it is an inner node with a single child (the child becomes 
		/// the new root node), or it is a leaf node with no children.
		/// </returns> 
		public abstract AListNode<T> RemoveAt(uint index, uint count);

		/// <summary>Takes an element from a right sibling.</summary>
		/// <returns>Returns the number of elements moved on success (1 if a leaf 
		/// node, TotalCount of the child moved otherwise), or 0 if either (1) 
		/// IsFullLeaf is true, or (2) one or both nodes is frozen.</returns>
		internal abstract uint TakeFromRight(AListNode<T> rightSibling);
		
		/// <summary>Takes an element from a left sibling.</summary>
		/// <returns>Returns the number of elements moved on success (1 if a leaf 
		/// node, TotalCount of the child moved otherwise), or 0 if either (1) 
		/// IsFullLeaf is true, or (2) one or both nodes is frozen.</returns>
		internal abstract uint TakeFromLeft(AListNode<T> leftSibling);

		/// <summary>Returns true if the node is explicitly marked read-only. 
		/// Conceptually, the node can still be changed, but when any change needs 
		/// to be made, a clone of the node is created and modified instead.</summary>
		/// <remarks>When an inner node is frozen, all its children are implicitly 
		/// frozen, but not actually marked as frozen until the parent is cloned.
		/// This allows instantaneous cloning, since only the root node is marked 
		/// frozen in the beginning.</remarks>
		public abstract bool IsFrozen { get; }
		public abstract void Freeze();

		public abstract int CapacityLeft { get; }
		
		/// <summary>Creates an unfrozen duplicate copy of this node.</summary>
		public abstract AListNode<T> Clone();

		/// <summary>Same as Assert(), except that the condition expression can 
		/// have side-effects because it is evaluated even in Release builds.</summary>
		protected static void Verify(bool condition) { System.Diagnostics.Debug.Assert(condition); }
	}
}
