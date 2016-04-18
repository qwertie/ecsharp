// Generated from StageOneParserGrammar.ecs by LeMP custom tool. LeMP version: 1.7.3.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
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
			var e = GrammarExpr();
			Match((int) EOF);
			#line 51 "StageOneParserGrammar.ecs"
			return e;
			#line default
		}
		protected LNode GrammarExpr()
		{
			TT la0;
			var a = SlashExpr();
			// Line 58: (TT.Alt SlashExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Alt) {
					var op = MatchAny();
					var b = SlashExpr();
					#line 58 "StageOneParserGrammar.ecs"
					Infix(ref a, (Symbol) op.Value, b);
					#line default
				} else
					break;
			}
			#line 59 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode SlashExpr()
		{
			TT la0;
			var a = GateExpr();
			// Line 63: (TT.Slash GateExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Slash) {
					var op = MatchAny();
					var b = GateExpr();
					#line 63 "StageOneParserGrammar.ecs"
					Infix(ref a, (Symbol) op.Value, b);
					#line default
				} else
					break;
			}
			#line 64 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode GateExpr()
		{
			TT la0;
			#line 67 "StageOneParserGrammar.ecs"
			Token? altType = null;
			#line default
			// Line 68: ((TT.Default|TT.Error))?
			la0 = (TT) LA0;
			if (la0 == TT.Default || la0 == TT.Error)
				altType = MatchAny();
			var a = SeqExpr();
			// Line 70: (TT.Arrow GateExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Arrow) {
				var op = MatchAny();
				var b = GateExpr();
				#line 70 "StageOneParserGrammar.ecs"
				Infix(ref a, (Symbol) op.Value, b);
				#line default
			}
			#line 72 "StageOneParserGrammar.ecs"
			if (altType != null)
				a = F.Call((Symbol) altType.Value.Value, a, altType.Value.StartIndex, altType.Value.EndIndex);
			#line 74 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode SeqExpr()
		{
			TT la0;
			#line 78 "StageOneParserGrammar.ecs"
			var seq = LNode.List();
			#line default
			// Line 79: (LoopExpr (TT.Comma)?)*
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
						// Line 79: (TT.Comma)?
						la0 = (TT) LA0;
						if (la0 == TT.Comma)
							Skip();
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			#line 81 "StageOneParserGrammar.ecs"
			if (seq.Count == 1)
				return seq[0];
			else if (seq.IsEmpty)
				return F.Tuple();
			#line 83 "StageOneParserGrammar.ecs"
			return F.Tuple(seq, seq[0].Range.StartIndex, seq.Last.Range.EndIndex);
			#line default
		}
		LNode LoopExpr()
		{
			TT la0;
			#line 87 "StageOneParserGrammar.ecs"
			LNode a;
			#line default
			// Line 88: ((TT.Greedy|TT.Nongreedy) AssignExpr | AssignExpr)
			la0 = (TT) LA0;
			if (la0 == TT.Greedy || la0 == TT.Nongreedy) {
				var loopMod = MatchAny();
				a = AssignExpr();
				#line 89 "StageOneParserGrammar.ecs"
				a = F.Call((Symbol) loopMod.Value, a, loopMod.StartIndex, a.Range.EndIndex);
				#line default
			} else
				a = AssignExpr();
			// Line 92: ( TT.Star | TT.Plus | TT.QMark )?
			la0 = (TT) LA0;
			if (la0 == TT.Star) {
				var op = MatchAny();
				#line 92 "StageOneParserGrammar.ecs"
				a = F.Call(_SufStar, a, a.Range.StartIndex, op.EndIndex);
				#line default
			} else if (la0 == TT.Plus) {
				var op = MatchAny();
				#line 93 "StageOneParserGrammar.ecs"
				a = F.Call(_SufPlus, a, a.Range.StartIndex, op.EndIndex);
				#line default
			} else if (la0 == TT.QMark) {
				var op = MatchAny();
				#line 94 "StageOneParserGrammar.ecs"
				a = F.Call(_SufOpt, a, a.Range.StartIndex, op.EndIndex);
				#line default
			}
			#line 96 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode AssignExpr()
		{
			TT la0;
			#line 100 "StageOneParserGrammar.ecs"
			Token op;
			#line default
			var a = PrefixExpr();
			// Line 102: (TT.Bang)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Bang) {
					op = MatchAny();
					#line 102 "StageOneParserGrammar.ecs"
					a = F.Call(_SufBang, a, a.Range.StartIndex, op.EndIndex);
					#line default
				} else
					break;
			}
			// Line 103: ((TT.Assignment|TT.Colon|TT.HostOperator) AssignExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Assignment || la0 == TT.Colon || la0 == TT.HostOperator) {
				op = MatchAny();
				var b = AssignExpr();
				#line 103 "StageOneParserGrammar.ecs"
				Infix(ref a, (Symbol) op.Value, b);
				#line default
			}
			#line 104 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode PrefixExpr()
		{
			TT la0;
			// Line 108: ( TT.InvertSet PrefixExpr | TT.And PrefixExprOrBraces | TT.AndNot PrefixExprOrBraces | RangeExpr )
			la0 = (TT) LA0;
			if (la0 == TT.InvertSet) {
				var op = MatchAny();
				var r = PrefixExpr();
				#line 108 "StageOneParserGrammar.ecs"
				return F.Call(S.NotBits, r, op.StartIndex, r.Range.EndIndex);
				#line default
			} else if (la0 == TT.And) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				#line 109 "StageOneParserGrammar.ecs"
				return F.Call(S.AndBits, r, op.StartIndex, r.Range.EndIndex);
				#line default
			} else if (la0 == TT.AndNot) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				#line 110 "StageOneParserGrammar.ecs"
				return F.Call(_AndNot, r, op.StartIndex, r.Range.EndIndex);
				#line default
			} else {
				var r = RangeExpr();
				#line 111 "StageOneParserGrammar.ecs"
				return r;
				#line default
			}
		}
		LNode PrefixExprOrBraces()
		{
			TT la0;
			// Line 114: (TT.LBrace TT.RBrace / PrefixExpr)
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				#line 114 "StageOneParserGrammar.ecs"
				return ParseHostBraces(lb, rb.EndIndex, ParsingMode.Exprs);
				#line default
			} else {
				var e = PrefixExpr();
				#line 115 "StageOneParserGrammar.ecs"
				return e;
				#line default
			}
		}
		LNode RangeExpr()
		{
			TT la0;
			var a = PrimaryExpr();
			// Line 120: (TT.DotDotDot PrimaryExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.DotDotDot) {
				var op = MatchAny();
				var b = PrimaryExpr();
				#line 120 "StageOneParserGrammar.ecs"
				Infix(ref a, (Symbol) op.Value, b);
				#line default
			}
			#line 121 "StageOneParserGrammar.ecs"
			return a;
			#line default
		}
		LNode PrimaryExpr()
		{
			TT la0, la1;
			Token lp = default(Token);
			Token rp = default(Token);
			Token tok__Any = default(Token);
			Token tok__Id = default(Token);
			// Line 125: ( TT.Minus PrimaryExpr | TT.Any TT.Id (TT.In PrimaryExpr / ) | Atom greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} (TT.LParen TT.RParen | TT.LBrack TT.RBrack))* )
			la0 = (TT) LA0;
			if (la0 == TT.Minus) {
				Skip();
				var e = PrimaryExpr();
				#line 125 "StageOneParserGrammar.ecs"
				return F.Call(S._Negate, e);
				#line default
			} else if (la0 == TT.Any) {
				#line 126 "StageOneParserGrammar.ecs"
				LNode e, id;
				#line default
				tok__Any = MatchAny();
				tok__Id = Match((int) TT.Id);
				#line 127 "StageOneParserGrammar.ecs"
				id = F.Id(tok__Id);
				#line default
				// Line 128: (TT.In PrimaryExpr / )
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
						#line 129 "StageOneParserGrammar.ecs"
						e = id;
						#line default
					}
				} while (false);
				e = F.Call(_Any, id, e, tok__Any.StartIndex, e.Range.EndIndex);
				return e;
			} else {
				var a = Atom();
				// Line 135: greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} (TT.LParen TT.RParen | TT.LBrack TT.RBrack))*
				for (;;) {
					la0 = (TT) LA0;
					if (la0 == TT.Dot) {
						var op = MatchAny();
						var b = Atom();
						#line 135 "StageOneParserGrammar.ecs"
						Infix(ref a, (Symbol) op.Value, b);
						#line default
					} else if (la0 == TT.LParen) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							la1 = (TT) LA(1);
							if (la1 == TT.RParen)
								goto match2_a;
							else
								break;
						} else
							break;
					} else if (la0 == TT.LBrack) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							la1 = (TT) LA(1);
							if (la1 == TT.RBrack)
								goto match2_a;
							else
								break;
						} else
							break;
					} else
						break;
					continue;
				match2_a:
					{
						// Line 138: (TT.LParen TT.RParen | TT.LBrack TT.RBrack)
						la0 = (TT) LA0;
						if (la0 == TT.LParen) {
							lp = MatchAny();
							rp = Match((int) TT.RParen);
						} else {
							lp = Match((int) TT.LBrack);
							rp = Match((int) TT.RBrack);
						}
						#line 141 "StageOneParserGrammar.ecs"
						a = F.Call(a, ParseHostArgList(lp, ParsingMode.Exprs), a.Range.StartIndex, rp.EndIndex);
						#line default
					}
				}
				#line 143 "StageOneParserGrammar.ecs"
				return a;
				#line default
			}
		}
		LNode Atom()
		{
			#line 147 "StageOneParserGrammar.ecs"
			LNode e;
			#line default
			// Line 148: ( (TT.Id|TT.In) | TT.Literal | TT.LParen TT.RParen | TT.LBrace TT.RBrace | TT.LBrack TT.RBrack &((TT.QMark|TT.Star)) )
			switch ((TT) LA0) {
			case TT.Id:
			case TT.In:
				{
					var t = MatchAny();
					#line 149 "StageOneParserGrammar.ecs"
					e = F.Id(t);
					#line default
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					#line 151 "StageOneParserGrammar.ecs"
					e = F.Literal(t);
					#line default
				}
				break;
			case TT.LParen:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					#line 152 "StageOneParserGrammar.ecs"
					e = ParseParens(lp, rp.EndIndex);
					#line default
				}
				break;
			case TT.LBrace:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					#line 153 "StageOneParserGrammar.ecs"
					e = ParseHostBraces(lb, rb.EndIndex, ParsingMode.Stmts);
					#line default
				}
				break;
			case TT.LBrack:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrack);
					Check(Try_Atom_Test0(0), "(TT.QMark|TT.Star)");
					#line 155 "StageOneParserGrammar.ecs"
					e = ParseParens(lb, rb.EndIndex);
					#line default
				}
				break;
			default:
				{
					#line 157 "StageOneParserGrammar.ecs"
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
					#line default
				}
				break;
			}
			#line 161 "StageOneParserGrammar.ecs"
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
