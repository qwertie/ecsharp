using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Loyc.BooStyle;
using Loyc.Runtime;
using Loyc.Utilities;
using Loyc.CompilerCore;
using Loyc.CompilerCore.ExprParsing;
using Loyc.CompilerCore.ExprNodes;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Loyc.BooStyle.Tests
{
	class Program
	{
		public static PoorMansLinq<T> Linq<T>(IEnumerable<T> source)
		{
			return new PoorMansLinq<T>(source);
		}
		public static void Main(string[] args)
		{
			Console.WriteLine("Running tests on stable code...");
			RunTests.Run(new SimpleCacheTests());
			RunTests.Run(new GTests());
			RunTests.Run(new HashTagsTests());
			RunTests.Run(new StringCharSourceTests());
			RunTests.Run(new StreamCharSourceTests(Encoding.Unicode, 256));
			RunTests.Run(new StreamCharSourceTests(Encoding.Unicode, 16));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 256));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 16));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF8, 27));
			RunTests.Run(new StreamCharSourceTests(Encoding.UTF32, 64));
			RunTests.Run(new ThreadExTests());
			RunTests.Run(new ExtraTagsInWListTests());
			RunTests.Run(new LocalizeTests());

			for(;;) {
				ConsoleKeyInfo k;
				string s;
				Console.WriteLine();
				Console.WriteLine("What do you want to do?");
				Console.WriteLine("1. Run unit tests that expect exceptions");
				Console.WriteLine("2. Run unit tests on unstable code");
				Console.WriteLine("3. Try out BooLexer");
				Console.WriteLine("4. Try out BasicOneParser with standard operator set (not done)");
				Console.WriteLine("5. Parse LAIF");
				Console.WriteLine("9. Benchmarks");
				Console.WriteLine("Z. List encodings");
				Console.WriteLine("Press ESC or ENTER to Quit");
				Console.WriteLine((k = Console.ReadKey(true)).KeyChar);
				if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Enter)
					break;
				else if (k.KeyChar == '1') {
					RunTests.Run(new SymbolTests());
					RunTests.Run(new RWListTests()); 
					RunTests.Run(new WListTests());
					RunTests.Run(new RVListTests());
					RunTests.Run(new VListTests());
					RunTests.Run(new ParseTokenTests());
				} else if (k.KeyChar == '2') {
					RunTests.Run(new BooLexerCoreTest());
					RunTests.Run(new BooLexerTest());
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), false));
					RunTests.Run(new OneParserTests(new BasicOneParser<AstNode>(), true));
					RunTests.Run(new EssentialTreeParserTests());
					RunTests.Run(new LaifParserTests());
				}
				else if (k.KeyChar == '3')
				{
					BooLanguage lang = new BooLanguage();
					Console.WriteLine("Boo Lexer: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						Lexer(lang, s);
				} else if (k.KeyChar == '4') {
					BooLanguage lang = new BooLanguage();
					Console.WriteLine("BasicOneParser: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						OneParserDemo(lang, s);
				} else if (k.KeyChar == '5') {
					Console.WriteLine("LaifParser: Type input, or a blank line to stop.");
					while ((s = System.Console.ReadLine()).Length > 0)
						ParseLaif(s);
				} else if (k.KeyChar == '9') {
					Benchmarks();
				} else if (k.KeyChar == 'z' || k.KeyChar == 'Z') {
					foreach (EncodingInfo inf in Encoding.GetEncodings())
						Console.WriteLine("{0} {1}: {2}", inf.CodePage, inf.Name, inf.DisplayName);
				}
			}
		}

		static void Lexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(s, "Boo");
			BooLexer lexer = new BooLexer(input, lang.StandardKeywords, false);

			foreach (Loyc.CompilerCore.AstNode t in lexer) {
				System.Console.WriteLine("{0} <{1}>", t.NodeType, t.SourceText);
			}
		}
		static void TreeLexer(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(s, lang.LanguageName);
			IEnumerable<AstNode> lexer = new BooLexer(input, lang.StandardKeywords, false);
			EssentialTreeParser etp = new EssentialTreeParser();
			AstNode root = AstNode.New(SourceRange.Nowhere, Symbol.Empty);
			etp.Parse(ref root, lexer); // May print errors
		}

		private static void OneParserDemo(ILanguageStyle lang, string s)
		{
			StringCharSourceFile input = new StringCharSourceFile(s, lang.LanguageName);
			IEnumerable<AstNode> lexer = new BooLexer(input, lang.StandardKeywords, true);
			IEnumerable<AstNode> lexFilter = new VisibleTokenFilter<AstNode>(lexer);
			List<AstNode> tokens = Linq(lexFilter).ToList();
			EnumerableSource<AstNode> source = new EnumerableSource<AstNode>(tokens);
			int pos = 0;
			IOneParser<AstNode> parser = new BasicOneParser<AstNode>(OneParserTests.TestOps);
			OneOperatorMatch<AstNode> expr = parser.Parse(source, ref pos, false);
			System.Console.WriteLine("Parsed as: " + OneParserTests.BuildResult(expr));
		}

		private static void ParseLaif(string input)
		{
			LaifParser p = new LaifParser();
			RVList<AstNode> output = p.Parse(
				new StringCharSourceFile(input, "Laif"), SourceRange.Nowhere.Source);
			
			Console.WriteLine("TODO");
		}
		private static void Benchmarks()
		{
			Benchmark.ByteArrayAccess();
			Benchmark.ThreadLocalStorage();
		}
	}

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
		
		/// This benchmark is for the sake of JPTrie, which encodes keys in a
		/// byte array. Often, it needs to do operations that operate on 4 bytes
		/// at a time, so in this benchmark I attempt to do the same operations
		/// 1 byte at a time and 4 bytes at a time, to compare the difference.
		/// 
		/// Note that the arrays are small and therefore likely to be in L1 
		/// cache. Consequently, any inefficiency in the code produced by JIT
		/// tends to be obvious in this benchmark, as it is not hidden behind 
		/// memory latency.
		/// 
		/// Unfortunately, these benchmarks suggest that it is impossible to 
		/// approach the theoretical optimum speed using managed code. On my 
		/// machine, Array.Copy can copy 256 bytes 1,000,000 times in 78 ms.
		/// Array.Copy requires 9 times as long to do the same operation 16
		/// bytes at a time, which suggests that Array.Copy has a very high 
		/// call overhead. About half of the 78 ms is probably overhead, so
		/// the theoretical optimum speed must be under 50 ms.
		/// 
		/// The fastest equivalent with purely managed code is to copy 32 bits
		/// at a time with a pair of pinned pointers, and this takes 171 ms.
		/// 
		/// Strangely, you don't gain any performance by using pointers if you
		/// access the array one byte at a time. To the contrary, using the 
		/// managed byte array directly is generally faster than using a 
		/// pointer, unless you access the array 32 bits at a time.
		/// 
		/// Also very strangely, reading from the array (and summing up the 
		/// elements) tends to be slower than writing to the array (writing 
		/// the array index into each element).
		/// 
		/// Another notable result is that pinning an array seems to be a
		/// cheap operation. One copying test pins the arrays for every 4 
		/// bytes copied, and this only takes 234 ms, versus 171 ms if the 
		/// array is pinned once per 256 bytes.
		/// 
		/// There is one operation that can be done fast in managed code with 
		/// pointers: a 32-bit fill in which the same value, or a counter, is
		/// written to each element. This is about the same speed as the 
		/// 256-byte Array.Copy operation.
		public unsafe static void ByteArrayAccess()
		{
			byte[] array1 = new byte[256];
			byte[] array2 = new byte[256];
			int total1 = 0, total2 = 0, total3 = 0;
			const int Iterations = 2000000;

			SimpleTimer t = new SimpleTimer();
			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < 256; B++)
					array1[B] = (byte)B;
			int time1 = t.Restart();

			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < array2.Length; B++)
					array2[B] = (byte)B;
			int time1b = t.Restart();

			for (int i = 0; i < Iterations; i++)
			{
				int left2 = array1.Length >> 2;
				fixed (byte* p = array1)
				{
					// Fast fill: fill with zeros
					uint* p2 = (uint*)p;
					while (left2-- > 0)
						*p2++ = 0;
				}
			}
			int time2 = t.Restart();

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

			for (int i = 0; i < Iterations; i++)
			{
				fixed (byte* p = array1)
				{
					byte* p2 = p; // compiler won't let p change

					// Fill the byte array with an advancing pointer
					int left = array1.Length;
					for (int B = 0; left-- > 0; B++)
						*p2++ = (byte)B;
				}
			}
			int time4 = t.Restart();

			Console.WriteLine("Performance of writing a byte array:");
			Console.WriteLine("    Standard for loop: {0}ms or {1}ms", time1, time1b);
			Console.WriteLine("    Pinned pointer, one byte at a time: {0}ms", time4);
			Console.WriteLine("    Pinned pointer, 32 bits at a time: {0}ms or {1}ms", time3, time3b);
			Console.WriteLine("    Pinned pointer, 32-bit fast fill: {0}ms", time2);
			t.Restart();

			for (int i = 0; i < Iterations; i++)
				for (int B = 0; B < array1.Length; B++)
					total1 += array1[B];
			time1 = t.Restart();

			for (int i = 0; i < Iterations; i++)
			{
				int left = array1.Length;
				fixed (byte* p = array1)
				{
					byte* p2 = p;
					while (left-- > 0)
						total2 += *p2++;
				}
			}
			time2 = t.Restart();

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

			int dummy = 0; // to thwart optimizer
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

			if (total1 != total2 || total2 != total3)
				throw new Exception("bug");

			Console.WriteLine("Performance of reading a byte array:");
			Console.WriteLine("    Standard for loop: {0}ms", time1);
			Console.WriteLine("    Pinned pointer, one byte at a time: {0}ms", time2);
			Console.WriteLine("    Pinned pointer, 32 bits at a time, equivalent: {0}ms", time3);
			Console.WriteLine("    Pinned pointer, 32 bits at a time, sans math: {0}ms", time4);
			t.Restart();

			for (int i = 0; i < Iterations; i++)
			{
				for (int B = 0; B < array1.Length; B++)
					array2[B] = array1[B];
			}
			time1 = t.Restart();

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

			for (int i = 0; i < Iterations; i++)
				Array.Copy(array1, array2, array1.Length);
			int time5 = t.Restart();

			for (int i = 0; i < Iterations; i++)
				Array.Copy(array1, 0, array2, 0, array1.Length);
			int time5b = t.Restart();

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
		}
	}
}
