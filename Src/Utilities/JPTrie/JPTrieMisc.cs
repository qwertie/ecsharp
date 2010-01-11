using System;
using System.Diagnostics;

namespace Loyc.Utilities.JPTrie
{
	public class JPEnumerator
	{
		internal void Normalize()
		{
			throw new NotImplementedException();
		}
	}

	[Flags]
	public enum JPMode {
		Create = 1, // Create if key doesn't exist
		Set = 2,    // Change if key already exists
		Find = 0,   // Neither create nor change existing value
	}

	public struct KeyWalker
	{
		byte[] _key;
		int _left;
		int _offset;

		public KeyWalker(byte[] key, int left)
		{
			_key = key;
			_left = left;
			_offset = 0;
			Debug.Assert(_left >= 0 && _left <= _key.Length);
		}
		public byte this[int index]
		{
			get {
				Debug.Assert(index < _left);
				return _key[_offset + index];
			}
			set {
				_key[_offset + index] = value;
			}
		}

		public int Left { get { return _left; } }
		public int Offset
		{ 
			get { return _offset; }
			set { 
				int len = _offset + _left;
				Debug.Assert(value >= 0 && value <= len);
				_offset = value;
				_left = len - _offset;
			}
		}

		public void Advance(int amount)
		{
			Debug.Assert(amount >= -_offset && amount <= _left);
			_left -= amount;
			_offset += amount;
		}
		public void Reset()
		{
			_left += _offset;
			_offset = 0;
		}
		
		#if DEBUG // For display in the debugger
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < _offset; i++)
				sb.Append((char)_key[i]);
			sb.Append('|');
			for (int i = 0; i < _left; i++)
				sb.Append((char)_key[_offset + i]);
			return sb.ToString();
		}
		#endif
	}

	abstract class JPNode<T>
	{
		// Returns true if key exists
		public abstract bool Find(ref KeyWalker key, JPEnumerator e);

		// Returns true if key already existed. Can be used to find rather than 
		// create or set a value (mode==JPMode.Find), if the caller just wants 
		// the value and not an enumerator.
		public abstract bool Set(ref KeyWalker key, ref T value, ref JPNode<T> self, JPMode mode);

		// Returns true if key formerly existed
		public abstract bool Remove(ref KeyWalker key, ref T oldValue, ref JPNode<T> self);
	}
}