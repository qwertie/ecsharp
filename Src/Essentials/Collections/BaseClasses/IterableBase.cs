/*
 * Created by SharpDevelop.
 * User: Pook
 * Date: 4/10/2011
 * Time: 8:45 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;

namespace Loyc.Collections
{
	/// <summary>A helper class for implementing <see cref="IIterable{T}"/> that
	/// contains GetEnumerator implementations.</summary>
	[Serializable]
	public abstract class IterableBase<T> : IIterable<T>
	{
		public abstract Iterator<T> GetIterator();

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
		    return GetIterator().AsEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetIterator().AsEnumerator();
		}
		public IteratorEnumerator<T> GetEnumerator()
		{
			return GetIterator().AsEnumerator();
		}
	}
}
