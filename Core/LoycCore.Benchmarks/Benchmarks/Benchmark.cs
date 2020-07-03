using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Threading;
using Loyc.Geometry;
using PointD = Loyc.Geometry.Point<double>;

namespace Benchmark
{
	using System;
	using Loyc;

	/// <summary>Miscellaneous benchmarks.</summary>
	static class Benchmarks
	{
		[ThreadStatic]
		static int _threadStatic;
		static LocalDataStoreSlot _tlSlot;
		static Dictionary<int, int> _dictById = new Dictionary<int,int>();
		static ThreadLocalVariable<int> _dict = new ThreadLocalVariable<int>();
		static int _globalVariable = 0;

		public static void ThreadLocalStorage()
		{
			Console.WriteLine("Performance of accessing a thread-local variable 10,000,000 times:");
			SimpleTimer t = new SimpleTimer();
			const int Iterations = 10000000;
			
			// Baseline comparison
			for (int i = 0; i < Iterations; i++)
				_globalVariable += i;
			Console.WriteLine("    Non-thread-local global variable: {0}ms", t.Restart());

			// ThreadStatic attribute
			t = new SimpleTimer();
			for (int i = 0; i < Iterations; i++)
			{
				// In CLR 2.0, this is the same performance-wise as two separate 
				// operations (a read and a write)
				_threadStatic += i;
			}
			int time = t.Restart();
			for (int i = 0; i < Iterations; i++)
			{
				_globalVariable += _threadStatic;
			}
			int time2 = t.Restart();
			Console.WriteLine("    ThreadStatic variable: {0}ms (read-only: {1}ms)", time, time2);

			// ThreadLocalVariable<int>
			_dict.Value = 0;
			for (int i = 0; i < Iterations; i++)
				_dict.Value += i;
			time = t.Restart();
			for (int i = 0; i < Iterations; i++)
				_globalVariable += _dict.Value;
			time2 = t.Restart();
			Console.WriteLine("    ThreadLocalVariable: {0}ms (read-only: {1}ms)", time, time2);

			// Dictionary indexed by thread ID
			_dictById[Thread.CurrentThread.ManagedThreadId] = 0;
			for (int i = 0; i < Iterations; i++)
			{
				lock (_dictById)
				{
					_dictById[Thread.CurrentThread.ManagedThreadId] += i;
				}
			}
			time = t.Restart();
			// Calling Thread.CurrentThread.ManagedThreadId
			for (int i = 0; i < Iterations; i++)
				_globalVariable += Thread.CurrentThread.ManagedThreadId;
			time2 = t.Restart();
			Console.WriteLine("    Dictionary: {0}ms ({1}ms getting the current Thread ID)", time, time2);

			// Thread Data Slot: slow, so extrapolate from 1/5 the work
			_tlSlot = Thread.AllocateDataSlot();
			Thread.SetData(_tlSlot, 0);
			t.Restart();
			for (int i = 0; i < Iterations/5; i++)
				Thread.SetData(_tlSlot, (int)Thread.GetData(_tlSlot) + i);
			time = t.Restart() * 5;
			Console.WriteLine("    Thread-local data slot: {0}ms (extrapolated)", time);
		}

