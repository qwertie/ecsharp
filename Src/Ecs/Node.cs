using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.CompilerCore;
using Loyc.Collections;
using Loyc.Utilities;
using Loyc.Collections.Impl;

namespace ecs
{
	using F = GreenFactory;

	/// <summary>
	/// Represents any node in an EC# abstract syntax tree.
	/// </summary>
	/// <remarks>
	/// The main properties of a node are
	/// 1. Head (a node), which represents the "full name" of a node, or null if
	///    the name is trivial (a simple name).
	/// 2. Name (a Symbol), which represents the name of a node if the name is 
	///    simple enough to be expressed as a Symbol. For example, if a node 
	///    represents the variable "foo", Name returns "foo" (as a symbol, denoted
	///    $foo). But if a node represents "String.Empty", Name just returns "#.",
	///    which indicates a "path", i.e. a series of nodes separated by dots.
	///    The name is #literal for any literal value such as 100 or "Hi, mom!".
	///    Head.Name always equals Name when Head is not null, which implies 
	///    Head.Head.Name==Name, too.
	/// 3. Args (an IListSource of nodes), which holds the arguments to the node,
	///    if any. Returns an empty list if the node does not have an argument 
	///    list.
	/// 4. LiteralValue, which holds the value of a literal if the name is 
	///    #literal. For anything that is not a literal, LiteralValue returns
	///    a special object that does not compare equal to anything but itself;
	///    LiteralValue.ToString() == "#nonliteral" in this case.
	/// 5. Attrs (an RVList of nodes), which holds the attributes of a node, if 
	///    any, or a zero-length list if there are none.
	/// 6. The IsAbc properties such as IsSymbol, IsLiteral and IsList, which tell 
	///    you what category of node you have. Note that when a node has no 
	///    arguments, there is still a distinction between IsCall=true and 
	///    IsCall=false; "foo()" is a call, while "foo" is not.
	/// <para/>
	/// Children of a node are never null.
	/// <para/>
	/// Here is some background information.
	/// <para/>
	/// EC# (enhanced C#) is intended to be the starting point of the Loyc 
	/// (Language of your choice) project, which will be a family of programming
	/// languages that will share a common representation for the syntax tree and 
	/// other compiler-related data structures.
	/// <para/>
	/// Just as LLVM assembly has emerged as a nearly universal standard 
	/// intermediate representation for back-ends, Loyc nodes are intended to be a 
	/// universal intermediate representation for syntax trees, and Loyc will 
	/// (eventually) include a generic set of tools for semantic analysis.
	/// <para/>
	/// EC#, then, will be the first language to use the Loyc syntax tree 
	/// representation, known as the "Loyc tree" for short. Most syntax trees are 
	/// very strongly typed, with separate data types for, say, variable 
	/// declarations, binary operators, method calls, method declarations, unary 
	/// operators, and so forth. Loyc, however, only has this single data type for 
	/// all nodes. There are several reasons for this:
	/// <para/>
	/// - Simplicity. Many projects have thousands of lines of code dedicated 
	///   to the AST (abstract syntax tree) data structure itself, because each 
	///   kind of AST node has its own class. 
	/// - Serializability. Loyc nodes can always be serialized to a plain text 
	///   "prefix tree" and deserialized back to objects, even by programs that 
	///   are not designed to handle the language that the tree represents*. This 
	///   makes it easy to visualize syntax trees or exchange them between 
	///   programs.
	/// - Extensibility. Loyc nodes can represent any language imaginable, and
	///   they are suitable for embedded DSLs (domain-specific languages). Since 
	///   nodes do not enforce a particular structure, they can be used in 
	///   different ways than originally envisioned. For example, most languages 
	///   only have "+" as a binary operator, that is, with two arguments. If  
	///   Loyc had a separate class for each AST, there would probably be a 
	///   PlusOperator class derived from BinaryOperator, or something. But since 
	///   there is only one node class, a "+" operator with three arguments is 
	///   always possible; this is denoted by #+(a, b, c) in EC# source code.
	/// <para/>
	///   * Currently, the only supported syntax for plain-text Loyc trees is 
	///     EC# syntax, either normal EC# or prefix-tree notation. As Loyc grows 
	///     in popularity, a more universal syntax should be standardized.
	/// <para/>
	/// Loyc's representation is both an blessing and a curse. The advantage is 
	/// that Loyc nodes can be used for almost any purpose, perhaps even 
	/// representing data instead of code in some cases. However, there is no 
	/// guarantee that a given AST follows the structure prescribed by a particular 
	/// programming language, unless a special validation step is performed after 
	/// parsing. In this way, Loyc trees are similar to XML trees, only simpler.
	/// <para/>
	/// Another major disadvantage is that it is more difficult to interpret a 
	/// syntax tree correctly: you have to remember that a method definition has 
	/// the structure #def(name, args, return_type, body), so if "node" is a method
	/// definition then node.Args[2] represents the return type, for example. In 
	/// contrast, most compilers have an AST class called MethodDefinition or 
	/// something, that provides properties such as Name and ReturnType. Once EC# 
	/// is developed, however, aliases could help avoid this problem by providing 
	/// a more friendly veneer over the raw nodes.
	/// <para/>
	/// For optimization purposes, the node class is actually a class hierarchy, 
	/// but most users should only use this class.
	/// <para/>
	/// Now let's talk about EC# syntax and how it relates to this class.
	/// <para/>
	/// EC# supports a generalized C-style syntax which will be described briefly
	/// here. Basically, almost any code that a programming student might mistake 
	/// for real C# code is legal, and there is some odd-looking syntax you've 
	/// never seen before that is also legal.
	/// <para/>
	/// Also, virtually any tree of nodes can be represented in EC# source code 
	/// using a prefix notation, which helps you understand Loyc ASTs because
	/// the prefix notation closely corresponds to the AST. For example, 
	/// #=(x, #*(y, 2)) is equivalent to the expression x = y * 2. This notation 
	/// tells you that 
	/// - there are two nodes with two arguments each
	/// - the outer one is named #= and the inner is named #*
	/// The syntax tree built from these two representations is identical.
	/// <para/>
	/// Prefix notation can be freely mixed with normal EC# code, although usually 
	/// there is no reason to do so:
	/// <para/>
	/// public Point OneTwo = MakePoint(1, 2);
	/// public #var(Point, Origin(MakePoint(0, 0)));
	/// public static #def(MakePoint, #(int x, int y), System.Drawing.Point, #{
	///		return new Point(x, y);
	///	});
	/// <para/>
	/// The prefix notation often involves special tokens of the form #X, where X is
	/// 1. A C# or EC# identifier
	/// 2. A C# keyword
	/// 3. A C# or EC# operator
	/// 4. A single-quoted string containing one or more characters
	/// 5. A backquoted string
	/// 6. One of the following pairs of tokens: {}, [], or <> (angle brackets)
	/// 7. Whitespace, an open brace '{' not immediately followed by '}', or an
	///    open parenthesis '('. The whitespace, paren or brace is not included 
	///    in the token.
	/// <para/>
	/// As it builds the AST, the parser translates all of these forms into a 
	/// Symbol that also starts with '#'. The following examples show how source 
	/// code text is translated into symbol names:
	/// <pre>
	/// #foo     ==> "#foo"         #>>          ==> "#>>"
	/// #?       ==> "#?"           #{}          ==> "#{}"          
	/// #while   ==> "#while"       #'Newline\n' ==> "#Newline\n"
	/// #@while  ==> "#while"       #`hi there!` ==> "#`hi there!`"
	/// #'while' ==> "#while"       #(whatever)  ==> "#"
	/// </pre>
	/// The parser treats all of these forms as a special "#keyword" token. A 
	/// #keyword token is parsed differently than a C# keyword, but semantically
	/// it is usually the same. For example, "#struct" has the same semantic 
	/// meaning as "struct" but the syntax is quite different, so the following 
	/// forms are equivalent:
	/// <pre>
	/// #struct(X, #(), #(int x));
	/// struct X { int x; }
	/// </pre>
	/// Ordinary method calls like Foo(x, y) also count as prefix notation; it
	/// just so happens that plain C# assigns the same meaning to this notation as
	/// EC# does. In fact, syntactically, "#return(7);" is simply a function call
	/// to a function called "#return". Although the parser treats it like a 
	/// function call, it produces the same syntax tree as "return 7;" would have.
	/// <para/>
	/// Another way to think about this is that #struct is a keyword that is parsed 
	/// like an identifier. This is different from the notation @struct which 
	/// already exists in plain C#; this is an ordinary identifier that has a "@" 
	/// sign in front to ensure that the parser does not mistake it for a keyword. 
	/// To the parser, @struct and #struct are mostly the same except that the 
	/// parser removes the @ sign but not the # sign. However, later stages of the 
	/// compiler treat @struct and #struct completely differently.
	/// <para/>
	/// Since the "#" character is already reserved in plain C# for preprocessor 
	/// directives, any node name such as "#if" and "#else" that could be mistaken
	/// for an old-fashioned preprocessor directive must be preceded by "@" at the 
	/// beginning of a line. For example, the statement "if (failed) return;" can 
	/// be represented in prefix notation as "#@if(failed, return)", although the 
	/// node name of "#@if" is actually "#if" (while the node name of "return" is 
	/// actually "#return"). Please note that preprocessor directives themselves 
	/// are not part of the normal syntax tree, because they can appear 
	/// midstatement. For example, this is valid C#:
	/// <pre>
	/// if (condition1
	///    #if DEBUG
	///       && condition2
	///    #endif
	///    ) return;
	/// </pre>
	/// How to represent these shenanigans is not yet decided.
	/// <para/>
	/// The special #X tokens don't require an argument list. When a #keyword token 
	/// lacks an argument list, it is treated much like a variable name.
	/// <para/>
	/// Any statement can have attributes attached to it; the attributes are 
	/// attached to the root node (in this example, the = operator):
	/// <pre>
	/// [PointlessAttribute(true)] x = y * 2;
	/// </pre>
	/// In fact, attributes are allowed not just at the beginning of a statement,
	/// but at the beginning of any subexpression in parenthesis:
	/// <pre>
	/// Debug.Assert(x == ([PointlessAttribute(true)] y + 2));
	/// </pre>
	/// Here, the PointlessAttribute is attached to the addition operator (+).
	/// These attributes are simply normal EC# nodes (arbitrary expressions), so 
	/// they don't have to look like normal attributes:
	/// <pre>
	/// [TheKing is dead] LongLive(TheKing);
	/// </pre>
	/// Applying attributes to executable statements has no predefined meaning; A 
	/// warning is issued if the compiler encounters an attribute where one is not 
	/// allowed in plain C#, or if the syntax cannot be interpreted as an 
	/// attribute. The parser supports this feature because it is sometimes useful 
	/// in metaprogramming and DSLs.
	/// <para/>
	/// Because an attribute must be attached to something, an "assembly:" 
	/// attribute is represented as an #assembly node with an attribute attached:
	/// <pre>
	/// [assembly: AssemblyTitle("MyApp")]  // Normal EC#
	/// [AssemblyTitle("MyApp")] #assembly; // The way EC# sees it internally
	/// </pre>
	/// Unlike in plain C#, by the way, EC# labels do not have to be attached to 
	/// anything; they are considered statements by themselves:
	/// <pre>
	/// void f() {
	///    goto end;
	///    end:       // OK in EC#, syntax error in plain C#
	/// }
	/// </pre>
	/// Perhaps the most interesting thing about EC# is that it is actually an 
	/// expression-based language, like LISP: everything in EC# can be considered
	/// an expression! For example, instead of writing a method as a list of 
	/// statements, we can write it as a list of expressions:
	/// <pre>
	/// // Normal EC#
	///	public static char HexDigitChar(int value)
	///	{
	///		Debug.Assert(16u > (uint)value);
	///		if ((uint)value >= 10)
	///			return (char)('A' - 10 + value);
	///		else
	///			return (char)('0' + value);
	///	}
	///	// Bizarro EC#
	///	[#public, #static] #def(HexDigitChar, #(#var(int, value)), #char, #
	///	(
	///		Debug.Assert(16u > (uint)value),
	///		#if ((uint)value >= 10,
	///			return (char)('A' - 10 + value),
	///			return (char)('0' + value))
	///	);
	/// </pre>
	/// Just so we're clear, you're not supposed to write "bizarro" code, but this
	/// notation can help you understand the underlying representation. The parser
	/// basically operates in two modes, one for expressions and one for
	/// statements. Statement mode allows certain constructs like "if", "while" 
	/// and "class" that expression mode does not understand. But once parsing is 
	/// finished, the code is just a tree of nodes with almost nothing to
	/// distinguish statements from expressions.
	/// <para/>
	/// EC# adopts a convention from LISP: the value of the final statement in a 
	/// block is the value of the block as a whole. This can be used to simplify 
	/// method and property definitions:
	/// <para/>
	///	int _value;
	///	public int Value { get { _value; } }
	/// <para/>
	/// The EC# if-else and switch statements (but not loops) work the same way, 
	/// and you can put a braced block in the middle of any expression:
	/// <pre>
	/// int hexChar = {
	///			if ((uint)value >= 10)
	///				'A' - 10;
	///			else
	///				'0';
	///		} + value;
	/// </pre>
	/// The braced block is represented by a #{} node, which introduces a new scope.
	/// In contrast, the special # node type (known as the "list keyword"), does 
	/// not create a new scope. It can be used with expression or statement syntax:
	/// <pre>
	/// var three = #(Console.WriteLine("Fetching the three!"), 3);
	/// var eight = #{ int x = 5; three + x; };
	/// var seven = x + 2;
	/// </pre>
	/// Since # does not create a new scope, the variable "x" is created in the 
	/// outer scope, where it can be used to compute the value of seven.
	/// The # keyword is intended mostly to express lists with prefix notation, but 
	/// it can be used with braces in case there is a need to switch back to 
	/// statement notation. 
	/// <para/>
	/// The above code is a bit confusing because of how it is written; EC# is 
	/// meant for mature people who have enough sense not to write confusing code 
	/// like this.
	/// </remarks>
	public class Node : INodeReader, IEquatable<Node>
	{
		/// <summary>A placeholder to represent a missing item in a list.</summary>
		public static Node Missing { get { return NewFromGreenFrozen(GreenFactory.Missing, -1); } }

