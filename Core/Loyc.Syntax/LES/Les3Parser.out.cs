// Generated from Les3Parser.ecs by LeMP custom tool. LeMP version: 1.9.4.0
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
using Loyc;	// optional (for IMessageSink, Symbol, etc.)
using Loyc.Collections;	// optional (many handy interfaces & classes)
using Loyc.Syntax.Lexing;	// For BaseLexer
using Loyc.Syntax;	// For BaseParser<Token> and LNode
namespace Loyc.Syntax.Les {
	using TT = TokenType;	// Abbreviate TokenType as TT
	using P = LesPrecedence;
	using S = CodeSymbols;

	partial class Les3Parser {
		static readonly Symbol sy__apos_colonsuf = (Symbol) "':suf";
		#pragma warning disable 162, 642
	
		protected new const TT EOF = TT.EOF;
	
		void CheckEndMarker(ref TokenType endMarker, ref Token end) {
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
		void MissingEndMarker(LNode previousExpr, TokenType endMarker) {
			SourcePos location = SourceFile.IndexToLine(LT(-1).EndIndex + 1);
			ErrorSink.Write(Severity.Error, location, "Expected '{0}'", endMarker == TT.Comma ? ',' : ';');
		}
		public VList<LNode> ExprList(VList<LNode> list = default(VList<LNode>), bool allowBlockCalls = true) {
			var endMarker = default(TT);
			return ExprList(ref endMarker, list, allowBlockCalls);
		}
		public override VList<LNode> ExprList(ref TokenType endMarker, VList<LNode> list = default(VList<LNode>)) {
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
				// Line 83: greedy(TT.Newline)*
				for (;;) {
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					else
						break;
				}
				// Line 1: ( / TopExpr)
				switch ((TT) LA0) {
				case EOF:
				case TT.Comma:
				case TT.Newline:
				case TT.RBrace:
				case TT.RBrack:
				case TT.RParen:
				case TT.Semicolon:
					{ }
					break;
				default:
					e = TopExpr();
					break;
				}
				// Line 89: ((TT.Comma|TT.Newline|TT.Semicolon) greedy(TT.Newline)* ( / TopExpr))*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Comma:
					case TT.Newline:
					case TT.Semicolon:
						{
							end = MatchAny();
							// Line 89: greedy(TT.Newline)*
							for (;;) {
								la0 = (TT) LA0;
								if (la0 == TT.Newline)
									Skip();
								else
									break;
							}
							CheckEndMarker(ref endMarker, ref end);
							list.Add(e ?? MissingExpr(end));
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
								{ }
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
							list.Add(e ?? MissingExpr(LT0));
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
								{ }
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
					list.Add(e ?? MissingExpr(end));
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
			// line 102
			if (LT0.Value is string) {
				endMarker = TT.EOF;
			}
			;
			// Line 103: greedy(TT.Newline)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline)
					Skip();
				else
					break;
			}
			// Line 1: ( / TopExpr)
			switch ((TT) LA0) {
			case EOF:
			case TT.Comma:
			case TT.Newline:
			case TT.Semicolon:
				{ }
				break;
			default:
				e = TopExpr();
				break;
			}
			// Line 105: ((TT.Comma|TT.Newline|TT.Semicolon) greedy(TT.Newline)* ( / TopExpr))*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Comma:
				case TT.Newline:
				case TT.Semicolon:
					{
						end = MatchAny();
						// Line 105: greedy(TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
						CheckEndMarker(ref endMarker.Value, ref end);
						yield return e ?? MissingExpr(end);
						e = null;
						// Line 1: ( / TopExpr)
						switch ((TT) LA0) {
						case EOF:
						case TT.Comma:
						case TT.Newline:
						case TT.Semicolon:
							{ }
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
						yield return e ?? MissingExpr(LT0);
						e = null;
						// Line 1: ( / TopExpr)
						switch ((TT) LA0) {
						case EOF:
						case TT.Comma:
						case TT.Newline:
						case TT.Semicolon:
							{ }
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
			// line 114
			if (e != null || end.Type() == TT.Comma) {
				yield return e ?? MissingExpr(end);
			}
		}
	
		new protected LNode TopExpr()
		{
			TT la0, la1;
			LNode e = default(LNode);
			Token lit_colon = default(Token);
			int startIndex = LT0.StartIndex;
			var attrs = new VList<LNode>();
			// Line 120: (TT.At Particle greedy(TT.Newline)*)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Skip();
					attrs.Add(Particle(isAttribute: true));
					// Line 120: greedy(TT.Newline)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						else
							break;
					}
				} else
					break;
			}
			// Line 122: (Expr (TT.Colon (EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen) => )?)
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
					// Line 124: (TT.Colon (EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen) => )?
					la0 = (TT) LA0;
					if (la0 == TT.Colon) {
						lit_colon = MatchAny();
						// line 125
						e = F.Call(sy__apos_colonsuf, e, e.Range.StartIndex, lit_colon.EndIndex, lit_colon.StartIndex, lit_colon.EndIndex);
					}
				}
				break;
			default:
				{
					// line 127
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 128: nongreedy(~(EOF))*
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
				stop:;
					// Line 129: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => )
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
						// line 129
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			if (!attrs.IsEmpty) {
				e = e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
			}
			return e;
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
				// Line 144: greedy(BracesWithContinuators)?
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
				result = MarkSpecial(F.Call(keyword, args, litx2E.StartIndex, args.Last.Range.EndIndex, litx2E.StartIndex, id.EndIndex));
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
			// line 148
			args.Add(bb);
			endIndex = bb.Range.EndIndex;
			// Line 150: greedy(ContinuatorKeyword (BracedBlock | TT.LParen ExprList TT.RParen (BracedBlock / )))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Id) {
					la1 = (TT) LA(1);
					if (la1 == TT.LBrace || la1 == TT.LParen) {
						kw = ContinuatorKeyword();
						// line 150
						var opName = Continuators[kw.Value];
						// Line 151: (BracedBlock | TT.LParen ExprList TT.RParen (BracedBlock / ))
						la0 = (TT) LA0;
						if (la0 == TT.LBrace) {
							bb = BracedBlock();
							// line 151
							args.Add(F.Call(opName, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex));
						} else {
							Match((int) TT.LParen);
							got_ExprList = ExprList();
							lit_rpar = Match((int) TT.RParen);
							// Line 153: (BracedBlock / )
							la0 = (TT) LA0;
							if (la0 == TT.LBrace) {
								bb = BracedBlock();
								// line 154
								args.Add(F.Call(opName, got_ExprList.Add(bb), kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex));
							} else
								// line 155
								args.Add(F.Call(opName, got_ExprList, kw.StartIndex, lit_rpar.EndIndex, kw.StartIndex, kw.EndIndex));
						}
					} else
						break;
				} else
					break;
			}
			// line 158
			return endIndex;
		}
	
