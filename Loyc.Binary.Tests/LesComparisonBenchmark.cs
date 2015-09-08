using Ecs.Parser;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary.Tests
{
    public struct RoundTripPerformance
    {
        public RoundTripPerformance(TimeSpan readPerformance, TimeSpan writePerformance, long size)
        {
            this = default(RoundTripPerformance);
            ReadPerformance = readPerformance;
            WritePerformance = writePerformance;
            Size = size;
        }

        public TimeSpan ReadPerformance { get; private set; }
        public TimeSpan WritePerformance { get; private set; }
        public long Size { get; private set; }

        public static RoundTripPerformance operator +(RoundTripPerformance lhs, RoundTripPerformance rhs)
        {
            return new RoundTripPerformance(lhs.ReadPerformance + rhs.ReadPerformance, lhs.WritePerformance + rhs.WritePerformance, lhs.Size + rhs.Size);
        }
    }

    /// <summary>
    /// Unit tests that benchmark LES vs BLT (binary loyc tree) read/write performance.
    /// Disk size is examined, too.
    /// </summary>
    [TestFixture]
    public class LesComparisonBenchmark
    {
        public string[] GrabEcsFiles()
        {
            return Directory.GetFiles("..\\..\\..\\Loyc.Binary\\", "*.cs");
        }

        /// <summary>
        /// Gets the number of round trips that are performed for every file.
        /// This number should be adequately high to reduce timer noise.
        /// </summary>
        private const int TimedRoundTripCount = 200;

        private RoundTripPerformance MakeBltRoundTrip(LNode[] Nodes)
        {
            using (var memStream = new MemoryStream())
            {
                var timer = new Stopwatch();
                timer.Start();

                var writer = new LoycBinaryWriter(memStream);

                for (int i = 0; i < TimedRoundTripCount; i++)
                {
                    writer.WriteFile(Nodes);
                    memStream.Seek(0, SeekOrigin.Begin);
                }

                timer.Stop();
                var writePerf = timer.Elapsed;
                long size = memStream.Length;
                timer.Restart();

                for (int i = 0; i < TimedRoundTripCount; i++)
                {
                    var reader = new LoycBinaryReader(memStream);
                    reader.ReadFile("test.blt");
                    memStream.Seek(0, SeekOrigin.Begin);
                }

                timer.Stop();
                var readPerf = timer.Elapsed;

                return new RoundTripPerformance(readPerf, writePerf, size);
            }
        }

        private RoundTripPerformance MakeLesRoundTrip(LNode[] Nodes)
        {
            var timer = new Stopwatch();
            timer.Start();

            string[] data = null;
            for (int i = 0; i < TimedRoundTripCount; i++)
            {
                data = Nodes.Select(LesLanguageService.Value.Print).ToArray();
            }

            var writePerf = timer.Elapsed;
            timer.Stop();

            int size = data.Aggregate(0, (acc, item) => acc + item.Length);

            timer.Restart();

            for (int i = 0; i < TimedRoundTripCount; i++)
            {
                data.Select(item => LesLanguageService.Value.Parse(item)).ToArray();
            }

            timer.Stop();
            var readPerf = timer.Elapsed;

            return new RoundTripPerformance(readPerf, writePerf, size);
        }

        private LNode[] ParseEcs(string File)
        {
            using (var stream = new FileStream(File, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                string contents = reader.ReadToEnd();
                var parsed = EcsLanguageService.Value.Parse(contents);
                return parsed.ToArray();
            }
        }

        private void PublishComparison(RoundTripPerformance LesResults, RoundTripPerformance BltResults)
        {
            Console.WriteLine(" * LES size: " + LesResults.Size + " characters");
            Console.WriteLine(" * BLT size: " + BltResults.Size + " bytes");
            Console.WriteLine(" * LES/BLT size: " + ((double)LesResults.Size / (double)BltResults.Size));
            Console.WriteLine(" * LES read time: " + LesResults.ReadPerformance.Milliseconds + "ms");
            Console.WriteLine(" * BLT read time: " + BltResults.ReadPerformance.Milliseconds + "ms");
            Console.WriteLine(" * LES/BLT read time: " + ((double)LesResults.ReadPerformance.Ticks / (double)BltResults.ReadPerformance.Ticks));
            Console.WriteLine(" * LES write time: " + LesResults.WritePerformance.Milliseconds + "ms");
            Console.WriteLine(" * BLT write time: " + BltResults.WritePerformance.Milliseconds + "ms");
            Console.WriteLine(" * LES/BLT write time: " + ((double)LesResults.WritePerformance.Ticks / (double)BltResults.WritePerformance.Ticks));
            Console.WriteLine();
        }

        public Tuple<LNode[], RoundTripPerformance, RoundTripPerformance> BenchmarkBltPerformance(string File, bool PublishResults)
        {
            var nodes = ParseEcs(File);

            if (nodes == null)
            {
                Console.WriteLine("EC# parser couldn't handle '" + File + "'.");
                return Tuple.Create(new LNode[0], new RoundTripPerformance(), new RoundTripPerformance());
            }

            return BenchmarkBltPerformance(nodes, "File '" + File + "':", PublishResults);
        }

        public Tuple<LNode[], RoundTripPerformance, RoundTripPerformance> BenchmarkBltPerformance(LNode[] Nodes, string Title, bool PublishResults)
        {
            // First, we'll do an LES round-trip, then we'll do a BLT round-trip.

            var lesPerf = MakeLesRoundTrip(Nodes);
            var bltPerf = MakeBltRoundTrip(Nodes);

            if (PublishResults)
            {
                Console.WriteLine(Title);
                PublishComparison(lesPerf, bltPerf);
            }

            return Tuple.Create(Nodes, lesPerf, bltPerf);
        }

        /// <summary>
        /// Benchmarks and compares LES/BLT performance.
        /// Loyc.Binary is parsed by the EC# parser, and then
        /// a LES and a BLT round-trip are performed.
        /// </summary>
        [Test]
        public void BenchmarkBltPerformance()
        {
            string[] allFiles = GrabEcsFiles();

            BenchmarkBltPerformance(allFiles[0], false);

            Console.WriteLine("Note: the following benchmarks are LES/BLT round-trips that have been performed 200 times.");
            Console.WriteLine();

            var aggregate = allFiles.Select(item => BenchmarkBltPerformance(item, true))
                                    .Aggregate(Tuple.Create(Enumerable.Empty<LNode>(), new RoundTripPerformance(), new RoundTripPerformance()),
                                               (aggr, item) => Tuple.Create(aggr.Item1.Concat(item.Item1), aggr.Item2 + item.Item2, aggr.Item3 + item.Item3));

            Console.WriteLine("Sum of individual read/writes:");
            PublishComparison(aggregate.Item2, aggregate.Item3);

            BenchmarkBltPerformance(aggregate.Item1.ToArray(), "Read/write of all nodes stitched together:", true);
        }
    }
}
