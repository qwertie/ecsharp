using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections.Impl
{
	/// <summary>
	/// Internal implementation class. Base class for tree nodes in a list class 
	/// derived from <see cref="AListBase{T}"/>. These nodes basically form an 
	/// in-memory B+tree, not necessarily sorted, but structured like a B+tree. That 
	/// means there are two node types: leaf and inner (internal) nodes.
	/// </summary>
	/// <remarks>
	/// Indexes that are passed to methods such as Index, this[] and RemoveAt are
	/// not range-checked except by assertion. The caller (AList or BList) is 
	/// expected to ensure indexes are valid.
	/// <para/>
	/// At the root node level, indexes have the same meaning as they do in 
	/// AListBase itself. However, below the root node, each node has a "base 
	/// index" that is subtracted from any index passed to the node. For example, 
	/// if the root node has two leaf children, and the left one has 20 items, then 
	/// the right child's base index is 20. When accessing item 23, the subindex 3 
	/// is passed to the right child. Note that the right child is not aware of its 
	/// own base index (the parent node manages the base index); as far as each 
	/// node is concerned, it manages a collection of items numbered 0 to 
	/// TotalCount-1.
	/// <para/>
	/// Indexes are expressed with a uint so that nodes are capable of holding up 
	/// to uint.MaxValue-1 elements. AList itself doesn't support sizes over 
	/// int.MaxValue, since it assumes indexes are signed (some protected methods
	/// in AList take unsigned indexes, however). It should be possible to support 
	/// oversize lists in 64-bit machines by writing a derived class based on 
	/// "uint" or "long" indexes; 32-bit processes, however, don't have enough 
	/// address space to even hold int.MaxValue bytes.
	/// <para/>
	/// Before calling any method that modifies a node, it is necessary to call
	/// AutoClone() to check if the node is frozen and clone it if necessary.
	/// TakeFromRight and TakeFromLeft can be called when one or both nodes are 
	/// frozen, but will have no effect.
	/// <para/>
	/// Philosophical note: the kinds of features I'd like to see in Enhanced C#
	/// would make it easier to create the AList series of data structures. Each
	/// kind of AList is similar yet different and many of the operations of an
	/// AList are similar yet different and C# doesn't, I think, provide enough
	/// flexibility to handle this variety without smooshing a bunch of different 
	/// things clumsily into a single class hierarchy and without having to write
	/// methods that are in charge of several different operations (e.g. 
	/// DoSingleOperation). Traits and D-style templates would help make the cod
	/// of the AList family of types cleaner and faster.
	/// </remarks>
	[Serializable]
	public abstract class AListNode<K, T>
	{
		public abstract bool IsLeaf { get; }

		/// <summary>Inserts an item at the specified index. This method can only
		/// be called for ALists, since other tree types don't allow insertion at
		/// a specific index.</summary>
		/// <returns>Returns null if the insert completed normally. If the node 
		/// split in half, the return value is the left side, and splitRight is
		/// set to the right side.</returns>
		/// <exception cref="NotSupportedException">This node does not allow insertion at an arbitrary location (e.g. BList node).</exception>
		public virtual AListNode<K, T> Insert(uint index, T item, out AListNode<K, T> splitRight, IAListTreeObserver<K, T> tob)
		{
			throw new NotSupportedException();
		}

		/// <summary>Inserts a list of items at the specified index. This method
		/// may not insert all items at once, so there is a sourceIndex parameter 
		/// which points to the next item to be inserted. When sourceIndex reaches
		/// source.Count, the insertion is complete.</summary>
		/// <param name="index">The index at which to insert the contents of 
		/// source. Important: if sourceIndex > 0, insertion of the remaining 
		/// items starts at [index + sourceIndex].</param>
		/// <returns>Returns non-null if the node is split, as explained 
		/// in the documentation of <see cref="Insert"/>.</returns>
		/// <remarks>This method can only be called for ALists, since other tree 
		/// types don't allow insertion at a specific index.</remarks>
		/// <exception cref="NotSupportedException">This node does not allow insertion at an arbitrary location (e.g. BList node).</exception>
		public virtual AListNode<K, T> InsertRange(uint index, IListSource<T> source, ref int sourceIndex, out AListNode<K, T> splitRight, IAListTreeObserver<K, T> tob)
		{
			throw new NotSupportedException();
		}

		/// <summary>Performs a retrieve, add, remove or replace operation on a
		/// single item in an organized A-list (such as a BList or BDictionary).</summary>
		/// <param name="op">An object that describes the operation to be performed
		/// and the parameters of the tree (comparers and observers).</param>
		/// <param name="splitLeft">null if the operation completed normally. If an
		/// item was added and the node split, splitLeft and splitRight are new 
		/// nodes that each contain roughly half of the items from this node.
		/// <para/>
		/// If an item was removed and the node became undersized, splitLeft is set
		/// to this (the node itself) and splitRight is set to null. Likewise, if
		/// the aggregate value of the node changed (in a B+tree, this means that
		/// the highest key changed) then splitLeft is set to the node itself and
		/// splitRight is set to null.
		/// </param>
		/// <returns>Returns 1 if a new item was added, -1 if an item was removed,
		/// or 0 if the number of items in the tree did not change.</returns>
		/// <exception cref="NotSupportedException">This node does not belong to an 
		/// organized tree (e.g. normal AList).</exception>
		/// <exception cref="KeyAlreadyExistsException">The key op.NewKey already 
		/// existed in the tree and op.Mode was 
		/// <see cref="AListOperation"/>.AddOrThrow.</exception>
		/// <remarks>
		/// If op.Mode is <see cref="AListOperation"/>.ReplaceIfPresent, this method
		/// informs the caller when replacement occurs in this mode by changing 
		/// op.Mode to AddDuplicateMode.ReplaceExisting.
		/// </remarks>
		internal virtual int DoSingleOperation(ref AListSingleOperation<K, T> op, out AListNode<K, T> splitLeft, out AListNode<K, T> splitRight)
		{
			throw new NotSupportedException();
		}
		
		/// <summary>Performs an insert or replace operation in a <see cref="SparseAList{T}"/>.</summary>
		/// <param name="op">Describes the operation to be performed</param>
		/// <param name="index">Relative index in child node where the sparse
		/// operation began; <c>index + op.SourceIndex</c> is where the next 
		/// insertion or replacement must occur. For example if the child 
		/// node represents items 100-200, and op.SourceIndex is 7, and 
		/// the absolute index of the start of the operation (op.AbsoluteIndex)
		/// is 98, then <c>index</c> will be -2 (98-100) and the next insertion
		/// or replacement will occur at index 5 in the child. <c>index</c> may 
		/// be negative but <c>index + op.SourceIndex >= 0</c>.</param>
		/// <param name="splitLeft">If the node needs to split, splitLeft and 
		/// splitRight are set to the new pieces. If the node is undersized, 
		/// splitLeft is set to <c>this</c> (the called node) and splitRight 
		/// is set to null.</param>
		/// <returns>Size change (always 0 for a replacement operation)</returns>
		/// <remarks>In a language with templates, I would change a couple
		/// of elements of <see cref="AListSparseOperation{T}"/> into 
		/// template parameters.</remarks>
		internal virtual int DoSparseOperation(ref AListSparseOperation<T> op, int index, out AListNode<K, T> splitLeft, out AListNode<K, T> splitRight)
		{
			throw new NotSupportedException();
		}
		/// <summary>Same as this[index] except that if there is empty space at 
		/// the specified location, the index can be adjusted up or down 
		/// (depending on whether direction is 1 or -1) to reach an item and the 
		/// item at the new index is returned. index is set to null if this 
		/// doesn't work because there are no items above or below, or if 
		/// direction is 0. On entry, index must NOT be null.</summary>
		internal virtual T SparseGetNearest(ref int? index, int direction)
		{
			throw new NotSupportedException();
		}

		/// <summary>Gets the last item in the last leaf node (needed by B+ trees, 
		/// but is also called by <see cref="AListBase{K,T}.Last"/>).</summary>
		public abstract T GetLastItem();

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
		/// <remarks>Currently, this method can be called for all tree types, even 
		/// though improper use could break the tree invariant (e.g. sorted order of BList).
		/// </remarks>
		public abstract void SetAt(uint index, T item, IAListTreeObserver<K, T> tob);

		/// <summary>Removes an item at the specified index.</summary>
		/// <returns>Returns true if the node is undersized after the removal, or 
		/// if this is an organized tree and the removal caused the aggregate key 
		/// (highest key in a B+tree) to change.</returns>
		/// <remarks>
		/// When the node is undersized, but is not the root node, the parent will 
		/// shift an item from a sibling, or discard the node and redistribute its 
		/// children among existing nodes. If it is the root node, it is only 
		/// discarded if it is an inner node with a single child (the child becomes 
		/// the new root node), or it is a leaf node with no children.
		/// </remarks>
		public abstract bool RemoveAt(uint index, uint count, IAListTreeObserver<K, T> tob);

		/// <summary>Takes an element from a right sibling.</summary>
		/// <returns>Returns the number of elements moved on success (1 if a leaf 
		/// node, TotalCount of the child moved otherwise), or 0 if either (1) 
		/// IsFullLeaf is true, or (2) one or both nodes is frozen.</returns>
		internal abstract uint TakeFromRight(AListNode<K, T> rightSibling, IAListTreeObserver<K, T> tob);
		
		/// <summary>Takes an element from a left sibling.</summary>
		/// <returns>Returns the number of elements moved on success (1 if a leaf 
		/// node, TotalCount of the child moved otherwise), or 0 if either (1) 
		/// IsFullLeaf is true, or (2) one or both nodes is frozen.</returns>
		internal abstract uint TakeFromLeft(AListNode<K, T> leftSibling, IAListTreeObserver<K, T> tob);

		/// <summary>Returns true if the node is explicitly marked read-only. 
		/// Conceptually, the node can still be changed, but when any change needs 
		/// to be made, a clone of the node is created and modified instead.</summary>
		/// <remarks>When an inner node is frozen, all its children are implicitly 
		/// frozen, but not actually marked as frozen until the parent is cloned.
		/// This allows instantaneous cloning, since only the root node is marked 
		/// frozen in the beginning.</remarks>
		public bool IsFrozen { get { return _isFrozen; } }
		public abstract void Freeze();

		public abstract int CapacityLeft { get; }
		
		/// <summary>Creates an unfrozen shallow duplicate copy of this node. The 
		/// child nodes (if this is an inner node) are frozen so that they will
		/// require duplication if they are to be modified. The name 
		/// "DetachedClone" is intended to emphasize that the AListNodeObserver 
		/// (if any) is not notified, and the clone is effectively independent of 
		/// the list that it came from.</summary>
		public abstract AListNode<K, T> DetachedClone();

		/// <summary>Checks whether 'node' is frozen and if so, replaces it with an unfrozen copy.</summary>
		/// <param name="node">A node that the caller needs to be unfrozen</param>
		/// <param name="parent">Parent node (used by tob)</param>
		/// <param name="tob">Tree observer (null if none)</param>
		/// <returns>True if the node was unfrozen</returns>
		#if DotNet45
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		#endif
		public static bool AutoClone(ref AListNode<K, T> node, AListInnerBase<K, T> parent, IAListTreeObserver<K, T> tob)
		{
			if (node.IsFrozen) {
				node = Clone(node, parent, tob);
				return true;
			}
			return false;
		}
		static AListNode<K, T> Clone(AListNode<K, T> node, AListInnerBase<K, T> parent, IAListTreeObserver<K, T> tob)
		{
			var clone = node.DetachedClone();
			if (tob != null) tob.HandleChildReplaced(node, clone, null, parent);
			Debug.Assert(!clone.IsFrozen);
			return clone;
		}

		/// <summary>Same as Assert(), except that the condition expression can 
		/// have side-effects because it is evaluated even in Release builds.</summary>
		protected static void Verify(bool condition) { System.Diagnostics.Debug.Assert(condition); }

		/// <summary>Extracts and returns, as fast as possible, a subrange of the 
		/// list that this node represents.</summary>
		/// <param name="index">Index to start copying</param>
		/// <param name="count">Number of Ts to copy (must be greater than zero).</param>
		/// <param name="list">List that is making the request. This parameter
		/// may be needed by organized trees that need to call list.GetKey().</param>
		/// <remarks>This method may return a size-one inner node that the caller
		/// must replace with its child. It will fast-clone any nodes that can be
		/// copied in their entirety, including this node itself.</remarks>
		public abstract AListNode<K, T> CopySection(uint index, uint count, AListBase<K, T> list);

		/// <summary>Maximum number of slots in this node</summary>
		protected ushort _maxNodeSize;
		/// <summary>Whether the node is knowingly cloned an therefore frozen.</summary>
		protected volatile bool _isFrozen;
		/// <summary>Number of children, if this is an inner node.</summary>
		/// <remarks>
		/// Since <see cref="AListLeaf{T}"/> uses DListInternal, a separate item
		/// count is not needed and this counter is always zero. This field 
		/// logically belongs in <see cref="AListInnerBase{K,T}"/> but is defined here
		/// to ensure that inner nodes are not 4 bytes larger than necessary. This
		/// field is "free" if it is declared in the base class, since class sizes
		/// are rounded up to the nearest multiple of 4 bytes (8 bytes in 64-bit).
		/// The fact that this field is a byte, however, does limit inner node 
		/// sizes to 255.
		/// </remarks>
		protected byte _childCount;

		/// <summary>Allows derived classes of AListNode to access AListBase._observer.</summary>
		protected IAListTreeObserver<K, T> GetObserver(AListBase<K, T> tree) { return tree._observer; }
		/// <summary>Allows derived classes of AListNode to fire the AListBase.ListChanging event.</summary>
		protected bool HasListChanging(AListBase<K, T> tree) { return tree._listChanging != null; }
		/// <summary>Allows derived classes of AListNode to fire the AListBase.ListChanging event properly.</summary>
		protected void CallListChanging(AListBase<K, T> tree, ListChangeInfo<T> listChangeInfo)
		{
			if (tree._listChanging != null)
				tree.CallListChanging(listChangeInfo);
		}

		/// <summary>Diagnostic method. See <see cref="AListBase{K,T}.GetImmutableCount()"/>.</summary>
		/// <param name="excludeSparse">Should be false for normal ALists. If true, the count is of real items only.</param>
		public abstract uint GetImmutableCount(bool excludeSparse);
		
		/// <summary>For sparse lists: counts the number of non-sparse items.</summary>
		public virtual uint GetRealItemCount() { return TotalCount; }
	}

	/// <summary>This structure is passed from the collection class (AList, BList 
	/// or BDictionary) to the tree (AListNode classes), and it holds information 
	/// needed to run a command like "add or replace item", "add if not present" 
	/// or "remove item".</summary>
	/// <typeparam name="K">Key type (stored in inner nodes)</typeparam>
	/// <typeparam name="T">Item type (stored in leaf nodes)</typeparam>
	/// <remarks>A-List operations as diverse as "add", "remove" and "retrieve"
	/// are actually very similar and are implemented by the same method, 
	/// <see cref="AListNode{K,T}.DoSingleOperation"/>.</remarks>
	internal struct AListSingleOperation<K, T>
	{
		/// <summary>Specifies which operation is to be performed.</summary>
		public AListOperation Mode;
		/// <summary>While traversing the tree, this starts at zero and is 
		/// incremented by the base index of each new node before it is traversed.
		/// In the leaf node it is incremented by the local index of the affected
		/// item, so that its final value is the actual index of the item that
		/// was added, removed, replaced or retrieved.</summary>
		/// <remarks>
		/// This value is needed to call ListChanging in the leaf, and
		/// it is also used to support IndexOf(K) if K is a key.
		/// <para/>
		/// If the Mode is Retrieve and the tree is a B+ tree and the Key was not 
		/// found, op.BaseIndex will tell you the index of the item with the 
		/// nearest higher key, or it will be equal to Count if the requested key 
		/// is higher than all keys in the tree.
		/// </remarks>
		public uint BaseIndex;
		/// <summary>A function that compares two keys.</summary>
		/// <remarks>NOTE: by convention, the search key (<see cref="Key"/>) is 
		/// always passed as the second parameter. The caller can use a special
		/// search function based on this assumption (e.g. 
		/// <see cref="BList{T}.FindUpperBound"/>.</remarks>
		public Func<K, K, int> CompareKeys;
		/// <summary>A function that compares an item to a key.</summary>
		/// <remarks>This is used to select an insertion location or (when 
		/// replacing an existing item) to find an item to be replaced. The
		/// second parameter will be the search key (<see cref="Key"/>).</remarks>
		public Func<T, K, int> CompareToKey;
		/// <summary>An A-list object that contains the tree being searched.</summary>
		public AListBase<K, T> List;
		/// <summary>For an add or remove operation, this field specifies an item 
		/// to be added or removed, and on exit it is set to the value of the item
		/// that was removed or replaced (if any). For a retrieval operation, its
		/// initial value is default(T) and it is set to the retrieved value on
		/// exit.</summary>
		public T Item;
		/// <summary>Key of the item to be added, removed or retrieved.</summary>
		public K Key;
		/// <summary>On exit, if <see cref="AggregateChanged"/> is true, this is
		/// set to the aggregate key of the node. In a B+ tree, this is the value 
		/// of the highest key in the node and all child nodes.</summary>
		public K AggregateKey;
		/// <summary>Specifies that the operation will not be performed unless 
		/// the candidate item c is not equal to Item (object.Equals(Item, c)).</summary>
		/// <remarks>
		/// WARNING: If the collection is a dictionary with duplicate keys, 
		/// <see cref="AListNode{K,T}.DoSingleOperation"/>() will NOT NECESSARILY 
		/// find an exact match when one exists! DoSingleOperation only finds one
		/// item with a matching key. It does not necessarily find the exact item
		/// you were looking for!
		/// <para/>
		/// In order to find an exact match when the dictionary has duplicate keys,
		/// one must check all candidate items one-by-one. DoSingleOperation cannot 
		/// do this because it is only designed to look for a single item.
		/// </remarks>
		public bool RequireExactMatch;
		/// <summary>DoSingleOperation() sets this field to true if the Key was 
		/// found in the collection.</summary>
		public bool Found;
		/// <summary>Specifies that when multiple items have the same key, the one
		/// with the lowest index should be found or replaced. If this is false (the
		/// default) then any of the items can be chosen, which may be slightly 
		/// faster.</summary>
		public bool LowerBound;
		/// <summary>DoSingleOperation() sets this field to 1 if the aggregate
		/// key value (in a B+ tree, the value of the highest key) changed.</summary>
		/// <remarks>
		/// This must be cleared before the list calls DoSingleOperation(). The
		/// data type is byte instead of bool so that <see cref="BListLeaf{K,T}"/> 
		/// can internally use the extra bits to notify itself when a node split is 
		/// in progress.
		/// <para/>
		/// If a node splits, the parent node is responsible to send 
		/// <see cref="IAListTreeObserver{K,T}.RemoveAll"/>() and AddAll() to 
		/// transfer the items to the new nodes. In that case, the child must not
		/// send an ItemAdded() notification because the AddAll() command will
		/// implicitly send that notification, and we mustn't double-notify.
		/// </remarks>
		public byte AggregateChanged;
	}

	/// <summary>This structure is passed from the collection class (SparseAList)
	/// to the tree (AListNode classes), and it holds information needed to run 
	/// a command like "change item at index X" or "clear empty space".</summary>
	/// <typeparam name="T">Item type (stored in leaf nodes)</typeparam>
	internal struct AListSparseOperation<T>
	{
		public AListSparseOperation(uint index, bool isInsert, bool writeEmpty, int count, IAListTreeObserver<int, T> tob)
		{
			AbsoluteIndex = (uint)index;
			IsInsert = isInsert;
			WriteEmpty = writeEmpty;
			Item = default(T);
			SourceIndex = 0;
			Source = SparseSource = null;
			SourceCount = count;
			this.tob = tob;
		}

		/// <summary>Which operation: Insert (true) or Set (false)</summary>
		public bool IsInsert;
		/// <summary>Whether to write empty space (true) or values (false)</summary>
		public bool WriteEmpty;
		/// <summary>Index into sparse list where the operation starts.</summary>
		public uint AbsoluteIndex;
		/// <summary>A single item to insert if <c>Source==null &amp;&amp; !WriteEmpty</c></summary>
		public T Item;
		/// <summary>When nonzero, the operation is partialy complete and items in 
		/// range Source[0...SourceIndex-1] have already been inserted.</summary>
		public int SourceIndex;
		/// <summary>Equals SparseSource.Count, but SparseSource may be null when inserting
		/// a single item (then SourceCount==1) or when inserting blank space.</summary>
		public int SourceCount;
		public IListSource<T> Source;
		public ISparseListSource<T> SparseSource; // Source as sparse
		public IAListTreeObserver<int, T> tob;
	}
}

