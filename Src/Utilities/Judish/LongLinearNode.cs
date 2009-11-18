using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish.Internal
{
	/// <summary>
	/// Stores rows in a sorted list; some of the rows are left unused in order to
	/// improve performance of inserts and deletes.
	/// </summary>
	sealed internal class LongLinearNode : LinearNode
	{
		public LongLinearNode()
		{
			_r = InternalList<JRow>.EmptyArray;
			_lkf = null;
		}
		
		private LongLinearNode(LongLinearNode copy)
		{
			_r = (JRow[])copy._r.Clone();
			_lkf = (int[])copy._lkf.Clone();
			for (int i = 0; i < _r.Length; i++)
			{
				var child = _r[i].V as NodeBase;
				if (child != null)
					_r[i].V = child.Clone();
			}
		}

		private LongLinearNode(KeyPrefix k1, object v1, KeyPrefix k2, object v2)
		{
		}

		public int MinAlloc = 6; // minimum number of rows to allocate

		public LongLinearNode(NodeBase copy)
		{
			JKeyPart part;
			int copyCount = copy.LocalCount;
			_stat.NumKeys = copyCount;
			
			// Mark 25% of the entries unused (1 of every 4).
			int countPlus = copyCount + (copyCount / 3);
			_r = new JRow[Math.Max(countPlus, MinAlloc)];
			
			int counter = 0;
			object cur = copy.MoveFirst(out part);
			Debug.Assert(cur != NotFound);
			do {
				_r[counter] = new JRow(part.KeyPartSHL, cur);
				SetLongKeyFlag(counter, part.KeyPartSize > 3 ? 1 : 0);
				if (part.KeyPartSize <= 3)
					_r[counter].K |= part.KeyPartSize;

				if ((++counter & 3) == 0)
					// Add a free entry after every 3
					AddFree(counter++);

				cur = copy.MoveNext(ref part);
			} while (cur != NotFound);

			Debug.Assert(counter == countPlus);
			while (counter < _r.Length)
				AddFree(counter++);
		}
		private void AddFree(int counter)
		{
			_r[counter].K = _r[counter - 1].K;
			SetLongKeyFlag(counter, LongKeyFlag(counter - 1));
			_r[counter].V = JRow.Free;
		}

		int[] _lkf; // null if there are no keys shorter than 4B
		/// <summary>These rows are sorted by Decode(index).</summary>
		JRow[] _r;
		StatHeader _stat;

		internal int BinarySearch(KeyPrefix k)
		{
			Debug.Assert(k.kLength <= 4);

			int low = 0;
			int high = _r.Length - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				KeyPrefix midk = Decode(mid);
				if (midk.IsLessThan(k))
					low = mid + 1;
				else if (k.IsLessThan(midk))
					high = mid - 1;
				else {
					while (_r[mid].IsFree)
						mid--;
					return mid;
				} 
			}
			while (_r[low].IsFree)
				low--;
			return ~low;
		}

		struct KeyPrefix
		{
			public KeyPrefix(uint k, int kLength)
				{ this.k = k; this.kLength = (byte)kLength; }
			
			public uint k;
			public int kLength;
			
			public bool IsLessThan(KeyPrefix other)
			{
				return k <= other.k && (k < other.k || kLength < other.kLength);
			}
		}
		private KeyPrefix Decode(int i)
		{
			uint k = _r[i].K;
			if (_lkf == null || LongKeyFlag2(i) != 0)
				return new KeyPrefix(k, 4);
			else
				return new KeyPrefix(k & ~3u, (int)k & 3);
		}
		void Encode(int i, KeyPrefix k)
		{
			if (k.kLength > 3) {
				SetLongKeyFlag(i, 1);
				_r[i].K = k.k;
			}
			else {
				Debug.Assert((byte)k.k == 0);
				SetLongKeyFlag(i, 0);
				_r[i].K = k.k | (uint)k.kLength;
			}
		}

		private int LongKeyFlag(int i)
		{
			if (_lkf == null)
				return 1;
			return LongKeyFlag2(i);
		}
		private int LongKeyFlag2(int i)
		{
			return (_lkf[i >> 5] >> (i & 0x1F)) & 1;
		}
		private void SetLongKeyFlag(int i, int flag)
		{
			if (_lkf == null) {
				if (flag == 0)
					CreateLongKeyFlags();
			}
			int bit = 1 << (i & 31);
			if (flag == 0)
				_lkf[i >> 5] &= ~bit;
			else
				_lkf[i >> 5] |= bit;
		}
		private void CreateLongKeyFlags()
		{
			Debug.Assert(_lkf == null);
			_lkf = new int[(_r.Length >> 5) + 1];
			for (int w = 0; w < _lkf.Length; w++)
				_lkf[w] = ~0;
		}

		public override object Get(ref QueryState q)
		{
			KeyPrefix k = new KeyPrefix(q.Key.KeyPart, q.Key.BytesLeft);
			if (k.kLength > 4)
				k.kLength = 4;

			int i;
			if ((i = BinarySearch(k)) < 0)
				return NotFound;

			return FinishGet(ref q, _r[i].V, k.kLength);
		}

		public override object Set(ref QueryState q, object value, out object thisNode)
		{
			KeyPrefix k = new KeyPrefix(q.Key.KeyPart, q.Key.BytesLeft);
			if (k.kLength > 4)
				k.kLength = 4;
			
			int i = BinarySearch(k);
			if (i >= 0) {
				thisNode = this;
				return SetExisting(ref q, ref _r[i].V, value, k.kLength);
			} else {
				Debug.Assert(!_r[~i].IsFree);
				if (_stat.NumKeys >= _r.Length || _stat.DecisionCounter >= _stat.NumKeysMinus2) {
					return AutoMakeSpace().Set(ref q, value, out thisNode);
				} else {
					// Get a free slot in which to put the new value
					i = ~i;
					int freeIndex = FindFree(i);
					if (freeIndex < i) {
						for (i--; freeIndex < i; freeIndex++)
							Copy(freeIndex + 1, freeIndex);
					} else {
						// freeIndex > i
						for (; freeIndex > i; freeIndex--)
							Copy(freeIndex - 1, freeIndex);
					}
					
					// Assign the key and value to slot i
					byte kLength;
					G.Verify(SetNew(ref q, value, out _r[i].K, out kLength, out _r[i].V) == NotFound);
					Debug.Assert(kLength == k.kLength);
					if (k.kLength > 3) {
						SetLongKeyFlag(i, 1);
					} else {
						_r[i].K |= (uint)k.kLength;
						SetLongKeyFlag(i, 0);
					}
					thisNode = this;
					return NotFound;
				}
			}
		}
		private void Copy(int from, int to)
		{
			_r[to] = _r[from];
			if (_lkf != null)
				SetLongKeyFlag(to, LongKeyFlag2(from));
			if (++_stat.DecisionCounter == 0)
				_stat.DecisionCounter = 255;
		}
		private int FindFree(int near_i)
		{
			int lo = near_i - 1;
			int hi = near_i + 1;
			for(;;) {
				if (lo > 0 && _r[lo].IsFree)
					return lo;
				if ((uint)hi < (uint)_r.Length && _r[hi].IsFree)
					return hi;
				lo--;
				hi++;
			}
		}

		/// <summary>Called by Set(), either because the node is full or because
		/// a lot of moves have been needed during previous inserts and there is
		/// a need for more rows or redistributed rows.</summary>
		/// <returns>Either this or a new node, as needed</returns>
		private NodeBase AutoMakeSpace()
		{
			if (_stat.NumKeys >= _r.Length - (_r.Length >> 3))
			{	// Too many keys; we need more rows or a new node type
				return MakeSpace();
			}
			else if (_stat.NumKeys < _r.Length - 2)
			{	// Redistribute the rows so that free space is evenly distributed.
				// 1. Put all the free slots at the beginning
				int j = _r.Length - 1;
				for (int i = _r.Length - 1; i >= 0; i--)
					if (!_r[i].IsFree) {
						if (i != j) Copy(i, j);
						j--;
					}
				j++; 
				// j is the number of free slots and the first used slot's index
				Debug.Assert(j > 0);
				Debug.Assert(j == _r.Length - _stat.NumKeys);

				// 2. Redistribute
				int freeInterval = (_r.Length - 1) / j;
				int freeCounter = 0;
				for (int i = 0; i < _r.Length; i++) {
					if ((uint)j < (uint)_r.Length && freeCounter++ < freeInterval) {
						Copy(j++, i);
					} else {
						Debug.Assert(i > 0);
						Copy(i - 1, i);
						_r[i].V = JRow.Free;
						freeCounter = 0;
					}
				}

				return this;
			} else
				return this;
		}

		private NodeBase MakeSpace()
		{
			// Scan the rows to figure out the best kind of subdivision to do.
			// Look for a prefix (1, 2 or 3 bytes) that is common to as many 
			// rows as possible.
			int minimumRun = 15;
			if (_stat.NumKeys >= 224)
				minimumRun = 2;

			uint prevK = 0;
			int C1 = 0, bestC1 = minimumRun, bestS1 = -1, start1 = 0;
			int C2 = 0, bestC2 = minimumRun, bestS2 = -1, start2 = 0;
			int C3 = 0, bestC3 = minimumRun, bestS3 = -1, start3 = 0;
			int start = 0;

			for (int i = 0; i < _r.Length; i++)
			{
				JRow r = _r[i];
				if (r.IsFree)
					continue;
				
				// Determine the length of this key...
				int kLength = _lkf == null || LongKeyFlag2(i) != 0 ? 4 : (int)(r.K & 3);

				if ((prevK & ~0xFFu) != (r.K & ~0xFFu))
				{
					if (bestC3 <= C3) {
						bestC3 = C3; C3 = 0;
						bestS3 = start3; start3 = i;
					}

					if ((prevK & ~0xFFFFu) != (r.K & ~0xFFFFu))
					{
						if (bestC2 <= C2) {
							bestC2 = C2; C2 = 0;
							bestS2 = start2; start2 = i;
						}

						if ((prevK & 0xFF000000u) != (r.K & 0xFF000000u))
						{
							if (bestC1 <= C1) {
								bestC1 = C1; C1 = 0;
								bestS1 = start1; start1 = i;
							}
						}
					}
				}

				if (kLength > 0) {
					C1++;
					if (kLength > 1) {
						C2++;
						if (kLength > 2)
							C3++;
					}
				}

				prevK = r.K;
			}

			if (bestC3 <= C3) {
				bestC3 = C3;
				bestS3 = start3;
			}
			if (bestC2 <= C2) {
				bestC2 = C2;
				bestS2 = start2;
			}
			if (bestC1 <= C1) {
				bestC1 = C1;
				bestS1 = start1;
			}

			if (bestS1 == 0 && bestS2 == )
			{
			}
		}

		public override object Remove(ref QueryState q, out object thisNode)
		{
			throw new NotImplementedException();
		}

		public override object MoveFirst(JKeyPath path)
		{
			throw new NotImplementedException();
		}

		public override object MoveLast(JKeyPath path)
		{
			throw new NotImplementedException();
		}

		public override object MoveNext(JKeyPath path)
		{
			throw new NotImplementedException();
		}

		public override object MovePrev(JKeyPath path)
		{
			throw new NotImplementedException();
		}

		public override int LocalCount
		{
			get { throw new NotImplementedException(); }
		}

		public override DecisionStats Stats
		{
			get { throw new NotImplementedException(); }
		}

		public override NodeBase Clone()
		{
			throw new NotImplementedException();
		}

		public override void CheckValidity(bool recursive)
		{
			throw new NotImplementedException();
		}
	}
}

