using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities.CPTrie;
using System.Diagnostics;

namespace Loyc.Utilities
{
	/// <summary>This CPTrie "bitmap" node splits the 8-bit alphabet space into 8
	/// buckets of 5 bytes each; a CPSNode is used to store the keys in each
	/// bucket.</summary>
	/// <typeparam name="T">Type of values associated with each key</typeparam>
	class CPBNode<T> : CPNode<T>
	{
		CPNode<T>[] _children = new CPNode<T>[8];

		static readonly object NoZLK = new object();
		/// <summary>The value associated with a zero-length key, if any, is stored
		/// here directly rather than in any of the children.</summary>
		object _zlk = NoZLK;

		public CPBNode() {}
		public CPBNode(CPBNode<T> copy)
		{
			for (int i = 0; i < _children.Length; i++)
				if (copy._children[i] != null)
					_children[i] = copy._children[i].CloneAndOptimize();
			
			_zlk = copy._zlk;
		}

		public override bool Find(ref KeyWalker key, CPEnumerator<T> e)
		{
			if (key.Left == 0) {
				MoveFirst(e);
				return _zlk != NoZLK;
			} else {
				int i = key[0] >> 5;
				e.Stack.Add(new CPEnumerator<T>.Entry(this, i, e.Key.Offset));
				if (_children[i] != null)
					return _children[i].Find(ref key, e);
				else {
					e.MoveNext();
					return false;
				}
			}
		}

		public override bool Set(ref KeyWalker key, ref T value, ref CPNode<T> self, CPMode mode)
		{
			if (key.Left > 0)
			{
				int i = key[0] >> 5;
				if (_children[i] != null)
					return _children[i].Set(ref key, ref value, ref _children[i], mode);
				else {
					if ((mode & CPMode.Create) != (CPMode)0)
						_children[i] = new CPSNode<T>(ref key, value);
					return false;
				}
			}
			else
			{
				// key.Left == 0
				if (_zlk == NoZLK)
				{
					if ((mode & CPMode.Create) != (CPMode)0)
						_zlk = value;
					return false;
				}
				else
				{
					T oldValue = (T)_zlk;
					if ((mode & CPMode.Set) != (CPMode)0)
						_zlk = value;
					value = oldValue;
					return true;
				}
			}
		}

		public override void AddChild(ref KeyWalker key, CPNode<T> value, ref CPNode<T> self)
		{
			Debug.Assert(key.Left > 0);
			int i = key[0] >> 5;
			if (_children[i] != null)
				_children[i].AddChild(ref key, value, ref _children[i]);
			else
				_children[i] = new CPSNode<T>(ref key, value);
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref CPNode<T> self)
		{
			if (key.Left > 0)
			{
				int i = key[0] >> 5;
				if (_children[i] != null) {
					bool found = _children[i].Remove(ref key, ref oldValue, ref _children[i]);
					if (_children[i] == null && IsEmpty())
						self = null;
					return found;
				} else
					return false;
			}
			else
			{
				// key.Left == 0
				if (_zlk == NoZLK)
					return false;
				else {
					oldValue = (T)_zlk;
					_zlk = NoZLK;
					if (IsEmpty())
						self = null;
					return true;
				}
			}
		}

		private bool IsEmpty()
		{
			for (int i = 0; i < _children.Length; i++)
				if (_children[i] != null)
					return false;
			return _zlk == NoZLK;
		}

		public override int CountMemoryUsage(int sizeOfT)
		{
			int size = 16 + 16 + _children.Length * 4;
			for (int i = 0; i < _children.Length; i++)
				if (_children[i] != null)
					size += _children[i].CountMemoryUsage(sizeOfT);

			if (_zlk != NoZLK && _zlk != null)
				size += 8 + sizeOfT;

			return size;	
		}
		public override CPNode<T> CloneAndOptimize()
		{
			return new CPBNode<T>(this);
		}

		public override int LocalCount
		{
			get {
				int count = _zlk != NoZLK ? 1 : 0;
				for (int i = 0; i < _children.Length; i++)
					if (_children[i] != null)
						count += _children[i].LocalCount;
				return count;
			}
		}

		public override void MoveFirst(CPEnumerator<T> e)
		{
			if (_zlk != NoZLK)
			{
				e.Stack.Add(new CPEnumerator<T>.Entry(this, -1, e.Key.Offset));
				return;
			}
			for (int i = 0; i < _children.Length; i++)
				if (_children[i] != null) {
					e.Stack.Add(new CPEnumerator<T>.Entry(this, i, e.Key.Offset));
					_children[i].MoveFirst(e);
					return;
				}
		}
		public override void MoveLast(CPEnumerator<T> e)
		{
			for (int i = _children.Length - 1; i >= 0; i--)
				if (_children[i] != null)
				{
					e.Stack.Add(new CPEnumerator<T>.Entry(this, i, e.Key.Offset));
					_children[i].MoveLast(e);
					return;
				}
			Debug.Assert(_zlk != NoZLK);
			e.Stack.Add(new CPEnumerator<T>.Entry(this, -1, e.Key.Offset));
			return;
		}
		public override bool MoveNext(CPEnumerator<T> e)
		{
			int top = e.Stack.Count - 1;
			Debug.Assert(e.Stack[top].Node == this);
			for (int i = e.Stack[top].Index + 1; i < _children.Length; i++)
				if (_children[i] != null)
				{
					e.Stack.InternalArray[top].Index = i;
					_children[i].MoveFirst(e);
					return true;
				}
			return false;
		}
		public override bool MovePrev(CPEnumerator<T> e)
		{
			int top = e.Stack.Count - 1;
			Debug.Assert(e.Stack[top].Node == this);
			for (int i = e.Stack[top].Index - 1; i >= 0; i--)
				if (_children[i] != null)
				{
					e.Stack.InternalArray[top].Index = i;
					_children[i].MoveLast(e);
					return true;
				}
			if (e.Stack[top].Index > -1 && _zlk != NoZLK)
			{
				e.Stack.InternalArray[top].Index = -1;
				return true;
			}
			return false;
		}
	}
}
