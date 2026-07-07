using Loyc.Collections;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	/// <summary>Creates a <see cref="SyncProtobuf.Schema"/>, an object that produces a
	///   Protocol Buffers <c>.proto</c> (proto3) definition describing the messages that
	///   <see cref="SyncProtobuf.Writer"/> would produce. After using it to "synchronize"
	///   one root object, call <see cref="Schema.Finish"/> (or ToString) to get the
	///   schema document.</summary>
	public static Schema NewSchema(Options? options = null)
		=> new Schema(new SchemaState(options ?? _defaultOptions));

	/// <summary>Generates a proto3 <c>.proto</c> schema (UTF-8) describing the Protobuf
	///   output that <see cref="SyncProtobuf.Write{T}(T, SyncObjectFunc{Writer, T}, Options?)"/>
	///   produces for type T with the same synchronizer.</summary>
	/// <remarks>The synchronizer runs once in <see cref="SyncMode.Schema"/> mode, in which
	///   there is no data: it receives a default/null value of T, and the Sync methods
	///   return default values. Field numbers in the schema are assigned exactly as the
	///   writer assigns them (from <see cref="FieldId.Id"/>, else auto <c>N+1</c>).</remarks>
	public static ReadOnlyMemory<byte> WriteSchema<T>(SyncObjectFunc<Schema, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		var schema = NewSchema(options);
		SyncManagerExt.Sync(schema, null, default(T), sync, options.RootMode);
		return schema.Finish();
	}
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>
	public static ReadOnlyMemory<byte> WriteSchemaI<T>(SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		var schema = NewSchema(options);
		SyncManagerExt.Sync<ISyncManager, T>(schema, null, default(T), sync, options.RootMode);
		return schema.Finish();
	}
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>
	public static ReadOnlyMemory<byte> WriteSchema<T, SyncObject>(SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<Schema, T>
	{
		options ??= _defaultOptions;
		var schema = NewSchema(options);
		SyncManagerExt.Sync<Schema, SyncObject, T>(schema, null, default(T), sync, options.RootMode);
		return schema.Finish();
	}

	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>
	public static string WriteSchemaString<T>(SyncObjectFunc<Schema, T> sync, Options? options = null)
		=> Utf8ToString(WriteSchema(sync, options));
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>
	public static string WriteSchemaStringI<T>(SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		=> Utf8ToString(WriteSchemaI(sync, options));
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{Schema, T}, Options?)"/>
	public static string WriteSchemaString<T, SyncObject>(SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<Schema, T>
		=> Utf8ToString(WriteSchema<T, SyncObject>(sync, options));

	internal static string Utf8ToString(ReadOnlyMemory<byte> utf8)
		=> Encoding.UTF8.GetString(utf8.ToArray());

	/// <summary>
	///   An implementation of <see cref="ISyncManager"/> that produces a Protocol Buffers
	///   proto3 <c>.proto</c> definition describing the output of <see cref="SyncProtobuf.Writer"/>.
	/// </summary><remarks>
	///   <see cref="Mode"/> is <see cref="SyncMode.Schema"/>, so there is no data: your
	///   synchronizer receives a default/null value and should call the same methods it
	///   would call when loading. Since a schema saver has no data, it cannot see
	///   data-dependent behavior (fields synchronized only on some code paths, or the
	///   branch chosen for a polymorphic type tag). It reports
	///   <see cref="NeedsIntegerIds"/> = true, so synchronizers take the same code path
	///   they take for the real writer and field numbers line up.
	/// <para/>
	///   Each distinct sub-object type (identified by the childKey passed to
	///   <see cref="BeginSubObject"/>, normally <c>typeof(T)</c>) becomes a
	///   <c>message</c>. Recursive/cyclic types are handled by referencing the message by
	///   name. A type synchronized in two conflicting ways causes
	///   <see cref="EndSubObject"/> to throw. The message name is the .NET type name unless
	///   <see cref="SyncTypeTag"/> is called.
	/// <para/>
	///   The generated <c>.proto</c> describes the wire format exactly: any Protobuf
	///   implementation can use it to parse <see cref="SyncProtobuf.Writer"/>'s output.
	///   Lists, nullable list elements and deduplicated values appear as generated
	///   wrapper message types (e.g. <c>Int32List</c>, <c>StringOpt</c>, <c>PersonRef</c> —
	///   see <see cref="SyncProtobuf"/> for the encoding); <c>decimal</c> and
	///   <see cref="BigInteger"/> (which Protobuf lacks) map to <c>bytes</c>; and a
	///   comment at the top names the root message.
	/// </remarks>
	public partial struct Schema : ISyncManager
	{
		internal SchemaState _s;
		internal Schema(SchemaState s) => _s = s;

		public SyncMode Mode => SyncMode.Schema;
		public bool IsReading => true;
		public bool IsWriting => false;

		public bool SupportsReordering => true;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => true;
		public bool SupportsNextField => false;

		public bool IsInsideList => _s.IsInsideList;
		public bool? ReachedEndOfList => _s.ReachedEndOfList;
		public int? MinimumListLength => _s.MinimumListLength;
		public int Depth => _s.Depth;

		public object CurrentObject { set { } }
		public FieldId NextField => FieldId.Missing;
		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => SyncType.Unknown;

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
			=> _s.BeginSubObject(name, childKey, mode, listLength);

		public void EndSubObject() => _s.EndSubObject();

		public string? SyncTypeTag(string? tag) => _s.SyncTypeTag(tag);

		/// <summary>Renders the recorded schema as a proto3 <c>.proto</c> document (UTF-8).</summary>
		public ReadOnlyMemory<byte> Finish()
		{
			var output = new ArrayBufferWriter<byte>(System.Math.Max(64, _s._opt.Write.InitialBufferSize));
			_s.Render(output);
			return output.WrittenMemory;
		}
		/// <summary>Renders the recorded schema as a proto3 <c>.proto</c> string.</summary>
		public override string ToString() => Utf8ToString(Finish());

		#region Primitive Sync methods

		public bool   Sync(FieldId name, bool savable)   { _s.SyncScalar(name, "bool", false); return default; }
		public sbyte  Sync(FieldId name, sbyte savable)  { _s.SyncScalar(name, "int32", false); return default; }
		public byte   Sync(FieldId name, byte savable)   { _s.SyncScalar(name, "uint32", false); return default; }
		public short  Sync(FieldId name, short savable)  { _s.SyncScalar(name, "int32", false); return default; }
		public ushort Sync(FieldId name, ushort savable) { _s.SyncScalar(name, "uint32", false); return default; }
		public int    Sync(FieldId name, int savable)    { _s.SyncScalar(name, "int32", false); return default; }
		public uint   Sync(FieldId name, uint savable)   { _s.SyncScalar(name, "uint32", false); return default; }
		public long   Sync(FieldId name, long savable)   { _s.SyncScalar(name, "int64", false); return default; }
		public ulong  Sync(FieldId name, ulong savable)  { _s.SyncScalar(name, "uint64", false); return default; }
		public float  Sync(FieldId name, float savable)  { _s.SyncScalar(name, "float", false); return default; }
		public double Sync(FieldId name, double savable) { _s.SyncScalar(name, "double", false); return default; }
		public decimal Sync(FieldId name, decimal savable) { _s.SyncScalar(name, "bytes", false); return default; }
		public BigInteger Sync(FieldId name, BigInteger savable) { _s.SyncScalar(name, "bytes", false); return default; }
		public char   Sync(FieldId name, char savable)   { _s.SyncScalar(name, "uint32", false); return default; }

		public int Sync(FieldId name, int savable, int bits, bool signed = true) { _s.SyncScalar(name, "int32", false); return default; }
		public long Sync(FieldId name, long savable, int bits, bool signed = true) { _s.SyncScalar(name, "int64", false); return default; }
		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true) { _s.SyncScalar(name, "bytes", false); return default; }

		public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal)
			{ _s.SyncScalar(name, "string", true, dedup: (mode & ObjectMode.Deduplicate) != 0); return default; }

		public bool?   Sync(FieldId name, bool? savable)   { _s.SyncScalar(name, "bool", true); return default; }
		public sbyte?  Sync(FieldId name, sbyte? savable)  { _s.SyncScalar(name, "int32", true); return default; }
		public byte?   Sync(FieldId name, byte? savable)   { _s.SyncScalar(name, "uint32", true); return default; }
		public short?  Sync(FieldId name, short? savable)  { _s.SyncScalar(name, "int32", true); return default; }
		public ushort? Sync(FieldId name, ushort? savable) { _s.SyncScalar(name, "uint32", true); return default; }
		public int?    Sync(FieldId name, int? savable)    { _s.SyncScalar(name, "int32", true); return default; }
		public uint?   Sync(FieldId name, uint? savable)   { _s.SyncScalar(name, "uint32", true); return default; }
		public long?   Sync(FieldId name, long? savable)   { _s.SyncScalar(name, "int64", true); return default; }
		public ulong?  Sync(FieldId name, ulong? savable)  { _s.SyncScalar(name, "uint64", true); return default; }
		public float?  Sync(FieldId name, float? savable)  { _s.SyncScalar(name, "float", true); return default; }
		public double? Sync(FieldId name, double? savable) { _s.SyncScalar(name, "double", true); return default; }
		public decimal? Sync(FieldId name, decimal? savable) { _s.SyncScalar(name, "bytes", true); return default; }
		public BigInteger? Sync(FieldId name, BigInteger? savable) { _s.SyncScalar(name, "bytes", true); return default; }
		public char?   Sync(FieldId name, char? savable)   { _s.SyncScalar(name, "uint32", true); return default; }

		#endregion

		#region List implementations

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			SyncScalarList(name, "bool", mode, tupleLength);
			return default;
		}
		public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
		{
			// Byte lists are stored as a Protobuf `bytes` value, not as a list container
			bool nullable = (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;
			_s.SyncScalar(name, "bytes", nullable, dedup: (mode & ObjectMode.Deduplicate) != 0);
			return default;
		}
		public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			SyncScalarList(name, "uint32", mode, tupleLength);
			return default;
		}

		// Records a list/tuple of scalar elements the same way the writer encodes it
		// (a list container, or a message with auto-numbered fields for a tuple)
		void SyncScalarList(FieldId name, string elemType, ObjectMode mode, int tupleLength)
		{
			var (begun, _, _) = _s.BeginSubObject(name, null, mode | ObjectMode.List, tupleLength);
			if (begun) {
				int n = (mode & ObjectMode.Tuple) == ObjectMode.Tuple && tupleLength > 0 ? tupleLength : 1;
				for (int i = 0; i < n; i++)
					_s.SyncScalar((string?)null, elemType, false);
				_s.EndSubObject();
			}
		}

		#endregion
	}
}
