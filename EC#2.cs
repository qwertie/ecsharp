// Roadmap:
//
// 1. Printer
// 2. Parser generator
// 3. Parser
// 4. Program tree data structure
// 5. Program tree of references
// 6. Method lookup
// 7. Macro expansion (static methods with untyped args only)
// 8. Registration of variables, properties, methods
// 9. Profit!

////////////////////////////////////////////////////////////////////////////////
//                   ///////////////////////////////////////////////////////////
// EC#: second draft ///////////////////////////////////////////////////////////
//                   ///////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// Idea: write documents for different audiences:
// 1. For ordinary C# developers: what does EC# offer you?
// 2. For language theorists/pundits: how does EC# fit into the language ecosystem?
// 3. For extension makers and language rewriters: discuss the standard syntax tree and how to manipulate it
// 4. Describe LLLPG alone for non-Loyc users

// The first draft was focused on straightforward improvements to C#.
//
// The second draft changes the focus to providing tools that allow users themselves
// to improve C#. These tools will be used to implement many of the features in the
// first draft.
//
// You can think of EC# (second draft), hereafter simply called EC#, as a hybrid 
// language that merges ideas from conventional (Algol-style) programming 
// languages together with ideas from LISP.
//
// EC# enhances C# with the following categories of features:
//
// 1. Compile-time code execution (CTCE)
// 2. A template system (the "parameterized program tree")
// 3. A procedural macro system
// 4. An alias system (which is a minor tweak to the type system)
// 5. Miscellaneous specific syntax enhancements
//
// EC# is mainly a compile-time metaprogramming system built on top of C#, but it
// also provides lots of useful enhancements for people that are not interested in
// metaprogramming. Many of these enhancements are built using the metaprogramming
// facilities, but developers don't have to worry or care about that.
//
// "Metaprogramming" means compile-time code generation; CTCE, templates and macros
// each contribute in a different way to EC#'s metaprogramming system.

////////////////////////////////////////////////////////////////////////////////
//               ///////////////////////////////////////////////////////////////
// How EC# works ///////////////////////////////////////////////////////////////
//               ///////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

/// Overview of the EC# compilation process
/// ---------------------------------------
//
// The phases of compilation are
// 
// 1. Parsing (source code is lexed and parsed into an abstract syntax tree).
// 2. Building the program tree.
// 3. Semantic analysis and building the executable code.
// 4. Converting EC# code to plain C# or a .NET assembly (the back-end).
// 
// Phases 1 to 3 are collectively known as the front-end. The parser is quite 
// separate from the other phases, but phases 2 through 4 may overlap, because in
// order to build the program tree, phase 2 requires some of the executable code to 
// be built in advance, and it may (theoretically) use the back-end to help run
// code at compile-time.
// 
// Thus, the compiler's most difficult responsibility is to convert the syntax tree
// to a "program tree", which is a tree of "spaces" and "members".
// 
// 1. Spaces are addressable code containers, such as namespaces, classes, and 
//    aliases, that can contain members and other spaces. "Addressable" means that 
//    every space has a unique name in the tree, and any part of a program can
//    refer to a particular space. All data types are spaces, but some spaces are
//    not data types. Aliases are a special kind of space that refers to another
//    space, optionally creating a new perspective on that space.
// 2. Members are other named code elements defined in spaces, such as methods,
//    fields, and properties. Members cannot contain spaces (although a macro could
//    be used to simulate creating a local data type inside a method.) The most
//    important kind of member is the method, which contains executable code.
// 
// The program tree is like a file system in which spaces are folders, members are 
// files, and aliases are symbolic links. Some spaces and members can be composed 
// from multiple syntax subtrees; for example, two "partial class" definitions with
// the same name are combined into a single space. Thus, the program tree is a 
// separate entity derived from the syntax tree, but it refers back to some of the
// code that the syntax tree contains.
// 
// So far, this is all very similar to plain C#, but the program tree can also, 
// temporarily, contain executable code outside of any member; in particular it can 
// have macro calls. A macro is executed at compile-time to produce new code that 
// replaces the macro call. The new code, in turn, can also contain macros, which 
// are called at compile-time.
// 
// A macro is just a method with the [macro] attribute. When the compiler
// determines that a method call refers to a macro, it "quotes" the macro's
// arguments instead of evaluating them; these arguments, which have a data type of
// "code" or "node", are passed to the macro, then the macro is executed, and then
// the compiler replaces the macro call with whatever code the macro returned. 
// "node" and "code" refer to the data types ecs.node and ecs.code; respectively,
// these represent a single "program node" and a sequence of them.
// 
// "node" and "code" are real data types that can be used at runtime as well as
// compile-time, if the program has a reference to the assembly that defines them.
// Typically, however, they are only used at compile-time, and they are named with
// lowercase letters to emphasize the fact that they are integrated into EC#.
//
// EC# also automatically calls non-macro methods at compile-time inside any 
// "const" context, e.g. when computing the value of a "const" variable.
//
// Once the program tree is complete, EC# builds any executable code that wasn't
// built in advance (phase 3), and then it converts the program tree to an output 
// language (phase 4). At first, plain C# will be the only output language, but I 
// hope that someday it will be possible to support other backends such as C++
// code, D code, or a .NET assembly. The EC# compiler will be designed to make it 
// straightforward to write and install new backends (not to mention new parsers
// and other compiler components).
//
// CTCE will be limited to a subset of EC#, and this subset will slowly expand as
// EC# is developed. Eventually the goal is to allow you to run any safe code (i.e.
// code that is not marked unsafe) that does not access global variables, subject
// to restrictions on the use of external assemblies (by default, only certain
// whitelisted BCL classes will be accessible, e.g. you will not be able to access
// the file system at compile-time).

