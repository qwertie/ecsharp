using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Collections;
using Loyc.Utilities;

namespace Loyc.Syntax
{
	/// <summary>
	/// Holds a reference to a source file (ISourceFile&lt;char&gt;) and the
	/// beginning and end indices of a range in that file.
	/// </summary>
	public struct SourceRange
	{
		public static readonly SourceRange Nowhere = new SourceRange(EmptySourceFile.Default, -1, 0);

		public SourceRange(ISourceFile source, int beginIndex = -1, int length = -1)
		{
			_source = source;
			_startIndex = beginIndex;
			_length = length;
		}

		private ISourceFile _source;
		private int _startIndex;
		private int _length;

		public ISourceFile Source { [DebuggerStepThrough] get { return _source; } }
		public int StartIndex     { [DebuggerStepThrough] get { return _startIndex; } }
		public int EndIndex       { [DebuggerStepThrough] get { return _startIndex + System.Math.Max(_length, 0); } }
		public int Length         { [DebuggerStepThrough] get { return _length; } }

		public SourcePos Begin
		{
			get { 
				if (Source == null)
					return SourcePos.Nowhere;
				return Source.IndexToLine(StartIndex);
			}
		}
		public SourcePos End
		{
			get { 
				if (Source == null)
					return SourcePos.Nowhere;
				return Source.IndexToLine(EndIndex);
			}
		}

		public char this[int subIndex]
		{
			get {
				Debug.Assert((uint)subIndex < (uint)_length);
				return _source.TryGet(_startIndex + subIndex, '\uFFFF');
			}
		}

		public static bool operator ==(SourceRange a, SourceRange b)
		{
			return a._source == b._source && a._startIndex == b._startIndex && a._length == b._length;
		}
		public static bool operator !=(SourceRange a, SourceRange b) { return !(a == b); }

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
			return string.Format("{0}[{1}+{2}]", _source.FileName, _startIndex, _length);
		}
	}

#if false
	/// <summary>SourceFileRange specifies a range of positions in a source file.
	/// </summary><remarks>
	/// A SourceRange is used to represent the part of a compilation unit that
	/// represents a particular token or AST node.
	/// </remarks>
	public struct SourceRange
	{
		public SourceRange(ISourceFile charSource, int startIndex, int endIndex)
		{
			if ((startIndex < 0 || endIndex < startIndex) && charSource != null)
				throw new ArgumentException("SourceRange.StartIndex or SourceRange.Length can't be negative");
			Source = charSource;
			StartIndex = startIndex;
			EndIndex = endIndex;
		}

		public static readonly SourceRange Empty = new SourceRange();
		
		/// <summary>The source in which the range is located. If Source is
		/// null, StartLength/EndIndex/Length must be ignored.</summary>
		public ISourceFile Source;

		/// <summary>Starting location in the range.</summary>
		public int StartIndex;
		
		/// <summary>Ending location in the range.</summary>
		public int EndIndex;

		/// <summary>Length of the range.</summary>
		public int Length { get { return EndIndex - StartIndex; } }

		public ILanguageStyle Language { get { return Source == null ? null : Source.Language; } }

		public string SourceText {
			get {
				if (Source == null)
					return null;
				else
					return Source.Substring(StartIndex, Length);
			}
		}
		public override string ToString()
		{
			return SourceText;
		}
	}
#endif
}
