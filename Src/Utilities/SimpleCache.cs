using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;

namespace Loyc.Utilities
{
	/// <summary>A cache designed to save memory by sharing instances of identical 
	/// strings and other immutable objects.</summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks>
	/// SimpleCache is not thread safe. However, G.Cache() is.
	/// <para/>
	/// SimpleCache is used simply by calling Cache(). For example, if C is a 
	/// SimpleCache(of string), then C.Cache("Hello") adds the string "Hello" to 
	/// the cache if it is not present already, or returns the existing string if 
	/// it is.
	/// <para/>
	/// I'll describe SimpleCache as a two-way set associative hash cache. An object 
	/// O with some hashcode X is always located at one of two locations in the 
	/// cache: X%S or (X+1)%S, where S is the size of the hashtable. If C.Cache(O) 
	/// is called and O is not in the cache, O is always placed at position X%S. 
	/// If another object P with hash Y is added, and Y%S == X%S, then O is moved 
	/// to position (X+1)%S so that P can take the position. Thus, an object is only
	/// located in the "plus one" position if it was less recently used, and the 
	/// cache will choose to discard that object when necessary.
	/// <para/>
	/// The cache size doubles when the number of objects discarded (replacements) 
	/// reaches the cache size, provided that the cache is at least 50% used. The 
	/// initial size is normally 32, and the maximum size can be specified in the 
	/// constructor (default: 1024). Note that regardless of cache size, it is 
	/// impossible for three objects to be in the cache at the same time if they
	/// all share the same hash code. But on the plus side, if you alternate between 
	/// calling Cache(A) and Cache(B), for any A and B, you are guaranteed to get 
	/// only cache hits in steady-state. (To prove this, by the way, one must 
	/// consider not only when A and B have the same hash code, but when 
	/// A.GetHashCode() == B.GetHashCode() + 1).
	/// <para/>
	/// The algorithm is pretty simple--Cache() has no loops--so it should be quite 
	/// fast as well.
	/// <para/>
	/// TODO: try supporting hashtables with non-power-of-2 sizes for possible speedup.
	/// </remarks>
	public class SimpleCache<T> where T : class
	{
		protected IEqualityComparer<T> _comparer;
		protected int _mask;
		protected int _maxSize;
		protected T[] _table;
		protected int _replacementCount;
		protected int _inUse;
		protected int _cacheHits, _cacheMisses; // Statistics

		public SimpleCache() : this(1024, null) { }
		public SimpleCache(int maxSize) : this(maxSize, null) { }
		public SimpleCache(int maxSize, IEqualityComparer<T> comparer) 
		{
			_maxSize = maxSize;
			_comparer = (comparer == null ? EqualityComparer<T>.Default : comparer);
			Clear();
		}
		public T Cache(T obj)
		{
			if (obj == null)
				return null;

			if (_replacementCount >= _table.Length
				&& _inUse > (_table.Length >> 1)
				&& _table.Length <= (_maxSize >> 1))
				Enlarge();

			int hash = obj.GetHashCode();
			int index1 = hash & _mask;
			if (_table[index1] == null) {
				_table[index1] = obj;
				_inUse++;
				_cacheMisses++;
			} else if (_comparer.Equals(obj, _table[index1])) {
				_cacheHits++;
				return _table[index1];
			} else {
				int index2 = (index1 + 1) & _mask;
				if (_table[index2] != null && _comparer.Equals(obj, _table[index2]))
				{
					if ((_table[index1].GetHashCode() & _mask) == index1)
						G.Swap<T>(ref _table[index1], ref _table[index2]);
					else {
						_table[index1] = _table[index2];
						_table[index2] = null;
						
						// Handle a rare case that can allow a duplicate cache 
						// entry later on, eventually leading to an Assert 
						// (though having a duplicate is probably harmless)
						for (;;) {
							int index3 = (index2 + 1) & _mask;
							if (_table[index3] != null 
								&& (_table[index3].GetHashCode() & _mask) == index2) {
								_table[index2] = _table[index3];
								_table[index3] = null;
								index2 = index3;
								continue;
							}
							break;
						}
						
						_inUse--;
						_replacementCount++;
					}
					_cacheHits++;
					return _table[index1];
				}
				else
				{
					if ((_table[index1].GetHashCode() & _mask) == index1) {
						if (_table[index2] != null)
							_replacementCount++;
						else
							_inUse++;
						_table[index2] = _table[index1];
					} else
						_replacementCount++;
					_table[index1] = obj;
					_cacheMisses++;
				}
			}
			return obj;
		}
		protected void Enlarge()
		{
			DebugCheck();
			T[] oldTable = _table;
			int oldInUse = _inUse;
			
			// Re-initialize variables to start with a "clean slate"
			int oldHits = _cacheHits, oldMisses = _cacheMisses;
			_table = new T[oldTable.Length * 2];
			_mask = _table.Length - 1;
			_replacementCount = 0;
			_inUse = 0;
			for (int i = oldTable.Length - 1; i >= 0; i--)
				Cache(oldTable[i]);
			// replacement is rare, but not impossible
			//Debug.Assert(_replacementCount == 0);
			Debug.Assert(oldInUse == _inUse + _replacementCount);
			Debug.Assert(_cacheHits == oldHits);
			Debug.Assert(_cacheMisses == oldMisses + oldInUse);
			_cacheMisses -= oldInUse;
		}
		public void Clear()
		{
			_table = new T[_maxSize < 32 ? (_maxSize < 16 ? 8 : 16) : 32];
			_mask = _table.Length - 1;
			_inUse = _replacementCount = 0;
			_cacheHits = _cacheMisses = 0;
		}
		public void DebugCheck()
		{
			Debug.Assert(_mask == _table.Length - 1);

			int inUse = 0;
			for (int i = 0; i < _table.Length; i++)
				if (_table[i] != null)
				{
					inUse++;
					int hash = _table[i].GetHashCode();
					Debug.Assert((hash & _mask) == i || ((hash + 1) & _mask) == i);
				}
			Debug.Assert(_inUse == inUse);
		}

		public int CacheHits { get { return _cacheHits; } }
		public int CacheMisses { get { return _cacheMisses; } }
	}

	[TestFixture]
	public class SimpleCacheTests
	{
		[Test] public void TestRandom()
		{
			SimpleCache<string> c = new SimpleCache<string>();
			StringBuilder sb = new StringBuilder("12345678");
			int seed = Environment.TickCount;
			Random r = new Random(seed);
			List<string> words = new List<string>();
			int cacheHits = 0;

			for (int i = 0; i < 1000; i++)
			{
				for (int ci = 0; ci < 8; ci++)
					sb[ci] = (char)r.Next(0x7F);
				words.Add(sb.ToString());

				Try(words[r.Next(words.Count)], c, ref cacheHits);
				Try(words[r.Next(words.Count)], c, ref cacheHits);
				Try(words[r.Next(words.Count)], c, ref cacheHits);
			}
			Assert.GreaterOrEqual(c.CacheHits, 500);
			Assert.GreaterOrEqual(c.CacheMisses, 500);
		}

		private void Try(string word, SimpleCache<string> c, ref int cacheHits)
		{
			string cacheWord = c.Cache(word);
			Assert.AreEqual(word, cacheWord);
		}
	}
}
