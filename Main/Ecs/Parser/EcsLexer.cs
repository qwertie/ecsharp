using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Threading;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Syntax.Les;
using System.Numerics;

namespace Loyc.Ecs.Parser
{
	using TT = TokenType;

	/// <summary>Lexer for EC# source code (see <see cref="ILexer{Token}"/>).</summary>
	/// <seealso cref="TokensToTree"/>
	public partial class EcsLexer : BaseLexer, ILexer<Token>
	{
		public EcsLexer(string text, IMessageSink sink) : base(new UString(text), "") { ErrorSink = sink; }
		public EcsLexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) : base(text, fileName, startPosition) { ErrorSink = sink; }

		public bool AllowNestedComments = false;
		private bool _parseNeeded, _verbatim;
		// Alternate: hex numbers, verbatim strings
		// UserFlag: bin numbers, double-verbatim
		private NodeStyle _style;
		private TokenType _type; // predicted type of the current token
		private object _value; // if !_textValue.IsNull, this must be the type marker of a literal
		protected UString _textValue;
		private int _startPosition;
		// _allowPPAt is used to detect whether a preprocessor directive is allowed
		// at the current input position. When _allowPPAt==_startPosition, it's allowed.
		private int _allowPPAt;

		public void Reset(ICharSource source, string fileName = "", int inputPosition = 0)
		{
			base.Reset(source, fileName, inputPosition, true);
		}

		public new ISourceFile SourceFile { get { return base.SourceFile; } }

		int _indentLevel;
		UString _indent;
		public int IndentLevel { get { return _indentLevel; } }
		public UString IndentString { get { return _indent; } }
		public int SpacesPerTab = 4;

		public Maybe<Token> NextToken()
		{
			int la1;
			if (LA0 == '\t' || LA0 == ' ')
				Spaces();
			else if (LA0 == '.' && InputPosition == _lineStartAt && ((la1 = LA(1)) == '\t' || la1 == ' '))
				DotIndent();
			_startPosition = InputPosition;
			_value = null;
			_textValue = default;
			_style = 0;

			if (InputPosition >= CharSource.Count)
				return Maybe<Token>.NoValue;
			else {
				Token();
				Debug.Assert(InputPosition > _startPosition);
				return new Token((int)_type, _startPosition, Text(), _style, _value, _textValue);
			}
		}

		protected override void Error(int lookaheadIndex, string message)
		{
			// the fast "blitting" code path may not be able to handle errors
			_parseNeeded = true;

			var pos = new SourceRange(SourceFile, InputPosition + lookaheadIndex);
			if (ErrorSink != null)
				ErrorSink.Error(pos, message);
			else
				throw new FormatException(pos + ": " + message);
		}
				
		public void Restart()
		{
			_indentLevel = 0;
			_lineNumber = 0;
			_allowPPAt = _lineStartAt = 0;
		}

		#region String unescaping & identifier parsing
		// After the generated lexer code determines the boundaries of the token, 
		// one of these methods extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		#region String parsing

		static Symbol _c = (Symbol)"c"; // Standard TypeMarker for a character

		void ParseSQStringValue()
		{
			int len = InputPosition - _startPosition;
			if (!_parseNeeded && len == 3) {
				_value = CG.Cache(CharSource[_startPosition + 1]);
			} else {
				UString s = UnescapeQuotedString();
				if (s.Length == 1)
					_value = CG.Cache(s[0]);
				else {
					_value = _c;
					_textValue = s;
					if (s.Length == 0)
						Error(_startPosition - InputPosition, "Empty character literal".Localized());
					else
						Error(_startPosition - InputPosition, "Character literal has {0} characters (there should be exactly one)".Localized(s.Length));
				}
			}
		}

		UString Text() => CharSource.Slice(_startPosition, InputPosition - _startPosition);

		UString UnescapeQuotedString() => UnescapeQuotedString(_startPosition);

		UString UnescapeQuotedString(int start)
		{
			Debug.Assert(_verbatim == (CharSource[start] == '@'));
			if (_verbatim)
				start++;
			char q;
			Debug.Assert((q = CharSource.TryGet(start, '\0')) == '"' || q == '\'' || q == '`');
			bool tripleQuoted = (_style & NodeStyle.BaseStyleMask) == NodeStyle.TDQStringLiteral ||
			                    (_style & NodeStyle.BaseStyleMask) == NodeStyle.TQStringLiteral;

			if (!_parseNeeded) {
				Debug.Assert(!tripleQuoted);
				return CharSource.Slice(start + 1, InputPosition - start - 2);
			} else {
				UString original = CharSource.Slice(start, InputPosition - start);
				return UnescapeQuotedString(ref original, _verbatim, Error, _indent);
			}
		}

