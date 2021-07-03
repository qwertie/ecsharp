// Generated from SyncLibBigStandardModel.ecs by LeMP custom tool. LeMP version: 30.0.5.0
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

namespace Loyc.Essentials.Tests
{

	internal class StandardFields
	{
		public bool Bool;
		public int Int32;
		public uint Uint32;
		public long Int64;
		public ulong Uint64;
		public float Single;
		public double Double;
		public BigInteger BigInteger;
		public char Char;
		public string String;
		public bool? BoolNullable;
		public int? Int32Nullable;
		public uint? Uint32Nullable;
		public long? Int64Nullable;
		public ulong? Uint64Nullable;
		public float? SingleNullable;
		public double? DoubleNullable;
		public BigInteger? BigIntegerNullable;
		public char? CharNullable;
		public string? StringNullable;

		public StandardFields(int seed)
		{
			Bool = (seed & 1) != 0;
			BoolNullable = null;
			String = (seed++).ToString();
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
			BigInteger = (BigInteger) seed++;
			BigIntegerNullable = (BigInteger) seed++;
			Char = (char) seed++;
			CharNullable = (char) seed++;
		}
	}

	/// This class uses all standard types, their nullable variants,
	/// and most of the supported collections of standard types. 
	/// But there are no custom types.
	internal class BigStandardModel : StandardFields
	{
		public bool[] BoolArray;
		public int[] Int32Array;
		public uint[] Uint32Array;
		public long[] Int64Array;
		public ulong[] Uint64Array;
		public float[] SingleArray;
		public double[] DoubleArray;
		public BigInteger[] BigIntegerArray;
		public char[] CharArray;
		public string[] StringArray;
		public Memory<bool> BoolMemory;
		public Memory<int> Int32Memory;
		public Memory<uint> Uint32Memory;
		public Memory<long> Int64Memory;
		public Memory<ulong> Uint64Memory;
		public Memory<float> SingleMemory;
		public Memory<double> DoubleMemory;
		public Memory<BigInteger> BigIntegerMemory;
		public Memory<char> CharMemory;
		public Memory<string> StringMemory;
		public List<bool> BoolList;
		public List<int> Int32List;
		public List<uint> Uint32List;
		public List<long> Int64List;
		public List<ulong> Uint64List;
		public List<float> SingleList;
		public List<double> DoubleList;
		public List<BigInteger> BigIntegerList;
		public List<char> CharList;
		public List<string> StringList;
		public IReadOnlyCollection<bool> BoolColl;
		public IReadOnlyCollection<int> Int32Coll;
		public IReadOnlyCollection<uint> Uint32Coll;
		public IReadOnlyCollection<long> Int64Coll;
		public IReadOnlyCollection<ulong> Uint64Coll;
		public IReadOnlyCollection<float> SingleColl;
		public IReadOnlyCollection<double> DoubleColl;
		public IReadOnlyCollection<BigInteger> BigIntegerColl;
		public IReadOnlyCollection<char> CharColl;
		public IReadOnlyCollection<string> StringColl;

