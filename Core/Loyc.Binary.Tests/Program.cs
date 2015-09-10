using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary.Tests
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // Copied from LoycCore.Tests' Program.cs.
            // =======================================
            // Workaround for MS bug: Assert(false) will not fire in debugger
            Debug.Listeners.Clear();
            Debug.Listeners.Add(new DefaultTraceListener());

            // ========================================

            // Round-trip fuzz tests
            RunTests.Run(new RoundTripTests());
            // LES comparison benchmark
            RunTests.Run(new LesComparisonBenchmark());

            Console.WriteLine("Job's done.");
        }
    }
}
