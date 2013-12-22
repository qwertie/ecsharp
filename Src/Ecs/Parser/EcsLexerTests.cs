using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc;
using System.Diagnostics;
using S = Loyc.Syntax.CodeSymbols;

namespace Ecs.Parser
{
	using TT = TokenType;
	using Loyc.Syntax.Lexing;

	[TestFixture]
	class EcsLexerTests
	{
		[DebuggerStepThrough] static Symbol _(string s) { return GSymbol.Get(s); }
		[DebuggerStepThrough] static T[] A<T>(params T[] list) { return list; }

		static readonly object WS = WhitespaceTag.Value;

		[Test]
		public void Basics()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Spaces, TT.Id, TT.Not), 
				_("hello"), _("#,"), WS, _("world"), _("#!"));
			Case(@"this is""just""1 lexer test '!'",
				A(TT.@this, TT.Spaces, TT.@is, TT.String, TT.Number, TT.Spaces, TT.Id, TT.Spaces, TT.Id, TT.Spaces, TT.SQString),
				S.This, WS, S.Is, "just", 1, WS, _("lexer"), WS, _("test"), WS, '!');
			Case(@"12:30", A(TT.Number, TT.Colon, TT.Number), 12, _("#:"), 30);
			Case(@"c+='0'", A(TT.Id, TT.CompoundSet, TT.SQString), _("c"), _("#+="), '0');
			Case("// hello\n\r\n\r/* world */",
				A(TT.SLComment, TT.Newline, TT.Newline, TT.Newline, TT.MLComment));
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen));
			Case(@"finally@@{`boom!` @@bam;}", A(TT.@finally, TT.At, TT.At, TT.LBrace, TT.BQString, TT.Spaces, TT.Symbol, TT.Semicolon, TT.RBrace),
				S.Finally, _("#@"), _("#@"), null, _("boom!"), WS, _("bam"), _("#;"), null);
		}

		[Test]
		public void TestIdentifiers()
		{
			Case("abc_123/_0",   A(TT.Id, TT.DivMod, TT.Id),            _("abc_123"), _("#/"), _("_0"));
			Case("is@is",        A(TT.@is, TT.Id),                   S.Is, _("is"));
			Case("\u0041\U00000062\u0063", A(TT.Id),                 _("Abc"));
			Case("No#error",     A(TT.Id),                           _("No#error"));
			Case("@#error!@fail",A(TT.Id, TT.Id),                    _("#error!"), _("fail"));
			Case("#love!foods",  A(TT.Id, TT.Not, TT.Id),            _("#love"), _("#!"), _("foods"));
			Case("#@food:@yum",  A(TT.Id, TT.Id, TT.Colon, TT.Id),   _("#"), _("food"), _("#:"), _("yum"));
			Case(@"#()\",        A(TT.Id, TT.LParen, TT.RParen, TT.Backslash), _("#"), null, null, _("#\\"));
			Case(@"@#\@$_$@#==>",A(TT.Id, TT.Id, TT.Id),             _(@"#\"), _("$_$"), _("#==>"));
			Case("@`#{}`[@#>>=]",A(TT.Id, TT.LBrack, TT.Id, TT.RBrack), _("#{}"), null, _("#>>="), null);
			Case(@"@0@`@\n`",    A(TT.Id, TT.Id),                    _("0"), _(@"@\n"));
			Case("won't prime'", A(TT.Id, TT.Spaces, TT.Id),         _("won't"), WS, _("prime'"));
			Case("@````@#`hi!`", A(TT.Id, TT.Id, TT.BQString),       _("`"), _("#"), _("hi!"));
		}

		[Test]
		public void TestOperators()
		{
			Case("3 - 2",     A(TT.Number, TT.Spaces, TT.Sub, TT.Spaces, TT.Number), 3, WS, _("#-"), WS, 2);
			Case("a-b",       A(TT.Id, TT.Sub, TT.Id),            _("a"), _("#-"), _("b"));
			Case("a`blah`b",  A(TT.Id, TT.BQString, TT.Id),   _("a"), _("blah"), _("b"));
			Case(@"a`_\`_`b", A(TT.Id, TT.BQString, TT.Id),   _("a"), _("_`_"), _("b"));
			Case(@">><<",     A(TT.GT, TT.GT, TT.LT, TT.LT),  _("#>"), _("#>"), _("#<"), _("#<"));
			Case(@">>===>",   A(TT.CompoundSet, TT.Forward),  _("#>>="), _("#==>"));
			Case("3**2 % 10", A(TT.Number, TT.Power, TT.Number, TT.Spaces, TT.DivMod, TT.Spaces, TT.Number), 3, _("#**"), 2, WS, _("#%"), WS, 10);
		}

		[Test]
		public void TestStrings()
		{
			Case(@"`Testing`""Testing""'!'", A(TT.BQString, TT.String, TT.SQString), _("Testing"), "Testing", '!');
			Case(@"`\a\b\f\v\`\'\""`""\a\b\f\v\`\'\""""'\0'", A(TT.BQString, TT.String, TT.SQString),
				_("\a\b\f\v`\'\""), "\a\b\f\v`\'\"", '\0');
			Case(@"'''Triple-quoted!'''", A(TT.String), "Triple-quoted!");
			Case(@"""""""Triple\n/-quoted!""""""", A(TT.String), "Triple\n-quoted!");
		}

		[Test]
		public void TestShebang()
		{
			Case("#!/bin/sh\r\n// that's called a shebang!",
				A(TT.Shebang, TT.SLComment));
			Case(".#!/bin/sh",
				A(TT.Dot, TT.Id, TT.Not, TT.DivMod, TT.Id, TT.DivMod, TT.Id),
				_("#."), _("#"), _("#!"), _("#/"), _("bin"), _("#/"), _("sh"));
		}

		[Test]
		public void TestIntegers()
		{
			Case("9", A(TT.Number), 9);
			Case("1337", A(TT.Number), 1337);
			Case("-1", A(TT.Number), -1);
			Case("9111222U", A(TT.Number), 9111222U);
			Case("0L", A(TT.Number), 0L);
			Case("-9111222L", A(TT.Number), -9111222L);
			Case("-1U", A(TT.Sub, TT.Number), _("#-"), 1U);
			Case("9_111_222", A(TT.Number), 9111222);
			Case("9_111_222_333", A(TT.Number), 9111222333);
			Case("4_111_222_333", A(TT.Number), 4111222333);
			Case("4_111_222_333U", A(TT.Number), 4111222333U);
			Case("9_111_222_333_444_555", A(TT.Number), 9111222333444555);
			Case("9_111_222_333_444_555L", A(TT.Number), 9111222333444555L);
			Case("9_111_222_333_444_555UL", A(TT.Number), 9111222333444555UL);
			Case("0x9+0x0A=0x0000_0000_13", A(TT.Number, TT.Add, TT.Number, TT.Set, TT.Number), 0x9, _("#+"), 0x0A, _("#="), 0x13);
			Case("0x5.Equals()", A(TT.Number, TT.Dot, TT.Id, TT.LParen, TT.RParen), 0x5, _("#."), _("Equals"), null, null);
			Case("0b1000_0000_1000_0001_1111_1111==0x8081FF", A(TT.Number, TT.EqNeq, TT.Number), 0x8081FF, _("#=="), 0x8081FF);
			Case("0b11L0b10000000_10000001_10010010_11111111U", A(TT.Number, TT.Number), 3L, 0x808192FFU);
			Case("0b1111_10000000_10000001_10010010_11111111", A(TT.Number), 0x0F808192FF);
		}

		[Test]
		public void TestFloats()
		{
			Case("0.0", A(TT.Number), 0.0);
			Case("0.1", A(TT.Number), 0.1);
            Case("25d25f25m", A(TT.Number, TT.Number, TT.Number), 25d,25f,25m);
			Case("0.25d", A(TT.Number), 0.25d);
			Case("0.25f", A(TT.Number), 0.25f);
			Case("0.25m", A(TT.Number), 0.25m);
			Case("0.25e2", A(TT.Number), 0.25e2);
			Case("10e-20", A(TT.Number), 10e-20);
			Case("0.3e+2d", A(TT.Number), 0.3e+2d);
			Case("0.3e+2f", A(TT.Number), 0.3e+2f);
			Case("0.3e+2m", A(TT.Number), 0.3e+2m);
			Case("1234567890123456789012345678901234567890d", A(TT.Number), 1234567890123456789012345678901234567890d);
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(TT.Number), 
			      123456789012345678901234567890.1234567890123456789012345678901234567890f);
			Case(".5e+2.5e+2f.5m", A(TT.Number, TT.Number, TT.Number), .5e+2, .5e+2f, .5m);
			Case("Y.5", A(TT.Id, TT.Number), _("Y"), .5);
			Case("0.1.5", A(TT.Number, TT.Number), 0.1, .5);
			Case("5.ToString", A(TT.Number, TT.Dot, TT.Id), 5, _("#."), _("ToString"));
		}
		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.C", A(TT.Number, TT.Dot, TT.Id), 0, _("#."), _("C")); // this is not a float
			Case("0x0.8", A(TT.Number), 0.5); // I changed my mind, this IS a single float
			Case("0x0.8p", A(TT.Number, TT.Id), 0.5, _("p"));
			Case("0x0.0p0", A(TT.Number), 0.0);
			Case("0xF.8p0", A(TT.Number), 15.5);
			Case("0xF.8p+1;0xF.8p1", A(TT.Number, TT.Semicolon, TT.Number), 31, _("#;"), 31);
			Case("0xA.8p-1", A(TT.Number), 5.25);
			Case("0b101.01", A(TT.Number), 5.25);
			Case("0b101.01p0f", A(TT.Number), 5.25f);
			Case("0b101.01p2", A(TT.Number), 21.0);
			Case("0b1111_1111.1111_1111p+8", A(TT.Number), (double)0xFFFF);
			Case("0b.1p-2", A(TT.Number), 0.125);
			Case("0b.1p-2f", A(TT.Number), 0.125f);
		}

		[Test]
		public void TestSymbols()
		{
			Case(@"@@public@@is@@A@@`common\\word`@@around",
				A(TT.Symbol, TT.Symbol, TT.Symbol, TT.Symbol, TT.Symbol),
				_("public"), _("is"), _("A"), _(@"common\\word"), _("around"));
			Case(@"@@her\u0065", A(TT.Symbol), _("here"));
		}

		[Test]
		public void TestPreprocessor()
		{
			Case("#if",       A(TT.PPif), _("##if"));
			Case("/**/#if  ", A(TT.MLComment, TT.Id, TT.Spaces), WS, _("#if"), WS);
			Case("\t\t#if  ", A(TT.Spaces, TT.PPif, TT.Spaces), WS, _("##if"), WS);
			Case("#if Foo\n#elif Bar\n#else//otherwise\n#endif//Foo", 
				A(TT.PPif, TT.Spaces, TT.Id, TT.Newline, TT.PPelif, TT.Spaces, TT.Id, TT.Newline, TT.PPelse, TT.SLComment, TT.Newline, TT.PPendif, TT.SLComment),
				_("##if"), WS, _("Foo"), WS, _("##elif"), WS, _("Bar"), WS, _("##else"), WS, WS, _("##endif"), WS);
			Case("#define Foo\n#undef Foo",
				A(TT.PPdefine, TT.Spaces, TT.Id, TT.Newline, TT.PPundef, TT.Spaces, TT.Id),
				_("##define"), WS, _("Foo"), WS, _("##undef"), WS, _("Foo"));
			Case("#warning Your life is going nowhere.\n#error--sorry.\n#note EC# only.",
				A(TT.PPwarning, TT.Newline, TT.PPerror, TT.Newline, TT.PPnote),
				" Your life is going nowhere.", WS, "--sorry.", WS, " EC# only.");
			Case("#region The netherworld\n#endregion",
				A(TT.PPregion, TT.Newline, TT.PPendregion),
				" The netherworld", WS, _("##endregion"));
			// Exact match or it's Id
			Case("#defined\n#defin\n#regio\n#endregi", 
				A(TT.Id, TT.Newline, TT.Id, TT.Newline, TT.Id, TT.Newline, TT.Id),
				_("#defined"), WS, _("#defin"), WS, _("#regio"), WS, _("#endregi"));
		}

		const string ERROR = "ERROR";

		[Test]
		public void EofInToken()
		{
			Case(@"""", A(TT.String), ERROR);
			Case(@"'", A(TT.SQString), ERROR);
			Case(@"`", A(TT.BQString), ERROR);
			Case(@"\", A(TT.Backslash), _(@"#\"));
			Case(@"@", A(TT.At), _(@"#@"));
			Case(@"2.0e+", A(TT.Number), ERROR);
			Case("'''fin", A(TT.String), ERROR);
			Case(@"""""""", A(TT.String), ERROR);
		}

		[Test]
		public void TestOtherErrors()
		{
			//Case("\0",              A(TT.Error), ERROR);
			//Case("\x07",            A(TT.Error), ERROR);
			Case("x=\"Hello\n",     A(TT.Id, TT.Set, TT.String, TT.Newline), _("x"), _("#="), ERROR, WS);
			Case("'\n'o''pq\n?''",  A(TT.SQString, TT.Newline, TT.SQString, TT.SQString, TT.Newline, TT.QuestionMark, TT.SQString),
			                        ERROR, WS, 'o', ERROR, WS, _("#?"), ERROR);
			Case("'abc'",           A(TT.SQString), ERROR);
			Case("0x!0b",           A(TT.Number, TT.Not, TT.Number), ERROR, _("#!"), ERROR);
			Case("`weird\nnewline", A(TT.BQString, TT.Newline, TT.Id), ERROR, WS, _("newline"));
			Case("0xFF_0000_0000U", A(TT.Number), ERROR);
			Case("0xFFFF_FFFF_0000_0000L", A(TT.Number), ERROR);
			Case("0x1_FFFF_FFFF_0000_0000", A(TT.Number), ERROR);
		}

		[Test]
		public void TestKeywords()
		{
			Case("public static int @default=default(stackalloc)as this",
				A(TT.AttrKeyword, TT.Spaces, TT.AttrKeyword, TT.Spaces, TT.TypeKeyword, TT.Spaces, TT.Id, TT.Set, TT.@default, TT.LParen, TT.@stackalloc, TT.RParen, TT.@as, TT.Spaces, TT.@this),
				S.Public, WS, S.Static, WS, S.Int32, WS, _("default"), _("#="), S.Default, null, S.StackAlloc, null, S.As, WS, S.This);
			Case("case'\0':return'x';",
				A(TT.@case, TT.SQString, TT.Colon, TT.@return, TT.SQString, TT.Semicolon),
				S.Case, '\0', _("#:"), S.Return, 'x', _("#;"));
		}

		void Case(string input, TokenType[] tokenTypes, params object[] values)
		{
			Debug.Assert(values.Length <= tokenTypes.Length);
			
			bool error = false;
			var lexer = new EcsLexer(input, (_, msg) => { Trace.WriteLine(msg); error = true; });

			int index = 0;
			for (int i = 0; i < tokenTypes.Length; i++)
			{
				error = false;
				Token token = lexer.NextToken().Value;
				Assert.AreEqual(index, token.StartIndex);
				Assert.AreEqual(tokenTypes[i], token.Type());
				if (i < values.Length) {
					Assert.AreEqual(values[i] == (object)ERROR, error);
					if (!error)
						Assert.AreEqual(values[i], token.Value);
				}
				index += token.Length;
			}
			Assert.That(lexer.NextToken() == null);
		}
	}
}
