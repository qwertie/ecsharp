using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Collections;
using System.Diagnostics;
using System.ComponentModel;
using Loyc.Utilities;

namespace Loyc.Syntax
{
	public enum NodeKind { Id, Literal, Call }

	/// <summary>All nodes in a Loyc syntax tree share this base class.</summary>
	/// <remarks>
	/// Loyc defines only three types of nodes: simple symbols, literals, and calls.
	/// <ul>
	/// <li>A <see cref="IdNode"/> is a simple identifier, such as a VariableName</li>
	/// <li>A <see cref="LiteralNode"/> is a literal constant, such as 123 or "hello"</li>
	/// <li>A <see cref="CallNode"/> encompasses all other kinds of nodes, such as
	/// normal function calls like <c>f(x)</c>, generic specifications like <c>f&lt;x></c>
	/// (represented <c>#of(f, x)</c>), braced blocks of statements (represented 
	/// <c>#{}(stmt1, stmt2, ...)</c>), and so on. Also, parenthesized expressions
	/// are represented as a call with one argument and <c>null</c> as the <see cref="Target"/>.</li>
	/// </ul>
	/// This class provides access to all properties of all three types of nodes,
	/// in order to make this class easier to access from plain C#, and to avoid
	/// unnecessary downcasting in some cases.
	/// <para/>
	/// Loyc nodes are typically immutable, except for the 8-bit <see cref="Style"/> 
	/// property which normally affects printing only. If a node allows editing of 
	/// any other properties, <see cref="Frozen"/> returns false.
	/// <para/>
	/// <h3>Background information</h3>
	/// <para/>
	/// EC# (enhanced C#) is intended to be the starting point of the Loyc 
	/// (Language of your choice) project, which will be a family of programming
	/// languages that will share a common representation for the syntax tree and 
	/// other compiler-related data structures.
	/// <para/>
	/// Just as LLVM assembly has emerged as a nearly universal standard 
	/// intermediate representation for back-ends, Loyc nodes are intended to be a 
	/// universal intermediate representation for syntax trees, and Loyc will 
	/// (eventually) include a generic set of tools for semantic analysis so that
	/// it provides a generic representation for front-ends.
	/// <para/>
	/// EC#, then, will be the first language to use the Loyc syntax tree 
	/// representation, known as the "Loyc tree" for short. Most syntax trees are 
	/// very strongly typed, with separate data types for, say, variable 
	/// declarations, binary operators, method calls, method declarations, unary 
	/// operators, and so forth. Loyc, however, defines only three types of Nodes,
	/// and this one class provides access to all the parts of a node. There are 
	/// several reasons for this design:
	/// <ul>
	/// <li>Simplicity. Many projects have thousands of lines of code dedicated 
	///   to the AST (abstract syntax tree) data structure itself, because each 
	///   kind of AST node has its own class.</li>
	/// <li>Serializability. Loyc nodes can always be serialized to a plain text 
	///   "prefix tree" and deserialized back to objects, even by programs that 
	///   are not designed to handle the language that the tree represents*. This 
	///   makes it easy to visualize syntax trees or exchange them between 
	///   programs.</li>
	/// <li>Extensibility. Loyc nodes can represent any language imaginable, and
	///   they are suitable for embedded DSLs (domain-specific languages). Since 
	///   nodes do not enforce a particular structure, they can be used in 
	///   different ways than originally envisioned. For example, most languages 
	///   only have "+" as a binary operator, that is, with two arguments. If  
	///   Loyc had a separate class for each AST, there would probably be a 
	///   PlusOperator class derived from BinaryOperator, or something, with 
	///   properties "Left" and "Right". But since there is only one node class, 
	///   a "+" operator with three arguments is always possible; this is denoted 
	///   by #+(a, b, c) in EC# source code.</li>
	/// </ul>
	///   * Currently, the only supported syntax for plain-text Loyc trees is 
	///     EC# syntax, either normal EC# or prefix-tree notation. As Loyc grows 
	///     in popularity, a more universal syntax should be standardized.
	/// <para/>
	/// Loyc trees are comparable to LISP trees, except that "attributes" and
	/// position information are added to the tree, and the concept of a "list" 
	/// is replaced with the concept of a "call", which I feel is a more 
	/// intuitive notion in most programming languages that are not LISP.
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
	/// the structure <c>#def(return_type, name, args, body)</c>, so if "node" is 
	/// a method definition then <c>node.Args[2]</c> represents the return type, 
	/// for example. In contrast, most compilers have an AST class called 
	/// <c>MethodDefinition</c> or something, that provides properties such as 
	/// Name and ReturnType. Once EC# is developed, however, aliases could help 
	/// avoid this problem by providing a more friendly veneer over the raw nodes.
	/// <para/>
	/// For optimization purposes, the node class is a class hierarchy, but most 
	/// users should only use this class and perhaps the three derived classes
	/// <see cref="IdNode"/>, <see cref="LiteralNode"/> and <see cref="CallNode"/>.
	/// Some users will also find it useful to use <see cref="LNodeFactory"/> for 
	/// generating synthetic code snippets (bits of code that never existed in any 
	/// source file), although you can also use the methods defined here in this
	/// class: <see cref="Id()"/>, <see cref="Literal()"/>, <see cref="Call()"/>,
	/// <see cref="InParens()"/>.
	/// <para/>
	/// Normal <see cref="LNode"/>s are "persistent" in the comp-sci sense, which 
	/// means that a subtree can be shared among multiple syntax trees, and nodes
	/// do not know their own parents. This allows a single node to exist at 
	/// multiple locations in a syntax tree. This makes manipulation of trees 
	/// convenient, as there is no need to "detach" a node from one place, or 
	/// duplicate it, before it can be inserted in another place. Immutable nodes
	/// can be safely re-used within different source files or multiple versions 
	/// of one source file in an IDE's "intellisense" or "code completion" engine.
	///
	/// <h3>Loyc and EC#</h3>
	/// 
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
	/// - there are two nodes with two arguments each, 
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
	/// The prefix notation often involves special identifiers of the form #X, 
	/// where X is
	/// <ol>
	/// <li>1. A C# or EC# identifier</li>
	/// <li>2. A C# keyword</li>
	/// <li>3. A C# or EC# operator</li>
	/// <li>4. A single-quoted string containing one or more characters</li>
	/// <li>5. A backquoted string</li>
	/// <li>6. One of the following pairs of tokens: {}, [], or <> (angle brackets)</li>
	/// <li>7. Nothing. If # is not followed by one of the above, "#" by itself is 
	///        counted as identifier.</li>
	/// </ol>
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
	/// The parser treats all of these forms as "special identifiers". Special
	/// identifiers are parsed like normal identifiers, but are reserved for
	/// things that have special semantic meaning. For example, "#class" has 
	/// the same semantic meaning as "class", although a structure defined with 
	/// "#class" looks quite different from the same structure defined using 
	/// "class". For example, the following forms are equivalent:
	/// <pre>
	/// #class(X, #(), #(int x));
	/// class X { int x; }
	/// </pre>
	/// The #class(...) form is the prefix notation, and it demonstrates the
	/// structure of the Loyc tree for a class declaration.
	/// <para/>
	/// As another example, "#return(7);" is (syntactically) a function call to a 
	/// function called "#return". Although the parser treats it like a function 
	/// call, it produces the same syntax tree as "return 7;" does.
	/// <para/>
	/// Ordinary method calls like <c>Foo(x, y)</c> count as prefix notation, and 
	/// in EC# there is actually a non-prefix notation for this call: <c>x `Foo` y</c>.
	/// Both forms are equivalent, but the infix notation can only be used when you
	/// are calling a method that takes two arguments (also, the string `Foo` must 
	/// be a simple identifier; it cannot contain dots or have generic arguments.)
	/// <para/>
	/// So #class is a keyword that is parsed like an identifier, but this is 
	/// different from the notation @class which already exists in plain C#.
	/// @class is an ordinary identifier that has a "@" sign in front to ensure 
	/// that the compiler does not treat it like a keyword at all. #class is a 
	/// special identifier that is parsed like an identifier but then treated like 
	/// a keyword after parsing is complete.
	/// <para/>
	/// In other words, to the parser, @struct and #struct are the same except that 
	/// the parser removes the @ sign but not the # sign. However, later stages of 
	/// the compiler treat @struct (now stored without the @ sign) and #struct quite 
	/// differently, as <c>#struct</c> is treated like a keyword and <c>struct</c> 
	/// is not.
	/// <para/>
	/// Since the "#" character is already reserved in plain C# for preprocessor 
	/// directives, any node name such as "#if" and "#else" that could be mistaken
	/// for an old-fashioned preprocessor directive must be preceded by "@" at the 
	/// beginning of a line. For example, the statement "if (failed) return;" can 
	/// be represented in prefix notation as "@#if(failed, return)", although the 
	/// node name of "@#if" is actually "#if" (while the node name of the 
	/// preprocessor directive "#if" would be "##if", and the node name of "return"
	/// is actually "#return"). Please note that preprocessor directives themselves 
	/// are not part of the normal syntax tree, because they can appear 
	/// midstatement. For example, this is valid C#:
	/// <pre>
	/// if (condition1
	///    #if DEBUG
	///       && condition2
	///    #endif
	///    ) return;
	/// </pre>
	/// Preprocessor statements will be processed early in the compiler and then 
	/// deleted.
	/// <para/>
	/// The special #X tokens don't require an argument list, although the compiler
	/// expects most of them to have one (and often it must have a specific length).
	/// This doesn't matter for parsing, however, only for later stages of analysis.
	/// <para/>
	/// Any statement or expression can have attributes attached to it; when 
	/// attributes are seen beside a statement, they are attached to the root node 
	/// of that statement. In this example, the attribute is attached to the = 
	/// operator:
	/// <pre>
	/// [PointlessAttribute(true)] x = y * 2;
	/// </pre>
	/// Attributes are allowed not just at the beginning of a statement, but at the 
	/// beginning of any subexpression in parenthesis:
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
	/// (assembly and module attributes must be special-cased anyway, since it 
	/// doesn't make sense for them to be attached to whatever follows them.)
	/// <para/>
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
	///			#return((char)('A' - 10 + value)),
	///			#return((char)('0' + value)));
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
	///	public int Value { get { _value } }
	/// <para/>
	/// The EC# if-else and switch statements (but not loops) work the same way, 
	/// and you can put a braced block in the middle of any expression:
	/// <pre>
	/// int hexChar = {
	///			if ((uint)value >= 10)
	///				'A' - 10
	///			else
	///				'0'
	///		} + value;
	/// </pre>
	/// The braced block is represented by a #{} node, which introduces a new scope.
	/// In contrast, the special # node type (known as the "list keyword"), does 
	/// not create a new scope. It can be used with expression or statement syntax:
	/// <pre>
	/// var three = #(Console.WriteLine("Fetching the three!"), 3);
	/// var eight = #{ int x = 5; three + x };
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
	/// 
	/// <h3>The reimplementation</h3>
	/// 
	/// This implementation has been redesigned (in Subversion, the last version
	/// based on the old design is revision 289.) The core concept is the same as 
	/// described in my blog at
	/// http://loyc-etc.blogspot.ca/2013/04/the-loyc-tree-and-prefix-notation-in-ec.html
	/// except that the concept of a "Head" has mostly been eliminated, although
	/// you might see it occasionally because it still has a meaning. The "head"
	/// of a node refers either to the Name of a symbol, the Value of a literal,
	/// or the Target of a call (i.e. the name of the method being called, which
	/// could be an arbitrarily complex node). In the original implementation, it 
	/// was also possible to have a complex head (a head that is itself a node) 
	/// even when the node was not a call; this situation was used to represent
	/// an expression in parenthesis.
	/// <para/>
	/// This didn't quite feel right, so I changed it. Now, only calls can be
	/// complex, and the head of a call (the method being called) is called the
	/// Target.
	/// <para/>
	/// In the new version, there are explicitly three types of nodes: symbols, 
	/// literals, and calls. There is no longer a Head property, instead there 
	/// are three separate properties for the three kinds of heads, <see 
	/// cref="Name"/> (a Symbol), <see cref="Value"/> (an Object), and <see 
	/// cref="Target"/> (an LNode). Only call nodes have a Target, and only 
	/// literal nodes have a Value (as an optimization, <see 
	/// cref="StdTriviaNode"/> breaks this rule; it can only do this because it
	/// represents special attributes that are outside the normal syntax tree,
	/// such as comments). Symbol nodes have a Name, but I thought it would be 
	/// useful for some call nodes to also have a Name, which is defined as the 
	/// name of the Target if the Target is a symbol (if the Target is not a 
	/// symbol, the Name must be blank.)
	/// <para/>
	/// An expression in parenthesis is now represented by a call with a blank
	/// name (use <see cref="IsParenthesizedExpr"/> to detect this case; it is
	/// incorrect to test <c><see cref="Name"/> == $``</c> because a call with 
	/// a non-symbol Target also has a blank name.)
	/// <para/>
	/// The following differences in implementation have been made:
	/// <ul>
	/// <li>"Red" and "green" nodes have basically been eliminated, at least for now.</li>
	/// <li>Nodes normally do not contain parent references anymore</li>
	/// <li>Mutable nodes have been eliminated, for now.</li>
	/// <li>There are now three standard subclasses, <see cref="IdNode"/>,
	///     <see cref="LiteralNode"/> and <see cref="CallNode"/>, and a node
	///     can no longer change between classes after it is created.</li>
	/// <li>An empty Name is now allowed. A literal now has a blank name (instead 
	///     of #literal) and a method that calls anything other than a simple symbol
	///     will also have a blank Name. Note:
	///     The <see cref="Name"/> property will still never return null.</li>
	/// <li>As mentioned, an expression in parenthesis is represented differently.</li>
	/// </ul>
	/// The problems that motivated a redesign are described at
	/// http://loyc-etc.blogspot.ca/2013/05/redesigning-loyc-tree-code.html
	/// <para/>
	/// One very common use of mutable nodes is building lists of statements, e.g. 
	/// you might create an empty braced block or an empty loop and then add 
	/// statements to the body of the block or loop. To do this without mutable 
	/// nodes, create a mutable <see cref="RWList{LNode}"/> instead and add 
	/// statements there; once the list is finished, create the braced block or
	/// loop afterward. The new design stores arguments and attributes in 
	/// <see cref="RVList{LNode}"/> objects; you can instantly convert your WList 
	/// to a VList by calling <see cref="RWList{LNode}.ToRVList()"/>.
	/// <para/>
	/// During the redesign I've decided on some small changes to the representation
	/// of certain expressions in EC#.
	/// <ul>
	/// <li>The '.' operator is now treated more like a normal binary operator; 
	///     <c>a.b.c</c> is now represented <c>#.(#.(a, b), c)</c> rather than 
	///     <c>#.(a, b, c)</c> mainly because it's easier that way, and because the 
	///     second representation doesn't buy anything significant other than a 
	///     need for special-casing.</li>
	/// <li>(TODO) <c>int x = 0</c> will now be represented <c>#var(int, x = 0)</c>
	///     rather than <c>#var(int, x(0))</c>. I chose the latter representation 
	///     initially because it is slightly more convenient, because you can 
	///     always learn the name of the declared variable by calling 
	///     <c>var.Args[1].Name</c>. However, I decided that it was more important
	///     for the syntax tree to be predictable, with obvious connections between
	///     normal and prefix notations. Since I decided that <c>alias X = Y;</c> 
	///     was to be represented <c>#alias(X = Y, #())</c>, it made sense for the 
	///     syntax tree of a variable declaration to also resemble its C# syntax. 
	///     There's another small reason: C++ has both styles <c>Foo x(y)</c> and 
	///     <c>Foo x = y</c>; if Loyc were to ever support C++, it would make sense 
	///     to use <c>#var(Foo, x(y))</c> and <c>#var(Foo, x = y)</c> for these two 
	///     cases, and I believe C#'s variable declarations are semantically closer 
	///     to the latter.</li>
	/// <li>An constructor argument list is required on <i>all</i> types using the #new
	///     operator, e.g. <c>new int[] { x }</c> must have an empty set of arguments
	///     on int[], i.e. <c>#new(#of(#[],int)(), x)</c>; this rule makes the 
	///     different kinds of new expressions easier to interpret by making them 
	///     consistent with each other.</li>
	/// <li>A missing syntax element is now represented by an empty symbol instead 
	///     of the symbol #missing.</li>
	/// <li>I've decided to adopt the generics syntax from Nemerle as an unambiguous
	///     alternative to angle brackets: List.[int] means List&lt;int> and the 
	///     printer will use this syntax in cases where angle brackets are ambiguous.</li>
	/// <li>By popular demand, constructors will be written this(...) instead
	///     of new(...), since both D and Nemerle use the latter notation.</li>
	/// <li>(TODO) Swap \ and $ characters</li>
	/// </ul>
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
	/// <para/>
	/// The argument and attribute lists cannot be null, since they have type 
	/// <see cref="RVList{Node}"/> which is a struct.
	/// 
	/// <h3>Note</h3>
	/// 
	/// The argument and attribute lists should never contain null nodes. However,
	/// there is currently no code to ensure that null entries are not placed in 
	/// these lists (such an invariant is difficult to achieve since the argument
	/// list is stored in <see cref="RVList{T}"/>, a general-purpose data type, 
	/// not in a specialized list designed just for this class). Any code that 
	/// uses nulls should be considered buggy and fixed.
	/// </remarks>
	[DebuggerDisplay("{ToString()}")]
	public abstract class LNode : ICloneable<LNode>, IEquatable<LNode>
	{
		#region Constructors and static node creator methods

