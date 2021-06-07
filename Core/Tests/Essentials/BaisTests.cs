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
	public class BaisTests : TestHelpers
	{
		[Test]
		public void BasicTest()
		{
			var arr = new byte[] { 67, 97, 116, 131, 10, 69, 255, 65, 66, 67, 68 };
			var str = "Cat\b`piE?tEB!CD";
			var str2 = ByteArrayInString.ConvertFromBytes(arr, false);
			var arr2 = ByteArrayInString.ConvertToBytes(str);
			Assert.AreEqual(str, str2);
			ExpectList(arr, arr2);
		}

		Random _r = new Random();

		[Test]
		public void RandomTest()
		{
			for (int len = 0; len < 100; len++) {
				byte[] bytes = new byte[len];
				_r.NextBytes(bytes);
				string asStr = ByteArrayInString.ConvertFromBytes(bytes, _r.Next(2) != 0);
				byte[] result = ByteArrayInString.ConvertToBytes(asStr).ToArray();
				ExpectList(result, bytes);
			}
		}
	}
}
