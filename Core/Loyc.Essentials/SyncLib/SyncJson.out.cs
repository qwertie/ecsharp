// Generated from SyncJson.ecs by LeMP custom tool. LeMP version: 30.0.5.0
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

		partial class WriterState
		{ }

		public partial struct Writer
		{
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
			public string Sync(Symbol? name, string savable) {
				_s.WriteProp(name == null ? "" : name.Name, savable);
				return savable;
			}
			public bool? SyncNullable(Symbol? name, bool? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteLiteralProp(nameS, savable.Value ? _true : _false);
				return savable;
			}
			public int? SyncNullable(Symbol? name, int? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public uint? SyncNullable(Symbol? name, uint? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public long? SyncNullable(Symbol? name, long? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public ulong? SyncNullable(Symbol? name, ulong? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, (long) savable.Value, false);
				return savable;
			}
			public float? SyncNullable(Symbol? name, float? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public double? SyncNullable(Symbol? name, double? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public BigInteger? SyncNullable(Symbol? name, BigInteger? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public char? SyncNullable(Symbol? name, char? savable) {
				string nameS = name == null ? "" : name.Name;
				if (savable == null)
					_s.WriteNull(nameS);
				else
					_s.WriteProp(nameS, savable.Value);
				return savable;
			}
			public InternalList<bool> SyncListImpl(Symbol? name, ReadOnlySpan<bool> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<sbyte> SyncListImpl(Symbol? name, ReadOnlySpan<sbyte> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<short> SyncListImpl(Symbol? name, ReadOnlySpan<short> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<ushort> SyncListImpl(Symbol? name, ReadOnlySpan<ushort> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<int> SyncListImpl(Symbol? name, ReadOnlySpan<int> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<uint> SyncListImpl(Symbol? name, ReadOnlySpan<uint> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<long> SyncListImpl(Symbol? name, ReadOnlySpan<long> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<ulong> SyncListImpl(Symbol? name, ReadOnlySpan<ulong> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<float> SyncListImpl(Symbol? name, ReadOnlySpan<float> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<double> SyncListImpl(Symbol? name, ReadOnlySpan<double> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<BigInteger> SyncListImpl(Symbol? name, ReadOnlySpan<BigInteger> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<char> SyncListImpl(Symbol? name, ReadOnlySpan<char> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
			public InternalList<string?> SyncListImpl(Symbol? name, ReadOnlySpan<string?> savable, SubObjectMode listMode = SubObjectMode.List)
			{
				SyncManagerHelper.SaveList(ref this, name, savable, new Helper(), listMode);
				return default;
			}
		}
	}
}