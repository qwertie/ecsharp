using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using Loyc.Collections;
using Loyc.Collections.MutableListExtensionMethods;
using Loyc.Math;
using Loyc;

namespace Benchmark
{
	class BenchmarkSetsBase<T>
	{
		protected const int ItemQuota = 5000000;

		protected class Result
		{
			public string Descr;
			public int DataSize;
			public int HTime, MTime, ITime;
			public long HSetMemory, MSetMemory;
			public override string ToString()
			{
				string msg = string.Format("{0,-33},{1,4},{2,4},{3,4}",
					string.Format("{0} ({1})", Descr, DataSize == -1 ? "avg" : (object)DataSize),
					HTime, MTime, ITime);
				if (HSetMemory != 0 && MSetMemory != 0) {
					msg += string.Format(",{0}%", Math.Round((double)MSetMemory / (double)HSetMemory * 100.0));
					Trace.WriteLine(msg);
					Trace.WriteLine(string.Format("HSetMemory={0}, MSetMemory={1}", HSetMemory, MSetMemory));
				}
				return msg;
			}
		}

		protected void DoForVariousSizes(string description, int maxSize, Action<int> testCode)
		{
			int iterations = 0;
			for (int size = 5; size <= maxSize; size *= 2) {
				DoForSize(description, testCode, size);
				iterations++;
				//DoForSize(description, testCode, size);
				//iterations++;
			}

			var combined = new Result { Descr = description, DataSize = -1 };
			for (int i = 0; i < iterations; i++) {
				var cur = _results[_results.Count - iterations + i];
				combined.HTime += cur.HTime;
				combined.MTime += cur.MTime;
				combined.ITime += cur.ITime;
			}
			combined.HTime /= iterations;
			combined.MTime /= iterations;
			combined.ITime /= iterations;
			_results.Add(combined);
			Console.WriteLine(combined.ToString());
		}

		protected void DoForSize(string description, Action<int> testCode, int size)
		{
			GC.Collect();
			BeginTest();
			testCode(size);
			SaveResults(description, size);
		}
		protected void SaveResults(string description, int size)
		{
			_results.Add(new Result { 
				Descr = description, DataSize = size, 
				HTime = _hTime, MTime = _mTime, ITime = _iTime,
				HSetMemory = _hSetMemory, MSetMemory = _mSetMemory
			});
			Console.WriteLine(_results[_results.Count - 1].ToString());
		}

		protected List<Result> _results = new List<Result>();

		protected Random _r = new Random();
		protected EzStopwatch _timer = new EzStopwatch();
		protected T[] _data;

		protected int _hTime, _mTime, _iTime;
		protected long _mSetMemory, _hSetMemory;

		protected void BeginTest()
		{
			_hTime = 0;
			_mTime = 0;
			_iTime = 0;
			_mSetMemory = 0;
			_hSetMemory = 0;
		}

		protected void DoTimes(int times, Action action)
		{
			for (int i = 0; i < times; i++)
				action();
		}
	}

