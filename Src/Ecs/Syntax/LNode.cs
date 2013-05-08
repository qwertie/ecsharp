using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Collections;
using System.Diagnostics;
using System.ComponentModel;

namespace Loyc.Syntax
{
	/// <summary>A node in a Loyc tree.</summary>
	/// <remarks>
	/// Loyc nodes are typically immutable, except for the 8-bit <see cref="Style"/> 
	/// property which normally affects printing only. If a node allows editing of 
	/// any other properties, <see cref="Frozen"/> returns false.
	/// <para/>
	/// This is the second iteration of the Loyc syntax tree. The core concept is
	/// the same as described in my blog at
	/// http://loyc-etc.blogspot.ca/2013/04/the-loyc-tree-and-prefix-notation-in-ec.html
	/// but "red" and "green" nodes have basically been eliminated, at least for 
	/// now, and nodes normally do not contain parent references anymore.
	/// </remarks>
	public abstract class LNode : ILNode, ICloneable<LNode>
	{
		#region Constructors and static node creator methods

		protected LNode(LNode prototype)
		{
			RAS = prototype.RAS;
		}
		protected LNode(SourceRange range, NodeStyle style)
		{
			RAS = new RangeAndStyle(range, style);
		}

		public static SymbolNode Symbol(Symbol name, SourceRange range) { return new StdSymbolNode(name, range); }
		public static SymbolNode Symbol(string name, SourceRange range) { return new StdSymbolNode(GSymbol.Get(name), range); }
		public static SymbolNode Symbol(RVList<LNode> attrs, Symbol name, SourceRange range) { return new StdSymbolNodeWithAttrs(attrs, name, range); }
		public static SymbolNode Symbol(RVList<LNode> attrs, string name, SourceRange range) { return new StdSymbolNodeWithAttrs(attrs, GSymbol.Get(name), range); }
		public static StdLiteralNode Literal(object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, range, style); }
		public static StdLiteralNode Literal(RVList<LNode> attrs, object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, range, style); }
		public static StdCallNode Call(Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, range, style); }
		public static StdCallNode Call(LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, range, style); }
		public static StdCallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new  StdSimpleCallNodeWithAttrs(attrs, name, args, range, style); }
		public static StdCallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, range, style); }
		public static StdCallNode InParens(LNode node, SourceRange range) { return new StdComplexCallNode(null, new RVList<LNode>(node), range); }
		public static StdCallNode InParens(RVList<LNode> attrs, LNode node, SourceRange range) { return new StdComplexCallNodeWithAttrs(attrs, null, new RVList<LNode>(node), range); }

		public static SymbolNode Symbol(Symbol name, ISourceFile file, int position = -1, int width = -1) { return new StdSymbolNode(name, new SourceRange(file, position, width)); }
		public static SymbolNode Symbol(string name, ISourceFile file, int position = -1, int width = -1) { return new StdSymbolNode(GSymbol.Get(name), new SourceRange(file, position, width)); }
		public static SymbolNode Symbol(RVList<LNode> attrs, Symbol name, ISourceFile file, int position = -1, int width = -1) { return new StdSymbolNodeWithAttrs(attrs, name, new SourceRange(file, position, width)); }
		public static SymbolNode Symbol(RVList<LNode> attrs, string name, ISourceFile file, int position = -1, int width = -1) { return new StdSymbolNodeWithAttrs(attrs, GSymbol.Get(name), new SourceRange(file, position, width)); }
		public static StdLiteralNode Literal(object value, ISourceFile file, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file, position, width), style); }
		public static StdLiteralNode Literal(RVList<LNode> attrs, object value, ISourceFile file, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(Symbol name, RVList<LNode> args, ISourceFile file, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(LNode target, RVList<LNode> args, ISourceFile file, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, ISourceFile file, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new  StdSimpleCallNodeWithAttrs(attrs, name, args, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, ISourceFile file, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, new SourceRange(file, position, width), style); }
		public static StdCallNode InParens(LNode node, ISourceFile file, int position = -1, int width = -1) { return new StdComplexCallNode(null, new RVList<LNode>(node), new SourceRange(file, position, width)); }
		public static StdCallNode InParens(RVList<LNode> attrs, LNode node, ISourceFile file, int position = -1, int width = -1) { return new StdComplexCallNodeWithAttrs(attrs, null, new RVList<LNode>(node), new SourceRange(file, position, width)); }

		#endregion

		#region Fields

		protected RangeAndStyle RAS;

		protected internal struct RangeAndStyle
		{
			public RangeAndStyle(SourceRange range, NodeStyle style)
			{
				Source = range.Source;
				BeginIndex = range.BeginIndex;
				_stuff = (range.Length & LengthMask) | ((int)style << StyleShift);
			}
			public RangeAndStyle(ISourceFile source, int beginIndex, int length, NodeStyle style)
			{
				Source = source;
				BeginIndex = beginIndex;
				_stuff = (length & LengthMask) | ((int)style << StyleShift);
			}

			public ISourceFile Source;
			public int BeginIndex;
			private int _stuff;

			const int StyleShift = 23;
			const int NonWidthBits = 32 - StyleShift;
			const int LengthMask = (1 << StyleShift) - 1;
			const int MutableFlag = unchecked((int)0x80000000);

			public int Length { 
				[DebuggerStepThrough] get { return _stuff & LengthMask; }
				[DebuggerStepThrough] set { _stuff = (_stuff & ~LengthMask) | value; }
			}
			public NodeStyle Style {
				[DebuggerStepThrough] get { return (NodeStyle)(_stuff >> StyleShift); }
				[DebuggerStepThrough] set { _stuff = (_stuff & ~(0xFF << StyleShift)) | ((int)value << StyleShift); }
			}
			public bool IsMutable { get { return (_stuff & MutableFlag) != 0; } }
			public void MarkFrozen() { _stuff &= ~MutableFlag; }
			public void MarkMutable() { _stuff |= MutableFlag; }

			public static explicit operator SourceRange(RangeAndStyle ras) { return new SourceRange(ras.Source, ras.BeginIndex, ras.Length); }
		}

		#endregion

		#region Common to all nodes

		/// <summary>Returns the location and range in source code of this node.</summary>
		/// <remarks>
		/// A parser should record a sufficiently wide range for each parent node, 
		/// such that all children are fully contained within the range. However, 
		/// this is not an invariant; macros can splice together syntax trees from 
		/// different source files or add synthetic nodes, so that the parent range
		/// does not necessarily include all child ranges. (In fact, in general it 
		/// is impossible to ensure that parent ranges include child ranges because
		/// a parent can only specify a single source file, while children can come
		/// from several source files.)
		/// </remarks>
		public virtual SourceRange Range { get { return (SourceRange)RAS; } }
		/// <summary>Returns the source file (shortcut for <c><see cref="Range"/>.Source</c>).</summary>
		public ISourceFile Source { get { return RAS.Source; } }

		/// <summary>Indicates the preferred style to use when printing the node to a text string.</summary>
		public NodeStyle Style
		{
			get { return RAS.Style; }
			set { RAS.Style = value; }
		}
		public NodeStyle BaseStyle { get { return RAS.Style & NodeStyle.BaseStyleMask; } }

		/// <summary>Returns the attribute list for this node.</summary>
		public virtual RVList<LNode> Attrs { get { return RVList<LNode>.Empty; } }

		/// <summary>Returns true if the node is immutable, and false if any part of it can be edited.</summary>
		public virtual bool IsFrozen { get { return true; } }
		
		/// <summary>Returns the <see cref="NodeType"/>: Symbol, Literal, or Call.</summary>
		public abstract NodeType Type { get; }
		public bool IsCall { get { return Type == NodeType.Call; } }
		public bool IsSymbol { get { return Type == NodeType.Symbol; } }
		public bool IsLiteral { get { return Type == NodeType.Literal; } }

		#endregion

		#region Properties and methods for Symbol nodes (and simple calls)

		/// <summary>Returns the Symbol if <see cref="IsSymbol"/>. If this node is 
		/// a call (<see cref="IsCall"/>) and <c>Target.IsSymbol</c> is true, 
		/// this property returns <c>Target.Name</c>. In all other cases, the name
		/// is <see cref="GSymbol.Empty"/>. Shall not return null.</summary>
		public abstract Symbol Name { get; }

		/// <summary>Returns true if <see cref="Name"/> starts with '#'.</summary>
		public bool IsSpecialName { get { string n = Name.Name; return n.Length > 0 && n[0] == '#'; } }

		/// <summary>Creates a node with a new name. If <see cref="IsCall"/>, this 
		/// method returns <c>WithTarget(Target.WithName(name))</c>; however, this
		/// call may throw an exception, so if you already know that this Node is a
		/// call, you should call <see cref="WithTarget(Symbol)"/> instead.</summary>
		/// <exception cref="InvalidOperationException">This node does not have a
		/// Name, so Name cannot be changed.</exception>
		public abstract LNode WithName(Symbol name);


		#endregion

		#region Properties and methods for Literal nodes

		/// <summary>Returns the value of a literal node, or null if this node is 
		/// not a literal (<see cref="IsLiteral"/> is false).</summary>
		public abstract object Value { get; }

		public abstract LiteralNode WithValue(object value);

		#endregion

		#region Properties and methods for Call nodes

		/// <summary>Returns the target of a method call, or null if <see cref="IsCall"/> 
		/// is false. This can also be null if <see cref="IsCall"/> is true; this
		/// case represents a parenthesized expression if there is one argument.</summary>
		/// <remarks>
		/// EC# has no representation for the case that Target==null and there is
		/// more than one argument. The node printer will print that case as a 
		/// tuple, as if the Target were #tuple, but it is not round-trippable.
		/// </remarks>
		public abstract LNode Target { get; }

		/// <summary>Returns the argument list of this node. Always empty when <c><see cref="IsCall"/>==false</c>.</summary>
		/// <remarks>
		/// Depending on the <see cref="Target"/>, Args may represent an actual 
		/// argument list, or it may represent some other kind of list. For 
		/// example, if the target is #{} then Args represents a list of 
		/// statements in a braced block, and if the target is #>= then Args 
		/// represents the two arguments to the ">=" operator.
		/// </remarks>
		public abstract RVList<LNode> Args { get; }

		public virtual CallNode WithTarget(LNode target)                { return With(target, Args); }
		public virtual CallNode WithTarget(Symbol name)                 { return With(name, Args); }
		public abstract CallNode With(LNode target, RVList<LNode> args);
		public abstract CallNode With(Symbol target, RVList<LNode> args);

		/// <summary>Creates a Node with a new argument list. If this node is not a 
		/// call, a new node is created using this node as its target. Otherwise,
		/// the existing argument list is replaced.</summary>
		/// <param name="args">New argument list</param>
		public abstract CallNode WithArgs(RVList<LNode> args);

		#endregion

		#region Other WithXyz methods, and Clone()

		/// <summary>Creates a copy of the node. Since nodes are immutable, there 
		/// is little reason for an end-user to call this, but Clone() is used 
		/// internally as a helper method by the WithXyz() methods.</summary>
		public abstract LNode Clone();
		
		public LNode WithRange(SourceRange range) { return With(range, Style); }
		public LNode WithStyle(NodeStyle style)   { return With(Range, style); }
		public virtual LNode With(SourceRange range, NodeStyle style)
		{
			var copy = Clone();
			copy.RAS = new RangeAndStyle(range, style);
			return copy;
		}

		public virtual LNode WithoutAttrs() { return WithAttrs(RVList<LNode>.Empty); }
		public abstract LNode WithAttrs(RVList<LNode> attrs);
		
		public LNode WithAttrs(params LNode[] attrs) { return WithAttrs(new RVList<LNode>(attrs)); }
		public LNode WithArgs(params LNode[] args) { return WithArgs(new RVList<LNode>(args)); }
		public LNode AddAttr(LNode attr) { return WithAttrs(Attrs.Add(attr)); }
		public LNode AddAttrs(RVList<LNode> attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode AddAttrs(params LNode[] attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode AddArg(LNode arg) { return WithArgs(Args.Add(arg)); }
		public LNode AddArgs(RVList<LNode> args) { return WithArgs(Args.AddRange(args)); }
		public LNode AddArgs(params LNode[] args) { return WithArgs(Args.AddRange(args)); }

		#endregion

		public abstract void Call(LNodeVisitor visitor);
		public abstract void Call(ILNodeVisitor visitor);

		#region Other stuff

		public virtual string Print(NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n")
		{
			throw new NotImplementedException();
		}

		#endregion
	}

	/// <summary>Base class of all nodes that represent simple symbols (including special symbols such as #foo).</summary>
	public abstract class SymbolNode : LNode
	{
		protected SymbolNode(LNode ras) : base(ras) { }
		protected SymbolNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override NodeType Type { get { return NodeType.Symbol; } }
		public abstract override Symbol Name { get; }
		public abstract override LNode WithName(Symbol name);
		
		[EditorBrowsable(EditorBrowsableState.Never)] public override object Value { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LiteralNode WithValue(object value)           { throw new InvalidOperationException("WithValue(): this is a SymbolNode, cannot change Value."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode Target { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override RVList<LNode> Args { get { return RVList<LNode>.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(LNode target, RVList<LNode> args)  { throw new InvalidOperationException("With(): this is a SymbolNode, cannot use With(target, args)."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(Symbol target, RVList<LNode> args) { throw new InvalidOperationException("With(): this is a SymbolNode, cannot use With(target, args)."); }
		public override CallNode WithArgs(RVList<LNode> args) { return new StdComplexCallNode(this, args, Range); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }
	}
	
	/// <summary>Base class of all nodes that represent literal values such as 123 and "foo".</summary>
	public abstract class LiteralNode : LNode
	{
		protected LiteralNode(LNode ras) : base(ras) { }
		protected LiteralNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override NodeType Type { get { return NodeType.Literal; } }
		public abstract override object Value { get; }
		public abstract override LiteralNode WithValue(object value);

		[EditorBrowsable(EditorBrowsableState.Never)] public override Symbol Name { get { return GSymbol.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode WithName(Symbol name)                   { throw new InvalidOperationException("WidthName(): this is a LiteralNode, cannot change Name."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode Target { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override RVList<LNode> Args { get { return RVList<LNode>.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(LNode target, RVList<LNode> args)  { throw new InvalidOperationException("With(): this is a LiteralNode, cannot use With(target, args)."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(Symbol target, RVList<LNode> args) { throw new InvalidOperationException("With(): this is a LiteralNode, cannot use With(target, args)."); }
		public override CallNode WithArgs(RVList<LNode> args) { return new StdComplexCallNode(this, args, Range); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }
	}
	
	/// <summary>Base class of all nodes that represent calls such as <c>f(x)</c>, 
	/// operator calls such as <c>x + y</c>, braced blocks, and all other things 
	/// that are not simple symbols and literals.</summary>
	public abstract class CallNode : LNode
	{
		protected CallNode(LNode ras) : base(ras) { }
		protected CallNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override NodeType Type { get { return NodeType.Call; } }
		public override Symbol Name {
			get {
				var target = Target;
				if (target == null || !target.IsSymbol)
					return GSymbol.Empty;
				return target.Name;
			}
		}
		public override LNode WithName(Symbol name)
		{
			return WithTarget(Target.WithName(name));
		}
		[EditorBrowsable(EditorBrowsableState.Never)] public override object Value { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LiteralNode WithValue(object value) { throw new InvalidOperationException("WithValue(): this is a CallNode, cannot change Value."); }
		public abstract override LNode Target { get; }
		public abstract override RVList<LNode> Args { get; }
		public abstract override CallNode With(LNode target, RVList<LNode> args);
		public abstract override CallNode With(Symbol target, RVList<LNode> args);
		public override CallNode WithArgs(RVList<LNode> args) { return With(Target, args); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }
	}
}
