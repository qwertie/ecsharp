using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.Collections
{
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
	/// Look in <see cref="LCInterfaces"/> for default implementations of the extra methods.
	/// And, as always, <see cref="KeyCollection{K,V}"/>, <see cref="ValueCollection{K,V}"/> 
	/// and <see cref="Impl.DictionaryBase{K,V}"/>will help you implement dictionary types.
	/// </remarks>
	public interface IDictionaryEx<K, V> : IDictionaryImpl<K, V>, ICloneable<IDictionaryEx<K, V>>, ITryGet<K, V>
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

	public static partial class LCInterfaces
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

		/// <summary>Adds a key/value pair to the dictionary if the key is not present. If the 
		/// key is already present, this method has no effect.</summary>
		/// <returns>True if the pair was added, false if not.</returns>
		public static bool AddIfNotPresent<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			return !dict.GetAndEdit(ref key, ref value, DictEditMode.AddIfNotPresent);
		}
		/// <summary>Adds a key/value pair to the dictionary if the key is not already present,
		/// and returns the existing or new value.</summary>
		/// <returns>The existing value (if the key already existed) or the new value.</returns>
		public static V GetOrAdd<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			dict.GetAndEdit(ref key, ref value, DictEditMode.AddIfNotPresent);
			return value;
		}
		/// <summary>Adds a new key/value pair if the key was not present, or gets the existing 
		/// value if the key was present.</summary>
		/// <returns>The existing value. If a new pair was added, the result has no value.</returns>
		/// <seealso cref="GetOrAdd{K, V}(IDictionaryEx{K, V}, K, V)"/>
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
		[Obsolete("This was renamed to GetAndSet")]
		public static Maybe<V> SetAndGet<K, V>(this IDictionaryEx<K, V> dict, K key, V value) => GetAndSet(dict, key, value);
		/// <summary>Associates a key with a value in the dictionary, and gets the old value
		/// if the key was already present.</summary>
		/// <returns>The old value associated with the same key. If a new pair was added,
		/// the result has no value.</returns>
		public static Maybe<V> GetAndSet<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			if (dict.GetAndEdit(ref key, ref value, DictEditMode.AddOrReplace))
				return value;
			return new Maybe<V>();
		}

		public static bool TryAdd<K, V>(this IDictionaryEx<K, V> dict, K key, V value)
		{
			return dict.GetAndEdit(ref key, ref value, DictEditMode.AddIfNotPresent);
		}
	}

	public interface IDictionaryWithChangeEvents<K, V> : IDictionaryAndReadOnly<K, V>, INotifyListChanging<KeyValuePair<K, V>, IDictionary<K, V>>, INotifyListChanged<KeyValuePair<K, V>, IDictionary<K, V>>
	{
	}

	public interface IDictionaryExWithChangeEvents<K, V> : IDictionaryEx<K, V>, IDictionaryWithChangeEvents<K, V>
	{
	}
}
