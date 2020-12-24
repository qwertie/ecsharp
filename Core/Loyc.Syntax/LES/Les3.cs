// Currently, no IntelliSense (code completion) is available in .ecs files,
// so it can be useful to split your Lexer and Parser classes between two 
// files. In this file (the .cs file) IntelliSense will be available and 
// the other file (the .ecs file) contains your grammar code.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using System.ComponentModel;
using Loyc;               // optional (for IMessageSink, Symbol, etc.)
using Loyc.Collections;   // optional (many handy interfaces & classes)
using Loyc.Syntax.Lexing; // For BaseLexer
using Loyc.Syntax;        // For BaseParser<Token> and LNode
using Loyc.Collections.Impl;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Threading;
using System.Text;

namespace Loyc.Syntax.Les
{
	[EditorBrowsable(EditorBrowsableState.Never)] // external code shouldn't use this class, except the syntax highlighter
	public partial class Les3Lexer : BaseILexer<ICharSource, Token>, ILexer<Token>
	{
		// When using the Loyc libraries, `BaseLexer` and `BaseILexer` read character 
		// data from an `ICharSource`, which the string wrapper `UString` implements.
		public Les3Lexer(string text, string fileName = "")
			: this((UString)text, fileName, BaseLexer.LogExceptionErrorSink) { }
		public Les3Lexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0)
			: base(text, fileName, startPosition) { ErrorSink = sink; }

		/// <summary>If this flag is true, all literals except plain strings and
		/// true/false/null are stored as CustomLiteral, bypassing number parsing 
		/// so that all original characters are preserved if the output is written 
		/// back to text.</summary>
		public bool PreferCustomLiterals { get; set; }

		protected TokenType _type; // predicted type of the current token
		protected NodeStyle _style; // indicates triple-quoted string, hex or binary literal
		protected int _startPosition;
		protected UString _textValue; // part of the token that contains literal or comment text

		// Helps filter out newlines that are not directly inside braces or at the top level.
		InternalList<TokenType> _brackStack = new InternalList<TokenType>(
			new TokenType[8] { TokenType.LBrace, 0,0,0,0,0,0,0 }, 1);

		// Gets the text of the current token that has been parsed so far
		protected UString Text()
		{
			return CharSource.Slice(_startPosition, InputPosition - _startPosition);
		}
		protected UString Text(int startPosition)
		{
			return CharSource.Slice(startPosition, InputPosition - startPosition);
		}

		protected sealed override void AfterNewline() // sealed to avoid virtual call
		{
			base.AfterNewline();
		}
		protected override bool SupportDotIndents() { return true; }

		protected Dictionary<Pair<UString, bool>, Symbol> _typeMarkers = new Dictionary<Pair<UString, bool>, Symbol>();

		Symbol GetTypeMarkerSymbol(UString typeMarker, bool isNumericLiteral)
		{
			var pair = Pair.Create(typeMarker, isNumericLiteral);
			if (!_typeMarkers.TryGetValue(pair, out Symbol typeMarkerSym))
				_typeMarkers[pair] = typeMarkerSym = (Symbol)(isNumericLiteral ? "_" + (string)typeMarker : typeMarker);
			return typeMarkerSym;
		}

		[Obsolete("Please call StandardLiteralHandlers.Value.TryParse(UString, Symbol) instead")]
		public static object ParseLiteral(Symbol typeMarker, UString unescapedText, out string syntaxError)
		{
			var result = StandardLiteralHandlers.Value.TryParse(unescapedText, typeMarker);
			syntaxError = result.Right.HasValue ? result.Right.Value.Formatted : null;
			return result.Left.Or(null);
		}

