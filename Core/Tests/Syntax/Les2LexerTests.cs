using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Numerics;
using Loyc.MiniTest;
using Loyc.Syntax.Lexing;
using Loyc.Utilities;
using Loyc;

namespace Loyc.Syntax.Les
{
	using S = Loyc.Syntax.CodeSymbols;
	using TT = TokenType;

	[TestFixture]
	public class Les2LexerTests
	{
		[DebuggerStepThrough] static Symbol _(string s) => (Symbol) s;
		[DebuggerStepThrough] static T[] A<T>(params T[] list) => list;

		static readonly object WS = WhitespaceTag.Value;
		Pair<string, Symbol> L(string v, string tm) => Pair.Create(v, (Symbol)tm);
		Pair<string, Symbol> Num(string v) => L(v, "_");
		Pair<string, Symbol> Num(int x) => L(x.ToString(), "_");
		Pair<string, Symbol> Num(float x) => L(x.ToString(), "_f");
		Pair<string, Symbol> Num(double x) => L(x.ToString(), "_d");
		Pair<string, Symbol> Str(string v) => L(v, null);
		Pair<string, Symbol> Sym(string v) => L(v, "s");

		[Test]
		public void Basics()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Id, TT.Not), 
				_("hello"), _("',"), _("world"), _("'!"));
			Case("this is\t\"just\"1 lexer test '!'",
				A(TT.Id, TT.Id, TT.Literal, TT.Literal, TT.Id, TT.Id, TT.Literal),
				_("this"), _("is"), L("just", null), Num(1), _("lexer"), _("test"), L("!", "c"));
			Case(@"12:30", A(TT.Literal, TT.NormalOp, TT.Literal), Num(12), _("':"), Num(30));
			Case(@"c+='0'", A(TT.Id, TT.Assignment, TT.Literal), _("c"), _("'+="), L("0","c"));
			Case("// hello\n\r\n\r/* world */",
				A(TT.SLComment, TT.Newline, TT.Newline, TT.Newline, TT.MLComment));
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen), null, null, null, null, null, null);
			Case(@"finally@@{`boom!` ;}", A(TT.Id, TT.At, TT.At, TT.LBrace, TT.BQOperator, TT.Semicolon, TT.RBrace),
				_("finally"), _(""), _(""), null, _("boom!"), _("';"), null);
			Case("a\n\"b\"", A(TT.Id, TT.Newline, TT.Literal), _("a"), WS, Str("b"));
		}

		[Test]
		public void TestIdentifiers()
		{
			Case("abc_123/_0",   A(TT.Id, TT.NormalOp, TT.Id),       _("abc_123"), _("'/"), _("_0"));
			Case("is@is",        A(TT.Id, TT.Id),                    _("is"), _("is"));
			Case("No#error",     A(TT.Id),                           _("No#error"));
			Case("#error.",      A(TT.Id, TT.Dot),                   _("#error"), _("'."));
			Case("@#food:@yum",  A(TT.Id, TT.Id),                    _("#food:"), _("yum"));
			Case(@"#()/",        A(TT.Id, TT.LParen, TT.RParen, TT.NormalOp), _("#"), null, null, _(@"'/"));
			Case(@"@/*@$@==>@??.",A(TT.Id, TT.Id, TT.Id, TT.Id),     _(@"/*"), _("$"), _("==>"), _("??."));
			Case("@>>@>>=@<<",   A(TT.Id, TT.Id, TT.Id),             _(">>"), _(">>="), _("<<"));
			Case(@"@0@`@\n`",    A(TT.Id, TT.Id),                    _("0"), _("@\n"));
			Case("won't prime'", A(TT.Id, TT.Id),         _("won't"), _("prime'"));
			Case("@+- /**/",     A(TT.Id, TT.MLComment),  _("+-"), WS);
			Case("@'+- /**/",    A(TT.Id, TT.MLComment),  _("'+-"), WS);
			Case("@+-/**/",      A(TT.Id),                           _("+-/**/"));
		}

		[Test]
		public void IdLikeLiterals()
		{
			Case("@null",           A(TT.Literal),               new object[] {null});
			Case("@true@false",     A(TT.Literal, TT.Literal),  true, false);
			Case("@foo",            A(TT.Id),                     _("foo"));
			Case("@`true`@`false`", A(TT.Id, TT.Id),              _("true"), _("false"));
		}

		[Test]
		public void TestNormalStrings()
		{
			Case(@"`Testing`""Testing""'!'", A(TT.BQOperator, TT.Literal, TT.Literal), _("Testing"), Str("Testing"), L("!", "c"));
			Case(@"`\a\b\f\v\`\'\""`""\a\b\f\v\`\'\""""'\0'", A(TT.BQOperator, TT.Literal, TT.Literal),
				_("\a\b\f\v`\'\""), Str("\a\b\f\v`\'\""), L("\0", "c"));
			// There are no C#-style 'verbatim' strings in LES, use triple-quoted strings instead.
			Case(@"$""\n"" @""\\""", A(TT.PrefixOp, TT.Literal, TT.At, TT.Literal), _("'$"), Str("\n"), _(""), Str("\\"));
			// Previously caused an error, but now that we're using uninterpreted literals it doesn't
			Case("'abc'", A(TT.Literal), L("abc", "c"));
		}

		[Test]
		public void TestTQStrings()
		{
			Case("\"\"\"Hello'''', quotes!\"\"\".", A(TT.Literal, TT.Dot), Str("Hello'''', quotes!"), _("'."));
			Case("'''Hello\"\"\"\", quotes!'''.", A(TT.Literal, TT.Dot), Str("Hello\"\"\"\", quotes!"), _("'."));
			Case("'''Hello\"\"\"\", quotes!'''.", A(TT.Literal, TT.Dot), Str("Hello\"\"\"\", quotes!"), _("'."));
			
			// Triple-quoted strings always use \n as the line separator, 
			// and ignore indentation insofar as it matches the first line.
			Case("  '''One\n  Two'''",    A(TT.Literal), Str("One\nTwo"));
			Case("  '''One\r  Two'''",    A(TT.Literal), Str("One\nTwo"));
			Case("  '''One\r\n  Two'''",  A(TT.Literal), Str("One\nTwo"));
			Case("\t\t'''One\n\t\tTwo'''",A(TT.Literal), Str("One\nTwo"));
			Case(" '''One\r  Two'''",     A(TT.Literal), Str("One\n Two"));
			Case("\t '''One\r\tTwo'''",   A(TT.Literal), Str("One\nTwo"));
			Case("  '''One\r\tTwo'''",    A(TT.Literal), Str("One\n\tTwo"));
			Case(" \t '''One\r \t  \tTwo'''", A(TT.Literal), Str("One\n \tTwo"));
			Case("  '''One\nTwo\n   Three'''", A(TT.Literal), Str("One\nTwo\n Three"));
			
			// Triple-quoted strings also support escape sequences: \\\, \\n, \\r, \\", \\'
			Case(@"'''Three quotes: ''\'/!'''", A(TT.Literal), Str("Three quotes: '''!"));
			Case(@"'''Escapes: \r/\n/, \\/, \""/, \0/ and \'/'''.", A(TT.Literal, TT.Dot), Str("Escapes: \r\n, \\, \", \0 and '"), _("'."));
			Case(@"'''Escaped: \\/r/\\/n/, \\/\/, \\/""/, and \\/'/'''.", A(TT.Literal, TT.Dot), Str(@"Escaped: \r/\n/, \\/, \""/, and \'/"), _("'."));
			Case(@"'''Unrecognized escapes: \//\o/'''", A(TT.Literal), Str(@"Unrecognized escapes: \//\o/"));
		}

		[Test]
		public void TestDotIndents()
		{
			// A dot-indented line must start with a dot and each dot must be followed by a space.
			Case(". Hello", A(TT.Id), _("Hello"));
			Case(" .\n. ", A(TT.Dot, TT.Newline), _("'."), WS);
			Case(".   .  . Hello", A(TT.Id), _("Hello"));
			Case(".\t.\t.\tHello\n.. Goodbye", A(TT.Id, TT.Newline, TT.NormalOp, TT.Id), _("Hello"), WS, _("'.."), _("Goodbye"));
			Case(". .Hello",  A(TT.Dot, TT.Id), _("'."), _("Hello"));
			Case(". ..Hello", A(TT.NormalOp, TT.Id), _("'.."), _("Hello"));
		}

		[Test]
		public void TestShebang()
		{
			Case("#!/bin/sh\r\n// that's called a shebang!",
				A(TT.Shebang, TT.SLComment));
			Case(".#!/bin/sh",
				A(TT.Dot, TT.Id, TT.NormalOp, TT.Id, TT.NormalOp, TT.Id),
				_("'."), _("#"), _("'!/"), _("bin"), _("'/"), _("sh"));
		}

		[Test]
		public void TestSimpleLiterals()
		{
			Case("1 1.0f 1.0 0x1 0b1", A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				Num(1), L("1.0", "_f"), Num("1.0"), Num("0x1"), Num("0b1"));
			Case("1L 2UL 3u 4f 5d6m", A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				L("1", "_L"), L("2", "_UL"), L("3", "_u"), Num(4f), L("5", "_d6m"));
			Case("@true @false @null @void", 
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				true, false, null, @void.Value);
			Case("@@symbol \"string\"", A(TT.Literal, TT.Literal), Sym("symbol"), Str("string"));
			Case(@"'\'''!'", A(TT.Literal, TT.Literal), L("\'", "c"), L("!", "c"));
		}

		[Test]
		public void TestCustomLiterals()
		{
			Case("hi there\"Dave!\"", A(TT.Id, TT.Literal), _("hi"), L("Dave!", "there"));
			Case("hi there\"\"\"Josh!\"\"\"", A(TT.Id, TT.Literal), _("hi"), L("Josh!", "there"));
			Case("foo'''string'''", A(TT.Literal), L("string", "foo"));
			Case(@"..s""Hi!""", A(TT.NormalOp, TT.Literal), _("'.."), Sym("Hi!"));
			Case(@"s""Hi!\n""  ", A(TT.Literal), Sym("Hi!\n"));
			Case(@"@s""Hi!\n""", A(TT.Literal), Sym("Hi!\n"));
			Case("2.3f 9.9e9d", A(TT.Literal, TT.Literal), L("2.3", "_f"), L("9.9e9", "_d"));
			Case("0b101a 0b1p1q", A(TT.Literal, TT.Literal), L("0b101", "_a"), L("0b1p1", "_q"));
			Case("0b101.1abc", A(TT.Literal), L("0b101.1", "_abc"));
			Case("0x2.3p+2f", A(TT.Literal), L("0x2.3p+2", "_f"));
			Case("hi 0x2.3f", A(TT.Id, TT.Literal), _("hi"), L("0x2.3f", "_"));
			Case("0x2.3e-2d", A(TT.Literal, TT.NormalOp, TT.Literal), L("0x2.3e", "_"), _("'-"), Num(2.0));
		}

		[Test]
		public void TestOperators()
		{
			Case(@"%", A(TT.NormalOp), _(@"'%"));
			Case("`backquoted`x", A(TT.BQOperator, TT.Id), _("backquoted"), _("x"));
			//Case(@"\++ ++ ++\", A(TT.PreOrSufOp, TT.PreOrSufOp, TT.SuffixOp), _("++"), _("++"), _(@"++\"));
			//Case(@"\solve-for-x x\squared\", A(TT.NormalOp, TT.Id, TT.SuffixOp), _("solve-for-x"), _("x"), _(@"squared\"));
			Case(@"+++x---", A(TT.PreOrSufOp, TT.Id, TT.PreOrSufOp), _("'+++"), _("x"), _("'---"));
			//Case(@"$x\y\`bq`\", A(TT.PrefixOp, TT.Id, TT.SuffixOp, TT.BQString, TT.NormalOp), _("$"), _("x"), _(@"y\"), _("bq"), _(@"\"));
			Case(@"$~!%^&*-+=|<>/?:._", A(TT.PrefixOp, TT.Id), _("'$~!%^&*-+=|<>/?:."), _("_"));
			Case(@"$~!%^&*-+=|<>_/?:.", A(TT.PrefixOp, TT.Id, TT.Dot), _("'$~!%^&*-+=|<>"), _("_"), _("'/?:."));
			Case(@"@~!%^&*-+=|<>@@/?:.$", A(TT.Id, TT.Literal), _("~!%^&*-+=|<>"), Sym("/?:.$"));
			Case(@"@,!;: :^=", A(TT.At, TT.Comma, TT.Not, TT.Semicolon, TT.NormalOp, TT.Assignment), _(""), _("',"), _("'!"), _("';"), _("':"), _(@"':^="));
		}

		[Test]
		public void TestIntegers()
		{
			Case("9", A(TT.Literal), Num(9));
			Case("1337", A(TT.Literal), Num(1337));
			Case("−1", A(TT.Literal), Num("−1"));
			Case("9111222U", A(TT.Literal), L("9111222", "_U"));
			Case("0L", A(TT.Literal), L("0", "_L"));
			Case("−9111222L", A(TT.Literal), L("−9111222", "_L"));
			Case("-1U", A(TT.NormalOp, TT.Literal), _("'-"), L("1", "_U"));
			Case("9_111_222", A(TT.Literal), Num("9_111_222"));
			Case("9_111_222_333", A(TT.Literal), Num("9_111_222_333"));
			Case("11Z", A(TT.Literal), L("11", "_Z"));
			Case("9_111_222_333_444_555Z", A(TT.Literal), L("9_111_222_333_444_555", "_Z"));
			// These used to be errors until it started supporting uninterpreted literals
			Case("0xFF_0000_0000U", A(TT.Literal), L("0xFF_0000_0000", "_U"));
			Case("0xFFFF_FFFF_0000_0000L", A(TT.Literal), L("0xFFFF_FFFF_0000_0000", "_L"));
		}

		[Test]
		public void TestHexEscapeSequences()
		{
			Case(@"""\u0020\U00020\x20""", A(TT.Literal), Str("   "));
			Case(@"""\U0020\u00200\x20""", A(TT.Literal), Str("  0 "));
			Case(@"""\u0009\u00009\x09""", A(TT.Literal), Str("\t\09\t"));
			Case(@"""\u2202\U02202\x2202""", A(TT.Literal), Str("\u2202\u2202\"02")); // • Bullet
			Case(@"""\U1F4A9\U10FFFF""", A(TT.Literal), Str("\uD83D\uDCA9\uDBFF\uDFFF")); // 💩 pile of poo and highest code point
			Case(@"""\u1F4A9\u10FFFF""", A(TT.Literal), Str("\u1F4A9\u10FFFF")); // only 4 digits are part of each escape sequence
			Case(@"""\U1F4A90""", A(TT.Literal), ERROR); // This escape is not really 6 digits
			Case(@"""\U110000""", A(TT.Literal), ERROR); // The greatest escape U+10FFFF plus 1
		}

		[Test]
		public void TestFloats()
		{
			Case("0.0", A(TT.Literal), Num("0.0"));
			Case("0.1", A(TT.Literal), Num("0.1"));
			Case("25d 25f,25m", A(TT.Literal, TT.Literal, TT.Comma, TT.Literal), Num(25d), Num(25f), _("',"), L("25", "_m"));
			Case("0.25d", A(TT.Literal), Num(0.25d));
			Case("0.25f", A(TT.Literal), Num(0.25f));
			Case("0.25e2", A(TT.Literal), Num("0.25e2"));
			Case("10e-20", A(TT.Literal), Num("10e-20"));
			Case("0.3e+2d", A(TT.Literal), L("0.3e+2", "_d"));
			Case("0.3e+2f", A(TT.Literal), L("0.3e+2", "_f"));
			Case("0.3e+2m", A(TT.Literal), L("0.3e+2", "_m"));
			Case("1234567890123456789012345678901234567890d", A(TT.Literal), 
			   L("1234567890123456789012345678901234567890", "_d"));
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(TT.Literal), 
			   L("123456789012345678901234567890.1234567890123456789012345678901234567890", "_f"));
			Case(".5e+2.5e+2f.5m", A(TT.Literal, TT.Literal, TT.Literal), 
			   Num(".5e+2"), L(".5e+2", "_f"), L(".5", "_m"));
			Case("Y.5", A(TT.Id, TT.Literal), _("Y"), Num(".5"));
			Case("0.1.5", A(TT.Literal, TT.Literal), Num("0.1"), Num(".5"));
			Case("5.ToString", A(TT.Literal, TT.Dot, TT.Id), Num(5), _("'."), _("ToString"));
		}
		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.0p1234", A(TT.Literal), Num("0x0.0p1234"));
			Case("0x0.0", A(TT.Literal), Num("0x0.0"));
			Case("0xF.8", A(TT.Literal), Num("0xF.8"));
			Case("0xF.8p+1;0xF.8p1", A(TT.Literal, TT.Semicolon, TT.Literal), Num("0xF.8p+1"), _("';"), Num("0xF.8p1"));
			Case("0xA.8p-1", A(TT.Literal), Num("0xA.8p-1"));
			Case("0b101.01", A(TT.Literal), Num("0b101.01"));
			Case("0b101.01p0f", A(TT.Literal), L("0b101.01p0", "_f"));
			Case("0b101.01p2", A(TT.Literal), Num("0b101.01p2"));
			Case("0b1111_1111.1111_1111p+8", A(TT.Literal), Num("0b1111_1111.1111_1111p+8"));
			Case("0b.1p-2", A(TT.Literal), Num("0b.1p-2"));
			Case("0b.1p-2f", A(TT.Literal), L("0b.1p-2", "_f"));
		}

		[Test]
		public void TestSymbols()
		{
			Case(@"@@public@@is@@A@@`common\\word`@@around@@here",
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				Sym("public"), Sym("is"), Sym("A"), Sym(@"common\word"), Sym("around"), Sym("here"));
			Case(@"@@+-*/", A(TT.Literal), Sym("+-*/"));
			Case(@"@@+- //x", A(TT.Literal, TT.SLComment), Sym("+-"), WS);
			Case(@"@@+-//x", A(TT.Literal), Sym("+-//x"));
		}

		const string ERROR = "ERROR";

		[Test]
		public void EofInToken()
		{
			Case(@"""", A(TT.Literal), ERROR);
			Case(@"'", A(TT.Literal), ERROR);
			Case(@"`", A(TT.BQOperator), ERROR);
			//Case(@"\\", A(TT.SuffixOp), _(@"\"));
			Case(@"@", A(TT.At), _(@""));
			Case(@"@@", A(TT.At, TT.At));
			Case(@"2.0e+", A(TT.Literal), ERROR);
		}

		[Test]
		public void TestErrors()
		{
			//Case("\0",              A(TT.Error), ERROR);
			//Case("\x07",            A(TT.Error), ERROR);
			Case("x=\"Hello\n",     A(TT.Id, TT.Assignment, TT.Literal, TT.Newline), _("x"), _("'="), ERROR, WS);
			Case("'\n'o''pq\n?''",  A(TT.Literal, TT.Newline, TT.Literal, TT.Literal, TT.Newline, TT.NormalOp, TT.Literal),
			                        ERROR, WS, L("o", "c"), ERROR, WS, _("'?"), L("", "c"));
			// Probably "0x" should be an error, but it's a minor hassle to detect 
			// the error so it is left undetected.
			Case("0x!0b",           A(TT.Literal, TT.Not, TT.Literal), Num("0x"), _("'!"), ERROR);
			Case("`weird\nnewline", A(TT.BQOperator, TT.Newline, TT.Id), ERROR, WS, _("newline"));
		}

		[Test]
		public void Regressions()
		{
			Case(@"Foo@[@ @ ?!]", A(TT.Id, TT.At, TT.LBrack, TT.At, TT.At, TT.NormalOp, TT.RBrack), 
				_("Foo"), _(""), null, _(""), _(""), _("'?!"), null);
		}

		void Case(string input, TokenType[] tokenTypes, params object[] values)
		{
			Debug.Assert(values.Length <= tokenTypes.Length);
			
			bool error = false;
			var lexer = new Les2Lexer(input, new MessageSinkFromDelegate((type, ctx, msg, args) => {
				TraceMessageSink.Value.Write(type, ctx, msg, args); error = true;
			}));

			int index = 0;
			for (int i = 0; i < tokenTypes.Length; i++)
			{
				error = false;
				Token token = lexer.NextToken().Value;
				Assert.LessOrEqual(index, token.StartIndex);
				Assert.AreEqual(tokenTypes[i], token.Type());
				if (i < values.Length) {
					Assert.AreEqual(values[i] == (object)ERROR, error);
					if (!error) {
						if (values[i] is Pair<string, Symbol>)
							Assert.AreEqual(values[i], L(token.TextValue(lexer).ToString(), token.TypeMarker?.Name));
						else
							Assert.AreEqual(values[i], token.Value);
					}
				}
				index = token.EndIndex;
			}
			var nothing = lexer.NextToken();
			Assert.That(!nothing.HasValue);
		}
	}
}
