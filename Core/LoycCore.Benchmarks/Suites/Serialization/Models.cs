// Data models for the non-calendar serialization scenarios. Each model carries the
// attributes required by all attribute-driven serializers under test
// ([Serializable] for BinaryFormatter, [ProtoContract]/[ProtoMember] for
// protobuf-net, [MessagePackObject]/[Key] for MessagePack); System.Text.Json and
// Newtonsoft need no attributes, and SyncLib uses the sync functions in
// SyncFunctions.cs instead of attributes.
using MessagePack;
using ProtoBuf;

namespace Benchmark.Serialization
{
	/// <summary>A minimal object (same shape as SmallObject in the SyncLib test
	/// suite), used in lists to measure per-object overhead.</summary>
	[Serializable, ProtoContract, MessagePackObject]
	public class SmallObject : IEquatable<SmallObject>
	{
		[ProtoMember(1), Key(0)] public int Field1;
		[ProtoMember(2), Key(1)] public string? Field2;
		[ProtoMember(3), Key(2)] public double Field3;

		public bool Equals(SmallObject? other) =>
			other != null && Field1 == other.Field1 && Field2 == other.Field2 && Field3 == other.Field3;
		public override bool Equals(object? obj) => obj is SmallObject so && Equals(so);
		public override int GetHashCode() => Field1 + (Field2?.GetHashCode() ?? 0) + Field3.GetHashCode();

		public static List<SmallObject> MakeList(int count, int seed = 999)
		{
			var random = new Random(seed);
			var words = SampleData.Words;
			return Enumerable.Range(0, count).Select(i => new SmallObject {
				Field1 = random.Next(1000000),
				Field2 = words[random.Next(words.Length)],
				Field3 = random.NextDouble() * 1000,
			}).ToList();
		}
	}

	/// <summary>A wide, flat object: 13 primitive/string field types plus a nullable
	/// variant of each (26 fields). BigInteger and char are omitted because
	/// protobuf-net does not support them.</summary>
	[Serializable, ProtoContract, MessagePackObject]
	public class WideObject
	{
		[ProtoMember(1),  Key(0)]  public bool Bool;
		[ProtoMember(2),  Key(1)]  public sbyte Int8;
		[ProtoMember(3),  Key(2)]  public byte Uint8;
		[ProtoMember(4),  Key(3)]  public short Int16;
		[ProtoMember(5),  Key(4)]  public ushort Uint16;
		[ProtoMember(6),  Key(5)]  public int Int32;
		[ProtoMember(7),  Key(6)]  public uint Uint32;
		[ProtoMember(8),  Key(7)]  public long Int64;
		[ProtoMember(9),  Key(8)]  public ulong Uint64;
		[ProtoMember(10), Key(9)]  public float Single;
		[ProtoMember(11), Key(10)] public double Double;
		[ProtoMember(12), Key(11)] public decimal Decimal;
		[ProtoMember(13), Key(12)] public string String = "";
		[ProtoMember(14), Key(13)] public bool? BoolNullable;
		[ProtoMember(15), Key(14)] public sbyte? Int8Nullable;
		[ProtoMember(16), Key(15)] public byte? Uint8Nullable;
		[ProtoMember(17), Key(16)] public short? Int16Nullable;
		[ProtoMember(18), Key(17)] public ushort? Uint16Nullable;
		[ProtoMember(19), Key(18)] public int? Int32Nullable;
		[ProtoMember(20), Key(19)] public uint? Uint32Nullable;
		[ProtoMember(21), Key(20)] public long? Int64Nullable;
		[ProtoMember(22), Key(21)] public ulong? Uint64Nullable;
		[ProtoMember(23), Key(22)] public float? SingleNullable;
		[ProtoMember(24), Key(23)] public double? DoubleNullable;
		[ProtoMember(25), Key(24)] public decimal? DecimalNullable;
		[ProtoMember(26), Key(25)] public string? StringNullable;

		public static WideObject Make(int seed = 23)
		{
			return new WideObject {
				Bool = true,
				Int8 = (sbyte)-seed, Uint8 = (byte)seed,
				Int16 = (short)(-seed * 100), Uint16 = (ushort)(seed * 100),
				Int32 = -seed * 1000000, Uint32 = (uint)seed * 1000000,
				Int64 = -seed * 100000000000L, Uint64 = (ulong)seed * 100000000000,
				Single = seed * 1.5f, Double = seed * Math.PI, Decimal = seed * 123.456m,
				String = "The quick brown fox jumps over the lazy dog",
				// Half of the nullable fields hold values, half are null
				BoolNullable = false,
				Int8Nullable = null, Uint8Nullable = (byte)(seed + 1),
				Int16Nullable = null, Uint16Nullable = (ushort)(seed + 2),
				Int32Nullable = null, Uint32Nullable = (uint)(seed + 3),
				Int64Nullable = null, Uint64Nullable = (ulong)(seed + 4),
				SingleNullable = null, DoubleNullable = seed * 2.25,
				DecimalNullable = null, StringNullable = null,
			};
		}

