using Loyc.MiniTest;
using Loyc.SyncLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace Loyc.Essentials.Tests
{
	public abstract class SyncLibTests<Manager>
	{
	}
	
	public abstract class SyncLibWriterTests<Writer> : SyncLibTests<Writer>
	{
		[Test]
		public void Foo()
		{
			
		}
	}

	[TestFixture]
	public class SyncJsonWriterTests : SyncLibWriterTests<SyncJson.Writer>
	{
		[DataContract]
		public class Family
		{
			[DataMember]    public IList<Parent> Parents;
			[DataMember]    public IList<Child> Children;
		}
		[DataContract]
		public class Parent
		{
			[DataMember]    public string Name { get; set; }
			[DataMember]    public IList<Child> Children { get; set; }
		}
		[DataContract]
		public class Child
		{
			[DataMember]    public string Name { get; set; }
			[DataMember]    public Parent Father { get; set; }
			[DataMember]    public Parent Mother { get; set; }
		}

		[Test]	
		public void NewtonsoftInterop()
		{
			var dad = new Parent { Name = "John" };
			var mum = new Parent { Name = "Mary" };

			var kid1 = new Child { Name = "Ann", Mother = mum, Father = dad };
			var kid2 = new Child { Name = "Barry", Mother = mum, Father = dad };
			var kid3 = new Child { Name = "Charlie", Mother = mum, Father = dad };

			var listOfKids = new List<Child> {kid1, kid2, kid3};
			dad.Children = listOfKids;
			mum.Children = listOfKids;

			var family = new Family { Parents = new List<Parent> {mum, dad}, Children = listOfKids };

			var jsonSerializer = new JsonSerializer
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
				PreserveReferencesHandling = PreserveReferencesHandling.All,
				Formatting = Formatting.Indented,
			};

			var sb = new StringBuilder();
			using (var sw = new StringWriter(sb))
				using (var jtw = new JsonTextWriter(sw))
					jsonSerializer.Serialize(jtw, family);

			var result = sb.ToString();
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
