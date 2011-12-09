using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Loyc.Utilities
{
	/// <summary>
	/// A lightweight class to help you compute the minimum, maximum, average
	/// and standard deviation of a set of values. Call Clear(), then Add(each
	/// value); you can compute the average and standard deviation at any time by 
	/// calling Avg() and StdDeviation().
	/// </summary>
	public class Statistic
	{
		public double Min;
		public double Max;
		public double SumTotal;
		public double SumOfSquares;
		public int Count;

		public void Clear()
		{
			SumOfSquares = Min = Max = SumTotal = 0;
			Count = 0;
		}
		public void Add(double nextValue)
		{
			Debug.Assert(!(double.IsNaN(nextValue) || double.IsInfinity(nextValue)));
			if (Count > 0)
			{
				if (Min > nextValue)
					Min = nextValue;
				if (Max < nextValue)
					Max = nextValue;
				SumTotal += nextValue;
				SumOfSquares += nextValue * nextValue;
				Count++;
			}
			else
			{
				Min = Max = SumTotal = nextValue;
				SumOfSquares = nextValue * nextValue;
				Count = 1;
			}
		}
		public double Avg()
		{
			return SumTotal / Count;
		}
		public double Variance()
		{
			return (SumOfSquares * Count - SumTotal * SumTotal) / (Count * (Count - 1));
		}
		public double StdDeviation()
		{
			double v = Variance();
			Debug.Assert(double.IsNaN(v) || v > -0.00001); // can be negative due to a rounding error
			if (v < 0)
				return 0;
			return System.Math.Sqrt(v);
		}
		public Statistic Clone()
		{
			return (Statistic)MemberwiseClone();
		}
	};
}
