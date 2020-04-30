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
using System.Runtime.CompilerServices;
using Loyc;	// for IMessageSink, Symbol, etc.
using Loyc.Collections;	// many handy interfaces & classes
using Loyc.Collections.Impl;	// For InternalList
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
		void CheckForSpace(bool expectSpace, string errorMsg) {
			if ((LT0.StartIndex == LT(-1).EndIndex) == expectSpace) {
				var location = new SourceRange(SourceFile, LT0.StartIndex);
				ErrorSink.Write(Severity.Error, location, errorMsg);
			}
		}
		bool IsContinuator(object ltv) => ltv != null && Continuators.ContainsKey(ltv);
		[MethodImpl(MethodImplOptions.AggressiveInlining)] bool IsConjoinedToken(int li) => LT(li - 1).EndIndex == LT(li).StartIndex;
	
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
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
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
			// line 83
			separator = default(Token);
			// Line 84: (TopExpr / {..})
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
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
			LNodeList got_TokenListEx = default(LNodeList);
			// line 113
			var list = InternalList<LNode>.Empty;
			// line 114
			if (firstItem != null)
				list.Add(firstItem);
			NewlinesOpt();
			// Line 116: ( &!{isBracedBlock} &!{IsConjoinedToken($LI + 1)} TT.Dot greedy(CompactExpression)* / TT.SingleQuote TokenListEx / (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)* )
			do {
				switch ((TT) LA0) {
				case TT.Dot:
					{
						if (!isBracedBlock) {
							if (!IsConjoinedToken(0 + 1)) {
								Skip();
								// Line 119: greedy(CompactExpression)*
								for (;;) {
									switch ((TT) LA0) {
									case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
									case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
									case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
									case TT.Newline: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
									case TT.PreOrSufOp: case TT.Semicolon: case TT.TreeDef:
										list.Add(CompactExpression());
										break;
									default:
										goto stop;
									}
								}
							stop:;
								// line 120
								separatorType = TT.Comma;
							} else
								goto match3;
						} else
							goto match3;
					}
					break;
				case TT.SingleQuote:
					{
						Skip();
						got_TokenListEx = TokenListEx();
						// line 123
						list.AddRange(got_TokenListEx);
						// line 124
						separatorType = TT.Comma;
					}
					break;
				case EOF: case TT.Assignment: case TT.At: case TT.BackRef:
				case TT.BQId: case TT.Colon: case TT.Comma: case TT.Id:
				case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
				case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				case TT.RBrace: case TT.RBrack: case TT.RParen: case TT.Semicolon:
				case TT.TreeDef:
					goto match3;
				default:
					{
						// line 133
						Error(0, "Expected an expression here");
						got_TokenListEx = TokenListEx();
					}
					break;
				}
				break;
			match3:
				{
					// line 125
					var separator = default(Token);
					// Line 126: (((TT.Comma|TT.Semicolon) | TopExpr) => NextExpression)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
						case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
						case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
						case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
						case TT.Semicolon: case TT.TreeDef:
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
			} while (false);
			// line 135
			return LNode.List(list);
		}
	
		void ErrorTokensOpt()
		{
			LNodeList got_TokenList = default(LNodeList);
			// line 139
			int _errorPosition = InputPosition;
			got_TokenList = TokenList();
			// line 141
			if (!got_TokenList.IsEmpty)
				Error(_errorPosition - InputPosition, "Expected end of expression (',', ';', etc.)");
		}
	
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> separatorType)
		{
			TT la0;
			LNode got_NextExpression = default(LNode);
			LNodeList got_TokenListEx = default(LNodeList);
			// line 148
			bool isBracedBlock = true;
			NewlinesOpt();
			// Line 151: greedy(&!{IsConjoinedToken($LI + 1)} TT.Dot)?
			la0 = (TT) LA0;
			if (la0 == TT.Dot) {
				if (!IsConjoinedToken(0 + 1)) {
					Skip();
					// line 152
					Error(0, "Expected a statement here");
				}
			}
			// Line 154: (TT.SingleQuote TokenListEx | (NextExpression)*)
			la0 = (TT) LA0;
			if (la0 == TT.SingleQuote) {
				Skip();
				got_TokenListEx = TokenListEx();
				// line 157
				foreach (var item in got_TokenListEx)
					yield return item;
			} else {
				// line 160
				var separator = default(Token);
				// Line 162: (NextExpression)*
				for (;;) {
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
					case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
					case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
					case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
					case TT.Semicolon: case TT.TreeDef:
						{
							got_NextExpression = NextExpression(ref separatorType.Value, out separator, isBracedBlock);
							yield // line 164
							return got_NextExpression;
							break;
						}
					default:
						goto stop;
					}
				}
			stop:;
				// line 169
				if (separator.Type() == TT.Comma)
					yield return F.Id(S.Missing, separator);
			}
		}
		static readonly HashSet<int> TopExpr_set0 = NewSet((int) EOF, (int) TT.Assignment, (int) TT.At, (int) TT.BackRef, (int) TT.BQId, (int) TT.Colon, (int) TT.Comma, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Newline, (int) TT.NormalOp, (int) TT.Not, (int) TT.PrefixOp, (int) TT.PreOrSufOp, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.TreeDef);
	
		protected LNode TopExpr(bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			Token treeDef = default(Token);
			// Line 176: (TT.TreeDef)?
			la0 = (TT) LA0;
			if (la0 == TT.TreeDef)
				treeDef = MatchAny();
			// Line 177: (Expr)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				e = Expr(Precedence.MinValue, compactMode);
				break;
			default:
				{
					// line 182
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 183: nongreedy(~(EOF))*
					for (;;) {
						la0 = (TT) LA0;
						if (TopExpr_set0.Contains((int) la0))
							break;
						else
							Skip();
					}
					// Line 184: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => {..})
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
					case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
					case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
						e = TopExpr(compactMode);
						break;
					default:
						// line 185
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			// line 189
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
			// line 215
			int startIndex = LT0.StartIndex;
			// line 216
			var attrs = LNode.List();
			// Line 217: (TT.At &!{compactMode && context != Precedence.MinValue} (&{IsConjoinedToken($LI)} TT.At / &{IsConjoinedToken($LI)} => Expr)?  => NewlinesOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Skip();
					Check(!(compactMode && context != Precedence.MinValue), "An attribute cannot appear mid-expression in a compact expression list.");
					// Line 223: (&{IsConjoinedToken($LI)} TT.At / &{IsConjoinedToken($LI)} => Expr)?
					la0 = (TT) LA0;
					if (la0 == TT.At) {
						if (IsConjoinedToken(0))
							Skip();
					} else {
						if (IsConjoinedToken(0))
							attrs.Add(Expr(Precedence.MinValue, compactMode: true));
					}
					NewlinesOpt();
				} else
					break;
			}
			// Line 230: (&{(TT) LA(1) == TT.Id && !compactMode} KeywordExpression / (TT.Dot | PrefixExpr) => PrefixExpr greedy( &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || IsConjoinedToken($LI)} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(_precMap.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.SuffixWord)} TT.BQId | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} TT.Keyword )*)
			do {
				la0 = (TT) LA0;
				if (la0 == TT.Dot) {
					if ((TT) LA(1) == TT.Id && !compactMode)
						e = KeywordExpression();
					else
						goto match2_a;
				} else
					goto match2_a;
				break;
			match2_a:
				{
					// line 234
					Precedence prec;
					e = PrefixExpr(context, compactMode);
					// Line 241: greedy( &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || IsConjoinedToken($LI)} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(_precMap.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.SuffixWord)} TT.BQId | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} TT.Keyword )*
					for (;;) {
						switch ((TT) LA0) {
						case TT.LBrack: case TT.LParen:
							{
								if (!compactMode || IsConjoinedToken(0)) {
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
								if (!compactMode || IsConjoinedToken(0)) {
									if (CanParse(context, 0, out prec))
										goto match2;
									else
										goto stop;
								} else
									goto stop;
							}
						case TT.Colon:
							{
								if (!compactMode || IsConjoinedToken(0)) {
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
								if (!compactMode || IsConjoinedToken(0)) {
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
								if (context.CanParse(_precMap.Find(OperatorShape.Suffix, LT(0).Value))) {
									if (!compactMode || IsConjoinedToken(0)) {
										var t = MatchAny();
										// line 254
										e = F.Call(_precMap.ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
									} else
										goto stop;
								} else
									goto stop;
							}
							break;
						case TT.BQId:
							{
								if (!compactMode || IsConjoinedToken(0)) {
									if (context.CanParse(P.SuffixWord)) {
										var unit = MatchAny();
										// line 259
										e = F.Call(S.IS, e, F.Id(unit), e.Range.StartIndex, unit.EndIndex, unit.StartIndex, unit.EndIndex, NodeStyle.Operator);
									} else
										goto stop;
								} else
									goto stop;
							}
							break;
						case TT.Not:
							{
								if (context.CanParse(P.Of)) {
									if (!compactMode || IsConjoinedToken(0)) {
										lit_excl = MatchAny();
										// line 264
										LNodeList args;
										// line 265
										int endIndex;
										// Line 266: (TT.LParen ExprList TT.RParen / Expr)
										la0 = (TT) LA0;
										if (la0 == TT.LParen) {
											Skip();
											args = ExprList(e);
											var c = Match((int) TT.RParen);
											// line 266
											endIndex = c.EndIndex;
										} else {
											var T = Expr(P.Of, compactMode);
											// line 267
											args = LNode.List(e, T);
											endIndex = T.Range.EndIndex;
										}
										// line 269
										e = F.Call(S.Of, args, e.Range.StartIndex, endIndex, lit_excl.StartIndex, lit_excl.EndIndex, NodeStyle.Operator);
									} else
										goto stop;
								} else
									goto stop;
							}
							break;
						case TT.Keyword:
							{
								if (!compactMode || IsConjoinedToken(0)) {
									if (context.CanParse(P.Primary)) {
										var kw = MatchAny();
										// line 275
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
							// line 249
							e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
						}
					}
				stop:;
				}
			} while (false);
			// line 280
			return attrs.IsEmpty ? e : e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
		}
	
		Symbol InfixOperatorName(out Token op, bool compactMode)
		{
			TT la0;
			Token op2 = default(Token);
			Symbol result = default(Symbol);
			// Line 283: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | &{!IsContinuator(LT($LI).Value) && !compactMode} TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..}) (TT.Newline (TT.Newline)* / {..}) )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Dot: case TT.NormalOp:
				{
					op = MatchAny();
					// Line 283: (TT.Newline)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						else
							break;
					}
					// line 283
					result = (Symbol) op.Value;
				}
				break;
			case TT.Colon:
				{
					Check((TT) LA(0 + 1) != TT.Newline, "Expected (TT) LA($LI + 1) != TT.Newline");
					op = MatchAny();
					// line 284
					result = (Symbol) op.Value;
				}
				break;
			default:
				{
					Check(!IsContinuator(LT(0).Value) && !compactMode, "Expected !IsContinuator(LT($LI).Value) && !compactMode");
					op = Match((int) TT.Id);
					// Line 288: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..})
					do {
						la0 = (TT) LA0;
						if (la0 == TT.Dot) {
							if (op.EndIndex == LT0.StartIndex) {
								switch ((TT) LA(1)) {
								case EOF: case TT.Assignment: case TT.At: case TT.BackRef:
								case TT.BQId: case TT.Colon: case TT.Dot: case TT.Id:
								case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
								case TT.Newline: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
								case TT.PreOrSufOp:
									goto match1;
								default:
									goto match2;
								}
							} else
								goto match2;
						} else if (la0 == TT.Assignment || la0 == TT.NormalOp) {
							if (op.EndIndex == LT0.StartIndex)
								goto match1;
							else
								goto match2;
						} else
							goto match2;
					match1:
						{
							op2 = MatchAny();
							// line 290
							result = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
						}
						break;
					match2:
						{
							// line 293
							result = GSymbol.Get("'" + op.Value.ToString());
							if ((TT) LA0 == TT.Newline)
								Error(0, "Syntax error. {0}' is used like an operator but is followed by a newline, which is not allowed unless the expression is placed in parentheses.".Localized(result));
						}
					} while (false);
					// Line 298: (TT.Newline (TT.Newline)* / {..})
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						Skip();
						// Line 298: (TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
					} else// line 300
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
			// Line 309: (CallArgs | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen)
				result = CallArgs(e);
			else {
				var lb = Match((int) TT.LBrack);
				args = ExprList(e);
				var rb = Match((int) TT.RBrack);
				// line 312
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
			}
			return result;
		}
	
		LNode CallArgs(LNode target)
		{
			LNodeList args = default(LNodeList);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// line 316
			var endMarker = default(TokenType);
			Skip();
			args = ExprList(ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 319
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
			// Line 325: (&{!compactMode || LT(0).EndIndex == LT(1).StartIndex} (TT.Assignment|TT.Colon|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) NewlinesOpt Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Colon: case TT.NormalOp: case TT.Not:
			case TT.PrefixOp: case TT.PreOrSufOp:
				{
					Check(!compactMode || LT(0).EndIndex == LT(1).StartIndex, "Expected !compactMode || LT(0).EndIndex == LT(1).StartIndex");
					op = MatchAny();
					NewlinesOpt();
					e = Expr(PrefixPrecedenceOf(op), compactMode);
					// line 329
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
			// Line 341: ( (TT.BQId|TT.Id) | TT.Literal | BracedBlock | SquareBracketList | TT.LParen ExprList TT.RParen | TT.BackRef )
			switch ((TT) LA0) {
			case TT.BQId: case TT.Id:
				{
					var id = MatchAny();
					// line 342
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 344
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
					// line 354
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 355
					bool saveParens = !compactMode && (TT) LA0 != TT.At;
					var list = ExprList(ref endMarker);
					lit_rpar = Match((int) TT.RParen);
					// line 358
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
					// line 365
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
					// line 375
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
			// line 380
			result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex).SetStyle(NodeStyle.Expression);
			return result;
		}
		static readonly HashSet<int> TokenList_set0 = NewSet((int) EOF, (int) TT.Comma, (int) TT.Newline, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon);
	
		new LNodeList TokenList()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			// line 384
			result = LNode.List();
			// Line 385: greedy(TokenListParticle)*
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
			// Line 388: ( (TT.Comma|TT.Semicolon) | TT.Newline | TokenListParticle )*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					var t = MatchAny();
					// line 388
					result.Add(F.Id(t));
				} else if (la0 == TT.Newline) {
					var t = MatchAny();
					// line 389
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
			// Line 394: ( TT.LParen TokenListEx TT.RParen / SquareBracketList / BracedBlock / TT.Literal / ~(EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				got_TokenListEx = TokenListEx();
				lit_rpar = Match((int) TT.RParen);
				// line 395
				return F.Call(sy__apos_lpar_rpar, got_TokenListEx, lit_lpar.StartIndex, lit_rpar.EndIndex);
			} else if (la0 == TT.LBrack) {
				got_SquareBracketList = SquareBracketList();
				// line 396
				return got_SquareBracketList;
			} else if (la0 == TT.LBrace) {
				got_BracedBlock = BracedBlock();
				// line 397
				return got_BracedBlock;
			} else if (la0 == TT.Literal) {
				var t = MatchAny();
				// line 398
				return F.Literal(t);
			} else {
				var t = MatchExcept(TokenList_set0);
				// line 399
				return F.Id(t);
			}
		}
	
		LNode KeywordExpression()
		{
			TT la0, la1;
			Token litx2E = default(Token);
			LNode result = default(LNode);
			Token word = default(Token);
			// line 405
			var args = new LNodeList();
			Check(IsConjoinedToken(0 + 1), "Expected IsConjoinedToken($LI + 1)");
			litx2E = MatchAny();
			word = Match((int) TT.Id);
			// Line 409: ((EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) =>  / Expr)
			switch ((TT) LA0) {
			case EOF: case TT.Newline: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				args.Add(Expr(Precedence.MinValue, compactMode: false));
				break;
			}
			// Line 411: greedy((TT.Newline)? BracedBlock)?
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
					// Line 411: (TT.Newline)?
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					args.Add(BracedBlock());
				}
			} while (false);
			// Line 412: greedy(Continuator)*
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
			// line 414
			var keyword = GSymbol.Get("#" + word.Value.ToString());
			int endIndex = args.IsEmpty ? word.EndIndex : args.Last.Range.EndIndex;
			result = MarkSpecial(F.Call(keyword, args, litx2E.StartIndex, endIndex, litx2E.StartIndex, word.EndIndex));
			return result;
		}
	
		LNode Continuator()
		{
			TT la0, la1;
			LNode bb = default(LNode);
			LNode e = default(LNode);
			Token kw = default(Token);
			LNode result = default(LNode);
			// Line 421: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			kw = ContinuatorKeyword();
			// line 422
			var opName = Continuators[kw.Value];
			// Line 424: greedy(TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			// Line 425: (BracedBlock / TopExpr (greedy(TT.Newline)? BracedBlock / {..}))
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				bb = BracedBlock();
				// line 425
				result = F.Call(opName, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			} else {
				e = TopExpr(compactMode: false);
				// Line 427: (greedy(TT.Newline)? BracedBlock / {..})
				do {
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto matchBracedBlock;
						else
							// line 429
							result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					} else if (la0 == TT.LBrace)
						goto matchBracedBlock;
					else
						// line 429
						result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					break;
				matchBracedBlock:
					{
						// Line 427: greedy(TT.Newline)?
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						bb = BracedBlock();
						// line 428
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
			// line 438
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