using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	/// <summary>
	///   Generative round-trip fuzzing, shared by all (Reader, Writer) fixture pairs.
	///   Each iteration builds a random object tree from a seed, writes it, reads it
	///   back using the tree itself as the schema, and checks (1) structural equality
	///   and (2) write stability (re-serializing the read-back tree yields identical
	///   bytes). The failing seed is included in the failure message so any case can
	///   be replayed. Set SYNCLIB_FUZZ_ITERS to change the iteration count.
	/// </summary>
	public abstract partial class SyncLibTests<Reader, Writer>
		where Writer : ISyncManager
		where Reader : ISyncManager
	{
		[Test]
		public void GenerativeRoundTripFuzz()
		{
			int iters = int.TryParse(Environment.GetEnvironmentVariable("SYNCLIB_FUZZ_ITERS"), out int n) ? n : 250;
			int baseSeed = Environment.TickCount;

			for (int i = 0; i < iters; i++) {
				int caseSeed = baseSeed + i;
				FuzzNode? tree = null;
				try {
					var rng = new Random(caseSeed);
					tree = FuzzNode.GenerateObject(rng, maxDepth: 4);

					// The template tree is the schema on both sides, so it is passed via
					// closure (on the read side the savable parameter arrives as null)
					FuzzNode template = tree;
					var data = Write<FuzzNode>(tree, (Writer sm, FuzzNode? _) => FuzzNode.Sync(sm, template), 0);
					var tree2 = Read<FuzzNode>(data, (Reader sm, FuzzNode? _) => FuzzNode.Sync(sm, template))!;
					FuzzNode.AssertEqual(tree, tree2, "root");

					// Write stability: serializing what we read must be byte-identical
					var data2 = Write<FuzzNode>(tree2, (Writer sm, FuzzNode? _) => FuzzNode.Sync(sm, tree2!), 0);
					ExpectList(data2, data);
				} catch (Exception e) when (!(e is Loyc.MiniTest.IgnoreException)) {
					Fail("Fuzz seed {0} failed{1}: {2}", caseSeed,
						tree == null ? " (while generating)" : "", e);
				}
			}
		}
	}

	enum FuzzKind
	{
		Bool, Sbyte, Byte, Short, Ushort, Int, Uint, Long, Ulong,
		Float, Double, Decimal, BigInt, Char,
		NInt, NLong, NDouble, NDecimal, NBool,
		String, DedupString,
		IntArray, ByteArray, CharArray, DoubleArray,
		Tuple2,
		Object,
	}

	/// <summary>A node of a randomly generated object tree. The same tree acts as
	///   the schema on both the write side and the read side.</summary>
	class FuzzNode
	{
		public FuzzKind Kind;
		public ObjectMode Mode = ObjectMode.Normal;  // for Object kind
		public object? Value;                        // boxed primitive / string / array
		public List<FuzzNode>? Children;             // for Object kind (null Children = null object)

		#region Generation

		public static FuzzNode GenerateObject(Random rng, int maxDepth)
		{
			var node = new FuzzNode {
				Kind = FuzzKind.Object,
				// The root must be NotNull-safe; Deduplicate on sub-objects is chosen below
				Mode = ObjectMode.Normal,
				Children = new List<FuzzNode>(),
			};
			int numChildren = rng.Next(1, 7);
			for (int i = 0; i < numChildren; i++)
				node.Children.Add(Generate(rng, maxDepth - 1, node.Children));
			return node;
		}

		static readonly FuzzKind[] LeafKinds = ((FuzzKind[])Enum.GetValues(typeof(FuzzKind)))
			.Where(k => k != FuzzKind.Object).ToArray();

		static FuzzNode Generate(Random rng, int depthLeft, List<FuzzNode> priorSiblings)
		{
			if (depthLeft > 0 && rng.Next(4) == 0) {
				if (rng.Next(8) == 0)
					return new FuzzNode { Kind = FuzzKind.Object, Mode = RandomObjectMode(rng), Children = null }; // null object
				if (rng.Next(6) == 0) {
					// Repeat an earlier deduplicated sibling to exercise back-references
					var dedupSibling = priorSiblings.FirstOrDefault(
						s => s.Kind == FuzzKind.Object && s.Children != null && (s.Mode & ObjectMode.Deduplicate) != 0);
					if (dedupSibling != null)
						return dedupSibling;
				}
				var obj = new FuzzNode { Kind = FuzzKind.Object, Mode = RandomObjectMode(rng), Children = new List<FuzzNode>() };
				int numChildren = rng.Next(0, 6);
				for (int i = 0; i < numChildren; i++)
					obj.Children!.Add(Generate(rng, depthLeft - 1, obj.Children));
				return obj;
			}
			var kind = LeafKinds[rng.Next(LeafKinds.Length)];
			return new FuzzNode { Kind = kind, Value = RandomValue(rng, kind) };
		}

		static ObjectMode RandomObjectMode(Random rng)
			=> rng.Next(3) == 0 ? ObjectMode.Deduplicate : ObjectMode.Normal;

		static long RandomInt64(Random rng, long min, long max) // inclusive bounds
		{
			// Bias toward boundary values, small numbers and format-size edges
			switch (rng.Next(8)) {
				case 0: return min;
				case 1: return max;
				case 2: return 0;
				case 3: return rng.Next(-1, 2);
				case 4: {
					// A value near a power of two, where encodings switch formats
					int bit = rng.Next(1, 63);
					long v = (1L << bit) + rng.Next(-2, 3);
					return v <= min ? min : v >= max ? max : rng.Next(2) == 0 ? v : SafeNegate(v, min);
				}
				default: {
					ulong range = (ulong)(max - min) + 1; // 0 means the full 2^64 range
					ulong v = ((ulong)(uint)rng.Next() << 32) | (uint)rng.Next();
					return unchecked(min + (long)(range == 0 ? v : v % range));
				}
			}

			static long SafeNegate(long v, long min) => -v <= min ? min : -v;
		}

		static object? RandomValue(Random rng, FuzzKind kind)
		{
			switch (kind) {
				case FuzzKind.Bool: return rng.Next(2) == 0;
				case FuzzKind.Sbyte: return (sbyte)RandomInt64(rng, sbyte.MinValue, sbyte.MaxValue);
				case FuzzKind.Byte: return (byte)RandomInt64(rng, byte.MinValue, byte.MaxValue);
				case FuzzKind.Short: return (short)RandomInt64(rng, short.MinValue, short.MaxValue);
				case FuzzKind.Ushort: return (ushort)RandomInt64(rng, ushort.MinValue, ushort.MaxValue);
				case FuzzKind.Int: return (int)RandomInt64(rng, int.MinValue, int.MaxValue);
				case FuzzKind.Uint: return (uint)RandomInt64(rng, 0, uint.MaxValue);
				case FuzzKind.Long: return RandomInt64(rng, long.MinValue + 1, long.MaxValue);
				case FuzzKind.Ulong: return unchecked((ulong)RandomInt64(rng, long.MinValue, long.MaxValue));
				case FuzzKind.Float: return RandomFloat(rng);
				case FuzzKind.Double: return RandomDouble(rng);
				case FuzzKind.Decimal: return RandomDecimal(rng);
				case FuzzKind.BigInt: return RandomBigInt(rng);
				case FuzzKind.Char: return RandomChar(rng);
				case FuzzKind.NInt: return rng.Next(4) == 0 ? null : (object)(int)RandomInt64(rng, int.MinValue, int.MaxValue);
				case FuzzKind.NLong: return rng.Next(4) == 0 ? null : (object)RandomInt64(rng, long.MinValue + 1, long.MaxValue);
				case FuzzKind.NDouble: return rng.Next(4) == 0 ? null : (object)RandomDouble(rng);
				case FuzzKind.NDecimal: return rng.Next(4) == 0 ? null : (object)RandomDecimal(rng);
				case FuzzKind.NBool: return rng.Next(4) == 0 ? null : (object)(rng.Next(2) == 0);
				case FuzzKind.String:
				case FuzzKind.DedupString: return rng.Next(8) == 0 ? null : RandomString(rng);
				case FuzzKind.IntArray:
					return rng.Next(8) == 0 ? null
						: Enumerable.Range(0, rng.Next(0, 20)).Select(_ => (int)RandomInt64(rng, int.MinValue, int.MaxValue)).ToArray();
				case FuzzKind.ByteArray:
					return rng.Next(8) == 0 ? null
						: Enumerable.Range(0, rng.Next(0, 40)).Select(_ => (byte)rng.Next(256)).ToArray();
				case FuzzKind.CharArray:
					return rng.Next(8) == 0 ? null
						: Enumerable.Range(0, rng.Next(0, 20)).Select(_ => RandomChar(rng)).ToArray();
				case FuzzKind.DoubleArray:
					return rng.Next(8) == 0 ? null
						: Enumerable.Range(0, rng.Next(0, 10)).Select(_ => RandomDouble(rng)).ToArray();
				case FuzzKind.Tuple2:
					return ((int)RandomInt64(rng, int.MinValue, int.MaxValue), RandomString(rng));
				default: throw new ArgumentException(kind.ToString());
			}
		}

		static float RandomFloat(Random rng)
			=> rng.Next(8) switch {
				0 => float.NaN, 1 => float.PositiveInfinity, 2 => float.NegativeInfinity,
				3 => float.MinValue, 4 => float.MaxValue, 5 => float.Epsilon,
				_ => (float)(rng.NextDouble() * 2e10 - 1e10),
			};

		static double RandomDouble(Random rng)
			=> rng.Next(8) switch {
				0 => double.NaN, 1 => double.PositiveInfinity, 2 => double.NegativeInfinity,
				3 => double.MinValue, 4 => double.MaxValue, 5 => double.Epsilon,
				_ => rng.NextDouble() * 2e100 - 1e100,
			};

		static decimal RandomDecimal(Random rng)
			=> rng.Next(6) switch {
				0 => decimal.MinValue, 1 => decimal.MaxValue, 2 => decimal.Zero,
				3 => new decimal(rng.Next(), rng.Next(), rng.Next(), rng.Next(2) == 0, (byte)rng.Next(29)),
				_ => (decimal)(rng.NextDouble() * 2e6 - 1e6),
			};

		static BigInteger RandomBigInt(Random rng)
		{
			var bytes = new byte[rng.Next(1, 40)];
			rng.NextBytes(bytes);
			return new BigInteger(bytes);
		}

		static char RandomChar(Random rng)
		{
			// Any char except the surrogate range (lone surrogates cannot survive UTF-8)
			char c;
			do {
				c = rng.Next(4) switch {
					0 => (char)rng.Next(0, 0x80),     // ASCII incl. control chars
					1 => (char)rng.Next(0x80, 0x800),
					_ => (char)rng.Next(0, 0x10000),
				};
			} while (char.IsSurrogate(c));
			return c;
		}

		static string RandomString(Random rng)
		{
			int len = rng.Next(6) == 0 ? rng.Next(0, 3000) : rng.Next(0, 30);
			var sb = new StringBuilder(len);
			for (int i = 0; i < len; i++) {
				if (rng.Next(20) == 0) {
					// A random non-BMP code point as a proper surrogate pair
					int cp = rng.Next(0x10000, 0x110000);
					sb.Append(char.ConvertFromUtf32(cp));
					i++;
				} else
					sb.Append(RandomChar(rng));
			}
			return sb.ToString();
		}

		#endregion

		#region Sync (walks the template; returns a new tree with the synced values)

		public static FuzzNode Sync<SM>(SM sm, FuzzNode template) where SM : ISyncManager
			=> SyncBody(sm, template);

		static FuzzNode SyncBody<SM>(SM sm, FuzzNode template) where SM : ISyncManager
		{
			var result = new FuzzNode { Kind = FuzzKind.Object, Mode = template.Mode, Children = new List<FuzzNode>() };
			var children = template.Children!;
			for (int i = 0; i < children.Count; i++)
				result.Children.Add(SyncNode(sm, "f" + i, children[i]));
			return result;
		}

		static FuzzNode SyncNode<SM>(SM sm, FieldId name, FuzzNode t) where SM : ISyncManager
		{
			var r = new FuzzNode { Kind = t.Kind, Mode = t.Mode };
			switch (t.Kind) {
				case FuzzKind.Bool: r.Value = sm.Sync(name, (bool)t.Value!); break;
				case FuzzKind.Sbyte: r.Value = sm.Sync(name, (sbyte)t.Value!); break;
				case FuzzKind.Byte: r.Value = sm.Sync(name, (byte)t.Value!); break;
				case FuzzKind.Short: r.Value = sm.Sync(name, (short)t.Value!); break;
				case FuzzKind.Ushort: r.Value = sm.Sync(name, (ushort)t.Value!); break;
				case FuzzKind.Int: r.Value = sm.Sync(name, (int)t.Value!); break;
				case FuzzKind.Uint: r.Value = sm.Sync(name, (uint)t.Value!); break;
				case FuzzKind.Long: r.Value = sm.Sync(name, (long)t.Value!); break;
				case FuzzKind.Ulong: r.Value = sm.Sync(name, (ulong)t.Value!); break;
				case FuzzKind.Float: r.Value = sm.Sync(name, (float)t.Value!); break;
				case FuzzKind.Double: r.Value = sm.Sync(name, (double)t.Value!); break;
				case FuzzKind.Decimal: r.Value = sm.Sync(name, (decimal)t.Value!); break;
				case FuzzKind.BigInt: r.Value = sm.Sync(name, (BigInteger)t.Value!); break;
				case FuzzKind.Char: r.Value = sm.Sync(name, (char)t.Value!); break;
				case FuzzKind.NInt: r.Value = sm.Sync(name, (int?)t.Value); break;
				case FuzzKind.NLong: r.Value = sm.Sync(name, (long?)t.Value); break;
				case FuzzKind.NDouble: r.Value = sm.Sync(name, (double?)t.Value); break;
				case FuzzKind.NDecimal: r.Value = sm.Sync(name, (decimal?)t.Value); break;
				case FuzzKind.NBool: r.Value = sm.Sync(name, (bool?)t.Value); break;
				case FuzzKind.String: r.Value = sm.Sync(name, (string?)t.Value); break;
				case FuzzKind.DedupString: r.Value = sm.Sync(name, (string?)t.Value, ObjectMode.Deduplicate); break;
				case FuzzKind.IntArray: r.Value = sm.SyncArray(name, (int[]?)t.Value); break;
				case FuzzKind.ByteArray: r.Value = sm.SyncArray(name, (byte[]?)t.Value); break;
				case FuzzKind.CharArray: r.Value = sm.SyncArray(name, (char[]?)t.Value); break;
				case FuzzKind.DoubleArray: r.Value = sm.SyncArray(name, (double[]?)t.Value); break;
				case FuzzKind.Tuple2: r.Value = DefaultSynchronizer.Sync(ref sm, name, ((int, string))t.Value!); break;
				case FuzzKind.Object: {
					FuzzNode? synced = sm.Sync(name, t.Children == null ? null : t,
						(SM sm2, FuzzNode? t2) => SyncBody(sm2, t2!), t.Mode);
					return synced ?? new FuzzNode { Kind = FuzzKind.Object, Mode = t.Mode, Children = null };
				}
				default: throw new ArgumentException(t.Kind.ToString());
			}
			return r;
		}

		#endregion

		#region Structural comparison

		public static void AssertEqual(FuzzNode expected, FuzzNode actual, string path)
		{
			Assert.AreEqual(expected.Kind, actual.Kind, "kind mismatch at {0}", path);
			if (expected.Kind == FuzzKind.Object) {
				if (expected.Children == null) {
					Assert.IsNull(actual.Children, "expected null object at {0}", path);
					return;
				}
				Assert.IsNotNull(actual.Children, "unexpected null object at {0}", path);
				Assert.AreEqual(expected.Children.Count, actual.Children!.Count, "child count at {0}", path);
				for (int i = 0; i < expected.Children.Count; i++)
					AssertEqual(expected.Children[i], actual.Children[i], path + ".f" + i);
			} else if (expected.Value is Array ea) {
				var aa = (Array?)actual.Value;
				Assert.IsNotNull(aa, "expected array at {0}", path);
				Assert.AreEqual(ea.Length, aa!.Length, "array length at {0}", path);
				for (int i = 0; i < ea.Length; i++)
					Assert.AreEqual(ea.GetValue(i), aa.GetValue(i), "array element {0}[{1}]", path, i);
			} else {
				Assert.AreEqual(expected.Value, actual.Value, "value at {0} (kind {1})", path, expected.Kind);
			}
		}

		#endregion
	}
}
