using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Diagnostics;

namespace Loyc.CompilerCore
{
    /// <summary>Represents a list of a node's children.</summary>
    /// <remarks>
    /// AstList is a struct in order to increase memory efficiency. Classes have at
    /// least 12 bytes of overhead (in 32-bit code), and since an AstNode may
    /// contain multiple lists, creating an object for each list would be expensive
    /// while compiling a large program. Hence, this compact 8-byte struct is used
    /// instead.
    /// 
    /// Because AstList is a struct, it cannot provide virtual methods. Therefore,
    /// AstList calls virtual methods in AstNode to allow derived classes to
    /// implement their own list management. However, AstList itself takes care of
    /// calling ILanguageStyle.AstListChanged and AstNode.SetParent when the list is
    /// modified. This guarantees that the parent reference is updated correctly and
    /// that the language style is informed of changes to the AST. It also means
    /// that derived classes are freed from the chore of calling these methods.
    /// 
    /// Clear(), AddRange() and InsertRange() reparent all the items before calling
    /// Clear() or InsertRange() on the AstNode. If any of the reparenting calls
	/// throw an exception, AstList tries to reverse the reparenting operation
	/// before rethrowing the exception.
    /// 
    /// If the AstNode throws an exception during a call to Insert, InsertRange,
    /// RemoveAt, Clear, or the list setter, AstList uses the node's indexer to test
    /// whether the list was actually modified. If it was modified, AstList posts a
	/// notification by calling ILanguageStyle.AstListChanged. If it was not
	/// modified, AstNode reparents the relevant item(s) to restore them to their
	/// original states. For example, if AstNode.RemoveAt() throws and the item is
	/// still in the list, AstList restores the parent by calling
	/// item.SetParent(this).
    /// </remarks>
	[DebuggerTypeProxy(typeof(AstNodeCollectionDebugView)), DebuggerDisplay("Count = {Count}")]
	public struct AstList : IList<AstNode>, IList<IAstNode>, ISimpleSource2<AstNode>
	{
		public AstList(AstNode node, Symbol listId) {
			Debug.Assert(node != null);
			_node = node; _listId = listId;
		}
		//[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private System.Collections.IList ItemList { get { return new DebuggerList(this); } }
		private class DebuggerList : System.Collections.IList
		{
			AstList _l;
			public DebuggerList(AstList l) { _l = l; }

			#region IList Members

			Exception NotDone() { return new Exception("The method or operation is not implemented."); }
			public int Add(object value) { throw NotDone(); }
			public void Clear() { throw NotDone(); }
			public bool Contains(object value) { throw NotDone(); }

			public int IndexOf(object value)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public void Insert(int index, object value)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public bool IsFixedSize
			{
				get { return false; }
			}

			public bool IsReadOnly
			{
				get { return true; }
			}

			public void Remove(object value)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public void RemoveAt(int index)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public object this[int index]
			{
				get { return _l[index]; }
				set
				{
					throw new Exception("The method or operation is not implemented.");
				}
			}

			#endregion

			#region ICollection Members

			public void CopyTo(Array array, int index)
			{
				throw new Exception("The method or operation is not implemented.");
			}

			public int Count
			{
				get { return _l.Count; }
			}

			public bool IsSynchronized
			{
				get { return false; }
			}

			public object SyncRoot
			{
				get { return this; }
			}

			#endregion

			#region IEnumerable Members

			public System.Collections.IEnumerator GetEnumerator()
			{
				return _l.GetEnumerator();
			}

			#endregion
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private AstNode _node;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private Symbol _listId;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static Symbol _Insert = Symbol.Get("Insert");
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static Symbol _Remove = Symbol.Get("Remove");
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static Symbol _Set = Symbol.Get("Set");

		public AstNode Node { get { return _node; } }
		public Symbol ListId { get { return _listId; } }

		internal void AstListChanged(int firstIndex, Symbol changeType)
		{
			ILanguageStyle language = _node.Language;
			if (language != null)
				language.AstListChanged(this, firstIndex, changeType);
		}

