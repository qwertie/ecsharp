using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static System.Math;

namespace Loyc.Collections
{
	partial class ListExt
	{
		/// <summary>Converts an <see cref="IEnumerable{T}"/> to <see cref="IScannable{T}"/>.</summary>
		public static ScannableEnumerable<T> AsScannable<T>(this IEnumerable<T> sequence) 
			=> new ScannableEnumerable<T>(sequence);

		public static ScannableEnumerable<T>.Scanner<TEnumerator> 
			AsScanner<TEnumerator, T>(this TEnumerator enumerator) where TEnumerator : IEnumerator<T>
			=> new ScannableEnumerable<T>.Scanner<TEnumerator>(enumerator);
		
		/// <summary>A no-op.</summary>
		[Obsolete("The object is already IScannable; this method is a no-op.")]
		public static IScannable<T> AsScannable<T>(this IScannable<T> sequence)
			=> sequence;
	}

	/// <summary>An adapter that implements <see cref="IScannable{T}"/> on any collection.</summary>
	public struct ScannableEnumerable<T> : IScannable<T>
	{
		IEnumerable<T> _seq;
		public ScannableEnumerable(IEnumerable<T> sequence) => _seq = sequence;

		IScanner<T> IScan<T>.Scan() => Scan();
		public IScanner<T> Scan() => new Scanner<IEnumerator<T>>(_seq.GetEnumerator());

		public IEnumerator<T> GetEnumerator() => _seq.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Scanner<TEnumerator> : IScanner<T> where TEnumerator : IEnumerator<T>
		{
			TEnumerator _rest;
			Memory<T> _buffer;
			int _offset; // Current position as offset from beginning of _buffer (<= _count)
			int _count; // Number of valid elements in _buffer (starting from index 0)
			
			const int MinAllocatedBufferSize = 16;

			public Scanner(TEnumerator seq)
			{
				_rest = seq;
				_buffer = default(Memory<T>);
				_offset = _count = 0;
			}

			public bool CanScanBackward => false;

			public ReadOnlyMemory<T> Read(int skip, int minLength, ref Memory<T> buffer)
			{
				CheckParam.IsNotNegative(nameof(skip), skip);
				Span<T> span;
				int i;

				if (minLength <= 0)
					minLength = 32;

				// Are we skipping all items that were previously loaded?
				int itemsLeft = _count - _offset;
				if ((uint)skip >= (uint)itemsLeft) {
					// The new region shares nothing with whatever is in there now.
					// We will be reading data into the beginning of _buffer (index 0)
					if (minLength < _count)
						_buffer.Span.Slice(minLength, _count - minLength).Clear();
					_offset = _count = 0;

					// If we've been asked to skip items without returning them, do so
					for (int skipUnseen = skip - itemsLeft; skipUnseen > 0; skipUnseen--)
						if (!_rest.MoveNext())
							return default;
				} else checked {
					_offset += skip;
				}

				// Prepare the _buffer
				int keepItems = _count - _offset;
				int spaceAvailable = _buffer.Length - _offset;

				// Caution: if minLength is extremely big, don't allocate so much
				// until we know there are enough items to justify the allocation.
				// Notably, the caller can request int.MaxValue which we mustn't allocate.
				int minLength2 = minLength;
				if (minLength2 > 1 << 20) {
					// Note: Read() calls itself recursively if the default amount of
					// 1 << 12 is insufficient. In that case we want to read more than
					// last time, and keepItems is the number of items already loaded,
					// so try to load Times2(keepItems) items.
					minLength2 = Min(minLength, Max(1 << 12, Times2(keepItems)));
				}

				if (spaceAvailable < minLength2)
				{
					// There isn't enough space in the right side of the buffer for the new items.
					if (_buffer.Length < minLength2) {
						// In fact, we'll need a new buffer
						var prevBuf = _buffer;

						if (minLength2 <= buffer.Length)
							_buffer = buffer;
						else {
							int newSize = Max(minLength2, _buffer.Length + (_buffer.Length >> 1));
							_buffer = buffer = new T[Max(newSize, MinAllocatedBufferSize)];
						}

						// Copy already-loaded items (if any) that the caller isn't skipping over
						prevBuf.Slice(_offset, keepItems).CopyTo(_buffer);
					} else {
						// Make space in the current buffer by moving re-used items to the beginning
						Debug.Assert(keepItems > 0);
						_buffer.Slice(_offset, keepItems).CopyTo(_buffer);
						if (minLength2 < _count)
							_buffer.Span.Slice(minLength2, _count - minLength2).Clear();
					}

					_count -= _offset;
					_offset = 0;
				}

				// Read the necessary number of items (minLength2 - keepItems)
				span = _buffer.Span;
				var stopAt = _offset + minLength2;
				for (i = _count; i < stopAt; i++) {
					if (_rest.MoveNext())
						span[i] = _rest.Current;
					else {
						_count = i;
						return _buffer.Slice(_offset, i - _offset);
					}
				}
				if (_count < stopAt)
					_count = stopAt;

				// If there really are zillions of items in the source stream,
				// reallocate as necessary and keep reading.
				while (minLength2 < minLength) {
					minLength2 = Min(Times2(minLength2), minLength);
					var result = Read(0, minLength2, ref buffer);
					if (result.Length < minLength2)
						return result;
				}

				return _buffer.Slice(_offset, _count - _offset);
			}

			static int Times2(int x) => x < int.MaxValue / 2 ? x * 2 : int.MaxValue;
		}
	}
}
