using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc;
using LEL.Prelude;

namespace LEL
{
	using S = CodeSymbols;

	[TestFixture]
	class MacroProcessingTests
	{
		[Test]
		public void TrivialTest()
		{
			Test("no.macros apply.here;",
				"no.macros(apply.here);");
			Test("while (@true) {};",
				"while (true) {}");
		}

		[Test]
		public void ImportsTest()
		{
			Test("import macros LEL; x();",
				"x();");
			Test("import x.y;",
				"using x.y;");
			Test("Identity(x); { import LEL; Identity(x); }; Identity(x);",
				"Identity(x); { using LEL; x; } Identity(x);");
			Test("{{ import LEL; Identity(x); }}; Identity(x);",
				"{{ using LEL; x; }} Identity(x);");
		}

		class TestCompiler : Compiler
		{
			public TestCompiler(IMessageSink sink, IEnumerable<ISourceFile> sourceFiles, Type prelude)
				: base(sink, sourceFiles, prelude) 
				{ Parallel = false; MacroProcessor.AddMacros(typeof(TestCompiler)); }
			
			public StringBuilder Output;
			
			protected override void WriteOutput(ISourceFile file, Loyc.Collections.RVList<LNode> results)
			{
				Output = new StringBuilder();
				foreach (LNode node in results)
					LNode.Printer(node, Output, Sink, null, IndentString, NewlineString);
			}

			[SimpleMacro("Identity(args...)", "Expanded args in-place (kinda pointless?)")]
			public static LNode Identity(LNode node, IMessageSink sink)
			{
				return node.WithName(S.Splice);
			}
		}

		private void Test(string input, string output)
		{
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.Printer)) {
				var c = new TestCompiler(MessageSink.Console,
					new ISourceFile[] { new StringCharSourceFile(input, "") },
					typeof(LEL.Prelude.Macros));
				c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LEL.Prelude"));
				c.Run();
				Assert.AreEqual(StripExtraWhitespace(output), StripExtraWhitespace(c.Output.ToString()));
			}
		}
		public static string StripExtraWhitespace(string a)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < a.Length; i++) {
				char c = a[i];
				if (c == '\n' || c == '\r' || c == '\t')
					continue;
				if (c == ' ' && (!MaybeId(a.TryGet(i - 1, '\0')) || !MaybeId(a.TryGet(i + 1, '\0'))))
					continue;
				if (c == '/' && a.TryGet(i + 1, '\0') == '/') {
					// Skip comment
					do ++i; while (i < a.Length && (c = a[i]) != '\n' && c != '\r');
					continue;
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
		static bool MaybeId(char c) { return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }
	}
}
