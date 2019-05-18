using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <summary>This interface models the capabilities of an array: getting and
	/// setting elements by index, but not adding or removing elements.</summary>
	/// <remarks>
	/// Member list:
	/// <code>
	/// public T this[int index] { get; set; }
	/// public T TryGet(int index, ref bool fail);
	/// public Iterator&lt;T> GetIterator();
	/// public int Count { get; }
	/// public IEnumerator&lt;T> GetEnumerator();
	/// System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator();
	/// </code>
	/// </remarks>
	public interface IArray<T> : IListSource<T>, IArraySink<T>
	{
		/// <summary>Gets or sets an element of the array-like collection.</summary>
		/// <returns>The value of the array at the specified index.</returns>
		/// <remarks>
		/// This redundant indexer is required by C# because the compiler imagines
		/// that the setter in <see cref="IArraySink{T}"/> conflicts with the getter
		/// in <see cref="IListSource{T}"/>.
		/// </remarks>
		new T this[int index] { get; set; }

		bool TrySet(int index, T value);
	}

	/// <summary>An interface typically implemented alongside <see cref="ICollection{T}"/>,
	/// for collection types that can add multiple items in one method call.</summary>
	public interface IAddRange<T> : ICount
	{
		void AddRange(IEnumerable<T> e);
		void AddRange(IReadOnlyCollection<T> s);
	}
	
	/// <summary>An interface typically implemented alongside <see cref="IList{T}"/> 
	/// for collection types that can add or remove multiple items in one method 
	/// call.</summary>
	public interface IListRangeMethods<T> : IAddRange<T>
	{
		void InsertRange(int index, IEnumerable<T> e);
		void InsertRange(int index, IReadOnlyCollection<T> s);
		void RemoveRange(int index, int amount);
	}

	/// <summary>Extension methods for Loyc collection interfaces</summary>
	public static partial class LCInterfaces
	{
		public static void Resize<T>(this IListRangeMethods<T> list, int newSize)
		{
			int count = list.Count;
			if (newSize < count)
				list.RemoveRange(newSize, count - newSize);
			else if (newSize > count)
				list.InsertRange(count, (IListSource<T>)ListExt.Repeat(default(T), newSize - count));
		}

		//public static void Sort<T>(this IListRangeMethods<T> list)
		//{
		//    list.Sort(0, list.Count, Comparer<T>.Default.Compare);
		//}
		//public static void Sort<T>(this IListRangeMethods<T> list, Comparison<T> comp)
		//{
		//    list.Sort(0, list.Count, comp);
		//}
		//public static void Sort<T>(this IListRangeMethods<T> list, IComparer<T> comp)
		//{
		//    list.Sort(0, list.Count, comp.Compare);
		//}
		//public static void Sort<T>(this IListRangeMethods<T> list, int index, int count, IComparer<T> comp)
		//{
		//    list.Sort(index, count, comp.Compare);
		//}
	}

	/// <summary>This interface combines the original <see cref="ICollection{T}"/> 
	/// with <see cref="IReadOnlyCollection{T}"/>. It exists for the same reason
	/// as <see cref="IListAndListSource{T}"/>, to fix ambiguity errors.</summary>
	public interface ICollectionAndReadOnly<T> : ICollection<T>, IReadOnlyCollection<T> { }

	/// <summary>
	/// This interface combines the original ICollection(T) interface with
	/// IReadOnlyCollection(T), ISinkCollection(T), and IAddRange(T), a convenient 
	/// way to implement all three.
	/// </summary>
	/// <remarks>
	/// IReadOnlyCollection(T) and ISinkCollection(T) are subsets of the ICollection(T)
	/// interface. ICollectionEx the following methods that ICollection(T) does not:
	/// AddRange() and RemoveAll().
	/// </remarks>
	public interface ICollectionEx<T> : ICollectionAndReadOnly<T>, ICollectionSink<T>, IAddRange<T>, IIsEmpty
	{
		/// <summary>Removes the all the elements that match the conditions defined 
		/// by the specified predicate.</summary>
		/// <param name="match">A delegate that defines the conditions of the elements to remove</param>
		/// <returns>The number of elements removed.</returns>
		int RemoveAll(Predicate<T> match);
		// A reasonable default implementation for lists:
		// int RemoveAll(Predicate<T> match) { return LCExt.RemoveAll(this, match); }
	}

	/// <summary>This interface combines the original <see cref="IList{T}"/> 
	/// interface with its "source" (read-only) component interfaces, including 
	/// <see cref="IReadOnlyList{T}"/>, plus <see cref="IListSource{T}"/>.</summary>
	/// <remarks>
	/// This interface is not meant to be used by callers. It exists mainly to 
	/// avoiding ambiguity errors when invoking extension methods in plain C#. For 
	/// example, there is a TryGet() extension method for IList(T) and an identical 
	/// TryGet() method for IListSource(T). To prevent the C# compiler from giving an 
	/// ambiguity error when you try to use TryGet(), 
	/// <ul>
	/// <li>The list class must implement this interface (or <see cref="IListEx{T}"/>), and</li>
	/// <li>There must be a third version of TryGet() that accepts this interface.</li>
	/// </ul>
	/// Ironically, however, if you actually try to use the list through this 
	/// interface you'll tend to get errors. For instance, both <see cref="IList{T}"/>
	/// and <see cref="IReadOnlyList{T}"/> have an indexer, so using the indexer in
	/// this interface is ambiguous.
	/// <para/>
	/// Does not include <see cref="IListSink{T}"/> because this interface may be 
	/// implemented by list classes that are read-only.
	/// </remarks>
	public interface IListAndListSource<T> : IList<T>, IListSource<T>, ICollectionAndReadOnly<T> { }

	/// <summary>This interface combines the original <see cref="IDictionary{K, V}"/> 
	/// interface with its read-only version, <see cref="IReadOnlyDictionary{K, V}"/>.</summary>
	/// <remarks>
	/// This interface is not meant to be used by callers. It exists mainly to 
	/// avoiding ambiguity errors when invoking extension methods in plain C#. For 
	/// example, there is a TryGetValue() extension method for IDictionary(T) and an identical 
	/// TryGetValue() method for IReadOnlyDictionary(T). To prevent the C# compiler from giving 
	/// an ambiguity error when you try to use TryGetValue(), 
	/// <ul>
	/// <li>The dictionary class must implement this interface, and</li>
	/// <li>There must be a third version of TryGetValue() that accepts this interface.</li>
	/// </ul>
	/// Ironically, however, if you actually try to use the list <i>through</i> this 
	/// interface you'll tend to get errors. For instance, both <see cref="IDictionary{K,V}"/>
	/// and <see cref="IReadOnlyDictionary{K,V}"/> have an indexer, so using the indexer in
	/// this interface is ambiguous.
	/// </remarks>
	public interface IDictionaryAndReadOnly<K, V> : IDictionary<K, V>, IReadOnlyDictionary<K, V>, IDictionarySink<K, V> { }

	/// <summary>Helper enum for <see cref="IDictionaryEx{K, V}.GetAndEdit"/>.</summary>
	[Flags]
	public enum DictEditMode
	{
		/// <summary>Do not change the collection.</summary>
		Retrieve = 0,
		/// <summary>Replace an existing pair if present, or do nothing if no matching key.</summary>
		ReplaceIfPresent = 1,
		/// <summary>Add a new item if the key doesn't match an existing pair, or do nothing if it does.</summary>
		AddIfNotPresent = 2,
		/// <summary>Insert a key-value pair, replacing an existing one if the key already exists.</summary>
		AddOrReplace = 3,
	}

	/// <summary>Combines <c>IDictionary</c>, <c>IReadOnlyDictionary</c>, and <c>IDictonarySink</c> 
	/// with a few additional methods.</summary>
	/// <remarks>
	/// This interface also derives from ICloneable for the sake of 
	/// implementations such as <see cref="MMap{K,V}"/> that support fast cloning.
	/// <c>RemoveRange()</c> is only provided as an extension method because it is
	/// rare that a dictionary can accelerate bulk removal of an arbitrary sequence.
	/// <para/>
	/// Look in <see cref="DictionaryExt"/> for default implementations of the extra methods.
	/// And, as always, <see cref="KeyCollection{K,V}"/>, <see cref="ValueCollection{K,V}"/> 
	/// and <see cref="Impl.DictionaryBase{K,V}"/>will help you implement dictionary types.
	/// </remarks>
	public interface IDictionaryEx<K,V> : IDictionaryAndReadOnly<K,V>, IDictionarySink<K,V>, ICloneable<IDictionaryEx<K,V>>
	{
		// Disambiguation through duplication: since they decided IDictionary wouldn't
		// derive from IReadOnlyDictionary...and that the two would have different types
		// for Keys and Values...and since property getters and setters are 
		// inappropriately treated as a single unit...ambiguity errors must result when 
		// calling an instance of IDictionaryEx. Method duplication is the only answer.
		// Note that the most appropriate type for Keys and Values would be 
		// IReadOnlyCollection but neither base dictionary type uses that type; there
		// is little to gain by changing the type here.
		new V this[K key] { get; set; }
		new ICollection<K> Keys { get; }
		new ICollection<V> Values { get; }

		// These methods are the same in IDictionary and IReadOnlyDictionary but still 
		// officially conflict. AFAICT duplication is still the only solution.
		new bool ContainsKey(K key);
		new bool TryGetValue(K key, out V value);

		// Resolve the so-called ambiguity between IDictionary and IDictionarySink.
		new void Add(K key, V value);
		new bool Remove(K key);
		new void Clear();

		/// <summary>Gets the value associated with the specified key, then
		/// removes the pair with that key from the dictionary.</summary>
		/// <param name="key">Key to search for.</param>
		/// <returns>The value that was removed. If the key is not found, 
		/// the result has no value (<see cref="Maybe{V}.HasValue"/> is false).</returns>
		/// <remarks>This method shall not throw when the key is null.</remarks>
		Maybe<V> GetAndRemove(K key);

		/// <summary>Combines a get and change operation into a single method call.
		/// You rarely need to call this method directly; the following extension 
		/// methods are based on it: <see cref="DictionaryExt.SwapIfPresent"/>,
		/// <see cref="DictionaryExt.AddIfNotPresent"/>, <see cref="DictionaryExt.AddOrGetExisting"/>,
		/// <see cref="DictionaryExt.ReplaceIfPresent"/>, <see cref="DictionaryExt.SetAndGet"/>.</summary>
		/// <param name="key">Specifies the key that you want to search for in the 
		/// map. Some implementations will update the key with the version of it
		/// found in the dictionary (although the new key is "equal" to the old key, 
		/// it may be a different object); otherwise the key is left unchanged.</param>
		/// <param name="value">If the key is found, the old value is saved  in this
		/// parameter. Otherwise, it is left unchanged.</param>
		/// <param name="mode">The specific behavior of this method depends on this.
		/// See <see cref="DictEditMode"/> to understand its effect.</param>
		/// <returns>True if the pair's key ALREADY existed, false if not.</returns>
		/// <remarks>
		/// This method exists because some collections can optimize certain 
		/// combinations of operations, avoiding the two traversals through the data 
		/// structure that would be required by the IDictionary interface.
		/// <para/>
		/// This method shall not throw when the key is null, unless the 
		/// AddIfNotPresent bit is set in <c>mode</c> and the dictionary does not 
		/// support a null key.
		/// </remarks>
		/// <seealso cref="DictionaryExt.AddIfNotPresent"/>
		bool GetAndEdit(ref K key, ref V value, DictEditMode mode);

		/// <summary>Merges the contents of the specified sequence into this map.</summary>
		/// <param name="data">Pairs to merge in. Duplicates are allowed; if the 
		/// <c>ReplaceIfPresent</c> bit is set in <c>mode</c>, later values take 
		/// priority over earlier values, otherwise earlier values take priority.</param>
		/// <param name="mode">Specifies how to combine the collections.</param>
		/// <returns>The number of pairs that did not already exist in the collection.
		/// if the AddIfNotPresent bit is set on <c>mode</c>, this is the number of new 
		/// pairs added.</returns>
		/// <seealso cref="DictionaryExt.AddRange{K, V}(IDictionary{K, V}, IEnumerable{KeyValuePair{K, V}})"/>
		int AddRange(IEnumerable<KeyValuePair<K, V>> data, DictEditMode mode);
	}
	public static partial class DictionaryExt
	{
		/// <summary>Default implementation of <see cref="IDictionaryEx{K, V}.GetAndEdit"/>.</summary>
		public static bool GetAndEdit<K, V>(IDictionary<K, V> dict, K key, ref V value, DictEditMode mode)
		{
			V newValue = value;
			if (dict.TryGetValue(key, out value))
			{
				if ((mode & DictEditMode.ReplaceIfPresent) != 0)
					dict[key] = newValue;
				return true;
			}
			else
			{
				if ((mode & DictEditMode.AddIfNotPresent) != 0)
					dict[key] = newValue;
				return false;
			}
		}

		/// <summary>Default implementation of <see cref="IDictionaryEx{K, V}.AddRange"/>.
		/// Merges the contents of the specified sequence into this map.</summary>
		public static int AddRange<K, V>(this IDictionary<K, V> dict, IEnumerable<KeyValuePair<K, V>> data, DictEditMode mode)
		{
			var e = data.GetEnumerator();
			int numMissing = 0;
			foreach (var pair in data) {
				K key = pair.Key;
				V val = pair.Value;
				if (!GetAndEdit(dict, key, ref val, mode))
					numMissing++;
			}
			return numMissing;
		}

		/// <summary>Default implementation of <see cref="IDictionaryEx{K, V}.GetAndRemove"/>.
		/// Gets the value associated with the specified key, then removes the 
		/// pair with that key from the dictionary.</summary>
		public static Maybe<V> GetAndRemove<K, V>(this IDictionary<K, V> dict, K key)
		{
			if (dict.TryGetValue(key, out V value)) {
				dict.Remove(key);
				return value;
			}
			return default(Maybe<V>);
		}

		/// <summary>Adds an item to the map if the key is not present. If the 
		/// key is already present, this method has no effect.</summary>
		/// <returns>True if the pair was added, false if not.</returns>
		public static bool AddIfNotPresent<K,V>(this IDictionaryEx<K,V> dict, K key, V value)
		{
			return !dict.GetAndEdit(ref key, ref value, DictEditMode.AddIfNotPresent);
		}
		/// <summary>Gets the existing value, or adds a new value if there was no existing value.</summary>
		/// <returns>The existing value. If a new pair was added, the result has no value.</returns>
		public static Maybe<V> AddOrGetExisting<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			if (dict.GetAndEdit(ref key, ref value, DictEditMode.AddIfNotPresent))
				return value;
			return default(Maybe<V>);
		}
		/// <summary>Replaces an item in the map if the key was already present.
		/// If the key was not already present, this method has no effect.</summary>
		/// <returns>True if a value existed and was replaced, false if not.</returns>
		public static bool ReplaceIfPresent<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			return dict.GetAndEdit(ref key, ref value, DictEditMode.ReplaceIfPresent);
		}
		/// <summary>Replaces an item in the map if the key was already present.
		/// If the key was not already present, this method has no effect.</summary>
		/// <returns>The old value if a value was replaced, or an empty value of <see cref="Maybe{V}"/> otherwise.</returns>
		public static Maybe<V> SwapIfPresent<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			return dict.GetAndEdit(ref key, ref value, DictEditMode.ReplaceIfPresent) ?
				(Maybe<V>)value : default(Maybe<V>);
		}
		/// <summary>Associates a key with a value in the dictionary, and gets the old value
		/// if the key was already present.</summary>
		/// <returns>The old value associated with the same key. If a new pair was added,
		/// the result has no value.</returns>
		public static Maybe<V> SetAndGet<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			if (dict.GetAndEdit(ref key, ref value, DictEditMode.AddOrReplace))
				return value;
			return NoValue.Value;
		}
	}

	/// <summary>
	/// This interface combines the original IList(T) interface with others -
	/// IListSource(T), ISinkList(T), IArray(T) - and some additional methods
	/// (e.g. RemoveAll, InsertRange).
	/// </summary>
	/// <remarks>
	/// <see cref="IArray{T}"/> (a version of <see cref="IListSource{T}"/> that adds the writability of an
	/// array) and <see cref="IListSink{T}"/> are largely subsets of the IList(T) interface. 
	/// IListSource has two methods that IList(T) does not (TryGet() and Slice()), while
	/// <see cref="ICollectionEx{T}"/> adds RemoveAll and AddRange.
	/// <para/>
	/// Just as Iterator scans a collection faster than IEnumerator, TryGet() is intended to
	/// accelerate access to a list at a specific index; see <see
	/// cref="IListSource{T}"/> for more information. TryGet() may be called
	/// in different ways, through extension methods with the same name.
	/// <para/>
	/// Using <see cref="Impl.ListExBase{T}"/> as your base class can help you implement
	/// this interface more easily.
	/// </remarks>
	public interface IListEx<T> : IListAndListSource<T>, ICollectionEx<T>, IArray<T>, IListRangeMethods<T>
	{
	}

	/// <summary>An auto-sizing array is a list structure that allows you to modify
	/// the element at any index, including indices that don't yet exist; the
	/// collection automatically adds missing indices.</summary>
	/// <typeparam name="T">Data type of each element.</typeparam>
	/// <remarks>
	/// This interface begins counting elements at index zero. The <see
	/// cref="INegAutoSizeArray{T}"/> interface supports negative indexes.
	/// <para/>
	/// Although it is legal to set <c>this[i]</c> for any <c>i >= 0</c> (as long
	/// as there is enough memory available for required array), <c>this[i]</c>
	/// may still throw <see cref="ArgumentOutOfRangeException"/> when the 
	/// index is not yet valid. However, implementations can choose not to throw
	/// an exception and return <c>default(T)</c> instead.
	/// </remarks>
	public interface IAutoSizeArray<T> : IArray<T>
	{
		/// <summary>Optimizes the data structure to consume less memory or storage space.</summary>
		/// <remarks>
		/// A simple auto-sizing array can implement this method by examining the
		/// final elements and removing any that are equal to default(T).
		/// </remarks>
		void Optimize();
	}
}
