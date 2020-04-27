// Generated from Les3Parser.ecs by LeMP custom tool. LeMP version: 2.8.0.0
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
using Loyc;	// for IMessageSink, Symbol, etc.
using Loyc.Collections;	// many handy interfaces & classes
using Loyc.Collections.Impl;
using Loyc.Syntax.Lexing;	// For BaseLexer
using Loyc.Syntax;	// For BaseParser<Token> and LNode
namespace Loyc.Syntax.Les
{
	using TT = TokenType;	// Abbreviate TokenType as TT
	using P = LesPrecedence;
	using S = CodeSymbols;

	partial class Les3Parser {
		static readonly Symbol sy__apos_lpar_rpar = (Symbol) "'()";
		#pragma warning disable 162, 642
	
		protected new const TT EOF = TT.EOF;
	
		void MissingEndMarker(LNode previousExpr, TokenType endMarker) {
			var location = new SourceRange(SourceFile, LT(-1).EndIndex + 1);
			ErrorSink.Write(Severity.Error, location, "Expected '{0}'", endMarker == TT.Comma ? ',' : ';');
		}
		public LNodeList ExprList(LNode firstItem = null) {
			var endMarker = default(TT);
			return ExprList(ref endMarker, firstItem, isBracedBlock: false);
		}
		public bool IsSpaceBefore(int li) {
			return LT(li).StartIndex > LT(li - 1).EndIndex;
		}
		public void CheckForSpace(bool expectSpace, string errorMsg) {
			if ((LT0.StartIndex == LT(-1).EndIndex) == expectSpace) {
				var location = new SourceRange(SourceFile, LT0.StartIndex);
				ErrorSink.Write(Severity.Error, location, errorMsg);
			}
		}
		public bool IsContinuator(object ltv) => ltv != null && Continuators.ContainsKey(ltv);
	
		void NewlinesOpt()
		{
			TT la0;
			// Line 67: greedy(TT.Newline)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline)
					Skip();
				else
					break;
			}
		}
	
