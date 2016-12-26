---
title: "LeMP Macro Reference: Code generation & compile-time decision-making"
tagline: Standard macros in the LeMP namespace
layout: article
date: 20 Mar 2016
toc: true
---

Introduction
------------

One of the main uses of LeMP is generating large amounts of code that follow obvious patterns.

- The `static` macros (`static if`, `static deconstruct`, `static matchCode`, `staticMatches`) make decisions at compile-time by analyzing syntax.
- The `replace`, `define` and `unroll` macros are typically used to generate user-defined code, but `replace` and  `define` may also useful for other tasks, such as implementing optimizations or doing syntactically predictable refactorings.
- The `alt class` macro is the only macro on this page designed for a particular use case: generating disjoint union types in the form of class hierarchies.

In addition, there is a macro for the unary `$` operator, which looks up and inserts a syntax variable captured by `static deconstruct`, `static tryDeconstruct` or the `` `staticMatches` `` operator. (This works differently from `static matchCode`, because `static matchCode` performs replacements "in advance" inside the "handler" below the matching `case`, whereas `$` replaces lazily. In most cases, however, the net effect is the same.)

### alt class: Algebraic Data Type ###

~~~csharp
alt class Color { 
  alt this(byte Red, byte Green, byte Blue, byte Opacity = 255);
}
void DrawLine(Color c) {
  if (c.Opacity > 0) {
    (var r, var g, var b) = c;
    ...
  }
}
~~~

