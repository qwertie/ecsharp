using Loyc.MiniTest;
using Loyc.SyncLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Loyc.Essentials.Tests
{
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
			return newtonSB.ToString().Replace(".0,", ",")
				.Replace(".0\r", "\r").Replace(".0\n", "\n").Replace(".0]", "]");
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
			Console.WriteLine(json);

			var options = new SyncJson.Options { Indent = "  ", SpaceAfterColon = true, RootMode = 0 };
			var syncJson = SyncJson.WriteString(obj, BigStandardModelSync<SyncJson.Writer>.SyncBasics, options);
			var syncJson2 = SyncJson.WriteString(obj, BigStandardModelSync<ISyncManager>.SyncBasics, options);

			Assert.AreEqual(syncJson, syncJson2);
			Assert.AreEqual(json, syncJson);

			jsonSerializer = new JsonSerializer
			{
				ContractResolver = new DefaultContractResolver {
					NamingStrategy = new CamelCaseNamingStrategy()
				},
				Formatting = Formatting.None,
			};
			json = ToNewtonString(jsonSerializer, obj);

			options = new SyncJson.Options(compactMode: true) {
				NameConverter = SyncJson.ToCamelCase,
				RootMode = 0,
			};
			syncJson = SyncJson.WriteString(obj, BigStandardModelSync<SyncJson.Writer>.SyncBasics, options);
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
			Console.WriteLine(json);

			var options = new SyncJson.Options { Indent = "  ", SpaceAfterColon = true, RootMode = 0 };
			var syncJson = SyncJson.WriteString(obj, BigStandardModelSync<SyncJson.Writer>.SyncBigModelNoMem, options);
			var syncJson2 = SyncJson.WriteString(obj, BigStandardModelSync<ISyncManager>.SyncBigModelNoMem, options);

			Assert.AreEqual(syncJson, syncJson2);
			Assert.AreEqual(json, syncJson);

			jsonSerializer = new JsonSerializer
			{
				ContractResolver = new DefaultContractResolver {
					NamingStrategy = new CamelCaseNamingStrategy()
				},
				Formatting = Formatting.None,
			};
			json = ToNewtonString(jsonSerializer, obj).Replace(@"\u001a", @"\u001A");

			options = new SyncJson.Options(compactMode: true) {
				NameConverter = SyncJson.ToCamelCase,
				RootMode = 0,
			};
			syncJson = SyncJson.WriteString(obj, BigStandardModelSync<SyncJson.Writer>.SyncBigModelNoMem, options);
			Assert.AreEqual(json, syncJson);
		}

		public class Family
		{
			public IList<Parent> Parents;
			public IList<Child> Children;

			//public static Family Sync(ISyncManager sync, Family? obj)
			//{
			//	obj ??= new Family();
			//	obj.Parents = sync.SyncList("Parents", obj.Parents);
			//	obj.Children = sync.SyncList("Children", obj.Children);
			//	return obj;
			//}
		}
		public class Parent
		{
			public string Name { get; set; }
			public IList<Child> Children { get; set; }
		}
		public class Child
		{
			public string Name { get; set; }
			public Parent Father { get; set; }
			public Parent Mother { get; set; }
		}

		public Family NewFamily()
		{
			var dad = new Parent { Name = "John" };
			var mum = new Parent { Name = "Mary" };

			var kid1 = new Child { Name = "Ann", Mother = mum, Father = dad };
			var kid2 = new Child { Name = "Barry", Mother = mum, Father = dad };
			var kid3 = new Child { Name = "Charlie", Mother = mum, Father = dad };

			var listOfKids = new List<Child> {kid1, kid2, kid3};
			dad.Children = listOfKids;
			mum.Children = listOfKids;

			return new Family { Parents = new List<Parent> {mum, dad}, Children = listOfKids };
		}

		[Test]
		public void NewtonsoftFamilyInterop_Write()
		{
			var family = NewFamily();

			var jsonSerializer = new JsonSerializer
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
				PreserveReferencesHandling = PreserveReferencesHandling.All,
				Formatting = Formatting.Indented,
			};

			var newtonSB = new StringBuilder();
			using (var sw = new StringWriter(newtonSB))
				using (var jtw = new JsonTextWriter(sw))
					jsonSerializer.Serialize(jtw, family);

			var result = newtonSB.ToString();
			Console.WriteLine(result);

			/* OUTPUT:
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
