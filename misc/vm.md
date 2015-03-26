Memory management
-----------------

Ordinarily I take an agressive stance toward supporting all existing programming languages in the design of this VM, but for memory management I would consider making an exception for one simple reason: it is a massive undertaking to create an efficient and reliable GC for multithreaded environments that doesn't stop all threads to perform garbage collection. As far as I know, only Microsoft and Sun, not any indie group or OSS collective, has ever accomplished this feat. It took years for Mono to get a two-generation semi-precise moving GC with worse performance than the Microsoft GC. _Garbage collection is a very tricky component to get right_. I do believe we need some kind of garbage collector, to support existing and new languages, but perhaps it should be optimized for simpler use cases such as single-threaded operation (I can't suggest the fine details, as I am not an expert in GC technology.) At the same time, I believe developers should have a lot of freedom to choose whether their data is garbage-collected or not, as allowed by languages such as D.

Most programming languages have fairly simple memory model: either (A) a single heap that is garbage-collected by a single multi-threaded garbage collector, and shared by all reference types, or (B) manual memory management and/or automatic reference counting with one or more heaps, managed by standard (or nonstandard) libraries. C# and C++/CLI are only slightly more complex: they support both A and B, but there is a strict separation between them, since all non-temporary pointers are typed to either the GC heap or the "native" heap, there is no conversion between or union of the two pointer types, and it is impossible to define a class that can exist outside the GC heap. In contrast, the D language is much more liberal, allowing a single pointer to arbitrarily point to the stack, GC heap, or a non-GC heap, and allowing a class object to be allocated on any heap. This is one of the main reasons you can' run arbitrary D code in .NET.

Genericity
----------

The .NET generics system is a nice start, but it has limited flexibility becuse generic code cannot be specialized for specific types. This makes it impossible in .NET to efficiently extend the type system beyond what it was originally designed to do. For instance, if one creates a programming language that has generic math (so that you can add or multiply two ints, floats, rationales, BigNums, etc., in the same generic function), this cannot be efficient under .NET. Even if the compiler knows in advance all possible types for which a generic math function may be instantiated, and instantiates them all at compile-time, it is difficult for "normal" (JIT-instantiated) generic code to call such functions. For example, consider this math function and a normal generic function that wants to call it:

    // Imagine a language that allows this function, in which the 
    // compiler pre-specializes T for int, float, BigInt, etc.
    // (Suppose $T means that T is specialized at compile time.)
    static T DoMath<$T>(T x, T y, T z) { return x * y + z; }
    // Without runtime specialization, it's very hard for Normal() 
    // to call DoMath().
    static int Normal<T>(T x, T y, T z) { return DoMath(x, y, z); }

Obviously, the specific case of arithmetic can be solved by including arithmetic as part of the interface for numeric types, e.g.

    static T DoMath<T>(T x, T y, T z) where T:INum<T> { return x * y + z; }

But I don't believe this will solve all imaginable problems. D, for instance, allows its generic functions to be specialized based on arbitrary properties of the type parameter; wouldn't it be great if such functions could be specialized at runtime? For that we need some mechanism that can be controlled by a programming language, allowing generic methods to be specialized by data type.

Fixing null
-----------

Null has been called the "billion-dollar mistake", as constantly having to worry that a reference might be null gets quite irritating for more enlightened developers. In .NET there is a further irritation because there are two different null types, a "value-type null" (`default(Nullable<T>)`) and the normal reference-type null, and one is prohibited from writing a generic function that understands both kinds of null at the same time.

Null is, however, deeply ingrained in the design of the .NET and Java VMs, and cannot simply be eliminated. What we can do, however, is to add a new category of reference types that cannot be null, and _harmonize_ null so that the "value-type null" can be compatible with the reference-type null in generic code. The type system will recognize which references can be null, and may have additional optimization opportunities in case a reference is non-null.

The simplest way to harmonize nulls is to declare that nullable value types are allocated on the heap. This has a performance disadvantage in many cases, but is an advantage in cases where the value is _usually_ null, since the null value itself is cheaper than a multi-word .NET `Nullable<T>`.

Union and intersection types
----------------------------

