using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Math;

namespace Loyc.SyncLib;

partial class SyncJson
{
	/// <summary>
	///   ReaderState is responsible for managing the higher-level idea of reading 
	///   specific properties from a JSON stream: "finding" properties (and skipping 
	///   over properties we're not reading yet), "rewinding" to skipped objects, 
	///   dealing with type conversions and type errors, and supporting object
	///   deduplication.
	/// </summary><remarks>
	///   Please see the description of <see cref="Parser"/> to understand the 
	///   low-level API that this class depends on. That description also summarizes
	///   some key points about how this class works.
	/// </remarks>
	internal class ReaderState : Parser
	{
		public ReaderState(ReadOnlyMemory<byte> memory, Options options) : base(memory, options) { }
		public ReaderState(IScanner<byte> scanner,      Options options) : base(scanner, options) { }

		internal string? ReadTypeTag()
		{
			var keySpan = NextFieldKey.Span;
			if (AreEqual(keySpan, _t) || AreEqual(keySpan, _type))
				return ReadString(null);
			return null;
		}

		public void SetCurrentObject(object value) {
			if (_stack.Count != 0 && _stack.Last.Id.Text.Length != 0) {
				_objects ??= new Dictionary<JsonValue, object>();
				_objects[_stack.Last.Id] = value;
			}
		}
			
		// Map from object IDs to objects.
		private Dictionary<JsonValue, object>? _objects;
		// TODO: delete _skippedObjects entry after reading the object into _objects
		private Dictionary<JsonValue, (JsonValue value, long position)>? _skippedObjects;

		byte[] _nameBuf = Empty<byte>.Array;

		// Caches the results of _opt.NameConverter so that it doesn't reallocate the
		// same strings numerous times.
		Dictionary<string, string>? _nameCache;

		// Applies _opt.NameConverter, caching its result per distinct property name.
		string ConvertName(string name)
		{
			_nameCache ??= new Dictionary<string, string>();
			if (!_nameCache.TryGetValue(name, out var converted))
				_nameCache[name] = converted = _opt.NameConverter!(name);
			return converted;
		}

		public FieldId NextField
			=> CurPropKey.Type <= JsonType.NotParsed 
				? FieldId.Missing
				: DecodeString(CurPropKey);

		Memory<byte> ToNameBuf(ReadOnlySpan<char> name)
		{
			int len = WriterState.GetLengthAsBytes(name, false);
			if (_nameBuf.Length < len)
				_nameBuf = new byte[Max((len | 7) + 1, 16)];
			int i = 0;
			// TODO: don't use EscapeUnicode here
			WriterState.WriteStringCore(_nameBuf.AsSpan(), name, len, ref i, _opt.Write.EscapeUnicode);
			Debug.Assert(i == len + 2);
			return _nameBuf.AsMemory().Slice(1, len);
		}



		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool FindProp(string? name, out (JsonValue value, long position) v)
		{
			v = (default(JsonValue), CurPosition);
			if (IsInsideList || name == null)
				return true;

			var originalName = name;
			if (_opt.NameConverter != null)
				name = ConvertName(name);

			// Is it the current property?
			if (AreEqual(in CurPropKey, name))
				return true;

			return FindPropOutOfOrder(name, originalName, ref v);
		}
		bool FindPropOutOfOrder(string? name, string originalName, ref (JsonValue value, long position) v)
		{
			var skippedProps = _stack.LastRef.SkippedProps;

			// Is it something we skipped over earlier?
			Memory<byte> nameBytes;
			if (skippedProps != null) {
				nameBytes = ToNameBuf(name.AsSpan());
				if (skippedProps.TryGetValue(new JsonValue(JsonType.String, nameBytes), out v))
					return true;
			}

			var cur = CurPointer;

			// The current and skipped properties aren't what we need. Scan forward.
			if (cur.CurPropKey.Type != JsonType.Invalid) {
				while (true) {
					long valuePosition = PositionOf(cur);
					var skippedValue = ScanValue(ref cur);

					SaveSkippedValue(ref cur.CurPropKey, in skippedValue, valuePosition);

					if (!BeginProp(true, ref cur)) {
						// end of object, EOF, or syntax error
						if (IsCloserAt(cur))
							break;
						if (cur.Index >= cur.Buf.Length) {
							if (_stack.Count != 0)
								ThrowError(cur.Index, "JSON ended unexpectedly");
						} else {
							ThrowError(cur.Index, "JSON syntax error");
						}
					}

					if (AreEqual(cur.CurPropKey.Text.Span, cur.CurPropKey.Type, name)) {
						Commit(ref cur);
						v.position = PositionOf(cur);
						return true;
					}
				}
			} else if (!IsCloserAt(cur)) {
				throw NewError(cur.Index, "Syntax error in JSON object");
			}
				
			Commit(ref cur);

			if (skippedProps == null)
				return false;

			// Name was not found in this object. Fallback: if there is a
			// NameConverter, try looking for the orginalName.
			nameBytes = ToNameBuf(originalName.AsSpan());
			return skippedProps.TryGetValue(new JsonValue(JsonType.String, nameBytes), out v);
		}



