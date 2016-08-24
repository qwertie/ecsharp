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
	public class LesLexerTests
	{
		[DebuggerStepThrough] static Symbol _(string s) { return GSymbol.Get(s); }
		[DebuggerStepThrough] static T[] A<T>(params T[] list) { return list; }

		static readonly object WS = WhitespaceTag.Value;

		[Test]
		public void Basics()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Id, TT.Not), 
				_("hello"), _("',"), _("world"), _("'!"));
			Case(@"this is""just""1 lexer test '!'",
				A(TT.Id, TT.Id, TT.Literal, TT.Literal, TT.Id, TT.Id, TT.Literal),
				_("this"), _("is"), "just", 1, _("lexer"), _("test"), '!');
			Case(@"12:30", A(TT.Literal, TT.NormalOp, TT.Literal), 12, _("':"), 30);
			Case(@"c+='0'", A(TT.Id, TT.Assignment, TT.Literal), _("c"), _("'+="), '0');
			Case("// hello\n\r\n\r/* world */",
				A(TT.SLComment, TT.Newline, TT.Newline, TT.Newline, TT.MLComment));
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen), null, null, null, null, null, null);
			Case(@"finally@@{`boom!` ;}", A(TT.Id, TT.At, TT.At, TT.LBrace, TT.BQOperator, TT.Semicolon, TT.RBrace),
				_("finally"), _(""), _(""), null, _("boom!"), _("';"), null);
			Case(@"a""b""", A(TT.Id, TT.Literal), _("a"), "b");
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
			Case(@"`Testing`""Testing""'!'", A(TT.BQOperator, TT.Literal, TT.Literal), _("Testing"), "Testing", '!');
			Case(@"`\a\b\f\v\`\'\""`""\a\b\f\v\`\'\""""'\0'", A(TT.BQOperator, TT.Literal, TT.Literal),
				_("\a\b\f\v`\'\""), "\a\b\f\v`\'\"", '\0');
			// There are no C#-style 'verbatim' strings in LES, use triple-quoted strings instead.
			Case(@"#""\n"" @""\\""", A(TT.Id, TT.Literal, TT.At, TT.Literal), _("#"), "\n", _(""), "\\");
		}

		[Test]
		public void TestTQStrings()
		{
			Case("\"\"\"Hello'''', quotes!\"\"\".", A(TT.Literal, TT.Dot), "Hello'''', quotes!", _("'."));
			Case("'''Hello\"\"\"\", quotes!'''.", A(TT.Literal, TT.Dot), "Hello\"\"\"\", quotes!", _("'."));
			Case("'''Hello\"\"\"\", quotes!'''.", A(TT.Literal, TT.Dot), "Hello\"\"\"\", quotes!", _("'."));
			
			// Triple-quoted strings always use \n as the line separator, 
			// and ignore indentation insofar as it matches the first line.
			Case("  '''One\n  Two'''",    A(TT.Literal), "One\nTwo");
			Case("  '''One\r  Two'''",    A(TT.Literal), "One\nTwo");
			Case("  '''One\r\n  Two'''",  A(TT.Literal), "One\nTwo");
			Case("\t\t'''One\n\t\tTwo'''",A(TT.Literal), "One\nTwo");
			Case(" '''One\r  Two'''",     A(TT.Literal), "One\n Two");
			Case("\t '''One\r\tTwo'''",   A(TT.Literal), "One\nTwo");
			Case("  '''One\r\tTwo'''",    A(TT.Literal), "One\n\tTwo");
			Case(" \t '''One\r \t  \tTwo'''", A(TT.Literal), "One\n \tTwo");
			Case("  '''One\nTwo\n   Three'''", A(TT.Literal), "One\nTwo\n Three");
			
			// Triple-quoted strings also support escape sequences: \\\, \\n, \\r, \\", \\'
			Case(@"'''Three quotes: ''\'/!'''", A(TT.Literal), "Three quotes: '''!");
			Case(@"'''Escapes: \r/\n/, \\/, \""/, \0/ and \'/'''.", A(TT.Literal, TT.Dot), "Escapes: \r\n, \\, \", \0 and '", _("'."));
			Case(@"'''Escaped: \\/r/\\/n/, \\/\/, \\/""/, and \\/'/'''.", A(TT.Literal, TT.Dot), @"Escaped: \r/\n/, \\/, \""/, and \'/", _("'."));
			Case(@"'''Unrecognized escapes: \//\o/'''", A(TT.Literal), @"Unrecognized escapes: \//\o/");
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
				1, 1.0f, 1d, 0x1, 1);
			Case("1L2UL3u4f5d6m", A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				1L, 2UL, 3u, 4f, 5d, 6m);
			Case("@true @false @null @void", 
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				true, false, null, @void.Value);
			Case("@@symbol \"string\"", A(TT.Literal, TT.Literal), GSymbol.Get("symbol"), "string");
			Case(@"'\'''!'", A(TT.Literal, TT.Literal), '\'', '!');
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
			Case(@"$~!%^&*-+=|<>_/?:.", A(TT.PrefixOp, TT.Id, TT.NormalOp), _("'$~!%^&*-+=|<>"), _("_"), _("'/?:."));
			Case(@"@~!%^&*-+=|<>@@/?:.$", A(TT.Id, TT.Literal), _("~!%^&*-+=|<>"), _("/?:.$"));
			Case(@"@,!;: :^=", A(TT.At, TT.Comma, TT.Not, TT.Semicolon, TT.NormalOp, TT.Assignment), _(""), _("',"), _("'!"), _("';"), _("':"), _(@"':^="));
		}

		[Test]
		public void TestIntegers()
		{
			Case("9", A(TT.Literal), 9);
			Case("1337", A(TT.Literal), 1337);
			Case("-1", A(TT.Literal), -1);
			Case("9111222U", A(TT.Literal), 9111222U);
			Case("0L", A(TT.Literal), 0L);
			Case("-9111222L", A(TT.Literal), -9111222L);
			Case("-1U", A(TT.NormalOp, TT.Literal), _("'-"), 1U);
			Case("9_111_222", A(TT.Literal), 9111222);
			Case("9_111_222_333", A(TT.Literal), 9111222333);
			Case("4_111_222_333", A(TT.Literal), 4111222333);
			Case("4_111_222_333U", A(TT.Literal), 4111222333U);
			Case("9_111_222_333_444_555", A(TT.Literal), 9111222333444555);
			Case("9_111_222_333_444_555L", A(TT.Literal), 9111222333444555L);
			Case("9_111_222_333_444_555UL", A(TT.Literal), 9111222333444555UL);
			Case("0x9+0x0A=0x0000_0000_13", A(TT.Literal, TT.NormalOp, TT.Literal, TT.Assignment, TT.Literal), 0x9, _("'+"), 0x0A, _("'="), 0x13);
			Case("0b1000_0000_1000_0001_1111_1111==0x8081FF", A(TT.Literal, TT.NormalOp, TT.Literal), 0x8081FF, _("'=="), 0x8081FF);
			Case("0b11L0b10000000_10000001_10010010_11111111U", A(TT.Literal, TT.Literal), 3L, 0x808192FFU);
			Case("0b1111_10000000_10000001_10010010_11111111", A(TT.Literal), 0x0F808192FF);
			Case("11Z", A(TT.Literal), new BigInteger(11));
			Case("9_111_222_333_444_555Z", A(TT.Literal), new BigInteger(9111222333444555UL));
			Case("9999111222333444555000Z", A(TT.Literal), 1000 * new BigInteger(9999111222333444555UL));
			Case("9999111222333444555000", A(TT.Literal), 1000 * new BigInteger(9999111222333444555UL));
			Case("0x1_FFFF_FFFF_0000_0000", A(TT.Literal), BigInteger.Parse("1FFFFFFFF00000000", System.Globalization.NumberStyles.HexNumber));
			Case("-9111222Z", A(TT.Literal), new BigInteger(-9111222L));
			Case("-9999111222333444555000Z", A(TT.Literal), -1000 * new BigInteger(9999111222333444555UL));
			Case("-9999111222333444555000", A(TT.Literal), -1000 * new BigInteger(9999111222333444555UL));
			Case("-18446744069414584320", A(TT.Literal), BigInteger.Parse("-18446744069414584320"));
		}

		[Test]
		public void TestFloats()
		{
			Case("0.0", A(TT.Literal), 0.0);
			Case("0.1", A(TT.Literal), 0.1);
            Case("25d25f25m", A(TT.Literal, TT.Literal, TT.Literal), 25d,25f,25m);
			Case("0.25d", A(TT.Literal), 0.25d);
			Case("0.25f", A(TT.Literal), 0.25f);
			Case("0.25m", A(TT.Literal), 0.25m);
			Case("0.25e2", A(TT.Literal), 0.25e2);
			Case("10e-20", A(TT.Literal), 10e-20);
			Case("0.3e+2d", A(TT.Literal), 0.3e+2d);
			Case("0.3e+2f", A(TT.Literal), 0.3e+2f);
			Case("0.3e+2m", A(TT.Literal), 0.3e+2m);
			Case("1234567890123456789012345678901234567890d", A(TT.Literal), 1234567890123456789012345678901234567890d);
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(TT.Literal), 
			      123456789012345678901234567890.1234567890123456789012345678901234567890f);
			Case(".5e+2.5e+2f.5m", A(TT.Literal, TT.Literal, TT.Literal), .5e+2, .5e+2f, .5m);
			Case("Y.5", A(TT.Id, TT.Literal), _("Y"), .5);
			Case("0.1.5", A(TT.Literal, TT.Literal), 0.1, .5);
			Case("5.ToString", A(TT.Literal, TT.Dot, TT.Id), 5, _("'."), _("ToString"));
		}
		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.0p1234", A(TT.Literal), 0.0);
			Case("0x0.0", A(TT.Literal), 0.0);
			Case("0xF.8", A(TT.Literal), 15.5);
			Case("0xF.8p+1;0xF.8p1", A(TT.Literal, TT.Semicolon, TT.Literal), 31.0, _("';"), 31.0);
			Case("0xA.8p-1", A(TT.Literal), 5.25);
			Case("0b101.01", A(TT.Literal), 5.25);
			Case("0b101.01p0f", A(TT.Literal), 5.25f);
			Case("0b101.01p2", A(TT.Literal), 21.0);
			Case("0b1111_1111.1111_1111p+8", A(TT.Literal), (double)0xFFFF);
			Case("0b.1p-2", A(TT.Literal), 0.125);
			Case("0b.1p-2f", A(TT.Literal), 0.125f);
		}

		[Test]
		public void TestSymbols()
		{
			Case(@"@@public@@is@@A@@`common\\word`@@around@@here",
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				_("public"), _("is"), _("A"), _(@"common\word"), _("around"), _("here"));
			Case(@"@@+-*/", A(TT.Literal), _("+-*/"));
			Case(@"@@+- //x", A(TT.Literal, TT.SLComment), _("+-"), WS);
			Case(@"@@+-//x", A(TT.Literal), _("+-//x"));
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
			                        ERROR, WS, 'o', ERROR, WS, _("'?"), ERROR);
			Case("'abc'",           A(TT.Literal), ERROR);
			Case("0x!0b",           A(TT.Literal, TT.Not, TT.Literal), ERROR, _("'!"), ERROR);
			Case("`weird\nnewline", A(TT.BQOperator, TT.Newline, TT.Id), ERROR, WS, _("newline"));
			Case("0xFF_0000_0000U", A(TT.Literal), ERROR);
			Case("0xFFFF_FFFF_0000_0000L", A(TT.Literal), ERROR);
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
			var lexer = new LesLexer(input, new MessageSinkFromDelegate((type, ctx, msg, args) => {
				MessageSink.Trace.Write(type, ctx, msg, args); error = true;
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
					if (!error)
						Assert.AreEqual(values[i], token.Value);
				}
				index = token.EndIndex;
			}
			var nothing = lexer.NextToken();
			Assert.That(!nothing.HasValue);
		}
	}
}
