using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Loyc.Collections;
using Loyc.Math;
using Loyc.Syntax;
using Microsoft.VisualStudio.Text;

namespace Loyc.VisualStudio
{
	/// <summary>Converts a Visual Studio ITextSnapshot to Loyc's ISourceFile interface 
	/// (and ICharSource, the value of the ISourceFile.Text property).</summary>
	public class TextSnapshotAsSourceFile : ICharSource, ISourceFile
	{
		public TextSnapshotAsSourceFile(ITextSnapshot ss) { TextSnapshot = ss; }

		ITextSnapshot _ss;
		public ITextSnapshot TextSnapshot
		{
			get { return _ss; }
			set { _ss = value; }
		}

		public Loyc.UString Substring(int startIndex, int length)
		{
			return SubstringCore(startIndex, length);
		}
		public string SubstringCore(int startIndex, int length)
		{
			int c = Count;
			if (startIndex >= c) return "";
			if (length > c - startIndex)
				length = c - startIndex;
			return _ss.GetText(startIndex, length);
		}
		IRange<char> IListSource<char>.Slice(int start, int count) { return Slice(start, count); }
		public StringSlice Slice(int start, int count = 2147483647)
		{
			string s = SubstringCore(start, count);
			return new StringSlice(s, 0, s.Length);
		}
		public char TryGet(int index, out bool fail)
		{
			fail = false;
			if ((uint)index < (uint)Count)
				return _ss[index];
			fail = true;
			return '\0';
		}
		public char this[int index]
		{
			get { return _ss[index]; }
		}
		public int Count
		{
			get { return _ss.Length; }
		}
		public IEnumerator<char> GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
				yield return this[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public SourcePos IndexToLine(int index)
		{
			var line = _ss.GetLineFromPosition(index);
			return new SourcePos("", line.LineNumber + 1, index - line.Start.Position + 1);
		}

		public int LineToIndex(LineAndCol pos)
		{
			if (pos.Line <= 0)
				return -1;
			if (pos.Line >= _ss.LineCount)
				return _ss.Length;
			var line = _ss.GetLineFromLineNumber(pos.Line);
			return line.Start.Position + Range.PutInRange(pos.PosInLine - 1, 0, line.Length + 1);
		}
		public int LineToIndex(int lineNo)
		{
			if (lineNo <= 0)
				return -1;
			if (lineNo >= _ss.LineCount)
				return _ss.Length;
			var line = _ss.GetLineFromLineNumber(lineNo);
			return line.Start.Position;
		}

		string IIndexToLine.FileName
		{
			get { return ""; }
		}
		ICharSource ISourceFile.Text
		{
			get { return this; }
		}
	}
}
