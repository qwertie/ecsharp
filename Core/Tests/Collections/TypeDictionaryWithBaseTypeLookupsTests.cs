using Loyc.MiniTest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections.Tests
{
	[TestFixture]
	public class TypeDictionaryWithBaseTypeLookupsTests
	{
		[Test]
		public void BasicTests()
		{
			var dict = new TypeDictionaryWithBaseTypeLookups<string>();
			dict[typeof(bool)] = "bool";
			dict[typeof(string)] = "string";
			dict[typeof(ValueType)] = "ValueType";
			dict[typeof(IEnumerable)] = "IEnumerable";

			string value;
			Assert.IsTrue(dict.ContainsKey(typeof(string)));
			Assert.IsTrue(dict.ContainsKey(typeof(IEnumerable)));
			Assert.IsTrue(dict.ContainsKey(typeof(bool)));
			Assert.IsFalse(dict.ContainsKey(typeof(int)));
			Assert.IsFalse(dict.ContainsKey(typeof(IList<int>)));
			Assert.IsTrue(dict.TryGetValue(typeof(bool), out value));
			Assert.AreEqual("bool", value);
			Assert.IsTrue(dict.TryGetValue(typeof(int), out value));
			Assert.AreEqual("ValueType", value);
			Assert.IsTrue(dict.TryGetValue(typeof(IList<int>), out value));
			Assert.AreEqual("IEnumerable", value);
		}
	}
}
