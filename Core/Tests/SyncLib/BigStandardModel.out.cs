// Generated from BigStandardModel.ecs by LeMP custom tool. LeMP version: 30.1.0.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

using Loyc.SyncLib;

namespace Loyc.SyncLib.Tests
{

	internal class StandardFields
	{
		public bool Bool;
		public sbyte Int8;
		public byte Uint8;
		public short Int16;
		public ushort Uint16;
		public int Int32;
		public uint Uint32;
		public long Int64;
		public ulong Uint64;
		public float Single;
		public double Double;
		public decimal Decimal;
		public BigInteger BigInteger;
		public char Char;
		public string String;
		public bool? BoolNullable;
		public sbyte? Int8Nullable;
		public byte? Uint8Nullable;
		public short? Int16Nullable;
		public ushort? Uint16Nullable;
		public int? Int32Nullable;
		public uint? Uint32Nullable;
		public long? Int64Nullable;
		public ulong? Uint64Nullable;
		public float? SingleNullable;
		public double? DoubleNullable;
		public decimal? DecimalNullable;
		public BigInteger? BigIntegerNullable;
		public char? CharNullable;
		public string? StringNullable;

		public StandardFields(int seed)
		{
			Bool = (seed & 1) != 0;
			BoolNullable = null;
			String = (seed++).ToString();
			Int8 = (sbyte) seed++;
			Int8Nullable = (sbyte) seed++;
			Uint8 = (byte) seed++;
			Uint8Nullable = (byte) seed++;
			Int16 = (short) seed++;
			Int16Nullable = (short) seed++;
			Uint16 = (ushort) seed++;
			Uint16Nullable = (ushort) seed++;
			Int32 = (int) seed++;
			Int32Nullable = (int) seed++;
			Uint32 = (uint) seed++;
			Uint32Nullable = (uint) seed++;
			Int64 = (long) seed++;
			Int64Nullable = (long) seed++;
			Uint64 = (ulong) seed++;
			Uint64Nullable = (ulong) seed++;
			Single = (float) seed++;
			SingleNullable = (float) seed++;
			Double = (double) seed++;
			DoubleNullable = (double) seed++;
			Decimal = (decimal) seed++;
			DecimalNullable = (decimal) seed++;
			BigInteger = (BigInteger) seed++;
			BigIntegerNullable = (BigInteger) seed++;
			Char = (char) seed++;
			CharNullable = (char) seed++;
		}

		public override bool Equals(object other) => other is StandardFields sf && Equals(sf);
		public bool Equals(StandardFields other)
		{
			return other.Bool == Bool 
			&& other.Int8 == Int8 
			&& other.Uint8 == Uint8 
			&& other.Int16 == Int16 
			&& other.Uint16 == Uint16 
			&& other.Int32 == Int32 
			&& other.Uint32 == Uint32 
			&& other.Int64 == Int64 
			&& other.Uint64 == Uint64 
			&& other.Single == Single 
			&& other.Double == Double 
			&& other.Decimal == Decimal 
			&& other.BigInteger == BigInteger 
			&& other.Char == Char 
			&& other.String == String 
			&& other.BoolNullable == BoolNullable 
			&& other.Int8Nullable == Int8Nullable 
			&& other.Uint8Nullable == Uint8Nullable 
			&& other.Int16Nullable == Int16Nullable 
			&& other.Uint16Nullable == Uint16Nullable 
			&& other.Int32Nullable == Int32Nullable 
			&& other.Uint32Nullable == Uint32Nullable 
			&& other.Int64Nullable == Int64Nullable 
			&& other.Uint64Nullable == Uint64Nullable 
			&& other.SingleNullable == SingleNullable 
			&& other.DoubleNullable == DoubleNullable 
			&& other.DecimalNullable == DecimalNullable 
			&& other.BigIntegerNullable == BigIntegerNullable 
			&& other.CharNullable == CharNullable 
			&& other.StringNullable == StringNullable;
		}
	}

	/// This class uses all standard types, their nullable variants,
	/// and most of the supported collections of standard types, 
	/// notably excluding Memory<T> which is not supported by Newtonsoft
	/// (and so interferes with the Newtonsoft compatibility tests).
	/// There are no custom types here, just natively-supported types.
	internal class BigStandardModelNoMem : StandardFields
	{
		public bool[] BoolArray;
		public sbyte[] Int8Array;
		public byte[] Uint8Array;
		public short[] Int16Array;
		public ushort[] Uint16Array;
		public int[] Int32Array;
		public uint[] Uint32Array;
		public long[] Int64Array;
		public ulong[] Uint64Array;
		public float[] SingleArray;
		public double[] DoubleArray;
		public decimal[] DecimalArray;
		public BigInteger[] BigIntegerArray;
		public char[] CharArray;
		public string[] StringArray;
		public List<bool> BoolList;
		public List<sbyte> Int8List;
		public List<byte> Uint8List;
		public List<short> Int16List;
		public List<ushort> Uint16List;
		public List<int> Int32List;
		public List<uint> Uint32List;
		public List<long> Int64List;
		public List<ulong> Uint64List;
		public List<float> SingleList;
		public List<double> DoubleList;
		public List<decimal> DecimalList;
		public List<BigInteger> BigIntegerList;
		public List<char> CharList;
		public List<string> StringList;