		#region Data

		// I love to minimize memory usage. Most people would just use two separate 
		// pointers here: one for the parent node and one for the source file that 
		// the node came from. I am doing that too, but I seriously considered an 
		// alternate plan:
		// - Use one reference (object _parentOrFile) that points to the parent if
		//   attached or to the source file if detached.
		// - When detaching, replace parent reference with source file reference.
		// - When reattaching, check if source file is the same in the new parent. 
		//   If so (usual case), no special action is required. If not, check if 
		//   the basis is frozen. If it is, wrap it in a special node that adds an 
		//   attribute that points to the source file. If not, just add the 
		//   attribute to the mutable green node.
		// - When someone asks for the source file, we have to check for the 
		//   presence of the special attribute on each node before asking the 
		//   parent.
		// However, I relented, because this plan could cost a lot of CPU cycles 
		// when the source file is requested and moreover, the source file must be 
		// requested whenever a node is attached or detached. So there are two 
		// separate references.
		//
		// Usually, the cost for the extra reference is still modest. Mostly you
		// have to pay for it if you traverse an entire $Mutable source tree. So 
		// you can avoid the cost by either freezing the tree first, or mutating 
		// on Clone($Cursor). Both approaches allow the Node objects to be garbage-
		// collected soon after you examine them, and they also avoid the 
		// significant cost of storing a child list in each node.
		//
		// On the whole, Loyc trees will still be smaller than many, if not most,
		// AST designs in other compilers. An immutable Node needs 7 32-bit words 
		// or 6 64-bit words, while leaf GreenNodes (4 words for a simple symbol
		// and 8 words for an integer literal and its boxed value) are cached so 
		// that their size is almost negligible. Fully mutable Loyc trees need 
		// about twice as much space. The two node lists per node--and I mean the 
		// lists in EditableNode and EditableGreenNode, not the Args and Attrs
		// lists--are the biggest cost of a mutable tree, but leaves avoid 
		// allocating these lists.

