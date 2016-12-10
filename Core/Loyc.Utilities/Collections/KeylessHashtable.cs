// Author: David Piepgrass
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Math;
using Loyc.MiniTest;

namespace Loyc.Collections.Impl
{
	/// <summary>
	/// A fairly obscure space-saving hashtable that offers no built-in way to 
	/// store keys, only values. Because there are no keys, the hashtable cannot
	/// be rehashed when it is full, and searching for a given key finds all
	/// values in the same bucket, some of which may be unrelated.
	/// </summary>
	/// <remarks>
	/// My primary primary motivation for this data structure is compactness. It's 
	/// comparable to a "counting bloom filter", in that searching for a key can
	/// find false positives, but not false negatives, but it offers the additional
	/// feature that one or more values can be associated with each key.
	/// <para/>
	/// The size per entry dependends on the size of the hashtable. This data 
	/// structure is the most compact when its size is limited to 65536 entries and 
	/// buckets; its overhead doubles when you exceed this limit, since "shorts" 
	/// become "ints".
	/// <para/>
	/// The Count is allowed to exceed the Capacity, but it is not allowed to cross
	/// a size threshold (255 or 65535). Capacity returns the number of buckets,
	/// so if Count exceeds Capacity, it simply means there are more items than 
	/// buckets, so there will be a larger-than-normal amount of "false positives" 
	/// (multiple items will typically be returned from a search).
	/// <para/>
	/// The size requirement per entry is 2 bytes (plus sizeof(T)) for a table of
	/// size 255 or less, 4 bytes (plus sizeof(T)) for a table of size 65535 or 
	/// less, and 8 bytes (plus sizeof(T)) for larger tables. Prime number sizes
	/// are generally preferred for best performance.
	/// <para/>
	/// The memory for buckets (1-4 bytes) is allocated up-front, but other memory
	/// is allocated on-demand. For example, if you create a new hashtable with 
	/// capacity 251 and add 50 items, 251 bytes are allocated up-front, but less
	/// than 100 * (1+sizeof(T)) additional bytes are allocated.
	/// <para/>
	/// By its very nature, KeylessHashtable allows multiple values to be 
	/// associated with a single key.
	/// <para/>
	/// A normal hashtable could theoretically be built on top of this one by
	/// storing the key and value together in type T.
	/// </remarks>
	[Serializable]
	public abstract class KeylessHashtable<T> : IReadOnlyCollection<T>
	{
		public static KeylessHashtable<T> New(int numBuckets)
		{
			return New(numBuckets, numBuckets);
		}
		public static KeylessHashtable<T> New(int numBuckets, int maxCount)
		{
			if (maxCount < numBuckets)
				maxCount = numBuckets;
			if (maxCount < 256)
				return new KeylessHashtable<T, byte, MathU8>(numBuckets);
			else if (maxCount < 65536)
				return new KeylessHashtable<T, ushort, MathU16>(numBuckets);
			else
				return new KeylessHashtable<T, int, MathI>(numBuckets);
		}

		protected KeylessHashtable()
		{
			_values = EmptyArray<T>.Value;
		}
		
		protected T[] _values;
		protected int _firstUnused = -1; // _values[_firstUnused] is unused (-1 if _values if full)
		protected int _count;            // Number of T used.

		public int Count { get { return _count; } }
		public void Add<K>(K key, T value) { Add((uint)key.GetHashCode(), value); }
		public bool Remove<K>(K key, T value) { return Remove((uint)key.GetHashCode(), value); }
		public IEnumerator<T> Find<K>(K key) { return Find((uint)key.GetHashCode()); }