		LNode BracedBlock()
		{
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			lit_lcub = Match((int) TT.LBrace);
			var stmts = StmtList();
			lit_rcub = Match((int) TT.RBrace);
			// line 162
			return F.Call(S.Braces, stmts, lit_lcub.StartIndex, lit_rcub.EndIndex, lit_lcub.StartIndex, lit_lcub.EndIndex).SetStyle(NodeStyle.Statement);
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
			// line 165
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
			Token op = default(Token);
			Token op2 = default(Token);
			Token t = default(Token);
			// Line 184: (KeywordExpression | PrefixExpr greedy( &{CanParse(context, $LI, out prec)} ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / ) ) Expr | &{context.CanParse(P.Add)} TT.NegativeLiteral | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp | &{context.CanParse(P.Primary)} FinishPrimaryExpr )*)
			la0 = (TT) LA0;
			if (la0 == TT.Dot)
				e = KeywordExpression();
			else {
				// line 185
				Precedence prec;
				e = PrefixExpr(context);
				// Line 189: greedy( &{CanParse(context, $LI, out prec)} ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / ) ) Expr | &{context.CanParse(P.Add)} TT.NegativeLiteral | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp | &{context.CanParse(P.Primary)} FinishPrimaryExpr )*
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
								// line 203
								e = F.Call(S.Sub, e, ToPositiveLiteral(rhs), e.Range.StartIndex, rhs.EndIndex, rhs.StartIndex, rhs.StartIndex + 1, NodeStyle.Operator);
							} else
								goto stop;
						}
						break;
					case TT.PreOrSufOp:
						{
							if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
								t = MatchAny();
								// line 207
								e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
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
						// line 190
						Symbol opName;
						// Line 191: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / ) )
						switch ((TT) LA0) {
						case TT.Assignment:
						case TT.Dot:
						case TT.NormalOp:
							{
								op = MatchAny();
								// Line 191: (TT.Newline)*
								for (;;) {
									la0 = (TT) LA0;
									if (la0 == TT.Newline)
										Skip();
									else
										break;
								}
								// line 191
								opName = (Symbol) op.Value;
							}
							break;
						case TT.Colon:
							{
								Check((TT) LA(0 + 1) != TT.Newline, "(TT) LA($LI + 1) != TT.Newline");
								op = MatchAny();
								// line 192
								opName = (Symbol) op.Value;
							}
							break;
						default:
							{
								op = Match((int) TT.Id);
								// Line 195: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / )
								la0 = (TT) LA0;
								if (la0 == TT.Assignment || la0 == TT.Dot || la0 == TT.NormalOp) {
									if (op.EndIndex == LT0.StartIndex) {
										op2 = MatchAny();
										// line 196
										opName = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
									} else
										// line 197
										opName = GSymbol.Get("'" + op.Value.ToString());
								} else
									// line 197
									opName = GSymbol.Get("'" + op.Value.ToString());
							}
							break;
						}
						var rhs = Expr(prec);
						// line 200
						e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
					}
				}
			stop:;
			}
			// line 212
			return e;
		}
	
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			Token lit_excl = default(Token);
			LNode result = default(LNode);
			// Line 217: ( CallArgs | TT.Not (TT.LParen ExprList TT.RParen / Expr) | TT.LBrack ExprList TT.RBrack )
			la0 = (TT) LA0;
			if (la0 == TT.LBrace || la0 == TT.LParen)
				result = CallArgs(e);
			else if (la0 == TT.Not) {
				lit_excl = MatchAny();
				// line 220
				var args = new VList<LNode> { 
					e
				};
				int endIndex;
				// Line 221: (TT.LParen ExprList TT.RParen / Expr)
				la0 = (TT) LA0;
				if (la0 == TT.LParen) {
					Skip();
					args = ExprList(args);
					var c = Match((int) TT.RParen);
					// line 221
					endIndex = c.EndIndex;
				} else {
					var T = Expr(P.Primary);
					// line 222
					args.Add(T);
					endIndex = T.Range.EndIndex;
				}
				// line 224
				return F.Call(S.Of, args, e.Range.StartIndex, endIndex, lit_excl.StartIndex, lit_excl.EndIndex, NodeStyle.Operator);
			} else {
				// line 226
				var args = new VList<LNode> { 
					e
				};
				var lb = Match((int) TT.LBrack);
				args = ExprList(args);
				var rb = Match((int) TT.RBrack);
				// line 228
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
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
			// Line 234: (TT.LParen ExprList TT.RParen greedy(&{_allowBlockCalls} BracesWithContinuators)? | &{_allowBlockCalls} BracesWithContinuators)
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				Skip();
				args = ExprList(ref endMarker);
				lit_rpar = Match((int) TT.RParen);
				// line 234
				endIndex = lit_rpar.EndIndex;
				// Line 236: greedy(&{_allowBlockCalls} BracesWithContinuators)?
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
								// line 236
								hasBraces = true;
							}
							break;
						}
					}
				}
			} else {
				Check(_allowBlockCalls, "_allowBlockCalls");
				endIndex = BracesWithContinuators(ref args);
				// line 237
				hasBraces = true;
			}
			// line 239
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
			Token op = default(Token);
			LNode result = default(LNode);
			// Line 246: ((TT.Assignment|TT.BQOperator|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment:
			case TT.BQOperator:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				{
					op = MatchAny();
					e = Expr(PrefixPrecedenceOf(op));
					// line 248
					result = F.Call(op, e, op.StartIndex, e.Range.EndIndex, NodeStyle.Operator);
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
			// Line 261: ( (TT.BQId|TT.Id) | (TT.Literal|TT.NegativeLiteral) | TT.LTokenLiteral TokenTree TT.RBrace | BracedBlock | TT.LBrack ExprList TT.RBrack | TT.LParen ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.BQId:
			case TT.Id:
				{
					var id = MatchAny();
					// line 262
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
			case TT.NegativeLiteral:
				{
					var lit = MatchAny();
					// line 264
					result = F.Literal(lit);
				}
				break;
			case TT.LTokenLiteral:
				{
					lit_apos_lcub = MatchAny();
					tree = TokenTree();
					c = Match((int) TT.RBrace);
					// line 267
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
					// line 272
					result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex).SetStyle(NodeStyle.Expression);
				}
				break;
			case TT.LParen:
				{
					// line 274
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					var saveParens = !isAttribute && (TT) LA0 != TT.At;
					var old_allowBlockCalls_2 = _allowBlockCalls;
					_allowBlockCalls = true;
					try {
						var list = ExprList(ref endMarker);
						lit_rpar = Match((int) TT.RParen);
						// line 279
						if (endMarker == TT.Semicolon || list.Count != 1) {
							result = F.Call(S.Tuple, list, lit_lpar.StartIndex, lit_rpar.EndIndex, lit_lpar.StartIndex, lit_lpar.EndIndex);
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
					// line 290
					Error(0, "Expected a particle (id, literal, {braces} or (parens)).");
					result = MissingExpr(LT0);
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
			// Line 297: nongreedy((TT.LBrace|TT.LBrack|TT.LParen) TokenTree (TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
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
							// line 299
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
}	// braces around the rest of the file are optional
