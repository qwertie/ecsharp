---
title: "Enhanced C# for PL Nerds, Part 3: EC# syntax"
layout: article
toc: true
---

Although I'd like EC# to be an extensible language, C# is far from an ideal starting point for an extensible syntax; when you try to add stuff to C#, it tends to become ambiguous and you may lose backward compatibility. Therefore, the EC# parser is not extensible; instead, I selected a set of syntax changes that are useful for many purposes, while preserving almost total backward compatibility. For example, the new backquoted strings (`foo`) are considered to be operators, and users can define custom operators through this mechanism as long as they surround them with backquotes.

Grammatically, EC# is a very different language from C# except that it "just so happens" to accept virtually all valid C# code. EC# is an expression-based language with two different syntactic styles, "expression style" and "statement style" (it also allows raw token lists for DSLs, but I'll skip those in this article). For any statement there is an equivalent expression in "expression style" (which may or may not use "prefix notation"). And of course, you can put an expression anywhere that a statement is allowed (just add a semicolon at the end).

As I mentioned before, EC# syntax is "generalized C#"; the parser accepts almost anything that looks vaguely like C# code, as well as some other stuff that doesn't look like C# at all.


## Overview: new operators and other junk ######################################

There are some new operators in EC#, which will be discussed in another article.

   - binary "??=": fills a gap in C#. x ??= y is a shortcut for x = x ?? y.
   - binary "??.": safe navigation operator, like in the language Groovy. 
     Its behavior is not built into the compiler, but rather described by a macro.
	- binary "**": exponentiation, e.g. 2 ** 8 = 256. Can be overloaded.
	- binary "in": e.g. x in (1,2,3) checks whether (x==1 || x==2 || x==3).
	  Its behavior is described with a macro.
	- unary "::": (::x) is a shortcut for global::x.
	- unary ".": (.foo) is a valid expression, but it has no predefined meaning 
	  and requires a macro to make sense of it.
	- forwarding operator "==>": "==> foo" is the forwarding operator and this arrow
	  can also be used as a clause on a method.
	- unary "is legal": "expr is legal" checks whether the expression "expr" compiles
	  without errors; it evaluates to a boolean constant. Same precedence as "is".
	- "using" cast operator: a cast that is legal only if it is guaranteed to succeed.
	- suffix and infix `backquoted string`: used to define custom operators.
	- .. range operator: no built-in meaning; must be overloaded.
	- unary dollar sign: $name and $(expr) perform code substitution

In addition there are some new bits of syntax that aren't classified as "operators":

	- Variable declarations in subexpressions, e.g. Console.WriteLine(string s = "Hi")
	- (a, b, c): creates a tuple (normally of type System.Tuple<...>)
	- #X: denotes a special identifier called "#X".
	- #{ a; b; c; }: forces statement notation without creating a new scope
	- { a; b; c; }: forces statement notation and creates a new scope
	- #(a, b, c): specifies a list of expressions. The value of the final expression
	  (in this case, c) is the value of the whole thing.
	- $ keyword: gets the length of an array being indexed.
	- $X: creates a Symbol (of type Loyc.Symbol) named "X"
	- @(code), @{code} and @[code]: creates a syntax tree from the given code.
	- @@(code), @@{code}: creates a syntax tree from the given code, and then 
	  performs isolation and substitution.
	- $"Hello $(e)": substitutes the value of e into the string at run-time.
	- The switch() statement is now reclassified as an expression
	- this(...) constructor syntax
   - Generalized expression syntax (see below)
   - Generalized statement syntax (see below)
	- typeof<e>: a type (not an expression) based on the type of the expression e
	- try a(); finally b(); without braces
	- forwarding clause: int Foo() ==> Bar;
	- "if" clause:       int Foo<$T>() if type T is string { ... }

There is no "comma" operator in EC#. Rather, the comma ',' separates expressions, just as ';' separates statements. Its precise meaning depends on where it appears; at statement level, something like "x = 0, y = z;" is interpreted as two separate statements x = 0 and y = z, except that you can put both statements in a location where one statement was expected:

	if (x)
		x = 0, y = z;

### Generalized expression syntax

Most of the time, EC# expression syntax is the same as C#, with operators like + and += that work just as before. There are some new operators (mentioned above) but the syntax of this new stuff is nothing special, e.g. the new "x in y" operator is no different syntactically than "x + y" or "x && y".

