using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A dictionary with weak keys.</summary>
	/// <remarks>
	/// Original source: datavault project. License: Apache License 2.0
	/// </remarks>
    [Obsolete("It is proposed to use the standard `ConditionalWeakTable` class instead.")]
	public sealed class WeakKeyDictionary<TKey, TValue> : DictionaryBase<TKey, TValue>
        where TKey : class
    {
        // All keys actually have type WeakKeyReference<TKey>; the key type is 
		// object in order to allow comparing weak references to strong references
		// (e.g. dictionary.Contains(strongRef))
		private Dictionary<object, TValue> dictionary;
        private WeakKeyComparer<TKey> comparer;

        public WeakKeyDictionary()
            : this(0, EqualityComparer<TKey>.Default) { }

        public WeakKeyDictionary(int capacity)
            : this(capacity, EqualityComparer<TKey>.Default) { }

        public WeakKeyDictionary(IEqualityComparer<TKey> comparer)
            : this(0, comparer) { }

        public WeakKeyDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.comparer = new WeakKeyComparer<TKey>(comparer);
            this.dictionary = new Dictionary<object, TValue>(capacity, this.comparer);
        }
        
        /// <summary>Number of items in the collection.</summary>
        /// <remarks>WARNING: The count returned here may include entries for which
        /// key value objects have already been garbage collected. Call 
        /// RemoveCollectedEntries to weed out collected entries and update the count 
        /// accordingly.</remarks>
        public override int Count
        {
            get { return this.dictionary.Count; }
        }

        public override void Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");
            WeakKeyReference<TKey> weakKey = new WeakKeyReference<TKey>(key, this.comparer);
            this.dictionary.Add(weakKey, value);
        }

        public override bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public override bool Remove(TKey key)
        {
            return this.dictionary.Remove(key);
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        protected override void SetValue(TKey key, TValue value)
        {
            WeakKeyReference<TKey> weakKey = new WeakKeyReference<TKey>(key, this.comparer);
            this.dictionary[weakKey] = value;
        }

        public override void Clear()
        {
            this.dictionary.Clear();
        }

        public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<object, TValue> kvp in this.dictionary)
            {
                WeakKeyReference<TKey> weakKey = (WeakKeyReference<TKey>)(kvp.Key);
                TKey? key = weakKey.Target;
                TValue value = kvp.Value;
                if (weakKey.IsAlive)
                {
                    yield return new KeyValuePair<TKey, TValue>(key!, value);
                }
            }
        }

        // Removes the left-over weak references for entries in the dictionary
        // whose key has already been reclaimed by the garbage
        // collector. This will reduce the dictionary's Count by the number
        // of dead key-value pairs that were eliminated.
        public void RemoveCollectedEntries()
        {
            List<object>? toRemove = null;
            foreach (KeyValuePair<object, TValue> pair in this.dictionary)
            {
                WeakKeyReference<TKey> weakKey = (WeakKeyReference<TKey>)(pair.Key);
                if (!weakKey.IsAlive)
                {
                    if (toRemove == null)
                        toRemove = new List<object>();
                    toRemove.Add(weakKey);
                }
            }

            if (toRemove != null)
            {
                foreach (object key in toRemove)
                    this.dictionary.Remove(key);
            }
        }
    }
}