		public abstract int Capacity { get; }
		public abstract void Add(uint hashCode, T value);
		public abstract bool Remove(uint hashCode, T value);
		public abstract int Remove(uint hashCode, Predicate<T> shouldRemove, int maxToRemove);
		public abstract void Clear();
		public abstract IEnumerator<T> GetEnumerator();
		public abstract IEnumerator<T> Find(uint hashCode);

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// <summary>The concrete implementation of <see cref="KeylessHashtable{T}"/>.
	/// Do not use directly; instead, call <see cref="KeylessHashtable{T}.New"/>.</summary>
	[Serializable]
	public class KeylessHashtable<T, Int, Math> : KeylessHashtable<T> 
		where Int : struct, IConvertible
		where Math : struct, IMath<Int>
	{
		readonly static Math M = new Math();
		readonly static int END = (int)M.MaxInt;
		readonly static Int IntEND = M.From(M.MaxInt);

		Int[] _next = EmptyArray<Int>.Value;
		Int[] _buckets;

		public KeylessHashtable(int numBuckets)
		{
			CheckParam.IsInRange("numBuckets", numBuckets, 1, END);
			
			_buckets = new Int[numBuckets];
			Clear();
		}

		public override int Capacity
		{
			get { return _buckets.Length; }
		}
		protected void AutoRaiseCapacity()
		{
			if (_count < END)
			{
				Debug.Assert((_firstUnused == -1) == (_count == _values.Length));
				if (_firstUnused == -1)
				{
					int newCapacity = InternalList.NextLargerSize(_count, END);
					_values = InternalList.CopyToNewArray(_values, _values.Length, newCapacity);
					_firstUnused = _count;
					
					// Enlarge _next
					int oldCapacity = _next.Length;
					_next = InternalList.CopyToNewArray(_next, _next.Length, newCapacity);
					for (int i = oldCapacity; i < newCapacity; i++)
						_next[i] = M.From(i + 1);
					_next[newCapacity - 1] = IntEND;
				}
				return;
			}
			throw new InvalidOperationException(Localize.Localized("KeylessHashtable is full"));
		}

		public sealed override void Add(uint hashCode, T value)
		{
			if (_firstUnused == -1)
				AutoRaiseCapacity();

			uint ib = hashCode % (uint)_buckets.Length;
			// Assign the next free entry to _buckets[ib], and populate the entry.
			// (the term "entry" refers to _next[p] and _values[p] together.)
			Int oldP = _buckets[ib];
			int newP;
			_buckets[ib] = M.From(newP = _firstUnused);
			_firstUnused = _next[newP].ToInt32(null);
			if (_firstUnused == END)
				_firstUnused = -1;
			_next[newP] = oldP;
			_values[newP] = value;
			_count++;
		}

		public override bool Remove(uint hashCode, T value)
		{
			uint iB = hashCode % (uint)_buckets.Length;
			int iN = _buckets[iB].ToInt32(null);
			int iOldN = -1;

			while (iN != END)
			{
				T curValue = _values[iN];
				if (value == null ? curValue == null : value.Equals(curValue))
				{
					// value found. Remove the entry.
					if (iOldN == -1)
						_buckets[iB] = _next[iN];
					else
						_next[iOldN] = _next[iN];
					_next[iN] = M.From(_firstUnused);
					_firstUnused = iN;
					--_count;
					return true;
				}
				iOldN = iN;
				iN = _next[iN].ToInt32(null);
			}
			return false;
		}
		public sealed override int Remove(uint hashCode, Predicate<T> shouldRemove, int maxToRemove)
		{
			uint iB = hashCode % (uint)_buckets.Length;
			int iN = _buckets[iB].ToInt32(null);
			int iOldN = -1;
			int numRemoved = 0;

			while (iN != END)
			{
				if (shouldRemove(_values[iN]))
				{
					// Remove the entry.
					Int iNextN;
					if (iOldN == -1)
						_buckets[iB] = iNextN = _next[iN];
					else
						_next[iOldN] = iNextN = _next[iN];
					_next[iN] = M.From(_firstUnused);
					_firstUnused = iN;
					--_count;
					if (++numRemoved >= maxToRemove)
						return numRemoved;
					iN = iNextN.ToInt32(null);
					continue;
				}
				iOldN = iN;
				iN = _next[iN].ToInt32(null);
			}
			return numRemoved;
		}

		public sealed override void Clear()
		{
			_count = 0;
			_values = EmptyArray<T>.Value;
			_next = EmptyArray<Int>.Value;
			InternalList.Fill(_buckets, IntEND);
		}

		public override IEnumerator<T> GetEnumerator()
		{
		    uint iB = 0;
		    int iN = _buckets[iB].ToInt32(null);

			for (;;) {
				while (iN == END)
				{
					++iB;
					if (iB >= _buckets.Length)
						yield break;
					iN = _buckets[iB].ToInt32(null);
				}
				T value = _values[iN];
				iN = _next[iN].ToInt32(null);
				yield return value;
			}
		}

		public override IEnumerator<T> Find(uint hashCode)
		{
		    uint iB = hashCode % (uint)_buckets.Length;
		    int iN = _buckets[iB].ToInt32(null);

			while (iN != END) {
				T value = _values[iN];
				iN = _next[iN].ToInt32(null);
				yield return value;
			}
		}
	}

