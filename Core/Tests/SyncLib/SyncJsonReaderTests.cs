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
		public void TrickyBackRef()
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


	}
}
