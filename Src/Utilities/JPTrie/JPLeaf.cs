using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Runtime;
using Loyc.Utilities.JPTrie;

namespace Loyc.Utilities
{
	class JPLeaf<T> : JPNode<T>
	{
		byte[] _cells;
		T[] _values;   // null if no values are associated with any keys
		byte _count;
		byte _partition;
		byte _extraCellsUsed;
		byte _firstFree; // 0 if none free
		
		const int Less = -1;
		const int More = 1;
		const int MinPartition = 4; // because 0-3 are key lengths
		#if DEBUG
		internal const int MaxCells = 128; // for testing
		#else
		internal const int MaxCells = 256;
		#endif
		internal const int MaxCount = 32;
		internal const int BurstSingleKeyMaxLength = 12;

		int Partition
		{
			[DebuggerStepThrough]
			get { return _partition; }
			set {
				Debug.Assert(value >= 4 && value <= CellCount && value <= MaxCount);
				_partition = (byte)value;
			}
		}
		int ExtraCells { get { return (_cells.Length >> 2) - Partition; } }
		int ExtraCellsFree { get { return (_cells.Length >> 2) - Partition - _extraCellsUsed; } }
		int CellCount { [DebuggerStepThrough] get { return _cells.Length >> 2; } }
		int Count { [DebuggerStepThrough] get { return _count; } }
		int ExtraBytesLeft { get { return ExtraCellsFree * 3; } }

		public JPLeaf(ref KeyWalker key, T value, out JPNode<T> self)
		{
			_partition = 4;
			int initialCells = Math.Min(MaxCells, 4 + key.Left / 3);
			_cells = new byte[initialCells << 2];
			if (initialCells > 4)
				FreeNewCells(_cells, 4 << 2);

			self = this;
			Insert(0, ref key, value, ref self);
		}

		T Value(int i) { return _values == null ? default(T) : _values[i]; }
		
		void SetValue(int i, T value)
		{
			if (value != null)
			{
				if (_values == null)
					_values = new T[Math.Max(Count, 3)];
				else if (i >= _values.Length) {
					Debug.Assert(i < CellCount);
					T[] old = _values;
					int newLength = Math.Max(Count, 
						Math.Min(MaxCount, old.Length + (old.Length >> 1) + 1));
					_values = new T[newLength];
					for (int v = 0; v < old.Length; v++)
						_values[v] = old[v];
				}
				_values[i] = value;
			}
		}
		void InsertValueAndIncreaseCount(int i, T value)
		{
			_count++;
			if (_values == null && value == null)
				return;

			try {
				SetValue(_count - 1, value);
			} catch (OutOfMemoryException) {
				_count--;
				throw;
			}

			for (int j = _count - 1; j > i; j--)
				_values[j] = _values[j - 1];
			_values[i] = value;
		}
		
		int Compare(int i, ref KeyWalker key)
		{
			int lengthMatched = 0;
			int nextOrLen;
			for(;;) {
				int c = CompareCell(i, ref key, out nextOrLen);
				if (c != 0 || nextOrLen <= 3)
					return c;
				lengthMatched += 3;
				i = nextOrLen;
			}
		}
		int CompareCell(int i, ref KeyWalker key, out int len)
		{
			int B = i << 2, c;
			len = _cells[B];
			if (key.Left == 0)
				return len > 0 ? More : 0;
			if (len == 0)
				return Less;
			
			byte b1 = _cells[B + 1];
			byte b2 = _cells[B + 2];
			byte b3 = _cells[B + 3];
			if ((c = b1.CompareTo(key[0])) != 0)
				return c;
			if (key.Left == 1)
				return len > 1 ? More : 0;
			if (len == 1)
				return Less;
			if ((c = b2.CompareTo(key[1])) != 0)
				return c;
			if (key.Left == 2)
				return len > 2 ? More : 0;
			if (len == 2)
				return Less;
			if ((c = b3.CompareTo(key[2])) != 0)
				return c;
			return key.Left > 3 ? Less : 0;
				
		}

		bool FindIndex(ref KeyWalker key, out int index)
		{
			// Do a binary search for the key
			int low = 0;
			int high = _count - 1;
			do {
				index = low + ((high - low) >> 1);
				int c = Compare(index, ref key);
				if (c < 0)
					low = index + 1;
				else if (c > 0)
					high = index - 1;
				else
					return true;
			} while (low <= high);
			index = low;
			return false;
		}

		public override bool Find(ref KeyWalker key, JPEnumerator e)
		{
			/*int index;
			int oldOffset = key.Offset;
			if (FindIndex(ref key, out index)) {
				// Complete match!
				e.Push(this, index);
				value = _values[index];
				return true;
			}
			e.Push(this, index);*/
			throw new NotImplementedException();
		}

