using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Runtime;
using Loyc.Utilities.JPTrie;
using System.Collections;

namespace Loyc.Utilities
{
	struct LCell
	{
		internal const byte FreeP = 255;

		public byte P;  
		public byte K0; // or next free cell
		public byte K1; // or previous free cell
		public byte K2; // or key length, if key is short

		public byte NextFree { get { return K0; } set { Debug.Assert(P == FreeP); K0 = value; } }
		public byte PrevFree { get { return K1; } set { Debug.Assert(P == FreeP); K1 = value; } }
		public byte LengthOrK2 { get { return K2; } set { Debug.Assert(P == FreeP); K2 = value; } }
		public bool IsFree { get { return P == FreeP; } }

		public LCell(byte p, byte k0, byte k1, byte k2) { P = p; K0 = k0; K1 = k1; K2 = k2; }
	}

	class JPLinear<T> : JPNode<T>
	{
		// _cells contains 4-byte groups called "cells". Cells encode partial (or 
		// complete) keys and pointers to values or child nodes. The first _count
		// cells encode the beginning of each key in the node.
		//
		// The first byte P acts as a pointer to one of four things:
		// 1. A child node (if P < _count); _children[P] is the child.
		// 2. Another cell (if P < CellCount); _cells[P] starts the next cell.
		// 3. The value _values[253-P] (if P < 253)
		// 4. The null value (if the byte is 254)
		//
		// If P is 255, it means that the cell is not in use; in that case, K0
		// points to the "next" free cell and K1 points to the "previous" free cell;
		// the free list is circular so that these pointers are always valid.
		//
		// If P points to another cell or if the fourth byte of the cell is not one
		// of the length specifiers LengthZero, LengthOne or LengthTwo, the remaining
		// 3 bytes of the cell are bytes of the key. Otherwise, the cell is the last 
		// one in the (partial) key and the fourth byte specifies the number of bytes 
		// of the key that the cell contains (LengthZero, LengthOne or LengthTwo). 
		// There can be (at most) one zero-length key in any given node (although 
		// there can sometimes be a zero-length cell following a cell of length 
		// three). A zero-length key never includes a pointer to a child node.
		LCell[] _cells;
		JPNode<T>[] _children; // null if there are no children
		T[] _values;           // null if no values are associated with any keys
		
		byte _count;
		byte _extraCellsUsed;
		byte _firstFree; // NullP if none free
		byte _childrenUsed;
		uint _valuesUsed; // Each bit indicates whether an entry in _values is in use

		const int MinPartition = 4;

		const int ContinueComparing = 256;

		internal const int MaxCount = 40; // Note: only 32 can have values
		internal const int MaxChildren = 32;
		internal const int MaxLengthPerKey = 50;
		internal const int NewChildThreshold = 13;
		internal const int NullP = 254;
		internal const int FreeP = 255;
		#if DEBUG
		internal const int MaxCells = 128; // for testing
		#else
		internal const int MaxCells = NullP - MaxChildren; // 222
		#endif
		
		internal const byte LengthZero = 255;
		internal const byte LengthOne = 254;
		internal const byte LengthTwo = 253;
		internal const byte LengthLong = 0; // not actually used in cells

		int ExtraCells { get { return _cells.Length - _count; } }
		int ExtraCellsFree { get { return _cells.Length - _count - _extraCellsUsed; } }
		int CellCount { [DebuggerStepThrough] get { return _cells.Length; } }
		int Count { [DebuggerStepThrough] get { return _count; } }
		int ExtraBytesLeft { get { return ExtraCellsFree * 3; } }

		public JPLinear(ref KeyWalker key, T value) : this(3 + (key.Left >> 1))
		{
			JPNode<T> self = this;
			Insert(0, ref key, value, ref self);
			Debug.Assert(self == this);
		}
		public JPLinear(ref KeyWalker key, JPNode<T> child) : this(3 + (key.Left >> 1))
		{
			JPNode<T> self = this;
			Insert(0, ref key, child, ref self);
			Debug.Assert(self == this);
		}
		public JPLinear(int initialCells)
		{
			if (initialCells > MaxCells)
				initialCells = MaxCells;
			
			_firstFree = NullP;
			FreeNewCells(_cells = new LCell[initialCells], 0);
		}

		#region Binary search for a key

