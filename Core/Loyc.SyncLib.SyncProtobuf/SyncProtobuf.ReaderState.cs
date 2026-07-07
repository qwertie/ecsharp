using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	internal class ReaderState
	{
		const string BadFormat = "Invalid Protobuf data";

		readonly Options _opt;
		readonly ReadOnlyMemory<byte> _mem;
		Dictionary<long, object>? _objects;

		int _lastFieldId;
		readonly List<RFrame> _stack = new List<RFrame>(8);
		RFrame _top;

		enum ObjType : byte { Normal = ObjectMode.Normal, List = ObjectMode.List, Tuple = ObjectMode.Tuple }

		struct FieldEntry
		{
			public int Num;
			public WireType Wire;
			public int ValueStart; // index just after the tag
		}

		class RFrame
		{
			public ObjType Type;
			public ObjectMode Mode;
			public int SavedLastFieldId;
			// Normal object:
			public List<FieldEntry>? Fields;
			public int Cursor;
			// List/Tuple:
			public int ListPos, ListEnd;
			// Deduplication:
			public long Id;
			public bool HasId;
		}

		public ReaderState(ReadOnlyMemory<byte> input, Options options)
		{
			_mem = input;
			_opt = options;
			// The whole input is treated as a message body; index its top-level fields.
			_top = new RFrame {
				Type = ObjType.Normal,
				Mode = ObjectMode.Normal,
				Fields = IndexFields(0, input.Length),
			};
			_stack.Add(_top);
		}

		public ReaderState(IScanner<byte> scanner, Options options)
			: this(DrainScanner(scanner), options) { }

		static ReadOnlyMemory<byte> DrainScanner(IScanner<byte> scanner)
		{
			var buf = new Loyc.Collections.Impl.InternalList<byte>(256);
			Memory<byte> scratch = default;
			ReadOnlyMemory<byte> chunk;
			int skip = 0;
			while ((chunk = scanner.Read(skip, -1, ref scratch)).Length != 0) {
				var span = chunk.Span;
				for (int i = 0; i < span.Length; i++)
					buf.Add(span[i]);
				skip = chunk.Length;
			}
			return buf.InternalArray.AsMemory(0, buf.Count);
		}

		internal int Depth => _stack.Count - 1;
		internal bool IsInsideList => _top.Type != ObjType.Normal;
		internal bool? ReachedEndOfList => _top.Type != ObjType.Normal ? _top.ListPos >= _top.ListEnd : (bool?)null;
		internal int? MinimumListLength => _top.Type != ObjType.Normal ? 0 : (int?)null;

		#region Low-level decoding

		[DebuggerHidden]
		Exception Error(string msg) => new FormatException("{0} ({1})".Localized(BadFormat, msg));

		ulong ReadVarint(ReadOnlySpan<byte> span, ref int pos)
		{
			ulong result = 0;
			int shift = 0;
			while (true) {
				if ((uint)pos >= (uint)span.Length)
					throw Error("unexpected end of data");
				byte b = span[pos++];
				result |= (ulong)(b & 0x7F) << shift;
				if ((b & 0x80) == 0)
					return result;
				shift += 7;
				if (shift > 63)
					throw Error("varint is too long");
			}
		}

		byte ReadByteChecked(ReadOnlySpan<byte> span, ref int pos)
		{
			if ((uint)pos >= (uint)span.Length)
				throw Error("unexpected end of data");
			return span[pos++];
		}

		void CheckAvailable(ReadOnlySpan<byte> span, int pos, int count)
		{
			if (pos < 0 || count < 0 || pos + count > span.Length)
				throw Error("value extends past end of data");
		}

		uint ReadLE32(ReadOnlySpan<byte> span, ref int pos)
		{
			CheckAvailable(span, pos, 4);
			uint v = (uint)(span[pos] | (span[pos + 1] << 8) | (span[pos + 2] << 16) | (span[pos + 3] << 24));
			pos += 4;
			return v;
		}
		ulong ReadLE64(ReadOnlySpan<byte> span, ref int pos)
		{
			ulong lo = ReadLE32(span, ref pos);
			ulong hi = ReadLE32(span, ref pos);
			return lo | (hi << 32);
		}

		// Reads a length-prefixed byte range, validating the length against the buffer.
		ReadOnlySpan<byte> ReadLenPrefixed(ReadOnlySpan<byte> span, ref int pos)
		{
			ulong len = ReadVarint(span, ref pos);
			if (len > (ulong)_opt.MaxPayloadSize)
				throw Error("length-delimited payload is too large");
			CheckAvailable(span, pos, (int)len);
			var result = span.Slice(pos, (int)len);
			pos += (int)len;
			return result;
		}

		// Skips a field's value given its wire type, returning the position after it.
		int SkipValue(ReadOnlySpan<byte> span, int pos, WireType wire)
		{
			switch (wire) {
				case WireType.Varint: ReadVarint(span, ref pos); return pos;
				case WireType.I32: return pos + 4;
				case WireType.I64: return pos + 8;
				case WireType.Len:
					ulong len = ReadVarint(span, ref pos);
					if (len > (ulong)_opt.MaxPayloadSize)
						throw Error("length-delimited payload is too large");
					return pos + (int)len;
				default:
					throw Error("unsupported wire type " + (int)wire);
			}
		}

		List<FieldEntry> IndexFields(int start, int end)
		{
			var span = _mem.Span;
			if (end > span.Length)
				throw Error("payload extends past end of data");
			var list = new List<FieldEntry>();
			int pos = start;
			while (pos < end) {
				ulong tag = ReadVarint(span, ref pos);
				int num = (int)(tag >> 3);
				var wire = (WireType)(byte)(tag & 7);
				int valueStart = pos;
				pos = SkipValue(span, pos, wire);
				if (pos > end)
					throw Error("field value extends past end of message");
				list.Add(new FieldEntry { Num = num, Wire = wire, ValueStart = valueStart });
			}
			return list;
		}

		#endregion

		#region Field lookup and numbering

		int ResolveFieldId(FieldId name)
		{
			int id = name.Id != int.MinValue ? name.Id : _lastFieldId + 1;
			_lastFieldId = id;
			return id;
		}

		// Field-context lookup: resolves the field number and locates its value.
		bool TryGetField(FieldId name, out int valueStart, out WireType wire)
		{
			int id = ResolveFieldId(name);
			return FindField(id, out valueStart, out wire);
		}

		bool FindField(int id, out int valueStart, out WireType wire)
		{
			var fields = _top.Fields!;
			// Fast path: fields are usually read in the order they were written.
			int c = _top.Cursor;
			if (c < fields.Count && fields[c].Num == id) {
				var fe0 = fields[c];
				_top.Cursor = c + 1;
				valueStart = fe0.ValueStart; wire = fe0.Wire;
				return true;
			}
			for (int i = 0; i < fields.Count; i++) {
				if (fields[i].Num == id) {
					var fe = fields[i];
					_top.Cursor = i + 1;
					valueStart = fe.ValueStart; wire = fe.Wire;
					return true;
				}
			}
			valueStart = 0; wire = default;
			return false;
		}

		#endregion

		#region Scalar readers

		internal long ReadInt(FieldId name) => unchecked((long)ReadRawVarintField(name));
		internal ulong ReadUInt(FieldId name) => ReadRawVarintField(name);

		ulong ReadRawVarintField(FieldId name)
		{
			var span = _mem.Span;
			if (IsInsideList) {
				int p = _top.ListPos;
				ulong v = ReadVarint(span, ref p);
				_top.ListPos = p;
				return v;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return ReadVarint(span, ref p);
			}
			return 0; // absent -> default
		}

		internal long? ReadIntN(FieldId name)
		{
			var r = ReadRawVarintFieldN(name);
			return r.HasValue ? unchecked((long)r.Value) : (long?)null;
		}
		internal ulong? ReadUIntN(FieldId name) => ReadRawVarintFieldN(name);

		ulong? ReadRawVarintFieldN(FieldId name)
		{
			var span = _mem.Span;
			if (IsInsideList) {
				int p = _top.ListPos;
				byte present = ReadByteChecked(span, ref p);
				ulong? result = present == 0 ? (ulong?)null : ReadVarint(span, ref p);
				_top.ListPos = p;
				return result;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return ReadVarint(span, ref p);
			}
			return null; // absent -> null
		}

		internal float ReadFloat(FieldId name) => ReadFloatN(name) ?? default;
		internal float? ReadFloatN(FieldId name)
		{
			var span = _mem.Span;
			if (IsInsideList) {
				// Non-nullable float lists write raw bits; nullable float lists write a
				// presence byte. The element sync method decides which by its static type,
				// so this method (float?) always uses presence framing.
				int p = _top.ListPos;
				byte present = ReadByteChecked(span, ref p);
				float? result = present == 0 ? (float?)null : BitsToFloat(ReadLE32(span, ref p));
				_top.ListPos = p;
				return result;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return BitsToFloat(ReadLE32(span, ref p));
			}
			return null;
		}
		internal float ReadFloatRaw(FieldId name) // non-nullable list/field element
		{
			var span = _mem.Span;
			if (IsInsideList) {
				int p = _top.ListPos;
				float f = BitsToFloat(ReadLE32(span, ref p));
				_top.ListPos = p;
				return f;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return BitsToFloat(ReadLE32(span, ref p));
			}
			return 0;
		}

		internal double ReadDoubleRaw(FieldId name)
		{
			var span = _mem.Span;
			if (IsInsideList) {
				int p = _top.ListPos;
				double d = BitsToDouble(ReadLE64(span, ref p));
				_top.ListPos = p;
				return d;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return BitsToDouble(ReadLE64(span, ref p));
			}
			return 0;
		}
		internal double? ReadDoubleN(FieldId name)
		{
			var span = _mem.Span;
			if (IsInsideList) {
				int p = _top.ListPos;
				byte present = ReadByteChecked(span, ref p);
				double? result = present == 0 ? (double?)null : BitsToDouble(ReadLE64(span, ref p));
				_top.ListPos = p;
				return result;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return BitsToDouble(ReadLE64(span, ref p));
			}
			return null;
		}

		// Reads a length-delimited payload (string/decimal/BigInteger) as a byte span.
		// Returns false when the value is null/absent.
		bool TryReadLenBytes(FieldId name, out ReadOnlySpan<byte> bytes)
		{
			var span = _mem.Span;
			if (IsInsideList) {
				int p = _top.ListPos;
				ulong v = ReadVarint(span, ref p);
				if (v == 0) { _top.ListPos = p; bytes = default; return false; } // null element
				if (v - 1 > (ulong)_opt.MaxPayloadSize)
					throw Error("length-delimited payload is too large");
				int len = (int)(v - 1);
				CheckAvailable(span, p, len);
				bytes = span.Slice(p, len);
				_top.ListPos = p + len;
				return true;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				bytes = ReadLenPrefixed(span, ref p);
				return true;
			}
			bytes = default;
			return false; // absent -> null
		}

		internal string? ReadString(FieldId name)
		{
			if (!TryReadLenBytes(name, out var bytes))
				return null;
			#if NETSTANDARD2_0
			return Encoding.UTF8.GetString(bytes.ToArray());
			#else
			return Encoding.UTF8.GetString(bytes);
			#endif
		}

		internal decimal ReadDecimal(FieldId name) => ReadDecimalN(name) ?? default;
		internal decimal? ReadDecimalN(FieldId name)
		{
			if (!TryReadLenBytes(name, out var bytes))
				return null;
			if (bytes.Length != 16)
				throw Error("decimal payload is not 16 bytes");
			int[] bits = new int[4];
			for (int i = 0; i < 4; i++)
				bits[i] = unchecked((int)(uint)(bytes[i * 4] | (bytes[i * 4 + 1] << 8) | (bytes[i * 4 + 2] << 16) | (bytes[i * 4 + 3] << 24)));
			return new decimal(bits);
		}

		internal BigInteger ReadBigInt(FieldId name) => ReadBigIntN(name) ?? default;
		internal BigInteger? ReadBigIntN(FieldId name)
		{
			if (!TryReadLenBytes(name, out var bytes))
				return null;
			#if NETSTANDARD2_0
			return new BigInteger(bytes.ToArray());
			#else
			return new BigInteger(bytes);
			#endif
		}

		#endregion

		#region Type tag

		internal string? ReadTypeTag()
		{
			if (IsInsideList)
				return null;
			if (!FindField(TypeTagFieldNumber, out int vs, out _))
				return null;
			var span = _mem.Span;
			int p = vs;
			var bytes = ReadLenPrefixed(span, ref p);
			#if NETSTANDARD2_0
			return Encoding.UTF8.GetString(bytes.ToArray());
			#else
			return Encoding.UTF8.GetString(bytes);
			#endif
		}

		#endregion

		#region BeginSubObject / EndSubObject

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, ObjectMode mode)
		{
			var span = _mem.Span;
			bool insideList = IsInsideList;
			ObjectMode kind = mode & (ObjectMode.List | ObjectMode.Tuple);

			int payloadStart, payloadEnd;
			if (insideList) {
				int p = _top.ListPos;
				if (p >= _top.ListEnd)
					return (false, 0, null);
				ulong v = ReadVarint(span, ref p);
				if (v == 0) { _top.ListPos = p; return (false, 0, null); } // null element
				if (v - 1 > (ulong)_opt.MaxPayloadSize)
						throw Error("length-delimited payload is too large");
					int frameLen = (int)(v - 1);
				payloadStart = p;
				payloadEnd = p + frameLen;
				_top.ListPos = payloadEnd;
			} else {
				if (!TryGetField(name, out int vs, out WireType wire)) {
					return (false, 0, null); // absent -> null object/list
				}
				if (wire != WireType.Len)
					throw Error("expected a length-delimited value");
				int p = vs;
				ulong len = ReadVarint(span, ref p);
				payloadStart = p;
				if (len > (ulong)_opt.MaxPayloadSize)
						throw Error("length-delimited payload is too large");
					payloadEnd = p + (int)len;
			}

			if (payloadEnd < payloadStart || payloadEnd > span.Length)
				throw Error("object payload extends past end of data");

			// Every sub-object/list payload begins with a framing marker (written even when
			// deduplication was off), so the reader detects dedup from the data itself and
			// tolerates the Deduplicate flag being toggled between writing and reading.
			long objId = 0;
			bool hasId = false;
			int bodyStart;
			{
				int p = payloadStart;
				if (p >= payloadEnd) throw Error("empty object payload");
					byte marker = ReadByteChecked(span, ref p);
				if (marker == DedupNone) {
					bodyStart = p;
				} else if (marker == DedupFirst) {
					objId = (long)ReadVarint(span, ref p);
					hasId = true;
					bodyStart = p;
				} else if (marker == DedupBackRef) {
					ulong id = ReadVarint(span, ref p);
					if (_objects == null || !_objects.TryGetValue((long)id, out var existing))
						throw Error("dangling deduplication back-reference");
					return (false, 0, existing);
				} else {
					throw Error("invalid object framing marker");
				}
			}

			var frame = new RFrame {
				Type = (ObjType)kind,
				Mode = mode,
				SavedLastFieldId = _lastFieldId,
				Id = objId,
				HasId = hasId,
			};
			if (kind == 0) {
				frame.Fields = IndexFields(bodyStart, payloadEnd);
			} else {
				frame.ListPos = bodyStart;
				frame.ListEnd = payloadEnd;
			}
			Push(frame);

			return (true, kind == 0 ? 1 : int.MaxValue, null);
		}

		public void EndSubObject()
		{
			var frame = _top;
			_stack.RemoveAt(_stack.Count - 1);
			_top = _stack[_stack.Count - 1];
			_lastFieldId = frame.SavedLastFieldId;
		}

		void Push(RFrame frame)
		{
			_stack.Add(frame);
			_top = frame;
			_lastFieldId = 0;
		}

		#endregion

		internal void SetCurrentObject(object value)
		{
			if (_top.HasId) {
				_objects ??= new Dictionary<long, object>();
				_objects[_top.Id] = value;
			}
		}

		internal FieldId NextField
		{
			get {
				if (_top.Type != ObjType.Normal || _top.Fields == null || _top.Cursor >= _top.Fields.Count)
					return FieldId.Missing;
				return new FieldId(null, _top.Fields[_top.Cursor].Num);
			}
		}

		internal SyncType GetFieldType(FieldId name, SyncType expectedType)
		{
			if (IsInsideList)
				return SyncType.Unknown;
			int id = name.Id != int.MinValue ? name.Id : _lastFieldId + 1;
			// Note: this does not advance _lastFieldId (it's only a peek).
			if (!FindFieldPeek(id, out WireType wire))
				return SyncType.Missing;
			return wire switch {
				WireType.Varint => SyncType.Integer,
				WireType.I32 => SyncType.Float,
				WireType.I64 => SyncType.Float,
				WireType.Len => SyncType.List, // string/bytes/message/list
				_ => SyncType.Exists,
			};
		}
		bool FindFieldPeek(int id, out WireType wire)
		{
			var fields = _top.Fields;
			if (fields != null)
				foreach (var fe in fields)
					if (fe.Num == id) { wire = fe.Wire; return true; }
			wire = default;
			return false;
		}

		internal void VerifyEof()
		{
			if (!_opt.Read.VerifyEof)
				return;
			// The root frame indexes the whole input; a well-formed stream has exactly one
			// top-level field (the root object) covering all the bytes.
			var root = _stack[0];
			if (root.Fields != null && root.Fields.Count > 1)
				throw Error("unexpected trailing data after root object");
		}

		static float BitsToFloat(uint bits)
		{
			#if NETSTANDARD2_0
			return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
			#else
			return BitConverter.Int32BitsToSingle(unchecked((int)bits));
			#endif
		}
		static double BitsToDouble(ulong bits) => BitConverter.Int64BitsToDouble(unchecked((long)bits));
	}
}
