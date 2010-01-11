using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Utilities.JPTrie;

namespace Loyc.Utilities
{
	class JPLinear<T> : JPNode<T>
	{
		public override bool Find(ref KeyWalker key, JPEnumerator e)
		{
			throw new NotImplementedException();
		}

		public override bool Set(ref KeyWalker key, ref T value, ref JPNode<T> self, JPMode mode)
		{
			throw new NotImplementedException();
		}

		public override bool Remove(ref KeyWalker key, ref T oldValue, ref JPNode<T> self)
		{
			throw new NotImplementedException();
		}

		internal void Set(ref KeyWalker key, JPNode<T> leaf)
		{
			throw new NotImplementedException();
		}
	}
}
