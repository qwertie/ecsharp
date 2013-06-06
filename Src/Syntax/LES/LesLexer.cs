using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Syntax;
using Loyc.LLParserGenerator;
using Loyc.Threading;
using Loyc.Collections;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using System.Globalization;

	public enum TokenType
	{
		Spaces = ' ',
		Newline = '\n',
		SLComment = '/',
		MLComment = '*',
		SQString = '\'',
		DQString = '"',
		BQString = '`',
		Comma = ',',
		Semicolon = ';',
		Id = 'i',
		Symbol = 'S',
		LParen = '(',
		RParen = ')',
		LBrack = '[',
		RBrack = ']',
		LBrace = '{',
		RBrace = '}',
		OpenOf = 'o',
		At = '@',
		AtAt = '2',
		Number = 'n',
		Shebang = 'G',
		
		@base = 'b',
		@false = '0',
		@null = 'n',
		@true = '1',
		@this = 't',
	
		@break = 192,
		@case     ,
		@checked  ,
		@class    ,
		@continue ,
		@default  ,
		@delegate ,
		@do       ,
		@enum     ,
		@event    ,
		@fixed    ,
		@for      ,
		@foreach  ,
		@goto     ,
		@if       ,
		@interface,
		@lock     ,
		@namespace,
		@return   ,
		@struct   ,
		@switch   ,
		@throw    ,
		@try      ,
		@unchecked,
		@using    ,
		@while    ,

		@operator  ,
		@sizeof    ,
		@typeof    ,

		@else      ,
		@catch     ,
		@finally   ,

		@in        ,
		@as        ,
		@is        ,

		@new       ,
		@out       ,
		@stackalloc,

		PPif   = 11,
		PPelse     ,
		PPelif     ,
		PPendif    ,
		PPdefine   ,
		PPundef    ,
		PPwarning  ,
		PPerror    ,
		PPnote     ,
		PPline     ,
		PPregion   ,
		PPendregion,

		Hash = '#',
		Backslash = '\\',

		// Operators
		Mul = '*', Div = '/', 
		Add = '+', Sub = '-',
		Mod = '%', // there is no Exp token due to ambiguity
		Inc = 'U', Dec = 'D',
		And = 'A', Or = 'O', Xor = 'X', Not = '!',
		AndBits = '&', OrBits = '|', XorBits = '^', NotBits = '~',
		Set = '=', Eq = '≈', Neq = '≠', 
		GT = '>', GE = '≥', LT = '<', LE = '≤',
		Shr = '»', Shl = '«',
		QuestionMark = '?',
		DotDot = '…', Dot = '.', NullDot = '_', NullCoalesce = '¿',
		ColonColon = '¨', QuickBind = 'q',
		PtrArrow = 'R', Forward = '→',
		Substitute = '$',
		LambdaArrow = 'L',

		AddSet = '2', SubSet = '3',
		MulSet = '4', DivSet = '5', 
		ModSet = '6', ExpSet = '7',
		ShrSet = '8', ShlSet = '9', 
		ConcatSet = 'B', XorBitsSet = 'D', 
		AndBitsSet = 'E', OrBitsSet = 'F',
		NullCoalesceSet = 'H', 
		QuickBindSet = 'Q',
		
		Indent = '\t', Dedent = '\b'
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
		private bool _isTripleQuoted;
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
				int len;
				id = ParseIdentifier(CharSource.Text, _startPosition, out len, Error);
				Debug.Assert(len == InputPosition - _startPosition);
			} else
				id = CharSource.Text.USlice(_startPosition, InputPosition - _startPosition);

			_value = ToSymbol(id);
		}

		void ParseSymbolValue()
		{
			Debug.Assert(CharSource[_startPosition] == '\\' && CharSource[_startPosition + 1] != '\\');
			if (_parseNeeded) {
				Debug.Assert(CharSource[_startPosition + 1] == '`');
				int stop;
				string text = UnescapeString(CharSource.Text, _startPosition + 1, out stop, Error);
			}
			_value = ToSymbol(CharSource.Substring(_startPosition + 1, InputPosition - (_startPosition + 1)));
		}

		void ParseCharValue()
		{
			var sb = TempSB();
			int length;
			char c;
			if (_parseNeeded) {
				int stop;
				UnescapeString(CharSource.Text, _startPosition, out stop, Error, sb);
				c = sb.TryGet(0, '\0');
				length = sb.Length;
				Debug.Assert(stop == InputPosition);
			} else {
				Debug.Assert(CharSource.TryGet(InputPosition - 1, '?') == CharSource.TryGet(_startPosition, '!'));
				Debug.Assert(!_isTripleQuoted);
				c = CharSource.TryGet(_startPosition + 1, '\0');
				length = InputPosition - _startPosition - 2;
			}
			_value = CG.Cache(c);
			if (length == 0)
				Error(_startPosition, Localize.From("Empty character literal"));
			else if (length != 1)
				Error(_startPosition, Localize.From("Character constant has {0} characters (there should be exactly one)", length));
		}

		void ParseBQStringValue()
		{
			ParseStringCore();
			_value = ToSymbol(_value.ToString());
		}

		void ParseStringValue()
		{
			ParseStringCore();
			if (_value.ToString().Length < 64)
				_value = CG.Cache(_value);
		}

		void ParseStringCore()
		{
			if (_parseNeeded) {
				int stop;
				_value = UnescapeString(CharSource.Text, _startPosition, out stop, Error);
				Debug.Assert(stop == InputPosition);
			} else {
				Debug.Assert(CharSource.TryGet(InputPosition - 1, '?') == CharSource.TryGet(_startPosition, '!'));
				if (_isTripleQuoted)
					_value = CharSource.Substring(_startPosition + 3, InputPosition - _startPosition - 6);
				else
					_value = CharSource.Substring(_startPosition + 1, InputPosition - _startPosition - 2);
			}
		}

		void ParseOp()
		{
			_value = ToSymbol(CharSource.Substring(_startPosition, InputPosition - _startPosition));
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
                G.Verify(float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out f));
				_value = f;
			} else if (_typeSuffix == _M) {
                decimal m = 0.3e+2m;
				G.Verify(decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out m));
				_value = m;
			} else {
				double d;
                G.Verify(double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d));
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
				_type = TT.Sub;
				_value = _sub;
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

		protected Dictionary<UString, Symbol> _idCache;
		Symbol ToSymbol(UString ustr)
		{
			Symbol sym;
			if (!_idCache.TryGetValue(ustr, out sym)) {
				string str = ustr.ToString();
				_idCache[str] = sym = GSymbol.Get(str);
			}
			return sym;
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

		static readonly IntSet SpecialIdSet = IntSet.Parse("[0-9a-zA-Z_'#~!%^&*-+=|<>/?:.@$]");
		static readonly IntSet IdContSet = IntSet.Parse("[0-9a-zA-Z_']");

		public static UString ParseIdentifier(string source, int start, out int length, Action<int, string> onError)
		{
			StringBuilder parsed = TempSB();

			int stop = start;
			char c = source.TryGet(stop, '\0');
			if (c == '\\') {
				char c1 = source.TryGet(stop+1, '\0');
				if (c1 == '\\') {
					stop += 2;
					// expecting: (BQString | Star(Set("[0-9a-zA-Z_'#~!%^&*-+=|<>/?:.@$]") | IdExtLetter))
					c = source.TryGet(stop, '\0');
					if (c == '`') {
						UnescapeString(source, stop, out stop, onError, parsed);
					} else {
						while (SpecialIdSet.Contains(c) || c >= 128 && char.IsLetter(c)) {
							parsed.Append(c);
							c = source.TryGet(++stop, '\0');
						}
					}
				}
			} else {
				if (c == '#' && source.TryGet(stop+1, '\0') == '`') {
					parsed.Append('#');
					UnescapeString(source, stop+1, out stop, onError, parsed);
				} else if (IsIdStartChar(c)) {
					parsed.Append(c);
					for (;;) {
						c = source.TryGet(++stop, '\0');
						if (!IsIdContChar(c))
							break;
						parsed.Append(c);
					}
				}
			}
			length = stop - start;
			return parsed.ToString();
		}

		static bool IsIdStartChar(char c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= 0x80 && char.IsLetter(c); }
		static bool IsIdContChar(char c) { return IsIdStartChar(c) || c >= '0' && c <= '9' || c == '\''; }

		public static string UnescapeString(string sourceText, int openQuoteIndex, out int stop, Action<int, string> onError)
		{
			var sb = new StringBuilder();
			UnescapeString(sourceText, openQuoteIndex, out stop, onError, sb);
			return sb.ToString();
		}
		public static void UnescapeString(string sourceText, int openQuoteIndex, out int stop, Action<int, string> onError, StringBuilder sb)
		{
			bool isTripleQuoted = false;
			char stringType = sourceText[openQuoteIndex];
			if (sourceText.TryGet(openQuoteIndex + 1, '\0') == stringType &&
				sourceText.TryGet(openQuoteIndex + 2, '\0') == stringType) {
				openQuoteIndex += 2;
				isTripleQuoted = true;
			}
			UnescapeString(sourceText, openQuoteIndex + 1, out stop, onError, sourceText[openQuoteIndex], isTripleQuoted, sb);
		}
		public static void UnescapeString(string sourceText, int start, out int stop, Action<int, string> onError, char quoteType, bool isTripleQuoted, StringBuilder sb)
		{
			Debug.Assert(quoteType == '"' || quoteType == '\'' || quoteType == '`');
			stop = start;
			for (;;) {
				if ((uint)stop >= (uint)sourceText.Length) {
					onError(stop, Localize.From("End-of-file in string literal"));
					break;
				}
				if (!isTripleQuoted) {
					int oldStop = stop;
					char c = G.UnescapeChar(sourceText, ref stop);
					if (c == quoteType && stop == oldStop + 1)
						break; // end of string
					if (c == '\\' && stop == oldStop + 1) {
						// This backslash was ignored by UnescapeChar
						onError(oldStop, Localize.From(@"Unrecognized escape sequence '\{0}' in string", G.EscapeCStyle(c.ToString(), EscapeC.Control)));
					}
					sb.Append(c);
				} else {
					char c = sourceText[stop];
					if (c == quoteType) {
						if (sourceText.TryGet(stop + 1, '\0') == quoteType &&
							sourceText.TryGet(stop + 2, '\0') == quoteType) {
								stop += 3;
								if (sourceText.TryGet(stop, '\0') == quoteType) {
									// four quotes means three quotes (last one added below)
									sb.Append(quoteType, 2);
								} else {
									// end of string
									break;
								}
						}
					}
					sb.Append(c);
					stop++;
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
