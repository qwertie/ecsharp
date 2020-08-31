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
		protected LNode comment = F.Trivia(S.TriviaSLComment, " Comment!");

		[Test]
		public void Test_Comparisons()
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
		public void Test_NoNullChildrenAllowed()
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
		public void TestExt_AsList()
		{
			var add = F.Call(S.Add, a, one);
			var assign = F.Call(S.Assign, a, b);
			var splice = F.Splice(add, assign);
			ExpectList(add.AsList(S.Splice), add);
			ExpectList(splice.AsList(S.Splice), add, assign);
			ExpectList(splice.AsList(S.Tuple), splice);
		}

		[Test]
		public void TestExt_Unsplice()
		{
			var add = F.Call(S.Add, a, one);
			var assign = F.Call(S.Assign, a, b);
			var splice = F.Splice(add, assign);
			ExpectList(splice.Unsplice(), add, assign);
			Assert.AreEqual(splice.Unsplice(), splice.Args);
			ExpectList(splice.PlusAttr(comment).Unsplice(), add.PlusAttr(comment), assign);
			ExpectList(splice.PlusAttrs(a, comment, b).Unsplice(), add.PlusAttrs(a, comment, b), assign);
			ExpectList(splice.PlusTrailingTrivia(comment).Unsplice(), add, assign.PlusTrailingTrivia(comment));
			ExpectList(splice.PlusTrailingTrivia(comment).PlusAttrs(comment).Unsplice(), 
			           add.PlusAttrs(comment), assign.PlusTrailingTrivia(comment));
			ExpectList(splice.PlusTrailingTrivia(comment).PlusAttrs(comment, a).Unsplice(),
			           add.PlusAttrs(comment, a), assign.PlusTrailingTrivia(comment));
			ExpectList(splice.PlusTrailingTrivia(comment).PlusAttrs(comment, a).Unsplice(),
			           add.PlusAttrs(comment, a), assign.PlusTrailingTrivia(comment));
		}

		[Test]
		public void TestExt_IncludingAttributes()
		{
			var add = F.Call(S.Add, a, one).WithAttrs(Foo);
			var assign = F.Call(S.Assign, a, b);
			var list = LNode.List(add, assign);
			ExpectList(list.IncludingAttributes(LNode.List()), add, assign);
			ExpectList(list.IncludingAttributes(LNode.List(comment)), add.PlusAttrBefore(comment), assign);
			ExpectList(list.IncludingAttributes(LNode.List(F.Call(S.TriviaTrailing, comment))),
				add, assign.PlusTrailingTrivia(comment));
			ExpectList(list.IncludingAttributes(LNode.List(comment, F.Call(S.TriviaTrailing, comment))),
				add.PlusAttrBefore(comment), assign.PlusTrailingTrivia(comment));
			ExpectList(list.IncludingAttributes(LNode.List(F.Call(Foo), F.Call(S.TriviaTrailing, comment), F.Call(S.TriviaTrailing, Foo))),
				add.PlusAttrBefore(F.Call(Foo)), assign.PlusTrailingTrivia(LNode.List(comment, Foo)));
		}

		[Test]
		public void TestExt_PlusTriviaFrom()
		{
			var add = F.Call(S.Add, a, one).WithAttrs(Foo, F.TriviaNewline).PlusTrailingTrivia(comment);
			var assign = F.Call(S.Assign, a, b);
			Assert.AreEqual(add, add.IncludingTriviaFrom(assign));

			var assign2 = assign.WithAttrs(F.Call(Foo));
			var expected = assign2.PlusAttrBefore(F.TriviaNewline).PlusTrailingTrivia(comment);
			Assert.AreEqual(expected, assign2.IncludingTriviaFrom(add));
		}
	}
}
