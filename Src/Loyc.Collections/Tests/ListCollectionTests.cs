using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Collections;
using Loyc.Collections.Impl;

namespace Loyc.Collections.Tests
{
	/// <summary>A test class for collections that implement IListSource{int} and 
	/// are cloneable, such as AList, BList and DList.</summary>
	[TestFixture]
	public class ListCollectionTests<ListT> : TestHelpers where ListT : IListSource<int>, ICollection<int>, ICloneable<ListT>
	{
		protected Func<ListT> _newList;
		protected bool _isSorted;
		protected int _randomSeed;
		protected Random _r;

		public ListCollectionTests(Func<ListT> newList, int randomSeed, bool isSorted)
		{
			_newList = newList;
			_isSorted = isSorted;
			_r = new Random(_randomSeed = randomSeed);
		}

		// Resolves the "ambiguity" between IListSource.Count and ICollection.Count.
		protected static int Count<T>(ICollection<T> list) { return list.Count; }

		[Test]
		public void TestAdd()
		{
			const int BaseCount = 128;
			const int ExtraCount = 128;

			ListT list = _newList();
			for (int i = 0; i < BaseCount; i++)
			{
				list.Add(i * 10);
				CheckAddResult(list, i + 1);
			}

			ListT backup = list.Clone();

			List<int> extras = new List<int>();
			for (int i = 0; i < ExtraCount; i++)
			{
				int n = _r.Next(-200, 1800);
				list.Add(n);
				extras.Add(n);
			}

			int count = ((ICollection<int>)list).Count;
			Assert.AreEqual(256, count);
			if (_isSorted)
			{
				for (int i = 0; i < count; i++)
				{
					Assert.That(list[i] % 10 == 0 || extras.Contains(i));
					if (i > 0)
						Assert.GreaterOrEqual(list[i], list[i - 1]);
				}
				for (int i = 0; i < extras.Count; i++)
					Assert.That(list.Contains(extras[i]));
			}
			else
			{
				for (int i = 0; i < BaseCount; i++)
					Assert.AreEqual(i * 10, list[i]);
				for (int i = 0; i < extras.Count; i++)
					Assert.AreEqual(extras[i], list[i + count - extras.Count]);
			}

			CheckAddResult(backup, BaseCount);
			backup.Add(BaseCount * 10);
			CheckAddResult(backup, BaseCount + 1);
		}

		private void CheckAddResult(ListT list, int i)
		{
			Assert.AreEqual(i, ((IReadOnlyCollection<int>)list).Count);
			for (int j = 0; j < i; j++)
				Assert.AreEqual(j * 10, list[j]);
		}

		[Test]
		public void TestAddRemove()
		{
			ListT list = _newList();
			var   @ref = new List<int>(1000); // reference list

			// Add a bunch of numbers
			for (int i = 0; i < 500; i++) {
				int n = _r.Next(1000);
				list.Add(n);
				AddToReferenceList(@ref, n);
				if (i % 30 == 0)
					ExpectList(list, @ref, false);
				if (i % 100 == 0)
					ExpectList(list, @ref, true);
			}

			// Add and remove a bunch of numbers at random
			for (int i = 0; i < 500; i++)
			{
				int n = _r.Next(1000);
				if (_r.Next(2) == 0)
				{
					// Add n
					list.Add(n);
					AddToReferenceList(@ref, n);
				}
				else
				{
					// Try to remove n
					bool a = list.Remove(n);
					bool b = @ref.Remove(n);
					Assert.AreEqual(a, b);
				}
				Assert.AreEqual(@ref.Count, Count(list));
				Assert.AreEqual(@ref.Contains(n), list.Contains(n));

				if (i % 30 == 0)
					ExpectList(list, @ref, _r.Next(2) == 0);
			}

			// Remove all remaining numbers in random order
			while (@ref.Count > 0)
			{
				int i = _r.Next(@ref.Count);
				int n = @ref[i];
				@ref.RemoveAt(i);
				Assert.IsTrue(list.Remove(n));

				if (!_isSorted)
				{
					// Remove other instances of the same number to ensure that the 
					// two lists are in sync
					while (list.Remove(n))
						Assert.IsTrue(@ref.Remove(n));
				}

				Assert.AreEqual(@ref.Count, Count(list));
				if (i % 30 == 0)
					ExpectList(list, @ref, _r.Next(2) == 0);
			}
		}

		private void AddToReferenceList(List<int> @ref, int i)
		{
			if (_isSorted) {
				int at = @ref.BinarySearch(i);
				if (at < 0)
					at = ~at;
				@ref.Insert(at, i);
			} else
				@ref.Add(i);
		}
	}
}
