using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Collections;

namespace Loyc.Syntax
{
	public class StringCharSource : CharIndexPositionMapper
	{
		public StringCharSource(string text) 
			{ _text = text; }
		public StringCharSource(string text, SourcePos startingPos) : base(startingPos)
			{ _text = text; }

		protected readonly string _text;
		public string Text { get { return _text; } }

		public override char TryGet(int index, ref bool fail)
		{
			if ((uint)index < (uint)_text.Length)
				return _text[index];
			fail = true;
			return (char)0xFFFF;
		}
		public override int Count { get { return _text.Length; } }
		public override UString Substring(int startIndex, int length) 
			{ return _text.USlice(startIndex, length); }

		// For now we'll put some line tracking here. In the end it may be
		// better to abstract it out of here (although the public functions will 
		// stay here, of course). Note! line/col numbers start at 0, as do
		// positions within a line.

		public override string ToString() { return _text; }
	}

	public class StringCharSourceFile : StringCharSource, ISourceFile
	{
		public StringCharSourceFile(string text, string filename)
			: base(text, new SourcePos(filename, 1, 0)) { }
		public StringCharSourceFile(string text, SourcePos startingPos)
			: base(text, startingPos) { }

		public string FileName { get { return _startingPos.FileName; } }
	}

	[TestFixture]
	public class StringCharSourceTests : CharIndexPositionMapperTests
	{
		protected override CharIndexPositionMapper CreateSource(string s)
		{
			return new StringCharSource(s);
		}
	}
}
