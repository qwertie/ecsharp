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

		protected UString ParseStringValue(bool parseNeeded, bool isTripleQuoted)
		{
			UString value;
			if (parseNeeded) {
				UString original = Text();
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

		protected UString UnescapeSQStringValue(bool parseNeeded)
		{
			var text = Text();
			if (!parseNeeded)
				return text.Slice(1, text.Length - 2);
			else
				return Les2Lexer.UnescapeQuotedString(ref text, Error);
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
			: base(list, default(Token), file, startIndex) { ErrorSink = sink; }

		public new IMessageSink ErrorSink
		{
			get { return base.ErrorSink; }
			set { base.ErrorSink = value; F.ErrorSink = base.ErrorSink; }
		}

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
