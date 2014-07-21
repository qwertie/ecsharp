using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Loyc.Collections;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Loyc.Syntax.Lexing;
using System.Windows.Threading;
using System.Threading;
using Microsoft.VisualStudio.Text.Tagging;
using System.Diagnostics;

namespace Loyc.VisualStudio
{
	/// <summary>Base class for parsers or other analysis taggers that are too slow 
	/// to run synchronously. The Run() method is called on a thread-pool thread to
	/// perform the analysis.</summary>
	/// <remarks>
	/// This class is designed for cases where the number of tags in a file is 
	/// manageable (e.g. one tag per line or less), as it does not use any memory-
	/// saving techniques.
	/// <para/>
	/// To create a background tagger, make a derived class that implements
	/// <c>Run()</c>, and write a factory class with MEF attributes that implements
	/// <c>ITaggerProvider</c>. This class accepts an optional <c>SyntaxClassifierForVS</c>
	/// which will will provide token information.</remarks>
	public abstract class BackgroundTaggerForVS<TTag> : ITagger<TTag>
		where TTag : ITag
	{
		// Visual Studio text buffer
		protected ITextBuffer _buffer;
		// Optional: Loyc lexical analyzer
		protected SyntaxClassifierForVS _classifier;
		// Timer that initiates Run() a certain amount of time after editing (default: 750ms)
		protected Lazy<DispatcherTimer> _runTimer;
		// Results of Run()
		protected IEnumerable<ITagSpan<TTag>> _results;
		// Set to null when we have not yet requested to start Run()
		CancellationTokenSource _ctsIfRunning;
		protected bool IsRunning { get { return _ctsIfRunning != null; } }

		internal BackgroundTaggerForVS(ITextBuffer buffer, SyntaxClassifierForVS classifier = null)
		{
			Debug.Assert(classifier == null || classifier.Buffer == buffer);
			_buffer = buffer;
			_buffer.Changed += OnTextBufferChanged;
			_classifier = classifier;
			_runTimer = new Lazy<DispatcherTimer>(() => {
				var timer = new DispatcherTimer(DispatcherPriority.Background) { 
					Interval = TimeSpan.FromSeconds(0.75), IsEnabled = true
				};
				timer.Tick += ParseTimerTick;
				return timer;
			});
		}

		public static DList<Token> ToNormalTokens(SparseAList<EditorToken> eTokens)
		{
			var output = new DList<Token>();
			int? index = null;
			for(;;) {
				EditorToken eTok = eTokens.NextHigherItem(ref index);
				if (index == null) break;
				output.Add(eTok.ToToken(index.Value));
			}
			return output;
		}

		/// <summary>Called on a thread-pool thread to parse the file. The returned 
		/// list must be immutable (it must not change later). The cancellation 
		/// signal indicates that the text buffer has changed since the operation
		/// started; aborting is optional and if the method does abort, it should 
		/// return null.</summary>
		protected abstract IEnumerable<ITagSpan<TTag>> Run(ITextSnapshot snapshot, SparseAList<EditorToken> tokens, CancellationToken cancellationToken);

		protected void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			if (_runTimer.IsValueCreated)
				_runTimer.Value.Start();
			if (IsRunning)
				_ctsIfRunning.Cancel();
		}

		#region ITagger<TTag> interface

		public IEnumerable<ITagSpan<TTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			var _ = _runTimer.Value; // auto-create timer
			if (_results == null)
				return null;
			return _results.Where(tagAndSpan => {
				foreach(SnapshotSpan span in spans)
					if (tagAndSpan.Span.OverlapsWith(span.Span))
						return true;
				return false;
			});
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		#endregion

		private void ParseTimerTick(object sender, EventArgs e)
		{
			if (!IsRunning) {
				_runTimer.Value.Stop();
				_ctsIfRunning = new CancellationTokenSource(); // IsRunning = true
				
				ITextSnapshot snapshot = _buffer.CurrentSnapshot;
				SparseAList<EditorToken> tokens = _classifier != null ? _classifier.GetSnapshotOfTokens() : null;
				ThreadPool.QueueUserWorkItem(@null => {
					try {
						var tags = Run(snapshot, tokens, _ctsIfRunning.Token);
						if (tags != null)
							_runTimer.Value.Dispatcher.Invoke(new Action<IEnumerable<ITagSpan<TTag>>>(OnRunSucceeded), tags);
					} catch (Exception ex) {
						Trace.WriteLine(string.Format("Exception occurred in tagger: {0}", GetType().Name));
						Trace.WriteLine(ex.DescriptionAndStackTrace());
					} finally {
						_ctsIfRunning = null; // set IsRunning = false
					}
				});
			}
		}

		protected virtual void OnRunSucceeded(IEnumerable<ITagSpan<TTag>> results) // called on UI thread
		{
			_results = results;

			// We don't have a "diff" algorithm or anything, so claim everything changed.
			if (TagsChanged != null)
				TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(
					_buffer.CurrentSnapshot, new Span(0, _buffer.CurrentSnapshot.Length))));

			if (_runTimer.Value.IsEnabled) { // Add a delay between successful runs
				_runTimer.Value.Stop();
				_runTimer.Value.Start();
			}
		}
	}


	//[Export(typeof(EditorFormatDefinition))]
	//[Name("mymarker")]
	//internal sealed class MyMarkerDefinition : MarkerFormatDefinition
	//{
	//	public MyMarkerDefinition()
	//	{
	//		this.ZOrder = 1;
	//		this.Fill = Brushes.Blue;
	//		this.Border = new Pen(Brushes.DarkGray, 0.5);
	//		this.Fill.Freeze();
	//		this.Border.Freeze();
	//	}
	//}
}
