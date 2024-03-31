using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.Compatibility;
using Loyc.Syntax;
using Loyc.SyncLib.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Runtime.CompilerServices;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		public static SyncJson.Writer NewWriter(IBufferWriter<byte>? output = null, Options? options = null)
			=> new Writer(new WriterState(output ?? new ArrayBufferWriter<byte>(), options ?? _defaultOptions));

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
			where SyncObject : ISyncObject<SyncJson.Writer, T>
		{
			options ??= _defaultOptions;
			var output = new ArrayBufferWriter<byte>(options.Write.InitialBufferSize);
			Writer writer = NewWriter(output, options);
			SyncManagerExt.Sync(writer, null, value, sync, options.RootMode);
			writer._s.Flush();
			return output.WrittenMemory;
		}
		public static string WriteString<T>(T value, SyncObjectFunc<Writer, T> sync, Options? options = null)
			=> Utf8ToString(Write(value, sync, options));
		public static string WriteStringI<T>(T value, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
			=> Utf8ToString(WriteI(value, sync, options));
		public static string WriteString<T, SyncObject>(T value, SyncObject sync, Options? options = null)
			where SyncObject : ISyncObject<SyncJson.Writer, T>
			=> Utf8ToString(Write(value, sync, options));

		static string Utf8ToString(ReadOnlyMemory<byte> text)
			#if NETSTANDARD2_0 || NET45 || NET46 || NET47
			=> Encoding.UTF8.GetString(text.ToArray());
			#else
			=> Encoding.UTF8.GetString(text.Span);
			#endif

		private static bool MayBeNullable(ObjectMode mode)
			=> (mode & (ObjectMode.NotNull | ObjectMode.Deduplicate)) != ObjectMode.NotNull;

		/// <summary>
		///   An optimized implementation of <see cref="ISyncManager"/> for writing JSON 
		///   objects. <see cref="SupportsReordering"/> and <see cref="SupportsDeduplication"/>
		///   are both true.
		/// </summary><remarks>
		///   This is a struct rather than a class for performance reasons. Don't try to use
		///   a <c>default(Writer)</c>; it'll throw <see cref="NullReferenceException"/>. 
		/// </remarks>
		public partial struct Writer : ISyncManager
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

			public int Depth => _s.Depth;

			public object CurrentObject { set { } } // implementation is not needed for a writer

			public bool SupportsNextField => false;

			public FieldId NextField => FieldId.Missing;

			public (bool Begun, object? Object) BeginSubObject(FieldId name, object? childKey, ObjectMode mode, int listLength = -1)
			{
				return _s.BeginSubObject(name.Name, childKey, mode);
			}

			public void EndSubObject() => _s.EndSubObject();

			public SyncType GetFieldType(FieldId name, SyncType expectedType = SyncType.Unknown) => SyncType.Unknown;

			public string? SyncTypeTag(string? tag) {
				_s.WriteProp(_s._opt.NewtonsoftCompatibility ? "$type" : "\t", tag);
				return tag;
			}

			public bool Sync(FieldId name, bool savable)
			{
				_s.WriteLiteralProp(name.Name, savable ? _true : _false);
				return savable;
			}

			public int Sync(FieldId name, int savable, int bits, bool signed = true)
			{
				if (signed)
					return Sync(name, savable);
				else
					return (int) _s.WriteProp(name == null ? "" : name.Name, (uint) savable, signed);
			}

			public long Sync(FieldId name, long savable, int bits, bool signed = true)
			{
				if (signed)
					return Sync(name, savable);
				else
					return _s.WriteProp(name == null ? "" : name.Name, savable, signed);
			}

			public BigInteger Sync(FieldId name, BigInteger savable, int bits, bool signed = true) => Sync(name, savable);

			public string? Sync(FieldId name, string? savable, ObjectMode mode = ObjectMode.Normal) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}

			/// <summary>Ensures that all written output has been registered with the 
			///   <see cref="IBufferWriter{byte}"/> object with which this writer was 
			///   initialized, and then returns it. If you called <see cref="NewWriter"/> 
			///   without arguments, this function returns a <see cref="ArrayBufferWriter{T}"/>
			///   (see example below).</summary>
			/// <remarks>
			///   It is only necessary to call this method just after writing a primitive 
			///   (e.g. number or string), because flushing happens automatically when 
			///   <see cref="ISyncManager.EndSubObject"/> is used to finish writing an 
			///   object or list.
			/// <para/>
			///   Here's an example that uses this method to write a primitive:
			/// <pre><![CDATA[
			///     var writer = SyncJson.NewWriter();
			///     writer.Sync(null, 1234.5);
			///     var output = (ArrayBufferWriter<byte>) writer.Flush();
			///     
			///     // Output: 1234.5
			///     Console.WriteLine("Output: " + Encoding.UTF8.GetString(output.WrittenSpan));
			/// ]]></pre>
			/// </remarks>
			public IBufferWriter<byte> Flush() => _s.Flush();

			public List? SyncListBoolImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<bool>
				where ListBuilder : IListBuilder<List, bool>
			{
				var saver = new ScannerSaver<Writer, Scanner, bool, SyncPrimitive<Writer>>(new SyncPrimitive<Writer>(), mode);
				saver.Write(ref this, name, scanner!, saving, tupleLength);
				return saving;
			}

			public List? SyncListCharImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<char>
				where ListBuilder : IListBuilder<List, char>
			{
				if (MayBeNullable(mode) && saving == null) {
					var status = BeginSubObject(name, null, mode, 0);
					Debug.Assert(!status.Begun && status.Object == null);
					return default;
				} else {
					// TODO: support deduplication
					if (_s._opt.Write.CharListAsString ?? !_s._opt.NewtonsoftCompatibility) {
						// Write character list as string
						var empty = default(Memory<char>);
						var chars = scanner.Read(0, int.MaxValue, ref empty);
						_s.WriteProp(name.Name ?? "", chars.Span);
					} else {
						// Write character list as array
						var saver = new ScannerSaver<SyncJson.Writer, Scanner, char, SyncPrimitive<SyncJson.Writer>>(new SyncPrimitive<SyncJson.Writer>(), mode);
						saver.Write(ref this, name, scanner!, saving, tupleLength);
					}
					return saving;
				}
			}

			public List? SyncListByteImpl<Scanner, List, ListBuilder>(FieldId name, Scanner scanner, List? saving, ListBuilder builder, ObjectMode mode, int tupleLength = -1)
				where Scanner : IScanner<byte>
				where ListBuilder : IListBuilder<List, byte>
			{
				if (MayBeNullable(mode) && saving == null) {
					var status = BeginSubObject(name, null, mode, 0);
					Debug.Assert(!status.Begun && status.Object == null);
					return default;
				} else {
					// TODO: support deduplication
					if (_s._opt.NewtonsoftCompatibility && scanner is not InternalList.Scanner<byte> || 
						_s._opt.ByteArrayMode == JsonByteArrayMode.Array) {
						// Write bytes as list
						var saver = new ScannerSaver<SyncJson.Writer, Scanner, byte, SyncPrimitive<SyncJson.Writer>>(new SyncPrimitive<SyncJson.Writer>(), mode);
						saver.Write(ref this, name, scanner!, saving, tupleLength);
					} else {
						// Write bytes as string
						var empty = default(Memory<byte>);
						var bytes = scanner.Read(0, int.MaxValue, ref empty);
						_s.WriteBytesAsString(name.Name ?? "", bytes.Span);
					}
					return saving;
				}
			}
		}
	}
}
