// Generated from LesParserGrammar.les by LeMP custom tool. LLLPG version: 1.3.2.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;
	#pragma warning disable 162, 642
	public partial class LesParser
	{
		public RVList<LNode> ExprList(RVList<LNode> list = default(RVList<LNode>))
		{
			var endMarker = default(TT);
			return (ExprList(ref endMarker, list));
		}
		void CheckEndMarker(ref TokenType endMarker, ref Token end)
		{
			if ((endMarker != end.Type())) {
				if ((endMarker == default(TT))) {
					endMarker = end.Type();
				} else {
					Error(-1, "Unexpected separator: {0} should be {1}", ToString(end.TypeInt), ToString((int) endMarker));
				}
			}
		}
		public RVList<LNode> StmtList()
		{
			RVList<LNode> result = default(RVList<LNode>);
			var endMarker = TT.Semicolon;
			result = ExprList(ref endMarker);
			return result;
		}
		public RVList<LNode> ExprList(ref TokenType endMarker, RVList<LNode> list = default(RVList<LNode>))
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// Line 1: ( / ExprStart)
			switch ((TT) LA0) {
			case EOF:
			case TT.Comma:
			case TT.Dedent:
			case TT.RBrace:
			case TT.RBrack:
			case TT.RParen:
			case TT.Semicolon:
				{
				}
				break;
			default:
				e = ExprStart();
				break;
			}
			// Line 59: ((TT.Comma|TT.Semicolon) ( / ExprStart))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					list.Add(e ?? MissingExpr());
					CheckEndMarker(ref endMarker, ref end);
					// Line 62: ( / ExprStart)
					switch ((TT) LA0) {
					case EOF:
					case TT.Comma:
					case TT.Dedent:
					case TT.RBrace:
					case TT.RBrack:
					case TT.RParen:
					case TT.Semicolon:
						// line 62
						e = null;
						break;
					default:
						e = ExprStart();
						break;
					}
				} else
					break;
			}
			if ((e != null || end.Type() == TT.Comma))
				list.Add(e ?? MissingExpr());
			return list;
		}
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// Line 1: ( / ExprStart)
			la0 = (TT) LA0;
			if (la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon) {
			} else
				e = ExprStart();
			// Line 69: ((TT.Comma|TT.Semicolon) ( / ExprStart))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					// line 70
					yield
					return e ?? MissingExpr();
					CheckEndMarker(ref endMarker.Value, ref end);
					// Line 72: ( / ExprStart)
					la0 = (TT) LA0;
					if (la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon)
						// line 72
						e = null;
					else
						e = ExprStart();
				} else
					break;
			}
			// line 74
			if ((e != null || end.Type() == TT.Comma)) {
				yield
				return e ?? MissingExpr();
			}
		}
		protected LNode ExprStart()
		{
			TT la0;
			RVList<LNode> attrs = default(RVList<LNode>);
			LNode e = default(LNode);
			Token t = default(Token);
			// Line 91: (TT.LBrack ExprList TT.RBrack)?
			la0 = (TT) LA0;
			if (la0 == TT.LBrack) {
				t = MatchAny();
				attrs = ExprList();
				Match((int) TT.RBrack);
			}
			e = Expr(StartStmt);
			// line 95
			if ((t.TypeInt != 0)) {
				e = e.WithRange(t.StartIndex, e.Range.EndIndex);
			}
			return e.PlusAttrs(attrs);
		}
		LNode Expr(Precedence context)
		{
			LNode e = default(LNode);
			Token t = default(Token);
			// line 109
			Precedence prec;
			e = PrefixExpr(context);
			// Line 113: greedy( &{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} ((TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp) | &{LA($LI + 1) != TT.Indent->@int} TT.Colon) Expr / &{context.CanParse(P.Primary)} FinishPrimaryExpr / &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp / &{context.CanParse(P.Juxtaposition)} Atom greedy(Atom)* )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Assignment:
				case TT.BQString:
				case TT.Dot:
				case TT.NormalOp:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0))))
							goto matchExpr;
						else
							goto stop2;
					}
				case TT.Colon:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							if (LA(0 + 1) != (int) TT.Indent)
								goto matchExpr;
							else if (context.CanParse(P.Juxtaposition))
								goto matchAtom;
							else
								goto stop2;
						} else if (LA(0 + 1) != (int) TT.Indent) {
							if (context.CanParse(P.Juxtaposition))
								goto matchAtom;
							else
								goto stop2;
						} else if (context.CanParse(P.Juxtaposition))
							goto matchAtom;
						else
							goto stop2;
					}
				case TT.LParen:
					{
						if (context.CanParse(P.Primary))
							e = FinishPrimaryExpr(e);
						else if (context.CanParse(P.Juxtaposition))
							goto matchAtom;
						else
							goto stop2;
					}
					break;
				case TT.LBrack:
				case TT.Not:
					{
						if (context.CanParse(P.Primary))
							e = FinishPrimaryExpr(e);
						else
							goto stop2;
					}
					break;
				case TT.PreOrSufOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
							t = MatchAny();
							// line 123
							e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex).SetBaseStyle(NodeStyle.Operator);
						} else
							goto stop2;
					}
					break;
				case TT.At:
				case TT.Id:
				case TT.LBrace:
				case TT.Number:
				case TT.OtherLit:
				case TT.String:
					{
						if (context.CanParse(P.Juxtaposition))
							goto matchAtom;
						else
							goto stop2;
					}
				default:
					goto stop2;
				}
				continue;
			matchExpr:
				{
					// Line 114: ((TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp) | &{LA($LI + 1) != TT.Indent->@int} TT.Colon)
					switch ((TT) LA0) {
					case TT.Assignment:
					case TT.BQString:
					case TT.Dot:
					case TT.NormalOp:
						t = MatchAny();
						break;
					default:
						{
							Check(LA(0 + 1) != (int) TT.Indent, "LA($LI + 1) != TT.Indent->@int");
							t = Match((int) TT.Colon);
						}
						break;
					}
					var rhs = Expr(prec);
					// line 116
					e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex).SetBaseStyle(NodeStyle.Operator);
				}
				continue;
			matchAtom:
				{
					var hasArgsAlready = e.BaseStyle == NodeStyle.PrefixNotation;
					var args = hasArgsAlready ? e.Args : RVList<LNode>.Empty;
					args.Add(Atom(true));
					// Line 128: greedy(Atom)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.At:
						case TT.Colon:
						case TT.Id:
						case TT.LBrace:
						case TT.LParen:
						case TT.Number:
						case TT.OtherLit:
						case TT.String:
							args.Add(Atom(true));
							break;
						default:
							goto stop;
						}
					}
				stop:;
					// line 130
					if ((hasArgsAlready)) {
						e = e.WithArgs(args).WithRange(e.Range.StartIndex, args.Last.Range.EndIndex);
					} else {
						e = F.Call(e, args, e.Range.StartIndex, args.Last.Range.EndIndex);
					}
				}
			}
		stop2:;
			// line 137
			return e;
		}
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			RVList<LNode> list = default(RVList<LNode>);
			// Line 143: ( TT.LParen ExprList TT.RParen | TT.Not (TT.LParen ExprList TT.RParen / Expr) | TT.LBrack ExprList TT.RBrack )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				Skip();
				list = ExprList();
				var c = Match((int) TT.RParen);
				// line 144
				e = F.Call(e, list, e.Range.StartIndex, c.EndIndex).SetBaseStyle(NodeStyle.PrefixNotation);
			} else if (la0 == TT.Not) {
				Skip();
				// line 147
				var args = new RVList<LNode> { 
					e
				};
				int endIndex;
				// Line 148: (TT.LParen ExprList TT.RParen / Expr)
				la0 = (TT) LA0;
				if (la0 == TT.LParen) {
					Skip();
					args = ExprList(args);
					var c = Match((int) TT.RParen);
					// line 148
					endIndex = c.EndIndex;
				} else {
					var T = Expr(P.Primary);
					// line 149
					args.Add(T);
					endIndex = T.Range.EndIndex;
				}
				// line 151
				e = F.Call(S.Of, args, e.Range.StartIndex, endIndex).SetBaseStyle(NodeStyle.Operator);
			} else {
				// line 153
				var args = new RVList<LNode> { 
					e
				};
				Match((int) TT.LBrack);
				args = ExprList(args);
				var c = Match((int) TT.RBrack);
				// line 155
				e = F.Call(S.Bracks, args, e.Range.StartIndex, c.EndIndex).SetBaseStyle(NodeStyle.Operator);
			}
			// line 157
			return e;
		}
		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			LNode result = default(LNode);
			Token t = default(Token);
			// Line 161: (Atom | (TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr)
			switch ((TT) LA0) {
			case TT.At:
			case TT.Colon:
			case TT.Id:
			case TT.LBrace:
			case TT.LParen:
			case TT.Number:
			case TT.OtherLit:
			case TT.String:
				result = Atom();
				break;
			case TT.Assignment:
			case TT.BQString:
			case TT.Dot:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				{
					t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t));
					// line 165
					result = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetBaseStyle(NodeStyle.Operator);
				}
				break;
			default:
				{
					// line 167
					result = MissingExpr();
					Error(0, "Expected an expression here");
				}
				break;
			}
			return result;
		}
		LNode Atom(bool juxtaposition = false)
		{
			TT la0;
			LNode result = default(LNode);
			TokenTree tree = default(TokenTree);
			// Line 178: ( TT.Id | (TT.Number|TT.OtherLit|TT.String) | TT.At TT.LBrack TokenTree TT.RBrack | TT.Colon TT.Indent StmtList TT.Dedent greedy(TT.Colon)? | TT.LBrace StmtList TT.RBrace | TT.LParen ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.Id:
				{
					var id = MatchAny();
					// line 179
					result = F.Id(id);
				}
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.String:
				{
					var lit = MatchAny();
					// line 181
					result = F.Literal(lit);
				}
				break;
			case TT.At:
				{
					var o = MatchAny();
					Match((int) TT.LBrack);
					tree = TokenTree();
					var c = Match((int) TT.RBrack);
					// line 184
					result = F.Literal(tree, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.Colon:
				{
					var o = MatchAny();
					Match((int) TT.Indent);
					var list = StmtList();
					var c = Match((int) TT.Dedent);
					// Line 186: greedy(TT.Colon)?
					la0 = (TT) LA0;
					if (la0 == TT.Colon)
						Skip();
					// line 187
					result = F.Braces(list, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var o = MatchAny();
					var list = StmtList();
					var c = Match((int) TT.RBrace);
					// line 190
					result = F.Braces(list, o.StartIndex, c.EndIndex).SetBaseStyle(NodeStyle.Statement);
				}
				break;
			case TT.LParen:
				{
					// line 193
					if ((juxtaposition)) {
						Error(0, "'(' is not permitted here. Did you forget a ';' (or ',') at the end of the previous expression?");
					}
					var endMarker = default(TT);
					var o = MatchAny();
					// line 198
					var hasAttrList = (TT) LA0 == TT.LBrack;
					var list = ExprList(ref endMarker);
					var c = Match((int) TT.RParen);
					// line 201
					if ((endMarker == TT.Semicolon || list.Count != 1)) {
						result = F.Call(S.Tuple, list, o.StartIndex, c.EndIndex);
						if ((endMarker == TT.Comma)) {
							ErrorSink.Write(Severity.Error, list[0].Range.End, "Tuples require ';' as a separator.");
						}
					} else {
						result = hasAttrList ? list[0] : F.InParens(list[0], o.StartIndex, c.EndIndex);
					}
				}
				break;
			default:
				{
					// line 210
					Error(0, "Subexpression expected here");
					Skip();
					// line 211
					result = MissingExpr();
				}
				break;
			}
			return result;
		}
		TokenTree TokenTree()
		{
			TokenTree got_TokenTree = default(TokenTree);
			TokenTree result = default(TokenTree);
			result = new TokenTree(SourceFile);
			// Line 218: nongreedy((TT.Indent|TT.LBrace|TT.LBrack|TT.LParen) TokenTree (TT.Dedent|TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
			for (;;) {
				switch ((TT) LA0) {
				case EOF:
				case TT.Dedent:
				case TT.RBrace:
				case TT.RBrack:
				case TT.RParen:
					goto stop;
				case TT.Indent:
				case TT.LBrace:
				case TT.LBrack:
				case TT.LParen:
					{
						var open = MatchAny();
						got_TokenTree = TokenTree();
						// line 220
						result.Add(open.WithValue(got_TokenTree));
						result.Add(Match((int) TT.Dedent, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen));
					}
					break;
				default:
					result.Add(MatchAny());
					break;
				}
			}
		stop:;
			return result;
		}
	}
}
