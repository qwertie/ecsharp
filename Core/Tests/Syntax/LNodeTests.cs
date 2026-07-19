using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Syntax.Les;
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
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call((LNode?) null, LNode.Missing));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(CodeSymbols.Sub, LNode.List((LNode?) null)));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.Id(CodeSymbols.Sub), LNode.List((LNode?) null)));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.List((LNode?) null), CodeSymbols.Sub, LNode.List(LNode.Id("x"))));
			Assert.ThrowsAny<ArgumentNullException>(() => LNode.Call(LNode.List((LNode?) null), LNode.Id(CodeSymbols.Sub), LNode.List(LNode.Id("x"))));
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

		[Test]
		public void TestExt_MatchesPattern_1()
		{
			var les3 = Les3LanguageService.Value;
			LNode candidate = les3.ParseSingle("Foo(x, y + z)");

			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("x + $x"), out var captures));
			Assert.IsTrue(captures == null || captures.Count == 0);
			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("Foo($x, y)"), out captures));
			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("Foo($x, y + z, zzz)"), out captures));
			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("Foo($x)"), out captures));
			
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("Foo($x, $y)"), out captures));
			Assert.AreEqual(2, captures.Count);
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("$x(x, $y)"), out captures));
			Assert.AreEqual(2, captures.Count);
		}

		[Test]
		public void TestExt_MatchesPattern_2()
		{
			var les3 = Les3LanguageService.Value;
			var candidate = les3.ParseSingle("1 + 2");

			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("1 - 2"), out var captures));
			Assert.IsTrue(captures == null || captures.Count == 0);
			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("x + $x"), out captures));
			
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("1 + $x"), out captures));
			Assert.AreEqual(1, captures.Count);
			
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("$x + $x"), out captures, true));
			Assert.AreEqual(1, captures.Count);
			// OMG the order is reversed.
			// I bet no one is using this, then.
			Assert.AreEqual(new KeyValuePair<Symbol,LNode>((Symbol)"x", les3.ParseSingle("#splice(2, 1)")), captures.Single());

			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("$x + $x"), out captures, false));

			candidate = les3.ParseSingle("123 + 123");
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("$N + $N"), out captures, false));
			Assert.AreEqual(1, captures.Count);
			Assert.AreEqual(new KeyValuePair<Symbol,LNode>((Symbol)"N", LNode.Literal(123)), captures.Single());
		}

		[Test]
		public void TestExt_MatchesPattern_3()
		{
			var les3 = Les3LanguageService.Value;
			var candidate = les3.ParseSingle("@Attr 2 + 3 * 4");
			
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("$x + $y"), out var captures, out var unmatchedAttrs));
			Assert.AreEqual(2, captures.Count);
			Assert.AreEqual(1, unmatchedAttrs.Count);

			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("@($a) $x + $y"), out captures, out unmatchedAttrs));
			Assert.AreEqual(3, captures.Count);
			Assert.AreEqual(0, unmatchedAttrs.Count);

			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("@Blah $x + $y"), out captures, out unmatchedAttrs));
			
			candidate = les3.ParseSingle("((@Attr1 2 + 3 * 4))");
			Assert.AreEqual(2, candidate.AttrCount); // 1 normal attribute + 1 parens trivia
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("@($(..attrs)) 2 + $y"), out captures, out unmatchedAttrs));
			Assert.AreEqual(2, captures.Count);
			Assert.AreEqual(0, unmatchedAttrs.Count);
			
			// TODO: Probably this should not fail; we're treating the attribute list like an 
			//       argument list, but they are used differently.
			Assert.IsFalse(candidate.MatchesPattern(les3.ParseSingle("@Attr1 2 + $y"), out captures, out unmatchedAttrs));
			
			// OTOH If the attribute list pattern has $(..list) in it, then maybe we should
			// treat the attribute list like an argument list.
			Assert.IsTrue(candidate.MatchesPattern(les3.ParseSingle("@$(.._) @Attr1 @$(.._) 2 + $y"), out captures, out unmatchedAttrs));
			Assert.AreEqual(0, unmatchedAttrs.Count);

			// TODO: tests with attributes inside subexpressions
		}
	}
}
