---
title: C# 6 versus Enhanced C#
layout: post
---

With some C# 6 features semi-officially announced, it's time to comment on C# 6 and how it affects Enhanced C#.

## 1. New initializer syntax

~~~csharp
var cppHelloWorldProgram = new Dictionary<int, string>
{
  [10] = "main() {",
  [20] = "    printf(\"hello, world\")",
  [30] = "}"
};
~~~

It's not like Microsoft to do minor syntax tweaks with no major advantage, but that's exactly what this seems to be. This is just an improvement over the old syntax, whose meaning was less apparent:

~~~csharp
var cppHelloWorldProgram = new Dictionary<int, string>
{
  {10, "main() {"},
  {20, "    printf(\"hello, world\")"},
  {30, "}"}
};
~~~

_Edit_: It's not _quite_ the same; The old syntax `{key, value}` calls `.Add(key, value)`, while the new syntax calls `[key] = value`. Which means that the new syntax will not cause a runtime error in case of duplicate keys.

At first glance, the new syntax appears to be in conflict with EC#'s ability to attach attributes to any expression. Isn't it a problem that [...] looks like an attribute? I don't think so; my parser can easily look ahead and see the '=' to determine that it is not an attribute.

## 2. Dollar-sign operator

This is a weird one to me. In Enhanced C#, $ means "substitute" and will be used inside code literals to represent "capturing" or "expanding" an expression or statement; I was also planning to use it to identify a C++-style template parameter.

The C# team has more banal ideas for the dollar sign: It appears that $Foo can be expanded lexically to `["Foo"]`. For example, `X.$Y = Z` means `X["Y"] = Z`. Perhaps it is not a true operator, but just a lexical shortcut. Nevertheless, so far it looks like the EC# parser can already parse it without difficulty, and `$X` could be translated to `["X"]` somewhere in semantic analysis.

Using both the C# 6 and EC# definitions of `$` in a single language looks doable, but it doesn't feel right. Maybe I'll give it some thought later.

## 3. Auto-Properties with Initializers

    private List<T> InternalCollection { get; } = new List<T>; 

Okay, that's handy. I suppose I can dig it. This could be supported in EC# as a straightforward change to the property declaration parser.

## 4. Primary constructors

    [Serializable]
    public class Patent(string title, string yearOfPublication)
    {
      public Patent(string title, string yearOfPublication,
        IEnumerable<string> inventors) : this(title, yearOfPublication)
      {
        Inventors.AddRange(inventors);
      }
      public string YearOfPublication { get; set; } = yearOfPublication;
      private string _title = title;
      public string Title {
        get { return _title; }
        set {
          if (value == null)
            throw new ArgumentNullException("Title");
          _title = value;
        }
      }
      public List<string> Inventors { get; } = new List<string>();
    }

This class has two constructors: one primary constructor and one normal constructor. The arguments on the `Patent` class are transformed into a constructor by the compiler. If a class or struct has a primary constructor, all other contructors must call it. Unfortunately, you cannot provide a body for the constructor, but you can use the parameter values when initializing members of the type.

