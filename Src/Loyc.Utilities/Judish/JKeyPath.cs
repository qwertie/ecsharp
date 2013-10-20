using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities.Judish
{
	/// <summary>
	/// JKeyPath is used to keep track of the "path" to a node and a key that,
	/// when searched for, causes that path to be followed. JKeyPath is used
	/// during enumeration, and when searching for the nearest key to some key 
	/// that is not (necessarily) in the collection.
	/// </summary><remarks>
	/// JKeyPath implements JKeyEnumerator so that it can be used by an 
	/// implementation of JKeySerializer.Deserialize to convert the key path 
	/// into a key. Typically it is used in two phases:
	/// </remarks>
	internal class JKeyPath : JKeyEnumerator
	{
		JKeyPath() { _path = new InternalList<JKeyPart>(0); }

		InternalList<JKeyPart> _path;
		object _value;
		int _pathIndex;    // used during key enumeration (GetBytes)
		byte _keyPartLeft; // used during key enumeration (GetBytes)
		ushort _keyLength; // Length of the key described by _path

		#region Stuff used by nodes

		private void Push(JKeyPart part)
		{
			_path.Add(part);
			_keyLength += part.KeyPartSize;
		}
		private void Pop()
		{
			_keyLength -= Top.KeyPartSize;
			_path.RemoveLast();
		}
		private int TopIndex
		{
			get { return _path.Count - 1; }
		}
		private JKeyPart Top
		{
			get { return _path[_path.Count - 1]; }
			set {
				Debug.Assert(value.Node == Top.Node);
				_path[_path.Count - 1] = value;
			}
		}

		#endregion

		#region Enumeration methods

		public void MoveFirst(NodeBase root)
		{
			_path.Clear();
			_keyLength = 0;
			_value = null;

			NodeBase child = root;
			do {
				_path.Count++;
				_value = child.MoveFirst(out _path.InternalArray[TopIndex]);
				child = _value as NodeBase;
			} while (child != null);
		}
		public void MoveLast(NodeBase root)
		{
			_path.Clear();
			_keyLength = 0;
			_value = null;

			NodeBase child = root;
			do {
				_path.Count++;
				_value = child.MoveLast(out _path.InternalArray[TopIndex]);
				child = _value as NodeBase;
			} while (child != null);
		}
		public bool MoveNext()
		{
			for (;;) {
				_value = Top.Node.MoveNext(ref _path.InternalArray[TopIndex]);
				if (_value != NodeBase.NotFound)
					break;
				Pop();
				if (_path.IsEmpty)
				{
					Debug.Assert(_keyLength == 0);
					return false;
				}
			}
			var child = _value as NodeBase;
			while (child != null)
			{
				_path.Count++;
				_value = child.MoveFirst(out _path.InternalArray[TopIndex]);
				child = _value as NodeBase;
			}
			Debug.Assert(_value != NodeBase.NotFound);
			return true;
		}
		public bool MovePrev()
		{
			for (;;) {
				_value = Top.Node.MovePrev(ref _path.InternalArray[TopIndex]);
				if (_value != NodeBase.NotFound)
					break;
				Pop();
				if (_path.IsEmpty)
				{
					Debug.Assert(_keyLength == 0);
					return false;
				}
			}
			var child = _value as NodeBase;
			while (child != null)
			{
				_path.Count++;
				_value = child.MoveLast(out _path.InternalArray[TopIndex]);
				child = _value as NodeBase;
			}
			Debug.Assert(_value != NodeBase.NotFound);
			return true;
		}

		#endregion

		public object CurrentValue { get { return _value; } }

		public int BeginKey()
		{
			_pathIndex = 0;
			_keyPartLeft = _path[0].KeyPartSize;
			return _keyLength;
		}

		public override uint GetBytes(int numBytes)
		{
			JKeyPart[] path = _path.InternalArray;
			uint keyPart = path[_pathIndex].KeyPart;
			if (_keyPartLeft == numBytes)
			{
				if (++_pathIndex < path.Length)
					_keyPartLeft = path[_pathIndex].KeyPartSize;
				if (numBytes == 4)
					return keyPart;
				else
					return keyPart & ((1u << (numBytes << 3)) - 1u);
			}
			else
			{
				int left = --_keyPartLeft;
				uint output = (byte)(keyPart >> (left << 3));
				for (;;)
				{
					if (left <= 0)
					{
						Debug.Assert(left == 0);
						if (++_pathIndex >= _path.Count) {
							Debug.Assert(numBytes == 1);
							return output;
						}
						keyPart = path[_pathIndex].KeyPart;
						_keyPartLeft = path[_pathIndex].KeyPartSize;
					}
					if (--numBytes == 0)
						return output;

					left = --_keyPartLeft;
					output = (output << 8) | (byte)(keyPart >> (left << 3));
				}
			}
		}
	}

	/// <summary>Represents one node and part of a key in a JKeyPath.</summary>
	public struct JKeyPart
	{
		public JKeyPart(NodeBase node, int keyPartSize, uint keyPartSHR)
			: this(node, keyPartSize, keyPartSHR, 0) {}
		public JKeyPart(NodeBase node, int keyPartSize, uint keyPartSHR, int IWN)
		{
			Node = node;
			KeyPartSize = (byte)keyPartSize;
			KeyPart = keyPartSHR;
			IndexWithinNode = (short)IWN;
			Debug.Assert(IndexWithinNode == IWN);
		}
		public NodeBase Node;
		public uint KeyPart;     // if less than 4 bytes, high-order bits are 0
		public byte KeyPartSize; // 0 to 4
		public short IndexWithinNode; // used by the node class
		
		public uint KeyPartSHL
		{
			get { return KeyPart << ((4 - KeyPartSize) << 3); }
		}
	}
}
