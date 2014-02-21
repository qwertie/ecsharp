using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections.Impl
{
	public class TestHelpers : Assert
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
				ExpectList(list, expected);
			else
			{
				for (int i = 0; i < expected.Count; i++)
					Assert.AreEqual(expected[i], list[i]);
			}
		}
		protected static void ExpectList<T>(IEnumerable<T> list, IEnumerable<T> expected)
		{
			IEnumerator<T> listE = list.GetEnumerator();
			int i = 0;
			foreach (T expectedItem in expected)
			{
				Assert.That(listE.MoveNext());
				Assert.AreEqual(expectedItem, listE.Current);
				i++;
			}
			Assert.IsFalse(listE.MoveNext());
		}

		protected static void AssertThrows<Type>(Action @delegate)
		{
			try {
				@delegate();
			} catch (Exception exc) {
				Assert.IsInstanceOf<Type>(exc);
				return;
			}
			Assert.Fail("Delegate did not throw '{0}' as expected.", typeof(Type).Name);
		}

		protected static void ExpectSet<T>(IEnumerable<T> set, params T[] expected)
		{
			ExpectSet(set, new HashSet<T>(expected));
		}
		protected static void ExpectSet<T>(IEnumerable<T> set, HashSet<T> expected)
		{
			int count = 0;
			foreach (T item in set) {
				Assert.That(expected.Contains(item));
				count++;
			}
			Assert.AreEqual(expected.Count, count);
			if (set is ICollection<T>)
				Assert.AreEqual(expected.Count, ((ICollection<T>)set).Count);
		}
	}
}
