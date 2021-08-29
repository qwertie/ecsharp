using Loyc.Collections.Impl;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using Loyc.Syntax;
using System;
using System.Buffers;
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
		internal partial class WriterStateBase
		{
			protected IBufferWriter<byte> _output;
			//protected Memory<byte> _buf; // a sub-buffer returned from _output
			protected int _i = 0; // next index within _out to write

			protected ObjectIDGenerator _idGen = new ObjectIDGenerator(); // IDs start at one
			//SyncTypeRegistry _typeRegistry;
			//static Dictionary<int, object> _idTable;
			
			public WriterStateBase(IBufferWriter<byte> output) => _output = output;
			protected Span<byte> GetOutBuf(int requiredBytes)
			{
				Flush();
				return _output.GetMemory(requiredBytes).Span;
			}
			internal void Flush()
			{
				_output.Advance(_i);
				_i = 0;
			}
		}

		/// <summary>The core logic for writing JSON data in UTF-8 format</summary>
		internal partial class WriterState : WriterStateBase
		{
			internal Options _opt;
			internal Options.ForWriter _optWrite;
			internal bool _isInsideList = true;
			internal InternalList<SubObjectMode> _stack = InternalList<SubObjectMode>.Empty;
			internal byte[] _indent;
			internal byte[] _newline;
			internal byte _pendingComma;
			internal int _compactMode;
			
			public WriterState(IBufferWriter<byte> output, Options options) : base(output) {
				_opt = options;
				_optWrite = _opt.Write;
				_indent = Encoding.UTF8.GetBytes(_optWrite.Indent);
				_newline = Encoding.UTF8.GetBytes(_optWrite.Newline);
			}

			// Writes the pending comma/newline, if any, and gets a Span for writing.
			Span<byte> GetNextBuf(int requiredBytes)
			{
				if (_pendingComma != 0) {
					var buf = base.GetOutBuf(requiredBytes + 1 + NewlineSize);
					if (_pendingComma != '\n')
						buf[_i++] = _pendingComma;
					MaybeNewlineWithIndent(buf);
					_pendingComma = 0;
					return buf;
				}
				return base.GetOutBuf(requiredBytes);
			}

			public (bool Begun, object? Object) BeginSubObject(string? name, object? childKey, SubObjectMode mode)
			{
				if (childKey == null && MayBeNullable(mode)) {
					WriteNull(name);
					return (false, childKey);
				}

				var buf = BeginProp(name, 25); // Reserve extra bytes for refs: {"$ref":"12345678901"}

				if ((mode & SubObjectMode.Deduplicate) != 0) {
					long id = _idGen.GetId(childKey, out bool firstTime);
					if (!firstTime) {
						WriteBackReference(buf, id);
						return (false, childKey);
					} else {
						OpenBraceOrBrack(mode & ~SubObjectMode.List);
						if (_opt.NewtonsoftCompatibility)
							WriteProp("$id", id.ToString());
						else
							WriteProp("\f", id);
						if ((mode & SubObjectMode.List) != 0) {
							string valuesProp = _opt.NewtonsoftCompatibility ? "$values" : "";
							BeginProp(valuesProp, 10);
							OpenBraceOrBrack(mode);
						}
						return (true, childKey);
					}
				} else {
					OpenBraceOrBrack(mode);
				}

				if ((mode & SubObjectMode.Compact) != 0)
					_compactMode++;
				return (true, childKey);
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

			void OpenBraceOrBrack(SubObjectMode mode)
			{
				_stack.Add(mode);

				var buf = GetNextBuf(1 + NewlineSize);

				_isInsideList = (mode & SubObjectMode.List) != 0;

				buf[_i++] = (byte) (_isInsideList ? '[' : '{');

				// GetNextBuf() understands this as a request for a newline + indentation
				_pendingComma = (byte) '\n';
			}

			void CloseBraceOrBrack()
			{
				var mode = _stack.Last;
				if ((mode & SubObjectMode.Compact) != 0)
					_compactMode--;
				
				// This will cause an unindent, since it is done before GetNextBuf() which writes the newline/indent
				_stack.Pop();

				// Cancel ',' at end of list/object; just make a newline. If _pendingComma
				// is '\n', the object/list is empty and we don't even need a newline.
				_pendingComma = (byte) (_pendingComma == '\n' ? 0 : '\n');

				bool isList = (mode & SubObjectMode.List) != 0;
				if (isList && (mode & SubObjectMode.Deduplicate) != 0)
				{
					Debug.Assert(_stack.Last == (mode & ~SubObjectMode.List));
					
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

				_pendingComma = (byte) ',';

				if (_stack.IsEmpty) {
					Flush();
					_isInsideList = true;
				} else {
					_isInsideList = (_stack.Last & SubObjectMode.Tuple) != 0;
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
						#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
						WriteProp(propName, System.Convert.ToBase64String(value.ToArray()));
						#else
						WriteProp(propName, System.Convert.ToBase64String(value));
						#endif
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
				WriteLiteralProp(propName, num.ToString(CultureInfo.InvariantCulture));
				return num;
			}
			public double WriteProp(string? propName, double num)
			{
				WriteLiteralProp(propName, num.ToString(CultureInfo.InvariantCulture));
				return num;
			}
			public decimal WriteProp(string? propName, decimal num)
			{
				WriteLiteralProp(propName, num.ToString(CultureInfo.InvariantCulture));
				return num;
			}
			public void WriteNull(string? propName) => WriteLiteralProp(propName, _null);
			public void WriteLiteralProp(string? propName, byte[] literal)
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
					WriteProp(propName, c.ToString());
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
						propName = _opt.NameConverter(propName ?? "");
					buf = WriteString(propName.AsSpan(), 3 + reserveExtra);
					buf[_i++] = (byte) ':';
					if (_optWrite.SpaceAfterColon && _compactMode == 0)
						buf[_i++] = (byte) ' ';
				}
				return buf;
			}

			void Blurt(Span<byte> buf, byte[] bytes)
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
					for (int i = 0; i < s.Length; i++)
						buf[_i++] = (byte)s[i];
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
						} else if (c < 127) {
							if (c == '\\' || c == '"')
								buf[_i++] = (byte)'\\';
							buf[_i++] = (byte)c;
						} else if (c <= 0x9F || escapeUnicode) {
							buf[_i++] = (byte)'\\';
							FinishEscape(buf, c, ref _i);
						} else if (c <= 0x07FF) {
							buf[_i++] = (byte)(0xC0 | (c >> 6));
							buf[_i++] = (byte)(0x80 | (c & 0x3F));
						} else if (c < 0xD800 || c > 0xDBFF || i + 1 >= s.Length || s[i + 1] < 0xDC00 || s[i + 1] > 0xDFFF) {
							buf[_i++] = (byte)(0xE0 | (c >> 12));
							buf[_i++] = (byte)(0x80 | (c >> 6) & 0x3F);
							buf[_i++] = (byte)(0x80 | (c & 0x3F));
						} else { // valid surrogate pair
							c = ((c & 0x3FF) << 10) + (s[i + 1] & 0x3FF) + 0x10000;
							buf[_i++] = (byte)(0xF0 | (c >> 18));
							buf[_i++] = (byte)(0x80 | (c >> 12) & 0x3F);
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
				int len = s.Length;
				for (int i = 0; i < s.Length; i++) {
					var c = s[i];
					if (c <= 31) {
						// Amazingly, \0 is not supported in JSON. Facepalm.
						len += (c == '\t' || c == '\n' || c == '\r' || c == '\b' || c == '\f' ? 1 : 5);
					} else if (c >= 127) {
						if (c <= 0x9F || escapeUnicode)
							len += 5;
						else if (c <= 0x07FF)
							len += 1;
						else if (c < 0xD800 || c > 0xDBFF || i + 1 >= s.Length || s[i + 1] < 0xDC00 || s[i + 1] > 0xDFFF)
							len += 2;
						else // valid surrogate pair
							len += 3;
					} else if (c == '\\' || c == '"') {
						len += 1;
					}
				}
				return len;
			}
		}
	}
}
