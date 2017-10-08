---
title: "Enhanced C# for PL Nerds, Part 2: Macros and Loyc trees"
layout: article
toc: true
---

TODO: update this documentation, it's **woefully** out of date.

## Features of the macro system ################################################

As I mentioned, a macro is a method that is called at compile time, that takes syntax trees as arguments and returns another syntax tree, which is expanded in-place. A macro can also take values as arguments, which are evaluated as compile-time constants.

The static_if macro is a simple example:

	[SimpleMacro]
	public Node static_if(bool condition, Node then)
	{
		return condition ? then : quote();
	}

As a macro argument, "condition" must be evaluated at compile-time. Here, [SimpleMacro] is given no arguments, which means that the method always acts as a macro and cannot be called as an ordinary method.

By convention, macro names are lowercase to convey their nature as extensions of the programming language, but the parser does not attempt to distinguish macro calls from ordinary method calls; figuring out which is which requires a method lookup--a process that is probably more complex than macro expansion itself.

Here's another simple macro. All it does is run a statement twice:

	[SimpleMacro]
	public Node twice(Node action)
	{
		return s_quote { 
			$action;
			$action;
		};
	}

quote {...} and s_quote {...} cause a block of code to be treated as data; surrounding code with quote {...} or s_quote {...} is called "quoting" the code. This mechanism is similar to C# expression trees, but more flexible: expression trees can only represent simple functions, but quote {...} can contain any code whatsoever: statements, methods, properties, classes, events, "using" directives, and more.

The main difference between quote {...} and s_quote {...} is that s_quote {...} supports substitution (the $ operator). In a s_quote {...} block, the substitution operator $ inserts the value of a variable or expression into the quoted code. The same character is used to insert values into strings:

	MessageBox.Show($"Access to $(filename) has been denied.");

When EC# is complete, you'll be able to define a macro and call it in the same source file (or from another source file in the same project). Order will not matter; you could call static_if above or below its definition in the source code. However, calling a macro defined in the same project requires compile-time code execution (CTCE), which is not yet implemented. Therefore, right now you must define macros in a separate project, compile that project to an assembly (i.e. DLL or dynamic-link library), and then pass the macro assembly as a "compile-time reference" to the compiler.

When macro calls are nested, they are executed outside-in. So if you nest a static_if() within another static_if(), the outer one is executed first, then the inner one--which is the reverse of how normal method calls work. This makes sense, of course, since manipulations by the outer macro can affect the inner macro, or even eliminate the inner macro from existence.

A macro cannot be overloaded with ordinary methods, and a warning is issued if the name lookup process finds any methods that accept the same number of arguments being passed to the macro. In a sense, macros can be overloaded with each other, but a macro itself, rather than the compiler, decides if it applies to the arguments supplied. If a macro does not apply, it simply has to return a quoted @#error() node (as the top-level syntax element), or throw an exception. If exactly one macro does not error out, it is called.

However, if this is not enough to disambiguate between two macros with the same name, you can use the resolution mechanism that is already built into C#: the namespace system. As long as different people define their macros in different namespaces, it's easy to disambiguate them, either using a fully-qualified name, or by not importing two namespaces that have conflicting macros in the first place. Even if two macros in two different DLLs are defined in the same namespace and have the same name, you can still disambiguate them with C#'s "extern alias" feature. You can disambiguate a method call from a macro call with the same mechanisms.

### Macro hygiene

Macros are hygienic by default. Symbols in quoted code (s_quote(...) or s_quote {...}) are resolved in the context where they appear; for instance, when you use this macro:

	[Macro]
	public Node positive(params Node[] content)
	{
		return s_quote(Math.Abs($content));
	}

It will always use global::System.Math.Abs, even if "Math" is redefined as something else at the call site:

	void Oops()
	{
		string Math = "No thanks";
		int seven = positive(-7);   // No problem
	}

Similarly, a macro can safely declare variables:

	[Macro]
	public Node swap(Node a, Node b)
	{
		return s_quote { {
			var temp = $a;
			$a = $b;
			$b = temp;
		} };
	}

