////////////////////////////////////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
// introduction to Enhanced C# /////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// This is a pep talk for myself. EC# does not yet exist, but I am writing this
// document as though it does.
//
// EC# is an enhanced version of C# that introduces several new features to
// make "power engineers" more productive.
//
// EC# is designed to feel like a straightforward extension of the C# language,
// that does not fundamentally change the "flavor" of the language. It is also
// intended to be backward compatible with plain C#. It tries not to break any 
// code that already works, but there is one known exception involving the new 
// "alias" directive.
// 
// Currently, there is no "proper" compiler for EC#. Instead, EC# code (*.ecs)
// compiles down to plain C# (*.cs) which is then fed to the standard compiler.
// Someday I hope to have a complete compiler from EC# to CIL.
//
// Ultimately, EC# is intended to be vastly more powerful than standard C#, but
// the new features in EC# 1.0 are comparatively minor. The long-term road map
// includes powerful metaprogramming, extensibility, compile-time code execution,
// and unit inference. The most powerful features in EC# 1.0 are compile-time 
// templates, compile-time function evaluation, and the "static if" statements, 
// which, together, allow you to generate multiple versions of a function or 
// class to use at runtime. There are some useful but less powerful features in 
// EC# 1.0, too, such as more powerful variable declarations, the "null dot" 
// operator (?.), "multiple-source name lookup"

LINQ-to-template???

list.Specialized().Where(x => x < 0).Select(x => x.ToString())

////////////////////////////////////////////////////////////////////////////////
//                                //////////////////////////////////////////////
// powerful variable declarations //////////////////////////////////////////////
//                                //////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// In plain C#, you can only declare a single variable in a "var" statement.
// EC# conservatively extends the "var" statement so that you can declare multiple
// variables. However, the initializer expressions must have exactly the same type.
var aloha = "hello", farewell = "goodbye";       // OK
var question = "6*7", answer = 42;               // ERROR: types are not identical
var b = new BaseClass(), d = new DerivedClass(); // ERROR: types are not identical

// In plain C#, the following statement does not compile, because the compiler does 
// not know whether you want to create a delegate or an expression tree:
var square = (double x) => x*x;
// EC#, however, accepts the above statement by assuming you want to create a 
// delegate of type System.Func<double, double>. Please note that for this to work,
// you must supply the data types of the parameters. Also, "ref" and "out" 
// parameters are not supported since they cannot be represented with System.Func.

// EC# allows variable declarations as the target of "out" parameters, like so:
int OutVar() {
	string input = "five";
	if (int.TryParse(input, out int output)) {
		Console.WriteLine("Yeah, {0} is an integer all right.", output);
		return output;
	}
}

// Please note that the variable "output" is no longer accessible beyond the end 
// of the "if" block. This decision was made for consistency with variable
// declarations in the existing "for" loop, which behave similarly:
for (int x = 0; x < 100; x++) {}
return x; // ERROR
// The implementation of this rule involves putting the "if" statement itself in
// an anonymous block.

// Ideally, EC# would allow ordinary variable declarations inside arbitrary 
// expressions, but we must consider how this impacts the difficulty of parsing 
// the language.
//
// The main difficulty is generic types, which use the "<" and ">" characters
// that may also be overloaded comparison operators. This difficulty does not 
// exist for ordinary variable declarations, because a C# statement is 
// simply not allowed to start with a comparison. For example:
int a = 1, b = 2, c;
a < b ? c = a : a = b; // ERROR: Only assignment, call, increment, decrement, 
                       // and new object expressions can be used as a statement

// Now consider the three statements in "LooksAmbiguous":
class MyClass
{
	public static MyClass operator <(MyClass a, MyClass b) { return a; }
	public static MyClass operator >(MyClass a, MyClass b) { return a; }
	
	static MyClass List = new MyClass(), Int32 = new MyClass(), result1 = new MyClass();
	
	static void LooksAmbiguous()
	{
		Trace.WriteLine((List<Int32> result1).GetType());       // OK
		AcceptsDelegate((List<Int32> result2) => result2);      // OK
		Trace.WriteLine((List<Int32> result3) = new MyClass()); // ??!?!
	}
	static void AcceptsDelegate<T>(Action<T> a) {}
}
// It may impress you a little to learn that modern C# is perfectly capable of
// parsing and understanding the first two statements, even though they look 
// so similar at first and have very different meanings. The compiler understands 
// that "List<Int32> result1" is an expression with two comparisons, while 
// "List<Int32> result2" is an argument to a lambda expression. Now, all modern
// languages have learned to avoid the complex and ambiguous grammar of C++, in
// which different phases of the compiler must talk to each other in order to
// make sense of the code. C++'s complicated rules are a very bad idea since 
// it makes the task of interpreting the language far more difficult. Therefore, 
// a C# compiler certainly does not decide that "List<Int32> result1" is an 
// expression based on the fact that variables exist called "List", "Int32" and 
// "result1". Now, I have never looked at a C# grammar or parser, nor have I read
// the language standard carefully. I can still say with confidence, though, that
// when the compiler decides "List<Int32> result1" is an expression and not an
// argument, it does so on the basis that there is no "=>" following the closing
// parenthesis. That is, the decision is purely local: it doesn't depend on code
// earlier in the file or code in other source files.
//
// We must draw a line somewhere to ensure that the rules for interpreting the
// language do not get too complex. Certainly, by making using parenthesis to
// separate the declaration from its initial value, the assignment on the third 
// line above (result3) is very "clever" and it would be entirely reasonable for 
// EC# to reject it, since the parenthesis are unnecessary.
//
// I carefully considered  the parsing difficulties raised by allowing variable
// declarations inside expressions, including the case above, and I decided that 
// allowing such declarations is indeed practical as an extension of C# as it 
// exists today. However, it does require extra work for the parser, which must 
// perform a (possibly expensive) lookahead operation every time it sees "A < B" 
// at the beginning of a subexpression, and the difficulty may increase as EC# 
// is extended further in the future.
// 
// Therefore, EC# does allow variable declarations inside expressions, but to
// keep the parsing rules simple, they must start with "var". You are allowed
// to specify a data type, but this must be provided in addition to "var". Thus,
// the third line above must be changed as follows:
Trace.WriteLine((var List<Int32> result3) = new MyClass()); // OK

// The data type can be omitted provided that the data type is given in the
// same subexpression, or if the variable is declared as part of a tuple that
// is immediately assigned:
Trace.WriteLine((var result3) = new MyClass()); // ERROR: result3 must be immediately assigned ('var result3 =') when no data type is provided.
Trace.WriteLine(var result3 = new MyClass()); // OK
Trace.WriteLine((var result4, var result5) = (4, 5)); // also OK

// When the data type is simple (non-generic), EC# will understand that you wanted 
// a variable declaration and remind you to use 'var':
Trace.WriteLine(MyClass result3 = new MyClass()); // SYNTAX ERROR. Remember that 'var' is required for inline variable declarations ('var MyClass result3')

// For consistency, the 'var type name' syntax is also allowed for standalone 
// variable declarations:
var int x, y; // OK, declare x and y to be integers

// However, variable declarations inside expressions can only declare a single 
// variable at a time. For example, the following WriteLine statement is called 
// with two arguments, and the second one is not being declared; b must exist 
// already:
string b;
Console.WriteLine(var a = "1", b = "2");
// Similarly, the following code prints out a tuple. The variable c is being 
// declared and d must already exist.
int d;
Console.WriteLine("{0}", (var int c = 5, d = 10));
// Since 'var' is required, variable declarations in expressions are never 
// ambiguous, and the parser can handle a variable declaration in the middle of a
// subexpression:
if (var open = Port.Is0pen && var ready = Port.IsReady) {} // OK
// Plain C# allows symbols to be named var. This is still permitted; the parser
// only treats "var" specially when it is followed by another word:
String var = "Still allowed";
if ((var String = "No prob.") != null) {}



/*
// Here are the rules. In order to declare a new variable inside an expression, 
// EC# requires four things:
//
// 1. It must look like a variable declaration (A<...> B)
// 2. It must declare only a single variable
// 3. It must be located at the beginning of an expression, after "(", "," or ";"
// 4. The apparent variable declaration must be followed by "," or "=". If it 
//    is not followed by "=" then the expression that encloses it must be followed
//    by "=".
// 
// The result3 assignment meets requirements (1) to (3) but not (4). In fact, the 
// fourth rule is very generous--if there is a comma after the apparent variable 
// declaration, the compiler must find the end of the expression and then look 
// for an equals sign. The reason for this complication is tuples, so that you
// can declare a variable in a tuple as in (int one, int two) = (1, 2); 
// Rule #4 could easily be extended to support the result3 third line above, but I 
// decided not to support it, for now, because the tuple feature should be 
// considered a separate feature from this "inline variable declaration" feature.
// So just because I made a complicated rule for tuples, doesn't necessarily mean
// the rule should handle non-tuples too. After all, if the plain C# team 
// considered adding support for inline variable declarations, they might not 
// include tuples at the same time.
//
// The correct version way to declare result3, which meets all requirements, is
//    Trace.WriteLine(List<Int32> result3 = new MyClass());
//

// Here are some examples to illustrate the rules:
int b = (int a = 5) + 5;             // OK (very weird, but OK)
(var t = Database.Table).RunQuery();  // OK
Console.WriteLine(string a = "1", b = "2"); // OK, WriteLine() called with two arguments.
                                      // b must exist already, it is not being declared!
Console.WriteLine("{0}", (int a = 5, b = 10)); // OK, but this actually prints a tuple.
                                      // b must exist already, it is not being declared!
if (bool open = Port.Is0pen && bool ready = Port.IsReady) {} // SYNTAX ERROR.
                                      // Use (bool ready = Port.IsReady) instead.
(int[] ints, IEnumerable<int> more) = (new[] { 1, 2 }, new[] { 3, 4 }); // OK (tuple) 
*/


