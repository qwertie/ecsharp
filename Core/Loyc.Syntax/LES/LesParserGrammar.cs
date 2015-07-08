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
			// Line 1: ( / TopExpr)
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
				e = TopExpr();
				break;
			}
			// Line 58: ((TT.Comma|TT.Semicolon) ( / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					list.Add(e ?? MissingExpr());
					CheckEndMarker(ref endMarker, ref end);
					// Line 61: ( / TopExpr)
					switch ((TT) LA0) {
					case EOF:
					case TT.Comma:
					case TT.Dedent:
					case TT.RBrace:
					case TT.RBrack:
					case TT.RParen:
					case TT.Semicolon:
						// line 61
						e = null;
						break;
					default:
						e = TopExpr();
						break;
					}
				} else
					break;
			}
			if ((e != null || end.Type() == TT.Comma)) {
				list.Add(e ?? MissingExpr());
			}
			return list;
		}
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// Line 1: ( / TopExpr)
			la0 = (TT) LA0;
			if (la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon) {
			} else
				e = TopExpr();
			// Line 68: ((TT.Comma|TT.Semicolon) ( / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					// line 69
					yield
					return e ?? MissingExpr();
					CheckEndMarker(ref endMarker.Value, ref end);
					// Line 71: ( / TopExpr)
					la0 = (TT) LA0;
					if (la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon)
						// line 71
						e = null;
					else
						e = TopExpr();
				} else
					break;
			}
			// line 73
			if ((e != null || end.Type() == TT.Comma)) {
				yield
				return e ?? MissingExpr();
			}
		}
		protected LNode TopExpr()
		{
			TT la0;
			RVList<LNode> attrs = default(RVList<LNode>);
			LNode e = default(LNode);
			Token t = default(Token);
			// Line 90: (TT.LBrack ExprList TT.RBrack)?
			la0 = (TT) LA0;
			if (la0 == TT.LBrack) {
				t = MatchAny();
				attrs = ExprList();
				Match((int) TT.RBrack);
			}
			// Line 92: (Expr / TT.Id Expr (Particle)*)
			do {
				switch ((TT) LA0) {
				case TT.Assignment:
				case TT.BQString:
				case TT.Dot:
				case TT.NormalOp:
				case TT.Not:
				case TT.PrefixOp:
				case TT.PreOrSufOp:
					e = Expr(StartStmt);
					break;
				case TT.Id:
					{
						switch ((TT) LA(1)) {
						case TT.Assignment:
						case TT.BQString:
						case TT.Dot:
						case TT.NormalOp:
							e = Expr(StartStmt);
							break;
						case TT.Colon:
							{
								if (LA(1 + 1) != (int) TT.Indent)
									e = Expr(StartStmt);
								else
									goto match2;
							}
							break;
						case EOF:
						case TT.Comma:
						case TT.Dedent:
						case TT.LBrack:
						case TT.LParen:
						case TT.Not:
						case TT.PreOrSufOp:
						case TT.RBrace:
						case TT.RBrack:
						case TT.RParen:
						case TT.Semicolon:
							e = Expr(StartStmt);
							break;
						default:
							goto match2;
						}
					}
					break;
				default:
					e = Expr(StartStmt);
					break;
				}
				break;
			match2:
				{
					var id = MatchAny();
					// line 95
					var args = RVList<LNode>.Empty;
					args.Add(Expr(P.SuperExpr));
					// Line 97: (Particle)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.At:
						case TT.Colon:
						case TT.Id:
						case TT.LBrace:
						case TT.LParen:
						case TT.Number:
						case TT.OtherLit:
						case TT.SpaceLParen:
						case TT.String:
							{
								// line 98
								if (((TT) LA0 == TT.LParen)) {
									var loc = args[args.Count - 2, args.Last].Range.End;
									Error(0, "Expected a space before '(' (possibly missing ';' or ',' at {0})", loc);
								}
								args.Add(Particle());
							}
							break;
						default:
							goto stop;
						}
					}
				stop:;
					// line 105
					e = MarkSpecial(F.Call((Symbol) id.Value, args, id.StartIndex, args.Last.Range.EndIndex));
				}
			} while (false);
			if ((t.TypeInt != 0)) {
				e = e.WithRange(t.StartIndex, e.Range.EndIndex);
			}
			return e.PlusAttrs(attrs);
		}
		LNode Expr(Precedence context)
		{
			LNode e = default(LNode);
			Token t = default(Token);
			// line 120
			Precedence prec;
			e = PrefixExpr(context);
			// Line 124: greedy( &{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} ((TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp) | &{LA($LI + 1) != TT.Indent->@int} TT.Colon) Expr | &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp )*
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
							goto stop;
					}
				case TT.Colon:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							if (LA(0 + 1) != (int) TT.Indent)
								goto matchExpr;
							else
								goto stop;
						} else
							goto stop;
					}
				case TT.LBrack:
				case TT.LParen:
				case TT.Not:
					{
						if (context.CanParse(P.Primary))
							e = FinishPrimaryExpr(e);
						else
							goto stop;
					}
					break;
				case TT.PreOrSufOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
							t = MatchAny();
							// line 137
							e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex).SetStyle(NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
				continue;
			matchExpr:
				{
					// line 125
					if ((!prec.CanMixWith(context))) {
						Error(0, "Operator '{0}' is not allowed in this context. Add parentheses to clarify the code's meaning.", LT0.Value);
					}
					// Line 128: ((TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp) | &{LA($LI + 1) != TT.Indent->@int} TT.Colon)
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
					// line 130
					e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex).SetStyle(NodeStyle.Operator);
				}
			}
		stop:;
			// line 139
			return e;
		}
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			RVList<LNode> list = default(RVList<LNode>);
			// Line 145: ( TT.LParen ExprList TT.RParen | TT.Not (TT.LParen ExprList TT.RParen / Expr) | TT.LBrack ExprList TT.RBrack )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				// line 145
				var endMarker = default(TokenType);
				Skip();
				list = ExprList(ref endMarker);
				var c = Match((int) TT.RParen);
				// line 148
				e = MarkCall(F.Call(e, list, e.Range.StartIndex, c.EndIndex));
				if ((endMarker == TT.Semicolon)) {
					e.Style = NodeStyle.Statement | NodeStyle.Alternate;
				}
			} else if (la0 == TT.Not) {
				Skip();
				// line 153
				var args = new RVList<LNode> { 
					e
				};
				int endIndex;
				// Line 154: (TT.LParen ExprList TT.RParen / Expr)
				la0 = (TT) LA0;
				if (la0 == TT.LParen) {
					Skip();
					args = ExprList(args);
					var c = Match((int) TT.RParen);
					// line 154
					endIndex = c.EndIndex;
				} else {
					var T = Expr(P.Primary);
					// line 155
					args.Add(T);
					endIndex = T.Range.EndIndex;
				}
				// line 157
				e = F.Call(S.Of, args, e.Range.StartIndex, endIndex).SetStyle(NodeStyle.Operator);
			} else {
				// line 159
				var args = new RVList<LNode> { 
					e
				};
				Match((int) TT.LBrack);
				args = ExprList(args);
				var c = Match((int) TT.RBrack);
				// line 161
				e = F.Call(S.Bracks, args, e.Range.StartIndex, c.EndIndex).SetStyle(NodeStyle.Operator);
			}
			// line 163
			return e;
		}
		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			LNode result = default(LNode);
			Token t = default(Token);
			// Line 168: ((TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr | Particle)
			switch ((TT) LA0) {
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
					// line 170
					result = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetStyle(NodeStyle.Operator);
				}
				break;
			default:
				result = Particle();
				break;
			}
			return result;
		}
		LNode Particle()
		{
			TT la0;
			LNode result = default(LNode);
			TokenTree tree = default(TokenTree);
			// Line 182: ( TT.Id | (TT.Number|TT.OtherLit|TT.String) | TT.At TT.LBrack TokenTree TT.RBrack | TT.Colon TT.Indent StmtList TT.Dedent greedy(TT.Colon)? | BracedBlock | (TT.LParen|TT.SpaceLParen) ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.Id:
				{
					var id = MatchAny();
					// line 183
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.String:
				{
					var lit = MatchAny();
					// line 185
					result = F.Literal(lit).SetStyle(lit.Style);
				}
				break;
			case TT.At:
				{
					var o = MatchAny();
					Match((int) TT.LBrack);
					tree = TokenTree();
					var c = Match((int) TT.RBrack);
					// line 188
					result = F.Literal(tree, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.Colon:
				{
					var o = MatchAny();
					Match((int) TT.Indent);
					var list = StmtList();
					var c = Match((int) TT.Dedent);
					// Line 190: greedy(TT.Colon)?
					la0 = (TT) LA0;
					if (la0 == TT.Colon)
						Skip();
					// line 191
					result = F.Braces(list, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.LBrace:
				result = BracedBlock();
				break;
			case TT.LParen:
			case TT.SpaceLParen:
				{
					// line 195
					var endMarker = default(TT);
					var o = MatchAny();
					// line 196
					var hasAttrList = (TT) LA0 == TT.LBrack;
					var list = ExprList(ref endMarker);
					var c = Match((int) TT.RParen);
					// line 199
					if ((endMarker == TT.Semicolon || list.Count != 1)) {
						result = F.Call(S.Tuple, list, o.StartIndex, c.EndIndex);
						if ((endMarker == TT.Comma)) {
							var msg = "Tuples require ';' as a separator.";
							if ((o.Type() == TT.SpaceLParen)) {
								msg += " If a function call was intended, remove the space(s) before '('.";
							}
							ErrorSink.Write(Severity.Error, list[0].Range.End, msg);
						}
					} else {
						result = hasAttrList ? list[0] : F.InParens(list[0], o.StartIndex, c.EndIndex);
					}
				}
				break;
			default:
				{
					// line 213
					Error(0, "Expected a particle (id, literal, {braces} or (parens)).");
					result = MissingExpr();
				}
				break;
			}
			return result;
		}
		LNode BracedBlock()
		{
			LNode result = default(LNode);
			var o = MatchAny();
			var list = StmtList();
			var c = Match((int) TT.RBrace);
			result = F.Braces(list, o.StartIndex, c.EndIndex).SetStyle(NodeStyle.Statement);
			return result;
		}
		TokenTree TokenTree()
		{
			TT la1;
			TokenTree got_TokenTree = default(TokenTree);
			TokenTree result = default(TokenTree);
			result = new TokenTree(SourceFile);
			// Line 227: nongreedy((TT.Indent|TT.LBrace|TT.LBrack|TT.LParen|TT.SpaceLParen) TokenTree (TT.Dedent|TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
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
				case TT.SpaceLParen:
					{
						la1 = (TT) LA(1);
						if (la1 != EOF) {
							var open = MatchAny();
							got_TokenTree = TokenTree();
							// line 229
							result.Add(open.WithValue(got_TokenTree));
							result.Add(Match((int) TT.Dedent, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen));
						} else
							result.Add(MatchAny());
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
