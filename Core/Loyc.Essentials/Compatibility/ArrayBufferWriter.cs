using System;
using System.Buffers;

namespace Loyc.Compatibility
{
	#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472

	/// <summary>Makes System.Buffers.ArrayBufferWriter<T> available in .NET Standard 2.0 and .NET 4.x.</summary>
	public sealed class ArrayBufferWriter<T> : IBufferWriter<T>
	{
		private T[] _buffer;

		private int _index;

		public ReadOnlyMemory<T> WrittenMemory => (ReadOnlyMemory<T>)_buffer.AsMemory(0, _index);

		public ReadOnlySpan<T> WrittenSpan => (ReadOnlySpan<T>)_buffer.AsSpan(0, _index);

		public int WrittenCount => _index;

		public int Capacity => _buffer.Length;

		public int FreeCapacity => _buffer.Length - _index;

		public ArrayBufferWriter()
		{
			_buffer = Array.Empty<T>();
			_index = 0;
		}
		public ArrayBufferWriter(int initialCapacity)
		{
			CheckParam.IsNotNegative(nameof(initialCapacity), initialCapacity);
			_buffer = new T[initialCapacity];
			_index = 0;
		}

		public void Clear()
		{
			_buffer.AsSpan(0, _index).Clear();
			_index = 0;
		}

		public void Advance(int count)
		{
			CheckParam.IsNotNegative(nameof(count), count);
			if (count > _buffer.Length - _index)
				throw new InvalidOperationException("Advanced too far ({0} > {1})".Localized(count, _buffer.Length - _index));
			_index += count;
		}

		public Memory<T> GetMemory(int sizeRequired = 0)
		{
			CheckAndResizeBuffer(sizeRequired);
			return _buffer.AsMemory(_index);
		}

		public Span<T> GetSpan(int sizeRequired = 0)
		{
			CheckAndResizeBuffer(sizeRequired);
			return _buffer.AsSpan(_index);
		}

		private void CheckAndResizeBuffer(int sizeHint)
		{
			CheckParam.IsNotNegative(nameof(sizeHint), sizeHint);
			if (sizeHint <= FreeCapacity) {
				return;
			}
			int size1 = _buffer.Length;
			int size2 = System.Math.Max(sizeHint, size1);
			if (size1 == 0) {
				size2 = System.Math.Max(size2, 256);
			}
			int num3 = size1 + size2;
			if ((uint)num3 > 2147483647u) {
				num3 = size1 + sizeHint;
				if ((uint)num3 > 2147483647u) {
					throw new OutOfMemoryException("Buffer cannot enlarge beyond 2GB");
				}
			}
			Array.Resize(ref _buffer, num3);
		}
	}

	#endif
}