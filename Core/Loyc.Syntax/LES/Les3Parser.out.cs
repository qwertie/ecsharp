// Generated from Les3Parser.ecs by LeMP custom tool. LeMP version: 2.9.0.0
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


	partial class Les3Parser
	{
		static readonly Symbol sy__aposx0A = (Symbol) "'\n", sy__apos_lpar_rpar = (Symbol) "'()";
		#pragma warning disable 162, 642
	
		protected new const TT EOF = TT.EOF;
	
			// Note: verbose messages are only printed when custom tool is given --verbose flag
		Dictionary<UString, LNode> _sharedTrees;
		bool _isCommaSeparatedListContext;
		string _listContextName;
		void MissingEndMarker(LNode previousExpr, TokenType endMarker) {
			var location = new SourceRange(SourceFile, LT(-1).EndIndex + 1);
			ErrorSink.Write(Severity.Error, location, "Expected '{0}'", endMarker == TT.Comma ? ',' : ';');
		}
		public LNodeList ExprList(string listContextName, LNode firstItem = null, bool presumeCommaSeparated = true) {
			var endMarker = default(TT);
			return ExprList(listContextName, ref endMarker, firstItem, presumeCommaSeparated, isBracedBlock: false);
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
			// Line 72: greedy(TT.Newline)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline)
					Skip();
				else
					break;
			}
		}
	
		protected LNode CompactExpression(ref TokenType separatorType)
		{
			TT la0;
			Token lit_comma = default(Token);
			Token lit_semi = default(Token);
			Token litx0A = default(Token);
			LNode result = default(LNode);
			// Line 79: ( TopExpr greedy(TT.Comma)? | TT.Comma | TT.Semicolon | TT.Newline )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
				{
					result = TopExpr(compactMode: true);
					// Line 79: greedy(TT.Comma)?
					la0 = (TT) LA0;
					if (la0 == TT.Comma) {
						lit_comma = MatchAny();
						// line 79
						separatorType = TT.Comma;
					}
				}
				break;
			case TT.Comma:
				{
					lit_comma = MatchAny();
					// line 80
					result = F.Id(GSymbol.Empty, lit_comma);
					separatorType = TT.Comma;
				}
				break;
			case TT.Semicolon:
				{
					lit_semi = MatchAny();
					// line 81
					result = F.Id(S.Semicolon, lit_semi);
				}
				break;
			default:
				{
					litx0A = Match((int) TT.Newline);
					// line 82
					result = F.Id(S.Semicolon, litx0A);
				}
				break;
			}
			return result;
		}
	
		protected LNode NextExpression(ref TokenType separatorType, out Token trailingSeparator, bool isBracedBlock)
		{
			TT la0;
			LNode result = default(LNode);
			// Line 88: (TopExpr / {..})
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
				result = TopExpr(compactMode: false);
				break;
			default:
				// line 89
				result = F.Id(S.Missing, LT0);
				break;
			}
			ErrorTokensOpt();
			// Line 92: greedy(&{isBracedBlock} (TT.RBrack|TT.RParen))?
			la0 = (TT) LA0;
			if (la0 == TT.RBrack || la0 == TT.RParen) {
				if (isBracedBlock) {
					Skip();
					// line 92
					Error(-1, "Ignoring unexpected closing bracket");
				}
			}
			// Line 94: greedy((TT.Comma|TT.Newline|TT.Semicolon) / {..})?
			la0 = (TT) LA0;
			if (la0 == TT.Comma || la0 == TT.Newline || la0 == TT.Semicolon)
				trailingSeparator = MatchAny();
			else
				// line 95
				trailingSeparator = default;
			NewlinesOpt();
			// line 98
			bool endOfExprList = false;
			// Line 99: ((EOF|TT.RBrace|TT.RBrack|TT.RParen) => {..})?
			switch ((TT) LA0) {
			case EOF: case TT.RBrace: case TT.RBrack: case TT.RParen:
				// line 99
				endOfExprList = true;
				break;
			}
			// line 108
			TokenType curSepType = trailingSeparator.Type();
			if (!endOfExprList && curSepType == TT.Newline)
				curSepType = TT.Semicolon;
			if (separatorType != default(TT) && curSepType != separatorType && curSepType != TT.Newline && !(curSepType == 0 && endOfExprList)) {
				Error(-1, "Unexpected separator: {0} should be {1}", ToString((int) trailingSeparator.Type()), ToString((int) separatorType));
				separatorType = default(TT);
			} else if (curSepType == TT.Comma || curSepType == TT.Semicolon) {
				separatorType = curSepType;
			}
			return result;
		}
	
		public LNodeList ExprList(string listContextName, ref TokenType separatorType, LNode firstItem = null, bool presumeCommaSeparated = true, bool isBracedBlock = false)
		{
			TT la0;
			LNodeList got_TokenListEx = default(LNodeList);
			// line 126
			bool oldCommaSeparatedContext = _isCommaSeparatedListContext;
			string oldListContext = _listContextName;
			_isCommaSeparatedListContext = presumeCommaSeparated;
			_listContextName = listContextName;
			LNodeList list = LNodeList.Empty;
			if (firstItem != null)
				list.Add(firstItem);
			NewlinesOpt();
			// Line 138: (&!{isBracedBlock} &!{IsConjoinedToken($LI + 1)} TT.Dot greedy(CompactExpression)* / (TT.SingleQuote TokenListEx / NextExpression)*)
			do {
				la0 = (TT) LA0;
				if (la0 == TT.Dot) {
					if (!IsConjoinedToken(0 + 1)) {
						if (!isBracedBlock) {
							Skip();
							// Line 141: greedy(CompactExpression)*
							for (;;) {
								switch ((TT) LA0) {
								case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
								case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
								case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
								case TT.Newline: case TT.NormalOp: case TT.Not: case TT.PrefixOp:
								case TT.PreOrSufOp: case TT.Semicolon: case TT.TreeDef:
									list.Add(CompactExpression(ref separatorType));
									break;
								default:
									goto stop;
								}
							}
						stop:;
						} else
							goto match2;
					} else
						goto match2;
				} else
					goto match2;
				break;
			match2:
				{
					// line 142
					Token trailingSeparator = default;
					// Line 143: (TT.SingleQuote TokenListEx / NextExpression)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.SingleQuote:
							{
								Skip();
								got_TokenListEx = TokenListEx();
								// line 145
								list.AddRange(got_TokenListEx);
								// line 146
								trailingSeparator = default;
							}
							break;
						case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
						case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
						case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
						case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
						case TT.Semicolon: case TT.TreeDef:
							{
								// line 147
								_isCommaSeparatedListContext = presumeCommaSeparated ? separatorType != TT.Semicolon : separatorType == TT.Comma;
								list.Add(NextExpression(ref separatorType, out trailingSeparator, isBracedBlock));
							}
							break;
						case EOF: case TT.RBrace: case TT.RBrack: case TT.RParen:
							goto stop2;
						default:
							{
								// line 149
								Error(0, "Expected an expression here");
								ErrorTokenList();
							}
							break;
						}
					}
				stop2:;
					// line 152
					if (trailingSeparator.Type() == TT.Comma)
						list.Add(F.Id(S.Missing, trailingSeparator));
				}
			} while (false);
			{
				var __result__ = list;
				_isCommaSeparatedListContext = oldCommaSeparatedContext;
				_listContextName = oldListContext;
				// line 156
				return __result__;
			}
		}
	
		void ErrorTokensOpt()
		{
			LNodeList got_ErrorTokenList = default(LNodeList);
			// line 160
			int _errorPosition = InputPosition;
			got_ErrorTokenList = ErrorTokenList();
			// line 162
			if (!got_ErrorTokenList.IsEmpty)
				Error(_errorPosition - InputPosition, "Expected end of expression (',', ';', etc.)");
		}
	
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> separatorType)
		{
			TT la0;
			LNode got_NextExpression = default(LNode);
			LNodeList got_TokenListEx = default(LNodeList);
			// line 169
			bool isBracedBlock = true;
			NewlinesOpt();
			// Line 172: greedy(&!{IsConjoinedToken($LI + 1)} TT.Dot)?
			la0 = (TT) LA0;
			if (la0 == TT.Dot) {
				if (!IsConjoinedToken(0 + 1)) {
					Skip();
					// line 173
					Error(0, "Expected a statement here");
				}
			}
			// line 176
			Token trailingSeparator = default;
			// Line 177: (TT.SingleQuote TokenListEx / NextExpression)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.SingleQuote:
					{
						Skip();
						got_TokenListEx = TokenListEx();
						// line 179
						foreach (var item in got_TokenListEx)
							yield return item;
						// line 180
						trailingSeparator = default;
					}
					break;
				case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
				case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
				case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
				case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				case TT.Semicolon: case TT.TreeDef:
					{
						// line 181
						_isCommaSeparatedListContext = separatorType.Value == TT.Comma;
						got_NextExpression = NextExpression(ref separatorType.Value, out trailingSeparator, isBracedBlock);
						yield // line 183
						return got_NextExpression;
						// line 184
						break;
					}
				case EOF:
					goto stop;
				default:
					{
						// line 185
						Error(0, "Expected an expression here");
						ErrorTokenList();
					}
					break;
				}
			}
		stop:;
			// line 188
			if (trailingSeparator.Type() == TT.Comma)
				yield return F.Id(S.Missing, trailingSeparator);
		}
		static readonly HashSet<int> TopExpr_set0 = NewSet((int) EOF, (int) TT.Assignment, (int) TT.At, (int) TT.BackRef, (int) TT.BQId, (int) TT.Colon, (int) TT.Comma, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Newline, (int) TT.NormalOp, (int) TT.Not, (int) TT.PrefixOp, (int) TT.PreOrSufOp, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.TreeDef);
	
		protected LNode TopExpr(bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			Token treeDef = default(Token);
			// Line 194: (TT.TreeDef)?
			la0 = (TT) LA0;
			if (la0 == TT.TreeDef)
				treeDef = MatchAny();
			// Line 195: (Expr)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				e = Expr(Precedence.MinValue, compactMode);
				break;
			default:
				{
					// line 200
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 201: nongreedy(~(EOF))*
					for (;;) {
						la0 = (TT) LA0;
						if (TopExpr_set0.Contains((int) la0))
							break;
						else
							Skip();
					}
					// Line 202: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => {..})
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
					case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
					case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
						e = TopExpr(compactMode);
						break;
					default:
						// line 203
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			// line 207
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
			// line 233
			int startIndex = LT0.StartIndex;
			// line 234
			var attrs = LNode.List();
			// Line 235: (TT.At &!{compactMode && context != Precedence.MinValue} (&{IsConjoinedToken($LI)} TT.At / &{IsConjoinedToken($LI)} => Expr)?  => NewlinesOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Skip();
					Check(!(compactMode && context != Precedence.MinValue), "An attribute cannot appear mid-expression in a compact expression list.");
					// Line 241: (&{IsConjoinedToken($LI)} TT.At / &{IsConjoinedToken($LI)} => Expr)?
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
			// Line 248: (&{(TT) LA(1) == TT.Id && !compactMode} KeywordExpression / (TT.Dot | PrefixExpr) => PrefixExpr greedy( &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || IsConjoinedToken($LI)} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(_precMap.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.SuffixWord)} TT.BQId | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} TT.Keyword )*)
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
					// line 252
					Precedence prec;
					e = PrefixExpr(context, compactMode);
					// Line 259: greedy( &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || IsConjoinedToken($LI)} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(_precMap.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.SuffixWord)} TT.BQId | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} TT.Keyword )*
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
									if (!IsContinuator(LT(0).Value) && !compactMode) {
										if (CanParse(context, 0, out prec))
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
								if (!compactMode || IsConjoinedToken(0)) {
									if (context.CanParse(_precMap.Find(OperatorShape.Suffix, LT(0).Value))) {
										var t = MatchAny();
										// line 272
										e = F.CallSuffixOp(e, _precMap.ToSuffixOpName((Symbol) t.Value), t);
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
										// line 277
										e = F.CallInfixOp(e, S.IS, unit, F.Id(unit));
									} else
										goto stop;
								} else
									goto stop;
							}
							break;
						case TT.Not:
							{
								if (!compactMode || IsConjoinedToken(0)) {
									if (context.CanParse(P.Of)) {
										lit_excl = MatchAny();
										// line 282
										LNodeList args;
										// line 283
										int endIndex;
										// Line 284: (TT.LParen ExprList TT.RParen / Expr)
										la0 = (TT) LA0;
										if (la0 == TT.LParen) {
											Skip();
											args = ExprList("argument list", e);
											var c = Match((int) TT.RParen);
											// line 284
											endIndex = c.EndIndex;
										} else {
											var T = Expr(P.Of, compactMode);
											// line 285
											args = LNode.List(e, T);
											endIndex = T.Range.EndIndex;
										}
										// line 287
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
										// line 293
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
							var opName = InfixOperatorName(out Token op, compactMode);
							var rhs = Expr(prec, compactMode);
							// line 267
							e = F.CallInfixOp(e, opName, op, rhs);
						}
					}
				stop:;
				}
			} while (false);
			// line 298
			return attrs.IsEmpty ? e : e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
		}
	
		Symbol InfixOperatorName(out Token op, bool compactMode)
		{
			TT la0;
			Token op2 = default(Token);
			Symbol result = default(Symbol);
			// Line 301: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | &{!IsContinuator(LT($LI).Value) && !compactMode} TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..}) (TT.Newline (TT.Newline)* / {..}) )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Dot: case TT.NormalOp:
				{
					op = MatchAny();
					// Line 301: (TT.Newline)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						else
							break;
					}
					// line 301
					result = (Symbol) op.Value;
				}
				break;
			case TT.Colon:
				{
					Check((TT) LA(0 + 1) != TT.Newline, "Expected (TT) LA($LI + 1) != TT.Newline");
					op = MatchAny();
					// line 302
					result = (Symbol) op.Value;
				}
				break;
			default:
				{
					Check(!IsContinuator(LT(0).Value) && !compactMode, "Expected !IsContinuator(LT($LI).Value) && !compactMode");
					op = Match((int) TT.Id);
					// Line 306: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..})
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
							// line 308
							result = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
						}
						break;
					match2:
						{
							// line 311
							result = GSymbol.Get("'" + op.Value.ToString());
							if ((TT) LA0 == TT.Newline)
								Error(0, "Syntax error. {0}' is used like an operator but is followed by a newline, which is not allowed unless the expression is placed in parentheses.".Localized(result));
						}
					} while (false);
					// Line 316: (TT.Newline (TT.Newline)* / {..})
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						Skip();
						// Line 316: (TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
					} else// line 318
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
			// Line 327: (CallArgs | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen)
				result = CallArgs(e);
			else {
				var lb = Match((int) TT.LBrack);
				args = ExprList("square brackets", e);
				var rb = Match((int) TT.RBrack);
				// line 330
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
			}
			return result;
		}
	
		LNode CallArgs(LNode target)
		{
			LNodeList args = default(LNodeList);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// line 334
			var endMarker = default(TokenType);
			Skip();
			args = ExprList("argument list", ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 337
			result = MarkCall(F.CallPrefix(target, args, lit_rpar));
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
			// Line 343: (&{!compactMode || LT(0).EndIndex == LT(1).StartIndex} (TT.Assignment|TT.Colon|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) NewlinesOpt Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Colon: case TT.NormalOp: case TT.Not:
			case TT.PrefixOp: case TT.PreOrSufOp:
				{
					Check(!compactMode || LT(0).EndIndex == LT(1).StartIndex, "Expected !compactMode || LT(0).EndIndex == LT(1).StartIndex");
					op = MatchAny();
					NewlinesOpt();
					e = Expr(PrefixPrecedenceOf(op), compactMode);
					// line 347
					result = F.CallPrefixOp(op, e);
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
			// Line 359: ( (TT.BQId|TT.Id) | TT.Literal | BracedBlock | SquareBracketList | TT.LParen ExprList TT.RParen | TT.BackRef )
			switch ((TT) LA0) {
			case TT.BQId: case TT.Id:
				{
					var id = MatchAny();
					// line 360
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 362
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
					// line 368
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 369
					bool saveParens = !compactMode && (TT) LA0 != TT.At;
					var list = ExprList("parentheses", ref endMarker, presumeCommaSeparated: false);
					lit_rpar = Match((int) TT.RParen);
					// line 372
					if (endMarker != default(TT) || list.Count != 1) {
						result = F.CallBrackets(S.Tuple, lit_lpar, list, lit_rpar);
					} else {
						result = saveParens ? F.InParens(list[0], lit_lpar.StartIndex, lit_rpar.EndIndex) : list[0];
					}
					;
				}
				break;
			case TT.BackRef:
				{
					var backRef = MatchAny();
					// line 379
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
					// line 389
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
			var list = ExprList("square brackets");
			lit_rsqb = Match((int) TT.RBrack);
			// line 394
			result = F.CallBrackets(S.Array, lit_lsqb, list, lit_rsqb, NodeStyle.Expression);
			return result;
		}
		static readonly HashSet<int> ErrorTokenList_set0 = NewSet((int) EOF, (int) TT.Comma, (int) TT.Newline, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.SingleQuote);
	
		LNodeList ErrorTokenList()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			// line 398
			result = LNode.List();
			// Line 399: greedy(TokenListParticle | TT.SingleQuote)*
			for (;;) {
				la0 = (TT) LA0;
				if (!ErrorTokenList_set0.Contains((int) la0))
					result.Add(TokenListParticle());
				else if (la0 == TT.SingleQuote)
					Skip();
				else
					break;
			}
			return result;
		}
	
		LNodeList TokenListEx()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			// Line 404: ( (TT.Comma|TT.Semicolon) | TT.Newline | TokenListParticle )*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					var t = MatchAny();
					// line 404
					result.Add(F.Id(t));
				} else if (la0 == TT.Newline) {
					var t = MatchAny();
					// line 405
					result.Add(F.Id(sy__aposx0A, t));
				} else if (!ErrorTokenList_set0.Contains((int) la0))
					result.Add(TokenListParticle());
				else
					break;
			}
			// Line 408: (TT.SingleQuote)?
			la0 = (TT) LA0;
			if (la0 == TT.SingleQuote)
				Skip();
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
			// Line 411: ( TT.LParen TokenListEx TT.RParen / SquareBracketList / BracedBlock / TT.Literal / ~(EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon|TT.SingleQuote) )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				got_TokenListEx = TokenListEx();
				lit_rpar = Match((int) TT.RParen);
				// line 412
				return F.CallBrackets(sy__apos_lpar_rpar, lit_lpar, got_TokenListEx, lit_rpar);
			} else if (la0 == TT.LBrack) {
				got_SquareBracketList = SquareBracketList();
				// line 413
				return got_SquareBracketList;
			} else if (la0 == TT.LBrace) {
				got_BracedBlock = BracedBlock();
				// line 414
				return got_BracedBlock;
			} else if (la0 == TT.Literal) {
				var t = MatchAny();
				// line 415
				return F.Literal(t);
			} else {
				var t = MatchExcept(ErrorTokenList_set0);
				// line 416
				return F.Id(t);
			}
		}
	
		LNode BracedBlock()
		{
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			LNodeList stmts = default(LNodeList);
			lit_lcub = Match((int) TT.LBrace);
			var endMarker = default(TT);
			stmts = ExprList("braced block", ref endMarker, isBracedBlock: true, presumeCommaSeparated: false);
			lit_rcub = Match((int) TT.RBrace);
			// line 424
			return F.CallBrackets(S.Braces, lit_lcub, stmts, lit_rcub, NodeStyle.StatementBlock);
		}
	
		LNode KeywordExpression()
		{
			TT la0, la1;
			Token lit_period = default(Token);
			LNode result = default(LNode);
			Token word = default(Token);
			// line 430
			var args = new LNodeList();
			Check(IsConjoinedToken(0 + 1), "Expected IsConjoinedToken($LI + 1)");
			lit_period = MatchAny();
			word = Match((int) TT.Id);
			// Line 434: ((EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) =>  / Expr (CommaContinuator)*)
			switch ((TT) LA0) {
			case EOF: case TT.Newline: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				{
					args.Add(Expr(Precedence.MinValue, compactMode: false));
					// Line 436: (CommaContinuator)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Comma) {
							switch ((TT) LA(1)) {
							case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
							case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
							case TT.LBrack: case TT.Literal: case TT.LParen: case TT.Newline:
							case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
							case TT.TreeDef:
								args.Add(CommaContinuator((Symbol) word.Value));
								break;
							default:
								goto stop;
							}
						} else
							goto stop;
					}
				stop:;
				}
				break;
			}
			// Line 438: greedy((TT.Newline)? BracedBlock)?
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
					// Line 438: (TT.Newline)?
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					args.Add(BracedBlock());
				}
			} while (false);
			// Line 439: greedy(Continuator)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline) {
					la1 = (TT) LA(1);
					if (la1 == TT.Id) {
						if (IsContinuator(LT(1).Value))
							args.Add(Continuator((Symbol) word.Value));
						else
							break;
					} else
						break;
				} else if (la0 == TT.Id) {
					if (IsContinuator(LT(0).Value))
						args.Add(Continuator((Symbol) word.Value));
					else
						break;
				} else
					break;
			}
			// line 441
			var keyword = GSymbol.Get("#" + word.Value.ToString());
			int endIndex = args.IsEmpty ? word.EndIndex : args.Last.Range.EndIndex;
			result = MarkSpecial(F.CallPrefixOp(keyword, new IndexRange(lit_period.StartIndex) { 
				EndIndex = word.EndIndex
			}, args));
			return result;
		}
	
		LNode CommaContinuator(Symbol word)
		{
			TT la0;
			LNode result = default(LNode);
			Skip();
			// line 449
			if (_isCommaSeparatedListContext)
				Error(-1, "Please add parentheses around the '.{1}' expression. Otherwise, a comma is not allowed " + "because it is unclear whether the comma separates items of the '{0}' or the .{1} expression.", _listContextName, word);
			// Line 454: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			result = TopExpr(compactMode: false);
			return result;
		}
	
		LNode Continuator(Symbol word)
		{
			TT la0, la1;
			LNode bb = default(LNode);
			LNode e = default(LNode);
			Token kw = default(Token);
			LNode result = default(LNode);
			// Line 458: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			kw = ContinuatorKeyword();
			// line 459
			var opName = Continuators[kw.Value];
			// Line 461: greedy(TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			// Line 462: (BracedBlock / TopExpr (greedy(TT.Newline)? BracedBlock / {..}))
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				bb = BracedBlock();
				// line 462
				result = F.CallPrefixOp(opName, kw, bb);
			} else {
				e = TopExpr(compactMode: false);
				// Line 464: (greedy(TT.Newline)? BracedBlock / {..})
				do {
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto matchBracedBlock;
						else
							// line 466
							result = F.CallPrefixOp(opName, kw, e);
					} else if (la0 == TT.LBrace)
						goto matchBracedBlock;
					else
						// line 466
						result = F.CallPrefixOp(opName, kw, e);
					break;
				matchBracedBlock:
					{
						// Line 464: greedy(TT.Newline)?
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						bb = BracedBlock();
						// line 465
						result = F.CallPrefixOp(opName, kw, LNode.List(e, bb));
					}
				} while (false);
			}
			return result;
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