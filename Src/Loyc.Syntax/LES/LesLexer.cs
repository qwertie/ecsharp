using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Syntax;
using Loyc.Threading;
using Loyc.Collections;
using uchar = System.Int32;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using System.Globalization;
	using Loyc.Syntax.Lexing;
	using Loyc.Collections.Impl;
	using Loyc.Utilities;

	/// <summary>Lexer for EC# source code (see <see cref="ILexer"/>).</summary>
	/// <seealso cref="TokensToTree"/>
	public partial class LesLexer : BaseLexer, ILexer, ICloneable<LesLexer>
	{
		public LesLexer(string text, IMessageSink errorSink) : this(new StringSlice(text), "", errorSink) { }
		public LesLexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) : base(text, fileName, startPosition) {
			ErrorSink = sink;
		}

		public bool AllowNestedComments = true;
		/// <summary>Used for syntax highlighting, which doesn't care about token values.
		/// This option causes the Token.Value to be set to a default, like '\0' for 
		/// single-quoted strings and 0 for numbers. Operator names are still parsed.</summary>
		public bool SkipValueParsing = false;
		private bool _isFloat, _parseNeeded, _isNegative;
		// Alternate: hex numbers, verbatim strings
		// UserFlag: bin numbers, double-verbatim
		private NodeStyle _style;
		private int _numberBase;
		private Symbol _typeSuffix;
		private TokenType _type; // predicted type of the current token
		private object _value;
		private int _startPosition;

		protected InternalList<int> _lineIndexes = InternalList<int>.Empty;

		new public void Reset(ICharSource source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
		{
			base.Reset(source, fileName, inputPosition, newSourceFile);
		}

		new public ISourceFile SourceFile { get { return base.SourceFile; } }

		/// <summary>Error messages are given to the message sink.</summary>
		/// <remarks>The context argument will have type SourcePos.</remarks>
		public IMessageSink ErrorSink { get; set; }
		
		StringSlice _indent;
		int _indentLevel;
		public UString IndentString { get { return _indent; } }
		public int IndentLevel { get { return _indentLevel; } }
		public int SpacesPerTab = 4;

		protected override void Error(int index, string message)
		{
			_parseNeeded = true; // don't use the "fast" code path
			ErrorSink.Write(Severity.Error, SourceFile.IndexToLine(index), message);
		}

		protected sealed override void AfterNewline()
		{
			base.AfterNewline();
			_lineIndexes.Add(InputPosition);
		}

		public LesLexer Clone()
		{
			return (LesLexer)MemberwiseClone();
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
				return _current = new Token((int)_type, _startPosition, InputPosition - _startPosition, _style, _value);
			}
		}

		#region Value parsers
		// After the generated lexer code determines the boundaries of the token, 
		// one of these methods extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		#region String parsing (including public UnescapeQuotedString())

		static readonly object BoxedZeroChar = '\0';

		void ParseSQStringValue()
		{
			int c = -1;
			if (SkipValueParsing)
				c = '\0';
			else { 
				int len = InputPosition - _startPosition;
				if (!_parseNeeded && len == 3) {
					c = CharSource[_startPosition + 1];
				} else {
					var sb = TempSB();
					UString original = CharSource.Slice(_startPosition, len);
					UnescapeQuotedString(ref original, Error, sb, _indent);
					Debug.Assert(original.IsEmpty);
					if (sb.Length == 1)
						c = sb[0];
					else {
						_value = sb.ToString();
						if (sb.Length == 0)
							Error(_startPosition, Localize.From("Empty character literal"));
						else
							Error(_startPosition, Localize.From("Character literal has {0} characters (there should be exactly one)", sb.Length));
					}
				}
			}
			if (c != -1)
				_value = CG.Cache((char)c);
		}

		void ParseBQStringValue()
		{
			var s = ParseStringCore(false);
			_value = IdToSymbol(s);
		}

		void ParseStringValue(bool isTripleQuoted)
		{
			_value = ParseStringCore(isTripleQuoted);
			if (_value.ToString().Length < 64)
				_value = CG.Cache(_value);
		}

		string ParseStringCore(bool isTripleQuoted)
		{
			if (SkipValueParsing)
				return "";
			string value;
			if (_parseNeeded) {
				UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);
				value = UnescapeQuotedString(ref original, Error, _indent);
				Debug.Assert(original.IsEmpty);
			} else {
				Debug.Assert(CharSource.TryGet(InputPosition - 1, '?') == CharSource.TryGet(_startPosition, '!'));
				if (isTripleQuoted)
					value = CharSource.Slice(_startPosition + 3, InputPosition - _startPosition - 6).ToString();
				else
					value = CharSource.Slice(_startPosition + 1, InputPosition - _startPosition - 2).ToString();
			}
			return value;
		}

		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes. Supports quote types '\'', '"' and '`'.</summary>
		/// <param name="sourceText">input text</param>
		/// <param name="onError">Called in case of parsing error (unknown escape sequence or missing end quotes)</param>
		/// <param name="indentation">Inside a triple-quoted string, any text
		/// following a newline is ignored as long as it matches this string. 
		/// For example, if the text following a newline is "\t\t Foo" and this
		/// string is "\t\t\t", the tabs are ignored and " Foo" is kept.</param>
		/// <returns>The decoded string</returns>
		/// <remarks>This method recognizes LES and EC#-style string syntax.
		/// Firstly, it recognizes triple-quoted strings (''' """ ```). These 
		/// strings enjoy special newline handling: the newline is always 
		/// interpreted as \n regardless of the actual kind of newline (\r and 
		/// \r\n newlines come out as \n), and indentation following the newline
		/// can be stripped out. Triple-quoted strings can have escape sequences
		/// that use both kinds of slash, like so: <c>\n/ \r/ \'/ \"/ \0/</c>.
		/// However, there are no unicode escapes (\u1234/ is NOT supported).
		/// <para/>
		/// Secondly, it recognizes normal strings (' " `). These strings stop 
		/// parsing (with an error) at a newline, and can contain C-style escape 
		/// sequences: <c>\n \r \' \" \0</c> etc. C#-style verbatim strings are 
		/// NOT supported.
		/// </remarks>
		public static string UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, UString indentation = default(UString))
		{
			var sb = new StringBuilder();
			UnescapeQuotedString(ref sourceText, onError, sb, indentation);
			return sb.ToString();
		}
		
		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes (see documentation of the first overload) into a 
		/// StringBuilder.</summary>
		public static void UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString))
		{
			bool isTripleQuoted = false, fail;
			char quoteType = (char)sourceText.PopFront(out fail);
			if (sourceText[0, '\0'] == quoteType &&
				sourceText[1, '\0'] == quoteType) {
				sourceText = sourceText.Substring(2);
				isTripleQuoted = true;
			}
			if (!UnescapeString(ref sourceText, quoteType, isTripleQuoted, onError, sb, indentation))
				onError(sourceText.InternalStart, Localize.From("String literal did not end properly"));
		}
		
		/// <summary>Parses a normal or triple-quoted string whose starting quotes 
		/// have been stripped out. If triple-quote parsing was requested, stops 
		/// parsing at three quote marks; otherwise, stops parsing at a single 
		/// end-quote or newline.</summary>
		/// <returns>true if parsing stopped at one or three quote marks, or false
		/// if parsing stopped at the end of the input string or at a newline (in
		/// a string that is not triple-quoted).</returns>
		/// <remarks>This method recognizes LES and EC#-style string syntax.</remarks>
		public static bool UnescapeString(ref UString sourceText, char quoteType, bool isTripleQuoted, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString))
		{
			Debug.Assert(quoteType == '"' || quoteType == '\'' || quoteType == '`');
			bool fail;
			for (;;) {
				if (sourceText.IsEmpty)
					return false;
				int i0 = sourceText.InternalStart;
				if (!isTripleQuoted) {
					char c = G.UnescapeChar(ref sourceText);
					if ((c == quoteType || c == '\n') && sourceText.InternalStart == i0 + 1) {
						return c == quoteType; // end of string
					}
					if (c == '\\' && sourceText.InternalStart == i0 + 1) {
						// This backslash was ignored by UnescapeChar
						onError(i0, Localize.From(@"Unrecognized escape sequence '\{0}' in string", G.EscapeCStyle(sourceText[0, ' '].ToString(), EscapeC.Control)));
					}
					sb.Append(c);
				} else {
					// Inside triple-quoted string
					int c;
					if (sourceText[2, '\0'] == '/') {
						c = G.UnescapeChar(ref sourceText);
						if (sourceText.InternalStart > i0 + 1)
							G.Verify(sourceText.PopFront(out fail) == '/');
					} else {
						c = sourceText.PopFront(out fail);
						if (fail)
							return false;
						if (c == quoteType) {
							if (sourceText[0, '\0'] == quoteType &&
								sourceText[1, '\0'] == quoteType) {
								sourceText = sourceText.Substring(2);
								// end of string
								return true;
							}
						}
						if (c == '\r' || c == '\n') {
							// To ensure platform independency of source code, CR and 
							// CR-LF become LF.
							if (c == '\r') {
								c = '\n';
								var copy = sourceText.Clone();
								if (sourceText.PopFront(out fail) != '\n')
									sourceText = copy;
							}
							// Inside a triple-quoted string, the indentation following a newline 
							// is ignored, as long as it matches the indentation of the first line.
							UString src = sourceText.Clone(), ind = indentation;
							while (src.PopFront(out fail) == ind.PopFront(out fail) && !fail)
								sourceText = src;
						}
					}
					
					sb.Append((char)c);
				}
			}
		}

		#endregion

		#region Identifier & Symbol parsing (includes @true, @false, @null) (including public ParseIdentifier())

		UString TrueString = "true", FalseString = "false", NullString = "null", VoidString = "void";
		object BoxedTrue = true, BoxedFalse = false, BoxedVoid = new @void();

		void ParseIdValue()
		{
			if (SkipValueParsing) {
				_value = GSymbol.Empty;
				return;
			}
			UString id;
			if (_parseNeeded) {
				// includes @etc-etc and @`backquoted`
				UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);
				bool checkForNamedLiteral;
				id = ParseIdentifier(ref original, Error, out checkForNamedLiteral);
				Debug.Assert(original.IsEmpty);
				if (checkForNamedLiteral) {
					if (id == TrueString) {
						_value = BoxedTrue;
						_type = TT.OtherLit;
						return;
					} else if (id == FalseString) {
						_value = BoxedFalse;
						_type = TT.OtherLit;
						return;
					} else if (id == NullString) {
						_value = null;
						_type = TT.OtherLit;
						return;
					} else if (id == VoidString) {
						_value = BoxedVoid;
						_type = TT.OtherLit;
						return;
					}
				}
			} else // normal identifier
				id = CharSource.Slice(_startPosition, InputPosition - _startPosition);

			_value = IdToSymbol(id);
		}

		void ParseSymbolValue()
		{
			if (SkipValueParsing)
			{
				_value = GSymbol.Empty;
				return;
			}
			Debug.Assert(CharSource[_startPosition] == '@' && CharSource[_startPosition + 1] == '@');
			UString original = CharSource.Slice(_startPosition + 2, InputPosition - _startPosition - 2);
			if (_parseNeeded) {
				string text = UnescapeQuotedString(ref original, Error);
				Debug.Assert(original.IsEmpty);
				_value = IdToSymbol(text);
			} else if (original[0, '\0'] == '`')
				_value = IdToSymbol(original.Substring(1, original.Length - 2));
			else
				_value = IdToSymbol(original);
		}

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

		/// <summary>Parses an LES-style identifier such as <c>foo</c>, <c>@foo</c>, 
		/// <c>@`foo`</c> or <c>@--punctuation--</c>. Also recognizes <c>#`foo`</c>.
		/// </summary>
		/// <param name="source">Text to parse. On return, the range has been 
		/// decreased by the length of the token; this method also stops if this
		/// range becomes empty.</param>
		/// <param name="onError">A method to call on error</param>
		/// <param name="checkForNamedLiteral">This is set to true when the input 
		/// starts with @ but is a normal identifier, which could indicate that 
		/// it is an LES named literal such as @false or @null.</param>
		/// <returns>The parsed version of the identifier.</returns>
		public static string ParseIdentifier(ref UString source, Action<int, string> onError, out bool checkForNamedLiteral)
		{
			checkForNamedLiteral = false;
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
					checkForNamedLiteral = true;
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

		#endregion

		#region Number parsing

		static Symbol _sub = GSymbol.Get("-");
		static Symbol _F = GSymbol.Get("F");
		static Symbol _D = GSymbol.Get("D");
		static Symbol _M = GSymbol.Get("M");
		static Symbol _U = GSymbol.Get("U");
		static Symbol _L = GSymbol.Get("L");
		static Symbol _UL = GSymbol.Get("UL");

		void ParseNumberValue()
		{
			if (SkipValueParsing)
			{
				_value = OneDigitInts[0];
				return;
			}
			// Optimize the most common case: a one-digit integer
			if (InputPosition == _startPosition + 1) {
				Debug.Assert(char.IsDigit(CharSource[_startPosition]));
				_value = OneDigitInts[CharSource[_startPosition] - '0'];
				return;
			}

			int start = _startPosition;
			if (_isNegative)
				start++;
			if (_numberBase != 10)
				start += 2;
			int stop = InputPosition;
			if (_typeSuffix != null)
				stop -= _typeSuffix.Name.Length;

			UString digits = CharSource.Slice(start, stop - start);
			string error;
			if ((_value = ParseNumberCore(digits, _isNegative, _numberBase, _isFloat, _typeSuffix, out error)) == null)
				_value = 0;
			else if (_value == CodeSymbols.Sub) {
				InputPosition = _startPosition + 1;
				_type = TT.NormalOp;
			}
			if (error != null)
				Error(_startPosition, error);
		}

		/// <summary>Parses the digits of a literal (integer or floating-point),
		/// not including the radix prefix (0x, 0b) or type suffix (F, D, L, etc.)</summary>
		/// <param name="source">Digits of the number (not including radix prefix or type suffix)</param>
		/// <param name="isFloat">Whether the number is floating-point</param>
		/// <param name="numberBase">Radix. Must be 2 (binary), 10 (decimal) or 16 (hexadecimal).</param>
		/// <param name="typeSuffix">Type suffix: F, D, M, U, L, UL, or null.</param>
		/// <param name="error">Set to an error message in case of error.</param>
		/// <returns>Boxed value of the literal, null if total failure (result 
		/// is not null in case of overflow), or <see cref="CodeSymbols.Sub"/> (-)
		/// if isNegative is true but the type suffix is unsigned or the number 
		/// is larger than long.MaxValue.</returns>
		public static object ParseNumberCore(UString source, bool isNegative, int numberBase, bool isFloat, Symbol typeSuffix, out string error)
		{
			error = null;
			if (!isFloat) {
				return ParseIntegerValue(source, isNegative, numberBase, typeSuffix, ref error);
			} else {
				if (numberBase == 10)
					return ParseNormalFloat(source, isNegative, typeSuffix, ref error);
				else
					return ParseSpecialFloatValue(source, isNegative, numberBase, typeSuffix, ref error);
			}
		}

		static readonly object[] OneDigitInts = new object[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

		static object ParseIntegerValue(UString source, bool isNegative, int numberBase, Symbol suffix, ref string error)
		{
			if (source.IsEmpty) {
				error = Localize.From("Syntax error in integer literal");
				return 0;
			}
			// Parse the integer
			ulong unsigned;
			bool overflow = !G.TryParseUInt(ref source, out unsigned, numberBase, G.ParseFlag.SkipUnderscores);
			if (!source.IsEmpty) {
				// I'm not sure if this can ever happen
				error = Localize.From("Syntax error in integer literal");
			}

			// If no suffix, automatically choose int, uint, long or ulong
			if (suffix == null) {
				if (!isNegative && unsigned < (uint)OneDigitInts.Length)
					return OneDigitInts[unsigned];

				if (unsigned > long.MaxValue)
					suffix = _UL;
				else if (unsigned > uint.MaxValue)
					suffix = _L;
				else if (unsigned > int.MaxValue)
					suffix = isNegative ? _L : _U;
			}

			if (isNegative && (suffix == _U || suffix == _UL)) {
				// Oops, an unsigned number can't be negative, so treat 
				// '-' as a separate token and let the number be reparsed.
				return CodeSymbols.Sub;
			}

			// Create boxed integer of the appropriate type 
			object value;
			if (suffix == _UL)
				value = unsigned;
			else if (suffix == _U) {
				overflow = overflow || (uint)unsigned != unsigned;
				value = (uint)unsigned;
			} else if (suffix == _L) {
				if (isNegative) {
					overflow = overflow || -(long)unsigned > 0;
					value = -(long)unsigned;
				} else {
					overflow = overflow || (long)unsigned < 0;
					value = (long)unsigned;
				}
			} else {
				value = isNegative ? -(int)unsigned : (int)unsigned;
			}

			if (overflow)
				error = Localize.From("Overflow in integer literal (the number is 0x{0:X} after binary truncation).", value);
			return value;
		}

		static object ParseNormalFloat(UString source, bool isNegative, Symbol typeSuffix, ref string error)
		{
			string token = (string)source;
			token = token.Replace("_", "");
			if (typeSuffix == _F) {
				float f;
				if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
					return isNegative ? -f : f;
			} else if (typeSuffix == _M) {
				decimal m;
				if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out m))
					return isNegative ? -m : m;
			} else {
				double d;
				if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
					return isNegative ? -d : d;
			}
			error = Localize.From("Syntax error in float literal");
			return null;
		}

		private static object ParseSpecialFloatValue(UString source, bool isNegative, int radix, Symbol typeSuffix, ref string error)
		{
			if (typeSuffix == _F)
			{
				float result = G.TryParseFloat(ref source, radix, G.ParseFlag.SkipUnderscores);
				if (float.IsNaN(result))
					error = Localize.From("Syntax error in '{0}' literal", "float");
				else if (float.IsInfinity(result))
					error = Localize.From("Overflow in '{0}' literal", "float");
				if (isNegative)
					result = -result;
				return result;
			}
			else
			{
				string type = "double";
				if (typeSuffix == _M) {
					error = "Support for hex and binary literals of type decimal is not implemented. Converting from double instead.";
					type = "decimal";
				}

				double result = G.TryParseDouble(ref source, radix, G.ParseFlag.SkipUnderscores);
				if (double.IsNaN(result))
					error = Localize.From("Syntax error in '{0}' literal", type);
				else if (double.IsInfinity(result))
					error = Localize.From("Overflow in '{0}' literal", type);
				if (isNegative)
					result = -result;
				if (typeSuffix == _M)
					return (decimal)result;
				else
					return result;
			}
		}

		#endregion

		#region Operator parsing

		void ParseNormalOp()
		{
			_parseNeeded = false;
			ParseOp(false);
		}

		static Symbol _Backslash = GSymbol.Get(@"\");

		void ParseBackslashOp()
		{
			ParseOp(true);
		}

		protected Dictionary<UString, Pair<Symbol, TokenType>> _opCache = new Dictionary<UString, Pair<Symbol, TokenType>>();
		void ParseOp(bool backslashOp)
		{
			UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);

			Pair<Symbol, TokenType> sym;
			if (_opCache.TryGetValue(original, out sym)) {
				_value = sym.A;
				_type = sym.B;
			} else {
				Debug.Assert(backslashOp == (original[0] == '\\'));
				// op will be the operator text without the initial backslash, if any:
				// && => &&, \foo => foo, \`foo` => foo, \`@`\ => @\
				UString op = original;
				if (backslashOp)
				{
					if (original.Length == 1)
					{	
						// Just a single backslash is the "\" operator
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
				_opCache[opStr] = sym = Pair.Create(GSymbol.Get(opStr), _type);
				_value = sym.A;
			}
		}

		private TokenType GetOpType(string op)
		{
			Debug.Assert(op.Length > 0);
			if (op == ":")
				return TT.Colon;
			if (op == "!")
				return TT.Not;
			char last = op[op.Length - 1], first = op[0];
			if (op.Length >= 2 && ((first == '+' && last == '+') || (first == '-' && last == '-')))
				return TT.PreSufOp;
			if (last == '\\')
				return TT.SuffixOp;
			if (last == '$')
				return TT.PrefixOp;
			if (last == '.' && (op.Length == 1 || first != '.'))
				return TT.Dot;
			if (last == '=' && (op.Length == 1 || first != '!' && first != '='))
				return TT.Assignment;
			return TT.NormalOp;
		}

		#endregion

		[ThreadStatic]
		static StringBuilder _tempsb;
		static StringBuilder TempSB()
		{
			var sb = _tempsb;
			if (sb == null)
				_tempsb = sb = new StringBuilder();
			sb.Length = 0; // sb.Clear() only exists in .NET 4
			return sb;
		}

		static readonly HashSet<int> SpecialIdSet = NewSetOfRanges('0', '9', 'a', 'z', 'A', 'Z', '_', '_', '\'', '\'', '#', '#', 
			'~', '~', '!', '!', '%','%', '^','^', '&','&', '*','*', '-','-', '+','+', '=','=', '|','|', '<','<', '>','>', '/','/', '\\', '\\', '?','?', ':',':', '.','.', '@','@', '$','$');
		static readonly HashSet<int> IdContSet = NewSetOfRanges('0', '9', 'a', 'z', 'A', 'Z', '_', '_', '\'', '\'');
		static readonly HashSet<int> OpContSet = NewSetOfRanges(
			'~', '~', '!', '!', '%','%', '^','^', '&','&', '*','*', '-','-', '+','+', '=','=', '|','|', '<','<', '>','>', '/','/', '\\', '\\', '?','?', ':',':', '.','.', '@','@', '$','$');

		public static bool IsIdStartChar(uchar c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c >= 0x80 && char.IsLetter((char)c); }
		public static bool IsIdContChar(uchar c) { return IsIdStartChar(c) || c >= '0' && c <= '9' || c == '\''; }
		public static bool IsOpContChar(char c) { return OpContSet.Contains(c); }
		public static bool IsSpecialIdChar(char c) { return SpecialIdSet.Contains(c); }

		#endregion // Value parsers

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

		Token? _current;

		void IDisposable.Dispose() {}
		Token IEnumerator<Token>.Current { get { return _current.Value; } }
		object System.Collections.IEnumerator.Current { get { return _current; } }
		void System.Collections.IEnumerator.Reset() { throw new NotSupportedException(); }
		bool System.Collections.IEnumerator.MoveNext()
		{
			_current = NextToken();
			return _current.HasValue;
		}

		public SourcePos IndexToLine(int index)
		{
			var fn = SourceFile.FileName;
			if (index < 0 || _lineIndexes.IsEmpty)
				return new SourcePos(fn, 0, 0);
			int lastIndex = _lineIndexes.Last;
			if (index < lastIndex) {
				int i = _lineIndexes.BinarySearch(index);
				int line = i >= 0 ? i : ~i - 1;
				int col = index - _lineIndexes[line];
				return new SourcePos(fn, line + 1, col + 1);
			} else {
				if (index > InputPosition)
					index = InputPosition;
				return new SourcePos(fn, _lineIndexes.Count, index - lastIndex + 1);
			}
		}

		public int LineToIndex(int lineNo)
		{
			--lineNo;
			if (lineNo < 0) return -1;
			if (lineNo >= _lineIndexes.Count)
				return CharSource.Count;
			return _lineIndexes[lineNo];
		}

		public int LineToIndex(SourcePos pos)
		{
			return LineToIndex(pos.Line) + pos.PosInLine - 1;
		}
	}
}
