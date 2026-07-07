using Loyc.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	/// <summary>The mutable state behind <see cref="SyncProtobuf.Reader"/>. The whole
	///   input is kept in memory (Protobuf messages are length-prefixed, and SyncLib
	///   supports reading fields in any order, so random access is required). Every read
	///   is bounds-checked; malformed input causes <see cref="FormatException"/>.</summary>
	internal class ReaderState
	{
		readonly Options _opt;
		readonly ReadOnlyMemory<byte> _mem;
		Dictionary<long, object>? _objects;

		int _lastFieldId;
		readonly List<RFrame> _stack = new List<RFrame>(8);
		RFrame? _top;

		enum FrameKind : byte { Object, Tuple, List }

		struct FieldEntry
		{
			public int Num;
			public WireType Wire;
			public int ValueStart; // index just after the tag
		}

		class RFrame
		{
			public FrameKind Kind;
			public int SavedLastFieldId;
			// Object/Tuple: the message's fields, indexed for reordering support
			public List<FieldEntry>? Fields;
			public int Cursor;
			public byte DupState; // 0 = unknown, 1 = no duplicate field numbers, 2 = duplicates exist
			// List: the entries of field 1 within the list container message
			public List<FieldEntry>? Entries;
			public int ElemIdx;                // next unconsumed entry
			public int PackedPos, PackedEnd;   // current packed container (Pos >= End: none open)
			// Deduplication:
			public long Id;
			public bool HasId;
		}

		public ReaderState(ReadOnlyMemory<byte> input, Options options)
		{
			_mem = input;
			_opt = options;
		}

		public ReaderState(IScanner<byte> scanner, Options options)
			: this(DrainScanner(scanner), options) { }

		static ReadOnlyMemory<byte> DrainScanner(IScanner<byte> scanner)
		{
			byte[] buf = new byte[256];
			int count = 0;
			Memory<byte> scratch = default;
			ReadOnlyMemory<byte> chunk;
			int skip = 0;
			while ((chunk = scanner.Read(skip, -1, ref scratch)).Length != 0) {
				if (count + chunk.Length > buf.Length)
					Array.Resize(ref buf, System.Math.Max(buf.Length * 2, count + chunk.Length));
				chunk.Span.CopyTo(buf.AsSpan(count));
				count += chunk.Length;
				skip = chunk.Length;
			}
			return buf.AsMemory(0, count);
		}

		internal int Depth => _stack.Count;
		internal bool IsInsideList => _top != null && _top.Kind != FrameKind.Object;
		bool InListFrame => _top != null && _top.Kind == FrameKind.List;

		internal bool? ReachedEndOfList {
			get {
				var top = _top;
				if (top == null || top.Kind != FrameKind.List)
					return null; // unknown for objects; null for tuples (length not stored)
				return top.PackedPos >= top.PackedEnd && top.ElemIdx >= top.Entries!.Count;
			}
		}
		internal int? MinimumListLength => InListFrame ? 0 : (int?)null;

		#region Low-level decoding

		[DebuggerHidden]
		Exception Error(int position, string msg) =>
			new FormatException("Invalid Protobuf data at byte {0}: {1}".Localized(position, msg));

		ulong ReadVarint(ReadOnlySpan<byte> span, ref int pos)
		{
			ulong result = 0;
			int shift = 0;
			while (true) {
				if ((uint)pos >= (uint)span.Length)
					throw Error(pos, "unexpected end of data");
				byte b = span[pos++];
				result |= (ulong)(b & 0x7F) << shift;
				if ((b & 0x80) == 0)
					return result;
				shift += 7;
				if (shift > 63)
					throw Error(pos, "varint is too long");
			}
		}

		void CheckAvailable(ReadOnlySpan<byte> span, int pos, int count)
		{
			if (pos < 0 || count < 0 || (long)pos + count > span.Length)
				throw Error(pos, "value extends past end of data");
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

		// Reads a length prefix and returns the [start, end) range of the payload,
		// validating it against the buffer and the MaxPayloadSize option.
		(int Start, int End) ReadLenPayload(ReadOnlySpan<byte> span, int pos)
		{
			ulong len = ReadVarint(span, ref pos);
			if (len > (ulong)_opt.MaxPayloadSize)
				throw Error(pos, "length-delimited payload is too large");
			CheckAvailable(span, pos, (int)len);
			return (pos, pos + (int)len);
		}

		// Skips a field's value given its wire type, returning the position after it.
		int SkipValue(ReadOnlySpan<byte> span, int pos, WireType wire)
		{
			switch (wire) {
				case WireType.Varint: ReadVarint(span, ref pos); return pos;
				case WireType.I32: return pos + 4;
				case WireType.I64: return pos + 8;
				case WireType.Len:
					int lenPos = pos;
					ulong len = ReadVarint(span, ref pos);
					if (len > (ulong)_opt.MaxPayloadSize)
						throw Error(lenPos, "length-delimited payload is too large");
					long end = pos + (long)len;
					if (end > int.MaxValue)
						throw Error(lenPos, "length-delimited payload is too large");
					return (int)end;
				default:
					throw Error(pos, "unsupported wire type " + (int)wire);
			}
		}

		List<FieldEntry> IndexFields(int start, int end)
		{
			var span = _mem.Span;
			if (end > span.Length)
				throw Error(start, "payload extends past end of data");
			var list = new List<FieldEntry>();
			int pos = start;
			while (pos < end) {
				ulong tag = ReadVarint(span, ref pos);
				int num = (int)(tag >> 3);
				if (num == 0 || tag > ((ulong)MaxFieldNumber << 3 | 7))
					throw Error(pos, "invalid field number");
				var wire = (WireType)(byte)(tag & 7);
				int valueStart = pos;
				pos = SkipValue(span, pos, wire);
				if (pos > end)
					throw Error(valueStart, "field value extends past end of message");
				list.Add(new FieldEntry { Num = num, Wire = wire, ValueStart = valueStart });
			}
			return list;
		}

		// Collects the entries of field 1 in a list container message [start, end).
		// Other field numbers are permitted and ignored (like unknown fields).
		List<FieldEntry> IndexListEntries(int start, int end)
		{
			var all = IndexFields(start, end);
			var entries = new List<FieldEntry>(all.Count);
			foreach (var fe in all)
				if (fe.Num == 1)
					entries.Add(fe);
			return entries;
		}

		#endregion

		#region Field lookup and numbering

		// If no sub-object was ever begun, the whole input acts as one message body
		// (this supports reading fields directly from a NewReader without a root object).
		RFrame TopObject()
		{
			if (_top == null) {
				var root = new RFrame {
					Kind = FrameKind.Object,
					Fields = IndexFields(0, _mem.Length),
				};
				Push(root);
			}
			return _top!;
		}

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
			var frame = TopObject();
			var fields = frame.Fields!;
			// Fast path: fields are usually read in the order they were written. Skipped
			// if any field number occurs twice, because then the last occurrence must win.
			int c = frame.Cursor;
			if (c < fields.Count && fields[c].Num == id && !HasDuplicateFieldNumbers(frame)) {
				var fe0 = fields[c];
				frame.Cursor = c + 1;
				valueStart = fe0.ValueStart; wire = fe0.Wire;
				return true;
			}
			// Search backward so that, per the Protobuf specification, the last
			// occurrence of a duplicated (non-repeated) field wins.
			for (int i = fields.Count - 1; i >= 0; i--) {
				if (fields[i].Num == id) {
					var fe = fields[i];
					frame.Cursor = i + 1;
					valueStart = fe.ValueStart; wire = fe.Wire;
					return true;
				}
			}
			valueStart = 0; wire = default;
			return false;
		}

		static bool HasDuplicateFieldNumbers(RFrame frame)
		{
			if (frame.DupState == 0) {
				frame.DupState = 1;
				var fields = frame.Fields!;
				if (fields.Count > 1) {
					var seen = new HashSet<int>();
					foreach (var fe in fields)
						if (!seen.Add(fe.Num)) { frame.DupState = 2; break; }
				}
			}
			return frame.DupState == 2;
		}

		#endregion

		#region List-element access

		// Returns the payload bounds of the next length-delimited list element.
		(int Start, int End) NextElemPayload(ReadOnlySpan<byte> span)
		{
			var top = _top!;
			Debug.Assert(top.Kind == FrameKind.List);
			if (top.PackedPos < top.PackedEnd)
				throw Error(top.PackedPos, "expected a packed scalar element (lists cannot mix element kinds)");
			if (top.ElemIdx >= top.Entries!.Count)
				throw Error(_mem.Length, "read past the end of a list");
			var entry = top.Entries[top.ElemIdx++];
			if (entry.Wire != WireType.Len)
				throw Error(entry.ValueStart, "expected a length-delimited list element");
			return ReadLenPayload(span, entry.ValueStart);
		}

		// Positions the packed-container cursor at the next packed scalar, opening the
		// next container (field-1 entry) as needed. Returns false at the end of the list.
		// Also accepts the unpacked encoding (one tagged entry per scalar), which other
		// Protobuf implementations may produce for repeated scalar fields.
		bool OpenPackedScalar(ReadOnlySpan<byte> span)
		{
			var top = _top!;
			Debug.Assert(top.Kind == FrameKind.List);
			while (top.PackedPos >= top.PackedEnd) {
				if (top.ElemIdx >= top.Entries!.Count)
					return false;
				var entry = top.Entries[top.ElemIdx++];
				if (entry.Wire == WireType.Len)
					(top.PackedPos, top.PackedEnd) = ReadLenPayload(span, entry.ValueStart);
				else {
					top.PackedPos = entry.ValueStart;
					top.PackedEnd = SkipValue(span, entry.ValueStart, entry.Wire);
				}
			}
			return true;
		}

		// Finds field `num` in a small wrapper message [start, end) without allocating.
		bool FindWrapperField(ReadOnlySpan<byte> span, int start, int end, int num, out int valueStart, out WireType wire)
		{
			int pos = start;
			while (pos < end) {
				ulong tag = ReadVarint(span, ref pos);
				var wt = (WireType)(byte)(tag & 7);
				if ((int)(tag >> 3) == num) {
					valueStart = pos; wire = wt;
					return true;
				}
				pos = SkipValue(span, pos, wt);
				if (pos > end)
					throw Error(pos, "field value extends past end of message");
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
			if (InListFrame) {
				if (!OpenPackedScalar(span))
					throw Error(_mem.Length, "read past the end of a list");
				var top = _top!;
				int p = top.PackedPos;
				ulong v = ReadVarint(span, ref p);
				if (p > top.PackedEnd)
					throw Error(top.PackedPos, "packed element extends past its container");
				top.PackedPos = p;
				return v;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return ReadVarint(span, ref p);
			}
			return 0; // absent -> default (standard Protobuf semantics)
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
			if (InListFrame) {
				// Nullable elements are wrapped: {} = null, { 1: value } otherwise
				var (s, e) = NextElemPayload(span);
				if (!FindWrapperField(span, s, e, 1, out int vs, out _))
					return null;
				int p = vs;
				return ReadVarint(span, ref p);
			}
			if (TryGetField(name, out int vs2, out _)) {
				int p = vs2;
				return ReadVarint(span, ref p);
			}
			return null; // absent -> null
		}

		internal float ReadFloatRaw(FieldId name)
		{
			var span = _mem.Span;
			if (InListFrame) {
				if (!OpenPackedScalar(span))
					throw Error(_mem.Length, "read past the end of a list");
				var top = _top!;
				if (top.PackedEnd - top.PackedPos < 4)
					throw Error(top.PackedPos, "packed element extends past its container");
				int p = top.PackedPos;
				float f = BitsToFloat(ReadLE32(span, ref p));
				top.PackedPos = p;
				return f;
			}
			if (TryGetField(name, out int vs, out _)) {
				int p = vs;
				return BitsToFloat(ReadLE32(span, ref p));
			}
			return 0;
		}
		internal float? ReadFloatN(FieldId name)
		{
			var span = _mem.Span;
			if (InListFrame) {
				var (s, e) = NextElemPayload(span);
				if (!FindWrapperField(span, s, e, 1, out int vs, out _))
					return null;
				int p = vs;
				return BitsToFloat(ReadLE32(span, ref p));
			}
			if (TryGetField(name, out int vs2, out _)) {
				int p = vs2;
				return BitsToFloat(ReadLE32(span, ref p));
			}
			return null;
		}

		internal double ReadDoubleRaw(FieldId name)
		{
			var span = _mem.Span;
			if (InListFrame) {
				if (!OpenPackedScalar(span))
					throw Error(_mem.Length, "read past the end of a list");
				var top = _top!;
				if (top.PackedEnd - top.PackedPos < 8)
					throw Error(top.PackedPos, "packed element extends past its container");
				int p = top.PackedPos;
				double d = BitsToDouble(ReadLE64(span, ref p));
				top.PackedPos = p;
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
			if (InListFrame) {
				var (s, e) = NextElemPayload(span);
				if (!FindWrapperField(span, s, e, 1, out int vs, out _))
					return null;
				int p = vs;
				return BitsToDouble(ReadLE64(span, ref p));
			}
			if (TryGetField(name, out int vs2, out _)) {
				int p = vs2;
				return BitsToDouble(ReadLE64(span, ref p));
			}
			return null;
		}

		// Locates a non-nullable length-delimited value (decimal, BigInteger, bytes): a
		// plain LEN field, or a direct LEN element inside a list. False when absent.
		bool TryReadLenBounds(FieldId name, out int start, out int end)
		{
			var span = _mem.Span;
			if (InListFrame) {
				(start, end) = NextElemPayload(span);
				return true;
			}
			if (TryGetField(name, out int vs, out WireType wire)) {
				if (wire != WireType.Len)
					throw Error(vs, "expected a length-delimited value");
				(start, end) = ReadLenPayload(span, vs);
				return true;
			}
			start = end = 0;
			return false;
		}

		// Locates a nullable length-delimited value (string, decimal?, BigInteger?):
		// omitted when null in a message; wrapped as { 1: value } / {} inside a list.
		// Returns false when the value is null/absent.
		bool TryReadLenBoundsN(FieldId name, out int start, out int end)
		{
			var span = _mem.Span;
			if (InListFrame) {
				var (s, e) = NextElemPayload(span);
				if (!FindWrapperField(span, s, e, 1, out int vs, out WireType wire)) {
					start = end = 0;
					return false; // empty wrapper = null element
				}
				if (wire != WireType.Len)
					throw Error(vs, "expected a length-delimited value");
				(start, end) = ReadLenPayload(span, vs);
				if (end > e)
					throw Error(vs, "value extends past end of its wrapper");
				return true;
			}
			if (TryGetField(name, out int fvs, out WireType fwire)) {
				if (fwire != WireType.Len)
					throw Error(fvs, "expected a length-delimited value");
				(start, end) = ReadLenPayload(span, fvs);
				return true;
			}
			start = end = 0;
			return false; // absent -> null
		}

		bool TryReadLenBytes(FieldId name, out ReadOnlySpan<byte> bytes)
		{
			bool found = TryReadLenBounds(name, out int s, out int e);
			bytes = found ? _mem.Span.Slice(s, e - s) : default;
			return found;
		}
		bool TryReadLenBytesN(FieldId name, out ReadOnlySpan<byte> bytes)
		{
			bool found = TryReadLenBoundsN(name, out int s, out int e);
			bytes = found ? _mem.Span.Slice(s, e - s) : default;
			return found;
		}

		internal string? ReadString(FieldId name, ObjectMode mode)
		{
			if ((mode & ObjectMode.Deduplicate) != 0)
				return (string?)ReadDedupLenValue(name, isString: true);
			if (!TryReadLenBytesN(name, out var bytes))
				return null;
			return Utf8Decode(bytes);
		}

		// Reads a deduplicated string or byte[]: a wrapper { 1: id, 2: value } on the
		// first occurrence, { 1: id } for a back-reference, or absent/{} for null.
		object? ReadDedupLenValue(FieldId name, bool isString)
		{
			var span = _mem.Span;
			int s, e;
			if (InListFrame) {
				(s, e) = NextElemPayload(span);
				if (s == e)
					return null; // empty wrapper = null element
			} else {
				if (!TryGetField(name, out int vs, out WireType wire))
					return null; // absent -> null
				if (wire != WireType.Len)
					throw Error(vs, "expected a length-delimited value");
				(s, e) = ReadLenPayload(span, vs);
			}
			if (!FindWrapperField(span, s, e, 1, out int idStart, out _))
				throw Error(s, "deduplicated value has no id");
			int p = idStart;
			long id = (long)ReadVarint(span, ref p);
			if (FindWrapperField(span, s, e, 2, out int valStart, out WireType valWire)) {
				if (valWire != WireType.Len)
					throw Error(valStart, "expected a length-delimited value");
				var (vs2, ve2) = ReadLenPayload(span, valStart);
				object value = isString
					? (object)Utf8Decode(span.Slice(vs2, ve2 - vs2))
					: span.Slice(vs2, ve2 - vs2).ToArray();
				_objects ??= new Dictionary<long, object>();
				_objects[id] = value;
				return value;
			}
			if (_objects == null || !_objects.TryGetValue(id, out var existing))
				throw Error(s, "dangling deduplication back-reference");
			return existing;
		}

		internal decimal ReadDecimal(FieldId name)
		{
			if (!TryReadLenBytes(name, out var bytes))
				return default;
			return DecimalFromBytes(bytes);
		}
		internal decimal? ReadDecimalN(FieldId name)
		{
			if (!TryReadLenBytesN(name, out var bytes))
				return null;
			return DecimalFromBytes(bytes);
		}
		decimal DecimalFromBytes(ReadOnlySpan<byte> bytes)
		{
			if (bytes.Length != 16)
				throw new FormatException("Invalid Protobuf data: decimal payload is not 16 bytes".Localized());
			int[] bits = new int[4];
			for (int i = 0; i < 4; i++)
				bits[i] = unchecked((int)(uint)(bytes[i * 4] | (bytes[i * 4 + 1] << 8) | (bytes[i * 4 + 2] << 16) | (bytes[i * 4 + 3] << 24)));
			return new decimal(bits);
		}

		internal BigInteger ReadBigInt(FieldId name)
		{
			if (!TryReadLenBytes(name, out var bytes))
				return default;
			return BigIntFromBytes(bytes);
		}
		internal BigInteger? ReadBigIntN(FieldId name)
		{
			if (!TryReadLenBytesN(name, out var bytes))
				return null;
			return BigIntFromBytes(bytes);
		}
		static BigInteger BigIntFromBytes(ReadOnlySpan<byte> bytes)
		{
			#if NETSTANDARD2_0
			return new BigInteger(bytes.ToArray());
			#else
			return new BigInteger(bytes);
			#endif
		}

		// Locates a byte[] value written by WriterState.WriteByteListField.
		// Start >= 0: the value occupies [Start, Start+Length) of InputSpan.
		// Start == -1: the value is null. Start == -2: Backref holds a byte[] instance
		// (a deduplicated value; ReadDedupLenValue registers each new byte[] it creates,
		// so back-references resolve to the same array instance).
		internal (int Start, int Length, object? Backref) ReadByteListField(FieldId name, ObjectMode mode)
		{
			if ((mode & ObjectMode.Deduplicate) != 0) {
				object? value = ReadDedupLenValue(name, isString: false);
				return value == null ? (-1, 0, (object?)null) : (-2, 0, value);
			}
			bool nullable = (mode & ObjectMode.NotNull) == 0;
			bool found = nullable
				? TryReadLenBoundsN(name, out int s, out int e)
				: TryReadLenBounds(name, out s, out e);
			if (!found)
				return (-1, 0, null);
			return (s, e - s, null);
		}

		internal ReadOnlySpan<byte> InputSpan => _mem.Span;

		#endregion

		#region Type tag

		internal string? ReadTypeTag()
		{
			if (InListFrame)
				return null;
			if (!FindField(TypeTagFieldNumber, out int vs, out _))
				return null;
			var span = _mem.Span;
			var (s, e) = ReadLenPayload(span, vs);
			return Utf8Decode(span.Slice(s, e - s));
		}

		#endregion

		#region BeginSubObject / EndSubObject

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, ObjectMode mode)
		{
			var span = _mem.Span;
			FrameKind kind = (mode & ObjectMode.Tuple) == ObjectMode.Tuple ? FrameKind.Tuple
				: (mode & ObjectMode.List) != 0 ? FrameKind.List : FrameKind.Object;
			bool dedup = (mode & ObjectMode.Deduplicate) != 0;
			bool nullable = (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;

			if (_top == null)
				return BeginRoot(span, kind, dedup);

			if (InListFrame) {
				if (ReachedEndOfList == true)
					return (false, 0, null);
				var (s, e) = NextElemPayload(span);
				if (dedup)
					return BeginDedup(span, s, e, kind);
				if (nullable) {
					// Nullable element wrapper: {} = null, { 1: body } otherwise
					if (!FindWrapperField(span, s, e, 1, out int vs, out WireType wire))
						return (false, 0, null);
					if (wire != WireType.Len)
						throw Error(vs, "expected a length-delimited value");
					var (bs, be) = ReadLenPayload(span, vs);
					return PushBody(kind, bs, be, 0, false);
				}
				return PushBody(kind, s, e, 0, false); // NotNull element: direct body
			}

			// Field context: consume the field number even if the field is absent
			if (!TryGetField(name, out int fvs, out WireType fwire))
				return (false, 0, null); // absent -> null object/list
			if (fwire != WireType.Len)
				throw Error(fvs, "expected a length-delimited value");
			var (ps, pe) = ReadLenPayload(span, fvs);
			if (dedup)
				return BeginDedup(span, ps, pe, kind);
			return PushBody(kind, ps, pe, 0, false);
		}

		// The root value is a bare message body occupying the whole input.
		(bool Begun, int Length, object? Object) BeginRoot(ReadOnlySpan<byte> span, FrameKind kind, bool dedup)
		{
			if (kind != FrameKind.Object)
				throw new NotSupportedException(
					"SyncProtobuf: the root value must be an object (a Protobuf message), not a list or tuple.");
			if (_mem.Length == 0)
				return (false, 0, null); // zero bytes = null root
			if (dedup)
				return BeginDedup(span, 0, _mem.Length, kind);
			return PushBody(kind, 0, _mem.Length, 0, false);
		}

		// Interprets [s, e) as a dedup wrapper { 1: id, 2: body } / { 1: id } / {}.
		(bool Begun, int Length, object? Object) BeginDedup(ReadOnlySpan<byte> span, int s, int e, FrameKind kind)
		{
			if (s == e)
				return (false, 0, null); // empty wrapper = null
			if (!FindWrapperField(span, s, e, 1, out int idStart, out _))
				throw Error(s, "deduplicated object has no id");
			int p = idStart;
			long id = (long)ReadVarint(span, ref p);
			if (FindWrapperField(span, s, e, 2, out int valStart, out WireType valWire)) {
				if (valWire != WireType.Len)
					throw Error(valStart, "expected a length-delimited value");
				var (bs, be) = ReadLenPayload(span, valStart);
				return PushBody(kind, bs, be, id, true);
			}
			// Back-reference: no body
			if (_objects == null || !_objects.TryGetValue(id, out var existing))
				throw Error(s, "dangling deduplication back-reference");
			return (false, 0, existing);
		}

		(bool Begun, int Length, object? Object) PushBody(FrameKind kind, int start, int end, long id, bool hasId)
		{
			var frame = new RFrame {
				Kind = kind,
				SavedLastFieldId = _lastFieldId,
				Id = id,
				HasId = hasId,
			};
			if (kind == FrameKind.List) {
				frame.Entries = IndexListEntries(start, end);
			} else {
				frame.Fields = IndexFields(start, end);
			}
			Push(frame);
			return (true, kind == FrameKind.Object ? 1 : int.MaxValue, null);
		}

		public void EndSubObject()
		{
			var frame = _top!;
			_stack.RemoveAt(_stack.Count - 1);
			_top = _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
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
			if (_top != null && _top.HasId) {
				_objects ??= new Dictionary<long, object>();
				_objects[_top.Id] = value;
			}
		}

		internal void RegisterObject(long id, object value)
		{
			_objects ??= new Dictionary<long, object>();
			_objects[id] = value;
		}

		internal FieldId NextField {
			get {
				var top = _top;
				if (top == null || top.Kind == FrameKind.List || top.Fields == null)
					return FieldId.Missing;
				// Skip the reserved field numbers (_type and _present markers)
				for (int i = top.Cursor; i < top.Fields.Count; i++) {
					int num = top.Fields[i].Num;
					if (num < PresentFieldNumber)
						return new FieldId(null, num);
				}
				return FieldId.Missing;
			}
		}

		internal SyncType GetFieldType(FieldId name, SyncType expectedType)
		{
			if (IsInsideList)
				return SyncType.Unknown;
			if (_top == null && _mem.Length == 0)
				return SyncType.Missing;
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
			var fields = TopObject().Fields;
			if (fields != null)
				for (int i = fields.Count - 1; i >= 0; i--)
					if (fields[i].Num == id) { wire = fields[i].Wire; return true; }
			wire = default;
			return false;
		}

		static string Utf8Decode(ReadOnlySpan<byte> bytes)
		{
			#if NETSTANDARD2_0
			return Encoding.UTF8.GetString(bytes.ToArray());
			#else
			return Encoding.UTF8.GetString(bytes);
			#endif
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
