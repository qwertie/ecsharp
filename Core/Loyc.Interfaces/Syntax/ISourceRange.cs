using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax
{
	/// <summary>Represents a pair of integers that represents a range of indices:
	/// either start & end or start & length. Invariant: Length == EndIndex-StartIndex.</summary>
	public interface IIndexRange
	{
		int StartIndex { get; }
		int EndIndex { get; }
		int Length { get; }
	}

	/// <summary>Represents a (contiguous) region of text in a source file.</summary>
	public interface ISourceRange : IIndexRange
	{
		ISourceFile Source { get; }
	}

	/// <summary>Standard extension methods for <see cref="ISourceRange"/>.</summary>
	public static class SourceRangeExt
	{
		public static UString SourceText<SourceRange>(this SourceRange range) where SourceRange : ISourceRange
		{
			if (range.EndIndex <= range.StartIndex)
				return "";
			if (range.StartIndex >= range.Source.Text.Count)
				return Localize.Localized(range.Source.Text.Count == 0 ? "(not available)" : "(invalid range)");
			return range.Source.Text.Slice(range.StartIndex, range.EndIndex - range.StartIndex);
		}
		public static ILineColumnFile Start<SourceRange>(this SourceRange range) where SourceRange : ISourceRange
		{
			if (range.Source == null)
				return LineColumnFile.Nowhere;
			return range.Source.IndexToLine(range.StartIndex);
		}
		public static ILineColumnFile End<SourceRange>(this SourceRange range) where SourceRange : ISourceRange
		{
			if (range.Source == null)
				return LineColumnFile.Nowhere;
			return range.Source.IndexToLine(range.EndIndex);
		}
	}
}
