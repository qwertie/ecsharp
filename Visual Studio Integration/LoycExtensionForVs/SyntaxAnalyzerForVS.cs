using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Loyc.VisualStudio
{
	/// <summary>Combines <see cref="SyntaxClassifierForVS"/>, which is designed for
	/// synchronous lexical analysis, with <see cref="BackgroundAnalyzerForVS{I,O}"/> 
	/// which uses a thread-pool thread to do asynchronous analysis. This class
	/// implements <see cref="IClassifier"/> (for lexer classifications), 
	/// <see cref="ITagger{ClassificationTag}"/> (for parser classifications), and
	/// <see cref="ITagger{ErrorTag}"/> for error classifications.</summary>
	/// <typeparam name="ParseResults"></typeparam>
	/// <remarks>The derived class overrides <see cref="PrepareLexer"/> to provide
	/// a lexer to <see cref="SyntaxClassifierForVS"/>, and <see cref="RunAnalysis"/> 
	/// to implement parsing (or any other analysis) on a background thread.</remarks>
	public abstract class SyntaxAnalyzerForVS<ParseResults> : SyntaxClassifierForVS,
		IBackgroundAnalyzerImpl<SparseAList<EditorToken>, ParseResults>,
		ITagger<ClassificationTag>,
		ITagger<ErrorTag>
	{
		protected BackgroundAnalyzerForVS<SparseAList<EditorToken>, ParseResults> _parseHelper;
		protected IReadOnlyList<ITagSpan<ITag>> _results;

		public SyntaxAnalyzerForVS(VSBuffer ctx)
			: base(ctx)
		{
			_parseHelper = new BackgroundAnalyzerForVS<SparseAList<EditorToken>, ParseResults>(ctx.Buffer, this, false);
		}

		// Derived class is responsible for parsing in this method
		public abstract ParseResults RunAnalysis(ITextSnapshot snapshot, SparseAList<EditorToken> input, CancellationToken cancelToken);

		public virtual SparseAList<EditorToken> GetInputSnapshot()
		{
			return base.GetSnapshotOfTokens();
		}

		// Derived class must override if ParseResults does not implement 
		// IReadOnlyList<ITagSpan<ITag>>. Note: The results must be sorted by 
		// position; GetTags() uses binary searches to find tags.
		public virtual void OnRunSucceeded(ParseResults results)
		{
			_results = results as IReadOnlyList<ITagSpan<ITag>> ?? _results;
			
			// We don't have a "diff" algorithm or anything, so claim everything changed.
			if (TagsChanged != null)
				TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(
					_ctx.Buffer.CurrentSnapshot, new Span(0, _ctx.Buffer.CurrentSnapshot.Length))));
		}

		#region ITagger<T> interfaces

		IEnumerable<ITagSpan<ClassificationTag>> ITagger<ClassificationTag>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			return GetTags<ClassificationTag>(spans);
		}
		IEnumerable<ITagSpan<ErrorTag>> ITagger<ErrorTag>.GetTags(NormalizedSnapshotSpanCollection spans)
		{
			//var sspan = new SnapshotSpan(Buffer.CurrentSnapshot, new Span(0, 4));
			//return new ITagSpan<ErrorTag>[] { new TagSpan<ErrorTag>(sspan, new ErrorTag("compiler warning", "Example warning tag")) };
			
			List<ITagSpan<ErrorTag>> errors = GetTags<ErrorTag>(spans);
			AddLexerErrors(spans, errors);
			return errors;
		}
		
		public List<ITagSpan<TTag>> GetTags<TTag>(NormalizedSnapshotSpanCollection spans) where TTag : ITag
		{
			_parseHelper.AutoCreateTimer();
			if (_results == null)
				return null;

			List<ITagSpan<TTag>> results = new List<ITagSpan<TTag>>();

			int iStart, iEnd = -1;
			foreach (var span in spans)
			{
				TagAggregatorOptions[] x;
				iStart = _results.BinarySearch2(span.Start.Position, (tspan, start) => (tspan.Span.End.Position-1).CompareTo(start));
				if (iStart < 0) iStart = ~iStart;
				if (iStart < iEnd) iStart = iEnd;
				iEnd = _results.BinarySearch2(span.End.Position, (tspan, end) => tspan.Span.Start.Position.CompareTo(end));
				if (iEnd < 0) iEnd = ~iEnd;

				for (int i = iStart; i < iEnd; i++)
				{
					if (_results[i] is ITagSpan<TTag>)
						results.Add((ITagSpan<TTag>)_results[i]);
				}
			}
			return results;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		#endregion

		#region Translation of lexer errors to ITagSpan<ErrorTag>

		void AddLexerErrors(NormalizedSnapshotSpanCollection spans, List<ITagSpan<ErrorTag>> errors)
		{
			if (_haveLexerErrors) {
				foreach (var span in spans) {
					int? i = span.Start.Position + 1;
					EditorToken t = _tokens.NextLowerItem(ref i);
					while (i != null) do
					{
						if (t.Value is LexerMessage)
							errors.Add(LexerMessageToErrorTagSpan(span.Snapshot, t.ToToken(i.Value, false), t.Value as LexerMessage));
						t = _tokens.NextHigherItem(ref i);
					}
					while (i < span.End.Position);
				}
			}
		}

		protected virtual ITagSpan<ErrorTag> LexerMessageToErrorTagSpan(ITextSnapshot vsSnapshot, Token t, LexerMessage lmsg)
		{
			StringBuilder str = new StringBuilder();

			AppendMessage(str, lmsg.Msg);
			while (lmsg.OriginalValue is LexerMessage)
			{
				lmsg = lmsg.OriginalValue as LexerMessage;
				str.Append("\n");
				AppendMessage(str, lmsg.Msg);
			}

			string errorType = "syntax error";
			if (lmsg.Msg.Severity < Severity.Error)
				errorType = "compiler warning";

			return new TagSpan<ErrorTag>(
				new SnapshotSpan(Buffer.CurrentSnapshot, new Span(t.StartIndex, t.Length)),
				new ErrorTag(errorType, str.ToString()));
		}
		protected void AppendMessage(StringBuilder str, MessageHolder.Message mhmsg)
		{
			str.Append(Localize.From(mhmsg.Severity.ToString()));
			str.Append(": ");
			str.AppendFormat(mhmsg.Format, mhmsg.Args);
		}

		protected override void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			base.OnTextBufferChanged(sender, e);

			if (_haveLexerErrors && TagsChanged != null)
				foreach(ITextChange chg in e.Changes)
					TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(e.After, chg.NewSpan)));
		}
		
		#endregion
	}
}
