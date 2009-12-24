using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.JPTrie
{
	class JPLeaf<T> : JPNode<T>
	{
		byte _count;
		byte _partitionMinus4;
		byte _extraCellsUsed;
		byte _firstFree;
		byte[] _cells;
		T[] _values;   // null if no values are associated with any keys
		
		const int Less = -1;
		const int More = 1;
		internal const int MaxKeyLength = 3 * 252;

		int Partition { get { return _partitionMinus4 + 4; } }
		int ExtraCellsFree { get { return _cells.Length - Partition - _extraCellsUsed; } }
		int AllocExtraCell();

		public JPLeaf(ref KeyWalker key, T value)
		{
			Debug.Assert(key.Left <= MaxKeyLength);
			_partitionMinus4 = 4 - 4;
			_cells = new byte[16 + (key.Left / 3 << 2)];
			JPNode<T> self = this;
			Insert(0, ref key, value, ref self);
			Debug.Assert(self == this);
		}

		T Value(int i) { return _values == null ? default(T) : _values[i]; }
		
		int Compare(int i, ref KeyWalker key)
		{
			int lengthMatched = 0;
			int len;
			for(;;) {
				int c = CompareCell(i, ref key, out len);
				if (c != 0)
					return c;
				if (len <= 3) {
					lengthMatched += len;
					Debug.Assert(lengthMatched == key.Left);
					return 0;
				}
				lengthMatched += 3;
				i = _partitionMinus4 + len;
			}
		}
		int NextCell(int i)
		{
			return (int)_cells[i];
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
			}
		}

		private bool ItIsTimeToBurst(int keySize)
		{
			return _count == 32 || ((_cells.Length >> 2) - ExtraCellsFree)*3 + keySize > 255*3;
		}
		private JPNode<T> Burst()
		{
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
