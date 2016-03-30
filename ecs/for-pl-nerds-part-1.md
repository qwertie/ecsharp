---
title: "Enhanced C# for PL Nerds, Part 1 of 4: Introduction"
layout: article
toc: true
---

Enhanced C# (EC#) is a new statically-typed programming language that combines C# with features of LISP, D, and other languages.

This article is a quick introduction to Enhanced C# for programming language wonks, the kind of people who fantasize about using continuations to model higher-order polymorphic multimethod monads to conjure the spirit of Robin Milner, or something.

I'm not that kind of guy, though. Computer science is wonderful, but my main concern in life is creating useful tools for the real world. Frankly, I don't know the first thing about automated theorem provers, not one course in my university teaches type theory or Prolog, and I had to read about monads at least 10 times before I "got" it.

I'll discuss EC# briefly, but my main goal here is to explain the grand ideas behind it, and perhaps recruit some bright people to work on it with me as volunteers. EC# rules are currently tentative and subject to change. Even the name is debatable; someone suggested I should call it "C Major", for example. But Enhanced C# and its abbreviation are Google-friendly, an important feature.

My real goal is to unify the computer industry and move it forward. I want to be able to use powerful programming languages that let me do things I couldn't easily do before; but I don't want to throw away my old code - I want to interoperate with it as easily and seamlessly as possible. Enhanced C# (and its components, such as Loyc trees and LeMP) are part of a collection of projects called Loyc; Loyc (Language Of Your Choice) is itself a nebulous idea with themes of interoperability and metaprogramming, and the goal of unifying the computer industry around powerful tools.

Quite frankly I think my goals are loftier than my abilities, but I can do a better job than most because I've been thinking about making a language for over ten years. I assume that the people who are most qualified to create something like Loyc are either (A) hashing out some aspect of type theory at a university, writing papers that no one outside their niche can understand, published in journals inaccessible to the public, or (B) making big bucks on proprietary tools like Resharper or the DMS Software Reengineering Toolkit. Nevertheless, EC# is my itch, so I'm scratching it.

Like C#, EC# is a statically typed object-oriented language in the C family. When complete, it should be about 99.7% backward compatible with C#. At first it will compile down to plain C#; eventually I want it to have a proper .NET compiler, and someday, a native-code compiler. EC# enhances C# with the following categories of features:

1. A procedural macro system
2. Compile-time code execution (CTCE)
3. A template system (the "parameterized program tree")
4. An alias system (which is a minor tweak to the type system)
5. Miscellaneous specific syntax enhancements
6. Miscellaneous semantic enhancements

Only item #1 and most of #5 currently exist. But EC# is much more powerful than C# with just the macro system and syntax extensions alone. The term "macro" comes from LISP; a macro is a method that runs at compile time that (usually) passes and returns code instead of normal data (in EC#, code is just data of type Node.)

EC# does not substantially change the type system, and the syntax is only slightly extensible because the need for backward compatibility with C# limits the possibilities. However, with the help of macros, EC# syntax is vastly more flexible than C#.

EC# is mainly a compile-time metaprogramming system built on top of C#, but it also provides lots of useful enhancements for people that are not interested in metaprogramming. Many of these enhancements are built using the metaprogramming facilities, but developers don't have to know or care about that.

"Metaprogramming" here refers to compile-time code generation and/or analysis; CTCE, templates and macros will each contribute in a different way to EC#'s metaprogramming system.

Here are some quick highlights. First, EC# offers some syntactic shortcuts compared to C#. Many shortcuts are offered by macros; others are built-in syntactic sugar.

	public struct MyPoint
	{
		// Simultaneously declares two fields "X" and "Y" and a constructor for them.
		// In general, EC# allows this() as a quicker syntax for writing constructors.
		// This feature is provided by a macro that watches all constructors (and 
		// methods) looking for parameters that have an attribute like "public".
		public this(public int X, public int Y);

		// You could make a test() macro for writing unit tests easily, and use it 
		// with syntax like this (where parentheses denote an assertion to check):
		test {
			var p = new Point(2, 3);
			(p.X == 2 && p.Y == 3);
			var q = p;
			q.X = 5;
			(p.X == 2);
		}
	}

	// Attaching a "field" attribute creates a property with a backing field of the
	// same type, although this has been largely superceded by a feature of C# 6.
	[protected field _foo]
	public int Foo { get; set; }

	// Method forwarding ("#" means "the thing with the same name", i.e. Min):
	int Min(int x, int y) ==> Math.#;
	static readonly int two = Min(12, 2);

	// Quick variable binding (creates a variable called "r" to hold the result)
	if (Database.TryRunQuery()::r != null)
		Console.WriteLine("Found {0} results", r.Count);

