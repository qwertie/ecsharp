// Generated from Les2ParserGrammar.les by LeMP custom tool. LeMP version: 2.9.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
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

namespace Loyc.Syntax.Les {
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;

	// 0162=Unreachable code detected; 0642=Possibly mistaken empty statement
	#pragma warning disable 162, 642

	public partial class Les2Parser
	{
		public LNodeList ExprList(LNodeList list = default(LNodeList)) {
			var endMarker = default(TT);
			return (ExprList(ref endMarker, list));
		}
		void CheckEndMarker(ref TokenType endMarker, ref Token end)
		{
			if ((endMarker != end.Type())) {
				if ((endMarker == default(TT))) {
					endMarker = end.Type();
				} else {
					Error(-1, "Unexpected separator: {0} should be {1}", 
					ToString(end.TypeInt), ToString((int) endMarker));
				}
			}
		}

		public LNodeList StmtList()
		{
			LNodeList result = default(LNodeList);
			var endMarker = LT0.Type() == TT.Literal && LT0.TypeMarker == null ? TT.EOF : TT.Semicolon;
			result = ExprList(ref endMarker);
			return result;
		}

		// A sequence of expressions separated by commas OR semicolons.
		// The `ref endMarker` parameter tells the caller if semicolons were used.
		public virtual LNodeList ExprList(ref TokenType endMarker, LNodeList list = default(LNodeList))
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// Line 1: ( / TopExpr)
			switch ((TT) LA0) {
			case EOF: case TT.Comma: case TT.RBrace: case TT.RBrack:
			case TT.RParen: case TT.Semicolon:
				{ }
				break;
			default:
				e = TopExpr();
				break;
			}
			// Line 61: ((TT.Comma|TT.Semicolon) ({..} / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					e = e ?? MissingExpr(end);
					// line 63
					list.Add(e.WithRange(e.Range.StartIndex, end.EndIndex));
					// line 64
					CheckEndMarker(ref endMarker, ref end);
					// Line 65: ({..} / TopExpr)
					switch ((TT) LA0) {
					case EOF: case TT.Comma: case TT.RBrace: case TT.RBrack:
					case TT.RParen: case TT.Semicolon:
						// line 65
						e = null;
						break;
					default:
						e = TopExpr();
						break;
					}
				} else
					break;
			}
			// line 67
			if ((e != null || end.Type() == TT.Comma)) {
				list.Add(e ?? MissingExpr(end));
			}
			// line 68
			return list;
		}

		public virtual IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// Line 1: ( / TopExpr)
			la0 = (TT) LA0;
			if (la0 == (TT) EOF || la0 == TT.Comma || la0 == TT.Semicolon) { } else
				e = TopExpr();
			// Line 72: ((TT.Comma|TT.Semicolon) ({..} / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					e = e ?? MissingExpr(end);
					// line 74
					yield
					return e.WithRange(e.Range.StartIndex, end.EndIndex);
					// line 75
					CheckEndMarker(ref endMarker.Value, ref end);
					// Line 76: ({..} / TopExpr)
					la0 = (TT) LA0;
					if (la0 == (TT) EOF || la0 == TT.Comma || la0 == TT.Semicolon)
						// line 76
						e = null;
					else
						e = TopExpr();
				} else
					break;
			}
			// line 78
			if ((e != null || end.Type() == TT.Comma)) {
				yield return e ?? MissingExpr(end);
			}
		}

		protected LNode TopExpr()
		{
			TT la0, la1;
			Token at = default(Token);
			LNodeList attrs = default(LNodeList);
			LNode e = default(LNode);
			Token t = default(Token);
			// line 93
			var attrStart = int.MaxValue;
			// Line 95: greedy(TT.At TT.LBrack ExprList TT.RBrack)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At) {
					la1 = (TT) LA(1);
					if (la1 == TT.LBrack) {
						at = MatchAny();
						// line 96
						if (at.Type() == default(TT)) {
							ErrorSink.Write(Severity.Warning, LaIndexToMsgContext(0), "Attribute: expected '@['");
						} else {
							attrStart = System.Math.Min(attrStart, at.StartIndex);
						}
						t = MatchAny();
						attrs = ExprList(attrs);
						Match((int) TT.RBrack);
					} else
						break;
				} else
					break;
			}
			// Line 100: (Expr / TT.Id Expr (Particle)*)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQOperator: case TT.Dot: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				e = Expr(StartStmt);
				break;
			case TT.Id:
				{
					switch ((TT) LA(1)) {
					case EOF: case TT.Assignment: case TT.BQOperator: case TT.Comma:
					case TT.Dot: case TT.LBrack: case TT.LParen: case TT.NormalOp:
					case TT.Not: case TT.PreOrSufOp: case TT.RBrace: case TT.RBrack:
					case TT.RParen: case TT.Semicolon:
						e = Expr(StartStmt);
						break;
					default:
						{
							var id = MatchAny();
							// line 103
							var args = LNodeList.Empty;
							args.Add(Expr(P.SuperExpr));
							// Line 105: (Particle)*
							for (;;) {
								switch ((TT) LA0) {
								case TT.At: case TT.Id: case TT.LBrace: case TT.LBrack:
								case TT.Literal: case TT.LParen: case TT.SpaceLParen:
									{
										// line 106
										if ((((TT) LA0) == TT.LParen)) {
											var loc = args[args.Count - 2, args.Last].Range.End;
											Error(0, "Expected a space before '(' (possibly missing ';' or ',' at {0})", loc);
										}
										args.Add(Particle());
									}
									break;
								default:
									goto stop;
								}
							}
						stop:;
							// line 113
							e = MarkSpecial(F.Call(id, args, id.StartIndex, args.Last.Range.EndIndex));
						}
						break;
					}
				}
				break;
			default:
				e = Expr(StartStmt);
				break;
			}
			// line 115
			if ((attrStart < e.Range.StartIndex)) {
				e = e.WithRange(attrStart, e.Range.EndIndex);
			}
			// line 116
			return e.PlusAttrsBefore(attrs);
		}

		// Types of (normal) expressions:
		// - particles: ids, literals, (parenthesized), {braced}
		// - ++prefix_operators
		// - infix + operators
		// - suffix_operators++
		// - Special primary expressions:
		//   method_calls(with arguments), indexers[with indexes], generic!arguments
		LNode Expr(Precedence context)
		{
			TT la0;
			LNode e = default(LNode);
			Token lit_excl = default(Token);
			Token t = default(Token);
			// line 128
			Precedence prec;
			e = PrefixExpr(context);
			// Line 132: greedy( &{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} (TT.Assignment|TT.BQOperator|TT.Dot|TT.NormalOp) Expr | &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{context.CanParse(P.Of)} TT.Not (TT.LParen ExprList TT.RParen / Expr) | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Assignment: case TT.BQOperator: case TT.Dot: case TT.NormalOp:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							// line 133
							if ((!prec.CanMixWith(context))) {
								Error(0, "Operator '{0}' is not allowed in this context. Add parentheses to clarify the code's meaning.", LT0.Value);
							}
							t = MatchAny();
							var rhs = Expr(prec);
							// line 138
							e = F.Call(t, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex, NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				case TT.LBrack: case TT.LParen:
					{
						if (context.CanParse(P.Primary))
							e = FinishPrimaryExpr(e);
						else
							goto stop;
					}
					break;
				case TT.Not:
					{
						if (context.CanParse(P.Of)) {
							lit_excl = MatchAny();
							// line 145
							var args = new LNodeList { 
								e
							};
							int endIndex;
							// Line 146: (TT.LParen ExprList TT.RParen / Expr)
							la0 = (TT) LA0;
							if (la0 == TT.LParen) {
								Skip();
								args = ExprList(args);
								var c = Match((int) TT.RParen);
								// line 146
								endIndex = c.EndIndex;
							} else {
								var T = Expr(P.Of);
								// line 147
								args.Add(T);
								endIndex = T.Range.EndIndex;
							}
							// line 149
							e = F.Call(S.Of, args, e.Range.StartIndex, endIndex, lit_excl.StartIndex, lit_excl.EndIndex, NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				case TT.PreOrSufOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
							t = MatchAny();
							// line 153
							e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex, t.StartIndex, t.EndIndex, NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 155
			return e;
		}

		// Helper rule that parses one of the syntactically special primary expressions
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			LNodeList list = default(LNodeList);
			Token lit_lsqb = default(Token);
			// Line 161: (TT.LParen ExprList TT.RParen | TT.LBrack ExprList TT.RBrack)
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				// line 161
				var endMarker = default(TokenType);
				Skip();
				list = ExprList(ref endMarker);
				var c = Match((int) TT.RParen);
				// line 164
				e = MarkCall(F.Call(e, list, e.Range.StartIndex, c.EndIndex));
				if ((endMarker == TT.Semicolon)) {
					e.Style = NodeStyle.StatementBlock | NodeStyle.Alternate;
				}
			} else {
				// line 168
				var args = new LNodeList { 
					e
				};
				lit_lsqb = Match((int) TT.LBrack);
				args = ExprList(args);
				var c = Match((int) TT.RBrack);
				// line 170
				e = F.Call(S.IndexBracks, args, e.Range.StartIndex, c.EndIndex, lit_lsqb.StartIndex, lit_lsqb.EndIndex, NodeStyle.Operator);
			}
			// line 172
			return e;
		}

		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			LNode result = default(LNode);
			Token t = default(Token);
			// Line 177: ((TT.Assignment|TT.BQOperator|TT.Dot|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr | Particle)
			switch ((TT) LA0) {
			case TT.Assignment: case TT.BQOperator: case TT.Dot: case TT.NormalOp:
			case TT.Not: case TT.PrefixOp: case TT.PreOrSufOp:
				{
					t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t));
					// line 179
					result = F.Call(t, e, t.StartIndex, e.Range.EndIndex, NodeStyle.Operator);
				}
				break;
			default:
				result = Particle();
				break;
			}
			return result;
		}

		// An Particle is:
		// - an (expression) in parenthesis or a tuple
		// - a literal or simple identifier
		//   - simple calls are also handled here, as a space optimization
		// - a token literal @{ ... }
		// - a prefix operator followed by an Expr
		// - a { block } in braces
		// - a [ list  ] in square brackets
		LNode Particle()
		{
			TT la0;
			Token c = default(Token);
			Token o = default(Token);
			LNode result = default(LNode);
			TokenTree tree = default(TokenTree);
			// Line 192: ( TT.Id | TT.Literal | TT.At (TT.LBrack TokenTree TT.RBrack | TT.LBrace TokenTree TT.RBrace) | TT.LBrace StmtList TT.RBrace | TT.LBrack ExprList TT.RBrack | (TT.LParen|TT.SpaceLParen) ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.Id:
				{
					var id = MatchAny();
					// line 193
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 195
					result = F.Literal(lit).SetStyle(lit.Style);
				}
				break;
			case TT.At:
				{
					o = MatchAny();
					// Line 198: (TT.LBrack TokenTree TT.RBrack | TT.LBrace TokenTree TT.RBrace)
					la0 = (TT) LA0;
					if (la0 == TT.LBrack) {
						Skip();
						tree = TokenTree();
						c = Match((int) TT.RBrack);
					} else {
						Match((int) TT.LBrace);
						tree = TokenTree();
						c = Match((int) TT.RBrace);
					}
					// line 200
					result = F.Literal(tree, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					o = MatchAny();
					var list = StmtList();
					c = Match((int) TT.RBrace);
					// line 203
					result = F.Braces(list, o.StartIndex, c.EndIndex).SetStyle(NodeStyle.StatementBlock);
				}
				break;
			case TT.LBrack:
				{
					o = MatchAny();
					var list = ExprList();
					c = Match((int) TT.RBrack);
					// line 205
					result = F.Call(S.Array, list, o.StartIndex, c.EndIndex, o.StartIndex, o.EndIndex, NodeStyle.Expression);
				}
				break;
			case TT.LParen: case TT.SpaceLParen:
				{
					// line 207
					var endMarker = default(TT);
					o = MatchAny();
					// line 208
					var hasAttrList = ((TT) LA0) == TT.LBrack || ((TT) LA0) == TT.At;
					var list = ExprList(ref endMarker);
					c = Match((int) TT.RParen);
					// line 211
					if ((endMarker == TT.Semicolon || list.Count != 1)) {
						result = F.Call(S.Tuple, list, o.StartIndex, c.EndIndex, o.StartIndex, o.EndIndex, NodeStyle.Expression);
						if ((endMarker == TT.Comma)) {
							var msg = "Tuples require ';' as a separator.";
							if ((o.Type() == TT.SpaceLParen)) {
								msg += " If a function call was intended, remove the space(s) before '('.";
							}
							ErrorSink.Error(list[0].Range.End, msg);
						}
					} else {
						result = hasAttrList ? list[0] : F.InParens(list[0], o.StartIndex, c.EndIndex);
					}
				}
				break;
			default:
				{
					// line 225
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
			// line 232
			result = new TokenTree(SourceFile);
			// Line 234: nongreedy((TT.LBrace|TT.LBrack|TT.LParen|TT.SpaceLParen) TokenTree (TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
			for (;;) {
				switch ((TT) LA0) {
				case EOF: case TT.RBrace: case TT.RBrack: case TT.RParen:
					goto stop;
				case TT.LBrace: case TT.LBrack: case TT.LParen: case TT.SpaceLParen:
					{
						la1 = (TT) LA(1);
						if (la1 != (TT) EOF) {
							var open = MatchAny();
							got_TokenTree = TokenTree();
							// line 236
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