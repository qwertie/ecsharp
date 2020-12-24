using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
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
	public partial class Les2Lexer : BaseILexer<ICharSource, Token>, ILexer<Token>, ICloneable<Les2Lexer>
	{
		public Les2Lexer(UString text, IMessageSink errorSink) : this(text, "", errorSink) { }
		public Les2Lexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) : base(text, fileName, startPosition) {
			ErrorSink = sink;
		}

		public bool AllowNestedComments = true;
		
		/// <summary>Used for syntax highlighting, which doesn't care about token values.
		/// This option causes the Token.Value to be set to a default, like '\0' for 
		/// single-quoted strings and 0 for numbers. Operator names are still parsed.</summary>
		public bool SkipValueParsing = false;

		protected bool _isFloat, _hasEscapes, _isNegative;
		protected NodeStyle _style;
		protected int _numberBase;
		protected TokenType _type; // predicted type of the current token
		protected object _value;
		protected UString _textValue;
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
			_hasEscapes = true; // don't use the "fast" code path
			base.Error(lookaheadIndex, message, args);
		}

		// Gets the text of the current token that has been parsed so far
		protected UString Text()
		{
			return CharSource.Slice(_startPosition, InputPosition - _startPosition);
		}

		protected sealed override void AfterNewline() // sealed to avoid virtual call
		{
			base.AfterNewline();
		}
		protected override bool SupportDotIndents() => true;
		
		public Les2Lexer Clone()
		{
			return (Les2Lexer)MemberwiseClone();
		}

		#region Token value parsers
		// After the generated lexer code determines the boundaries of the token, 
		// one of these methods extracts the value of the token (e.g. "17L" => (long)17)
		// There are value parsers for identifiers, numbers, and strings; certain
		// parser cores are also accessible as public static methods.

		#region String unescaping (including public UnescapeQuotedString())

		protected void UnescapeSQStringValue()
		{
			_value = _c;
			var text = Text();
			if (!_hasEscapes)
				_textValue = text.Slice(1, text.Length - 2);
			else
				_textValue = Les3Lexer.UnescapeQuotedString(ref text, Error);
		}

		protected internal static object ParseSQStringValue(UString text, Action<int, string> Error)
		{
			var sb = TempSB();
			Les3Lexer.UnescapeQuotedString(ref text, Error, sb, "\t");
			Debug.Assert(text.IsEmpty);
			if (sb.Length == 1)
				return CG.Cache(sb[0]);
			else {
				if (sb.Length == 0) {
					Error(0, Localize.Localized("Empty character literal"));
				} else {
					Error(0, Localize.Localized("Character literal has {0} characters (there should be exactly one)", sb.Length));
				}
				return sb.ToString();
			}
		}

		protected Symbol ParseBQStringValue()
		{
			UString s = UnescapeString(false);
			_textValue = default; // UnescapeString thinks it's a literal, so it sets _textValue, but it's not.
			return IdToSymbol(s);
		}

		protected UString UnescapeString(bool isTripleQuoted, bool allowExtraIndent = false)
		{
			if (SkipValueParsing)
				return "";
			if (_hasEscapes) {
				UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);
				_textValue = Les3Lexer.UnescapeQuotedString(ref original, Error, IndentString, allowExtraIndent);
				Debug.Assert(original.IsEmpty);
			} else {
				Debug.Assert(CharSource.TryGet(InputPosition - 1, '?') == CharSource.TryGet(_startPosition, '!'));
				if (isTripleQuoted)
					_textValue = CharSource.Slice(_startPosition + 3, InputPosition - _startPosition - 6).ToString();
				else
					_textValue = CharSource.Slice(_startPosition + 1, InputPosition - _startPosition - 2).ToString();
			}
			return _textValue;
		}

		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes. Supports quote types '\'', '"' and '`'.</summary>
		/// <param name="sourceText">input text</param>
		/// <param name="onError">Called in case of parsing error (unknown escape sequence or missing end quotes)</param>
		/// <param name="indentation">Inside a triple-quoted string, any text
		/// following a newline is ignored as long as it matches this string. 
		/// For example, if the text following a newline is "\t\t Foo" and this
		/// string is "\t\t\t", the tabs are ignored and " Foo" is kept.</param>
		/// <param name="les3TQIndents">Enable EC# triple-quoted string indent
		/// rules, which allow an additional one tab or three spaces of indent.
		/// (I'm leaning toward also supporting this in LES; switched on in v3)</param>
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
		[Obsolete("Please call the same method in Les3Lexer instead")]
		public static string UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, UString indentation = default(UString), bool les3TQIndents = false)
			=> Les3Lexer.UnescapeQuotedString(ref sourceText, onError, indentation, les3TQIndents);

		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes (see documentation of the first overload) into a 
		/// StringBuilder.</summary>
		[Obsolete("Please call the same method in Les3Lexer instead")]
		public static void UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString), bool les3TQIndents = false)
			=> Les3Lexer.UnescapeQuotedString(ref sourceText, onError, sb, indentation, les3TQIndents);

		/// <summary>Parses a normal or triple-quoted string whose starting quotes 
		/// have been stripped out. If triple-quote parsing was requested, stops 
		/// parsing at three quote marks; otherwise, stops parsing at a single 
		/// end-quote or newline.</summary>
		/// <returns>true if parsing stopped at one or three quote marks, or false
		/// if parsing stopped at the end of the input string or at a newline (in
		/// a string that is not triple-quoted).</returns>
		/// <remarks>This method recognizes LES and EC#-style string syntax.</remarks>
		[Obsolete("Please call the same method in Les3Lexer instead")]
		public static bool UnescapeString(ref UString sourceText, char quoteType, bool isTripleQuoted, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString), bool les3TQIndents = false) =>
			Les3Lexer.UnescapeString(ref sourceText, quoteType, isTripleQuoted, onError, sb, indentation, les3TQIndents);

		#endregion

		#region Identifier & Symbol parsing (includes @true, @false, @null, named floats) (including public ParseIdentifier())

		internal static Dictionary<UString, object> NamedLiterals = new Dictionary<UString, object>()
		{
			{ "true", true },
			{ "false", false },
			{ "null", null },
			{ "void", new @void() },
			// Old names
			{ "nan_f", float.NaN },
			{ "nan_d", double.NaN },
			{ "inf_f", float.PositiveInfinity },
			{ "inf_d", double.PositiveInfinity },
			{ "-inf_f", float.NegativeInfinity },
			{ "-inf_d", double.NegativeInfinity },
			// New names
			{ "nan.f", float.NaN },
			{ "nan.d", double.NaN },
			{ "inf.f", float.PositiveInfinity },
			{ "inf.d", double.PositiveInfinity },
			{ "-inf.f", float.NegativeInfinity },
			{ "-inf.d", double.NegativeInfinity }
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
				id = Text();

			return _value = IdToSymbol(id);
		}

		static readonly Symbol _s = (Symbol)"s";
		static readonly Symbol _c = (Symbol)"c";

		protected void UnescapeSymbolValue()
		{
			Debug.Assert(CharSource[_startPosition] == '@' && CharSource[_startPosition + 1] == '@');
			if (SkipValueParsing)
				return;

			_value = _s;
			UString original = CharSource.Slice(_startPosition + 2, InputPosition - _startPosition - 2);
			if (_hasEscapes) {
				_textValue = Les3Lexer.UnescapeQuotedString(ref original, Error);
				Debug.Assert(original.IsEmpty);
			} else if (original[0, '\0'] == '`')
				_textValue = original.Substring(1, original.Length - 2);
			else
				_textValue = original;
		}

		protected Dictionary<UString, Symbol> _idCache = new Dictionary<UString,Symbol>();
		protected Symbol IdToSymbol(UString ustr)
		{
			Symbol sym;
			if (!_idCache.TryGetValue(ustr, out sym)) {
				string str = ustr.ToString();
				_idCache[str] = sym = (Symbol) str;
			}
			return sym;
		}

		/// <summary>Parses an LES2-style identifier such as <c>foo</c>, <c>@foo</c>, 
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
			int c = source.PopFirst(out fail);
			if (c == '@') {
				// expecting: (BQString | Star(Set("[0-9a-zA-Z_'#~!%^&*-+=|<>/?:.@$]") | IdExtLetter))
				c = source.PopFirst(out fail);
				if (c == '`') {
					Les3Lexer.UnescapeString(ref source, (char)c, false, onError, parsed);
				} else {
					while (SpecialIdSet.Contains(c) || c >= 128 && char.IsLetter((char)c)) {
						parsed.Append((char)c);
						c = source.PopFirst(out fail);
					}
					checkForNamedLiteral = true;
				}
			} else if (IsIdStartChar(c)) {
				parsed.Append(c);
				for (;;) {
					c = source.PopFirst(out fail);
					if (!IsIdContChar(c))
						break;
					parsed.Append((char)c);
				}
			}
			return parsed.ToString();
		}

		#endregion

		#region Operator parsing

		protected object ParseNormalOp()
		{
			_hasEscapes = false;
			return ParseOp();
		}

		static Symbol _Backslash = GSymbol.Get(@"\");

		protected Dictionary<UString, Pair<Symbol, TokenType>> _opCache = new Dictionary<UString, Pair<Symbol, TokenType>>();
		protected Symbol ParseOp()
		{
			UString opText = Text();

			Pair<Symbol, TokenType> sym;
			if (!_opCache.TryGetValue(opText, out sym)) {
				string opStr = opText.ToString();
				_opCache[opText] = sym = GetOpNameAndType(opStr);
			}
			_value = sym.A;
			_type = sym.B;
			return sym.A;
		}

		private Pair<Symbol, TokenType> GetOpNameAndType(string op)
		{
			Debug.Assert(op.Length > 0);
			TT tt;
			Symbol name;

			if (op == "!")
				return Pair.Create(CodeSymbols.Not, TT.Not);

			// Get first and last of the operator's initial punctuation
			int length = op.Length;
			char first = op[0], last = op[length - 1];
			if (first == '\'') {
				Debug.Assert(length > 1);
				length--;
				first = op[1];
				name = (Symbol)op;
			} else {
				name = (Symbol)("'" + op);
				if (name == CodeSymbols.Dot)
					return Pair.Create(name, TT.Dot);
			}
			
			if (length >= 2 && first == last && (last == '+' || last == '-' || last == '!'))
				tt = TT.PreOrSufOp;
			else if (first == '$')
				tt = TT.PrefixOp;
			else if (last == '.' && (length == 1 || first != '.'))
				tt = TT.Dot;
			else if (last == '=' && (length == 1 || (first != '=' && first != '!' && !(length == 2 && (first == '<' || first == '>')))))
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
