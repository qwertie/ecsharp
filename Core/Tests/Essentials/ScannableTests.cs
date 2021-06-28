using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Loyc.Collections.Tests
{
	/// <summary>
	/// A generic test fixture for testing ny implementation of <see cref="IScannable{T}"/>.
	/// Just override the abstract members for your particular implementation.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class ScannableTests<T>
	{
		/// <summary>Creates a scannable version of a list with the same sequence of items.</summary>
		public abstract IScannable<T> ToScannable(List<T> list);
		
		/// <summary>Returns any T value (the value should vary on repeated calls).</summary>
		public abstract T NextValue();

		protected Memory<T> _buf;
		protected Random _r;
		protected readonly int BaseSize;

		// `baseSize` is the default list size in some tests.
		// It must be at least 50 or the tests will break.
		protected ScannableTests(int randomSeed = 0, int baseSize = 1000)
		{
			_r = new Random(randomSeed);
			BaseSize = baseSize;
		}

		public virtual List<T> NewList(int count)
		{
			var list = new List<T>(count);
			for (int i = 0; i < count; i++)
				list.Add(NextValue());
			return list;
		}

		[SetUp]
		public void SetUp() => _buf = default;

		[Test]
		public void ReadItAllInOneOrTwoBlocks()
		{
			var list = NewList(BaseSize);
			var scannable = ToScannable(list);

			var scanner = scannable.Scan();
			var index = 0;
			ReadAndCheck(scanner, 0, BaseSize, list, ref index);
			
			// Read in one segment, skipping some items without reading them
			scanner = scannable.Scan();
			index = 0;
			ReadAndCheck(scanner, 50, BaseSize - 50, list, ref index);

			foreach (int firstSize in new[] { 2, 20, BaseSize / 2 + 1 }) {
				foreach (int overlapSize in new[] { 0, 1, 2 }) {
					foreach (int secondSize in new[] { BaseSize, int.MaxValue, BaseSize - (firstSize - overlapSize) }) {
						scanner = scannable.Scan();
						index = 0;
						ReadAndCheck(scanner, 0, firstSize, list, ref index);
						ReadAndCheck(scanner, firstSize - overlapSize, secondSize, list, ref index);
					}
				}
			}
		}

		protected void ReadAndCheck(IScanner<T> scanner, int skip, int itemsToRead, List<T> list, ref int index)
		{
			if ((uint)(index + skip) > (uint)list.Count) {
				Assert.IsTrue(skip > 0); // if not, it'a bug in the test itself
				index = list.Count;
			} else {
				index += skip;
			}

			var result = scanner.Read(skip, itemsToRead, ref _buf);

			Assert.LessOrEqual(result.Length, list.Count - index);
			Assert.IsTrue(result.Length >= itemsToRead || index + result.Length == list.Count);
			for (int i = 0; i < result.Length; i++)
				Assert.AreEqual(list[index + i], result.Span[i]);
		}

		[Test]
		public void SkipToTheMax()
		{
			var list = NewList(BaseSize);
			var scannable = ToScannable(list);

			var scanner = scannable.Scan();
			var index = 0;
			ReadAndCheck(scanner, int.MaxValue, 0, list, ref index);
			Assert.AreEqual(list.Count, index);

			foreach (var initialSkip in new[] { 0, 1, 2, 50 }) {
				scanner = scannable.Scan();
				index = 0;
				ReadAndCheck(scanner, initialSkip, 10, list, ref index);
				ReadAndCheck(scanner, int.MaxValue, 10, list, ref index);
				Assert.AreEqual(list.Count, index);
			}
		}

		[Test]
		public void ReadAndRewindIfPossible()
		{
			var list = NewList(BaseSize);
			var scannable = ToScannable(list);

			var scanner = scannable.Scan();
			var index = 0;

			// Read everything except some items at the beginning
			ReadAndCheck(scanner, 50, BaseSize, list, ref index);

			if (!scanner.CanScanBackward)
				return;

			// Rewind and read most of those unread items...
			ReadAndCheck(scanner, -40, BaseSize, list, ref index);

			// You know what, let's rewind and read the first 50
			ReadAndCheck(scanner, -10, 50, list, ref index);

			// Let's try again, without that weird skipping
			scanner = scannable.Scan();
			index = 0;

			ReadAndCheck(scanner, 0, 40, list, ref index);
			ReadAndCheck(scanner, 40, 10, list, ref index);
			Assert.AreEqual(index, 40);
			ReadAndCheck(scanner, -10, 10, list, ref index);
			ReadAndCheck(scanner, -10, 12, list, ref index);
			Assert.AreEqual(index, 20);
			ReadAndCheck(scanner, -1, 1, list, ref index);
			ReadAndCheck(scanner, -19, 20, list, ref index);
			Assert.AreEqual(index, 0);
		}

		[Test]
		public void RandomizedTest()
		{
			for (int trial = 1; trial <= 10; trial++) {
				_buf = default;
				int Size = BaseSize / trial;
				
				var list = NewList(Size);
				var scannable = ToScannable(list);

				var scanner = scannable.Scan();
				var index = 0;

				int maxShift = G.Log2Floor(Size / 10);
				
				for (int i = 0; i < 50; i++) {
					var skip = _r.Next(4) << _r.Next(maxShift + 1);
					if (scanner.CanScanBackward && _r.Next(6) == 0)
						skip = System.Math.Max(-skip, -index);

					var itemsToRead = _r.Next(4) << _r.Next(maxShift + 1);

					ReadAndCheck(scanner, skip, itemsToRead, list, ref index);
					//Console.Write(index + " ");
				}
				//Console.WriteLine();
			}
		}
	}

	public class ScannableEnumerableTests : ScannableTests<int>
	{
		public ScannableEnumerableTests(int randomSeed = 0, int baseSize = 1000) : base(randomSeed, baseSize) { }

		int _next;
		public override int NextValue() => _next++;
		public override IScannable<int> ToScannable(List<int> list) => new ScannableEnumerable<int>(list);
	}

	public class InternalListScannerTests : ScannableTests<int>
	{
		public InternalListScannerTests(int randomSeed = 0, int baseSize = 1000) : base(randomSeed, baseSize) { }

		int _next;
		public override int NextValue() => _next++;
		public override IScannable<int> ToScannable(List<int> list) => new InternalList<int>(list);
	}

	public class BufferedSequenceScannerTests : ScannableTests<int>
	{
		public BufferedSequenceScannerTests(int randomSeed = 0, int baseSize = 1000) : base(randomSeed, baseSize) { }

		int _next;
		public override int NextValue() => _next++;
		public override IScannable<int> ToScannable(List<int> list) => new BufferedSequence<int>(list);
	}
}