		/// <summary>Returns the name of the first field whose value differs from
		/// <c>other</c>'s, or null if all 26 fields match.</summary>
		/// <remarks>One-line checks, against our usual style: this is a table of 26
		/// mechanical comparisons, and two-line form would double its length without
		/// making it clearer.</remarks>
		public string? DiffFrom(WideObject? other)
		{
			if (other == null) return "deserialized to null";
			if (Bool != other.Bool) return "Bool";
			if (Int8 != other.Int8) return "Int8";
			if (Uint8 != other.Uint8) return "Uint8";
			if (Int16 != other.Int16) return "Int16";
			if (Uint16 != other.Uint16) return "Uint16";
			if (Int32 != other.Int32) return "Int32";
			if (Uint32 != other.Uint32) return "Uint32";
			if (Int64 != other.Int64) return "Int64";
			if (Uint64 != other.Uint64) return "Uint64";
			if (Single != other.Single) return "Single";
			if (Double != other.Double) return "Double";
			if (Decimal != other.Decimal) return "Decimal";
			if (String != other.String) return "String";
			if (BoolNullable != other.BoolNullable) return "BoolNullable";
			if (Int8Nullable != other.Int8Nullable) return "Int8Nullable";
			if (Uint8Nullable != other.Uint8Nullable) return "Uint8Nullable";
			if (Int16Nullable != other.Int16Nullable) return "Int16Nullable";
			if (Uint16Nullable != other.Uint16Nullable) return "Uint16Nullable";
			if (Int32Nullable != other.Int32Nullable) return "Int32Nullable";
			if (Uint32Nullable != other.Uint32Nullable) return "Uint32Nullable";
			if (Int64Nullable != other.Int64Nullable) return "Int64Nullable";
			if (Uint64Nullable != other.Uint64Nullable) return "Uint64Nullable";
			if (SingleNullable != other.SingleNullable) return "SingleNullable";
			if (DoubleNullable != other.DoubleNullable) return "DoubleNullable";
			if (DecimalNullable != other.DecimalNullable) return "DecimalNullable";
			if (StringNullable != other.StringNullable) return "StringNullable";
			return null;
		}
	}

	/// <summary>A linked node for the deep-nesting scenario.</summary>
	[Serializable, ProtoContract, MessagePackObject]
	public class Node
	{
		[ProtoMember(1), Key(0)] public int Id;
		[ProtoMember(2), Key(1)] public string Name = "";
		[ProtoMember(3), Key(2)] public Node? Child;

		public static Node MakeChain(int depth, int seed = 7)
		{
			var words = SampleData.Words;
			var random = new Random(seed);
			Node? child = null;
			for (int i = depth; i >= 1; i--)
				child = new Node { Id = i, Name = words[random.Next(words.Length)], Child = child };
			return child!;
		}

		public static string? Diff(Node? a, Node? b)
		{
			for (int depth = 0; ; depth++, a = a.Child, b = b.Child) {
				if (a == null || b == null)
					return a == b ? null : $"chain length differs at depth {depth}";
				if (a.Id != b.Id || a.Name != b.Name)
					return $"node mismatch at depth {depth}";
			}
		}
	}

	/// <summary>Generators for the primitive-array and dictionary scenarios.</summary>
	public static class ArrayData
	{
		public static int[] MakeSmallInts(int count) {
			var r = new Random(101);
			return Enumerable.Range(0, count).Select(_ => r.Next(128)).ToArray();
		}
		public static int[] MakeLargeInts(int count) {
			var r = new Random(102);
			return Enumerable.Range(0, count).Select(_ => r.Next(int.MinValue, int.MaxValue)).ToArray();
		}
		public static long[] MakeLongs(int count) {
			var r = new Random(103);
			return Enumerable.Range(0, count).Select(_ => r.NextInt64()).ToArray();
		}
		public static double[] MakeDoubles(int count) {
			var r = new Random(104);
			return Enumerable.Range(0, count).Select(_ => (r.NextDouble() - 0.5) * 1e6).ToArray();
		}
		public static byte[] MakeBytes(int count) {
			var bytes = new byte[count];
			new Random(105).NextBytes(bytes);
			return bytes;
		}
		public static string[] MakeAsciiStrings(int count) {
			var r = new Random(106);
			var words = SampleData.Words;
			return Enumerable.Range(0, count)
				.Select(_ => string.Join(" ",
					Enumerable.Range(0, r.Next(2, 9)).Select(_ => words[r.Next(words.Length)])))
				.ToArray();
		}
		/// <summary>Strings containing characters that JSON must escape, plus non-ASCII text.</summary>
		public static string[] MakeMessyStrings(int count) {
			var r = new Random(107);
			string[] spice = { "\"quoted\"", "back\\slash", "tab\there", "new\nline", "héllo wörld",
				"日本語テキスト", "emoji 🚀💾", "«guillemets»", " control" };
			var ascii = MakeAsciiStrings(count);
			return ascii.Select(s => s + " " + spice[r.Next(spice.Length)]).ToArray();
		}

		public static Dictionary<string, string> MakeStringDict(int count) {
			var r = new Random(108);
			var words = SampleData.Words;
			var dict = new Dictionary<string, string>();
			for (int i = 0; dict.Count < count; i++)
				dict[words[r.Next(words.Length)] + "_" + i] =
					string.Join(" ", Enumerable.Range(0, r.Next(1, 7)).Select(_ => words[r.Next(words.Length)]));
			return dict;
		}
	}
}