		protected UString GetUnescapedString(bool hasEscapes, bool isTripleQuoted)
		{
			UString value;
			if (hasEscapes) {
				UString original = Text();
				value = UnescapeQuotedString(ref original, Error, IndentString, true);
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

		protected UString UnescapeSQStringValue(bool hasBackslash)
		{
			var text = Text();
			if (!hasBackslash)
				return text.Slice(1, text.Length - 2);
			else
				return UnescapeQuotedString(ref text, Error);
		}

		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes. Supports quote types '\'', '"' and '`'.</summary>
		/// <param name="sourceText">input text</param>
		/// <param name="onError">Called in case of parsing error (unknown escape sequence or missing end quotes)</param>
		/// <param name="indentation">Inside a triple-quoted string, any text
		/// following a newline is ignored as long as it matches this string. 
		/// For example, if the text following a newline is "\t\t Foo" and this
		/// string is "\t\t\t", the tabs are ignored and " Foo" is kept.</param>
		/// <param name="allowExtraIndent">Enable EC#/LES triple-quoted string 
		/// indent rules, which allow an additional one tab or three spaces of 
		/// indent beyond what the identation parameter specifies.</param>
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
		public static string UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, UString indentation = default(UString), bool allowExtraIndent = false)
		{
			var sb = new StringBuilder();
			UnescapeQuotedString(ref sourceText, onError, sb, indentation, allowExtraIndent);
			return sb.ToString();
		}

		/// <summary>Parses a normal or triple-quoted string that still includes 
		/// the quotes (see documentation of the first overload) into a 
		/// StringBuilder.</summary>
		public static void UnescapeQuotedString(ref UString sourceText, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString), bool allowExtraIndent = false)
		{
			bool isTripleQuoted = false, fail;
			char quoteType = (char)sourceText.PopFirst(out fail);
			if (sourceText[0, '\0'] == quoteType &&
				sourceText[1, '\0'] == quoteType)
			{
				sourceText = sourceText.Substring(2);
				isTripleQuoted = true;
			}
			if (!UnescapeString(ref sourceText, quoteType, isTripleQuoted, onError, sb, indentation, allowExtraIndent))
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
		public static bool UnescapeString(ref UString sourceText, char quoteType, bool isTripleQuoted, Action<int, string> onError, StringBuilder sb, UString indentation = default(UString), bool allowExtraIndent = false)
		{
			Debug.Assert(quoteType == '"' || quoteType == '\'' || quoteType == '`');
			bool fail;
			for (; ; )
			{
				if (sourceText.IsEmpty)
					return false;
				int i0 = sourceText.InternalStart;
				if (!isTripleQuoted)
				{
					EscapeC category = 0;
					int c = ParseHelpers.UnescapeChar(ref sourceText, ref category);
					if ((c == quoteType || c == '\n') && sourceText.InternalStart == i0 + 1)
					{
						return c == quoteType; // end of string
					}
					if ((category & EscapeC.Unrecognized) != 0)
					{
						// This backslash was ignored by UnescapeChar
						onError(i0, @"Unrecognized escape sequence '\{0}' in string".Localized(PrintHelpers.EscapeCStyle(sourceText[0, ' '].ToString(), EscapeC.Control)));
					}
					else if ((category & EscapeC.HasInvalid6DigitEscape) != 0)
						onError(i0, @"Invalid 6-digit \u code treated as 5 digits".Localized());
					sb.AppendCodePoint(c);
					if ((category & EscapeC.BackslashX) != 0 && c >= 0x80)
						DetectUtf8(sb);
					else if (c.IsInRange(0xDC00, 0xDFFF))
						RecodeSurrogate(sb);
				}
				else
				{
					// Inside triple-quoted string
					int c;
					if (sourceText[2, '\0'] == '/')
					{
						// Detect escape sequence
						c = ParseHelpers.UnescapeChar(ref sourceText);
						if (sourceText.InternalStart > i0 + 1)
							G.Verify(sourceText.PopFirst(out fail) == '/');
					}
					else
					{
						c = sourceText.PopFirst(out fail);
						if (fail)
							return false;
						if (c == quoteType)
						{
							if (sourceText[0, '\0'] == quoteType &&
								sourceText[1, '\0'] == quoteType)
							{
								sourceText = sourceText.Substring(2);
								// end of string
								return true;
							}
						}
						if (c == '\r' || c == '\n')
						{
							// To ensure platform independency of source code, CR and 
							// CR-LF become LF.
							if (c == '\r')
							{
								c = '\n';
								var copy = sourceText.Clone();
								if (sourceText.PopFirst(out fail) != '\n')
									sourceText = copy;
							}
							// Inside a triple-quoted string, the indentation following a newline 
							// is ignored, as long as it matches the indentation of the first line.
							UString src = sourceText, ind = indentation;
							int sp;
							while ((sp = src.PopFirst(out fail)) == ind.PopFirst(out fail) && !fail)
								sourceText = src;
							if (allowExtraIndent && fail)
							{
								// Allow an additional one tab or three spaces when initial indent matches
								if (sp == '\t')
									sourceText = src;
								else if (sp == ' ')
								{
									sourceText = src;
									if (src.PopFirst(out fail) == ' ')
										sourceText = src;
									if (src.PopFirst(out fail) == ' ')
										sourceText = src;
								}
							}
						}
					}

					sb.AppendCodePoint(c);
				}
			}
		}

		// This function is called after every "\xNN" escape where 0xNN > 0x80.
		// Now, in LESv3, the input must be valid UTF-8, but strings can contain
		// raw bytes as \x escape sequences. We must evaluate sequences of such
		// characters as UTF-8 and figure out if they're valid or invalid.
		// They can write "\xE2\x82\xAC" = "\u20AC" = "€", and also invalid 
		// bytes or byte sequences. An invalid byte like "\xFF" is recoded as an
		// invalid single surrogate like 0xDCFF. 
		//   This function is in charge of performing that translation. See also:
		// https://github.com/sunfishcode/design/pull/3#issuecomment-236777361
		// Note: this function is shared by LESv2 too, because, who cares, why not.
		static void DetectUtf8(StringBuilder sb)
		{
			int minus1 = sb[sb.Length - 1];
			Debug.Assert(minus1.IsInRange((char)128, (char)255));
			minus1 = sb[sb.Length - 1] = (char)(minus1 | 0xDC00);
			if (sb.Length > 1 && minus1.IsInRange(0xDC80, 0xDCBF))
			{
				int minus2 = sb[sb.Length - 2];
				if (minus2.IsInRange(0xDCC0, 0xDCDF))
				{
					// 2-byte UTF8 character detected; decode into UTF16
					int c = ((minus2 & 0x1F) << 6) | (minus1 & 0x3F);
					if (c > 0x7F)
					{ // ignore overlong characters
						sb.Remove(sb.Length - 1, 1);
						sb[sb.Length - 1] = (char)c;
					}
				}
				else if (sb.Length > 2 && minus2.IsInRange(0xDC80, 0xDCBF))
				{
					int minus3 = sb[sb.Length - 3];
					if (minus3.IsInRange(0xDCE0, 0xDCEF))
					{
						// 3-byte UTF8 character detected; decode into UTF16 unless
						// the character is in the low surrogate range 0xDC00..0xDFFF.
						// This avoids collisions with the 0xDCxx space reserved for 
						// encodings of arbitrary bytes, and and also avoids 
						// translating UTF-8 encodings of UTF-16 surrogate pairs, 
						// which wouldn't round trip, e.g. \xED\xA0\xBD\xED\xB2\xA9
						// !=> \uD83D\uDCA9 (UTF16) => \u1F4A9 => \xF0\x9F\x92\xA9 (UTF8).
						int c = ((minus3 & 0xF) << 12) | ((minus2 & 0x3F) << 6) | (minus1 & 0x3F);
						if (c > 0x7FF && !c.IsInRange(0xDC00, 0xDFFF))
						{ // ignore overlong characters
							sb.Remove(sb.Length - 2, 2);
							sb[sb.Length - 1] = (char)c;
						}
					}
					else if (sb.Length > 3 && minus3.IsInRange(0xDC80, 0xDCBF))
					{
						int minus4 = sb[sb.Length - 4];
						if (minus4.IsInRange(0xDCF0, 0xDCF7))
						{
							// 4-byte UTF8 character detected; decode into UTF16 surrogate pair
							int c = ((minus4 & 0x7) << 18) | ((minus3 & 0x3F) << 12) | ((minus2 & 0x3F) << 6) | (minus1 & 0x3F);
							if (c > 0xFFFF)
							{ // ignore overlong characters
								sb.Remove(sb.Length - 4, 4);
								sb.AppendCodePoint(c);
							}
						}
					}
				}
			}
		}

		// To prevent collisions between the single invalid UTF-8 byte 0xFF,
		// which is coded as 0xDCFF and the three-byte sequence represented
		// by \uDCFF, and also to allow round-tripping of UTF8 encodings of 
		// UTF16 surrogates, this function's job is to treat "low" surrogates 
		// in range 0xDC00 to 0xDFFF as if they were three UTF-8 bytes 
		// recoded individually:
		//     \uDCFF => 0xED 0xB3 0xBF => 0xDCED 0xDCB3 0xDCBF
		// by avoiding collisions, the UTF-16 output from this lexer can be 
		// used to reconstruct the byte stream it represents in all cases.
		// If the final LESv3 spec disallows individual surrogate characters
		// then we can just print an error instead of doing this trick.
		static void RecodeSurrogate(StringBuilder sb)
		{
			int c = sb[sb.Length - 1];
			Debug.Assert(c.IsInRange(0xDC00, 0xDFFF));
			int b1 = 0xE0 | (c >> 12);
			int b2 = 0x80 | ((c >> 6) & 0x3F);
			int b3 = 0x80 | (c & 0x3F);
			sb[sb.Length - 1] = (char)(b1 | 0xDC00);
			sb.Append((char)(b2 | 0xDC00));
			sb.Append((char)(b3 | 0xDC00));
		}

		#region Operator parsing

		protected Dictionary<UString, Pair<Symbol, TokenType>> _opCache = new Dictionary<UString, Pair<Symbol, TokenType>>();

		protected Symbol ParseOp(out TokenType type)
		{
			UString opText = Text();

			Pair<Symbol, TokenType> symAndType;
			if (!_opCache.TryGetValue(opText, out symAndType))
				_opCache[opText] = symAndType = GetOperatorNameAndType(opText);
			type = symAndType.B;
			return symAndType.A;
		}

		/// <summary>Under the assumption that <c>op</c> is a sequence of punctuation 
		/// marks that forms a legal operator, this method decides its TokenType.</summary>
		public static TokenType GetOperatorTokenType(UString op)
		{
			Debug.Assert(op.Length > 0);

			int length = op.Length;
			// Get first and last of the operator's initial punctuation
			char first = op[0], last = op[length - 1];

			if (length == 1) {
				if (first == '!')
					return TokenType.Not;
				if (first == ':')
					return TokenType.Colon;
				if (first == '.')
					return TokenType.Dot;
			}

			Debug.Assert(first != '\'');
			Symbol name = (Symbol)("'" + op);
			
			if (length >= 2 && first == last && (last == '+' || last == '-' || last == '!'))
				return TokenType.PreOrSufOp;
			else if (first == '$')
				return TokenType.PrefixOp;
			else if (last == '.' && first != '.')
				return TokenType.Dot;
			else if (last == '=' && (length == 1 || (first != '=' && first != '!' && !(length == 2 && (first == '<' || first == '>')))))
				return TokenType.Assignment;
			else
				return TokenType.NormalOp;
		}

		static Pair<Symbol, TokenType> GetOperatorNameAndType(UString op)
		{
			if (op.Length == 1)
			{
				char first = op[0];
				if (first == '!')
					return Pair.Create(CodeSymbols.Not, TokenType.Not);
				if (first == ':')
					return Pair.Create(CodeSymbols.Colon, TokenType.Colon);
				if (first == '.')
					return Pair.Create(CodeSymbols.Dot, TokenType.Dot);
			}
			return Pair.Create((Symbol)("'" + op), GetOperatorTokenType(op));
		}

		#endregion
	}

