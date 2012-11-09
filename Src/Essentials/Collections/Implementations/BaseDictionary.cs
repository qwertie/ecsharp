using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Collections
{
	/// <remarks>Source: datavault project. License: Apache License 2.0</remarks>
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(PREFIX + "DictionaryDebugView`2" + SUFFIX)]
    public abstract class BaseDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private const string PREFIX = "System.Collections.Generic.Mscorlib_";
        private const string SUFFIX = ",mscorlib,Version=2.0.0.0,Culture=neutral,PublicKeyToken=b77a5c561934e089";

        private KeyCollection<TKey, TValue> keys;
        private ValueCollection<TKey, TValue> values;

        public abstract int Count { get; }
        public abstract void Clear();
        public abstract void Add(TKey key, TValue value);
        public abstract bool ContainsKey(TKey key);
        public abstract bool Remove(TKey key);
        public abstract bool TryGetValue(TKey key, out TValue value);
        public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();
        protected abstract void SetValue(TKey key, TValue value);

        public bool IsReadOnly
        {
            get { return false; }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                if (this.keys == null)
                    this.keys = new KeyCollection<TKey, TValue>(this);

                return this.keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                if (this.values == null)
                    this.values = new ValueCollection<TKey, TValue>(this);

                return this.values;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!this.TryGetValue(key, out value))
                    throw new KeyNotFoundException();

                return value;
            }
            set
            {
                SetValue(key, value);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            TValue value;
            if (!this.TryGetValue(item.Key, out value))
                return false;

            return EqualityComparer<TValue>.Default.Equals(value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Copy(this, array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!this.Contains(item))
                return false;

            return this.Remove(item.Key);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException("arrayIndex");

            if ((array.Length - arrayIndex) < source.Count)
                throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");

            foreach (T item in source)
                array[arrayIndex++] = item;
        }
    }
}