		internal GreenNode _basis;
		internal Node _parent;
		// Cached; may be wrong if parent has changed. 0 represents the head node, 
		// 1..ArgCount represents the arguments, and above that represents the 
		// attributes.
		protected int _indexInParent;
		// Position in _sourceFile, and frozen flag
		protected int _sourceIndex;
		const int FrozenFlag = unchecked((int)0x80000000);
		const int SourceIndexMask = ~FrozenFlag;
		
		protected internal int SourceIndex
		{
			get { return _sourceIndex << 1 >> 1; } // sign extend
			set { _sourceIndex = (_sourceIndex & FrozenFlag) | (value & SourceIndexMask); }
		}
		public bool IsFrozen
		{
			get { return _sourceIndex < 0; }
		}
		protected void SetFrozenFlag()
		{
			_sourceIndex |= FrozenFlag;
		}
		protected internal int CachedIndexInParent
		{
			get { return _indexInParent; }
			set { _indexInParent = value; }
		}

		#endregion

		public static Node NewFromGreen(GreenNode basis, int sourceIndex)
		{
			return new EditableNode(basis, sourceIndex);
		}
		public static Node NewFromGreenFrozen(GreenNode basis, int sourceIndex)
		{
			var n = new Node(basis, sourceIndex);
			n.Freeze();
			return n;
		}
		public static Node NewFromGreenCursor(GreenNode basis, int sourceIndex)
		{
			return new Node(basis, sourceIndex);
		}
		public static Node NewSynthetic(Symbol name, SourceRange location)
		{
			var basis = new EditableGreenNode(name, location.Source, location.Length);
			return new EditableNode(basis, location.BeginIndex);
		}
		public static Node NewSyntheticCursor(Symbol name, SourceRange location)
		{
			var basis = new EditableGreenNode(name, location.Source, location.Length);
			return new Node(basis, location.BeginIndex);
		}
		public static Node NewSynthetic(Node location)
		{
			var basis = new EditableGreenNode(location.Name, location.SourceFile, location.SourceWidth);
			return new EditableNode(basis, location.SourceIndex);
		}
		public static Node NewSyntheticCursor(Node location)
		{
			var basis = new EditableGreenNode(location.Name, location.SourceFile, location.SourceWidth);
			return new Node(basis, location.SourceIndex);
		}

