using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestMatchMacro : MacroTesterBase
	{
		[Test]
		public void TestMatch1()
		{
			// Check the basics: equality of literals, deconstruction, multiple case handlers.
			TestEcs(@"
					match (obj) {
						case (Prop1: 1, Prop2: '2', Prop3: @@3): 
							DoSomething1();
						case ($x, _, $z):
							DoSomething1();
							DoSomething2();
					}",
					@"do {
						if (1.Equals(obj.Prop1) && '2'.Equals(obj.Prop2) && @@3.Equals(obj.Prop3)) {
							DoSomething1();
							break;
						}
						{
							var x = obj.Item1;
							var z = obj.Item3;
							DoSomething1();
							DoSomething2();
							break;
						}
					} while(false);");

			// Test core features used separately: type testing (is X), literal testing (2), 
			// field names and deconstruction (C: c), and a guard (c > 3)
			int n = MacroProcessor.NextTempCounter;
			TestEcs(@"
					match (obj) {
						case is Thing(is A, 2, C: $c) && c > 3:
							DoSomethingWith(c);
					}",
					@"do
						if (obj is Thing) {
							Thing tmp_1 = (Thing)obj;
							if (tmp_1.Item1 is A && 2.Equals(tmp_1.Item2)) {
								var c = tmp_1.C;
								if (c > 3) {
									DoSomethingWith(c);
									break;
								}
							}
						}
					while(false);"
					.Replace("tmp_1", "tmp_" + n));

			// Use core features together, with nesting
			n = MacroProcessor.NextTempCounter;
			TestEcs(@"
					match (obj) {
						case is Shape(ShapeType.Circle, $size, Location: $p is Point<int>($x, $y) && x > y):
							Circle(size, x, y);
						case _:
							Default();
					}",
				@"do {
					if (obj is Shape) {
						Shape tmp_A = (Shape)obj;
						if (ShapeType.Circle.Equals(tmp_A.Item1)) {
							var size = tmp_A.Item2;
							var tmp_B = tmp_A.Location;
							if (tmp_B is Point<int>) {
								Point<int> p = (Point<int>)tmp_B;
								var x = p.Item1; 
								var y = p.Item2; 
								if (x > y) {
									Circle(size, x, y); 
									break;
								}
							} 
						}
					}
					{
						Default();
						break;
					}
				} while(false);"
				.Replace("tmp_A", "tmp_" + n)
				.Replace("tmp_B", "tmp_" + (n + 1)));

			n = MacroProcessor.NextTempCounter;
			TestEcs(@"
				match (Foo.Bar) {
					case true || false: True();
					case null:
					default: Default();
				}",
				@"do {
					var tmp_1 = Foo.Bar;
					if ((true || false).Equals(tmp_1)) {
						True();
						break;
					}
					if (tmp_1 == null)
						break;
					{
						Default();
					}
				} while(false);".Replace("tmp_1", "tmp_"+n));
		}

		[Test]
		public void TestMatch2()
		{
			// Test `ref` inside and outside `$`
			int n = MacroProcessor.NextTempCounter;
			TestEcs(@"
				int x, y;
				SizeF size;
				Point<int> p;
				match (obj) {
					case is Shape(ShapeType.Circle, ref size, Location: ref p is Point<int>(ref x, $(ref y)) && x > y):
						Circle(size, x, y);
				}", @"
				int x, y;
				SizeF size;
				Point<int> p;
				do
					if (obj is Shape) {
						Shape tmp_A = (Shape)obj;
						if (ShapeType.Circle.Equals(tmp_A.Item1)) {
							size = tmp_A.Item2;
							var tmp_B = tmp_A.Location;
							if (tmp_B is Point<int>) {
								p = (Point<int>)tmp_B;
								x = p.Item1; 
								y = p.Item2; 
								if (x > y) {
									Circle(size, x, y); 
									break;
								}
							} 
						}
					}
				while(false);"
				.Replace("tmp_A", "tmp_" + n)
				.Replace("tmp_B", "tmp_" + (n + 1)));
			
			// Test two patterns on one case
			TestEcs(@"
				match (obj) {
					case ((x), $y), ($y, x): DoSomethingWith(x, y);
				}",
				@"do {
					if ((x).Equals(obj.Item1)) {
						var y = obj.Item2;
						DoSomethingWith(x, y);
						break;
					}
					{
						var y = obj.Item1;
						if (x.Equals(obj.Item2)) {
							DoSomethingWith(x, y);
							break;
						}
					}
				} while (false);
				");
		}

		[Test]
		public void TestMatch3()
		{
			// Test ranges
			int n = MacroProcessor.NextTempCounter;
			TestEcs(@"
				match (obj) {
					case $t is Thing(ref $r is double in x..y, c...d) in x..<y:
						DoSomethingWith(t, r);
				}",
				@"do
					if (obj is Thing) {
						Thing t = (Thing)obj;
						var tmp_1 = t.Item1;
						if (tmp_1 is double) {
							r = (double)tmp_1;
							if (r.IsInRangeExcludeHi(x, y) && t.Item2.IsInRange(c, d) && t.IsInRangeExcludeHi(x, y)) {
								DoSomethingWith(t, r);
								break;
							}
						}
					}
				while(false);"
				.Replace("tmp_1", "tmp_" + n));

			// Bug fix: This combination didn't work
			n = MacroProcessor.NextTempCounter;
			TestEcs(@"
				match (value) {
					case is Point(X: $x, Y: $y) in polygon:
						CollisionDetected(x, y);
				}",
				@"do
					if (value is Point) {
						Point tmp_1 = (Point) value;
						var x = tmp_1.X;
						var y = tmp_1.Y;
						if (polygon.Contains(tmp_1)) {
							CollisionDetected(x, y);
							break;
						}
					}
				while(false);"
				.Replace("tmp_1", "tmp_" + n));
		}
	}
}
