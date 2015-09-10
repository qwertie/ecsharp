using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Binary.Tests
{
    /// <summary>
    /// Unit tests that test binary loyc tree LES round-trips.
    /// </summary>
    [TestFixture]
    public class RoundTripTests
    {
        #region Helpers

        private IReadOnlyList<LNode> PerformRoundTrip(IReadOnlyList<LNode> Nodes)
        {
            using (var memStream = new MemoryStream())
            {
                var writer = new LoycBinaryWriter(memStream);
                writer.WriteFile(Nodes);
                memStream.Seek(0, SeekOrigin.Begin);
                var reader = new LoycBinaryReader(memStream);
                return reader.ReadFile("test.blt");
            }
        }

        private void TestRoundTrip(IReadOnlyList<LNode> Nodes)
        {
            var newNodes = PerformRoundTrip(Nodes);
            Assert.IsTrue(AreEquivalent(Nodes, newNodes));
        }

        private bool AreEquivalent(IEnumerable<LNode> First, IEnumerable<LNode> Second)
        {
            if (First.Count() != Second.Count())
            {
                return false;
            }
            else
            {
                return First.Zip(Second, Pair.Create).All(item => AreEquivalent(item.Item1, item.Item2));
            }
        }

        private bool AreEquivalent(LNode First, LNode Second)
        {
            if (First.Kind != Second.Kind || !AreEquivalent(First.Attrs, Second.Attrs))
            {
                return false;
            }
            if (First.IsCall)
            {
                return AreEquivalent(First.Target, Second.Target) && AreEquivalent(First.Args, Second.Args);
            }
            else if (First.IsId)
            {
                return First.Name.Name == Second.Name.Name;
            }
            else
            {
                return object.Equals(First.Value, Second.Value);
            }
        }

        #endregion

        #region Random tree generation

        private Symbol GenerateRandomSymbol(Random Rand)
        {
            return GSymbol.Pool.ElementAt(Rand.Next(GSymbol.Pool.Count()));
        }

        private static Lazy<char[]> CharacterList = new Lazy<char[]>(GetCharacterList);
        private static char[] GetCharacterList()
        {
            var results = new HashSet<char>();
            foreach (var item in GSymbol.Pool)
            {
                results.UnionWith(item.Name);
            }
            return results.ToArray();
        }

        private char GenerateRandomCharacter(Random Rand)
        {
            return CharacterList.Value[Rand.Next(CharacterList.Value.Length)];
        }

        private object GenerateRandomLiteral(Random Rand)
        {
            switch ((NodeEncodingType)Rand.Next(2, 18))
            {
                case NodeEncodingType.String:
                    int length = Rand.Next(30);
                    var results = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        results[i] = GenerateRandomCharacter(Rand);
                    }
                    return new string(results);
                case NodeEncodingType.Int8:
                    return (sbyte)Rand.Next(sbyte.MinValue, sbyte.MaxValue);
                case NodeEncodingType.Int16:
                    return (short)Rand.Next(short.MinValue, short.MaxValue);
                case NodeEncodingType.Int32:
                    return Rand.Next();
                case NodeEncodingType.Int64:
                    return (long)((ulong)(Rand.Next() << 32) | (ulong)Rand.Next());
                case NodeEncodingType.UInt8:
                    return (byte)Rand.Next(byte.MaxValue);
                case NodeEncodingType.UInt16:
                    return (ushort)Rand.Next(ushort.MaxValue);
                case NodeEncodingType.UInt32:
                    return (uint)Rand.Next();
                case NodeEncodingType.UInt64:
                    return (ulong)(Rand.Next() << 32) | (ulong)Rand.Next();
                case NodeEncodingType.Float32:
                    return (float)Rand.NextDouble();
                case NodeEncodingType.Float64:
                    return Rand.NextDouble();
                case NodeEncodingType.Char:
                    return GenerateRandomCharacter(Rand);
                case NodeEncodingType.Boolean:
                    return Rand.Next(1) == 1;
                case NodeEncodingType.Void:
                    return @void.Value;
                case NodeEncodingType.Decimal:
                    return (decimal)Rand.NextDouble();
                case NodeEncodingType.Null:
                default:
                    return null;
            }
        }

        private LNode GenerateRandomNode(LNodeFactory Factory, Random Rand, int Depth)
        {
            int index = Rand.Next(5);
            switch (Depth <= 0 && index > 1 ? Rand.Next(2) : index)
            {
                case 0:
                    return Factory.Literal(GenerateRandomLiteral(Rand));
                case 1:
                    return Factory.Id(GenerateRandomSymbol(Rand));
                case 2:
                    return Factory.Attr(GenerateRandomNode(Factory, Rand, Depth - 1), GenerateRandomNode(Factory, Rand, Depth - 1));
                case 3:
                    return Factory.Call(GenerateRandomSymbol(Rand), GenerateRandomNodeList(Factory, Rand, Depth - 1));
                default:
                    return Factory.Call(GenerateRandomNode(Factory, Rand, Depth - 1), GenerateRandomNodeList(Factory, Rand, Depth - 1));
            }
        }

        private IReadOnlyList<LNode> GenerateRandomNodeList(LNodeFactory Factory, Random Rand, int Depth)
        {
            var results = new LNode[(int)System.Math.Sqrt(Rand.Next(100))];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GenerateRandomNode(Factory, Rand, Depth);
            }
            return results;
        }

        #endregion

        [Test] 
        public void TestSimple()
        {
            var nodes = LesLanguageService.Value.Parse("#if(x + 3 > y, x, y)");
            TestRoundTrip(nodes);
        }

        [Test]
        public void TestFuzz()
        {
            for (int i = 0; i < 200; i++)
            {
                TestRoundTrip(GenerateRandomNodeList(new LNodeFactory(new EmptySourceFile("test.les")), new Random(), 5));
            }
        }
    }
}