Expands a short description of an 'algebraic data type' (a.k.a. disjoint union) into a set of classes with a common base class. All data members are read-only, and for each member (e.g. `Item1` and `Item2` above), a `With()` method is generated to let users create modified versions. Example:`

<div class='sbs' markdown='1'>
~~~csharp
// A binary tree
public partial abstract alt class Tree<T> 
	where T: IComparable<T>
{
  alt this(T Value);
  alt Leaf();
  alt Node(Tree<T> Left, Tree<T> Right);
}
~~~

~~~csharp
// Output of LeMP
// A binary tree
public partial abstract class Tree<T>
 where T: IComparable<T> {
  public Tree(T Value) {
    this.Value = Value;
  }
  public T Value { get; private set; }
  public abstract Tree<T>
  WithValue(T newValue);
  [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public T Item1 {
    get {
      return Value;
    }
  }
}
// A binary tree
public partial abstract static partial class Tree {
  public static Tree<T>
  New<T>(T Value) where T: IComparable<T> {
    return new Tree<T>
    (Value);
  }
}
class Leaf<T> : Tree<T>
 where T: IComparable<T> {
  public Leaf(T Value)
     : base(Value) { }
  public override Tree<T>
  WithValue(T newValue) {
    return new Leaf<T>(newValue);
  }
}
class Node<T> : Tree<T>
 where T: IComparable<T> {
  public Node(T Value, Tree<T> Left, Tree<T> Right)
     : base(Value) {
    this.Left = Left;
    this.Right = Right;
  }
  public Tree<T> Left { get; private set; }
  public Tree<T> Right { get; private set; }
  public override Tree<T>
  WithValue(T newValue) {
    return new Node<T>(newValue, Left, Right);
  }
  public Node<T> WithLeft(Tree<T> newValue) {
    return new Node<T>(Value, newValue, Right);
  }
  public Node<T> WithRight(Tree<T> newValue) {
    return new Node<T>(Value, Left, newValue);
  }
  [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Tree<T> Item2 {
    get {
      return Left;
    }
  }
  [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Tree<T> Item3 {
    get {
      return Right;
    }
  }
}
static partial class Node {
  public static Node<T> New<T>(T Value, Tree<T> Left, Tree<T> Right) where T: IComparable<T> {
    return new Node<T>(Value, Left, Right);
  }
}
~~~
</div>

### define ###

<div class='sbs' markdown='1'>
~~~csharp
define MakeSquare($T) { 
	void Square($T x) { return x*x; }
}
MakeSquare(int);
MakeSquare(double);
MakeSquare(float);

[Passive]
define operator=(Foo[$index], $value) {
	Foo.SetAt($index, $value);
}
x = Foo[y] = z;
~~~

~~~csharp
// Output of LeMP
void Square(int x) {
  return x * x;
}
void Square(double x) {
  return x * x;
}
void Square(float x) {
  return x * x;
}

x = Foo.SetAt(y, z);
~~~
</div>

Defines a new macro, scoped to the current braced block, that matches the specified pattern and replaces it with the specified output code. `define` has the same syntax as a method, so you can use either lambda syntax like `replace MacroName(...) => ...`, or brace syntax like `replace MacroName(...) { ... }`. Brace syntax is more general, since it allows you to put multiple statements in the output, and you can also include type declarations.

The `[Passive]` option in the above example prevents warning messages when assignment operators are encountered that do not fit the specified pattern (e.g. `X = Y` and `X[index] = Y` do not match the pattern). See [MacroMode](http://ecsharp.net/doc/code/namespaceLeMP.html#ab267185fdc116f4e8f06125be9858721) for a list of available options.

Matching and replacement occur the same way as in the older [`replace`](#replace) macro. One difference is worth noting: if there are braces around a match argument, those 
braces are treated literally, not ignored (even though the braces around the replacement code 
are _not_ considered part of the replacement code; they _are_ ignored).

The technical difference between this and the older `replace()` macro is that `replace` performs a find-and-replace operation directly, whereas this one creates a macro with a specific name. This leads to a couple of differences in behavior which ensure that the old macro is still useful in certain situations.

The first difference is that `define` works recursively, but `replace` doesn't:

<div class='sbs' markdown='1'>
~~~csharp
replace (Foo => Bar(Foo($x)));
Foo(5);
~~~

~~~csharp
// Output of LeMP
Bar(Foo($x))(5);
~~~
</div>

Currently, `define` doesn't handle this well; `Foo(...)` is expanded recursively up to the iteration limit (or until stack overflow).

The second difference is that the older macro performs replacements immediately, while `define` generates a macro whose expansions are interleaved with other macros in the usual way. For example, if you write

~~~csharp
    replace (A => B);  
    define macro2($X) => $X * $X;  
    macro1(macro2(macro3(A)));
~~~

The order of replacements is 

1. `A` is replaced with `B`
2. `macro1` is executed (if there is a macro by this name)
3. `macro2` is executed (if it still exists in the output of `macro1`)
4. `macro3` is executed (if there is a macro by this name)

Often, `define` has higher performance than `replace` because, by piggybacking on the usual macro expansion process, it avoids performing an extra pass on the syntax tree.

**Note**: `define` and `replace` are not hygienic. For example, variables defined in the replacement expression are not renamed to avoid conflicts with variables defined at the point of expansion.

### replace ###

<div class='sbs' markdown='1'>
~~~csharp
replace (x => xxx) { Foo.x(); }
replace (Polo() => Marco(),
    Marco($x) => Polo($x));
if (Marco(x + y)) Polo();
~~~

~~~csharp
// Output of LeMP
Foo.xxx();
if (Polo(x + y))
  Marco();
~~~
</div>

Finds one or more patterns in a block of code and replaces each matching expression with another expression. The braces  are omitted from the output (and are not matchable). This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list.

The patterns can include both literal elements (e.g. `123` matches the integer literal `123` and nothing else, and `F()` only matches a call to `F` with no arguments) and "captures" like `$x`, which match any syntax tree and assign it to a "capture variable". Captures can be repeated in the replacement expression (after `=>`) to transfer subexpressions and statements from the original expression to the new expression. 

For example, above you can see that the expression `Marco(x + y)` matched the search expression `Marco($x)`. The identifier `Marco` was matched literally. `$x` was associated with the expression `x + y`, and it was inserted into the output, `Polo(x + y)`.

The match expression and/or the replacement expression (left and right sides of `=>`, respectively) can be enclosed in braces to enable statement syntax. Example:

<div class='sbs' markdown='1'>
~~~csharp
// Limitation: can't check $w and $s are the same.
replace ({ 
  List<$T> $L2 = $L1
    .Where($w => $wc)
    .Select($s => $sc).ToList(); 
} => {
  List<$T> $L2 = new List<$T>();
  foreach (var $w in $L1) {
    if ($wc) {
      static if (!($w `code==` $s))
      	var $s = $w;
      $L2.Add($sc);
    }
  }
});

void LaterThatDay()
{
  List<Item> paidItems = 
    items.Where(it => it.IsPaid)
         .Select(it => it.SKU).ToList();
}
~~~

~~~csharp
// Output of LeMP
void LaterThatDay()
{
  List<Item> paidItems = new List<Item>();
  foreach (var it in items) {
    if (it.IsPaid) {
      paidItems.Add(it.SKU);
    }
  }
}
~~~
</div>

The braces are otherwise ignored; for example, `{ 123; }` really just means `123`. If you actually want to match braces literally, use double braces: `{{ statement list; }}`

You can match a sequence of zero or more expressions using syntax like `$(..x)` on the search side (left side of `=>`). For example,

<div class='sbs' markdown='1'>
~~~csharp
replace (WL($fmt, $(..args)) => Console.WriteLine($fmt, $args));
WL(); // not matched
WL("Hello!");
WL("Hello {0}!", name);
~~~

~~~csharp
// Output of LeMP
WL();	// not matched
Console.WriteLine("Hello!");
Console.WriteLine("Hello {0}!", name);
~~~
</div>

The alternate name `replacePP(...)` additionally preprocesses the match and replacement expressions, which may be useful to get around problems with macro execution order. Caution: `replacePP` runs the macro processor twice on the replacement expression: once at the beginning, and again on the final output.

### unroll ###

<div class='sbs' markdown='1'>
~~~csharp
unroll ((X, Y) in ((X, Y), (Y, X)))
{ 
	DoSomething(X, Y);
	DoSomethingElse(X, Y);
	DoSomethingMore(X, Y);
}
~~~

~~~csharp
// Output of LeMP
DoSomething(X, Y);
DoSomethingElse(X, Y);
DoSomethingMore(X, Y);
DoSomething(Y, X);
DoSomethingElse(Y, X);
DoSomethingMore(Y, X);
~~~
</div>

Produces variations of a block of code, by replacing an identifier left of `in` with each of the corresponding expressions on the right of `in`.

The left hand side of `unroll` must be either a simple identifier or a tuple. The braces are not included in the output.

The right-hand side of `in` can be a tuple in parentheses, or a list of statements in braces, or a call to the `#splice(...)` pseudo-operator. If the right-hand side of `in` is none of these things, `unroll()` runs macros on the right-hand side of `in` in the hope that doing so will produce a list. However, note that this behavior can cause macros to be executed twice in some cases: once on the right-hand side of `in`, and then again on the final output of `unroll`. For example, the `noMacros` macro doesn't work if macros run twice.

