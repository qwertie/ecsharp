using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Collections.MutableListExtensionMethods
{
	public static partial class IListExt
	{
		[Obsolete("An IList<T> does not need to be Buffered(). If you would like to convert the IList to IListSource, it is recommended to use AsListSource() instead.")]
		public static BufferedSequence<T> Buffered<T>(this IList<T> source)
		{
			return new BufferedSequence<T>(source);
		}
	}
}

namespace Loyc.Collections
{
	public static partial class EnumerableExt
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
	}

	/// <summary>Adapter: This class wraps an <see cref="IEnumerator{T}"/> or 
	/// <see cref="IEnumerable{T}"/> into an <see cref="IListSource{T}"/>, lazily 
	/// reading the sequence as <see cref="TryGet"/> is called.</summary>
	/// <remarks>Avoid calling <see cref="Count"/> if you actually want laziness;
	/// this property must read and buffer the entire sequence.</remarks>
	public class BufferedSequence<T> : ListSourceBase<T>, IScannable<T>
	{
		InternalList<T> _buffer = InternalList<T>.Empty;
		IEnumerator<T>? _e; // set to null when ended

		public BufferedSequence(IEnumerable<T> e) : this(e.GetEnumerator()) { }
		public BufferedSequence(IEnumerator<T> e) { _e = e; }

		public override IEnumerator<T> GetEnumerator()
		{
			bool fail;
			for (int i = 0; ; i++) {
				T? value = TryGet(i, out fail);
				if (fail) break;
				yield return value!;
			}
		}

		[return: MaybeNull] // There's no attribute like [return: MaybeNullIf("fail")]
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

		/// <summary>Gets the number of items that have already been pulled from the 
		/// enumerator and stored in this object's buffer.</summary>
		public int BufferedCount => _buffer.Count;

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

		public Scanner Scan() => new Scanner(this);
		IScanner<T> IScan<T>.Scan() => Scan();

		public struct Scanner : IScanner<T>
		{
			BufferedSequence<T> _seq;
			private int _index;

			public Scanner(BufferedSequence<T> seq)
			{
				_seq = seq;
				_index = 0;
			}

			public bool CanScanBackward => true;

			public ReadOnlyMemory<T> Read(int skip, int minLength, ref Memory<T> buffer)
			{
				_index += skip;

				if (minLength < 0)
					minLength = 16;

				// Ensure we've buffered up enough items to fulfill the request
				int lastIndex = _index + minLength - 1;
				_seq.TryGet(lastIndex, out bool _);

				if ((uint)_index > (uint)_seq._buffer.Count) {
					if (skip < 0) {
						_index = 0;
						CheckParam.ThrowBadArgument(nameof(skip), "Attempted to rewind before beginning of array");
					} else {
						_index = _seq._buffer.Count;
					}
				}
				
				return _seq._buffer.AsMemory().Slice(_index);
			}
		}
	}
}
