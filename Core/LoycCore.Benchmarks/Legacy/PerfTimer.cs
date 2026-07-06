using System;
using System.Diagnostics;

namespace Benchmark
{
	/// <summary>A <see cref="Stopwatch"/>-based drop-in replacement for the legacy
	/// <c>Loyc.Utilities.SimpleTimer</c> and <c>Loyc.EzStopwatch</c> timers used by
	/// the ported console benchmarks, but with fractional-millisecond resolution.</summary>
	/// <remarks>
	/// The two legacy timers measured time in whole (integer) milliseconds, which
	/// forced benchmarks to run huge iteration counts just so a single measurement
	/// would round to a meaningful number of milliseconds. PerfTimer exposes the
	/// union of the members those two classes used, but <see cref="Millisec"/> and
	/// <see cref="Restart"/> now return <c>double</c> (fractional milliseconds),
	/// which lets the benchmarks measure short operations accurately and therefore
	/// run far fewer iterations.
	/// <para/>
	/// API compatibility notes:
	/// <ul>
	/// <li>Like <c>SimpleTimer</c>, the parameterless constructor starts the timer
	///   immediately.</li>
	/// <li>Like <c>EzStopwatch</c>, the <c>PerfTimer(bool start)</c> overload lets
	///   you create a timer without starting it. (In practice every use restarts
	///   the timer before measuring, so this only matters in theory.)</li>
	/// <li><see cref="Restart"/> returns the fractional milliseconds elapsed prior
	///   to the reset (the legacy timers returned an integer/long).</li>
	/// <li><see cref="Pause"/>/<see cref="Resume"/> and <see cref="ClearAfter"/>
	///   match the legacy behavior.</li>
	/// </ul>
	/// </remarks>
	public class PerfTimer
	{
		readonly Stopwatch _timer = new Stopwatch();
		// Elapsed ticks already "consumed" by a Restart() or a Millisec setter, i.e.
		// the zero point that the current Millisec value is measured relative to.
		long _offsetTicks = 0;

		/// <summary>Creates and starts the timer (SimpleTimer compatibility).</summary>
		public PerfTimer() : this(true) { }

		/// <summary>Creates the timer, starting it immediately if <paramref name="start"/>
		/// is true (EzStopwatch compatibility).</summary>
		public PerfTimer(bool start)
		{
			if (start)
				_timer.Start();
		}

		/// <summary>Gets or sets the current time on the clock, in fractional
		/// milliseconds. Works whether the timer is running or paused.</summary>
		public double Millisec
		{
			get => TicksToMs(_timer.ElapsedTicks - _offsetTicks);
			set => _offsetTicks = _timer.ElapsedTicks - MsToTicks(value);
		}

		/// <summary>True if the timer is not currently running.</summary>
		public bool Paused => !_timer.IsRunning;

		/// <summary>Restarts the timer from zero (unpausing it if it is paused), and
		/// returns the number of fractional milliseconds elapsed prior to the reset.</summary>
		public double Restart()
		{
			long now = _timer.ElapsedTicks;
			double ms = TicksToMs(now - _offsetTicks);
			_offsetTicks = now;
			_timer.Start();
			return ms;
		}

		/// <summary>Resets the timer to zero and pauses it there.</summary>
		public void Reset()
		{
			_timer.Reset();
			_offsetTicks = 0;
		}

		/// <summary>Pauses the timer. Returns false if it was already paused.</summary>
		public bool Pause()
		{
			bool wasRunning = _timer.IsRunning;
			_timer.Stop();
			return wasRunning;
		}

		/// <summary>Resumes (unpauses) the timer. Returns false if already running.</summary>
		public bool Resume()
		{
			bool wasPaused = !_timer.IsRunning;
			_timer.Start();
			return wasPaused;
		}

		/// <summary>Restarts the timer from zero if at least the specified number of
		/// milliseconds have passed, and returns the former value of <see cref="Millisec"/>
		/// (or 0 if the timer was not reset).</summary>
		public double ClearAfter(double minimumMillisec)
		{
			double ms = Millisec;
			if (ms < minimumMillisec)
				return 0;
			Millisec = 0;
			return ms;
		}

		static double TicksToMs(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
		static long MsToTicks(double ms) => (long)(ms * Stopwatch.Frequency / 1000.0);
	}
}