		public AstNode First
		{
			get {
				if (_node.Count(_listId) == 0)
					return null;
				return _node[_listId, 0];
			}
			set {
				if (Count == 0)
					Add(value);
				else
					this[0] = value;
			}
		}
		public AstNode Second
		{
			get { 
				if (_node.Count(_listId) < 2)
					return null;
				return _node[_listId, 1];
			}
			set {
				int count = Count;
				if (count == 0)
					throw new InvalidOperationException("AstList: First must be set before Second.");
				if (count == 1)
					Add(value);
				else
					this[1] = value;
			}
		}

		public void AddRange(IList<AstNode> items)
		{
			InsertRange(Count, items);
		}
		public void InsertRange(int index, IList<AstNode> items)
		{
			if (items == null)
				throw new ArgumentNullException("items");

			// Reparent the items.
			int itemCount = items.Count;
			for (int i = 0; i < itemCount; i++) {
				try {
					AstNode item = items[i];
					if (item == null)
						throw new InvalidOperationException("null cannot be put in an AstList.");
					item.SetParent(this);
				} catch {
					// Try to undo the reparenting done so far by setting the
					// parents of the items to null.
					for (int j = 0; j < i; j++) {
						try {
							items[j].ClearParent(this);
						} catch (Exception e) {
							Debug.Fail(e.Message);
						}
					}
					throw;
				}
			}
			int oldCount = Count;

			try {
				_node.InsertRange(_listId, index, items);
			} catch {
				if (Count == oldCount) {
					for (int i = 0; i < itemCount; i++) {
						try {
							items[i].ClearParent(this);
						} catch (Exception e) {
							Debug.Fail(e.Message);
						}
					}
				} else {
					Debug.Assert(Count == oldCount + itemCount);
					AstListChanged(oldCount, _Insert);
				}
				throw;
			}
			
			Debug.Assert(Count == oldCount + itemCount);
			AstListChanged(oldCount, _Insert);
		}

		#region IList<AstNode>, IList<IAstNode> Members

		int IList<IAstNode>.IndexOf(IAstNode item) { return IndexOf((AstNode)item); }
		public int IndexOf(AstNode item)
		{
			int count = Count;
			for (int i = 0; i < count; i++)
				if (_node[_listId, i] == item)
					return i;
			return -1;
		}

		void IList<IAstNode>.Insert(int index, IAstNode item) { Insert(index, (AstNode)item); }
		public void Insert(int index, AstNode item)
		{
			if (item == null)
				throw new InvalidOperationException("null cannot be put in an AstList.");
			item.SetParent(this);

			try {
				_node.Insert(_listId, index, item);
			} catch {
				try {
					if (Count == index || this[index] != item)
						item.ClearParent(this);
					else
						AstListChanged(index, _Insert);
				} catch (Exception e) {
					Debug.Fail(e.Message);
				}
				throw;
			}
			
			AstListChanged(index, _Insert);
		}

		public void RemoveAt(int index)
		{
			AstNode item = _node[_listId, index];
			item.ClearParent(this);

			try {
				_node.RemoveAt(_listId, index);
			} catch {
				try {
					if (Count > index && this[index] == item)
						item.SetParent(this);
					else
						AstListChanged(index, _Remove);
				} catch (Exception e) {
					Debug.Fail(e.Message);
				}
				throw;
			}
			
			AstListChanged(index, _Remove);
		}

