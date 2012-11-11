using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.CompilerCore;
using Loyc.Collections;
using Loyc.Utilities;
using ecs;

namespace ecs
{
	using F = CodeFactory;
	using SourceLocation = String;

	/// <summary>
	/// Represents any node in an EC# abstract syntax tree.
	/// </summary>
	/// <remarks>
	/// Needs optimization. I will do that later.
	/// <para/>
	/// The main properties of a node are
	/// 1. Head (a node), which represents the "full name" of a node; never null.
	/// 2. Name (a Symbol), which represents the name of a node if the name is 
	///    simple enough to be expressed as a Symbol. For example, if a node 
	///    represents the variable "foo", Name returns "foo" (as a symbol, denoted
	///    $foo). But if a node represents "String.Empty", Name just returns "#.",
	///    which indicates a "path", i.e. a series of nodes separated by dots.
	///    The name is #literal for any literal value such as 100 or "Hi, mom!".
	///    Head.Name always equals Name, which implies Head.Head.Head.Name==Name.
	///    As they say, it's turtles all the way down.
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
	///    you what category of node you have.
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
	/// include a generic set of tools for semantic analysis.
	/// <para/>
	/// EC#, then, will be the first language to use the Loyc syntax tree 
	/// representation. Most syntax trees are very strongly typed, with separate
	/// data types for, say, variable declarations, binary operators, method calls,
	/// method declarations, unary operators, and so forth. Loyc, however, only has 
	/// this single data type for all nodes. There are several reasons for this:
	/// <para/>
	/// - Simplicity. Many projects have thousands of lines of code dedicated 
	///   to the AST (abstract syntax tree) data structure itself, because each 
	///   kind of AST node has its own class.
	/// - Serializability. Loyc nodes can always be serialized to plain text and 
	///   deserialized back to objects. This makes it easy to visualize syntax
	///   trees or exchange them between programs.
	/// - Extensibility. Loyc nodes can easily represent any language imaginable,
	///   and they are suitable for embedded DSLs (domain-specific languages). 
	///   Since nodes do not enforce a particular structure, they can be used in 
	///   different ways than originally envisioned. For example, most languages 
	///   only have "+" as a binary operator, that is, with two arguments. If  
	///   Loyc had a separate class for each AST, there would probably be a 
	///   PlusOperator class derived from BinaryOperator, or something. But since 
	///   there is only one node class, a "+" operator with three arguments is 
	///   always possible; this is denoted by #+(a, b, c) in EC# source code.
	/// <para/>
	/// Loyc's representation is both an blessing and a curse. The advantage is 
	/// that Loyc nodes can be used for almost any purpose, perhaps even 
	/// representing data instead of code in some cases. However, there is no 
	/// guarantee that a given AST follows the structure prescribed by a particular 
	/// programming language, unless a special validation step is performed after 
	/// parsing.
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
	/// The node class is actually a class hierarchy, with different classes to 
	/// represent different patterns of syntax, but users are expected to always 
	/// use the base class.
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
	/// The syntax tree built from these two representations is identical except
	/// for the "trivia" which includes the "syntax style" and allows the code 
	/// to be printed out in the same form in which it was originally written.
	/// Trivia refers to things that should not affect a program's behavior, such
	/// as whitespace and comments.
	/// <para/>
	/// Prefix notation can be freely mixed with normal EC# code, although usually 
	/// there is no reason to do so:
	/// <para/>
	/// public Point OneTwo = MakePoint(1, 2);
	/// public #var(Point, Origin(MakePoint(0, 0)));
	/// public static #def(MakePoint, #(int x, int y), System.Drawing.Point, {
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
	/// node name of "#@if" is actually "#if", while the node name of "return" is 
	/// actually "#return". Please note that preprocessor directives themselves 
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
	/// <para/>
	/// The notation #(x), with a single argument to #, is exactly equivalent to 
	/// the expression (x). So the syntax tree for (x + 2) * y includes the 
	/// parenthesis using a # node: #*(#(+(x, 2)), y). Prefix notation, or at 
	/// least hybrid notation such as #*(x + 2, y), is the only way to avoid 
	/// encoding the parenthesis themselves into the expression.
	/// <para/>
	/// <para/>
	/// </remarks>
	public abstract partial class Node
	{
		public const SourceLocation UnknownLocation = "Unknown:0";
		public static readonly Node[] EmptyArray = new Node[0];

		protected Node(object basis)
		{
			var bnode = basis as Node;
			if (bnode != null)
				Location = bnode.Location;
			else
				Location = (basis ?? UnknownLocation).ToString();
		}
		protected Symbol _name;
		public Symbol Name { get { return _name; } } // If IsList, returns the Kind of the first item
		public virtual Symbol Kind { get { return Name; } } // #list for a list, Name otherwise
		public virtual object LiteralValue { get { return NonliteralValue.Value; } }
		public SourceLocation Location { get; protected set; }

