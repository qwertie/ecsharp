using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc
{
	public class GoInterfaceBenchmark
	{
		public interface IEnumerableCount<T> : IEnumerable<T>
		{
			int Count { get; }
		}
		public interface IReadOnlyList<T> : IEnumerableCount<T>
		{
			T this[int index] { get; }
		}

		public static void DoBenchmark()
		{
			const int Iterations = 100000000;

			// Measure the time it takes to instantiate ten versions of
			// IReadOnlyList<T>. GoInterface is not able to create generic wrappers,
			// so every time you wrap the same generic type with a different type
			// parameter, GoInterface produces a completely separate wrapper. This
			// is not good for performance, but at least it makes it easy for our
			// benchmark to pick 10 "different" classes to create wrappers of.
			//
			// It is possible to wrap List<byte> not only as IReadOnlyList<byte>
			// but also as any larger integer type, such as IReadOnlyList<int>. 
			// However, the wrapping of the GetEnumerator() methods doesn't work.
			// List<byte>.GetEnumerator() returns IEnumerator<byte>, but 
			// IReadOnlyList<int>.GetEnumerator() returns IEnumerator<int>. There is
			// no implicit conversion from IEnumerator<byte> to IEnumerator<int>,
			// so GoInterface fails to wrap it. However, by using ForceFrom we get
			// around this limitation, which still allows us to use the indexer and
			// Count properties. If you call GetEnumerator(), though, you get a
			// MissingMethodException.
			// 
			// Note that if you run this part of the benchmark twice without
			// exiting the program, the second time around it should take zero
			// milliseconds. And the benchmark generally runs more slowly right
			// after you reboot your computer.
			SimpleTimer timer = new SimpleTimer();
			var dummy0 = GoInterface<IReadOnlyList<byte>>.ForceFrom(new List<byte>());
			int firstOne = timer.Restart();
			var dummy1 = GoInterface<IReadOnlyList<short>>.ForceFrom(new List<byte>());
			var dummy2 = GoInterface<IReadOnlyList<ushort>>.ForceFrom(new List<byte>());
			var dummy3 = GoInterface<IReadOnlyList<int>>.ForceFrom(new List<byte>());
			var dummy4 = GoInterface<IReadOnlyList<uint>>.ForceFrom(new List<byte>());
			var dummy5 = GoInterface<IReadOnlyList<long>>.ForceFrom(new List<byte>());
			var dummy6 = GoInterface<IReadOnlyList<ulong>>.ForceFrom(new List<byte>());
			var dummy7 = GoInterface<IReadOnlyList<float>>.ForceFrom(new List<byte>());
			var dummy8 = GoInterface<IReadOnlyList<double>>.ForceFrom(new List<byte>());
			var dummy9 = GoInterface<IReadOnlyList<object>>.ForceFrom(new List<byte>());
			int nineMore = timer.Millisec;

			Console.WriteLine("First interface wrapped in {0}ms; nine more took {1}ms", firstOne, nineMore);

			// Second test: measure how long it takes to wrap the same List<int>
			// many times, using either GoInterface class.
			var list = new List<int>();
			list.Add(0);
			list.Add(1);
			list.Add(2);
			list.Add(3);
			IList<int> ilist;
			IReadOnlyList<int> rolist;
			GoInterface<IReadOnlyList<int>>.From(list); // ignore first call
			
			timer.Restart();
			int i = 0;
			do {
				ilist = list; // normal interface assignment is pretty much a no-op
			} while (++i < Iterations);
			int wrapTest0 = timer.Restart();

			i = 0;
			do {
				rolist = GoInterface<IReadOnlyList<int>>.From(list);
			} while (++i < Iterations);
			int wrapTest1 = timer.Restart();

			i = 0;
			do {
				rolist = GoInterface<IReadOnlyList<int>, List<int>>.From(list);
			} while (++i < Iterations) ;
			int wrapTest2 = timer.Restart();

			Console.WriteLine("Wrapper creation speed ({0} million times):", Iterations / 1000000);
			Console.WriteLine("- {0} ms for normal .NET interfaces (no-op)", wrapTest0);
			Console.WriteLine("- {0} ms for GoInterface<IReadOnlyList<int>>.From()", wrapTest1);
			Console.WriteLine("- {0} ms for GoInterface<IReadOnlyList<int>,List<int>>.From()", wrapTest2);

			int total0 = 0, total1 = 0, total2 = 0;

			timer.Restart();
			for (i = 0; i < Iterations; i++)
			{
				total0 += list[i & 3];
			}
			int callTestDirectCall = timer.Restart();

			for (i = 0; i < Iterations; i++)
			{
				total1 += ilist[i & 3];
			}
			int callTestNormalInterface = timer.Restart();

			for (i = 0; i < Iterations; i++)
			{
				total2 += rolist[i & 3];
			}
			int callTestGoInterface = timer.Restart();

			Debug.Assert(total0 == total1 && total1 == total2);

			Console.WriteLine("Time to call indexer of List<int> ({0} million times):", Iterations / 1000000);
			Console.WriteLine("- {0} ms for direct calls (not through an interface)", callTestDirectCall);
			Console.WriteLine("- {0} ms through IList<int> (normal interface)", callTestNormalInterface);
			Console.WriteLine("- {0} ms through IReadOnlyList<int> (GoInterface)", callTestGoInterface);
		}
	}
}