		/// This benchmark is for the sake of CPTrie, which encodes keys in a byte
		/// array. Often, it needs to do operations that operate on 4 bytes at a
		/// time, so in this benchmark I attempt to do the same operations 1 byte at
		/// a time and 4 bytes at a time, to compare the difference.
		/// 
		/// Note that the arrays are small and therefore likely to be in L1 cache.
		/// Consequently, any inefficiency in the code produced by JIT tends to be
		/// obvious in this benchmark, as it is not hidden behind memory latency.
		/// 
		/// Unfortunately, these benchmarks suggest that it is impossible to
		/// reach the theoretical optimum speed using managed code. On my machine, 
		/// Array.Copy can copy 256 bytes 2,000,000 times in 150 ms. Array.Copy 
		/// requires 9 times as long to do the same operation 16 bytes at a time, 
		/// which suggests that Array.Copy has a very high call overhead and should 
		/// not be used to copy small arrays. About half of the 150 ms is probably 
		/// overhead, so the theoretical optimum speed must be under 100 ms.
		/// 
		/// If you don't use loop unrolling, the fastest equivalent with C# code 
		/// is to copy 32 bits at a time with a pair of pinned pointers, and this 
		/// takes 375 ms.
		/// 
		/// Strangely, you don't gain any performance by using pointers if you
		/// access the array one byte at a time. To the contrary, using the managed
		/// byte array directly is generally faster than using a pointer, unless you
		/// access the array 32 bits at a time. This is odd since using pointers
		/// eliminates array bounds checking.
		/// 
		/// Also very strangely, reading from the array (and summing up the
		/// elements) tends to be slower than writing to the array (writing the
		/// array index into each element).
		/// 
		/// Most strangely of all, the benchmark shows that copying from one array 
		/// to another is generally faster than simply totalling up the bytes in a 
		/// single array.
		/// 
		/// Another notable result is that pinning an array seems to be a cheap
		/// operation. One copying test pins the arrays for every 4 bytes copied,
		/// and this only takes around 500 ms (it varies), versus ~350 ms if the
		/// array is pinned once per 256 bytes, and 800 ms when copying one byte at
		/// a time with verifiable code (copy test #1).
		/// 
		/// Now I just tried the same benchmark on an older laptop and pinning was
		/// not quite so cheap there--2328 ms with repeated pinning vs 1015 ms
		/// without--but it's still slightly cheaper than copying the array one byte
		/// at a time, which takes 2483 ms.
		/// 
		/// There is one operation that can be done fast in managed code with
		/// pointers: a 32-bit fill in which the same value, or a counter, is
		/// written to each element. This is about the same speed as the 256-byte
		/// Array.Copy operation.
		/// 
		/// The rather odd results prompted me to check out the assembly code. I
		/// did this by running the Release build of the benchmark outside the
		/// debugger to obtain optimized code, then attaching the VS Pro debugger,
		/// tracing into the benchmark, and viewing disassembly. Note: on my main
		/// machine I have .NET 3.5 SP1 installed, and it made no difference to 
		/// performance whether the project was set to use .NET 2.0 or 3.5.
		/// 
		/// The good news is, the assembly is faster than you'd expect from a
		/// typical C compiler's debug build. The bad news is, it's distinctly worse 
		/// than you would expect from a C compiler's Release build.
		///
		/// Notably, in these tests, the JIT sometimes makes poor use of x86's 
		/// limited registers. In read test #1, for example, it stores the inner 
		/// loop counter on the stack instead of in a register. Yet ebx, ecx, and 
		/// edi are left unused in both the inner and outer loops. Also, the JIT 
		/// will sometimes unnecessarily copy values between eax and edx, 
		/// effectively wasting one of those registers. Also, it does not cache an 
		/// array's length in a register (yet this seems to have a minimal 
		/// performance impact).
		/// 
		/// I was surprised to learn that "pinning" the array did not actually cause
		/// any special code to be generated--the machine code did not, for
		/// instance, place a special flag in the array to mark it as pinned.
		/// Perhaps this was not so surprising, since "normal" managed code doesn't
		/// have to do anything special to access an array either. As I recall,
		/// there is metadata associated with the assembly code that informs the GC
		/// about which registers contain pointers and when, so that the GC knows
		/// which registers to change during a GC. The fixed statement, then,
		/// probably just produces some metadata that marks whatever object the
		/// pointer points to as unmovable. This is a good design, as it makes the
		/// 'fixed' statement almost free as long as there is no garbage collection.
		/// Therefore, if you pin an array for a very short time, your code is 
		/// unlikely to be interrupted for a GC during that time, making the 
		/// operation free. Note that I'm only talking about the "fixed" statement
		/// here, not the Pinned GCHandle, for which I have no benchmark.
		///
		/// But if the fixed statement does not introduce additional instructions,
		/// why does it make the code slower? Well, it doesn't ALWAYS make the code
		/// slower. The JIT will produce significantly different code depending on
		/// which of several equivalent implementations you use. The benchmark shows
		/// that reading the array with the traditional advancing pointer technique
		///
		///     while (left-- > 0) 
		///         total += *p++;
		///
		/// is slightly slower than the for-loop alternative:
		/// 
		///     for (int B = 0; B &lt; array1.Length; B++) 
		///         total2 += p[B];
		///
		/// and this latter version outperforms the "normal" version (read test #1)
		/// slightly. Since this is only because of how the code is generated, the
		/// first version could easily be faster on some other JIT or processor
		/// architecture.
		/// 
		/// But why is the code slower when it uses a "fixed" statement in the inner
		/// loop (copy test #4)? I see two reasons. Firstly, the "fixed" statement
		/// itself performs a range check on the array index. Secondly, the pointers
		/// p1 and p2 are stored on the stack and then, Lord only knows why, the JIT
		/// reads them back in from the stack as dw1 and dw2, instead of caching the
		/// pointers in registers. Then, for good measure, dw2 is written back to a
		/// different slot on the stack, even though it is never read back in again.
		/// Basically, the JIT is being stupid. If Microsoft makes it smarter (maybe
		/// CLR 4.0 is better?), this code will magically become faster.
		/// 
		/// Why is reading the array (totalling up the values) so much slower than
		/// writing or copying? I compared the assembly code for the two standard,
		/// managed loops, and the only obvious problem is that the JIT doesn't hold
		/// the total in a register, but adds directly to the variable's slot on the
		/// stack. But this doesn't seem like it's enough to explain the difference.
		/// After all, the JIT generates extra code and a memory access for the
		/// array bounds check in write test #1, yet this has only a slight
		/// performance impact. I'm guessing that read loop somehow stalls the CPU
		/// pipelines on both of my test processors (an Intel Core 2 Duo and an AMD
		/// Turion) twice as much as the write loop does.
		/// 
		/// I figured that the read-modify-write memory operation might cause some
		/// stalling, so I temporarily changed "total +=" to simply "total =" in 
		/// read tests #1, #2 and #2b. Sure enough, tests #1 and #2b nearly doubled
		/// in speed--but the speed of test #2 did not change at all. In read test
		/// #2, the JIT holds the loop counter on the stack; presumably, this
		/// stalls the processor in the same way the "+=" operation does.
		///
		/// I thought perhaps the processor would be better able to handle an
		/// unrolled loop, so I added read test #5 and write test #5, which do 
		/// twice as much work per iteration. On the Intel Core 2 Duo, it reduced
		/// the run-time of both loops by about 1/3 compared to read and write 
		/// test #3; and on the AMD Turion, I observed a more modest improvement.
		/// So, if you need to squeeze a little more performance from your 
		/// performance-critical C# code without resorting to a native C++ DLL, 
		/// consider loop unrolling.
		/// 
		/// Or, simply fiddle with the loop structure, sometimes that helps too! 
		/// I mean, write test #3b takes 30% less time than #3 by using an 
		/// advancing pointer... even though read test #2b, which uses an 
		/// advancing pointer, is slower than test #2, which does not. Or, just
		/// wait for Microsoft to improve the JIT, and ... um ... force all your 
		/// users to upgrade.
		/// 
		/// When I developed these benchmarks I forgot something rather obvious:
		/// If we're working on groups of 4 bytes all the time, we could hold the 
		/// four bytes in a structure (which I call a Cell). So I added benchmarks
		/// for reading, writing and copying 64*4-byte cells instead of 256 plain
		/// bytes. For certain operations, cells are much faster, but for others
		/// they are slower (but not dramatically so). The benchmarks seem to show
		/// that
		/// - it is much faster to initialize the cell in-place by writing each 
		///   field, rather than to call a constructor, or to initialize the cell 
		///   on the stack and then copy it into the array.
		/// - when reading the cells, the exact way that the loop is coded can
		///   have a dramatic impact on the speed. I suspect that the JIT 
		///   generally does calculations faster when they are phrased as 
		///   expressions, i.e. when you avoid writing to local variables more
		///   than necessary.
		/// - Copying cells is dramatically faster than copying bytes, and even
		///   faster than copying 32 bits at a time with a pinned array. A 
		///   standard for loop copies 64 cells more than 4 times faster than it
		///   copies 256 bytes; it's not much slower than Array.Copy.
		public unsafe static void ByteArrayAccess()
		{
			byte[] array1 = new byte[256];
			byte[] array2 = new byte[256];
			int total1 = 0, total2 = 0, total3 = 0;
			const int Iterations = 2000000;

			SimpleTimer t = new SimpleTimer();
			// Write test #1
			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < 256; B++)
					array1[B] = (byte)B;
			int time1 = t.Restart();

