---
title: Design elements of a multi-language standard library
layout: post
toc: true
commentIssueId: 5
---

Recently the Wolfram Language became available. While the language itself has some innovations, the primary attraction of this language is a fantastically rich library of professionally curated functionality. But it is a proprietary "cethedral" model and it the library is tied to just one language, the Wolfram Language. Meanwhile, the open-source "bazaar" model has almost taken over the landscape of software development, but in a messy and irregular way. Given any two random open-source libraries, chances are they cannot talk to each other even if they are designed for the same domain. Because most standard libraries are impoverished, with a dearth of standard interfaces, it is often the case that two libraries written for the same domain are incompatible, even if they are written in the same language. In other words, the output of one library cannot be used directly as the input of the other.

It's bad enough that libraries written in different languages are incompatible, but even in environments like Java and C# with relatively good libraries, I have seen basic concepts like "points" and "colors" redefined, because the standard library wasn't good enough. This need to change.

## Purpose

The primary purpose of the Loyc Multi-Language Standard Library (MLSL) will be the foundation of a framework for automatically converting libraries and programs between programming languages. The idea is that you should be able to write a library in one "canonical" language (perhaps a language that doesn't exist yet) and a compiler will convert it to several other languages for you.

Of course, your library will no doubt need to use collections of some sort, and strings, and it will probably use one or more common feaures such as streams/files/sockets, threads, logging, assertions, unit tests, localization, XML manipulation, or windows (I don't plan to deal with windowing libraries initially, but hopefully someone will, someday).

Obviously, converting code between languages ("transpiling") is a challenging problem by itself, but it's much harder still if the source code is using a library that doesn't exist in the target language. So in practice, to convert code from "source" language A to "target" language B, the language A code must _not_ use the standard library of language A; it must instead use a standard library that exists in both languages. That library is the MLSL.

I am interested primarily in designing the MLSL for statically-typed impure OO and functional languages, but I welcome volunteers to help steer the design in such a way that the MLSL will be usable in dynamically-typed languages also. My (naive) expectation is that a dynamic language tends to be suitable as a target language but not as a source language.

MLSL has other purposes too:

- Raising the bar on standard libraries. Hopefully some very smart volunteers will choose to work on the design of the MLSL, and together we can avoid some of the limitations, flaws and inconsistencies in older standard libraries. Every language I have used has flaws in its standard library design; let's try to make fewer flaws in this one, yes?

- Although professional programmers normally learn multiple programming languages, it's tough to remember all the nuances of several different standard libraries. By using a MLSL, you can program in different languages more easily (as long as those different languages all have an implementation of the MLSL).

- Every new programming language has a new standard library, created from scratch. If the MLSL were complete, new languages could be created without creating an all-new standard library. Of course, new languages always have new features that require new libraries to fully take advantage of them, but the MLSL could provide a quick way to bootstrap new languages.

- Hopefully someday the MLSL will be part of a standard for binary interoperability, which will allow complex data types such as hash trees. Such a standard may or may not include a "managed" VM platform with features like reflection and run-time code generation; but whether there's a JIT or not, whether there is a multi-language garbage collector or not, one feature that is absolutely essential is a way to pass complex data structures across language boundares. The MLSL will help with that.

The MLSL does not exist yet. I am calling for volunteers to help build it.

So, here are some of the design elements I envision for the MLSL....

## MLSL should try to fit into its environment, at least a little bit

In C#, method names normally start with a capital letter. In Java, method names normally start with a lower-case letter. The MLSL should accommodate such minor style differences. So MLSL interfaces in C# should start with a capital `I`, while MLSL interfaces in Java should not.

However, variation among languages must be limited to things that a cross-language converter could handle automatically. A C#-to-Java converter could reasonably adjust class and method names, but it could not change anything important.

## This is not a lowest-common-denominator approach

The design of the MLSL should generally keep in mind design patterns and limitations found in the majority of popular statically-typed languages; for instance, it should fit naturally into a language that has single inheritance and dynamic dispatch on the first parameter only.

However, the MLSL should not be a truly lowest-common-denominator design; we must not allow ourselves to be limited by the _least_ powerful target languages.

Firstly, the MLSL should not be held back by _unusual_ limitations of one or two languages. For example, most languages support anonymous inner functions or closures, so the MLSL should be designed to take advantage of these by providing features that work best with closures (such as `filter/map/reduce` functions corresponding to LINQ's Where/Select/Aggregate). To deal with unusual languages that don't support closures, special supplementary functionality could be added (e.g. common predicate functions for use with `filter`). This additional functionality should probably be made available to all languages, because code based on the MLSL should always be easily portable from a less powerful language to a more powerful one, but the extra functionality should be in a special namespace/module to highlight its special status as a workaround for limitations of a particular language.

Secondly, the MLSL should not bother to target unusually limited languages at all, but only mainstream and new, powerful languages.

Thirdly, the MLSL should not unnecessarily limit itself based on the capabilities of target languages; if possible, features that are not available or not efficient in a given language should be simulated by isomorphic (equivalent) features. For example, suppose we want to define a method on strings that gets the value on the left and right side of a given character. Naturally, this method wants to return a pair of strings:

    // C#-like pseudo-code, where "T??" represents an "optional" T value
    (string, string??) SplitAt(string s, char c)
    {
      int?? i = s.IndexOf(c);
      return i.HasValue ? (s.Slice(0, i.Value), s.Slice(i.Value + 1)) : (s, null);
    }

Unfortuntely, most languages support only a single return value, but all languages have some sort of workaround to deal with this desire to return multiple results. For instance, in both C# and Java, some sort of tuple could be used to return the two values. But allocating a tuple on the heap is wasteful in general, so in C# we should consider alternatives such as a `Pair` structure...

    // What is a UString? More on that later.
    public static Pair<UString,UString> SplitAt(this string s, char c)
    {
      int i = s.IndexOf(c);
      if (i == -1)
        return new Pair<UString, UString>(s, UString.Null);
      else
        return new Pair<UString, UString>(s.USlice(0, i), s.USlice(i + 1));
    }

or `out` parameters...

    public static UString SplitAt(this string s, char c, out UString result2)
    {
      int i = s.IndexOf(c);
      if (i == -1) {
        result2 = UString.Null;
        return s;
      else {
        result2 = s.USlice(i + 1);
        return s.USlice(0, i);
      }
    }

Most likely we will standardize upon one of these techniques and use it everywhere.

In Java, which supports neither `out` parameters nor value types, a heap object may be the only way to return two values, but perhaps we could consider allowing the caller to supply the heap object:

    public static Pair<String,String> splitAt(this string s, char c) 
        { return splitAt(s, c, new Pair<String,String>()); }
    public static Pair<String,String> splitAt(this string s, char c,
                  Pair<String,String> result)
    {
      int i = s.indexOf(c);
      if (i == -1) {
        result.First result2 = UString.Null;
        return s;
      else {
        result2 = s.USlice(i + 1);
        return s.USlice(0, i);
      }
    }

This allows the caller to reduce (but not eliminate) heap allocations by re-using the same "result" object across many calls. How a technique like this will relate to a cross-language converter is an open problem, though.

In general, to design the MLSL, we should imagine a _powerful and flexible_ language, an impure OO/functional hybrid language, design the MLSL _for_ this powerful language, and then figure out how to adapt it to less powerful languages. If adaptation is impossible, then and only then will we redesign it for "weaker" languages.

## MLSL should be efficient in most languages

Speed is always an important design consideration--not the #1 design goal, but it's up there. We don't want anyone to choose another library over the MLSL for performance reasons.

The degree to which an instance of the MLSL emphasizes performance may depend on the target language: the MLSL must be fastest in languages like C++ that have a reputation and historical focus on speed, while we may be able to sacrifice some speed for other things (e.g. flexibility, elegance, simplicity) in other languages.

At times there will be a conflict between good design and efficiency. In that case we will just have to find the best compromise we can. TODO: find a good example of this and explain it.

## A bit of a quagmire is to be expected

The `SplitAt` method above raises a couple of issues about string representations, especially in .NET.

In my experience, code that _analyzes_ the content of strings (e.g. parsers) wants to use a divide-and-conquer approach: breaking up the string into smaller pieces and looking at those pieces, often in a recursive way. It is inefficient to physically create new substrings for analysis purposes; instead, what we want is to use _slices_.

A _slice_ is the original string buffer plus two integers which represent a section of that string. <div class="sidebox">My favorite implementation of slices occurs in the D language. In D, a slice requires only one integer (the length), not two, because a slice holds a pointer directly to the beginning of the slice, not to the beginning of the string buffer. You can also efficiently append things to the end of the slice, which is a neat trick of memory management.</div> So rather than actually creating two new strings and returning them, which would be inefficient, the C# SplitAt method creates two slices and returns those. `UString` is a slice data type in Loyc.Essentials, which is a .NET library that you can think of as a precursor to the MLSL. The standard `string` type can be converted implicitly to `UString` thanks, while the reverse conversion requires an explicit cast.

In Java the situation is rather different, because the standard String type is _already_ a slice; two Strings can share the same underlying buffer, so there is no need to create a new type for slicing. However, the MLSL string data type will probably have a different API than the Java String. Somehow we should make it possible to code against the MLSL string API while using the actual java.lang.String class; for instance, the MLSL interface could be represented by a set of static methods, and a transpiler could then treat those methods as if they were part of the String class.

Another issue with strings is their physical representation. All newer languages use Unicode as their standard string representation, but there's a problem: there's more than one Unicode representation. Unicode code points need 21 bits (17 planes of 65536 code points each) so a modern computer uses a 32-bit type to store a single code point. But arrays of 32-bit characters are extremely inefficient, so no major programming language uses 32-bit character arrays as the standard string representation. Instead, they use either UTF-8 or UTF-16. In order for the MLSL to "fit in" to a programming language, the MLSL for that language should use the "normal" string representation--UTF-16 in some languages, UTF-8 in others. In my opinion, the MLSL must provide a string interface that may be UTF-8 or UTF-16 depending on the current programming environment, and code written based on the MLSL must be designed to work in both environments.

Anoter issue is that standard strings in some languages are mutable, while standard strings in other languages are immutable; the latter usually has a "freezing" paradigm, in which there is a "special" mutable string type ("StringBuilder") from which you can create a immutable version on-demand ("String"). <div class="sidebox">Again I think the D language is the leader here, as mutable and immutable strings are equally first-class. A function that accepts a "const" string and does not modify it can accept either a mutable or immutable string, in contrast to languages like C# and Java that force each method to choose which kind of string they will accept. D strings are also considered to be primitive arrays of characters, not as something special and different than all other kinds of lists, yet D is still an excellent language for string manpulation. Technical limitations of other languages may prevent the MLSL from using the D model, but I would like to point it out as being, in my opinion, an ideal model.</div> I'm not sure how best to reconcile the various paradigms in the MLSL, but I think the MLSL should favor immutable strings primarily. The fact is, most code merely stores strings or passes them around without modifying them; immutable strings allow such code to be implemented without concern that the string might be changed by other code unexpectedly.

Finally, it is fairly obvious to comp-sci folks that a string is "just" an array or list of characters (or partial characters), no different than a list of anything else. Ideally, the MLSL would treat strings the same way, as just one kind of random-access list out of many. Unfortunately some languages treat Strings as something entirely different. For example, the .NET `String` and `StringBuilder` types are not only not treatable as arrays, and not only typed separately from all .NET collection classes, but they don't even implement the standard collection interfaces like `IReadOnlyList<char>` or `IList<char>`. Yuck.

Clearly the way we deal with strings in the MLSL is important and is representative of the kinds of difficulties we will face throughout the project. I don't have all the answers yet; I just hope that others will join the project to offer their thoughts on how to solve the problems we face.

## Interoperability with the "native" standard library

Code based on the MLSL should interoperate easily with code based on the standard library of the same language, and the MLSL data structures in a particular language should provide interfaces (or be convertible to interfaces) that are normal for data structures in that language.

For example, in the C++ edition of the MLSL, its collection classes should expose an STL interface (in addition to the MLSL interface), and it should be easy to adapt an STL collection to the MLSL interface. In .NET, the MLSL collections should implement standard interfaces like `IEnumerable<T>` and `IList<T>`, and it should be easy to wrap standard .NET collections into MLSL collections.

## Interfaces should be designed for both static and dynamic dispatch

Interfaces should not be overly chatty, meaning they should not require many method calls to accomplish tasks. The "interface" of D input ranges is a good example of an overly chatty interface:

    // An input range provides the following methods:
    bool empty();
    T front();
    void popFront();

A loop that uses an input range `r` must call all three methods on every iteration:

    while (!r.empty) {
        DoSomethingWith(r.front());
        r.popFront();
    }

If `r` is statically typed and the source code of `r`'s type is available to the compiler, then this is no problem. All three of the methods can potentially be inlined, so the code will be fast. But if `r` is an interface or abstract base class, this loop will require three dynamic dispatches and will probably be much slower than necessary. In an environment where dynamic dispatch is normal, interfaces should not be so "chatty": they should not require so many calls.

<div class="sidebox">By the way, for multiple reasons, C# nullable types are not suitable for the concept of "optional".</div> Here is a better interface, where T?? represents some "optional" type:

		T?? tryPopFront();
		bool empty();
		T front();

In this interface, most loops only need to call the first method, while the other two exist only for convenience. Using extension methods in C# or UFCS in D or other languages, popFront() can be added as a wrapper around tryPopFront(). In fact, given ranges that support copying, empty() and front() could both be implemented via extension methods, traits / default interface implementations, or UFCS in terms of tryPopFront().

Usually, classes in the MLSL should offer both static (direct) dispatch and dynamic (interface) dispatch, since each kind of dispatch has advantages that the other does not.

## MLSL Collections

Data structures, algorithms and interfaces for collections are the keystone of any modern standard library.

I propose a rich set of collections founded on the concepts of iterators (like C++, but an MLSL iterator should be smarter: it should know whether it is valid), ranges (as in D), and interfaces (C#, Java, etc.). The MLSL should provide all the usual mutable data structures--linked lists, growable arrays, hashtables, sorted trees--but also data structures that fit well into a functional programming style, such as VLists/WLists and immutable hash trees.

As an author of numerous data structures, I think I have found a beautiful way to bridge the procedural and functional worlds, in the form of data structures that can be either mutable or immutable and morph from immutable to mutable form and vice versa. Loyc.Collections has three sets of data types in this category:

1. (R)VLists: These types act like growable arrays, but are actually linked lists of arrays ([read more](http://en.wikipedia.org/wiki/VList)). I have written immutable `FVList<T>` and `RVList<T>` list types along with mutable forms `FWList<T>` and `RWList<T>`. Any mutable list can be converted to an immutable list in O(1) time and vice versa.
2. hash trees: I have written immutable `Set<T>` and `Map<T>` types along with mutable forms `MSet<T>` and `MMap<T>`. Any mutable set can be converted to an immutable set in O(1) time and vice versa. All four types are wrappers around the same internal implementation, `InternalSet<T>`.
3. ALists: The AList family of data structures are indexable lists, structured like B+trees, that hold sorted or unsorted data. ALists can be frozen and fast-cloned in O(1) time; thus, any mutable list can be converted to an immutable list in O(1) time and vice versa. My ALists also offer a variety of "bonus features", capabilities that you won't see in most existing standard libraries.

I must say, I love these mutable-immutable fast-cloning data structures. They give a lot of flexibility to the programmer to use the right tool for any given job, and when your needs change, you simply flip the switch to go from mutable to immutable or vice versa. How does cloning in O(1) time work? Simple, each of these data structures consists of a number of sub-objects or "nodes". Each node has a read-only flag. When you clone, the root node is marked read-only ("child" nodes are marked read-only later, on-demand). Even though individual nodes (or _all_ the node) are marked read-only, the data structure as a whole (or rather, the object that you, the user, interact with) can still be mutable. When you modify the data structure, any nodes that must be modified to satisfy the mutation request are duplicated.

These data structures are closely related to ["persistent" data structures](http://en.wikipedia.org/wiki/Persistent_data_structure). "Persistent" does not mean "stored on disk" like you would think, it means "any and all old versions of a data structure can continue to exist after new versions are created". The data structures I have created do not generally preserve all old versions, so they are not "fully persistent", but they can preserve old versions on demand. Therefore I call them "optionally-persistent" (I wanted to call them "semi-persistent" but that term was already used to mean something else.)

In terms of typing, ALists are different from the VLists and hash trees, in that a single data type is used for both the immutable and mutable forms. This was a choice I made--the AList family of types is already very complex, and maintaining two separate data types for each kind of AList would exacerbate the situation.

For the sake of code clarity, I tend to believe that it is better to have separate types for the mutable and immutable forms. When you see `Set<T>` in code, you know it is immutable; when you see `MSet<T>` you know it is mutable. If you see `AList<T>`, you don't know, it could be frozen or not, thus the code is harder to understand because you must deduce whether the list is always mutable, always frozen, or sometimes frozen. On the other hand, using a single "freezable" type allows you to create objects that start mutable and then can be frozen, without the need to manage two fields (one for the mutable form and another for the immutable form).

So on the whole, I favor the two-separate-types paradigm. If you need to store a reference to an object that might be mutable or immutable, you can use an interface reference (or, the mutable and immutable versions of each data structure could share a base class, so you could use a reference to the base class.) Still, I'm open to the possibility that the "freezable" paradigm has advantages that I do not yet fully appreciate.

For the MLSL, I want to offer highly flexible data structures such as the AList and hash trees. The main problem with this type of data structure, though, is that it is designed for garbage-collected environments. Hopefully they can be adapted to be efficient in environments with manual memory management.

Multithreaded data structures are also important to have in an MLSL. In particular I'd like to have the Disruptor, a fast queue (more flexible than normal queues) designed for multithreaded environments.

## Geometry is not a GUI thing

For some reason, standard libraries in many languages do not define types as simple as points, vectors and colors, or these types are misunderstood as being artefacts of a windowing system. Consequently, geometric data types are typically redefined separately for every GUI toolkit and every geometry library, which makes it impossible to write truly "generic" geometry code. In .NET, for example, `Point` (two integer coordinates) and `PointF` (two float coordinates) are defined in the `System.Drawing` namespace, implying they are part of the WinForms GDI+ toolkit. These types are underdeveloped, with, for example, no overloaded operators and no corresponding `Vector` or `VectorF`. Microsoft's second GUI toolkit, WPF, defines a second `Point` type in the `System.Windows` (i.e. WPF) namespace, this one with `double` coordinates, overloaded operators and a corresponding `Vector` type.

This is very wrong. Points are a generic geometric concept, not a "Windows" concept (and similarly for colors). An MLSL should define points in a generic way, not favoring one particular data type for the coordinates, and certainly not favoring a particular GUI toolkit. 3-dimensional and N-dimensional points should also be defined with analogous interfaces, so that data structures designed for N dimensions should also be able to process 2- or 3- dimensional point data. Other concepts often defined in the GUI toolkit, such as splines and hit testing code, should be moved out into a generic geometry library.

Even some things that are mostly about GUIs, such as layout algorithms, box models, or a structure that holds font metrics, should be isolated from any particular GUI toolkit and standardized.

Perhaps the MLSL itself should standardize a GUI toolkit, but even in that case, we should still make a clear distinction between the "generic" stuff that applies to all windowing systems from the functionality of a particular system.

## Let's have lots of interfaces

Most standard libraries do not include very many fundamental types and interfaces for various fields of math and science. Let's fix that. In addition to basic types like complex numbers, points, vectors (differences of points), and colors, I want to have matrices, line segments, interfaces (if not implementations) for spatial indexes, linear algebra stuff, quaternians, crypto interfaces, and interfaces for any other data types that are crucial in well-known fields of math, science, computer science, or practical programming. All these interfaces do _not_ need implementations in the MLSL, but it is important to standardize the interfaces to increase interoperability between code writen by different people related to a particular field.

I will need expert volunteers to design some of these interfaces and provide written rationales for their design.

## Error handling

Most popular languages support exceptions, so exceptions will probably be used for signalling errors in most incarnations of the MLSL. Sadly, not all popular and upcoming languages support exceptions; for instance, C and Rust don't. Frankly, I'm not worrying about C, as most people can use C++ instead. Rust, though? That's a new language designed by some very smart people. I think it will be necessary to support Rust somehow. I have no idea how to do it yet. Of course, error handling isn't the only concern when it comes to Rust; its unusual type system could easily come in conflict with a system that wants to automatically convert code between languages. Time will tell.

## Closing thoughts

Usually when I seek feedback about research topics like this, I get none. But hey, maybe this time it'll be different. I think it's obvious that someday a standard for cross-language interoperability must emerge. Let's make it sooner rather than later.

Here are some questions for you, the reader:

- What are some of the best-designed _interfaces_ you know of, regardless of programming language? 
- In your programming specialty, whether it's DNA processing or ORMs, which libraries do you feel are the best designed? I'm not asking about libraries that are _useful_; I'm talking about design aesthetics, like elegance, power, extensibility and ease-of-use, all shaved by [occam's razor](http://en.wikipedia.org/wiki/Occam's_razor).
- What "antipatterns" or poor design elements do you think should be avoided?
- What programming language features do you think are important for facilitating good library design (e.g. type system features, code contracts, shorthand notations)?

I've been meaning to look at the KDE Frameworks project as a possible starting point for the MLSL, but I haven't got around to it yet. Can anyone point me to a primer on the Tier 1 components?

### A new programming language?

P.S.

I'm considering defining a new programming language family for the Loyc project, something highly modular so that it can be customized for various needs. This would not be one language, but a family of languages built out of different collections of modules that are designed to fit together. There would be multiple defined "profiles" of the language, built out of specific sets of modules:

1. "Expression language": an expression evaluator that may support loops but probably not function or type definitions. This subset of the language would be part of the MLSL, as a compact interpreter that could be used, for example, to add searching and filtering functionality in end-user applications.
2. "Basic": a subset of the language such that a compiler for it is relatively easy to implement, a language that is powerful for its size: a competitor to Lua.
3. "Universal": a subset of the language designed to allow code written in it to be automatically converted to popular statically-typed languages like C#, Java, and C++. This version of the language could and should contain features that the target languages _do not have_, such as LISP-style macros, "traits", or static unit checking; the only constraint is that there must be some way to translate the code into the target language, and both the translation process and the target language code must be reasonably efficient. Hopefully code in the "Universal" subset can also be converted to the "Basic" subset.
4. "Flagship": a maximal version of the language using all standard modules, including features that some popular languages cannot efficiently support, e.g. pointers, fibers, multiple dispatch.

To be clear, that's not a list of modules, it's a list of subsets. The language would consist of dozens of modules that can be put together in various ways; a "subset" refers to a specific collections of modules, configured in a specific way.

The language will not have a single syntax either. There should be a simple "canonical" syntax, probably [LES](https://github.com/qwertie/LoycCore/wiki/Loyc-Expression-Syntax), but other parsers could be written that would allow the Loyc language to directly compile _subsets_ of other languages such as C# or Julia.

The type system is a very important piece that I haven't really worked out yet. I'm looking into things like higher-kinded types, multiple kinds of type aliases, dependent types, and union and intersection types.

So let me know if you're interested in helping me design this language.

<small><a href="http://www.codeproject.com/script/Articles/BlogArticleList.aspx?amid=3453924" rel="tag" style="display:none">Published on CodeProject</a></small>