// If you feel that (var String name = "John") is kind of a clunky way to declare a 
// variable, well, I agree. It's clunky in order to make it very clear what '<' means 
// in case you want to use a generic data type (we don't want a different syntax for 
// generic and non-generic data types).
// 
// EC# also provides a new non-clunky syntax for declaring write-once variables 
// inside expressions: the colon operator (:). This operator is designed to be 
// concise and convenient to use:
if (DB.TryToRunQuery():r != null)
	Console.WriteLine("Found {0} results", r.Count);
	
// The above code runs a database query, saves the results in "r", and checks 
// whether "r" is null all in one line. 
//
// When variables declared with this operator are declared inside the parenthesis 
// of if(), while(), for(), foreach(), switch(), or using(), they live on beyond 
// the block of that statement. This behavior is different from the "old-style"
// variable declarations defined above, which limited the scope of such variables.
// For example:
if (Database.RunQuery():r != null) {
	Console.WriteLine("Found {0} results", r.Count:c);
}
int ERROR = c; // ERROR, c is not declared in this scope
return r;      // OK, r still exists

// As you can see, this operator can often be used without parenthesis, unlike
// conventional declarations, which almost always require extra parenthesis.
// The order of arguments to ":" actually helps avoid parenthesis. For example, 
// consider this code:
Point? ParsePoint(string s) // parses a string such as "4,-5"
{
	if (s.Split(','):parts.Count != 2)
		return null;
	if (int.TryParse(parts[0], out int x) &&
		int.TryParse(parts[1], out int y))
		return new Point(x, y);
	else
		return null;
}
// If ":" had been a prefix operator, as in "(parts:s.Split(',')).Count", then we 
// would need an extra parenthesis to indicate that "parts" refers to "s.Split(',')"
// and not "s.Split(',').Count" or simply "s" itself. Moreover, writing code this
// way is more natural because you might write "s.Split(',')" first and only then
// realize that you need to store the result for later.
//
// The precedence of the colon operator is just below that of the "unary" operators 
// on the C# precedence table (that is, below operators like "!", "~", and casts.
//
// Therefore, an expression like 
//    (IEnumerable<T>)DB.Tables:list
// means
//    ((IEnumerable<T>)DB.Tables):list
// and not
//    (IEnumerable<T>)(DB.Tables:list)
//
// This fact may not be obvious from reading the code, so EC# issues a warning
// for the first expression: "Note: operator '(IEnumerable<T>)' has higher precedence
// than operator ':'. To eliminate this message, add a space before ':list'."
//
// The warning disappears when you change the expression to
//    (IEnumerable<T>)DB.Tables :list
// because this gives a visual hint that the cast happens first and the binding 
// happens afterward. To use the colon operator with "is" or "as", you must place
// the "is" or "as" expression in parenthesis:
//    (DB.Tables as IEnumerable<T>):list
//
// Note that the colon operator creates a copy of a value, not an alias. So in the
// code above, s.Split(',') is called only once.
//
// Colon-bound "variables" are read-only. In fact, I would like them to behave 
// identically to fields marked 'readonly' in C#, but this is not practical because
// EC# is compiled to plain C#, which does not allow local variables to be declared
// readonly. Therefore, while EC# does not allow you to use the '=' operator on 
// colon-bound variables, to change properties on them, or to use them as a "ref" or
// "out" parameter, it currently does not protect against side effects from method 
// calls. For example:
struct Oops {
	public int X;
	public void Inc() { ++X; }
}
void ShouldWorkDifferently() {
	var a = new Oops():b;
	a.Inc(); // Legal, ++a.X == 1
	//++b.X  // You can't do this, it would cause a compiler error from EC#
	b.Inc(); // Should not mutate b, but actually does due to a limitation of C#.
	b.Inc(); // ++b.X twice yields 2.
	Console.WriteLine("{0} {1}", a.X, b.X); // prints "1 2"
}

// If you want to use the colon operator with a simple pair of identifiers at 
// the very beginning of a statement, the value (not including the colon) must 
// be enclosed in parenthesis so that the parser does not confuse it with a 
// label (i.e. the target of a goto statement). You will be given a warning if 
// it appears you forgot this rule.
int answer = 42;
(answer):a.GetType():t; // OK, declare variables 'a' and 't'
answer:b.GetType(); // NOTE: 'answer' is a label, not an expression. If that 
          // was your intention, add a space after the colon to eliminate this 
          // message. To create a variable named 'b', use '(answer):b' instead.

// The colon operator creates a potential ambiguity with the existing conditional
// operator (c ? x : y). To resolve this problem and avoid confusion, the colon 
// operator cannot be used after a question mark, in the same subexpression.
var hmm = c ? x : y : z;   // ERROR: ':' cannot appear more than once after '?'.
var ok1 = c ? x : (y : z); // OK
var ok2 = c ? (x : y) : z; // OK
var ok3 = (c ? x : y) : z; // OK

////////////////////////////////////////////////////////////////////////////////
//              ////////////////////////////////////////////////////////////////
// constructors ////////////////////////////////////////////////////////////////
//              ////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// For convenience, EC# allows constructors named "init", to save typing. 
// EC# constructors, whether named "init" or not, have the special ability 
// to copy constructor arguments to member variables or properties 
// automatically, as well as to create new member variables. Here are a 
// couple of examples.
struct PointD
{
	double _x, _y;
	public init(_x, _y) { }
	// Equivalent to public PointD(double x, double y) { _x = x; _y = y; }.
	// If a leading underscore is present in the names being initialized, it is 
	// stripped so that the constructor can be called without an underscore in 
	// named parameters, e.g.
	//    var p = new PointD(x: 1, y: 2);
}

partial struct Fraction
{
	// Specifying an accessibility such as "public" causes a new field to be 
	// created, if it does not already exist.
	public init(public int Numerator, public int Denominator) { }
	public init(public int Numerator) { Denominator = 1; }
	
	// Constructor parameters do not have to initialize anything implicitly.
	// This constructor, for example, does NOT create a new field.
	public init(string frac) {
		var parts = frac.Split('/');
		Numerator = parts[0];
		Denominator = parts[1];
	}
}

// Standard constructors don't encapsulate properly: they restrict the way that 
// a class is implemented and prevent future changes to the implementation, as 
// explained in my blog post "Constructors Considered Harmful": 
//    http://loyc-etc.blogspot.ca/2012/07/constructors-considered-harmful.html
// EC# has a couple of mechanisms to mitigate these problems:
//
// 1. When you declare a constructor in a public or protected class in EC#, the 
//    compiler implicitly creates a static method function called "new" (called 
//    @new in plain C#) which forwards to the real constructor. This behavior 
//    can be turned off with [assembly:ConstructorWrapper(false)] or by applying
//    [ConstructorWrapper(false)] to a specific class or a specific constructor.
// 2. A constructor call such as "new Foo(...)" is considered equivalent to 
//    "Foo.new(...)" within EC# code, except when both a constructor and a 
//    static "new" method are defined and accessible. Regardless of which syntax 
//    you use, the EC# compiler may directly call the constructor, or it may 
//    invoke a static method called "new", depending on the situation. Generally,
//    if Foo is located in the same assembly then the constructor is called 
//    directly. If Foo is in a different assembly, the static method is called 
//    if it exists. This allows the other assembly to change its constructor into 
//    a static method without breaking compatibility.
//
// For example:
class Foo {
	public Foo(int n) {}
	public static Foo new(string s) { return new Bar(int.Parse(s)); }
}
class Bar : Foo {
   public Bar(int n) : base(n) {}
}
Foo foo1 = new Foo(7);   // OK
Foo foo2 = Foo.new(7);   // OK, Equivalent
Foo foo3 = new Foo("7"); // OK
Foo foo4 = Foo.new("7"); // OK, Equivalent

// Unfortunately, the above feature is not fully compatible with .NET generics, 
// due to limitations of the CLR. Specifically, generic code that uses the new()
// constraint cannot call the static method "new()":
class FakeNew {
	private FakeNew() {}
	public static FakeNew new() { return new FakeNew(); }
}
FakeNew new1 = new FakeNew(); // OK

T Create<T>() where T:new()  { return new T(); }
FakeNew new2 = Create<FakeNew>(); // ERROR
// "new() method is not compatible with the .NET Generic constraint new()"


