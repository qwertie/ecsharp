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
	using System.Numerics;
	using S = Loyc.Syntax.CodeSymbols;
	using TT = TokenType;

	[TestFixture]
	public class Les3LexerTests
	{
		[DebuggerStepThrough] static Symbol _(string s) { return (Symbol)s; }
		[DebuggerStepThrough] static T[] A<T>(params T[] list) { return list; }

		static readonly object WS = WhitespaceTag.Value;

		[Test]
		public void Comments()
		{
			Case("// hello",
				A(TT.SLComment), WS);
			Case("/* hello *//* I mean, \r\n hello world! */",
				A(TT.MLComment, TT.MLComment), WS, WS);
			Case("// hello\n\r\n\r/* world */",
				A(TT.SLComment, TT.Newline, TT.Newline, TT.Newline, TT.MLComment), WS, null, null, null, WS);
			Case("// C:\\hello\\\\\n/* world */",
				A(TT.SLComment, TT.Newline, TT.MLComment), WS, null, WS);
			Case("// C:\\hello\\\\/* world */",
				A(TT.SLComment, TT.MLComment), WS, WS);
			Case("/* hello */**/",
				A(TT.MLComment, TT.NormalOp), WS, _("'**/"));
		}

		[Test]
		public void TestIdentifiers()
		{
			Case("abc_123/_0", A(TT.Id, TT.NormalOp, TT.Id), _("abc_123"), _("'/"), _("_0"));
			Case("is`is`", A(TT.Id, TT.BQId), _("is"), _("is"));
			Case("`backquoted`x", A(TT.BQId, TT.Id), _("backquoted"), _("x"));
			Case("No#error", A(TT.Id), _("No#error"));
			Case("`#food:``yum`", A(TT.BQId, TT.BQId), _("#food:"), _("yum"));
			Case(@"`/*``$``==>``??.`", A(TT.BQId, TT.BQId, TT.BQId, TT.BQId), _(@"/*"), _("$"), _("==>"), _("??."));
			Case(@"`0``\`\n`", A(TT.BQId, TT.BQId), _("0"), _("`\n"));
			Case("isn't prime'", A(TT.Id, TT.Id), _("isn't"), _("prime'"));
			Case(@" `\a\b\f\v\`\'\""`", A(TT.BQId), _("\a\b\f\v`\'\""));
		}

		[Test]
		public void TestNewlines()
		{
			// Newline counts as whitespace only inside parens and square brackets
			Case("\nfoo\n", A(TT.Newline, TT.Id, TT.Newline), null, _("foo"), null);
			Case("{\nfoo\n}", A(TT.LBrace, TT.Newline, TT.Id, TT.Newline, TT.RBrace), null, null, _("foo"), null, null);
			Case("(\nfoo\n)", A(TT.LParen, TT.Newline, TT.Id, TT.Newline, TT.RParen), null, WS, _("foo"), WS, null);
			Case("[\nfoo\n]", A(TT.LBrack, TT.Newline, TT.Id, TT.Newline, TT.RBrack), null, WS, _("foo"), WS, null);
			Case("(\n{\n}\n)", A(TT.LParen, TT.Newline, TT.LBrace, TT.Newline, TT.RBrace, TT.Newline, TT.RParen), null, WS,  null, null, null, WS, null);
		}

		[Test]
		public void TrueFalseNullAndSimilarIds()
		{
			// true, false and null: the only keywords so far
			Case("`foo`", A(TT.BQId), _("foo"));
			Case("null", A(TT.Literal), new object[] { null });
			Case("`null`", A(TT.BQId), _("null"));
			Case("true false", A(TT.Literal, TT.Literal), true, false);
			Case("`true``false`", A(TT.BQId, TT.BQId), _("true"), _("false"));
		}

		[Test]
		public void TestSQOperators()
		{
			var sqOperator = TT.PreOrSufOp;
			Case("x'y", A(TT.Id), _("x'y"));
			Case("x 'y", A(TT.Id, sqOperator), _("x"), _("'y"));
			Case("x 'abcABC123", A(TT.Id, sqOperator, TT.Literal), _("x"), _("'abcABC"), "123");
			Case("'>s 0", A(TT.SingleQuote, TT.NormalOp, TT.Id, TT.Literal), _("'"), _("'>"), _("s"), "0");
			Case("'s+0", A(sqOperator, TT.NormalOp, TT.Literal), _("'s"), _("'+"), "0");
			Case(@"'{ } '", A(TT.SingleQuote, TT.LBrace, TT.RBrace, TT.SingleQuote),
				_("'"), null, null, _("'"));
			Case(@"'hello++;'GOODBYE", A(sqOperator, TT.PreOrSufOp, TT.Semicolon, sqOperator),
				_("'hello"), _("'++"), _("';"), _("'GOODBYE"));
		}

		[Test]
		public void TestNormalStrings()
		{
			Case("'X'", A(TT.Literal), "X");
			Case("'X''!'", A(TT.Literal, TT.Literal), "X", "!");
			Case(@"""Testing""'!'", A(TT.Literal, TT.Literal), "Testing", "!");
			Case(@" ""\a\b\f\v\`\'\""""'\0'", A(TT.Literal, TT.Literal), "\a\b\f\v`\'\"", "\0");
			// There are no C#-style 'verbatim' strings in LES, use triple-quoted strings instead.
			Case(@"""\n"" @""\\""", A(TT.Literal, TT.At, TT.Literal), "\n", _("'@"), "\\");
			Case(@"""""", A(TT.Literal), "");

			// Pile of poo is \U1F4A9 - astral escape sequences are tested separately
			Case(@"'💩'", A(TT.Literal), "💩");
			Case(@"""💩""", A(TT.Literal), "💩");
		}

		[Test]
		public void TestTQStrings()
		{
			Case("\"\"\"Hello'''', quotes!\"\"\".", A(TT.Literal, TT.Dot), "Hello'''', quotes!", _("'."));
			Case("'''Hello\"\"\"\", quotes!'''.", A(TT.Literal, TT.Dot), "Hello\"\"\"\", quotes!", _("'."));
			Case("'''Hello\"\"\"\", quotes!'''.", A(TT.Literal, TT.Dot), "Hello\"\"\"\", quotes!", _("'."));

			// Triple-quoted strings always use \n as the line separator, 
			// and ignore indentation insofar as it matches the first line.
			Case("  '''One\n  Two'''", A(TT.Literal), "One\nTwo");
			Case("  '''One\r  Two'''", A(TT.Literal), "One\nTwo");
			Case("  '''One\r\n  Two'''", A(TT.Literal), "One\nTwo");
			Case("\t\t'''One\n\t\tTwo'''", A(TT.Literal), "One\nTwo");
			Case("\t\t'''One\n\t\t\tTwo'''", A(TT.Literal), "One\nTwo");
			Case("\t\t'''One\n\t\t\t\tTwo'''", A(TT.Literal), "One\n\tTwo");
			Case("\n\t\t'''One\n\t\t\t\tTwo'''", A(TT.Newline, TT.Literal), null, "One\n\tTwo");
			Case(" '''One\r     Two'''", A(TT.Literal), "One\n Two");
			Case("\t '''One\r\tTwo'''", A(TT.Literal), "One\nTwo");
			Case("  '''One\r\tTwo'''", A(TT.Literal), "One\n\tTwo");
			Case(" \t '''One\r \t  \tTwo'''", A(TT.Literal), "One\n\tTwo");
			Case("  '''One\nTwo\n      Three'''", A(TT.Literal), "One\nTwo\n Three");

			// Triple-quoted strings also support limited escape sequences: \\\, \\n, \\r, \\", \\'
			Case(@"'''Three quotes: ''\'/!'''", A(TT.Literal), "Three quotes: '''!");
			Case(@"'''Escapes: \r/\n/, \\/, \""/, \0/ and \'/'''.", A(TT.Literal, TT.Dot), "Escapes: \r\n, \\, \", \0 and '", _("'."));
			Case(@"'''Escaped: \\/r/\\/n/, \\/\/, \\/""/, and \\/'/'''.", A(TT.Literal, TT.Dot), @"Escaped: \r/\n/, \\/, \""/, and \'/", _("'."));
			Case(@"'''Unrecognized escapes: \x/\//\o/'''", A(TT.Literal), @"Unrecognized escapes: \x/\//\o/");

			// Ensure astral chars work in normal and triple quotes
			Case("\"\uD83D\uDCA9\"", A(TT.Literal), "\uD83D\uDCA9");
			Case("'''\uD83D\uDCA9'''", A(TT.Literal), "\uD83D\uDCA9");
		}

		[Test]
		public void TestSpecialStrings()
		{
			Case("foo\"bar\"", A(TT.Literal),         L("bar", "foo"));
			Case("`foo`\"bar\"", A(TT.Literal),       L("bar", "foo"));
			Case("foo\"\"\"bar\"\"\"", A(TT.Literal), L("bar", "foo"));
			Case("foo'''bar'''", A(TT.Literal),       L("bar", "foo"));
			Case(@"`foo\n`'''bar'''", A(TT.Literal),  L("bar", "foo\n"));
			Case("foo '''bar'''", A(TT.Id, TT.Literal), (Symbol)"foo", "bar");
		}

		[Test]
		public void TestHexEscapeSequences()
		{
			// \U escape sequences have variable length
			Case(@"'\UA' '\U020'", A(TT.Literal, TT.Literal), "\n", " ");
			Case(@"'\U1F4A9'", A(TT.Literal), "💩");
			Case(@"'\U01F4A9'", A(TT.Literal), "💩");
			Case(@"""\u0020\U00020\x20""", A(TT.Literal), "   ");
			Case(@"'''\u0020/\U00020/\x20/'''", A(TT.Literal), @"\u0020/\U00020/\x20/"); // Not supported in TQ strings
			Case(@"""\U0020\u00200\x20""", A(TT.Literal), "  0 ");
			Case(@"""\u0009\u00009\x09""", A(TT.Literal), "\t\09\t");
			Case(@"""\u2202\U02202\x2202""", A(TT.Literal), "\u2202\u2202\"02"); // • Bullet
			Case(@"""\U1F4A9\U10FFFF""", A(TT.Literal), "\uD83D\uDCA9\uDBFF\uDFFF"); // 💩 pile of poo and highest code point
			Case(@"""\u1F4A9\u10FFFF""", A(TT.Literal), "\u1F4A9\u10FFFF"); // only 4 digits are part of each escape sequence
			Case(@"""\U1F4A90""", A(TT.Literal), new Error("\uD83D\uDCA9"+"0")); // This escape is not really 6 digits
			Case(@"""\U110000""", A(TT.Literal), new Error("\uD804\uDC00"+"0")); // The greatest escape U+10FFFF plus 1
		}

		[Test]
		public void TestUtf8EscapeCharacterInterpretation()
		{
			// In LES, escapes are interpreted as if the string is a byte string with 
			// arbitrary bytes that may or may not be valid UTF-8. These bytes are 
			// interpreted in such a way that they can round-trip back to the 
			// same bytes, with invalid UTF-8 transliterated to invalid UTF-16. See 
			// here:
			// https://github.com/sunfishcode/design/pull/3#issuecomment-236777361
			Case(@"""\x80\xAA""",     A(TT.Literal), "\uDC80\uDCAA");       // Bytes that are invalid as UTF-8
			Case(@"""\xc1\x8b\x00""", A(TT.Literal), "\uDCC1\uDC8B\0");     // overlong "K" and null character

			// Valid UTF-8 sequences
			Case(@"""\xC3\xA9""",     A(TT.Literal), "é");                  // é U+00E9 expressed in UTF-8 (2 bytes)
			Case(@"""\xE2\x82\xAC""", A(TT.Literal), "\u20AC");             // € U+20AC expressed in UTF-8 (3 bytes)
			Case(@"""\xEF\xBB\xBF""", A(TT.Literal), "\uFEFF");             // Byte order mark U+FEFF (0b1111_111011_111111) expressed in UTF-8
			Case(@"""\xF0\x9F\x92\xA9""", A(TT.Literal), "\uD83D\uDCA9");   // 💩 pile of poo U+1F4A9 (0b00_011111_010010_101001) in UTF-8 (4 bytes)

			// Low surrogate U+DCFF (0b1101_110011_111111) collides with the 
			// representation of the single byte \xFF and must be expressed as 
			// three UTF8 bytes transliterated to invalid UTF16. This is true
			// whether we write it as \xED\xB3\xBF or \uDCFF. Meanwhile, \xFF
			// becomes \uDCFF in UTF16.
			Case(@"""\xED\xB3\xBF""", A(TT.Literal), "\uDCED\uDCB3\uDCBF");
			Case(@"""\uDCFF""",       A(TT.Literal), "\uDCED\uDCB3\uDCBF");
			Case(@"""\xFF""",         A(TT.Literal), "\uDCFF");
			
			// This is the UTF-8 encoding of \uD83D\uDCA9 (the UTF16 of poo 💩)
			// The lexer must NOT produce \uD83D\uDCA9 as output, since it 
			// would not round-trip faithfully back to bytes. Instead, the second
			// surrogate is transliterated to 3 invalid UTF16 low surrogates.
			Case(@"""\xED\xA0\xBD\xED\xB2\xA9""", A(TT.Literal), "\uD83D\uDCED\uDCB2\uDCA9");
			
			// All low surrogates are coded as invalid UTF8 converted to UTF16
			Case(@"""\uDC00\uDFFF""", A(TT.Literal), "\uDCED\uDCB0\uDC80\uDCED\uDCBF\uDCBF");
		}

		[Test]
		public void TestShebang()
		{
			Case("#!/bin/sh\r\n// that's called a shebang!",
				A(TT.Shebang, TT.SLComment));
			//Case("+#!/bin",
			//	A(TT.NormalOp, TT.Unknown, TT.NormalOp, TT.Id),
			//	_("'+"), null, _("'!/"), _("bin"));
		}

		[Test]
		public void TestOperators()
		{
			Case(@"%", A(TT.NormalOp), _(@"'%"));
			//Case(@"\++ ++ ++\", A(TT.PreOrSufOp, TT.PreOrSufOp, TT.SuffixOp), _("++"), _("++"), _(@"++\"));
			//Case(@"\solve-for-x x\squared\", A(TT.NormalOp, TT.Id, TT.SuffixOp), _("solve-for-x"), _("x"), _(@"squared\"));
			Case(@"+++x---", A(TT.PreOrSufOp, TT.Id, TT.PreOrSufOp), _("'+++"), _("x"), _("'---"));
			Case(@"!!x!?!", A(TT.PreOrSufOp, TT.Id, TT.PreOrSufOp), _("'!!"), _("x"), _("'!?!"));
			Case(@"x!!.y", A(TT.Id, TT.Dot, TT.Id), _("x"), _("'!!."), _("y"));
			//Case(@"$x\y\`bq`\", A(TT.PrefixOp, TT.Id, TT.SuffixOp, TT.BQString, TT.NormalOp), _("$"), _("x"), _(@"y\"), _("bq"), _(@"\"));
			Case(@"$~!%^&*-+=|<>/?:._", A(TT.PrefixOp, TT.Id), _("'$~!%^&*-+=|<>/?:."), _("_"));
			Case(@"$~!%^&*-+=|<>_/?:.", A(TT.PrefixOp, TT.Id, TT.Dot), _("'$~!%^&*-+=|<>"), _("_"), _("'/?:."));
			Case(@"@,!;: :^=", A(TT.At, TT.Comma, TT.Not, TT.Semicolon, TT.Colon, TT.Assignment), _("'@"), _("',"), _("'!"), _("';"), _("':"), _(@"':^="));
		}

		[Test]
		public void TestIntegers()
		{
			// Number parsing has moved to the parser, so these tests are less important than they were
			// TODO: test more on parser side
			Case("9", A(TT.Literal), "9");
			Case("1337", A(TT.Literal), "1337");
			Case("-1", A(TT.NormalOp, TT.Literal), S.Sub, "1");
			Case("−1", A(TT.Literal), "−1");
			Case("_\"-1\"", A(TT.Literal), "-1");
			Case("9111222U", A(TT.Literal), "9111222");
			Case("0x8000_0000", A(TT.Literal), "0x8000_0000");
			Case("0L e 0x8000000000000000", A(TT.Literal, TT.Id, TT.Literal), L("0", "_L"), _("e"), "0x8000000000000000");
			Case("_L\"-9111222\"", A(TT.Literal), "-9111222");
			Case("_L\"−9222333\"", A(TT.Literal), "−9222333");
			Case("_U\"-1\"", A(TT.Literal), L("-1", "_U")); // the error here is no longer detected
			Case("9_111_222",   A(TT.Literal), "9_111_222");
			//Case("9_111_222_333", A(TT.Literal), 9111222333);
			//Case("4_111_222_333", A(TT.Literal), 4111222333);
			//Case("4_111_222_333U", A(TT.Literal), 4111222333U);
			//Case("9_111_222_333_444_555", A(TT.Literal), 9111222333444555);
			//Case("9_111_222_333_444_555L", A(TT.Literal), 9111222333444555L);
			//Case("9_111_222_333_444_555UL", A(TT.Literal), 9111222333444555UL);
			Case("0x9+0x0A=0x0000_0000_13", A(TT.Literal, TT.NormalOp, TT.Literal, TT.Assignment, TT.Literal), "0x9", _("'+"), "0x0A", _("'="), "0x0000_0000_13");
			Case("0b1000_0000_1000_0001_1111_1111==0x8081FF", A(TT.Literal, TT.NormalOp, TT.Literal), "0b1000_0000_1000_0001_1111_1111", _("'=="), "0x8081FF");
			Case("0b11L 0b10000000_10000001_10010010_11111111U", A(TT.Literal, TT.Literal), "0b11", "0b10000000_10000001_10010010_11111111");
			Case("0b1111_10000000_10000001_10010010_11111111", A(TT.Literal), "0b1111_10000000_10000001_10010010_11111111");
			Case("0xFF_0000_0000U", A(TT.Literal), "0xFF_0000_0000");
		}

		[Test]
		public void TestBigInts()
		{
			// Number parsing has moved to the parser, so these tests are less important than they were
			Case("11z\n                  ",  A(TT.Literal, TT.Newline), "11", null);
			Case("9_111_222_333_444_555z ",  A(TT.Literal), L("9_111_222_333_444_555", "_z"));
			Case("9999111222333444555000z",  A(TT.Literal), L("9999111222333444555000", "_z"));
			Case("9999111222333444555000",   A(TT.Literal), L("9999111222333444555000", "_"));
			Case("0x1_FFFF_FFFF_0000_0000",  A(TT.Literal), L("0x1_FFFF_FFFF_0000_0000", "_"));
			//Case("_z\"-1\"",                      A(TT.Literal), new BigInteger(-1));
			//Case("_z\"-9999111222333444555000\"", A(TT.Literal), -1000 * new BigInteger(9999111222333444555L));
			//Case("-18_446_744_069_414_584_320",   A(TT.NormalOp, TT.Literal), S.Sub, 18446744069414584320uL);
			//Case("_\"-18_446_744_069_414_584_320\"", A(TT.Literal), BigInteger.Parse("-18446744069414584320"));
			//Case("-9111222z             \n", A(TT.NormalOp, TT.Literal, TT.Newline), S.Sub, new BigInteger(9111222L), null);
			//Case("_\"-9999111222333444555000\"", A(TT.Literal), -1000 * new BigInteger(9999111222333444555UL));
			//Case("123456789012345678901234567890", A(TT.Literal), BigInteger.Parse("123456789012345678901234567890"));
		}

		[Test]
		public void TestFloats()
		{
			// Number parsing has moved to the parser, so these tests are less important than they were
			Case("0.0", A(TT.Literal), "0.0");
			Case("0.1", A(TT.Literal), L("0.1", "_"));
			Case("25d 25f 25m", A(TT.Literal, TT.Literal, TT.Literal), L("25", "_d"), L("25", "_f"), L("25", "_m"));
			Case("0.25d", A(TT.Literal), "0.25");
			Case("0.25e2", A(TT.Literal), "0.25e2");
			Case("10e-20", A(TT.Literal), "10e-20");
			Case("0.3e+2d", A(TT.Literal), "0.3e+2");
			Case("0.3e+2f", A(TT.Literal), "0.3e+2");
			Case("0.3e+2m", A(TT.Literal), "0.3e+2");
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(TT.Literal),
				 L("123456789012345678901234567890.1234567890123456789012345678901234567890", "_f"));
			Case(".5e+2.5e+2f.5m", A(TT.Literal, TT.Literal, TT.Literal), ".5e+2", ".5e+2", ".5");
			Case("Y.5", A(TT.Id, TT.Literal), _("Y"), ".5");
			Case("0.1.5", A(TT.Literal, TT.Literal), "0.1", ".5");
			Case("5.ToString", A(TT.Literal, TT.Dot, TT.Id), "5", _("'."), _("ToString"));
		}

		[Test]
		public void TestOperatorFloatAmbiguity()
		{
			Case("x..5", A(TT.Id, TT.NormalOp, TT.Literal), _("x"), _("'.."), "5");
			Case("x*.5", A(TT.Id, TT.NormalOp, TT.Literal), _("x"), _("'*"), ".5");
			Case("x...5", A(TT.Id, TT.NormalOp, TT.Literal), _("x"), _("'..."), "5");
			Case("x.*.5", A(TT.Id, TT.NormalOp, TT.Literal), _("x"), _("'.*"), ".5");
		}

		public void TestFloats2()
		{
			Case("9876543210987654321098765432109876543210d", A(TT.Literal), 9876543210987654321098765432109876543210d);
		}

		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.0p1234", A(TT.Literal), L("0x0.0p1234", "_"));
			Case("0x0.0", A(TT.Literal), "0x0.0");
			Case("0xF.8", A(TT.Literal), "0xF.8");
			Case("0xF.8p+1;0xF.8p1", A(TT.Literal, TT.Semicolon, TT.Literal), "0xF.8p+1", _("';"), "0xF.8p1");
			Case("0xA.8p-1", A(TT.Literal), "0xA.8p-1");
			Case("0x123.88p0", A(TT.Literal), "0x123.88p0");
			Case("0x123.A8p0", A(TT.Literal), "0x123.A8p0");
			Case("0x123.8p-10", A(TT.Literal), "0x123.8p-10");
			Case("0b101.01", A(TT.Literal), "0b101.01");
			Case("0b101.01p0f", A(TT.Literal), L("0b101.01p0", "_f"));
			Case("0b101.01p2", A(TT.Literal), "0b101.01p2");
			Case("0b1111_1111.1111_1111p+8", A(TT.Literal), "0b1111_1111.1111_1111p+8");
			Case("0b.1p-2", A(TT.Literal), "0b.1p-2");
			Case("0b.1p-2f", A(TT.Literal), "0b.1p-2");
			Case(" 123u32 123u64", A(TT.Literal, TT.Literal), L("123", "_u32"), L("123", "_u64"));
			Case("−123i32−123i64", A(TT.Literal, TT.Literal), L("−123", "_i32"), L("−123", "_i64"));
			Case("0x123u32 0x123u64", A(TT.Literal, TT.Literal), L("0x123", "_u32"), L("0x123", "_u64"));
			Case("0.0f32   0xF.8p0f64", A(TT.Literal, TT.Literal), L("0.0", "_f32"), L("0xF.8p0", "_f64"));
		}

		Pair<string, Symbol> L(string v, string tm) { return Pair.Create(v, (Symbol) tm); }

		[Test]
		public void TestUnifiedLiteralPipeline()
		{
			// Number parsing has moved to the parser, so really we're just testing the same stuff again
			Case(@"-123 0b1111", A(TT.NormalOp, TT.Literal, TT.Literal), S.Sub, L("123", "_"), L("0b1111", "_"));
			Case(@"_""-123"" _""0b1111""", A(TT.Literal, TT.Literal), L("-123", "_"), L("0b1111", "_"));
			//Case(@"_""123"" _""0x123""", A(TT.Literal, TT.Literal), 123, 0x123);
			//Case(@"_""123.0"" _""0x123.4p4""", A(TT.Literal, TT.Literal), 123.0, (double)0x1234);
			//Case(@"_""-123d"" _"" 123 """, A(TT.Literal, TT.Literal), new Error(L("-123d", "_")), new Error(L(" 123 ", "_")));
			//Case(@"_f64""123"" _f32""-1.25e2""", A(TT.Literal, TT.Literal), 123d, -125f);
			Case(@"`_uL`""9876543210"" `_u`""-123""", A(TT.Literal, TT.Literal), L("9876543210", "_uL"), L("-123", "_u"));
			Case(@"s""""", A(TT.Literal), L("", "s"));
			Case(@"s""Hello!"" s""""", A(TT.Literal, TT.Literal), L("Hello!", "s"), L("", "s"));
			Case(@"123e456i32", A(TT.Literal), L("123e456", "_i32"));
			Case("``'''123'''  '''1234'''", A(TT.Literal, TT.Literal), L("123", ""), L("1234", ""));
			Case("123``",       A(TT.Literal, TT.BQId), "123", _(""));
			Case(@"_""\x31\x32\x33""", A(TT.Literal), L("123", "_"));
			Case("foo'''string'''", A(TT.Literal), L("string", "foo"));
		}

		[Test]
		public void TestCustomNumericLiterals()
		{
			Case("123kb", A(TT.Literal), L("123", "_kb"));
			Case("−123KB", A(TT.Literal), L("−123", "_KB"));
			Case("123foo", A(TT.Literal), L("123", "_foo"));
			Case("123poo", A(TT.Literal), L("123", "_poo"));
			Case("0xAzoo", A(TT.Literal), L("0xA", "_zoo"));
			Case("0xA.8p0float", A(TT.Literal), L("0xA.8p0", "_float"));
			Case("123over500", A(TT.Literal), L("123",   "_over500"));
			Case("123.0float", A(TT.Literal), L("123.0", "_float"));
			Case("123f123d", A(TT.Literal),   L("123",   "_f123d"));
		}

		[Test]
		public void TestAmbiguousHexLiteral()
		{
			Case("0x2.Equals", A(TT.Literal), L("0x2.E", "_quals"));
		}

		[Test]
		public void TestBackRefAndTreeDef()
		{
			Case(@"@@foo@@IS_a!common@@word@@around@@here",
				A(TT.BackRef, TT.BackRef, TT.Not, TT.Id, TT.BackRef, TT.BackRef, TT.BackRef),
				null, null, _("'!"), _("common"), null, null, null);
			Case(@"@.az09'@@az09'", A(TT.TreeDef, TT.BackRef), null, null);
		}

		[Test]
		public void OtherSpecialLiterals()
		{
			Case("true false null", A(TT.Literal, TT.Literal, TT.Literal), true, false, null);
			Case(@"`inf`_d""inf""", A(TT.BQId, TT.Literal), _("inf"), L("inf", "_d"));
			Case(@"`inf`_f""inf""", A(TT.BQId, TT.Literal), _("inf"), L("inf", "_f"));
			Case(@"_d""nan""", A(TT.Literal), L("nan", "_d"));
			Case(@"_f""nan""", A(TT.Literal), L("nan", "_f"));
			Case(@"_d""-inf"" _f""-inf""", A(TT.Literal, TT.Literal), L("-inf", "_d"), L("-inf", "_f"));
		}

		[Test]
		public void Misc()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Id, TT.Not),
				_("hello"), _("',"), _("world"), _("'!"));
			Case(@"this is ""just""1 lexer test '!'",
				A(TT.Id, TT.Id, TT.Literal, TT.Literal, TT.Id, TT.Id, TT.Literal),
				_("this"), _("is"), "just", "1", _("lexer"), _("test"), "!");
			Case(@"12:30", A(TT.Literal, TT.Colon, TT.Literal), "12", _("':"), "30");
			Case(@"c+='0'", A(TT.Id, TT.Assignment, TT.Literal), _("c"), _("'+="), "0");
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen), null, null, null, null, null, null);
			Case(@"a ""b""", A(TT.Id, TT.Literal), _("a"), "b");
			Case(@"'\'''!'", A(TT.Literal, TT.Literal), L("\'", "c"), L("!", "c"));
			Case(@"null `null`""""", A(TT.Literal, TT.Literal), null, L("", "null"));
			Case(@"null""""", A(TT.Literal), new object[] { L("", "null") });
			Case(@"false bool""true""", A(TT.Literal, TT.Literal), false, L("true", "bool"));
			Case(@"`true`""bool?"" `null`""null?""", A(TT.Literal, TT.Literal), L("bool?", "true"), L("null?", "null"));
			Case(@"true""nonbool"" null""nonnull""", A(TT.Literal, TT.Literal), L("nonbool", "true"), L("nonnull", "null"));
			Case(".if .If", A(TT.Dot, TT.Id, TT.Dot, TT.Id), _("'."), _("if"), _("'."), _("If"));
			Case("A.kw 5.kw", A(TT.Id, TT.Dot, TT.Id, TT.Literal, TT.Dot, TT.Id), _("A"), _("'."), _("kw"), "5", _("'."), _("kw"));
			Case("x'.abc.de", A(TT.Id, TT.Dot, TT.Id, TT.Dot, TT.Id), _("x'"), _("'."), _("abc"), _("'."), _("de"));
			Case(".kw'123!", A(TT.Dot, TT.Id, TT.Not), _("'."), _("kw'123"), _("'!"));
		}

		[Test]
		public void EofInToken()
		{
			Case(@"""", A(TT.Literal), new Error(""));
			Case(@"`", A(TT.BQId), new Error(_("")));
			//Case(@"\\", A(TT.SuffixOp), _(@"\"));
			Case(@"@", A(TT.At), _(@"'@"));
			Case(@"@@", A(TT.At, TT.At), _(@"'@"), _(@"'@"));
			Case(@"2.0e+", A(TT.Literal), new Error(L("2.0e+", "_")));
			// This interpretation would also be reasonable:
			//Case(@"2.0e+", A(TT.Literal, TT.NormalOp), CL("2.0", "e"), _("'+"));
		}

		[Test]
		public void TestErrors()
		{
			Case("\0", A(TT.Unknown), (object)null);
			Case("x=\"Hello\n", A(TT.Id, TT.Assignment, TT.Literal, TT.Newline), _("x"), _("'="), new Error("Hello"), null);
			Case("'\n'o'\"pq\n?", A(TT.SingleQuote, TT.Newline, TT.Literal, TT.Literal, TT.Newline, TT.NormalOp),
			                      _("'"), null, L("o", "c"), new Error("pq"), null, _("'?"));
			// No longer a lexer error
			Case("0x!0b", A(TT.Literal, TT.Not, TT.Literal), L("0x", "_"), _("'!"), L("0b", "_"));
			Case("0b102", A(TT.Literal), L("0b102", "_"));
			//Case("0xFF_0000_0000_0000_0000U", A(TT.Literal), new Error(L("0xFF_0000_0000_0000_0000", "_U")));
			//Case("0xFFFF_FFFF_0000_0000L", A(TT.Literal), new Error(L("0xFFFF_FFFF_0000_0000", "_L")));
			Case("(`weird\nnewline", A(TT.LParen, TT.BQId, TT.Newline, TT.Id), null, new Error(_("weird")), WS, _("newline"));
			Case(@"\()\", A(TT.Unknown, TT.LParen, TT.RParen, TT.Unknown), null, null, null, null);
			Case("'abc'", A(TT.PreOrSufOp, TT.SingleQuote), _("'abc"), _("'"));
			Case("'' ''", A(TT.SingleQuote, TT.Literal, TT.SingleQuote), _("'"), " ", _("'"));
			Case("x '", A(TT.Id, TT.SingleQuote), _("x"), _("'"));
			Case("x/*/",  A(TT.Id, TT.MLComment), _("x"), new Error(WS));
			Case("/*EOF", A(TT.MLComment), new Error(WS));
			Case("/*",    A(TT.MLComment), new Error(WS));
			Case("/*/",   A(TT.MLComment), new Error(WS));
		}

		[Test]
		public void TestDotIndents()
		{
			// A dot-indented line must start with a dot and each dot must be followed by a space.
			Case(". Hello", A(TT.Id), _("Hello"));
			Case(" .\n. ", A(TT.Dot, TT.Newline), _("'."), null);
			Case(".   .  . Hello", A(TT.Id), _("Hello"));
			Case(".\t.\t.\tHello\n.. Goodbye", A(TT.Id, TT.Newline, TT.NormalOp, TT.Id), _("Hello"), null, _("'.."), _("Goodbye"));
			Case(". .Hello",  A(TT.Dot, TT.Id), _("'."), _("Hello"));
			Case(". ..Hello", A(TT.NormalOp, TT.Id), _("'.."), _("Hello"));
		}

		[Test]
		public void Regressions()
		{
			Case(@"Foo@[@ @ ?!]", A(TT.Id, TT.At, TT.LBrack, TT.At, TT.At, TT.NormalOp, TT.RBrack), 
				_("Foo"), _("'@"), null, _("'@"), _("'@"), _("'?!"), null);
		}

		class Error
		{
			public readonly object Value;
			public Error(object value) { Value = value; }
		}

		void Case(UString input, TokenType[] tokenTypes, params object[] expected)
		{
			Debug.Assert(expected.Length <= tokenTypes.Length);
			
			bool error = false;
			var lexer = new Les3Lexer(input, "", new MessageSinkFromDelegate((type, ctx, msg, args) => {
				TraceMessageSink.Value.Write(type, ctx, msg, args); error = true;
			}));

			int index = 0;
			for (int i = 0; i < tokenTypes.Length; i++)
			{
				error = false;
				Token token = lexer.NextToken().Value;
				Assert.LessOrEqual(index, token.StartIndex);
				Assert.AreEqual(tokenTypes[i], token.Type());
				if (i < expected.Length) {
					var value = token.IsUninterpretedLiteral ? token.TextValue(lexer).ToString() : token.Value;
					var expected_i = expected[i];
					if (expected[i] is Error e) {
						Assert.IsTrue(error, "Expected error didn't occur in «{0}»", input);
						expected_i = e.Value;
					} else {
						Assert.IsFalse(error, "Unexpected error in token [{0}] of «{1}»", i, input);
					}
					if (expected_i is Pair<string,Symbol> pair) {
						Assert.AreEqual(pair.A, value);
						Assert.AreEqual(pair.B, token.TypeMarker);
					} else {
						Assert.AreEqual(expected_i, value);
					}
				}
				index = token.EndIndex;
			}
			var nothing = lexer.NextToken();
			Assert.That(!nothing.HasValue, "Extra token after the expected ones in «" + input.ToString() + "»");
		}
	}
}
