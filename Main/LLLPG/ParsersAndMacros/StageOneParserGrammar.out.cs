// Generated from StageOneParserGrammar.ecs by LeMP custom tool. LeMP version: 2.7.0.0
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

		// Replaces 'a' with the LNode for the infix expression a`op`b (e.g. a | b)
		void Infix(ref LNode a, Symbol op, LNode b) {
			a = F.Call(op, a, b, a.Range.StartIndex, b.Range.EndIndex);
		}
	
	
		public LNode Parse()
		{
			var e = GrammarExpr();
			Match((int) EOF);
			// line 51
			return e;
		}
	
	
		////////////////////////////////////////////////////////////////////
		// Top-level rule body expression: a | b | ...
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
					// line 58
					Infix(ref a, (Symbol) op.Value, b);
				} else
					break;
			}
			// line 59
			return a;
		}
	
		private LNode SlashExpr()
		{
			TT la0;
			var a = GateExpr();
			// Line 63: (TT.Slash GateExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Slash) {
					var op = MatchAny();
					var b = GateExpr();
					// line 63
					Infix(ref a, (Symbol) op.Value, b);
				} else
					break;
			}
			// line 64
			return a;
		}
	
		private LNode GateExpr()
		{
			TT la0;
			// line 67
			Token? altType = null;
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
				// line 70
				Infix(ref a, (Symbol) op.Value, b);
			}
			// line 72
			if (altType != null)
				a = F.Call((Symbol) altType.Value.Value, a, altType.Value.StartIndex, altType.Value.EndIndex);
			return a;
		}
	
		private LNode SeqExpr()
		{
			TT la0, la1;
			// line 78
			var seq = LNode.List();
			// Line 79: (LoopExpr (TT.Comma)?)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.And: case TT.AndNot: case TT.Greedy: case TT.InvertSet:
				case TT.Nongreedy:
					{
						switch ((TT) LA(1)) {
						case TT.And: case TT.AndNot: case TT.Any: case TT.Id:
						case TT.In: case TT.InvertSet: case TT.LBrace: case TT.LBrack:
						case TT.Literal: case TT.LParen: case TT.Minus:
							goto matchLoopExpr;
						default:
							goto stop;
						}
					}
				case TT.Minus:
					{
						switch ((TT) LA(1)) {
						case TT.Any: case TT.Id: case TT.In: case TT.LBrace:
						case TT.LBrack: case TT.Literal: case TT.LParen: case TT.Minus:
							goto matchLoopExpr;
						default:
							goto stop;
						}
					}
				case TT.Any:
					{
						la1 = (TT) LA(1);
						if (la1 == TT.Id)
							goto matchLoopExpr;
						else
							goto stop;
					}
				case TT.Id: case TT.In: case TT.Literal:
					goto matchLoopExpr;
				case TT.LParen:
					{
						la1 = (TT) LA(1);
						if (la1 == TT.RParen)
							goto matchLoopExpr;
						else
							goto stop;
					}
				case TT.LBrace:
					{
						la1 = (TT) LA(1);
						if (la1 == TT.RBrace)
							goto matchLoopExpr;
						else
							goto stop;
					}
				case TT.LBrack:
					{
						la1 = (TT) LA(1);
						if (la1 == TT.RBrack)
							goto matchLoopExpr;
						else
							goto stop;
					}
				default:
					goto stop;
				}
			matchLoopExpr:
				{
					seq.Add(LoopExpr());
					// Line 79: (TT.Comma)?
					la0 = (TT) LA0;
					if (la0 == TT.Comma)
						Skip();
				}
			}
		stop:;
			// line 81
			if (seq.Count == 1)
				return seq[0];
			else if (seq.IsEmpty)
				return F.Tuple();
			return F.Tuple(seq, seq[0].Range.StartIndex, seq.Last.Range.EndIndex);
		}
	
		private LNode LoopExpr()
		{
			TT la0;
			// line 87
			LNode a;
			// Line 88: ((TT.Greedy|TT.Nongreedy) AssignExpr | AssignExpr)
			la0 = (TT) LA0;
			if (la0 == TT.Greedy || la0 == TT.Nongreedy) {
				var loopMod = MatchAny();
				a = AssignExpr();
				// line 89
				a = F.Call((Symbol) loopMod.Value, a, loopMod.StartIndex, a.Range.EndIndex);
			} else
				a = AssignExpr();
			// Line 92: ( TT.Star | TT.Plus | TT.QMark )?
			la0 = (TT) LA0;
			if (la0 == TT.Star) {
				var op = MatchAny();
				// line 92
				a = F.Call(_SufStar, a, a.Range.StartIndex, op.EndIndex);
			} else if (la0 == TT.Plus) {
				var op = MatchAny();
				// line 93
				a = F.Call(_SufPlus, a, a.Range.StartIndex, op.EndIndex);
			} else if (la0 == TT.QMark) {
				var op = MatchAny();
				// line 94
				a = F.Call(_SufOpt, a, a.Range.StartIndex, op.EndIndex);
			}
			// line 96
			return a;
		}
	
	
		private LNode AssignExpr()
		{
			TT la0;
			// line 100
			Token op;
			var a = PrefixExpr();
			// Line 102: (TT.Bang)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Bang) {
					op = MatchAny();
					// line 102
					a = F.Call(_SufBang, a, a.Range.StartIndex, op.EndIndex);
				} else
					break;
			}
			// Line 103: ((TT.Assignment|TT.Colon|TT.HostOperator) AssignExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Assignment || la0 == TT.Colon || la0 == TT.HostOperator) {
				switch ((TT) LA(1)) {
				case TT.And: case TT.AndNot: case TT.Any: case TT.Id:
				case TT.In: case TT.InvertSet: case TT.LBrace: case TT.LBrack:
				case TT.Literal: case TT.LParen: case TT.Minus:
					{
						op = MatchAny();
						var b = AssignExpr();
						// line 103
						Infix(ref a, (Symbol) op.Value, b);
					}
					break;
				}
			}
			// line 104
			return a;
		}
	
	
		private LNode PrefixExpr()
		{
			TT la0;
			// Line 108: ( TT.InvertSet PrefixExpr | TT.And PrefixExprOrBraces | TT.AndNot PrefixExprOrBraces | RangeExpr )
			la0 = (TT) LA0;
			if (la0 == TT.InvertSet) {
				var op = MatchAny();
				var r = PrefixExpr();
				// line 108
				return F.Call(S.NotBits, r, op.StartIndex, r.Range.EndIndex);
			} else if (la0 == TT.And) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				// line 109
				return F.Call(S.AndBits, r, op.StartIndex, r.Range.EndIndex);
			} else if (la0 == TT.AndNot) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				// line 110
				return F.Call(_AndNot, r, op.StartIndex, r.Range.EndIndex);
			} else {
				var r = RangeExpr();
				// line 111
				return r;
			}
		}
	
		private LNode PrefixExprOrBraces()
		{
			TT la0;
			// Line 114: (TT.LBrace TT.RBrace / PrefixExpr)
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				// line 114
				return ParseHostBraces(lb, rb.EndIndex, ParsingMode.Expressions);
			} else {
				var e = PrefixExpr();
				// line 115
				return e;
			}
		}
	
	
		private LNode RangeExpr()
		{
			TT la0;
			var a = PrimaryExpr();
			// Line 120: (TT.DotDotDot PrimaryExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.DotDotDot) {
				switch ((TT) LA(1)) {
				case TT.Any: case TT.Id: case TT.In: case TT.LBrace:
				case TT.LBrack: case TT.Literal: case TT.LParen: case TT.Minus:
					{
						var op = MatchAny();
						var b = PrimaryExpr();
						// line 120
						Infix(ref a, (Symbol) op.Value, b);
					}
					break;
				}
			}
			// line 121
			return a;
		}
	
		static readonly HashSet<int> PrimaryExpr_set0 = NewSet((int) EOF, (int) TT.Alt, (int) TT.And, (int) TT.AndNot, (int) TT.Any, (int) TT.Arrow, (int) TT.Assignment, (int) TT.Bang, (int) TT.Colon, (int) TT.Comma, (int) TT.Default, (int) TT.DotDotDot, (int) TT.Error, (int) TT.Greedy, (int) TT.HostOperator, (int) TT.Id, (int) TT.In, (int) TT.InvertSet, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Minus, (int) TT.Nongreedy, (int) TT.Plus, (int) TT.QMark, (int) TT.Slash, (int) TT.Star);
	
		private LNode PrimaryExpr()
		{
			TT la0, la1;
			Token lp = default(Token);
			Token rp = default(Token);
			Token tok__Any = default(Token);
			Token tok__Id = default(Token);
			// Line 125: ( TT.Minus PrimaryExpr | TT.Any TT.Id (TT.In GateExpr / {..}) | Atom greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} (TT.LParen TT.RParen | TT.LBrack TT.RBrack))* )
			la0 = (TT) LA0;
			if (la0 == TT.Minus) {
				Skip();
				var e = PrimaryExpr();
				// line 125
				return F.Call(S._Negate, e);
			} else if (la0 == TT.Any) {
				// line 126
				LNode e, id;
				tok__Any = MatchAny();
				tok__Id = Match((int) TT.Id);
				// line 127
				id = F.Id(tok__Id);
				// Line 128: (TT.In GateExpr / {..})
				la0 = (TT) LA0;
				if (la0 == TT.In) {
					la1 = (TT) LA(1);
					if (PrimaryExpr_set0.Contains((int) la1)) {
						Skip();
						e = GateExpr();
					} else
						// line 129
						e = id;
				} else
					// line 129
					e = id;
				// line 130
				e = F.Call(_Any, id, e, tok__Any.StartIndex, e.Range.EndIndex);
				// line 131
				return e;
			} else {
				var a = Atom();
				// Line 135: greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} (TT.LParen TT.RParen | TT.LBrack TT.RBrack))*
				for (;;) {
					la0 = (TT) LA0;
					if (la0 == TT.Dot) {
						var op = MatchAny();
						var b = Atom();
						// line 135
						Infix(ref a, (Symbol) op.Value, b);
					} else if (la0 == TT.LParen) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							la1 = (TT) LA(1);
							if (la1 == TT.RParen)
								goto match2;
							else
								break;
						} else
							break;
					} else if (la0 == TT.LBrack) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							la1 = (TT) LA(1);
							if (la1 == TT.RBrack)
								goto match2;
							else
								break;
						} else
							break;
					} else
						break;
					continue;
				match2:
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
						// line 141
						a = F.Call(a, ParseHostCode(lp, ParsingMode.Expressions), a.Range.StartIndex, rp.EndIndex);
					}
				}
				// line 143
				return a;
			}
		}
	
	
		private LNode Atom()
		{
			// line 147
			LNode e;
			// Line 148: ( (TT.Id|TT.In) | TT.Literal | TT.LParen TT.RParen | TT.LBrace TT.RBrace | TT.LBrack TT.RBrack &((TT.QMark|TT.Star)) )
			switch ((TT) LA0) {
			case TT.Id: case TT.In:
				{
					var t = MatchAny();
					// line 149
					e = F.Id(t);
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					// line 151
					e = F.Literal(t);
				}
				break;
			case TT.LParen:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					// line 152
					e = ParseParens(lp, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					// line 153
					e = ParseHostBraces(lb, rb.EndIndex, ParsingMode.Statements);
				}
				break;
			case TT.LBrack:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrack);
					Check(Try_Atom_Test0(0), "Expected (TT.QMark|TT.Star)");
					// line 155
					e = ParseParens(lb, rb.EndIndex);
				}
				break;
			default:
				{
					// line 157
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
				}
				break;
			}
			// line 161
			return e;
		}
	
		private bool Try_Atom_Test0(int lookaheadAmt) {
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