using Loyc.Collections;
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

partial class SyncProtobuf
{
	/// <summary>The mutable state behind <see cref="SyncProtobuf.Writer"/>. The writer
	///   keeps the whole output in one contiguous in-memory buffer until
	///   <see cref="Flush"/> is called, because Protobuf messages are length-prefixed
	///   and each length is only known after the message body has been written (a
	///   reserved length byte is patched in place, and the body is shifted only in the
	///   rare case that a length prefix needs more than one byte).</summary>
	internal class WriterState
	{
		internal Options _opt;
		internal IBufferWriter<byte> _output;
		// Was ObjectIDGenerator, which lives in the BinaryFormatter corner of the BCL.
		// ObjectIdGenerator is the same reference-equality id table (IDs also start at one).
		readonly ObjectIdGenerator _idGen = new ObjectIdGenerator();

		byte[] _data;
		int _pos;

		// The current message's last-used field number (for auto-numbering).
		// Saved/restored via the stack as sub-messages are entered and left.
		int _lastFieldId;

		// Object  = a message with tagged fields (used for objects, tuples and the root)
		// List    = a list container message whose elements are all stored in field 1
		enum FrameKind : byte { Object, Tuple, List }

		struct WFrame
		{
			public FrameKind Kind;
			public int OuterLenPos;  // reserved length byte of this frame's outer LEN, or -1 (bare root)
			public int InnerLenPos;  // reserved length byte of an inner LEN (dedup `value` field or
			                         // element wrapper `value` field), or -1
			public int PackedLenPos; // List: reserved length byte of the open packed container, or -1
			public int BodyStartPos; // position just after the last length prefix (detects an empty root)
			public int SavedLastFieldId;
		}
		InternalList<WFrame> _stack = new InternalList<WFrame>(8);

		ref WFrame Top => ref _stack.InternalArray[_stack.Count - 1];

		internal int Depth => _stack.Count;
		internal bool IsInsideList => _stack.Count != 0 && Top.Kind != FrameKind.Object;
		// True when writes should be encoded as list elements (tuples use tagged fields instead)
		bool InListFrame => _stack.Count != 0 && Top.Kind == FrameKind.List;