		void Insert(int index, ref KeyWalker key, T value, ref JPNode<T> self)
		{
			int count = _count;

			// Is this node too big? If so, burst it
			if (ItIsTimeToBurst(key.Left))
			{
				self = Burst();
				bool existed = self.Set(ref key, ref value, ref self, JPMode.Create);
				Debug.Assert(!existed);
			}
			else
				InsertNormally(index, ref key, value);
		}

		private void InsertNormally(int index, ref KeyWalker key, T value)
		{
			if (Partition == _count)
				EnlargeLeftPartition((key.Left - 1) / 3);
			else if (ExtraCellsFree * 3 < key.Left - 3)
				Expand((key.Left - 1) / 3);

			InsertValueAndIncreaseCount(index, value);

			int i;
			for (i = _count - 1; i > index; i--)
				CopyCell(i - 1, i);

			CopyCell(ref key, i);
			while (key.Left > 3)
			{
				int next = AllocCell();
				_cells[i << 2] = (byte)next;
				key.Advance(3);
				i = next;
				CopyCell(ref key, i);
			}
		}

		#region Memory management in the _cells array

		void FreeCell(int i)
		{
			Debug.Assert(i >= Partition && i < CellCount && i < MaxCells);

			FreeCellInternal(i);
			_extraCellsUsed--;

			Debug.Assert(ExtraCellsFree > 0);
		}
		void FreeCellInternal(int i)
		{
			_cells[i << 2] = _firstFree;
			_firstFree = (byte)i;
		}
		int AllocCell()
		{
			Debug.Assert(ExtraCellsFree > 0);
			Debug.Assert(_firstFree >= Partition);

			_extraCellsUsed++;
			int i = AllocCellInternal();
			
			Debug.Assert((_firstFree == 0) == (ExtraCellsFree == 0));
			return i;
		}
		int AllocCellInternal()
		{
			int i = _firstFree;
			_firstFree = _cells[i << 2];
			return i;
		}

		private void Expand(int cellsNeeded)
		{
			int oldCellCount = CellCount;
			int newCellCount = Math.Min(oldCellCount + (oldCellCount >> 1) + cellsNeeded, MaxCells);

			byte[] newCells = new byte[newCellCount << 2];
			Copy(_cells, 0, newCells, 0, oldCellCount);
			byte[] oldCells = _cells;
			_cells = newCells;

			FreeNewCells(newCells, oldCells.Length);
		}

		private void FreeNewCells(byte[] newCells, int oldCellsBytes)
		{
			Debug.Assert(oldCellsBytes < newCells.Length);
			Debug.Assert(((oldCellsBytes | newCells.Length) & 3) == 0);
			int B;
			for (B = oldCellsBytes; B + 4 < newCells.Length; B += 4)
				newCells[B] = (byte)((B + 4) >> 2);
			newCells[B] = _firstFree;
			_firstFree = (byte)(oldCellsBytes >> 2);
		}

		private void EnlargeLeftPartition(int extraCellsIfExpanding)
		{
			int partition = Partition;
			int newPartition = Math.Min(partition + (partition >> 1) + (_extraCellsUsed >> 3), MaxCount);
			int nexti;
			Debug.Assert(newPartition > partition);

			int cellsNeeded = newPartition - partition + extraCellsIfExpanding;
			if (cellsNeeded > ExtraCellsFree)
				Expand(cellsNeeded);

			// Eliminate free cells that use the region that is to be reserved
			if (_firstFree != 0)
			{
				for (int i = _firstFree; ; i = nexti) {
					nexti = _cells[i << 2];
					if (nexti == 0)
						break;
					if (nexti < newPartition)
					{
						do	nexti = _cells[nexti << 2];
						while (nexti < newPartition && nexti != 0);
						_cells[i << 2] = (byte)nexti;
					}
				}
				if (_firstFree < newPartition)
					_firstFree = _cells[_firstFree << 2];
				Debug.Assert(_firstFree >= newPartition);
			}
			
			// Relocate cells in the reserved region
			for (int k = 0; k < _count; k++)
			{
				for (int i = k; ; i = nexti)
				{
					if ((nexti = _cells[i << 2]) < MinPartition)
						break;
					if (nexti < newPartition) {
						int newi = AllocCellInternal();
						Debug.Assert(newi >= newPartition);
						_cells[i << 2] = (byte)newi;
						CopyCell(nexti, newi);
						_cells[nexti << 2] = 0; // not strictly necessary
						nexti = newi;
					}
				}
			}

			Partition = newPartition;
		}
		
