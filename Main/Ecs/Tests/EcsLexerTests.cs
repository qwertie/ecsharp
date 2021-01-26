using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Numerics;
using Loyc.MiniTest;
using Loyc.Ecs.Parser;
using Loyc.Syntax.Lexing;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	using TT = TokenType;

	[TestFixture]
	public class EcsLexerTests
	{
		[DebuggerStepThrough] static Symbol _(string s) { return GSymbol.Get(s); }
		[DebuggerStepThrough] static T[] A<T>(params T[] list) { return list; }

		static readonly object WS = WhitespaceTag.Value;

		Pair<string, Symbol> L(string v, string tm) => Pair.Create(v, (Symbol)tm);
		Pair<string, Symbol> Str(string v) => Pair.Create(v, (Symbol)null);
		Pair<string, Symbol> Num(string v) => Pair.Create(v, (Symbol)"_");
		Pair<string, Symbol> Num(int v) => Pair.Create(v.ToString(), (Symbol)"_");
		Pair<string, Symbol> Num(double v) => Pair.Create(v.ToString(), (Symbol)"_");
		Pair<string, Symbol> Char(char c) => Pair.Create(c.ToString(), (Symbol)"c");

		[Test]
		public void Basics()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Id, TT.Not), 
				_("hello"), S.Comma, _("world"), S.Not);
			Case(@"this is ""just""1 lexer test '!'",
				A(TT.This, TT.Is, TT.Literal, TT.Literal, TT.Id, TT.Id, TT.Literal),
				S.This, S.Is, Str("just"), Num(1), _("lexer"), _("test"), '!');
			Case(@"12:30", A(TT.Literal, TT.Colon, TT.Literal), Num(12), S.Colon, Num(30));
			Case(@"c+='0'", A(TT.Id, TT.CompoundAssign, TT.Literal), _("c"), S.AddAssign, '0');
			Case("// hello\n\r\n\r/* world */",
				A(TT.SLComment, TT.Newline, TT.Newline, TT.Newline, TT.MLComment));
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen));
			Case(@"finally@@{`boom!` @@bam;}", A(TT.Finally, TT.At, TT.At, TT.LBrace, TT.BQString, TT.Literal, TT.Semicolon, TT.RBrace),
				S.Finally, _("'@"), _("'@"), null, _("boom!"), _("bam"), S.Semicolon, null);
		}

		[Test]
		public void TestIdentifiers()
		{
			Case("abc_123/_0",   A(TT.Id, TT.DivMod, TT.Id),         _("abc_123"), _("'/"), _("_0"));
			Case("is@is",        A(TT.Is, TT.Id),                    S.Is, _("is"));
			Case("A Z@3",        A(TT.Id, TT.Id, TT.Id),             _("A"), _("Z"), _("3"));
			Case(@"\u0041\U00000062\u0063", A(TT.Id),                _("Abc"));
			Case("No#error",     A(TT.Id),                           _("No#error"));
			Case("@#error!@fail",A(TT.Id, TT.Not, TT.Id),            _("#error"), _("'!"), _("fail"));
			Case("#love!foods",  A(TT.Id, TT.Not, TT.Id),            _("#love"), _("'!"), _("foods"));
			Case("#@food:@yum",  A(TT.Id, TT.Id, TT.Colon, TT.Id),   _("#"), _("food"), _("':"), _("yum"));
			Case(@"#()\",        A(TT.Id, TT.LParen, TT.RParen, TT.Backslash), _("#"), null, null, _("'\\"));
			Case(@"@#\u0035@':@",A(TT.Id, TT.Id, TT.At),             _(@"#5"), _("':"), _("'@"));
			Case(@"@0@`@\n`",    A(TT.Id, TT.Id),                    _("0"), _("@\n"));
			Case("won't prime'", A(TT.Id, TT.Id),                    _("won't"), _("prime'"));
			Case(@"@`\``@#`hi!`",A(TT.Id, TT.Id, TT.BQString),       _("`"), _("#"), _("hi!"));
			Case(@"@'()",        A(TT.Id, TT.LParen, TT.RParen),     _("'"),  null, null);
			Case(@"@''@{}",      A(TT.Id, TT.At, TT.LBrace, TT.RBrace), _("''"), _("'@"), null, null);
			Case(@"@#\ X",       A(TT.Id, TT.Backslash, TT.Id),      _(@"#"), _("'\\"), _("X"));
		}

		public void TestUnquotedPunctuationIdentifiers()
		{
			Case("@#error!@fail", A(TT.Id, TT.Not, TT.Id), _("#error"), _("'!"), _("fail"));
			Case("@'error!@fail", A(TT.Id, TT.Id), _("'error!"), _("fail"));
			Case(@"@#\@$_$@==>",  A(TT.Id, TT.Id, TT.Id), _(@"#\"), _("$_$"), _("==>"));
			Case("@`{}`[@>>=]",   A(TT.Id, TT.LBrack, TT.Id, TT.RBrack), _("{}"), null, _(">>="), null);
		}

		[Test]
		public void TestOperators()
		{
			Case("3 - 2",     A(TT.Literal, TT.Sub, TT.Literal), Num(3), _("'-"), Num(2));
			Case("a-b",       A(TT.Id, TT.Sub, TT.Id),            _("a"), _("'-"), _("b"));
			Case("a`blah`b",  A(TT.Id, TT.BQString, TT.Id),   _("a"), _("blah"), _("b"));
			Case(@"a`_\`_`b", A(TT.Id, TT.BQString, TT.Id),   _("a"), _("_`_"), _("b"));
			Case(@">><<",     A(TT.GT, TT.GT, TT.LT, TT.LT),  _("'>"), _("'>"), _("'<"), _("'<"));
			Case(@">>===>",   A(TT.CompoundAssign, TT.Forward),  _("'>>="), _("'==>"));
			Case("3**2 % 10", A(TT.Literal, TT.Power, TT.Literal, TT.DivMod, TT.Literal), Num(3), _("'**"), Num(2), _("'%"), Num(10));
		}

		[Test]
		public void TestStrings()
		{
			Case(@"`testing` ""Testing""'!'", A(TT.BQString, TT.Literal, TT.Literal), _("testing"), Str("Testing"), '!');
			Case(@"   testing""Testing""'!'", A(TT.Literal, TT.Literal), L("Testing", "testing"), '!');
			Case(@"@`testing`""Testing""'!'", A(TT.Literal, TT.Literal), L("Testing", "testing"), '!');
			Case(@"`\a\b\f\v\`\'\""`""\a\b\f\v\`\'\""""'\0'", A(TT.BQString, TT.Literal, TT.Literal),
				_("\a\b\f\v`\'\""), Str("\a\b\f\v`\'\""), '\0');
			Case("@\"\n\"", A(TT.Literal), Str("\n"));
			Case(@"'''Triple-quoted!'''", A(TT.Literal), Str("Triple-quoted!"));
			Case(@"quoted'''Triple!!'''", A(TT.Literal), L("Triple!!", "quoted"));
			Case(@"""""""Triple\n/-quoted!""""""", A(TT.Literal), Str("Triple\n-quoted!"));
			Case("    \"\"\"Triple\n"+
			     "    -quoted!\"\"\"", A(TT.Literal), Str("Triple\n-quoted!"));
			Case("    \"\"\"Triple\n"+
			     "      -quoted!\"\"\"", A(TT.Literal), Str("Triple\n-quoted!"));
			Case("\t\"\"\"Triple\n"+
			     "\t\t-quoted!\"\"\"", A(TT.Literal), Str("Triple\n-quoted!"));
			Case("    \"\"\"Triple\n"+
			     "        -quoted!\"\"\"", A(TT.Literal), Str("Triple\n -quoted!"));
		}

		[Test]
		public void TestHexEscapeSequences()
		{
			// \U escape sequences have variable length (but \u is always 4 chars)
			Case(@"""\UAG"" ""\U020""",    A(TT.Literal, TT.Literal), Str("\nG"), Str(" "));
			Case(@"""\u0020\U00020\x20""", A(TT.Literal), Str("   "));
			Case(@"""\U0020\u00200\x20""", A(TT.Literal), Str("  0 "));
			Case(@"""\u0009\u00009\x09""", A(TT.Literal), Str("\t\09\t"));
			Case(@"""\u2202\U02202\x2202""", A(TT.Literal), Str("\u2202\u2202\"02")); // • Bullet
			Case(@"""\U1F4A9\U10FFFF""",  A(TT.Literal), Str("\uD83D\uDCA9\uDBFF\uDFFF")); // 💩 pile of poo and highest code point
			Case(@"""\U110000""",         A(TT.Literal), Str("\uD804\uDC00" + "0")); // But only 5 digits can be part of this escape
			Case(@"""\U01F4A9\U000020""", A(TT.Literal), Str("\uD83D\uDCA9 ")); // 💩 pile of poo and a space
			Case(@"""\U1F4A90\U20""",     A(TT.Literal), Str("\uD83D\uDCA9" + "0 ")); // Only 5 digits can be part of the first escape
			Case(@"""\u1F4A9\u10FFFF""",  A(TT.Literal), Str("\u1F4A9\u10FFFF")); // only 4 digits are part of each escape sequence
		}

		[Test]
		public void TestShebang()
		{
			Case("#!/bin/sh\r\n// that's called a shebang!",
				A(TT.Shebang, TT.SLComment));
			Case(".#!/bin/sh",
				A(TT.Dot, TT.Id, TT.Not, TT.DivMod, TT.Id, TT.DivMod, TT.Id),
				_("'."), _("#"), _("'!"), _("'/"), _("bin"), _("'/"), _("sh"));
		}

		[Test]
		public void TestBOM()
		{
			Case("\uFEFFx", A(TT.Spaces, TT.Id), WS, _("x"));
		}
		
		[Test]
		public void TestIntegers()
		{
			Case("9", A(TT.Literal), Num(9));
			Case("1337", A(TT.Literal), Num(1337));
			Case("-1", A(TT.Sub, TT.Literal), _("'-"), Num(1));
			Case("9111222U", A(TT.Literal), L("9111222", "_u"));
			Case("0L", A(TT.Literal), L("0","_L"));
			Case("-9111222L", A(TT.Sub, TT.Literal), _("'-"), L("9111222","_L"));
			Case("-1U", A(TT.Sub, TT.Literal), _("'-"), L("1","_u"));
			Case("9_111_222", A(TT.Literal), Num("9_111_222"));
			Case("9_111_222_333", A(TT.Literal), Num("9_111_222_333"));
			Case("4_111_222_333", A(TT.Literal),  L("4_111_222_333", "_"));
			Case("4_111_222_333U", A(TT.Literal), L("4_111_222_333", "_u"));
			Case("9_111_222_333_444_555", A(TT.Literal), Num("9_111_222_333_444_555"));
			Case("9_111_222_333_444_555L", A(TT.Literal),  L("9_111_222_333_444_555", "_L"));
			Case("9_111_222_333_444_555UL", A(TT.Literal), L("9_111_222_333_444_555", "_uL"));
			Case("0x9+0x0A=0x0000_0000_13", A(TT.Literal, TT.Add, TT.Literal, TT.Assign, TT.Literal), Num("0x9"), _("'+"), Num("0x0A"), _("'="), Num("0x0000_0000_13"));
			Case("0x5.Equals()", A(TT.Literal, TT.Dot, TT.Id, TT.LParen, TT.RParen), Num("0x5"), _("'."), _("Equals"), null, null);
			Case("0b1000_0000_1000_0001_1111_1111==0x8081FF", A(TT.Literal, TT.EqNeq, TT.Literal), Num("0b1000_0000_1000_0001_1111_1111"), _("'=="), Num("0x8081FF"));
			Case("0b11L0b10000000_10000001_10010010_11111111U", A(TT.Literal, TT.Literal), L("0b11", "_L"), L("0b10000000_10000001_10010010_11111111", "_u"));
			Case("0b1111_10000000_10000001_10010010_11111111", A(TT.Literal), Num("0b1111_10000000_10000001_10010010_11111111"));

			// These used to be lexer errors, but now that we're using 
			// Uninterpreted Literals, they should be parser errors instead.
			Case("0xFF_0000_0000U", A(TT.Literal), L("0xFF_0000_0000", "_u"));
			Case("0xFFFF_FFFF_0000_0000L", A(TT.Literal), L("0xFFFF_FFFF_0000_0000", "_L"));
			Case("0x1_FFFF_FFFF_0000_0000", A(TT.Literal), L("0x1_FFFF_FFFF_0000_0000", "_"));
		}

		[Test]
		public void TestFloats()
		{
			Case("0.0", A(TT.Literal), Num("0.0"));
			Case("0.1", A(TT.Literal), Num("0.1"));
			Case("25d25f25m", A(TT.Literal, TT.Literal, TT.Literal), L("25","_d"),L("25","_f"),L("25","_m"));
			Case("0.25d", A(TT.Literal), L("0.25", "_d"));
			Case("0.25f", A(TT.Literal), L("0.25", "_f"));
			Case("0.25m", A(TT.Literal), L("0.25", "_m"));
			Case("0.25e2", A(TT.Literal), Num("0.25e2"));
			Case("10e-20", A(TT.Literal), Num("10e-20"));
			Case("0.3e+2d", A(TT.Literal), L("0.3e+2", "_d"));
			Case("0.3e+2f", A(TT.Literal), L("0.3e+2", "_f"));
			Case("0.3e+2m", A(TT.Literal), L("0.3e+2", "_m"));
			Case("1234567890123456789012345678901234567890d", A(TT.Literal),
			   L("1234567890123456789012345678901234567890","_d"));
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(TT.Literal), 
			   L("123456789012345678901234567890.1234567890123456789012345678901234567890","_f"));
			Case(".5e+2.5e+2f.5m", A(TT.Literal, TT.Literal, TT.Literal), 
			   Num(".5e+2"), L(".5e+2","_f"), L(".5","_m"));
			Case("Y.5", A(TT.Id, TT.Literal), _("Y"), Num(".5"));
			Case("0.1.5", A(TT.Literal, TT.Literal), Num("0.1"), Num(".5"));
			Case("5.ToString", A(TT.Literal, TT.Dot, TT.Id), Num(5), _("'."), _("ToString"));
		}
		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.C", A(TT.Literal, TT.Dot, TT.Id), Num("0x0"), _("'."), _("C")); // this is not a float
			Case("0x0.8", A(TT.Literal), Num("0x0.8")); // I changed my mind, this IS a single float
			Case("0x0.8p", A(TT.Literal, TT.Id), Num("0x0.8"), _("p"));
			Case("0x0.0p0", A(TT.Literal), Num("0x0.0p0"));
			Case("0xF.8p0", A(TT.Literal), Num("0xF.8p0"));
			Case("0xF.8p+1;0xF.8p1", A(TT.Literal, TT.Semicolon, TT.Literal), Num("0xF.8p+1"), _("';"), Num("0xF.8p1"));
			Case("0xA.8p-1", A(TT.Literal), Num("0xA.8p-1"));
			Case("0b101.01", A(TT.Literal), Num("0b101.01"));
			Case("0b101.01p0f", A(TT.Literal), L("0b101.01p0","_f"));
			Case("0b101.01p2", A(TT.Literal), Num("0b101.01p2"));
			Case("0b1111_1111.1111_1111p+8", A(TT.Literal), Num("0b1111_1111.1111_1111p+8"));
			Case("0b.1p-2", A(TT.Literal), Num("0b.1p-2"));
			Case("0b.1p-2f", A(TT.Literal), L("0b.1p-2", "_f"));
		}

		[Test]
		public void TestSymbols()
		{
			Case(@"@@public@@is@@A@@`common\\word`@@around",
				A(TT.Literal, TT.Literal, TT.Literal, TT.Literal, TT.Literal),
				_("public"), _("is"), _("A"), _(@"common\word"), _("around"));
			Case(@"@@her\u0065", A(TT.Literal), _("here"));
		}

		[Test]
		public void TestPreprocessor()
		{
			// Note: the standard C# parser accepts input like "# if" (with spaces) 
			// for preprocessor directives; the EC# lexer currently does not.
			Case("#if",       A(TT.PPif), _("##if"));
			Case("/**/#if  ", A(TT.MLComment, TT.Id), WS, _("#if"));
			Case("\t\t#if  ", A(TT.PPif), _("##if"));
			Case("#if Foo\n#elif Bar\n#else//otherwise\n#endif//Foo", 
				A(TT.PPif, TT.Id, TT.Newline, TT.PPelif, TT.Id, TT.Newline, TT.PPelse, TT.SLComment, TT.Newline, TT.PPendif, TT.SLComment),
				_("##if"), _("Foo"), WS, _("##elif"), _("Bar"), WS, _("##else"), WS, WS, _("##endif"), WS);
			Case("#define Foo\n#undef Foo",
				A(TT.PPdefine, TT.Id, TT.Newline, TT.PPundef, TT.Id),
				_("##define"), _("Foo"), WS, _("##undef"), _("Foo"));
			Case("#warning Your life is going nowhere.\n#error--sorry.\n#note EC# only.",
				A(TT.PPwarning, TT.Newline, TT.PPerror, TT.Newline, TT.PPnote),
				" Your life is going nowhere.", WS, "--sorry.", WS, " EC# only.");
			Case("#region The netherworld\n#endregion",
				A(TT.PPregion, TT.Newline, TT.PPendregion),
				" The netherworld", WS, "");
			// Exact match or it's Id
			Case("#defined\n#defin\n#regio\n#endregions", 
				A(TT.Id, TT.Newline, TT.Id, TT.Newline, TT.Id, TT.Newline, TT.Id),
				_("#defined"), WS, _("#defin"), WS, _("#regio"), WS, _("#endregions"));
		}

		const string ERROR = "ERROR";

		[Test]
		public void EofInToken()
		{
			Case(@"""", A(TT.Unknown), ERROR);
			Case(@"'", A(TT.Unknown), ERROR);
			Case(@"`", A(TT.BQString), ERROR);
			Case(@"\", A(TT.Backslash), _(@"'\"));
			Case(@"@", A(TT.At), _("'@"));
			Case(@"2.0e+", A(TT.Literal), ERROR);
			Case("'''fin", A(TT.Literal), ERROR);
			Case(@"""""""", A(TT.Literal), ERROR);
		}

		[Test]
		public void TestOtherErrors()
		{
			//Case("\0",              A(TT.Error), ERROR);
			//Case("\x07",            A(TT.Error), ERROR);
			Case("x=\"Hello\n",     A(TT.Id, TT.Assign, TT.Literal, TT.Newline), _("x"), _("'="), ERROR, WS);
			Case("'\n'o''pq\n?''",  A(TT.Unknown, TT.Newline, TT.Literal, TT.Literal, TT.Newline, TT.QuestionMark, TT.Literal),
			                        ERROR, WS, 'o', ERROR, WS, _("'?"), ERROR);
			Case("'abc'",           A(TT.Literal), ERROR);
			Case("0x!0b",           A(TT.Literal, TT.Not, TT.Literal), ERROR, _("'!"), ERROR);
			Case("`weird\nnewline", A(TT.BQString, TT.Newline, TT.Id), ERROR, WS, _("newline"));
		}

		[Test]
		public void TestKeywords()
		{
			Case("public static int @default=default(stackalloc)as this",
				A(TT.AttrKeyword, TT.AttrKeyword, TT.TypeKeyword, TT.Id, TT.Assign, TT.Default, TT.LParen, TT.Stackalloc, TT.RParen, TT.As, TT.This),
				S.Public, S.Static, S.Int32, _("default"), _("'="), S.Default, null, S.StackAlloc, null, S.As, S.This);
			Case("case'\0':return'x';",
				A(TT.Case, TT.Literal, TT.Colon, TT.Return, TT.Literal, TT.Semicolon),
				S.Case, '\0', _("':"), S.Return, 'x', _("';"));
		}

		void Case(string input, TokenType[] tokenTypes, params object[] values)
		{
			Debug.Assert(values.Length <= tokenTypes.Length);
			
			bool error = false;
			var lexer = new EcsLexer(input, new MessageSinkFromDelegate((type, ctx, msg, args) => {
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
					var expected = values[i];
					if (expected is Pair<string, Symbol> pair) {
						Assert.AreEqual(pair.A, token.TextValue((UString)input).ToString());
						Assert.AreEqual(pair.B, token.TypeMarker);
					} else {
						Assert.AreEqual(values[i] == (object)ERROR, error);
						if (!error)
							Assert.AreEqual(values[i], token.Value);
					}
				}
				index = token.EndIndex;
			}
			Assert.IsFalse(lexer.NextToken().HasValue);
		}
	}
}