Here, s_quote { { ... } } is used to create a new scope. s_quote { { ... } } is not really a special syntax, it is just a block "{...}" nested inside a quotation "s_quote {  }". "s_quote" does not propagate the outer braces to the output, so an extra pair are needed to create a block.

The extra braces are needed because swap() is not isolated from itself. If you call swap() twice, you don't want "var temp" to be declared twice in the same scope, which is illegal according to the plain C# rules inherited by EC#.

However, the braced block {...} doesn't help you in this case:

int temp = 0, zero = 5;
swap(temp, zero);

A naive expansion of this would be silly:

int temp = 0, zero = 5;
{
	var temp = temp; // don't worry, EC# doesn't actually do this.
	temp = zero;
	zero = temp;
}

Luckily, you don't have to worry about that. Even though swap() is not isolated from copies of itself, it is isolated from its caller. Therefore, the plain C# will actually end up looking something like this:

int temp = 0, zero = 5;
{
	var temp_1 = temp;
	temp = zero;
	zero = temp_1;
}

During conversion to plain C#, the name "temp" that came from the macro is renamed to prevent a conflict; however, I don't have the details worked out! I am open to ideas about the exact rules and mechanism that should be used to provide this isolation.

Please note that only "s_quote" quoted code offers isolation (hygiene). quote {...} constructs a syntax tree without any special processing. For example:

	[Macro] Node get_x() { 
		return quote(x);
	}
	void got_x()
	{
		int x = 5;
		get_x() *= 2;
	}
	
Here, get_x() return a non-isolated reference to "x". Therefore, it is able to access the variable "x" from the calling scope. If get_x() returned s_quote(x) instead, the compiler would complain that "x" could not be found because "x" was interpreted in a special context, isolated from the macro call.

'quote' does not support the substitution operator "$". The quote

	quote {
		try {
			$foo;
		} catch {
			Console.WriteLine("foo flubbed!");
		}
	}
	
is parsed successfully, but it does not look up "foo" nor does it substitute its value; it merely captures the syntax of substitution itself. Usually this is not what you want, so be sure to use s_quote {...} when you need substitution to occur.

The difference between s_quote(...) and s_quote {...} is that s_quote(...) accepts a list of expressions, while s_quote {...} accepts a list of statements. Similarly, quote(...) quotes a list of expressions, while @{...} quotes a list of statements.

The "swap" macro above is more powerful than a standard method such as 

	void Swap<T>(ref T a, ref T b) { ... }

because you can swap properties, not just fields:

	var p = new System.Drawing.Point(3, 4);
	swap(p.X, p.Y); // OK

On the other hand, this macro is rather dangerous as-written because it evaluates each of its arguments twice, which is almost never a good idea. In many cases this can harm performance; in rare cases, it can cause incorrect behavior, as in

	var r = new Random();
	swap(array[r.Next(10)], array[r.Next(10)]);

This expands to

	var r = new Random();
	{
		var temp = array[r.Next(10)];
		array[r.Next(10)] = array[r.Next(10)];
		array[r.Next(10)] = temp;
	}

Which, of course, will do something quite weird. Avoiding this problem in swap() would require that the macro create temporary variables to hold intermediate values such as the two r.Next(10)s; but such transformations are too advanced for this introductory article.

Luckily, most macros just read variables and do not change them. Such macros can simply read the value once and store the result in a variable, e.g.

	[Macro]
	public Node square(Node x)
	{
		return s_quote { {
			var tmp = $x;
			tmp * tmp;
		} };
	}

Here we create a temporary variable "tmp" to avoid evaluating "x" twice; the second statement implicitly returns the value of tmp*tmp to the macro's caller.

### Macros versus templates

Of course, "square" is a bad example of a macro because you should really just write an ordinary method:

	public T Square<$T>(T x)
	{
		return x * x;
	}

This template method easily supports any numeric type.

There is a relationship between templates and macros. Both templates and macros generate code; the difference is that macros generate code at the location they are called, while templates generate code remotely. For example, Square<int>(10) generates a Square(int) method in the location where Square<$T> was defined, while square(10) generates the code "{ var tmp = 10; tmp * tmp; }" right where it is called. Another difference is that templates generate code once for a given type argument (or set of arguments), while a macro generates new code each time it is called.

