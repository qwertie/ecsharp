using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Threading;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Utilities;
using uchar = System.Int32;

namespace Loyc.Syntax.Les
{
	using TT = TokenType;

	/// <summary>Lexer for EC# source code.</summary>
	/// <seealso cref="ILexer{Token}"/>
	/// <seealso cref="TokensToTree"/>
	public partial class LesLexer : BaseILexer<ICharSource, Token>, ILexer<Token>, ICloneable<LesLexer>
	{
		public LesLexer(UString text, IMessageSink errorSink) : this(text, "", errorSink) { }
		public LesLexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) : base(text, fileName, startPosition) {
			ErrorSink = sink;
		}

		public bool AllowNestedComments = true;
		/// <summary>Used for syntax highlighting, which doesn't care about token values.
		/// This option causes the Token.Value to be set to a default, like '\0' for 
		/// single-quoted strings and 0 for numbers. Operator names are still parsed.</summary>
		public bool SkipValueParsing = false;
		protected bool _isFloat, _parseNeeded, _isNegative;
		// Alternate: hex numbers, verbatim strings
		// UserFlag: bin numbers, double-verbatim
		protected NodeStyle _style;
		protected int _numberBase;
		protected Symbol _typeSuffix;
		protected TokenType _type; // predicted type of the current token
		protected object _value;
		protected int _startPosition;

		//now we use LexerSourceFile instead
		//protected InternalList<int> _lineIndexes = InternalList<int>.Empty;

		public override void Reset(ICharSource source, string fileName = "", int inputPosition = 0, bool newSourceFile = true)
		{
			base.Reset(source, fileName, inputPosition, newSourceFile);
			InputPosition += IndentString.Length; // skip initial indent, if any
		}

		protected override void Error(int lookaheadIndex, string message, params object[] args)
		{
			_parseNeeded = true; // don't use the "fast" code path
			base.Error(lookaheadIndex, message, args);
		}

		// Gets the text of the current token that has been parsed so far
		private UString Text()
		{
			return CharSource.Slice(_startPosition, InputPosition - _startPosition);
		}

		protected sealed override void AfterNewline()
		{
			base.AfterNewline();
		}
		protected override bool SupportDotIndents() { return true; }
		
		public LesLexer Clone()
		{
			return (LesLexer)MemberwiseClone();
		}

