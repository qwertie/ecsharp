using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.CompilerCore
{
	using F = GreenFactory;
	using S = CodeSymbols;
	using Loyc.Essentials;

	[TestFixture]
	public class GreenTests : Assert
	{
		[Test]
		public void SanityChecksAndBasicEquality()
		{
			var x1 = F.Symbol("x");
			var x2 = F.Symbol("x");
			var x3 = F.Call(GSymbol.Get("x"));
			AreEqual(x1.AttrCount, 0);
			AreEqual(x1.ArgCount, 0);
			AreEqual(x3.ArgCount, 0);
			AreEqual(null, x1.Head);
			IsFalse(x1.IsCall);
			IsTrue(x3.IsCall);
			AreEqual(x1.Name, GSymbol.Get("x"));
			AreEqual(x1.Name, x3.Name);
			AreNotEqual(x1, x2);
			IsTrue(x1.EqualsStructurally(x2));
			IsFalse(x1.EqualsStructurally(x3));
			IsTrue(x1.IsFrozen);
			IsTrue(x3.IsFrozen);

			var add1 = F.Call(S._Add, x1, x2);
			var add2 = F.Call(S._Add, x2, x1);
			var add3 = F.Call(S._Add, x1, F.Symbol("y"));
			var add4 = F.Call(S._Add, x1, x2, F.Symbol("y"));
			var sub  = F.Call(S._Sub, x1, x2);
			AreEqual(sub.ArgCount, 2);

			AreNotEqual(add1, add2);
			IsTrue(add1.EqualsStructurally(add2));
			IsFalse(add1.EqualsStructurally(add2));
			IsFalse(add1.EqualsStructurally(add3));
			IsFalse(add1.EqualsStructurally(add4));
			IsFalse(add1.EqualsStructurally(sub));
			IsTrue(add1.IsFrozen);
			IsTrue(sub.IsFrozen);

			var attr1 = sub.Clone();
			attr1.Attrs.Add(new GreenAndOffset(add4));
			AreEqual(attr1.AttrCount, 1);
			AreEqual(attr1.Attrs.Count, 1);
			IsFalse(sub.EqualsStructurally(attr1));
			IsFalse(attr1.EqualsStructurally(sub));

			var attr2 = attr1.Clone();
			attr2.Attrs.Set(0, new GreenAndOffset(attr2.Attrs[0], 1234)); // doesn't affect structural equality
			IsFalse(attr1.EqualsStructurally(attr2));
			attr2.Attrs.Set(0, new GreenAndOffset(attr2.Attrs[0].Node, 123));
			IsFalse(attr1.IsFrozen);
			IsFalse(attr2.IsFrozen);
			attr2.Freeze();
			IsTrue(attr2.IsFrozen);
			IsTrue(attr1.EqualsStructurally(attr2));
		}

		[Test]
		public void ArgsMakeItACall()
		{
			var n = GreenNode.New(GSymbol.Get("foo"));
			var attr = new GreenAndOffset(F.Call(GSymbol.Get("Attribute")));
			IsFalse(n.IsCall);
			n.Attrs.Add(attr);
			IsFalse(n.IsCall);
			n.Args.Add(new GreenAndOffset(F.int_0));
			IsTrue(n.IsCall);
			var n2 = F.Call(GSymbol.Get("foo"), F.int_0);
			IsTrue(n2.IsFrozen);
			n2 = n2.Clone();
			IsFalse(n.IsCall);
			n2.Args.AddRange(n.Args);
			IsTrue(n.IsCall);
			n2.Attrs.AddRange(n.Attrs);
			IsTrue(n.EqualsStructurally(n2));

			var n3 = n2.AutoOptimize(true, true);
			IsTrue(n3.EqualsStructurally(n2));
		}
	}
}
