---
title: C# Gets Pattern Matching, Algebraic Data Types, Tuples and Ranges
layout: article
date: 7 Mar 2016
tagline: Well, not literally. LeMP/EC# supports pattern matching, ADTs, and tuples, so C# gets all that by transitivity.
toc: true
---

_March 7, 2016_

Pattern matching!
-----------------

There is a code pattern that pops up occasionally: "get an object from somewhere, see if it has type X, and if so, get/query its properties". This can get tedious very quickly:

~~~csharp
var obj = connection.DownloadNextObject();
if (obj is StatusReport)
{
	StatusReport report = (StatusReport)obj;
	if (report.IsValid) {
		SaveReport(report);
	}
}
else if (obj is DataPacket)
{
	DataPacket packet = (DataPacket)obj;
	DoStuffWith(packet);
}
~~~

I bet you've written code like this. Some of you write this style of code a lot. But now LeMP has a shortcut for patterns like these: it's called `match`. It's like `switch`, but for "pattern matching". With `match`, the code above becomes simply

~~~csharp
match (connection.DownloadNextObject()) {
	case $report is StatusReport(IsValid: true):
		SaveReport(report);
	case $packet is DataPacket:
		DoStuffWith(packet);
}
~~~

easy, right? LeMP will translate this to C# code like

~~~csharp
do {
	var tmp_1 = connection.DownloadNextObject();
	if (tmp_1 is StatusReport) {
		StatusReport report = (StatusReport) tmp_1;
		if (true.Equals(report.IsValid)) {
			SaveReport(report);
			break;
		}
	}
	if (tmp_1 is DataPacket) {
		DataPacket packet = (DataPacket) tmp_1;
		DoStuffWith(packet);
		break;
	}
} while (false);
~~~

