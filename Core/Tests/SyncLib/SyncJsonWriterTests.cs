using Loyc.MiniTest;
using Loyc.SyncLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Loyc.SyncLib.Tests
{
	/// <summary>
	/// These tests check whether SyncJson.Writer produces output compatible with Newtonsoft.Json
	/// </summary>
	[TestFixture]
	public class SyncJsonWriterTests : SyncLibWriterTests<SyncJson.Writer>
	{
		static string ToNewtonString<T>(JsonSerializer jsonSerializer, T obj)
		{
			var newtonSB = new StringBuilder();
			using (var sw = new StringWriter(newtonSB))
				using (var jtw = new JsonTextWriter(sw))
					jsonSerializer.Serialize(jtw, obj);

			// Newtonsoft adds .0 on float and double; we don't.
			// Use Replace() so this string can equal ours.
			string json = newtonSB.ToString().Replace(".0,", ",")
				.Replace(".0\r", "\r").Replace(".0\n", "\n").Replace(".0]", "]");

			// Also, SyncJson has a nice compact representation of refs, and 
			// we need to remove some whitespace from the Newton JSON (again,
			// so that this string can equal the one from SyncJson).
			json = Regex.Replace(json, @"{[ \r\n]+""\$ref"": ""([0-9]+)""[ \r\n]+}", @"{""$ref"": ""$1""}");
			return json;
		}

		[Test]
		public void NewtonsoftStandardFieldsInterop()
		{
			var obj = new StandardFields(100);

			var jsonSerializer = new JsonSerializer {
				PreserveReferencesHandling = PreserveReferencesHandling.None,
				Formatting = Formatting.Indented,
			};
			var json = ToNewtonString(jsonSerializer, obj);
			//Console.WriteLine(json);

			var options = new SyncJson.Options { Indent = "  ", SpaceAfterColon = true, RootMode = 0 };
			var syncJson = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>().Sync, options);
			var syncJson2 = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>(), options);

			Assert.AreEqual(syncJson, syncJson2);
			Assert.AreEqual(json, syncJson);

			jsonSerializer = new JsonSerializer
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects,
				ContractResolver = new DefaultContractResolver {
					NamingStrategy = new CamelCaseNamingStrategy()
				},
				Formatting = Formatting.None,
			};
			json = ToNewtonString(jsonSerializer, obj);

			options = new SyncJson.Options(compactMode: true) {
				NameConverter = SyncJson.ToCamelCase,
				RootMode = SubObjectMode.Deduplicate,
				NewtonsoftCompatibility = true,
			};
			syncJson = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>(), options);
			Assert.AreEqual(json, syncJson);
		}

		[Test]
		public void NewtonsoftBigStandardModelInterop()
		{
			var obj = new BigStandardModelNoMem(10);

			// We should produce the same output as Newtonsoft except for minor formatting
			// differences. ToNewtonString already deletes Newton's ".0" suffix on floats;
			// also, Newtonsoft uses lowercase unicode escapes while we use uppercase.
			var jsonSerializer = new JsonSerializer {
				PreserveReferencesHandling = PreserveReferencesHandling.None,
				Formatting = Formatting.Indented,
			};
			var json = ToNewtonString(jsonSerializer, obj).Replace(@"\u001a", @"\u001A");;
			//Console.WriteLine(json);

			var options = new SyncJson.Options { Indent = "  ", SpaceAfterColon = true, RootMode = 0 };
			var syncJson  = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>(), options);
			var syncJson2 = SyncJson.WriteString(obj, (SyncObjectFunc<ISyncManager, BigStandardModelNoMem>) 
			                                          new BigStandardModelSync<ISyncManager>().Sync, options);

			Assert.AreEqual(syncJson, syncJson2);
			Assert.AreEqual(json, syncJson);

			jsonSerializer = new JsonSerializer
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects,
				ContractResolver = new DefaultContractResolver {
					NamingStrategy = new CamelCaseNamingStrategy()
				},
				Formatting = Formatting.None,
			};
			json = ToNewtonString(jsonSerializer, obj).Replace(@"\u001a", @"\u001A");

			options = new SyncJson.Options(compactMode: true) {
				NameConverter = SyncJson.ToCamelCase,
				RootMode = SubObjectMode.Deduplicate,
				NewtonsoftCompatibility = true,
			};
			syncJson = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>().Sync, options);
			Assert.AreEqual(json, syncJson);
		}

	
		[Test]
		public void NewtonsoftFamilyInterop_Write()
		{
			var family = Family.DemoFamily();

			for (int iteration = 1; iteration <= 2; iteration++) {
				bool deduplicateLists = iteration == 2;

				var jsonSerializer = new JsonSerializer
				{
					ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
					PreserveReferencesHandling = deduplicateLists ? 
						PreserveReferencesHandling.All : PreserveReferencesHandling.Objects,
					Formatting = Formatting.Indented,
				};

				string json = ToNewtonString(jsonSerializer, family);

				var options = new SyncJson.Options {
					Indent = "  ",
					SpaceAfterColon = true,
					RootMode = SubObjectMode.Deduplicate
				};
				var syncHelper = new FamilyModel<SyncJson.Writer>((deduplicateLists ? SubObjectMode.Deduplicate : 0) | SubObjectMode.List);
				var syncJson = SyncJson.WriteString(family, syncHelper, options);

				Assert.AreEqual(json, syncJson);
			}
			/* Example Newtonsoft output from iteration 2:
			{
			  "$id": "1",
			  "Parents": {
			    "$id": "2",
			    "$values": [
			      {
			        "$id": "3",
			        "Name": "Mary",
			        "Children": {
			          "$id": "4",
			          "$values": [
			            {
			              "$id": "5",
			              "Name": "Ann",
			              "Father": {
			                "$id": "6",
			                "Name": "John",
			                "Children": {
			                  "$ref": "4"
			                }
			              },
			              "Mother": {
			                "$ref": "3"
			              }
			            },
			            {
			              "$id": "7",
			              "Name": "Barry",
			              "Father": {
			                "$ref": "6"
			              },
			              "Mother": {
			                "$ref": "3"
			              }
			            },
			            {
			              "$id": "8",
			              "Name": "Charlie",
			              "Father": {
			                "$ref": "6"
			              },
			              "Mother": {
			                "$ref": "3"
			              }
			            }
			          ]
			        }
			      },
			      {
			        "$ref": "6"
			      }
			    ]
			  },
			  "Children": {
			    "$ref": "4"
			  }
			}
			*/
		}
	}
}
