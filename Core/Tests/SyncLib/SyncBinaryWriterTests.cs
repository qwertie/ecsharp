using Loyc.SyncLib.Tests;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Loyc.MiniTest;
using Loyc.SyncLib;
using System.Numerics;
using System.Diagnostics;
using System.Linq;
using Loyc.Collections.Impl;
using Loyc.Compatibility;

namespace Loyc.SyncLib.Tests;

/// <summary>
/// These tests check whether SyncJson.Writer produces output compatible with Newtonsoft.Json
/// </summary>
[TestFixture]
public class SyncBinaryWriterTests : TestHelpers
{
	public List<byte> GetExpectedBinaryEncodingForStandardFieldsWithSeed110(SyncBinary.Markers markers)
	{
		var standardFieldsBinary = new List<byte> {
			// object start marker
			(byte)'(',
			// obj.Bool = sync.Sync("Bool", (seed & 1) != 0);
			0,
			// obj.Int8 = sync.Sync("Int8", seed + 1);
			0b10000000, 111,
			// obj.Uint8 = sync.Sync("Uint8", seed + 3);
			113,
			// obj.Int16 = sync.Sync("Int16", seed + 5);
			0b10000000, 115,
			// obj.Uint16 = sync.Sync("Uint16", seed + 7);
			117,
			// obj.Int32 = sync.Sync("Int32", seed + 9);
			0b10000000, 119,
			// obj.Uint32 = sync.Sync("Uint32", seed + 11);
			121,
			// obj.Int64 = sync.Sync("Int64", seed + 13);
			0b10000000, 123,
			// obj.Uint64 = sync.Sync("Uint64", seed + 15);
			125,
			// obj.Single = sync.Sync("Single", seed + 17); => 0x42FE0000
			0, 0, 0xFE, 0x42,
			// obj.Double = sync.Sync("Double", seed + 19); => 0x4060200000000000
			0, 0, 0, 0, 0, 0x20, 0x60, 0x40,
			// obj.Decimal = sync.Sync("Decimal", seed + 21);
			131, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			// obj.BigInteger = sync.Sync("BigInteger", seed + 23);
			0b10000000, 133,
			// obj.Char = sync.Sync("Char", seed + 25);
			0b10000000, 135,
			// obj.String = sync.Sync("String", seed.ToString());
			(byte)'[', 3, (byte)'1', (byte)'1', (byte)'0', (byte)']',
			// obj.BoolNullable = sync.Sync("BoolNullable", null);
			255,
			// obj.Int8Nullable = sync.Sync("Int8Nullable", seed + 2);
			0b10000000, 112,
			// obj.Uint8Nullable = sync.Sync("Uint8Nullable", seed + 4);
			114,
			// obj.Int16Nullable = sync.Sync("Int16Nullable", seed + 6);
			0b10000000, 116,
			// obj.Uint16Nullable = sync.Sync("Uint16Nullable", seed + 8);
			118,
			// obj.Int32Nullable = sync.Sync("Int32Nullable", seed + 10);
			0b10000000, 120,
			// obj.Uint32Nullable = sync.Sync("Uint32Nullable", seed + 12);
			122,
			// obj.Int64Nullable = sync.Sync("Int64Nullable", seed + 14);
			0b10000000, 124,
			// obj.Uint64Nullable = sync.Sync("Uint64Nullable", seed + 16);
			126,
			// obj.SingleNullable = sync.Sync("SingleNullable", seed + 18);
			0, 0, 0, 67,
			// obj.DoubleNullable = sync.Sync("DoubleNullable", seed + 20);
			0, 0, 0, 0, 0, 64, 96, 64,
			// obj.DecimalNullable = sync.Sync("DecimalNullable", seed + 22);
			132, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			// obj.BigIntegerNullable = sync.Sync("BigIntegerNullable", seed + 24);
			0b10000000, 134,
			// obj.CharNullable = sync.Sync("CharNullable", seed + 26);
			0b10000000, 136,
			// obj.StringNullable = sync.Sync("StringNullable", null);
			255,
			// object end marker
			(byte)')',
		};

		Debug.Assert(standardFieldsBinary.Count(x => x == '[') == 1);
		Debug.Assert(standardFieldsBinary.Count(x => x == ']') == 1);
		if ((markers & SyncBinary.Markers.ListStart) == 0)
			standardFieldsBinary.RemoveAll(x => x == '[');
		if ((markers & SyncBinary.Markers.ListEnd) == 0)
			standardFieldsBinary.RemoveAll(x => x == ']');
		if ((markers & SyncBinary.Markers.ObjectStart) == 0)
			standardFieldsBinary.RemoveAt(0);
		if ((markers & SyncBinary.Markers.ObjectEnd) == 0)
			standardFieldsBinary.RemoveAt(standardFieldsBinary.Count - 1);

		return standardFieldsBinary;
	}

