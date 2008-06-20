using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.CompilerCore
{
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
}
