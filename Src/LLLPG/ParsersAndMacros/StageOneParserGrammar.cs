// Generated from StageOneParserGrammar.ecs by LLLPG custom tool. LLLPG version: 1.0.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
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
	internal partial class StageOneParser : BaseParser<Token>
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
			var a = GateExpr();
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Alt) {
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
			la0 = LA0;
			if (la0 == TT.Default || la0 == TT.Error)
				altType = MatchAny();
			var a = SeqExpr();
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
			for (;;) {
				switch (LA0) {
				case TT.And:
				case TT.AndNot:
				case TT.Greedy:
				case TT.Id:
				case TT.InvertSet:
				case TT.LBrace:
				case TT.LBrack:
				case TT.LParen:
				case TT.Minus:
				case TT.Nongreedy:
				case TT.Not:
				case TT.Number:
				case TT.OtherLit:
				case TT.String:
					{
						seq.Add(LoopExpr());
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
			la0 = LA0;
			if (la0 == TT.Greedy || la0 == TT.Nongreedy) {
				var loopMod = MatchAny();
				a = AssignExpr();
				a = F.Call((Symbol) loopMod.Value, a, loopMod.StartIndex, a.Range.EndIndex);
			} else
				a = AssignExpr();
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
			var a = PrefixExpr();
			la0 = LA0;
			if (la0 == TT.Assignment || la0 == TT.HostOperator) {
				var op = MatchAny();
				var b = AssignExpr();
				Infix(ref a, (Symbol) op.Value, b);
			}
			return a;
		}
		LNode PrefixExpr()
		{
			switch (LA0) {
			case TT.InvertSet:
				{
					var op = MatchAny();
					var r = PrefixExpr();
					return F.Call(S.NotBits, r, op.StartIndex, r.Range.EndIndex);
				}
			case TT.And:
				{
					var op = MatchAny();
					var r = PrefixExprOrBraces();
					return F.Call(S.AndBits, r, op.StartIndex, r.Range.EndIndex);
				}
			case TT.AndNot:
			case TT.Not:
				{
					var op = MatchAny();
					var r = PrefixExprOrBraces();
					return F.Call(_AndNot, r, op.StartIndex, r.Range.EndIndex);
				}
			default:
				{
					var r = RangeExpr();
					return r;
				}
			}
		}
		LNode PrefixExprOrBraces()
		{
			TT la0;
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
			la0 = LA0;
			if (la0 == TT.Minus) {
				Skip();
				var e = PrimaryExpr();
				return F.Call(S._Negate, e);
			} else {
				var a = Atom();
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
					var rb = Match((int) TT.LBrack);
					Check(Try_Atom_Test0(0), "(TT.Star|TT.QMark)");
					e = ParseParens(lb, rb.EndIndex);
				}
				break;
			default:
				{
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(InputPosition, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
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
