using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities.CPTrie;
using Loyc.Runtime;
using System.Diagnostics;

namespace Loyc.Utilities.CPTrie
{
	class CPBitArrayLeaf<T> : CPNode<T>
	{
		// _flags[0..7] is a bitmap of keys that have been assigned.
		// _flags[8..15] is a bitmap of value slots that have been allocated.
		uint[] _flags = new uint[16];
		byte[][] _indices; // 8 groups of 32 bytes
		T[] _values;

		bool IsPresent(int k)
		{
			return (_flags[k >> 5] & (1 << (k & 0x1F))) != 0;
		}
		int GetValueIndex(int k)
		{
			if (_indices != null && _indices[k >> 3] != null)
				return _indices[k >> 3][k & 0x1F];
			return 0xFF;
		}
		T GetValueAt(int k)
		{
			uint P;
			if (_indices != null && _indices[k >> 3] != null && (P = _indices[k >> 3][k & 0x1F]) < (uint)_values.Length)
				return _values[P];
			return default(T);
		}

		public override bool Find(ref KeyWalker key, CPEnumerator<T> e)
		{
			if (key.Left == 0) {
				MoveFirst(e);
				return false;
			}
			byte k = key.Buffer[key.Offset];
			if (key.Left == 1 && IsPresent(k)) {
				e.Stack.Add(new CPEnumerator<T>.Entry(this, k, e.Key.Offset));
				ExtractCurrent(e, k);
				return true;
			} else {
				int nextK = FindNextInUse(k);
				if (nextK >= 0)
				{
					e.Stack.Add(new CPEnumerator<T>.Entry(this, nextK, e.Key.Offset));
					ExtractCurrent(e, (byte)nextK);
				}
				else
				{	// code duplicated from CPSNode
					if (!e.Stack.IsEmpty)
					{
						e.Key.Reset(e.Stack.Last.KeyOffset);
						e.MoveNext();
					}
				}
				return false;
			}
		}

		private int FindNextInUse(int k)
		{
			++k;
			uint f;
			for(;;) {
				if (k == 0x100)
					return -1;
				f = _flags[k >> 5];
				if ((f & (-1 << (k & 0x1F))) != 0)
					break;
				// Move to next section
				k = (k & ~0x1F) + 0x20;
			}
			return k + G.FindFirstOne(f >> (k & 0x1F));
		}

		private void ExtractCurrent(CPEnumerator<T> e, byte k)
		{
			Debug.Assert(IsPresent(k));

			byte[] buf = e.Key.Buffer;
			int offs = e.Key.Offset;
			if (buf.Length == offs)
			{
				// out of buffer space in e.Key, only need one more byte!
				buf = InternalList<byte>.CopyToNewArray(buf, offs, offs + 1);
			}
			buf[offs] = k;
			e.CurrentValue = GetValueAt(k);
		}

		public override bool Set(ref KeyWalker key, ref T value, ref CPNode<T> self, CPMode mode)
		{
			byte k = key.Buffer[key.Offset];
			if (key.Left == 1 && IsPresent(k))
			{
				T newValue = value;
				int P = GetValueIndex(k);
				if (P < _values.Length) {
					value = _values[P];
					if ((mode & CPMode.Set) != (CPMode)0)
						_values[P] = newValue;
				} else {
					value = default(T);
					if ((mode & CPMode.Set) != (CPMode)0)
						Create(k, newValue);
				}
				return true;
			}
			else if ((mode & CPMode.Create) != (CPMode)0)
			{
				if (key.Left == 1)
				{
					Create(k, value);
				}
				else
				{
					// Must convert back to bitmap or sparse node!
					throw new NotImplementedException();
				}
			}
			return false;
		}

		private void Create(byte k, T value)
		{
			_flags[k >> 5] |= (uint)k & 0x1F;
			if (value != null || (_values != null && _values.Length == 256))
				AssociateValue(k, AllocValue(value));
		}

		private int AllocValueSlot()
		{
			for (int i = 8; i < _flags.Length; i++)
			{
				if (_flags[i] != 0xFFFFFFFF)
				{
					int fz = G.FindFirstZero(_flags[i]);
					_flags[i] |= (1u << fz);
					return ((i - 8) << 5) + fz;
				}
			}
			throw new InvalidProgramException("bug");
		}
		private int AllocValue(T value)
		{
			int slot = AllocValueSlot();
			if (_values == null) {
				Debug.Assert(slot == 0);
				_values = new T[4];
			} else if (slot >= _values.Length) {
				_values = InternalList<T>.CopyToNewArray(_values, _values.Length, _values.Length << 1);
			}
			_values[slot] = value;
			return slot;
		}
		private void AssociateValue(byte k, byte P)
		{
			int section = k >> 5;
			if (_indices == null)
				_indices = new byte[8][];
			if (_indices[section] == null)
				_indices[section] = new byte[32];
			_indices[section][k & 0x1F] = (byte)P;
		}

		public override void AddChild(ref KeyWalker key, CPNode<T> value, ref CPNode<T> self)
		{
			throw new NotImplementedException();
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref CPNode<T> self)
		{
			throw new NotImplementedException();
		}

		public override int CountMemoryUsage(int sizeOfT)
		{
			throw new NotImplementedException();
		}

		public override CPNode<T> CloneAndOptimize()
		{
			throw new NotImplementedException();
		}

		public override int LocalCount
		{
			get { throw new NotImplementedException(); }
		}

		public override void MoveFirst(CPEnumerator<T> e)
		{
			throw new NotImplementedException();
		}

		public override void MoveLast(CPEnumerator<T> e)
		{
			throw new NotImplementedException();
		}

		public override bool MoveNext(CPEnumerator<T> e)
		{
			throw new NotImplementedException();
		}

		public override bool MovePrev(CPEnumerator<T> e)
		{
			throw new NotImplementedException();
		}
	}
}