namespace Loyc.Collections
{
	/// <summary>Indicates the way an add operation (such as <see cref="BList{T}.Do"/>
	/// should behave when an item being added to a set or list is a duplicate of 
	/// an item that is already present, or when the key of a key-value pair being 
	/// added to a dictionary is a duplicate of a key that is already present in 
	/// the dictionary.</summary>
	/// <remarks>All the "add" operations are deliberately listed last, so that
	/// <see cref="AListBase{K,T}.DoSingleOperation"/> can use a greater-than 
	/// operator to figure out whether an item may be added or not.</remarks>
	public enum AListOperation
	{
		/// <summary>Default operation. The item with the specified key will be 
		/// retrieved. The tree will not be modified.</summary>
		Retrieve = 0,
		/// <summary>Replace an existing item/key if present, or do nothing if 
		/// there is no matching item/key.</summary>
		ReplaceIfPresent = 1,
		/// <summary>Remove the item with the specified key if present.</summary>
		Remove = 2,
		/// <summary>A new item will be added unconditionally, without affecting 
		/// existing elements, in no particular order with respect to existing
		/// items that have the same key.</summary>
		Add = 3,
		/// <summary>A new item will replace an item that has the same key. If the 
		/// collection already contains multiple instances of the item/key, the 
		/// instance to be replaced is undefined.</summary>
		AddOrReplace = 4,
		/// <summary>A new item will be added if its key doesn't match an existing
		/// element. If the item already exists, it is not replaced.</summary>
		AddIfNotPresent = 5,
		/// <summary>The item will be added if its key is not already present, but
		/// <see cref="KeyAlreadyExistsException"/> or <see cref="InvalidOperationException"/> 
		/// will be thrown if the new item is equal to an existing element. If this 
		/// exception occurs during an AddRange() operation, some of the items may 
		/// have already been added successfully, and the changes will not be 
		/// rolled back unless otherwise specified in the documentation of the 
		/// method that performs the add operation.</summary>
		AddOrThrow = 6,
	};
}
