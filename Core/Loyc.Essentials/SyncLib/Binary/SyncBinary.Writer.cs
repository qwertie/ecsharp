using Loyc.Collections;
using Loyc.Compatibility;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.SyncLib;

partial class SyncBinary
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
		where SyncObject : ISyncObject<SyncBinary.Writer, T>
	{
		options ??= _defaultOptions;
		var output = new ArrayBufferWriter<byte>(options.Write.InitialBufferSize);
		Writer writer = NewWriter(output, options);
		SyncManagerExt.Sync(writer, null, value, sync, options.RootMode);
		writer._s.Flush();
		return output.WrittenMemory;
	}

	/// <summary>Creates a writer that writes to <paramref name="output"/> (a new
	///   <see cref="ArrayBufferWriter{T}"/> if it is null). To write a single value, it
	///   is simpler to call SyncBinary.Write.</summary>
	/// <remarks>Output is buffered. It reaches <paramref name="output"/> (via
	///   <see cref="IBufferWriter{T}.Advance"/>) when the writer's buffer fills up, and
	///   when the root object or list is finished (by <see cref="Writer.EndSubObject"/>).
	///   So you only need to call <see cref="Writer.Flush"/> if the root value is a
	///   primitive (e.g. a number or string), or if you want to see the output before the
	///   root object or list is finished (but not in the middle of a series of
	///   bitfields).</remarks>
	public static SyncBinary.Writer NewWriter(IBufferWriter<byte>? output = null, Options? options = null)
		=> new Writer(new WriterState(output ?? new ArrayBufferWriter<byte>(), options ?? _defaultOptions));

	/// <summary>
	///   The <see cref="ISyncManager"/> implementation for writing SyncLib's compact,
	///   high-performance binary data format (see <see cref="SyncBinary"/> for details and 
	///   guidelines). Please call <see cref="WriteI"/> or <see cref="Write"/> if you just 
	///   want to serialize something, or <see cref="NewWriter"/> to create one of these.
	/// </summary><remarks>
	///   This is a struct rather than a class for performance reasons. Don't try to use 
	///   a <c>default(Writer)</c>; it'll just throw <see cref="NullReferenceException"/>. 
	/// </remarks>
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
		public bool IsPlainText => false;
		public ISyncOptions? Options => _s._opt;

		public bool IsInsideList => _s.IsInsideList;

		public bool? ReachedEndOfList => null;

		public int? MinimumListLength => null;

		public int Depth => _s.Depth;

		public object CurrentObject { set { } } // implementation is not needed for a writer

		public bool SupportsNextField => false;

		public FieldId NextField => FieldId.Missing;

		public (bool Begun, int Length, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
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

		public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal) {
			_s.Write(savable, mode);
			return savable;
		}

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

		public List? SyncListBoolImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<bool>
			where ListBuilder : IListBuilder<List, bool>
		{
			WriteByteOrBoolList<Scanner, bool>(name, scanner, saving, mode, tupleLength);
			return saving;
		}

		public List? SyncListByteImpl<Scanner, List, ListBuilder>(
			FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
			where Scanner : IScanner<byte>
			where ListBuilder : IListBuilder<List, byte>
		{
			WriteByteOrBoolList<Scanner, byte>(name, scanner, saving, mode, tupleLength);
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

		// Writes a list of bytes or bools (T must be byte or bool). Unlike other lists, these
		// are written in bulk: raw bytes for a byte list (the same format as a string), and
		// one byte (0 or 1) per item for a bool list.
		void WriteByteOrBoolList<Scanner, T>(FieldId name, Scanner scanner, object? saving, ObjectMode mode, int tupleLength)
			where Scanner : IScanner<T>
			where T : struct
		{
			var (begun, _, _) = BeginSubObject(name, saving, mode | ObjectMode.List, tupleLength);
			if (begun) {
				Memory<T> mem = default;
				ReadOnlySpan<T> span = default;
				while ((span = scanner!.Read(span.Length, -1, ref mem).Span).Length != 0) {
					if (typeof(T) == typeof(byte))
						_s.WriteRawBytes(MemoryMarshal.Cast<T, byte>(span));
					else
						_s.WriteBools(MemoryMarshal.Cast<T, bool>(span));
				}
				EndSubObject();
			}
		}

		public IBufferWriter<byte> Flush()
		{
			_s.Flush();
			return _s._output;
		}
	}
}