		int FindIndex(ref KeyWalker key, out int finalCell)
		{
			// Do a binary search for the key. Returns the index of a successful 
			// match, or the index where the key should be inserted if there was
			// no match. finalCell is set to the index of the final cell of the 
			// matching key, or -1 if the match was unsuccessful. The final cell
			// is important, as it points to the associated value or child node.
			int oldOffset = key.Offset;

			int low = 0;
			int high = _count - 1;
			while (low <= high) {
				Debug.Assert(key.Offset == oldOffset);

				int index = low + ((high - low) >> 1);
				int c = Compare(index, ref key, out finalCell);
				if (c < 0)
					low = index + 1;
				else if (c > 0)
					high = index - 1;
				else
					return index;
			}
			finalCell = -1;
			return low;
		}

		int Compare(int i, ref KeyWalker key, out int finalCell)
		{
			int oldOffset = key.Offset;
			int Pnext;

			finalCell = i;
			int c = CompareCell(i, ref key, out Pnext);
			if (c != ContinueComparing)
			{
				Debug.Assert(c == 0 || key.Offset == oldOffset);
				return c;
			}
			do {
				Debug.Assert(IsNextCellP(Pnext));
				finalCell = Pnext;
				c = CompareCell(Pnext, ref key, out Pnext);
			} while (c == ContinueComparing);

			if (c != 0)
				key.Offset = oldOffset;
			return c;
		}
		
		int CompareCell(int i, ref KeyWalker key, out int P)
		{
			int dif;
			P = _cells[i].P;
			bool haveNextCell = IsNextCellP(P);
			byte k2 = _cells[i].LengthOrK2;
			byte cellLen = haveNextCell ? LengthLong : k2;

			if (key.Left == 0)
				return cellLen == LengthZero ? 0 : 1;
			if (cellLen == LengthZero)
				return IsChildP(P) ? 0 : -1;
			
			if (((dif = ((int)_cells[i].K0 - key[0]))) != 0)
				return dif;
			if (key.Left == 1)
			{
				if (cellLen == LengthOne) {
					key.Advance(1);
					return 0;
				} else
					return 1;
			}
			if (cellLen == LengthOne)
			{
				if (IsChildP(P)) {
					key.Advance(1);
					return 0;
				} else
					return -1;
			}
			if (((dif = ((int)_cells[i].K1 - key[1]))) != 0)
				return dif;
			if (key.Left == 2)
			{
				if (cellLen == LengthTwo) {
					key.Advance(2);
					return 0;
				} else
					return 1;
			}
			if (cellLen == LengthTwo)
			{
				if (IsChildP(P)) {
					key.Advance(2);
					return 0;
				} else
					return -1;
			}

			if (((dif = ((int)k2 - key[2]))) != 0)
				return dif;
			if (key.Left == 3)
			{
				if (!haveNextCell) {
					key.Advance(3);
					return 0;
				} else
					return ContinueComparing;
			}
			
			// at this point, key.Left > 3 and cell length >= 3
			if (haveNextCell) {
				key.Advance(3);
				return ContinueComparing;
			} else if (IsChildP(P)) {
				key.Advance(3);
				return 0;
			} else
				return -1; // cell associated with a value
		}

		#endregion

		#region Public methods

