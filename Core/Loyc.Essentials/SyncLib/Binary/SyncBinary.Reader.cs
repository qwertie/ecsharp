using Loyc.Collections;
using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
{
	public static SyncBinary.Reader NewReader(IScanner<byte> input, Options? options = null)
		=> new Reader(new ReaderState(input ?? throw new ArgumentNullException(nameof(input)), options ?? _defaultOptions));
	public static SyncBinary.Reader NewReader(ReadOnlyMemory<byte> input, Options? options = null)
		=> new Reader(new ReaderState(input, options ?? _defaultOptions));
	public static SyncBinary.Reader NewReader(byte[] input, Options? options = null) => NewReader(input.AsMemory(), options);

	internal static T? Read<T>(ReadOnlyMemory<byte> input, SyncObjectFunc<Reader, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(input, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}
	internal static T? ReadI<T>(ReadOnlyMemory<byte> input, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
	{
		options ??= _defaultOptions;
		Reader reader = NewReader(input, options);
		return SyncManagerExt.Sync(reader, null, default(T), sync, options.RootMode);
	}

	internal static T? Read<T>(byte[] input, SyncObjectFunc<Reader, T> sync, Options? options = null)
		=> Read(input.AsMemory(), sync, options);
	internal static T? ReadI<T>(byte[] input, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		=> ReadI(input.AsMemory(), sync, options);

	/// <summary>
	///   An implementation of <see cref="ISyncManager"/> for reading SyncLib's compact, 
	///   high-performance data format. Note that while <see cref="SupportsDeduplication"/> 
	///   is true, <see cref="SupportsReordering"/> and <see cref="SupportsNextField"/> are
	///   false, the binary format is <b>not</b> self-describing, and field names/IDs are 
	///   not stored in the data stream, so you must be careful to read the data in the 
	///   same order and with compatible types as what was written (for details and 
	///   guidelines, see <see cref="SyncBinary"/>). Please call <see cref="SyncBinary.Read"/>
	///   or <see cref="SyncBinary.ReadI"/> if you just want to serialize something, or
	///   <see cref="NewWriter"/> to create one of these.
	/// </summary><remarks>
	///   <see cref="Reader"/> can read files of unlimited size.
	///   <para/>
	///   This is a struct rather than a class for performance reasons. Don't try to use
	///   a <c>default(Reader)</c>; it'll just throw <see cref="NullReferenceException"/>. 
	/// </remarks>
	public partial struct Reader : ISyncManager
	{
		private ReaderState _s;

		internal Reader(ReaderState state) => _s = state;

		public SyncMode Mode => SyncMode.Reading;
		public bool IsReading => true;
		public bool IsWriting => false;

		public bool SupportsReordering => false;
		public bool SupportsDeduplication => true;
		public bool NeedsIntegerIds => false;

		public bool IsInsideList => _s.IsInsideList;

		public bool? ReachedEndOfList => null;// _s.IsInsideList ? _s.ReachedEndOfList : null;

		public int? MinimumListLength => 0;

		public int Depth => _s.Depth;

		public object CurrentObject { set => _s.SetCurrentObject(value); }

		public bool SupportsNextField => true; // TODO: support it!

		public FieldId NextField => FieldId.Missing;

		public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => SyncType.Unknown;

		public bool Sync(FieldId name, bool savable) => _s.ReadNormalInt<int>() != 0;

		public sbyte Sync(FieldId name, sbyte savable) => _s.ReadSmallInt<sbyte>();

		public byte Sync(FieldId name, byte savable) => _s.ReadSmallInt<byte>();

		public short Sync(FieldId name, short savable) => _s.ReadSmallInt<short>();

		public ushort Sync(FieldId name, ushort savable) => _s.ReadSmallInt<ushort>();

		public int Sync(FieldId name, int savable) => _s.ReadNormalInt<int>();

		public uint Sync(FieldId name, uint savable) => _s.ReadNormalInt<uint>();

		public long Sync(FieldId name, long savable) => _s.ReadNormalInt<long>();

		public ulong Sync(FieldId name, ulong savable) => _s.ReadNormalInt<ulong>();

		public float Sync(FieldId name, float savable) => throw new NotImplementedException();

		public double Sync(FieldId name, double savable) => throw new NotImplementedException();

		public decimal Sync(FieldId name, decimal savable) => throw new NotImplementedException();

		public BigInteger Sync(FieldId name, BigInteger savable) => throw new NotImplementedException();

		public char Sync(FieldId name, char savable) => (char) _s.ReadSmallInt<ushort>();

		public string Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal) => throw new NotImplementedException();

		public bool? Sync(FieldId name, bool? savable) => throw new NotImplementedException();

		public sbyte? Sync(FieldId name, sbyte? savable) => _s.ReadSmallIntOrNull<sbyte>();

		public byte? Sync(FieldId name, byte? savable) => _s.ReadSmallIntOrNull<byte>();

		public short? Sync(FieldId name, short? savable) => _s.ReadSmallIntOrNull<short>();

		public ushort? Sync(FieldId name, ushort? savable) => _s.ReadSmallIntOrNull<ushort>();

		public int? Sync(FieldId name, int? savable) => _s.ReadNormalIntOrNull<int>();

		public uint? Sync(FieldId name, uint? savable) => _s.ReadNormalIntOrNull<uint>();

		public long? Sync(FieldId name, long? savable) => _s.ReadNormalIntOrNull<long>();

		public ulong? Sync(FieldId name, ulong? savable) => _s.ReadNormalIntOrNull<ulong>();

		public float? Sync(FieldId name, float? savable) => throw new NotImplementedException();

		public double? Sync(FieldId name, double? savable) => throw new NotImplementedException();

		public decimal? Sync(FieldId name, decimal? savable) => throw new NotImplementedException();

		public BigInteger? Sync(FieldId name, BigInteger? savable) => throw new NotImplementedException();

		public char? Sync(FieldId name, char? savable) => (char) _s.ReadSmallIntOrNull<ushort>();

		public int Sync(FieldId name, int savable, int bits, bool signed = true)
			=> throw new NotImplementedException();

		public long Sync(FieldId name, long savable, int bits, bool signed = true)
			=> throw new NotImplementedException();

		public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true)
			=> throw new NotImplementedException();

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
			var loader = new ListLoader<Reader, List, byte, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode, tupleLength);
			return loader.Sync(ref this, name, saving);
		}

		public List? SyncListCharImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<char>
			where ListBuilder : IListBuilder<List, char>
		{
			var loader = new ListLoader<Reader, List, char, ListBuilder, SyncPrimitive<Reader>>(new SyncPrimitive<Reader>(), builder, mode, tupleLength);
			return loader.Sync(ref this, name, saving);
		}

		public (bool Begun, int Length, object Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
		{
			return _s.BeginSubObject(mode, listLength);
		}

		public void EndSubObject()
		{
			throw new NotImplementedException();
		}

		public string SyncTypeTag(string? tag) => throw new NotImplementedException();
	}
}
