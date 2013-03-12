using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Loyc;
using System.Diagnostics;

namespace Ecs.Parser
{
	[TestFixture]
	class EcsLexerTests : EcsLexerSymbols
	{
		static Symbol S(string s) { return GSymbol.Get(s); }
		static T[] A<T>(params T[] list) { return list; }

		[Test]
		public void Basics()
		{
			Case(@"hello, world!",
				A(Id, Comma, Spaces, Id, Operator), 
				S("hello"), S("#,"), null, S("world"), S("#!"));
			Case(@"this is""just""1 lexer test '!'",
				A(@this, Spaces, @is, DQString, Number, Spaces, Id, Spaces, Id, Spaces, SQString),
				@this, null, @is, "just", 1, null, S("lexer"), null, S("test"), null, '!');
			Case(@"12:30", A(Number, Colon, Number), 12, S("#:"), 30);
			Case(@"c+='0'", A(Id, Operator, SQString), S("c"), S("#+="), '0');
			Case("// hello\n\r\n\r/* world */",
				A(SLComment, Newline, Newline, Newline, MLComment));
			Case(@"{}[]()", A(LBrace, RBrace, LBrack, RBrack, LParen, RParen));
			Case(@"finally@@{`boom!` $bam;}", A(@finally, LCodeQuoteS, BQString, Spaces, Symbol, Semicolon, RBrace),
				S("finally"), null, S("boom!"), null, S("bam"), S("#;"), null);
		}

		[Test]
		public void TestIdentifiers()
		{
			Case("abc_123/_0",   A(Id, Operator, Id),       S("abc_123"), S("#/"), S("_0"));
			Case("is@is",        A(@is, Id),                @is, S("is"));
			Case("\u0041\U00000062\u0063", A(Id),           S("Abc"));
			Case("No#error",     A(Id, Id),                 S("No"), S("#error"));
			Case("@#error.",     A(Id, Operator),           S("#error"), S("#."));
			Case("#@food:@yum",  A(Id, Colon, Id),          S("#food"), S("#:"), S("yum"));
			Case("#()$",         A(Id, LParen, RParen, Id), S("#"), null, null, S("$"));
			Case("#$#==>#??.",   A(Id, Id, Id),             S("#$"), S("#==>"), S("#??."));
			Case("#>>#>>=#<<",   A(Id, Id, Id),             S("#>>"), S("#>>="), S("#<<"));
			Case(@"@0@`@\n`",    A(Id, Id),                 S("0"), S(@"@\n"));
			Case("won't prime'", A(Id, Spaces, Id),         S("won't"), null, S("prime'"));
		}