	[EditorBrowsable(EditorBrowsableState.Never)] // used only by syntax highlighter
	public partial class Les3Parser : BaseParserForList<Token, int>
	{
		LNodeFactory F;

		public Les3Parser(IList<Token> list, ISourceFile file, IMessageSink sink, int startIndex = 0)
			: base(list, prev => new Token((int)TokenType.EOF, prev.EndIndex, 0, null), (int)TokenType.EOF, file, startIndex) { ErrorSink = sink; }

		public new IMessageSink ErrorSink
		{
			get { return base.ErrorSink; }
			set { base.ErrorSink = value; F.ErrorSink = base.ErrorSink; }
		}

		public void Reset(IList<Token> list, ISourceFile file, int startIndex = 0)
		{
			Reset(list, default(Token), file, startIndex);
		}
		protected override void Reset(IList<Token> list, Func<Token, Token> getEofToken, int eof, ISourceFile file, int startIndex = 0)
		{
			CheckParam.IsNotNull("file", file);
			base.Reset(list, getEofToken, eof, file, startIndex);
			F = new LNodeFactory(file);
		}

		/// <summary>Top-level rule: expects a sequence of statements followed by EOF</summary>
		/// <param name="separator">If there are multiple expressions, the Value of 
		/// this Holder is set to the separator between them: Comma or Semicolon.</param>
		public IEnumerable<LNode> Start(Holder<TokenType> separator)
		{
			_listContextName = "top level";
			foreach (var stmt in ExprListLazy(separator))
				yield return stmt;
			Match((int) EOF, (int) separator.Value);
			_sharedTrees = null;
		}

