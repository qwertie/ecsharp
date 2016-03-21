---
title: "LeMP Macro Reference: Other"
tagline: Standard macros in the LeMP namespace
layout: article
date: 20 Mar 2016
toc: true
---

### assert ###

	assert(condition);

Translates `assert(expr)` to `System.Diagnostics.Debug.Assert(expr, "Assertion failed in Class.MethodName: expr")`. You can change the assert method with `#snippet` as shown in this example:

~~~exec
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

### alt class: algebraic data type ###

	e.g. alt class Tree<T> { alt Node(Tree<T> Left, Tree<T> Right); alt Leaf(T Value); }

Expands a short description of an 'algebraic data type' (a.k.a. disjoint union) into a set of classes with a common base class. All data members are read-only, and for each member (e.g. Item1 and Item2 above), a With() method is generated to let users create modified versions. Example:

~~~exec
public partial abstract alt class Tree<T> 
	where T: IComparable<T>
{
	alt this(T Value);
	alt Leaf();
	alt Node(Tree<T> Left, Tree<T> Right);
}
~~~

### Backing fields: [field] ###

~~~exec
[field]        int X { get; set; }
[field _y]     int Y { get; set; }
[field int _z] int Z { get; set; }
~~~

Create a backing field for a property. In addition, if the body of the property is empty, a getter is added.

### ColonEquals ###

~~~exec
A := B
~~~

Deprecated.

Declare a variable A and set it to the value of B. Equivalent to "var A = B".

### concatId ###

~~~exec
concatId(Sq, uare);
a `##` b; // synonyn
~~~

Concatenates identifiers and/or literals to produce an identifier. For example, the output of ``a `##` b`` is `ab`.

**Note**: concatId cannot be used directly as a variable or method name unless you use `$(out concatId(...))`.

### this-constructors ###

~~~exec
class Foo { this() {} }
~~~

Supports the EC# 'this()' syntax for constructors by replacing 'this' with the name of the enclosing class.

### $(out ...) ###

~~~exec
// DollarSignIdentity macro
$(out expression);
~~~

`$(out ...)` allows you to use a macro in Enhanced C# in places where macros are ordinarily not allowed, such as in places where a data type or a method name are expected. The `out` attribute is required to make it clear you want to run this macro and that some other meaning of `$` does not apply. Examples:

~~~exec
$(out Foo) number;
int $(out concatId(Sq, uare))(int x) => x*x;
~~~

### Contract attributes ###

	Documentation [here](ref-code-contracts.html).

### Method forwarding ###

	Type SomeMethod(Type param) ==> target.Method;
	Type Prop ==> target;
	Type Prop { get ==> target; set ==> target; }

