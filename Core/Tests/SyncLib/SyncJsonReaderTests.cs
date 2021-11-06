using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	public class SyncJsonReaderTests : TestHelpers
	{
		[Test]
		public void TrickyBackRefsEtc()
		{
			// The idea of this test is to present the same information in various ways in
			// an effort to confuse the reader
			string[] json = {
				// newline before '{'
				@"
				{
				    ""favorite"": {""\f"":1, ""name"":""Joe""},
				    ""items"": [{""\r"":1}, {""\f"":2, ""name"":""Dan""}]
				}",
				// "favorite" is read before "items", forcing indirect reading of items[0]
				@"// By the way, comment support is enabled by default
				{
				    ""items"": [{""\f"":1, ""name"":""Joe""}, {""\f"":2, ""name"":""Dan""}],
				    ""favorite"": {""\r"":1}
				}",
				// a test in which a JSON obj or list has extra fields after those read by the Sync func
				@"{
				    ""favorite"": {""\f"":1, ""name"":""Joe"", ""unread"": null},
				    ""items"": [
				        { ""\r"": 1 },
				        { ""\f"": 2, ""name"":""Dan""}
				    ],
				    ""ignored"": 111111111111111111111111111111111111111111111111
				}",
				// JSON obj or list has extra fields that include an object with an $id that is used later
				@"{
				    ""ignored"": {
				        ""$id"": ""1"",
				        ""unread"": 0.09e+02,
				        ""name"":""Joe""
				    },
				    ""favorite"": { ""$ref"": ""1"" },
				    ""items"": [
				        { ""$ref"": ""1"" },
				        {
				            ""$id"": ""2"",
				            ""name"":""Dan""
				        }
				    ],
				}",
			};
			for (int i = 0; i < json.Length; i++) {
				var obj = SyncJson.ReadI<ItemsAndFavorite>(json[i], ItemsAndFavorite.Sync)!;
				AreEqual(2, obj.Items.Count);
				AreEqual("Joe", obj.Items[0]);
				AreEqual("Dan", obj.Items[1]);
				AreSame(obj.Favorite, obj.Items[0]);
			}
		}

		static string SyncName<SM>(SM sm, string? name) where SM : ISyncManager
		{
			return sm.Sync("name", name) ?? "";
		}

		class ItemsAndFavorite
		{
			public List<string> Items = new List<string>();
			public string? Favorite;

			public static ItemsAndFavorite Sync(ISyncManager sm, ItemsAndFavorite? value)
			{
				value ??= new ItemsAndFavorite();
				value.Favorite = sm.Sync("favorite", value.Favorite, SyncName);
				value.Items = sm.SyncList("items", value.Items, SyncName, ObjectMode.Deduplicate) ?? new List<string>();
				return value;
			}
		}

		[Test]
		public void YouOnlySkipTwice()
		{
			// A test where the same object is skipped twice.
			// Also, a number is implicitly converted to a string.
			string json = @"
			    {
			       ""A"": {
			          ""X"": { ""$id"": ""9"", ""name"": 111 },
			          ""Y"": 222,
			          ""Z"": { ""$ref"": ""9"" }
			       },
			       ""B"": 333
			    }";

			// We read B, then A, then Y, so object "9" is skipped when
			// reading "B" and again when reading "Y".
			var obj = SyncJson.Read<((string? X, int Y, string? Z) A, int B)>(json, SyncRoot);
			AreEqual(333, obj.B);
			AreEqual(222, obj.A.Y);
			AreEqual("111", obj.A.X);
			AreSame(obj.A.X, obj.A.Z);

			(string? X, int Y, string? Z) SyncXYZ(SyncJson.Reader sm, (string? X, int Y, string? Z) obj)
			{
				obj.Y = sm.Sync("Y", obj.Y);
				obj.X = sm.Sync("X", obj.X, SyncName);
				obj.Z = sm.Sync("Z", obj.Z, SyncName);
				return obj;
			}
			((string? X, int Y, string? Z) A, int B) SyncRoot(SyncJson.Reader sm, ((string? X, int Y, string? Z) A, int B) obj)
			{
				obj.B = sm.Sync("B", obj.B);
				obj.A = sm.Sync("A", obj.A, SyncXYZ);
				return obj;
			}
		}

		[Test]
		public void SkipDeduplicatedAndReadLater1()
		{
			// A test in which a deduplicated object is skipped and then read later:
			string json = @"{ 
				""A"": { ""\f"": 1, ""name"": ""Beatlejuice"" },
				""B"": { ""\r"": 1 },
				""C"": 3
			}";

			// (i) order: A, C, B
			bool read_A_last = false;
			var tuple1 = SyncJson.Read<(string? A, string? B, int C)>(json, Sync);
			// (ii) order: C, B, A
			read_A_last = true;
			var tuple2 = SyncJson.Read<(string? A, string? B, int C)>(json, Sync);

			AreEqual("Beatlejuice", tuple1.A);
			AreEqual("Beatlejuice", tuple1.B);
			AreEqual(tuple1, tuple2);
			AreSame(tuple1.A, tuple1.B);
			AreSame(tuple2.A, tuple2.B);

			(string? A, string? B, int C) Sync(SyncJson.Reader sm, (string? A, string? B, int C) obj)
			{
				if (!read_A_last)
					obj.A = sm.Sync("A", obj.A, SyncName);
				obj.C = sm.Sync("C", obj.C);
				obj.B = sm.Sync("B", obj.B, SyncName);
				if (read_A_last)
					obj.A = sm.Sync("A", obj.A, SyncName);
				return obj;
			}
		}

		[Test]
		public void SkipDeduplicatedAndReadLater2()
		{
			string[] json = {
				@"{
				    ""A"": {
				        ""$id"": ""9"",
				        ""name"": ""Bad Wolf""
				    },
				    ""B"": {
				        ""X"": { ""$ref"": ""9"" },
				    },
				}",
				@"{ 
				    ""B"": {
				        ""X"": {
				            ""$id"": ""9"",
				            ""name"": ""Bad Wolf""
				        },  // this extra comma is not an error without Strict mode
				    },
				    ""A"": { ""$ref"": ""9"" }
				}"
			};

			for (int i = 0; i < json.Length; i++) {
				var tuple = SyncJson.Read<(string? A, string? B)>(json[i], Sync);
				AreEqual("Bad Wolf", tuple.A);
				AreSame(tuple.A, tuple.B);
			}

			(string? A, string? B) Sync(SyncJson.Reader sm, (string? A, string? B) obj)
			{
				obj.A = sm.Sync("A", obj.A, SyncName);
				obj.B = sm.Sync("B", obj.B, SyncX!);
				return obj;
			}
			string? SyncX(SyncJson.Reader sm, string? obj) => sm.Sync("X", obj, SyncName);
		}

		[Test]
		public void EscapedLettersAndMissingOrRearrangedFields()
		{
			var options = new SyncJson.Options { 
				Read = { AllowMissingFields = true }
			};

			string json1 = @"{ ""A"": ""a"", ""B"": 0 }";
			string json2 = @"{ ""A"": ""\u0061"" }";
			string json3 = @"{ ""\u0041"": ""\u0061"" }";
			string json4 = @"{ ""A"": ""a"", ""B"": null }";
			string json5 = @"{ ""?"": -1, ""A"": ""!a!"", ""B"": 123 }";
			string json6 = @"{ ""\f"": 7, ""\r"": 7, ""B"": 123, ""A"": ""!a!"" }";
			string json7 = @"{ ""\u0042"": 123, ""A"": ""!a!"" }";
			var r1 = SyncJson.ReadI<(string A, int B)>(json1, SyncAB);
			var r2 = SyncJson.ReadI<(string A, int B)>(json2, SyncAB, options);
			var r3 = SyncJson.ReadI<(string A, int B)>(json3, SyncAB, options);
			options.Read.ReadNullPrimitivesAsDefault = true;
			var r4 = SyncJson.ReadI<(string A, int B)>(json4, SyncAB, options);
			Assert.AreEqual(r1, r2);
			Assert.AreEqual(r1, r3);
			Assert.AreEqual(r1, r4);
			var r5 = SyncJson.ReadI<(string A, int B)>(json5, SyncAB);
			var r6 = SyncJson.ReadI<(string A, int B)>(json6, SyncAB);
			var r7 = SyncJson.ReadI<(string A, int B)>(json7, SyncAB);
			Assert.AreEqual(r5, r6);
			Assert.AreEqual(r5, r7);
		}

		[Test]
		public void TestMaxDepthLimit()
		{
			// TODO: test recursion MaxDepth limit in (1) normal reading and (2) skipping
			string[] json = {
				@"{ // next first - same order as ReadLinkedList() expects
				    ""rest"": {
				        ""rest"": {
				            ""rest"": {
				                ""rest"": null,
				                ""value"": ""D"",
				            },
				            ""value"": ""C"",
				        },
				        ""value"": ""B"",
				    },
				    ""value"": ""A"",
				}",
				@"{ // value first - reverse of the expected order
				    ""value"": ""A"",
				    ""rest"": {
				        ""value"": ""B"",
				        ""rest"": {
				            ""value"": ""C"",
				            ""rest"": {
				                ""value"": ""D"",
				                ""rest"": null,
				            }
				        }
				    }
				}",
				@"{ // next is missing from last item, AND the deepest nesting is in 
				    // something that is skipped. The skipped thing can cause an exception!
				    ""skipped"": [[[[[[[ 0 ]]]]]]],
				    ""value"": ""A"",
				    ""rest"": {
				        ""value"": ""B"",
				        ""rest"": {
				            ""value"": ""C"",
				            ""rest"": {
				                ""value"": ""D"",
				            }
				        }
				    }
				}",
			};

			var options = new SyncJson.Options { Read = { AllowMissingFields = true } };

			for (int i = 0; i < json.Length; i++) {
				// MaxDepth is barely high enough to read the json
				options.Read.MaxDepth = i == 2 ? 7 : 4;
				options.Read.AllowMissingFields = i == 2;
				var list = ReadLinkedList(SyncJson.NewReader(json[i], options));
				ExpectList(list, "A", "B", "C", "D");

				// MaxDepth is not quite high enough to read the json
				options.Read.MaxDepth--;
				var error = ThrowsAny<FormatException>(() => 
					ReadLinkedList(SyncJson.NewReader(json[i], options)));

				IsTrue(error.Message.Contains("too deeply nested"));
			}

			FVList<string?> ReadLinkedList(ISyncManager sm, FieldId name = default)
			{
				if (sm.GetFieldType(name) is SyncType.Null or SyncType.Missing)
					return new FVList<string?>();
				return sm.Sync(name, default(FVList<string?>), ReadLinkedListCore);
			}

			FVList<string?> ReadLinkedListCore(ISyncManager sm, FVList<string?> list)
			{
				Debug.Assert(sm.IsReading);
				var value = sm.Sync("value", "");
				var rest = ReadLinkedList(sm, "rest");
				return rest.Add(value);
			}
		}

		static (string A, int B) SyncAB(ISyncManager sm, (string A, int B) ab)
		{
			ab.A = sm.Sync("A", ab.A) ?? "";
			ab.B = sm.Sync("B", ab.B);
			return ab;
		}

		[Test]
		public void ReadJustPrimitives()
		{
			var strict = new SyncJson.Options { Read = { Strict = true } };

			var sm = SyncJson.NewReader("1234567890123456789")!;
			AreEqual(1234567890123456789L, sm.Sync(null, 0L));
			// If ".0" is appended, it's parsed as `double` even though a `long` was
			// requested. Therefore, the last 2-3 digits of a long will be incorrect.
			sm = SyncJson.NewReader("1234567890123456789.0")!;
			AreEqual(1234567890123456768L, sm.Sync(null, 0L));

			sm = SyncJson.NewReader("-1234567890123456789")!;
			AreEqual(-1234567890123456789d, sm.Sync(null, 0d));

			sm = SyncJson.NewReader("1234567890123456789")!;
			AreEqual(1234567890123456789m, sm.Sync(null, 0m));

			// This is actually parsed as decimal directly (no precision loss via `double` conversion)
			sm = SyncJson.NewReader("1234567890.1234567899")!;
			AreEqual(1234567890.1234567899m, sm.Sync(null, 0m));
			
			sm = SyncJson.NewReader("true")!;
			AreEqual(true, sm.Sync(null, false));
			
			sm = SyncJson.NewReader(@"""!string!""")!;
			AreEqual("!string!", sm.Sync(null, ""));
			
			sm = SyncJson.NewReader(@"""\n\r\t\f\v\0""")!;
			AreEqual("\n\r\t\f\v\0", sm.Sync(null, ""));
			// \v and \0 are not available in strict mode
			ThrowsAny<FormatException>(() => SyncJson.NewReader(@"""\v""", strict).Sync(null, ""));
			ThrowsAny<FormatException>(() => SyncJson.NewReader(@"""\0""", strict).Sync(null, ""));

			foreach (var json in new[] {
				@"""!hi!""",
				@"""" + Convert.ToBase64String(Encoding.UTF8.GetBytes("hi!")) + @"""",
				"[104,105,33]", // string.Join(",", "hi!".Select(c => (int)c).ToArray())
				@"{ ""$id"": ""7"", ""$values"": [104,105,33] }",
				@"{ ""\f"": 7, """": [104,105,33] }",
			}) {
				sm = SyncJson.NewReader(json)!;
				ExpectList(sm.SyncList(null, (byte[]?)null), (byte)'h', (byte)'i', (byte)'!');
			}
			// Currently, big numbers overflow. DP: it would be nice if clamping were an 
			// option, but that'd require extra checks on the hot path, so I'm disinclined.
			sm = SyncJson.NewReader("1234567890123456789.0")!;
			ThrowsAny<OverflowException>(() => sm.Sync(null, 0));
			// We should be able to read the invalid JSON "1, 2" when VerifyEof is off,
			// since the outermost level is treated as a list (IsInsideList = true)
			sm = SyncJson.NewReader("1, 2", new SyncJson.Options { Read = { VerifyEof = false } })!;
			IsTrue(sm.IsInsideList);
			AreEqual(1, sm.Sync(null, 0));
			AreEqual(2, sm.Sync(null, 0));
			ThrowsAny<FormatException>(() => sm.Sync(null, 0));
		}

		[Test]
		public void VerifiesEof()
		{
			var sm = SyncJson.NewReader("{}, 456")!;
			AreEqual((true, (object?) null), sm.BeginSubObject(null, null, ObjectMode.NotNull));
			var error = ThrowsAny<FormatException>(() => sm.EndSubObject());
			IsTrue(error.Message.Contains("Expected EOF"));
		}

		[Test]
		public void TypeRetryAndNextFieldTests()
		{
			string[] json = {
				@"{
				    ""Name"": {""$id"": 1, ""name"":""Jackie""},
				    ""Items"": []
				}",
				@"{ // Reverse order, plus ignored backref field at the end
				    ""Items"": [""Joe""],
				    ""Name"": {
				        ""$id"": 1,
				        ""name"": ""Jackie""
				    },
				    ""Unused"": {""$ref"": 1}
				}",
				@"{ // Alternate format: Name is just a string
				    ""Name"": ""Jackie"",
				    ""Items"": [""Joe"", ""Dan""]
				}",
				@"{ // Alternate format, reverse order and bonus escape sequence in value
				    ""Items"": [
				        ""Joe Johanneson"",
				        ""Dan Dopplegang"",
				        ""Emily Airheart"",
				        ""Jennifer Juniper""
				    ],
				    ""Name"": ""\u004Aackie"",
				}",
			};
			for (int i = 0; i < json.Length; i++) {
				var obj1 = SyncJson.ReadI<TypeConfusion>(json[i], TypeConfusion.CatchingSync)!;
				var obj2 = SyncJson.ReadI<TypeConfusion>(json[i], TypeConfusion.AnalyticSync)!;
				LessOrEqual(i, obj1.Items.Count);
				AreEqual("Jackie", obj2.Name);
				AreEqual(obj1.Name, obj2.Name);
				ExpectList(obj1.Items, obj2.Items);
			}
		}

		class TypeConfusion
		{
			public string? Name;
			public List<string> Items = new List<string>();

			public static TypeConfusion CatchingSync(ISyncManager sm, TypeConfusion? value)
			{
				value ??= new TypeConfusion();

				// Try to read the wrong type first, to test that the error is recoverable
				try {
					// Name is never a list, so this always fails
					sm.SyncList("Name", Array.Empty<string>());
				} catch (FormatException) { }
				try {
					// The object may have the format {"name":"string"}...
					value.Name = sm.Sync("Name", value.Name, SyncName);
				} catch (FormatException) {
					// ...or the object may simply be a string
					value.Name = sm.Sync("Name", value.Name);
				}

				// Try to read the wrong type first, to test that the error is recoverable
				try {
					sm.Sync("Items", "", SyncName);
				} catch (FormatException) { }
				value.Items = sm.SyncList("Items", value.Items!, ObjectMode.NotNull)!;
				return value;
			}

			public static TypeConfusion AnalyticSync(ISyncManager sm, TypeConfusion? value)
			{
				Debug.Assert(sm.IsReading && sm.SupportsNextField && !sm.NeedsIntegerIds);

				value ??= new TypeConfusion();

				// This synchronizer decides how to read its fields using NextField
				while (sm.NextField != FieldId.Missing) {
					var name = sm.NextField.Name?.ToUpper();
					if (name == "NAME") {
						if (sm.NextFieldType() == SyncType.String)
							value.Name = sm.Sync(null, value.Name);
						else
							value.Name = sm.Sync(null, value.Name, SyncName);
					} else if (name == "ITEMS") {
						value.Items = sm.SyncList(null, value.Items!, ObjectMode.NotNull)!;
					} else {
						break;
					}
				}
				return value;
			}
		}


		// TODO: syntax error tests
		//   These were being handled weirdly:
		//   	@"{
		//		    ""A"": {
		//		        ""$id"": ""9"",
		//		        ""name"": ""Bad Wolf""
		//		    }                                   // error is here (weird message reading B after A)
		//		    ""B"": {
		//		        ""X"": { ""$ref"": ""9"" },
		//		    },
		//		}",
		//		@"{ 
		//		    ""B"": {
		//		        ""X"": {
		//		            ""$id"": ""9"",
		//		            ""name"": ""Bad Wolf""
		//		        },                              // error is here (weird message skipping B to read A)
		//		    },
		//		    ""A"": { ""$ref"": ""9"" }
		//		}"
		// TODO: arrange for all the strings to be read as an scanner wrapped around IEnumerable<byte>
		// TODO: tests of reading JSON strictly and non-strictly
		// TODO: a test in which \r is represented as \u000D and \f is represented as \u0066.
		// TODO: a test in which $ref is represented as \u0024ref and $id is $i\u0064.
		// TODO: a test with $values and with \u0024values
		// TODO (low priority): support $id that is not the first property
	}
}