		public WriterState(IBufferWriter<byte> output, Options options)
		{
			_output = output;
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
			// BinaryPrimitives compiles to a single unaligned store (with a bswap on
			// big-endian hosts), replacing 4 (resp. 8) separate shift+store pairs.
			// Available on every target via System.Memory.
			BinaryPrimitives.WriteUInt32LittleEndian(_data.AsSpan(_pos), bits);
			_pos += 4;
		}
		internal void WriteRawLE64(ulong bits)
		{
			EnsureRoom(8);
			BinaryPrimitives.WriteUInt64LittleEndian(_data.AsSpan(_pos), bits);
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

		// Reserves space for a length prefix whose value isn't known yet, and returns its
		// position for PatchLen. One byte is reserved because most messages are under 128
		// bytes; PatchLen shifts the payload only when the length needs a longer varint.
		int ReserveLen()
		{
			WriteByte(0);
			return _pos - 1;
		}

		// Sets the length prefix at `lenPos` (reserved by ReserveLen) to the number of
		// bytes written after it. If the varint needs more than the reserved byte, the
		// payload is shifted right to make room.
		void PatchLen(int lenPos)
		{
			int len = _pos - lenPos - 1;
			Debug.Assert(len >= 0);
			if (len < 0x80) {
				_data[lenPos] = (byte)len;
				return;
			}
			int size = VarintSize((ulong)len);
			int extra = size - 1;
			EnsureRoom(extra);
			Array.Copy(_data, lenPos + 1, _data, lenPos + 1 + extra, len);
			_pos += extra;
			ulong v = (ulong)len;
			int p = lenPos;
			while (v >= 0x80) {
				_data[p++] = (byte)(v | 0x80);
				v >>= 7;
			}
			_data[p] = (byte)v;
		}

		#endregion

		#region Field-number and tag helpers

		int ResolveFieldId(FieldId name)
		{
			int id = name.Id != int.MinValue ? name.Id : _lastFieldId + 1;
			if ((uint)(id - 1) >= MaxUserFieldNumber || (id >= 19000 && id <= 19999))
				throw new ArgumentException(
					"SyncProtobuf: field '{0}' has invalid Protobuf field number {1}. Field numbers must be in the range 1 to {2}, excluding 19000-19999 (reserved by Protobuf)."
					.Localized(name.Name ?? "(unnamed)", id, MaxUserFieldNumber));
			_lastFieldId = id;
			return id;
		}

		void WriteTag(int fieldNumber, WireType wt)
			=> WriteVarint(((ulong)(uint)fieldNumber << 3) | (byte)wt);

		// In a list, scalar elements are stored in packed containers (field 1, LEN).
		// Opens a packed container if none is open.
		void BeginPackedElement()
		{
			ref WFrame f = ref Top;
			Debug.Assert(f.Kind == FrameKind.List);
			if (f.PackedLenPos < 0) {
				WriteTag(1, WireType.Len);
				f.PackedLenPos = ReserveLen();
			}
		}
		// Closes the open packed container, if any (called before writing a
		// length-delimited element and when the list ends).
		void EndPackedContainer()
		{
			ref WFrame f = ref Top;
			if (f.PackedLenPos >= 0) {
				PatchLen(f.PackedLenPos);
				f.PackedLenPos = -1;
			}
		}

		#endregion

		#region Scalar field writers

		// Signed integer (sbyte/short/int/long): stored as its 64-bit two's-complement
		// bit pattern, matching Protobuf int32/int64.
		internal void WriteIntField(FieldId name, long value) => WriteVarintValue(name, unchecked((ulong)value));
		// Unsigned integer / bool / char.
		internal void WriteUIntField(FieldId name, ulong value) => WriteVarintValue(name, value);

		void WriteVarintValue(FieldId name, ulong bits)
		{
			if (InListFrame) {
				BeginPackedElement();
				WriteVarint(bits);
			} else {
				WriteTag(ResolveFieldId(name), WireType.Varint);
				WriteVarint(bits);
			}
		}
		internal void WriteVarintValueN(FieldId name, ulong? bits)
		{
			if (InListFrame) {
				// Nullable elements are wrapped: { 1: value } if present, {} if null
				EndPackedContainer();
				WriteTag(1, WireType.Len);
				int lenPos = ReserveLen();
				if (bits != null) {
					WriteTag(1, WireType.Varint);
					WriteVarint(bits.Value);
				}
				PatchLen(lenPos);
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
			if (InListFrame) { BeginPackedElement(); WriteRawLE32(bits); }
			else { WriteTag(ResolveFieldId(name), WireType.I32); WriteRawLE32(bits); }
		}
		internal void WriteFloatFieldN(FieldId name, float? value)
		{
			if (InListFrame) {
				EndPackedContainer();
				WriteTag(1, WireType.Len);
				int lenPos = ReserveLen();
				if (value != null) {
					WriteTag(1, WireType.I32);
					WriteRawLE32(FloatToBits(value.Value));
				}
				PatchLen(lenPos);
			} else {
				int id = ResolveFieldId(name);
				if (value == null) return;
				WriteTag(id, WireType.I32);
				WriteRawLE32(FloatToBits(value.Value));
			}
		}

		internal void WriteDoubleField(FieldId name, double value)
		{
			ulong bits = DoubleToBits(value);
			if (InListFrame) { BeginPackedElement(); WriteRawLE64(bits); }
			else { WriteTag(ResolveFieldId(name), WireType.I64); WriteRawLE64(bits); }
		}
		internal void WriteDoubleFieldN(FieldId name, double? value)
		{
			if (InListFrame) {
				EndPackedContainer();
				WriteTag(1, WireType.Len);
				int lenPos = ReserveLen();
				if (value != null) {
					WriteTag(1, WireType.I64);
					WriteRawLE64(DoubleToBits(value.Value));
				}
				PatchLen(lenPos);
			} else {
				int id = ResolveFieldId(name);
				if (value == null) return;
				WriteTag(id, WireType.I64);
				WriteRawLE64(DoubleToBits(value.Value));
			}
		}

		// Non-nullable length-delimited value (decimal, BigInteger): a plain LEN field,
		// or a direct LEN element (`repeated bytes`) inside a list.
		void WriteLenValue(FieldId name, ReadOnlySpan<byte> bytes)
		{
			if (InListFrame) {
				EndPackedContainer();
				WriteTag(1, WireType.Len);
			} else {
				WriteTag(ResolveFieldId(name), WireType.Len);
			}
			WriteVarint((ulong)bytes.Length);
			WriteRawBytes(bytes);
		}
		// Nullable length-delimited value (string, decimal?, BigInteger?): omitted when
		// null in a message; wrapped as { 1: value } / {} (null) inside a list, so that
		// null and empty values remain distinguishable.
		void WriteLenValueN(FieldId name, ReadOnlySpan<byte> bytes, bool isNull)
		{
			if (InListFrame) {
				EndPackedContainer();
				WriteTag(1, WireType.Len);
				int lenPos = ReserveLen();
				if (!isNull) {
					WriteTag(1, WireType.Len);
					WriteVarint((ulong)bytes.Length);
					WriteRawBytes(bytes);
				}
				PatchLen(lenPos);
			} else {
				int id = ResolveFieldId(name);
				if (isNull) return; // omit absent field
				WriteTag(id, WireType.Len);
				WriteVarint((ulong)bytes.Length);
				WriteRawBytes(bytes);
			}
		}
		// Deduplicated length-delimited value (string or byte[] with
		// ObjectMode.Deduplicate): a wrapper message { 1: id, 2: value } on the first
		// occurrence, or { 1: id } for a back-reference.
		void WriteDedupLenValue(FieldId name, object obj, ReadOnlySpan<byte> bytes)
		{
			long id = _idGen.GetId(obj, out bool firstTime);
			if (InListFrame) {
				EndPackedContainer();
				WriteTag(1, WireType.Len);
			} else {
				WriteTag(ResolveFieldId(name), WireType.Len);
			}
			int lenPos = ReserveLen();
			WriteTag(1, WireType.Varint);
			WriteVarint((ulong)id);
			if (firstTime) {
				WriteTag(2, WireType.Len);
				WriteVarint((ulong)bytes.Length);
				WriteRawBytes(bytes);
			}
			PatchLen(lenPos);
		}

		internal void WriteStringField(FieldId name, string? value, ObjectMode mode)
		{
			if (value == null) { WriteLenValueN(name, default, isNull: true); return; }
			int byteCount = Utf8ByteCount(value);
			// Rent rather than allocate when the string exceeds the inline scratch
			// buffer. Nothing escapes: the bytes are copied into the output below.
			bool rented = byteCount > _scratch.Length;
			byte[] tmp = rented ? ArrayPool<byte>.Shared.Rent(byteCount) : _scratch;
			try {
				int written = Utf8GetBytes(value, tmp);
				Debug.Assert(written == byteCount);
				if ((mode & ObjectMode.Deduplicate) != 0)
					WriteDedupLenValue(name, value, tmp.AsSpan(0, byteCount));
				else
					WriteLenValueN(name, tmp.AsSpan(0, byteCount), isNull: false);
			} finally {
				if (rented)
					ArrayPool<byte>.Shared.Return(tmp);
			}
		}
		readonly byte[] _scratch = new byte[256];

		internal void WriteDecimalField(FieldId name, decimal value)
			=> WriteLenValue(name, DecimalToBytes(value));
		internal void WriteDecimalFieldN(FieldId name, decimal? value)
			=> WriteLenValueN(name, value == null ? default : DecimalToBytes(value.Value), value == null);

		// BigInteger: little-endian two's complement (BigInteger.ToByteArray's format)
		internal void WriteBigIntField(FieldId name, BigInteger value)
			=> WriteLenValue(name, value.ToByteArray());
		internal void WriteBigIntFieldN(FieldId name, BigInteger? value)
			=> WriteLenValueN(name, value == null ? default : value.Value.ToByteArray(), value == null);

		// byte[] and other byte lists are stored as a Protobuf `bytes` value rather than
		// as a list container, matching how Protobuf schemas normally model binary data.
		internal void WriteByteListField<Scanner>(FieldId name, Scanner scanner, object? list, ObjectMode mode)
			where Scanner : IScanner<byte>
		{
			bool nullable = (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;
			if (list == null && nullable) { WriteLenValueN(name, default, isNull: true); return; }

			bool dedup = (mode & ObjectMode.Deduplicate) != 0;
			if (dedup) {
				long id = _idGen.GetId(list!, out bool firstTime);
				if (InListFrame) { EndPackedContainer(); WriteTag(1, WireType.Len); }
				else WriteTag(ResolveFieldId(name), WireType.Len);
				int lenPos = ReserveLen();
				WriteTag(1, WireType.Varint);
				WriteVarint((ulong)id);
				if (firstTime) {
					WriteTag(2, WireType.Len);
					int valLenPos = ReserveLen();
					CopyScanner(scanner);
					PatchLen(valLenPos);
				}
				PatchLen(lenPos);
			} else if (InListFrame) {
				// byte[] as a list element: wrapped like other nullable elements
				EndPackedContainer();
				WriteTag(1, WireType.Len);
				int lenPos = ReserveLen();
				WriteTag(1, WireType.Len);
				int valLenPos = ReserveLen();
				CopyScanner(scanner);
				PatchLen(valLenPos);
				PatchLen(lenPos);
			} else {
				WriteTag(ResolveFieldId(name), WireType.Len);
				int lenPos = ReserveLen();
				CopyScanner(scanner);
				PatchLen(lenPos);
			}
		}
		void CopyScanner<Scanner>(Scanner scanner) where Scanner : IScanner<byte>
		{
			Memory<byte> scratch = default;
			ReadOnlyMemory<byte> chunk;
			int skip = 0;
			while ((chunk = scanner.Read(skip, -1, ref scratch)).Length != 0) {
				WriteRawBytes(chunk.Span);
				skip = chunk.Length;
			}
		}

		#endregion

		#region Type tag

		internal void WriteTypeTag(string? tag)
		{
			// The type tag is stored as a string field with a reserved field number.
			if (tag == null) return;
			if (InListFrame)
				throw new InvalidOperationException("SyncTypeTag cannot be used inside a list.");
			int byteCount = Utf8ByteCount(tag);
			bool rented = byteCount > _scratch.Length;
			byte[] tmp = rented ? ArrayPool<byte>.Shared.Rent(byteCount) : _scratch;
			try {
				Utf8GetBytes(tag, tmp);
				WriteTag(TypeTagFieldNumber, WireType.Len);
				WriteVarint((ulong)byteCount);
				WriteRawBytes(tmp.AsSpan(0, byteCount));
			} finally {
				if (rented)
					ArrayPool<byte>.Shared.Return(tmp);
			}
		}

		#endregion

		#region BeginSubObject / EndSubObject

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength)
		{
			FrameKind kind = (mode & ObjectMode.Tuple) == ObjectMode.Tuple ? FrameKind.Tuple
				: (mode & ObjectMode.List) != 0 ? FrameKind.List : FrameKind.Object;
			bool dedup = (mode & ObjectMode.Deduplicate) != 0;
			bool nullable = (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;
			bool isNull = childKey == null && nullable;

			if (_stack.Count == 0)
				return BeginRoot(childKey, kind, dedup, isNull);

			bool inList = InListFrame;
			// The field number must be consumed for every field (even a null one) so the
			// reader and writer stay aligned on auto-assigned numbers.
			int fieldNum = inList ? 0 : ResolveFieldId(name);

			if (isNull) {
				if (inList) {
					// A null element is an empty wrapper message
					EndPackedContainer();
					WriteTag(1, WireType.Len);
					WriteByte(0);
				}
				return (false, 0, null);
			}

			long dedupId = 0;
			bool firstTime = true;
			if (dedup)
				dedupId = _idGen.GetId(childKey!, out firstTime);

			if (inList) { EndPackedContainer(); WriteTag(1, WireType.Len); }
			else WriteTag(fieldNum, WireType.Len);
			int outerLenPos = ReserveLen();
			int innerLenPos = -1;

			if (dedup) {
				// Dedup wrapper: { 1: id, 2: body } on first occurrence, { 1: id } after
				WriteTag(1, WireType.Varint);
				WriteVarint((ulong)dedupId);
				if (!firstTime) {
					PatchLen(outerLenPos);
					return (false, 0, childKey);
				}
				WriteTag(2, WireType.Len);
				innerLenPos = ReserveLen();
			} else if (inList && nullable) {
				// Nullable element wrapper: { 1: body }
				WriteTag(1, WireType.Len);
				innerLenPos = ReserveLen();
			}

			_stack.Add(new WFrame {
				Kind = kind,
				OuterLenPos = outerLenPos,
				InnerLenPos = innerLenPos,
				PackedLenPos = -1,
				BodyStartPos = _pos,
				SavedLastFieldId = _lastFieldId,
			});
			_lastFieldId = 0;
			return (true, kind == FrameKind.Object ? 1 : listLength, childKey);
		}

		// The root value is written as a bare message body (as a .proto-described file
		// or network message would be), so there is no tag or length prefix.
		(bool Begun, int Length, object? Object) BeginRoot(object? childKey, FrameKind kind, bool dedup, bool isNull)
		{
			if (kind != FrameKind.Object)
				throw new NotSupportedException(
					"SyncProtobuf: the root value must be an object (a Protobuf message), not a list or tuple. " +
					"Wrap the list in an object, or use a synchronizer whose root is an object.");
			if (isNull)
				return (false, 0, null); // a null root produces zero bytes

			int innerLenPos = -1;
			if (dedup) {
				// The root of a deduplicated graph is always a first occurrence
				long dedupId = _idGen.GetId(childKey!, out bool _);
				WriteTag(1, WireType.Varint);
				WriteVarint((ulong)dedupId);
				WriteTag(2, WireType.Len);
				innerLenPos = ReserveLen();
			}
			_stack.Add(new WFrame {
				Kind = FrameKind.Object,
				OuterLenPos = -1,
				InnerLenPos = innerLenPos,
				PackedLenPos = -1,
				BodyStartPos = _pos,
				SavedLastFieldId = _lastFieldId,
			});
			_lastFieldId = 0;
			return (true, 1, childKey);
		}

		public void EndSubObject()
		{
			ref WFrame f = ref Top;
			if (f.PackedLenPos >= 0)
				PatchLen(f.PackedLenPos);
			if (f.OuterLenPos < 0 && f.InnerLenPos < 0 && _pos == f.BodyStartPos) {
				// A bare root whose body is empty would be indistinguishable from a null
				// root (zero bytes), so mark it as present with a reserved boolean field.
				WriteTag(PresentFieldNumber, WireType.Varint);
				WriteVarint(1);
			}
			if (f.InnerLenPos >= 0)
				PatchLen(f.InnerLenPos);
			if (f.OuterLenPos >= 0)
				PatchLen(f.OuterLenPos);
			int saved = f.SavedLastFieldId;
			_stack.Pop();
			_lastFieldId = saved;
		}

		#endregion

		#region Encoding helpers

		internal static uint FloatToBits(float value)
		{
			#if NETSTANDARD2_0 || NETFRAMEWORK
			// BitConverter.Int32BitsToSingle/SingleToInt32Bits don't exist here, and the
			// old GetBytes()+ToUInt32 round-trip allocated a byte[4] for EVERY float
			// written. Unsafe.As is the same reinterpretation with no allocation.
			return Unsafe.As<float, uint>(ref value);
			#else
			return unchecked((uint)BitConverter.SingleToInt32Bits(value));
			#endif
		}
		internal static ulong DoubleToBits(double value)
			=> unchecked((ulong)BitConverter.DoubleToInt64Bits(value));

		static byte[] DecimalToBytes(decimal value)
		{
			#if NET5_0_OR_GREATER
			// decimal.GetBits(decimal, Span<int>) is .NET 5+. NOTE: netstandard2.1 does
			// NOT have it, so the house `NETSTANDARD2_0 || NETFRAMEWORK` guard would be
			// wrong here.
			Span<int> bits = stackalloc int[4];
			decimal.GetBits(value, bits);
			#else
			int[] bits = decimal.GetBits(value);
			#endif
			var bytes = new byte[16];
			for (int i = 0; i < 4; i++)
				BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), unchecked((uint)bits[i]));
			return bytes;
		}

		static int Utf8ByteCount(string s)
		{
			#if NETSTANDARD2_0 || NETFRAMEWORK
			return Encoding.UTF8.GetByteCount(s);
			#else
			return Encoding.UTF8.GetByteCount(s.AsSpan());
			#endif
		}
		static int Utf8GetBytes(string s, byte[] dest)
		{
			#if NETSTANDARD2_0 || NETFRAMEWORK
			return Encoding.UTF8.GetBytes(s, 0, s.Length, dest, 0);
			#else
			return Encoding.UTF8.GetBytes(s.AsSpan(), dest.AsSpan());
			#endif
		}

		#endregion

		public IBufferWriter<byte> Flush()
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
