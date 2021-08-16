// Generated from SyncJson.ecs by LeMP custom tool. LeMP version: 30.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
///
/// This file uses Enhanced C# to generate some of the code of SyncJson,
/// SyncJson.Writer and SyncJson.Reader.
///
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

using Loyc.Collections.Impl;
using Loyc.SyncLib.Impl;

namespace Loyc.SyncLib
{
	// Here we generate SOME code; the rest of SyncJson is in .cs files.
	public static partial class SyncJson
	{
		internal static readonly byte[] _true = new byte[] { 
			(byte) 't', (byte) 'r', (byte) 'u', (byte) 'e'
		};
		internal static readonly byte[] _false = new byte[] { 
			(byte) 'f', (byte) 'a', (byte) 'l', (byte) 's', (byte) 'e'
		};
		internal static readonly byte[] _null = new byte[] { 
			(byte) 'n', (byte) 'u', (byte) 'l', (byte) 'l'
		};

		internal static readonly byte[] _ref = new byte[] { 
			(byte) '"', (byte) '$', (byte) 'r', (byte) 'e', (byte) 'f', (byte) '"'
		};	// Newtonsoft-style backreference
		internal static readonly byte[] _id = new byte[] { 
			(byte) '"', (byte) '$', (byte) 'i', (byte) 'd', (byte) '"'
		};	// Newtonsoft-style id
		internal static readonly byte[] _values = new byte[] { 
			(byte) '"', (byte) '$', (byte) 'v', (byte) 'a', (byte) 'l', (byte) 'u', (byte) 'e', (byte) 's', (byte) '"'
		};	// Newtonsoft-style array prop
		internal static readonly byte[] _f = new byte[] { 
			(byte) '"', (byte) '\\', (byte) 'f', (byte) '"'
		};	// SyncJson-style id
		internal static readonly byte[] _r = new byte[] { 
			(byte) '"', (byte) '\\', (byte) 'r', (byte) '"'
		};	// SyncJson-style backreference
		static Options _defaultOptions = new Options();

		public partial struct Writer
		{
			public sbyte Sync(FieldId name, sbyte savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false || false || false));
				return savable;
			}
			public byte Sync(FieldId name, byte savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(true || false || false || false));
				return savable;
			}
			public short Sync(FieldId name, short savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false || false || false));
				return savable;
			}
			public ushort Sync(FieldId name, ushort savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || true || false || false));
				return savable;
			}
			public int Sync(FieldId name, int savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false || false || false));
				return savable;
			}
			public uint Sync(FieldId name, uint savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false || true || false));
				return savable;
			}
			public long Sync(FieldId name, long savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false || false || false));
				return savable;
			}
			public ulong Sync(FieldId name, ulong savable) {
				_s.WriteProp(name == null ? "" : name.Name, (long) savable, !(false || false || false || true));
				return savable;
			}
			public BigInteger Sync(FieldId name, BigInteger savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public float Sync(FieldId name, float savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public double Sync(FieldId name, double savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public decimal Sync(FieldId name, decimal savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public char Sync(FieldId name, char savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public bool? Sync(FieldId name, bool? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteLiteralProp(nameS, savable.Value ? _true : _false);
				return savable;
			}
			public sbyte? Sync(FieldId name, sbyte? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public byte? Sync(FieldId name, byte? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public short? Sync(FieldId name, short? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public ushort? Sync(FieldId name, ushort? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public int? Sync(FieldId name, int? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public uint? Sync(FieldId name, uint? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public long? Sync(FieldId name, long? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public ulong? Sync(FieldId name, ulong? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public float? Sync(FieldId name, float? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public double? Sync(FieldId name, double? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public decimal? Sync(FieldId name, decimal? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public BigInteger? Sync(FieldId name, BigInteger? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public char? Sync(FieldId name, char? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
		}
	}
}