		public BigStandardModelNoMem(int seed) : base(seed)
		{
			BoolArray = new bool[0];
			BoolList = new List<bool> { 
				false, true
			};
			//BoolColl = BoolList;
			StringArray = new string[] { 
				"Yarn", "Twine"
			};
			StringList = new List<string> { 
				"Rope", "String"
			};
			//StringColl = StringList;
			Int8 = (sbyte) seed++;
			//concatId($varstem, "Coll") = 
			Int8List = new List<sbyte> { 
				
				(sbyte) seed++, (sbyte) seed++
				
			};
			Int8Array = new sbyte[] { 
				
				(sbyte) seed++, (sbyte) seed++, (sbyte) seed++
				
			};
			Uint8 = (byte) seed++;
			//concatId($varstem, "Coll") = 
			Uint8List = new List<byte> { 
				
				(byte) seed++, (byte) seed++
				
			};
			Uint8Array = new byte[] { 
				
				(byte) seed++, (byte) seed++, (byte) seed++
				
			};
			Int16 = (short) seed++;
			//concatId($varstem, "Coll") = 
			Int16List = new List<short> { 
				
				(short) seed++, (short) seed++
				
			};
			Int16Array = new short[] { 
				
				(short) seed++, (short) seed++, (short) seed++
				
			};
			Uint16 = (ushort) seed++;
			//concatId($varstem, "Coll") = 
			Uint16List = new List<ushort> { 
				
				(ushort) seed++, (ushort) seed++
				
			};
			Uint16Array = new ushort[] { 
				
				(ushort) seed++, (ushort) seed++, (ushort) seed++
				
			};
			Int32 = (int) seed++;
			//concatId($varstem, "Coll") = 
			Int32List = new List<int> { 
				
				(int) seed++, (int) seed++
				
			};
			Int32Array = new int[] { 
				
				(int) seed++, (int) seed++, (int) seed++
				
			};
			Uint32 = (uint) seed++;
			//concatId($varstem, "Coll") = 
			Uint32List = new List<uint> { 
				
				(uint) seed++, (uint) seed++
				
			};
			Uint32Array = new uint[] { 
				
				(uint) seed++, (uint) seed++, (uint) seed++
				
			};
			Int64 = (long) seed++;
			//concatId($varstem, "Coll") = 
			Int64List = new List<long> { 
				
				(long) seed++, (long) seed++
				
			};
			Int64Array = new long[] { 
				
				(long) seed++, (long) seed++, (long) seed++
				
			};
			Uint64 = (ulong) seed++;
			//concatId($varstem, "Coll") = 
			Uint64List = new List<ulong> { 
				
				(ulong) seed++, (ulong) seed++
				
			};
			Uint64Array = new ulong[] { 
				
				(ulong) seed++, (ulong) seed++, (ulong) seed++
				
			};
			Single = (float) seed++;
			//concatId($varstem, "Coll") = 
			SingleList = new List<float> { 
				
				(float) seed++, (float) seed++
				
			};
			SingleArray = new float[] { 
				
				(float) seed++, (float) seed++, (float) seed++
				
			};
			Double = (double) seed++;
			//concatId($varstem, "Coll") = 
			DoubleList = new List<double> { 
				
				(double) seed++, (double) seed++
				
			};
			DoubleArray = new double[] { 
				
				(double) seed++, (double) seed++, (double) seed++
				
			};
			Decimal = (decimal) seed++;
			//concatId($varstem, "Coll") = 
			DecimalList = new List<decimal> { 
				
				(decimal) seed++, (decimal) seed++
				
			};
			DecimalArray = new decimal[] { 
				
				(decimal) seed++, (decimal) seed++, (decimal) seed++
				
			};
			BigInteger = (BigInteger) seed++;
			//concatId($varstem, "Coll") = 
			BigIntegerList = new List<BigInteger> { 
				
				(BigInteger) seed++, (BigInteger) seed++
				
			};
			BigIntegerArray = new BigInteger[] { 
				
				(BigInteger) seed++, (BigInteger) seed++, (BigInteger) seed++
				
			};
			Char = (char) seed++;
			//concatId($varstem, "Coll") = 
			CharList = new List<char> { 
				
				(char) seed++, (char) seed++
				
			};
			CharArray = new char[] { 
				
				(char) seed++, (char) seed++, (char) seed++
				
			};
		}
	}

