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

			CPStringTrie<int> t = new CPStringTrie<int>();
			t["This little piggy went to market"] = 1;
			t["This little piggy stayed at home"] = 2;
			t["This little piggy had roast beef"] = 3;
			t["This little piggy had none"] = 4;
			t["And this little piggy went"] = 5;
			t["'Wee wee wee' all the way home."] = 6;
			t["And then some wolf came along"] = 7;
			t["and blew down two of their houses!"] = 8;


			// Obtain the 12dicts English word list 2of12.txt
			// See http://wordlist.sourceforge.net/
			string wordList = Resources.WordList;
			string[] words = wordList.Split(new string[] { "\n", "\r\n" },
											StringSplitOptions.RemoveEmptyEntries);
			CPTrieBenchmark.Benchmark(words);

		}
	}
}