/// The difficulties of implementing EC#
/// ------------------------------------
//
// The most obvious difficulty is CTCE. I haven't decided how to implement it yet.
//
// Obviously, in the course of generating the program tree, EC# will often have to
// run parts of the program, and the results of compile-time code execution (CTCE)
// may be used to create new parts of the program. This creates a puzzle in the
// semantics of EC#, because a method running at compile-time could refer to a part
// of the program that has not yet been created. Even worse, the name could
// ambiguously refer both to something that has been created already and to 
// something that has not been created yet. In other words, because the metaprogram
// can use parts of the program being compiled, there can be a circular dependency 
// between the program and the metaprogram. The following example (a variation on a
// "static if" example from the first draft of EC#) illustrates the problem. 
// 
int CallFunction(int x) { return (int)Overloaded(x); }

static_if (CallFunction(2) != 2)
{
	int Overloaded(int j) { return j; }
}
long Overloaded(long i) { return i*i; }

const int C1 = Overloaded(3);   // will C1 be 3 or 9?
Puzzle();                       // creates a new method, Overloaded(int)
const int C2 = CallFunction(3); // will C2 be 3 or 9?

// This code uses the static_if macro:

[macro]
node static_if(bool condition, node then)
{
	return condition ? then : @(());
}

// First, the question arises: which should be evaluated first, C1 or static_if()? 
// The answer is static_if(); the compiler expands macros as soon as possible,
// before analyzing fields or other members, so that other members have access to
// whatever the macro creates. But in order to evaluate the macro, the macro and
// its non-code arguments must be semantically analyzed, and then somehow executed.
// The natural sequence of events is as follows:
// 
// 1. To understand what "CallFunction(2)" means, the compiler looks up this name 
//    in the current scope and finds the definition on the first line.
// 2. The compiler analyzes CallFunction(int). To understand what Overloaded(x)
//    means, the compiler looks up this name in the current scope and finds the
//    definition on the second line, "long Overloaded(long i)".
// 3. The compiler analyzes Overloaded(), then executes "CallFunction(2) != 2".
//    Overloaded(2) returns 4, so CallFunction(2) also returns 4, so the first
//    argument to static_if() is "true".
// 4. static_if() is called, which simply returns the method definition that was 
//    passed to it.
// 5. This new method definition is inserted in place of the original call to 
//    static_if().
// 6. C1 is evaluated. Since Overloaded(int) exists, it is called, so C1 is 3.
// 7. C2 is evaluated. It calls CallFunction(), which the compiler has already 
//    analyzed! Without any obvious reason to repeat the analysis, the compiler 
//    keeps the existing interpretation, so CallFunction() calls Overloaded(long)
//    even though Overloaded(int) is a better match. Therefore, C2 is 9.
//
// Obviously, this behavior is counterintuitive and depends on the implementation
// details of the compiler.
// 
// In my opinion, it would be best to report an error when code like this is used.
// But how can the error be detected? A conservative approach is to remember the
// set of all names that have been looked up so far in a given space. . .


////////////////////////////////////////////////////////////////////////////////
//                    //////////////////////////////////////////////////////////
// Some useful macros //////////////////////////////////////////////////////////
//                    //////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

class TEMP {
	//@@(\x * \y) <=> substitute_code(\0 * \1, x, y)
	//  "\x * \y" <=> substitute_string("{0} * {1}", x, y)
	//  (x, y)    <=> #tuple(x, y)
	//  foo(#,(x, y, z)) => foo(x, y, z) during semantic analysis
	//  { x; y; z; } <=> #{}(x, y, z)
	//  #;(x, y, z) <=> x; y; z; (or { x; y; z; } where a block is required)
	//  the value of #;(...) is the value of its last argument; in order to execute
	//  such a sequence, the other args must be valid as statements in plain C#.

