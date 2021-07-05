// Generated from ParserSourceTest.ecs by LeMP custom tool. LeMP version: 30.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax.Lexing;	// for LexerSource, ISimpleToken<int>
using Loyc.Syntax;	// for ParserSource<Token>
using Loyc.MiniTest;

namespace Loyc.Syntax.Tests
{
	using TT = CalcTokenType;

	[TestFixture] 
	public class ParserSourceTests_Calculator : Assert
	{
		[Test] 
		public void SimpleTests()
		{
			AreEqual(2, Calculator.Calculate("2"));
			AreEqual(25, Calculator.Calculate("25"));
			AreEqual(2.5, Calculator.Calculate("2.5"));
			AreEqual(0.25, Calculator.Calculate(".25"));
			AreEqual(5, Calculator.Calculate("x=5"));
		}
		[Test] 
		public void MoreTests()
		{
			AreEqual(5, Calculator.Calculate("2+3"));
			AreEqual(5, Calculator.Calculate(" 2+3 "));
			AreEqual(26, Calculator.Calculate("2*3 + 4*5"));
			AreEqual(0.125, Calculator.Calculate("2/4 - 3/8"));
			AreEqual(25, Calculator.Calculate("5 * ( 2 + 3 )"));
			AreEqual(25, Calculator.Calculate("5(5)"));
			AreEqual(25, Calculator.Calculate("5(2+3)"));
			AreEqual(25, Calculator.Calculate("5(2+3)"));
		}
		[Test] 
		public void SumTest()
		{
			AreEqual(15, Calculator.Calculate("1;2;3;4;5 +"));
			AreEqual(14, Calculator.Calculate("1*1; 2*2; 3*3 +"));
			AreEqual(150, Calculator.Calculate("( 1;2;3;4 +)*( 1;2;3;4;5 +) +"));
		}
	}

	public partial class Calculator
	{
		public Dictionary<string, double> Vars = new Dictionary<string, double>();

		public ParserSource<Token> Src { get; set; }

		public Calculator(ParserSource<Token> src	// LLLPG API
		) {
			Src = src;
		}

		public static double Calculate(string input)
		{
			var lexer = new CalculatorLexer(input);
			Token EOF = new Token((int) TT.EOF, input.Length, 0);
			var parser = new Calculator(
			  new ParserSource<Token>(lexer, EOF, lexer.SourceFile));

			return parser.Expr();
		}

		static double Do(double left, Token op, double right)
		{
			switch ((TT) op.TypeInt) {
			case TT.Add: return left + right;
			case TT.Sub: return left - right;
			case TT.Mul: return left * right;
			case TT.Div: return left / right;
			case TT.Semicolon: return right;
			}
			return double.NaN;	// unreachable
		}

