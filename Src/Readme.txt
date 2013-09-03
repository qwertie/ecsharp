README
------
September 3, 2013

The Language of Your Choice (Loyc) project is intended to become a rich set of tools for:

- Transforming source code between different languages
- Cross-language library programming, also known as acmeism
- IDEs (code completion lists, various kinds of code visualization, intellisense)
- Code analysis and transformation

Enhanced C#, LES (Loyc Expression Syntax) and LEL (Loyc expression langauge) are also under the Loyc umbrella.

It is in very early stages right now because I am working on it alone. I am focusing on the first point right now (transforming code between languages). Also, LES is working and I'm busy developing early stages of LEL. The project also currently includes a set of general-purpose libraries that will eventually be spun off into a separate project for a cross-language standard library.

Source code overview
--------------------

Loyc currently contains the following projects, listed in order from the lowest level to the highest level. The dependency tree is

          Loyc.Essentials
                 |
          Loyc.Collections
                 |
            Loyc.Syntax
                 |
           Loyc.Utilities
                 |
  +------+-------+-+------------+-------+----+
  |      |         |            |       |    |
Tests  LLLPG* MiniTestRunner  Baadia*  LEL  Ecs*

* I will eventually split out LLLPG, Baadia, Ecs (Enhanced C#), and the low-level libraries (Essentials, Collections) into separate projects on SourceForge or GitHub.

Terminology:
- LLLPG is a parser generator (Loyc LL(k) Parser Generator) to help make fast recursive-descent parsers
- LEL is a LISP-inspired statically-typed programming language based on LES (Loyc Expression Syntax)
- EC# is an enhanced version of C# that does not exist yet but which will have tons of new features
- Baadia is a gesture-based program for drawing "Boxes and arrows" diagrams (yes, it totally doesn't belong here.)

I am currently keeping the unit tests in the same assemblies as the code being tested. I suspect this is why my libraries tend to be larger than many other "small" .NET libraries. Eventually I'll move the unit tests out into their own assemblies. 

Loyc.Essentials
---------------

A set of low-level interfaces, structures, functions, and helper classes. Basically, this library contains the kinds of things that would be in the Microsoft BCL if I had been in charge.

- Math (Loyc.Math namespace): 
  - Misc. math (MathEx.cs)
    - Two of my favorite methods: InRange and IsInRange
    - Integer bit-fiddling (RoL, RoR, CountOnes, Log2Floor...)
    - Floating-point bit-fiddling (NextHigher, NextLower, Int64BitsToDouble...)
    - Generic math that works on both integers and floating point (Sqrt, Shift, Mod, Sign, MulDiv, Square)
    - Generic pair operations (Swap, SortPair, Average, Min, Max)
  - Helper code for doing math in .NET Generics (Interfaces.cs, Maths.tt);
  - 128-bit arithmetic (Math128)
  - Fixed-point types (FixedPoint.tt)
- 2D/3D Geometry (Loyc.Geometry namespace): types are parameterized on coordinate type
  - Interfaces: IRectangle<T>, IRectangle3<T>, IPoint<T>, IPoint3<T>
  - 2D implementations: Point<T>, Vector<T>, LineSegment<T>, BoundingBox<T>
  - 3D implementations: Point3<T>, Vector3<T>, LineSegment3<T>
- Threading: helps implement the "Ambient Service Pattern" in a multithreaded environment
  - ThreadEx: A thread that propagates thread-local variables from "parent" to "child" threads
  - ThreadLocalVariable<T>: an alternative to [ThreadStatic], compatible with ThreadEx
  - PushedTLV<T>: helper struct for temporarily changing a ThreadLocalVariable<T>
- Misc:
  - Symbol, GSymbol, SymbolPool, SymbolPool<E> - Singleton strings based on Ruby, Lisp, etc.
  - SimpleTimer, EzStopwatch - More convenient to use than BCL's Stopwatch
  - GoInterface, GoInterface<Interface>, GoInterface<Interface, T>
  - ITags<T>, HashTags<T> - a way of attaching properties to existing objects)
  - Localize.From() - a simple pluggable mechanism for internationalization)
  - ICloneable<T> - a Clone() method that returns T
  - IMessageSink<T> etc. - a simple, generic mechanism to write logs & compiler warnings/errors
  - MiniTest.cs, RunTests.cs - stripped-down NUnit lookalike to remove need for separate test framework
    (note: I am currently still using NUnit)
  - Pair<A,B> - A struct with A and B values (in contrast, Tuple<A,B> allocates on the heap)
  - WeakReference<T> - Strongly-typed weak reference, plus WeakNullReference<T>.Singleton for null.