	// Note to self: avoid optimizing the compiler. We will rewrite the compiler
	// in EC# later, which will allow different kinds of optimizations than are
	// currently possible in C#.

	[macro] node error(params node[] args)
	{
		var e = new node($'#error', args);
		return @@( @(\e) );
	}

	[macro($static_if)]
	node StaticIf(bool condition, node then)
	{
		return condition ? then : @(());
	}
	[macro($static_assert)]
	node StaticAssert(node condition)
	{
		return @@{
			static_if(!\condition) {
				@#error("Static assertion failed: {0}", condition);
			}
		};
	}

C	[macro]
	node string_of(node expr)
	{
		if (expr.Count != 0)
			return error("string_of currently allows only a single identifier as its argument.");
		else
			return expr.Name.ToString();
	}

	[macro]
	node operator ??.(node left, node right)
	{
		// a??.b.c parses as (a ??. (b.c)), which expands to
		// (a::tmp != null ? tmp.b.c : null) for some generated symbol "tmp".
		var tmp = unique_name();
		return @@(\left::\tmp != null ? \tmp.\right : null);
	}

	[macro($static_foreach)]
	node StaticForeach(node expr, node body)
	{
		// e.g.
		//     static_foreach(\x in (X, Y, Z)) {
		//         Console.WriteLine("{0} = {1}", string_of(\x), p.\x);
		//     }
		match_syntax(node, \placeholder in (\*substitutions,),
		{
			node results = null;
			for (int i = 0; i < substitutions.Length; i++) {
				var bodyi = FindAndReplace(placeholder, substitutions[i], body);
				results = (results == null ? bodyi : @@{ \results; \bodyi; });
			}
			return results;
		});
	}

	[macro(replace)]
	node Replace(params node[] args)
	{
		var body = args[$-1];
		foreach (var replacement in args[0..$-1])
			match_syntax(replacement, \name = \value) {
				body = FindAndReplace(name, value, body);
			}
		return body;
	}

	[macro(match_syntax)]
	node MatchSyntax(node input, node body) {
	}
	[macro(match_syntax)]
	node MatchSyntax(node input, node pattern, node body)
	{
		return MatchSyntax(input, pattern, body, @(match_syntax_failed(pattern)));
	}
	[macro(match_syntax)]
	node MatchSyntax(node input, node pattern, node success, node fail)
	{
		// example: 
		//     match_syntax(input, \x + \y, success, fail);
		// output:
		// {
		//     var input_ = input;
		//     typeof<(input[0])> x, y;
		//     if (input_.Is($'#+', 2) ? { x = input_[0]; y = input_[1]; true; } : false)
		//         success;
		//     else
		//         fail;
		// }
	}

	// Allow overloading of macros based on characteristics of the code passed in?
	// - Compiler could just call all macros in scope and print an error if more
	//   than one macro does *not* directly return an #error() node
}

////////////////////////////////////////////////////////////////////////////////
//                       ///////////////////////////////////////////////////////
// syntax tree reference //////////////////////////////////////////////////////
//                       ///////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// 1. Beginning of file
using X;         // #import(X); multiple args allowed
using System { Math, Collections.Generic } // #import (System.Math, System.C...)
using Y = X;     // #import(X = Y)
extern alias Z;  // #externalias(Z)
// We use #import to distinguish the using directive from the using() statement.

2. Special cases
- [assembly:Attr] // [Attr] #assembly;
- case ...:       // #case(...);
- default:        // #default; (see also #default(int))
- label_name:     // #label($label_name);

