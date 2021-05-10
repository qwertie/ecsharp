using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Ecs;
using Loyc.MiniTest;
using Loyc;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestUseSequenceExpressionsMacro : MacroTesterBase
	{
		[Test]
		public void TestBasics()
		{
			// Check that it doesn't do anything when there's nothing to do.
			TestCs("#useSequenceExpressions; " +
				"void F() { { f(); } } " +
				"int P { get { return _p; } set { _p = value; } } " +
				"int _x = externAlias::something;",
				"void F() { { f(); } } " +
				"int P { get { return _p; } set { _p = value; } } " +
				"int _x = externAlias::something;");

			// Test that it works inside a namespace and a class
			TestCs(@"#useSequenceExpressions;
				public namespace NS {
					public class Program {
						static void FooBar() {
							#runSequence(Foo(), Bar());
						}
					}
				}",
				@"public namespace NS {
					public class Program {
						static void FooBar() {
							Foo(); Bar();
						}
					}
				}");

			// Check basic functionality, including if-statement and nesting
			TestCs(@"#useSequenceExpressions;
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

			// Test with some args being sequences and others not
			var n = MacroProcessor.NextTempCounter;
			TestCs(@"#useSequenceExpressions;
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
				.Replace("GetB_2", "GetB_" + (n + 1))
				.Replace("c_3", "c_" + (n + 2)));

			// Test #if with #runSequence as body
			TestCs(@"#useSequenceExpressions;
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
				class Foo {
					static double nine = Math.Sqrt(9)::three * three;
					Pair<Symbol,Symbol> p = Pair.Create(""foo""(->Symbol)::str, str);
				}
				", @"
				class Foo {
					static double nine = nine_initializer();
					static double nine_initializer() {
						var three = Math.Sqrt(9);
						return ([@`%isTmpVar`] three) * three;
					}
					Pair<Symbol,Symbol> p = p_initializer();
					static Pair<Symbol,Symbol> p_initializer() {
						var str = ""foo""(->Symbol);
						return Pair.Create([@`%isTmpVar`] str, str);
					}
				}
				");
		}

		[Test]
		public void TestAssignmentsAndLValues()
		{
			// Straightforward cases
			TestCs(@"#useSequenceExpressions;
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
				.Replace("tmp_1", "tmp_" + MacroProcessor.NextTempCounter));

			// Tricky cases: must take into account that LHS is an lvalue
			TestCs(@"#useSequenceExpressions;
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
				}".Replace("tmp_1", "tmp_" + (MacroProcessor.NextTempCounter))
				  .Replace("C_2", "C_" + (MacroProcessor.NextTempCounter + 1)));
			TestCs(@"#useSequenceExpressions;
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
				}".Replace("tmp_1", "tmp_" + (MacroProcessor.NextTempCounter))
				  .Replace("c_2", "c_" + (MacroProcessor.NextTempCounter + 1)));

			// ref and out expressions are also lvalues
			TestCs(@"#useSequenceExpressions;
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
				}".Replace("D_1", "D_" + (MacroProcessor.NextTempCounter))
				  .Replace("x_2", "x_" + (MacroProcessor.NextTempCounter + 1)));
		}

		[Test]
		public void TestInWhileLoop()
		{
			TestCs(@"#useSequenceExpressions;
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
				.Replace("_min_1", "_min_" + MacroProcessor.NextTempCounter));

			TestCs(@"#useSequenceExpressions;
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
				.Replace("_min_1", "_min_" + MacroProcessor.NextTempCounter));

			// It's only a test. Don't actually write code this way
			TestCs(@"#useSequenceExpressions;
				void f() {
					Console.WriteLine(""Please press a digit."");
					while (#runSequence(#var(char, key), {
							while (!char.IsDigit(key = Console.ReadKey(true)::k.KeyChar)) 
								if (k.Key == ConsoleKey.Escape) { key = '\0'; break; }
						}, key) != '\0')
						Console.WriteLine(""You pressed ({0}, {1})"", foo.Property::p.Item1, p.Item2);
					Console.WriteLine(""Okay bye!"");
				}",
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
				}".Replace("_min_1", "_min_" + MacroProcessor.NextTempCounter));
		}

		[Test]
		public void TestInDoWhileLoop()
		{
			TestCs(@"#useSequenceExpressions;
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

			TestCs(@"#useSequenceExpressions;
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
				}".Replace("continue_1", "continue_" + MacroProcessor.NextTempCounter));
		}

		[Test]
		public void TestInForLoop()
		{
			// Sequence in initializer
			TestCs(@"#useSequenceExpressions;
				void abc() {
					for (int i = Foo()::foo.Count - 1; i >= 0; i--)
						foo[i]++;
				}", @"
				void abc() {
					{
						var foo = Foo();
						for (int i = foo.Count - 1; i >= 0; i--) 
							foo[i]++;
					}
				}");

			// Sequence in condition
			TestCs(@"#useSequenceExpressions;
				void def() {
					for (int i = 0; i < List.Count::c; i++)
						foo[c - i]++;
				}", @"
				void def() {
					for (int i = 0; ; i++) {
						var i_1 = i;
						var c = List.Count;
						if (i_1 < c)
							foo[c - i]++;
						else
							break;
					}
				}".Replace("i_1", "i_" + MacroProcessor.NextTempCounter));

			// Sequence in increment expression
			TestCs(@"#useSequenceExpressions;
				void ghi() {
					for (int i = 0; i < list.Count; i += Foo()::f.x + f.y)
						Body();
				}", @"
				void ghi() {
					for (int i = 0; i < list.Count;) {
						Body();
						var f = Foo();
						i += f.x + f.y;
					}
				}");

			// Sequences everywhere
			TestCs(@"#useSequenceExpressions;
				void jkl() {
					for (Init(I()::i, i); C(i)::c; #runSequence(inc1(), inc2()))
						A(B()::b, c);
				}", @"
				void jkl() {
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
		public void TestNestedRunSequences()
		{
			// Test a nested run sequence
			TestCs(@"#useSequenceExpressions;
				void f() {
					Foo(#runSequence(Debug.Assert(X()::x > 0), x));
				}",
				@"void f() {
					var x = X();
					Debug.Assert(x > 0);
					Foo(x);
				}");
			
			// 2021-01: Buggy example found
			TestCs(@"#useSequenceExpressions;
				void nrs2() {
					for (var i = I(); #runSequence(Bam(), C(i)::c); #runSequence(inc1(), inc2()))
						A(B()::b, c);
				}", @"
				void nrs2() {
					for (var i = I();;) {
						Bam();
						var c = C(i);
						if (c) {
							var b = B();
							A(b, c);
							inc1();
							inc2();
						} else
							break;
					}
				}");
		}

		[Test]
		public void TestLambdaMethod()
		{
			// Test lambda-style method
			TestCs(@"#useSequenceExpressions;
				int fSquare(int x) => f(x)::fx * fx;
				", @"
				int fSquare(int x) {
					var fx = f(x);
					return fx * fx;
				}");

			// Test lambda function
			using (MessageSink.SetDefault(new SeverityMessageFilter(_msgHolder, Severity.DebugDetail))) {
				_msgHolder.List.Clear();
				TestCs(@"#useSequenceExpressions;
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
				Assert.AreEqual(1, _msgHolder.List.Count);
				Assert.AreEqual(Severity.Warning, _msgHolder.List[0].Severity);
				_msgHolder.WriteListTo(TraceMessageSink.Value);
			}
		}

		[Test]
		public void TestInForeachLoop()
		{
			// foreach
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
				int f() {
					return GetList()::L.Capacity - L.Count;
				}", @"
				int f() {
					var L = GetList();
					return L.Capacity - L.Count;
				}");

			// using
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
			TestCs(@"#useSequenceExpressions;
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
				}".Replace("x_1", "x_" + MacroProcessor.NextTempCounter));
		}

		[Test]
		public void TestRefVarDecl()
		{
			TestCs(@"#useSequenceExpressions;
				void f() { Foo(ref int x = 5); }", @"
				void f() {
					int x = 5;
					Foo(ref x);
				}");
		}

		//[Test]
		//public void TestOutVarDecl()
		//{
		//	TestEcs(@"#useSequenceExpressions;
		//		static int? Parse(string s) => int.Parse(s, out int x) ? (int?)x : null;", @"
		//		static int? Parse(string s) {
		//			int x;
		//			return int.Parse(s, out x) ? (int?)x : null;
		//		}");
		//}

		[Test]
		public void TestObviousBugWithRunSequenceInVarDecl()
		{
			TestEcs(@"#useSequenceExpressions;
				static double Circumference(double radius) {
					double c = #runSequence(Console.WriteLine(), Math.PI * 2 * radius);
					return c;
				}", @"
				static double Circumference(double radius) {
					Console.WriteLine();
					double c = Math.PI * 2 * radius;
					return c;
				}");
		}

		[Test]
		public void TestTopLevelRunSequenceWithBraces()
		{
			TestEcs(@"#ecs;
				define operator<<($s, $x) => $s.Append($x);

				[PriorityOverride, Passive]
				define operator<<($s, ($items, $toString)) {
					// Not advisable for production use, but good as a test	
					#runSequence {
						var s__ = $s;
						IReadOnlyList<string> it__ = $items;
						for (int i__ = 0; i__ < it__.Count; ++i__)
							(i__ > 0 ? s__ << "", "" : s__) << $toString(it__[i__]);
						s__;
					}
				}

				string Ack(string s) => s + ""!"";
				var names = new[] { ""Holly"", ""Molly"" };
				var b = new StringBuilder();
				b << ""Hello... "";
				b << ""Howdy "" << (names, Ack) << "" etc."";
			", @"
				string Ack(string s) => s + ""!"";

				var names = new[] {
					""Holly"", ""Molly""
				};
				var b = new StringBuilder() ;
				b.Append(""Hello... "");
				var s__ = b.Append(""Howdy "");
				IReadOnlyList<string> it__ = names;
				for (int i__ = 0; i__ < it__.Count; ++i__)
					(i__ > 0 ? s__.Append("", "") : s__).Append(Ack(it__[i__]));
				s__.Append("" etc."");
			");
		}

		[Test]
		public void UsingStmt()
		{
			TestEcs(@"#useSequenceExpressions;
				using (var x = Foo(#runSequence(Thing1(), Thing2())))
					NormalCode();
			",
			@"{
				Thing1();
				using (var x = Foo(Thing2()))
					NormalCode();
			}");
			TestEcs(@"#useSequenceExpressions;
				using (new OrdinaryDisposable())
					Foo(#runSequence(var x = Thing1(), Thing2(x)));
			", @"
				using (new OrdinaryDisposable()) {
					var x = Thing1();
					Foo(Thing2(x));
				}
			");
			TestEcs(@"#useSequenceExpressions;
				using (var x = #runSequence {
					moveOutsideCurrentMethod {
						class InnerClassX : BaseType {
							OuterType outer;
							public InnerClassX(OuterType outer) { this.outer = outer; }
						}
					}
					new InnerClassX(this);
				})
					InsideUsing(#runSequence(int x = 5, x));
			",
			@"{
				moveOutsideCurrentMethod {
					class InnerClassX : BaseType {
						OuterType outer;
						public InnerClassX(OuterType outer) { this.outer = outer; }
					}
				}
				using (var x = new InnerClassX(this))
				{
					int x = 5;
					InsideUsing(x);
				}
			}");
		}

		[Test]
		public void RefVarLogic()
		{
			TestCs(@"#useSequenceExpressions;
				ref int a = ref b;
				Foo(ref int c = 0);
				Foo(ref int x = ref y);
			", @"
				ref int a = ref b;
				int c = 0;
				Foo(ref c);
				ref int x = ref y;
				Foo(x);
			");
		}

		[Test(Fails = "TODO: Bug #136")]
		public void Bug_2021_02_ColonColonStatement()
		{
			TestCs(@"#useSequenceExpressions;
				new Point()::p;
				Thing f()
				{
					new Thing()::t;
					return t;
				}
				", @"
				var p = new Point();
				Thing f()
				{
					var t = new Thing();
					return t;
				}");
		}

		[Test(Fails = "TODO: Bug #136")]
		public void Bug_2021_02_WithStatement()
		{
			TestCs(@"#useSequenceExpressions;
				with (#runSequence(var p = new Person(), p))
				{
					.Name = ""John Smith"";
					.Age = 39;
				}
				", @"
				var p = new Person();
						var tmp_10 = p;
						tmp_10.Name = ""John Smith"";
				tmp_10.Age = 39;
				".Replace("tmp_A", "tmp_" + MacroProcessor.NextTempCounter));
			TestCs(@"#useSequenceExpressions;
				with (FindById(personId)::p)
				{
					.FavoriteColor = Color.Red;
					.Age = 39;
				}
			", @"
				var p = FindById(personId);
				var tmp_A = p;
				tmp_A.FavoriteColor = Color.Red;
				tmp_A.Age = 39;
			".Replace("tmp_A", "tmp_" + MacroProcessor.NextTempCounter));
		}

		[Test]
		public void Bug_2021_01_VarDeclProblem()
		{
			// A variable declaration whose initializer used #runSequence { with braces } 
			// was translated incorrectly. The problem was that a widespread change that 
			// was needed to support the braces had not been applied to the special case 
			// of `$T $x = #runSequence {...}`. In addition, I added a special case for
			// `using ($T $x = ...)` to improve the output and there's a test here for it.
			TestCs(@"#useSequenceExpressions;
				Foo(var x = #runSequence { FirstThing(); SecondThing(); });
			", @"
				FirstThing();
				var x = SecondThing();
				Foo(x);
			");

			TestCs(@"#useSequenceExpressions;
				PlainStatement(var x = #runSequence {
					moveOutsideCurrentMethod {
						class InnerClassX : BaseType {
							OuterType outer;
							public InnerClassX(OuterType outer) => this.outer = outer;
						}
					}
					new InnerClassX(this);
				}, #runSequence(int y = 5, y));
			", @"
				moveOutsideCurrentMethod {
					class InnerClassX : BaseType {
						OuterType outer;
						public InnerClassX(OuterType outer) => this.outer = outer;
					}
				}
				var x = new InnerClassX(this);
				int y = 5;
				PlainStatement(x, y);
			");
		}

		[Test]
		public void Bug_2021_01_TrivialRunSequences()
		{
			// Calls to #runSequence with one argument should disappear, but weren't
			TestEcs(@"#useSequenceExpressions;
				using (var Q = #runSequence(new Thing()))
					DoStuffWith(#runSequence(Q));
			", @"
				using (var Q = new Thing())
					DoStuffWith(Q);
			");
			// TODO: the transform of the `if` statement probably shouldn't add braces
			TestCs(@"#useSequenceExpressions;
				if (var x = #runSequence(#splice(), #splice(), new Thing()))
					#runSequence(#splice(), DoStuffWith)(#runSequence(#splice(), x));
				while (#runSequence(condition))
					#runSequence(DoStuff());
				do
					#runSequence(TwiddleThumbs());
				while (#runSequence(bored));
				for (#runSequence(#splice(), int x = 0); #runSequence(x < 10); #runSequence(x++))
					Console.WriteLine(#runSequence(x));
			", @"
				{
					var x = new Thing();
					if (x)
						DoStuffWith(x);
				}
				while (condition)
					DoStuff();
				do
					TwiddleThumbs();
				while (bored);
				for (int x = 0; x < 10; x++)
					Console.WriteLine(x);
			");
			TestEcs(@"#useSequenceExpressions;
				Foo(x, y - #runSequence(#splice()), #runSequence(#splice()));
			", @"
				Foo(x, y - #runSequence(), #runSequence());
			");
			TestCs(@"#useSequenceExpressions;
				Foo(var x = #runSequence { new Crapola(this); });
			", @"
				var x = new Crapola(this);
				Foo(x);
			");
		}
	}
}