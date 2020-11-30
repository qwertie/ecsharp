using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Essentials.Tests
{
	[TestFixture]
	public class EitherTests
	{
		[Test]
		public void BasicTest()
		{
			Either<int, decimal> x = 5;
			Assert.AreEqual(5, x.Value);
			Assert.IsTrue(x.Left.HasValue);
			Assert.IsFalse(x.Right.HasValue);
			Assert.AreEqual(5, x.Left.Value);

			Either<int, string> y = "Hi";
			Assert.AreEqual("Hi", y.Value);
			Assert.IsFalse(y.Left.HasValue);
			Assert.IsTrue(y.Right.HasValue);
			Assert.AreEqual("Hi", y.Right.Value);
		}

		[Test]
		public void DupeTest()
		{
			Either<int, int> x = Either<int, int>.NewLeft(5);
			Assert.AreEqual(5, x.Value);
			Assert.IsTrue(x.Left.HasValue);
			Assert.IsFalse(x.Right.HasValue);
			Assert.AreEqual(5, x.Left.Value);

			x = Either<int, int>.NewRight(42);
			Assert.AreEqual(42, x.Value);
			Assert.IsFalse(x.Left.HasValue);
			Assert.IsTrue(x.Right.HasValue);
			Assert.AreEqual(42, x.Right.Value);
		}

		[Test]
		public void CastTest()
		{
			Either<string, ArgumentException> s = "Hi";
			Either<string, ArgumentException> e = new ArgumentException();
			var s2 = Either<object, Exception>.From(s);
			var e2 = Either<object, Exception>.From(e);
			Assert.AreEqual("Hi", s2.Value);
			Assert.IsTrue(e2.Value is ArgumentException);
			
			IEither<object, Exception> s3 = s;
			IEither<object, Exception> e3 = e;
			Assert.AreEqual("Hi", s3.Left.Value);
			Assert.IsTrue(e3.Right.Value is ArgumentException);
		}

		[Test]
		public void SelectTest()
		{
			Either<int, Exception> i = 123;
			Either<int, Exception> e = new ArgumentException("Hi");
			Either<double, string> d = i.Select(x => (double)x, y => y.Message);
			Either<double, string> s = e.Select(x => (double)x, y => y.Message);
			Assert.AreEqual(123.0, d.Value);
			Assert.AreEqual("Hi", s.Value);
		}

		[Test]
		public void MapTest()
		{
			Either<int, Exception> i = 404;
			Either<int, Exception> e = new ArgumentException("Hi");
			Either<double, Exception> out1 = i.MapLeft(x => x * 0.5);
			Either<double, Exception> out2 = e.MapLeft(x => x * 0.5);
			Assert.AreEqual(202.0, out1.Value);
			Assert.AreEqual(e.Value, out2.Value);
			Either<int, string> out3 = i.MapRight(x => x.Message);
			Either<int, string> out4 = e.MapRight(x => x.Message);
			Assert.AreEqual(404, out3.Value);
			Assert.AreEqual("Hi", out4.Value);
		}

		[Test]
		public void IfTest()
		{
			Either<int, decimal> left = 5;
			Either<int, string> right = "Hi";
			string s = "";
			left.IfLeft(L => s += L + "!");
			left.IfRight(R => s += R + "!");
			right.IfLeft(L => s += L + "!");
			right.IfRight(R => s += R + "!");
			Assert.AreEqual("5!Hi!", s);
		}
	}
}