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
	/// <c>(a, b) | @`suf*`(c)</c>. This class handles the first stage. The second 
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

		static readonly Symbol _EqGate = GSymbol.Get("<=>");
		static readonly Symbol _AndNot = GSymbol.Get("&!");
		static readonly Symbol _Nongreedy = GSymbol.Get("nongreedy");
		static readonly Symbol _Greedy = GSymbol.Get("greedy");
		static readonly Symbol _Default = GSymbol.Get("default");
		static readonly Symbol _Error = GSymbol.Get("error");
		static readonly Symbol _SufStar = GSymbol.Get("suf*");
		static readonly Symbol _SufPlus = GSymbol.Get("suf+");
		static readonly Symbol _SufOpt = GSymbol.Get("suf?");
		
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
}
