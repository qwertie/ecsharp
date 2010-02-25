using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using CPTrieTests;

namespace Loyc.Utilities
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Running tests...");
			RunTests.Run(new CPTrieTests());

			Console.WriteLine();
			Console.WriteLine("Running benchmarks (note: Debug builds of CPTrie are slow)");
			// Obtain the word list
			string wordList = Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" },
											StringSplitOptions.RemoveEmptyEntries);
			CPTrieBenchmark.Benchmark(words);
		}
	}
}