		// Insertion process:
		// - Find index (to determine whether we're inserting here or in a child)
		// - Ensure the partition is big enough and that there are enough value slots
		// - Ensure there are enough extra free cells
		// - Add the new item
		public override bool Set(ref KeyWalker key, ref T value, ref JPNode<T> self, JPMode mode)
		{
			int finalCell;
			int index = FindIndex(ref key, out finalCell);
			if (finalCell >= 0)
			{
				int P = _cells[finalCell].P;
				if (IsChildP(P))
					return _children[P].Set(ref key, ref value, ref _children[P], mode);
				else {
					int vIndex = NullP - 1 - P;
					if (vIndex >= 0) {
						T oldValue = _values[vIndex];
						if ((mode & JPMode.Set) != (JPMode)0)
							_values[vIndex] = value;
						value = oldValue;
					} else {
						if ((mode & JPMode.Set) != (JPMode)0)
							_cells[finalCell].P = (byte)AllocValueP(value);
						value = default(T); // old value
					}
					return true;
				}
			}
			else if ((mode & JPMode.Create) != (JPMode)0)
			{
				Insert(index, ref key, value, ref self);
			}
			return false;
		}
		private void Insert(int index, ref KeyWalker key, T value, ref JPNode<T> self)
		{
			if (PrepareSpace(key.Left, ref self) <= index)
			{
				// Reorganization occurred; retry
				bool existed = self.Set(ref key, ref value, ref self, JPMode.Create);
				Debug.Assert(!existed);
				return;
			}
			
			if (key.Left > MaxLengthPerKey)
			{
				KeyWalker key0 = new KeyWalker(key.Buffer, key.Offset, MaxLengthPerKey);
				key.Advance(MaxLengthPerKey);
				JPLinear<T> child = new JPLinear<T>(ref key, value);
				int P = AllocChildP(child);
				int finalCell = LLInsertKey(index, ref key0);
				_cells[finalCell].P = (byte)P;
			}
			else
			{
				// Normal case
				int P = AllocValueP(value);
				int finalCell = LLInsertKey(index, ref key);
				_cells[finalCell].P = (byte)P;
			}

			CheckValidity();
		}
		public override void AddChild(ref KeyWalker key, JPNode<T> child, ref JPNode<T> self)
		{
			int finalCell;
			int index = FindIndex(ref key, out finalCell);

			if (finalCell >= 0)
			{
				int P = _cells[finalCell].P;
				Debug.Assert(IsChildP(P));
				_children[P].AddChild(ref key, child, ref _children[P]);
				return;
			}

			Insert(index, ref key, child, ref self);
		}
		private void Insert(int index, ref KeyWalker key, JPNode<T> child, ref JPNode<T> self)
		{
			if (PrepareSpace(key.Left, ref self) <= index)
			{
				// Reorganization occurred; retry
				self.AddChild(ref key, child, ref self);
				return;
			}

			if (key.Left > MaxLengthPerKey)
			{
				KeyWalker key0 = new KeyWalker(key.Buffer, key.Offset, MaxLengthPerKey);
				key.Advance(MaxLengthPerKey);
				child = new JPLinear<T>(ref key, child);
				int P = AllocChildP(child);
				int finalCell = LLInsertKey(index, ref key0);
				_cells[finalCell].P = (byte)P;
			}
			else
			{
				// Normal case
				int P = AllocChildP(child);
				int finalCell = LLInsertKey(index, ref key);
				_cells[finalCell].P = (byte)P;
			}

			CheckValidity();
		}

		#endregion

		#region Insertion process

		private int LLInsertKey(int index, ref KeyWalker key)
		{
			LCell[] cells = _cells;
			Debug.Assert(_cells[_count].IsFree);

			AllocCellInternal(_count);

			for (int i = _count; i > index; i--)
				cells[i] = cells[i - 1];

			_count++;

			return LLWriteKey(index, ref key);
		}
		private int LLWriteKey(int index, ref KeyWalker key)
		{
			int last;
			do
				index = LLWriteCell(ref key, last = index);
			while (index != NullP);
			return last;
		}

		private int LLWriteCell(ref KeyWalker key, int to)
		{
			LCell[] cells = _cells;
			int left = key.Left;
			if (left > 0) {
				cells[to].K0 = key[0];
				if (left >= 2) {
					cells[to].K1 = key[1];
					if (left > 2) {
						byte K2 = key[2];
						cells[to].K2 = K2;
						if (left > 3 || K2 >= LengthTwo)
						{
							key.Advance(3);
							return cells[to].P = (byte)AllocCell();
						}
					} else
						cells[to].K2 = LengthTwo;
				} else
					cells[to].K2 = LengthOne;
			} else
				cells[to].K2 = LengthZero;
			
			return NullP;
		}

		#endregion

		#region Misc

		bool IsNextCellP(int P)
		{
			return (uint)(P - _count) < (uint)(_cells.Length - _count);
		}
		bool IsChildP(int P)
		{
			return P < _count;
		}

		static int FirstZero(uint i)
		{
			int result = 0;
			if ((ushort)~i == 0) { // (i & 0xFFFF) == 0xFFFF
				i >>= 16;
				result = 16;
			}
			if ((byte)~i == 0) { // (i & 0xFF) == 0xFF
				i >>= 8;
				result += 8;
			}
			if ((i & 0xF) == 0xF) {
				i >>= 4;
				result += 4;
			}
			if ((i & 3) == 3)
			{
				i >>= 2;
				result += 2;
			}
			if ((i & 1) == 1)
			{
				result++;
				if ((i & 2) == 2)
				{
					Debug.Assert(result == 31);
					return result + 1;
				}
			}
			return result;
		}

		#endregion

		#region Memory management

		private int AllocValueP(T value)
		{
			if (value == null)
				return NullP;
			if (_values == null)
			{
				_values = new T[4];
				_valuesUsed = 1;
				_values[0] = value;
				return NullP - 1;
			}
			else
			{
				int v = FirstZero(_valuesUsed);
				if (v >= _values.Length)
					_values = InternalList<T>.CopyToNewArray(_values, _values.Length, _values.Length + 1 + (_values.Length >> 1));
				_values[v] = value;
				_valuesUsed |= (1u << v);
				return NullP - 1 - v;
			}
		}
		
