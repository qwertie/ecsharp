using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Ecs;

namespace Loyc.LLParserGenerator
{
	/// <summary>
	/// Shared base class for "full-stack" LLLPG tests that use LeMP, the Ecs parser,
	/// and LLLPG macros in addition to the core engine.
	/// </summary>
	[TestFixture]
	class LlpgGeneralTestsBase
	{
		protected class TestCompiler : LeMP.TestCompiler
		{
			public TestCompiler(IMessageSink sink, ICharSource text, string fileName = "")
				: base(sink, text, fileName)
			{
				MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
				MacroProcessor.PreOpenedNamespaces.Add(Loyc.LLPG.Macros.MacroNamespace);
				MacroProcessor.AbortTimeout = TimeSpan.Zero;
				AddMacros(Assembly.GetExecutingAssembly());
			}
		}

		protected IMessageSink _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		// This method can be used when the LES and EC# versions of a grammar 
		// produce identical output: two inputs, one output.
		protected void DualLanguageTest(string inputLES, string inputECS, string expected, IMessageSink sink = null)
		{
			Test(inputLES, expected, sink); // default is LES
			Test(inputECS, expected, sink, EcsLanguageService.Value);
		}
		protected void Test(string input, string expected, IMessageSink sink = null, IParsingService parser = null)
		{
			using (ParsingService.PushCurrent(parser ?? Les2LanguageService.Value))
			using (LNode.PushPrinter(EcsLanguageService.WithPlainCSharpPrinter)) {
				var c = new TestCompiler(sink ?? _sink, new UString(input));
				c.Run();
				Assert.AreEqual(StripExtraWhitespace(expected), StripExtraWhitespace(c.Output.ToString()));
			}
		}
		static readonly string[] CommentPrefixes = new[] { "//", "#line" };
		protected static string StripExtraWhitespace(string a) { 
			return LeMP.TestCompiler.StripExtraWhitespace(a, CommentPrefixes);
		}
	}
}
