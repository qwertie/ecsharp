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
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
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
	}
}