		#region Token value parsers
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
					UnescapeQuotedString(ref original, Error, sb, IndentString);
					Debug.Assert(original.IsEmpty);
					if (sb.Length == 1)
						c = sb[0];
					else {
						_value = sb.ToString();
						if (sb.Length == 0)
							Error(_startPosition, Localize.Localized("Empty character literal"));
						else
							Error(_startPosition, Localize.Localized("Character literal has {0} characters (there should be exactly one)", sb.Length));
					}
				}
			}
			if (c != -1)
				_value = CG.Cache((char)c);
		}

		protected Symbol ParseBQStringValue()
		{
			UString s = ParseStringCore(false);
			return IdToSymbol(s);
		}

		protected object ParseStringValue(bool isTripleQuoted)
		{
			_value = ParseStringCore(isTripleQuoted);
			if (_value.ToString().Length < 64)
				_value = CG.Cache(_value);
			return _value;
		}

		string ParseStringCore(bool isTripleQuoted)
		{
			if (SkipValueParsing)
				return "";
			string value;
			if (_parseNeeded) {
				UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);
				value = UnescapeQuotedString(ref original, Error, IndentString);
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
		/// <param name="ecsTQIndents">Enable EC# triple-quoted string indent
		/// rules, which allow an additional one tab or three spaces of indent.
		/// (It hasn't been decided whether to support this in LES.)</param>
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
		public static string UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, UString indentation = default(UString), bool ecsTQIndents = false)
		{
			var sb = new StringBuilder();
			UnescapeQuotedString(ref sourceText, onError, sb, indentation, ecsTQIndents);
			return sb.ToString();
		}
		
		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes (see documentation of the first overload) into a 
		/// StringBuilder.</summary>
		public static void UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString), bool ecsTQIndents = false)
		{
			bool isTripleQuoted = false, fail;
			char quoteType = (char)sourceText.PopFront(out fail);
			if (sourceText[0, '\0'] == quoteType &&
				sourceText[1, '\0'] == quoteType) {
				sourceText = sourceText.Substring(2);
				isTripleQuoted = true;
			}
			if (!UnescapeString(ref sourceText, quoteType, isTripleQuoted, onError, sb, indentation, ecsTQIndents))
				onError(sourceText.InternalStart, Localize.Localized("String literal did not end properly"));
		}
		
		/// <summary>Parses a normal or triple-quoted string whose starting quotes 
		/// have been stripped out. If triple-quote parsing was requested, stops 
		/// parsing at three quote marks; otherwise, stops parsing at a single 
		/// end-quote or newline.</summary>
		/// <returns>true if parsing stopped at one or three quote marks, or false
		/// if parsing stopped at the end of the input string or at a newline (in
		/// a string that is not triple-quoted).</returns>
		/// <remarks>This method recognizes LES and EC#-style string syntax.</remarks>
		public static bool UnescapeString(ref UString sourceText, char quoteType, bool isTripleQuoted, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString), bool ecsTQIndents = false)
		{
			Debug.Assert(quoteType == '"' || quoteType == '\'' || quoteType == '`');
			bool fail;
			for (;;) {
				if (sourceText.IsEmpty)
					return false;
				int i0 = sourceText.InternalStart;
				if (!isTripleQuoted) {
					char c = ParseHelpers.UnescapeChar(ref sourceText);
					if ((c == quoteType || c == '\n') && sourceText.InternalStart == i0 + 1) {
						return c == quoteType; // end of string
					}
					if (c == '\\' && sourceText.InternalStart == i0 + 1) {
						// This backslash was ignored by UnescapeChar
						onError(i0, Localize.Localized(@"Unrecognized escape sequence '\{0}' in string", ParseHelpers.EscapeCStyle(sourceText[0, ' '].ToString(), EscapeC.Control)));
					}
					sb.Append(c);
				} else {
					// Inside triple-quoted string
					int c;
					if (sourceText[2, '\0'] == '/') {
						// Detect escape sequence
						c = ParseHelpers.UnescapeChar(ref sourceText);
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
							UString src = sourceText, ind = indentation;
							int sp;
							while ((sp = src.PopFront(out fail)) == ind.PopFront(out fail) && !fail)
								sourceText = src;
							if (ecsTQIndents) {
								// Allow an additional one tab or three spaces
								if (sp == '\t')
									sourceText = src;
								else if (sp == ' ') { 
									sourceText = src;
									if (src.PopFront(out fail) == ' ')
										sourceText = src;
									if (src.PopFront(out fail) == ' ')
										sourceText = src;
								}
							}
						}
					}
					
					sb.Append((char)c);
				}
			}
		}

		#endregion

		#region Identifier & Symbol parsing (includes @true, @false, @null, named floats) (including public ParseIdentifier())

		Dictionary<UString, object> NamedLiterals = new Dictionary<UString, object>()
		{
			{ "true", true },
			{ "false", false },
			{ "null", null },
			{ "void", new @void() },
			{ "nan_f", float.NaN },
			{ "nan_d", double.NaN },
			{ "inf_f", float.PositiveInfinity },
			{ "inf_d", double.PositiveInfinity },
			{ "-inf_f", float.NegativeInfinity },
			{ "-inf_d", double.NegativeInfinity }
		};

		protected object ParseIdValue(bool isFancy)
		{
			if (SkipValueParsing)
				return _value = GSymbol.Empty;
			UString id;
			if (isFancy) {
				// includes @etc-etc and @`backquoted`
				UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);
				bool checkForNamedLiteral;
				id = ParseIdentifier(ref original, Error, out checkForNamedLiteral);
				Debug.Assert(original.IsEmpty);
				if (checkForNamedLiteral) {
					object namedValue;
					if (NamedLiterals.TryGetValue(id, out namedValue)) {
						_type = TT.Literal;
						return _value = namedValue;
					}
				}
			} else // normal identifier
				id = CharSource.Slice(_startPosition, InputPosition - _startPosition);

			return _value = IdToSymbol(id);
		}

		protected object ParseSymbolValue()
		{
			if (SkipValueParsing)
			{
				return _value = GSymbol.Empty;
			}
			Debug.Assert(CharSource[_startPosition] == '@' && CharSource[_startPosition + 1] == '@');
			UString original = CharSource.Slice(_startPosition + 2, InputPosition - _startPosition - 2);
			if (_parseNeeded) {
				string text = UnescapeQuotedString(ref original, Error);
				Debug.Assert(original.IsEmpty);
				return _value = IdToSymbol(text);
			} else if (original[0, '\0'] == '`')
				return _value = IdToSymbol(original.Substring(1, original.Length - 2));
			else
				return _value = IdToSymbol(original);
		}

		protected Dictionary<UString, Symbol> _idCache = new Dictionary<UString,Symbol>();
		Symbol IdToSymbol(UString ustr)
		{
			Symbol sym;
			if (!_idCache.TryGetValue(ustr, out sym)) {
				string str = ustr.ToString();
				_idCache[str] = sym = (Symbol) str;
			}
			return sym;
		}

		/// <summary>Parses an LES-style identifier such as <c>foo</c>, <c>@foo</c>, 
		/// <c>@`foo`</c> or <c>@--punctuation--</c>.
		/// </summary>
		/// <param name="source">Text to parse. On return, the range has been 
		/// decreased by the length of the token; this method also stops if this
		/// range becomes empty.</param>
		/// <param name="onError">A method to call on error</param>
		/// <param name="checkForNamedLiteral">This is set to true when the input 
		/// starts with @ but doesn't use backquotes, which could indicate that 
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
			} else if (IsIdStartChar(c)) {
				parsed.Append(c);
				for (;;) {
					c = source.PopFront(out fail);
					if (!IsIdContChar(c))
						break;
					parsed.Append((char)c);
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

		protected object ParseNumberValue()
		{
			if (SkipValueParsing)
			{
				return _value = CG.Cache(0);
			}
			// Optimize the most common case: a one-digit integer
			if (InputPosition == _startPosition + 1) {
				Debug.Assert(char.IsDigit(CharSource[_startPosition]));
				return _value = CG.Cache((int)(CharSource[_startPosition] - '0'));
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
			return _value;
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

		static object ParseIntegerValue(UString source, bool isNegative, int numberBase, Symbol suffix, ref string error)
		{
			if (source.IsEmpty) {
				error = Localize.Localized("Syntax error in integer literal");
				return CG.Cache(0);
			}
			// Parse the integer
			ulong unsigned;
			bool overflow = !ParseHelpers.TryParseUInt(ref source, out unsigned, numberBase, ParseNumberFlag.SkipUnderscores);
			if (!source.IsEmpty) {
				// I'm not sure if this can ever happen
				error = Localize.Localized("Syntax error in integer literal");
			}

			// If no suffix, automatically choose int, uint, long or ulong
			if (suffix == null) {
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
				error = Localize.Localized("Overflow in integer literal (the number is 0x{0:X} after binary truncation).", value);
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
			error = Localize.Localized("Syntax error in float literal");
			return null;
		}

		private static object ParseSpecialFloatValue(UString source, bool isNegative, int radix, Symbol typeSuffix, ref string error)
		{
			if (typeSuffix == _F)
			{
				float result = ParseHelpers.TryParseFloat(ref source, radix, ParseNumberFlag.SkipUnderscores);
				if (float.IsNaN(result))
					error = Localize.Localized("Syntax error in '{0}' literal", "float");
				else if (float.IsInfinity(result))
					error = Localize.Localized("Overflow in '{0}' literal", "float");
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

				double result = ParseHelpers.TryParseDouble(ref source, radix, ParseNumberFlag.SkipUnderscores);
				if (double.IsNaN(result))
					error = Localize.Localized("Syntax error in '{0}' literal", type);
				else if (double.IsInfinity(result))
					error = Localize.Localized("Overflow in '{0}' literal", type);
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
			ParseOp();
		}

		static Symbol _Backslash = GSymbol.Get(@"\");

		protected Dictionary<UString, Pair<Symbol, TokenType>> _opCache = new Dictionary<UString, Pair<Symbol, TokenType>>();
		void ParseOp()
		{
			UString opText = Text();

			Pair<Symbol, TokenType> sym;
			if (!_opCache.TryGetValue(opText, out sym)) {
				string opStr = opText.ToString();
				_opCache[opText] = sym = GetOpNameAndType(opStr);
			}
			_value = sym.A;
			_type = sym.B;
		}

		private Pair<Symbol, TokenType> GetOpNameAndType(string op)
		{
			Debug.Assert(op.Length > 0);
			TT tt;
			Symbol name;

			// Get first and last of the operator's initial punctuation
			char first = op[0], last = first;
			if (first != '\'') {
				name = (Symbol)op;
				// TODO: turn on this new behavior:
				//name = (Symbol)("'" + op);
				last = op[op.Length - 1];
				if (op == "!")
					return Pair.Create((Symbol)"!", TT.Not);
			} else {
				name = (Symbol)op;
				Debug.Assert(op.Length > 1);
				first = op[1];
				for (int i = 1; i < op.Length; i++) {
					if (IsIdContChar(op[i]))
						break;
					last = op[i];
				}
			}

			if (op.Length >= 2 && ((first == '+' && last == '+') || (first == '-' && last == '-')))
				tt = TT.PreOrSufOp;
			else if (last == '$')
				tt = TT.PrefixOp;
			else if (last == '.' && (op.Length == 1 || first != '.'))
				tt = TT.Dot;
			else if (last == '=' && (op.Length == 1 || first != '!' && first != '='))
				tt = TT.Assignment;
			else
				tt = TT.NormalOp;
			return Pair.Create(name, tt);
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
			'~', '~', '!', '!', '%','%', '^','^', '&','&', '*','*', '-','-', '+','+', '=','=', '|','|', '<','<', '>','>', '/','/', '?','?', ':',':', '.','.', '@','@', '$','$', '\\', '\\');
		static readonly HashSet<int> IdContSet = NewSetOfRanges('0', '9', 'a', 'z', 'A', 'Z', '_', '_', '\'', '\'');
		static readonly HashSet<int> OpContSet = NewSetOfRanges(
			'~', '~', '!', '!', '%','%', '^','^', '&','&', '*','*', '-','-', '+','+', '=','=', '|','|', '<','<', '>','>', '/','/', '?','?', ':',':', '.','.', '@','@', '$','$');

		public static bool IsIdStartChar(uchar c) { return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '_' || c == '#' || c >= 0x80 && char.IsLetter((char)c); }
		public static bool IsIdContChar(uchar c) { return IsIdStartChar(c) || c >= '0' && c <= '9' || c == '\''; }
		public static bool IsOpContChar(char c) { return OpContSet.Contains(c); }
		public static bool IsSpecialIdChar(char c) { return SpecialIdSet.Contains(c); }

		#endregion // Value parsers
	}
}
