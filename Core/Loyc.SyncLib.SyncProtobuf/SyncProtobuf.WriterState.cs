using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	/// <summary>The mutable state behind <see cref="SyncProtobuf.Writer"/>. Unlike
	///   <see cref="SyncBinary"/>, this writer keeps the whole output in one contiguous
	///   in-memory buffer until <see cref="Flush"/> is called, because Protobuf messages
	///   are length-prefixed and the length is only known after the body is written (it is
	///   back-patched in place).</summary>
	internal class WriterState : WriterStateBase
	{
		internal Options _opt;

		byte[] _data;
		int _pos;

		// The current object's last-used field number (for auto-numbering). Saved/restored
		// via the stack as sub-objects are entered and left.
		int _lastFieldId;

		struct WFrame
		{
			public ObjectMode Mode;
			public int LenInsertPos; // where this payload's length varint will be inserted
			public bool IsElement;   // true if this sub-object is a positional list element
			public int SavedLastFieldId;
		}
		InternalList<WFrame> _stack = new InternalList<WFrame>(8);

		internal int Depth => _stack.Count;
		internal bool IsInsideList =>
			_stack.Count != 0 && (_stack.Last.Mode & (ObjectMode.List | ObjectMode.Tuple)) != 0;

		public WriterState(IBufferWriter<byte> output, Options options) : base(output)
		{
			_opt = options;
			_data = new byte[System.Math.Max(16, options.Write.InitialBufferSize)];
		}

		#region Low-level buffer primitives

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void EnsureRoom(int extra)
		{
			if (_pos + extra > _data.Length)
				Grow(_pos + extra);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		void Grow(int minCapacity)
		{
			int newCap = _data.Length * 2;
			if (newCap < minCapacity) newCap = minCapacity;
			Array.Resize(ref _data, newCap);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void WriteByte(byte b)
		{
			EnsureRoom(1);
			_data[_pos++] = b;
		}
		internal void WriteRawBytes(ReadOnlySpan<byte> bytes)
		{
			EnsureRoom(bytes.Length);
			bytes.CopyTo(_data.AsSpan(_pos));
			_pos += bytes.Length;
		}
		internal void WriteRawLE32(uint bits)
		{
			EnsureRoom(4);
			_data[_pos] = (byte)bits;
			_data[_pos + 1] = (byte)(bits >> 8);
			_data[_pos + 2] = (byte)(bits >> 16);
			_data[_pos + 3] = (byte)(bits >> 24);
			_pos += 4;
		}
		internal void WriteRawLE64(ulong bits)
		{
			EnsureRoom(8);
			for (int i = 0; i < 8; i++)
				_data[_pos + i] = (byte)(bits >> (i * 8));
			_pos += 8;
		}

		internal static int VarintSize(ulong v)
		{
			int n = 1;
			while (v >= 0x80) { v >>= 7; n++; }
			return n;
		}

		internal void WriteVarint(ulong v)
		{
			EnsureRoom(10);
			while (v >= 0x80) {
				_data[_pos++] = (byte)(v | 0x80);
				v >>= 7;
			}
			_data[_pos++] = (byte)v;
		}

		// Inserts a varint at position `at`, shifting existing bytes [at.._pos) to the right.
		void InsertVarintAt(int at, ulong v)
		{
			int size = VarintSize(v);
			EnsureRoom(size);
			int count = _pos - at;
			Array.Copy(_data, at, _data, at + size, count);
			int p = at;
			while (v >= 0x80) {
				_data[p++] = (byte)(v | 0x80);
				v >>= 7;
			}
			_data[p] = (byte)v;
			_pos += size;
		}

		#endregion

		#region Field-number and tag helpers

		int ResolveFieldId(FieldId name)
		{
			int id = name.Id != int.MinValue ? name.Id : _lastFieldId + 1;
			_lastFieldId = id;
			return id;
		}

		void WriteTag(int fieldNumber, WireType wt)
			=> WriteVarint(((ulong)(uint)fieldNumber << 3) | (byte)wt);

		#endregion

		#region Scalar field writers

		// Signed integer (sbyte/short/int/long): stored as its 64-bit two's-complement
		// bit pattern, matching Protobuf int32/int64.
		internal void WriteIntField(FieldId name, long value) => WriteVarintValue(name, unchecked((ulong)value));
		// Unsigned integer / bool / char.
		internal void WriteUIntField(FieldId name, ulong value) => WriteVarintValue(name, value);

		void WriteVarintValue(FieldId name, ulong bits)
		{
			if (IsInsideList) {
				WriteVarint(bits);
			} else {
				WriteTag(ResolveFieldId(name), WireType.Varint);
				WriteVarint(bits);
			}
		}
		internal void WriteVarintValueN(FieldId name, ulong? bits)
		{
			if (IsInsideList) {
				if (bits == null) { WriteByte(0); }
				else { WriteByte(1); WriteVarint(bits.Value); }
			} else {
				int id = ResolveFieldId(name);
				if (bits == null) return; // omit absent field
				WriteTag(id, WireType.Varint);
				WriteVarint(bits.Value);
			}
		}

		internal void WriteFloatField(FieldId name, float value)
		{
			uint bits = FloatToBits(value);
			if (IsInsideList) WriteRawLE32(bits);
			else { WriteTag(ResolveFieldId(name), WireType.I32); WriteRawLE32(bits); }
		}
		internal void WriteFloatFieldN(FieldId name, float? value)
		{
			if (IsInsideList) {
				if (value == null) WriteByte(0);
				else { WriteByte(1); WriteRawLE32(FloatToBits(value.Value)); }
			} else {
				int id = ResolveFieldId(name);
				if (value == null) return;
				WriteTag(id, WireType.I32); WriteRawLE32(FloatToBits(value.Value));
			}
		}

		internal void WriteDoubleField(FieldId name, double value)
		{
			ulong bits = DoubleToBits(value);
			if (IsInsideList) WriteRawLE64(bits);
			else { WriteTag(ResolveFieldId(name), WireType.I64); WriteRawLE64(bits); }
		}
		internal void WriteDoubleFieldN(FieldId name, double? value)
		{
			if (IsInsideList) {
				if (value == null) WriteByte(0);
				else { WriteByte(1); WriteRawLE64(DoubleToBits(value.Value)); }
			} else {
				int id = ResolveFieldId(name);
				if (value == null) return;
				WriteTag(id, WireType.I64); WriteRawLE64(DoubleToBits(value.Value));
			}
		}

		// Length-delimited value from materialized bytes (string/decimal/BigInteger).
		void WriteLenValue(FieldId name, ReadOnlySpan<byte> bytes, bool isNull)
		{
			if (IsInsideList) {
				if (isNull) WriteVarint(0);
				else { WriteVarint((ulong)(bytes.Length + 1)); WriteRawBytes(bytes); }
			} else {
				int id = ResolveFieldId(name);
				if (isNull) return; // omit absent field
				WriteTag(id, WireType.Len);
				WriteVarint((ulong)bytes.Length);
				WriteRawBytes(bytes);
			}
		}

		internal void WriteStringField(FieldId name, string? value)
		{
			if (value == null) { WriteLenValue(name, default, isNull: true); return; }
			int byteCount = Utf8ByteCount(value);
			// Materialize into a temporary span. We could write in place, but null/length
			// framing makes a temp buffer simpler and strings are usually short.
			byte[] tmp = byteCount <= 256 ? _scratch : new byte[byteCount];
			int written = Utf8GetBytes(value, tmp);
			Debug.Assert(written == byteCount);
			WriteLenValue(name, tmp.AsSpan(0, byteCount), isNull: false);
		}
		readonly byte[] _scratch = new byte[256];

		internal void WriteDecimalField(FieldId name, decimal value)
			=> WriteLenValue(name, DecimalToBytes(value), isNull: false);
		internal void WriteDecimalFieldN(FieldId name, decimal? value)
			=> WriteLenValue(name, value == null ? default : DecimalToBytes(value.Value), value == null);

		internal void WriteBigIntField(FieldId name, BigInteger value)
			=> WriteLenValue(name, value.ToByteArray(), isNull: false); // little-endian two's complement
		internal void WriteBigIntFieldN(FieldId name, BigInteger? value)
			=> WriteLenValue(name, value == null ? default : value.Value.ToByteArray(), value == null);

		#endregion

		#region Type tag

		internal void WriteTypeTag(string? tag)
		{
			// The type tag is stored as a reserved field number, before other fields.
			if (tag == null) return;
			Debug.Assert(!IsInsideList);
			int byteCount = Utf8ByteCount(tag);
			byte[] tmp = byteCount <= 256 ? _scratch : new byte[byteCount];
			Utf8GetBytes(tag, tmp);
			WriteTag(TypeTagFieldNumber, WireType.Len);
			WriteVarint((ulong)byteCount);
			WriteRawBytes(tmp.AsSpan(0, byteCount));
		}

		#endregion

		#region BeginSubObject / EndSubObject

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength)
		{
			bool insideList = IsInsideList;
			bool nullable = (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;
			bool isNull = childKey == null && nullable;

			// Field number must be consumed for every field (even null/back-ref) so the
			// reader and writer stay aligned.
			int fieldId = insideList ? 0 : ResolveFieldId(name);

			if (isNull) {
				if (insideList) WriteVarint(0); // null element marker
				return (false, 0, null);
			}

			bool dedup = (mode & ObjectMode.Deduplicate) != 0;
			long dedupId = 0;
			bool firstTime = true;
			if (dedup) {
				Debug.Assert(childKey != null);
				dedupId = _idGen.GetId(childKey!, out firstTime);
			}

			// Every sub-object/list payload starts with a one-byte framing marker so the
			// reader can detect deduplication regardless of its own ObjectMode.
			if (!insideList)
				WriteTag(fieldId, WireType.Len);
			int lenInsertPos = _pos;

			if (dedup && !firstTime) {
				// Back-reference: payload is [DedupBackRef][varint id], no body.
				WriteByte(DedupBackRef);
				WriteVarint((ulong)dedupId);
				int payloadLen = _pos - lenInsertPos;
				InsertVarintAt(lenInsertPos, (ulong)(insideList ? payloadLen + 1 : payloadLen));
				return (false, 0, childKey);
			}

			if (dedup) {
				WriteByte(DedupFirst);
				WriteVarint((ulong)dedupId);
			} else {
				WriteByte(DedupNone);
			}

			_stack.Add(new WFrame {
				Mode = mode,
				LenInsertPos = lenInsertPos,
				IsElement = insideList,
				SavedLastFieldId = _lastFieldId,
			});
			_lastFieldId = 0;

			ObjectMode kind = mode & (ObjectMode.List | ObjectMode.Tuple);
			return (true, kind == 0 ? 1 : listLength, childKey);
		}

		public void EndSubObject()
		{
			var frame = _stack.Last;
			_stack.Pop();
			_lastFieldId = frame.SavedLastFieldId;

			int payloadLen = _pos - frame.LenInsertPos;
			ulong lenValue = frame.IsElement ? (ulong)(payloadLen + 1) : (ulong)payloadLen;
			InsertVarintAt(frame.LenInsertPos, lenValue);
		}

		#endregion

		#region Encoding helpers

		internal static uint FloatToBits(float value)
		{
			#if NETSTANDARD2_0
			return BitConverter.ToUInt32(BitConverter.GetBytes(value), 0);
			#else
			return unchecked((uint)BitConverter.SingleToInt32Bits(value));
			#endif
		}
		internal static ulong DoubleToBits(double value)
			=> unchecked((ulong)BitConverter.DoubleToInt64Bits(value));

		static byte[] DecimalToBytes(decimal value)
		{
			int[] bits = decimal.GetBits(value);
			var bytes = new byte[16];
			for (int i = 0; i < 4; i++) {
				uint b = unchecked((uint)bits[i]);
				bytes[i * 4] = (byte)b;
				bytes[i * 4 + 1] = (byte)(b >> 8);
				bytes[i * 4 + 2] = (byte)(b >> 16);
				bytes[i * 4 + 3] = (byte)(b >> 24);
			}
			return bytes;
		}

		static int Utf8ByteCount(string s)
		{
			#if NETSTANDARD2_0
			return Encoding.UTF8.GetByteCount(s);
			#else
			return Encoding.UTF8.GetByteCount(s.AsSpan());
			#endif
		}
		static int Utf8GetBytes(string s, byte[] dest)
		{
			#if NETSTANDARD2_0
			return Encoding.UTF8.GetBytes(s, 0, s.Length, dest, 0);
			#else
			return Encoding.UTF8.GetBytes(s.AsSpan(), dest.AsSpan());
			#endif
		}

		#endregion

		public new IBufferWriter<byte> Flush()
		{
			if (_pos > 0) {
				var span = _output.GetSpan(_pos);
				_data.AsSpan(0, _pos).CopyTo(span);
				_output.Advance(_pos);
				_pos = 0;
			}
			return _output;
		}
	}
}
