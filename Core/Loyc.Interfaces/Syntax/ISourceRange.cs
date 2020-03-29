using System;

namespace Loyc.Syntax
{
	/// <summary>Represents a (contiguous) region of text in a source file.</summary>
	public interface ISourceRange
	{
		ISourceFile Source { get; }
		int StartIndex { get; }
		int EndIndex { get; }
		int Length { get; }
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
				return SourcePos.Nowhere;
			return range.Source.IndexToLine(range.StartIndex);
		}
		public static ILineColumnFile End<SourceRange>(this SourceRange range) where SourceRange : ISourceRange
		{
			if (range.Source == null)
				return SourcePos.Nowhere;
			return range.Source.IndexToLine(range.EndIndex);
		}
	}
}
