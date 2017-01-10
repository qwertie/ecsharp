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

namespace Loyc.Syntax.Les
{
	[EditorBrowsable(EditorBrowsableState.Never)] // used only by syntax highlighter
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

		/// <summary>Used for syntax highlighting, which doesn't care about token values.
		/// This option causes the Token.Value to be set to a default, like '\0' for 
		/// single-quoted strings and 0 for numbers. Operator names are still parsed.</summary>
		bool SkipValueParsing { get; set; }

		protected TokenType _type; // predicted type of the current token
		protected NodeStyle _style; // indicates triple-quoted string, hex or binary literal
		protected int _startPosition;

		// Helps filter out newlines that are not directly inside braces or at the top level.
		InternalList<TokenType> _brackStack = new InternalList<TokenType>(
			new TokenType[8] { TokenType.LBrace, 0,0,0,0,0,0,0 }, 1);

		// Gets the text of the current token that has been parsed so far
		protected UString Text()
		{
			return CharSource.Slice(_startPosition, InputPosition - _startPosition);
		}

		protected sealed override void AfterNewline() // sealed to avoid virtual call
		{
			base.AfterNewline();
		}
		protected override bool SupportDotIndents() { return true; }

		object ParseLiteral2(UString typeMarker, UString parsedText, bool isNumericLiteral)
		{
			if (SkipValueParsing)
				return null;
			if (PreferCustomLiterals)
				return new CustomLiteral(parsedText.ToString(), (Symbol)typeMarker.ToString());
			else {
				string syntaxError;
				var result = ParseLiteral(typeMarker, parsedText, isNumericLiteral, out syntaxError);
				if (syntaxError != null) {
					var pos = new SourceRange(SourceFile, _startPosition, InputPosition - _startPosition);
					ErrorSink.Write(Severity.Error, pos, syntaxError);
				}
				return result;
			}
		}

		public static object ParseLiteral(UString typeMarker, UString parsedText, bool isNumericLiteral, out string syntaxError)
		{
			syntaxError = null;
			if (typeMarker.Length == 0) {
				if (isNumericLiteral) {
					// Optimize the most common case: a one-digit integer
					if (parsedText.Length == 1) {
						Debug.Assert(char.IsDigit(parsedText[0]));
						return CG.Cache((int)(parsedText[0] - '0'));
					}
					typeMarker = "number";
				} else {
					syntaxError = null;
					return parsedText.ToString();
				}
			}
			return ParseNonStringLiteral(typeMarker, parsedText, out syntaxError);
		}

