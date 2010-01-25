using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities.JPTrie;

namespace Loyc.Utilities
{
	class CPBitmap<T> : CPNode<T>
	{
		public CPBitmap()
		{

		}

		public override bool Find(ref KeyWalker key, CPEnumerator e)
		{
			throw new NotImplementedException();
		}

		public override bool Set(ref KeyWalker key, ref T value, ref CPNode<T> self, CPMode mode)
		{
			throw new NotImplementedException();
		}
		public override void AddChild(ref KeyWalker key, CPNode<T> value, ref CPNode<T> self)
		{
			throw new NotImplementedException();
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref CPNode<T> self)
		{
			throw new NotImplementedException();
		}

		internal void Set(ref KeyWalker key, CPNode<T> leaf)
		{
			throw new NotImplementedException();
		}
	}
}