	/// This class uses all standard types, their nullable variants,
	/// and most of the supported collections of standard types. 
	/// But there are no custom types.
	internal class BigStandardModel : BigStandardModelNoMem
	{
		public Memory<bool> BoolMemory;
		public Memory<sbyte> Int8Memory;
		public Memory<byte> Uint8Memory;
		public Memory<short> Int16Memory;
		public Memory<ushort> Uint16Memory;
		public Memory<int> Int32Memory;
		public Memory<uint> Uint32Memory;
		public Memory<long> Int64Memory;
		public Memory<ulong> Uint64Memory;
		public Memory<float> SingleMemory;
		public Memory<double> DoubleMemory;
		public Memory<decimal> DecimalMemory;
		public Memory<BigInteger> BigIntegerMemory;
		public Memory<char> CharMemory;
		public Memory<string> StringMemory;

		public BigStandardModel(int seed) : base(seed)
		{

			Int8Memory = Int8Array.AsMemory();

			Uint8Memory = Uint8Array.AsMemory();

			Int16Memory = Int16Array.AsMemory();

			Uint16Memory = Uint16Array.AsMemory();

			Int32Memory = Int32Array.AsMemory();

			Uint32Memory = Uint32Array.AsMemory();

			Int64Memory = Int64Array.AsMemory();

			Uint64Memory = Uint64Array.AsMemory();

			SingleMemory = SingleArray.AsMemory();

			DoubleMemory = DoubleArray.AsMemory();

			DecimalMemory = DecimalArray.AsMemory();

			BigIntegerMemory = BigIntegerArray.AsMemory();

			CharMemory = CharArray.AsMemory();
		}
	}
	
	
	struct BigStandardModelSync<S> : ISyncObject<S, StandardFields>, ISyncObject<S, BigStandardModel>, ISyncObject<S, BigStandardModelNoMem> where S: ISyncManager
	
	{
		public BigStandardModel Sync(S sync, BigStandardModel? obj)
		{
			obj = SyncMemory(sync, obj);
			Sync(sync, (BigStandardModelNoMem) obj);
			return obj;
		}

		public BigStandardModelNoMem Sync(S sync, BigStandardModelNoMem? obj)
		{
			obj = SyncLists(sync, obj);
			Sync(sync, (StandardFields) obj);
			return obj;
		}

		public StandardFields Sync(S sync, StandardFields? obj)
		{
			obj = obj ?? new BigStandardModel(0);
			obj.Bool = sync.Sync("Bool", obj.Bool);
			obj.Int8 = sync.Sync("Int8", obj.Int8);
			obj.Uint8 = sync.Sync("Uint8", obj.Uint8);
			obj.Int16 = sync.Sync("Int16", obj.Int16);
			obj.Uint16 = sync.Sync("Uint16", obj.Uint16);
			obj.Int32 = sync.Sync("Int32", obj.Int32);
			obj.Uint32 = sync.Sync("Uint32", obj.Uint32);
			obj.Int64 = sync.Sync("Int64", obj.Int64);
			obj.Uint64 = sync.Sync("Uint64", obj.Uint64);
			obj.Single = sync.Sync("Single", obj.Single);
			obj.Double = sync.Sync("Double", obj.Double);
			obj.Decimal = sync.Sync("Decimal", obj.Decimal);
			obj.BigInteger = sync.Sync("BigInteger", obj.BigInteger);
			obj.Char = sync.Sync("Char", obj.Char);
			obj.String = sync.Sync("String", obj.String);
			obj.BoolNullable = sync.Sync("BoolNullable", obj.BoolNullable);
			obj.Int8Nullable = sync.Sync("Int8Nullable", obj.Int8Nullable);
			obj.Uint8Nullable = sync.Sync("Uint8Nullable", obj.Uint8Nullable);
			obj.Int16Nullable = sync.Sync("Int16Nullable", obj.Int16Nullable);
			obj.Uint16Nullable = sync.Sync("Uint16Nullable", obj.Uint16Nullable);
			obj.Int32Nullable = sync.Sync("Int32Nullable", obj.Int32Nullable);
			obj.Uint32Nullable = sync.Sync("Uint32Nullable", obj.Uint32Nullable);
			obj.Int64Nullable = sync.Sync("Int64Nullable", obj.Int64Nullable);
			obj.Uint64Nullable = sync.Sync("Uint64Nullable", obj.Uint64Nullable);
			obj.SingleNullable = sync.Sync("SingleNullable", obj.SingleNullable);
			obj.DoubleNullable = sync.Sync("DoubleNullable", obj.DoubleNullable);
			obj.DecimalNullable = sync.Sync("DecimalNullable", obj.DecimalNullable);
			obj.BigIntegerNullable = sync.Sync("BigIntegerNullable", obj.BigIntegerNullable);
			obj.CharNullable = sync.Sync("CharNullable", obj.CharNullable);
			obj.StringNullable = sync.Sync("StringNullable", obj.StringNullable);

			return obj;
		}

