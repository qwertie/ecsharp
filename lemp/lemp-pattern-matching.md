---
title: C# Gets Pattern Matching & Algebraic Data Types
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

I bet you've written code like this. Some of you write this style of code a lot. But now LeMP has a shortcut for patterns like these: it's called `match`. It's like `switch`, but for pattern-matching. With `match`, the code above becomes simply

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

Pattern Matching With Tuples
----------------------------

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

More Pattern Matching
---------------------

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

Several features are shown here:

- Enhanced C# allows `case` to have multiple _separate_ cases separated by commas, such as `1..10, 20, 30`. Unfortunately, when translating your code to plain C#, it is often impossible for two separate patterns to lead into the same _handler_, and therefore the output will duplicate the handler. For example, the first `case` above is translated as 

		if (7.Equals(tmp_0)) {
			Console.WriteLine("You lucky bastard!");
			break;
		}
		if (777.Equals(tmp_0)) {
			Console.WriteLine("You lucky bastard!");
			break;
		}
	
	In this particular example it _is_ possible to avoid duplicating the `Console.WriteLine` statement, but in most nontrivial patterns it is not possible, so `match` doesn't even try. Therefore, if possible, avoid writing large code blocks inside a `case` with multiple patterns.

- Enhanced C# defines binary and unary operators named `..<` and `...`.
	- `..<` is the exclusive range operator: it means you want the number on the right side to be excluded from the range. For example, `case 1..<10` is translated to something like `if (tmp_0 >= 1 && tmp_0 < 10)`. This operator has two names, in fact: you can write `1..10`, as used in [Rust](https://www.rust-lang.org/), or `1..<10`, as used in [Swift](http://oleb.net/blog/2015/09/swift-ranges-and-intervals/). The lexer treats them as the same operator.
	- `...` is the inclusive range operator: it means you want the number on the right side to be included in the range. For example, `case 10...99` is translated to something like `if (tmp_0 >= 10 && tmp_0 <= 99)`. (This operator's name is also three dots in Rust and Swift.)

To express an open-ended range, you can use underscores, e.g. `case _...-1` translates to `if (tmp_0 <= -1)`. Please note that `..<` and `...` also exist as unary operators, so you can write `...-1` instead. However, there is no corresponding suffix operator: you must write `100..._`, not `100...`.



It is recommended to use dollar signs ($) to mark new variables. However, 

UNFINISHED