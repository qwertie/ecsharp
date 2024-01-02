using static System.Math;
using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Loyc.Collections.Impl;
using System.Buffers.Binary;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	internal class ReaderState
	{
		const string UnexpectedDataStreamFormat = "Unexpected format in binary data stream";

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
		public bool IsInsideList;
		public int Depth { get; internal set; }
		
		// This is only created if an object with an ID (marked with '#') is encountered.
		// It maps previously-encountered object IDs to objects.
		private Dictionary<int, object>? _objects { get; set; }

		// An error that left the stream unreadable
		protected Exception? _fatalError;

		private struct ReadingFrame
		{
			// The buffer being read from (something returned by _scanner if it isn't null)
			public ReadOnlyMemory<byte> Buf;
			// The currrent position as an index into Buf.
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

		protected struct StackEntry
		{
			public int Id;
			public bool HasId;
			public bool IsList;
		}

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

		private void Commit(ref ReadingPointer cur)
		{
			Debug.Assert(_frame.Buf.Span == cur.Buf);
			_frame.Index = cur.Index;
		}

		#endregion

		internal void SetCurrentObject(object value)
		{
			if (_stack.Count != 0) {
				var topOfStack = _stack.Last;
				if (topOfStack.HasId) {
					_objects ??= new Dictionary<int, object>();
					_objects[topOfStack.Id] = value;
				}
			}
		}

		internal (bool Begun, object? Object) BeginSubObject(ObjectMode mode, int tupleLength)
		{
			//if (childKey == null && (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull) {
			//	WriteNull();
			//	return (false, childKey);
			//}

			//if (listLength < 0 && (mode & ObjectMode.List) != 0) {
			//	throw new ArgumentException("No valid listLength was given to SyncBinary.Writer.BeginSubObject");
			//}

			//Span<byte> span;
			//if ((mode & ObjectMode.Deduplicate) != 0) {
			//	Debug.Assert(childKey != null);
			//	long id = _idGen.GetId(childKey, out bool firstTime);

			//	if (firstTime) {
			//		// Write object ID
			//		span = GetOutSpan(MaxSizeOfInt64 + 2);
			//		span[_i++] = (byte)'#';
			//		Write(id);
			//	} else {
			//		// Write backreference to object ID
			//		span = GetOutSpan(MaxSizeOfInt64 + 1);
			//		span[_i++] = (byte)'@';
			//		Write(id);

			//		return (false, childKey); // Skip object that is already written
			//	}
			//} else {
			//	span = GetOutSpan(1);
			//}

			//// Take note than an object has been started
			//_stack.Add(mode);

			//// Write start marker (if enabled in _opt) and list length (if applicable)
			//ObjectMode objectKind = mode & (ObjectMode.List | ObjectMode.Tuple);
			//if (objectKind == ObjectMode.Normal) {
			//	if ((_opt.Markers & Markers.ObjectStart) != 0)
			//		span[_i++] = (Depth & 1) != 0 ? (byte)'(' : (byte)'{';
			//} else if (objectKind == ObjectMode.List) {
			//	if ((_opt.Markers & Markers.ListStart) != 0)
			//		span[_i++] = (byte)'[';

			//	Write(listLength);
			//} else if (objectKind == ObjectMode.Tuple) {
			//	if ((_opt.Markers & Markers.TupleStart) != 0)
			//		span[_i++] = (byte)'[';
			//}

			//return (true, childKey);
			throw new NotImplementedException();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public sbyte? ReadInt8OrNull()
		{
			var cur = _frame.Pointer;
			int? number = DecodeInt32OrNull(ref cur);
			if (number == null) {
				Commit(ref cur);
				return null;
			} else {
				sbyte truncated = (sbyte)number.Value;
				if (truncated != number.Value && !_opt.Read.SilentlyTruncateLargeNumbers)
					ThrowError(cur.Index, $"{UnexpectedDataStreamFormat}; number is too large (expected 8 bits)", fatal: false);
				Commit(ref cur);
				return truncated;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public short? ReadInt16OrNull()
		{
			var cur = _frame.Pointer;
			int? number = DecodeInt32OrNull(ref cur);
			if (number == null) {
				Commit(ref cur);
				return null;
			} else {
				short truncated = (short)number.Value;
				if (truncated != number.Value && !_opt.Read.SilentlyTruncateLargeNumbers)
					ThrowError(cur.Index, $"{UnexpectedDataStreamFormat}; number is too large (expected 16 bits)", fatal: false);
				Commit(ref cur);
				return truncated;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int? ReadInt32OrNull()
		{
			var cur = _frame.Pointer;
			int? number = DecodeInt32OrNull(ref cur);
			Commit(ref cur);
			return number;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long? ReadInt64OrNull()
		{
			var cur = _frame.Pointer;
			long? number = DecodeInt64OrNull(ref cur);
			Commit(ref cur);
			return number;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public short ReadInt16()
		{
			int number = DecodeInt32(out var cur);
			short truncated = (short)number;
			ThrowSmallIntegerOverflowIf(truncated != number, "16 bits", cur.Index);
			Commit(ref cur);
			return truncated;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ThrowSmallIntegerOverflowIf(bool overflow, string expected, int curIndex)
		{
			if (overflow && !_opt.Read.SilentlyTruncateLargeNumbers)
				ThrowError(curIndex, $"{UnexpectedDataStreamFormat}; number is too large (expected {expected})", fatal: false);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ReadInt32()
		{
			int number = DecodeInt32(out var cur);
			Commit(ref cur);
			return number;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public long ReadInt64()
		{
			var cur = _frame.Pointer;
			long? value = DecodeInt64OrNull(ref cur);
			long number = value is null ? UnexpectedNull() : value.Value;
			Commit(ref cur);
			return number;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int DecodeInt32(out ReadingPointer cur)
		{
			cur = _frame.Pointer;
			int? value = DecodeInt32OrNull(ref cur);
			return value is null ? UnexpectedNull() : value.Value;
		}

		int? DecodeInt32OrNull(ref ReadingPointer cur)
		{
			// TODO: optimize this by duplicating code of ReadInt64OrNull
			long? value = DecodeInt64OrNull(ref cur);
			if (value is null)
				return null;
			
			int truncated = unchecked((int)value);
			if (truncated != value && !_opt.Read.SilentlyTruncateLargeNumbers)
				ThrowError(cur.Index, $"{UnexpectedDataStreamFormat}; number is too large: {value}", fatal: false);

			return truncated;
		}

		long? DecodeInt64OrNull(ref ReadingPointer cur)
		{
			ExpectBytes(ref cur, 1);

			var firstByte = cur.Byte;
			if (firstByte < 0xFE)
			{
				cur.Index++;
				if (firstByte < 0x80)
				{
					return firstByte;
				}
				else
				{
					// Read small-format variable-length number
					int integerSize = LeadingOneCount(firstByte) + 1;
					long highBitsOfNumber = (firstByte & (0xFF >> integerSize)) << (integerSize * 8);

					if (AutoRead(ref cur, integerSize)) {
						return highBitsOfNumber |
							ReadRemainingBytesAsBigEndianInt64(ref cur, integerSize);
					} else {
						ThrowUnexpectedEOF(_frame.Index);
						return 0;
					}
				}
			}
			else if (firstByte == 0xFF)
			{
				return null;
			}
			else
			{
				return ReadLargeFormatInt64(ref cur);
			}
		}

		long ReadLargeFormatInt64(ref ReadingPointer cur)
		{
			// Read length prefix
			Debug.Assert(cur.Byte == 0xFE);
			cur.Index++;

			if (AutoRead(ref cur, 1) && cur.Byte >= 0xFE) {
				ThrowError(cur.Index, cur.Byte == 0xFF
					? $"{UnexpectedDataStreamFormat}; number length is null"
					: $"{UnexpectedDataStreamFormat}; length prefix is itself length-prefixed");
			}

			int integerSize = DecodeInt32OrNull(ref cur).Value;

			if (integerSize > _opt.MaxNumberSize) {
				ThrowError(_frame.Pointer.Index, $"{UnexpectedDataStreamFormat}; length prefix is too large: {integerSize}");
			}

			// Read the integer itself
			ExpectBytes(ref cur, integerSize);

			if (integerSize > 8) {
				if (!_opt.Read.SilentlyTruncateLargeNumbers)
					ExpectZeroes(cur.Index, cur.Slice(0, integerSize - 8));
				
				cur.Index += integerSize - 8;
				integerSize = 8;
			}

			long number = ReadRemainingBytesAsBigEndianInt64(ref cur, integerSize);
			cur.Index += integerSize;
			return number;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		long ReadRemainingBytesAsBigEndianInt64(ref ReadingPointer cur, int integerSize)
		{
			Debug.Assert(integerSize <= 8);
			Debug.Assert(cur.BytesLeft >= integerSize);

			if (cur.BytesLeft >= 8) {
				// Fast branchless path: read 8 bytes, then discard any that
				// are not part of the number.
				long number = BigEndianBytesToInt64(cur.Span);
				cur.Index += integerSize;

				return number >> (64 - integerSize * 8);
			} else {
				// Slow path for tiny buffer: read bytes in a loop
				long number = 0;
				for (int i = 0; i < integerSize; i++) {
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

		int BigEndianBytesToInt32(ReadOnlySpan<byte> span)
		{
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47 || NET48
			return span[3] + (span[2] << 8) + (span[1] << 16) + (span[0] << 24);
			#else
			return BinaryPrimitives.ReadInt32BigEndian(span);
			#endif
		}

		long BigEndianBytesToInt64(ReadOnlySpan<byte> span)
		{
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47 || NET48
			return unchecked((long)BigEndianBytesToInt32(span) << 32) + BigEndianBytesToInt32(span.Slice(4));
			#else
			return BinaryPrimitives.ReadInt64BigEndian(span);
			#endif
		}

		public static int LeadingOneCount(byte b)
		{
			#if NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_0 || NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_1 || NET45 || NET46 || NET47 || NET48
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
				return BitOperations.LeadingZeroCount(~(b << 24));
			#endif
		}

		private int UnexpectedNull(bool fatal = false)
		{
			if (!_opt.Read.ReadNullPrimitivesAsDefault)
				ThrowError(_frame.Index, $"{UnexpectedDataStreamFormat}; unexpected null", fatal);

			return 0;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ThrowUnexpectedEOF(int index)
			=> ThrowError(index, "Data stream ended unexpectedly");

		[MethodImpl(MethodImplOptions.NoInlining)]
		protected void ThrowError(int i, string msg, bool fatal = false)
			=> throw NewError(i, msg, fatal);

		[MethodImpl(MethodImplOptions.NoInlining)]
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

			var exc = new FormatException(msg);
			exc.Data["position"] = position;
			exc.Data["recoverable"] = false;

			if (fatal)
				_fatalError = exc;
			return exc;
		}
	}
}
