using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;
	public partial class LesParser
	{
		LNode Atom(Precedence contextA, ref RWList<LNode> attrs)
		{
			TT la0, la1;
			LNode e = MissingExpr;
			LNode _;
			switch (LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					la0 = LA0;
					if (la0 == TT.LParen) {
						if (t.EndIndex == LT(0).StartIndex && contextA.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var p = MatchAny();
								var rp = MatchAny();
								e = ParseCall(t, p, rp.EndIndex);
							} else
								e = F.Id((Symbol) t.Value, t.StartIndex, t.Length);
						} else
							e = F.Id((Symbol) t.Value, t.StartIndex, t.Length);
					} else
						e = F.Id((Symbol) t.Value, t.StartIndex, t.Length);
				}
				break;
			case TT.Symbol:
			case TT.Number:
			case TT.String:
			case TT.SQString:
			case TT.OtherLit:
				{
					var t = MatchAny();
					e = F.Literal(t.Value, t.StartIndex, t.Length);
				}
				break;
			case TT.At:
				{
					Skip();
					var t = Match((int) TT.LBrack);
					var rb = Match((int) TT.RBrack);
					e = F.Literal(t.Children, t.StartIndex, rb.EndIndex - t.StartIndex);
				}
				break;
			case TT.PrefixOp:
			case TT.PreSufOp:
				{
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex - t.StartIndex);
				}
				break;
			case TT.NormalOp:
			case TT.Not:
			case TT.BQString:
			case TT.Dot:
			case TT.Assignment:
			case TT.Colon:
				{
					Check(contextA != P_SuperExpr, "contextA != P_SuperExpr");
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex - t.StartIndex);
				}
				break;
			case TT.LBrack:
				{
					var t = MatchAny();
					Match((int) TT.RBrack);
					attrs = AppendExprsInside(t, attrs);
					e = Atom(contextA, ref attrs);
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
					e = F.Id(S.Missing, LT0.StartIndex, 0);
					Error(InputPosition, "Expected an expression here");
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
			var contextA = context;
			for (;;) {
				switch (LA0) {
				case TT.NormalOp:
				case TT.BQString:
				case TT.Dot:
				case TT.Assignment:
				case TT.Colon:
					{
						switch (LA(1)) {
						case TT.Symbol:
						case TT.Number:
						case TT.Id:
						case TT.String:
						case TT.SQString:
						case TT.OtherLit:
						case TT.At:
						case TT.PrefixOp:
						case TT.PreSufOp:
							goto match1;
						case TT.NormalOp:
						case TT.Not:
						case TT.BQString:
						case TT.Dot:
						case TT.Assignment:
						case TT.Colon:
							{
								if (contextA != P_SuperExpr)
									goto match1;
								else
									goto stop2;
							}
							break;
						case TT.LBrack:
						case TT.LParen:
						case TT.LBrace:
							goto match1;
						default:
							goto stop2;
						}
					}
					break;
				case TT.Not:
					{
						if (context.CanParse(P.Primary)) {
							switch (LA(1)) {
							case TT.Symbol:
							case TT.Number:
							case TT.Id:
							case TT.String:
							case TT.SQString:
							case TT.OtherLit:
							case TT.At:
							case TT.PrefixOp:
							case TT.PreSufOp:
								goto match2;
							case TT.NormalOp:
							case TT.Not:
							case TT.BQString:
							case TT.Dot:
							case TT.Assignment:
							case TT.Colon:
								{
									if (contextA != P_SuperExpr)
										goto match2;
									else
										goto stop2;
								}
								break;
							case TT.LBrack:
							case TT.LParen:
							case TT.LBrace:
								goto match2;
							default:
								goto stop2;
							}
						} else if (context.CanParse(P_SuperExpr) && contextA != P_SuperExpr) {
							switch (LA(1)) {
							case TT.Symbol:
							case TT.Number:
							case TT.Id:
							case TT.String:
							case TT.SQString:
							case TT.OtherLit:
							case TT.At:
							case TT.PrefixOp:
							case TT.PreSufOp:
								goto match6;
							case TT.NormalOp:
							case TT.Not:
							case TT.BQString:
							case TT.Dot:
							case TT.Assignment:
							case TT.Colon:
								{
									if (contextA != P_SuperExpr)
										goto match6;
									else
										goto stop2;
								}
								break;
							case TT.LBrack:
							case TT.LParen:
							case TT.LBrace:
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
						else if (context.CanParse(P_SuperExpr)) {
							switch (LA(1)) {
							case TT.Symbol:
							case TT.Number:
							case TT.Id:
							case TT.String:
							case TT.SQString:
							case TT.OtherLit:
							case TT.At:
							case TT.PrefixOp:
							case TT.PreSufOp:
								goto match6;
							case TT.NormalOp:
							case TT.Not:
							case TT.BQString:
							case TT.Dot:
							case TT.Assignment:
							case TT.Colon:
								{
									if (contextA != P_SuperExpr)
										goto match6;
									else
										goto stop2;
								}
								break;
							case TT.LBrack:
							case TT.LParen:
							case TT.LBrace:
								goto match6;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
					break;
				case TT.SuffixOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0))))
							goto match3;
						else
							goto stop2;
					}
					break;
				case TT.LParen:
					{
						if (e.Range.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var p = MatchAny();
								var rp = MatchAny();
								e = primary = ParseCall(e, p, rp.EndIndex);
								e.BaseStyle = NodeStyle.PurePrefixNotation;
							} else
								goto stop2;
						} else if (context.CanParse(P_SuperExpr)) {
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
								e = primary = F.Call(S.Bracks, args.ToRVList(), e.Range.StartIndex, rb.EndIndex - e.Range.StartIndex);
								e.BaseStyle = NodeStyle.Expression;
							} else
								goto stop2;
						} else if (context.CanParse(P_SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.RBrack)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
					break;
				case TT.Symbol:
				case TT.Number:
				case TT.Id:
				case TT.String:
				case TT.SQString:
				case TT.OtherLit:
					{
						if (context.CanParse(P_SuperExpr))
							goto match6;
						else
							goto stop2;
					}
					break;
				case TT.At:
					{
						if (context.CanParse(P_SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.LBrack)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
					break;
				case TT.PrefixOp:
					{
						if (context.CanParse(P_SuperExpr)) {
							switch (LA(1)) {
							case TT.Symbol:
							case TT.Number:
							case TT.Id:
							case TT.String:
							case TT.SQString:
							case TT.OtherLit:
							case TT.At:
							case TT.PrefixOp:
							case TT.PreSufOp:
								goto match6;
							case TT.NormalOp:
							case TT.Not:
							case TT.BQString:
							case TT.Dot:
							case TT.Assignment:
							case TT.Colon:
								{
									if (contextA != P_SuperExpr)
										goto match6;
									else
										goto stop2;
								}
								break;
							case TT.LBrack:
							case TT.LParen:
							case TT.LBrace:
								goto match6;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
					break;
				case TT.LBrace:
					{
						if (context.CanParse(P_SuperExpr)) {
							la1 = LA(1);
							if (la1 == TT.RBrace)
								goto match6;
							else
								goto stop2;
						} else
							goto stop2;
					}
					break;
				default:
					goto stop2;
				}
				continue;
			match1:
				{
					if ((!context.CanParse(prec = InfixPrecedenceOf(LT(0))))) {
						goto end;
					}
					var t = MatchAny();
					var rhs = Expr(prec, out primary);
					e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);
					e.BaseStyle = NodeStyle.Operator;
					if ((!prec.CanParse(P.NullDot)))
						primary = e;
				}
				continue;
			match2:
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
					e = primary = F.Call(S.Of, args, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);
					e.BaseStyle = NodeStyle.Operator;
				}
				continue;
			match3:
				{
					var t = MatchAny();
					e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex - e.Range.StartIndex);
					e.BaseStyle = NodeStyle.Operator;
					if ((t.Type() == TT.PreSufOp))
						primary = null;
				}
				continue;
			match6:
				{
					var rhs = RVList<LNode>.Empty;
					contextA = P_SuperExpr;
					rhs.Add(Expr(P_SuperExpr, out _));
					for (;;) {
						switch (LA0) {
						case TT.Symbol:
						case TT.Number:
						case TT.Id:
						case TT.String:
						case TT.SQString:
						case TT.OtherLit:
							rhs.Add(Expr(P_SuperExpr, out _));
							break;
						case TT.At:
							{
								la1 = LA(1);
								if (la1 == TT.LBrack)
									rhs.Add(Expr(P_SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.PrefixOp:
						case TT.PreSufOp:
							{
								switch (LA(1)) {
								case TT.Symbol:
								case TT.Number:
								case TT.Id:
								case TT.String:
								case TT.SQString:
								case TT.OtherLit:
								case TT.At:
								case TT.PrefixOp:
								case TT.PreSufOp:
									rhs.Add(Expr(P_SuperExpr, out _));
									break;
								case TT.NormalOp:
								case TT.Not:
								case TT.BQString:
								case TT.Dot:
								case TT.Assignment:
								case TT.Colon:
									{
										if (contextA != P_SuperExpr)
											rhs.Add(Expr(P_SuperExpr, out _));
										else
											goto stop;
									}
									break;
								case TT.LBrack:
								case TT.LParen:
								case TT.LBrace:
									rhs.Add(Expr(P_SuperExpr, out _));
									break;
								default:
									goto stop;
								}
							}
							break;
						case TT.NormalOp:
						case TT.Not:
						case TT.BQString:
						case TT.Dot:
						case TT.Assignment:
						case TT.Colon:
							{
								if (contextA != P_SuperExpr) {
									switch (LA(1)) {
									case TT.Symbol:
									case TT.Number:
									case TT.Id:
									case TT.String:
									case TT.SQString:
									case TT.OtherLit:
									case TT.At:
									case TT.PrefixOp:
									case TT.PreSufOp:
										rhs.Add(Expr(P_SuperExpr, out _));
										break;
									case TT.NormalOp:
									case TT.Not:
									case TT.BQString:
									case TT.Dot:
									case TT.Assignment:
									case TT.Colon:
										{
											if (contextA != P_SuperExpr)
												rhs.Add(Expr(P_SuperExpr, out _));
											else
												goto stop;
										}
										break;
									case TT.LBrack:
									case TT.LParen:
									case TT.LBrace:
										rhs.Add(Expr(P_SuperExpr, out _));
										break;
									default:
										goto stop;
									}
								} else
									goto stop;
							}
							break;
						case TT.LBrack:
							{
								la1 = LA(1);
								if (la1 == TT.RBrack)
									rhs.Add(Expr(P_SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.LParen:
							{
								la1 = LA(1);
								if (la1 == TT.RParen)
									rhs.Add(Expr(P_SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.LBrace:
							{
								la1 = LA(1);
								if (la1 == TT.RBrace)
									rhs.Add(Expr(P_SuperExpr, out _));
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
		end:
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
			switch (LA0) {
			case TT.PreSufOp:
			case TT.Symbol:
			case TT.Number:
			case TT.Not:
			case TT.SQString:
			case TT.Dot:
			case TT.Colon:
			case TT.LBrack:
			case TT.LParen:
			case TT.PrefixOp:
			case TT.LBrace:
			case TT.Id:
			case TT.String:
			case TT.NormalOp:
			case TT.OtherLit:
			case TT.BQString:
			case TT.At:
			case TT.Assignment:
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
			switch (LA0) {
			case TT.PreSufOp:
			case TT.Symbol:
			case TT.Number:
			case TT.Not:
			case TT.SQString:
			case TT.Dot:
			case TT.Colon:
			case TT.LBrack:
			case TT.LParen:
			case TT.PrefixOp:
			case TT.LBrace:
			case TT.Id:
			case TT.String:
			case TT.NormalOp:
			case TT.OtherLit:
			case TT.BQString:
			case TT.At:
			case TT.Assignment:
				{
					exprs.Add(SuperExpr());
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
			switch (LA0) {
			case TT.PreSufOp:
			case TT.Symbol:
			case TT.Number:
			case TT.Not:
			case TT.SQString:
			case TT.Dot:
			case TT.Colon:
			case TT.LBrack:
			case TT.LParen:
			case TT.PrefixOp:
			case TT.LBrace:
			case TT.Id:
			case TT.String:
			case TT.NormalOp:
			case TT.OtherLit:
			case TT.BQString:
			case TT.At:
			case TT.Assignment:
				e = SuperExpr();
				break;
			}
			bool error = false;
			for (;;) {
				la0 = LA0;
				if (la0 != EOF) {
					la0 = LA0;
					if (la0 != terminator) {
						if ((!error)) {
							error = true;
							Error(InputPosition, "Expected " + terminator.ToString());
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