		protected Node(GreenNode basis, int sourceIndex, Node parent = null, int indexInParent = -1)
		{
			Debug.Assert(parent != null);
			_basis = basis;
			_parent = parent;
			CachedIndexInParent = indexInParent;
			SourceIndex = sourceIndex;
			if (parent != null && parent.IsFrozen)
				SetFrozenFlag();
		}

		public static bool operator ==(Node a, Node b) { return (object)a == null ? (object)b == null : a.Equals(b); }
		public static bool operator !=(Node a, Node b) { return !(a == b); }
		public override bool Equals(object obj)        { return ((object)this) == obj || Equals(obj as Node); }
		public bool Equals(Node other)                 { return other != null && other._basis == _basis; }
		public override int GetHashCode()              { return ~_basis.GetHashCode(); }
		public override string ToString()              { return _basis.ToString(); }

		// Clone mode values
		public static readonly Symbol _Mutable = GSymbol.Get("Mutable");
		public static readonly Symbol _Cursor = GSymbol.Get("Cursor");
		public static readonly Symbol _Frozen = GSymbol.Get("Frozen");
		public static readonly Symbol _FrozenOrSelf = GSymbol.Get("FrozenOrSelf");
		/// <summary>Clones using $Mutable mode, see <see cref="Clone(Symbol)"/>.</summary>
		public Node Clone() { return Clone(_Mutable); }
		/// <summary>Clones the node.</summary>
		/// <param name="mode">One of the following symbols:
		/// <list type="bullet">
		/// <item><term>$Frozen</term><description>
		/// Create the clone pre-frozen, which is slightly faster than a mutable clone.</description></item>
		/// <item><term>$FrozenOrSelf</term><description>
		/// Create the clone pre-frozen, or return 'this' if if the current node is already frozen.</description></item>
		/// <item><term>$Mutable</term><description>
		/// Create a fully editable clone. Note: children of a mutable clone can 
		/// be frozen individually, so that the parent is mutable but one or more 
		/// children are not.</description></item>
		/// <item><term>$Cursor</term><description>
		/// Creates a fast-mutating clone. When you are changing the cloned tree,
		/// you must access it as a "cursor" (like a database cursor), meaning 
		/// that you should only view and edit one node at a time. You can safely 
		/// keep references to parents of the node you are editing, but it is not 
		/// safe to keep child references, or multiple references to the same child.
		/// <para/>
		/// Specifically, a fast-mutating tree has the following major limitations:
		/// (1) If you read a child or descendent D of a node P, and then you 
		/// change P, D is "invalidated" and may throw NodeInvalidatedException if 
		/// you try to modify it. (2) If you read a child of P and then read the 
		/// same child again, two separate "views" of the Node are returned; if you 
		/// modify one of these, the changes may or may not be visible to the other 
		/// view.
		/// <para/>
		/// The speed of a $Cursor clone depends on fast garbage collection, since
		/// you get a new object every time you ask for the child of a node; this
		/// is related to the "red-green" design of the Loyc tree. Another 
		/// consequence of this design is that if you freeze a child and then ask
		/// for the same child again via the parent, the new version of the same
		/// child will be unfrozen and you can modify it. This peculiar behavior
		/// does not occur in $Mutable mode because that mode keeps track of all 
		/// children. You should avoid "taking advantage" of this property because
		/// it is an implementation detail, but it's possible to rely on this 
		/// property accidentally, by (1) calling some code that freezes a subtree
		/// and then (2) modifying that same subtree.
		/// <para/>
		/// Due to the differences between $Mutable and $Cursor, it is possible to 
		/// write code that works right on $Mutable clones but causes 
		/// NodeInvalidatedException on $Cursor clones, and conversely, to write
		/// code that works right on $Cursor clones but throws ReadOnlyException 
		/// on $Mutable clones.
		/// <para/>
		/// When <see cref="IsFrozen"/> is false, call <see cref="FullyEditable"/>
		/// to distinguish between $Mutable and $Cursor mode.
		/// </description></item>
		/// </param>
		/// <remarks>
		/// The cloned node will not have a Parent.
		/// <para/>
		/// In the worst case, cloning is an O(N) operation for a subtree with N 
		/// nodes, but in practice it is fast. Cloning a frozen Node, or a Node
		/// that has just been cloned, takes O(1) time.
		/// <para/>
		/// Before cloning, the green tree must be frozen, which is the O(N) part 
		/// (see <see cref="INodeReader"/> for a discussion of how Loyc node 
		/// trees work) and then a copy of just the current node is returned. 
		/// Usually, freezing and cloning are very fast, but changing either copy 
		/// of the tree after it has been cloned will take usually longer than 
		/// editing a tree that has never been cloned. The two copies of the tree 
		/// will share the same green nodes at first, and these frozen nodes will 
		/// be duplicated (along with any frozen parents) as you modify one or 
		/// both copies.
		/// </remarks>
		public Node Clone(Symbol mode)
		{
			_basis.Freeze();

			if (mode == _Mutable)
				throw new NotImplementedException();//TODO
			else if (mode == _Cursor)
				throw new NotImplementedException();//TODO
			
			if (mode == _FrozenOrSelf)
				if (IsFrozen)
					return this;
				else
					mode = _Frozen;
			if (mode != _Frozen)
				throw new ArgumentException("Invalid mode value in Node.Clone()");
			
			_basis.Freeze();
			throw new NotImplementedException();//TODO
		}

