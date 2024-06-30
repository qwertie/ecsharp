using static System.Math;
using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Buffers.Binary;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	internal partial class ReaderState
	{
		const string UnexpectedDataStreamFormat = "Unexpected binary data format";

		public ReaderState(IScanner<byte> scanner, Options options)
		{
			_scanner = scanner;
			_opt = options;
		}

		public ReaderState(ReadOnlyMemory<byte> bytes, Options options)
		{
			_frame = new ReadingFrame { Buf = bytes };
			_opt = options;
		}

		private Options _opt;
		private IScanner<byte>? _scanner;
		private Memory<byte> _scannerBuf; // not used by ReaderState; it's passed to _scanner.Read()
		private ReadingFrame _frame;
		private InternalList<StackEntry> _stack = new InternalList<StackEntry>(4);
		public bool IsInsideList => _stack.Last.Type != ObjType.Normal;
		public int Depth => _stack.Count;
		
		// This is only created if an object with an ID (marked with '#') is encountered.
		// It maps previously-encountered object IDs to objects.
		private Dictionary<long, object>? _objects { get; set; }

		// An error that left the stream unreadable
		protected Exception? _fatalError;

		#region Helper types

		private struct ReadingFrame
		{
			// The buffer being read from (something returned by _scanner if it isn't null)
			public ReadOnlyMemory<byte> Buf;
			// The current position as an index into Buf.
			public int Index;
			// Location within the JSON file of Buf.Span[0] (used for error reporting)
			public long PositionOfBuf0;
			// A location in Buf that shouldn't be unloaded when reading further into the
			// file (int.MaxValue if none)
			public int ObjectStartIndex; // = int.MaxValue;

			public ReadingPointer Pointer => new ReadingPointer {
				Buf = Buf.Span,
				Index = Index,
			};
	}

		protected ref struct ReadingPointer
		{
			/// <summary>The part of the file that is currently loaded.</summary>
			public ReadOnlySpan<byte> Buf;
			/// <summary>The currrent position as an index into Buf.</summary>
			public int Index;
			/// <summary>Current byte in data stream</summary>
			public byte Byte => Buf[Index];
			public int BytesLeft => Buf.Length - Index;

			public ReadOnlySpan<byte> Span => Buf.Slice(Index);
			public ReadOnlySpan<byte> Slice(int offset, int size) => Buf.Slice(Index + offset, size);
			//public ReadOnlySpan<byte> FromOffset(int offset) => Buf.Slice(Index + offset);
		}

		protected enum ObjType : byte
		{
			Normal = ObjectMode.Normal,
			List = ObjectMode.List,
			Tuple = ObjectMode.Tuple
		}

		protected struct StackEntry
		{
			public long Id;
			public bool HasId;
			public ObjType Type;
		}

		#endregion

		#region Input buffer management: AutoRead, ExpectBytes, Commit

		// The scanner could choose a much larger size, but this is the minimum we'll tolerate
		const int DefaultMinimumScanSize = 32;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ExpectBytes(ref ReadingPointer cur, int requiredBytes)
		{
			if (!AutoRead(ref cur, requiredBytes))
				ThrowUnexpectedEOF(cur.Index);
		}

		// Ensures that at least `extraLookahead + 1` bytes are available in cur.Buf
		// starting at cur.Index. On return, _i < _buf.Length if it returns true.
		// Returns false if the request could not be satisfied.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool AutoRead(ref ReadingPointer cur, int requiredBytes)
		{
			Debug.Assert(cur.Buf == _frame.Buf.Span);
			if ((uint)(cur.Index + requiredBytes) <= (uint)cur.Buf.Length)
				return true;

			return ReadMoreBytes(ref cur, requiredBytes);
		}

		// Reads new data into _frame.Buf if possible
		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool ReadMoreBytes(ref ReadingPointer cur, int requiredBytes)
		{
			Debug.Assert(cur.BytesLeft < requiredBytes);
			if (_scanner == null)
				return false;

			int requestSize = Max(requiredBytes, DefaultMinimumScanSize);
			int skip = Min(cur.Index, _frame.ObjectStartIndex);

			_frame.Buf = _scanner.Read(skip, (cur.Index -= skip) + requestSize, ref _scannerBuf);

			if (_frame.ObjectStartIndex != int.MaxValue)
				_frame.ObjectStartIndex -= skip;

			cur.Buf = _frame.Buf.Span;
			return cur.BytesLeft >= requiredBytes;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Commit(in ReadingPointer cur)
		{
			Debug.Assert(_frame.Buf.Span == cur.Buf);
			_frame.Index = cur.Index;
		}

		#endregion

		#region BeginSubObject/EndSubObject

		internal (bool Begun, int Length, object? Object) BeginSubObject(ObjectMode mode, int listLength)
		{
			var cur = _frame.Pointer;
			ExpectBytes(ref cur, 1);

			// Check for null
			byte firstByte = cur.Byte;
			if (firstByte == 0xFF)
			{
				if ((mode & ObjectMode.NotNull) != 0)
					ThrowError(cur.Index, "unexpected null value");

				cur.Index++;
				Commit(cur);
				return (false, 0, null);
			}

			ObjectMode objectKind = mode & (ObjectMode.List | ObjectMode.Tuple);
			Markers markerMask = (Markers)(1 << (int)objectKind);

			long? refId = null;
			byte? expectedStartMarker = null;
			bool isPossibleBackRef = firstByte == (byte)'@';

			if ((_opt.Markers & markerMask) != 0)
				expectedStartMarker = objectKind == ObjectMode.Normal
					? ((Depth & 1) != 0 ? (byte)'{' : (byte)'(')
					: (byte)'[';

			// Check for deduplicated objects
			if (((mode & ObjectMode.Deduplicate) != 0 || expectedStartMarker.HasValue) &&
				(firstByte == (byte)'#' || isPossibleBackRef))
			{
				cur.Index++;
				refId = DecodeIntOrNull<long>(ref cur) ??
					throw NewError(cur.Index, "missing reference id for deduplicated object");

				// Return back-reference
				if (isPossibleBackRef)
				{
					if (_objects is not null && 
						_objects.TryGetValue(refId.Value, out var obj)) 
					{
						Commit(cur);
						return (false, 0, obj);
					}

					ThrowError(cur.Index, "no object found for back-reference");
				}
			}

			// Check for start marker
			if (expectedStartMarker.HasValue) 
			{
				if (cur.Byte != expectedStartMarker)
					ThrowError(cur.Index, "invalid start marker");
				cur.Index++;
			}

			// Get valid length
			if (objectKind == ObjectMode.List)
				listLength = DecodeIntOrNull<int>(ref cur)
					?? throw NewError(cur.Index, "missing prefixed length for list");
			else if (objectKind == ObjectMode.Normal)
				listLength = 1;

			Commit(cur);
			_stack.Add(new()
			{
				Id = refId.GetValueOrDefault(),
				HasId = refId.HasValue,
				Type = (ObjType)objectKind,
			});

			return (true, listLength, null);
		}

		public void EndSubObject()
		{
			var last = _stack.Last;
			Markers markerMask = (Markers)(16 << (int)last.Type);

			if ((_opt.Markers & markerMask) != 0)
			{
				var cur = _frame.Pointer;
				var endMarker = last.Type == ObjType.Normal
				   ? ((Depth & 1) != 0 ? (byte)')' : (byte)'}')
				   : (byte)']';

				if (cur.Byte != endMarker)
					ThrowError(cur.Index, "invalid end marker");

				cur.Index++;
				Commit(cur);
			}

			_stack.Pop();
		}

		#endregion

		#region String reader
		internal string? ReadStringOrNull()
		{
			var (begun, strLength, obj) = BeginSubObject(ObjectMode.List, -1);
			if (!begun)
				return (string?) obj;

			var cur = _frame.Pointer;
			ExpectBytes(ref cur, strLength);

			var span = cur.Buf.Slice(cur.Index, strLength);
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47
			var str = Encoding.UTF8.GetString(span.ToArray());
			#else
			var str = Encoding.UTF8.GetString(span);
			#endif

			cur.Index += strLength;
			Commit(cur);
			EndSubObject();
			return str;
		}

		#endregion

		#region Floating-point reader

		internal float? ReadFloatOrNull()
		{
			var cur = _frame.Pointer;
			ExpectBytes(ref cur, 4);

			var num = LittleEndianBytesToUInt32(cur.Span);
			cur.Index += 4;
			Commit(cur);

			if (num == FloatNullBitPattern)
				return null;

			#if NETSTANDARD2_0 || NET45 || NET46 || NET47 || NET48
			return BitConverter.ToSingle(BitConverter.GetBytes(num), 0);
			#else
			return BitConverter.Int32BitsToSingle((int) num);
			#endif
		}

		internal double? ReadDoubleOrNull()
		{
			var cur = _frame.Pointer;
			ExpectBytes(ref cur, 8);

			var num = LittleEndianBytesToUInt64(cur.Span);
			cur.Index += 8;
			Commit(cur);

			if (num == DoubleNullBitPattern)
				return null;

			return BitConverter.Int64BitsToDouble((long) num);
		}

		internal decimal? ReadDecimalOrNull()
		{
			var cur = _frame.Pointer;
			ExpectBytes(ref cur, 16);

			var i = cur.Index;
			var the13thByte = cur.Buf[i + 12];
			var the14thByte = cur.Buf[i + 13];
			decimal? value = null;

			if (the13thByte != 0xFF && the14thByte != 0xFF)
			{
				var bits = new int[4] {
					unchecked((int) LittleEndianBytesToUInt32(cur.Span)),
					unchecked((int) LittleEndianBytesToUInt32(cur.Buf.Slice(i + 4))),
					unchecked((int) LittleEndianBytesToUInt32(cur.Buf.Slice(i + 8))),
					unchecked((int) LittleEndianBytesToUInt32(cur.Buf.Slice(i + 12)))
				};
				value = new decimal(bits);
			}
			else if (the13thByte != 0x00 && the14thByte != 0x00)
				ThrowError(cur.Index, "invalid data for decimal");

			cur.Index += 16;
			Commit(cur);
			return value;
		}

		internal float ReadFloat() 
			=> ReadFloatOrNull() ?? UnexpectedNull<float>();

		internal double ReadDouble() 
			=> ReadDoubleOrNull() ?? UnexpectedNull<float>();

		internal decimal ReadDecimal() 
			=> ReadDecimalOrNull() ?? UnexpectedNull<decimal>();

		static uint LittleEndianBytesToUInt32(ReadOnlySpan<byte> span)
		{
			#if NETSTANDARD1_6 || NET45 || NET46 || NET47 || NET48
			return (uint)(span[0] + (span[1] << 8) + (span[2] << 16) + (span[3] << 24));
			#else
			return BinaryPrimitives.ReadUInt32LittleEndian(span);
			#endif
		}

		static ulong LittleEndianBytesToUInt64(ReadOnlySpan<byte> span)
		{
			#if NETSTANDARD1_6 || NET45 || NET46 || NET47 || NET48
			return LittleEndianBytesToUInt32(span) + unchecked((ulong)LittleEndianBytesToUInt32(span.Slice(4)) << 32);
			#else
			return BinaryPrimitives.ReadUInt64LittleEndian(span);
			#endif
		}

		#endregion

		#region Variable-length integer readers

		internal BigInteger? ReadBigIntegerOrNull()
		{
			var cur = _frame.Pointer;
			if (cur.Byte == 0xFE)
			{
				BigInteger value = DecodeLargestIntFormat(ref cur);
				Commit(cur);
				return value;
			} 
			else
			{
				BigInteger? value = (BigInteger?)DecodeIntOrNull<long>(ref cur);
				Commit(cur);
				return value;
			}
		}

		internal BigInteger ReadBigInteger()
			=> ReadBigIntegerOrNull() ?? UnexpectedNull<BigInteger>();

		// Here we assume the JIT optimizes away tests like `if (typeof(TInt) == typeof(int))`.
		// Reads an integer from the data stream. `TInt` MUST be int, uint, long or ulong.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal TInt ReadNormalInt<TInt>() where TInt : struct
		{
			var cur = _frame.Pointer;
			TInt? value = DecodeIntOrNull<TInt>(ref cur);
			Commit(cur);
			return value.HasValue ? value.Value : UnexpectedNull<TInt>();
		}

		// Reads an integer/null from the data stream. `TInt` MUST be int, uint, long or ulong.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal TInt? ReadNormalIntOrNull<TInt>() where TInt : struct
		{
			var cur = _frame.Pointer;
			TInt? value = DecodeIntOrNull<TInt>(ref cur);
			Commit(cur);
			return value;
		}

		// Reads a small integer/null from the data stream. `TInt` MUST be byte, sbyte, short or ushort.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal TShort? ReadSmallIntOrNull<TShort>() where TShort : struct
		{
			var cur = _frame.Pointer;
			int value;
			if (typeof(TShort) == typeof(byte) || typeof(TShort) == typeof(ushort))
			{
				var uvalue = DecodeIntOrNull<uint>(ref cur);
				if (!uvalue.HasValue)
				{
					Commit(cur);
					return null;
				}
				value = (int)uvalue.Value;
			}
			else
			{
				var ivalue = DecodeIntOrNull<int>(ref cur);
				if (!ivalue.HasValue)
				{
					Commit(cur);
					return null;
				}
				value = ivalue.Value;
			}

			MaybeThrowIntegerOverflowIf(
				typeof(TShort) == typeof(short) && (short)value != value ||
				typeof(TShort) == typeof(ushort) && (ushort)value != value ||
				typeof(TShort) == typeof(sbyte) && (sbyte)value != value ||
				typeof(TShort) == typeof(byte) && (byte)value != value,
				typeof(TShort).Name, _frame.Index);

			Commit(cur);
			return ShortenInt<TShort>(value);
		}

		// Reads a small integer from the data stream. `TInt` MUST be byte, sbyte, short or ushort.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal TShort ReadSmallInt<TShort>() where TShort : struct
		{
			var cur = _frame.Pointer;
			int value;
			if (typeof(TShort) == typeof(byte) || typeof(TShort) == typeof(ushort))
			{
				var uvalue = DecodeIntOrNull<uint>(ref cur);
				if (!uvalue.HasValue)
					return UnexpectedNull<TShort>();
				value = (int)uvalue.Value;
			}
			else
			{
				var ivalue = DecodeIntOrNull<int>(ref cur);
				if (!ivalue.HasValue)
					return UnexpectedNull<TShort>();
				value = ivalue.Value;
			}

			MaybeThrowIntegerOverflowIf(
				typeof(TShort) == typeof(short) && (short)value != value ||
				typeof(TShort) == typeof(ushort) && (ushort)value != value ||
				typeof(TShort) == typeof(sbyte) && (sbyte)value != value ||
				typeof(TShort) == typeof(byte) && (byte)value != value,
				typeof(TShort).Name, _frame.Index);

			Commit(cur);
			return ShortenInt<TShort>(value);
		}

		// Decodes an integer from the data stream. `TInt` MUST be int, uint, long, ulong or BigInteger.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		TInt? DecodeIntOrNull<TInt>(ref ReadingPointer cur) where TInt : struct
		{
			ExpectBytes(ref cur, 1);

			byte firstByte = cur.Byte;
			if (firstByte < 0xFE)
			{
				cur.Index++;
				if (firstByte < 0x80)
				{
					// Simply return firstByte if TInt is unsigned, otherwise sign-extend it
					if (typeof(TInt) == typeof(uint))
						return (uint)firstByte is TInt r ? r : default;
					else if (typeof(TInt) == typeof(ulong))
						return (ulong)firstByte is TInt r ? r : default;
					else if (typeof(TInt) == typeof(int))
						return ((int)firstByte << 25) >> 25 is TInt r ? r : default;
					else if (typeof(TInt) == typeof(long))
						return (long)(((int)firstByte << 25) >> 25) is TInt r ? r : default;
					Debug.Fail("unreachable");
					return default;
				}
				else
				{
					// Read small-format variable-length number (sizeOfRest <= 7). We
					// always read it as `long` because if sizeOfRest >= 4, a 32-bit
					// version of this would require more branches. Any speed advantage of
					// using 32-bit math would probably be negated by branch misprediction,
					// even on mobile processors.
					int sizeOfRest = LeadingOneCount(firstByte) + 1;
					
					ExpectBytes(ref cur, sizeOfRest);

					int extraBits = firstByte & (0x7F >> sizeOfRest);

					long highBitsOfNumber;
					if (typeof(TInt) == typeof(int) || typeof(TInt) == typeof(long)) {
						int shift = sizeOfRest + 57;
						highBitsOfNumber = (long)extraBits << shift >> (shift - sizeOfRest * 8);
					} else { // unsigned
						highBitsOfNumber = (long)extraBits << sizeOfRest * 8;
					}
						
					long number = highBitsOfNumber | (long)ReadRemainingBytesAsBigEndian(ref cur, sizeOfRest);

					if (typeof(TInt) == typeof(int))
						MaybeThrowIntegerOverflowIf((int)number != number, "Int32", cur.Index);
					if (typeof(TInt) == typeof(uint))
						MaybeThrowIntegerOverflowIf((uint)number != number, "UInt32", cur.Index);

					return FromLong<TInt>(number);
				}
			}
			else if (firstByte == 0xFF)
			{
				cur.Index++;
				return null;
			}
			else
			{
				long number = DecodeLargeFormatInt64(ref cur, typeof(TInt) == typeof(int) || typeof(TInt) == typeof(long));
				return FromLong<TInt>(number);
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		long DecodeLargeFormatInt64(ref ReadingPointer cur, bool signed)
		{
			int integerSize = ReadIntLengthPrefix(ref cur);
			if (integerSize > 8) {
				if (!_opt.Read.SilentlyTruncateLargeNumbers)
					ExpectZeroes(cur.Index, cur.Slice(0, integerSize - 8));
				
				cur.Index += integerSize - 8;
				integerSize = 8;
			}

			long number = (long) ReadRemainingBytesAsBigEndian(ref cur, integerSize);
			cur.Index += integerSize;

			if (signed) {
				// Sign-extend the number
				int shiftAmount = 64 - integerSize * 8;
				return number << shiftAmount >> shiftAmount;
			} else {
				return number;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		BigInteger DecodeLargestIntFormat(ref ReadingPointer cur)
		{
			int integerSize = ReadIntLengthPrefix(ref cur);
			var span = cur.Buf.Slice(cur.Index, integerSize);
			cur.Index += integerSize;

			#if NETSTANDARD2_0 || NET45 || NET46 || NET47
			var bytesArray = span.ToArray();
			Array.Reverse(bytesArray);
			return new BigInteger(bytesArray);
			#else
			return new BigInteger(span, isUnsigned: false, isBigEndian: true);
			#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ReadIntLengthPrefix(ref ReadingPointer cur)
		{
			// Read length prefix
			Debug.Assert(cur.Byte == 0xFE);
			cur.Index++;

			if (!AutoRead(ref cur, 2))
			{
				ThrowUnexpectedEOF(_frame.Index);
			}
			if (cur.Byte >= 0xFE)
			{
				ThrowError(cur.Index, cur.Byte == 0xFF
					? $"{UnexpectedDataStreamFormat}; number length is null"
					: $"{UnexpectedDataStreamFormat}; length prefix is itself length-prefixed");
			}

			int integerSize = (int)DecodeIntOrNull<uint>(ref cur)!.Value;
			if (integerSize > _opt.MaxNumberSize)
			{
				ThrowError(_frame.Pointer.Index, $"{UnexpectedDataStreamFormat}; length prefix is too large: {integerSize}");
			}

			ExpectBytes(ref cur, integerSize);
			return integerSize;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ulong ReadRemainingBytesAsBigEndian(ref ReadingPointer cur, int sizeOfRemaining)
		{
			Debug.Assert(sizeOfRemaining <= 8 && sizeOfRemaining > 0);
			Debug.Assert(cur.BytesLeft >= sizeOfRemaining);

			if (cur.BytesLeft >= 8)
			{
				// Fast branchless path: read 8 bytes, then discard any that
				// are not part of the number. However, special logic is needed when
				// reading a 32-bit number if the number size is 5 bytes or more...
				ulong number = BigEndianBytesToUInt64(cur.Span) >> ((8 - sizeOfRemaining) << 3);
				cur.Index += sizeOfRemaining;
				return number;
			}
			return ReadInLoop(ref cur, sizeOfRemaining);

			[MethodImpl(MethodImplOptions.NoInlining)]
			static ulong ReadInLoop(ref ReadingPointer cur, int sizeOfRemaining)
			{
				// Slow path for tiny buffer: read bytes in a loop
				ulong number = 0;
				for (int i = 0; i < sizeOfRemaining; i++) {
					number = (number << 8) + cur.Byte;
					cur.Index++;
				}
				return number;
			}
		}

		void ExpectZeroes(int index, ReadOnlySpan<byte> span)
		{
			for (int i = 0; i < span.Length; i++) {
				if (span[i] != 0)
					ThrowError(index + i, $"{UnexpectedDataStreamFormat}; integer is too large");
			}
		}

		TInt UnexpectedNull<TInt>(bool fatal = false) where TInt : struct
		{
			if (!_opt.Read.ReadNullPrimitivesAsDefault)
				ThrowError(_frame.Index, $"{UnexpectedDataStreamFormat}; unexpected null", fatal);

			return default;
		}

		static uint BigEndianBytesToUInt32(ReadOnlySpan<byte> span)
		{
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47 || NET48
			return (uint)(span[3] + (span[2] << 8) + (span[1] << 16) + (span[0] << 24));
			#else
			return (uint)BinaryPrimitives.ReadInt32BigEndian(span);
			#endif
		}

		static ulong BigEndianBytesToUInt64(ReadOnlySpan<byte> span)
		{
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47 || NET48
			return unchecked((ulong)BigEndianBytesToUInt32(span) << 32) + BigEndianBytesToUInt32(span.Slice(4));
			#else
			return (ulong)BinaryPrimitives.ReadInt64BigEndian(span);
			#endif
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//static TInt BigEndianBytesToUInt<TInt>(ReadOnlySpan<byte> span, int integerSize)
		//{
		//	if (typeof(TInt) == typeof(int)) {
		//		return (int)(BigEndianBytesToUInt32(span) >> (32 - integerSize * 8)) is TInt r ? r : default;
		//	} else if (typeof(TInt) == typeof(uint)) {
		//		return (uint)(BigEndianBytesToUInt32(span) >> (32 - integerSize * 8)) is TInt r ? r : default;
		//	} else if (typeof(TInt) == typeof(long)) {
		//		return (long)(BigEndianBytesToUInt64(span) >> (64 - integerSize * 8)) is TInt r ? r : default;
		//	} else if (typeof(TInt) == typeof(ulong)) {
		//		return (ulong)(BigEndianBytesToUInt64(span) >> (64 - integerSize * 8)) is TInt r ? r : default;
		//	}
		//	Debug.Fail("Unreachable");
		//	return default;
		//}

		#endregion

		#region Generic math functions
		// When increase the minimum .NET version to .NET 7, we can switch to generic
		// numerics instead (but will have to retest performance)

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//static int SizeOf<TInt>()
		//{
		//	if (typeof(TInt) == typeof(int) || typeof(TInt) == typeof(uint))
		//		return 4;
		//	else if (typeof(TInt) == typeof(long) || typeof(TInt) == typeof(ulong))
		//		return 8;
		//	else
		//		return System.Runtime.InteropServices.Marshal.SizeOf<TInt>();
		//}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static TInt FromLong<TInt>(long number) where TInt: struct
		{
			if (typeof(TInt) == typeof(int))
				return (int)number is TInt r ? r : default;
			else if (typeof(TInt) == typeof(uint))
				return (uint)number is TInt r ? r : default;
			else if (typeof(TInt) == typeof(long))
				return (long)number is TInt r ? r : default;
			else if (typeof(TInt) == typeof(ulong))
				return (ulong)number is TInt r ? r : default;
			Debug.Fail("Unreachable");
			return default;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static TInt ShortenInt<TInt>(int number) where TInt: struct
		{
			if (typeof(TInt) == typeof(short))
				return (short)number is TInt r ? r : default;
			else if (typeof(TInt) == typeof(ushort))
				return (ushort)number is TInt r ? r : default;
			else if (typeof(TInt) == typeof(sbyte))
				return (sbyte)number is TInt r ? r : default;
			else if (typeof(TInt) == typeof(byte))
				return (byte)number is TInt r ? r : default;
			Debug.Fail("Unreachable");
			return default;
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//static TInt ShiftRight<TInt>(TInt num, int amount)
		//{
		//	if (num is int i32) {
		//		return (i32 >> amount) is TInt r ? r : default;
		//	} else if (num is uint u32) {
		//		return (u32 >> amount) is TInt r ? r : default;
		//	} else if (num is long i64) {
		//		return (i64 >> amount) is TInt r ? r : default;
		//	} else if (num is ulong u64) {
		//		return (u64 >> amount) is TInt r ? r : default;
		//	}
		//	Debug.Fail("Unreachable");
		//	return default;
		//}

		public static int LeadingOneCount(byte b)
		{
			#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NET45 || NET46 || NET47 || NET48
				int result = 0;
				int i = b << 24;
				if (i >> 28 == 0b1111)
				{
					i <<= 4;
					result += 4;
				}
				if (i >> 30 == 0b11)
				{
					i <<= 2;
					result += 2;
				}
				if (i >> 31 == 1)
				{
					i <<= 1;
					result += 1;
					if (result == 7 && i == 1)
						return 8;
				}
				return result;
			#else
				return BitOperations.LeadingZeroCount((uint)~(b << 24));
			#endif
		}

		#endregion

		#region Error throwers

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void MaybeThrowIntegerOverflowIf(bool overflow, string expectedSize, int curIndex)
		{
			if (!_opt.Read.SilentlyTruncateLargeNumbers && overflow)
				ThrowIntegerOverflow(expectedSize, curIndex);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void ThrowIntegerOverflow(string expectedSize, int curIndex)
		{
			ThrowError(curIndex, $"{UnexpectedDataStreamFormat}; number is too large (expected {expectedSize})", fatal: false);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ThrowUnexpectedEOF(int index)
			=> ThrowError(index, "Data stream ended unexpectedly");

		[MethodImpl(MethodImplOptions.NoInlining)]
		[DoesNotReturn]
		protected void ThrowError(int i, string msg, bool fatal = false)
			=> throw NewError(i, msg, fatal);

		[MethodImpl(MethodImplOptions.NoInlining)]
		[DoesNotReturn]
		protected void ThrowError(long position, string msg, bool fatal = false)
			=> throw NewError(position, msg, fatal);

		protected Exception NewError(int i, string msg, bool fatal = false)
			=> NewError(_frame.PositionOfBuf0 + i, msg, fatal);

		[MethodImpl(MethodImplOptions.NoInlining)]
		protected Exception NewError(long position, string msg, bool fatal = false)
		{
			if (_fatalError != null)
				return _fatalError; // New error is just a symptom of the old error; rethrow

			string msg2;
			int index = (int)(position - _frame.PositionOfBuf0);
			if ((uint)index >= (uint)_frame.Buf.Length) {
				msg2 = "{0} (at byte {1})".Localized(msg, position);
			} else {
				byte b = _frame.Buf.Span[index];
				msg2 = "{0} (at byte {1} '{2}')".Localized(msg, position, b < 32 || b >= 127 ? "0x" + b.ToString("X") : (char)b);
			}

			var exc = new FormatException(msg2);
			exc.Data["position"] = position;
			exc.Data["recoverable"] = false;

			if (fatal)
				_fatalError = exc;
			return exc;
		}

		#endregion

		internal void SetCurrentObject(object value)
		{
			if (_stack.Count != 0) {
				var topOfStack = _stack.Last;
				if (topOfStack.HasId) {
					_objects ??= new Dictionary<long, object>();
					_objects[topOfStack.Id] = value;
				}
			}
		}
	}
}
