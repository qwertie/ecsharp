// TODO: write about
// - IDE renaming refactor
// - Attribute or something: Sample template parameter for intellisense
// - interfaces with operators
// - traits


////////////////////////////////////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
// introduction to Enhanced C# /////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// This is a pep talk for myself. EC# does not yet exist, but I am writing this
// document as though it does.
//
// EC# is an enhanced version of C# that introduces numerous new features to
// help "power engineers" write code faster and write faster code. It 
// incorporates ideas from several sources, but especially from the D programming,
// language, another member of the C family of languages. In fact, eventually I 
// hope that someone will write a tool to convert EC# code to D and C++; such 
// multitargeting could make EC# an ideal language for writing general-purpose
// libraries for a broad audience.
// 
// EC# is designed to feel like a straightforward extension of the C# language,
// that does not fundamentally change the "flavor" of the language. It is also
// intended to be backward compatible with plain C#. It tries not to break any 
// code that already works, but there are some very rare exceptions, such as a
// minor ambiguity involving the new "alias" declaration.
// 
// Initially there will be no "proper" compiler for EC#. Instead, EC# code (*.ecs)
// compiles down to plain C# (*.cs) which is then fed to the standard compiler.
// EC# 1.0 will be more convenient and powerful than plain C#, but the 
// metaprogramming facilities will be relatively basic (better metaprogramming
// will have to wait for 2.0).
//
// The features planned for EC# 1.0 are as follows:
// - Easier variable declarations
//   - Declare multiple variables in one 'var' statement (first priority)
//   - Declare out parameters in-situ (first priority)
//   - Declare variables in any expression
//   - The quick binding operator (first priority)
// - Improved constructor abstraction
//   - [set] attribute (first priority)
//   - Quick type definition: field and constructor declarations combined (first priority)
//   - Constructors and static methods named 'new'
//   - Better encapsulation for constructors a.k.a. implementation hiding
//   - No-argument constructors for structs (does not work with generics)
// - Return value covariance
// - String interpolation and double-verbatim strings (first priority)
// - Compile-time code execution (rudimentary) (high priority)
//   - IsCompileTime constant
// - Code contracts (low priority)
//   - "in" clause (preconditions)
//   - "out" clause (postconditions)
//   - inheritance of contracts
// - getter/setter independence
// - Compile-time templates
//   - Method templates (high priority)
//     - Template parameter inference for methods
//   - Property templates (high priority)
//   - struct/class templates (high priority)
//   - namespace templates
//   - [DotNetName], [GenerateAll], [AutoGenerate]
//   - Dynamic linking: type unification across assemblies
// - Features in support of templates (that do not require templates)
//   - conditional overload resolution ("if" and "if legal" clauses)
//   - "is legal" and "type ... is legal" operators 
//   - "type ... is" operator
//   - "using" cast operator (also listed under "Other refinements") (high priority)
//   - "static if" statement
//   - typeof<expression>
// - Aliases (simple aliases have high priority)
//   - "using" as an alias with restricted visibility
//   - adding methods, properties and events to existing types
//   - declaring additional interfaces on existing types
//   - explicit aliases
//   - type-safe conversion to an undeclared interface (@using<T,I>)
//   - references to multiple interfaces (I<IOne, ITwo>)
// - Symbols (high priority)
//   - [OneOf]
//   - code transformations to make Symbols seem like constants
//   - implicit conversion from Symbol constant to enum constant
// - Miscellaneous refinements
//   - global methods, properties, fields, events (high priority)
//   - globally-defined extension methods
//   - globally-defined operators
//   - non-static operator methods
//   - #pragma info
//   - #pragma print
//   - "using" operator (high priority)
//   - alternate syntax for "as"
//   - [Required] attribute (first priority)
//   - first-class void (low priority)
//   - static methods in interfaces (low priority)
//   - default method implementations in interfaces (low priority)
//   - "??=" operator (first priority)
//   - null dot a.k.a. safe navigation operator (?.) (first priority)
//   - "**" operator (high priority)
//   - "in" operator (high priority)
//   - Type inference for fields
//   - Type inference for lambdas assigned to "var" (low priority)
//   - Type inference for return values ("def") (low priority)
//   - typeof<> (low priority, except in alias statements)
//   - try/catch/finally do not require braces for single statements (high priority)
//   - Break and continue at label
//   - Implicit "break" in switch statements
//     - New syntax for 'case'
//   - Implicit "var" in foreach statements (high priority)
//   - Identifiers that contain punctuation
//   - Underscores in numeric literals
//   - Shebang (#!) support
// - Code blocks as subexpressions
//   - The out statement
// - The backtick operator
//   - Nebulous precedence
// - Slices and ranges
//   - "$" pseudo-operator (high priority)
//   - ".." operator (high priority)
//   - "T[..]" syntax (high priority)
// - "on" statements
//   - on exit
//   - on failure
//   - on success
// - Tuples
//   - Tuple literals (high priority)
//   - Attribute to select tuple data type (high priority)
//   - Tuple unpacking (with or without variable declarations)
//   - assignment to blank (_)
//   - "explode" operator
// - Multiple-source name lookup (high priority)
//   - static extension methods (simulated)
//   - anti-hijacking rule
//
// As you can see, the list of features is really, really long. This project is fairly ambitious, because while C# was my favorite language for years, I still feel that it needs a lot of improvement, and after learning about D I wasn't really happy going on in C# without some of D's major features. I also considered writing a .NET back-end for D, but unfortunately .NET is too limited to accommodate some of D's features well; besides which, D's front-end is written in C++ so I didn't really care to deal with it. By enhancing C# instead,
// - I would get a self-hosting compiler, which is awesome;
// - I wouldn't have to ask anyone's permission to add features; and
// - I'd have a language that works within the limitations of .NET
//
// Last but not least, I figured EC# would attract more interest than writing a
// new library or program in D. My goal in life, after all, it to create 
// programming tools that people will actually use. That said, if you like C#,
// go learn D too! It's nice!

////////////////////////////////////////////////////////////////////////////////
//            //////////////////////////////////////////////////////////////////
// Background //////////////////////////////////////////////////////////////////
//            //////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// You may wonder, why do I want a .NET language at all?
//
// Well, the really key thing I like about .NET is that it is specifically a
// multi-language, multi-OS platform with a standard binary format on all 
// platforms--much like Java, but technically superior. It's only "multi-OS", of
// course, insofar as you avoid Windows-only libraries such as WPF and WCF, and 
// I would encourage you to do so (if your boss will allow it). Multi-language is 
// really important because there is no consensus in the programming world about 
// what language is best, and multi-platform is important to avoid vendor lock-in.
// Without a multi-language runtime, interoperability is limited to the lowest 
// common denominator, which is C, which is an extremely painful limitation. Modern
// languages should be able to interact on a much higher level.
// 
// A multi-language platform avoids a key problem with most new languages: they
// define their own new "standard library" from scratch. You know what they say 
// about standards, that the nice thing about standards is that there are so many 
// to choose from? I hate that. I don't want to learn a new standard library with 
// every new language I learn. Nor do I want to be locked into a certain API based
// on the operating system (Windows APIs and Linux APIs). And all the engineering 
// effort that goes into the plethora of "standard" libraries could be better spent
// on other things. The existence of many standard libraries increases learning 
// curves and severely limits interoperability between languages. 
// 
// This just might be the most important problem .NET solves: there is just one 
// standard library for all languages and all operating systems (plus an optional 
// "extra" standard library per language, if necessary). This makes all languages 
// and all operating systems interoperable. It's really nice! The .NET standard 
// library (called the BCL, base class library) definitely could have been 
// designed better, but at least they have the Right Idea.
//
// Java has the same appeal, but it was always designed for a single language,
// Java, which is an unusually limited language. It lacks several important 
// features that .NET has, especially delegates, value types, reified generics and
// "unsafe" pointers.
//
// And I like C#. While it's not an incredibly powerful language, it's carefully
// designed and it's more powerful and more efficient than Java. Similarly for
// the .NET CLR: yeah, it has some significant limitations. For example, it has
// no concept of slices or Go-style interfaces. Still, in my opinion it is the 
// best platform available.
//
// I should say, after learning about D (a very promising C++ replacement) it was 
// tempting just to program everything in D and forget about .NET. However, I still
// have a lot of C# code laying around, and there are some things about D that do
// not please me:
// 
// - It is not Googlable. When you search for D, Google doesn't understand that
//   "D" is not the same as the D in "I'd", "They'd" and so forth. And when you 
//   search for "D programming language", you are as likely to find a page about 
//   D1 as D2 (both versions of the language have the same name, but they have
//   significant differences.) The D community could solve this by using the name 
//   D2 instead of D.
// - The documentation is not nearly as complete or nice as the .NET Framework's
//   documentation (and the .NET Framework docs are incomplete enough already).
// - D code completion and analysis tools are pitiful compared to those for C#.
// - There are lots of odd corner cases and bugs in the D compiler, and the 
//   language does not feel as cleanly designed (e.g. the namespaces of types 
//   and variables are not clearly separated like they are in C#)
// - There is no documented way to target Android with D (but Mono does, if you 
//   fork over enough cash)
// - It doesn't support dynamic loading (I'm not sure about dynamic linking) of 
//   DLLs. In .NET, of course, dynamic loading is fundamental and unavoidable.
// - I strongly prefer a couple of C# rules: (1) C# requires "ref" and "out" at the 
//   call site, but D prohibits it. (2) C# performs "definite assignment analysis"
//   to ensure that you write to a local variable before reading from it, and this 
//   has detected bugs in my code more than once. D, however, just assigns a 
//   default value to all variables, which is 0 for integers but NaN for floating-
//   point.
//
// Since D is young and underfunded, I can forgive it. The key problem to me is 
// the challenge of becoming proficient at D. They need documentation that includes 
// a comprehensive tutorial and overview of their standard library, and a reliable 
// code analysis tool that lets me ask: what does this symbol (in some random 
// source code) refer to? I'm so impatient that I'd rather make my own language 
// than puzzle over things like this. Hence, EC#.
//
// Lest I be a hypocrite: if you find that EC#'s documentation is lacking, please 
// let me know how I can improve it.

////////////////////////////////////////////////////////////////////////////////
//             /////////////////////////////////////////////////////////////////
// Limitations /////////////////////////////////////////////////////////////////
//             /////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// One of the major limitations of C# which remains in EC# is that it is still not
// designed to be a parallel language: it doesn't provide any way to guarantee that
// multiple threads to not access mutable state at the same time. Unfortunately I
// came to realize that I am not the right person to design such features due to
// my lack of experience in the parallel space. For my entire programming career
// I have had to write code that runs fast on a single processor (first it was 
// conventional PCs from around the year 2000, then it was single-core ARM 
// devices), so the business case to learn proper multithreaded programming just 
// didn't exist for me (I preferred to write coroutines using Duff's device in C++ 
// to do cooperative multitasking, so that the dangers of multithreading did not 
// occur.) Certainly, however, I will be studying the issue and I am open to ideas.
//
// The other major limitation is that EC# 1.0's metaprogramming facilities will be
// limited to templates. EC# templates are as powerful as C++ templates and far, 
// far more user-friendly, but they fall short of the power of D's mixins. I felt 
// that it would be better to focus on polishing lots of "little things" in C# 
// before I tried to tackle the grandiose topic of metaprogramming. On the plus
// side, I expect it will be much more practical for IDEs to do refactoring 
// operations on EC# templates than D's mixins (although both of these are much 
// harder to handle than generics.)
//
// After designing features for EC# for a few weeks, I came to realize that C# is
// not ideal to serve as an extensible language:
// - its syntax is too ambiguous to allow users to add new operators and statements 
//   that feel like they were built-in from the beginning
// - Due to its many, many rules, the compiler must be complex and it may be hard
//   to provide a really smooth metaprogramming experience with it.
//
// It's on my TO-DO list to study languages like Racket
// (http://docs.racket-lang.org/guide/index.html); someday I would like to use
// or design a language that has the expressive power and flexibility of LISP (and 
// some of its simplicity), while being statically typed and syntactically rich 
// (like C# and other popular languages). Someday! But for now, I hope you will
// enjoy using Enhanced C#.

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
// This design ensures that you can still place your cursor on the word "var" in
// your IDE and still Go To Definition.

// NOTE: Implementation of the following feature will have low priority.
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
	public static implicit operator List<int>(MyClass c) { return new List<int>(); }
	
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
// I considered the parsing difficulties raised by allowing variable declarations 
// inside expressions, including the case above, and I decided that allowing such 
// declarations is indeed practical as an extension of C# as it exists today. 
// However, it does require extra work for the parser, which must perform a 
// lookahead operation every time it sees "A < B" at the beginning of a 
// subexpression (actually, this is already necessary, but in EC# it could get
// worse). And the above ambiguity is not the only one. Consider this code:
//
class A<X, Y> { ... }
class WhatDoesItMean {
	static int A = 1, B = 2, C = 3, D;
	void F()
	{
		BlowsYourMind(A < B, C > D = 4);
	}
	static void BlowsYourMind(params object[] x) { Console.WriteLine("{0} args", x.Length); }
}
//
// This code appears to be passing two arguments to BlowsYourMind at first,
// right up until the "= 4" part. This implies that the parser must look 
// ahead until it finds the "=" sign in order to tell whether the user is
// declaring a variable or just passing two arguments to a method. Plain C#
// rejects the above code because "C > D = 4" actually means "(C > D) = 4" 
// according to the precedence rules of C#; note that this is a semantic 
// error, not a syntax error, so the syntax is ambiguous. Nevertheless, I 
// could not think of a situation where code that looks like a variable 
// declaration, inside an expression, is legal in plain C#.
//
// Although none of these problems are necessarily deal-breakers, I decided to
// stay on the safe side and not support the standard variable declaration syntax. 
// EC# does allow variable declarations inside expressions, but they must start 
// with "var". You are allowed to specify a data type, but this must be provided 
// in addition to "var". Thus, the third line above must be changed as follows:
Trace.WriteLine((var List<Int32> result3) = new MyClass()); // OK

// The data type can be omitted provided that the data type is given in the
// same subexpression, or if the variable is declared as part of a tuple that
// is immediately assigned:
Trace.WriteLine((var result3) = new MyClass()); // ERROR: result3 must be immediately assigned ('var result3 =') when no data type is provided.
Trace.WriteLine(var result3 = new MyClass()); // OK
Trace.WriteLine((var result4, var result5) = (4, 5)); // also OK

// If you forget "var", EC# will still understand that you wanted a variable 
// declaration and remind you:
Trace.WriteLine(MyClass result3 = new MyClass()); // ERROR: remember that 'var' is required for inline variable declarations ('var MyClass result3')

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


// If you feel that (var String name = "John") is kind of a clunky way to declare a 
// variable, well, I agree. It's clunky in order to make it very clear what '<' means 
// in case you want to use a generic data type (we don't want a different syntax for 
// generic and non-generic data types).
// 
// EC# also provides a new non-clunky syntax for declaring write-once variables 
// inside expressions: the colon operator, a.k.a. the quick binding operator (:). 
// This operator is designed to be concise and convenient to use:
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
// If the variable name came first, as in "(parts:s.Split(',')).Count", then we 
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
// it appears you forgot this rule (because of the spacing).
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

// Note to self: remember this ambiguity in standard C#:
class Program
{
	static int A, B, C, D;
		
	static void Main(string[] args)
	{
		BlowsYourMind(A < B, C > (D)); // ERROR: Program.A is not a generic method
	}
	static void BlowsYourMind(params bool[] x) {}
		
	//static bool A<X, Y>(bool z) { return z; }
	//class B { }
	//class C { }
	//static bool D;
}
// The C# parser apparently has a special rule that says "if it could be a list
// of generic arguments, then assume it is." But this only happens if 'D' is in
// parenthesis; "BlowsYourMind(A < B, C > D)" does not produce an error. Thus 
// we see the requirement for lookahead to distinguish the cases. Also, keep
// in mind that A<B,C> could refer either to a type or to a method; in general
// the compiler must defer judgement until seeing what comes afterward, and if
// A<B,C> is followed by a "." then we've got a new kind of ambiguity in EC#, 
// now that generic properties are allowed (previously, in "A<B,C>.Something",
// A<B,C> was guaranteed to refer to a type).

////////////////////////////////////////////////////////////////////////////////
//              ////////////////////////////////////////////////////////////////
// constructors ////////////////////////////////////////////////////////////////
//              ////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// For convenience, EC# allows constructors named "new", to save typing. Also, the 
// [set] attriute can by applied to method arguments, which causes the value of 
// the parameter to be assigned automatically to the member variable or property 
// of the same name, when the method begins. Here is an example. The [set] 
// attribute is not limited to constructor arguments.
struct PointD
{
	double _x, _y;
	public new([set] double _x, [set] double _y) { }
	// Equivalent to public PointD(double x, double y) { _x = x; _y = y; }.
	// If a leading underscore is present in the names being initialized, it is 
	// removed from the perspective of someone calling the method, so that the
	// constructor can be called without an underscore in named parameters, e.g.
	//    var p = new PointD(x: 1, y: 2);
	// When you refer to _x or _y inside the constructor, you are referring to
	// the method's arguments and not the member variables this._x and this._y.
	// It is not legal to refer to just "x" and "y" (without an underscore), and
	// the names "x" and "y" are reserved within the constructor so you cannot 
	// declare variables with those names.
}

// For rapid prototyping, you can also declare fields (not properties) just by
// adding an accessibility to a parameter declaration.
partial struct Fraction
{
	// When you specify an accessibility such as "public", a new field is created
	// if it does not already exist. If the field already exists, the accessibility
	// specified must match the actual accessibility.
	public new(public int Numerator, public int Denominator) { }
	public new(public int Numerator) { Denominator = 1; }
	
