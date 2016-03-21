---
title: "LeMP Macro Reference: Other"
tagline: Standard macros in the LeMP namespace
layout: article
date: 20 Mar 2016
toc: true
---

### assert ###

	assert(condition);

Translates `assert(expr)` to `System.Diagnostics.Debug.Assert(expr, "Assertion failed in Class.MethodName: expr")`. You can change the assert method with `#snippet` as shown in the following example:

<div class='sbs' markdown='1'>
~~~csharp
class Class {
  void Bar() {
    #snippet #assertMethod = MyAssert;
    assert(BarIsOpen)
  }
  void Foo() {
    assert(FoodIsHot);
  }
}
~~~

~~~csharp
// Output of LeMP
class Class
{
  void Bar()
  {
    MyAssert(BarIsOpen, "Assertion failed in `Class.Bar`: BarIsOpen")
  }
  void Foo()
  {
    System.Diagnostics.Debug.Assert(FoodIsHot, "Assertion failed in `Class.Foo`: FoodIsHot");
  }
}
~~~
</div>

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
public partial abstract class Tree<T> where T: IComparable<T>
{
  public Tree(T Value)
  {
    this.Value = Value;
  }
  public T Value
  {
    get;
    private set;
  }
  public abstract Tree<T> WithValue(T newValue);
  [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public T Item1
  {
    get {
      return Value;
    }
  }
}
public partial abstract static partial class Tree
{
  public static Tree<T> New<T>(T Value) where T: IComparable<T>
  {
    return new Tree<T>(Value);
  }
}
class Leaf<T> : Tree<T> where T: IComparable<T>
{
  public Leaf(T Value) : base(Value)
  {
  }
  public override Tree<T> WithValue(T newValue)
  {
    return new Leaf<T>(newValue);
  }
}
class Node<T> : Tree<T> where T: IComparable<T>
{
  public Node(T Value, Tree<T> Left, Tree<T> Right) : base(Value)
  {
    this.Left = Left;
    this.Right = Right;
  }
  public Tree<T> Left
  {
    get;
    private set;
  }
  public Tree<T> Right
  {
    get;
    private set;
  }
  public override Tree<T> WithValue(T newValue)
  {
    return new Node<T>(newValue, Left, Right);
  }
  public Node<T> WithLeft(Tree<T> newValue)
  {
    return new Node<T>(Value, newValue, Right);
  }
  public Node<T> WithRight(Tree<T> newValue)
  {
    return new Node<T>(Value, Left, newValue);
  }
  [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Tree<T> Item2
  {
    get {
      return Left;
    }
  }
  [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] public Tree<T> Item3
  {
    get {
      return Right;
    }
  }
}
static partial class Node
{
  public static Node<T> New<T>(T Value, Tree<T> Left, Tree<T> Right) where T: IComparable<T>
  {
    return new Node<T>(Value, Left, Right);
  }
}
~~~
</div>

### Backing fields: [field] ###

<div class='sbs' markdown='1'>
~~~csharp
[field]          int X { get; set; }
[field _y]       int Y { get; set; }
[field Int32 _z] int Z { get; set; }
~~~

~~~csharp
// Output of LeMP
int _x;
int X
{
  get {
    return _x;
  }
  set {
    _x = value;
  }
}
int _y;
int Y
{
  get {
    return _y;
  }
  set {
    _y = value;
  }
}
Int32 _z;
int Z
{
  get {
    return _z;
  }
  set {
    _z = value;
  }
}
~~~
</div>

Creates a backing field for a property. In addition, if the body of the property is empty, a getter is added.

### concatId ###

<div class='sbs' markdown='1'>
~~~csharp
concatId(Sq, uare);
a `##` b; // synonyn
~~~

~~~csharp
// Output of LeMP
Square;
ab;
~~~
</div>

Concatenates identifiers and/or literals to produce an identifier. For example, the output of ``a `##` b`` is `ab`.

**Note**: concatId cannot be used directly as a variable or method name unless you use `$(out concatId(...))`.

### this-constructors ###

<div class='sbs' markdown='1'>
~~~csharp
class Foo { 
	public this(int x) : base(x) 
	{
	}
}
~~~

~~~csharp
// Output of LeMP
class Foo
{
  public Foo(int x) : base(x)
  {
  }
}
~~~
</div>

Supports the EC# 'this()' syntax for constructors by replacing 'this' with the name of the enclosing class.

### $(out ...) ###

<div class='sbs' markdown='1'>
~~~csharp
// DollarSignIdentity macro
$(out expression);
~~~

~~~csharp
// Output of LeMP
expression;
~~~
</div>

`$(out ...)` allows you to use a macro in Enhanced C# in places where macros are ordinarily not allowed, such as in places where a data type or a method name are expected. The `out` attribute is required to make it clear you want to run this macro and that some other meaning of `$` does not apply. Examples:

<div class='sbs' markdown='1'>
~~~csharp
$(out Foo) number;
int $(out concatId(Sq, uare))(int x) => x*x;
~~~

~~~csharp
// Output of LeMP
Foo number;
int Square(int x) => x * x;
~~~
</div>

### Contract attributes ###

Documentation [here](ref-code-contracts.html).

### Method Forwarding ###

~~~csharp
Type SomeMethod(Type param) ==> target.Method;
Type Prop ==> target._;
Type Prop { get ==> target._; set ==> target._; }
~~~

This is really handy for implementing the [adapter pattern](https://en.wikipedia.org/wiki/Adapter_pattern) or the [wrapper pattern](https://en.wikipedia.org/wiki/Decorator_pattern).

`==>` forwards a call to another method. The target method must not include an argument list; the method parameters are forwarded automatically. If the target expression includes an underscore (`_`), it is replaced with the name of the current function. Examples:

<div class='sbs' markdown='1'>
~~~csharp
Type SomeMethod(Type param) ==> target.Method;
int Compute(int x) ==> base._;
Type Prop ==> target._; 
Type Prop { get ==> target; set ==> target; }
~~~

~~~csharp
// Output of LeMP
Type SomeMethod(Type param)
{
  return target.Method(param);
}
int Compute(int x)
{
  return base.Compute(x);
}
Type Prop
{
  get {
    return target.Prop;
  }
}
Type Prop
{
  get {
    return target;
  }
  set {
    target = value;
  }
}
~~~
</div>

### in-range operator combination ###

<div class='sbs' markdown='1'>
~~~csharp
// Variations of 'in' operator
x in range;
x in lo..hi; 
x in lo...hi; 
x in ..hi; 
x in lo..._;
~~~

~~~csharp
// Output of LeMP
range.Contains(x);
x.IsInRangeExcludeHi(lo, hi);
x.IsInRange(lo, hi);
x < hi;
x >= lo;
~~~
</div>

Converts an 'in' expression to a normal C# expression using the following rules (keeping in mind that the EC# parser treats `..<` as an alias for `..`):

1. `x in _..hi` and `x in ..hi` become `x.IsInRangeExcl(hi)`
2. `x in _...hi` and `x in ...hi` become `x.IsInRangeIncl(hi)`
3. `x in lo.._` and `x in lo..._` become simply `x >= lo`
4. `x in lo..hi` becomes `x.IsInRangeExcludeHi(lo, hi)`
5. `x in lo...hi` becomes `x.IsInRange(lo, hi)`
6. `x in range` becomes `range.Contains(x)`

The first applicable rule is used.

### includeFile (a.k.a. #include) ###

~~~csharp
includeFile("Filename.cs")
includeFile("Filename.les")
~~~

Reads source code from the specified file, and inserts the syntax tree in place of the macro call. The input language is determined automatically according to the file extension. If the file extension is not recognized, the current input language is assumed.

For nostalgic purposes (to resemble C/C++), `#include` is a synonym of `includeFile`.

### match ###

~~~csharp
match (var) { case pattern: handler }; // EC# syntax
match (var) { pattern => { handler }; }; // LES syntax (also works in EC#)
~~~

Attempts to match and deconstruct an object against a "pattern", such as a tuple or an algebraic data type. There can be multiple 'case' blocks, and a `default`. Example:

<div class='sbs' markdown='1'>
~~~csharp
match (obj) {  
  case is Shape(ShapeType.Circle, $size, 
       Location: $p is Point<int>($x, $y)): 
    DrawCircle(size, x, y); 
}
~~~

~~~csharp
// Output of LeMP
do
  if (obj is Shape) {
    Shape tmp_0 = (Shape) obj;
    if (ShapeType.Circle.Equals(tmp_0.Item1)) {
      var size = tmp_0.Item2;
      var tmp_1 = tmp_0.Location;
      if (tmp_1 is Point<int>) {
        Point<int> p = (Point<int>) tmp_1;
        var x = p.Item1;
        var y = p.Item2;
        DrawCircle(size, x, y);
        break;
      }
    }
  }
while (false);
~~~
</div>

`break` is not expected at the end of each handler (`case` code block), but it can be used to exit early from a `case`. You can associate multiple patterns with the same handler using `case pattern1, pattern2:` in EC#, but please note that (due to a limitation of plain C#) this causes code duplication since the handler will be repeated for each pattern.

### matchCode ###

~~~csharp
matchCode (var) { 
  case expression: handler;     // C# style
  case { statement; }: handler; // C# style
  expression => handler;        // LES style
};
~~~

Attempts to match and deconstruct a Loyc tree against a series of cases with patterns, e.g. `case $a + $b:` expects a tree that calls `+` with two parameters, placed in new variables called a and b. `break` is not required or recognized at the end of each case's handler (code block). Use `$(...x)` to gather zero or more parameters into a list `x`. Use `case pattern1, pattern2:` in EC# to handle multiple cases with the same handler.

**Note**: Currently there is an inconsistency where you can use `break` to exit `match` but you cannot use `break` to exit `matchCode`, because the latter produces a simple `if-else` chain as its output. It is likely that in the future `matchCode`'s output will be wrapped in `do { } while (false)` so that the `break;` statement works the same way as it does in `match` and `switch`.

Example:

<div class='sbs' markdown='1'>
~~~csharp
Console.WriteLine("Input an expression plz");
string str = Console.ReadLine();
LNode tree = EcsLanguageService.Value.Parse(
  str, null, ParsingService.Exprs);
Console.WriteLine(Eval(tree));

dynamic Eval(LNode code)
{
  dynamic value;
  matchCode(code) {
  case $x + $y:
    return Eval(x)+Eval(y);
  case $x * $y:
    return Eval(x)*Eval(y);
  case $x == $y:
    return Eval(x) == Eval(y);
  case $x ? $y : $z:
    return Eval(x) ? Eval(y) : Eval(z);
  default:
    if (code.IsLiteral)
      return code.Value;
  }
}
~~~

~~~csharp
// Output of LeMP
Console.WriteLine("Input an expression plz");
string str = Console.ReadLine();
LNode tree = EcsLanguageService.Value.Parse(str, null, ParsingService.Exprs);
Console.WriteLine(Eval(tree));
dynamic Eval(LNode code)
{
  dynamic value;
  {
    LNode x, y, z;
    if (code.Calls(CodeSymbols.Add, 2) && (x = code.Args[0]) != null && (y = code.Args[1]) != null)
      return Eval(x) + Eval(y);
    else if (code.Calls(CodeSymbols.Mul, 2) && (x = code.Args[0]) != null && (y = code.Args[1]) != null)
      return Eval(x) * Eval(y);
    else if (code.Calls(CodeSymbols.Eq, 2) && (x = code.Args[0]) != null && (y = code.Args[1]) != null)
      return Eval(x) == Eval(y);
    else if (code.Calls(CodeSymbols.QuestionMark, 3) && (x = code.Args[0]) != null && (y = code.Args[1]) != null && (z = code.Args[2]) != null)
      return Eval(x) ? Eval(y) : Eval(z);
    else if (code.IsLiteral)
      return code.Value;
  }
}
~~~
</div>

### nameof ###

	nameof(id_or_expr)

Converts the "key" name component of an expression to a string. Example:

**TODO:** remove this feature, since C# 6 got it.

<div class='sbs' markdown='1'>
~~~csharp
nameof(Ant.Banana<C>(Dandilion));
~~~

~~~csharp
// Output of LeMP
"Banana";
~~~
</div>

### `namespace` without braces ###

~~~csharp
namespace Foo;
~~~

Surrounds the remaining code in a namespace block.

<div class='sbs' markdown='1'>
~~~csharp
namespace Normal { 
  class C {}
}
namespace NoBraces;
class C {}
~~~

~~~csharp
// Output of LeMP
namespace Normal
{
  class C
  {
  }
}
namespace NoBraces
{
  class C
  {
  }
}
~~~
</div>

### ??= ###

<div class='sbs' markdown='1'>
~~~csharp
A ??= B;
~~~

~~~csharp
// Output of LeMP
A = A ?? B;
~~~
</div>

Assign A = B only when A is null. **Caution**: currently, A is evaluated twice.

### Null-dot (`?.`) ###

<div class='sbs' markdown='1'>
~~~csharp
if (a.b?.c.d) {
  Good();
}
~~~

~~~csharp
// Output of LeMP
if (a.b != null ? a.b.c.d : null) {
  Good();
}
~~~
</div>

`a.b?.c.d` means `(a.b != null ? a.b.c.d : null)`.

**TODO**: remove this, since it was added to C# 6. **Note**: you can use `noMacro(e)` to disable macros in an expression `e`.

### on_finally, on_throw, etc. ###

<div class='sbs' markdown='1'>
~~~csharp
on_finally { _obj = null; }
on_return (result) { Trace.WriteLine(result); }
on_throw_catch(exc) { MessageBox.Show(exc.Message); }
on_throw(exc) { _success = false; }
return _obj.ReadFile(filename);
~~~

~~~csharp
// Output of LeMP
try {
  try {
    try {
      {
        var result = _obj.ReadFile(filename);
        Trace.WriteLine(result);
        return result;
      }
    } catch (Exception exc) {
      _success = false;
      throw;
    }
  } catch (Exception exc) {
    MessageBox.Show(exc.Message);
  }
} finally {
  _obj = null;
}
~~~
</div>

These are explained in a [separate page](ref-on_star.html).

### =: ###

<div class='sbs' markdown='1'>
~~~csharp
if (int.Parse(text)=:num > 0)
	positives += num;
~~~

~~~csharp
// Output of LeMP
if (#var(@``, num = int.Parse(text)) > 0)
  positives += num;
~~~
</div>

This macro isn't useful yet - the feature that it represents is not yet functional.

### quote ###

<div class='sbs' markdown='1'>
~~~csharp
var str = quote("Hello, world!");
var code = quote {
	Console.WriteLine($str);
};
~~~

~~~csharp
// Output of LeMP
var str = LNode.Literal("Hello, world!");
var code = LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Console"), LNode.Id((Symbol) "WriteLine"))), LNode.List(str));
~~~
</div>

Macro-based code quote mechanism, to be used as long as a more complete compiler is not availabe. If there is a single parameter that is braces, the braces are stripped out. If there are multiple parameters, or multiple statements in braces, the result is a call to `#splice()`. Note that some code, such as the macro processor, recognizes `#splice` as a signal that you want to insert a list of things into an outer list:

<div class='sbs' markdown='1'>
~~~csharp
a = 1;
#splice(b = 2, c = 3);
d = 4;
~~~

~~~csharp
// Output of LeMP
a = 1;
b = 2;
c = 3;
d = 4;
~~~
</div>

The output refers unqualified to `CodeSymbols` and `LNode` so you must have `using Loyc.Syntax` at the top of your file. The substitution operator $(expr) causes the specified expression to be inserted unchanged into the output. `using Loyc.Collections` is also recommended so that you can use `VList<LNode>`, the data type that `LNode` itself uses to store a list of `LNode`s.

### `rawQuote` ###

<div class='sbs' markdown='1'>
~~~csharp
rawQuote($foo);
~~~

~~~csharp
// Output of LeMP
LNode.Call(CodeSymbols.Substitute, LNode.List(LNode.Id((Symbol) "foo")));
~~~
</div>

Behaves the same as `quote(code)` except that the substitution operator `$` is treated the same as all other code, instead of being recognized as a request for substitution.

### Range operators (`..` and `...`) ###

<div class='sbs' markdown='1'>
~~~csharp
lo..hi; 
..hi; 
lo.._;
lo..hi; 
..hi; 
lo.._;
~~~

~~~csharp
// Output of LeMP
Range.ExcludeHi(lo, hi);
Range.UntilExclusive(hi);
Range.StartingAt(lo);
Range.ExcludeHi(lo, hi);
Range.UntilExclusive(hi);
Range.StartingAt(lo);
~~~
</div>

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

The alternate name `replacePP` additionally preprocesses the input and output arguments, and is useful to get around problems with macro execution order. This behavior is not the default, since the final output will be macro-processed a second time.

### scope(...) ###

	scope(exit) { ... }; scope(success) {..}; scope(failure) {...}

An homage to D, this is equivalent to [`on_finally` et al](ref-on_star.html).

### Set or create member ###

<div class='sbs' markdown='1'>
~~~csharp
Type Method(set Type member) {}
Type Method2(public Type Member2) {}
~~~

~~~csharp
// Output of LeMP
Type Method(Type member)
{
  this.member = member;
}
public Type Member2;
Type Method2(Type member2)
{
  Member2 = member2;
}
~~~
</div>

Automatically assigns a value to an existing field, or creates a new field with an initial value set by calling the method. This macro can be used with constructors and methods. This macro is activated by attaching one of the following modifiers to a method parameter: `set, public, internal, protected, private, protectedIn, static, partial`.

### \#setTupleType ###

~~~csharp
#setTupleType(BareName);
#setTupleType(TupleSize, BareName);
#setTupleType(TupleSize, BareName, Factory.Method)
~~~

Configures the type and creation method for tuples, either for a specific size of tuple, or for all sizes at once. Example:

<div class='sbs' markdown='1'>
~~~csharp
#setTupleType(2, Pair);
var pair = (1234, "bad password");
#useDefaultTupleTypes;
var tupl = (1234, "bad password");
~~~

~~~csharp
// Output of LeMP
var pair = Pair.Create(1234, "bad password");
var tupl = Tuple.Create(1234, "bad password");
~~~
</div>

### \#useDefaultTupleTypes ###

Reverts to using `Tuple` and `Tuple.Create` for all arities of tuple.

### static if ###

<div class='sbs' markdown='1'>
~~~csharp
// Normally this variable is predefined
#set #inputFile = "Foo.cs"; 

static if (#get(#inputFile) `tree==` "Foo.cs")
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

A very basic "compile-time if" facility. It can't do very much yet.

### stringify ###

<div class='sbs' markdown='1'>
~~~csharp
stringify(expr);
Console.WriteLine(stringify(u+me=luv));
~~~

~~~csharp
// Output of LeMP
"expr";
Console.WriteLine("u + me = luv");
~~~
</div>

Converts an expression to a string (note: original formatting is not preserved.)

### tree== ###

<div class='sbs' markdown='1'>
~~~csharp
x `tree==` y;
x + 1 `tree==` x + "1";
x + 777 `tree==` x+777;
~~~

~~~csharp
// Output of LeMP
false;
false + "1";
false + 777;
~~~
</div>

Returns the literal true if two or more syntax trees are equal, or false if not.

### Tuple macro ###

<div class='sbs' markdown='1'>
~~~csharp
(x,);
(x, y);
(x, y, z);
~~~

~~~csharp
// Output of LeMP
Tuple.Create(x);
Tuple.Create(x, y);
Tuple.Create(x, y, z);
~~~
</div>

Create a tuple.

### Tuple type shortcut ###

<div class='sbs' markdown='1'>
~~~csharp
#<int, string, double> tuple;
~~~

~~~csharp
// Output of LeMP
Tuple<int,string,double> tuple;
~~~
</div>

`#<...>` is a shortcut for `Tuple<...>` or whatever data type is currently configured for tuples, but it isn't really recommended to use it since its meaning is less than obvious.

### Tuple deconstruction ###

<div class='sbs' markdown='1'>
~~~csharp
(a, b, var c) = expr;
~~~

~~~csharp
// Output of LeMP
a = expr.Item1;
b = expr.Item2;
var c = expr.Item3;
~~~
</div>

Extracts components of a tuple or an `alt class`.

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

Produces variations of a block of code, by replacing an identifier left of `in` with each of the corresponding expressions on the right of `in`. The braces are omitted from the output. 

### \#useSymbols ###

<div class='sbs' markdown='1'>
~~~csharp
#useSymbols;
void Increment()
{
  if (dict.Contains(@@Foo))
    dict[@@Foo]++;
  else
    dict[@@Foo] = 1;
}
~~~

~~~csharp
// Output of LeMP
static readonly Symbol sy_Foo = (Symbol) "Foo";
void Increment()
{
  if (dict.Contains(sy_Foo))
    dict[sy_Foo]++;
  else
    dict[sy_Foo] = 1;
}
~~~
</div>

Replaces each symbol in the code that follows with a `static readonly` variable named `sy_X` for each symbol `@@X`. The #useSymbols; statement should be placed near the top of a class definition.

### Multi-using ###

<div class='sbs' markdown='1'>
~~~csharp
using System(, .Collections.Generic, .Linq);
using Loyc(, .Math, .Collections, .Syntax);
~~~

~~~csharp
// Output of LeMP
using System;
using System.Collections.Generic;
using System.Linq;
using Loyc;
using Loyc.Math;
using Loyc.Collections;
using Loyc.Syntax;
~~~
</div>

Generates multiple using-statements from a single one.

### "with" statement ###

<div class='sbs' markdown='1'>
~~~csharp
with (Some.Thing) { .Member = 0; .Method(); }
~~~

~~~csharp
// Output of LeMP
{
  var tmp_2 = Some.Thing;
  tmp_2.Member = 0;
  tmp_2.Method();
}
~~~
</div>

Use members of a particular object with a shorthand "prefix-dot" notation. 

**Caution**: if used with a value type, a copy of the value is made; you won't be editing the original.