using Loyc.Collections;
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
	public delegate Pair<PrinterHelper, StringBuilder> LNodePrinterHelperFactory<PrinterHelper>(string indent = ". ", string newline = "\n", Action<ILNode, IndexRange, int> saveRange = null, bool allowNewlineRevocation = true, string labelIndent = " ", string subexprIndent = "  ") where PrinterHelper : ILNodePrinterHelper<PrinterHelper>;

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
			pair = _factory("...", "\r\n", null, false, "\t", "\b");
			sb = pair.B;
			using (var helper = pair.A)
			{
				WriteLoopExampleTo(helper);
				WriteLongStatementExampleTo(helper);
				WriteLabelExampleTo(helper);
			}
			var expect = LoopExampleText + "\n" + LongStatementExampleText + "\n" + LabelExampleText;
			expect = expect.Replace(". ", "...").Replace("  ", "\b").Replace(" myLabel:", "\tmyLabel:").Replace("\n", "\r\n");
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
		
		const string LoopExampleText = "while (i > 0) {\n. i--;\n}";
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

		const string LongStatementExampleText = "{" +
			"\n. tokenList = parser.ReadAllTokens(" +
			"\n.   // comment" +
			"\n.   new StreamCharSource(" +
			"\n.     File.OpenRead(filename)));" +
			"\n}";
		static LNode LongStatementExampleTree = F.Braces(
			F.Call(S.Assign, F.Id("tokenList"), F.Call(F.Dot(F.Id("parser"), F.Id("ReadAllTokens")),
				F.Attr(F.TriviaNewline, F.Trivia(S.TriviaSLComment, " comment"),
				F.Call(S.New, F.Call("StreamCharSource", F.Attr(F.TriviaNewline, 
					F.Call(F.Dot(F.Id("File"), F.Id("OpenRead")), F.Id("filename")))))))));
		private static void WriteLongStatementExampleTo(PrinterHelper helper)
		{
			Symbol SE = PrinterIndentHint.Subexpression, B = PrinterIndentHint.Brackets;
			var braces = LongStatementExampleTree;
			var stmt = braces[0];
			helper.BeginNode(braces).Write('{').Indent();
			{
				helper.Newline().BeginNode(stmt);
				helper.BeginNode(stmt[0], SE).Write("tokenList").EndNode(SE).Write(' ');
				helper.BeginNode(stmt.Target, SE).Write("= ").EndNode(SE);
				helper.BeginNode(stmt[1], SE);
				{
					helper.BeginNode(stmt[1].Target);
					helper.BeginNode(stmt[1].Target[0], SE).Write("parser").EndNode(SE);
					helper.BeginNode(stmt[1].Target.Target, SE).Write(".").EndNode(SE);
					helper.BeginNode(stmt[1].Target[1], SE).Write("ReadAllTokens").EndNode(SE);
					helper.EndNode(); // end stmt[1].Target
					helper.Write('(').Indent(B);
					var newExpr = stmt[1][0];
					helper.BeginNode(newExpr, SE);
					{
						helper.BeginNode(newExpr.Attrs[0], SE).Newline().EndNode(SE);
						helper.BeginNode(newExpr.Attrs[1], SE).Write("// comment").NewlineIsRequiredHere().EndNode(SE);
						var constructor = newExpr[0];
						helper.Write("new ").BeginNode(constructor, SE);
						helper.BeginNode(constructor.Target, SE).Write("StreamCharSource").EndNode(SE);
						helper.Write('(').Indent(B).BeginNode(constructor[0], SE).Newline();
						helper.BeginNode(constructor[0].Target, SE);
						helper.BeginNode(constructor[0].Target[0], SE).Write("File").EndNode(SE);
						helper.BeginNode(constructor[0].Target.Target, SE).Write(".").EndNode(SE);
						helper.BeginNode(constructor[0].Target[1], SE).Write("OpenRead").EndNode(SE);
						helper.EndNode(SE).Write('(').Indent(B); // end constructor[0].Target
						helper.BeginNode(constructor[0][0], SE).Write("filename").EndNode(SE);
						helper.Write(')').Dedent(B).EndNode(SE);
						helper.Write(')').Dedent(B).EndNode(SE); // end constructor
					}
					helper.EndNode(SE); // end newExpr
				}
				helper.Write(')').Dedent(B).EndNode(SE); // end stmt[1]
				helper.Write(';').EndNode(); // end stmt
			}
			helper.Dedent().Newline().Write('}').NewlineIsRequiredHere().EndNode(); // end braces
		}

		const string LabelExampleText = "{\n. goto myLabel;\n. {\n.  myLabel:\n. . return;\n. }\n}";
		static LNode LabelExampleTree = F.Braces(
			F.Call(S.Goto, F.Id("myLabel")),
			F.Braces(F.Call(S.Label, F.Id("myLabel")), F.Call(S.Return)));
		private static void WriteLabelExampleTo(PrinterHelper helper)
		{
			var SE = PrinterIndentHint.Subexpression;
			var ex = LabelExampleTree;
			helper.BeginNode(ex).Write('{').Indent();
			{
				var @goto = ex[0];
				helper.Newline().BeginNode(@goto);
				helper.Write("goto ").BeginNode(@goto[0], SE).Write("myLabel").EndNode(SE).Write(';').EndNode();
				var braces = ex[1];
				helper.Newline().BeginNode(braces).Write('{').Indent();
				{
					helper.Newline(deferIndent: true);
					helper.BeginNode(braces[0], PrinterIndentHint.Label).FlushIndent();
					helper.BeginNode(braces[0][0], SE).Write("myLabel").EndNode(SE);
					helper.Write(':').EndNode(PrinterIndentHint.Label);
					helper.Newline(deferIndent: true);
					helper.BeginNode(braces[1]).Write("return;").EndNode();
				}
				helper.Dedent().Newline().Write('}').EndNode();
			}
			helper.Dedent().Newline().Write('}').NewlineIsRequiredHere().EndNode();
		}

		[Test]
		public void TestSaveRanges()
		{
			var ranges = new Dictionary<ILNode, IndexRange>();
			var pair = _factory(". ", "\n", (node, range, depth) => ranges[node] = range, false);
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
			sb.Replace(". ", "  ");

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
			"\n. VariableNameOfUnusualSize = " +
			"\n.   FunctionNameOfUnusualSize(" +
			"\n.   parameterOfUnusualSize);" +
			"\n. SimpleMethod(123);" +
			"\n. ReasonableMethod(notAsLong, so);" +
			"\n. WantToRevoke(// but can't" +
			"\n.   otherParameter);" +
			"\n}";
		static LNode NewlineRevocationExampleTree = F.Braces(
				F.Call(S.Assign, F.Id("VariableNameOfUnusualSize"), F.Call("FunctionNameOfUnusualSize", F.Id("parameterOfUnusualSize"))),
				F.Call("SimpleMethod", F.Literal(123)),
				F.Call("ReasonableMethod", F.Id("notAsLong"), F.Id("so")),
				F.Call("WantToRevoke", F.Attr(F.Trivia(S.TriviaSLComment, " but can't"), F.Id("otherParameter")))
			);
		static void WriteNewlineRevocationExampleTo(PrinterHelper helper)
		{
			Symbol SE = PrinterIndentHint.Subexpression, B = PrinterIndentHint.Brackets;
			LNode braces = NewlineRevocationExampleTree, first = braces[0], second = braces[1], third = braces[2], fourth = braces[3];
			helper.BeginNode(braces).Write('{').Indent();

			var checkpoint1 = helper.BeginNode(first).Newline().CommitNewlines().GetCheckpoint();
			helper.BeginNode(first[0], SE).Write("VariableNameOfUnusualSize").EndNode(SE).Write(' ');
			helper.BeginNode(first.Target, SE).Write("= ").Newline().EndNode(SE); // revokable!
			helper.BeginNode(first[1], SE).BeginNode(first[1].Target, SE).Write("FunctionNameOfUnusualSize").EndNode(SE).Write('(');
			helper.BeginNode(first[1][0], B).Newline().Write("parameterOfUnusualSize").EndNode(B);
			helper.Write(')').EndNode(SE); // end first[1]
			helper.Write(';').EndNode(); // end first
			AreEqual(2, helper.RevokeOrCommitNewlines(checkpoint1, 60));

			var checkpoint2 = helper.BeginNode(second).Newline().GetCheckpoint();
			helper.BeginNode(second.Target, SE).Write("SimpleMethod").EndNode(SE).Write('(').Indent(B);
			helper.BeginNode(second[0], SE).Newline().Write("123").EndNode(SE).Write(')').Dedent(B);
			helper.Write(';').EndNode(); // end second
			AreEqual(-1, helper.RevokeOrCommitNewlines(checkpoint2, 60));

			var checkpoint3 = helper.BeginNode(third).Newline(deferIndent: true).GetCheckpoint();
			helper.FlushIndent(); // shouldn't disrupt the checkpoint
			helper.BeginNode(third.Target, SE).Write("ReasonableMethod").EndNode(SE).Write('(').Indent(B);
			helper.BeginNode(third[0], SE).Newline().Write("notAsLong").EndNode(SE).Write(", ");
			helper.BeginNode(third[1], SE).Newline().Write("so").EndNode(SE).Write(')').Dedent(B);
			helper.Write(';').EndNode(); // end third
			AreEqual(-2, helper.RevokeOrCommitNewlines(checkpoint3, 60));

			var checkpoint4 = helper.BeginNode(fourth).Newline().CommitNewlines().GetCheckpoint();
			helper.BeginNode(fourth.Target, SE).Write("WantToRevoke").EndNode(SE).Write('(').Indent(B);
			helper.BeginNode(fourth[0], SE)
			      .BeginNode(fourth[0].Attrs[0], SE).Write("// but can't").NewlineIsRequiredHere().Newline().EndNode(SE)
			      .Write("otherParameter").EndNode(SE).Write(')').Dedent(B);
			helper.Write(';').EndNode(); // end fourth
			AreEqual(0, helper.RevokeNewlinesSince(checkpoint4));

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


			var wrote = new List<string>();
			pair = _factory();
			sb = pair.B;
			using (var helper = pair.A)
			{
				var cp = helper.Indent().GetCheckpoint();
				
				helper.Write("By the way, if you have revokable newlines ".With(wrote.Add)).Newline();
				helper.Write("mixed with irrevokable ones...".With(wrote.Add)).NewlineIsRequiredHere().Newline(); // irrevokable
				helper.Write("It's currently ".With(wrote.Add)).Newline().Write("all-or-nothing.".With(wrote.Add));
				// ideally it would be able to revoke just the first newline and not the third.
				// but no.
				AreEqual(2, helper.RevokeOrCommitNewlines(cp, 60));
				helper.Dedent();
			}
			AreEqual(string.Join("\n. ", wrote), sb.ToString());
		}

		[Test]
		public void TestNewlineRevocationWithSavedRanges()
		{
			var ranges = new Dictionary<ILNode, IndexRange>();
			var pair = _factory(". ", "\n", (node, range, depth) => ranges[node] = range, true);
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
				return Pair.Create(new LNodePrinterHelper(
						sb, saveRange, allowNewlineRevocation, indent, newline, labelIndent, subexprIndent, 4),
					sb);
			})
		{ }
	}
}