	// Constructor parameters do not have to initialize anything implicitly.
	// This constructor, for example, does not create a new field.
	public new(string frac) {
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
//    can be turned off with [assembly:EcsConstructorWrapper(false)] or by applying
//    [EcsConstructorWrapper(false)] to a specific class or a specific constructor.
// 2. A constructor call such as "new Foo(...)" is considered equivalent to 
//    "Foo.new(...)" within EC# code, except when both a constructor and a 
//    static "new" method are defined and accessible. Regardless of which syntax 
//    you use, the EC# compiler may directly call the constructor, or it may 
//    invoke a static method called "new", depending on the situation. Generally,
//    if Foo is located in the same assembly then the constructor is called 
//    directly. If Foo is in a different assembly, the static method is called 
//    if it exists, otherwise the constructor is called. This allows the other 
//    assembly to change its constructor into a static method without breaking 
//    source or binary compatibility.
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
// "The default constructor in struct 'Path' is not compatible with the .NET generic constraint new()"

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
//        //////////////////////////////////////////////////////////////////////
// traits //////////////////////////////////////////////////////////////////////
//        //////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// A trait is basically a collection of methods for holding a unit of behavior.
// Multiple traits can be combined into a class. Traits are like mixins, but
//
// Like EC# templates which will be described later, traits only exist at compile-
// time.

trait Box<#Property>
{
	private Property.Type Value;
	{|
		quoted $code = statements;
		Console.WriteLine(code);
	|}
	(| int $x = 2 + expression |)
	
}

trait NotifyPropertyChanged : INotifyPropertyChanged {
	// - Could implement an interface by default
	// - But 
	
};



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
// the problem.

// Case 1: Covariance with an interface (for structs, this is the only case):
class ClonerA : ICloneable {
	// You write:
	public virtual ClonerA Clone() { return (ClonerA)MemberwiseClone(); }
	
	// This case is very straightforward, EC# just adds an explicit interface 
	// implementation if one is not already present:
	object ICloneable.Clone() { return Clone(); }
}

// Case 2: Covariance with a base class.
class ClonerB : ClonerA {
	// You write:
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
	// You write:
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
// not when using an implementation of IPoint, such as PointD.

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

// You can specify a method to use for string interpolation, if necessary, with an 
// assembly attribute:
[assembly:EcsInterpolatedStringFormatter(typeof(string), "Format")]
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
//                             /////////////////////////////////////////////////
// compile-time code execution /////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// Plain C# doesn't have a notation for binary number constants. Neither does EC#,
// but you can just write a function to fill that particular gap and call it at
// compile-time:
int   Binary(long binaryInDecimal)  { return Binary(binaryInDecimal.ToString()); }
int   Binary(string number)   { return checked((int)BinaryL(binaryNumber)); }
uint  BinaryU(string number)  { return checked((uint)BinaryL(binaryNumber)); }
ulong BinaryUL(string number) { return unchecked((ulong)BinaryL(binaryNumber)); }
long  BinaryL(string number) 
{
	if (number[0] == '-')
		return -BinaryConverter(number, 1, 0);
	else
		return BinaryConverter(number, 0, 0);
}
private long BinaryConverter(string number, int i, long total)
{
	if (i > 0 && i >= number.Length)
		return total;
	else if (number[i]:ch == '0' || ch == '1')
		return BinaryConverter(number, i+1, checked(total << 1) + (ch == '1' ? 1 : 0));
	else if (ch == '_' || ch == ' ')
		return BinaryConverter(number, i+1, total);
	throw new ArgumentException("Invalid character in binary number string");
}

const int TwentyFiveInBinary = Binary(11001);
const int NegativeTwentyFive = Binary("-11001");
const long EightGig = BinaryL("10_00000000_00000000_00000000_00000000");
readonly int[] DiamondPattern = new int[] {
	BinaryL("0000000010000000"),
	BinaryL("0000000111000000"),
	BinaryL("0000001111100000"),
	BinaryL("0000011111110000"),
	BinaryL("0000111111111000"),
	BinaryL("0001111111111100"),
	BinaryL("0011111111111110"),
	BinaryL("0111111111111111"),
	BinaryL("0011111111111110"),
	BinaryL("0001111111111100"),
	BinaryL("0000111111111000"),
	BinaryL("0000011111110000"),
	BinaryL("0000001111100000"),
	BinaryL("0000000111000000"),
	BinaryL("0000000010000000"),
	BinaryL("0000000000000000"),
};

// (The inspiration for this comes from D's compile-time "octal" function.)
//
// Eventually, I want EC# to be able to run any safe EC# code at compile-time.
// For version 1.0, EC# will be able to run the following:
//
// (1) the same kind of constant expression that plain C# can already evaluate, 
// (2) static functions that accept and return such constant values, do not use
// any other types internally, do not contain any loops, do not modify external
// state (fields), and do not use any 'exotic' statements such as switch, try/
// catch, "goto" or "on exit". These functions can have template arguments, but 
// cannot have generic arguments. They can throw a new exception that takes one 
// constructor argument (nothing more), which is printed as a compile-time error 
// with a stack trace.
// (3) string.Format(), and interpolated strings
// (4) if quoted expressions get implemented, CTCE will be able to quote arbitrary
// code, expand quoted code that follows rules (1) and (2), and use the 
// substitution operator.
//
// (1) refers to expressions involving strings and/or the primitive types sbyte, 
// byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, 
// bool, any enumeration type, or the null type. You won't be able to use classes 
// or structs, or access global variables. ToString() will be allowed on these 
// types, though; on strings, concatenation, indexing, the 'Length' property, and
// Substring() will be permitted.
//
// (2) says that you can run functions that accept a number of primitive arguments 
// and type arguments, do some manipulation or arithmetic on those arguments, and 
// return values. The functions cannot access any external state, but they can 
// call other functions that meet the same description. 'ref' and 'out' parameters
// will not be supported.
//
// (1) is more limited than C++11's constexpr functions because it does not allow
// operations involving structs or classes, but (2) is more powerful than C++ 
// constexpr because multiple statements including "if" statements and variable
// declarations will be allowed. In any case, EC# does not require any special 
// annotation on a function to make it callable at compile-time.
// 
// As you can see, compile-time code execution (CTCE) will be quite limited 
// initially. Loops are off-limits, but recursive calls will be allowed provided 
// that they do not nest too many levels deep (at least some hundreds of nested 
// calls should usually be allowed, though.) I will consider supporting one-
// dimensional arrays of primitives, however. Calls into compiled assemblies (e.g.
// Math.Abs()) will not be supported, except for the exceptions mentioned.
//
// Since the EC# compiler runs under .NET, it is theoretically not that difficult 
// to run arbitrary code, and that code could run just as fast as the final 
// compiled program, but EC# is based on NRefactory which does not support code 
// generation. Consequently, a full implementation of CTCE would be equivalent
// to writing a full compiler back-end, which would take a lot more time than I 
// have available.
// 
// You can force code to run at compile time using the static() pseudo-function, 
// which executes an expression at compile-time and returns the result as a 
// constant. Expressions that calculate values for enum values, "const" variables
// and "static if" are implicitly executed at compile-time and do not require the 
// static() function, but when you define constants, it is still recommended to 
// use it in order to make it clear that the code is not compatible with plain
// C#. Also, please note that readonly variables will use run-time function 
// evaluation by default.

// The main challenge created by CTCE is not actually "how do we run code at 
// compile time?" but rather "how can the IDE keep working well when CTCE exists?"
// And it isn't CTCE itself that creates this challenge, but rather the combination 
// of CTCE and static "if" clauses.
//
// On the plus side, once an IDE supports CTCE, a new opportunity appears: it can
// form the basis of code simulation features, as proposed in Bret Victor's 
// groundbreaking presentation "Inventing on Principle":
// http://www.i-programmer.info/news/112-theory/3900-a-better-way-to-program.html
//
// Consider this silly function:
long Slow(long x) { 
	return x > 0 ? Identity(x/2) + Identity(x/2) + (x & 1) : 0;
}
// This function computes the same thing as Math.Max(x, 0), but does so very 
// slowly (assuming the compiler does not memoize results or optimize the code--
// and by the way, it doesn't.) It is written this way so that it will not 
// overflow the compile-time call stack even if x is very large, but it will 
// waste a lot of time.
//
// The user can call this on an "if" clause, which controls the existence of a 
// type or a method:
string Convert(int x) if Slow(1000000000) == 1000000000
{
	return x.ToString();
}
decimal Convert(long x)
{
	return (decimal)x;
}

// So far, so good; the IDE should probably act like the "if" clause is true inside 
// the method itself, even if it is really false, otherwise no IDE features will 
// work; thus it does not need to evaluate Slow() yet. Notice that an "if" clause
// should be handled quite differently from an #if directive, because an #if 
// directive can completely change the meaning of the code:
class Outer {
	void f() {}
	#if BAM
	class Inner {
	#endif
		void g() {}
	#if BAM
	}
	#endif
}
// Given such drastic effects, an IDE is forced to know whether BAM is true or 
// false before it can even parse the code. And if BAM is false, providing code 
// completion within an "#if BAM" block is almost unthinkable. On the other hand,
// the compiler can meaningfully analyze most of the program without knowing 
// whether any of the "if" clauses are true or false.

// The first difficulty comes when the user writes this:
void UhOh()
{
	var x = Convert(//!!
}
// When it shows its overload list, should it include Convert(int)? It really needs
// to invoke Slow() to be sure. But, no big deal, it could just ignore the "if" 
// clause again and show Convert(int) no matter what. The greater difficulty is here:
void UhOh()
{
	var x = Convert(7);
	x.//!!
}
// This time the IDE must know the type of X (string or decimal), so it has no
// choice but to test the "if" condition. On the plus side, though, it only has to
// check this one single "if" clause. But what about...
//
var x = // user presses Ctrl+Space to show the list of all symbols
//
// Now there's no escape: to show a correct symbol list, the compiler must run
// the "if" clauses on almost all symbols that are in scope. Still, the good news 
// is, if the IDE doesn't have time to do that, it can degrade gracefully: just 
// assume true and let the user figure out that certain symbols are not really 
// available. However, the IDE will need some mechanism to terminate "runaway" 
// code, perhaps remembering which clauses or called methods are badly-behaved
// and run the well-behaved code first in the future.
//
// Assuming array support gets implemented, there's one other big challenge:
// CTCE will be able to use an unbounded amount of memory. As long as CTCE is 
// run by an interpreter, however, the interpreter could easily track memory
// usage and bail out with an error if it becomes excessive. In the distant
// future, when CTCE is accomplished via the same backend that compiles the
// actual program, memory usage may be harder to control, especially if arbitrary
// calls into external DLLs are allowed (how do we even detect that memory is
// allocated in such DLLs?) Memory control is far more important in the IDE than
// when formally compiling, since the IDE is always running and "does its thing"
// in the background, where it cannot even report problems to the user. It is
// simply not acceptable, ever, for the IDE to suddenly suck up 4 GB of memory
// because it tries to run a compile-time function that does something crazy.
//
// Stack overflows could also be a problem (StackOverflowException is uncatchable).
//
// All in all, the problems are not too serious, since pretending everything exists
// is usually just a minor nuisance to the user; IDE perfection can wait.
//
// One serious limitation of "assuming true" is related to aliases. Some aliases 
// will be ambiguous until the IDE executes the necessary "if" clauses:
alias X = String if Slow(1000000000) == 1000000000;
alias X = Stream if Slow(1000000000) == 999999999;
//
// Another important source of CTCE is "static if" statements, but these have less
// impact since they are isolated inside methods:
void IdeWorkout() {
	static if (Slow(1000000000) == 1000000000)
		int y;
	static else
		string y;
	y.// Why you make your IDE work so hard?
}
// Note that an IDE does not have to worry about static() expressions so much, 
// because an IDE only cares about the types of expressions, not their values. 
// It is not necessary to actually evaluate a static() expression to find out its type;
// but the type of any expression, static() or not, can be affected by "if" clauses
// and "static if" statements earlier in the method. An IDE also doesn't really 
// have to worry about typeof<> either, because it doesn't have to run 
// Slow(1000000000) to know that typeof<Slow(1000000000)> is long.
//
// The IDE only has to evaluate a static() expression if it wants to tell you the 
// result of the expression; this is separate from ordinary code completion.
//
// And once the IDE has evaluated an if clause or a static if, it immediately has to worry about when to re-evaluate the "if" clause. At the very least, the IDE could keep track of which methods are ever executed at compile-time and mark all the "if" clauses for re-evaluation every time any of those methods are changed. A better approach, of course, would be to save some kind of dependency graph that indicates which methods are called by which other methods and "if" clauses.
//
// In summary, CTCE + static "if" creates the following challenges for an IDE:
// 1. Showing the overload list for a single symbol
// 2. Showing the list of available symbols in a given context
// 3. Implementing the "Go to definition" command (but by "assuming true", the IDE
//    can just list all possibilities and let the user pick one; this UI will 
//    already exist anyway, since a plain C# "partial class" can have multiple 
//    definitions.)
// 4. Determining the data type of an expression, necessary for (1), (2) and (3);
//    unfortunately this will not work reliably without CTCE.
// 5. Reporting the values of constants
// 6. Deciding when to recompute values that depend on CTCE
//
// 7. CTCE also creates a difficulty for refactoring, including my favorite 
//    refactoring operation by far, the "rename" command. Consider:
//
static void Foo(int x) if Slow(1000000000) == 1000000000 { }
static void Foo(long x) { }

static void Main(string[] args)
{
	Foo(1); // User requests to rename this method
	Foo(1000000000000000000);
}
// The IDE really must figure out whether the "if" is true in order to figure out
// which method has to be renamed.
//
// But the really big difficulties with refactoring are caused not by CTCE but by
// templates. More on that later, on the section on templates.

// EC# predefines a boolean constant ECSharp.IsCompileTime, which is true if the
// code is being interpreted at compile-time. It can be used with a "static if"
// statement to select a different version of a function to compile at compile-
// time.


////////////////////////////////////////////////////////////////////////////////
//           ///////////////////////////////////////////////////////////////////
// contracts //////////////////////////////////////////////////////////////////
//           ///////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
// NOTE: Implementation of the following feature will have low priority.

// EC# will support a simple way to express requirements on the arguments and 
// return values of a method: the "in" and "out" clauses. For example:
VictimStatus KillBill(string victim),
	in (victim in ("Vernita", "O-Ren", "Elle", "Buck", "Bill")),
	out return == VictimStatus.Dead
{
	...
}

// "in" and "out" are two of the three clauses defined on methods in EC#; the third
// clause is "if" which is used to help define templates. The "out" clause 
// implicitly defines a variable called "return" that represents the return value 
// of the method, so the above example is checking that the return value is always 
// "VictimStatus.Dead".
//
// "in" is also a new operator that checks whether a value is in a set, so this
// example checks whether victim == "Vernita" || victim == "O-Ren" etc.
//
// By default, the "in" and "out" clauses are simply translated into calls to 
// Microsoft Code Contracts. So the above is translated to plain C# as
VictimStatus KillBill(string victim)
{
	Contract.Requires((victim in ("Vernita", "O-Ren", "Elle", "Buck", "Bill")));
	Contract.Ensures(Contract.Result<VictimStatus>() == VictimStatus.Dead);
	...
}

// The expression after "in" and "out" does not require parenthesis around it. 
// Instead, a comma is required to separate clauses when there is more than one.
// A comma is also allowed (not required) after the last clause. If you forget 
// the comma, the compiler will suggest adding one (if it can figure out that the 
// error is caused by a missing comma.) Likewise if you accidentally use a 
// semicolon instead of a comma, the error message will tell you to use a comma 
// instead.

// Contracts can appear on properties, but only on the "get" and "set" parts:
int Size
{
	get, out return > 0 { return _size; }
	set, in value > 0 { _size = value; }
}
// A comma is permitted (not required) after "get" and "set", just for the sake of 
// visual separation.

// Contracts can appear on auto-properties, too, but this forces EC# (instead of 
// the C# compiler) to choose a name for the backing field.
int Size { get; set in value > 0; }

// If there are multiple preconditions or postconditions separated by &&, such as
R Accumulate<T,R>(R seed, List<T> list, Func<R,T,R> reduce)
	in list != null && reduce != null
{
	foreach(T x in list) seed = reduce(seed, x);
	return seed;
}
// Then EC# will produce a separate check for each subexpression:
R Accumulate<T,R>(R seed, List<T> list, Func<R,T,R> reduce)
{
	Contract.Requires(list != null);
	Contract.Requires(reduce != null);
	foreach(T x in list) seed = reduce(seed, x);
	return seed;
}

// The following attributes control the method that is to be called to check each 
// precondition and postcondition.
[assembly:EcsContractRequires("Contract.Requires")]
[assembly:EcsContractEnsures("Contract.Ensures", "Contract.Result")]
// By default, the "in" and "out" clauses are simply translated into MS code 
// contract calls. [assembly:EcsContractEnsures()] can take one or two arguments;
// if it takes two arguments then EC# uses the syntax required by MS code 
// contracts, but if it takes only one argument then EC# assumes that the checker 
// is a normal method such as "Debug.Assert". After you've written
[assembly:EcsContractEnsures("System.Diagnostics.Debug.Assert")]
// an out clause is translated like this:
int Quadruple(int input)
	out return > input || (input <= 0 && return < input)
{
	return input * 4;
}
// becomes
int Quadruple(int input)
	out return > input || (input <= 0 && return < input)
{
	int @return = input * 4;
	System.Diagnostics.Debug.Assert(@return > input || (input <= 0 && @return < input));
	return @return;
}

// This transformation uses the same compiler machinery as the new "on success" 
// statement, except that you are given access to the return value ("on success"
// cannot do so because a "return" statement is not the only way to leave a block.)

// Initially, EC# contracts may be translated using simple string substitution of
// the ContractRequires and ContractEnsures attributes. This is tentative, and is
// likely to work differently in the end.

// D offers a theoretically sound contract system (except that its contracts vanish
// in release builds, making its contracts worthless for public APIs). 
// TODO: consider implementing D's system

// TODO: finish plan for contracts

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
// compile-time. I assume you're familiar with generic methods; now let's look
// at a template:
public ElType<L> Min<#L>(L list)
{
	var min = list[0];
	for(int i = 1, count = (int)list.Count; i < count; i++)
		if (min > list[i])
			min = list[i];
	return min;
}

int Min(int[] array) { return Min<int[]>(array); }

// The number sign # indicates that L is a template parameter. At first I intended
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
// ElType<L> to denote the type of elements inside L. ElType<L> refers to 
// another template (based on another EC# feature, aliases):
alias ElType<#L> = typeof<default(L).GetEnumerator().Current>;

// So ElType<L> is just another name for the type returned from
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
// The number sign is only allowed in the definition of the template parameter.
// It is not necessary or allowed anywhere else (except stuff like #if, of course.)
//
// Templates can accomplish more than ordinary generics can:
// 
// 1. Although there is only one template parameter, Min actually has access both 
//    to the list type AND the item type ElType<L>. In fact, in theory, a single
//    template parameter #C is enough to accomplish anything because C can act as
//    a bundle to reference any number of other types. This alone makes templates
//    more powerful than generics; I ran into a situation in C# recently where 
//    seven generic parameters were appropriate and it was just far too unweildy
//    to actually use seven (I picked two that seemed the most important, but this,
//    too, led to code that was somewhat unweildy.)
// 2. Templates can use all the functionality of a type, including operators; Min
//    uses the ">" operator, for example. This make templates far more useful for
//    numeric programming than generics.
// 3. Templates can contain "static if" statements, so you can easily write special 
//    code for certain data types.
// 4. Since templates are specialized at compile-time, a template specialized for a
//    given type will have the same performance as code written by hand for the 
//    same type. Standard .NET generics do not offer this guaratee; my performance
//    tests show that, depending on the circumstances, generics sometimes have the
//    same performance as hand-written code but they can be slower. Especially for 
//    number manipulation, templates have higher performance and are far, far 
//    easier to use.
// 
// The following example shows how templates can use "static if" statements that 
// make judgements about their parameters, using the "type ... is" and "is legal" 
// operators.
int ToInt<#T>(T value)
{
	// Check whether T is string. In general, you can't simply use "T is string" 
	// because normally the left-hand side of "is" is an expression, not a type. 
	// The "type" prefix is defined to force it to be interpreted as a type instead.
	static if (type T is string)
		return int.Parse(value);
	// The "is legal" operator checks whether an expression compiles without error.
	else if ((int)value is legal)
		return (int)value;
	// "type" and "is legal" can be combined.
	else if (type ElType<T> is legal)
		throw new InvalidOperationException("Can't convert a collection to an int! You fool!");
	// You can check that an expression is legal and has a certain type at the same
	// time using the "using" operator.
	else if ((value.ToInt() using int) is legal)
		return value.ToInt();
	else
		return Convert.ChangeType(value, typeof(T));
}
// The precedence of "is legal" is the same as "is". This operator introduces a
// very minor incompatibility with C#, since "is legal" is treated as a postfix
// operator even if there is a type called "legal" in scope. However, there's a 
// strong convention that types start with uppercase letters, so this is highly
// unlikely. If there is a type called "legal", the old meaning can be demanded 
// using "is @legal".
//
// --- sidebar ---
// I was going to define "is legal" as a prefix operator instead, called "valid"
// as in "valid (x + 1) * 2", but then I realized that "valid" is indistinguishable 
// from a method call if it is followed by "(". Any attempt to define a single-word 
// prefix operator will have the same problem if it is not already a keyword. 
//
// Next, I planned to use "compiles" as a suffix as in "x*x compiles", but I didn't
// want to make "compiles" a "real" keyword for fear of breaking existing code. 
// Then I realized that this is ambiguous too, because the expression "(x)compiles"
// looks like a cast that converts variable "compiles" to type x. I also got a 
// nagging suspicion that other ambiguities could exist (for instance, C# has no 
// postfix operators that are also infix operators, but if there were such an 
// operator, call it OP, "x OP compiles" could be interpreted as either 
// "(x OP) compiles" or as (x OP compiles), where "compiles" is considered to be a
// variable/property in the latter case.)
// 
// This has nothing to do with "is legal", but I also observed that infix operators 
// that are not keywords wouldn't work either. Let's say "infix" is a new operator
// that is not a keyword: then "(X) infix (Y)" could be parsed as a calling the 
// "infix" function and then casting the result to type X. This particular 
// ambiguity can be resolved by assuming "infix" is not an operator in this case,
// by assuming (X) is a cast. After all, if it is not intended to be a cast, why
// does it have parens around it? If you respond "because it encloses a lower 
// precedence operation, e.g. 1+1", I can counter "well, (1+1) cannot be mistaken
// for a cast - and if you just wanted to say X or X.Y, you didn't need to put
// parenthesis around it". The same reasoning leads the plain C# compiler to 
// assume that (x)(y) must be a cast and not a function call: "if you wanted to
// call a delegate called x, you didn't need to put parenthesis around it.
// Therefore you must have wanted a cast to type X."
// 
// However, consider the expression "X<Y> infix (z)". Now this ambiguity is more
// difficult to resolve. Here, "infix" could be interpreted as an operator that
// takes X<Y> as its left-hand argument, but it could just as easily be parsed as 
// ((x < y) > infix)(z), interpreting infix as a variable/property. Obviously, 
// this cannot be solved by requiring X<Y> to be placed in parenthesis, because 
// then it would be parsed as a cast!
// 
// Conclusion: it is not practical to add a new C# operator that is an identifier
// but not a keyword; the three operator types (prefix, postfix and suffix) are 
// all ambiguous. However, operators like "is legal" that require two words are 
// significantly less ambiguous. This is why C# has "yield return" instead of
// just "yield"; "yield (x)" looks like a function call. Neither of the two words
// necessarily need to be existing keywords, either, although I would point out
// that "from x" could be a variable declaration or the beginning of a LINQ query,
// depending on the context. The key thing is that C# has no "juxtaposition" 
// operator like, for example, Haskell: aside from variable declarations, "x y"
// has no meaning in C#, a fact we can use to define new operators without breaking
// backward compatibility.
// --- end sidebar ---
//
// When you use 'is legal', be careful not to make a spelling mistake or to leave
// out a 'using' directive. The compiler will allow such an expression and its value
// is always false. The compiler will issue a warning if a directly-named symbol 
// (such as 'A', 'C' and 'E' in "A.B<C.D>(E.F) is legal") does not exist (use a 
// qualified name to eliminate the warning); However, there is no warning for, say, 
// 'Int32.MaxVale is legal' (notice the misspelling) because it *could* exist, e.g. 
// someone might create a symbol called 'MaxVale' in a static class:
static class Int32 { public static readonly int MaxVale = 15; }
// Therefore, it is reasonable for another piece of code to test whether it
// exists, but the compiler cannot warn about a spelling error.

// Do not to forget the word "type" if you are checking for the existence of a type. 
// The expression "System.String is legal" is generally false, because 
// "System.String" is considered to be an expression, not a type, so the compiler 
// looks for a property or variable called "System.String", instead of looking for 
// the type "System.String". A warning is issued if the result of "is legal" is 
// false but the subject refers to a type that exists. If you did not intend the 
// expression to refer to a type, place the expression in parenthesis to eliminate 
// the warning. Since the phrase "type (String) is legal" is considered a syntax
// error, the compiler can assume that "(String) is legal" must be asking whether 
// there is a property or field named String, not a type.

// These operators aren't limited to templates or static ifs:
bool true1 = type string is object; // true, a string is an object
bool true2 = (default(Int32) using ValueType) is legal; // true, Int32 is a ValueType

// Note that there is no operator that tests whether a type exists and also tests 
// the identity of the type at the same time. If a type X may or may not exist, you
// must not write
static if (type X is legal && type X is string) { ... }
// If type X does not compile, the test "type X is string" produces a compile-time 
// error. You might expect "&&" to "short-circuit" the test so that the second 
// subexpression is never considered when the first subexpression is false, but 
// this not how the compiler works. Semantic analysis never short-circuits; the 
// entire expression is analysed, and only the evaluation process can short-
// circuit. After all, if you write the following expression in normal C#, it
// refuses to compile:
const bool X = (true == false && 9 + "one" == ten); // ERROR
// Instead, use the idiom ((default(X) using string) is legal), which causes the 
// compiler error to be swallowed if X does not exist. The only problem is that
// ((default(X) using string) is legal) can be true if X is not a string but is 
// implicitly convertible to a string; usually this is acceptable in practice.
// If you really want to check that one thing is derived from another thing or
// implements an interface, you could use a helper method:
void AssertIs<B,A>() where B:A {}
...
static if (AssertIs<X, string>() is legal) { ... }
// (and maybe someone clever will come up with a simpler approach.)

// "static if"s are not limited to templates, but they are limited to being inside 
// methods; see the section on "static if" below.

// Although templates are quite powerful, they are not meant to replace generics,
// because generics have their own advantages:
//
// 1. Templates only exist at compile-time, so unlike generics, one assembly 
//    cannot use a template defined in another assembly (at least not yet), and
//    you cannot create new specializations at runtime.
// 2. Templates cause compile-time code bloat: every new type that you use for 
//    L causes a new specialization of Min<#L>() to be created. Generics only cause 
//    run-time code bloat, so your DLL is generally smaller. Also, code bloat is 
//    limited when you use reference type parametes (in MS.NET, generics are 
//    specialized for each value type, but only once for all reference types.)
// 3. Generics provide a useful guarantee: instantiating a generic method or class 
//    never fails for any type argument that meets the generic constraints (the
//    where clause). In contrast, template parameters are "duck-typed" by deault,
//    as the "Min" example demonstrates, which means that instantiating a template 
//    will fail whenever you try to use a type that does not have the necessary
//    methods, operators or conversions defined on it. For example, Min(13) 
//    obviously would not work, but the error message tends to be confusing because
//    the error is reported inside Min, instead of at the call site.
//   
//    A more subtle example would be a list that only implements indexing through an 
//    explicit interface implementation. For example, one might naively expect the 
//    following code to work:
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
//    cannot be used directly on "list"). Presumably I will look for ways to solve
//    this problem, eventually.

// The current plan is that EC# will not support standard "where" clauses on 
// template parameters. Instead, "if" clauses are supported:
public ElType<L> Min<#L>(L list) 
	if (list[0] < list[0]) is legal && (int)list.Count is legal
{
	var min = list[0];
	for(int i = 1, count = (int)list.Count; i < count; i++)
		if (min > list[i])
			min = list[i];
	return min;
}

// This "if" clause tests whether (list[0] < list[0]) is a valid expression. It 
// does not actually run the expression, it merely tests whether it is meaningful.
// So if you try to call Min(7), the compiler will not try to compile Min<int> 
// because 7[0] < 7[0] is not a meaningful expression. Instead you will get an
// error message like "The template method 'Min<int>' cannot be created because
// it does not meet the constraint: "(list[0] < list[0]) is legal".
//
// The "if" clause, unlike the "if" statement, does not require parenthesis around 
// its argument, so in this example the expression does not have parenthesis around 
// it (the normal "if" statement cannot operate this way because its syntax would 
// be ambiguous.) The above clause is commonly expressed more compactly as
//
// 	if (list[0] < list[0], (int)list.Count) is legal
//
// using a tuple to combine two tests into a single test.
//
// It is usually a good idea to provide an "if" clause because someone else might
// want to overload your template with another template, to support other data
// types that your template doesn't support. Also, the error message is easier
// to understand when the 'if' clause returns false than when the template fails
// to compile.
//
// A very lazy approach is the special clause "if legal", which simply means
// "try to instantiate this template. If anything goes wrong, ignore this template
// and use another one." This approach is more generally more taxing for the 
// compiler and should only be used if either (1) you expect instantiation to
// almost always succeed or (2) the method is small, so it is cheap to attempt
// instantiation. Now, if a method with the "if legal" clause fails to compile
// and no other template works either, errors will be reported on all candidate
// templates. "if legal" can also be used on ordinary methods, but it's hard
// to think of a legitimate reason to use it.

// In plain C#, non-generic functions are given preferential treatment for 
// overloading purposes. So if you define
static T Overload<T>(T x) { return x; }
static long Overload(long x) { return x*x; }
// then Overload(5L) == 25L. Note, however, that Overload(5) is still 5; the 
// compiler's preference is weak.
//
// Likewise, non-template, non-generic functions are given priority over template
// functions. To accomplish this, the EC# compiler notices that the expansion of
// Overload<int> is "int Overload(int x)", which conflicts 

TODO

Perhaps ideally, plain methods would take priority 
// when they are overloaded with template methods. However, since template 
// methods are compiled to plain C#, this is not what happens; instead, plain
// methods and template methods compete on the same level, because template
// methods become plain methods upon conversion to C#. (it would have been 
// possible to implement different overloading behavior by giving them unique 
// names, but keeping the original name is preferable, since a template
// method, once specialized, might be called by plain C# or VB.)

// An ordinary function can also have an "if" clause, which acts like a "static 
// if" around the entire function ("static if" itself is not allowed outside 
// functions; see the "static if" section below for rationale.)
//
// Generic arguments and template arguments can be mixed, and the "if" clause 
// must come after any "where" clauses. For example:
void AddSquare<L, #N>(L list, N number)
	where L : IList<N>
	if ((number * number) using N) is legal
{
	list.Add(number * number);
}

// Note that template parameters cannot be *constrained* in "where" clauses; they
// can, however, be used to constrain generic parameters. Here, N constrains L, but
// a where clause cannot constrain N itself because the "where" clause specifies 
// constraints that are interpreted by the .NET type system. The .NET type system 
// does not understand templates or template arguments, so you cannot describe them 
// in "where" clauses; use an "if" clause to constrain template parameters. If "L"
// were a template parameter, the constraint would have to be written "if type L is 
// IList<N>" instead of "where L : IList<N>".
//
// Eventually, where clauses may be allowed to constrain template parameters, but 
// I want to point out that "if" clauses will never be able to constain generic 
// parameters. If L is a generic parameter and you write 
void AddSquare<L, #N>(L list, N number)
	if type L is IList<N>
// This clause always evaluates to false! That's because L has no constraints 
// declared on it, meaning it could be almost anything at run-time; and at compile-
// time, L only has the common characteristics of all types that L could be. So the
// "if" clause is really asking "is type L guaranteed to be IList<N>?" and the 
// answer is no. Another way to think about it is that when the "if" clause tests a
// generic parameter, it always produces the same result no matter what L turns
// out to be; in cases like this one, the result is always true or always false.
// Since the result is always true or always false, what's the point of using "if"
// on a generic parameter at all? In fact, most of the time there is no point, and
// you shouldn't do it.

// EC# also supports template properties:
bool IsFloatingPoint<#T>
{
	get { return (default(T) using double) is legal; }
}
bool IsInteger<#T>
{
	get { return (default(T) using long) is legal || (default(T) using ulong) is legal; }
}
bool IsNumericPrimitive<#T>
{
	get { return IsFloatingPoint<T> || IsInteger<T>; }
}
// I do not plan to support generic (as opposed to template) arguments, however.
//
// Please note that no C# code will be generated for these properties if they are
// only called at compile-time; for example,
const bool True = IsInteger<byte>;
// simply becomes
const bool True = true;
// in plain C#, so there is no need to generate plain C# code for IsInteger<byte>.

// One more thing. In C++ it is possible to use constant value template parameters
// (let's call them CVTPs), e.g. Foo<1234>. At first I was interested in supporting 
// a more powerful form of this, because while it is not used very often in C++, 
// a more powerful form is possible in D and used quite often there.
//
// However, it would require quite a lot of machinery. Firstly, parsing a template 
// argument list that contains arbitrary expressions is quite difficult because of 
// the dual meaning of the angle brackets and because it would require recursive 
// (stateful) lookahead in the parser. Secondly, it would probably be a huge amount 
// of work to introduce enough new functionality to make this feature worth the
// effort. CVTPs are not used a lot in C++ because you can't do that much with 
// them; but in D you can pass a lambda as a CVTP and have the compiler inline it,
// or you can pass a string like "struct Wow" as a template parameter and the 
// template could generate a data type called "Wow" for you. But these kinds of
// features are far beyond the available manpower (i.e. me). 
//
// In C++ I most often will pass a simple boolean to a template to enable or 
// disable a feature, and it is straightforward to simulate this using data types 
// rather than booleans. Sometimes I will pass an integer as a template argument to 
// indicate the size of a stack array or an array in a struct, but C# has minimal 
// support for constant-size arrays so it hardly applies in this context.
//
// But don't worry, Without CVTPs it is still possible to pass constant numbers, 
// strings and functions to a EC# template; it just takes a little more work. You
// just have to define a data type that contains the constants and functions you
// want to pass, then pass the data type itself to the template. For example,
// if I want to pass the constants 1234 and "Hello" to a template, I would do it
// like this:
struct Arguments {
	public const int Num = 1234;
	public const string Str = "Hello";
}
void Sender() {
	Receiver<Arguments>();
}

// Receiver() uses an "if" clause to document the arguments it expects to receive
void Receiver<#Args>() if (Args.Num, Args.Str) is legal
{
	Console.WriteLine("{0} {1}", Args.Num, Args.Str);
}

// The compiler will resolve any types that a template method's signature refers 
// to, and issue error messages if they don't exist or are ambiguous, even if 
// the template is never invoked or if the template uses "if false". For example, 
// the following template methods cause errors.
namespace NS1 { class Duplicated {} }
namespace NS2 { class Duplicated {} }
namespace NS3 {
	using NS1;
	using NS2;
	asdfjkl<T> BadTemplate1<#T>() if false {} // ERROR, type 'asdfjkl<T>' undefined
	T BadTemplate2<#T>(Duplicated d) if false {} // ERROR, type 'Duplicated' ambiguous
}
// Types may be resolved and errors may be issued inside the template's body, too.
// I am undecided about whether to enforce this rule for "if legal", however.
// Certainly, since "if legal" is allowed on a non-template method, semantic 
// errors within the method body should not be issued. I am undecided, however,
// about whether the signature of such a method should be able to refer to 
// undeclared types, although certainly it will be allowed to refer to types that 
// have an "if" clause that turns out to be "false".

// Using templates shouldn't mean that your IDE doesn't help you. When you write
void Template<#T>(T x)
{
	x.//??
}
// What can the compiler possibly offer you in its code completion box? T could be
// anything! It really needs more information before it can help you very much,
// but at least it can offer the standard public methods of T such as ToString().
//
// TODO

/*//
// If there is an "if" clause like "if type T is Regex || type T is 
// string", a smart IDE could notice this and perhaps offer the members of both 
// Regex and string, or maybe the intersection of those members. Just how smart
// should the IDE be, though?
//
// I would propose that an IDE 
// 1. scan for data types mentioned in the "if" clause, such as (type T is U)
//    and (default(T)(using int) is legal). Note that the "if" clause may 
//    reference types that contain a template parameter, such as 
//    (type T is IComparable<T>),
*/



////////////////////////////////////////////////////////////////////////////////
//                //////////////////////////////////////////////////////////////
// template types //////////////////////////////////////////////////////////////
//                //////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# supports template types, too. The syntax is as you would expect:
struct Point<#N>
{
	public N X, Y;
}

// EC# allows an 'if' clause on a template structure, and it actually allows
// a kind of "overloading" for template data types:
class Commentary<#N> if IsNumeric<N>
{
	static string Praise = "I'm glad you went with a number, son! Excellent choice!";
	public override string ToString() { return Praise; }
}
class Commentary<#N> if type N is string
{
	static string Concern = "Now hold on, how you gonna do any math with a string?";
	public override string ToString() { return Concern; }
}

// Only one version of a non-partial class is allowed to have an "if" clause that 
// is true. It is legal for all "if" clauses to be false, as long as no one tries 
// to use the type.

// "if" clauses don't have to be mutually exclusive if you use the
// "partial" keyword. Using "partial", you can define some code that is only 
// available for certain template parameters, and other code that is available 
// for all template parameters. For example:
public partial struct Point<#T>
{
	public T X, Y;
}
public partial struct Point<#T> if type T is float or type T is double
{
	public T Length
	{
		get { return (T)Math.Sqrt(X*X + Y*Y); }
	}
}

// In this case, the 'Length' property is only available when N is float or double,
// but X and Y are available no matter what T is. For this example, it is slightly 
// simpler to express the same constraint using an "if" clause on the Length 
// property itself:
public struct Point<#T>
{
	public T X, Y;
	public T Length, if type T is float or type T is double 
	{
		get { return (T)Math.Sqrt(X*X + Y*Y); }
	}
}
// An "if" clause can be placed on the property itself or on the getter and
// setter individually (it is not permitted to put an "if" clause on both the 
// property itself and on the getter or setter, however.)
//
// A comma is allowed between the property name and the "if" clause, to provide
// adequate visual separation between the two.

// If an "if" clause is false, it does not make a difference whether the type 
// guarded by the clause is marked "partial" or not. The compiler does not care.

// Template types must be represented in plain C# somehow, and in fact it's 
// desirable that plain C# code be able to consume instantiated templates. 
// 
// The "Point" type is a perfect example, because it is useful to offer a series
// of point types with different kinds of coordinates: "int" coordinates, "float"
// coordinates, "double" coordinates an so on. And of course we would like to be
// able to "export" these different types of points so that other .NET languages,
// such as plain C# and Visual Basic, can use them.
//
// For practical purposes, it is useful to have both "Point" and "Vector" types.
// A "point" is an absolute location in space, and a "vector" represents the 
// difference between two points. It does not make any sense to add two points,
// together, but it makes sense to subtract two points (to make a vector), add
// two vectors (to make another vector), or add a point to a vector. These data
// types should offer several other common operations too, such as "rotate 90 
// degrees", "negate", "dot product", "cross product", "scale", etc.
//
// In plain C# it is a big pain in the butt to support points with several 
// different types--you have to copy and paste the definition of a "Point" and
// a "Vector" and change the data types on each copy. And you have to remember,
// every time you add a feature to one Point, you must add it to the others. And
// don't forget to write all the conversion operators between different kinds of
// points.
//
// In EC# it's easy. Write one template, then create aliases for the type. Each
// alias needs the [DotNetName] attribute, which designates an alias as the 
// ".NET name" for the type. If the alias is public, then all members of the
// template type are generated automatically (by default, template members are 
// not instantiated in plain C# unless they are somehow used by run-time code; 
// this default can be overridden by placing the [GenerateAll] attribute on the 
// template type.) Here is a borderline-useful example:
using ECSharp;
namespace MyPoints
{
	[GenerateAll]
	public partial struct Point<#T>
	{
		using P = Point<T>;
		using V = Vector<T>;
		public new(public T X, public T Y) { }
		public static explicit operator V(P p) { return new V(p.X, p.Y); }
		public static explicit operator P(V p) { return new P(p.X, p.Y); }
		public static P operator+(P a, V b) { return new P(a.X+b.X, a.Y+b.Y); }
		public static P operator-(P a, V b) { return new P(a.X-b.X, a.Y-b.Y); }
		public static V operator-(P a, P b) { return new V(a.X-b.X, a.Y-b.Y); }
		[AutoGenerate]
		public explicit operator Point<U><#U>(P p) if !(type U is T)
			{ return new Point<U>((U)p.X, (U)p.Y); }
	}
	
	[GenerateAll]
	public partial struct Vector<#T>
	{
		using V = Vector<T>;
		public new(public T X, public T Y) {}
		public static V operator+(V a, V b) { return new V(a.X+b.X, a.Y+b.Y); }
		public static V operator-(V a, V b) { return new V(a.X-b.X, a.Y-b.Y); }
		[AutoGenerate]
		public static explicit operator Vector<U><#U>(V p) if !(type U is T)
			{ return new Vector<U>((U)p.X, (U)p.Y); }
	}

	[DotNetName] public alias PointI = Point<int>;
	[DotNetName] public alias PointL = Point<long>;
	[DotNetName] public alias PointF = Point<float>;
	[DotNetName] public alias PointD = Point<double>;
	[DotNetName] public alias VectorI = Vector<int>;
	[DotNetName] public alias VectorL = Vector<long>;
	[DotNetName] public alias VectorF = Vector<float>;
	[DotNetName] public alias VectorD = Vector<double>;
}

// It is not required to define a DotNetName for a template type. If you do not
// define one, one will be generated on-demand using a partially-predictable
// naming system, e.g. Point<double> will come out as "Point__D", where "D" is 
// predefined shorthand for "System.Double". Other types will be abbreviated 
// also, with (of course) logic to avoid using the same name for different types.

// This example uses a few interesting EC# features:
// - "using" statements make the code shorter, as "Point<T>" and "Vector<T>" look
//   visually noisy when repeated ad-nauseam.
// - "public new" defines the constructor; "public T X" and "public T Y" create
//   and initialize public fields named X and Y.
// - The conversion operator begs for a double take:
//   
//      [AutoGenerate]
//      public explicit operator Point<U><#U>(P p) if !(type U is T)
//
//   Remember that a normal template has a list of template arguments after the
//   name of the function. However, the name of a conversion operator is basically
//   "operator DataType". In this case the data type is Point<U>, so, logically,
//   the name of the method is "operator Point<U>" and the list of template 
//   arguments, "<#U>", comes right afterward: hence "operator Point<U><#U>". You
//   have never seen anything like this in plain C# code because C# does not allow 
//   a generic method to serve as a conversion operator. EC# does not allow that
//   either, but you can define a template method that serves as a conversion 
//   operator (in other words, you can only use template arguments, not generic 
//   arguments).
//   
//   You are not allowed to define an identity conversion (i.e. from Point<T> to 
//   Point<T>), so the check "if !(type U is T)" ensures that U and T are not
//   the same data type.
//
//   [AutoGenerate] is a special compile-time attribute that directs the EC# 
//   compiler to generate specializations for all types U that are relevant to
//   this particular method. More specifically, [AutoGenerate] looks at the 
//   return types and parameters of the method to see how U is used (in this 
//   case the method returns Vector<U>) and then it instantiates the template 
//   automatically for all types U for which Vector<U> is defined. Thus, a 
//   series of conversion operators will be generated to allow conversion 
//   between every kind of Points and Vectors that exists in the assembly. 
//   TODO: add a parameter to determine handling of failed specializations.

// You can define a type that is both a generic type and a template type at the
// same time. For example, the following (valid, but useless) type
class Foo<A, #B> where A : IComparable<B>
{ 
	int Cmp(A a, B b) { return a.CompareTo(b); }
}
[DotNetName] public alias FooI<A> = Foo<A, int>;
//
// produces the following plain C#:
//
class FooI<A> where A : IComparable<int>
{ 
	int Cmp(A a, int b) { return a.CompareTo(b); }
}

// Templates and dynamic linking
//
// In EC# 1.0, templates will be completely erased from the output assembly: the
// template will be expanded for each type parameter with which it is used, and
// the original template will be gone. Since EC# merely compiles down to C#, it
// seems like it must work that way. However, it's not inconceivable that someday 
// templates that are declared "public" or "protected" could be saved in the 
// output assembly, so that when you reference that assembly from another EC# 
// project, you can use the template. In this section we will briefly consider
// this possibility and others.
//
// Aliases, another EC# compile-time concept, also disappear from the output.
//
// Of course, this limitation means that you can't distribute a "template library"
// in the form of a DLL. That doesn't mean that EC# can afford to completely 
// ignore dynamic linking, however. Let's say that a class library "A" defines the
// above template for Vector<T> and it creates specializations VectorI and VectorD.
// Now, a user of the class library wants to use the very same template in his
// program, this time with an additional instantiation VectorF. He also wants to
// add a scaling operator "*".
//
// So, he adds the same source file used in "A" to his project "B", and then 
// writes a new source file that says:
namespace MyPoints {
	[DotNetName] public alias PointF = Point<float>;
	[DotNetName] public alias VectorF = Vector<float>;
	
	public partial struct Vector<#T> {
		// Multiplies a vector by a scaling factor.
		public static MyVector<T> operator*(Vector<T> a, T factor) {
			return new MyVector<T>(a.X * factor, a.Y * factor);
		}
	}
}

// Now, here's the thing. It's really, really useful if the types in A and B unify.
// They don't have to--the compiler could consider A's VectorI to be completely 
// unrelated to B's Vector<int>, and that is how EC# will work at first, because
// unification might take a lot of work to implement. But it would be quite
// helpful if the compiler could somehow pretend that the two types are the same 
// type, and specifically that VectorF, VectorI and VectorD are all in some sense
// the same type even though they come from two different assemblies.
//
// Suppose library A defines a function GetVectors() that returns a VectorI[] 
// array. Then library B should be able to do
//
Vector<int>[] array1 = A.GetVectors(); // are A's VectorI and B's Vector<int> the same?
Vector<int>[] array2 = array.Select(v => v * 2).ToArray(); // Scale the vectors!
//
// Right? Well, it seems to me that this should be possible, at least kind-of: if 
// a template was instantiated in A, EC# could just use that one when the "same"
// template (meaning, the template with the same name, in the same namespace, and
// the same type parameters) is used in B. Some metadata would be required, of 
// course, so that definition of VectorI in A is used where possible. For instance, 
// A's VectorI might look like this when converted to plain C#:
[Was("MyPoints.Vector<#T>", 0x1F4ABB01, typeof(int))]
public partial struct VectorI
{
	public int X;
	public int Y;
	public VectorI(int X, int Y) {
		this.X = X;
		this.Y = Y;
	}
	public static VectorI operator+(VectorI a, VectorI b) { return new VectorI(a.X+b.X, a.Y+b.Y); }
	public static VectorI operator-(VectorI a, VectorI b) { return new VectorI(a.X-b.X, a.Y-b.Y); }
	
	[Was("operator Vector<U><#U>", 0x32112C28, typeof(double))]
	public static explicit operator VectorD(VectorI p) 
		{ return new VectorD((double)p.X, (double)p.Y); }
}
// The [Was] attribute would inform the EC# compiler that "VectorI" came from a 
// template called "Vector<#T>" in namespace "MyPoints". The integer would be 
// some sort of hashcode derived from Vector<#T>, so that the compiler can tell
// whether the template definition in B is "the same" as the definition in A, and
// typeof(int), of course, refers to the template parameter that has been 
// substituted into the definition of the struct.
//
// While compiling B, EC# would scan A and save all the [Was] attributes for later.
// Then, whenever some code in B uses Vector<#T> with some specific T, EC# could 
// check: does A have a [Was] instance named "MyPoints.Vector<#T>" for this T? If 
// so, the compiler can simply reference the type in A rather than generating the 
// template in B.
//
// There is an obvious problem in this example: B attempts to add new stuff to 
// Vector<T>! Clearly, this is not possible in general. B's Vector<T> can't 
// possibly unify with A's VectorI if B adds 13 new fields to Vector<T>. On the
// other hand, in this case B is merely adding a static member (an operator,
// mind you, which is rather special), and in theory this could be supported by 
// generating some special static class to hold it. However, I'm inclined to think
// that the compiler shouldn't have to go to great lengths to allow users to make
// changes between DLLs: it's okay if the compiler simply responds "screw you, I
// can't do that! The template definitions have to match, asshole!" After all, 
// there are other ways to extend types that don't give the compiler a headache.
// B could define operator* as a global operator, or it could define an alias 
// MyVector<#T> with the new operator in it. No need to ask the compiler to work
// miracles.
// 
// The really big problem here is "operator Vector<U><#U>", for three reasons.
//
// 1. It's a template. This implies that B can create new instantiations of it.
//    But B is using VectorI in A, and A is immutable.
// 2. It's got the [AutoGenerate] property on it, which means that even if B
//    doesn't ask for new instantiations, the compiler should create one
//    automatically for each Vector<U> that exists. B creates a new version
//    of the type, Vector<float>, so a corresponding conversion operator from
//    Vector<int> to Vector<float> should be created. But again, A is read-only!
//    Where can it be created?
// 3. It's an operator. In plain C#, an operator has to be "relevant" to the
//    type in which it is located; so the conversion operator VectorI -> VectorF
//    must be located either in VectorI or VectorF. Since the compiler is 
//    creating VectorF in B, it could put the operator there. But this is not
//    a solution in general. Imagine that B references two assemblies, A and 
//    A2 (and A does not reference A2 or vice versa); A defines VectorI and 
//    VectorD while A2 defines VectorF. EC# can't change A or A2, but it still
//    has to generate the conversion operator. That said, EC# already plans
//    to support conversion operators that are defined outside the types that
//    they apply to; perhaps the same mechanism can somehow handle this problem.
// 
// Evidently, EC# needs some standard way to generate "new" methods in types in
// referenced assemblies. Obviously, this would be limited to static and non-
// virtual methods (including properties and operators), i.e. methods that don't 
// change the runtime representation of the type. (True, reflection won't report
// the added methods, but nothing can be done about that, and I don't mind; the
// important thing is to provide source-level compatibility. Some other EC# 
// features are not understood by .NET reflection either.)
//
// To support dynamic linking of templates, some other scenarios need to be 
// considered:
// - B may reference two assemblies A1 and A2 that define exactly the same data 
//   type, e.g. both A1 and A2 define Vector<int>. Then what? Even if the type
//   definitions are identical, .NET does not support structural typing: A1's
//   Vector<int> cannot possibly unify with A2's, period, even if they have the 
//   same name and namespace. Uh-oh.
// - A's Vector<#T> may have the wrong hashcode, indicating it is different 
//   from B's. Then what?
// - A could define a template type Outer<#T> that contains another template type
//   nested within it, Inner<#U>. Library A defines an Outer<int> but it does not
//   use Inner<U> at all. Then B tries to access Outer<int>.Inner<string>. Now
//   what?
//
// I don't have all the answers yet. Comments welcome.
//
// I know a simple way that A could carry around its templates, eliminating the
// need for B to have its own copy of the templates: EC# could provide the option 
// to dump templates to assembly attributes in the form of source code embedded 
// in a string. For example, A could contain:
[assembly:EcsTemplate(@"
using ECSharp;
namespace MyPoints
{
	[GenerateAll]
	public partial struct Point<#T>
	{
		using P = Point<T>;
		using V = Vector<T>;
		public new(public T X, public T Y) { }
		...
	}
}
")]
// While compiling B, the compiler could pick up the template from A and use them
// to create new instantiations such as Vector<float>, or even just to consume
// the existing instantiations in an elegant way without creating new ones. For
// example, B could define a method that takes a "Vector<T>" as an argument; in
// order to call this method with a VectorI, the compiler must first understand 
// that A's VectorI represents Vector<int>.
//
// And maybe there is a better way, perhaps involving binary resources. I never 
// figured out how resources work in .NET though; where is it documented?
//
// So, in summary: EC# will not support templates across DLL boundaries at first.
// It would be feasible to support it, but it could take a lot of work. And the
// lack of structural typing is a fundamental limitation of the CLR.
//
// Don't worry, although EC# will not support type unification initially, there
// is a workaround. So let's review: library A defines VectorI, and VectorD, and B 
// wants to be able to refer to those types as Vector<int> and Vector<double> while
// defining a new version, Vector<float>. There is a way to do this! Here's how:
//
// 1. B uses the same template definition that A uses, but with a small change:
//    the namespace MyPoints is changed to MyPointsB.
// 2. B then defines the following aliases:
namespace MyPoints {
	alias Vector<#T> = MyPoints.VectorI if AreSame<T, int>;
	alias Vector<#T> = MyPoints.VectorD if AreSame<T, double>;
	alias Vector<#T> = MyPointsB.Vector<T> if !AreSame<T, int> && !AreSame<T, double>;
	private bool AreSame<#A,#B> { 
		get { return type A is B && type B is A; }
	}
}
// There you go! MyPoints.Vector<T> is now defined as an alias for A's VectorI or
// VectorD if T is int or double, otherwise it is an alias for MyPointsB.Vector<T>.


// One more thing. If all the "if" clauses on a type X are false, the type name 
// is still considered visible, although it cannot be used. This can cause 
// ambiguity if there is another type X in a different namespace, e.g.
namespace N1 { class X {} }
namespace N2 { class X if false {} }
namespace N3 {
	using N1;
	using N2;
	
	X myX; // ERROR, 'X' is ambiguous. Could be 'N1.X' or 'N2.X'.
	void method(X x) { } // ERROR, 'X' is ambiguous.
	void Error() { X xx = new X(); } // ERROR, yeah, it's still ambiguous.
}

// Note that these errors do not occur if the two X classes are in the same 
// namespace, because the two versions "unify" into a single class in the type 
// system.
//
// X is still visible for the following reasons:
// - it allows type resolution to work most of the time without any compile-time
//   code execution (which is potentially expensive to evaluate in an IDE). Note
//   that CTCE is still required in case the code says something like X<Y>.Z 
//   where X<T> is an alias with an "if" clause: aliases form a sort of barrier 
//   that cannot be crossed without running the "if" clause. If it were not so,
//   two mutually-exclusive definitions of the same alias in the same namespace 
//   could conflict too easily with each other even though one of them is disabled.
// - it allows template declarations to be mostly understood without knowing what 
//   type will be substituted for those arguments. For example, given the template
void Template<#T>(X<T>) { ... }
//   if type X<T> has an "if" clause, its existence may depend on the value of T.
//   By treating X as always visible, the IDE can provide a working "go to 
//   definition" command for X, without knowing what T is.


// For EC# 1.0, I do not plan to support templates of enums or delegates, except
// non-template enums and delegates that are embedded inside templates.

// Footnote: the following should be legal:
class X if true {}
struct X if false {}
// Making it legal means that the "if" clauses must be evaluated in order to 
// figure out whether X is a struct or a class. However, it only rarely matters 
// (at compile-time) whether a given type is a class or a struct.

////////////////////////////////////////////////////////////////////////////////
//                     /////////////////////////////////////////////////////////
// template namespaces /////////////////////////////////////////////////////////
//                     /////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// A namespace template is a special kind of namespace that only exists at 
// compile-time. It is useful when you want to write modules that are somehow
// customizable, but you want to avoid the hassle of putting template parameters
// all over the place, and you'd rather not use traditional dependency injection
// and interfaces either--maybe because the dependency injection is somehow
// inconvenient, or maybe interfaces don't provide enough speed or power.
// 
// By putting the parameters on the namespace instead of the individual classes 
// within a namespace, you no longer have to think of your classes as "template" 
// classes. Within the namespace, they look and act like normal classes; outside 
// the namespace, you typically bring in a template namespace with a "using" 
// statement so that, again, the template arguments only have to be given once,
// not repeated over and over, and the classes inside the namespaces are used
// as if they were normal non-template classes:
namespace Calculus<#num> if IsNumeric<#num>
{
	num[] Integrate(num[] input, int C)
	{
		var output = new num[input.Length];
		num sum = C;
		for(int i = 0; i < input.Length; i++)
			output[i] = (sum += input[i]);
	}
	num[] Differentiate(num[] input)
	{
		var output = new num[input.Length - 1];
		for(int i = 0; i < output.Length; i++)
			output[i] = input[i+1] - input[i];
	}
}
//
// elsewhere...
//
using Calculus<double>;

void UsesCalculus()
{
	double[] results = Integrate(new double[] { 1,1,1,2,5,10,5,2,1,1,1 }, 0);
}

// As you can see, the contents of the namespace (Integrate and Differentiate)
// do not need any special syntax to indicate that they are templates, nor are
// template parameters required to call them, and when you type "Integrate(",
// the IDE will show you the signature "Integrate(double[] input, int C)" with
// no hint that it is a template. Thus, it is fairly easy to 'templatize' 
// existing code to make it more reusable.
//
// Again, a clear motivation for template namespaces comes from numeric code. 
// Consider the open-source polygon math library called "Clipper"; this library 
// is hard-coded to work with "long" (Int64) coordinates, but some users really 
// want to use floating-point coordinates, some want Int32 coordinates, and still 
// others might even want fixed-point coordinates. Rather than inserting template 
// parameters on each class individually, it would be simpler to insert a single 
// template parameter "coord" at the namespace level to indicate the coordinate 
// type to use throughout the library.
//
// Like normal namespaces, namespace templates are always 'partial', i.e. you can 
// define more than one namespace block named Calculus<#num>; the 'if' clause can
// be different on each one (just as for partial types).
//
// If there were no template namespaces, an alternative approach would be to 
// require the user to define an alias in a separate source file, to indicate
// the desired coordinate type:
public alias Coord = long;
//
// But this would force the program to contain only a single implementation of
// the clipper library. What if somebody want to use both the "long" clipper 
// library and the "double" clipper library in the same program? Template 
// namespaces solve this problem.
//
// In order to control the conversion to plain C#, the alias statement is allowed 
// to refer to a namespace, and you can use DotNetName on it:
namespace Calculus {
	[DotNetName] alias D = Calculus<double>;
	// The syntax for this kind of alias is identical as for a normal alias, but
	// it is quite special since it does not define a type name like a normal  
	// alias. You are not allowed to declare any interfaces or include a braced
	// block for this kind of alias. Also, this kind of alias must not have
	// type parameters.
	//
	// This example creates a real namespace called Calculus.D to hold the 
	// specialized contents of Calculus<double>. A couple of things to note:
	// - As far as the compiler is concerned, the "Calculus" namespace is 
	//   totally separate and unrelated to "Calculus<#num>", just as it considers
	//   Tuple<T1> separate from Tuple<T1,T2>, for example.
	// - You cannot write "alias Calculus.D" at global scope, because no support
	//   has been written for this syntax. That's why "alias D" is placed inside 
	//   "namespace Calculus".
	//
	// Of course, the alias is accessible from EC# code as well as C# code; 
	// Calculus.D will always be a synonym for Calculus<double>.
}

// As with generic types, template namespaces can be "overloaded" based on the 
// number of type parameters; in addition, you can effectively specialize a
// namespace template by writing multiple namespace blocks with different "if"
// clauses. If you try to instantiate a namespace and the "if" clause fails on
// all of the namespace blocks, the compiler will tell you why the failure 
// occurred in each "if" clause.

////////////////////////////////////////////////////////////////////////////////
//           ///////////////////////////////////////////////////////////////////
// static if ///////////////////////////////////////////////////////////////////
//           ///////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// As shown earlier, "static if" is an "if" statement that is evaluated at compile-
// time, before its contents are evaluated. It can have an "else" or "else if" 
// clause, optionally preceded by "static". The contents of the block must be
// syntactically valid, not necessarily semantically valid:
string StaticIfExample()
{
	static if (true == false)
		ten = "seven" + 3;
	static else if (true)
		return "This method is useless! It is for illustration only.";
	static else
		this.Makes.Little.Sense();
}

// Here, only the middle branch is fully compiled. The other two branches are 
// basically meaningless, but their syntax is valid so the compiler allows them.
// It's like how the sentence "Homonyms eats fish that prioritize" is correct
// English, but meaningless; so all branches must be valid statements, but need 
// not have meaning.
//
// The word "static" can be omitted after the first "static if"; please note that,
// unlike a normal if statement, the static if statement recognizes a difference
// between "else if (foo) {...}" and "else { if (foo) { ... } }". The first "if"
// is a compile-time if; the second one is a run-time if.

// It might be possible to support "static if" outside functions; after 
// all, the D language already does. The main problem with "static if", given that 
// EC# can run code at compile-time, is a scenario like the following:

const int C1 = Overloaded(3); // In D, the value of C1 is 3

int CallFunction(int x) { return (int)Overloaded(x); }
long Overloaded(long i) { return i*i; }
static if (CallFunction(3) != 3)
{
	int Overloaded(int j) { return j; }
}

const int C2 = CallFunction(3); // But what is the value of C2?

// Here you can see the subtle problem. The "static if" statement calls 
// CallFunction(), which calls Overloaded(). Since the contents of the "static if"
// block have not been analyzed yet, CallFunction() calls the only version of 
// Overloaded() that exists, the one that takes a "long" argument. This returns 9,
// so the second version of Overloaded() comes into existence. In D, the value C1 
// is evaluated after the "static if" statement ("static if"s are evaluated as 
// soon as possible, so their contents can be used from other modules and from 
// above the "static if" statement itself); therefore, C1 has the value 3.
//
// That's all fine and makes sense. Okay, there is a minor paradox, but frankly
// it doesn't bother me--not yet. The real problem is what happens when 
// CallFunction() is called a second time. Logically, since C1 has the value 3,
// C2 should also have the value 3, since it is evaluated after the "static if"
// statement. As of this writing, however, C2 actually gets the value 9, because
// the compiler believes that the meaning of "CallFunction" has already been
// resolved: it does not reconsider what CallFunction(3) means in light of the
// new version of Overloaded() that has been created.
// 
// What really bothers me is this: if you change "static if (CallFunction...)"
// to "static if (Overloaded...)", then the value of C2 changes from 9 to 3!
// This I don't like, because it's a kind of "spooky action at a distance"; it
// it not obvious that calling the wrapper function "CallFunction" instead of 
// just calling "Overloaded" directly would change the value of a "constant"
// somewhere else in the program.
//
// Because of this, I do not want to support "static if" at file scope unless
// someone can think of a sane resolution to this paradox. "static if" inside
// a function, on the other hand, is safe because it cannot affect the 
// arguments to the function, and therefore it cannot affect symbol resolution
// or overload resolution. "static if" can affect the return type (for functions
// declared with "def") but the difference is that the compiler can detect
// dependency cycles in this case and halt with an error. On the other hand,
// it seems more difficult for the compiler to detect problems with "static if"
// when it is used at file scope. Even if we solve the particular problem
// described here, there are other potential semantic problems, too.
//
// Luckily, the "if" clause comes to the rescue. An "if" clause at the end of a 
// template function is really a "static if" in disguise. The above example 
// could be written with an if clause, as follows:

const int C1 = Overloaded(3); // ERROR

int CallFunction(int x) { return (int)Overloaded(x); }
int Overloaded(long i) { return (int)(i*i); }
int Overloaded(int j) if (CallFunction(3) != 3) { return j; }

const int C2 = CallFunction(3); // ERROR

// The "if" clause doesn't have the same problem because, from the compiler's
// perspective, the method "int Overloaded(int j)" is created before the if 
// clause is evaluated. So Overloaded(int) is visible from all call sites, not
// just from call sites that are considered after the "static if" is considered.
// 
// 1. C1 calls Overloaded(3), which clearly matches Overloaded(int) better than 
//    Overloaded(long). However, the compiler will not actually call 
//    Overloaded(int) unless the "if" clause is satisfied.
// 2. The "if" clause calls CallFunction(3) which calls Overloaded(x). Again,
//    this matches Overloaded(int) better than Overloaded(long), and the compiler
//    would have to evaluate the "if" clause to determine whether Overloaded(int)
//    can actually be invoked.
// 3. The compiler knows that it is already in the process of evaluating the
//    "if" clause, so it issues an error, which behaves similarly to other errors
//    that occur in calling methods (such as "ambiguous call" or "undefined 
//    method"). This error is "circular dependency in 'if' clause; the 
//    'if' clause was accessed recursively inside 'CallFunction'."
// 4. The result of the "if" clause is an error, which causes the evaluation of
//    C1 to fail; C2 will fail in a similar way. Note that an "error" result is
//    quite different from a "false" result; if the function had been written
//    
//        int Overloaded(int j) if (Overloaded((long)3) != 3) { return j; }
//
//    then the result would be false, and Overloaded(int) would simply drop itself 
//    from consideration, which means that it cannot ever be called. In fact,
//    EC# will not even emit this function during conversion to plain C#; it is
//    treated like a template with no arguments, and if a template is never called
//    then it never gets emitted in plain C#.

// Beyond questions of semantics, "static ifs" at file scope also create 
// performance problems for code completion, as discussed in the section about
// CTCE (compile-time code execution). Since a "static if" can call functions,
// a static if statement can take an arbitrary amount of time to complete (in
// fact, it could literally take forever), which would make it more difficult for 
// IntelliSense to build up a database of the program. Since the "if" clause
// effectively makes methods and types disappear, it has basically the same 
// problem, but I believe the solution is fairly straightforward: the IDE could 
// assume that all functions and types exist until proven otherwise--so the IDE 
// could build the program database first and run all "if" clauses later, in a 
// background thread; any methods and types that have not been proven nonexistant 
// by the time the user opens a completion list would be presumed real, and 
// listed.

// EC# does not offer a built-in "static assert" statement because it would imply
// that there should be a corresponding "assert" statement, and it is relatively
// difficult to support the syntax of the latter while maintaining backward
// compatibility because "assert(x > 0)" looks like it should call a method.
// As a substitute, you can use a static if statement such as the following:
static if (Version < 3.0) fail;
// Of course, you have to invert the test (this code means "static assert 
// (Version >= 3.0)".)
// 
// Outside of a function, where "static if" is not available, a static assertion
// can be simulated in a more roundabout way, by declaring a method with invalid
// contents and placing the failure condition in an "if" clause:
void fail() if Version < 3.0 { fail; }
// This works because the contents of a method are not semantically analyzed if
// the "if" clause is false. So if (Version >= 3.0), fail() does not exist and 
// there is no error. If (Version < 3.0), fail() exists and contains an invalid 
// statement.
//
// TODO: To support this, ensure that the "if" clause is checked even if the
// method is never called.
//
// You can use this over and over with the same method name (e.g. "fail") each
// time; there is no conflict since none of the methods are considered to exist.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
// SCRATCHPAD //////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

*makePoint<double>("D");

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


////////////////////////////////////////////////////////////////////////////////
//                                             /////////////////////////////////
// on finally, on success, on throw, on return /////////////////////////////////
//                                             /////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# supports an "on finally" statement which is based on the "scope(exit)" 
// statement from D; it allows you to take an action when the current block exits.
// The following function
int DoSomething()
{
	Console.WriteLine("Entering DoSomething");
	on finally Console.WriteLine("Exiting DoSomething");
	Foo();
	Bar();
}
// Is translated to
int DoSomething()
{
	Console.WriteLine("Entering DoSomething");
	try {
		Foo();
		Bar();
	} finally {
		Console.WriteLine("Exiting DoSomething");
	}
}

// "on finally" is equivalent to D's "scope(exit)".
//
// The main benefit of "on finally" is that you can place "initialization" code 
// and "clean-up" code next to each other, so that you don't have to remember to 
// write the clean-up code later. It also makes the code look simpler, since you
// don't have to write a "try { ... }" block; thus the code is more readable.
// Of course, it is recommended that you use the standard "using()" statement 
// instead of "on finally", if all you want to do is call Dispose() on something.

// If there are multiple "on" statements in a block, they are executed in the 
// reverse lexical order in which they appear. Also, please note that "on" 
// statements are executed when the block exits, not when the function exits.
// For example, the output of this method is "1 4 3 2 5".
void Digits()
{
	Console.Write("1 ");
	{
		on finally Console.Write("2 ");
		on finally Console.Write("3 ");
		Console.Write("4 ");
	}
	Console.WriteLine("5");
}

// The "on throw" statement is translated using a catch and a throw. So
int SoMuchFail() {
	on throw Console.WriteLine("oops");
	return int.Parse("zero");
}
// becomes
int SoMuchFail() {
	try {
		return int.Parse("zero");
	} catch {
		Console.WriteLine("oops");
		throw;
	}
}

// Currently, there is no way to query the exception object or handle only 
// specific exceptions inside an "on failure" block; use try/catch for that.
//
// "on throw" is equivalent to D's "scope(failure)".

// The "on success" statement does an action when the block exits without an
// exception, no matter how it exits: whether via break, continue, return or 
// just reaching the end. For example,
int Yay(int x) {
	on success Console.WriteLine("It worked!");
	if ((x & 1) != 0)
		return checked(x*x);
	else
		return checked(x+x);
}
// is currently translated to
int Yay(int x) {
	if ((x & 1) != 0) {
		int @return = checked(x*x);
		Console.WriteLine("It worked!");
		return @return;
	} else {
		int @return = checked(x+x);
		Console.WriteLine("It worked!");
		return @return;
	}
}

// "on return" is like "on success" except that it is only triggered when a
// block exits via a return statement. It provides access to the return value
// in a variable called @return. you cannot use "on return" if the function
// manually defines a variable called @return. (Both "on return" and "on 
// success" may be used inside other statements, so a "return" statement may
// not be the only way to exit the block.)
int Yay(int x) {
	on return Console.WriteLine("We're returning {0}!", @return);
	if ((x & 1) != 0)
		return checked(x*x);
	else
		return checked(x+x);
}


// As a nod to D, the phrases "scope exit", "scope failure", "scope success",
// "on exit" and "on failure" are all permitted, but they emit a warning telling
// you the expected syntax in EC#. (Note that the D syntax "scope(exit)" could not
// be allowed since it would require the creation of a new "scope" keyword.)

////////////////////////////////////////////////////////////////////////////////
//                            //////////////////////////////////////////////////
// aliases and go-style casts //////////////////////////////////////////////////
//                            //////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// The basic "alias" statement defines an alternate name for a type:
alias Map<K,V> = Dictionary<K,V>;
// EC# also allows "using" directives to have generic or template parameters, 
// because the "using" directive is considered a special kind of alias. If the 
// alias has generic parameters (as opposed to template parameters), the generic 
// parameters must represent generic parameters on the underlying type; otherwise 
// the alias must use template parameters. So in this example, K and V are allowed 
// to be generic (not template) parameters because they are passed as generic 
// parameters to Dictionary<K,V>. Since aliases disappear at runtime but generic 
// parameters do not, it does not make sense to declare an alias with a generic
// parameter that is not passed to the underlying type.
//
// You can sometimes use template parameters instead of generic parameters, but 
// you won't be able to pass open generic types as template parameters, so it is 
// recommended to use generic parameters where applicable.
//
// In simple cases like the one above, the alias is erased entirely from the C#
// output and always replaced with the type it represents.

// "using" is defined in EC# as a special type of alias that is only available in 
// the source file in which it is placed*. Other than that, "using" has all the 
// same features as "alias". (* may be implemented as lexical scope; undecided)
using Map<K,V> = Dictionary<K,V>;
using IMap<K,V> = IDictionary<K,V>;

// The "alias" construct creates new type names for existing types, optionally 
// adding a custom set of methods. But an alias doesn't create a real type (well,
// in fact it may create a new type, but that's just an implementation detail),
// so the new methods cannot be virtual. Obviously, aliases cannot contain 
// fields either, unless they are static.
alias MyString = string
{
	// Members of an alias are "public" by default, though they can also be private
	// or even protected (protected means that only other aliases can access 
	// them).
	int ToInt() { return int.Parse(this); }
}

// When used this way, aliases are EC#'s answer to extension methods. While 
// extension methods are implicit, available on any type at any time without
// any hint that a given method call might be an extension method, aliases 
// create clarity that you want a type to be extended, since you can't call
// ToInt() unless the string as been declared as a MyString. In particular,
// aliases are ideal in conjunction with templates, to help you create 
// adapters between types without the tedium and run-time overhead of creating
// wrapper classes or wrapper structs; more on this later.
//
// Aliases also allow you to add properties, operators and even events (just so
// long as the event doesn't need extra state on the underlying object.)
// Aliases can create new names not just for classes and structs, but also
// enums and delegates.

// "alias" a contextual keyword. It is only recognized as a keyword if it is 
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

// "alias" can be used like "using" except that the names are visible in other 
// source files:
alias Polygon = List<Point>; // in A.ecs
var myPoly = new Polygon();  // in B.ecs
// This particular use of alias is actually converted into plain C# 'using' 
// statement(s), if the alias is at namespace scope and does not have type
// parameters.

// Ahh, the using keyword. It's the gift that keeps on giving, isn't it? It imports
// namespaces (using System;), it renames types ("using Polygon = List<Point>"),
// and it disposes of resources for you ("using (var form = new Form())"). In EC#,
// "using" does one more thing.

// EC# introduces a new kind of cast operator called "using" to help you select
// an alias or interface to use. It has two syntaxes, which are equivalent:
int nine = "9"(using MyString).ToInt();
int nine = ("9" using MyString).ToInt();
// (using T) behaves similarly to a cast, except that the compiler must know at
// compile time that the conversion is valid.
// 
// You can see full details about this operator in the section "Small refinements 
// to C#". Now, back to aliases.

// Normally, the original type is allowed anywhere that the alias is allowed. 
// However, an "explicit alias" treats the alias as if it were a derived type, 
// so that a cast is required from the original type to the alias. However, 
// the alias type is still implicitly convertible back the original type.
MyString path0 = @"C:\";                 // OK
explicit alias PathStr = string;
PathStr path1 = @"C:\"                   // Error: an explicit cast is required
PathStr path2 = @"C:\" using PathStr;    // OK
string p3 = path2;                       // OK, can still go from PathStr to string
Debug.Assert (type string is MyString);  // OK
Debug.Assert (type string is MyString);  // OK

// Function overloading based on aliases is not allowed because it would add a lot
// of complexity to EC# and run-time reflection couldn't support it anyway.
void IllegalOverload(Polygon p);
void IllegalOverload(List<Point> p); // ERROR: conflicts with existing overload


// Because aliases do not exist at runtime, they are considered identical to
// their underlying type when used as generic arguments. However, the compiler
// still requires a cast to 'convert' the underlying type to an explicit alias,
// but this is a formality that has no run-time effect:
List<MyPoint> list = new List<Point>(); // OK
List<Point> list = new List<MyPoint>(); // OK
List<string> list = new List<PathStr>(); // OK
List<PathStr> list = new List<string>(); // ERROR: a cast is required
List<PathStr> list = new List<string>() using List<PathStr>; // OK

// However, because aliases do exist at compile time, they can be passed as
// template arguments to create distinct template behaviors. For example, 
// consider this alias:
alias PottyMouth<#T> = T
{
	new string ToString() { return "DAMN \(base.ToString()), NICE ASS!"; }
}

// Now let's define a template and call it...
IEnumerable<string> ToStrings1<#T>(T[] inputs)
{
	return inputs.Select(t => t.ToString());
}
IEnumerable<string> ToStrings2<T>(T[] inputs)
{
	return inputs.Select(t => t.ToString());
}
int[] array = new int[] { 2, 3, 5 };
PottyMouth<int>[] potty = array;
var A1 = ToStrings1(array);
var P1 = ToStrings1(potty);
var A2 = ToStrings2(array);
var P2 = ToStrings2(potty);

// A1 will be the sequence { "2", "3", "5" }, but P1 will be a shocking series of high-energy compliments to the world's most popular prime numbers. This only works with templates; because the T in ToString2<T>() is not a template parameter, PottyMouth<int> is automatically reduced to plain int before calling ToStrings2(), so P2 and A2 will both be the same as A1.
//
// In aliases that replace ToString(), Equals() or GetHashCode(), the compiler will warn you when the alias is reduced to the original type when it is inferred as a generic type parameter. The compiler will not warn you in all other cases, however; for example, if you pass PottyMouth<int> to a function that expects simply "int", the compiler will not warn you, because logically you should know that the custom ToString() method is lost. After all, in plain C# if you have

class A { void Foo() { } }
class B : A { new void Foo() { } }

// And you pass a B to a method that expects an A, you are expected to be aware that "B.Foo" will not be called if the method calls "Foo()". The warning is issued only when generic parameters that are aliases are reduced to the original type, because some programmers, especially newbies, might not be expecting that ToStrings2<PottyMouth<int>>() actually means the same thing as ToStrings2<int>(). The warning only happens when the reduction cuts off access to ToString(), Equals() or GetHashCode(), because in all other cases the generic method could not have access to methods of the alias anyway.


// Aliases can declare interfaces. Here's a spectacularly pointless example:
interface IHalvable<T>
{
	T ChopInHalf();
}
alias MyFloat = float : IHalvable<MyFloat>
{
	MyFloat ChopInHalf() { return this / 2; }
}

// Now, multiple libraries have been written to allow you to "cast" objects to an
// interface type that an object adheres to, but does not explicitly implement.
// For example, using one of these libraries you could write an interface like
public interface IListSource<out T> : IEnumerable<T>
{
	int Count { get; }
	T this[int index] { get; }
}
// and then, using my "GoInterface" library, apply this interface to a List<T>:
var fellas = new List<string> { "Snap", "Crackle", "Pop" };
IListSource<object> fellas2 = GoInterface<IListSource<string>>.From(fellas);
object second = fellas2[1];

// There are several reasons you might want to cast an object to an interface that
// it doesn't implement; perhaps in this case we were given a list of strings but 
// we really need a list of objects for some reason. You cannot cast an 
// IList<string> to an IList<object>, because IList<T> uses T for both input and 
// output: if the cast were allowed, you could then add the number 3.14159 to your
// IList<object> even though it might be an IList<string> in reality.
//
// IListSource<T>, however, only returns T as output, it does not take it as 
// input, so the .NET framework can allow the cast from IListSource<string> to 
// IListSource<object>. Only problem is, List<T> does not technically implement
// our interface, so we need this special library to perform the cast.
//
// Libraries like this generate code at run-time, so they have a significant 
// cost on startup and when you use them, the compiler cannot verify that the cast
// is valid.
//
// EC# provides a more efficient mechanism through aliases. Given the following 
// alias and method:

alias @using<#T, #I> = T : I {}
internal @using<T,I> Using<#I,#T>(T t) { return t; }

// the cast above can be written as
var fellas = new List<string> { "Snap", "Crackle", "Pop" };
IListSource<object> fellas2 = Using<IListSource<string>>(fellas);

// Unlike the "GoInterface" cast, the code for this cast is generated at compile-
// time so it suffers the minimum possible runtime overhead (specifically, a small
// wrapper class is created that implements the Count, this[] and GetEnumerator() 
// methods.) The compiler will give an error message if the type passed to Using()
// is not known to implement the requested interface; for example, you can't say 
// "Using<IDisposable>(new object())" (If you need to cast any random object to
// an interface, you should use one of the three libraries I mentioned, such as 
// GoInterface.)

// If a type "almost" implements an interface, you can use an alias to finish the
// job. For example, given this interface:
public interface IAddRemove<in T>
{
	void Add(T item);
	void AddRange(IEnumerable<T> list);
	void Remove(T item);
	void Clear();
}
// You can already do this:
void UseAdd()
{
	var adder = Using<IAddRemove<string>>(new List<string>());
	add.AddRange(new[] { "Example", "code", "here" });
	add.Remove("code");
}
// But you cannot do this:
IAdd<T> UseAdd(IList<T> list)
{
	// error: missing interface member 'IAdd<T>.AddRange(IEnumerable<T>)' in '@using<T, IAdd<T>>'
	return Using<IAdd<T>>(list);
}
// ...because for some reason IList<T> doesn't have an AddRange() method. No problem though, just create an alias that does have it:
alias MyIList<T> = IList<T> : IAddRemove<T>
{
	void AddRange(IEnumerable<T> list) {
		foreach (var item in list)
			Add(item);
	}
}
IAddRemove<T> UseAdd(IList<T> list)
{
	return list using MyIList<T>; // OK
}

// Pleease note that once an alias is cast to an interface defined by the alias, it 
// cannot simply be cast back:

IList<int> list1;
IAddRemove<int> addRem = list1 using MyIList<int>; // OK
IList<int> list2 = (IList<int>)addRem; // InvalidCastException at runtime!

// This is a side-effect of the way the feature is implemented in .NET, and it is
// caused by the limitations of .NET; if an EC# compiler ever targeted native code,
// this exception would not occur (not under these circumstances, anyway) because 
// interfaces would be implemented differently. The problem is that when you cast 
// list1 to IAddRemove<int>, a wrapper object is created that implements the 
// IAddRemove<int> interface, because IList<int> does not. The reverse operation, 
// casting back to IList<int>, is performed by the CLR. The CLR has no idea it
// is dealing with a wrapper object, so it cannot unwrap the object in order to
// make the cast succeed.
//
// All EC# interface wrappers implement an interface that provides access to the
// original object, through an "Unwrapped" property. The name of this interface
// is "IEcsWrapper<T>" by default, but you can override the default with the
// [EcsAliasUnwrapper] attribute:
[assembly:EcsAliasUnwrapper(typeof(IEcsWrapper<>), "Unwrapped")]

// EC# actually predefines the Using() method and @using alias in the ECSharp 
// namespace. In fact, there are some overloads:
alias @using<#T, #I> = T : I {}
alias @using<#T, #I1, #I2> = T : I<I1, I2> {}
alias @using<#T, #I1, #I2, #I3> = T : I<I1, I2, I3> {}
alias @using<#T, #I1, #I2, #I3, #I4> = T : I<I1, I2, I3, I4> {}
internal @using<T,I> Using<#I,#T>(T t) { return t; }
internal @using<T,I1,I2> Using<#I1,#I2,#T>(T t) { return t; }
internal @using<T,I1,I2,I3> Using<#I1,#I2,#I3,#T>(T t) { return t; }
internal @using<T,I1,I2,I3,I4> Using<#I1,#I2,#I3,#I4,#T>(T t) { return t; }

// The above definitions use three interface templates that are also predefined:
interface I<#IA, #IB> : IA, IB { }
interface I<#IA, #IB, #IC> : I<IB,IC>, I<IA,IC>, I<IA,IB> { }
interface I<#IA, #IB, #IC, #ID> : I<IB,IC,ID>, I<IA,IC,ID>, I<IA,IB,ID>, I<IA,IB,IC> { }

// These allow you to join two or three interfaces together into a single interface without having to dream up a name for the interface. For example, with this you could declare a variable of type "I<IEnumerable, IComparer, IDisposable>" which means that the variable points to something that implements all three of those interfaces. I<IEnumerable, IComparer, IDisposable> implements not just "IEnumerable", "IComparer", and "IDisposable" themselves; it also implements "I<IComparer, IDisposable>", "I<IEnumerable, IDisposable>" and "I<IEnumerable, IComparer>" so that you can always drop one of the interfaces when you don't need it.
// 
// However, .NET itself doesn't let you declare multiple interfaces on a single storage location like this (except via generic constraints, but let's assume we're just talking about a simple variable). So, in order to cast something that implements "IEnumerable" and "IDisposable" to "I<IEnumerable, IDisposable>", you will need an explicit conversion, which creates a wrapper object:
I<IEnumerable, IDisposable> both = Using<IEnumerable, IDisposable>(something);

// Ultimately, EC# allow you to convert any object to any (statically) compatible 
// interface with the "using" operator instead of the "Using()" method:
IAddRemove<int> addRem = new List<int>() using IAddRemove<T>;
// But implementation of this feature will have low priority.


// Let's talk now about how aliases are reduced to plain C#. First of all, if an 
// alias can be reduced to a "using" directive then it is. So if you use
alias Integer = long;
// at the top of your file, outside any classes, it will be converted to a "using"
// statement, and the same "using" statement will be copied to any other source 
// files that refer to "Integer".
//
// Aliases that are inside classes, have type arguments, declare interfaces or 
// define new methods can't really be translated to plain C# and are usually 
// replaced by the underlying type. If the alias defines new methods or interfaces 
// for a type, EC# generates a plain C# class, named after the alias, to hold 
// these methods and interfaces (the plain C# type is always a class regardless of 
// whether you made an alias for a struct, class, delegate or enum.)
//
// For example, the following code...
alias AInt = int {
	AInt MulDiv(int timesBy, int divideBy) {
		return (int)((long)this * timesBy / divideBy);
	}
}
AInt MetresToYards(AInt metres) {
	return metres.MulDiv(109_361, 100_000);
}
// ... looks like this in plain C#:
class AInt {
	public static int MulDiv(int @this, int timesBy, int divideBy) {
		return (int)((long)@this * timesBy / divideBy);
	}
}
partial static class G {
	public static int MetresToYards(int metres) {
		return AInt.MulDiv(metres, 109_361, 100_000);
	}
}

// Aliases with type arguments will get a plain C# type named in the same manner
// as templates. For example, 
alias MyIList<T> = IList<T> : IAddRemove<T>
{
	void AddRange(IEnumerable<T> list) {
		foreach (var item in list)
			Add(item);
	}
}


// When converting an alias of a struct to plain C#, the struct is passed by 
// reference to methods of the alias, unless it is "obvious" that the methods
// do not mutate the structure (the definition of "obvious" is something we
// can debate later). For example, the following alias
alias MyPoint = Point // System.Drawing.Point
{
	public int Sum() { return X + Y; }
	public int Increase() { return X++ + Y++; }
}

// Is translated to
static class MyPoint
{
	public static int Sum(Point @this) { return @this.X + @this.Y; }
	public static void Increase(ref Point @this) { @this.X++; @this.Y++; }
}





// 
interface IAdd<T> { T operator+(T b); } 
alias Int = int : IAdd<Int>;
=>
struct Int : IAdd<int> {
	public int __value;
	public Int(int value) { __value = value; }
	public int opAddition(int b) { return __value + b; }
}

as<IEnumerable, IComparer, IDisposable>(foo)

// 
// After a colon, an alias can list one or more interfaces. If the aliased type 
// does not already implement the interface, EC# can create a run-time wrapper 
// type that implements the interface, but a wrapper is only created when an alias
// value is casted (implicitly or explicitly) to the interface; if you have defined
// an alias A = B : I and you use "A" a lot in your code, the plain C# version 
// will normally refer to B, not A or I. For example:
public interface IOneProvider<T>
{
	T One { get; } // This will be used in a later example
}
public interface IMultiply<T> : IOneProvider<T>
{
	T operator*(T rhs);
}
public partial alias MyInt = int : IMultiply<int>
{
	int operator*(int rhs) { return this * rhs; }
	int One                 { get { return 1; } }
}
IMultiply<int> seven = 7 as MyInt;
int fourteen = seven.Times(2);
// The code above introduces another new feature, which is operators in interfaces.
// In EC#, operators can be written as member functions: instead of the usual
// "static T operator+(T a, T b)" you can write "T operator+(T b)". Note that
// a binary operator expressed as a member function must take one argument, and
// a unary operator expressed as a member function must take no arguments. During 
// conversion to plain C#, the operator is rewritten as a static method (first 
// parameter named @this), but if the operator is declared virtual or is a member 
// of an interface, then a member function (named e.g. "__opAddition") is actually 
// created. If the operator is virtual then the static operator forwards to the 
// virtual method; but if the operator becomes part of an interface for an alias, 
// the interface method forwards to the static addition operator (which may have
// been defined already by the underlying type).
// 
// The primary motivation for this feature is to fill a hole in C# and the .NET
// framework: namely, the inconvenient fact that primitive types do not implement
// any kind of arithmetic interface. By allowing operators as non-static functions,
// it is possible to cast primitive types to an interface and still use them as
// you normally use numbers. The only caveat is that since the interface is not
// built into the .NET framework, you must somehow define it yourself (a standard
// interface will be developed for EC# eventually). For example:
interface IArithmetic {
	T operator+(T rhs);
	T operator-(T rhs);
	T operator*(T rhs);
	T operator/(T rhs);
	T operator-();
}
IArithmetic eight = (@using<IArithmetic>)(8);
IArithmetic sixteen = eight + eight;

// This example uses the built-in function @using<I> to treat 

class Addable : IAdd<Addable> {
	float f;
	public static Addable operator+(Addable a, Addable b) 
		{ return new Addable { f = a.f + b.f }; }
}
//
// Why do I mention all this in the section on "alias"? Because this feature is 
// specifically designed to help fix a deficiency of the C# primitive types, 
// namely, that they do not implement any arithmetic interface. Although 
// types like "int" and "float" do

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
	public new(public int aliasValue) {}
	public implicit operator int(MyInt i) { return i.aliasValue; }
	public implicit operator MyInt(int i) { return new MyInt(i); }
}

// Although aliases cannot be used as generic arguments, they CAN be used as template
// arguments (see the section below about templates).
int Compare<static T>(T a, T b) where T:IComparable<T> { return a.CompareTo(b); }


// Currently, 'ref' is used if the function mutates any field, or if @this has to 
// be passed to any other method that is not a property getter (including property
// setters). This heuristic may be tweaked in the future and one should not rely on 
// it.
//
// A 'ref' parameter requires extra code transformation if the function is called 
// on an rvalue:
MyPoint OneTwo { get { return new MyPoint(1, 2); } }
MyPoint TwoThree() { return OneTwo.Increase(); }

// In order to call Increase(), TwoThree() must be translated to
Point TwoThree() { 
	Point @1 = OneTwo;
	return MyPoint.Increase(ref @1);
}


// Aliases only support a single attribute, the compile-time attribute [DotNetName]



/*
// The [BlockBase] compiler attribute is useful on aliases. It blocks access to 
// the methods and operators of the original type unless specifically requested 
// through the "base" keyword, so that you can replace the entire interface of a 
// type. Only the methods of System.Object are left accessible. In the following
// example, the methods defined in string are not accessible from a variable of
// type Handle. However, please note that a Handle is still implicitly 
// convertible to string; a proper handle should wrap a value in a structure.
// Note: attributes that exist at run-time cannot be applied to aliases.
[BlockBase] alias Handle = string;
*/


////////////////////////////////////////////////////////////////////////////////
//         /////////////////////////////////////////////////////////////////////
// Symbols /////////////////////////////////////////////////////////////////////
//         /////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// I was first introduced to the concept of a symbol in Ruby, where is commonly 
// used (instead of enumerations) to indicate options when calling a method. A 
// symbol is like a string, except that it is "interned" by default. This means 
// that the it is guaranteed that only one instance of a symbol exists, so 
// comparing two symbols for equality means comparing two references, which is 
// faster than comparing two strings and the the same speed as comparing two 
// integers or enums.
//
// The same concept exists in other languages too, such as LISP. Symbols are more 
// convenient than enums for two reasons:
//
// 1. When calling a method, you don't have to give the name of the enum type.
// 2. When defining a method, you don't have to define an enum type.
// 
// The second point is more important. A lot of times people use one or more 
// boolean flags or integers rather than a descriptive enum because it is 
// inconvenient to define one. Usually you don't want to define the enum right 
// beside the method that needs it, because the caller would have to qualify the
// name excessively:

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

// Isn't that horrible? You don't want your clients to have to double-qualify the name like this. But maintaining and documenting an enum located outside the class is inconvenient. So to avoid the hassle, you replace "MaintainConnection option" with "bool maintainConnection" and be done with it.
// 
// EC# provides a much easier way with Symbols, which are written with a $DollarSign; the symbol "$" should remind you of "S" for "Symbol". Actually I was thinking of using the dollar sign for substitution in metaprogramming (another S word!), but I decided it was probably better to share the same syntax used for string interpolation (\LikeThis). Anyway, with the syntax decided, the above code would be written like this in EC#:

class DatabaseManager {
	...
	public static DatabaseConnection Open(string command, 
		[OneOf($CloseImmediately, $KeepOpen)] Symbol option);
	...
}
// later...
void Open() {
	var c = DatabaseManager.Open("...", $CloseImmediately);
}

// The [OneOf] operator causes the compiler to check the symbol that you pass in, and to generate a static field to hold the symbol, that is accessible from the callers of the method. The above code is converted to plain C# as
class DatabaseManager {
	...
	public static readonly Symbol s_CloseImmediately = GSymbol.Get("CloseImmediately");
	public static readonly Symbol s_KeepOpen = GSymbol.Get("KeepOpen");
	public static DatabaseConnection Open(string command, 
		[OneOf("CloseImmediately", "KeepOpen")] Symbol option);
}
// later...
void Open() {
	var c = DatabaseManager.Open("...", DatabaseManager.s_CloseImmediately);
}

// GSymbol is a class used for constructing symbols. Symbol and GSymbol are defined in the EC# runtime library. The classes used for Symbols and their construction can be controlled with [EcsSymbolClass] and [EcsGetSymbolMethod]:
[assembly:EcsSymbolClass(typeof(Symbol))]
[assembly:EcsGetSymbolMethod(typeof(GSymbol), "Get")]

// The prefix used to define symbols can be changed with the EcsSymbolFieldPrefix attribute:
[assembly:EcsSymbolFieldPrefix("s_", "s_")]
[assembly:EcsSymbolFieldSuffix("", "")]
// The two arguments represent
// (1) the prefix to use for symbols mentioned by [OneOf()], which are public (or the maximum visibility necessary to be used by callers)
// (2) the prefix to use for symbols not mentioned by [OneOf()], which are private
// The prefix can be "", which is convenient when clients of your code may not be written in EC#, but be aware that EC# may not implement collision avoidance between symbols and other identifiers in the same class. If a symbol is used by itself and not directly as an argument to a method, as in
//
Symbol Hi() { return $Hello; }
//
// a field for the symbol may be created in the same class:
//
private static readonly Symbol s_Hello = GSymbol.Get("Hello");
Symbol Hi() { return s_Hello; }
//
// However, if the same class uses [OneOf($Hello, ...)] somewhere, that symbol will be used instead of creating a new one.

// Symbols names cannot start with a digit or be a reserved word unless you use "$@" instead of "$":
var twoDollars = $2; // ERROR
var twoSymbol = $@2; // OK
// Symbol names can contain punctuation using the following syntax:
var four = $@'2+2';
// No space is allowed between $ and the symbol identifier ($ X is illegal).

// The [OneOf] attribute is defined in the EC# runtime, and it can be used not just with symbol arguments but also integers, strings and even enums. It can be applied to properties and fields as well as method arguments.
//
// When you pass a constant value to a method that has a [OneOf] attribute, the EC# compiler checks that the constant matches one of the allowed values. If it doesn't, a compile-time error occurs.
//
// However, [OneOf] does not create a runtime check. If and when EC# implements code contracts, perhaps a new attribute such as [in($Foo, $Bar)] could instruct the compiler to create a run-time "in" contract in addition to a compile-time check.

// EC# itself considers symbols to be constants. Conversion from string to Symbol and back requires a cast but it can be done at compile-time. Symbols cannot be concatenated directly, but you can just concatenate two strings and cast the result to a Symbol. Compile-time casts bypass the EC# runtime library (which defines the Symbol class), and will work even if no such conversion operator exists (although this operator does, in fact, exist in the runtime library).
// 
// The compiler must deal with one very annoying fact: in plain C#, Symbols are not constants.
//
// Firstly, if you declare a constant such as
const Symbol S = $S;
// EC# converts this to "static readonly Symbol" if it is a field or just "Symbol" if it is a local variable.
//
// Secondly, if you use a switch statement with symbols:
switch (shape) {
case $Circle: 
	...
	if (size == 0) break;
	...
case $Square:
	...
	goto case $Circle;
}
// I was planning to convert this to to a sequence of if-else statements, with auto-generated labels and "gotos" if the switch uses "goto case", or "break" before the end of a case. However, "goto case" cannot actually be implemented with a goto because plain C# puts unnecessary restrictions on "goto":
if (shape == $Circle) {
	case_Circle:;
	...
	if (size == 0) goto break_switch; // OK
	...
} else if (shape == $Square) {
	...
	goto case_Circle; // ERROR: No such label 'case_Circle' within the scope of the goto statement
}
break_switch:;
// Therefore, EC# simply converts a switch on Symbols to a switch on strings instead; "switch(sym)" becomes "switch((string)sym)".

// Thirdly, C# attributes cannot actually accept symbols because attributes must be declared with constant expressions, and symbols are not considered constants. EC# uses a workaround: the attribute definition itself must take String or String[] arguments, and those arguments must have the [SymbolInAttribute] attribute to indicate that they are "really" symbols, e.g.

// Usage: [ShapeType($Circle)] or [ShapeType(Type = $Circle)]
class ShapeTypeAttribute {
	public ShapeTypeAttribute() {}
	public ShapeTypeAttribute(
		[SymbolInAttribute, OneOf($Point, $Circle, $Rectangle, $Polygon)] 
		private string _shapeType) {}
	
	[SymbolInAttribute, OneOf($Point, $Circle, $Rectangle, $Polygon)] 
	public string Type
	{
		get { return _shapeType; }
		set { _shapeType = value; }
	}
}

// However, please note that EC# only recognizes [SymbolInAttribute] when you are declaring an attribute, and all it means is that the symbol is converted immediately to a string when the attribute is used (or that the symbol array is converted to a string array). In addition, EC# will not allow strings to be used in place of symbols, unless you use [SymbolInAttribute($OrString)] which means that the argument can be a string or a symbol. At runtime, however, there is no way to distinguish whether a symbol or a string was used to construct the attribute.

// EC# also makes enums easier to use with the following rule: at compile-time, a symbol constant can be converted implicitly to an enum if it matches one of the values of that enum. For example, 
var parts = "1   23  54".Split(new char[] { ' ' }, $RemoveEmptyEntries);
// is translated at compile-time to
var parts = "1   23  54".Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//
// The compiler does not require that this conversion exist on the actual Symbol type (which is defined in the EC# runtime DLL), and in fact the conversion does not exist at run-time.

// The runtime library provides additional functionality for symbols, such as
// "symbol pools" that act as namespaces for symbols, and strongly-typed symbols,
// but the EC# compiler only deals with basic global symbols.

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
// class called "G", located in whatever namespace the global members are located.
// You can choose the class name to use when these members are consumed by other 
// .NET languages. The following assembly attribute changes the name of the class 
// used for namespace "MyNamespace" to "MyCustomName":
//    [assembly:EcsGlobalClass("MyNamespace.MyCustomName")]
//
// Global fields, methods and properties are public by default. They cannot be 
// marked protected. If they are marked private, they are inaccessible from inside
// classes and structs, even in the same namespace.
const double PI = Math.PI;
string UnixToWindows(string text) { return text.Replace("\n", "\r\n"); }

// These methods are accessible in EC# via their plain C# name:
double raspberryPi = G.PI; // OK
// (This implies that assembly:EcsGlobalClass attributes are evaluated before any 
// other expressions, since they could change the meaning of almost any 
// expression. CTCE is disabled while evaluating these attributes.)

// You can also define extension methods at global scope.
int ToInteger(this string s) { return int.Parse(s); }

// You can also define operators at global scope, provided that they do not 
// conflict with built-in operators. A compelling use of this feature is to
// support arraywise operations:
T[] operator+ <#T>(T[] a, T[] b)
{
	if (a.Length != b.Length)
		throw new ArgumentException("Array lengths differ in operator+");
		
	T[] result = new T[a.Length];
	for (int i = 0; i < a.Length; i++)
		result[i] = a[i] + b[i];
	return result;
}
L operator+ <#L>(L a, L b) if (a[0], a.Count, new L()) is legal
{
	if (a.Count:count != b.Count)
		throw new ArgumentException("List lengths differ in operator+");
	
	static if (new L(count) is legal)
		L result = new L(count);
	else
		L result = new L();

	for (int i = 0; i < count; i++)
		result.Add(a[i] + b[i]);
	return result;
}

// So now, given arrays or lists named x and y, you can add them elementwise, as 
// in MATLAB:
int[] a = x + y;
// Operators that are defined in the normal way (on types) cannot be overridden.
// If an operator is invoked and both a "normal" operator and a global operator
// can handle it, a warning is issued with reference to the global operator and 
// the normal operator is actually invoked. (My feeling is that it "should" be an
// error, but global operators will often be templates, and I can't think of an
// easy way to detect that the type used as an argument to a template already 
// defines a given operator.)


// The CLR has a ceq opcode that is supposed to bitwise-compare two values on the
// evaluation stack. There is no reason why it shouldn'work on arbitrary value
// types rather than just primitives, but whether it does is not documented in the
// ECMA spec, and C# provides no access to this opcode.
//
// In a functional "persistent" data structure, I was writing a function like
// "Transform(Sequence<T> s, Func<T,T> d)" that needed to quickly determine whether 
// the function "d" returns the same value or a different value. If the function 
// does not modify most/all of its arguments, then the output sequence can share 
// some/all memory from the input sequence. Without the ability to bitwise compare 
// any value or reference with ceq, a much slower comparison must be used, which \
// hurts performance tremendously.
//
// EC# cannot fix this flaw in C# (or the flaw in the CLR, if it turns out that the 
// opcode doesn't work on arbitrary values) but it can reduce the magnitude of the 
// problem. Incredibly, the default implementation of object.Equals() for value 
// types is documented to use reflection:
//
// "The Equals(Object) implementation provided by ValueType performs a byte-by-byte comparison for value types whose fields are all value types, but it uses reflection to perform a field-by-field comparison of value types whose fields include reference types."
//
// That doesn't even sound right, because I don't actually expect Equals() to behave the same way as ceq: if two Doubles are bitwise equal then Equals() should return true;  but when they are not equal, I would kind of expect it to check whether one is positive zero and one is negative zero, for example.
//
// In any case, EC#'s answer is to add default comparison operators for value types:
// 1. EC# will automatically implement operator "==" for value types. This operator
//    will compare each field with == if available. If the "==" operator is not 
//    available, the compiler will cancel generation of "==" and, if you attempt
//    to use operator ==, an error message will be printed.
// 2. EC# will create operator "!=" in the same manner, looking for "!=" in each
//    field.
// 3. If operator "==" was successfully generated, EC# overrides Equals() 
//    automatically and simply calls operator == if its argument has the right 
//    type.
// 
// (to be clear, EC# does not automatically create these methods if you do it 
// yourself.)
//
// The [assembly:EcsDefineEquality] attribute controls this behavior. Use false
// as the second argument to disable this behavior.
[assembly:EcsDefineEquality(typeof(ValueType), false)]
[assembly:EcsDefineEquality(typeof(MyValueType), true)]


// "#pragma info" prints the location in source code where a symbol is defined, 
// and the locations of any related symbols. For example,
alias Foo = List<Regex>;
#pragma info Foo
// reports something like:
// C:\Temp\EcsTest\Program.ecs(13): #pragma info Foo => type
// C:\Temp\EcsTest\Program.ecs(12): alias EcsTest.Foo = List<Regex>
//   C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\mscorlib.dll: class System.Collections.Generic.List<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
//   C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\System.dll: class System.Text.RegularExpressions.Regex : ISerializable

// "#pragma print" just prints a value
#pragma print "I lub you!"

// EC# introduces a new kind of cast operator, the "using" operator, to help you 
// select an alias or interface to use. "using" is basically a kind of static 
// assertion: it asserts that "t" can act as a T. It behaves like a cast, except 
// that the implicit conversion to T must be legal. In other words, the compiler 
// must know at compile time that the conversion is valid, or there is a compiler
// error. By itself, it can be used to 
// - select an alias to use (including explicit aliases)
// - select an interface (for calling explicit interface implementations)
// - select a base class (for calling methods shadowed by 'new')
// - invoke an implicit conversion (but not an explicit conversion)
// Here's an example:
int three = (new[] { 1,2,3 } using IList).Count;
//
// All arrays actually have a "Count" property, but it is inaccessible unless
// you cast the array to IList or IList<T> first. The "using" operator lets
// you access the Count property just as you could with "as", but "using" is
// safer because the compiler statically guarantees that the cast is valid;
// a run-time cast is not required (EC# does not, however, go to the trouble 
// of actually eliminating the cast during conversion to plain C#.) "using"
// produces an error if the cast is not statically guaranteed to be legal 
// (although if it invokes a conversion operator, the conversion is allowed
// to throw).
//
// The operator has two forms, postfix and infix:
// - infix: "expr using Type"
// - postfix: "expr(using Type)"
//
// The reason for the two syntaxes is to make code easier to write. Let's consider
// a normal cast as an example: say you have an object "obj" that, among other
// things, has a user-defined bag of "annotations" on it, and you have to call  
// "obj.Annotation(key)" to retrieve the annotation associated with the key "key".
// So you write "obj.Annotation(key).X", knowing that this returns an object of 
// type MyAnnotation, and you want to access property X on it. Problem is, 
// "Annotation()" returns an object--you need a dynamic cast! Realizing this, you 
// go back to the beginning of the expression and add the cast by typing 
// "((MyAnnotation)", then you go back before the second dot to add the closing 
// parenthesis.
// 
// To write the code more quickly, you have to realize in advance that a cast will
// be required and write "((MyAnnotation)" up front, complete with two parenthesis.
// The "as" operator has a similar problem; you have to realize in advance that you
// need a parenthsis and write it at the beginning, or else you have to go back 
// later to add a paren. Wouldn't it be nice if you could just write the parenthesis
// right when you need it, instead?
//
// In the example above we want to access the IList.Count property. The normal cast
// syntax requires a parenthesis before "new", but EC# introduces a new cast syntax
// that lets you add the parenthesis "just in time":
int three = new[] { 1,2,3 } (using IList).Count;
//
// Now you might ask, why do we need parenthesis at all? Why not simply suppport:
int three = new[] { 1,2,3 } using IList.Count;
//
// There are two problems with this syntax:
// 1. When your eyes look at this, they naturally group "IList.Count" together, 
//    even though we don't want to use (IList.Count), we want to (use IList).Count.
//    This isn't just a problem for humans, either; someone could have actually 
//    defined a data type called "IList.Count", and then, shouldn't this be a cast
//    to that data type?
// 2. The precedence of "as" is the same as the relational operators like ">", 
//    ">=". For the sake of consistency, the new "using" cast should have the
//    same precedence, therefore "X using Y.Z" must be parsed as X using (Y.Z).
//
// Clearly, we need parenthesis, but (using IList) is more convenient than having
// to add a parenthesis at the beginning of the expression. The postfix operator
// (using X) is not allowed to combine with prefix operators; for example, the 
// expression "(X)value(using Y)" produces a semantic error. (According to the
// standard C# precedence table, suffix operators bind more tightly than prefix
// operators, but this feels slightly counterintuitive to me; to make code clear,
// I simply ban the combination.)
//
// EC# will emit a warning if (using T) converts a struct to an interface,
// because doing so creates a copy of the structure; this is not always what you
// want, especially in template code which is not always designed explicitly
// to handle structures. To eliminate the warning, just change (using T) to a
// regular cast. (The warning is not emitted inside a "is legal" expression.)

// EC# 1.0 does not allow the following use of (using T) even though the cast 
// clearly must be valid:
void AutoDispose<#T>(T t) {
	if (t is IDisposable)
		t(using IDisposable).Dispose();
}
// The compiler does not contain the analysis logic necessary to understand 
// the situation. Just use the "as" operator instead, or write
void AutoDispose<#T>(T t) {
	if ((t as IDisposable):t_ != null)
		t_.Dispose();
}

// For consistency, EC# creates a variant form of the "as" operator, too:
	t(as IDisposable).Dispose();
// This means the same as "(t as IDisposable).Dispose()". I am considering a 
// similar cast operator:
	t(cast IDisposable).Dispose();
// but I have not decided whether to implement it.
//
// It may seem weird that "using" can convert something to an explicit alias, yet
// it cannot invoke an "explicit operator" cast method. My rationale is that a
// conversion to an explicit alias is always possible (and in fact is a no-op),
// whereas an "explicit operator" is typically selected over "implicit operator"
// when either (1) the cast may fail at run-time, or (2) the cast loses 
// information in the conversion process. Since some "explicit operator" casts
// may fail, I felt that "using" was not an appropriate way to express them. Just
// use a cast.


// I considered adding a "non-nullable" type to EC#, perhaps named "string!", that
// would not be allowed to have the value null. However, I had the feeling that 
// this would be a complex feature to add, and I wasn't sure that it was enough:
// shouldn't the user be allowed to define types with arbitrary constraints 
// somehow? What if the user wants an integer that has to be positive, for 
// instance?
//
// I decided that the problem was too big to solve in EC# 1.0, so instead I 
// decided to import a small and simple feature from Boo that addresses one most 
// common sources of null references: method arguments and return values. It is a 
// pain to manually test that each method argument is not null, so EC# 1.0 will 
// offer the [Required] compile-time attribute.
string Juntos([Required] string x, [Required] string y) { return x+y; }
// This attribute can be used with nullable types, just in case it turns out to 
// have some valid use case, and it will produce a warning if applied to a non-
// nullable type. Calling Juntos(null, null) will produce a compile-time warning,
// but the compiler has no other compile-time analysis to detect that null is
// being passed illegally. During conversion to standard C#, [Required] is 
// translated to the following statement at the beginning of the called method: 
//
		if (argument != null) throw new ArgumentNullException("argument");
// 
// The attribute is not supported for return values (maybe in EC# 2.0?)


/*
// EC# allows static members to become part of an interface.
partial struct Fraction : IAdditionGroup<Fraction>
{
	public new(public int Numerator, public int Denominator) { }
	public Fraction Plus(Fraction b) { ... }
	public Fraction Minus(Fraction b) { ... }
	
	// This property is static, but the interface still includes it. 
	// Unfortunately, you cannot call this static method through a null
	// reference to IAdditionGroup<Fraction>, so someone still needs to
	// create an boxed instance of Fraction before calling this property.
	// It would be nice to eliminate this restriction; ideas welcome.
	public static Fraction Zero { get { return new Fraction(0, 1); } }
}
*/

// NOTE: Implementation of the following feature will have low priority.
// Void is treated a first-class type whose only value is "()", a.k.a. 
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
// "@void" when you actually use it as a value. Functions that return void are
// left alone, i.e. they do not return @void at run-time, even though they 
// return it conceptually. The runtime type of void can be changed with the 
// following attribute:
[EcsRuntimeVoid(typeof(ECSharp.@void)]

// NOTE: Implementation of the following feature will have low priority.
// In EC#, interfaces can have static methods which are public by default. These
// static methods are allowed to be extension methods that extend the same 
// interface that is being declared.
interface IStatic {
	static Func<IStatic> Factory { get; set; }
	static bool IsNull(this IStatic self) { return self != null; }
}

// NOTE: Implementation of the following feature will have low priority.
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

// The null-dot operator is compatible with value types. For example, if I have a
// list object called "list" and I call "list?.Count", the result is a nullable
// integer that contains null if list==null, and list.Count otherwise.
//
// When used this way, it is often convenient to combine the ?. and ?? operators:
int GetCount<T>(ICollection<T> collection) {
	// Returns the number of items in the collection, or 0 if collection==null.
	return collection?.Count ?? 0;
	// Equivalent to "return collection != null ? collection.Count : 0"
}

// EC# defines a binary operator "**". It has a single built-in overload for double
// that calls Math.Pow(), and its precedence is above multiplication and division:
double sixteen = 2.0 ** 4; // two to the fourth power
// This new operator creates a minor ambiguity in the lexer, since x**y could mean
// either "x * *y" or "x ** y". EC# assumes that two adjacent stars are the new 
// operator and "***" is parsed as "** *". This does create an incompatibility with
// plain C#, but only in code that uses pointers and multiplication at the same 
// time and doesn't use proper spacing: a very, very tiny amount of C# code.

// EC# defines a binary operator "in". This operator can be overloaded; if there
// is no "operator in" defined in scope, code such as
if (x in (a, b, c)) {}
// is rewritten into one of the following three forms:
// 1. If x has a primitive type, or is a string, or has enum type, or is the 
//    literal null:
if (x == a || x == b || x == c) {}
// 2. If x may have a reference type (including unconstrained generic parameters)
if (object.Equals(x, a) || object.Equals(x, b) || object.Equals(x, c)) {}
// 3. If x is a value type (including a nullable type)
if (x.Equals(a) || x.Equals(b) || x.Equals(c)) {}
// 4. In any other cases (e.g. pointers), translation (1) is the default.
//
// If x is not a local variable, a temporary is created to hold its value, so
// for example if x is a primitive property or field then the translation will
// be something like
if (x:@0 == a || @0 == b || @0 == c) {}
// A tuple literal is not required for these rewrites; any type that has members
// Item1, Item2, etc. will suffice. The void value () will not be supported as
// a right-hand side unless someone can present a significant use case to me.
// 
// The EC# runtime library will support range tests such as "x in 0.UpTo(10)"
// and "i in 0..10" (keep in mind that the range 0..10 does not include 10, and 
// therefore should only be used to test array indexes. Heck, even this usage 
// seems slightly confusing, but maybe we can get used to it.)
//
// The precedence of "in" is just above relational operators (>=, as, etc.) 
// which is below + and -. EC# gives a warning if it is used in the same 
// subexpression with the shift operators.

// EC# allows the type of a field to be inferred, if possible.
var _assemblyTable = new Dictionary<string, Assembly>();

// NOTE: Implementation of the following feature will have low priority.
// EC# allows the type of a lambda function to be inferred to be one of the "Func" 
// delegate types. For example:
var Square = (double d) => d*d;

// NOTE: Implementation of the following feature will have low priority.
// EC# 2.0 may introduce the keyword "def" for declaring methods, a syntax copied 
// from Python and Boo. If a method is declared with the "def" keyword, it is 
// optional to specify its return type, and the return type is inferred from the 
// content of the method, e.g.
def Cube(double d) { return d*d*d; }
// The primary purpose of "def" is to make the return value optional; but if you
// use it everywhere, it makes functions easier to find with plain-text search
// functions. "def" is a contextual keyword, so it does not work if there is a 
// type in scope called "def".

// The following feature is cancelled for now: it is not clear whether it is worth 
// the complexity, or whether the syntax proposed is ideal, or how to permit or 
// prevent forwarding to the methods of 'object' or to base classes.
/*
// EC# may define the "using fallback" directive, which effectively overloads the
// dot operator. The "fallback" is used not only when a type is accessed with an 
// actual dot, but it also provides access to the operators (including conversion
// operators) inside the inner type. The fallback has low priority: it is used only
// if nothing on the original type can do the job. For example:
class FakeString {
	public new(public string Value) {}
	using fallback Value;
	public static implicit operator string(FakeString ps) { return ps.Value; }
	public static implicit operator FakeString(string rs) { return new FakeString(rs); }
}

// Since FakeString has a fallback, you can call the methods and properties of 
// string from a PretendString:
Debug.Assert(new PretendString("hello").StartsWith("hell"));

// The fallback can implicitly provide inteface implementations:
class FakeString : IEnumerable<char> {
	public new(public string Value) {}
	using fallback Value;
	...
}
// In this case, FakeString automatically implements IEnumerable<char> by 
// forwarding its methods to Value. If the fallback is a value type field that has
// explicit interface implementations, the fallback will have to be boxed, which
// may create surprising behavior because as methods that mutate the struct will
// not work, so a warning will be issued about such methods unless they are 
// property getters. 

// Of course, if you are going to wrap a single value you would ordinarily just use
// an alias. However, an alias cannot add any new state to an object; the fallback 
// mechanism allows you to create a decorator with extra state beyond that of the
// original object.

// The fallback mechanism does not provide a conversion to the underlying type nor 
// access to the original object itself; for example, consider this scenario:
struct AB { 
	public int A, B;
}
struct AB2 {
	private AB _ab;
	using fallback _ab;
	public int B;
}
// In this case, AB2 cannot be converted to AB and code outside the structure can
// modify only _ab.A, not _ab.B (because AB2.B hides it.)

// My original plan was to allow you to write an "operator ." method, but if you 
// were to return a field from this method, the method would have to return a
// copy of the field instead of a reference to it. For example, consider this
// "3D" point structure:
struct Point3D {
	using fallback _2d;
	public new(private Point _2d) {}
	public new(int x, int y) { X = x; Y = y; }
	public int Z { get; set; }
	public static explicit operator Point(Point3D p) { return p._2d; }
	public static implicit operator Point3D(Point p) { return new Point3D(p); }
}
// The second constructor, which modifies _2d.X and _2d.Y, could not work if it
// invoked an overloaded "operator ." because it would not be possible to 
// modify the Point that is returned by the operator. "using fallback", in
// contrast, provides direct access to the field _2d. It could instead, if you 
// want, return an arbitrary expression (e.g. using fallback 1 + 1;)
// Note that in order to provide access to the field _2d outside the assembly,
// it must be made public during conversion to C#. To avoid making it public,
// you could make a copy of _2d by writing "using fallback (Point)_2d", but 
// then it would not be possible to modify _2d without naming it specifically.

// The implicit conversion operators are needed to use operator overloads, such
// as operator+, because the actual operator+ in System.String expects two
// strings, not two PretendStrings, and it returns a real string. So the 
// following statement can only work if both implicit conversions exist:
PretendString food = new PretendString("foo") + new PretendString("d");
*/

// NOTE: Implementation of the following feature will have low priority, except 
// in alias definitions where it is most needed.
//
// Plain C# allows you to get the run-time Type of a type T using typeof(T).
// EC#, in addition, allows you to use "typeof<exp>" in place of a type, to
// convert an expression "exp" into a type, in any context where a type is
// expected. The change from parenthesis () to angle brackets <> tells the
// compiler that typeof represents a compile-time type instead of a reflection
// object.
//
// If the typeof expression contains a greater-than sign or a less-than sign,
// the expression must be placed in parenthesis:
typeof<7>6> booleanVar = true; // SYNTAX ERROR
typeof<(7>6)> booleanVar = true; // OK
typeof<new List<int>()> = new List<int>(); // SYNTAX ERROR
typeof<(new List<int>())> = new List<int>(); // OK, but VERY BIZARRE
// The expression inside typeof<> cannot create any real variables, and the 
// compiler will give a warning if any variables are created whose name does not
// consist entirely of underscores.

// If EC# had its own real compiler, it could support ref locals and ref returns.
// See here for a sketch of the feature:
// http://blogs.msdn.com/b/ericlippert/archive/2011/06/23/ref-returns-and-ref-locals.aspx

// Also if EC# had its own real compiler, it would be feasible to standardize the
// names of the generic classes used by anonymous types:
// http://blogs.msdn.com/b/ericlippert/archive/2010/12/20/why-are-anonymous-types-generic.aspx

// EC# allows "try", "catch" and "finally" blocks to be used without braces, just 
// like "if", "while", and "for" statements:
T ParseOrDefault<#T>(string s, T defaultValue)
{
	try 
		return T.Parse(s);
	catch (FormatException)
		return defaultValue;
	finally
		Trace.WriteLine("Just tried to parse '\(s)'");
}
// If the keyword catch is followed by '(', it must specify a type of exception to 
// catch. For example, "catch (7).ToString();" is a syntax error even though 
// "catch { (7).ToString(); }" is valid C#.

// If you don't use any "breaks" in a switch statement, EC# will insert them all 
// for you.
string s;
switch (n) {
case 0: s = "zero";
case 1: s = "one";
case 2: s = "two";
case 3: s = "three";
case 4: s = "four";
}
// If you use at least one break, EC# will issue a warning about inconsistent 
// usage of 'break'. One of my concerns is that if the programmer doesn't have to 
// use breaks, he or she may forget that 'break' (although not continue) actually 
// applies to the switch() and not to the enclosing loop:
for (i = list.Count; i >= 0; i--) {
	switch(list[i]) {
	case 'A': 
		foo.A(i);
	case 'B':
		if (--i < 0)
			break; // breaks out of the switch. But is that what the user intended?
		foo.B(i);
	}
	foo.Done(i);
}
// The warning appears only if there is no label on the break statement.

// EC# also produces a warning if there is an empty case; break is not inserted
// for that case.
switch (choice) {
	case "y": case "Y": Console.WriteLine("Yes!!");
	case "n": case "N": Console.WriteLine("No!!");
	default: Console.WriteLine("What you say!!");
}
// The problem, of course, is that it is not really clear whether the user wanted
// to fall through the case or to do nothing for that case. Therefore, EC# 
// provides a new syntax, "case A, B:" to make it clear that you want one block to
// handle multiple cases. If you really want a case block to do nothing, use an
// empty statement ";" or block "{}".

// The 'break' and 'continue' statements can now have a single identifier as an
// argument, which refers to which loop to break out of in case multiple loops are
// nested:
outer:for (int i = 0; i < list.Count; i++)
	for (int j = 0; j < list[i].Count; j++) {
		if (list[i][j] < 0)
			break outer;
		if (list[i][j] == 0)
			continue outer;
		Process(list[i][j]);
	}
// This is translated to plain C# by creating new label(s) plus goto(s):
outer:for (int i = 0; i < list.Count; i++) {
	for (int j = 0; j < list[i].Count; j++) {
		if (list[i][j] < 0)
			goto break_outer;
		if (list[i][j] == 0)
			goto continue_outer;
		Process(list[i][j]);
	}
	continue_outer:;
}
break_outer:;
// (Note to self, braces must be added in some cases)

// In EC#, the foreach statement does not require the word 'var':
foreach (i in new int[] { 1,2,4,8,16,32 }) { ... }
// A new variable 'i' is still created; it is converted to plain C# simply by 
// changing "i" to "var i".


// NOTE: The following feature will probably not be implemented, since it doesn't 
// seem worth the large effort required (keep in mind that this could happen inside 
// any other statement, and there are additional difficulties if the value to be
// changed is a property.)
//
// It would be nice if it were legal to select a variable or property to change 
// based on a condition:
int Var = 0;
int Prop { get; set; }
...
(condition ? Var : Prop) = value;


// EC# allows you to define identifiers that contain punctuation using an "at" sign
// followed by a single-quoted string:
bool @'IsEC#Code' = true;    // Declares a variable called "IsEC#Code" in EC#
// Plain C# already has a comparable feature, namely, unicode escapes:
bool IsEC\u0035Code = false; // Declares "IsEC#Code" in plain C#

// EC# allows you to put underscores in number literals, to provide visual 
// separation:
int TwoBillion = 2_000_000_000;
long FourGB = 0x1_0000_0000;
// Underscores can only appear between two digits of a number.


// EC# allows the first line of a source file to being with "#!", e.g.
#!/bin/run_ecs_script
// This line is called a shebang, and if it is present, it is treated as a comment. A shebang is used on Unix and Linux systems to specify a program to use to invoke a script that is marked as executable. EC# does not yet define a program able to run an EC# script, but allowing a shebang allows interested parties to set up a mechanism for invoking EC# scripts.


/* FYI

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
// since you can just remove the parenthesis around TestD if you want to invoke
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
*/

////////////////////////////////////////////////////////////////////////////////
//                            //////////////////////////////////////////////////
// Code blocks as expressions //////////////////////////////////////////////////
//                            //////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// In EC#, anonymous blocks can be used as subexpressions. All exit points of 
// these blocks must be statically proven to use the new "out" statement to 
// produce a single value. A block used as an expression cannot exit without 
// producing a value, unless it throws an exception. A typical use would be a
// switch statement:
string category = 
{
	switch (x) { 
		case 0, 1, 2: out "lo";
		case 3, 4, 5: out "med";
	}
	out (x < 0 ? "neg" : "hi");
};

// Translating these beasts to C# is similar to tuple unpacking.
if (a < 50 && { ... } < 50 && c < 50)
	return;
// is converted to
if (a < 50) {
	TYPE @0; // where TYPE is the inferred result type of the block
	@0:{ ... } // within the block, "out xyz;" becomes "{ @0 = xyz; break @0; }"
	           // or simply @0 = xyz; if "out" is the last statement in the block.
	if (@0 < 50 && c < 50)
		return;
}
// and
if (a < 50 || { ... } < 50 || c < 50)
	return;
// becomes
{
	TYPE @0; // where TYPE is the inferred result type of the block
	if (a < 50)
		goto if_0;
	@0:{ ... } // within the block, "out xyz;" becomes "{ @0 = xyz; break @0; }"
	if (@0 < 50 || c < 50) {
		if_0:;
		return;
	}
}

// TODO: generalize. Remember to preserve correct order of evaluation.
if (Foo() + { ... } + Bar() > Baz())
	Etc();
// becomes
{
	var @0 = Foo();
	TYPE @1; // where TYPE is the inferred result type of the block
	@1:{ ... } // within the block, "out xyz;" becomes "{ @1 = xyz; break @1; }"
	if (@0 + @1 + Bar() > Baz())
		Etc();
}


if (a < 50 || Foo(b < 50 || { ... } < 50) || c < 50)
	React();
// becomes
{
	if (a < 50)
		goto if_0;
	var @0 = b < 50 || { ... } < 50;
	if (Foo(@0) || c < 50) {
		if_0:;
		React();
	}
}
// becomes
{
	if (a < 50)
		goto if_0;
	bool @0 = b < 50;
	TYPE @1;
	@1:{ ... }
	@0 = @0 || @1 < 50;
	if (Foo(@0) || c < 50) {
		if_0:;
		React();
	}
}




// It must be obvious that a block was intended to be a subexpression, so a 
// stand-alone or lambda expression cannot begin with a block; for example, you 
// cannot write something as ridiculous as
{ out new Func<int>(x => x+x); }(21);
// but you can write
var answer = { out new Func<int>(x => x+x); }(21); // 42!
// which is at the outer limits of silliness.

// Of course, blocks cannot be transformed into expression trees.

////////////////////////////////////////////////////////////////////////////////
//                         /////////////////////////////////////////////////////
// the `backtick` operator /////////////////////////////////////////////////////
//                         /////////////////////////////////////////////////////
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
bool operator `is odd`(int x) { return (x & 1) != 0; }
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
// "normalize" to the range 0 to 360, I can do it with (angle `mod` 360), but
// (angle % 360) does not give the desired result if the angle is negative.

// The backtick operator's precedence is nebulously defined. It is above the 
// relational operators such as '>=' and 'as', and below the unary opertors. The
// unary backtick has higher precedence than the binary backtick, and it is left-
// associative, so the expression:
Console.WriteLine(Man `from` Spain `shoot` President `elect`);
// parses as
Console.WriteLine((Man `from` Spain) `shoot` (President `elect`));

// An error occurs if the backtick operator is mixed with arithmetic operators 
// such as >>, +, -, or /:

if (i `mod` 5 == 0) { } // OK
if (i `mod` 5 + 1 == 1) {} // ERROR: the backtick operator cannot be mixed directly 
           // with arithmetic operators; use parenthesis to clarify your intention.

// Someday, EC# may define a mechanism to select the precedence of specific user-
// defined operators; until that day, the precedence is left deliberately unclear.

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
// out that a proposed operator can use any two: it can be prefix/postfix,
// prefix/infix, or infix/postfix, but it cannot be all three of these.


////////////////////////////////////////////////////////////////////////////////
//                   ///////////////////////////////////////////////////////////
// Slices and ranges ///////////////////////////////////////////////////////////
//                   ///////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// First of all, EC# has a new $ "count operator" lifted directly from D:
var team = new List<char> { 't', 'e', 'a', 'm' };
var em = team[$-1]; // em == 'm'
// The $ "operator" can only appear inside square brackets or after '.', and it 
// does one of three things. First, the EC# compiler looks for an "operator$" 
// function that takes a single argument (the object being indexed, for which to 
// get the end location). If there is no such operator, EC# invokes the Count 
// property on the value that is being indexed (in this case "team"). If there is 
// no Count property on the value, EC# looks for a Length property instead.
//
// Outside square brackets, you can also use it like a property and it behaves
// the same way (please note that it cannot access 'this' unless you specifically
// write "this.$".)
int four = team.$;
//
// In the field of parsing, the symbol $ traditionally means "end of input", so it 
// makes sense to use it to represent the end of a list. It seems a little strange 
// to call it an "operator", because syntactically it acts as an identifier rather 
// than an operator. Oh well, whatever. What would you call it?

// EC# also has a new ".." operator which is used to express a range; however, 
// no meaning for this operator is built into the compiler and it must be added by
// referencing the EC# runtime library or defining a meaning; for example, a
// basic implementation would be
public operator..(int start, int stop) { return new IntRange(start, stop); }
public struct IntRange : IEnumerable<int>
{
	public new (public int Start, public int Stop) {}
	
	public IEnumerator<int> GetEnumerator() {
		for (int i = Start; i < Stop; i++)
			yield return i;
	}
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
		return GetEnumerator();
	}
}

// With this, you can write code such as
foreach (int i in 1..list.$)
	list[i-1] = list[i];
// By convention, a range includes the first element but not the last one. So
// 0..10 really represents the sequence 0 to 9 inclusive. This makes the most
// sense for ranges that represent array indexes, so that the index variable "i" 
// will stop before it reaches list.Count.
// 
// 0..0 is an empty range, and given the above definition of IntRange, 10..0 is
// also empty, since the enumerator will not return anything. For now, the 
// version of IntRange in EC#'s runtime library throws ArgumentException if 
// "Stop" is less than "Start". It would be logical if 10..0 returns 10 down to 
// 1 inclusive, but this behavior is not especially useful or intuitive, so it
// is not supported. Instead you should use the Ruby-like extension methods
// UpTo and DownTo: 9.DownTo(0) and 0.UpTo(9) give ranges that include both 0
// and 9. 10.UpTo(0) and 0.DownTo(10) both return empty ranges.
//
// The precedence of ".." is above the relational operators (>, ==, etc.) and 
// below the shift operators (<<, >>) so that things like "1..$-1" are understood 
// as "1..($-1)".
// 
// Now let's talk about the really important part: slices. New languages including 
// D2 and Go use "slices" in preference to arrays or pointers. One way to think of 
// a slice is as a "fat pointer": a pointer plus a Length. Whereas .NET array 
// references always point to the beginning of an array, a slice can point anywhere 
// in the middle of an array. 
//
// Some arguments in favor of slices:
// - Performance and convenience: in a language without slices, a function may take 
//   an "int[]" as an argument. What if you only want to pass part of an array? 
//   You'll have to manually make a copy of the section you want to pass. 
// - Convenience: In .NET it is not uncommon to take three arguments, such as
//   (T[] array, int start, int end) or (T[] array, int start, int count). With 
//   slices, only one argument is needed. Also notice that taking three arguments
//   is dangerous, since there are two obvious ways to express the range: the 
//   third argument could be the last index or the length of the subrange. The
//   compiler cannot detect if you make a mistake and use the wrong interpretation.
// - Convenience: In the .NET BCL you'll often see two overloaded methods, one 
//   taking just an array, and another taking (array, start, end). If slices 
//   existed this would be unnecessary.
// - Performance: When the .NET framework sees a loop of the form:
			for (int i = 0; i < array.Length; i++) { ... }
//   the JIT can optimize out the bounds check when accessing array[i]. However,
//   if your method is written
	int IndexOf(double[] array, int start, int end, double item) {
		for (int i = start; i < end; i++)
			if (array[i] == item) return i;
	}
//   the optimizer cannot remove the bounds check. If the CLR supported slices,
//   the loop would be written like the first loop above, so the bounds check
//   could be optimized out.
// - Performance and convenience: you never have to check if a slice is null;
//   it is always safe to read the Count.
// - Performance: slices use less memory than triplets.
// - Power: slices could be even more useful if you could cast them in type-safe 
//   ways. For instance you should be able to cast a int[4] slice to a short[8] 
//   slice, and you should be able to cast a short[7] to an int[3] (there is the 
//   possibility of an alignment exception in the latter case on certain 
//   processors, but it is still "safe" in the sense that memory corruption is 
//   impossible). Slice casts would be extremely useful for parsing various binary 
//   data blobs (e.g. binary files, network packets), which are difficult to deal 
//   with in .NET right now.
// - Power, performance and convenience: slices in D actually act as dynamic 
//   arrays; you can append items one-at-a-time to any slice, regardless of whether 
//   the slice currently points to the stack or the heap and whether the slice is 
//   aliased by a larger slice. In most situations, a sequence of append operations 
//   occurs in amortized linear time. Therefore, D-style slices completely 
//   eliminate the need for separate List<T> and T[] data types, and they are
//   more efficient than List<T>. But that's not all! In D, array elements can
//   be immutable, which eliminates the need for a separate string data type too
//   ("string" is nothing but an alias for "immutable(char)[]", where "[]" means
//   a slice, since slices and arrays are practically the same thing.)

// It's pretty obvious that slices are the future. Sadly, it is impossible to gain 
// the performance advantages of slices in .NET; for high-performance slices, CLR 
// support would be essential, and I urge you to pester your local Microsoft CLR 
// representative about this issue. That said, EC# can at least provide some of
// the syntactic convenience of slices. The syntactic sugars provided for this 
// purpose are:
//
// 1. T[..], which is translated textually to EcsSlice<T>. A suitable type or 
//    alias must be in scope to give meaning to it; the default assignment is
//    alias T[..] = CowArraySlice<T>.
// 2. The .. operator.
// 3. The $ pseudo-operator.

// With these features in place, the rest of the machinery is provided by the EC# 
// runtime library:
// - The library overloads operator[] to let you easily create slices from sections 
//   of arrays, List<T>s, IList<T>s and strings.
// - The template "struct SliceOf<#L>" is a slice specialized for list type L. Four
//   concrete specializations are in the runtime DLL:
//   - Slice<T> slices any IList<T>
//   - ArraySlice<T> slices any array
//   - ListSlice<T> slices List<T>
//   - StringSlice slices System.String
//   - Conversions to Slice<T> are implicit; conversions from Slice<T> are explicit
// - DSlice<T>, a struct, is like Slice<T> except that it supports add, insert 
//   and remove operations; its semantics are modeled on the D slice, which is 
//   designed to be more efficient than intuitive. It supports Add() and Insert() 
//   by copying the underlying data to a special slice type; RemoveAt() works 
//   without creating a copy (unless a copy has already been made), by rearranging 
//   members in the original list and then decreasing the slice length by one.
//   DSlice<T> is actually not that efficient, but if the .NET framework were 
//   ever to add support for efficient slices, DSlice<T> represents the way they 
//   would most likely behave, because the D system is fairly efficient (although
//   it seems to lack a mechanism for allocating non-growable arrays of a specific
//   size on the heap, for some reason, but I digress).
// - DArraySlice<T> is like DSlice<T>, but specialized for arrays.
// - CowSlice<T> is a copy-on-write slice struct. It treats the original IList<T> 
//   as read-only, and copies the data to a new array the first time you make a 
//   change. It is based on a template CowSliceOf<#L> which is also specialized
//   to CowListSlice<T>, CowArraySlice<T> and CowStringBuilder.
// - SourceSlice<T> is a slice of an IListSource<T>. slice types based on IList<T>, 
//   List<T> or arrays can be converted to SourceSlice<T> with an explicit cast,
//   but this requires a memory allocation so CowSlice<T> is generally preferred.
//
// The EC# runtime library also provides several other collection-related types 
// and interfaces (including IListSource<T>) that are not specifically related to 
// slices.

alias SliceOf<#L> = SliceOf<typeof<default(L)[0]>, L>;
struct SliceOf<T, #L> if typeof<default(L)[0]> is T {
	...
}
[DotNetName] alias Slice<T> = SliceOf<IList<T>>;
[DotNetName] alias ArraySlice<T> = SliceOf<T[]>;
[DotNetName] alias ListSlice<T> = SliceOf<List<T>>;

