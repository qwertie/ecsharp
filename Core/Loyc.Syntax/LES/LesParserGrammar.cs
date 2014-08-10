// Generated from LesParserGrammar.les by LLLPG custom tool. LLLPG version: 1.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --verbose             Allow verbose messages (shown as 'warnings')
// --no-out-header       Suppress this message
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
		LNode Atom(Precedence context, ref RWList<LNode> attrs)
		{
			TT la0, la1;
			LNode e = MissingExpr;
			LNode _;
			// Line 39: ( TT.Id (&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen / ) | (TT.Symbol|TT.Number|TT.String|TT.SQString|TT.OtherLit) | TT.At TT.LBrack TT.RBrack | (TT.PrefixOp|TT.PreSufOp) Expr | &{context != P.SuperExpr} (TT.Colon|TT.NormalOp|TT.Not|TT.BQString|TT.Dot|TT.Assignment) Expr | TT.LBrack TT.RBrack Atom | TT.LParen TT.RParen | TT.LBrace TT.RBrace )
			 switch (LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					// Line 40: (&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen / )
					la0 = LA0;
					if (la0 == TT.LParen) {
						if (t.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var p = MatchAny();
								var rp = MatchAny();
								e = ParseCall(t, p, rp.EndIndex);
							} else
								e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
						} else
							e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
					} else
						e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				{
					var t = MatchAny();
					e = F.Literal(t.Value, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.At:
				{
					Skip();
					var t = Match((int) TT.LBrack);
					var rb = Match((int) TT.RBrack);
					e = F.Literal(t.Children, t.StartIndex, rb.EndIndex);
				}
				break;
			case TT.PrefixOp:
			case TT.PreSufOp:
				{
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.Assignment:
			case TT.BQString:
			case TT.Colon:
			case TT.Dot:
			case TT.NormalOp:
			case TT.Not:
				{
					Check(context != P.SuperExpr, "context != P.SuperExpr");
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.LBrack:
				{
					var t = MatchAny();
					Match((int) TT.RBrack);
					attrs = AppendExprsInside(t, attrs);
					e = Atom(context, ref attrs);
				}
				break;
			case TT.LParen:
				{
					var t = MatchAny();
					var rp = Match((int) TT.RParen);
					e = ParseParens(t, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var t = MatchAny();
					var rb = Match((int) TT.RBrace);
					e = ParseBraces(t, rb.EndIndex);
				}
				break;
			default:
				{
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "Expected an expression here");
				}
				break;
			}
			return e;
		}
		LNode Expr(Precedence context, out LNode primary)
		{
			TT la1;
			LNode e;
			LNode _;
			Precedence prec;
			RWList<LNode> attrs = null;
			e = Atom(context, ref attrs);
			primary = e;
			// Line 89: greedy( (&{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} (TT.Colon|TT.NormalOp|TT.BQString|TT.Dot|TT.Assignment) Expr | &{context.CanParse(P.Primary)} TT.Not Expr | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} (TT.SuffixOp|TT.PreSufOp) | &{e.Range.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen) | (&{context.CanParse(P.Primary)} TT.LBrack TT.RBrack / &{context.CanParse(P.SuperExpr)} Expr greedy(Expr)*) )*
			 for (;;) {
				switch (LA0) {
				case TT.Assignment:
				case TT.BQString:
				case TT.Colon:
				case TT.Dot:
				case TT.NormalOp:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							switch (LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LBrack:
							case TT.LParen:
							case TT.NormalOp:
							case TT.Not:
							case TT.Number:
							case TT.OtherLit:
							case TT.PrefixOp:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.String:
							case TT.Symbol:
								{
									var t = MatchAny();
									var rhs = Expr(prec, out primary);
									e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
									e.BaseStyle = NodeStyle.Operator;
									if ((!prec.CanParse(P.NullDot)))
										primary = e;
								}
								break;
							default:
								goto stop2;
							}
						} else if (context.CanParse(P.SuperExpr)) {
							switch (LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LBrack:
							case TT.LParen:
							case TT.NormalOp:
							case TT.Not:
							case TT.Number:
							case TT.OtherLit:
							case TT.PrefixOp:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.String:
							case TT.Symbol:
								goto match6;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
					break;
				case TT.Not:
					{
						if (context.CanParse(P.Primary)) {
							switch (LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LBrack:
							case TT.LParen:
							case TT.NormalOp:
							case TT.Not:
							case TT.Number:
							case TT.OtherLit:
							case TT.PrefixOp:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.String:
							case TT.Symbol:
								{
									Skip();
									var rhs = Expr(P.Primary, out primary);
									RVList<LNode> args;
									if ((rhs.Calls(S.Tuple))) {
										args = new RVList<LNode>(e).AddRange(rhs.Args);
									} else {
										int i = rhs.Attrs.IndexWithName(S.TriviaInParens);
										if ((i > -1))
											rhs = rhs.WithAttrs(rhs.Attrs.RemoveAt(i));
										args = new RVList<LNode>(e, rhs);
									}
									e = primary = F.Call(S.Of, args, e.Range.StartIndex, rhs.Range.EndIndex);
									e.BaseStyle = NodeStyle.Operator;
								}
								break;
							default:
								goto stop2;
							}
						} else if (context.CanParse(P.SuperExpr)) {
							switch (LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LBrack:
							case TT.LParen:
							case TT.NormalOp:
							case TT.Not:
							case TT.Number:
							case TT.OtherLit:
							case TT.PrefixOp:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.String:
							case TT.Symbol:
								goto match6;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
					break;
				case TT.PreSufOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0))))
							goto match3;
						else if (context.CanParse(P.SuperExpr)) {
							switch (LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LBrack:
							case TT.LParen:
							case TT.NormalOp:
							case TT.Not:
							case TT.Number:
							case TT.OtherLit:
							case TT.PrefixOp:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.String:
							case TT.Symbol:
								goto match6;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
				case TT.SuffixOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0))))
							goto match3;
						else
							goto stop2;
					}
				case TT.LParen:
					{
						if (e.Range.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var p = MatchAny();
								var rp = MatchAny();
								e = primary = ParseCall(e, p, rp.EndIndex);
								e.BaseStyle = NodeStyle.PrefixNotation;
							} else
								goto stop2;
						} else if (context.CanParse(P.SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.RParen)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
					break;
				case TT.LBrack:
					{
						if (context.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RBrack) {
								var t = MatchAny();
								var rb = MatchAny();
								var args = new RWList<LNode> { 
									e
								};
								AppendExprsInside(t, args);
								e = primary = F.Call(S.Bracks, args.ToRVList(), e.Range.StartIndex, rb.EndIndex);
								e.BaseStyle = NodeStyle.Expression;
							} else
								goto stop2;
						} else if (context.CanParse(P.SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.RBrack)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
					break;
				case TT.Id:
				case TT.Number:
				case TT.OtherLit:
				case TT.SQString:
				case TT.String:
				case TT.Symbol:
					{
						if (context.CanParse(P.SuperExpr))
							goto match6;
						else
							goto stop2;
					}
				case TT.At:
					{
						if (context.CanParse(P.SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.LBrack)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
				case TT.PrefixOp:
					{
						if (context.CanParse(P.SuperExpr)) {
							switch (LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LBrack:
							case TT.LParen:
							case TT.NormalOp:
							case TT.Not:
							case TT.Number:
							case TT.OtherLit:
							case TT.PrefixOp:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.String:
							case TT.Symbol:
								goto match6;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
				case TT.LBrace:
					{
						if (context.CanParse(P.SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.RBrace)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
				default:
					goto stop2;
				}
				continue;
			match3:
				{
					var t = MatchAny();
					e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex);
					e.BaseStyle = NodeStyle.Operator;
					if ((t.Type() == TT.PreSufOp))
						primary = null;
				}
				continue;
			match6:
				{
					var rhs = RVList<LNode>.Empty;
					rhs.Add(Expr(P.SuperExpr, out _));
					// Line 138: greedy(Expr)*
					 for (;;) {
						switch (LA0) {
						case TT.Id:
						case TT.Number:
						case TT.OtherLit:
						case TT.SQString:
						case TT.String:
						case TT.Symbol:
							rhs.Add(Expr(P.SuperExpr, out _));
							break;
						case TT.At:
							{
								la1 = LA(1);
								if (la1 == TT.LBrack)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.Assignment:
						case TT.BQString:
						case TT.Colon:
						case TT.Dot:
						case TT.NormalOp:
						case TT.Not:
						case TT.PrefixOp:
						case TT.PreSufOp:
							{
								switch (LA(1)) {
								case TT.Assignment:
								case TT.At:
								case TT.BQString:
								case TT.Colon:
								case TT.Dot:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.LParen:
								case TT.NormalOp:
								case TT.Not:
								case TT.Number:
								case TT.OtherLit:
								case TT.PrefixOp:
								case TT.PreSufOp:
								case TT.SQString:
								case TT.String:
								case TT.Symbol:
									rhs.Add(Expr(P.SuperExpr, out _));
									break;
								default:
									goto stop;
								}
							}
							break;
						case TT.LBrack:
							{
								la1 = LA(1);
								if (la1 == TT.RBrack)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.LParen:
							{
								la1 = LA(1);
								if (la1 == TT.RParen)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.LBrace:
							{
								la1 = LA(1);
								if (la1 == TT.RBrace)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						default:
							goto stop;
						}
					}
				 stop:;
					e = MakeSuperExpr(e, ref primary, rhs);
				}
			}
		 stop2:;
			return attrs == null ? e : e.WithAttrs(attrs.ToRVList());
		}
		protected LNode SuperExpr()
		{
			LNode _;
			var e = Expr(StartStmt, out _);
			return e;
		}
		protected LNode SuperExprOpt()
		{
			// Line 155: (SuperExpr | )
			 switch (LA0) {
			case TT.Assignment:
			case TT.At:
			case TT.BQString:
			case TT.Colon:
			case TT.Dot:
			case TT.Id:
			case TT.LBrace:
			case TT.LBrack:
			case TT.LParen:
			case TT.NormalOp:
			case TT.Not:
			case TT.Number:
			case TT.OtherLit:
			case TT.PrefixOp:
			case TT.PreSufOp:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				{
					var e = SuperExpr();
					return e;
				}
				break;
			default:
				return MissingExpr;
				break;
			}
		}
		protected void ExprList(ref RWList<LNode> exprs)
		{
			TT la0;
			exprs = exprs ?? new RWList<LNode>();
			// Line 160: (SuperExpr (TT.Comma SuperExprOpt)* | TT.Comma SuperExprOpt (TT.Comma SuperExprOpt)*)?
			 switch (LA0) {
			case TT.Assignment:
			case TT.At:
			case TT.BQString:
			case TT.Colon:
			case TT.Dot:
			case TT.Id:
			case TT.LBrace:
			case TT.LBrack:
			case TT.LParen:
			case TT.NormalOp:
			case TT.Not:
			case TT.Number:
			case TT.OtherLit:
			case TT.PrefixOp:
			case TT.PreSufOp:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				{
					exprs.Add(SuperExpr());
					// Line 161: (TT.Comma SuperExprOpt)*
					 for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							exprs.Add(SuperExprOpt());
						} else
							break;
					}
				}
				break;
			case TT.Comma:
				{
					exprs.Add(MissingExpr);
					Skip();
					exprs.Add(SuperExprOpt());
					// Line 163: (TT.Comma SuperExprOpt)*
					 for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							exprs.Add(SuperExprOpt());
						} else
							break;
					}
				}
				break;
			}
		}
		LNode SuperExprOptUntil(TokenType terminator)
		{
			TT la0;
			LNode e = MissingExpr;
			// Line 169: (SuperExpr)?
			 switch (LA0) {
			case TT.Assignment:
			case TT.At:
			case TT.BQString:
			case TT.Colon:
			case TT.Dot:
			case TT.Id:
			case TT.LBrace:
			case TT.LBrack:
			case TT.LParen:
			case TT.NormalOp:
			case TT.Not:
			case TT.Number:
			case TT.OtherLit:
			case TT.PrefixOp:
			case TT.PreSufOp:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				e = SuperExpr();
				break;
			}
			bool error = false;
			// Line 173: greedy(&{$LA != terminator} ~(EOF))*
			 for (;;) {
				la0 = LA0;
				if (la0 != EOF) {
					la0 = LA0;
					if (la0 != terminator) {
						if ((!error)) {
							error = true;
							Error(0, "Expected " + terminator.ToString());
						}
						Skip();
					} else
						break;
				} else
					break;
			}
			return e;
		}
		public void StmtList(ref RWList<LNode> exprs)
		{
			TT la0;
			exprs = exprs ?? new RWList<LNode>();
			var next = SuperExprOptUntil(TT.Semicolon);
			// Line 187: (TT.Semicolon SuperExprOptUntil)*
			 for (;;) {
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					exprs.Add(next);
					Skip();
					next = SuperExprOptUntil(TT.Semicolon);
				} else
					break;
			}
			if ((next != (object) MissingExpr))
				exprs.Add(next);
		}
	}
}
