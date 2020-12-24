using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Math;

namespace Loyc.Syntax
{
	/// <summary>A mutable pair of (StartIndex, Length) that implements <see cref="IIndexRange"/>.</summary>
	public struct IndexRange : IIndexRange
	{
		public IndexRange(int startIndex, int length = 0)
		{
			StartIndex = startIndex;
			Length = length;
		}
		public int StartIndex { get; set; }
		public int Length { get; set; }
		public int EndIndex
		{
			get => checked(StartIndex + Length);
			set => Length = checked(value - StartIndex);
		}
		
		/// <summary>Ensures that StartIndex <= EndIndex by swapping the two if the Length is negative.</summary>
		/// <exception cref="OverflowException">An integer overflow occurred.</exception>
		/// <returns><c>this</c>.</returns>
		public IndexRange Normalize()
		{
			if (Length < 0) checked
			{
				StartIndex += Length;
				Length = -Length;
			}
			return this;
		}

		/// <summary>Assuming both ranges are normalized, returns the range of overlap between them.
		/// If the ranges do not overlap, the Length of the returned range will be zero or negative.</summary>
		public IndexRange GetRangeOfOverlap(IndexRange other) =>
			new IndexRange(Max(StartIndex, other.StartIndex)) { EndIndex = Min(other.EndIndex, EndIndex) };
		/// <summary>Returns true if, assuming both ranges are normalized, the two regions 
		/// share at least one common character.</summary>
		/// <remarks>Note: this returns false if either of the ranges has a Length of zero 
		/// and is at the boundary of the other range.</remarks>
		public bool Overlaps(IndexRange other) => EndIndex > other.StartIndex && StartIndex < other.EndIndex;
		
		/// <summary>Returns true if, assuming both ranges are normalized, the <c>other</c>
		/// range is entirely within the boundaries of this range.</summary>
		public bool Contains(IndexRange other) => StartIndex <= other.StartIndex && EndIndex >= other.EndIndex;
	}
}
