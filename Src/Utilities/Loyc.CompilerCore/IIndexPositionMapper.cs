using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;

namespace Loyc.CompilerCore
{
	/// <summary>Normally I prefer not to make interfaces with only one method 
	/// (delegates usually suffice for such a case), but it's necessary here. 
	/// IIndexToLine contains the IndexToLine() method, which is shared between 
	/// <see cref="ISimpleSource2{T}"/> and <see cref="IIndexPositionMapper"/>. 
	/// Also, as a non-generic class, RecognitionException must take IIndexToLine 
	/// in its constructor rather than ISimpleSource(of T).</summary>
	public interface IIndexToLine
	{
		/// <summary>Returns the position in a source file of the specified index.</summary>
		SourcePosition IndexToLine(int index);
	}

	/// <summary>
	/// This interface is for classes that can convert indexes to LinePos
	/// structures and back.
	/// </summary>
	public interface IIndexPositionMapper : IIndexToLine
	{
		int LineToIndex(int lineNo);
		int LineToIndex(SourcePosition pos);
	}
}