		private void ShrinkLeftPartition()
		{
			int newPartition = Math.Max(4, Count);
			Debug.Assert(newPartition <= Partition);
			for (int i = Partition; i < newPartition; i++)
				FreeCellInternal(i);
			Partition = newPartition;
		}

		void Shrink(bool shrinkPartition)
		{
			int newPartition = Partition;
			if (shrinkPartition)
				newPartition = _count;
			byte[] newCells = new byte[newPartition + _extraCellsUsed];
			Copy(_cells, 0, newCells, 0, _count);
			CompactInto(newCells, newPartition);
			_cells = newCells;
			Partition = newPartition;
		}

		private void CompactInto(byte[] newCells, int newPartition)
		{
			int p = newPartition;
			for (int i = 0; i < _count; i++)
			{
				Debug.Assert(newCells[i << 2] == _cells[i << 2]);
				// Follow key [i], copying it to newCells
				int next = _cells[i << 2];
				if (next >= MinPartition)
				{
					newCells[i << 2] = (byte)p;
					for(;;)
					{
						newCells[(p << 2) + 1] = _cells[(next << 2) + 1];
						newCells[(p << 2) + 2] = _cells[(next << 2) + 2];
						newCells[(p << 2) + 3] = _cells[(next << 2) + 3];
						next = _cells[(next << 2)];
						if (next < MinPartition)
							break;
						newCells[p << 2] = (byte)(p + 1);
						p++;
					}
					newCells[p << 2] = (byte)next;
					p++;
				}
			}

			// caller provides no free cells
			Debug.Assert(newPartition + _extraCellsUsed == newCells.Length >> 2);
			Debug.Assert(p << 2 == newCells.Length);
			_firstFree = 0;
		}

		#endregion

		private void CopyCell(int from, int to)
		{
			from <<= 2;
			to <<= 2;
			byte[] cells = _cells;
			cells[to    ] = cells[from    ];
			cells[to + 1] = cells[from + 1];
			cells[to + 2] = cells[from + 2];
			cells[to + 3] = cells[from + 3];
		}

		private void CopyCell(ref KeyWalker key, int to)
		{
			byte[] cells = _cells;
			int left = key.Left;
			int B = to << 2;
			if (left > 0) {
				cells[B + 1] = key[0];
				if (left >= 2) {
					cells[B + 2] = key[1];
					if (left > 2) {
						cells[B + 3] = key[2];
						if (left > 3)
							return;
					}
				}
			}
			cells[B] = (byte)left;
		}

		static void Copy(byte[] sourceCells, int sourceCell, byte[] destCells, int destCell, int numCells)
		{
			if (numCells <= 16)
			{
				// TODO: fast unsafe version
				int from = sourceCell << 2;
				int toStop = (destCell + numCells) << 2;
				for (int to = destCell << 2; to < toStop; to += 4, from += 4)
				{
					destCells[to]     = sourceCells[from];
					destCells[to + 1] = sourceCells[from + 1];
					destCells[to + 2] = sourceCells[from + 2];
					destCells[to + 3] = sourceCells[from + 3];
				}
			}
			else
				Array.Copy(sourceCells, sourceCell << 2, destCells, destCell << 2, numCells << 2);
		}

		#region Burst algorithm

		private bool ItIsTimeToBurst(int keySize)
		{
			if (_count >= MaxCount)
				return true;
			else if ((MaxCount + _extraCellsUsed)*3 + keySize > (MaxCells-1)*3)
				return _count == Partition || keySize - 3 > ExtraBytesLeft;
			else
				return false;
		}
		private JPNode<T> Burst()
		{
			JPLinear<T> repl = new JPLinear<T>();

			KeyWalker key;
			bool existed;
			int length;
			int i = 0;
			do {
				int common = DetectCommonPrefix(i, out length);
				Debug.Assert(length > 0);

				if (GetKeyForBurst(i, common, length, out key)) {
					// Write a single key
					Debug.Assert(length == 1);
					T value = Value(i);
					JPNode<T> repl2 = repl;
					existed = repl.Set(ref key, ref value, ref repl2, JPMode.Create);
					Debug.Assert(repl == repl2); // TODO: ensure this is guaranteed
					Debug.Assert(!existed);
					i++;
				} else {
					// Create a JPLeaf to hold the key(s) that follow the common prefix
					Debug.Assert(common > 0);
					KeyWalker leafKey;
					GetKey(i, out leafKey);
					leafKey.Advance(common);
					
					JPNode<T> leaf;
					new JPLeaf<T>(ref leafKey, Value(i), out leaf);
					
					int stop = i + length;
					for (i++; i < stop; i++)
					{
						GetKey(i, out leafKey);
						leafKey.Advance(common);
						
						T value = Value(i);
						existed = leaf.Set(ref leafKey, ref value, ref leaf, JPMode.Create);
						Debug.Assert(!existed);
					}

					// Add the common prefix and leaf to repl
					repl.Set(ref key, leaf);
				}
			} while (i < _count);

			return repl;
		}