/*	Sorting by K makes things abstract lot more difficult. if guess I won't.

			// Watch out for the ordering problem: the source is enumerated in 
			// the true order, but _r is supposed to be in order by K. Do an
			// insertion sort to enforce this desired order.
			InsertionSort();

		private void InsertionSort()
		{
			// This sort is made complicated mainly by the key comparisons, 
			// because the long-key flag acts as the lowest bit of the key, and it 
			// is stored separately from the rest of the key.
			for (int i = 1, j; i < _r.Length; i++) {
				j = i - 1;
				if (_r[j].K >= _r[i].K) {
					JRow ri = _r[i];
					int lkfi = LongKeyFlag(i);
					int lkfj = LongKeyFlag(j);
					if (_r[j].K > ri.K || lkfj > lkfi)
					{
						for (;;)
						{
							_r[j + 1] = _r[j];
							SetLongKeyFlag(j + 1, lkfj);
							if (--j < 0)
								break;
							lkfj = LongKeyFlag(j);
							if (_r[j].K < ri.K || (_r[j].K == ri.K && lkfj < lkfi))
								break;
						}
						_r[j + 1] = ri;
					}
				}
			}
		}

		public bool Find(ref QueryState q, out int i)
		{
			uint k = q.Key.KeyPart;
			int longKeyFlag = 1;
			int bytesLeft = q.Key.BytesLeft;
			if (bytesLeft < 4) {
				longKeyFlag = 0;
				Debug.Assert((byte)k == 0);
				k |= (uint)q.Key.BytesLeft;
			} else
				bytesLeft = 4;

			if ((i = BinarySearch(k, longKeyFlag)) >= 0) {
				q.Key.Advance(bytesLeft);
				return true;
			} else {
				i = ~i;
				return false;
			}
		}

		internal int BinarySearch(uint k, int longKeyFlag)
		{
			int low = 0;
			int high = _r.Length - 1;
			while (low <= high)
			{
				int mid = low + ((high - low) >> 1);
				if (_r[mid].K < k)
					low = mid + 1;
				else if (_r[mid].K > k)
					high = mid - 1;
				else {
					int longKeyMid = LongKeyFlag(mid);
					if (longKeyMid == longKeyFlag) {
						while (_r[mid].IsFree)
							mid--;
						return mid;
					} else if (longKeyMid < longKeyFlag)
						low = mid + 1;
					else
						high = mid - 1;
				} 
			}
			while (_r[low].IsFree)
				low--;
			return ~low;
		}
*/
