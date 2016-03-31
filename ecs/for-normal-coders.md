---
title: "Learn Enhanced C#"
tagline: An article for ordinary C# coders
date: 30 Mar 2016
layout: page
toc: true
commentIssueId: 28
---

Introduction
------------

Enhanced C# (EC#) is a new programming language intended to supercharge C# with powerful features inspired by lesser-known languages such as LISP and D. I created it alone; it is not affiliated with or endorsed by Microsoft. This article is for normal developers; if you really know your stuff, you might also want to read my [EC# for PL Nerds](for-programming-language-pundits.html) series.

EC# tries to be about 99.9% backward compatible with C#, and in fact compiles itself down to plain C#; eventually it would be nice to have a proper .NET compiler that produces CIL/MSIL, but some may find it's _more_ useful without one, because compiling to C# means perfect interoperability with existing code. Take any existing C# project and you can add Enhanced C# to it, without even breaking the builds of other team members. And if you decide you don't want to use it anymore, you can just throw away the Enhanced C# code and keep the generated C# code.

Through EC#, I planned to enhance C# with the following categories of features:

1. A procedural macro system
2. Compile-time code execution (CTCE)
3. A template system (the "parameterized program tree")
4. An alias system (which is a minor tweak to the type system)
5. Miscellaneous specific syntax enhancements
6. Miscellaneous features to boost productivity - pattern matching, disjoint unions, unrolling, replacing, and more

It's okay if you have no idea what the items on the list mean. Trust me though, it's good stuff. Only items #1, #5 and #6 are actually implemented, so that's what I'll be talking about in this article.

EC# is a grand plan to transform C# into a much more powerful and more succinct language\*. The plan has a lot of stuff that doesn't exist yet, but I focused on some of my most-wanted features first, so I hope you'll find that EC# is already powerful enough for a variety of useful tasks where plain C# falls short.

In no particular order, here's what you get.

Safe navigation operator and lambda methods
-------------------------------------------

When I originally wrote this article, the `?.` operator did not exist, but I added it to Enhanced C#. Now that C# 6.0 has this operator, all I have to do is disable the code transformation in EC# that supported it.

> In certain cases, this operator is a huge time saver. For example, what if "DBConnection", "PersonTable", and "FirstRow" in the following line might all return `null`?
>
> ~~~ecsharp
> var firstName = DBConnection.Tables.Get("Person").FirstRow.Name;
> ~~~
>
> In plain C#, it's a giant pain in the butt to check if each object is null:
>
> ~~~ecsharp
> string firstName = null;
> var dbc = DBConnection;
> if (dbc != null) {
>    var pt = dbc.Tables.Get("Person");
>    if (pt != null) {
>      var fr = pt.FirstRow;
>      if (fr != null)
>        firstName = fr.Name;
>    }
> }
> ~~~
> 
> But with the safe navigation operator it's easy. The above code only needs one line in EC#:
>
> ~~~ecsharp
> var firstName = DBConnection?.Tables.Get("Person")?.FirstRow?.Name;
> ~~~

Similarly, when I wrote this article, I planned to support "lambda" methods and properties like

~~~ecsharp
static int Square(int x) => x*x;
public string Address => _address;
~~~

And I planned to implement so-called string interpolation like `$"Hello $_name!"`. C# 6 got all of these features, with exactly the same syntax I was using (or, in the case of string interpolation, planning to use). We can only hope that MS eventually adds all the features of EC# to plain C#!

Triple-quoted strings
---------------------

C# has a concept of "verbatim" strings, which allow newlines inside the string:

~~~csharp
namespace Foo {
   class Foo {
	  static string ThreeLines() { 
		 return @"Line one
			Line two
			Line three";
	  }
   }
}
~~~

But you might not like how this works. First of all, any indentation of the second line is included in the string. Writing the string so that lines two and three are _not_ indented looks _very_ ugly:

~~~csharp
namespace Foo {
   class Foo {
	  static string ThreeLines() { 
		 return @"Line one
Line two
Line three";
	  }
   }
}
~~~

Secondly, the newlines in the string depend on the text file format: so a UNIX-style C# file will have "\n" newlines, but a Windows-style C# file will have "\r\n" newlines. Source control repositories may change the format automatically, so that the "same" source file contains a "different" string after it passes from one system to another through source control.

Thus, verbatim strings aren't ideal for representing typical multiline strings. That's why Enhanced C# introduces triple-quoted strings:

~~~csharp
class Foo {
   static string ThreeLines() { 
      return """Line one
         Line two
         Line three""";
   }
}
~~~

Newlines in a triple-quoted string are always treated as \n characters, no matter whether the text file actually contains Unix (\n), Windows (\r\n) or Mac (\r) line endings. The indentation of the starting line (the line with the opening triple quote) is remembered, and other lines of the string are allowed to begin with the same indentation characters plus up to _three_ additional spaces, or _one_ additional tab. Therefore, in this example, "Line two" and "Line three" start with _no_ space characters.

Why are _three_ spaces allowed? It's the minimum number of spaces required to allow the first line to line up with subsequent lines:

~~~csharp
string GetMarkdown() 
{
   return 
      """- This is the first line, which lines up with the second.
         - This is the second line. None of the leading spaces count.
           - This line counts as having two spaces at the beginning""";
}
~~~

Triple-quoted strings support escape sequences, but they are slightly different than in normal strings. Ordinary C-style escape sequences are not interpreted, so these two strings are identical, each with 7 characters:

