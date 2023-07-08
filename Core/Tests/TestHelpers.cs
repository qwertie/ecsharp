using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace Loyc.Collections.Impl
{
	/// <summary>Helpers methods for unit tests, especially used by Loyc collection 
	/// classes but sometimes useful in other cases.</summary>
	public class TestHelpers : Assert
	{
		protected static void ExpectList<T>(IReadOnlyList<T>? list, params T[]? expected)
		{
			ExpectList(list, expected as IList<T>, false);
		}
		/// <summary>
		/// When testing a buggy collection type, the enumerator might behave 
		/// differently than the indexer, so this alternate comparer is provided.
		/// </summary>
		protected static void ExpectListByEnumerator<T>(IReadOnlyList<T>? list, params T[]? expected)
		{
			ExpectList(list, expected as IList<T>, true);
		}
		public static void ExpectList<T>(IReadOnlyList<T>? list, IList<T>? expected, bool useEnumerator = false)
		{
			if (list == null || expected == null) {
				Assert.AreEqual(list, expected);
				return;
			}
			if (useEnumerator)
				ExpectList((IEnumerable<T>) list, expected);
			else
			{
				for (int i = 0; i < expected.Count; i++) {
					try {
						Assert.AreEqual(expected[i], list[i]);
					} catch (Exception e) {
						throw new AssertionException($"Lists differ at index {i}:\n{e.Message}");
					}
				}
				Assert.AreEqual(expected.Count, list.Count, "List lengths differ");
			}
		}
		public static void ExpectList<T>(IEnumerable<T> list, IEnumerable<T> expected)
		{
			IEnumerator<T> listE = list.GetEnumerator();
			int i = 0;
			foreach (T expectedItem in expected)
			{
				try {
					Assert.That(listE.MoveNext());
					Assert.AreEqual(expectedItem, listE.Current);
				} catch (Exception e) {
					throw new AssertionException($"Lists differ at index {i}: {e.Message}");
				}
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

		public static void ExpectSet<T>(IEnumerable<T> set, params T[] expected)
		{
			ExpectSet(set, new HashSet<T>(expected));
		}
		public static void ExpectSet<T>(IEnumerable<T> set, HashSet<T> expected)
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