However, at the beginning of every expression there can be a "preamble", so an EC# subexpression is parsed differently at the beginning than "in the middle". The "expression preamble" has the following parts which are all optional and must be provided in order:

1. a single word followed by a colon, e.g. foo:. In statement context, the parser considers it a label (which becomes a separate statement in the Loyc tree); in all other situations it is interpreted as a named argument.
2. a list of attributes, e.g. [x, y] or [x] [y] (the two styles are equivalent). "Macros as attributes", which have the form [[m(args)]], can also appear here; they are parsed like attributes but behave like a method call that takes the remainder of the expression as an argument.
3. either
  3a. a list of modifier keywords (such as public or ref)
  3b. a list of "attribute words" followed by a variable declaration

"attribute words" refers to identifiers ("foo" but not "foo.bar"), "modifier" keywords, and words preceded by a dollar sign (e.g. $x). The "modifier keywords" are listed in the first column of this table of C# keywords:

+-------------------------------------------------------------+
| (allowed as attrs.) |  (not allowed as keyword attributes)  |
|                     |                 |         |           |
| Modifier keywords   | Statement names | Types** | Other***  |
|:-------------------:|:---------------:|:-------:|:---------:|
| abstract            | break           | bool    | operator  |
| const               | case            | byte    | sizeof    |
| explicit            | checked         | char    | typeof    |
| extern              | class           | decimal |:---------:|
| implicit            | continue        | double  | else      |
| internal            | default         | float   | catch     |
| new*                | delegate        | int     | finally   |
| override            | do              | long    |:---------:|
| params              | enum            | object  | in        |
| private             | event           | sbyte   | as        |
| protected           | fixed           | short   | is        |
| public              | for             | string  |:---------:|
| readonly            | foreach         | uint    | base      |
| ref                 | goto            | ulong   | false     |
| sealed              | if              | ushort  | null      |
| static              | interface       | void    | true      |
| unsafe              | lock            |         | this      |
| virtual             | namespace       |         |:---------:|
| volatile            | return          |         | stackalloc|
| out                 | struct          |         |           |
|                     | switch          |         |           |
|                     | throw           |         |           |
|                     | try             |         |           |
|                     | unchecked       |         |           |
|                     | using           |         |           |
|                     | while           |         |           |
+-------------------------------------------------------------+

	*  Allowed on declarations only ('new' is ambiguous otherwise)
	** Type keywords would be unambiguous as attributes on variable 
	   declarations only, but allowing them would be confusing, so you can't
	*** Ambiguous examples showing that these should not be allowed as modifiers:
	    - "A(X) is Y"         could mean "A(X, #{is Y})" at statement level
	    - "if (C) X; else Y;" could mean "{ if (C) X; } { else Y; }"
	    - "this.X = Y"        could mean "this (.X = Y)"
	    - "typeof(X)"         could mean simply (X), with an attribute

For example, 
	
	arg: [Attr] public partial wacky int x = y + z

is a valid EC# expression, i.e. the parser accepts it, although the attributes "public", "partial" and "wacky" may not be meaningful to the rest of the compiler. "arg" is a named argument, "Attr" is a normal attribute, and "int x", of course, is a variable declaration. Since this is an expression, not a statement, it can appear within some other expression, e.g.

   HappyJoy(arg: [Attr] public partial wacky int x = y + z);

The preamble is only allowed at the beginning of a subexpression, so

	error = [Attr] public partial wacky int x = y + z; // ERROR
	
gives a syntax error at [Attr]; you must instead write

	weird = ([Attr] public partial wacky int x = y + z); // OK (but weird)

using '(' to start a new subexpression. ',' also starts a new subexpression, so

	HappyJoy(a, b, arg: [Attr] public partial wacky int x = y + z, c = d);

is parseable ("c" must already exist; it is not part of the variable declaration!). Of course, this is useless in "plain" EC#, and the compiler will give you an error if you directly give it this statement. Instead, a macro is required to interpret the "attributes" and transform them into something that EC# or C# can compile. The non-attribute parts, "arg: int x = y + z" do have a built-in meaning: "arg:" specifies a named argument (i.e. HappyJoy must have an argument called "arg"), and "int x = ..." declares a variable and assigns a value to it.

