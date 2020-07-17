---
title: "LeMP Home Page"
layout: page
tagline: "the Lexical Macro Processor for C#"
toc: false
---
## Introduction ##

LeMP is an open-source LISP-style macro processor for C#, comparable to [sweet.js](http://sweetjs.org/) for Javascript. Install it in Visual Studio to help you write boilerplate.

<div class='sbs' markdown='1'>

~~~csharp
public static partial class ExtensionMethods
{
  define GenerateInRangeMethods($Num)
  {
    // Returns true if num in range lo..hi
    public static bool IsInRange
      (this $Num num, $Num lo, $Num hi)
      => num >= lo && num <= hi;
    public static $Num PutInRange
      (this $Num n, $Num min, $Num max)
    {
      if (n < min)
        return min;
      if (n > max)
        return max;
      return n;
    }
  }

  GenerateInRangeMethods(int);
  GenerateInRangeMethods(long);
  GenerateInRangeMethods(double);
}
~~~

~~~csharp
// Generated from Untitled.ecs by LeMP 2.6.4.0.
public static partial class ExtensionMethods
{
  // Returns true if num in range lo..hi
  public static bool IsInRange
    (this int num, int lo, int hi) => 
    num >= lo && num <= hi;
  public static int PutInRange
    (this int n, int min, int max)
  {
    if (n < min)
      return min;
    if (n > max)
      return max;
    return n;
  }
  // Returns true if num in range lo..hi
  public static bool IsInRange
    (this long num, long lo, long hi) => 
    num >= lo && num <= hi;
  public static long PutInRange
    (this long n, long min, long max)
  {
    if (n < min)
      return min;
    if (n > max)
      return max;
    return n;
  }
  // Returns true if num in range lo..hi
  public static bool IsInRange
    (this double num, double lo, double hi) => 
    num >= lo && num <= hi;
  public static double PutInRange
    (this double n, double min, double max)
  {
    if (n < min)
      return min;
    if (n > max)
      return max;
    return n;
  }
}
~~~
</div>

LeMP helps you solve the **repetition-of-boilerplate** problem, and it allows you to transform code at compile-time in arbitrary ways.

<div class="sidebox" style="max-width:231px;"><img src="lemp-sidebar.png" style="max-width:100%; max-height:100%;"/></div>

### Example: compile-time code execution and code manipulation ###

LeMP can be used for compile-time calculations, reflection and code generation. It is possible to use ordinary [reflection](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/reflection), but this example shows off more features of LeMP. It scans a syntax tree at compile-time and produces a list of the function names that it saw.

~~~csharp
compileTimeAndRuntime {
  // This code is executed at compile-time and also included in the output
  using System;
  using System.Collections.Generic;
  using System.Linq;
}
compileTime {
  // This code is executed at compile-time but not included in the output
  static List<string> GetFunctionNames(LNodeList input) {
    var functionNames = new List<string>();
    // Scan the list of statements we were given and find functions among them
    foreach (LNode part in input) {
      // Use the matchCode macro to detect anything that looks like a function
      matchCode(part) {
        // $name means "create a variable representing the code at this location".
        // [$(..attributes)] means "capture attributes attached to this construct".
      	case { [$(..attributes)] $retVal $name($(..argList)) => $(body); }:
      		functionNames.Add(name.Name.Name);
      }
    }
    return functionNames;
  }
  
  static LNode WithMetadata(LNode input) {
    // quote { } produces a single node when the input was a list. 
    // Convert input back to a list.
    var names = GetFunctionNames(input.AsList(CodeSymbols.Splice));
    return quote {
      static string[] metadata = new[] {
      	$(..names.Select(name => LNode.Literal(name)))
      };
      $input;
    };
  }
}
// This creates a user-defined macro called withMetadata
define withMetadata({ $(..code); }) {
  // `precompute` runs an expression at compile time and replaces itself with 
  // its own result, which can either be a primitive (e.g. number or string)
  // or a syntax tree
  precompute(WithMetadata(quote { $code; }));
}

class Example {
  withMetadata {
    public static int Square(int x) => x*x;
    public static int Cube(int x) => x*x*x;
    public static int DoNothing() {}
    static int variable;
  }
}
~~~

Output:

~~~csharp
using System;
using System.Collections.Generic;
using System.Linq;
class Example {
	static string[] metadata = new[] { 
		"Square", "Cube", "DoNothing"
	};
	public static int Square(int x) => x * x;
	public static int Cube(int x) => x * x * x;
	public static int DoNothing() { }
	static int variable;
}
~~~

Need help using these features? Please ask on StackOverflow (put the `lemp` tag on your question and I will see it.)

Note: `compileTime` and `precompute` are based on the C# Interactive engine, which has some limitations (e.g. namespaces are not supported). Also, notice that `using` statements are ignored at compile-time unless they are inside a `compileTimeAndRuntime` or `compileTime` block. This is because assemblies used by your project are not automatically referenced at compile time, so the namespaces that you use at runtime might not exist at compile-time.

### Example: using ###

A really simple example is 'using' statements:

~~~csharp
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
~~~

Luckily, Visual Studio can add these for us. But wouldn't it be nice if half the screen wasn't 'using' statements every time you open a file? There is a LeMP macro that lets you collapse these onto a couple of lines:

~~~csharp
using System(.Linq, .Text, .Collections(, .Generic), .IO, );
using Loyc(.Collections, .MiniTest, .Syntax);
~~~