			// Write test #1b
			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < array2.Length; B++)
					array2[B] = (byte)B;
			int time1b = t.Restart();

			// Write test #2
			for (int i = 0; i < Iterations; i++) {
				fixed (byte* p = array1) {
					byte* p2 = p; // compiler won't let p change

					// Fill the byte array with an advancing pointer
					int left = array1.Length;
					for (int B = 0; left-- > 0; B++)
						*p2++ = (byte)B;
				}
			}
			int time2 = t.Restart();

			// Write test #2b
			for (int i = 0; i < Iterations; i++) {
				fixed (byte* p = array1) {
					int length = array1.Length;
					for (int B = 0; B < length; B++)
						p[B] = (byte)B;
				}
			}
			int time2b = t.Restart();

			// Write test #3
			for (int i = 0; i < Iterations; i++)
			{
				fixed (byte* p = array1)
				{
					// Do effectively the same thing as the first two loops (on a 
					// little-endian machine), but 32 bits at once
					uint* p2 = (uint*)p;
					int length2 = array1.Length >> 2;
					for (int dw = 0; dw < length2; dw++)
					{
						uint B = (uint)dw << 2;
						p2[dw] = B | ((B + 1) << 8) | ((B + 2) << 16) | ((B + 3) << 24);
					}
				}
			}
			int time3 = t.Restart();