		private void SaveSkippedValue(ref JsonValue propName, in JsonValue skippedValue, long position)
		{
			if (propName.Type == JsonType.SimpleString) {
				propName.Text = propName.Text.Slice(1, propName.Text.Length - 2);
				propName.Type = JsonType.String;
			} else if (propName.Type == JsonType.String) {
				// Normalize the value: e.g. "\u0041" => "A", otherwise it'll be unfindable.
				// This is not very efficient since it takes two passes over the string and
				// requires multiple memory allocations, but optimizing it isn't worthwhile:
				//   (1) escape sequences are rare in keys, but this key has 'em
				//   (2) it's on the slow path anyway - we're optimizing mainly for reading
				//       fields in the correct order, but the current field is unexpected or
				//       out of order
				var utf16 = DecodeString(propName);
				propName.Text = Encoding.UTF8.GetBytes(utf16);
			}

			ref var tos = ref _stack.LastRef;
			tos.SkippedProps ??= new Dictionary<JsonValue, (JsonValue value, long position)>();
			tos.SkippedProps[propName] = (skippedValue, position);
		}

		internal (bool Begun, int Length, object? Object) BeginSubObject(string? name, ObjectMode mode)
		{
			if (!FindProp(name, out (JsonValue value, long position) skippedObject)) {
				if (_optRead.AllowMissingFields)
					return (false, 0, null); 
				ThrowError(CurPointer.Index, "Property \"{0}\" was missing".Localized(name), fatal: false);
			}

			bool expectList = (mode & ObjectMode.List) != 0;

			// Begin the object/list, if there is an object/list here
			JsonPointer cur;
			var type = skippedObject.value.Type;
			bool isReplay = type != JsonType.NotParsed;
			if (isReplay) {
				BeginReplay(skippedObject, out cur);
				Debug.Assert(ReplayDepth > 0);
			} else {
				cur = CurPointer;
			}
			Debug.Assert(skippedObject.position == PositionOf(cur));

			type = TryToBeginObject(ref cur, expectList);

			if (type >= JsonType.FirstCompositeType)
			{
				if (type == JsonType.List) {
					if (expectList) {
						Commit(ref cur);
						return (true, int.MaxValue, null); // Success! List opened.
					} else {
						// Unexpected list!
						// TryToBeginObject() did NOT enter the list, so we need not
						// call UndoBeginObject() to return to the original position.
						// However, if we called BeginReplay(), we need to undo that.
						if (isReplay)
							EndReplay();
						int i = (int)(skippedObject.position - PositionOfBuf0);
						throw NewError(i, "Expected object, got list", fatal: false);
					}
				}

				// Check if the object is a backreference ("$ref" or "\r")
				var propKey = cur.CurPropKey;
				var keySpan = propKey.Text.Span;
				if (AreEqual(keySpan, _ref) || AreEqual(keySpan, _r))
				{
					long position = PositionOf(cur);
					var value = ScanValueAndBeginNext(ref cur);
						
					// A backreference must be a primitive and not followed by another prop
					if (value.Type is > JsonType.NotParsed and < JsonType.FirstCompositeType
						&& cur.CurPropKey.Text.Length == 0)
					{
						// Okay, good, we can finish reading the backref object (and if the
						// backref was a skipped object, return to the previous frame)
						EndSubObjectAndCommit(ref cur);

						// Now try to get the already-read deduplicated object.
						if (_objects != null && _objects.TryGetValue(value, out object? existing)) {
							// Good! Return it.
							return (false, 0, existing);
						} else {
							// The object wasn't read earlier. Was it skipped?
							if (_skippedObjects != null && _skippedObjects.TryGetValue(value, out skippedObject) 
									&& skippedObject.value.Type == JsonType.Object)	{
								// It was skipped! So now we need to switch to that skipped object
								// and begin reading it, similar to the case where FindProp finds
								// a skipped object. Note: don't worry, objects in _skippedObjects
								// cannot be backrefrences.
								BeginReplay(skippedObject, out cur);
								isReplay = true;
								type = TryToBeginObject(ref cur, expectList);
								Debug.Assert(type == JsonType.Object);
								propKey = cur.CurPropKey;
								keySpan = propKey.Text.Span;
							} else {
								// Sad!
								ThrowError(position, "Backreferenced object not found", fatal: false);
							}
						}
					} else {
						// Apparently this is not a real backreference; ignore it.
						SaveSkippedValue(ref propKey, value, CurPosition);
					}
				}

				// Read object ID, if any ("$id" or "\f")
				if (IsObjectIdProp(propKey))
				{
					var idValue = ScanValueAndBeginNext(ref cur);

					if (_objects != null && _objects.TryGetValue(idValue, out object? existing))
					{
						// Whoa! Either the same object ID was used twice, or this exact area
						// of the JSON was already read earlier. For example, suppose the user
						// of SyncLib reads "favorite" from this JSON first, and then reads
						// "items" afterward:
						// {
						//   "items": [{"\f":1, "name":"Joe"}, {"\f":2, "name":"Dan"}],
						//   "favorite": {"\r":1}
						// }
						// Reading "favorite" has the effect of "redirecting" the read operation
						// to `items[0]`, so when "items" is read later, the object with ID 1
						// has already been loaded and is normally stored in the _objects table.
						EndSubObjectAndCommit(ref cur);
						return (false, 0, existing);
					}
						
					if (idValue.Type == JsonType.Invalid)
					{
						ThrowSyntaxError(cur.Index, name ?? AsciiToString(keySpan));
					}
					else if (idValue.Type < JsonType.FirstCompositeType)
					{
						// The ID seems acceptable (though we haven't checked if it's a duplicate)
						_stack.LastRef.Id = idValue;

						// If caller expected a list, there should be a list prop next:
						// "$values":[...] or "":[...]
						keySpan = cur.CurPropKey.Text.Span;
						if (expectList) {
							if (keySpan.Length == 2 || AreEqual(keySpan, _values)) {
								if (TryOpenListValuesAndCommit(ref cur))
									return (true, int.MaxValue, null);
							}
							// No list found. An error will be thrown below.
						}
					}
					else
					{
						// Apparently this is not a real object id; ignore it.
						SaveSkippedValue(ref cur.CurPropKey, idValue, PositionOf(cur));
					}
				}

				if (expectList) {
					// Unexpected object! We can undo entering the object, so that the
					// error is not fatal. It's important that no Commit() was done
					// after entering the object, otherwise undo would be impossible.
					UndoBeginObject(ref cur);
					if (isReplay)
						EndReplay();

					ThrowError(skippedObject.position, "Expected list, got object", fatal: false);
				} else  {
					Commit(ref cur);
					return (true, 1, null); // success: object opened
				}
				Debug.Fail("unreachable");
			}

			if (type == JsonType.NotParsed)
				type = DetectTypeOfUnparsedValue(ref cur);

			if (type == JsonType.Null)
			{
				if ((mode & ObjectMode.NotNull) != 0)
					ThrowError(cur.Index, "\"{0}\" is not nullable, but was null".Localized(name));

				if (IsInsideList) {
					Debug.Assert(!isReplay);
					// Unlike an object element (whose EndSubObject advances the reader)
					// nothing will consume the null, so we must consume (skip) it here.
					var nullValue = ScanValue(ref cur);
					if (nullValue.Type == JsonType.Invalid)
						ThrowSyntaxError(cur.Index, name, "list item");
					BeginNext(ref cur);
					Commit(ref cur);
				}
				return (false, 0, null);
			}

			throw NewError(cur.Index, "\"{0}\" was expected to be a {1}, but it was a {2}"
				.Localized(name ?? "list item", expectList ? "list" : "object", type.ToString()));
		}

