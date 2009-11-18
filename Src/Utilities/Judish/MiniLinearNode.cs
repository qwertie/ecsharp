using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish.Internal
{
	/// <summary>Contains code shared between the different kinds of linear node.</summary>
	internal class LinearNode : NodeBase
	{
		/// <summary>Assigns 'value' to the slot 'v'; advances q.Key by advanceBy.</summary>
		/// <param name="v">Current value of the slot. If v is a NodeBase (child) 
		/// then the value is assigned in the child node, otherwise we normally
		/// assign the value to v itself--unless the key is longer than 4 bytes,
		/// in which case this method sets v to a new MiniLinearNode, which is 
		/// used to hold the remainder of the key.</param>
		/// <param name="value">Value to assign</param>
		/// <returns>Old value associated with the key, or NotFound if none</returns>
		protected object SetExisting(ref QueryState q, ref object v, object value, int advanceBy)
		{
			Debug.Assert(value != NotFound);
			q.Key.Advance(advanceBy);

			var oldValue = v;
			var child = v as NodeBase;
			if (child != null)
			{
				q.Parent = this;
				oldValue = child.Set(ref q, value, out v);
			}
			else if (q.Key.BytesLeft > 0)
			{
				// Special case: the existing key conflicts with this new, longer 
				// key. We'll need a child MiniLinearNode to represent both keys.
				Debug.Assert(advanceBy == 4);
				var mini = new MiniLinearNode(0, 0, oldValue);
				object dummy;
				q.Parent = this;
				oldValue = mini.Set(ref q, value, out dummy);
				Debug.Assert(dummy == mini);
				Debug.Assert(oldValue == NotFound);
				v = mini;
			}
			else
				v = value;
			
			return oldValue;
		}

		/// <summary>After the derived class determines that q.Key.KeyPart does not
		/// already exist in the node, it reserves a new slot and calls this method.
		/// This method populates k, kLength and v according to the key (q.Key) and 
		/// value supplied, and it returns NotFound.
		/// </summary>
		/// <returns>Returns NotFound.</returns>
		protected object SetNew(ref QueryState q, object value, out uint k, out byte kLength, out object v)
		{
			Debug.Assert(value != NotFound);
			k = q.Key.KeyPart;

			if (q.Key.BytesLeft <= 4)
			{
				kLength = (byte)q.Key.BytesLeft;
				v = value;
				return NotFound;
			}
			else
			{	// Need child node for very long key
				kLength = 4;
				var mini = new MiniLinearNode();
				v = mini;
				q.Key.Advance(4);
				object dummy;
				return mini.Set(ref q, value, out dummy);
			}
		}

		/// <summary>Completes a Get operation after the caller found a match.</summary>
		/// <remarks>
		/// There are three cases:
		/// (1) value is a NodeBase -> search the child for the subkey
		/// (2) the input key is too long -> return NotFound
		/// (3) default -> return value
		/// </remarks>
		protected object FinishGet(ref QueryState q, object value, int advanceBy)
		{
			Debug.Assert(advanceBy <= q.Key.BytesLeft);

			var child = value as NodeBase;
			if (child != null) {
				q.Key.Advance(advanceBy);
				return child.Get(ref q);
			} else if (q.Key.BytesLeft > advanceBy) {
				Debug.Assert(advanceBy == 4);
				return NotFound;
			} else
				return value;
		}

		/// <summary>Figures out whether a given search key prefix (keyPart with 
		/// length keyLength) matches a key in the node (k with length kLength).
		/// Both keys must be left-shifted so that the first byte of the key is 
		/// in the most significant byte.</summary>
		/// <remarks>
		/// If keyLength is less than kLength, there is surely not a match. However,
		/// if keyLength is more than kLength, there might be a match if there is
		/// a child node in the current slot, so this method returns true in that 
		/// case (provided that the first kLength bytes match); the caller must 
		/// check for that case.
		/// </remarks>
		protected bool IsPrefixMatch(uint keyPart, int keyLength, uint k, int kLength)
		{
			Debug.Assert(kLength <= 3);
			if (kLength > 3)
				return k == keyPart && keyLength > 3;
			else if (keyLength < kLength)
				return false;
			else {
				int shift = (4 - kLength) << 3;
				return keyPart >> shift == k >> shift;
			}
		}
	}

	internal sealed class MiniLinearNode : LinearNode
	{
		// This node stores its entries in no particular order.
		// When enumerating we must sort on-the-fly.
		uint k1, k2, k3;
		byte kLength1 = 0xFF, kLength2 = 0xFF, kLength3 = 0xFF; // 0xFF if unused
		byte NumKeys;
		object v1 = NotFound, v2 = NotFound, v3 = NotFound; // NotFound if unused

		public MiniLinearNode() { }

		public MiniLinearNode(uint firstK, int firstKeyLength, object firstValue)
		{
			Debug.Assert((uint)firstKeyLength <= 4u);
			k1 = firstK;
			kLength1 = (byte)firstKeyLength;
			v1 = firstValue;
		}

		public override object Get(ref QueryState q)
		{
			object value;
			int keyLength = Math.Min(q.Key.BytesLeft, 4);
			uint keyPart = q.Key.KeyPart;
			if (keyPart == k1 && kLength1 == keyLength)
				value = v1;
			else if (keyPart == k2 && kLength2 == keyLength)
				value = v2;
			else if (keyPart == k3 && kLength3 == keyLength)
				value = v3;
			else
			{	// No exact match; look for prefix matches
				if (v3 is NodeBase && IsPrefixMatch(keyPart, keyLength, k3, keyLength = kLength3))
					value = v3;
				else if (v2 is NodeBase && IsPrefixMatch(keyPart, keyLength, k2, keyLength = kLength2))
					value = v2;
				else if (v1 is NodeBase && IsPrefixMatch(keyPart, keyLength, k1, keyLength = kLength1))
					value = v1;
				else
					return NotFound;
			}

			return FinishGet(ref q, value, keyLength);
		}

		public override object Set(ref QueryState q, object value, out object thisNode)
		{
			int keyLength = Math.Min(q.Key.BytesLeft, 4);
			uint keyPart = q.Key.KeyPart;
			if (IsPrefixMatch(keyPart, keyLength, k1, kLength1) && (keyLength == kLength1 || v1 is NodeBase)) {
				thisNode = this;
				return SetExisting(ref q, ref v1, value, kLength1);
			} else if (IsPrefixMatch(keyPart, keyLength, k2, kLength2) && (keyLength == kLength2 || v2 is NodeBase)) {
				thisNode = this;
				return SetExisting(ref q, ref v2, value, kLength2);
			} else if (IsPrefixMatch(keyPart, keyLength, k3, kLength3) && (keyLength == kLength3 || v3 is NodeBase)) {
				thisNode = this;
				return SetExisting(ref q, ref v3, value, kLength3);
			}
			
			// No existing row matches. Put the value in a new slot.
			thisNode = this;
			if (v1 == NotFound)
				return SetNew(ref q, value, out k1, out kLength1, out v1);
			else if (v2 == NotFound)
				return SetNew(ref q, value, out k2, out kLength2, out v2);
			else if (v3 == NotFound)
				return SetNew(ref q, value, out k3, out kLength3, out v3);
			else {
				// The node is full; convert it to a larger type
				var replacement = new LongLinearNode(this);
				return replacement.Set(ref q, value, out thisNode);
			}
		}

		public override object Remove(ref QueryState q, out object thisNode)
		{
			int keyLength = Math.Min(q.Key.BytesLeft, 4);
			uint keyPart = q.Key.KeyPart;
			if (IsPrefixMatch(keyPart, keyLength, k1, kLength1) && (keyLength == kLength1 || v1 is NodeBase)) {
				return RemoveExisting(ref q, ref v1, ref kLength1, out thisNode);
			} else if (IsPrefixMatch(keyPart, keyLength, k2, kLength2) && (keyLength == kLength2 || v2 is NodeBase)) {
				return RemoveExisting(ref q, ref v2, ref kLength2, out thisNode);
			} else if (IsPrefixMatch(keyPart, keyLength, k3, kLength3) && (keyLength == kLength3 || v3 is NodeBase)) {
				return RemoveExisting(ref q, ref v3, ref kLength3, out thisNode);
			} else {
				thisNode = this;
				return NotFound;
			}
		}

		private object RemoveExisting(ref QueryState q, ref object v, ref byte kLength, out object thisNode)
		{
			thisNode = this; // by default

			object oldValue;
			var child = v as NodeBase;
			if (child != null)
			{
				q.Key.Advance(kLength);
				q.Parent = this;
				oldValue = child.Remove(ref q, out v);
			}
			else if (q.Key.BytesLeft > kLength)
			{
				return NotFound; // A longer key does not exist.
			}
			else
			{
				oldValue = v;
				v = NotFound;
			}

			if (v == NotFound)
			{
				kLength = 0xFF;
				if (v1 == NotFound && v2 == NotFound && v3 == NotFound)
					// The node is empty! Delete ourself!
					thisNode = NotFound;
			}
			return oldValue;
		}

		struct KeyPrefix
		{
			public KeyPrefix(uint k, int kLength, byte index) 
				{ this.k = k; this.kLength = (byte)kLength; }
			public uint k;
			public byte kLength;
			public byte index;
			
			public bool IsLessThan(KeyPrefix other)
			{
				return k <= other.k && (k < other.k || kLength < other.kLength);
			}
		}

		public override object MoveFirst(out JKeyPart path)
		{
			path = new JKeyPart();
			path.Node = this;
			path.IndexWithinNode = (short)((ComputeOrder() << 6) | 4);
			return MoveNext(ref path);
		}

		public override object MoveLast(out JKeyPart path)
		{
			path = new JKeyPart();
			path.Node = this;
			path.IndexWithinNode = (short)((ComputeOrder() << 6) | (6 + NumKeys));
			Debug.Assert((path.IndexWithinNode >> (path.IndexWithinNode & 0xF) & 3) == 0);
			return MovePrev(ref path);
		}

		private short ComputeOrder()
		{
			KeyPrefix _1 = new KeyPrefix(k1, kLength1, 1);
			KeyPrefix _2 = new KeyPrefix(k2, kLength2, 2);
			KeyPrefix _3 = new KeyPrefix(k3, kLength3, 3);

			// Put the keys in order (it doesn't matter if one or two are unused)
			if (_2.IsLessThan(_1))
			{
				if (_3.IsLessThan(_2))
					G.Swap(ref _1, ref _3);
				else
					G.Swap(ref _1, ref _2);
			}
			else if (_3.IsLessThan(_1))
				G.Swap(ref _1, ref _3);
			if (_2.IsLessThan(_3))
				G.Swap(ref _2, ref _3);

			Debug.Assert(_1.IsLessThan(_2) && _2.IsLessThan(_3));

			// Encode the order into a short, 2 bits per index
			int order = 0;
			if (_1.kLength != 0xFF)
				order = _1.index;
			if (_2.kLength != 0xFF)
				order = (order << 2) | _2.index;
			if (_3.kLength != 0xFF)
				order = (order << 2) | _3.index;
			return (short)order;
		}

		public override object MoveNext(ref JKeyPart path)
		{
			int iwn = path.IndexWithinNode + 2;
			int index = (iwn >> (iwn & 0xF)) & 0x3;
			if (index == 0)
				return NotFound;
			path.IndexWithinNode = (short)iwn;
			return GetAt(index);
		}

		public override object MovePrev(ref JKeyPart path)
		{
			int iwn = path.IndexWithinNode - 2;
			int index = (iwn >> (iwn & 0xF)) & 0x3;
			if (index == 0)
				return NotFound;
			path.IndexWithinNode = (short)iwn;
			return GetAt(index);
		}

		public object GetAt(int index)
		{
			if (index == 1)
				return v1;
			else if (index == 2)
				return v2;
			else if (index == 3)
				return v3;
			else
				return NotFound;
		}

		public override int LocalCount
		{
			get { return NumKeys; }
		}

		public override NodeBase Clone()
		{
			var copy = (MiniLinearNode)MemberwiseClone();
			var n1 = copy.v1 as NodeBase;
			var n2 = copy.v2 as NodeBase;
			var n3 = copy.v3 as NodeBase;
			if (n1 != null) copy.v1 = n1.Clone();
			if (n2 != null) copy.v2 = n2.Clone();
			if (n3 != null) copy.v3 = n3.Clone();
			return copy;
		}

		public override void CheckValidity(bool recursive)
		{
			throw new NotImplementedException();
		}
	}
}