			// Write test #3b
			for (int i = 0; i < Iterations; i++)
			{
				fixed (byte* p = array1)
				{
					// same as the last test, but with an advancing pointer
					uint* p2 = (uint*)p;
					for (uint B = 0; B < array1.Length; B += 4)
						*p2++ = B | ((B + 1) << 8) | ((B + 2) << 16) | ((B + 3) << 24);
				}
			}
			int time3b = t.Restart();

			// Write test #4
			for (int i = 0; i < Iterations; i++) {
				int left2 = array1.Length >> 2;
				fixed (byte* p = array1) {
					// Fast fill: fill with zeros
					uint* p2 = (uint*)p;
					while (left2-- > 0)
						*p2++ = 0;
				}
			}
			int time4 = t.Restart();

			// Write test #5
			for (int i = 0; i < Iterations; i++) {
				fixed (byte* p = array1) {
					// same as test #3, but unrolled
					uint* p2 = (uint*)p;
					for (uint B = 0; B < array1.Length; B += 4) {
						*p2++ = B | ((B + 1) << 8) | ((B + 2) << 16) | ((B + 3) << 24);
						B += 4;
						*p2++ = B | ((B + 1) << 8) | ((B + 2) << 16) | ((B + 3) << 24);
					}
				}
			}
			int time5 = t.Restart();

			Console.WriteLine("Performance of writing a byte array (256B * 2M):");
			Console.WriteLine("    Standard for loop: {0}ms or {1}ms", time1, time1b);
			Console.WriteLine("    Pinned pointer, one byte at a time: {0}ms", time2);
			Console.WriteLine("    Pinned pointer, 32 bits at a time: {0}ms or {1}ms", time3, time3b);
			Console.WriteLine("    Pinned pointer, 32-bit fast fill: {0}ms", time4);
			Console.WriteLine("    Pinned pointer, 32 bits, unrolled: {0}ms", time5);
			t.Restart();