	class BenchmarkSets<T> : BenchmarkSetsBase<T>
	{
		public void Run(T[] data)
		{
			_data = data;
			// Scenarios...
			// - enumerator
			// - membership tests (random), membership sets (absent), membership tests (present)
			// - add tests (random), add tests (absent), add tests (present)
			// - remove tests (random), remove tests (absent), remove tests (present)
			// - union tests (random), intersections (random), subtraction (random)
			int size100 = _data.Length;
			int size50 = _data.Length * 2 / 3;
			int size0 = _data.Length / 2;
			Console.WriteLine("*** BenchmarkSets<{0}> ***", typeof(T).Name);
			Console.WriteLine("Add items,                    ,HashSet,MSet, Set,M/H% memory");
			DoForVariousSizes("Add items, all new",          size100, size => DoAddTests(size, 0));
			DoForVariousSizes("Add items, half new",         size50,  size => DoAddTests(size, size/2));
			DoForVariousSizes("Add items, none new",         size0,   size => DoAddTests(size, size));
			Console.WriteLine("Remove items,                 ,HashSet,MSet, Set,M/H% memory");
			DoForVariousSizes("Remove items, all found",     size100, size => DoRemoveTests(size, 0));
			DoForVariousSizes("Remove items, half found",    size50,  size => DoRemoveTests(size, size/2));
			DoForVariousSizes("Remove items, none found",    size0,   size => DoRemoveTests(size, size));
			Console.WriteLine("Union,                        ,HashSet,MSet, Set");
			DoForVariousSizes("Union, full overlap",         size100, size => DoSetOperationTests(size, 0, Op.Or));
			DoForVariousSizes("Union, half overlap",         size50,  size => DoSetOperationTests(size, size/2, Op.Or));
			DoForVariousSizes("Union, no overlap",           size0,   size => DoSetOperationTests(size, size, Op.Or));
			Console.WriteLine("Intersect,                    ,HashSet,MSet, Set");
			DoForVariousSizes("Intersect, full overlap",     size100, size => DoSetOperationTests(size, 0, Op.And));
			DoForVariousSizes("Intersect, half overlap",     size50,  size => DoSetOperationTests(size, size/2, Op.And));
			DoForVariousSizes("Intersect, no overlap",       size0,   size => DoSetOperationTests(size, size, Op.And));
			Console.WriteLine("Subtract and Xor              ,HashSet,MSet, Set");
			DoForVariousSizes("Subtract, no overlap",        size0,  size => DoSetOperationTests(size, size, Op.Sub));
			DoForVariousSizes("Subtract, half overlap",      size50,  size => DoSetOperationTests(size, size/2, Op.Sub));
			DoForVariousSizes("Xor, half overlap",           size50,  size => DoSetOperationTests(size, size/2, Op.Xor));
			Console.WriteLine("Enumeration,                  ,HashSet,MSet, Set");
			DoForVariousSizes("Enumeration,",                size100, size => DoEnumeratorTests(size));
			Console.WriteLine("Membership,                   ,HashSet,MSet, Set");
			DoForVariousSizes("Membership, all found", size100, size => DoMembershipTests(size, 0));
			DoForVariousSizes("Membership, half found",      size50,  size => DoMembershipTests(size, size/2));
			DoForVariousSizes("Membership, none found",      size0,   size => DoMembershipTests(size, size));
		}

		HashSet<T> _hSet;
		MSet<T> _mSet;
		Set<T> _iSet;

		void SetData(IListSource<T> data)
		{
			_hSet = new HashSet<T>(data);
			_mSet = new MSet<T>(data);
			_iSet = new Set<T>(data);
		}

		void DoEnumeratorTests(int size)
		{
			BeginTest();
			int i = 0;
			for (int counter = 0; counter < ItemQuota; counter += 10*size) {
				i = (i + 2) % (_data.Length - size);
				SetData(_data.Slice(i, size));
				DoTimes(10, TrialEnumerateSets);
			}
		}
		void TrialEnumerateSets()
		{
			int count = 0;
			_timer.Restart();
			foreach (T item in _hSet)
				count++;
			_hTime += _timer.Restart();
			foreach (T item in _mSet)
				count++;
			_mTime += _timer.Restart();
			foreach (T item in _iSet)
				count++;
			_iTime += _timer.Restart();
			Debug.Assert(count == _hSet.Count * 3);
		}

		protected static int SizeOfT = typeof(T).IsValueType ? (typeof(T) == typeof(int) ? 4 : -1) : IntPtr.Size;
		void TallyMemory()
		{
			_hSetMemory += CountMemory(_hSet, SizeOfT);
			_mSetMemory += _mSet.CountMemory(SizeOfT);
		}

