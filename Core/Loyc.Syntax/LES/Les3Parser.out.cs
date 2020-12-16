// Generated from Les3Parser.ecs by LeMP custom tool. LeMP version: 2.8.4.0
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
			// line 93
			trailingSeparator = default;
			// Line 94: greedy((TT.Comma|TT.Newline|TT.Semicolon))?
			la0 = (TT) LA0;
			if (la0 == TT.Comma || la0 == TT.Newline || la0 == TT.Semicolon)
				trailingSeparator = MatchAny();
			// line 96
			var curSepType = trailingSeparator.Type();
			if (curSepType == TT.Newline && !isBracedBlock) {
				Error(-1, "Expected ',' or ';' here");
			} else if (curSepType != 0) {
				if (curSepType == TT.Newline)
					curSepType = TT.Semicolon;
				if (curSepType != separatorType) {
					if (separatorType == default(TT)) {
						separatorType = curSepType;
					} else if (!(separatorType == TT.Semicolon && curSepType == TT.EOF)) {
						Error(-1, "Unexpected separator: {0} should be {1}", ToString((int) trailingSeparator.Type()), ToString((int) separatorType));
					}
				}
			}
			NewlinesOpt();
			return result;
		}
	
		public LNodeList ExprList(string listContextName, ref TokenType separatorType, LNode firstItem = null, bool presumeCommaSeparated = true, bool isBracedBlock = false)
		{
			TT la0;
			LNodeList got_TokenListEx = default(LNodeList);
			// line 119
			bool oldCommaSeparatedContext = _isCommaSeparatedListContext;
			string oldListContext = _listContextName;
			_isCommaSeparatedListContext = presumeCommaSeparated;
			_listContextName = listContextName;
			LNodeList list = LNodeList.Empty;
			if (firstItem != null)
				list.Add(firstItem);
			NewlinesOpt();
			// Line 131: (&!{isBracedBlock} &!{IsConjoinedToken($LI + 1)} TT.Dot greedy(CompactExpression)* / (TT.SingleQuote TokenListEx / NextExpression)*)
			do {
				la0 = (TT) LA0;
				if (la0 == TT.Dot) {
					if (!isBracedBlock) {
						if (!IsConjoinedToken(0 + 1)) {
							Skip();
							// Line 134: greedy(CompactExpression)*
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
					// line 135
					Token trailingSeparator = default;
					// Line 136: (TT.SingleQuote TokenListEx / NextExpression)*
					for (;;) {
						switch ((TT) LA0) {
						case TT.SingleQuote:
							{
								Skip();
								got_TokenListEx = TokenListEx();
								// line 138
								list.AddRange(got_TokenListEx);
								// line 139
								trailingSeparator = default;
							}
							break;
						case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
						case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
						case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
						case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
						case TT.Semicolon: case TT.TreeDef:
							{
								// line 140
								_isCommaSeparatedListContext = presumeCommaSeparated ? separatorType != TT.Semicolon : separatorType == TT.Comma;
								list.Add(NextExpression(ref separatorType, out trailingSeparator, isBracedBlock));
							}
							break;
						case EOF: case TT.RBrace: case TT.RBrack: case TT.RParen:
							goto stop2;
						default:
							{
								// line 142
								Error(0, "Expected an expression here");
								ErrorTokenList();
							}
							break;
						}
					}
				stop2:;
					// line 145
					if (trailingSeparator.Type() == TT.Comma)
						list.Add(F.Id(S.Missing, trailingSeparator));
				}
			} while (false);
			{
				var __result__ = list;
				_isCommaSeparatedListContext = oldCommaSeparatedContext;
				_listContextName = oldListContext;
				// line 149
				return __result__;
			}
		}
	
		void ErrorTokensOpt()
		{
			LNodeList got_ErrorTokenList = default(LNodeList);
			// line 153
			int _errorPosition = InputPosition;
			got_ErrorTokenList = ErrorTokenList();
			// line 155
			if (!got_ErrorTokenList.IsEmpty)
				Error(_errorPosition - InputPosition, "Expected end of expression (',', ';', etc.)");
		}
	
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> separatorType)
		{
			TT la0;
			LNode got_NextExpression = default(LNode);
			LNodeList got_TokenListEx = default(LNodeList);
			// line 162
			bool isBracedBlock = true;
			NewlinesOpt();
			// Line 165: greedy(&!{IsConjoinedToken($LI + 1)} TT.Dot)?
			la0 = (TT) LA0;
			if (la0 == TT.Dot) {
				if (!IsConjoinedToken(0 + 1)) {
					Skip();
					// line 166
					Error(0, "Expected a statement here");
				}
			}
			// line 169
			Token trailingSeparator = default;
			// Line 170: (TT.SingleQuote TokenListEx / NextExpression)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.SingleQuote:
					{
						Skip();
						got_TokenListEx = TokenListEx();
						// line 172
						foreach (var item in got_TokenListEx)
							yield return item;
						// line 173
						trailingSeparator = default;
					}
					break;
				case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
				case TT.Colon: case TT.Comma: case TT.Dot: case TT.Id:
				case TT.LBrace: case TT.LBrack: case TT.Literal: case TT.LParen:
				case TT.NormalOp: case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				case TT.Semicolon: case TT.TreeDef:
					{
						// line 174
						_isCommaSeparatedListContext = separatorType.Value == TT.Comma;
						got_NextExpression = NextExpression(ref separatorType.Value, out trailingSeparator, isBracedBlock);
						yield // line 176
						return got_NextExpression;
						// line 177
						break;
					}
				case EOF:
					goto stop;
				default:
					{
						// line 178
						Error(0, "Expected an expression here");
						ErrorTokenList();
					}
					break;
				}
			}
		stop:;
			// line 181
			if (trailingSeparator.Type() == TT.Comma)
				yield return F.Id(S.Missing, trailingSeparator);
		}
		static readonly HashSet<int> TopExpr_set0 = NewSet((int) EOF, (int) TT.Assignment, (int) TT.At, (int) TT.BackRef, (int) TT.BQId, (int) TT.Colon, (int) TT.Comma, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Newline, (int) TT.NormalOp, (int) TT.Not, (int) TT.PrefixOp, (int) TT.PreOrSufOp, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.TreeDef);
	
		protected LNode TopExpr(bool compactMode)
		{
			TT la0;
			LNode e = default(LNode);
			Token treeDef = default(Token);
			// Line 187: (TT.TreeDef)?
			la0 = (TT) LA0;
			if (la0 == TT.TreeDef)
				treeDef = MatchAny();
			// Line 188: (Expr)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
			case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				e = Expr(Precedence.MinValue, compactMode);
				break;
			default:
				{
					// line 193
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 194: nongreedy(~(EOF))*
					for (;;) {
						la0 = (TT) LA0;
						if (TopExpr_set0.Contains((int) la0))
							break;
						else
							Skip();
					}
					// Line 195: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => {..})
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BackRef: case TT.BQId:
					case TT.Colon: case TT.Dot: case TT.Id: case TT.LBrace:
					case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.TreeDef:
						e = TopExpr(compactMode);
						break;
					default:
						// line 196
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			// line 200
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
			// line 226
			int startIndex = LT0.StartIndex;
			// line 227
			var attrs = LNode.List();
			// Line 228: (TT.At &!{compactMode && context != Precedence.MinValue} (&{IsConjoinedToken($LI)} TT.At / &{IsConjoinedToken($LI)} => Expr)?  => NewlinesOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Skip();
					Check(!(compactMode && context != Precedence.MinValue), "An attribute cannot appear mid-expression in a compact expression list.");
					// Line 234: (&{IsConjoinedToken($LI)} TT.At / &{IsConjoinedToken($LI)} => Expr)?
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
			// Line 241: (&{(TT) LA(1) == TT.Id && !compactMode} KeywordExpression / (TT.Dot | PrefixExpr) => PrefixExpr greedy( &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || IsConjoinedToken($LI)} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(_precMap.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.SuffixWord)} TT.BQId | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} TT.Keyword )*)
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
					// line 245
					Precedence prec;
					e = PrefixExpr(context, compactMode);
					// Line 252: greedy( &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{!compactMode || IsConjoinedToken($LI)} &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(_precMap.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.SuffixWord)} TT.BQId | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{!compactMode || IsConjoinedToken($LI)} &{context.CanParse(P.Primary)} TT.Keyword )*
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
									if ((TT) LA(0 + 1) != TT.Newline) {
										if (CanParse(context, 0, out prec))
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
								if (!IsContinuator(LT(0).Value) && !compactMode) {
									if (!compactMode || IsConjoinedToken(0)) {
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
								if (context.CanParse(_precMap.Find(OperatorShape.Suffix, LT(0).Value))) {
									if (!compactMode || IsConjoinedToken(0)) {
										var t = MatchAny();
										// line 265
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
										// line 270
										e = F.Call(S.IS, e, F.Id(unit), e.Range.StartIndex, unit.EndIndex, unit.StartIndex, unit.EndIndex, NodeStyle.Operator);
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
										// line 275
										LNodeList args;
										// line 276
										int endIndex;
										// Line 277: (TT.LParen ExprList TT.RParen / Expr)
										la0 = (TT) LA0;
										if (la0 == TT.LParen) {
											Skip();
											args = ExprList("argument list", e);
											var c = Match((int) TT.RParen);
											// line 277
											endIndex = c.EndIndex;
										} else {
											var T = Expr(P.Of, compactMode);
											// line 278
											args = LNode.List(e, T);
											endIndex = T.Range.EndIndex;
										}
										// line 280
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
										// line 286
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
							// line 260
							e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
						}
					}
				stop:;
				}
			} while (false);
			// line 291
			return attrs.IsEmpty ? e : e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
		}
	
		Symbol InfixOperatorName(out Token op, bool compactMode)
		{
			TT la0;
			Token op2 = default(Token);
			Symbol result = default(Symbol);
			// Line 294: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | &{!IsContinuator(LT($LI).Value) && !compactMode} TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..}) (TT.Newline (TT.Newline)* / {..}) )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Dot: case TT.NormalOp:
				{
					op = MatchAny();
					// Line 294: (TT.Newline)*
					for (;;) {
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						else
							break;
					}
					// line 294
					result = (Symbol) op.Value;
				}
				break;
			case TT.Colon:
				{
					Check((TT) LA(0 + 1) != TT.Newline, "Expected (TT) LA($LI + 1) != TT.Newline");
					op = MatchAny();
					// line 295
					result = (Symbol) op.Value;
				}
				break;
			default:
				{
					Check(!IsContinuator(LT(0).Value) && !compactMode, "Expected !IsContinuator(LT($LI).Value) && !compactMode");
					op = Match((int) TT.Id);
					// Line 299: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..})
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
							// line 301
							result = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
						}
						break;
					match2:
						{
							// line 304
							result = GSymbol.Get("'" + op.Value.ToString());
							if ((TT) LA0 == TT.Newline)
								Error(0, "Syntax error. {0}' is used like an operator but is followed by a newline, which is not allowed unless the expression is placed in parentheses.".Localized(result));
						}
					} while (false);
					// Line 309: (TT.Newline (TT.Newline)* / {..})
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						Skip();
						// Line 309: (TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
					} else// line 311
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
			// Line 320: (CallArgs | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen)
				result = CallArgs(e);
			else {
				var lb = Match((int) TT.LBrack);
				args = ExprList("square brackets", e);
				var rb = Match((int) TT.RBrack);
				// line 323
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
			}
			return result;
		}
	
		LNode CallArgs(LNode target)
		{
			LNodeList args = default(LNodeList);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// line 327
			var endMarker = default(TokenType);
			Skip();
			args = ExprList("argument list", ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 330
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
			// Line 336: (&{!compactMode || LT(0).EndIndex == LT(1).StartIndex} (TT.Assignment|TT.Colon|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) NewlinesOpt Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Colon: case TT.NormalOp: case TT.Not:
			case TT.PrefixOp: case TT.PreOrSufOp:
				{
					Check(!compactMode || LT(0).EndIndex == LT(1).StartIndex, "Expected !compactMode || LT(0).EndIndex == LT(1).StartIndex");
					op = MatchAny();
					NewlinesOpt();
					e = Expr(PrefixPrecedenceOf(op), compactMode);
					// line 340
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
			// Line 352: ( (TT.BQId|TT.Id) | TT.Literal | BracedBlock | SquareBracketList | TT.LParen ExprList TT.RParen | TT.BackRef )
			switch ((TT) LA0) {
			case TT.BQId: case TT.Id:
				{
					var id = MatchAny();
					// line 353
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 355
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
					// line 361
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 362
					bool saveParens = !compactMode && (TT) LA0 != TT.At;
					var list = ExprList("parentheses", ref endMarker, presumeCommaSeparated: false);
					lit_rpar = Match((int) TT.RParen);
					// line 365
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
					// line 372
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
					// line 382
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
			// line 387
			result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex).SetStyle(NodeStyle.Expression);
			return result;
		}
		static readonly HashSet<int> ErrorTokenList_set0 = NewSet((int) EOF, (int) TT.Comma, (int) TT.Newline, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.SingleQuote);
	
		new LNodeList ErrorTokenList()
		{
			TT la0;
			LNodeList result = default(LNodeList);
			// line 391
			result = LNode.List();
			// Line 392: greedy(TokenListParticle | TT.SingleQuote)*
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
			// Line 397: ( (TT.Comma|TT.Semicolon) | TT.Newline | TokenListParticle )*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					var t = MatchAny();
					// line 397
					result.Add(F.Id(t));
				} else if (la0 == TT.Newline) {
					var t = MatchAny();
					// line 398
					result.Add(F.Id(sy__aposx0A, t));
				} else if (!ErrorTokenList_set0.Contains((int) la0))
					result.Add(TokenListParticle());
				else
					break;
			}
			// Line 401: (TT.SingleQuote)?
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
			// Line 404: ( TT.LParen TokenListEx TT.RParen / SquareBracketList / BracedBlock / TT.Literal / ~(EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon|TT.SingleQuote) )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				got_TokenListEx = TokenListEx();
				lit_rpar = Match((int) TT.RParen);
				// line 405
				return F.Call(sy__apos_lpar_rpar, got_TokenListEx, lit_lpar.StartIndex, lit_rpar.EndIndex);
			} else if (la0 == TT.LBrack) {
				got_SquareBracketList = SquareBracketList();
				// line 406
				return got_SquareBracketList;
			} else if (la0 == TT.LBrace) {
				got_BracedBlock = BracedBlock();
				// line 407
				return got_BracedBlock;
			} else if (la0 == TT.Literal) {
				var t = MatchAny();
				// line 408
				return F.Literal(t);
			} else {
				var t = MatchExcept(ErrorTokenList_set0);
				// line 409
				return F.Id(t);
			}
		}
	
		LNode BracedBlock()
		{
			TT la0;
			Token lit_lcub = default(Token);
			Token lit_rcub = default(Token);
			LNodeList stmts = default(LNodeList);
			lit_lcub = Match((int) TT.LBrace);
			var endMarker = default(TT);
			// Line 415: greedy(TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			stmts = ExprList("braced block", ref endMarker, isBracedBlock: true, presumeCommaSeparated: false);
			lit_rcub = Match((int) TT.RBrace);
			// line 418
			return F.Call(S.Braces, stmts, lit_lcub.StartIndex, lit_rcub.EndIndex, lit_lcub.StartIndex, lit_lcub.EndIndex).SetStyle(NodeStyle.StatementBlock);
		}
	
		LNode KeywordExpression()
		{
			TT la0, la1;
			Token lit_period = default(Token);
			LNode result = default(LNode);
			Token word = default(Token);
			// line 424
			var args = new LNodeList();
			Check(IsConjoinedToken(0 + 1), "Expected IsConjoinedToken($LI + 1)");
			lit_period = MatchAny();
			word = Match((int) TT.Id);
			// Line 428: ((EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) =>  / Expr (CommaContinuator)*)
			switch ((TT) LA0) {
			case EOF: case TT.Newline: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				{
					args.Add(Expr(Precedence.MinValue, compactMode: false));
					// Line 430: (CommaContinuator)*
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
			// Line 432: greedy((TT.Newline)? BracedBlock)?
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
					// Line 432: (TT.Newline)?
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					args.Add(BracedBlock());
				}
			} while (false);
			// Line 433: greedy(Continuator)*
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
			// line 435
			var keyword = GSymbol.Get("#" + word.Value.ToString());
			int endIndex = args.IsEmpty ? word.EndIndex : args.Last.Range.EndIndex;
			result = MarkSpecial(F.Call(keyword, args, lit_period.StartIndex, endIndex, lit_period.StartIndex, word.EndIndex));
			return result;
		}
	
		LNode CommaContinuator(Symbol word)
		{
			TT la0;
			LNode result = default(LNode);
			Skip();
			// line 443
			if (_isCommaSeparatedListContext)
				Error(-1, "Please add parentheses around the '.{1}' expression. Otherwise, a comma is not allowed " + "because it is unclear whether the comma separates items of the '{0}' or the .{1} expression.", _listContextName, word);
			// Line 448: (TT.Newline)?
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
			// Line 452: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			kw = ContinuatorKeyword();
			// line 453
			var opName = Continuators[kw.Value];
			// Line 455: greedy(TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			// Line 456: (BracedBlock / TopExpr (greedy(TT.Newline)? BracedBlock / {..}))
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				bb = BracedBlock();
				// line 456
				result = F.Call(opName, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			} else {
				e = TopExpr(compactMode: false);
				// Line 458: (greedy(TT.Newline)? BracedBlock / {..})
				do {
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto matchBracedBlock;
						else
							// line 460
							result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					} else if (la0 == TT.LBrace)
						goto matchBracedBlock;
					else
						// line 460
						result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					break;
				matchBracedBlock:
					{
						// Line 458: greedy(TT.Newline)?
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						bb = BracedBlock();
						// line 459
						result = F.Call(opName, e, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
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