		[Test]
		public void TestStrings()
		{
			Case(@"`Testing`""Testing""'!'", A(BQString, DQString, SQString), S("Testing"), "Testing", '!');
			Case(@"`\a\b\f\v\`\'\""`""\a\b\f\v\`\'\""""'\0'", A(BQString, DQString, SQString),
				S("\a\b\f\v`\'\""), "\a\b\f\v`\'\"", '\0');
		}

		[Test]
		public void TestShebang()
		{
			Case("#!/bin/sh\r\n// that's called a shebang!",
				A(Shebang, SLComment));
			Case(".#!/bin/sh",
				A(Operator, Id, Operator, Id, Operator, Id),
				S("#."), S("#!"), S("#/"), S("bin"), S("#/"), S("sh"));
		}

		[Test]
		public void TestIntegers()
		{
			Case("9", A(Number), 9);
			Case("1337", A(Number), 1337);
			Case("-1", A(Number), -1);
			Case("9111222U", A(Number), 9111222U);
			Case("0L", A(Number), 0L);
			Case("-9111222L", A(Number), -9111222L);
			Case("-1U", A(Operator, Number), S("#-"), 1U);
			Case("9_111_222", A(Number), 9111222);
			Case("9_111_222_333", A(Number), 9111222333);
			Case("4_111_222_333", A(Number), 4111222333);
			Case("4_111_222_333U", A(Number), 4111222333U);
			Case("9_111_222_333_444_555", A(Number), 9111222333444555);
			Case("9_111_222_333_444_555L", A(Number), 9111222333444555L);
			Case("9_111_222_333_444_555UL", A(Number), 9111222333444555UL);
			Case("0x9+0x0A=0x0000_0000_13", A(Number, Operator, Number, Operator, Number), 0x9, S("#+"), 0x0A, S("#="), 0x13);
			Case("0b1000_0000_1000_0001_1111_1111=0x8081FF", A(Number, Operator, Number), 0x8081FF, S("#="), 0x8081FF);
			Case("0b11L0b10000000_10000001_10010010_11111111U", A(Number, Number), 3L, 0x808192FFU);
			Case("0b1111_10000000_10000001_10010010_11111111", A(Number), 0x0F808192FF);
		}

		[Test]
		public void TestFloats()
		{
			Case("0.0", A(Number), 0.0);
			Case("0.1", A(Number), 0.1);
            Case("25d25f25m", A(Number, Number, Number), 25d,25f,25m);
			Case("0.25d", A(Number), 0.25d);
			Case("0.25f", A(Number), 0.25f);
			Case("0.25m", A(Number), 0.25m);
			Case("0.25e2", A(Number), 0.25e2);
			Case("10e-20", A(Number), 10e-20);
			Case("0.3e+2d", A(Number), 0.3e+2d);
			Case("0.3e+2f", A(Number), 0.3e+2f);
			Case("0.3e+2m", A(Number), 0.3e+2m);
			Case("1234567890123456789012345678901234567890d", A(Number), 1234567890123456789012345678901234567890d);
			Case("123456789012345678901234567890.1234567890123456789012345678901234567890f", A(Number), 
			      123456789012345678901234567890.1234567890123456789012345678901234567890f);
			Case(".5e+2.5e+2f.5m", A(Number, Number, Number), .5e+2, .5e+2f, .5m);
			Case("Y.5", A(Id, Number), S("Y"), .5);
			Case("0.1.5", A(Number, Number), 0.1, .5);
			Case("5.ToString", A(Number, Operator, Id), 5, S("#."), S("ToString"));
		}
		[Test]
		public void TestHexAndBinFloats()
		{
			Case("0x0.0", A(Number), 0.0);
			Case("0xF.8", A(Number), 15.5);
			Case("0xF.8p+1;0xF.8p1", A(Number, Semicolon, Number), 31, 31);
			Case("0xA.8p-1", A(Number), 5.25);
			Case("0b101.01", A(Number), 5.25);
			Case("0b101.01p0f", A(Number), 5.25f);
			Case("0b101.01p2", A(Number), 21.0);
			Case("0b1111_1111.1111_1111p+8", A(Number), (double)0xFFFF);
			Case("0b.1p-2", A(Number), 0.125);
			Case("0b.1p-2f", A(Number), 0.125f);
		}

		[Test]
		public void TestSymbols()
		{
			Case(@"$public$is$A$`common\\word`$around$her\u0065",
				A(Symbol, Symbol, Symbol, Symbol, Symbol, Symbol),
				S("public"), S("is"), S("A"), S(@"common\word"), S("around"), S("here"));
		}

		[Test]
		public void TestPreprocessor()
		{
			Case("/**/#if  ", A(MLComment, Id, Spaces), null, S("#if"), null);
			Case("\t\t#if  ", A(Spaces, PPif, Spaces), null, PPif, null);
			Case("#if Foo\n#elif Bar\n#else//otherwise\n#endif//Foo", 
				A(PPif, Spaces, Id, Newline, PPelif, Spaces, Id, Newline, PPelse, SLComment, Newline, PPendif, SLComment),
				PPif, null, S("Foo"), null, PPelif, null, S("Bar"), null, PPelse, null, null, PPendif, null);
			Case("#define Foo\n#undef Foo",
				A(PPdefine, Spaces, Id, Newline, PPundef, Spaces, Id),
				PPdefine, null, S("Foo"), null, PPundef, null, S("Foo"));
			Case("#warning Your life is going nowhere.\n#error--sorry.\n#note EC# only.",
				A(PPwarning, Newline, PPerror, Newline, PPnote),
				" Your life is going nowhere.", null, "--sorry.", null, " EC# only.");
			Case("#region The netherworld\n#endregion",
				A(PPregion, Newline, PPendregion),
				" The netherworld", null, PPendregion);
			// Exact match or it's Id
			Case("#defined\n#defin\n#regio\n#endregi", 
				A(Id, Newline, Id, Newline, Id, Newline, Id),
				S("#defined"), null, S("#defin"), null, S("#regio"), null, S("#endregi"));
		}

		const string ERROR = "ERROR";

		[Test]
		public void TestErrors()
		{
			Case("x=\"Hello\n",     A(Id, Operator, DQString, Newline), S("x"), S("#="), ERROR, null);
			Case("'\n'o''pq\n?''",  A(SQString, Newline, SQString, SQString, Newline, Operator, SQString),
			                        ERROR, null, 'o', ERROR, null, S("#?"), ERROR);
			Case("'abc'",           A(SQString), ERROR);
			Case("0x!0b",           A(Number, Operator, Number), ERROR, S("#!"), ERROR);
			Case("`weird\nnewline", A(BQString, Newline, Id), ERROR, null, S("newline"));
			Case("0xFF_0000_0000U", A(Number), ERROR);
			Case("0xFFFF_FFFF_0000_0000L", A(Number), ERROR);
			Case("0x1_FFFF_FFFF_0000_0000", A(Number), ERROR);
		}

		void Case(string input, Symbol[] tokenTypes, params object[] values)
		{
			Debug.Assert(values.Length <= tokenTypes.Length);
			
			bool error = false;
			var lexer = new EcsLexer(input, (_, msg) => { Trace.WriteLine(msg); error = true; });

			int index = 0;
			for (int i = 0; i < tokenTypes.Length; i++)
			{
				error = false;
				Token token = lexer.ParseNextToken().Value;
				Assert.AreEqual(index, token.StartIndex);
				Assert.AreEqual(tokenTypes[i], token.Type);
				if (i < values.Length) {
					Assert.AreEqual(values[i] == (object)ERROR, error);
					if (!error)
						Assert.AreEqual(values[i], token.Value);
				}
				index += token.Length;
			}
			Assert.That(lexer.ParseNextToken() == null);
		}
	}
}