		public BigStandardModelNoMem SyncLists(S sync, BigStandardModelNoMem? obj)
		{
			obj = obj ?? new BigStandardModelNoMem(0);
			obj.BoolArray = sync.SyncList("BoolArray", obj.BoolArray);
			obj.Int8Array = sync.SyncList("Int8Array", obj.Int8Array);
			obj.Uint8Array = sync.SyncList("Uint8Array", obj.Uint8Array);
			obj.Int16Array = sync.SyncList("Int16Array", obj.Int16Array);
			obj.Uint16Array = sync.SyncList("Uint16Array", obj.Uint16Array);
			obj.Int32Array = sync.SyncList("Int32Array", obj.Int32Array);
			obj.Uint32Array = sync.SyncList("Uint32Array", obj.Uint32Array);
			obj.Int64Array = sync.SyncList("Int64Array", obj.Int64Array);
			obj.Uint64Array = sync.SyncList("Uint64Array", obj.Uint64Array);
			obj.SingleArray = sync.SyncList("SingleArray", obj.SingleArray);
			obj.DoubleArray = sync.SyncList("DoubleArray", obj.DoubleArray);
			obj.DecimalArray = sync.SyncList("DecimalArray", obj.DecimalArray);
			obj.BigIntegerArray = sync.SyncList("BigIntegerArray", obj.BigIntegerArray);
			obj.CharArray = sync.SyncList("CharArray", obj.CharArray);
			obj.StringArray = sync.SyncList("StringArray", obj.StringArray);
			obj.BoolList = sync.SyncList("BoolList", obj.BoolList);
			obj.Int8List = sync.SyncList("Int8List", obj.Int8List);
			obj.Uint8List = sync.SyncList("Uint8List", obj.Uint8List);
			obj.Int16List = sync.SyncList("Int16List", obj.Int16List);
			obj.Uint16List = sync.SyncList("Uint16List", obj.Uint16List);
			obj.Int32List = sync.SyncList("Int32List", obj.Int32List);
			obj.Uint32List = sync.SyncList("Uint32List", obj.Uint32List);
			obj.Int64List = sync.SyncList("Int64List", obj.Int64List);
			obj.Uint64List = sync.SyncList("Uint64List", obj.Uint64List);
			obj.SingleList = sync.SyncList("SingleList", obj.SingleList);
			obj.DoubleList = sync.SyncList("DoubleList", obj.DoubleList);
			obj.DecimalList = sync.SyncList("DecimalList", obj.DecimalList);
			obj.BigIntegerList = sync.SyncList("BigIntegerList", obj.BigIntegerList);
			obj.CharList = sync.SyncList("CharList", obj.CharList);
			obj.StringList = sync.SyncList("StringList", obj.StringList);

			return obj;
		}

		public BigStandardModel SyncMemory(S sync, BigStandardModel? obj)
		{
			obj = obj ?? new BigStandardModel(0);
			obj.BoolMemory = sync.SyncList("BoolMemory", obj.BoolMemory);
			obj.Int8Memory = sync.SyncList("Int8Memory", obj.Int8Memory);
			obj.Uint8Memory = sync.SyncList("Uint8Memory", obj.Uint8Memory);
			obj.Int16Memory = sync.SyncList("Int16Memory", obj.Int16Memory);
			obj.Uint16Memory = sync.SyncList("Uint16Memory", obj.Uint16Memory);
			obj.Int32Memory = sync.SyncList("Int32Memory", obj.Int32Memory);
			obj.Uint32Memory = sync.SyncList("Uint32Memory", obj.Uint32Memory);
			obj.Int64Memory = sync.SyncList("Int64Memory", obj.Int64Memory);
			obj.Uint64Memory = sync.SyncList("Uint64Memory", obj.Uint64Memory);
			obj.SingleMemory = sync.SyncList("SingleMemory", obj.SingleMemory);
			obj.DoubleMemory = sync.SyncList("DoubleMemory", obj.DoubleMemory);
			obj.DecimalMemory = sync.SyncList("DecimalMemory", obj.DecimalMemory);
			obj.BigIntegerMemory = sync.SyncList("BigIntegerMemory", obj.BigIntegerMemory);
			obj.CharMemory = sync.SyncList("CharMemory", obj.CharMemory);
			obj.StringMemory = sync.SyncList("StringMemory", obj.StringMemory);

			return obj;
		}
	}
}