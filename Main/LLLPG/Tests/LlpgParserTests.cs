using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Utilities;
using Loyc.Collections;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;
	using Loyc.Syntax.Lexing;

	/// <summary>Tests the stage 1 parser and the stage 2 parser.</summary>
	[TestFixture]
	class LlpgParserTests : Assert
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
		static Symbol AndNot = GSymbol.Get("'&!"), AddColon = GSymbol.Get("'+:");
		static Symbol Gate = S.Lambda, Plus = GSymbol.Get("'+suf"), Star = GSymbol.Get("'*suf"), Opt = GSymbol.Get("'?suf"), Bang = GSymbol.Get("'!suf");
		static Symbol Greedy = GSymbol.Get("greedy"), Nongreedy = GSymbol.Get("nongreedy");
		static Symbol Default = GSymbol.Get("default"), Error = GSymbol.Get("error");
		static LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c");

		[SetUp]
		void SetUp()
		{
			ParsingService.Default = Les2LanguageService.Value;
			MessageSink.SetDefault(ConsoleMessageSink.Value);
		}

		[Test]
		public void Stage1Les_SimpleTests()
		{
			TestStage1("a", a);
			TestStage1("'a'", F.Literal('a'));
			TestStage1("123", F.Literal(123));
			TestStage1("a...b", F.Call(S.DotDotDot, a, b));
			TestStage1("a..b", F.Call(S.DotDot, a, b));
			TestStage1("~a", F.Call(S.NotBits, a));
			TestStage1("a*", F.Call(Star, a));
			TestStage1("a+", F.Call(Plus, a));
			TestStage1("a | b", F.Call(S.OrBits, a, b));
			TestStage1("a / b", F.Call(S.Div, a, b));
			TestStage1("a(b | c)", F.Call(a, F.Call(S.OrBits, b, c)));
			TestStage1("a => b", F.Call(Gate, a, b));
			TestStage1("=> a", F.Call(Gate, F.Tuple(), a));
			TestStage1("()", F.Tuple());
			TestStage1("a b", F.Tuple(a, b));
			TestStage1("(a) (b)", F.Tuple(a, b));
			TestStage1("(a b)?", F.Call(Opt, F.Tuple(a, b)));
			TestStage1("{ a(); b(); }", F.Braces(F.Call(a), F.Call(b)));
			TestStage1("a = b...c", F.Call(S.Assign, a, F.Call(S.DotDotDot, b, c)));
			TestStage1("a += _", F.Call(S.AddAssign, a, F.Id("_")));
			TestStage1("a := b", F.Call(S.QuickBindAssign, a, b));
			TestStage1("a : b",  F.Call(S.Colon, a, b));
			TestStage1("a +: b", F.Call(AddColon, a, b));
			TestStage1("greedy a", F.Call(Greedy, a));
			TestStage1("nongreedy a", F.Call(Nongreedy, a));
			TestStage1("nongreedy(a)", F.Call(Nongreedy, a));
			TestStage1("default a", F.Call(Default, a), false);
			TestStage1("error a", F.Call(Error, a));
			TestStage1("&{ a = b / c }", F.Call(S.AndBits, F.Braces(F.Call(S.Assign, a, F.Call(S.Div, b, c)))));
			TestStage1("&!{ a(), b() }", F.Call(AndNot, F.Braces(F.Call(a), F.Call(b))), false);
			TestStage1("a!", F.Call(Bang, a));
		}
		[Test]
		public void Stage1Les_MoreTests()
		{
			TestStage1("~a..b", F.Call(S.NotBits, F.Call(S.DotDot, a, b)));
			TestStage1("~a..b!", F.Call(Bang, F.Call(S.NotBits, F.Call(S.DotDot, a, b))));
			TestStage1("{ a(); } b c", F.Tuple(F.Braces(F.Call(a)), b, c));
			TestStage1("a (b+ c)", F.Tuple(a, F.Tuple(F.Call(Plus, b), c)));
			TestStage1("a | (a b c)", F.Call(S.OrBits, a, F.Tuple(a, b, c)));
			TestStage1("a(b+ c)", F.Call(a, F.Call(S.Add, b, c)));
			TestStage1("a | b / c", F.Call(S.OrBits, a, F.Call(S.Div, b, c)));
			TestStage1("a / b | c", F.Call(S.OrBits, F.Call(S.Div, a, b), c));
			TestStage1("a* b | c", F.Call(S.OrBits, F.Tuple(F.Call(Star, a), b), c));
			TestStage1("a b? / c", F.Call(S.Div, F.Tuple(a, F.Call(Opt, b)), c));
			TestStage1("a / b => b+ / c", F.Call(S.Div, F.Call(S.Div, a, F.Call(Gate, b, F.Call(Plus, b))), c));
			TestStage1("=> a b / c", F.Call(S.Div, F.Call(Gate, F.Tuple(), F.Tuple(a, b)), c));
			TestStage1("~(a..b) | (-a)..b.c", F.Call(S.OrBits, F.Call(S.NotBits, F.Call(S.DotDot, a, b)), F.Call(S.DotDot, F.Call(S.Sub, a), F.Dot(b, c))));
			TestStage1("~ a..b  |  -a ..b.c", F.Call(S.OrBits, F.Call(S.NotBits, F.Call(S.DotDot, a, b)), F.Call(S.DotDot, F.Call(S.Sub, a), F.Dot(b, c))));
			TestStage1("a..b+", F.Call(Plus, F.Call(S.DotDot, a, b)));
			TestStage1("greedy(a | b)+", F.Call(Plus, F.Call(Greedy, F.Call(S.OrBits, a, b))));
			TestStage1("nongreedy a+",   F.Call(Plus, F.Call(Nongreedy, a)));
			TestStage1("default a b | c", F.Call(S.OrBits, F.Call(Default, F.Tuple(a, b)), c), false);
			TestStage1("error   a b | c", F.Call(S.OrBits, F.Call(Error,   F.Tuple(a, b)), c));
			TestStage1("(a | b? 'c')*", F.Call(Star, F.Call(S.OrBits, a, F.Tuple(F.Call(Opt, b), F.Literal('c')))));
			TestStage1("t:=id { x=t; } / '-' t:=num { } / '(' ')'", F.Call(S.Div, F.Call(S.Div, 
				F.Tuple(F.Call(S.QuickBindAssign, F.Id("t"), F.Id("id")), F.Braces(F.Call(S.Assign, F.Id("x"), F.Id("t")))),
				F.Tuple(F.Literal('-'), F.Call(S.QuickBindAssign, F.Id("t"), F.Id("num")), F.Braces())),
				F.Tuple(F.Literal('('), F.Literal(')'))));
		}

		void TestStage1(string text, LNode expected, bool tryECSharp = true)
		{
			TestStage1Core(text, expected);
			if (tryECSharp)
				using (ParsingService.PushCurrent(Ecs.EcsLanguageService.Value))
					TestStage1Core(text, expected);
		}
		void TestStage1Core(string text, LNode expected)
		{
			var lexer = ParsingService.Default.Tokenize(text, ConsoleMessageSink.Value);
			var treeified = new TokensToTree(lexer, true);
			var tokens = treeified.Buffered();
			var parser = new StageOneParser(tokens, lexer.SourceFile, ConsoleMessageSink.Value);
			LNode result = parser.Parse();
			AreEqual(expected, result);
		}

		[Test]
		public void Stage2_Tests()
		{
			// If we change the way Preds are printed, these tests will break, of course
			TestStage2(true, "az",     "'a'..'z'", "[a-z]");
			TestStage2(true, "azAZ",   "('a'..'z')|('A'..'Z')", "[A-Za-z]");
			TestStage2(true, "NotAZ",  "~('A'..'Z')", @"[^\$A-Z]");
			TestStage2(true, "Seq",    "('-'; '0'..'9')", @"[\-] [0-9]");
			TestStage2(true, "Hi0-9",  @"(""Hi""; '0'..'9')", "[H] [i] [0-9]");
			TestStage2(true, "Or1",    @"""ETX"" | 3", "([E] [T] [X] | (3))");
			TestStage2(true, "Or2",    @"(~10; {code;}) | '\n'", @"(~(-1, 10) | [\n])"); // code blocks not printed
			TestStage2(true, "Star",   @"@`'*suf`('0'..'9')", "([0-9])*");
			TestStage2(true, "Plus",   @"@`'+suf`('0'..'9')", "[0-9] ([0-9])*");
			TestStage2(true, "Opt",    @"@`'?suf`(('a';'b'))", "([a] [b])?");
			TestStage2(true, "Greedy", @"@`'*suf`(greedy(('a';'b')))", "greedy([a] [b])*");
			TestStage2(true, "Nongreedy", @"@`'*suf`(nongreedy(('a';'b')))", "nongreedy([a] [b])*");
			TestStage2(true, "Default1",  @"('a'|""bee""|default('b'))", "( [a] | [b] [e] [e] | default [b] )");
			TestStage2(true, "Default2",  @"@`'*suf`('a'|default('b')|'c')", "( [a] | default [b] | [c] )*");
			TestStage2(true, Tuple.Create("RuleRef", @"'.' | Digit", "([.] | Digit)"),
			                 Tuple.Create("Digit",    "'0'..'9'",    "[0-9]"));
			TestStage2(true, "aeiou",     @"'a'|'e'|'i'|'o'|'u'", "[aeiou]");
			TestStage2(true, "PrefixGate", "(=> ('a'; 'b')) / 'c'", "( => [a] [b] / [c])");
			TestStage2(false, "AB+orCD",  @"@`'+suf`(A.B) | C.D", "(A.B (A.B)* | C.D)");
			TestStage2(true,  "EOF1",     "('a'; 'b'; 'c'; -1)", @"[a] [b] [c] (-1)");
			TestStage2(false, "EOF2",     "('a'; 'b'; 'c'; EOF)", @"'a' 'b' 'c' EOF");
			TestStage2(false, "Slashes1", "(a3;{}) | ((a4;{}) | a5) / a6", "( a3 | ((a4 | a5) / a6) )");
			TestStage2(false, "Slashes2", "((a8;{}) | a9) / ((a10;{}) | (a11;{}) / a12)", "( (a8 | a9) / (a10 | (a11 / a12)) )");
			TestStage2(false, "Slashes3", "@`'*suf`( ((a0;{}) / a1 / a2) / " +
				"((a3;{}) | ((a4;{}) | a5) / a6) | a7 / (((a8;{}) | a9) / ((a10;{}) | (a11;{}) / a12)) )",
				"( ((a0 / a1 / a2) / (a3 | ((a4 | a5) / a6))) | (a7 / ((a8 | a9) / (a10 | (a11 / a12)))) )*");
		}

		[Test]
		public void Stage2_Nondeterministic()
		{
			// can randomly switch between zero|one and one|zero due to hashtable representation in PGNodeSet
			// (I'm not sure why it can change every time the program runs, though!)
			TestStage2(false, Tuple.Create("RuleRef", @"""NaN"" | (Digit; _)", @"(""NaN"" | Digit ~(EOF))"),
			                  Tuple.Create("Digit", "zero|one", "(one|zero)"));
			// can randomly switch between a|b and b|a due to hashtable representation
			TestStage2(false, "AorB", @"a | b", "(a|b)");
		}

		void TestStage2(bool lexerMode, string ruleName, string inputExpr, string asString)
		{
			TestStage2(lexerMode, Tuple.Create(ruleName, inputExpr, asString));
		}
		void TestStage2(bool lexerMode, params Tuple<string,string,string>[] ruleTuples)
		{
			var helper = lexerMode ? (IPGCodeGenHelper)new IntStreamCodeGenHelper() : new GeneralCodeGenHelper();
			var rules = new List<Pair<Rule,LNode>>();
			foreach (var tuple in ruleTuples)
			{
				string ruleName = tuple.Item1, inputExpr = tuple.Item2;
				var node = LesLanguageService.Value.ParseSingle(inputExpr, ConsoleMessageSink.Value, ParsingMode.Expressions);
				var rule = new Rule(node, GSymbol.Get(ruleName), null);
				rules.Add(Pair.Create(rule, node));
			}
			
			var parser = new StageTwoParser(helper, ConsoleMessageSink.Value);
			parser.Parse(rules);
			for (int i = 0; i < rules.Count; i++) {
				var rule = rules[i].A;
				var ruleAsString = rule.Pred.ToString();
				var expected = ruleTuples[i].Item3;
				if (expected == null)
					ConsoleMessageSink.Value.Warning(ruleTuples[i].Item1, ruleAsString);
				else
					AreEqual(expected, ruleAsString);
			}
		}
	}
}
