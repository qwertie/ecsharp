// Generated from LesParserGrammar.les by LeMP custom tool. LeMP version: 1.9.0.0
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
namespace Loyc.Syntax.Les
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;
	#pragma warning disable 162, 642
	public partial class LesParser
	{
		public VList<LNode> ExprList(VList<LNode> list = default(VList<LNode>))
		{
			var endMarker = default(TT);
			return (ExprList(ref endMarker, list));
		}
		void CheckEndMarker(ref TokenType endMarker, ref Token end)
		{
			if ((endMarker != end.Type())) {
				if ((endMarker == default(TT))) {
					endMarker = end.Type();
				} else {
					Error(-1, "Unexpected separator: {0} should be {1}", ToString(end.TypeInt), ToString((int) endMarker));
				}
			}
		}
		public VList<LNode> StmtList()
		{
			VList<LNode> result = default(VList<LNode>);
			var endMarker = TT.Semicolon;
			result = ExprList(ref endMarker);
			return result;
		}
		public virtual VList<LNode> ExprList(ref TokenType endMarker, VList<LNode> list = default(VList<LNode>))
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// line 57
			if ((LT0.Value is string)) {
				endMarker = TT.EOF;
			}
			// Line 1: ( / TopExpr)
			switch ((TT) LA0) {
			case EOF:
			case TT.Comma:
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
			// Line 59: ((TT.Comma|TT.Semicolon) ( / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					list.Add(e ?? MissingExpr());
					CheckEndMarker(ref endMarker, ref end);
					// Line 62: ( / TopExpr)
					switch ((TT) LA0) {
					case EOF:
					case TT.Comma:
					case TT.RBrace:
					case TT.RBrack:
					case TT.RParen:
					case TT.Semicolon:
						// line 62
						e = null;
						break;
					default:
						e = TopExpr();
						break;
					}
				} else
					break;
			}
			if ((e != null || end.Type() == TT.Comma)) {
				list.Add(e ?? MissingExpr());
			}
			return list;
		}
		public virtual IEnumerable<LNode> ExprListLazy(Holder<TokenType> endMarker)
		{
			TT la0;
			LNode e = default(LNode);
			Token end = default(Token);
			// line 68
			if ((LT0.Value is string)) {
				endMarker = TT.EOF;
			}
			// Line 1: ( / TopExpr)
			la0 = (TT) LA0;
			if (la0 == (TT) EOF || la0 == TT.Comma || la0 == TT.Semicolon) {
			} else
				e = TopExpr();
			// Line 70: ((TT.Comma|TT.Semicolon) ( / TopExpr))*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon) {
					end = MatchAny();
					// line 71
					yield
					return e ?? MissingExpr();
					CheckEndMarker(ref endMarker.Value, ref end);
					// Line 73: ( / TopExpr)
					la0 = (TT) LA0;
					if (la0 == (TT) EOF || la0 == TT.Comma || la0 == TT.Semicolon)
						// line 73
						e = null;
					else
						e = TopExpr();
				} else
					break;
			}
			// line 75
			if ((e != null || end.Type() == TT.Comma)) {
				yield return e ?? MissingExpr();
			}
		}
		protected LNode TopExpr()
		{
			TT la0, la1;
			Token at = default(Token);
			VList<LNode> attrs = default(VList<LNode>);
			LNode e = default(LNode);
			Token t = default(Token);
			// Line 91: (TT.At TT.LBrack ExprList TT.RBrack)?
			la0 = (TT) LA0;
			if (la0 == TT.At) {
				la1 = (TT) LA(1);
				if (la1 == TT.LBrack) {
					at = MatchAny();
					// line 92
					if (at.Type() == default(TT)) {
						ErrorSink.Write(Severity.Warning, LaIndexToSourcePos(0), "Attribute: expected '@['");
					}
					t = MatchAny();
					attrs = ExprList();
					Match((int) TT.RBrack);
				}
			}
			// Line 95: (Expr / TT.Id Expr (Particle)*)
			switch ((TT) LA0) {
			case TT.Assignment:
			case TT.BQOperator:
			case TT.Dot:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				e = Expr(StartStmt);
				break;
			case TT.Id:
				{
					switch ((TT) LA(1)) {
					case EOF:
					case TT.Assignment:
					case TT.BQOperator:
					case TT.Comma:
					case TT.Dot:
					case TT.LBrack:
					case TT.LParen:
					case TT.NormalOp:
					case TT.Not:
					case TT.PreOrSufOp:
					case TT.RBrace:
					case TT.RBrack:
					case TT.RParen:
					case TT.Semicolon:
						e = Expr(StartStmt);
						break;
					default:
						{
							var id = MatchAny();
							// line 98
							var args = VList<LNode>.Empty;
							args.Add(Expr(P.SuperExpr));
							// Line 100: (Particle)*
							for (;;) {
								switch ((TT) LA0) {
								case TT.At:
								case TT.Id:
								case TT.LBrace:
								case TT.LBrack:
								case TT.Literal:
								case TT.LParen:
								case TT.SpaceLParen:
									{
										// line 101
										if (((TT) LA0 == TT.LParen)) {
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
							// line 108
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
			if ((t.TypeInt != 0)) {
				e = e.WithRange(t.StartIndex, e.Range.EndIndex);
			}
			return e.PlusAttrs(attrs);
		}
		LNode Expr(Precedence context)
		{
			LNode e = default(LNode);
			Token t = default(Token);
			// line 123
			Precedence prec;
			e = PrefixExpr(context);
			// Line 127: greedy( &{context.CanParse(prec = InfixPrecedenceOf(LT($LI)))} (TT.Assignment|TT.BQOperator|TT.Dot|TT.NormalOp) Expr | &{context.CanParse(P.Primary)} FinishPrimaryExpr | &{context.CanParse(SuffixPrecedenceOf(LT($LI)))} TT.PreOrSufOp )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.Assignment:
				case TT.BQOperator:
				case TT.Dot:
				case TT.NormalOp:
					{
						if (context.CanParse(prec = InfixPrecedenceOf(LT(0)))) {
							// line 128
							if ((!prec.CanMixWith(context))) {
								Error(0, "Operator '{0}' is not allowed in this context. Add parentheses to clarify the code's meaning.", LT0.Value);
							}
							t = MatchAny();
							var rhs = Expr(prec);
							// line 133
							e = F.Call((Symbol) t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex).SetStyle(NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				case TT.LBrack:
				case TT.LParen:
				case TT.Not:
					{
						if (context.CanParse(P.Primary))
							e = FinishPrimaryExpr(e);
						else
							goto stop;
					}
					break;
				case TT.PreOrSufOp:
					{
						if (context.CanParse(SuffixPrecedenceOf(LT(0)))) {
							t = MatchAny();
							// line 140
							e = F.Call(ToSuffixOpName((Symbol) t.Value), e, e.Range.StartIndex, t.EndIndex).SetStyle(NodeStyle.Operator);
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			// line 142
			return e;
		}
		LNode FinishPrimaryExpr(LNode e)
		{
			TT la0;
			VList<LNode> list = default(VList<LNode>);
			// Line 148: ( TT.LParen ExprList TT.RParen | TT.Not (TT.LParen ExprList TT.RParen / Expr) | TT.LBrack ExprList TT.RBrack )
			la0 = (TT) LA0;
			if (la0 == TT.LParen) {
				// line 148
				var endMarker = default(TokenType);
				Skip();
				list = ExprList(ref endMarker);
				var c = Match((int) TT.RParen);
				// line 151
				e = MarkCall(F.Call(e, list, e.Range.StartIndex, c.EndIndex));
				if ((endMarker == TT.Semicolon)) {
					e.Style = NodeStyle.Statement | NodeStyle.Alternate;
				}
			} else if (la0 == TT.Not) {
				Skip();
				// line 156
				var args = new VList<LNode> { 
					e
				};
				int endIndex;
				// Line 157: (TT.LParen ExprList TT.RParen / Expr)
				la0 = (TT) LA0;
				if (la0 == TT.LParen) {
					Skip();
					args = ExprList(args);
					var c = Match((int) TT.RParen);
					// line 157
					endIndex = c.EndIndex;
				} else {
					var T = Expr(P.Primary);
					// line 158
					args.Add(T);
					endIndex = T.Range.EndIndex;
				}
				// line 160
				e = F.Call(S.Of, args, e.Range.StartIndex, endIndex).SetStyle(NodeStyle.Operator);
			} else {
				// line 162
				var args = new VList<LNode> { 
					e
				};
				Match((int) TT.LBrack);
				args = ExprList(args);
				var c = Match((int) TT.RBrack);
				// line 164
				e = F.Call(S.IndexBracks, args, e.Range.StartIndex, c.EndIndex).SetStyle(NodeStyle.Operator);
			}
			// line 166
			return e;
		}
		LNode PrefixExpr(Precedence context)
		{
			LNode e = default(LNode);
			LNode result = default(LNode);
			Token t = default(Token);
			// Line 171: ((TT.Assignment|TT.BQOperator|TT.Dot|TT.NormalOp|TT.Not|TT.PrefixOp|TT.PreOrSufOp) Expr | Particle)
			switch ((TT) LA0) {
			case TT.Assignment:
			case TT.BQOperator:
			case TT.Dot:
			case TT.NormalOp:
			case TT.Not:
			case TT.PrefixOp:
			case TT.PreOrSufOp:
				{
					t = MatchAny();
					e = Expr(PrefixPrecedenceOf(t));
					// line 173
					result = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex).SetStyle(NodeStyle.Operator);
				}
				break;
			default:
				result = Particle();
				break;
			}
			return result;
		}
		LNode Particle()
		{
			TT la0;
			Token c = default(Token);
			Token o = default(Token);
			LNode result = default(LNode);
			TokenTree tree = default(TokenTree);
			// Line 186: ( TT.Id | TT.Literal | TT.At (TT.LBrack TokenTree TT.RBrack | TT.LBrace TokenTree TT.RBrace) | TT.LBrace StmtList TT.RBrace | TT.LBrack ExprList TT.RBrack | (TT.LParen|TT.SpaceLParen) ExprList TT.RParen )
			switch ((TT) LA0) {
			case TT.Id:
				{
					var id = MatchAny();
					// line 187
					result = F.Id(id).SetStyle(id.Style);
				}
				break;
			case TT.Literal:
				{
					var lit = MatchAny();
					// line 189
					result = F.Literal(lit).SetStyle(lit.Style);
				}
				break;
			case TT.At:
				{
					o = MatchAny();
					// Line 192: (TT.LBrack TokenTree TT.RBrack | TT.LBrace TokenTree TT.RBrace)
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
					// line 194
					result = F.Literal(tree, o.StartIndex, c.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					o = MatchAny();
					var list = StmtList();
					c = Match((int) TT.RBrace);
					// line 197
					result = F.Braces(list, o.StartIndex, c.EndIndex).SetStyle(NodeStyle.Statement);
				}
				break;
			case TT.LBrack:
				{
					o = MatchAny();
					var list = ExprList();
					c = Match((int) TT.RBrack);
					// line 199
					result = F.Call(S.Array, list, o.StartIndex, c.EndIndex).SetStyle(NodeStyle.Expression);
				}
				break;
			case TT.LParen:
			case TT.SpaceLParen:
				{
					// line 201
					var endMarker = default(TT);
					o = MatchAny();
					// line 202
					var hasAttrList = (TT) LA0 == TT.LBrack || (TT) LA0 == TT.At;
					var list = ExprList(ref endMarker);
					c = Match((int) TT.RParen);
					// line 205
					if ((endMarker == TT.Semicolon || list.Count != 1)) {
						result = F.Call(S.Tuple, list, o.StartIndex, c.EndIndex);
						if ((endMarker == TT.Comma)) {
							var msg = "Tuples require ';' as a separator.";
							if ((o.Type() == TT.SpaceLParen)) {
								msg += " If a function call was intended, remove the space(s) before '('.";
							}
							ErrorSink.Write(Severity.Error, list[0].Range.End, msg);
						}
					} else {
						result = hasAttrList ? list[0] : F.InParens(list[0], o.StartIndex, c.EndIndex);
					}
				}
				break;
			default:
				{
					// line 219
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
			// Line 228: nongreedy((TT.LBrace|TT.LBrack|TT.LParen|TT.SpaceLParen) TokenTree (TT.RBrace|TT.RBrack|TT.RParen) / ~(EOF))*
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
				case TT.SpaceLParen:
					{
						la1 = (TT) LA(1);
						if (la1 != (TT) EOF) {
							var open = MatchAny();
							got_TokenTree = TokenTree();
							// line 230
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