		/// <summary>Clones the node only if it is frozen; returns 'this' otherwise.</summary>
		public Node Unfrozen() { return IsFrozen ? Clone() : this; }

		public Node Parent           { get { return _parent; } }
		public bool HasParent        { get { return _parent != null; } }
		INodeReader INodeReader.Head { get { return _basis.Head; } }
		public bool HasSimpleHead    { get { return _basis.HasSimpleHead; } }
		public Symbol Name           { get { return _basis.Name; } }
		public Symbol Kind           { get { return _basis.Kind; } }
		public object Value          { get { return _basis.Value; } }
		public bool IsSynthetic      { get { return _basis.IsSynthetic; } }
		public bool IsCall           { get { return _basis.IsCall; } }
		public bool IsLiteral        { get { return _basis.IsLiteral; } }
		public bool IsSimpleSymbol   { get { return _basis.IsSimpleSymbol; } }
		public bool IsKeyword        { get { return _basis.IsKeyword; } }
		public bool IsIdent          { get { return _basis.IsIdent; } }
		public int SourceWidth       { get { return _basis.SourceWidth; } }
		public int ArgCount          { get { return _basis.ArgCount; } }
		public int AttrCount         { get { return _basis.AttrCount; } }
		public ArgList Args          { get { return new ArgList(this); } }
		public AttrList Attrs        { get { return new AttrList(this); } }
		IListSource<INodeReader> INodeReader.Args { get { return _basis.Args; } }
		IListSource<INodeReader> INodeReader.Attrs { get { return _basis.Attrs; } }
		INodeReader INodeReader.TryGetArg(int i) { return TryGetArg(i); }
		INodeReader INodeReader.TryGetAttr(int i) { return TryGetAttr(i); }
		public NodeStyle Style       { get { return _basis.Style; } }
		public NodeStyle BaseStyle   { get { return _basis.BaseStyle; } }
		public string Print(NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n") { return _basis.Print(style, indentString, lineSeparator); }
		// TODO: trivia. Idea: source file object keeps track of trivia, until user adds synthetic trivia
		public void Name_set(Symbol value) { AutoThawBasis(); _basis.Name_set(value); }
		public void Value_set(object value) { AutoThawBasis(); _basis.Value = value; }
		/// <summary>Returns true if this node is both mutable and operates in 
		/// "fully editable" mode, as opposed to cursor mode which has some
		/// limitations.</summary>
		public virtual bool FullyEditable { get { return false; } }

		public virtual void Freeze()
		{
			SetFrozenFlag();
			if (!_basis.IsFrozen)
				_basis.Freeze();
		}

		// combines source file and line number into one string

		static readonly Symbol _pos = GSymbol.Get("pos");
		public virtual ISourceFile SourceFile
		{
			get { return _basis.SourceFile; }
		}
		public virtual SourceRange SourceRange
		{
			get { return new SourceRange(_basis.SourceFile, _sourceIndex, _basis.SourceWidth); }
		}
		public string SourceLocation
		{
			get { return string.Format("{0}:{1}", SourceFile.FileName, SourceRange.Begin.Line); }
		}
		public int IndexInParent
		{
			get { return _parent == null ? -1 : _parent.IndexOf_OrBust(this); }
		}

		// Child getters
		public Node Head 
		{
			get {
				var h = _basis.HeadEx;
				if (h.Node == null) return null;
				return new Node(h, h.GetSourceIndex(SourceIndex), this, 0);
			}
		}
		public Node HeadOrThis
		{
			get { return Head ?? this; }
		}
		/// <summary>Returns the requested argument, or null if the index is invalid.</summary>
		public virtual Node TryGetArg(int index)
		{
			if ((uint)index >= (uint)ArgCount)
				return null;
			var g = _basis.TryGetArg(index);
			return new Node(g.Node, g.GetSourceIndex(SourceIndex), this, 1 + index);
		}
		/// <summary>Returns the requested attribute, or null if the index is invalid.</summary>
		public virtual Node TryGetAttr(int index)
		{
			if ((uint)index >= (uint)AttrCount)
				return null;
			var g = _basis.TryGetAttr(index);
			return new Node(g.Node, g.GetSourceIndex(SourceIndex), this, 1 + ArgCount + index);
		}

		#region Thawing behavior & mutability support

		protected internal void AutoThawBasis()
		{
			if (_basis.IsFrozen) {
				if (IsFrozen)
					_basis.ThrowCannotEditError();
				var newBasis = _basis.Clone();
				if (_parent != null)
					_parent.HandleChildThawing(this, newBasis);
				_basis = newBasis;
			}
		}
		virtual protected internal void HandleChildThawing(Node child, GreenNode newBasis)
		{
			AutoThawBasis(); // thaw ourself first
			int childIndex = IndexOf_OrBust(child);
			var g = _basis.TryGetChild(childIndex);
			Debug.Assert(g.Node == child._basis);
			_basis.SetChild(childIndex, new GreenAtOffs(newBasis, g.Offset));
		}
		virtual protected internal int IndexOf_OrBust(Node child)
		{
			Debug.Assert(child._parent == this);
			Debug.Assert(!(this is EditableNode));
			// Remember that a parent can have multiple copies of a given green 
			// child, and this restricts our ability to find a child in its parent 
			// without a list of red children. This base class method is designed 
			// for $Cursor mode; the version in EditableNode is for $Mutable mode. 
			// In $Cursor mode, it is illegal to (1) get a child reference, (2) 
			// modify the parent, then (3) use the child. We can assume therefore
			// that the child's CachedIndexInParent is correct; if not, the user
			// has made a mistake. Again, we cannot work around this with an O(N) 
			// scan because there could be multiple green children that match the 
			// child node.
			int i = child.CachedIndexInParent;
			if (_basis.TryGetChild(i).Node == child._basis)
				return i;
			throw new NodeInvalidatedException(Localize.From("Child node '{0}' invalidated in $Cursor-mode tree. After it was created, the parent '{1}' changed.", child, this));
		}
		protected internal void ChangeParent(Node newParent) // newParent may be null
		{
			_parent = newParent;
		}

		#endregion

		#region Methods that consider all children as a single list
		
		public void ThrowIfFrozen()
		{
			if (IsFrozen)
				throw new ReadOnlyException(string.Format("The node '{0}' is frozen against modification.", ToString()));
		}
		internal void ThrowIfHasParent()
		{
			if (HasParent)
				throw new InvalidOperationException(string.Format("The node '{0}' cannot be inserted elsewhere in the tree because it already has a parent. Detach the node first, or use a clone."));
		}
		public virtual int ChildCount { get { return 1 + ArgCount + AttrCount; } }
		public virtual Node TryGetChild(int index)
		{
			int index2 = index - ArgCount;
			if (index == 0)
				return Head;
			else if (index2 <= 0)
				return TryGetArg(index - 1);
			else
				return TryGetAttr(index2 - 1);
		}
		public virtual void SetChild(int index, Node child)
		{
			if (child == null) _basis.ThrowNullChildError(index <= ArgCount ? (index == 0 ? "Head" : "Args") : "Attrs");
			if (child.HasParent) child.ThrowIfHasParent();
			AutoThawBasis();
			_basis.SetChild(index, new GreenAtOffs(child._basis));
			child.CachedIndexInParent = index;
		}

		#endregion

		public void Detach()
		{
			if (_parent == null)
				return;
			_parent.ThrowIfFrozen();
			int i = _parent.IndexOf_OrBust(this);
			if (i == 0)
				_parent.SetChild(0, _parent);
			else if (i <= ArgCount)
				_parent.Args.RemoveAt(i - 1);
			else
				_parent.Attrs.RemoveAt(i - ArgCount - 1);
			Debug.Assert(_parent == null);
		}

		virtual protected internal void HandleChildInserted(int index, Node item)
		{
			Debug.Assert(!IsFrozen);
			Debug.Assert(TryGetChild(index) != null && TryGetChild(index)._basis == item._basis);
			item.ChangeParent(this);
		}
		virtual protected internal void HandleRangeRemoved(int index, int count)
		{
		}

		[Conditional("DEBUG")]
		public virtual void DebugCheck(bool recursive)
		{
			Debug.Assert(!IsFrozen || _basis.IsFrozen); // if we are frozen, so is basis
			if (_parent != null) {
				Debug.Assert(_parent._basis != _basis);
				_parent.IndexOf_OrBust(this); // throws on failure
			}
		}
	}


