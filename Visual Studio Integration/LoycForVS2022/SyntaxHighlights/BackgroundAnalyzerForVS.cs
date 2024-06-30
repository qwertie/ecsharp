using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;

namespace Loyc.VisualStudio
{
	/// <summary>Helper class for parsers or other analysis taggers that are too 
	/// slow to run synchronously. The <c>IBackgroundAnalyzerImpl.Run()</c> method 
	/// is called on a thread-pool thread to perform the analysis.</summary>
	/// <remarks>
	/// This class is designed for cases where the number of tags in a file is 
	/// manageable (e.g. one tag per line or less), as it does not use any memory-
	/// saving techniques.
	/// <para/>
	/// To create a background tagger, make a derived class that implements
	/// <c>Run()</c>, and write a factory class with MEF attributes that implements
	/// <c>ITaggerProvider</c>. This class accepts an optional <c>SyntaxClassifierForVS</c>
	/// which will will provide token information.</remarks>
	public class BackgroundAnalyzerForVS<TInput, TResults>
	{
		// Analysis logic
		IBackgroundAnalyzerImpl<TInput, TResults> _impl;
		// Visual Studio imports and text buffer
		protected ITextBuffer _buffer;
		// Timer that initiates _impl.Run() a certain amount of time after editing (default: 750ms)
		protected Lazy<DispatcherTimer> _runTimer;
		static TimeSpan TimerDelay = TimeSpan.FromMilliseconds(750);
		// Results of Run()
		protected TResults _results;
		// Set to null when we have not yet requested to start Run()
		CancellationTokenSource _ctsIfRunning;
		protected bool IsRunning { get { return _ctsIfRunning != null; } }

		internal BackgroundAnalyzerForVS(ITextBuffer buffer, IBackgroundAnalyzerImpl<TInput, TResults> impl, bool createTimerNow)
		{
			_buffer = buffer;
			_impl = impl;
			_buffer.Changed += OnTextBufferChanged;
			_runTimer = new Lazy<DispatcherTimer>(() => {
				var timer = new DispatcherTimer(DispatcherPriority.Background) {
					Interval = TimerDelay, IsEnabled = true
				};
				timer.Tick += ParseTimerTick;
				return timer;
			});
			if (createTimerNow)
				AutoCreateTimer();
		}

		public void AutoCreateTimer()
		{
			var _ = _runTimer.Value;
		}

		protected void OnTextBufferChanged(object sender, TextContentChangedEventArgs e)
		{
			if (_runTimer.IsValueCreated)
				_runTimer.Value.Start();
			if (IsRunning)
				_ctsIfRunning.Cancel();
		}

		private void ParseTimerTick(object sender, EventArgs e)
		{
			if (!IsRunning) {
				_runTimer.Value.Stop();
				_ctsIfRunning = new CancellationTokenSource(); // IsRunning = true
				
				ITextSnapshot snapshot = _buffer.CurrentSnapshot;
				TInput input = _impl.GetInputSnapshot();
				ThreadPool.QueueUserWorkItem(@null => {
					try {
						var results = _impl.RunAnalysis(snapshot, input, _ctsIfRunning.Token);
						if (results != null)
							_runTimer.Value.Dispatcher.Invoke(new Action<TResults>(OnRunSucceeded), results);
					} catch (Exception ex) {
						Trace.WriteLine(string.Format("Exception occurred in tagger: {0}", GetType().Name));
						Trace.WriteLine(ex.DescriptionAndStackTrace());
					} finally {
						_ctsIfRunning = null; // set IsRunning = false
					}
				});
			}
		}

		protected virtual void OnRunSucceeded(TResults results) // called on UI thread
		{
			_results = results;

			_impl.OnRunSucceeded(results);

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

	/// <summary>Three methods called by <see cref="BackgroundAnalyzerForVS{I,R}"/>.</summary>
	internal interface IBackgroundAnalyzerImpl<TInput, TResults> 
	{
		/// <summary>Step 1: This is called on the UI thread to get pre-existing 
		/// data used by the analyzer (e.g. tokens).</summary>
		TInput GetInputSnapshot();
		/// <summary>Step 2: This is called in the Thread Pool to run a background 
		/// analysis procedure (e.g. parsing).</summary>
		/// <param name="input">The value returned by GetInputSnapshot()</param>
		/// <param name="cancelToken">The cancellation signal indicates that the 
		/// text buffer has changed since the operation started; aborting is 
		/// optional and if the method does abort, it should return null.</param>
		/// <remarks>Any thrown exception is traced out and ignored.</remarks>
		TResults RunAnalysis(ITextSnapshot snapshot, TInput input, CancellationToken cancelToken);
		/// <summary>Step 3: This is called to notify the UI thread that Run() 
		/// succeeded, which is defined as whenever Run() does not return null or 
		/// throw an exception.</summary>
		void OnRunSucceeded(TResults results);
	}
}
