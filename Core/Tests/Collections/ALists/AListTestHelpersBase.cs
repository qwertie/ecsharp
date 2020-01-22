using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;
using System.ComponentModel;
using Loyc.MiniTest;
using Loyc.Math;
using Loyc.Collections.Impl;

namespace Loyc.Collections.Tests
{
	using System;

	public abstract class AListTestHelpersBase<AList, T> : TestHelpers where AList : AListBase<int, T>, ICloneable<AList>
	{
		public int MaxInnerSize { get; }
		public int MaxLeafSize { get; }

		public AListTestHelpersBase(int maxLeafSize, int maxInnerSize)
		{
			MaxInnerSize = maxInnerSize;
			MaxLeafSize = maxLeafSize;
		}

		/// <summary>Retuns a new, empty list.</summary>
		public abstract AList NewList();
		
		/// <summary>Adds an item to an AList and then to a List at the same place.</summary>
		/// <param name="preferredIndex">Index to use if alist is an AList. 
		/// If it is a B+ tree then the item is always added in sorted order.</param>
		public abstract int AddToBoth(AList alist, List<T> list, int item, int preferredIndex);

		/// <summary>Adds an item to an AList.</summary>
		public abstract int Add(AList alist, int item, int preferredIndex);

		public abstract AList CopySection(AList alist, int start, int subcount);

		public abstract AList RemoveSection(AList alist, int start, int subcount);

		public abstract bool RemoveFromBoth(AList alist, List<T> list, int item);

		public abstract int GetKey(T item);
		
		/// <summary>Remove an item from an AList and then from a List at the same location.</summary>
		public void RemoveAtInBoth(AList alist, List<T> list, int index)
		{
			Assert.AreEqual(list.Count, alist.Count);
			Debug.Assert(index < list.Count);
			alist.RemoveAt(index);
			list.RemoveAt(index);
		}

		public virtual AList NewList(int initialCount, out List<T> list)
		{
			AList alist = NewList();
			list = new List<T>();

			// Make a list from 0..initialCount-1
			// Add the items in such a way that we end up with a sorted list 
			// whether it is an AList or a B+ tree, but don't simply add the
			// items in order because we want to give the tree more chances 
			// to malfunction during the test :)
			int middle = Math.Max(initialCount * 2 / 3, 1);
			for (int i = middle; i < initialCount; i++)
				AddToBoth(alist, list, i, i - middle);
			ExpectList(alist, list, true);

			for (int i = 1; i < middle; i++)
				AddToBoth(alist, list, i, i - 1);
			if (initialCount > 0)
				AddToBoth(alist, list, 0, 0);
			ExpectList(alist, list, true);
			return alist;
		}
	}

	public abstract class AListTestHelpersBase<AList> : AListTestHelpersBase<AList, int> where AList : AListBase<int>, ICloneable<AList>
	{
		public AListTestHelpersBase(int maxLeafSize, int maxInnerSize) : base(maxLeafSize, maxInnerSize) { }

		#region Implementations of abstract methods

		public override int AddToBoth(AList alist, List<int> list, int item, int preferredIndex)
		{
			alist.Insert(preferredIndex, item);
			list.Insert(preferredIndex, item);
			return preferredIndex;
		}
		public override int Add(AList alist, int item, int preferredIndex)
		{
			alist.Insert(preferredIndex, item);
			return preferredIndex;
		}
		public override bool RemoveFromBoth(AList alist, List<int> list, int item)
		{
			int i = alist.IndexOf(item);
			if (i == -1) {
				Assert.IsFalse(alist.Remove(item));
				return false;
			}
			Assert.IsTrue(alist.Remove(item));
			list.RemoveAt(i);
			return true;
		}
		public override int GetKey(int item)
		{
			return item;
		}

		#endregion
	}
}