		static FieldInfo HashSet_buckets;
		static long CountMemory(HashSet<T> set, int sizeOfT)
		{
			int bytes = IntPtr.Size * 6 + 4 * 4; // size of HashSet<T> itself
			
			if (HashSet_buckets == null)
				HashSet_buckets = set.GetType().GetField("m_buckets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

			int arrayLength = 0;
			if (HashSet_buckets != null) {
				int[] buckets = (int[])HashSet_buckets.GetValue(set);
				if (buckets != null)
					arrayLength = buckets.Length;
			} else
				arrayLength = MathEx.NextPowerOf2(set.Count) - 1; // guess array length
			if (arrayLength != 0) {
				bytes += IntPtr.Size * 6; // overhead of the two arrays in HashSet
				bytes += arrayLength * (sizeOfT + 12); // size of the buckets, cached hashcodes, "next" indexes and T data
			}

			return bytes;
		}

		void DoMembershipTests(int size, int phase)
		{
			Debug.Assert(phase <= size && size + phase <= _data.Length);
			BeginTest();
			int i0 = 0, i1;
			for (int counter = 0; counter < ItemQuota; counter += 10 * size) {
				i0 = (i0 + 2) % (_data.Length - (size + phase));
				i1 = i0 + phase;
				SetData(_data.Slice(i0, size));
				DoTimes(10, () => TrialMembershipTests(_data, i1, i1 + size));
			}
		}
		void TrialMembershipTests(T[] data, int start, int stop)
		{
			int countH = TrialMembershipTests(data, start, stop, ref _hTime, _hSet);
			int countO = TrialMembershipTests(data, start, stop, ref _mTime, _mSet);
			int countI = TrialMembershipTests(data, start, stop, ref _iTime, _iSet);
			Debug.Assert(countH == countO);
			Debug.Assert(countH == countI);
		}
		private int TrialMembershipTests(T[] data, int start, int stop, ref int time, ICollection<T> set)
		{
			_timer.Restart();
			int count = 0;
			for (int i = start; i < stop; i++) {
				if (set.Contains(data[i]))
					count++;
			}
			time += _timer.Restart();
			return count;
		}

		void DoAddTests(int size, int prepopulation)
		{
			BeginTest();
			Debug.Assert(prepopulation <= size);
			// We'll put some of the data in the set already, before we start 
			// adding anything. This allows us to see how it affects performance 
			// when some or all of the data is already present in the set.
			
			int i = 0;
			for (int counter = 0; counter < ItemQuota; counter += size) {
				i = (i + 2) % (_data.Length - size);
				SetData(_data.Slice(i, prepopulation));
				TrialAdds(_data, i, i + size, true);
			}
		}
		void TrialAdds(T[] data, int start, int stop, bool randomOrder)
		{
			var indexes = GetIndexes(start, stop, randomOrder);
			int hCount = 0, oCount = 0, oldICount = _iSet.Count;
				
			_timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				if (_hSet.Add(data[indexes[i]]))
					hCount++;
			_hTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				if (_mSet.Add(data[indexes[i]]))
					oCount++;
			_mTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				_iSet = _iSet + data[indexes[i]];
			_iTime += _timer.Restart();

			Debug.Assert(hCount == oCount);
			Debug.Assert(hCount == _iSet.Count - oldICount);
			TallyMemory();
		}

		List<int> _indexes = new List<int>();
		List<int> GetIndexes(int start, int stop, bool randomOrder)
		{
			_indexes.Resize(stop - start);
			for (int i = start; i < stop; i++)
				_indexes[i - start] = i;
			if (randomOrder)
				_indexes.Randomize();
			return _indexes;
		}

		void DoRemoveTests(int size, int phase)
		{
			BeginTest();
			Debug.Assert(phase <= size && size + phase <= _data.Length);
			// In these tests, the number of items that we attempt to remove
			// is always the same as the number of items that are in the sets
			// at the beginning of the process. However, depending on 'phase',
			// some of the items that we attempt to remove will not be in the
			// set.
			int i0 = 0, i1;
			for (int counter = 0; counter < ItemQuota; counter += size) {
				i0 = (i0 + 2) % (_data.Length - (size + phase));
				i1 = i0 + phase;
				SetData(_data.Slice(i0, size));
				TrialRemoves(_data, i1, i1 + size, true);
			}
		}
		void TrialRemoves(T[] data, int start, int stop, bool randomOrder)
		{
			var indexes = GetIndexes(start, stop, randomOrder);
			int hCount = 0, oCount = 0, oldICount = _iSet.Count;

			_timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				if (_hSet.Remove(data[indexes[i]]))
					hCount++;
			_hTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				if (_mSet.Remove(data[indexes[i]]))
					oCount++;
			_mTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				_iSet = _iSet - data[indexes[i]];
			_iTime += _timer.Restart();

			Debug.Assert(hCount == oCount);
			Debug.Assert(hCount == oldICount - _iSet.Count);
			TallyMemory();
		}

		enum Op { And, Or, Sub, Xor };

		void DoSetOperationTests(int size, int phase, Op op)
		{
			BeginTest();
			Debug.Assert(phase <= size && size + phase <= _data.Length);
			// In these tests, the number of items that we attempt to remove
			// is always the same as the number of items that are in the sets
			// at the beginning of the process. However, depending on 'phase',
			// some of the items that we attempt to remove will not be in the
			// set.
			int i0 = 0, i1;
			for (int counter = 0; counter < ItemQuota; counter += size) {
				i0 = (i0 + 2) % (_data.Length - (size + phase));
				i1 = i0 + phase;
				TrialSetOperation(_data.Slice(i0, size), _data.Slice(i1, size), op);
			}
		}
		void TrialSetOperation(IListSource<T> data1, IListSource<T> data2, Op op)
		{
			var hSet1 = new HashSet<T>(data1);
			var hSet2 = new HashSet<T>(data2);
			var oSet1 = new MSet<T>(data1);
			var oSet2 = new MSet<T>(data2);
			var iSet1 = new Set<T>(data1);
			var iSet2 = new Set<T>(data2);
			_timer.Restart();
			// HashSet lacks non-mutating operators, so clone it explicitly
			var hSet = new HashSet<T>(hSet1);
			switch(op) {
				case Op.Or:  hSet.UnionWith(hSet2); break;
				case Op.And: hSet.IntersectWith(hSet2); break;
				case Op.Sub: hSet.ExceptWith(hSet2); break;
				case Op.Xor: hSet.SymmetricExceptWith(hSet2); break;
			}
			_hTime += _timer.Restart();
			MSet<T> oSet;
			switch(op) {
				case Op.Or:  oSet = oSet1 | oSet2; break;
				case Op.And: oSet = oSet1 & oSet2; break;
				case Op.Sub: oSet = oSet1 - oSet2; break;
				case Op.Xor: oSet = oSet1 ^ oSet2; break;
			}
			_mTime += _timer.Restart();
			Set<T> iSet;
			switch(op) {
				case Op.Or:  iSet = iSet1 | iSet2; break;
				case Op.And: iSet = iSet1 & iSet2; break;
				case Op.Sub: iSet = iSet1 - iSet2; break;
				case Op.Xor: iSet = iSet1 ^ iSet2; break;
			}
			_iTime += _timer.Restart();
		}
	}