		protected LNode CompactExpression()
		{
			TT la0;
			Token lit_comma = default(Token);
			Token lit_semi = default(Token);
			Token litx0A = default(Token);
			LNode result = default(LNode);
			// Line 74: ( TopExpr greedy(TT.Comma)? | TT.Comma | TT.Semicolon | TT.Newline )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
			case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				{
					result = TopExpr(compactMode: true);
					// Line 74: greedy(TT.Comma)?
					la0 = (TT) LA0;
					if (la0 == TT.Comma)
						lit_comma = MatchAny();
				}
				break;
			case TT.Comma:
				{
					lit_comma = MatchAny();
					// line 75
					result = F.Id(GSymbol.Empty, lit_comma);
				}
				break;
			case TT.Semicolon:
				{
					lit_semi = MatchAny();
					// line 76
					result = F.Id(S.Semicolon, lit_semi);
				}
				break;
			default:
				{
					litx0A = Match((int) TT.Newline);
					// line 77
					result = F.Id(S.Semicolon, litx0A);
				}
				break;
			}
			return result;
		}
	
		protected LNode NextExpression(ref TokenType separatorType, out Token separator, bool isBracedBlock)
		{
			TT la0;
			LNode result = default(LNode);
			// line 82
			separator = default(Token);
			// Line 84: (TopExpr / {..})
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
			case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				result = TopExpr(compactMode: false);
				break;
			default:
				// line 85
				result = F.Id(S.Missing, LT0);
				break;
			}
			ErrorTokensOpt();
			// Line 88: greedy(&{isBracedBlock} (TT.RBrack|TT.RParen))?
			la0 = (TT) LA0;
			if (la0 == TT.RBrack || la0 == TT.RParen) {
				if (isBracedBlock) {
					Skip();
					// line 88
					Error(-1, "Ignoring unexpected closing bracket");
				}
			}
			// Line 89: greedy((TT.Comma|TT.Newline|TT.Semicolon))?
			la0 = (TT) LA0;
			if (la0 == TT.Comma || la0 == TT.Newline || la0 == TT.Semicolon)
				separator = MatchAny();
			// line 91
			var curSepType = separator.Type();
			if (curSepType == TT.Newline && !isBracedBlock) {
				Error(-1, "Expected ',' or ';' here");
			} else if (curSepType != 0) {
				if (curSepType == TT.Newline)
					curSepType = TT.Semicolon;
				if (curSepType != separatorType) {
					if (separatorType == default(TT)) {
						separatorType = curSepType;
					} else if (!(separatorType == TT.Semicolon && curSepType == TT.EOF)) {
						Error(-1, "Unexpected separator: {0} should be {1}", ToString((int) curSepType), ToString((int) separatorType));
					}
				}
			}
			NewlinesOpt();
			return result;
		}
	
		public LNodeList ExprList(ref TokenType separatorType, LNode firstItem = null, bool isBracedBlock = false)
		{
			TT la0;
			LNodeList got_TokenListEx = default(LNodeList);
			// line 113
			var list = InternalList<LNode>.Empty;
			// line 114
			if (firstItem != null)
				list.Add(firstItem);
			NewlinesOpt();
			// Line 116: ( &!{isBracedBlock} &{LT($LI).EndIndex < LT($LI + 1).StartIndex} TT.Dot greedy(CompactExpression)* | TT.SingleQuote TokenListEx | (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)* )
			la0 = (TT) LA0;
			if (la0 == TT.Dot) {
				Check(!isBracedBlock, "Did not expect isBracedBlock");
				Check(LT(0).EndIndex < LT(0 + 1).StartIndex, "Expected LT($LI).EndIndex < LT($LI + 1).StartIndex");
				Skip();
				// Line 119: greedy(CompactExpression)*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Comma: case TT.Id: case TT.Keyword:
					case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
					case TT.Newline: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
					case TT.PreOrSufOp: case TT.Semicolon:
						list.Add(CompactExpression());
						break;
					default:
						goto stop;
					}
				}
			stop:;
				// line 120
				separatorType = TT.Comma;
			} else if (la0 == TT.SingleQuote) {
				Skip();
				got_TokenListEx = TokenListEx();
				// line 123
				list.AddRange(got_TokenListEx);
				// line 124
				separatorType = TT.Comma;
			} else {
				// line 125
				var separator = default(Token);
				// Line 126: (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Comma: case TT.Id: case TT.Keyword:
					case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
					case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
					case TT.Semicolon:
						list.Add(NextExpression(ref separatorType, out separator, isBracedBlock));
						break;
					default:
						goto stop2;
					}
				}
			stop2:;
				// line 130
				if (separator.Type() == TT.Comma)
					list.Add(F.Id(S.Missing, separator));
			}
			// line 134
			return LNode.List(list);
		}
	
		void ErrorTokensOpt()
		{
			LNodeList got_TokenList = default(LNodeList);
			// line 138
			int _errorPosition = InputPosition;
			got_TokenList = TokenList();
			// line 140
			if (!got_TokenList.IsEmpty)
				Error(_errorPosition - InputPosition, "Expected end of expression (',', ';', etc.)");
		}
	
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> separatorType, bool isBracedBlock = true)
		{
			TT la0;
			LNode got_CompactExpression = default(LNode);
			LNode got_NextExpression = default(LNode);
			LNodeList got_TokenListEx = default(LNodeList);
			NewlinesOpt();
			// Line 148: ( &!{isBracedBlock} &{LT($LI).EndIndex < LT($LI + 1).StartIndex} TT.Dot greedy(CompactExpression)* | TT.SingleQuote TokenListEx | (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)* )
			la0 = (TT) LA0;
			if (la0 == TT.Dot) {
				Check(!isBracedBlock, "Did not expect isBracedBlock");
				Check(LT(0).EndIndex < LT(0 + 1).StartIndex, "Expected LT($LI).EndIndex < LT($LI + 1).StartIndex");
				Skip();
				// Line 151: greedy(CompactExpression)*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Comma: case TT.Id: case TT.Keyword:
					case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
					case TT.Newline: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
					case TT.PreOrSufOp: case TT.Semicolon:
						{
							got_CompactExpression = CompactExpression();
							yield // line 152
							return got_CompactExpression;
							break;
						}
					default:
						goto stop;
					}
				}
			stop:;
			} else if (la0 == TT.SingleQuote) {
				Skip();
				got_TokenListEx = TokenListEx();
				// line 158
				foreach (var item in got_TokenListEx)
					yield return item;
			} else {
				// line 161
				var separator = default(Token);
				// Line 162: (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Comma: case TT.Id: case TT.Keyword:
					case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
					case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
					case TT.Semicolon:
						{
							got_NextExpression = NextExpression(ref separatorType.Value, out separator, isBracedBlock);
							yield // line 165
							return got_NextExpression;
							break;
						}
					default:
						goto stop2;
					}
				}
			stop2:;
				// line 170
				if (separator.Type() == TT.Comma)
					yield return F.Id(S.Missing, separator);
			}
		}
	
		protected LNode TopExpr(bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			LNode result = default(LNode);
			// line 177
			int startIndex = LT0.StartIndex;
			// line 178
			var attrs = new LNodeList();
			// Line 180: (TT.At (TT.At | Expr) greedy(~(EOF|TT.LBrace|TT.Newline) => {..})? NewlinesOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Skip();
					// line 180
					CheckForSpace(false, "Unexpected space after `@`");
					// Line 181: (TT.At | Expr)
					la0 = (TT) LA0;
					if (la0 == TT.At)
						Skip();
					else {
						var attr = Expr(Precedence.MinValue, compactMode: true);
						// line 183
						attrs.Add(attr);
					}
					// Line 185: greedy(~(EOF|TT.LBrace|TT.Newline) => {..})?
					la0 = (TT) LA0;
					if (!(la0 == (TT) EOF || la0 == TT.LBrace || la0 == TT.Newline))
						// line 185
						CheckForSpace(true, "Expected space after attribute");
					NewlinesOpt();
				} else
					break;
			}
			// Line 188: (Expr)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQId: case TT.BQOperator: case TT.Colon:
			case TT.Id: case TT.Keyword: case TT.LBrace: case TT.LBrack:
			case TT.Literal: case TT.LParen: case TT.NormalOp: case TT.Not:
			case TT.PrefixOp: case TT.PreOrSufOp:
				e = Expr(Precedence.MinValue, compactMode);
				break;
			default:
				{
					// line 193
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 194: nongreedy(~(EOF))*
					for (;;) {
						switch ((TT) LA0) {
						case EOF: case TT.Assignment: case TT.At: case TT.BQId:
						case TT.BQOperator: case TT.Colon: case TT.Comma: case TT.Id:
						case TT.Keyword: case TT.LBrace: case TT.LBrack: case TT.Literal:
						case TT.LParen: case TT.Newline: case TT.NormalOp: case TT.Not:
						case TT.PrefixOp: case TT.PreOrSufOp: case TT.RBrace: case TT.RBrack:
						case TT.RParen: case TT.Semicolon:
							goto stop;
						default:
							Skip();
							break;
						}
					}
				stop:;
					// Line 195: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => {..})
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
					case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
						TopExpr(compactMode);
						break;
					default:
						// line 195
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			// line 197
			if (!attrs.IsEmpty) {
				e = e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
			}
			// line 198
			result = e;
			return result;
		}
	
		LNode Expr(Precedence context, bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			Token lit_excl = default(Token);
			Token t = default(Token);
			// Line 213: (&!{compactMode} KeywordExpression | PrefixExpr greedy( &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(_prec.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) )*)
			la0 = (TT) LA0;
			if (la0 == TT.Keyword) {
				Check(!compactMode, "A compact list cannot directly contain a keyword expression. Surround it with parentheses.");
				e = KeywordExpression();
			} else {
				// line 216
				Precedence prec;
				e = PrefixExpr(context, compactMode);
				// Line 223: greedy( &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(_prec.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) )*
				for (;;) {
					switch ((TT) LA0) {
					case TT.LBrack: case TT.LParen:
						{
							if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
								if (context.CanParse(P.Primary))
									e = FinishPrimaryExpr(e);
								else
									goto stop;
							} else
								goto stop;
						}
						break;
					case TT.Assignment: case TT.Dot: case TT.NormalOp:
						{
							if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
								if (CanParse(context, 0, out prec))
									goto match2;
								else
									goto stop;
							} else
								goto stop;
						}
					case TT.Colon:
						{
							if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
								if (CanParse(context, 0, out prec)) {
									if ((TT) LA(0 + 1) != TT.Newline)
										goto match2;
									else
										goto stop;
								} else
									goto stop;
							} else
								goto stop;
						}
					case TT.Id:
						{
							if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
								if (CanParse(context, 0, out prec)) {
									if (!IsContinuator(LT(0).Value) && !compactMode)
										goto match2;
									else
										goto stop;
								} else
									goto stop;
							} else
								goto stop;
						}
					case TT.PreOrSufOp:
						{
							if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
								if (context.CanParse(_prec.Find(OperatorShape.Suffix, LT(0).Value))) {
									t = MatchAny();
									// line 236
									e = F.Call(_prec.ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
								} else
									goto stop;
							} else
								goto stop;
						}
						break;
					case TT.Not:
						{
							if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
								if (context.CanParse(P.Of)) {
									lit_excl = MatchAny();
									// line 241
									LNodeList args;
									// line 242
									int endIndex;
									// Line 243: (TT.LParen ExprList TT.RParen / Expr)
									la0 = (TT) LA0;
									if (la0 == TT.LParen) {
										Skip();
										args = ExprList(e);
										var c = Match((int) TT.RParen);
										// line 243
										endIndex = c.EndIndex;
									} else {
										var T = Expr(P.Of, compactMode);
										// line 244
										args = LNode.List(e, T);
										endIndex = T.Range.EndIndex;
									}
									// line 246
									e = F.Call(S.Of, args, e.Range.StartIndex, endIndex, lit_excl.StartIndex, lit_excl.EndIndex, NodeStyle.Operator);
								} else
									goto stop;
							} else
								goto stop;
						}
						break;
					default:
						goto stop;
					}
					continue;
				match2:
					{
						Token op;
						var opName = InfixOperatorName(out op, compactMode);
						var rhs = Expr(prec, compactMode);
						// line 231
						e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
					}
				}
			stop:;
			}
			// line 248
			return e;
		}
	
		Symbol InfixOperatorName(out Token op, bool compactMode)
		{
			TT la0;
			Token op2 = default(Token);
			Symbol result = default(Symbol);
			// Line 269: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | &{!IsContinuator(LT($LI).Value) && !compactMode} TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..}) (TT.Newline (TT.Newline)* / {..}) )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Dot: case TT.NormalOp:
				{
					op = MatchAny();
					// Line 269: (TT.Newline)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						else
							break;
					}
					// line 269
					result = (Symbol) op.Value;
				}
				break;
			case TT.Colon:
				{
					Check((TT) LA(0 + 1) != TT.Newline, "Expected (TT) LA($LI + 1) != TT.Newline");
					op = MatchAny();
					// line 270
					result = (Symbol) op.Value;
				}
				break;
			default:
				{
					Check(!IsContinuator(LT(0).Value) && !compactMode, "Expected !IsContinuator(LT($LI).Value) && !compactMode");
					op = Match((int) TT.Id);
					// Line 274: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..})
					do {
						la0 = (TT) LA0;
						if (la0 == TT.Assignment || la0 == TT.NormalOp) {
							if (op.EndIndex == LT0.StartIndex)
								goto match1;
							else
								goto match2;
						} else if (la0 == TT.Dot)
							goto match1;
						else
							goto match2;
					match1:
						{
							Check(op.EndIndex == LT0.StartIndex, "Expected op.EndIndex == LT0.StartIndex");
							op2 = MatchAny();
							// line 276
							result = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
						}
						break;
					match2:
						{
							// line 279
							result = GSymbol.Get("'" + op.Value.ToString());
							if ((TT) LA0 == TT.Newline)
								Error(0, "Syntax error. {0}' is used like an operator but is followed by a newline, which is not allowed unless the expression is placed in parentheses.".Localized(result));
						}
					} while (false);
					// Line 284: (TT.Newline (TT.Newline)* / {..})
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						Skip();
						// Line 284: (TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
					} else// line 286
					if (LT(-1).EndIndex == LT0.StartIndex)
						Error(0, "Syntax error. {0}' is used like an operator but is not followed by a space.".Localized(result));
				}
				break;
			}
			return result;
		}
	
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			LNodeList args = default(LNodeList);
			LNode result = default(LNode);
			// Line 295: (CallArgs | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen)
				result = CallArgs(e);
			else {
				var lb = Match((int) TT.LBrack);
				args = ExprList(e);
				var rb = Match((int) TT.RBrack);
				// line 298
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
			}
			return result;
		}
	
		LNode CallArgs(LNode target)
		{
			LNodeList args = default(LNodeList);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// line 302
			var endMarker = default(TokenType);
			Skip();
			args = ExprList(ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 305
			result = MarkCall(F.Call(target, args, target.Range.StartIndex, lit_rpar.EndIndex).SetBaseStyle(NodeStyle.PrefixNotation));
			if (endMarker == TT.Semicolon) {
				result.Style |= NodeStyle.Alternate;
			}
			return result;
		}
	
		LNode PrefixExpr(Precedence context, bool compactMode)
		{
			LNode e = default(LNode);
			Token op = default(Token);
			LNode result = default(LNode);
			// Line 311: (&{!compactMode || LT(0).EndIndex == LT(1).StartIndex} (TT.Assignment|TT.BQOperator|TT.Colon|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) NewlinesOpt Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQOperator: case TT.Colon: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				{
					Check(!compactMode || LT(0).EndIndex == LT(1).StartIndex, "Expected !compactMode || LT(0).EndIndex == LT(1).StartIndex");
					op = MatchAny();
					NewlinesOpt();
					e = Expr(PrefixPrecedenceOf(op), compactMode);
					// line 315
					result = F.Call(op, e, op.StartIndex, e.Range.EndIndex, NodeStyle.Operator);
				}
				break;
			default:
				result = Particle(compactMode);
				break;
			}
			return result;
		}
	
		LNode Particle(bool compactMode = false)
		{
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// Line 326: ( (TT.BQId|TT.Id) | TT.Literal | BracedBlock | SquareBracketList | TT.LParen ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.BQId: case TT.Id:
				{
					var id = MatchAny();
					// line 327
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 329
					result = F.Literal(lit);
				}
				break;
			case TT.LBrace:
				result = BracedBlock();
				break;
			case TT.LBrack:
				result = SquareBracketList();
				break;
			case TT.LParen:
				{
					// line 339
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 340
					bool saveParens = !compactMode && (TT) LA0 != TT.At;
					var list = ExprList(ref endMarker);
					lit_rpar = Match((int) TT.RParen);
					// line 343
					if (endMarker != default(TT) || list.Count != 1) {
						result = F.Call(S.Tuple, list, lit_lpar.StartIndex, lit_rpar.EndIndex, lit_lpar.StartIndex, lit_lpar.EndIndex);
					} else {
						result = saveParens ? F.InParens(list[0], lit_lpar.StartIndex, lit_rpar.EndIndex) : list[0];
					}
					;
				}
				break;
			default:
				{
					// line 350
					result = MissingExpr(LT0, "Expected a particle (id, literal, {braces} or (parens)).");
				}
				break;
			}
			return result;
		}
	
		LNode SquareBracketList()
		{
			Token lit_lsqb = default(Token);
			Token lit_rsqb = default(Token);
			LNode result = default(LNode);
			lit_lsqb = MatchAny();
			var list = ExprList();
			lit_rsqb = Match((int) TT.RBrack);
			// line 355
			result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex).SetStyle(NodeStyle.Expression);
			return result;
		}
		static readonly HashSet<int> TokenList_set0 = NewSet((int) EOF, (int) TT.Comma, (int) TT.Newline, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon);
	
		new LNodeList TokenList()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			// line 359
			result = LNode.List();
			// Line 360: greedy(TokenListParticle)*
			for (;;) {
				la0 = (TT) LA0;
				if (!TokenList_set0.Contains((int) la0))
					result.Add(TokenListParticle());
				else
					break;
			}
			return result;
		}
	
		LNodeList TokenListEx()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			Token t = default(Token);
			// Line 363: ((TT.Comma|TT.Semicolon) | TokenListParticle)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					t = MatchAny();
					// line 363
					result.Add(F.Id(t));
				} else if (!TokenList_set0.Contains((int) la0))
					result.Add(TokenListParticle());
				else
					break;
			}
			return result;
		}
	
		LNode TokenListParticle()
		{
			TT la0;
			LNode got_BracedBlock = default(LNode);
			LNode got_SquareBracketList = default(LNode);
			LNodeList got_TokenListEx = default(LNodeList);
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			// Line 368: ( TT.LParen TokenListEx TT.RParen / SquareBracketList / BracedBlock / TT.Literal / ~(EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				got_TokenListEx = TokenListEx();
				lit_rpar = Match((int) TT.RParen);
				// line 369
				return F.Call(sy__apos_lpar_rpar, got_TokenListEx, lit_lpar.StartIndex, lit_rpar.EndIndex);
			} else if (la0 == TT.LBrack) {
				got_SquareBracketList = SquareBracketList();
				// line 370
				return got_SquareBracketList;
			} else if (la0 == TT.LBrace) {
				got_BracedBlock = BracedBlock();
				// line 371
				return got_BracedBlock;
			} else if (la0 == TT.Literal) {
				var t = MatchAny();
				// line 372
				return F.Literal(t);
			} else {
				var t = MatchExcept(TokenList_set0);
				// line 373
				return F.Id(t);
			}
		}
	
		LNode KeywordExpression()
		{
			TT la0, la1;
			Token kw = default(Token);
			LNode result = default(LNode);
			// line 379
			var args = new LNodeList();
			kw = MatchAny();
			// line 380
			var keyword = kw.Value as Symbol;
			// Line 382: ((EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) =>  / Expr)
			switch ((TT) LA0) {
			case EOF: case TT.Newline: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				args.Add(Expr(Precedence.MinValue, compactMode: false));
				break;
			}
			// Line 384: greedy((TT.Newline)? BracedBlock)?
			do {
				la0 = (TT) LA0;
				if (la0 == TT.Newline) {
					la1 = (TT) LA(1);
					if (la1 == TT.LBrace)
						goto matchBracedBlock;
				} else if (la0 == TT.LBrace)
					goto matchBracedBlock;
				break;
			matchBracedBlock:
				{
					// Line 384: (TT.Newline)?
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					args.Add(BracedBlock());
				}
			} while (false);
			// Line 385: greedy(Continuator)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline) {
					la1 = (TT) LA(1);
					if (la1 == TT.Id) {
						if (IsContinuator(LT(1).Value))
							args.Add(Continuator());
						else
							break;
					} else
						break;
				} else if (la0 == TT.Id) {
					if (IsContinuator(LT(0).Value))
						args.Add(Continuator());
					else
						break;
				} else
					break;
			}
			// line 387
			int endIndex = args.IsEmpty ? kw.EndIndex : args.Last.Range.EndIndex;
			result = MarkSpecial(F.Call(keyword, args, kw.StartIndex, endIndex, kw.StartIndex, kw.EndIndex));
			return result;
		}
	
		LNode Continuator()
		{
			TT la0, la1;
			LNode bb = default(LNode);
			LNode e = default(LNode);
			Token kw = default(Token);
			LNode result = default(LNode);
			// Line 393: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			kw = ContinuatorKeyword();
			// line 394
			var opName = Continuators[kw.Value];
			// Line 396: greedy(TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			// Line 397: (BracedBlock / TopExpr (greedy(TT.Newline)? BracedBlock / {..}))
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				bb = BracedBlock();
				// line 397
				result = F.Call(opName, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			} else {
				e = TopExpr(compactMode: false);
				// Line 399: (greedy(TT.Newline)? BracedBlock / {..})
				do {
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto matchBracedBlock;
						else
							// line 401
							result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					} else if (la0 == TT.LBrace)
						goto matchBracedBlock;
					else
						// line 401
						result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					break;
				matchBracedBlock:
					{
						// Line 399: greedy(TT.Newline)?
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						bb = BracedBlock();
						// line 400
						result = F.Call(opName, e, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					}
				} while (false);
			}
			return result;
		}
	
		LNode BracedBlock()
		{
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			LNodeList stmts = default(LNodeList);
			lit_lcub = Match((int) TT.LBrace);
			var endMarker = default(TT);
			stmts = ExprList(ref endMarker, isBracedBlock: true);
			lit_rcub = Match((int) TT.RBrace);
			// line 410
			return F.Call(S.Braces, stmts, lit_lcub.StartIndex, lit_rcub.EndIndex, lit_lcub.StartIndex, lit_lcub.EndIndex).SetStyle(NodeStyle.StatementBlock);
		}
	
		Token ContinuatorKeyword()
		{
			Token result = default(Token);
			Check(IsContinuator(LT(0).Value), "Expected IsContinuator(LT($LI).Value)");
			result = Match((int) TT.Id);
			return result;
		}
	}
}	// braces around the rest of the file are optional