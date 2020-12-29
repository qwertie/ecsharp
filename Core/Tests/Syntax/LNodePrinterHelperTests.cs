using Loyc.Collections.Impl;
using Loyc.MiniTest;
using Loyc.Syntax.Impl;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Tests
{
	public delegate Pair<PrinterHelper, StringBuilder> LNodePrinterHelperFactory<PrinterHelper>(string indent = "  ", string newline = "\n", Action<ILNode, IndexRange> saveRange = null, bool allowNewlineRevocation = true, string labelIndent = " ", string subexprIndent = "\t\t") where PrinterHelper : ILNodePrinterHelper<PrinterHelper>;

	public class LNodePrinterHelperTests<PrinterHelper> : TestHelpers where PrinterHelper : ILNodePrinterHelper<PrinterHelper>
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Synthetic);
		protected LNodePrinterHelperFactory<PrinterHelper> _factory;

		public LNodePrinterHelperTests(LNodePrinterHelperFactory<PrinterHelper> factory) => _factory = factory;

		[Test]
		public void BasicTests()
		{
			var pair = _factory();
			var sb = pair.B;
			using (var helper = pair.A)
			{
				WriteVarExampleTo(helper);
				WriteLoopExampleTo(helper);
				helper.Newline();
			}
			AreEqual(VarExampleText + "\n" + LoopExampleText + "\n", sb.ToString());

			pair = _factory();
			sb = pair.B;
			using (var helper = pair.A)
			{
				WriteLongStatementExampleTo(helper);
				WriteLabelExampleTo(helper);
			}
			AreEqual(LongStatementExampleText + "\n" + LabelExampleText, sb.ToString());

			// Make sure it accepts different initialization parameters
			pair = _factory(".  ", "\r\n", null, false, ". ", "\t\t");
			sb = pair.B;
			using (var helper = pair.A)
			{
				WriteLoopExampleTo(helper);
				WriteLongStatementExampleTo(helper);
				WriteLabelExampleTo(helper);
			}
			var expect = LoopExampleText + "\n" + LongStatementExampleText + "\n" + LabelExampleText;
			expect = expect.Replace("\n    ", "\n.  .  ").Replace("\n   ", "\n.  . ").Replace("\n  ", "\n.  ").Replace("\n", "\r\n");
			AreEqual(expect, sb.ToString());
		}

		const string VarExampleText = "int x = 123; // comment";
		static LNode VarExampleTree = 
			F.Vars(F.Int32, F.Call(S.Assign, F.Id("x"), F.Literal(123)))
				.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " comment"));
		static void WriteVarExampleTo(PrinterHelper helper)
		{
			var @var = VarExampleTree;
			helper.BeginNode(@var);
			helper.BeginNode(@var[0]).Write("int").EndNode().Write(' ');
			helper.BeginNode(@var[1]);
			helper.BeginNode(@var[1][0]).Write("x").EndNode().Write(' ');
			helper.BeginNode(@var[1].Target).Write("= ").EndNode();
			helper.BeginNode(@var[1][1]).Write((UString)"123").EndNode();
			helper.EndNode(); // end var[1]
			helper.Write("; ").BeginNode(@var.Attrs[0][0]).Write("//").Write((UString)" comment")
				.NewlineIsRequiredHere().EndNode();
			helper.EndNode(); // end @var
		}
		
		const string LoopExampleText = "while (i > 0) {\n  i--;\n}";
		static LNode LoopExampleTree = 
			F.Call(S.While, F.Call(S.GT, F.Id("i"), F.Literal(0)),
				F.Braces(F.Call(S.PostDec, F.Id("i"))));
		private static void WriteLoopExampleTo(PrinterHelper helper)
		{
			var loop = LoopExampleTree;	
			helper.BeginNode(loop).Write("while (");
			helper.BeginNode(loop[0]);
			helper.BeginNode(loop[0][0]).Write("i").EndNode();
			helper.Write(' ').BeginNode(loop[0].Target).Write("> ").EndNode();
			helper.BeginNode(loop[0][1]).Write("0").EndNode();
			helper.Write(')').EndNode(); // end loop[0]
			helper.Write(' ').BeginNode(loop[1]).Write('{').Indent().Newline(); // begin braces
			helper.BeginNode(loop[1][0]).BeginNode(loop[1][0][0]).Write("i").EndNode()
				.BeginNode(loop[1][0].Target).Write("--").EndNode()
				.Write(';').EndNode();
			helper.Dedent().Newline().Write('}').EndNode().NewlineIsRequiredHere().EndNode(); // end braces and loop
		}

		const string LongStatementExampleText = "{\n  tokenList = parser.ReadAllTokens(\n  \t\t// comment\n  \t\tnew StreamCharSource(File.OpenRead(filename)));\n}";
		static LNode LongStatementExampleTree = F.Braces(
			F.Call(S.Assign, F.Id("tokenList"), F.Call(F.Dot(F.Id("parser"), F.Id("ReadAllTokens")),
				F.Attr(F.TriviaNewline, F.Trivia(S.TriviaSLComment, " comment"),
				F.Call(S.New, F.Call("StreamCharSource", F.Call(F.Dot(F.Id("File"), F.Id("OpenRead")), F.Id("filename"))))))));
		private static void WriteLongStatementExampleTo(PrinterHelper helper)
		{
			var SE = PrinterIndentHint.Subexpression;
			var braces = LongStatementExampleTree;
			var stmt = braces[0];
			helper.BeginNode(braces).Write('{').Indent();
			{
				helper.Newline().BeginNode(stmt);
				helper.BeginNode(stmt[0], SE).Write("tokenList").EndNode().Write(' ');
				helper.BeginNode(stmt.Target, SE).Write("= ").EndNode();
				helper.BeginNode(stmt[1], SE);
				helper.BeginNode(stmt[1].Target);
				helper.BeginNode(stmt[1].Target[0], SE).Write("parser").EndNode();
				helper.BeginNode(stmt[1].Target.Target, SE).Write(".").EndNode();
				helper.BeginNode(stmt[1].Target[1], SE).Write("ReadAllTokens").EndNode();
				helper.EndNode().Write('('); // end stmt[1].Target
				var newExpr = stmt[1][0];
				helper.BeginNode(newExpr, SE);
				{
					helper.BeginNode(newExpr.Attrs[0], SE).WriteSmartly("\n").EndNode();
					helper.BeginNode(newExpr.Attrs[1], SE).WriteSmartly("// comment").NewlineIsRequiredHere().EndNode();
					var constructor = newExpr[0];
					helper.Write("new ").BeginNode(constructor, SE);
					helper.BeginNode(constructor.Target, SE).Write("StreamCharSource").EndNode();
					helper.Write('(').BeginNode(constructor[0], SE);
					helper.BeginNode(constructor[0].Target, SE);
					helper.BeginNode(constructor[0].Target[0], SE).Write("File").EndNode();
					helper.BeginNode(constructor[0].Target.Target, SE).Write(".").EndNode();
					helper.BeginNode(constructor[0].Target[1], SE).Write("OpenRead").EndNode();
					helper.EndNode().Write('('); // end constructor[0].Target
					helper.BeginNode(constructor[0][0], SE).Write("filename").EndNode();
					helper.Write(')').EndNode().Write(')').EndNode(); // end constructor
				}
				helper.EndNode(); // end newExpr
				helper.Write(')').EndNode(); // end stmt[1]
				helper.Write(';').EndNode(); // end stmt
			}
			helper.Dedent().Newline().Write('}').NewlineIsRequiredHere().EndNode(); // end braces
		}

		const string LabelExampleText = "{\n  goto myLabel;\n  {\n   myLabel:\n    return;\n  }\n}";
		static LNode LabelExampleTree = F.Braces(
			F.Call(S.Goto, F.Id("myLabel")),
			F.Braces(F.Call(S.Label, F.Id("myLabel")), F.Call(S.Return)));
		private static void WriteLabelExampleTo(PrinterHelper helper)
		{
			var ex = LabelExampleTree;
			helper.BeginNode(ex).Write('{').Indent();
			{
				var @goto = ex[0];
				helper.Newline().BeginNode(@goto);
				helper.Write("goto ").BeginNode(@goto[0], PrinterIndentHint.Subexpression).Write("myLabel").EndNode().Write(';').EndNode();
				var braces = ex[1];
				helper.Newline().BeginNode(braces).Write('{').Indent();
				{
					helper.BeginNode(braces[0], PrinterIndentHint.Label).Newline();
					helper.BeginNode(braces[0][0]).Write("myLabel").EndNode().Write(':').EndNode();
					helper.BeginNode(braces[1]).Newline().Write("return;").EndNode();
				}
				helper.Dedent().Newline().Write('}').EndNode();
			}
			helper.Dedent().Newline().Write('}').NewlineIsRequiredHere().EndNode();
		}

		[Test]
		public void TestSaveRanges()
		{
			var ranges = new Dictionary<ILNode, IndexRange>();
			var pair = _factory("  ", "\n", (node, range) => ranges[node] = range, false);
			var sb = pair.B;
			using (var helper = pair.A)
			{
				WriteVarExampleTo(helper);
				WriteLoopExampleTo(helper);
			}

			// Actually 16, but the identifier i appears twice and we ignore one for simplicity's sake
			AreEqual(15, ranges.Count);
			// Now check ranges of identifiers/literals
			CheckRanges(ranges, sb);
		}

		protected static void CheckRanges(Dictionary<ILNode, IndexRange> ranges, StringBuilder sb)
		{
			foreach (var p in ranges)
			{
				ILNode node = p.Key;
				string expect;
				if (node.IsId() && !node.HasAttrs())
				{
					if (node.IsIdNamed(S.Int32))
						expect = "int";
					else if (node.Name.Name.StartsWith("'suf"))
						expect = node.Name.Name.Substring(4);
					else if (node.Name.Name.StartsWith("'"))
						expect = node.Name.Name.Substring(1);
					else
						expect = node.Name.Name;
				}
				else if (node.IsLiteral())
					expect = node.Value.ToString();
				else if (node.Calls(S.TriviaSLComment))
					expect = "//" + node[0].Value.ToString();
				else
					continue;

				AreEqual(expect, sb.Substring(p.Value.StartIndex, p.Value.Length).Trim());
			}
		}
	}

	public class LNodePrinterHelperTestsWithRevocation<PrinterHelper, C> : LNodePrinterHelperTests<PrinterHelper> 
		where PrinterHelper : ILNodePrinterHelperWithRevokableNewlines<C, PrinterHelper>
	{
		public LNodePrinterHelperTestsWithRevocation(LNodePrinterHelperFactory<PrinterHelper> factory) : base(factory) { }

		const string NewlineRevocationExampleText = "{" +
			"\n  VariableNameOfUnusualSize = \n  \t\tFunctionNameOfUnusualSize(\n  \t\tparameterOfUnusualSize);" +
			"\n  ReasonableMethod(notAsLong, so);" +
			"\n  WantToRevoke(// but can't" +
			"\n  \t\totherParameter);" +
			"\n}";
		static LNode NewlineRevocationExampleTree = F.Braces(
				F.Call(S.Assign, F.Id("VariableNameOfUnusualSize"), F.Call("FunctionNameOfUnusualSize", F.Id("parameterOfUnusualSize"))),
				F.Call("ReasonableMethod", F.Id("notAsLong"), F.Id("so")),
				F.Call("WantToRevoke", F.Attr(F.Trivia(S.TriviaSLComment, " but can't"), F.Id("otherParameter")))
			);
		static void WriteNewlineRevocationExampleTo(PrinterHelper helper)
		{
			Symbol SE = PrinterIndentHint.Subexpression;
			LNode braces = NewlineRevocationExampleTree, first = braces[0], second = braces[1], third = braces[2];
			helper.BeginNode(braces).Write('{').Indent();

			helper.BeginNode(first).Newline().CommitNewlines();
			var checkpoint1 = helper.GetCheckpoint();
			helper.BeginNode(first[0], SE).Write("VariableNameOfUnusualSize").EndNode().Write(' ');
			helper.BeginNode(first.Target, SE).Write("= ").Newline().EndNode(); // revokable!
			helper.BeginNode(first[1], SE).BeginNode(first[1].Target, SE).Write("FunctionNameOfUnusualSize").EndNode().Write('(');
			helper.BeginNode(first[1][0], SE).Newline().Write("parameterOfUnusualSize").EndNode();
			helper.Write(')').EndNode(); // end first[1]
			helper.Write(';').EndNode(); // end first
			AreEqual(2, helper.RevokeOrCommitNewlines(checkpoint1, 60));

			helper.BeginNode(second).Newline();
			var checkpoint2 = helper.GetCheckpoint();
			helper.BeginNode(second.Target, SE).Write("ReasonableMethod").EndNode().Write('(');
			helper.BeginNode(second[0], SE).Newline().Write("notAsLong").EndNode().Write(", ");
			helper.BeginNode(second[1], SE).Newline().Write("so").EndNode().Write(')');
			helper.Write(';').EndNode(); // end second
			AreEqual(-2, helper.RevokeOrCommitNewlines(checkpoint2, 60));

			helper.BeginNode(third).Newline().CommitNewlines();
			var checkpoint3 = helper.GetCheckpoint();
			helper.BeginNode(third.Target, SE).Write("WantToRevoke").EndNode().Write('(');
			helper.BeginNode(third[0], SE).BeginNode(third[0].Attrs[0], SE).Write("// but can't").IrrevokableNewline().EndNode()
			                          .Write("otherParameter").EndNode().Write(')');
			helper.Write(';').EndNode(); // end third
			AreEqual(0, helper.RevokeNewlinesSince(checkpoint3));

			helper.Dedent().Newline().Write('}').EndNode(); // end braces
		}

		[Test]
		public void TestNewlineRevocation()
		{
			var pair = _factory();
			var sb = pair.B;
			using (var helper = pair.A)
			{
				WriteNewlineRevocationExampleTo(helper);
			}
			AreEqual(NewlineRevocationExampleText, sb.ToString());
		}

		[Test]
		public void TestNewlineRevocationWithSavedRanges()
		{
			var ranges = new Dictionary<ILNode, IndexRange>();
			var pair = _factory("  ", "\n", (node, range) => ranges[node] = range, true);
			var sb = pair.B;
			using (var helper = pair.A)
			{
				WriteNewlineRevocationExampleTo(helper);
			}
			
			AreEqual(NewlineRevocationExampleText, sb.ToString());
			Greater(ranges.Count, 10);
			CheckRanges(ranges, sb);
		}
	}

	public class LNodePrinterHelperTests : LNodePrinterHelperTestsWithRevocation<LNodePrinterHelper, LNodePrinterHelperLocation>
	{
		public LNodePrinterHelperTests() : base(
			(indent, newline, saveRange, allowNewlineRevocation, labelIndent, subexprIndent) =>
			{
				var sb = new StringBuilder();
				return Pair.Create(
					new LNodePrinterHelper(
						sb, indent, newline, allowNewlineRevocation, labelIndent, subexprIndent
					) { SaveRange = saveRange },
					sb);
			})
		{ }
	}
}
