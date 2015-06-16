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
	public enum LNodeKind { Id, Literal, Call }

	/// <summary>Signature for a method that serializes a Loyc tree to text. Each
	/// programming language will have one.</summary>
	/// <param name="node">Node to print</param>
	/// <param name="target">Output buffer</param>
	/// <param name="mode">A language-specific way of modifying printer behavior.
	/// The printer should ignore the mode object if it does not not understand it.</param>
	/// <param name="indentString">A string to print for each level of indentation, such as a tab or four spaces.</param>
	/// <param name="lineSeparator">Line separator, typically "\n" or "\r\n".</param>
	/// <remarks>This delegate only prints to a StringBuilder. Printing directly to 
	/// a stream requires language-specific facilities.</remarks>
	public delegate void LNodePrinter(LNode node, StringBuilder target, IMessageSink errors, object mode = null, string indentString = "\t", string lineSeparator = "\n");

	/// <summary>All nodes in a Loyc syntax tree share this base class.</summary>
	/// <remarks>
	/// Loyc defines only three types of nodes: simple symbols, literals, and calls.
	/// <ul>
	/// <li>A <see cref="IdNode"/> is a simple identifier, such as a VariableName</li>
	/// <li>A <see cref="LiteralNode"/> is a literal constant, such as 123 or "hello"</li>
	/// <li>A <see cref="CallNode"/> encompasses all other kinds of nodes, such as
	/// normal function calls like <c>f(x)</c>, generic specifications like <c>f&lt;x></c>
	/// (represented as <c>#of(f, x)</c>), braced blocks of statements (represented as
	/// <c>@`{}`(stmt1, stmt2, ...)</c>), and so on. Also, parenthesized expressions
	/// are represented as a call with one argument and <c>null</c> as the <see cref="Target"/>.</li>
	/// </ul>
	/// This class provides access to all properties of all three types of nodes,
	/// in order to make this class easier to access from plain C#, and to avoid
	/// unnecessary downcasting in some cases.
	/// <para/>
	/// Loyc nodes are always immutable, except for the 8-bit <see cref="Style"/> 
	/// property which normally affects printing only.
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
	/// <li>Simplicity. Many compilers have thousands of lines of code dedicated 
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
	///   * Currently, the only supported languages for plain-text Loyc trees are 
	///     LES and EC# (either normal EC# or prefix-tree notation).
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
	/// the structure <c>#fn(return_type, name, args, body)</c>, so if "node" is 
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
	/// class: <see cref="Id"/>(), <see cref="Literal"/>(), <see cref="Call"/>(),
	/// <see cref="InParens"/>().
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
	/// @=(x, @*(y, 2)) is equivalent to the expression x = y * 2. This notation 
	/// tells you that 
	/// - there are two nodes with two arguments each, 
	/// - the outer one is named "=" and the inner is named "*"
	/// The syntax tree built from these two representations is identical.
	/// <para/>
	/// Prefix notation can be freely mixed with normal EC# code, although usually 
	/// there is no reason to do so:
	/// <para/>
	/// public Point OneTwo = MakePoint(1, 2);
	/// public #var(Point, Origin(MakePoint(0, 0)));
	/// public static #fn(MakePoint, #(int x, int y), System.Drawing.Point, #{
	///		return new Point(x, y);
	///	});
	/// <para/>
	/// The prefix notation often involves special identifiers of the form #X, 
	/// where X is an identifier (or keyword). In the LES and EC# parsers, the # 
	/// character is considered a normal part of an identifier, no different than
	/// a letter (A-Z) or an underscore (_). However, the # is "special" in the
	/// sense that when a special construct (like a method, or a field declaration)
	/// is converted to a Loyc tree, the Loyc tree represents that special 
	/// construct using an identifier that starts with "#".
	/// <para/>
	/// Code in prefix notation also often has identifiers of the form @X, where X is
	/// <ol>
	/// <li>An operator such as + or ==</li>
	/// <li>A C# keyword</li>
	/// <li>A backquoted string</li>
	/// </ol>
	/// The @ character is <i>not</i> part of the identifier; it merely indicates
	/// that the characters after the @ symbol represent an identifier, rather than
	/// an operator or keyword. For example, "==" is normally an operator, and 
	/// "class" is normally a keyword, but @== and @class are both identifiers,
	/// not operators or keywords.
	/// <para/>
	/// The following examples show how source code text is translated into symbol names:
	/// <pre>
	/// #foo     ==> "#foo"        @>>           ==> ">>"
	/// #?       ==> "?"           @`{}`         ==> "{}"
	/// #while   ==> "#while"      @`#Newline\n` ==> "#Newline\n"
	/// @#while  ==> "#while"      @`hi there!`  ==> "hi there!"
	/// @while   ==> "while"
	/// </pre>
	/// Identifiers that start with # are parsed like normal identifiers, but 
	/// are reserved for things that have special semantic meaning. For example, 
	/// "#class" is treated by the parser like any other identifier, but it has
	/// the same semantic meaning as "class", although a structure defined with 
	/// "#class" looks quite different from the same structure defined using 
	/// "class". For example, the following forms are equivalent:
	/// <pre>
	/// #class(X, #(), @`{}`(int x));
	/// class X { int x; }
	/// </pre>
	/// The #class(...) form uses prefix notation, and it demonstrates the
	/// structure of the Loyc tree for a class declaration.
	/// <para/>
	/// As another example, "#return(7);" is syntactically a function call to a 
	/// function called "#return". Although the parser treats it like a function 
	/// call, it produces the same syntax tree as "return 7;" does.
	/// <para/>
	/// Ordinary method calls like <c>Foo(x, y)</c> count as prefix notation, and 
	/// in EC# there is actually a non-prefix notation for this call: <c>x `Foo` y</c>.
	/// Both forms are equivalent, but the infix notation can only be used when you
	/// are calling a method that takes two arguments (also, the string `Foo` should
	/// be a simple identifier; if it contains a dot, it is treated as part of the
	/// identifier name, not as a normal dot operator.)
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
	///       &amp;&amp; condition2
	///    #endif
	///    ) return;
	/// </pre>
	/// Preprocessor statements will be processed early in the compiler and then 
	/// deleted.
	/// <para/>
	/// The special #X tokens don't require an argument list, although the compiler
	/// expects most of them to have one (and often it must have a specific length).
	/// Again, this doesn't matter for parsing, only for later stages of analysis.
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
	///	[#public, #static] #fn(HexDigitChar, #(#var(int, value)), #char, #
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
	/// The braced block is represented by a {} node, which introduces a new scope.
	/// In contrast, the special # node type (the "list marker"), does not create 
	/// a new scope. It is normally used with expression syntax:
	/// <pre>
	/// var three = #(Console.WriteLine("Fetching the three!"), 3);
	/// var eight = #(int x = 5, three + x);
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
	/// to a VList by calling <see cref="WListBase{LNode}.ToRVList()"/>.
	/// <para/>
	/// During the redesign I've decided on some small changes to the representation
	/// of certain expressions in EC#.
	/// <ul>
	/// <li>The '.' operator is now treated more like a normal binary operator; 
	///     <c>a.b.c</c> is now represented <c>#.(#.(a, b), c)</c> rather than 
	///     <c>#.(a, b, c)</c> mainly because it's easier that way, and because the 
	///     second representation doesn't buy anything significant other than a 
	///     need for special-casing.</li>
	/// <li><c>int x = 0</c> will now be represented <c>#var(int, x = 0)</c>
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
	///     to the latter. (Note: another possibility was #var(int, x) = 0, but I 
	///     decided this wasn't an improvement, it would just shift the pain around.)</li>
	/// <li>An constructor argument list is required on <i>all</i> types using the #new
	///     operator, e.g. <c>new int[] { x }</c> must have an empty set of arguments
	///     on int[], i.e. <c>#new(#of(#[],int)(), x)</c>; this rule makes the 
	///     different kinds of new expressions easier to interpret by making them 
	///     consistent with each other.</li>
	/// <li>A missing syntax element is now represented by an empty symbol instead 
	///     of the symbol #missing.</li>
	/// <li>I've decided to adopt the "in-expression" generics syntax from Nemerle 
	///     as an unambiguous alternative to angle brackets: List.[int] means 
	///     List&lt;int> and the printer will use this syntax in cases where angle 
	///     brackets are ambiguous.</li>
	/// <li>By popular demand, constructors will be written this(...) instead
	///     of new(...), since both D and Nemerle use the latter notation.</li>
	/// <li>The \ and $ characters have been swapped; \S now denotes a symbol S,
	///     while $S now denotes a substitution.</li>
	/// <li>The \ and $ characters have been swapped; \S now denotes a symbol S,
	///     while $S now denotes a substitution. Originally EC# was designed just
	///     as an extension of C#, so \ made sense as a substitution operator for
	///     string interpolation because it doesn't hurt backward compatibility:
	///     "Loaded '\(filename)' successfully". But now that my focus has shifted 
	///     to multi-language interoperability, $ makes more sense, as it is used 
	///     for string interpolation in at least five other languages and it makes
	///     sense to use the same character for both string substitution and code
	///     substitution.</li>
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
	/// <para/>
	/// <c>LNode</c> implements <see cref="INegListSource{T}"/>, so you can loop 
	/// through all children of the node like this:
	/// <code>
	/// for (int i = node.Min; i &lt;= node.Max; i++) {
	///     LNode child = node[i];
	/// }
	/// </code>
	/// You can also use <c>foreach</c>. The children are numbered like this:
	/// <ul>
	/// <li>if i is less than -1, node[i] refers to an attribute. Specifically, node[i] 
	/// means node.Attrs[i + node.Attrs.Count + 1] in that case.</li>
	/// <li>node[-1] refers to <c>Target</c> (but throws if there is no target)</li>
	/// <li>Non-negative values are indexes of Args, e.g. node[i] means node.Args[i].</li>
	/// </ul>
	/// LNode also provides <c>Select(child => result)</c> and <c>ReplaceRecursive(child => result)</c>
	/// methods which allows you to transform all children (Atrrs, Target and Args).
	/// Currently there is no <c>Where(child => bool)</c> method because it is not
	/// possible to remove the <see cref="Target"/> of an LNode (you can still use
	/// standard LINQ Where(), of course, but the result is not an LNode).
	/// 
	/// <h3>Note</h3>
	/// 
	/// The argument and attribute lists should never contain null nodes. Any code 
	/// that puts nulls in <see cref="Args"/> or <see cref="Attrs"/> is buggy. 
	/// However, we can't ensure nulls are not placed into <see cref="RVList{T}"/> 
	/// since it's a general-purpose data type, not specialized for LNode. There is
	/// code to ensure nulls are not placed in Args and Attrs (<see cref="NoNulls"/>),
	/// but only in debug builds, since null-checking is fairly expensive.
	/// </remarks>
	[DebuggerDisplay("{ToString()}")]
	public abstract class LNode : ICloneable<LNode>, IEquatable<LNode>, IHasLocation, IHasValue<object>, INegListSource<LNode>
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

		public static readonly IdNode Missing = Id(CodeSymbols.Missing);

		public static IdNode Id(Symbol name, LNode prototype) { return new StdIdNode(name, prototype); }
		public static IdNode Id(string name, LNode prototype) { return new StdIdNode(GSymbol.Get(name), prototype); }
		public static IdNode Id(RVList<LNode> attrs, Symbol name, LNode prototype) { return new StdIdNodeWithAttrs(attrs, name, prototype); }
		public static IdNode Id(RVList<LNode> attrs, string name, LNode prototype) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), prototype); }
		public static LiteralNode Literal(object value, LNode prototype) { return new StdLiteralNode(value, prototype); }
		public static LiteralNode Literal(RVList<LNode> attrs, object value, LNode prototype) { return new StdLiteralNode(value, prototype); }
		public static CallNode Call(Symbol name, LNode prototype) { return new StdSimpleCallNode(name, RVList<LNode>.Empty, prototype); }
		public static CallNode Call(LNode target, LNode prototype) { return new StdComplexCallNode(target, RVList<LNode>.Empty, prototype); }
		public static CallNode Call(Symbol name, RVList<LNode> args, LNode prototype) { return new StdSimpleCallNode(name, args, prototype); }
		public static CallNode Call(LNode target, RVList<LNode> args, LNode prototype) { return new StdComplexCallNode(target, args, prototype); }
		public static CallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, LNode prototype) { return new  StdSimpleCallNodeWithAttrs(attrs, name, args, prototype); }
		public static CallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, LNode prototype) { return new StdComplexCallNodeWithAttrs(attrs, target, args, prototype); }
		public static CallNode Trivia(Symbol name, object value, LNode prototype) { return new StdTriviaNode(name, value, prototype); }
		public static LNode InParens(LNode node) { return node.PlusAttr(Id(CodeSymbols.TriviaInParens, node.Range)); }

		public static IdNode Id(Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNode(name, range, style); }
		public static IdNode Id(string name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNode(GSymbol.Get(name), range, style); }
		public static IdNode Id(RVList<LNode> attrs, Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNodeWithAttrs(attrs, name, range, style); }
		public static IdNode Id(RVList<LNode> attrs, string name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), range, style); }
		public static LiteralNode Literal(object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, range, style); }
		public static LiteralNode Literal(RVList<LNode> attrs, object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdLiteralNodeWithAttrs(attrs, value, range, style); }
		public static CallNode Call(Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, RVList<LNode>.Empty, range, style); }
		public static CallNode Call(LNode target, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, RVList<LNode>.Empty, range, style); }
		public static CallNode Call(Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, range, style); }
		public static CallNode Call(LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, range, style); }
		public static CallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNodeWithAttrs(attrs, name, args, range, style); }
		public static CallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, range, style); }
		public static CallNode Trivia(Symbol name, object value, SourceRange range, NodeStyle style = NodeStyle.Default) { return new StdTriviaNode(name, value, range, style); }
		public static LNode InParens(LNode node, SourceRange range) { return node.PlusAttr(Id(CodeSymbols.TriviaInParens, range)); }

		public static IdNode Id(Symbol name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNode(name, new SourceRange(file, position, width)); }
		public static IdNode Id(string name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNode(GSymbol.Get(name), new SourceRange(file, position, width)); }
		public static IdNode Id(RVList<LNode> attrs, Symbol name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNodeWithAttrs(attrs, name, new SourceRange(file, position, width)); }
		public static IdNode Id(RVList<LNode> attrs, string name, ISourceFile file = null, int position = -1, int width = -1) { return new StdIdNodeWithAttrs(attrs, GSymbol.Get(name), new SourceRange(file, position, width)); }
		public static LiteralNode Literal(object value, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file, position, width), style); }
		public static LiteralNode Literal(RVList<LNode> attrs, object value, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdLiteralNode(value, new SourceRange(file, position, width), style); }
		public static CallNode Call(Symbol name, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, RVList<LNode>.Empty, new SourceRange(file, position, width), style); }
		public static CallNode Call(LNode target, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, RVList<LNode>.Empty, new SourceRange(file, position, width), style); }
		public static CallNode Call(Symbol name, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNode(name, args, new SourceRange(file, position, width), style); }
		public static CallNode Call(LNode target, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNode(target, args, new SourceRange(file, position, width), style); }
		public static CallNode Call(RVList<LNode> attrs, Symbol name, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdSimpleCallNodeWithAttrs(attrs, name, args, new SourceRange(file, position, width), style); }
		public static CallNode Call(RVList<LNode> attrs, LNode target, RVList<LNode> args, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdComplexCallNodeWithAttrs(attrs, target, args, new SourceRange(file, position, width), style); }
		public static CallNode Trivia(Symbol name, object value, ISourceFile file = null, int position = -1, int width = -1, NodeStyle style = NodeStyle.Default) { return new StdTriviaNode(name, value, new SourceRange(file, position, width), style); }
		public static LNode InParens(LNode node, ISourceFile file = null, int position = -1, int width = -1) { return node.PlusAttr(Id(CodeSymbols.TriviaInParens, file, position, width)); }

		// It's difficult to enforce "nulls not allowed" with high performance.
		// Compromise: only check in debug builds. This is called by node types
		// that accept lists of args or attrs.
		[Conditional("DEBUG")]
		protected static void NoNulls(RVList<LNode> node, string propName)
		{
			for (int i = 0, c = node.Count; i < c; i++) {
				if (node[i] == null)
					throw new ArgumentNullException(propName, string.Format("Attempted to construct an LNode with a null reference at {0}[{1}].", propName, i));
			}
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

			const int StyleShift = 23;
			const int NonWidthBits = 32 - StyleShift;
			const int LengthMask = (1 << StyleShift) - 1;
			const int MutableFlag = unchecked((int)0x80000000);

			public int Length { 
				[DebuggerStepThrough] get { return _stuff & LengthMask; }
				[DebuggerStepThrough] set { _stuff = (_stuff & ~LengthMask) | value; }
			}
			public int EndIndex { get { return StartIndex + Length; } }
			public NodeStyle Style {
				[DebuggerStepThrough] get { return (NodeStyle)(_stuff >> StyleShift); }
				[DebuggerStepThrough] set { _stuff = (_stuff & ~(0xFF << StyleShift)) | ((int)value << StyleShift); }
			}
			public bool IsMutable { get { return (_stuff & MutableFlag) != 0; } }
			public void MarkFrozen() { _stuff &= ~MutableFlag; }
			public void MarkMutable() { _stuff |= MutableFlag; }

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
		[DebuggerDisplay("ToString()")]
		public virtual SourceRange Range { get { return (SourceRange)RAS; } }
		/// <summary>Returns the source file (shortcut for <c><see cref="Range"/>.Source</c>).</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public ISourceFile Source { get { return RAS.Source; } }

		/// <summary>Gets Range.Begin.ToString().</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object IHasLocation.Location
		{
			get { return Range.Start; }
		}

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
		public virtual RVList<LNode> Attrs { get { return RVList<LNode>.Empty; } }

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
		public abstract Symbol Name { get; }

		/// <summary>Returns true if <see cref="Name"/> starts with '#'.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasSpecialName { get { string n = Name.Name; return n.Length > 0 && n[0] == '#'; } }

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

		/// <summary>Returns the argument list of this node. Always empty when <c><see cref="IsCall"/>==false</c>.</summary>
		/// <remarks>
		/// Depending on the <see cref="Target"/>, Args may represent an actual 
		/// argument list, or it may represent some other kind of list. For 
		/// example, if the target is "{}" then Args represents a list of 
		/// statements in a braced block, and if the target is ">=" then Args 
		/// represents the two arguments to the ">=" operator.
		/// </remarks>
		public abstract RVList<LNode> Args { get; }

		public virtual CallNode WithTarget(LNode target)                { return With(target, Args); }
		public virtual CallNode WithTarget(Symbol name)                 { return With(name, Args); }
		
		/// <summary>Creates a Node with a new argument list. If this node is not a 
		/// call, a new node is created using this node as its target. Otherwise,
		/// the existing argument list is replaced.</summary>
		/// <param name="args">New argument list</param>
		public abstract CallNode WithArgs(RVList<LNode> args);

		/// <summary>Creates a <see cref="CallNode"/> with the same attributes and 
		/// <see cref="Range"/>, but a different target and argument list. If the 
		/// current node is not a CallNode, it becomes one (the Range, Style and 
		/// attributes of the current node are kept, but the Kind, Value, and 
		/// Name are discarded.)</summary>
		public virtual CallNode With(LNode target, RVList<LNode> args)
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
		public virtual CallNode With(Symbol target, RVList<LNode> args)
		{
			var attrs = Attrs;
			if (attrs.Count == 0)
				return new StdSimpleCallNode(target, args, this);
			else
				return new StdSimpleCallNodeWithAttrs(attrs, target, args, this);
		}
		public CallNode With(Symbol target, params LNode[] args) { return With(target, new RVList<LNode>(args)); }

		#endregion

		#region Other WithXyz methods, and Clone()

		/// <summary>Creates a copy of the node. Since nodes are immutable, there 
		/// is little reason for an end-user to call this, but Clone() is used 
		/// internally as a helper method by the WithXyz() methods.</summary>
		public abstract LNode Clone();
		
		public LNode WithRange(SourceRange range) { return With(range, Style); }
		public LNode WithRange(int startIndex, int endIndex)
		{
			var copy = Clone();
			copy.RAS = new RangeAndStyle(RAS.Source, startIndex, endIndex-startIndex, RAS.Style);
			return copy;
		}
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
		public LNode PlusAttrs(RVList<LNode> attrs) { return attrs.IsEmpty ? this : WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrs(IEnumerable<LNode> attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusAttrs(params LNode[] attrs) { return WithAttrs(Attrs.AddRange(attrs)); }
		public LNode PlusArg(LNode arg) { return WithArgs(Args.Add(arg)); }
		public LNode PlusArgs(RVList<LNode> args) { return args.IsEmpty ? this : WithArgs(Args.AddRange(args)); }
		public LNode PlusArgs(IEnumerable<LNode> args) { return WithArgs(Args.AddRange(args)); }
		public LNode PlusArgs(params LNode[] args) { return WithArgs(Args.AddRange(args)); }
		public LNode WithArgChanged(int index, LNode newValue)
		{
			CheckParam.IsNotNull("newValue", newValue);
			var a = Args;
			if (newValue != a[index]) {
				a[index] = newValue;
				return WithArgs(a);
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

		#endregion

		public abstract void Call(LNodeVisitor visitor);
		public abstract void Call(ILNodeVisitor visitor);

		#region Node printer service (used by ToString())

		static LNodePrinter _defaultPrinter = Loyc.Syntax.Les.LesNodePrinter.Printer;
		static ThreadLocalVariable<LNodePrinter> _printer = new ThreadLocalVariable<LNodePrinter>();

		/// <summary>Gets or sets the default node printer on the current thread,
		/// which controls how nodes are serialized to text by default.</summary>
		/// <remarks>The LES printer is the default, and will be used if you try
		/// to set this property to null.</remarks>
		public static LNodePrinter Printer
		{
			get { return _printer.Value ?? _defaultPrinter; }
			set { _printer.Value = value; }
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
		public static PushedPrinter PushPrinter(LNodePrinter printer) { return new PushedPrinter(printer); }
		/// <summary>Returned by <see cref="PushPrinter(LNodePrinter)"/>.</summary>
		public struct PushedPrinter : IDisposable
		{
			LNodePrinter old;
			public PushedPrinter(LNodePrinter @new) { old = Printer; Printer = @new; }
			public void Dispose() { Printer = old; }
		}

		public virtual string Print(object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			StringBuilder sb = new StringBuilder();
			Printer(this, sb, MessageSink.Null, mode, indentString, lineSeparator);
			return sb.ToString();
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
				RVList<LNode> args = Args;
				if (args.Count >= 1)
					return args[0].Value;
				return NoValue.Value;
			}
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool HasTokenValue { get { return TriviaValue != NoValue.Value; } }

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
				if (!a[i].IsTrivia)
					return true;
			return false;
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public bool IsTrivia
		{
			get { return CodeSymbols.IsTriviaSymbol(Name); }
		}

		public virtual bool Calls(Symbol name, int argCount)       { Debug.Assert(!IsCall); return false; }
		public virtual bool Calls(Symbol name)                     { Debug.Assert(!IsCall); return false; }
		public virtual bool CallsMin(Symbol name, int argCount)    { Debug.Assert(!IsCall); return false; }
		/// <summary>Returns true if the Target is an Id or a Literal, and the Target has no attributes.</summary>
		public virtual bool HasSimpleHead()                        { Debug.Assert(!IsCall); return true; }
		/// <summary>Returns true if the Target is an Id or a Literal, and the Target has only trivia attributes.</summary>
		public virtual bool HasSimpleHeadWithoutPAttrs()           { Debug.Assert(!IsCall); return true; }
		
		public LNode WithAttrs(Func<LNode, Maybe<LNode>> selector) {
			RVList<LNode> attrs = Attrs, newAttrs = attrs.WhereSelect(selector);
			if (attrs == newAttrs)
				return this;
			return WithAttrs(newAttrs);
		}
		public virtual LNode WithArgs(Func<LNode, Maybe<LNode>> selector) { Debug.Assert(!IsCall); return this; }
		
		public virtual bool IsIdWithoutPAttrs()                    { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdWithoutPAttrs(Symbol name)         { Debug.Assert(!IsId); return false; }
		public virtual bool IsIdNamed(Symbol name)                 { Debug.Assert(!IsId); return false; }
		public virtual bool IsParenthesizedExpr()
		{
			return this.AttrNamed(CodeSymbols.TriviaInParens) != null;
		}

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
			} else {
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
			get { throw new NotImplementedException(); }
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

		/// <summary>Transforms the attributes, <see cref="Target"/>, and parameters 
		/// of an LNode, returning another LNode of the same Kind. If the selector
		/// makes no changes, Select() returns <c>this</c>.</summary>
		/// <remarks>The selector is not allowed to return null.</remarks>
		public abstract LNode Select(Func<LNode, LNode> selector);
		
		/// <summary>Performs a recursive find-and-replace operation, by attempting
		/// to replace each child (among <see cref="Attrs"/>, <see cref="Target"/>, 
		/// <see cref="Args"/>) using the specified selector. This method can also
		/// be used for simple searching, by giving a selector that always returns 
		/// null.</summary>
		/// <param name="selector">The selector is called for each descendant, and
		/// optionally the root node. If the selector returns a node, the new node 
		/// replaces the node that was passed to <c>selector</c> and the children of 
		/// the new node are ignored. If the selector returns null, children of the 
		/// child are scanned recursively.</param>
		/// <param name="replaceRoot">Whether to call <c>selector(this)</c>.</param>
		public abstract LNode ReplaceRecursive(Func<LNode, LNode> selector, bool replaceRoot = true);
	}
}