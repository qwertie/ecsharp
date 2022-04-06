// http://www.codeproject.com/KB/recipes/cptrie.aspx
namespace Loyc.Collections.Impl
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Loyc.Collections;
	using Loyc.Utilities;
	using System.Diagnostics;
	using Loyc.Math;

	internal class CPBitArrayLeaf<T> : CPNode<T>
	{
		// _flags[0..7] is a bitmap of keys that have been assigned.
		// _flags[8..15] is a bitmap of value slots that have been allocated,
		//               e.g. if _flags[9]=1, then _values[32] is allocated.
		uint[] _flags = new uint[16];
		// 8 groups of 32 bytes associating keys with value slots, e.g. if
		// _indices[1][0] == 3, then _values[3] is the value for key #32. Bytes in
		// this array have no meaning if the corresponding key is not allocated. An
		// index of 0xFF indicates null, unless _values.Length reaches 256.
		byte[][] _indices;
		T[] _values;
		short _localCount;
		short _valueCount;

		public CPBitArrayLeaf() { }
		public CPBitArrayLeaf(CPBitArrayLeaf<T> clone)
		{
			_flags = InternalList.CopyToNewArray(clone._flags);
			if (clone._indices != null) {
				_indices = InternalList.CopyToNewArray(clone._indices);
				for (int i = 0; i < _indices.Length; i++)
					_indices[i] = InternalList.CopyToNewArray(_indices[i]);
			}
			// TODO: shrink _values array
			_values = InternalList.CopyToNewArray(_values);
			_localCount = clone._localCount;
			_valueCount = clone._valueCount;
		}

		bool IsPresent(int k)
		{
			return (_flags[k >> 5] & (1 << (k & 0x1F))) != 0;
		}
		int GetValueIndex(int k)
		{
			if (_indices != null && _indices[k >> 5] != null)
				return _indices[k >> 5][k & 0x1F];
			return 0x100;
		}
		T GetValueAt(int k)
		{
			uint P;
			if (_indices != null && _indices[k >> 5] != null && (P = _indices[k >> 5][k & 0x1F]) < (uint)_values.Length)
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
			return k + MathEx.PositionOfLeastSignificantOne(f >> (k & 0x1F));
		}

		private int FindPrevInUse(int k)
		{
			uint f = _flags[k >> 5] & ((1u << (k & 0x1F)) - 1u);
			if (f != 0)
				return (k & ~0x1F) | MathEx.PositionOfMostSignificantOne(f);

			for (int section = (k >> 5) - 1; section >= 0; section--)
			{
				if (_flags[section] != 0)
					return (section << 5) | MathEx.PositionOfMostSignificantOne(_flags[section]);
			}
			return -1;
		}

		private void ExtractCurrent(CPEnumerator<T> e, byte k)
		{
			Debug.Assert(IsPresent(k));

			byte[] buf = e.Key.Buffer;
			int offs = e.Key.Offset;
			if (buf.Length == offs)
			{
				// out of buffer space in e.Key, only need one more byte!
				buf = InternalList.CopyToNewArray(buf, offs, offs + 1);
			}
			buf[offs] = k;
			e.Key.Reset(buf, offs, 1);
			e.CurrentValue = GetValueAt(k);
		}

		public override bool Set(ref KeyWalker key, ref T value, ref CPNode<T> self, CPMode mode)
		{
			byte k = key.Buffer[key.Offset];
			if (key.Left == 1 && IsPresent(k))
			{
				T newValue = value;
				int P = GetValueIndex(k);
				if (P < 0x100 && P < _values.Length) {
					value = _values[P];
					if ((mode & CPMode.Set) != (CPMode)0)
						_values[P] = newValue;
				} else {
					value = default(T);
					if ((mode & CPMode.Set) != (CPMode)0)
						Assign(k, newValue);
				}
				return true;
			}
			else if ((mode & CPMode.Create) != (CPMode)0)
			{
				if (key.Left == 1)
				{
					Assign(k, value);
					_localCount++;
				}
				else
				{
					// Must convert back to bitmap or sparse node!
					ConvertToBOrSNode(ref self, key.Left / 3 + 1);
					self.Set(ref key, ref value, ref self, mode);
				}
			}
			return false;
		}

		private void ConvertToBOrSNode(ref CPNode<T> self, int extraCells)
		{
			if (_localCount < 32)
				self = new CPSNode<T>(_localCount + extraCells);
			else
				self = new CPBNode<T>();

			// Scan key-value pairs in this node
			KeyWalker kw = new KeyWalker(new byte[1], 1);
			for (int section = 0; section < 8; section++) {
				uint f = _flags[section];
				if (f == 0)
					continue;
				for (int i = MathEx.PositionOfLeastSignificantOne(f); i < 32; i++) {
					if ((f & (1 << i)) != 0) // IsPresent(k)
					{
						// Get the key and value
						int k = (section << 5) + i;
						kw.Buffer[0] = (byte)k;

						T value = default(T);
						if (_values != null) {
							int P = GetValueIndex(k);
							if (P < _values.Length)
								value = _values[P];
						}

						// Assign them to the new node
						bool existed = self.Set(ref kw, ref value, ref self, CPMode.Create | CPMode.FixedStructure);
						Debug.Assert(!existed);
						kw.Reset();
					}
				}
			}
		}

		private void Assign(byte k, T value)
		{
			if (value == null && (_values == null || _values.Length <= 0xFF))
				AssociateValue(k, 0xFF);
			else
				AssociateValue(k, (byte)AllocValue(value));
			_flags[k >> 5] |= 1u << (k & 0x1F);
		}

		private int AllocValueSlot()
		{
			for (int i = 8; i < _flags.Length; i++)
			{
				if (_flags[i] != 0xFFFFFFFF)
				{
					int fz = MathEx.PositionOfLeastSignificantZero(_flags[i]);
					_flags[i] |= (1u << fz);
					_valueCount++;
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
				_values = InternalList.CopyToNewArray(_values, _values.Length, 
				                             Math.Min(_values.Length << 1, 256));
			}
			_values[slot] = value;
			return slot;
		}
		private void AssociateValue(int k, byte P)
		{
			int section = k >> 5;
			if (_indices == null) {
				if (P == 0xFF)
					return;
				_indices = new byte[8][];
			}
			if (_indices[section] == null) {
				if (P == 0xFF)
					return;
				byte[] sec = _indices[section] = new byte[32];
				if (_flags[section] != 0) {
					// One or more null values already exist in this section, even
					// though there was no array for it in _indices. Here, we must
					// be careful to init such null entries properly. Normally it
					// suffices to init all bytes to 0xFF, unless _values.Length is
					// 256 (very rare).
					for (int i = 0; i < sec.Length; i++) {
						sec[i] = 0xFF;
						if (_values.Length == 256 && IsPresent((section << 5) + i))
							sec[i] = (byte)AllocValue(default(T));
					}
				}
			}
			_indices[section][k & 0x1F] = P;
		}

		public override void AddChild(ref KeyWalker key, CPNode<T> value, ref CPNode<T> self)
		{
			// Must convert back to bitmap or sparse node!
			ConvertToBOrSNode(ref self, key.Left / 3 + 1);
			self.AddChild(ref key, value, ref self);
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref CPNode<T> self)
		{
			if (key.Left != 1)
				return false;
			byte k = key.Buffer[key.Offset];
			if (!IsPresent(k))
				return false;

			if (_values != null) {
				int P = GetValueIndex(k);
				if (P < _values.Length) {
					oldValue = _values[P];
					FreeValueSlot(P);
				}
			} else
				Debug.Assert(_indices == null);

			_localCount--;
			_flags[k >> 5] &= ~(1u << (k & 0x1F));
			if (_localCount < 24 && (_valueCount > 0 || _localCount < 12))
				ConvertToBOrSNode(ref self, 0);
			return true;
		}
		private void FreeValueSlot(int P)
		{
			int i = 8 + (P >> 5);
			Debug.Assert((_flags[i] & (1u << (P & 0x1F))) != 0);
			Debug.Assert(P < _values.Length);
			_flags[i] &= (uint)~(1u << (P & 0x1F));
			_valueCount--;
			_values[P] = default(T);
			if (_valueCount == 0) {
				_indices = null;
				_values = null;
			}
		}

		public override int CountMemoryUsage(int sizeOfT)
		{
			// assumes a 32-bit architecture
			int size = (6 + 3 + _flags.Length) * 4;
			if (_values != null) {
				size += 3 * 4 + _values.Length * sizeOfT;
				// TODO: size is 4 bytes more if T is a reference type; detect
			}
			if (_indices != null) {
				size += (4 + _indices.Length) * 4;
				for (int i = 0; i < _indices.Length; i++) {
					if (_indices[i] != null)
						size += (3 + _indices[i].Length) * 4;
				}
			}
			return size;
		}

		public override CPNode<T> CloneAndOptimize()
		{
			return new CPBitArrayLeaf<T>(this);
		}

		public override int LocalCount
		{
			get { return _localCount; }
		}

		/// (1) Add()s an Entry to e.Stack pointing to the first (lowest) item in 
		///     the node, using e.Key.Offset as the value of Entry.KeyOffset;
		/// (2) extracts the key to e.Key such that e.Key.Offset+e.Key.Left is the
		///     length of the complete key (if e.Key.Buffer is too small, it is
		///     copied to a larger buffer as needed);
		/// (3) if the current item points to a child, this method advances e.Key 
		///     to the end of the key so that e.Key.Left is 0, and calls MoveFirst 
		///     on the child;
		/// (4) otherwise, this method leaves e.Key.Offset equal to
		///     e.Stack.Last.KeyOffset, so that e.Key.Left is the number of bytes
		///     of the key that are stored in this node.
		public override void MoveFirst(CPEnumerator<T> e)
		{
			int k = FirstKeyInUse();
			// No need to store the "index" on the stack;
			// just store the current key in e.Key.
			e.Stack.Add(new CPEnumerator<T>.Entry(this, 0, e.Key.Offset));
			ExtractCurrent(e, (byte)k);
		}
		int FirstKeyInUse()
		{
			for (int section = 0; ; section++) {
				Debug.Assert(section < 8);
				uint f = _flags[section];
				if (f != 0)
					return MathEx.PositionOfLeastSignificantOne(f) + (section << 5);
			}
		}

		public override void MoveLast(CPEnumerator<T> e)
		{
			int k = LastKeyInUse();
			e.Stack.Add(new CPEnumerator<T>.Entry(this, 0, e.Key.Offset));
			ExtractCurrent(e, (byte)k);
		}
		int LastKeyInUse()
		{
			for (int section = 7; ; section--) {
				Debug.Assert(section >= 0);
				uint f = _flags[section];
				if (f != 0)
					return MathEx.PositionOfLeastSignificantOne(f) + (section << 5);
			}
		}

		public override bool MoveNext(CPEnumerator<T> e)
		{
			int k = e.Key[0];
			if ((k = FindNextInUse(k)) == -1)
				return false;
			ExtractCurrent(e, (byte)k);
			return true;
		}

		public override bool MovePrev(CPEnumerator<T> e)
		{
			int k = e.Key[0];
			if ((k = FindPrevInUse(k)) == -1)
				return false;
			ExtractCurrent(e, (byte)k);
			return true;
		}
	}
}