// EC# allows you to define a "default constructor" for structs. Because of this,
// default(S) is not necessarily the same as new S() when S is a struct. This 
// feature is converted to plain C# by calling a static new() method. Caution: 
// remember that .NET itself does not allow a default constructor for structs.
// Therefore, only EC# code, not plain C# code, can call it with "new S()". To
// avoid confusion, you may prefer to use S.new() or S.@new() (the latter works
// in plain C#.)
struct Path
{
	public string Value;
	public Path() { Value = @"\"; }
	// Equivalent to: public static Path new() { return new Path { Value = @"\" }; }
}
void NewVersusDefault()
{
	Debug.Assert(new Path().Value == @"\");
	Debug.Assert(default(Path).Value == null);
}

// Note: again, the above feature is not compatible with generics:
Path new3 = Create<Path>(); // ERROR
// "A default constructor in a struct is not compatible with the .NET generic constraint new()"

/* This idea needs more work to be practical.
// In EC#, static methods can be part of an interface, and since each constructor
// comes with its own static method, constructors can be used to define their own 
// factory interface, if the factory interface uses "new" as the method name. 
// For example:
interface IEnumerableFactory
{
	IEnumerable @new();
}
class Foo : IEnumerable, static IEnumerableFactory
{
	public Foo() { ... }
	// The above constructor implies creation of a static "new" method:
	// public static Foo @new(int count) { return new Foo(count); }
	// Consequently, Foo implements IEnumerableFactory.
	
	public IEnumerable GetEnumerator() { ... }
}
*/

////////////////////////////////////////////////////////////////////////////////
//                         /////////////////////////////////////////////////////
// return value covariance ////////////////////////////////////////////////////
//                         /////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// Unlike C#, EC# allows return value covariance. Here, Clone() is allowed to 
// implement ICloneable.Clone() even though its declared return type is "object".
// Cloner2.Clone() can safely override Cloner1.Clone(), since a Cloner2 is a
// Cloner1.
class ClonerA : ICloneable {
	public virtual ClonerA Clone() { return (ClonerA)MemberwiseClone(); }
};
class ClonerB : Cloner1 {
	public override ClonerB Clone() { return (ClonerB)MemberwiseClone(); }
};

// CLR engineers have blinders on regarding this issue. Since the .NET framework 
// itself does not allow return value covariance, we need a trick to work around 
// the problem. EC# provides two workarounds, depending on whether covariance is
// needed with a class or with an interface.

// Case 1: Covariance with an interface (for structs, this is the only case):
class ClonerA : ICloneable {
	public virtual ClonerA Clone() { return (ClonerA)MemberwiseClone(); }
	
	// This case is very straightforward, EC# just adds an explicit interface 
	// implementation if one is not already present:
	object ICloneable.Clone() { return Clone(); }
}

// Case 2: Covariance with a base class.
class ClonerB : ClonerA {
	public override ClonerB Clone() { return (ClonerB)MemberwiseClone(); }
	
	// In this case the implementation MUST return ClonerA as the .NET requires.
	// So EC# creates a second method called cov_Clone() that returns a ClonerB.
	// The second method is virtual by default, in case a derived class wants to
	// override it (the method becomes nonvirtual if you mark Clone() as sealed).
	public override ClonerA Clone() { return cov_Clone(); }
	public virtual ClonerB cov_Clone() { return (ClonerB)MemberwiseClone(); }
	
	void CallSite() {
		ClonerB b = Clone();
		// Rewritten as: ClonerB b = cov_Clone();
	}
};

// Case 3: Covariance with a base class that already has a cov_* method:
class ClonerC : ClonerB {
	public override ClonerC Clone() { return (ClonerC)MemberwiseClone(); }

	// In this case EC# is forced to create a third method, which it calls
	// cov1_Clone(). Similarly, if someone overrides ClonerC.Clone() then a 
	// third forwarding function (cov2_Clone) will be needed.
	public override ClonerA Clone() { return cov1_Clone(); }
	public override ClonerB cov_Clone() { return cov1_Clone(); }
	public virtual ClonerC cov1_Clone() { return (ClonerC)MemberwiseClone(); }
}

// Case 4: Overriding a method noncovariantly that already has cov_* helper
class NonCovariant : ClonerB {
	public override ClonerB Clone() { return new NonCovariant(); }
	
	// In this case, EC# simply has to change the above to:
	public override ClonerB cov_Clone() { return new NonCovariant(); }
}

// You may wonder why EC# does not simply insert a cast at the call site. That would
// work, but the cast wastes CPU time. Of course, forwarding the call to a cov_* 
// method is also unnecessary work, but the extra work is only required if another 
// class overrides the same method covariantly (case 3). In the other 3 cases, time
// is saved by avoiding the unnecessary cast.
//
// Although EC# supports return value covariance, it does not support covariance on 
// "out" parameters or contravariance on input parameters, again because the .NET 
// Framework lacks such support and I don't have time to develop another workaround.

// One more thing. EC# allows covariance between "void" and any other type:
class VoidFoo { virtual void Foo() {} }
class LongFoo { override int Foo() { return 123456789; } }
// This is implemented as:
class LongFoo {
	override void Foo() { cov_Foo(); }
	virtual int cov_Foo() { return 9876543210; }
}

////////////////////////////////////////////////////////////////////////////////
//                            //////////////////////////////////////////////////
// getter/setter independence //////////////////////////////////////////////////
//                            //////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// In plain C# I have often run into problems caused by C#'s strange belief that a 
// property is a single indivisible unit instead of a pair of methods. For example,
// consider the following pair of interfaces. The first defines a read-only point 
// and the second adds mutability:
public interface IPointReader<T>
{
	T X { get; }
	T Y { get; }
}
public interface IPoint<T> : IPointReader<T>
{
	T X { set; } // WARNING: 'IPoint<T>.X' hides inherited member 'IPointReader<T>.X'.
	T Y { set; } // WARNING: 'IPoint<T>.Y' hides inherited member 'IPointReader<T>.Y'.
}
double XTimesY(IPoint<double> p)
{
	return p.X * p.Y; // ERROR: 'IPoint<double>.X' cannot be used in this context because it lacks the get accessor
}

// C# doesn't understand the concept of "adding" a setter because its philosophy is
// that a property is a single thing that must be replaced entirely or not at all,
// instead of a pair of independent methods. In this case, there is a 
// straightforward workaround: you must define a duplicate getter in IPoint<T>:
public interface IPoint<T> : IPointReader<T>
{
	new T X { get; set; }
	new T Y { get; set; }
}
// As far as the CLR is concerned, the two getters IPointReader<T>.X and IPoint<T>.X
// are completely unrelated; luckily this is not normally a problem because you can
// implicitly define both of them:
public struct PointD : IPoint<double>
{
	public double X { get; set; } // used for both IPointReader.X and IPoint.X
	public double Y { get; set; } // used for both IPointReader.Y and IPoint.Y
}
// However, if you want to use an explicit interface implementation then you must
// supply separate getters for IPoint and IPointReader:
public struct PointD : IPoint<double>
{
	public double X, Y;
	double IPoint<double>.X { get { return X; } set { X = value; } }
	double IPoint<double>.Y { get { return Y; } set { Y = value; } }
	double IPointReader<double>.X { get { return X; } }
	double IPointReader<double>.Y { get { return Y; } }
}

// EC# solves this problem (and several others) by redefining property getters and 
// setters as independent entities. Therefore, the original definition of IPoint<T>
// produces no warnings and XTimesY() does not produce an error:
public interface IPoint<T> : IPointReader<T>
{
	T X { set; } // OK
	T Y { set; } // OK
}
double XTimesY(IPoint<double> p)
{
	return p.X * p.Y; // OK
}
// In order to convert this to plain C# that works, EC# uses a different workaround:
// rather than insert an extra getter, the EC# compiler changes the way IPoint is
// used, like so:
double XTimesY(IPoint<double> p)
{
	return (p as IPointReader<double>).X * (p as IPointReader<double>).Y; // OK
}
// Note that this particular problem occurs only when using the interface IPoint,
// not when using an implementation such as PointD.

// The problem and solution is the same in the case of base and derived classes:
class Person {
	protected string _name;
	public virtual string Name { get { return _name; } }
}
class PersonEditor {
	public virtual string Name { set { _name = value; } }
	// WARNING: 'PersonEditor.Name' hides inherited member 'Person.Name'
}
void AnnoyingError()
{
	var p = new PersonEditor();
	p.Name = "Steve";            // OK
	Console.WriteLine(p.Name);   // ERROR: 'Name' cannot be used in this context because it lacks the get accessor
}

// Now, plain C# does allow the getter and setter to have different visibilities, 
// provided that one (the annotated one) is more restricted than the other:
class Person
{
	protected string _name;
	public virtual string Name 
	{ 
		get { return _name; } 
		protected set { _name = value; } // OK 
	}
}
class PersonEditor {
	public override string Name { 
		set { 
			if (value.IndexOfAny(new[] { '$', ' ', '?' }) > -1)
				throw new ArgumentException("Invalid character in new Name value");
			_name = value;
		} 
	}
	// WARNING: 'PersonEditor.Name' hides inherited member 'Person.Name'
}

// Normally that's fine, except in the very rare case that you want a "protected" 
// getter and an "internal" setter, which C# makes illegal. EC# allows this using a 
// simple workaround on conversion to C#: the protected accessor is changed to 
// "protected internal" to make it legal.

// A far more annoying problem appears if you want a a nonvirtual getter and a 
// virtual/abstract setter. I often want to do this so that the getter can be 
// inlined, and the derived class can to validate the new value and/or take some 
// action when it changes:
class Person
{
	protected string _name;
	public string Name 
	{ 
		get { return _name; }
		abstract set; // ERROR: The modified 'abstract' is not valid for this item
	}
}
class PersonEditor : Person
{
	public string Name 
	{ 
		override set { // ERROR: The modified 'override' is not valid for this item
			if (value.IndexOfAny(new[] { '$', ' ', '?' }) > -1)
				throw new ArgumentException("Invalid character in new Name value");
			_name = value;
		}
	}
}

// The workaround in plain C# is to create a special "set" method and to forward
// the real setter to this method:
class Person
{
	protected string _name;
	public string Name 
	{ 
		get { return _name; }
		set { Name_set(value); }
	}
	protected abstract void Name_set(string value);
}

// Normally, this is exactly the workaround that EC# employs during conversion 
// to plain C#. However, the problem becomes more serious if you want to override 
// a getter AND add a setter:
class Person
{
	string _name;
	virtual public string Name { get { return _name; } }
}
class PersonEditor : Person
{
	public override string Name
	{
		get { Console.WriteLine("overridden!"); return _name; }
		virtual set { _name = value; } // ERROR: The modifier 'virtual' is not valid for this item
	}
	public PersonEditor() { Name = ""; }
}

// And yes, C# gives an error whether there is a 'virtual' keyword or not. EC#, 
// of course, accepts this code without complaint because getters and setters are
// considered independent. To convert it to normal C#, EC# cannot use forwarding
// since adding a setter is impossible. Instead, the alias Name_set is treated as
// being the property. So the above code is converted to plain C# as:
class PersonEditor : Person
{
	public override string Name
	{
		get { Console.WriteLine("overridden!"); return _name; }
	}
	public virtual Name_set(string value) { _name = value; }
	public PersonEditor() { Name_set(""); }
}

// This works across DLL boundaries, too, because EC# recognizes the _set suffix 
// as an alternative way to define a property. Since the PersonEditor has no setter
// for Name, EC# assumes Name_set represents the missing setter as long as its 
// signature is appropriate. Currently, EC# only requires one relationship between 
// getters and setters: they must use the same data type. Even this rule may seem
// overly restrictive in some cases, because it may make sense to use a covariant
// return type when overriding a property getter (but not the setter). Therefore,
// this rule may be relaxed in a future version, but for now you'll have to tough
// it out.

////////////////////////////////////////////////////////////////////////////////
//                                                  ////////////////////////////
// string interpolation and double-verbatim strings ////////////////////////////
//                                                  ////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# introduces a new type of string, the "interpolated string", which enables a
// feature commonly known as "string interpolation" in several other programming
// languages. It looks like this:
int twelve = 3 * 4;
MessageBox.Show("3 times 4 equals \{twelve}.");
// The syntax \{expr} was chosen because it avoids any conflict with plain C#,
// since "\{" is illegal there. By default, this example is equivalent to
MessageBox.Show(String.Format("3 times 4 equals {0}.", twelve));
// An interpolated string can have a format field:
MessageBox.Show("Two pies are about \{3.14159*2:0.00}.");
// This translates to
MessageBox.Show(String.Format("Two pies are about {0:0.00}.", 3.14159*2));
// Note that if a colon needs to be part of the expression, it must be surrounded
// with parenthesis in addition to braces, or instead of braces:
MessageBox.Show("3 times 4 equals \{lie ? 11 : 12}."); // SYNTAX ERROR
MessageBox.Show("3 times 4 equals \(lie ? 11 : 12)."); // OK
MessageBox.Show("3 times 4 equals \{(lie ? 11 : 12)}."); // OK
MessageBox.Show("3 times 4 equals \{(lie ? 11 : 12):000}."); // OK
// The entire embedded expression must still have the syntax of a string. So in
// the unlikely event that you need to embed a string inside the string, you would
// have to escape the quotes and any backslashes that the string contains, as the 
// following example demonstates.
var message = "The quote character \" will be replaced.";
MessageBox.Show("Your message \"\{message.Replace('\"', '\\'')}"" has been processed.");
// However, code like this is ugly and should be avoided.

// Interpolated strings are not allowed in verbatim strings; for example, the 
// contents of @"\{boo!}" are not interpreted by EC#. It does work, however, if
// you use a "double-vebatim" string:
MessageBox.Show(@@"Your message ""\{message.Replace('""', '\'')}"" has been processed.");

// "double-verbatim" or "double-at-sign" strings begin with two @ signs; the second 
// sign is used to avoid any conflict with plain C#. If you forget the second at 
// sign but the string contains a syntactically valid string interpolation, EC# 
// issues a warning that the interpolation will be ignored unless you add another @ 
// sign. A double-verbatim string does not have to contain interpolations, and it 
// differs slightly from a standard verbatim string in one other respect, described 
// below.

// Currently, the method in charge of formatting is selected (roughly speaking)
// by mere string replacement. So "String.Format" will only be resolved to
// System.String.Format if you've got a "using System" directive somewhere above.
//
// You can specify a different method, if necessary, with an assembly attribute:
[assembly:InterpolatedStringFormatter("System.String.Format")]
// The above assembly attribute ensures that the Format method will still be
// resolved correctly, even where there is no "using System" directive.

// Double-verbatim strings, like standard verbatim strings, can span multiple 
// lines, but they are interpreted slightly differently. Consider the following
// statement:
class Program {
	public static void Main(string[] args)
	{
		Console.WriteLine
			(@@"What do you want to do?
			  1. Loop
			  2. Triangle
			  X. Exit");
		while (!(Console.ReadKey(true).KeyChar:k in ('1', '2', 'x', 'X'))) {}
		if (k == '1') 
			for(int i = 0;; i++)
				Console.WriteLine("Looping ({0})", i);
		if (k == '2')
			for (int i = -12; i <= 12; i++)
				Console.WriteLine(new string('*', Math.Abs(12 - Math.Abs(i)) * 2));
	}
}

// If the menu were a normal verbatim string, the menu options would be printed 
// starting with three tabs and two spaces, which is far more than you want. In 
// contrast, in a double-verbatim string, indentation after the newline is ignored
// up until the degree of indentation of the first line. Thus, the menu options 
// above are indented by two spaces (since they start two spaces to the right of 
// the first non-whitespace character on the line).
//
// The EC# compiler should not have to be aware of the width of a tab character.
// Therefore, EC# expects you not to change between spaces and tabs when 
// indenting subsequent lines. The compiler will issue a warning if it encounters 
// tabs where spaces were expected or vice versa. Compilation will still proceed, 
// with the assumption that a tab is four spaces wide.
//
// To illustrate this rule, the following examples use "TAB" for tabs and "_" for
// spaces:

TAB TAB Console.WriteLine(@@"First line
TAB TAB Second line (not indented at all)"); // OK

________Console.WriteLine(@@"First line
________TAB ____Second line (indented by one tab and four spaces)"); // OK

TAB TAB Console.WriteLine(@@"First line
________Second line"); // WARNING! Double-verbatim string is continued with 
                       // different whitespace than the first line had.

________Console.WriteLine(@@"First line
TAB TAB Second line"); // WARNING!

// At most one warning is issued per double-verbatim string. By the way, when you
// use a double-verbatim string, you do not HAVE to indent it, but if you do indent
// it then the initial whitespace is ignored. For example:
class Program {
	public static void Main(string[] args) {
		Console.WriteLine(@@"Line 1 - not indented
Line 2 - no spaces at the beginning of this line (obviously)
	Line 3 - still no spaces
		Line 4 - still no spaces
			Line 5 - indented by one tab");
	}
}

////////////////////////////////////////////////////////////////////////////////
//                        //////////////////////////////////////////////////////
// compile-time templates //////////////////////////////////////////////////////
//                        //////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// C# has had a feature called generics since C# 2.0, which allows you to write
// code that can handle many different data types with a single block of code.
// Generics are well-designed, and they are defined as part of the CLR (.NET 
// runtime system), not just C# itself. EC# does not and cannot change the way
// generics work, but it does define an alternative to generics for situations 
// that standard generics don't support very well.
// 
// EC#'s alternative to generics is called "templates". Both generics and templates
// can be "instantiated"; the fundamental difference between the two is that
// generics are instantiated at runtime, while templates are instantiated at
// compile-time. Assuming you are already familiar with standard generics, let's
// examine an example of a template:
public ElType<L> Min<$L>(L list)
{
	var min = list[0];
	for(var i = 1, count = list.Count; i < count; i++)
		if (min > list[i])
			min = list[i];
	return min;
}

int Min(int[] array) { return Min(array); }

// The dollar sign indicates that L is a template parameter. At first I intended
// to use a keyword for this purpose, but I felt that templates would be used
// often, so there should be a very easy way for you to say that you want a 
// template. A full keyword such as "template" would not only make the code 
// longer, but make it harder for a human to visually parse.
//
// This "Min" method finds the minimum value in an indexed list. It does not 
// require a generic constraint (where clause) to indicate the interface that L
// must support (although you can use a where clause if you want); basically, L is
// compile-time duck-typed. If you have programmed in C++ or D then you should be
// familiar with this behavior; it is exactly the same. Now, this template uses 
// ElType<$L> to denote the type of elements inside L. ElType<$L> refers to 
// another template:
alias ElType<$L> = typeof<default(L).GetEnumerator().Current>;

// So ElType<$L> is just another name for the type returned from
//    default(L).GetEnumerator().Current
// which should be the type of any elements stored in the list. Of course, if
// you actually RAN that expression for some class type L, default(L) would
// be null so GetEnumerator() would cause a NullReferenceException to occur.
// That's not a problem, however, because typeof<> does not actually run any
// code, it just figures out what the data type of the result would be "in 
// theory". It's like how you can write "var x = default(object).ToString()" 
// and the compiler still figures out that x is a string even though the 
// expression causes an exception at runtime.
//
// The dollar sign is required to define a template parameter (since without it, 
// you would be defining a plain C# generic parameter), and you are allowed to also
// use a dollar sign to refer to all template parameters that are inside angle
// brackets, for example "ElType<$List<int>>". The dollar sign indicates that the 
// parameter (in this example, List<int>) is being inserted into a template--it 
// does not indicate that List<int> itself is a template or a template argument. 
// In this case the dollar sign is not required, but in situations where a template 
// parameter is a value (see below), the dollar sign is required for grammatical 
// clarity (to avoid ambiguity in the EC# language, particularly in type casts).
//
// Templates can accomplish more than ordinary generics can:
// 
// 1. Although there is only one template parameter, Min actually has access both 
//    to the list type AND the item type ElType<L>. In fact, in theory, a single
//    template parameter $C is enough to accomplish anything because C can act as
//    a bundle to reference any number of other types. This alone makes templates
//    more powerful than generics; I ran into a situation in C# recently where 
//    seven generic parameters were appropriate and it was just far too unweildy
//    to actually use seven (I picked two that seemed the most important, but this,
//    too, led to code that was somewhat unweildy.)
// 2. Templates can use all the functionality of a type, including operators; Min
//    uses the ">" operator, for example. This make templates far more useful for
//    numeric programming than generics.
// 3. Templates can contain "static if" and "static foreach" statements, so you can 
//    easily write special code for certain data types.
// 4. Since templates are specialized at compile-time, a template specialized for a
//    given type will have the same performance as code written by hand for the 
//    same type. The same guaratee is not available for standard .NET generics; my 
//    perfomance tests show that, depending on the circumstances, generics sometimes
//    have the same performance as hand-written code but they can be slower. 
//    Specifically, for number manipulation, templates have higher performance and
//    are much easier to use.
// 5. Templates can have constant expressions such as "123" as parameters. In this
//    case a dollar sign must be placed in front of the expression (e.g. $123); 
//    more about this later.
// 
// The following example shows how templates can use "static if" statements that 
// make judgements about their parameters, using the "type ... is" and "exists" 
// operators.
int ToInt<$T>(T value)
{
	// Check whether T is string. In general, you can't simply use "T is string" 
	// because normally the left-hand side of "is" is an expression, not a type. 
	// The "type" prefix is defined to force it to be interpreted as a type instead.
	static if (type T is string)
		return int.Parse(value);
	// The "exists" operator checks whether an expression compiles without error.
	else if ((int)value exists)
		return (int)value;
	// "type" and "exists" can be combined.
	else if (type ElType<$T> exists)
		throw new InvalidOperationException("Can't convert a collection to an int! You fool!");
	// You can check that an expression is valid and has a certain type at the same
	// time using 'exists as'.
	else if (value.ToInt())
		return value.ToInt();
	else
		return Convert.ChangeType(value, typeof(T));
}

// When you use 'exists', be careful not to make a spelling mistake or to leave
// out a 'using' directive. The compiler will allow such an expression and its value
// is always false. Currently, the compiler will issue a warning if a directly-named
// symbol (such as 'A', 'C' and 'E' in 'A.B<C.D>(E.F) exists') does not exist.
// However, there is no warning for, say, 'Int32.EmptyArray exists' because someone
// might actually create a symbol called 'EmptyArray' in a static class:
static class Int32 { public static readonly int[] EmptyArray = new int[0]; }
// Therefore, it is reasonable for another piece of code to test whether it
// exists, but the compiler cannot warn about a spelling error.

// These operators aren't limited to templates or static ifs:
bool true1 = type string is object;         // true, a string is an object
bool true2 = type Int32 exists as ValueType; // true, Int32 is a ValueType


// SCRATCHPAD

$(makePoint<double>("D"));

[CompileTime] decls makePoint<$Type>(string suffix)
{
	return decls {
		public struct $("Point" + suffix) : IPoint<$Type>
		{
			$Type X { get; set; }
			$Type Y { get; set; }
		}
	};
}
decls combine(decls a, decls b)
{
	return decls { $a; $b; };
}




// Symbol replacement:
const string len "Length";
string s = "hi";
int two = s.$len;

using CompileTime.Reflection;

// Compile-time reflection: useful for serialization
class Foo {
	int _x, _y;
	string _s;
	void Save()
	{
		static foreach(var field in typeof(typeof(this)).Fields(:this, :base, :static))
			Console.WriteLine(this.$field);
	}
}

public macro class SaveJson<T> {
	public void Apply<T>(type T) {
		macro() {
			int _x;
			void f() {}
			class Foo {}
		}
	}
}

// IS THERE AN ALTERNATIVE TO THESE DOLLAR SIGNS AT INSTANTIATION??
// Goal: to supply an integer to a struct template...
struct FixedArray<T, $public int Length> {
	T[] _a = new T[Length];
	public T this[int i]
	{
		get { return _a[i]; }
		set { _a[i] = value; }
	}
}

default alias IntAlias5 = IntAlias<$5>;



// Although templates are quite powerful, they are not meant to replace generics,
// because generics have their own advantages:
//
// 1. Templates only exist at compile-time, so unlike generics, one assembly 
//    cannot use a template defined in another assembly (at least not yet), and
//    you cannot create new specializations at runtime.
// 2. Templates cause compile-time code bloat: every new type that you use for 
//    $L causes a new specialization of Min() to be created. Generics only cause 
//    run-time code bloat, so your DLL is generally smaller. Also, code bloat is 
//    limited when you use reference type parametes (in MS.NET, generics are 
//    specialized for each value type, but only once for all reference types.)
// 3. Generics provide a useful guarantee: instantiating a generic method or class 
//    never fails for any type argument that meets the generic constraints (the
//    where clause). In contrast, template parameters are "duck-typed" by deault,
//    as the "Min" example demonstrates, which means that instantiating a template 
//    will fail whenever you try to use a type that does not have the necessary
//    methods, operators or conversions defined on it. For example, Min(13) 
//    obviously would not work. A more subtle example would be a list that only
//    implements indexing through an explicit interface implementation. For example,
//    one might naively expect the following code to work:
//    
//    class IntCollection : IList<int>
//    {
//        int IList<int>.this[int i] { get { ... } }
//        ...
//    }
//    int GetMin(IntCollection c) { return Min<IntCollection>(c); }
//
//    But when Min uses list[0] and list[i], it cannot reach the explicit interface
//    implementation ("list" has type IntCollection, not IList<int>, so the indexer
//    cannot be used directly on "list"). You can fix this problem by using a where
//    clause (see below), but then the template would no longer be able to use a
//    comparison operator ("min > list[i]") because there is no way to ask for 
//    operators using a where clause (which follows the rules of plain C#).



////////////////////////////////////////////////////////////////////////////////
//                         /////////////////////////////////////////////////////
// small refinements to C# /////////////////////////////////////////////////////
//                         /////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// Some small refinements to C# have already been described: constructors that
// auto-initialize variables, covariant return types and getter/setter independence.
// Here are a few more features that EC# offers.

// EC# allows you to put methods, fields, properties and events outside any type.
// When converted to plain C#, global members are implicitly placed in a static
// class called "Global", located in whatever namespace the global members are 
// located. You can choose the class name to use when these members are consumed 
// by other .NET languages. The following assembly attribute changes the name of 
// the class used for namespace "MyNamespace" to "MyCustomName":
//    [assembly:GlobalClass("MyNamespace.MyCustomName")]
const double PI = Math.PI;

// You can also define extension methods at global scope.
int ToInteger(this string s) { return int.Parse(s); }

// EC# allows static members to become part of an interface.
partial struct Fraction : IAdditionGroup<Fraction>
{
	public init(public int Numerator, public int Denominator) { }
	public Fraction Plus(Fraction b) { ... }
	public Fraction Minus(Fraction b) { ... }
	
	// This property is static, but the interface still includes it. 
	// Unfortunately, you cannot call this static method through a null
	// reference to IAdditionGroup<Fraction>, so someone still needs to
	// create an boxed instance of Fraction before calling this property.
	// It would be nice to eliminate this restriction; ideas welcome.
	public static Fraction Zero { get { return new Fraction(0, 1); } }
}


// Void is treated a first-class type whose only value is "()", also known as
// default(void):
void nothingToSeeHere = (); // OK

// By itself, the void value is useless. But notice that the .NET framework has 
// two classes, Dictionary<K,V> and HashSet<K>, which are both hashtables, the only
// difference being that HashSet doesn't have a value associated with each key.
// If .NET properly supported void as a first-class type, it would not have been
// necessary to create HashSet<K>, since Dictionary<K,void> could do the job
// just as well. (true, HashSet has additional functionality such as 
// IntersectWith() and ExceptWith(), but these could have been added to 
// Dictionary as extension methods.) Similarly, when one needs a sorted binary
// tree without associated values, one could use SortedDictionary<K,void>.
//
// So in normal code, it is not useful to have void as a first-class type, but
// when you are using a generic class it becomes useful because you might not
// need a certain type parameter, and you would like to collapse it to nothing.
//
// C# and .NET itself don't treat void very well, though. C# refuses to allow 
// values of type System.Void, and .NET itself considers ANY empty structure
// to have a size of 1 byte instead of 0. Given struct alignment requirements, 
// a 1-byte structure is very wasteful because it usually consumes 8 bytes on a 
// 64-bit machine! 
//
// Given the lack of support in the CLR itself, why do I bother supporting void
// in EC#? Mainly it is a form of protest against the limitations of .NET, to 
// teach people that an empty structure does have legitimate uses. And if you 
// are still writing code for .NET 2.0, where HashSet is not available, it 
// makes sense to use Dictionary<K,void> instead. If, in the future, EC# targets
// native code instead of the CLR, it will be possible to give 'void' a size of
// zero so that it works efficiently.
//
// Since C# does not allow us to use System.Void, EC# replaces void with 
// "System.RuntimeVoid" when you actually use it as a value, and this type is
// auto-defined if no referenced assembly includes it. Functions that return
// void are left alone, i.e. they do not return RuntimeVoid at run-time, even 
// though they conceptually return the void value, ().


// In EC#, interfaces can have static methods which are public by default. These
// static methods are allowed to be extension methods that extend the same 
// interface that is being declared.
interface IStatic {
	static Func<IStatic> Factory { get; set; }
	static bool IsNull(this IStatic self) { return self != null; }
}

// EC# also allows interfaces to have default implementations. A default 
// implementation can be replaced, or not, by the implementing class. Default
// implementations often make more sense than extension methods because they 
// may offer an opportunity for optimization. For example, consider this:
interface ISource<T> : IEnumerable<T> {
	int Count { get; }
	bool IsEmpty { get { return Count == 0; } }
}
// If the class that implements ISource knows in advance how many items are 
// in the collection, then the default implementation of IsEmpty is sensible
// and should be kept. However, if the class is a linked list then it could
// be expensive to count the elements. Therefore, IsEmpty should be replaced 
// with a custom version that just checks whether a head element exists.


// EC# supports the ??= operator, which is used to set the value of a variable
// or property if its current value is null. (x ??= y) is equivalent to 
// (x = x ?? y). For example, the following property returns the value of
// Environment.TickCount the first time it is called, and then it returns the 
// same value on every subsequent occasion.
int? _startTime = null;
int StartTime { get { return _startTime ??= Environment.TickCount; } }

// EC# introduces the "null dot" operator, a.k.a. the "safe navigation operator",
// which is used to avoid null reference exceptions by not calling methods on 
// objects that are null.
// 
// For example, what if "DBConnection", "PersonTable", and "FirstRow" 
// in the following line might all return null?
var firstName = DBConnection.Tables.Get("Person").FirstRow.Name;

// In plain C#, it's a giant pain in the ass to check if each object is null:
string firstName = null;
var dbc = DBConnection;
if (dbc != null) {
    var pt = dbc.Tables.Get("Person");
    if (pt != null) {
        var fr = pt.FirstRow;
        if (fr != null)
            firstName = fr.Name;
    }
}

// But with the safe navigation operator, "?.", it's easy. The above code (with
// three "if" statements) is equivalent to this one line of EC#:
var firstName = DBConnection?.Tables.Get("Person")?.FirstRow?.Name;

// The syntax "?." and name "safe navigation operator" is borrowed from the Groovy 
// language. However, EC#'s operator is slightly more sophisticated than the one
// in Groovy.
//
// In EC#, the null dot operator provides protection on the entire chain of dots
// (whether they are "null dots" or "regular dots") following the first "?.", in
// the same subexpression. In the above example, suppose "DBConnection"
// returns null. In that case it is safe to access Tables, but what about the
// Get() method? It was not accessed with "?.", so does it still have any 
// protection?
//
// To make the problem more clear, suppose we rewrite the above line as
var temp = DBConnection?.Tables;
var firstName = temp.Get("Person")?.FirstRow?.Name;
// If "?." was a "normal" operator, then the two statements here should be 
// equivalent to the single statement above, just as we can rewrite
// "int x = 2*3*4*5" as "int temp = 2*3; int x=temp*4*5;". However, in fact
// "?." does NOT behave like a normal operator, and the two-statement version 
// is NOT equivalent!
//
// In this version, if DBConnection == null then temp == null, so the attempt
// to call temp.Get("Person") will cause a NullReferenceException because Get() is
// called through a regular dot, not a null dot.
//
// In effect, the "?." operator in the first line is useless because it only 
// protects the first line from a NullPointerException, when we really need 
// protection on both lines.
//
// Now let's look at the original version again:
var firstName = DBConnection?.Tables.Get("Person")?.FirstRow?.Name;
//
// If the null dot behaved like a normal operator, then it would be equivalent to
var firstName = (DBConnection:dbc != null ? dbc.Tables : null).Get("Person")?.FirstRow?.Name;
//
// However, in reality the null-dot is fancier than a normal operator; it extends
// its protection to the other dots in the same subexpression, like this:
var firstName = (DBConnection:dbc != null ? dbc.Tables.Get("Person")?.FirstRow?.Name : null);
//
// Since Tables.Get("Person") uses a normal dot, a NullReferenceException can
// occur if "Tables" returns null, but a NullReferenceException cannot occur just 
// because "DBConnection" returns null.
//
// Please note that the following version of the statement does NOT provide this
// protection:
var firstName = (DBConnection?.Tables).Get("Person")?.FirstRow?.Name;
// The parenthesis in this version have the effect of isolating the first "?." from 
// the rest of the expression, which disables the protection that it would normally 
// provide. Therefore, this version is interpreted as
var firstName = (DBConnection:dbc != null ? dbc.Tables : null).Get("Person")?.FirstRow?.Name;
// Clearly, this version causes a NullReferenceException if DBConnection is null,
// so it should not be used.

// The null-dot operator is compatible with value types. For example, if have a
// list object called "list" and I call "list?.Count", the result is a nullable
// integer that contains null if list==null, and list.Count otherwise.
//
// When used this way, it is often convenient to combine the ?. and ?? operators:
int GetCount<T>(ICollection<T> collection) {
	// Returns the number of items in the collection, or 0 if collection==null.
	return collection?.Count ?? 0;
	// Equivalent to "return collection != null ? collection.Count : 0"
}

// EC# allows the type of a field to be inferred, if possible.
var _assemblyTable = new Dictionary<string, Assembly>();

// As mentioned before, EC# allows the type of a lambda function to be inferred 
// to be one of the "Func" delegate types. For example:
var Square = (double d) => d*d;

// EC# introduces the keyword "def" for declaring methods, a syntax copied from
// Python and Boo. If a method is declared with the "def" keyword, it is optional 
// to specify its return type, and the return type is inferred from the content 
// of the method, e.g.
def Cube(double d) { return d*d*d; }


// In EC#, you can overload the dot operator. This operator is used not only
// when a type is accessed with an actual dot, but it also provides access to
// the operators (including conversion operators) inside the inner type. The 
// dot operator has low priority: it is used only if nothing on the original 
// type can do the job. For example:
class FakeString {
	public init(public string Value) {}
	public static string operator.() { return Value; }
	public static implicit operator string(FakeString ps) { return ps.Value; }
	public static implicit operator FakeString(string rs) { return new FakeString(rs); }
}

// Since PretendString overloads operator., you can of course call the 
// methods and properties of string from a PretendString:
Debug.Assert(new PretendString("hello").StartsWith("hell"));

// The implicit conversion operators are needed to use operator overloads, such
// as operator+, because the actual operator+ in System.String expects two
// strings, not two PretendStrings, and it returns a real string. So the 
// following statement can only work if both implicit conversions exist:
PretendString food = new PretendString("foo") + new PretendString("d");

// Plain C# allows you to get the run-time Type of a type T using typeof(T).
// EC#, in addition, allows you to use "typeof<exp>" in place of a type, to
// convert an expression "exp" into a type, in certain contexts where a type 
// is expected. The change from parenthesis () to angle brackets <> tells the
// compiler that typeof represents a compile-time type instead of a reflection
// object.
//
// If the typeof expression contains a greater-than sign, the expression must
// be placed in parenthesis:
typeof<7>6> booleanVar = true; // SYNTAX ERROR
typeof<(7>6)> booleanVar = true; // OK
typeof<new List<int>()> = new List<int>(); // SYNTAX ERROR
typeof<(new List<int>())> = new List<int>(); // OK, but VERY BIZARRE
// The expression inside typeof<> cannot create any real variables, and the 
// compiler will give an error if any variables are created whose name does not
// consist entirely of underscores.

// If EC# had its own real compiler, it could support ref locals and ref returns.
// See here for a sketch of the feature:
// http://blogs.msdn.com/b/ericlippert/archive/2011/06/23/ref-returns-and-ref-locals.aspx

// Also if EC# had its own real compiler, it would be feasible to standardize the
// names of the generic classes used by anonymous types:
// http://blogs.msdn.com/b/ericlippert/archive/2010/12/20/why-are-anonymous-types-generic.aspx

// Standard C# casts are ambiguous in two ways. The first way has to do with 
// negative numbers:
var x = (MyEnum)-2; // ERROR: To cast a negative value, you must enclose the value in parenthesis.
// This error occurs because the parser cannot tell if this is a subtraction or
// cast without first knowing whether "MyEnum" is a value or a type. Besides, it's
// possible that there could be both a type AND a variable called "MyEnum", and
// then what would it mean?
//
// The second ambiguity involves delegates:
delegate int TestD(object obj);
class AmbiguousCast2
{
	static TestD TestD;
	static void CallOrCast()
	{
		object obj = "Is this a cast or a delegate call?";
		var result = (TestD)(obj);
	}
}
// This code could reasonably be invoking the delegate field "TestD" using "obj" as 
// an argument, or it could be casting "obj" to type "TestD". Plain C# assumes that 
// (TestD)(obj) is a cast, however, so "result" has type TestD. That's reasonable,
// since you can't just remove the parenthesis around TestD if you want to invoke
// it. Slightly less reasonable is the fact that the parenthesis are not allowed 
// even when there is only one interpretation:
class AmbiguousCast3
{
	static TestD MyTestD;
	static TestD TestD() { return MyTestD; }
	static void CallOrCast()
	{
		object obj = "Must be a delegate call?";
		var result2 = (TestD())(obj); // OK, this calls TestD() and then invokes the result
		var result3 = (MyTestD)(obj); // ERROR: 'MyTestD' is a 'field' but is used like a 'type'
	}
}
// To a human, "(MyTestD)(obj)" may appear unambiguous, in that there is only 
// one interpretation that makes sense. But to keep the compiler simple, the 
// parser must interpret the code without knowing whether "MyTestD" is a type, a
// variable or a property. So it assumes that (MyTestD) is a cast because it looks
// like one, and then it complains later, during semantic analysis, because its
// assumption was wrong. "(TestD())", however, cannot possibly be a cast because
// type names can't have parenthesis, so the compiler does not get confused.
//
// The way casts work in C# creates a constaint on the syntax of type names. We
// would like something like Foo<7> x to be 

// EC# introduces the positional keyword "cast" for doing casts. This keyword is intended 
// to make casts easier to find through text searches, and they overcome the 
// abiguous syntax of code such as this:
var x = -2 as is MyEnum; // OK, this must be a cast to the type MyEnum
*/

////////////////////////////////////////////////////////////////////////////////
//                         ///////////////////////////////////////////////////////
// the `backtick` operator //////////////////////////////////////////////////////
//                         ///////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# defines a new string token based on backticks such as `this string`.
// This token does not define a new type of string; instead, a backticked string
// is actually a new operator called the "backtick operator". It can be used 
// either as a binary operator or unary postfix operator, and its effect is to
// invoke either an "operator" or a global method that has the same name and the
// same number of arguments. Inside the backticks, spaces are equivalent to 
// underscores.
//
// For example:
bool operator is_odd(int x) { return (x & 1) != 0; }
void UseBackticks()
{
	if (7 `is odd`)
		Console.WriteLine("It's true, 7 is odd!");
}

// EC# does not care whether a method to be backticked is declared with the 
// "operator" keyword or not; "operator" serves only as documentation and it
// is stripped out during conversion to plain C#. As I mentioned, backtick
// is also a binary operator:
bool is_even(int x) { return (x & 1) == 0; }
bool divides_into(int a, int b) { return b % a == 0; }
void UseBackticks(int x)
{
	for (int i = 0; i < 50; i++) {
		if (i `is odd`)
			Console.WriteLine("{0} is even", i);
		if (i `divides into` 100)
			Console.WriteLine("{0} divides into 100", i);
}

// The backtick operator has low precedence, just above the relational operators
// such as '==' and '>'. For example, the expression
//    x + y `divides into` z >> 2 == false
// means
//    ((x + y) `divides into` (z >> 2)) == false

// One useful operator that I'd like to mention is the "mod" operator, which is 
// like the C# remainder operator (%) except that its output range is the same 
// whether the second argument is positive or negative:
int mod(int x, int m)
{
    int r = x%m;
    return r<0 ? r+m : r;
}
void UseMod()
{
	for (int i = -5; i < 5; i--)
		Console.Write(i `mod` 5); // Output: "01234012340"
}
// In contrast, the sign of (i % 5) is negative when i is negative, but this result
// is almost never useful. For example, if I have an angle that I would like to
// "normalize" to the range 0 to 360, I can to it with (angle `mod` 360), but
// (angle % 360) does not work if the angle is negative.
//
// Just remember that backtick has a low precedence, so (10 `mod` 4 + 3) actually 
// means (10 `mod` (4 + 3)).

// A backticked string cannot be used as prefix operator because it would make the 
// language ambiguous. For example, it would be impossible to figure out (without
// knowing whether 'a' and 'b' take one or two parameters) whether the expression
//    X `a` `b` Y
// means
//    (X `a`) `b` Y
// or
//    X `a` (`b` Y)
//
// Assuming there are three operator types (prefix, postfix and infix), it turns 
// out that you can pick any two for a new operator: it can be prefix/postfix,
// prefix/infix, or infix/postfix, but it cannot be all three of these.

Since ordinary function call

// EC# allows you to define identifiers that contain punctuation using the 
// backslash operator:
bool IsEC\#Code = true;      // Declares a variable called "IsEC#Code" in EC#
// If you've never seen escaped identifiers this may seem a little crazy, but in
// fact plain C# already has a similar feature, namely, unicode escapes:
bool IsEC\u0035Code = false; // Declares "IsEC#Code" in plain C#
// The escape character is currently not permitted before a normal identifier
// character, not even a number, nor can you escape the space or tab characters.
// These restrictions keep our options open for a possible future backslash 
// operator, although we don't know yet what it would mean. The exception, of 
// course, is that excapes like \u0035 are still allowed in EC#.

// Since you can easily define identifiers that contain punctuation, it is
// straightforward to define new operators that contain punctuation:
char operator \

bool divides
int b = 
bool operator is_odd(int x) { return (x & 1) != 0; }

 new binary operator and a new postfix operator using 
// syntactic sugar for 


////////////////////////////////////////////////////////////////////////////////
//                  ////////////////////////////////////////////////////////////
// pairs and tuples ///////////////////////////////////////////////////////////
//                  ////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# allows tuples to be expressed as a comma-delimited list. A trailing comma
// forces an expression to be treated as a tuple.
var triplet = ("five", 6, 7.0);
var pair = (3, 4);
var single = (3,);

// A tuple can be unpacked by making it the target of assignment:
int a;
(a, var s) = Tuple.Create(45, "forty-five");
// The tuple-unpacking operator assumes that the type has members named "Item1",
// "Item2", etc. in the unpacked type, so it can unpack types other than 
// System.Tuple that have the same members. The line above is reduced to plain 
// C# as something like
var @0 = Tuple.Create(45, "forty-five");
a = @0.Item1;
var s = @0.Item2;
// The colon operator can unpack tuples, too, but it can only create new variables,
// not set existing variables.
Tuple.Create(45, "forty-five"):(x,y);

// If a tuple is unpacked inside another expression, the expression is converted
// into multiple statements, in the same way that variable declarations inside
// expressions cause statements to be created. For example,
if (a < 50 && ((a, var s) = Tuple.Create(45, "forty-five")).Item1 < 50)
	return;
// is converted to
if (a < 50) {
	var @0 = Tuple.Create(45, "forty-five");
	a = @0.Item1;
	var s = @0.Item2;
	if (@0.Item1 < 50)
		return;
}

// When you unpack a tuple, you can use the identifier '_' (called 'blank') to 
// discard a value. For example, the following statement discards the first two 
// values in the tuple and assigns 3 to 'three'.
(_, _, int three) = (1.0, "2", 3);

// 'blank' is not limited to tuples. It can be used to read a property, too, or
// you can use it document the fact that you are discarding a return value.
_ = new Form().Handle;

// You can't do this if there is a symbol defined called '_':
{ int _ = 5; _ = "5"; } // ERROR: cannot convert string to int

// The default type of a tuple is System.Tuple by default, but you may get better
// performance by using a struct instead to represent pairs. This can be 
// accomplished (e.g. using Loyc.Essentials.Pair) as follows:
[assembly:TupleType(typeof(Loyc.Essentials.Pair<,>))]

// There is no built-in representation for the type of a tuple. Use typeof() to
// represent a tuple type by example, and remember that you need two pairs of
// parenthesis (one for typeof, and one for the tuple):
typeof((1, (object)null)) TupleOfIntAndObject()
{ 
	return (1, "This string is returned as an object");
}

////////////////////////////////////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
// multiple-source name lookup ////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# allows "multiple-source name lookup" (MSNL), i.e. lookup of static members in
// multiple classes. In addition, static classes are ignored for resolving variable
// declarations. These two changes allow you to write "static extension methods". 
// For example, the following code is illegal in standard C#, but works in EC#.
namespace MSNL {
	// By itself, this class is legal in plain C#, but it's hard to actually use it.
	public static class String {
		public static string Join(IEnumerable things, string delimiter)
		{
			StringBuilder sb = new StringBuilder();
			bool first = true;
			foreach (object thing in things)
			{
				if (!first)
					sb.Append(delimiter);
				first = false;
				if (thing != null)
					sb.Append(thing.ToString());
			}
			return sb.ToString();
		}
	}
}
namespace NSNL2 {
	//using System;
	using MSNL;
	
	void TwoJoins()
	{
		var list = new List<string> { "A", "B", "C" };
		var array = new string[] { "A", "B", "C" };
		
		// In plain C#, "String" will always be interpreted as MSNL.String if "using 
		// System" is commented out, even if "using System" appears above the namespace 
		// SNL2. If "using System" is not commented out then "String" will always be
		// considered an "ambiguous reference between 'MSNL.String' and 'string'".
		// However, in EC# you can freely call methods from both String classes.
		string join1 = String.Join(list, "-");  // Refers to MSNL.String.Join()
		string join2 = String.Join("-", array); // Refers to string.Join() in EC#
		
		// You might wondered why the parameters are reversed. It's because EC# has
		// "anti-hijacking" rules which are a mixture of the rules used in plain C# 
		// and the D programming language. If the parameters were in the same order, 
		// you would get a warning on the second call, because the call 
		// String.Join("-", array); could match both MSNL.String.Join() and
		// System.String.Join(). The call would still be allowed, however, because
		// it is actually allowed in plain C# (String always resolves to MSNL.String 
		// in plain C#). However, if "using System" is not commented out then any 
		// attempt to call methods on "String" is an error in plain C#. Therefore, 
		// in EC# the warning would be escalated to an error. See below for a brief
		// explanation of the anti-hijacking rule.

		// The following is illegal in plain C# but legal in EC#. The reason is that
		// plain C# thinks that String can refer to "MSNL.String" even though 
		// "MSNL.String" is a static class. In EC# it must refer to System.String.
		String hello = "hello";
	}
}

// The anti-hijacking rule says that a set of arguments should not match two 
// functions in two different classes or namespaces (even if one match is
// better). The purpose of the rule is to avoid accidents in which new code
// (perhaps written by a third party, perhaps not) silently "hijacks" a 
// function call. For example, imagine that we have a function "Foo.Lish" and 
// we're trying to call it:
using NS1;
using NS2;
namespace NS1 {
	static class Foo {
		public static void Lish(IEnumerable<char> x) {}
	}
}
void Bar() { Foo.List("Hello!"); } // OK

// All is well. But now, suppose that there is another "Foo" class elsewhere.
// Another developer, someone who is unaware of Bar() and may be unaware of 
// NS1.Foo, decides to make his own Foo class with a Lish() method:
namespace NS2 {
	class Foo {
		public static void Lish(string x) {}
	}
}

// Without an anti-hijacking rule, the code in Bar() that used to call 
// NS1.Foo.Lish() would silently start calling NS2.Foo.Lish() instead. This
// is dangerous if no one knows that the meaning of the code has changed. 
// The anti-hijacking rule is a safety feature to prevent this kind of silent
// change to a program's semantics. Once an error has been issued, the user
// can fix the problem by adding qualification (NS1.Foo.List()). Please
// note that the anti-hijacking rule is suspended if Foo is defined in the
// same namespace as Bar, since it is very likely that Bar wants to use a
// class in the same namespace.

// Although EC# allows static name lookups in multiple classes, it does not 
// currently support name lookup in multiple variables/properties, or in both a 
// variable and a class. For example:
class NotSupported
{
	NotSupported String;
	void f() {
		var abc = String.Join("-", new string[] { "A", "B", "C" }); // ERROR
		// Because "String" refers to the field "String" not the class "String".
	} 
}

// MSNL works similarly for global members in multiple namespaces.
namespace MSNL {
	void Overload(string s) {}
}
namespace MSNL2 {
	using MSNL;
	void Overload(int i) {}
	void NoProblem() { Overload("string"); } // OK
}

// MSNL also works between classes and namespaces.
namespace MSNL3 {
	public class System {
		public class String {
			public static void Print(string s) { Console.Writeline(s); }
		}
	}
	void NoProblem() {
		System.String.Print("Hi mom!"); // OK, refers to MSNL3.System.String
		System.String.Intern("Hi mom!"); // OK, refers to global::System.String
	}
}

// In combination with the rule about constructors that was described earlier,
// MSNL can be used to define "extension constructors":
namespace MSNL {
	public static class String {
		public static string new(int x) { return x.ToString(); }
	}
	void Example() { string seven = new String(7); } // OK, equivalent to String.new(7)
}


////////////////////////////////////////////////////////////////////////////////
//         /////////////////////////////////////////////////////////////////////
// aliases /////////////////////////////////////////////////////////////////////
//         /////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// Simple aliases define alternate names for types:
alias Map<K,V> = Dictionary<K,V>;
// EC# also allows "using" directives to have template parameters. "using" is 
// defined in EC# as a special type of alias that is only available in the 
// lexical scope where it is placed.
using Map<K,V> = Dictionary<K,V>;

// The "alias" construct creates new type names for existing types, optionally 
// with a custom set of methods. But an alias doesn't create a real type, so the 
// new methods cannot be virtual.
alias MyString = string
{
	// Members of an alias are public by default
	int ToInt() { return int.Parse(this); }
	
	// Default forwarding operator. An alias that defines its own methods requires 
	// a forwarding operator if it wants to provide access to the original methods.
	public static string operator.(MyString s) { return s; }
}

// After conversion to plain C#:
[Alias] struct MyString {
    public string @this;
    public MyString(string @this) { this.@this = @this; }
    public static int ToInt(string @this) { return int.Parse(@this); }
}

// Normally, the original type is allowed anywhere that the alias is allowed. However,
// an "explicit alias" does not allow implicit casting from the original type:
MyString path0 = @"C:\";      // OK
explicit alias Path = string;
Path path1 = @"C:\"           // Error: a cast is required
Path path2 = @"C:\" as Path;  // OK

// "alias" can be used like "using" except that the names are visible in other source files:
alias Polygon = List<Point>; // in A.ecs
var myPoly = new Polygon();  // in B.ecs

// "alias" a positional keyword. It is only recognized as a keyword if it is 
// followed by an identifier and is located in a place where an alias statement 
// is allowed. For example:
namespace alias { // OK
	class alias { // OK
		alias() {} // OK
		alias(alias alias) {} // OK
	}
	var Var1 = new alias(); // OK
	alias Var2 = new alias(); // SYNTAX ERROR in alias statement
}

// Function overloading based on aliases is not allowed because it would add a lot
// of complexity to EC# and run-time reflection couldn't support it anyway.
void IllegalOverload(Polygon p);
void IllegalOverload(List<Point> p); // ERROR: conflicts with existing overload

// After a colon, an alias can list one or more interfaces. If the aliased type does not
// already implement the interface, EC# can create a run-time wrapper type that implements
// the interface, but a wrapper is only created when an alias value is casted (implicitly 
// or explicitly) to the interface. For example:
public interface IOneProvider<T>
{
	T One { get; } // This will be used in a later example
}
public interface IMultiply<T> : IOneProvider<T>
{
	T Times(T by);
}
public partial alias MyInt = int : IMultiply<MyInt>
{
	MyInt Times(MyInt by)  { return this * by; }
	MyInt One              { get { return 1; } }
	public static int operator.(MyInt i) { return i; }
}
IMultiply<MyInt> seven = 7 as MyInt;
int fourteen = seven.Times(2);

// When an alias appears as a generic parameter, it is always reduced to the original
// type in plain C# so that, for example, IMultiply<int> and IMultiply<MyInt> are
// interchangeable. Unfortunately, this means that aliases cannot be used to affect
// generic constraints. For example:
public partial alias MyInt : IComparable<MyInt>
{
	// This method reverses the usual comparison behavior of an int.
	int CompareTo(MyInt other) { return -(this as int).CompareTo(other); }
}
int Compare<T>(T a, T b) where T:IComparable<T> { return a.CompareTo(b); }

void DoesntWork()
{
	MyInt eight = 8;
	Compare(seven, eight); // ERROR: Cannot satisfy the generic constraint 
	// 'IComparable<MyInt>' on 'Compare', because 'MyInt' is only an alias.
}

// As a workaround, you can use the [alias] macro on a struct:
[alias(typeof(int))] public struct MyInt : IComparable<MyInt>
{
	MyInt Times(MyInt by)  { return this * by; }
	MyInt One              { get { return 1; } }
	public static int operator.(MyInt i) { return i; }
	int CompareTo(MyInt other) { return -(this as int).CompareTo(other); }
}

// The macro generates implicit conversions in MyInt to make it act similarly to an alias:
[CompilerGenerated]
partial struct MyInt {
	public init(public int aliasValue) {}
	public implicit operator int(MyInt i) { return i.aliasValue; }
	public implicit operator MyInt(int i) { return new MyInt(i); }
}

// Although aliases cannot be used as generic arguments, they CAN be used as template
// arguments (see the section below about templates).
int Compare<static T>(T a, T b) where T:IComparable<T> { return a.CompareTo(b); }

////////////////////////////////////////////////////////////////////////////////
//                  ////////////////////////////////////////////////////////////
// accelerated LINQ ///////////////////////////////////////////////////////////
//                  ////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////


public Specialized<T> Specialized<static T>(this T list) 
	where T : IEnumerable<typeof(default(T).GetEnumerator().Current)>
	{ return list; }

public alias Specialized<static T> = T
	where T : IEnumerable<typeof(default(T).GetEnumerator().Current)>
{
	public static List operator.() { return _source; }
	public static implicit operator IEnumerable<T>
}
