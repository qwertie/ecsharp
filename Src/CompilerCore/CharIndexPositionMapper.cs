using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Collections;
using Loyc.Essentials;
using Loyc.Utilities;
using NUnit.Framework;
using Loyc.Collections.Impl;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Abstract base class for an ICharSource that supports mapping from indices to
	/// SourcePos and back.
	/// </summary><remarks>
	/// This class's job is to keep track of the locations of line breaks in order
	/// to map from indices to SourcePos objects or vice versa. The derived class
	/// must implement the indexer and the length property. An implementation of
	/// Substring() is provided that calls the indexer for each character requested;
	/// override it to improve performance.
	/// </remarks>
	public abstract class CharIndexPositionMapper : ICharSource, IIndexPositionMapper, IEnumerable<char>
	{
		protected const char EOF = (char)0xFFFF;

		public CharIndexPositionMapper()
		{
			_lineOffsets.Add(0);
		}
		public CharIndexPositionMapper(SourcePos startingPos)
		{
			_lineOffsets.Add(0);
			_startingPos = startingPos;
		}

		public virtual string Substring(int startIndex, int length)
		{
			if (startIndex < 0)
				throw new ArgumentException("startIndex < 0");
			if (length < 0)
				throw new ArgumentException("length < 0");

			StringBuilder sb = new StringBuilder(length);
			for (int i = 0; i < length; i++) {
				int ch = TryGet(startIndex + i, EOF);
				if (ch == EOF)
					break;
				sb.Append((char)ch);
			}
			return sb.ToString();
		}
		
		public char this[int index] 
		{
			get {
				char value = '\0';
				if (!TryGet(index, ref value))
					throw new IndexOutOfRangeException();
				return value;
			}
		}
		public char TryGet(int index, ref bool fail)
		{
			char value = '\0';
			if (!TryGet(index, ref value))
				fail = true;
			return value;
		}
		public char TryGet(int index, char defaultValue)
		{
			char value = defaultValue;
			TryGet(index, ref value);
			return value;
		}
		public abstract bool TryGet(int index, ref char value);
		public abstract int Count { get; }

		Iterator<char> IIterable<char>.GetIterator()
		{
			return GetEnumerator().AsIterator();
		}

		// This code computes the line boundaries lazily. 
		// _lineOffsets contains the indices of the start of every line, so
		// this[_lineOffsets[2]] would be the first character of the third line.
		protected InternalList<int> _lineOffsets = InternalList<int>.Empty;
		protected bool _offsetsComplete = false;
		protected readonly SourcePos _startingPos = null;

		protected SourcePos NewSourcePos(int Line, int PosInLine)
		{
			if (_startingPos == null)
				return new SourcePos(string.Empty, Line, PosInLine);
			else if (Line <= 1)
				return new SourcePos(_startingPos.FileName, _startingPos.Line, _startingPos.PosInLine + PosInLine);
			else
				return new SourcePos(_startingPos.FileName, _startingPos.Line + Line-1, PosInLine);
		}
		public SourcePos IndexToLine(int index)
		{
			if (index < 0)
				return NewSourcePos(index + 1, 1);
			BufferUp(index);
			
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
			if (lineNo >= _lineOffsets.Count) {
				BufferUp(this.Count);
				if (lineNo >= _lineOffsets.Count)
					lineNo = _lineOffsets.Count - 1;
			}
			if (lineNo < 0)
				return -1;
			else
				return _lineOffsets[lineNo];
		}
		public int LineToIndex(SourcePos pos)
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
			BufferUp(Count);
			return _lineOffsets.Count;
		} }
		protected void BufferUp(int toIndex)
		{
			if (_offsetsComplete)
				return;
			int index = _lineOffsets[_lineOffsets.Count-1];
			for (;;) {
				if (index >= toIndex)
					return;
				if (!AdvanceAfterNextNewline(ref index))
					break;
				_lineOffsets.Add(index);
			}
		}
		protected bool AdvanceAfterNextNewline(ref int index)
		{
			for(;;) {
				char c = TryGet(index, EOF);
				if (c == '\uFFFF') {
					_offsetsComplete = true;
					return false;
				}
				bool isCr = c == '\r';
				if (isCr || c == '\n')
				{
					index++;
					if (isCr && TryGet(index, EOF) == '\n')
						index++;
					return true;
				}
				index++;
			}
		}

		#region IEnumerable Members
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<char> GetEnumerator()
		{
			int c;
			for (int i = 0; (c = TryGet(i, EOF)) != -1; i++)
				yield return (char)c;
		}
		#endregion
	}
	public abstract class CharIndexPositionMapperTests
	{
		protected const char EOF = (char)0xFFFF;
		protected abstract CharIndexPositionMapper CreateSource(string s);

		[Test] public void TestCharLookup()
		{
			// Prepare a long test string
			StringBuilder sb = new StringBuilder("Foo:\nCopyright \u00A9 2007\n");
			int Length = 3000;
			sb.Capacity = Length;
			while (sb.Length < Length)
				sb.Append((char)sb.Length);
			
			// Do some easy tests
			CharIndexPositionMapper cs = CreateSource(sb.ToString());
			Assert.AreEqual('F', cs.TryGet(0, EOF));
			Assert.AreEqual('o', cs.TryGet(1, EOF));
			Assert.AreEqual(':', cs.TryGet(3, EOF));
			Assert.AreEqual('\n', cs.TryGet(4, EOF));
			Assert.AreEqual(' ', cs.TryGet(16, EOF));
			Assert.AreEqual('\u00A9', cs.TryGet(15, EOF));
			Assert.AreEqual(":", cs.Substring(3, 1));
			Assert.AreEqual("Foo:", cs.Substring(0, 4));
			Assert.AreEqual("oo:\nC", cs.Substring(1, 5));
			Assert.AreEqual("\u00A9 2007", cs.Substring(15, 6));
			Assert.AreEqual("2007", cs.Substring(17, 4));
			Assert.AreEqual("", cs.Substring(0, 0));
			Assert.AreEqual("", cs.Substring(100, 0));
			
			// Stress test
			Random r = new Random(123);
			for (int i = 0; i < 1000; i++) {
				int len = r.Next(0, 50);
				int index = r.Next(0, Length - len);
				string expected = sb.ToString(index, len);
				Assert.AreEqual(expected, cs.Substring(index, len));
				if (len > 0)
					Assert.AreEqual(expected[0], cs.TryGet(index, EOF));
			}

			Assert.AreEqual("", cs.Substring(Length, 0));
			Assert.AreEqual('\uFFFF', cs.TryGet(Length, EOF));
		}

		[Test] public void TestOneLine()
		{
			CharIndexPositionMapper cs;
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
			CharIndexPositionMapper cs;
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