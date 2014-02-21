using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Syntax.Les;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Syntax.Lexing;

namespace Loyc.LLParserGenerator
{
#if true
	using TT = TokenType;
	using S = CodeSymbols;

	/// <summary>Token types in LLLPG's stage one parser.</summary>
	/// <remarks>Note: Spaces and Comments should never be encountered by the 
	/// parser. I assume they are filtered out of all Token Literals.</remarks>
	public enum TokenType
	{
		EOF         = 0,

		Spaces      = TokenKind.Spaces,
		Comment     = TokenKind.Comment,
		Id          = TokenKind.Id,
		Number      = TokenKind.Number,
		String      = TokenKind.String,
		OtherLit    = TokenKind.OtherLit,
		Dot         = TokenKind.Dot,
		Assignment  = TokenKind.Assignment, // = += :=
		
		HostOperator= TokenKind.Operator,
		Alt         = TokenKind.Operator + 1, // |
		Slash       = TokenKind.Operator + 2, // /
		DotDot      = TokenKind.Operator + 3, // .. or :
		InvertSet   = TokenKind.Operator + 4, // ~
		Plus        = TokenKind.Operator + 5, // +
		Star        = TokenKind.Operator + 6, // *
		QMark       = TokenKind.Operator + 7, // ?
		Arrow       = TokenKind.Operator + 8, // => <=>
		And         = TokenKind.Operator + 9, // &
		Not         = TokenKind.Operator + 10, // !
		AndNot      = TokenKind.Operator + 11, // &!
		Minus       = TokenKind.Operator + 12, // -

		AttrKeyword = TokenKind.AttrKeyword,
		TypeKeyword = TokenKind.TypeKeyword,
		OtherKeyword= TokenKind.OtherKeyword,
		Greedy      = TokenKind.OtherKeyword + 1,
		Nongreedy   = TokenKind.OtherKeyword + 2,
		Error       = TokenKind.OtherKeyword + 3,
		Default     = TokenKind.OtherKeyword + 4,
		
		Separator   = TokenKind.Separator,
		OtherToken  = TokenKind.Other,
		LParen      = TokenKind.LParen,
		RParen      = TokenKind.RParen,
		LBrack      = TokenKind.LBrack,
		RBrack      = TokenKind.RBrack,
		LBrace      = TokenKind.LBrace,
		RBrace      = TokenKind.RBrace,
	}
	public static class TokenTypeExt
	{
		public static TokenType Type(this Token t) { return (TokenType)t.TypeInt; }
	}

	/// <summary>
	/// Parses a <see cref="TokenTree"/> from LES or EC# into an LNode.
	/// </summary>
	/// <remarks>
	/// LLLPG grammars are parsed in two stages. First, a token tree is parsed into 
	/// an <see cref="LNode"/>, e.g. <c>a b | c*</c> is parsed into the tree 
	/// <c>(a, b) | @#suf*(c)</c>. This class handles the first stage. The second 
	/// stage (<see cref="StageTwoParser"/>) is that the <see cref="LNode"/> is 
	/// parsed into a tree of <see cref="Pred"/> objects.
	/// <para/>
	/// There is no lexer for LLLPG; LLLPG relies on the host language for lexing.
	/// This class contains code to change the Type() of each token to one of the 
	/// types that LLLPG supports (see the #region "Token reclassification").
	/// <para/>
	/// Stage 1 does not transform the entire token tree, because many of the 
	/// tokens must be interpreted by the host language. Luckily such tokens are 
	/// always in parenthesis or braces and it's not hard to avoid touching them.
	/// </remarks>
	internal partial class StageOneParser : BaseParser<Token>
	{
		[ThreadStatic]
		static StageOneParser _parser;

		public static LNode ParseTokenTree(TokenTree tokens, IMessageSink sink, LNode basis)
		{
			return StageOneParser.Parse(tokens, tokens.File, sink);
		}
		public static LNode Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			if (_parser == null)
				_parser = new StageOneParser(tokenTree, file, messages);
			else {
				_parser.Reset(tokenTree, file);
				_parser.ErrorSink = messages;
			}
			return _parser.Parse();
		}

