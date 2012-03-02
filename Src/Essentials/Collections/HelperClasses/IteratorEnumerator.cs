/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:40 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	[Serializable]
	public struct IteratorEnumerator<T> : IEnumerator<T>
	{
		Iterator<T> _it;
		T _current;
		public IteratorEnumerator(Iterator<T> it) { _it = it; _current = default(T); }

		public T Current
		{
			get { return _current; }
		}
		public void Dispose()
		{
		}
		object System.Collections.IEnumerator.Current
		{
			get { return _current; }
		}
		public bool MoveNext()
		{
			bool ended = false;
			_current = _it(ref ended);
			return !ended;
		}
		public void Reset()
		{
			throw new NotSupportedException("An Iterator<T> cannot be reset.");
		}
	}
}
