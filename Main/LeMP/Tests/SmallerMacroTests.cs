using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Ecs;

namespace LeMP.Tests
{
	/// <summary>Combined test suite for all the "smaller" macros that don't need 
	/// a lot of testing.</summary>
	[TestFixture]
	public class SmallerMacroTests : MacroTesterBase
	{
		[Test]
		public void ColonEquals()
		{
			TestEcs("x := new List<int>(100);",
			     "var x = new List<int>(100);");
		}

		[Test]
		public void TestUnless()
		{
			TestBoth("unless input.IsLowPriority { Process(input); };",
			       "unless (input.IsLowPriority) { Process(input); }",
			         "if  (!input.IsLowPriority) { Process(input); }");
		}

		[Test]
		public void TestSaveAndRestore()
		{
			TestEcs("saveAndRestore(Foo.Bar); F();",
				"var oldBar_1 = Foo.Bar; try { F(); } finally { Foo.Bar = oldBar_1; }"
				.Replace("oldBar_1", "oldBar_" + MacroProcessor.NextTempCounter));
			TestEcs("saveAndRestore(Foo.Bar = 5); F();",
				"var oldBar_1 = Foo.Bar; Foo.Bar = 5; try { F(); } finally { Foo.Bar = oldBar_1; }"
				.Replace("oldBar_1", "oldBar_" + MacroProcessor.NextTempCounter));
			TestEcs("void f() { saveAndRestore(Foo.Bar = 5); F(); }",
				@"void f() {
					var oldBar_1 = Foo.Bar; 
					Foo.Bar = 5; 
					try { 
						F();
					} finally {
						Foo.Bar = oldBar_1;
					}
				}".Replace("oldBar_1", "oldBar_" + MacroProcessor.NextTempCounter));
		}

