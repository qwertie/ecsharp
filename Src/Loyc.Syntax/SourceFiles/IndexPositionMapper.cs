using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Collections;
using Loyc.Utilities;
using Loyc.MiniTest;
using Loyc.Collections.Impl;
using System.Diagnostics;

namespace Loyc.Syntax
{
	/// <summary>
	/// Helper class for mapping from indexes to SourcePos and back.
	/// </summary><remarks>
	/// This class's job is to keep track of the locations of line breaks in order
	/// to map from indices to SourcePos objects or vice versa. Converting indexes 
	/// to SourcePos is commonly needed for error reporting; lexers, parsers and 
	/// code analyzers typically use indexes (simple integers) internally, but must
	/// convert to SourcePos in order to communicate with the end user. Occasionally
	/// one may wish to convert in the reverse direction also (SourcePos to index).
	/// <para/>
	/// Line breaks themselves are classified as being at the end of each line.
	/// So if the file is "Bob\nJoe", <c>IndexToLine(3).Line == 1</c>, not 2.
	/// <para/>
	/// The outputs are immutable and this class assumes the input file never 
	/// changes. However, this class is not entirly multi-thread-safe until the 
	/// entire input file or string has been scanned, since the list of line breaks
	/// is built on-demand, without locking.
	/// </remarks>
	public class IndexPositionMapper : IIndexPositionMapper
	{
		protected IListSource<char> _source;

		/// <summary>Initializes CharIndexPositionMapper.</summary>
		/// <param name="source">An immutable list of characters.</param>
		/// <param name="startingPos">Optional. The first character of <c>source</c> 
		/// will be considered to have the file name and line number specified by 
		/// this object. If this is null, IndexToLine() will return a blank file 
		/// name ("").</param>
		public IndexPositionMapper(IListSource<char> source, SourcePos startingPos = null)
		{
			_source = source;
			_lineOffsets.Add(0);
			_startingPos = startingPos;
		}
		public IndexPositionMapper(IListSource<char> source, string fileName) : this(source, new SourcePos(fileName, 1, 1)) {}

		// This code computes the line boundaries lazily. 
		// _lineOffsets contains the indices of the start of every line, so
		// this[_lineOffsets[2]] would be the first character of the third line.
		protected InternalList<int> _lineOffsets = InternalList<int>.Empty;
		protected bool _offsetsComplete = false;
		protected readonly SourcePos _startingPos = null;

		public string FileName
		{
			get { return _startingPos == null ? null : _startingPos.FileName; }
		}

		protected SourcePos NewSourcePos(int Line, int PosInLine)
		{
			if (_startingPos == null)
				return new SourcePos(string.Empty, Line, PosInLine);
			else if (Line <= 1)
				return new SourcePos(_startingPos.FileName, _startingPos.Line, _startingPos.PosInLine-1 + PosInLine);
			else
				return new SourcePos(_startingPos.FileName, _startingPos.Line + Line-1, PosInLine);
		}

		public SourcePos IndexToLine(int index)
		{
			if (index < 0)
				return NewSourcePos(index + 1, 1);
			ReadUntilAfter(index);
			
			// Binary search
			int line = _lineOffsets.BinarySearch(index);
			if (line < 0)
				line = ~line - 1;
			
			// Create LinePos using a one-based line number and position
			return NewSourcePos(line + 1, index - _lineOffsets[line] + 1);
		}

		public int LineToIndex(int lineNo)
		{
			// Remove _startingPos bias and convert to zero-based index
			if (_startingPos != null)
				lineNo -= _startingPos.Line;
			else
				lineNo--; // Convert to zero-based index
			while (_lineOffsets.Count < lineNo && !_offsetsComplete)
				ReadNextLine(_lineOffsets.Last);
			if (lineNo < 0)
				return -1;
			else
				return _lineOffsets[lineNo];
		}
		public int LineToIndex(LineAndPos pos)
		{
			int lineIndex = LineToIndex(pos.Line);
			if (pos.PosInLine > 0)
				lineIndex += pos.PosInLine - 1;
			if (_startingPos != null && pos.Line == _startingPos.Line)
				return lineIndex + (_startingPos.PosInLine - 1);
			else
				return lineIndex;
		}
		public int LineCount { get {
			ReadUntilAfter(_source.Count);
			return _lineOffsets.Count;
		} }
		protected void ReadUntilAfter(int toIndex)
		{
			int index = _lineOffsets.Last;
			while (index < toIndex && !_offsetsComplete)
				index = ReadNextLine(index);
		}
		private int ReadNextLine(int index)
		{
			if (AdvanceAfterNextNewline(ref index))
				_lineOffsets.Add(index);
			else
				_offsetsComplete = true;
			return index;
		}
		protected bool AdvanceAfterNextNewline(ref int index)
		{
			for(;;) {
				bool fail;
				char c = _source.TryGet(index, out fail);
				if (fail)
					return false;
				if (c == '\r' || c == '\n')
				{
					index++;
					if (c == '\r' && _source.TryGet(index, out fail) == '\n')
						index++;
					return true;
				}
				index++;
			}
		}
	}

	public class CharIndexPositionMapperTests
	{
		protected const char EOF = (char)0xFFFF;
		protected IndexPositionMapper CreateSource(string s) { return new IndexPositionMapper((UString)s); }

		[Test] public void TestOneLine()
		{
			IndexPositionMapper cs;
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
			IndexPositionMapper cs;
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