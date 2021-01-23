namespace LeMP.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Loyc.Ecs;
	using Loyc.MiniTest;
	using Loyc.Syntax;

	[TestFixture]
	public class LiteralTests : MacroTesterBase
	{
		[Test]
		public void TestOneDimensionalArrayLiterals()
		{
			var lemp = NewLemp(10, null);
			Test(LNode.Literal(new Int32[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }), 
			     lemp,        "new Int32[] { 1, 2, 3, 4, 5, 6, 7, 8,\n 9, 10 };", EcsLanguageService.Value);
			Test(LNode.Literal(new Int32[] { }), 
			     lemp,        "new Int32[] { };", EcsLanguageService.Value);
			Test(LNode.Literal(new SByte[] { -1, 2, 3, 4 }), 
			     lemp,        "new SByte[] { -1, 2, 3, 4 };", EcsLanguageService.Value);
			Test(LNode.Literal(new Byte[] { 0, 255 }), 
			     lemp,        "new Byte[] { 0, 255 };", EcsLanguageService.Value);
			Test(LNode.Literal(new Int16[] { -32768, 0, 32767 }),
			     lemp,        "new Int16[] { -32768, 0, 32767 };", EcsLanguageService.Value);
			Test(LNode.Literal(new Int16[] { -0x8000, 0x0, 0x7FFF }).SetBaseStyle(NodeStyle.HexLiteral),
			     lemp,        "new Int16[] { -0x8000, 0x0, 0x7FFF };", EcsLanguageService.Value);
			Test(LNode.Literal(new UInt16[] { 0, 1, 2, 3, 65535 }),
			     lemp,        "new UInt16[] { 0, 1, 2, 3, 65535 };", EcsLanguageService.Value);
			Test(LNode.Literal(new String[] { "hello", "!" }),
			     lemp,        "new String[] { \"hello\", \"!\" };", EcsLanguageService.Value);
			Test(LNode.Literal(new String[][] { new String[] { "hello" }, new String[] { "!" } }),
			     lemp,        "new String[][] { new String[] { \"hello\" }, new String[] { \"!\" } };", EcsLanguageService.Value);
		}

		[Test(Fails = "TODO")]
		public void TestTwoDimensionalArrayLiterals()
		{
			var lemp = NewLemp(10, null);
			Test(LNode.Literal(new String[,] { { "hello" }, { "!" } }),
			     lemp,        "new String[,] { { \"hello\" }, { \"!\" } };", EcsLanguageService.Value);
			Test(LNode.Literal(new byte[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 254, 255, 0 } }),
			     lemp,        "new byte[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 }, { 254, 255, 0 } };",
				 EcsLanguageService.Value);
		}
	}
}
