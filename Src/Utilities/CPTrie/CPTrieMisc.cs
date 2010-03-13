using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using Loyc.Runtime;

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
				if (_trie.Head == null)
					return false;

				_trie.Head.MoveFirst(this);
				Debug.Assert(Stack.Last.KeyOffset == Key.Offset);
				return true;
			}
			
			for(;;) {
				int top = Stack.Count - 1;
				bool success = Stack[top].Node.MoveNext(this);
				Debug.Assert(Stack.Last.KeyOffset == Key.Offset);
				if (success) {
					Debug.Assert(top < Stack.Count);
					return true;
				} else {
					Debug.Assert(top == Stack.Count - 1);
					Stack.Pop();
					if (Stack.IsEmpty) {
						Key = KeyWalker.Empty;
						return false;
					}
					Key.Reset(Stack.Last.KeyOffset);
				}
			}
		}
		public bool MovePrev()
		{
			if (Stack.IsEmpty) {
				if (_trie.Head == null)
					return false;
				
				_trie.Head.MoveLast(this);
				Debug.Assert(Stack.Last.KeyOffset == Key.Offset);
				return true;
			}
			
			for(;;) {
				int top = Stack.Count - 1;
				bool success = Stack[top].Node.MovePrev(this);
				Debug.Assert(Stack.Last.KeyOffset == Key.Offset);
				if (success) {
					Debug.Assert(top < Stack.Count);
					return true;
				} else {
					Debug.Assert(top == Stack.Count - 1);
					Stack.Pop();
					if (Stack.IsEmpty) {
						Key = KeyWalker.Empty;
						return false;
					}
					Key.Reset(Stack.Last.KeyOffset);
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
		
		/// <summary>Returns true if this enumerator points to an item and Current
		/// is valid.</summary>
		public bool IsValid { get { return !Stack.IsEmpty; } }

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
			Debug.Assert(offset >= 0 && left >= 0 && 
				offset + left <= (_key == null ? 0 : _key.Length));
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
		public void Reset(byte[] key, int offset, int left)
		{
			_key = key;
			_offset = offset;
			_left = left;
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
			
			sb.Replace("\0", "\\0"); // Debugger terminates at embedded null
			return sb.ToString();
		}
		#endif

		public byte[] Buffer { get { return _key; } }

		public static readonly KeyWalker Empty = new KeyWalker(InternalList<byte>.EmptyArray, 0);
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

		/// <summary>Sets up the specified enumerator to point at the first item in
		/// this node.</summary>
		/// <remarks>
		/// On entry to the method, e.Key.Buffer[0..e.Key.Offset] specifies the
		/// prefix on all keys in the node. On exit, e.Key.Offset should remain the
		/// same unless MoveFirst was called on a child node.
		/// <para/>
		/// This method
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
		/// </remarks>
		public abstract void MoveFirst(CPEnumerator<T> e);
		/// <summary>Does the same thing as MoveFirst except that the last item is
		/// retrieved instead of the first one.</summary>
		public abstract void MoveLast(CPEnumerator<T> e);

		/// <summary>Moves to the next item in the node.</summary>
		/// <returns>Returns true if the next item was extracted or false if the 
		/// end of the node was reached.</returns>
		/// <remarks>
		/// Upon entry to this method, e.Stack.Last.Node == this
		///                        and e.Stack.Last.KeyOffset == e.Key.Offset.
		/// <para/>
		/// This method
		/// (1) increases e.Stack.Last.Index to point to the next item in the node;
		/// (2) returns false if Index has advanced past the last item in the node;
		/// (3) otherwise, repeats steps (2)-(4) in the documentation of
		///     MoveFirst() and returns true.
		/// </remarks>
		public abstract bool MoveNext(CPEnumerator<T> e);

		/// <summary>Does the same thing as MovePrev except that it moves to the
		/// previous item instead of the next item.</summary>
		public abstract bool MovePrev(CPEnumerator<T> e);
	}
}