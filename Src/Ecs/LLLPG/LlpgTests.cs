using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;
using ecs;
using Loyc.CompilerCore;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;

	[TestFixture]
	public class LlpgTests : Assert
	{
		/// // rule a ==> #[ 'a' | 'A' ];
		/// // rule b ==> #[ 'b' | 'B' ];
		/// // public rule Foo ==> #[ a | b ];
		/// public void Foo()
		/// {
		///   var la0 = LA(0);
		///   if (la0 == 'a' || la0 == 'A')
		///     a();
		///   else
		///     b();
		/// }

		protected Symbol _(string symbol) { return GSymbol.Get(symbol); }
		protected static Alts Star(Pred contents) { return Pred.Star(contents); }
		protected static Alts NongreedyStar(Pred contents) { var star = Pred.Star(contents); star.Nongreedy = true; return star; }
		protected static Alts Opt(Pred contents) { return Pred.Opt(contents); }
		protected static Seq Plus(Pred contents) { return Pred.Plus(contents); }
		protected static Gate Gate(Pred predictor, Pred match) { return new Gate(null, predictor, match); }
		protected static TerminalPred R(char lo, char hi) { return Pred.Range(lo, hi); }
		protected static TerminalPred C(char ch) { return Pred.Char(ch); }
		protected static TerminalPred Cs(params char[] chars) { return Pred.Chars(chars); }
		protected static TerminalPred Set(string set) { return Pred.Set(set); }
		protected static TerminalPred Dot { get { return Set("[^]"); } }
		protected static Rule Rule(string name, Pred contents, bool isStartingRule = true, int k = 0) { return Pred.Rule(name, contents, isStartingRule, false, k); }
		protected static AndPred And(object test) { return Pred.And(test); }
		protected static AndPred AndNot(object test) { return Pred.AndNot(test); }

		protected LLParserGenerator _pg;
		protected NodeFactory NF = new NodeFactory(EmptySourceFile.Default);

		protected void CheckResult(Node result, string verbatim)
		{
			verbatim = verbatim.Replace("\r\n", "\n"); // verbatim strings include \r?!
			string from = "\n\t\t\t\t";
			if (verbatim.StartsWith("\n"))
			{
				int i;
				for (i = 1; verbatim[i] == '\t' || verbatim[i] == ' '; i++) { }
				from = verbatim.Substring(0, i);
				verbatim = verbatim.Substring(i);
			}
			verbatim = verbatim.Replace(from, "\n");

			AreEqual(verbatim, result.Print());
		}

		[SetUpAttribute]
		public void SetUp()
		{
			_pg = new LLParserGenerator();
		}

		[Test]
		public void SimpleMatching()
		{
			Rule Foo = Rule("Foo", 'x' + R('0', '9') + R('0', '9'));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("LlpgTests.cs"));
			CheckResult(result, @"
				public partial class FooClass
				{
					public void Foo()
					{
						Match('x');
						MatchRange('0', '9');
						MatchRange('0', '9');
					}
				}");
		}

		[Test]
		public void SimpleAltsNoLoop()
		{
			Rule a = Rule("a", C('a') | 'A');
			Rule b = Rule("b", C('b') | 'B');
			Rule Foo = Rule("Foo", a | b);
			_pg.AddRules(new[] { a, b, Foo });
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class FooClass
				{
					public void a()
					{
						Match('A', 'a');
					}
					public void b()
					{
						Match('B', 'b');
					}
					public void Foo()
					{
						int la0;
						la0 = LA(0);
						if (la0 == 'A' || la0 == 'a')
							a();
						else
							b();
					}
				}");
		}

		[Test]
		public void SimpleAltsOptStar()
		{
			Rule a = Rule("a", C('a') | 'A');
			Rule b = Rule("b", C('b') | 'B');
			// public rule Foo ==> #[ (a | b? 'c')* ];
			Rule Foo = Rule("Foo", Star(a | Opt(b) + 'c'));
			_pg.AddRules(new[] { a, b, Foo });
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class FooClass
				{
					public void a()
					{
						Match('A', 'a');
					}
					public void b()
					{
						Match('B', 'b');
					}
					public void Foo()
					{
						int la0;
						for (;;) {
							la0 = LA(0);
							if (la0 == 'A' || la0 == 'a')
								a();
							else if (la0 == 'B' || la0 >= 'b' && la0 <= 'c') {
								la0 = LA(0);
								if (la0 == 'B' || la0 == 'b')
									b();
								Match('c');
							} else
								break;
						}
					}
				}");
		}

		[Test]
		public void LL2Example1()
		{
			// public rule Foo ==> #[ 'a'..'z'+ | 'x' '0'..'9' '0'..'9' ];
			Rule Foo = Rule("Foo", Plus(R('a','z')) | 'x' + R('0','9') + R('0','9'));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class FooClass
				{
					public void Foo()
					{
						int la0, la1;
						la0 = LA(0);
						if (la0 == 'x') {
							la1 = LA(1);
							if (la1 == -1 || la1 >= 'a' && la1 <= 'z')
								goto match1;
							else {
								Match('x');
								MatchRange('0', '9');
								MatchRange('0', '9');
							}
						} else
							goto match1;
						goto done;
						match1:
						{
							MatchRange('a', 'z');
							for (;;) {
								la0 = LA(0);
								if (la0 >= 'a' && la0 <= 'z')
									MatchRange('a', 'z');
								else
									break;
							}
						}
						done:
					}
				}");
			// NOTE: MatchRange('a', 'z') should be simply Match()
		}
		[Test]
		public void LL2Example2()
		{
			// rule Foo ==> #[ (('a'|'A') 'A' | 'a'..'z' 'a'..'z')* ];
			Rule Foo = Rule("Foo", Star((C('a')|'A') + 'A' | R('a','z') + R('a','z')));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			CheckResult(result, @"
				public partial class Parser
				{
					public void Foo()
					{
						int la0, la1;
						for (;;) {
							la0 = LA(0);
							if (la0 == 'a') {
								la1 = LA(1);
								if (la1 == 'A')
									goto match1;
								else
									goto match2;
							} else if (la0 == 'A')
								goto match1;
							else if (la0 >= 'a' && la0 <= 'z')
								goto match2;
							else
								break;
							match1:
							{
								Match('A', 'a');
								Match('A');
							}
							goto done;
							match2:
							{
								MatchRange('a', 'z');
								MatchRange('a', 'z');
							}
							done:
						}
					}
				}");
		}

		[Test]
		public void LL2Example3()
		{
			// rule Foo ==> #[ (('a'|'A') 'A')* 'a'..'z' 'a'..'z' ];
			Rule Foo = Rule("Foo", Star(Set("[aA]") + 'A') + R('a','z') + R('a','z'));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			CheckResult(result, @"
				public partial class Parser
				{
					public void Foo()
					{
						int la0, la1;
						for (;;) {
							la0 = LA(0);
							if (la0 == 'a') {
								la1 = LA(1);
								if (la1 == 'A')
									goto match1;
								else
									break;
							} else if (la0 == 'A')
								goto match1;
							else
								break;
							match1:
							{
								Match('A', 'a');
								Match('A');
							}
						}
						MatchRange('a', 'z');
						MatchRange('a', 'z');
					}
				}");
		}


		[Test]
		public void MatchInvertedSet()
		{
			// public rule Except ==> #[ ~'a' ~('a'..'z') ];
			// public rule String ==> #[ '"' ~('"'|'\n')* '"' ];
			
			Rule Except = Rule("Except", Set("[^a]") + Set("[^a-z]"));
			Rule String = Rule("String", '"' + Star(Set("[^\"\n]")) + '"');
			_pg.AddRule(Except);
			_pg.AddRule(String);
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class Parser
				{
					public void Except()
					{
						MatchExcept('a');
						MatchExceptRange('a', 'z');
					}
					public void String()
					{
						int la0;
						Match('""');
						for (;;) {
							la0 = LA(0);
							if (!(la0 == -1 || la0 == '\n' || la0 == '""'))
								MatchExcept('\n', '""');
							else
								break;
						}
						Match('""');
					}
				}");
		}

		[Test]
		public void MatchComplexSet()
		{
			Rule Odd = Rule("Odd", Plus(Set("[\\--.13579a-z]")));
			_pg.AddRule(Odd);
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class Parser
				{
					static readonly IntSet Odd_set0 = IntSet.Parse(""[\\--.13579a-z]"");
					public void Odd()
					{
						int la0;
						Match(Odd_set0);
						for (;;) {
							la0 = LA(0);
							if (Odd_set0.Contains(la0))
								Match(Odd_set0);
							else
								break;
						}
					}
				}");
			// NOTE: the second Match(Odd_set0) should be simply Match()
		}

		public Pred Act(string pre, Pred pred, string post)
		{
			if (pre != null) pred.PreAction = NF.Symbol(pre);
			if (post != null) pred.PostAction = NF.Symbol(post);
			return pred;
		}
		[Test]
		public void ActionsTest()
		{
			// public rule Foo ==> #[
			//     { StartRule; }
			//     ( { BeforeA; } 'A' { AfterA; }
			//     | { BeforeSeq; } ('1' { After1; } { Before2; } '2' '3') { AfterSeq; }
			//     | { BeforeOpt; } ('?'? { AfterOpt; }) .
			//     )*
			//     { EndRule; }
			// ];
			// without actions: ('A' | ('1' '2' '3') | '?'? .)*
			Rule Foo = Rule("Foo",
				Act("StartRule",
					Star( Act("BeforeA", C('A'), "AfterA")
						| Act("BeforeSeq", Act(null, C('1'), "After1") + Act("Before2", C('2'), null) + '3', "AfterSeq")
						| Act("BeforeOpt", Opt(Act(null, C('?'), "AfterQMark")), "AfterOpt") + Dot), 
					"EndRule"));

			Foo.K = 1;
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class Parser
				{
					public void Foo()
					{
						int la0;
						StartRule;
						for (;;) {
							la0 = LA(0);
							if (la0 == 'A') {
								BeforeA;
								Match('A');
								AfterA;
							} else if (la0 == '1') {
								BeforeSeq;
								Match('1');
								After1;
								Before2;
								Match('2');
								Match('3');
								AfterSeq;
							} else if (la0 != -1) {
								BeforeOpt;
								la0 = LA(0);
								if (la0 == '?') {
									Match('?');
									AfterQMark;
								}
								AfterOpt;
								MatchExcept();
							} else
								break;
						}
						EndRule;
					}
				}");
		}

		[Test]
		public void SimpleNongreedyTest()
		{
			Rule String = Rule("String", '"' + NongreedyStar(Dot) + '"', false);
			Rule Token = Rule("Token", Star(String | Dot), true);
			_pg.AddRule(String);
			_pg.AddRule(Token);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			// The output is a little odd: instead of (la0 == '"' || la0 == -1) 
			// there are two "if" statements. This occurs because if la0 == '"', 
			// there is an ambiguity between the dot and the closing quotation mark, 
			// so LLLPG generates another prediction tree for LA(1) to "resolve" 
			// the ambiguity. However, LA(1) has exactly the same problem, so LLLPG
			// ends up using the default (which is to exit the loop). The case of
			// la0 == -1 is unambiguous, however, so it is created as a separate
			// top-level branch on the prediction tree.
			CheckResult(result, @"
				public partial class Parser
				{
					public void String()
					{
						int la0;
						Match('""');
						for (;;) {
							la0 = LA(0);
							if (la0 == '""')
								break;
							else if (la0 == -1)
								break;
							else
								MatchExcept();
						}
						Match('""');
					}
					public void Token()
					{
						int la0;
						for (;;) {
							la0 = LA(0);
							if (la0 == '""')
								String();
							else if (la0 != -1)
								MatchExcept();
							else
								break;
						}
					}
				}");
		}

		[Test]
		public void MLComment()
		{
			// public rule MLComment() ==> #[ '/' '*' nongreedy(.)* '*' '/' ];
			Rule MLComment = Rule("MLComment", C('/') + '*' + NongreedyStar(Dot) + '*' + '/', true, 2);
			_pg.AddRule(MLComment);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			CheckResult(result, @"
				public partial class Parser
				{
					public void MLComment()
					{
						int la0, la1;
						Match('/');
						Match('*');
						for (;;) {
							la0 = LA(0);
							if (la0 == '*') {
								la1 = LA(1);
								if (la1 == -1 || la1 == '/')
									break;
								else
									MatchExcept();
							} else if (la0 == -1)
								break;
							else
								MatchExcept();
						}
						Match('*');
						Match('/');
					}
				}");
		}

		Node Set(string var, object value) { return NF.Call(S.Set, NF.Symbol(var), NF.Literal(value)); }

		[Test]
		public void Number()
		{
			// // Helper rules for Number
			// rule DecDigits() ==> #[ '0'..'9'+ ('_' '0'..'9'+)* ]
			// rule BinDigits() ==> #[ '0'..'1'+ ('_' '0'..'1'+)* ]
			// rule HexDigits() ==> #[ ('0'..'9' | 'a'..'f' | 'A'..'F')+ ('_' ('0'..'9' | 'a'..'f' | 'A'..'F')+)* ]
			Rule DecDigits = Rule("DecDigits", Plus(Set("[0-9]")) + Star('_' + Plus(Set("[0-9]"))), false);
			Rule BinDigits = Rule("BinDigits", Plus(Set("[0-1]")) + Star('_' + Plus(Set("[0-1]"))), false);
			Rule HexDigits = Rule("HexDigits", Plus(Set("[0-9a-fA-F]")) + Star('_' + Plus(Set("[0-9a-fA-F]"))), false);

			// rule DecNumber() ==> #[
			//     {_numberBase=10;} 
			//     ( DecDigits ( {_isFloat=true;} '.' DecDigits )?
			//     | {_isFloat=true;} '.'
			//     ( {_isFloat=true;} ('e'|'E') ('+'|'-')? DecDigits )?
			// ];
			// rule HexNumber() ==> #[
			//     {_numberBase=16;}
			//     '0' ('x'|'X') HexDigits
			//     ( {_isFloat=true;} '.' HexDigits )?
			//     ( {_isFloat=true;} ('p'|'P') ('+'|'-')? DecDigits )?
			// ];
			// rule BinNumber() ==> #[
			//     {_numberBase=2;}
			//     '0' ('b'|'B') BinDigits
			//     ( {_isFloat=true;} '.' BinDigits )?
			//     ( {_isFloat=true;} ('p'|'P') ('+'|'-')? DecDigits )?
			// ];
			Rule DecNumber = Rule("DecNumber",
				Set("_numberBase", 10)
				+ (RuleRef)DecDigits
				+ Opt(Set("_isFloat", true) + C('.') + DecDigits)
				+ Opt(Set("_isFloat", true) + Set("[eE]") + Opt(Set("[+\\-]")) + DecDigits),
				false);
			Rule HexNumber = Rule("HexNumber",
				Set("_numberBase", 16)
				+ C('0') + Set("[xX]") + HexDigits
				+ Opt(Set("_isFloat", true) + C('.') + HexDigits)
				+ Opt(Set("_isFloat", true) + Set("[pP]") + Opt(Set("[+\\-]")) + DecDigits),
				false);
			Rule BinNumber = Rule("BinNumber",
				Set("_numberBase", 2)
				+ C('0') + Set("[bB]") + BinDigits
				+ Opt(Set("_isFloat", true) + C('.') + BinDigits)
				+ Opt(Set("_isFloat", true) + Set("[pP]") + Opt(Set("[+\\-]")) + DecDigits),
				false);

			// [TokenType($'#literal')]
			// token Number() ==> #[
			//     { _isFloat = false; _typeSuffix = $``; }
			//     (HexNumber | BinNumber | DecNumber)
			//     ( &{_isFloat} 
			//       ( ('f'|'F') {_typeSuffix=$F;}
			//       | ('d'|'D') {_typeSuffix=$D;}
			//       | ('m'|'M') {_typeSuffix=$M;}
			//       )
			//     | ('l'|'L') {_typeSuffix=$L;} (('u'|'U') {_typeSuffix=$UL;})?
			//     | ('u'|'U') {_typeSuffix=$U;} (('l'|'L') {_typeSuffix=$UL;})?
			//     )?
			// ];
			// rule Tokens() ==> #[ Token* ];
			// rule Token() ==> #[ Number | . ];
			Rule Number = Rule("Number", 
				Set("_isFloat", false) + (Set("_typeSuffix", GSymbol.Empty) 
				+ (HexNumber | BinNumber | DecNumber))
				+ Opt(And(NF.Symbol("_isFloat")) +
				    ( Set("[fF]") + Set("_typeSuffix", GSymbol.Get("F"))
				    | Set("[dD]") + Set("_typeSuffix", GSymbol.Get("D"))
				    | Set("[mM]") + Set("_typeSuffix", GSymbol.Get("M")) )
				  | Set("[lL]") + Set("_typeSuffix", GSymbol.Get("L")) + Opt(Set("[uU]") + Set("_typeSuffix", GSymbol.Get("UL")))
				  | Set("[uU]") + Set("_typeSuffix", GSymbol.Get("U")) + Opt(Set("[lL]") + Set("_typeSuffix", GSymbol.Get("UL")))
				  ), true);
			Rule Token = Rule("Token", DecNumber | Dot, false);
			Rule Tokens = Rule("Tokens", Star(Token), true);

			_pg.AddRules(new[] { DecDigits, HexDigits, BinDigits, DecNumber, HexNumber, BinNumber, Number, Token, Tokens });
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));

			//CheckResult(result, @"");
		}

	}
}