	[TestFixture]
	public class KeylessHashtableTests
	{
		[Test] public void BasicTests1() { BasicTests(1, 1); }
		[Test] public void BasicTests5() { BasicTests(3, 5); }
		[Test] public void BasicTests10() { BasicTests(11, 10); }
		[Test] public void BasicTests100() { BasicTests(101, 100); }
		[Test] public void BasicTests300() { BasicTests(199, 300); }
		[Test] public void BasicTests1000() { BasicTests(997, 1000); }
		[Test] public void BasicTests10000() { BasicTests(65537, 10000); }

		private void BasicTests(int buckets, int maxCount)
		{
			Random r = new Random(buckets);
			var ht = KeylessHashtable<int>.New(buckets, maxCount);
			var ht2 = new Dictionary<int, int>(maxCount);
			int count;

			Assert.AreEqual(buckets, ht.Capacity);

			for (int i = 0; i < maxCount; i++)
			{
				int k = r.Next(maxCount * 2);
				ht.Add(k, k);
				
				ht2.TryGetValue(k, out count);
				ht2[k] = count + 1;
				Assert.AreEqual(i + 1, ht.Count);
			}
			
			// Make sure that we can retrieve all the values that we added through 
			// the Find() method.
			int falsePositives = 0;
			foreach (var kvp in ht2)
			{
				var it = ht.Find(kvp.Key);
				int found = 0;
				while (it.MoveNext())
					if (it.Current == kvp.Key)
						found++;
					else
						falsePositives++;
				Assert.AreEqual(kvp.Value, found);
			}
			
			// TODO: analyze carefully to find a realistic upper bound on false positives
			// The limit here is just a guess.
			Assert.Less(falsePositives, 2 * maxCount * maxCount / buckets);

			// Make sure that we can retrieve all the values that we added through 
			// the main iterator.
			count = 0;
			for (var it = ht.GetEnumerator(); it.MoveNext(); count++)
				Assert.That(ht2.ContainsKey(it.Current));
			Assert.AreEqual(ht.Count, count);
				
			// Delete all the items using both available methods
			foreach (var kvp in ht2)
			{
				if (r.Next(2) == 0)
				{
					// Method 1: remove by key-value pair (removes one at a time)
					for (int i = 0; i < kvp.Value; i++)
						Assert.That(ht.Remove(kvp.Key, kvp.Key));
					Assert.IsFalse(ht.Remove(kvp.Key, kvp.Key));
				}
				else
				{
					// Method 2: remove by predicate with removal limit
					int removed, total = 0;
					do {
						total += (removed = ht.Remove((uint)kvp.Key, v => v == kvp.Key, 2));
						Assert.That(removed >= 0 && removed <= 2);
					} while (removed == 2);
					Assert.AreEqual(kvp.Value, total);
				}
			}

			Assert.AreEqual(0, ht.Count);
		}
	}
}
