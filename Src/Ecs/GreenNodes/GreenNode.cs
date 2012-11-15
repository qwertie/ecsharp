using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using Loyc.Collections;
using System.Diagnostics;
using Loyc.Collections.Impl;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	using S = CodeSymbols;

	/// <summary>
	/// Represents a green node, which is a low-level node class typically used by
	/// parsers. See documentation of <see cref="INodeReader"/> for more 
	/// information.
	/// </summary>
	public abstract class GreenNode : INodeReader, IEquatable<GreenNode>, IEnumerable<GreenAndOffset>
	{
		private int _stuff;
		const int FrozenFlag = unchecked((int)0x80000000);
		const int IsCallFlag = unchecked((int)0x40000000);
		const int StyleShift = 22;
		const int SourceWidthMask = (1 << StyleShift) - 1;

		// If Head != this and Head is mutable, this is _null to force lookup in Head, because the name can change
		protected Symbol _name;

		protected GreenNode(Symbol name, int sourceWidth)
		{
			_stuff = System.Math.Min(sourceWidth, SourceWidthMask >> 1);
			_name = name;
			G.RequireArg(_name != null && _name.Name != "");
		}
		protected GreenNode(GreenNode head, int sourceWidth) : this(head.Name, sourceWidth)
		{
			Debug.Assert(head != this && head != null);
			Debug.Assert((head.Name.Name ?? "") != "");
			if (!head.IsFrozen)
				_name = null; // do not cache Name; it could change in Head
		}
		protected GreenNode(GreenNode clone)
		{
			_stuff = clone._stuff & ~FrozenFlag;
			_name = clone.Name;
			G.RequireArg(_name != null && _name.Name != "");
			var h = clone.Head;
			if (h != null && !h.IsFrozen)
				_name = null; // do not cache Name; it could change in Head
		}

		public override string ToString()
		{
			return Name.Name + (IsLiteral ? (Value ?? "null").ToString() : IsCall ? "()" : ""); // TODO
		}
		/// <summary>Compares two nodes for reference equality.</summary>
		public bool Equals(GreenNode other) { return this == other; }

		/// <inheritdoc cref="EqualsStructurally(GreenNode)"/>
		public static bool EqualsStructurally(GreenNode a, GreenNode b)
		{
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Name != b.Name)
				return false;
			if (a.ArgCount != b.ArgCount || a.AttrCount != b.AttrCount)
				return false;
			if (a.IsCall != b.IsCall)
				return false;
			for (int i = 0, c = a.ArgCount; i < c; i++)
				if (!EqualsStructurally(a.TryGetArg(i).Node, b.TryGetArg(i).Node))
					return false;
			for (int i = 0, c = a.AttrCount; i < c; i++)
				if (!EqualsStructurally(a.TryGetAttr(i).Node, b.TryGetAttr(i).Node))
					return false;
			return true;
		}
		/// <summary>Compares two nodes for structural equality. Two green nodes 
		/// are considered equal if they have the same name, the same value, the
		/// same arguments, and the same attributes. IsCall must be the same, but
		/// they need not have the same values of SourceWidth, UserByte, or 
		/// IsFrozen.</summary>
		public bool EqualsStructurally(GreenNode other)
		{
			return EqualsStructurally(this, other);
		}

		protected void SetFrozenFlag() { _stuff |= FrozenFlag; }
		public void ThrowIfFrozen()
		{
			Debug.Assert(FrozenFlag < 0);
			if (_stuff < 0)
				throw new ReadOnlyException(string.Format("The node '{0}' is frozen against modification.", ToString()));
		}

		// To be implemented by derived class.
		public abstract GreenNode Head { get; }
		public abstract GreenAndOffset HeadEx { get; set; }
		public virtual Symbol Kind { get { return IsCall ? S._CallKind : Name; } } // same as Name or #callKind if IsCall
		public abstract int ArgCount { get; }
		public abstract int AttrCount { get; }
		public abstract GreenAndOffset TryGetArg(int index);
		public abstract GreenAndOffset TryGetAttr(int index);
		public virtual void Freeze()
		{
			if (!IsFrozen)
			{
				Debug.Assert(ArgCount == 0 || Args[0].Node.IsFrozen); // derived class freezes children first
				_stuff |= FrozenFlag;
				var h = Head;
				if (h != null) {
					h.Freeze();
					_name = h.Name;
					Debug.Assert((_name ?? GSymbol.Empty).Name != "");
				} else
					Debug.Assert(_name != null);
			}
		}
		public virtual void Name_set(Symbol name) { throw new NotSupportedException(string.Format("Cannot change Name of node '{0}'", ToString())); }
		public virtual object Value
		{
			get { return NonliteralValue.Value; }
			set { throw new NotSupportedException(string.Format("Cannot change Value of node '{0}'", ToString())); }
		}
		
		// Cancelled plan to freeze lazily b/c $Cursor mode requires immediate freezing
		//protected void AutoFreezeChild(GreenNode child)
		//{
		//    child._stuff |= (_stuff & FrozenFlag);
		//}
		
		/// <summary>Produces a mutable copy of the node</summary>
		public virtual GreenNode Clone() { return new EditableGreenNode(this); }
		
		/// <summary>Produces a frozen and optimized copy of the node, or returns 
		/// the node itself if it is already frozen in its optimal form.</summary>
		/// <returns>The frozen clone.</returns>
		public virtual GreenNode CloneFrozen(bool optimizeRecursively = false) { throw new NotImplementedException(); }

		public GreenNode HeadOrThis { get { return Head ?? this; } } // this, if name is simple
		INodeReader INodeReader.Head { get { return Head; } }
		IListSource<INodeReader> INodeReader.Args { get { return Args; } }
		IListSource<INodeReader> INodeReader.Attrs { get { return Attrs; } }
		public GreenArgList Args { get { return new GreenArgList(this); } }
		public GreenAttrList Attrs { get { return new GreenAttrList(this); } }
		public Symbol Name { get { return _name ?? Head.Name; } }
		public bool IsCall {
			get { return (_stuff & IsCallFlag) != 0; }
			protected set {
				ThrowIfFrozen();
				_stuff = (_stuff & ~IsCallFlag) | (value ? IsCallFlag : 0);
			}
		}
		public bool IsLiteral { get { return _name == S._Literal; } }
		public bool IsSimpleSymbol { get { return !IsCall && _name != S._Literal; } }
		public bool IsKeyword { get { return Name.Name[0] == '#' && _name != S._Literal; } }
		public bool IsIdent { get { return Name.Name[0] != '#'; } }

		public bool IsFrozen { get { return _stuff < 0; } }
		public int SourceWidth { get { return _stuff << 9 >> 9; } } // sign-extend top 9 bits
		public bool IsSynthetic { get { return SourceWidth <= -1; } }

		/// <summary>Returns a frozen, optimized form of the node.</summary>
		/// <remarks>If the node is already in the optimal form, this method 
		/// returns this, and this node freezes if it is not frozen already.</remarks>
		//public virtual GreenNode Optimize() { Freeze(); return this; }

		/// <summary>Indicates the preferred style to use when printing the node to
		/// a text string.</summary>
		/// <remarks>
		/// The NodeStyle can be edited even when the node is frozen, but be aware
		/// that a frozen node might be re-used in different parts of a syntax tree.
		/// <para/>
		/// In rare cases, it is useful to use this byte to temporarily mark nodes 
		/// during analysis tasks. Different tasks may use the byte for different
		/// purposes; you should use this byte only if you know you are the only 
		/// thread using it and you should be aware that external code may use it,
		/// or may have already used it, for some other purpose.
		/// </remarks>
		public NodeStyle Style
		{
			get { return (NodeStyle)(_stuff >> StyleShift); }
			set { _stuff = (_stuff & ~(0xFF << StyleShift)) | ((byte)value << StyleShift); }
		}
		
		internal void ThrowNullChildError(string part)
		{
			throw new InvalidOperationException(Localize.From("An attempt was made to use a null child in '{0}' of '{1}'.", part, ToString()));
		}
		internal void ThrowCannotEditError()
		{
			Debug.Assert(IsFrozen);
			if (this is EditableGreenNode)
				throw new InvalidOperationException(Localize.From("Cannot change node '{0}' because it is frozen", ToString()));
			else
				throw new InvalidOperationException(Localize.From("Cannot change node '{0}' because its type, {1}, is not editable", ToString(), GetType().Name));
		}

		#region Methods that consider all children as a single list
		// The list claims that the number of children is ArgCount+AttrCount+1;
		// this[0] is HeadEx, this[1..ArgCount+1] is the args, rest are attrs.
		// Enumerator skips HeadEx if Head is this. Intended for internal use--
		// some range checks are omitted for performance.

		public virtual int ChildCount { get { return 1 + ArgCount + AttrCount; } }
		public virtual GreenAndOffset TryGetChild(int index)
		{
			int index2 = index - ArgCount;
			if (index == 0)
				return HeadEx;
			else if (index2 <= 0)
				return TryGetArg(index - 1);
			else
				return TryGetAttr(index2 - 1);
		}
		public virtual int IndexOf(GreenNode subject)
		{
			if (HeadEx.Node == subject)
				return 0;
			for (int i = 0, c = ArgCount; i < c; i++)
				if (TryGetArg(i).Node == subject)
					return 1 + i;
			for (int i = 0, c = AttrCount; i < c; i++)
				if (TryGetAttr(i).Node == subject)
					return 1 + ArgCount + i;
			return -1;
		}
		public virtual void SetChild(int index, GreenAndOffset newValue) { ThrowCannotEditError(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<GreenAndOffset> GetEnumerator()
		{
			for (int i = 0, c = ChildCount; i < c; i++)
				yield return TryGetChild(i);
		}
		
		#endregion
	}

	/// <summary>Suggested printing style when serializing a Loyc tree to text.</summary>
	/// <remarks>See <see cref="GreenNode.Style"/>.</remarks>
	[Flags]
	public enum NodeStyle : byte
	{
		/// <summary>No style flags are specified; the printer should choose a style automatically.</summary>
		Default = 0,
		/// <summary>The node(s) should be printed as an expression (if possible given the context in which it is located).</summary>
		Expression = 1,
		/// <summary>The node(s) should be printed as a statement (if possible given the context in which it is located).</summary>
		Statement = 2,
		/// <summary>The node(s) should be printed in prefix notation, except #. and #&lt;> nodes</summary>
		PrefixNotation = 4,
		/// <summary>The node(s) should be printed in prefix notation</summary>
		PurePrefixNotation = 5,
		/// <summary>The node and its immediate children should be on a single line.</summary>
		SingleLine = 8,
		/// <summary>Each of the node's immediate children should be on separate lines.</summary>
		MultiLine = 16,
	}

	public struct GreenAndOffset
	{
		public readonly GreenNode Node;
		public readonly int Offset;

		public GreenAndOffset(GreenNode node, int offset) { Node = node; Offset = offset; }
		public GreenAndOffset(GreenNode node) { Node = node; Offset = UnknownOffset; }

		public static implicit operator GreenNode(GreenAndOffset g) { return g.Node; }
		public bool HasOffset { get { return Offset != UnknownOffset; } }
		public const int UnknownOffset = int.MinValue;
	}

	class NonliteralValue
	{
		private NonliteralValue() { }
		public static readonly NonliteralValue Value = new NonliteralValue();
		public override string ToString() { return "#nonliteral"; }
	}

	public struct GreenArgList : IList<GreenAndOffset>, IListSource<GreenNode>
	{
		GreenNode _node;
		EditableGreenNode _eNode;
		public GreenArgList(GreenNode node) { _node = node; _eNode = null; }
		public GreenArgList(EditableGreenNode eNode) { _node = _eNode = eNode; }

		internal static void OutOfRange(GreenNode node, int index)
		{
			throw new IndexOutOfRangeException(Localize.From("Can't use Args[{0}] of '{1}', which has {2} arguments", index, node, node.ArgCount));
		}
		void AutoOutOfRange(int index)
		{
			if (index >= Count) OutOfRange(_node, index);
		}
		private void NullError(int index)
		{
			_node.ThrowNullChildError(string.Format("Args[{0}]", index));
		}
		private void CannotEditError()
		{
			_node.ThrowCannotEditError();
		}
		private void BeginEdit()
		{
			if (_node.IsFrozen) CannotEditError();
			Debug.Assert(_node is EditableGreenNode);
			_eNode = (EditableGreenNode)_node;
			Debug.Assert((uint)_eNode._argCount <= (uint)_eNode._children.Count);
		}
		internal GreenNode Node { get { return _node; } }
		public bool IsNull { get { return _node == null; } }

		#region IList<GreenAndOffset>

		public int Count 
		{
			get { return _node.ArgCount; }
		}
		public void Insert(int index, GreenAndOffset item)
		{
			if (item.Node == null) NullError(index);
			if (_eNode == null) BeginEdit();
			if ((uint)index > (uint)_eNode._argCount)
				OutOfRange(_eNode, index);
			_eNode._children.Insert(index, item);
			_eNode.IsCall = true;
			_eNode._argCount++;
		}
		public void Add(GreenAndOffset item)
		{
			Insert(Count, item);
		}
		public void RemoveAt(int index)
		{
			if (_eNode == null) BeginEdit();
			AutoOutOfRange(index);
			_eNode._children.RemoveAt(index);
			_eNode._argCount--;
		}
		public GreenAndOffset this[int index]
		{
			get { 
				var g = _node.TryGetArg(index);
				if (g.Node == null) OutOfRange(_node, index);
				return g;
			}
			set {
				if (value.Node == null) NullError(index);
				if (_eNode == null) BeginEdit();
				AutoOutOfRange(index);
				_eNode._children[index] = value;
			}
		}
		public void Clear()
		{
			if (Count > 0)
			{
				if (_eNode == null) BeginEdit();
				_eNode._children.RemoveRange(0, Count);
			}
		}

		public void AddRange(GreenArgList other)
		{
			if (other.Count != 0)
			{
				if (_eNode == null) BeginEdit();
				var c = _eNode._children;
				int offs = _eNode._argCount;
				c = new InternalList<GreenAndOffset>(InternalList.InsertRangeHelper(
					offs, other.Count, c.InternalArray, c.Count), c.Count + other.Count);
				for (int i = 0; i < other.Count; i++) {
					Debug.Assert(other[i].Node != null);
					c[offs + i] = other[i];
				}
				_eNode._children = c;
				_eNode._argCount += other.Count;
			}
		}

		#endregion

		#region Pure boilerplate

		public int IndexOf(GreenAndOffset item)
		{
			EqualityComparer<GreenAndOffset> comparer = EqualityComparer<GreenAndOffset>.Default;
			for (int i = 0; i < Count; i++)
				if (comparer.Equals(this[i], item))
					return i;
			return -1;
		}
		public bool Contains(GreenAndOffset item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(GreenAndOffset[] array, int arrayIndex)
		{
			for (int i = 0; i < Count; i++)
				array[arrayIndex++] = this[i];
		}
		public bool IsReadOnly
		{
			get { return _node.IsFrozen; }
		}
		public bool Remove(GreenAndOffset item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<GreenAndOffset> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}

		#endregion

		#region IListSource<GreenNode>

		GreenNode              IListSource<GreenNode>.this[int index] { get { return this[index].Node; } }
		public Iterator<GreenNode> GetIterator() { return GetGNEnumerator().AsIterator(); }
		IEnumerator<GreenNode> IEnumerable<GreenNode>.GetEnumerator() { return GetGNEnumerator(); }
		public GreenNode TryGet(int index, ref bool fail)
		{
			var g = _node.TryGetArg(index);
			fail = (g.Node == null);
			return g;
		}
		private IEnumerator<GreenNode> GetGNEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i].Node;
		}
		
		#endregion
	}

	public struct GreenAttrList : IList<GreenAndOffset>, IListSource<GreenNode>
	{
		GreenNode _node;
		EditableGreenNode _eNode;
		public GreenAttrList(GreenNode node) { _node = node; _eNode = null; }

		internal static void OutOfRange(GreenNode node, int index)
		{
			throw new IndexOutOfRangeException(Localize.From("Can't use Attrs[{0}] of '{1}', which has {2} arguments", index, node, node.AttrCount));
		}
		void AutoOutOfRange(int index)
		{
			if (index >= Count) OutOfRange(_node, index);
		}
		private void NullError(int index)
		{
			_node.ThrowNullChildError(string.Format("Attrs[{0}]", index));
		}
		private void CannotEditError()
		{
			_node.ThrowCannotEditError();
		}
		private void BeginEdit()
		{
			if (_node.IsFrozen) CannotEditError();
			Debug.Assert(_node is EditableGreenNode);
			_eNode = (EditableGreenNode)_node;
			Debug.Assert((uint)_eNode._argCount <= (uint)_eNode._children.Count);
		}
		internal GreenNode Node { get { return _node; } }
		public bool IsNull { get { return _node == null; } }

		#region IList<GreenAndOffset>

		public int Count 
		{
			get { return _node.AttrCount; }
		}
		public void Insert(int index, GreenAndOffset item)
		{
			if (item.Node == null) NullError(index);
			if (_eNode == null) BeginEdit();
			if ((uint)index > (uint)_eNode.AttrCount)
				OutOfRange(_eNode, index);
			_eNode._children.Insert(_eNode._argCount + index, item);
			_eNode.IsCall = true;
		}
		public void Add(GreenAndOffset item)
		{
			Insert(Count, item);
		}
		public void RemoveAt(int index)
		{
			if (_eNode == null) BeginEdit();
			AutoOutOfRange(index);
			_eNode._children.RemoveAt(_eNode._argCount + index);
		}
		public GreenAndOffset this[int index]
		{
			get { 
				var g = _node.TryGetAttr(index);
				if (g.Node == null) OutOfRange(_node, index);
				return g;
			}
			set {
				if (value.Node == null) NullError(index);
				if (_eNode == null) BeginEdit();
				AutoOutOfRange(index);
				_eNode._children[_eNode._argCount + index] = value;
			}
		}
		public void Clear()
		{
			if (Count > 0)
			{
				if (_eNode == null) BeginEdit();
				_eNode._children.RemoveRange(_eNode._argCount, Count);
			}
		}

		public void AddRange(GreenAttrList other)
		{
			if (other.Count != 0)
			{
				if (_eNode == null) BeginEdit();
				var c = _eNode._children;
				int offs = c.Count;
				c = new InternalList<GreenAndOffset>(InternalList.InsertRangeHelper(
					offs, other.Count, c.InternalArray, c.Count), c.Count + other.Count);
				for (int i = 0; i < other.Count; i++) {
					Debug.Assert(other[i].Node != null);
					c[offs + i] = other[i];
				}
				_eNode._children = c;
			}
		}

		#endregion

		#region Pure boilerplate

		public int IndexOf(GreenAndOffset item)
		{
			EqualityComparer<GreenAndOffset> comparer = EqualityComparer<GreenAndOffset>.Default;
			for (int i = 0; i < Count; i++)
				if (comparer.Equals(this[i], item))
					return i;
			return -1;
		}
		public bool Contains(GreenAndOffset item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(GreenAndOffset[] array, int arrayIndex)
		{
			for (int i = 0; i < Count; i++)
				array[arrayIndex++] = this[i];
		}
		public bool IsReadOnly
		{
			get { return _node.IsFrozen; }
		}
		public bool Remove(GreenAndOffset item)
		{
			int i = IndexOf(item);
			if (i == -1)
				return false;
			RemoveAt(i);
			return true;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<GreenAndOffset> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}

		#endregion

		#region IListSource<GreenNode>

		GreenNode              IListSource<GreenNode>.this[int index] { get { return this[index].Node; } }
		public Iterator<GreenNode> GetIterator() { return GetGNEnumerator().AsIterator(); }
		IEnumerator<GreenNode> IEnumerable<GreenNode>.GetEnumerator() { return GetGNEnumerator(); }
		public GreenNode TryGet(int index, ref bool fail)
		{
			var g = _node.TryGetAttr(index);
			fail = (g.Node == null);
			return g;
		}
		private IEnumerator<GreenNode> GetGNEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i].Node;
		}
		
		#endregion
	}
}
