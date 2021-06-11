// Generated from SyncJson.ecs by LeMP custom tool. LeMP version: 30.0.5.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace Loyc.SyncLib
{
	public static partial class SyncJson
	{
		partial class WriterState
		{
			internal static readonly byte[] _null = new byte[] { 
				(byte) 'n', (byte) 'u', (byte) 'l', (byte) 'l'
			};
		}

		public partial struct Writer
		{
			internal static readonly byte[] _true = new byte[] { 
				(byte) 't', (byte) 'r', (byte) 'u', (byte) 'e'
			};
			internal static readonly byte[] _false = new byte[] { 
				(byte) 'f', (byte) 'a', (byte) 'l', (byte) 's', (byte) 'e'
			};
			internal static readonly byte[] _null = WriterState._null;
			public int Sync(Symbol? name, int savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false));
				return savable;
			}
			public uint Sync(Symbol? name, uint savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(true || false));
				return savable;
			}
			public long Sync(Symbol? name, long savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false));
				return savable;
			}
			public ulong Sync(Symbol? name, ulong savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || true));
				return savable;
			}
			public BigInteger Sync(Symbol? name, BigInteger savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public float Sync(Symbol? name, float savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public double Sync(Symbol? name, double savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public char Sync(Symbol? name, char savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public bool? SyncNullable(Symbol? name, bool? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value ? _true : _false);
				return savable;
			}
			public int? SyncNullable(Symbol? name, int? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public uint? SyncNullable(Symbol? name, uint? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public long? SyncNullable(Symbol? name, long? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public ulong? SyncNullable(Symbol? name, ulong? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public float? SyncNullable(Symbol? name, float? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public double? SyncNullable(Symbol? name, double? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public BigInteger? SyncNullable(Symbol? name, BigInteger? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public char? SyncNullable(Symbol? name, char? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public ReadOnlyMemory<char>? SyncNullable(Symbol? name, ReadOnlyMemory<char>? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					_s.WriteProp(nameS, savable.Value.Span);
				return savable;
			}
			public ReadOnlyMemory<byte>? SyncNullable(Symbol? name, ReadOnlyMemory<byte>? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteProp(nameS, _null);
				else
					;// _s.WriteProp(nameS, ByteArrayInString.Convert(savable.Value, allowControlChars: false));
				return savable;
			}
		}
	}
}