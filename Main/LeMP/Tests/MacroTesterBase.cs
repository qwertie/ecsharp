using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;

namespace LeMP.Tests
{
	public class MacroTesterBase
	{
		protected MessageHolder _msgHolder;

		[SetUp]
		public void SetUp() {
			// Block verbose messages
			MessageSink.Default = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);
			_msgHolder = new MessageHolder();
		}

		protected void TestLes(string input, string outputLes, int maxExpand = 0xFFFF)
		{
			Test(input, Les2LanguageService.Value, outputLes, Les2LanguageService.Value, maxExpand);
		}
		protected void TestEcs(string input, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(input, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		protected void TestCs(string input, string outputCs, int maxExpand = 0xFFFF)
		{
			// This is used mainly to avoid printing [#trivia_isTmpVar] when testing sequence expressions
			Test(input, EcsLanguageService.Value, outputCs, EcsLanguageService.WithPlainCSharpPrinter, maxExpand);
		}
		protected void TestBoth(string inputLes, string inputEcs, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(inputLes, Les2LanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
			Test(inputEcs, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		protected void Test(string input, IParsingService inLang, string expected, IParsingService outLang, int maxExpand = 0xFFFF)
		{
			var lemp = NewLemp(maxExpand, inLang);
			
			// The current printer affects the assert macro and contract macros
			using (LNode.PushPrinter((ILNodePrinter)outLang))
			{
				var inputCode = new VList<LNode>(inLang.Parse(input, MessageSink.Default));
				var results = lemp.ProcessSynchronously(inputCode);
				var expectCode = outLang.Parse(expected, MessageSink.Default);
				if (!results.SequenceEqual(expectCode))
				{	// TEST FAILED, print error
					string resultStr = results.Select(n => ((ILNodePrinter)outLang).Print(n)).Join("\n");
					Assert.AreEqual(TestCompiler.StripExtraWhitespace(expected),
									TestCompiler.StripExtraWhitespace(resultStr));
					// In some tests, the text is equal even though the trees are different,
					// typically because of differences in #trivia attributes between the two.
					Console.WriteLine(); // it's OK, but print a hint that this occurred.
				}
			}
		}
		protected virtual MacroProcessor NewLemp(int maxExpand, IParsingService inLang)
		{
			var lemp = new MacroProcessor(MessageSink.Default, typeof(LeMP.Prelude.BuiltinMacros));
			lemp.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			lemp.AddMacros(typeof(LeMP.StandardMacros).Assembly);
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			if (inLang.FileExtensions.Any(e => e == "les"))
				lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
			lemp.MaxExpansions = maxExpand;
			return lemp;
		}
	}
}
