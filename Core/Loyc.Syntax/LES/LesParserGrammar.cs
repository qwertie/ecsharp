// Generated from LesParserGrammar.les by LeMP custom tool. LLLPG version: 1.3.0.0
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
		LNode Atom(Precedence context, ref RWList<LNode> attrs)
		{
			TT la0, la1;
			// line 39
			LNode e = MissingExpr;
			LNode _;
			// Line 41: ( (TT.Id (&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen / ) | (TT.Number|TT.OtherLit|TT.SQString|TT.String|TT.Symbol) | TT.At TT.LBrack TT.RBrack | (TT.PrefixOp|TT.PreSufOp) Expr) | (TT.Colon TT.Indent TT.Dedent greedy(TT.Colon)? / &{context != P.SuperExpr} (TT.Assignment|TT.BQString|TT.Colon|TT.Dot|TT.NormalOp|TT.Not) Expr) | TT.LBrack TT.RBrack Atom | TT.LParen TT.RParen | TT.LBrace TT.RBrace )
			do {
				switch ((TT) LA0) {
				case TT.Id:
					{
						var t = MatchAny();
						// Line 42: (&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen / )
						la0 = (TT) LA0;
						if (la0 == TT.LParen) {
							if (t.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
								la1 = (TT) LA(1);
								if (la1 == TT.RParen) {
									var p = MatchAny();
									var rp = MatchAny();
									// line 44
									e = ParseCall(t, p, rp.EndIndex);
								} else
									// line 45
									e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
							} else
								// line 45
								e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
						} else
							// line 45
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
						// line 49
						e = F.Literal(t.Value, t.StartIndex, t.EndIndex);
					}
					break;
				case TT.At:
					{
						Skip();
						var t = Match((int) TT.LBrack);
						var rb = Match((int) TT.RBrack);
						// line 52
						e = F.Literal(t.Children, t.StartIndex, rb.EndIndex);
					}
					break;
				case TT.PrefixOp:
				case TT.PreSufOp:
					{
						var t = MatchAny();
						e = Expr(PrefixPrecedenceOf(t), out _);
						e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex);
						e.BaseStyle = NodeStyle.Operator;
					}
					break;
				case TT.Colon:
					{
						if (context != P.SuperExpr) {
							switch ((TT) LA(1)) {
							case TT.Indent:
								goto match5;
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
								goto error;
							}
						} else
							goto match5;
					}
				case TT.Assignment:
				case TT.BQString:
				case TT.Dot:
				case TT.NormalOp:
				case TT.Not:
					goto match6;
				case TT.LBrack:
					{
						var t = MatchAny();
						Match((int) TT.RBrack);
						// line 69
						attrs = AppendExprsInside(t, attrs);
						e = Atom(context, ref attrs);
					}
					break;
				case TT.LParen:
					{
						var t = MatchAny();
						var rp = Match((int) TT.RParen);
						// line 72
						e = ParseParens(t, rp.EndIndex);
					}
					break;
				case TT.LBrace:
					{
						var t = MatchAny();
						var rb = Match((int) TT.RBrace);
						// line 74
						e = ParseBraces(t, rb.EndIndex);
					}
					break;
				default:
					goto error;
				}
				break;
			match5:
				{
					Skip();
					var t = Match((int) TT.Indent);
					var rb = Match((int) TT.Dedent);
					// Line 59: greedy(TT.Colon)?
					la0 = (TT) LA0;
					if (la0 == TT.Colon)
						Skip();
					// line 60
					e = ParseBraces(t, rb.EndIndex);
				}
				break;
			match6:
				{
					Check(context != P.SuperExpr, "context != P.SuperExpr");
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex);
					e.BaseStyle = NodeStyle.Operator;
				}
				break;
			error:
				{
					// line 76
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "Expected an expression here");
				}
			} while (false);
			// line 80
			return e;
		}
		LNode Expr(Precedence context, out LNode primary)
		{
			TT la1;
			// line 91
			LNode e;
			LNode _;
			Precedence prec;
			RWList<LNode> attrs = null;
			e = Atom(context, ref attrs);
			// line 93
			primary = e;
			// Line 96: greedy( (&{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} (TT.Assignment|TT.BQString|TT.Colon|TT.Dot|TT.NormalOp) Expr | &{context.CanParse(P.Primary)} TT.Not Expr | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} (TT.PreSufOp|TT.SuffixOp) | &{e.Range.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen) | (&{context.CanParse(P.Primary)} TT.LBrack TT.RBrack / &{context.CanParse(P.SuperExpr)} Expr greedy(Expr)*) )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Colon:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							if (context.CanParse(P.SuperExpr)) {
								switch ((TT) LA(1)) {
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
									goto match1;
								case TT.Indent:
									goto matchExpr;
								default:
									goto stop2;
								}
							} else {
								switch ((TT) LA(1)) {
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
									goto match1;
								default:
									goto stop2;
								}
							}
						} else if (context.CanParse(P.SuperExpr)) {
							switch ((TT) LA(1)) {
							case TT.Assignment:
							case TT.At:
							case TT.BQString:
							case TT.Colon:
							case TT.Dot:
							case TT.Id:
							case TT.Indent:
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
								goto matchExpr;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
				case TT.Assignment:
				case TT.BQString:
				case TT.Dot:
				case TT.NormalOp:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							switch ((TT) LA(1)) {
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
								goto match1;
							default:
								goto stop2;
							}
						} else if (context.CanParse(P.SuperExpr)) {
							switch ((TT) LA(1)) {
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
								goto matchExpr;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
				case TT.Not:
					{
						if (context.CanParse(P.Primary)) {
							switch ((TT) LA(1)) {
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
									// line 108
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
							switch ((TT) LA(1)) {
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
								goto matchExpr;
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
							switch ((TT) LA(1)) {
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
								goto matchExpr;
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
							la1 = (TT) LA(1);
							if (la1 == TT.RParen) {
								var p = MatchAny();
								var rp = MatchAny();
								e = primary = ParseCall(e, p, rp.EndIndex);
								e.BaseStyle = NodeStyle.PrefixNotation;
							} else
								goto stop2;
						} else if (context.CanParse(P.SuperExpr)) {
							la1 = (TT) LA(1);
							if (la1 == TT.RParen)
								goto matchExpr;
							else
								goto stop2;
						} else
							goto stop2;
					}
					break;
				case TT.LBrack:
					{
						if (context.CanParse(P.Primary)) {
							la1 = (TT) LA(1);
							if (la1 == TT.RBrack) {
								var t = MatchAny();
								var rb = MatchAny();
								// line 134
								var args = new RWList<LNode> { 
									e
								};
								AppendExprsInside(t, args);
								e = primary = F.Call(S.Bracks, args.ToRVList(), e.Range.StartIndex, rb.EndIndex);
								e.BaseStyle = NodeStyle.Expression;
							} else
								goto stop2;
						} else if (context.CanParse(P.SuperExpr)) {
							la1 = (TT) LA(1);
							if (la1 == TT.RBrack)
								goto matchExpr;
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
							goto matchExpr;
						else
							goto stop2;
					}
				case TT.At:
					{
						if (context.CanParse(P.SuperExpr)) {
							la1 = (TT) LA(1);
							if (la1 == TT.LBrack)
								goto matchExpr;
							else
								goto stop2;
						} else
							goto stop2;
					}
				case TT.PrefixOp:
					{
						if (context.CanParse(P.SuperExpr)) {
							switch ((TT) LA(1)) {
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
								goto matchExpr;
							default:
								goto stop2;
							}
						} else
							goto stop2;
					}
				case TT.LBrace:
					{
						if (context.CanParse(P.SuperExpr)) {
							la1 = (TT) LA(1);
							if (la1 == TT.RBrace)
								goto matchExpr;
							else
								goto stop2;
						} else
							goto stop2;
					}
				default:
					goto stop2;
				}
				continue;
			match1:
				{
					var t = MatchAny();
					var rhs = Expr(prec, out primary);
					e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
					e.BaseStyle = NodeStyle.Operator;
					if ((!prec.CanParse(P.NullDot)))
						primary = e;
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
			matchExpr:
				{
					// line 144
					var rhs = RVList<LNode>.Empty;
					rhs.Add(Expr(P.SuperExpr, out _));
					// Line 145: greedy(Expr)*
					for (;;) {
						switch ((TT) LA0) {
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
								la1 = (TT) LA(1);
								if (la1 == TT.LBrack)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.PrefixOp:
						case TT.PreSufOp:
							{
								switch ((TT) LA(1)) {
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
						case TT.Colon:
							{
								switch ((TT) LA(1)) {
								case TT.Assignment:
								case TT.At:
								case TT.BQString:
								case TT.Colon:
								case TT.Dot:
								case TT.Id:
								case TT.Indent:
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
						case TT.Assignment:
						case TT.BQString:
						case TT.Dot:
						case TT.NormalOp:
						case TT.Not:
							{
								switch ((TT) LA(1)) {
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
								la1 = (TT) LA(1);
								if (la1 == TT.RBrack)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.LParen:
							{
								la1 = (TT) LA(1);
								if (la1 == TT.RParen)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
							}
							break;
						case TT.LBrace:
							{
								la1 = (TT) LA(1);
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
					// line 146
					e = MakeSuperExpr(e, ref primary, rhs);
				}
			}
		stop2:;
			// line 148
			return attrs == null ? e : e.WithAttrs(attrs.ToRVList());
		}
		protected LNode SuperExpr()
		{
			// line 156
			LNode _;
			var e = Expr(StartStmt, out _);
			// line 158
			return e;
		}
		protected LNode SuperExprOpt()
		{
			// Line 162: (SuperExpr | )
			switch ((TT) LA0) {
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
					// line 162
					return e;
				}
				break;
			default:
				// line 162
				return MissingExpr;
				break;
			}
		}
		protected void ExprList(ref RWList<LNode> exprs)
		{
			TT la0;
			// line 166
			exprs = exprs ?? new RWList<LNode>();
			// Line 167: (SuperExpr (TT.Comma SuperExprOpt)* | TT.Comma SuperExprOpt (TT.Comma SuperExprOpt)*)?
			switch ((TT) LA0) {
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
					// Line 168: (TT.Comma SuperExprOpt)*
					for (;;) {
						la0 = (TT) LA0;
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
					// line 169
					exprs.Add(MissingExpr);
					Skip();
					exprs.Add(SuperExprOpt());
					// Line 170: (TT.Comma SuperExprOpt)*
					for (;;) {
						la0 = (TT) LA0;
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
			// line 175
			LNode e = MissingExpr;
			// Line 176: (SuperExpr)?
			switch ((TT) LA0) {
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
			// line 177
			bool error = false;
			// Line 180: greedy(&{$LA != terminator} ~(EOF))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 != EOF) {
					la0 = (TT) LA0;
					if (la0 != terminator) {
						// line 181
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
			// line 189
			return e;
		}
		public void StmtList(ref RWList<LNode> exprs)
		{
			TT la0;
			// line 192
			exprs = exprs ?? new RWList<LNode>();
			var next = SuperExprOptUntil(TT.Semicolon);
			// Line 194: (TT.Semicolon SuperExprOptUntil)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Semicolon) {
					// line 194
					exprs.Add(next);
					Skip();
					next = SuperExprOptUntil(TT.Semicolon);
				} else
					break;
			}
			// line 198
			if ((next != (object) MissingExpr))
				exprs.Add(next);
		}
	}
}
