	using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using static System.Math;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		internal class ReaderState
		{
			enum JsonType {
				Invalid = -1,
				NotParsed = 0,
				SimpleString = 1, // An ASCII string with no escapes
				String = 2,       // A string with escapes or multibyte characters
				PlainInteger = 3, // A number with no '.' nor 'e'
				Number = 4,
				Null = 5,

				True = 6,
				False = 7,

				FirstCompositeType = 10,
				Object = 10,
				List = 11,
			}

			// Raw bytes of a JSON value
			struct JsonValue : IEquatable<JsonValue>
			{
				public JsonType Type;
				public ReadOnlyMemory<byte> Text;

				public JsonValue(JsonType type, ReadOnlyMemory<byte> text)
				{
					Type = type;
					Text = text;
				}
				public override int GetHashCode() => Text.GetHashCode();
				public override bool Equals(object? obj) => obj is JsonValue v && Equals(v);
				public bool Equals(JsonValue v) => Type == v.Type && Text.Equals(v.Text);
			}

			ref struct CurrentByte {
				public ReadOnlySpan<byte> span;
				public int i;
				public byte Byte => span[i];
				public byte this[int offs] => span[i + offs];
			}

			/// <summary>Scanning of multiple JSON objects can be in progress 
			/// simultaneously; a JsonFrame is created for each nesting level
			/// being scanned.</summary>
			/// <remarks>
			/// For example, suppose we have some JSON like this:
			/// <code>
			/// {
			///     "items": [9, 8, 7, 6, {"\f":1,"name":"Joe"}]
			///     "favorite": {"\r":1}
			///     "size": 5,
			///     "total": 30
			/// }
			/// </code>
			/// If the caller reads "size" first, the "items" and "favorite" props are
			/// skipped, so SkippedProps and SkippedDefs will have 2 and 1 items in 
			/// them respectively. If the user reads "favorite" next by calling 
			/// BeginSubObject, it is read from the skipped prop {"\r":1}, causing a 
			/// new and empty JsonFrame to be created on the frame stack for the 
			/// subobject. Since the subobject is a backreference, the new frame is 
			/// converted into a reader for the object {"\f":1,"name":"Joe"} that was 
			/// encountered earlier. After the user is done reading this subobject, 
			/// the JsonFrame that was created for it can be discarded, returning to 
			/// the outer frame.
			/// <para/>
			/// For performance, the SkippedProps and SkippedDefs dictionaries
			/// are not created unless properties/definitions are actually skipped.
			/// </remarks>
			struct JsonFrame
			{
				// The buffer being read from (replayed buffer, or something from _mainScanner)
				public ReadOnlyMemory<byte> Buf;
				// True iff this frame is reading a memory region that was skipped earlier
				public bool IsReplaying;
				// Location within the JSON file of Buf.Span[0] (used for error reporting)
				public long PositionOfBuf0;
				// True iff the current object is a list
				public bool IsInsideList;
				// Location where current token starts in Buf
				public int TokenIndex;
				public CurrentByte TokenStart => new CurrentByte { span = Buf.Span, i = TokenIndex };
				// Location where current object being scanned starts in Buf (or int.MaxValue)
				public int ObjectStartIndex; // = int.MaxValue;
				// Location where current property key starts in Buf
				public int CurPropIndex;
				// value of "$id" or "\f" prop at start of the current object
				public JsonValue CurId;
				// Name of current property in the current object, with the quotes stripped off
				public JsonValue CurPropKey;
				// List of properties that were skipped earlier (immediate children)
				// Note: the keys here never use JsonType.SimpleString
				public Dictionary<JsonValue, (JsonValue value, long position)>? SkippedProps;
				// List of object definitions that were skipped earlier (any nesting level)
				public Dictionary<JsonValue, (JsonValue value, long position)>? SkippedDefs;
			}

			public ReaderState(IScanner<byte> scanner, Options options)
			{
				_mainScanner = scanner;
				_opt = options;
				_optRead = options.Read;
				TOS.IsInsideList = true;
				TOS.ObjectStartIndex = int.MaxValue;
			}

			private IScanner<byte> _mainScanner;
			private Memory<byte> _mainScannerBuf;
			internal Options _opt;
			internal Options.ForReader _optRead;
			
			// Top of stack (not stored in _stack)
			private JsonFrame TOS;
			private InternalList<JsonFrame> _stack = InternalList<JsonFrame>.Empty;
			
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
				Debug.Assert(i == len);
				return _nameBuf.AsMemory().Slice(0, len);
			}

			bool FindProp(string? name, out (JsonValue value, long position) v)
			{
				ref JsonFrame tos = ref TOS;

				v = (default(JsonValue), tos.PositionOfBuf0 + tos.TokenIndex);
				if (tos.IsInsideList || name == null)
					return true;

				var originalName = name;
				if (_opt.NameConverter != null)
					name = _opt.NameConverter(name);

				// Is it the current property?
				if (AreEqual(tos.CurPropKey.Text.Span, tos.CurPropKey.Type, name))
					return true;

				// Is it something we skipped over earlier?
				Memory<byte> nameBytes;
				if (tos.SkippedProps != null) {
					nameBytes = ToNameBuf(name.AsSpan());
					if (tos.SkippedProps.TryGetValue(new JsonValue(JsonType.String, nameBytes), out v))
						return true;
				}

				// The current and skipped properties aren't what we need. Scan forward.
				if (tos.CurPropKey.Type != JsonType.Invalid) {
					while (true) {
						int tokenIndex = TOS.TokenIndex;
						var skippedValue = ScanValue();
						
						SaveSkippedValue(ref tos.CurPropKey, skippedValue, tokenIndex);

						if (!BeginProp(true))
							break; // no comma
					}
				} else if (tos.SkippedProps == null)
					return false;

				// Name was not found in this object. Fallback: if there is a
				// NameConverter, try looking for the orginalName.
				nameBytes = ToNameBuf(originalName.AsSpan());
				return tos.SkippedProps!.TryGetValue(new JsonValue(JsonType.String, nameBytes), out v);
			}

			private void SaveSkippedValue(ref JsonValue propName, in JsonValue skippedValue, int tokenIndex)
			{
				// TODO: normalize the value! e.g. "\u0041" => "A", otherwise it'll be unfindable
				if (propName.Type == JsonType.SimpleString)
					propName.Type = JsonType.String;
				TOS.SkippedProps ??= new Dictionary<JsonValue, (JsonValue value, long position)>();
				TOS.SkippedProps[propName] = (skippedValue, TOS.PositionOfBuf0 + tokenIndex);
			}

			internal (bool Begun, object? Object) BeginSubObject(string? name, SubObjectMode mode)
			{
				if (!FindProp(name, out (JsonValue value, long position) skippedObject))
					throw new FormatException("Property \"{0}\" was missing".Localized(name));

				var type = skippedObject.value.Type;
				if (type == JsonType.NotParsed)
					type = TryOpenObject();
				else
					BeginSkippedObject(skippedObject);

				bool expectList = (mode & SubObjectMode.List) != 0;

				if (type >= JsonType.FirstCompositeType) {
					// Check if it's an object containing a backreference
					var keySpan = TOS.CurPropKey.Text.Span;
					if (TOS.IsInsideList) {
						if (expectList)
							return (true, null); // success: list opened
					} else {
						int valueIndex = TOS.TokenIndex;
						if (_objects != null && (AreEqual(keySpan, _ref) || AreEqual(keySpan, _r))) {
							// Read backreference
							var value = ScanValue();
							bool hasNext = BeginProp(true);
							if (!hasNext && value.Type < JsonType.FirstCompositeType) {
								EndSubObject();
								return (false, _objects![value]);
							} else {
								// This cannot be a backreference.
								SaveSkippedValue(ref TOS.CurPropKey, value, valueIndex);
							}
						}

						// Read object ID, if any
						if (AreEqual(keySpan, _f) || AreEqual(keySpan, _id)) {
							TOS.CurId = ScanValue();
							if (TOS.CurId.Type >= JsonType.FirstCompositeType) {
								// This cannot be an id.
								SaveSkippedValue(ref TOS.CurPropKey, TOS.CurId, valueIndex);
								TOS.CurId = default;
							}
							BeginProp(true);

							// If caller wants a list, there should be a list prop "": [...]
							keySpan = TOS.CurPropKey.Text.Span;
							if (expectList && (keySpan.Length == 0 || AreEqual(keySpan, _values))) {
								if (TryOpenListValues()) {
									return (true, null);
								}
							}
						}

						if (!expectList)
							return (true, null); // success: object opened
					}

					// Data type was wrong. "Back out" of the object/list so that our state 
					// remains valid and the user can retry with another type if desired.
					if (type == JsonType.NotParsed)
						UndoOpenObject();
					else
						EndSubObject();
				}

				throw new FormatException("\"{0}\" was expected to be a {1}, but it was a {2}."
					.Localized(name ?? "list item", expectList ? "list" : "object", DetectTokenType()));
			}

			internal SyncType HasField(string name)
			{
				if (FindProp(name, out var v)) {
					var type = v.value.Type;
					if (type == JsonType.NotParsed)
						type = DetectTypeOfUnparsedValue();

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
				return SyncType.Missing;
			}

			// Used by ISyncManager.HasField() to detect the type of the current (unparsed) value
			private JsonType DetectTypeOfUnparsedValue()
			{
				var cur = TOS.TokenStart;
				if (AutoRead(ref cur)) {
					switch ((char) cur.Byte) {
						case '"':
							return JsonType.String;
						case '.':
							return JsonType.Number;
						case '0': case '1': case '2': case '3':
						case '4': case '5': case '6': case '7':
						case '8': case '9':
							// Find out if it's an integer
							for (cur.i++; AutoRead(ref cur); cur.i++) {
								if (cur.Byte >= '0' && cur.Byte <= '9')
									continue;
								else if (cur.Byte == '.' || cur.Byte == 'e')
									return JsonType.Number;
							}
							return JsonType.PlainInteger;
						case 't':
							return JsonType.True;
						case 'f':
							return JsonType.False;
						case 'n':
							return JsonType.Null;
						case '[':
							return JsonType.List;
						case '{':
							return JsonType.Object;
					}
				}
				return JsonType.Invalid;
			}

			internal string ReadString(string? name)
			{
				throw new NotImplementedException();
			}

			private bool TryOpenListValues()
			{
				throw new NotImplementedException();
			}

			private void UndoOpenObject()
			{
				throw new NotImplementedException();
			}

			private void EndSubObject()
			{
				throw new NotImplementedException();
			}

			private void BeginSkippedObject(in (JsonValue value, long position) v)
			{
				Debug.Assert(v.value.Type >= JsonType.FirstCompositeType);
				Debug.Assert((v.value.Text.Span[0] == '{') == (v.value.Type == JsonType.Object));
				_stack.Add(TOS);
				TOS = new JsonFrame {
					Buf = v.value.Text,
					IsReplaying = true,
					PositionOfBuf0 = v.position,
					IsInsideList = v.value.Type == JsonType.List,
					TokenIndex = 1,
					ObjectStartIndex = int.MaxValue,
				};
				if (v.value.Type == JsonType.List)
					SkipWhitespace();
				else
					BeginProp(false);
			}

			private bool BeginProp(bool expectComma)
			{
				Debug.Assert(!TOS.IsInsideList);
				if (expectComma) {
					if (!SkipWhitespaceAnd(','))
						return false;
				} else {
					SkipWhitespace();
				}

				var key = ScanValue();
				TOS.CurPropIndex = TOS.TokenIndex - key.Text.Length;

				if (key.Type == JsonType.Invalid) {
					// Detect end-of-object
					var cur = TOS.TokenStart;
					if ((uint)cur.i < (uint)cur.span.Length && (cur.Byte == '}' || cur.Byte == ']')) {
						if (expectComma && _optRead.Strict)
							Error(cur.i, "Comma is not allowed before '{0}'".Localized(cur.Byte));
						return false;
					} else
						Error(cur.i, "Expected a property name");
				} else if (key.Type <= JsonType.String) {
					Debug.Assert(key.Text.Length >= 2);
					key.Text = key.Text.Slice(1, key.Text.Length - 2);
				} else {
					if (_optRead.Strict)
						Error(TOS.TokenIndex - key.Text.Length, "Expected a string");
				}
				TOS.CurPropKey = key;
				return true;
			}

			private bool AreEqual(ReadOnlySpan<byte> curProp, byte[] name)
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
					// Comparing a JSON-escaped UTF-8 string to a UTF-16 string: it's tricky.
					// But one thing we can be sure of is that `name` shouldn't be longer.
					if (name.Length > curProp.Length)
						return false;

					for (int i = 0, cp_i = 0; i < name.Length; i++) {
						byte b = curProp[cp_i];
						if (name[i] != b) {
							int name_c;
							if (b == '\\') {
								int cp_c = DecodeEscape(curProp, ref cp_i, TOS.CurPropIndex);
								name_c = new UString(name).TryDecodeAt(i);
								if (name_c != cp_c)
									return false;
							} else if (b < 0x80 || name[i] < 0x80) {
								return false;
							} else {
								int cp_c = G.DecodeUTF8Char(curProp, ref cp_i);
								name_c = new UString(name).TryDecodeAt(i);
								if (cp_c != name_c)
									return false;
							}
							if (name_c >= 0x10000) // detect surrogate pair & skip it properly
								i++;
						} else if (curProp[cp_i] == '\\') {
							cp_i++;
							if (cp_i < curProp.Length && curProp[cp_i] != '\\')
								return false;
						} else {
							cp_i++;
						}
					}
					return false;
				} else {
					if (type != JsonType.SimpleString) {
						if (_optRead.Strict)
							Error(TOS.CurPropIndex, "String expected");
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

			private int DecodeEscape(ReadOnlySpan<byte> curProp, ref int cp_i, int stringStartIndex)
			{
				Debug.Assert(curProp[cp_i] == '\\');
				if ((uint)++cp_i >= (uint)curProp.Length)
					return -1; // invalid escape sequence
				switch (curProp[cp_i]) {
					case (byte) '/':  ++cp_i; return '/';
					case (byte) '\\': ++cp_i; return '\\';
					case (byte) 'b':  ++cp_i; return '\b';
					case (byte) 'f':  ++cp_i; return '\f';
					case (byte) 'n':  ++cp_i; return '\n';
					case (byte) 'r':  ++cp_i; return '\r';
					case (byte) 't':  ++cp_i; return '\t';
					case (byte) '0':
						if (_optRead.Strict)
							Error(stringStartIndex + cp_i, "JSON does not support the '\\0' escape sequence");
						++cp_i;
						return '\0';
					case (byte) 'u':
						int a = 0, b = 0, c = 0, d = 0;
						if (cp_i + 4 >= curProp.Length
							|| (a = G.HexDigitValue((char) curProp[cp_i + 1])) < 0
							|| (b = G.HexDigitValue((char) curProp[cp_i + 2])) < 0
							|| (c = G.HexDigitValue((char) curProp[cp_i + 3])) < 0
							|| (d = G.HexDigitValue((char) curProp[cp_i + 4])) < 0)
							Error(stringStartIndex + cp_i, "'\\u' escape sequence was too short");
						cp_i += 5;
						int ch = (a << 12) | (b << 8) | (c << 4) | d;
						return ch;
				}
				if (_optRead.Strict)
					Error(stringStartIndex + cp_i, "Invalid escape sequence '\\{0}'".Localized(curProp[cp_i]));
				return '\\';
			}

			private JsonType TryOpenObject()
			{
				SkipWhitespace();
				//if (_buf[0] == '{') {

				//}
				return JsonType.NotParsed;
			}

			private void SkipWhitespace()
			{
				var cur = TOS.TokenStart;
				SkipWhitespace(ref cur);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void SkipWhitespace(ref CurrentByte cur)
			{
				for (; AutoRead(ref cur); cur.i++) {
					byte c = cur.Byte;
					if (c == ' ' || c == '\t' || c == '\r')
						continue;
					if (c == '/')
						SkipComment(ref cur);
					else
						break;
				}
			}

			private void SkipComment(ref CurrentByte cur)
			{
				byte c = cur.Byte;
				if (c == '/' && AutoRead(ref cur, 1)) {
					if ((c = cur[1]) == '/') {
						if (!_optRead.AllowComments)
							Error(cur.i, "JSON does not support comments");
							
						// Skip single-line comment
						for (cur.i += 2; ; cur.i++) {
							if (!AutoRead(ref cur, 1))
								return;
							if ((c = cur.Byte) == '\r' || c == '\n')
								break;
						}
					} else if (c == '*') {
						if (!_optRead.AllowComments)
							Error(cur.i, "JSON does not support comments");

						// Skip multi-line comment
						int commentSize = 2;
						for (cur.i += 2; ; cur.i++, commentSize++) {
							if (!AutoRead(ref cur, 1))
								Error(cur.i - commentSize, "JSON syntax error: multiline comment was not closed");
							if (cur.Byte == '*' && cur[1] == '/') {
								cur.i += 2;
								break;
							}
						}
					}
				}
			}

			private void Error(int i, string msg)
			{
				long index = TOS.PositionOfBuf0 + i;
				var exc = new FormatException(msg + " " + "(at byte index {0})".Localized(index));
				exc.Data["index"] = index;
				throw exc;
			}

			// The scanner could choose a much larger size, but this is the minimum we'll tolerate
			const int DefaultMinimumScanSize = 32;

			// Ensures that the _i < _buf.Length by reading more if necessary
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool AutoRead(ref CurrentByte cur, int extraLookahead = 0)
			{
				Debug.Assert(cur.span == TOS.Buf.Span);
				if ((uint)(cur.i + extraLookahead) < (uint)cur.span.Length)
					return true;
				cur.i = Read(cur.i, extraLookahead + 1);
				cur.span = TOS.Buf.Span;
				return (uint)(cur.i + extraLookahead) < (uint)cur.span.Length;
			}
			// Reads new data into TOS.Buf if possible
			private int Read(int index, int lookaheadNeeded)
			{
				if (TOS.IsReplaying)
					return index;

				int requestSize = Max(lookaheadNeeded, DefaultMinimumScanSize);
				int skip = Min(TOS.ObjectStartIndex, Min(TOS.TokenIndex, index));
				
				TOS.Buf = _mainScanner.Read(skip, (index -= skip) + requestSize, ref _mainScannerBuf);

				TOS.TokenIndex -= skip;
				if (TOS.ObjectStartIndex != int.MaxValue)
					TOS.ObjectStartIndex -= skip;
				
				return index;
			}

			private JsonValue ScanValue()
			{
				var cur = TOS.TokenStart;
				return ScanValue(ref cur);
			}
			private JsonValue ScanValue(ref CurrentByte cur)
			{
				if (AutoRead(ref cur)) {
					var b = cur.Byte;
					if (b == '"')
					{
						var type = JsonType.SimpleString;
						for (cur.i++; AutoRead(ref cur); cur.i++) {
							if (cur.Byte == '\\') {
								type = JsonType.String;
								cur.i++; // we don't yet care which escape it is, but be sure to skip \"
							} else if (cur.Byte == '"') {
								var value = new JsonValue(type, TOS.Buf.Slice(TOS.TokenIndex, ++cur.i - TOS.TokenIndex));
								TOS.TokenIndex = cur.i;
								return value;
							} else if (cur.Byte >= 0x80) { // non-ASCII character
								type = JsonType.String;
							}
						}
					}
					else if (b == 'n' && AutoRead(ref cur, 4) && cur[1] == 'u' && cur[2] == 'l' && cur[3] == 'l')
					{
						TOS.TokenIndex = cur.i + 4;
						return new JsonValue(JsonType.Null, TOS.Buf.Slice(cur.i, 4));
					}
					else if (b == 'f' && AutoRead(ref cur, 5) && cur[1] == 'a' && cur[2] == 'l' && cur[3] == 's' && cur[2] == 'e')
					{
						TOS.TokenIndex = cur.i + 5;
						return new JsonValue(JsonType.Null, TOS.Buf.Slice(cur.i, 5));
					}
					else if (b == 't' && AutoRead(ref cur, 4) && cur[1] == 'r' && cur[2] == 'u' && cur[3] == 'e')
					{
						TOS.TokenIndex = cur.i + 4;
						return new JsonValue(JsonType.Null, TOS.Buf.Slice(cur.i, 4));
					}
					else if (b == '{' || b == '[')
					{
						bool isList = b == '[';
						char closer = isList ? ']' : '}';

						// TOS.ObjectStartIndex acts as a "don't skip" signal, to ensure Read()
						// doesn't skip past it, guaranteeing that the beginning of the object
						// will remain in the buffer. considering that this method can be on the
						// stack multiple times, we can safely decrease it, but not increase it.
						// Also, the old value must be restored once we're done scanning.
						var oldObjectStart = TOS.ObjectStartIndex;
						TOS.ObjectStartIndex = Min(TOS.ObjectStartIndex, cur.i);
						int objectOffset = cur.i - TOS.ObjectStartIndex;

						SkipWhitespace(ref cur);
						if (!SkipIf(ref cur, closer)) {
							for (;;) {
								if (ScanValue(ref cur).Type == JsonType.Invalid)
									Error(cur.i, "Expected a value");
								if (!isList) {
									SkipWhitespace(ref cur);
									if (!SkipIf(ref cur, ':'))
										Error(cur.i, "Expected ':'");
									if (ScanValue(ref cur).Type == JsonType.Invalid)
										Error(cur.i, "Expected a value");
								}
								SkipWhitespace(ref cur);
								if (!SkipIf(ref cur, ',')) {
									if (SkipIf(ref cur, closer))
										break;
									Error(cur.i, "Expected ','");
								}
							}
						}

						var type = isList ? JsonType.List : JsonType.Object;
						int objectStart = TOS.ObjectStartIndex + objectOffset;
						TOS.ObjectStartIndex = oldObjectStart;
						TOS.TokenIndex = cur.i;
						return new JsonValue(type, TOS.Buf.Slice(objectStart, cur.i - objectStart));
					}
					else // expect a number
					{
						if (b == '-')
							cur.i++;
						if (AutoRead(ref cur)) {
							var type = JsonType.PlainInteger;
							// Read initial digits
							b = cur.Byte;
							if (b == '0' && _optRead.Strict) {
								cur.i++;
							} else if (b >= '0' && b <= '9') {
								do
									cur.i++;
								while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9');
							} else if (b != '.' || _optRead.Strict) {
								Error(cur.i, "Expected '0.' instead of '.'");
							}

							// Read decimal places, if any
							if (AutoRead(ref cur) && cur.Byte == '.') {
								type = JsonType.Number;
								cur.i++;
								int old_i = cur.i;
								while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9')
									cur.i++;
								if (old_i == cur.i && _optRead.Strict)
									Error(cur.i, "Expected digits after '.'");
							}

							// Read exponent, if any
							if (AutoRead(ref cur) && (cur.Byte == 'e' || cur.Byte == 'E')) {
								type = JsonType.Number;
								cur.i++;
								if (AutoRead(ref cur) && (cur.Byte == '+' || cur.Byte == '-'))
									cur.i++;
								int iOld = cur.i;
								while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9')
									cur.i++;
								if (iOld == cur.i)
									Error(cur.i, "Expected exponent digits");
							}

							var value = new JsonValue(type, TOS.Buf.Slice(TOS.TokenIndex, cur.i - TOS.TokenIndex));
							TOS.TokenIndex = cur.i;
							return value;
						}
					}
				}
				return new JsonValue(JsonType.Invalid, default); // unreachable
			}

			private bool SkipWhitespaceAnd(char expecting)
			{
				var cur = TOS.TokenStart;
				SkipWhitespace(ref cur);
				return SkipIf(ref cur, expecting);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool SkipIf(ref CurrentByte cur, char expecting)
			{
				if (AutoRead(ref cur) && cur.Byte == expecting) {
					cur.i++;
					return true;
				}
				return false;
			}

			private object? DetectTokenType()
			{
				throw new NotImplementedException();
			}
		}
	}
}