		private double Atom()
		{
			TT la0;
			double got_Atom = default(double);
			double result = default(double);
			// Line 103: ( TT.Id | TT.Num | TT.LParen Expr TT.RParen )
			la0 = (TT) Src.LA0;
			if (la0 == TT.Id) {
				var t = Src.MatchAny();
				// line 103
				result = Vars[(string) t.Value];
			} else if (la0 == TT.Num) {
				var t = Src.MatchAny();
				// line 104
				result = (double) t.Value;
			} else if (la0 == TT.LParen) {
				Src.Skip();
				result = Expr();
				Src.Match((int) TT.RParen);
			} else {
				// line 106
				result = double.NaN;
				Src.Error(0, "Expected identifer, number, or (parens)");
			}
			// Line 109: greedy(TT.Exp Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Exp) {
					Src.Skip();
					got_Atom = Atom();
					// line 109
					result = System.Math.Pow(result, got_Atom);
				} else
					break;
			}
			return result;
		}

		private bool Scan_Atom()
		{
			TT la0;
			// Line 103: ( TT.Id | TT.Num | TT.LParen Expr TT.RParen )
			la0 = (TT) Src.LA0;
			if (la0 == TT.Id)
				Src.Skip();
			else if (la0 == TT.Num)
				Src.Skip();
			else if (la0 == TT.LParen) {
				Src.Skip();
				if (!Scan_Expr())
					return false;
				if (!Src.TryMatch((int) TT.RParen))
					return false;
			} else
				return false;
			// Line 109: greedy(TT.Exp Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Exp) {
					Src.Skip();
					if (!Scan_Atom())
						return false;
				} else
					break;
			}
			return true;
		}

		private double Term()
		{
			TT la0;
			var result = Atom();
			// Line 114: (Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id || la0 == TT.LParen || la0 == TT.Num) {
					var rest = Atom();
					// line 114
					result *= rest;
				} else
					break;
			}
			// line 115
			return result;
		}
		private bool Scan_Term()
		{
			TT la0;
			if (!Scan_Atom())
				return false;
			// Line 114: (Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id || la0 == TT.LParen || la0 == TT.Num){
					if (!Scan_Atom())
						return false;}
				else
					break;
			}
			return true;
		}

		double PrefixExpr()
		{
			TT la0;
			// Line 118: (TT.Sub Term | Term)
			la0 = (TT) Src.LA0;
			if (la0 == TT.Sub) {
				Src.Skip();
				var r = Term();
				// line 118
				return -r;
			} else {
				var r = Term();
				// line 119
				return r;
			}
		}
		bool Scan_PrefixExpr()
		{
			TT la0;
			// Line 118: (TT.Sub Term | Term)
			la0 = (TT) Src.LA0;
			if (la0 == TT.Sub) {
				Src.Skip();
				if (!Scan_Term())
					return false;
			} else if (!Scan_Term())
				return false;
			return true;
		}

		double MulExpr()
		{
			TT la0;
			var result = PrefixExpr();
			// Line 123: ((TT.Div|TT.Mul) PrefixExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Div || la0 == TT.Mul) {
					var op = Src.MatchAny();
					var rhs = PrefixExpr();
					// line 123
					result = Do(result, op, rhs);
				} else
					break;
			}
			// line 124
			return result;
		}
		bool Scan_MulExpr()
		{
			TT la0;
			if (!Scan_PrefixExpr())
				return false;
			// Line 123: ((TT.Div|TT.Mul) PrefixExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Div || la0 == TT.Mul) {
					Src.Skip();
					if (!Scan_PrefixExpr())
						return false;
				} else
					break;
			}
			return true;
		}

		double AddExpr()
		{
			TT la0;
			var result = MulExpr();
			// Line 128: ((TT.Add|TT.Sub) MulExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Add) {
					switch ((TT) Src.LA(1)) {
					case TT.Id: case TT.LParen: case TT.Num: case TT.Sub:
						goto match1;
					default:
						goto stop;
					}
				} else if (la0 == TT.Sub)
					goto match1;
				else
					goto stop;
			match1:
				{
					var op = Src.MatchAny();
					var rhs = MulExpr();
					// line 128
					result = Do(result, op, rhs);
				}
			}
		stop:;
			// line 129
			return result;
		}
		bool Scan_AddExpr()
		{
			TT la0;
			if (!Scan_MulExpr())
				return false;
			// Line 128: ((TT.Add|TT.Sub) MulExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Add) {
					switch ((TT) Src.LA(1)) {
					case TT.Id: case TT.LParen: case TT.Num: case TT.Sub:
						goto match1;
					default:
						goto stop;
					}
				} else if (la0 == TT.Sub)
					goto match1;
				else
					goto stop;
			match1:
				{
					Src.Skip();
					if (!Scan_MulExpr())
						return false;
				}
			}
		stop:;
			return true;
		}

		double AssignExpr()
		{
			TT la0, la1;
			double result = default(double);
			// Line 133: (TT.Id TT.Assign AssignExpr | AddExpr)
			la0 = (TT) Src.LA0;
			if (la0 == TT.Id) {
				la1 = (TT) Src.LA(1);
				if (la1 == TT.Assign) {
					var t = Src.MatchAny();
					Src.Skip();
					result = AssignExpr();
					// line 133
					Vars[t.Value.ToString()] = result;
				} else
					result = AddExpr();
			} else
				result = AddExpr();
			return result;
		}
		bool Scan_AssignExpr()
		{
			TT la0, la1;
			// Line 133: (TT.Id TT.Assign AssignExpr | AddExpr)
			do {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id) {
					la1 = (TT) Src.LA(1);
					if (la1 == TT.Assign) {
						Src.Skip();
						Src.Skip();
						if (!Scan_AssignExpr())
							return false;
					} else
						goto matchAddExpr;
				} else
					goto matchAddExpr;
				break;
			matchAddExpr:
				{
					if (!Scan_AddExpr())
						return false;
				}
			} while (false);
			return true;
		}

		double ExprSequence()
		{
			TT la0;
			double result = default(double);
			result = AssignExpr();
			// Line 137: (TT.Semicolon AssignExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Semicolon) {
					Src.Skip();
					result = AssignExpr();
				} else
					break;
			}
			return result;
		}
		bool Scan_ExprSequence()
		{
			TT la0;
			if (!Scan_AssignExpr())
				return false;
			// Line 137: (TT.Semicolon AssignExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Semicolon) {
					Src.Skip();
					if (!Scan_AssignExpr())
						return false;
				} else
					break;
			}
			return true;
		}

		double Expr()
		{
			TT la0;
			double n = default(double);
			double result = default(double);
			// Line 142: (&(ExprSequence TT.Add) AssignExpr (TT.Semicolon AssignExpr)* TT.Add / ExprSequence)
			do {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id) {
					if (Try_Expr_Test0(0)) {
						switch ((TT) Src.LA(1)) {
						case TT.Add: case TT.Assign: case TT.Div: case TT.Exp:
						case TT.Id: case TT.LParen: case TT.Mul: case TT.Num:
						case TT.Semicolon: case TT.Sub:
							goto match1;
						default:
							result = ExprSequence();
							break;
						}
					} else
						result = ExprSequence();
				} else if (la0 == TT.Sub) {
					if (Try_Expr_Test0(0))
						goto match1;
					else
						result = ExprSequence();
				} else if (la0 == TT.Num) {
					if (Try_Expr_Test0(0)) {
						switch ((TT) Src.LA(1)) {
						case TT.Add: case TT.Div: case TT.Exp: case TT.Id:
						case TT.LParen: case TT.Mul: case TT.Num: case TT.Semicolon:
						case TT.Sub:
							goto match1;
						default:
							result = ExprSequence();
							break;
						}
					} else
						result = ExprSequence();
				} else {
					if (Try_Expr_Test0(0))
						goto match1;
					else
						result = ExprSequence();
				}
				break;
			match1:
				{
					result = AssignExpr();
					// Line 143: (TT.Semicolon AssignExpr)*
					for (;;) {
						la0 = (TT) Src.LA0;
						if (la0 == TT.Semicolon) {
							Src.Skip();
							n = AssignExpr();
							// line 143
							result += n;
						} else
							break;
					}
					Src.Match((int) TT.Add);
				}
			} while (false);
			return result;
		}
		bool Scan_Expr()
		{
			TT la0;
			// Line 142: (&(ExprSequence TT.Add) AssignExpr (TT.Semicolon AssignExpr)* TT.Add / ExprSequence)
			do {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id) {
					if (Try_Expr_Test0(0)) {
						switch ((TT) Src.LA(1)) {
						case TT.Add: case TT.Assign: case TT.Div: case TT.Exp:
						case TT.Id: case TT.LParen: case TT.Mul: case TT.Num:
						case TT.Semicolon: case TT.Sub:
							goto match1;
						default:
							goto matchExprSequence;
						}
					} else
						goto matchExprSequence;
				} else if (la0 == TT.Sub) {
					if (Try_Expr_Test0(0))
						goto match1;
					else
						goto matchExprSequence;
				} else if (la0 == TT.Num) {
					if (Try_Expr_Test0(0)) {
						switch ((TT) Src.LA(1)) {
						case TT.Add: case TT.Div: case TT.Exp: case TT.Id:
						case TT.LParen: case TT.Mul: case TT.Num: case TT.Semicolon:
						case TT.Sub:
							goto match1;
						default:
							goto matchExprSequence;
						}
					} else
						goto matchExprSequence;
				} else {
					if (Try_Expr_Test0(0))
						goto match1;
					else
						goto matchExprSequence;
				}
			match1:
				{
					if (!Scan_AssignExpr())
						return false;
					// Line 143: (TT.Semicolon AssignExpr)*
					for (;;) {
						la0 = (TT) Src.LA0;
						if (la0 == TT.Semicolon) {
							Src.Skip();
							if (!Scan_AssignExpr())
								return false;
						} else
							break;
					}
					if (!Src.TryMatch((int) TT.Add))
						return false;
				}
				break;
			matchExprSequence:
				{
					if (!Scan_ExprSequence())
						return false;
				}
			} while (false);
			return true;
		}

		private bool Try_Expr_Test0(int lookaheadAmt) {
			using (new ParserSource<Token>.SavePosition(Src, lookaheadAmt))
				return Expr_Test0();
		}
		private bool Expr_Test0()
		{
			if (!Scan_ExprSequence())
				return false;
			if (!Src.TryMatch((int) TT.Add))
				return false;
			return true;
		}
	}
}	// end namespace