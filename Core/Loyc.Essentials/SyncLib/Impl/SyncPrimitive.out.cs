// Generated from SyncPrimitive.ecs by LeMP custom tool. LeMP version: 30.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Impl
{

	public struct SyncPrimitive<SyncManager>
	
	 : 
	ISyncField<SyncManager, bool>, 
	ISyncField<SyncManager, sbyte>, 
	ISyncField<SyncManager, byte>, 
	ISyncField<SyncManager, short>, 
	ISyncField<SyncManager, ushort>, 
	ISyncField<SyncManager, int>, 
	ISyncField<SyncManager, uint>, 
	ISyncField<SyncManager, long>, 
	ISyncField<SyncManager, ulong>, 
	ISyncField<SyncManager, float>, 
	ISyncField<SyncManager, double>, 
	ISyncField<SyncManager, decimal>, 
	ISyncField<SyncManager, BigInteger>, 
	ISyncField<SyncManager, char>, 
	ISyncField<SyncManager, bool?>, 
	ISyncField<SyncManager, sbyte?>, 
	ISyncField<SyncManager, byte?>, 
	ISyncField<SyncManager, short?>, 
	ISyncField<SyncManager, ushort?>, 
	ISyncField<SyncManager, int?>, 
	ISyncField<SyncManager, uint?>, 
	ISyncField<SyncManager, long?>, 
	ISyncField<SyncManager, ulong?>, 
	ISyncField<SyncManager, float?>, 
	ISyncField<SyncManager, double?>, 
	ISyncField<SyncManager, decimal?>, 
	ISyncField<SyncManager, BigInteger?>, 
	ISyncField<SyncManager, char?>, 
	ISyncField<SyncManager, string?> where SyncManager: ISyncManager
	
	
	
	
	{
		public bool Sync(ref SyncManager sync, Symbol? name, bool x) => sync.Sync(name, x);
		public sbyte Sync(ref SyncManager sync, Symbol? name, sbyte x) => sync.Sync(name, x);
		public byte Sync(ref SyncManager sync, Symbol? name, byte x) => sync.Sync(name, x);
		public short Sync(ref SyncManager sync, Symbol? name, short x) => sync.Sync(name, x);
		public ushort Sync(ref SyncManager sync, Symbol? name, ushort x) => sync.Sync(name, x);
		public int Sync(ref SyncManager sync, Symbol? name, int x) => sync.Sync(name, x);
		public uint Sync(ref SyncManager sync, Symbol? name, uint x) => sync.Sync(name, x);
		public long Sync(ref SyncManager sync, Symbol? name, long x) => sync.Sync(name, x);
		public ulong Sync(ref SyncManager sync, Symbol? name, ulong x) => sync.Sync(name, x);
		public float Sync(ref SyncManager sync, Symbol? name, float x) => sync.Sync(name, x);
		public double Sync(ref SyncManager sync, Symbol? name, double x) => sync.Sync(name, x);
		public decimal Sync(ref SyncManager sync, Symbol? name, decimal x) => sync.Sync(name, x);
		public BigInteger Sync(ref SyncManager sync, Symbol? name, BigInteger x) => sync.Sync(name, x);
		public char Sync(ref SyncManager sync, Symbol? name, char x) => sync.Sync(name, x);
		public bool? Sync(ref SyncManager sync, Symbol? name, bool? x) => sync.SyncNullable(name, x);
		public sbyte? Sync(ref SyncManager sync, Symbol? name, sbyte? x) => sync.SyncNullable(name, x);
		public byte? Sync(ref SyncManager sync, Symbol? name, byte? x) => sync.SyncNullable(name, x);
		public short? Sync(ref SyncManager sync, Symbol? name, short? x) => sync.SyncNullable(name, x);
		public ushort? Sync(ref SyncManager sync, Symbol? name, ushort? x) => sync.SyncNullable(name, x);
		public int? Sync(ref SyncManager sync, Symbol? name, int? x) => sync.SyncNullable(name, x);
		public uint? Sync(ref SyncManager sync, Symbol? name, uint? x) => sync.SyncNullable(name, x);
		public long? Sync(ref SyncManager sync, Symbol? name, long? x) => sync.SyncNullable(name, x);
		public ulong? Sync(ref SyncManager sync, Symbol? name, ulong? x) => sync.SyncNullable(name, x);
		public float? Sync(ref SyncManager sync, Symbol? name, float? x) => sync.SyncNullable(name, x);
		public double? Sync(ref SyncManager sync, Symbol? name, double? x) => sync.SyncNullable(name, x);
		public decimal? Sync(ref SyncManager sync, Symbol? name, decimal? x) => sync.SyncNullable(name, x);
		public BigInteger? Sync(ref SyncManager sync, Symbol? name, BigInteger? x) => sync.SyncNullable(name, x);
		public char? Sync(ref SyncManager sync, Symbol? name, char? x) => sync.SyncNullable(name, x);
		public string? Sync(ref SyncManager sync, Symbol? name, string? x) => sync.Sync(name, x);
	}
}