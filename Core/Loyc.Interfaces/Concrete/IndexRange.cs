using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			get => StartIndex + Length;
			set => Length = value - StartIndex;
		}
	}
}