		protected string ParseStringValue(bool parseNeeded, bool isTripleQuoted)
		{
			if (SkipValueParsing)
				return null;
			string value;
			if (parseNeeded) {
				UString original = CharSource.Slice(_startPosition, InputPosition - _startPosition);
				value = Les2Lexer.UnescapeQuotedString(ref original, Error, IndentString, true);
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

		protected object ParseSQStringValue(bool parseNeeded)
		{
			if (SkipValueParsing)
				return null;
			else {
				var text = Text();
				if (!parseNeeded && text.Length == 3)
					return text[1];
				else {
					var value = Les2Lexer.ParseSQStringValue(text, Error);
					if (value is string)
						return new CustomLiteral(value, (Symbol)"char");
					return value;
				}
			}
		}

		protected static Dictionary<UString, Func<UString, object>> LiteralParsers = InitLiteralParsers();
		protected static Dictionary<UString, Func<UString, object>> InitLiteralParsers()
		{
			var dict = new Dictionary<UString, Func<UString, object>>();
			Func<UString, object> i32 = s => { long n; return ParseSigned(s, out n) && (int)n == n ? (object)(int)n : null; };
			Func<UString, object> u32 = s => { long n; return ParseSigned(s, out n) && (uint)n == n ? (object)(uint)n : null; };
			Func<UString, object> i64 = s => { long n; return ParseSigned(s, out n) ? (object)(long)n : null; };
			Func<UString, object> u64 = s => { ulong n; return ParseULong(s, out n) ? (object)(ulong)n : null; };
			Func<UString, object> big = s => { BigInteger n; return ParseSigned(s, out n) ? (object)n : null; };
			Func<UString, object> f32 = s => { double n; return ParseDouble(s, out n) && n >= float.MinValue && n <= float.MaxValue ? (object)(float)n : null; };
			Func<UString, object> f64 = s => { double n; return ParseDouble(s, out n) ? (object)n : null; };
			Func<UString, object> dec = s => {
				// TODO: support decimal properly
				long n;
				double d;
				if (ParseSigned(s, out n))
					return (decimal)n;
				if (ParseDouble(s, out d))
					return (decimal)d;
				return null;
			};

			dict["u32"] = dict["u"] = dict["U"] = u32;
			dict["i32"] = i32;
			dict["u64"] = dict["ul"] = dict["uL"] = dict["UL"] = dict["Ul"] = u64;
			dict["i64"] = dict["l"] = dict["L"] = i64;
			dict["f32"] = dict["f"] = dict["F"] = f32;
			dict["f64"] = dict["d"] = dict["D"] = f64;
			dict["m"] = dec;
			dict["z"] = big;
			dict["s"] = s => (Symbol)s;
			dict["re"] = s => {
				try { return new System.Text.RegularExpressions.Regex((string)s); }
				catch { return new CustomLiteral(s, (Symbol)"re"); }
			};
			dict["number"] = GeneralNumberParser;
			dict["@@"] = ParseSingletonLiteral;
			return dict;
		}

		static object GeneralNumberParser(UString s)
		{
			long n;
			BigInteger z;
			double d;
			if (ParseSigned(s, out n)) {
				if ((int)n == n)
					return (int)n;
				else if ((uint)n == n)
					return (uint)n;
				else
					return n;
			} else if (s.Length >= 18 && ParseSigned(s, out z)) {
				// (The length check is an optimization: the shortest number that 
				// does not fit in a long is 0x8000000000000000.)
				if (z >= 0 && z <= UInt64.MaxValue)
					return (ulong)z;
				return z;
			} else if (ParseDouble(s, out d)) {
				return d;
			} else
				return null;
		}

		static bool ParseSigned(UString s, out long n)
		{
			n = 0;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			ulong u;
			if (ParseHelpers.TryParseUInt(ref s, out u, radix, flags) && s.Length == 0) {
				n = negative ? -(long)u : (long)u;
				return (long)u >= 0;
			}
			return false;
		}
		static bool ParseSigned(UString s, out BigInteger n)
		{
			n = 0;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			if (ParseHelpers.TryParseUInt(ref s, out n, radix, flags) && s.Length == 0) {
				if (negative) n = -n;
				return true;
			}
			return false;
		}
		static bool ParseULong(UString s, out ulong u)
		{
			u = 0;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0 || negative)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			return ParseHelpers.TryParseUInt(ref s, out u, radix, flags) && s.Length == 0;
		}
		private static bool ParseDouble(UString s, out double d)
		{
			d = double.NaN;
			bool negative;
			int radix = GetSignAndRadix(ref s, out negative);
			if (radix == 0)
				return false;
			var flags = ParseNumberFlag.SkipSingleQuotes | ParseNumberFlag.SkipUnderscores | ParseNumberFlag.StopBeforeOverflow;
			d = ParseHelpers.TryParseDouble(ref s, radix, flags);
			if (negative) d = -d;
			return !double.IsNaN(d) && s.Length == 0;
		}

		static int GetSignAndRadix(ref UString s, out bool negative)
		{
			negative = false;
			if (s.Length == 0)
				return 0;
			if (s[0] == '-') {
				negative = true;
				s = s.Slice(1);
				if (s.Length == 0)
					return 0;
			}
			int radix = 10;
			if (s[0] == '0') {
				var x = s[1, '\0'];
				if ((radix = x == 'x' ? 16 : x == 'b' ? 2 : 10) != 10) {
					s = s.Substring(2);
					if (s.Length == 0)
						return 0;
				}
			}
			return radix;
		}

		private static object ParseNonStringLiteral(UString typeMarker, UString parsedText, out string syntaxError)
		{
			syntaxError = null;
			Func<UString, object> parser;
			object value = null;
			if (LiteralParsers.TryGetValue(typeMarker, out parser)) {
				value = parser(parsedText);
				if (value == null)
					syntaxError = "Syntax error in '{typeMarker}' literal".Localized("typeMarker", typeMarker);
			}
			return value ?? new CustomLiteral(parsedText.ToString(), (Symbol)typeMarker.ToString());
		}

		protected static readonly Symbol _AtAt = GSymbol.Get("@@");

		#region Singleton literals (e.g. @@-inf.f)

		internal static Dictionary<UString, object> NamedLiterals = new Dictionary<UString, object>()
		{
			{ "true", G.BoxedTrue },
			{ "false", G.BoxedFalse },
			{ "null", null },
			{ "void", @void.Value },
			{ "nan.f", float.NaN },
			{ "nan.d", double.NaN },
			{ "inf.f", float.PositiveInfinity },
			{ "inf.d", double.PositiveInfinity },
			{ "-inf.f", float.NegativeInfinity },
			{ "-inf.d", double.NegativeInfinity }
		};

		private object ParseAtAtLiteral(UString text)
		{
			Debug.Assert(text.StartsWith("@@"));
			text = text.Substring(2);
			if (PreferCustomLiterals)
				return new CustomLiteral(CG.Cache(text.ToString()), _AtAt);
			return ParseSingletonLiteral(text);
		}
		static object ParseSingletonLiteral(UString text)
		{
			object value;
			if (NamedLiterals.TryGetValue(text, out value))
				return value;
			return new CustomLiteral(CG.Cache(text.ToString()), _AtAt);
		}

		#endregion

		#region Operator parsing

		protected Dictionary<UString, Pair<Symbol, TokenType>> _opCache = new Dictionary<UString, Pair<Symbol, TokenType>>();

		protected Symbol ParseOp(out TokenType type)
		{
			UString opText = Text();

			Pair<Symbol, TokenType> symAndType;
			if (!_opCache.TryGetValue(opText, out symAndType))
				_opCache[opText] = symAndType = GetOpNameAndType(opText);
			type = symAndType.B;
			return symAndType.A;
		}

		private Pair<Symbol, TokenType> GetOpNameAndType(UString op)
		{
			Debug.Assert(op.Length > 0);
			TokenType tt;
			Symbol name;

			if (op == "!")
				return Pair.Create(CodeSymbols.Not, TokenType.Not);
			if (op == ":")
				return Pair.Create(CodeSymbols.Colon, TokenType.Colon);

			// Get first and last of the operator's initial punctuation
			int length = op.Length;
			char first = op[0], last = op[length - 1];

			Debug.Assert(first != '\'');
			name = (Symbol)("'" + op);
			if (name == CodeSymbols.Dot)
				return Pair.Create(name, TokenType.Dot);
			
			if (length >= 2 && ((first == '+' && last == '+') || (first == '-' && last == '-')))
				tt = TokenType.PreOrSufOp;
			else if (first == '$')
				tt = TokenType.PrefixOp;
			else if (first == '.' && (length == 1 || last != '.'))
				tt = TokenType.Dot;
			else if (last == '=' && (length == 1 || (first != '=' && first != '!' && !(length == 2 && (first == '<' || first == '>')))))
				tt = TokenType.Assignment;
			else
				tt = TokenType.NormalOp;

			return Pair.Create(name, tt);
		}

		#endregion

		void PrintErrorIfTypeMarkerIsKeywordLiteral(object boolOrNull)
		{
			if (boolOrNull != NoValue.Value)
				ErrorSink.Write(Severity.Error, IndexToPositionObject(_startPosition), "Keyword '{0}' used as a type marker", boolOrNull);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)] // used only by syntax highlighter
	public partial class Les3Parser : BaseParserForList<Token, int>
	{
		LNodeFactory F;