	public class EditableNode : Node
	{
		static Node[] EmptyArray = new Node[0];

		/// <summary>Contains a list of children or null if frozen.</summary>
		/// <remarks>This is sort of a cache; it only holds children that have 
		/// been requested before. Mutable nodes require this list and cannot 
		/// simply create new instances every time a child is requested, as a
		/// consequence of the fact that a nonfrozen red node can point to a 
		/// frozen green node.
		/// <para/>
		/// To understand why, imagine the mutable red syntax tree Foo(bar), 
		/// where bar's green node is frozen. Now, suppose two different modules
		/// request a reference to the first child of bar. If Foo does not have
		/// a list of its children, it must manufacture new references on demand,
		/// so it'll return two wrappers for 'bar', let's call them bar#1 and 
		/// bar#2. If someone modifies bar#1, the frozen green node must be 
		/// replaced with an unfrozen one. After creating the new green node, we
		/// can (and must) go up to the parent node to replace the reference in
		/// the parent's green node (which may unfreeze the parent in a similar
		/// manner). However, there is no way to notify bar#2 so it will have
		/// stale data; moreover, if one attempts to modify bar#2, it will be
		/// impossible to locate it in the parent and an exception must be thrown.
		/// <para/>
		/// This is basically how the $Cursor clone mode works, but this mode is
		/// too dangerous for general use.
		/// <para/>
		/// The array starts with zero length and is allocated with size determined
		/// by the green node (with room for one addition), when accessing one of 
		/// the children. The array reserves [0] for the head, even in nodes with 
		/// no separate head, in case the head is changed later. After the head 
		/// come the Args, then the Attrs. We don't need to track the number of
		/// used entries in the array, since this is determined by the green node.
		/// Children are created on-demand and an array entry is null until it is
		/// needed.
		/// </remarks>
		internal Node[] _children = EmptyArray;

		internal protected EditableNode(GreenNode basis, int sourceIndex, Node parent = null, int indexInParent = -1)
			: base(basis, sourceIndex, parent, indexInParent) { }
		
		public sealed override void Freeze()
		{
			if (!IsFrozen) {
				base.Freeze();
				for (int i = 0; i < _children.Length; i++)
					if (_children[i] != null)
						_children[i].Freeze();
				_children = null;
			}
		}

