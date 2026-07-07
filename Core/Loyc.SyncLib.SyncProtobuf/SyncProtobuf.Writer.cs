using Loyc.Collections;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Numerics;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	public static ReadOnlyMemory<byte> Write<T>(T value, SyncObjectFunc<Writer, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		var output = new ArrayBufferWriter<byte>(options.Write.InitialBufferSize);
		Writer writer = NewWriter(output, options);
		SyncManagerExt.Sync(writer, null, value, sync, options.RootMode);
		writer._s.Flush();
		return output.WrittenMemory;
	}
	public static ReadOnlyMemory<byte> WriteI<T>(T value, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		var output = new ArrayBufferWriter<byte>(options.Write.InitialBufferSize);
		Writer writer = NewWriter(output, options);
		SyncManagerExt.Sync(writer, null, value, sync, options.RootMode);
		writer._s.Flush();
		return output.WrittenMemory;
	}
	public static ReadOnlyMemory<byte> Write<T, SyncObject>(T value, SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<SyncProtobuf.Writer, T>
	{
		options ??= _defaultOptions;
		var output = new ArrayBufferWriter<byte>(options.Write.InitialBufferSize);
		Writer writer = NewWriter(output, options);
		SyncManagerExt.Sync(writer, null, value, sync, options.RootMode);
		writer._s.Flush();
		return output.WrittenMemory;
	}

	public static SyncProtobuf.Writer NewWriter(IBufferWriter<byte>? output = null, Options? options = null)
		=> new Writer(new WriterState(output ?? new ArrayBufferWriter<byte>(), options ?? _defaultOptions));

	/// <summary>
	///   The <see cref="ISyncManager"/> implementation that writes the Protocol Buffers
	///   wire format (see <see cref="SyncProtobuf"/> for the format and guidelines). Call
	///   <see cref="Write{T}(T, SyncObjectFunc{Writer, T}, Options?)"/> to serialize an
	///   object, or <see cref="NewWriter"/> to construct one of these directly.
	/// </summary><remarks>
	///   This is a struct (for performance) that wraps a mutable state class. Do not use
	///   <c>default(Writer)</c>; it will throw <see cref="NullReferenceException"/>.
	/// </remarks>
	public struct Writer : ISyncManager
	{
		internal WriterState _s;
		internal Writer(WriterState s) => _s = s;

		public SyncMode Mode => SyncMode.Writing;
		public bool IsReading => false;
		public bool IsWriting => true;

		public bool SupportsReordering => true;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => true;
		public bool SupportsNextField => false;

		public bool IsInsideList => _s.IsInsideList;
		public bool? ReachedEndOfList => null;
		public int? MinimumListLength => null;
		public int Depth => _s.Depth;

		public FieldId NextField => FieldId.Missing;
		public object CurrentObject { set { } } // not needed for a writer (dedup uses references)

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => SyncType.Unknown;

		public string? SyncTypeTag(string? tag) { _s.WriteTypeTag(tag); return tag; }

		public bool   Sync(FieldId name, bool savable)   { _s.WriteUIntField(name, savable ? 1u : 0u); return savable; }
		public sbyte  Sync(FieldId name, sbyte savable)  { _s.WriteIntField(name, savable); return savable; }
		public byte   Sync(FieldId name, byte savable)   { _s.WriteUIntField(name, savable); return savable; }
		public short  Sync(FieldId name, short savable)  { _s.WriteIntField(name, savable); return savable; }
		public ushort Sync(FieldId name, ushort savable) { _s.WriteUIntField(name, savable); return savable; }
		public int    Sync(FieldId name, int savable)    { _s.WriteIntField(name, savable); return savable; }
		public uint   Sync(FieldId name, uint savable)   { _s.WriteUIntField(name, savable); return savable; }
		public long   Sync(FieldId name, long savable)   { _s.WriteIntField(name, savable); return savable; }
		public ulong  Sync(FieldId name, ulong savable)  { _s.WriteUIntField(name, savable); return savable; }
		public float  Sync(FieldId name, float savable)  { _s.WriteFloatField(name, savable); return savable; }
		public double Sync(FieldId name, double savable) { _s.WriteDoubleField(name, savable); return savable; }
		public decimal Sync(FieldId name, decimal savable) { _s.WriteDecimalField(name, savable); return savable; }
		public BigInteger Sync(FieldId name, BigInteger savable) { _s.WriteBigIntField(name, savable); return savable; }
		public char   Sync(FieldId name, char savable)   { _s.WriteUIntField(name, (ushort)savable); return savable; }

		public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal)
		{
			_s.WriteStringField(name, savable);
			return savable;
		}

		public int Sync(FieldId name, int savable, int bits, bool signed = true)
		{
			// Bitfields aren't a Protobuf concept; store as an ordinary varint.
			_s.WriteIntField(name, savable);
			return savable;
		}
		public long Sync(FieldId name, long savable, int bits, bool signed = true)
		{
			_s.WriteIntField(name, savable);
			return savable;
		}
		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
		{
			_s.WriteBigIntField(name, savable);
			return savable;
		}

		public bool?   Sync(FieldId name, bool? savable)   { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : (savable.Value ? 1u : 0u)); return savable; }
		public sbyte?  Sync(FieldId name, sbyte? savable)  { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : unchecked((ulong)(long)savable.Value)); return savable; }
		public byte?   Sync(FieldId name, byte? savable)   { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : savable.Value); return savable; }
		public short?  Sync(FieldId name, short? savable)  { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : unchecked((ulong)(long)savable.Value)); return savable; }
		public ushort? Sync(FieldId name, ushort? savable) { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : savable.Value); return savable; }
		public int?    Sync(FieldId name, int? savable)    { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : unchecked((ulong)(long)savable.Value)); return savable; }
		public uint?   Sync(FieldId name, uint? savable)   { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : savable.Value); return savable; }
		public long?   Sync(FieldId name, long? savable)   { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : unchecked((ulong)savable.Value)); return savable; }
		public ulong?  Sync(FieldId name, ulong? savable)  { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : savable.Value); return savable; }
		public float?  Sync(FieldId name, float? savable)  { _s.WriteFloatFieldN(name, savable); return savable; }
		public double? Sync(FieldId name, double? savable) { _s.WriteDoubleFieldN(name, savable); return savable; }
		public decimal? Sync(FieldId name, decimal? savable) { _s.WriteDecimalFieldN(name, savable); return savable; }
		public BigInteger? Sync(FieldId name, BigInteger? savable) { _s.WriteBigIntFieldN(name, savable); return savable; }
		public char?   Sync(FieldId name, char? savable)   { _s.WriteVarintValueN(name, savable == null ? (ulong?)null : (ushort)savable.Value); return savable; }

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			var saver = new ScannerSaver<Writer, Scanner, bool, SyncPrimitive<Writer>>(new SyncPrimitive<Writer>(), mode);
			saver.Write(ref this, name, scanner!, saving, tupleLength);
			return saving;
		}

		public List? SyncListByteImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
		{
			var saver = new ScannerSaver<Writer, Scanner, byte, SyncPrimitive<Writer>>(new SyncPrimitive<Writer>(), mode);
			saver.Write(ref this, name, scanner!, saving, tupleLength);
			return saving;
		}

		public List? SyncListCharImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			var saver = new ScannerSaver<Writer, Scanner, char, SyncPrimitive<Writer>>(new SyncPrimitive<Writer>(), mode);
			saver.Write(ref this, name, scanner!, saving, tupleLength);
			return saving;
		}

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
			=> _s.BeginSubObject(name, childKey, mode, listLength);

		public void EndSubObject() => _s.EndSubObject();

		public IBufferWriter<byte> Flush() => _s.Flush();
	}
}