- Compatibility with earlier .NET versions:
  - Defines IReadOnlyCollection<T>, IReadOnlyList<T>, IReadOnlyDictionary<T> before .NET 4.5
- Collections:
  - Collection interfaces:
    - ICount, IListSource<T>, INotifyListChanging<T>, IAutoCreatePool<K,V>
    - Ranges: IFRange<T>, IMFRange<T>, IBRange<T>, IMBRange<T>, IRange<T>, IMRange<T>
              IBRangeEx<T>, IBRangeEx<RangeT,T>, IRangeEx<T>, IRangeEx<RangeT,T>
    - Immutable sets: ISetImm<T, SetT>, ISetTests<SetT>, ISetOperations<T, SetT>
    - Extended enumerators: IBinumerator<T>, IMEnumerator<T>, IMBinumerator<T>, IBinumerable<T>
    - Queue interfaces: IPush<T>, IPop<T>, IQueue<T>, IStack<T>, IDeque<T>
    - Read-write interfaces: IArray<T>, IAddRange<T>, IListRangeMethods<T>, IAutoSizeArray<T>
      - Unifying interfaces: ICollectionEx<T>, IListEx<T>
    - Sink interfaces: IHasAdd<T>, ISinkCollection<T>, ISinkArray<T>, ISinkList<T>
    - Lists that are not zero-based: INegListSource<T>, INegArray<T>, INegAutoSizeArray<T>, INegDeque<T>
  - Collection implementations (only simple collections are defined in Loyc.Essentials):
    - DList<T>, InternalDList<T>, InternalList<T>, WeakKeyDictionary<K,V>, WeakValueDictionary<K,V>
  - Helper classes:
    - Slices: ArraySlice<T>, ListSlice<T>, Slice_<T>, StringSlice, UString
    - Mutability adapters: ListAsListSource<T>, ListSourceAsList<T>, CollectionAsSource<T>, SourceAsCollection<T>
    - Index-offset adapters: NegList<T>, NegListSource<T>
    - Reverse-view adapters: ReverseBinumerator<T>, ReversedList<T>, ReversedListSource<T>
    - Linq adapters: SelectListSource<T,TResult>
    - Other adapters: RangeEnumerator<T>, RangeEnumerator<R, T>, SelectNegLists<T>, SelectNegListSources<T>
    - Empty immutable collections: EmptyList<T>, EmptyEnumerator<T>
    - Misc: BufferedSequence<T>, NestedEnumerator<Frame,T>
    - Sequence generators: IntRange, Repeated<T>
    - Dictionary helpers: KeyCollection<K,V>, ValueCollection<K,V>
  - Base classes: BaseDictionary<K,V>, ListExBase<T>, ListSourceBase<T>, SourceBase<T>
  - Debugger display helper: CollectionDebugView<T>
    - see http://www.codeproject.com/Articles/28405/Make-the-debugger-show-the-contents-of-your-custom
  - Extension methods
    - Views: list.Slice(start, len), list.ReverseView(), list.NegView(zeroOffset)
    - Adapters: ICollection.AsSource(), IReadOnlyCollection.AsCollection(), 
                IList.AsListSource(), IListSource.AsList(), IEnumerable.Buffered()
    - Queries (here, 'e' means IEnumerable<T>, 'list' refers to one or more list types)
      - Upcasting: list.UpCast<T,TResult>()
      - list.ForEach(lambda)
      - list.Select(lambda) where list is IListSource<T>
      - e.AdjacentPairs(), e.AdjacentPairsCircular(),
      - e.Zip(other), e.ZipLeft(...), e.ZipLonger(...)
      - list.WhereNotNull(),
      - list.SelectArray()
      - list.IndexWhere(lambda), list.LastIndexWhere(lambda), list.IndexOf(...)
      - e.IndexOfMin(...), e.IndexOfMax(...)
      - e.MaxOrDefault(...), e.MinOrDefault(...)
      - e.Join(separator) - joins list items into a string
      - list.CopyTo(T[], arrayIndex), 
      - list.Randomized()
      - list.TryGet(index, defaultValue)
    - Mutators:
      - list.Reverse()
      - list.Resize(newSize)
      - list.AddRange(IEnumerable<T>), list.RemoveRange(index, count)
      - list.RemoveAll(lambda)
      - list.Randomize()
      - list.Sort(...), list.StableSort(...), list.InsertionSort(...), 
      - list.Swap(i, j)
    - For ranges:
      - range.Skip(count), range.DropLast(count)
      - range.PopFirst(), range.PopFirst(defaultValue)
      - range.PopLast(), range.PopLast(defaultValue)
      - range.Contains(IRangeEx), range.Overlaps(IRangeEx)
    - Non-extension helper methods: 
      - ListExt.SortPair(list, i, j, comp)
      - ListExt.RangeArray(count)
      - Range.Single(value), Range.Repeat(value, count)
      - Range.IntRange(start, count)