		public sealed override Node TryGetChild(int index)
		{
			if (IsFrozen)
				return base.TryGetChild(index);

			if (_children == EmptyArray) {
				if (_basis.ChildCount == 1) {
					if (index != 0)
						return null;
					if (_basis.Head == null)
						return null;
				}
				_children = new Node[_basis.ChildCount + 1];
			}
			Debug.Assert(_children.Length >= _basis.ChildCount);

			if ((uint)index >= (uint)_children.Length)
				return null;
			var c = _children[index];
			if (c != null) {
				c.CachedIndexInParent = index;
			} else {
				var g = _basis.TryGetChild(index);
				if (g.Node == null)
					return null;
				_children[index] = c = new EditableNode(g.Node, g.GetSourceIndex(SourceIndex), this, index);
			}
			return c;
		}
		public sealed override Node TryGetArg(int index) { return TryGetChild(1 + index); }
		public sealed override Node TryGetAttr(int index) { return TryGetChild(1 + ArgCount + index); }
		public sealed override bool FullyEditable { get { return !IsFrozen; } }

		protected internal sealed override void HandleChildThawing(Node child, GreenNode newBasis)
		{
			base.HandleChildThawing(child, newBasis);
		}
		protected internal sealed override int IndexOf_OrBust(Node child)
		{
			Debug.Assert(child._parent == this);
			int i = child.CachedIndexInParent;
			if ((uint)i < (uint)_children.Length && _children[i] == child)
				return i;

			for (i = 0; i < _children.Length; i++)
				if (_children[i] == child) {
					child.CachedIndexInParent = i;
					return i;
				}
			throw new InvalidStateException(Localize.From("Bug detected: cannot find child '{0}' in '{1}'. Last known index: {2}", child, this, child.CachedIndexInParent));
		}

		protected internal override void HandleChildInserted(int index, Node item)
		{
			_children = InternalList.Insert(index, item, _children, _basis.ChildCount);
			base.HandleChildInserted(index, item);
		}
		protected internal override void HandleRangeRemoved(int index, int count)
		{
			Debug.Assert(_basis.ChildCount + count <= _children.Length); // already removed from green
			for (int i = index; i < index + count; i++)
			{
				Node detaching = _children[i];
				if (detaching != null)
					detaching.ChangeParent(null);
			}
			InternalList.RemoveAt(index, count, _children, _basis.ChildCount + count);
			base.HandleRangeRemoved(index, count);
		}

		public override void DebugCheck(bool recursive)
		{
			base.DebugCheck(recursive);
			if (IsFrozen) return;
			
			Debug.Assert(_children.Length >= _basis.ChildCount);
			for (int i = 0; i < _children.Length; i++)
				if (_children[i] != null)
				{
					var g = _basis.TryGetChild(i);
					Debug.Assert(g.Node == _children[i]._basis);
					if (recursive)
						_children[i].DebugCheck(recursive);
				}
		}
	}

	/*public class RootNode : EditableNode
	{
		protected ISourceFile _sourceFile;
		protected int _sourceIndex;

		internal RootNode(GreenNode basis, ISourceFile sourceFile, int sourceIndex, bool startFrozen = false) : base(basis, null, -1)
		{
			_sourceFile = sourceFile;
			_sourceIndex = sourceIndex;
			if (startFrozen)
				Freeze();
		}
		internal RootNode(Node location) : base(new EditableGreenNode(location.Name, location.SourceWidth), null, -1) // creates an empty editable node
		{
			var r = location.SourceRange;
			_sourceFile = r.Source;
			_sourceIndex = r.BeginIndex;
		}

		public override SourceRange SourceRange
		{
			get {
				return new SourceRange(_sourceFile, _sourceIndex, _basis.SourceWidth);
			}
		}
	}*/

	public struct ArgList : IList<Node>, IListSource<Node>
	{
		Node _node;
		EditableGreenNode _egNode;
		public ArgList(Node node) { _node = node; _egNode = null; }

		void AutoOutOfRange(int index)
		{
			if (index >= Count) GreenArgList.OutOfRange(_node._basis, index);
		}
		private void NullError(int index)
		{
			_node._basis.ThrowNullChildError(string.Format("Args[{0}]", index));
		}
		private GreenArgList AutoBeginEdit()
		{
			if (_egNode == null) {
				_node.AutoThawBasis();
				Debug.Assert(_node._basis is EditableGreenNode);
				_egNode = (EditableGreenNode)_node._basis;
			}
			return new GreenArgList(_egNode);
		}

		#region IList<Node>

		public int Count 
		{
			get { return _node.ArgCount; }
		}
		public void Insert(int index, Node item)
		{
			if (item.HasParent)
				throw new InvalidOperationException(Localize.From("Insert: cannot insert Node '{0}' because it already has a parent.", item._basis));
			var gArgs = AutoBeginEdit();
			// TODO: figure out what to do about positions. User should be able to
			// detach a node from one place and insert it somewhere else and still
			// have the position and source file name intact.
			gArgs.Insert(index, new GreenAtOffs(item._basis));
			_node.HandleChildInserted(index + 1, item);
		}
		public void RemoveAt(int index)
		{
			RemoveRange(index, 1);
		}
		public void RemoveRange(int index, int count)
		{
			var gArgs = AutoBeginEdit();
			gArgs.RemoveRange(index, count);
			_node.HandleRangeRemoved(index + 1, count);
		}
		public Node Detach(int index)
		{
			var orphan = this[index];
			orphan.Detach();
			return orphan;
		}
		public Node this[int index]
		{
			get {
				var n = _node.TryGetArg(index);
				if (n == null) GreenArgList.OutOfRange(_node._basis, index);
				return n;
			}
			set {
				_node.SetChild(1 + index, value);
			}
		}

		#endregion

		#region Pure boilerplate