		internal void EndSubObject()
		{
			var cur = CurPointer;
			EndSubObjectAndCommit(ref cur);
		}
		private void EndSubObjectAndCommit(ref JsonPointer cur)
		{
			bool hasNextProp = EndObjectAndCommit(ref cur);
			if (!hasNextProp && cur.Index >= cur.Buf.Length && ReplayDepth > 0) {
				// Reached end of frame. Return to original frame.
				EndReplay();
			}
		}


		protected override bool IsObjectIdProp(in JsonValue key)
		{
			var keySpan = key.Text.Span;
			return AreEqual(keySpan, _id) || AreEqual(keySpan, _f);
		}
		protected override void SaveSkippedObjectWithId(in JsonValue idValue, in JsonValue @object, long position)
		{
			_skippedObjects ??= new Dictionary<JsonValue, (JsonValue value, long position)>();
				
			// Note: we can't use .Add() here because it is possible for the *same*
			// JSON object to be skipped more than once. For instance:
			//     {
			//        "A": {
			//           "X": { "$id": "9", "field": 111 },
			//           "Y": 222,
			//           "Z": { "$ref": "9" }
			//        },
			//        "B": 333
			//     }
			// If the user reads B, then A, then Y, object "9" is skipped when
			// reading "B" and again when reading "Y".
			_skippedObjects[idValue] = (@object, position);
		}

