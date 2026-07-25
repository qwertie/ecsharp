using Loyc.Collections.Impl;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using Loyc.Syntax;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		/// <summary>The core logic for writing JSON data in UTF-8 format</summary>
		internal partial class WriterState : WriterStateBase
		{
			internal bool _isInsideList = true;
			internal Options _opt;
			protected Options.ForWriter _optWrite;

			/// <summary>Keeps track of objects that the user has started writing with 
			///   BeginSubObject, but hasn't finished writing.</summary>
			protected InternalList<ObjectMode> _stack = InternalList<ObjectMode>.Empty;

			protected byte[] _indent;
			protected byte[] _newline;
			protected byte _pendingComma;
			protected int _compactMode;

			public WriterState(IBufferWriter<byte> output, Options options) : base(output) {
				_opt = options;
				_optWrite = _opt.Write;
				_indent = Encoding.UTF8.GetBytes(_optWrite.Indent);
				_newline = Encoding.UTF8.GetBytes(_optWrite.Newline);
				if (_opt.NameConverter != null)
					_nameCache = new Dictionary<string, string>();
			}

			// Caches the results of _opt.NameConverter so that it doesn't reallocate the
			// same strings numerous times.
			protected Dictionary<string, string>? _nameCache;

			// Applies _opt.NameConverter, memoizing its result per distinct property name.
			string ConvertName(string? propName)
			{
				propName ??= "";
				if (!_nameCache!.TryGetValue(propName, out var converted))
					_nameCache[propName] = converted = _opt.NameConverter!(propName);
				return converted;
			}

			public int Depth => _stack.Count;

			// Writes the pending comma/newline, if any, and gets a Span for writing.
			Span<byte> GetNextBuf(int requiredBytes)
			{
				if (_pendingComma != 0) {
					var buf = base.FlushAndGetOutSpan(requiredBytes + 1 + NewlineSize);
					if (_pendingComma != '\n')
						buf[_i++] = _pendingComma;
					MaybeNewlineWithIndent(buf);
					_pendingComma = 0;
					return buf;
				} else {
					return base.FlushAndGetOutSpan(requiredBytes);
				}
			}

			public (bool Begun, int Length, object? Object) BeginSubObject(string? name, object? childKey, ObjectMode mode)
			{
				if (childKey == null && MayBeNullable(mode)) {
					WriteNull(name);
					return (false, 0, childKey);
				}

				var buf = BeginProp(name, 25); // Reserve extra bytes for refs: {"$ref":"12345678901"}

				if ((mode & ObjectMode.Deduplicate) != 0) {
					long id = _idGen.GetId(childKey, out bool firstTime);
					if (!firstTime) {
						WriteBackReference(buf, id);
						return (false, 0, childKey);
					} else {
						OpenBraceOrBrack(mode & ~ObjectMode.List);
						if (_opt.NewtonsoftCompatibility)
							WriteIdAsQuotedNumber("$id", id); // was: WriteProp("$id", id.ToString())
						else
							WriteProp("\f", id);
						if ((mode & ObjectMode.List) != 0) {
							string valuesProp = _opt.NewtonsoftCompatibility ? "$values" : "";
							BeginProp(valuesProp, 10);
							OpenBraceOrBrack(mode);
						}
						return (true, int.MaxValue, childKey);
					}
				} else {
					OpenBraceOrBrack(mode);
				}

				if ((mode & ObjectMode.Compact) != 0)
					_compactMode++;
				return (true, int.MaxValue, childKey);
			}

			// Writes `"name": "<id>"` (the Newtonsoft $id form) without allocating a
			// string for the number.
			void WriteIdAsQuotedNumber(string propName, long id)
			{
				Span<byte> buf = BeginProp(propName, 22);
				buf[_i++] = (byte) '"';
				WriteNumber(buf, id, true);
				buf[_i++] = (byte) '"';
				_pendingComma = (byte) ',';
			}

			private void WriteBackReference(Span<byte> buf, long id)
			{
				buf[_i++] = (byte) '{';
				buf[_i++] = (byte) '"';
				if (_opt.NewtonsoftCompatibility) {
					buf[_i++] = (byte) '$';
					buf[_i++] = (byte) 'r';
					buf[_i++] = (byte) 'e';
					buf[_i++] = (byte) 'f';
					buf[_i++] = (byte) '"';
					buf[_i++] = (byte) ':';
					if (_optWrite.SpaceAfterColon)
						buf[_i++] = (byte) ' ';
					buf[_i++] = (byte) '"';
					WriteNumber(buf, id, true);
					buf[_i++] = (byte) '"';
				} else {
					buf[_i++] = (byte) '\\';
					buf[_i++] = (byte) 'r';
					buf[_i++] = (byte) '"';
					buf[_i++] = (byte) ':';
					if (_optWrite.SpaceAfterColon)
						buf[_i++] = (byte) ' ';
					WriteNumber(buf, id, true);
				}
				buf[_i++] = (byte) '}';
				_pendingComma = (byte) ',';
			}

			public void EndSubObject()
			{
				CloseBraceOrBrack();
			}

			void OpenBraceOrBrack(ObjectMode mode)
			{
				_stack.Add(mode);

				var buf = GetNextBuf(1 + NewlineSize);

				_isInsideList = (mode & ObjectMode.List) != 0;

				buf[_i++] = (byte) (_isInsideList ? '[' : '{');

				// GetNextBuf() understands this as a request for a newline + indentation
				_pendingComma = (byte) '\n';
			}

			void CloseBraceOrBrack()
			{
				var mode = _stack.Last;

				// This will cause an unindent, since it is done before GetNextBuf() which writes the newline/indent
				_stack.Pop();

				// Cancel ',' at end of list/object; just make a newline. If _pendingComma
				// is '\n', the object/list is empty and we don't even need a newline.
				_pendingComma = (byte) (_pendingComma == '\n' ? 0 : '\n');

				bool isList = (mode & ObjectMode.List) != 0;
				if (isList && (mode & ObjectMode.Deduplicate) != 0)
				{
					Debug.Assert(_stack.Last == (mode & ~ObjectMode.List));
					
					// In this case, two JSON objects are used to represent a single list, e.g.
					//     "List": { "$id": "7", "$values": [...] }
					// Also, there are two entries on the stack for this single list
					// (so that indentation works properly). Unlike OpenBraceOrBrack()
					// which is called twice in this case, CloseBraceOrBrack() is called
					// only once. Therefore we need to pop both stack entries and write
					// ']' followed by '}'.
					WriteBraceOrBrack(isList);
					_stack.Pop();
					_pendingComma = (byte) '\n';
					WriteBraceOrBrack(false);
				}
				else
				{
					WriteBraceOrBrack(isList);
				}

				// Decrement _compactMode only after writing the closing brace/bracket, so
				// that a Compact object is written entirely on one line, e.g. {"a":1}
				if ((mode & ObjectMode.Compact) != 0)
					_compactMode--;

				_pendingComma = (byte) ',';

				if (_stack.IsEmpty) {
					Flush();
					_isInsideList = true;
				} else {
					_isInsideList = (_stack.Last & ObjectMode.Tuple) != 0;
				}
			}

			// Helper function of CloseBraceOrBrack
			void WriteBraceOrBrack(bool list)
			{
				var buf = GetNextBuf(1);
				buf[_i++] = (byte) (list ? ']' : '}');
			}

			int NewlineSize => _newline.Length + _indent.Length * _stack.Count;

			void MaybeNewlineWithIndent(Span<byte> buf)
			{
				if (_newline.Length != 0 && _compactMode == 0) {
					Blurt(buf, _newline);
					for (int i = 0, count = System.Math.Min(_stack.Count, _optWrite.MaxIndentDepth); i < count; i++)
						Blurt(buf, _indent);
				}
			}

			public string? WriteProp(string? propName, string? value)
			{
				WriteProp(propName, value.AsSpan());
				return value;
			}
			public void WriteProp(string? propName, ReadOnlySpan<char> value)
			{
				if (value == default)
					WriteNull(propName);
				else {
					int valueLen = GetLengthAsBytes(value, _optWrite.EscapeUnicode);
					Span<byte> buf = BeginProp(propName, valueLen);
					WriteStringCore(buf, value, valueLen, ref _i, _optWrite.EscapeUnicode);
					_pendingComma = (byte)',';
				}
			}
			public void WriteBytesAsString(string? propName, ReadOnlySpan<byte> value)
			{
				if (value == default)
					WriteNull(propName);
				else {
					Debug.Assert(_opt.ByteArrayMode != JsonByteArrayMode.Array);
					if (_opt.NewtonsoftCompatibility || _opt.ByteArrayMode == JsonByteArrayMode.Base64) {
						// Base64 is ASCII, and the output is UTF-8, so encode straight into
						// the output buffer: no byte[] copy (ns2.0) and no intermediate
						// string or char->byte widening on any target.
						int b64Len = Base64.GetMaxEncodedToUtf8Length(value.Length);
						Span<byte> buf = BeginProp(propName, b64Len + 2);
						buf[_i++] = (byte) '"';
						var status = Base64.EncodeToUtf8(value, buf.Slice(_i), out _, out int written);
						Debug.Assert(status == OperationStatus.Done); // destination is pre-sized
						if (status == OperationStatus.Done) {
							_i += written;
						} else {
							// Defensive only. Note we must NOT call WriteProp here: BeginProp
							// has already emitted the property name, colon and opening quote.
							#if NETSTANDARD2_0 || NETFRAMEWORK
							string s64 = System.Convert.ToBase64String(value.ToArray());
							#else
							string s64 = System.Convert.ToBase64String(value);
							#endif
							for (int k = 0; k < s64.Length; k++)
								buf[_i++] = (byte) s64[k];
						}
						buf[_i++] = (byte) '"';
						_pendingComma = (byte) ',';
					} else {
						var bais = ByteArrayInString.ConvertFromBytes(value, false, 
						           _opt.ByteArrayMode == JsonByteArrayMode.PrefixedBais);
						WriteProp(propName, bais);
					}
				}
			}
			public long WriteProp(string? propName, long num, bool isSigned = true)
			{
				Span<byte> buf = BeginProp(propName, 20);
				WriteNumber(buf, num, isSigned);
				_pendingComma = (byte) ',';
				return num;
			}
			public BigInteger WriteProp(string? propName, BigInteger num)
			{
				WriteLiteralProp(propName, num.ToString(CultureInfo.InvariantCulture));
				return num;
			}
			public float WriteProp(string? propName, float num)
			{
				#if NETSTANDARD2_0 || NETFRAMEWORK
				bool finite = !(float.IsNaN(num) || float.IsInfinity(num));
				#else
				bool finite = float.IsFinite(num);
				#endif
				if (!finite) {
					// NaN/Infinity aren't legal JSON numbers, so they're written as strings.
					// NOTE for the .NET Framework build: ToString("R") on double is a
					// documented non-round-tripping API there; "G17" would be more correct,
					// but changing it would change observable output, so it is left alone.
					WriteProp(propName, num.ToString("R", CultureInfo.InvariantCulture));
					return num;
				}
				#if NETCOREAPP3_0_OR_GREATER
				// Format straight into the output buffer: no intermediate string. Guarded
				// to .NET Core 3+, where Utf8Formatter's 'R' is the shortest-round-trip
				// algorithm and matches ToString("R") exactly (verified by fuzzing).
				Span<byte> fbuf = BeginProp(propName, 32);
				if (Utf8Formatter.TryFormat(num, fbuf.Slice(_i), out int fwritten, new StandardFormat('R'))) {
					_i += fwritten;
					_pendingComma = (byte) ',';
					return num;
				}
				Blurt(fbuf, Encoding.ASCII.GetBytes(num.ToString("R", CultureInfo.InvariantCulture)));
				_pendingComma = (byte) ',';
				#else
				WriteLiteralProp(propName, num.ToString("R", CultureInfo.InvariantCulture));
				#endif
				return num;
			}
			public double WriteProp(string? propName, double num)
			{
				#if NETSTANDARD2_0 || NETFRAMEWORK
				bool finite = !(double.IsNaN(num) || double.IsInfinity(num));
				#else
				bool finite = double.IsFinite(num);
				#endif
				if (!finite) {
					WriteProp(propName, num.ToString("R", CultureInfo.InvariantCulture));
					return num;
				}
				#if NETCOREAPP3_0_OR_GREATER
				Span<byte> dbuf = BeginProp(propName, 32);
				if (Utf8Formatter.TryFormat(num, dbuf.Slice(_i), out int dwritten, new StandardFormat('R'))) {
					_i += dwritten;
					_pendingComma = (byte) ',';
					return num;
				}
				Blurt(dbuf, Encoding.ASCII.GetBytes(num.ToString("R", CultureInfo.InvariantCulture)));
				_pendingComma = (byte) ',';
				#else
				WriteLiteralProp(propName, num.ToString("R", CultureInfo.InvariantCulture));
				#endif
				return num;
			}
			public decimal WriteProp(string? propName, decimal num)
			{
				#if NETCOREAPP3_0_OR_GREATER
				// decimal.MaxValue is 29 digits + sign + point; 40 bytes is ample.
				Span<byte> buf = BeginProp(propName, 40);
				if (Utf8Formatter.TryFormat(num, buf.Slice(_i), out int written)) {
					_i += written;
					_pendingComma = (byte) ',';
					return num;
				}
				Blurt(buf, Encoding.ASCII.GetBytes(num.ToString(CultureInfo.InvariantCulture)));
				_pendingComma = (byte) ',';
				#else
				WriteLiteralProp(propName, num.ToString(CultureInfo.InvariantCulture));
				#endif
				return num;
			}
			public void WriteNull(string? propName) => WriteLiteralProp(propName, _null);
			public void WriteLiteralProp(string? propName, ReadOnlySpan<byte> literal)
			{
				Span<byte> buf = BeginProp(propName, literal.Length);
				Blurt(buf, literal);
				_pendingComma = (byte) ',';
			}
			public void WriteLiteralProp(string? propName, string ascii)
			{
				Span<byte> buf = BeginProp(propName, ascii.Length);
				for (int i = 0; i < ascii.Length; i++)
					buf[_i++] = (byte)ascii[i];
				_pendingComma = (byte) ',';
			}
			public char WriteProp(string? propName, char c)
			{
				if (c < 127 && c >= 32 && c != '\\' && c != '"') {
					Span<byte> buf = BeginProp(propName, 3);
					buf[_i++] = (byte)'"';
					buf[_i++] = (byte)c;
					buf[_i++] = (byte)'"';
				} else {
					// Avoid allocating a 1-char string: the ReadOnlySpan<char> overload
					// of WriteProp does the same work with a stack buffer.
					Span<char> one = stackalloc char[1];
					one[0] = c;
					WriteProp(propName, (ReadOnlySpan<char>)one);
				}
				_pendingComma = (byte) ',';
				return c;
			}

			// Calls GetNextBuf and writes the beginning of a JSON prop (`"propName":`),
			// unless a list is being written, in which case it only calls GetNextBuf().
			public Span<byte> BeginProp(string? propName, int reserveExtra)
			{
				Span<byte> buf;
				if (_isInsideList) {
					buf = GetNextBuf(reserveExtra);
				} else {
					if (_opt.NameConverter != null)
						propName = ConvertName(propName);
					buf = WriteString(propName.AsSpan(), 3 + reserveExtra);
					buf[_i++] = (byte) ':';
					if (_optWrite.SpaceAfterColon && _compactMode == 0)
						buf[_i++] = (byte) ' ';
				}
				return buf;
			}

			void Blurt(Span<byte> buf, ReadOnlySpan<byte> bytes)
			{
				bytes.CopyTo(buf.Slice(_i));
				_i += bytes.Length;
			}

			// Writes a number into buf at _i. 20 bytes should be available for arbitrary longs.
			void WriteNumber(Span<byte> buf, long iNum, bool isSigned)
			{
				Debug.Assert(buf.Length - _i >= 16);
				ulong num;
				if (isSigned && iNum < 0) {
					buf[_i++] = (byte)'-';
					num = (ulong)-iNum;
				} else {
					num = (ulong)iNum;
				}

				// optimize the common case
				if (num < 10) {
					buf[_i++] = (byte)('0' + num);
					return;
				}

				// Utf8Formatter computes the digit count up front and writes two digits
				// at a time, so it avoids both the per-digit division and the reversal
				// pass below (measured ~2.7x faster on multi-digit values). It is always
				// invariant, which is what JSON wants.
				if (Utf8Formatter.TryFormat(num, buf.Slice(_i), out int written)) {
					_i += written;
					return;
				}

				// Write the number... backwards
				int start = _i;
				do {
					buf[_i++] = (byte)('0' + num % 10);
					num /= 10;
				} while (num != 0);

				// Reverse the number
				for (int offs = (_i - start) / 2 - 1; offs >= 0; offs--)
					G.Swap(ref buf[_i - offs - 1], ref buf[start + offs]);
			}

			// calls GetNextBuf and writes a quoted string into the returned buffer
			Span<byte> WriteString(ReadOnlySpan<char> s, int reserveExtra = 0)
			{
				int s_len = GetLengthAsBytes(s, _optWrite.EscapeUnicode);
				Span<byte> buf = GetNextBuf(s_len + 2 + reserveExtra);
				WriteStringCore(buf, s, s_len, ref _i, _optWrite.EscapeUnicode);
				return buf;
			}

			// writes a quoted string into buf at _i
			internal static void WriteStringCore(Span<byte> buf, ReadOnlySpan<char> s, int s_len, ref int _i, bool escapeUnicode)
			{
				buf[_i++] = (byte) '"';
				if (s_len == s.Length) {
					// Same length in bytes as in chars implies pure ASCII with nothing to escape
					#if NETSTANDARD2_0 || NETFRAMEWORK
					// (.NET Standard 2.0 lacks the fast span overloads of Encoding methods)
					for (int i = 0; i < s.Length; i++)
						buf[_i++] = (byte)s[i];
					#else
					Encoding.ASCII.GetBytes(s, buf.Slice(_i)); // vectorized on .NET Core 3+
					_i += s.Length;
					#endif
				} else {
					for (int i = 0; i < s.Length; i++) {
						int c = s[i];
						if (c <= 31) {
							buf[_i++] = (byte)'\\';
							switch (c) {
								case '\t': buf[_i++] = (byte)'t'; break;
								case '\n': buf[_i++] = (byte)'n'; break;
								case '\r': buf[_i++] = (byte)'r'; break;
								case '\b': buf[_i++] = (byte)'b'; break;
								case '\f': buf[_i++] = (byte)'f'; break;
								default: FinishEscape(buf, c, ref _i); break;
							}
						} else if (c <= 127) {
							if (c == '\\' || c == '"')
								buf[_i++] = (byte)'\\';
							buf[_i++] = (byte)c;
						} else if (escapeUnicode) {
							buf[_i++] = (byte)'\\';
							FinishEscape(buf, c, ref _i);
						} else if (c <= 0x07FF) {
							buf[_i++] = (byte)(0xC0 | (c >> 6));
							buf[_i++] = (byte)(0x80 | (c & 0x3F));
						} else if (c >= 0xD800 && c <= 0xDFFF) {
							if (c < 0xDC00 && i + 1 < s.Length && s[i + 1] >= 0xDC00 && s[i + 1] <= 0xDFFF) {
								// valid surrogate pair
								c = ((c & 0x3FF) << 10) + (s[++i] & 0x3FF) + 0x10000;
								buf[_i++] = (byte)(0xF0 | (c >> 18));
								buf[_i++] = (byte)(0x80 | (c >> 12) & 0x3F);
								buf[_i++] = (byte)(0x80 | (c >> 6) & 0x3F);
								buf[_i++] = (byte)(0x80 | (c & 0x3F));
							} else {
								// always escape unpaired surrogate characters. This is required because
								// Encoding.UTF8 refuses to decode the UTF-8 form of such characters.
								buf[_i++] = (byte)'\\';
								FinishEscape(buf, c, ref _i);
							}
						} else {
							// other BMP character
							buf[_i++] = (byte)(0xE0 | (c >> 12));
							buf[_i++] = (byte)(0x80 | (c >> 6) & 0x3F);
							buf[_i++] = (byte)(0x80 | (c & 0x3F));
						}
					}
				}
				buf[_i++] = (byte) '"';
			}

			static void FinishEscape(Span<byte> buf, int c, ref int _i)
			{
				buf[_i++] = (byte)'u';
				buf[_i++] = (byte)PrintHelpers.HexDigitChar(c >> 12);
				buf[_i++] = (byte)(PrintHelpers.HexDigitChar((c >> 8) & 0xF));
				buf[_i++] = (byte)(PrintHelpers.HexDigitChar((c >> 4) & 0xF));
				buf[_i++] = (byte)PrintHelpers.HexDigitChar(c & 0xF);
			}

			internal static int GetLengthAsBytes(ReadOnlySpan<char> s, bool escapeUnicode)
			{
				#if NET8_0_OR_GREATER
				// Vectorized fast path for the overwhelmingly common case: printable
				// ASCII with nothing to escape means the byte length equals the char
				// length. Two SIMD passes beat one scalar pass over the whole string.
				// Inactive until a net8.0 target is added.
				if (!s.ContainsAnyExceptInRange(' ', '~') && !s.ContainsAny('\\', '"'))
					return s.Length;
				#endif
				int len = s.Length;
				for (int i = 0; i < s.Length; i++) {
					var c = s[i];
					if (c <= 31) {
						// Amazingly, \0 is not supported in JSON. Facepalm.
						len += (c == '\t' || c == '\n' || c == '\r' || c == '\b' || c == '\f' ? 1 : 5);
					} else if (c > 127) {
						if (escapeUnicode)
							len += 5;
						else if (c <= 0x07FF)
							len += 1;
						else if (c >= 0xD800 && c <= 0xDFFF) {
							if (c < 0xDC00 && i + 1 < s.Length && s[i + 1] >= 0xDC00 && s[i + 1] <= 0xDFFF) {
								len += 3; // valid surrogate pair (4 bytes)
								i++;
							} else {
								len += 5; // lone surrogate: WriteStringCore escapes it (\uXXXX)
							}
						} else
							len += 2; // other BMP character (3 bytes)
					} else if (c == '\\' || c == '"') {
						len += 1;
					}
				}
				return len;
			}
		}
	}
}
