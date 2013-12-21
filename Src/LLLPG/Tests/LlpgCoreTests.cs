using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;
using Ecs;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	public class LlpgHelpers
	{
		protected static LNodeFactory F = new LNodeFactory(new EmptySourceFile("Plain-C# Grammar"));
		protected static Symbol _(string symbol) { return GSymbol.Get(symbol); }
		protected static Alts Star(Pred contents, bool? greedy = null) { return Pred.Star(contents, greedy); }
		protected static Alts Opt(Pred contents, bool? greedy = null) { return Pred.Opt(contents, greedy); }
		protected static Seq Plus(Pred contents, bool? greedy = null) { return Pred.Plus(contents, greedy); }
		protected static Gate Gate(Pred predictor, Pred match) { return new Gate(null, predictor, match); }
		protected static TerminalPred R(char lo, char hi) { return Pred.Range(lo, hi); }
		protected static TerminalPred C(char ch) { return Pred.Char(ch); }
		protected static TerminalPred Cs(params char[] chars) { return Pred.Chars(chars); }
		protected static TerminalPred Set(string set) { return Pred.Set(set); }
		protected static TerminalPred Lit(params object[] s) { return Pred.Set(s.Select(s0 => F.Literal(s0)).ToArray()); }
		protected static TerminalPred Lit(object s) { return Pred.Set(F.Literal(s)); }
		protected static TerminalPred Sym(params string[] s) { return Pred.Set(s.Select(s0 => F.Literal(GSymbol.Get(s0))).ToArray()); }
		protected static TerminalPred Sym(string s) { return Pred.Set(F.Literal(GSymbol.Get(s))); }
		protected static TerminalPred Id(params string[] s) { return Pred.Set(s.Select(s0 => F.Id(s0)).ToArray()); }
		protected static TerminalPred Id(string s) { return Pred.Set(F.Id(s)); }
		protected static TerminalPred NotSym(params Symbol[] s) { return Pred.Not(s.Select(s0 => F.Literal(s0)).ToArray()); }
		protected static TerminalPred NotId(params string[] s) { return Pred.Not(s.Select(s0 => F.Id(s0)).ToArray()); }
		protected static TerminalPred AnyCh { get { return Set("[^]"); } }
		protected static TerminalPred AnyNode { get { return NotSym(); } }
		protected static AndPred And(LNode test) { return Pred.And(test); }
		protected static AndPred And(Pred test) { return Pred.And(test); }
		protected static AndPred And(string expr) { return Pred.And(Expr(expr)); }
		protected static AndPred AndNot(LNode test) { return Pred.AndNot(test); }
		protected static AndPred AndNot(Pred test) { return Pred.AndNot(test); }
		protected static AndPred AndNot(string expr) { return Pred.AndNot(Expr(expr)); }
		protected static RuleRef Call(Rule rule, params LNode[] args) { 
			return new RuleRef(null, rule) { Params = new RVList<LNode>(args) };
		}

		protected static Seq Seq(string s) { return Pred.Seq(s); }
		protected Seq Seq()
		{
			return new Seq(null);
		}
		protected static Pred Set(string varName, Pred pred) { return Pred.Set(varName, pred); }
		protected static Pred SetVar(string varName, Pred pred) { return Pred.SetVar(varName, pred); }
		protected static Pred AddSet(string varName, Pred pred) { return Pred.AddSet(varName, pred); }

		protected static LNode Stmt(string code)
		{
			return F.Attr(F.Trivia(S.TriviaRawTextBefore, code), F._Missing);
		}
		protected static LNode Expr(string code)
		{
			var expr = F.Trivia(S.RawText, code);
			return expr;
		}
		protected static Symbol Token = _("Token");
		protected static Symbol Start = _("Start");
		protected static Symbol Private = _("Private");
		protected static Rule Rule(string name, Pred contents, Symbol mode = null, int k = 0)
		{
			var rule = Pred.Rule(name, contents, (mode ?? Start) == Start, mode == Token, k);
			if (mode == Private)
				rule.IsPrivate = true;
			return rule;
		}
	}

	/// <summary>These are basic tests of the core engine, <see cref="LLParserGenerator"/>.</summary>
	/// <remarks>This was the initial test suite, written before the LES and EC# parsers existed.</remarks>
	[TestFixture]
	public class LlpgCoreTests : LlpgHelpers
	{
		public Pred Do(Pred pred, LNode postAction)
		{
			pred.PostAction = Pred.MergeActions(pred.PostAction, postAction);
			return pred;
		}

		protected LLParserGenerator _pg;
		protected ISourceFile _file;

		[SetUpAttribute]
		public void SetUp()
		{
			LNode.Printer = EcsNodePrinter.Printer;
			_pg = new LLParserGenerator(new IntStreamCodeGenHelper());
			_pg.Sink = new MessageSinkFromDelegate(OutputMessage);
			_messageCounter = 0;
			_expectingOutput = false;
			_file = new EmptySourceFile("LlpgTests.cs");
		}

		int _messageCounter;
		bool _expectingOutput;
		void OutputMessage(Symbol type, object context, string msg, params object[] args)
		{
			_messageCounter++;
			var tmp = Console.ForegroundColor;
			Console.ForegroundColor = _expectingOutput ? ConsoleColor.DarkGray : ConsoleColor.Yellow;
			Console.WriteLine("--- at {0}:\n--- {1}: {2}", context, type, msg);
			Console.ForegroundColor = tmp;
		}


		protected void CheckResult(LNode result, string verbatim)
		{
			var sb = new StringBuilder();
			var np = EcsNodePrinter.New(result, sb);
			np.SetPlainCSharpMode();
			np.PrintStmt();
			Assert.AreEqual(StripExtraWhitespace(verbatim), StripExtraWhitespace(sb.ToString()));
		}
		public static string StripExtraWhitespace(string a)
		{
			return LEL.MacroProcessorTests.StripExtraWhitespace(a);
		}
		static bool MaybeId(char c) { return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }

		[Test]
		public void SimpleMatching()
		{
			Rule Foo = Rule("Foo", 'x' + R('0', '9') + R('0', '9'));
			_pg.AddRule(Foo);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
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
			_pg.AddRules(a, b, Foo);
			a.IsPrivate = true; // allow prematching
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
				{
					private void a()
					{
						Skip();
					}
					public void b()
					{
						Match('B', 'b');
					}
					public void Foo()
					{
						int la0;
						la0 = LA0;
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
			// public rule Foo ==> @[ (a | b? 'c')* ];
			Rule Foo = Rule("Foo", Star(a | Opt(b) + 'c'));
			_pg.AddRules(a, b, Foo);
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
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
							switch (LA0) {
							case 'A':
							case 'a':
								a();
								break;
							case 'B':
							case 'b':
							case 'c':
								{
									la0 = LA0;
									if (la0 == 'B' || la0 == 'b')
										b();
									Match('c');
								}
								break;
							default:
								goto stop;
							}
						}
					stop:;
					}
				}");
		}

		[Test]
		public void LL2Example1()
		{
			// public rule Foo ==> @[ 'a'..'z'+ | 'x' '0'..'9' '0'..'9' ];
			Rule Foo = Rule("Foo", Plus(R('a','z')) | 'x' + R('0','9') + R('0','9'));
			_pg.AddRule(Foo);
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
				{
					public void Foo()
					{
						int la0, la1;
						do {
							la0 = LA0;
							if (la0 == 'x') {
								la1 = LA(1);
								if (la1 == -1 || la1 >= 'a' && la1 <= 'z')
									goto match1;
								else {
									Skip();
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
									la0 = LA0;
									if (la0 >= 'a' && la0 <= 'z')
										Skip();
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
			// rule Foo ==> @[ (('a'|'A') 'A' | 'a'..'z' 'a'..'z')* ];
			Rule Foo = Rule("Foo", Star((C('a')|'A') + 'A' | R('a','z') + R('a','z')));
			_pg.AddRule(Foo);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"
				{
					public void Foo()
					{
						int la0, la1;
						for (;;) {
							la0 = LA0;
							if (la0 == 'a') {
								la1 = LA(1);
								if (la1 == 'A')
									goto match1;
								else
									goto match2;
							} else if (la0 == 'A')
								goto match1;
							else if (la0 >= 'b' && la0 <= 'z')
								goto match2;
							else
								break;
						match1:
							{
								Skip();
								Match('A');
							}
							continue;
						match2:
							{
								Skip();
								MatchRange('a', 'z');
							}
						}
					}
				}");
		}

		[Test]
		public void LL2Example3()
		{
			// rule Foo ==> @[ (('a'|'A') 'A')* 'a'..'z' 'a'..'z' ];
			Rule Foo = Rule("Foo", Star(Set("[aA]") + 'A') + R('a','z') + R('a','z'));
			_pg.AddRule(Foo);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"
				{
					public void Foo()
					{
						int la0, la1;
						for (;;) {
							la0 = LA0;
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
								Skip();
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
			// public rule Except ==> @[ ~'a' ~('a'..'z') ];
			// public rule String ==> @[ '"' ~('"'|'\n')* '"' ];
			
			Rule Except = Rule("Except", Set("[^a]") + Set("[^a-z]"));
			Rule String = Rule("String", '"' + Star(Set("[^\"\n]")) + '"');
			_pg.AddRule(Except);
			_pg.AddRule(String);
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
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
							la0 = LA0;
							if (!(la0 == -1 || la0 == '\n' || la0 == '""'))
								Skip();
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
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
			{
				static readonly HashSet<int> Odd_set0 = NewSetOfRanges('-', '.', '1', '1', '3', '3', '5', '5', '7', '7', '9', '9', 'a', 'z');
				public void Odd()
				{
					int la0;
					Match(Odd_set0);
					for (;;) {
						la0 = LA0;
						if (Odd_set0.Contains(la0))
							Skip();
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
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
				{
					public void Nest()
					{
						int la0, la1;
						do {
							la0 = LA0;
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
								la0 = LA0;
								if (la0 == 'a')
									Skip();
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
		public void FullLL2()
		{
			// FullLL2 ==> @[ ('a' 'b' | 'b' 'a') 'c' | ('a' 'a' | 'b' 'b') 'c' ];
			Rule Nope = Rule("FullLL2", (C('a') + 'b' | C('b') + 'a') + 'c' | (C('a') + 'a' | C('b') + 'b') + 'c');
			_pg.AddRule(Nope);
			
			// Without Full LL(2), prediction always chooses the first alternative,
			// so second branch completely disappears from the output.
			_expectingOutput = true; // "Branch 2 is unreachable."
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void FullLL2()
				{
					int la0;
					la0 = LA0;
					if (la0 == 'a') {
						Skip();
						Match('b');
					} else {
						Match('b');
						Match('a');
					}
					Match('c');
				}
			}");

			// Try again with experimental Full LL(k)
			_pg.FullLLk = true;
			_expectingOutput = false;
			result = _pg.Run(_file);
			CheckResult(result, @"{
				public void FullLL2()
				{
					int la0, la1;
					do {
						la0 = LA0;
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'b')
								goto match1;
							else
								goto match2;
						} else {
							la1 = LA(1);
							if (la1 == 'a')
								goto match1;
							else
								goto match2;
						}
					match1:
						{
							la0 = LA0;
							if (la0 == 'a') {
								Skip();
								Match('b');
							} else {
								Match('b');
								Match('a');
							}
							Match('c');
						}
						break;
					match2:
						{
							la0 = LA0;
							if (la0 == 'a') {
								Skip();
								Match('a');
							} else {
								Match('b');
								Match('b');
							}
							Match('c');
						}
					} while (false);
				}
			}");
		}

		public Pred Act(string pre, Pred pred, string post)
		{
			if (pre != null) pred.PreAction = F.Id(pre);
			if (post != null) pred.PostAction = F.Id(post);
			return pred;
		}
		[Test]
		public void ActionsTest()
		{
			// public rule Foo ==> @[
			//     { StartRule; }
			//     ( { BeforeA; } 'A' { AfterA; }
			//     / { BeforeSeq; } ('1' { After1; } { Before2; } '2' '3') { AfterSeq; }
			//     / { BeforeOpt; } (greedy('?')? { AfterOpt; }) _
			//     )*
			//     { EndRule; }
			// ];
			// without actions: ('A' | ('1' '2' '3') | '?'? .)*
			Alts qmark;
			Rule Foo = Rule("Foo",
				Act("StartRule",
					Star( Act("BeforeA", C('A'), "AfterA")
						/ Act("BeforeSeq", Act(null, C('1'), "After1") + Act("Before2", C('2'), null) + '3', "AfterSeq")
						/ (Act("BeforeOpt", qmark=Opt(Act(null, C('?'), "AfterQMark")), "AfterOpt") + AnyCh)), 
					"EndRule"));
			qmark.Greedy = true;

			Foo.K = 1;
			_pg.AddRule(Foo);
			LNode result = _pg.Run(_file);

			CheckResult(result, @"
			{
				public void Foo()
				{
					int la0;
					StartRule;
					for (;;) {
						la0 = LA0;
						if (la0 == 'A') {
							BeforeA;
							Skip();
							AfterA;
						} else if (la0 == '1') {
							BeforeSeq;
							Skip();
							After1;
							Before2;
							Match('2');
							Match('3');
							AfterSeq;
						} else if (la0 != -1) {
							BeforeOpt;
							la0 = LA0;
							if (la0 == '?') {
								Skip();
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
			// public rule Foo ==> @[ ({a1} 'a' {a2} | {b1} 'b' {b2}) ];
			Rule Foo = Rule("Foo", Act("a1", C('a'), "a2") | Act("b1", C('b'), "b2"));
			_pg.AddRule(Foo);
			LNode result = _pg.Run(F.File);

			CheckResult(result, @"
				{
					public void Foo()
					{
						int la0;
						la0 = LA0;
						if (la0 == 'a') {
							a1;
							Skip();
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
		public void ComplexSwitch()
		{
			Alts alts;
			Rule token = Rule("Token", alts = (Alts)(
				Stmt("type = Punctuation") + Set(@"[*+\-/%^&,|]") |
				Stmt("type = Identifier")  + Set(@"[_$a-zA-Z]") + Star(Set(@"[_$a-zA-Z0-9]")) |
				Stmt("type = Integer")     + Plus(Set("[0-9]")) |
				Stmt("type = Space")       + Set("[ \t]")));
			alts.ErrorBranch = DefaultErrorBranch.Value;
			_pg.AddRule(token);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"{
				static readonly HashSet<int> Token_set0 = NewSetOfRanges('$', '$', '0', '9', 'A', 'Z', '_', '_', 'a', 'z');
				static readonly HashSet<int> Token_set1 = NewSetOfRanges('$', '$', 'A', 'Z', '_', '_', 'a', 'z');
				public void Token()
				{
					int la0;
					la0 = LA0;
					switch (la0) {
					case '%':
					case '&':
					case '*':
					case '+':
					case ',':
					case '-':
					case '/':
					case '^':
					case '|':
						{
							type = Punctuation;
							Skip();
						}
						break;
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						{
							type = Integer;
							Skip();
							for (;;) {
								la0 = LA0;
								if (la0 >= '0' && la0 <= '9')
									Skip();
								else
									break;
							}
						}
						break;
					case '\t':
					case ' ':
						{
							type = Space;
							Skip();
						}
						break;
					default:
						if (Token_set1.Contains(la0)) {
							type = Identifier;
							Skip();
							for (;;) {
								la0 = LA0;
								if (Token_set0.Contains(la0))
									Skip();
								else
									break;
							}
						} else
							Error(InputPosition+0, ""In rule 'Token', expected one of: [\\t $-&*-\\-/-9A-Z^_a-z|]"");
						break;
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
			//      ==> @[ (c:='0'..'9' { n = checked((n * 10) + (c - '0')); })+ ];
			//      return n;
			//   }
			//   rule int AddNumbers() ==> @[
			//      total := Number ('+' total+=Number | '-' total-=Number)*
			//      { return total; }
			//   ];
			//
			// Since the C# parser doesn't exist yet, this is done the hard way...
			var n = F.Id("n");
			var stmt = F.Set(n, F.Call(S.Checked, 
					F.Call(S.Add, F.Call(S.Mul, n, F.Literal(10)),
					   F.InParens(F.Call(S.Sub, F.Id("c"), F.Literal('0'))))));
			var Number = Rule("Number", 
				(LNode)F.Var(F.Int32, n.Name, F.Literal(0)) + 
				Star(SetVar("c", R('0', '9')) + 
					(LNode)stmt) +
				(LNode)F.Call(S.Return, n));
			var AddNumbers = Rule("AddNumbers", 
				Pred.SetVar("total", Number) + 
				Star( C('+') + Pred.Op("total", S.AddSet, Number) 
				    | C('-') + Pred.Op("total", S.SubSet, Number)) +
				F.Call(S.Return, F.Id("total")));
			Number.Basis = (LNode)F.Attr(F.Public, F.Def(F.Int32, F.Id("Number"), F.Tuple()));
			//Number.MethodCreator = (rule, body) => {
			//    return Node.FromGreen(
			//        F.Attr(F.Public, F.Def(F.Int32, F.Id(rule.Name), F.List(), F.Braces(
			//            F.Var(F.Int32, n, F.Literal(0)),
			//            body.FrozenGreen,
			//            F.Call(S.Return, n)
			//        ))));
			//};
			_pg.AddRule(Number);
			_pg.AddRule(AddNumbers);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"{
				public int Number()
				{
					int la0;
					int n = 0;
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9') {
							var c = MatchAny();
							n = checked(n * 10 + (c - '0'));
						} else
							break;
					}
					return n;
				}
				public void AddNumbers()
				{
					int la0;
					var total = Number();
					for (;;) {
						la0 = LA0;
						if (la0 == '+') {
							Skip();
							total += Number();
						} else if (la0 == '-') {
							Skip();
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
			Rule String = Rule("String", '"' + Star(AnyCh,false) + '"', Token);
			Rule token = Rule("Token", Star(String / AnyCh), Start);
			_pg.AddRule(String);
			_pg.AddRule(token);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"
				{
					public void String()
					{
						int la0;
						Match('""');
						for (;;) {
							la0 = LA0;
							if (la0 == -1 || la0 == '""')
								break;
							else
								Skip();
						}
						Match('""');
					}
					public void Token()
					{
						int la0, la1;
						for (;;) {
							la0 = LA0;
							if (la0 == '""') {
								la1 = LA(1);
								if (la1 != -1)
									String();
								else
									Skip();
							} else if (la0 != -1)
								Skip();
							else
								break;
						}
					}
				}");
		}

		[Test]
		public void MLComment()
		{
			// public rule MLComment() ==> @[ '/' '*' nongreedy(_)* '*' '/' ];
			Rule MLComment = Rule("MLComment", C('/') + '*' + Star(AnyCh,false) + '*' + '/', Token, 2);
			_pg.AddRule(MLComment);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"
				{
					public void MLComment()
					{
						int la0, la1;
						Match('/');
						Match('*');
						for (;;) {
							la0 = LA0;
							if (la0 == '*') {
								la1 = LA(1);
								if (la1 == -1 || la1 == '/')
									break;
								else
									Skip();
							} else if (la0 == -1)
								break;
							else
								Skip();
						}
						Match('*');
						Match('/');
					}
				}");
		}

		protected virtual LNode Set(string var, object value)
		{
			return F.Set(F.Id(var), F.Literal(value));
		}

		[Test]
		public void AndPredMatching()
		{
			// public rule MLComment() ==> @[ '/' '*' nongreedy(_)* '*' '/' ];
			Rule Foo = Rule("Foo", And(F.Id("a")) + 'a' | And(F.Id("b")) + 'b');
			_pg.AddRule(Foo);
			LNode result = _pg.Run(F.File);
			CheckResult(result, @"
				{
					public void Foo()
					{
						int la0;
						la0 = LA0;
						if (la0 == 'a') {
							Check(a, ""a"");
							Skip();
						} else {
							Check(b, ""b"");
							Match('b');
						}
					}
				}");
		}

		[Test]
		public void AndPred1()
		{
			// public rule Foo ==> @[ (&a (Letter|Digit) | &b Digit | '_' ];
			// public set Letter ==> @[ 'a'..'z' | 'A'..'Z' ];
			// public set Digit ==> @[ '0'..'9' ];
			Rule Foo = Rule("Foo", And(F.Id("a")) + Set("[a-zA-Z0-9]") | And(F.Id("b")) + Set("[0-9]") | '_');
			_pg.AddRule(Foo);
			LNode result = _pg.Run(F.File);
			
			CheckResult(result, @"
				{
					public void Foo()
					{
						int la0;
						do {
							la0 = LA0;
							if (la0 >= '0' && la0 <= '9') {
								if (a)
									goto match1;
								else {
									Check(b, ""b"");
									Skip();
								}
							} else if (la0 >= 'A' && la0 <= 'Z' || la0 >= 'a' && la0 <= 'z')
								goto match1;
							else
								Match('_');
							break;
						match1:
							{
								Check(a, ""a"");
								Skip();
							}
						} while (false);
					}
				}");
		}

		[Test]
		public void AndPred2()
		{
			// ( &{a} &{b} ('x'|'X')
			// | &{c}      ('x'|'y'))?
			Rule Foo = Rule("Foo", And("a") + And("b") + Set("[xX]") | And("c") + Set("[xy]"));
			_pg.AddRule(Foo);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void Foo()
				{
					int la0;
					do {
						la0 = LA0;
						if (la0 == 'x') {
							if (a && b)
								goto match1;
							else
								goto match2;
						} else if (la0 == 'X')
							goto match1;
						else
							goto match2;
					match1:
						{
							Check(a, ""a"");
							Check(b, ""b"");
							Skip();
						}
						break;
					match2:
						{
							Check(c, ""c"");
							MatchRange('x', 'y');
						}
					} while (false);
				}
			}");
		}

		[Test]
		public void AndPred3()
		{
			// ( &{a} (&{b} | &{c} {Foo;}) &{d} '?' ':'
			// | &{a} '?' (':'|'?')
			// | ':')
			Rule Foo = Rule("Foo", And("a") + (And("b") | And("c") + Stmt("Foo")) + And("d") + '?' + ':'
			                     | And("a") + '?' + '?' | ':');
			_pg.AddRule(Foo);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void Foo()
				{
					int la0, la1;
					do {
							la0 = LA0;
							if (la0 == '?') {
									if (d) {
											if (b || c) {
													la1 = LA(1);
													if (la1 == ':') {
															Check(a, ""a"");
															if (b) {
															} else {
																	Check(c, ""c"");
																	Foo;
															}
															Skip();
															Skip();
													} else
															goto match2;
											} else
													goto match2;
									} else
											goto match2;
							} else
									Match(':');
							break;
					match2:
							{
									Check(a, ""a"");
									Skip();
									Match('?');
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
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
				{
					public void AmbigLL2()
					{
						int la0, la1;
						la0 = LA0;
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'b') {
								Skip();
								Skip();
							}
						}
						Match('a');
						la0 = LA0;
						if (la0 == 'b')
							Skip();
					}
					public void UnambigLL3()
					{
						int la0, la1, la2;
						la0 = LA0;
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'b') {
								la2 = LA(2);
								if (la2 == 'a') {
									Skip();
									Skip();
								}
							}
						}
						Match('a');
						la0 = LA0;
						if (la0 == 'b')
							Skip();
					}
				}");
			Assert.AreEqual(1, _messageCounter);
		}

		[Test]
		public void OneAmbiguityExpectedB()
		{
			// There are two ambiguities here, but thanks to the slash, only one will be reported.
			_pg.AddRule(Rule("AmbigWithWarning", Plus(Set("[aeiou]")) / Plus(Set("[a-z]")) | Set("[aA]"), Start));
			_expectingOutput = true;
			LNode result = _pg.Run(_file);
			Assert.AreEqual(1, _messageCounter);
		}

		[Test]
		public void PlusOperators()
		{
			// Note: "++" and "+=" must come before "+" so that they have higher 
			// priority. LLPG doesn't implement a "longer match automatically wins" 
			// rule; the user must prioritize manually.
			// token MoreOrLess() ==> @[ "+=" | "++" | "--" | '+' | '-' ];
			_pg.AddRule(Rule("MoreOrLess", Seq("+=") / Seq("++") / Seq("--") / C('+') / C('-'), Token));
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
				{
					public void MoreOrLess()
					{
						int la0, la1;
						la0 = LA0;
						if (la0 == '+') {
							la1 = LA(1);
							if (la1 == '=') {
								Skip();
								Skip();
							} else if (la1 == '+') {
								Skip();
								Skip();
							} else
								Skip();
						} else {
							la1 = LA(1);
							if (la1 == '-') {
								Match('-');
								Skip();
							} else
								Match('-');
						}
					}
				}");
		}

		[Test]
		public void NullableStarError1()
		{
			Rule Bad = Rule("Bad", Star(Opt(Set("[0-9]")) + Opt(Set("[a-z]"))));
			// rule Bad ==> @[ ('0'..'9'? '0'..'9'?)* ];
			// ERROR IS EXPECTED.
			_pg.AddRule(Bad);
			_expectingOutput = true;
			LNode result = _pg.Run(_file);
			Assert.GreaterOrEqual(_messageCounter, 1);
		}

		[Test]
		public void NullableStarError2()
		{
			// rule Number ==> @[ ('0'..'9')* ('.' ('0'..'9')+)? ];
			// rule WS     ==> @[ (' '|'\t')+ ];
			// rule Tokens ==> @[ (Number / WS)* ];
			// ERROR IS EXPECTED in Tokens: Arm #1 of this loop is nullable.
			Rule Number = Rule("Number", Star(Set("[0-9]")) + Opt(C('.') + Plus(Set("[0-9]"))), Token);
			Rule WS = Rule("WS", Plus(Set("[ \t]")), Token);
			Rule Tokens = Rule("Tokens", Star(Number / WS), Start);
			_pg.AddRules(Number, WS, Tokens);
			_expectingOutput = true;
			LNode result = _pg.Run(_file);
			Assert.GreaterOrEqual(_messageCounter, 1);
		}

		[Test]
		public void LeftRecursive1()
		{
			// I really didn't know how LLPG would react to a left-recursive 
			// grammar. It turns out that it produces code that causes a stack 
			// overflow if there are 2 or more 'A's in a row (at first I actually 
			// thought it worked in the general case, because apparently I am not 
			// that smart.) Later on, I fixed a bug which triggered LLLPG to 
			// inform me that the grammar is ambiguous in case of input such as 
			// "AA", which made me think about this code again and realize it was 
			// defective. TODO: think of a way to detect a left-recursive grammar
			// (whether directly or indirectly recursive) and print an error.

			// rule A ==> @[ A? ('a'|'A') ];
			RuleRef ARef = new RuleRef(null, null);
			Rule A = Rule("A", Opt(ARef) + Set("[aA]"), Token);
			A.K = 3;
			ARef.Rule = A;
			_pg.AddRule(A);
			_expectingOutput = true;
			LNode result = _pg.Run(_file);
//            CheckResult(result, @"
//				{
//					public void A()
//					{
//						int la0, la1;
//						la0 = LA0;
//						if (la0 == 'A' || la0 == 'a') {
//							la1 = LA(1);
//							if (la1 == 'A' || la1 == 'a')
//								A();
//						}
//						Match('A', 'a');
//					}
//				}");
			// Code generation has changed. I'm not sure what exactly happens now, 
			// but since the grammar is illegal it doesn't really matter.
			CheckResult(result, @"{
				public void A()
				{
					;
					Match('A', 'a');
				}
			}");
		}

		[Test]
		public void LeftRecursive2()
		{
			// This is a more typical left-recursive grammar, and it doesn't work.
			// Originally LLPG did not detect left-recursion, but it detected LL(2)
			// ambiguity and complained about it. Now the ComputeNext class 
			// complains about excessive recursion although it still does not detect
			// whether the grammar is left-recursive, infinite-looping, or just too
			// complex.
			Rule Int = Rule("Int", Plus(Set("[0-9]")), Token);
			Rule Expr = Rule("Expr", Int, Start);
			Expr.Pred = Expr + C('+') + Int | Expr + C('-') + Int | Int;
			_pg.AddRule(Int);
			_pg.AddRule(Expr);

			_expectingOutput = true;
			LNode result = _pg.Run(_file);
			Assert.GreaterOrEqual(_messageCounter, 1);
		}

		[Test]
		public void SemPredUsingLI()
		{
			// You can write a semantic predicate using the replacement "$LI" which
			// will insert the index of the current lookahead, or "$LA" which 
			// inserts a variable that holds the actual lookahead symbol. Test this 
			// feature with two different lookahead amounts for the same predicate.
			
			// rule Id() ==> @[ &{char.IsLetter($LA)} _ (&{char.IsLetter($LA) || char.IsDigit($LA)} _)* ];
			// rule Twin() ==> @[ 'T' &{$LA == LA($LI+1)} '0'..'9' '0'..'9' ];
			// token Token() ==> @[ Twin / Id ];
			var la = F.Call(S.Substitute, F.Id("LA"));
			var li = F.Call(S.Substitute, F.Id("LI"));
			var isLetter = F.Call(F.Dot(F.Char, F.Id("IsLetter")), la);
			var isDigit = F.Call(F.Dot(F.Char, F.Id("IsDigit")), la);
			var isTwin = F.Call(S.Eq, la, F.Call(F.Id("LA"), F.Call(S.Add, li, F.Literal(1))));
			Rule id = Rule("Id", And((LNode)isLetter) + AnyCh + Star(And((LNode)F.Call(S.Or, isLetter, isDigit)) + AnyCh));
			Rule twin = Rule("Twin", C('T') + And((LNode)isTwin) + Set("[0-9]") + Set("[0-9]"));
			Rule token = Rule("Token", twin / id, Token);
			_pg.AddRules(id, twin, token);
			
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
				{
					public void Id()
					{
						int la0;
						Check(char.IsLetter(LA0), ""char.IsLetter($LA)"");
						MatchExcept();
						for (;;) {
							la0 = LA0;
							if (la0 != -1) {
								la0 = LA0;
								if (char.IsLetter(la0) || char.IsDigit(la0))
									Skip();
								else
									break;
							} else
								break;
						}
					}
					public void Twin()
					{
						Match('T');
						Check(LA0 == LA(0 + 1), ""$LA == LA($LI + 1)"");
						MatchRange('0', '9');
						MatchRange('0', '9');
					}
					public void Token()
					{
						int la0, la1;
						la0 = LA0;
						if (la0 == 'T') {
							la0 = LA0;
							if (char.IsLetter(la0)) {
								la1 = LA(1);
								if (la1 >= '0' && la1 <= '9') {
									la1 = LA(1);
									if (la1 == LA(1 + 1))
										Twin();
									else
										Id();
								} else
									Id();
							} else
								Twin();
						} else
							Id();
					}
				}");
		}

		[Test]
		public void DifferentDefault()
		{
			// In this test, the default arm is set to the second or third item
			// rather than the exit branch, which affects code generation. Sometimes
			// you can simplify or speed up the code by changing the default branch.
			// Changing the default branch should never change the behavior of the
			// generated parser /when the input is valid/. However, when the input
			// is ungrammatical, the default branch is invoked; therefore, changing 
			// the default branch implies changing how unexpected input is handled.
			Alts star1 = Star(Set("[aA]") + 'x' 
			                | (Seq("BAT") | Seq("bat")) + '!'
			                | Set("[b-z]") + Set("[b-z]"));
			Alts star2 = (Alts)star1.Clone();
			star1.DefaultArm = 1;
			star2.DefaultArm = 2;
			_pg.AddRule(Rule("Default1", star1 + '.', Token));
			_pg.AddRule(Rule("Default2", star2 + '.', Token));
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
				{
					public void Default1()
					{
						int la0, la1;
						for (;;) {
							la0 = LA0;
							switch (la0) {
							case 'A':
							case 'a': {
									Skip(); Match('x');
								}
								break;
							case 'b': {
									la1 = LA(1);
									if (la1 >= 'b' && la1 <= 'z')
										goto match3;
									else
										goto match2;
								}
							case -1:
							case '.':
								goto stop;
							default:
								if (la0 >= 'c' && la0 <= 'z')
									goto match3;
								else
									goto match2;
							}
							continue;
						match2: {
								la0 = LA0;
								if (la0 == 'B') {
									Skip(); Match('A'); Match('T');
								} else {
									Match('b'); Match('a'); Match('t');
								}
								Match('!');
							}
							continue;
						match3: {
								Skip(); MatchRange('b', 'z');
							}
						}
					stop:;
						Match('.');
					}
					public void Default2()
					{
						int la0, la1;
						for (;;) {
							switch (LA0) {
							case 'A':
							case 'a': {
									Skip();
									Match('x');
								}
								break;
							case 'b': {
									la1 = LA(1);
									if (la1 == 'a')
										goto match2;
									else
										goto match3;
								}
							case 'B':
								goto match2;
							case -1:
							case '.':
								goto stop;
							default:
								goto match3;
							}
							continue;
						match2: {
								la0 = LA0;
								if (la0 == 'B') {
									Skip(); Match('A'); Match('T');
								} else {
									Match('b'); Match('a'); Match('t');
								}
								Match('!');
							}
							continue;
						match3: {
								MatchRange('b', 'z'); MatchRange('b', 'z');
							}
						}
					stop:;
						Match('.');
					}
				}");
		}

		[Test]
		public void DifferentDefault2()
		{
			Rule at = Rule("At", C('@'), Token);
			Rule id = Rule("Id", Opt(C('@')) + Set("[a-zA-Z_]") + Star(Set("[a-zA-Z_0-9]"), true), Token);
			Rule @int = Rule("Int", Plus(Set("[0-9]")), Token);
			Alts a;
			Rule tokens = Rule("Tokens", a = Star(id / at / @int));
			a.DefaultArm = 0;
			at.IsPrivate = id.IsPrivate = @int.IsPrivate = true;
			_pg.AddRules(at, id, @int, tokens);
			
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				private void At()
				{
					Skip();
				}
				static readonly HashSet<int> Id_set0 = NewSetOfRanges('A', 'Z', '_', '_', 'a', 'z');
				static readonly HashSet<int> Id_set1 = NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z');
				private void Id()
				{
					int la0;
					la0 = LA0;
					if (la0 == '@')
						Skip();
					Match(Id_set0);
					for (;;) {
						la0 = LA0;
						if (Id_set1.Contains(la0))
							Skip();
						else
							break;
					}
				}
				private void Int()
				{
					int la0;
					Skip();
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
				}
				public void Tokens()
				{
					int la0, la1;
					for (;;) {
						la0 = LA0;
						if (la0 == '@') {
							la1 = LA(1);
							if (la1 >= 'A' && la1 <= 'Z' || la1 == '_' || la1 >= 'a' && la1 <= 'z')
								Id();
							else
								At();
						} else if (la0 >= '0' && la0 <= '9')
							Int();
						else if (la0 == -1)
							break;
						else
							Id();
					}
				}
			}");
		}

		[Test]
		public void SymbolTest()
		{
			// 10 PRINT "Hello" ; 20 GOTO 10
			Rule Stmt = Rule("Stmt", Sym("Number") + (Sym("print") + Sym("DQString") | Sym("goto") + Sym("Number")) + Sym("Newline"));
			Rule Stmts = Rule("Stmts", Star(Stmt), Start);
			_pg.CodeGenHelper = new GeneralCodeGenHelper(F.Id("Symbol"), false);
			_pg.AddRules(Stmt, Stmts);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
				{
					public void Stmt()
					{
						Symbol la0;
						Match(@@Number);
						la0 = LA0;
						if (la0 == @@print) {
							Skip();
							Match(@@DQString);
						} else {
							Match(@@goto);
							Match(@@Number);
						}
						Match(@@Newline);
					}
					public void Stmts()
					{
						Symbol la0;
						for (;;) {
							la0 = LA0;
							if (la0 == @@Number)
								Stmt();
							else
								break;
						}
					}
				}");
		}

		[Test]
		public void SymbolTestEx()
		{
			//   (break | continue | return) ';'
			//   (goto [case] | goto | return | throw | using) Expr ';'
			// | (do | checked | unchecked | try) Stmt
			// | (fixed | lock | switch | using | while | for | if) @@`(` Expr @@`)` Stmt
			Rule Expr = Rule("Expr", Sym("Id") | Sym("Number"));
			Rule Stmt = Rule("Stmt", Sym(""), Start);
			Rule TrivialStmt = Rule("TrivialStmt", Sym("break", "continue", "return"));
			Rule SimpleStmt = Rule("SimpleStmt", (Sym("goto") + Sym("case") | Sym("goto", "return", "throw", "using")) + Expr);
			Rule BlockStmt1 = Rule("BlockStmt1", Sym("do", "checked", "unchecked", "try") + Stmt);
			Rule BlockStmt2 = Rule("BlockStmt2", Sym("fixed", "lock", "switch", "using", "while", "for", "if") + Sym("(") + Expr + Sym(")") + Stmt);
			Stmt.Pred = (TrivialStmt | SimpleStmt | BlockStmt1 | BlockStmt2) + Sym(";");

			_pg.CodeGenHelper = new GeneralCodeGenHelper(F.Id("Symbol"), false);
			_pg.AddRules(Expr, Stmt, TrivialStmt, SimpleStmt, BlockStmt1, BlockStmt2);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"
				{
					public void Expr()
					{
						Match(@@Id, @@Number);
					}
					public void Stmt()
					{
						Symbol la0, la1;
						la0 = LA0;
						if (la0 == @@return) {
							la1 = LA(1);
							if (la1 == @@`;`)
								TrivialStmt();
							else
								SimpleStmt();
						} else if (la0 == @@break || la0 == @@continue)
							TrivialStmt();
						else if (la0 == @@using) {
							la1 = LA(1);
							if (la1 == @@Id || la1 == @@Number)
								SimpleStmt();
							else
								BlockStmt2();
						} else if (la0 == @@goto || la0 == @@throw)
							SimpleStmt();
						else if (la0 == @@checked || la0 == @@do || la0 == @@try || la0 == @@unchecked)
							BlockStmt1();
						else
							BlockStmt2();
						Match(@@`;`);
					}
					public void TrivialStmt()
					{
						Match(@@break, @@continue, @@return);
					}
					public void SimpleStmt()
					{
						Symbol la0, la1;
						la0 = LA0;
						if (la0 == @@goto) {
							la1 = LA(1);
							if (la1 == @@case) {
								Skip();
								Skip();
							} else
								Match(@@goto, @@return, @@throw, @@using);
						} else
							Match(@@goto, @@return, @@throw, @@using);
						Expr();
					}
					public void BlockStmt1()
					{
						Match(@@checked, @@do, @@try, @@unchecked);
						Stmt();
					}
					static readonly HashSet<Symbol> BlockStmt2_set0 = NewSet(@@fixed, @@for, @@if, @@lock, @@switch, @@using, @@while);
					public void BlockStmt2()
					{
						Match(BlockStmt2_set0);
						Match(@@`(`);
						Expr();
						Match(@@`)`);
						Stmt();
					}
				}");
		}

		[Test]
		public void InvertedIdSet()
		{
			_pg.AddRule(Rule("TokenLists", Star(Id("Semicolon", "Comma") / Plus(NotId("Semicolon", "Comma"), true)), Start));
			_pg.CodeGenHelper = new GeneralCodeGenHelper(F.Id("Symbol"), true);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void TokenLists()
				{
					Symbol la0;
					for (;;) {
						la0 = LA0;
						if (la0 == Comma || la0 == Semicolon)
							Skip();
						else if (!(la0 == Comma || la0 == EOF || la0 == Semicolon)) {
							Skip();
							for (;;) {
								la0 = LA0;
								if (!(la0 == Comma || la0 == EOF || la0 == Semicolon))
									Skip();
								else
									break;
							}
						} else
							break;
					}
				}
			}");
		}

		[Test]
		public void SimpleGateTest()
		{
			// rule Foo @[ ('a' &{cond} / _+ => "abc") 'd' ];
			Rule Foo = Rule("Foo", ((C('a') + And(F.Id("cond"))) / Gate(Plus(AnyCh), Seq("abc"))) + 'd', Start);
			Rule Bar = Rule("Bar", Gate(C('b'), Seq("bar")) / Set("[a-z]"), Token);
			_pg.AddRule(Foo);
			_pg.AddRule(Bar);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void Foo()
				{
					int la0, la1;
					do {
						la0 = LA0;
						if (la0 == 'a') {
							la1 = LA(1);
							if (la1 == 'd') {
								if (cond)
									Skip();
								else
									goto match2;
							} else
								goto match2;
						} else
							goto match2;
						break;
					match2:
						{
							Match('a');
							Match('b');
							Match('c');
						}
					} while (false);
					Match('d');
				}
				public void Bar()
				{
					int la0;
					la0 = LA0;
					if (la0 == 'b') {
						Skip();
						Match('a');
						Match('r');
					} else
						MatchRange('a', 'z');
				}
			}");
		}

		[Test]
		public void CrossRuleGateTest()
		{
			// token Number ==> @[ ('0'..'9' | '.' '0'..'9') =>
			//                     '0'..'9'* ('.' '0'..'9'+)? ];
			// token Tokens ==> @[ (Number / _)* ];
			var number = Rule("Number", Gate(Set("[0-9]") | '.' + Set("[0-9]"), 
			                            Star(Set("[0-9]")) + Opt('.' + Plus(Set("[0-9]")))), Token);
			var tokens = Rule("Tokens", Star(number / AnyCh), Token);
			_pg.AddRules(number, tokens);
			
			// Tokens won't bother looking at LA(1) without full LL(k) mode.
			// Another workaround is to add a '.' branch to Tokens, alongside «_»
			_pg.FullLLk = true;
			
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void Number()
				{
					int la0, la1;
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
					la0 = LA0;
					if (la0 == '.') {
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9') {
							Skip();
							Skip();
							for (;;) {
								la0 = LA0;
								if (la0 >= '0' && la0 <= '9')
									Skip();
								else
									break;
							}
						}
					}
				}
				public void Tokens()
				{
					int la0, la1;
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Number();
						else if (la0 == '.') {
							la1 = LA(1);
							if (la1 >= '0' && la1 <= '9')
								Number();
							else
								Skip();
						} else if (la0 != -1)
							Skip();
						else
							break;
					}
				}
			}");
		}


		[Test]
		public void ComplicatedAndPreds()
		{
			// So complicated I'm not completely sure that the result is correct. 
			// The important thing is, LLLPG doesn't crash anymore.
			// ( &{a} (&{b} {Foo();} | &{c})      // zero-width
			//   &{d} (&{e} ('w'|'W'))?           // optional 'w'|'W'
			//   (&{f} ('x'|'y') | 'X')           // ('x'|'y') or 'X'
			// | &{c} (&{f} ('w'|'y') | 'x') 'z'  // ('w'|'y') or 'x', then 'z'
			// | '!' )
			_pg.AddRule(Rule("Complicated",
				  And("a") + (And("b") + Stmt("Foo()") | And("c"))
				+ And("d") + Opt(And("e") + Set("[wW]"))
				+ (And("f") + Set("[xy]") | 'X')
				| And("c") + (And("f") + Set("[wy]") | 'x') + 'z'
				| '!'));
			LNode result = _pg.Run(_file);
		}

		[Test]
		public void SynPred1()
		{
			// token Number ==> @[ &('0'..'9'|'.')
			//                     '0'..'9'* ('.' '0'..'9'+)? ];
			Rule number = Rule("Number", And(Set("[0-9.]")) + Star(Set("[0-9]")) + Opt('.' + Plus(Set("[0-9]"))), Token);
			_pg.AddRule(number);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void Number()
				{
					int la0, la1;
					Check(Try_Number_Test0(0), ""[.0-9]"");
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
					la0 = LA0;
					if (la0 == '.') {
						la1 = LA(1);
						if (la1 >= '0' && la1 <= '9') {
							Skip();
							Skip();
							for (;;) {
								la0 = LA0;
								if (la0 >= '0' && la0 <= '9')
									Skip();
								else
									break;
							}
						}
					}
				}
				private bool Try_Number_Test0(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return Number_Test0();
				}
				private bool Number_Test0()
				{
					if (!TryMatchRange('.', '.', '0', '9'))
						return false;
					return true;
				}
			}");
		}

		[Test]
		public void SynPred2()
		{
			// public rule Tokens   ==> @[ (&Int => Int / Float / Id)* ];
			// private token Float  ==> @[ '0'..'9'* '.' '0'..'9'+ ];
			// private token Int    ==> @[ '0'..'9'+ ];
			// private token Id     ==> @[ ('a'..'z' | 'A'..'Z' | '_') ('a'..'z' | 'A'..'Z' | '_' | '0'..'9')* ];
			Rule Float, Int, Id;
			_pg.AddRule(Float = Rule("Float", Star(Set("[0-9]")) + '.' + Plus(Set("[0-9]"), true), Private));
			_pg.AddRule(Int = Rule("Int", Plus(Set("[0-9]"), true), Private));
			_pg.AddRule(Id = Rule("Id", Set("[a-zA-Z_]") + Star(Set("[0-9a-zA-Z_]"), true), Private));
			_pg.AddRule(Rule("Tokens", Star(Gate(And(Int) + AnyCh, Int) / Float / Id), Start));
			LNode result = _pg.Run(_file);
			// Note that Tokens calls Scan_Int when la0 is [a-zA-Z_], because LLLPG
			// does not understand the content of an and-predicate.
			CheckResult(result, @"{
				private void Float()
				{
					int la0;
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
					Match('.');
					MatchRange('0', '9');
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
				}
				private void Int()
				{
					int la0;
					MatchRange('0', '9');
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9')
							Skip();
						else
							break;
					}
				}
				private bool Try_Scan_Int(int lookaheadAmt)
				{
					using (new SavePosition(this, lookaheadAmt))
						return Scan_Int();
				}
				private bool Scan_Int()
				{
					int la0;
					if (!TryMatchRange('0', '9'))
						return false;
					for (;;) {
						la0 = LA0;
						if (la0 >= '0' && la0 <= '9') {
							if (!TryMatchRange('0', '9'))
								return false;
						} else
							break;
					}
					return true;
				}
				static readonly HashSet<int> Id_set0 = NewSetOfRanges('0', '9', 'A', 'Z', '_', '_', 'a', 'z');
				private void Id()
				{
					int la0;
					Skip();
					for (;;) {
						la0 = LA0;
						if (Id_set0.Contains(la0))
							Skip();
						else
							break;
					}
				}
				public void Tokens()
				{
					int la0;
					for (;;) {
						la0 = LA0;
						if (la0 == '.' || la0 >= '0' && la0 <= '9') {
							if (Try_Scan_Int(0))
								Int();
							else
								Float();
						} else if (la0 >= 'A' && la0 <= 'Z' || la0 == '_' || la0 >= 'a' && la0 <= 'z') {
							if (Try_Scan_Int(0))
								Int();
							else
								Id();
						} else if (la0 != -1)
							Int();
						else
							break;
					}
				}
			}");
		}

		[Test]
		public void RuleRefWithArgs()
		{
			Rule NTokens = Rule("NTokens", 
				Set("x", 0) + Opt(And(Expr("x < max")) + Plus(Set("[^\n\r ]"))) +
				             Star(And(Expr("x < max")) + C(' ') + Star(Set("[^\n\r ]")) + Stmt("x++"), true));
			NTokens.Basis = F.Def(F.Void, F._Missing, F.Tuple(F.Var(F.Int32, "max")));
			Rule Line = Rule("Line", SetVar("c", Set("[0-9]")) + Call(NTokens, Expr("c - '0'")) + Opt(Set("[\n\r]")));

			_pg.AddRules(NTokens, Line);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				void NTokens(int max)
				{
					int la0;
					x = 0;
					la0 = LA0;
					if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == ' ')) {
						Check(x < max, ""x < max"");
						Skip();
						for (;;) {
							la0 = LA0;
							if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == ' '))
								Skip();
							else
								break;
						}
					}
					for (;;) {
						la0 = LA0;
						if (la0 == ' ') {
							Check(x < max, ""x < max"");
							Skip();
							for (;;) {
								la0 = LA0;
								if (!(la0 == -1 || la0 == '\n' || la0 == '\r' || la0 == ' '))
									Skip();
								else
									break;
							}
							x++;
						} else
							break;
					}
				}
				public void Line()
				{
					int la0;
					var c = MatchRange('0', '9');
					NTokens(c - '0');
					la0 = LA0;
					if (la0 == '\n' || la0 == '\r')
						Skip();
				}
			}");
		}

		[Test]
		public void IfElseAmbig()
		{
			//private token IfStmt @[ 
			//	// "if" "(" Expr ")" Stmt ("else" Stmt)?
			//	TT.If TT.LParen Expr TT.RParen Stmt (TT.Else Stmt)?
			//];
			//private token Stmt @[
			//	IfStmt | Expr TT.Semicolon
			//];
			//private token Expr @[
			//	Id | Literal
			//];
			Rule Expr = Rule("Expr", Sym("Id") | Sym("Literal"));
			Rule Stmt = Rule("Stmt", Sym(""), Start);
			Rule IfStmt = Rule("IfStmt", Sym("If") + Sym("LParen") + Expr + Sym("RParen") + Stmt 
			                           + Opt(Sym("Else") + Stmt, true));
			Stmt.Pred = (IfStmt | Expr + Sym("Semicolon"));
			
			_pg.CodeGenHelper = new GeneralCodeGenHelper(F.Id("TT"), false);
			_pg.AddRules(IfStmt, Stmt, Expr);
			_expectingOutput = true;
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void IfStmt()
				{
					TT la0, la1;
					Match(@@If);
					Match(@@LParen);
					Expr();
					Match(@@RParen);
					Stmt();
					la0 = LA0;
					if (la0 == @@Else) {
						la1 = LA(1);
						if (la1 == @@Id || la1 == @@If || la1 == @@Literal) {
							Skip();
							Stmt();
						}
					}
				}
				public void Stmt()
				{
					TT la0;
					la0 = LA0;
					if (la0 == @@If)
						IfStmt();
					else {
						Expr();
						Match(@@Semicolon);
					}
				}
				public void Expr()
				{
					Match(@@Id, @@Literal);
				}
			}");
		}

		[Test]
		public void EmptyBranch()
		{
			// NOTE: oddity here. In order for an empty branch to work, one must use
			// Expr() rather than Stmt() because Stmt() is represented as an "empty 
			// statement" (S.Missing) with a [#trivia_rawTextBefore] attached. LLLPG 
			// sees this as an empty statement that it can eliminate, whereas Expr() 
			// is stored as a #rawText node which is not mistaken for an empty stmt.
			Rule BinaryOpt = Rule("BinaryOpt", 
				(Set("[0-1]") + Expr(@"Console.WriteLine(""binary!"");")) /
				(Expr(@"Console.WriteLine(""not binary!"");") + Seq()), Token);
			_pg.AddRule(BinaryOpt);
			LNode result = _pg.Run(_file);
			CheckResult(result, @"{
				public void BinaryOpt()
				{
					int la0;
					la0 = LA0;
					if (la0 >= '0' && la0 <= '1') {
						Skip();
						Console.WriteLine(""binary!"");
					} else
						Console.WriteLine(""not binary!"");
				}
			}");
		}
	}
}
