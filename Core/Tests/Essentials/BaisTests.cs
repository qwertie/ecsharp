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

			str = "!" + str;
			var arr3 = ByteArrayInString.ConvertToBytes(str);
			ExpectList(arr3, arr2);
		}

		[Test]
		public void BasicTest2()
		{
			// If we start with non-ASCII, it'll have to start in base64 mode
			var arr = new byte[] { 192, 255, 32, 67, 97, 116, 115, 131, 10, 69, 255, 65, 66, 67 };
			var str = "\b\u0070\u004F\u007C\u0060!Cats\b`piE?tEB!C";
			var str2 = ByteArrayInString.ConvertFromBytes(arr, false, true);
			var arr2 = ByteArrayInString.ConvertToBytes(str);
			Assert.AreEqual(str, str2);
			ExpectList(arr, arr2);
		}

		[Test]
		public void BasicTest33()
		{
			var arr = new byte[] { 33 };
			var str = "!!";
			var str2 = ByteArrayInString.ConvertFromBytes(arr, false, true);
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