		// Method required by base class for error messages
		protected override string ToString(int type)
		{
			switch ((TokenType)type) {
				case TokenType.LParen: return "'('";
				case TokenType.RParen: return "')'";
				case TokenType.LBrack: return "'['";
				case TokenType.RBrack: return "']'";
				case TokenType.LBrace: return "'{'";
				case TokenType.RBrace: return "'}'";
				case TokenType.Comma:  return "','";
				case TokenType.Semicolon: return "';'";
			}
			return ((TokenType)type).ToString().Localized();
		}

		// This is virtual so that a syntax highlighter can easily override and colorize it
		protected virtual LNode MarkSpecial(LNode n)
		{
			return n.SetBaseStyle(NodeStyle.Special);
		}
		// This is virtual so that a syntax highlighter can easily override and colorize it
		protected virtual LNode MarkCall(LNode n)
		{
			return n.SetBaseStyle(NodeStyle.PrefixNotation);
		}

		protected LNode MissingExpr(Token tok, string error = null, bool afterToken = false)
		{
			int startIndex = afterToken ? tok.EndIndex : tok.StartIndex;
			LNode missing = F.Id(S.Missing, startIndex, tok.EndIndex);
			if (error != null)
				ErrorSink.Write(Severity.Error, missing.Range, error);
			return missing;
		}