		private byte AllocChildP(JPNode<T> child)
		{
			Debug.Assert(child != null);
			if (_children == null)
			{
				Debug.Assert(_childrenUsed == 0);
				_children = new JPNode<T>[4];
				_children[0] = child;
				_childrenUsed = 1;
				return 0;
			}
			else if (_childrenUsed == _children.Length)
			{
				_children = InternalList<JPNode<T>>.CopyToNewArray(_children, _children.Length, _children.Length << 1);
				_children[_childrenUsed] = child;
				return _childrenUsed++;
			}
			else
			{
				int c = 0;
				for (c = 0; _children[c] != null; c++) {}
				_children[c] = child;
				_childrenUsed++;
				return (byte)c;
			}
		}

		#endregion

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref JPNode<T> self)
		{
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

		/// <summary>
		/// Makes sure there is a free cell at _count, and that there are enough 
		/// free cells for a new key of the specified length. Child node(s) are 
		/// created if necessary; in the worst case, the whole node is converted
		/// to a bitmap node.
		/// </summary>
		/// <param name="keyLeft"></param>
		/// <param name="self"></param>
		/// <returns>Returns the first index affected by any modifications, or -1 
		/// if 'self' changed</returns>
		/// <remarks>
		/// Linear nodes are "reorganized" when they run out of free space, when
		/// Count reaches MaxCount, when the number of values reaches 32, or
		/// when the number of children reaches MaxChildren.
		/// <para/>
		/// Well, actually if we run out of free space we can either reorganize, or
		/// allocate more space, unless the number of cells reaches MaxCells.
		/// <para/>
		/// Another problem that this method must address is what to do if there are
		/// enough free cells, but the cell at _count is in use. This problem only 
		/// occurs if there is fragmentation in the free cell list.  In that case an
		/// O(_count+_extraCellsUsed) scan would be needed to find who is using the 
		/// cell and relocate it, but it's easier (and not much slower) just to do 
		/// the same compaction process that is done when enlarging _cells.
		/// <para/>
		/// Compaction moves all free cells to the middle of the array, which helps
		/// ensure that when _count needs to increase, _cells[_count] is free.
		/// <para/>
		/// There are two "reorganizing" options:
		/// 1. Create a child node to free up space (can be done repeatedly)
		/// 2. Convert this node to a JPBitmap node (TODO)
		/// <para/>
		/// </remarks>
		private int PrepareSpace(int keyLeft, ref JPNode<T> self)
		{
			int cellsNeeded = keyLeft / 3 + 1;
			int firstIndexAffected = _count + 1;
			bool max = MaxCountReached;
			if (max || ExtraCellsFree < cellsNeeded) {
				firstIndexAffected = Reorganize(cellsNeeded, ref self, max);
			} else if (!_cells[_count].IsFree) {
				// Defragment the node to ensure _cells[_count] is free
				int numCellsUsed = _count + _extraCellsUsed;
				ResizeAndDefrag(cellsNeeded + numCellsUsed + (numCellsUsed >> 1));
				Debug.Assert(ExtraCellsFree >= cellsNeeded);
			}
			Debug.Assert(self != this || _cells[_count].IsFree);
			return firstIndexAffected;
		}

		private int Reorganize(int cellsNeeded, ref JPNode<T> self, bool maxCountReached)
		{
			int firstIndexAffected = _count + 1;

			if (_count <= 4) {
				Enlarge(cellsNeeded);
				return firstIndexAffected;
			}

			do {
				int index, length, prefixBytes, savings;
				savings = FindCommonPrefix(out index, out length, out prefixBytes);
				int maxEnlargement = MaxCells - _count - _extraCellsUsed;
				if (!maxCountReached && savings < NewChildThreshold && cellsNeeded <= maxEnlargement)
				{
					Enlarge(cellsNeeded);
					return _count;
				}
				
				// TODO: currently, switching to a bitmap node is treated as a last resort. Be more willing.
				if (savings > -1) {
					CreateChildWithCommonPrefix(index, length, prefixBytes);
					firstIndexAffected = Math.Min(firstIndexAffected, index);
				} else {
					ConvertToBitmapNode(ref self);
					return -1;
				}
			} while (ExtraCellsFree < cellsNeeded);
			
			return firstIndexAffected;
		}

		bool MaxCountReached
		{
			get { return _count >= MaxCount || _valuesUsed == 0xFFFFFFFFu || _childrenUsed >= MaxChildren; }
		}

		private void CreateChildWithCommonPrefix(int index, int length, int prefixBytes)
		{
			Debug.Assert(length > 1);
			Debug.Assert(index + length <= _count);
			Debug.Assert(MeasureCommonPrefix(index, index + 1) >= prefixBytes);

			JPNode<T> child = null;
			KeyWalker kw = new KeyWalker(new byte[MaxLengthPerKey], 0);
			int finalP;
			bool existed;
			T value;

			// Build the child node
			int i;
			for (i = index; i < index + length; i++)
			{
				kw.Reset();
				ExtractKey(i, ref kw, out finalP);
				kw.Reset(prefixBytes);
				if (child == null)
					child = new JPLinear<T>(3 + (kw.Left >> 1));
				if (finalP < _count) {
					child.AddChild(ref kw, _children[finalP], ref child);
				} else {
					value = finalP < NullP ? _values[NullP - 1 - finalP] : default(T);
					existed = child.Set(ref kw, ref value, ref child, JPMode.Create);
				}
			}
			
			// Delete all entries with the common prefix, and replace them with a single entry
			for (i = index; i < index + length; i++)
				LLFreeItem(i);
			
			for (i = index + 1; i + length - 1 < _count; i++)
				_cells[i] = _cells[i+length-1];
			for (; i < _count; i++)
				FreeLeftHandCell(i);

			EliminateIllegalChildIndices(_count - (length-1));

			_count -= (byte)(length-1);

			kw = new KeyWalker(kw.Buffer, prefixBytes);
			int lastCell = LLWriteKey(index, ref kw);
			_cells[lastCell].P = AllocChildP(child);

			CheckValidity();
		}

		private void EliminateIllegalChildIndices(int firstIllegal)
		{
			for (int i = 0; i < _count; i++)
			{
				byte P = _cells[i].P;
				if (P < _count && P >= firstIllegal)
				{
					byte P2 = AllocChildP(_children[P]);
					Debug.Assert(P2 < firstIllegal);
					_cells[i].P = P2;
					_children[P] = null;
				}
			}
		}

		private void LLFreeItem(int i)
		{
			byte P = _cells[i].P;
			_cells[i].P = NullP; // not strictly necessary

			if (IsNextCellP(P))
			{
				// Add the extra cells of item i to the free list in such a way that 
				// (assuming the item was allocated right-to-left in the first place)
				// the next allocation from the free cells will also occur right-to-
				// left. We can't easily do this with the normal FreeCell methods.
				byte first = _firstFree;
				if (first == NullP)
				{	// oops, we need one free already
					i = P;
					P = _cells[i].P;
					FreeCell(i);
					first = _firstFree;
					if (!IsNextCellP(P))
						goto end;
				}

				byte last = _cells[_firstFree].PrevFree;

				_cells[last].NextFree = P;
				_firstFree = P;

				byte prevP = last;
				do {
					i = P;
					P = _cells[i].P;
					_cells[i].P = FreeP;
					_cells[i].NextFree = P;
					_cells[i].PrevFree = prevP;
					_extraCellsUsed--;
					prevP = (byte)i;
				} while (IsNextCellP(P));

				_cells[i].NextFree = first;
				_cells[first].PrevFree = (byte)i;
			}
		
		end:
			LLFreeValueOrChild(P);
		}

		private void LLFreeValueOrChild(byte P)
		{
			if (P < _count)
			{
				Debug.Assert(_children[P] != null);
				_children[P] = null;

				if (_childrenUsed-- == 1)
					_children = null;
			}
			else if (P < NullP)
			{
				int v = NullP - 1 - P;
				Debug.Assert(v < _values.Length);
				_values[v] = default(T);
				_valuesUsed &= ~(1u << v);
				
				int half = _values.Length >> 1;
				if (_valuesUsed <= (1 << half) && half > 2)
					_values = InternalList<T>.CopyToNewArray(_values, half, half);
			}
		}

		/// <summary>Appends the key at the specified index to kw, allocating new 
		/// buffer space if needed.</summary>
		/// <param name="index">Index of the key to extract</param>
		/// <param name="kw">The key is written to kw starting at kw.Buffer[kw.Offset]</param>
		private void ExtractKey(int index, ref KeyWalker kw, out int finalP)
		{
			bool done = false;
			do {
				LCell cell = _cells[index];
				int cellLen = 3;
				if (!IsNextCellP(finalP = cell.P))
				{
					done = true;
					if (cell.LengthOrK2 >= LengthTwo) {
						cellLen = LengthZero - cell.LengthOrK2;
						if (cellLen == 0)
							break;
					}
				}
				byte[] buf = kw.Buffer;
				int bufLeft = buf.Length - kw.Offset;
				if (bufLeft < cellLen)
					buf = InternalList<byte>.CopyToNewArray(buf, kw.Offset, kw.Offset + 4 + (kw.Offset >> 1));

				buf[kw.Offset] = cell.K0;
				if (cellLen >= 2)
					buf[kw.Offset + 1] = cell.K1;
				if (cellLen > 2)
					buf[kw.Offset + 2] = cell.K2;

				kw = new KeyWalker(buf, kw.Offset + cellLen, 0);
				index = cell.P;
			} while (!done);
		}

		private void ConvertToBitmapNode(ref JPNode<T> self)
		{
			throw new NotImplementedException();
		}

		/// <summary>Finds the "best" common prefix to factor out into a child node.</summary>
		/// <param name="index">First index of a range of items with a common prefix</param>
		/// <param name="length">Number of items with a common prefix (minimum 2)</param>
		/// <param name="prefixBytes">Number of bytes this range of items has in common</param>
		/// <returns>An estimate of the number of cells that will be freed up by 
		/// creating a child node, or -1 if there are no common prefixes in this 
		/// node.</returns>
		private int FindCommonPrefix(out int bestIndex, out int bestLength, out int bestPrefixBytes)
		{
			int length;
			int bestSavings = -1;
			bestIndex = bestLength = bestPrefixBytes = 0;

			for (int i = 0; i < _count; i += length) {
				int prefixBytes = MeasureCommonPrefix(i, out length);
				if (length > 1) {
					int savings = prefixBytes * length;
					if (savings > bestSavings)
					{
						bestSavings = savings;
						bestIndex = i;
						bestLength = length;
						bestPrefixBytes = prefixBytes;
					}
				}
			}
			bestSavings = (bestSavings + 2) / 3; // convert to cells
			return bestSavings;
		}

		// Called by FindCommonPrefix. Returns the length of the common prefix.
		private int MeasureCommonPrefix(int start, out int length)
		{
			// Start by seeing how many cells have a common prefix of 3 or more.
			// Also find out how many cells have a common prefix of 2 or 1, in
			// case we save more by using a shorter prefix (rare).
			LCell[] cells = _cells;
			int end1, end2, end3;
			int common = 0, common3N = 0;
			for (end3 = start + 1; end3 < _count; end3++) {
				common = MeasureCommonPrefix(start, end3);
				if (common < 3)
					break;
				if (common < common3N || common3N == 0)
					common3N = common;
			}
			end2 = end3;
			if (common == 2)
				for (end2++; end2 < _count; end2++)
				{
					common = MeasureCommonPrefix(start, end2);
					if (common < 2)
						break;
				}
			end1 = end2;
			if (common == 1)
				for (end1++; end1 < _count; end1++)
					if (MeasureCommonPrefix(start, end1) == 0)
						break;

			int savings1 = end1 - start;
			int savings2 = (end2 - start) << 1;
			int savingsN = (end3 - start) * common3N;

			if (savingsN > savings2 && savingsN > savings1)
			{
				length = end3 - start;
				return common3N;
			}
			else if (savings2 > savings1)
			{
				length = end2 - start;
				return 2;
			}
			else
			{
				length = end1 - start;
				return 1;
			}
		}
		
		private int MeasureCommonPrefix(int i1, int i2)
		{
			Debug.Assert(i1 < _count && i2 < _count);

			int settled = 0;
			for (;;) {
				LCell cell1 = _cells[i1], cell2 = _cells[i2];
				if (cell1.K0 != cell2.K0)
					return settled;

				int minLen = Math.Max(CellLength(cell1), CellLength(cell2));
				if (minLen == LengthZero)
					return settled;
				if (minLen == LengthOne)
					return settled + 1;

				if (cell1.K1 != cell2.K1)
					return settled + 1;
				else if (minLen == LengthTwo)
					return settled + 2;
				else if (cell1.K2 != cell2.K2)
					return settled + 2;
				else if (!IsNextCellP(cell1.P) || !IsNextCellP(cell2.P))
					return settled + 3;

				// Move to the next cell
				settled += 3;
				i1 = cell1.P;
				i2 = cell2.P;
			}
		}

		private byte CellLength(LCell cell)
		{
			return IsNextCellP(cell.P) ? LengthLong : cell.K2;
		}
		private int CellLengthAsInt(int i)
		{
			return IsNextCellP(_cells[i].P) ? 3 : Math.Min(LengthZero - _cells[i].K2, 3);
		}

		private void Enlarge(int cellsNeeded)
		{
			int newSize = _cells.Length + (_cells.Length >> 1) + cellsNeeded;
			ResizeAndDefrag(newSize);
			Debug.Assert(ExtraCellsFree >= cellsNeeded);
		}

		private void ResizeAndDefrag(int proposedSize)
		{
			int newSize = proposedSize;
			if (newSize >= MaxCells - (MaxCells >> 3))
				newSize = MaxCells;

			LCell[] newCells = new LCell[newSize];
			int oldP, newP, nextP = newSize - 1;

			for (int i = 0; i < _count; i++)
			{
				newCells[i] = _cells[i];
				newP = i;
				oldP = _cells[i].P;
				while (IsNextCellP(oldP))
				{
					newCells[newP].P = (byte)nextP;
					newP = nextP;
					nextP--;
					newCells[newP] = _cells[oldP];
					oldP = _cells[oldP].P;
				}
			}

			// Init free space
			Debug.Assert(nextP + 1 - _count == newSize - _extraCellsUsed - _count);
			if (nextP == _count - 1) {
				_firstFree = NullP;
			} else {
				Debug.Assert(nextP >= _count);
				_firstFree = (byte)nextP;
				_cells = newCells;
				for (; nextP >= _count; nextP--)
				{
					newCells[nextP].P = FreeP;
					newCells[nextP].NextFree = (byte)(nextP-1);
					newCells[nextP].PrevFree = (byte)(nextP+1);
				}
				newCells[_firstFree].PrevFree = (byte)(nextP + 1);
				newCells[nextP + 1].NextFree = _firstFree;
			}

			CheckValidity();
		}

		[Conditional("DEBUG")]
		private void CheckValidity()
		{
			Debug.Assert(_cells != null);
			BitArray cellsSeen = new BitArray(_cells.Length, false);
			BitArray childrenUsed = new BitArray(_count, false);
			uint valuesUsed = 0;
			
			Debug.Assert(_count <= _cells.Length);
			Debug.Assert(_cells.Length <= MaxCells);

			// Gather bit arrays indicating which elements of each array are in use,
			// and make sure that there is only one key using each element.
			for (int i = 0; i < _count; i++)
			{
				byte P = _cells[i].P;
				for(;;) {
					if (P < _count)
					{
						Debug.Assert(!childrenUsed[P]);
						Debug.Assert(_children != null);
						Debug.Assert(P < _children.Length);
						childrenUsed[P] = true;
						break;
					}
					else if (P >= _cells.Length)
					{
						Debug.Assert(P != FreeP);
						if (P != NullP)
						{
							int v = NullP - 1 - P;
							Debug.Assert(_values != null);
							Debug.Assert((uint)v < (uint)_values.Length);
							Debug.Assert((valuesUsed & (1u << v)) == 0);
							valuesUsed |= (1u << v);
						}
						break;
					}
					else
					{
						Debug.Assert(IsNextCellP(P));
						Debug.Assert(!cellsSeen[P]);
						cellsSeen[P] = true;
						P = _cells[P].P;
					}
				}
			}

			// Check that the expected children and values are/are not in use
			Debug.Assert(_valuesUsed == valuesUsed);
			int numChildrenUsed = 0;
			for (int i = 0; i < childrenUsed.Length; i++)
			{
				if (childrenUsed[i]) {
					numChildrenUsed++;
					Debug.Assert(_children != null && _children.Length > i);
				} else
					Debug.Assert(_children == null || _children.Length <= i || _children[i] == null);
			}
			Debug.Assert(numChildrenUsed == _childrenUsed);

			// Verify the integrity of the linked list of free cells
			int freeCount = 0;
			if (_firstFree != NullP) {
				Debug.Assert(IsNextCellP(_firstFree));
				byte P;
				for (byte nextP = _firstFree; ; P = nextP)
				{
					P = nextP;
					nextP = _cells[P].NextFree;

					Debug.Assert(_cells[P].P == FreeP);
					Debug.Assert(IsNextCellP(nextP));
					Debug.Assert(_cells[nextP].PrevFree == P);
					if (freeCount > 0 && P == _firstFree)
						break;

					freeCount++;
					Debug.Assert(!cellsSeen[P]);
					cellsSeen[P] = true;
				}
			}
			Debug.Assert(freeCount == ExtraCellsFree);

			// All cells should be accounted for
			for (int c = _count; c < cellsSeen.Length; c++)
				Debug.Assert(cellsSeen[c]);

			// TODO: verify that the items are sorted
		}

		#region Memory management in the _cells array

		private void FreeNewCells(LCell[] newCells, int oldCellCount)
		{
			Debug.Assert(oldCellCount < newCells.Length);
			Debug.Assert((_firstFree == NullP) == (oldCellCount == _count + _extraCellsUsed));

			for (int i = newCells.Length - 1; i >= oldCellCount; i--)
			{
				newCells[i].P = FreeP;
				newCells[i].NextFree = (byte)(i-1);
				newCells[i].PrevFree = (byte)(i+1);
			}
			if (_firstFree == NullP) {
				newCells[newCells.Length - 1].PrevFree = (byte)oldCellCount;
				newCells[oldCellCount].NextFree = (byte)(newCells.Length - 1);
			} else {
				newCells[newCells.Length - 1].PrevFree = newCells[_firstFree].PrevFree;
				newCells[oldCellCount].NextFree = _firstFree;
			}
			_firstFree = (byte)(newCells.Length - 1);
		}

		void FreeCell(int i)
		{
			Debug.Assert(!_cells[i].IsFree);

			FreeCellInternal(i);
			_extraCellsUsed--;

			Debug.Assert(ExtraCellsFree > 0);
		}
		void FreeCellInternal(int i)
		{
			_cells[i].P = FreeP;
			if (_firstFree != NullP)
			{
				byte next = _firstFree, prev = _cells[_firstFree].PrevFree;
				_cells[i].NextFree = next;
				_cells[i].PrevFree = prev;
				_cells[next].PrevFree = (byte)i;
				_cells[prev].NextFree = (byte)i;
			}
			else
			{
				_cells[i].NextFree = _cells[i].PrevFree = (byte)i;
			}
			_firstFree = (byte)i;
		}
		private void FreeLeftHandCell(int i)
		{
			FreeCellInternal(i);
			_firstFree = _cells[_firstFree].NextFree; // "move" cell i to the end of the list
		}
		int AllocCell()
		{
			Debug.Assert(ExtraCellsFree > 0);
			Debug.Assert(_firstFree >= _count);

			_extraCellsUsed++;
			int i = _firstFree;
			AllocCellInternal(i);
			
			Debug.Assert((_firstFree == NullP) == (ExtraCellsFree == 0));
			return i;
		}
		void AllocCellInternal(int P)
		{
			LCell[] cells = _cells;
			byte next = cells[P].NextFree;
			byte prev = cells[P].PrevFree;
			cells[next].PrevFree = prev;
			cells[prev].NextFree = next;
			if (_firstFree == P)
			{
				if (_firstFree != next)
					_firstFree = next;
				else {
					Debug.Assert(prev == next);
					_firstFree = NullP;
				}
			}
		}

		/*
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
		}*/

		#endregion

		static void Copy(LCell[] sourceCells, int sIndex, LCell[] destCells, int dIndex, int length)
		{
			if (length <= 32) {
				int destStop = dIndex + length;
				while (dIndex < destStop)
					destCells[dIndex++] = sourceCells[sIndex++];
			} else
				Array.Copy(sourceCells, sIndex, destCells, dIndex, length);
		}
		/*
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
			JPBitmap<T> repl = new JPBitmap<T>();

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
					leaf = new JPLinear<T>(ref leafKey, Value(i));
					
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
		*/
		#if DEBUG
		/// <summary>Debugging aid: spits out the contents of each 4-byte cell</summary>
		public string[] CellInfo
		{
			get {
				string[] info = new string[_cells.Length];
				StringBuilder sb = new StringBuilder(7);
				for (int i = 0; i < info.Length; i++) {
					sb.Length = 7;
					LCell cell = _cells[i];
					if (cell.IsFree)
					{
						sb[0] = (char)((cell.NextFree / 100) + '0');
						sb[1] = (char)((cell.NextFree / 10) % 10 + '0');
						sb[2] = (char)((cell.NextFree % 10) + '0');
						sb[3] = '«';
						sb[4] = (char)((cell.PrevFree / 100) + '0');
						sb[5] = (char)((cell.PrevFree / 10) % 10 + '0');
						sb[6] = (char)((cell.PrevFree % 10) + '0');
					}
					else
					{
						if (cell.P == NullP)
						{
							sb[0] = 'n';
							sb[1] = 'i';
							sb[2] = 'l';
						}
						else {
							int P = cell.P;
							if (P < _count)
								sb[0] = 'c';
							else if (P > _cells.Length) {
								sb[0] = 'v';
								P = NullP - 1 - P;
							} else {
								sb[0] = (char)((P / 100) + '0');
							}
							sb[1] = (char)((P / 10) % 10 + '0');
							sb[2] = (char)((P % 10) + '0');
						}
						sb[3] = ':';
						sb[4] = (char)cell.K0;
						sb[5] = (char)cell.K1;
						sb[6] = (char)cell.K2;
						
						int len = CellLengthAsInt(i);
						if (len < 3)
							sb.Length = 4 + len;
					}
					info[i] = sb.ToString();
				}
				return info;
			}
		}
		#endif
	}
}
