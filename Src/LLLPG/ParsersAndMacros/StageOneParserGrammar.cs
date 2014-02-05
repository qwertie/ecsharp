using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Syntax.Lexing;
namespace Loyc.LLParserGenerator
{
	using TT = TokenType;
	using S = CodeSymbols;
	public enum TokenType
	{
		EOF = 0, Spaces = TokenKind.Spaces, Comment = TokenKind.Comment, Id = TokenKind.Id, Number = TokenKind.Number, String = TokenKind.String, OtherLit = TokenKind.OtherLit, Dot = TokenKind.Dot, Assignment = TokenKind.Assignment, HostOperator = TokenKind.Operator, Alt = TokenKind.Operator + 1, DotDot = TokenKind.Operator + 2, InvertSet = TokenKind.Operator + 3, Plus = TokenKind.Operator + 4, Star = TokenKind.Operator + 5, QMark = TokenKind.Operator + 6, Arrow = TokenKind.Operator + 7, And = TokenKind.Operator + 8, Not = TokenKind.Operator + 9, AndNot = TokenKind.Operator + 10, AttrKeyword = TokenKind.AttrKeyword, TypeKeyword = TokenKind.TypeKeyword, OtherKeyword = TokenKind.OtherKeyword, Greedy = TokenKind.OtherKeyword + 1, Nongreedy = TokenKind.OtherKeyword + 2, Error = TokenKind.OtherKeyword + 3, Default = TokenKind.OtherKeyword + 4, Separator = TokenKind.Separator, OtherToken = TokenKind.Other, LParen = TokenKind.LParen, RParen = TokenKind.RParen, LBrack = TokenKind.LBrack, RBrack = TokenKind.RBrack, LBrace = TokenKind.LBrace, RBrace = TokenKind.RBrace
	}
	public static class TokenTypeExt
	{
		public static TokenType Type(this Token t)
		{
			return (TokenType) t.TypeInt;
		}
	}
	internal class StageOneParser_Rewrite : BaseParser<Token>
	{
		[ThreadStatic] static StageOneParser_Rewrite _parser;
		public static LNode Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			if (_parser == null)
				_parser = new StageOneParser_Rewrite(tokenTree, file, messages);
			else {
				_parser.Reset(tokenTree, file);
				_parser.ErrorSink = messages;
			}
			return _parser.Start();
		}
		public StageOneParser_Rewrite(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink, IParsingService hostLanguage = null)
		{
			ErrorSink = messageSink;
			Reset(tokens, file, hostLanguage);
		}
		public virtual void Reset(IListSource<Token> tokens, ISourceFile file, IParsingService hostLanguage = null)
		{
			_hostLanguage = hostLanguage ?? ParsingService.Current;
			_tokensRoot = _tokens = tokens;
			_sourceFile = file;
			F = new LNodeFactory(file);
			InputPosition = 0;
		}
		protected LNodeFactory F;
		protected ISourceFile _sourceFile;
		protected IListSource<Token> _tokensRoot;
		protected IListSource<Token> _tokens;
		protected int _startTextIndex = 0;
		protected IMessageSink _messages;
		protected IParsingService _hostLanguage;
		public IMessageSink ErrorSink
		{
			get {
				return _messages;
			}
			set {
				_messages = value ?? Loyc.Utilities.MessageSink.Current;
			}
		}
		protected override int EofInt()
		{
			return (int) TokenType.EOF;
		}
		protected override int LA0Int
		{
			get {
				return _lt0.TypeInt;
			}
		}
		protected override Token LT(int i)
		{
			return _tokens.TryGet(InputPosition + i, default(Token));
		}
		protected override string ToString(int tokenType)
		{
			return ((TT) tokenType).ToString();
		}
		protected override void Error(int inputPosition, string message)
		{
			int iPos = GetTextPosition(inputPosition);
			ErrorSink.Write(MessageSink.Error, _sourceFile.IndexToLine(iPos), message);
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
		TokenType LA0
		{
			get {
				return _lt0.Type();
			}
		}
		TokenType LA(int i)
		{
			return LT(i).Type();
		}
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
			{ S.OrBits, TT.Alt
			}, { S.Div, TT.Alt
			}, { S.DotDot, TT.DotDot
			}, { S.Colon, TT.DotDot
			}, { S.NotBits, TT.InvertSet
			}, { S.Add, TT.Plus
			}, { S.Mul, TT.Star
			}, { S.QuestionMark, TT.QMark
			}, { S.Lambda, TT.Arrow
			}, { _EqGate, TT.Arrow
			}, { S.AndBits, TT.Not
			}, { S.Not, TT.AndNot
			}, { _AndNot, TT.Greedy
			}, { _Nongreedy, TT.Nongreedy
			}, { _Greedy, TT.Greedy
			}, { _Error, TT.Error
			}, { S.Error, TT.Error
			}, { _Default, TT.Default
			}, { S.Default, TT.Default
			}
		};
		static IListSource<Token> ReclassifyTokens(IListSource<Token> oldList)
		{
			InternalList<Token> newList = new InternalList<Token>(oldList.Count);
			int c = oldList.Count;
			for (int i = 0; i < c;)
				newList.Add(Reclassify(oldList, ref i));
			return newList;
		}
		private static Token Reclassify(IListSource<Token> list, ref int i)
		{
			Token token = list[i++];
			var newType = (TT) token.Kind;
			if (token.Kind != TokenKind.String && token.Kind != TokenKind.OtherLit && token.Value != null) {
				TT newType_;
				if (_tokenNameTable.TryGetValueSafe(token.Value as Symbol, out newType_))
					newType = newType_;
				else if (i < list.Count && token.EndIndex == list[i].StartIndex) {
					if (token.Value == S.AndBits && list[i].Value == S.Not) {
						i++;
						token = token.WithValue(_AndNot);
						newType = TT.AndNot;
					}
					if (token.Value == S.LE && list[i].Value == S.GT) {
						i++;
						token = token.WithValue(_EqGate);
						newType = TT.Arrow;
					}
				}
			}
			return token.WithType((int) newType);
		}
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
				return F.Braces(_hostLanguage.Parse(ch, ch.File, _messages, mode).Buffered(), p.StartIndex, endIndex);
			}
		}
		Stack<Pair<IListSource<Token>,int>> _parents = new Stack<Pair<IListSource<Token>,int>>();
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
		void Infix(ref LNode a, Symbol op, LNode b)
		{
			a = F.Call(op, a, b, a.Range.StartIndex, b.Range.EndIndex);
		}
		LNode Start()
		{
			var e = Expr();
			return e;
		}
		LNode Expr()
		{
			TT la0;
			var a = GateExpr();
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Alt) {
					var op = MatchAny();
					var b = GateExpr();
					Infix(ref a, (Symbol) op.Value, b);
				} else
					break;
			}
			return a;
		}
		LNode GateExpr()
		{
			TT la0;
			var a = SeqExpr();
			la0 = LA0;
			if (la0 == TT.Arrow) {
				var op = MatchAny();
				var b = GateExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			return a;
		}
		LNode SeqExpr()
		{
			TT la0;
			LNode seq = F.Tuple();
			for (;;) {
				switch (LA0) {
				case TT.And:
				case TT.AndNot:
				case TT.Greedy:
				case TT.Id:
				case TT.InvertSet:
				case TT.LBrace:
				case TT.LBrack:
				case TT.LParen:
				case TT.Nongreedy:
				case TT.Not:
				case TT.Number:
				case TT.OtherLit:
				case TT.String:
					{
						var next = LoopExpr();
						seq = seq.PlusArg(next);
						la0 = LA0;
						if (la0 == TT.Separator)
							Skip();
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			return seq;
		}
		LNode LoopExpr()
		{
			TT la0;
			LNode a;
			la0 = LA0;
			if (la0 == TT.Greedy || la0 == TT.Nongreedy) {
				var loopMod = MatchAny();
				a = AssignExpr();
				a = F.Call((Symbol) loopMod.Value, a, loopMod.StartIndex, a.Range.EndIndex);
			} else
				a = AssignExpr();
			la0 = LA0;
			if (la0 == TT.Star) {
				var op = MatchAny();
				a = F.Call(_SufStar, a, a.Range.StartIndex, op.EndIndex);
			} else if (la0 == TT.Plus) {
				var op = MatchAny();
				a = F.Call(_SufPlus, a, a.Range.StartIndex, op.EndIndex);
			} else if (la0 == TT.QMark) {
				var op = MatchAny();
				a = F.Call(_SufOpt, a, a.Range.StartIndex, op.EndIndex);
			}
			return a;
		}
		LNode AssignExpr()
		{
			TT la0;
			var a = PrefixExpr();
			la0 = LA0;
			if (la0 == TT.Assignment || la0 == TT.HostOperator) {
				var op = MatchAny();
				var b = AssignExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			return a;
		}
		LNode PrefixExpr()
		{
			switch (LA0) {
			case TT.InvertSet:
				{
					var op = MatchAny();
					var r = PrefixExpr();
					return F.Call(S.NotBits, r, op.StartIndex, r.Range.EndIndex);
				}
			case TT.AndNot:
			case TT.Not:
				{
					var op = MatchAny();
					var r = PrefixExprOrBraces();
					return F.Call(_AndNot, r, op.StartIndex, r.Range.EndIndex);
				}
			case TT.And:
				{
					var op = MatchAny();
					var r = PrefixExprOrBraces();
					return F.Call(S.AndBits, r, op.StartIndex, r.Range.EndIndex);
				}
			default:
				{
					var r = RangeExpr();
					return r;
				}
			}
		}
		LNode PrefixExprOrBraces()
		{
			TT la0;
			la0 = LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				return ParseBraces(lb, rb.EndIndex, true);
			} else {
				var e = PrefixExpr();
				return e;
			}
		}
		LNode RangeExpr()
		{
			TT la0;
			var a = PrimaryExpr();
			la0 = LA0;
			if (la0 == TT.DotDot) {
				var op = MatchAny();
				var b = PrimaryExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			return a;
		}
		LNode PrimaryExpr()
		{
			TT la0, la1;
			var a = Atom();
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					var op = MatchAny();
					var b = Atom();
					Infix(ref a, (Symbol) op.Value, b);
				} else if (la0 == TT.LParen) {
					if (a.Range.EndIndex == LT(0).StartIndex) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var lp = MatchAny();
							var rp = MatchAny();
							a = F.Call(a, ParseArgList(lp), a.Range.StartIndex, rp.EndIndex);
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return a;
		}
		LNode Atom()
		{
			LNode e;
			switch (LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					e = F.Id(t);
				}
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.String:
				{
					var t = MatchAny();
					e = F.Literal(t);
				}
				break;
			case TT.LParen:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					e = ParseParens(lp, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					e = ParseBraces(lb, rb.EndIndex, false);
				}
				break;
			case TT.LBrack:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.LBrack);
					Check(Try_Atom_Test0(0), "(TT.Plus|TT.Star|TT.QMark)");
					e = ParseParens(lb, rb.EndIndex);
				}
				break;
			default:
				{
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(InputPosition, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
				}
				break;
			}
			return e;
		}
		private bool Try_Atom_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Atom_Test0();
		}
		private bool Atom_Test0()
		{
			if (!TryMatch((int) TT.Plus, (int) TT.QMark, (int) TT.Star))
				return false;
			return true;
		}
	}
}
