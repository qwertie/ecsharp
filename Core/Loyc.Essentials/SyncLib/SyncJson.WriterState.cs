using Loyc.Collections.Impl;
using Loyc.Compatibility;
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
			protected IBufferWriter<byte> _output = new ArrayBufferWriter<byte>(1024);
			protected Memory<byte> _buf; // a sub-buffer returned from _output
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
			protected void Flush()
			{
				_output.Advance(_i);
				_i = 0;
			}
		}

		internal partial class WriterState : WriterStateBase
		{
			internal bool _isInsideList = true;
			internal InternalList<SubObjectMode> _stack = InternalList<SubObjectMode>.Empty;
			internal byte[] _indent;
			internal byte[] _newline;
			internal byte _pendingComma;
			internal Options _opt;
			internal int _compactMode;
			
			public WriterState(IBufferWriter<byte> output, Options options) : base(output) {
				_opt = options;
				_indent = Encoding.UTF8.GetBytes(_opt.Indent);
				_newline = Encoding.UTF8.GetBytes(_opt.Newline);
			}

			// Writes the pending comma/newline, if any, and gets a Span for writing.
			Span<byte> GetNextBuf(int requiredBytes)
			{
				if (_pendingComma != 0) {
					var buf = base.GetOutBuf(requiredBytes + 1 + NewlineSize);
					buf[_i++] = _pendingComma;
					MaybeNewlineWithIndent();
					_pendingComma = 0;
					return buf;
				}
				return base.GetOutBuf(requiredBytes);
			}

			public (bool Begun, object? Object) BeginSubObject(string name, object? childKey, SubObjectMode mode, int listLength)
			{
				if (childKey == null && (mode & (SubObjectMode.Deduplicate | SubObjectMode.NotNull)) != SubObjectMode.NotNull) {
					WriteNull(name);
					return (false, childKey);
				}

				BeginProp(name, 25); // Reserve extra bytes for refs: {"$ref":12345678901}

				if ((mode & SubObjectMode.Deduplicate) != 0) {
					long id = _idGen.GetId(childKey, out bool firstTime);
					if (!firstTime) {
						WriteBackReference(_buf.Span, id);
						return (false, childKey);
					} else {
						OpenBraceOrBrack(mode);
						if (_opt.NewtonsoftCompatibility)
							WriteProp("$id", id.ToString());
						else
							WriteProp("\r", id);
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
					buf[_i++] = (byte) '"';
					WriteNumber(buf, id, true);
					buf[_i++] = (byte) '"';
				} else {
					buf[_i++] = (byte) '\\';
					buf[_i++] = (byte) 'r';
					buf[_i++] = (byte) '"';
					buf[_i++] = (byte) ':';
					WriteNumber(buf, id, true);
				}
				buf[_i++] = (byte) '}';
			}

			public void EndSubObject()
			{
				CloseBraceOrBrack();
			}

			void OpenBraceOrBrack(SubObjectMode mode)
			{
				_stack.Add(mode);
				_isInsideList = (mode & SubObjectMode.Tuple) != 0;

				var buf = GetNextBuf(1 + NewlineSize);
				buf[_i++] = (byte) (_isInsideList ? '[' : '{');
				MaybeNewlineWithIndent();
			}

			void CloseBraceOrBrack()
			{
				if ((_stack.Last & SubObjectMode.Compact) != 0)
					_compactMode--;
				_stack.Pop();
				
				var buf = GetNextBuf(NewlineSize + 1);
				MaybeNewlineWithIndent();
				buf[_i++] = (byte) (_isInsideList ? ']' : '}');

				_isInsideList = _stack.IsEmpty ? true : (_stack.Last & SubObjectMode.Tuple) != 0;
				_pendingComma = (byte) ',';
			}

			int NewlineSize => _newline.Length + _indent.Length * _stack.Count;

			void MaybeNewlineWithIndent()
			{
				if (_newline.Length != 0 && _compactMode == 0) {
					Blurt(_newline);
					for (int i = 0, count = System.Math.Min(_stack.Count, _opt.MaxIndentDepth); i < count; i++)
						Blurt(_indent);
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
					int valueLen = GetLengthAsBytes(value);
					Span<byte> buf = BeginProp(propName, valueLen);
					WriteStringCore(buf, value, valueLen);
					_pendingComma = (byte)',';
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
			public void WriteNull(string? propName) => WriteLiteralProp(propName, _null);
			public void WriteLiteralProp(string? propName, byte[] literal)
			{
				Span<byte> buf = BeginProp(propName, literal.Length);
				Blurt(literal);
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
					buf = WriteString(propName.AsSpan(), 1 + reserveExtra);
					buf[_i++] = (byte) ':';
				}
				return buf;
			}

			void Blurt(byte[] bytes) => bytes.CopyTo(_buf.Slice(_i));

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
				int s_len = GetLengthAsBytes(s);
				Span<byte> buf = GetNextBuf(s_len + reserveExtra);
				return WriteStringCore(buf, s, s_len);
			}

			// writes a quoted string into buf at _i
			Span<byte> WriteStringCore(Span<byte> buf, ReadOnlySpan<char> s, int s_len)
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
								default: FinishEscape(buf, c); break;
							}
						} else if (c < 127) {
							buf[_i++] = (byte)c;
						} else if (c <= 0x9F || _opt.EscapeUnicode) {
							buf[_i++] = (byte)'\\';
							FinishEscape(buf, c);
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
				return buf;
			}

			void FinishEscape(Span<byte> buf, int c)
			{
				buf[_i++] = (byte)'u';
				buf[_i++] = (byte)PrintHelpers.HexDigitChar(c >> 12);
				buf[_i++] = (byte)(PrintHelpers.HexDigitChar(c >> 8) & 0xF);
				buf[_i++] = (byte)(PrintHelpers.HexDigitChar(c >> 4) & 0xF);
				buf[_i++] = (byte)PrintHelpers.HexDigitChar(c & 0xF);
			}

			int GetLengthAsBytes(ReadOnlySpan<char> s)
			{
				int len = s.Length;
				for (int i = 0; i < s.Length; i++) {
					var c = s[i];
					if (c <= 31) {
						// Amazingly, \0 is not supported in JSON. Facepalm.
						len += (c == '\t' || c == '\n' || c == '\r' || c == '\b' || c == '\f' ? 1 : 5);
					} else if (c >= 127) {
						if (c <= 0x9F || _opt.EscapeUnicode)
							len += 5;
						else if (c <= 0x07FF)
							len += 1;
						else if (c < 0xD800 || c > 0xDBFF || i + 1 >= s.Length || s[i + 1] < 0xDC00 || s[i + 1] > 0xDFFF)
							len += 2;
						else // valid surrogate pair
							len += 3;
					}
				}
				return len;
			}
		}
	}
}
