// JsonDocument is unavailable in .NET Standard 2.x and .NET Framework (and this
// library avoids taking a dependency on the System.Text.Json package there)
#if NETCOREAPP3_0_OR_GREATER

using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Numerics;
using System.Text.Json;

namespace Loyc.SyncLib;

/// <summary>
///   Contains an implementation of <see cref="ISyncManager"/> for reading JSON data
///   from a <see cref="System.Text.Json.JsonDocument"/> or <see cref="JsonElement"/>
///   (<see cref="SyncJsonDOM.Reader"/>), plus convenience methods for reading objects
///   from such a DOM (the Read and ReadI methods).
/// </summary><remarks>
///   <see cref="SyncJson.Reader"/> parses raw UTF-8 bytes itself, which is faster and
///   uses less memory, so prefer it when your input is text or bytes. This class is
///   for situations where (part of) a document has already been parsed by
///   System.Text.Json — for example, when a web framework hands your code a
///   <see cref="JsonElement"/>, or when your code must inspect a JSON document to
///   decide how to deserialize it. It understands the same JSON conventions as
///   <see cref="SyncJson"/> (including <see cref="SyncJson.Options.NameConverter"/>,
///   deduplication and cyclic object graphs via "$id"/"$ref", and byte arrays as
///   Base64 or BAIS), so JSON written by <see cref="SyncJson.Writer"/> can be read
///   with this class.
/// <para/>
///   Behavioral differences from <see cref="SyncJson.Reader"/>:
///   <ul>
///   <li>Syntax rules are System.Text.Json's, and syntax/depth errors are detected
///     when the document is parsed, not while reading it. Consequently, the options
///     <c>Read.Strict</c>, <c>Read.AllowComments</c>, <c>Read.MaxDepth</c> and
///     <c>Read.VerifyEof</c> take effect only in the convenience methods of this
///     class that parse text, which report syntax errors by throwing
///     <see cref="System.Text.Json.JsonException"/>.</li>
///   <li>Out-of-order reads are cheap, and a "$ref" backreference can be resolved
///     even if the object with the matching "$id" hasn't been visited yet, no matter
///     where it is in the document (the stream-based reader can only resolve ids
///     it has already scanned past).</li>
///   <li>Since JSON property names are unescaped during parsing, names that are
///     escaped differently but decode to the same string are identical here
///     (e.g. the object ids "3" and "3" are the same id).</li>
///   </ul>
///   This class is only available in builds of Loyc.SyncLib.SyncJson for
///   .NET Core 3.0+ (it is absent from the .NET Standard 2.x builds, because
///   JsonDocument itself is unavailable there).
/// </remarks>
public static partial class SyncJsonDOM
{
	static SyncJson.Options _defaultOptions = new SyncJson.Options();

	public static Reader NewReader(JsonDocument document, SyncJson.Options? options = null)
		=> NewReader((document ?? throw new ArgumentNullException(nameof(document))).RootElement, options);
	public static Reader NewReader(JsonElement element, SyncJson.Options? options = null)
		=> new Reader(new ReaderState(element, options ?? _defaultOptions));

	public static T? Read<T>(JsonDocument json, SyncObjectFunc<Reader, T> sync, SyncJson.Options? options = null)
		=> Read((json ?? throw new ArgumentNullException(nameof(json))).RootElement, sync, options);
	public static T? Read<T>(JsonElement json, SyncObjectFunc<Reader, T> sync, SyncJson.Options? options = null)
	{
		options ??= _defaultOptions;
		return SyncManagerExt.Sync(NewReader(json, options), null, default(T), sync, options.RootMode);
	}

	public static T? ReadI<T>(JsonDocument json, SyncObjectFunc<ISyncManager, T> sync, SyncJson.Options? options = null)
		=> ReadI((json ?? throw new ArgumentNullException(nameof(json))).RootElement, sync, options);
	public static T? ReadI<T>(JsonElement json, SyncObjectFunc<ISyncManager, T> sync, SyncJson.Options? options = null)
	{
		options ??= _defaultOptions;
		return SyncManagerExt.Sync(NewReader(json, options), null, default(T), sync, options.RootMode);
	}

