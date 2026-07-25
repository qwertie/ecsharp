// SyncJsonDOM only exists in .NET Core 3+ builds of Loyc.SyncLib.SyncJson
#if NETCOREAPP3_0_OR_GREATER

using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Loyc.SyncLib.Tests
{
	/// <summary>
	///   Pairs <see cref="SyncJson.Writer"/> with <see cref="SyncJsonDOM.Reader"/>, so
	///   that the whole shared round-trip suite (SyncLibTests and its Values/Fuzz
	///   partials) is written as UTF-8 JSON and read back through a System.Text.Json
	///   <see cref="JsonDocument"/>. The tests in this class itself cover only
	///   DOM-specific behavior.
	/// </summary>
	public class SyncJsonDOMTests : SyncLibTests<SyncJsonDOM.Reader, SyncJson.Writer>
	{
		SyncJson.Options _options = new SyncJson.Options();
		ObjectMode _saveMode;

		public SyncJsonDOMTests(bool newtonCompat, bool nonDefaultSettings = false, bool minify = false)
		{
			if (nonDefaultSettings) {
				_options = new SyncJson.Options {
					NameConverter = SyncJson.ToCamelCase,
					Write = {
						EscapeUnicode = true,
						MaxIndentDepth = 2,
						CharListAsString = false,
						SpaceAfterColon = false,
						Indent = "  ",
						Newline = "\n",
						InitialBufferSize = 1,
					},
					Read = {
						Strict = true,
						AllowComments = false,
					}
				};
				_saveMode = ObjectMode.Deduplicate;
			}
			_options.NewtonsoftCompatibility = newtonCompat;
			_options.Write.Minify = minify;
		}

		protected override T Read<T>(byte[] data, SyncObjectFunc<SyncJsonDOM.Reader, T> sync)
		{
			_options.RootMode = _saveMode;
			return SyncJsonDOM.Read<T>(data, sync, _options)!;
		}

		protected override byte[] Write<T>(T value, SyncObjectFunc<SyncJson.Writer, T> sync, ObjectMode mode)
		{
			_options.RootMode = mode;
			return SyncJson.Write(value, sync, _options).ToArray();
		}

		[Test]
		public void ReadFromSubElementOfLargerDocument()
		{
			// A JsonElement from the middle of a document that the caller parsed
			// (perhaps to decide how to deserialize it) works as a root object.
			// Also, A and B are read in the opposite of their storage order.
			string json = @"{ ""apiVersion"": 9, ""payload"": { ""B"": 22, ""A"": ""ay"" } }";
			using var doc = JsonDocument.Parse(json);
			AreEqual(9, doc.RootElement.GetProperty("apiVersion").GetInt32());

			var pair = SyncJsonDOM.Read<(string A, int B)>(doc.RootElement.GetProperty("payload"), SyncAB);
			AreEqual(("ay", 22), pair);

			static (string A, int B) SyncAB(SyncJsonDOM.Reader sm, (string A, int B) ab)
			{
				ab.A = sm.Sync("A", ab.A) ?? "";
				ab.B = sm.Sync("B", ab.B);
				return ab;
			}
		}

		[Test]
		public void OutOfOrderReadsAndBackRefs()
		{
			// Same idea as SyncJsonReaderTests.YouOnlySkipTwice: B is read before A,
			// and inside A, the fields are read in the order Y, X, Z. The second
			// document also places the "$ref" BEFORE the object that declares the
			// matching "$id" — which the stream-based reader does not support, but a
			// DOM reader can find an id anywhere in the document.
			string[] json = {
				@"{
				   ""A"": {
				      ""X"": { ""$id"": ""9"", ""name"": 111 },
				      ""Y"": 222,
				      ""Z"": { ""$ref"": ""9"" }
				   },
				   ""B"": 333
				}",
				@"{
				   ""B"": 333,
				   ""A"": {
				      ""X"": { ""$ref"": ""9"" },
				      ""Y"": 222,
				      ""Z"": { ""$id"": ""9"", ""name"": 111 }
				   }
				}",
			};
			foreach (string s in json) {
				var obj = SyncJsonDOM.Read<((string? X, int Y, string? Z) A, int B)>(s, SyncRoot);
				AreEqual(333, obj.B);
				AreEqual(222, obj.A.Y);
				AreEqual("111", obj.A.X); // the number 111 is implicitly converted to a string
				AreSame(obj.A.X, obj.A.Z);
			}

			static string SyncName(SyncJsonDOM.Reader sm, string? name)
				=> sm.Sync("name", name) ?? "";
			static (string? X, int Y, string? Z) SyncXYZ(SyncJsonDOM.Reader sm, (string? X, int Y, string? Z) obj)
			{
				obj.Y = sm.Sync("Y", obj.Y);
				obj.X = sm.Sync("X", obj.X, SyncName);
				obj.Z = sm.Sync("Z", obj.Z, SyncName);
				return obj;
			}
			static ((string? X, int Y, string? Z) A, int B) SyncRoot(SyncJsonDOM.Reader sm, ((string? X, int Y, string? Z) A, int B) obj)
			{
				obj.B = sm.Sync("B", obj.B);
				obj.A = sm.Sync("A", obj.A, SyncXYZ);
				return obj;
			}
		}

		[Test]
		public void NextFieldReadingAndErrorRecovery()
		{
			string json = @"{ ""Name"": ""Jackie"", ""Items"": [""Joe"", ""Dan""] }";
			using var doc = JsonDocument.Parse(json);
			var sm = SyncJsonDOM.NewReader(doc);
			AreEqual((true, 1, (object?) null), sm.BeginSubObject(null, null, ObjectMode.NotNull));

			// A failed read of the wrong type must not consume anything
			ThrowsAny<FormatException>(() => sm.SyncArray("Name", (string[]?) null));
			ThrowsAny<FormatException>(() => sm.Sync("Items", 0));
			AreEqual("Name", sm.NextField.Name);
			AreEqual(SyncType.String, sm.GetFieldType("Name"));
			AreEqual(SyncType.List, sm.GetFieldType("Items"));
			AreEqual(SyncType.Missing, sm.GetFieldType("Absent"));

			// Read the fields in the order they appear, without naming them
			AreEqual("Jackie", sm.Sync(null, ""));
			AreEqual("Items", sm.NextField.Name);
			var items = sm.SyncList(null, (List<string>?) null, ObjectMode.NotNull)!;
			ExpectList(items, "Joe", "Dan");
			AreEqual(FieldId.Missing.Name, sm.NextField.Name);

			sm.EndSubObject();
			AreEqual(0, sm.Depth);
		}

		[Test]
		public void MissingAndNullFields()
		{
			string json = @"{ ""A"": ""a"", ""B"": null }";

			// Reading the missing "C" (or the null "B" as a plain int) throws...
			ThrowsAny<FormatException>(() => SyncJsonDOM.ReadI<(string A, int B, int C)>(json, Sync3));
			// ...unless the options forgive it
			var options = new SyncJson.Options {
				Read = { AllowMissingFields = true, ReadNullPrimitivesAsDefault = true }
			};
			var r = SyncJsonDOM.ReadI<(string A, int B, int C)>(json, Sync3, options);
			AreEqual(("a", 0, 0), r);

			static (string A, int B, int C) Sync3(ISyncManager sm, (string A, int B, int C) v)
			{
				v.A = sm.Sync("A", v.A) ?? "";
				v.B = sm.Sync("B", v.B);
				v.C = sm.Sync("C", v.C);
				return v;
			}
		}

		[Test]
		public void ReadsAllByteArrayForms()
		{
			foreach (var json in new[] {
				@"""!hi!""", // BAIS
				@"""" + Convert.ToBase64String(Encoding.UTF8.GetBytes("hi!")) + @"""",
				"[104,105,33]",
				@"{ ""$id"": ""7"", ""$values"": [104,105,33] }",
				@"{ ""\f"": 7, """": [104,105,33] }",
			}) {
				using var doc = JsonDocument.Parse(json);
				var sm = SyncJsonDOM.NewReader(doc);
				ExpectList(sm.SyncArray(null, (byte[]?) null)!, (byte) 'h', (byte) 'i', (byte) '!');
			}
		}
	}
}

#endif
