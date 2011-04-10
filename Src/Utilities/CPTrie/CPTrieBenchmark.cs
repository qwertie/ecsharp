// http://www.codeproject.com/KB/recipes/cptrie.aspx
using System;
using System.Collections.Generic;
using System.Text;
using System.Resources;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Utilities;

namespace Loyc.Collections
{
	public class CPTrieBenchmark
	{
		static int _randomSeed = Environment.TickCount;
		static Random _random = new Random(_randomSeed);

		/* Results on my work machine:
		                                      |-Int Dictionary--|   |-SortedDictionary-|   |----CPIntTrie----|
		Scenario              Reps Set size   Fill   Scan  Memory   Fill   Scan   Memory   Fill   Scan  Memory
		--------              ---- --------   ----   ----  ------   ----   ----   ------   ----   ----  ------
		1-100,000, sorted       10   100000   15ms   12ms   2.5M    128ms   58ms   2.7M     63ms   46ms  0.9M
		1-100,000, random       10   100000   15ms    4ms   2.5M    132ms   62ms   2.7M     58ms   44ms  0.9M
		1-100,000 w/ null vals  10   100000   15ms    6ms   2.5M    129ms   58ms   2.7M     47ms   43ms  0.0M
		24-bit keys with 100K items:
		Random 24-bit ints      10   100000   19ms    9ms   2.5M    134ms   66ms   2.7M    157ms   65ms  2.0M
		Random set (null vals.) 10   100000   18ms   13ms   2.5M    134ms   62ms   2.7M    117ms   63ms  1.3M
		Clusters(20, 100,2)     10   100000   15ms    9ms   2.5M    134ms   66ms   2.7M    101ms   60ms  2.2M
		Clusters(same w/ nulls) 10   100000   18ms    9ms   2.5M    135ms   63ms   2.7M     81ms   46ms  0.2M
		Clusters(20, 100,9)     10   100000   16ms    9ms   2.5M    131ms   60ms   2.7M    134ms   65ms  2.3M
		Clusters(20,1000,2)     10   100000   21ms    9ms   2.5M    131ms   63ms   2.7M    126ms   70ms  1.4M
		Clusters(20,1000,9)     10   100000   15ms   12ms   2.5M    132ms   55ms   2.7M    129ms   74ms  1.4M
		Clusters(50, 100,2)     10   100000   15ms   10ms   2.5M    129ms   60ms   2.7M     84ms   47ms  1.6M
		Clusters(50, 100,9)     10   100000   21ms   12ms   2.5M    132ms   58ms   2.7M    128ms   52ms  3.1M
		Clusters(50,1000,2)     10   100000   15ms   12ms   2.5M    131ms   62ms   2.7M    137ms   62ms  1.7M
		Clusters(50,1000,9)     10   100000   16ms   10ms   2.5M    128ms   58ms   2.7M    143ms   70ms  2.2M
		Tests with 32-bit keys:
		Random 32-bit ints      10   100000   23ms   16ms   2.5M    154ms   58ms   2.7M    175ms   96ms  2.0M
		Random 32-bit ints       5   200000   49ms   43ms   5.1M    315ms  165ms   5.3M    468ms  237ms  5.1M
		Random 32-bit ints       3   500000  135ms  130ms  12.7M   1051ms  530ms  13.4M   1291ms  614ms  8.5M
		Random 32-bit ints       2  1000000  335ms  281ms  25.4M   2593ms 1257ms  26.7M   2390ms 1328ms 13.3M
		Exponential 32-bit      10   100000   21ms   13ms   2.5M    134ms   62ms   2.7M    185ms   88ms  1.9M
		Exponential 32-bit       5   200000   49ms   49ms   5.1M    321ms  162ms   5.3M    390ms  218ms  3.8M
		Exponential 32-bit       3   500000  140ms  135ms  12.7M   1056ms  525ms  13.4M   1234ms  567ms  9.1M
		Exponential 32-bit       2  1000000  335ms  288ms  25.4M   2671ms 1218ms  26.7M   2601ms 1218ms 17.8M
		Clusters(25,25,1)       10   100000   15ms    0ms   2.5M    129ms   62ms   2.7M     81ms   62ms  1.5M
		Clusters(25,30000,5)    10   100000   16ms   10ms   2.5M    134ms   60ms   2.7M    155ms   93ms  1.3M
		Clusters(50,50000,5)    10   100000   16ms   12ms   2.5M    134ms   60ms   2.7M    180ms   90ms  2.0M
		Clusters(75,90000,5)    10   100000   19ms    9ms   2.5M    129ms   60ms   2.7M    169ms   82ms  2.1M
		Clusters(75,90000,5)     5   200000   40ms   40ms   5.1M    318ms  165ms   5.3M    368ms  205ms  4.3M
		Clusters(75,90000,5)     3   500000  130ms  130ms  12.7M   1031ms  530ms  13.4M   1202ms  577ms 10.6M
		Clusters(75,90000,5)     2  1000000  320ms  296ms  25.4M   2624ms 1241ms  26.7M   2562ms 1234ms 21.3M
		Clusters(75,90000,5)     1  2000000  687ms  593ms  50.9M   6671ms 2796ms  53.4M   5265ms 2656ms 42.7M
		Clusters(99,90000,2)     1  2000000  703ms  578ms  50.9M   6640ms 2796ms  53.4M   5328ms 3062ms 28.1M
		Tests with 64-bit keys:
		Clusters(25,50000,9)    10   100000   23ms   15ms   3.1M    151ms   71ms   3.1M    177ms   97ms  1.4M
		Clusters(50,20000,5)    10   100000   23ms   15ms   3.1M    148ms   68ms   3.1M    179ms   91ms  2.0M
		Clusters(75,1000,3)     10   100000   19ms   13ms   3.1M    144ms   71ms   3.1M    156ms   70ms  1.8M
		Random 32-bit longs     10   100000   26ms   15ms   3.1M    149ms   71ms   3.1M    179ms   96ms  2.0M
		Random 40-bit longs     10   100000   24ms   15ms   3.1M    174ms   78ms   3.1M    190ms   93ms  2.5M
		Random 64-bit longs     10   100000   26ms   15ms   3.1M    148ms   68ms   3.1M    201ms   94ms  3.1M
		Random set (null vals.) 10   100000   27ms   16ms   3.1M    144ms   71ms   3.1M    187ms   90ms  2.3M
		Exponential longs       10   100000   26ms   15ms   3.1M    154ms   74ms   3.1M    210ms  112ms  2.3M
		Exponential longs        5   200000   55ms   46ms   6.1M    365ms  187ms   6.1M    534ms  246ms  4.7M
		Exponential longs        3   500000  156ms  135ms  15.3M   1296ms  567ms  15.3M   1406ms  697ms 11.4M
		Exponential longs        2  1000000  343ms  296ms  30.5M   3218ms 1452ms  30.5M   3343ms 1531ms 22.3M
		 */