			// Read test #1
			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < array1.Length; B++)
					total1 += array1[B];
			time1 = t.Restart();

			// Read test #2
			for (int i = 0; i < Iterations; i++) {
				fixed (byte* p = array1) {
					for (int B = 0; B < array1.Length; B++)
						total2 += p[B];
				}
			}
			time2 = t.Restart();

			// Read test #2b
			int total2b = 0;
			for (int i = 0; i < Iterations; i++)
			{
				int left = array1.Length;
				fixed (byte* p = array1)
				{
					byte* p2 = p;
					while (left-- > 0)
						total2b += *p2++;
				}
			}
			time2b = t.Restart();

			// Read test #3
			for (int i = 0; i < Iterations; i++)
			{
				int left2 = array1.Length >> 2;
				fixed (byte* p = array2)
				{
					uint* p2 = (uint*)p;
					while (left2-- > 0)
					{
						uint v = *p2++;
						total3 += (int)((v >> 24) + (byte)(v >> 16) + (byte)(v >> 8) + (byte)v);
					}
				}
			}
			time3 = t.Restart();

			// Read test #4
			int dummy = 0; // to prevent overoptimization
			for (int i = 0; i < Iterations; i++)
			{
				int left2 = array1.Length >> 2;
				fixed (byte* p = array2)
				{
					uint* p2 = (uint*)p;
					while (left2-- > 0)
						dummy += (int)(*p2++);
				}
			}
			time4 = t.Restart() | (dummy & 1);

			// Read test #5
			int total5 = 0;
			for (int i = 0; i < Iterations; i++) {
				int lengthDW = array1.Length >> 2;
				fixed (byte* p = array2) {
					// same as test #3, but unrolled
					uint* p2 = (uint*)p;
					for (int dw = 0; dw < lengthDW;) {
						uint v = p2[dw++];
						total5 += (int)((v >> 24) + (byte)(v >> 16) + (byte)(v >> 8) + (byte)v);
						v = p2[dw++];
						total5 += (int)((v >> 24) + (byte)(v >> 16) + (byte)(v >> 8) + (byte)v);
					}
				}
			}
			time5 = t.Restart();

			Console.WriteLine("Performance of reading a byte array:");
			Console.WriteLine("    Standard for loop: {0}ms", time1);
			Console.WriteLine("    Pinned pointer, one byte at a time: {0}ms or {1}ms", time2, time2b);
			Console.WriteLine("    Pinned pointer, 32 bits at a time, equivalent: {0}ms", time3);
			Console.WriteLine("    Pinned pointer, 32 bits at a time, sans math: {0}ms", time4);
			Console.WriteLine("    Pinned pointer, 32 bits, unrolled: {0}ms", time5);
			t.Restart();

			if (total1 != total2 || total2 != total3 || total2 != total2b || total3 != total5)
				throw new Exception("bug");

			// Copy test #1
			for (int i = 0; i < Iterations; i++)
			{
				for (int B = 0; B < array1.Length; B++)
					array2[B] = array1[B];
			}
			time1 = t.Restart();

			// Copy test #2
			for (int i = 0; i < Iterations; i++)
			{
				int left = array1.Length;
				fixed (byte* p1_ = array1)
				fixed (byte* p2_ = array2)
				{
					byte* p1 = p1_, p2 = p2_;
					while (left-- > 0)
						*p1++ = *p2++;
				}
			}
			time2 = t.Restart();

			// Copy test #3
			for (int i = 0; i < Iterations; i++)
			{
				int left2 = array1.Length >> 2;
				fixed (byte* p1 = array1)
				fixed (byte* p2 = array2)
				{
					uint* dw1 = (uint*)p1;
					uint* dw2 = (uint*)p2;
					while (left2-- > 0)
						*dw1++ = *dw2++;
				}
			}
			time3 = t.Restart();

			// Copy test #4
			for (int i = 0; i < Iterations; i++)
			{
				for (int dw = 0; dw < (array1.Length >> 2); dw++)
				{
					fixed (byte* p1 = &array1[dw << 2])
					fixed (byte* p2 = &array2[dw << 2])
					{
						uint* dw1 = (uint*)p1;
						uint* dw2 = (uint*)p2;
						*dw1 = *dw2;
					}
				}
			}
			time4 = t.Restart();

			// Copy test #5
			for (int i = 0; i < Iterations; i++)
				Array.Copy(array1, array2, array1.Length);
			time5 = t.Restart();

			// Copy test #5b
			for (int i = 0; i < Iterations; i++)
				Array.Copy(array1, 0, array2, 0, array1.Length);
			int time5b = t.Restart();

			// Copy test #6
			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < array1.Length; B += 16)
					Array.Copy(array1, B, array2, B, 16);
			int time6 = t.Restart();

			Console.WriteLine("Performance of copying a byte array:");
			Console.WriteLine("    Standard for loop: {0}ms", time1);
			Console.WriteLine("    Pinned pointer, one byte at a time: {0}ms", time2);
			Console.WriteLine("    Pinned pointer, 32 bits at a time: {0}ms", time3);
			Console.WriteLine("    Repeated pinning, 32 bits at a time: {0}ms", time4);
			Console.WriteLine("    Array.Copy, 256 bytes at a time: {0}ms or {1}ms", time5, time5b);
			Console.WriteLine("    Array.Copy, 16 bytes at a time: {0}ms", time6);

			Cell[] cells = new Cell[64];
			
			// Cell write test #1
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
				{
					cells[c].a = (byte)c;
					cells[c].b = (byte)c;
					cells[c].c = (byte)c;
					cells[c].d = (byte)c;
				}
			time1 = t.Restart();

			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < 64; c++)
				{
					cells[c].a = (byte)c;
					cells[c].b = (byte)c;
					cells[c].c = (byte)c;
					cells[c].d = (byte)c;
				}
			time1b = t.Restart();

			// Cell write test #2
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
				{
					Cell cell = new Cell();
					cell.a = (byte)c;
					cell.b = (byte)c;
					cell.c = (byte)c;
					cell.d = (byte)c;
					cells[c] = cell;
				}
			time2 = t.Restart();

			// Cell write test #3
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
					cells[c] = new Cell((byte)c, (byte)c, (byte)c, (byte)c);
			time3 = t.Restart();

			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
					cells[c] = new Cell(c, c, c, c);
			time3b = t.Restart();

			// Cell write test #4
			for (int i = 0; i < Iterations; i++)
				for (int c = 1; c < cells.Length; c++)
				{
					Cell temp = cells[c - 1];
					temp.a++;
					cells[c] = temp;
				}
			time4 = t.Restart();

			for (int i = 0; i < Iterations; i++)
				for (int c = 1; c < cells.Length; c++)
					cells[c].a = (byte)(cells[c - 1].a + 1);
			int time4b = t.Restart();

			Console.WriteLine("Performance of writing a cell array (256B * 2M):");
			Console.WriteLine("    One byte at a time: {0}ms or {1}ms", time1, time1b);
			Console.WriteLine("    One byte at a time + copy: {0}ms", time2);
			Console.WriteLine("    Constructor calls: {0}ms (bytes), {1}ms (ints)", time3, time3b);
			Console.WriteLine("    Read-inc-write every fourth byte: {0}ms or {1}ms", time3, time3b);

			// Cell read test #1
			total1 = 0;
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
				{
					total1 += cells[c].a;
					total1 += cells[c].b;
					total1 += cells[c].c;
					total1 += cells[c].d;
				}
			time1 = t.Restart();

			total2 = 0;
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
					total2 += (cells[c].a + cells[c].b) + (cells[c].c + cells[c].d);
			time1b = t.Restart();

			total3 = 0;
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
				{
					Cell cell = cells[c];
					total3 += cell.a;
					total3 += cell.b;
					total3 += cell.c;
					total3 += cell.d;
				}
			time2 = t.Restart();

			int total4 = 0;
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
				{
					Cell cell = cells[c];
					total4 += (cell.a + cell.b) + (cell.c + cell.d);
				}
			time2b = t.Restart();

			Console.WriteLine("Performance of reading the cells:");
			Console.WriteLine("    In-place: {0}ms or {1}ms", time1, time1b);
			Console.WriteLine("    From stack copy: {0}ms or {1}ms", time2, time2b);
			if (total1 != total2 || total2 != total3 || total3 != total4)
				throw new Exception("bug");

			// Cell copy test #1
			Cell[] cells2 = new Cell[64];
			t.Restart();
			for (int i = 0; i < Iterations; i++)
				for (int c = 0; c < cells.Length; c++)
					cells2[c] = cells[c];
			time1 = t.Restart();

			// Cell copy test #2
			for (int i = 0; i < Iterations; i++)
				Array.Copy(cells, cells2, cells.Length);
			time2 = t.Restart();

			for (int i = 0; i < Iterations; i++)
				Array.Copy(cells, 0, cells2, 0, cells.Length);
			time2b = t.Restart();
			
			Console.WriteLine("Performance of copying a cell array");
			Console.WriteLine("    Standard for loop: {0}ms", time1);
			Console.WriteLine("    Array.Copy: {0}ms or {1}ms", time2, time2b);
		}
		
		struct Cell
		{
			public Cell(int a)
			{
				this.a = (byte)a;
				b = c = d = 0;
			}
			public Cell(byte a, byte b, byte c, byte d)
			{
				this.a = a;
				this.b = b;
				this.c = c;
				this.d = d;
			}
			public Cell(int a, int b, int c, int d)
			{
				this.a = (byte)a;
				this.b = (byte)b;
				this.c = (byte)c;
				this.d = (byte)d;
			}
			public byte a, b, c, d;
		}

		/*public static void CountOnes()
		{
			SimpleTimer t = new SimpleTimer();

			int total1 = 0, total2 = 0;
			for (int i = 0; i < 0x10000000; i++)
				total1 += MathEx.CountOnes((uint)i);
			int time1 = t.Restart();
			
			for (int i = 0; i < 0x10000000; i++)
				total2 += G.CountOnesAlt((uint)i);
			int time2 = t.Restart();

			Console.WriteLine("CountOnes 268M times: {0}ms or {1}ms", time1, time2);

			if (total1 != total2)
				throw new Exception("bug");
		}*/

		public delegate T Iterator<T>(ref bool end);
		public static bool MoveNext<T>(this Iterator<T> it, out T value)
		{
			bool ended = false;
			value = it(ref ended);
			return !ended;
		}

		static IEnumerator<int> Counter1(int limit)
		{
			for (int i = 0; i < limit; i++)
				yield return i;
		}
		static Iterator<int> Counter2(int limit)
		{
			int i = -1;
			return (ref bool ended) =>
			{
				if (++i < limit)
					return i;
				ended = true;
				return 0;
			};
		}
		public static void EnumeratorVsIterator()
		{
			SimpleTimer t = new SimpleTimer();
			int total1 = 0, total2 = 0, total2b = 0;
			const int Limit = 333333333;

			for (int i = 0; i < 3; i++)
			{
				Console.Write("IEnumerator<int>...  ");
				t.Restart();
				var b1 = Counter1(Limit);
				int current;
				while (b1.MoveNext())
					current = b1.Current;
				total1 += t.Millisec;
				Console.WriteLine("{0} seconds", t.Millisec * 0.001);

				Console.Write("Iterator<int>...     ");
				t.Restart();
				var b2 = Counter2(Limit);
				bool ended = false;
				do
					current = b2(ref ended);
				while (!ended);
				total2 += t.Millisec;
				Console.WriteLine("{0} seconds", t.Millisec * 0.001);

				Console.Write("Iterator.MoveNext... ");
				t.Restart();
				b2 = Counter2(Limit);
				while (b2.MoveNext(out current)) { }
				total2b += t.Millisec;
				Console.WriteLine("{0} seconds", t.Millisec * 0.001);
			}
			Console.WriteLine("On average, IEnumerator consumes {0:0.0}% as much time as Iterator.", total1 * 100.0 / total2);
			Console.WriteLine("However, Iterator.MoveNext needs {0:0.0}% as much time as Iterator used directly.", total2b * 100.0 / total2);
			Console.WriteLine();
		}

		internal static void BenchmarkSets(string[] words)
		{
			// Synthesize more words, in order to increase the size limit
			Random _r = new Random();
			HashSet<string> words2 = new HashSet<string>();
			Debug.Assert(words.Length * words.Length >= 250000);
			for (int i = 0; words2.Count < 200000; i++)
				words2.Add(string.Format("{0} {1} {2}", words[_r.Next(words.Length)], words[_r.Next(words.Length)], i));

			// Our goal is to compare InternalSet to HashSet. Try three data types:
			// Symbol, string and int (performance is expected to be much worse 
			// than HashSet in the last case).
			var symbols = words2.Select(s => GSymbol.Get(s)).ToList().Randomized();
			var words3 = words2.ToList().Randomized();
			var numbers = Enumerable.Range(0, words2.Count).ToList().Randomized();
			new BenchmarkMaps<Symbol>().Run(symbols);
			new BenchmarkSets<Symbol>().Run(symbols);
			new BenchmarkSets<string>().Run(words3);
			new BenchmarkSets<int>().Run(numbers);
		}

		public static void LinqVsForLoop()
		{
			Random r = new Random();
			List<int> numbers = new List<int>();
			for (int i = 0; i < 1000000; i++)
				numbers.Add(r.Next(1000000));

			SimpleTimer t = new SimpleTimer();
			List<int> numbers2 = null;
			for (int trial = 0; trial < 200; trial++)
			{
				numbers2 = (from n in numbers where n < 100000 select n + 1).ToList();
			}
			Console.WriteLine("LINQ:    {0}ms ({1} results)", t.Restart(), numbers2.Count);

			for (int trial = 0; trial < 200; trial++)
			{
				numbers2 = new List<int>();
				for (int i = 0; i < numbers.Count; i++) {
					int n = numbers[i];
					if (n < 100000)
						numbers2.Add(n + 1);
				}
			}
			Console.WriteLine("for:     {0}ms ({1} results)", t.Restart(), numbers2.Count);

			for (int trial = 0; trial < 200; trial++)
			{
				numbers2 = new List<int>();
				foreach (int n in numbers) {
					if (n < 100000)
						numbers2.Add(n + 1);
				}
			}
			Console.WriteLine("foreach: {0}ms ({1} results)", t.Restart(), numbers2.Count);
		}


		public static void ConvexHull()
		{
			int[] testSizes = new int[] { 12345, 100, 316, 1000, 3160, 10000, 31600, 100000, 316000, 1000000, 3160000, 10000000 };
			for (int iter = 0; iter < testSizes.Length; iter++) {
				Random r = new Random();
				
				List<PointD> points = new List<PointD>(testSizes[iter]);
				for (int i = 0; i < points.Capacity; i++) {
					double size = r.NextDouble();
					double ang = r.NextDouble() * (Math.PI * 2);
					points.Add(new PointD(size * Math.Cos(ang), size * Math.Sin(ang)));
				}
				// Plus: test sorting time to learn how much of the time is spent sorting
				var points2 = new List<PointD>(points);
				EzStopwatch timer = new EzStopwatch(true);
				points2.Sort((a, b) => a.X == b.X ? a.Y.CompareTo(b.Y) : (a.X < b.X ? -1 : 1));
				Stopwatch timer2 = new Stopwatch(); timer2.Start();
				int sortTime = timer.Restart();
				IListSource<Point<double>> output = PointMath.ComputeConvexHull(points, true);
				int hullTime = timer.Millisec;
				Console.WriteLine("{0:c}   (ticks:{1,10} freq:{2})", timer2.Elapsed, timer2.ElapsedTicks, Stopwatch.Frequency);

				if (iter == 0) continue; // first iteration primes the JIT/caches
				Console.WriteLine("Convex hull of {0,8} points took {1} ms ({2} ms for sorting step). Output has {3} points.", 
					testSizes[iter], hullTime, sortTime, output.Count);
			}
		}
	}
}
