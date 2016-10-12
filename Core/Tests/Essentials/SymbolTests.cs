using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc
{
	[TestFixture]
	public class SymbolTests
	{
		[Test]
		public void BasicChecks()
		{
			Assert.AreEqual(null, GSymbol.Get(null));
			Assert.AreEqual(0, GSymbol.Get("").Id);
			Assert.AreEqual(GSymbol.Empty, GSymbol.GetById(0));

			Symbol foo = GSymbol.Get("Foo");
			Symbol bar = GSymbol.Get("Bar");
			Assert.AreNotEqual(foo, bar);
			Assert.AreEqual("Foo", foo.ToString());
			Assert.AreEqual("Bar", bar.ToString());
			Assert.AreEqual("Foo", foo.Name);
			Assert.AreEqual("Bar", bar.Name);
			//Assert.IsNotNull(string.IsInterned(foo.Name));
			//Assert.IsNotNull(string.IsInterned(bar.Name));

			Symbol foo2 = GSymbol.Get("Foo");
			Symbol bar2 = GSymbol.Get("Bar");
			Assert.AreNotEqual(foo2, bar2);
			Assert.AreEqual(foo, foo2);
			Assert.AreEqual(bar, bar2);
			Assert.That(object.ReferenceEquals(foo.Name, foo2.Name));
			Assert.That(object.ReferenceEquals(bar.Name, bar2.Name));
		}

		[Test]
		public void TestPrivatePools()
		{
			SymbolPool p1 = new SymbolPool();
			SymbolPool p2 = new SymbolPool(3);
			SymbolPool p3 = new SymbolPool(0);
			Symbol a = GSymbol.Get("a");
			Symbol b = GSymbol.Get("b");
			Symbol c = GSymbol.Get("c");
			Symbol s1a = p1.Get("a");
			Symbol s1b = p1.Get("b");
			Symbol s1c = p1.Get("c");
			Symbol s2a = p2.Get("a");
			Symbol s3a = p3.Get("a");
			Symbol s3b = p3.Get("b");

			Assert.That(s1a.Id == 1 && p1.GetById(1) == s1a);
			Assert.That(s1b.Id == 2 && p1.GetById(2) == s1b);
			Assert.That(s1c.Id == 3 && p1.GetById(3) == s1c);
			Assert.That(s2a.Id == 3 && p2.GetById(3) == s2a);
			Assert.That(s3b.Id == 1 && p3.GetById(1) == s3b);
			Assert.That(s3a.Id == 0 && p3.GetById(0) == s3a);
			Assert.AreEqual(GSymbol.Empty, p1.GetById(0));
			Assert.AreEqual(s1c, p1.GetIfExists("c"));
			Assert.AreEqual(3, p1.TotalCount);
			Assert.AreEqual(null, p2.GetIfExists("c"));
			Assert.AreEqual(c, p2.GetGlobalOrCreateHere("c"));
			Assert.AreEqual(p2, p2.GetGlobalOrCreateHere("$!unique^&*").Pool);
		}

		public class ShapeType : Symbol
		{
			private ShapeType(Symbol prototype) : base(prototype) { }
			public static new readonly SymbolPool<ShapeType> Pool
								 = new SymbolPool<ShapeType>(delegate(Symbol p) { return new ShapeType(p); });

			public static readonly ShapeType Circle = Pool.Get("Circle");
			public static readonly ShapeType Rect = Pool.Get("Rect");
			public static readonly ShapeType Line = Pool.Get("Line");
			public static readonly ShapeType Polygon = Pool.Get("Polygon");
		}
		public class FractalShape
		{
			public static readonly ShapeType Mandelbrot = ShapeType.Pool.Get("XyzCorp.Mandelbrot");
			public static readonly ShapeType Julia = ShapeType.Pool.Get("XyzCorp.Julia");
			public static readonly ShapeType Fern = ShapeType.Pool.Get("XyzCorp.Fern");
		}


		[Test]
		public void TestDerivedSymbol()
		{
			int count = 0;
			foreach (ShapeType s in ShapeType.Pool)
			{
				count++;
				Assert.That(s == ShapeType.Circle || s == ShapeType.Rect ||
						   s == ShapeType.Polygon || s == ShapeType.Line);
				Assert.That(s.Id > 0);
				Assert.That(!s.IsGlobal);
				Assert.AreEqual(s, ShapeType.Pool.GetById(s.Id));
				Assert.AreEqual(s, ShapeType.Pool.GetIfExists(s.Name));
			}
			Assert.AreEqual(4, count);
		}

		[Test]
		public void GetNonexistantId()
		{
			Assert.AreEqual(GSymbol.GetById(876543210), null);
			Assert.AreEqual(GSymbol.GetById(-876543210), null);
		}

#if !DotNet35
		private static void RunParallel(int parallelCount, Action action)
		{
			System.Threading.Tasks.Parallel.Invoke(
				Enumerable.Repeat(action, parallelCount).ToArray());
		}

		[Test]
		public void BugFixOct2016_SymbolGetRaceCondition()
		{
			// Bug: SymbolPool.Get used to check the symbol table contained the symbol's 
			// name, and then acquire a lock if it didn't. Inside the lock it assumed that _map 
			// would still not contain the symbol and called Add(), which threw 
			// KeyAlreadyExistsException when the assumption was false.
			RunParallel(8, () =>
			{
				for (int i = 0; i < 1000; i++)
					GSymbol.Get(i.ToString());
			});
		}
#endif
	}
}