		public static void BenchmarkInts()
		{
			Console.WriteLine("                                      |-Int Dictionary--|   |-SortedDictionary-|   |----CPIntTrie----|");
			Console.WriteLine("Scenario              Reps Set size   Fill   Scan  Memory   Fill   Scan   Memory   Fill   Scan  Memory");
			Console.WriteLine("--------              ---- --------   ----   ----  ------   ----   ----   ------   ----   ----  ------");

			int[] ints = GetLinearInts(100000, 1);
			DoIntBenchmarkLine(4, "1-100,000, sorted", 10, ints, "not null");
			Randomize(ints); // already scrambled, but just to be clear
			DoIntBenchmarkLine(4, "1-100,000, random", 10, ints, "not null");
			DoIntBenchmarkLine(4, "1-100,000 w/ null vals", 10, ints, null);

			Console.WriteLine("24-bit keys with 100K items:");
			DoIntBenchmarkLine(4, "Random 24-bit ints", 10, GetRandomInts(100000, 0, 0xFFFFFF), "not null");
			DoIntBenchmarkLine(4, "Random set (null vals.)", 10, GetRandomInts(100000, 0, 0xFFFFFF), null);
			DoIntBenchmarkLine(4, "Clusters(20, 100,2)", 10, GetIntClusters(100000, 20, 100, 2, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(same w/ nulls)", 10, GetIntClusters(100000, 20, 100, 2, 0), null);
			DoIntBenchmarkLine(4, "Clusters(20, 100,9)", 10, GetIntClusters(100000, 20,  100, 9, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(20,1000,2)", 10, GetIntClusters(100000, 20, 1000, 2, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(20,1000,9)", 10, GetIntClusters(100000, 20, 1000, 9, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(50, 100,2)", 10, GetIntClusters(100000, 50,  100, 2, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(50, 100,9)", 10, GetIntClusters(100000, 50,  100, 9, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(50,1000,2)", 10, GetIntClusters(100000, 50, 1000, 2, 0), "not null");
			DoIntBenchmarkLine(4, "Clusters(50,1000,9)", 10, GetIntClusters(100000, 50, 1000, 9, 0), "not null");

			Console.WriteLine("Tests with 32-bit keys:");
			DoIntBenchmarkLine(4, "Random 32-bit ints", 10, GetRandomInts(100000, int.MinValue, int.MaxValue), "not null");
			DoIntBenchmarkLine(4, "Random 32-bit ints", 5, GetRandomInts(200000, int.MinValue, int.MaxValue), "not null");
			DoIntBenchmarkLine(4, "Random 32-bit ints", 3, GetRandomInts(500000, int.MinValue, int.MaxValue), "not null");
			DoIntBenchmarkLine(4, "Random 32-bit ints", 2, GetRandomInts(1000000, int.MinValue, int.MaxValue), "not null");
			DoIntBenchmarkLine(4, "Exponential 32-bit", 10, GetExponentialInts(100000), "not null");
			DoIntBenchmarkLine(4, "Exponential 32-bit", 5, GetExponentialInts(200000), "not null");
			DoIntBenchmarkLine(4, "Exponential 32-bit", 3, GetExponentialInts(500000), "not null");
			DoIntBenchmarkLine(4, "Exponential 32-bit", 2, GetExponentialInts(1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(25,25,1)", 10, GetIntClusters(100000, 25, 25, 1, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(25,30000,5)", 10, GetIntClusters(100000, 25, 30000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(50,50000,5)", 10, GetIntClusters(100000, 50, 50000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(75,90000,5)", 10, GetIntClusters(100000, 75, 90000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(75,90000,5)", 5, GetIntClusters(200000, 75, 90000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(75,90000,5)", 3, GetIntClusters(500000, 75, 90000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(75,90000,5)", 2, GetIntClusters(1000000, 75, 90000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(75,90000,5)", 1, GetIntClusters(2000000, 75, 90000, 5, 0x1000000), "not null");
			DoIntBenchmarkLine(4, "Clusters(99,90000,2)", 1, GetIntClusters(2000000, 9, 90000, 2, 0x1000000), "not null");
			
			long[] longs;
			ints = GetRandomInts(int.MinValue, int.MaxValue, 100000);
			longs = new long[ints.Length];

			Console.WriteLine("Tests with 64-bit keys:");
			DoIntBenchmarkLine(8, "Clusters(25,50000,9)", 10, GetLongClusters(100000, 25, 50000,  9, 0x0123456789ABCDEF), "not null");
			DoIntBenchmarkLine(8, "Clusters(50,20000,5)", 10, GetLongClusters(100000, 50, 20000, 5, 0x0123456789ABCDEF), "not null");
			DoIntBenchmarkLine(8, "Clusters(75,1000,3)", 10, GetLongClusters(100000, 75, 1000, 3, 0x0123456789ABCDEF), "not null");
			DoIntBenchmarkLine(8, "Random 32-bit longs", 10, GetRandomLongs(100000, 0), "not null");
			DoIntBenchmarkLine(8, "Random 40-bit longs", 10, GetRandomLongs(100000, 8), "not null");
			DoIntBenchmarkLine(8, "Random 64-bit longs", 10, GetRandomLongs(100000, 32), "not null");
			DoIntBenchmarkLine(8, "Random set (null vals.)", 10, GetRandomLongs(100000, 32), null);
			DoIntBenchmarkLine(8, "Exponential longs", 10, GetExponentialLongs(100000), "not null");
			DoIntBenchmarkLine(8, "Exponential longs", 5,  GetExponentialLongs(200000), "not null");
			DoIntBenchmarkLine(8, "Exponential longs", 3,  GetExponentialLongs(500000), "not null");
			DoIntBenchmarkLine(8, "Exponential longs", 2,  GetExponentialLongs(1000000), "not null");

			Console.WriteLine();
		}

		/// <summary>Builds a set of clustered keys for benchmarking CPIntTrie.
		/// "clustered" means that keys come in groups called clusters. Keys in a
		/// cluster are near one other.</summary>
		/// <param name="clusterMax">Number of keys per cluster will be in the range clusterMax/2..clusterMax.</param>
		/// <param name="spacerMax">Each cluster is separated by a spacer of size 1..spacerMax.</param>
		/// <param name="clusterSpread">Difference between consecutive keys within the cluster is 1..clusterSpread.</param>
		/// <param name="numKeys">Number of keys total</param>
		/// <returns></returns>
		static int[] GetIntClusters(int numKeys, int clusterMax, int spacerMax, int clusterSpread, int minKey)
		{
			int[] keys = new int[numKeys];
			int clusterSize;
			int key = minKey - 1;
			for (int i = 0; i < keys.Length; i += clusterSize)
			{
				clusterSize = _random.Next(clusterMax/2, clusterMax+1);
				for (int j = 0; j < clusterSize && i + j < keys.Length; j++)
				{
					key += _random.Next(clusterSpread) + 1;
					keys[i + j] = key;
				}
				key += _random.Next(spacerMax) + 1;
			}
			
			Randomize(keys);
			return keys;
		}
		/// <summary>Same as GetIntClusters, but gets longs instead.</summary>
		static long[] GetLongClusters(int numKeys, int clusterMax, long spacerMax, int clusterSpread, long minKey)
		{
			long[] keys = new long[numKeys];
			int clusterSize;
			long key = minKey;
			for (int i = 0; i < keys.Length; i += clusterSize)
			{
				clusterSize = _random.Next(clusterMax / 2, clusterMax + 1);
				for (int j = 0; j < clusterSize && i + j < keys.Length; j++)
				{
					key += _random.Next(clusterSpread) + 1;
					keys[i + j] = key;
				}
				key += (long)((0.5 + _random.NextDouble() * 0.5) * (spacerMax + 1));
			}
			
			Randomize(keys);
			return keys;
		}

		static void Randomize<T>(T[] keys)
		{
			for (int i = 0; i < keys.Length; i++)
				G.Swap(ref keys[i], ref keys[_random.Next(keys.Length)]);
		}
		static int[] GetRandomInts(int numKeys, int min, int max)
		{
			Dictionary<int,bool> keys = new Dictionary<int, bool>();
			while (keys.Count < numKeys)
				keys[_random.Next(min, max)] = true;
			return ToArray(keys);
		}
		static long[] GetRandomLongs(int numKeys, int shift)
		{
			Dictionary<long,bool> keys = new Dictionary<long, bool>();
			while (keys.Count < numKeys)
			{
				// The low bits shouldn't matter, so let them be zero
				long k = (long)_random.Next() << shift;
				if (_random.Next(0, 2) == 0)
					k = -k;
				keys[k] = true;
			}
			return ToArray(keys);
		}
		static INT[] ToArray<INT>(Dictionary<INT, bool> keys)
		{
			INT[] keys2 = new INT[keys.Count];
			keys.Keys.CopyTo(keys2, 0);
			return keys2;
		}
		static int[] GetLinearInts(int numKeys, int min)
		{
			int[] keys = new int[numKeys];
			for (int i = 0; i < keys.Length; i++)
				keys[i] = min + i;
			return keys;
		}
		static int[] GetExponentialInts(int numKeys)
		{
			Dictionary<int,bool> keys = new Dictionary<int,bool>();
			while (keys.Count < numKeys)
			{
				int key = _random.Next() >> _random.Next(17);
				if (_random.Next(0, 2) == 0)
					key = ~key;
				keys[key] = true;
			}
			return ToArray(keys);
		}
		static long[] GetExponentialLongs(int numKeys)
		{
			Dictionary<long,bool> keys = new Dictionary<long, bool>();
			while (keys.Count < numKeys)
			{
				long key = (long)_random.Next() << _random.Next(33);
				if (_random.Next(0, 2) == 0)
					key = ~key;
				keys[key] = true;
			}
			return ToArray(keys);
		}

		public static void DoIntBenchmarkLine<T>(int bytesPerKey, string name, int reps, T[] keys, object value)
		{
			int dictFillTime = 0, sdicFillTime = 0, trieFillTime = 0;
			int dictScanTime = 0, sdicScanTime = 0, trieScanTime = 0;
			long dictMemory = 0, sdicMemory = 0, trieMemory = 0;

			for (int rep = 0; rep < reps; rep++)
			{
				IDictionary<T, object> dict, sdic, trie;

				GC.Collect();
				dictFillTime += Fill(keys, dict = new Dictionary<T, object>(), value);
				sdicFillTime += Fill(keys, sdic = new SortedDictionary<T, object>(), value);
				trieFillTime += Fill(keys, trie = (IDictionary<T,object>) new CPIntTrie<object>(), value);

				dictMemory += CountMemoryUsage((Dictionary<T, object>)dict, bytesPerKey, 4);
				sdicMemory += CountMemoryUsage((SortedDictionary<T, object>)sdic, bytesPerKey, 4);
				trieMemory += ((CPIntTrie<object>)trie).CountMemoryUsage(4);

				Randomize(keys);

				GC.Collect();

				dictScanTime += Scan(keys, dict);
				sdicScanTime += Scan(keys, sdic);
				trieScanTime += Scan(keys, trie);
			}

			if (name != null)
			{
				Debug.Assert(reps > 0);
				double dictMB = (double)dictMemory / (1024 * 1024) / reps;
				double sdicMB = (double)sdicMemory / (1024 * 1024) / reps;
				double trieMB = (double)trieMemory / (1024 * 1024) / reps;
				string info0 = string.Format("{0,-24}{1,2} {2,8} ", name, reps, keys.Length);
				string info1 = string.Format("{0,4}ms {1,4}ms {2,5:#0.0}M  ",
					dictFillTime / reps, dictScanTime / reps, dictMB);
				string info2 = string.Format("{0,5}ms{1,5}ms {2,5:#0.0}M  ",
					sdicFillTime / reps, sdicScanTime / reps, sdicMB);
				string info3 = string.Format("{0,5}ms{1,5}ms {2,4:#0.0}M",
					trieFillTime / reps, trieScanTime / reps, trieMB);

				Console.WriteLine(info0 + info1 + info2 + info3);
			}
		}

		public static int Fill<T>(T[] keys, IDictionary<T, object> dict, object value)
		{
			SimpleTimer t = new SimpleTimer();
			for (int i = 0; i < keys.Length; i++)
				dict[keys[i]] = value;
			return t.Millisec;
		}
		private static int Scan<T>(T[] keys, IDictionary<T, object> dict)
		{
			SimpleTimer t = new SimpleTimer();

			int irrelevant = 0;
			for (int i = 0; i < keys.Length; i++)
				if (dict[keys[i]] != null)
					irrelevant++;

			return t.Millisec;
		}


		/* Results on my work machine:
		                                 |--String Dictionary---|  |----SortedDictionary----|  |--CPStringTrie---|
		Scenario          Reps Sec.size  Fill   Scan  Memory+Keys  Fill    Scan   Memory+Keys  Fill   Scan  Memory
		--------          ---- --------  ----   ----  ------ ----  ----    ----   -----------  ----   ----  ------
		Basic word list     10    41238   10ms    9ms  1.0M+ 1.3M   107ms   52ms  1.1M+ 1.3M     91ms    47ms  0.8M
		Basic words opt.    10    41238   --ms   --ms  -- M+ -- M   -- ms  -- ms  -- M+ -- M     93ms    46ms  0.7M
		200K pairs           2   200000   77ms   85ms  5.1M+ 9.7M   773ms  421ms  5.3M+ 9.7M    765ms   429ms  6.3M
		200K pairs opt.      2   200000   --ms   --ms  -- M+ -- M   -- ms  -- ms  -- M+ -- M    780ms   359ms  5.0M
		200K pairs           2     1000  109ms   85ms  5.1M+ 9.7M   531ms  312ms  5.4M+ 9.7M    640ms   280ms  7.7M
		200K pairs           2      500  109ms   85ms  5.1M+ 9.7M   523ms  328ms  5.4M+ 9.7M    616ms   273ms  7.6M
		200K pairs           2      250  117ms   93ms  5.1M+ 9.7M   484ms  288ms  5.4M+ 9.7M    624ms   288ms  8.1M
		200K pairs           2      125  132ms  101ms  5.2M+ 9.7M   405ms  250ms  5.4M+ 9.7M    710ms   265ms  9.1M
		200K pairs opt.      2      125   --ms   --ms  -- M+ -- M   -- ms  -- ms  -- M+ -- M    655ms   234ms  7.1M
		200K pairs           2       64  117ms  125ms  5.3M+ 9.7M   335ms  218ms  5.5M+ 9.7M    648ms   234ms  9.2M
		200K pairs           2       32  117ms  109ms  5.5M+ 9.7M   265ms  171ms  5.7M+ 9.7M    390ms   187ms  6.6M
		200K pairs           2       16   85ms   70ms  6.0M+ 9.7M   187ms  125ms  6.1M+ 9.7M    351ms   148ms  7.3M
		200K pairs           2        8  171ms   70ms  6.9M+ 9.7M   132ms   78ms  6.9M+ 9.7M    398ms   140ms  9.3M
		200K pairs           2        4  101ms   54ms  8.7M+ 9.7M   101ms   62ms  8.4M+ 9.7M    406ms   109ms 10.7M
		200K pairs opt.      2        4   --ms   --ms  -- M+ -- M   -- ms  -- ms  -- M+ -- M    335ms   117ms  8.8M
		1M pairs             1  1000000  453ms  500ms 25.4M+48.4M  5593ms 3031ms 26.7M+48.4M   4234ms  2562ms 24.7M
		1M pairs opt.        1  1000000   --ms   --ms  -- M+ -- M   -- ms  -- ms  -- M+ -- M   4921ms  2078ms 19.5M
		1M pairs, 31 prefs.  1  1000000  421ms  484ms 25.4M+39.9M  5140ms 2968ms 26.7M+39.9M   4046ms  2265ms 20.7M
		1M pairs, 31, opt.   1  1000000   --ms   --ms  -- M+ -- M   -- ms  -- ms  -- M+ -- M   4108ms  1796ms 16.5M
		1M pairs, 31 prefs.  1      500 1062ms  640ms 25.6M+39.9M  3453ms 1953ms 26.8M+39.9M   3171ms  1453ms 24.8M
		1M pairs, 31 prefs.  1      250 1093ms  656ms 25.7M+39.9M  2765ms 1671ms 26.9M+39.9M   3578ms  1390ms 29.0M
		1M pairs, 31 prefs.  1      125 1046ms  640ms 26.0M+39.9M  2453ms 1515ms 27.2M+39.9M   4031ms  1296ms 36.7M
		1M pairs, 31 prefs.  1       64  968ms  609ms 26.6M+39.9M  2375ms 1140ms 27.7M+39.9M   3687ms  1093ms 41.8M
		1M pairs, 31 prefs.  1       32  859ms  531ms 27.7M+39.9M  1609ms  890ms 28.6M+39.9M   2015ms   906ms 31.0M
		1M pairs, 31 prefs.  1       16  718ms  359ms 29.9M+39.9M  1343ms  656ms 30.5M+39.9M   1953ms   640ms 30.3M
		1M pairs, 31 prefs.  1        8 1046ms  328ms 34.5M+39.9M  1015ms  453ms 34.3M+39.9M   2140ms   562ms 38.6M
		1M pairs, 31 prefs.  1        4 1015ms  265ms 43.4M+39.9M  1187ms  312ms 42.0M+39.9M   1390ms   515ms 44.3M	
		 */
		public static void BenchmarkStrings(string[] words)
		{
			Console.WriteLine("                                 |--String Dictionary---|  |----SortedDictionary----|  |--CPStringTrie---|");
			Console.WriteLine("Scenario          Reps Sec.size  Fill   Scan  Memory+Keys  Fill    Scan   Memory+Keys  Fill   Scan  Memory");
			Console.WriteLine("--------          ---- --------  ----   ----  ------ ----  ----    ----   -----------  ----   ----  ------");
			
			// - Basic word list, 10 trials; discard first trial
			StringBenchmarkLine(null,              words, words.Length, 1, false);
			StringBenchmarkLine("Basic word list", words, words.Length, 10, false);
			StringBenchmarkLine("Basic words opt.", words, words.Length, 10, true);

			// - 1,000,000 random word pairs, section sizes of 4, 8, 16, 32, 64,
			//   125, 250, 500, 1000, 2000, 4000, 8000, 16000, 32000, 64000,
			//   125000, 250000, 500000, 1000000.
			string[] pairs1 = BuildPairs(words, words, " ", 1000000);

			StringBenchmarkLine("200K pairs",      pairs1,  200000, 2, false, 200000);
			StringBenchmarkLine("200K pairs opt.", pairs1,  200000, 2, true,  200000);
			StringBenchmarkLine("200K pairs",      pairs1,    1000, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,     500, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,     250, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,     125, 2, false, 200000);
			StringBenchmarkLine("200K pairs opt.", pairs1,     125, 2, true,  200000);
			StringBenchmarkLine("200K pairs",      pairs1,      64, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,      32, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,      16, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,       8, 2, false, 200000);
			StringBenchmarkLine("200K pairs",      pairs1,       4, 2, false, 200000);
			StringBenchmarkLine("200K pairs opt.", pairs1,       4, 2, true,  200000);
			StringBenchmarkLine("1M pairs",        pairs1, 1000000, 1, false);
			StringBenchmarkLine("1M pairs opt.",   pairs1, 1000000, 1, true);

			// - 1,000,000 word pairs with limited prefixes
			string[] prefixes = new string[] {
				"a", "at", "the", "them", "some", "my", "your", "do", "good", "bad", "ugly", "***",
				"canned", "erroneous", "fracking", "hot", "inner", "John", "kill", "loud", "muddy",
				"no", "oh", "pro", "quality", "red", "unseen", "valuable", "wet", "x", "ziffy"
			};
			string name1  = string.Format("1M pairs, {0} prefs.", prefixes.Length);
			string name2 = string.Format("1M pairs, {0}, opt.", prefixes.Length);
			string[] pairs2 = BuildPairs(prefixes, words, " ", 1000000);
			StringBenchmarkLine(name1, pairs2, 1000000, 1, false);
			StringBenchmarkLine(name2, pairs2, 1000000, 1, true);
			StringBenchmarkLine(name1, pairs2, 500, 1, false);
			StringBenchmarkLine(name1, pairs2, 250, 1, false);
			StringBenchmarkLine(name1, pairs2, 125, 1, false);
			StringBenchmarkLine(name1, pairs2, 64, 1, false);
			StringBenchmarkLine(name1, pairs2, 32, 1, false);
			StringBenchmarkLine(name1, pairs2, 16, 1, false);
			StringBenchmarkLine(name1, pairs2, 8, 1, false);
			StringBenchmarkLine(name1, pairs2, 4, 1, false);

			Console.WriteLine();
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

		public static void StringBenchmarkLine(string name, string[] words, int sectionSize, int reps, bool optimizeTrie)
		{
			StringBenchmarkLine(name, words, sectionSize, reps, optimizeTrie, words.Length);
		}
		public static void StringBenchmarkLine(string name, string[] words, int sectionSize, int reps, bool optimizeTrie, int wordCount)
		{
			int dictFillTime = 0, sdicFillTime = 0, trieFillTime = 0;
			int dictScanTime = 0, sdicScanTime = 0, trieScanTime = 0;
			long dictMemory = 0, sdicMemory = 0, trieMemory = 0;
			for (int rep = 0; rep < reps; rep++) {
				IDictionary<string, string>[] dicts = null, sdics = null, tries;

				GC.Collect();
				if (!optimizeTrie)
				{	
					// Each line where we optimize the trie is paired with another 
					// line where we don't; there is no need to repeat the non-trie
					// benchmarks.
					dictFillTime += Fill(words, wordCount, sectionSize, out dicts, 
						delegate() { return new Dictionary<string,string>(); });
					sdicFillTime += Fill(words, wordCount, sectionSize, out sdics,
						delegate() { return new SortedDictionary<string, string>(StringComparer.Ordinal); });
				}
				trieFillTime += Fill(words, wordCount, sectionSize, out tries, 
					delegate() { return new CPStringTrie<string>(); });

				if (optimizeTrie)
				{
					SimpleTimer t = new SimpleTimer();

					for (int i = 0; i < tries.Length; i++)
						tries[i] = ((CPStringTrie<string>)tries[i]).Clone();

					trieFillTime += t.Millisec;
				}

				if (!optimizeTrie)
				{
					for (int i = 0; i < dicts.Length; i++)
						dictMemory += CountMemoryUsage((Dictionary<string, string>)dicts[i], 4, 4);
					for (int i = 0; i < sdics.Length; i++)
						sdicMemory += CountMemoryUsage((SortedDictionary<string, string>)sdics[i], 4, 4);
				}
				for (int i = 0; i < tries.Length; i++)
					trieMemory += ((CPStringTrie<string>)tries[i]).CountMemoryUsage(4);

				Scramble(words, wordCount, sectionSize);
				
				GC.Collect();

				if (!optimizeTrie)
				{
					dictScanTime += Scan(words, wordCount, sectionSize, dicts);
					sdicScanTime += Scan(words, wordCount, sectionSize, sdics);
				}
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
				Debug.Assert(reps > 0); 
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
				if (optimizeTrie)
				{
					info1 = "  --ms   --ms  -- M+ -- M ";
					info2 = "  -- ms  -- ms  -- M+ -- M  ";
				}
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
			Debug.Assert(sectionSize > 0);
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
