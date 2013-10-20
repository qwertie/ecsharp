using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish.Internal
{
	/// <summary>
	/// Stores rows in a packed sorted list of 32 items or less. 
	/// Each key can be 0 to 4 bytes with no restrictions.
	/// </summary>
	/*internal sealed class ShortLinearNode : NodeBase
	{
		public ShortLinearNode()
		{
			_r = new InternalList<JRow>(0);
		}
		private ShortLinearNode(ShortLinearNode copy)
		{
			_r = copy._r.Clone();
			_lkf = copy._lkf;
			var r = _r.InternalArray;
			for (int i = 0; i < _r.Count; i++)
			{
				var child = r[i].V as NodeBase;
				if (child != null)
					r[i].V = child.Clone();
			}
		}

		// Sorted by key (not necessarily sorted by JRow.K)
		InternalList<JRow> _r;
		uint _lkf;

		public override object Get(ref QueryState q)
		{
			int i;
			if (!Find(ref q, out i))
				return NotFound;

			var value = _r[i].V;
			var child = value as NodeBase;
			if (child != null)
				return child.Get(ref q);
			if (q.Key.BytesLeft > 0)
				return NotFound;
			else
				return value;
		}

		public override object Set(ref QueryState q, object value, out object thisNode)
		{
			int i;
			if (Find(ref q, out i))
			{
				var oldValue = _r[i].V;
				var child = oldValue as NodeBase;
				if (child != null) {
					q.Parent = this;
					oldValue = child.Set(ref q, value, out _r.InternalArray[i].V);
				} else
					_r.InternalArray[i].V = value;
				thisNode = this;
				return oldValue;
			}
			else
			{
				// Key not found
				if (q.Key.BytesLeft > 4) {
					var child = new ShortLinearNode();
					_r.Insert(i, new JRow(q.Key.KeyPart, child));
					q.Key.Advance(4);
					q.Parent = this;
					child.Set(ref q, value, out _r.InternalArray[i].V);
				} else {
					uint mask = (1u << i) - 1u;
					_lkf = (uint)((_lkf & ~mask) << 1) | (_lkf & mask);
					uint k = q.Key.KeyPart;
					if (q.Key.BytesLeft == 4)
						_lkf |= (1u << i);
					else
						k |= (uint)q.Key.BytesLeft;
					_r.Insert(i, new JRow(k, value));
				}
				AutoReform(out thisNode);
				return NotFound;
			}
		}

		private void AutoReform(out object thisNode)
		{
			if (_r.Count < 3)
			{
				
			}
			if (_r.Count > 12)
			{

			}
			thisNode = this;
		}

		public override object Remove(ref QueryState q, out object thisNode)
		{
			int i;
			if (!Find(ref q, out i))
			{
				thisNode = this;
				return NotFound;
			}

			var value = _r[i].V;
			var child = value as NodeBase;
			if (child != null) {
				// Remove from a child node
				q.Parent = this;
				var oldValue = child.Remove(ref q, out thisNode);
				if (thisNode != NotFound)
				{
					_r.InternalArray[i].V = thisNode;
					thisNode = this;
					return oldValue;
				}
			} else if (q.Key.BytesLeft > 0) {
				// Matching key was too short
				thisNode = this;
				return NotFound;
			}
			
			// There is no child node or the child node just became empty, so 
			// delete the entry from this node
			uint mask = (1u << i) - 1u;
			_lkf = ((_lkf >> 1) & ~mask) | (_lkf & mask);
			_r.RemoveAt(i);

			// Delete this node if empty (or if it only contains a ZLK)
			if (_r.Count <= 1) {
				if (_r.IsEmpty)
					thisNode = NotFound;
				else if (_r[0].K == 0 && (_lkf & 1) == 0)
					thisNode = _r[0].V;
				else
					thisNode = this;
			} else
				thisNode = this;
			return value;
		}

		public override object MoveFirst(out JKeyPart part)
		{
			part = JKeyPart(0);
			return _r[0].V;
		}
		public override object MoveLast(out JKeyPart part)
		{
			int i = _r.Count - 1;
			part = JKeyPart(i);
			return _r[i].V;
		}

		bool IsLongKey(int i) { return (_lkf & (1 << i)) != 0; }
		int KeyPartSize(int i)
		{
			if ((_lkf & (1u << i)) != 0)
				return 4;
			else
				return (int)_r[i].K & 3;
		}
		JKeyPart JKeyPart(int i)
		{
			uint k = _r[i].K;
			if ((_lkf & (1u << i)) != 0) {
				return new JKeyPart(this, 4, _r[i].K, i);
			} else {
				return new JKeyPart(this, (int)k & 3, k >> (int)((4 - (k & 3)) << 3), i);
			}
		}

		public override object MoveNext(ref JKeyPart part)
		{
			Debug.Assert(part.Node == this);
			int index = part.IndexWithinNode + 1;
			if (index > _r.Count)
				return NotFound;
			part = JKeyPart(index);
			return _r[index].V;
		}

		public override object MovePrev(ref JKeyPart part)
		{
			Debug.Assert(part.Node == this);
			int index = part.IndexWithinNode - 1;
			if (index < 0)
				return NotFound;
			part = JKeyPart(index);
			return _r[index].V;
		}

		public override int LocalCount
		{
			get { return _r.Count; }
		}

		public override NodeBase Clone()
		{
			return new ShortLinearNode(this);
		}

		public bool Find(ref QueryState q, out int i)
		{
			uint lookFor = q.Key.KeyPart;
			int lookForLength = q.Key.BytesLeft;
			uint lkf = _lkf;
			for (i = 0; ; i++)
			{
				if (i >= _r.Count)
					return false;
				uint k = _r[i].K;
				if ((lkf & 1) != 0) {
					// k is a long key (>= 4 bytes)
					if (k <= lookFor)
					{
						if (k == lookFor && lookForLength >= 4) {
							q.Key.Advance(4);
							return true;
						} else
							return false;
					}
				} else {
					// k is a short key (<= 3 bytes)
					if ((k & ~3u) <= lookFor) {
						int foundLength = (int)k & 3;
						if (lookForLength <= foundLength)
						{
							if ((k & ~3u) == lookFor && lookForLength == foundLength) {
								q.Key.Advance(foundLength);
								return true;
							} else
								return false;
						}
					}
				}
				lkf >>= 1;
			}
		}

		public override void CheckValidity(bool recursive)
		{
			G.Require(_r.Count > 0, "ShortLinearNode is empty");
			
			var prev = JKeyPart(0);
			if (recursive && _r[0].V is NodeBase)
				((NodeBase)_r[0].V).CheckValidity(true);

			for (int i = 1; i < _r.Count; i++)
			{
				var next = JKeyPart(i);
				if (next.KeyPartSHL == prev.KeyPartSHL)
					G.Require(next.KeyPartSize > prev.KeyPartSize, "Key size ordering error in ShortLinearNode");
				else
					G.Require(next.KeyPartSHL > prev.KeyPartSHL, "Key ordering error in ShortLinearNode");
				
				if (recursive && _r[i].V is NodeBase)
					((NodeBase)_r[i].V).CheckValidity(true);

				prev = next;
			}
		}
	}
	*/
}