### static deconstruct a.k.a. #deconstruct ###

<div class='sbs' markdown='1'>
~~~csharp
  #snippet tree = 8.5 / 11;
  #deconstruct($x * $y | $x / $y = #get(tree));
  var firstNumber = $x;
  var secondNumber = $y;
~~~

~~~csharp
// Output of LeMP
var firstNumber = 8.5;
var secondNumber = 11;
~~~
</div>

Syntax:

    #deconstruct(pattern1 | pattern2 = tree);

Deconstructs the syntax tree `tree` into constituent parts which are assigned to
compile-time syntax variables marked with `$` that can be used later in the
same braced block. For example, `#deconstruct($a + $b = x + y + 123)` creates
a syntax variable called `$a` which expands to `x + y`, and another variable `$b`
that expands to `123`. These variables behave like macros in their own right that
can be used later in the same braced block (although technically `$` is a macro in the `LeMP` namespace).

The left-hand side of `=` can specify multiple patterns separated by `|`. If you 
want `=` or `|` themselves (or other low-precedence operators, such as `&&`) to be part of the pattern on the left-hand side, you should enclose the pattern in braces (note: expressions in braces must end with `;` in EC#). If the pattern itself is intended to match a braced block, use double braces (e.g. 
`{{ $stuff; }}`).

