using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;

namespace Loyc.Syntax
{
	public class IndexPositionMapperTests
	{
		protected const char EOF = (char)0xFFFF;
		protected IndexPositionMapper<UString> CreateSource(string s) { return new IndexPositionMapper<UString>((UString)s); }

		[Test] public void TestOneLine()
		{
			IndexPositionMapper<UString> cs;
			cs = CreateSource("One line");
			Assert.AreEqual(new SourcePos("", 1, 6), cs.IndexToLine(5));
			Assert.AreEqual(new SourcePos("", 1, 13), cs.IndexToLine(12));
			Assert.AreEqual(0, cs.LineToIndex(1));
			Assert.AreEqual(5, cs.LineToIndex(new SourcePos("", 1, 6)));
			Assert.AreEqual(1, cs.LineCount);

			cs = CreateSource("");
			Assert.AreEqual(1, cs.LineCount);
			Assert.AreEqual(13, cs.IndexToLine(12).PosInLine);
		}

		[Test] public void TestMultiLine()
		{
			IndexPositionMapper<UString> cs;
			cs = CreateSource("Line 1\r\nLine 2\n\nLine 4\n\rLine 6");
			Assert.AreEqual(new SourcePos("", 1, 8), cs.IndexToLine(7));
			Assert.AreEqual(new SourcePos("", 2, 1), cs.IndexToLine(8));
			Assert.AreEqual(new SourcePos("", 4, 5), cs.IndexToLine(20));
			Assert.AreEqual(new SourcePos("", 5, 1), cs.IndexToLine(23));
			Assert.AreEqual(new SourcePos("", 6, 7), cs.IndexToLine(30));
			Assert.AreEqual(6, cs.LineCount);

			// Same input, but do things a little differently
			cs = CreateSource("Line 1\r\nLine 2\n\nLine 4\n\rLine 6");
			Assert.AreEqual(new SourcePos("", 3, 1), cs.IndexToLine(15));
			Assert.AreEqual(8, cs.LineToIndex(2));
			Assert.AreEqual(16, cs.LineToIndex(4));
			Assert.AreEqual(new SourcePos("", 4, 5), cs.IndexToLine(20));
			Assert.AreEqual(6, cs.LineCount);
			Assert.AreEqual(new SourcePos("", 6, 7), cs.IndexToLine(30));
		}
	};
}