		public StageOneParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink, IParsingService hostLanguage = null)
		{
			ErrorSink = messageSink;
			Reset(tokens, file, hostLanguage);
		}

		public virtual void Reset(IListSource<Token> tokens, ISourceFile file, IParsingService hostLanguage = null)
		{
			_hostLanguage = hostLanguage ?? ParsingService.Current;
			_tokensRoot = _tokens = ReclassifyTokens(tokens);
			_sourceFile = file;
			F = new LNodeFactory(file);
			InputPosition = 0; // reads LT(0)
		}

		protected LNodeFactory F;
		protected ISourceFile _sourceFile;
		protected IListSource<Token> _tokensRoot;
		protected IListSource<Token> _tokens;
		// index into source text of the first token at the current depth (inside 
		// parenthesis, etc.). Used if we need to print an error inside empty {} [] ()
		protected int _startTextIndex = 0;
		protected IMessageSink _messages;
		protected IParsingService _hostLanguage;

		public IMessageSink ErrorSink
		{
			get { return _messages; } 
			set { _messages = value ?? Loyc.MessageSink.Current; }
		}
		
		#region Methods required by BaseLexer & LLLPG
		
		protected override int EofInt() { return (int)TokenType.EOF; } // zero
		protected override int LA0Int { get { return _lt0.TypeInt; } }
		protected override Token LT(int i) { return _tokens.TryGet(InputPosition + i, default(Token)); }
		protected override string ToString(int tokenType) { return ((TT)tokenType).ToString(); }
		protected override void Error(int li, string message)
		{
			int iPos = GetTextPosition(InputPosition + li);
			ErrorSink.Write(Severity.Error, _sourceFile.IndexToLine(iPos), message);
		}
		protected int GetTextPosition(int tokenPosition)
		{
			bool fail;
			Token token = _tokens.TryGet(tokenPosition, out fail);
			if (!fail)
				return token.StartIndex;
			else if (_tokens.Count == 0 || tokenPosition < 0)
				return _startTextIndex;
			else
				return _tokens[_tokens.Count - 1].EndIndex;
		}
		
		TokenType LA0 { get { return _lt0.Type(); } }
		TokenType LA(int i) { return LT(i).Type(); }
		
		#endregion
		
		#region Token reclassification

		static readonly Symbol _EqGate = GSymbol.Get("#<=>");
		static readonly Symbol _AndNot = GSymbol.Get("#&!");
		static readonly Symbol _Nongreedy = GSymbol.Get("nongreedy");
		static readonly Symbol _Greedy = GSymbol.Get("greedy");
		static readonly Symbol _Default = GSymbol.Get("default");
		static readonly Symbol _Error = GSymbol.Get("error");
		static readonly Symbol _SufStar = GSymbol.Get("#suf*");
		static readonly Symbol _SufPlus = GSymbol.Get("#suf+");
		static readonly Symbol _SufOpt = GSymbol.Get("#suf?");
		
		static readonly Dictionary<Symbol,TT> _tokenNameTable = new Dictionary<Symbol,TT> {
			{S.OrBits,   TT.Alt},
			{S.Div,      TT.Slash},
			{S.DotDot,   TT.DotDot},
			{S.Colon,    TT.DotDot},
			{S.NotBits,  TT.InvertSet},
			{S.Add,      TT.Plus},
			{S.Sub,      TT.Minus},
			{S.Mul,      TT.Star},
			{S.QuestionMark, TT.QMark},
			{S.Lambda,   TT.Arrow},
			{_EqGate,    TT.Arrow},
			{S.AndBits,  TT.And},
			{S.Not,      TT.AndNot},
			{_AndNot,    TT.Greedy},
			{_Nongreedy, TT.Nongreedy},
			{_Greedy,    TT.Greedy},
			{_Error,     TT.Error},
			{S.Error,    TT.Error},
			{_Default,   TT.Default},
			{S.Default,  TT.Default},
		};
		
		static IListSource<Token> ReclassifyTokens(IListSource<Token> oldList)
		{
			// Only reclassifies tokens on the current level. Child tokens are untouched.
			InternalList<Token> newList = new InternalList<Token>(oldList.Count);
			int c = oldList.Count;
			for (int i = 0; i < c;)
				newList.Add(Reclassify(oldList, ref i));
			return newList;
		}
		private static Token Reclassify(IListSource<Token> list, ref int i)
		{
			Token token = list[i++];
			var newType = (TT)token.Kind;
			if (token.Kind != TokenKind.String &&
				token.Kind != TokenKind.OtherLit &&
				token.Value != null) do
			{
				TT newType_;
				if (i < list.Count && token.EndIndex == list[i].StartIndex) {
					// Detect the two-token combinations &!, <=>, :=
					if (token.Value == S.AndBits && list[i].Value == S.Not) {
						i++;
						token = token.WithValue(_AndNot);
						newType = TT.AndNot;
						break;
					}
					if (token.Value == S.LE && list[i].Value == S.GT) {
						i++;
						token = token.WithValue(_EqGate);
						newType = TT.Arrow;
						break;
					}
					if (token.Value == S.Colon && list[i].Value == S.Set) {
						i++;
						token = token.WithValue(S.QuickBindSet);
						newType = TT.Assignment;
						break;
					}
				}
				if (_tokenNameTable.TryGetValueSafe(token.Value as Symbol, out newType_))
					newType = newType_;
			} while(false);
			return token.WithType((int)newType);
		}

		#endregion
		
		#region Tree structure handling

		RVList<LNode> ParseArgList(Token group)
		{
			var ch = group.Children;
			if (ch != null)
				return new RVList<LNode>(_hostLanguage.Parse(ch, ch.File, _messages, ParsingService.Exprs));
			else
				return RVList<LNode>.Empty;
		}
		
		LNode ParseParens(Token p, int endIndex)
		{
			var ch = p.Children;
			if (ch == null)
				return F.Call(S.Tuple);
			else {
				var newList = ReclassifyTokens(ch);
				G.Verify(Down(newList));
				return Up(Expr());
			}
		}
		
		LNode ParseBraces(Token p, int endIndex, bool singleExpr)
		{
			var ch = p.Children;
			if (ch == null)
				return F.Braces(RVList<LNode>.Empty, p.StartIndex, endIndex);
			else {
				var mode = singleExpr ? ParsingService.Exprs : ParsingService.Stmts;
				return F.Braces(
					_hostLanguage.Parse(ch, ch.File, _messages, mode).Buffered(), 
					p.StartIndex, endIndex);
			}
		}

		#endregion

		#region Down & Up
		// These are used to traverse into token subtrees, e.g. given w=(x+y)*z, 
		// the outer token list is w=()*z, and the 3 tokens x+y are children of '('
		// So the parser calls something like Down(lparen) to begin parsing inside,
		// then it calls Up() to return to the parent tree.

		Stack<Pair<IListSource<Token>, int>> _parents = new Stack<Pair<IListSource<Token>, int>>();

		protected bool Down(int li)
		{
			return Down(LT(li).Children);
		}
		protected bool Down(IListSource<Token> children)
		{
			if (children != null) {
				_parents.Push(Pair.Create(_tokens, InputPosition));
				_tokens = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		protected T Up<T>(T value)
		{
			Up();
			return value;
		}
		protected void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokens = pair.A;
			InputPosition = pair.B;
		}

		#endregion
	}


