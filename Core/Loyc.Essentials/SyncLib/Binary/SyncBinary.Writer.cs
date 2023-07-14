using Loyc.Collections;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	/// <summary>
	///   The <see cref="ISyncManager"/> implementation for writing <see cref="SyncBinary"/>'s 
	///   data format. Please call <see cref="WriteI"/> or <see cref="Write"/> if you just 
	///   want to serialize something, or <see cref="NewWriter"/> to create one of these.
	/// </summary>
	public struct Writer : ISyncManager
	{
		internal WriterState _s;
		internal Writer(WriterState s) => _s = s;

		public SyncMode Mode => SyncMode.Writing;
		public bool IsReading => false;
		public bool IsWriting => true;

		public bool SupportsReordering => false;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => false;

		public bool IsInsideList => _s.IsInsideList;

		public bool? ReachedEndOfList => null;

		public int? MinimumListLength => null;

		public int Depth => _s.Depth;

		public object CurrentObject { set { } } // implementation is not needed for a writer

		public bool SupportsNextField => false;

		public FieldId NextField => FieldId.Missing;

		public (bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
		{
			return _s.BeginSubObject(childKey, mode, listLength);
		}

		public void EndSubObject()
		{
			_s.EndSubObject();
		}

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => SyncType.Unknown;

		public string? SyncTypeTag(string? tag) { _s.WriteTypeTag(tag); return tag; }

		public bool   Sync(FieldId name, bool savable) { _s.Write(savable); return savable; }

		public sbyte  Sync(FieldId name, sbyte savable) { _s.Write(savable); return savable; }

		public byte   Sync(FieldId name, byte savable) { _s.Write((uint) savable); return savable; }

		public short  Sync(FieldId name, short savable) { _s.Write(savable); return savable; }

		public ushort Sync(FieldId name, ushort savable) { _s.Write((uint) savable); return savable; }

		public int    Sync(FieldId name, int savable) { _s.Write(savable); return savable; }

		public uint   Sync(FieldId name, uint savable) { _s.Write(savable); return savable; }

		public long   Sync(FieldId name, long savable) { _s.Write(savable); return savable; }

		public ulong  Sync(FieldId name, ulong savable) { _s.Write(savable); return savable; }

		public float  Sync(FieldId name, float savable) { _s.Write(savable); return savable; }

		public double Sync(FieldId name, double savable) { _s.Write(savable); return savable; }

		public decimal Sync(FieldId name, decimal savable) { _s.Write(savable); return savable; }

		public BigInteger Sync(FieldId name, BigInteger savable) { _s.Write(savable); return savable; }

		public char   Sync(FieldId name, char savable) { _s.Write((uint)savable); return savable; }

		public string? Sync(FieldId name, string? savable) { _s.Write(savable); return savable; }

		public int Sync(FieldId name, int savable, int bits, bool signed = true)
		{
			_s.WriteBitfield((uint)savable, (uint)bits);
			return savable;
		}

		public long Sync(FieldId name, long savable, int bits, bool signed = true)
		{
			_s.WriteBitfield((ulong)savable, (uint)bits);
			return savable;
		}

		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
		{
			_s.WriteBitfield(savable, (uint)bits);
			return savable;
		}

		public bool?   Sync(FieldId name, bool? savable) { _s.WriteNullable(savable); return savable; }

		public sbyte?  Sync(FieldId name, sbyte? savable) { _s.WriteNullable(savable); return savable; }

		public byte?   Sync(FieldId name, byte? savable) { _s.WriteNullable(savable); return savable; }

		public short?  Sync(FieldId name, short? savable) { _s.WriteNullable(savable); return savable; }

		public ushort? Sync(FieldId name, ushort? savable) { _s.WriteNullable(savable); return savable; }

		public int?    Sync(FieldId name, int? savable) { _s.WriteNullable(savable); return savable; }

		public uint?   Sync(FieldId name, uint? savable) { _s.WriteNullable(savable); return savable; }

		public long?   Sync(FieldId name, long? savable) { _s.WriteNullable(savable); return savable; }

		public ulong?  Sync(FieldId name, ulong? savable) { _s.WriteNullable(savable); return savable; }

		public float?  Sync(FieldId name, float? savable) { _s.WriteNullable(savable); return savable; }

		public double? Sync(FieldId name, double? savable) { _s.WriteNullable(savable); return savable; }

		public decimal? Sync(FieldId name, decimal? savable) { _s.WriteNullable(savable); return savable; }

		public BigInteger? Sync(FieldId name, BigInteger? savable) { _s.WriteNullable(savable); return savable; }

		public char? Sync(FieldId name, char? savable)
		{
			if (savable == null)
				_s.WriteNull();
			else
				_s.Write((ushort) savable.Value);
			return savable;
		}

		static bool MayBeNullable(ObjectMode mode)
			=> (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			if (MayBeNullable(mode) && saving == null) {
				var status = BeginSubObject(name, null, mode, 0);
				Debug.Assert(!status.Begun && status.Object == null);
				return default;
			} else {
				var saver = new ScannerSaver<SyncBinary.Writer, Scanner, bool, SyncPrimitive<SyncBinary.Writer>>(new SyncPrimitive<SyncBinary.Writer>(), mode);
				saver.Write(ref this, name, scanner!, saving, tupleLength);
				return saving;
			}
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

		public IBufferWriter<byte> Flush()
		{
			_s.Flush();
			return _s._output;
		}
	}
}
