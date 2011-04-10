using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Loyc.Essentials
{
	/// <summary>
	/// A timer class with a more convenient interface than 
	/// System.Diagnostics.Stopwatch. Its resolution is typically 10 ms. It 
	/// uses DateTime.UtcNow, so it could change suddenly and even become 
	/// negative if the user changes the system time, so be careful how you 
	/// rely on it.
	/// </summary>
	/// <remarks>
	/// With SimpleTimer, the timer starts when you construct the object and 
	/// it is always counting. You can get the elapsed time and restart the 
	/// timer from zero with a single call to Restart(). The Stopwatch class 
	/// requires you to make three separate method calls to do the same thing:
	/// you have to call ElapsedMilliseconds, then Reset(), then Start().
	/// </remarks>
	public class SimpleTimer
	{
		DateTime _start = DateTime.UtcNow;

		/// <summary>
		/// The getter returns the number of milliseconds since the timer was 
		/// started; the resolution of this property depends on the system timer.
		/// The setter changes the value of the timer.
		/// </summary>
		public int Millisec
		{
			get { return (int)((DateTime.UtcNow - _start).Ticks / 10000); }
			set {
				_start = new DateTime(DateTime.UtcNow.Ticks - (long)value * 10000);
			}
		}

		/// <summary>Restarts the timer from zero, and returns the number of 
		/// elapsed milliseconds prior to the reset.</summary>
		public int Restart()
		{
			DateTime now = DateTime.UtcNow;
			int millisec = (int)((now - _start).Ticks / 10000);
			_start = now;
			return millisec;
		}

		/// <summary>Restarts the timer from zero if , and returns the number of 
		/// elapsed milliseconds at the time of the restart.</summary>
		/// <returns>If the timer was restarted, this method returns the number of 
		/// elapsed milliseconds prior to the reset. Returns 0 if the timer was not 
		/// reset.</returns>
		public int RestartAfter(int minimumMillisec)
		{
			if ((uint)Millisec < (uint)minimumMillisec)
				return 0;
			else
				return Restart();
		}
	}
}