		static string UnescapeQuotedString(ref UString source, bool isVerbatim, Action<int, string> onError, UString indentation)
		{
			Debug.Assert(source.Length >= 1);
			if (isVerbatim) {
				bool fail;
				char stringType = (char)source.PopFirst(out fail);
				StringBuilder sb = new StringBuilder();
				int c;
				for (;;) {
					c = source.PopFirst(out fail);
					if (fail) break;
					if (c == stringType) {
						if ((c = source.PopFirst(out fail)) != stringType)
							break;
					}
					sb.Append((char)c);
				}
				return sb.ToString();
			} else {
				// triple-quoted or normal string: let LES lexer handle it
				return Les3Lexer.UnescapeQuotedString(ref source, onError, indentation, true, false);
			}
		}

		#endregion

		#region Identifier & Symbol parsing (including public ParseIdentifier())

		// id & symbol cache. For Symbols, includes only one of the two @ signs.
		protected Dictionary<UString, Symbol> _idCache = new Dictionary<UString, Symbol>();

		Symbol ParseIdValue(int skipAtSign, bool isBQString)
			=> ParseIdOrSymbol(_startPosition + skipAtSign, isBQString);
		Symbol ParseSymbolValue(bool isBQString)
			=> ParseIdOrSymbol(_startPosition + 2, isBQString);

		Symbol ParseIdOrSymbol(int start, bool isBQString)
		{
			UString unparsed = CharSource.Slice(start, InputPosition - start);
			UString parsed;
			Debug.Assert(isBQString == (CharSource.TryGet(start, '\0') == '`'));
			Debug.Assert(!_verbatim);
			if (_idCache.TryGetValue(unparsed, out Symbol value))
				return value;
			else {
				if (isBQString)
					parsed = UnescapeQuotedString(start);
				else if (_parseNeeded)
					parsed = ScanNormalIdentifier(unparsed);
				else
					parsed = unparsed;
				return _idCache[unparsed.ShedExcessMemory(50)] = (Symbol) parsed;
			}
		}

		static string ScanNormalIdentifier(UString text)
		{
			var parsed = new StringBuilder();
			char c;
			while ((c = text[0, '\0']) != '\0') {
				if (!ScanUnicodeEscape(ref text, parsed, c)) {
					parsed.Append(c);
					text = text.Slice(1);
				}
			}
			return parsed.ToString();
		}
		static bool ScanUnicodeEscape(ref UString text, StringBuilder parsed, char c)
		{
			// I can't imagine why this exists in C# in the first place. Unicode 
			// escapes inside identifiers are required to be letters or digits,
			// although my lexer doesn't enforce this (EC# needs no such rule.)
			if (c != '\\')
				return false;
			char u = text.TryGet(1, '\0');
			int len = 4;
			if (u == 'u' || u == 'U') {
				if (u == 'U') len = 8;
				if (text.Length < 2 + len)
					return false;

				var digits = text.Substring(2, len);
				int code;
				if (ParseHelpers.TryParseHex(digits, out code) && code <= 0x0010FFFF) {
					if (code >= 0x10000) {
						parsed.Append((char)(0xD800 + ((code - 0x10000) >> 10)));
						parsed.Append((char)(0xDC00 + ((code - 0x10000) & 0x3FF)));
					} else
						parsed.Append((char)code);
					text = text.Substring(2 + len);
					return true;
				}
			}
			return false;
		}

		#endregion

		#endregion

		// Due to the way generics are implemented, repeating the implementation 
		// of this base-class method might improve performance (TODO: verify this idea)
		new protected int LA(int i)
		{
			bool fail;
			char result = CharSource.TryGet(InputPosition + i, out fail);
			return fail ? -1 : result;
		}

		int MeasureIndent(UString indent)
		{
			return MeasureIndent(indent, SpacesPerTab);
		}
		public static int MeasureIndent(UString indent, int spacesPerTab)
		{
			int amount = 0;
			for (int i = 0; i < indent.Length; i++)
			{
				char ch = indent[i];
				if (ch == '\t') {
					amount += spacesPerTab;
					amount -= amount % spacesPerTab;
				} else if (ch == '.' && i + 1 < indent.Length) {
					amount += spacesPerTab;
					amount -= amount % spacesPerTab;
					i++;
				} else
					amount++;
			}
			return amount;
		}

		Maybe<Token> _current;

		void IDisposable.Dispose() {}
		Token IEnumerator<Token>.Current { get { return _current.Value; } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		bool System.Collections.IEnumerator.MoveNext()
		{
			_current = NextToken();
			return _current.HasValue;
		}
	}

}
