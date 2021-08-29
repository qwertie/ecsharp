using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using static Loyc.SyncLib.SyncJson.ReaderState;

namespace Loyc.SyncLib
{
	public partial class SyncJson
	{
		public static SyncJson.Reader NewReader(IScanner<byte> input, Options? options = null)
			=> new Reader(new ReaderState(input ?? throw new ArgumentNullException(nameof(input)), options ?? _defaultOptions));

		internal static T? Read<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<Reader, T> sync, Options? options = null)
		{
			options ??= _defaultOptions;
			Reader reader = NewReader(new InternalList.Scanner<byte>(json), options);
			return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
		}
		internal static T? ReadI<T>(ReadOnlyMemory<byte> json, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		{
			options ??= _defaultOptions;
			Reader reader = NewReader(new InternalList.Scanner<byte>(json), options);
			return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
		}

		internal static T? Read<T>(string json, SyncObjectFunc<Reader, T> sync, Options? options = null)
			=> Read(Encoding.UTF8.GetBytes(json), sync, options);
		internal static T? ReadI<T>(string json, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
			=> ReadI(Encoding.UTF8.GetBytes(json), sync, options);

		public partial struct Reader : ISyncManager
		{
			private ReaderState _s;

			internal Reader(ReaderState state) => _s = state;

			public SyncMode Mode => SyncMode.Loading;
			public bool IsSaving => false;
	
			public bool SupportsReordering => true;
			public bool SupportsDeduplication => true;
			public bool NeedsIntegerIds => false;
	
			public bool IsInsideList => _s.IsInsideList;

			public bool? ReachedEndOfList => _s.ReachedEndOfList;

			public int? MinimumListLength => 0;

			public int Depth => _s.Depth;

			public object CurrentObject { set => _s.SetCurrentObject(value); }

			public (bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, SubObjectMode mode, int listLength = -1)
			{
				return _s.BeginSubObject(name.Name, mode);
			}

			public void EndSubObject() => _s.EndSubObject();

			public SyncType HasField(FieldId name, SyncType expectedType = SyncType.Unknown)
			{
				if (name == null)
					return SyncType.Unknown;

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

			public bool Sync(FieldId name, bool savable) => _s.ReadBoolean(name.Name, false)!.Value;

			public sbyte Sync(FieldId name, sbyte savable)
				=> checked((sbyte) _s.ReadInteger(name.Name, false)!.Value);

			public byte Sync(FieldId name, byte savable)
				=> checked((byte) _s.ReadInteger(name.Name, false)!.Value);

			public short Sync(FieldId name, short savable)
				=> checked((short) _s.ReadInteger(name.Name, false)!.Value);

			public ushort Sync(FieldId name, ushort savable)
				=> checked((ushort) _s.ReadInteger(name.Name, false)!.Value);

			public int Sync(FieldId name, int savable)
				=> checked((int) _s.ReadInteger(name.Name, false)!.Value);

			public uint Sync(FieldId name, uint savable)
				=> checked((uint) _s.ReadInteger(name.Name, false)!.Value);

			public long Sync(FieldId name, long savable)
				=> checked((long) _s.ReadInteger(name.Name, false)!.Value);

			public ulong Sync(FieldId name, ulong savable)
				=> checked((ulong) _s.ReadInteger(name.Name, false)!.Value);

			public float Sync(FieldId name, float savable)
				=> (float) _s.ReadDouble(name.Name, false)!.Value;

			public double Sync(FieldId name, double savable)
				=> _s.ReadDouble(name.Name, false)!.Value;

			public decimal Sync(FieldId name, decimal savable)
				=> _s.ReadDecimal(name.Name, false)!.Value;

			public BigInteger Sync(FieldId name, BigInteger savable)
				=> _s.ReadInteger(name.Name, false)!.Value;

			public char Sync(FieldId name, char savable)
				=> _s.ReadChar(name.Name, false)!.Value;

			public int Sync(FieldId name, int savable, int bits, bool signed = true)
			{
				return (int) _s.ReadInteger(name.Name, false)!.Value;
			}

			public long Sync(FieldId name, long savable, int bits, bool signed = true)
			{
				return (long) _s.ReadInteger(name.Name, false)!.Value;
			}

			public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
			{
				return _s.ReadInteger(name.Name, false)!.Value;
			}

			public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<bool>
				where ListBuilder : IListBuilder<List, bool>
			{
				var loader = new ListLoader<Reader, List, bool, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode, tupleLength);
				return loader.Sync(ref this, name, saving);
			}

			public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<byte>
				where ListBuilder : IListBuilder<List, byte>
				=> _s.ReadByteArray<ListBuilder, List>(name, builder, mode);

			public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
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
