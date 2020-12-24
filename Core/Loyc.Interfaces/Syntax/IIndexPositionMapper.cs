using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Loyc.MiniTest;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>Contains an <see cref="IndexToLine"/> method.</summary>
	/// <remarks>
	/// The FileName property gets the name of the file on which results returned by 
	/// <see cref="IndexToLine(int)"/> are based. It is not guaranteed that <i>all</i> 
	/// return values from <see cref="IndexToLine(int)"/> use this filename. 
	/// For example, the file may have mappings to other files; see 
	/// <see cref="SourceFileWithLineRemaps"/>.</remarks>
	public interface IIndexToLine : IHasFileName
	{
		/// <summary>Returns the position in a source file of the specified index.</summary>
		/// <remarks>If index is negative, this should return a SourcePos where 
		/// Line and PosInLine are zero (signifying an unknown location). If index 
		/// is beyond the end of the file, this should retun the final position in 
		/// the file.</remarks>
		ILineColumnFile IndexToLine(int index);
	}

	/// <summary>Contains <see cref="LineToIndex"/> methods.</summary>
	public interface ILineToIndex : IHasFileName
	{
		/// <summary>Returns the index in a source file of the beginning of the 
		/// specified line, where the first line is number 1, not 0.</summary>
		/// <remarks>If lineNo is zero, this method should return -1 (signifying 
		/// an unknown location). If lineNo is larger than the largest line 
		/// number, this method should return the index of end-of-file.</remarks>
		int LineToIndex(int lineNo);
		int LineToIndex(ILineAndColumn pos);
	}

	/// <summary>
	/// This interface is for classes that can convert indexes to SourcePos
	/// structures and back.
	/// </summary>
	public interface IIndexPositionMapper : ILineToIndex, IIndexToLine
	{
	}
}
