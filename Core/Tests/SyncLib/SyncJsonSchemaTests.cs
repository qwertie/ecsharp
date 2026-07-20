using Loyc.MiniTest;
using Loyc.SyncLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Loyc.SyncLib.Tests
{
	/// <summary>Tests for <see cref="SyncJson.SchemaWriter"/>, which generates JSON Schema
	/// (draft 2020-12) documents describing the output of <see cref="SyncJson.Writer"/>.</summary>
	[TestFixture]
	public class SyncJsonSchemaTests : Assert
	{
		#region Test models and synchronizers

		class PrimitiveModel
		{
			public int Int; public uint UInt; public double Dbl; public bool Flag;
			public int? MaybeInt; public string? Name; public char Letter; public BigInteger Big;
			public byte[]? Blob; public List<byte>? ByteList; public char[]? Chars; public bool[]? Bools;
			public int Bits;
		}

		static PrimitiveModel SyncPrim<SM>(SM sm, PrimitiveModel? m) where SM : ISyncManager
		{
			m ??= new PrimitiveModel();
			m.Int = sm.Sync("Int", m.Int);
			m.UInt = sm.Sync("UInt", m.UInt);
			m.Dbl = sm.Sync("Dbl", m.Dbl);
			m.Flag = sm.Sync("Flag", m.Flag);
			m.MaybeInt = sm.Sync("MaybeInt", m.MaybeInt);
			m.Name = sm.Sync("Name", m.Name);
			m.Letter = sm.Sync("Letter", m.Letter);
			m.Big = sm.Sync("Big", m.Big);
			m.Blob = sm.SyncArray("Blob", m.Blob);
			m.ByteList = sm.SyncList("ByteList", m.ByteList);
			m.Chars = sm.SyncArray("Chars", m.Chars);
			m.Bools = sm.SyncArray("Bools", m.Bools);
			m.Bits = sm.Sync("Bits", m.Bits, 5, signed: false);
			return m;
		}

		class TupleHolder { public int[]? Triple; }

		static TupleHolder SyncTuples(ISyncManager sm, TupleHolder? h)
		{
			h ??= new TupleHolder();
			// A "tuple" of two different types, written the low-level way
			var (begun, _, _) = sm.BeginSubObject("Pair", null, ObjectMode.NotNull | ObjectMode.Tuple, 2);
			if (begun) {
				sm.Sync(null, 0);
				sm.Sync(null, "");
				sm.EndSubObject();
			}
			// A fixed-length list of a single type
			h.Triple = sm.SyncArray("Triple", h.Triple, ObjectMode.Tuple, 3);
			return h;
		}

		class Shape { public double Size; }

		static Shape SyncShape(ISyncManager sm, Shape? s)
		{
			sm.SyncTypeTag("Ellipse");
			s ??= new Shape();
			s.Size = sm.Sync("Size", s.Size);
			return s;
		}

		class Widget { public int A; public string? B; }
		class WidgetHolder { public Widget? W1, W2; }

		static Widget SyncWidgetA(ISyncManager sm, Widget? w)
		{
			w ??= new Widget();
			w.A = sm.Sync("A", w.A);
			return w;
		}
		static Widget SyncWidgetB(ISyncManager sm, Widget? w)
		{
			w ??= new Widget();
			w.B = sm.Sync("B", w.B);
			return w;
		}
		static WidgetHolder SyncHolderConsistent(ISyncManager sm, WidgetHolder? h)
		{
			h ??= new WidgetHolder();
			h.W1 = sm.Sync("W1", h.W1, SyncWidgetA);
			h.W2 = sm.Sync("W2", h.W2, SyncWidgetA);
			return h;
		}
		static WidgetHolder SyncHolderConflicting(ISyncManager sm, WidgetHolder? h)
		{
			h ??= new WidgetHolder();
			h.W1 = sm.Sync("W1", h.W1, SyncWidgetA);
			h.W2 = sm.Sync("W2", h.W2, SyncWidgetB);
			return h;
		}

		struct PointS { public int X, Y; }
		class PointHolder { public PointS P1, P2, P3; }

		static PointS SyncPoint(ISyncManager sm, PointS p)
		{
			p.X = sm.Sync("X", p.X);
			p.Y = sm.Sync("Y", p.Y);
			return p;
		}
		static PointHolder SyncPoints(ISyncManager sm, PointHolder? h)
		{
			h ??= new PointHolder();
			h.P1 = sm.Sync("P1", h.P1, SyncPoint, ObjectMode.Deduplicate);
			h.P2 = sm.Sync("P2", h.P2, SyncPoint, ObjectMode.Deduplicate);
			// NotNull without Deduplicate is the boxing-avoidance mode, in which
			// ObjectSyncher passes typeof(T) as childKey in every SyncMode
			h.P3 = sm.Sync("P3", h.P3, SyncPoint, ObjectMode.NotNull);
			return h;
		}

		#endregion

		static SyncJson.Options Minified(bool newtonsoftCompat = true)
			=> new SyncJson.Options(compactMode: true) { NewtonsoftCompatibility = newtonsoftCompat };

		/// <summary>Checks the schema against expectations and also verifies that it is
		/// well-formed JSON (using Newtonsoft as an independent parser).</summary>
		static void CheckSchema(string expected, string actual)
		{
			JToken.Parse(actual);
			AreEqual(expected, actual);
		}

		[Test]
		public void PrimitiveFieldsSchema()
		{
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/PrimitiveModel"",""$defs"":{""PrimitiveModel"":{""type"":""object"",""properties"":{""Int"":{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647},""UInt"":{""type"":""integer"",""minimum"":0,""maximum"":4294967295},""Dbl"":{""type"":""number""},""Flag"":{""type"":""boolean""},""MaybeInt"":{""type"":[""integer"",""null""],""minimum"":-2147483648,""maximum"":2147483647},""Name"":{""type"":[""string"",""null""]},""Letter"":{""type"":""string"",""minLength"":1,""maxLength"":1},""Big"":{""type"":""integer""},""Blob"":{""type"":[""string"",""null""],""contentEncoding"":""base64""},""ByteList"":{""type"":[""array"",""null""],""items"":{""type"":""integer"",""minimum"":0,""maximum"":255}},""Chars"":{""type"":[""array"",""null""],""items"":{""type"":""string"",""minLength"":1,""maxLength"":1}},""Bools"":{""type"":[""array"",""null""],""items"":{""type"":""boolean""}},""Bits"":{""type"":""integer"",""minimum"":0,""maximum"":31}},""required"":[""Int"",""UInt"",""Dbl"",""Flag"",""MaybeInt"",""Name"",""Letter"",""Big"",""Blob"",""ByteList"",""Chars"",""Bools"",""Bits""]}}}",
				SyncJson.WriteSchemaStringI<PrimitiveModel>(SyncPrim, Minified()));
		}

		[Test]
		public void PrimitiveFieldsSchema_NonNewtonsoft()
		{
			// Byte arrays, byte lists and char lists all become (BAIS) strings
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/PrimitiveModel"",""$defs"":{""PrimitiveModel"":{""type"":""object"",""properties"":{""Int"":{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647},""UInt"":{""type"":""integer"",""minimum"":0,""maximum"":4294967295},""Dbl"":{""type"":""number""},""Flag"":{""type"":""boolean""},""MaybeInt"":{""type"":[""integer"",""null""],""minimum"":-2147483648,""maximum"":2147483647},""Name"":{""type"":[""string"",""null""]},""Letter"":{""type"":""string"",""minLength"":1,""maxLength"":1},""Big"":{""type"":""integer""},""Blob"":{""type"":[""string"",""null""]},""ByteList"":{""type"":[""string"",""null""]},""Chars"":{""type"":[""string"",""null""]},""Bools"":{""type"":[""array"",""null""],""items"":{""type"":""boolean""}},""Bits"":{""type"":""integer"",""minimum"":0,""maximum"":31}},""required"":[""Int"",""UInt"",""Dbl"",""Flag"",""MaybeInt"",""Name"",""Letter"",""Big"",""Blob"",""ByteList"",""Chars"",""Bools"",""Bits""]}}}",
				SyncJson.WriteSchemaStringI<PrimitiveModel>(SyncPrim, Minified(false)));
		}

		[Test]
		public void NameConverterIsApplied()
		{
			var options = Minified();
			options.NameConverter = SyncJson.ToCamelCase;
			string schema = SyncJson.WriteSchemaStringI<PrimitiveModel>(SyncPrim, options);
			JToken.Parse(schema);
			// Property names are converted, but schema keywords and type names are not
			Contains(@"""maybeInt"":{""type"":[""integer"",""null""]", schema);
			Contains(@"""$ref"":""#/$defs/PrimitiveModel""", schema);
			Contains(@"""required"":[""int"",""uInt"",""dbl"",""flag"",""maybeInt"",""name"",""letter"",""big"",""blob"",""byteList"",""chars"",""bools"",""bits""]", schema);
		}

		[Test]
		public void ByteArrayModesAffectSchema()
		{
			var base64 = Minified(false);
			base64.ByteArrayMode = JsonByteArrayMode.Base64;
			string schema = SyncJson.WriteSchemaStringI<PrimitiveModel>(SyncPrim, base64);
			JToken.Parse(schema);
			Contains(@"""Blob"":{""type"":[""string"",""null""],""contentEncoding"":""base64""}", schema);
			Contains(@"""ByteList"":{""type"":[""string"",""null""],""contentEncoding"":""base64""}", schema);

			var array = Minified(false);
			array.ByteArrayMode = JsonByteArrayMode.Array;
			schema = SyncJson.WriteSchemaStringI<PrimitiveModel>(SyncPrim, array);
			JToken.Parse(schema);
			Contains(@"""Blob"":{""type"":[""array"",""null""],""items"":{""type"":""integer"",""minimum"":0,""maximum"":255}}", schema);
		}

		[Test]
		public void FamilySchema()
		{
			// The Family object graph is cyclic (Parent <-> Child) and uses deduplication,
			// so the schema must use $defs/$ref for the types, describe the Newtonsoft-style
			// {"$id", "$values"} wrapper for deduplicated lists, and allow a back-reference
			// (or null) anywhere a deduplicated object can appear.
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/Family"",""$defs"":{""Family"":{""type"":""object"",""properties"":{""Parents"":{""anyOf"":[{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""$values"":{""type"":""array"",""items"":{""anyOf"":[{""$ref"":""#/$defs/Parent""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}}},""required"":[""$id"",""$values""]},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""Children"":{""anyOf"":[{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""$values"":{""type"":""array"",""items"":{""anyOf"":[{""$ref"":""#/$defs/Child""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}}},""required"":[""$id"",""$values""]},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""Parents"",""Children""]},""backReference"":{""type"":""object"",""properties"":{""$ref"":{""type"":""string""}},""required"":[""$ref""]},""Parent"":{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""Name"":{""type"":[""string"",""null""]},""Children"":{""anyOf"":[{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""$values"":{""type"":""array"",""items"":{""anyOf"":[{""$ref"":""#/$defs/Child""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}}},""required"":[""$id"",""$values""]},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""Name"",""Children""]},""Child"":{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""Name"":{""type"":[""string"",""null""]},""Father"":{""anyOf"":[{""$ref"":""#/$defs/Parent""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""Mother"":{""anyOf"":[{""$ref"":""#/$defs/Parent""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""Name"",""Father"",""Mother""]}}}",
				SyncJson.WriteSchemaString<Family, FamilySync<SyncJson.SchemaWriter>>(
					new FamilySync<SyncJson.SchemaWriter>(ObjectMode.Deduplicate), Minified()));
		}

		[Test]
		public void FamilySchema_NonNewtonsoft()
		{
			// In non-Newtonsoft mode the markers are "\f" (id), "\r" (backref) and "" (values)
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/Family"",""$defs"":{""Family"":{""type"":""object"",""properties"":{""Parents"":{""anyOf"":[{""type"":""object"",""properties"":{""\f"":{""type"":""integer""},"""":{""type"":""array"",""items"":{""anyOf"":[{""$ref"":""#/$defs/Parent""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}}},""required"":[""\f"",""""]},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""Children"":{""anyOf"":[{""type"":""object"",""properties"":{""\f"":{""type"":""integer""},"""":{""type"":""array"",""items"":{""anyOf"":[{""$ref"":""#/$defs/Child""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}}},""required"":[""\f"",""""]},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""Parents"",""Children""]},""backReference"":{""type"":""object"",""properties"":{""\r"":{""type"":""integer""}},""required"":[""\r""]},""Parent"":{""type"":""object"",""properties"":{""\f"":{""type"":""integer""},""Name"":{""type"":[""string"",""null""]},""Children"":{""anyOf"":[{""type"":""object"",""properties"":{""\f"":{""type"":""integer""},"""":{""type"":""array"",""items"":{""anyOf"":[{""$ref"":""#/$defs/Child""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}}},""required"":[""\f"",""""]},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""Name"",""Children""]},""Child"":{""type"":""object"",""properties"":{""\f"":{""type"":""integer""},""Name"":{""type"":[""string"",""null""]},""Father"":{""anyOf"":[{""$ref"":""#/$defs/Parent""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""Mother"":{""anyOf"":[{""$ref"":""#/$defs/Parent""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""Name"",""Father"",""Mother""]}}}",
				SyncJson.WriteSchemaString<Family, FamilySync<SyncJson.SchemaWriter>>(
					new FamilySync<SyncJson.SchemaWriter>(ObjectMode.Deduplicate), Minified(false)));
		}

		[Test]
		public void TupleSchemas()
		{
			// A tuple of mixed types uses prefixItems; a fixed-length list of one type
			// is simplified to items + minItems/maxItems.
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/TupleHolder"",""$defs"":{""TupleHolder"":{""type"":""object"",""properties"":{""Pair"":{""type"":""array"",""prefixItems"":[{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647},{""type"":[""string"",""null""]}],""items"":false,""minItems"":2,""maxItems"":2},""Triple"":{""type"":[""array"",""null""],""items"":{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647},""minItems"":3,""maxItems"":3}},""required"":[""Pair"",""Triple""]}}}",
				SyncJson.WriteSchemaStringI<TupleHolder>(SyncTuples, Minified()));
		}

		[Test]
		public void SyncTypeTagRenamesDefinitionAndAddsConst()
		{
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/Ellipse"",""$defs"":{""Ellipse"":{""type"":""object"",""properties"":{""$type"":{""type"":""string"",""const"":""Ellipse""},""Size"":{""type"":""number""}},""required"":[""$type"",""Size""]}}}",
				SyncJson.WriteSchemaStringI<Shape>(SyncShape, Minified()));
		}

		[Test]
		public void SameTypeTwiceProducesOneDefinition()
		{
			// Two fields of the same type (synced the same way) share a $defs entry.
			// The Widget definition gains an optional "$id" because Deduplicate mode
			// (the default of the Sync extension method) was used.
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/WidgetHolder"",""$defs"":{""WidgetHolder"":{""type"":""object"",""properties"":{""W1"":{""anyOf"":[{""$ref"":""#/$defs/Widget""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""W2"":{""anyOf"":[{""$ref"":""#/$defs/Widget""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]}},""required"":[""W1"",""W2""]},""Widget"":{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""A"":{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647}},""required"":[""A""]},""backReference"":{""type"":""object"",""properties"":{""$ref"":{""type"":""string""}},""required"":[""$ref""]}}}",
				SyncJson.WriteSchemaStringI<WidgetHolder>(SyncHolderConsistent, Minified()));
		}

		[Test]
		public void ConflictingSchemasForOneTypeAreDetected()
		{
			var e = ThrowsAny<InvalidOperationException>(() =>
				SyncJson.WriteSchemaStringI<WidgetHolder>(SyncHolderConflicting, Minified()));
			Contains("Widget", e.Message);
			Contains("conflicting", e.Message);
		}

		[Test]
		public void StructsCanBeSkippedWhenSchemaIsKnown()
		{
			// Three things are checked here. (1) The second occurrence of PointS is
			// declined by BeginSubObject; this must not crash even though PointS is a
			// value type (SchemaState returns a boxed default value that ObjectSyncher
			// can cast to PointS). (2) P1/P2 pass a boxed PointS as childKey (a "sample
			// instance"), while P3 passes typeof(PointS) (boxing-avoidance mode); both
			// must map to the same $defs entry. (3) P3's NotNull mode means its schema
			// is a plain $ref: not nullable, no back-reference.
			CheckSchema(
				@"{""$schema"":""https://json-schema.org/draft/2020-12/schema"",""$ref"":""#/$defs/PointHolder"",""$defs"":{""PointHolder"":{""type"":""object"",""properties"":{""P1"":{""anyOf"":[{""$ref"":""#/$defs/PointS""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""P2"":{""anyOf"":[{""$ref"":""#/$defs/PointS""},{""$ref"":""#/$defs/backReference""},{""type"":""null""}]},""P3"":{""$ref"":""#/$defs/PointS""}},""required"":[""P1"",""P2"",""P3""]},""PointS"":{""type"":""object"",""properties"":{""$id"":{""type"":""string""},""X"":{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647},""Y"":{""type"":""integer"",""minimum"":-2147483648,""maximum"":2147483647}},""required"":[""X"",""Y""]},""backReference"":{""type"":""object"",""properties"":{""$ref"":{""type"":""string""}},""required"":[""$ref""]}}}",
				SyncJson.WriteSchemaStringI<PointHolder>(SyncPoints, Minified()));
		}

		[Test]
		public void PrettyPrintedSchema()
		{
			var options = new SyncJson.Options();
			options.Write.Newline = "\n";
			AreEqual(
				"{\n" +
				"\t\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\n" +
				"\t\"$ref\":\"#/$defs/Ellipse\",\n" +
				"\t\"$defs\":{\n" +
				"\t\t\"Ellipse\":{\n" +
				"\t\t\t\"type\":\"object\",\n" +
				"\t\t\t\"properties\":{\n" +
				"\t\t\t\t\"$type\":{\"type\":\"string\",\"const\":\"Ellipse\"},\n" +
				"\t\t\t\t\"Size\":{\"type\":\"number\"}\n" +
				"\t\t\t},\n" +
				"\t\t\t\"required\":[\"$type\",\"Size\"]\n" +
				"\t\t}\n" +
				"\t}\n" +
				"}",
				SyncJson.WriteSchemaStringI<Shape>(SyncShape, options));
		}

		[Test]
		public void AnonymousRecursionIsDetected()
		{
			// A recursive object synced via BeginSubObject with childKey == null cannot
			// be recognized when it recurs, so the schema saver must detect the runaway
			// recursion and throw rather than overflow the stack.
			void Recurse(ISyncManager sm)
			{
				if (sm.BeginSubObject("X", null, ObjectMode.NotNull).Begun) {
					Recurse(sm);
					sm.EndSubObject();
				}
			}
			var e = ThrowsAny<InvalidOperationException>(() => {
				var schema = SyncJson.NewSchemaWriter(Minified());
				Recurse(schema);
			});
			Contains("childKey", e.Message);
		}

		static void Contains(string expectedSubstring, string actual)
		{
			if (!actual.Contains(expectedSubstring))
				Fail("String does not contain expected substring:\n  substring: {0}\n  actual: {1}", expectedSubstring, actual);
		}
	}
}