		public AstNode this[int index]
		{
			get { return _node[_listId, index]; }
			set {
				if (value == null)
					throw new InvalidOperationException("null cannot be put in an AstList.");

				AstNode old = _node[_listId, index];
				if (old != value) {
					// Reparent the old & new nodes
					old.ClearParent(this); // may throw
					try {
						value.SetParent(this);
					} catch {
						// Oops, can't reparent the new node. Restore the parent of
						// the old node and return without changing the list.
						try {
							old.SetParent(this);
						} catch (Exception e) {
							Debug.Fail(e.Message);
						}
						throw;
					}
					
					try {
						_node[_listId, index] = value; // may throw
					} catch {
						// Clear parent if the list was not updated
						try {
							AstNode cur = this[index];
							if (cur != value)
								value.ClearParent(this);
							if (cur == old)
								old.SetParent(this);
							else
								AstListChanged(index, _Set);
						} catch(Exception e) {
							Debug.Fail(e.Message);
						}
						throw;
					}
					
					AstListChanged(index, _Set);
				}
			}
		}
		IAstNode IList<IAstNode>.this[int index]
		{
			get { return this[index]; }
			set { this[index] = (AstNode)value; }
		}

		#endregion
		#region ICollection<AstNode>, ICollection<IAstNode> Members

		void ICollection<IAstNode>.Add(IAstNode item) { Add((AstNode)item); }
		public void Add(AstNode item)
		{
			Insert(Count, item);
		}
		public void Clear()
		{
			// Reparent the items to null.
			int count = Count;
			for (int i = 0; i < count; i++) {
				try {
					this[i].ClearParent(this);
				} catch {
					// Try to undo the reparenting done so far.
					for (int j = 0; j < i; j++) {
						try {
							this[j].SetParent(this);
						} catch (Exception e) {
							Debug.Fail(e.Message);
						}
					}
					throw;
				}
			}
			
			int oldCount = count;
			try {
				_node.Clear(_listId);
			} catch {
				count = Count;
				for (int i = 0; i < count; i++) {
					try {
						this[i].SetParent(this);
					} catch (Exception e) {
						Debug.Fail(e.Message);
					}
				}
				if (count != oldCount)
					AstListChanged(0, _Remove);
				throw;
			}

			if (oldCount > 0)
				AstListChanged(0, _Remove);
		}
		bool ICollection<IAstNode>.Contains(IAstNode item) { return Contains((AstNode)item); }
		public bool Contains(AstNode item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(IAstNode[] array, int arrayIndex)
		{
			int count = Count;
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = _node[_listId, i];
		}
		public void CopyTo(AstNode[] array, int arrayIndex)
		{
			int count = Count;
			for (int i = 0; i < count; i++)
				array[arrayIndex + i] = _node[_listId, i];
		}
		public int Count
		{
			get { return _node.Count(_listId); }
		}
		public bool IsReadOnly
		{
			get { return _node.IsReadOnly(_listId); }
		}
		bool ICollection<IAstNode>.Remove(IAstNode item) { return Remove((AstNode)item); }
		public bool Remove(AstNode item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}

		#endregion
		#region IEnumerable<AstNode>, IEnumerable<IAstNode> Members

		public IEnumerator<AstNode> GetEnumerator()
		{
			int count = Count;
			for (int i = 0; i < count; i++)
				yield return this[i];
			if (count != Count)
				throw new InvalidOperationException("The collection changed length during enumeration.");
		}
		IEnumerator<IAstNode> IEnumerable<IAstNode>.GetEnumerator()
		{
			int count = Count;
			for (int i = 0; i < count; i++)
				yield return this[i];
			if (count != Count)
				throw new InvalidOperationException("The collection changed length during enumeration.");
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
		#region IIndexToLine Members

		public SourcePos IndexToLine(int index)
		{
			return this[index].Position;
		}

		#endregion
	}
	
	/// <summary>Debug view helper for AstList</summary>
	/// <remarks>Explained at http://www.codeproject.com/KB/dotnet/DebugIList.aspx.</remarks>
	public class AstNodeCollectionDebugView
	{
		private ICollection<AstNode> collection;

		public AstNodeCollectionDebugView(ICollection<AstNode> collection)
		{
			if (collection == null)
				throw new ArgumentNullException("collection");
			this.collection = collection;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public AstNode[] Items
		{
			get
			{
				AstNode[] array = new AstNode[this.collection.Count];
				this.collection.CopyTo(array, 0);
				return array;
			}
		}
	}
}