3. Namespace statements
- [Attr] public partial class  Foo<T> : IFoo where ... if ... { ... }
- [Attr] public partial struct Foo<T> : IFoo where ... if ... { ... }
- [Attr] public         enum   Foo    : byte if ... { ... }
- [Attr] public partial trait  Foo<#T>  if ... { ... }
- [Attr] public partial interface Foo<T> : IFoo if ... { ... }
- [Attr]                namespace Foo<#T> { ... }
- [#where(...), #if(...)] #class(Foo<T>, #(IFoo), #{ ... })
- [#if(...)] #enum(Foo<T>, #(byte), #(Fighters = -1, Bar, Baz))
- Common syntax:
  - [Attr] modifier modifier kind name <T, U, V> : base, base { ... }
    - Simplest: kind name { ... }
    - Detection: two adjacent words, optional type parameters, then ':' or '{'
    - Ambiguity: a property also has two words followed by type params and '{'.
      - Solution: parse initially as a namespace, later analysis converts to property
      - Introduce 'prop' contextual keyword to identify properties unambiguously

3. Namespace statements with special syntax (no user-defined versions)
- [Attr]                 using  Foo<T> = Bar;
- [Attr] public partial alias  Foo<T> = Bar : IFoo { ... } // ambiguity with variable declarations
- [Attr] modifier modifier (using | alias) name <T, U, V> = base : base, base { ... }
- [#if(...), #fileScope /*using*/] #alias(Foo<T> = Bar, #(IFoo), #{ ... })
  - Simplest: (using | alias) name = base (: | {)
  - Takes priority over variable declarations.

4. Member and variable declarations
- Events
  - [Attr] public event EventHandler<T> Name;
  - [Attr] public event EventHandler<T> Name { add { ... } remove { ... } }
  - Detection: the keyword 'event' sets it apart from variables and properties.
- Methods, operators and constructors
  - [Attr] public partial void F(...) if ... { ... }
  - [Attr] public       string F(...) if ... ==> Target;
  - [Attr] public partial void F(...);
  - [Attr] public def   string F(...) if ...;
  - [Attr] static Foo<T> operator *(Foo x, Foo y) { ... }
  - [Attr] static Foo<T> operator -(Foo x) { ... }
  - [Attr] public Foo<T> operator -() { ... }
  - [Attr] public Foo<T> operator `-`() { ... }
  - [Attr] public new(...) if ... { ... }
  - [Attr] public ClassName(...) if ... { ... } // ambiguity with method calls (resolvable)
  - Common syntax:
    - [Attr] modifier modifier TYPE name (...) ...
    - Detection: an optional type, then a name followed by '('
    - Ambiguity: "Foo(...)" could be an expression
      - Solution: examine the contents of the parenthesis to see if it could be an argument list
    - Note that the ambiguity is rare. Normally will clearly be a method because there 
      will be a data type, but note that "X < Y > Z (..." could be an expression too.
    - Introduce 'def' contextual keyword to identify methods, operators and constructors unambiguously
- Explicit interface implementation (unique syntax)
  - [Attr] 
- Conversion operators (unique syntax)
  - [Attr] static implicit operator MyType(int i) { ... }
  - [Attr] static explicit operator MyType<#T>(int i) { ... }
  - Detection: "operator" is followed by a data type instead of an operator token
- Destructors
  - [Attr] ~Foo() if ... { ... }
  - Ambiguity: "~Foo()" could be an expression
    - Solution: assume "~Foo()" is a destructor
- Fields and properties
  - [Attr] public Foo<T> X;                     // ambiguity with expressions (takes precedence)
  - [Attr] public Foo<T> X = Y;
  - [Attr] public Foo<T> X ==> Y;
  - [Attr] public Foo<T> X { get { ... } set ==> SetX; }
Common syntaxes:


5. Executable code
- [Attr] expr;
- [Attr] if (...) { ... } // else is a special case
- [Attr] for (...) { ... }
- [Attr] while (...) { ... }
- [Attr] foreach (...) { ... }
- [Attr] switch (...) { ... }
- [Attr] checked { ... }
- [Attr] unchecked { ... }	
- [Attr] using (...) { ... }
- [Attr] fixed (type* ptr = expr) { ... }
- At the statement level only, the form "name (...) {...}" is recognized and rewritten as "name (..., {...});".
  - A braced block cannot be considered a postfix operator; it would be ambiguous in cases such as "f = (X) {...}" or "f = X `test` {...}".
  - A braced block would not work as an optional suffix to the function-call operator either, because it would then require a semicolon to close the statement, unlike all the built-in statements.

6. Executable code with special syntax
- [Attr] return expr;
- [Attr] out expr;
- [Attr] goto case expr;
- [Attr] break label;
- [Attr] continue label;
- [Attr] goto label;
- [Attr] if (...) { ... } else { ... } // no label allowed before "else", "catch", "finally"
- [Attr] do { ... } while (...);
- [Attr] try { ... } catch(...) { ... } finally { ... }


// C# constructor base initializers don't fit into EC#'s flexible perspecitive of 
// the universe. The constructor in
class Foo : Base {
	public Foo(int x) : base(x) {
		Fight();
	}
	public void Fight() { Console.Writeline("Foo fighting!"); }
}
// Is converted by the parser to
public Foo(int x) {
	base(x);
	Fight();
}
// and EC# would be happy to allow
public Foo(int x) {
	Fight();
	base(x);
}
// Except that there is no way to successfully convert it back to plain C#. 
// (I've heard that .NET itself allows it, however.)

////////////////////////////////////////////////////////////////////////////////
//                   ///////////////////////////////////////////////////////////
// compile-time LINQ ///////////////////////////////////////////////////////////
//                   ///////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// Input:
// (new int[] { 1, 2, 3, 4, 5 } using StaticLinq<int>)
//     .Where(x => (x & 1) == 0).Select(x => x * 10)
// 
// Can this be converted into a loop using macros?
// 
// Maybe need a way to have typed macro arguments or runtime macro arguments,
// e.g. in order to resolve macros defined as extension methods.
