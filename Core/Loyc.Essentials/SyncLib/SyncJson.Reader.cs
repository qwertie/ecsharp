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

			public (bool Begun, object? Object) BeginSubObject(Symbol? name, object? childKey, SubObjectMode mode, int listLength = -1)
			{
				return _s.BeginSubObject(name != null ? name.Name : "", mode);
			}

			public void EndSubObject()
			{
				throw new NotImplementedException();
			}

			public bool? HasField(Symbol name)
			{
				throw new NotImplementedException();
			}

			public bool Sync(Symbol? name, bool savable)
			{
				throw new NotImplementedException();
			}

			public sbyte Sync(Symbol? name, sbyte savable)
			{
				throw new NotImplementedException();
			}

			public byte Sync(Symbol? name, byte savable)
			{
				throw new NotImplementedException();
			}

			public short Sync(Symbol? name, short savable)
			{
				throw new NotImplementedException();
			}

			public ushort Sync(Symbol? name, ushort savable)
			{
				throw new NotImplementedException();
			}

			public int Sync(Symbol? name, int savable)
			{
				throw new NotImplementedException();
			}

			public uint Sync(Symbol? name, uint savable)
			{
				throw new NotImplementedException();
			}

			public long Sync(Symbol? name, long savable)
			{
				throw new NotImplementedException();
			}

			public ulong Sync(Symbol? name, ulong savable)
			{
				throw new NotImplementedException();
			}

			public float Sync(Symbol? name, float savable)
			{
				throw new NotImplementedException();
			}

			public double Sync(Symbol? name, double savable)
			{
				throw new NotImplementedException();
			}

			public decimal Sync(Symbol? name, decimal savable)
			{
				throw new NotImplementedException();
			}

			public BigInteger Sync(Symbol? name, BigInteger savable)
			{
				throw new NotImplementedException();
			}

			public char Sync(Symbol? name, char savable)
			{
				throw new NotImplementedException();
			}

			public string Sync(Symbol? name, string savable)
			{
				throw new NotImplementedException();
			}

			public int Sync(Symbol? name, int savable, int bits, bool signed = true)
			{
				throw new NotImplementedException();
			}

			public long Sync(Symbol? name, long savable, int bits, bool signed = true)
			{
				throw new NotImplementedException();
			}

			public BigInteger Sync(Symbol? name, BigInteger savable, int bits, bool signed = true)
			{
				throw new NotImplementedException();
			}

			public List? SyncListBoolImpl<Scanner, List, ListBuilder>(Symbol? name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<bool>
				where ListBuilder : IListBuilder<List, bool>
			{
				throw new NotImplementedException();
			}

			public List? SyncListByteImpl<Scanner, List, ListBuilder>(Symbol? name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<byte>
				where ListBuilder : IListBuilder<List, byte>
			{
				throw new NotImplementedException();
			}

			public List? SyncListCharImpl<Scanner, List, ListBuilder>(Symbol? name, Scanner scanner, List? saving, ListBuilder builder, SubObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<char>
				where ListBuilder : IListBuilder<List, char>
			{
				throw new NotImplementedException();
			}

			public bool? SyncNullable(Symbol? name, bool? savable)
			{
				throw new NotImplementedException();
			}

			public sbyte? SyncNullable(Symbol? name, sbyte? savable)
			{
				throw new NotImplementedException();
			}

			public byte? SyncNullable(Symbol? name, byte? savable)
			{
				throw new NotImplementedException();
			}

			public short? SyncNullable(Symbol? name, short? savable)
			{
				throw new NotImplementedException();
			}

			public ushort? SyncNullable(Symbol? name, ushort? savable)
			{
				throw new NotImplementedException();
			}

			public int? SyncNullable(Symbol? name, int? savable)
			{
				throw new NotImplementedException();
			}

			public uint? SyncNullable(Symbol? name, uint? savable)
			{
				throw new NotImplementedException();
			}

			public long? SyncNullable(Symbol? name, long? savable)
			{
				throw new NotImplementedException();
			}

			public ulong? SyncNullable(Symbol? name, ulong? savable)
			{
				throw new NotImplementedException();
			}

			public float? SyncNullable(Symbol? name, float? savable)
			{
				throw new NotImplementedException();
			}

			public double? SyncNullable(Symbol? name, double? savable)
			{
				throw new NotImplementedException();
			}

			public decimal? SyncNullable(Symbol? name, decimal? savable)
			{
				throw new NotImplementedException();
			}

			public BigInteger? SyncNullable(Symbol? name, BigInteger? savable)
			{
				throw new NotImplementedException();
			}

			public char? SyncNullable(Symbol? name, char? savable)
			{
				throw new NotImplementedException();
			}

			public string? SyncNullable(Symbol? name, string? savable)
			{
				throw new NotImplementedException();
			}
		}
	}
}
