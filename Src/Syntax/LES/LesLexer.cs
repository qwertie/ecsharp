using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Syntax;
using Loyc.LLParserGenerator;
using Loyc.Threading;
using Loyc.Collections;
using uchar = System.Int32;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using System.Globalization;

	public enum TokenType
	{
		Spaces     = TokenKind.Spacer,
		Newline    = TokenKind.Spacer + 1,
		SLComment  = TokenKind.Spacer + 2,
		MLComment  = TokenKind.Spacer + 3,
		Id         = TokenKind.Id,
		Number     = TokenKind.Number,
		String     = TokenKind.Literal,
		SQString   = TokenKind.Literal + 1,
		BQString   = TokenKind.Literal + 2,
		Symbol     = TokenKind.Literal + 3,
		OtherLit   = TokenKind.Literal + 4, // true, false, null
		Dot        = TokenKind.Dot,
		Assignment = TokenKind.Assignment,
		NormalOp   = TokenKind.Operator,
		PreSufOp   = TokenKind.Operator + 1,  // ++, --, $
		SuffixOp   = TokenKind.Operator + 2,  // \\...
		Colon      = TokenKind.Operator + 3,
		At         = TokenKind.Operator + 4,
		Comma      = TokenKind.Separator,
		Semicolon  = TokenKind.Separator + 1,
		LParen     = TokenKind.Parens + 1,
		RParen     = TokenKind.Parens + 2,
		LBrack     = TokenKind.Bracks + 1,
		RBrack     = TokenKind.Bracks + 2,
		LBrace     = TokenKind.Braces + 1,
		RBrace     = TokenKind.Braces + 2,
		OpenOf     = TokenKind.OtherGroup + 1,
		Indent     = TokenKind.OtherGroup + 2,
		Dedent     = TokenKind.OtherGroup + 3,
		Shebang    = TokenKind.Other + 1,
	}

	public interface ILexer
	{
		/// <summary>The file being lexed.</summary>
		ISourceFile Source { get; }
		/// <summary>Scans and returns information about the next token.</summary>
		Token? NextToken();
		/// <summary>Event handler for errors.</summary>
		Action<int, string> OnError { get; set; }
		/// <summary>Indentation level of the current line. This is updated after 
		/// scanning the first whitespaces on a new line, and may be reset to zero 
		/// when <see cref="NextToken()"/> returns a newline.</summary>
		int IndentLevel { get; }
		/// <summary>Current line number (1 for the first line).</summary>
		int LineNumber { get; }
		/// <summary>Restart lexing from beginning of <see cref="Source"/>.</summary>
		void Restart();
	}
	
	/// <summary>Lexer for EC# source code (see <see cref="ILexer"/>).</summary>
	/// <seealso cref="WhitespaceFilter"/>
	/// <seealso cref="TokensToTree"/>
	public partial class LesLexer : BaseLexer<StringCharSourceFile>, ILexer
	{
		public LesLexer(string text, Action<int, string> onError) : base(new StringCharSourceFile(text, "")) { OnError = onError; }
		public LesLexer(StringCharSourceFile file, Action<int, string> onError) : base(file) { OnError = onError; }

		public bool AllowNestedComments = false;
		private bool _isFloat, _parseNeeded, _isNegative;
		// Alternate: hex numbers, verbatim strings
		// UserFlag: bin numbers, double-verbatim
		private NodeStyle _style;
		private int _numberBase;
		private Symbol _typeSuffix;
		private TokenType _type; // predicted type of the current token
		private object _value;
		private int _startPosition;
		// _allowPPAt is used to detect whether a preprocessor directive is allowed
		// at the current input position. When _allowPPAt==_startPosition, it's allowed.
		private int _allowPPAt, _lineStartAt;

		ISourceFile ILexer.Source { get { return CharSource; } }
		public StringCharSourceFile Source { get { return CharSource; } }
		public Action<int, string> OnError { get; set; }

		int _indentLevel, _lineNumber;
		public int IndentLevel { get { return _indentLevel; } }
		public int LineNumber { get { return _lineNumber; } }
		public int SpacesPerTab = 4;

		protected override void Error(string message)
		{
			_parseNeeded = true; // don't use the "fast" code path
			Error(InputPosition, message);
		}
		protected void Error(int index, string message)
		{
			if (OnError != null)
				OnError(index, message);
			else
				throw new FormatException(message);
		}

		
		public void Restart()
		{
			_indentLevel = 0;
			_lineNumber = 0;
			_allowPPAt = _lineStartAt = 0;
		}

		public Token? NextToken()
		{
			_startPosition = InputPosition;
			_value = null;
			_style = 0;
			if (InputPosition >= CharSource.Count)
				return null;
			else {
				Token();
				Debug.Assert(InputPosition > _startPosition);
				return new Token(_type, _startPosition, InputPosition - _startPosition, _style, _value);
			}
		}

		#region Post-scan parsers
		// After the generated lexer code determines the boundaries of the token, 
		// one of these methods extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		void ParseIdValue()
		{
			UString id;
			if (_parseNeeded) {
				var original = CharSource.Substring(_startPosition, InputPosition - _startPosition);
				id = ParseIdentifier(ref original, Error);
				Debug.Assert(original.IsEmpty);
			} else
				id = CharSource.Text.USlice(_startPosition, InputPosition - _startPosition);

			_value = IdToSymbol(id);
		}

		void ParseSymbolValue()
		{
			Debug.Assert(CharSource[_startPosition] == '@' && CharSource[_startPosition + 1] == '@');
			UString original = CharSource.Substring(_startPosition + 2, InputPosition - _startPosition - 2);
			if (_parseNeeded) {
				string text = UnescapeQuotedString(ref original, Error);
				Debug.Assert(original.IsEmpty);
				_value = IdToSymbol(text);
			} else if (original[0, '\0'] == '`')
				_value = IdToSymbol(original.Substring(1, original.Length - 2));
			else
				_value = IdToSymbol(original);
		}

		void ParseCharValue()
		{
			var sb = TempSB();
			int length;
			int c = -1;
			if (_parseNeeded) {
				UString original = CharSource.Substring(_startPosition, InputPosition - _startPosition);
				UnescapeQuotedString(ref original, Error, sb);
				Debug.Assert(original.IsEmpty);
				length = sb.Length;
				if (sb.Length == 1)
					c = sb[0];
				else
					_value = sb.ToString();
			} else {
				Debug.Assert(CharSource[InputPosition-1] == '\'' && CharSource[_startPosition] == '\'');
				length = InputPosition - _startPosition - 2;
				if (length == 1)
					c = CharSource[_startPosition + 1];
				else
					_value = CharSource.Substring(_startPosition + 1, InputPosition - _startPosition - 2).ToString();
			}
			if (c != -1)
				_value = CG.Cache((char)c);
			else if (length == 0)
				Error(_startPosition, Localize.From("Empty character literal"));
			else
				Error(_startPosition, Localize.From("Character literal has {0} characters (there should be exactly one)", length));
		}

		void ParseBQStringValue()
		{
			ParseStringCore(false);
			_value = IdToSymbol(_value.ToString());
		}

		void ParseStringValue(bool isTripleQuoted)
		{
			ParseStringCore(isTripleQuoted);
			if (_value.ToString().Length < 64)
				_value = CG.Cache(_value);
		}

		void ParseStringCore(bool isTripleQuoted)
		{
			if (_parseNeeded) {
				var original = CharSource.Substring(_startPosition, InputPosition - _startPosition);
				_value = UnescapeQuotedString(ref original, Error);
				Debug.Assert(original.IsEmpty);
			} else {
				Debug.Assert(CharSource.TryGet(InputPosition - 1, '?') == CharSource.TryGet(_startPosition, '!'));
				if (isTripleQuoted)
					_value = CharSource.Substring(_startPosition + 3, InputPosition - _startPosition - 6).ToString();
				else
					_value = CharSource.Substring(_startPosition + 1, InputPosition - _startPosition - 2).ToString();
			}
		}

		void ParseNormalOp()
		{
			_parseNeeded = false;
			ParseOp(false);
		}

		static Symbol _Backslash = GSymbol.Get(@"#\");

		void ParseBackslashOp()
		{
			ParseOp(true);
		}

		static Symbol _sub = GSymbol.Get("#-");
		static Symbol _F = GSymbol.Get("F");
		static Symbol _D = GSymbol.Get("D");
		static Symbol _M = GSymbol.Get("M");
		static Symbol _U = GSymbol.Get("U");
		static Symbol _L = GSymbol.Get("L");
		static Symbol _UL = GSymbol.Get("UL");

		void ParseNumberValue()
		{
			// Optimize the most common case: a one-digit integer
			if (InputPosition == _startPosition + 1) {
				Debug.Assert(char.IsDigit(CharSource[_startPosition]));
				_value = CG.Cache(CharSource[_startPosition] - '0');
				return;
			}

			if (_isFloat) {
				if (_numberBase == 10) {
					ParseFloatValue();
				} else {
					Debug.Assert(char.IsLetter(CharSource[_startPosition+1]));
					ParseSpecialFloatValue();
				}
			} else {
				ParseIntegerValue();
			}
		}

		private void ParseFloatValue()
		{
			string token = CharSource.Substring(_startPosition, InputPosition - _startPosition - _typeSuffix.Name.Length);
			token = token.Replace("_", "");
			if (_typeSuffix == _F) {
				float f;
				float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out f);
				_value = f;
			} else if (_typeSuffix == _M) {
				decimal m;
				decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out m);
				_value = m;
			} else {
				double d;
				double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
				_value = d;
			}
		}

		private void ParseIntegerValue()
		{
			// Some kind of integer
			int index = _startPosition;
			if (_isNegative)
				index++;
			if (_numberBase != 10) {
				Debug.Assert(char.IsLetter(CharSource[index + 1]));
				index += 2;
			}
			int len = InputPosition - _startPosition;

			// Parse the integer
			ulong unsigned;
			bool overflow = !G.TryParseAt(CharSource.Text, ref index, out unsigned, _numberBase, G.ParseFlag.SkipUnderscores);
			Debug.Assert(index == InputPosition - _typeSuffix.Name.Length);

			// If no suffix, automatically choose int, uint, long or ulong
			var suffix = _typeSuffix;
			if (suffix == GSymbol.Empty) {
				if (unsigned > long.MaxValue)
					suffix = _UL;
				else if (unsigned > uint.MaxValue)
					suffix = _L;
				else if (unsigned > int.MaxValue)
					suffix = _U;
			}

			if (_isNegative && (suffix == _U || suffix == _UL)) {
				// Oops, an unsigned number can't be negative, so treat 
				// '-' as a separate token and let the number be reparsed.
				InputPosition = _startPosition + 1;
				_type = TT.NormalOp;
				_value = CodeSymbols.Sub;
				return;
			}

			// Set _value to an integer of the appropriate type 
			if (suffix == _UL)
				_value = unsigned;
			else if (suffix == _U) {
				overflow = overflow || (uint)unsigned != unsigned;
				_value = (uint)unsigned;
			} else if (suffix == _L) {
				if (_isNegative) {
					overflow = overflow || -(long)unsigned > 0;
					_value = -(long)unsigned;
				} else {
					overflow = overflow || (long)unsigned < 0;
					_value = (long)unsigned;
				}
			} else {
				_value = _isNegative ? -(int)unsigned : (int)unsigned;
			}

			if (overflow)
				Error(_startPosition, Localize.From("Overflow in integer literal (the number is 0x{0:X} after binary truncation).", _value));
			return;
		}

		private void ParseSpecialFloatValue()
		{
			Error(_startPosition, "Support for hex and binary float constants is not yet implemented.");
			_value = double.NaN;
		}

		#endregion

		#region Parsing helper methods

		protected Dictionary<UString, Symbol> _idCache = new Dictionary<UString,Symbol>();
		Symbol IdToSymbol(UString ustr)
		{
			Symbol sym;
			if (!_idCache.TryGetValue(ustr, out sym)) {
				string str = ustr.ToString();
				_idCache[str] = sym = GSymbol.Get(str);
			}
			return sym;
		}

		protected Dictionary<UString, Pair<Symbol, TokenType>> _opCache = new Dictionary<UString, Pair<Symbol, TokenType>>();
		void ParseOp(bool backslashOp)
		{
			var original = CharSource.Substring(_startPosition, InputPosition - _startPosition);

			Pair<Symbol, TokenType> sym;
			if (_opCache.TryGetValue(original, out sym)) {
				_value = sym.A;
				_type = sym.B;
			} else {
				Debug.Assert(backslashOp == (original[0] == '\\'));
				// op will be the operator text without the initial backslash, if any:
				// && => &&, \foo => foo, \`foo` => foo, \\`foo` => \foo
				UString op = original;
				if (backslashOp)
				{
					if (original.Length == 1)
					{	
						// Just a single backslash is the "#\" operator
						_opCache[original.ToString()] = sym = Pair.Create(_Backslash, TT.NormalOp);
						_value = sym.A;
						_type = sym.B;
						return;
					}
					op = original.Substring(1);
					if (_parseNeeded)
					{
						var sb = TempSB();
						bool _;
						var quoted = original;
						if (quoted[0] != '`')
							sb.Append((char)quoted.PopFront(out _));
						UnescapeQuotedString(ref quoted, Error, sb);
						op = sb.ToString();
					}
				}

				string opStr = op.ToString();
				_type = GetOpType(opStr);
				if (!backslashOp)
					opStr = "#" + opStr;
				_opCache[opStr] = sym = Pair.Create(GSymbol.Get(opStr), _type);
				_value = sym.A;
			}
		}

		private TokenType GetOpType(string op)
		{
			Debug.Assert(op.Length > 0);
			if (op.Length >= 2 && ((op[0] == '+' && op[op.Length - 1] == '+') || (op[0] == '-' && op[op.Length - 1] == '-')))
				return TT.PreSufOp;
			if (op == ":")
				return TT.Colon;
			char last = op[op.Length - 1], first = op[0];
			if (first == '\\')
				return TT.SuffixOp;
			if (last == '$')
				return TT.PreSufOp;
			if (last == '.' && (op.Length == 1 || first != '.'))
				return TT.Dot;
			if (last == '=' && (op.Length == 1 || first != '!' && first != '='))
				return TT.Assignment;
			return TT.NormalOp;
		}

		[ThreadStatic]
		static StringBuilder _tempsb;
		static StringBuilder TempSB()
		{
			var sb = _tempsb;
			if (sb == null)
				_tempsb = sb = new StringBuilder();
			sb.Clear();
			return sb;
		}

		public static UString ParseIdentifier(ref UString source, Action<int, string> onError)
		{
			StringBuilder parsed = TempSB();

			UString start = source;
			bool fail;
			int c = source.PopFront(out fail);
			if (c == '@') {
				// expecting: (BQString | Star(Set("[0-9a-zA-Z_'#~!%^&*-+=|<>/?:.@$]") | IdExtLetter))
				c = source.PopFront(out fail);
				if (c == '`') {
					UnescapeString(ref source, (char)c, false, onError, parsed);
				} else {
					while (SpecialIdSet.Contains(c) || c >= 128 && char.IsLetter((char)c)) {
						parsed.Append((char)c);
						c = source.PopFront(out fail);
					}
				}
			} else {
				if (c == '#' && source[0, '\0'] == '`') {
					parsed.Append('#');
					source.PopFront(out fail);
					UnescapeString(ref source, '`', false, onError, parsed);
				} else if (IsIdStartChar(c)) {
					parsed.Append(c);
					for (;;) {
						c = source.PopFront(out fail);
						if (!IsIdContChar(c))
							break;
						parsed.Append((char)c);
					}
				}
			}
			return parsed.ToString();
		}

		static readonly IntSet SpecialIdSet = IntSet.Parse(@"[0-9a-zA-Z_'#~!%^&*\-+=|<>/\\?:.@$]");
		static readonly IntSet IdContSet = IntSet.Parse("[0-9a-zA-Z_']");
		static readonly IntSet OpContSet = IntSet.Parse("[\\~!%^&*-+=|<>/?:.@$]");

		public static bool IsIdStartChar(uchar c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= 0x80 && char.IsLetter((char)c); }
		public static bool IsIdContChar(uchar c) { return IsIdStartChar(c) || c >= '0' && c <= '9' || c == '\''; }
		public static bool IsOpContChar(char c) { return OpContSet.Contains(c); }
		public static bool IsSpecialIdChar(char c) { return SpecialIdSet.Contains(c); }

		public static string UnescapeQuotedString(ref UString sourceText, Action<int, string> onError)
		{
			var sb = new StringBuilder();
			UnescapeQuotedString(ref sourceText, onError, sb);
			return sb.ToString();
		}
		public static void UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, StringBuilder sb)
		{
			bool isTripleQuoted = false, fail;
			char quoteType = (char)sourceText.PopFront(out fail);
			if (sourceText[0, '\0'] == quoteType &&
				sourceText[1, '\0'] == quoteType) {
				sourceText = sourceText.Substring(2);
				isTripleQuoted = true;
			}
			if (!UnescapeString(ref sourceText, quoteType, isTripleQuoted, onError, sb))
				onError(sourceText.InternalStart, Localize.From("End-of-file in string literal"));
		}
		public static bool UnescapeString(ref UString sourceText, char quoteType, bool isTripleQuoted, Action<int, string> onError, StringBuilder sb)
		{
			Debug.Assert(quoteType == '"' || quoteType == '\'' || quoteType == '`');
			bool fail;
			for (;;) {
				if (sourceText.IsEmpty)
					return false;
				if (!isTripleQuoted) {
					int i0 = sourceText.InternalStart;
					char c = G.UnescapeChar(ref sourceText);
					if (c == quoteType && sourceText.InternalStart == i0 + 1)
						return true; // end of string
					if (c == '\\' && sourceText.InternalStart == i0 + 1) {
						// This backslash was ignored by UnescapeChar
						onError(i0, Localize.From(@"Unrecognized escape sequence '\{0}' in string", G.EscapeCStyle(sourceText[0, ' '].ToString(), EscapeC.Control)));
					}
					sb.Append(c);
				} else {
					int c = sourceText.PopFront(out fail);
					if (c == quoteType) {
						if (sourceText[0, '\0'] == quoteType &&
							sourceText[1, '\0'] == quoteType) {
							sourceText = sourceText.Substring(2);
							// end of string
							return true;
						}
					} else if (c == '\\' && sourceText[0, '\0'] == '\\') {
						// Triple-quoted strings support the following escape sequences:
						//   \\\, \\n, \\r, \\", \\'
						// If two backslashes are followed by anything else, they are 
						// simply interpreted as two backslashes.
						char c1 = sourceText[1, '\0'];
						if (c1 == '\\' || c1 == 'n' || c1 == 'r' || c1 == '"' || c1 == '\'') {
							sourceText = sourceText.Substring(2); // skip
							if (c1 == 'n')
								c = '\n';
							else if (c1 == 'r')
								c = '\r';
							else
								c = c1;
						}
					}
					sb.Append(c);
				}
			}
		}

		#endregion

		// Due to the way generics are implemented, repeating the implementation 
		// of this base-class method might improve performance (TODO: verify this idea)
		new protected int LA(int i)
		{
			bool fail = false;
			char result = CharSource.TryGet(InputPosition + i, ref fail);
			return fail ? -1 : result;
		}

		private int MeasureIndent(int startIndex, int length)
		{
			int indent = 0, end = startIndex + length;
			for (int i = startIndex; i != end; i++) {
				if (Source[startIndex] == '\t')
					indent = ((indent / SpacesPerTab) + 1) * SpacesPerTab;
				else
					indent++;
			}
			return indent;
		}
	}
}
