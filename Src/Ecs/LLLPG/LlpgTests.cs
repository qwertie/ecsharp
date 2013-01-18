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

		public Symbol _(string symbol) { return GSymbol.Get(symbol); }
		public static Alts Star(Pred contents) { return Pred.Star(contents); }
		public static Alts NongreedyStar(Pred contents) { var star = Pred.Star(contents); star.Greedy = false; return star; }
		public static Alts Opt(Pred contents) { return Pred.Opt(contents); }
		public static Seq Plus(Pred contents) { return Pred.Plus(contents); }
		public static Gate Gate(Pred predictor, Pred match) { return new Gate(null, predictor, match); }
		public static TerminalPred R(char lo, char hi) { return Pred.Range(lo, hi); }
		public static TerminalPred C(char ch) { return Pred.Char(ch); }
		public static TerminalPred Cs(params char[] chars) { return Pred.Chars(chars); }
		public static TerminalPred S(IPGTerminalSet set) { return Pred.Set(set); }
		public static TerminalPred Dot { get { return S(PGIntSet.All()); } }
		public static Rule Rule(string name, Pred contents, bool isStartingRule = true, int k = 0) { return Pred.Rule(name, contents, isStartingRule, false, k); }

		public LLParserGenerator _pg;
		public NodeFactory NF = new NodeFactory(EmptySourceFile.Default);

		public void CheckResult(Node result, string verbatim)
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
		public void LL2NestedPredictionTree()
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
						int alt = 0;
						la0 = LA(0);
						if (la0 == 'x') {
							la1 = LA(1);
							if (la1 == -1 || la1 >= 'a' && la1 <= 'z')
								alt = 1;
							else {
								Match('x');
								MatchRange('0', '9');
								MatchRange('0', '9');
							}
						} else
							alt = 1;
						if (alt == 1) {
							MatchRange('a', 'z');
							for (;;) {
								la0 = LA(0);
								if (la0 >= 'a' && la0 <= 'z')
									MatchRange('a', 'z');
								else
									break;
							}
						}
					}
				}");
			// NOTE: MatchRange('a', 'z') should be simply Match()
		}

		[Test]
		public void MatchInvertedSet()
		{
			// public rule Except ==> #[ ~'a' ~('a'..'z') ];
			// public rule String ==> #[ '"' ~('"'|'\n')* '"' ];
			
			Rule Except = Rule("Except", S(PGIntSet.WithoutChars('a')) + S(PGIntSet.WithoutCharRanges('a', 'z')));
			Rule String = Rule("String", '"' + Star(S(PGIntSet.WithoutChars('"', '\n'))) + '"');
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
		public void MLComment()
		{
			// public rule MLComment() ==> #[ '/' '*' nongreedy(.)* '*' '/' ];
			Rule MLComment = Rule("MLComment", C('/') + '*' + NongreedyStar(Dot) + '*' + '/', true, 2);
		}

		[Test]
		public void MatchComplexSet()
		{
			Rule Odd = Rule("Odd", Plus(Cs('-', '.', '1', '3', '5', '7', '9')));
			_pg.AddRule(Odd);
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class Parser
				{
					static readonly IntSet Odd_set0 = IntSet.Parse(""[\\--.13579]"");
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
						| Act("BeforeOpt", Opt(C('?')), "AfterOpt") + S(PGIntSet.WithoutChars())), 
					"EndRule"));

			Foo.K = 1;
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class FooClass
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
								if (la0 == '?')
									Match('?');
								AfterOpt;
								MatchExcept();
							} else
								break;
						}
						EndRule;
					}
				}");
		}
	}
}
