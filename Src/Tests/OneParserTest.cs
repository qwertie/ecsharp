using System;
using System.Collections.Generic;
using Loyc.Essentials;
using NUnit.Framework;
using Loyc.CompilerCore.ExprNodes;
using Loyc.BooStyle;
using System.Text;

namespace Loyc.CompilerCore.ExprParsing
{
	[TestFixture]
	public class OneParserTests
	{
		protected class IfThenMatchOp : BaseMatchOp<AstNode>
		{
			public IfThenMatchOp()
				: base("if-then", GSymbol.Get("if_then"),
				new OneOperatorPart[] {
					new OneOperatorPart("if"),
					new OneOperatorPart(100),
					new OneOperatorPart("then"),
					new OneOperatorPart((int)Precedence.Phrase),
				}) { }
			public override int ComparePriority(IOneOperator<AstNode> other)
			{
				if (other.Type == GSymbol.Get("if_then_else"))
					return -1; // Lower priority than if-then-else.
				return 0;
			}
		}
		protected class IfThenElseMatchOp : BaseMatchOp<AstNode>
		{
			public IfThenElseMatchOp()
				: base("if-then-else", GSymbol.Get("if_then_else"),
				new OneOperatorPart[] {
					new OneOperatorPart("if"),
					new OneOperatorPart(100),
					new OneOperatorPart("then"),
					new OneOperatorPart(100),
					new OneOperatorPart("else"),
					new OneOperatorPart((int)Precedence.Phrase),
				}) { }
		}
		protected bool _reverseOrder;
		protected IOneParser<AstNode> _parser;
		public OneParserTests(IOneParser<AstNode> parser, bool reverseOrder) 
			{ _parser = parser; _reverseOrder = reverseOrder; }

		public static IOneOperator<AstNode>[] TestOps = new IOneOperator<AstNode>[] {
			new BinaryMatchOp<AstNode>(":", (int)Precedence.TightBinOp),
			new BinaryMatchOp<AstNode>("**", (int)Precedence.Exponentiation),
			new PrefixMatchOp<AstNode>("-", (int)Precedence.UnaryMed),
			new BinaryMatchOp<AstNode>("*", (int)Precedence.MulDiv),
			new BinaryMatchOp<AstNode>("/", (int)Precedence.MulDiv),
			new BinaryMatchOp<AstNode>("-", (int)Precedence.AddSub),
			new BinaryMatchOp<AstNode>("+", (int)Precedence.AddSub),
			new BinaryMatchOp<AstNode>("is", (int)Precedence.UnaryMed),
			new TernaryMatchOp<AstNode>("?", ":", (int)Precedence.Phrase),
			new IfThenMatchOp(),
			new IfThenElseMatchOp(),
			new BinaryMatchOp<AstNode>("=", (int)Precedence.Assignment),
			new IDMatchOp<AstNode>(),
			new INTMatchOp<AstNode>(),
			new BracketsMatchOp<AstNode>(),
			new BaseMatchOp<AstNode>("function call", GSymbol.Get("e()"),
				new OneOperatorPart[] {
					new OneOperatorPart((int)Precedence.UnaryHi),
					new OneOperatorPart(Tokens.LPAREN),
					new OneOperatorPart(Tokens.RPAREN),
				}),
			new BaseMatchOp<AstNode>("cast", GSymbol.Get("Cast"),
				new OneOperatorPart[] {
					new OneOperatorPart(Tokens.LPAREN),
					new OneOperatorPart(Tokens.RPAREN),
					new OneOperatorPart((int)Precedence.UnaryHi),
				}),
		};

		[SetUp]
		public void SetUp()
		{
			_parser.Clear();
			_parser.AddRange(GetOps());
		}
		IEnumerable<IOneOperator<AstNode>> GetOps()
		{
			if (_reverseOrder) {
				// Add operators in reverse order (this shouldn't make any 
				// difference to the parser's behavior)
				List<IOneOperator<AstNode>> opsRev = new List<IOneOperator<AstNode>>(TestOps);
				opsRev.Reverse();
				return opsRev;
			} else
				return TestOps;
		}

		[Test]
		public void OneTokenInputs()
		{
			DoTest("12           ", true, "12");
			DoTest("foo          ", true, "foo");
		}
		[Test]
		public void SimpleExpressions()
		{
			for (bool untilEnd = true; ; untilEnd = !untilEnd) {
				DoTest("-a           ", untilEnd, "- (a)");
				DoTest("a-b          ", untilEnd, "(a) - (b)");
				DoTest("John is human", untilEnd, "(John) is (human)");
				DoTest("a**b         ", untilEnd, "(a) ** (b)");
				if (!untilEnd)
					break;
			}
			DoTest(@"a \ b", false, "a");
			DoTest(@"a * b c", false, "(a) * (b)");
		}
		[Test]
		public void MiscUnambiguousExpressions()
		{
			for (bool untilEnd = true; ; untilEnd = !untilEnd) {
				DoTest("a-b+c        ", untilEnd, "((a) - (b)) + (c)");
				DoTest("a**b + c     ", untilEnd, "((a) ** (b)) + (c)");
				DoTest("a - b ** c   ", untilEnd, "(a) - ((b) ** (c))");
				DoTest("-a ** -b     ", untilEnd, "(- (a)) ** (- (b))");
				DoTest("-a ** -b - c ", untilEnd, "((- (a)) ** (- (b))) - (c)");
				DoTest("a ? b : c    ", untilEnd, "(a) ? (b) : (c)");
				DoTest("if a then b  ", untilEnd, "if (a) then (b)");
				DoTest("if if then if", untilEnd, "if (if) then (if)");
				DoTest("if a then b else c", untilEnd, "if (a) then (b) else (c)");
				if (!untilEnd)
					break;
			}
		}

		protected void DoTest(string Input, bool untilEnd, string expected)
		{
			// Lex and filter the input, then wrap it in an EnumerableSource and parse it
			StringCharSourceFile input = new StringCharSourceFile(Input, "Boo");
			IEnumerable<AstNode> lexer;
            if (untilEnd)
                lexer = new BooLexerCore(input, new Dictionary<string, Symbol>());
            else
                lexer = new BooLexer(input, new Dictionary<string, Symbol>(), true);
			IEnumerable<AstNode> lexFilter = new VisibleTokenFilter<AstNode>(lexer);
			EnumerableSource<AstNode> source = new EnumerableSource<AstNode>(lexFilter);
			int pos = 0;
			OneOperatorMatch<AstNode> expr = _parser.Parse((IParserSource<AstNode>)source, ref pos, untilEnd);
			
			// Build result string
			Assert.IsNotNull(expr);
			string result = BuildResult(expr);
			Assert.AreEqual(expected, result);
		}

		public static string BuildResult(OneOperatorMatch<AstNode> expr)
		{
			return BuildResult(expr, new StringBuilder()).ToString();
		}
		protected static StringBuilder BuildResult(OneOperatorMatch<AstNode> expr, StringBuilder sb)
		{
			//sb.Append(expr.Operator.NodeType.SafeName);
			bool first = true;
			foreach (OneOperatorMatchPart<AstNode> part in expr.Parts) {
				if (!first)
					sb.Append(" ");
				else
					first = false;
				if (part.MatchedExpr) {
					sb.Append('(');
					BuildResult(part.Expr, sb);
					sb.Append(')');
				} else
					sb.Append(part.Text);
			}
			return sb;
		}
	}
}