		public void Clear()
		{
			RemoveRange(0, Count);
		}
		public void Add(Node item)
		{
			Insert(Count, item);
		}
		public int IndexOf(Node item)
		{
			EqualityComparer<Node> comparer = EqualityComparer<Node>.Default;
			for (int i = 0; i < Count; i++)
				if (comparer.Equals(this[i], item))
					return i;
			return -1;
		}
		public bool Contains(Node item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(Node[] array, int arrayIndex)
		{
			for (int i = 0; i < Count; i++)
				array[arrayIndex++] = this[i];
		}
		public bool IsReadOnly
		{
			get { return _node.IsFrozen; }
		}
		public bool Remove(Node item)
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
		public IEnumerator<Node> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}

		#endregion

		#region IListSource<GreenNode>

		public Node TryGet(int index, ref bool fail)
		{
			var g = _node.TryGetArg(index);
			fail = (g == null);
			return g;
		}
		Iterator<Node> IIterable<Node>.GetIterator() { return GetEnumerator().AsIterator(); }

		#endregion
	}

	public struct AttrList 
	{
		Node _node;
		internal AttrList(Node node) { _node = node; }

		internal void RemoveAt(int p)
		{
			throw new NotImplementedException();
		}

		public Node this[Symbol name, Node defaultIfNotFound = null]
		{
			get {
				for (int i = 0, c = _node._basis.AttrCount; i < c; i++) {
					var child = _node._basis.TryGetAttr(i).Node;
					if (child.Name == name)
						return _node.TryGetAttr(i);
				}
				return defaultIfNotFound;
			}
		}
	}


	// Suggested symbols for EC# node names
	// - A node with no arguments and a complex head can be used to represent an 
	//   expression in parenthesis. This is not legal in all situations, e.g.
	//   (Console.WriteLine)("hi") is not legal in EC#.
	// - $'.' as the dot operator, but it's usually written A.B.C instead of #.(A, B, C)
	// - $'#invoke' as the delegate call operator, WHEN THE LEFT HAND SIDE IS AN EXPRESSION.
	//   i.e. f(x) is stored exactly that way, but f(x)(y) is stored as #invoke(f(x), (y))
	// - $'#' holds a list of expressions. If executed, the last expr gives the overall value,
	//   like LISP's progn).
	// - $'#{}' for explicitly-represented code blocks. This different than $'', in that it 
	//   introduces a new scope.
	// - $'#[]' for the indexing operator (as a tag, it holds one or more attributes)
	// - $'#of' to represent an identifier with type arguments
	// - $'#tuple' for tuples.
	// - $'#literal' for all primitive literals including symbols 
	//   (Value == null for null, otherwise Value.GetType() to find out what kind)
	// - Data type names: $'int' for the int keyword, $'double' for the double keyword, etc.
	// - $'#$' for the list-count symbol "$"
	// - $'#\\' (that's a single backslash) for the substitution operator.
	// - $'#::' for the binding operator
	// - $'#var' for other variable declarations: int i=0, j; ==> #var(int, i(0), j)
	// - $'#def' for methods: [Test] void F([required] List<int> list) { return; }
	//   ==> #def(F, #(#var(#of(List, int), list, #[]: required)), void, #(return)))
	// A.B(arg)       ==> #.(A, B(arg))       ==> #.(A, #(B, arg))
	// A.B(arg)(parm) ==> #.(A, B(arg)(parm)) ==> #.(A, #(#(B, arg), parm)


	// Design notes
	/// Life cycle:
	/// 1. Lexer creates flyweight greens for each token, caching as much as possible.
	///    (But parser generator is representation-agnostic and does not create any objects itself.)
	/// 2. Tree parser creates green wrappers.
	/// 3. Parser parses, again producing green tree only. 
	///    Parser outputs a single red node for the root of the source file (Name: #ecs_root)
	/// 
	/// 4. Analysis scans outside-in, 
	///    4a. Building program tree
	///    4b. Expanding macros            - produces lots of intermediate forms!
	///    4c. Reducing complex statements - produces lots of intermediate forms!
	///    4c. Semantic analysis
	/// 5. Back-end
	///    
	/// 
	/// 
	/// Hopeful possibility:
	/// - Red/green tree
	/// - Fast-cloning
	/// - Red node for every single darn token in the program
	/// - But red nodes are small and cheap: head, trivia, attrs, args, width, value all in green tree
	/// - Red tree has: parent, ptr to green, mutable analysis info?
	/// - Mutable green nodes may be larger, optimized on freeze
	/// 
	/// 
	/// 
	/// Cost assessment... suppose:
	/// - about 2/3 of nodes are simple identifiers, punctuation or literals: slightly fewer than the # of tokens
	/// - about 1/8 of identifiers are unique on average: user-defined tokens are less unique, compiler-defined operators more unique
	/// - in a program without comments (which is the worst case), maybe 1/24 of trivia is unique
	/// - each token averages 4 chars
	/// 
	/// Need:
	/// - Terminal flyweight green node: about 6 words + 5 words for Symbol + 6 words for Name of Symbol = 17 words. Somewhat lower cost for literals, except strings
	///   - But only 1/8 unique => just over 17/8 words per token (just over 2)
	///   - Add 1/24 unique trivia => 17/24 words per token
	/// - red node, suppose all are small: say, 6 word minimum. This will be about 6 words per token.
	/// - trivia red nodes created on demand, assume none exist
	/// - 1/3 of green nodes are non-unique parents. Size: about 10 words, Symbol already counted (heads of nodes)
	///   - This is 1 parent per 2 terminals -- half as many as # of tokens
	///   - so 10/2 = 5 words per token
	/// - Total: 2+1+6+3=12 words per non-trivia token. Assuming 4 chars = 4 bytes per non-trivia token, minimum overhead is 12x source file size
	/// - Plus there will be lots of garbage.


}
