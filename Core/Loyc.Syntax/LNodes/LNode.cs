using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Loyc.Utilities;
using Loyc.Threading;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>All nodes in a Loyc syntax tree share this base class.</summary>
	/// <remarks>
	/// Loyc defines only three types of nodes: simple symbols, literals, and calls.
	/// <ul>
	/// <li>A <see cref="IdNode"/> is a simple identifier, such as a VariableName</li>
	/// <li>A <see cref="LiteralNode"/> is a literal constant, such as 123 or "hello"</li>
	/// <li>A <see cref="CallNode"/> encompasses all other kinds of nodes, such as
	/// normal function calls like <c>f(x)</c>, generic specifications like <c>f&lt;x></c>
	/// (represented as <c>@'of(f, x)</c>), braced blocks of statements (represented as
	/// <c>@`'{}`(stmt1, stmt2, ...)</c>), and so on.</li>
	/// </ul>
	/// See http://loyc.net/loyc-trees to learn more about the Loyc tree concept.
	/// <para/>
	/// This class provides access to all properties of all three types of nodes,
	/// in order to make this class easier to access from plain C#, and to avoid
	/// unnecessary downcasting in some cases. In fact, you never need to use the
	/// derived classes; you can think of them simply as a way that the implementation
	/// is optimized.
	/// <para/>
	/// Loyc nodes are immutable (except the <see cref="Style"/> property).
	///
	/// <h3>Important properties</h3>
	/// 
	/// The main properties of a node are
	/// <ol>
	/// <li><see cref="Attrs"/>: holds the attributes of the node, if any.</li>
	/// <li><see cref="Name"/>: the name of an <see cref="IdNode"/>, or the name 
	///    of the <see cref="IdNode"/> that is acting as the <see cref="Target"/> 
	///    of a <see cref="CallNode"/>.</li>
	/// <li><see cref="Value"/>: the value of a <see cref="LiteralNode"/>.</li>
	/// <li><see cref="Target"/>: the target of a <see cref="CallNode"/>. It 
	///    represents a method, macro, or special identifier that is being called.</li>
	/// <li><see cref="Args"/>: holds the arguments to a <see cref="CallNode"/>,
	///    if any. Returns an empty list if the node does not have an argument list.</li>
	/// <li><see cref="Range"/>: indicates the source file that the node came from
	///    and location in that source file.</li>
	/// <li><see cref="Style"/>: an 8-bit flag value that is used as a hint to the
	///    node printer about how the node should be printed. For example, a hex
	///    literal like 0x10 has the <see cref="NodeStyle.Alternate"/> style to
	///    distinguish it from decimal literals such as 16. Custom display styles 
	///    that do not fit in the Style property can be expressed with attributes.</li>
	/// </ol>
	/// To learn more about working with LNode, see http://loyc.net/loyc-trees/dotnet.html
	/// </remarks>
	public abstract class LNode : ILNode, ICloneable<LNode>, IEquatable<LNode>, IHasLocation, IHasValue<object>, INegListSource<LNode>
	{
		#region Constructors and static node creator methods

		protected LNode(LNode prototype)
		{
			RAS = (prototype ?? LNode.Missing).RAS;
		}
		protected LNode(SourceRange range, NodeStyle style)
		{
			RAS = new RangeAndStyle(range, style);
			if (RAS.Source == null)
				RAS.Source = SyntheticSource;
		}

		public static readonly EmptySourceFile SyntheticSource = new EmptySourceFile("<Synthetic Code>");

		/// <summary>The empty identifier, used to represent missing information.</summary>
		public static readonly IdNode Missing = Id(CodeSymbols.Missing);

		protected static readonly CallNode EmptySplice = Call(CodeSymbols.Splice);

		public static IdNode Id(Symbol name, LNode prototype) { return new StdIdNode(name, prototype); }
		public static IdNode Id(string name, LNode prototype) { return new StdIdNode(GSymbol.Get(name), prototype); }
		public static IdNode Id(LNodeList attrs, Symbol name, LNode prototype) { return new StdIdNodeWithAttrs(attrs, name, prototype); }
		public static IdNode Id(LNodeList attrs, string name, LNode prototype) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), prototype); }
		public static LiteralNode Literal(object value, LNode prototype) { return new StdLiteralNode(value, prototype); }
		public static LiteralNode Literal(LNodeList attrs, object value, LNode prototype) { return new StdLiteralNode(value, prototype); }
		public static CallNode Call(Symbol name, LNode prototype) { return new StdSimpleCallNode(name, LNodeList.Empty, prototype); }
		public static CallNode Call(LNode target, LNode prototype) { return new StdComplexCallNode(target, LNodeList.Empty, prototype); }
		public static CallNode Call(Symbol name, LNodeList args, LNode prototype) { return new StdSimpleCallNode(name, args, prototype); }
		public static CallNode Call(LNode target, LNodeList args, LNode prototype) { return new StdComplexCallNode(target, args, prototype); }
		public static CallNode Call(LNodeList attrs, Symbol name, LNodeList args, LNode prototype) { return new StdSimpleCallNodeWithAttrs(attrs, name, args, prototype); }
		public static CallNode Call(LNodeList attrs, LNode target, LNodeList args, LNode prototype) { return new StdComplexCallNodeWithAttrs(attrs, target, args, prototype); }
		public static CallNode Trivia(Symbol name, object value, LNode prototype) { return new StdTriviaNode(name, value, prototype); }

		public static IdNode Id(Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNode(name, range, style); }
		public static IdNode Id(string name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNode(GSymbol.Get(name), range, style); }
		public static IdNode Id(LNodeList attrs, Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNodeWithAttrs(attrs, name, range, style); }
		public static IdNode Id(LNodeList attrs, string name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), range, style); }
		public static LiteralNode Literal(object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, range, style); }
		public static LiteralNode Literal(LNodeList attrs, object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNodeWithAttrs(attrs, value, range, style); }
		public static CallNode Call(Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, LNodeList.Empty, range, style); }
		public static CallNode Call(Symbol name, SourceRange range, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, LNodeList.Empty, range, targetStart, targetEnd, style); }
		public static CallNode Call(LNode target, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, LNodeList.Empty, range, style); }
		public static CallNode Call(Symbol name, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, range, style); }
		public static CallNode Call(Symbol name, LNodeList args, SourceRange range, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, range, targetStart, targetEnd, style); }
		public static CallNode Call(LNode target, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, range, style); }
		public static CallNode Call(LNodeList attrs, Symbol name, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNodeWithAttrs(attrs, name, args, range, style); }
		public static CallNode Call(LNodeList attrs, LNode target, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, range, style); }
		public static CallNode Trivia(Symbol name, object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdTriviaNode(name, value, range, style); }

		public static IdNode Id(Symbol name, ISourceFile file = null) { return new StdIdNode(name, new SourceRange(file)); }
		public static IdNode Id(string name, ISourceFile file = null) { return new StdIdNode(GSymbol.Get(name), new SourceRange(file)); }
		public static IdNode Id(LNodeList attrs, Symbol name, ISourceFile file = null) { return new StdIdNodeWithAttrs(attrs, name, new SourceRange(file)); }
		public static IdNode Id(LNodeList attrs, string name, ISourceFile file = null) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), new SourceRange(file)); }
		public static LiteralNode Literal(object value, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file), style); }
		public static LiteralNode Literal(LNodeList attrs, object value, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file), style); }
		public static CallNode Call(Symbol name, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, LNodeList.Empty, new SourceRange(file), style); }
		public static CallNode Call(LNode target, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, LNodeList.Empty, new SourceRange(file), style); }
		public static CallNode Call(Symbol name, LNodeList args, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, new SourceRange(file), style); }
		public static CallNode Call(LNode target, LNodeList args, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, new SourceRange(file), style); }
		public static CallNode Call(LNodeList attrs, Symbol name, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNodeWithAttrs(attrs, name, LNodeList.Empty, new SourceRange(file), style); }
		public static CallNode Call(LNodeList attrs, LNode target, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, LNodeList.Empty, new SourceRange(file), style); }
		public static CallNode Call(LNodeList attrs, Symbol name, LNodeList args, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNodeWithAttrs(attrs, name, args, new SourceRange(file), style); }
		public static CallNode Call(LNodeList attrs, LNode target, LNodeList args, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, new SourceRange(file), style); }
		public static CallNode Trivia(Symbol name, object value, ISourceFile file = null, NodeStyle style = NodeStyle.Default) { return new StdTriviaNode(name, value, new SourceRange(file), style); }

		/// <summary>Used by the <c>quote {...}</c> macro.</summary>
		public static readonly LNode InParensTrivia = Id(CodeSymbols.TriviaInParens);

		// List creation with null check (even in release builds)
		public static LNodeList List() { return new LNodeList(); }
		public static LNodeList List(LNode item_0) => new LNodeList(item_0);
		public static LNodeList List(LNode item_0, LNode item_1) => new LNodeList(item_0, item_1);
		public static LNodeList List(params LNode[] list) => new LNodeList(list);
		public static LNodeList List(IEnumerable<LNode> list) => new LNodeList(list);
		public static LNodeList List(VList<LNode> list) => new LNodeList(list);

		// "nulls not allowed" is now enforced by LNodeList, but just to be sure...
		[Conditional("DEBUG")]
		protected static void NoNulls(LNodeList list, string propName)
		{
			foreach (var node in ((VList<LNode>)list).ToFVList()) // FVList enumerates faster
				Debug.Assert(node != null);
		}

		#endregion

		#region Fields

		protected RangeAndStyle RAS;

		protected internal struct RangeAndStyle
		{
			public RangeAndStyle(SourceRange range, NodeStyle style)
			{
				Source = range.Source;
				StartIndex = range.StartIndex;
				_stuff = (range.Length & LengthMask) | ((int)style << StyleShift);
			}
			public RangeAndStyle(ISourceFile source, int beginIndex, int length, NodeStyle style)
			{
				Source = source;
				StartIndex = beginIndex;
				_stuff = (length & LengthMask) | ((int)style << StyleShift);
			}

			public ISourceFile Source;
			public int StartIndex;
			private int _stuff;

			const int StyleShift = 24;
			const int LengthMask = (1 << StyleShift) - 1;

			public int Length {
				[DebuggerStepThrough] get { return _stuff << 8 >> 8; }
				[DebuggerStepThrough] set { _stuff = (_stuff & ~LengthMask) | (value & LengthMask); }
			}
			public int EndIndex { get { return StartIndex + Length; } }
			public NodeStyle Style {
				[DebuggerStepThrough] get { return (NodeStyle)(_stuff >> StyleShift); }
				[DebuggerStepThrough] set { _stuff = (_stuff & ~(0xFF << StyleShift)) | ((int)value << StyleShift); }
			}

			public static explicit operator SourceRange(RangeAndStyle ras) { return new SourceRange(ras.Source, ras.StartIndex, ras.Length); }
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
		[DebuggerDisplay("{ToString()}")]
		public virtual SourceRange Range { get { return (SourceRange)RAS; } }
		/// <summary>Returns the source file (shortcut for <c><see cref="Range"/>.Source</c>).</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ISourceFile Source { get { return RAS.Source; } }

		/// <summary>Returns Range.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object IHasLocation.Location
		{
			get { return Range; }
		}

		/// <summary>Indicates the preferred style to use when printing the node to a text string.</summary>
		/// <remarks>
		/// The Style is an 8-bit value that acts as a hint to the node printer about 
		/// how the node should be printed. Custom display styles that do not fit in 
		/// the Style property can be expressed with special "trivia" attributes that 
		/// have a <see cref="Name"/> starting with <c>%</c>. (trivia attributes, which
		/// are also used to store comments in the syntax tree, are not printed like
		/// normal attributes and are normally ignored if the node printer does not 
		/// specifically recognize them.)
		/// </remarks>
		public NodeStyle Style
		{
			get { return RAS.Style; }
			set { RAS.Style = value; }
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public NodeStyle BaseStyle
		{
			get { return RAS.Style & NodeStyle.BaseStyleMask; }
			set { Style = (RAS.Style & ~NodeStyle.BaseStyleMask) | (value & NodeStyle.BaseStyleMask); }
		}

		public LNode SetBaseStyle(NodeStyle s)
		{
			BaseStyle = s;
			return this;
		}
		public LNode SetStyle(NodeStyle s)
		{
			Style = s;
			return this;
		}

		/// <summary>Returns the attribute list for this node.</summary>
		public virtual LNodeList Attrs { get { return LNodeList.Empty; } }

		/// <summary>Returns true if the node is immutable, and false if any part of it can be edited.
		/// Currently, mutable nodes are not implemented.</summary>
		/// <remarks>Debugger-hidden until such time as mutable nodes actually exist.</remarks>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual bool IsFrozen { get { return true; } }

		/// <summary>Returns the <see cref="LNodeKind"/>: Symbol, Literal, or Call.</summary>
		public abstract LNodeKind Kind { get; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsCall { get { return Kind == LNodeKind.Call; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsId { get { return Kind == LNodeKind.Id; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsLiteral { get { return Kind == LNodeKind.Literal; } }

		#endregion

		#region Properties and methods for Symbol nodes (and simple calls)

		/// <summary>Returns the Symbol if <see cref="IsId"/>. If this node is 
		/// a call (<see cref="IsCall"/>) and <c>Target.IsId</c> is true, 
		/// this property returns <c>Target.Name</c>. In all other cases, the name
		/// is <see cref="GSymbol.Empty"/>. Shall not return null.</summary>
		/// <remarks>Examples (using C#/LES syntax):
		/// <pre>
		/// Expression   Kind    Name (blank if empty)
		/// hello        Id      hello
		/// @#if         Id      #if
		/// Foo(x, y)    Call    Foo
		/// x += y       Call    +=
		/// x.Foo(y)     Call    
		/// 5.0          Literal 
		/// </pre>
		/// </remarks>
		public abstract Symbol Name { get; }

		/// <summary>Returns true if <see cref="Name"/> is a "special" name 
		/// (i.e. starts with '#' or '\'' or '.' or any character below 48 in ASCII).</summary>
		/// <remarks>Note that this property returns false for the empty identifier <c>@``</c>.</remarks>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasSpecialName { get { return IsSpecialName(Name.Name); } }

		/// <summary>Returns true if <c>name</c> is considered a "special" name that
		/// starts with any character below 48 in ASCII, such as '#', '\'', and '.'.</summary>
		/// <remarks>
		/// This returns false for the empty string or null. 
		/// <para/>
		/// In order to keep the check trivially simple, this returns true for '$'
		/// even though it is <i>not</i> special in some languages (e.g. JavaScript).
		/// <para/>
		/// Letters, underscores, digits, and some punctuation do not count as special.
		/// The full list of specials is <c>! " # $ % &amp; ' ( ) * + , - . /</c> plus
		/// the space character and the control characters.
		/// </remarks>
		public static bool IsSpecialName(string name) { return name != null && name.Length > 0 && name[0] <= '\''; }
		public static bool IsSpecialName(Symbol name) { return name != null && name.Name.Length > 0 && name.Name[0] <= '\''; }

		/// <summary>Creates a node with a new value for Name.</summary>
		/// <remarks>If IsId, the Name is simply changed. If <see cref="IsCall"/>, 
		/// this method returns the equivalent of <c>WithTarget(Target.WithName(name))</c>
		/// (which may be optimized for the particular call type). If <see 
		/// cref="IsLiteral"/>, the <see cref="Kind"/> changes to <see cref="LNodeKind.Id"/> in
		/// order to set the name.</remarks>
		public virtual LNode WithName(Symbol name)
		{
			var attrs = Attrs;
			if (attrs.Count == 0)
				return new StdIdNode(name, this);
			else
				return new StdIdNodeWithAttrs(attrs, name, this);
		}

		#endregion

		#region Properties and methods for Literal nodes

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasValue { get { return Value != NoValue.Value; } }

		/// <summary>Returns the value of a literal node, or <see cref="NoValue.Value"/> 
		/// if this node is not a literal (<see cref="IsLiteral"/> is false).</summary>
		public abstract object Value { get; }

		/// <summary>Creates a new literal node with a different Value than the current literal node.</summary>
		/// <exception cref="InvalidOperationException">The node was not a literal already.</exception>
		public abstract LiteralNode WithValue(object value);

		#endregion

		#region Properties and methods for Call nodes

		/// <summary>Returns the target of a method call, or null if <see cref="IsCall"/> 
		/// is false. The target can be a symbol with no name (<see cref="GSymbol.Empty"/>)
		/// to represent a parenthesized expression, if there is one argument.</summary>
		public abstract LNode Target { get; }

		ILNode ILNode.Target { get { return Target; } }

		/// <summary>Returns the argument list of this node. Always empty when <c><see cref="IsCall"/>==false</c>.</summary>
		/// <remarks>
		/// Depending on the <see cref="Target"/>, Args may represent an actual 
		/// argument list, or it may represent some other kind of list. For 
		/// example, if the target is "{}" then Args represents a list of 
		/// statements in a braced block, and if the target is ">=" then Args 
		/// represents the two arguments to the ">=" operator.
		/// </remarks>
		public abstract LNodeList Args { get; }

		public virtual CallNode WithTarget(LNode target) { return With(target, Args); }
		public virtual CallNode WithTarget(Symbol name) { return With(name, Args); }

		/// <summary>Creates a Node with a new argument list. If this node is not a 
		/// call, a new node is created using this node as its target. Otherwise,
		/// the existing argument list is replaced.</summary>
		/// <param name="args">New argument list</param>
		public abstract CallNode WithArgs(LNodeList args);

		/// <summary>Creates a <see cref="CallNode"/> with the same attributes and 
		/// <see cref="Range"/>, but a different target and argument list. If the 
		/// current node is not a CallNode, it becomes one (the Range, Style and 
		/// attributes of the current node are kept, but the Kind, Value, and 
		/// Name are discarded.)</summary>
		public virtual CallNode With(LNode target, LNodeList args)
		{
			if (target == null) throw new ArgumentNullException("target");
			if (Target == target && Args == args)
				return (CallNode)this;

			var attrs = Attrs;
			if (attrs.Count == 0)
				return new StdComplexCallNode(target, args, this);
			else
				return new StdComplexCallNodeWithAttrs(attrs, target, args, this);
		}

		/// <summary>Creates a <see cref="CallNode"/> with the same attributes and 
		/// <see cref="Range"/>, but a different target and argument list. If the 
		/// current node is not a CallNode, it becomes one (the Range, Style and 
		/// attributes of the current node are kept, but the Kind, Value, and 
		/// Name are discarded.)</summary>
		public virtual CallNode With(Symbol target, LNodeList args)
		{
			var attrs = Attrs;
			if (attrs.Count == 0)
				return new StdSimpleCallNode(target, args, this);
			else
				return new StdSimpleCallNodeWithAttrs(attrs, target, args, this);
		}
		public CallNode With(Symbol target, params LNode[] args) { return With(target, new LNodeList(args)); }

		#endregion

		#region Other WithXyz methods, and Clone()

		/// <summary>Creates a copy of the node. Since nodes are immutable, there 
		/// is little reason for an end-user to call this, but Clone() is used 
		/// internally as a helper method by the WithXyz() methods.</summary>
		public abstract LNode Clone();

		public LNode WithRange(SourceRange range) { return With(range, Style); }
		public virtual LNode WithRange(int startIndex, int endIndex)
		{
			var copy = Clone();
			copy.RAS = new RangeAndStyle(RAS.Source, startIndex, endIndex - startIndex, RAS.Style);
			return copy;
		}
		public LNode WithStyle(NodeStyle style) { return With(Range, style); }
		public virtual LNode With(SourceRange range, NodeStyle style)
		{
			var copy = Clone();
			copy.RAS = new RangeAndStyle(range, style);
			return copy;
		}

		public virtual LNode WithoutAttrs() { return WithAttrs(LNodeList.Empty); }
		public abstract LNode WithAttrs(LNodeList attrs);

		public LNode WithAttrs(params LNode[] attrs) { return WithAttrs(new LNodeList(attrs)); }
		public CallNode WithArgs(params LNode[] args) { return WithArgs(new LNodeList(args)); }
		public LNode PlusAttr(LNode attr) { return WithAttrs(Attrs.Add(attr)); }
		public LNode PlusAttrs(IEnumerable<LNode> attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrs(params LNode[] attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrs(LNodeList attrs) { return attrs.IsEmpty ? this : WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrsBefore(LNodeList attrs) { return attrs.IsEmpty ? this : WithAttrs(attrs.AddRange(Attrs)); }
		public LNode PlusAttrsBefore(params LNode[] attrs) { return WithAttrs(Attrs.InsertRange(0, attrs)); }
		public LNode PlusAttrBefore(LNode attr) { return WithAttrs(Attrs.Insert(0, attr)); }
		public LNode PlusArg(LNode arg) { return WithArgs(Args.Add(arg)); }
		public LNode PlusArgs(LNodeList args) { return args.IsEmpty ? this : WithArgs(Args.AddRange(args)); }
		public LNode PlusArgs(IEnumerable<LNode> args) { return WithArgs(Args.AddRange(args)); }
		public LNode PlusArgs(params LNode[] args) { return WithArgs(Args.AddRange(args)); }
		public LNode WithArgChanged(int index, Func<LNode, LNode> selector)
		{
			var args = Args;
			var arg = args[index];
			var newValue = selector(arg);
			CheckParam.IsNotNull("return value of selector", newValue);
			if (newValue != arg) {
				args[index] = newValue;
				return WithArgs(args);
			} else
				return this;
		}
		public LNode WithArgChanged(int index, LNode newValue)
		{
			CheckParam.IsNotNull("newValue", newValue);
			var args = Args;
			if (newValue != args[index]) {
				args[index] = newValue;
				return WithArgs(args);
			} else
				return this;
		}
		public LNode WithAttrChanged(int index, LNode newValue)
		{
			CheckParam.IsNotNull("newValue", newValue);
			var a = Attrs;
			if (newValue != a[index]) {
				a[index] = newValue;
				return WithAttrs(a);
			} else
				return this;
		}
		public LNode WithChildChanged(int index, LNode newValue)
		{
			if (index >= 0) {
				return WithArgChanged(index, newValue);
			} else if (index < -1) {
				return WithAttrChanged(index + 1 + AttrCount, newValue);
			} else { // index == -1
				return WithTarget(newValue);
			}
		}

		#endregion

		public abstract void Call(LNodeVisitor visitor);
		public abstract void Call(ILNodeVisitor visitor);

		#region Node printer service (used by ToString())

		static ILNodePrinter _defaultPrinter = Loyc.Syntax.Les.Les2LanguageService.Value;
		static ThreadLocalVariable<ILNodePrinter> _printer = new ThreadLocalVariable<ILNodePrinter>();

		/// <summary>Gets or sets the default node printer on the current thread,
		/// which controls how nodes are serialized to text by default.</summary>
		/// <remarks>The LES printer is the default, and will be used if you try
		/// to set this property to null.</remarks>
		public static ILNodePrinter Printer
		{
			get { return _printer.Value ?? _defaultPrinter; }
			set { _printer.Value = value; }
		}
        
        public static SavedValue<ILNodePrinter> SetPrinter(ILNodePrinter newValue)
        {
            return new SavedValue<ILNodePrinter>(_printer, newValue);
        }

        /// <summary>Helps you change printers temporarily. Usage in C#: 
        /// <c>using (LNode.PushPrinter(myPrinter)) { ... }</c></summary>
        /// <remarks>For example, to switch to the EC# printer, use
        /// <c>using (LNode.PushPrinter(EcsNodePrinter.Printer)) { ... }</c>.
        /// This changes the default printer. If you don't want to change the
        /// default printer, please invoke the printer directly: 
        /// <code>
        ///     var sb = new StringBuilder();
        ///     EcsNodePrinter.Printer(node, sb, MessageSink.Trace);
        /// </code>
        /// </remarks>
        [Obsolete("Please use using(LNode.SetPrinter(...)) instead")]
        public static PushedPrinter PushPrinter(ILNodePrinter printer) { return new PushedPrinter(printer); }
        
        /// <summary>Returned by <see cref="PushPrinter(ILNodePrinter)"/>.</summary>
        [Obsolete]
        public struct PushedPrinter : IDisposable
		{
			ILNodePrinter old;
			public PushedPrinter(ILNodePrinter @new) { old = Printer; Printer = @new; }
			public void Dispose() { Printer = old; }
		}

		public virtual string Print(ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			return Printer.Print(this, MessageSink.Null, mode, options);
		}

		#endregion

		#region Other stuff

		/// <summary>Gets the value of <c>Args[0].Value</c>, if Args[0] exists; 
		/// otherwise, returns <see cref="NoValue.Value"/>.</summary>
		/// <remarks>"Trivia nodes" are used to efficiently represent the value of
		/// trivia and non-tree <see cref="Lexing.Token"/>s; they can be created by 
		/// calling the <see cref="LNode.Trivia"/> function. Since an LNode is not 
		/// allowed to have both a Name and a Value (as there is no syntax in LES 
		/// or EC# for such a node), a trivia node pretends that there is an 
		/// argument list with one item, and that one item is always a literal 
		/// whose Value is the value stored in the trivia node. Thus, a token node 
		/// is printed out as <c>TokenType(Value)</c> where <c>Value</c> is some 
		/// literal.
		/// <para/>
		/// If you suspect you're dealing with a trivia node, it is wasteful to 
		/// actually call <c>node.Args[0].Value</c> since this causes a temporary
		/// token list to be allocated. Instead you should use this property, which
		/// returns the token value without allocating memory. Of course, if this 
		/// property is called on a non-trivia node, it simply returns 
		/// <c>Args[0].Value</c>.
		/// </remarks>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual object TriviaValue
		{
			get {
				LNodeList args = Args;
				if (args.Count >= 1)
					return args[0].Value;
				return NoValue.Value;
			}
		}

		public override string ToString()
		{
			return Print();
		}

		/// <summary>Compares two lists of nodes for structural equality.</summary>
		/// <param name="mode">Whether to pay attention to <see cref="Style"/> and trivia attributes</param>
		/// <remarks>Position information is not compared.</remarks>
		public static bool Equals(LNodeList a, LNodeList b, CompareMode mode = CompareMode.Normal)
		{
			if (a.Count != b.Count)
				return false;
			while (!a.IsEmpty)
				if (!Equals(a.Pop(), b.Pop(), mode))
					return false;
			return true;
		}
		
		/// <inheritdoc cref="Equals(ILNode, CompareMode)"/>
		public static bool Equals(ILNode a, ILNode b, CompareMode mode = CompareMode.Normal)
		{
			if ((object)a == b)
				return true;
			if (a == null || b == null)
				return false;
			int max = a.Max, min;
			if (max != b.Max)
				return false;
			var kind = a.Kind;
			if (kind != b.Kind)
				return false;
			if ((mode & CompareMode.Styles) != 0 && a.Style != b.Style)
				return false;
			if (a.Name != b.Name)
				return false;
			if (kind == LNodeKind.Literal && !object.Equals(a.Value, b.Value))
				return false;
			if ((mode & CompareMode.IgnoreTrivia) != 0) {
				// TODO: unit test this
				int ia, ib;
				for (ia = a.Min, ib = b.Min; ia < -1 && ib < -1; ia++)
				{
					var attr_a = a[ia];
					if (!attr_a.IsTrivia()) {
						do {
							var attr_b = b[ib];
							ib++;
							if (!attr_b.IsTrivia()) {
								if (Equals(attr_a, attr_b, mode))
									break;
								else
									return false;
							}
						} while (ib < -1);
					}
				}
				while (ib < -1 && b[ib].IsTrivia())
					ib++;
				if (ia != ib)
					return false;
				min = -1;
			} else if ((min = a.Min) != b.Min)
				return false;

			for (int i = min; i <= max; i++)
				if (!Equals(a[i], b[i], mode))
					return false;
			return true;
		}

		[Flags]
		public enum CompareMode { Normal = 0, Styles = 1, IgnoreTrivia = 2 }

		/// <summary>Compares two nodes for structural equality. Two nodes are 
		/// considered equal if they have the same kind, the same name, the same 
		/// value, the same arguments, and the same attributes.</summary>
		/// <param name="mode">Whether to pay attention to <see cref="Style"/> and trivia attributes</param>
		/// <remarks>Position information (<see cref="Range"/>) is not compared.</remarks>
		public virtual bool Equals(ILNode other, CompareMode mode) { return Equals(this, other, mode); }
		public bool Equals(LNode other) { return Equals(this, other); }
		public bool Equals(ILNode other) { return Equals(this, other); }
		public override bool Equals(object other) { var b = other as LNode; return Equals(this, b); }
		protected internal abstract int GetHashCode(int recurse, int styleMask);
		/// <summary>Gets the hash code based on the structure of the tree.</summary>
		/// <remarks>
		/// If the tree is large, less than the entire tree is scanned to produce 
		/// the hashcode (in the absolute worst case, about 4000 nodes are examined, 
		/// but usually it is less than 100).
		/// </remarks>
		public override int GetHashCode() { return GetHashCode(3, 0); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int ArgCount { get { return Args.Count; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int AttrCount { get { return Attrs.Count; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool HasAttrs { get { return Attrs.Count != 0; } }

		public bool HasPAttrs()
		{
			var a = Attrs;
			for (int i = 0, c = a.Count; i < c; i++)
				if (!a[i].IsTrivia)
					return true;
			return false;
		}

		public LNodeList PAttrs()
		{
			return Attrs.SmartWhere(a => !a.IsTrivia);
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsTrivia
		{
			get { return CodeSymbols.IsTriviaSymbol(Name); }
		}

		public virtual bool Calls(Symbol name, int argCount)       { Debug.Assert(!IsCall); return false; }
		public virtual bool Calls(string name, int argCount)       { Debug.Assert(!IsCall); return false; }
		public virtual bool Calls(Symbol name)                     { Debug.Assert(!IsCall); return false; }
		public virtual bool Calls(string name)                     { Debug.Assert(!IsCall); return false; }
		/// <summary>Returns true if this is a call with the specified name and the specified minimum number of arguments.</summary>
		public virtual bool CallsMin(Symbol name, int minArgCount) { Debug.Assert(!IsCall); return false; }
		/// <summary>Returns true if this is a call with the specified name and the specified minimum number of arguments.</summary>
		public virtual bool CallsMin(string name, int minArgCount) { Debug.Assert(!IsCall); return false; }
		/// <summary>Returns true if this is not a call, or if the call's Target is an Id or a Literal.</summary>
		public virtual bool HasSimpleHead()                        { Debug.Assert(!IsCall); return true; }
		/// <summary>Returns true if this is not a call, or if the call's Target is an Id or a Literal, and the Target has only trivia attributes.</summary>
		public virtual bool HasSimpleHeadWithoutPAttrs()           { Debug.Assert(!IsCall); return true; }
		
		public virtual LNode WithAttrs(Func<LNode, Maybe<LNode>> selector) {
			Debug.Assert(AttrCount == 0);
			return this;
		}
		public virtual LNode WithArgs(Func<LNode, Maybe<LNode>> selector) { Debug.Assert(!IsCall); return this; }
		
		public virtual bool IsIdWithoutPAttrs()                    { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdWithoutPAttrs(Symbol name)         { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdNamed(Symbol name)                 { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdNamed(string name)                 { Debug.Assert(!IsId); return false; }

		/// <summary>Some <see cref="CallNode"/>s are used to represent lists. This 
		/// method merges two nodes, forming or appending a list (see remarks).</summary>
		/// <param name="node1">First node, list, or null.</param>
		/// <param name="node2">Second node, list, or null.</param>
		/// <param name="listName">The <see cref="Name"/> used to detect whether a 
		/// node is a list (typically "#splice"). Any other name is considered a 
		/// normal call, not a list. If this method creates a list from two non-
		/// lists, this parameter specifies the Name that the list will have.</param>
		/// <returns>The merged list.</returns>
		/// <remarks>
		/// The order of the data is retained (i.e. the data in node1 is inserted
		/// before the data in node2).
		/// <ul>
		/// <li>If either node1 or node2 is null, this method returns the other (node1 ?? node2).</li>
		/// <li>If both node1 and node2 are lists, this method merges the list 
		/// into a single list by appending node2's arguments at the end of node1.
		/// The attributes of node1 are kept and those of node2 are discarded.</li>
		/// <li>If one of the nodes is a list and the other is not, the non-list
		/// is inserted into the list's Args.</li>
		/// <li>If neither node is a list, a list is created with both nodes as 
		/// its two Args.</li>
		/// </ul>
		/// </remarks>
		/// <seealso cref="LNodeExt.WithSpliced"/>
		public static LNode MergeLists(LNode node1, LNode node2, Symbol listName)
		{
			if (node1 == null)
				return node2;
			if (node2 == null)
				return node1;
			if (node1.Calls(listName))
				return node1.WithSplicedArgs(node2, listName);
			else if (node2.Calls(listName))
				return node2.WithSplicedArgs(0, node1, listName);
			else
				return LNode.Call(listName, new LNodeList(node1, node2));
		}

		/// <summary>Combines two nodes using a binary operator or function.</summary>
		/// <param name="node1">First node, list, or null.</param>
		/// <param name="node2">Second node, list, or null.</param>
		/// <param name="binaryOpName">Binary operator to use when the nodes are not null.</param>
		/// <returns>If either node1 or node2 is null, this method returns the other node
		/// (node1 ?? node2), otherwise the nodes are joined with the specified operator.</returns>
		/// <seealso cref="FlattenBinaryOpSeq"/>
		public static LNode MergeBinary(LNode node1, LNode node2, Symbol binaryOpName)
		{
			if (node1 == null) return node2;
			if (node2 == null) return node1;
			SourceRange r1 = node1.Range, r2 = node2.Range;
			if (r2.Source == r1.Source) {
				int start = System.Math.Min(r1.StartIndex, r2.StartIndex);
				r1 = new SourceRange(r1.Source, start, System.Math.Max(r1.EndIndex, r2.EndIndex) - start);
			}
			return Call(binaryOpName, new LNodeList(node1, node2), r1);
		}

		/// <summary>Converts a sequence of the same operator (e.g. `x+y+z`) to a list (e.g. x, y, z).</summary>
		/// <param name="expr">An expression that may or may not call the operator.</param>
		/// <param name="opName">The operator to recognize.</param>
		/// <param name="rightAssociative">Whether the operator is right associative. 
		/// If this is null then either or both associativities will work (e.g. 
		/// <c>(a*b)*(c*d)</c> is flattened to a,b,c,d).</param>
		/// <returns>A list of subexpressions with the operator removed. If <c>expr</c> 
		/// does not call the specified binary operator, the list contains a single item 
		/// which is the original expression.</returns>
		/// <remarks>This is the reverse operation of <see cref="MergeBinary"/>.</remarks>
		public static List<LNode> FlattenBinaryOpSeq(LNode expr, Symbol opName, bool? rightAssociative = null)
		{
			var list = new List<LNode>();
			FlattenBinaryOpSeqCore(list, expr, opName, rightAssociative);
			return list;
		}
		static void FlattenBinaryOpSeqCore(List<LNode> list, LNode expr, Symbol opName, bool? rightAssociative = null)
		{
			while (expr.Calls(opName, 2))
			{
				var args = expr.Args;
				if (rightAssociative == true) {
					list.Add(args[0]);
				} else { // may be left associative
					FlattenBinaryOpSeqCore(list, args[0], opName, rightAssociative);
				}
				if (rightAssociative == false) {
					list.Add(args[1]);
					return;
				} else { // may be right associative
					expr = args[1];
				}
			}
			list.Add(expr);
		}

		public CallNode WithSplicedArgs(int index, LNode from, Symbol listName)
		{
			return WithArgs(LNodeExt.WithSpliced(Args, index, from, listName));
		}
		public CallNode WithSplicedArgs(LNode from, Symbol listName)
		{
			return WithArgs(LNodeExt.WithSpliced(Args, from, listName));
		}
		public LNode WithSplicedAttrs(int index, LNode from, Symbol listName)
		{
			return WithAttrs(LNodeExt.WithSpliced(Attrs, index, from, listName));
		}
		public LNode WithSplicedAttrs(LNode from, Symbol listName)
		{
			return WithAttrs(LNodeExt.WithSpliced(Attrs, from, listName));
		}

		public NestedEnumerable<DescendantsFrame, LNode> Descendants(NodeScanMode mode = NodeScanMode.YieldAllChildren)
		{
			return new NestedEnumerable<DescendantsFrame, LNode>(new DescendantsFrame(this, mode));
		}
		public NestedEnumerable<DescendantsFrame, LNode> DescendantsAndSelf()
		{
			return new NestedEnumerable<DescendantsFrame, LNode>(new DescendantsFrame(this, NodeScanMode.YieldAll));
		}

		#endregion

		#region INegListSource<LNode>

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Min
		{
			get { return -AttrCount - 1; }
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual int Max
		{
			get { return IsCall ? ArgCount - 1 : -2; }
		}

		public LNode this[int index]
		{
			get {
				bool fail;
				var r = TryGet(index, out fail);
				if (fail) throw new ArgumentOutOfRangeException("index");
				return r;
			}
		}

		public LNode TryGet(int index, out bool fail)
		{
			if (index >= 0) {
				return Args.TryGet(index, out fail);
			} else if (index < -1) {
				var a = Attrs;
				return a.TryGet(index + 1 + a.Count, out fail);
			} else { // index == -1
				var t = Target;
				fail = t == null;
				return t;
			}
		}

		public IRange<LNode> Slice(int start, int count = int.MaxValue)
		{
			return new NegListSlice<LNode>(this, start, count);
		}

		public int Count
		{
			get { return Max - Min + 1; }
		}

		public IEnumerator<LNode> GetEnumerator()
		{
			for (int i = Min; i <= Max; i++)
				yield return this[i];
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Explicit interface implementations (INegListSource<ILNode>)

		ILNode INegListSource<ILNode>.this[int index] { get { return this[index]; } }
		ILNode INegListSource<ILNode>.TryGet(int index, out bool fail) { return TryGet(index, out fail); }
		IEnumerator<ILNode> IEnumerable<ILNode>.GetEnumerator() { return GetEnumerator(); }
		IRange<ILNode> INegListSource<ILNode>.Slice(int start, int count)
		{
			return new NegListSlice<ILNode>(this, start, count);
		}

		#endregion

		/// <summary>Transforms the <see cref="Target"/>, parameters, and optionally
		/// the attributes of an LNode, returning another LNode of the same Kind. If 
		/// the selector makes no changes, Select() returns <c>this</c>.</summary>
		/// <remarks>The selector is not allowed to return null, but it can return
		/// <c>NoValue.Value</c> to delete a parameter or target. If the current 
		/// node is a target, it cannot be deleted, so it is replaced with 
		/// <c>#splice()</c> which, by convention, represents an empty list.
		/// If you're wondering why we don't use <c>null</c> for deletions, it is 
		/// because the functionality of this method is actually implemented by 
		/// <see cref="VList{T}.WhereSelect(Func{T, Maybe{T}})"/>; since T could be a 
		/// value type, that method cannot use null as a signal to delete items from 
		/// the collection.
		/// <para/>
		/// It is not possible to delete the <see cref="Target"/> of a call, and
		/// if the selector returns <c>NoValue.Value</c> for the Target, the target 
		/// is replaced with an empty call to <c>#splice()</c>.</remarks>
		public virtual LNode Select(Func<LNode, Maybe<LNode>> selector, ReplaceOpt options = ReplaceOpt.ProcessAttrs)
		{
			Debug.Assert(ArgCount == 0);
			return (options & ReplaceOpt.ProcessAttrs) != 0 ? WithAttrs(selector) : this;
		}
		
		[Flags]
		public enum ReplaceOpt
		{
			/// <summary>When calling <c>n.ReplaceRecursive</c>, specifies that the 
			/// selector should be called on <c>n</c> itself, not just its children.</summary>
			ReplaceRoot = 1,
			/// <summary>When calling <see cref="ReplaceRecursive(Func{LNode, Maybe{LNode}}, ReplaceOpt)"/>
			/// or <see cref="Select(Func{LNode, Maybe{LNode}}, ReplaceOpt)"/>, specifies
			/// that attributes should be processed rather than left unchanged.</summary>
			ProcessAttrs = 2,
			/// <summary>ReplaceRoot and ProcessAttrs</summary>
			Default = ReplaceRoot | ProcessAttrs
		}

		/// <summary>Performs a recursive find-and-replace operation, by attempting
		/// to replace each child (among <see cref="Attrs"/>, <see cref="Target"/>, 
		/// <see cref="Args"/>) using the specified selector. This method can also
		/// be used for simple searching, by giving a selector that always returns 
		/// null.</summary>
		/// <param name="matcher">This method is called for each descendant, and
		/// optionally the root node. If the selector returns a node, the new node 
		/// replaces the node that was passed to <c>selector</c> and the children of 
		/// the new node are ignored. If the selector returns null, children of the 
		/// child are scanned recursively. If the selector returns Maybe{LNode}.NoValue
		/// then the current node is deleted if it is an argument or attribute. If 
		/// the current node is a target, it cannot be deleted, so it is replaced 
		/// with <c>#splice()</c> which, by convention, represents an empty list.
		/// If you delete the root node then this method returns Maybe{LNode}.NoValue.</param>
		/// <param name="options">Options, see <see cref="ReplaceOpt"/>.</param>
		/// <returns>The new node produced after all replacements have occurred.</returns>
		/// <remarks>If <c>replaceFunc</c> always returns null (or if <c>replaceRoot</c>
		/// is false and the root has no children), <c>ReplaceRecursive</c> returns 
		/// <c>this</c>.</remarks>
		public Maybe<LNode> ReplaceRecursive(Func<LNode, Maybe<LNode>> matcher, ReplaceOpt options = ReplaceOpt.Default)
		{
			Maybe<LNode> newRoot;
			if ((options & ReplaceOpt.ReplaceRoot) != 0)
				newRoot = matcher(this);
			else
				newRoot = new Maybe<LNode>(null);

			if (newRoot.HasValue) {
				if (newRoot.Value != null)
					return newRoot.Value;

				Func<LNode, Maybe<LNode>> selector = null; selector = node =>
				{
					Maybe<LNode> @new = matcher(node);
					if (@new.HasValue)
						return @new.Value ?? node.Select(selector, options);
					else
						return @new;
				};
				return Select(selector, options);
			} else
				return NoValue.Value;
		}
		public LNode ReplaceRecursive(Func<LNode, LNode> matcher, ReplaceOpt options = ReplaceOpt.Default)
		{
			return ReplaceRecursive(node => new Maybe<LNode>(matcher(node)), options).Value;
		}
	}
}