	// TODO: support TestCase(...) attribute
	[Test] public void TestStandardFieldsInterop0() => TestStandardFieldsInterop(0);
	[Test] public void TestStandardFieldsInterop1() => TestStandardFieldsInterop(SyncBinary.Markers.Default);
	[Test] public void TestStandardFieldsInterop2() => TestStandardFieldsInterop(SyncBinary.Markers.ListStart | SyncBinary.Markers.ListEnd);
	[Test] public void TestStandardFieldsInterop3() => TestStandardFieldsInterop(SyncBinary.Markers.ListEnd | SyncBinary.Markers.ObjectEnd);
	public void TestStandardFieldsInterop(SyncBinary.Markers markers)
	{
		var opts = new SyncBinary.Options { Markers = markers };
		var obj = new StandardFields(110);

		var results = SyncBinary.Write(obj, new BigStandardModelSync<SyncBinary.Writer>(), opts);
		var results2 = SyncBinary.Write(obj, new BigStandardModelSync<SyncBinary.Writer>().Sync, opts);

		var expectedBytes = GetExpectedBinaryEncodingForStandardFieldsWithSeed110(markers);
		ExpectList(results.ToArray(), expectedBytes);
		ExpectList(results.ToArray(), results2.ToArray());
	}

	// TODO: Things to test:
	// - Various values of `SyncBinary.Markers` work properly
	// - Check all edge cases of integer serialization
	// - Check (de)serialization of floating-point "null"
	// - Objects nested several levels deep
	// - Deduplication works and changes output
	// - Tuple mode
	// - Root ObjectMode e.g. deduplication
	// - Test based on SyncLibTests, for RoundTripJackAndJill etc.

	//[Test]
	//public void NewtonsoftFamilyInterop_Write()
	//{
	//	var family = Family.DemoFamily();

	//	// This test writes a Demo Family to JSON in two different ways, either with lists
	//	// deduplicated or not deduplicated. In both cases we verify that SyncLib and
	//	// Newtonsoft produce equivalent output (though they don't produce the _same_ output).
	//	for (int iteration = 1; iteration <= 2; iteration++) {
	//		bool deduplicateLists = iteration == 2;

	//		var jsonSerializer = new JsonSerializer {
	//			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
	//			PreserveReferencesHandling = deduplicateLists ?
	//				PreserveReferencesHandling.All : PreserveReferencesHandling.Objects,
	//			Formatting = Formatting.Indented,
	//		};

	//		string json = ToNewtonString(jsonSerializer, family);

	//		var options = new SyncJson.Options {
	//			RootMode = ObjectMode.Deduplicate,
	//			Write = { Indent = "  ", SpaceAfterColon = true }
	//		};
	//		var syncHelper = new FamilySync<SyncJson.Writer>((deduplicateLists ? ObjectMode.Deduplicate : 0) | ObjectMode.List);
	//		var syncJson = SyncJson.WriteString(family, syncHelper, options);

	//		Assert.AreEqual(json, syncJson);
	//	}
	//	/* Example Newtonsoft output from iteration 2:
	//	{
	//	  "$id": "1",
	//	  "Parents": {
	//		"$id": "2",
	//		"$values": [
	//		  {
	//			"$id": "3",
	//			"Name": "Mary",
	//			"Children": {
	//			  "$id": "4",
	//			  "$values": [
	//				{
	//				  "$id": "5",
	//				  "Name": "Ann",
	//				  "Father": {
	//					"$id": "6",
	//					"Name": "John",
	//					"Children": {
	//					  "$ref": "4"
	//					}
	//				  },
	//				  "Mother": {
	//					"$ref": "3"
	//				  }
	//				},
	//				{
	//				  "$id": "7",
	//				  "Name": "Barry",
	//				  "Father": {
	//					"$ref": "6"
	//				  },
	//				  "Mother": {
	//					"$ref": "3"
	//				  }
	//				},
	//				{
	//				  "$id": "8",
	//				  "Name": "Charlie",
	//				  "Father": {
	//					"$ref": "6"
	//				  },
	//				  "Mother": {
	//					"$ref": "3"
	//				  }
	//				}
	//			  ]
	//			}
	//		  },
	//		  {
	//			"$ref": "6"
	//		  }
	//		]
	//	  },
	//	  "Children": {
	//		"$ref": "4"
	//	  }
	//	}
	//	*/
	//}
}