struct CowArraySlice<T> {
	int start;
	int count; // high bit set if owned
	T[] array;
	
	T this[int index] {
		get { 
			if ((uint)(index - start) >= count)
				throw IndexOutOfRangeException();
			return array[start + index];
		}
		set {
			if (count >= 0)
				{make copy}
			array[index] = value;
		}
	}
}

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

// Tuple types are not defined by EC# itself but by the programmer. To be fully
// functional, you should use a tuple type that has a "Count" member that returns 
// the number of items in the tuple.
struct Tuple<#A, #B>
{
	public new(public A A, public B B) {}
	public A Item1 { get { return A; } }
	public B Item2 { get { return B; } }
	public const int Count = 2;
	
	// TODO: Slicing???
}

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
// Initially, the colon operator is not planned to be able to unpack tuples.
Tuple.Create(45, "forty-five"):(x,y); // ERROR: not supported.

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
// you can use it document the fact that you are deliberately discarding a return 
// value.
_ = new Form().Handle;

// You can't do this if there is a symbol defined called '_':
{ int _ = 5; _ = "5"; } // ERROR: cannot convert string to int

// The default type of a tuple is System.Tuple by default, but you may get better
// performance by using a struct instead to represent pairs. This can be 
// accomplished (e.g. using Loyc.Essentials.Pair) as follows:
[assembly:EcsTupleType(typeof(Loyc.Essentials.Pair<,>))]
// (This implies that the compiler must analyze assembly:EcsTupleType attributes 
// before any expressions that use tuples.)