The comma `,` before the closing `)` adds an "empty" parameter to the list, which indicates that `using System` itself is one of the outputs you want to produce.

### Example: Small data types ###

I like to create a lot of small data types, rather than using a few huge ones. And when you're making small data types, C# is annoying. A simple type isn't hard:

~~~csharp
public class Person {
	public string Name;
	public DateTime DateOfBirth;
	public List<Person> Children;
};
~~~

But this simplicity has a big price:

- There's no constructor, so you must always use property-initializer syntax to create one of these. That could get old fast. And if you ever _add_ a constructor later, you might have to change every place where you created one of those types.
- Since there's no constructor, you can't easily validate that valid values are used for the fields, and none of your fields have mandatory initialization.
- Many of the best developers say you should make your fields read-only if possible. And the style police say you should make them properties instead of fields.

So, you probably need a constructor. But adding a constructor is a pain!

~~~csharp
public class Person
{
	public string Name           { get; }
	public DateTime DateOfBirth  { get; }
	public List<Person> Children { get; }
	public Person(string name, DateTime dateOfBirth, List<Person> children)
	{ 
		Name = name;
		DateOfBirth = dateOfBirth;
		Children = children;
		// TODO: Add validation code
	}
}
~~~

It's too much repetition!

- You repeat the class name twice.
- You repeat each data type twice.
- You repeat each property name twice.
- You repeat the name of each constructor parameter twice.
- You repeat "public" for each field (and more, if they are properties)

LeMP solves these problems with a combination of (1) a macro, and (2) a little syntactical "makeover" of C#. In LeMP you'd write this:

~~~csharp
public class Person
{
	public this(
		public string Name           { get; },
		public DateTime DateOfBirth  { get; },
		public List<Person> Children { get; })
	{
		// TODO: Add validation code
	}
}
~~~

Your output file will contain exactly the code listed above, and there is no repetition except for `public .. { get; }` (but you might not want everything to be public anyway). Great! 

What's going on? Enhanced C# includes two syntax changes to support this, each with a supporting macro:

1. To reduce repetition and ambiguity, Enhanced C# allows `this` as a constructor name (a feature borrowed from the [D language](http://dlang.org)). A macro changes `this` into `Person` so that plain C# understands it.
2. Enhanced C# allows property definitions as method parameters (or wherever an expression is allowed). A macro is programmed to notice properties, and visibility attributes (like `public`) on variables. When it notices one of those, it responds by transferring it out to the class, and putting a normal argument in the constructor. Finally, it adds a statement at the beginning of the constructor, to assign the value of the argument to the property or field.

### Example: parsing ###

The biggest macro packaged with LeMP is a parser generator called [LLLPG](http://ecsharp.net/lllpg/). This example defines `EmailAddress.Parse()`, which parses an email address into `UserName` and `Domain` parts:

~~~csharp
using Loyc.Syntax; // Get the Loyc.Syntax package from NuGet
#importMacros(Loyc.LLPG);

struct EmailAddress
{
   public EmailAddress(public UString UserName, public UString Domain) {}
   public override string ToString() { return UserName + "@" + Domain; }

   LLLPG (lexer(inputSource: src, inputClass: LexerSource)) {
      // LexerSource provides the runtime APIs that LLLPG uses. This is
      // static to avoid reallocating the helper object for each address.
      [ThreadStatic] static LexerSource<UString> src;
   
      public static rule EmailAddress Parse(UString email) @{
         {
            if (src == null)
               src = new LexerSource<UString>(email, "", 0, false);
            else
               src.Reset(email, "", 0, false);
         }
         UsernameChars ('.' UsernameChars)*
         { int at = src.InputPosition; }
         '@' DomainCharSeq ('.' DomainCharSeq)* EOF
         {
            UString userName = email.Substring(0, at);
            UString domain = email.Substring(at + 1);
            return new EmailAddress(userName, domain);
         }
      }
      static rule UsernameChars() @{
         ('a'..'z'|'A'..'Z'|'0'..'9'|'!'|'#'|'$'|'%'|'&'|'\''|
         '*'|'+'|'/'|'='|'?'|'^'|'_'|'`'|'{'|'|'|'}'|'~'|'-')+
      };
      static rule DomainCharSeq() @{
               ('a'..'z'|'A'..'Z'|'0'..'9')
         [ '-'? ('a'..'z'|'A'..'Z'|'0'..'9') ]*
      };
   }
}
~~~

Learn more
----------

Learn more about LeMP in these published articles:

- [Avoid tedious coding with LeMP](avoid-tedium-with-LeMP.html)
- [Using LeMP as a C# code generator](lemp-code-gen-and-analysis.html)
- [C# Gets Pattern Matching, Algebraic Data Types, Tuples and Ranges](pattern-matching.html)

Macro reference manual
----------------------

- [Reference manual: main page](reference.html)
    - [Built-in macros](ref-builtin-macros.html)
    - [Code Contracts](ref-code-contracts.html)
    - [on_return, on_throw, on_throw_catch, on_finally](ref-on_star.html)
    - [Code generation & compile-time decision-making](ref-codegen.html)
    - [Other macros](ref-other.html)

More links
----------

- [Download & installation](install.html)
- [FAQ](faq.html)
- [Version history](version-history.html)
- [Source code](https://github.com/qwertie/Loyc/tree/master/Main/LeMP)

Help wanted
-----------

Do you have time to [make LeMP better](/help-wanted.html)? 

Integration into Visual Studio is basic at the moment; help wanted if you have skill in writing extensions.