`match` allows the case blocks to use `break` to exit each branch, but unlike `switch` it does not require the `break` statements. `match` wraps its output in `do...while(false)` in case your `case`-block includes a `break` statement (and, well, it adds a `break` if you don't). Note that the logic here isn't quite the same as the original `if-else` code: `match` behaves like a big "if-else" chain, so if the first `case` doesn't match, it always tries the second one. So if it's a `StatusReport` but `IsValid` is false, it'll go on and check whether it's a `DataPacket` (this may sound like a dumb thing to do, but think about it: it is possible that the object is _both_ a `StatusReport` _and_ a `DataPacket`. And if that's impossible, maybe if we're lucky the compiler will optimize it.)

`match` has more features, which we'll discuss later. But first, a word from its sponsor:

Algebraic Data Types!
---------------------

Many languages offer data types that consist of a series of alternatives, like this simple representation of a [binary search tree](https://en.wikipedia.org/wiki/Binary_search_tree) in Haskell:

~~~
data BinaryTree t = Leaf t
                  | Node t (BinaryTree t) (BinaryTree t)
~~~

This says that a value of the `BinaryTree` type is generic (`t` is a type parameter, like `<T>` in C#), a `BinaryTree` value is _either_ a `Leaf` (which contains a single value of type `t`) or a `Node` (which contains a value of type `t` and two more values, each of type `BinaryTree t`). Haskell folks call this an [Algebraic Data Type](https://wiki.haskell.org/Algebraic_data_type).

Algebraic Data Types (ADTs) may have a dumb name, but they are also known by another name: disjoint unions. 

With LeMP you can write ADTs using `alt class`. You will see that C# ADTs these don't work the same as disjoint unions in other languages; because of their increased flexibility, arguably they shouldn't be called "disjoint unions" at all. But whatever you call them, they are quite useful, especially when productivity and concise code are your top priorities.

Under LeMP, an ADT is called `alt class` and it looks like this:

~~~csharp
public abstract alt class BinaryTree<T> where T: IComparable<T>
{
	alt Leaf<T>(T Value);
	alt Node(T Value, BinaryTree<T> Left, BinaryTree<T> Right);
	// (you could also have written `Node<T>`)
}
~~~

In fact, this is nothing more or less than a really quick way to produce a class hierarchy of immutable types. The output is fairly massive:

~~~csharp
public abstract class BinaryTree<T> where T: IComparable<T>
{
	public BinaryTree() { }
}
class Leaf<T> : BinaryTree<T> where T: IComparable<T>
{
	public Leaf(T Value)
	{
		this.Value = Value;
	}
	public T Value { get; private set; }
	public Leaf<T> WithValue(T newValue)
	{
		return new Leaf<T>(newValue);
	}
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public T Item1
	{
		get { return Value; }
	}
}
static partial class Leaf
{
	public static Leaf<T> New<T>(T Value) where T: IComparable<T>
	{
		return new Leaf<T>(Value);
	}
}
class Node<T> : BinaryTree<T> where T: IComparable<T>
{
	public Node(T Value, BinaryTree<T> Left, BinaryTree<T> Right)
	{
		this.Value = Value;
		this.Left = Left;
		this.Right = Right;
	}
	public T Value { get; private set; }
	public BinaryTree<T> Left { get; private set; }
	public BinaryTree<T> Right { get; private set; }
	public Node<T> WithValue(T newValue)
	{
		return new Node<T>(newValue, Left, Right);
	}
	public Node<T> WithLeft(BinaryTree<T> newValue)
	{
		return new Node<T>(Value, newValue, Right);
	}
	public Node<T> WithRight(BinaryTree<T> newValue)
	{
		return new Node<T>(Value, Left, newValue);
	}
	// Additional code hidden to spare your eyes
}
static partial class Node
{
	public static Node<T> New<T>(T Value, BinaryTree<T> Left, BinaryTree<T> Right) where T: IComparable<T>
	{
		return new Node<T>(Value, Left, Right);
	}
}
~~~

As you can see, there's a lot of helper code to make these types easy to use.

First of all, you don't just get a `Leaf<T>` and `Node<T>` class, you also get `Leaf` and `Node` classes with no type parameters. These allow you to create leaves and nodes without mentioning the type T:

~~~csharp
void TreeOfThree()
{
	var tree = Node.New(42, Leaf.New(17), Leaf.New(99));
}
~~~

**Note**: A `New` method is created only when an `alt` uses type parameters.

You can "modify" individual properties of an ADT with the appropriate "`With`" method, like this:

~~~csharp
Node<T> node = Node.New(42, Leaf.New(17), Leaf.New(99));
// Use `WithRight` to change the right child to the leaf `101`:
node = node.WithRight(Leaf.New(101));
~~~

And of course, you can use `match` to learn about your ADT. For example, if the binary tree is sorted, it could be searched like this:

~~~csharp
public static bool Contains<T>(BinaryTree<T> tree, T item)
{
	T value;
	match (tree) {
		case is Leaf<T>($value):
			return Compare(value, item) == 0;
		case is Node<T>($value, $left, $right):
			int cmp = Compare(item, value);
			if (cmp < 0)
				return left != null && Contains(left, item);
			else if (cmp > 0)
				return right != null && Contains(right, item);
			else
				return true;
	}
}
internal static int Compare<T>(T a, T b) where T:IComparable<T>
{	// It's null's fault that this method exists.
	if (a != null)
		return a.CompareTo(b);
	else if (b != null)
		return -a.CompareTo(a);
	else
		return 0;
}
~~~

Notice that this code says `Leaf<T>($value)` instead of `Leaf<T>(Value: $value)`, and similarly `case is Node<T>` does not mention `Value`, `Left` or `Right`. If you leave out the property names, `match` will read items "by position" from `Item1`, `Item2`, etc., and that's why the generated `Leaf<T>` has a `public T Item1` property, which is marked with `EditorBrowsableState.Never` to hide it from IntelliSense.

But wait, there's more! In fact, `alt class` can do more than ADTs in other languages that support ADTs, because it "accepts" its identity as a class hierarchy instead of pretending to be a traditional mathematical disjoint union: _it has the same capabilities as a normal class hierarchy_.

For starters, notice that `Leaf` and `Node` both have a `Value` property. We can move the common property into the base class like this:

~~~csharp
public partial abstract alt class BinaryTree<T> where T: IComparable<T>
{
	alt this(T Value);
	alt Leaf();
	alt Node(BinaryTree<T> Left, BinaryTree<T> Right);
}
~~~

`alt this` provides a way to add data to the base class. Plus, if you give it a body in `{ braces }`, it becomes the code of the constructor.

Now, since `Leaf()` contains no additional data beyond what the base class contains, we can go a step further and eliminate it completely, while removing the `abstract` attribute from `BinaryTree<T>`:

~~~csharp
public alt class BinaryTree<T> where T: IComparable<T>
{
	alt this(T Value);
	alt Node(BinaryTree<T> Left, BinaryTree<T> Right);
}
~~~

This does have a disadvantage: leaf nodes are created with `BinaryTree.New` instead of `Leaf.New`. But it still works.

If we'd like to ensure that `Node` is not initialized with two `null` children, we can add validation code. This is done by adding a body to `Node`, which is treated as a class body, and then adding a "constructor" named `alt this`:

~~~csharp
public alt class BinaryTree<T> where T: IComparable<T>
{
	alt this(T Value);
	alt Node(BinaryTree<T> Left, BinaryTree<T> Right)
	{
		public alt this() {
			if (Left == null) throw new ArgumentNullException("Left");
			if (Right == null) throw new ArgumentNullException("Right");
		}
	}
}
~~~

**Note**: admittedly, there is something odd about this: the new `this` constructor has its own argument list, even though `Node` already had one. As you saw from the first constructor (`alt this(T Value)`), constructor arguments in an `alt class` create new properties. So you are allowed to place new properties in either the `Node` list or the inner `this` list; it is recommended not to use both.

Also, instead of using `match`, you could implement the `Contains` method as a virtual method.

~~~csharp
public alt class BinaryTree<T> where T: IComparable<T>
{
	alt this(T Value);
	alt Node(BinaryTree<T> Left, BinaryTree<T> Right)
	{
		public alt this() {
			if (Left == null && Right == null) throw new ArgumentNullException("Both children");
		}
		public override bool Contains(T item)
		{
			int cmp = Compare(item, Value);
			if (cmp < 0)
				return Left != null && Left.Contains(item);
			else if (cmp > 0)
				return Right != null && Right.Contains(item);
			else
				return true;
		}
	}
	
	public virtual bool Contains(T item)
	{
		return Compare(Value, item) == 0;
	}
	internal static int Compare(T a, T b)
	{	// It's null's fault that this method exists.
		if (a != null)
			return a.CompareTo(b);
		else if (b != null)
			return -a.CompareTo(a);
		else
			return 0;
	}
}
~~~

You can also define a multi-level class hierarchy. In the following example, `MyTuple<T1,T2>` is derived from `MyTuple<T1>` and `MyTuple<T1,T2,T3>` is derived from `MyTuple<T1,T2>`:

~~~csharp
public alt class MyTuple<T1> {
	public alt this(T1 Item1);
	public alt MyTuple<T1,T2>(T2 Item2) {
		public alt MyTuple<T1,T2,T3>(T3 Item3) { }
	}
}
~~~

A more complex example would be a tree of GUI widget information:

~~~csharp
public abstract alt class Widget {
	alt this(Rectangle Location) {
		if (Location == null) throw new ArgumentNullException("Location");
	}
	alt Button(string Text) { }
	alt TextBox(string Text) { }
	abstract alt StringListWidget(string[] subItems) {
		alt ComboBox();
		alt ListBox();
	}
	public abstract alt Container() {
		alt TabControl(TabPage[] Children);
		alt Panel(Widget[] Children) {
			alt TabPage(string Title);
		}
	}
}
~~~

Just remember that the nested `alt`s are _not_ nested classes; in the output, each new class is brought out to the "top level". This means, for example, that you should write `new ComboBox(location, subItems)` and **not** `new Widget.StringListWidget.ComboBox(location, subItems)`.

Finally, you can use `alt class` to produce immutable classes with only one "case", like this:

~~~
public abstract alt class Rectangle {
	alt this(int X, int Y, int Width, int Height);
}
~~~

**Note**: as of this writing, `alt class` does not support non-public constructors.

Okay, I think we covered everything! I hope you enjoy.

Tuples
------

Enhanced C# supports tuples, e.g. 

~~~csharp
var pair = (12, "twelve");
~~~

This comes out as

~~~csharp
var pair = Tuple.Create(12, "twelve");
~~~

**Note**: [`Tuple`](https://msdn.microsoft.com/en-us/library/system.tuple%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396) is a standard class of the .NET framework.

You can also deconstruct tuples, like this:

~~~csharp
(var num, var str) = pair; 
~~~

Output:

~~~csharp
var num = pair.Item1;
var str = pair.Item2;
~~~

Since ADTs have properties named `Item1`, `Item2`, etc., you can deconstruct them the same way:

~~~csharp
(var x, var y, var w, var h) = new Rectangle(10, 10, 600, 400);
~~~

Output:

~~~csharp
var tmp_0 = new Rectangle(10, 10, 600, 400);
var x = tmp_0.Item1;
var y = tmp_0.Item2;
var w = tmp_0.Item3;
var h = tmp_0.Item4;
~~~

However, EC# does not help you write tuple _types_. For example, to return `(5,"five")` from a function, the return type is `Tuple<int, string>` - there is no shortcut.

More Pattern Matching
---------------------

### With tuples ###

`match`, too, supports tuples and other things that have `Item1`, `Item2`, etc.:

~~~csharp
	match (rect) {
	case ($x, $y, $w, $h):
		Console.WriteLine("("+x+","+y+","+w+","+h+")");
	}
~~~

But this is no different from the tuple deconstruction shown above. It bypasses type checking entirely; the code is simply

~~~csharp
do {
	var x = rect.Item1;
	var y = rect.Item2;
	var w = rect.Item3;
	var h = rect.Item4;
	Console.WriteLine("(" + x + "," + y + "," + w + "," + h + ")");
	break;
} while(false);
~~~

This means if `rect` itself has the wrong type (e.g. it's a tuple of 3 rather than 4) you'll get an error from the C# compiler. Remember, you have to use `is` to check the data type:

~~~csharp
	match (rect) {
	case is Rectangle($x, $y, $w, $h):
		Console.WriteLine("("+x+","+y+","+w+","+h+")");
	}
~~~

Often, though, it's useful to use tuple syntax once you know what the type is. For example, after using `is Button`, we know we have a `Button` widget, we know the first component is a `Rectangle` so we can deconstruct it with tuple syntax, like this:

~~~csharp
void DrawIfButton(Graphics g, Widget widget) {
	match(widget) {
		case is Button(($x, $y, $width, $height), $text):
			// TODO: draw the button
	}
}
~~~

Simple matching
---------------

What else can you do with pattern matching? For one thing, you can do _equality testing_ and _range testing_, like this:

~~~csharp
static void FavoriteNumberGame()
{
	Console.Write("What's your favorite number? ");
	match(int.Parse(Console.ReadLine())) {
		case 7, 777:  Console.WriteLine("You lucky bastard!");
		case 5, 10:   Console.WriteLine("I have that many fingers too!");
		case 0, 1:    Console.WriteLine("What? Nobody picks that!");
		case 2, 3:    Console.WriteLine("Yeah, I guess you deal with those a lot.");
		case 12:      Console.WriteLine("I prefer a baker's dozen.");
		case 666, 13: Console.WriteLine("Isn't that bad luck though?");
		case 1..<10:  Console.WriteLine("Kind of boring, don't you think?");
		case 11, 13, 17, 19, 23, 29: Console.WriteLine("A prime choice.");
		case 10...99: Console.WriteLine("I have to admit... it has two digits.");
		case _...-1:  Console.WriteLine("Oh, don't be so negative.");
		default:      Console.WriteLine("What are you, high? Like that number?");
	}
}
~~~

This example illustrates several things.

**Single evaluation**: as you would expect, `match` makes a temporary variable so that `Console.ReadLine()` is only called once.

**Priority order**: Earlier cases are tested before lower cases, so `5` matches `case 5, 10` and not `case 1..<10`.

**Equality testing**: You can use not just literals like `case 5` but expressions like `case (x + y):`. In the output, `case 5` becomes `if (5.Equals(matchExpr))`. Why is this particular equality test used? Consider the alternatives:

- `if (matchExpr == 5)` works in fewer cases. Specifically, consider `match((object)5) {case 5:}`. `5.Equals((object)5)` returns true, but `(object)5 == 5` is a compile-time error. Thus `Equals` is more flexible.
- `if (object.Equals(matchExpr, 5))` would involve boxing and dynamic downcasting.
- `if (matchExpr.Equals(5))` would cause `NullReferenceException` in case `matchExpr` is null.

The form only changes if you write `case null`; this becomes `if (matchExpr == null)`. **Note**: other than the literal null, which is allowed, it is your responsibility to ensure that the cases themselves do not evaluate to null. For reasons of performance and epistemology, when you use non-literal expressions like `case X`, `match` does not check whether `X` itself is null (in fact, it cannot tell whether `X` has a nullable type).

**Default**: Like `switch`, `match` can have a `default:`, but it must come last. It's equivalent to `case _:`.

**Multiple patterns per `case`**: Enhanced C# allows `case` to have multiple _separate_ cases separated by commas, such as `1..10, 20, 30`. Unfortunately, when translating your code to plain C#, it is often impossible for two separate patterns to lead into the same _handler_, and therefore the output will duplicate the handler. For example, the first `case` above is translated as 

~~~csharp
if (7.Equals(tmp_0)) {
	Console.WriteLine("You lucky bastard!");
	break;
}
if (777.Equals(tmp_0)) {
	Console.WriteLine("You lucky bastard!");
	break;
}
~~~

In this particular example it _is_ possible to avoid duplicating the `Console.WriteLine` statement, but in most nontrivial patterns it is not (at least not without analysis capabilities LeMP doesn't have), so `match` doesn't even try. Therefore, if possible, avoid writing large code blocks inside a `case` with multiple patterns.

**Range operators**: Enhanced C# defines binary and unary operators named `..<` and `...`, as well as a binary `in` operator that is intended to test whether a value is contained in a collection.
	- `..<` is the exclusive range operator: it means you want the number on the right side to be excluded from the range. `case 1..<10` is translated to something like `if (tmp_0.IsInRangeExcl(1, 10))`. `IsInRangeExcl` should be an extension method, either one you define yourself or one of the extension methods in Loyc.Essentials.dll. This operator has two names, in fact: you can write `1..10`, as used in [Rust](https://www.rust-lang.org/), or `1..<10`, as used in [Swift](http://oleb.net/blog/2015/09/swift-ranges-and-intervals/). The lexer treats them as the same operator (named `..`).
	- `...` is the inclusive range operator: it means you want the number on the right side to be included in the range. For example, `case 10...99` is translated to something like `if (tmp_0.IsInRangeIncl(10, 99))`. (This operator's name is also three dots in Rust and Swift.)

You can use underscores to express an open-ended range, e.g. `case _..-1`. `..<` and `...` also exist as unary operators, so you can write `...-1` instead. However, there is no corresponding suffix operator: you must write `100..._`, not `100...`. All of these are translated to the appropriate binary operator (e.g. `_...9` becomes `matchExpr <= 9`).

### A fancy example ###

Here's a much more complicated example that shows most of the features of `match`:

~~~csharp
match (obj) {
	case is Shape(ShapeType.Circle, $size, Location: $p is Point<int>($x, $y) && x > y):
		Circle(size, x, y);
}
~~~

When I wrote this article I tried explaining what this does, but the explanation was so long I decided it would be better just to show the output code:

~~~csharp
if (obj is Shape) {
	Shape tmp_0 = (Shape) obj;
	if (ShapeType.Circle.Equals(tmp_0.Item1)) {
		var size = tmp_0.Item2;
		var tmp_1 = tmp_0.Location;
		if (tmp_1 is Point<int>) {
			Point<int> p = (Point<int>) tmp_1;
			var x = p.Item1;
			var y = p.Item2;
			if (x > y) {
				Circle(size, x, y);
				break;
			}
		}
	}
}
~~~

You can see several more features in action here:

**Unary and binary "is" operators**: `is Type` is a new operator added to Enhanced C# for the specific purpose of supporting pattern matching. It means "check if the `match_expression is Type` and if so, downcast to `Type` and make a temporary variable to hold the result". The binary version of `is` allows a few different things on the left-hand side:

- `$newVar is Type` creates a new variable `Type newVar` to hold the downcasted value
- `ref var is Type` sets an _existing_ variable called `var` to the downcasted value
- `low..hi is Type` holds the downcasted the value in a temporary variable, then checks if it's in the specified range
- `otherExpr is Type` (where `otherExpr` matches none of the other patterns above) holds the downcasted the value in a temporary variable, then checks if `otherExpr` is equal to it.

**Subpatterns in parentheses**: After the `is` part of the pattern, you can write "inner patterns" or "subpatterns" in parentheses. For example, in `case A(B(C), D)`, `A` has subpatterns `B(C)` and `D`, and `D` is a subpattern of `B`.  Each subpattern is treated the same way as the outermost pattern, except that subpatterns can specify a property name (e.g. `Location:`) and the outer pattern cannot.

**Positional and named properties**: In this example, the first two components of `Shape` are treated as "positional" properties while the third component is a "named" property (its name is `Location`). A named property consists of an identifier followed by colon (`:`) such as `Location:`. If you don't provide a name, `match` uses a numbered property instead (`Item1`, `Item2`, etc.). So in this example the `Shape.Item1` property is matched against the subpattern `ShapeType.Circle`, and `Shape.Item2` is matched against `$size`.

Please note that you can only name simple properties, not nested properties, methods or indexer properties. For example, you might be tempted to write `case is Foo(Bar(): 777)` to find out if the `Foo.Bar()` method returns `777`, but this is not allowed because it is a syntax error. However, you can write `case $foo is Foo && foo.Bar() == 777` instead.

**Variable binding**: Use the `$` operator to create a new variable and assign it to the value of part of an object. In this case the new `size` variable is assigned to the `Shape.Item2` property of `obj`, the new `p` variable is assigned to `Shape.Location`, and so forth.

**Extra conditions**: You can use the `&&` operator on the main pattern or subpatterns to add extra conditions to a pattern, e.g. given

    case is Size(Width: $w, Height: $h && h > 100) && w > h:
		DoSomethingWith(w, h);

The output is something like

	if (obj is Size) {
		Size tmp_2 = (Size) obj;
		var w = tmp_2.Width;
		var h = tmp_2.Height;
		if (h > 100 && w > h) {
			DoSomethingWith(w, h);
			break;
		}
	}

**Rough left-to-right evaluation**: Patterns are evaluated roughly left-to-right, except that if you're using a binary `is` condition such as `$x is Type`, the type test on the right-hand side (obviously) runs before the test or binding on the left-hand side.

### Cases using the "in" operator ###

Earlier you saw that you could write `case lo..hi` to find out if a value is within a range. If you want to combine a range test with a variable binding, an equality test, or subpattern matching, you can use the `in` operator. Here are some examples:

~~~csharp
match(value) {
	// Is value a double between 0 and 1 ?
	case $newVar is double in 0.0...1.0:
		ZeroToOne(newVar);
	// This one is tricky! It requires that `coefficient.Equals(value) && value in 0...1`
	case coefficient in 0.0...1.0:
		ZeroToOne(newVar);
	// Due to the precedence rules of EC#, if you combine `in` with 
	// subpatterns, the subpatterns must come before `in`.
	case _ is Point(X: $x, Y: $y) in polygon:
		CollisionDetected(x, y);
	// However, when you add conditions with `&&`, they still come last.
	case is Size(Width: $w, Height: $h) in acceptableSizes && w > h:
		SizeIsOK(w, h);
}
~~~

### Assigning to an existing variable with `ref` ###

You can use `ref variable` instead of `$variable` to assign a value to an existing variable rather than creating a new variable. For consistency with the `matchCode` macro introduced in the [previous article](lemp-code-gen-and-analysis.html), `$(ref variable)` is also accepted.

### Rationales ###

That reminds me: why do you think the syntax for creating a new variable in `case` is `$x` instead of, say, `var x`? Partially it's because `var x` would, in general, not be permitted by the parser, but it's also because the `matchCode` macro also uses the `$x` syntax, and `var x` usually isn't even _possible_ in the context of `matchCode`.

It's also fair to ask why you have to write `case $x is Foo` instead of simply `case x is Foo`. In fact, initially I did support the latter syntax (because Rust works similarly), but soon afterward I decided to drop support. There are three reasons:

  1. `$x` is easier to spot, so people reading the code can more easily see the point where `x` is created.
  2. `$x` is consistent with the syntax of `matchCode`.
  3. If you write `777 is Foo` or `Foo.Bar is Foo`, the left-hand side is interpreted as an equality test. Thus `x is Foo` would have been a special case, and it would arguably be surprising if `x is Foo` and `x.y is Foo` did fundamentally different things.

### Standalone ranges and `in` operator ###

The `..<`, `...`, and `in` operators are not limited to `match`. You can use them in ordinary expressions, like this:

~~~csharp
	if (!(index in 0..list.Count))
		throw new ArgumentOutOfRangeException("index");
~~~

As before, the `x in lo..<hi` pattern translates to `x.IsInRangeExcl(lo, hi)` while the `x in lo...hi` pattern translates to `x.IsInRangeIncl(lo, hi)`. You can also use `in` by itself, or use the range operators by themselves:

~~~csharp
	var range = 0..list.Count;
	if (!(index in range))
		throw new ArgumentOutOfRangeException("index");
~~~

This is translated to

~~~csharp
	var range = Range.ExcludeHi(0, list.Count);
	if (!range.Contains(index))
		throw new ArgumentOutOfRangeException("index");
~~~

Loyc.Essentials.dll contains all of the methods shown here; `IsInRangeExcl` is an extension method in class `Loyc.In`, while the `Range` is currently placed in the `Loyc.Collections` namespace and returns a variable of type `NumRange<Num,Math>` where `Num` is a numeric type such as `int`, and `Math` is a helper type that allows `NumRange` to perform arithmetic on that numeric type (it is needed since .NET does not define math interfaces for built-in types.) It is worth noting that `NumRange` implements `IReadOnlyList<Num>`, so you can use it in `foreach` loops and LINQ expressions.

Since the expression `x in range` just calls `range.Contains(x)`, it also works with standard collection types.

### Wrapping up ###

I think that's everything. I hope these features make you more productive. Enjoy!

To learn more or download LeMP, visit the [LeMP home page](/lemp).