// There is no built-in representation for the type of a tuple. Use typeof<> to
// represent a tuple type by example, and remember that you need two pairs of
// parenthesis (one for typeof, and one for the tuple):
typeof<(1, (object)null)> TupleOfIntAndObject()
{
	return (1, "This string is returned as an object");
}

// A tuple can be "exploded" using the * operator. An exploded tuple can be passed
// as a sequence of arguments to a method, or as a series of elements of an array.
var args = (1, 3, 4);
Console.WriteLine("{0} plus {1} equals {2}", *args);
Console.WriteLine("{0} plus {1} equals {2}", args.Item1, args.Item2, args.Item3); // plain C#
int[] array = new[] { *args };
int[] array = new[] { args.Item1, args.Item2, args.Item3 }; // plain C#
// If the tuple type does not contain a Count, the compiler effectively uses a 
// sequence of "is legal" tests to find out how many items a tuple contains 
// (e.g. "args.Item1 is legal", "args.Item2 is legal", etc.).

// Currently, EC# does not offer such a thing as a variable template argument list;
// Maybe EC# 2.0 could use tuples to accomplish this. EC# does not support slicing 
// on tuples, either.

////////////////////////////////////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
// multiple-source name lookup ////////////////////////////////////////////////
//                             /////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// EC# allows "multiple-source name lookup" (MSNL), i.e. lookup of static members in
// multiple classes. In addition, static classes are ignored when resolving variable
// declarations and return types. These two changes allow you to write what act like
// "static extension methods". For example, the following code is illegal in plain
// C#, but works in EC#.
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

