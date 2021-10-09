using static System.Math;
using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Loyc.Collections.Impl;
using System.Diagnostics.CodeAnalysis;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		// Operations it should do:
		// - Low-level begin object (skip '{' and read first key, or skip '[')
		// - Low-level end object (skip '}' or ']')
		// - Scan value AND begin next prop?
		// - get cur
		/// <summary>
		/// Low-level UTF8 JSON scanner. This class understands the details of JSON 
		/// syntax, but is only designed to read JSON linearly (it doesn't track 
		/// skipped values).
		/// </summary>
		internal class Parser
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

				Missing = 8,

				FirstCompositeType = 10,
				Object = 11,
				List = 12,
				// Used by the derived class (ReaderState) to represent an array enclosed in
				// a wrapper object with an ID code, e.g. { $id": "7", "$values": [ ... ] }.
				ListWithId = 13,
			}

			// Raw bytes of a JSON value
			protected struct JsonValue : IEquatable<JsonValue>
			{
				public JsonType Type;
				public ReadOnlyMemory<byte> Text;

				public JsonValue(JsonType type, ReadOnlyMemory<byte> text)
				{
					Type = type;
					Text = text;
				}
				public override int GetHashCode() => Text.Span.SequenceHashCode();
				public override bool Equals(object? obj) => obj is JsonValue v && Equals(v);
				public bool Equals(JsonValue v) => Type == v.Type && Text.Span.SequenceEqual(v.Text.Span);

				// For debugging only
				public override string ToString() => Encoding.UTF8.GetString(Text.ToArray());
			}

			/// <summary>
			/// Represents a pointer to the current position in the JSON data stream, plus
			/// the index where the current item started and the property key at that index.
			/// </summary>
			protected ref struct JsonPosition
			{
				public ReadOnlySpan<byte> span;
				public int i;
				public byte Byte => span[i];
				public byte this[int offs] => span[i + offs];
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public int ByteOr(int fallback) => (uint)i < (uint)span.Length ? span[i] : fallback;
				
				// Name of current property in the current object (type JsonType.Invalid at end of object)
				// (TODO: what if reading a list?)
				// (TODO: make more efficient by not storing Memory<byte> of the key)
				public JsonValue CurPropKey;
				// Location where current property key starts in Buf
				public int PropKeyIndex;
				public JsonCheckpoint AsCheckpoint()
				{
					return new JsonCheckpoint {
						KeyIndex = PropKeyIndex,
						KeyLength = CurPropKey.Text.Length,
						KeyType = CurPropKey.Type,
						ValueIndex = i,
					};
				}
				public override string ToString() // for debugging only
				{
					return Encoding.UTF8.GetString(span.Slice(i).ToArray());
				}
			}

			protected struct JsonCheckpoint
			{
				public int KeyIndex; // 0 in case of list
				public int KeyLength; // 0 in case of list
				public JsonType KeyType;
				public int ValueIndex;
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
			protected struct JsonFrame
			{
				// The buffer being read from (replayed buffer, or something from _mainScanner)
				public ReadOnlyMemory<byte> Buf;
				// True iff this frame is reading a memory region rather than _mainScanner
				public bool IsReplaying;
				// Location within the JSON file of Buf.Span[0] (used for error reporting)
				public long PositionOfBuf0;
				// Location where the next value starts in Buf
				public int ValueIndex;
				public JsonPosition Position => new JsonPosition {
					span = Buf.Span,
					i = ValueIndex,
					PropKeyIndex = PropKeyIndex,
					CurPropKey = CurPropKey,
				};

				public JsonCheckpoint? Checkpoint; // TODO: keep this valid during Read()
				public void DiscardCheckpoint() => Checkpoint = null;
				public void RevertToCheckpoint()
				{
					PropKeyIndex = Checkpoint!.Value.KeyIndex;
					var keyText = Buf.Slice(PropKeyIndex, Checkpoint.Value.KeyLength);
					CurPropKey = new JsonValue(Checkpoint.Value.KeyType, keyText);
					ValueIndex = Checkpoint.Value.ValueIndex;
					DiscardCheckpoint();
				}

				// updated during Commit: Position.Byte, or ']' at EOF. ']' is used as the
				// EOF indicator so ReachedEndOfList is detected efficiently in that case.
				public int CurrentByte;
				// Location where a current or ancestor object starts in Buf, if this
				// location is being tracked (or int.MaxValue if not)
				public int ObjectStartIndex; // = int.MaxValue;
				// Location where current property key starts in Buf (TODO: what if reading a list?)
				public int PropKeyIndex;
				// Name of current property in the current object (type JsonType.Invalid at end of object)
				public JsonValue CurPropKey;

				public string ValueDebugString => Encoding.UTF8.GetString(Buf.Slice(ValueIndex).ToArray());
				
				// This seems wrong because JsonFrame doesn't correspond to a single nesting level
					// List of properties that were skipped earlier (immediate children)
					// Note: the keys here never use JsonType.SimpleString
					//public Dictionary<JsonValue, (JsonValue value, long position)>? SkippedProps;
				// List of object definitions that were skipped earlier (any nesting level)
				public Dictionary<JsonValue, (JsonValue value, long position)>? SkippedDefs;
			}

			public Parser(IScanner<byte> scanner, Options options)
			{
				_mainScanner = scanner;

				Init(options);
			}
			public Parser(ReadOnlyMemory<byte> memory, Options options)
			{
				_mainScanner = null;
				Frame.Buf = memory;
				Frame.IsReplaying = true;

				Init(options);
			}
			[MemberNotNull(nameof(_opt), nameof(_optRead))]
			private void Init(Options options)
			{
				Frame.ObjectStartIndex = int.MaxValue;

				_opt = options;
				_optRead = options.Read;

				// Skip initial whitespace
				var cur = CurPosition;
				SkipWhitespace(ref cur);
				Commit(ref cur);
			}

			protected JsonFrame Frame;

			protected IScanner<byte>? _mainScanner;
			protected Memory<byte> _mainScannerBuf; // not used by ReaderState; it's passed to _mainScanner.Read()

			internal Options _opt;
			internal Options.ForReader _optRead;

			// An syntax error that left the reader in an invalid state
			protected Exception? _fatalError;

			protected struct StackEntry
			{
				public JsonValue Id;
				public JsonType Type;
				public Dictionary<JsonValue, (JsonValue value, long position)>? SkippedProps;

				public StackEntry(JsonType objectType)
				{
					Type = objectType;
					Id = default;
					SkippedProps = null;
				}
			}

			// A stack of values of the "$id" or "\f" prop from the start of each object.
			// The size of this stack indicates the current object nesting level (though
			// a list of the form {"\f":id, "":[ ... ]} consumes only one stack entry)
			protected InternalList<StackEntry> _stack = new InternalList<StackEntry>(4);
			
			public int Depth => _stack.Count;

			// Caches the value of _stack.Last.type != JsonType.Object
			public bool IsInsideList { get; protected set; } = true;

			public bool ReachedEndOfList => Frame.CurrentByte == ']';

			/// <summary>Returns a struct that points to the beginning of the current value 
			///   and if !IsInsideList, knows the associated key. If there are no more 
			///   values in the current object, thie property points to the closing ']'
			///   or '}', or to the end of the file. If parsing stopped at a syntax error,
			///   this may point to the error or to the last value before the error.</summary>
			protected JsonPosition CurPosition => Frame.Position;

			protected void Commit(ref JsonPosition cur)
			{
				Frame.ValueIndex = cur.i;
				Frame.PropKeyIndex = cur.PropKeyIndex;
				Frame.CurPropKey = cur.CurPropKey;
				Frame.CurrentByte = cur.ByteOr(']');
				
				// TODO: Commit() is getting too expensive, can we make it faster?
				Frame.DiscardCheckpoint();
			}

			/// <summary>
			/// Checks if cur points to an object or list value and if so, "begins" the 
			/// object by (1) skipping the opening '[' or '{', (2) pushing the object type 
			/// onto _stack, (3) advancing cur to the beginning of the first value in the 
			/// object, (4) updating ReachedEndOfList, and (5) committing cur because 
			/// there's no mechanism to undo all the prior operations.
			/// </summary>
			/// <returns>JsonType.List or JsonType.Object if a subobject was begun;
			/// otherwise, returns JsonType.NotParsed.</returns>
			protected JsonType TryToBeginObjectAndCommit(ref JsonPosition cur)
			{
				bool isList;
				if (AutoRead(ref cur) && ((isList = cur.Byte == '[') || cur.Byte == '{')) {
					cur.i++;
					SkipWhitespace(ref cur);
					if (isList) {
						Push(JsonType.List);
						Commit(ref cur);
						return JsonType.List;
					} else {
						Push(JsonType.Object);
						BeginProp(false, ref cur);
						Commit(ref cur);
						return JsonType.Object;
					}
				} else {
					return JsonType.NotParsed;
				}
			}

			protected bool TryOpenListValuesAndCommit(ref JsonPosition cur)
			{
				if (AutoRead(ref cur) && cur.Byte == '[') {
					cur.i++;
					SkipWhitespace(ref cur);
					_stack.LastRef.Type = JsonType.ListWithId;
					Commit(ref cur);
					return true;
				}
				return false;
			}

			/// <summary>
			/// (1) Scans to the end of the current object or list if neccessary,
			/// (2) skips the closing ']' or '}', throwing an syntax error if it's the wrong closer,
			/// (3) pops the top item from the stack,
			/// (4) skips the comma, if any, in the outer object/list, and if that works,
			/// (5) reads the next property key, if the outer object isn't a list, and
			/// (6) Commit()s
			/// </summary>
			protected void EndObjectAndCommit(ref JsonPosition cur)
			{
				var type = _stack.Last.Type;

				EndObjectCore(ref cur);

				// TODO: the derived class should handle this case
				if (type == JsonType.ListWithId) {
					EndObjectCore(ref cur);
				}

				Pop();

				// TODO: this use of VerifyEof looks suspicious to me, is the code correct?
				if (_stack.Count != 0 || !_optRead.VerifyEof) {
					BeginNext(ref cur);
				} else {
					SkipWhitespace(ref cur);
					if (AutoRead(ref cur)) {
						Error(cur.i, "Expected EOF", false);
					}
				}
				Commit(ref cur);
			}

			private void EndObjectCore(ref JsonPosition cur)
			{
				bool isList;
				if (!((isList = cur.Byte == ']') || cur.Byte == '}')) {
					// Skip any remaining properties of the object
					bool hasMoreData;
					do {
						// TODO: if ScanValue ecounters a subobject with an id, we must save it for later
						// TODO: document limitation: any single skipped value is limited to 2GB
						var ignored = ScanValue(ref cur);
						hasMoreData = BeginNext(ref cur);
					} while (hasMoreData);

					if (!((isList = cur.Byte == ']') || cur.Byte == '}'))
						goto missingCloser;
				}

				// check that the closer was expected
				if (IsInsideList != isList)
					goto missingCloser;
				cur.i++;
				return;

			missingCloser:
				Error(cur.i, IsInsideList ? "Expected ']'" : "Expected '}'", fatal: true);
			}

			// Used by ISyncManager.HasField() to detect the type of the current (unparsed) value
			protected JsonType DetectTypeOfUnparsedValue(ref JsonPosition cur)
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
							var lookahead = cur;
							for (lookahead.i++; AutoRead(ref lookahead); lookahead.i++) {
								if (cur.Byte < '0' || cur.Byte > '9') {
									if (cur.Byte == '.' || cur.Byte == 'e')
										return JsonType.Number;
									break;
								}
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

			#region BeginProp and BeginNext

			protected bool BeginProp(bool expectComma, ref JsonPosition cur)
			{
				// This must not be called inside a list (but at the beginning of an
				// object, IsInsideList hasn't been updated)
				Debug.Assert(!IsInsideList || !expectComma);
				
				SkipWhitespace(ref cur);
				if (expectComma) {
					if (!SkipIf(',', ref cur)) {
						cur.PropKeyIndex = cur.i;
						cur.CurPropKey = default;
						return false;
					}
					SkipWhitespace(ref cur);
				}

				cur.PropKeyIndex = cur.i;
				var key = cur.CurPropKey = ScanValue(ref cur);

				if (key.Type == JsonType.Invalid)
				{
					// There's no property here. It's either the end of the object, or a syntax error
					if ((uint)cur.i < (uint)cur.span.Length && (cur.Byte == '}' || cur.Byte == ']')) {
						if (expectComma && _optRead.Strict)
							Error(cur.i, "Comma is not allowed before '{0}'".Localized((char) cur.Byte));
						else if (cur.Byte == ']')
							Error(cur.i, "Expected '}'");

						cur.PropKeyIndex = cur.i;
						cur.CurPropKey = default;
						return false;
					} else
						Error(cur.i, "Expected a property name");
				}
				else if (key.Type <= JsonType.String)
				{
					// Normal property detected
					Debug.Assert(key.Text.Length >= 2);
					key.Text = key.Text.Slice(1, key.Text.Length - 2);
				}
				else
				{
					if (_optRead.Strict)
						Error(cur.PropKeyIndex, "Expected a string");
				}

				// Skip the ':'
				if (!SkipWhitespaceAnd(':', ref cur))
					Error(cur.i, "Expected ':'");
				
				// Skip whitespace again to reach the beginning of the value
				SkipWhitespace(ref cur);

				return true;
			}

			// A method called to move to the next prop or list item after reading a prop/item.
			// Its job is to skip the comma and either detect end-of-list or read the next key.
			protected bool BeginNext(ref JsonPosition cur)
			{
				if (IsInsideList) {
					cur.CurPropKey = default;

					bool reachedEnd;
					if (SkipWhitespaceAnd(',', ref cur)) {
						SkipWhitespace(ref cur);
						cur.PropKeyIndex = cur.i;

						reachedEnd = cur.ByteOr(']') == ']';
						if (reachedEnd && _optRead.Strict)
							Error(cur.i, "Comma is not allowed before '{0}'".Localized((char)cur.Byte));
					} else {
						cur.PropKeyIndex = cur.i;

						reachedEnd = cur.ByteOr(']') == ']';
						if (!reachedEnd)
							Error(cur.i, "Expected ']'", fatal: true);
					}
					return !reachedEnd;
				} else {
					return BeginProp(expectComma: true, ref cur);
				}
			}

			protected JsonValue ScanValueAndBeginNext(ref JsonPosition cur)
			{
				var value = ScanValue(ref cur);
				BeginNext(ref cur);
				return value;
			}

			#endregion

			#region SkipWhitespace, SkipComment

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SkipWhitespace(ref JsonPosition cur)
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

			protected void SkipComment(ref JsonPosition cur)
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

			#endregion

			#region Error management

			[MethodImpl(MethodImplOptions.NoInlining)]
			protected Exception SyntaxError(int index, string? name, string context = "JSON value")
			{
				// TODO: localize all errors exactly once
				var msg = "Syntax error in {0}".Localized(context.Localized());
				if (name != null)
					msg += " \"" + name + '"';
				return NewError(index, msg);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			protected Exception NewError(int i, string msg, bool fatal = true)
			{
				if (_fatalError != null)
					return _fatalError; // New error is just a symptom of the old error; rethrow

				long position = Frame.PositionOfBuf0 + i;
				var exc = new FormatException(msg + " " + "(at byte {0})".Localized(position));
				exc.Data["position"] = position;
				
				if (fatal)
					_fatalError = exc;
				return exc;
			}

			protected void Error(int i, string msg, bool fatal = true) => throw NewError(i, msg, fatal);

			#endregion

			#region AutoRead

			// The scanner could choose a much larger size, but this is the minimum we'll tolerate
			const int DefaultMinimumScanSize = 32;

			// Ensures that the _i < _buf.Length by reading more if necessary
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool AutoRead(ref JsonPosition cur, int extraLookahead = 0)
			{
				Debug.Assert(cur.span == Frame.Buf.Span);
				if ((uint)(cur.i + extraLookahead) < (uint)cur.span.Length)
					return true;
				cur.i = Read(cur.i, extraLookahead + 1);
				cur.span = Frame.Buf.Span;
				return (uint)(cur.i + extraLookahead) < (uint)cur.span.Length;
			}
			// Reads new data into Frame.Buf if possible
			private int Read(int index, int lookaheadNeeded)
			{
				if (Frame.IsReplaying)
					return index;

				int requestSize = Max(lookaheadNeeded, DefaultMinimumScanSize);
				int skip = Min(Frame.ObjectStartIndex, Min(Frame.ValueIndex, index));
				
				Frame.Buf = _mainScanner.Read(skip, (index -= skip) + requestSize, ref _mainScannerBuf);

				Frame.ValueIndex -= skip;
				Frame.PropKeyIndex -= skip;
				if (Frame.ObjectStartIndex != int.MaxValue)
					Frame.ObjectStartIndex -= skip;
				
				return index;
			}

			#endregion

			#region ScanValue()

			protected JsonValue ScanValue()
			{
				var cur = Frame.Position;
				return ScanValue(ref cur);
			}
			protected JsonValue ScanValue(ref JsonPosition cur)
			{
				Frame.ValueIndex = cur.i;
				if (AutoRead(ref cur)) {
					var b = cur.Byte;
					if (b == '"')
					{
						var type = JsonType.SimpleString;
						Debug.Assert(cur.i == Frame.ValueIndex);
						for (cur.i++; AutoRead(ref cur); cur.i++) {
							if (cur.Byte == '\\') {
								type = JsonType.String;
								cur.i++; // we don't yet care which escape it is, but be sure to skip \"
							} else if (cur.Byte == '"') {
								return new JsonValue(type, Frame.Buf.Slice(Frame.ValueIndex, ++cur.i - Frame.ValueIndex));
							} else if (cur.Byte >= 0x80) { // non-ASCII character
								type = JsonType.String;
							}
						}
					}
					else if (b == 'n' && AutoRead(ref cur, 4) && cur[1] == 'u' && cur[2] == 'l' && cur[3] == 'l')
					{
						int i = cur.i;
						cur.i += 4;
						return new JsonValue(JsonType.Null, Frame.Buf.Slice(i, 4));
					}
					else if (b == 'f' && AutoRead(ref cur, 5) && cur[1] == 'a' && cur[2] == 'l' && cur[3] == 's' && cur[4] == 'e')
					{
						int i = cur.i;
						cur.i += 5;
						return new JsonValue(JsonType.False, Frame.Buf.Slice(i, 5));
					}
					else if (b == 't' && AutoRead(ref cur, 4) && cur[1] == 'r' && cur[2] == 'u' && cur[3] == 'e')
					{
						int i = cur.i;
						cur.i += 4;
						return new JsonValue(JsonType.True, Frame.Buf.Slice(i, 4));
					}
					else if (b == '{' || b == '[')
					{
						char closer = b == '[' ? ']' : '}';

						// TOS.ObjectStartIndex acts as a "don't skip" signal, to ensure Read()
						// doesn't skip past it, guaranteeing that the beginning of the object
						// will remain in the buffer. considering that this method can be on the
						// stack multiple times, we can safely decrease it, but not increase it.
						// Also, the old value must be restored once we're done scanning.
						var oldObjectStart = Frame.ObjectStartIndex;
						Frame.ObjectStartIndex = Min(Frame.ObjectStartIndex, cur.i);
						long objectPosition = cur.i + Frame.PositionOfBuf0;
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
						int objectStart = (int)(objectPosition - Frame.PositionOfBuf0);
						Frame.ObjectStartIndex = oldObjectStart;
						//TOS.TokenIndex = cur.i;
						return new JsonValue(type, Frame.Buf.Slice(objectStart, cur.i - objectStart));
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

						return new JsonValue(type, Frame.Buf.Slice(Frame.ValueIndex, cur.i - Frame.ValueIndex));
					}
				}
				return new JsonValue(JsonType.Invalid, default); // EOF
			}

			#endregion

			private bool SkipWhitespaceAnd(char expecting, ref JsonPosition cur)
			{
				SkipWhitespace(ref cur);
				return SkipIf(expecting, ref cur);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool SkipIf(char expecting, ref JsonPosition cur)
			{
				if (AutoRead(ref cur) && cur.Byte == expecting) {
					cur.i++;
					return true;
				}
				return false;
			}

			void Push(JsonType objectType)
			{
				_stack.Add(new StackEntry(objectType));
				IsInsideList = objectType != JsonType.Object;
			}
			void Pop()
			{
				_stack.Pop();
				IsInsideList = _stack.Count == 0 ? true : _stack.Last.Type != JsonType.Object;
			}

			#region Decoders of primitive values

			protected static BigInteger DecodeInteger(ReadOnlySpan<byte> text)
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
			protected static double DecodeNumber(ReadOnlySpan<byte> text)
			{
				return double.Parse(AsciiToString(text), NumberStyles.Float);
			}
			protected static decimal DecodeDecimal(ReadOnlySpan<byte> text)
			{
				return decimal.Parse(AsciiToString(text), NumberStyles.Float);
			}

			protected static string AsciiToString(ReadOnlySpan<byte> text)
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

			StringBuilder? _sb;

			protected string DecodeString(in JsonValue value)
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

			protected int DecodeStringChar(ReadOnlySpan<byte> curProp, ref int cp_i)
			{
				byte b = curProp[cp_i];
				if (b < 0x80) {
					if (b == '\\')
						return DecodeEscape(curProp, ref cp_i, Frame.PropKeyIndex);
					else {
						cp_i++;
						return b;
					}
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
