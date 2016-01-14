// Generated from StageOneParserGrammar.ecs by LeMP custom tool. LLLPG version: 1.3.2.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
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
		void Infix(ref LNode a, Symbol op, LNode b)
		{
			a = F.Call(op, a, b, a.Range.StartIndex, b.Range.EndIndex);
		}
		public LNode Parse()
		{
			var e = Expr();
			Match((int) EOF);
			#line 52 "StageOneParserGrammar.ecs"
			return e;
			#line default
		}
		LNode Expr()
		{
			TT la0;
			var a = SlashExpr();
			// Line 72: (TT.Alt SlashExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Alt) {
					var op = MatchAny();
					var b = SlashExpr();
					#line 72 "StageOneParserGrammar.ecs"
					Infix(ref a, (Symbol) op.Value, b);
					#line default
				} else
					break;
			}
			#line 73 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode SlashExpr()
		{
			TT la0;
			var a = GateExpr();
			// Line 77: (TT.Slash GateExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Slash) {
					var op = MatchAny();
					var b = GateExpr();
					#line 77 "StageOneParserGrammar.ecs"
					Infix(ref a, (Symbol) op.Value, b);
					#line default
				} else
					break;
			}
			#line 78 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode GateExpr()
		{
			TT la0;
			#line 81 "StageOneParserGrammar.ecs"
			Token? altType = null;
			#line default
			// Line 82: ((TT.Default|TT.Error))?
			la0 = (TT) LA0;
			if (la0 == TT.Default || la0 == TT.Error)
				altType = MatchAny();
			var a = SeqExpr();
			// Line 84: (TT.Arrow GateExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Arrow) {
				var op = MatchAny();
				var b = GateExpr();
				#line 84 "StageOneParserGrammar.ecs"
				Infix(ref a, (Symbol) op.Value, b);
				#line default
			}
			#line 86 "StageOneParserGrammar.ecs"
			if (altType != null)
				a = F.Call((Symbol) altType.Value.Value, a, altType.Value.StartIndex, altType.Value.EndIndex);
			#line 88 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode SeqExpr()
		{
			TT la0;
			#line 92 "StageOneParserGrammar.ecs"
			var seq = RVList<LNode>.Empty;
			#line default
			// Line 93: (LoopExpr (TT.Separator)?)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.And:
				case TT.AndNot:
				case TT.Any:
				case TT.Greedy:
				case TT.Id:
				case TT.In:
				case TT.InvertSet:
				case TT.LBrace:
				case TT.LBrack:
				case TT.Literal:
				case TT.LParen:
				case TT.Minus:
				case TT.Nongreedy:
					{
						seq.Add(LoopExpr());
						// Line 93: (TT.Separator)?
						la0 = (TT) LA0;
						if (la0 == TT.Separator)
							Skip();
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			#line 95 "StageOneParserGrammar.ecs"
			if (seq.Count == 1)
				return seq[0];
			else if (seq.IsEmpty)
				return F.Tuple();
			#line 97 "StageOneParserGrammar.ecs"
			return F.Tuple(seq, seq[0].Range.StartIndex, seq.Last.Range.EndIndex);
			#line default
		}
		LNode LoopExpr()
		{
			TT la0;
			#line 101 "StageOneParserGrammar.ecs"
			LNode a;
			#line default
			// Line 102: ((TT.Greedy|TT.Nongreedy) AssignExpr | AssignExpr)
			la0 = (TT) LA0;
			if (la0 == TT.Greedy || la0 == TT.Nongreedy) {
				var loopMod = MatchAny();
				a = AssignExpr();
				#line 103 "StageOneParserGrammar.ecs"
				a = F.Call((Symbol) loopMod.Value, a, loopMod.StartIndex, a.Range.EndIndex);
				#line default
			} else
				a = AssignExpr();
			// Line 106: ( TT.Star | TT.Plus | TT.QMark )?
			la0 = (TT) LA0;
			if (la0 == TT.Star) {
				var op = MatchAny();
				#line 106 "StageOneParserGrammar.ecs"
				a = F.Call(_SufStar, a, a.Range.StartIndex, op.EndIndex);
				#line default
			} else if (la0 == TT.Plus) {
				var op = MatchAny();
				#line 107 "StageOneParserGrammar.ecs"
				a = F.Call(_SufPlus, a, a.Range.StartIndex, op.EndIndex);
				#line default
			} else if (la0 == TT.QMark) {
				var op = MatchAny();
				#line 108 "StageOneParserGrammar.ecs"
				a = F.Call(_SufOpt, a, a.Range.StartIndex, op.EndIndex);
				#line default
			}
			#line 110 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode AssignExpr()
		{
			TT la0;
			#line 114 "StageOneParserGrammar.ecs"
			Token op;
			#line default
			var a = PrefixExpr();
			// Line 116: (TT.Bang)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Bang) {
					op = MatchAny();
					#line 116 "StageOneParserGrammar.ecs"
					a = F.Call(_SufBang, a, a.Range.StartIndex, op.EndIndex);
					#line default
				} else
					break;
			}
			// Line 117: ((TT.Assignment|TT.HostOperator) AssignExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Assignment || la0 == TT.HostOperator) {
				op = MatchAny();
				var b = AssignExpr();
				#line 117 "StageOneParserGrammar.ecs"
				Infix(ref a, (Symbol) op.Value, b);
				#line default
			}
			#line 118 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode PrefixExpr()
		{
			TT la0;
			// Line 122: ( TT.InvertSet PrefixExpr | TT.And PrefixExprOrBraces | TT.AndNot PrefixExprOrBraces | RangeExpr )
			la0 = (TT) LA0;
			if (la0 == TT.InvertSet) {
				var op = MatchAny();
				var r = PrefixExpr();
				#line 122 "StageOneParserGrammar.ecs"
				return F.Call(S.NotBits, r, op.StartIndex, r.Range.EndIndex);
				#line default
			} else if (la0 == TT.And) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				#line 123 "StageOneParserGrammar.ecs"
				return F.Call(S.AndBits, r, op.StartIndex, r.Range.EndIndex);
				#line default
			} else if (la0 == TT.AndNot) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				#line 124 "StageOneParserGrammar.ecs"
				return F.Call(_AndNot, r, op.StartIndex, r.Range.EndIndex);
				#line default
			} else {
				var r = RangeExpr();
				#line 125 "StageOneParserGrammar.ecs"
				return r;
				#line default
			}
		}
		LNode PrefixExprOrBraces()
		{
			TT la0;
			// Line 128: (TT.LBrace TT.RBrace / PrefixExpr)
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				#line 128 "StageOneParserGrammar.ecs"
				return ParseBraces(lb, rb.EndIndex, true);
				#line default
			} else {
				var e = PrefixExpr();
				#line 129 "StageOneParserGrammar.ecs"
				return e;
				#line default
			}
		}
		LNode RangeExpr()
		{
			TT la0;
			var a = PrimaryExpr();
			// Line 134: (TT.DotDot PrimaryExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.DotDot) {
				var op = MatchAny();
				var b = PrimaryExpr();
				#line 134 "StageOneParserGrammar.ecs"
				Infix(ref a, (Symbol) op.Value, b);
				#line default
			}
			#line 135 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode PrimaryExpr()
		{
			TT la0, la1;
			Token tok__Any = default(Token);
			Token tok__Id = default(Token);
			// Line 139: ( TT.Minus PrimaryExpr | TT.Any TT.Id (TT.In PrimaryExpr / ) | Atom greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} TT.LParen TT.RParen)* )
			la0 = (TT) LA0;
			if (la0 == TT.Minus) {
				Skip();
				var e = PrimaryExpr();
				#line 139 "StageOneParserGrammar.ecs"
				return F.Call(S._Negate, e);
				#line default
			} else if (la0 == TT.Any) {
				#line 140 "StageOneParserGrammar.ecs"
				LNode e, id;
				#line default
				tok__Any = MatchAny();
				tok__Id = Match((int) TT.Id);
				#line 141 "StageOneParserGrammar.ecs"
				id = F.Id(tok__Id);
				#line default
				// Line 142: (TT.In PrimaryExpr / )
				do {
					la0 = (TT) LA0;
					if (la0 == TT.In) {
						switch ((TT) LA(1)) {
						case TT.Any:
						case TT.Id:
						case TT.In:
						case TT.LBrace:
						case TT.LBrack:
						case TT.Literal:
						case TT.LParen:
						case TT.Minus:
							{
								Skip();
								e = PrimaryExpr();
							}
							break;
						default:
							goto match2;
						}
					} else
						goto match2;
					break;
				match2:
					{
						#line 143 "StageOneParserGrammar.ecs"
						e = id;
						#line default
					}
				} while (false);
				e = F.Call(_Any, id, e, tok__Any.StartIndex, e.Range.EndIndex);
				return e;
			} else {
				var a = Atom();
				// Line 149: greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} TT.LParen TT.RParen)*
				for (;;) {
					la0 = (TT) LA0;
					if (la0 == TT.Dot) {
						var op = MatchAny();
						var b = Atom();
						#line 149 "StageOneParserGrammar.ecs"
						Infix(ref a, (Symbol) op.Value, b);
						#line default
					} else if (la0 == TT.LParen) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							la1 = (TT) LA(1);
							if (la1 == TT.RParen) {
								var lp = MatchAny();
								var rp = MatchAny();
								#line 153 "StageOneParserGrammar.ecs"
								a = F.Call(a, ParseArgList(lp), a.Range.StartIndex, rp.EndIndex);
								#line default
							} else
								break;
						} else
							break;
					} else
						break;
				}
				#line 155 "StageOneParserGrammar.ecs"
				return a;
				#line default
			}
		}
		LNode Atom()
		{
			#line 159 "StageOneParserGrammar.ecs"
			LNode e;
			#line default
			// Line 160: ( (TT.Id|TT.In) | TT.Literal | TT.LParen TT.RParen | TT.LBrace TT.RBrace | TT.LBrack TT.RBrack &((TT.QMark|TT.Star)) )
			switch ((TT) LA0) {
			case TT.Id:
			case TT.In:
				{
					var t = MatchAny();
					#line 161 "StageOneParserGrammar.ecs"
					e = F.Id(t);
					#line default
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					#line 163 "StageOneParserGrammar.ecs"
					e = F.Literal(t);
					#line default
				}
				break;
			case TT.LParen:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					#line 164 "StageOneParserGrammar.ecs"
					e = ParseParens(lp, rp.EndIndex);
					#line default
				}
				break;
			case TT.LBrace:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					#line 165 "StageOneParserGrammar.ecs"
					e = ParseBraces(lb, rb.EndIndex, false);
					#line default
				}
				break;
			case TT.LBrack:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrack);
					Check(Try_Atom_Test0(0), "(TT.QMark|TT.Star)");
					#line 167 "StageOneParserGrammar.ecs"
					e = ParseParens(lb, rb.EndIndex);
					#line default
				}
				break;
			default:
				{
					#line 169 "StageOneParserGrammar.ecs"
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
					#line default
				}
				break;
			}
			#line 173 "StageOneParserGrammar.ecs"
			return e;
			#line default
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
