// Generated from ParserSourceTest.ecs by LeMP custom tool. LeMP version: 1.7.3.0
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
using Loyc.Syntax.Lexing;
using Loyc.Syntax;
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
		public Dictionary<string,double> Vars = new Dictionary<string,double>();
		public ParserSource<Token> Src
		{
			get;
			set;
		}
		public static double Calculate(string input)
		{
			var lexer = new CalculatorLexer(input);
			Token EOF = new Token((int) TT.EOF, input.Length, 0);
			var parser = new Calculator { 
				Src = new ParserSource<Token>(lexer, EOF, lexer.SourceFile)
			};
			return parser.Expr();
		}
		static double Do(double left, Token op, double right)
		{
			switch ((TT) op.TypeInt) {
			case TT.Add:
				return left + right;
			case TT.Sub:
				return left - right;
			case TT.Mul:
				return left * right;
			case TT.Div:
				return left / right;
			case TT.Semicolon:
				return right;
			}
			return double.NaN;
		}
		double Atom()
		{
			TT la0, la1;
			double got_Atom = default(double);
			double result = default(double);
			// Line 101: ( TT.Id | TT.Num | TT.LParen Expr TT.RParen )
			la0 = (TT) Src.LA0;
			if (la0 == TT.Id) {
				var t = Src.MatchAny();
				#line 101 "ParserSourceTest.ecs"
				result = Vars[(string) t.Value];
				#line default
			} else if (la0 == TT.Num) {
				var t = Src.MatchAny();
				#line 102 "ParserSourceTest.ecs"
				result = (double) t.Value;
				#line default
			} else if (la0 == TT.LParen) {
				Src.Skip();
				result = Expr();
				Src.Match((int) TT.RParen);
			} else {
				#line 104 "ParserSourceTest.ecs"
				result = double.NaN;
				#line 104 "ParserSourceTest.ecs"
				Src.Error(0, "Expected identifer, number, or (parens)");
				#line default
			}
			// Line 107: greedy(TT.Exp Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Exp) {
					la1 = (TT) Src.LA(1);
					if (la1 == TT.Id || la1 == TT.LParen || la1 == TT.Num) {
						Src.Skip();
						got_Atom = Atom();
						#line 107 "ParserSourceTest.ecs"
						result = System.Math.Pow(result, got_Atom);
						#line default
					} else
						break;
				} else
					break;
			}
			return result;
		}
		bool Scan_Atom()
		{
			TT la0, la1;
			// Line 101: ( TT.Id | TT.Num | TT.LParen Expr TT.RParen )
			la0 = (TT) Src.LA0;
			if (la0 == TT.Id)
				{if (!Src.TryMatch((int) TT.Id))
					return false;}
			else if (la0 == TT.Num)
				{if (!Src.TryMatch((int) TT.Num))
					return false;}
			else if (la0 == TT.LParen) {
				if (!Src.TryMatch((int) TT.LParen))
					return false;
				if (!Scan_Expr())
					return false;
				if (!Src.TryMatch((int) TT.RParen))
					return false;
			} else {
			}
			// Line 107: greedy(TT.Exp Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Exp) {
					la1 = (TT) Src.LA(1);
					if (la1 == TT.Id || la1 == TT.LParen || la1 == TT.Num) {
						if (!Src.TryMatch((int) TT.Exp))
							return false;
						if (!Scan_Atom())
							return false;
					} else
						break;
				} else
					break;
			}
			return true;
		}
		double Term()
		{
			TT la0;
			var result = Atom();
			// Line 112: (Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id || la0 == TT.LParen || la0 == TT.Num) {
					var rest = Atom();
					#line 112 "ParserSourceTest.ecs"
					result *= rest;
					#line default
				} else
					break;
			}
			#line 113 "ParserSourceTest.ecs"
			return result;
			#line default
		}
		bool Scan_Term()
		{
			TT la0;
			if (!Scan_Atom())
				return false;
			// Line 112: (Atom)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id || la0 == TT.LParen || la0 == TT.Num)
					{if (!Scan_Atom())
						return false;}
				else
					break;
			}
			return true;
		}
		double PrefixExpr()
		{
			TT la0;
			// Line 116: (TT.Sub Term | Term)
			la0 = (TT) Src.LA0;
			if (la0 == TT.Sub) {
				Src.Skip();
				var r = Term();
				#line 116 "ParserSourceTest.ecs"
				return -r;
				#line default
			} else {
				var r = Term();
				#line 117 "ParserSourceTest.ecs"
				return r;
				#line default
			}
		}
		bool Scan_PrefixExpr()
		{
			TT la0;
			// Line 116: (TT.Sub Term | Term)
			la0 = (TT) Src.LA0;
			if (la0 == TT.Sub) {
				if (!Src.TryMatch((int) TT.Sub))
					return false;
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
			// Line 121: ((TT.Div|TT.Mul) PrefixExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Div || la0 == TT.Mul) {
					var op = Src.MatchAny();
					var rhs = PrefixExpr();
					#line 121 "ParserSourceTest.ecs"
					result = Do(result, op, rhs);
					#line default
				} else
					break;
			}
			#line 122 "ParserSourceTest.ecs"
			return result;
			#line default
		}
		bool Scan_MulExpr()
		{
			TT la0;
			if (!Scan_PrefixExpr())
				return false;
			// Line 121: ((TT.Div|TT.Mul) PrefixExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Div || la0 == TT.Mul) {
					if (!Src.TryMatch((int) TT.Div, (int) TT.Mul))
						return false;
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
			// Line 126: ((TT.Add|TT.Sub) MulExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Add) {
					switch ((TT) Src.LA(1)) {
					case TT.Id:
					case TT.LParen:
					case TT.Num:
					case TT.Sub:
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
					#line 126 "ParserSourceTest.ecs"
					result = Do(result, op, rhs);
					#line default
				}
			}
		stop:;
			#line 127 "ParserSourceTest.ecs"
			return result;
			#line default
		}
		bool Scan_AddExpr()
		{
			TT la0;
			if (!Scan_MulExpr())
				return false;
			// Line 126: ((TT.Add|TT.Sub) MulExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Add) {
					switch ((TT) Src.LA(1)) {
					case TT.Id:
					case TT.LParen:
					case TT.Num:
					case TT.Sub:
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
					if (!Src.TryMatch((int) TT.Add, (int) TT.Sub))
						return false;
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
			// Line 131: (TT.Id TT.Assign AssignExpr | AddExpr)
			la0 = (TT) Src.LA0;
			if (la0 == TT.Id) {
				la1 = (TT) Src.LA(1);
				if (la1 == TT.Assign) {
					var t = Src.MatchAny();
					Src.Skip();
					result = AssignExpr();
					#line 131 "ParserSourceTest.ecs"
					Vars[t.Value.ToString()] = result;
					#line default
				} else
					result = AddExpr();
			} else
				result = AddExpr();
			return result;
		}
		bool Scan_AssignExpr()
		{
			TT la0, la1;
			// Line 131: (TT.Id TT.Assign AssignExpr | AddExpr)
			do {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Id) {
					la1 = (TT) Src.LA(1);
					if (la1 == TT.Assign) {
						if (!Src.TryMatch((int) TT.Id))
							return false;
						if (!Src.TryMatch((int) TT.Assign))
							return false;
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
			// Line 135: (TT.Semicolon AssignExpr)*
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
			// Line 135: (TT.Semicolon AssignExpr)*
			for (;;) {
				la0 = (TT) Src.LA0;
				if (la0 == TT.Semicolon) {
					if (!Src.TryMatch((int) TT.Semicolon))
						return false;
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
			// Line 140: (&(ExprSequence TT.Add) AssignExpr (TT.Semicolon AssignExpr)* TT.Add / ExprSequence)
			if (Try_Expr_Test0(0)) {
				switch ((TT) Src.LA(1)) {
				case TT.Add:
				case TT.Assign:
				case TT.Div:
				case TT.Exp:
				case TT.Id:
				case TT.LParen:
				case TT.Mul:
				case TT.Num:
				case TT.Semicolon:
				case TT.Sub:
					{
						result = AssignExpr();
						// Line 141: (TT.Semicolon AssignExpr)*
						for (;;) {
							la0 = (TT) Src.LA0;
							if (la0 == TT.Semicolon) {
								Src.Skip();
								n = AssignExpr();
								#line 141 "ParserSourceTest.ecs"
								result += n;
								#line default
							} else
								break;
						}
						Src.Match((int) TT.Add);
					}
					break;
				default:
					result = ExprSequence();
					break;
				}
			} else
				result = ExprSequence();
			return result;
		}
		bool Scan_Expr()
		{
			TT la0;
			// Line 140: (&(ExprSequence TT.Add) AssignExpr (TT.Semicolon AssignExpr)* TT.Add / ExprSequence)
			do {
				if (Try_Expr_Test0(0)) {
					switch ((TT) Src.LA(1)) {
					case TT.Add:
					case TT.Assign:
					case TT.Div:
					case TT.Exp:
					case TT.Id:
					case TT.LParen:
					case TT.Mul:
					case TT.Num:
					case TT.Semicolon:
					case TT.Sub:
						{
							if (!Scan_AssignExpr())
								return false;
							// Line 141: (TT.Semicolon AssignExpr)*
							for (;;) {
								la0 = (TT) Src.LA0;
								if (la0 == TT.Semicolon) {
									if (!Src.TryMatch((int) TT.Semicolon))
										return false;
									if (!Scan_AssignExpr())
										return false;
								} else
									break;
							}
							if (!Src.TryMatch((int) TT.Add))
								return false;
						}
						break;
					default:
						goto matchExprSequence;
					}
				} else
					goto matchExprSequence;
				break;
			matchExprSequence:
				{
					if (!Scan_ExprSequence())
						return false;
				}
			} while (false);
			return true;
		}
		private bool Try_Expr_Test0(int lookaheadAmt)
		{
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
}
