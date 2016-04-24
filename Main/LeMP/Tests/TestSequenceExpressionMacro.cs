using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class SequenceExpressionMacroTests : MacroTesterBase
	{
		[Test]
		public void TestBasics()
		{
			// Check that it doesn't do anything when there's nothing to do.
			TestEcs("#useSequenceExpressions; " +
				"void F() { { x } } " +
				"int P { get { return _p; } set { _p = value; } } " +
				"int _x = 0;",
				"void F() { { x } } " +
				"int P { get { return _p; } set { _p = value; } } " +
				"int _x = 0;");
			
			// Check basic functionality, including nesting
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
		public void TestInLoops()
		{
			TestEcs(@"#useSequenceExpressions;
				void f(int min) {
					Before();
					while (_min > Foo()::foo.Bar + foo.Baz)
						Console.WriteLine(foo.ToString());
				}", @"void f() {
					Before();
					for (;;) {
						var _min_1 = _min;
						var foo = Foo();
						if (_min_1 > foo.Bar + foo.Baz > 0)
							Console.WriteLine(foo.Reprocess());
						else
							break;
					}
				}"
				.Replace("_min_1", "_min_"+StandardMacros.NextTempCounter));
		}

		[Test]
		public void TestInOtherConstructs()
		{
			// TODO
		}

		[Test]
		public void TestLambdaMethod()
		{
			// Test lambda method
			TestEcs(@"#useSequenceExpressions;
				static double Nine => Math.Sqrt(9)::three * three;
				", @"
				static double Nine() {
					double three = Math.Sqrt(9);
					return three * three;
				}");
		}
	}
}
