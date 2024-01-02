using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using static Loyc.SyncLib.SyncJson.Parser;

namespace Loyc.SyncLib
{
	public partial class SyncJson
	{
		public static SyncJson.Reader NewReader(IScanner<byte> input, Options? options = null)
			=> new Reader(new ReaderState(input ?? throw new ArgumentNullException(nameof(input)), options ?? _defaultOptions));
		public static SyncJson.Reader NewReader(ReadOnlyMemory<byte> input, Options? options = null)
			=> new Reader(new ReaderState(input, options ?? _defaultOptions));
		public static SyncJson.Reader NewReader(string input, Options? options = null)
			=> NewReader(Encoding.UTF8.GetBytes(input), options);

		internal static T? Read<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<Reader, T> sync, Options? options = null)
		{
			options ??= _defaultOptions;
			Reader reader = NewReader(json, options);
			return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
		}
		internal static T? ReadI<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		{
			options ??= _defaultOptions;
			Reader reader = NewReader(json, options);
			return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
		}

		internal static T? Read<T>(string json, SyncObjectFunc<Reader, T> sync, Options? options = null)
			=> Read(Encoding.UTF8.GetBytes(json), sync, options);
		internal static T? ReadI<T>(string json, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
			=> ReadI(Encoding.UTF8.GetBytes(json), sync, options);

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
	
			public bool IsInsideList => _s.IsInsideList;

			public bool? ReachedEndOfList => _s.IsInsideList ? _s.ReachedEndOfList : null;

			public int? MinimumListLength => 0;

			public int Depth => _s.Depth;

			public object CurrentObject { set => _s.SetCurrentObject(value); }

			public bool SupportsNextField => true;

			public FieldId NextField => _s.NextField;

			public (bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
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

			// Note: these casts are inherently checked for overflow. Even if "unchecked" 
			//       were used here, they would still be checked because BigInteger's 
			//       conversion operators use checked conversion internally.

			public sbyte Sync(FieldId name, sbyte savable)
				=> checked((sbyte) (_s.ReadInteger(name.Name, false) ?? default));

			public byte Sync(FieldId name, byte savable)
				=> checked((byte) (_s.ReadInteger(name.Name, false) ?? default));

			public short Sync(FieldId name, short savable)
				=> checked((short) (_s.ReadInteger(name.Name, false) ?? default));

			public ushort Sync(FieldId name, ushort savable)
				=> checked((ushort) (_s.ReadInteger(name.Name, false) ?? default));

			public int Sync(FieldId name, int savable)
				=> checked((int) (_s.ReadInteger(name.Name, false) ?? default));

			public uint Sync(FieldId name, uint savable)
				=> checked((uint) (_s.ReadInteger(name.Name, false) ?? default));

			public long Sync(FieldId name, long savable)
				=> checked((long) (_s.ReadInteger(name.Name, false) ?? default));

			public ulong Sync(FieldId name, ulong savable)
				=> checked((ulong) (_s.ReadInteger(name.Name, false) ?? default));

			public float Sync(FieldId name, float savable)
				=> (float) (_s.ReadDouble(name.Name, false) ?? default);

			public double Sync(FieldId name, double savable)
				=> _s.ReadDouble(name.Name, false) ?? default;

			public decimal Sync(FieldId name, decimal savable)
				=> _s.ReadDecimal(name.Name, false) ?? default;

			public BigInteger Sync(FieldId name, BigInteger savable)
				=> _s.ReadInteger(name.Name, false) ?? default;

			public char Sync(FieldId name, char savable)
				=> _s.ReadChar(name.Name, false) ?? '\0';

			public int Sync(FieldId name, int savable, int bits, bool signed = true)
			{
				return (int) (_s.ReadInteger(name.Name, false) ?? default);
			}

			public long Sync(FieldId name, long savable, int bits, bool signed = true)
			{
				return (long) (_s.ReadInteger(name.Name, false) ?? default);
			}

			public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
			{
				return _s.ReadInteger(name.Name, false) ?? default;
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

			public sbyte? Sync(FieldId name, sbyte? savable) => checked((sbyte?) _s.ReadInteger(name, true));

			public byte? Sync(FieldId name, byte? savable) => checked((byte?) _s.ReadInteger(name, true));

			public short? Sync(FieldId name, short? savable) => checked((short?) _s.ReadInteger(name, true));

			public ushort? Sync(FieldId name, ushort? savable) => checked((ushort?) _s.ReadInteger(name, true));

			public int? Sync(FieldId name, int? savable) => checked((int?) _s.ReadInteger(name, true));

			public uint? Sync(FieldId name, uint? savable) => checked((uint?) _s.ReadInteger(name, true));

			public long? Sync(FieldId name, long? savable) => checked((long?) _s.ReadInteger(name, true));

			public ulong? Sync(FieldId name, ulong? savable) => checked((ulong?) _s.ReadInteger(name, true));

			public float? Sync(FieldId name, float? savable) => (float?) _s.ReadDouble(name, true);

			public double? Sync(FieldId name, double? savable) => _s.ReadDouble(name, true);

			public decimal? Sync(FieldId name, decimal? savable) => _s.ReadDecimal(name, true);

			public BigInteger? Sync(FieldId name, BigInteger? savable) => _s.ReadInteger(name, true);

			public char? Sync(FieldId name, char? savable) => _s.ReadChar(name, true);

			public string? Sync(FieldId name, string? savable) => _s.ReadString(name.Name);
		}
	}
}