You can think of EC# syntax as "generalized C#"; the parser accepts almost anything that looks vaguely like C# code, and macros are required to convert code that EC# would not understand into code that has a well-defined meaning. In addition, the parser accepts a few things like ``a `plus` b`` and `obj(->string)` that don't look like traditional C#.

Compile-time code execution will "just work". The const() pseudo-function will force compile-time evaluation, and it is implied in any context where a constant is required:

	const double TripleWhammy = new[] { Math.PI, Math.E, Math.Sqrt(2) }.Sum();

The template system will complement the existing C# generics system. In C#, a generic method that compiles is guaranteed to work for all types that meet the method's constraints. That's a useful property, but it limits what you can do. Templates (marked with $ on the type argument) are handy when you need compile-time duck typing:

	public static T Sum<$T>(IEnumerable<T> list)
	{
		T sum = (T)0;
		foreach (var item in list)
			sum += item;
		return sum;
	}

.NET Generics are well-designed and bless their hearts I love 'em, but they can't do anything like this because "+" and "0" don't exist for all types, and any attempt to write a "generic" sum function is very clunky in one way or another. What you see here is a C++-style template, and it solves the problem neatly.


## Background ##################################################################

First of all, I'm a performance guy; I've had to optimize most of the programs I've ever written (from the old Super Nintendo emulator SNEqr to the FastNav GPS system, which is optimized for a 400MHz ARMV4I machine), so I'm in the habit of constantly thinking about performance. However, the go-to language for performance is C++... and I hate C++ the more I use it. C++ is horrible. It's ugly, it compiles really slowly, it's bureaucratic, it requires copious code duplication, and GCC's error messages are a cruel joke. I could go on.

Not long ago I discovered a practical alternative, D version 2, which I like to call D2 because Google doesn't understand "D". The delightful D language, which inspires EC# in many ways, can't replace C++ fast enough if you ask me. It has many great features including slices and ranges, compile-time reflection, lambdas as template arguments, string and template mixins, rudimentary SIMD support, and "alias this" which is sort of like overloading the "." operator.

However, it also has a lot of rough edges that make me uncomfortable, and since its compiler is written in C++, I am not interested in hacking on it myself. Besides compiler bugs and a general beta feel, it also can't target Android, doesn't really support dynamic loading of DLLs, doesn't have a runtime reflection system (though I'm sure the compile-time reflection is really nice if you can figure out how to use it), and has a rather basic garbage collector.

Perhaps all this will be solved in time, but I also really want an "extensible" language, and I have a clear impression that D's creator, Walter Bright, does not.

My need for an extensible language crystalized after I wrote an extension for the boo programming language that provided unit inference. You'd write x = y / 2`hr` in one place, and x = 160`km/hr` elsewhere, and hey presto, the compiler would infer that the variable y has units of `km`. If you then pass this to a function that expects `mph`, you get a compiler scolding. But when I anounced this creation on boo's mailing list, no one acknowledged it. Apparently it didn't matter to anyone there. Plus, it required a tweak to boo's syntax, but boo's creator or one of the developers (I forget) told me that there was no willingness to change the parser. Disappointed, I stopped working on the inference engine, and that was that.

Soon afterward I had the idea for Loyc: Language of your choice. It would be a system that lets individual users, not some slow and conservative language committee or ostensibly benevolent dictator, tweak the syntax and semantics of a language. This idea, however, puttered along very slowly because I had, besides a full-time job, no idea how to actually create an extensible language. How do you make a language in which lots of syntax and semantic extensions written by different people can magically get along? I kept asking myself that question and my brain was all like, ppffft, I dunno buddy!

I reduced my full-time job reduced to a part-time job as of August 2012. Inspired by D2 and a brief study of LISP, the ideas for EC# finally started crystalizing.


### Don't We Have Enough Languages Already?


