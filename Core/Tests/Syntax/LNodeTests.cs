using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax
{
	// Originally no unit tests were written for LNode, so we have only regression tests.
	[TestFixture]
	public class LNodeTests : TestHelpers
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Synthetic);
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), Foo = F.Id("Foo");
		protected LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);

		[Test]
		public void Comparisons()
		{
			IsTrue(F.Attr(Foo, zero).Equals(F.Attr(Foo, zero)));
			IsFalse(F.Attr(Foo, zero).Equals(F.Attr(a, zero)));
			IsFalse(zero.Equals(F.Attr(Foo, zero)));
			IsTrue(F.Attr(Foo, a).Equals(F.Attr(Foo, a)));
			IsFalse(F.Attr(Foo, a).Equals(F.Attr(Foo, b)));
			IsFalse(a.Equals(F.Attr(Foo, a)));
		}

		[Test]
		public void Test_FlattenBinaryOpSeq()
		{
			ExpectList(LNode.FlattenBinaryOpSeq(a, S.Add, null), new[] { a });
			ExpectList(LNode.FlattenBinaryOpSeq(a, S.Add, false), new[] { a });
			ExpectList(LNode.FlattenBinaryOpSeq(a, S.Add, true), new[] { a });
			var expr = F.Call(S.Add, a, b);
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Add, null), new[] { a, b });
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Add, false), new[] { a, b });
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Add, true), new[] { a, b });
			var fooCall = F.Call(Foo, c, zero); // Ensure this is not mistaken for an operator
			expr = F.Call(S.Assign, a, F.Call(S.Assign, b, fooCall));
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Assign, null), new[] { a, b, fooCall });
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Assign, false), new[] { a, F.Call(S.Assign, b, fooCall) });
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Assign, true), new[] { a, b, fooCall });
			expr = F.Call(S.Mul, F.Call(S.Mul, a, b), F.Call(S.Mul, c, two));
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Mul, null), new[] { a, b, c, two });
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Mul, false), new[] { a, b, F.Call(S.Mul, c, two) });
			ExpectList(LNode.FlattenBinaryOpSeq(expr, S.Mul, true), new[] { F.Call(S.Mul, a, b), c, two });
		}

		[Test]
		public void NoNullChildrenAllowed()
		{
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call((LNode)null, LNode.Missing));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(CodeSymbols.Sub, LNode.List((LNode)null)));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.Id(CodeSymbols.Sub), LNode.List((LNode)null)));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.List((LNode)null), CodeSymbols.Sub, LNode.List(LNode.Id("x"))));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.List((LNode)null), LNode.Id(CodeSymbols.Sub), LNode.List(LNode.Id("x"))));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(CodeSymbols.Sub, LNode.List(LNode.Id("x"))).PlusAttr(null));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(CodeSymbols.Sub, LNode.List(LNode.Id("x"))).PlusArg(null));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.Id(CodeSymbols.Sub), LNode.List(LNode.Id("x"))).PlusAttr(null));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.Id(CodeSymbols.Sub), LNode.List(LNode.Id("x"))).PlusArg(null));
		}

		[Test]
		public void Test_SimpleLiteral()
		{
			var source = new SourceFile<UString>(" 123 ", "x.txt");
			var attr = LNode.List(LNode.Missing);
			var lit111 = LNode.Literal(111, EmptySourceFile.Synthetic, NodeStyle.HexLiteral);
			var lit112 = LNode.Literal(attr, 112, EmptySourceFile.Synthetic, NodeStyle.HexLiteral);
			var lit123 = LNode.Literal(123, new SourceRange(source, 1, 3), NodeStyle.BinaryLiteral);
			var lit125 = LNode.Literal(attr, 125, new SourceRange(source, 1, 3), NodeStyle.OctalLiteral);
			var lit455 = LNode.Literal(455, lit123);
			var lit456 = LNode.Literal(attr, 456, lit125);
			Assert.AreEqual(111, lit111.Value);
			Assert.AreEqual(112, lit112.Value);
			Assert.AreEqual(123, lit123.Value);
			Assert.AreEqual(125, lit125.Value);
			Assert.AreEqual(455, lit455.Value);
			Assert.AreEqual(456, lit456.Value);
			Assert.AreEqual(0, lit111.AttrCount);
			Assert.AreEqual(1, lit112.AttrCount);
			Assert.AreEqual(0, lit123.AttrCount);
			Assert.AreEqual(1, lit125.AttrCount);
			Assert.AreEqual(0, lit455.AttrCount);
			Assert.AreEqual(1, lit456.AttrCount);
			Assert.AreEqual(NodeStyle.HexLiteral,    lit111.Style);
			Assert.AreEqual(NodeStyle.HexLiteral,    lit112.Style);
			Assert.AreEqual(NodeStyle.BinaryLiteral, lit123.Style);
			Assert.AreEqual(NodeStyle.OctalLiteral,  lit125.Style);
			Assert.AreEqual(NodeStyle.BinaryLiteral, lit455.Style);
			Assert.AreEqual(NodeStyle.OctalLiteral,  lit456.Style);
			Assert.AreEqual(default(UString), lit111.TextValue);
			Assert.AreEqual(default(UString), lit112.TextValue);
			Assert.AreEqual(default(UString), lit123.TextValue);
			Assert.AreEqual(default(UString), lit456.TextValue);
			Assert.AreEqual(null, lit111.TypeMarker);
			Assert.AreEqual(null, lit112.TypeMarker);
			Assert.AreEqual(null, lit123.TypeMarker);
			Assert.AreEqual(null, lit456.TypeMarker);
		}
		
		[Test]
		public void Test_CustomLiteral()
		{
			var source = new SourceFile<UString>("111z _\"0x123'456\"", "x.txt");
			var lit111 = LNode.Literal(new SourceRange(source, 0, 4), new SerializedLiteral("111", (Symbol)"_z"), NodeStyle.Default);
			var lit123 = LNode.Literal(new SourceRange(source, 5, 9), new ParsedLiteral(0x123456, 7, 9, (Symbol)"_"), NodeStyle.HexLiteral);
			Assert.AreEqual((UString)"111", lit111.Value);
			Assert.AreEqual(0x123456, lit123.Value);
			Assert.AreEqual(NodeStyle.Default, lit111.Style);
			Assert.AreEqual(NodeStyle.HexLiteral, lit123.Style);
			Assert.AreEqual((UString)"111", lit111.TextValue);
			Assert.AreEqual((UString)"0x123'456", lit123.TextValue);
			Assert.AreEqual((Symbol)"_z", lit111.TypeMarker);
			Assert.AreEqual((Symbol)"_", lit123.TypeMarker);
		}
	}
}
