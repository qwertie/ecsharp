using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.LLParserGenerator;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc;

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
			LNode e, _;
			switch (LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					la0 = LA0;
					if (la0 == TT.LParen) {
						if (t.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							var p = MatchAny();
							Match(TT.RParen);
							e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList());
						} else
							e = F.Id((Symbol)t.Value);
					} else
						e = F.Id((Symbol)t.Value);
				}
				break;
			case TT.OtherLit:
			case TT.Number:
			case TT.Symbol:
			case TT.String:
				{
					var t = MatchAny();
					e = F.Literal(t.Value);
				}
				break;
			case TT.At:
				{
					Skip();
					var t = Match(TT.LBrack);
					Match(TT.RBrack);
					e = F.Literal(t.Children);
				}
				break;
			case TT.NormalOp:
			case TT.Dot:
			case TT.Assignment:
			case TT.PreSufOp:
				{
					var t = MatchAny();
					e = Expr(UnaryPrecedenceOf(t), out _);
					e = F.Call((Symbol)t.Value, e);
				}
				break;
			case TT.LBrack:
				{
					var t = MatchAny();
					Match(TT.RBrack);
					attrs = AppendExprsInside(t, attrs);
					e = Atom(context, ref attrs);
				}
				break;
			case TT.LParen:
				{
					var t = MatchAny();
					Match(TT.RParen);
					e = InterpretParens(t);
				}
				break;
			default:
				{
					var t = Match(TT.LBrace);
					Match(TT.RBrace);
					e = InterpretBraces(t);
				}
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
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							switch (LA(1)) {
							case TT.NormalOp:
							case TT.Assignment:
							case TT.At:
							case TT.PreSufOp:
							case TT.Number:
							case TT.Symbol:
							case TT.LParen:
							case TT.String:
							case TT.OtherLit:
							case TT.Id:
							case TT.LBrack:
							case TT.Dot:
							case TT.LBrace:
								{
									var t = MatchAny();
									var rhs = Expr(prec, out primary);
									e = F.Call((Symbol)t.Value, e, rhs);;
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
				case TT.PreSufOp:
					{
						if (context.CanParse(UnaryPrecedenceOf(LT(0)))) {
							var t = MatchAny();
							e = F.Call(ToSuffixOpName((Symbol)t.Value), e);
							e.BaseStyle = NodeStyle.Operator;
							primary = null; // disallow superexpression after suffix (prefix/suffix ambiguity;
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
								Skip();
								
							var args = new RWList<LNode> { e };
							AppendExprsInside(t, args);
							e = primary = F.Call(S.Bracks, args.ToRVList());
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
			LNode primary, _;
			var e = Expr(StartStmt, out primary);
			var otherExprs = RVList<LNode>.Empty;
			for (;;) {
				switch (LA0) {
				case TT.NormalOp:
				case TT.Assignment:
				case TT.At:
				case TT.PreSufOp:
				case TT.Number:
				case TT.Symbol:
				case TT.LParen:
				case TT.String:
				case TT.OtherLit:
				case TT.Id:
				case TT.LBrack:
				case TT.Dot:
				case TT.LBrace:
					{
						otherExprs.Add(Expr(StartStmt, out _));
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
			case TT.NormalOp:
			case TT.Assignment:
			case TT.At:
			case TT.PreSufOp:
			case TT.Number:
			case TT.Symbol:
			case TT.LParen:
			case TT.String:
			case TT.OtherLit:
			case TT.Id:
			case TT.LBrack:
			case TT.Dot:
			case TT.LBrace:
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
		void ExprList(ref RWList<LNode> exprs)
		{
			TT la0;
			exprs = exprs ?? new RWList<LNode>();
			switch (LA0) {
			case TT.NormalOp:
			case TT.Assignment:
			case TT.At:
			case TT.PreSufOp:
			case TT.Number:
			case TT.Symbol:
			case TT.LParen:
			case TT.String:
			case TT.OtherLit:
			case TT.Id:
			case TT.LBrack:
			case TT.Dot:
			case TT.LBrace:
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
			exprs.Add(SuperExprOpt());
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					Skip();
					exprs.Add(SuperExprOpt());
				} else
					break;
			}
			if (object.ReferenceEquals(exprs[exprs.Count-1], MissingExpr)) exprs.RemoveAt(exprs.Count-1);
		}
	}
}