using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Numerics;
using System.Text;
using static Loyc.SyncLib.SyncJson.Parser;

namespace Loyc.SyncLib;

public partial class SyncJson
{
	public static SyncJson.Reader NewReader(IScanner<byte> input, Options? options = null)
		=> new Reader(new ReaderState(input ?? throw new ArgumentNullException(nameof(input)), options ?? _defaultOptions));
	public static SyncJson.Reader NewReader(ReadOnlyMemory<byte> input, Options? options = null)
		=> new Reader(new ReaderState(input, options ?? _defaultOptions));
	public static SyncJson.Reader NewReader(string input, Options? options = null)
		=> NewReader(Encoding.UTF8.GetBytes(input), options);

	public static T? Read<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<Reader, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(json, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}
	public static T? ReadI<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(json, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}
	public static T? Read<T, SyncObject>(ReadOnlyMemory<byte> input, SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(input, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}

	public static T? Read<T>(string json, SyncObjectFunc<Reader, T> sync, Options? options = null)
		=> Read(Encoding.UTF8.GetBytes(json), sync, options);
	public static T? ReadI<T>(string json, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		=> ReadI(Encoding.UTF8.GetBytes(json), sync, options);
	public static T? Read<T, SyncObject>(string json, SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
		=> Read<T, SyncObject>(Encoding.UTF8.GetBytes(json), sync, options);

	/// <summary>
	///   An implementation of <see cref="ISyncManager"/> for reading JSON objects. 
	///   Designed to be both fast and flexible, this implementation normally reads 
	///   UTF8 directly into values without allocating intermediate strings, and
	///   without requiring the entire JSON file to be loaded into memory at once.
	///   <see cref="SupportsReordering"/>, <see cref="SupportsDeduplication"/> and
	///   <see cref="SupportsNextField"/> are all true, and non-strict JSON is 
	///   allowed (e.g. comments are accepted but ignored, unless you turn off 
	///   support in the <see cref="Options"/>.)
	/// </summary><remarks>
	///   For best performance, your synchronizers should read the JSON data in the 
	///   same order it was written. Synchronizers written in the usual way naturally 
	///   work this way. Out-of-order reads are supported but are slower and, when
	///   reading large JSON files, may use more memory.
	/// <para/>
	///   Since the JSON is always read in a single pass from an <see cref="IScanner{byte}"/>,
	///   trying to read a JSON property that doesn't exist can, in the worst case, 
	///   cause the whole file to be buffered into memory. However, JSON files that
	///   are (essentially) large arrays won't have this problem, since you can only 
	///   read arrays in order.
	/// <para/>
	///   This type can read JSON files larger than 2GB, provided that an out-of-
	///   order read doesn't cause over 2GB of data to be scanned at once.
	/// <para/>
	///   While normally you can read properties in any order, metadata properties 
	///   such as $id and $ref must be located at the beginning of a JSON object in
	///   order to be detected during deserialization. (Newtonsoft.Json has the same
	///   restriction, by the way.) In addition, object IDs must always be 
	///   represented by the same byte sequence, e.g. "3" and "\u0033" are not 
	///   treated as the same ID even though they both represent "3" in JSON.
	/// <para/>
	///   This is a struct rather than a class for performance reasons. Don't try to use
	///   a <c>default(Reader)</c>; it'll throw <see cref="NullReferenceException"/>. 
	/// </remarks>
	public partial struct Reader : ISyncManager
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

		public int? MinimumListLength => 0;

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

		// Note: integers are parsed as long/ulong for speed; BigInteger is only used
		//       when a value doesn't fit in 64 bits, in which case the behavior is
		//       controlled by Options.Read.HandleOverflow (which throws by default).
		//       If a value fits in 64 bits but not in the target field, these
		//       helpers also invoke HandleOverflow, then truncate its return value
		//       to the size of the target field.

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
			var type = _s.HasFieldCore(name.Name);
			if (type == JsonType.Null) {
				return default(List);
			} else if (type == JsonType.List) {
				return new ListLoader<SyncJson.Reader, List, char, ListBuilder, SyncPrimitive<SyncJson.Reader>>
					(new SyncPrimitive<SyncJson.Reader>(), builder, mode, tupleLength).Sync(ref this, name, saving);
			} else {
				var s = _s.ReadString(name.Name)!;

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
