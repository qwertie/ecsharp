using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Math;

namespace Loyc
{
	/// <summary>
	/// A wrapper around <see cref="Stopwatch"/> with a more convenient interface,
	/// currently oriented around measuring milliseconds 
	/// (TODO: increase similarity to <c>Stopwatch</c>)
	/// </summary>
	/// <remarks>
	/// EzStopwatch is a wrapper around the normal <see cref="Stopwatch"/> that
	/// is less clumsy to use: you can get the elapsed time and restart the 
	/// timer from zero with a single call to Restart(). The Stopwatch class
	/// (prior to .NET 4, anyway) requires you to make three separate method 
	/// calls to do the same thing: you have to call ElapsedMilliseconds, then 
	/// Reset(), then Start().
	/// <para/>
	/// Unlike <see cref="Loyc.Utilities.SimpleTimer"/>, this class does not start 
	/// timing when it is created, which allows it to be a struct without a 
	/// constructor.
	/// <para/>
	/// EzStopwatch behaves differently from Stopwatch when restarting, because I
	/// observed a problem using the timer to measure short time intervals. I ran
	/// trials of three operations in a loop, and the loop was programmed to run 
	/// until the total elapsed time for one of the operations exceeded 100 ms. On 
	/// each iteration I restarted the timer three times because there were three 
	/// operations to measure, and when I replaced <see cref="SimpleTimer"/> with 
	/// <see cref="EzStopwatch"/> for greater accuracy, the loop ran forever! The
	/// problem was that (depending on the benchmark's input parameters) the 
	/// operation could take less than 1 millisecond to complete, so 
	/// <see cref="Millisec"/> always returned zero, and the total never reached
	/// 100.
	/// <para/>
	/// To solve this problem, when you "<see cref="Restart"/>" the timer, it is
	/// not completely reset to zero, but rather the current value of Millisec is
	/// subtracted from the timer. This leaves a fractional amount of time less
	/// than 1 millisecond in the timer, so that if you take two measurements 
	/// that each take 0.6 milliseconds, Millisec will return 0 the first time
	/// and 1 the second time, leaving 0.2 milliseconds on the clock.
	/// <para/>
	/// TODO: change interfaces of SimpleTimer and EzStopwatch to better resemble
	/// Stopwatch, even though the behavior of "Pause" and "Resume" is more obvious
	/// than "Stop" and "Start".
	/// </remarks>
	public struct EzStopwatch
	{
		Stopwatch _timer;
		long _offs;

		public EzStopwatch(bool start)
		{
			_timer = null; _offs = 0;
			if (start) Resume();
		}
		void AutoInit()
		{
			if (_timer == null)
				_timer = new Stopwatch();
		}

		/// <summary>Gets or sets the current time on the clock.</summary>
		/// <remarks>This property can be used whether the timer is running or not,
		/// and it does not affect the value of <see cref="Paused"/>. It is legal
		/// to make the current value negative.</remarks>
		public int Millisec
		{
			get { return (int)LongMillisec; }
			set { LongMillisec = value; }
		}
		public long LongMillisec
		{
			get { return (_timer == null ? 0 : _timer.ElapsedMilliseconds) - _offs; }
			set { AutoInit(); _offs = _timer.ElapsedMilliseconds - value; }
		}

		/// <summary>Restarts the timer from zero (unpausing it if it is paused), 
		/// and returns the number of elapsed milliseconds prior to the reset.</summary>
		public int Restart()
		{
			AutoInit();
			long ms = LongMillisec;
			_offs += ms; // reset to zero
			if ((int)_offs != _offs)
				Reset(); // _base getting huge => re-center on zero
			_timer.Start();
			return (int)MathEx.InRange(ms, int.MinValue, int.MaxValue);
		}
		/// <summary>Resets the timer to 0 and pauses it there.</summary>
		public void Reset()
		{
			_timer.Reset();
			_offs = 0;
		}
		public bool Paused 
		{
			get { return _timer == null || !_timer.IsRunning; }
		}
		public void Pause()
		{
			if (_timer != null)
				_timer.Stop();
		}
		public void Resume()
		{
			AutoInit();
			_timer.Start();
		}
		/// <summary>Restarts the timer from zero if the specified number of 
		/// milliseconds have passed, and returns the former value of Millisec.</summary>
		/// <returns>If the timer was restarted, this method returns the number of 
		/// elapsed milliseconds prior to the reset. Returns 0 if the timer was not 
		/// reset.</returns>
		/// <remarks>If this method resets a paused timer, it remains paused but 
		/// Millisec is set to zero.</remarks>
		public int ClearAfter(int minimumMillisec)
		{
			long millisec = Millisec;
			if (millisec < minimumMillisec)
				return 0;
			else {
				Millisec = 0;
				return (int)millisec;
			}
		}
	}
}
