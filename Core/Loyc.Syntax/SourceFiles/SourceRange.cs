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
		public static readonly SourceRange Nowhere = new SourceRange(EmptySourceFile.Synthetic, -1, 0);

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

		public static SourceRange New<IndexRange>(ISourceFile source, IndexRange range) where IndexRange : IIndexRange
			=> new SourceRange(source, range.StartIndex, range.Length);
		public static explicit operator IndexRange(SourceRange r) => new IndexRange(r.StartIndex, r.Length);

		private ISourceFile _source;
		private int _startIndex;
		private int _length;

		/// <summary>Returns the source file of which this range is a part.
		/// If this <see cref="SourceRange"/> represents a "synthetic" syntax tree 
		/// (created programmatically), the source file may be an empty "dummy" 
		/// object such as <see cref="EmptySourceFile"/>. In this case, the 
		/// <see cref="Length"/> should be zero.
		/// </summary>
		public ISourceFile Source { [DebuggerStepThrough] get { return _source; } }
		public int StartIndex { [DebuggerStepThrough] get { return _startIndex; } }
		public int EndIndex { [DebuggerStepThrough] get { return _startIndex + _length; } }
		public int Length { [DebuggerStepThrough] get { return _length; } }

		public UString SourceText => SourceRangeExt.SourceText(this);

		public ILineColumnFile Start => SourceRangeExt.Start(this);
		public ILineColumnFile End => SourceRangeExt.End(this);

		/// <summary>If the Length is negative so StartIndex > EndIndex, this returns a 
		/// copy of the range with StartIndex and EndIndex swapped; otherwise the same 
		/// range is returned.</summary>
		/// <exception cref="OverflowException">An integer overflow occurred.</exception>
		public SourceRange Normalized()
		{
			return _length < 0 ? new SourceRange(_source, checked(_startIndex + _length), checked(-_length)) : this;
		}

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
		public static bool operator !=(SourceRange a, SourceRange b) => !(a == b);

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
			ILineColumnFile start = Start, end = End;
			return string.Format(Length > 1 ? "{0}({1},{2},{3},{4})" : "{0}({1},{2})",
				Source.FileName, start.Line, start.Column, end.Line, end.Column);
		}

		/// <summary>Assuming both ranges are normalized, returns the range of overlap between them.
		/// If the ranges are in different files or do not overlap, null is returned. If the two
		/// ranges share a border, this method returns a zero-Length range.</summary>
		public SourceRange? GetRangeOfOverlap(SourceRange other)
		{
			if (Source == other.Source) {
				var overlap = ((IndexRange)this).GetRangeOfOverlap((IndexRange)other);
				if (overlap.Length >= 0)
					return New(Source, overlap);
			}
			return null;
		}
		/// <summary>Returns true if, assuming both ranges are normalized, the two regions 
		/// share at least one common character.</summary>
		/// <remarks>Note: this returns false if either of the ranges has a Length of zero 
		/// and is at the boundary of the other range.</remarks>
		public bool Overlaps(SourceRange other)
		{
			return Source == other.Source && EndIndex > other.StartIndex && StartIndex < other.EndIndex;
		}
		public bool Contains(SourceRange inner)
		{
			return Source == inner.Source && StartIndex <= inner.StartIndex && EndIndex >= inner.EndIndex;
		}
	}
}