		public BigStandardModel(int seed) : base(seed)
		{
			BoolArray = new bool[0];
			BoolList = new List<bool> { 
				false, true
			};
			BoolColl = BoolList;
			StringArray = new string[] { 
				"Yarn", "Twine"
			};
			StringList = new List<string> { 
				"Rope", "String"
			};
			StringColl = StringList;
			Int32 = (int) seed++;
			Int32Coll = Int32List = new List<int> { 
				
				(int) seed++, (int) seed++
				
			};
			Int32Array = new int[] { 
				
				(int) seed++, (int) seed++, (int) seed++
				
			};
			Int32Memory = Int32Array.AsMemory();
			Uint32 = (uint) seed++;
			Uint32Coll = Uint32List = new List<uint> { 
				
				(uint) seed++, (uint) seed++
				
			};
			Uint32Array = new uint[] { 
				
				(uint) seed++, (uint) seed++, (uint) seed++
				
			};
			Uint32Memory = Uint32Array.AsMemory();
			Int64 = (long) seed++;
			Int64Coll = Int64List = new List<long> { 
				
				(long) seed++, (long) seed++
				
			};
			Int64Array = new long[] { 
				
				(long) seed++, (long) seed++, (long) seed++
				
			};
			Int64Memory = Int64Array.AsMemory();
			Uint64 = (ulong) seed++;
			Uint64Coll = Uint64List = new List<ulong> { 
				
				(ulong) seed++, (ulong) seed++
				
			};
			Uint64Array = new ulong[] { 
				
				(ulong) seed++, (ulong) seed++, (ulong) seed++
				
			};
			Uint64Memory = Uint64Array.AsMemory();
			Single = (float) seed++;
			SingleColl = SingleList = new List<float> { 
				
				(float) seed++, (float) seed++
				
			};
			SingleArray = new float[] { 
				
				(float) seed++, (float) seed++, (float) seed++
				
			};
			SingleMemory = SingleArray.AsMemory();
			Double = (double) seed++;
			DoubleColl = DoubleList = new List<double> { 
				
				(double) seed++, (double) seed++
				
			};
			DoubleArray = new double[] { 
				
				(double) seed++, (double) seed++, (double) seed++
				
			};
			DoubleMemory = DoubleArray.AsMemory();
			BigInteger = (BigInteger) seed++;
			BigIntegerColl = BigIntegerList = new List<BigInteger> { 
				
				(BigInteger) seed++, (BigInteger) seed++
				
			};
			BigIntegerArray = new BigInteger[] { 
				
				(BigInteger) seed++, (BigInteger) seed++, (BigInteger) seed++
				
			};
			BigIntegerMemory = BigIntegerArray.AsMemory();
			Char = (char) seed++;
			CharColl = CharList = new List<char> { 
				
				(char) seed++, (char) seed++
				
			};
			CharArray = new char[] { 
				
				(char) seed++, (char) seed++, (char) seed++
				
			};
			CharMemory = CharArray.AsMemory();
		}
	}

	class BigStandardModelSync<S> where S: ISyncManager
	{
		public static readonly SyncObjectFunc<S, BigStandardModel> SyncBigModel = (sync, obj) => 
		{
			obj = obj ?? new BigStandardModel(0);
			SyncBasics(sync, obj);
			SyncLists(sync, obj);
			return obj;
		};

		public static readonly SyncObjectFunc<S, StandardFields> SyncBasics = SyncBasics_;
		public static StandardFields SyncBasics_(S sync, StandardFields obj)
		{
			obj = obj ?? new BigStandardModel(0);
			obj.Bool = sync.Sync("Bool", obj.Bool);
			obj.Int32 = sync.Sync("Int32", obj.Int32);
			obj.Uint32 = sync.Sync("Uint32", obj.Uint32);
			obj.Int64 = sync.Sync("Int64", obj.Int64);
			obj.Uint64 = sync.Sync("Uint64", obj.Uint64);
			obj.Single = sync.Sync("Single", obj.Single);
			obj.Double = sync.Sync("Double", obj.Double);
			obj.BigInteger = sync.Sync("BigInteger", obj.BigInteger);
			obj.Char = sync.Sync("Char", obj.Char);
			obj.String = sync.Sync("String", obj.String);
			obj.BoolNullable = sync.SyncNullable("BoolNullable", obj.BoolNullable);
			obj.Int32Nullable = sync.SyncNullable("Int32Nullable", obj.Int32Nullable);
			obj.Uint32Nullable = sync.SyncNullable("Uint32Nullable", obj.Uint32Nullable);
			obj.Int64Nullable = sync.SyncNullable("Int64Nullable", obj.Int64Nullable);
			obj.Uint64Nullable = sync.SyncNullable("Uint64Nullable", obj.Uint64Nullable);
			obj.SingleNullable = sync.SyncNullable("SingleNullable", obj.SingleNullable);
			obj.DoubleNullable = sync.SyncNullable("DoubleNullable", obj.DoubleNullable);
			obj.BigIntegerNullable = sync.SyncNullable("BigIntegerNullable", obj.BigIntegerNullable);
			obj.CharNullable = sync.SyncNullable("CharNullable", obj.CharNullable);
			obj.StringNullable = sync.SyncNullable("StringNullable", obj.StringNullable);

			return obj;
		}

