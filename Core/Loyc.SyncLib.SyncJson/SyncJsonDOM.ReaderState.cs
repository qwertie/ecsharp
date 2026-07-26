// JsonDocument is unavailable in .NET Standard 2.x and .NET Framework (and this
// library avoids taking a dependency on the System.Text.Json package there)
#if NETCOREAPP3_0_OR_GREATER

using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace Loyc.SyncLib;

partial class SyncJsonDOM
{
	/// <summary>
	///   ReaderState is responsible for the higher-level idea of reading specific
	///   properties from a <see cref="JsonElement"/> tree: finding properties (in or
	///   out of order), tracking which properties were already read, dealing with
	///   type conversions and type errors, and supporting object deduplication.
	/// </summary><remarks>
	///   This class plays the same role as SyncJson.ReaderState, but it is much
	///   simpler because System.Text.Json has already parsed the document: finding a
	///   property out of order is a simple lookup in the current object, and reading
	///   an object that was skipped earlier just means starting a new stack frame at
	///   that element (no "replay" machinery is needed).
	/// <para/>
	///   Each stack frame remembers a cursor: the index of the next unread property
	///   (or list item). Reading a property at the cursor advances it; reading a
	///   property beyond the cursor records its index in a "consumed" set so that
	///   cursor-based (in-order) reading skips it later. Properties are never
	///   consumed when an attempted read fails, so type errors are recoverable.
	/// </remarks>
	internal class ReaderState
	{
		internal readonly SyncJson.Options _opt;
		internal readonly SyncJson.Options.ForReader _optRead;

		enum FrameKind {
			Root,   // a pseudo-list containing only the root element
			Object, // a JSON object
			List,   // a JSON array (possibly unwrapped from {"$id":..., "$values":[...]})
		}

		class Frame
		{
			public FrameKind Kind;
			public JsonElement Element; // the object or array (Root: the root value)
			public int Index;           // next unread list index, or object property cursor
			public int Count;           // list/root only: number of items
			public HashSet<int>? Consumed; // object only: props at index > cursor read out of order
			public string? Id;          // value of the "$id"/"\f" property, if any
		}

		readonly List<Frame> _stack = new List<Frame>();
		Frame Top => _stack[_stack.Count - 1];

		// Map from object IDs to already-deserialized objects (see SetCurrentObject)
		Dictionary<string, object>? _objects;
		// Map from object IDs to their elements; built lazily when a back-reference
		// points to an object that hasn't been deserialized yet.
		Dictionary<string, JsonElement>? _idIndex;
		// Caches the results of _opt.NameConverter so that it doesn't reallocate the
		// same strings numerous times.
		Dictionary<string, string>? _nameCache;

		public ReaderState(JsonElement root, SyncJson.Options options)
		{
			_opt = options;
			_optRead = options.Read;
			_stack.Add(new Frame { Kind = FrameKind.Root, Element = root, Count = 1 });
		}

		public int Depth => _stack.Count - 1;

		public bool IsInsideList => Top.Kind != FrameKind.Object;

		public bool ReachedEndOfList => Top.Index >= Top.Count;

		// Note: unlike SyncJson.Reader, the total list length is known in advance
		public int ListLengthRemaining => IsInsideList ? System.Math.Max(Top.Count - Top.Index, 0) : 0;

		public void SetCurrentObject(object value)
		{
			if (_stack.Count > 1 && Top.Id != null)
				(_objects ??= new Dictionary<string, object>())[Top.Id] = value;
		}

		// Applies _opt.NameConverter, caching its result per distinct property name.
		string ConvertName(string name)
		{
			_nameCache ??= new Dictionary<string, string>();
			if (!_nameCache.TryGetValue(name, out var converted))
				_nameCache[name] = converted = _opt.NameConverter!(name);
			return converted;
		}

		#region Property lookup (cursor + out-of-order reads)

		static JsonProperty? PropAt(in JsonElement obj, int index)
		{
			if (index >= 0) {
				int i = 0;
				foreach (var p in obj.EnumerateObject())
					if (i++ == index)
						return p;
			}
			return null;
		}

		static void SkipConsumed(Frame f)
		{
			while (f.Consumed != null && f.Consumed.Contains(f.Index))
				f.Index++;
		}