		[Test]
		public void TestUsingMulti()
		{
			TestEcs("using System.Collections;", 
			        "using System.Collections;");
			TestEcs("using System(.Collections, .Collections.Generic, .Text, .Linq);",
			       @"using System.Collections;
			         using System.Collections.Generic;
			         using System.Text;
			         using System.Linq;");
			TestEcs("using System(, .Collections(, .Generic), .Text, .Linq);",
			       @"using System;
			         using System.Collections;
			         using System.Collections.Generic;
			         using System.Text;
			         using System.Linq;");
			TestEcs(@"/*Comment!*/
			         using System(.Linq, .Text);
			         /*Trailing comment!*/",
			       @"/*Comment!*/
			         using System.Linq;
			         using System.Text;
			         /*Trailing comment!*/");
		}

		[Test]
		public void TestThisConstructor()
		{
			TestEcs(@"
				namespace N {
					class Klass {
						public this() { Smile(); }
					}
				}", @"
				namespace N {
					class Klass {
						public Klass() { Smile(); }
					}
				}");
			TestEcs(@"
				class Derived<T> : Base<T> {
					public this(T value) : base(value) { }
				}", @"
				class Derived<T> : Base<T> {
					public Derived(T value) : base(value) { }
				}");
		}

		[Test]
		public void TestDotDotRanges()
		{
			TestEcs("A.B..C.D", "Range.ExcludeHi(A.B, C.D)");
			TestEcs("A.B...C.D", "Range.Inclusive(A.B, C.D)");
			TestEcs("..C.D", "Range.UntilExclusive(C.D)");
			TestEcs("_..C.D", "Range.UntilExclusive(C.D)");
			TestEcs("...C.D", "Range.UntilInclusive(C.D)");
			TestEcs("_...C.D", "Range.UntilInclusive(C.D)");
			TestEcs("A.B.._", "Range.StartingAt(A.B)");
			TestEcs("A.B..._", "Range.StartingAt(A.B)");
		}

		[Test]
		public void Test_in()
		{
			TestEcs("A + 1 in C.D;", "C.D.Contains(A + 1);");
			TestEcs("A.B in x..y;",  "A.B.IsInRangeExcludeHi(x, y);");
			TestEcs("A.C in x...y;", "A.C.IsInRange(x, y);");
			TestEcs("A.D in ..y;",   "A.D < y;");
			TestEcs("A.E in _..y;",  "A.E < y;");
			TestEcs("A.F in ...y;",  "A.F <= y;");
			TestEcs("A.G in _...y;", "A.G <= y;");
			TestEcs("A.H in x.._;",  "A.H >= x;");
			TestEcs("A.I in x..._;", "A.I >= x;");
			TestEcs("A.J in (x..y);", "Range.ExcludeHi(x, y).Contains(A.J);");
			TestEcs("A.K in (x...y);","Range.Inclusive(x, y).Contains(A.K);");
		}

		[Test]
		public void TestNullDot()
		{
			TestBoth(@"#importMacros(LeMP.CSharp6.To.OlderVersions); a = b?.c.d;",
					 @"#importMacros(LeMP.CSharp6.To.OlderVersions); a = b?.c.d;",
			          "a = (b != null ? b.c.d : null);");
			int n = MacroProcessor.NextTempCounter;
			TestEcs(@"#importMacros(LeMP.CSharp6.To.OlderVersions); a = B?.c.d;", 
			         "a = (([] var B_"+n+" = B) != null ? B_"+n+".c.d : null);");
			TestBoth(@"#importMacros(LeMP.CSharp6.To.OlderVersions); a = b?.c[d];",
					 @"#importMacros(LeMP.CSharp6.To.OlderVersions); a = b?.c[d];",
			          "a = (b != null ? b.c[d] : null);");
			TestEcs(@"#importMacros(LeMP.CSharp6.To.OlderVersions); a = b?.c?.d();",
			         "a = (b != null ? (b.c != null ? b.c.d() : null) : null);");
			TestBoth(@"#importMacros(LeMP.CSharp6.To.OlderVersions); return a.b?.c().d!x;",
			         @"#importMacros(LeMP.CSharp6.To.OlderVersions); return a.b?.c().d<x>;",
			          "return (a.b != null ? a.b.c().d<x> : null);");
		}

		[Test]
		public void TestNullDotWithVarDeclFactoredOut()
		{
			// Combine ?. with #ecs macro (#useSequenceExpressions)
			int n = MacroProcessor.NextTempCounter;
			TestEcs(@"#ecs; #importMacros(LeMP.CSharp6.To.OlderVersions); void F() { a = B?.c.d; }",
			          "void F() { var B_"+n+" = B; a = (([@`%isTmpVar`] B_"+n+") != null ? B_"+n+".c.d : null); }");
			n = MacroProcessor.NextTempCounter;
			TestEcs(@"#ecs; #importMacros(LeMP.CSharp6.To.OlderVersions); void F() { a = A.B?.c.d; }",
			         "void F() { var tmp_"+n+" = A.B; a = (([@`%isTmpVar`] tmp_" + n+") != null ? tmp_"+n+".c.d : null); }");
		}

		[Test]
		public void TestNullCoalesceSet()
		{
			TestBoth(@"a ??= (new A());", @"a ??= new A();",
			          "a = a ?? new A();");
		}

		[Test]
		public void TestTuples()
		{
			var import = "#importMacros(LeMP.CSharp7.To.OlderVersions); ";
			TestEcs(import + "#useDefaultTupleTypes();", "");
			TestBoth(import + "(1; a) + (2; a; b) + (3; a; b; c);",
					 import + "(1, a) + (2, a, b) + (3, a, b, c);",
			         "Tuple.Create(1, a) + Tuple.Create(2, a, b) + Tuple.Create(3, a, b, c);");
			TestBoth(import + "x::#!(String; DateTime) = (\"\"; DateTime.Now); y::#!(Y) = (new Y(););",
					 import + "#<String, DateTime> x = (\"\", DateTime.Now);     #<Y> y = (new Y(),);",
			         "Tuple<String, DateTime> x = Tuple.Create(\"\", DateTime.Now); Tuple<Y> y = Tuple.Create(new Y());");
			TestEcs(import + "#setTupleType(Sum, Sum); a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
					"a = Sum(1) + Sum(1, 2) + Sum(1, 2, 3, 4, 5);");
			TestEcs(import + "#setTupleType(Tuple); #setTupleType(2, Pair, Pair.Create);" +
			        "a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Tuple.Create(1) + Pair.Create(1, 2) + Tuple.Create(1, 2, 3, 4, 5);");
			
			TestBoth(import + "(a; b; c) = foo;", import + "(a, b, c) = foo;",
			        "a = foo.Item1; b = foo.Item2; c = foo.Item3;");
			TestEcs(import + "(var a, var b, c) = foo;",
			        "var a = foo.Item1; var b = foo.Item2; c = foo.Item3;");
			int n = MacroProcessor.NextTempCounter;
			TestEcs(import + "(a, b.c.d) = Foo;",
			        "var Foo_"+n+" = Foo; a = Foo_"+n+".Item1; b.c.d = Foo_"+n+".Item2;");
			n = MacroProcessor.NextTempCounter;
			TestEcs(import + "(a, b, c, d) = X.Y();",
			        "var tmp_1 = X.Y(); a = tmp_1.Item1; b = tmp_1.Item2; c = tmp_1.Item3; d = tmp_1.Item4;"
					.Replace("tmp_1", "tmp_"+n));
		}

		[Test]
		public void WithTest()
		{
			int n = MacroProcessor.NextTempCounter;
			TestEcs("with (foo) { .bar = .baz(#); }",
			        "{ var tmp_1 = foo; tmp_1.bar = tmp_1.baz(tmp_1); }".Replace("tmp_1", "tmp_" + n));
			
			// This test ensures with() doesn't act like it is in an expression context
			n = MacroProcessor.NextTempCounter;
			TestEcs("if (c) with (foo) { .bar = .baz(#); }",
			        "if (c) { var tmp_1 = foo; tmp_1.bar = tmp_1.baz(tmp_1); }".Replace("tmp_1", "tmp_" + n));
			
			// Ignore note about 'declined to process... with'
			using (MessageSink.SetDefault(_msgHolder)) {
				n = MacroProcessor.NextTempCounter;
				TestEcs(@"
					with (jekyll) { 
						.A = 1; 
						with(mr.hyde()) { x = .F(x); }
						with(.B + .C(.D));
					}", @"{
						var tmp_{0} = jekyll;
						tmp_{0}.A = 1;
						{
							var tmp_{1} = mr.hyde();
							x = tmp_{1}.F(x);
						}
						with(tmp_{0}.B + tmp_{0}.C(tmp_{0}.D));
					}".Replace("{0}", (n + 1).ToString()).Replace("{1}", (n).ToString()));
			}
		}

		[Test]
		public void WithExpressionTest()
		{
			TestEcs(@"#useSequenceExpressions; 
				void f() {
					if (with (new Person(""John Doe"")) { .Commit(dbConnection); }.IsSaved) 
						Yay();
				}", @"
				void f() {
					{ var tmp_{0} = new Person(""John Doe"");
					  tmp_{0}.Commit(dbConnection);
					  if (tmp_{0}.IsSaved)
						Yay();
					}
				}".Replace("{0}", MacroProcessor.NextTempCounter.ToString()));
		}

		
		/*[Test(Fails = "Macro not implemented")]
		public void ResultTest()
		{
			TestEcs("static int Square(int x) { x*x }",
			        "static int Square(int x) { return x*x; }");
			TestEcs("static int Abs(int x) { if (x >= 0) x else -x }",
			        "static int Abs(int x) { if (x >= 0) return x; else return -x; }");
			TestEcs("static int Smallr(int x) { if (x > 100) { while(x > 100) x /= 2; x } else { x - 1 } }",
			        "static int Smallr(int x) { if (x > 100) { while(x > 100) x /= 2; return x; } else { return x - 1; } }");
			TestEcs("static bool ToBool(bool? b) { " +
					  "if (b == null) throw new InvalidCastException(); else if (b) true else false }",
					"static bool ToBool(bool? b) { " +
					  "if (b == null) throw new InvalidCastException(); else if (b) return true; else return false; }");
			TestEcs(@"static string Ordinal(int x) { switch(x) { 
						case 1: {""first""} case 2: {""second""} case 3: {""third""} 
						case 4: {""fourth""} case 5: {""fifth""} case 6: {""sixth""} 
						case 7: {""seventh""} case 8: {""eighth""} case 9: {""ninth""}
						default: {""(not supported)""} 
					} }",
				   @"static string Ordinal(int x) { switch(x) { 
						case 1: {return ""first"";} case 2: {return ""second"";} case 3: {return ""third"";} 
						case 4: {return ""fourth"";} case 5: {return ""fifth"";} case 6: {return ""sixth"";} 
						case 7: {return ""seventh"";} case 8: {return ""eighth"";} case 9: {return ""ninth"";}
						default: {return ""(not supported)"";} 
					} }");
		}*/
		
		[Test]
		public void ForwardedMethodTest()
		{
			TestEcs("static void Exit() ==> Application.Exit;",
			        "static void Exit() { Application.Exit(); }");
			TestEcs("static int InRange(int x, int lo, int hi) ==> MathEx.InRange;",
			        "static int InRange(int x, int lo, int hi) { return MathEx.InRange(x, lo, hi); }");
			TestEcs("void Append(string fmt, params string[] args) ==> sb.AppendFormat;",
			        "void Append(string fmt, params string[] args) { sb.AppendFormat(fmt, args); }");
			TestEcs("void AppendFormat(string fmt, params string[] args) ==> sb._;",
					"void AppendFormat(string fmt, params string[] args) { sb.AppendFormat(fmt, args); }");
			TestEcs("internal int Count ==> _list.Count;",
					"internal int Count { get { return _list.Count; } }");
			TestEcs("internal int Count ==> _list._;",
					"internal int Count { get { return _list.Count; } }");
			TestEcs("internal int Count { get ==> _list._; set ==> _list._; }",
					"internal int Count { get { return _list.Count; } set { _list.Count = value; } }");
		}

		[Test]
		public void BackingFieldTest()
		{
			TestEcs("[field _name] public string Name { get; }",
			        "string _name; public string Name { get { return _name; } }");
			TestEcs("[protected field _name] public string Name { get; protected set; }",
			        "protected string _name; public string Name { get { return _name; } protected set { _name = value; } }");
			TestEcs("[field] public string Name { get; }",
			        "string _name; public string Name { get { return _name; } }");
			TestEcs("[[A] field _lives = 3] [B] public int LivesLeft { internal get; set; }",
			        "[A] int _lives = 3; [B] public int LivesLeft { internal get { return _lives; } set { _lives = value; } }");
			TestEcs("[field] public string Name { get; set { _name = value; } }",
			        "string _name; public string Name { get { return _name; } set { _name = value; } }");
			TestEcs("[[A] field] [B, C] public string Name { get; }",
			        "[A] string _name; [B, C] public string Name { get { return _name; } }");
			TestEcs("public string Name { get; protected set; }",
					"public string Name { get; protected set; }");
			TestEcs("[field string _name] public string Name { get; protected set; }",
					"string _name; public string Name { get { return _name; } protected set { _name = value; } }");
			TestEcs("[field List<T> L] T this[int x] { get; set; }",
					"List<T> L; T this[int x] { get { return L[x]; } set { L[x] = value; } }");
		}

		[Test]
		public void AssertTest()
		{
			TestLes("assert(condition);", 
			       @"System.Diagnostics.Debug.Assert(condition, ""Assertion failed in ``: condition"");");
			TestEcs("void Foo() { assert(condition); }", 
			       @"void Foo() { System.Diagnostics.Debug.Assert(condition, ""Assertion failed in `Foo`: condition""); }");
			TestEcs("int Num { set { assert(condition); } }", 
			       @"int Num { set { System.Diagnostics.Debug.Assert(condition, ""Assertion failed in `Num`: condition""); } }");
			TestEcs(@"class Foo<T> : IFoo {
			            int IFoo.Num { 
			                set { assert(condition); }
			            }
			       }",
			       @"class Foo<T> : IFoo {
			            int IFoo.Num {
			                set { System.Diagnostics.Debug.Assert(condition, ""Assertion failed in `Foo<T>.Num`: condition""); }
			            }
			       }");
			TestEcs(@"interface IFoo<T> {
			            void Foo() { 
			                assert(condition);
			            }
			       }",
			       @"interface IFoo<T> {
			            void Foo() { 
			                System.Diagnostics.Debug.Assert(condition, ""Assertion failed in `IFoo<T>.Foo`: condition"");
			            }
			       }");
			TestEcs(@"struct Foo {
			            event EventHandler Ev { 
			                add { assert(condition); }
			            }
			       }",
			       @"struct Foo {
			            event EventHandler Ev { 
			                add { System.Diagnostics.Debug.Assert(condition, ""Assertion failed in `Foo.Ev`: condition""); }
			            }
			       }");
			TestEcs("#snippet #assertMethod = Contract.Assert; assert(condition);", 
			       @"Contract.Assert(condition, ""Assertion failed in ``: condition"");");
		}


		[Test]
		public void TestStringify()
		{
			TestBoth(@"s = stringify(hello);", @"s = stringify(hello);",  @"s = ""hello"";");
			TestEcs (@"s = stringify(A.B<C>(D));",                        @"s = ""A.B<C>(D)"";");
		}

		[Test]
		public void TestNameOf()
		{
			TestBoth(@"#importMacros(LeMP.CSharp6.To.OlderVersions); s = nameof(hello);",
					 @"#importMacros(LeMP.CSharp6.To.OlderVersions); s = nameof(hello);",     @"s = ""hello"";");
			TestBoth(@"#importMacros(LeMP.CSharp6.To.OlderVersions); s = nameof(A.B!C(D));",
					 @"#importMacros(LeMP.CSharp6.To.OlderVersions); s = nameof(A.B<C>(D));", @"s = ""B"";");
		}

		[Test]
		public void TestConcat()
		{
			TestBoth(@"@1stprog = hello `##` world;", @"@1stprog = hello `##` world;", @"@1stprog = helloworld;");
			TestLes(@"##(call, ""_func_"", with(argument));", @"call_func_with(argument);");
		}

		[Test]
		public void TestTreeEqualsAndStaticIf()
		{
			TestEcs(@"bool b = a `code==` 'A';",                        @"bool b = false;");
			TestEcs(@"bool b = f(x) `code==` f(x /*comment*/);",        @"bool b = true;");
			TestEcs(@"a(); static if (true)  { b(); c(); } d();",       @"a(); b(); c(); d();");
			TestEcs(@"a(); static if (false) { b(); c(); } d();",       @"a(); d();");
			TestEcs(@"a(); static if (a `code==` 'A') {{ b(); }} c();", @"a(); c();");
			TestEcs(@"a(); static if (foo() `code==` foo(2)) b(); else {{ c(); }} d();", @"a(); { c(); } d();");
			TestEcs(@"a(); static if (foo.bar<baz> `code==` foo.bar<baz>) b(); else c(); d();", @"a(); b(); d();");
			TestEcs(@"c = static_if(false, see, C);", @"c = C;");
			TestEcs(@"#set Flag; a(); static if (#get(Flag, false)) b();
			                          static if (#get(Nada, false)) c(); d();",
			        @"a(); b(); d();");
			TestEcs(@"#set #inputFile = ""Foo.cs"";
				static if (#get(#inputFile) `code==` ""Foo.cs"")
					WeAreInFoo();
				else
					ThisIsNotFoo();
				", "WeAreInFoo();");
		}

		[Test]
		public void TestStaticIfBooleanExprs()
		{
			TestEcs(@"static if (true || false) { T; } else { f; }",    "T;");
			TestEcs(@"static if (true || true)  { T; } else { f; }",    "T;");
			TestEcs(@"static if (false || true) { T; } else { f; }",    "T;");
			TestEcs(@"static if (false | false) { T; } else { f; }",    "f;");
			TestEcs(@"static if (true && false) { T; } else { f; }",    "f;");
			TestEcs(@"static if (true && true)  { T; } else { f; }",    "T;");
			TestEcs(@"static if (false && true) { T; } else { f; }",    "f;");
			TestEcs(@"static if (false & false) { T; } else { f; }",    "f;");
			TestEcs(@"static if (!true) { T; } else { f; }",            "f;");
			TestEcs(@"static if (~false) { T; } else { f; }",           "T;");
			TestEcs(@"static if (!false || ~true) { T; } else { f; }",  "T;");
			TestEcs(@"static if (!false && ~false) { T; } else { f; }", "T;");
			TestEcs(@"static if (~false && false && !false) { T; } else { f; }", "f;");
		}

		[Test]
		public void StaticMatches_WithStaticIf()
		{
			TestEcs(@"static if (xx `staticMatches` $x) {}"+
			        @"static if (Pie(""apple"") `staticMatches` Pie($x)) { Cake($x); }", @"Cake(""apple"");");
			TestEcs(@"static if (xx `staticMatches` $x) {}"+
			        @"static if (!(Pie(""apple"") `staticMatches` Cake($x))) { $x; }", @"xx;");
		}

		[Test]
		public void EcsMacro()
		{
			// #ecs = #splice(#useSymbols, #useSequenceExpressions)
			TestCs(@"#ecs;
				public class Program {
					public static void Main(string[] args) {
						Commit(Lookup(@@foo)::x);
					}
				}", @"
				public class Program {
					static readonly Symbol sy_foo = (Symbol)""foo"";
					public static void Main(string[] args) {
						var x = Lookup(sy_foo);
						Commit(x);
					}
				}");
		}

		[Test]
		public void MacroScope()
		{
			TestEcs("macro_scope { define Foo() { Fool(); } Foo(); } Foo();",
			        "Fool(); Foo();");
		}

		[Test]
		public void ResetMacros()
		{
			TestEcs(@"define Foo() { Food(); } reset_macros { define Foo() { Fool(); } Foo(); } Foo();",
			        @"Fool(); Food();");
			TestEcs(@"#set Foo; "+
			        @"static if (#get(Foo)) Defined(); "+
			        @"reset_macros { static if (#get(Foo, false)) Error(); }"+
			        @"static if (#get(Foo)) StillDefined(); ",
			        @"Defined(); StillDefined();");
		}
	}
}