// Note: MSNL will not really help resolve templates. Given the following pair of 
// methods:
void DoTemplateT<#T>() { ... }
void DoTemplateG<G>() { ... }
// If you invoke DoTemplateT<String> inside namespace MSNL, the compiler will give 
// an error because it cannot decide whether to create DoTemplateT<MSNL.String> or
// DoTemplateT<System.String>; it cannot use both. On the other hand, if you invoke
// DoTemplateG<String>, there is no error because it is not legal to use a static 
// class like MSNL.String as a generic argument, so System.String is the only 
// possible meaning. The reason for the difference is that template methods can
// access static members, while generic methods cannot.

////////////////////////////////////////////////////////////////////////////////
//                    //////////////////////////////////////////////////////////
// Quoted expressions //////////////////////////////////////////////////////////
//                    //////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

// This feature is tentative and subject to cancellation, and since it will be 
// useless without CTCE, it will be a long time before it actually gets 
// implemented, if it is ever implemented at all.
//
// Plain C# supports a concept called expression trees. EC# supports a compile-
// time concept called "quoted expressions". Quoted expressions allow you to
// store an uninterpreted expression and expand it later, in a different context;
// they permit simple compile-time macros, and are typically used with CTCE.
// "#expr" is a new keyword that represents the data type of a quoted expression.
//
// Here is a simple example:

