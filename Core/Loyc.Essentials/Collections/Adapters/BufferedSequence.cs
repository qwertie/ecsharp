using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Collections
{
	// Not in EnumerableExt because LCExt is for adapters and BufferedSequence<T> implements IListSource<T>
	public static partial class LinqToLists
	{
		public static BufferedSequence<T> Buffered<T>(this IEnumerator<T> source)
		{
			return new BufferedSequence<T>(source);
		}
		public static BufferedSequence<T> Buffered<T>(this IEnumerable<T> source)
		{
			return new BufferedSequence<T>(source);
		}
		
		[Obsolete("An IListSource<T> does not need to be Buffered(). This is treated as a no-op.")]
		public static IListSource<T> Buffered<T>(this IListSource<T> source)
		{
			return source;
		}
		[Obsolete("An IList<T> does not need to be Buffered(). If you would like to convert the IList to IListSource, it is recommended to use AsListSource() instead.")]
		public static BufferedSequence<T> Buffered<T>(this IList<T> source)
		{
			return new BufferedSequence<T>(source);
		}
	}

	/// <summary>Adapter: This class wraps an <see cref="IEnumerator{T}"/> or 
	/// <see cref="IEnumerable{T}"/> into an <see cref="IListSource{T}"/>, lazily 
	/// reading the sequence as <see cref="TryGet"/> is called.</summary>
	/// <remarks>Avoid calling <see cref="Count"/> if you actually want laziness;
	/// this property must read and buffer the entire sequence.</remarks>
	public class BufferedSequence<T> : ListSourceBase<T>
	{
		InternalList<T> _buffer = InternalList<T>.Empty;
		IEnumerator<T> _e; // set to null when ended

		public BufferedSequence(IEnumerable<T> e) : this(e.GetEnumerator()) { }
		public BufferedSequence(IEnumerator<T> e) { _e = e; }

		public override IEnumerator<T> GetEnumerator()
		{
			bool fail;
			for (int i = 0; ; i++) {
				T value = TryGet(i, out fail);
				if (fail) break;
				yield return value;
			}
		}

		public override T TryGet(int index, out bool fail)
		{
			if ((uint)index < (uint)_buffer.Count) {
				fail = false;
				return _buffer[index];
			} else if (index >= 0 && _e != null) {
				while (_e.MoveNext()) {
					_buffer.Add(_e.Current);
					if (index < _buffer.Count) {
						fail = false;
						return _buffer[index];
					}
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
					TryGet(int.MaxValue, out _);
				}
				return _buffer.Count;
			}
		}
	}
}