		internal SyncType HasField(string? name)
		{
			var type = HasFieldCore(name);

			// Translate JsonType to SyncType
			switch (type) {
				case JsonType.SimpleString: 
				case JsonType.String:       return SyncType.String;
				case JsonType.PlainInteger: return SyncType.Integer;
				case JsonType.Number:       return SyncType.Float;
				case JsonType.Null:         return SyncType.Null;
				case JsonType.False:        return SyncType.Boolean;
				case JsonType.True:         return SyncType.Boolean;
				case JsonType.List:         return SyncType.List;
				case JsonType.Object:       return SyncType.Object;
				case JsonType.Invalid:      return SyncType.Missing;
			}
			Debug.Fail("unreachable");
			return SyncType.Exists;
		}

		internal JsonType HasFieldCore(string? name)
		{
			if (FindProp(name, out var v)) {
				// Get type of property value
				var type = v.value.Type;
				if (type == JsonType.NotParsed) {
					var cur = CurPointer;
					type = DetectTypeOfUnparsedValue(ref cur);
				}
				return type;
			}
			return JsonType.Invalid;
		}

		internal List? ReadByteArray<ListBuilder, List>(string? name, ListBuilder builder, ObjectMode mode) 
			where ListBuilder : IListBuilder<List, byte>
		{
			if (FindProp(name, out (JsonValue value, long position) v)) {
				var type = v.value.Type;
				if (type == JsonType.NotParsed) {
					var cur = CurPointer;
					type = DetectTypeOfUnparsedValue(ref cur);
				}

				if (type == JsonType.List || type == JsonType.Object) {
					// Read array in the standard way
					// TODO: support { "$id":"...", "$values":"byte array as string" }
					var reader = new Reader(this);
					var loader = new ListLoader<Reader, List, byte, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode);
					return loader.Sync(ref reader, name, default);
				} else if (type == JsonType.String || type == JsonType.SimpleString) {
					v = ReadPrimitive(name);
					Debug.Assert(v.value.Type == JsonType.String || v.value.Type == JsonType.SimpleString);
					Debug.Assert(v.value.Text.Length >= 2);

					var text = v.value.Text; // note: text begins and ends with '"'
					if (text.Length == 2) {
						// empty string
						return builder.Empty;
					} else if (text.Span[1] != '!' && !(text.Span[1] == '\\' && text.Span[2] == 'b') &&
						(_opt.NewtonsoftCompatibility || _opt.ByteArrayMode != JsonByteArrayMode.Bais))
					{
						// Interpret as Base64
						// TODO: add ability to decode byte array directly from UTF-8 bytes
						string str = DecodeString(v.value);
						// TODO: make errors here properly nonfatal by saving skipped value
						// (also, catch+rethrow; technically it's not even marked fatal right now
						// but it malfunctions: the same field cannot necessarily be read again)
						byte[] bytes = Convert.FromBase64String(str);
						if (bytes is List list)
							return list;

						return BuildListFromSpan<ListBuilder, List>(bytes.AsSpan(), builder);
					}
						
					// Interpret as BAIS
					ArraySlice<byte>? output;
					if (v.value.Type == JsonType.SimpleString) {
						// Fast path: an ASCII string with no escape sequences
						output = ByteArrayInString.TryConvertToBytes(text.Span.Slice(1, text.Length - 2));
					} else {
						// The string contains JSON escape sequences (e.g. \b, \\, )
						// which must be decoded before interpreting the BAIS format
						output = ByteArrayInString.TryConvertToBytes(DecodeString(v.value));
					}
					if (output.HasValue) {
						if (output.Value.AsMemory() is List memory)
							return memory;

						return BuildListFromSpan<ListBuilder, List>(output.Value.AsMemory().Span, builder);
					}
					// TODO: make this nonfatal by saving skipped value
					ThrowSyntaxError((int)(v.position - PositionOfBuf0), name, "BAIS byte array");
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

		public string? ReadString(string? name)
		{
			(JsonValue value, long position) v = ReadPrimitive(name);

			switch (v.value.Type) {
				case JsonType.SimpleString:
				case JsonType.String:
					return DecodeString(v.value);

				case JsonType.PlainInteger:
				case JsonType.Number:
					return AsciiToString(v.value.Text.Span);

				case JsonType.Null:
					return null;
						
				case JsonType.True:
					return _optRead.TrueAsString;

				case JsonType.False:
					return _optRead.FalseAsString;

				case JsonType.Object:
				case JsonType.List:
					if (_optRead.ObjectToPrimitive == null)
						throw UnexpectedTypeError(v.position, name, "string", v.value.Type);
							
					return _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, typeof(string))?.ToString();
			}
			return null; // missing property
		}

		/// <summary>Type-specific behavior of ReadNumber. Implementations are structs
		///   so that the JIT specializes ReadNumber for each of them, making the
		///   interface calls direct and inlinable (same trick as IListBuilder).</summary>
		private interface INumberDecoder<T> where T : struct
		{
			/// <summary>Type name used in UnexpectedTypeError messages</summary>
			string TypeName { get; }
			/// <summary>Type passed to the user's ObjectToPrimitive handler</summary>
			Type ObjType(bool nullable);
			T True { get; }
			T False { get; }
			/// <summary>Converts a PlainInteger or Number to T</summary>
			T Decode(ReaderState s, in JsonValue value, string? name);
			/// <summary>Converts a JSON string (already decoded to UTF-16) to T</summary>
			T Parse(ReaderState s, string str, string? name);
			/// <summary>Converts the return value of ObjectToPrimitive to T</summary>
			T FromObject(IConvertible obj);
		}

		private T? ReadNumber<T, Decoder>(string? name, bool nullable, Decoder decoder)
			where T : struct where Decoder : INumberDecoder<T>
		{
			(JsonValue value, long position) v = ReadPrimitive(name);

			switch (v.value.Type) {
				case JsonType.SimpleString:
				case JsonType.String:
					return decoder.Parse(this, DecodeString(v.value), name);

				case JsonType.PlainInteger:
				case JsonType.Number:
					return decoder.Decode(this, in v.value, name);

				case JsonType.Null:
					if (!nullable && !_optRead.ReadNullPrimitivesAsDefault)
						throw UnexpectedNullError(v.position, name);
					return null;

				case JsonType.True:
					return decoder.True;

				case JsonType.False:
					return decoder.False;

				case JsonType.Object:
				case JsonType.List:
					if (_optRead.ObjectToPrimitive == null)
						throw UnexpectedTypeError(v.position, name, decoder.TypeName, v.value.Type);

					var result = _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, decoder.ObjType(nullable));
					if (result == null) {
						if (nullable)
							return null;
						throw UnexpectedNullError(v.position, name, true);
					}
					return decoder.FromObject(result);
			}
			return null; // missing property
		}

