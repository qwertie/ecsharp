using Loyc.Collections;
using Loyc.MiniTest;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Loyc.Syntax.Tests
{
	[TestFixture]
	public class StandardLiteralHandlersTests
	{
		static StandardLiteralHandlers SLH = StandardLiteralHandlers.Value;

		static Triplet<Symbol, string, object> T(string typeMarker, string text, object value) => 
			Triplet.Create((Symbol)typeMarker, text, value);

		static Triplet<Symbol, string, object>[] TestItems = new Triplet<Symbol, string, object>[] {
			T("_i8",  "-120",  (SByte)(-120)),
			T("_i16", "-121",  (Int16)(-121)),
			T("_i32", "-122",  (Int32)(-122)),
			T("_i64", "-123",  (Int64)(-123)),
			T("_L",   "-124",  (Int64)(-124)),
			T("_u8",   "125",  (Byte)   125),
			T("_u16",  "126",  (UInt16) 126),
			T("_u32",  "127",  (UInt32) 127),
			T("_u64",  "128",  (UInt64) 128),
			T("_uL",   "129",  (UInt64) 129),
			T("_z",    "130",  (BigInteger) 130),
			T("_f",    "131",  (Single) 131),
			T("_r32", "-132", -(Single) 132),
			T("_d",    "133",  (Double) 133),
			T("_r64", "-134", -(Double) 134),
			T("c",     "x",    (char)'x'),
			T("c",     "✅",   (char)'✅'),
			T("c",     "💩",   "💩"),        // \U1F4A9
			T("",      "hi✋", (string)"hi✋"),
			T("s",     "sym",  (Symbol)"sym"),
			T("void",  "",     @void.Value),
			T("bool",  "TRUE",  true),
			T("bool",  "False", false),
			T("bais",  "Cat\b`@iE?tEB!CD", new byte[] { 67, 97, 116, 128, 10, 69, 255, 65, 66, 67, 68 }),
		};

		[Test]
		public void TestStandardParsers()
		{
			Assert.IsFalse(SLH.CanParse((Symbol)"other"));
			Assert.IsTrue(SLH.TryParse("blah", (Symbol)"other").Right.HasValue);

			Assert.IsTrue(SLH.CanParse((Symbol)"_"), "CanParse _?");
			Assert.AreEqual(0, SLH.TryParse("__0__", (Symbol)"_").Left.Value);
			Assert.AreEqual(1234567890, SLH.TryParse("__123'456'789_0__", (Symbol)"_").Left.Value);
			Assert.AreEqual(123456789012345L, SLH.TryParse("123456789012345", (Symbol)"_").Left.Value);
			Assert.AreEqual(BigInteger.Parse("123456789012345678901234567890"), SLH.TryParse("1234567890_1234567890_1234567890", (Symbol)"_").Left.Value);

			Assert.IsTrue(SLH.CanParse((Symbol)"_u"), "CanParse _u?");
			Assert.AreEqual(1234567890u, SLH.TryParse("'''123'_456_'7890'''", (Symbol)"_u").Left.Value);

			foreach (var item in TestItems)
			{
				Assert.IsTrue(SLH.CanParse(item.Item1), "CanParse was false for {0}", item.Item1);
				var result = SLH.TryParse(item.Item2, item.Item1);
				Assert.IsTrue(result.Left.HasValue, "TryParse failed for {0} {1}", item.Item1, item.Item2);
				Assert.AreEqual(item.Item3.GetType(), result.Left.Value.GetType());
				if (result.Left.Value is byte[] bytes)
					Loyc.Collections.Impl.TestHelpers.ExpectList((byte[]) item.Item3, bytes);
				else
					Assert.AreEqual(item.Item3, result.Left.Value);
			}
		}

		static LiteralNode CL(object? value, string? symbol) => 
			LNode.Literal(SourceRange.Synthetic, new LiteralValue(value, (Symbol?)symbol));

		[Test]
		public void TestStandardPrinters()
		{
			var sb = new StringBuilder();
			Assert.IsFalse(SLH.CanPrint((Symbol)"other"));
			
			// Try to print null
			Assert.AreEqual(null, SLH.TryPrint(CL(null, null), sb).Left.Or((Symbol)"Err")?.Name);
			Assert.AreEqual("", sb.ToString());
			Assert.AreEqual("xy", SLH.TryPrint(CL(null, "xy"), sb).Left.Or((Symbol)"Err")?.Name);
			Assert.AreEqual("", sb.ToString());

			Assert.IsTrue(SLH.TryPrint(LNode.Literal(new MemoryStream()), sb).Right.Or(null)?.Format.Contains("no printer") ?? false);

			// Note: the printers are registered for types rather than type markers,
			//       that's why CanPrint is false for "_" and "_u".
			Assert.IsFalse(SLH.CanPrint((Symbol)"_"), "Can print _?");
			Assert.IsFalse(SLH.CanPrint((Symbol)"_u"), "Can print _u?");
			Assert.IsTrue(SLH.CanPrint(typeof(float))  , "Can't print float?");
			Assert.IsTrue(SLH.CanPrint(typeof(decimal)), "Can't print decimal?");
			Assert.IsTrue(SLH.CanPrint(typeof(Regex))  , "Can't print Regex?");

			Assert.AreEqual((Symbol)"_", SLH.TryPrint(CL(77, null), sb).Left.Or(null));
			Assert.AreEqual("77", sb.ToString());
			Assert.AreEqual((Symbol)"_u", SLH.TryPrint(CL(99u, null), sb).Left.Or(null));
			Assert.AreEqual("99", sb.ToString());
			Assert.AreEqual("_", SLH.TryPrint(CL(123, "_"), sb).Left.Or(null).Name);
			Assert.AreEqual("123", sb.ToString());
			Assert.AreEqual("_u", SLH.TryPrint(CL(246u, "_u"), sb).Left.Or(null).Name);
			Assert.AreEqual("246", sb.ToString());
			Assert.AreEqual("_", SLH.TryPrint(CL(7.125, null), sb).Left.Or(null).Name);
			Assert.AreEqual("7.125", sb.ToString());
			Assert.AreEqual("_", SLH.TryPrint(CL(1.0, null), sb).Left.Or(null).Name);
			Assert.AreEqual("1.0", sb.ToString());

			Assert.AreEqual((Symbol)"_m", SLH.TryPrint(CL(1.5m, "_m"), sb).Left.Or(null));
			Assert.AreEqual((Symbol)"_m", SLH.TryPrint(CL(1.5m, null), sb).Left.Or(null));
			Assert.AreEqual("1.5", sb.ToString());
			Assert.AreEqual((Symbol)"re", SLH.TryPrint(CL(new Regex("[0-9]"), "re"), sb).Left.Or(null));
			Assert.AreEqual((Symbol)"re", SLH.TryPrint(CL(new Regex("[0-9]"), null), sb).Left.Or(null));
			Assert.AreEqual("[0-9]", sb.ToString());

			foreach (var item in TestItems) {
				Assert.IsFalse(SLH.CanPrint(item.Item1), "I thought printers aren't registered by TypeMarker");
				Assert.IsTrue(SLH.CanPrint(item.Item3.GetType()), "Can't print " + item.Item3.GetType());
				var result = SLH.TryPrint(CL(item.Item3, item.Item1.Name), sb);
				Assert.IsTrue(result.Left.HasValue, "Print failed for {0} {1}", item.Item1, item.Item2);
				Assert.AreEqual(item.Item2.ToLower(), sb.ToString().ToLower(), "Printed value mismatch");
				var expectMarker = item.Item1.Name;
				if (item.Item3 is int) expectMarker = "_";
				if (item.Item3 is uint) expectMarker = "_u";
				if (item.Item3 is float) expectMarker = "_f";
				if (item.Item3 is string) expectMarker = "";
				Assert.AreEqual(expectMarker, result.Left.Value.Name, "Type marker mismatch for {0}", item.Item2);
			}
		}
	}
}
