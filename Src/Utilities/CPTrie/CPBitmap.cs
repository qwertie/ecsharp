using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities.CPTrie;
using System.Diagnostics;

namespace Loyc.Utilities
{
	class CPBitmap<T> : CPNode<T>
	{
		CPNode<T>[] _children = new CPNode<T>[8];

		static readonly object NoZLK = new object();
		object _zlk = NoZLK;

		public override bool Find(ref KeyWalker key, CPEnumerator e)
		{
			if (key.Left == 0)
			{
				throw new NotImplementedException();
			}
			else
			{
				int i = key[0] >> 5;
				if (_children[i].Find(ref key, e))
				{
					throw new NotImplementedException();
				}
				return false;
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
						_children[i] = new CPLinear<T>(ref key, value);
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
				_children[i] = new CPLinear<T>(ref key, value);
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref CPNode<T> self)
		{
			if (key.Left > 0)
			{
				int i = key[0] >> 5;
				if (_children[i] != null)
					return _children[i].Remove(ref key, ref oldValue, ref _children[i]);
				else
					return false;
			}
			else
			{
				// key.Left == 0
				if (_zlk == NoZLK)
					return false;
				else {
					oldValue = (T)_zlk;
					return true;
				}
			}
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
	}
}
