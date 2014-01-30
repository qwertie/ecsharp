using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>Contains <see cref="IndexToLine"/> method.</summary>
	/// <seealso cref="IIndexPositionMapper"/>
	public interface IIndexToLine
	{
		/// <summary>Returns the position in a source file of the specified index.</summary>
		/// <remarks>If index is negative, this should return a SourcePos where 
		/// Line and PosInLine are zero (signifying an unknown location). If index 
		/// is beyond the end of the file, this should retun the final position in 
		/// the file.</remarks>
		SourcePos IndexToLine(int index);
	}

	/// <summary>
	/// This interface is for classes that can convert indexes to SourcePos
	/// structures and back.
	/// </summary>
	public interface IIndexPositionMapper : IIndexToLine
	{
		/// <summary>Returns the index in a source file of the beginning of the 
		/// specified line, where the first line is number 1, not 0.</summary>
		/// <remarks>If lineNo is zero, this method should return -1 (signifying 
		/// an unknown location). If lineNo is larger than the largest line 
		/// number, this method should return the index of end-of-file.</remarks>
		int LineToIndex(int lineNo);
		int LineToIndex(LineAndPos pos);
	}
}
