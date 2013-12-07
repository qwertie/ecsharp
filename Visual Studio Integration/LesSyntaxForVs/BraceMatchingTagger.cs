using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Linq;
using Loyc;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
 
namespace VS.Common
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType("LES")] // could be any ContentType that wants brace matching
	[TagType(typeof(TextMarkerTag))]
	public sealed class DefaultBraceMatchingTaggerProvider : IViewTaggerProvider
	{
		[Import]
		public IClassifierAggregatorService AggregatorService;

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			if (textView == null)
				return null;

			var aggregator = AggregatorService.GetClassifier(buffer);
			var pairs = new KeyValuePair<char, char>[]
				{
					new KeyValuePair<char, char>('(', ')'),
					new KeyValuePair<char, char>('{', '}'),
					new KeyValuePair<char, char>('[', ']')
				};
			return new BraceMatchingTagger(textView, buffer, aggregator, pairs) as ITagger<T>;
		}
	}

	internal sealed class BraceMatchingTagger : ITagger<TextMarkerTag>
	{
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		public BraceMatchingTagger(ITextView textView, ITextBuffer sourceBuffer, IClassifier aggregator, IEnumerable<KeyValuePair<char, char>> matchingCharacters)
		{
			CheckParam.IsNotNull("textView", textView);
			CheckParam.IsNotNull("sourceBuffer", sourceBuffer);
			CheckParam.IsNotNull("aggregator", aggregator);
			CheckParam.IsNotNull("matchingCharacters", matchingCharacters);

			this.TextView = textView;
			this.SourceBuffer = sourceBuffer;
			this.Aggregator = aggregator;
			this.MatchingCharacters = matchingCharacters.ToList().AsReadOnly();

			this.TextView.Caret.PositionChanged += Caret_PositionChanged;
			this.TextView.LayoutChanged += TextView_LayoutChanged;
		}

		public ITextView TextView { get; private set; }
		public ITextBuffer SourceBuffer { get; private set; }
		public IClassifier Aggregator { get; private set; }
		public ReadOnlyCollection<KeyValuePair<char, char>> MatchingCharacters { get; private set; }
		private SnapshotPoint? CurrentChar { get; set; }

		private static bool IsInCommentOrLiteral(IClassifier aggregator, SnapshotPoint point, PositionAffinity affinity)
		{
			CheckParam.IsNotNull("aggregator", aggregator);

			// TODO: handle affinity
			SnapshotSpan span = new SnapshotSpan(point, 1);

			var classifications = aggregator.GetClassificationSpans(span);
			var relevant = classifications.FirstOrDefault(classificationSpan => classificationSpan.Span.Contains(point));
			if (relevant == null || relevant.ClassificationType == null)
				return false;

			return relevant.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Comment)
				|| relevant.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Literal)
				|| relevant.ClassificationType.IsOfType(PredefinedClassificationTypeNames.String)
				|| relevant.ClassificationType.IsOfType(PredefinedClassificationTypeNames.Identifier);
		}

		private bool IsMatchStartCharacter(char c)
		{
			return MatchingCharacters.Any(pair => pair.Key == c);
		}

		private bool IsMatchCloseCharacter(char c)
		{
			return MatchingCharacters.Any(pair => pair.Value == c);
		}

		private char GetMatchCloseCharacter(char c)
		{
			return MatchingCharacters.First(pair => pair.Key == c).Value;
		}

		private char GetMatchOpenCharacter(char c)
		{
			return MatchingCharacters.First(pair => pair.Value == c).Key;
		}

		private static bool FindMatchingCloseChar(SnapshotPoint start, IClassifier aggregator, char open, char close, int maxLines, out SnapshotSpan pairSpan)
		{
			pairSpan = new SnapshotSpan(start.Snapshot, 1, 1);
			ITextSnapshotLine line = start.GetContainingLine();
			string lineText = line.GetText();
			int lineNumber = line.LineNumber;
			int offset = start.Position - line.Start.Position + 1;

			int stopLineNumber = start.Snapshot.LineCount - 1;
			if (maxLines > 0)
				stopLineNumber = Math.Min(stopLineNumber, lineNumber + maxLines);

			int openCount = 0;
			while (true)
			{
				while (offset < line.Length)
				{
					char currentChar = lineText[offset];
					// TODO: is this the correct affinity
					if (currentChar == close && !IsInCommentOrLiteral(aggregator, new SnapshotPoint(start.Snapshot, offset + line.Start.Position), PositionAffinity.Successor))
					{
						if (openCount > 0)
						{
							openCount--;
						}
						else
						{
							pairSpan = new SnapshotSpan(start.Snapshot, line.Start + offset, 1);
							return true;
						}
					}
					// TODO: is this the correct affinity
					else if (currentChar == open && !IsInCommentOrLiteral(aggregator, new SnapshotPoint(start.Snapshot, offset + line.Start.Position), PositionAffinity.Successor))
					{
						openCount++;
					}

					offset++;
				}

				// move on to the next line
				lineNumber++;
				if (lineNumber > stopLineNumber)
					break;

				line = line.Snapshot.GetLineFromLineNumber(lineNumber);
				lineText = line.GetText();
				offset = 0;
			}

			return false;
		}

		private static bool FindMatchingOpenChar(SnapshotPoint start, IClassifier aggregator, char open, char close, int maxLines, out SnapshotSpan pairSpan)
		{
			pairSpan = new SnapshotSpan(start, start);
			ITextSnapshotLine line = start.GetContainingLine();
			int lineNumber = line.LineNumber;
			int offset = start - line.Start - 1;

			// if the offset is negative, move to the previous line
			if (offset < 0)
			{
				lineNumber--;
				line = line.Snapshot.GetLineFromLineNumber(lineNumber);
				offset = line.Length - 1;
			}

			string lineText = line.GetText();

			int stopLineNumber = 0;
			if (maxLines > 0)
				stopLineNumber = Math.Max(stopLineNumber, lineNumber - maxLines);

			int closeCount = 0;
			while (true)
			{
				while (offset >= 0)
				{
					char currentChar = lineText[offset];
					// TODO: is this the correct affinity
					if (currentChar == open && !IsInCommentOrLiteral(aggregator, new SnapshotPoint(start.Snapshot, offset + line.Start.Position), PositionAffinity.Successor))
					{
						if (closeCount > 0)
						{
							closeCount--;
						}
						else
						{
							pairSpan = new SnapshotSpan(line.Start + offset, 1);
							return true;
						}
					}
					// TODO: is this the correct affinity
					else if (currentChar == close && !IsInCommentOrLiteral(aggregator, new SnapshotPoint(start.Snapshot, offset + line.Start.Position), PositionAffinity.Successor))
					{
						closeCount++;
					}

					offset--;
				}

				// move to the previous line
				lineNumber--;
				if (lineNumber < stopLineNumber)
					break;

				line = line.Snapshot.GetLineFromLineNumber(lineNumber);
				lineText = line.GetText();
				offset = line.Length - 1;
			}

			return false;
		}

		public static readonly TextMarkerTag BraceHighlightTag = new TextMarkerTag(PredefinedClassificationTypeNames.Keyword);

		public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			if (spans.Count == 0)
				yield break;

			// don't do anything if the current SnapshotPoint is not initialized or at the end of the buffer
			if (!CurrentChar.HasValue || CurrentChar.Value.Position >= CurrentChar.Value.Snapshot.Length)
				yield break;

			// hold on to a snapshot of the current character
			var currentChar = CurrentChar.Value;

			if (IsInCommentOrLiteral(Aggregator, currentChar, TextView.Caret.Position.Affinity))
				yield break;

			// if the requested snapshot isn't the same as the one the brace is on, translate our spans to the expected snapshot
			currentChar = currentChar.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);

			// get the current char and the previous char
			char currentText = currentChar.GetChar();
			// if current char is 0 (beginning of buffer), don't move it back
			SnapshotPoint lastChar = currentChar == 0 ? currentChar : currentChar - 1;
			char lastText = lastChar.GetChar();
			SnapshotSpan pairSpan = new SnapshotSpan();

			if (IsMatchStartCharacter(currentText))
			{
				char closeChar = GetMatchCloseCharacter(currentText);
				/* TODO: Need to improve handling of larger blocks. this won't highlight if the matching brace is more
				 *       than 1 screen's worth of lines away. Changing this to 10 * TextView.TextViewLines.Count seemed
				 *       to improve the situation.
				 */
				if (BraceMatchingTagger.FindMatchingCloseChar(currentChar, Aggregator, currentText, closeChar, 10 * TextView.TextViewLines.Count, out pairSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentChar, 1), BraceHighlightTag);
					yield return new TagSpan<TextMarkerTag>(pairSpan, BraceHighlightTag);
				}
			}
			else if (IsMatchCloseCharacter(lastText))
			{
				var open = GetMatchOpenCharacter(lastText);
				if (BraceMatchingTagger.FindMatchingOpenChar(lastChar, Aggregator, open, lastText, 10 * TextView.TextViewLines.Count, out pairSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(lastChar, 1), BraceHighlightTag);
					yield return new TagSpan<TextMarkerTag>(pairSpan, BraceHighlightTag);
				}
			}
		}

		private void UpdateAtCaretPosition(CaretPosition caretPosition)
		{
			CurrentChar = caretPosition.Point.GetPoint(SourceBuffer, caretPosition.Affinity);
			if (!CurrentChar.HasValue)
				return;

			var t = TagsChanged;
			if (t != null)
				t(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
		}

		private void TextView_LayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (e.NewSnapshot != e.OldSnapshot)
				UpdateAtCaretPosition(TextView.Caret.Position);
		}

		private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			UpdateAtCaretPosition(e.NewPosition);
		}
	}
}