		protected Les3PrecedenceMap _precMap = Les3PrecedenceMap.Default;

		protected Precedence PrefixPrecedenceOf(Token t)
		{
			var prec = _precMap.Find(OperatorShape.Prefix, t.Value);
			if (prec == LesPrecedence.Illegal)
				ErrorSink.Write(Severity.Error, F.Id(t),
					"Operator `{0}` cannot be used as a prefix operator", t.Value);
			return prec;
		}

		// Note: continuators cannot be used as binary operator names
		internal static readonly HashSet<Symbol> ContinuatorOps = new HashSet<Symbol> {
			(Symbol)"#else",  (Symbol)"#elsif",  (Symbol)"#elseif",
			(Symbol)"#catch", (Symbol)"#except", (Symbol)"#finally",
			(Symbol)"#while", (Symbol)"#until",  (Symbol)"#initially",
			(Symbol)"#plus",  (Symbol)"#using",
		};

		internal static readonly Dictionary<object, Symbol> Continuators =
			ContinuatorOps.ToDictionary(kw => (object)(Symbol)kw.Name.Substring(1), kw => kw);

		bool CanParse(Precedence context, int li, out Precedence prec)
		{
			var opTok = LT(li);
			if (opTok.Type() == TokenType.Id) {
				var opTok2 = LT(li + 1);
				if (opTok2.Type() == TokenType.NormalOp && opTok.EndIndex == opTok2.StartIndex)
					prec = _precMap.Find(OperatorShape.Infix, opTok2.Value);
				else {
					// Oops, LesPrecedenceMap doesn't yet support non-single-quote ops
					// (because it's shared with LESv2 which doesn't have them)
					// TODO: improve performance by avoiding this concat
					prec = _precMap.Find(OperatorShape.Infix, (Symbol)("'" + opTok.Value.ToString()));
				}
			} else
				prec = _precMap.Find(OperatorShape.Infix, opTok.Value);
			bool result = context.CanParse(prec);
			if (!context.CanMixWith(prec))
				Error(li, "Operator \"{0}\" cannot be mixed with the infix operator to its left. Add parentheses to clarify the code's meaning.", LT(li).Value);
			return result;
		}
	}
}
