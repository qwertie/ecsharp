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
			MessageSink.SetDefault(new SeverityMessageFilter(ConsoleMessageSink.Value, Severity.DebugDetail));
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
			// This is used mainly to avoid printing @[@%isTmpVar] when testing sequence expressions
			Test(input, EcsLanguageService.Value, outputCs, EcsLanguageService.WithPlainCSharpPrinter, maxExpand);
		}
		protected void TestBoth(string inputLes, string inputEcs, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(inputLes, Les2LanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
			Test(inputEcs, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		protected void Test(string input, IParsingService inLang, string expected, IParsingService outLang, int maxExpand = 0xFFFF, IMessageSink sink = null)
		{
			MacroProcessor lemp = NewLemp(maxExpand, inLang).With(l => l.Sink = sink);
			var inputCode = new LNodeList(inLang.Parse(input, MessageSink.Default));

			Test(inputCode, lemp, expected, outLang);
		}
		protected void Test(LNode input, MacroProcessor lemp, string expected, IParsingService outLang)
			=> Test(LNode.List(input), lemp, expected, outLang);
		protected void Test(LNodeList input, MacroProcessor lemp, string expected, IParsingService outLang)
		{
			// The current printer affects the assert macro and contract macros,
			// so we'll want to set it up before running LeMP
			using (LNode.SetPrinter((ILNodePrinter)outLang))
			{
				var inputCode = input;
				var results = lemp.ProcessSynchronously(inputCode);
				var expectCode = outLang.Parse(expected, MessageSink.Default);
				if (!results.SequenceEqual(expectCode))
				{	// TEST FAILED, print error
					string resultStr = results.Select(n => ((ILNodePrinter)outLang).Print(n)).Join("\n");
					Assert.AreEqual(TestCompiler.StripExtraWhitespace(expected),
									TestCompiler.StripExtraWhitespace(resultStr));
					// In some tests, the text is equal even though the trees are different,
					// typically because of differences in #trivia attributes between the two.
					Console.WriteLine("(minor dif)"); // it's OK, but print a hint that this occurred.
				}
			}
		}
		protected virtual MacroProcessor NewLemp(int maxExpand, IParsingService inLang)
		{
			var lemp = new MacroProcessor(MessageSink.Default, typeof(LeMP.Prelude.BuiltinMacros));
			lemp.AddMacros(typeof(LeMP.StandardMacros).Assembly);
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.ecs"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			if (inLang?.FileExtensions.Any(e => e == "les2") ?? false)
				lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.les2.to.ecs"));
			if (inLang?.FileExtensions.Any(e => e == "les3") ?? false)
				lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.les3.to.ecs"));
			lemp.MaxExpansions = maxExpand;
			return lemp;
		}
	}
}
