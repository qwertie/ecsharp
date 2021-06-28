using Loyc.SyncLib.Impl;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Loyc.SyncLib
{
	partial class SyncJson
	{
		partial struct Helper : 
			ISyncWrite<SyncJson.Writer, bool>,
			ISyncWrite<SyncJson.Writer, sbyte>,
			ISyncWrite<SyncJson.Writer, short>,
			ISyncWrite<SyncJson.Writer, int>,
			ISyncWrite<SyncJson.Writer, long>,
			ISyncWrite<SyncJson.Writer, byte>,
			ISyncWrite<SyncJson.Writer, ushort>,
			ISyncWrite<SyncJson.Writer, uint>,
			ISyncWrite<SyncJson.Writer, ulong>,
			ISyncWrite<SyncJson.Writer, float>,
			ISyncWrite<SyncJson.Writer, double>,
			ISyncWrite<SyncJson.Writer, BigInteger>,
			ISyncWrite<SyncJson.Writer, char>,
			ISyncWrite<SyncJson.Writer, ReadOnlyMemory<char>>,
			ISyncWrite<SyncJson.Writer, string?>,
			ISyncWrite<SyncJson.Writer, bool?>,
			ISyncWrite<SyncJson.Writer, sbyte?>,
			ISyncWrite<SyncJson.Writer, short?>,
			ISyncWrite<SyncJson.Writer, int?>,
			ISyncWrite<SyncJson.Writer, long?>,
			ISyncWrite<SyncJson.Writer, byte?>,
			ISyncWrite<SyncJson.Writer, ushort?>,
			ISyncWrite<SyncJson.Writer, uint?>,
			ISyncWrite<SyncJson.Writer, ulong?>,
			ISyncWrite<SyncJson.Writer, float?>,
			ISyncWrite<SyncJson.Writer, double?>,
			ISyncWrite<SyncJson.Writer, BigInteger?>,
			ISyncWrite<SyncJson.Writer, char?>,
			ISyncWrite<SyncJson.Writer, ReadOnlyMemory<char>?>
		{
			public void Write(ref SyncJson.Writer w, Symbol? propName, bool x)
				=> w._s.WriteLiteralProp(propName?.Name, x ? _true : _false);
			public void Write(ref SyncJson.Writer w, Symbol? propName, sbyte x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, short x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, int x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, long x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, byte x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, ushort x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, uint x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, ulong x)
				=> w._s.WriteProp(propName?.Name, (long)x, isSigned: false);
			public void Write(ref SyncJson.Writer w, Symbol? propName, float x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, double x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, BigInteger x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, char x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, string? x)
				=> w._s.WriteProp(propName?.Name, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, ReadOnlyMemory<char> x)
				=> w._s.WriteProp(propName?.Name, x.Span);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void WriteNullable<T, This>(SyncJson.Writer w, Symbol? propName, T? x)
				where T : struct
				where This : ISyncWrite<SyncJson.Writer, T>, new() {
				if (x == null)
					w._s.WriteNull(propName?.Name);
				else
					new This().Write(ref w, propName, x.Value);
			}

			public void Write(ref SyncJson.Writer w, Symbol? propName, bool? x)
				=> WriteNullable<bool, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, sbyte? x)
				=> WriteNullable<sbyte, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, short? x)
				=> WriteNullable<short, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, int? x)
				=> WriteNullable<int, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, long? x)
				=> WriteNullable<long, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, byte? x)
				=> WriteNullable<byte, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, ushort? x)
				=> WriteNullable<ushort, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, uint? x)
				=> WriteNullable<uint, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, ulong? x)
				=> WriteNullable<ulong, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, float? x)
				=> WriteNullable<float, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, double? x)
				=> WriteNullable<double, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, BigInteger? x)
				=> WriteNullable<BigInteger, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, char? x)
				=> WriteNullable<char, Helper>(w, propName, x);
			public void Write(ref SyncJson.Writer w, Symbol? propName, ReadOnlyMemory<char>? x)
				=> WriteNullable<ReadOnlyMemory<char>, Helper>(w, propName, x);
		}
	}
}
