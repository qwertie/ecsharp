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
		static Options _defaultOptions = new Options();

		public static SyncJson.Writer NewWriter(IBufferWriter<byte> output, Options? options = null)
			=> new Writer(new WriterState(output ?? new ArrayBufferWriter<byte>(), options ?? _defaultOptions));
		public static ReadOnlyMemory<byte> Write<T>(T value, SyncObjectFunc<Writer, T> sync, Options? options = null)
		{
			var output = new ArrayBufferWriter<byte>(1024);
			Writer w = NewWriter(output, options);
			SyncManagerExt.Sync(w, (Symbol?) null, value, sync, (options ?? _defaultOptions).RootMode);
			return output.WrittenMemory;
		}
		public static ReadOnlyMemory<byte> Write<T>(T value, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
		{
			var output = new ArrayBufferWriter<byte>(1024);
			Writer w = NewWriter(output, options);
			SyncManagerExt.Sync(w, (Symbol?) null, value, sync, (options ?? _defaultOptions).RootMode);
			return output.WrittenMemory;
		}
		public static string WriteString<T>(T value, SyncObjectFunc<Writer, T> sync, Options? options = null)
			#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
			=> Encoding.UTF8.GetString(Write(value, sync, options).ToArray());
			#else
			=> Encoding.UTF8.GetString(Write(value, sync, options).Span);
			#endif
		public static string WriteString<T>(T value, SyncObjectFunc<ISyncManager, T> sync, Options? options = null)
			#if NETSTANDARD2_0 || NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472
			=> Encoding.UTF8.GetString(Write(value, sync, options).ToArray());
			#else
			=> Encoding.UTF8.GetString(Write(value, sync, options).Span);
			#endif

		public partial struct Writer : ISyncManager
		{
			internal WriterState _s;
			internal Writer(WriterState s) => _s = s;

			public SyncMode Mode => SyncMode.Saving;

			public bool IsSaving => true;

			public bool SupportsReordering => true;

			public bool SupportsDeduplication => true;

			public bool IsInsideList => _s._isInsideList;

			public bool NeedsIntegerIds => false;

			public bool? ReachedEndOfList => null;

			public int? MinimumListLength => null;

			public int Depth => _s._stack.Count - 1;

			public object CurrentObject { set { } }

			public (bool Begun, object? Object) BeginSubObject(Symbol? name, object? childKey, SubObjectMode mode, int listLength = -1)
			{
				return _s.BeginSubObject(name?.Name ?? "", childKey, mode, listLength);
			}

			public void EndSubObject() => _s.EndSubObject();

			public bool? HasField(Symbol name) => null;

			public bool Sync(Symbol? name, bool savable)
			{
				_s.WriteLiteralProp(name?.Name ?? "", savable ? _true : _false);
				return savable;
			}

			public int Sync(Symbol? name, int savable, int bits, bool signed = true)
			{
				if (signed)
					return Sync(name, savable);
				else
					return (int) _s.WriteProp(name == null ? "" : name.Name, (uint) savable, signed);
			}

			public long Sync(Symbol? name, long savable, int bits, bool signed = true)
			{
				if (signed)
					return Sync(name, savable);
				else
					return _s.WriteProp(name == null ? "" : name.Name, savable, signed);
			}

			public BigInteger Sync(Symbol? name, BigInteger savable, int bits, bool signed = true) => Sync(name, savable);

			public InternalList<byte> SyncListImpl(Symbol? name, ReadOnlySpan<byte> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				var name2 = name == null ? "" : name.Name;
				if (savable == default)
					_s.WriteNull(name2);
				else {
					if (_s._opt.UseBais ?? !_s._opt.NewtonsoftCompatibility) {
						_s.WriteProp(name2, ByteArrayInString.ConvertFromBytes(savable, allowControlChars: false));
					} else {
						SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
					}
				}
				return default;
			}

			public string Sync(Symbol? name, string savable) => SyncNullable(name, savable)!;
			public string? SyncNullable(Symbol? name, string? savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}

			/// <summary>Ensures that all written output has been registered with the 
			/// <see cref="IBufferWriter{byte}"/> object with which this writer was 
			/// initialized, so that e.g. <see cref="ArrayBufferWriter{T}.WrittenMemory"/> 
			/// returns complete JSON output. It is only necessary to call this method 
			/// if you are writing a primitive (e.g. number or string), because flushing 
			/// happens automatically when <see cref="ISyncManager.EndSubObject"/> is 
			/// used to finish writing an object or list.</summary>
			public void Flush() => _s.Flush();
		}
	}
}