Although this example comes from a [Microsoft spokesman](http://msdn.microsoft.com/en-us/magazine/dn683793.aspx), it appears to have a flaw: when calling the primary constructor, there is no check that `title` is not null.

First of all, I believe the way this feature works is a mistake because it does not directly support lowering, despite [Eric Lippert advocating it in his recent blog post](http://ericlippert.com/2014/04/28/lowering-in-language-design-part-one/). Consider this example:

    class Foo(int x) {
      public int X { get; } = x;
    }

I heard it said that X is read-only in a normal constructor; thus primary constructors cannot be lowered to normal constructors. Besides, the lowering process is relatively complicated, since it would have to gather up the fields that use the constructor arguments and moved those initializers to the primary constructor. <div class="sidebox">Mind you, perhaps a similar lowering behavior (for normal constructors) might already have existed in the compiler, even though such a lowering cannot be expressed in the C# language.</div>

Secondly, as [Jon Skeet mentioned](msmvps.com/blogs/jon_skeet/), the feature is not usable in various circumstances because it is overly limited:

- The primary constructor must be essentially `public` (or `internal` if the class is `internal`). You might not want that!
- The primary constructor cannot (easily) add validation code to check the arguments and it cannot add pre- or post-processing code to run before/after the class members are initialized.
- All other constructors are forced to call the primary constructor (granted, it's rare that you wouldn't want to).

I don't believe in creating limited features that are only useful in narrow scenarios. That's why I planned something quite different for Enhanced C#.

The EC# version of the `Patent` class might look like this:

    [Serializable]
    public class Patent
    {
      public this(
        private string Title { get; [required] set; },
        private string YearOfPublication { get; set; }
      ) {}
      public this(set string Title, set string YearOfPublication, IEnumerable<string> inventors)
      {
        Inventors.AddRange(inventors);
      }
      [field _inventors = new List<string>()]
      public List<string> Inventors { get; }
    }

I realize that this may look weird at first, but bear with me.

First of all, "this" just means "define a constructor". One of the annoying things about constructors is that you must repeat the class name, no matter how long it is (plus, you cannot text-search to find all constructors in a file or project). The D language already solved this problem by using `this` as the constructor name instead.

Second, you can use a field or property declaration as a constructor argument, which is interpreted to mean "create this field or property _and also_ create a matching parameter and assign it". So this constructor does three things at once:

      public this(
        private string Title { get; [required] set; },
        private string YearOfPublication { get; set; }
      ) {}

1. It declares two properties, `Patent.Title` and `Patent.YearOfPublication`
2. It declares two constructor parameters, `title` and `yearOfPublication` (the compiler would rename the format parameter to have a lowercase first letter, since uppercase parameter names are unconventional. This only makes a difference if you use keyword arguments.)
3. It implicitly sets `this.Title = title` and `this.YearOfPublication = yearOfPublication`

You can also use the "set" prefix on any parameter (even in normal methods), which means "assign this parameter to the matching field or property, without creating a new field or property". That's what the other constructor does.

Finally, the `[required]` attribute will mean "throw `ArgumentNullException` if the parameter is null", which eliminates some extra code from the C# 6 `Patent` class.

Although the C# 6 syntax is slightly more elegant, notice that EC#'s syntax removes all the limitations I mentioned:

- The "primary" constructor does not have to be `public` (and since it is no longer a special constructor, we can stop calling it "primary").
- The "primary" constructor can easily add validation code to check the arguments and do any other necessary initialization.
- The other constructors are not forced to call it.

Also, a compiler can easily "lower" my version of the syntax down to normal constructors; since EC# is inspired by LISP, easy lowering is my modus operandi whenever possible.

## 5. Static `using` statements

    using System.Console;

I've always wanted a feature like this; my only comment is you should at least be _allowed_, if not required, to write `static using` to differentiate it from a normal `using`.

But on a related note, most C# files import a lot of namespaces, to the point where the screen is initially filled with `using` statements when you first open a file.

The EC# parser already accepts this syntax:

    using System.(this, Collections, Collections.Generic, Linq, Xml);

Now it's just a matter of somebody writing a macro to lower this into

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;

## 6. Variable declaration Expressions

This feature is hugely welcome! It can be considered two separate features:

    // 1. Declaring a variable to accept an "out" parameter:
    if(!Enum.TryParse<FileAttributes>(attributeName, out var attributeValue))
    {
      // 2. Declaring a variable and immediately assigning it with "="
      result = string.Format(
        "'{0}' is not one of the possible {2} option combinations ({1})",
        attributeName, 
        string.Join(",", 
          string[] fileAtrributeNames = Enum.GetNames(typeof(FileAttributes))),
        fileAtrributeNames.Length);
    }

This was one of the first features I thought of for EC#. But I also developed a more succinct notation for the second feature:

    // C# 6 (also supported in EC#)
    if ((var files = Directory.GetFiles(folderName)).Length != 0)
      Trace.WriteLine("Folder not empty, contains: "+string.Join(", ", files));

    // EC#
    if (Directory.GetFiles(folderName)::files.Length != 0)
      Trace.WriteLine("Folder not empty, contains: "+string.Join(", ", files));

Notice that EC# notation does not require extra parenthesis. In my opinion, this makes the feature _feel_ less clunky.

My version of the feature scoped the variable to the containing block, meaning that this:

    if ((var files = Directory.GetFiles(folderName)).Length != 0)
      Trace.WriteLine("Folder not empty, contains: "+string.Join(", ", files));

was _exactly_ equivalent to

    var files = Directory.GetFiles(folderName);
    if (files.Length != 0)
      Trace.WriteLine("Folder not empty, contains: "+string.Join(", ", files));

This definition made the code more straightforward to convert to C# 4/5, and it also meant that you could do something with the variable after the "if" statement, which I think is often useful. C# 6, however, scopes the variable to the "if" statement itself. Therefore, the definition of EC# will have to change to match C# 6.

## Exception-Handling Improvements

In C# 6 you can use `await` in catch and finally blocks. Nuff said. You can also write exception filters:

    try {
      throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    catch (Win32Exception exception) 
      if (exception.NativeErrorCode == 0x00042) {}

This is a welcome feature, but the syntax conflicts with EC#, since EC# does not require braces around the `try` or `catch` blocks. Oh well, I guess I'll have to start requiring braces now.

## Improved numeric literals

    int TwoHundredMillion = 200_000_000; // underscores for readability
    int OneHundred        = 0b0110_0100; // binary notation

The EC# lexer already supports these two features. It also supports hex float literals. Where are _your_ hex float literals, C# 6?

## Multiple expressions separated by semicolons

Eric Lippert [says](http://ericlippert.com/2014/05/01/lowering-in-language-design-part-two/) that this feature is just "proposed" but I certainly hope it makes the cut:

    if (Circle != null && (var r = Circle.ChooseRadius(); r*r < 0))
      throw new InvalidOperationException("I don't believe in imaginary numbers");

This is equivalent to

    if (Circle != null) {
      var r = Circle.ChooseRadius();
      if (r*r < 0)
        throw new InvalidOperationException("I don't believe in imaginary numbers");
    }

While generally it is bad style to write this kind of code directly (usually you _should_ just use a few more lines of code), it is useful for defining lowerings inside the compiler, it is sometimes handy for code generators and pretty much a __mandatory prerequisite__ for a core feature of EC#: LISP-style macros (which, as it happens, are also code generators). In fact, EC# goes further and allows you to insert arbitrary code in braced blocks:

    if ({
      var s = path;
      while (s.Length > rootPath.Length && s.Contains("\\"))
        s = Path.GetDirectoryName(s); 
      s
    } != rootPath)
      Console.WriteLine("{0} is not within the {1} folder", path, rootPath);

Again, you would not usually use this feature directly, but it allows macros to inject arbitrarily complex code anywhere, which is important to make the macro system really useful.

## And what about...

The following features were [proposed earlier](damieng.com/blog/2013/12/09/probable-c-6-0-features-illustrated) but not mentioned in Mark Michaelis' [new article](http://msdn.microsoft.com/en-us/magazine/dn683793.aspx):

    // safe navigation a.k.a. null-dot a.k.a. monadic dot operator:
    var bestValue = points?.FirstOrDefault()?.X ?? -1;

    // Lambda notation for properties & methods
    public double Distance => Math.Sqrt((X * X) + (Y * Y));
    public static Point Square(int x) => x*x;

    // More flexible "params"
    public void Do(params IEnumerable<Point> points) { ... }
    
    // Constructor type inference
    new Tuple(1, "one", 1.0);

The null-dot operator is incredibly useful (and also present in EC#), so I doubt they dropped it.

After hearing about the lambda syntax for methods and properties, I added it to the EC# parser. For EC# it's a trivial change, since EC# already supported a similar "forwarding" notation, which helps you implement the decorator pattern and various day-to-day code refactoring:

    class ReadOnlyWrapper<T> : IList<T>, IReadOnlyList<T>
    {
        public this(private IList<T> _list) {}
        
        void Throw() { throw new ReadOnlyException(); }
        
        T this[int index] { get { return _list[index]; } }

        // Forward these methods and properties to the underlying object.
        int Count ==> _list.Count;
        int IndexOf(T item) ==> _list.IndexOf;
        bool Contains(T item) ==> _list.Contains;
        void CopyTo(T[] array, int arrayIndex) ==> _list.CopyTo;
        IEnumerator<T> GetEnumerator() ==> _list.GetEnumerator;
        IEnumerator IEnumerable.GetEnumerator() ==> GetEnumerator;

        bool IsReadOnly { get { return true; } }

        void Insert(int index, T item) { Throw(); }
        void RemoveAt(int index) { Throw(); }
        void Add(T item) { Throw(); }
        void Clear() { Throw(); }
        bool Remove(T item) { Throw(); }
    }

I wasn't planning the "params IEnumerable" or constructor type inference features for EC#, but I'll support whatever C# does.

## Record types

Record types are a quick way to declare bundles of data.

    public record class Person(string firstName: FirstName, 
        string lastName: LastName, int age: Age);

This is one of those features that aren't needed in a language like EC# that supports macros. EC# could support the same thing, with no changes to the parser, using a syntax like this:

    record Person(string FirstName, string LastName, int Age);

Or like this:

    record(Person) { string FirstName, LastName; int Age; }

Someone simply needs to write a macro to translate this into a normal class declaration.

## Pattern matching

[It is said](http://www.infoq.com/news/2014/08/Pattern-Matching) that C# 6 will support a new "pattern matching" syntax that looks like this:

    public static class PhoneNumber
    {
       public static bool operator is(string s, out int areaCode, out int number)
       {
          Match m = Regex.Match(s, @"^\s*(\((\d\d\d)\))?\s*(\d\d\d)-?(\d\d\d\d)$");
          if (!m.Success) return false;
          areaCode = int.Parse(m.Groups[2].Value);
          number = int.Parse(m.Groups[3].Value) * 10000 + int.Parse(m.Groups[4].Value);
          return true;
       }
    }
    string s = "(403) 777-3760";
    if (c is PhoneNumber(var acode, *))
        Console.WriteLine("Area code: " + acode);

I can only assume this feature works automatically in conjunction with record types. It seems a little odd that one writes `var acode` rather than `out var acode`; I wonder if this means that the pattern-matching `is` operator is only allowed to have output parameters, not input parameters.

I wasn't sure how to support pattern matching in EC#. This plan seems as good as any.

"`*`" means "don't care". In EC# I was planning to introduce "`_`" to represent "don't care" for any out parameter or unused result (e.g. `_ = control.Handle` calls a property and discards the result) rather than `*`; `_` would mean "don't care" only if there was no explicitly-declared variable named `_`. I suppose `*` can do the same job instead.

## A lot more is planned for EC# ##

In the long run I want to add tons of stuff to EC#, but for now EC# is just a parser plus LeMP (Lexical Macro Processor), not a complete compiler. There is, of course, one thing that EC# can do already that C# cannot: lexical macros. Lexical macros are powerful: have you heard of the [Loyc LL(k) Parser Generator](http://www.codeproject.com/Articles/664785/A-New-Parser-Generator-for-Csharp), well, that's just a macro inside a nascent version of EC#. Macros allow third parties to add a variety of features to the language, and some of EC#'s features (such as the `?.` operator) will initially (if not forever) be implemented as lexical macros.

In the long run EC# may also support:

1. An even better macro system
2. Compile-time code execution (CTCE)
3. A "template" system (the "parameterized program tree")
4. An alias system (which is a minor tweak to the type system, not unlike)
5. Miscellaneous semantic enhancements including a "trait" system
6. Roslyn integration (this was out-of-the-question until Roslyn was open-sourced recently)

I have written draft articles about my various ideas, but they will not happen unless I can find supporters to help write the code. If you're interested, you might want to check out the [drafts on github](https://github.com/qwertie/Loyc/tree/master/Doc) as well as the [Loyc blog](http://loyc.net/blog) before you go looking for my email address on the [Loyc home page](http://loyc.net).

## So much left to fix

I just finished writing a whole nother article about the [flaws in the CLR](http://loyc.net/2014/dotnet-annoyances.html), and as I mentioned I could have made the list longer by including flaws in C#. Sadly, C# 6 does not try to work around the limitations of .NET or address flaws inherited from prior versions of C#. Enhanced C# will address some, but not all, of these flaws. To fix everything we need not only a new language, but a [new runtime environment](http://loyc.net/2014/open-letter.html); no one is stepping up to fund such a thing, however.

<small><a href="http://www.codeproject.com/script/Articles/BlogArticleList.aspx?amid=3453924" rel="tag" style="display:none">Published on CodeProject</a></small>
