using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Utilities;

namespace Loyc.Syntax
{
	/// <summary>
	/// Holds a reference to a source file (ISourceFile&lt;char&gt;) and the
	/// beginning and end indices of a range in that file.
	/// </summary>
	[DebuggerDisplay("{_source.FileName}[{_startIndex}, Length {_length}]")]
	public struct SourceRange : IEquatable<SourceRange>, ISourceRange
	{
		public static readonly SourceRange Nowhere = new SourceRange(EmptySourceFile.Default, -1, 0);

		public SourceRange(ISourceFile source, int beginIndex = -1, int length = 0)
		{
			_source = source;
			_startIndex = beginIndex;
			_length = length;
		}
		public SourceRange(ISourceRange range)
		{
			if (range is SourceRange range2)
				this = range2;
			else {
				_source = range.Source;
				_startIndex = range.StartIndex;
				_length = range.Length;
			}
		}
		//public SourceRange(ISourceFile source, Lexing.Token token)
		//{
		//	_source = source;
		//	_startIndex = token.StartIndex;
		//	_length = token.Length;
		//}

		private ISourceFile _source;
		private int _startIndex;
		private int _length;

		public ISourceFile Source { [DebuggerStepThrough] get { return _source; } }
		public int StartIndex { [DebuggerStepThrough] get { return _startIndex; } }
		public int EndIndex { [DebuggerStepThrough] get { return _startIndex + System.Math.Max(_length, 0); } }
		public int Length { [DebuggerStepThrough] get { return _length; } }

		public UString SourceText => SourceRangeExt.SourceText(this);

		public ILineColumnFile Start => SourceRangeExt.Start(this);
		public ILineColumnFile End => SourceRangeExt.End(this);

		[Obsolete("I never ended up using this. Anyone else using it?")]
		public char this[int subIndex]
		{
			get {
				Debug.Assert((uint)subIndex < (uint)_length);
				return _source.Text.TryGet(_startIndex + subIndex, '\uFFFF');
			}
		}

		public static bool operator ==(SourceRange a, SourceRange b)
		{
			return a._source == b._source && a._startIndex == b._startIndex && a._length == b._length;
		}
		public static bool operator !=(SourceRange a, SourceRange b) { return !(a == b); }

		public bool Equals(SourceRange other) => this == other;
		public override bool Equals(object obj)
		{
			return obj is SourceRange && (SourceRange)obj == this;
		}
		public override int GetHashCode()
		{
			int hc = 0;
			if (_source != null) hc = _source.GetHashCode();
			return hc ^ _startIndex ^ (_length << 4);
		}
		public override string ToString()
		{
			if (StartIndex <= 0)
				return string.Format("{0}({1}..{2})", Source.FileName, StartIndex, EndIndex);
			SourcePos start = Start, end = End;
			return string.Format(Length > 1 ? "{0}({1},{2},{3},{4})" : "{0}({1},{2})",
				Source.FileName, start.Line, start.PosInLine, end.Line, end.PosInLine);
		}

		public bool Contains(SourceRange inner)
		{
			return Source == inner.Source && StartIndex <= inner.StartIndex && EndIndex >= inner.EndIndex;
		}
	}
}