		protected LNode(LNode prototype)
		{
			RAS = prototype.RAS;
		}
		protected LNode(SourceRange range, NodeStyle style)
		{
			RAS = new RangeAndStyle(range, style);
			if (RAS.Source == null)
				RAS.Source = SyntheticSource;
		}

		public static readonly EmptySourceFile SyntheticSource = new EmptySourceFile("<SyntheticCode>");

		public static readonly IdNode Missing = Id(ecs.CodeSymbols.Missing);

		public static IdNode Id(Symbol name, SourceRange range) { return new StdIdNode(name, range); }
		public static IdNode Id(string name, SourceRange range) { return new StdIdNode(GSymbol.Get(name), range); }
		public static IdNode Id(RVList<LNode> attrs, Symbol name, SourceRange range) { return new StdIdNodeWithAttrs(attrs, name, range); }
		public static IdNode Id(RVList<LNode> attrs, string name, SourceRange range) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), range); }
		public static StdLiteralNode Literal(object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, range, style); }
		public static StdLiteralNode Literal(RVList<LNode> attrs, object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, range, style); }
		public static StdCallNode Call(Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, range, style); }
		public static StdCallNode Call(LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, range, style); }
		public static StdCallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new  StdSimpleCallNodeWithAttrs(attrs, name, args, range, style); }
		public static StdCallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, range, style); }
		public static StdCallNode InParens(LNode node, SourceRange range) { return new StdComplexCallNode(Missing, new RVList<LNode>(node), range); }
		public static StdCallNode InParens(RVList<LNode> attrs, LNode node, SourceRange range) { return new StdComplexCallNodeWithAttrs(attrs, Missing, new RVList<LNode>(node), range); }

		public static IdNode Id(Symbol name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNode(name, new SourceRange(file, position, width)); }
		public static IdNode Id(string name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNode(GSymbol.Get(name), new SourceRange(file, position, width)); }
		public static IdNode Id(RVList<LNode> attrs, Symbol name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNodeWithAttrs(attrs, name, new SourceRange(file, position, width)); }
		public static IdNode Id(RVList<LNode> attrs, string name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), new SourceRange(file, position, width)); }
		public static StdLiteralNode Literal(object value, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file, position, width), style); }
		public static StdLiteralNode Literal(RVList<LNode> attrs, object value, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(Symbol name, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(LNode target, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNodeWithAttrs(attrs, name, args, new SourceRange(file, position, width), style); }
		public static StdCallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, new SourceRange(file, position, width), style); }
		public static StdCallNode InParens(LNode node, ISourceFile file = null, int position = -1, int width = -1) { return new StdComplexCallNode(null, new RVList<LNode>(node), new SourceRange(file, position, width)); }
		public static StdCallNode InParens(RVList<LNode> attrs, LNode node, ISourceFile file = null, int position = -1, int width = -1) { return new StdComplexCallNodeWithAttrs(attrs, null, new RVList<LNode>(node), new SourceRange(file, position, width)); }

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
		[DebuggerDisplay("ToString()")]
		public virtual SourceRange Range { get { return (SourceRange)RAS; } }
		/// <summary>Returns the source file (shortcut for <c><see cref="Range"/>.Source</c>).</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ISourceFile Source { get { return RAS.Source; } }

		/// <summary>Indicates the preferred style to use when printing the node to a text string.</summary>
		/// <remarks>
		/// The Style is an 8-bit value that acts as a hint to the node printer about 
		/// how the node should be printed. Custom display styles that do not fit in 
		/// the Style property can be expressed with special attributes that have a
		/// <see cref="Name"/> starting with "#trivia_". ("#trivia" attributes, which
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

		/// <summary>Returns the attribute list for this node.</summary>
		public virtual RVList<LNode> Attrs { get { return RVList<LNode>.Empty; } }

		/// <summary>Returns true if the node is immutable, and false if any part of it can be edited.</summary>
		public virtual bool IsFrozen { get { return true; } }
		
		/// <summary>Returns the <see cref="NodeKind"/>: Symbol, Literal, or Call.</summary>
		public abstract NodeKind Kind { get; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsCall { get { return Kind == NodeKind.Call; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsId { get { return Kind == NodeKind.Id; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsLiteral { get { return Kind == NodeKind.Literal; } }

		#endregion

		#region Properties and methods for Symbol nodes (and simple calls)

		/// <summary>Returns the Symbol if <see cref="IsId"/>. If this node is 
		/// a call (<see cref="IsCall"/>) and <c>Target.IsId</c> is true, 
		/// this property returns <c>Target.Name</c>. In all other cases, the name
		/// is <see cref="GSymbol.Empty"/>. Shall not return null.</summary>
		public abstract Symbol Name { get; }

		/// <summary>Returns true if <see cref="Name"/> starts with '#'.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasSpecialName { get { string n = Name.Name; return n.Length > 0 && n[0] == '#'; } }

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
		/// is false. The target can be a symbol with no name (<see cref="GSymbol.Empty"/>)
		/// to represent a parenthesized expression, if there is one argument.</summary>
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
		public CallNode WithArgs(params LNode[] args) { return WithArgs(new RVList<LNode>(args)); }
		public LNode PlusAttr(LNode attr) { return WithAttrs(Attrs.Add(attr)); }
		public LNode PlusAttrs(RVList<LNode> attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrs(IEnumerable<LNode> attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrs(params LNode[] attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusArg(LNode arg) { return WithArgs(Args.Add(arg)); }
		public LNode PlusArgs(RVList<LNode> args) { return WithArgs(Args.AddRange(args)); }
		public LNode PlusArgs(IEnumerable<LNode> args) { return WithArgs(Args.AddRange(args)); }
		public LNode PlusArgs(params LNode[] args) { return WithArgs(Args.AddRange(args)); }
		public LNode WithArgChanged(int index, LNode newValue)
		{
			CheckParam.IsNotNull("newValue", newValue);
			var a = Args;
			a[index] = newValue;
			return WithArgs(a);
		}
		public LNode WithAttrChanged(int index, LNode newValue)
		{
			CheckParam.IsNotNull("newValue", newValue);
			var a = Attrs;
			a[index] = newValue;
			return WithAttrs(a);
		}

		#endregion

		public abstract void Call(LNodeVisitor visitor);
		public abstract void Call(ILNodeVisitor visitor);

		#region Other stuff

		public virtual string Print(NodeStyle style = NodeStyle.Statement, string indentString = "\t", string lineSeparator = "\n")
		{
			return NodePrinter.Print(this, style, indentString, lineSeparator).ToString();
		}
		public override string ToString()
		{
			return Print();
		}

		/// <inheritdoc cref="Equals(LNode, bool)"/>
		public static bool Equals(LNode a, LNode b, bool compareStyles = false)
		{
			if (a == b)
				return true;
			if (a == null)
				return false;
			return a.Equals(b, compareStyles);
		}
		/// <summary>Compares two lists of nodes for structural equality.</summary>
		/// <param name="compareStyles">Whether to compare values of <see cref="Style"/></param>
		/// <remarks>Position information is not compared.</remarks>
		public static bool Equals(RVList<LNode> a, RVList<LNode> b, bool compareStyles = false)
		{
			if (a.Count != b.Count)
				return false;
			while (!a.IsEmpty)
				if (!Equals(a.Pop(), b.Pop()))
					return false;
			return true;
		}

		/// <summary>Compares two nodes for structural equality. Two green nodes 
		/// are considered equal if they have the same name, the same value, the
		/// same arguments, and the same attributes. IsCall must be the same, but
		/// they need not have the same values of SourceWidth or IsFrozen.</summary>
		/// <param name="compareStyles">Whether to compare values of <see cref="Style"/></param>
		/// <remarks>Position information (<see cref="Range"/>) is not compared.</remarks>
		public abstract bool Equals(LNode other, bool compareStyles);
		public bool Equals(LNode other) { return Equals(other, false); }
		public override bool Equals(object other) { var b = other as LNode; return Equals(b, false); }
		protected internal abstract int GetHashCode(int recurse, int styleMask);
		/// <summary>Gets the hash code based on the structure of the tree.</summary>
		/// <remarks>
		/// If the tree is large, less than the entire tree is scanned to produce 
		/// the hashcode (in the absolute worst case, about 4000 nodes are examined, 
		/// but usually it is less than 100).
		/// </remarks>
		public override int GetHashCode() { return GetHashCode(3, 0); }

		/// <summary>An IEqualityComparer that compares nodes structurally.</summary>
		public class DeepComparer : IEqualityComparer<LNode>
		{
			public static readonly DeepComparer Value = new DeepComparer(false);
			public static readonly DeepComparer WithStyleCompare = new DeepComparer(true);

			bool _compareStyles;
			public DeepComparer(bool compareStyles) { _compareStyles = compareStyles; }

			public bool Equals(LNode x, LNode y)
			{
				return LNode.Equals(x, y, _compareStyles);
			}
			public int GetHashCode(LNode node)
			{
				return node.GetHashCode(3, _compareStyles ? 0xFF : 0);
			}
		}

		protected internal void ThrowIfFrozen()
		{
			if (IsFrozen)
				throw new ReadOnlyException(string.Format("The node '{0}' is frozen against modification.", ToString()));
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int ArgCount { get { return Args.Count; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public int AttrCount { get { return Attrs.Count; } }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool HasAttrs { get { return Attrs.Count != 0; } }

		public bool HasPAttrs()
		{
			var a = Attrs;
			for (int i = 0, c = a.Count; i < c; i++)
				if (a[i].IsPrintableAttr())
					return true;
			return false;
		}
		public bool IsPrintableAttr()
		{
			return !Name.Name.StartsWith("#trivia_");
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual bool IsParenthesizedExpr             { get  { Debug.Assert(!IsCall); return false; } }
		public virtual bool Calls(Symbol name, int argCount)       { Debug.Assert(!IsCall); return false; }
		public virtual bool Calls(Symbol name)                     { Debug.Assert(!IsCall); return false; }
		public virtual bool CallsMin(Symbol name, int argCount)    { Debug.Assert(!IsCall); return false; }
		public virtual bool HasSimpleHead()                        { Debug.Assert(!IsCall); return true; }
		public virtual bool HasSimpleHeadWithoutPAttrs()           { Debug.Assert(!IsCall); return true; }
		public virtual LNode WithArgs(Func<LNode, LNode> selector) { Debug.Assert(!IsCall); return this; }
		public virtual LNode Unparenthesized()                     { Debug.Assert(!IsCall); return this; }
		public virtual bool IsIdWithoutPAttrs()                    { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdWithoutPAttrs(Symbol name)         { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdNamed(Symbol name)                 { Debug.Assert(!IsId); return false; }

		/// <summary>Some <see cref="CallNode"/>s are used to represent lists. This 
		/// method merges two nodes, forming or appending a list (see remarks).</summary>
		/// <param name="node1">First node, list, or null.</param>
		/// <param name="node2">Second node, list, or null.</param>
		/// <param name="listName">The Name used to detect whether a node is a list
		/// (typically "#"). Any other name is considered a normal call, not a list.
		/// If this method creates a list from two non-lists, this parameter 
		/// specifies the Name that the list will have.</param>
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
				return LNode.Call(listName, new RVList<LNode>(node1, node2));
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
	}
}