using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Ecs;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestSequenceExpressionMacro : MacroTesterBase
	{
		[Test]
		public void TestBasics()
		{
			// Check that it doesn't do anything when there's nothing to do.
			TestEcs("#useSequenceExpressions; " +
				"void F() { { f(); } } " +
				"int P { get { return _p; } set { _p = value; } } " +
				"int _x = externAlias::something;",
				"void F() { { f(); } } " +
				"int P { get { return _p; } set { _p = value; } } " +
				"int _x = externAlias::something;");
			
			// Check basic functionality, including if-statement and nesting
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(new List<int>()::list);
					if (DBConnection.Tables.Get(""Person"")::table != null) {
						Process(table);
						if (AdjacencyList.Count::c > 1)
							pairs += c - 1;
					}
				}",
				@"void f() {
					var list = new List<int>();
					Foo(list);
					{
						var table = DBConnection.Tables.Get(""Person"");
						if (table != null) {
							Process(table);
							{
								var c = AdjacencyList.Count;
								if (c > 1)
									pairs += c - 1;
							}
						}
					}
				}");

			// Test a nested run sequence
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(#runSequence(Debug.Assert(X()::x > 0), x));
				}",
				@"void f() {
					var x = X();
					Debug.Assert(x > 0);
					Foo(x);
				}");

			// Test with some args being sequences and others not
			var n = StandardMacros.NextTempCounter;
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(a, #runSequence(PrepareB(), GetB()), c, #runSequence(var d = D(), d) + 1, e);
				}", @"
				void f() {
					var a_1 = a;
					PrepareB();
					var GetB_2 = GetB();
					var c_3 = c;
					var d = D();
					Foo(a_1, GetB_2, c_3, d + 1, e);
				}
				".Replace("a_1", "a_" + n)
				.Replace("GetB_2", "GetB_" + (n+1))
				.Replace("c_3", "c_" + (n+2)));

			// Test #if with #runSequence as body
			TestEcs(@"#useSequenceExpressions;
				void f() {
					if (c) #runSequence(A(), B());
					if (c) #runSequence(A(), B()); else #runSequence(X(), Y());
				}",
				@"void f() {
					if (c) { A(); B(); }
					if (c) { A(); B(); } else { X(); Y(); }
				}");
		}

		[Test]
		public void TestFieldInitializers()
		{
			TestEcs(@"#useSequenceExpressions;
				static double nine = Math.Sqrt(9)::three * three;
				Pair<Symbol,Symbol> p = Pair.Create(""foo""(->Symbol)::str, str);
				", @"
				static double nine = nine_initializer();
				static double nine_initializer() {
					var three = Math.Sqrt(9);
					return three * three;
				}
				Pair<Symbol,Symbol> p = p_initializer();
				static Pair<Symbol,Symbol> p_initializer() {
					var str = ""foo""(->Symbol);
					return Pair.Create(str, str);
				}
				");
		}

		[Test]
		public void TestAssignmentsAndLValues()
		{
			// Straightforward cases
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(A::a.B[C] = D);
					Foo(A.B::b[C] = D);
					Foo(A.B[C::c] = D);
				}", @"
				void f() {
					var a = A;
					Foo(a.B[C] = D);
					var b = A.B;
					Foo(b[C] = D);
					var c = C;
					Foo(A.B[c] = D);
				}"
				.Replace("tmp_1", "tmp_"+StandardMacros.NextTempCounter));
			
			// Tricky cases: must take into account that LHS is an lvalue
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(A.B[C] + D::d1);
					Foo(A.B[C] = D::d2);
				}", @"
				void f() {
					var tmp_1 = A.B[C];
					var d1 = D;
					Foo(tmp_1 + d1);
					var C_2 = C;
					var d2 = D;
					Foo(A.B[C_2] = d2);
				}".Replace("tmp_1", "tmp_"+(StandardMacros.NextTempCounter))
				  .Replace("C_2", "C_"+(StandardMacros.NextTempCounter+1)));
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(A.B[C::c] + D::d1);
					Foo(A.B[C::c] = D::d2);
				}", @"
				void f() {
					var c = C;
					// A better implementation would put A.B in a temporary 
					// before it does c = C, but the current impl can't tell 
					// if A.B[c] is an lvalue or not, so it acts cautiously
					// (if A.B is an lvalue and a struct, it shouldn't be replaced)
					var tmp_1 = A.B[c];
					var d1 = D;
					Foo(tmp_1 + d1);
					var c = C;
					var d2 = D;
					Foo(A.B[c] = d2);
				}".Replace("tmp_1", "tmp_"+(StandardMacros.NextTempCounter))
				  .Replace("c_2", "c_"+(StandardMacros.NextTempCounter+1)));

			// ref and out expressions are also lvalues
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Foo(out A, ref B.C[D], out F(x).Y);
					Foo(out A, ref B.C[D], out F(x).Y, 0, E()::e);
				}", @"
				void f() {
					Foo(out A, ref B.C[D], out F(x).Y);
					var D_1 = D;
					var x_2 = x;
					var e = E();
					Foo(out A, ref B.C[D_1], out F(x_2).Y, 0, e);
				}".Replace("D_1", "D_"+(StandardMacros.NextTempCounter))
				  .Replace("x_2", "x_"+(StandardMacros.NextTempCounter+1)));
		}

		[Test]
		public void TestInWhileLoop()
		{
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Before();
					while (_min > Foo()::foo.Bar + foo.Baz)
						Console.WriteLine(foo.ToString());
				}", @"void f() {
					Before();
					for (;;) {
						var _min_1 = _min;
						var foo = Foo();
						if (_min_1 > foo.Bar + foo.Baz)
							Console.WriteLine(foo.ToString());
						else
							break;
					}
				}"
				.Replace("_min_1", "_min_"+StandardMacros.NextTempCounter));

			TestEcs(@"#useSequenceExpressions;
				void f() {
					while (_min > Foo().Bar)
						Console.WriteLine(""({0}, {1})"", foo.Property::p.Item1, p.Item2);
					After();
				}", @"void f() {
					while (_min > Foo().Bar) {
						var p = foo.Property;
						Console.WriteLine(""({0}, {1})"", p.Item1, p.Item2);
					}
					After();
				}"
				.Replace("_min_1", "_min_"+StandardMacros.NextTempCounter));

			// It's only a test. Don't actually write code this way
			Test(@"#useSequenceExpressions;
				void f() {
					Console.WriteLine(""Please press a digit."");
					while (#runSequence(#var(char, key), {
							while (!char.IsDigit(key = Console.ReadKey(true)::k.KeyChar)) 
								if (k.Key == ConsoleKey.Escape) { key = '\0'; break; }
						}, key) != '\0')
						Console.WriteLine(""You pressed ({0}, {1})"", foo.Property::p.Item1, p.Item2);
					Console.WriteLine(""Okay bye!"");
				}", EcsLanguageService.Value, 
				@"void f() {
					Console.WriteLine(""Please press a digit."");
					for (;;) {
						char key;
						{
							for (;;) {
								var k = Console.ReadKey(true);
								if (!char.IsDigit(key = k.KeyChar))
									{if (k.Key == ConsoleKey.Escape) { key = '\0'; break; }}
								else
									break;
							}
						}
						if (key != '\0') {
							var p = foo.Property;
							Console.WriteLine(""You pressed ({0}, {1})"", p.Item1, p.Item2);
						} else
							break;
					}
					Console.WriteLine(""Okay bye!"");
				}".Replace("_min_1", "_min_"+StandardMacros.NextTempCounter),
				EcsLanguageService.WithPlainCSharpPrinter);
		}

		[Test]
		public void TestInDoWhileLoop()
		{
			TestEcs(@"#useSequenceExpressions;
				void f(IEnumerator<Point> e) {
					do 
						dict[e.Current::c.X] = c.Y;
					while(e.MoveNext() > 0);
				}", @"
				void f(IEnumerator<Point> e) {
					do {
						var c = e.Current;
						dict[c.X] = c.Y;
					} while(e.MoveNext() > 0);
				}");

			TestEcs(@"#useSequenceExpressions;
				static void f() {
					do #runSequence(F(), x) = 0;
					while(Bool()::b);
				}", @"
				static void f() {
					for(bool continue_1 = true; continue_1;) {
						F();
						x = 0;
						var b = Bool();
						continue_1 = b;
					}
				}".Replace("continue_1", "continue_" + StandardMacros.NextTempCounter));
		}

		[Test]
		public void TestInForLoop()
		{
			// Sequence in initializer
			TestEcs(@"#useSequenceExpressions;
				void f() {
					for (int i = Foo()::foo.Count - 1; i >= 0; i--)
						foo[i]++;
				}", @"
				void f() {
					{
						var foo = Foo();
						for (int i = foo.Count - 1; i >= 0; i--) 
							foo[i]++;
					}
				}");

			// Sequence in condition
			TestEcs(@"#useSequenceExpressions;
				void f() {
					for (int i = 0; i < List.Count::c; i++)
						foo[c - i]++;
				}", @"
				void f() {
					for (int i = 0; ; i++) {
						var i_1 = i;
						var c = List.Count;
						if (i_1 < c)
							foo[c - i]++;
						else
							break;
					}
				}".Replace("i_1", "i_"+StandardMacros.NextTempCounter));

			// Sequence in increment expression
			TestEcs(@"#useSequenceExpressions;
				void f() {
					for (int i = 0; i < list.Count; i += Foo()::f.x + f.y)
						Body();
				}", @"
				void f() {
					for (int i = 0; i < list.Count;) {
						Body();
						var f = Foo();
						i += f.x + f.y;
					}
				}");

			// Sequences everywhere
			TestEcs(@"#useSequenceExpressions;
				void f() {
					for (Init(I()::i, i); C(i)::c; #runSequence(inc1(), inc2()))
						A(B()::b, c);
				}", @"
				void f() {
					{
						var i = I();
						for (Init(i, i);;) {
							var c = C(i);
							if (c) {
								var b = B();
								A(b, c);
								inc1();
								inc2();
							} else
								break;
						}
					}
				}");
		}

		[Test]
		public void TestLambdaMethod()
		{
			// Test lambda-style method
			TestEcs(@"#useSequenceExpressions;
				int fSquare(int x) => f(x)::fx * fx;
				", @"
				int fSquare(int x) {
					var fx = f(x);
					return fx * fx;
				}");

			// Test lambda function
			TestEcs(@"#useSequenceExpressions;
				void f() {
					Func<int,int> fSquare = (int x) => f(x)::fx * fx;
				}
				", @"
				void f() {
					Func<int,int> fSquare = (int x) => {
						var fx = f(x);
						return fx * fx;
					};
				}");
		}

		[Test]
		public void TestInForeachLoop()
		{
			// foreach
			TestEcs(@"#useSequenceExpressions;
				int f() {
					Before();
					foreach (var x in GetList()::list)
						#runSequence(x.Reset(), x.ParentList = list);
				}", @"
				int f() {
					Before();
					{
						var list = GetList();
						foreach (var x in list) {
							x.Reset();
							x.ParentList = list;
						}
					}
				}");
		}

		[Test]
		public void TestInSimpleKeywordStatements()
		{
			// return
			TestEcs(@"#useSequenceExpressions;
				int f() {
					return GetList()::L.Capacity - L.Count;
				}", @"
				int f() {
					var L = GetList();
					return L.Capacity - L.Count;
				}");

			// using
			TestEcs(@"#useSequenceExpressions;
				void f() {
					using (Foo()::f.PushState())
						f.SetState(GetState()::s, s.Bar);
				}", @"
				void f() {
					{
						var f = Foo();
						using (f.PushState()) {
							var s = GetState();
							f.SetState(s, s.Bar);
						}
					}
				}");

			// lock
			TestEcs(@"#useSequenceExpressions;
				void f() {
					lock (Foo()::f)
						f.Bar::b.Baz(b);
				}", @"
				void f() {
					{
						var f = Foo();
						lock (f) {
							var b = f.Bar;
							b.Baz(b);
						}
					}
				}");

			// switch
			TestEcs(@"#useSequenceExpressions;
				void f() {
					switch (Foo()::f) {
					case 777:
						#runSequence(Lucky(), Me());
					}
				}", @"
				void f() {
					{
						var f = Foo();
						switch (f) {
						case 777:
							Lucky();
							Me();
						}
					}
				}");
		}

		[Test]
		public void TestInTryCatchFinally()
		{
			// try-finally
			TestEcs(@"#useSequenceExpressions;
				void f() {
					try
						#runSequence(F(), G());
					finally
						#runSequence(X(), Y());
					return true;
				}", @"
				void f() {
					try {
						F();
						G();
					} finally {
						X();
						Y();
					}
					return true;
				}");

			// try-catch
			TestEcs(@"#useSequenceExpressions;
				void f() {
					try {
						#runSequence(F(), G());
					} catch (Exception ex) {
						PrintInfo(ex.GetType()::t, t.Name + "": "" + ex.Message);
					}
				}", @"
				void f() {
					try {
						F();
						G();
					} catch (Exception ex) {
						var t = ex.GetType();
						PrintInfo(t, t.Name + "": "" + ex.Message);
					}
				}");

			// try/catch/finally/throw
			// Note: `::` is not currently supported in the `when` clause
			TestEcs(@"#useSequenceExpressions;
				void f() {
					try
						#runSequence(F(), G());
					catch (IgnoreException ex) when (true)
						#runSequence(Console.WriteLine(""nothing serious""), #return(false));
					catch {
						#runSequence(Who(), Cares());
					}  finally
						#runSequence(X(), Y());
					return true;
				}", @"
				void f() {
					try {
						F();
						G();
					} catch (IgnoreException ex) when (true) {
						Console.WriteLine(""nothing serious"");
						return false;
					} catch {
						Who();
						Cares();
					} finally {
						X();
						Y();
					}
					return true;
				}");
		}

		[Test]
		public void TestInFixed()
		{
			TestEcs(@"#useSequenceExpressions;
				void f() {
					fixed (_)
						Foo::o.a = o.b;
				}", @"
				void f() {
					fixed (_) {
						var o = Foo;
						o.a = o.b;
					}
				}");
			TestEcs(@"#useSequenceExpressions;
				void f() {
					fixed (int* x = &list[CurrentIndex::i])
						*x = Foo::o.a + o.b;
				}", @"
				void f() {
					{
						var i = CurrentIndex;
						fixed (int* x = &list[i]) {
							var x_1 = x;
							var o = Foo;
							*x_1 = o.a + o.b;
						}
					}
				}".Replace("x_1", "x_"+StandardMacros.NextTempCounter));
		}

		[Test]
		public void TestRefVarDecl()
		{
			TestEcs(@"#useSequenceExpressions;
				void f() { Foo(ref int x = 5); }", @"
				void f() {
					int x = 5;
					Foo(ref x);
				}");
		}

		[Test]
		public void TestOutVarDecl()
		{
			TestEcs(@"#useSequenceExpressions;
				static int? Parse(string s) => int.Parse(s, out int x) ? (int?)x : null;", @"
				static int? Parse(string s) {
					int x;
					return int.Parse(s, out x) ? (int?)x : null;
				}");
		}
	}
}