		public FieldId NextField
		{
			get {
				var f = Top;
				if (f.Kind != FrameKind.Object)
					return FieldId.Missing;
				SkipConsumed(f);
				return PropAt(f.Element, f.Index) is JsonProperty p ? p.Name : FieldId.Missing;
			}
		}

		/// <summary>Finds the value of the property or list item that `name` refers to,
		///   without consuming it. Returns false if it wasn't found (which, inside a
		///   list, means the end of the list was reached).</summary>
		internal bool TryFindProp(string? name, out JsonElement value, out int index)
		{
			var f = Top;
			if (f.Kind != FrameKind.Object) {
				index = f.Index;
				if (f.Index < f.Count) {
					value = f.Kind == FrameKind.Root ? f.Element : f.Element[f.Index];
					return true;
				}
				value = default;
				return false;
			}

			SkipConsumed(f);
			if (name == null) {
				// A request for the current property, whatever its name is
				index = f.Index;
				var p = PropAt(f.Element, f.Index);
				value = p?.Value ?? default;
				return p != null;
			}

			string converted = _opt.NameConverter == null ? name : ConvertName(name);
			if (TryFindPropNamed(f, converted, out value, out index))
				return true;
			// Fallback: if there is a NameConverter, try the original name too
			return converted != name && TryFindPropNamed(f, name, out value, out index);
		}

		static bool TryFindPropNamed(Frame f, string name, out JsonElement value, out int index)
		{
			int i = 0;
			foreach (var p in f.Element.EnumerateObject()) {
				// Everything before the cursor was already consumed
				if (i >= f.Index && (f.Consumed == null || !f.Consumed.Contains(i)) && p.NameEquals(name)) {
					value = p.Value;
					index = i;
					return true;
				}
				i++;
			}
			value = default;
			index = -1;
			return false;
		}

		/// <summary>Marks the property/item at `index` as read. Call this only after a
		///   read succeeds, so that failed reads (e.g. type mismatches) are recoverable.</summary>
		void Consume(int index)
		{
			var f = Top;
			if (f.Kind != FrameKind.Object) {
				f.Index = index + 1;
			} else if (index == f.Index) {
				f.Index++;
				SkipConsumed(f);
			} else {
				(f.Consumed ??= new HashSet<int>()).Add(index);
			}
		}

		// Consumes a null list item, which nothing else would consume (an object/list
		// element is consumed by BeginSubObject, but a null never "begins").
		internal void ConsumeNullListItem(string? name)
		{
			if (name == null && Top.Kind != FrameKind.Object
				&& TryFindProp(null, out var v, out int index) && v.ValueKind == JsonValueKind.Null)
				Consume(index);
		}

		/// <summary>Like TryFindProp, but on failure it throws (or, if a named field is
		///   simply absent and Options.Read.AllowMissingFields is true, returns false).</summary>
		bool FindPropOrThrow(string? name, out JsonElement value, out int index)
		{
			if (TryFindProp(name, out value, out index))
				return true;
			if (Top.Kind == FrameKind.Object && name != null) {
				if (_optRead.AllowMissingFields)
					return false;
				throw NotFoundError(name);
			}
			throw NewError(Top.Kind == FrameKind.Object
				? "Attempted to read beyond the last property of a JSON object"
				: "Attempted to read beyond the end of a JSON list");
		}

		#endregion

		#region BeginSubObject/EndSubObject and deduplication

		internal (bool Begun, int Length, object? Object) BeginSubObject(string? name, ObjectMode mode)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return (false, 0, null); // AllowMissingFields

			bool expectList = (mode & ObjectMode.List) != 0;
			switch (v.ValueKind) {
				case JsonValueKind.Null:
					if ((mode & ObjectMode.NotNull) != 0)
						throw NewError("\"{0}\" is not nullable, but was null".Localized(name));
					Consume(index);
					return (false, 0, null);

				case JsonValueKind.Array:
					if (!expectList)
						throw NewError("Expected object, got list");
					Consume(index);
					return (true, PushList(v, null), null);

				case JsonValueKind.Object:
					return BeginJsonObject(v, index, name, expectList);

				default:
					throw NewError("\"{0}\" was expected to be a {1}, but it was a {2}"
						.Localized(name ?? "list item", expectList ? "list" : "object", v.ValueKind));
			}
		}