The world is already awash with hundreds (thousands?) of programming languages, so when a new language comes out, it's always fair to question if it's needed.

EC#'s foremost feature, macros, is based on LISP, and it's fair to ask, why don't I just use LISP? Well, for one thing, it's dynamically typed and I prefer static typing. But it's not just that.

Throughout my programming career I've mostly gone with the flow, learning mostly popular and well-known languages (BASIC, Pascal, C, C++, Ruby, C#, etc.) But every so often someone like Paul Graham or a random guy on Slashdot gushes about one of those "alternative" languages like LISP or Haskell, or possibly Prolog, Erlang or OCaml... the kinds of languages you should learn "even if you never use it for real-world apps".

And I know they're right. I have studied LISP and Haskell enough to know that the concepts they teach are very important. But somehow, it seems like every time I sit down to study them, I can't bring myself to write real software with them.

Part of the problem is just that I've been using conventional languages so long that it's difficult to change. I know that the popular languages are limited beasts and full of warts, but I know many of those warts inside and out. I understand the way a computer works almost down to the level of logic circuits; I am comfortable with the imperative model of computing that maps so well to the underlying machine; I understand the performance characteristics of my languages; and I enjoy working in Visual Studio. It's difficult to step out of that comfortable world into something as alien as LISP or Haskell.

But it's not just that, either. There are two other problems with these powerful, but unpopular, languages that I'd like to highlight. I think these "powerful fringe languages", or PFLs, have two problems that keep them unpopular:

1. The communication gap
2. The integration gap

The goal of EC# is to be as powerful as LISP (not to mention D2), without having these two problems.

The **"communication gap"** is the gulf between the terminology and mindset teachers use to describe "far-out" languages, compared with the terminology and mindset that seasoned programmers already understand. It also refers to the way that some PFLs "abstract away the computer" so that one can no longer understand how a program works in terms of the physical machine (I am thinking of Haskell and Prolog here, not so much LISP); Thus, the better you understand a computer as a machine rather than as a mathematical tool, the more difficulty you have learning the PFL.

Whenever I went out to the web to learn about Haskell, the tutorials I found tended to treat me like a programming novice with a degree in mathematics, which, of course, is exactly backwards. From what I've see, tutorials about Haskell do not mention the memory model of the computer, or how an executable program is a sequence of little instructions that fetch data from memory and manipulate it, or pointers are combined with little objects on the "heap" to construct complex data structures. And why should they? Haskell is a "powerful" language that abstracts away these details far more than C, C++, Java, or even LISP. So instead they talk about how you can write math-like equations that describe the relationship between input and output; they talk about currying and partial application and recursion and higher-order functions and monads.

Reading about Haskell can be interesting, overwhelming and/or exhausting, and yet I walk away disappointed, unsure if I have really learned anything. Indeed, after several unsuccessful attempts to understand what a "monad" was, I finally found an read an article about them and thought: "oh, I see, yeah, I think I get it now". A month later I realized that I had completely forgotten what I had learned.

I have never seen a Haskell tutorial that talked about how a Haskell program is actually executed, how memory is managed, how to scale it to many processors or whether that is even possible, how to use hashtables in Haskell, or how the concept of typeclasses compares with the familiar concepts of classes, inheritance, and virtual function tables. See the communication gap? Haskell programmers speak a different language than seasoned "real-world" devs. This gap is especially wide for me: most of the programs I've written have needed high performance and low memory usage, and when I write Haskell, I have little confidence in my ability to achieve those things. Even the cost of basic properties like lazy evaluation is unknown to me. Plus, I have little confidence that algorithms based on arrays and hashtables can remain fast in Haskell.

The LISP family of languages, meanwhile, is (at least in some incarnations) closer to familiar imperative languages in terms of semantics, but its syntax is even more unusual than Haskell. And again, usually tutorials are written as if for programming novices, without relating LISP to popular languages whose names start with "J" or "C". Some LISP programmers even continue to use functions like "car" and "cdr" which are utterly meaningless outside the LISP world, the automotive world, and the optical media world. You could make similar complaints about C functions like "strstr" and "atoi", of course; the difference is that C can get away with a lot of stupid crap because it's already popular.

Indeed, the very fact that LISP continues to persist using s-expression syntax when perfectly good alternatives exist (e.g. sweet expressions) makes it obvious why LISP has not taken off. LISPers insist on communicating in their own special way, and it puts people off.

