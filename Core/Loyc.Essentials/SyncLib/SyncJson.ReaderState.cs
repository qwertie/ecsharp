	using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Math;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		internal class ReaderState
		{
			internal enum JsonType {
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
				Object = 11,
				List = 12,
				ListWithId = 13, // represents a case of {"$id":"7", [ ... ]}
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
				// Location where the token most recently read by ScanValue() started in Buf
				public int TokenIndex;
				public CurrentByte TokenStart => new CurrentByte { span = Buf.Span, i = TokenIndex };
				// Location where a current or ancestor object starts in Buf, if this
				// location is being tracked (or int.MaxValue if not)
				public int ObjectStartIndex; // = int.MaxValue;
				// Location where current property key starts in Buf
				public int CurPropIndex;
				// Name of current property in the current object
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
			private Memory<byte> _mainScannerBuf; // not used by ReaderState; it's passed to _mainScanner.Read()
			internal Options _opt;
			internal Options.ForReader _optRead;
			
			// A syntax error that left the reader in an invalid state
			private Exception _fatalError;
			
			// Top of stack (not stored in _stack)
			private JsonFrame TOS;
			// The stack is only needed if data is read out-of-order?
			private InternalList<JsonFrame> _stack = InternalList<JsonFrame>.Empty;
			// A stack of values of the "$id" or "\f" prop from the start of each object.
			// The size of this stack indicates the current object nesting level (though
			// a list of the form {"\f":id, "":[ ... ]} consumes only one stack entry)
			private InternalList<(JsonValue id, JsonType type)> _miniStack = new InternalList<(JsonValue id, JsonType mode)>(4);
			
			// Map from object IDs to objects
			private Dictionary<JsonValue, object>? _objects;

			StringBuilder _sb;

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
					Error(TOS.TokenStart.i, "Property \"{0}\" was missing".Localized(name), fatal: false);

				bool expectList = (mode & SubObjectMode.List) != 0;
				CurrentByte cur;

				// Begin parsing the object, if possible
				var type = skippedObject.value.Type;
				if (type == JsonType.NotParsed) {
					Debug.Assert(TOS.ObjectStartIndex == int.MaxValue);
					type = TryToBeginObject(out cur);
					Debug.Assert(!TOS.IsReplaying);
				} else {
					BeginSkippedObject(skippedObject, out cur);
					Debug.Assert(TOS.IsReplaying);
				}

				if (type >= JsonType.FirstCompositeType) {
					// Check if it's an object containing a backreference
					var propKey = TOS.CurPropKey;
					var keySpan = propKey.Text.Span;
					if (TOS.IsInsideList) {
						if (expectList) {
							_miniStack.Add((default(JsonValue), JsonType.List));
							return (true, null); // Success! List opened.
						}
					} else {
						// Check for backreference ("$ref" or "\r")
						if (_objects != null && (AreEqual(keySpan, _ref) || AreEqual(keySpan, _r))) {
							var value = ScanValue(ref cur);
							// A backreference must be a primitive and not followed by another prop
							if (value.Type is > JsonType.NotParsed and < JsonType.FirstCompositeType
								&& !SkipWhitespaceAnd(',', ref cur))
							{
								object? existing = null;
								if (_objects == null || !_objects.TryGetValue(value, out existing)) {
									Error(TOS.TokenIndex, "Backreferenced object not found");
								}

								EndSubObjectCore(ref cur);
								TOS.TokenIndex = cur.i;

								return (false, existing);
							} else {
								// The prop cannot be a backreference. Back up to the beginning of the value
								cur.i = TOS.TokenIndex;
							}
						}

						_miniStack.Add((default(JsonValue), JsonType.Object));

						// Read object ID, if any ("$id" or "\f")
						if (AreEqual(keySpan, _f) || AreEqual(keySpan, _id)) {
							var id = ScanValue(ref cur);

							if (id.Type == JsonType.Invalid) {
								Error(TOS.TokenIndex, "Syntax error; expected a value");
							} else if (id.Type < JsonType.FirstCompositeType) {
								// The ID seems acceptable (though we haven't checked if it's a duplicate)
								_miniStack.Last = (id, JsonType.Object);
								
								BeginProp(true, ref cur);

								// If caller expected a list, there should be a list prop next: "":[...]
								keySpan = TOS.CurPropKey.Text.Span;
								if (expectList) {
									if (keySpan.Length == 0 || AreEqual(keySpan, _values)) {
										if (TryOpenListValues(ref cur)) {
											_miniStack.Last = (id, JsonType.ListWithId);
											return (true, null);
										}
									}
								}
							} else {
								// This cannot be an id. Back up to the beginning of the value.
								cur.i = TOS.TokenIndex;
							}
						}

						if (!expectList)
							return (true, null); // success: object opened
					}

					// Data type was wrong. TODO: "Back out" of the object/list so that
					// our state remains valid and the user can retry with another type
					// if desired. For now, it's hard, don't bother.
				} else {
					Debug.Assert(!TOS.IsReplaying);
				}

				Error(cur.i, "\"{0}\" was expected to be a {1}, but it was a {2}"
					.Localized(name ?? "list item", expectList ? "list" : "object", type.ToString()));
				return (false, null); // unreachable
			}

			private JsonType TryToBeginObject(out CurrentByte cur)
			{
				Debug.Assert(!TOS.IsReplaying);

				cur = TOS.TokenStart;

				SkipWhitespace(ref cur);
				Debug.Assert(TOS.ObjectStartIndex == int.MaxValue);
				//TOS.ObjectStartIndex = TOS.TokenIndex;
				
				bool isList;
				if (AutoRead(ref cur) && ((isList = cur.Byte == '[') || cur.Byte == '{')) {
					cur.i++;
					SkipWhitespace(ref cur);
					TOS.IsInsideList = isList;
					if (isList) {
						return JsonType.List;
					} else {
						BeginProp(false, ref cur);
						return JsonType.Object;
					}
				} else {
					return DetectTypeOfUnparsedValue(ref cur);
				}
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
						var cur = TOS.TokenStart;
						type = DetectTypeOfUnparsedValue(ref cur);
					}
					return type;
				}
				return JsonType.Invalid;
			}

			// Used by ISyncManager.HasField() to detect the type of the current (unparsed) value
			private JsonType DetectTypeOfUnparsedValue(ref CurrentByte cur)
			{
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

			public string? ReadString(string? name)
			{
				if (FindProp(name, out var v)) {
					if (v.value.Type == JsonType.NotParsed) {
						var cur = TOS.TokenStart;
						SkipWhitespace(ref cur);
						v.value = ScanValue(ref cur);

						BeginProp(true, ref cur);
					}
					
					var span = v.value.Text.Span;

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
								Error((int)(v.position - TOS.PositionOfBuf0), "Expected string, got " + v.value.Type, false);
							
							return _optRead.ObjectToPrimitive!(name, v.value.Text, v.position, typeof(string))?.ToString();
					}
					Debug.Fail("unreachable");
				}
				Error(TOS.TokenIndex, "Property not found: {0}".Localized(name), false);
				return null;
			}


			private bool TryOpenListValues(ref CurrentByte cur)
			{
				if (AutoRead(ref cur) && cur.Byte == '[') {
					cur.i++;
					BeginProp(false, ref cur);
					return true;
				}
				return false;
			}

			internal void EndSubObject()
			{
				var type = _miniStack.Last.type;
				var cur = TOS.TokenStart;

				EndSubObjectCore(ref cur);

				if (type == JsonType.ListWithId) {
					EndSubObjectCore(ref cur);
				}

				_miniStack.Pop();
			}

			private void EndSubObjectCore(ref CurrentByte cur)
			{
				// TODO: write test with mismatched {] and [}
				if (TOS.IsReplaying) {
					// On replay, we already know the object ends with '}' or ']'
					#if DEBUG
					SkipWhitespace(ref cur);
					Debug.Assert(cur.Byte == '}' || cur.Byte == ']');
					#endif
					TOS = _stack.Last;
				} else {
					SkipWhitespace(ref cur);
					bool isList = cur.Byte == ']';
					if (isList || cur.Byte == '}') {
						cur.i++;
						// TODO: check that the closer was expected
					} else {
						Error(cur.i, "Expected closer"); // TODO: check whether this can ever happen
					}
				}
			}

			private void BeginSkippedObject(in (JsonValue value, long position) v, out CurrentByte cur)
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
				cur = TOS.TokenStart;
				if (v.value.Type == JsonType.List)
					SkipWhitespace(ref cur);
				else
					BeginProp(false, ref cur);
			}


			private bool BeginProp(bool expectComma)
			{
				var cur = TOS.TokenStart;
				return BeginProp(expectComma, ref cur);
			}
			private bool BeginProp(bool expectComma, ref CurrentByte cur)
			{
				Debug.Assert(!TOS.IsInsideList);
				SkipWhitespace(ref cur);
				if (expectComma) {
					if (!SkipIf(',', ref cur)) {
						TOS.TokenIndex = cur.i;
						return false;
					}
				}

				TOS.CurPropIndex = cur.i;

				var key = TOS.CurPropKey = ScanValue(ref cur);
				Debug.Assert(TOS.TokenIndex == TOS.CurPropIndex);

				if (key.Type == JsonType.Invalid) {
					// There's no property here. Is it the end of an object?
					if ((uint)cur.i < (uint)cur.span.Length && (cur.Byte == '}' || cur.Byte == ']')) {
						if (expectComma && _optRead.Strict)
							Error(cur.i, "Comma is not allowed before '{0}'".Localized(cur.Byte));
						
						return false;
					} else
						Error(cur.i, "Expected a property name");
				} else if (key.Type <= JsonType.String) {
					// Normal property detected
					Debug.Assert(key.Text.Length >= 2);
					key.Text = key.Text.Slice(1, key.Text.Length - 2);
				} else {
					if (_optRead.Strict)
						Error(TOS.TokenIndex - key.Text.Length, "Expected a string");
				}

				// Skip the ':'
				if (!SkipWhitespaceAnd(':', ref cur))
					Error(cur.i, "Expected ':'");
				
				TOS.TokenIndex = cur.i;
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
					if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
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

			private void Error(int i, string msg, bool fatal = true)
			{
				if (_fatalError != null)
					throw _fatalError; // New error is just a symptom of the old error; rethrow

				long position = TOS.PositionOfBuf0 + i;
				var exc = new FormatException(msg + " " + "(at byte {0})".Localized(position));
				exc.Data["position"] = position;
				
				if (fatal)
					_fatalError = exc;
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
				TOS.CurPropIndex -= skip;
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
				TOS.TokenIndex = cur.i;
				if (AutoRead(ref cur)) {
					var b = cur.Byte;
					if (b == '"')
					{
						var type = JsonType.SimpleString;
						Debug.Assert(cur.i == TOS.TokenIndex);
						for (cur.i++; AutoRead(ref cur); cur.i++) {
							if (cur.Byte == '\\') {
								type = JsonType.String;
								cur.i++; // we don't yet care which escape it is, but be sure to skip \"
							} else if (cur.Byte == '"') {
								return new JsonValue(type, TOS.Buf.Slice(TOS.TokenIndex, ++cur.i - TOS.TokenIndex));
							} else if (cur.Byte >= 0x80) { // non-ASCII character
								type = JsonType.String;
							}
						}
					}
					else if (b == 'n' && AutoRead(ref cur, 4) && cur[1] == 'u' && cur[2] == 'l' && cur[3] == 'l')
					{
						int i = cur.i;
						cur.i += 4;
						return new JsonValue(JsonType.Null, TOS.Buf.Slice(i, 4));
					}
					else if (b == 'f' && AutoRead(ref cur, 5) && cur[1] == 'a' && cur[2] == 'l' && cur[3] == 's' && cur[2] == 'e')
					{
						int i = cur.i;
						cur.i += 5;
						return new JsonValue(JsonType.Null, TOS.Buf.Slice(i, 5));
					}
					else if (b == 't' && AutoRead(ref cur, 4) && cur[1] == 'r' && cur[2] == 'u' && cur[3] == 'e')
					{
						int i = cur.i;
						cur.i += 5;
						return new JsonValue(JsonType.Null, TOS.Buf.Slice(i, 4));
					}
					else if (b == '{' || b == '[')
					{
						char closer = b == '[' ? ']' : '}';

						// TOS.ObjectStartIndex acts as a "don't skip" signal, to ensure Read()
						// doesn't skip past it, guaranteeing that the beginning of the object
						// will remain in the buffer. considering that this method can be on the
						// stack multiple times, we can safely decrease it, but not increase it.
						// Also, the old value must be restored once we're done scanning.
						var oldObjectStart = TOS.ObjectStartIndex;
						TOS.ObjectStartIndex = Min(TOS.ObjectStartIndex, cur.i);
						long objectPosition = cur.i + TOS.PositionOfBuf0;
						cur.i++;

						if (!SkipWhitespaceAnd(closer, ref cur)) {
							for (;;) {
								if (ScanValue(ref cur).Type == JsonType.Invalid)
									Error(cur.i, "Expected a value");
								if (closer == '}') {
									if (!SkipWhitespaceAnd(':', ref cur))
										Error(cur.i, "Expected ':'");
									if (ScanValue(ref cur).Type == JsonType.Invalid)
										Error(cur.i, "Expected a value");
								}
								if (!SkipWhitespaceAnd(',', ref cur)) {
									if (SkipIf(closer, ref cur))
										break;
									Error(cur.i, "Expected ','");
								}
							}
						}

						var type = closer == ']' ? JsonType.List : JsonType.Object;
						int objectStart = (int)(objectPosition - TOS.PositionOfBuf0);
						TOS.ObjectStartIndex = oldObjectStart;
						//TOS.TokenIndex = cur.i;
						return new JsonValue(type, TOS.Buf.Slice(objectStart, cur.i - objectStart));
					}
					else // expect a number, or end-of-object
					{
						if (b == '-') {
							cur.i++;
							if (!AutoRead(ref cur))
								return new JsonValue(JsonType.Invalid, default); // EOF
							b = cur.Byte;
						}

						var type = JsonType.PlainInteger;
						if (b >= '0' && b <= '9') {
							// Read initial digits
							if (_optRead.Strict && b == '0')
								cur.i++;
							else {
								do
									cur.i++;
								while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9');
							}
						} else if (b != '.' || _optRead.Strict) {
							if (b == '.')
								Error(cur.i, "Expected '0.' instead of '.'");
							else
								return new JsonValue(JsonType.Invalid, default);
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

						return new JsonValue(type, TOS.Buf.Slice(TOS.TokenIndex, cur.i - TOS.TokenIndex));
					}
				}
				return new JsonValue(JsonType.Invalid, default); // EOF
			}

			private bool SkipWhitespaceAnd(char expecting, ref CurrentByte cur)
			{
				SkipWhitespace(ref cur);
				return SkipIf(expecting, ref cur);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool SkipIf(char expecting, ref CurrentByte cur)
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

			#region Decoders of primitive values

			static BigInteger DecodeInteger(ReadOnlySpan<byte> text)
			{
				// We can assume that the syntax has already been verified by ScanValue()
				if (text.Length == 1)
					return text[0] - '0';
				else if (text.Length < 19) {
					// This integer fits in a long.
					if (text[0] == '-')
						return -DecodeInteger(text.Slice(1));

					// Because it's bytes, we must parse manually if we want speed
					int i = text.Length - 1;
					long multiplier = 10, num = text[i] - '0';
					for (i--; i >= 0; i--) {
						Debug.Assert((char)text[i] is >= '0' and <= '9');
						num += (text[i] - '0') * multiplier;
						multiplier *= 10;
					}
					return num;
				} else {
					string text2 = AsciiToString(text);
					return BigInteger.Parse(text2);
				}
			}
			static double DecodeNumber(ReadOnlySpan<byte> text)
			{
				return double.Parse(AsciiToString(text));
			}
			static decimal DecodeDecimal(ReadOnlySpan<byte> text)
			{
				return decimal.Parse(AsciiToString(text));
			}

			static string AsciiToString(ReadOnlySpan<byte> text)
			{
				// I can't find a fast way to widen to bytes to a string.
				// Encoding.* objects aren't terrible, but
				// (1) they waste time looking for non-ASCII characters to bitch about
				// (2) they are optimized for large strings (but text is usually small)
				// Maybe I should just write an ordinary loop?
				#if NET50 // is this the right name for .NET 5?
				return Encoding.Latin1.GetString(text);
				#elif NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
				return Encoding.ASCII.GetString(text.ToArray());
				#else
				return Encoding.ASCII.GetString(text);
				#endif
			}

			static List? RebuildString<List, ListBuilder>(string s, ListBuilder builder)
				where ListBuilder : IListBuilder<List, char>
			{
				builder.Alloc(s.Length);
				for (int i = 0; i < s.Length; i++)
					builder.Add(s[i]);
				return builder.List;
			}

			string DecodeString(in JsonValue value)
				=> DecodeUnquotedString(value.Text.Span.Slice(1, value.Text.Length - 2), value.Type);

			string DecodeUnquotedString(ReadOnlySpan<byte> span, JsonType type)
			{
				var sb = (_sb ??= new StringBuilder());
				Debug.Assert(type is JsonType.SimpleString or JsonType.String);
				if (type is JsonType.SimpleString) {
					sb.Length = span.Length;
					for (int i = 0; i < span.Length; i++)
						sb[i] = (char) span[i];
				} else {
					sb.Length = 0;
					for (int i = 0; i < span.Length;) {
						int c = DecodeStringChar(span, ref i);
						if ((uint)c <= 0xFFFF)
							sb.Append((char) c);
						else {
							c -= 0x10000;
							sb.Append((char)((c >> 10) | 0xD800));
							sb.Append((char)((c & 0x3FF) | 0xDC00));
						}
					}
				}
				return sb.ToString();
			}

			#endregion

			#region Decoding single characters / escape sequences

			int DecodeStringChar(ReadOnlySpan<byte> curProp, ref int cp_i)
			{
				byte b = curProp[cp_i];
				if (b < 0x80) {
					if (b == '\\')
						return DecodeEscape(curProp, ref cp_i, TOS.CurPropIndex);
					else
						return b;
				} else {
					return G.DecodeUTF8Char(curProp, ref cp_i);
				}
			}

			int DecodeEscape(ReadOnlySpan<byte> curProp, ref int cp_i, int stringStartIndex)
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

			#endregion
		}
	}
}