		public Les3Parser(IList<Token> list, ISourceFile file, IMessageSink sink, int startIndex = 0)
			: base(list, default(Token), file, startIndex) { ErrorSink = sink; }

		public void Reset(IList<Token> list, ISourceFile file, int startIndex = 0)
		{
			Reset(list, default(Token), file, startIndex);
		}
		protected override void Reset(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0)
		{
			CheckParam.IsNotNull("file", file);
			base.Reset(list, eofToken, file, startIndex);
			F = new LNodeFactory(file);
		}

		/// <summary>Top-level rule: expects a sequence of statements followed by EOF</summary>
		/// <param name="separator">If there are multiple expressions, the Value of 
		/// this Holder is set to the separator between them: Comma or Semicolon.</param>
		public IEnumerable<LNode> Start(Holder<TokenType> separator)
		{
			foreach (var stmt in ExprListLazy(separator))
				yield return stmt;
			Match((int) EOF, (int) separator.Value);
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
			return ((TokenType)type).ToString();
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

		protected LNode MissingExpr(Token tok) { return F.Id(S.Missing, tok.StartIndex, tok.EndIndex); }

		protected LesPrecedenceMap _prec = LesPrecedenceMap.Default;

		protected new Precedence PrefixPrecedenceOf(Token t)
		{
			var prec = _prec.Find(OperatorShape.Prefix, t.Value);
			if (prec == LesPrecedence.Other)
				ErrorSink.Write(Severity.Error, F.Id(t),
					"Operator `{0}` cannot be used as a prefix operator", t.Value);
			return prec;
		}

		// Note: continuators cannot be used as binary operator names
		internal static readonly HashSet<Symbol> ContinuatorOps = new HashSet<Symbol> {
			(Symbol)"'else",  (Symbol)"'elsif",
			(Symbol)"'catch", (Symbol)"'except", (Symbol)"'finally",
			(Symbol)"'while", (Symbol)"'until",
			(Symbol)"'plus",  (Symbol)"'minus",
			(Symbol)"'or", (Symbol)"'and", (Symbol)"'but", (Symbol)"'so",
		};

		internal static readonly Dictionary<object, Symbol> Continuators =
			ContinuatorOps.ToDictionary(kw => (object)(Symbol)kw.Name.Substring(1), kw => kw);

		/// <summary>Helper method used in Expr for cases like x-2, which is an 
		/// Id token followed by a NegativeLiteral token, which needs to be 
		/// reinterpreted as a subtraction by <i>positive</i> 2.</summary>
		LNode ToPositiveLiteral(Token rhs)
		{
			Debug.Assert(rhs.Type() == TokenType.NegativeLiteral);
			object value = rhs.Value;
			if (value is CustomLiteral) {
				var cl = (CustomLiteral)value;
				value = new CustomLiteral(NegateValue(cl.Value), cl.TypeMarker);
			} else
				value = NegateValue(value);
			return F.Literal(value, rhs.StartIndex + 1, rhs.EndIndex);
		}
		static object NegateValue(object value)
		{
			// There is no easy way to do this. I investigated having the lexer keep track of
			// the original positive value in a special "Les3NegativeValue" class, but this
			// made the lexer so much more complicated that I decided it would be better to
			// back out the changes and just implement a manual type-by-type negation process.
			if (value is int) {
				var n = (int)value;
				return (n == int.MinValue ? (object)unchecked((uint)int.MinValue) : -n);
			} else if (value is long) {
				var n = (long)value;
				return (n == long.MinValue ? (object)unchecked((ulong)long.MinValue) : -n);
			} else if (value is double) {
				return -(double)value;
			} else if (value is float) {
				return -(float)value;
			} else if (value is decimal) {
				return -(decimal)value;
			} else if (value is BigInteger) {
				return -(decimal)value;
			} else if ((value as string ?? "").StartsWith("-")) {
				return value.ToString().Substring(1);
			}
			throw new InvalidOperationException("Invalid negative literal: {0}".Localized(value));
		}

		bool CanParse(Precedence context, int li, out Precedence prec)
		{
			var opTok = LT(li);
			if (opTok.Type() == TokenType.Id) {
				var opTok2 = LT(li + 1);
				if (opTok2.Type() == TokenType.NormalOp && opTok.EndIndex == opTok2.StartIndex)
					prec = _prec.Find(OperatorShape.Infix, opTok2.Value);
				else {
					// Oops, LesPrecedenceMap doesn't yet support non-single-quote ops
					// (bacause it's shared with LESv2 which doesn't have them)
					// TODO: improve performance by avoiding this concat
					prec = _prec.Find(OperatorShape.Infix, (Symbol)("'" + opTok.Value.ToString()));
				}
			} else
				prec = _prec.Find(OperatorShape.Infix, opTok.Value);
			bool result = context.CanParse(prec);
			if (!context.CanMixWith(prec))
				Error(li, "Operator \"{0}\" cannot be mixed with the infix operator to its left. Add parentheses to clarify the code's meaning.", LT(li).Value);
			return result;
		}
	}
}
