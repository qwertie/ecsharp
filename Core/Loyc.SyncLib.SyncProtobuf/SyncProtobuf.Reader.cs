using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib;

partial class SyncProtobuf
{
	public static SyncProtobuf.Reader NewReader(ReadOnlyMemory<byte> input, Options? options = null)
		=> new Reader(new ReaderState(input, options ?? _defaultOptions));
	public static SyncProtobuf.Reader NewReader(byte[] input, Options? options = null)
		=> NewReader(input.AsMemory(), options);
	public static SyncProtobuf.Reader NewReader(IScanner<byte> input, Options? options = null)
		=> new Reader(new ReaderState(input ?? throw new ArgumentNullException(nameof(input)), options ?? _defaultOptions));

	public static T? Read<T>(ReadOnlyMemory<byte> input, SyncObjectFunc<Reader, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(input, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}
	public static T? ReadI<T>(ReadOnlyMemory<byte> input, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(input, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}
	public static T? Read<T, SyncObject>(ReadOnlyMemory<byte> input, SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(input, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}

	public static T? Read<T>(byte[] input, SyncObjectFunc<Reader, T> sync, Options? options = null)
		=> Read(input.AsMemory(), sync, options);
	public static T? ReadI<T>(byte[] input, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		=> ReadI(input.AsMemory(), sync, options);
	public static T? Read<T, SyncObject>(byte[] input, SyncObject sync, Options? options = null)
		where SyncObject : ISyncObject<Reader, T>
		=> Read<T, SyncObject>(input.AsMemory(), sync, options);

	/// <summary>
	///   The <see cref="ISyncManager"/> implementation that reads the Protocol Buffers wire
	///   format (see <see cref="SyncProtobuf"/>). Because fields are keyed by integer ID,
	///   this reader supports reordering (<see cref="SupportsReordering"/>) and skips
	///   unknown fields. Call <see cref="Read{T}(ReadOnlyMemory{byte}, SyncObjectFunc{Reader, T}, Options?)"/>
	///   to deserialize, or <see cref="NewReader(ReadOnlyMemory{byte}, Options?)"/> to
	///   construct one directly.
	/// </summary><remarks>
	///   This is a struct (for performance) that wraps a mutable state class. Do not use
	///   <c>default(Reader)</c>; it will throw <see cref="NullReferenceException"/>.
	/// </remarks>
	public struct Reader : ISyncManager
	{
		internal ReaderState _s;
		internal Reader(ReaderState state) => _s = state;

		public SyncMode Mode => SyncMode.Reading;
		public bool IsReading => true;
		public bool IsWriting => false;

		public bool SupportsReordering => true;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => true;
		public bool SupportsNextField => true;
		public bool IsPlainText => false;
		public ISyncOptions? Options => _s._opt;

		public bool IsInsideList => _s.IsInsideList;
		public bool? ReachedEndOfList => _s.ReachedEndOfList;
		public int? MinimumListLength => _s.MinimumListLength;
		public int Depth => _s.Depth;

		public FieldId NextField => _s.NextField;
		public object CurrentObject { set => _s.SetCurrentObject(value); }

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => _s.GetFieldType(name, expectedType);

		public string? SyncTypeTag(string? tag) => _s.ReadTypeTag();

		public bool   Sync(FieldId name, bool savable)   => _s.ReadUInt(name) != 0;
		public sbyte  Sync(FieldId name, sbyte savable)  => unchecked((sbyte)_s.ReadInt(name));
		public byte   Sync(FieldId name, byte savable)   => unchecked((byte)_s.ReadUInt(name));
		public short  Sync(FieldId name, short savable)  => unchecked((short)_s.ReadInt(name));
		public ushort Sync(FieldId name, ushort savable) => unchecked((ushort)_s.ReadUInt(name));
		public int    Sync(FieldId name, int savable)    => unchecked((int)_s.ReadInt(name));
		public uint   Sync(FieldId name, uint savable)   => unchecked((uint)_s.ReadUInt(name));
		public long   Sync(FieldId name, long savable)   => _s.ReadInt(name);
		public ulong  Sync(FieldId name, ulong savable)  => _s.ReadUInt(name);
		public float  Sync(FieldId name, float savable)  => _s.ReadFloatRaw(name);
		public double Sync(FieldId name, double savable) => _s.ReadDoubleRaw(name);
		public decimal Sync(FieldId name, decimal savable) => _s.ReadDecimal(name);
		public BigInteger Sync(FieldId name, BigInteger savable) => _s.ReadBigInt(name);
		public char   Sync(FieldId name, char savable)   => unchecked((char)(ushort)_s.ReadUInt(name));

		public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal)
			=> _s.ReadString(name, mode);

		public int Sync(FieldId name, int savable, int bits, bool signed = true) => unchecked((int)_s.ReadInt(name));
		public long Sync(FieldId name, long savable, int bits, bool signed = true) => _s.ReadInt(name);
		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true) => _s.ReadBigInt(name);

		public bool?   Sync(FieldId name, bool? savable)   { var v = _s.ReadUIntN(name); return v.HasValue ? v.Value != 0 : (bool?)null; }
		public sbyte?  Sync(FieldId name, sbyte? savable)  { var v = _s.ReadIntN(name); return v.HasValue ? unchecked((sbyte)v.Value) : (sbyte?)null; }
		public byte?   Sync(FieldId name, byte? savable)   { var v = _s.ReadUIntN(name); return v.HasValue ? unchecked((byte)v.Value) : (byte?)null; }
		public short?  Sync(FieldId name, short? savable)  { var v = _s.ReadIntN(name); return v.HasValue ? unchecked((short)v.Value) : (short?)null; }
		public ushort? Sync(FieldId name, ushort? savable) { var v = _s.ReadUIntN(name); return v.HasValue ? unchecked((ushort)v.Value) : (ushort?)null; }
		public int?    Sync(FieldId name, int? savable)    { var v = _s.ReadIntN(name); return v.HasValue ? unchecked((int)v.Value) : (int?)null; }
		public uint?   Sync(FieldId name, uint? savable)   { var v = _s.ReadUIntN(name); return v.HasValue ? unchecked((uint)v.Value) : (uint?)null; }
		public long?   Sync(FieldId name, long? savable)   => _s.ReadIntN(name);
		public ulong?  Sync(FieldId name, ulong? savable)  => _s.ReadUIntN(name);
		public float?  Sync(FieldId name, float? savable)  => _s.ReadFloatN(name);
		public double? Sync(FieldId name, double? savable) => _s.ReadDoubleN(name);
		public decimal? Sync(FieldId name, decimal? savable) => _s.ReadDecimalN(name);
		public BigInteger? Sync(FieldId name, BigInteger? savable) => _s.ReadBigIntN(name);
		public char?   Sync(FieldId name, char? savable)   { var v = _s.ReadUIntN(name); return v.HasValue ? unchecked((char)(ushort)v.Value) : (char?)null; }

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			var loader = new ListLoader<Reader, List, bool, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode, tupleLength);
			return loader.Sync(ref this, name, saving);
		}

		public List? SyncListByteImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
		{
			// Byte lists are stored as a Protobuf `bytes` value, not as a list container
			var (start, length, backref) = _s.ReadByteListField(name, mode);
			if (start == -1)
				return default; // null
			if (start == -2)
				return builder.CastList(backref!);
			builder.Alloc(length);
			var span = _s.InputSpan.Slice(start, length);
			for (int i = 0; i < length; i++)
				builder.Add(span[i]);
			return builder.List;
		}

		public List? SyncListCharImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			var loader = new ListLoader<Reader, List, char, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode, tupleLength);
			return loader.Sync(ref this, name, saving);
		}

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
			=> _s.BeginSubObject(name, mode);

		public void EndSubObject() => _s.EndSubObject();
	}
}
