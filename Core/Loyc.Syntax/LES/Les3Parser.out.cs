// Generated from Les3Parser.ecs by LeMP custom tool. LeMP version: 1.9.2.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Loyc.Syntax;
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using P = LesPrecedence;
	using S = CodeSymbols;
	partial class Les3Parser
	{
		static readonly Symbol sy__colonsuf = (Symbol) ":suf";
		#pragma warning disable 162, 642
		protected new const TT EOF = TT.EOF;
		void CheckEndMarker(ref TokenType endMarker, ref Token end)
		{
			var endType = end.Type();
			if (endType == TokenType.Newline)
				endType = TokenType.Semicolon;
			if (endMarker != endType) {
				if (endMarker == default(TT)) {
					endMarker = endType;
				} else {
					Error(-1, "Unexpected separator: {0} should be {1}", ToString(end.TypeInt), ToString((int) endMarker));
				}
			}
		}
		void MissingEndMarker(LNode previousExpr, TokenType endMarker)
		{
			Error(0, "Expected '{0}'", endMarker == TT.Comma ? ',' : ';');
		}
		public VList<LNode> ExprList(VList<LNode> list = default(VList<LNode>), bool allowBlockCalls = true)
		{
			var endMarker = default(TT);
			return ExprList(ref endMarker, list, allowBlockCalls);
		}
		public override VList<LNode> ExprList(ref TokenType endMarker, VList<LNode> list = default(VList<LNode>))
		{
			return ExprList(ref endMarker, list, true);
		}
		protected bool _allowBlockCalls = true;
		new public VList<LNode> StmtList()
		{
			VList<LNode> result = default(VList<LNode>);
			var endMarker = TT.Semicolon;
			result = ExprList(ref endMarker);
			return result;
		}
		public VList<LNode> ExprList(ref TokenType endMarker, VList<LNode> list, bool allowBlockCalls)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			var old_allowBlockCalls_0 = _allowBlockCalls;
			_allowBlockCalls = allowBlockCalls;
			try {
				if (LT0.Value is string) {
					endMarker = TT.EOF;
				}
				;
				// Line 1: ( / TopExpr)
				switch ((TT) LA0) {
				case EOF:
				case TT.Comma:
				case TT.Newline:
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
				// Line 87: (((TT.Comma|TT.Newline) | TT.Semicolon greedy(TT.Newline)?) ( / TopExpr))*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Comma:
					case TT.Newline:
					case TT.Semicolon:
						{
							// Line 87: ((TT.Comma|TT.Newline) | TT.Semicolon greedy(TT.Newline)?)
							la0 = (TT) LA0;
							if (la0 == TT.Comma || la0 == TT.Newline)
								end = MatchAny();
							else {
								end = Match((int) TT.Semicolon);
								// Line 87: greedy(TT.Newline)?
								la0 = (TT) LA0;
								if (la0 == TT.Newline)
									Skip();
							}
							CheckEndMarker(ref endMarker, ref end);
							list.Add(e ?? MissingExpr());
							e = null;
							// Line 1: ( / TopExpr)
							switch ((TT) LA0) {
							case EOF:
							case TT.Comma:
							case TT.Newline:
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
						}
						break;
					case EOF:
					case TT.RBrace:
					case TT.RBrack:
					case TT.RParen:
						goto stop;
					default:
						{
							MissingEndMarker(e, endMarker);
							list.Add(e ?? MissingExpr());
							e = null;
							// Line 1: ( / TopExpr)
							switch ((TT) LA0) {
							case EOF:
							case TT.Comma:
							case TT.Newline:
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
						}
						break;
					}
				}
			stop:;
				if (e != null || end.Type() == TT.Comma) {
					list.Add(e ?? MissingExpr());
				}
				return list;
			} finally {
				_allowBlockCalls = old_allowBlockCalls_0;
			}
		}
		public override IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// line 100
			if (LT0.Value is string) {
				endMarker = TT.EOF;
			}
			;
			// Line 1: ( / TopExpr)
			switch ((TT) LA0) {
			case EOF:
			case TT.Comma:
			case TT.Newline:
			case TT.Semicolon:
				{
				}
				break;
			default:
				e = TopExpr();
				break;
			}
			// Line 102: (((TT.Comma|TT.Newline) | TT.Semicolon greedy(TT.Newline)*) ( / TopExpr))*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Comma:
				case TT.Newline:
				case TT.Semicolon:
					{
						// Line 102: ((TT.Comma|TT.Newline) | TT.Semicolon greedy(TT.Newline)*)
						la0 = (TT) LA0;
						if (la0 == TT.Comma || la0 == TT.Newline)
							end = MatchAny();
						else {
							end = Match((int) TT.Semicolon);
							// Line 102: greedy(TT.Newline)*
							for (;;) {
								la0 = (TT) LA0;
								if (la0 == TT.Newline)
									Skip();
								else
									break;
							}
						}
						CheckEndMarker(ref endMarker.Value, ref end);
						yield return e ?? MissingExpr();
						e = null;
						// Line 1: ( / TopExpr)
						switch ((TT) LA0) {
						case EOF:
						case TT.Comma:
						case TT.Newline:
						case TT.Semicolon:
							{
							}
							break;
						default:
							e = TopExpr();
							break;
						}
					}
					break;
				case EOF:
					goto stop;
				default:
					{
						MissingEndMarker(e, endMarker.Value);
						yield return e ?? MissingExpr();
						e = null;
						// Line 1: ( / TopExpr)
						switch ((TT) LA0) {
						case EOF:
						case TT.Comma:
						case TT.Newline:
						case TT.Semicolon:
							{
							}
							break;
						default:
							e = TopExpr();
							break;
						}
					}
					break;
				}
			}
		stop:;
			// line 111
			if (e != null || end.Type() == TT.Comma) {
				yield return e ?? MissingExpr();
			}
		}
		new protected LNode TopExpr()
		{
			TT la0, la1;
			LNode e = default(LNode);
			Token lit_colon = default(Token);
			Token litx40 = default(Token);
			// line 114
			var attrs = new VList<LNode>();
			// Line 116: (TT.At Particle)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					litx40 = MatchAny();
					attrs.Add(Particle(isAttribute: true));
				} else
					break;
			}
			// Line 118: (Expr (TT.Colon TT.Newline => )?)
			switch ((TT) LA0) {
			case TT.Assignment:
			case TT.BQId:
			case TT.BQOperator:
			case TT.Dot:
			case TT.Id:
			case TT.LBrace:
			case TT.LBrack:
			case TT.Literal:
			case TT.LParen:
			case TT.LTokenLiteral:
			case TT.NegativeLiteral:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				{
					e = Expr(StartStmt);
					// Line 120: (TT.Colon TT.Newline => )?
					la0 = (TT) LA0;
					if (la0 == TT.Colon) {
						lit_colon = MatchAny();
						// line 121
						e = F.Call(sy__colonsuf, e, e.Range.StartIndex, lit_colon.EndIndex);
					}
				}
				break;
			default:
				{
					// line 123
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 124: nongreedy(~(EOF))*
					for (;;) {
						switch ((TT) LA0) {
						case TT.At:
							{
								switch ((TT) LA(1)) {
								case EOF:
								case TT.BQId:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.Literal:
								case TT.LParen:
								case TT.LTokenLiteral:
								case TT.NegativeLiteral:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case TT.Dot:
							{
								la1 = (TT) LA(1);
								if (la1 == EOF || la1 == TT.Id)
									goto stop;
								else
									Skip();
							}
							break;
						case TT.Assignment:
						case TT.BQOperator:
						case TT.NormalOp:
						case TT.Not:
						case TT.PrefixOp:
						case TT.PreOrSufOp:
							{
								switch ((TT) LA(1)) {
								case EOF:
								case TT.Assignment:
								case TT.BQId:
								case TT.BQOperator:
								case TT.Dot:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.Literal:
								case TT.LParen:
								case TT.LTokenLiteral:
								case TT.NegativeLiteral:
								case TT.NormalOp:
								case TT.Not:
								case TT.PrefixOp:
								case TT.PreOrSufOp:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case TT.BQId:
						case TT.Id:
						case TT.Literal:
						case TT.NegativeLiteral:
							{
								switch ((TT) LA(1)) {
								case TT.Assignment:
								case TT.Colon:
								case TT.Dot:
								case TT.Id:
								case TT.LParen:
								case TT.NegativeLiteral:
								case TT.NormalOp:
								case TT.PreOrSufOp:
									goto stop;
								case TT.LBrace:
									{
										if (_allowBlockCalls)
											goto stop;
										else
											Skip();
									}
									break;
								case EOF:
								case TT.Comma:
								case TT.LBrack:
								case TT.Newline:
								case TT.Not:
								case TT.RBrace:
								case TT.RBrack:
								case TT.RParen:
								case TT.Semicolon:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case TT.LTokenLiteral:
							goto stop;
						case TT.LBrace:
							{
								switch ((TT) LA(1)) {
								case EOF:
								case TT.Assignment:
								case TT.At:
								case TT.BQId:
								case TT.BQOperator:
								case TT.Comma:
								case TT.Dot:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.Literal:
								case TT.LParen:
								case TT.LTokenLiteral:
								case TT.NegativeLiteral:
								case TT.Newline:
								case TT.NormalOp:
								case TT.Not:
								case TT.PrefixOp:
								case TT.PreOrSufOp:
								case TT.RBrace:
								case TT.Semicolon:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case TT.LBrack:
							{
								switch ((TT) LA(1)) {
								case EOF:
								case TT.Assignment:
								case TT.At:
								case TT.BQId:
								case TT.BQOperator:
								case TT.Comma:
								case TT.Dot:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.Literal:
								case TT.LParen:
								case TT.LTokenLiteral:
								case TT.NegativeLiteral:
								case TT.Newline:
								case TT.NormalOp:
								case TT.Not:
								case TT.PrefixOp:
								case TT.PreOrSufOp:
								case TT.RBrack:
								case TT.Semicolon:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case TT.LParen:
							{
								switch ((TT) LA(1)) {
								case EOF:
								case TT.Assignment:
								case TT.At:
								case TT.BQId:
								case TT.BQOperator:
								case TT.Comma:
								case TT.Dot:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.Literal:
								case TT.LParen:
								case TT.LTokenLiteral:
								case TT.NegativeLiteral:
								case TT.Newline:
								case TT.NormalOp:
								case TT.Not:
								case TT.PrefixOp:
								case TT.PreOrSufOp:
								case TT.RParen:
								case TT.Semicolon:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case TT.RBrace:
						case TT.RBrack:
						case TT.RParen:
							{
								switch ((TT) LA(1)) {
								case EOF:
								case TT.Comma:
								case TT.Newline:
								case TT.RBrace:
								case TT.RBrack:
								case TT.RParen:
								case TT.Semicolon:
									goto stop;
								default:
									Skip();
									break;
								}
							}
							break;
						case EOF:
							goto stop;
						default:
							Skip();
							break;
						}
					}
				stop:;
					// Line 125: (TopExpr | (EOF|TT.RBrace|TT.RBrack|TT.RParen))
					switch ((TT) LA0) {
					case TT.Assignment:
					case TT.At:
					case TT.BQId:
					case TT.BQOperator:
					case TT.Dot:
					case TT.Id:
					case TT.LBrace:
					case TT.LBrack:
					case TT.Literal:
					case TT.LParen:
					case TT.LTokenLiteral:
					case TT.NegativeLiteral:
					case TT.NormalOp:
					case TT.Not:
					case TT.PrefixOp:
					case TT.PreOrSufOp:
						e = TopExpr();
						break;
					default:
						{
							Match((int) EOF, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen);
							// line 125
							e = MissingExpr();
						}
						break;
					}
				}
				break;
			}
			// line 127
			if (litx40.TypeInt != 0) {
				e = e.WithRange(litx40.StartIndex, e.Range.EndIndex);
			}
			;
			return e.PlusAttrsBefore(attrs);
		}
		LNode KeywordExpression()
		{
			TT la0;
			LNode got_Expr = default(LNode);
			Token id = default(Token);
			Token litx2E = default(Token);
			LNode result = default(LNode);
			var old_allowBlockCalls_1 = _allowBlockCalls;
			_allowBlockCalls = false;
			try {
				litx2E = MatchAny();
				Check(LT0.StartIndex == litx2E.EndIndex, "LT0.StartIndex == litx2E.EndIndex");
				id = Match((int) TT.Id);
				got_Expr = Expr(StartStmt);
				var keyword = GSymbol.Get("." + id.Value.ToString());
				var args = new VList<LNode>(got_Expr);
				// Line 140: (BracesWithContinuators)?
				la0 = (TT) LA0;
				if (la0 == TT.LBrace) {
					switch ((TT) LA(1)) {
					case TT.Assignment:
					case TT.At:
					case TT.BQId:
					case TT.BQOperator:
					case TT.Comma:
					case TT.Dot:
					case TT.Id:
					case TT.LBrace:
					case TT.LBrack:
					case TT.Literal:
					case TT.LParen:
					case TT.LTokenLiteral:
					case TT.NegativeLiteral:
					case TT.Newline:
					case TT.NormalOp:
					case TT.Not:
					case TT.PrefixOp:
					case TT.PreOrSufOp:
					case TT.RBrace:
					case TT.Semicolon:
						BracesWithContinuators(ref args);
						break;
					}
				}
				result = MarkSpecial(F.Call(keyword, args, litx2E.StartIndex, args.Last.Range.EndIndex));
				return result;
			} finally {
				_allowBlockCalls = old_allowBlockCalls_1;
			}
		}
		int BracesWithContinuators(ref VList<LNode> args)
		{
			TT la0, la1;
			LNode bb = default(LNode);
			VList<LNode> got_ExprList = default(VList<LNode>);
			Token kw = default(Token);
			Token lit_rpar = default(Token);
			int endIndex;
			bb = BracedBlock();
			// line 144
			args.Add(bb);
			endIndex = bb.Range.EndIndex;
			// Line 146: greedy(ContinuatorKeyword (BracedBlock | TT.LParen ExprList TT.RParen (BracedBlock / )))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Id) {
					la1 = (TT) LA(1);
					if (la1 == TT.LBrace || la1 == TT.LParen) {
						kw = ContinuatorKeyword();
						// line 146
						var opName = Continuators[kw.Value];
						// Line 147: (BracedBlock | TT.LParen ExprList TT.RParen (BracedBlock / ))
						la0 = (TT) LA0;
						if (la0 == TT.LBrace) {
							bb = BracedBlock();
							// line 147
							args.Add(F.Call(opName, bb));
						} else {
							Match((int) TT.LParen);
							got_ExprList = ExprList();
							lit_rpar = Match((int) TT.RParen);
							// Line 149: (BracedBlock / )
							la0 = (TT) LA0;
							if (la0 == TT.LBrace) {
								bb = BracedBlock();
								// line 150
								args.Add(F.Call(opName, got_ExprList.Add(bb), kw.StartIndex, lit_rpar.EndIndex));
							} else
								// line 151
								args.Add(F.Call(opName, got_ExprList, kw.StartIndex, bb.Range.EndIndex));
						}
					} else
						break;
				} else
					break;
			}
			// line 154
			return endIndex;
		}
		LNode BracedBlock()
		{
			TT la0;
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			lit_lcub = Match((int) TT.LBrace);
			// Line 157: greedy(TT.Newline)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline) {
					switch ((TT) LA(1)) {
					case TT.Assignment:
					case TT.At:
					case TT.BQId:
					case TT.BQOperator:
					case TT.Comma:
					case TT.Dot:
					case TT.Id:
					case TT.LBrace:
					case TT.LBrack:
					case TT.Literal:
					case TT.LParen:
					case TT.LTokenLiteral:
					case TT.NegativeLiteral:
					case TT.Newline:
					case TT.NormalOp:
					case TT.Not:
					case TT.PrefixOp:
					case TT.PreOrSufOp:
					case TT.RBrace:
					case TT.Semicolon:
						Skip();
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		stop:;
			var stmts = StmtList();
			lit_rcub = Match((int) TT.RBrace);
			// line 158
			return F.Call(S.Braces, stmts, lit_lcub.StartIndex, lit_rcub.EndIndex).SetStyle(NodeStyle.Statement);
		}
		LNode Parentheses()
		{
			VList<LNode> exprs = default(VList<LNode>);
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			var endMarker = default(TokenType);
			lit_lpar = Match((int) TT.LParen);
			exprs = ExprList(ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 161
			return exprs.Count == 1 && endMarker != TT.Semicolon ? exprs[0] : F.Tuple(exprs, lit_lpar.StartIndex, lit_rpar.EndIndex);
		}
		Token ContinuatorKeyword()
		{
			Token result = default(Token);
			Check(Continuators.ContainsKey(LT(0).Value), "Continuators.ContainsKey(LT($LI).Value)");
			result = MatchAny();
			return result;
		}
		LNode Expr(Precedence context)
		{
			TT la0;
			LNode e = default(LNode);
			Token op2 = default(Token);
			Token t = default(Token);
			// Line 180: (KeywordExpression | PrefixExpr greedy( &{CanParse(context, $LI, out prec)} ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / ) ) Expr | &{context.CanParse(P.Add)} TT.NegativeLiteral | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp | &{context.CanParse(P.Primary)} FinishPrimaryExpr )*)
			la0 = (TT) LA0;
			if (la0 == TT.Dot)
				e = KeywordExpression();
			else {
				// line 181
				Precedence prec;
				e = PrefixExpr(context);
				// Line 185: greedy( &{CanParse(context, $LI, out prec)} ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / ) ) Expr | &{context.CanParse(P.Add)} TT.NegativeLiteral | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp | &{context.CanParse(P.Primary)} FinishPrimaryExpr )*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment:
					case TT.Dot:
					case TT.NormalOp:
						{
							if (CanParse(context, 0, out prec))
								goto matchExpr;
							else
								goto stop;
						}
					case TT.Colon:
						{
							if ((TT) LA(0 + 1) != TT.Newline) {
								if (CanParse(context, 0, out prec))
									goto matchExpr;
								else
									goto stop;
							} else
								goto stop;
						}
					case TT.Id:
						{
							if (CanParse(context, 0, out prec))
								goto matchExpr;
							else
								goto stop;
						}
					case TT.NegativeLiteral:
						{
							if (context.CanParse(P.Add)) {
								var rhs = MatchAny();
								// line 199
								e = F.Call(S.Sub, e, ToPositiveLiteral(rhs), e.Range.StartIndex, rhs.EndIndex).SetStyle(NodeStyle.Operator);
							} else
								goto stop;
						}
						break;
					case TT.PreOrSufOp:
						{
							if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
								t = MatchAny();
								// line 203
								e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex).SetStyle(NodeStyle.Operator);
							} else
								goto stop;
						}
						break;
					case TT.LParen:
						{
							if (context.CanParse(P.Primary))
								e = FinishPrimaryExpr(e);
							else
								goto stop;
						}
						break;
					case TT.LBrace:
						{
							if (_allowBlockCalls) {
								if (context.CanParse(P.Primary))
									e = FinishPrimaryExpr(e);
								else
									goto stop;
							} else
								goto stop;
						}
						break;
					case TT.LBrack:
					case TT.Not:
						{
							if (context.CanParse(P.Primary))
								e = FinishPrimaryExpr(e);
							else
								goto stop;
						}
						break;
					default:
						goto stop;
					}
					continue;
				matchExpr:
					{
						// line 186
						Symbol opName;
						// Line 187: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / ) )
						switch ((TT) LA0) {
						case TT.Assignment:
						case TT.Dot:
						case TT.NormalOp:
							{
								var op = MatchAny();
								// Line 187: (TT.Newline)*
								for (;;) {
									la0 = (TT) LA0;
									if (la0 == TT.Newline)
										Skip();
									else
										break;
								}
								// line 187
								opName = (Symbol) op.Value;
							}
							break;
						case TT.Colon:
							{
								Check((TT) LA(0 + 1) != TT.Newline, "(TT) LA($LI + 1) != TT.Newline");
								var op = MatchAny();
								// line 188
								opName = (Symbol) op.Value;
							}
							break;
						default:
							{
								var op = Match((int) TT.Id);
								// Line 191: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / )
								la0 = (TT) LA0;
								if (la0 == TT.Assignment || la0 == TT.Dot || la0 == TT.NormalOp) {
									if (op.EndIndex == LT0.StartIndex) {
										op2 = MatchAny();
										// line 192
										opName = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
									} else
										// line 193
										opName = GSymbol.Get("'" + op.Value.ToString());
								} else
									// line 193
									opName = GSymbol.Get("'" + op.Value.ToString());
							}
							break;
						}
						var rhs = Expr(prec);
						// line 196
						e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex).SetStyle(NodeStyle.Operator);
					}
				}
			stop:;
			}
			// line 208
			return e;
		}
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			LNode result = default(LNode);
			// Line 213: ( CallArgs | TT.Not (TT.LParen ExprList TT.RParen / Expr) | TT.LBrack ExprList TT.RBrack )
			la0 = (TT) LA0;
			if (la0 == TT.LBrace || la0 == TT.LParen)
				result = CallArgs(e);
			else if (la0 == TT.Not) {
				Skip();
				// line 216
				var args = new VList<LNode> { 
					e
				};
				int endIndex;
				// Line 217: (TT.LParen ExprList TT.RParen / Expr)
				la0 = (TT) LA0;
				if (la0 == TT.LParen) {
					Skip();
					args = ExprList(args);
					var c = Match((int) TT.RParen);
					// line 217
					endIndex = c.EndIndex;
				} else {
					var T = Expr(P.Primary);
					// line 218
					args.Add(T);
					endIndex = T.Range.EndIndex;
				}
				// line 220
				return F.Call(S.Of, args, e.Range.StartIndex, endIndex).SetStyle(NodeStyle.Operator);
			} else {
				// line 222
				var args = new VList<LNode> { 
					e
				};
				Match((int) TT.LBrack);
				args = ExprList(args);
				var c = Match((int) TT.RBrack);
				// line 224
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, c.EndIndex).SetStyle(NodeStyle.Operator);
			}
			return result;
		}
		LNode CallArgs(LNode target)
		{
			TT la0;
			VList<LNode> args = default(VList<LNode>);
			int endIndex = 0;
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			var endMarker = default(TokenType);
			bool hasBraces = false;
			// Line 230: (TT.LParen ExprList TT.RParen greedy(&{_allowBlockCalls} BracesWithContinuators)? | &{_allowBlockCalls} BracesWithContinuators)
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				Skip();
				args = ExprList(ref endMarker);
				lit_rpar = Match((int) TT.RParen);
				// line 230
				endIndex = lit_rpar.EndIndex;
				// Line 232: greedy(&{_allowBlockCalls} BracesWithContinuators)?
				la0 = (TT) LA0;
				if (la0 == TT.LBrace) {
					if (_allowBlockCalls) {
						switch ((TT) LA(1)) {
						case TT.Assignment:
						case TT.At:
						case TT.BQId:
						case TT.BQOperator:
						case TT.Comma:
						case TT.Dot:
						case TT.Id:
						case TT.LBrace:
						case TT.LBrack:
						case TT.Literal:
						case TT.LParen:
						case TT.LTokenLiteral:
						case TT.NegativeLiteral:
						case TT.Newline:
						case TT.NormalOp:
						case TT.Not:
						case TT.PrefixOp:
						case TT.PreOrSufOp:
						case TT.RBrace:
						case TT.Semicolon:
							{
								endIndex = BracesWithContinuators(ref args);
								// line 232
								hasBraces = true;
							}
							break;
						}
					}
				}
			} else {
				Check(_allowBlockCalls, "_allowBlockCalls");
				endIndex = BracesWithContinuators(ref args);
				// line 233
				hasBraces = true;
			}
			// line 235
			result = F.Call(target, args, target.Range.StartIndex, endIndex).SetBaseStyle(NodeStyle.PrefixNotation);
			if (hasBraces)
				MarkSpecial(result);
			if (endMarker == TT.Semicolon) {
				result.Style |= NodeStyle.Alternate;
			}
			;
			return result;
		}
		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			LNode result = default(LNode);
			Token t = default(Token);
			// Line 242: ((TT.Assignment|TT.BQOperator|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment:
			case TT.BQOperator:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				{
					t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t));
					// line 244
					result = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetStyle(NodeStyle.Operator);
				}
				break;
			default:
				result = Particle();
				break;
			}
			return result;
		}
		LNode Particle(bool isAttribute = false)
		{
			Token c = default(Token);
			Token lit_apos_lcub = default(Token);
			Token lit_lpar = default(Token);
			Token lit_lsqb = default(Token);
			Token lit_rpar = default(Token);
			Token lit_rsqb = default(Token);
			LNode result = default(LNode);
			TokenTree tree = default(TokenTree);
			// Line 257: ( (TT.BQId|TT.Id) | (TT.Literal|TT.NegativeLiteral) | TT.LTokenLiteral TokenTree TT.RBrace | BracedBlock | TT.LBrack ExprList TT.RBrack | TT.LParen ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.BQId:
			case TT.Id:
				{
					var id = MatchAny();
					// line 258
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
			case TT.NegativeLiteral:
				{
					var lit = MatchAny();
					// line 260
					result = F.Literal(lit);
				}
				break;
			case TT.LTokenLiteral:
				{
					lit_apos_lcub = MatchAny();
					tree = TokenTree();
					c = Match((int) TT.RBrace);
					// line 263
					result = F.Literal(tree, lit_apos_lcub.StartIndex, c.EndIndex);
				}
				break;
			case TT.LBrace:
				result = BracedBlock();
				break;
			case TT.LBrack:
				{
					lit_lsqb = MatchAny();
					var list = ExprList();
					lit_rsqb = Match((int) TT.RBrack);
					// line 268
					result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex).SetStyle(NodeStyle.Expression);
				}
				break;
			case TT.LParen:
				{
					// line 270
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					var saveParens = !isAttribute && (TT) LA0 != TT.At;
					var old_allowBlockCalls_2 = _allowBlockCalls;
					_allowBlockCalls = true;
					try {
						var list = ExprList(ref endMarker);
						lit_rpar = Match((int) TT.RParen);
						// line 275
						if (endMarker == TT.Semicolon || list.Count != 1) {
							result = F.Call(S.Tuple, list, lit_lpar.StartIndex, lit_rpar.EndIndex);
							if (endMarker == TT.Comma) {
								var msg = "Tuples require ';' as a separator.";
								ErrorSink.Write(Severity.Error, list[0].Range.End, msg);
							}
							;
						} else {
							result = saveParens ? F.InParens(list[0], lit_lpar.StartIndex, lit_rpar.EndIndex) : list[0];
						}
						;
					} finally {
						_allowBlockCalls = old_allowBlockCalls_2;
					}
				}
				break;
			default:
				{
					// line 286
					Error(0, "Expected a particle (id, literal, {braces} or (parens)).");
					result = MissingExpr();
				}
				break;
			}
			return result;
		}
		TokenTree TokenTree()
		{
			TT la1;
			TokenTree got_TokenTree = default(TokenTree);
			TokenTree result = default(TokenTree);
			result = new TokenTree(SourceFile);
			// Line 293: nongreedy((TT.LBrace|TT.LBrack|TT.LParen) TokenTree (TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
			for (;;) {
				switch ((TT) LA0) {
				case EOF:
				case TT.RBrace:
				case TT.RBrack:
				case TT.RParen:
					goto stop;
				case TT.LBrace:
				case TT.LBrack:
				case TT.LParen:
					{
						la1 = (TT) LA(1);
						if (la1 != (TT) EOF) {
							var open = MatchAny();
							got_TokenTree = TokenTree();
							// line 295
							result.Add(open.WithValue(got_TokenTree));
							result.Add(Match((int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen));
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
