using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace Loyc.Utilities.CPTrie
{
	public class CPEnumerator<T> : IEnumerator<T>
	{
		protected internal CPEnumerator(CPTrie<T> trie)
		{
			_trie = trie;
			Stack.Clear();
		}

		public bool MoveNext()
		{
			if (Stack.IsEmpty) {
				_trie.Head.MoveFirst(this);
				return true;
			}
			
			for(;;) {
				if (Stack[Stack.Count-1].Node.MoveNext(this))
					return true;
				else {
					Stack.RemoveAt(Stack.Count - 1);
					if (Stack.IsEmpty)
						return false;
					Key.Reset(Stack[Stack.Count - 1].KeyOffset);
				}
			}
		}
		public bool MovePrev()
		{
			if (Stack.IsEmpty) {
				_trie.Head.MoveLast(this);
				return true;
			}
			
			for(;;) {
				if (Stack[Stack.Count-1].Node.MovePrev(this))
					return true;
				else {
					Stack.RemoveAt(Stack.Count - 1);
					if (Stack.IsEmpty)
						return false;
				}
			}
		}
		public void Reset()
		{
			Stack.Clear();
			CurrentValue = default(T);
		}
		public T Current
		{
			get { return CurrentValue; }
		}
		object System.Collections.IEnumerator.Current
		{
			get { return CurrentValue; }
		}
		public void Dispose() { }

		internal struct Entry
		{
			public Entry(CPNode<T> node, int index, int keyOffset) 
				{ Node = node; Index = index; KeyOffset = keyOffset; }
			public CPNode<T> Node;
			public int Index;
			public int KeyOffset;
		}
		internal InternalList<Entry> Stack;
		internal protected T CurrentValue;
		
		internal KeyWalker Key = new KeyWalker(InternalList<byte>.EmptyArray, 0);
		protected InternalList<byte> CurrentKey
		{
			get { return new InternalList<byte>(Key.Buffer, Key.Offset + Key.Left); }
		}
		protected CPTrie<T> _trie;

		internal void Normalize()
		{
			throw new NotImplementedException();
		}
	}

	[Flags]
	public enum CPMode {
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
		public KeyWalker(byte[] key, int offset, int left)
		{
			_key = key;
			_left = left;
			_offset = offset;
			Debug.Assert(offset >= 0 && left >= 0 && offset + left <= _key.Length);
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

		public int Left { [DebuggerStepThrough] get { return _left; } }
		public int Offset
		{
			[DebuggerStepThrough]
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
		public void Reset(int offset)
		{
			_left += _offset - offset;
			_offset = offset;
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

		public byte[] Buffer { get { return _key; } }
	}

	abstract class CPNode<T>
	{
		// Returns true if key exists
		public abstract bool Find(ref KeyWalker key, CPEnumerator<T> e);

		// Returns true if key already existed. Can be used to find rather than 
		// create or set a value (mode==JPMode.Find), if the caller just wants 
		// the value and not an enumerator. If the key already existed, this 
		// method sets value to the original value associated with the key.
		public abstract bool Set(ref KeyWalker key, ref T value, ref CPNode<T> self, CPMode mode);
		
		// Associates the specified node with a given key. AddChild() requires 
		// that the specified key does not exist already.
		public abstract void AddChild(ref KeyWalker key, CPNode<T> value, ref CPNode<T> self);

		// Returns true if key formerly existed
		public abstract bool Remove(ref KeyWalker key, ref T oldValue, ref CPNode<T> self);

		public abstract int CountMemoryUsage(int sizeOfT);

		public abstract CPNode<T> CloneAndOptimize();

		public abstract int LocalCount { get; }

		public abstract void MoveFirst(CPEnumerator<T> e);
		public abstract void MoveLast(CPEnumerator<T> e);
		public abstract bool MoveNext(CPEnumerator<T> e);
		public abstract bool MovePrev(CPEnumerator<T> e);
	}
}