For squaring numbers, it is much more appropriate to use a template than a macro, because

- The code of the macro is longer and more complicated.
- The square() macro will generate more code than Square<$T> and the output C# code may look messy.
- Templates are very similar to C# generics, so C# developers can understand them more easily

In fact, I would always recommend using templates or generic methods instead of macros if it is possible to do so. Sadly, template support is not yet implemented! Therefore, you can use a macro as a temporary substitute. Or, you can write it the old-fashioned way, with copy-and-paste programming:

	public int Square(int x) { return x * x; }
	public long Square(long x) { return x * x; }
	public float Square(float x) { return x * x; }
	public double Square(double x) { return x * x; }

TODO: write a macro:

	[[expand_for(int, long, float, double)]]
	public T Square<$T>(T x) { return x * x; }

## Statement/expression equivalence

From the caller's perspective, it doesn't matter which of the four forms (@@(...), @@{...}, @(...), or @{...}) the macro actually uses; in fact, the macro may use none of them: it could construct syntax trees using Loyc and EC# compiler APIs.

And even though square() produces a block containing two statements, you can still use it inside an expression:

	var r = new Random();
	int teen = 13 + square(r.Next(7));

and this works as expected, because EC# allows statements to be inserted in a location where expressions are expected and vice versa. The macro expands to

	int teen = 13 + {
		var tmp = r.Next(7);
		tmp * tmp
	};
	
which is later converted to plain C# as

	int __0;
	{
		var tmp = r.Next(7);
		__0 = tmp * tmp;
	}
	int teen = 13 + __0;

An important feature of EC# is that the difference between "statements" and "expressions" is only syntactic, not semantic, i.e. the difference is only skin-deep. Any EC# expression can be a statement, and any statement can be written in the form of an expression. The EC# parser needs to know whether to expect expressions or statements in a given location, but once a syntax tree is built, the distinction between statements and expressions disappears.

Of course, plain C# doesn't work that way. Therefore, the conversion to plain C# can sometimes be messy and difficult, as you can imagine from the above code (which is a very simple example).

## List nodes and splicing

A macro can return multiple statements at the outer level (it can also return multiple expressions, which is no different.) For example, suppose you define the following useless macro:

	public static Random _r = new Random();
	[Macro] 
	public static Node passTwoRandomDigitsTo(Node method)
	{
		return @@{
			$method(_r.Next(10));
			$method(_r.Next(10));
		};
	}
	
And suppose you call it like this:

	static void FourRandoms()
	{
		passTwoRandomDigitsTo(Trace.WriteLine);
		passTwoRandomDigitsTo(Trace.WriteLine);
	}
	
Because @@{...} has only a single value but contains two statements, it actually creates the two statements in a special kind of node called a "list", which is denoted #(...) or #{...} in code. Thus, the macro expansion is

	static void FourRandoms()
	{
		#{
			Trace.WriteLine(_r.Next(10));
			Trace.WriteLine(_r.Next(10));
		}
		#{
			Trace.WriteLine(_r.Next(10));
			Trace.WriteLine(_r.Next(10));
		}
	}

Since the concept of a list does not exist in plain C#, the list is eliminated during conversion:

	static void FourRandoms()
	{
		Trace.WriteLine(_r.Next(10));
		Trace.WriteLine(_r.Next(10));
		
		Trace.WriteLine(_r.Next(10));
		Trace.WriteLine(_r.Next(10));
	}

Note that the compiler will ensure that '_r' refers to the Random instance associated with the macro, not to a local variable or something. Also, note that '_r' must be declared public so that it is accessible to anyone that calls the macro.

When a list is used in an expression context, it is evaluated by executing the expressions in the list and then taking the value of the final expression. For example,

	int nine = #(int three = 3, three * three);
   
actually means

	int three = 3;
	int nine = three * three;

and is a horrendously bad style that should not be used. The point is that if I write

	Console.WriteLine("{0}", passTwoRandomDigitsTo(Math.Cos));
	