- Non-collection extension methods
  - Type.NameWithGenericArgs() - e.g. typeof(List<int>).NameWithGenericArgs() == "List<Int32>"
  - Exception.ToDetailedString()
  - string.SplitAt(char)
  - string.Right(count), string.Left(count), string.TryGet(...), StringBuilder.TryGet(...), 
    string.SafeSubstring(index, count), string.USlice(start, count), string.Find(...)...

Loyc.Collections
----------------

I seem to be drawn to creating general-purpose data structures; most of these data structures can be found in this assembly. Out of all these, my favorite data structure is the persistent hashtables, although I didn't write an article about them yet.

- VLists: see http://www.codeproject.com/Articles/26171/VList-data-structures-in-C
  - FVList<T>, RVList<T>, FWList<T>, RWList<T>
- Persistent (forkable/copy-on-write) hashtables, mutable and immutable:
  - Set<T>, Map<K,V> - immutable set structure and dictionary class
  - MSet<T>, MMap<K,V> - mutable set and dictionary classes
  - InvertibleSet<T> - an immutable set that can be inverted, meaning "everything except { a, b, c }"
  - InternalSet<T> - common implementation behind all five data types
- ALists: see http://www.codeproject.com/Articles/568095/The-List-Trifecta-Part-1
  - AList<T>, BList<T>, BDictionary<K, V>, BMultiMap<K, V>
- CPTries: see http://www.codeproject.com/Articles/61230/CPTrie-A-sorted-data-structure-for-NET
  - CPIntTrie<TValue>, CPStringTrie<TValue>, CPByteTrie<TValue>
- Misc:
  - KeylessHashtable<V>
  - SimpleCache<T>

Loyc.Syntax
-----------

A core part of Loyc is the concept of the "Loyc tree". If you are not familiar with Loyc trees, read this article: http://loyc-etc.blogspot.ca/2013/04/the-loyc-tree-and-prefix-notation-in-ec.html

Loyc.Syntax contains

- CodeSymbols, a static class that defines the names of most built-in features of Enhanced C#.
  In the future, I hope we will define some kind of "standard imperative language" that will 
  use many of the same symbol names as EC#. In that case, perhaps CodeSymbols will just contain
  the standard (lower-level) symbols and EC# will have its own class for EC#-specific features.
- My Loyc tree implementation (Nodes folder).
  - LNode class represents a subtree of source code. Currently, all nodes are immutable.
  - Typically one uses LNodeFactory to create LNodes in C#.
- Facilities for representing source code and source code locations (SourceFiles folder)
  - The design of this portion is unstable. You might see "leftover code" that isn't being used.
  - The ISourceFile interface and its subinterfaces represent a source file of characters (.NET chars).
  - EmptySourceFile, a dummy object that can be used as the "source file" for synthetic nodes
  - SourceRange, a structure that holds an ISourceFile reference and a range of indexes (two integers)
  - SourcePos, a position represented as a file name string, a line number, and a column number
    - The SourceRange.Begin and SourceRange.End properties create SourcePos objects on-the-fly
- Facilities for implementing lexers a.k.a. scanners/tokenizers (Lexing folder):
  - Again, I'm not confident about the design and may change it.
  - ILexer, a suggested interface for lexers to implement
  - BaseLexer<TSource>, a suggested base class for implementing lexers using LLLPG
  - Token, a universal 4-word structure (3 ints and an object reference) for tokens in any language
  - TokenTree, a list of tokens plus an ISourceFile that is common to all the tokens
  - TokensToTree: a class that arranges tokens into a tree, grouping tokens by (), {} and [].
- BaseParser<Token>, a base class designed for parsers generated by LLLPG
- IntSet, a class for holding a collection of integers or characters, expressed as a series of integer ranges.
- LES (Loyc Expression Syntax) support (Les folder, Loyc.Syntax.Les namespace)
  - LesLexer class
  - LesParser class
  - TokenType enum
  - LesNodePrinter class (currently very rudimentary)

