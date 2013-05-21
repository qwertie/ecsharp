using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc;
using System.Diagnostics;
using S = ecs.CodeSymbols;

namespace Ecs.Parser
{
	using TT = TokenType;

	[TestFixture]
	class EcsLexerTests
	{
		[DebuggerStepThrough] static Symbol _(string s) { return GSymbol.Get(s); }
		[DebuggerStepThrough] static T[] A<T>(params T[] list) { return list; }

		[Test]
		public void Basics()
		{
			Case(@"hello, world!",
				A(TT.Id, TT.Comma, TT.Spaces, TT.Id, TT.Not), 
				_("hello"), _("#,"), null, _("world"), _("#!"));
			Case(@"this is""just""1 lexer test '!'",
				A(TT.@this, TT.Spaces, TT.@is, TT.DQString, TT.Number, TT.Spaces, TT.Id, TT.Spaces, TT.Id, TT.Spaces, TT.SQString),
				S.This, null, S.Is, "just", 1, null, _("lexer"), null, _("test"), null, '!');
			Case(@"12:30", A(TT.Number, TT.Colon, TT.Number), 12, _("#:"), 30);
			Case(@"c+='0'", A(TT.Id, TT.AddSet, TT.SQString), _("c"), _("#+="), '0');
			Case("// hello\n\r\n\r/* world */",
				A(TT.SLComment, TT.Newline, TT.Newline, TT.Newline, TT.MLComment));
			Case(@"{}[]()", A(TT.LBrace, TT.RBrace, TT.LBrack, TT.RBrack, TT.LParen, TT.RParen));
			Case(@"finally@@{`boom!` $bam;}", A(TT.@finally, TT.At, TT.At, TT.LBrace, TT.BQString, TT.Spaces, TT.Symbol, TT.Semicolon, TT.RBrace),
				S.Finally, null, null, null, _("boom!"), null, _("bam"), _("#;"), null);
		}

		[Test]
		public void TestIdentifiers()
		{
			Case("abc_123/_0",   A(TT.Id, TT.Div, TT.Id),            _("abc_123"), _("#/"), _("_0"));
			Case("is@is",        A(TT.@is, TT.Id),                   S.Is, _("is"));
			Case("\u0041\U00000062\u0063", A(TT.Id),                 _("Abc"));
			Case("No#error",     A(TT.Id, TT.Id),                    _("No"), _("#error"));
			Case("@#error.",     A(TT.Id, TT.Dot),                   _("#error"), _("#."));
			Case("#@food:@yum",  A(TT.Id, TT.Colon, TT.Id),          _("#food"), _("#:"), _("yum"));
			Case("#()$",         A(TT.Id, TT.LParen, TT.RParen, TT.Id), _("#"), null, null, _("$"));
			Case("#$#==>#??.",   A(TT.Id, TT.Id, TT.Id),             _("#$"), _("#==>"), _("#??."));
			Case("#>>#>>=#<<",   A(TT.Id, TT.Id, TT.Id),             _("#>>"), _("#>>="), _("#<<"));
			Case(@"@0@`@\n`",    A(TT.Id, TT.Id),                    _("0"), _(@"@\n"));
			Case("won't prime'", A(TT.Id, TT.Spaces, TT.Id),         _("won't"), null, _("prime'"));
		}

