using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Syntax.Les
{
	public abstract class Les3PrinterAndParserTests : Assert
	{
		protected static LNodeFactory F = new LNodeFactory(EmptySourceFile.Unknown);
		
		protected LNode a = F.Id("a"), b = F.Id("b"), c = F.Id("c"), x = F.Id("x");
		protected LNode Foo = F.Id("Foo"), IFoo = F.Id("IFoo"), T = F.Id("T");
		protected LNode zero = F.Literal(0), one = F.Literal(1), two = F.Literal(2);
		protected LNode _(string name) { return F.Id(name); }
		protected LNode _(Symbol name) { return F.Id(name); }

		protected LNode Op(LNode node)
		{
			return node.SetBaseStyle(NodeStyle.Operator);
		}
		protected LNode KeywordExpr(LNode node)
		{
			return node.SetBaseStyle(NodeStyle.Special);
		}
		protected LNode OnNewLine(LNode node)
		{
			return node.PlusAttrBefore(F.TriviaNewline);
		}
		protected LNode NewlineAfter(LNode node)
		{
			return node.PlusTrailingTrivia(F.TriviaNewline);
		}

		#region Literals
		// Note: The most rigorous testing may be done in the Lexer tests,
		//       but these tests have the advantage of also testing the printer.

		[Test]
		public void NumericLiterals()
		{
			Exact(@"123",       F.Literal(123));
			Exact(@"(123)",     F.InParens(F.Literal(123)));
			Exact(@"123uL",     F.Literal(123uL));
			Exact(@"123.25",    F.Literal(123.25));
			Exact(@"123.25f",   F.Literal(123.25f));
			Exact("0x5D",       F.Literal(0x5D).WithStyle(NodeStyle.HexLiteral));
			Exact("0b1011101",  F.Literal(0x5D).WithStyle(NodeStyle.BinaryLiteral));
			Exact("0x2.E8",     F.Literal(0x2E8/256.0).WithStyle(NodeStyle.HexLiteral));
			Exact("0b10.11101", F.Literal(0x2E8/256.0).WithStyle(NodeStyle.BinaryLiteral));
			Exact("@@inf.f",     F.Literal(float.PositiveInfinity).WithStyle(NodeStyle.HexLiteral));
			Exact("@@-inf.d",    F.Literal(double.NegativeInfinity).WithStyle(NodeStyle.HexLiteral));
			// TODO: more tests for printer, especially of hex/binary literals... random numbers? denormals?
		}

		[Test]
		public void StringLiterals()
		{
			// General parser and printer tests
			Exact(@"'!'", F.Literal('!'));
			Exact(@"""!""", F.Literal("!"));
			Exact(@"'''!'''", F.Literal("!").SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact(@"""""""!""""""", F.Literal("!").SetBaseStyle(NodeStyle.TDQStringLiteral));
			Exact(@"""\\\r\n\t\0\x12""", F.Literal("\\\r\n\t\0\x12").SetBaseStyle(NodeStyle.Default));
			Exact(@"""•téŝt•""", F.Literal("•téŝt•").SetBaseStyle(NodeStyle.Default));
			Exact(@"'''''\'/ \'/'''", F.Literal("''' '").SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact(@"'''\x/\z/ ""\'/'''", F.Literal(@"\x/\z/ ""'").SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact(@"'''\r/\0/ '\'/'''", F.Literal("\r\0 ''").SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact("{\n  ''' Line 1\n      Line 2\n     '''\n}", F.Braces(F.Literal(" Line 1\n Line 2\n").SetBaseStyle(NodeStyle.TQStringLiteral)));
			// Parser-focused tests
			Stmt ("{\n  ''' Line 1\n      Line 2\n '''\n}",     F.Braces(F.Literal(" Line 1\n Line 2\n").SetBaseStyle(NodeStyle.TQStringLiteral)));
			Stmt ("{\n\t''' Line 1\n\t    Line 2\n\t\t '''\n}", F.Braces(F.Literal(" Line 1\n Line 2\n ").SetBaseStyle(NodeStyle.TQStringLiteral)));
			Stmt ("{\n  '''\tLine 1\n \tLine 2\n  \tLine 3'''\n}", F.Braces(F.Literal("\tLine 1\n\tLine 2\nLine 3").SetBaseStyle(NodeStyle.TQStringLiteral)));
			// Parser tests. Printer will print \u1234\uABCD and \n and \t as characters, so don't use exact matching
			Stmt (@"""\u1234\uABCD\x12""", F.Literal("\u1234\uABCD\x12").SetBaseStyle(NodeStyle.Default));
			Stmt (@"'''\\r/\n/\t/\0/ '\'/'''", F.Literal("\\\r\n\t\0 ''").SetBaseStyle(NodeStyle.TQStringLiteral));
			Stmt (@"""""""\\/\r/\n/\t/\0/ \""/""""""", F.Literal("\\\r\n\t\0 \"").SetBaseStyle(NodeStyle.TDQStringLiteral));
			// Ensure that the printer doesn't print false escape sequences
			Exact(@"'''\\/r/\\/0/\\/n/\\/t/\\/\/'''", F.Literal(@"\r/\0/\n/\t/\\/").SetBaseStyle(NodeStyle.TQStringLiteral));
		}

		[Test]
		public void Utf8BasedEscapeSequences()
		{
			// Astral characters are stored as surrogate pairs in C#
			// and are printed as 6-digit escapes by the printer.
			Exact(@"""\u01F4A9.\u10FFFF""", F.Literal("\uD83D\uDCA9.\uDBFF\uDFFF").SetBaseStyle(NodeStyle.Default));
			// Invalid UTF-8 bytes are transliterated to 0xDCxx bytes.
			// High surrogates (0xD800..0xDBFF) are left alone.
			Exact(@"""\xFF.\uD800""", F.Literal("\uDCFF.\uD800").SetBaseStyle(NodeStyle.Default));
		}

		[Test]
		public void StringLiteralsWithArbitraryBytes()
		{
			Exact(@"""é""",        F.Literal("é"));
			Exact(@"""\u01F4A9""", F.Literal("\uD83D\uDCA9")); // 💩 pile of poo U+1F4A9
			Stmt (@" ""\u1F4A9""", F.Literal("\uD83D\uDCA9")); // Printer uses 6 digits, only 5 needed
			Exact(@"""\x1B\xFF""", F.Literal("\x1B\uDCFF"));
			// Triple-quote request is ignored if the string contains invalid UTF-8,
			// since triple-quoted strings do not support \xNN or \uNNNN escapes.
			Exact(@"""\x1B\xFF""", F.Literal("\x1B\uDCFF").SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact("'''<\uD83D\uDCA9>'''", F.Literal("<\uD83D\uDCA9>").SetBaseStyle(NodeStyle.TQStringLiteral));
		}

		[Test]
		public void NamedFloatLiteral()
		{
			Exact("@@-inf.f", F.Literal(float.NegativeInfinity));
			Exact("@@inf.f", F.Literal(float.PositiveInfinity));
			Exact("@@nan.f", F.Literal(float.NaN));
			Exact("@@-inf.d", F.Literal(double.NegativeInfinity));
			Exact("@@inf.d", F.Literal(double.PositiveInfinity));
			Exact("@@nan.d", F.Literal(double.NaN));
		}

		[Test]
		public void CustomLiterals()
		{
			Exact(@"special""!""",      F.Literal(new CustomLiteral("!", (Symbol)"special")));
			Exact(@"special'''!'''",    F.Literal(new CustomLiteral("!", (Symbol)"special")).SetBaseStyle(NodeStyle.TQStringLiteral));
			Exact(@"@@unknown-literal", F.Literal(new CustomLiteral("unknown-literal", (Symbol)"@@")));
			Exact(@"123.5f00bar",       F.Literal(new CustomLiteral("123.5", (Symbol)"f00bar")));
			Exact(@"f00bar""0x1234""",  F.Literal(new CustomLiteral("0x1234", (Symbol)"f00bar")));
			Exact(@"`WTF!\n`""0x1234""",F.Literal(new CustomLiteral("0x1234", (Symbol)"WTF!\n")));
			// Backquotes added due to 'e' in error which resembles an exponent
			Exact(@"123.5`error`",      F.Literal(new CustomLiteral("123.5", (Symbol)"error")));
			// Parses OK but printer currently prints as in string form
			Stmt (@"0x123special",      F.Literal(new CustomLiteral("0x123", (Symbol)"special")).SetBaseStyle(NodeStyle.HexLiteral));
			Stmt (@"0x1234`f00bar`",    F.Literal(new CustomLiteral("0x1234", (Symbol)"f00bar")).SetBaseStyle(NodeStyle.HexLiteral));
		}

		[Test]
		public void NegativeLiteral()
		{
			Exact("-x", F.Call(S.Sub, x));
			Exact("-2", F.Call(S.Sub, two));
			Exact("n\"-2\"", F.Literal(-2));
			Stmt("n\"-111222333444\"", F.Literal(-111222333444));
			Exact("L\"-2\"", F.Literal(-2L));
			Stmt("n\"-2.0\"", F.Literal(-2.0));
			Stmt("d\"-2\"", F.Literal(-2.0));
			Exact("f\"-2\"", F.Literal(-2.0f));
			Stmt("f\"-2.0\"", F.Literal(-2.0f));
		}

		[Test]
		public void BigIntLiterals()
		{
			Exact(@"123z", F.Literal(new BigInteger(123)));
			// TODO: add underscores in printer
			Stmt (@"9_876_543_210z", F.Literal(new BigInteger(9876543210)));
			Exact("z\"-2\"", F.Literal(new BigInteger(-2)));
		}
		
		#endregion

		#region Basic expressions: calls, unary & binary operators

		[Test]
		public void SimpleCalls()
		{
			Exact("x", x);
			Exact("x()", F.Call(x));
			Exact("Foo(a, b, c)", F.Call(Foo, a, b, c));
			Exact("Foo(a(b, c), b(c))", F.Call(Foo, F.Call(a, b, c), F.Call(b, c)));
		}

		[Test]
		public void LiteralKeywords()
		{
			Exact(@"x(`true`, `false`, `null`)", F.Call(x, F.Id("true"), F.Id("false"), F.Id("null")));
			Expr (@"x( true,   false,   null)", F.Call(x, F.Literal(true), F.Literal(false), F.Literal(null)));
		}

		[Test]
		public void BinaryOps()
		{
			Exact("x + 1",        F.Call(S.Add, x, one));
			Exact("x * 2 + 1",    F.Call(S.Add, F.Call(S.Mul, x, two), one));
			Exact("a + b + 1",    F.Call(S.Add, F.Call(S.Add, a, b), one));
			Exact("a = b = 0",    F.Call(S.Assign, a, F.Call(S.Assign, b, zero)));
			Exact("a >= b..c",    F.Call(S.GE, a, F.Call(S.DotDot, b, c)));
			Exact("a == b && c != 0", F.Call(S.And, F.Call(S.Eq, a, b), F.Call(S.Neq, c, zero)));
			Exact("(a ? b : c)",  F.InParens(F.Call(S.QuestionMark, a, F.Call(S.Colon, b, c))));
			Exact("a ?? b <= c",  F.Call(S.LE, F.Call(S.NullCoalesce, a, b), c));
			Exact("a - b / c**2", F.Call(S.Sub, a, F.Call(S.Div, b, F.Call(S.Exp, c, two))));
			Exact("a >>= 1",      F.Call(S.ShrAssign, a, one));
			Exact("a.b?.c(x)",    F.Call(S.NullDot, F.Dot(a, b), F.Call(c, x)));
			Exact(@"a!.b**2",     F.Call(S.Exp, F.Call((Symbol)"'!.", a, b), two));
			Exact("a.b::x.c",     F.Dot(F.Call(S.ColonColon, F.Dot(a, b), x), c));
			Exact("a <- b <- c",  F.Call(S.LeftArrow, a, F.Call(S.LeftArrow, b, c)));
			Exact("a -> a * b",   F.Call(S._RightArrow, a, F.Call(S.Mul, a, b)));
			Exact("c && a <- b > 1", F.Call(S.And, c, F.Call(S.LeftArrow, a, F.Call(S.GT, b, one))));
			
			// Custom ops
			Exact("a |-| b + c",   F.Call("'|-|", a, F.Call(S.Add, b, c)));
			Exact("a.b!!.c.?. 1",  F.Call("'.?.", F.Call("'!!.", F.Dot(a, b), c), one));
			Exact("a +/ b *+ c",   F.Call("'*+", F.Call("'+/", a, b), c));
		}

		[Test]
		public void WordOperators()
		{
			Exact("a b c", Op(F.Call("'b", a, c)));
			Exact("a is b as c", Op(F.Call("'is", a, Op(F.Call("'as", b, c)))));
			Exact("{\n  a\n} foo_bar (b)", Op(F.Call("'foo_bar", F.Braces(a), F.InParens(b))));
			Exact("a if c otherwise b", Op(F.Call("'if", a, Op(F.Call("'otherwise", c, b)))));

			// Lowercase word ops have ultra-low precedence
			var stmt = Op(F.Call("'if", F.Call(S.Assign, a, b),
			           Op(F.Call("'otherwise", F.Call(S.And, c, x), F.Call(S.AddAssign, a, b)))));
			Exact("a = b if c && x otherwise a += b", stmt);

			// Uppercase has a higher precedence
			Exact("a MOD b == 0", F.Call(S.Eq, Op(F.Call("'MOD", a, b)), zero));
			Exact("a HATES b but b LOVES a", Op(F.Call("'but", 
				Op(F.Call("'HATES", a, b)), Op(F.Call("'LOVES", b, a)))));
			
			// Printer doesn't allow digits in operator names
			Exact("`'f00`(a, b)", Op(F.Call("'f00", a, b)));
			Exact("a FOO b", Op(F.Call("'FOO", a, b)));
		}

		[Test]
		public void InvalidOperators()
		{
			// Not usable as prefix operators: ? = > <
			Exact("Foo(`'?`(x), `'>`(a), `'<`(b), `'=`(c))",
				F.Call(Foo, Op(F.Call("'?", x)), Op(F.Call("'>", a)),
				            Op(F.Call("'<", b)), Op(F.Call("'=", c))));
			Test(Mode.Expr, 1, "?Foo", Op(F.Call("'?", Foo)));
			Test(Mode.Expr, 1, ">Foo", Op(F.Call("'>", Foo)));
			Test(Mode.Expr, 1, "<Foo", Op(F.Call("'<", Foo)));
			Test(Mode.Expr, 1, "=Foo", Op(F.Call("'=", Foo)));
			
			// Derived operators of ? = > < can't be prefix operators either
			Exact("Foo(`'??`(x), `'>=`(a), `'<=`(b), `'==`(c))",
				F.Call(Foo, Op(F.Call("'??", x)), Op(F.Call("'>=", a)),
				            Op(F.Call("'<=", b)), Op(F.Call("'==", c))));
			Test(Mode.Expr, 1, "??Foo", Op(F.Call("'??", Foo)));

			// Try to provoke printer into making invalid output
			// "Prefix word operator"
			Exact("`'hello`(x)", Op(F.Call("'hello", x)));
			// "Suffix word operator"
			Exact("`'sufsuf`(x)", Op(F.Call("'sufsuf", x)));
			// "Prefix combo operator"
			Exact("`'ref==`(x)", Op(F.Call("'ref==", x)));
			// "Suffix combo operator"
			Exact("`'suf+suf`(x)", Op(F.Call("'suf+suf", x)));
			// "Binary suffix operator"
			Exact("`'++suf`(a, b)", Op(F.Call("'++suf", a, b)));
			// Invalid combo operator
			Exact(@"`'>s`(a, b)",  Op(F.Call(_("'>s"), a, b)));

			// Single-quoted binary operators no longer exist
			Test(Mode.Expr, 1, "a 'x b", a);

			// Comments as operators
			Exact(@"`'/*`(a, b)",  Op(F.Call(_("'/*"), a, b)));
			Exact(@"`'+/*`(a, b)", Op(F.Call(_("'+/*"), a, b)));
			Exact(@"`'//`(a, b)",  Op(F.Call(_("'//"), a, b)));

			// \ and # are not valid punctuation in operators
			Stmt (@"`'/\\`(b)", Op(F.Call(_(@"'/\"), b)));
			Stmt (@"`'#`(x)", Op(F.Call(_(@"'#"), x)));
		}

		[Test]
		public void ComboOperators()
		{
			// Precedence is determined by the punctuation portion of the operator
			Exact("x s> 1", F.Call("'s>", x, one));
			Exact("(a) code== [b]", Op(F.Call("'code==", F.InParens(a), F.Call(S.Array, b))));
			Exact("$x code== a + b", F.Call("'code==", F.Call(S.Substitute, x), F.Call(S.Add, a, b)));
			Exact("a + 1 s> b && c", Op(F.Call(S.And, Op(F.Call("'s>", Op(F.Call(S.Add, a, one)), b)), c)));
			Exact("c && $x code== a + b", F.Call(S.And, c, F.Call("'code==", F.Call(S.Substitute, x), F.Call(S.Add, a, b))));
			// A space is required after a combo operator. Whitespace doesn't count, but a newline does
			Expr ("x s>1", F.Call("'s>", x, one), 1);
			Expr ("x s>\n1", F.Call("'s>", x, OnNewLine(one)));
			Exact("x s> \n1", F.Call("'s>", x, OnNewLine(one)));
		}

		[Test]
		public void PrefixOps()
		{
			Exact("-a * b", F.Call(S.Mul, F.Call(S._Negate, a), b));
			Stmt ("-x ** +x / ~x + &x & *x && !x == ^x",
				F.Call(S.And, F.Call(S.AndBits, F.Call(S.Add, F.Call(S.Div, F.Call(S.Exp,
					F.Call(S._Negate, x), F.Call(S._UnaryPlus, x)), F.Call(S.NotBits, x)),
					F.Call(S._AddressOf, x)), F.Call(S._Dereference, x)),
					F.Call(S.Eq, F.Call(S.Not, x), F.Call(S.XorBits, x))));
			Exact("| a = %b", F.Call(S.OrBits, F.Call(S.Assign, a, F.Call(S.Mod, b))));
			Exact("..a + b && c", F.Call(S.And, F.Call(S.Add, F.Call(S.DotDot, a), b), c));
			Exact("$a / $*b", F.Call(S.Div, F.Call(S.Substitute, a), F.Call("'$*", b)));
			Exact("/x", F.Call(S.Div, x));
			Exact(@"- -a", F.Call(S.Sub, F.Call(S.Sub, a)));
			Exact(@"!! !!a", F.Call(S.PreBangBang, F.Call(S.PreBangBang, a)));
		}

		[Test]
		public void PrecedenceOverrideEffect()
		{
			// The precedence override effect is something weird that prefix operators do.
			// Whereas a.b.c means (a.b).c, a. -b.c means a.(-(b.c)). Effectively, the '-' 
			// operator lowers the precedence of the expression that follows it.
			Expr (@"a.(@@ -b)",    F.Dot(a, F.Call(S.Sub, b)));
			Exact(@"a. -b",        F.Dot(a, F.Call(S.Sub, b)));
			Expr (@"a.(@@ -b.c)",  F.Dot(a, F.Call(S.Sub, F.Dot(b, c))));
			Exact(@"a. -b++",      F.Dot(a, F.Call(S.Sub, F.Call(S.PostInc, b))));
			Exact(@"a.(@@ -b)++",  F.Call(S.PostInc, F.Dot(a, F.Call(S.Sub, b))));
			Exact("a.(@@ !b)(x)",  F.Call(F.Dot(a, F.Call(S.Not, b)), x));
			Exact("a. !b(x)",      F.Dot(a, F.Call(S.Not, F.Call(b, x))));
			Exact(@"a. -b.c",      F.Dot(a, F.Call(S.Sub, F.Dot(b, c))));
			// TODO: rethink keyword parsing (parser sees .c as keyword)
			//Exact(@"a.(@@ -b).c",     F.Dot(a, F.Call(S.Sub, b), c));
		}

		[Test]
		public void PrecedenceChallenge()
		{
			Exact("a.b::(@@ x.c)", F.Call(S.ColonColon, F.Dot(a, b), F.Dot(x, c)));
		}

		[Test]
		public void SuffixOps()
		{
			Stmt("a++ + ++a", F.Call(S.Add, F.Call(S.PostInc, a), F.Call(S.PreInc, a)));
			Stmt(@"a.b --", F.Call(@"'--suf", F.Call(S.Dot, a, b)));
			Stmt(@"a + b -<>-", F.Call(S.Add, a, F.Call(@"'-<>-suf", b)));
			// Ensure printer isn't confused by "suf" suffix which also appears on suffix operators
			Exact(@"do_suf(x)", F.Call(@"do_suf", x).SetBaseStyle(NodeStyle.Operator));
			Exact(@"`'do_suf`(x)", F.Call(@"'do_suf", x).SetBaseStyle(NodeStyle.Operator));
			Exact(@"a!! !!", F.Call(S.SufBangBang, F.Call(S.SufBangBang, a)));
			Exact(@"!!a!!", F.Call(S.PreBangBang, F.Call(S.SufBangBang, a)));
		}

		[Test]
		public void SubtractNegativeLiteral()
		{
			Expr("a-b", F.Call(S.Sub, a, b));
			Expr("x-2", F.Call(S.Sub, x, F.Literal(2)));
		}

		[Test]
		public void DollarSignOnlyAtStartOfOperator()
		{
			Stmt (@"a-$b",     F.Call(S.Sub, a, F.Call(S.Substitute, b)));
			Stmt (@"-$b",      F.Call(S.Sub, F.Call(S.Substitute, b)));
			Exact(@"`'-$`(b)", F.Call("'-$", b));
			Exact(@"$-b",      F.Call("'$-", b));
			Stmt (@"a-$-b",    F.Call(S.Sub, a, F.Call("'$-", b)));
			Exact(@"a - $-b",  F.Call(S.Sub, a, F.Call("'$-", b)));
		}

		#endregion

		#region Braces, tuples, lists

		[Test]
		public void Tuples()
		{
			Stmt("(a)", F.InParens(a));
			Exact("(a;)", F.Tuple(a));
			Stmt("(;)", F.Tuple(_("")));
			Exact("(``;)", F.Tuple(_("")));
			Stmt("(a; ;)", F.Tuple(a, _("")));
			Stmt("(a; b)", F.Tuple(a, b));
			Stmt("(a; ``)", F.Tuple(a, _("")));
			Stmt("(a; b; c + x)", F.Tuple(a, b, F.Call(S.Add, c, x)));
			Stmt("(a, b, c + x)", F.Tuple(a, b, F.Call(S.Add, c, x)));
		}

		[Test]
		public void Stmts()
		{
			Test(Mode.Stmt, -1, "a;\nb;\nc", a, b, c);
			Stmt("a.b(c)", F.Call(F.Dot(a, b), c));
			Expr("{\n  b(c);\n} + {\n  ;\n  Foo()\n}", F.Call(S.Add, F.Braces(F.Call(b, c)), F.Braces(F.Missing, F.Call(Foo))));
			Stmt("a.{\n  b;\n  c;\n}()", F.Call(F.Dot(a, F.Braces(b, c))));
			Stmt(@"{ ""key"": ""value"", {""KEY"": ""VALUE""} }", F.Braces(
				F.Call(S.Colon, F.Literal("key"), F.Literal("value")), F.Braces(
				F.Call(S.Colon, F.Literal("KEY"), F.Literal("VALUE")))));
		}

		[Test]
		public void ListLiterals()
		{
			Exact(@"x = [1, 2, ""three""]", F.Call(S.Assign, x, 
				F.Call(S.Array, one, two, F.Literal("three"))));
			Stmt(@"{ ""a"" : [null], ""b"" : [] }", F.Braces(
				F.Call(S.Colon, F.Literal("a"), F.Call(S.Array, F.Null)), 
				F.Call(S.Colon, F.Literal("b"), F.Call(S.Array))));
			Exact("++[x]", F.Call(S.PreInc, F.Call(S.Array, x)));
			Exact("Foo = [a, b, c]", F.Call(S.Assign, Foo, F.Call(S.Array, a, b, c)));
			Exact("Foo = [a, b, c] + [x]", F.Call(S.Assign, Foo, F.Call(S.Add, F.Call(S.Array, a, b, c), F.Call(S.Array, x))));
		}

		#endregion
		
		#region Generics, indexers

		[Test]
		public void Generics()
		{
			Exact("a!b", F.Of(a, b));
			Expr ("a!(b)", F.Of(a, b));
			Exact("a!(b, c)", F.Of(a, b, c));
			Exact("a!()", F.Of(a));
			Exact("a.b!((x))", F.Dot(a, F.Of(b, F.InParens(x))));
			Exact("(@@ a.b)!Foo", F.Of(F.Dot(a, b), Foo));
			Exact("a.b!Foo",      F.Dot(a, F.Of(b, Foo)));
			Exact("a.b!Foo(x)", F.Call(F.Dot(a, F.Of(b, Foo)), x));
			Exact("a.b!(Foo.Foo)(x)", F.Call(F.Dot(a, F.Of(b, F.Dot(Foo, Foo))), x));
			Exact("a.b!(Foo(x))", F.Dot(a, F.Of(b, F.Call(Foo, x))));
			Expr ("Foo = a.b!c!x", F.Call(S.Assign, Foo, F.Dot(a, F.Of(b, F.Of(c, x)))));
			Exact("Foo = a.b!(c!x)", F.Call(S.Assign, Foo, F.Dot(a, F.Of(b, F.Of(c, x)))));
		}
		
		[Test]
		public void IndexBracks()
		{
			Exact("a[]", F.Call(S.IndexBracks, a));
			Exact("a[b]", F.Call(S.IndexBracks, a, b));
			Exact("a[b, c]", F.Call(S.IndexBracks, a, b, c));
		}

		#endregion

		#region Attributes

		[Test]
		public void SimpleAttributes()
		{
			Exact("@Foo a = b(c)",     F.Attr(Foo, F.Call(S.Assign, a, F.Call(b, c))));
			Exact("@Foo (a)(b)",       F.Attr(Foo, F.Call(F.InParens(a), b)));
			Exact("@123 Foo(x)",       F.Attr(F.Literal(123), F.Call(Foo, x)));
			Exact("@'!' Foo(x)",       F.Attr(F.Literal('!'), F.Call(Foo, x)));
			Exact("@n\"-12\" Foo(x)",  F.Attr(F.Literal(-12), F.Call(Foo, x)));
		}

		[Test]
		public void BraceAndBrackAttributes()
		{
			Exact("@[1, 2] Foo(x)",    F.Attr(F.Call(S.Array, one, two), F.Call(Foo, x)));
			Exact("@{\n  a\n} Foo(x)", F.Attr(F.Braces(a), F.Call(Foo, x)));
		}

		[Test]
		public void ParenAttributes()
		{
			Stmt("@(Foo) a = b(c)",    F.Attr(Foo, F.Call(S.Assign, a, F.Call(b, c))));
			Exact("@((Foo)) b(c)",     F.Attr(F.InParens(Foo), F.Call(b, c)));
			Stmt("@(Foo(x)) a = b(c)", F.Attr(F.Call(Foo, x), F.Call(S.Assign, a, F.Call(b, c))));
			Exact("@(@0 Foo(x)) b(c)", F.Attr(F.Attr(zero, F.Call(Foo, x)), F.Call(b, c)));
			Exact("@(a == c) b(c)",    F.Attr(F.Call(S.Eq, a, c), F.Call(b, c)));
			Exact("(@Foo a) = b(c)",   F.Call(S.Assign, F.Attr(Foo, a), F.Call(b, c)));
			Exact("((@Foo a)) = b(c)", F.Call(S.Assign, F.InParens(F.Attr(Foo, a)), F.Call(b, c)));
			Exact("(@Foo (a)) = b(c)", F.Call(S.Assign, F.Attr(Foo, F.InParens(a)), F.Call(b, c)));
		}

		#endregion

		#region Block expressions, and keyword statements
		
		[Test]
		public void KeywordStatements()
		{
			LNode code;
			Exact(".return",           KeywordExpr(F.Call("#return")));
			Exact(".on_catch Foo()",   KeywordExpr(F.Call("#on_catch", F.Call(Foo))));
			Exact("#on_catch(Foo())",              F.Call("#on_catch", F.Call(Foo)));
			Test(Mode.Exact, 0, ".return\nFoo()", KeywordExpr(F.Call("#return")), F.Call(Foo));
			Test(Mode.Expr, 1, ".`foo`");
			Exact(".return (x + 1)",   KeywordExpr(F.Call("#return", F.InParens(F.Call(S.Add, x, one)))));
			Exact(".return x + 1",     KeywordExpr(F.Call("#return", F.Call(S.Add, x, one))));
			Exact(".if Foo {\n  a\n}", KeywordExpr(F.Call("#if", Foo, F.Braces(a))));
			Exact(".if Foo {\n  a\n} else {\n  b\n}", KeywordExpr(F.Call("#if", Foo, F.Braces(a), 
				F.Call("#else", F.Braces(b)))));
			code = KeywordExpr(F.Call("#class",
				F.Call("'where", F.Of(Foo, T), F.Call(S.Colon, T, F.Id("IFoo"))),
				F.Braces(a)));
			Stmt (".class Foo!T where T : IFoo {\n  a\n}", code);
			code = F.Braces(
				KeywordExpr(F.Call("#while", c, F.Braces(F.Id("body")))),
				F.Call("andSoOn"));
			Stmt("{\n"+
			     "  .while c {\n    body\n  }\n"+
			     "  andSoOn()\n"+
				 "}", code);
			if (this is Les3ParserTests) { // TODO: make printer support newlines in kw exprs
				code = F.Braces(
					KeywordExpr(F.Call("#while", c, OnNewLine(F.Braces(F.Id("body"))))),
					F.Call("andSoOn"));
				Stmt("{\n"+
					 "  .while c\n  {\n    body\n  }\n"+
					 "  andSoOn()\n"+
					 "}", code);
			}
			code = F.Braces(
				KeywordExpr(F.Call("#if", c, 
				  F.Braces(F.Id("then")),
				  F.Call("#else",
				    F.Braces(F.Id("else"))))),
				F.Call("andSoOn"));
			Stmt("{\n"+
			     "  .if c {\n    then\n  } else {\n  else;\n}\n"+
			     "  andSoOn()\n"+
				 "}", code);
			if (this is Les3ParserTests) { // TODO: make printer support newlines in kw exprs
				code = F.Braces(
					KeywordExpr(F.Call("#if", c, 
					  OnNewLine(F.Braces(F.Id("then"))),
					  OnNewLine(F.Call("#else",
						OnNewLine(F.Braces(F.Id("else"))))))),
					F.Call("andSoOn"));
				Stmt("{\n"+
					 "  .if c\n  {\n    then\n  }\n"+
					 "  else\n  {\n  else;\n}\n"+
					 "  andSoOn()\n"+
					 "}", code);
			}
		}

		[Test]
		public void KeywordStmtWithMissingExpr()
		{
			// Here, the braced block counts as the initial expression
			var stmt = KeywordExpr(F.Call("#try",
						   F.Braces(F.Call(Foo)),
						   F.Call("#catch", F.Id("Exception"), F.Braces())));
			Exact(".try {\n  Foo()\n} catch Exception { }", stmt);
			if (this is Les3ParserTests) { // TODO: make printer support this
				// But the newline here means that the initial expression is missing
				stmt = KeywordExpr(F.Call("#try",
						   OnNewLine(F.Braces(F.Call(Foo))),
						   F.Call("#catch", F.Id("Exception"), F.Braces())));
				Exact(".try\n{\n  Foo()\n} catch Exception { }", stmt);
			}
			// TODO: try to trick printer into printing newlines that make a keyword statement invalid
			// TODO: try to trick printer into printing something with invalid continuators
			// TODO: try to trick printer into printing a newline before a word operator
		}

		[Test]
		public void AvoidKeywordStatementAmbiguity()
		{
			// TODO: try to trick printer into printing wrong output for this: Foo = .foo {}; \n __else(x)
			if (this is Les3ParserTests) // TODO: make printer support this
				Test(Mode.Exact, 0, ".return\n{ }", KeywordExpr(F.Call("#return", OnNewLine(F.Braces()))));
			Test(Mode.Exact, 0, ".return;\n{ }", KeywordExpr(F.Call("#return")), F.Braces());
			Test(Mode.Exact, 0, ".return;\n{ }", KeywordExpr(F.Call("#return")), F.Braces());
			Test(Mode.Stmt , 0, ".return\n@@ { }", KeywordExpr(F.Call("#return")), F.Braces());
			Test(Mode.Exact, 0, ".if true;\n{ }", KeywordExpr(F.Call("#if", F.True)), F.Braces());
			Test(Mode.Exact, 0, "{\n  .return;\n  { }\n}", F.Braces(KeywordExpr(F.Call("#return")), F.Braces()));
		}

		[Test]
		public void DotIdentifierIsKeyword()
		{
			Exact("abc.foo", F.Dot(F.Id("abc"), F.Id("foo")));
			Test(Mode.Stmt, 1, "123 .foo", F.Literal(123)); // error
		}

		#endregion

		#region Token lists Prefix notation demarcated with single-quote (')

		[Test]
		public void BasicPrefixNotation()
		{
			// TODO: add printer support
			Stmt("' a 2 'z'", F.Call(S.SingleQuote, a, two, F.Literal('z')));
			Stmt("' + x 1", F.Call(S.SingleQuote, _(S.Add), x, one));
			Stmt("' ()", F.Call(S.SingleQuote, F.Call(S.Parens)));
		}

		#endregion

		[Test]
		public void PrinterSpacingMinefield()
		{
			// LESv3 changed; `-` is no longer challenging. We could remove these tests.
			Exact("-2", F.Call(S.Sub, two));
			Exact("-2.5", F.Call(S.Sub, F.Literal(2.5)));
			Exact("+n\"-2\"", F.Call(S.Add, F.Literal(-2)));
			Exact("+-2", F.Call("'+-", F.Literal(2)));
			Exact("-(2.5)", F.Call(S.Sub, F.InParens(F.Literal(2.5))));
			Exact("-x** +x", F.Call(S.Exp, F.Call(S._Negate, x), F.Call(S._UnaryPlus, x)));
			// Challenges involving `.`
			Exact("x.x", F.Dot(x, x));
			Exact("x. 2", F.Dot(x, two));
			Exact("2 . x", F.Dot(two, x));
			Exact("1 . 2", F.Dot(one, two));
			Exact("1 x. 2", F.Call("'x.", one, two));
			Exact("@@inf.d . Foo", F.Dot(F.Literal(double.PositiveInfinity), Foo));
			Exact("0x2 . Ep0", F.Dot(two.WithStyle(NodeStyle.HexLiteral), F.Id("Ep0")));
		}

		[Test]
		public void TrickPrinterWithParens()
		{
			// Parens attributes on operator targets
			Exact("(`'-`)(x)",      Op(F.Call(F.InParens(F.Id(S.Sub)), x)));
			Exact("(`'++suf`)(x)",  Op(F.Call(F.InParens(F.Id(S.PostInc)), x)));
			Exact("(`'+`)(a, b)",   Op(F.Call(F.InParens(F.Id(S.Add)), a, b)));
			Exact("(`.foo`)(a, b)", Op(F.Call(F.InParens(F.Id(".foo")), a, b)));
		}

		[Test]
		public void OperatorFollowedByFraction()
		{
			Expr ("1**.2", Op(F.Call(S.Exp, one, F.Literal(0.2))));
			Exact("1**0.2", Op(F.Call(S.Exp, one, F.Literal(0.2))));
			Exact("1**. 2", Op(F.Call(_("'**."), one, two)));
			Expr ("1*.2", Op(F.Call(S.Mul, one, F.Literal(0.2))));
			Exact("1*. 2", Op(F.Call(_("'*."), one, two)));
			Expr ("1.*.2", Op(F.Call(_("'.*"), one, F.Literal(0.2))));
			// TODO: Shouldn't the printer print this as "1..2"? It's using "1 .. 2"
			Expr("1..2", Op(F.Call(S.DotDot, one, two)));
			Expr("1.*. 2", Op(F.Call(_("'.*."), one, two)));
		}

		[Test]
		public void TriviaTest_Comments()
		{
			LNode node;
			node = Foo.PlusAttr(F.Trivia(S.TriviaMLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/* before */Foo /* after */", node);
			node = one.PlusAttr(F.Trivia(S.TriviaSLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " after "));
			Exact("// before \n1\t// after ", node);

			Exact("Foo(a,\t// Commentary\n  b)",
				F.Call(Foo, a.PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, " Commentary")), OnNewLine(b)));

			node = F.Call(F.Id(S.Eq).PlusAttr(                 F.Trivia(S.TriviaMLComment, "[")).PlusTrailingTrivia(F.Trivia(S.TriviaSLComment, "]")), 
			                 Foo, x.PlusAttrs(F.TriviaNewline, F.Trivia(S.TriviaMLComment, "x"))
			                                 ).PlusAttr(F.Trivia(S.TriviaMLComment, " before ")).PlusTrailingTrivia(F.Trivia(S.TriviaMLComment, " after "));
			Exact("/* before */Foo /*[*/==\t//]\n/*x*/x /* after */", node);

			node = F.Call(Foo).PlusAttrs(a, F.Trivia(S.TriviaSLComment, "Comment after a"),
				b, F.Trivia(S.TriviaMLComment, "Comment before c"), c);
			Exact("@a //Comment after a\n"+
			      "@b /*Comment before c*/@c Foo()", node);
		}

		[Test]
		public void TriviaTest_Appending()
		{
			var append = F.Id(S.TriviaAppendStatement);
			LNode[] stmts = {
				F.Braces(F.Call(a), F.Call(b)).SetStyle(NodeStyle.OneLiner)
			};
			Test(Mode.Exact, 0, "{ a(); b() }", stmts);
			stmts = new[] {
				F.Call(a), F.Call(b), F.Call(c).PlusAttr(append)
			};
			Test(Mode.Exact, 0, "a()\nb(); c()", stmts);
		}

		[Test]
		public void TriviaTest_LineBreakBetweenAttrs()
		{
			Exact("@a \nFoo()", F.Call(Foo).PlusAttrs(a, F.TriviaNewline));
			Exact("@a @b \n@c \nFoo()", F.Call(Foo).PlusAttrs(a, b, F.TriviaNewline, c, F.TriviaNewline));
		}

		[Test]
		public void TriviaTest_BlankLinesBetweenStmts()
		{
			Test(Mode.Exact, 0, "a()\n\nb()\n\nc()",
				NewlineAfter(F.Call(a)),
				NewlineAfter(F.Call(b)),
				F.Call(c));
			Exact("{\n\n  a()\n  \n  b()\n  \n  c()\n  \n}",
				F.Call(NewlineAfter(F.Id(S.Braces)),
					NewlineAfter(F.Call(a)),
					NewlineAfter(F.Call(b)),
					NewlineAfter(F.Call(c))));
		}

		public void TriviaTest_BlankLinesBetweenArgs()
		{
			Exact("Foo(\n  a, \n  b, \n  c)",
				F.Call(Foo, OnNewLine(a), OnNewLine(b), OnNewLine(c)));

			Exact("Foo(\n  a, \n  \n  b, \n  // see?\n  c)",
				F.Call(Foo, NewlineAfter(OnNewLine(a)),
					NewlineAfter(OnNewLine(b)),
					c.PlusAttr(F.Trivia(S.TriviaSLComment, " see?"))));

			// TODO: this fails in printer because the newline after Foo cannot
			// be printed before '(' and is suppressed. Solution will be to wait
			// until after '(' before printing trivia attached to Foo, but this
			// is not easy to accomplish.
			//Test("Foo(\n  \n  a, \n  \n  b, \n  \n  c)",
			//	F.Call(NewlineAfter(Foo), NewlineAfter(OnNewLine(a)),
			//		NewlineAfter(OnNewLine(b)),
			//		OnNewLine(c)));
		}

		protected virtual void Expr(string text, LNode expr, int errorsExpected = 0)
		{
			Test(Mode.Expr, errorsExpected, text, new[] { expr });
		}
		protected virtual void Stmt(string text, LNode code, int errorsExpected = 0)
		{
			Test(Mode.Stmt, errorsExpected, text, new[] { code });
		}
		protected virtual void Exact(string text, LNode code, int errorsExpected = 0)
		{
			Test(Mode.Exact, errorsExpected, text, new[] { code });
		}

		/// <summary>Runs a printer or parser test.</summary>
		/// <param name="parseErrors">-1 if the printer and parser should both 
		/// test this example. If above -1, only the parser will run this example,
		/// and this parameter specifies the number of parse errors to expect 
		/// (may be 0).</param>
		protected abstract MessageHolder Test(Mode mode, int parseErrors, string text, params LNode[] code);
		protected enum Mode
		{
			Expr = 0,  // Parse expression list
			Stmt = 1,  // Parse statement list
			Exact = 3, // Parse statements, and expect exact (rather than equivalent) printer output
		}

		protected void ExpectMessageContains(MessageHolder messages, params string[] substrings)
		{
			foreach (var msg in messages.List)
				for (int i = 0; i < substrings.Length; i++)
					if (msg.Formatted.IndexOf(substrings[i], StringComparison.InvariantCultureIgnoreCase) > -1)
						substrings[i] = null;
			Assert.AreEqual(null, substrings.WhereNotNull().FirstOrDefault());
		}
	}
}
