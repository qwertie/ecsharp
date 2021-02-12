using Loyc.Collections;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections.Tests
{
	[TestFixture]	
	public class InternalDArrayTests
	{
		Random _r;
		public InternalDArrayTests(int seed)
		{
			_r = new Random(seed);
		}

		[Test]
		public void FuzzTest()
		{
			for (int trial = 0; trial < 100; trial++) {
				var dict = new Dictionary<int, float>();
				var list = InternalDArray<float>.Empty;
				for (int i = 0; i < 20; i++) {
					int index = _r.Next(-20, 20);
					dict[index] = (float)i;
					list[index] = (float)i;
				}
				for (int index = -20; index < 20; index++) {
					dict.TryGetValue(index, out float expected);
					Assert.AreEqual(expected, list[index, 0]);
				}
			}
		}
	}
}
