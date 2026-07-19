using Loyc.MiniTest;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Loyc.SyncLib.Tests
{
	/// <summary>
	///   Cross-implementation tests proving that <see cref="SyncProtobuf"/> speaks real
	///   Protocol Buffers: its output is parsed by protobuf-net using contract types that
	///   match the schema <see cref="SyncProtobuf.Schema"/> generates, and it parses
	///   protobuf-net's output, including encodings SyncProtobuf itself doesn't produce
	///   (such as unpacked repeated scalars).
	/// </summary>
	[TestFixture]
	public class SyncProtobufInteropTests : Loyc.Collections.Impl.TestHelpers
	{
		static byte[] PbSerialize<T>(T value)
		{
			using (var ms = new MemoryStream()) {
				Serializer.Serialize(ms, value);
				return ms.ToArray();
			}
		}
		static T PbDeserialize<T>(byte[] data)
		{
			using (var ms = new MemoryStream(data))
				return Serializer.Deserialize<T>(ms);
		}

		#region Scalar fields in both directions

		[ProtoContract]
		public class PbScalars
		{
			[ProtoMember(1)] public int Int;
			[ProtoMember(2)] public long Long;
			[ProtoMember(3)] public uint UInt;
			[ProtoMember(4)] public ulong ULong;
			[ProtoMember(5)] public bool Flag;
			[ProtoMember(6)] public float F;
			[ProtoMember(7)] public double D;
			[ProtoMember(8)] public string? Name;
			[ProtoMember(9)] public byte[]? Blob;
		}

		static PbScalars SyncScalars(ISyncManager sm, PbScalars? m)
		{
			m ??= new PbScalars();
			m.Int = sm.Sync(("Int", 1), m.Int);
			m.Long = sm.Sync(("Long", 2), m.Long);
			m.UInt = sm.Sync(("UInt", 3), m.UInt);
			m.ULong = sm.Sync(("ULong", 4), m.ULong);
			m.Flag = sm.Sync(("Flag", 5), m.Flag);
			m.F = sm.Sync(("F", 6), m.F);
			m.D = sm.Sync(("D", 7), m.D);
			m.Name = sm.Sync(("Name", 8), m.Name);
			m.Blob = sm.SyncList(("Blob", 9), m.Blob);
			return m;
		}

		static PbScalars NewScalars() => new PbScalars {
			Int = -42, Long = long.MinValue, UInt = uint.MaxValue, ULong = ulong.MaxValue,
			Flag = true, F = 2.5f, D = -1e100, Name = "interop ✓", Blob = new byte[] { 0, 1, 255 },
		};

		static void AssertScalarsEqual(PbScalars a, PbScalars b)
		{
			AreEqual(a.Int, b.Int);
			AreEqual(a.Long, b.Long);
			AreEqual(a.UInt, b.UInt);
			AreEqual(a.ULong, b.ULong);
			AreEqual(a.Flag, b.Flag);
			AreEqual(a.F, b.F);
			AreEqual(a.D, b.D);
			AreEqual(a.Name, b.Name);
			ExpectList(b.Blob!, a.Blob!);
		}

		[Test]
		public void ProtobufNetReadsSyncLibScalars()
		{
			var value = NewScalars();
			byte[] data = SyncProtobuf.WriteI(value, SyncScalars).ToArray();
			AssertScalarsEqual(value, PbDeserialize<PbScalars>(data));
		}

		[Test]
		public void SyncLibReadsProtobufNetScalars()
		{
			var value = NewScalars();
			byte[] data = PbSerialize(value);
			AssertScalarsEqual(value, SyncProtobuf.ReadI<PbScalars>(data, SyncScalars)!);
		}

		#endregion

		#region Nested messages (a linked list of them)

		[ProtoContract]
		public class PbTree
		{
			[ProtoMember(1)] public string? Label;
			[ProtoMember(2)] public PbTree? Child;
		}

		static PbTree SyncTree(ISyncManager sm, PbTree? t)
		{
			t ??= new PbTree();
			t.Label = sm.Sync(("Label", 1), t.Label);
			t.Child = sm.Sync(("Child", 2), t.Child, SyncTree, ObjectMode.Normal);
			return t;
		}

		[Test]
		public void NestedMessagesInBothDirections()
		{
			var tree = new PbTree { Label = "a", Child = new PbTree { Label = "b", Child = new PbTree { Label = "c" } } };

			var fromSyncLib = PbDeserialize<PbTree>(SyncProtobuf.WriteI(tree, SyncTree).ToArray());
			AreEqual("a", fromSyncLib.Label);
			AreEqual("b", fromSyncLib.Child!.Label);
			AreEqual("c", fromSyncLib.Child.Child!.Label);
			IsNull(fromSyncLib.Child.Child.Child);

			var fromPbNet = SyncProtobuf.ReadI<PbTree>(PbSerialize(tree), SyncTree)!;
			AreEqual("a", fromPbNet.Label);
			AreEqual("b", fromPbNet.Child!.Label);
			AreEqual("c", fromPbNet.Child.Child!.Label);
			IsNull(fromPbNet.Child.Child.Child);
		}

		#endregion

		#region Lists (the list container message, packed and unpacked)

		// Matches the schema SyncProtobuf generates for an int list field:
		// message Int32List { repeated int32 items = 1; } used via a message-typed field
		[ProtoContract]
		public class PbIntListPacked
		{
			[ProtoMember(1, IsPacked = true)] public int[]? Items;
		}
		[ProtoContract]
		public class PbIntListUnpacked
		{
			[ProtoMember(1)] public int[]? Items; // protobuf-net writes this unpacked
		}
		[ProtoContract]
		public class PbHolder<TList>
		{
			[ProtoMember(1)] public TList? List;
		}

		static int[]? SyncIntList(ISyncManager sm, int[]? v) => sm.SyncList(("List", 1), v);

		[Test]
		public void ProtobufNetReadsSyncLibIntList()
		{
			int[] ints = { 0, 1, -1, int.MaxValue, int.MinValue };
			byte[] data = SyncProtobuf.WriteI<int[]>(ints, SyncIntList).ToArray();
			var holder = PbDeserialize<PbHolder<PbIntListPacked>>(data);
			ExpectList(holder.List!.Items!, ints);
		}

		[Test]
		public void SyncLibReadsPackedAndUnpackedIntLists()
		{
			int[] ints = { 5, -6, 7000000, -8 };

			byte[] packed = PbSerialize(new PbHolder<PbIntListPacked> { List = new PbIntListPacked { Items = ints } });
			ExpectList(SyncProtobuf.ReadI<int[]>(packed, SyncIntList)!, ints);

			byte[] unpacked = PbSerialize(new PbHolder<PbIntListUnpacked> { List = new PbIntListUnpacked { Items = ints } });
			ExpectList(SyncProtobuf.ReadI<int[]>(unpacked, SyncIntList)!, ints);
		}

		#endregion

		#region Nullable list elements (the Opt wrapper message)

		[ProtoContract]
		public class PbStringOpt
		{
			[ProtoMember(1)] public string? Value;
		}
		[ProtoContract]
		public class PbStringOptList
		{
			[ProtoMember(1)] public List<PbStringOpt>? Items;
		}

		static string?[]? SyncStringList(ISyncManager sm, string?[]? v) => sm.SyncList(("L", 1), v);

		[Test]
		public void ProtobufNetReadsSyncLibStringListWithNulls()
		{
			var strings = new string?[] { "dup", null, "", "dup" };
			byte[] data = SyncProtobuf.WriteI(strings, SyncStringList).ToArray();

			var holder = PbDeserialize<PbHolder<PbStringOptList>>(data);
			var items = holder.List!.Items!;
			AreEqual(4, items.Count);
			AreEqual("dup", items[0].Value);
			AreEqual(null, items[1].Value); // a null element is an empty wrapper message
			AreEqual("", items[2].Value);   // ...which stays distinct from the empty string
			AreEqual("dup", items[3].Value);
		}

		#endregion

		#region Deduplicated values (the Ref wrapper message)

		[ProtoContract]
		public class PbTreeRef
		{
			[ProtoMember(1)] public ulong Id;
			[ProtoMember(2)] public PbTree? Value;
		}
		[ProtoContract]
		public class PbDedupPair
		{
			[ProtoMember(1)] public PbTreeRef? A;
			[ProtoMember(2)] public PbTreeRef? B;
		}

		[Test]
		public void ProtobufNetReadsSyncLibDedupWrappers()
		{
			var shared = new PbTree { Label = "shared" };
			byte[] data = SyncProtobuf.WriteI(shared, (sm, _) => {
				sm.Sync(("A", 1), shared, SyncTree, ObjectMode.Deduplicate);
				sm.Sync(("B", 2), shared, SyncTree, ObjectMode.Deduplicate);
				return shared;
			}).ToArray();

			var pair = PbDeserialize<PbDedupPair>(data);
			AreEqual("shared", pair.A!.Value!.Label);
			IsNull(pair.B!.Value); // second occurrence is a back-reference: id only
			AreEqual(pair.A.Id, pair.B.Id);
			IsTrue(pair.A.Id != 0);
		}

		#endregion
	}
}