	class BenchmarkMaps<T> : BenchmarkSetsBase<T>
	{
		public void Run(T[] data)
		{
			_data = data;
			// Scenarios...
			// - membership tests (random), membership sets (absent), membership tests (present)
			// - add tests (random), add tests (absent), add tests (present)
			// - remove tests (random), remove tests (absent), remove tests (present)
			// - union tests (random), intersections (random), subtraction (random)
			int size100 = _data.Length;
			int size50 = _data.Length * 2 / 3;
			int size0 = _data.Length / 2;
			Console.WriteLine("*** BenchmarkMaps<{0}> ***", typeof(T).Name);
			Console.WriteLine("Add items,                       ,Dict,MMap, Map,M/H% memory");
			DoForVariousSizes("Add items, all new",          size100, size => DoAddTests(size, 0));
			DoForVariousSizes("Add items, half new",         size50,  size => DoAddTests(size, size/2));
			DoForVariousSizes("Add items, none new",         size0,   size => DoAddTests(size, size));
			Console.WriteLine("Remove items,                    ,Dict,MMap, Map,M/H% memory");
			DoForVariousSizes("Remove items, all found",     size100, size => DoRemoveTests(size, 0));
			DoForVariousSizes("Remove items, half found",    size50,  size => DoRemoveTests(size, size/2));
			DoForVariousSizes("Remove items, none found",    size0,   size => DoRemoveTests(size, size));
			Console.WriteLine("Enumeration,                     ,Dict,MMap, Map");
			DoForVariousSizes("Enumeration,",                size100, size => DoEnumeratorTests(size));
			Console.WriteLine("Membership,                      ,Dict,MMap, Map");
			DoForVariousSizes("Membership, all found",       size100, size => DoMembershipTests(size, 0));
			DoForVariousSizes("Membership, half found",      size50,  size => DoMembershipTests(size, size/2));
			DoForVariousSizes("Membership, none found",      size0,   size => DoMembershipTests(size, size));
		}