		public bool IsList { get { return ArgCount > -1; } }
		public bool IsLiteral { get { return Kind == F._Literal; } }
		public bool IsSymbol { get { return this is SymbolNode; } } // IsSymbol => IsIdent or IsKeyword
		public bool IsIdent { get { return Kind.Name[0] != '#'; } }
		public bool IsKeyword { get { return Kind.Name[0] == '#'; } }
		public bool NameIsKeyword { get { return Name.Name[0] == '#'; } }
		public bool Calls(Symbol name, int argCount) { return Name == name && ArgCount == argCount; }
		public bool CallsMin(Symbol name, int argCount) { return Name == name && ArgCount >= argCount; }
		
		// List access
		public abstract int ArgCount { get; }

		public virtual Node Head { get { return this; } }
		public virtual IListSource<Node> Args { get { return EmptyList<Node>.Value; } }

		public virtual Node AddArg(Node value)
		{
			throw new InternalCompilerError(Localize.From("{0}: A list or call was expected here", Location));
		}
	}

	internal class TerminalNode : Node
	{
		public TerminalNode(object basis) : base(basis) { }
		public override int ArgCount { get { return -1; } }
	}
	internal sealed class SymbolNode : TerminalNode
	{
		public SymbolNode(Symbol name, object basis) : base(basis)
		{
			_name = name;
			if (_name == null || _name.Name.Length == 0)
				throw new ArgumentException(Localize.From(
					"{0}: Error: a null or zero-length name symbol is not allowed in a syntax tree.", Location));
		}
	}
	internal sealed class LiteralNode : TerminalNode
	{
		public LiteralNode(object value, object basis) : base(basis)
		{
			_value = value;
			_name = F._Literal;
		}
		object _value;
		public override object LiteralValue { get { return _value; } }
	}
	internal sealed class ListNode : Node // TODO: optimize for common amounts of list items
	{
		public ListNode(Node head, Node[] args, object basis) : base(basis)
		{
			_head = head;
			_args = args;
			_name = _args[0].Kind;
		}
		public Node _head;
		public Node[] _args; // never null
	
		public override Symbol Kind { get { return F._ListKind; } }
		public override Node Head { get { return _head; } }
		public override IListSource<Node> Args { get { return _args.AsListSource(); } }

		public override int ArgCount
		{
			get { return _args.Length; }
		}

		public override string ToString()
		{
			return new NodePrinter(this, 0).PrintStmts(false, false).Result();
		}
	}
	

	// Suggested symbols for EC# node names
	// - $'.' as the dot operator, but it's usually written A.B.C instead of #.(A, B, C)
	// - $'#invoke' as the delegate call operator, WHEN THE LEFT HAND SIDE IS AN EXPRESSION.
	//   i.e. f(x) is stored exactly that way, but f(x)(y) is stored as #invoke(f(x), (y))
	// - $'#' holds a list of expressions. If executed, the last expr gives the overall value,
	//   like LISP's progn).
	// - $'#{}' for explicitly-represented code blocks. This different than $'', in that it 
	//   introduces a new scope.
	// - $'#[]' for the indexing operator (as a tag, it holds one or more attributes)
	// - $'#<>' to represent an identifier with type arguments
	// - $'#tuple' for tuples.
	// - $'#literal' for all primitive literals including symbols 
	//   (Value == null for null, otherwise Value.GetType() to find out what kind)
	// - Data type names: $'int' for the int keyword, $'double' for the double keyword, etc.
	// - $'#$' for the list-count symbol "$"
	// - $'#\\' (that's a single backslash) for the substitution operator.
	// - $'#::' for the binding operator
	// - $'#var' for other variable declarations: int i=0, j; ==> #var(int, i(0), j)
	// - $'#def' for methods: [Test] void F([required] List<int> list) { return; }
	//   ==> #def(F, #(#var(#<>(List, int), list, #[]: required)), void, #(return)))
	// A.B(arg)       ==> #.(A, B(arg))       ==> #.(A, #(B, arg))
	// A.B(arg)(parm) ==> #.(A, B(arg)(parm)) ==> #.(A, #(#(B, arg), parm)

	class NonliteralValue
	{
		private NonliteralValue() { }
		public static readonly NonliteralValue Value = new NonliteralValue();
		public override SourceLocation ToString() { return "#nonliteral"; }
	}

	// Design notes
	/// Life cycle:
	/// 1. Lexer creates flyweight greens for each token, caching as much as possible.
	///    (But parser generator is representation-agnostic and does not create any objects itself.)
	/// 2. Tree parser creates green wrappers.
	/// 3. Parser parses, again producing green tree only. 
	///    Parser outputs a single red node for the root of the source file (Name: #ecsfile)
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
