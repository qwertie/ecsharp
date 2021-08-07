using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib
{
	public partial class SyncJson
	{
		public static SyncJson.Reader NewReader(IScanner<byte> input, Options? options = null)
			=> new Reader(new ReaderState(input ?? throw new ArgumentNullException(nameof(input)), options ?? _defaultOptions));

		public partial struct Reader : ISyncManager
		{
			private ReaderState _s;

			internal Reader(ReaderState state) => _s = state;

			public SyncMode Mode => SyncMode.Loading;
			public bool IsSaving => false;
	
			public bool SupportsReordering => true;
			public bool SupportsDeduplication => true;
			public bool NeedsIntegerIds => false;
	
			public bool IsInsideList => throw new NotImplementedException();

			public bool? ReachedEndOfList => throw new NotImplementedException();

			public int? MinimumListLength => throw new NotImplementedException();

			public int Depth => throw new NotImplementedException();

			public object CurrentObject { set => throw new NotImplementedException(); }

			public (bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, SubObjectMode mode, int listLength = -1)
			{
				return _s.BeginSubObject(name != null ? name.Name : "", mode);
			}

			public void EndSubObject()
			{
				throw new NotImplementedException();
			}

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

			public bool Sync(FieldId name, bool savable)
			{
				throw new NotImplementedException();
			}

			public sbyte Sync(FieldId name, sbyte savable)
			{
				throw new NotImplementedException();
			}

			public byte Sync(FieldId name, byte savable)
			{
				throw new NotImplementedException();
			}

			public short Sync(FieldId name, short savable)
			{
				throw new NotImplementedException();
			}

			public ushort Sync(FieldId name, ushort savable)
			{
				throw new NotImplementedException();
			}

			public int Sync(FieldId name, int savable)
			{
				throw new NotImplementedException();
			}

			public uint Sync(FieldId name, uint savable)
			{
				throw new NotImplementedException();
			}

			public long Sync(FieldId name, long savable)
			{
				throw new NotImplementedException();
			}

			public ulong Sync(FieldId name, ulong savable)
			{
				throw new NotImplementedException();
			}

			public float Sync(FieldId name, float savable)
			{
				throw new NotImplementedException();
			}

			public double Sync(FieldId name, double savable)
			{
				throw new NotImplementedException();
			}

			public decimal Sync(FieldId name, decimal savable)
			{
				throw new NotImplementedException();
			}

			public BigInteger Sync(FieldId name, BigInteger savable)
			{
				throw new NotImplementedException();
			}

			public char Sync(FieldId name, char savable)
			{
				throw new NotImplementedException();
			}

			public int Sync(FieldId name, int savable, int bits, bool signed = true)
			{
				throw new NotImplementedException();
			}

			public long Sync(FieldId name, long savable, int bits, bool signed = true)
			{
				throw new NotImplementedException();
			}

			public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
			{
				throw new NotImplementedException();
			}

			public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<bool>
				where ListBuilder : IListBuilder<List, bool>
			{
				throw new NotImplementedException();
			}

			public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<byte>
				where ListBuilder : IListBuilder<List, byte>
			{
				throw new NotImplementedException();
			}

			public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<char>
				where ListBuilder : IListBuilder<List, char>
			{
				throw new NotImplementedException();
			}

			public bool? Sync(FieldId name, bool? savable)
			{
				throw new NotImplementedException();
			}

			public sbyte? Sync(FieldId name, sbyte? savable)
			{
				throw new NotImplementedException();
			}

			public byte? Sync(FieldId name, byte? savable)
			{
				throw new NotImplementedException();
			}

			public short? Sync(FieldId name, short? savable)
			{
				throw new NotImplementedException();
			}

			public ushort? Sync(FieldId name, ushort? savable)
			{
				throw new NotImplementedException();
			}

			public int? Sync(FieldId name, int? savable)
			{
				throw new NotImplementedException();
			}

			public uint? Sync(FieldId name, uint? savable)
			{
				throw new NotImplementedException();
			}

			public long? Sync(FieldId name, long? savable)
			{
				throw new NotImplementedException();
			}

			public ulong? Sync(FieldId name, ulong? savable)
			{
				throw new NotImplementedException();
			}

			public float? Sync(FieldId name, float? savable)
			{
				throw new NotImplementedException();
			}

			public double? Sync(FieldId name, double? savable)
			{
				throw new NotImplementedException();
			}

			public decimal? Sync(FieldId name, decimal? savable)
			{
				throw new NotImplementedException();
			}

			public BigInteger? Sync(FieldId name, BigInteger? savable)
			{
				throw new NotImplementedException();
			}

			public char? Sync(FieldId name, char? savable)
			{
				throw new NotImplementedException();
			}

			public string? Sync(FieldId name, string? savable)
			{
				throw new NotImplementedException();
			}
		}
	}
}
