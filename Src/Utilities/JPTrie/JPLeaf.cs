using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.JPTrie
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
		internal const int MaxKeyLength = 3 * (255-MaxCount);
		internal const int MaxCount = 32;

		int Partition
		{
			get { return _partition; }
			set {
				Debug.Assert(value >= 4 && value <= CellCount && value <= MaxCount);
				_partition = (byte)value;
			}
		}
		int ExtraCells { get { return (_cells.Length >> 2) - Partition; } }
		int ExtraCellsFree { get { return (_cells.Length >> 2) - Partition - _extraCellsUsed; } }
		int CellCount { get { return _cells.Length >> 2; } }

		public JPLeaf(ref KeyWalker key, T value)
		{
			Debug.Assert(key.Left <= MaxKeyLength);
			_partition = 4;
			_cells = new byte[16 + (key.Left / 3 << 2)];

			JPNode<T> self = this;
			Insert(0, ref key, value, ref self);
			Debug.Assert(self == this);
		}

		T Value(int i) { return _values == null ? default(T) : _values[i]; }
		
		void SetValue(int i, T value)
		{
			if (value != null)
			{
				if (_values == null)
					_values = new T[Math.Max(CellCount, 3)];
				else if (i >= _values.Length) {
					Debug.Assert(i < CellCount);
					T[] old = _values;
					int newLength = Math.Max(CellCount, 
						Math.Min(MaxCount, old.Length + (old.Length >> 1) + 1));
					_values = new T[newLength];
					for (int v = 0; v < old.Length; v++)
						_values[v] = old[v];
				}
				_values[i] = value;
			}
		}
		
		int Compare(int i, ref KeyWalker key)
		{
			int lengthMatched = 0;
			int nextOrLen;
			for(;;) {
				int c = CompareCell(i, ref key, out nextOrLen);
				if (c != 0)
					return c;
				if (nextOrLen <= 3)
				{
					lengthMatched += nextOrLen;
					Debug.Assert(lengthMatched == key.Left);
					return 0;
				}
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
			else if (len == 0)
				return Less;
			
			byte b1 = _cells[B + 1];
			byte b2 = _cells[B + 2];
			byte b3 = _cells[B + 3];
			if ((c = b1.CompareTo(key[0])) != 0)
				return c;
			if (key.Left == 1)
				return len > 1 ? More : 0;
			else if (len == 1)
				return Less;
			if ((c = b2.CompareTo(key[1])) != 0)
				return c;
			if (key.Left == 2)
				return len > 2 ? More : 0;
			else if (len == 2)
				return Less;
			return b3.CompareTo(key[2]);
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
			return false;
		}

		public override bool Find(ref KeyWalker key, JPEnumerator e, ref T value)
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
			return false;
		}

		void Insert(int index, ref KeyWalker key, T value, ref JPNode<T> self)
		{
			int count = _count;

			// Is this node too big? If so, burst it
			if (ItIsTimeToBurst(key.Left))
				self = Burst();
			else {
				if (Partition == _count)
					EnlargeLeftPartition(key.Left);
				else if (ExtraCellsFree * 3 < key.Left - 3)
					Expand(key.Left);
				Debug.Assert(_count < Partition);

			}
		}

		#region Memory management in the _cells array

		void FreeCell(int i)
		{
			Debug.Assert(i >= Partition && i < CellCount && i < 256);

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
			int newCellCount = Math.Min(oldCellCount + (oldCellCount >> 1) + cellsNeeded, 256);

			byte[] newCells = new byte[newCellCount << 2];
			Copy(_cells, 0, newCells, 0, oldCellCount);
			byte[] oldCells = _cells;
			_cells = newCells;

			FreeNewCells(newCells, oldCells.Length);
		}

		private void FreeNewCells(byte[] newCells, int oldCellsLength)
		{
			Debug.Assert(oldCellsLength < newCells.Length);
			Debug.Assert(((oldCellsLength | newCells.Length) & 3) == 0);
			int B;
			for (B = oldCellsLength; B + 4 < newCells.Length; B += 4)
				newCells[B] = (byte)((B + 4) >> 2);
			newCells[B] = _firstFree;
			_firstFree = (byte)(oldCellsLength >> 2);
		}

		private void EnlargeLeftPartition(int extraCellsIfExpanding)
		{
			int partition = Partition;
			int newPartition = Math.Min(partition + (partition >> 1) + (_extraCellsUsed >> 3), MaxCount);
			Debug.Assert(newPartition > partition);

			int gap = newPartition - partition - ExtraCellsFree;
			while (gap > 0)
				Expand(extraCellsIfExpanding + gap);

			// Eliminate free cells that use the region that is to be reserved
			int nexti;
			for (int i = _firstFree; i != 0; i = nexti) {
				nexti = _cells[i << 2];
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
						nexti = newi;
					}
				}
			}
		}
		
		private void ShrinkLeftPartition()
		{
			int newPartition = Math.Max(4, (int)_count);
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

		#endregion

		private void CopyCell(int from, int to)
		{
			// TODO: fast version
			byte[] cells = _cells;
			cells[(to << 2)    ] = cells[(from << 2)    ];
			cells[(to << 2) + 1] = cells[(from << 2) + 1];
			cells[(to << 2) + 2] = cells[(from << 2) + 2];
			cells[(to << 2) + 3] = cells[(from << 2) + 3];
		}

		static void Copy(byte[] sourceCells, int cellIndex, byte[] destCells, int destIndex, int numCells)
		{
			// TODO: fast version
			Array.Copy(sourceCells, cellIndex << 2, destCells, destIndex << 2, numCells << 2);
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

		private bool ItIsTimeToBurst(int keySize)
		{
			return _count >= MaxCount || (MaxCount + _extraCellsUsed)*3 + keySize > 255*3;
		}
		private JPNode<T> Burst()
		{
			// Detect a common prefix of up to three bytes
			int common = 0;
			for (common = 0; common < 3; common++)
				for (int i = 1; i < _count; i++) {
					if (_cells[i << 2] <= common || _cells[(i << 2) + common + 1] != _cells[(i << 2) + common - 3])
						goto breakAll;
				}
			breakAll:

			if (common > 0)
			{
				// Create a Linear node with one child which contains 
			}
			throw new NotImplementedException();
		}

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
	}
}
