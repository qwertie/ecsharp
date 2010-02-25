using System;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.Diagnostics;
using Loyc.Runtime;

namespace Loyc.Utilities
{
	public class CPTrieBenchmark
	{
		static int _randomSeed = Environment.TickCount;
		static Random _random = new Random(_randomSeed);
		
		/* Results on my machine
										 |--String Dictionary---|  |----SortedDictionary----|  |--CPStringTrie---|
		Scenario          Reps Sec.size  Fill   Scan  Memory+Keys  Fill    Scan   Memory+Keys  Fill   Scan  Memory
		--------          ---- --------  ----   ----  ------ ----  ----    ----   -----------  ----   ----  ------
		Basic word list     10    41238   10ms    9ms  1.0M+ 1.3M   229ms  113ms  1.1M+ 1.3M     85ms    43ms  0.9M
		Basic words opt.    10    41238    9ms    7ms  1.0M+ 1.3M   234ms  117ms  1.1M+ 1.3M     85ms    43ms  0.7M
		200K pairs           2   200000   62ms   85ms  5.1M+ 9.7M  1538ms  827ms  5.3M+ 9.7M    695ms   390ms  6.4M
		200K pairs opt.      2   200000   78ms   78ms  5.1M+ 9.7M  1562ms  851ms  5.3M+ 9.7M    772ms   343ms  5.0M
		200K pairs           2     1000  101ms   85ms  5.1M+ 9.7M   937ms  554ms  5.4M+ 9.7M    570ms   257ms  7.7M
		200K pairs           2      500  101ms   93ms  5.1M+ 9.7M   859ms  515ms  5.4M+ 9.7M    531ms   242ms  7.7M
		200K pairs           2      250   93ms   85ms  5.1M+ 9.7M   804ms  500ms  5.4M+ 9.7M    562ms   234ms  8.1M
		200K pairs           2      125  101ms   78ms  5.2M+ 9.7M   679ms  437ms  5.4M+ 9.7M    648ms   250ms  9.1M
		200K pairs opt.      2      125  101ms   85ms  5.2M+ 9.7M   703ms  453ms  5.4M+ 9.7M    717ms   218ms  7.1M
		200K pairs           2       64  101ms  101ms  5.3M+ 9.7M   538ms  367ms  5.5M+ 9.7M    601ms   218ms  9.2M
		200K pairs           2       32   85ms   93ms  5.5M+ 9.7M   437ms  296ms  5.7M+ 9.7M    343ms   171ms  6.7M
		200K pairs           2       16   85ms   62ms  6.0M+ 9.7M   312ms  234ms  6.1M+ 9.7M    320ms   140ms  7.3M
		200K pairs           2        8   93ms   62ms  6.9M+ 9.7M   249ms  171ms  6.9M+ 9.7M    382ms   125ms  9.3M
		200K pairs           2        4   85ms   54ms  8.7M+ 9.7M   179ms  117ms  8.4M+ 9.7M    343ms   117ms 10.7M
		200K pairs opt.      2        4   93ms   46ms  8.7M+ 9.7M   156ms  125ms  8.4M+ 9.7M    429ms   109ms  8.8M
		1M pairs             1  1000000  406ms  453ms 25.4M+48.4M 10218ms 5453ms 26.7M+48.4M   3859ms  2359ms 25.7M
		1M pairs opt.        1  1000000  546ms  468ms 25.4M+48.4M 10343ms 5515ms 26.7M+48.4M   4390ms  1968ms 19.5M
		1M pairs, 31 prefs.  1  1000000  375ms  453ms 25.4M+39.9M  9593ms 5203ms 26.7M+39.9M   3843ms  2046ms 21.3M
		1M pairs, 31, opt.   1  1000000  546ms  453ms 25.4M+39.9M 10265ms 5421ms 26.7M+39.9M   3811ms  1734ms 16.5M
		1M pairs, 31 prefs.  1      500 1000ms  562ms 25.6M+39.9M  5390ms 3078ms 26.8M+39.9M   2609ms  1390ms 24.9M
		1M pairs, 31 prefs.  1      250 1000ms  593ms 25.7M+39.9M  4421ms 2656ms 26.9M+39.9M   3312ms  1296ms 29.2M
		1M pairs, 31 prefs.  1      125  937ms  546ms 26.0M+39.9M  3671ms 2265ms 27.2M+39.9M   3546ms  1218ms 37.0M
		1M pairs, 31 prefs.  1       64  796ms  515ms 26.6M+39.9M  3031ms 1890ms 27.7M+39.9M   3421ms  1046ms 42.0M
		1M pairs, 31 prefs.  1       32  718ms  437ms 27.7M+39.9M  2531ms 1546ms 28.6M+39.9M   1984ms   796ms 31.1M
		1M pairs, 31 prefs.  1       16  609ms  328ms 29.9M+39.9M  1921ms 1171ms 30.5M+39.9M   1843ms   609ms 30.3M
		1M pairs, 31 prefs.  1        8  921ms  296ms 34.5M+39.9M  1406ms  875ms 34.3M+39.9M   1984ms   546ms 38.6M
		1M pairs, 31 prefs.  1        4  796ms  265ms 43.4M+39.9M  1421ms  625ms 42.0M+39.9M   1312ms   531ms 44.3M
		*/
		public static void Benchmark(string[] words)
		{
			Console.WriteLine("                                 |--String Dictionary---|  |----SortedDictionary----|  |--CPStringTrie---|");
			Console.WriteLine("Scenario          Reps Sec.size  Fill   Scan  Memory+Keys  Fill    Scan   Memory+Keys  Fill   Scan  Memory");
			Console.WriteLine("--------          ---- --------  ----   ----  ------ ----  ----    ----   -----------  ----   ----  ------");
			
			// - Basic word list, 5 iterations
			CPTrieBenchmarkLine(null,              words, words.Length, 1, false);
			CPTrieBenchmarkLine("Basic word list", words, words.Length, 10, false);
			CPTrieBenchmarkLine("Basic words opt.", words, words.Length, 10, true);

			// - 1,000,000 random word pairs, section sizes of 4, 8, 16, 32, 64,
			//   125, 250, 500, 1000, 2000, 4000, 8000, 16000, 32000, 64000,
			//   125000, 250000, 500000, 1000000.
			string[] pairs1 = BuildPairs(words, words, " ", 1000000);

			CPTrieBenchmarkLine("200K pairs",      pairs1,  200000, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs opt.", pairs1,  200000, 2, true,  200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,    1000, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,     500, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,     250, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,     125, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs opt.", pairs1,     125, 2, true,  200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,      64, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,      32, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,      16, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,       8, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs",      pairs1,       4, 2, false, 200000);
			CPTrieBenchmarkLine("200K pairs opt.", pairs1,       4, 2, true,  200000);
			CPTrieBenchmarkLine("1M pairs",        pairs1, 1000000, 1, false);
			CPTrieBenchmarkLine("1M pairs opt.",   pairs1, 1000000, 1, true);

			// - 1,000,000 word pairs with limited prefixes
			string[] prefixes = new string[] {
				"a", "at", "the", "them", "some", "my", "your", "do", "good", "bad", "ugly", "***",
				"canned", "erroneous", "fracking", "hot", "inner", "John", "kill", "loud", "muddy",
				"no", "oh", "pro", "quality", "red", "unseen", "valuable", "wet", "x", "ziffy"
			};
			string name1  = string.Format("1M pairs, {0} prefs.", prefixes.Length);
			string name2 = string.Format("1M pairs, {0}, opt.", prefixes.Length);
			string[] pairs2 = BuildPairs(prefixes, words, " ", 1000000);
			CPTrieBenchmarkLine(name1, pairs2, 1000000, 1, false);
			CPTrieBenchmarkLine(name2, pairs2, 1000000, 1, true);
			CPTrieBenchmarkLine(name1, pairs2, 500, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 250, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 125, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 64, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 32, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 16, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 8, 1, false);
			CPTrieBenchmarkLine(name1, pairs2, 4, 1, false);
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

		public static void CPTrieBenchmarkLine(string name, string[] words, int sectionSize, int reps, bool optimizeTrie)
		{
			CPTrieBenchmarkLine(name, words, sectionSize, reps, optimizeTrie, words.Length);
		}
		public static void CPTrieBenchmarkLine(string name, string[] words, int sectionSize, int reps, bool optimizeTrie, int wordCount)
		{
			int dictFillTime = 0, sdicFillTime = 0, trieFillTime = 0;
			int dictScanTime = 0, sdicScanTime = 0, trieScanTime = 0;
			long dictMemory = 0, sdicMemory = 0, trieMemory = 0;
			for (int rep = 0; rep < reps; rep++) {
				IDictionary<string, string>[] dicts, sdics, tries;

				GC.Collect();
				dictFillTime += Fill(words, wordCount, sectionSize, out dicts, 
					delegate() { return new Dictionary<string,string>(); });
				sdicFillTime += Fill(words, wordCount, sectionSize, out sdics,
					delegate() { return new SortedDictionary<string, string>(); });
				trieFillTime += Fill(words, wordCount, sectionSize, out tries, 
					delegate() { return new CPStringTrie<string>(); });

				if (optimizeTrie)
				{
					SimpleTimer t = new SimpleTimer();

					for (int i = 0; i < tries.Length; i++)
						tries[i] = ((CPStringTrie<string>)tries[i]).Clone();

					trieFillTime += t.Millisec;
				}

				for (int i = 0; i < dicts.Length; i++)
					dictMemory += CountMemoryUsage((Dictionary<string, string>)dicts[i], 4, 4);
				for (int i = 0; i < sdics.Length; i++)
					sdicMemory += CountMemoryUsage((SortedDictionary<string, string>)sdics[i], 4, 4);
				for (int i = 0; i < tries.Length; i++)
					trieMemory += ((CPStringTrie<string>)tries[i]).CountMemoryUsage(4);

				Scramble(words, wordCount, sectionSize);
				
				GC.Collect();

				dictScanTime += Scan(words, wordCount, sectionSize, dicts);
				sdicScanTime += Scan(words, wordCount, sectionSize, sdics);
				trieScanTime += Scan(words, wordCount, sectionSize, tries);
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
			for (int i = 0; i < wordCount; i++)
				// Note: I'm guessing the overhead of System.String. I assume each 
				// string has a 12-byte header (8-byte object header plus Length) 
				// and a null terminator (for native interop).
				keyMemory += 16 + (words[i].Length & ~1) * 2;

			if (name != null)
			{
				double dictMB = (double)dictMemory / (1024 * 1024) / reps;
				double sdicMB = (double)sdicMemory / (1024 * 1024) / reps;
				double trieMB = (double)trieMemory / (1024 * 1024) / reps;
				double  keyMB = (double) keyMemory / (1024 * 1024);
				string info0 = string.Format("{0,-20}{1,2} {2,8} ", name, reps, sectionSize);
				string info1 = string.Format("{0,4}ms {1,4}ms {2,4:#0.0}M+{3,4:#0.0}M ",
					dictFillTime / reps, dictScanTime / reps, dictMB, keyMB);
				string info2 = string.Format("{0,5}ms {1,4}ms {2,4:#0.0}M+{3,4:#0.0}M  ",
					sdicFillTime / reps, sdicScanTime / reps, sdicMB, keyMB);
				string info3 = string.Format("{0,5}ms {1,5}ms {2,4:#0.0}M",
					trieFillTime / reps, trieScanTime / reps, trieMB);
				Console.WriteLine(info0 + info1 + info2 + info3);
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
				// arrays are currently, but on average, 25% of the entries (1/3 of
				// of the number of entries used) are unused, so assume that amount 
				// of overhead.
				int elemSize = 12 + keySize + valueSize;
				int usedSize = elemSize * dict.Count;
				size += usedSize + (usedSize / 3);
			}
			return size;
		}

		private static long CountMemoryUsage<Key, Value>(SortedDictionary<Key, Value> dict, int keySize, int valueSize)
		{
			// As you can see in reflector, a SortedDictionary creates a TreeSet
			// and a KeyValuePairComparer in its constructor, and for each item
			// you add, it creates a Node<KeyValuePair<Key,Value>> class instance.
			int size = 
				( 3 + 2 // SortedDictionary instance
				+ 6 + 2 // TreeSet instance
				+ 1 + 2 // KeyValuePairComparer instance
				) * 4;  // 16 DWORDs empty, assuming 32-bit architecture
			
			// A node is allocated for each item, size 20 bytes + key + value
			size += ((2 + 3) * 4 + keySize + valueSize) * dict.Count;
			
			return size;
		}

		private static void Scramble(string[] words, int wordCount, int sectionSize)
		{
			for (int offset = 0; offset < wordCount; offset += sectionSize)
			{
				int end = Math.Min(wordCount, offset + sectionSize);
				for (int i = offset; i < end; i++)
					G.Swap(ref words[i], ref words[_random.Next(offset, end)]);
			}
		}

		public static int Fill(string[] words, int wordCount, int sectionSize, out IDictionary<string, string>[] dicts, Func<IDictionary<string, string>> factory)
		{
			dicts = new IDictionary<string, string>[(wordCount - 1) / sectionSize + 1];
			for (int sec = 0; sec < dicts.Length; sec++)
				dicts[sec] = factory();

			SimpleTimer t = new SimpleTimer();

			for (int j = 0; j < sectionSize; j++) {
				for (int i = j, sec = 0; i < wordCount; i += sectionSize, sec++)
					dicts[sec][words[i]] = words[i];
			}
			
			return t.Millisec;
		}
		public static int Scan(string[] words, int wordCount, int sectionSize, IDictionary<string, string>[] dicts)
		{
			SimpleTimer t = new SimpleTimer();
			int total = 0;

			for (int j = 0; j < sectionSize; j++) {
				for (int i = j, sec = 0; i < wordCount; i += sectionSize, sec++)
					total += dicts[sec][words[i]].Length;
			}
			
			return t.Millisec;
		}
	}
}
