using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Loyc.Collections
{
	public class TestHelpers
	{
		protected static void ExpectList<T>(IListSource<T> list, params T[] expected)
		{
			ExpectList(list, expected as IList<T>, false);
		}
		protected static void ExpectList<T>(IListSource<T> list, bool useEnumerator, params T[] expected)
		{
			ExpectList(list, expected as IList<T>, useEnumerator);
		}
		protected static void ExpectList<T>(IListSource<T> list, IList<T> expected, bool useEnumerator)
		{
			Assert.AreEqual(expected.Count, list.Count);
			if (useEnumerator)
			{
				int i = 0;
				foreach (T item in list)
				{
					Assert.AreEqual(expected[i], item);
					i++;
				}
			}
			else
			{
				for (int i = 0; i < expected.Count; i++)
					Assert.AreEqual(expected[i], list[i]);
			}
		}
		protected static void AssertThrows<Type>(TestDelegate @delegate)
		{
			try {
				@delegate();
			} catch (Exception exc) {
				Assert.IsInstanceOf<Type>(exc);
				return;
			}
			Assert.Fail("Delegate did not throw '{0}' as expected.", typeof(Type).Name);
		}
	}
}