Macros are expanded in the right-hand side (`tree`) before deconstruction occurs.

If multiple arguments are provided, e.g. `#deconstruct(e1 => p1, e2 => p2)`,
it has the same effect as simply writing multiple `#deconstruct` commands.

An error is printed when a deconstruction operation fails.

### static tryDeconstruct a.k.a. #tryDeconstruct ###

Same as `static deconstruct`, except that an error message is not printed when deconstruction fails.

### static if ###

<div class='sbs' markdown='1'>
~~~csharp
// Normally this variable is predefined
#set #inputFile = "Foo.cs"; 

static if (#get(#inputFile) `code==` "Foo.cs")
	WeAreInFoo();
else
	ThisIsNotFoo();

var t = static_if(true, T(), F());
~~~

~~~csharp
// Output of LeMP

WeAreInFoo();
var t = T();
~~~
</div>

A basic "compile-time if" facility. 

The `static if (cond) { then; } else { otherwise; }` statement or `static_if(cond, then, otherwise)` expression is replaced with the `then` clause or the `otherwise` clause according to whether the first argument - a boolean expression - evaluates to true or false.

The `otherwise` clause is optional; if it is omitted and the boolean expression evaluates to false, the entire `static_if` statement disappears from the output.

Currently, the condition supports only boolean math (e.g. `!true || false` can be evaluated but not `5 > 4`). `static_if` is often used in conjunction with the [`` `staticMatches` `` operator](#staticMatches-operator).

### static matchCode ###

    static matchCode (expr) { case ...: ... }

Compares an expression or statement to a list of cases at compile-time and selects a block of code at compile-time to insert into the output.

<div class='sbs' markdown='1'>
~~~csharp
#snippet expression = apples > oranges; 
static matchCode(#get(expression)) {
  case $x > $y, $x < $y:
    Compare($x, $y);
  case $call($(..args)): 
    void $call(unroll(arg in $(..args)) { int arg; }) 
      { base.$call($args); }
  default:
    DefaultAction($#);
}
~~~

~~~csharp
// Output of LeMP
Compare(apples, oranges);
~~~
</div>

For example, `case $a + $b:` expects a tree that calls `+` with two parameters, placed in compile-time variables called $a and $b.

If `expr` is a single statement inside braces, the braces are stripped. Next, macros are executed on `expr` to produce a new syntax tree to be matched. `matchCode` then scans the cases to find one that matches. Finally, the entire `static matchCode` construct is replaced with the handler associated with the matching `case`.

If none of the cases match and there is no `default:` case, the entire `static matchCode` construct and all its cases are eliminated from the output.

Use `case pattern1, pattern2:` to handle multiple cases with the same handler. Unlike C# `switch`, this statement does not expect `break` at the end of each case. If `break` is present at the end of the matching case, it is emitted literally into the output.

### staticMatches operator ###

<div class='sbs' markdown='1'>
~~~csharp
bool b1 = (x * y + z) `staticMatches` ($a * $b);
bool b2 = (x * y + z) `staticMatches` ($a + $b);

static if (Pie("apple") `staticMatches` Pie($x))
{
  ConfectionMode = $x;
}
~~~

~~~csharp
// Output of LeMP
bool b1 = false;
bool b2 = true;

ConfectionMode = "apple";
~~~
</div>

``syntaxTree `staticMatches` pattern`` returns the literal `true` if the form of the syntax tree on the left matches the pattern on the right.

The pattern can use `$variables` to match any subtree. `$(..lists)` (multiple statements or arguments) can be matched too. In addition, if the result is true then a syntax variable is created for each binding in the pattern other than `$_`. For example, ``Foo(123) `codeMatches` Foo($arg)`` sets `$arg` to `123`; you can use `$arg` later in your code.

The syntax tree on the left is macro-preprocessed, but the argument on the right is not. If either side is a single statement in braces (before preprocessing), the braces are ignored.