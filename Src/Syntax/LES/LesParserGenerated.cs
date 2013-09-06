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
		LNode Atom(Precedence context, ref RWList<LNode> attrs)
		{
			TT la0;
			LNode e = F._Missing, _;
			switch (LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					la0 = LA0;
					if (la0 == TT.LParen) {
						if (t.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							var p = MatchAny();
							var rp = Match((int) TT.RParen);
							e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList(), t.StartIndex, rp.EndIndex - t.StartIndex);
						} else
							e = F.Id((Symbol)t.Value, t.StartIndex, t.Length);
					} else
						e = F.Id((Symbol)t.Value, t.StartIndex, t.Length);
				}
				break;
			case TT.OtherLit:
			case TT.Number:
			case TT.Symbol:
			case TT.SQString:
			case TT.String:
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
			case TT.Not:
			case TT.NormalOp:
			case TT.Dot:
			case TT.Assignment:
			case TT.PreSufOp:
			case TT.BQString:
			case TT.Colon:
				{
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					e = F.Call((Symbol)t.Value, e, t.StartIndex, e.Range.EndIndex - t.StartIndex);
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
					e = InterpretParens(t, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var t = MatchAny();
					var rb = Match((int) TT.RBrace);
					e = InterpretBraces(t, rb.EndIndex);
				}
				break;
			default:
				Error(InputPosition + 0, "In rule 'Atom', expected one of: (TT.OtherLit|TT.At|TT.PrefixOp|TT.Not|TT...");
				break;
			}
			return e;
		}
		LNode Expr(Precedence context, out LNode primary)
		{
			TT la1;
			LNode e; Precedence prec; RWList<LNode> attrs = null;
			e = Atom(context, ref attrs);
			primary = e;
			for (;;) {
				switch (LA0) {
				case TT.NormalOp:
				case TT.Dot:
				case TT.Assignment:
				case TT.BQString:
				case TT.Colon:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							switch (LA(1)) {
							case TT.OtherLit:
							case TT.At:
							case TT.PrefixOp:
							case TT.Not:
							case TT.Number:
							case TT.NormalOp:
							case TT.Dot:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.LParen:
							case TT.Id:
							case TT.LBrack:
							case TT.BQString:
							case TT.LBrace:
							case TT.Symbol:
							case TT.Assignment:
							case TT.Colon:
							case TT.String:
								{
									var t = MatchAny();
									var rhs = Expr(prec, out primary);
									e = F.Call((Symbol)t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);;
									e.BaseStyle = NodeStyle.Operator;
									if (!prec.CanParse(P.NullDot)) primary = e;;
								}
								break;
							default:
								goto stop;
							}
						} else
							goto stop;
					}
					break;
				case TT.Not:
					{
						if (context.CanParse(P.Primary)) {
							switch (LA(1)) {
							case TT.OtherLit:
							case TT.At:
							case TT.PrefixOp:
							case TT.Not:
							case TT.Number:
							case TT.NormalOp:
							case TT.Dot:
							case TT.PreSufOp:
							case TT.SQString:
							case TT.LParen:
							case TT.Id:
							case TT.LBrack:
							case TT.BQString:
							case TT.LBrace:
							case TT.Symbol:
							case TT.Assignment:
							case TT.Colon:
							case TT.String:
								{
									Skip();
									var rhs = Expr(P.Primary, out primary);
									
							RVList<LNode> args;
							if (rhs.Calls(S.Tuple))
								args = new RVList<LNode>(e).AddRange(rhs.Args);
							else
								args = new RVList<LNode>(e, rhs);
							e = F.Call(S.Of, args, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);
							e.BaseStyle = NodeStyle.Operator;;
								}
								break;
							default:
								goto stop;
							}
						} else
							goto stop;
					}
					break;
				case TT.PreSufOp:
				case TT.SuffixOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
							var t = MatchAny();
							e = F.Call(ToSuffixOpName((Symbol)t.Value), e, e.Range.StartIndex, t.EndIndex - e.Range.StartIndex);
							e.BaseStyle = NodeStyle.Operator;
							primary = null; // disallow superexpression after suffix (prefix/suffix ambiguity;
						} else
							goto stop;
					}
					break;
				case TT.LParen:
					{
						if (e.Range.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var t = MatchAny();
								var rp = MatchAny();
								e = primary = F.Call(e, ExprListInside(t).ToRVList(), e.Range.StartIndex, rp.EndIndex - e.Range.StartIndex);
								e.BaseStyle = NodeStyle.PurePrefixNotation;
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				case TT.LBrack:
					{
						if (context.CanParse(P.Primary)) {
							la1 = LA(1);
							if (la1 == TT.RBrack) {
								var t = MatchAny();
								var rb = MatchAny();
								
							var args = new RWList<LNode> { e };
							AppendExprsInside(t, args);
							e = primary = F.Call(S.Bracks, args.ToRVList(), e.Range.StartIndex, rb.EndIndex - e.Range.StartIndex);
							e.BaseStyle = NodeStyle.Expression;;
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			return attrs == null ? e : e.WithAttrs(attrs.ToRVList());
		}
		LNode SuperExpr()
		{
			LNode primary, p_;
			var e = Expr(StartStmt, out primary);
			var otherExprs = RVList<LNode>.Empty; p_ = e;;
			for (;;) {
				switch (LA0) {
				case TT.OtherLit:
				case TT.At:
				case TT.PrefixOp:
				case TT.Not:
				case TT.Number:
				case TT.NormalOp:
				case TT.Dot:
				case TT.PreSufOp:
				case TT.SQString:
				case TT.LParen:
				case TT.Id:
				case TT.LBrack:
				case TT.BQString:
				case TT.LBrace:
				case TT.Symbol:
				case TT.Assignment:
				case TT.Colon:
				case TT.String:
					{
						if (p_ == null) Error(InputPosition-2, "Suffix operator is ambiguous at superexpression boundary.");
						otherExprs.Add(Expr(StartStmt, out p_));
						primary.BaseStyle = NodeStyle.Special;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			return MakeSuperExpr(e, primary, otherExprs);
		}
		LNode SuperExprOpt()
		{
			switch (LA0) {
			case TT.OtherLit:
			case TT.At:
			case TT.PrefixOp:
			case TT.Not:
			case TT.Number:
			case TT.NormalOp:
			case TT.Dot:
			case TT.PreSufOp:
			case TT.SQString:
			case TT.LParen:
			case TT.Id:
			case TT.LBrack:
			case TT.BQString:
			case TT.LBrace:
			case TT.Symbol:
			case TT.Assignment:
			case TT.Colon:
			case TT.String:
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
		LNode SuperExprOptUntil(TokenType terminator)
		{
			TT la0;
			LNode e = MissingExpr;
			switch (LA0) {
			case TT.OtherLit:
			case TT.At:
			case TT.PrefixOp:
			case TT.Not:
			case TT.Number:
			case TT.NormalOp:
			case TT.Dot:
			case TT.PreSufOp:
			case TT.SQString:
			case TT.LParen:
			case TT.Id:
			case TT.LBrack:
			case TT.BQString:
			case TT.LBrace:
			case TT.Symbol:
			case TT.Assignment:
			case TT.Colon:
			case TT.String:
				e = SuperExpr();
				break;
			}
			bool error = false;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					la0 = LA0;
					if (la0 != terminator)
						goto match1;
					else
						break;
				} else if (!(la0 == EOF || la0 == TT.Semicolon))
					goto match1;
				else
					break;
			match1:
				{
					Check(LA0 != terminator, "$LA != terminator");
					
								if (!error) {
									error = true;
									Error(InputPosition, "Expected " + terminator.ToString());
								}
							;
					Skip();
				}
			}
			return e;
		}
		void ExprList(ref RWList<LNode> exprs)
		{
			TT la0;
			exprs = exprs ?? new RWList<LNode>();
			switch (LA0) {
			case TT.OtherLit:
			case TT.At:
			case TT.PrefixOp:
			case TT.Not:
			case TT.Number:
			case TT.NormalOp:
			case TT.Dot:
			case TT.PreSufOp:
			case TT.SQString:
			case TT.LParen:
			case TT.Id:
			case TT.LBrack:
			case TT.BQString:
			case TT.LBrace:
			case TT.Symbol:
			case TT.Assignment:
			case TT.Colon:
			case TT.String:
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
		void StmtList(ref RWList<LNode> exprs)
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
			if (next != (object)MissingExpr) exprs.Add(next);;
		}
	}
}