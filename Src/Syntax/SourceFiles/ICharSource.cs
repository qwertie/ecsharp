using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using NUnit.Framework;
using Loyc.Essentials;
using Loyc.Collections;

namespace Loyc.Syntax
{
	/// <summary>
	/// A character source. TokenId is int so that the indexer can return a
	/// unique EOF value (-1).
	/// </summary>
	public interface ICharSource : IParserSource<char>, IIndexPositionMapper
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
		UString Substring(int startIndex, int length);
	}

	public interface ISourceFile : ICharSource
	{
		string FileName { get; }

		//string Language { get; }
	}
}
