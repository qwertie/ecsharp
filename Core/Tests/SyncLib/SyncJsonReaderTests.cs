using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public class SyncJsonReaderTests
	{
		/*[Test]
		public void EscapedLettersAndMissingOrRearrangedFields()
		{
			var options = new SyncJson.Options { 
				Read = { AllowMissingFields = true }
			};

			string json1 = @"{ ""A"": ""a"", ""B"", 0 }";
			string json2 = @"{ ""A"": ""\u0061"" }";
			string json3 = @"{ ""\u0041"": ""\u0061"" }";
			string json4 = @"{ ""?"": -1, ""A"": ""!a!"", ""B"", 123 }";
			string json5 = @"{ ""\f"": 7, ""\r"": 7, ""B"": 123, ""A"": ""!a!"" }";
			string json6 = @"{ ""\u0062"": 123, ""A"": ""!a!"" }";
			var r1 = SyncJson.ReadI<(string A, int B)>(json1, SyncAB);
			var r2 = SyncJson.ReadI<(string A, int B)>(json2, SyncAB, options);
			var r3 = SyncJson.ReadI<(string A, int B)>(json3, SyncAB, options);
			Assert.AreEqual(r1, r2);
			Assert.AreEqual(r1, r3);
			var r4 = SyncJson.ReadI<(string A, int B)>(json4, SyncAB);
			var r5 = SyncJson.ReadI<(string A, int B)>(json5, SyncAB);
			var r6 = SyncJson.ReadI<(string A, int B)>(json6, SyncAB);
			Assert.AreEqual(r4, r5);
			Assert.AreEqual(r4, r6);
		}

		static (string A, int B) SyncAB(ISyncManager sm, (string A, int B) ab)
		{
			ab.A = sm.Sync("A", ab.A) ?? "";
			ab.B = sm.Sync("B", ab.B);
			return ab;
		}

		[Test]
		public void TrickyBackRef1()
		{
			string json1 = @"
				{
				    ""items"": [{""\f"":1, ""name"":""Joe""}, {""\f"":2, ""name"":""Joe""}]
				    ""favorite"": {""\r"":1}
				}
			";
		}

		static string SyncName(ISyncManager sm, string name)
		{
			return sm.Sync("name", name) ?? "";
		}

		class ItemsAndFavorite
		{
			public List<string> Items = new List<string>();
			public string Favorite;

			public static ItemsAndFavorite Sync(ISyncManager sm, ItemsAndFavorite? value)
			{
				value ??= new ItemsAndFavorite();
				value.Items = sm.SyncList("items", value.Items, SyncName, ObjectMode.Deduplicate) ?? "";
				return value;
			}
		}
		*/

		// TODO: test JSON file that begins with whitespace or comment
		// TODO: test JSON file that is just a primitive
		// TODO: a test in which a JSON obj or list has extra fields after those read by the Sync func
		// TODO: a test in which a JSON obj or list has extra fields that include an object with an $id that is used later
		// TODO: a test in which we read the invalid JSON "1, 2" when VerifyEof is off, which should work anyway
		// TODO: a test that makes Frame.Checkpoint invalid due to Read()
		// TODO: a test in which a deduplicated object is skipped and then read later:
		//       (i) order: A, C, B
		//       (ii) order: C, B, A
		//       { "A": { "\f": 1, "name": "Beatlejuice" }, "B": { "\r": 1 }, "C": 3 }
		// TODO: add a test where the same object is skipped twice.
		//     {
		//        "A": {
		//           "X": { "$id": "9", "field": 111 },
		//           "Y": 222,
		//           "Z": { "$ref": "9" }
		//        },
		//        "B": 333
		//     }
		//     If the user reads B, then A, then Y, object "9" is skipped when
		//     reading "B" and again when reading "Y".
		// TODO: a test in which \r is represented as \u000D and \f is represented as \u0066.
		// TODO: a test in which $ref is represented as \u0024ref and $id is $i\u0064.
		// TODO: a test with $values and with \u0024values
		// TODO: a test that reads "B" before "A" in
		//     {
		//        "A": {
		//           "X": { "$id": "9", "field": 111 },
		//        },
		//        "B": { "$ref": "9" }
		//     }
		//     ensuring that the C# object in property X is the same one in property B.
		// TODO: test recursion MaxDepth limit in (1) normal reading and (2) skipping
		// TODO (low priority): support $id that is not the first property
	}
}
