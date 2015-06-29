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
		LNode Atom(Precedence context, ref RWList<LNode> attrs)
		{
			TT la0, la1;
			// line 52
			LNode e = MissingExpr;
			LNode _;
			// Line 54: ( (TT.Id (&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen / ) | (TT.Number|TT.OtherLit|TT.SQString|TT.String|TT.Symbol) | TT.At TT.LBrack TT.RBrack | (TT.PrefixOp|TT.PreSufOp) Expr) | (TT.Colon TT.Indent TT.Dedent greedy(TT.Colon)? / &{context != P.SuperExpr} (TT.Assignment|TT.BQString|TT.Dot|TT.NormalOp|TT.Not) Expr) | TT.LBrack TT.RBrack Atom | TT.LParen TT.RParen | TT.LBrace TT.RBrace )
			switch ((TT) LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					// Line 55: (&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen / )
					la0 = (TT) LA0;
					if (la0 == TT.LParen) {
						if (t.EndIndex == LT(0).StartIndex && context.CanParse(P.Primary)) {
							la1 = (TT) LA(1);
							if (la1 == TT.RParen) {
								var p = MatchAny();
								var rp = MatchAny();
								// line 57
								e = ParseCall(t, p, rp.EndIndex);
							} else
								// line 58
								e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
						} else
							// line 58
							e = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
					} else
						// line 58
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
					// line 62
					e = F.Literal(t.Value, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.At:
				{
					Skip();
					var t = Match((int) TT.LBrack);
					var rb = Match((int) TT.RBrack);
					// line 65
					e = F.Literal(t.Children, t.StartIndex, rb.EndIndex);
				}
				break;
			case TT.PrefixOp:
			case TT.PreSufOp:
				{
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					// line 69
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetBaseStyle(NodeStyle.Operator);
				}
				break;
			case TT.Colon:
				{
					Skip();
					var t = Match((int) TT.Indent);
					var rb = Match((int) TT.Dedent);
					// Line 71: greedy(TT.Colon)?
					la0 = (TT) LA0;
					if (la0 == TT.Colon)
						Skip();
					// line 72
					e = ParseBraces(t, rb.EndIndex);
				}
				break;
			case TT.Assignment:
			case TT.BQString:
			case TT.Dot:
			case TT.NormalOp:
			case TT.Not:
				{
					Check(context != P.SuperExpr, "context != P.SuperExpr");
					var t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t), out _);
					// line 77
					e = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetBaseStyle(NodeStyle.Operator);
				}
				break;
			case TT.LBrack:
				{
					var t = MatchAny();
					Match((int) TT.RBrack);
					// line 80
					attrs = AppendExprsInside(t, attrs);
					e = Atom(context, ref attrs);
				}
				break;
			case TT.LParen:
				{
					var t = MatchAny();
					var rp = Match((int) TT.RParen);
					// line 83
					e = ParseParens(t, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var t = MatchAny();
					var rb = Match((int) TT.RBrace);
					// line 85
					e = ParseBraces(t, rb.EndIndex);
				}
				break;
			default:
				{
					// line 87
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "Expected an expression here");
				}
				break;
			}
			// line 91
			return e;
		}
		LNode Expr(Precedence context, out LNode primary)
		{
			TT la1;
			// line 102
			LNode e;
			LNode _;
			Precedence prec;
			RWList<LNode> attrs = null;
			e = Atom(context, ref attrs);
			// line 104
			primary = e;
			// Line 107: greedy( (&{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} (TT.Assignment|TT.BQString|TT.Colon|TT.Dot|TT.NormalOp) Expr | &{context.CanParse(P.Primary)} TT.Not Expr | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} (TT.PreSufOp|TT.SuffixOp) | &{e.Range.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)} TT.LParen TT.RParen | TT.At TT.LBrace TT.RBrace) | (&{context.CanParse(P.Primary)} TT.LBrack TT.RBrack / &{context.CanParse(P.SuperExpr)} Expr greedy(Expr)*) )*
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
							la1 = (TT) LA(1);
							if (la1 == TT.Indent)
								goto matchExpr;
							else
								goto stop2;
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
									// line 118
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
								// line 137
								e = primary = ParseCall(e, p, rp.EndIndex);
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
				case TT.At:
					{
						if (context.CanParse(P.SuperExpr)) {
							la1 = (TT) LA(1);
							if (la1 == TT.LBrace)
								goto match5;
							else if (la1 == TT.LBrack)
								goto matchExpr;
							else
								goto stop2;
						} else {
							la1 = (TT) LA(1);
							if (la1 == TT.LBrace)
								goto match5;
							else
								goto stop2;
						}
					}
				case TT.LBrack:
					{
						if (context.CanParse(P.Primary)) {
							la1 = (TT) LA(1);
							if (la1 == TT.RBrack) {
								var t = MatchAny();
								var rb = MatchAny();
								// line 145
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
					e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex).SetBaseStyle(NodeStyle.Operator);
					if ((!prec.CanParse(P.NullDot)))
						primary = e;
				}
				continue;
			match3:
				{
					var t = MatchAny();
					e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex).SetBaseStyle(NodeStyle.Operator);
					if ((t.Type() == TT.PreSufOp))
						primary = null;
				}
				continue;
			match5:
				{
					Skip();
					var p = MatchAny();
					var rp = Match((int) TT.RBrace);
					// line 140
					e = primary = ParseCallBraces(e, p, rp.EndIndex);
				}
				continue;
			matchExpr:
				{
					// line 155
					var rhs = RVList<LNode>.Empty;
					rhs.Add(Expr(P.SuperExpr, out _));
					// Line 156: greedy(Expr)*
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
								la1 = (TT) LA(1);
								if (la1 == TT.Indent)
									rhs.Add(Expr(P.SuperExpr, out _));
								else
									goto stop;
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
					// line 157
					e = MakeSuperExpr(e, ref primary, rhs);
				}
			}
		stop2:;
			// line 159
			return attrs == null ? e : e.WithAttrs(attrs.ToRVList());
		}
		protected LNode SuperExpr()
		{
			// line 167
			LNode _;
			var e = Expr(StartStmt, out _);
			// line 169
			return e;
		}
		protected LNode SuperExprOpt()
		{
			// Line 173: (SuperExpr | )
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
					// line 173
					return e;
				}
				break;
			default:
				// line 173
				return MissingExpr;
				break;
			}
		}
		protected void ExprList(ref RWList<LNode> exprs)
		{
			TT la0;
			// line 177
			exprs = exprs ?? new RWList<LNode>();
			// Line 178: (SuperExpr (TT.Comma SuperExprOpt)* | TT.Comma SuperExprOpt (TT.Comma SuperExprOpt)*)?
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
					// Line 179: (TT.Comma SuperExprOpt)*
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
					// line 180
					exprs.Add(MissingExpr);
					Skip();
					exprs.Add(SuperExprOpt());
					// Line 181: (TT.Comma SuperExprOpt)*
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
			// line 186
			LNode e = MissingExpr;
			// Line 187: (SuperExpr)?
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
			// line 188
			bool error = false;
			// Line 191: greedy(&{$LA != terminator} ~(EOF))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 != EOF) {
					la0 = (TT) LA0;
					if (la0 != terminator) {
						// line 192
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
			// line 200
			return e;
		}
		public void StmtList(ref RWList<LNode> exprs)
		{
			TT la0;
			// line 203
			exprs = exprs ?? new RWList<LNode>();
			var next = SuperExprOptUntil(TT.Semicolon);
			// Line 205: (TT.Semicolon SuperExprOptUntil)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Semicolon) {
					// line 205
					exprs.Add(next);
					Skip();
					next = SuperExprOptUntil(TT.Semicolon);
				} else
					break;
			}
			// line 209
			if ((next != (object) MissingExpr))
				exprs.Add(next);
		}
	}
}
