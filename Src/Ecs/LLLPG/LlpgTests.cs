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
		public static Alts Opt(Pred contents) { return Pred.Opt(contents); }
		public static Seq Plus(Pred contents) { return Pred.Plus(contents); }
		public static TerminalSet R(char lo, char hi) { return Pred.Range(lo, hi); }
		public static TerminalSet C(char c) { return Pred.Char(c); }
		public static TerminalSet Cs(params char[] c) { return Pred.Chars(c); }
		public static Rule Rule(string name, Pred contents) { return Pred.Rule(name, contents); }

		public LLParserGenerator _pg;

		public void CheckResult(Node result, string verbatim)
		{
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

		[TestFixtureSetUp]
		public void SetUp()
		{
			_pg = new LLParserGenerator();
		}

		partial class FooClass : BaseLexer
		{
			public void a()
			{
				Match('a', 'A');
			}
			public void b()
			{
				Match('b', 'B');
			}
			public void Foo()
			{
				Match('x');
				MatchRange('0', '9');
				MatchRange('0', '9');
			}
		}


		[Test]
		void Test1()
		{
			Rule Foo = Rule("Foo", 'x' + R('0', '9') + R('0', '9'));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("Test1.cs"));
			CheckResult(result, @"
				partial class FooClass
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
		void Test2()
		{
			Rule a = Rule("a", C('a') | 'A');
			Rule b = Rule("a", C('b') | 'B');
			Rule Foo = Rule("Foo", a | b);
			_pg.AddRules(new[] { a, b, Foo });
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("Test2.cs"));

			CheckResult(result, @"
				partial class FooClass {
					public void a()
					{
						Match('a', 'A');
					}
					public void b()
					{
						Match('b', 'B');
					}
					public void Foo()
					{
						int la0;
						la0 = LA(0);
						if (la0 == 'b' || la0 == 'B')
							b();
						else
							a();
					}
				}");
		}
		[Test]
		void Test3()
		{
			Rule a = Rule("a", C('a') | 'A');
			Rule b = Rule("a", C('b') | 'B');
			// public rule Foo ==> #[ (a | b? 'c')* ];
			Rule Foo = Rule("Foo", a | Opt(b) + 'c');
			_pg.AddRules(new[] { a, b, Foo });
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("Test3.cs"));

			CheckResult(result, @"
				partial class FooClass
				{
					public void a()
					{
						Match('a', 'A');
					}
					public void b()
					{
						Match('b', 'B');
					}
					public void Foo()
					{
						int la0;
						for (;;) {
							la0 = LA(0);
							if (la0 == 'a' || la0 == 'A')
								a();
							else if (la0 == 'b' || la0 == 'B' || la0 == 'c') {
								do {
									la0 = LA(0);
									if (la0 == 'b' || la0 == 'B')
										b();
								} while (false);
								Match('c');
							}
						}
					}
				}");
		}
		[Test]
		void Test4()
		{
			// public rule Foo ==> #[ 'a'..'z'+ | 'x' '0'..'9' '0'..'9' ];
			Rule Foo = Rule("Foo", Plus(R('a','z')) | 'x' + R('0','9') + R('0','9'));
			_pg.AddRule(Foo);
			Node result = _pg.GenerateCode(_("FooClass"), new EmptySourceFile("Test4.cs"));

			CheckResult(result, @"
				partial class FooClass
				{
					public void Foo()
					{
						int la0, la1;
						la0 = LA(0);
						int alt = 0;
						if (la0 == 'x') {
							la1 = LA(1);
							if (la1 >= '0' && '9' >= la1) {
								Match();
								Match();
								MatchRange('0', '9');
							} else
								alt = 1;
						}
						else
							alt = 1;
						if (alt == 1) {
							Match();
							for (;;) {
								la0 = LA(0);
								if (la0 >= 'a' && 'z' >= la0)
									Match();
								else
									break;
							}
						}
					}
				}");
		}
	}
}
