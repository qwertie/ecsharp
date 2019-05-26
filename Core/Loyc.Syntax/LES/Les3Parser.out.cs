// Generated from Les3Parser.ecs by LeMP custom tool. LeMP version: 2.6.8.0
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
using Loyc.Syntax.Lexing;	// For BaseLexer
using Loyc.Syntax;	// For BaseParser<Token> and LNode
namespace Loyc.Syntax.Les
{
	using TT = TokenType;	// Abbreviate TokenType as TT
	using P = LesPrecedence;
	using S = CodeSymbols;

	partial class Les3Parser {
		static readonly Symbol sy__apos_colonsuf = (Symbol) "':suf", sy__apos_lpar_rpar = (Symbol) "'()";
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
			var location = new SourceRange(SourceFile, LT(-1).EndIndex + 1);
			ErrorSink.Write(Severity.Error, location, "Expected '{0}'", endMarker == TT.Comma ? ',' : ';');
		}
		public VList<LNode> ExprList(VList<LNode> list = default(VList<LNode>)) {
			var endMarker = default(TT);
			return ExprList(ref endMarker, list);
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
			// Line 79: greedy(TT.Newline)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Newline)
					Skip();
				else
					break;
			}
		}
	
		public VList<LNode> ExprList(ref TokenType endMarker, VList<LNode> list = default(VList<LNode>), bool isBracedBlock = false)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			VList<LNode> result = default(VList<LNode>);
			NewlinesOpt();
			// Line 86: (TopExpr)?
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
			case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.SingleQuoteOp:
				e = TopExpr();
				break;
			}
			ErrorTokensOpt();
			// Line 88: ((&{isBracedBlock} (TT.RBrack|TT.RParen))? (TT.Comma|TT.Newline|TT.Semicolon) NewlinesOpt ({..} / TopExpr) ErrorTokensOpt)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.RBrack: case TT.RParen:
					{
						if (isBracedBlock)
							goto match1;
						else
							goto stop;
					}
				case TT.Comma: case TT.Newline: case TT.Semicolon:
					goto match1;
				default:
					goto stop;
				}
			match1:
				{
					// Line 88: (&{isBracedBlock} (TT.RBrack|TT.RParen))?
					la0 = (TT) LA0;
					if (la0 == TT.RBrack || la0 == TT.RParen) {
						Check(isBracedBlock, "Expected isBracedBlock");
						// line 88
						Error(0, "Unexpected closing bracket");
						Skip();
					}
					end = Match((int) TT.Comma, (int) TT.Newline, (int) TT.Semicolon);
					// line 90
					CheckEndMarker(ref endMarker, ref end);
					NewlinesOpt();
					// line 92
					list.Add(e ?? MissingExpr(end));
					// Line 93: ({..} / TopExpr)
					switch ((TT) LA0) {
					case EOF: case TT.Comma: case TT.Newline: case TT.RBrace:
					case TT.RBrack: case TT.RParen: case TT.Semicolon:
						// line 93
						e = null;
						break;
					default:
						e = TopExpr();
						break;
					}
					ErrorTokensOpt();
				}
			}
		stop:;
			// line 96
			if (e != null || end.Type() == TT.Comma)
				list.Add(e ?? MissingExpr(end, afterToken: true));
			result = list;
			return result;
		}
	
		void ErrorTokensOpt()
		{
			VList<LNode> got_TokenList = default(VList<LNode>);
			// line 103
			int _errorPosition = InputPosition;
			got_TokenList = TokenList();
			// line 105
			if (!got_TokenList.IsEmpty)
				Error(_errorPosition - InputPosition, "Expected end of expression (',', ';', etc.)");
		}
	
		public IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			NewlinesOpt();
			// Line 113: (TopExpr)?
			switch ((TT) LA0) {
			case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
			case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
			case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.SingleQuoteOp:
				e = TopExpr();
				break;
			}
			// Line 114: ((TT.Comma|TT.Newline|TT.Semicolon) NewlinesOpt ({..} / TopExpr) ErrorTokensOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Newline || la0 == TT.Semicolon) {
					end = MatchAny();
					// line 115
					CheckEndMarker(ref endMarker.Value, ref end);
					NewlinesOpt();
					yield // line 117
					return e ?? MissingExpr(end);
					// Line 118: ({..} / TopExpr)
					switch ((TT) LA0) {
					case EOF: case TT.Comma: case TT.Newline: case TT.Semicolon:
						// line 118
						e = null;
						break;
					default:
						e = TopExpr();
						break;
					}
					ErrorTokensOpt();
				} else
					break;
			}
			// line 121
			if (e != null || end.Type() == TT.Comma) {
				yield return e ?? MissingExpr(end, afterToken: true);
			}
		}
		static readonly HashSet<int> TopExpr_set0 = NewSet((int) EOF, (int) TT.Assignment, (int) TT.At, (int) TT.BQId, (int) TT.BQOperator, (int) TT.Colon, (int) TT.Comma, (int) TT.Id, (int) TT.Keyword, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Newline, (int) TT.NormalOp, (int) TT.Not, (int) TT.PrefixOp, (int) TT.PreOrSufOp, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.SingleQuoteOp);
	
		protected LNode TopExpr()
		{
			TT la0;
			LNode e = default(LNode);
			Token lit_colon = default(Token);
			LNode result = default(LNode);
			// line 125
			int startIndex = LT0.StartIndex;
			// line 126
			var attrs = new VList<LNode>();
			// Line 128: (TT.At (TT.At | Particle greedy(&!{IsSpaceBefore($LI)} CallArgs)?) greedy(~(EOF|TT.LBrace|TT.Newline) => {..})? NewlinesOpt)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					Skip();
					// line 128
					CheckForSpace(false, "Unexpected space after `@`");
					// Line 129: (TT.At | Particle greedy(&!{IsSpaceBefore($LI)} CallArgs)?)
					la0 = (TT) LA0;
					if (la0 == TT.At)
						Skip();
					else {
						var attr = Particle(inAttribute: true);
						// Line 131: greedy(&!{IsSpaceBefore($LI)} CallArgs)?
						la0 = (TT) LA0;
						if (la0 == TT.LParen) {
							if (!IsSpaceBefore(0))
								attr = CallArgs(attr);
						}
						// line 132
						attrs.Add(attr);
					}
					// Line 134: greedy(~(EOF|TT.LBrace|TT.Newline) => {..})?
					la0 = (TT) LA0;
					if (!(la0 == (TT) EOF || la0 == TT.LBrace || la0 == TT.Newline))
						// line 134
						CheckForSpace(true, "Expected space after attribute");
					NewlinesOpt();
				} else
					break;
			}
			// Line 137: (Expr greedy(TT.Colon (EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen) => )?)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQId: case TT.BQOperator: case TT.Colon:
			case TT.Id: case TT.Keyword: case TT.LBrace: case TT.LBrack:
			case TT.Literal: case TT.LParen: case TT.NormalOp: case TT.Not:
			case TT.PrefixOp: case TT.PreOrSufOp: case TT.SingleQuoteOp:
				{
					e = Expr(Precedence.MinValue);
					// Line 139: greedy(TT.Colon (EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen) => )?
					la0 = (TT) LA0;
					if (la0 == TT.Colon) {
						lit_colon = MatchAny();
						// line 140
						e = F.Call(sy__apos_colonsuf, e, e.Range.StartIndex, lit_colon.EndIndex, lit_colon.StartIndex, lit_colon.EndIndex);
					}
				}
				break;
			default:
				{
					// line 142
					Error(0, "Expected an expression here");
					MatchExcept();
					// Line 143: nongreedy(~(EOF))*
					for (;;) {
						la0 = (TT) LA0;
						if (TopExpr_set0.Contains((int) la0))
							break;
						else
							Skip();
					}
					// Line 144: (TopExpr | (EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) => {..})
					switch ((TT) LA0) {
					case TT.Assignment: case TT.At: case TT.BQId: case TT.BQOperator:
					case TT.Colon: case TT.Id: case TT.Keyword: case TT.LBrace:
					case TT.LBrack: case TT.Literal: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp: case TT.SingleQuoteOp:
						e = TopExpr();
						break;
					default:
						// line 144
						e = MissingExpr(LT0);
						break;
					}
				}
				break;
			}
			// line 146
			if (!attrs.IsEmpty) {
				e = e.PlusAttrsBefore(attrs).WithRange(startIndex, e.Range.EndIndex);
			}
			// line 147
			result = e;
			return result;
		}
	
		LNode Expr(Precedence context)
		{
			TT la0;
			LNode e = default(LNode);
			Token lit_excl = default(Token);
			Token t = default(Token);
			// Line 163: (KeywordExpression | PrefixExpr greedy( &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{context.CanParse(_prec.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) )*)
			la0 = (TT) LA0;
			if (la0 == TT.Keyword)
				e = KeywordExpression();
			else {
				// line 164
				Precedence prec;
				e = PrefixExpr(context);
				// Line 168: greedy( &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{CanParse(context, $LI, out prec)} InfixOperatorName Expr | &{context.CanParse(_prec.Find(OperatorShape.Suffix, LT($LI).Value))} TT.PreOrSufOp | &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) )*
				for (;;) {
					switch ((TT) LA0) {
					case TT.LBrack: case TT.LParen:
						{
							if (context.CanParse(P.Primary))
								e = FinishPrimaryExpr(e);
							else
								goto stop;
						}
						break;
					case TT.Assignment: case TT.Dot: case TT.NormalOp:
						{
							if (CanParse(context, 0, out prec))
								goto match2;
							else
								goto stop;
						}
					case TT.Colon:
						{
							if (CanParse(context, 0, out prec)) {
								if ((TT) LA(0 + 1) != TT.Newline)
									goto match2;
								else
									goto stop;
							} else
								goto stop;
						}
					case TT.Id:
						{
							if (CanParse(context, 0, out prec)) {
								if (!IsContinuator(LT(0).Value))
									goto match2;
								else
									goto stop;
							} else
								goto stop;
						}
					case TT.PreOrSufOp:
						{
							if (context.CanParse(_prec.Find(OperatorShape.Suffix, LT(0).Value))) {
								t = MatchAny();
								// line 178
								e = F.Call(_prec.ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
							} else
								goto stop;
						}
						break;
					case TT.Not:
						{
							if (context.CanParse(P.Of)) {
								lit_excl = MatchAny();
								// line 182
								var args = new VList<LNode> { 
									e
								};
								int endIndex;
								// Line 183: (TT.LParen ExprList TT.RParen / Expr)
								la0 = (TT) LA0;
								if (la0 == TT.LParen) {
									Skip();
									args = ExprList(args);
									var c = Match((int) TT.RParen);
									// line 183
									endIndex = c.EndIndex;
								} else {
									var T = Expr(P.Of);
									// line 184
									args.Add(T);
									endIndex = T.Range.EndIndex;
								}
								// line 186
								e = F.Call(S.Of, args, e.Range.StartIndex, endIndex, lit_excl.StartIndex, lit_excl.EndIndex, NodeStyle.Operator);
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
						var opName = InfixOperatorName(out op);
						var rhs = Expr(prec);
						// line 174
						e = F.Call(opName, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, op.StartIndex, op.EndIndex, NodeStyle.Operator);
					}
				}
			stop:;
			}
			// line 188
			return e;
		}
	
		Symbol InfixOperatorName(out Token op)
		{
			TT la0;
			Token op2 = default(Token);
			Symbol result = default(Symbol);
			// Line 191: ( (TT.Assignment|TT.Dot|TT.NormalOp) (TT.Newline)* | &{(TT) LA($LI + 1) != TT.Newline} TT.Colon | &!{IsContinuator(LT($LI).Value)} TT.Id (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..}) (TT.Newline (TT.Newline)* / {..}) )
			switch ((TT) LA0) {
			case TT.Assignment: case TT.Dot: case TT.NormalOp:
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
					result = (Symbol) op.Value;
				}
				break;
			case TT.Colon:
				{
					Check((TT) LA(0 + 1) != TT.Newline, "Expected (TT) LA($LI + 1) != TT.Newline");
					op = MatchAny();
					// line 192
					result = (Symbol) op.Value;
				}
				break;
			default:
				{
					Check(!IsContinuator(LT(0).Value), "Did not expect IsContinuator(LT($LI).Value)");
					op = Match((int) TT.Id);
					// Line 196: (&{op.EndIndex == LT0.StartIndex} (TT.Assignment|TT.Dot|TT.NormalOp) / {..})
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
							// line 198
							result = GSymbol.Get("'" + op.Value.ToString() + op2.Value.ToString().Substring(1));
						}
						break;
					match2:
						{
							// line 201
							result = GSymbol.Get("'" + op.Value.ToString());
							if ((TT) LA0 == TT.Newline)
								Error(0, "Syntax error. {0}' is used like an operator but is followed by a newline, which is not allowed unless the expression is placed in parentheses.".Localized(result));
						}
					} while (false);
					// Line 206: (TT.Newline (TT.Newline)* / {..})
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						Skip();
						// Line 206: (TT.Newline)*
						for (;;) {
							la0 = (TT) LA0;
							if (la0 == TT.Newline)
								Skip();
							else
								break;
						}
					} else// line 208
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
			LNode result = default(LNode);
			// Line 217: (CallArgs | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen)
				result = CallArgs(e);
			else {
				// line 219
				var args = new VList<LNode> { 
					e
				};
				var lb = Match((int) TT.LBrack);
				args = ExprList(args);
				var rb = Match((int) TT.RBrack);
				// line 221
				return F.Call(S.IndexBracks, args, e.Range.StartIndex, rb.EndIndex, lb.StartIndex, rb.EndIndex, NodeStyle.Operator);
			}
			return result;
		}
	
		LNode CallArgs(LNode target)
		{
			VList<LNode> args = default(VList<LNode>);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// line 225
			var endMarker = default(TokenType);
			Skip();
			args = ExprList(ref endMarker);
			lit_rpar = Match((int) TT.RParen);
			// line 228
			result = MarkCall(F.Call(target, args, target.Range.StartIndex, lit_rpar.EndIndex).SetBaseStyle(NodeStyle.PrefixNotation));
			if (endMarker == TT.Semicolon) {
				result.Style |= NodeStyle.Alternate;
			}
			return result;
		}
	
		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			Token op = default(Token);
			LNode result = default(LNode);
			// Line 234: ((TT.Assignment|TT.BQOperator|TT.Colon|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr / Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQOperator: case TT.Colon: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				{
					op = MatchAny();
					e = Expr(PrefixPrecedenceOf(op));
					// line 236
					result = F.Call(op, e, op.StartIndex, e.Range.EndIndex, NodeStyle.Operator);
				}
				break;
			default:
				result = Particle();
				break;
			}
			return result;
		}
	
		LNode Particle(bool inAttribute = false)
		{
			VList<LNode> got_TokenList = default(VList<LNode>);
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			LNode result = default(LNode);
			// Line 247: ( (TT.BQId|TT.Id) | TT.Literal | TT.SingleQuoteOp TokenList | BracedBlock | SquareBracketList | TT.LParen ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.BQId: case TT.Id:
				{
					var id = MatchAny();
					// line 248
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 250
					result = F.Literal(lit);
				}
				break;
			case TT.SingleQuoteOp:
				{
					var op = MatchAny();
					got_TokenList = TokenList();
					// line 253
					result = F.Call((Symbol) op.Value, got_TokenList, op.StartIndex, got_TokenList.IsEmpty ? op.EndIndex : got_TokenList.Last.Range.EndIndex);
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
					// line 260
					var endMarker = default(TT);
					lit_lpar = MatchAny();
					// line 261
					bool saveParens = !inAttribute && (TT) LA0 != TT.At;
					var list = ExprList(ref endMarker);
					lit_rpar = Match((int) TT.RParen);
					// line 264
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
					// line 271
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
			// line 276
			result = F.Call(S.Array, list, lit_lsqb.StartIndex, lit_rsqb.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex).SetStyle(NodeStyle.Expression);
			return result;
		}
		static readonly HashSet<int> TokenList_set0 = NewSet((int) EOF, (int) TT.Comma, (int) TT.Newline, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon);
	
		new VList<LNode> TokenList()
		{
			TT la0;
			VList<LNode> result = default(VList<LNode>);
			// line 280
			result = LNode.List();
			// Line 281: greedy(TokenListParticle)*
			for (;;) {
				la0 = (TT) LA0;
				if (!TokenList_set0.Contains((int) la0))
					result.Add(TokenListParticle());
				else
					break;
			}
			return result;
		}
	
		VList<LNode> TokenListEx()
		{
			TT la0;
			VList<LNode> result = default(VList<LNode>);
			Token t = default(Token);
			// Line 284: ((TT.Comma|TT.Semicolon) | TokenListParticle)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					t = MatchAny();
					// line 284
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
			VList<LNode> got_TokenListEx = default(VList<LNode>);
			Token lit_lpar = default(Token);
			Token lit_rpar = default(Token);
			// Line 289: ( TT.LParen TokenListEx TT.RParen / SquareBracketList / BracedBlock / TT.Literal / ~(EOF|TT.Comma|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				got_TokenListEx = TokenListEx();
				lit_rpar = Match((int) TT.RParen);
				// line 290
				return F.Call(sy__apos_lpar_rpar, got_TokenListEx, lit_lpar.StartIndex, lit_rpar.EndIndex);
			} else if (la0 == TT.LBrack) {
				got_SquareBracketList = SquareBracketList();
				// line 291
				return got_SquareBracketList;
			} else if (la0 == TT.LBrace) {
				got_BracedBlock = BracedBlock();
				// line 292
				return got_BracedBlock;
			} else if (la0 == TT.Literal) {
				var t = MatchAny();
				// line 293
				return F.Literal(t);
			} else {
				var t = MatchExcept(TokenList_set0);
				// line 294
				return F.Id(t);
			}
		}
	
		LNode KeywordExpression()
		{
			TT la0, la1;
			Token kw = default(Token);
			LNode result = default(LNode);
			// line 300
			var args = new VList<LNode>();
			kw = MatchAny();
			// line 301
			var keyword = kw.Value as Symbol;
			// Line 303: ((EOF|TT.Newline|TT.RBrace|TT.RBrack|TT.RParen|TT.Semicolon) =>  / Expr)
			switch ((TT) LA0) {
			case EOF: case TT.Newline: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				args.Add(Expr(Precedence.MinValue));
				break;
			}
			// Line 305: greedy((TT.Newline)? BracedBlock)?
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
					// Line 305: (TT.Newline)?
					la0 = (TT) LA0;
					if (la0 == TT.Newline)
						Skip();
					args.Add(BracedBlock());
				}
			} while (false);
			// Line 306: greedy(Continuator)*
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
			// line 308
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
			// Line 314: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			kw = ContinuatorKeyword();
			// line 315
			var opName = Continuators[kw.Value];
			// Line 317: (TT.Newline)?
			la0 = (TT) LA0;
			if (la0 == TT.Newline)
				Skip();
			// Line 318: (BracedBlock / TopExpr (greedy(TT.Newline)? BracedBlock / {..}))
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				bb = BracedBlock();
				// line 318
				result = F.Call(opName, bb, kw.StartIndex, bb.Range.EndIndex, kw.StartIndex, kw.EndIndex);
			} else {
				e = TopExpr();
				// Line 320: (greedy(TT.Newline)? BracedBlock / {..})
				do {
					la0 = (TT) LA0;
					if (la0 == TT.Newline) {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto matchBracedBlock;
						else
							// line 322
							result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					} else if (la0 == TT.LBrace)
						goto matchBracedBlock;
					else
						// line 322
						result = F.Call(opName, e, kw.StartIndex, e.Range.EndIndex, kw.StartIndex, kw.EndIndex);
					break;
				matchBracedBlock:
					{
						// Line 320: greedy(TT.Newline)?
						la0 = (TT) LA0;
						if (la0 == TT.Newline)
							Skip();
						bb = BracedBlock();
						// line 321
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
			VList<LNode> stmts = default(VList<LNode>);
			lit_lcub = Match((int) TT.LBrace);
			var endMarker = default(TT);
			stmts = ExprList(ref endMarker, isBracedBlock: true);
			lit_rcub = Match((int) TT.RBrace);
			// line 331
			return F.Call(S.Braces, stmts, lit_lcub.StartIndex, lit_rcub.EndIndex, lit_lcub.StartIndex, lit_lcub.EndIndex).SetStyle(NodeStyle.Statement);
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