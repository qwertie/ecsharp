using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	/// <summary>This class wraps an <see cref="IEnumerator{T}"/> or 
	/// <see cref="IEnumerable{T}"/> into an <see cref="IListSource"/>, lazily 
	/// reading the sequence as <see cref="TryGet"/> is called.</summary>
	/// <remarks>Avoid calling <see cref="Count"/> if you actually want laziness;
	/// this property must read and buffers the entire sequence.</remarks>
	public class BufferedSequence<T> : ListSourceBase<T>
	{
		InternalList<T> _buffer;
		IEnumerator<T> _e; // set to null when ended

		public BufferedSequence(IEnumerable<T> e) : this(e.GetEnumerator()) { }
		public BufferedSequence(IEnumerator<T> e) { _e = e; }
	
		public override T TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_buffer.Count)
				return _buffer[index];
			else if (index >= 0) {
				while (_e.MoveNext()) {
					_buffer.Add(_e.Current);
					if (index < _buffer.Count)
						return _buffer[index];
				}
				_e = null;
			}
			fail = true;
			return default(T);
		}

		public override int Count
		{
			get {
				if (_e != null) {
					bool _ = false;
					TryGet(int.MaxValue, ref _);
				}
				return _buffer.Count;
			}
		}
	}
}
