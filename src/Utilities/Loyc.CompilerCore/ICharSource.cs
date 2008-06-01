using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// A character source. TokenId is int so that the indexer can return a
	/// unique EOF value (-1).
	/// </summary>
	public interface ICharSource : ISimpleSource2<int>, IIndexPositionMapper
	{
		/// <summary>
		/// Returns a substring from the character source. If some of the
		/// requested characters are past the end of the stream, the string
		/// is truncated to the available number of characters.
		/// </summary>
		/// <param name="startIndex">Index of first character to return. If startIndex >= Count, an empty string is returned.</param>
		/// <param name="length">Number of characters desired.</param>
		/// <exception cref="ArgumentException">Thrown if startIndex or length are negative.</exception>
		/// <returns></returns>
		string Substring(int startIndex, int length);
	}
}