One of LISP's main advantages is its macro system, which allows you to easily manipulate syntax trees at compile-time. This allows you to automate the task of writing sequences of code that are similar, but not similar in a way that allows the similar sequences to be combined into a single function or a C++ template. A simple example of this would be if you have a series of 3-dimensional (X, Y, Z) points. Suppose that sometimes you need to manipulate the X axis, sometimes the Y axis, sometimes the Z axis, and sometimes all three (a k-d tree works this way, for example); in a conventional language, the only practical alternative is to use an array instead of naming each coordinate; but in many languages this is inefficient because the array is stored in a separate heap object from the "Point" object and then requires a bounds check on every access. In situations like this, I always give up and write separate, nearly identical, copies of the necessary code. LISP, though, solves this kind of problem easily via macros, with zero runtime cost.

Anyway, I find it ironic that while LISP has an unparalleled ability to manipulate syntax trees, making new forms of automation possible, it has no standard ability to manipulate syntax itself, forcing LISP programmers to "think like the machine" in a way that other languages don't. When it comes to creating a program, LISP says: doing a dull, repetitive task? No, no, let the computer handle that for you! But they still ask you to perform the dull, repetitive mental task of parsing LISP. Of course, humans are made for parsing, so I'm sure we could get used to LISP--someday. But while waiting for someday to come, LISP-based languages languish in obscurity.

Now, sure, you could go to some extra effort to install a special LISP reader that supports infix syntax. But when you go read a LISP tutorial, that won't be the syntax they use to teach it. When you go to a LISP mailing list, that won't be the syntax of all the code there. When you download a large LISP program, same thing. In theory you can transform LISP to any syntax you want, but as a student this fact doesn't help at all.

