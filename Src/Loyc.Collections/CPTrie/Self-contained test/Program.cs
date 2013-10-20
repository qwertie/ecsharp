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
			Console.WriteLine("Running benchmarks...");
			Console.WriteLine("Note: Set window width to 110 characters. Debug builds of CPTrie are slower");
			Console.WriteLine("      due to CPLinear<T>.CheckValidity() and the lack of optimizations. ");

			// Obtain the 12dicts English word list 2of12.txt
			// See http://wordlist.sourceforge.net/
			string wordList = Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" },
											StringSplitOptions.RemoveEmptyEntries);
			CPTrieBenchmark.BenchmarkStrings(words);
			CPTrieBenchmark.BenchmarkInts();
		}
	}
}
