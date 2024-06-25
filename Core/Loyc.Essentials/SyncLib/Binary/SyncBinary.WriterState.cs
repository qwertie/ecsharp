using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	const int MaxSizeOfInt32 = 5;
	const int MaxSizeOfInt64 = 10;

	internal class WriterState : WriterStateBase
	{
		internal Options _opt;
		internal Options.ForWriter _optWrite;
		
		// Number of bits in _buf[_i - 1] that have not yet been used, and could be
		// used by a bitfield.
		private uint _bitfieldBitsLeftInByte = 0;

		/// <summary>Keeps track of objects that the user has started writing with 
		///   BeginSubObject, but hasn't finished writing.</summary>
		protected internal InternalList<ObjectMode> _stack = InternalList<ObjectMode>.Empty;

		internal int Depth => _stack.Count;
		internal bool IsInsideList 
			=> _stack.Count != 0 && (_stack.Last & (ObjectMode.List | ObjectMode.Tuple)) != 0;

		public WriterState(IBufferWriter<byte> output, Options options) : base(output)
		{
			_opt = options;
			_optWrite = _opt.Write;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected new Span<byte> GetOutSpan(int requiredBytes)
		{
			_bitfieldBitsLeftInByte = 0;
			return base.GetOutSpan(requiredBytes);
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//protected Span<byte> GetBitfieldOutSpan(int requiredBytes)
		//{
		//	Debug.Assert(_i > 0);
		//	if (_i + requiredBytes < _buf.Length) {
		//		return _buf.Span;
		//	} else {
		//		return MostlyFlushAndGetOutSpan(requiredBytes);
		//	}
		//}
		//[MethodImpl(MethodImplOptions.NoInlining)]
		//protected Span<byte> MostlyFlushAndGetOutSpan(int requiredBytes)
		//{
		//	_output.Advance(_i - 1);
		//	_i = 1;
		//	_buf = _output.GetMemory(System.Math.Max(requiredBytes + 1, MinimumBufSize));
		//	return _buf.Span;
		//}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNull()
		{
			GetOutSpan(1)[_i++] = 255;
		}

		public void Write(bool value)
		{
			GetOutSpan(1)[_i++] = (byte)(value ? 1 : 0);
		}

		public void WriteNullable(bool? value)
		{
			GetOutSpan(1)[_i++] = (byte)(value == null ? 255 : value.Value ? 1 : 0);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(sbyte? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(short? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(int? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(long? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(byte? num)
		{
			if (num == null)
				WriteNull();
			else
				Write((uint) num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(ushort? num)
		{
			if (num == null)
				WriteNull();
			else
				Write((uint) num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(uint? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(ulong? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(BigInteger? num)
		{
			if (num == null)
				WriteNull();
			else
				Write(num.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(float? num)
		{
			if (num == null)
				WriteLittleEndianBytes(FloatNullBitPattern, 4, GetOutSpan(4));
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(double? num)
		{
			if (num == null)
				WriteLittleEndianBytes(DoubleNullBitPattern, 8, GetOutSpan(8));
			else
				Write(num.Value);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteNullable(decimal? num)
		{
			if (num == null) {
				var outBuf = GetOutSpan(16);
				for (int numBytes = 16; numBytes > 0; numBytes--)
					outBuf[_i++] = 0xFF;
			} else
				Write(num.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(int num)
		{
			// Fast path for small non-negative numbers
			if ((uint)num < 64)
				GetOutSpan(1)[_i++] = (byte)num;
			else
				WriteSignedOrUnsigned((uint)num, G.PositionOfMostSignificantOne((uint)(num < 0 ? ~num : num)) + 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(uint num)
		{
			// Fast path for small numbers
			if (num < 128)
				GetOutSpan(1)[_i++] = (byte)num;
			else
				WriteSignedOrUnsigned(num, G.PositionOfMostSignificantOne(num));
		}

		public void WriteSignedOrUnsigned(uint num, int msbPosition)
		{
			Debug.Assert((2 << msbPosition) > num || msbPosition == 31 || (int)num < 0);

			Span<byte> span;
			unchecked {
				switch (msbPosition) {
				// 29 to 32 significant bits
				case 32: case 31: case 30: case 29: case 28:
					span = GetOutSpan(5);
					bool isSigned = ((int)num & (1 << msbPosition) & ~1) != 0;
					span[_i] = (byte)((int)num < 0 && isSigned ? 0b1111_1111 : 0b1111_0000);
					span[_i + 1] = (byte)(num >> 24);
					span[_i + 2] = (byte)(num >> 16);
					span[_i + 3] = (byte)(num >> 8);
					span[_i + 4] = (byte)num;
					_i += 5;
					break;
				// 22 to 28 significant bits
				case 27: case 26: case 25: case 24: case 23: case 22: case 21:
					span = GetOutSpan(4);
					span[_i    ] = (byte)(0b1110_0000 | ((num >> 24) & 0b0000_1111));
					span[_i + 1] = (byte)(num >> 16);
					span[_i + 2] = (byte)(num >> 8);
					span[_i + 3] = (byte)num;
					_i += 4;
					break;
				// 15 to 21 significant bits
				case 20: case 19: case 18: case 17: case 16: case 15: case 14:
					span = GetOutSpan(3);
					span[_i    ] = (byte)(0b1100_0000 | ((num >> 16) & 0b0001_1111));
					span[_i + 1] = (byte)(num >> 8);
					span[_i + 2] = (byte)num;
					_i += 3;
					break;
				// 8 to 14 significant bits
				case 13: case 12: case 11: case 10: case 9: case 8: case 7:
					span = GetOutSpan(2);
					span[_i    ] = (byte)(0b1000_0000 | ((num >> 8) & 0b0011_1111));
					span[_i + 1] = (byte)num;
					_i += 2;
					break;
				// 0 to 7 significant bits
				default:
					GetOutSpan(1)[_i++] = (byte)(num & 0x7F);
					break;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(long num)
		{
			// Fast path for small non-negative numbers
			if ((uint)num < 64)
				GetOutSpan(1)[_i++] = (byte)num;
			else
				// -4 => 3 bits, -5 => 4 bits
				WriteSignedOrUnsigned((uint)num, G.PositionOfMostSignificantOne((ulong)(num < 0 ? ~num : num)) + 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(ulong num)
		{
			// Fast path for small numbers
			if (num < 128)
				GetOutSpan(1)[_i++] = (byte)num;
			else
				WriteSignedOrUnsigned(num, G.PositionOfMostSignificantOne(num));
		}

		public void WriteSignedOrUnsigned(ulong num, int msbPosition)
		{
			Debug.Assert((2uL << msbPosition) > num || msbPosition == 63 || (long)num < 0);

			if (msbPosition < 32) {
				WriteSignedOrUnsigned((uint)num, msbPosition);
			} else {
				Span<byte> span;
				unchecked {
					switch (msbPosition) {
					// 50 to 64 significant bits
					default:
						int numberSize = msbPosition >= 56 ? 10 : 9;
						span = GetOutSpan(numberSize);
						span[_i    ] = (byte)0b1111_1110;
						span[_i + 1] = (byte)(numberSize - 1);
						if (msbPosition >= 56) {
							span[_i + 2] = (byte)(num >> 56);
							_i++;
						}
						span[_i + 2] = (byte)(num >> 48);
						span[_i + 3] = (byte)(num >> 40);
						span[_i + 4] = (byte)(num >> 32);
						span[_i + 5] = (byte)(num >> 24);
						span[_i + 6] = (byte)(num >> 16);
						span[_i + 7] = (byte)(num >> 8);
						span[_i + 8] = (byte)num;
						_i += 9;
						break;
					// 43 to 49 significant bits
					case 48: case 47: case 46: case 45: case 44: case 43: case 42:
						span = GetOutSpan(7);
						span[_i    ] = (byte)(0b1111_1100 | ((num >> 48) & 1));
						span[_i + 1] = (byte)(num >> 40);
						span[_i + 2] = (byte)(num >> 32);
						span[_i + 3] = (byte)(num >> 24);
						span[_i + 4] = (byte)(num >> 16);
						span[_i + 5] = (byte)(num >> 8);
						span[_i + 6] = (byte)num;
						_i += 7;
						break;
					// 36 to 42 significant bits
					case 41: case 40: case 39: case 38: case 37: case 36: case 35:
						span = GetOutSpan(6);
						span[_i    ] = (byte)(0b1111_1000 | ((num >> 40) & 0b0011));
						span[_i + 1] = (byte)(num >> 32);
						span[_i + 2] = (byte)(num >> 24);
						span[_i + 3] = (byte)(num >> 16);
						span[_i + 4] = (byte)(num >> 8);
						span[_i + 5] = (byte)num;
						_i += 6;
						break;
					// 29 to 35 significant bits
					case 34: case 33: case 32:
						span = GetOutSpan(5);
						span[_i    ] = (byte)(0b1111_0000 | ((num >> 32) & 0b0111));
						span[_i + 1] = (byte)(num >> 24);
						span[_i + 2] = (byte)(num >> 16);
						span[_i + 3] = (byte)(num >> 8);
						span[_i + 4] = (byte)num;
						_i += 5;
						break;
					}
				}
			}
		}

		public void Write(BigInteger num)
		{
			if (num <= long.MaxValue && num >= long.MinValue) {
				Write((long)num);
			} else {
				#if NETSTANDARD2_0 || NET45 || NET46 || NET47
				var numberBytes = num.ToByteArray();
				int numNumberBytes = numberBytes.Length;
				#else
				int numNumberBytes = num.GetByteCount(isUnsigned: false);
				#endif

				if (numNumberBytes > _opt.MaxNumberSize)
					throw new ArgumentOutOfRangeException(
						"The BigInteger cannot be stored because it is larger than the current MaxNumberSize.");

				// Usually 2 bytes are needed for the number's length-prefix header,
				// but it could be up to 6 bytes in extreme cases.
				var span = GetOutSpan(6 + numNumberBytes);
				
				span[_i++] = 0b1111_1110;
				Write((uint) numNumberBytes);
				
				Debug.Assert(span == GetOutSpan(numNumberBytes));
				span = span.Slice(_i);
				Debug.Assert(span.Length >= numNumberBytes);

				#if NETSTANDARD2_0 || NET45 || NET46 || NET47
				Array.Reverse(numberBytes);
				numberBytes.CopyTo(span);
				#else
				G.Verify(num.TryWriteBytes(span, out int bytesWritten, false, isBigEndian: true));
				#endif
			}
		}

		public void WriteLittleEndianBytes(uint num, int numBytes = 4) 
			=> WriteLittleEndianBytes(num, numBytes, GetOutSpan(numBytes));
		public uint WriteLittleEndianBytes(uint num, int numBytes, Span<byte> outBuf)
		{
			for (; numBytes > 0; numBytes--) {
				outBuf[_i++] = (byte)num;
				num >>= 8;
			}
			return num;
		}
		public void WriteLittleEndianBytes(ulong num, int numBytes = 8)
			=> WriteLittleEndianBytes(num, numBytes, GetOutSpan(numBytes));
		public ulong WriteLittleEndianBytes(ulong num, int numBytes, Span<byte> outBuf)
		{
			for (; numBytes > 0; numBytes--) {
				outBuf[_i++] = (byte)num;
				num >>= 8;
			}
			return num;
		}

		public void WriteLittleEndianBytes(BigInteger num, int numBytes, Span<byte> outBuf)
		{
			Debug.Assert(outBuf.Length - _i >= numBytes);

			#if !(NETSTANDARD2_0 || NET45 || NET46 || NET47)
			if (num.TryWriteBytes(outBuf.Slice(_i), out int bytesWritten, false, isBigEndian: false)) {
				Debug.Assert(bytesWritten == numBytes);
				return;
			}
			#endif

			if (numBytes <= 8) {
				// Truncate BigInteger: https://stackoverflow.com/questions/74989790/how-to-truncate-a-biginteger-to-int-long-uint-ulong
				WriteLittleEndianBytes((ulong)(num & ulong.MaxValue), numBytes, outBuf);
			} else {
				// Allocating an array here isn't efficient, but I don't know a better way
				var numSpan = num.ToByteArray().AsSpan();
				if (numBytes < numSpan.Length)
					numSpan = numSpan.Slice(0, numBytes);
				numSpan.CopyTo(outBuf.Slice(_i));
			}
		}

		internal readonly static bool IsReversedEndian = (int)BitConverter.DoubleToInt64Bits(1) != 0;

		public void Write(double num)
		{
			// The documentation says "The order of bits in the integer returned by the
			// DoubleToInt64Bits method depends on whether the computer architecture is
			// little-endian or big-endian." I doubt this is meaningful:
			//
			// - The SingleToInt32Bits method doesn't say the same thing.
			// - The implementation uses `Unsafe.BitCast<double, long>` so as long as
			//   the endianness of `double` is the same as `long`, `DoubleToInt64Bits(1.5)`
			//   should be 0x3FF8_0000_0000_0000 regardless of platform endianness.
			//
			// However, I understand there are some ARM chips in which the endianness of
			// floating-point numbers is opposite to the endianness of integers. So this
			// code will detect that case and correct for it, just in case. I have no way
			// to test that code path, though.
			ulong bytes = (ulong) BitConverter.DoubleToInt64Bits(num);
			#if !(NETSTANDARD2_0 || NET45 || NET462 || NET472)
			if (IsReversedEndian)
				bytes = BinaryPrimitives.ReverseEndianness(bytes);
			#endif
			WriteLittleEndianBytes(bytes);
		}

		public void Write(float num)
		{
			#if NETSTANDARD2_0 || NET45 || NET462 || NET472
			// inefficient
			uint bytes = BitConverter.ToUInt32(BitConverter.GetBytes(num), 0);
			#else
			uint bytes = (uint) BitConverter.SingleToInt32Bits(num);
			if (IsReversedEndian)
				bytes = BinaryPrimitives.ReverseEndianness(bytes);
			#endif
			WriteLittleEndianBytes(bytes);
		}

		public void Write(decimal num)
		{
			//
			// TODO: what about endianness?
			//
			int[] arrayOf4 = decimal.GetBits(num); // little-endian on x64
			Span<byte> outBuf = GetOutSpan(16);
			WriteLittleEndianBytes(unchecked((uint)arrayOf4[0]), 4, outBuf);
			WriteLittleEndianBytes(unchecked((uint)arrayOf4[1]), 4, outBuf);
			WriteLittleEndianBytes(unchecked((uint)arrayOf4[2]), 4, outBuf);
			WriteLittleEndianBytes(unchecked((uint)arrayOf4[3]), 4, outBuf);
		}

		public void Write(string? str, ObjectMode mode = ObjectMode.Normal)
		{
			if (str == null)
				WriteNull();
			else
				Write(str.AsSpan());
		}
		public void Write(ReadOnlySpan<char> str)
		{
			// Encoding.UTF8 allows unpaired surrogates. Technically this is called "WTF-8"
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47
			var array = str.ToArray();
			int wtf8size = Encoding.UTF8.GetByteCount(array);
			#else
			int wtf8size = Encoding.UTF8.GetByteCount(str);
			#endif
			// worst-case overhead: 2 bytes for start/end markers + 5 for string length
			int requiredBytes = 7 + wtf8size;

			var outSpan = GetOutSpan(requiredBytes);

			if ((_opt.Markers & SyncBinary.Markers.ListStart) != 0)
				outSpan[_i++] = (byte)'[';

			Write((uint) wtf8size); // length prefix

			#if NETSTANDARD2_0 || NET45 || NET46 || NET47
			var outBytes = Encoding.UTF8.GetBytes(array, 0, array.Length);
			outBytes.CopyTo(outSpan.Slice(_i));
			int wtf8size2 = outBytes.Length;
			#else
			int wtf8size2 = Encoding.UTF8.GetBytes(str, outSpan.Slice(_i));
			#endif
			Debug.Assert(wtf8size == wtf8size2);
			_i += wtf8size;

			if ((_opt.Markers & SyncBinary.Markers.ListEnd) != 0)
				outSpan[_i++] = (byte)']';
		}

		public void WriteBitfield(uint value, uint bitfieldSize)
		{
			// Write the beginning bits of the bitfield
			uint bitsLeft = _bitfieldBitsLeftInByte;
			if (bitsLeft != 0) {
				Debug.Assert(_i > 0);
				if (bitsLeft >= bitfieldSize) {
					_bitfieldBitsLeftInByte = bitsLeft - bitfieldSize;
					_buf.Span[_i - 1] |= (byte)( (value & ((1u << (int)bitfieldSize) - 1)) << (8 - (int)bitsLeft) );
					return;
				} else {
					//_bitfieldBitsLeftInByte = 0 is redundant, as GetOutSpan() does this below
					_buf.Span[_i - 1] |= (byte)( value << (8 - (int)bitsLeft) );
					value >>= (int)bitsLeft;
					bitfieldSize -= bitsLeft;
				}
			}

			// Write the middle bytes of the bitfield
			int minNumBytesLeft = (int)bitfieldSize >> 3;
			var span = GetOutSpan(minNumBytesLeft + 1);
			if (bitfieldSize >= 8) {
				value = WriteLittleEndianBytes(value, minNumBytesLeft, span);
				bitfieldSize &= 7;
			}

			// Write the ending bits of the bitfield
			if (bitfieldSize != 0) {
				Debug.Assert(bitfieldSize < 8);
				span[_i++] = (byte)( value & ((1u << (int)bitfieldSize) - 1) );
				_bitfieldBitsLeftInByte = 8 - bitfieldSize;
			}
		}

		public void WriteBitfield(ulong value, uint bitfieldSize)
		{
			// Write the beginning bits of the bitfield
			uint bitsLeft = _bitfieldBitsLeftInByte;
			if (bitsLeft != 0) {
				Debug.Assert(_i > 0);
				if (bitsLeft >= bitfieldSize) {
					_bitfieldBitsLeftInByte = bitsLeft - bitfieldSize;
					_buf.Span[_i - 1] |= (byte)( ((uint)value & ((1u << (int)bitfieldSize) - 1)) << (8 - (int)bitsLeft) );
					return;
				} else {
					//_bitfieldBitsLeftInByte = 0 is redundant, as GetOutSpan() does this below
					_buf.Span[_i - 1] |= (byte)( (uint)value << (8 - (int)bitsLeft));
					value >>= (int)bitsLeft;
					bitfieldSize -= bitsLeft;
				}
			}

			// Write the middle bytes of the bitfield
			int minNumBytesLeft = (int)bitfieldSize >> 3;
			var span = GetOutSpan(minNumBytesLeft + 1);
			if (bitfieldSize >= 8) {
				value = WriteLittleEndianBytes(value, minNumBytesLeft, span);
				bitfieldSize &= 7;
			}

			// Write the ending bits of the bitfield
			if (bitfieldSize != 0) {
				Debug.Assert(bitfieldSize < 8);
				span[_i++] = (byte)( (uint)value & ((1u << (int)bitfieldSize) - 1));
				_bitfieldBitsLeftInByte = 8 - bitfieldSize;
			}
		}

		public void WriteBitfield(BigInteger value, uint bitfieldSize)
		{
			// Write the beginning bits of the bitfield
			uint bitsLeft = _bitfieldBitsLeftInByte;
			if (bitsLeft != 0) {
				Debug.Assert(_i > 0);
				if (bitsLeft >= bitfieldSize) {
					_bitfieldBitsLeftInByte = bitsLeft - bitfieldSize;
					_buf.Span[_i - 1] |= (byte)( (uint)(value & ((1u << (int)bitfieldSize) - 1)) << (8 - (int)bitsLeft) );
					return;
				} else {
					//_bitfieldBitsLeftInByte = 0 is redundant, as GetOutSpan() does this below
					// Use `(uint)(value & 0xFF)` instead of `(uint)value` to avoid OverflowException
					_buf.Span[_i - 1] |= (byte)( (uint)(value & 0xFF) << (8 - (int)bitsLeft));
					value >>= (int)bitsLeft;
					bitfieldSize -= bitsLeft;
				}
			}

			// Write the middle bytes of the bitfield
			int minNumBytesLeft = (int)bitfieldSize >> 3;
			var span = GetOutSpan(minNumBytesLeft + 1);
			if (bitfieldSize >= 8) {
				WriteLittleEndianBytes(value, minNumBytesLeft, span);
				value >>= minNumBytesLeft << 3; 
				bitfieldSize &= 7;
			}

			// Write the ending bits of the bitfield
			if (bitfieldSize != 0) {
				Debug.Assert(bitfieldSize < 8);
				span[_i++] = (byte)( value & ((1u << (int)bitfieldSize) - 1) );
				_bitfieldBitsLeftInByte = 8 - bitfieldSize;
			}
		}

		internal void WriteTypeTag(string? tag)
		{
			if ((_opt.Markers & SyncBinary.Markers.TypeTag) != 0) {
				GetOutSpan(1)[_i++] = (byte)'T';
			}
			Write(tag);
		}

		public (bool Begun, int Length, object? Object) BeginSubObject(object? childKey, ObjectMode mode, int listLength)
		{
			if (childKey == null && (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull) {
				WriteNull();
				return (false, 0, childKey);
			}

			if (listLength < 0 && (mode & ObjectMode.List) != 0) {
				throw new ArgumentException("No valid listLength was given to SyncBinary.Writer.BeginSubObject");
			}

			Span<byte> span;
			if ((mode & ObjectMode.Deduplicate) != 0)
			{
				Debug.Assert(childKey != null);
				long id = _idGen.GetId(childKey, out bool firstTime);

				if (firstTime) {
					// Write object ID
					span = GetOutSpan(MaxSizeOfInt64 + 2);
					span[_i++] = (byte)'#';
					Write(id);
				} else {
					// Write backreference to object ID
					span = GetOutSpan(MaxSizeOfInt64 + 1);
					span[_i++] = (byte)'@';
					Write(id);

					return (false, 0, childKey); // Skip object that is already written
				}
			}
			else
			{
				span = GetOutSpan(1);
			}

			// Take note than an object has been started
			_stack.Add(mode);

			// Write start marker (if enabled in _opt) and list length (if applicable)
			ObjectMode objectKind = mode & (ObjectMode.List | ObjectMode.Tuple);
			if (objectKind == ObjectMode.Normal)
			{
				if ((_opt.Markers & Markers.ObjectStart) != 0)
					span[_i++] = (Depth & 1) != 0 ? (byte)'(' : (byte)'{';

				return (true, 1, childKey);
			}

			if (objectKind == ObjectMode.List)
			{
				if ((_opt.Markers & Markers.ListStart) != 0)
					span[_i++] = (byte)'[';

				Write(listLength);
			}
			else if (objectKind == ObjectMode.Tuple)
			{
				if ((_opt.Markers & Markers.TupleStart) != 0)
					span[_i++] = (byte)'[';
			}

			return (true, listLength, childKey);
		}

		public void EndSubObject()
		{
			// Write end marker (if enabled in the Options)
			ObjectMode objectKind = _stack.Last & (ObjectMode.List | ObjectMode.Tuple);
			Markers markerMask = objectKind switch {
				ObjectMode.List => Markers.ListEnd,
				ObjectMode.Tuple => Markers.TupleEnd,
				_ => Markers.ObjectEnd,
			};
			if ((_opt.Markers & markerMask) != 0) {
				var span = GetOutSpan(1);
				span[_i++] = objectKind != ObjectMode.Normal
					? (byte)']'
					: (Depth & 1) != 0 ? (byte)')' : (byte)'}';
			}

			_stack.Pop();
		}
	}
}