		public char?       ReadChar(string? name, bool nullable)    => ReadNumber<char, CharDecoder>(name, nullable, default);
		public BigInteger? ReadBigInt(string? name, bool nullable)  => ReadNumber<BigInteger, BigIntDecoder>(name, nullable, default);
		public long?       ReadInt64(string? name, bool nullable)   => ReadNumber<long, Int64Decoder>(name, nullable, default);
		public ulong?      ReadUInt64(string? name, bool nullable)  => ReadNumber<ulong, UInt64Decoder>(name, nullable, default);
		public double?     ReadDouble(string? name, bool nullable)  => ReadNumber<double, DoubleDecoder>(name, nullable, default);
		public decimal?    ReadDecimal(string? name, bool nullable) => ReadNumber<decimal, DecimalDecoder>(name, nullable, default);
		public bool?       ReadBoolean(string? name, bool nullable) => ReadNumber<bool, BoolDecoder>(name, nullable, default);

		private struct Int64Decoder : INumberDecoder<long>
		{
			public string TypeName => "integer";
			public Type ObjType(bool nullable) => nullable ? typeof(long?) : typeof(long);
			public long True => 1;
			public long False => 0;
			public long Decode(ReaderState s, in JsonValue v, string? name)
				=> v.Type == JsonType.PlainInteger
					? s.DecodeInt64(v.Text.Span, name)
					: checked((long) DecodeNumber(v.Text.Span));
			public long Parse(ReaderState s, string str, string? name)
			{
				if (long.TryParse(str, out long num))
					return num;
				if (BigInteger.TryParse(str, out var big))
					return (long) s._optRead.HandleOverflow(name, big, true);
				return checked((long) double.Parse(str, NumberStyles.Float));
			}
			public long FromObject(IConvertible obj) => obj.ToInt64(null);
		}