~~~csharp
string nab1 = @"\n\a\b\";
string nab2 = """\n\a\b\""";
~~~

Escape sequences in triple-quoted strings have the format \x/ instead of \x, so these two strings are equivalent:

~~~csharp
string newline1 = "Line1\r\nLine2";
string newline2 = """Line1\r/\n/Line2""";
~~~

To use three quotes in a row inside a triple-quoted string, you can use an escape sequence to avoid closing the string. These two strings are equivalent:

~~~csharp
string quotes1 = "  \"\"\"   ";
string quotes2 = """  ""\"/   """;
~~~

Triple-quoted strings are useful for printing menus and usage notes, e.g.

~~~csharp
class Program {
   public static void Main(string[] args)
   {
      Console.WriteLine(
         """What do you want to do?
              1. Loop
              2. Triangle
              X. Exit""");
      char k;
      while (!((k = Console.ReadKey(true).KeyChar).IsOneOf('1', '2', 'x', 'X'))) {}
      if (k == '1') 
         for(int i = 0;; i++)
            Console.WriteLine("Looping ({0})", i);
      if (k == '2')
         for (int i = -12; i <= 12; i++)
            Console.WriteLine(new string('*', Math.Abs(12 - Math.Abs(i)) * 2));
   }
}
~~~

In this example, the lines of the menu are indented by two spaces, since "1" and "2" are indented five spaces beyond the indentation level of the first line, and the first three spaces are ignored.

Binary literals and hex floats
------------------------------

EC# has binary literals. For example, `0b1111_1111` is 255. Also, any number (not just hex floats) allows `_` as a separator; the lexer simply ignores it. For example, `2_000_000_000` is two billion.

It also has binary and hex float literals. For example, `0b0101.11p+0` is 5.75, while `0x5.Cp+0` is _also_ 5.75. Why the `p+0`? The `p+0` suffix tells the lexer that the stuff after the decimal point is a floating-point number and not a member access. Remember, theoretically you could define an extension method on `int` so that `0x5.C()` is a valid method call. Therefore `0x5.C` requires the `p+0` suffix to "prove" that it is a number and not a member of the number.

