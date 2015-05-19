// Generated from StageOneParserGrammar.ecs by LLLPG custom tool. LLLPG version: 1.3.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --verbose             Allow verbose messages (shown as 'warnings')
// --no-out-header       Suppress this message
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Syntax.Lexing;
namespace Loyc.LLParserGenerator
{
	using TT = TokenType;
	using S = CodeSymbols;
	internal partial class StageOneParser
	{
		static readonly TT EOF = TT.EOF;
		void Infix(ref LNode a, Symbol op, LNode b)
		{
			a = F.Call(op, a, b, a.Range.StartIndex, b.Range.EndIndex);
		}
		public LNode Parse()
		{
			var e = Expr();
			Match((int) EOF);
			return e;
		}
		LNode Expr()
		{
			TT la0;
			var a = SlashExpr();
			// Line 57: (TT.Alt SlashExpr)*
			 for (;;) {
				la0 = LA0;
				if (la0 == TT.Alt) {
					var op = MatchAny();
					var b = SlashExpr();
					Infix(ref a, (Symbol) op.Value, b);
				} else
					break;
			}
			return a;
		}
		LNode SlashExpr()
		{
			TT la0;
			var a = GateExpr();
			// Line 62: (TT.Slash GateExpr)*
			 for (;;) {
				la0 = LA0;
				if (la0 == TT.Slash) {
					var op = MatchAny();
					var b = GateExpr();
					Infix(ref a, (Symbol) op.Value, b);
				} else
					break;
			}
			return a;
		}
		LNode GateExpr()
		{
			TT la0;
			Token? altType = null;
			// Line 67: ((TT.Default|TT.Error))?
			la0 = LA0;
			if (la0 == TT.Default || la0 == TT.Error)
				altType = MatchAny();
			var a = SeqExpr();
			// Line 69: (TT.Arrow GateExpr)?
			la0 = LA0;
			if (la0 == TT.Arrow) {
				var op = MatchAny();
				var b = GateExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			if (altType != null)
				a = F.Call((Symbol) altType.Value.Value, a, altType.Value.StartIndex, altType.Value.EndIndex);
			return a;
		}
		LNode SeqExpr()
		{
			TT la0;
			var seq = RVList<LNode>.Empty;
			// Line 78: (LoopExpr (TT.Separator)?)*
			 for (;;) {
				switch (LA0) {
				case TT.And:
				case TT.AndNot:
				case TT.Any:
				case TT.Greedy:
				case TT.Id:
				case TT.InvertSet:
				case TT.LBrace:
				case TT.LBrack:
				case TT.LParen:
				case TT.Minus:
				case TT.Nongreedy:
				case TT.Number:
				case TT.OtherLit:
				case TT.String:
					{
						seq.Add(LoopExpr());
						// Line 78: (TT.Separator)?
						la0 = LA0;
						if (la0 == TT.Separator)
							Skip();
					}
					break;
				default:
					goto stop;
				}
			}
		 stop:;
			if (seq.Count == 1)
				return seq[0];
			else if (seq.IsEmpty)
				return F.Tuple();
			return F.Tuple(seq, seq[0].Range.StartIndex, seq.Last.Range.EndIndex);
		}
		LNode LoopExpr()
		{
			TT la0;
			LNode a;
			// Line 87: ((TT.Greedy|TT.Nongreedy) AssignExpr | AssignExpr)
			la0 = LA0;
			if (la0 == TT.Greedy || la0 == TT.Nongreedy) {
				var loopMod = MatchAny();
				a = AssignExpr();
				a = F.Call((Symbol) loopMod.Value, a, loopMod.StartIndex, a.Range.EndIndex);
			} else
				a = AssignExpr();
			// Line 91: ( TT.Star | TT.Plus | TT.QMark )?
			la0 = LA0;
			if (la0 == TT.Star) {
				var op = MatchAny();
				a = F.Call(_SufStar, a, a.Range.StartIndex, op.EndIndex);
			} else if (la0 == TT.Plus) {
				var op = MatchAny();
				a = F.Call(_SufPlus, a, a.Range.StartIndex, op.EndIndex);
			} else if (la0 == TT.QMark) {
				var op = MatchAny();
				a = F.Call(_SufOpt, a, a.Range.StartIndex, op.EndIndex);
			}
			return a;
		}
		LNode AssignExpr()
		{
			TT la0;
			Token op;
			var a = PrefixExpr();
			// Line 101: ((TT.Assignment|TT.HostOperator) AssignExpr)?
			la0 = LA0;
			if (la0 == TT.Assignment || la0 == TT.HostOperator) {
				op = MatchAny();
				var b = AssignExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			// Line 102: (TT.Bang)*
			 for (;;) {
				la0 = LA0;
				if (la0 == TT.Bang) {
					op = MatchAny();
					a = F.Call(_SufBang, a, a.Range.StartIndex, op.EndIndex);
				} else
					break;
			}
			return a;
		}
		LNode PrefixExpr()
		{
			TT la0;
			// Line 107: ( TT.InvertSet PrefixExpr | TT.And PrefixExprOrBraces | TT.AndNot PrefixExprOrBraces | RangeExpr )
			la0 = LA0;
			if (la0 == TT.InvertSet) {
				var op = MatchAny();
				var r = PrefixExpr();
				return F.Call(S.NotBits, r, op.StartIndex, r.Range.EndIndex);
			} else if (la0 == TT.And) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				return F.Call(S.AndBits, r, op.StartIndex, r.Range.EndIndex);
			} else if (la0 == TT.AndNot) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				return F.Call(_AndNot, r, op.StartIndex, r.Range.EndIndex);
			} else {
				var r = RangeExpr();
				return r;
			}
		}
		LNode PrefixExprOrBraces()
		{
			TT la0;
			// Line 113: (TT.LBrace TT.RBrace / PrefixExpr)
			la0 = LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				return ParseBraces(lb, rb.EndIndex, true);
			} else {
				var e = PrefixExpr();
				return e;
			}
		}
		LNode RangeExpr()
		{
			TT la0;
			var a = PrimaryExpr();
			// Line 119: (TT.DotDot PrimaryExpr)?
			la0 = LA0;
			if (la0 == TT.DotDot) {
				var op = MatchAny();
				var b = PrimaryExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			return a;
		}
		LNode PrimaryExpr()
		{
			TT la0, la1;
			Token tok__Any = default(Token);
			Token tok__Id = default(Token);
			// Line 124: ( TT.Minus PrimaryExpr | TT.Any TT.Id (TT.In PrimaryExpr | ) | Atom greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} TT.LParen TT.RParen)* )
			la0 = LA0;
			if (la0 == TT.Minus) {
				Skip();
				var e = PrimaryExpr();
				return F.Call(S._Negate, e);
			} else if (la0 == TT.Any) {
				LNode e, id;
				tok__Any = MatchAny();
				tok__Id = Match((int) TT.Id);
				id = F.Id(tok__Id);
				// Line 127: (TT.In PrimaryExpr | )
				la0 = LA0;
				if (la0 == TT.In) {
					Skip();
					e = PrimaryExpr();
				} else
					e = id;
				e = F.Call(_Any, id, e, tok__Any.StartIndex, e.Range.EndIndex);
				return e;
			} else {
				var a = Atom();
				// Line 134: greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} TT.LParen TT.RParen)*
				 for (;;) {
					la0 = LA0;
					if (la0 == TT.Dot) {
						var op = MatchAny();
						var b = Atom();
						Infix(ref a, (Symbol) op.Value, b);
					} else if (la0 == TT.LParen) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							la1 = LA(1);
							if (la1 == TT.RParen) {
								var lp = MatchAny();
								var rp = MatchAny();
								a = F.Call(a, ParseArgList(lp), a.Range.StartIndex, rp.EndIndex);
							} else
								break;
						} else
							break;
					} else
						break;
				}
				return a;
			}
		}
		LNode Atom()
		{
			LNode e;
			// Line 145: ( TT.Id | (TT.Number|TT.OtherLit|TT.String) | TT.LParen TT.RParen | TT.LBrace TT.RBrace | TT.LBrack TT.RBrack &((TT.QMark|TT.Star)) )
			 switch (LA0) {
			case TT.Id:
				{
					var t = MatchAny();
					e = F.Id(t);
				}
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.String:
				{
					var t = MatchAny();
					e = F.Literal(t);
				}
				break;
			case TT.LParen:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					e = ParseParens(lp, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					e = ParseBraces(lb, rb.EndIndex, false);
				}
				break;
			case TT.LBrack:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrack);
					Check(Try_Atom_Test0(0), "(TT.QMark|TT.Star)");
					e = ParseParens(lb, rb.EndIndex);
				}
				break;
			default:
				{
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
				}
				break;
			}
			return e;
		}
		private bool Try_Atom_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Atom_Test0();
		}
		private bool Atom_Test0()
		{
			if (!TryMatch((int) TT.QMark, (int) TT.Star))
				return false;
			return true;
		}
	}
}