		private struct UInt64Decoder : INumberDecoder<ulong>
		{
			public string TypeName => "integer";
			public Type ObjType(bool nullable) => nullable ? typeof(ulong?) : typeof(ulong);
			public ulong True => 1;
			public ulong False => 0;
			public ulong Decode(ReaderState s, in JsonValue v, string? name)
				=> v.Type == JsonType.PlainInteger
					? s.DecodeUInt64(v.Text.Span, name)
					: checked((ulong) DecodeNumber(v.Text.Span));
			public ulong Parse(ReaderState s, string str, string? name)
			{
				if (ulong.TryParse(str, out ulong num))
					return num;
				if (BigInteger.TryParse(str, out var big))
					return s._optRead.HandleOverflow(name, big, false);
				return checked((ulong) double.Parse(str, NumberStyles.Float));
			}
			public ulong FromObject(IConvertible obj) => obj.ToUInt64(null);
		}

		private struct BigIntDecoder : INumberDecoder<BigInteger>
		{
			public string TypeName => "integer";
			public Type ObjType(bool nullable) => nullable ? typeof(double?) : typeof(double);
			public BigInteger True => 1;
			public BigInteger False => 0;
			public BigInteger Decode(ReaderState s, in JsonValue v, string? name)
				=> v.Type == JsonType.PlainInteger
					? DecodeBigInt(v.Text.Span)
					: (BigInteger) DecodeNumber(v.Text.Span);
			public BigInteger Parse(ReaderState s, string str, string? name)
				=> BigInteger.TryParse(str, out var parsed) ? parsed
					: (BigInteger) double.Parse(str, NumberStyles.Float);
			public BigInteger FromObject(IConvertible obj) => (BigInteger) obj.ToDouble(null);
		}

