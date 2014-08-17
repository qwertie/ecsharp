---
title: .NET just keeps getting more annoying
layout: post
commentIssueId: 4
---
It seems like the little design flaws of .NET get on my nerves more and more every week.

Today I realized that I wanted to do a binary search. No problem--I put a binary search method in [Loyc.Essentials](https://github.com/qwertie/Loyc/tree/master/Src/Loyc.Essentials); four independent implementations, in fact, one for `IList<T>`, one for `T[]`, and two for more obscure scenarios. But the list I wanted to search implemented `IReadOnlyList<T>`, not `IList<T>`. Since `IReadOnlyList<T>` is strangely not a subtype of `IList<T>`, I realized I would have to add another overload with a verbatim copy of the existing implementation. And of course I'd need to add not one new overload, but three, to support .NET's three standard kinds of comparisons: `T` implementing `IComparable<T>`, independent `IComparer<T>`, and independent `Comparison<T>`:

    public static int BinarySearch<T>(this IReadOnlyList<T> list, T value) where T : IComparable<T>
    {
        return BinarySearch<T>(list, value, G.ToComparison<T>());
    }
    public static int BinarySearch<T>(this IReadOnlyList<T> list, T value, IComparer<T> comparer)
    {
        return BinarySearch<T>(list, value, G.ToComparison(comparer));
    }
    public static int BinarySearch<T>(this IReadOnlyList<T> list, T find, Comparison<T> compare)
    {
        // Duplication of code for IList<T>
    }

And then my build broke because some code had called `BinarySearch` with a list that implemented _both_ `IReadOnlyList<T>` and `IList<T>`, so I fixed that by adding the usual set of disambiguating overloads, raising the total number of BinarySearch methods in Loyc.Essentials somewhere north of 15.

Next, I realized that the kind of binary search I wanted was a bit unusual. I was writing a Visual Studio syntax highlighting extension and the list being searched was a list of `ITagSpan<T>` objects, which are Visual Studio objects that combine a "tag" object with a "span" (which is a range of characters in a text file). So I did not actually want to search for a particular `ITagSpan<T>` object in the list, I wanted to search for an _integer_ such that the `ITagSpan` matched it in a certain way, e.g.

    int i = _tagSpanList.BinarySearch(span.Start.Position, 
        (tspan, start) => (tspan.Span.End.Position-1).CompareTo(start));

This does not change the binary search algorithm at all, but it does change the method signature. Instead of this:

    public static int BinarySearch<T>(this IReadOnlyList<T> list, T find, Comparison<T> compare)

what I actually wanted was this:

		public static int BinarySearch<T, K>(this IReadOnlyList<T> list, K find, Func<T, K, int> compare)

The delegate type `Comparison<T>` only supports _symmetrical_ comparisons; in order to support _asymmetrical_ comparisons I had to change `Comparison` to `Func`. Of course, if `T = K` then `Comparison<T>` and `Func<T, K, int>` are exactly equivalent... or they __should__ be, but in fact are not, because Microsoft said so. You cannot pass a `Comparison<T>` into a method that expects `Func<T, T, int>` or vice versa. It's possible to convert between the two types, but you have to allocate an extra heap object to do so.

But I figured no one else was using my library (much as I wish they would), so I changed the signature... problem solved, and I was able to finish my multi-language syntax highlighter (which currently supports EC# and LES).

But Loyc.Essentials still offers other methods that use `Comparison<T>`, such as `Sort`:

    public static void Sort<T>(this IList<T> list, Comparison<T> comp) {...}

So imagine you are writing a module that needs to sort and repeatedly search a list. You want it to be generic, so you accept a delegate passed from the outside. What should the type of the delegate be? If you choose `Comparison<T>`, you can't call `BinarySearch` and if you choose `Func<T,T,int>` you can't call `Sort`. Wonderful.

So I realized that I had made a mistake. While clearly we need a version of `BinarySearch` that is asymmetrical, we clearly also need a version that accepts `Comparison<T>`. But this raises the minimum number of Binary Search _implementations_ to four:

- one for the combination `IList<T>` and `Comparison<T>`
- one for the combination `IList<T>` and `Func<T,K,int>`
- one for the combination `IReadOnlyList<T>` and `Comparison<T>`
- one for the combination `IReadOnlyList<T>` and `Func<T,K,int>`

All with identical code inside. Now, I could pick one canonical implementation and use wrappers or adapters for the others, but such things require heap allocations. Do you think a binary search should require heap allocation? Well I don't (I accept a single delegate allocation for the `IComparer<T>` overloads, which I personally never use.)

Meanwhile eight additional _method overloads_ are provided, which just forward to one of the implementations above:

- two for `IList<T>` with `IComparer<T>` or with no comparer provided
- two for `IReadOnlyList<T>` with `IComparer<T>` or with no comparer provided
- four for `IListAndListSource<T>` (which combines `IList<T>`, `IReadOnlyList<T>` and my own `IListSource<T>` interface) with `IComparer<T>`, with no comparer provided, with `Comparison<T>` or with `Func<T,K,int>`

With 12 overloads, we haven't even covered the case that you want to search a _slice_ of the list rather than the entire list (you can do that by calling `list.Slice(start, length).BinarySearch(item)`, of course, but it does have the disadvantage of boxing the slice structure.)

Oh, and one more thing. The version that takes `Comparison<T>` cannot have the same _method name_ as the version that takes `Func<T,K,int>` because if you pass a lambda to `BinarySearch`, the C# compiler will error out because it is "ambiguous" which version to call, even though, of course, it doesn't matter at all which version is called. So I decided to call the `Comparison<T>` version `BinarySearch` and renamed the `Func<T,K,int>` version `BinarySearch2`.

So this is my problem with .NET. We shouldn't need 12 overloads (including 3 under a different name), we should only need 2 overloads and a single implementation:

- One overload that accepts `IReadOnlyList<T>` and no special comparison method. This method should accept `IList<T>` also since `IReadOnlyList<T>` should be a subtype of `IList<T>`.
- One overload for the combination `IReadOnlyList<T>` and `Func<T,K,int>`. This should cover the other three cases since `Comparison<T>` should be considered an alias for `Func<T,T,int>`, not an unrelated type. (And while we're fixing this flaw, let's have delegates be value types, not heap-allocated, okay?)
- `IComparer<T>` is redundant, `Func<T,K,int>` is all you need. (Note that given a _class_ type `T` that implements `IComparable<T>`, you can efficiently derive a `Func<T,K,int>` from that by creating an _open delegate_; .NET conventionally converts to `IComparer<T>` instead but this less efficient on average, as it requires double virtual dispatch.)

As the author of a library that is highly generic, the unnecessary incompatibilities in .NET make me more and more uneasy every year. Today I've discussed two issues; here are some others, most of which I have [discussed before](http://loyc.net/2012/design-flaws-in-net.html):

- Structures/classes cannot embed a fixed-length array of arbitrary type `T`. This limits optimization opportunities when implementing data structures, which made my `CPTrie` and `InternalSet` data structures (among others) less efficient than they could have been.
- A nullable value type is not equivalent to or compatible with a nullable reference type. You cannot write generic code that treats them the same way. If the CLR designers had defined `T?` as a boxed version of `T`, this problem would not exist. (We would still have a smaller problem, that the "nullable" concept cannot be nested: you cannot use `Nullable<Nullable<T>>`. There is no standard way to express the concept "optional value of type T, in which T itself _might_ be nullable". The standard solution, of course, is to use an independent boolean to express whether the T is present.)
- .NET does not support weak delegates.
- [Covariant return types](http://blogs.msdn.com/b/cyrusn/archive/2004/12/08/278661.aspx) are _still_ not allowed in .NET after all these years!!!
- It is not possible to bitwise-compare two arbitrary values (two values of a value type _or_ two references, or for that matter the contents of two objects of the same type).
- "Generic interface unification" is forbidden even when it makes sense and causes no problems.
- .NET is not able to support ad-hoc interfaces (like those offered in Go) in a performant manner, nor is there "structural typing" for interfaces, so two interfaces with identical methods defined in different assemblies, or different versions of the same assembly, are incompatible, even if the methods are listed in the same order. For instance, I haven't been able to define `IReadOnlyList<T>` in an assembly built in Visual Studio 2010 (.NET 4) such that it is compatible with the identical interface in .NET 4.5 (but see [`[TypeForwardedTo]`](http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.typeforwardedtoattribute%28v=vs.110%29.aspx)).
- When creating a thread, it is impossible to cause the child thread to inherit thread-local values from the parent thread. Even propagating values manually is difficult.
- Fibers (stack switching) are not really supported (the async features of C# compensate for this. Technically stack-switching is more general and flexible, but I suppose it's okay not to support them: some tests I did in D suggested that either x86 CPUs are not designed to switch stacks efficiently, or, for all I know, that the Windows OS makes it slow by trapping the operation.)
- Having two different length attributes, "Length" and "Count", can be a nuisance.
- Needless coupling between property getters and setters causes various minor problems. You can't derive from a read-only base class or interface and "just" add a setter; you can't make _only_ the setter virtual, etc. (The other issues are CLR-wide; for this one I'm not sure if it's a CLR issue or a C# issue.)
- Structures can't have default constructors (as distinct from `default(T)`). Jon Skeet found that this was mostly a C# limitation except in one important way that is CLR-wide: the constructor is not invoked by generic code.

These are mostly limitations of the CLR. If we add limitations of C#, or limitations of the BCL, the list becomes considerably longer.

I really believe a "managed environment" for software development should be available, in which developers can easily use dynamic loading, reflection, garbage collection and run-time code generation, and in which programs written in different languages are interoperable without hassles. But it is clear to me that both the Java and .NET implementations of this concept are flawed. That's why I have called for a [new VM to fix all these problems](/2014/open-letter.html) and supplant Javascript, to boot. Now if only someone would hire me to help build it...

<small>Published on <a href="http://www.codeproject.com/script/Articles/BlogArticleList.aspx?amid=3453924" rel="tag" style="display:none">CodeProject</a></small>