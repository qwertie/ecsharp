using Loyc.Collections;
using Loyc.Collections.Impl;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Tests
{
	[TestFixture]
	class LiteralHandlerTableTests
	{
		[Test]
		public void TestCustomParsers()
		{
			var lht = new LiteralHandlerTable();

			Assert.IsTrue(lht.AddParser(false, (Symbol)"fail", (text, marker) =>
			{
				Assert.AreEqual(marker.Name, "fail");
				return new LogMessage(Severity.Error, null, "Always fails");
			}));
			Func<UString, Symbol, Either<object, LogMessage>> trimmer = (text, marker) =>
			{
				Assert.AreEqual(marker.Name, "trimmed");
				return text.ToString().Trim();
			};
			Assert.IsTrue(lht.AddParser(false, (Symbol)"trimmed", trimmer));
			Assert.IsFalse(lht.AddParser(false, (Symbol)"trimmed", trimmer));
			Assert.IsTrue(lht.AddParser(true, (Symbol)"trimmed", trimmer));

			Assert.AreEqual("Hi", lht.TryParse(" Hi ", (Symbol)"trimmed").Left.Value);
			Assert.AreEqual("Always fails", lht.TryParse(" Hi ", (Symbol)"fail").Right.Value.Format);
		}

		// Custom printers
		Either<Symbol, LogMessage> PrintFail(ILNode node, StringBuilder s)
		{
			s.Append("FAIL");
			return new LogMessage(Severity.Error, null, "Always fails");
		}
		Either<Symbol, LogMessage> PrintTrim(ILNode node, StringBuilder s)
		{
			s.Append(node.Value.ToString().Trim());
			return (Symbol)"hovercraft";
		}
		Either<Symbol, LogMessage> PrintFirstChar(ILNode node, StringBuilder s)
		{
			s.Append(node.Value.ToString().Slice(0, 1).ToString());
			return (Symbol)"eels";
		}
		Either<Symbol, LogMessage> PrintFirstElem(ILNode node, StringBuilder s)
		{
			s.Append(((System.Collections.IEnumerable)node.Value).Cast<object>().First().ToString());
			return null;
		}

		LiteralNode CL(object value, string symbol) => LNode.Literal(SourceRange.Synthetic, new LiteralValue(value, (Symbol)symbol));

		[Test]
		public void TestCustomPrinters()
		{
			var lht = new LiteralHandlerTable();

			// Can't add printer for null
			Assert.IsFalse(lht.AddPrinter(true, (Type)null, PrintTrim));
			Assert.IsFalse(lht.AddPrinter(true, (Symbol)null, PrintTrim));

			// Add printer for "trim" marker and string
			Assert.IsTrue(lht.AddPrinter(false, (Symbol)"trim", PrintTrim));
			Assert.IsTrue(lht.AddPrinter(false, typeof(string), PrintTrim));
			Assert.IsFalse(lht.AddPrinter(false, typeof(string), PrintTrim));
			Assert.IsTrue(lht.AddPrinter(true, typeof(string), PrintTrim));
			Assert.IsTrue(lht.AddPrinter(true, typeof(string), PrintTrim));

			// Originally there is no printer for int
			var sb = new StringBuilder();
			Assert.IsTrue(lht.TryPrint(LNode.Literal(123), sb).Right.HasValue);
			Assert.IsTrue(lht.TryPrint(LNode.Literal(123), sb).Right.Value.Format.Contains("no printer for type 'System.Int32'"));
			Assert.AreEqual("", sb.ToString());

			// Add a printer for int and try it
			Assert.IsTrue(lht.AddPrinter(false, typeof(int), PrintTrim));
			Assert.AreEqual("hovercraft", lht.TryPrint(CL(123, "fail"), sb).Left.Value.Name);
			Assert.AreEqual("123", sb.ToString());

			// Add printer for "fail" marker and try it.
			Assert.IsTrue(lht.AddPrinter(false, (Symbol)"fail", PrintFail));
			Assert.AreEqual("Always fails", lht.TryPrint(CL(new int[5], "fail"), sb).Right.Value.Format);
			Assert.AreEqual("FAIL", sb.ToString());

			// TypeMarker printers take priority, so "fail" runs at first. Then the handler for int runs.
			Assert.AreEqual("hovercraft", lht.TryPrint(CL(999, "fail"), sb).Left.Value.Name);
			Assert.AreEqual("999", sb.ToString());

			// Try our string and trim modes
			Assert.AreEqual("hovercraft", lht.TryPrint(LNode.Literal(" hi! "), sb).Left.Value.Name);
			Assert.AreEqual("hi!", sb.ToString());
			Assert.AreEqual("hovercraft", lht.TryPrint(CL(new StringBuilder(" bye! "), "trim"), sb).Left.Value.Name);
			Assert.AreEqual("bye!", sb.ToString());
			
			// TypeMarker printers take priority, so "firstChar" runs in preference to the string handler.
			Assert.IsTrue(lht.AddPrinter(false, (Symbol)"firstChar", PrintFirstChar));
			Assert.AreEqual("eels", lht.TryPrint(CL("hi!  ", "firstChar"), sb).Left.Value.Name);
			Assert.AreEqual("h", sb.ToString());
		}

		[Test]
		public void TestCustomPrinters_InheritanceTest()
		{
			// ReversedListSource<T> is sufficiently complicated for this test. Inheritance:
			// 
			// Many other interfaces         Many other interfaces (including IListSource)
			//       ^                            ^
			//       |                            |
			// ICollectionImpl<T>            IListAndListSource<T>
			//       ^                            ^
			//       |                            |
			// ReadOnlyCollectionBase<T>     IListImpl<T>
			//       ^                            ^
			//       |                            |
			// ListSourceBase<T>------------------/
			//       ^
			//       |
			// ReversedListSource<T>
			
			var lht = new LiteralHandlerTable();
			Assert.IsTrue(lht.AddPrinter(false, typeof(IListAndListSource<int>), PrintFirstElem));
			Assert.IsTrue(lht.AddPrinter(false, typeof(IListSource<int>), PrintFail)); // Won't be used

			var list = new DList<int> { 123, 456, 789 };
			var rlist = new ReversedListSource<int>(list);
			var sb = new StringBuilder();
			Assert.AreEqual("list!", lht.TryPrint(CL(rlist, "list!"), sb).Left.Value.Name);
			Assert.AreEqual("789", sb.ToString());

			// Now add something that will have higher priority...
			Assert.IsTrue(lht.AddPrinter(false, typeof(IListImpl<int>), PrintFirstChar));
			Assert.AreEqual("eels", lht.TryPrint(CL(rlist, "cool!"), sb).Left.Value.Name);
			Assert.AreEqual(1, sb.Length);

			// Printer for base class ReadOnlyCollectionBase takes priority over interface IListImpl.
			// And that one will fail. But the fallback will succeed.
			Assert.IsTrue(lht.AddPrinter(false, typeof(ReadOnlyCollectionBase<int>), PrintFail));
			Assert.AreEqual("eels", lht.TryPrint(CL(rlist, "..."), sb).Left.Value.Name);
			Assert.AreEqual(1, sb.ToString().Length);

			// Now install a printer that will succeed
			Assert.IsTrue(lht.AddPrinter(true, typeof(ReadOnlyCollectionBase<int>), PrintFirstElem));
			Assert.AreEqual("cool!", lht.TryPrint(CL(rlist, "cool!"), sb).Left.Value.Name);
			Assert.AreEqual("789", sb.ToString());
		}
	}
}
