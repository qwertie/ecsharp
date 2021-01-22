using Loyc;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LeMP.Tests
{
	// Tests the top-level Compiler class. TODO: more testing!
	[TestFixture]
	public class CompilerTests
	{
		[Test]
		public void TestAutoOpenedNamespaceByExtension()
		{
			// LeMP should open LeMP.ecs for .ecs files, but not for .les files.
			// As an EC# macro, `quote` will work in .ecs but not .les.
			var c = new Compiler(MessageSink.Default, typeof(LeMP.Prelude.BuiltinMacros));

			var fileName1 = Path.GetTempFileName();
			File.WriteAllText(fileName1, "define foo() { goo(); };\nfoo();\nquote(hoo);");
			var fileName2 = Path.ChangeExtension(Path.GetTempFileName(), "ecs");
			File.WriteAllText(fileName2, "define foo() { koo(); }\nfoo();\nquote(loo);");

			// --inlang:les2 affects only the first file (.tmp); language is autodetected for .ecs
			// (--forcelang is needed to override this behavior)
			var opts = c.ProcessArguments(new List<string> { fileName1, fileName2, "--inlang:les2", "--outext:cs" }, true, true);
			c.Run();

			var result1 = File.ReadAllText(Path.ChangeExtension(fileName1, "cs"));
			Assert.AreEqual("goo();\nquote(hoo);", result1);
			var result2 = File.ReadAllText(Path.ChangeExtension(fileName2, "cs"));
			Assert.AreEqual("koo();\nLNode.Id((Symbol) \"loo\");", result2);
		}
	}
}
