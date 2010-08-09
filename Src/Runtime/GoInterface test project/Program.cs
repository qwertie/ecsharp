using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace Loyc.Runtime
{
	class Program
	{
		static void Main(string[] args)
		{
			// Note 1: benchmark should run first in order to measure the time it
			// takes to use GoInterface for the first time, which is the slowest.
			// Note 2: Release builds run a bit faster
			Console.WriteLine("Running GoInterface benchmark");
			Console.WriteLine();
			GoInterfaceBenchmark.DoBenchmark();

			Console.WriteLine();
			Console.WriteLine("Running GoInterface test suite");
			Console.WriteLine();
			RunTests.Run(new GoInterfaceTests());

			Console.WriteLine();
			Console.WriteLine("Press any key.");
			Console.ReadKey(true);
		}
	}
}
