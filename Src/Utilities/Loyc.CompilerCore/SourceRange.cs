using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Collections;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// Holds a reference to a source file (ISourceFile&lt;char&gt;) and the
	/// beginning and end indices of a range in that file.
	/// </summary>
	public struct SourceRange
	{
		public static readonly SourceRange Nowhere = new SourceRange(EmptySourceFile.Default, -1, 0);

		public SourceRange(ISourceFile source, int beginIndex, int length)
		{
			_source = source;
			_beginIndex = beginIndex;
			_length = length;
		}

		private ISourceFile _source;
		private int _beginIndex;
		private int _length;

		public ISourceFile Source { [DebuggerStepThrough] get { return _source; } }
		public int BeginIndex     { [DebuggerStepThrough] get { return _beginIndex; } }
		public int EndIndex       { [DebuggerStepThrough] get { return _beginIndex + _length; } }
		public int Length         { [DebuggerStepThrough] get { return _length; } }

		public SourcePos Begin
		{
			get { 
				if (Source == null)
					return SourcePos.Nowhere;
				return Source.IndexToLine(BeginIndex);
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
				return _source.TryGet(_beginIndex + subIndex, '\uFFFF');
			}
		}

		public static bool operator ==(SourceRange a, SourceRange b)
		{
			return a._source == b._source && a._beginIndex == b._beginIndex && a._length == b._length;
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
			return hc ^ _beginIndex ^ (_length << 4);
		}
		public override string ToString()
		{
			return string.Format("{0}[{1}+{2}]", _source.FileName, _beginIndex, _length);
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