	public static T? Read<T, SyncObject>(JsonDocument json, SyncObject sync, SyncJson.Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
		=> Read<T, SyncObject>((json ?? throw new ArgumentNullException(nameof(json))).RootElement, sync, options);
	public static T? Read<T, SyncObject>(JsonElement json, SyncObject sync, SyncJson.Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
	{
		options ??= _defaultOptions;
		return SyncManagerExt.Sync(NewReader(json, options), null, default(T), sync, options.RootMode);
	}

	// Convenience methods that parse text with JsonDocument.Parse and then read it.
	// (If you're not planning to use the DOM for anything else, SyncJson.Read reads
	// the same data faster.)

	public static T? Read<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<Reader, T> sync, SyncJson.Options? options = null)
	{
		options ??= _defaultOptions;
		using var doc = JsonDocument.Parse(json, GetDocumentOptions(options));
		return Read(doc.RootElement, sync, options);
	}
	public static T? Read<T>(string json, SyncObjectFunc<Reader, T> sync, SyncJson.Options? options = null)
	{
		options ??= _defaultOptions;
		using var doc = JsonDocument.Parse(json, GetDocumentOptions(options));
		return Read(doc.RootElement, sync, options);
	}
	public static T? ReadI<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<ISyncManager, T> sync, SyncJson.Options? options = null)
	{
		options ??= _defaultOptions;
		using var doc = JsonDocument.Parse(json, GetDocumentOptions(options));
		return ReadI(doc.RootElement, sync, options);
	}
	public static T? ReadI<T>(string json, SyncObjectFunc<ISyncManager, T> sync, SyncJson.Options? options = null)
	{
		options ??= _defaultOptions;
		using var doc = JsonDocument.Parse(json, GetDocumentOptions(options));
		return ReadI(doc.RootElement, sync, options);
	}
	public static T? Read<T, SyncObject>(ReadOnlyMemory<byte> json, SyncObject sync, SyncJson.Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
	{
		options ??= _defaultOptions;
		using var doc = JsonDocument.Parse(json, GetDocumentOptions(options));
		return Read<T, SyncObject>(doc.RootElement, sync, options);
	}

	/// <summary>Derives <see cref="JsonDocumentOptions"/> from the reader options, for
	///   use with JsonDocument.Parse (comments are skipped if Read.AllowComments, and
	///   trailing commas are allowed unless Read.Strict).</summary>
	public static JsonDocumentOptions GetDocumentOptions(SyncJson.Options options) => new JsonDocumentOptions {
		CommentHandling = options.Read.AllowComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
		AllowTrailingCommas = !options.Read.Strict,
		MaxDepth = options.Read.MaxDepth,
	};

	/// <summary>
	///   An implementation of <see cref="ISyncManager"/> that reads JSON objects from
	///   a <see cref="JsonElement"/> tree (see <see cref="SyncJsonDOM"/> for details).
	///   <see cref="SupportsReordering"/>, <see cref="SupportsDeduplication"/> and
	///   <see cref="SupportsNextField"/> are all true. Because the data was parsed in
	///   advance, out-of-order reads are cheap: no buffering, rewinding or replaying
	///   is needed, and reading a JSON property that doesn't exist is harmless.
	/// </summary><remarks>
	///   As with <see cref="SyncJson.Reader"/>, metadata properties such as $id and
	///   $ref must be located at the beginning of a JSON object in order to be
	///   detected during deserialization.
	/// <para/>
	///   This is a struct rather than a class for performance reasons. Don't try to use
	///   a <c>default(Reader)</c>; it'll throw <see cref="NullReferenceException"/>.
	/// </remarks>
	public struct Reader : ISyncManager
	{
		private ReaderState _s;

		internal Reader(ReaderState state) => _s = state;

		public SyncMode Mode => SyncMode.Reading;
		public bool IsReading => true;
		public bool IsWriting => false;

		public bool SupportsReordering => true;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => false;
		public bool IsPlainText => true;
		public ISyncOptions? Options => _s._opt;

		public bool IsInsideList => _s.IsInsideList;

		public bool? ReachedEndOfList => _s.IsInsideList ? _s.ReachedEndOfList : null;

		public int? MinimumListLength => _s.ListLengthRemaining;

		public int Depth => _s.Depth;

		public object CurrentObject { set => _s.SetCurrentObject(value); }

		public bool SupportsNextField => true;

		public FieldId NextField => _s.NextField;

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
		{
			return _s.BeginSubObject(name.Name, mode);
		}

		public void EndSubObject() => _s.EndSubObject();

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown)
		{
			var type = _s.HasField(name.Name);

			// Check whether the type matches the expected type
			if (expectedType <= SyncType.Exists || expectedType == type ||
				type == SyncType.String && expectedType == SyncType.ByteList || // Can convert string => byte[]
				type ==	SyncType.List && (expectedType & SyncType.List) != 0 || // Assume any list matches any specific list type
				type == SyncType.Null && (expectedType & (SyncType.Null | SyncType.List)) != 0) // Null matches all nullables
				return type;

			// Check if there's an implicit type conversion to expectedType
			var expectedPrim = expectedType & ~SyncType.Null;
			if ((expectedPrim & SyncType.List) == 0 &&
				type >= SyncType.Boolean && type <= SyncType.Float &&
				expectedPrim >= type)
				return type;

			return SyncType.Missing;
		}

		public string? SyncTypeTag(string? tag) => _s.ReadTypeTag();

		public bool Sync(FieldId name, bool savable) => _s.ReadBoolean(name.Name, false) ?? false;

		// Note: if a value fits in 64 bits but not in the target field, these
		//       helpers invoke Options.Read.HandleOverflow (which throws by
		//       default), then truncate its return value to the target size.

		private long ReadSigned(string? name, long min, long max)
		{
			long num = _s.ReadInt64(name, false) ?? default;
			if (num < min || num > max)
				num = unchecked((long) _s._optRead.HandleOverflow(name, num, true));
			return num;
		}

		private long? ReadSignedOrNull(string? name, long min, long max)
		{
			long? num = _s.ReadInt64(name, true);
			if (num.HasValue && (num.Value < min || num.Value > max))
				return unchecked((long) _s._optRead.HandleOverflow(name, num.Value, true));
			return num;
		}

		private ulong ReadUnsigned(string? name, ulong max)
		{
			ulong num = _s.ReadUInt64(name, false) ?? default;
			if (num > max)
				num = _s._optRead.HandleOverflow(name, num, false);
			return num;
		}

		private ulong? ReadUnsignedOrNull(string? name, ulong max)
		{
			ulong? num = _s.ReadUInt64(name, true);
			if (num.HasValue && num.Value > max)
				return _s._optRead.HandleOverflow(name, num.Value, false);
			return num;
		}

		public sbyte Sync(FieldId name, sbyte savable)
			=> unchecked((sbyte) ReadSigned(name.Name, sbyte.MinValue, sbyte.MaxValue));

		public byte Sync(FieldId name, byte savable)
			=> unchecked((byte) ReadUnsigned(name.Name, byte.MaxValue));

		public short Sync(FieldId name, short savable)
			=> unchecked((short) ReadSigned(name.Name, short.MinValue, short.MaxValue));

		public ushort Sync(FieldId name, ushort savable)
			=> unchecked((ushort) ReadUnsigned(name.Name, ushort.MaxValue));

		public int Sync(FieldId name, int savable)
			=> unchecked((int) ReadSigned(name.Name, int.MinValue, int.MaxValue));

		public uint Sync(FieldId name, uint savable)
			=> unchecked((uint) ReadUnsigned(name.Name, uint.MaxValue));

		public long Sync(FieldId name, long savable)
			=> _s.ReadInt64(name.Name, false) ?? default;

		public ulong Sync(FieldId name, ulong savable)
			=> _s.ReadUInt64(name.Name, false) ?? default;

		public float Sync(FieldId name, float savable)
			=> (float) (_s.ReadDouble(name.Name, false) ?? default);

		public double Sync(FieldId name, double savable)
			=> _s.ReadDouble(name.Name, false) ?? default;

		public decimal Sync(FieldId name, decimal savable)
			=> _s.ReadDecimal(name.Name, false) ?? default;

		public BigInteger Sync(FieldId name, BigInteger savable)
			=> _s.ReadBigInt(name.Name, false) ?? default;

		public char Sync(FieldId name, char savable)
			=> _s.ReadChar(name.Name, false) ?? '\0';

		public int Sync(FieldId name, int savable, int bits, bool signed = true)
		{
			return unchecked((int) ReadSigned(name.Name, int.MinValue, int.MaxValue));
		}

		public long Sync(FieldId name, long savable, int bits, bool signed = true)
		{
			return _s.ReadInt64(name.Name, false) ?? default;
		}

		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
		{
			return _s.ReadBigInt(name.Name, false) ?? default;
		}

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			var loader = new ListLoader<Reader, List, bool, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode, tupleLength);
			return loader.Sync(ref this, name, saving);
		}

		public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
			=> _s.ReadByteArray<ListBuilder, List>(name, builder, mode);

		public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			var type = _s.HasField(name.Name);
			if (type == SyncType.Null) {
				_s.ConsumeNullListItem(name.Name);
				return default(List);
			} else if (type == SyncType.List) {
				return new ListLoader<Reader, List, char, ListBuilder, SyncPrimitive<Reader>>
					(new SyncPrimitive<Reader>(), builder, mode, tupleLength).Sync(ref this, name, saving);
			} else {
				var s = _s.ReadString(name.Name);
				if (s == null)
					return default(List); // missing (and Options.Read.AllowMissingFields is on)

				builder.Alloc(s.Length);
				for (int i = 0; i < s.Length; i++)
					builder.Add(s[i]);
				return builder.List;
			}
		}

		public bool? Sync(FieldId name, bool? savable) => _s.ReadBoolean(name, true);

		public sbyte? Sync(FieldId name, sbyte? savable) => unchecked((sbyte?) ReadSignedOrNull(name, sbyte.MinValue, sbyte.MaxValue));

		public byte? Sync(FieldId name, byte? savable) => unchecked((byte?) ReadUnsignedOrNull(name, byte.MaxValue));

		public short? Sync(FieldId name, short? savable) => unchecked((short?) ReadSignedOrNull(name, short.MinValue, short.MaxValue));

		public ushort? Sync(FieldId name, ushort? savable) => unchecked((ushort?) ReadUnsignedOrNull(name, ushort.MaxValue));

		public int? Sync(FieldId name, int? savable) => unchecked((int?) ReadSignedOrNull(name, int.MinValue, int.MaxValue));

		public uint? Sync(FieldId name, uint? savable) => unchecked((uint?) ReadUnsignedOrNull(name, uint.MaxValue));

		public long? Sync(FieldId name, long? savable) => _s.ReadInt64(name, true);

		public ulong? Sync(FieldId name, ulong? savable) => _s.ReadUInt64(name, true);

		public float? Sync(FieldId name, float? savable) => (float?) _s.ReadDouble(name, true);

		public double? Sync(FieldId name, double? savable) => _s.ReadDouble(name, true);

		public decimal? Sync(FieldId name, decimal? savable) => _s.ReadDecimal(name, true);

		public BigInteger? Sync(FieldId name, BigInteger? savable) => _s.ReadBigInt(name, true);

		public char? Sync(FieldId name, char? savable) => _s.ReadChar(name, true);

		public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal) => _s.ReadString(name.Name);
	}
}

#endif
