// SyncLib synchronization functions for the models in Models.cs. Each is generic
// over the sync manager type SM, so one function works for SyncJson and SyncBinary,
// reading and writing — and the JIT specializes it for each concrete manager
// (SyncLib's "fast path", no interface dispatch).
using Loyc.SyncLib;

namespace Benchmark.Serialization
{
	public static class SyncFunctions
	{
		public static SmallObject SyncSmallObject<SM>(SM sm, SmallObject? obj) where SM : ISyncManager
		{
			obj ??= new SmallObject();
			obj.Field1 = sm.Sync("Field1", obj.Field1);
			obj.Field2 = sm.Sync("Field2", obj.Field2);
			obj.Field3 = sm.Sync("Field3", obj.Field3);
			return obj;
		}

		public static List<SmallObject> SyncSmallObjectList<SM>(SM sm, List<SmallObject>? list) where SM : ISyncManager
			=> sm.SyncList("list", list, SyncSmallObject, ObjectMode.Normal)!;

		public static WideObject SyncWideObject<SM>(SM sm, WideObject? obj) where SM : ISyncManager
		{
			obj ??= new WideObject();
			obj.Bool    = sm.Sync("Bool", obj.Bool);
			obj.Int8    = sm.Sync("Int8", obj.Int8);
			obj.Uint8   = sm.Sync("Uint8", obj.Uint8);
			obj.Int16   = sm.Sync("Int16", obj.Int16);
			obj.Uint16  = sm.Sync("Uint16", obj.Uint16);
			obj.Int32   = sm.Sync("Int32", obj.Int32);
			obj.Uint32  = sm.Sync("Uint32", obj.Uint32);
			obj.Int64   = sm.Sync("Int64", obj.Int64);
			obj.Uint64  = sm.Sync("Uint64", obj.Uint64);
			obj.Single  = sm.Sync("Single", obj.Single);
			obj.Double  = sm.Sync("Double", obj.Double);
			obj.Decimal = sm.Sync("Decimal", obj.Decimal);
			obj.String  = sm.Sync("String", obj.String) ?? "";
			obj.BoolNullable    = sm.Sync("BoolNullable", obj.BoolNullable);
			obj.Int8Nullable    = sm.Sync("Int8Nullable", obj.Int8Nullable);
			obj.Uint8Nullable   = sm.Sync("Uint8Nullable", obj.Uint8Nullable);
			obj.Int16Nullable   = sm.Sync("Int16Nullable", obj.Int16Nullable);
			obj.Uint16Nullable  = sm.Sync("Uint16Nullable", obj.Uint16Nullable);
			obj.Int32Nullable   = sm.Sync("Int32Nullable", obj.Int32Nullable);
			obj.Uint32Nullable  = sm.Sync("Uint32Nullable", obj.Uint32Nullable);
			obj.Int64Nullable   = sm.Sync("Int64Nullable", obj.Int64Nullable);
			obj.Uint64Nullable  = sm.Sync("Uint64Nullable", obj.Uint64Nullable);
			obj.SingleNullable  = sm.Sync("SingleNullable", obj.SingleNullable);
			obj.DoubleNullable  = sm.Sync("DoubleNullable", obj.DoubleNullable);
			obj.DecimalNullable = sm.Sync("DecimalNullable", obj.DecimalNullable);
			obj.StringNullable  = sm.Sync("StringNullable", obj.StringNullable);
			return obj;
		}

		public static Node SyncNode<SM>(SM sm, Node? node) where SM : ISyncManager
		{
			node ??= new Node();
			node.Id = sm.Sync("Id", node.Id);
			node.Name = sm.Sync("Name", node.Name) ?? "";
			node.Child = sm.Sync("Child", node.Child, SyncNode, ObjectMode.Normal);
			return node;
		}

		#region Arrays (each element type needs its own tiny function for type inference)

		public static int[] SyncIntArray<SM>(SM sm, int[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		public static long[] SyncLongArray<SM>(SM sm, long[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		public static double[] SyncDoubleArray<SM>(SM sm, double[]? v) where SM : ISyncManager
			=> sm.SyncArray("list", v)!;
		public static byte[] SyncByteArray<SM>(SM sm, byte[]? v) where SM : ISyncManager => sm.SyncArray("list", v)!;
		public static string[] SyncStringArray<SM>(SM sm, string[]? v) where SM : ISyncManager
			=> sm.SyncArray("list", v)!;

		#endregion

		#region String dictionary

		/// <summary>Serializes a string dictionary the natural JSON way — as one JSON
		/// object whose property names are the keys. Only works with JSON-like
		/// managers (dynamic field names on write, NextField support on read).</summary>
		public static Dictionary<string, string> SyncStringDictAsObject<SM>(SM sm, Dictionary<string, string>? dict)
			where SM : ISyncManager
		{
			dict ??= new Dictionary<string, string>();
			if (sm.IsReading) {
				if (!sm.SupportsNextField || sm.NeedsIntegerIds)
					throw new NotSupportedException("SyncStringDictAsObject is incompatible with " + sm.GetType().Name);
				string? name;
				while ((name = sm.NextField.Name) != null)
					dict[name] = sm.Sync(null, "")!;
			} else {
				foreach (var pair in dict)
					sm.Sync(pair.Key, pair.Value);
			}
			return dict;
		}

		/// <summary>Serializes a string dictionary as a list of key/value pairs,
		/// which works in any format including SyncBinary.</summary>
		public static Dictionary<string, string> SyncStringDictAsList<SM>(SM sm, Dictionary<string, string>? dict)
			where SM : ISyncManager
			=> sm.SyncDict("list", dict, SyncKeyValuePair, ObjectMode.Normal)!;

		static KeyValuePair<string, string> SyncKeyValuePair<SM>(SM sm, KeyValuePair<string, string> pair)
			where SM : ISyncManager
		{
			var key = sm.Sync("K", pair.Key);
			var value = sm.Sync("V", pair.Value);
			return new(key!, value!);
		}

		#endregion
	}
}
