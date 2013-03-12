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

		protected static Symbol _(string symbol) { return GSymbol.Get(symbol); }
		protected static Alts Star(Pred contents, bool? greedy = null) { return Pred.Star(contents, greedy); }
		protected static Alts Opt(Pred contents, bool? greedy = null) { return Pred.Opt(contents, greedy); }
		protected static Seq Plus(Pred contents, bool? greedy = null) { return Pred.Plus(contents, greedy); }
		protected static Gate Gate(Pred predictor, Pred match) { return new Gate(null, predictor, match); }
		protected static TerminalPred R(char lo, char hi) { return Pred.Range(lo, hi); }
		protected static TerminalPred C(char ch) { return Pred.Char(ch); }
		protected static TerminalPred Cs(params char[] chars) { return Pred.Chars(chars); }
		protected static TerminalPred Set(string set) { return Pred.Set(set); }
		protected static TerminalPred Any { get { return Set("[^]"); } }
		protected static AndPred And(object test) { return Pred.And(test); }
		protected static AndPred AndNot(object test) { return Pred.AndNot(test); }
		protected static Seq Seq(string s) { return Pred.Seq(s); }
		protected static Pred Set(string varName, Pred pred) { return Pred.Set(varName, pred); }
		protected static Pred SetVar(string varName, Pred pred) { return Pred.SetVar(varName, pred); }

		protected static Symbol Token = _("Token");
		protected static Symbol Start = _("Start");
		protected static Symbol Fragment = _("Fragment");
		protected static Rule Rule(string name, Pred contents, Symbol mode = null, int k = 0)
		{
			return Pred.Rule(name, contents, (mode ?? Start) == Start, mode == Token, k);
		}
		public Pred Do(Pred pred, Node postAction)
		{
			pred.PostAction = Pred.AppendAction(pred.PostAction, postAction);
			return pred;
		}

		protected LLParserGenerator _pg;
		protected NodeFactory NF = new NodeFactory(EmptySourceFile.Default);
		protected GreenFactory F = new GreenFactory(EmptySourceFile.Default);

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
			_pg.OutputMessage += OutputMessage;
			_messageCounter = 0;
            _expectingOutput = false;
		}

		int _messageCounter;
        bool _expectingOutput;
		void OutputMessage(Node node, Pred pred, Symbol type, string msg)
		{
			_messageCounter++;
			object subj = node == Node.Missing ? (object)pred : node;
            var tmp = Console.ForegroundColor;
            Console.ForegroundColor = _expectingOutput ? ConsoleColor.DarkGray : ConsoleColor.Yellow;
            Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
            Console.ForegroundColor = tmp;
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
						do {
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
							break;
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
						} while (false);
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
							continue;
							match2:
							{
								MatchRange('a', 'z');
								MatchRange('a', 'z');
							}
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


		[Test]
		public void NestedAlts()
		{
			Rule Nest = Rule("Nest", (C('a') | C('d') + 'd') + 't' | (Cs('a', 'o')) + 'd' + 'd');
			_pg.AddRule(Nest);
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));

			CheckResult(result, @"
				public partial class Parser
				{
					public void Nest()
					{
						int la0, la1;
						do {
							la0 = LA(0);
							if (la0 == 'a') {
								la1 = LA(1);
								if (la1 == 't')
									goto match1;
								else
									goto match2;
							} else if (la0 == 'd')
								goto match1;
							else
								goto match2;
							match1:
							{
								la0 = LA(0);
								if (la0 == 'a')
									Match('a');
								else {
									Match('d');
									Match('d');
								}
								Match('t');
							}
							break;
							match2:
							{
								Match('a', 'o');
								Match('d');
								Match('d');
							}
						} while (false);
					}
				}");
		}

		[Test]
		public void NotSupportedLL2()
		{
			// In this case, prediction always chooses the first alternative, so
			// second branch completely disappears from the output.
			Rule Nope = Rule("NotSupported", (C('a') + 'b' | C('b') + 'a') + 'c' | (C('a') + 'a' | C('b') + 'b') + 'c');
			_pg.AddRule(Nope);
            _expectingOutput = true;
            Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));
			//Console.WriteLine(result.Print());
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
			//     / { BeforeSeq; } ('1' { After1; } { Before2; } '2' '3') { AfterSeq; }
			//     / { BeforeOpt; } (greedy('?')? { AfterOpt; }) .
			//     )*
			//     { EndRule; }
			// ];
			// without actions: ('A' | ('1' '2' '3') | '?'? .)*
			Alts qmark;
			Rule Foo = Rule("Foo",
				Act("StartRule",
					Star( Act("BeforeA", C('A'), "AfterA")
						/ Act("BeforeSeq", Act(null, C('1'), "After1") + Act("Before2", C('2'), null) + '3', "AfterSeq")
						/ (Act("BeforeOpt", qmark=Opt(Act(null, C('?'), "AfterQMark")), "AfterOpt") + Any)), 
					"EndRule"));
			qmark.Greedy = true;

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
		public void ActionsTest2()
		{
			// public rule Foo ==> #[ ({a1} 'a' {a2} | {b1} 'b' {b2}) ];
			Rule Foo = Rule("Foo", Act("a1", C('a'), "a2") | Act("b1", C('b'), "b2"));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);

			CheckResult(result, @"
				public partial class Parser
				{
					public void Foo()
					{
						int la0;
						la0 = LA(0);
						if (la0 == 'a') {
							a1;
							Match('a');
							a2;
						} else {
							b1;
							Match('b');
							b2;
						}
					}
				}");
		}

		[Test]
		public void AddNumbers()
		{
			// This is a test of variable assignments and custom method bodies. The
			// goal is to simulate the following input to the parser generator:
			//
			//   rule int Number()
			//   {
			//      int n = 0;
			//      ==> #[ (c:='0'..'9' { n = checked((n * 10) + (c - '0')); })+ ];
			//      return n;
			//   }
			//   rule int AddNumbers() ==> #[
			//      total := Number ('+' total+=Number | '-' total-=Number)*
			//      { return total; }
			//   ];
			//
			// Since the C# parser doesn't exist yet, this is done the hard way...
			var F = new GreenFactory(NF.File);
			var n = F.Symbol("n");
			var stmt = F.Call(S.Set, n, F.Call(S.Checked, 
					F.Call(S.Add, F.Call(S.Mul, n, F.Literal(10)),
					   F.InParens(F.Call(S.Sub, F.Symbol("c"), F.Literal('0'))))));
			var Number = Rule("Number", Do(SetVar("c", R('0', '9')), Node.FromGreen(stmt)));
			var AddNumbers = Rule("AddNumbers", Do(
				Pred.SetVar("total", Number) + 
				Star( C('+') + Pred.Op("total", S.AddSet, Number) 
				    | C('-') + Pred.Op("total", S.SubSet, Number)),
				Node.FromGreen(F.Call(S.Return, F.Symbol("total")))));
			Number.MethodCreator = (rule, body) => {
				return Node.FromGreen(
					F.Attr(F.Public, F.Def(F.Int32, F.Symbol(rule.Name), F.List(), F.Braces(
						F.Var(F.Int32, F.Call(n, F.Literal(0))),
						body.FrozenGreen,
						F.Call(S.Return, n)
					))));
			};
			_pg.AddRule(Number);
			_pg.AddRule(AddNumbers);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			CheckResult(result, @"
				public partial class Parser
				{
					public int Number()
					{
						int n = 0;
						{
							var c = MatchRange('0', '9');
							n = checked(n * 10 + (c - '0'));
						}
						return n;
					}
					public void AddNumbers()
					{
						int la0;
						var total = Number();
						for (;;) {
							la0 = LA(0);
							if (la0 == '+') {
								Match('+');
								total += Number();
							} else if (la0 == '-') {
								Match('-');
								total -= Number();
							} else
								break;
						}
						return total;
					}
				}");
		}

		[Test]
		public void SimpleNongreedyTest()
		{
			Rule String = Rule("String", '"' + Star(Any,false) + '"', Token);
			Rule token = Rule("Token", Star(String / Any), Start);
			_pg.AddRule(String);
			_pg.AddRule(token);
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
							if (la0 == -1 || la0 == '""')
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
			Rule MLComment = Rule("MLComment", C('/') + '*' + Star(Any,false) + '*' + '/', Token, 2);
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

		protected virtual Node Set(string var, object value)
		{
			return NF.Call(S.Set, NF.Symbol(var), NF.Literal(value));
		}

		[Test]
		public void AndPredMatching()
		{
			// public rule MLComment() ==> #[ '/' '*' nongreedy(.)* '*' '/' ];
			Rule Foo = Rule("Foo", And(NF.Symbol("a")) + 'a' | And(NF.Symbol("b")) + 'b');
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			CheckResult(result, @"
				public partial class Parser
				{
					public void Foo()
					{
						int la0;
						la0 = LA(0);
						if (la0 == 'a') {
							Check(a);
							Match('a');
						} else {
							Check(b);
							Match('b');
						}
					}
				}");
		}

		[Test]
		public void AndPred1()
		{
			// public rule Foo ==> #[ (&a (Letter|Digit) | &b Digit | '_' ];
			// public set Letter ==> #[ 'a'..'z' | 'A'..'Z' ];
			// public set Digit ==> #[ '0'..'9' ];
			Rule Foo = Rule("Foo", And(NF.Symbol("a")) + Set("[a-zA-Z0-9]") | And(NF.Symbol("b")) + Set("[0-9]") | '_');
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("Parser"), NF.File);
			
			CheckResult(result, @"
				public partial class Parser
				{
					static readonly IntSet Foo_set0 = IntSet.Parse(""[0-9A-Za-z]"");
					public void Foo()
					{
						int la0;
						do {
							la0 = LA(0);
							if (la0 >= '0' && la0 <= '9') {
								if (a)
									goto match1;
								else {
									Check(b);
									MatchRange('0', '9');
								}
							} else if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
								goto match1;
							else
								Match('_');
							break;
							match1:
							{
								Check(a);
								Match(Foo_set0);
							}
						} while (false);
					}
				}");
		}

		[Test]
		public void OneAmbiguityExpected()
		{
			// This grammar should produce a single ambiguity warning for AmbigLL2 
			// and no warning for UnambigLL3. The warning for AmbigLL2 is:
			//   Warning: Optional branch is ambiguous for input such as «ab» ([a], [b])
			// LLPG contains specific code to suppress the other three warnings that
			// would otherwise be produced by the fact that tokens can be followed by 
			// anything.
			var AmbigLL2 = Rule("AmbigLL2", Opt(Seq("ab")) + 'a' + Opt(C('b')), Token, 2);
			var UnambigLL3 = Rule("UnambigLL3", Opt(Seq("ab")) + 'a' + Opt(C('b')), Token, 3);
			_pg.AddRule(AmbigLL2);
			_pg.AddRule(UnambigLL3);
            _expectingOutput = true;
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));
			CheckResult(result, @"
				public partial class Parser
				{
					public void AmbigLL2()
					{
						int la0, la1;
						la0 = LA(0);
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'b') {
								Match('a');
								Match('b');
							}
						}
						Match('a');
						la0 = LA(0);
						if (la0 == 'b')
							Match('b');
					}
					public void UnambigLL3()
					{
						int la0, la1, la2;
						la0 = LA(0);
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'b') {
								la2 = LA(2);
								if (la2 == 'a') {
									Match('a');
									Match('b');
								}
							}
						}
						Match('a');
						la0 = LA(0);
						if (la0 == 'b')
							Match('b');
					}
				}");
			AreEqual(1, _messageCounter);
		}

		[Test]
		public void OneAmbiguityExpectedB()
		{
			// There are two ambiguities here, but thanks to the slash, only one will be reported.
			_pg.AddRule(Rule("AmbigWithWarning", Plus(Set("[aeiou]")) / Plus(Set("[a-z]")) | Set("[aA]"), Start));
            _expectingOutput = true;
            Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));
            AreEqual(1, _messageCounter);
		}

		[Test]
		public void PlusOperators()
		{
			// Note: "++" and "+=" must come before "+" so that they have higher 
			// priority. LLPG doesn't implement a "longer match automatically wins" 
			// rule; the user must prioritize manually.
			// token MoreOrLess() ==> #[ "+=" | "++" | "--" | '+' | '-' ];
			_pg.AddRule(Rule("MoreOrLess", Seq("+=") / Seq("++") / Seq("--") / C('+') / C('-'), Token));
			Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));
			CheckResult(result, @"
				public partial class Parser
				{
					public void MoreOrLess()
					{
						int la0, la1;
						la0 = LA(0);
						if (la0 == '+') {
							la1 = LA(1);
							if (la1 == '=') {
								Match('+');
								Match('=');
							} else if (la1 == '+') {
								Match('+');
								Match('+');
							} else
								Match('+');
						} else {
							la1 = LA(1);
							if (la1 == '-') {
								Match('-');
								Match('-');
							} else
								Match('-');
						}
					}
				}");
		}

        [Test]
        public void NullableStar1()
        {
            Rule Bad = Rule("Bad", Star(Opt(Set("[0-9]")) + Opt(Set("[a-z]"))));
            _pg.AddRule(Bad);
            _expectingOutput = true;
            Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));
            GreaterOrEqual(_messageCounter, 1);
        }

        [Test]
        public void NullableStar2()
        {
            Rule Number = Rule("Number", Star(Set("[0-9]")) + Opt(C('.') + Plus(Set("[0-9]"))), Token);
            Rule WS = Rule("WS", Plus(Set("[ \t]")), Token);
            Rule Tokens = Rule("Tokens", Star(Number / WS), Start);
            _pg.AddRules(new[] { Number, WS, Tokens });
            _expectingOutput = true;
            Node result = _pg.GenerateCode(_("Parser"), new EmptySourceFile("LlpgTests.cs"));
            GreaterOrEqual(_messageCounter, 1);
        }
	}
}
