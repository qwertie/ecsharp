README
------

The Language of Your Choice (Loyc) project is intended to become a set of tools for:

- Transforming source code between different languages
- Cross-language library programming, also known as acmeism
- IDEs (code completion lists, various kinds of code visualization, intellisense)
- Code analysis

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
- Threading:
- Misc:
  - Symbol, GSymbol, SymbolPool, SymbolPool<E>
  - SimpleTimer, EzStopwatch
  - GoInterface, GoInterface<Interface>, GoInterface<Interface, T>
  - ITags<T>, HashTags<T>
  - ICloneable<T>
  - IMessageSink<T>
  - Localize.From()
  
  
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