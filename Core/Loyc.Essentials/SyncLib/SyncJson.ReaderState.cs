using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Math;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{

		internal class ReaderState : Parser
		{
			public ReaderState(ReadOnlyMemory<byte> memory, Options options) : base(memory, options) { }
			public ReaderState(IScanner<byte> scanner,      Options options) : base(scanner, options) { }

			// Only needed if data is read out-of-order
			private InternalList<JsonFrame> _frames = InternalList<JsonFrame>.Empty;

			public ReadOnlyMemory<byte> NextFieldKey => Frame.CurPropKey.Text;

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
			
			// Map from object IDs to objects
			private Dictionary<JsonValue, object>? _objects;

			byte[] _nameBuf = Empty<byte>.Array;

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
				ref JsonFrame f = ref Frame;

				v = (default(JsonValue), f.PositionOfBuf0 + f.ValueIndex);
				if (IsInsideList || name == null)
					return true;

				var originalName = name;
				if (_opt.NameConverter != null)
					name = _opt.NameConverter(name);

				// Is it the current property?
				if (AreEqual(f.CurPropKey.Text.Span, f.CurPropKey.Type, name))
					return true;

				return FindPropOutOfOrder(name, originalName, ref v);
			}
			bool FindPropOutOfOrder(string? name, string originalName, ref (JsonValue value, long position) v)
			{
				ref JsonFrame f = ref Frame;
				var skippedProps = _stack.LastRef.SkippedProps;

				// Is it something we skipped over earlier?
				Memory<byte> nameBytes;
				if (skippedProps != null) {
					nameBytes = ToNameBuf(name.AsSpan());
					if (skippedProps.TryGetValue(new JsonValue(JsonType.String, nameBytes), out v))
						return true;
				}

				var cur = CurPosition;

				// The current and skipped properties aren't what we need. Scan forward.
				if (cur.CurPropKey.Type != JsonType.Invalid) {
					while (true) {
						var valueIndex = cur.i;
						var skippedValue = ScanValue(ref cur);
						
						SaveSkippedValue(ref cur.CurPropKey, in skippedValue, valueIndex);

						if (!BeginProp(true, ref cur))
							break; // no comma
					}
				} else if (skippedProps == null)
					return false;

				// Name was not found in this object. Fallback: if there is a
				// NameConverter, try looking for the orginalName.
				nameBytes = ToNameBuf(originalName.AsSpan());
				return skippedProps?.TryGetValue(new JsonValue(JsonType.String, nameBytes), out v) ?? false;
			}

			private void SaveSkippedValue(ref JsonValue propName, in JsonValue skippedValue, int valueIndex)
			{
				// TODO: normalize the value! e.g. "\u0041" => "A", otherwise it'll be unfindable
				
				if (propName.Type == JsonType.SimpleString)
					propName.Type = JsonType.String;

				ref var tos = ref _stack.LastRef;
				tos.SkippedProps ??= new Dictionary<JsonValue, (JsonValue value, long position)>();
				tos.SkippedProps[propName] = (skippedValue, Frame.PositionOfBuf0 + valueIndex);
			}

			internal (bool Begun, object? Object) BeginSubObject(string? name, ObjectMode mode)
			{
				if (!FindProp(name, out (JsonValue value, long position) skippedObject))
					Error(Frame.Position.i, "Property \"{0}\" was missing".Localized(name), fatal: false);

				bool expectList = (mode & ObjectMode.List) != 0;

				// Begin the object/list, if there is an object/list here
				JsonPosition cur;
				var type = skippedObject.value.Type;
				if (type == JsonType.NotParsed) {
					cur = Frame.Position;
					Debug.Assert(Frame.ObjectStartIndex == int.MaxValue);
					Debug.Assert(!Frame.IsReplaying);
				} else {
					PushFrame(skippedObject, out cur);
					Debug.Assert(Frame.IsReplaying);
				}
				var objectPosition = Frame.PositionOfBuf0 + cur.i;
				type = TryToBeginObjectAndCommit(ref cur);

				if (type >= JsonType.FirstCompositeType)
				{
					if (type == JsonType.List) {
						if (expectList) {
							return (true, null); // Success! List opened.
						} else {
							// Unexpected list!
							// TODO: find a way to make this nonfatal.
							throw NewError((int)(objectPosition - Frame.PositionOfBuf0), "Expected object, got list");
						}
					}

					// Check if the object starts with a backreference ("$ref" or "\r")
					var propKey = cur.CurPropKey;
					var keySpan = propKey.Text.Span;
					if (AreEqual(keySpan, _ref) || AreEqual(keySpan, _r))
					{
						Frame.Checkpoint = cur.AsCheckpoint();
						var value = ScanValueAndBeginNext(ref cur);
						
						// A backreference must be a primitive and not followed by another prop
						if (value.Type is > JsonType.NotParsed and < JsonType.FirstCompositeType
							&& cur.CurPropKey.Text.Length == 0)
						{
							// TODO: deal with backreferences to objects that were previously skipped
							object? existing = null;
							if (_objects == null || !_objects.TryGetValue(value, out existing)) {
								Error(Frame.ValueIndex, "Backreferenced object not found");
							}

							EndObjectAndCommit(ref cur);
							
							return (false, existing);
						} else {
							// The prop cannot be a backreference. Back up to the beginning of the value
							Frame.RevertToCheckpoint();
						}
					}

					// Read object ID, if any ("$id" or "\f")
					if (AreEqual(keySpan, _f) || AreEqual(keySpan, _id))
					{
						Frame.Checkpoint = cur.AsCheckpoint();
						var id = ScanValueAndBeginNext(ref cur);

						if (id.Type == JsonType.Invalid)
						{
							throw SyntaxError(cur.i, name ?? AsciiToString(keySpan));
						}
						else if (id.Type < JsonType.FirstCompositeType)
						{
							// The ID seems acceptable (though we haven't checked if it's a duplicate)
							_stack.LastRef.Id = id;

							Commit(ref cur);

							// If caller expected a list, there should be a list prop next: "":[...]
							keySpan = cur.CurPropKey.Text.Span;
							if (expectList) {
								if (keySpan.Length == 2 || AreEqual(keySpan, _values)) {
									if (TryOpenListValuesAndCommit(ref cur))
										return (true, null);
								}
								// No list found. An error will be thrown below.
							}
						}
						else
						{
							// This cannot be an id. Back up to the beginning of the value.
							Frame.RevertToCheckpoint();
						}
					}

					if (expectList) {
						// Unexpected object!
						// TODO: find a way to make this nonfatal
						throw NewError((int)(objectPosition - Frame.PositionOfBuf0), "Expected list, got object");
					} else  {
						return (true, null); // success: object opened
					}
				}

				if (type == JsonType.NotParsed)
					type = DetectTypeOfUnparsedValue(ref cur);

				if (type == JsonType.Null)
				{
					if ((mode & ObjectMode.NotNull) != 0)
						Error(cur.i, "\"{0}\" is not nullable, but was null".Localized(name));
					return (false, null);
				}

				throw NewError(cur.i, "\"{0}\" was expected to be a {1}, but it was a {2}"
					.Localized(name ?? "list item", expectList ? "list" : "object", type.ToString()));
			}

			internal void EndSubObject()
			{
				var cur = CurPosition;
				EndObjectAndCommit(ref cur);
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
						var cur = Frame.Position;
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
						var cur = CurPosition;
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
						
						// ***********************************************************
						// TODO: THIS IS BROKEN. WE MUST DECODE ESCAPE SEQUENCES FIRST: \b => 8, \\ => \
						// ***********************************************************
						// Interpret as BAIS
						var output = ByteArrayInString.TryConvertToBytes(text.Span);
						if (output.HasValue) {
							if (output.Value.AsMemory() is List memory)
								return memory;

							return BuildListFromSpan<ListBuilder, List>(output.Value.AsMemory().Span, builder);
						}
						// TODO: make this nonfatal by saving skipped value
						throw SyntaxError((int)(v.position - Frame.PositionOfBuf0), name, "BAIS byte array");
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
				return default;
			}

			public char? ReadChar(string? name, bool nullable)
			{
				(JsonValue value, long position) v = ReadPrimitive(name);

				switch (v.value.Type) {
					case JsonType.SimpleString:
					case JsonType.String:
						return DecodeString(v.value).TryGet(0, '\0');

					case JsonType.PlainInteger:
						return checked((char) DecodeInteger(v.value.Text.Span));
						
					case JsonType.Number:
						return checked((char) DecodeNumber(v.value.Text.Span));

					case JsonType.Null:
						if (!nullable)
							throw UnexpectedNullError(v.position, name);
						return null;
						
					case JsonType.True:
						return 't';

					case JsonType.False:
						return 'f';

					case JsonType.Object:
					case JsonType.List:
						if (_optRead.ObjectToPrimitive == null)
							throw UnexpectedTypeError(v.position, name, "char", v.value.Type);
							
						var result = _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, nullable ? typeof(char?) : typeof(char));
						if (result == null) {
							if (nullable)
								return null;
							throw UnexpectedNullError(v.position, name, true);
						}
						return result.ToChar(null);
				}
				return default;
			}

			public BigInteger? ReadInteger(string? name, bool nullable)
			{
				(JsonValue value, long position) v = ReadPrimitive(name);

				switch (v.value.Type) {
					case JsonType.SimpleString:
					case JsonType.String:
						var str = DecodeString(v.value);
						if (BigInteger.TryParse(str, out var parsed))
							return parsed;
						return (BigInteger) double.Parse(str, NumberStyles.Float);

					case JsonType.PlainInteger:
						return DecodeInteger(v.value.Text.Span);
						
					case JsonType.Number:
						return (BigInteger) DecodeNumber(v.value.Text.Span);

					case JsonType.Null:
						if (!nullable)
							throw UnexpectedNullError(v.position, name);
						return null;
						
					case JsonType.True:
						return 1;

					case JsonType.False:
						return 0;

					case JsonType.Object:
					case JsonType.List:
						if (_optRead.ObjectToPrimitive == null)
							throw UnexpectedTypeError(v.position, name, "integer", v.value.Type);

						var result = _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, nullable ? typeof(double?) : typeof(double));
						if (result == null) {
							if (nullable)
								return null;
							throw UnexpectedNullError(v.position, name, true);
						}
						return (BigInteger) result.ToDouble(null);
				}
				return default;
			}

			public double? ReadDouble(string? name, bool nullable)
			{
				(JsonValue value, long position) v = ReadPrimitive(name);

				switch (v.value.Type) {
					case JsonType.SimpleString:
					case JsonType.String:
						var str = DecodeString(v.value);
						return double.Parse(str, NumberStyles.Float);

					case JsonType.PlainInteger:
						return (double) DecodeInteger(v.value.Text.Span);
						
					case JsonType.Number:
						return (double) DecodeNumber(v.value.Text.Span);

					case JsonType.Null:
						if (!nullable)
							throw UnexpectedNullError(v.position, name);
						return null;
						
					case JsonType.True:
						return 1;

					case JsonType.False:
						return 0;

					case JsonType.Object:
					case JsonType.List:
						if (_optRead.ObjectToPrimitive == null)
							throw UnexpectedTypeError(v.position, name, "double", v.value.Type);
							
						var result = _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, nullable ? typeof(double?) : typeof(double));
						if (result == null) {
							if (nullable)
								return null;
							throw UnexpectedNullError(v.position, name, true);
						}
						return result.ToDouble(null);
				}
				return default;
			}

			public decimal? ReadDecimal(string? name, bool nullable)
			{
				(JsonValue value, long position) v = ReadPrimitive(name);

				switch (v.value.Type) {
					case JsonType.SimpleString:
					case JsonType.String:
						var str = DecodeString(v.value);
						return decimal.Parse(str, NumberStyles.Float);

					case JsonType.PlainInteger:
						return (decimal) DecodeInteger(v.value.Text.Span);
						
					case JsonType.Number:
						return (decimal) DecodeDecimal(v.value.Text.Span);

					case JsonType.Null:
						if (!nullable)
							throw UnexpectedNullError(v.position, name);
						return null;
						
					case JsonType.True:
						return 1;

					case JsonType.False:
						return 0;

					case JsonType.Object:
					case JsonType.List:
						if (_optRead.ObjectToPrimitive == null)
							throw UnexpectedTypeError(v.position, name, "decimal", v.value.Type);
							
						var result = _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, nullable ? typeof(decimal?) : typeof(decimal));
						if (result == null) {
							if (nullable)
								return null;
							throw UnexpectedNullError(v.position, name, true);
						}
						return result.ToDecimal(null);
				}
				return default;
			}

			public bool? ReadBoolean(string? name, bool nullable)
			{
				(JsonValue value, long position) v = ReadPrimitive(name);

				switch (v.value.Type) {
					case JsonType.SimpleString:
					case JsonType.String:
						var str = DecodeString(v.value);
						if (bool.TryParse(str, out bool parsed))
							return parsed;
						return double.Parse(str) != 0;

					case JsonType.PlainInteger:
						return DecodeInteger(v.value.Text.Span) != 0;

					case JsonType.Number:
						return DecodeNumber(v.value.Text.Span) != 0;

					case JsonType.Null:
						if (!nullable)
							Error((int)(v.position - Frame.PositionOfBuf0), "\"{0}\" is not nullable, but was null".Localized(name));
						return null;

					case JsonType.True:
						return true;

					case JsonType.False:
						return false;

					case JsonType.Object:
					case JsonType.List:
						if (_optRead.ObjectToPrimitive == null)
							throw UnexpectedTypeError(v.position, name, "boolean", v.value.Type);

						var result = _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, typeof(bool))?.ToBoolean(null);
						if (result == null) {
							if (nullable)
								return null;
							throw UnexpectedNullError(v.position, name, true);
						}
						return result.Value;
				}
				return default;
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
					return (new JsonValue(JsonType.Missing, default), Frame.PositionOfBuf0 + Frame.ValueIndex);
				throw NotFoundError(name);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private bool TryReadPrimitive(string? name, out (JsonValue value, long position) v)
			{
				if (FindProp(name, out v)) {
					if (v.value.Type == JsonType.NotParsed) {
						var cur = Frame.Position;
						v.value = ScanValue(ref cur);
						
						if (v.value.Type == JsonType.Invalid)
							throw SyntaxError(Frame.ValueIndex, name);

						BeginNext(ref cur);
						Commit(ref cur);
					}
					return true;
				}
				return false;
			}

			#endregion



			private void PushFrame(in (JsonValue value, long position) v, out JsonPosition cur)
			{
				Debug.Assert(v.value.Type >= JsonType.FirstCompositeType);
				Debug.Assert((v.value.Text.Span[0] == '{') == (v.value.Type == JsonType.Object));
				_frames.Add(Frame);
				Frame = new JsonFrame {
					Buf = v.value.Text,
					IsReplaying = true,
					PositionOfBuf0 = v.position,
					ValueIndex = 0,
					ObjectStartIndex = int.MaxValue,
				};
				cur = Frame.Position;
			}




			static bool AreEqual(ReadOnlySpan<byte> curProp, byte[] name)
			{
				if (name.Length != curProp.Length)
					return false;
				for (int i = 0; i < name.Length; i++)
					if (name[i] != curProp[i])
						return false;
				return true;
			}
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
							Error(Frame.PropKeyIndex, "String expected");
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
				return NewError((int)(position - Frame.PositionOfBuf0), msg, false);
			}
			
			[MethodImpl(MethodImplOptions.NoInlining)]
			public Exception NotFoundError(string? name)
				=> NewError(Frame.ValueIndex, "Property not found: {0}".Localized(name), false);
			
			[MethodImpl(MethodImplOptions.NoInlining)]
			private Exception UnexpectedTypeError(long position, string? name, string expected, JsonType type)
			{
				// TODO: localize all errors exactly once
				var msg = "Expected {0}, got {1} in JSON".Localized(expected, type);
				if (name != null)
					msg += " \"" + name + '"';
				return NewError((int)(position - Frame.PositionOfBuf0), msg, false);
			}
		}
	}
}
