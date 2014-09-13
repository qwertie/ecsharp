using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Syntax
{
	// Originally no unit tests were written for LNode, so we have only regression tests.
	[TestFixture]
	public class LNodeTests : Assert
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
	}
}