		[Test]
		public void TestStrings()
		{
			Case(@"`Testing`""Testing""'!'", A(TT.BQString, TT.DQString, TT.SQString), _("Testing"), "Testing", '!');
			Case(@"`\a\b\f\v\`\'\""`""\a\b\f\v\`\'\""""'\0'", A(TT.BQString, TT.DQString, TT.SQString),
				_("\a\b\f\v`\'\""), "\a\b\f\v`\'\"", '\0');
		}

		[Test]
		public void TestShebang()
		{
			Case("#!/bin/sh\r\n// that's called a shebang!",
				A(TT.Shebang, TT.SLComment));
			Case(".#!/bin/sh",
				A(TT.Dot, TT.Id, TT.Div, TT.Id, TT.Div, TT.Id),
				_("#."), _("#!"), _("#/"), _("bin"), _("#/"), _("sh"));
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
			Case("0b1000_0000_1000_0001_1111_1111==0x8081FF", A(TT.Number, TT.Eq, TT.Number), 0x8081FF, _("#=="), 0x8081FF);
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
			Case("0x0.0", A(TT.Number), 0.0);
			Case("0xF.8", A(TT.Number), 15.5);
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
			Case(@"$public$is$A$`common\\word`$around$her\u0065",
				A(TT.Symbol, TT.Symbol, TT.Symbol, TT.Symbol, TT.Symbol, TT.Symbol),
				_("public"), _("is"), _("A"), _(@"common\word"), _("around"), _("here"));
		}

		[Test]
		public void TestPreprocessor()
		{
			Case("/**/#if  ", A(TT.MLComment, TT.Id, TT.Spaces), null, _("#if"), null);
			Case("\t\t#if  ", A(TT.Spaces, TT.PPif, TT.Spaces), null, _("#if"), null);
			Case("#if Foo\n#elif Bar\n#else//otherwise\n#endif//Foo", 
				A(TT.PPif, TT.Spaces, TT.Id, TT.Newline, TT.PPelif, TT.Spaces, TT.Id, TT.Newline, TT.PPelse, TT.SLComment, TT.Newline, TT.PPendif, TT.SLComment),
				_("#if"), null, _("Foo"), null, _("#elif"), null, _("Bar"), null, _("#else"), null, null, _("#endif"), null);
			Case("#define Foo\n#undef Foo",
				A(TT.PPdefine, TT.Spaces, TT.Id, TT.Newline, TT.PPundef, TT.Spaces, TT.Id),
				_("#define"), null, _("Foo"), null, _("#undef"), null, _("Foo"));
			Case("#warning Your life is going nowhere.\n#error--sorry.\n#note EC# only.",
				A(TT.PPwarning, TT.Newline, TT.PPerror, TT.Newline, TT.PPnote),
				" Your life is going nowhere.", null, "--sorry.", null, " EC# only.");
			Case("#region The netherworld\n#endregion",
				A(TT.PPregion, TT.Newline, TT.PPendregion),
				" The netherworld", null, _("#endregion"));
			// Exact match or it's Id
			Case("#defined\n#defin\n#regio\n#endregi", 
				A(TT.Id, TT.Newline, TT.Id, TT.Newline, TT.Id, TT.Newline, TT.Id),
				_("#defined"), null, _("#defin"), null, _("#regio"), null, _("#endregi"));
		}

		const string ERROR = "ERROR";

		[Test]
		public void TestErrors()
		{
			Case("x=\"Hello\n",     A(TT.Id, TT.Set, TT.DQString, TT.Newline), _("x"), _("#="), ERROR, null);
			Case("'\n'o''pq\n?''",  A(TT.SQString, TT.Newline, TT.SQString, TT.SQString, TT.Newline, TT.QuestionMark, TT.SQString),
			                        ERROR, null, 'o', ERROR, null, _("#?"), ERROR);
			Case("'abc'",           A(TT.SQString), ERROR);
			Case("0x!0b",           A(TT.Number, TT.Not, TT.Number), ERROR, _("#!"), ERROR);
			Case("`weird\nnewline", A(TT.BQString, TT.Newline, TT.Id), ERROR, null, _("newline"));
			Case("0xFF_0000_0000U", A(TT.Number), ERROR);
			Case("0xFFFF_FFFF_0000_0000L", A(TT.Number), ERROR);
			Case("0x1_FFFF_FFFF_0000_0000", A(TT.Number), ERROR);
		}

		[Test]
		public void TestKeywords()
		{
			Case("public static int @default=default(stackalloc)as this",
				A(TT.AttrKeyword, TT.Spaces, TT.AttrKeyword, TT.Spaces, TT.TypeKeyword, TT.Spaces, TT.Id, TT.Set, TT.@default, TT.LParen, TT.@stackalloc, TT.RParen, TT.@as, TT.Spaces, TT.@this),
				S.Public, null, S.Static, null, S.Int32, null, _("default"), _("#="), S.Default, null, S.StackAlloc, null, S.As, null, S.This);
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
				Assert.AreEqual(tokenTypes[i], token.Type);
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
