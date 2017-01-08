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
			var sqOperator = TT.SingleQuoteOp;
			Case("x'y", A(TT.Id), _("x'y"));
			Case("x 'y", A(TT.Id, sqOperator), _("x"), _("'y"));
			Case("x 'abcABC123", A(TT.Id, sqOperator), _("x"), _("'abcABC123"));
			Case("'>s 0", A(sqOperator, TT.Literal), _("'>s"), 0);
			Case("'~!#$%^&*-_=+| '<>./?abc123ABC", A(sqOperator, sqOperator), _("'~!#$%^&*-_=+|"), _("'<>./?abc123ABC"));
			Case(@"'{ } '", A(sqOperator, TT.LBrace, TT.RBrace, sqOperator),
				_("'"), null, null, _("'"));
			Case(@"'hello++;'%GOODBYE$", A(sqOperator, TT.Semicolon, sqOperator),
				_("'hello++"), _("';"), _("'%GOODBYE$"));
		}

		[Test]
		public void TestKeywords()
		{
			Case(".if .If", A(TT.Keyword, TT.Keyword), _(".if"), _(".If"));
			Case("A.kw 5.kw", A(TT.Id, TT.Dot, TT.Id, TT.Literal, TT.Dot, TT.Id), _("A"), _("'."), _("kw"), 5, _("'."), _("kw"));
			Case("x'.abc.de", A(TT.Id, TT.Dot, TT.Id, TT.Dot, TT.Id), _("x'"), _("'."), _("abc"), _("'."), _("de"));
			// Keywords currently don't allow continuation characters like ' or digits
			Case(".kw123", A(TT.Keyword, TT.Literal), _(".kw"), 123);
		}

		[Test]
		public void TestNormalStrings()
		{
			Case(@"""Testing""'!'", A(TT.Literal, TT.Literal), "Testing", '!');
			Case(@" ""\a\b\f\v\`\'\""""'\0'", A(TT.Literal, TT.Literal), "\a\b\f\v`\'\"", '\0');
			// There are no C#-style 'verbatim' strings in LES, use triple-quoted strings instead.
			Case(@"""\n"" @""\\""", A(TT.Literal, TT.At, TT.Literal), "\n", _("'@"), "\\");
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
			Case("foo\"bar\"", A(TT.Literal),         CL("bar", "foo"));
			Case("`foo`\"bar\"", A(TT.Literal),       CL("bar", "foo"));
			Case("foo\"\"\"bar\"\"\"", A(TT.Literal), CL("bar", "foo"));
			Case("foo'''bar'''", A(TT.Literal),       CL("bar", "foo"));
			Case(@"`foo\n`'''bar'''", A(TT.Literal),  CL("bar", "foo\n"));
			Case("foo '''bar'''", A(TT.Id, TT.Literal), (Symbol)"foo", "bar");
		}

		[Test]
		public void TestEscapeSequences()
		{
			Case(@"""\u0020\u00020\x20""", A(TT.Literal), "   ");
			Case(@"""\u0009\u00009\x09""", A(TT.Literal), "\t\t\t");
			Case(@"""\u2202\u02202\x2202""", A(TT.Literal), "\u2202\u2202\"02"); // • Bullet
			Case(@"""\u1F4A9\u10FFFF""", A(TT.Literal), "\uD83D\uDCA9\uDBFF\uDFFF"); // 💩 pile of poo and highest code point
			Case(@"""\u1F4A90""", A(TT.Literal), new Error("\uD83D\uDCA9"+"0")); // This escape is not really 6 digits
			Case(@"""\u110000""", A(TT.Literal), new Error("\uD804\uDC00"+"0")); // The greatest escape U+10FFFF plus 1
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
			//Case(@"$x\y\`bq`\", A(TT.PrefixOp, TT.Id, TT.SuffixOp, TT.BQString, TT.NormalOp), _("$"), _("x"), _(@"y\"), _("bq"), _(@"\"));
			Case(@"$~!%^&*-+=|<>/?:._", A(TT.PrefixOp, TT.Id), _("'$~!%^&*-+=|<>/?:."), _("_"));
			Case(@"$~!%^&*-+=|<>_/?:.", A(TT.PrefixOp, TT.Id, TT.NormalOp), _("'$~!%^&*-+=|<>"), _("_"), _("'/?:."));
			Case(@"@,!;: :^=", A(TT.At, TT.Comma, TT.Not, TT.Semicolon, TT.Colon, TT.Assignment), _("'@"), _("',"), _("'!"), _("';"), _("':"), _(@"':^="));
		}

		[Test]
		public void TestIntegers()
		{
			Case("9", A(TT.Literal), 9);
			Case("1337", A(TT.Literal), 1337);
			Case("-1", A(TT.NegativeLiteral), -1);
			Case("9111222U", A(TT.Literal), 9111222U);
			Case("0x8000_0000", A(TT.Literal), 0x80000000);
			Case("0L e 0x8000000000000000", A(TT.Literal, TT.Id, TT.Literal), 0L, _("e"), 0x8000000000000000);
			Case("-9111222L", A(TT.NegativeLiteral), -9111222L);
			Case("-1U", A(TT.NegativeLiteral), new Error(CL("-1", "U")));
			Case("9_111_222", A(TT.Literal), 9111222);
			Case("9_111_222_333", A(TT.Literal), 9111222333);
			Case("4_111_222_333", A(TT.Literal), 4111222333);
			Case("4_111_222_333U", A(TT.Literal), 4111222333U);
			Case("9_111_222_333_444_555", A(TT.Literal), 9111222333444555);
			Case("9_111_222_333_444_555L", A(TT.Literal), 9111222333444555L);
			Case("9_111_222_333_444_555UL", A(TT.Literal), 9111222333444555UL);
			Case("0x9+0x0A=0x0000_0000_13", A(TT.Literal, TT.NormalOp, TT.Literal, TT.Assignment, TT.Literal), 0x9, _("'+"), 0x0A, _("'="), 0x13);
			Case("0b1000_0000_1000_0001_1111_1111==0x8081FF", A(TT.Literal, TT.NormalOp, TT.Literal), 0x8081FF, _("'=="), 0x8081FF);
			Case("0b11L 0b10000000_10000001_10010010_11111111U", A(TT.Literal, TT.Literal), 3L, 0x808192FFU);
			Case("0b1111_10000000_10000001_10010010_11111111", A(TT.Literal), 0x0F808192FF);
		}

		[Test]
		public void TestBigInts()
		{
			Case("11z\n                  ",  A(TT.Literal, TT.Newline), new BigInteger(11), null);
			Case("9_111_222_333_444_555z ",  A(TT.Literal), new BigInteger(9111222333444555UL));
			Case("9999111222333444555000z",  A(TT.Literal), 1000 * new BigInteger(9999111222333444555UL));
			Case("9999111222333444555000",   A(TT.Literal), 1000 * new BigInteger(9999111222333444555UL));
			Case("0x1_FFFF_FFFF_0000_0000",  A(TT.Literal), (BigInteger)0x1FFFFFFFF0000000 << 4);
			Case("-1z",                      A(TT.NegativeLiteral), new BigInteger(-1));
			Case("-9999111222333444555000",  A(TT.NegativeLiteral), -1000 * new BigInteger(9999111222333444555UL));
			Case("-18446744069414584320",    A(TT.NegativeLiteral), BigInteger.Parse("-18446744069414584320"));
			Case("-9111222z             \n", A(TT.NegativeLiteral, TT.Newline), new BigInteger(-9111222L), null);
			Case("-9999111222333444555000z", A(TT.NegativeLiteral), -1000 * new BigInteger(9999111222333444555UL));
			Case("123456789012345678901234567890", A(TT.Literal), BigInteger.Parse("123456789012345678901234567890"));
		}

		[Test]
		public void TestFloats()
		{
			Case("0.0", A(TT.Literal), 0.0);
			Case("0.1", A(TT.Literal), 0.1);
			Case("25d 25f 25m", A(TT.Literal, TT.Literal, TT.Literal), 25d, 25f, 25m);
			Case("0.25d", A(TT.Literal), 0.25d);
			Case("0.25f", A(TT.Literal), 0.25f);
			Case("0.25m", A(TT.Literal), 0.25m);
			Case("0.25e2", A(TT.Literal), 0.25e2);
			Case("10e-20", A(TT.Literal), 10e-20);
			Case("0.3e+2d", A(TT.Literal), 0.3e+2d);
			Case("0.3e+2f", A(TT.Literal), 0.3e+2f);
			Case("0.3e+2m", A(TT.Literal), 0.3e+2m);
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(TT.Literal),
				  123456789012345678901234567890.1234567890123456789012345678901234567890f);
			Case(".5e+2.5e+2f.5m", A(TT.Literal, TT.Literal, TT.Literal), .5e+2, .5e+2f, .5m);
			Case("Y.5", A(TT.Id, TT.Literal), _("Y"), .5);
			Case("0.1.5", A(TT.Literal, TT.Literal), 0.1, .5);
			Case("5.ToString", A(TT.Literal, TT.Dot, TT.Id), 5, _("'."), _("ToString"));
		}

		[Test(Fails = "Succeeds on qwertie's PC but not AppVeyor. Rounding difference somewhere.")]
		public void TestFloats2()
		{
			Case("1234567890123456789012345678901234567890d", A(TT.Literal), 1234567890123456789012345678901234567890d);
		}

		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.0p1234", A(TT.Literal), 0.0);
			Case("0x0.0", A(TT.Literal), 0.0);
			Case("0xF.8", A(TT.Literal), 15.5);
			Case("0xF.8p+1;0xF.8p1", A(TT.Literal, TT.Semicolon, TT.Literal), 31.0, _("';"), 31.0);
			Case("0xA.8p-1", A(TT.Literal), 5.25);
			Case("0x123.88p0", A(TT.Literal), 291.53125);
			Case("0x123.A8p0", A(TT.Literal), 291.65625);
			Case("0x123.8p-10", A(TT.Literal), 291.5 / 1024);
			Case("0b101.01", A(TT.Literal), 5.25);
			Case("0b101.01p0f", A(TT.Literal), 5.25f);
			Case("0b101.01p2", A(TT.Literal), 21.0);
			Case("0b1111_1111.1111_1111p+8", A(TT.Literal), (double)0xFFFF);
			Case("0b.1p-2", A(TT.Literal), 0.125);
			Case("0b.1p-2f", A(TT.Literal), 0.125f);
		}

		[Test]
		public void TestAlternateNumericSuffixes()
		{
			Case(" 123u32 123u64", A(TT.Literal, TT.Literal), 123u, 123uL);
			Case(" 123i32 123i64", A(TT.Literal, TT.Literal), 123, 123L);
			Case("-123i32-123i64", A(TT.NegativeLiteral, TT.NegativeLiteral), -123, -123L);
			Case("0x123u32 0x123u64", A(TT.Literal, TT.Literal), 0x123u, 0x123uL);
			Case("0x123i32 0x123i64", A(TT.Literal, TT.Literal), 0x123, 0x123L);
			Case("0.0f32   0.0f64", A(TT.Literal, TT.Literal), 0.0f, 0.0d);
			Case("0xF.8p0f32 0xF.8p0f64", A(TT.Literal, TT.Literal), 15.5f, 15.5d);
		}

		CustomLiteral CL(object v, string tm) { return new CustomLiteral(v, (Symbol)tm); }

		[Test]
		public void TestUnifiedLiteralPipeline()
		{
			Case(@"-123 0b1111", A(TT.NegativeLiteral, TT.Literal), -123, 15);
			Case(@"number""-123"" number""0b1111""", A(TT.Literal, TT.Literal), -123, 15);
			Case(@"number""123"" number""0x123""", A(TT.Literal, TT.Literal), 123, 0x123);
			Case(@"number""123.0"" number""0x123.4p4""", A(TT.Literal, TT.Literal), 123.0, (double)0x1234);
			Case(@"number""-123d"" number"" 123 """, A(TT.Literal, TT.Literal), new Error(CL("-123d", "number")), new Error(CL(" 123 ", "number")));
			Case(@"f64""123"" f32""-1.25e2""", A(TT.Literal, TT.Literal), 123d, -125f);
			Case(@"`uL`""9876543210"" `u`""-123""", A(TT.Literal, TT.Literal), 9876543210uL, new Error(CL("-123", "u")));
			Case(@"s""Hello!"" 123e456s", A(TT.Literal, TT.Literal), (Symbol)"Hello!", (Symbol)"123e456");
			Case(@"s""Hello!"" 123e456s", A(TT.Literal, TT.Literal), (Symbol)"Hello!", (Symbol)"123e456");
			// Empty type marker should be treated the same as no type marker
			Case("``'''123'''", A(TT.Literal), "123");
			Case("123``",       A(TT.Literal), 123);
			// PURE EVIL. "MOST NEFARIOUS" AWARD.
			Case(@"number""\x31\x32\x33""", A(TT.Literal), 123);
		}

		[Test]
		public void TestCustomNumericLiterals()
		{
			Case("123kb", A(TT.Literal), CL("123", "kb"));
			Case("-123KB", A(TT.NegativeLiteral), CL("-123", "KB"));
			Case("123foo", A(TT.Literal), CL("123", "foo"));
			Case("123poo", A(TT.Literal), CL("123", "poo"));
			Case("0xAzoo", A(TT.Literal), CL("0xA", "zoo"));
			Case("0xA.8p0float", A(TT.Literal), CL("0xA.8p0", "float"));
			Case("123over500", A(TT.Literal), CL("123",   "over500"));
			Case("123.0float", A(TT.Literal), CL("123.0", "float"));
			Case("123f123d", A(TT.Literal),   CL("123",   "f123d"));
		}

		[Test]
		public void TestCustomAtAtLiterals()
		{
			Case(@"@@foo@@is$a!common%word@@around@@here",
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				AtAt("foo"), AtAt("is$a!common%word"), AtAt("around"), AtAt("here"));
			Case(@"@@/?:.$", A(TT.Literal), AtAt("/?:.$"));
			Case(@"@@+-*/", A(TT.Literal), AtAt("+-*/"));
			Case(@"@@+- //x", A(TT.Literal, TT.SLComment), AtAt("+-"), WS);
			Case(@"@@+-//x", A(TT.Literal), AtAt("+-//x"));
		}
		CustomLiteral AtAt(string s) { return new CustomLiteral(s, _("@@")); }

		[Test]
		public void OtherSpecialLiterals()
		{
			// @@ means "special literal"; if the text after @@ is not recognized
			// as a special literal, then it is treated as a symbol.
			Case("@@true @@false @@null", A(TT.Literal, TT.Literal, TT.Literal), true, false, null);
			Case("`inf.d`@@inf.d", A(TT.BQId, TT.Literal), _("inf.d"), double.PositiveInfinity);
			Case("`inf.f`@@inf.f", A(TT.BQId, TT.Literal), _("inf.f"), float.PositiveInfinity);
			Case("`nan.d`@@nan.d", A(TT.BQId, TT.Literal), _("nan.d"), double.NaN);
			Case("`nan.f`@@nan.f", A(TT.BQId, TT.Literal), _("nan.f"), float.NaN);
			Case("@@-inf.d @@-inf.f", A(TT.Literal, TT.Literal), double.NegativeInfinity, float.NegativeInfinity);
		}

		[Test]
		public void TestSymbols()
		{
			Case(@"s""foo""s""is$a!common%word""s""""""around""""""s'''here'''",
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				_("foo"), _("is$a!common%word"), _("around"), _("here"));
		}

		[Test]
		public void Misc()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Id, TT.Not),
				_("hello"), _("',"), _("world"), _("'!"));
			Case(@"this is ""just""1 lexer test '!'",
				A(TT.Id, TT.Id, TT.Literal, TT.Literal, TT.Id, TT.Id, TT.Literal),
				_("this"), _("is"), "just", 1, _("lexer"), _("test"), '!');
			Case(@"12:30", A(TT.Literal, TT.Colon, TT.Literal), 12, _("':"), 30);
			Case(@"c+='0'", A(TT.Id, TT.Assignment, TT.Literal), _("c"), _("'+="), '0');
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen), null, null, null, null, null, null);
			Case(@"a ""b""", A(TT.Id, TT.Literal), _("a"), "b");

			Case(@"'\'''!'", A(TT.Literal, TT.Literal), '\'', '!');
			Case("1 1.0f 1.0 0x1 0b1", A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				1, 1.0f, 1d, 0x1, 1);
			Case("1L 2UL 3u 4f 5d 6m", A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				1L, 2UL, 3u, 4f, 5d, 6m);
		}

		[Test]
		public void EofInToken()
		{
			Case(@"""", A(TT.Literal), new Error(""));
			Case(@"`", A(TT.BQId), new Error(_("")));
			//Case(@"\\", A(TT.SuffixOp), _(@"\"));
			Case(@"@", A(TT.At), _(@"'@"));
			Case(@"@@", A(TT.At, TT.At), _(@"'@"), _(@"'@"));
			Case(@"2.0e+", A(TT.Literal), new Error(CL("2.0e+", "number")));
			// This interpretation would also be reasonable:
			//Case(@"2.0e+", A(TT.Literal, TT.NormalOp), CL("2.0", "e"), _("'+"));
		}

		[Test]
		public void TestErrors()
		{
			Case("\0", A(TT.Unknown), (object)null);
			Case("x=\"Hello\n", A(TT.Id, TT.Assignment, TT.Literal, TT.Newline), _("x"), _("'="), new Error("Hello"), null);
			Case("'\n'o'\"pq\n?", A(TT.SingleQuoteOp, TT.Newline, TT.Literal, TT.Literal, TT.Newline, TT.NormalOp),
			                      _("'"), null, 'o', new Error("pq"), null, _("'?"));
			Case("'' ''", A(TT.Unknown, TT.Unknown), new Error(_("''")), new Error(_("''")));
			Case("0x!0b", A(TT.Literal, TT.Not, TT.Literal), new Error(CL("0x", "number")), _("'!"), new Error(CL("0b", "number")));
			Case("(`weird\nnewline", A(TT.LParen, TT.BQId, TT.Newline, TT.Id), null, new Error(_("weird")), WS, _("newline"));
			Case("0xFF_0000_0000U", A(TT.Literal), new Error(CL("0xFF_0000_0000", "U")));
			Case("0xFFFF_FFFF_0000_0000L", A(TT.Literal), new Error(CL("0xFFFF_FFFF_0000_0000", "L")));
			Case(@"\()\", A(TT.Unknown, TT.LParen, TT.RParen, TT.Unknown), null, null, null, null);
			Case("'abc'", A(TT.Unknown), new Error(_("'abc'")));
			Case(@"`true`""bool?"" `null`""null?""", A(TT.Literal, TT.Literal), CL("bool?", "true"), CL("null?", "null"));
			Case(@"true""nonbool"" null""nonnull""", A(TT.Literal, TT.Literal), new Error(CL("nonbool", "true")), new Error(CL("nonnull", "null")));
			Case(".kw'", A(TT.Keyword, TT.SingleQuoteOp), _(".kw"), _("'"));
		}

		[Test]
		public void TestDotIndents()
		{
			// A dot-indented line must start with a dot and each dot must be followed by a space.
			Case(". Hello", A(TT.Id), _("Hello"));
			Case(" .\n. ", A(TT.Dot, TT.Newline), _("'."), null);
			Case(".   .  . Hello", A(TT.Id), _("Hello"));
			Case(".\t.\t.\tHello\n.. Goodbye", A(TT.Id, TT.Newline, TT.NormalOp, TT.Id), _("Hello"), null, _("'.."), _("Goodbye"));
			Case(". .Hello",  A(TT.Keyword), _(".Hello"));
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
				MessageSink.Trace.Write(type, ctx, msg, args); error = true;
			}));

			int index = 0;
			for (int i = 0; i < tokenTypes.Length; i++)
			{
				error = false;
				Token token = lexer.NextToken().Value;
				Assert.LessOrEqual(index, token.StartIndex);
				Assert.AreEqual(tokenTypes[i], token.Type());
				if (i < expected.Length) {
					if (expected[i] is Error) {
						Assert.IsTrue(error, "Expected error didn't occur in «{0}»", input);
						Assert.AreEqual(((Error)expected[i]).Value, token.Value);
					} else {
						Assert.AreEqual(expected[i], token.Value);
						Assert.IsFalse(error, "Unexpected error in token [{0}] of «{1}»", i, input);
					}
				}
				index = token.EndIndex;
			}
			var nothing = lexer.NextToken();
			Assert.That(!nothing.HasValue, "Extra token after the expected ones in «" + input.ToString() + "»");
		}
	}
}