Type arguments are ambiguous: "A < B > C" could be parsed as the variable declaration "A<B> C" or as a pair of comparisons, "(A < B) > C". To solve this problem, EC# requires that any potential variable declaration

1. is followed by '=' or '=>', e.g. A<B> C = 2, or is at the end of an expression (followed by ';', ',' or ')'
2. that a potential variable declaration whose type uses '<' is either
   2a. Followed by '=',
   2b. Preceded by non-keyword identifier attributes, e.g. "set A<B> C"
   2c. At the beginning of a statement (where '<' is not normally used for comparison),
   2d. Inside the argument list of an apparent method declaration
   2e. Contained in a pair of parenthesis that is followed by '=' or '=>', e.g.
       (x, A<B> C) = (y, D)
       (A<B> C) => C + C

Examples that follow these rules include

	(List<T> a, int b) = (new List<T>(), 7);
	big bad List<T> list;
	foo(List<T> list = new List<T>());
	foo((List<T> b, a) => b.Add(a));

Examples that do not follow these rules include

	int x = int y = 4;     // Syntax error; "int x = (int y = 4);" is legal
	foo(int b + 1);        // Syntax error; "b" should be followed by "="
	foo(big bad wolf * 2); // Syntax error; same problem
	
You might wonder: "hey, why don't you let me put attributes and variable declarations anywhere in the expression?" Well, some locations can't support attributes because certain cases like

	(foo) [a] (bar)
	
are ambiguous. This example could mean either

   1. (foo[a])(bar): get element [a] of foo, and then call it as a delegate.
   2. (foo)([a] bar): apply attribute [a] to bar, then cast bar to type foo.

The C/C++/C# cast syntax, by the way, is an endless source of problems. Whenever I need to know if a proposed syntax is ambiguous, the type cast is one of the first places I look for trouble.

Similarly, if we allow variable declarations anywhere, there are cases like
	
	f(a + B<C> d = e);
	
that are just not worth the headache. This example could mean either
   
	1. f((((a + B) < C) > d) = e);
	2. f((a + (B<C> d)) = e);

The generalized expression syntax is also used to parse argument lists of method declarations. This means that an argument list such as 

	A B(C D, out int X, [E] F G) { ... }

is simply parsed as a list of expressions, and indeed you can use syntax that really doesn't make sense in a method declaration, such as

	A B(a, b, c) { ... }
	A B(int a, abstract Savior = Jesus, getBlubberOf(whale)) { ... }

The compiler rejects but the parser accepts, since a macro could give it meaning later (the [[set]] macro does exactly that, allowing arguments such as "set int X".)

Note: C# 5's "await" is handled specially because it is not a real keyword and doesn't quite fit into this parsing framework.

### Prefix notation

As you've seen, EC# supports a prefix notation that allows you to represent arbitrary Loyc trees:

	[attributes] head(argument_list)

Recall that this represents three of the four parts of a Loyc node (the Value part is missing; literals such as "strings" are the only kind of nodes with values that EC# allows.) In terms of parsing, the (argument_list) has high precedence and binds tightly to the head, while the [attributes] must appear at the far left side of the subexpression and bind to the whole thing.

Prefix notation is simply a kind of expression notation, so you can freely mix prefix notation with ordinary expression notation.

The prefix notation often involves special tokens of the form #X, where X is
1. A C# or EC# identifier
2. A C# keyword
3. A C# or EC# operator
4. A backquoted string
5. One of the following pairs of tokens: {} or []
6. Whitespace, an open open parenthesis '(', or a brace '{' not immediately 
   followed by '}'. In this case, the whitespace, paren or brace is not 
   included in the token.

As it builds the AST, the parser translates all of these forms into a Symbol that also starts with '#'. The following examples show how source code text is translated into symbol name strings:

	#foo     ==> "#foo"         #>>          ==> "#>>"
	#?       ==> "#?"           #{}          ==> "#{}"
	#while   ==> "#while"       #`Newline\n` ==> "#Newline\n"
	@#while  ==> "#while"       #(whatever)  ==> "#"
	#`while` ==> "#while"       

The parser treats all of these forms as a special "hash-keyword" (#keyword) token. A #keyword token is parsed like an identifier, but has the semantics of a keyword. That is, the parser treats it like an identifier, but the rest of the compiler treats it like a keyword. For example, "#struct" has the same meaning as "struct" but the syntax is completely different. The following forms are equivalent:

	struct X : I { int x; }      // standard notation
	#struct(X, #(I), #(int x));  // prefix notation

Ordinary method calls like Foo(x, y) also count as prefix notation; it just so happens that plain C# assigns the same meaning to this notation as EC# does. In fact, syntactically, "#return(7);" is simply a method call to a method called "#return". Although the parser treats it like a method call, it produces the same syntax tree as "return 7;" would have.

The main purpose of #keyword-prefix notation is to show the structure of a syntax tree. Normally you should not use #struct(...), but it is occasionally useful when writing a macro, because it allows you to visualize the syntax tree in order to understand it or to debug problems with it. If a macro produces a syntax tree that cannot be represented by normal EC# code, the code printer (Node.Print()) will automatically display it with prefix notation instead.

So #struct is a keyword that is parsed like an identifier. This is different from the notation @struct which already exists in plain C#; this is an ordinary identifier that has a "@" sign in front to ensure that the parser does not mistake it for a keyword. To the parser, @struct and #struct are almost the same except that the parser removes the @ sign but not the # sign. However, later stages of the compiler treat @struct and #struct in completely different ways.

Since the "#" character is already reserved in plain C# for preprocessor directives, any node name such as "#if" and "#else" that could be mistaken for an old-fashioned preprocessor directive must use "@#" instead of "#" if it is at the beginning of a line. For example, the statement "if (failed) return;" can be represented in prefix notation as "@#if(failed, #return)"; the node name of "@#if" is actually "#if". Please note that preprocessor directives themselves are not part of the normal syntax tree, because they can appear midstatement. For example, this is valid C#:

	if (condition1
		#if DEBUG
			&& condition2
		#endif
		) return;

How to represent these shenanigans is not yet decided. The macro system eliminates the need for ugly tricks like this, but I won't sacrifice backward compatibility.

The special #X tokens don't require an argument list. When a #keyword token lacks an argument list, the parser treats it like a variable name.

Loyc trees that have values cannot be expressed in EC#, except for literal nodes such as "Hello, World!" or 1337 (e.g. the value of the node that represents 1337 is simply (object)1337.) Literal nodes always have the name #literal, and they cannot be expressed in prefix notation (literals can have attributes, though).

Attributes can only appear at the beginning of an expression, so if you want to attach an attribute to "Duke Nukem" in

	hero = "Duke Nukem";

you must use parenthesis to start a new subexpression*:

	hero = ([Attr] "Duke Nukem");

   * this changes the syntax tree slightly by introducing a nested head node, but it's no big deal.

Using '#' unnecessarily, whether it's a preprocessor statement or prefix notation, should be considered poor style because it is an advanced syntax that newbies don't need to know about.
   
### Generalized statement syntax

EC# smooshes "executable statements" and "declarations" into a single grammar. For example, the parser will allow input such as

	Console.WriteLine("Look ma, no method!");
	
	public static void Main(string[] args)
	{
		Console.WriteLine("Hey, boy! What's going on up there?");
	}

By itself, this won't work--you have to put your executable statements into a method--but the parser still understands it. This fact allows you to call macros outside methods, and it allows any kind of quoted code, since we might quote a method, or we might quote an executable statement:

	Node A = @@{
		public static void Main(string[] args) {}
	};
	Node B = @@{
		Console.WriteLine("Look ma, no method!");
	};

There are two syntactic sugars for invoking macros. The first looks like an attribute:

	[[foo(x)]] statement;

This is intended for use outside methods or outside classes, in locations where "normal" attributes like [Serializable] or [Conditional] would appear. After encountering one of these pseudo-attributes, the parser immediately rewrites it as foo(x, statement). If you mix [[Macro_calls]] with normal attributes, only attributes after the macro call are passed to the macro. For example,

	[A] [[B(c)]] [D] 
	void Explode(string reason) { throw new Exception(reason); }

means

	[A] B(c, 
		[D] void Explode(string reason) { throw new Exception(reason); }
	);

but the second form is not actually valid EC# syntax because "Explode" is written in statement form, in a location where a statement is not allowed. Anyway, you get the idea. After the compiler calls the macro B, it adds [A] at the front of the attribute list of the node returned by B.

The second syntactic sugar makes a macro call look similar to a built-in statement, and there are two variations:

	- Method call style: macroName(x) { y; } means foo(x, { y; })
	- Property style:    macroName { y; }    means foo({ y; })

A simple motivating example is the "unless" macro, which is the inverse of "if":

[Macro]
Node unless(Node cond, Node then)
{
	return @@{
		if (!$cond)
			$then;
	};
}

It would be nice if you could use it without braces, like an "if" statement:

unless (x == null) 
	x.Dispose();

In fact, the parser allows it, but braces are preferred. "macroName" can be a dotted name such as "A.B" and it can have type parameters as in "B<C>", but there are ambiguities to resolve, since "B<C>(X) { y; }" could be interpreted as

	(B < C) > ((X) { y; }) ... or as
	B<C> (X, { y; })

and similarly "B<C> { y; } -d;" could be parsed as

	two statements, "B<C> { y; }" and "-d", or as
	one statement, "(B < C) > ({ y; }) - d"

However, the plain C# parser already assumes that B<C>(X) is a method call; based on that assumption, it is reasonable to extend that assumption to macro syntax, using the interpretation B<C> (X, { y; }). The ambiguous input "B<C> { y; }" can be resolved in the same way, by assuming that { y; } is an argument to a macro called B<C>.

Observe that C# does not assign any meaning to a statement of the form

	x (expr) y ...

where x and y are identifiers. Therefore, if the parser sees this form, it can assume that the second part "y ..." is intended to be an argument to the first part, and the interpretation is

	x (expr, { y ...});

Unfortunately, the substatement is ambiguous in general:

	unless(x) -y;         could mean unless(x, { -y; }) or (unless(x)) - y
	unless(x) [y] .z = 1; could mean unless(x, { [y] .z = 1; }) or (unless(x))[y].z = 1

The latter interpretation is used in such cases.

Also, one of the most common mistakes in C# is to forget a semicolon. If the user simply forgets a semicolon in

	foo (expr)
	bar = 0;

then the parser mistakenly nests "bar = 0" inside the call to "foo". There are two terrible things about this:

- The error message may talk about failing to find an overload of foo() with 2 arguments!
- There may be no error message--it may compile successfully, with an unintended meaning caused by calling foo with an extra argument!

To mitigate these problems, the parser issues a "probably missing semicolon" warning when there are no braces and either

	1. The "macro" name does not start with a lowercase letter, or
	2. The "inner" statement is not indented with respect to the outer statement.

Furthermore, since the "name (expr) stmt" syntax is sometimes ambiguous, the parser prints a note, by default, that braces are recommended (one message per source file).

The parser understand the following patterns as macro calls:

	- x (y) z... where z is an identifier
	- x (y) z... where z is a keyword that cannot be an infix operator (not one of: in, as, is, using)
	- x (y) ++z... where z is an identifier
	- x (y) --z... where z is an identifier

Braces are mandatory if you want to start the child statement any other way, e.g.

	- with an opening parenthesis '('
	- with a prefix operator (-, *, dot, etc.)
	- with an attribute ([x] or [[x]])

The contents of a property definition are parsed the same way as statements in a type definition or a method definition. This allows you to place arbitrary statements inside a property definition:

	int X {
		int x;
		get { return x; }
		set { x = value; }
	}

The EC# compiler does not understand code like this, but a macro could.

The parser isn't explicitly programmed to understand that "get" and "set" are special inside the property definition; instead it simply parses "get {...}" and "set {...}" the same way that it would parse "hello {...}" or anything else of that form: as a macro call. Similarly, event definitions can have arbitrary statements; "add" and "remove" are not treated specially. "get" and "set" are treated specially only after parsing, while building the program tree.

### Ambiguities of EC#

The biggest ambiguities of generalized expression syntax have been discussed already, but we warned, the syntactic woes are just beginning.

Here's another one: the new quick-binding operator, ::, clashes with C#'s own scope-resolution operator; X::Y could be creating a variable called Y or referring to an existing symbol in namespace X.

This can be resolved with a postprocessing step after (or maybe during) parsing. We

1. scan the syntax tree for applicable "extern alias" and "using" statements, then
2. find all cases of x::y and change them to x:::y if no alias "x" is been defined. x:::y (with three colons) denotes variable creation unambiguously. This can "mistakenly" replace "::" with ":::" in contrived cases, but it will handle all existing C# code correctly.

Nullable types are almost ambiguous with expressions. Unlimited lookahead is required to find out whether a question mark represents the conditional operator or a nullable type:

int x = (A ? x = a+b+c+...);     // Declare variable x of nullable type A?
int x = (A ? x = a+b+c+... : y); // Conditional operator

Pointer syntax is ambiguous; it looks as if

	T* ptr = stackalloc T[n];

has a multiplication on the left side: (T * ptr) = stackalloc T[n];

That's unlikely (but possible), so the parser just assumes that, at statement level, the patterns X * Y and X * Y = Z declare pointers (when X looks like it might be a type name and Y is a simple identifier). Also, declaring pointers inside expressions is not allowed.

Now what about statements?

EC# smooshes executable statements and declarations together, which tends to create ambiguities, especially after adding the new features of EC#. The statements that use keywords do not cause any important conflicts, since the keywords guarantee a particular interpretation. So all of the following statements are non-issues:
	
	using x;
	class x { ... }
	struct x { ... }
	enum x { ... }
	interface x { ... }
	namespace x { ... }
	event type x;
	if (e) ... else ...
	for (...) ...
	foreach (...) ...
	while (...) ...
	switch (...) ...
	checked { ... }
	unchecked { ... }
	using (e) ...
	fixed (e) ...
	lock (e) ...
	return ...
	goto ...
	break; continue;
	do ... while(e);
	try ... catch ... finally
	case ...:
	default:
	delegate ...;
	
To avoid any problems, generalized expression syntax does not allow any of these keywords to be used as attributes; so, what we really have to worry about are statements that don't necessarily use any keywords.

The basic types of declarations in C# are

	1. Directives (using X; extern alias X;)
	2. Events, delegates, and other keyword-based statements
	3. Space declarations (namespace X.Y {...}, class F : I where... {...})
	4. Method definitions (T F() where... {...}) and declarations (T F();)
	5. Field definitions (A<B> C = D)
	6. Properties (A B { ... })

None of these are ambiguous with expression-statements, except field definitions, but since fields and variable definitions have basically the same syntax, we can use the same rules as plain C#, e.g. L<T> x; is assumed to be a variable declaration and not a pair of comparisons. But now EC#'s generalized expression syntax adds the following twists:

	7. Expressions that contain variable declarations: (x + (int y = 2))
	8. Macro calls that look like built-in statements: (x(y) {z;} and x {z;})
	9. Generalized expressions (expressions that start with "attributes")
	10. Alias statements (alias X = ...)
	11. Trait statements (trait T { ... })
	12. "if" clauses, "def"
	13. Substitution: $(x.ToString())

Due to (7), a statement like

	Foo(A a = c, B b = d);

is ambiguous: it could either (A) declare a constructor called Foo that takes two arguments, or (B) create two variables and invoke a method or macro named Foo. A postprocessing step after (or during) parsing can resolve this by checking whether this "Foo" is enclosed in a type by the same name; EC# also provides a new constructor syntax

	new(int x = 5, int y = 6) {}

which can be interpreted unambiguously as a constructor.

Methods definitions are not usually hard to parse; if you see patterns like

	abstract A B(...);
	abstract A B(...) {...}
	abstract A B(...) ==> ...
	abstract A B(...) where ...

(where A and B are simple or dotted identifiers and 'abstract' could be any number of 'attribute keywords', or none of them) there is only one interpretation: they must be method declarations.

It gets slightly more difficult when you consider methods that return pointers or generics, or that are themselves generic:

	A B<T>(...) ...
	A* B<U>(...) ...
	A<U> B<U>(...) ...
	A<L<U>> B<U>(...) ...
	A<T<V>,U> B<U>(...) ...

The first and last case are not ambiguous, but the others could possibly be expressions. In these cases EC# just assumes that these are method declarations (as you've seen, looking at the contents of the parenthesis does not necessarily help to resolve the ambiguity, so the parser doesn't even try).

It may appear that allowing expressions to start with "keyword attributes" is a problem:

	public Foo(int x = 0); 

This could be a constructor or an expression that starts with a "public" attribute; however it is not the keywords themselves that cause the problem; this case really no different from

	Foo(int x = 0); 

and we have already discussed why this input is difficult. However, non-keyword attributes are ambiguous:

	partial X(int x = 0);

This could be interpreted as a method call with attribute "partial", or as a method that returns a value of type "partial". For this reason, non-keyword attributes are not allowed on most expression-statements, such as method calls; but they are allowed on lots of other things:

	- Variable and field declarations
	- Statements based on keywords (such as "try", "do" and "class")
	- Property definitions
	- Method definitions
	- trait and alias definitions

(8) and (9) can be ambiguous but we've mostly covered this ground already.

The alias statement (10) is a bit of a troublemaker. An "alias" statement looks like this:

	alias Map<K,V> = Dictionary<K,V>
	{
		new V TryGetValue(K key, out V value) {
			if (key != null)
				return base.TryGetValue(key, value);
			value = default(V);
			return false;
		}
	}

Aliases will be interchangeable names for types that can, optionally, modify the set of methods available on a type. In this case the alias Map<K,V> is a synonym for Dictionary<K,V> except that its TryGetValue method never throws exceptions (obviously, this is how it should have worked in the first place). The names are completely interchangeable; you can replace Dictionary<A,B> with Map<A,B> at any random place in any program and it will still compile. The default accessibility is public in an alias, so this is not specified.

Anyway, since alias is not a real keyword, it can be ambiguous. Statements like

	int alias = 0;
	alias = 1;
	alias(x);

are clearly not alias definitions. However, a type named alias will cause trouble:

	struct alias {}         // OK, not ambiguous
	@alias Y = new alias(); // OK
	alias X = Y;            // ERROR: no type found with the name 'Y'

Oops. EC# always assumes that code of the form "alias x = ..." is an alias definition; this technically breaks C# compatibility, but since class names are normally capitalized, such an error is highly unlikely.

(11) is similarly ambiguous, since

	public trait X;
	public trait X { ... }

look like field and property definitions, respectively. EC# assumes instead that these are trait definitions, and this assumption will break compatibility with C# if there is a type defined called "trait". (Note: traits are not yet implemented and are a low priority, since they can be simulated with macros.)

(12) can cause a little trouble when an "if" clause is used with properties and fields:

A P if (C) { ... } // is it a property? or an "if" statement with non-keyword attributes?

Consequently, the "if" statement cannot have non-keyword attributes.

"def" can be used in place of (or in addition to) a method's return value:

def Square(int x) { return x * x; }

This tells the compiler to determine the return type automatically. A warning should be issued if there is a type in scope called "def"; actually, there should really be warnings just for defining types named "alias", "trait", "def", "var" or even "async".

Finally we have (13), dollar-sign-substitution. The rules about substitution are mostly relevant in quoted code (@@{ ... }) but the parser has to be designed to work in all situations. Just as we have to worry about pointer syntax ambiguities even though pointers are rarely used, so too must we be prepared to handle a dollar sign whereever it may appear. It may appear:

	1. Where a statement is expected: $a; substitutes the statement stored in 'a'
	2. Where an expression is expected: foo($a); substitutes the expression in 'a'
	3. Where a type is expected: $a b; creates a variable 'b' whose type depends on 'a'.
	4. Where an attribute is expected: [$a] class foo {}
	5. Where a macro call is expected: $a (x) { y(z); } or [[$a]] int foo(int x);
	6. Where a variable name, method name, property name, or type name is expected:
			int $name;
			int $name(int $arg);
			int $name { get; set; }
			class $name { $name() {} }

Substitution cannot occur where attribute words or keywords are expected, and it cannot be used to insert "class", "struct", etc. into a type definition. And obviously, it cannot replace punctuation:

	$accessibility void f();    // SYNTAX ERROR
	$type X : IEquatable<X> { } // SYNTAX ERROR
	int z = x $operator y;      // WHAT, ARE YOU NUTS?

It's an advanced topic, but you can still express these three ideas with prefix notation.

The substitution operator is not limited to @@(code) blocks, but the dollar sign must be followed immediately (without whitespace) by an identifier or by an open parenthesis, which denotes a subexpression that computes the value to be substituted.

### Returning without return

EC# allows you to embed statements inside expressions; as discussed in Part 2, the final statement in the list becomes the value of the block. Methods also have the same convention, so you can write simply

	int Square(int x) { x*x }
	int PI { get { Math.PI } }

without "return". The compiler will complain if your "expression-statement" is not the final value of the block:

	int Abs(int x) {
		x*x; // ERROR
		if (x < 0)
			-x // OK
		else
			x // OK
	}
