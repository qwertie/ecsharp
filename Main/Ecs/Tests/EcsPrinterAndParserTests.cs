using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Loyc;
using Loyc.Syntax;
using Loyc.Ecs.Parser;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

/// <summary>Unit tests for the Enhanced C# lexer, parser and printer.</summary>
namespace Loyc.Ecs.Tests
{
	/// <summary>Tests shared between the printer and the parser. Both tests
	/// together verify round-tripping from AST -> text -> AST.</summary>
	/// <remarks>Note that the other kind of round-tripping, text -> AST -> text,
	/// is not fully verified (and is not designed to be supported, as the
	/// printer is not designed to preserve spacing and the parser is not
	/// designed to save spacing information.)
	/// <para/>
	/// NOTE: The reason why this is a partial class, rather than a base class
	/// of the other test files, is that there are two derived classes, one
	/// for the printer and one for the parser. We couldn't do that if we had
	/// _separate_ classes for each test file.
	/// </remarks>
	public abstract partial class EcsPrinterAndParserTests : Assert
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		protected LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		protected LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		protected LNode @class = F.Id(S.Class), @partial = F.Id(S.Partial);
		protected LNode partialWA = F.Attr(F.Id(S.TriviaWordAttribute), F.Id(S.Partial));
		protected LNode @public = F.Id(S.Public), @static = F.Id(S.Static);
		protected LNode fooKW = F.Id("#foo"), fooWA = F.Attr(F.Id(S.TriviaWordAttribute), F.Id("#foo"));
		protected LNode @lock = F.Id(S.Lock), @if = F.Id(S.If);
		protected LNode @out = F.Id(S.Out), @ref = F.Id(S.Ref), @new = F.Id(S.NewAttribute);
		protected LNode trivia_forwardedProperty = F.Id(S.TriviaForwardedProperty);
		protected LNode get = F.Id("get"), set = F.Id("set"), value = F.Id("value"), _await = F.Id("await");
		protected LNode trivia_appendStatement = F.Id(S.TriviaAppendStatement);
		protected LNode trivia_operator = F.Id(S.TriviaUseOperatorKeyword);
		protected LNode _(string name) { return F.Id(name); }
		protected LNode _(Symbol name) { return F.Id(name); }
		protected LNode WordAttr(string name)
		{
			if (!name.StartsWith("#"))
				name = "#" + name;
			return F.Attr(F.Id(S.TriviaWordAttribute), _(name));
		}
		// Add trivia with special methods in case we ever change how trivia works
		protected LNode ChildStmt(LNode node)
		{
			return F.Attr(F.TriviaNewline, node);
		}
		protected LNode OnNewLine(LNode node)
		{
			return F.Attr(F.TriviaNewline, node);
		}
		protected LNode NewlineAfter(LNode node)
		{
			return node.PlusTrailingTrivia(F.TriviaNewline);
		}
		protected LNode AppendStmt(LNode node)
		{
			return F.Attr(trivia_appendStatement, node);
		}
		public LNode BracesOnOneLine(params LNode[] contents)
		{
			return F.Braces(contents.Select(n => AppendStmt(n)));
		}
		protected LNode MLComment(string text) { return F.Trivia(S.TriviaMLComment, text); }
		protected LNode SLComment(string text) { return F.Trivia(S.TriviaSLComment, text); }

		// Allows a particular test to exclude the printer or the parser
		[Flags]
		protected enum Mode {
			PrinterTest = 1, // Avoids parsing
			ParserTest = 2,  // Avoids printing
			Both = 3,        // Print & Parse (normal)
			Expression = 4,  // Expression mode flag (no semicolon, definitions not supported)
			// for Option(): print/parse the first string with default settings, but expect 
			// the printer to produce the second string after the configuration is applied
			PrintBothParseFirst = 8,
			// A parse error is expected and should be ignored (fails if no error occurs)
			ExpectAndDropParserError = 16,
			// Remove trivia when parsing
			IgnoreTrivia = 32,
		};

		// The tests were originally designed for printer tests, so they take
		// an Action<EcsNodePrinter> lambda. But the parser accepts no special
		// configuration, so EcsParserTests will just ignore the lambda.
		protected abstract void Stmt(string text, LNode code, Action<EcsPrinterOptions> configure = null, Mode mode = Mode.Both);

		protected void Stmt(string text, LNode code, Mode mode)
		{
			Stmt(text, code, null, mode);
		}
		protected void Expr(string text, LNode code, Mode mode)
		{
			Stmt(text, code, null, mode | Mode.Expression);
		}
		protected void Expr(string text, LNode code, Action<EcsPrinterOptions> configure = null, Mode mode = Mode.Both)
		{
			Stmt(text, code, configure, mode | Mode.Expression);
		}
		protected void Option(Mode mode, string before, string after, LNode code, Action<EcsPrinterOptions> configure = null)
		{
			Stmt(before, code, null,     mode == Mode.PrintBothParseFirst ? Mode.Both : mode);
			Stmt(after, code, configure, (mode & Mode.PrintBothParseFirst) != 0 ? Mode.PrinterTest : mode);
		}

		protected static LNode Attr(LNode attr, LNode node)
		{
			return node.WithAttrs(node.Attrs.Insert(0, attr));
		}
		protected static LNode Attr(params LNode[] attrsAndNode)
		{
			LNode node = attrsAndNode[attrsAndNode.Length - 1];
			var attrs = node.Attrs;
			for (int i = 0; i < attrsAndNode.Length - 1; i++)
				attrs.Insert(i, attrsAndNode[i]);
			return node.WithAttrs(attrs);
		}

		static LNode Alternate(LNode node)
		{
			node.Style |= NodeStyle.Alternate;
			return node;
		}
		static LNode AsStyle(NodeStyle s, LNode node) { return node.SetBaseStyle(s); }
		static LNode Operator(LNode node) { return node.SetBaseStyle(NodeStyle.Operator); }
	}
}
