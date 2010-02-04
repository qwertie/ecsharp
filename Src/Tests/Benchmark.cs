using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;
using System.Threading;
using Loyc.Utilities;
using Tests.Resources;
using System.Diagnostics;

namespace Loyc.Tests
{
	class Benchmark
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

		/// This benchmark is for the sake of JPTrie, which encodes keys in a byte
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

		static int _randomSeed = 0;
		static Random _random = new Random(_randomSeed);

		public static void CPTrieBenchmark()
		{
			Console.WriteLine("                                    String dictionary          CPStringTrie        ");
			Console.WriteLine("Scenario            Reps  Sec.size  Fill   Scan   Memory+Keys  Fill   Scan   Memory");
			Console.WriteLine("--------            ----  --------  ----   ----   ------ ----  ----   ----   ------");
			
			// Obtain the word list
			string[] words = Resources.WordList.Split(new string[] 
				{ "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			// - Basic word list, 5 iterations
			CPTrieBenchmarkLine(null,              words, words.Length, 1);
			CPTrieBenchmarkLine("Basic word list", words, words.Length, 10);

			// - 1,000,000 random word pairs, section sizes of 4, 8, 16, 32, 64,
			//   125, 250, 500, 1000, 2000, 4000, 8000, 16000, 32000, 64000,
			//   125000, 250000, 500000, 1000000.
			string[] pairs1 = BuildPairs(words, words, " ", 1000000);

			CPTrieBenchmarkLine("1,000,000 pairs", pairs1, 1000000, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,  100000, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,   10000, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,    1000, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,     500, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,     250, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,     125, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,      64, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,      32, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,      16, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,       8, 1);
			CPTrieBenchmarkLine("1,000,000 pairs", pairs1,       4, 1);

			// - 1,000,000 word pairs with limited prefixes
			string[] prefixes = new string[] {
				"a", "at", "the", "them", "some", "my", "your", "do", "good", "bad", "ugly", "***",
				"canned", "erroneous", "fracking", "hot", "inner", "John", "kill", "loud", "muddy",
				"no", "oh", "pro", "quality", "red", "unseen", "valuable", "wet", "x", "ziffy"
			};
			string name = "1,000,000 pre." + prefixes.Length.ToString();
			string[] pairs2 = BuildPairs(prefixes, words, " ", 1000000);
			CPTrieBenchmarkLine(name, pairs2, 1000000, 1);
			CPTrieBenchmarkLine(name, pairs2,     500, 1);
			CPTrieBenchmarkLine(name, pairs2, 250, 1);
			CPTrieBenchmarkLine(name, pairs2, 125, 1);
			CPTrieBenchmarkLine(name, pairs2, 64, 1);
			CPTrieBenchmarkLine(name, pairs2, 32, 1);
			CPTrieBenchmarkLine(name, pairs2, 16, 1);
			CPTrieBenchmarkLine(name, pairs2, 8, 1);
			CPTrieBenchmarkLine(name, pairs2, 4, 1);

		}

		private static string[] BuildPairs(string[] words1, string[] words2, string separator, int numPairs)
		{
			Dictionary<string, string> dict = new Dictionary<string,string>();
			string[] pairs = new string[numPairs];
			
			do {
				string pair = words1[_random.Next(words1.Length)] + separator + words2[_random.Next(words2.Length)];
				dict[pair] = null;
			} while (dict.Count < numPairs);

			int i = 0;
			foreach(string key in dict.Keys)
				pairs[i++] = key;
			Debug.Assert(i == pairs.Length);

			return pairs;
		}

		public static void CPTrieBenchmarkLine(string name, string[] words, int sectionSize, int reps)
		{
			int dictFillTime = 0, trieFillTime = 0;
			int dictScanTime = 0, trieScanTime = 0;
			long dictMemory = 0, trieMemory = 0;
			for (int rep = 0; rep < reps; rep++) {
				IDictionary<string, string>[] dicts, tries;

				GC.Collect();
				dictFillTime += Fill(words, sectionSize, out dicts, 
					delegate() { return new Dictionary<string,string>(); });
				trieFillTime += Fill(words, sectionSize, out tries, 
					delegate() { return new CPStringTrie<string>(); });

				Scramble(words, sectionSize);

				for (int i = 0; i < dicts.Length; i++)
					dictMemory += CountMemoryUsage((Dictionary<string, string>)dicts[i], 4, 4);
				for (int i = 0; i < dicts.Length; i++)
					trieMemory += ((CPStringTrie<string>)tries[i]).CountMemoryUsage(4);
				
				GC.Collect();
				dictScanTime += Scan(words, sectionSize, dicts);
				trieScanTime += Scan(words, sectionSize, tries);
			}

			// A CPStringTrie encodes its keys directly into the tree so that no
			// separate memory is required to hold the keys. Therefore, if you want
			// to compare the memory use of Dictionary and CPStringTrie, you should
			// normally count the size of the keys against the Dictionary, but not
			// against the trie. 
			// 
			// In this contrived example, however, the values are the same as the 
			// keys, so no memory is saved by encoding the keys in the trie.
			int keyMemory = 0;
			for (int i = 0; i < words.Length; i++)
				// Note: This is only a guess about the overhead of System.String.
				keyMemory += 16 + (words[i].Length & ~1) * 2;

			if (name != null)
			{
				int dictKB = (int)((double)dictMemory / 1024 / reps + 0.5);
				int trieKB = (int)((double)trieMemory / 1024 / reps + 0.5);
				int  keyKB = (int)((double) keyMemory / 1024        + 0.5);
				Console.WriteLine("{0,-20}{1,4}  {2,8} {3,4}ms {4,4}ms {5,5}K+{6,4}K {7,4}ms  {8,4}ms  {9,5}K",
					name, reps, sectionSize, dictFillTime / reps, dictScanTime / reps, dictKB, keyKB,
											 trieFillTime / reps, trieScanTime / reps, trieKB);
			}
		}

		private static long CountMemoryUsage<Key,Value>(Dictionary<Key,Value> dict, int keySize, int valueSize)
		{
			// As you can see in reflector, a Dictionary contains two arrays: a
			// list of "entries" and a list of "buckets". As you can see if you
			// open Resize() in reflector, the two arrays are the same size and
			// whenever the dictionary runs out of space, it roughly doubles in
			// size. The arrays are not allocated until the first item is added.
			// 12 additional bytes are allocated for a ValueCollection if you
			// call the Values property, but I'm not counting that here.
			int size = (11 + 2) * 4;
			if (dict.Count > 0)
			{
				size += 12 + 12; // Array overheads

				// The size per element is sizeof(Key) + sizeof(Value) + 12 (4 bytes
				// are in "buckets" and the rest are in "entries").
				//     There is no Capacity property so we can't tell how big the
				// arrays are currently, but on average, 50% of the entries are
				// unused, so assume that amount of overhead.
				int elemSize = 12 + keySize + valueSize;
				int usedSize = elemSize * dict.Count;
				size += usedSize + (usedSize >> 1);
			}
			return size;
		}

		private static void Scramble(string[] words, int sectionSize)
		{
			for (int offset = 0; offset < words.Length; offset += sectionSize) {
				int end = Math.Min(words.Length, offset + sectionSize);
				for (int i = offset; i < end; i++)
					G.Swap(ref words[i], ref words[_random.Next(offset, end)]);
			}
		}

		public static int Fill(string[] words, int sectionSize, out IDictionary<string, string>[] dicts, Func<IDictionary<string, string>> factory)
		{
			dicts = new IDictionary<string,string>[(words.Length - 1) / sectionSize + 1];
			for (int sec = 0; sec < dicts.Length; sec++)
				dicts[sec] = factory();

			SimpleTimer t = new SimpleTimer();

			for (int j = 0; j < sectionSize; j++) {
				for (int i = j, sec = 0; i < words.Length; i += sectionSize, sec++)
					dicts[sec][words[i]] = words[i];
			}
			
			return t.Millisec;
		}
		public static int Scan(string[] words, int sectionSize, IDictionary<string, string>[] dicts)
		{
			SimpleTimer t = new SimpleTimer();
			int total = 0;

			for (int j = 0; j < sectionSize; j++) {
				for (int i = j, sec = 0; i < words.Length; i += sectionSize, sec++)
					total += dicts[sec][words[i]].Length;
			}
			
			return t.Millisec;
		}
	}
}