		private struct DoubleDecoder : INumberDecoder<double>
		{
			public string TypeName => "double";
			public Type ObjType(bool nullable) => nullable ? typeof(double?) : typeof(double);
			public double True => 1;
			public double False => 0;
			public double Decode(ReaderState s, in JsonValue v, string? name)
			{
				var span = v.Text.Span;
				if (v.Type != JsonType.PlainInteger)
					return DecodeNumber(span);
				// 18 digits or less (17 if negative) always fits in a long
				return span.Length <= 18 ? (double) s.DecodeInt64(span, name) : (double) DecodeBigInt(span);
			}
			public double Parse(ReaderState s, string str, string? name)
				=> double.Parse(str, NumberStyles.Float, CultureInfo.InvariantCulture);
			public double FromObject(IConvertible obj) => obj.ToDouble(null);
		}

		private struct DecimalDecoder : INumberDecoder<decimal>
		{
			public string TypeName => "decimal";
			public Type ObjType(bool nullable) => nullable ? typeof(decimal?) : typeof(decimal);
			public decimal True => 1;
			public decimal False => 0;
			public decimal Decode(ReaderState s, in JsonValue v, string? name)
			{
				var span = v.Text.Span;
				if (v.Type != JsonType.PlainInteger)
					return DecodeDecimal(span);
				// 18 digits or less (17 if negative) always fits in a long
				return span.Length <= 18 ? (decimal) s.DecodeInt64(span, name) : (decimal) DecodeBigInt(span);
			}
			public decimal Parse(ReaderState s, string str, string? name)
				=> decimal.Parse(str, NumberStyles.Float);
			public decimal FromObject(IConvertible obj) => obj.ToDecimal(null);
		}

		private struct CharDecoder : INumberDecoder<char>
		{
			public string TypeName => "char";
			public Type ObjType(bool nullable) => nullable ? typeof(char?) : typeof(char);
			public char True => 't';
			public char False => 'f';
			public char Decode(ReaderState s, in JsonValue v, string? name)
				=> v.Type == JsonType.PlainInteger
					? checked((char) s.DecodeInt64(v.Text.Span, name))
					: checked((char) DecodeNumber(v.Text.Span));
			public char Parse(ReaderState s, string str, string? name) => str.TryGet(0, '\0');
			public char FromObject(IConvertible obj) => obj.ToChar(null);
		}

		private struct BoolDecoder : INumberDecoder<bool>
		{
			public string TypeName => "boolean";
			public Type ObjType(bool nullable) => nullable ? typeof(bool?) : typeof(bool);
			public bool True => true;
			public bool False => false;
			public bool Decode(ReaderState s, in JsonValue v, string? name)
			{
				var span = v.Text.Span;
				if (v.Type != JsonType.PlainInteger)
					return DecodeNumber(span) != 0;
				// 18 digits or less (17 if negative) always fits in a long
				return span.Length <= 18 ? s.DecodeInt64(span, name) != 0 : DecodeBigInt(span) != 0;
			}
			public bool Parse(ReaderState s, string str, string? name)
				=> bool.TryParse(str, out bool parsed) ? parsed : double.Parse(str) != 0;
			public bool FromObject(IConvertible obj) => obj.ToBoolean(null);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (JsonValue value, long position) ReadPrimitive(string? name)
		{
			if (TryReadPrimitive(name, out (JsonValue value, long position) v))
				return v;
			return MissingValue(name);
		}

