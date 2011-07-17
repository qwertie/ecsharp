// Author: David Piepgrass
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Collections;
using Loyc.Collections.Linq;
using Loyc.Math;

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
	/// The size per entry dependents on the size of the hashtable. This data 
	/// structure is the most compact when its size is limited to 65536 entries and 
	/// buckets; its size doubles when you exceed this limit, since "shorts" become
	/// "ints".
	/// <para/>
	/// The Count is allowed to exceed the Capacity, but it is not allowed to cross
	/// a size threshold (255 or 65535). Capacity returns the number of buckets,
	/// so if Count exceeds Capacity, it simply means there are more items than 
	/// buckets, so a larger-than-normal amount of .
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
	/// By its very nature, KeylessHashtable necessarily supports duplicate keys.
	/// <para/>
	/// A normal hashtable could theoretically be built on top of this one by
	/// storing the key and value together in type T.
	/// </remarks>
	public abstract class KeylessHashtable<T> : IIterable<T>
	{
		public KeylessHashtable<T> New(int numBuckets)
		{
			if (numBuckets < 256)
				return new KeylessHashtable<T, byte, MathU8>(numBuckets);
			else if (numBuckets < 65536)
				return new KeylessHashtable<T, ushort, MathU16>(numBuckets);
			else
				return new KeylessHashtable<T, int, MathI>(numBuckets);
		}

		protected KeylessHashtable()
		{
			_values = InternalList<T>.EmptyArray;
		}
		
		protected T[] _values;
		protected int _firstUnused = -1; // _values[_firstUnused] is unused (-1 if _values if full)
		protected int _count;            // Number of T used.

		public int Count { get { return _count; } }
		public void Insert<K>(K key, T value) { Insert(key.GetHashCode(), value); }
		public bool Remove<K>(K key, T value) { return Remove(key.GetHashCode(), value); }

		public abstract int Capacity { get; }
		public abstract void Insert(uint hashCode, T value);
		public abstract bool Remove(uint hashCode, T value);
		public abstract int Remove(uint hashCode, Predicate<T> shouldRemove, int maxToRemove);
		public abstract void Clear();
		public abstract Iterator<T> GetIterator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		public IEnumerator<T> GetEnumerator()
		{
			return GetIterator().AsEnumerator();
		}
	}

	/// <summary>A keyless hashtable with 8-bit buckets and an overhead of 2 bytes 
	/// per item.</summary>
	public class KeylessHashtable<T, Int, Math> : KeylessHashtable<T> 
		where Int : struct, IConvertible
		where Math : struct, IMath<Int>
	{
		readonly static Math M = new Math();
		readonly static int END = (int)M.MaxInt;
		readonly static Int IntEND = M.From(M.MaxInt);

		Int[] _next = InternalList<Int>.EmptyArray;
		Int[] _buckets;

		public KeylessHashtable(int numBuckets)
		{
			CheckParam.Range("numBuckets", numBuckets, 0, END);
			
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
			throw new InvalidOperationException(Localize.From("KeylessHashtable is full"));
		}

		public sealed override void Insert(uint hashCode, T value)
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
			_next[newP] = oldP;
			_values[newP] = value;
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
					if (iOldN == -1)
						_buckets[iB] = _next[iN];
					else
						_next[iOldN] = _next[iN];
					_next[iN] = M.From(_firstUnused);
					_firstUnused = iN;
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
					Int iNextN;
					if (iOldN == -1)
						_buckets[iB] = iNextN = _next[iN];
					else
						_next[iOldN] = iNextN = _next[iN];
					_next[iN] = M.From(_firstUnused);
					_firstUnused = iN;
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
			_values = InternalList<T>.EmptyArray;
			_next = InternalList<Int>.EmptyArray;
			InternalList.Fill(_buckets, IntEND);
		}

		public override Iterator<T> GetIterator()
		{
			return delegate(ref bool ended)
			{
				throw new NotImplementedException();
			};
		}
	}
}
