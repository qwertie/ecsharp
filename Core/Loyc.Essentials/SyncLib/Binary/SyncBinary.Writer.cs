using Loyc.Collections;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	public static SyncBinary.Writer NewWriter(IBufferWriter<byte> output, Options? options = null)
		=> new Writer(new WriterState(output ?? new ArrayBufferWriter<byte>(), options ?? _defaultOptions));

	/// <summary>The default binary writer for binary data, which uses SyncLib 
	/// integer encoding. 
	/// 
	/// </summary>
	public struct Writer : ISyncManager
	{
		internal WriterState _s;
		internal Writer(WriterState s) => _s = s;

		public SyncMode Mode => SyncMode.Writing;
		public bool IsReading => false;
		public bool IsWriting => true;

		public bool SupportsReordering => true;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => false;

		public bool IsInsideList => _s._isInsideList;


		public bool? ReachedEndOfList => null;

		public int? MinimumListLength => null;

		public int Depth => throw new NotImplementedException();

		public object CurrentObject { set { } }

		public bool SupportsNextField => false;

		public FieldId NextField => FieldId.Missing;

		public (bool Begun, object Object) BeginSubObject(FieldId name, object childKey, ObjectMode mode, int listLength = -1)
		{
			throw new NotImplementedException();
		}

		public void EndSubObject()
		{
			throw new NotImplementedException();
		}

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown)
		{
			throw new NotImplementedException();
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

		public string Sync(FieldId name, string savable)
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

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			throw new NotImplementedException();
		}

		public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
		{
			throw new NotImplementedException();
		}

		public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			throw new NotImplementedException();
		}

		public string SyncTypeTag(string tag)
		{
			throw new NotImplementedException();
		}
	}
}
