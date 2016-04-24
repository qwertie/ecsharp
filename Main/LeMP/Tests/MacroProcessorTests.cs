using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Ecs;

namespace LeMP.Test
{
	[ContainsMacros]
	public class TestMacros
	{
		[LexicalMacro("Identity(args...)", "Expanded args in-place (kinda pointless?) for testing")]
		public static LNode Identity(LNode node, IMessageSink sink)
		{
			return node.WithName(S.Splice);
		}
		[LexicalMacro("priorityTest(x, y)", "Change first argument to 'hi'", 
			"priorityTest", "priorityTestPCB", Mode = MacroMode.PriorityOverride | MacroMode.Passive)]
		public static LNode priorityTestHi(LNode node, IMessageSink sink)
		{
			if (node.ArgCount >= 1 && !node[0].IsIdNamed("hi"))
				return node.WithArgChanged(0, LNode.Id("hi"));
			return null;
		}
		[LexicalMacro("priorityTest(x, y)", "Swap arg 0 and arg 1", Mode = MacroMode.ProcessChildrenAfter | MacroMode.Passive)]
		public static LNode priorityTest(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithArgs(node[1], node[0]);
			return null;
		}
		[LexicalMacro("priorityTestPCB(x, y)", "Swap arg 0 and arg 1", Mode = MacroMode.ProcessChildrenBefore | MacroMode.Passive)]
		public static LNode priorityTestPCB(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithArgs(node[1], node[0]);
			return null;
		}
	}
}
namespace LeMP
{
	/// <summary>A simple version of Compiler that takes a single input and produces 
	/// a StringBuilder. Pre-opens LeMP.Prelude namespace.</summary>
	public class TestCompiler : Compiler
	{
		public TestCompiler(IMessageSink sink, ICharSource text, string fileName = "")
			: base(sink, typeof(LeMP.Prelude.BuiltinMacros), new[] { new InputOutput(text, fileName) }) 
		{
			Parallel = false;
			MacroProcessor.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			MacroProcessor.AddMacros(typeof(LeMP.Test.TestMacros));
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
		}
			
		public StringBuilder Output;
		public VList<LNode> Results;
			
		protected override void WriteOutput(InputOutput io)
		{
			Results = io.Output;
			Output = new StringBuilder();
			foreach (LNode node in Results) {
				LNode.Printer(node, Output, Sink, null, IndentString, NewlineString);
				Output.Append(NewlineString);
			}
		}

		#region static Test(), StripExtraWhitespace() methods

		public static void Test(string input, string output, IMessageSink sink, int maxExpand = 0xFFFF)
		{
			using (LNode.PushPrinter(new EcsNodePrinter(null, null) { PreferPlainCSharp = true }.Print)) {
				var c = new TestCompiler(sink, new UString(input), "");
				c.MaxExpansions = maxExpand;
				c.MacroProcessor.AbortTimeout = TimeSpan.Zero; // never timeout (avoids spawning a new thread)
				c.Run();
				Assert.AreEqual(StripExtraWhitespace(output), StripExtraWhitespace(c.Output.ToString()));
			}
		}
		
		static readonly string[] CommentPrefix = new[] { "//" };
		/// <summary>Strips whitespace and single-line comments from a string.
		/// Helps test whether two blocks of code are "sufficiently equal".</summary>
		public static string StripExtraWhitespace(string a, string[] commentPrefixes = null)
		{
			commentPrefixes = commentPrefixes ?? CommentPrefix;
			StringBuilder sb = new StringBuilder();
			char prev_c = '\0';
			for (int i = 0; i < a.Length; i++) {
				char c = a[i];

				var slice = a.Slice(i);
				for (int cp = 0; cp < commentPrefixes.Length; cp++) {
					if (slice.StartsWith(commentPrefixes[cp])) {
						do ++i; while (i < a.Length && (c = a[i]) != '\n' && c != '\r');
						break;
					}
				}

				if (c == '\n' || c == '\r' || c == '\t')
					c = ' ';
				if (c == ' ' && (!MaybeId(prev_c) || !MaybeId(a.TryGet(i + 1, '\0'))))
					continue;

				sb.Append(c);
				prev_c = c;
			}
			return sb.ToString();
		}
		static bool MaybeId(char c) { return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }

		#endregion
	}

	[TestFixture]
	public class MacroProcessorTests
	{
		[Test]
		public void TrivialTest()
		{
			Test("no macros.apply here;",   // LES input
				"no(macros.apply, here);"); // C# output
			Test("while (@true) {};",       // LES input
				"while ((true)) {}");       // C# output
		}

		[Test]
		public void ExpandLimit()
		{
			Test("{ x::Foo; y::int; }",
				"{ Foo x; int y; }", 2);
			Test("{ x::Foo; y::int; }",
				"{ Foo x; @int y; }", 1);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"[@static] @void Main() { @var(x::@int = @default(@int)); }", 1);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"static void Main() { @int x = @default(@int); }", 2);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"static void Main() { int x = default(@int); }", 3);
			Test("@[static] fn Main()::void { var x::int = `default` int; }",
				"static void Main() { int x = default(int); }", 4);
		}

		[Test]
		public void JustSpliceTest()
		{
			Test("the #splice(macro, inserts, stuff, in-place) here;",
				"the (macro, inserts, stuff, @in-place, here);");
		}

		[Test]
		public void NoLexicalMacrosTest()
		{
			Test("#noLexicalMacros(blocks macros, e.g. break, return Foo);",
				"blocks(macros); e.g.@break; @return(Foo);");
		}

		[Test]
		public void ImportsTest()
		{
			Test("import_macros LeMP.Test; x();",
				"x();");
			Test("import x.y;",
				"using x.y;");
			Test("Identity(x); { import LeMP.Test; Identity(x); }; Identity(x);",
				"Identity(x); { using LeMP.Test; x; } Identity(x);");
			Test("{{ import LeMP.Test; Identity(x); }}; Identity(x);",
				"{{ using LeMP.Test; x; }} Identity(x);");
		}

		[Test]
		public void PriorityTest()
		{
			Test("import_macros LeMP.Test; priorityTest(0, 1);",
			                              "priorityTest(1, hi);");
			Test("{ import_macros LeMP.Test; foo0(); priorityTest(0, 2); foo(); }",
				 "{                          foo0(); priorityTest(2, hi); foo(); }");
			Test("{ import_macros LeMP.Test; priorityTest(0, x::int = 3); foo(); }",
				 "{                          priorityTest(int x = 3, hi); foo(); }");
			Test("{ import_macros LeMP.Test; priorityTestPCB(0, x::int = 4); foo2(); }",
				 "{                          priorityTestPCB(int x = 4, hi); foo2(); }");
		}

		SeverityMessageFilter _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		private void Test(string input, string output, int maxExpand = 0xFFFF)
		{
			TestCompiler.Test(input, output, _sink, maxExpand);
		}
	}
}