Okay, that makes sense. But why `p+0`? This syntax comes directly from Java, which [also has hex float literals](https://blogs.oracle.com/darcy/entry/hexadecimal_floating_point_literals). It's actually an exponent, which indicates a power of two by which to shift the value of the number. For example, while `0x4.0p+0` is four, `0x4.0p+1` is eight, `0x4.0p+2` is sixteen, and `0x4.0p-1` is two.

Currently, the shorter Java syntax `0x5.Cp0` (without the `+`) is also allowed. Technically this could be a problem since "Cp0" could (technically) refer to an extension method, but it seems too unlikely to worry about it. After all, I'm only going for 99.9% backward compatibility.

Lexical Macros
--------------

Enhanced C# has a LISP-style macro system called LeMP (Lexical Macro Processor). It has its own name because it is agnostic regarding the input language; it is not C#-specific.

You'll see what LeMP is capable of in the following sections. Every feature you see from now on will be a LeMP macro, _not_ a built-in feature of Enhanced C#, although in some cases Enhanced C# has a syntactic feature that makes the macro easier to use. For example, the Enhanced C# parser recognizes these patterns:

~~~csharp
foo { code1; }
foo (expression) { code2; }
~~~

It translates these patterns to a syntax tree in which the last argument to `foo` is in braces:

~~~csharp
foo ({ code1; });
foo (expression, { code2; });
~~~

Other examples are `@@symbols`, tuples like `(1, "two", 3.0)`, and the method forwarding operator `==>`. In all these cases, the _syntax_ is built into Enhanced C# itself, but since there is no complete EC# compiler yet, a macro is required to make these features work.

Ideally, certain features in macros shouldn't _really_ be implemented as macros, and have "corner cases" that don't work, but could work properly as part of the EC# compiler that hasn't been written yet. I have implemented as many features as possible as macros, as a procrastication technique so I can do the "proper compiler" later, and also so I could find out just how much can be accomplished with macros alone. Many features can't be implemented with lexical macros, but on the other hand, many can.

Method forwarding
-----------------

Do you write wrapper classes sometimes, or implement the [decorator pattern](https://en.wikipedia.org/wiki/Decorator_pattern)? If you do, there's a good chance that you want to forward some of the methods to the original object without any other special actions. 

For example, if you need to write a wrapper for this interface:

~~~csharp
interface IFoo
{
   void MethodA(A objectA);
   long MethodB(A objectA, B objectB);
   string PropertyP { get; }
   object PropertyQ { get; set; }
}
~~~

You could do it like this:

~~~csharp
class FooWrapper : WrapperBase<IFoo>, IFoo 
{
   public FooWrapper(IFoo obj) : base(obj) {}
   void MethodA(A objectA) ==> _obj._;
   long MethodB(A objectA, B objectB) ==> _obj._;
   string PropertyP ==> _obj._;
   object PropertyQ { get ==> _obj._; set ==> _obj._; }
}
~~~

This is derived from `Loyc.WrapperBase<T>` in Loyc.Essentials.dll, an abstract class that helps you implement wrappers by automatically forwarding calls to `Equals()`, `GetHashCode()` and `ToString()`. It stores the original object in a field called `_obj`. That part of it has nothing to do with Enhanced C#.

After that you see the forwarding operator in action:

~~~csharp
long MethodB(A objectA, B objectB) ==> _obj._;
~~~

I think that writing wrappers is common enough to justify a little bit of syntactic sugar, so Enhanced C# defines a forwarding operator `==>` (I could have written the forwarding macro without changing the EC# parser, it would have just had to rely on a less elegant syntax).

After `==>` you specify the name of the target method or property. The underscore `_` refers to the name of the _current_ method or property, in this case `MethodB`. So the output code is:

~~~csharp
long MethodB(A objectA, B objectB)
{
   return _obj.MethodB(objectA, objectB);
}
~~~

**Note:** forwarding is not currently implemented for `event`s.

The quick binding operator
--------------------------

The new quick-binding operator allows you to create a variable any time you need one:

~~~csharp
if (DBConnection.Tables.Get("Person")::table != null) {
  foreach (var row in table)
     Process(row);
}
~~~

`::table` creates a variable called "table" to hold the return value of `DBConnection.Tables.Get("Person")`.

It makes your workflow easier. Imagine that you just wrote

~~~csharp
if (DBConnection.Tables.Get("Person") != null)
  foreach (var row in |
~~~

And then you realize: "wait, I need that table again!". This happens to me a lot, and over the years I've become adept at quickly rewriting the code as

~~~csharp
   var table = DBConnection.Tables.Get("Person");
   if (table != null)
      foreach (var row in table|
~~~

but I think you'll agree that it's far more convient to add "::table" than to 

1. select and cut the desired expression
2. write "table"
2. insert a newline before "if", then type "var table ="
3. paste the expression

I also think this is more readable. If you're reading a function that has lots of variable declarations, you can spend a lot of time figuring out what each variable is for. Quick-bind makes code shorter and tells you right away that a variable is used to cache a specific value.

The `::` operator itself already existed in C# before, but some of you may never have used it. It's used for namespace aliases, most notably [extern alias](https://msdn.microsoft.com/en-us/library/ms173212.aspx?f=255&MSPPError=-2147217396), to resolve naming conflicts between different assemblies. I've simply re-used it in Enhanced C#. Please note that you can still use `::` for its _original_ purpose, even in Enhanced C#.

Namespace aliases are simple identifiers, so if the expression before `::` is not a simple identifier, `::` can be used to create variables instead. **Note:** that this feature is currently implemented by LeMP, which doesn't know what namespace aliases have been defined. Instead it assumes that only _lowercase_ identifiers are namespace aliases and that _uppercase_ identifiers are values. For example `alias::Foo.Bar` will be treated as a reference to a namespace alias, while `Prop::Foo.Bar` will be treated as a variable declaration.

You could write

~~~csharp
new List<int>()::list;
~~~

instead of 
    
~~~csharp
var list = new List<int>();
~~~

but `::` is not intended to be used this way. Instead it is meant to appear "inline" so you can create a variable and immediately do something with it, like this:

~~~csharp
    Foo(new List<int>()::list);
~~~

A key feature of the new operator is its high precedence. In EC# you _could_ write code like this:

~~~csharp
    if ((var table = DBConnection.Tables.Get("Person")) != null)
      ...
~~~

but this approach requires parenthesis, since `var table = ... != null` would be parsed as `var table = (... != null)`. You don't want that, so you add parentheses. But this isn't very convenient, and the parentheses make the code slightly harder to read. In contrast, `::` binds as tightly as `.` does, so extra parenthesis are rarely needed.

To explain why I think we need this operator, I will use a math analogy. Math-speak is a little different than normal speech; mathematicians have a special way of speaking because it is more efficient than the alternative. For instance they might say: "consider a perfect number x in a set of integers S where x is coprime with some y in S, y != x.". They pair words together like "perfect number X" and "set of integers S", which are basically variable declarations embedded in a sentence. Mathematicians would be very unhappy if some grammar Nazis forced them to separate out those variable declarations: "x and y are integers, and S is a set of integers. x is a perfect number in S where..." It's longer, and when you are used to math-speak, it's slightly harder to understand the statement when the variable declarations are separated.

The quick-bind operator also promotes efficient code by nudging you toward the "pit of success". For example, in the past you might have written:

~~~csharp
if (AdjacencyList.Count > 1)
   pairs += AdjacencyList.Count - 1;
~~~

But in EC# it might be easier to write:

~~~csharp
if (AdjacencyList.Count::c > 1)
   pairs += c - 1;
~~~

And there's a reasonable chance this code is faster. `AdjacencyList` might be a property that reaches into some data structure to retrieve the list, and the `Count` property might be a virtual or interface method. Unless the JIT can be certain that `AdjacencyList.Count` is pure (has no side effects), the JIT cannot avoid evaluating it twice.

So, by using the quick binding operator, you've just optimized your code to call these properties only once. Congratulations, you've just fallen into the pit of success: you wrote faster code with little or no effort.

Of course, with variable declarations embedded in expressions like this, they can be slightly harder to notice. I'd like to experiment with syntax coloring that highlights variable declarations to make them more visible, but I don't have time to add that to the [syntax highlighter](https://github.com/qwertie/ecsharp/tree/master/Visual%20Studio%20Integration/LoycExtensionForVs).

`::` always creates a variable. It cannot change an existing variable.

~~~csharp
int ab;
if ((a + b)::ab > y) // ERROR
~~~

The variable created by `::` is mutable. My first draft of EC# said that the variables would be immutable, but that's impossible to enforce without writing a complete compiler. 

When I originally designed the `::` operator, it survived beyond the `if` statement itself, which I felt was better, because it made the operator useful in more situations:

~~~csharp
if (Computation(x)::y != 0) {}
Trace.WriteLine(y); // OK
~~~

Now I'm second-guessing myself though, for two reasons. The first reason is that when you declare a variable in a for-loop, it stops existing at the end of that loop:

~~~csharp
for (var y = Computation(x); y != 0; ...) {...}
Trace.WriteLine(y); // ERROR
~~~

Second, and more importantly, I consider Enhanced C# to be a series of feature requests for regular C#. I want these features in C# 7 and 8, but I'm guessing the C# team wouldn't allow the variable to survive beyond the end of the "if" statement or other location. If (to my surprise) they do allow the variable to survive, no harm done - the Enhanced C# code you write today will rarely be broken by such a change. But if it _is_ broken - like in this example:

~~~csharp
if (Expression::x > 0) {...}
if (Expression::x < 0) {...}
~~~

you could simply enclose the first `if`-statement in braces to fix the error.

Creating 'out' variables in-situ
--------------------------------

EC# also lets you create variables as targets of "out" or "ref" variables. This example speaks for itself:

~~~csharp
Dictionary<string,Form> _views;

// Shows a window corresponding to the specified key, or
// creates a new window for the key if one doesn't exist yet.
Form GetAndShow(string key)
{
   if (_views.TryGetValue(key, out Form form))
      form.BringToFront();
   else {
      _views[key] = form = new MyForm(key);
      form.Show();
   }
   return form;
}
~~~

This feature was planned to be added to C# 6, but strangely they removed the feature from the final compiler.

Because EC# doesn't have a complete compiler, it lacks access to type information about your program, so you must write the type of the `out` variable. This is necessary to be able to translate the code to plain C#.

`ref` variables are supported too, and require an explicit initializer, e.g.

~~~csharp
FunctionThatTakesARefVariable(ref int newInt = 5);
Console.WriteLine("Afterward, our variable is {0}.", newInt);
~~~

Code Contracts
--------------

Microsoft introduced [Code Contracts](http://research.microsoft.com/en-us/projects/contracts/) a few years ago, but since MS Code Contracts can't change the syntax of C#, they are slightly more unweildy to use than they should be, and even today they requires a Visual Studio extension to work (without which your contracts are silently ignored.)

Enhanced C# introduces its [own code contracts](http://ecsharp.net/lemp/ref-code-contracts.html), which are designed to work together with, or separately from, Microsoft Code Contracts. The single most handy contract is `notnull`:

~~~csharp
[field] notnull string Foo { get; set; }
~~~

This creates a contract in the `set { }` part that requires `value != null`, and a second contract in the `get { }` part that ensures the backing field is not `null`.

**Note**: Contract attributes on properties require a backing field; the `[field]` attribute creates a backing field.

You can learn more about code contracts in the [LeMP manual](http://ecsharp.net/lemp/ref-code-contracts.html).

Symbols
-------

I was first introduced to the concept of a symbol in Ruby, where is commonly used (instead of enumerations) to indicate options when calling a method. A symbol is like a string, except that it is "interned" by default. This means that the it is guaranteed that only one instance of a symbol string exists. Because of that, comparing two symbols for equality means comparing two references, which is faster than comparing two strings, and the the same speed as comparing two integer variables or enums.

The same concept exists in other languages too, such as LISP. Symbols are more convenient than enums for two reasons:

1. When calling a method, you don't have to write the name of the enum type.
2. When defining a method, you don't have to _define_ an enum type.

The second point is more important. A lot of times people use one or more boolean flags or integers rather than a descriptive enum because it is inconvenient to define one. Usually you don't want to define the enum right beside the method that needs it, because the caller would have to qualify the name excessively:

~~~csharp
class DatabaseManager {
    ...
    public static DatabaseConnection Open(string command, MaintainConnection option);
    public enum MaintainConnection {
       CloseImmediately, KeepOpen
    }
    ...
}
// later...
var c = DatabaseManager.Open("...", DatabaseManager.MaintainConnection.CloseImmediately);
~~~

Isn't that horrible? You don't want your clients to have to double-qualify the name like this. But it is inconvenient to maintain and document an `enum` located _outside_ the class. So to avoid the hassle, you replace `MaintainConnection option` with `bool maintainConnection` and you're done.

Symbols make this easier. They are written with `@@DoubleAtSigns`. The above code would be written like this in EC#:

~~~csharp
class DatabaseManager {
   ...
   public static DatabaseConnection Open(string command, 
      [requires(_.IsOneOf(@@CloseImmediately, @@KeepOpen))] Symbol option) {...}
   ...
}
// later...
void Open() {
   var c = DatabaseManager.Open("...", @@CloseImmediately);
}
~~~

The `[requires]` attribute is one of the code contracts mentioned in the previous section; the underscore `_` is a shortcut that refers to "the current parameter", i.e. `option`.

There's one more wrinkle. Since Enhanced C# isn't a "real" compiler yet, it needs a macro to enable this feature. That macro is called `#useSymbols`, and you need to write `#useSymbols;` near the top of any class that uses symbols. For example:

~~~csharp
class DatabaseManager {
   #useSymbols;
   public static DatabaseConnection Open(string command, 
      [requires(_.IsOneOf(@@CloseImmediately, @@KeepOpen))] Symbol option) {...}
   ...
}
~~~

`#useSymbols`'s job is to create static fields to hold each symbol that you use:

~~~csharp
class DatabaseManager
{
   static readonly Symbol sy_CloseImmediately = (Symbol) "CloseImmediately", 
                          sy_KeepOpen = (Symbol) "KeepOpen";
   public static DatabaseConnection Open(string command, Symbol option)
   {
      Contract.Assert(option.IsOneOf(sy_CloseImmediately, sy_KeepOpen), 
         "Precondition failed: option.IsOneOf(sy_CloseImmediately, sy_KeepOpen)");
   }
   ...
}
~~~

The `Symbol` type can be found in Loyc.Essentials.dll (part of LoycCore on NuGet) in the `Loyc` namespace.

Since the `Symbol` class has a explicit cast from `string`, in plain C# you can write `(Symbol)"SymbolName"` to convert a string to a Symbol at runtime, in order to call methods that expect Symbols.

`on_finally`
------------

If you write a lot of try-finally blocks, this one is for you.

C# has `using (...)`, which is great, but it requires an object that implements `IDisposable`, and sometimes the thing you want to do doesn't really work that way. For example, let's say you want to set a static property when entering a method, and revert to the old value when leaving the method:

~~~csharp
void Method()
{
   var oldValue = SomeBclClass.StaticProperty;
   SomeBclClass.StaticProperty = newValue;
   try {
      10 lines of code;
   } finally {
      SomeBclClass.StaticProperty = oldValue;
   }
}
~~~

This is the kind of thing where `on_finally` is useful. The above code can be rewritten like so:

~~~csharp
void Method()
{
  var oldValue = SomeBclClass.StaticProperty;
  SomeBclClass.StaticProperty = newValue;
  on_finally { SomeBclClass.StaticProperty = oldValue; }

   10 lines of code;
}
~~~

`on_finally` groups together the code that sets and restores the value of the property, it eliminates two lines of code, and it removes the indentation on those `10 lines of code`.

Actually, I often find myself wanting to save or use the _old_ value of something just before I _change_ the value of something. I feel like there should be some way to write `SomeBclClass.StaticProperty` only twice, instead of three times, and in the past I have suggested a ["slide operator"](http://loyc.net/2010/i-want-slide-operator.html) as the solution. But such an operator is not currently implemented; so if you like this idea, feel free to write it as a user-defined macro. Ask me how.

`on_finally`, which works like the `defer` statement in Swift and the `scope(exit)` statement in D, is actually part of a group of related macros. The others are [`on_return`, `on_throw` and `on_throw_catch`](http://ecsharp.net/lemp/ref-on_star.html).

Tuples and deconstruction
-------------------------

Sometimes you want to return multiple values from a function. Traditionally this is accomplished with `out` parameters, and as you've seen, EC# makes `out` parameters easier to use. Another alternative is tuples, a mechanism for bundling values together. Each value in a tuple can have a different type.

Tuple classes were added in .NET 4, but there is no special support for them in C#. To make a tuple in EC#, simply write a list of values in parenthesis:

~~~csharp
var tuple = (1, "2", 3.0);
int one = tuple.Item1;
string two = tuple.Item2;
~~~

EC# is not committed to a special syntax for expressing the type of a tuple; the traditional type-declaration syntax is recommended. For example, here is a method that returns a list of values together with the index of each value:

~~~csharp
public static IEnumerable<Tuple<T, int>> WithIndexes(this IEnumerable<T> list)
{
   int i = 0;
   foreach(T value in list)
      yield return (value, i++);
}
~~~

If you prefer [Pairs](http://ecsharp.net/doc/code/structLoyc_1_1Pair_3_01T1_00_01T2_01_4.html) over tuples of two, you can use [`#setTupleType(2, Pair);`](http://ecsharp.net/lemp/ref-other.html#settupletype) to accomplish that.

The real power of tuples is that you can easily "unpack" them in EC#. If a function returns a pair of things (even if that function was written in "plain" C#), you can write:

~~~csharp
(var firstThing, var secondThing) = FunctionThatReturnsAPair();
~~~

There are few shortcuts for _lists_ of tuples; for example you can't currently write code as elegant as

~~~csharp
list.WithIndexes().ForEach(((var item, int i)) => Console.WriteLine($"list[$i] = $item"));
~~~

However, this is valid EC# _syntax_, which means someone could implement this feature as a macro if they so desired.

Here, two variables "item" and "i" are created to hold the two subvalues of each tuple.

Replace and unroll
------------------

If, for some reason, you are forced to write several methods that are identical except for one data type, or several classes that are identical except for one thing, then `replace`, `unroll` and code snippets might be for you.

I'll just explain this briefly with an example. Let's suppose your class has a bunch of properties and that for some reason you are required to write a separate method that does some task for each property. Why would you need so many separate methods? I don't know. It's a code smell. But sometimes in the real world, you get yourself in odd situations and you find yourself having to do repetitive tasks. So let's just say you've landed in one of these situations, you're writing repetitive code, and there's no alternative. 

That's where `unroll` and `replace` come in. Let's say the task has something to do with, I don't now, serialization:

~~~csharp
unroll ((PropX, DefValueX) in 
        ((PropA, DefValueA), 
         (PropB, DefValueB),
         (PropC, DefValueC),
         (PropD, DefValueD)))
{
   replace(SavePropX => concatId(Save, PropX));
   void SavePropX(SerialBox serializer)
   {
      if (PropX != DefValueX) {
         serializer.Write(nameof(PropX));
         serializer.Write(PropX);
      }
   }
}
~~~

This generates four methods, called `SavePropA`, `SavePropB`, `SavePropC`, and `SavePropD`:

~~~csharp
void SavePropA(SerialBox serializer)
{
   if (PropA != DefValueA) {
      serializer.Write("PropA");
      serializer.Write(PropA);
   }
}
void SavePropB(SerialBox serializer)
{
   if (PropB != DefValueB) {
      serializer.Write("PropB");
      serializer.Write(PropB);
   }
}
// and so on
~~~

How does it work?

- `unroll` creates several copies of a piece of code, replacing some identifiers in each one
- `replace` replaces one "pattern" with another; in this case `SavePropX` becomes `concatId(Save,PropX)`. `replace` is used here because you can't use `concatId(Save, PropX)` directly as a method name (the parser cannot parse it).
- `concatId` is used to combine two or more identifiers, e.g. `concatId(Comb, ined)` becomes `Combined`.

In general, outer macros run before inner macros (this is the opposite of the evaluation order of normal methods). So `unroll` runs before `replace`, and `concatId` runs last. Since `unroll` runs before `concatId`, one of the things it does is to change the second argument of `concatId(Save, PropX)` into `PropA`, `PropB`, or whatever, so that the final output is `SavePropA`, `SavePropB`, etc.

`replace` and `unroll` are also useful for implementing `INotifyPropertyChanged`. See [here](http://ecsharp.net/lemp/avoid-tedium-with-LeMP.html#unroll) for an example.

"With" statement
----------------

This is an easy one, it lets you do a series of operations on an object:

~~~csharp
with (Foo()) {
   .Bar();
   .Property = 0;
   Foo(.Baz(.Property));
}
~~~

This produces output like

~~~csharp
{
   var tmp_0 = Foo();
   tmp_0.Bar();
   tmp_0.Property = 0;
   Foo(tmp_0.Baz(tmp_0.Property));
}
~~~

DSLs, "Record" types, disjoint unions and pattern matching
----------------------------------------------------------

It would be straightforward to write a macro that would let you write "one liner" type definitions, like

~~~csharp
public record Point(int X, int Y);
~~~

If there's demand, I could write an article about that. This could be considered an example of a [DSL](https://en.wikipedia.org/wiki/Domain-specific_language), and since DSLs are all the rage these days, I could write an article about "how to write a DSL in Enhanced C#" - if there's demand for such an article. Dear reader, what DSL would you like to see built on top of C#, that you _can't_ write due to limitations of C# language as it is today?

LeMP comes with a more flexible mechanism for building types quickly, but it uses three lines of code instead of one. It's called the alt class:

~~~csharp
public alt class Point {
   alt this(int X, int Y);
}
~~~

This creates an immutable type (in this case `Point`) with the public properties you asked for (in this case `X` and `Y`). The syntax may seem a bit mysterious at first - "what's `alt this` supposed to mean?" you may ask. Well, "alt class" is really designed to produce entire class hierarchies rather than simple types; the latter is an afterthought (and as a consequence, it currently only builds classes, not structs, though it would be straightforward to remove this limitation.) So `this` refers to the _current_ class, as opposed to a _subclass_. A [section](http://ecsharp.net/lemp/pattern-matching.html#algebraic-data-types) of my previous article explains this in more detail.

Enhanced C# also has a pattern matching construct called `match`, which is just like `switch`, but the `case`s are "patterns" rather than constants. You can read all about pattern matching and `alt class` in my older article, ["C# Gets Pattern Matching, Algebraic Data Types, Tuples and Ranges"](http://ecsharp.net/lemp/pattern-matching.html).

`quote` and `matchCode` for code generation and analysis
--------------------------------------------------------

If you'd like to analyze or generate source code, LeMP is really good for that; please read [this article](http://ecsharp.net/lemp/lemp-code-gen-and-analysis.html) to learn more.

However, since there's no complete compiler and no one has implemented Roslyn integration, you can only use it to analyze source code, not the semantics of that code.

`matchCode` is also super useful if you're using [LES for configuration files or DSLs or a prototype programming language](http://loyc.net/les/), because the syntax trees produced by LES are the same ones produced by EC# - they're both [Loyc trees](http://loyc.net/loyc-trees). Perhaps I should write an article about that someday.

Text parsers
------------

Regexes are pretty convenient, but if you'd like an alternative to regexes that is more readable and is compiled at compile-time, consider [LLLPG](http://ecsharp.net/lllpg). This example parses integers:

~~~csharp
LLLPG(lexer(inputSource(src), inputClass(LexerSource)))
{
   public static rule int ParseInteger(string input) 
   @{
      {var src = (LexerSource)input;}
      ' '* // skip spaces
      '-'?
      (d:'0'..'9' {$result = $result * 10 + ($d - '0');})+ 
      {if ($'-' != 0) return -$result;}
      EOF
      // LLLPG returns $result automatically
   };
}
~~~

Building classes quickly
------------------------

I don't know about you, but I write a lot of "simple" classes and structs, particularly the kind known as "plain-old data" or POD, meaning, little groups of fields like this:

~~~csharp
class FullAddress {
   public string Address;
   public string City;
   public string Province;
   public string Country;
   public string PostalCode;

   public FullAddress(string address, string city, string province, 
                      string country, string postalCode = "") {
      Address = address;
      City = city;
      Province = province;
      Country = country;
      PostalCode = postalCode;
   }
}
~~~

You don't have to write classes like this very many times before you start to get annoyed at having to repeat the same information over and over: each of "address", "city", "province", "country" and "postalCode" are repeated four times with varying case, "string" is stated ten times, and "FullAddress" is repeated twice (three times if you add a default constructor).

In EC# you get the same effect with much shorter code:

~~~csharp
class FullAddress {
   public this(
      public string Address,
      public string City,
      public string Province,
      public string Country,
      public string PostalCode) {}
}
~~~

There are two bits of new syntax here:

1. `this` denotes a constructor. It lets you make a constructor without the inconvenience of repeating the class name.
2. You can apply modifiers, like `public`, in many places where it is illegal in C#.

By marking each parameter as `public`, you are instructing EC# to create a `public` field which is set to the value of the parameter. The constructor is replaced with the same code you saw earlier:

~~~csharp
public string Address;
public string City;
public string Province;
public string Country;
public string PostalCode;

public FullAddress(string address, string city, string province, 
                   string country, string postalCode = "") {
   Address = address;
   City = city;
   Province = province;
   Country = country;
   PostalCode = postalCode;
}
~~~

You can create properties in the same way:

~~~csharp
public this(
   public string Address    { get; private set; },
   public string City       { get; private set; },
   public string Province   { get; private set; },
   public string Country    { get; private set; },
   public string PostalCode { get; private set; } = "") {}
~~~

You can also write an "update" function that changes existing fields or properties, using "set" parameters:

~~~csharp
public void SetAddress(set string address, set string city, 
                       set string province, set string country, 
                       set string postalCode = "") { }
~~~

This produces

~~~csharp
public void SetAddress(string address, string city, string province, string country, string postalCode = "")
{
   this.address = address;
   this.city = city;
   this.province = province;
   this.country = country;
   this.postalCode = postalCode;
}
~~~

The `set` prefix is required on each field/property that you want to set, otherwise the parameter has no special behavior. For example, as a convenience we might accept a separate building number:

~~~csharp
public void SetAddress(
   int buildingNumber,
   string streetName,
   set string City,
   set string Province,
   set string Country,
   set string PostalCode)
{
   Address = buildingNumber + " " + streetName;
}
~~~

Easter eggs
-----------

EC# has a small number of "easter eggs" in it, which are homages to other languages.

Specifically, EC# supports some "alternate" syntax elements from other languages. An alternate syntax allows you to import code more quickly from another C-style language by permitting syntax elements from that language. Only small and unambiguous differences are permitted this way. The currently supported alternate syntax elements are:

1. `:=` operator (from Go): behaves the same as the `::` operator, except that the precedence is the same as `=`, and the name of the new variable is on the left-hand side.
2. `!` operator (from D): alternate mechanism for specifying generic arguments, e.g. `List!int` means `List<int>`, and `Dictionary!(string,object)` means `Dictionary<string,object>`.
3. "scope(exit)" (from D): this is really a macro.

It is possible for users to support other kinds of imported syntax in EC# by writing macros that interpret @{...} blocks, but that's a bit advanced, and there is no way to change the token parser.

I am open to suggestions about other "syntax easter eggs" that would be helpful for people doing manual code conversion, but requests will be rejected if they create ambiguities. It is also possible to write automatic code conversion libraries, but that's a topic for another article.

Things we can't do yet
----------------------

I've always felt, much like the [CS-Script guy](http://www.csscript.net/) Oleg Shilo, that C# would be great as a scripting language, and that there's no solid justification for requiring functions to be inside classes. So, one of many other features I'd like to add to C# is to simply drop that requirement and let you put methods, fields, properties and events outside any type. However, because Enhanced C# is not a full C# compiler - and because of a specific technical limitation of C# - EC# can't support global methods at the present time, except in limited use cases.

Similarly, I would be interested in adding [extension properties](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2242236-allow-extension-properties), [traits](http://en.wikipedia.org/wiki/Trait_(computer_programming)) as a substitute for multiple inheritance, ["static extension methods"](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2060313-c-support-static-extension-methods-like-f) (not literally, but a simple feature called multiple-source name lookup or MSNL which would be even better), [`params IEnumerable`](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/3148411--params-keyword-for-every-list), allowing [readonly local variables](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/category/30931-languages-c?page=3), new [generic constraints such as Enum](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2557231-enums-constraint-for-generics), ["implicit" interfaces](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/4272089-support-implicit-interfaces-for-code-reuse) - see my [prior work](http://www.codeproject.com/Articles/87991/Dynamic-interfaces-in-any-NET-language) on this, [default interface implementations], [labeled `break`s](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/6340889-allow-the-c-break-statement-to-terminate-sever), [covariant return types](https://visualstudio.uservoice.com/forums/121579-visual-studio-2015/suggestions/2546150-support-covariant-return-types), a lightweight doc-comment system based on Markdown (or even that old Java system) that allows me to include the source code of short methods _right in the documentation_... 

Suffice it to say, the list of things I want goes on for a long time, but I can't do it alone and would need help - perhaps a _lot_ of help.

The End?
--------

\* In the beginning of this article I used the _present tense_ when I said EC# was a grand plan to transform C# into a much more powerful and more succinct language. That was the plan over three years ago when I started writing this article. But over those years, as I have published articles about the component parts and dependencies of Enhanced C# - [Loyc Core](http://core.ecsharp.net), [Loyc trees](http://loyc.net/loyc-trees), and so forth - I have become increasingly aware of how deeply indifferent most people are toward improving the foundations of .NET software.

Perhaps Jon Skeet put it best, when I emailed him two weeks ago about LeMP and Enhanced C#. He told me why _he_ wouldn't use it:

> I really don't want my code to be in a language that hardly anyone else knows - and which could conflict with future releases of C#. It's definitely an interesting project, and I could imagine people using it for hobby projects where it didn't really matter much if the whole thing went to pot (e.g. if you became unable to work on it) but I wouldn't want to use it for anything important due to those concerns.

People won't use it (or even blog about it, apparently) because it isn't popular. And it isn't popular because people won't use it. Classic chicken and egg. Plus, as Jon has pointed out, since Microsoft isn't paying me, I'm not improving the _real_ C#. Since Microsoft isn't paying me, my [libraries](http://core.ecsharp.net) do not improve the _real_ BCL. And although it has always been open source, he has presumed that no one would be willing to maintain it if I died tomorrow. Sadly, that's probably true.

The slow uptake of [Nemerle](http://nemerle.org/About) I could _somewhat_ understand; historically (if not right now) a lot of its documentation was in Russian, its wiki was a mess, it wasn't made by Microsoft and you couldn't easily migrate your C# code base to Nemerle. I could even understand the slow uptake of F#; as soon as it came out I tried to use it, but was immediately baffled by many of its syntax elements. A couple of years later I tried again, only to be thwarted by cryptic error messages while trying to write what I thought was a simple constructor. So syntactically, I found F# substantially harder than OCaml (with which I also have little experience), and I think ordinary C# and VB developers would have a hard time using it. Plus, no Windows Forms designer?

But honestly, it took me by surprise that backward compatibility with C#, and the fact it translates to plain C#, hasn't convinced at least a _few_ people to use LeMP / Enhanced C#, let alone improve it.

This article represents my last chance. I started out publishing [articles about the underlying ideas](http://www.codeproject.com/Articles/606610/LLLPG-cplusLoycplustrees-cplusandplusLES-cpluso), I've published articles about [the parser generator](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp), I've published articles about the [macro processor](http://www.codeproject.com/Articles/995264/Avoid-tedious-coding-with-LeMP-Part), and of course I've documented (if not promoted) [related technologies](http://loyc.net/les), my [collection types](http://core.ecsharp.net/collections) and all the rest of the [class libraries](http://ecsharp.net/doc/code). And I already published a whole [web site](http://ecsharp.net/lemp) and [reference manual](http://ecsharp.net/lemp/reference.html) for EC#'s macro engine. But as far as I can tell, no one but me has blogged about any of this, and other than me, I only have one confirmed user of my libraries. From where I sit, this whole project has been a failure of epic proportions.

Just to illustrate: I wrote a benchmark pitting C# versus C++ on CodeProject and ultimately got 522,000 views. Later, after months of work, I published a complex and feature-rich [data structure](http://cdn.codeproject.com/Articles/568095/The-List-Trifecta-Part) I had built and it got only a couple thousand views at first, with not quite 12,000 views trickling in over the last three years, and no comments. I published an article about [using the _old_ version of ICSharpCode.TextEditor](http://www.codeproject.com/Articles/30936/Using-ICSharpCode-TextEditor) and it got 215,000 views; I published my first article on LeMP ten months ago, and it earned just 11,000. After republishing several new versions of my [LLLPG article](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp) over the last couple of years, I did manage to rack up nearly 193,000 views - really? It was under 100,000 last time I checked! So that's good news... but so far I'm not aware of any actual _users_.

I keep hoping that by working harder and publishing new articles and putting a new spin on things, I could stir up some interest in my work, but experience has taught me that the vast majority of coders don't give a ʞɔnɟ. Encouraging comments are appreciated, and every, oh, every year or so I get three or four of those (thanks Jonathan and Kerry!), but what I really want is developers using this stuff, asking me questions, filing bug reports, telling me what their needs are and [hacking on the source code](http://ecsharp.net/help-wanted.html).

So yes, EC# _could_ transform C# into a much more powerful and more succinct language. But if I can't get 1%, or even 0.01% of C# developers to use it, I'll just be wasting my life. So, what'll it be? Should I quit now? I'm leaning toward "yes" (I'd still fix bugs and answer your questions, of course). There's this thing out there called WebAssembly, and there are other parts of the [Loyc initiative](http://loyc.net) that are largely unexplored. One thing's for sure: regardless of what I do with my life, it's going to be **extremely** hard to break through the apathy barrier and make a difference in the world.

It isn't just programming languages, either. I've worked on International Auxiliary Languages and noticed that the common man's reaction ranges from indifference to outright hostility. I found strong evidence that the religion I grew up in is not true, but I'm fairly sure most members wouldn't respond well if I told _them_ that. I've been trying to support the fight against corruption in U.S. politics, but while many people _vaguely_ recognize the problem, getting them to rally around a solution that would _actually_ work seems, well, unworkable. The list goes on, and the bottom line seems to be, I was born on the wrong planet. My kind is not welcome here.

And yet... I would like so much to make a difference. If only someone would let me, or help me.

Exercises for the reader
------------------------

It should be possible to port some of the features of [C omega](http://research.microsoft.com/en-us/um/cambridge/projects/comega/doc/comega_whatis.htm) to Enhanced C#. Anyone up for it? Ask me how to write a macro.

See [here](http://ecsharp.net/help-wanted) or [add an issue](https://github.com/qwertie/ecsharp/issues) if you'd like to help develop Enhanced C#.

Also, has anyone seen information about advanced usage and composition of LISP macros? I can't actually program in LISP so I've had difficulty understanding web sites that talk about how LISP dialects do macro composition and hygiene; in particular, controlling composition and evaluation order is tricky and I haven't figured out what abstraction would work best for this. So ... advice wanted.