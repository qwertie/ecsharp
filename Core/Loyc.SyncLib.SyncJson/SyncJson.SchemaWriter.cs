using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using Loyc.Compatibility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;

#nullable enable

namespace Loyc.SyncLib;

partial class SyncJson
{
	/// <summary>Creates a <see cref="SyncJson.SchemaWriter"/>, an object for saving a
	///   JSON Schema that describes the JSON produced by <see cref="SyncJson.Writer"/>.
	///   After using it to "synchronize" one root object, call
	///   <see cref="SchemaWriter.Finish"/> (or ToString) to get the schema document.</summary>
	public static SchemaWriter NewSchemaWriter(Options? options = null)
		=> new SchemaWriter(new SchemaState(options ?? _defaultOptions));

	/// <summary>Generates a JSON Schema (draft 2020-12, UTF-8) describing the JSON
	///   that <see cref="SyncJson.Write{T}(T, SyncObjectFunc{Writer, T}, Options?)"/>
	///   produces for type T with the same synchronizer and options.</summary>
	/// <remarks>The synchronizer function runs once in <see cref="SyncMode.Schema"/>
	///   mode, in which there is no data: the function receives a default/null value
	///   of T, and the Sync methods return default values. Fields that the function
	///   only synchronizes conditionally are recorded only if the condition happens
	///   to be true when given default values.</remarks>
	public static ReadOnlyMemory<byte> WriteSchema<T>(SyncObjectFunc<SchemaWriter, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		var schema = NewSchemaWriter(options);
		SyncManagerExt.Sync(schema, null, default(T), sync, options.RootMode);
		return schema.Finish();
	}
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{SchemaWriter, T}, Options?)"/>
	public static ReadOnlyMemory<byte> WriteSchemaI<T>(SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		var schema = NewSchemaWriter(options);
		SyncManagerExt.Sync<ISyncManager, T>(schema, null, default(T), sync, options.RootMode);
		return schema.Finish();
	}
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{SchemaWriter, T}, Options?)"/>
	public static ReadOnlyMemory<byte> WriteSchema<T, SyncObject>(SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<SchemaWriter, T>
	{
		options ??= _defaultOptions;
		var schema = NewSchemaWriter(options);
		SyncManagerExt.Sync<SchemaWriter, SyncObject, T>(schema, null, default(T), sync, options.RootMode);
		return schema.Finish();
	}

	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{SchemaWriter, T}, Options?)"/>
	public static string WriteSchemaString<T>(SyncObjectFunc<SchemaWriter, T> sync, Options? options = null)
		=> Utf8ToString(WriteSchema(sync, options));
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{SchemaWriter, T}, Options?)"/>
	public static string WriteSchemaStringI<T>(SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		=> Utf8ToString(WriteSchemaI(sync, options));
	/// <inheritdoc cref="WriteSchema{T}(SyncObjectFunc{SchemaWriter, T}, Options?)"/>
	public static string WriteSchemaString<T, SyncObject>(SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<SchemaWriter, T>
		=> Utf8ToString(WriteSchema<T, SyncObject>(sync, options));

	/// <summary>
	///   An implementation of <see cref="ISyncManager"/> that produces a JSON Schema
	///   (draft 2020-12) describing the JSON that <see cref="SyncJson.Writer"/> would
	///   produce with the same synchronizer and <see cref="Options"/> — including the
	///   effects of options such as <see cref="Options.NameConverter"/>,
	///   <see cref="Options.NewtonsoftCompatibility"/> and <see cref="Options.ByteArrayMode"/>,
	///   and the "$id"/"$ref" markers used when <see cref="ObjectMode.Deduplicate"/> is used.
	/// </summary><remarks>
	///   <see cref="Mode"/> is <see cref="SyncMode.Schema"/>, so there is no actual
	///   data: your synchronizer function receives a default/null value and should
	///   call the same methods it would call when loading. Sync methods return
	///   default values, and each sub-object type is traversed only as often as
	///   needed to learn (and double-check) its schema.
	/// <para/>
	///   The types of sub-objects are identified by the childKey passed to
	///   <see cref="BeginSubObject"/>, which is normally <c>typeof(T)</c> (helper
	///   methods such as <see cref="SyncManagerExt.Sync{SM, T}(SM, FieldId, T, SyncObjectFunc{SM, T}, ObjectMode)"/>
	///   arrange this automatically). Each type becomes a named definition in the
	///   "$defs" section of the schema, referenced via "$ref"; this is also what
	///   allows recursive (cyclic) types to be described. The definition's name is
	///   the .NET type name, unless the synchronizer calls <see cref="SyncTypeTag"/>,
	///   in which case the tag becomes the name. If the same type is synchronized in
	///   two conflicting ways, <see cref="EndSubObject"/> throws
	///   <see cref="InvalidOperationException"/>.
	/// <para/>
	///   Since a schema saver has no data, it cannot see synchronizer behavior that
	///   depends on data, e.g. (1) fields that are synchronized conditionally are
	///   recorded only if the synchronizer's code path with default values reaches
	///   them, and (2) for polymorphic types, only the branch for the default type
	///   tag is recorded.
	/// </remarks>
	public partial struct SchemaWriter : ISyncManager
	{
		internal SchemaState _s;
		internal SchemaWriter(SchemaState s) => _s = s;

		public SyncMode Mode => SyncMode.Schema;
		public bool IsReading => true;
		public bool IsWriting => false;

		public bool SupportsReordering => true;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => false;
		public bool IsPlainText => true;
		public ISyncOptions? Options => _s._opt;

		public bool IsInsideList => _s.IsInsideList;

		public bool? ReachedEndOfList => _s.ReachedEndOfList;

		public int? MinimumListLength => _s.MinimumListLength;

		public int Depth => _s.Depth;

		public object CurrentObject { set { } } // implementation is not needed for a schema saver

		public bool SupportsNextField => false;

		public FieldId NextField => FieldId.Missing;

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => SyncType.Unknown;

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
		{
			return _s.BeginSubObject(name.Name, childKey, mode, listLength);
		}

		public void EndSubObject() => _s.EndSubObject();

		public string? SyncTypeTag(string? tag) => _s.SyncTypeTag(tag);

		/// <summary>Renders the recorded schema as a JSON Schema (draft 2020-12)
		///   document in UTF-8 format.</summary>
		public ReadOnlyMemory<byte> Finish()
		{
			var output = new ArrayBufferWriter<byte>(_s._opt.Write.InitialBufferSize);
			_s.Render(output);
			return output.WrittenMemory;
		}

		/// <summary>Renders the recorded schema as a JSON Schema (draft 2020-12) string.</summary>
		public override string ToString() => Utf8ToString(Finish());

		#region Primitive Sync methods

		static SchemaNode IntNode(BigInteger? min, BigInteger? max, bool nullable = false)
			=> new SchemaNode { Types = JsonSchemaType.Integer, Minimum = min, Maximum = max, Nullable = nullable };
		static SchemaNode Node(JsonSchemaType type, bool nullable = false)
			=> new SchemaNode { Types = type, Nullable = nullable };
		static SchemaNode CharNode(bool nullable = false)
			=> new SchemaNode { Types = JsonSchemaType.String, MinLength = 1, MaxLength = 1, Nullable = nullable };
		static SchemaNode BitfieldNode(int bits, bool signed)
		{
			if (bits <= 0 || bits > 256)
				return Node(JsonSchemaType.Integer);
			return signed
				? IntNode(-(BigInteger.One << (bits - 1)), (BigInteger.One << (bits - 1)) - 1)
				: IntNode(BigInteger.Zero, (BigInteger.One << bits) - 1);
		}

		public bool Sync(FieldId name, bool savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Boolean));
			return default;
		}
		public sbyte Sync(FieldId name, sbyte savable)
		{
			_s.SyncPrim(name.Name, IntNode(sbyte.MinValue, sbyte.MaxValue));
			return default;
		}
		public byte Sync(FieldId name, byte savable)
		{
			_s.SyncPrim(name.Name, IntNode(byte.MinValue, byte.MaxValue));
			return default;
		}
		public short Sync(FieldId name, short savable)
		{
			_s.SyncPrim(name.Name, IntNode(short.MinValue, short.MaxValue));
			return default;
		}
		public ushort Sync(FieldId name, ushort savable)
		{
			_s.SyncPrim(name.Name, IntNode(ushort.MinValue, ushort.MaxValue));
			return default;
		}
		public int Sync(FieldId name, int savable)
		{
			_s.SyncPrim(name.Name, IntNode(int.MinValue, int.MaxValue));
			return default;
		}
		public uint Sync(FieldId name, uint savable)
		{
			_s.SyncPrim(name.Name, IntNode(uint.MinValue, uint.MaxValue));
			return default;
		}
		public long Sync(FieldId name, long savable)
		{
			_s.SyncPrim(name.Name, IntNode(long.MinValue, long.MaxValue));
			return default;
		}
		public ulong Sync(FieldId name, ulong savable)
		{
			_s.SyncPrim(name.Name, IntNode(ulong.MinValue, ulong.MaxValue));
			return default;
		}
		public float Sync(FieldId name, float savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Number));
			return default;
		}
		public double Sync(FieldId name, double savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Number));
			return default;
		}
		public decimal Sync(FieldId name, decimal savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Number));
			return default;
		}
		public BigInteger Sync(FieldId name, BigInteger savable)
		{
			_s.SyncPrim(name.Name, IntNode(null, null));
			return default;
		}
		public char Sync(FieldId name, char savable)
		{
			_s.SyncPrim(name.Name, CharNode());
			return default;
		}

		public int Sync(FieldId name, int savable, int bits, bool signed = true)
		{
			_s.SyncPrim(name.Name, BitfieldNode(bits, signed));
			return default;
		}
		public long Sync(FieldId name, long savable, int bits, bool signed = true)
		{
			_s.SyncPrim(name.Name, BitfieldNode(bits, signed));
			return default;
		}
		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
		{
			_s.SyncPrim(name.Name, BitfieldNode(bits, signed));
			return default;
		}

		public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal)
		{
			_s.SyncPrimValue(name.Name, Node(JsonSchemaType.String), mode);
			return default;
		}

		public bool? Sync(FieldId name, bool? savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Boolean, nullable: true));
			return default;
		}
		public sbyte? Sync(FieldId name, sbyte? savable)
		{
			_s.SyncPrim(name.Name, IntNode(sbyte.MinValue, sbyte.MaxValue, nullable: true));
			return default;
		}
		public byte? Sync(FieldId name, byte? savable)
		{
			_s.SyncPrim(name.Name, IntNode(byte.MinValue, byte.MaxValue, nullable: true));
			return default;
		}
		public short? Sync(FieldId name, short? savable)
		{
			_s.SyncPrim(name.Name, IntNode(short.MinValue, short.MaxValue, nullable: true));
			return default;
		}
		public ushort? Sync(FieldId name, ushort? savable)
		{
			_s.SyncPrim(name.Name, IntNode(ushort.MinValue, ushort.MaxValue, nullable: true));
			return default;
		}
		public int? Sync(FieldId name, int? savable)
		{
			_s.SyncPrim(name.Name, IntNode(int.MinValue, int.MaxValue, nullable: true));
			return default;
		}
		public uint? Sync(FieldId name, uint? savable)
		{
			_s.SyncPrim(name.Name, IntNode(uint.MinValue, uint.MaxValue, nullable: true));
			return default;
		}
		public long? Sync(FieldId name, long? savable)
		{
			_s.SyncPrim(name.Name, IntNode(long.MinValue, long.MaxValue, nullable: true));
			return default;
		}
		public ulong? Sync(FieldId name, ulong? savable)
		{
			_s.SyncPrim(name.Name, IntNode(ulong.MinValue, ulong.MaxValue, nullable: true));
			return default;
		}
		public float? Sync(FieldId name, float? savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Number, nullable: true));
			return default;
		}
		public double? Sync(FieldId name, double? savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Number, nullable: true));
			return default;
		}
		public decimal? Sync(FieldId name, decimal? savable)
		{
			_s.SyncPrim(name.Name, Node(JsonSchemaType.Number, nullable: true));
			return default;
		}
		public BigInteger? Sync(FieldId name, BigInteger? savable)
		{
			_s.SyncPrim(name.Name, IntNode(null, null, nullable: true));
			return default;
		}
		public char? Sync(FieldId name, char? savable)
		{
			_s.SyncPrim(name.Name, CharNode(nullable: true));
			return default;
		}

		#endregion

		#region List implementations

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			_s.SyncPrimList(name.Name, Node(JsonSchemaType.Boolean), mode, tupleLength);
			return default;
		}

		public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			if (_s._opt.Write.CharListAsString ?? !_s._opt.NewtonsoftCompatibility) {
				// Character lists are written as strings
				var node = Node(JsonSchemaType.String);
				if ((mode & ObjectMode.Tuple) == ObjectMode.Tuple && tupleLength >= 0)
					node.MinLength = node.MaxLength = tupleLength;
				_s.SyncPrimValue(name.Name, node, mode);
			} else {
				// Character lists are written as arrays of single-character strings
				_s.SyncPrimList(name.Name, CharNode(), mode, tupleLength);
			}
			return default;
		}

		public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
		{
			// This condition mirrors SyncJson.Writer.SyncListByteImpl: Newtonsoft mode
			// writes byte *lists* (not byte arrays) as JSON arrays, and byte arrays as
			// Base64 strings; otherwise Options.ByteArrayMode decides.
			if (_s._opt.NewtonsoftCompatibility && typeof(Scanner) != typeof(InternalList.Scanner<byte>)
				|| _s._opt.ByteArrayMode == JsonByteArrayMode.Array) {
				_s.SyncPrimList(name.Name, IntNode(byte.MinValue, byte.MaxValue), mode, tupleLength);
			} else {
				var node = Node(JsonSchemaType.String);
				if (_s._opt.NewtonsoftCompatibility || _s._opt.ByteArrayMode == JsonByteArrayMode.Base64)
					node.ContentEncoding = "base64";
				_s.SyncPrimValue(name.Name, node, mode);
			}
			return default;
		}

		#endregion
	}
}