		private (JsonValue value, long position) MissingValue(string? name)
		{
			if (_optRead.AllowMissingFields)
				return (new JsonValue(JsonType.Missing, default), CurPosition);
			throw NotFoundError(name);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private bool TryReadPrimitive(string? name, out (JsonValue value, long position) v)
		{
			if (FindProp(name, out v)) {
				if (v.value.Type == JsonType.NotParsed) {
					var cur = CurPointer;
					v.value = ScanValue(ref cur);
						
					if (v.value.Type == JsonType.Invalid)
						ThrowSyntaxError(cur.Index, name);

					BeginNext(ref cur);
					Commit(ref cur);
				}
				return true;
			}
			return false;
		}

		#endregion




		static bool AreEqual(ReadOnlySpan<byte> curProp, byte[] name)
		{
			if (name.Length != curProp.Length)
				return false;
			for (int i = 0; i < name.Length; i++)
				if (name[i] != curProp[i])
					return false;
			return true;
		}
		private bool AreEqual(in JsonValue value, string? name) => AreEqual(value.Text.Span, value.Type, name);
		private bool AreEqual(ReadOnlySpan<byte> curProp, JsonType type, string? name)
		{
			if (name == null)
				return false;
			if (type == JsonType.String) {
				curProp = curProp.Slice(1, curProp.Length - 2); // strip off the quotes

				// Comparing a JSON-escaped UTF-8 string to a UTF-16 string: it's tricky.
				// But one thing we can be sure of is that `name` shouldn't be longer.
				if (name.Length > curProp.Length)
					return false;

				int cp_i = 0;
				for (int i = 0; i < name.Length; i++) {
					if ((uint)cp_i >= (uint)curProp.Length)
						return false;

					// Match quickly in case of simple ASCII
					byte b = curProp[cp_i];
					if (b == name[i]) {
						cp_i++;
						if (b == '\\') {
							if (cp_i < curProp.Length && curProp[cp_i] != '\\')
								return false;
							cp_i++;
						} else if (b >= 0x80)
							return false;
					} else {
						// Slow path: decode both characters (one UTF8, one UTF16)
						int cp_c = DecodeStringChar(curProp, ref cp_i);
						int name_c = new UString(name).TryDecodeAt(i);
						if (cp_c != name_c)
							return false;
						if (name_c >= 0x10000) // detect surrogate pair & skip it properly
							i++;
					}
				}
				return cp_i == curProp.Length;
			} else {
				if (type == JsonType.SimpleString) {
					curProp = curProp.Slice(1, curProp.Length - 2); // strip off the quotes
				} else {
					if (_optRead.Strict)
						ThrowError(CurPointer.PropKeyIndex, "String expected");
					else if (type >= JsonType.FirstCompositeType)
						return false;
				}

				// Fast path: curProp has no escape sequences and no non-ASCII bytes
				if (name.Length != curProp.Length)
					return false;
				for (int i = 0; i < name.Length; i++)
					if (name[i] != curProp[i])
						return false;
				return true;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public Exception UnexpectedNullError(long position, string? name, bool nullFromConverter = false)
		{
			string msg;
			if (nullFromConverter)
				msg = "ObjectToPrimitive returned null for non-nullable JSON value";
			else
				msg = "null encountered in JSON at non-nullable location";
			if (name != null)
				msg += " \"" + name + '"';
			return NewError(position, msg, false);
		}
			
		[MethodImpl(MethodImplOptions.NoInlining)]
		public Exception NotFoundError(string? name)
			=> NewError(CurPosition, "Property not found: {0}".Localized(name), false);
			
		[MethodImpl(MethodImplOptions.NoInlining)]
		internal Exception UnexpectedTypeError(long position, string? name, string expected, JsonType type)
		{
			// TODO: localize all errors exactly once
			var msg = "Expected {0}, got {1} from JSON".Localized(expected, type);
			if (name != null)
				msg += " \"" + name + '"';
			return NewError(position, msg, false);
		}
	}
}
