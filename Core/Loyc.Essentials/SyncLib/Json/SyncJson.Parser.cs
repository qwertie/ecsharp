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
		/// <summary>
		///   Low-level UTF8 JSON scanner. This class understands the details of JSON 
		///   syntax, but is only designed to read JSON linearly (it doesn't track 
		///   skipped values).
		/// </summary><remarks>
		///   Let's go through an example to understand how the parser is designed to
		///   work:
		///     <code>
		///           { "array": [123, 456], "bool": true }
		///     </code>
		///   The initial state will point to the '{' (the JSON file itself is treated
		///   as if it were the contents of an array, i.e. IsInsideList == true, so
		///   '{' is the beginning of the first value of that aray. Of course, normally
		///   a JSON file will simply end rather than have a comma and a second value.)
		/// <para/>
		///   The derived class, <see cref="ReaderState"/>, will get the initial 
		///   position from the CurPosition property (cur = CurPosition) and then call 
		///   TryToBeginObject(ref cur, _) to enter the object, at which point cur will 
		///   point to the '['. This brings us to three notables thing to keep in mind 
		///   about the Parser: 
		/// <ol>
		///   <li>First, it "prefers" to stop scanning at the beginning of each value, 
		///     which is why TryToBeginObject scans forward to '[' rather than stopping 
		///     at the first '"' (i.e. the "array" key). The `cur` variable contains 
		///     enough information to get the bytes of the key so that the key can be 
		///     extracted to a string if necessary. </li>
		///   <li>Second, the current position is designed to be stored in a stack 
		///     variable (called `cur` by convention) so that the buffer can have type 
		///     <see cref="Span{byte}"/> (which cannot be stored on the heap). 
		///     Therefore, while `cur` is the "current" position, that position is not 
		///     saved on the heap until `Commit(ref cur)` is called. Thus, backtracking 
		///     to the last value is as simple as not committing `cur`, UNLESS there 
		///     has been a change to the object stack (if TryToBeginObject was called
		///     and it succeeded, the stack changed, but it can be undone by calling
		///     <see cref="UndoBeginObject"/>).</li>
		///   <li>Third, there is list called _stack that has a stack entry for each 
		///     JSON object or list that is entered. There is a special case for 
		///     deduplicated lists, which (in the Newtonsoft mode) have the form 
		///     `{ "$id": NNN, "$values": [list items] }`. In this case there is only
		///     a single entry on the _stack for both the outer object and the inner 
		///     list. The derived class calls <see cref="TryOpenListValuesAndCommit"/>
		///     when it detects the special list-of-values prop.</li>
		/// </ul>
		///   If the end user is expecting an array called "array", the derived class 
		///   will then call `TryToBeginObject(ref cur, true)` to enter it, after which
		///   cur will point to '1'. Primitive list items are read using 
		///   <see cref="ScanValue"/> followed by <see cref="BeginNext"/>, which skips 
		///   the comma and whitespace (when not reading a list, it also skips over the
		///   next key). As before, <see cref="Commit"/> must be called to save the new
		///   byte position.
		/// <para/>
		///   If the end user is expecting some prop other than "array", the derived
		///   class will skip over the value of "array" by calling ScanValue() and 
		///   BeginNext() (or BeginProp()) and then it will save the 
		///   <see cref="JsonValue"/> associated with "array" (which includes the
		///   memory block of `[123, 456]`) in a dictionary in _stack.Last for later 
		///   retrieval.
		/// <para/>
		///   If, later on, the user wants to read "array", the derived class can begin
		///   scanning it by calling <see cref="BeginReplay"/> and return to the 
		///   original location in the stream (with the corresponding original state) by 
		///   calling <see cref="EndReplay"/>. Note: these methods don't affect the 
		///   `_stack` of objects; there is a separate stack for replays.
		/// </remarks>
		internal abstract class Parser
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
			protected ref struct JsonPointer
			{
				public ReadOnlySpan<byte> Buf;
				public int Index;

				public byte Byte => Buf[Index];
				public byte this[int offs] => Buf[Index + offs];
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public int ByteOr(int fallback) => (uint)Index < (uint)Buf.Length ? Buf[Index] : fallback;
				
				// Name of current property in the current object (type JsonType.Invalid at
				// the end of an object)
				// (TODO: what if reading a list?)
				// (TODO: make more efficient by not storing Memory<byte> of the key)
				public JsonValue CurPropKey;
				// Location where current property key starts in Buf
				public int PropKeyIndex;

				public override string ToString() // for debugging only
				{
					return Encoding.UTF8.GetString(Buf.Slice(Index).ToArray());
				}
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
			private struct JsonFrame
			{
				// The buffer being read from (replayed buffer, or something from _mainScanner)
				public ReadOnlyMemory<byte> Buf;
				// True iff this frame is reading a memory region rather than _mainScanner
				public bool IsReplaying;
				// Location within the JSON file of Buf.Span[0] (used for error reporting)
				public long PositionOfBuf0;
				// Number of newlines encountered
				public int LineNumber;
				// TODO: Index of the most recent newline relative to Buf[0] (can be negative)
				public int LineStart;
				// Location where the next value starts in Buf
				public int ValueIndex;
				public JsonPointer Pointer => new JsonPointer {
					Buf = Buf.Span,
					Index = ValueIndex,
					PropKeyIndex = PropKeyIndex,
					CurPropKey = CurPropKey,
				};

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
				public string BufDebugString => Encoding.UTF8.GetString(Buf.ToArray());
				
				// This seems wrong because JsonFrame doesn't correspond to a single nesting level
					// List of properties that were skipped earlier (immediate children)
					// Note: the keys here never use JsonType.SimpleString
					//public Dictionary<JsonValue, (JsonValue value, long position)>? SkippedProps;
			}

			public Parser(IScanner<byte> scanner, Options options)
			{
				_mainScanner = scanner;

				Init(options);
			}
			public Parser(ReadOnlyMemory<byte> memory, Options options)
			{
				_mainScanner = null;
				_frame.Buf = memory;
				_frame.IsReplaying = true;

				Init(options);
			}
			[MemberNotNull(nameof(_opt), nameof(_optRead))]
			private void Init(Options options)
			{
				_frame.ObjectStartIndex = int.MaxValue;

				_opt = options;
				_optRead = options.Read;

				// Skip initial whitespace
				var cur = CurPointer;
				SkipWhitespace(ref cur);
				Commit(ref cur);
			}

			private JsonFrame _frame;

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
				
				// Not used by Parser. Used by ReaderState to track skipped values.
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

			public bool ReachedEndOfList => _frame.CurrentByte == ']';

			/// <summary>Returns a struct that points to the beginning of the current value 
			///   and if !IsInsideList, knows the associated key. If there are no more 
			///   values in the current object, thie property points to the closing ']'
			///   or '}', or to the end of the file. If parsing stopped at a syntax error,
			///   this may point to the error or to the last value before the error.</summary>
			protected JsonPointer CurPointer => _frame.Pointer;

			/// <summary>Returns the last index saved with Commit(), which should always point 
			///   either to the first byte of a JSON value (not a property key!), or to the end
			///   of an object (or the end of the file, if the end has been reached).
			///   This property is always equal to `CurPosition.Index`.
			/// </summary>
			protected int CurIndex => _frame.ValueIndex;
			internal long CurPosition => _frame.PositionOfBuf0 + _frame.ValueIndex;
			
			protected ref JsonValue CurPropKey => ref _frame.CurPropKey;

			/// <summary>Text of the current key (empty if inside a list or at end of object)</summary>
			protected ReadOnlyMemory<byte> NextFieldKey => _frame.CurPropKey.Text;

			protected long PositionOfBuf0 => _frame.PositionOfBuf0;
			protected long PositionOf(in JsonPointer cur) => _frame.PositionOfBuf0 + cur.Index;

			#region Support for "replay" of skipped memory blocks (to scan an object or value again)

			/// Only needed if data is read out-of-order; not used direct
			private InternalList<JsonFrame> _frameStack = InternalList<JsonFrame>.Empty;

			protected void BeginReplay(in (JsonValue value, long position) value, out JsonPointer cur)
			{
				Debug.Assert(value.value.Type >= JsonType.FirstCompositeType);
				Debug.Assert((value.value.Text.Span[0] == '{') == (value.value.Type == JsonType.Object));
				_frameStack.Add(_frame);
				_frame = new JsonFrame {
					Buf = value.value.Text,
					IsReplaying = true,
					PositionOfBuf0 = value.position,
					ValueIndex = 0,
					ObjectStartIndex = int.MaxValue,
				};
				cur = _frame.Pointer;
			}

			protected void EndReplay()
			{
				_frame = _frameStack.Last;
				_frameStack.Pop();
			}

			protected int ReplayDepth => _frameStack.Count;

			#endregion

			protected void Commit(ref JsonPointer cur)
			{
				_frame.ValueIndex = cur.Index;
				_frame.PropKeyIndex = cur.PropKeyIndex;
				_frame.CurPropKey = cur.CurPropKey;
				_frame.CurrentByte = cur.ByteOr(']');
			}

			/// <summary>
			/// Checks if cur points to an object or list value and if so, "begins" the 
			/// object by 
			/// (1) skipping the opening '[' or '{', 
			/// (2) pushing the object type onto _stack, which throws if
			///     _optRead.MaxDepth is exceeded.
			/// (3) advancing cur to the beginning of the first value in the object, 
			/// (4) updating ReachedEndOfList, and 
			/// (5) committing cur because there's no mechanism to undo all the prior 
			///     operations. 
			/// </summary>
			/// <param name="allowList">If this is true and the input is a list, the 
			///   operation is aborted (JsonType.List is still returned).</param>
			/// <returns>JsonType.List or JsonType.Object if a subobject was detected;
			///   otherwise, returns JsonType.NotParsed.</returns>
			protected JsonType TryToBeginObject(ref JsonPointer cur, bool allowList)
			{
				bool isList;
				if (AutoRead(ref cur) && ((isList = cur.Byte == '[') || cur.Byte == '{')) {
					if (isList) {
						if (allowList) {
							cur.Index++;
							SkipWhitespace(ref cur);
							Push(JsonType.List);
						}
						return JsonType.List;
					} else {
						cur.Index++;
						SkipWhitespace(ref cur);
						Push(JsonType.Object);
						BeginProp(false, ref cur);
						return JsonType.Object;
					}
				} else {
					return JsonType.NotParsed;
				}
			}

			// Undo action(s) taken by TryToBeginObject, under the assumption that
			// CurPointer points at the first byte of the object ('{' or '['), which
			// means that setting `cur = CurPosition` takes us back there.
			//
			// NOTE: often TryToBeginObject is called after BeginReplay. In that
			// case, the derived class must call EndReplay after UndoBeginObject.
			protected void UndoBeginObject(ref JsonPointer cur)
			{
				Pop();
				cur = CurPointer;
				Debug.Assert((char) cur.ByteOr('!') is '{' or '[');
			}

			/// <summary>
			/// This must be called when the list is reached in a deduplicated list such as 
			///   <c>{ "$id": 7, "$values": [1,2,3,4,5] }</c>.
			/// This method skips the opening '[' and whitespace, changes the top-of-stack 
			/// type to <see cref="JsonType.ListWithId"/> instead of creating a new stack 
			/// entry for the list, sets <see cref="IsInsideList"/>, and calls 
			/// <see cref="Commit"/>. None of this is done unless the next byte is '['.
			/// </summary>
			protected bool TryOpenListValuesAndCommit(ref JsonPointer cur)
			{
				if (AutoRead(ref cur) && cur.Byte == '[') {
					cur.Index++;
					SkipWhitespace(ref cur);
					_stack.LastRef.Type = JsonType.ListWithId;
					IsInsideList = true;
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
			protected bool EndObjectAndCommit(ref JsonPointer cur)
			{
				var type = _stack.Last.Type;

				if (type == JsonType.ListWithId) {
					EndObjectCore(ref cur);
					IsInsideList = false;
				}

				EndObjectCore(ref cur);

				Pop();

				// TODO: this use of VerifyEof looks suspicious to me, is the code correct?
				bool nextPropAfterObjectExists = false;
				if (_stack.Count != 0 || !_optRead.VerifyEof) {
					nextPropAfterObjectExists = BeginNext(ref cur);
				} else {
					SkipWhitespace(ref cur);
					if (AutoRead(ref cur)) {
						ThrowError(cur.Index, "Expected EOF", false);
					}
				}
				Commit(ref cur);
				
				return nextPropAfterObjectExists;
			}

			private void EndObjectCore(ref JsonPointer cur)
			{
				bool isList;
				if (!((isList = cur.Byte == ']') || cur.Byte == '}')) {
					// Skip any remaining properties of the object

					if (!IsInsideList && CurPropKey.Type == JsonType.Invalid) {
						// Reading the prop key failed earlier, so we should be at the end
						// of the object, but we're not. Must be a syntax error.
						goto missingCloser;
					}

					bool hasMoreData;
					do {
						// TODO: document limitation: any single skipped value is limited to 2GB
						var ignored = ScanValue(ref cur);
						hasMoreData = BeginNext(ref cur);
					} while (hasMoreData);

					var b = cur.ByteOr(0);
					if (!((isList = b == ']') || b == '}'))
						goto missingCloser;
				}

				// check that the closer was expected
				if (IsInsideList != isList)
					goto missingCloser;
				cur.Index++;
				return;

			missingCloser:
				ThrowError(cur.Index, IsInsideList ? "Expected ']'" : "Expected '}'", fatal: true);
			}

			// Used by ISyncManager.HasField() to detect the type of the current (unparsed) value
			protected JsonType DetectTypeOfUnparsedValue(ref JsonPointer cur)
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
							for (lookahead.Index++; AutoRead(ref lookahead); lookahead.Index++) {
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

			protected bool BeginProp(bool expectComma, ref JsonPointer cur)
			{
				// This must not be called inside a list (but at the beginning of an
				// object, IsInsideList hasn't been set to false yet)
				Debug.Assert(!IsInsideList || !expectComma);
				
				SkipWhitespace(ref cur);
				if (expectComma) {
					if (!SkipIf(',', ref cur)) {
						cur.PropKeyIndex = cur.Index;
						cur.CurPropKey = new JsonValue(JsonType.Invalid, default);
						return false;
					}
					SkipWhitespace(ref cur);
				}

				cur.PropKeyIndex = cur.Index;
				var key = cur.CurPropKey = ScanValue(ref cur);

				if (key.Type == JsonType.Invalid)
				{
					// There's no property here. It's either the end of the object, or a syntax error
					if (IsCloserAt(cur)) {
						if (expectComma && _optRead.Strict)
							ThrowError(cur.Index, "Comma is not allowed before '{0}'".Localized((char) cur.Byte));
						else if (cur.Byte == ']')
							ThrowError(cur.Index, "Expected '}'");

						cur.PropKeyIndex = cur.Index;
						cur.CurPropKey = default;
						return false;
					} else
						ThrowError(cur.Index, "Expected a property name");
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
						ThrowError(cur.PropKeyIndex, "Expected a string");
				}

				// Skip the ':'
				if (!SkipWhitespaceAnd(':', ref cur))
					ThrowError(cur.Index, "Expected ':'");
				
				// Skip whitespace again to reach the beginning of the value
				SkipWhitespace(ref cur);

				return true;
			}

			protected bool IsCloserAt(in JsonPointer cur)
			{
				return (uint)cur.Index < (uint)cur.Buf.Length && (cur.Byte == '}' || cur.Byte == ']');
			}

			// A method called to move to the next prop or list item after reading a prop/item.
			// Its job is to skip the comma and either detect end-of-list or read the next key.
			protected bool BeginNext(ref JsonPointer cur)
			{
				if (IsInsideList) {
					cur.CurPropKey = default;

					bool reachedEnd;
					if (SkipWhitespaceAnd(',', ref cur)) {
						SkipWhitespace(ref cur);
						cur.PropKeyIndex = cur.Index;

						reachedEnd = cur.ByteOr(']') == ']';
						if (reachedEnd && _optRead.Strict)
							ThrowError(cur.Index, "Comma is not allowed before '{0}'".Localized((char)cur.Byte));
					} else {
						cur.PropKeyIndex = cur.Index;

						reachedEnd = cur.ByteOr(']') == ']';
						if (!reachedEnd)
							ThrowError(cur.Index, "Expected ']'", fatal: true);
					}
					return !reachedEnd;
				} else {
					return BeginProp(expectComma: true, ref cur);
				}
			}

			protected JsonValue ScanValueAndBeginNext(ref JsonPointer cur)
			{
				var value = ScanValue(ref cur);
				BeginNext(ref cur);
				return value;
			}

			#endregion

			#region SkipWhitespace, SkipComment

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected void SkipWhitespace(ref JsonPointer cur)
			{
				for (; AutoRead(ref cur); cur.Index++) {
					byte c = cur.Byte;
					if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
						continue;
					if (c == '/')
						SkipComment(ref cur);
					else
						break;
				}
			}

			protected void SkipComment(ref JsonPointer cur)
			{
				byte c = cur.Byte;
				if (c == '/' && AutoRead(ref cur, 1)) {
					if ((c = cur[1]) == '/') {
						// TODO: add "warning" feature: Report(cur, _optRead.AllowComments, "JSON does not support comments")
						if (!_optRead.AllowComments)
							ThrowError(cur.Index, "JSON does not support comments");
							
						// Skip single-line comment
						for (cur.Index += 2; ; cur.Index++) {
							if (!AutoRead(ref cur, 1))
								return;
							if ((c = cur.Byte) == '\r' || c == '\n')
								break;
						}
					} else if (c == '*') {
						if (!_optRead.AllowComments)
							ThrowError(cur.Index, "JSON does not support comments");

						// Skip multi-line comment
						int commentSize = 2;
						for (cur.Index += 2; ; cur.Index++, commentSize++) {
							if (!AutoRead(ref cur, 1))
								ThrowError(cur.Index - commentSize, "JSON syntax error: multiline comment was not closed");
							if (cur.Byte == '*' && cur[1] == '/') {
								cur.Index += 2;
								break;
							}
						}
					}
				}
			}

			#endregion

			#region Error management

			[MethodImpl(MethodImplOptions.NoInlining)]
			protected void ThrowSyntaxError(int index, string? propName, string context = "JSON value")
			{
				// TODO: localize all errors exactly once
				var cur = CurPointer;
				var msg = "Syntax error in {0}".Localized(context.Localized());
				if (index >= cur.Buf.Length) {
					Debug.Assert(!AutoRead(ref cur));
					msg = "Unexpected end-of-file in {0}".Localized(context.Localized());
				}
				if (propName != null)
					msg += " \"" + propName + '"';
				throw NewError(index, msg);
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private void ThrowMaxDepthError(int i)
			{
				throw NewError(i, "Unable to read JSON because it is too deeply nested", fatal: true);
			}

			protected Exception NewError(int i, string msg, bool fatal = true)
				=> NewError(PositionOfBuf0 + i, msg, fatal);

			[MethodImpl(MethodImplOptions.NoInlining)]
			protected Exception NewError(long position, string msg, bool fatal = true)
			{
				if (_fatalError != null)
					return _fatalError; // New error is just a symptom of the old error; rethrow

				string msg2;
				int index = (int)(position - PositionOfBuf0);
				if ((uint)index >= (uint)CurPointer.Buf.Length) {
					msg2 = "{0} (at byte {1})".Localized(msg, position);
				} else {
					int c = G.DecodeUTF8Char(CurPointer.Buf, ref index);
					msg2 = "{0} (at byte {1} '{2}')".Localized(msg, position, c < 32 ? "0x" + c.ToString("X") : (char)c);
				}
				
				var exc = new FormatException(msg2);
				exc.Data["position"] = position;
				exc.Data["recoverable"] = !fatal;
				
				if (fatal)
					_fatalError = exc;
				return exc;
			}

			protected void ThrowError(int i, string msg, bool fatal = true)
				=> throw NewError(i, msg, fatal);
			protected void ThrowError(long position, string msg, bool fatal = true)
				=> throw NewError(position, msg, fatal);

			#endregion

			#region AutoRead

			// The scanner could choose a much larger size, but this is the minimum we'll tolerate
			const int DefaultMinimumScanSize = 32;

			// Ensures that the _i < _buf.Length by reading more if necessary
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool AutoRead(ref JsonPointer cur, int extraLookahead = 0)
			{
				Debug.Assert(cur.Buf == _frame.Buf.Span);
				if ((uint)(cur.Index + extraLookahead) < (uint)cur.Buf.Length)
					return true;
				cur.Index = Read(cur.Index, extraLookahead + 1);
				cur.Buf = _frame.Buf.Span;
				return (uint)(cur.Index + extraLookahead) < (uint)cur.Buf.Length;
			}
			// Reads new data into Frame.Buf if possible
			private int Read(int index, int lookaheadNeeded)
			{
				if (_frame.IsReplaying)
					return index;

				int requestSize = Max(lookaheadNeeded, DefaultMinimumScanSize);
				int skip = Min(_frame.ObjectStartIndex, Min(_frame.ValueIndex, index));
				
				_frame.Buf = _mainScanner!.Read(skip, (index -= skip) + requestSize, ref _mainScannerBuf);

				_frame.ValueIndex -= skip;
				_frame.PropKeyIndex -= skip;
				if (_frame.ObjectStartIndex != int.MaxValue)
					_frame.ObjectStartIndex -= skip;
				
				return index;
			}

			#endregion

			#region ScanValue()

			int _skipObjectDepth;

			protected JsonValue ScanValue(ref JsonPointer cur)
			{
				int startIndex = cur.Index;
				if (AutoRead(ref cur)) {
					var b = cur.Byte;
					if (b == '"')
					{
						var type = JsonType.SimpleString;
						for (cur.Index++; AutoRead(ref cur); cur.Index++) {
							if (cur.Byte == '\\') {
								type = JsonType.String;
								cur.Index++; // we don't yet care which escape it is, but be sure to skip \"
							} else if (cur.Byte == '"') {
								return new JsonValue(type, _frame.Buf.Slice(startIndex, ++cur.Index - startIndex));
							} else if (cur.Byte == '\n') {
								_frame.LineNumber++;
								if (_optRead.Strict)
									throw NewError(cur.Index, "Newline in JSON string literal");
							} else if (cur.Byte >= 0x80) { // non-ASCII character
								type = JsonType.String;
							}
						}
					}
					else if (b == 'n' && AutoRead(ref cur, 3) && cur[1] == 'u' && cur[2] == 'l' && cur[3] == 'l')
					{
						int i = cur.Index;
						cur.Index += 4;
						return new JsonValue(JsonType.Null, _frame.Buf.Slice(i, 4));
					}
					else if (b == 'f' && AutoRead(ref cur, 4) && cur[1] == 'a' && cur[2] == 'l' && cur[3] == 's' && cur[4] == 'e')
					{
						int i = cur.Index;
						cur.Index += 5;
						return new JsonValue(JsonType.False, _frame.Buf.Slice(i, 5));
					}
					else if (b == 't' && AutoRead(ref cur, 3) && cur[1] == 'r' && cur[2] == 'u' && cur[3] == 'e')
					{
						int i = cur.Index;
						cur.Index += 4;
						return new JsonValue(JsonType.True, _frame.Buf.Slice(i, 4));
					}
					else if (b == '{' || b == '[')
					{
						char closer = b == '[' ? ']' : '}';

						// TOS.ObjectStartIndex acts as a "don't skip" signal, to ensure Read()
						// doesn't skip past it, guaranteeing that the beginning of the object
						// will remain in the buffer. considering that this method can be on the
						// stack multiple times, we can safely decrease it, but not increase it.
						// Also, the old value must be restored once we're done scanning.
						var oldObjectStart = _frame.ObjectStartIndex;
						_frame.ObjectStartIndex = Min(_frame.ObjectStartIndex, cur.Index);
						long objectPosition = cur.Index + _frame.PositionOfBuf0;
						cur.Index++;

						// Avoid stack overflow (it would terminate the process)
						if (_skipObjectDepth++ + _stack.Count > _optRead.MaxDepth)
							ThrowMaxDepthError(cur.Index);

						JsonValue idValue = default;

						if (!SkipWhitespaceAnd(closer, ref cur)) {
							for (;;) {
								var key = ScanValue(ref cur);
								if (key.Type == JsonType.Invalid)
									ThrowError(cur.Index, "Expected a value");

								if (closer == '}') {
									if (!SkipWhitespaceAnd(':', ref cur))
										ThrowError(cur.Index, "Expected ':'");
									SkipWhitespace(ref cur);

									var value = ScanValue(ref cur);
									if (value.Type == JsonType.Invalid)
										ThrowError(cur.Index, "Expected a value");

									if (idValue.Type == default && IsObjectIdProp(key))
										idValue = value;
								}

								if (SkipWhitespaceAnd(',', ref cur)) {
									SkipWhitespace(ref cur);
									if (SkipIf(closer, ref cur)) {
										if (_optRead.Strict)
											ThrowError(cur.Index, "Comma is not allowed before '{0}'".Localized((char) cur.Byte));
										break;
									}
								} else {
									if (SkipIf(closer, ref cur))
										break;
									ThrowError(cur.Index, "Expected ','");
								}
							}
						}

						_skipObjectDepth--;

						var type = closer == ']' ? JsonType.List : JsonType.Object;
						int objectStart = (int)(objectPosition - _frame.PositionOfBuf0);
						_frame.ObjectStartIndex = oldObjectStart;
						//TOS.TokenIndex = cur.i;
						var @object = new JsonValue(type, _frame.Buf.Slice(objectStart, cur.Index - objectStart));
						if (idValue.Type != default)
							SaveSkippedObjectWithId(idValue, @object, _frame.PositionOfBuf0 + objectStart);

						return @object;
					}
					else // expect a number, or end-of-object
					{
						if (b == '-') {
							cur.Index++;
							if (!AutoRead(ref cur))
								return new JsonValue(JsonType.Invalid, default); // EOF
							b = cur.Byte;
						}

						var type = JsonType.PlainInteger;
						if (b >= '0' && b <= '9') {
							// Read initial digits
							if (_optRead.Strict && b == '0')
								cur.Index++;
							else {
								do
									cur.Index++;
								while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9');
							}
						} else if (b != '.' || _optRead.Strict) {
							if (b == '.')
								ThrowError(cur.Index, "Expected '0.' instead of '.'");
							else
								return new JsonValue(JsonType.Invalid, default);
						}

						// Read decimal places, if any
						if (AutoRead(ref cur) && cur.Byte == '.') {
							type = JsonType.Number;
							cur.Index++;
							int old_i = cur.Index;
							while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9')
								cur.Index++;
							if (old_i == cur.Index && _optRead.Strict)
								ThrowError(cur.Index, "Expected digits after '.'");
						}

						// Read exponent, if any
						if (AutoRead(ref cur) && (cur.Byte == 'e' || cur.Byte == 'E')) {
							type = JsonType.Number;
							cur.Index++;
							if (AutoRead(ref cur) && (cur.Byte == '+' || cur.Byte == '-'))
								cur.Index++;
							int iOld = cur.Index;
							while (AutoRead(ref cur) && cur.Byte >= '0' && cur.Byte <= '9')
								cur.Index++;
							if (iOld == cur.Index)
								ThrowError(cur.Index, "Expected exponent digits");
						}

						return new JsonValue(type, _frame.Buf.Slice(startIndex, cur.Index - startIndex));
					}
				}
				return new JsonValue(JsonType.Invalid, default); // EOF
			}

			// These methods exist because the derived class needs to know when a deduplicated
			// object is skipped, so that it can be read later. For example:
			//     {
			//        "A": {
			//           "X": { "$id": "9", "field": 111 },
			//        },
			//        "B": 222,
			//        "C": { "$ref": "9" }
			//     }
			// If "C" is read before "A", "A" was skipped and the inner object "9" must be
			// replayed to get the value of "C".
			protected abstract bool IsObjectIdProp(in JsonValue key);
			protected abstract void SaveSkippedObjectWithId(in JsonValue idValue, in JsonValue @object, long position);

			#endregion

			private bool SkipWhitespaceAnd(char expecting, ref JsonPointer cur)
			{
				SkipWhitespace(ref cur);
				return SkipIf(expecting, ref cur);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool SkipIf(char expecting, ref JsonPointer cur)
			{
				if (AutoRead(ref cur) && cur.Byte == expecting) {
					cur.Index++;
					return true;
				}
				return false;
			}

			private void Push(JsonType objectType)
			{
				// Avoid stack overflow (which would terminate the process)
				if (_stack.Count >= _optRead.MaxDepth)
					ThrowMaxDepthError(_frame.ValueIndex);

				_stack.Add(new StackEntry(objectType));
				IsInsideList = objectType != JsonType.Object;
			}
			private void Pop()
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
				#elif NETSTANDARD2_0 || NET45 || NET46 || NET47
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
						return DecodeEscape(curProp, ref cp_i, _frame.PropKeyIndex);
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
					case (byte) 'v':
						if (_optRead.Strict)
							ThrowError(stringStartIndex + cp_i, "JSON does not support the '\\v' escape sequence");
						++cp_i;
						return '\v';
					case (byte) '0':
						if (_optRead.Strict)
							ThrowError(stringStartIndex + cp_i, "JSON does not support the '\\0' escape sequence");
						++cp_i;
						return '\0';
					case (byte) 'u':
						int a = 0, b = 0, c = 0, d = 0;
						if (cp_i + 4 >= curProp.Length
							|| (a = G.HexDigitValue((char) curProp[cp_i + 1])) < 0
							|| (b = G.HexDigitValue((char) curProp[cp_i + 2])) < 0
							|| (c = G.HexDigitValue((char) curProp[cp_i + 3])) < 0
							|| (d = G.HexDigitValue((char) curProp[cp_i + 4])) < 0)
							ThrowError(stringStartIndex + cp_i, "'\\u' escape sequence was too short");
						cp_i += 5;
						int ch = (a << 12) | (b << 8) | (c << 4) | d;
						return ch;
				}
				if (_optRead.Strict)
					ThrowError(stringStartIndex + cp_i, "Invalid escape sequence '\\{0}'".Localized(curProp[cp_i]));
				return '\\';
			}

			#endregion
		}
	}
}