Union and intersection types were pioneered by the [Ceylon language](http://ceylon-lang.org), and I am a big fan.

Values of an intersection type `A & B` have both type A and type B simultaneously. .NET generics already support intersection types in a specific context, generic parameter constraints:

		public static void Normalize<T>(this BoundingBox<T> self) 
		  where T : IConvertible, IComparable<T>, IEquatable<T>

In this example, `T` refers to the intersection type `IConvertible & IComparable<T> & IEquatable<T>`.

Union types are more useful. A value of union type `A | B` has either type `A` or type `B`. Having this idea in the type system allows new optimizations, because if a value `t` has type `A | B` and we do a type test to check "is this an A?" and the answer is no, then we _know_ `t` is a `B` and can treat it as a `B` without needing a downcast. But this concept is useful even without optimizations; for example it allows functions to specify more precisely what types they accept and return, instead of using vague types like "object". Union types also help solve the "null problem".

In a system with union types, `null` can be viewed as a perfectly ordinary singleton value of a perfectly ordinary `Null` class. The VM will treat it specially, of course, but at the type system level there is nothing special about null. Non-nullable types, then, are simply those that do not include `Null` in the union.

In a VM designed for Java and .NET compatibility, it probably makes the most sense to implement union types as reference types. Thus values of type (A | B) are always stored on the heap even if A and B are both value types. Perhaps this could be invisible to the type system, though, which would enable the runtime to store small unions inline.

For certain types like `String | Bool | Null` the runtime could use a bit twiddling trick to save memory. On all modern architectures, the low bit of all object references will always be zero. Thus we may form a union between any single value type smaller than a reference, and any collection of reference types, such as `Short | String | Regex | Null`. If the low bit is 1, the "reference" directly stores a value type. Otherwise it stores one of the other types in the union.

Many programming languages (e.g. Haskell, F#, Nemerle, Rust) have a concept of "algebric data types" (ADTs) or non-extensible discriminated unions. Union types seem to be a strictly more general concept, as they are extensible (given any union A|B|C you can always add a new type D); thus if the VM supports union types, it also supports ADTs. ADTs can be simulated without union types using a base class and a series of derived classes, but union types add precision (the ability to say precisely which subclasses are allowed) and flexibility (such as the ability to use value types as elements of the union, even though value types cannot have a base class).

Union types also provide a natural form of extensible enum type, since you can combine any enum with another enum.

The empty union type ("Nothing" in Ceylon) has no instances. As a return type, it indicates that a function cannot return normally (so it either throws an exception, or runs forever).

Delegates
---------

Getting a one-word pointer to a method should be trivial (less costly than calling the method). Such pointers can be used as building blocks of nonstandard method dispatch techniques.

A normal delegate pairs a function pointer and an object pointer, and so is two words. For most developers, delegates are much more common and much more useful than plain function pointers, and are used much more often than the more general concept of currying. In functional-style code, delegates will be used constantly and need to be highly optimized (in a functional program, there may be no "current object" but functions will still have a context, which is the closure or environment of parameters of outer functions.)

Therefore, typical delegates should be two words and not require memory allocation, in contrast to the inefficient .NET delegates which are 4 or 5 words and are allocated on the heap. .NET delegates not only include a function pointer and object pointer, but also a handle to the method so that you can get the reflection object (MethodInfo) associated with the method. This handle is almost never used, but the VM should offer .NET compatibility, so what shall we do? I propose it should be possible to (somehow) look up the reflection handle for any function pointer. The only problem is that the VM will naturally want to share method bodies of simple functions. Many functions simply read or write a single field, and the runtime could share machine code for two unrelated functions (with different type signatures!) that happen to read or write from the same offset from the `this` pointer. One simple solution would be that all .NET code be marked with a flag to disable machine-code sharing so that all function pointers are unique, allowing the reflection info to be found reliably. Of course, this would mean that .NET code cannot reliably get a reflection handle for non-.NET code, but as I mentioned, this handle is almost never used anyway. There are some details to work out--different instances of the same generic method ought to be able to share machine code.

Something must be done about static methods. Static methods do not have a context parameter, yet we need to be able to have delegates that refer to them. For efficiency in the common case, delegates will be always called with a context parameter. Either we can declare that all static functions take a "dummy" context parameter which is ignored by the function, or else we can use special thunks for static functions, such that the context parameter is actually a pointer to the static function and the thunk's job is to shift the other parameters to the left to eliminate the dummy parameter before calling the real function. The latter solution has the advantage that one can create delegates that bind the context word to first parameter of a static method, using the normal entry point instead of a thunk.

You might think that two-word delegates don't allow us to accomplish multicast delegates, but this is incorrect. To combine any two delegates, one simply needs a forwarding function for each:

    // Pseudocode, because C#/C++/etc. don't support this directly.
    D Combine<D>(D first, D second) where D:delegate
    {
        // Define a function that takes the same parameters as D and 
        // has the same return type as D. Perhaps to make forwarding
        // functions like this possible, the VM should treat parameter
        // lists as tuples (but this means tuple types themselves will
        // be a built-in feature of the VM).
        D.ReturnType Combined(params D.ParameterTypeTuple p)
        {
            first(p);
            return second(p);
        }
        return Combined;
    }

The VM should be powerful enough that it is possible to actually define a piece of code that does this, so that multicast delegates need not be built into the VM. Perhaps delegates themselves need not be built-in; we need _standard_ delegate types, of course, so that delegates can be shared between different programming languages on the VM, but the VM, which lays below the standard library, need not be aware of delegates.