it will expand to

	Console.WriteLine(#{ Math.Cos(r.Next(10)), Math.Cos(r.Next(10)) });

which is equivalent to

	Math.Cos(r.Next(10));
	Console.WriteLine("{0}", Math.Cos(r.Next(10)));
	
which, of course, makes this useless macro seem even more stupid (if someone can propose a macro that returns two statements but is actually useful, I might use that instead).

Sometimes you don't want it to work this way; sometimes you want it to "splice" a list in the context where it is used. The "splice" macro uses the "Splice" option to make this happen:

	[Macro(Splice = true)]
	Node splice(Node n)
	{
		// This works for lists, but the real explode() also supports tuples
		return n;
	}

So if you write

	double[] digits = new double[] { splice(passTwoRandomDigitsTo(Math.Abs)) };

it expands to 

	double[] digits = new double[] { Math.Abs(r.Next(10)), Math.Abs(r.Next(10)) };

which makes a lot more sense than its meaning without splicing:

	Math.Abs(r.Next(10));
	double[] digits = new double[] { Math.Abs(r.Next(10)) };

You could also add the Splice=true option to passTwoRandomDigitsTo itself, which avoids the need for splice().


The chicken-and-egg problem
---------------------------

Obviously, in the course of generating the program tree, EC# will often have to run parts of the program, and the results of compile-time code execution (CTCE) may be used to create new parts of the program. This creates a puzzle in the semantics of EC#, because a method running at compile-time could refer to a part of the program that has not yet been created. Even worse, the name could ambiguously refer both to something that has been created already and to something that has not been created yet. In other words, because the metaprogram can use parts of the program being compiled, there can be a circular dependency between the program and the metaprogram.

In my opinion, it is crucial to address this problem. The following example illustrates it:

	int CallFunction(int x) { return (int)Overloaded(x); }

	static_if (CallFunction(2) != 2)
	{
		int Overloaded(int j) { return j; }
	}
	long Overloaded(long i) { return i*i; }

	const int C1 = Overloaded(3);   // will C1 be 3 or 9?
	const int C2 = CallFunction(3); // will C2 be 3 or 9?

First, the question arises: which should be evaluated first, C1 or static_if()? The answer is static_if(); the compiler expands macros as soon as possible, before analyzing fields or other members, so that other members have access to whatever the macro creates. But in order to evaluate the macro, the macro itself and its non-code arguments must be semantically analyzed, and then somehow executed. The "natural" sequence of events is as follows:

1. To understand what "CallFunction(2)" means, the compiler forces symbol tables
   to be built for the surrounding code. Then it looks up this name in the 
   current scope and finds the definition on the first line.
2. The compiler analyzes CallFunction(int). To understand what Overloaded(x)
   means, the compiler looks up "Overloaded" in the current scope and finds the
   definition on the second line, "long Overloaded(long i)".
3. The compiler analyzes Overloaded(), then executes "CallFunction(2) != 2".
   Overloaded(2) returns 4, so CallFunction(2) also returns 4, so the first
   argument to static_if() is "true".
4. static_if() is called, which simply returns the method definition that was 
   passed to it.
5. This new method definition is inserted in place of the original call to 
   static_if().
6. C1 is evaluated. Since Overloaded(int) exists, it is called, so C1 is 3.
7. C2 is evaluated. It calls CallFunction(), which the compiler has already 
   analyzed! Without any obvious reason to repeat the analysis, the compiler 
   keeps the existing interpretation, so CallFunction() calls Overloaded(long)
   even though Overloaded(int) is a better match. Therefore, C2 is 9.

However, this behavior is counterintuitive and may depend on the implementation details of the compiler.

In my opinion, it would be best to report an error when code like this is used. But how can the error be detected? I'm planning to use a conservative approach, which remembers the set of all names (with argument counts) that have been looked up so far in a given space. If a method or property by that name is created later (that supports that same number of arguments), the compiler issues an error.

This problem doesn't really exist yet, since you can only call macros in pre-built assemblies. But it will.


## The Loyc syntax tree

In most compilers, the syntax tree is very strongly typed, with separate classes or data structures for, say, variable declarations, binary operators, method calls, method declarations, unary operators, and so forth. Loyc, however, only has a single data type, Node, for all nodes*. There are several reasons for this:

- Simplicity. Many projects have thousands of lines of code dedicated 
  to the AST (abstract syntax tree) data structure itself, because each 
  kind of AST node has its own class. Simplicity means I write less code
  and users learn to use it faster.
- Serializability. Loyc nodes can always be serialized to a plain text 
  "prefix tree" and deserialized back to objects, even by programs that 
  are not designed to handle the language that the tree represents**. This 
  makes it easy to visualize syntax trees or exchange them between 
  programs.
- Extensibility. Loyc nodes can represent any programming language 
  imaginable, and they are suitable for embedded DSLs (domain-specific 
  languages). Since nodes do not enforce a particular structure, they can 
  be used in different ways than originally envisioned. For example, most 
  languages only have "+" as a binary operator, that is, with two arguments. 
  If Loyc had a separate class for each AST, there would probably be a 
  PlusOperator class derived from BinaryOperator, with a LeftChild and a
  RightChild. But since there is only one node class, a "+" operator with 
  three arguments is easy; this is denoted by #+(a, b, c) in EC# source 
  code. The EC# compiler won't understand it, but it might be meaningful
  to another compiler or to a macro.

   * In fact, there are a family of node classes, but this is just an 
     implementation detail.
   ** Currently, the only supported syntax for plain-text Loyc trees is 
     EC# syntax, either normal EC# or prefix-tree notation.

EC# syntax trees are stored in a universal format that I call a "Loyc tree". All nodes in a Loyc tree consist of up to four parts:

1. An attribute list (the Attrs property)
2. A Value
3. A Head or a Name (if a node has a Head, Name refers to Head.Name)
4. An argument list (the Args property)

The EC# language does not allow (2) and (3) to appear together (specifically, a Value can only be represented in source code if the Name is "#literal"), so for most purposes you can think of Value, Head and Name as a discriminated union known informally as "the head part of a node". There is no easy and efficient way to represent a discriminated union in .NET, so all five properties (Attrs, Value, Head, Name, Args) are present on all nodes.

Almost any Loyc node can be expressed in EC# using either "prefix notation" or ordinary code. The basic syntax of prefix notation is

	[attributes] head(argument_list)

where the [attributes] and (argument_list) are both optional, and the head part could be a simple name. For example, the EC# statement

	[Foo] Console.WriteLine("Hello");
	
is a single Node object with three children: Foo, Console.WriteLine, and "Hello". Foo is an attribute, Console.WriteLine is a Head, and "Hello" is an argument. Each of these children is a Node too, but neither Foo nor "Hello" have children of their own. The Head, Console.WriteLine, is a Node named "#." with two arguments, Console and WriteLine. The above statement could be expressed equivalently as

	[Foo] #.(Console, WriteLine)("Hello");

This makes its structure explicit, but the infix dot notation is preferred.

Conceptually, Loyc trees have either a Head node or a Name symbol but not both. Foo, Console, WriteLine, and #. are all node names, while Console.WriteLine is a head node. However, you can always ask a node what its Name is; if the node has a Head rather than a Name, Name returns Head.Name. Thus, #. is the Name of the entire statement.

Attributes can only appear at the beginning of an expression or statement. Use parenthesis to clarify your intention if necessary, but please note that parenthesis are represented explicitly in the syntax tree, not discarded by the parser. Parenthesis cause a node to be inserted into the head of another node, so

	(x())

is a node with no arguments, that has a Head that points to another node that represents x(). Attributes have lower precedence than everything else, and they do not require prefix notation, so 

	[Attr] x = y;

associates the attribute Attr with the "=" node, not with the "x" node.

Unlike C# attributes, EC# attributes can be any list of expressions, and do not imply any particular semantics. You can attach any expression as an attribute to any other statement or expression, e.g.

	[4 * y << z()]
	Console.WriteLine("What is this attribute I see before me?");

When the time comes to generate code, the compiler will warn you that it does not understand what the hell "4 * y << z()" is supposed to mean, but otherwise this statement is legal. Attributes serve as an information side-channel, used for instructions to macros or to the compiler. Macros can use attributes to receive information from users, to store information in a syntax tree temporarily, or to communicate with other macros.
