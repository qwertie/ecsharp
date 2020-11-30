using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc.Essentials.Tests
{
	public class SliceTests<SliceT> : TestHelpers where SliceT : IListSource<string>, ICloneable<SliceT>
	{
		protected Func<string[], int, int, SliceT> _new;

		public SliceTests(Func<string[], int, int, SliceT> factory)
		{
			_new = factory;
		}

		[Test]
		public void TestConstructor()
		{
			Assert.ThrowsAny<Exception>(() => _new(new string[] { "A", "B" }, -1, 2));
			Assert.ThrowsAny<Exception>(() => _new(new string[] { "A", "B" }, 1, -1));
		}

		[Test]
		public void TestEmpty()
		{
			// Get an empty slice in different ways
			var slices = new SliceT[] { 
				_new(new string[0], 0, 1),
				_new(new string[] { "A", "B" }, 1, 0),
				_new(new string[] { "A", "B" }, int.MaxValue - 1, 1),
				_new(new string[] { "A", "B" }, int.MaxValue - 1, int.MaxValue),
			};
			foreach (var slice in slices)
			{
				StandardTest(slice, new string[0]);
				Assert.ThrowsAny<Exception>(() => { var _ = slice[0]; });
				Assert.ThrowsAny<Exception>(() => { var _ = slice[-1]; });
			}
		}

		[Test]
		public void TestFullList()
		{
			string[] list;
			list = new string[] { "A" };
			StandardTest(_new(list, 0, 1), list);
			list = new string[] { "A", "B", "C", "D" };
			StandardTest(_new(list, 0, 4), list);
		}

		[Test]
		public void TestPartialList()
		{
			string[] list;
			list = new string[] { "A", "B", "C", "D" };
			int M = int.MaxValue;
			StandardTest(_new(list, 1, 4), new string[] { "B", "C", "D" });
			StandardTest(_new(list, 1, M), new string[] { "B", "C", "D" });
			StandardTest(_new(list, 0, 3), new string[] { "A", "B", "C" });
			StandardTest(_new(list, 1, 2), new string[] { "B", "C" });
		}

		// Requires a non-empty list
		void StandardTest(SliceT slice, string[] items)
		{
			Assert.AreEqual(items.Length, slice.Count);
			ExpectList(slice, items);
			ExpectListByEnumerator(slice, items);

			Assert.AreEqual(null, slice.TryGet(-1, out bool fail));
			Assert.IsTrue(fail);
			Assert.AreEqual(null, slice.TryGet(items.Length, out fail));
			Assert.IsTrue(fail);
			Assert.AreEqual(null, slice.TryGet(int.MinValue, out fail));
			Assert.IsTrue(fail);
			Assert.AreEqual(null, slice.TryGet(int.MaxValue, out fail));
			Assert.IsTrue(fail);

			for (int i = 0; i < items.Length; i++)
			{
				Assert.AreEqual(items[i], slice.TryGet(i, out fail));
				Assert.IsFalse(fail);
			}
		}
	}

	// TODO: Tests in which the list length decreases after slice creation

	[TestFixture]
	public class ListSourceSliceTests : SliceTests<Slice_<string>>
	{
		public ListSourceSliceTests() : base((list, start, count) => 
			new Slice_<string>(new InternalList<string>(list), start, count)) { }
	}

	[TestFixture]
	public class ROLSliceTests : SliceTests<ROLSlice<InternalList<string>, string>>
	{
		public ROLSliceTests() : base((list, start, count) => 
			new ROLSlice<InternalList<string>, string>(new InternalList<string>(list, list.Length), start, count)) { }
	}

	[TestFixture]
	public class ListSliceTests : SliceTests<ListSlice<string>>
	{
		// TODO: test mutable access
		public ListSliceTests() : base((list, start, count) => 
			new ListSlice<string>(new InternalList<string>(list), start, count)) { }
	}
}