		(bool Begun, int Length, object? Object) BeginJsonObject(JsonElement obj, int parentIndex, string? name, bool expectList)
		{
			// Check if the object is a backreference: {"$ref": id} or {"\r": id}
			string? refId = GetMetaId(obj, "$ref", "\r", requireSingleProp: true);
			if (refId != null) {
				if (_objects != null && _objects.TryGetValue(refId, out object? existing)) {
					Consume(parentIndex);
					return (false, 0, existing);
				}
				// The target hasn't been deserialized yet (it was skipped, or appears
				// later in the document). Find it and read it instead.
				if (!TryFindElementById(refId, out obj))
					throw NewError("Backreferenced object not found: {0}".Localized(refId));
			}

			// Read object ID, if any ("$id" or "\f")
			string? id = GetMetaId(obj, "$id", "\f", requireSingleProp: false);
			if (id != null && _objects != null && _objects.TryGetValue(id, out object? read)) {
				// This exact object was already deserialized (it was reached earlier
				// through a backreference). Return the existing instance.
				Consume(parentIndex);
				return (false, 0, read);
			}

			if (expectList) {
				// A deduplicated list is wrapped in an object:
				// {"$id":"1", "$values":[...]} or {"\f":1, "":[...]}
				if (id != null && PropAt(obj, 1) is JsonProperty values
					&& (values.NameEquals("$values") || values.NameEquals(""))
					&& values.Value.ValueKind == JsonValueKind.Array) {
					Consume(parentIndex);
					return (true, PushList(values.Value, id), null);
				}
				throw NewError("Expected list, got object");
			}

			Consume(parentIndex);
			Push(new Frame {
				Kind = FrameKind.Object, Element = obj,
				Index = id != null ? 1 : 0, // skip the "$id" prop, if any
				Id = id,
			});
			return (true, 1, null);
		}

		internal void EndSubObject()
		{
			Debug.Assert(_stack.Count > 1);
			_stack.RemoveAt(_stack.Count - 1);
		}

		int PushList(in JsonElement list, string? id)
		{
			int count = list.GetArrayLength();
			Push(new Frame { Kind = FrameKind.List, Element = list, Count = count, Id = id });
			return count;
		}

		void Push(Frame frame)
		{
			if (Depth >= _optRead.MaxDepth)
				throw NewError("JSON is too deeply nested (see Options.Read.MaxDepth)");
			_stack.Add(frame);
		}