Loyc.Utilities
--------------

This assembly contains miscellaneous code that is not considered universal enough to be in Loyc.Essentials, or classes that depend on Loyc.Collections and/or Loyc.Syntax. Some of the stuff in here is old and should be removed.

Currently, the most useful stuff in here is probably the Geometry code:
- BoundingBoxMath class: algorithms involving bounding boxes, e.g. Point.ProjectOnto(bbox)
- LineMath class: algorithms involving lines or line segments,
  e.g. SimplifyPolyline(), Point.ProjectOnto(line), line1.ComputeIntersection(line2, ...)
- PointMath class: algorithms involving points and vectors,
  e.g. a.Dot(b), a.Cross(b), vec.Normalized(), ComputeConvexHull(), etc.
- PolygonMath class: algorithms involving polygons,
  PolygonArea(), Orientation(), IsPointInPolygon(), etc.

LLLPG
-----

Contains the Loyc LL(k) Parser Generator. It is quite capable of generating parsers, but the input language is not complete, i.e. there is no truly convenient way to provide input to LLLPG. If you're interested, the Bootstrap folder contains code for generating a lexer and parser for LES.

Please see this blog entry regarding LLLPG's capabilities and my plan for finishing it:

http://loyc-etc.blogspot.ca/2013/06/lllpg-loyc-trees-and-les-oh-my.html

MiniTestRunner
--------------

I wanted to make a lightweight unit test framework and a convenient unit-test runner that runs tests automatically whenever an .exe/.dll file changes. I did not finish it, though. It can run tests but I had trouble figuring out how to design the system so that it could unload .exe/.dll files, leaving behind a representation of the test tree, and then reload the same .exe/.dll files and update the test tree to reflect any changes. 

Basically, I have a mental block that's preventing me from finishing the program. Also, I really wanted to be able to run the test assemblies in "partial trust" mode. Theoretically AppDomains are supposed to support securely running untrusted code, but in practice it's undocumented and very hard to accomplish, and I had to give up.

It is a WinForms app structured with a model-view-viewmodel (MVVM) architecture based on the Update Controls library.

Baadia
------

Boxes and Arrows diagram maker. It's VERY incomplete, but the mouse gestures for creating boxes work perfectly.

- Draw a box and you get a box; draw a line and you get a line.
- Select a box or line and start typing to put text in the box or along the line
- Click empty space and start typing to get free-floating text
- Double-click a line's endpoint to toggle between an arrow, a cicle, a diamond, or nothing.
- Double-click a box to toggle between a rectangle, an ellipse or no border.
- Double-click blank space to place a marker icon.
- "Scribble" a line or box to erase it. More specifically, you must reverse direction repeatedly a.k.a. "zig-zag"
- If you make a mistake while drawing something, you can (while still holding the mouse button)
  1. "Back up" the mouse to undo earlier mouse input
  2. Scribble (zig-zag) to cancel the current drawing command (or press Escape)
- While drawing something, "scribble"
- Work in progress.
  - Saving/loading is supported except that anchoring from arrows to boxes are lost.
  - Clipboard commands don't work.
  - No user interface for selecting line/box styles
  - Cannot multiselect and then move multiple items
  - I have lots of other ideas left to implement

LEL
---

Loyc Expression Language. Just starting it, stay tuned.

Ecs
---

This project will implement Enhanced C#. You may find my design documents interesting; I started designing EC# 1.0 some time in 2012 and then abandoned it in favor of more limited, focused improvements based on ideas from LISP, which I am calling EC# 2. Currently, the source tree contains several EC# design documents:

- EC#.cs: Design ideas for EC# 1.0. Although I don't have time to implement all these features in the foreseeable future, I hope with community support EC# will get them all.
- EC#2.cs: A brief document that explains how the project changed direction and then barfs out some tentative ideas.
- EC# for language pundits: a long, detailed overview of EC# 2 that tries to explain my thought processes and design ideas in a well-structured and accessible way.
- EC# for normal people: an overview of EC# 2 that explains the language in terms of new features that are useful for ordinary developers.

The only part of EC# that is fairly complete right now is the Node Printer (not the parser). This means that you can construct a syntax tree of EC# code and print it, but you cannot parse that same code from text into a syntax tree. This, of course, is a very unusual way to develop a compiler. The explanation is that I plan to compile EC# code into plain C# code at first; therefore I need a good node printer just as much as I need a parser. Since C# is a subset of EC#, an EC# node printer is capable of printing C# code too, as long as the code doesn't use any EC#-specific features.

