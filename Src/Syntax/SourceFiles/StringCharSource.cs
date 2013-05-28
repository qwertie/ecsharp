using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Loyc.Utilities;

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
		public override string Substring(int startIndex, int length) 
			{ return _text.Substring(startIndex, length); }

		// For now we'll put some line tracking here. In the end it may be
		// better to abstract it out of here (although the public functions will 
		// stay here, of course). Note! line/col numbers start at 0, as do
		// positions within a line.

		public override string ToString() { return _text; }
	}

	public class StringCharSourceFile : StringCharSource, ISourceFile
	{
		public StringCharSourceFile(string text, string filename)
			: base(text) { FileName = filename;  }
		public StringCharSourceFile(string text, SourcePos startingPos)
			: base(text, startingPos) { FileName = startingPos.FileName; }
		public string FileName { get; private set; }
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
