using Loyc.Collections;
using Loyc.Collections.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		internal class ReaderState
		{
			enum JsonType {
				NotParsed = 0,
				String = 1,
				Number = 2,
				Null = 3,
				True = 4,
				False = 5,

				FirstCompositeType = 10,
				Object = 10,
				List = 11,
			}

			struct JsonValue {
				public JsonType Type;
				public Memory<byte> Text;

				public JsonValue(JsonType type, Memory<byte> text)
				{
					Type = type;
					Text = text;
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
			struct JsonFrame {
				public Dictionary<ReadOnlyMemory<byte>, JsonValue>? SkippedProps;
				public Dictionary<ReadOnlyMemory<byte>, JsonValue>? SkippedDefs;
				public JsonValue CurId; // "$id" or "\f" prop at start of current object
				public bool IsInsideList;
				public ReadOnlyMemory<byte> CurProp; // without the quotes
				// TODO: optimize as InternalList.Scanner
				public IScanner<byte> Scanner;
			}

			public ReaderState(IScanner<byte> scanner, Options options)
			{
				_scanner = scanner;
				_opt = options;
				_stack.Add(new JsonFrame { IsInsideList = true, Scanner = scanner });
			}

			private IScanner<byte> _scanner;
			private Memory<byte> _buf;
			private int _i; // _buf[_i] is the beginning of the "next" token
			internal Options _opt;
			
			private InternalList<JsonFrame> _stack = InternalList<JsonFrame>.Empty;
			private JsonFrame TOS; // Top of stack (not stored in _stack)
			
			private JsonFrame _here;
			protected Dictionary<ReadOnlyMemory<byte>, object>? _objects;

			//internal bool _isInsideList = true;
			//Dictionary<ReadOnlyMemory<byte>, JsonValue>? _skippedProps;
			//Dictionary<ReadOnlyMemory<byte>, JsonValue>? _skippedRefs;
			InternalList<byte> _nameBuf;

			Memory<byte> ToNameBuf(ReadOnlySpan<char> name) {
				int len = WriterState.GetLengthAsBytes(name, _opt.EscapeUnicode);
				if (_nameBuf.Count < len)
					_nameBuf.Resize(len);
				int i = 0;
				WriterState.WriteStringCore(_nameBuf.AsSpan(), name, len, ref i, _opt.EscapeUnicode);
				Debug.Assert(i == len);
				return _nameBuf.InternalArray.AsMemory().Slice(0, len);
			}

			bool FindProp(string? name, out JsonValue value)
			{
				ref JsonFrame tos = ref TOS;
				value = default;
				if (tos.IsInsideList || name == null)
					return true;

				var originalName = name;
				if (_opt.NameConverter != null)
					name = _opt.NameConverter(name);

				// Is it the current property?
				if (AreEqual(tos.CurProp, name))
					return true;

				// Is it something we skipped over earlier?
				Memory<byte> nameBytes;
				var skippedProps = tos.SkippedProps;
				if (skippedProps != null) {
					nameBytes = ToNameBuf(name.AsSpan());
					if (skippedProps.TryGetValue(nameBytes, out value))
						return true;
				}

				// The current and skipped properties aren't what we need. Scan forward.
				if (tos.CurProp.Span != default(ReadOnlySpan<byte>)) {
					while (true) {
						var skippedValue = ScanValue();
						SaveSkippedValue(TOS.CurProp, skippedValue);

						if (!BeginProp((byte)','))
							break;
					}
				} else if (tos.SkippedProps == null)
					return false;

				// Name was not found in this object. Fallback: if there is a
				// NameConverter, try looking for the orginalName.
				nameBytes = ToNameBuf(originalName.AsSpan());
				return tos.SkippedProps!.TryGetValue(nameBytes, out value);
			}

			private void SaveSkippedValue(in ReadOnlyMemory<byte> propName, in JsonValue skippedValue)
			{
				TOS.SkippedProps ??= new Dictionary<ReadOnlyMemory<byte>, JsonValue>();
				TOS.SkippedProps[propName] = skippedValue;
			}

			internal (bool Begun, object? Object) BeginSubObject(string? name, SubObjectMode mode)
			{
				if (!FindProp(name, out JsonValue skippedObject))
					throw new FormatException("Property \"{0}\" was missing".Localized(name));

				var type = skippedObject.Type;
				if (type == JsonType.NotParsed)
					type = TryOpenObject();
				else
					BeginSkippedObject(skippedObject);

				bool expectList = (mode & SubObjectMode.List) != 0;

				if (type >= JsonType.FirstCompositeType) {
					// Check if it's an object containing a backreference
					var curProp = TOS.CurProp;
					if (TOS.IsInsideList) {
						if (expectList)
							return (true, null); // success: list opened
					} else {
						if (_objects != null && (AreEqual(curProp, _ref) || AreEqual(curProp, _r))) {
							// Read backreference
							var value = ScanValue();
							bool hasNext = BeginProp((byte)',');
							if (!hasNext && value.Type < JsonType.FirstCompositeType) {
								EndSubObject();
								return (false, _objects![value.Text]);
							} else {
								// This cannot be a backreference.
								SaveSkippedValue(curProp, value);
							}
						}

						// Read object ID, if any
						if (AreEqual(curProp, _f) || AreEqual(curProp, _id)) {
							TOS.CurId = ScanValue();
							if (TOS.CurId.Type >= JsonType.FirstCompositeType) {
								// This cannot be an id.
								SaveSkippedValue(curProp, TOS.CurId);
								TOS.CurId = default;
							}
							BeginProp((byte)',');

							// If caller wants a list, there should be a list prop "": [...]
							curProp = TOS.CurProp;
							if (expectList && (curProp.Length == 0 || AreEqual(curProp, _values))) {
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
				
				// "obj2":{"fld": {"\f":5, "x":5} }
				// "obj1":{"\r":5}
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

			private void BeginSkippedObject(in JsonValue value)
			{
				throw new NotImplementedException();
			}

			private bool BeginProp(byte separator)
			{
				throw new NotImplementedException();
			}

			private bool AreEqual(ReadOnlyMemory<byte> nextProp, string? name)
			{
				throw new NotImplementedException();
			}
			private bool AreEqual(ReadOnlyMemory<byte> nextProp, byte[] @ref)
			{
				throw new NotImplementedException();
			}
			private JsonType TryOpenObject()
			{
				SkipToNextToken(0);
				//if (_buf[0] == '{') {

				//}
				return JsonType.NotParsed;
			}

			private void SkipToNextToken(int minBytes)
			{
				var span = _buf.Span;
				for (; (uint)_i < (uint)span.Length; _i++) {
					byte c = span[_i];
					if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
						continue;
					if (c == '/' && (uint)(_i + 1) < (uint)span.Length) {
						// TODO: throw if JSON comment support is disabled
						if ((c = span[_i + 1]) == '/') {
							// Skip single-line comment
							for (_i += 2; ; _i++) {
								if ((uint)_i >= (uint)span.Length)
									return;
								if ((c = span[_i]) == '\r' || c == '\n')
									break;
							}
						} else if (c == '*') {
							// Skip multi-line comment
							for (_i += 3; ; _i++) {
								if ((uint)_i >= (uint)span.Length)
									throw new FormatException("JSON syntax error: multiline comment was not closed");
								if (span[_i] == '/' && span[_i - 1] == '*')
									break;
							}
						}
					}
				}
			}

			private JsonValue ScanValue()
			{
				throw new NotImplementedException();
			}

			private object? DetectTokenType()
			{
				throw new NotImplementedException();
			}
		}
	}
}