		static ScratchBuffer<byte[]> _kfbBuf;
		static ScratchBuffer<byte[]> _kapBuf;

		private bool GetKeyForBurst(int i, int common, int length, out KeyWalker key)
		{
			int singleKeyLength;
			if (length > 1 || (singleKeyLength = GetKeyLength(i)) > BurstSingleKeyMaxLength)
			{
				Debug.Assert(common <= 3);

				byte[] buf = _kfbBuf.Value;
				if (buf == null)
					_kfbBuf.Value = buf = new byte[3];

				int B = i << 2;
				buf[0] = _cells[B + 1];
				buf[1] = _cells[B + 2];
				buf[2] = _cells[B + 3];
				key = new KeyWalker(buf, common);
				return false;
			}
			else
			{	// Single key, not very long: put it directly in the linear node
				GetKey(i, out key);
				return true;
			}
		}
		private void GetKey(int i, out KeyWalker key)
		{
			GetKey(i, GetKeyLength(i), out key);
		}
		private void GetKey(int i, int keyLength, out KeyWalker key)
		{
			byte[] buf;
			if (keyLength > BurstSingleKeyMaxLength)
				buf = new byte[keyLength + 2];
			else if ((buf = _kapBuf.Value) == null)
				_kapBuf.Value = buf = new byte[BurstSingleKeyMaxLength];
			
			Debug.Assert(keyLength <= buf.Length);

			byte[] cells = _cells;
			int b = 0, B, next;
			for(;;) {
				B = i << 2;
				buf[b + 0] = cells[B + 1];
				buf[b + 1] = cells[B + 2];
				buf[b + 2] = cells[B + 3];
				next = cells[B];
				if (next < MinPartition)
					break;
				i = next;
				b += 3;
			}
			
			key = new KeyWalker(buf, keyLength);
		}
		
		private int GetKeyLength(int i)
		{
			byte[] cells = _cells;
			int B;
			for(int length = 0;; length += 3) {
				B = i << 2;
				int next = cells[B];
				if (next < MinPartition)
					return length + cells[B];
				i = next;
			}
		}

		private int DetectCommonPrefix(int start, out int length)
		{
			byte[] cells = _cells;
			if (cells[start << 2] == 0)
			{
				Debug.Assert(start == 0);
				length = 1;
				return 0;
			}
			
			int common = 3, B, stop = _count - 1;
			for (int i = start; i < stop; i++)
			{
				B = i << 2;
				if (common > cells[B])
					common = cells[B];
				if (cells[B + 1] != cells[B + 5]) {
					length = i + 1 - start;
					return common;
				}

				if (common > 1) {
					if (cells[B + 2] != cells[B + 6])
						common = 1;
					else if (cells[B + 3] != cells[B + 7])
						common = 2;
				}
			}

			B = stop << 2;
			if (common > cells[B])
				common = cells[B];
			length = stop + 1 - start;
			return common;
		}

		#endregion

		public override bool Set(ref KeyWalker key, ref T value, ref JPNode<T> self, JPMode mode)
		{
			int index;
			int oldOffset = key.Offset;
			if (FindIndex(ref key, out index))
			{
				// Complete match!
				T oldValue = _values[index];
				if ((mode & JPMode.Set) != JPMode.Find)
					_values[index] = value;
				value = oldValue;
				return true;
			}
			if ((mode & JPMode.Create) != JPMode.Find)
				Insert(index, ref key, value, ref self);
			return false;
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref JPNode<T> self)
		{
			return false;
		}

		#if DEBUG
		/// <summary>Debugging aid: spits out the contents of each 4-byte cell</summary>
		public string[] CellInfo
		{
			get {
				string[] info = new string[_cells.Length >> 2];
				StringBuilder sb = new StringBuilder(7);
				for (int i = 0; i < (_cells.Length >> 2); i++) {
					sb.Length = 7;
					int next = _cells[i << 2];
					sb[0] = (char)((next / 100) + '0');
					sb[1] = (char)((next / 10) % 10 + '0');
					sb[2] = (char)((next % 10) + '0');
					if (next < MinPartition)
						sb[0] = sb[1] = '_';
					sb[3] = ':';
					sb[4] = (char)_cells[(i << 2) + 1];
					sb[5] = (char)_cells[(i << 2) + 2];
					sb[6] = (char)_cells[(i << 2) + 3];
					if (next < 3)
						sb.Length = 4 + next;
					info[i] = sb.ToString();
				}
				return info;
			}
		}
		#endif
	}
}
