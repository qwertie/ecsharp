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
	public class SyncJsonWriterTests
	{
		static string ToNewtonString<T>(JsonSerializer jsonSerializer, T obj)
		{
			var newtonSB = new StringBuilder();
			using (var sw = new StringWriter(newtonSB))
				using (var jtw = new JsonTextWriter(sw))
					jsonSerializer.Serialize(jtw, obj);

			return RestyleNewtonJson(newtonSB.ToString());
		}
		public static string RestyleNewtonJson(string json)
		{
			// Newtonsoft adds .0 on float and double; we don't.
			// Use Replace() so this string can equal ours.
			json = json.Replace(".0,", ",")
				.Replace(".0\r", "\r").Replace(".0\n", "\n").Replace(".0]", "]");

			// Newtonsoft uses lowercase hex, SyncJson uses uppercase
			json = Regex.Replace(json, @"\\u....", m => "\\u" + m.Value.Substring(2).ToUpper());

			// Newtonsoft will write unpaired surrogate code points into its UTF-16 output,
			// but SyncJson cannot reasonably produce the equivalent UTF-8 code points
			// because Encoding.UTF8 refuses to decode such code points. Therefore, SyncJson
			// must use escaped output for these, and we must alter Newton's output to match.
			json = Regex.Replace(json, "[\uD800-\uDBFF](?![\uDC00-\uDFFF])",
				m => "\\u" + ((int)m.Value[0]).ToString("X4"));
			json = Regex.Replace(json, "(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]",
				m => "\\u" + ((int)m.Value[0]).ToString("X4"));

			// Also, SyncJson has a nice compact representation of refs, and 
			// we need to remove some whitespace from the Newton JSON (again,
			// so that this string can equal the one from SyncJson).
			return Regex.Replace(json, @"{[ \r\n]+""\$ref"": ""([0-9]+)""[ \r\n]+}", @"{""$ref"": ""$1""}");
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

			var options = new SyncJson.Options { RootMode = 0, Write = { Indent = "  ", SpaceAfterColon = true } };
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
				RootMode = ObjectMode.Deduplicate,
				NewtonsoftCompatibility = true,
			};
			syncJson = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>(), options);
			Assert.AreEqual(json, syncJson);
		}

		[Test]
		public void NewtonsoftBigStandardModelInterop()
		{
			var obj = new BigStandardModelNoMem(8);

			// We should produce the same output as Newtonsoft except for minor formatting
			// differences that are removed by ToNewtonString.
			var jsonSerializer = new JsonSerializer {
				PreserveReferencesHandling = PreserveReferencesHandling.None,
				Formatting = Formatting.Indented,
			};
			var json = ToNewtonString(jsonSerializer, obj);
			//Console.WriteLine(json);

			var options = new SyncJson.Options {
				RootMode = 0,
				Write = { Indent = "  ", SpaceAfterColon = true }
			};
			var syncJson  = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>(), options);
			var syncJson2 = SyncJson.WriteStringI(obj, new BigStandardModelSync<ISyncManager>().Sync, options);

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
				RootMode = ObjectMode.Deduplicate,
				NewtonsoftCompatibility = true,
			};
			syncJson = SyncJson.WriteString(obj, new BigStandardModelSync<SyncJson.Writer>().Sync, options);
			Assert.AreEqual(json, syncJson);
		}

	
		[Test]
		public void NewtonsoftFamilyInterop_Write()
		{
			var family = Family.DemoFamily();

			// This test writes a Demo Family to JSON in two different ways, either with lists
			// deduplicated or not deduplicated. In both cases we verify that SyncLib and
			// Newtonsoft produce equivalent output (though they don't produce the _same_ output).
			for (int iteration = 1; iteration <= 2; iteration++)
			{
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
					RootMode = ObjectMode.Deduplicate,
					Write = { Indent = "  ", SpaceAfterColon = true }
				};
				var syncHelper = new FamilySync<SyncJson.Writer>((deduplicateLists ? ObjectMode.Deduplicate : 0) | ObjectMode.List);
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
