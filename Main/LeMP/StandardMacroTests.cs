using Ecs.Parser;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP
{
	[TestFixture]
	public class StandardMacroTests
	{
		IMessageSink _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		[Test]
		public void TestNullDot()
		{
			TestBoth(@"a = b.c?.d;", @"a = b.c?.d;",
			          "a = b.c != null ? b.c.d : null;");
			TestEcs(@"a = b.c?.d.e;",
			         "a = b.c != null ? b.c.d.e : null;");
			TestBoth(@"a = b?.c[d];", @"a = b?.c[d];",
			          "a = b != null ? b.c[d] : null;");
			TestEcs(@"a = b?.c?.d();",
			         "a = b != null ? b.c != null ? b.c.d() : null : null;");
			TestBoth(@"return a.b?.c().d!x;", @"return a.b?.c().d<x>;",
			          "return a.b != null ? a.b.c().d<x> : null;");
		}

		[Test]
		public void TestTuples()
		{
			TestEcs("use_default_tuple_types();", "");
			TestBoth("(1, a) + (2, a, b) + (3, a, b, c);",
			         "(1, a) + (2, a, b) + (3, a, b, c);",
			         "Pair.Create(1, a) + Tuple.Create(2, a, b) + Tuple.Create(3, a, b, c);");
			TestBoth("x::#!(String, DateTime) = ('''''', DateTime.Now); y::#!(Y) = (new Y(),);",
			         "#<String, DateTime> x = (\"\", DateTime.Now);     #<Y> y = (new Y(),);",
			         "Pair<String, DateTime> x = Pair.Create(\"\", DateTime.Now); Tuple<Y> y = Tuple.Create(new Y());");
			TestEcs("set_tuple_type(Sum, Sum); a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Sum(1) + Sum(1, 2) + Sum(1, 2, 3, 4, 5);");
			TestEcs("set_tuple_type(Tuple); set_tuple_type(2, Pair, Pair.Create);"+
			        "a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Tuple.Create(1) + Pair.Create(1, 2) + Tuple.Create(1, 2, 3, 4, 5);");
			
			TestBoth("(a, b, c) = foo;", "(a, b, c) = foo;",
			        "a = foo.Item1; b = foo.Item2; c = foo.Item3;");
			int n = StandardMacros.NextTempCounter;
			TestEcs("(a, b.c.d) = Foo;",
			        "var tmp_"+n+" = Foo; a = tmp_"+n+".Item1; b.c.d = tmp_"+n+".Item2;");
			n = StandardMacros.NextTempCounter;
			TestEcs("(a, b, c, d) = X.Y();",
			        "var tmp_"+n+" = X.Y(); a = tmp_"+n+".Item1; b = tmp_"+n+".Item2; c = tmp_"+n+".Item3; d = tmp_"+n+".Item4;");
		}

		[Test]
		public void WithTest()
		{
			int n = StandardMacros.NextTempCounter;
			TestEcs("with (foo) { .bar = .baz(); }",
			        "{ var tmp_"+n+" = foo; tmp_"+n+".bar = tmp_"+n+".baz(); }");
			n = StandardMacros.NextTempCounter;
			TestEcs(@"with (jekyll) { 
						.A = 1; 
						with(mr.hyde()) { x = .F(x); }
						with(.B + .C(.D));
					}", string.Format(@"{{
						var tmp_{0} = jekyll;
						tmp_{0}.A = 1;
						{{
							var tmp_{1} = mr.hyde();
							x = tmp_{1}.F(x);
						}}
						with(tmp_{0}.B + tmp_{0}.C(tmp_{0}.D));
					}}", n + 1, n));
		}

		[Test]
		public void TestCodeQuote()
		{
			TestEcs("quote { F(); }",
				   @"F.Call(""F"");");
			TestEcs("quote(F(x, 0));",
				   @"F.Call(""F"", F.Id(""x""), F.Literal(0));");
			TestEcs("quote { x = x + 1; }",
				   @"F.Call(CodeSymbols.Assign, F.Id(""x""), F.Call(CodeSymbols.Add, F.Id(""x""), F.Literal(1)));");
			TestEcs("quote { Console.WriteLine(\"Hello\"); }",
				   @"F.Call(F.Dot(F.Id(""Console""), F.Id(""WriteLine"")), F.Literal(""Hello""));");
			TestEcs("q = quote({ while (Foo<T>) Yay(); });",
				   @"q = F.Call(CodeSymbols.While, F.Of(F.Id(""Foo""), F.Id(""T"")), F.Call(""Yay""));");
			TestEcs("q = quote({ if (true) { Yay(); } });",
				   @"q = F.Call(CodeSymbols.If, F.Literal(true), F.Braces(F.Call(""Yay"")));");
			TestEcs("q = quote({ Yay(); break; });",
				   @"q = F.Braces(F.Call(""Yay""), F.Call(CodeSymbols.Break));");
			TestEcs("q = quote({ $(dict[key]) = 1; });",
				   @"q = F.Call(CodeSymbols.Assign, dict[key], F.Literal(1));");
			TestEcs("q = quote(hello + $x);",
				   @"q = F.Call(CodeSymbols.Add, F.Id(""hello""), x);");
		}

		[Test]
		public void TestStringInterpolation()
		{
			Assert.Fail("TODO");
		}

		[Test]
		public void SetMemberTest()
		{
			var old = _sink;
			_sink = MessageSink.Trace;
			try {
				TestEcs("void Set(set int X);", "void Set(set int X);"); // Body required
			} finally { _sink = old; }
			TestEcs("void Set(set int X, set int Y) {}",
				"void Set(int x, int y) { X = x; Y = y; }");
			TestEcs("void Set(public int X, bool Y, private string Z) { if (Y) Rejoice(); }",
				@"public int X; private string Z; 
				void Set(int x, bool Y, string z) {
					X = x; Z = z; if (Y) Rejoice();
				}");
			TestEcs(
				@"void Set(
					[Spanish] set int _hola, 
					[English] static int _hello, 
					[Alzheimer's] partial long goodbye = 8, 
					[Hawaii] protected internal string Aloha = 5,
					[French] internal string _Bonjour = 7,
					[Other] readonly int _ciao = 4) { Foo(_ciao); }",
				@"
				[English] static int _hello;
				[Alzheimer's] partial long goodbye;
				[Hawaii] protected internal string Aloha;
				[French] internal string _Bonjour;
				void Set(
					[Spanish] int hola, 
					int hello, 
					long goodbye = 8, 
					string aloha = 5,
					string Bonjour = 7,
					[Other] readonly int _ciao = 4)
				{
					_hola = hola;
					_hello = hello;
					this.goodbye = goodbye;
					Aloha = aloha;
					_Bonjour = Bonjour;
					Foo(_ciao);
				}");
			TestEcs(@"class Point { 
				public Point(public int X, public int Y) {}
				public this(set int X, set int Y) {}
			}", @"class Point {
				public int X;
				public int Y;
				public Point(int x, int y) { X = x; Y = y; }
				public this(int x, int y) { X = x; Y = y; }
			}");
		}
		
		[Test]
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
		}
		
		[Test]
		public void ForwardedMethodTest()
		{
			TestEcs("static void Exit() ==> Application.Exit;",
			        "static void Exit() { Application.Exit(); }");
			TestEcs("static int InRange(int x, int lo, int hi) ==> MathEx.InRange;",
			        "static int InRange(int x, int lo, int hi) { return MathEx.InRange(x, lo, hi); }");
			TestEcs("void Append(string fmt, params string[] args) ==> sb.AppendFormat;",
			        "void Append(string fmt, params string[] args) { sb.AppendFormat(fmt, args); }");
			TestEcs("void AppendFormat(string fmt, params string[] args) ==> sb.#;",
					"void AppendFormat(string fmt, params string[] args) { sb.AppendFormat(fmt, args); }");
			TestEcs("internal int Count ==> _list.Count;",
					"internal int Count { get { return _list.Count; } set { _list.Count = value; } }");
			TestEcs("internal int Count ==> _list.#;",
					"internal int Count { get { return _list.Count; } set { _list.Count = value; } }");
			TestEcs("internal int Count { get ==> _list.#; set ==> _list.#; }",
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
		}

		[Test]
		public void Test_on_finally()
		{
			TestEcs(@"{ Foo(); on_finally { Console.WriteLine(""Finally!""); } Bar(); }",
					@"{ Foo(); try { Bar(); } finally { Console.WriteLine(""Finally!""); } }");
			TestEcs(@"{ x++; on_finally { x--; Foo(); } DoSomeStuff(); Etc(); }",
					@"{ x++; try { DoSomeStuff(); Etc(); } finally { x--; Foo(); } }");
			// Alternate syntax from D
			TestEcs(@"scope(exit) { Leaving(); } while(_fun) KeepDoingIt();",
			        @"try { while(_fun) KeepDoingIt(); } finally { Leaving(); }");
		}
		[Test]
		public void Test_on_catch_on_throw()
		{
			TestEcs(@"{ bool ok = true; on_catch { ok = false; } DoSomeStuff(); Etc(); }",
					@"{ bool ok = true; try { DoSomeStuff(); Etc(); } catch { ok = false; } }");
			TestEcs(@"{ _crashed = false; on_catch { _crashed = true; } DoSomeStuff(); }",
					@"{ _crashed = false; try { DoSomeStuff(); } catch { _crashed = true; } }");
			TestEcs(@"{ on_catch(ex) { MessageBox.Show(ex.Message); } Etc(); }",
					@"{ try { Etc(); } catch(Exception ex) { MessageBox.Show(ex.Message); } }");
			TestEcs(@"on_throw(ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(Exception ex) { MessageBox.Show(ex.Message); throw; }");
			TestEcs(@"on_catch(FormatException ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(FormatException ex) { MessageBox.Show(ex.Message); }");
			TestEcs(@"on_throw(FormatException ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(FormatException ex) { MessageBox.Show(ex.Message); throw; }");
		}

		[Test]
		public void Test_on_return()
		{
			TestEcs(@"{ on_return(R) { Log(R); } Foo(); return Bar(); }",
			        @"{ Foo(); { var R = Bar(); Log(R); return R; } }");
			TestEcs(@"{ on_return(r) { r++; } Foo(); return x > 0 ? x : -x; }",
					@"{ Foo(); { var r = x > 0 ? x : -x; r++; return r; } }");
			TestEcs(@"{ on_return { Log(""return""); } if (true) return 5; else return; }",
			        @"{ if (true) { var __result__ = 5; Log(""return""); return __result__; } else { Log(""return""); return; } }");
			TestEcs(@"on_return(int r = 5) { r++; } return;",
			        @"{ int r = 5; r++; return r; }");
			TestEcs(@"on_return(int r) { r++; } return 5;",
			        @"{ int r = 5; r++; return r; }");
		}

		[Test]
		public void RequireTest()
		{
			// "[requires] is currently not supported for methods that do not have a body (e.g. interface methods)"
			// "[ensures] is currently not supported for methods that do not have a body (e.g. interface methods)"
			TestEcs("[requires(t != null)]"+
			       @"public void Wait(Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Contract.Requires<ArgumentNullException>(t != null, ""Wait() requires t != null""); t.Wait(); }");
			TestEcs("public void Wait([requires(# != null)] Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Contract.Requires(t != null, ""Wait() requires t != null""); t.Wait(); }");
			TestEcs("public void Wait([required] Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Contract.Requires(t != null, ""Wait() requires t != null""); t.Wait(); }");
			TestEcs("void Wait([requires(# != null)] Task<T> t) { t.Wait(); }",
			       @"void Wait(Task<T> t) { Contract.Requires(t != null, ""t != null""); t.Wait(); }");
			TestEcs("[assert(t != null)]"+
			       @"public void Wait(Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Debug.Assert(t != null, ""Wait() expects t != null""); t.Wait(); }");
			TestEcs(@"[assert(x >= min, ""x is too low"")]"+
			        @"[assert(x <= max, ""x is too high"")]"+
			        @"public void AssertRange(int x, int min, int max) { }",
			        @"public void AssertRange(int x, int min, int max) {"+
			        @"  Debug.Assert(x >= min, ""x is too low""); Debug.Assert(x <= max, ""x is too high""); }");
		}
		[Test]
		public void EnsuresTest()
		{
			// TODO: support ensures-on-throw
			// TODO: support async (Task Result) ensures
			TestEcs(@"[ensures(# >= 0)]
			          public static int Square(int x) { 
			            return x*x;
			          }",
			        @"public static int Square(int x) {
			            var @return = x*x; 
			            Contract.Assert(x >= 0, ""Square() failed to ensure (result >= 0)""); 
			            return @return;
			          }");
			TestEcs(@"public static Node Root { 
			            [ensures(# != null, ""Internal error"")] 
			            get { return _root; }
			          }",
			        @"public static Node Root { get {
			            var @return = _root; 
			            Contract.Assert(@return != null, ""Internal error"");
			            return @return;
			          } }");
			TestEcs(@"[ensures(File.Exists(filename), ""Gimme a friggin' break!""]
			          void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!"");
			          }",
			        @"void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!""); 
			            Contract.Assert(File.Exists(filename), ""Gimme a friggin' break!"");
			          }");
			TestEcs(@"[assert_ensures(comp(lo, hi) <= 0)]
				public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp) {
					if (comp(lo, hi) > 0) {
						Swap(ref lo, ref hi);
						return true;
					}
					return false;
				}", @"
				public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp) {
					if (comp(lo, hi) > 0) {
						Swap(ref lo, ref hi);
						Debug.Assert(comp(lo, hi) <= 0, ""SortPair() failed to ensure comp(lo, hi) <= 0"");
						return true;
					}
					Debug.Assert(comp(lo, hi) <= 0, ""SortPair() failed to ensure comp(lo, hi) <= 0"");
					return false;
				}");
		}

		[Test]
		public void MixingFeatures()
		{
			TestEcs("[requires(x >= 0)] static double Sqrt(double x) ==> Math.Sqrt;",
			       @"static double Sqrt(double x) { Contract.Requires(x >= 0, ""Sqrt() requires x >= 0""); return Math.Sqrt(x); }");
			TestEcs("[field _foo] public List<int> Foo { get; [required] set; }",
			       @"List<int> _foo; public List<int> Foo { "+
			       @"  get { return _foo; } "+
			       @"  set { Contract.Requires(value != null, ""Foo requires value != null""); _foo = value; }");
			TestEcs("[field _foo] public List<int> Foo { [ensures(# != null)] get; [requires(# != null)] set; }",
			       @"List<int> _foo; public List<int> Foo { "+
			       @"  get { var @return = _foo; Contract.Assert(); return @return; } "+
			       @"  set { Contract.Requires(value != null, ""Foo requires value != null""); _foo = value; }");
			string result = 
			    @"public static Node Root { get {
			        var @return = _root; 
			        Contract.Assert(@return != null, ""Internal error"");
			        return @return;
			      } }";
			TestEcs(@"
				public static Node Root { 
					[ensures(# != null, ""Internal error"")] 
					get { _root; }
				}", result);
			TestEcs(@"[field _root]
				public static Node Root { 
					[ensures(# != null, ""Internal error"")] 
					get;
				}",
				"Node _root; " + result);
		}

		[Test]
		public void TestNameOf()
		{
			TestBoth(@"s = nameof(hello);", @"s = nameof(hello);", @"s = ""hello"";");
			TestBoth(@"s = nameof(f(x, 'y'));", @"s = nameof(f(x, 'y'));", @"s = ""f(x, 'y')"";");
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
			TestEcs(@"bool b = a `tree==` 'A';",                        @"bool b = false;");
			TestEcs(@"bool b = f(x) `tree==` f(x /*comment*/);",        @"bool b = true;");
			TestEcs(@"a(); static if (true)  { b(); c(); } d();",       @"a(); b(); c(); d();");
			TestEcs(@"a(); static if (false) { b(); c(); } d();",       @"a(); d();");
			TestEcs(@"a(); static if (a `tree==` 'A') {{ b(); }} c();", @"a(); c();");
			TestEcs(@"a(); static if (foo() `tree==` foo(2)) b(); else {{ c(); }} d();", @"a(); { c(); } d();");
			TestEcs(@"a(); static if (foo.bar<baz> `tree==` foo.bar<baz>) b(); else c(); d();", @"a(); b(); d();");
			TestEcs(@"c = static_if(false, see, C);", @"c = C;");
		}

		[Test]
		public void TestUnroll()
		{
			TestLes(@"unroll X \in (X, Y) { sum += X; }",
			          "sum += X; sum += Y;");
			TestBoth(@"unroll X \in (X, Y) { sum += X; }",
			          "unroll (X in (X, Y)) { sum += X; }",
			          "sum += X; sum += Y;");
			TestEcs("unroll (X in (X, Y)) { sum += X; Console.WriteLine(sum); }",
			        "sum += X; Console.WriteLine(sum);"+
			        "sum += Y; Console.WriteLine(sum);");
			TestEcs("unroll (X in (A(), [Oof] B)) { X(X, [Foo] X); }",
			        "A()(A(), [Foo] A());"+
			        "([Oof] B)([Oof] B, [Foo, Oof] B);");
			TestEcs("unroll ((type, name) in ((int, x), (uint, y), (float, z))) { type name = 0; }",
			        "int x = 0; "+
			        "uint y = 0; "+
			        "float z = 0;");
			TestEcs("unroll (X in (int X = 41, X++, Console.WriteLine(X))) { X; }",
			        "int X = 41; X++; Console.WriteLine(X);");
		}

		[Test]
		public void TestReplace_basics()
		{
			// Simple cases
			TestLes(@"replace(nothing => nobody) {nowhere;}", "nowhere;");
			TestLes(@"replace(a => b) {a;}", "b;");
			TestLes(@"replace(7 => seven) {x = 7;}", "x = seven;");
			TestLes(@"replace(7() => ""seven"") {x = 7() + 7;}", @"x = ""seven"" + 7;");
			TestLes(@"replace(a => b) {[Hello] a; a(a);}", "[Hello] b; b(b);");
			TestLes("replace(MS => MessageSink, C => Current, W => Write, S => Severity, B => \"Bam!\")\n"+
			        "    { MS.C.W(S.Error, @null, B); }",
			        @"MessageSink.Current.Write(Severity.Error, @null, ""Bam!"");");
			TestLes(@"replace(Write => Store, Console.Write => Console.Write)"+
			        @"{ Write(x); Console.Write(x); }", "Store(x); Console.Write(x);");
			
			// Swap
			TestLes("replace(foo => bar, bar => foo) {foo() = bar;}", "bar() = foo;");
			TestLes("replace(a => 'a', 'a' => A, A => a) {'a' = A - a;}", "A = a - 'a';");

			// replace(Foo => Bar, System.Foo => System.Foo)

			// Captures
			TestBoth("replace(input($capture) => output($capture)) { var i = 21; input(i * 2); };",
			         "replace(input($capture) => output($capture)) { var i = 21; input(i * 2); }",
			         "var i = 21; output(i * 2);");
		}

		[Test]
		public void TestReplace_params()
		{
			TestEcs("replace({ $(params before); on_exit { $(params command); } $(params after); } =>\n"+
			        "        { $before;          try  { $after; } finally { $command; }         }) \n"+
			        "{{ var foo = new Foo(); on_exit { foo.Dispose(); } Combobulate(foo); return foo; }}",
			        " { var foo = new Foo(); try { Combobulate(foo); return foo; } finally { foo.Dispose(); } }");
			
			TestLes("replace($($format, $([#params] args)) => String.Format($format, $args) )\n"+
			        @"   { MessageBox.Show($(""I hate {0}"", noun)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}"", noun));");
			TestLes("replace($($format, $([#params] args)) => String.Format($format, $args) )\n"+
			        @"   { MessageBox.Show($(""I hate {0}ing {1}s"", verb, noun), $(""FYI"",)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}ing {1}s"", verb, noun), String.Format(""FYI""));");
		}

		[Test]
		public void TestReplace_match_attributes()
		{
			// [foo] a([attr] Foo) `MatchesPattern` 
			// [#trivia_, bar] a([$attr] $foo)
			
			// ([foo] F([x] X, [y] Y, [a1(...), a2(...)] Z) `MatchesPattern`
			//        F(X, $Y, $(params P), [$A, a1($(params args))] $Z)) == false cuz [x] is unmatched

			// TODO
		}

		[Test]
		public void TestReplaceInTokenTree()
		{
			TestLes("replace(foo => bar, bar => foo) { foo(@[ foo(*****) ]); }", "bar(@[ bar(*****) ]);");
			TestLes("replace(foo => bar, bar => foo) { foo(@[ foo(%bar%) ]); }", "bar(@[ bar(%foo%) ]);");
			TestLes("replace(foo => bar, bar => foo) { foo(@[ ***(%bar%) ]); }", "bar(@[ ***(%foo%) ]);");
			TestLes(@"unroll((A, B) `in` ((Eh, Bee), ('a', ""b""), (1, 2d))) { foo(B - A, @[A + B]); }",
				@"foo(Bee - Eh,  @[Eh + Bee]);
				  foo(""b"" - 'a', @['a' + ""b""]);
				  foo(2d - 1, @[1 + 2d]);");
		}

		[Test]
		public void TestReplace_advanced()
		{
			// Nested replacements
			TestEcs(@"replace(X => Y, X($(params p)) => X($p)) { X = X(X, Y); }",
			        @"Y = X(Y, Y);");
			// Note: $a * $b doesn't work because it is seen as a variable decl
			TestEcs(@"replace(($a + $b + $c) => Add($a, $b, $c), ($a`*`$b) => Mul($a, $b))
			          { var y = 2*x*2 + 3*x + 4; }", 
			        @"var y = Add(Mul(Mul(2, x), 2), Mul(3, x), 4);");

			//TestEcs(@"replace(TO=>DO) {}", @"");
		}

		[Test]
		public void TestReplace_RemainingNodes()
		{
			TestEcs(@"{ replace(C => Console, WL => WriteLine); string name = ""Bob""; C.WL(""Hi ""+name); }",
			        @"{ string name = ""Bob""; Console.WriteLine(""Hi ""+name); }");
		}

		private void TestLes(string input, string outputLes, int maxExpand = 0xFFFF)
		{
			Test(input, LesLanguageService.Value, outputLes, LesLanguageService.Value, maxExpand);
		}
		private void TestEcs(string input, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(input, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		private void TestBoth(string inputLes, string inputEcs, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(inputLes, LesLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
			Test(inputEcs, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		private void Test(string input, IParsingService inLang, string expected, IParsingService outLang, int maxExpand = 0xFFFF)
		{
			var lemp = NewLemp(maxExpand);
			var inputCode = new RVList<LNode>(inLang.Parse(input, _sink));
			var results = lemp.ProcessSynchronously(inputCode);
			var expectCode = outLang.Parse(expected, _sink);
			if (!results.SequenceEqual(expectCode))
			{	// TEST FAILED, print error
				string resultStr = results.Select(n => outLang.Print(n, _sink)).Join("\n");
				Assert.AreEqual(TestCompiler.StripExtraWhitespace(expected), 
				                TestCompiler.StripExtraWhitespace(resultStr));
			}
		}
		MacroProcessor NewLemp(int maxExpand)
		{
			var lemp = new MacroProcessor(typeof(LeMP.Prelude.Macros), _sink);
			lemp.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			lemp.AddMacros(typeof(LeMP.StandardMacros));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
			lemp.MaxExpansions = maxExpand;
			return lemp;
		}
	}
}