#else
	// OLD VERSION
	using TT = Loyc.Syntax.Les.TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;

	/// <summary>
	/// Parses a <see cref="TokenTree"/> from LES or EC# into an LNode.
	/// </summary>
	/// <remarks>
	/// LLLPG grammars are parsed in two stages. First, a token tree is parsed into 
	/// an <see cref="LNode"/>, e.g. <c>a b | c*</c> is parsed into the tree 
	/// <c>#seq(a, b) | #*(c)</c>. This class handles the first stage. The second 
	/// stage is that the <see cref="LNode"/> is parsed into a tree of 
	/// <see cref="Pred"/> objects.
	/// <para/>
	/// I was going to use LLLPG to generate a parser for this, but writing 
	/// bootstrap grammars in C# is rather laborious, so I found a shortcut; I 
	/// realized that if I modified the LES parser slightly, I would be able to
	/// write a derived class of <see cref="LesParser"/> that would parse LLLPG's
	/// input language: two different languages with one parser!
	/// <para/>
	/// The first thing I did was to change superexpression parsing so that a
	/// "superexpression" is treated more like an ordinary operator. Previously,
	/// there was a SuperExpr rule that simply called Expr repeatedly. I changed
	/// this so that superexpression parsing occurs inside Expr itself, alongside
	/// the other operators, which allowed me to control its precedence.
	/// <para/>
	/// If you think about "juxtaposition" as a special operator ɸ, then it can be 
	/// assigned a precedence like all the other operators. So "x y | z" can be 
	/// parsed as <c>x ɸ (y | z)</c> if juxtaposition has low precedence (normal 
	/// LES), or <c>(x ɸ y) | z</c> if juxtaposition has higher precedence (as in 
	/// LLLPG). So in <see cref="LesPrecedence"/> I added a precedence value for 
	/// SuperExpr, which is, of course, lower than all other operators; but now
	/// LesParser doesn't use this value directly, it stores it in the P_SuperExpr
	/// field so that the derived class can change its precedence.
	/// <para/>
	/// There was already a method <c>MakeSuperExpr</c> to interpret the 
	/// juxtaposition operator, I just changed it to a virtual method. I also 
	/// created some new virtual methods for handling calls and braces, again so
	/// I could override them.
	/// <para/>
	/// <see cref="LesParser"/> already contains precedence tables; by changing
	/// them, we can change the precedence rules.
	/// <para/>
	/// The final problem is the input tokens. The original LES parser expects a
	/// certain set of token types produced by the LES lexer. There are two problems 
	/// to overcome:
	/// <ol>
	/// <li>The LES lexer has built-in knowledge about which operators are prefix
	/// and/or suffix operators, and assigns <see cref="TokenType"/> values such as
	/// NormalOp, PreSufOp, SuffixOp and PrefixOp to indicate what kind of operator
	/// a token can be. We need to change these token types for certain operators,
	/// e.g. we need to "reprogram" *, + and ? to be suffix operators.</li>
	/// <li>The Ecs lexer produces token type codes that are almost completely
	/// different, so we can't feed EC# tokens directly into the LES parser. EC#
	/// and LES both share the same <see cref="TokenKind"/> though, and we can use
	/// that to derive an LES TokenType artificially.</li>
	/// </ol>
	/// We can handle both cases in the same way, by using TokenKind to choose an
	/// TokenType, with special handling for operators like *, +, |, /. We can also
	/// handle LLLPG's greedy, nongreedy, default and error keywords by labeling 
	/// them <c>PrefixOp</c>.
	/// <para/>
	/// We should not transform the entire token tree. Many of the tokens are to be
	/// interpreted by the host language; luckily such tokens are always in 
	/// parenthesis or braces and it's not hard to avoid touching them.
	/// </remarks>
	internal class StageOneParser : LesParser
	{
		[ThreadStatic]
		static StageOneParser _parser;
		public static IListSource<LNode> Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			if (_parser == null)
				_parser = new StageOneParser(tokenTree, file, messages);
			else {
				_parser.Reset(tokenTree, file);
				_parser.MessageSink = messages;
			}
			return _parser.ParseStmtsGreedy();
		}
		public static LNode ParseTokenTree(TokenTree tokens, IMessageSink sink, LNode basis)
		{
			var list = StageOneParser.Parse(tokens, tokens.File, sink);
			if (list.Count == 1 && list[0].Calls(S.Tuple))
				return list[0];
			else
				return LNode.Call(S.Tuple, new RVList<LNode>(list), basis.Range);
		}

		public StageOneParser(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages) : base(tokenTree, file, messages)
		{
			_currentLanguage = ParsingService.Current;
			_suffixPrecedence = PredefinedSuffixPrecedence.AsMutable();
			_prefixPrecedence = PredefinedPrefixPrecedence.AsMutable();
			_infixPrecedence = PredefinedInfixPrecedence.AsMutable();
			P_SuperExpr = P.AndBits;
		}

		public override void Reset(IListSource<Token> tokens, ISourceFile file)
		{
			base.Reset(ReclassifyTokens(tokens), file);
		}

		IParsingService _currentLanguage;

		public LNode Parse()
		{
			var buffer = ParseStmtsLazy().Buffered();
			if (buffer.Count == 0)
				return F.Call(S.Tuple);
			else if (buffer.Count == 1)
				return buffer[0];
			else
				return F.Call(S.Tuple, buffer);
		}

		#region Overrides

		protected override LNode MakeSuperExpr(LNode lhs, ref LNode primary, RVList<LNode> rhs)
		{
			rhs.Insert(0, lhs);
			int start = lhs.Range.StartIndex, end = rhs.Last.Range.EndIndex;
			return F.Call(S.Tuple, rhs, start, end);
		}
		protected override LNode ParseBraces(Token t, int endIndex)
		{
			var ch = t.Children;
			if (ch == null || ch.Count == 0)
				return F.Braces(RVList<LNode>.Empty, t.StartIndex, endIndex);
			else {
				var list = new RWList<LNode>(_currentLanguage.Parse(ch, ch.File, MessageSink, ParsingService.Stmts));
				if (list.Last.Calls(S.Result, 1))
					list.Last = list.Last.Args[0]; // For EC#: interpret {foo} as {foo;} instead
				return F.Braces(list.ToRVList(), t.StartIndex, endIndex);
			}
		}

		protected override LNode ParseParens(Token t, int endIndex)
		{
			var ch = t.Children;
			if (ch == null)
				return F.Call(S.Tuple);
			else {
				var newList = ReclassifyTokens(ch);
				G.Verify(Down(newList));
				RWList<LNode> list = new RWList<LNode>();
				ExprList(ref list);
				if (list.Count == 1)
					return Up(list[0]);
				else {
					return Up(F.Call(S.Tuple, list.ToRVList(), t.StartIndex, endIndex));
				}
			}
		}

		protected IListSource<LNode> HostLangExprListInside(Token group)
		{
			var children = group.Children;
			if (Down(children))
				return Up(_currentLanguage.Parse(children, children.File, MessageSink, ParsingService.Exprs));
			return EmptyList<LNode>.Value;
		}
		protected override LNode ParseCall(Token target, Token paren, int endIndex)
		{
			Debug.Assert(target.TypeInt == (int)TT.Id);
			int start = target.StartIndex;
			return F.Call((Symbol)target.Value, HostLangExprListInside(paren), start, endIndex);
		}
		protected override LNode ParseCall(LNode target, Token paren, int endIndex)
		{
			int start = target.Range.StartIndex;
			return F.Call(target, HostLangExprListInside(paren), start, endIndex);
		}

		#endregion

		#region Code for remapping operators

		static TT[] _kindToTT = get_kindToTT();
		static TT[] get_kindToTT()
		{
			var map = new TT[((int)TokenKind.KindMask >> 8) + 1];
			for (int i = 0; i < map.Length; i++)
				map[i] = (TT)(i << 8);
			map[(int)TokenKind.Spaces >> 8] = TT.Spaces;
			return map;
		}
		static TT SwitchType(TokenKind kind) { return _kindToTT[(int)kind >> 8]; }

		static IListSource<Token> ReclassifyTokens(IListSource<Token> oldList)
		{
			// Only reclassifies tokens on the current level. Child tokens are untouched.
			InternalList<Token> newList = new InternalList<Token>(oldList.Count);
			for (int i = 0; i < oldList.Count; i++)
				newList.Add(Reclassify(oldList[i]));
			return newList;
		}

		private static Token Reclassify(Token token)
		{
			var newType = SwitchType(token.Kind);
			if (token.Kind == TokenKind.Operator) {
				// Decide what kind of operator it is
				if (PredefinedSuffixPrecedence.ContainsKey(token.Value))
					newType = TT.SuffixOp;
				else if (token.Value == S.NotBits || token.Value == S.Not || token.Value == S.AndBits)
					newType = TT.PrefixOp;
				else
					newType = TT.NormalOp;
			} else if (token.Kind == TokenKind.Id || token.Kind == TokenKind.OtherKeyword || token.Kind == TokenKind.AttrKeyword) {
				// Is it one of our "prefix keywords"?
				if (PredefinedPrefixPrecedence.ContainsKey(token.Value)) {
					var sym = token.Value as Symbol;
					// we're only looking for words here: "greedy", "default", etc.
					if (sym != null && char.IsLetter(sym.Name[sym.Name.Length - 1]))
						newType = TT.PrefixOp;
				}
			} else if (token.Kind == TokenKind.Separator) {
				if (token.Value == S.Comma)
					newType = TT.Comma;
				else if (token.Value == S.Semicolon)
					newType = TT.Semicolon;
			}
			return token.WithType((int)newType);
		}

		static readonly Symbol _DoubleArrow = GSymbol.Get("#<=>");

		new static readonly Map<object, Precedence> PredefinedSuffixPrecedence =
			LesParser.PredefinedSuffixPrecedence.Union(
				new MMap<object, Precedence>() {
					{ S.Mul,          P.Add  }, // a..b* (precedence is chosen to be lower than ~ so that ~a..b* is (~(a..b))*)
					{ S.Add,          P.Add  }, // a..b+
					{ S.QuestionMark, P.Add  }, // a..b?
				}, true);
		new static readonly Map<object, Precedence> PredefinedInfixPrecedence =
			LesParser.PredefinedInfixPrecedence.Union(
				new MMap<object, Precedence>() {
					{ S.OrBits,       P.Or  },     // a | b
					{ S.Div,          P.Or  },     // a / b
					{ S.Lambda,       P.Compare }, // prediction => match
					{ _DoubleArrow,   P.Compare }, // prediction <=> match
					{ S.DotDot,       P.Power },   // raise .. to help ~a..b parse as ~(a..b), but not too high because -a..b should be (-a)..b
					{ S.Set,          P.Arrow },   // raise = so that (digit=~'0'..'9'? etc) parses as ((digit=~('0'..'9'))? etc)
				}, true);
		new static readonly Map<object, Precedence> PredefinedPrefixPrecedence =
			LesParser.PredefinedPrefixPrecedence.Union(
				new MMap<object, Precedence>() {
					{ S.AndBits, P.Prefix },
					{ S.NotBits, P.Multiply },     // lower ~ so that ~a..b parses as ~(a..b) instead of (~a)..b
					// 'greedy' is higher than suffix *+? so greedy (a|b)+ is parsed (greedy(a|b))+
					// it doesn't really matter if it's higher or lower, we just have to pick one...
					{ S.Lambda,  P.Compare },
					{ GSymbol.Get("greedy"), P.Prefix }, 
					{ GSymbol.Get("nongreedy"), P.Prefix },
					{ GSymbol.Get("default"), new Precedence(30) }, // higher precedence than a|b, lower than p=>q
					{ GSymbol.Get("#default"), new Precedence(30) }, // in EC# it's a keyword, hence the #
					{ GSymbol.Get("error"), new Precedence(30) },
				}, true);
		
		#endregion // remapping operators
	}
#endif
}