Finally, I think I should take a moment to unfairly criticize Microsoft's own F#, too. I tried to learn it--twice--but I found its syntax very confusing; for instance I couldn't understand why there were such strikingly different syntaxes for defining functions that do the same thing ("static member inc(x) = x+1", "let inc x = x+1", "fun x -> x+1"), I couldn't figure out what parts were the "core" language and what parts were syntactic sugar, and I had a had a hell of a time trying to write a constructor that would compile without syntax errors. Since F# is impure and based on the .NET type system which I already grok, the problems I faced were purely matters of communication; the web-based tutorials I read simply didn't tell me what I wanted to know (if you get that vibe from EC#, by the way, let me know what to clarify!)

EC# solves the communication gap by providing LISP-like macros and other new features on top of the familiar syntax of C#. And by converting the code to plain C#, you can see the effect of any macro or syntactic sugar.

The **"integration gap"** is just as important: it refers to the difficulty of combining PFLs into existing systems and code bases.

The problem is that you can't just mash up LISP with Haskell or Haskell with Ruby or the language of your choice. I mean, I've already got a nice personal toolbox of C++ code and C# code. Some of it was written by me, some of it by third parties. Wherever it came from, I want to be able to simply use that existing code, without any hassles, from my PFL program. And I can't. Equally important, even if I'd like to write a brand new code library or program with no dependencies on existing code, the reverse problem appears: I can't take my new library or parts of that program and import it into one of the popular languages.
 
EC# solves the integration gap by 

1. being backward compatible with C# and by
2. being callable from all other .NET languages.

And hopefully someday, other projects in the [Loyc](http://loyc.net) (Language of your choice) ecosystem will tackle the integration gap in other ways.

EC# is not based on C# because I love C# (although it is my second-favorite language after D - then again, Swift just came out, and I admit I haven't learned it properly yet). EC# is based on C# because I believe it is the best way to build an ecosystem of users and tools.

Please don't get the impression that I don't like Haskell, by the way, or LISP for that matter. No, Haskell are really neat, but there's a gap that needs to be bridged. In fact, I hope that Loyc will evolve into a system in which most code is functional code. However, I think that the functional language should be built on top of an imperative language. I imagine a set of macro libraries that transform functional code back into imperative code, e.g. replacing tail-recursion with looping, and copying with in-place modification where possible, with intermediate code that the programmer can readily see and understand. If he is unhappy with the performance of the functional code, he can change the functional code until the imperative equivalent works as desired; he could even replace the functional code with imperative equivalent and hand-optimize it.

### Ahem!

And now, let us pause for a moment of code examples.

	// Element-in-set test
	if (x in (2, 3, 5, 7))
		Console.WriteLine("Congratulations, it's a prime! One digit, even! or odd!");

	// Tuples and tuple unpacking: x is an existing variable, y is a new one.
	(x, double y) = (a * b, a + b);

	// Safe navigation operator (textBox?.Text means textBox==null?null:textBox.Text):
	assert(textBox?.Text == model.Value.ToString());
	var firstPart = orderNo?.Substring(0, orderNo.IndexOf("-"));

	// Switch without braking, er, breaking:
	switch (choice) {
		case "y", "Y": Console.WriteLine("Yes!!");
		case "n", "N": Console.WriteLine("No!!");
		default:       Console.WriteLine("What you say!!");
	}

	// Statements as expressions (not yet implemented):
	Console.WriteLine(
		switch (choice) {
			case "y", "Y": "Yes!!"
			case "n", "N": "No!!"
			default:       "What you say!!"
		});
	
	// Expressions as statements (needs a macro to transform it into something useful):
	x => y | z;

	// "using" cast operator: allows a conversion only when it is guaranteed to succeed
	int a = 7, b = 8;
	var eight = Math.Max(a, b using double); // 'eight' has type double

	// Symbols (a kind of extensible enum)
	var sym = @@ThisIsASymbol;

  // Pattern matching EC# or LES code files
  double Eval(LNode code) {
    matchCode(node) {
      case $x + $y:  return Eval(x) + Eval(y);
      case $x * $y:  return Eval(x) * Eval(y);
      case -$x:      return -Eval(x);
      case $x == $y: return Eval(x) == Eval(y) ? 1.0 : 0.0;
      case $x >= $y: return Eval(x) >= Eval(y) ? 1.0 : 0.0;
      case $x >  $y: return Eval(x) >  Eval(y) ? 1.0 : 0.0;
      case $x = $y:            // TODO: assignment operator
      case { while ($cond) $body; }: // match while loop
        while(Eval(cond) != 0)
          Eval(body);
      case { { $(..body } }:         // match braces
        double result = double.NaN;
        for (var stmt in body)
          result = Eval(stmt);
        return result;
      default:
        if (node.IsLiteral)
          return Convert.ToDouble(node.Value);
        else
          // TODO
    }
  }

TODO: move discussion about macros elsewhere & revise it

	// This is a macro definition (a more compact way to write macros may come later).
	[LexicalMacro("static_assert(condition);", 
	 "Shows an error message at compile time if the specified condition is false.")]
	public static LNode static_assert(LNode call, IMacroContext ctx)
	{
    matchCode (call) {
      case $_($condition):
        return quote {
          static_if(!$condition) {
            @#error("Static assertion failed: " + stringify($condition));
          }
        };
    }
    return null; // reject the input (in this case, without giving a reason)
	}

A LISP-style macro is a function that takes a syntax tree as input, and returns another syntax tree as output. Early in the compilation process, before semantic analysis, the compiler scans the code for macro calls, invokes them, and replaces each macro call with its output.

In this case, an input like

    static_assert (Math.PI > 3);

is replaced by

    static_if(!(Math.PI > 3)) {
        @#error("Static assertion failed: " + stringify(Math.PI > 3));
    }

At the moment, a macro is defined as an ordinary method with a [LexicalMacro] attribute that allows it to also be treated as a macro. You must currently compile your macros as a separate step (normally in a separate assembly) before you can call those macros. You then add the macro assembly as a compile-time reference and use the macros in another assembly (TODO: add details)

The argument `call` is the entire syntax tree of the macro and its arguments, including the `static_assert` identifier itself, so `matchCode` is used to extract the `condition` (simply writing `LNode condition = call.Args[0]` would also work). `quote {...}` creates a syntax tree for a block of code, and it looks for the `$` operator which is used for substitution. So in this case the `condition` expression is substituted into the output.

	static_assert(Math.PI > 3);
    
becomes

	static_if(!(Math.PI > 3)) {
		@#error("Static assertion failed: {0}", quote(Math.PI > 3));
	}

static_if() is itself a macro, which collapses to nothing (vanishes) at compile-time because !(Math.PI > 3) is false. Macros can be called at the class level, outside of any method, so static_if can be used to decide whether to create a method or not.

static_if() uses syntactic sugar in order to take the braced block {...} as an argument. The parser automatically transforms statements of the form

	foo(expr) { stmt; }
	
into

	foo(expr, { stmt; });

where {...} denotes a list of statements. This transformation occurs without knowing whether "foo" is a macro or not, but normally it is.

@#error is a special built-in pseudo-method that prints a compile-time error. Note that "#error" cannot be used here because it would be parsed as a preprocessor directive, so the error would be printed by the preprocessor before the macro is even parsed, let alone invoked. The "@" sign marks "#error" as an identifier, and the "#" sign indicates that the identifier has some special meaning (in this case, the compiler recognizes #error as a directive to print a compile-time error).

When EC# is complete it will allow global operators, too, so EC# can serve as a do-it-yourself MATLAB:

	public T[] operator+ <$T>(T[] a, T[] b)
	{
		if (a.Length != b.Length)
			throw new ArgumentException("operator+: array lengths differ");
			
		T[] result = new T[a.Length];
		for (int i = 0; i < a.Length; i++)
			result[i] = a[i] + b[i];
		return result;
	}
	public T[] a<T>(params T[] list)
	{
		return list;
	}

	var fivePrimes = a(1,2,3,4,5) + a(1,1,2,3,7);

Note that since "operator+" appears outside any class, it is implicitly static; you don't have to tell the compiler the obvious.


### Why .NET?

You may wonder, if I care so much about performance, why do I want a .NET language?

First of all, at least on Windows, .NET's performance isn't bad at all. As for Mono... well, I'm sure it'll improve someday.

But the really key thing I like about .NET is that it is specifically a multi-language, multi-OS platform with a standard binary format on all platforms--much like Java, but technically superior. It's only "multi-OS", of course, insofar as you avoid Windows-only libraries such as WPF and WCF, and I would encourage you to do so (if your boss will allow it). .NET solves the "integration gap" as long as the languages you want to mash up are available in .NET.

Without a multi-language runtime, interoperability is limited to the lowest common denominator, which is C, which is an extremely painful limitation. Modern languages should be able to interact on a much higher level.

A multi-language platform avoids a key problem with most new languages: they define their own new "standard library" from scratch. You know what they say about standards, that the nice thing about standards is that there are so many to choose from? I hate that! I don't want to learn a new standard library with every new language I learn. Nor do I want to be locked into a certain API based on the operating system (Windows APIs and Linux APIs). And all the engineering effort that goes into the plethora of "standard" libraries could be better spent on other things. The existence of many standard libraries increases learning curves and severely limits interoperability between languages.

In my opinion, this is the most important problem .NET solves; there is just one standard library for all languages and all operating systems (plus an optional "extra" standard library per language, if necessary). All languages and all operating systems are interoperable--it's really nice! The .NET standard library (called the BCL, base class library) definitely could be and should be designed better, but at least they have the Right Idea.

Java has the same appeal, but it was always designed for a single language, Java, which is an unusually limited language. It lacks several important features that .NET has, especially value types, reified generics, pass-by-reference, delegates, and "unsafe" pointers. It doesn't matter if you switch to a different JVM language, those limitations have a significant cost even if the language hides them.

And C# is likeable. It's not an incredibly powerful language, but it's carefully designed and it's more powerful and more efficient than Java. Similarly for the .NET CLR: yeah, it has some significant limitations. For example, it has no concept of slices or Go-style interfaces. And the BCL is impoverished in places--its networking libraries are badly designed, its newest libraries are horribly bloated, and lots of important stuff is still missing from the BCL. Even so, in my opinion .NET is currently the best platform available.


## Welcome to EC# 2.0

When I began to design EC#, I couldn't figure out how to accomplish my end goal--a fully extensible language--so instead I decided, as a starting point, that I would simply improve the existing C# language with a series of specific and modest features such as the "null dot" or safe navigation operator (now denoted ??.), the quick-binding operator (::), multiple-source name lookup (which, among other things, basically allows static extension methods), the "if" clause and method forwarding clauses, return value covariance, CTCE, aliases, and most notably, compile-time templates. My plan was to start with NRefactory in order to get the new language working as quickly as possible.

Once I was done drafting the new language, however, I noticed that despite all the work I'd put into the draft alone, the new language still didn't address one of the most requested features from C# users: "Provide a way for INotifyPropertyChanged to be implemented for you automatically on classes".

INotifyPropertyChanged is a simple interface for allowing code to subscribe to an object to find out when it changes. For example:

	interface INotifyPropertyChanged {
		event PropertyChangedEventHandler PropertyChanged;
	}
	class Person : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		
		string _name;
		public string Name
		{
			get { return _name; }
			set {
				if (_name != value) {
					_name = Value;
					Changed("Name");
				}
			}
		}
		string _address;
		public string Address
		{
			... same thing ...
		}
		DateTime _dateOfBirth;
		public DateTime DateOfBirth
		{
			... same thing ...
		}
		void Changed(string prop)
		{
			if (PropertyChanged != null)
				// The .NET "EventArgs" concept is stupid, but I digress
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
		}
	}

The problem is that you need 10 new lines of code for every new property in your class. Couldn't the language have some kind of feature to make this easier? But I didn't want to have a feature that was specifically designed for INotifyPropertyChanged and nothing else. Besides, there are different ways that users might want the feature to work: maybe some people want the event to fire even if the value is not changing. Maybe some people want to do a reference comparison while others want to do object.Equals(). Maybe it can't be fully automatic--some properties may need some additional processing besides just firing the event. How could a feature built into the language be flexible enough for all scenarios?

I decided at that point that it was time to learn LISP. After studying it briefly, I figured out that what would really help in this case is macros. Macros would allow users to provide a code template that is expanded for each property. But ideally, LISP-style procedural macros also require some straightforward representation of the syntax tree so that syntax trees can be easily manipulated.

I suppose in the case of INotifyPropertyChanged, a macro doesn't really have to introspect a syntax tree, it just need a way to "plug things in" and some sort of trickery to convert an identifier to a string and back. Something like this:

	[SimpleMacro] 
	Node NPC(Node dataType, Node propName, Node fieldName)
	{
		string nameText = propName.ToString();
		return s_quote {
			// The substitution operator "$" inserts code from elsewhere.
			public $dataType $propName {
				get { return $fieldName; }
				set {
					if (!object.Equals($fieldName, value)) {
						$fieldName = value;
						Changed($nameText);
					}
				}
			}
		};
	}

	/* Usage: */
	class Person : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;
		
		string _name;
		NPC(string, Name, _name);
		string _address;
		NPC(string, Address, _address);
		DateTime _dateOfBirth;
		NPC(string, DateOfBirth, _dateOfBirth);
		...
	}

This feature would need substantial changes to NRefactory's parser, so it was looking quite difficult given that didn't have a clue how to user NRefactory's parser generator (called Jay). But for my macro system to be *really* powerful, it needed powerful tools to manipulate syntax trees. So I re-examined my whole plan--maybe I shouldn't start with NRefactory after all--and I thought about what kind of syntax tree would be best in a really good macro system.

I considered using LISP-style lists, but they didn't fit into C# very well. In particular I didn't feel that they were the right way to model complex C# declarations such as 

	[Serializable] struct X<Y> : IX<Y> where Y : class, Z { ... }

Besides, LISP lists don't hold any metadata such as the original source file and line number of a piece of code. And how would I address the needs of IDEs--incremental parsing, for example? So I fiddled around with different ideas until I found something that felt right for C# and other languages descended from Algol. Eventually I decided to call my idea the "Loyc node" or "Loyc tree".

The concept of a Loyc node involves three main parts: a "head", an "argument list" and "attributes".

I'll talk more about Loyc trees later. With my new idea ready, I decided to ditch my ideas for EC# 1.0 and go straight to the language of my dreams, EC# 2.0. No longer based on NRefactory, it would use a new syntax tree, a brand new parser, and a new set of features.

Footnote: the Loyc tree was originally two ideas that I developed concurrently: one idea for the concept of the tree as viewed by an end-user (i.e. other developers), and another idea for how the implementation works. The implementation originally involved two parallel trees, "red" and "green", inspired by a similar idea in Microsoft Roslyn, but I had difficulties with this approach and reverted to a simpler "just green" design in which the tree is entirely immutable and parents of nodes are unknown.
