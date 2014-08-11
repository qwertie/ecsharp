## `InternalList<T>`: a low-level alternative to List<T>

For some of us who write low-level code that is intended to be compact and fast, the standard `List<T>` can seem like overkill because it requires two separate heap objects, and must perform extra checks that a plain array does not. For that reason, if you're writing code that you want to be as fast as possible, you might consider using a `T[]` array instead of `List<T>`.

### The downside to `List<T>`

From a performance perspective, there are two things that make a `List<T>` "worse" than a plain array: size, and speed.

#### Size

A `List<T>` requires at least two heap allocations: one for the `List<T>` object itself, and another for the array backing the `List<T>`. Each .NET object has a two-word header, and `List<T>` has four fields:

~~~csharp
private T[] _items;
private int _size;
private object _syncRoot; // not used unless you call SyncRoot
private int _version;
~~~

So `List<T>` requires six additional words of memory compared to an array (I mean "machine words" in the traditional sense, meaning an IntPtr-sized memory area, so the total is 24 extra bytes in a 32-bit process and probably 40 bytes in 64-bit, assuming the CLR fits `_size` and `_version` into a single word.)

#### Speed

`List<T>` must also perform extra range checks. For example, the indexer looks like this:

~~~csharp
public T this[int index]
{
    get {
        if (index >= _size)
            ThrowHelper.ThrowArgumentOutOfRangeException();
        return _items[index];
    }
    set {
        if (index >= _size)
            ThrowHelper.ThrowArgumentOutOfRangeException();
        _items[index] = value;
        _version++;
    }
}
~~~

Note that the `_items[index]` operation implicitly contains a range check: the CLR performs a check equivalent to `(uint)index < (uint)_items.Length` before it actually reads from an array or writes to it. So there are actually two range checks here. The JIT usually does not know that `_size < _items.Length` (and besides, the `index >= _size` check does not verify that `index >= 0`) so it cannot remove the second check based on the result of the first check.

### The downside to `T[]`

Unfortunately, if you use a plain array instead, you can't simply "add" or "remove" items, since an array has a single fixed size. Consequently if you decide that you really want a plain array but you need to add, remove, or (heaven forbid!) insert items, you'll end up pretty much reimplementing `List<T>` by yourself. You'll have an array variable and a count variable...

    private T[] _array;
    private int _count;

and then you might write a bunch of code to do the things that `List<T>` already does, like insertions:

    static T[] Insert<T>(int index, T item, T[] array, ref int count)
    {
        Debug.Assert((uint)index <= (uint)count);
        if (count == array.Length) {
            int newCap = array.Length * 2;
            array = CopyToNewArray(array, count, newCap);
        }
        for (int i = count; i > index; i--)
            array[i] = array[i - 1];
        array[index] = item;
        count++;
        return array;
    }
    static T[] CopyToNewArray<T>(T[] _array, int _count, int newCapacity)
    {
        T[] a = new T[newCapacity];
        Array.Copy(_array, a, _count);
    }

Of course, this road leads to madness. Luckily, there's no need to ever write code like this: just use `InternalList<T>` instead!

## Introducing InternalList

    [Serializable]
    public struct InternalList<T> : IListAndListSource<T>, 
                  IListRangeMethods<T>, ICloneable<InternalList<T>>
    {
        public static readonly T[] EmptyArray = new T[0];
        public static readonly InternalList<T> Empty = new InternalList<T>(0);

        private T[] _array;
        private int _count;

        public InternalList(int capacity) {...}
        public InternalList(T[] array, int count) { _array=array; _count=count; }
        public InternalList(IEnumerable<T> items) : this(items.GetEnumerator()) {}
        public InternalList(IEnumerator<T> items) {}

        public int Count {...}
        public int Capacity {...}
        public void Resize(int newSize, bool allowReduceCapacity = true) {...}
        public void Add(T item) {...}
        ...
    }

To eliminate the extra memory required by `List<T>`, InternalList is a `struct` rather a `class`; and for maximum performance, it asserts rather than throwing an exception when an incorrect array index is used, so that Release builds (where `Debug.Assert` disappears) run as fast as possible.

`InternalList` also has an `InternalArray` property that provides access to the internal array. This actually allows you to work around certain pesky problems with the ordinary `List<T>`. For example, an ordinary List<T> doesn't allow you to do this:

    List<Point> pts;
    ...
    // error CS1612: Cannot modify the return value of 'List<Point>.this[int]' because it is not a variable
    pts[0].X = 5;

But if `pts` is an `InternalList` then you can write `pts.InternalArray[0].X = 5;`

`InternalList<T>` has other things that `List<T>` doesn't, such as a `Resize()` method (and an equivalent setter for `Count`), and a handy `Last` property to get or set the last item.

But it should be understood that `InternalList` is only meant for rare cases where you need better performance than `List<T>`. It does have major disadvantages:

1. You must not write `new InternalList<T>()` because C# does not support default constructors and `InternalList<T>` requires non-null initialization; methods such as `Add()`, `Insert()` and `Resize()` assume `_array` is not null. The correct initialization is `InternalList<T> list = InternalList<T>.Empty;`

2. Passing this structure by value is dangerous because changes to a copy of the structure may or may not be reflected in the original list. In particular the `_count` of the original list won't change but the contents of `_array` _may_ change. It's best not to pass it around at all, but if you must pass it, pass it by reference. This also implies that an `InternalList<T>` should not be exposed by any `public` API, and storing `InternalList<T>` inside another collection (e.g. `Dictionary<object, InternalList<T>>` can be done but must be done carefully to avoid code that _compiles_ but doesn't work as intended.

Again, the fundamental problem is that when you pass `InternalList` by value, a copy of the `_count` and `_array` variables is made. Changes to those variables do not affect the other copies, but changes to the _elements_ of `_array` _do_ affect other copies. If you want to return an internal list from a public API you can cast it to `IList<T>` or `IReadOnlyList<T>`, but be aware that future changes made to the `InternalList` by your code may not be reflected properly in client code, and vice versa. 

Finally, alongside `InternalList<T>`, there is a `static class InternalList` that has some static methods (`CopyToNewArray`, `Insert`, `RemoveAt`, `Move` to help manage raw arrays. Most methods of `InternalList<T>` simply call methods of `InternalList`.

## Download

`InternalList<T>` is part of Loyc.Essentials.dll
You can see the source code [here](https://github.com/qwertie/Loyc/blob/master/Src/Loyc.Essentials/Collections/Implementations/InternalList.cs) but I must warn you, it cannot be used directly. 