		Dictionary<T,T> _hSet;
		MMap<T,T> _mSet;
		Map<T,T> _iSet;

		static KeyValuePair<T, T> P(T t) { return new KeyValuePair<T, T>(t, t); }

		void SetData(IListSource<T> data)
		{
			_hSet = new Dictionary<T,T>(data.Count);
			_mSet = new MMap<T,T>();
			_iSet = new Map<T,T>(data.Select(P));
			foreach (T item in data) {
				_hSet.Add(item, item);
				_mSet.Add(item, item);
			}
		}

		void DoEnumeratorTests(int size)
		{
			BeginTest();
			int i = 0;
			for (int counter = 0; counter < ItemQuota; counter += 10*size) {
				i = (i + 2) % (_data.Length - size);
				SetData(_data.Slice(i, size));
				DoTimes(10, TrialEnumerateSets);
			}
		}
		void TrialEnumerateSets()
		{
			int count = 0;
			_timer.Restart();
			foreach (var item in _hSet)
				count++;
			_hTime += _timer.Restart();
			foreach (var item in _mSet)
				count++;
			_mTime += _timer.Restart();
			foreach (var item in _iSet)
				count++;
			_iTime += _timer.Restart();
			Debug.Assert(count == _hSet.Count * 3);
		}

		protected static int SizeOfPair = typeof(T).IsValueType ? (typeof(T) == typeof(int) ? 8 : -1) : IntPtr.Size*2;
		void TallyMemory()
		{
			_hSetMemory += CountMemory(_hSet, SizeOfPair);
			_mSetMemory += _mSet.CountMemory(SizeOfPair);
		}
		static FieldInfo Dict_buckets;
		static long CountMemory(Dictionary<T,T> set, int sizeOfPair)
		{
			// size of Dictionary<T,T> itself (buckets, entries, KeyCollection, ValueCollection, SyncRoot, SerializationInfo, Comparer, count, version, freeList, freeCount)
			int bytes = IntPtr.Size * 7 + 4 * 4; 
			
			if (Dict_buckets == null)
				Dict_buckets = set.GetType().GetField("buckets", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

			int arrayLength = 0;
			if (Dict_buckets != null) {
				int[] buckets = (int[])Dict_buckets.GetValue(set);
				if (buckets != null)
					arrayLength = buckets.Length;
			} else
				arrayLength = MathEx.NextPowerOf2(set.Count) - 1; // guess array length
			if (arrayLength != 0) {
				bytes += IntPtr.Size * 6; // overhead of the two arrays in Dictionary
				bytes += arrayLength * (sizeOfPair + 12); // size of the buckets, cached hashcodes, "next" indexes and pairs
			}

			return bytes;
		}

		void DoMembershipTests(int size, int phase)
		{
			Debug.Assert(phase <= size && size + phase <= _data.Length);
			BeginTest();
			int i0 = 0, i1;
			for (int counter = 0; counter < ItemQuota; counter += 10 * size) {
				i0 = (i0 + 2) % (_data.Length - (size + phase));
				i1 = i0 + phase;
				SetData(_data.Slice(i0, size));
				DoTimes(10, () => TrialMembershipTests(_data, i1, i1 + size));
			}
		}
		void TrialMembershipTests(T[] data, int start, int stop)
		{
			int countH = TrialMembershipTests(data, start, stop, ref _hTime, _hSet);
			int countO = TrialMembershipTests(data, start, stop, ref _mTime, _mSet);
			int countI = TrialMembershipTests(data, start, stop, ref _iTime, _iSet);
			Debug.Assert(countH == countO);
			Debug.Assert(countH == countI);
		}
		private int TrialMembershipTests(T[] data, int start, int stop, ref int time, IDictionary<T,T> set)
		{
			_timer.Restart();
			int count = 0;
			for (int i = start; i < stop; i++) {
				if (set.ContainsKey(data[i]))
					count++;
			}
			time += _timer.Restart();
			return count;
		}

		void DoAddTests(int size, int prepopulation)
		{
			BeginTest();
			Debug.Assert(prepopulation <= size);
			// We'll put some of the data in the set already, before we start 
			// adding anything. This allows us to see how it affects performance 
			// when some or all of the data is already present in the set.
			
			int i = 0;
			for (int counter = 0; counter < ItemQuota; counter += size) {
				i = (i + 2) % (_data.Length - size);
				SetData(_data.Slice(i, prepopulation));
				TrialAdds(_data, i, i + size, true);
			}
		}
		void TrialAdds(T[] data, int start, int stop, bool randomOrder)
		{
			var indexes = GetIndexes(start, stop, randomOrder);
				
			_timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				_hSet[data[indexes[i]]] = default(T);
			_hTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				_mSet[data[indexes[i]]] = default(T);
			_mTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				_iSet = _iSet.With(data[indexes[i]], default(T));
			_iTime += _timer.Restart();

			TallyMemory();
		}

		List<int> _indexes = new List<int>();
		List<int> GetIndexes(int start, int stop, bool randomOrder)
		{
			_indexes.Resize(stop - start);
			for (int i = start; i < stop; i++)
				_indexes[i - start] = i;
			if (randomOrder)
				_indexes.Randomize();
			return _indexes;
		}

		void DoRemoveTests(int size, int phase)
		{
			BeginTest();
			Debug.Assert(phase <= size && size + phase <= _data.Length);
			// In these tests, the number of items that we attempt to remove
			// is always the same as the number of items that are in the sets
			// at the beginning of the process. However, depending on 'phase',
			// some of the items that we attempt to remove will not be in the
			// set.
			int i0 = 0, i1;
			for (int counter = 0; counter < ItemQuota; counter += size) {
				i0 = (i0 + 2) % (_data.Length - (size + phase));
				i1 = i0 + phase;
				SetData(_data.Slice(i0, size));
				TrialRemoves(_data, i1, i1 + size, true);
			}
		}
		void TrialRemoves(T[] data, int start, int stop, bool randomOrder)
		{
			var indexes = GetIndexes(start, stop, randomOrder);
			int hCount = 0, oCount = 0, oldICount = _iSet.Count;

			_timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				if (_hSet.Remove(data[indexes[i]]))
					hCount++;
			_hTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				if (_mSet.Remove(data[indexes[i]]))
					oCount++;
			_mTime += _timer.Restart();
			for (int i = 0; i < indexes.Count; i++)
				_iSet = _iSet.Without(data[indexes[i]]);
			_iTime += _timer.Restart();

			Debug.Assert(hCount == oCount);
			Debug.Assert(hCount == oldICount - _iSet.Count);
			TallyMemory();
		}
	}
}