		public static BigStandardModel SyncLists(S sync, BigStandardModel obj)
		{
			obj = obj ?? new BigStandardModel(0);
			obj.BoolArray = sync.SyncList("BoolArray", obj.BoolArray);
			obj.Int32Array = sync.SyncList("Int32Array", obj.Int32Array);
			obj.Uint32Array = sync.SyncList("Uint32Array", obj.Uint32Array);
			obj.Int64Array = sync.SyncList("Int64Array", obj.Int64Array);
			obj.Uint64Array = sync.SyncList("Uint64Array", obj.Uint64Array);
			obj.SingleArray = sync.SyncList("SingleArray", obj.SingleArray);
			obj.DoubleArray = sync.SyncList("DoubleArray", obj.DoubleArray);
			obj.BigIntegerArray = sync.SyncList("BigIntegerArray", obj.BigIntegerArray);
			obj.CharArray = sync.SyncList("CharArray", obj.CharArray);
			obj.StringArray = sync.SyncList("StringArray", obj.StringArray);
			obj.BoolMemory = sync.SyncList("BoolMemory", obj.BoolMemory);
			obj.Int32Memory = sync.SyncList("Int32Memory", obj.Int32Memory);
			obj.Uint32Memory = sync.SyncList("Uint32Memory", obj.Uint32Memory);
			obj.Int64Memory = sync.SyncList("Int64Memory", obj.Int64Memory);
			obj.Uint64Memory = sync.SyncList("Uint64Memory", obj.Uint64Memory);
			obj.SingleMemory = sync.SyncList("SingleMemory", obj.SingleMemory);
			obj.DoubleMemory = sync.SyncList("DoubleMemory", obj.DoubleMemory);
			obj.BigIntegerMemory = sync.SyncList("BigIntegerMemory", obj.BigIntegerMemory);
			obj.CharMemory = sync.SyncList("CharMemory", obj.CharMemory);
			obj.StringMemory = sync.SyncList("StringMemory", obj.StringMemory);
			obj.BoolList = sync.SyncList("BoolList", obj.BoolList);
			obj.Int32List = sync.SyncList("Int32List", obj.Int32List);
			obj.Uint32List = sync.SyncList("Uint32List", obj.Uint32List);
			obj.Int64List = sync.SyncList("Int64List", obj.Int64List);
			obj.Uint64List = sync.SyncList("Uint64List", obj.Uint64List);
			obj.SingleList = sync.SyncList("SingleList", obj.SingleList);
			obj.DoubleList = sync.SyncList("DoubleList", obj.DoubleList);
			obj.BigIntegerList = sync.SyncList("BigIntegerList", obj.BigIntegerList);
			obj.CharList = sync.SyncList("CharList", obj.CharList);
			obj.StringList = sync.SyncList("StringList", obj.StringList);
			obj.BoolColl = sync.SyncList("BoolColl", obj.BoolColl);
			obj.Int32Coll = sync.SyncList("Int32Coll", obj.Int32Coll);
			obj.Uint32Coll = sync.SyncList("Uint32Coll", obj.Uint32Coll);
			obj.Int64Coll = sync.SyncList("Int64Coll", obj.Int64Coll);
			obj.Uint64Coll = sync.SyncList("Uint64Coll", obj.Uint64Coll);
			obj.SingleColl = sync.SyncList("SingleColl", obj.SingleColl);
			obj.DoubleColl = sync.SyncList("DoubleColl", obj.DoubleColl);
			obj.BigIntegerColl = sync.SyncList("BigIntegerColl", obj.BigIntegerColl);
			obj.CharColl = sync.SyncList("CharColl", obj.CharColl);
			obj.StringColl = sync.SyncList("StringColl", obj.StringColl);

			return obj;
		}
	}
}