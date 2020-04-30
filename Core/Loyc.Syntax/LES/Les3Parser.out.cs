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
		static readonly Symbol sy__aposx0A = (Symbol) "'\n", sy__apos_lpar_rpar = (Symbol) "'()";
		#pragma warning disable 162, 642
	
		protected new const TT EOF = TT.EOF;
	
		Dictionary<UString, LNode> _sharedTrees;
		void MissingEndMarker(LNode previousExpr, TokenType endMarker) {
			var location = new SourceRange(SourceFile, LT(-1).EndIndex + 1);
			ErrorSink.Write(Severity.Error, location, "Expected '{0}'", endMarker == TT.Comma ? ',' : ';');
		}
		public LNodeList ExprList(LNode firstItem = null) {
			var endMarker = default(TT);
			return ExprList(ref endMarker, firstItem, isBracedBlock: false);
		}
		bool IsSpaceBefore(int li) {
			return LT(li).StartIndex > LT(li - 1).EndIndex;
		}
		void CheckForSpace(bool expectSpace, string errorMsg) {
			if ((LT0.StartIndex == LT(-1).EndIndex) == expectSpace) {
				var location = new SourceRange(SourceFile, LT0.StartIndex);
				ErrorSink.Write(Severity.Error, location, errorMsg);
			}
		}
		bool IsContinuator(object ltv) => ltv != null && Continuators.ContainsKey(ltv);
	
		void NewlinesOpt()
		{
			TT la0;
			// Line 69: greedy(TT.Newline)*
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
			// Line 76: ( TopExpr greedy(TT.Comma)? | TT.Comma | TT.Semicolon | TT.Newline )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.BQOperator: case TT.Colon: case TT.Id: case TT.Keyword:
			case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
			case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
			case TT.TreeDef:
				{
					result = TopExpr(compactMode: true);
					// Line 76: greedy(TT.Comma)?
					la0 = (TT) LA0;
					if (la0 == TT.Comma)
						lit_comma = MatchAny();
				}
				break;
			case TT.Comma:
				{
					lit_comma = MatchAny();
					// line 77
					result = F.Id(GSymbol.Empty, lit_comma);
				}
				break;
			case TT.Semicolon:
				{
					lit_semi = MatchAny();
					// line 78
					result = F.Id(S.Semicolon, lit_semi);
				}
				break;
			default:
				{
					litx0A = Match((int) TT.Newline);
					// line 79
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
			// line 84
			separator = default(Token);
			// Line 85: (TopExpr / {..})
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.BQOperator: case TT.Colon: case TT.Id: case TT.Keyword:
			case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
			case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
			case TT.TreeDef:
				result = TopExpr(compactMode: false);
				break;
			default:
				// line 86
				result = F.Id(S.Missing, LT0);
				break;
			}
			ErrorTokensOpt();
			// Line 89: greedy(&{isBracedBlock} (TT.RBrack|TT.RParen))?
			la0 = (TT) LA0;
			if (la0 == TT.RBrack || la0 == TT.RParen) {
				if (isBracedBlock) {
					Skip();
					// line 89
					Error(-1, "Ignoring unexpected closing bracket");
				}
			}
			// Line 90: greedy((TT.Comma|TT.Newline|TT.Semicolon))?
			la0 = (TT) LA0;
			if (la0 == TT.Comma || la0 == TT.Newline || la0 == TT.Semicolon)
				separator = MatchAny();
			// line 92
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
			LNodeList got_TokenListEx = default(LNodeList);
			// line 114
			var list = InternalList<LNode>.Empty;
			// line 115
			if (firstItem != null)
				list.Add(firstItem);
			NewlinesOpt();
			// Line 117: ( &!{isBracedBlock} &{LT($LI).EndIndex < LT($LI + 1).StartIndex} TT.Dot greedy(CompactExpression)* | TT.SingleQuote TokenListEx | (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)* )
			switch ((TT) LA0) {
			case TT.Dot:
				{
					Check(!isBracedBlock, "Did not expect isBracedBlock");
					Check(LT(0).EndIndex < LT(0 + 1).StartIndex, "Expected LT($LI).EndIndex < LT($LI + 1).StartIndex");
					Skip();
					// Line 120: greedy(CompactExpression)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
						case TT.BQOperator: case TT.Colon: case TT.Comma: case TT.Id:
						case TT.Keyword: case TT.LBrace: case TT.LBrack: case TT.Literal:
						case TT.LParen: case TT.Newline: case TT.NormalOp: case TT.Not:
						case TT.PrefixOp: case TT.PreOrSufOp: case TT.Semicolon: case TT.TreeDef:
							list.Add(CompactExpression());
							break;
						default:
							goto stop;
						}
					}
				stop:;
					// line 121
					separatorType = TT.Comma;
				}
				break;
			case TT.SingleQuote:
				{
					Skip();
					got_TokenListEx = TokenListEx();
					// line 124
					list.AddRange(got_TokenListEx);
					// line 125
					separatorType = TT.Comma;
				}
				break;
			case EOF: case TT.Assignment: case TT.At: case TT.BackRef:
			case TT.BQId: case TT.BQOperator: case TT.Colon: case TT.Comma:
			case TT.Id: case TT.Keyword: case TT.LBrace: case TT.LBrack:
			case TT.Literal: case TT.LParen: case TT.NormalOp: case TT.Not:
			case TT.PrefixOp: case TT.PreOrSufOp: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon: case TT.TreeDef:
				{
					// line 126
					var separator = default(Token);
					// Line 127: (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
						case TT.BQOperator: case TT.Colon: case TT.Comma: case TT.Id:
						case TT.Keyword: case TT.LBrace: case TT.LBrack: case TT.Literal:
						case TT.LParen: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
						case TT.PreOrSufOp: case TT.Semicolon: case TT.TreeDef:
							list.Add(NextExpression(ref separatorType, out separator, isBracedBlock));
							break;
						default:
							goto stop2;
						}
					}
				stop2:;
					// line 131
					if (separator.Type() == TT.Comma)
						list.Add(F.Id(S.Missing, separator));
				}
				break;
			default:
				{
					// line 134
					Error(0, "Expected an expression here");
					got_TokenListEx = TokenListEx();
				}
				break;
			}
			// line 136
			return LNode.List(list);
		}
	
		void ErrorTokensOpt()
		{
			LNodeList got_TokenList = default(LNodeList);
			// line 140
			int _errorPosition = InputPosition;
			got_TokenList = TokenList();
			// line 142
			if (!got_TokenList.IsEmpty)
				Error(_errorPosition - InputPosition, "Expected end of expression (',', ';', etc.)");
		}
	
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> separatorType)
		{
			TT la0;
			LNode got_NextExpression = default(LNode);
			LNodeList got_TokenListEx = default(LNodeList);
			// line 149
			bool isBracedBlock = true;
			NewlinesOpt();
			// Line 151: (TT.Dot)?
			la0 = (TT) LA0;
			if (la0 == TT.Dot) {
				Skip();
				// line 151
				Error(0, "Expected a statement here");
			}
			// Line 152: (TT.SingleQuote TokenListEx | (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)*)
			la0 = (TT) LA0;
			if (la0 == TT.SingleQuote) {
				Skip();
				got_TokenListEx = TokenListEx();
				// line 155
				foreach (var item in got_TokenListEx)
					yield return item;
			} else {
				// line 158
				var separator = default(Token);
				// Line 159: (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
					case TT.BQOperator: case TT.Colon: case TT.Comma: case TT.Id:
					case TT.Keyword: case TT.LBrace: case TT.LBrack: case TT.Literal:
					case TT.LParen: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
					case TT.PreOrSufOp: case TT.Semicolon: case TT.TreeDef:
						{
							got_NextExpression = NextExpression(ref separatorType.Value, out separator, isBracedBlock);
							yield // line 162
							return got_NextExpression;
							break;
						}
					default:
						goto stop;
					}
				}
			stop:;
				// line 167
				if (separator.Type() == TT.Comma)
					yield return F.Id(S.Missing, separator);
			}
		}
		static readonly HashSet<int> TopExpr_set0 = NewSet((int) EOF, (int) TT.Assignment, (int) TT.At, (int) TT.BackRef, (int) TT.BQId, (int) TT.BQOperator, (int) TT.Colon, (int) TT.Comma, (int) TT.Id, (int) TT.Keyword, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Newline, (int) TT.NormalOp, (int) TT.Not, (int) TT.PrefixOp, (int) TT.PreOrSufOp, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.TreeDef);
	
		protected LNode TopExpr(bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			Token treeDef = default(Token);
			// Line 174: (TT.TreeDef)?
			la0 = (TT) LA0;
			if (la0 == TT.TreeDef)
				treeDef = MatchAny();
			// Line 175: (Expr)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.BQOperator: case TT.Colon: case TT.Id: case TT.Keyword:
			case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
			case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				e = Expr(Precedence.MinValue, compactMode);
				break;
			default:
				{
					// line 180
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 181: nongreedy(~(EOF))*
					for (;;) {
						la0 = (TT) LA0;
						if (TopExpr_set0.Contains((int) la0))
							break;
						else
							Skip();
					}
					// Line 182: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => {..})
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
					case TT.BQOperator: case TT.Colon: case TT.Id: case TT.Keyword:
					case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
					case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
					case TT.TreeDef:
						TopExpr(compactMode);
						break;
					default:
						// line 182
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			// line 185
			if (treeDef.Type() == TT.TreeDef) {
				UString treeId = treeDef.Value?.ToString() ?? treeDef.SourceText(SourceFile.Text).Slice(2);
				_sharedTrees = _sharedTrees ?? new Dictionary<UString, LNode>();
				try {
					_sharedTrees.Add(treeId, e);
				} catch (Exception) {
					ErrorSink.Error(treeDef.Range(SourceFile), "'@.{0}' was already defined at {1}", treeId, _sharedTrees[treeId].Range.Start);
				}
			}
			return e;
		}
	
		LNode Expr(Precedence context, bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			Token lit_excl = default(Token);
			Token t = default(Token);
			// line 211
			int startIndex = LT0.StartIndex;
			// line 212
			var attrs = LNode.List();
			// Line 214: (&!{compactMode && context != Precedence.MinValue} TT.At (TT.At / &{LT($LI - 1).EndIndex == LT($LI).StartIndex} Expr)? greedy(~(EOF|TT.LBrace|TT.Newline) => {..})? NewlinesOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Check(!(compactMode && context != Precedence.MinValue), "An attribute cannot appear mid-expression in a compact expression list.");
					Skip();
					// Line 216: (TT.At / &{LT($LI - 1).EndIndex == LT($LI).StartIndex} Expr)?
					switch ((TT) LA0) {
					case TT.At:
						Skip();
						break;
					case TT.Assignment: case TT.BackRef: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
					case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
						{
							if (LT(0 - 1).EndIndex == LT(0).StartIndex)
								attrs.Add(Expr(Precedence.MinValue, compactMode: true));
						}
						break;
					}
					// Line 220: greedy(~(EOF|TT.LBrace|TT.Newline) => {..})?
					la0 = (TT) LA0;
					if (!(la0 == (TT) EOF || la0 == TT.LBrace || la0 == TT.Newline))
						// line 220
						CheckForSpace(true, "Expected space after attribute");
					NewlinesOpt();
				} else
					break;
			}
			// Line 223: (&!{compactMode} KeywordExpression | PrefixExpr greedy( &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(_prec.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Primary)} TT.Keyword )*)
			la0 = (TT) LA0;
			if (la0 == TT.Keyword) {
				Check(!compactMode, "A compact list cannot directly contain a keyword expression. Surround it with parentheses.");
				e = KeywordExpression();
			} else {
				// line 226
				Precedence prec;
				e = PrefixExpr(context, compactMode);
				// Line 233: greedy( &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(_prec.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || LT($LI - 1).EndIndex == LT($LI).StartIndex} &{context.CanParse(P.Primary)} TT.Keyword )*
				for (;;) {
					switch ((TT) LA0) {
					case TT.LBrack: case TT.LParen:
						{
							if (context.CanParse(P.Primary)) {
								if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex)
									e = FinishPrimaryExpr(e);
								else
									goto stop;
							} else
								goto stop;
						}
						break;
					case TT.Assignment: case TT.Dot: case TT.NormalOp:
						{
							if (CanParse(context, 0, out prec)) {
								if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex)
									goto match2;
								else
									goto stop;
							} else
								goto stop;
						}
					case TT.Colon:
						{
							if (CanParse(context, 0, out prec)) {
								if ((TT) LA(0 + 1) != TT.Newline) {
									if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex)
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
							if (CanParse(context, 0, out prec)) {
								if (!IsContinuator(LT(0).Value) && !compactMode) {
									if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex)
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
							if (context.CanParse(_prec.Find(OperatorShape.Suffix, LT(0).Value))) {
								if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
									t = MatchAny();
									// line 246
									e = F.Call(_prec.ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
								} else
									goto stop;
							} else
								goto stop;
						}
						break;
					case TT.Not:
						{
							if (context.CanParse(P.Of)) {
								if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
									lit_excl = MatchAny();
									// line 251
									LNodeList args;
									// line 252
									int endIndex;
									// Line 253: (TT.LParen ExprList TT.RParen / Expr)
									la0 = (TT) LA0;
									if (la0 == TT.LParen) {
										Skip();
										args = ExprList(e);
										var c = Match((int) TT.RParen);
										// line 253
										endIndex = c.EndIndex;
									} else {
										var T = Expr(P.Of, compactMode);
										// line 254
										args = LNode.List(e, T);
										endIndex = T.Range.EndIndex;
									}
									// line 256
									e = F.Call(S.Of, args, e.Range.StartIndex, endIndex, lit_excl.StartIndex, lit_excl.EndIndex, NodeStyle.Operator);
								} else
									goto stop;
							} else
								goto stop;
						}
						break;
					case TT.Keyword:
						{
							if (context.CanParse(P.Primary)) {
								if (!compactMode || LT(0 - 1).EndIndex == LT(0).StartIndex) {
									var kw = MatchAny();
									// line 262
									var id = F.Id(kw.Value.ToString().Substring(1), kw.StartIndex + 1, kw.EndIndex);
									e = F.Dot(e, id, e.Range.StartIndex, kw.EndIndex);
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
						// line 241
						e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
					}
				}
			stop:;
			}
			// line 267
			return attrs.IsEmpty ? e : e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
		}
	
		Symbol InfixOperatorName(out Token op, bool compactMode)
		{
			TT la0;
			Token op2 = default(Token);
			Symbol result = default(Symbol);
			// Line 288: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | &{!IsContinuator(LT($LI).Value) && !compactMode} TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..}) (TT.Newline (TT.Newline)* / {..}) )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Dot: case TT.NormalOp:
				{
					op = MatchAny();
					// Line 288: (TT.Newline)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						else
							break;
					}
					// line 288
					result = (Symbol) op.Value;
				}
				break;
			case TT.Colon:
				{
					Check((TT) LA(0 + 1) != TT.Newline, "Expected (TT) LA($LI + 1) != TT.Newline");
					op = MatchAny();
					// line 289
					result = (Symbol) op.Value;
				}
				break;
			default:
				{
					Check(!IsContinuator(LT(0).Value) && !compactMode, "Expected !IsContinuator(LT($LI).Value) && !compactMode");
					op = Match((int) TT.Id);
					// Line 293: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..})
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
							// line 295
							result = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
						}
						break;
					match2:
						{
							// line 298
							result = GSymbol.Get("'" + op.Value.ToString());
							if ((TT) LA0 == TT.Newline)
								Error(0, "Syntax error. {0}' is used like an operator but is followed by a newline, which is not allowed unless the expression is placed in parentheses.".Localized(result));
						}
					} while (false);
					// Line 303: (TT.Newline (TT.Newline)* / {..})
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						Skip();
						// Line 303: (TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
					} else// line 305
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
			// Line 314: (CallArgs | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen)
				result = CallArgs(e);
			else {
				var lb = Match((int) TT.LBrack);
				args = ExprList(e);
				var rb = Match((int) TT.RBrack);
				// line 317
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
			}
			return result;
		}
	
		LNode CallArgs(LNode target)
		{
			LNodeList args = default(LNodeList);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// line 321
			var endMarker = default(TokenType);
			Skip();
			args = ExprList(ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 324
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
			// Line 330: (&{!compactMode || LT(0).EndIndex == LT(1).StartIndex} (TT.Assignment|TT.BQOperator|TT.Colon|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) NewlinesOpt Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQOperator: case TT.Colon: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				{
					Check(!compactMode || LT(0).EndIndex == LT(1).StartIndex, "Expected !compactMode || LT(0).EndIndex == LT(1).StartIndex");
					op = MatchAny();
					NewlinesOpt();
					e = Expr(PrefixPrecedenceOf(op), compactMode);
					// line 334
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
			// Line 346: ( (TT.BQId|TT.Id) | TT.Literal | BracedBlock | SquareBracketList | TT.LParen ExprList TT.RParen | TT.BackRef )
			switch ((TT) LA0) {
			case TT.BQId: case TT.Id:
				{
					var id = MatchAny();
					// line 347
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 349
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
					// line 359
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 360
					bool saveParens = !compactMode && (TT) LA0 != TT.At;
					var list = ExprList(ref endMarker);
					lit_rpar = Match((int) TT.RParen);
					// line 363
					if (endMarker != default(TT) || list.Count != 1) {
						result = F.Call(S.Tuple, list, lit_lpar.StartIndex, lit_rpar.EndIndex, lit_lpar.StartIndex, lit_lpar.EndIndex);
					} else {
						result = saveParens ? F.InParens(list[0], lit_lpar.StartIndex, lit_rpar.EndIndex) : list[0];
					}
					;
				}
				break;
			case TT.BackRef:
				{
					var backRef = MatchAny();
					// line 370
					UString treeId = backRef.Value?.ToString() ?? backRef.SourceText(SourceFile.Text).Slice(2);
					LNode tree = null;
					if (_sharedTrees?.TryGetValue(treeId, out tree) == true)
						result = tree;
					else {
						result = MissingExpr(backRef);
						Error(-1, "There is no previous definition for '@.{0}'", treeId);
					}
				}
				break;
			default:
				{
					// line 380
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
			// line 385
			result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex).SetStyle(NodeStyle.Expression);
			return result;
		}
		static readonly HashSet<int> TokenList_set0 = NewSet((int) EOF, (int) TT.Comma, (int) TT.Newline, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon);
	
		new LNodeList TokenList()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			// line 389
			result = LNode.List();
			// Line 390: greedy(TokenListParticle)*
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
			// Line 393: ( (TT.Comma|TT.Semicolon) | TT.Newline | TokenListParticle )*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					var t = MatchAny();
					// line 393
					result.Add(F.Id(t));
				} else if (la0 == TT.Newline) {
					var t = MatchAny();
					// line 394
					result.Add(F.Id(sy__aposx0A, t));
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
			// Line 399: ( TT.LParen TokenListEx TT.RParen / SquareBracketList / BracedBlock / TT.Literal / ~(EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				got_TokenListEx = TokenListEx();
				lit_rpar = Match((int) TT.RParen);
				// line 400
				return F.Call(sy__apos_lpar_rpar, got_TokenListEx, lit_lpar.StartIndex, lit_rpar.EndIndex);
			} else if (la0 == TT.LBrack) {
				got_SquareBracketList = SquareBracketList();
				// line 401
				return got_SquareBracketList;
			} else if (la0 == TT.LBrace) {
				got_BracedBlock = BracedBlock();
				// line 402
				return got_BracedBlock;
			} else if (la0 == TT.Literal) {
				var t = MatchAny();
				// line 403
				return F.Literal(t);
			} else {
				var t = MatchExcept(TokenList_set0);
				// line 404
				return F.Id(t);
			}
		}
	
		LNode KeywordExpression()
		{
			TT la0, la1;
			Token kw = default(Token);
			LNode result = default(LNode);
			// line 410
			var args = new LNodeList();
			kw = MatchAny();
			// line 411
			var keyword = kw.Value as Symbol;
			// Line 413: ((EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) =>  / Expr)
			switch ((TT) LA0) {
			case EOF: case TT.Newline: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				args.Add(Expr(Precedence.MinValue, compactMode: false));
				break;
			}
			// Line 415: greedy((TT.Newline)? BracedBlock)?
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
					// Line 415: (TT.Newline)?
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					args.Add(BracedBlock());
				}
			} while (false);
			// Line 416: greedy(Continuator)*
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
			// line 418
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
			// Line 424: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			kw = ContinuatorKeyword();
			// line 425
			var opName = Continuators[kw.Value];
			// Line 427: greedy(TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			// Line 428: (BracedBlock / TopExpr (greedy(TT.Newline)? BracedBlock / {..}))
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				bb = BracedBlock();
				// line 428
				result = F.Call(opName, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			} else {
				e = TopExpr(compactMode: false);
				// Line 430: (greedy(TT.Newline)? BracedBlock / {..})
				do {
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto matchBracedBlock;
						else
							// line 432
							result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					} else if (la0 == TT.LBrace)
						goto matchBracedBlock;
					else
						// line 432
						result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					break;
				matchBracedBlock:
					{
						// Line 430: greedy(TT.Newline)?
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						bb = BracedBlock();
						// line 431
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
			// line 441
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