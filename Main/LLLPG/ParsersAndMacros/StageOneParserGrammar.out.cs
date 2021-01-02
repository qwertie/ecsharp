// Generated from StageOneParserGrammar.ecs by LeMP custom tool. LeMP version: 2.9.0.0
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
	
		void Infix(ref LNode a, Token op, LNode b) => a = F.CallInfixOp(a, op, b);
	
		public LNode Parse()
		{
			var e = GrammarExpr();
			Match((int) EOF);
			// line 48
			return e;
		}
	
			////////////////////////////////////////////////////////////////////
	
		// Top-level rule body expression: a | b | ...
		protected LNode GrammarExpr()
		{
			TT la0;
			var a = SlashExpr();
			// Line 55: (TT.Alt SlashExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Alt) {
					var op = MatchAny();
					var b = SlashExpr();
					// line 55
					Infix(ref a, op, b);
				} else
					break;
			}
			// line 56
			return a;
		}
	
		private LNode SlashExpr()
		{
			TT la0;
			var a = GateExpr();
			// Line 60: (TT.Slash GateExpr)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Slash) {
					var op = MatchAny();
					var b = GateExpr();
					// line 60
					Infix(ref a, op, b);
				} else
					break;
			}
			// line 61
			return a;
		}
	
		private LNode GateExpr()
		{
			TT la0;
			// line 64
			Token? altType = null;
			// Line 65: ((TT.Default|TT.Error))?
			la0 = (TT) LA0;
			if (la0 == TT.Default || la0 == TT.Error)
				altType = MatchAny();
			var a = SeqExpr();
			// Line 67: (TT.Arrow GateExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Arrow) {
				var op = MatchAny();
				var b = GateExpr();
				// line 67
				Infix(ref a, op, b);
			}
			// line 69
			if (altType != null)
				a = F.CallPrefixOp(altType.Value, a);
			return a;
		}
	
		private LNode SeqExpr()
		{
			TT la0;
			// line 75
			var seq = LNode.List();
			// Line 76: (LoopExpr (TT.Comma)?)*
			for (;;) {
				switch ((TT) LA0) {
				case TT.And: case TT.AndNot: case TT.Any: case TT.Greedy:
				case TT.Id: case TT.In: case TT.InvertSet: case TT.LBrace:
				case TT.LBrack: case TT.Literal: case TT.LParen: case TT.Minus:
				case TT.Nongreedy: case TT.NonRecognizer: case TT.Recognizer:
					{
						seq.Add(LoopExpr());
						// Line 76: (TT.Comma)?
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
			// line 78
			if (seq.Count == 1)
				return seq[0];
			else if (seq.IsEmpty)
				return F.Tuple();
			return F.Tuple(seq, seq[0].Range.StartIndex, seq.Last.Range.EndIndex);
		}
	
		private LNode LoopExpr()
		{
			TT la0;
			// line 84
			LNode a;
			// Line 85: ((TT.Greedy|TT.Nongreedy|TT.NonRecognizer|TT.Recognizer) AssignExpr | AssignExpr)
			switch ((TT) LA0) {
			case TT.Greedy: case TT.Nongreedy: case TT.NonRecognizer: case TT.Recognizer:
				{
					var modifier = MatchAny();
					a = AssignExpr();
					// line 86
					a = F.CallPrefixOp(modifier, a, null, NodeStyle.Default);
				}
				break;
			default:
				a = AssignExpr();
				break;
			}
			// Line 89: ( TT.Star | TT.Plus | TT.QMark )?
			la0 = (TT) LA0;
			if (la0 == TT.Star) {
				var op = MatchAny();
				// line 89
				a = F.CallSuffixOp(a, _SufStar, op);
			} else if (la0 == TT.Plus) {
				var op = MatchAny();
				// line 90
				a = F.CallSuffixOp(a, _SufPlus, op);
			} else if (la0 == TT.QMark) {
				var op = MatchAny();
				// line 91
				a = F.CallSuffixOp(a, _SufOpt, op);
			}
			// line 93
			return a;
		}
	
		private LNode AssignExpr()
		{
			TT la0;
			// line 97
			Token op;
			var a = PrefixExpr();
			// Line 99: (TT.Bang)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.Bang) {
					op = MatchAny();
					// line 99
					a = F.Call(_SufBang, a, a.Range.StartIndex, op.EndIndex);
				} else
					break;
			}
			// Line 100: ((TT.Assignment|TT.Colon|TT.HostOperator) AssignExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.Assignment || la0 == TT.Colon || la0 == TT.HostOperator) {
				op = MatchAny();
				var b = AssignExpr();
				// line 100
				Infix(ref a, op, b);
			}
			// line 101
			return a;
		}
	
		private LNode PrefixExpr()
		{
			TT la0;
			// Line 105: ( TT.InvertSet PrefixExpr | TT.And PrefixExprOrBraces | TT.AndNot PrefixExprOrBraces | RangeExpr )
			la0 = (TT) LA0;
			if (la0 == TT.InvertSet) {
				var op = MatchAny();
				var r = PrefixExpr();
				// line 105
				return F.CallPrefixOp(op, r, S.NotBits);
			} else if (la0 == TT.And) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				// line 106
				return F.CallPrefixOp(op, r, S.AndBits);
			} else if (la0 == TT.AndNot) {
				var op = MatchAny();
				var r = PrefixExprOrBraces();
				// line 107
				return F.CallPrefixOp(op, r, _AndNot);
			} else {
				var r = RangeExpr();
				// line 108
				return r;
			}
		}
	
		private LNode PrefixExprOrBraces()
		{
			TT la0;
			// Line 111: (TT.LBrace TT.RBrace / PrefixExpr)
			la0 = (TT) LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				// line 111
				return ParseHostBraces(lb, rb.EndIndex, ParsingMode.Expressions);
			} else {
				var e = PrefixExpr();
				// line 112
				return e;
			}
		}
	
		private LNode RangeExpr()
		{
			TT la0;
			var a = PrimaryExpr();
			// Line 117: (TT.DotDotDot PrimaryExpr)?
			la0 = (TT) LA0;
			if (la0 == TT.DotDotDot) {
				var op = MatchAny();
				var b = PrimaryExpr();
				// line 117
				Infix(ref a, op, b);
			}
			// line 118
			return a;
		}
		static readonly HashSet<int> PrimaryExpr_set0 = NewSet((int) EOF, (int) TT.Alt, (int) TT.And, (int) TT.AndNot, (int) TT.Any, (int) TT.Arrow, (int) TT.Assignment, (int) TT.Bang, (int) TT.Colon, (int) TT.Comma, (int) TT.Default, (int) TT.DotDotDot, (int) TT.Error, (int) TT.Greedy, (int) TT.HostOperator, (int) TT.Id, (int) TT.In, (int) TT.InvertSet, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Minus, (int) TT.Nongreedy, (int) TT.NonRecognizer, (int) TT.Plus, (int) TT.QMark, (int) TT.Recognizer, (int) TT.Slash, (int) TT.Star);
	
		private LNode PrimaryExpr()
		{
			TT la0, la1;
			Token lit_dash = default(Token);
			Token lp = default(Token);
			Token rp = default(Token);
			Token tok__Any = default(Token);
			Token tok__Id = default(Token);
			// Line 122: ( TT.Minus PrimaryExpr | TT.Any TT.Id (TT.In GateExpr / {..}) | Atom greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} (TT.LParen TT.RParen | TT.LBrack TT.RBrack))* )
			la0 = (TT) LA0;
			if (la0 == TT.Minus) {
				lit_dash = MatchAny();
				var e = PrimaryExpr();
				// line 122
				return F.CallPrefixOp(lit_dash, e, S._Negate);
			} else if (la0 == TT.Any) {
				// line 123
				LNode e, id;
				tok__Any = MatchAny();
				tok__Id = Match((int) TT.Id);
				// line 124
				id = F.Id(tok__Id);
				// Line 125: (TT.In GateExpr / {..})
				la0 = (TT) LA0;
				if (la0 == TT.In) {
					la1 = (TT) LA(1);
					if (PrimaryExpr_set0.Contains((int) la1)) {
						Skip();
						e = GateExpr();
					} else
						// line 126
						e = id;
				} else
					// line 126
					e = id;
				// line 127
				e = F.CallPrefixOp(tok__Any, LNode.List(id, e));
				// line 128
				return e;
			} else {
				var a = Atom();
				// Line 132: greedy(TT.Dot Atom | &{a.Range.EndIndex == LT($LI).StartIndex} (TT.LParen TT.RParen | TT.LBrack TT.RBrack))*
				for (;;) {
					la0 = (TT) LA0;
					if (la0 == TT.Dot) {
						var op = MatchAny();
						var b = Atom();
						// line 132
						Infix(ref a, op, b);
					} else if (la0 == TT.LBrack || la0 == TT.LParen) {
						if (a.Range.EndIndex == LT(0).StartIndex) {
							// Line 135: (TT.LParen TT.RParen | TT.LBrack TT.RBrack)
							la0 = (TT) LA0;
							if (la0 == TT.LParen) {
								lp = MatchAny();
								rp = Match((int) TT.RParen);
							} else {
								lp = Match((int) TT.LBrack);
								rp = Match((int) TT.RBrack);
							}
							// line 138
							a = F.CallPrefix(a, ParseHostCode(lp, ParsingMode.Expressions), rp);
						} else
							break;
					} else
						break;
				}
				// line 140
				return a;
			}
		}
	
		private LNode Atom()
		{
			// line 144
			LNode e;
			// Line 145: ( (TT.Id|TT.In) | TT.Literal | TT.LParen TT.RParen | TT.LBrace TT.RBrace | TT.LBrack TT.RBrack &((TT.QMark|TT.Star)) )
			switch ((TT) LA0) {
			case TT.Id: case TT.In:
				{
					var t = MatchAny();
					// line 146
					e = F.Id(t);
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					// line 148
					e = F.Literal(t);
				}
				break;
			case TT.LParen:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					// line 149
					e = ParseParens(lp, rp.EndIndex);
				}
				break;
			case TT.LBrace:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrace);
					// line 150
					e = ParseHostBraces(lb, rb.EndIndex, ParsingMode.Statements);
				}
				break;
			case TT.LBrack:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrack);
					Check(Try_Atom_Test0(0), "Expected (TT.QMark|TT.Star)");
					// line 152
					e = ParseParens(lb, rb.EndIndex);
				}
				break;
			default:
				{
					// line 154
					e = F.Id(S.Missing, LT0.StartIndex, LT0.StartIndex);
					Error(0, "LLLPG: Expected an identifier, literal, or expression in parenthesis");
				}
				break;
			}
			// line 158
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