This is really handy for implementing the [adapter pattern](https://en.wikipedia.org/wiki/Adapter_pattern) or the [wrapper pattern](https://en.wikipedia.org/wiki/Decorator_pattern).

`==>` forwards a call to another method. The target method must not include an argument list; the method parameters are forwarded automatically. If the target expression includes an underscore (`_`), it is replaced with the name of the current function. Examples:

~~~exec
Type SomeMethod(Type param) ==> target.Method;
int Compute(int x) ==> base._;
Type Prop ==> target; 
Type Prop { get ==> target; set ==> target; }
~~~

### in-range operator combination ###

~~~exec
x in lo..hi; 
x in lo...hi; 
x in ..hi; 
x in lo..._; x in range
x in range;
~~~

Converts an 'in' expression to a normal C# expression using the following rules (keeping in mind that the EC# parser treats `..<` as an alias for `..`):

1. `x in _..hi` and `x in ..hi` become `x.IsInRangeExcl(hi)`
2. `x in _...hi` and `x in ...hi` become `x.IsInRangeIncl(hi)`
3. `x in lo.._` and `x in lo..._` become simply `x >= lo`
4. `x in lo..hi` becomes `x.IsInRangeExcludeHi(lo, hi)`
5. `x in lo...hi` becomes `x.IsInRange(lo, hi)`
6. `x in range` becomes `range.Contains(x)`

The first applicable rule is used.

### includeFile (a.k.a. #include) ###

	includeFile("Filename.cs")
	includeFile("Filename.les")

Reads source code from the specified file, and inserts the syntax tree in place of the macro call. The input language is determined automatically according to the file extension.

For nostalgic purposes (to resemble C/C++), `#include` is a synonym of `includeFile`.

### match ###

	match (var) { case pattern: handler }; // EC# syntax
	match (var) { pattern => { handler }; }; // LES syntax (also works in EC#)

Attempts to match and deconstruct an object against a "pattern", such as a tuple or an algebraic data type. There can be multiple 'case' blocks, and a `default`. Example:

~~~exec
match (obj) {  
  case is Shape(ShapeType.Circle, $size, 
       Location: $p is Point<int>($x, $y)): 
    DrawCircle(size, x, y); 
}
~~~

`break` is not expected at the end of each handler (`case` code block), but it can be used to exit early from a `case`. You can associate multiple patterns with the same handler using `case pattern1, pattern2:` in EC#, but please note that (due to a limitation of plain C#) this causes code duplication since the handler will be repeated for each pattern.

### matchCode ###

	matchCode (var) { case ...: ... }; // In LES, use a => b instead of case a: b

Attempts to match and deconstruct a Loyc tree against a series of cases with patterns, e.g. `case $a + $b:` expects a tree that calls `+` with two parameters, placed in new variables called a and b. `break` is not required or recognized at the end of each case's handler (code block). Use `$(...x)` to gather zero or more parameters into a list `x`. Use `case pattern1, pattern2:` in EC# to handle multiple cases with the same handler.

**Note**: Currently there is an inconsistency where you can use `break` to exit `match` but you cannot use `break` to exit `matchCode`, because the latter produces a simple `if-else` chain as its output. It is likely that in the future `matchCode`'s output will be wrapped in `do { } while (false)` so that the `break;` statement works the same way as it does in `match` and `switch`.

### nameof ###

	nameof(id_or_expr)

Converts the "key" name component of an expression to a string. Example:

**TODO:** remove this feature, since C# 6 got it.

~~~exec
nameof(Ant.Bug<C>(Dandilion));
~~~

### `namespace` without braces ###

	namespace Foo;

Surrounds the remaining code in a namespace block.

~~~exec
namespace Normal { 
	class C {}
}
namespace NoBraces;
class C {}
~~~

### ??= ###

~~~exec
A ??= B
~~~

Assign A = B only when A is null. **Caution**: currently, A is evaluated twice.

### @`?.` ###

~~~exec
a.b?.c.d
~~~

`a.b?.c.d` means `(a.b != null ? a.b.c.d : null)`.

**TODO**: remove this, since it was added to C# 6. **Note**: you can use `noMacro(e)` to disable macros in an expression `e`.

### on_finally, on_throw, etc. ###

~~~exec
on_finally { _obj = null; }
on_return (result) { Trace.WriteLine(result); }
on_throw_catch(exc) { MessageBox.Show(exc.Message); }
on_throw(exc) { _success = false; }
return _obj.ReadFile(filename);
~~~

These are explained in a [separate page](ref-on_star.html).

### =: ###

~~~exec
if (int.Parse(text)=:num > 0)
	positives += num;
~~~

This macro isn't useful yet - the feature that it represents is not yet functional.

### quote ###

~~~exec
var str = quote("Hello, world!");
var code = quote {
	Console.WriteLine($str);
};
~~~

Macro-based code quote mechanism, to be used as long as a more complete compiler is not availabe. If there is a single parameter that is braces, the braces are stripped out. If there are multiple parameters, or multiple statements in braces, the result is a call to #splice().

The output refers unqualified to `CodeSymbols` and `LNode` so you must have `using Loyc.Syntax` at the top of your file. The substitution operator $(expr) causes the specified expression to be inserted unchanged into the output. `using Loyc.Collections` is also recommended so that you can use `VList<LNode>`, the data type that `LNode` itself uses to store a list of `LNode`s.

### `rawQuote` ###

~~~exec
rawQuote($foo)
~~~

Behaves the same as `quote(code)` except that the substitution operator `$` is treated the same as all other code, instead of being recognized as a request for substitution.

### `..` and `...` ###

~~~exec
lo..hi; 
..hi; 
lo.._;
lo..hi; 
..hi; 
lo.._;
~~~

### replace ###

~~~exec
replace (x => X) { Foo.x(); }
replace (Polo() => Marco(),
    Marco($x) => Polo($x));
if (Marco(x + y)) Polo();
~~~

Finds one or more patterns in a block of code and replaces each matching expression with another expression. The braces  are omitted from the output (and are not matchable). This macro can be used without braces, in which case it affects all the statements/arguments that follow it in the current statement or argument list.

The alternate name `replacePP` additionally preprocesses the input and output arguments, and is useful to get around problems with macro execution order. This behavior is not the default, since the final output will be macro-processed a second time.

### scope(...) ###

	scope(exit) { ... }; scope(success) {..}; scope(failure) {...}

An homage to D, equivalent to [`on_finally` et al](ref-on_star.html).

### Set or create member ###

~~~exec
Type Method(set Type member) {}
Type Method2(public Type Member2) {}
~~~

Automatically assigns a value to an existing field, or creates a new field with an initial value set by calling the method. This macro can be used with constructors and methods. This macro is activated by attaching one of the following modifiers to a method parameter: `set, public, internal, protected, private, protectedIn, static, partial`.

### \#setTupleType ###

	#setTupleType(BareName);
	#setTupleType(TupleSize, BareName);
	#setTupleType(TupleSize, BareName, Factory.Method)

Configures the type and creation method for tuples, either for a specific size of tuple, or for all sizes at once.

### \#useDefaultTupleTypes ###

~~~exec
#setTupleType(2, Pair);
var pair = (1234, "bad password");
#useDefaultTupleTypes;
var tuple = (1234, "bad password");
~~~

Reverts to using Tuple and Tuple.Create for all arities of tuple.

### static_if ###

~~~exec
// Normally this variable is predefined
#set #inputFile = "Foo.cs"; 

static if (#get(#inputFile) `tree==` "Foo.cs")
	WeAreInFoo();
else
	ThisIsNotFoo();

var t = static_if(true, T(), F());
~~~

A very basic "static if" facility. It can't do very much yet.

### stringify ###

~~~exec
stringify(expr);
Console.WriteLine(stringify(u+me=luv));
~~~

Converts an expression to a string (note: original formatting is not preserved.)

### tree== ###

~~~exec
x `tree==` y
~~~

Returns the literal true if two or more syntax trees are equal, or false if not.

### (tuple,) ###

~~~exec
(x,);
(x, y);
(x, y, z);
~~~

Create a tuple.

### Tuple type shortcut ###

~~~exec
#<int, string, double> tuple;
~~~

`#<...>` is a shortcut for `Tuple<...>`.

### Tuple deconstruction ###

~~~exec
(a, b, var c) = expr;
~~~

Assign a = expr.Item1, b = expr.Item2, etc.

### unroll ###

~~~exec
unroll ((X, Y) in ((X, Y), (Y, X)))
{ 
	DoSomething(X, Y);
	DoSomethingElse(X, Y);
	DoSomethingMore(X, Y);
}
~~~

Produces variations of a block of code, by replacing an identifier left of `in` with each of the corresponding expressions on the right of `in`. The braces are omitted from the output. 

### \#useSymbols ###

~~~exec
#useSymbols;
void Increment()
{
	if (dict.Contains(@@Foo))
		dict[@@Foo]++;
	else
		dict[@@Foo] = 1;
}
~~~

Replaces each symbol in the code that follows with a `static readonly` variable named `sy_X` for each symbol `@@X`.

### Multi-using ###

~~~exec
using (System, System.(Collections.Generic, Linq, Text));
~~~

Generates multiple using-statements from a single one.

### "with" statement ###

~~~exec
with (Some.Thing) { .Member = 0; .Method(); }
~~~

Use members of a particular object with a shorthand "prefix-dot" notation. 

**Caution**: if used with a value type, a copy of the value is made; you won't be editing the original.
