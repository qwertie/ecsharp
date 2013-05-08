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
	using S = ecs.CodeSymbols;
	using Loyc.Math;

	/// <summary>
	/// Represents a green node, which is a low-level node class typically used by
	/// parsers. See documentation of <see cref="INodeReader"/> for more 
	/// information.
	/// </summary>
	[DebuggerDisplay("{Print()}")]
	public abstract class GreenNode : INodeReader, IEquatable<GreenNode>, IEnumerable<GreenAtOffs>
	{
		// If Head != this and Head is mutable, this is _null to force lookup in Head, because the name can change
		protected Symbol _name;

		private int _stuff;
		const int FrozenFlag = unchecked((int)0x80000000);
		const int IsCallFlag = unchecked((int)0x40000000);
		const int StyleShift = 22;
		const int SourceWidthMask = (1 << StyleShift) - 1;

		protected ISourceFile _sourceFile;

		#region New() and constructors

		public static GreenNode New(Symbol name, ISourceFile sourceFile, int sourceWidth = -1)
		{
			return new EditableGreenNode(name, sourceFile, sourceWidth);
		}
		public static GreenNode New(GreenAtOffs head, ISourceFile sourceFile, int sourceWidth = -1)
		{
			return new EditableGreenNode(head, sourceFile, sourceWidth);
		}

		protected GreenNode(Symbol name, ISourceFile sourceFile, int sourceWidth, bool isCall, bool freeze)
		{
			G.RequireArg(name != null && name.Name != "");
			_name = name;
			_stuff = System.Math.Min(sourceWidth, SourceWidthMask >> 1) & SourceWidthMask;
			_sourceFile = sourceFile;
			if (isCall) _stuff |= IsCallFlag;
			if (freeze)
			{
				_stuff |= FrozenFlag;
				G.Require(sourceFile != null);
			}
		}
		protected GreenNode(GreenNode head, ISourceFile sourceFile, int sourceWidth, bool isCall, bool freeze) 
			: this(head.Name, sourceFile, sourceWidth, isCall, freeze)
		{
			Debug.Assert(head != this && head != null);
			Debug.Assert((head.Name.Name ?? "") != "");
			if (!head.IsFrozen)
			{
				if (freeze)
					head.Freeze();
				else
					_name = null; // do not cache Name; it could change in Head
			}
		}
		protected GreenNode(GreenNode clone)
		{
			_stuff = clone._stuff & ~FrozenFlag;
			_name = clone.Name;
			_sourceFile = clone._sourceFile;
			G.RequireArg(_name != null && _name.Name != "");
			var h = clone.Head;
			if (h != null && !h.IsFrozen)
				_name = null; // do not cache Name; it could change in Head
		}
		
		#endregion
		
		public override string ToString()
		{
			return Print();
			//var head = Head == null ? Name.Name : Head.Print(NodeStyle.Expression);
			//return head + (IsLiteral ? " " + (Value ?? "null").ToString() : IsCall ? "()" : "");
		}

		/// <summary>Uses <see cref="NodePrinter.Print"/> to print the node as text.</summary>
		public string Print(NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n")
		{
			return NodePrinter.Print(this, style, indentString, lineSeparator).ToString();
		}

		/// <summary>Compares two nodes for reference equality.</summary>
		public bool Equals(GreenNode other) { return this == other; }

		/// <inheritdoc cref="EqualsStructurally(GreenNode)"/>
		public static bool EqualsStructurally(GreenNode a, GreenNode b, bool compareStyles = false)
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
			if (!object.Equals(a.Value, b.Value))
				return false;
			if (compareStyles && a.Style != b.Style)
				return false;
			GreenNode ha = a.Head, hb = b.Head;
			if (ha != hb) {
				ha = ha ?? a;
				hb = hb ?? b;
				if (!EqualsStructurally(ha, hb, compareStyles))
					return false;
			}
			for (int i = 0, c = a.ArgCount; i < c; i++)
				if (!EqualsStructurally(a.TryGetArg(i).Node, b.TryGetArg(i).Node, compareStyles))
					return false;
			for (int i = 0, c = a.AttrCount; i < c; i++)
				if (!EqualsStructurally(a.TryGetAttr(i).Node, b.TryGetAttr(i).Node, compareStyles))
					return false;
			return true;
		}
		/// <summary>Compares two nodes for structural equality. Two green nodes 
		/// are considered equal if they have the same name, the same value, the
		/// same arguments, and the same attributes. IsCall must be the same, but
		/// they need not have the same values of SourceWidth or IsFrozen.</summary>
		/// <param name="compareStyles">Whether to compare values of <see cref="Style"/></param>
		public bool EqualsStructurally(GreenNode other, bool compareStyles = false)
		{
			return EqualsStructurally(this, other, compareStyles);
		}
		bool INodeReader.EqualsStructurally(INodeReader other, bool compareStyles = false)
		{
			return EqualsStructurally(this, (GreenNode)other, compareStyles);
		}
		/// <summary>A comparer that produces equal for two nodes with that compare 
		/// equal with EqualsStructurally(). If the tree is large, less than the
		/// entire tree is scanned to produce the hashcode (in the absolute worst 
		/// case, about 4000 nodes are examined).</summary>
		public class DeepComparer : IEqualityComparer<GreenNode>
		{
			public static readonly DeepComparer Value = new DeepComparer(false);
			public static readonly DeepComparer WithStyleCompare = new DeepComparer(true);
			
			bool _compareStyles;
			public DeepComparer(bool compareStyles) { _compareStyles = compareStyles; }

			public bool Equals(GreenNode x, GreenNode y)
			{
				return EqualsStructurally(x, y);
			}
			public int GetHashCode(GreenNode node)
			{
				return GetHashCode(node, 3, _compareStyles ? 0x7F : 0);
			}
			static int GetHashCode(GreenNode node, int recurse, int compareStylesMask)
			{
				int hash = node.ChildCount + 1 + ((int)node.Style & compareStylesMask);
				if (node.IsCall)
					hash <<= 1;
				var h = node.Head;
				if (h != null && recurse > 0)
					hash += GetHashCode(node, recurse - 1, compareStylesMask);
				else
					hash += node.Name.GetHashCode();
				var value = node.Value;
				if (value != NonliteralValue.Value && value != null)
					hash += value.GetHashCode();
				
				if (recurse > 0) {
					for (int i = 0, c = System.Math.Min(node.AttrCount, recurse << 2); i < c; i++)
						hash = (hash * 65599) + GetHashCode(node.TryGetAttr(i).Node, recurse-1, compareStylesMask);
					for (int i = 0, c = System.Math.Min(node.ArgCount, recurse << 2); i < c; i++)
						hash = (hash * 4129) + GetHashCode(node.TryGetArg(i).Node, recurse-1, compareStylesMask);
				}
				return hash;
			}
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
		public abstract GreenAtOffs HeadEx { get; set; }
		public virtual Symbol Kind { get { return IsCall ? S.CallKind : Name; } } // same as Name or #callKind if IsCall
		public abstract int ArgCount { get; }
		public abstract int AttrCount { get; }
		public abstract GreenAtOffs TryGetArg(int index);
		public abstract GreenAtOffs TryGetAttr(int index);
		public virtual void Freeze()
		{
			if (!IsFrozen)
			{
				G.Require(_sourceFile != null); // must set source file before freezing
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
		public GreenNode Clone() { return new EditableGreenNode(this); }
		
		/// <summary>Clones the node only if it is frozen; returns 'this' otherwise.</summary>
		public GreenNode Unfrozen() { return IsFrozen ? Clone() : this; }

		/// <summary>Optimizes the children of the current mutable node or produces 
		/// an optimized, mutable or frozen clone if the node or tree can be 
		/// simplified by cloning. Children can be optimized recursively.</summary>
		/// <remarks>
		/// Optimization is a bit gimpy: it stops at frozen, non-optimizable nodes.
		/// recurseEditable recurses on otherwise-non-optimizable EditableNodes; 
		/// it can take O(N) time so take care not to optimize repeatedly.
		/// <para/>
		/// Depending on circumstances, the returned node may be frozen or 
		/// unfrozen regardless of whether the input was frozen or not.
		/// </remarks>
		public virtual GreenNode AutoOptimize(bool useCache, bool recurseEditable = false) {
			return useCache ? GreenFactory.Cache(this) : this;
		}
		
		/// <summary>Produces a frozen and optimized copy of the node, or returns 
		/// the node itself if it is already frozen in its optimal form.</summary>
		/// <returns>A frozen clone.</returns>
		public GreenNode CloneFrozen()
		{
			var opt = AutoOptimize(true, false);
			if (opt != this || IsFrozen)
				return opt;
			var clone = Clone();
			clone.Freeze();
			return clone;
		}

		public GreenNode HeadOrThis { get { return Head ?? this; } } // this, if head is trivial
		INodeReader INodeReader.Head { get { return Head; } }
		IListSource<INodeReader> INodeReader.Args { get { return Args; } }
		IListSource<INodeReader> INodeReader.Attrs { get { return Attrs; } }
		INodeReader INodeReader.TryGetArg(int i) { return TryGetArg(i).Node; }
		INodeReader INodeReader.TryGetAttr(int i) { return TryGetAttr(i).Node; }
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
		public bool HasSimpleHead
		{
			get {
				var h = Head; 
				return h == null || (h.Head == null && !h.IsCall);
			}
		}
		public void RemoveArgList()
		{
			Args.Clear();
			IsCall = false;
		}
		public bool IsLiteral { get { return _name == S.Literal; } }
		public bool IsSimpleSymbol { get { return !IsCall && !IsLiteral && Head == null; } }
		public bool IsKeyword { get { return Name.Name[0] == '#' && _name != S.Literal; } }
		public bool IsIdent { get { return Name.Name[0] != '#'; } }
		public bool IsFrozen { get { return _stuff < 0; } }
		public bool IsSynthetic { get { return SourceWidth <= -1; } }
		public int SourceWidth { 
			get { 
				const int SWBits = 32-StyleShift;
				return _stuff << SWBits >> SWBits; // sign-extend top 9 bits
			}
			set {
				ThrowIfFrozen();
				const int Max = (1 << (StyleShift-1))-1, Min = ~Max;
				int value2 = MathEx.InRange(value, Min, Max);
				_stuff = (_stuff & (-1 << StyleShift)) | (value2 & ((1 << StyleShift) - 1));
			}
		} 
		public string SourceFileName { get { return _sourceFile.FileName; } }
		public ISourceFile SourceFile
		{
			get { return _sourceFile; }
			set { ThrowIfFrozen(); _sourceFile = value; }
		}

		/// <summary>Returns a frozen, optimized form of the node.</summary>
		/// <remarks>If the node is already in the optimal form, this method 
		/// returns this, and this node freezes if it is not frozen already.</remarks>
		//public virtual GreenNode Optimize() { Freeze(); return this; }

		/// <summary>Indicates the preferred style to use when printing the node to
		/// a text string.</summary>
		/// <remarks>
		/// Not editable when frozen.
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
			set {
				ThrowIfFrozen();
				_stuff = (_stuff & ~(0xFF << StyleShift)) | ((byte)value << StyleShift);
			}
		}
		public NodeStyle BaseStyle
		{
			get { return (NodeStyle)((_stuff >> StyleShift) & (int)NodeStyle.BaseStyleMask); }
			set {
				ThrowIfFrozen();
				_stuff = (_stuff & ~((int)NodeStyle.BaseStyleMask << StyleShift)) 
				       | ((int)value & (int)NodeStyle.BaseStyleMask) << StyleShift;
			}
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
		// The Head is included even if it is null. Intended for internal use--
		// some range checks are omitted for performance.

		public virtual int ChildCount { get { return 1 + ArgCount + AttrCount; } }
		public virtual GreenAtOffs TryGetChild(int index)
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
		public virtual void SetChild(int index, GreenAtOffs newValue) { ThrowCannotEditError(); }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<GreenAtOffs> GetEnumerator()
		{
			for (int i = 0, c = ChildCount; i < c; i++)
				yield return TryGetChild(i);
		}
		
		#endregion
	}

	/// <summary>Suggests a printing style when serializing a Loyc tree to text.</summary>
	/// <remarks>See <see cref="GreenNode.Style"/>.
	/// <para/>
	/// A printer should not throw exceptions unless specifically requested. It 
	/// should ignore printing styles that it does not allow, rather than throwing.
	/// <para/>
	/// Please note that language-specific printing styles can be denoted by 
	/// attaching special attributes recognized by the printer for that language,
	/// e.g. the #macroAsAttribute attribute causes a statement like 
	/// <c>foo(int x = 2);</c> to be printed as <c>\[foo] int x = 2;</c>.</remarks>
	[Flags]
	public enum NodeStyle : byte
	{
		/// <summary>No style flags are specified; the printer should choose a 
		/// style automatically.</summary>
		Default = 0,
		/// <summary>The node(s) should be printed as an expression, if possible 
		/// given the context in which it is located (in EC# it is almost always 
		/// possible to print something as an expression).</summary>
		Expression = 1,
		/// <summary>The node(s) should be printed as a statement, if possible 
		/// given the context in which it is located (for example, EC# can only 
		/// switch to statement mode at certain node types such as # and #quote.)</summary>
		Statement = 2,
		/// <summary>The node(s) should be printed with infix or suffix notation
		/// instead of prefix notation if applicable (uses `backquote notation` 
		/// in EC#).</summary>
		Operator = 3,
		/// <summary>The node(s) should be printed in prefix notation, except 
		/// complex identifiers that use #. and #of nodes, which are printed in 
		/// EC# style e.g. Generic.List&ltint>.</summary>
		PrefixNotation = 4,
		/// <summary>The node(s) should be printed in prefix notation only.</summary>
		PurePrefixNotation = 5,
		/// <summary>If s is a NodeStyle, (s & NodeStyle.BaseStyleMask) gets the 
		/// base style (Default, Expression, Statement, Tokens, PrefixNotation,
		/// or PurePrefixNotation).</summary>
		/// <summary>The node(s) should be printed as a token list (if possible 
		/// given its Name and contents); this applies only to @[...], @@[...] and 
		/// #[...] nodes in EC# (types #quote, #quoteSubstituting and #). This
		/// mode always applies recursively, and it is ignored if the node contains 
		/// anything that is not valid inside a list of EC# tokens (except that 
		/// @@[...] has the special ability to switch back to "normal code" via
		/// the substitution operator '\', named #\).</summary>
		Tokens = 6,
		BaseStyleMask = 7,
		
		/// <summary>If this node has two common styles in which it is printed, this
		/// selects the second (either the less common style, or the EC# style for
		/// features of C# with new syntax in EC#). In EC#, alternate style denotes 
		/// verbatim strings, hex numbers, x(->int) as opposed to (int)x, x (as Y)
		/// as opposed to (x as Y). delegate(X) {Y;} is considered to be the 
		/// alternate style for X => Y, and it forces parens and braces as a side-
		/// effect.</summary>
		Alternate = 8,

		// *******************************************************************
		// **** The following are not yet supported or may be redesigned. ****
		// *******************************************************************

		/// <summary>The node and its immediate children should be on a single line.</summary>
		SingleLine = 16,
		/// <summary>Each of the node's immediate children should be on separate lines.</summary>
		MultiLine = 32,
		/// <summary>Applies the NodeStyle to children recursively, except on 
		/// children that also have this flag.</summary>
		Recursive = 64,
		/// <summary>User-defined meaning.</summary>
		UserFlag = 128,
	}

	public struct GreenAtOffs
	{
		public readonly GreenNode Node;
		public readonly int Offset;

		public GreenAtOffs(GreenNode node, int parentIndex, int childIndex) { Node = node; Offset = childIndex - parentIndex; }
		public GreenAtOffs(GreenNode node, int offset) { Node = node; Offset = offset; }
		public GreenAtOffs(GreenNode node) { Node = node; Offset = UnknownOffset; }
		public static implicit operator GreenNode(GreenAtOffs g) { return g.Node; }
		public static implicit operator GreenAtOffs(GreenNode n) { return new GreenAtOffs(n, UnknownOffset); }
		public bool HasOffset { get { return Offset != UnknownOffset; } }
		public const int UnknownOffset = int.MinValue;
		public GreenAtOffs AutoOptimize(bool useCache, bool recursiveEditable)
		{
			return new GreenAtOffs(Node.AutoOptimize(useCache, recursiveEditable), Offset);
		}
		public int GetSourceIndex(int parentIndex)
		{
			return HasOffset && parentIndex > -1 ? parentIndex + Offset : -1;
		}
	}

	public class NonliteralValue
	{
		private NonliteralValue() { }
		public static readonly NonliteralValue Value = new NonliteralValue();
		public override string ToString() { return "#nonliteral"; }
	}

	public struct GreenArgList : IList<GreenAtOffs>, IListSource<GreenNode>
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
		public void Insert(int index, GreenAtOffs item)
		{
			if (item.Node == null) NullError(index);
			if (_eNode == null) BeginEdit();
			if ((uint)index > (uint)_eNode._argCount)
				OutOfRange(_eNode, index);
			_eNode._children.Insert(index, item);
			_eNode.IsCall = true;
			_eNode._argCount++;
		}
		public void Add(GreenAtOffs item)
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
		public GreenAtOffs this[int index]
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
		/// <summary>Workaround for the error "Cannot modify the return value of 
		/// 'Loyc.CompilerCore.GreenNode.Args' because it is not a variable",
		/// which occurs when you change n.Args[index] directly on some node 'n'.
		/// </summary><remarks>
		/// The error occurs because the C# compiler is unaware that the 
		/// setter's purpose is to modify GreenNode rather than the struct itself. 
		/// That said, if you want to modify multiple children of a node, it is 
		/// more efficient to store GreenArgList in a variable and re-use it.
		/// Perhaps EC# could have a Mutates(true|false) attribute to tell the 
		/// compiler which method calls to allow on a struct.</remarks>
		public void Set(int index, GreenAtOffs value)
		{
			this[index] = value;
		}
		
		/// <summary>Clears the argument list but does not remove it entirely. If 
		/// you also want to clear IsCall to false, use <see cref="GreenNode.RemoveArgList"/>.</summary>
		public void Clear()
		{
			if (Count > 0)
				RemoveRange(0, Count);
		}
		public void RemoveRange(int index, int amount)
		{
			if ((uint)index > (uint)Count) OutOfRange(_node, index);
			if (amount > 0) {
				if ((uint)(index + amount) > (uint)Count)
					throw new ArgumentOutOfRangeException("amount");
				if (_eNode == null) BeginEdit();
				_eNode._children.RemoveRange(index, amount);
				_eNode._argCount -= amount;
			} else if (amount < 0)
				throw new ArgumentOutOfRangeException("amount");
		}

		public void AddRange(GreenArgList other)
		{
			InsertRangeCore(Count, other);
		}
		public void InsertRange(int index, GreenArgList other)
		{
			if ((uint)index > (uint)Count) OutOfRange(_node, index);
			InsertRangeCore(index, other);
		}
		public void InsertRangeCore(int index, GreenArgList other)
		{
			if (other.Count != 0)
			{
				if (_eNode == null) BeginEdit();
				var c = _eNode._children;
				c = new InternalList<GreenAtOffs>(InternalList.InsertRangeHelper(
					index, other.Count, c.InternalArray, c.Count), c.Count + other.Count);
				for (int i = 0; i < other.Count; i++) {
					Debug.Assert(other[i].Node != null);
					c[index + i] = other[i];
				}
				_eNode._children = c;
				_eNode._argCount += other.Count;
				_eNode.IsCall = true;
			}
		}

		#endregion

		#region Pure boilerplate

		public int IndexOf(GreenAtOffs item)
		{
			EqualityComparer<GreenAtOffs> comparer = EqualityComparer<GreenAtOffs>.Default;
			for (int i = 0; i < Count; i++)
				if (comparer.Equals(this[i], item))
					return i;
			return -1;
		}
		public bool Contains(GreenAtOffs item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(GreenAtOffs[] array, int arrayIndex)
		{
			for (int i = 0; i < Count; i++)
				array[arrayIndex++] = this[i];
		}
		public bool IsReadOnly
		{
			get { return _node.IsFrozen; }
		}
		public bool Remove(GreenAtOffs item)
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
		public IEnumerator<GreenAtOffs> GetEnumerator()
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

	public struct GreenAttrList : IList<GreenAtOffs>, IListSource<GreenNode>
	{
		GreenNode _node;
		EditableGreenNode _eNode;
		public GreenAttrList(GreenNode node) { _node = node; _eNode = null; }

		internal static void OutOfRange(GreenNode node, int index)
		{
			throw new IndexOutOfRangeException(Localize.From("Can't use Attrs[{0}] of '{1}', which has {2} attributes", index, node, node.AttrCount));
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
		public void Insert(int index, GreenAtOffs item)
		{
			if (item.Node == null) NullError(index);
			if (_eNode == null) BeginEdit();
			if ((uint)index > (uint)_eNode.AttrCount)
				OutOfRange(_eNode, index);
			_eNode._children.Insert(_eNode._argCount + index, item);
		}
		public void Add(GreenAtOffs item)
		{
			Insert(Count, item);
		}
		public void RemoveAt(int index)
		{
			if (_eNode == null) BeginEdit();
			AutoOutOfRange(index);
			_eNode._children.RemoveAt(_eNode._argCount + index);
		}
		public GreenAtOffs this[int index]
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
		/// <summary>Workaround for the error "Cannot modify the return value of 'Loyc.CompilerCore.GreenNode.Attrs' because it is not a variable"</summary>
		public void Set(int index, GreenAtOffs value)
		{
			this[index] = value;
		}
		public void Clear()
		{
			if (Count > 0)
				RemoveRange(0, Count);
		}
		public void RemoveRange(int index, int amount)
		{
			if ((uint)index > (uint)Count) OutOfRange(_node, index);
			if (amount > 0) {
				if ((uint)(index + amount) > (uint)Count)
					throw new ArgumentOutOfRangeException("amount");
				if (_eNode == null) BeginEdit();
				_eNode._children.RemoveRange(index + _eNode._argCount, amount);
			} else if (amount < 0)
				throw new ArgumentOutOfRangeException("amount");
		}
		public void AddRange(GreenAttrList other)
		{
			InsertRangeCore(Count, other);
		}
		public void InsertRange(int index, GreenAttrList other)
		{
			if ((uint)index > (uint)Count) OutOfRange(_node, index);
			InsertRangeCore(index, other);
		}
		public void InsertRangeCore(int index, GreenAttrList other)
		{
			if (other.Count != 0)
			{
				if (_eNode == null) BeginEdit();
				var c = _eNode._children;
				index += _eNode._argCount;
				c = new InternalList<GreenAtOffs>(InternalList.InsertRangeHelper(
					index, other.Count, c.InternalArray, c.Count), c.Count + other.Count);
				for (int i = 0; i < other.Count; i++) {
					Debug.Assert(other[i].Node != null);
					c[index + i] = other[i];
				}
				_eNode._children = c;
			}
		}

		#endregion

		#region Pure boilerplate

		public int IndexOf(GreenAtOffs item)
		{
			EqualityComparer<GreenAtOffs> comparer = EqualityComparer<GreenAtOffs>.Default;
			for (int i = 0; i < Count; i++)
				if (comparer.Equals(this[i], item))
					return i;
			return -1;
		}
		public bool Contains(GreenAtOffs item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(GreenAtOffs[] array, int arrayIndex)
		{
			for (int i = 0; i < Count; i++)
				array[arrayIndex++] = this[i];
		}
		public bool IsReadOnly
		{
			get { return _node.IsFrozen; }
		}
		public bool Remove(GreenAtOffs item)
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
		public IEnumerator<GreenAtOffs> GetEnumerator()
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