#expr FormatTwice(#expr input)
{
	return #(
		string.Format(\input) + "\n" + string.Format(\input)
	);
}

void Hellos()
{
	int i = 0;
	Console.WriteLine(*FormatTwice(#("Hello number {0}!", ++i)));
}

// This writes two lines to the console: "Hello number 1!" and "Hello number 2!". 
// Its translation to plain C# looks something like this:

void Hellos()
{
	int i = 0;
	Console.WriteLine(string.Format("Hello number {0}!", ++i) 
	         + "\n" + string.Format("Hello number {0}!", ++i));
}

// So what happened? First of all, the FormatTwice method is an ordinary method,
// not a template. It disappears during conversion to plain C#, but only because 
// "#expr" has no runtime representation. In a future version of EC#, #expr may be 
// given some runtime representation, in which case the method could still exist 
// at runtime and you would be able to manipulate (and possibly compile) a quoted 
// expression at runtime, just as you can with expression trees.
//
// Hellos() must "quote" the argument it sends to FormatTwice with the quote 
// operator, "#". Requiring these operators at the call site makes it very clear 
// that some magic is happening, something far outside the realm of plain C#.
// Hellos() quotes the expression 
// 
//     "Hello number {0}!", ++i
//
// The parenthesis around this expression are required because the # operator has
// high precedence, but the parenthesis do not create a tuple; rather, a "comma
// operator" separates the two subexpressions (I have not decided whether the 
// comma operator will work outside quoted expressions... ask me later.)
//
// Hellos() calls FormatTwice() at compile-time by invoking the explode operator 
// (*). This operator is a compile-time transformation that changes a quoted 
// expression into a normal expression (the explode operator can also be used on 
// tuples, to convert a tuple to a sequence of expressions. In both cases, the 
// explode operator performs a compile-time transformation, but with tuples the 
// actual value of the tuple is not known at compile-time. Exploding a quoted 
// expression is different, in that the value of the quoted expression must be 
// known at compile-time. Therefore, *x and static(*x) are equivalent.)
//
// FormatTwice() quotes a new expression:
//
//     string.Format(\input) + "\n" + string.Format(\input)
//
// This time, it inserts the value of "input" into the quoted expression--twice.
// This is analagous to the the substitution operator in a so-called "interpolated" 
// string such as "New high score, \(name)! Good job!".
//
// Please note that the expression is built by FormatTwice() but it is not 
// interpreted; the method cannot be written like this:
string FormatTwice(#expr input)
{
	return *#( // ERROR
		string.Format(\input) + "\n" + string.Format(\input)
	);
}
// This cannot work for two reasons:
// 1. The "input" variable refers to a variable called "i", but there is no 
//    variable "i" defined in the context of FormatTwice(). The explode operator
//    (*) can only be used in a location where the entire expression makes sense.
// 2. It is not legal for "input" to be used in a "static" context. Even though
//    the value of input may be known at compile-time, that is still too late,
//    because the compiler works in stages. The compiler interprets the * operator
//    before the method is actually called, so the compiler does not know what
//    the value of "input" will be when it is expanding the quoted expression.
//
// Therefore, only Hellos() can use the explode operator, not FormatTwice().

// Quoted expressions are a very peculiar type that only exists at compile-time;
// Any field, variable, property or method that uses #expr in its signature will
// vanish when EC# is converted to plain C#, because no run-time representation
// is defined for the type #expr. Any field or method that uses #expr must be 
// declared static, since there is currently no way to access a non-static value 
// at compile-time.

// Quoted expressions have a ToString() function that converts the expression to
// a string.


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