		/// <summary>If the first property of `obj` is named n1 or n2 and has a primitive
		///   value (and, optionally, is the only property), returns the value as a string
		///   suitable for use as a dictionary key; otherwise returns null.</summary>
		static string? GetMetaId(in JsonElement obj, string n1, string n2, bool requireSingleProp)
		{
			if (PropAt(obj, 0) is not JsonProperty p)
				return null;
			if (!p.NameEquals(n1) && !p.NameEquals(n2))
				return null;
			var v = p.Value;
			if (v.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
				return null; // apparently not a real id/backreference
			if (requireSingleProp && PropAt(obj, 1) != null)
				return null;
			return IdToString(v);
		}

		static string IdToString(in JsonElement v)
			=> v.ValueKind == JsonValueKind.String ? v.GetString()! : v.GetRawText();

		bool TryFindElementById(string id, out JsonElement element)
		{
			if (_idIndex == null) {
				_idIndex = new Dictionary<string, JsonElement>();
				IndexIds(_stack[0].Element);
			}
			return _idIndex.TryGetValue(id, out element);
		}

		void IndexIds(in JsonElement el)
		{
			switch (el.ValueKind) {
				case JsonValueKind.Object:
					var id = GetMetaId(el, "$id", "\f", requireSingleProp: false);
					if (id != null && !_idIndex!.ContainsKey(id))
						_idIndex[id] = el;
					foreach (var p in el.EnumerateObject())
						IndexIds(p.Value);
					break;
				case JsonValueKind.Array:
					foreach (var item in el.EnumerateArray())
						IndexIds(item);
					break;
			}
		}

		#endregion

		internal string? ReadTypeTag()
		{
			var f = Top;
			if (f.Kind != FrameKind.Object)
				return null;
			SkipConsumed(f);
			var p = PropAt(f.Element, f.Index);
			if (p != null && (p.Value.NameEquals("\t") || p.Value.NameEquals("$type")))
				return ReadString(null); // the cursor is at the tag, so this reads it
			return null;
		}

		internal SyncType HasField(string? name)
		{
			if (!TryFindProp(name, out var v, out _))
				return SyncType.Missing;

			// Translate JsonValueKind to SyncType
			switch (v.ValueKind) {
				case JsonValueKind.String: return SyncType.String;
				case JsonValueKind.Number: return HasIntegerText(v) ? SyncType.Integer : SyncType.Float;
				case JsonValueKind.Null:   return SyncType.Null;
				case JsonValueKind.False:  return SyncType.Boolean;
				case JsonValueKind.True:   return SyncType.Boolean;
				case JsonValueKind.Array:  return SyncType.List;
				case JsonValueKind.Object: return SyncType.Object;
			}
			return SyncType.Missing;
		}

		static bool HasIntegerText(in JsonElement number)
		{
			string raw = number.GetRawText();
			#if NET8_0_OR_GREATER
			return raw.AsSpan().IndexOfAny(_floatIndicators) < 0;
			#else
			return raw.IndexOfAny(_floatIndicators) < 0;
			#endif
		}
		#if NET8_0_OR_GREATER
		// Inactive until a net8.0 target is added.
		static readonly System.Buffers.SearchValues<char> _floatIndicators
			= System.Buffers.SearchValues.Create(".eE");
		#else
		static readonly char[] _floatIndicators = { '.', 'e', 'E' };
		#endif

		internal List? ReadByteArray<ListBuilder, List>(string? name, ListBuilder builder, ObjectMode mode)
			where ListBuilder : IListBuilder<List, byte>
		{
			if (TryFindProp(name, out JsonElement v, out int index)) {
				var kind = v.ValueKind;
				if (kind == JsonValueKind.Array || kind == JsonValueKind.Object) {
					// Read array in the standard way (BeginSubObject re-finds the prop,
					// and also handles the deduplicated form {"$id":..., "$values":[...]})
					var reader = new Reader(this);
					var loader = new ListLoader<Reader, List, byte, ListBuilder, SyncPrimitive<Reader>>(
						new SyncPrimitive<Reader>(), builder, mode);
					return loader.Sync(ref reader, name, default);
				} else if (kind == JsonValueKind.String) {
					var s = DecodeString(v);
					if (s.Length == 0) {
						Consume(index);
						return builder.Empty;
					} else if (s[0] != '!' && s[0] != '\b' &&
						(_opt.NewtonsoftCompatibility || _opt.ByteArrayMode != JsonByteArrayMode.Bais))
					{
						// Interpret as Base64
						byte[] bytes = Convert.FromBase64String(s);
						Consume(index);
						if (bytes is List list)
							return list;
						return BuildListFromSpan<ListBuilder, List>(bytes.AsSpan(), builder);
					}

					// Interpret as BAIS
					var output = ByteArrayInString.TryConvertToBytes(s);
					if (output.HasValue) {
						Consume(index);
						if (output.Value.AsMemory() is List memory)
							return memory;
						return BuildListFromSpan<ListBuilder, List>(output.Value.AsMemory().Span, builder);
					}
					throw NewError("Syntax error in BAIS byte array \"{0}\"".Localized(name));
				} else if (kind == JsonValueKind.Null) {
					ConsumeNullListItem(name);
				}
			}
			return default;
		}

		private List? BuildListFromSpan<ListBuilder, List>(Span<byte> span, ListBuilder builder)
			where ListBuilder : IListBuilder<List, byte>
		{
			builder.Alloc(span.Length);
			for (int i = 0; i < span.Length; i++)
				builder.Add(span[i]);

			return builder.List;
		}

		#region Primitive readers (String, Char, Integer, Double, Decimal, Boolean)

		/// <summary>Decodes a JSON string, tolerating unpaired surrogates (e.g. "\uDC00"),
		///   which SyncJson.Writer can produce but JsonElement.GetString() refuses to read.</summary>
		static string DecodeString(in JsonElement v)
		{
			try {
				return v.GetString()!;
			} catch (InvalidOperationException) {
				// The value is syntactically valid JSON (JsonDocument.Parse accepted it),
				// so this can only mean the string decodes to invalid UTF-16. Decode the
				// escape sequences ourselves, keeping any unpaired surrogates.
				string raw = v.GetRawText(); // includes the surrounding quotes
				var sb = new StringBuilder(raw.Length - 2);
				for (int i = 1; i < raw.Length - 1; i++) {
					char c = raw[i];
					if (c == '\\') {
						switch (c = raw[++i]) {
							case 'n': sb.Append('\n'); break;
							case 't': sb.Append('\t'); break;
							case 'r': sb.Append('\r'); break;
							case 'b': sb.Append('\b'); break;
							case 'f': sb.Append('\f'); break;
							case 'u':
								// AsSpan avoids allocating a 4-char string per \u escape.
							sb.Append((char) int.Parse(raw.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
								i += 4;
								break;
							default: sb.Append(c); break; // '"', '\\' or '/'
						}
					} else
						sb.Append(c);
				}
				return sb.ToString();
			}
		}

		public string? ReadString(string? name)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			string? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					result = DecodeString(v);
					break;
				case JsonValueKind.Number:
					result = v.GetRawText();
					break;
				case JsonValueKind.Null:
					result = null;
					break;
				case JsonValueKind.True:
					result = _optRead.TrueAsString;
					break;
				case JsonValueKind.False:
					result = _optRead.FalseAsString;
					break;
				default:
					if (_optRead.ObjectToPrimitive == null)
						throw UnexpectedTypeError(name, "string", v);
					result = ObjectToPrimitive(name, v, typeof(string))?.ToString();
					break;
			}
			Consume(index);
			return result;
		}

		public char? ReadChar(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			char? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					var s = DecodeString(v);
					result = s.Length > 0 ? s[0] : '\0';
					break;
				case JsonValueKind.Number:
					result = checked((char) (v.TryGetInt64(out long n) ? n : (long) v.GetDouble()));
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = 't';
					break;
				case JsonValueKind.False:
					result = 'f';
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, nullable ? typeof(char?) : typeof(char), "char", nullable);
					result = conv?.ToChar(null);
					break;
			}
			Consume(index);
			return result;
		}

		public BigInteger? ReadBigInt(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			BigInteger? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					var str = v.GetString()!;
					if (BigInteger.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
						result = parsed;
					else
						result = (BigInteger) double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
					break;
				case JsonValueKind.Number:
					result = HasIntegerText(v)
						? BigInteger.Parse(v.GetRawText(), CultureInfo.InvariantCulture)
						: (BigInteger) v.GetDouble();
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = 1;
					break;
				case JsonValueKind.False:
					result = 0;
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, nullable ? typeof(double?) : typeof(double), "integer", nullable);
					result = conv == null ? null : (BigInteger?) conv.ToDouble(null);
					break;
			}
			Consume(index);
			return result;
		}

		public long? ReadInt64(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			long? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					var str = v.GetString()!;
					if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsedInt64))
						result = parsedInt64;
					else if (BigInteger.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
						result = (long) _optRead.HandleOverflow(name, parsed, true);
					else
						result = checked((long) double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture));
					break;
				case JsonValueKind.Number:
					if (v.TryGetInt64(out long n))
						result = n;
					else if (HasIntegerText(v))
						result = unchecked((long) _optRead.HandleOverflow(name,
							BigInteger.Parse(v.GetRawText(), CultureInfo.InvariantCulture), true));
					else
						result = checked((long) v.GetDouble());
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = 1;
					break;
				case JsonValueKind.False:
					result = 0;
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, nullable ? typeof(long?) : typeof(long), "integer", nullable);
					result = conv?.ToInt64(null);
					break;
			}
			Consume(index);
			return result;
		}

		public ulong? ReadUInt64(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			ulong? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					var str = v.GetString()!;
					if (ulong.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong parsedUInt64))
						result = parsedUInt64;
					else if (BigInteger.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
						result = _optRead.HandleOverflow(name, parsed, false);
					else
						result = checked((ulong) double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture));
					break;
				case JsonValueKind.Number:
					if (v.TryGetUInt64(out ulong n))
						result = n;
					else if (HasIntegerText(v))
						result = _optRead.HandleOverflow(name,
							BigInteger.Parse(v.GetRawText(), CultureInfo.InvariantCulture), false);
					else
						result = checked((ulong) v.GetDouble());
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = 1;
					break;
				case JsonValueKind.False:
					result = 0;
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, nullable ? typeof(ulong?) : typeof(ulong), "integer", nullable);
					result = conv?.ToUInt64(null);
					break;
			}
			Consume(index);
			return result;
		}

		public double? ReadDouble(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			double? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					// Note: this handles "NaN", "Infinity" and "-Infinity", which is
					// how SyncJson.Writer writes non-finite numbers
					result = double.Parse(v.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture);
					break;
				case JsonValueKind.Number:
					result = v.GetDouble();
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = 1;
					break;
				case JsonValueKind.False:
					result = 0;
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, nullable ? typeof(double?) : typeof(double), "double", nullable);
					result = conv?.ToDouble(null);
					break;
			}
			Consume(index);
			return result;
		}

		public decimal? ReadDecimal(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			decimal? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					result = decimal.Parse(v.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture);
					break;
				case JsonValueKind.Number:
					if (v.TryGetDecimal(out decimal d))
						result = d;
					else if (HasIntegerText(v))
						result = (decimal) BigInteger.Parse(v.GetRawText(), CultureInfo.InvariantCulture);
					else
						result = (decimal) v.GetDouble();
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = 1;
					break;
				case JsonValueKind.False:
					result = 0;
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, nullable ? typeof(decimal?) : typeof(decimal), "decimal", nullable);
					result = conv?.ToDecimal(null);
					break;
			}
			Consume(index);
			return result;
		}

		public bool? ReadBoolean(string? name, bool nullable)
		{
			if (!FindPropOrThrow(name, out JsonElement v, out int index))
				return null; // missing property

			bool? result;
			switch (v.ValueKind) {
				case JsonValueKind.String:
					var str = v.GetString()!;
					if (bool.TryParse(str, out bool parsed))
						result = parsed;
					else
						result = double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture) != 0;
					break;
				case JsonValueKind.Number:
					if (v.TryGetInt64(out long n))
						result = n != 0;
					else if (HasIntegerText(v))
						result = !BigInteger.Parse(v.GetRawText(), CultureInfo.InvariantCulture).IsZero;
					else
						result = v.GetDouble() != 0;
					break;
				case JsonValueKind.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(name);
					result = null;
					break;
				case JsonValueKind.True:
					result = true;
					break;
				case JsonValueKind.False:
					result = false;
					break;
				default:
					var conv = ReadObjectAsPrimitive(name, v, typeof(bool), "boolean", nullable);
					result = conv?.ToBoolean(null);
					break;
			}
			Consume(index);
			return result;
		}

		// Handles the case where a primitive was requested but an object/list was found,
		// by calling Options.Read.ObjectToPrimitive (or throwing if there isn't one).
		IConvertible? ReadObjectAsPrimitive(string? name, in JsonElement v, Type type, string expected, bool nullable)
		{
			if (_optRead.ObjectToPrimitive == null)
				throw UnexpectedTypeError(name, expected, v);

			var result = ObjectToPrimitive(name, v, type);
			if (result == null && !nullable)
				throw NewError(NullMessage(name, nullFromConverter: true));
			return result;
		}

		IConvertible? ObjectToPrimitive(string? name, in JsonElement v, Type type)
		{
			// The delegate expects the raw UTF-8 bytes of the JSON object or list
			var utf8 = Encoding.UTF8.GetBytes(v.GetRawText());
			return _optRead.ObjectToPrimitive!(name, utf8, 0, type);
		}

		#endregion

		#region Error helpers

		internal static Exception NewError(string msg) => new FormatException(msg);

		internal Exception UnexpectedNullError(string? name)
			=> NewError(NullMessage(name, nullFromConverter: false));

		static string NullMessage(string? name, bool nullFromConverter)
		{
			string msg = nullFromConverter
				? "ObjectToPrimitive returned null for non-nullable JSON value"
				: "null encountered in JSON at non-nullable location";
			if (name != null)
				msg += " \"" + name + '"';
			return msg;
		}

		internal Exception NotFoundError(string? name)
			=> NewError("Property not found: {0}".Localized(name));

		internal Exception UnexpectedTypeError(string? name, string expected, in JsonElement v)
		{
			var msg = "Expected {0}, got {1} from JSON".Localized(expected, v.ValueKind);
			if (name != null)
				msg += " \"" + name + '"';
			return NewError(msg);
		}

		#endregion
	}
}

#endif
