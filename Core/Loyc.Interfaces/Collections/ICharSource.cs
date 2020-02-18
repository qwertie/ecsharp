using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Collections
{
	/// <summary>A read-only list of characters plus a <see cref="Slice(int,int)"/> method.</summary>
	/// <remarks>
	/// To get an instance of this interface from a string, write
	/// <c>new StringSlice("string")</c> or <c>(UString)"string"</c>.
	/// <para/>
	/// This is the standard interface for lexers to use as a source of 
	/// characters; is it defined in Loyc.Essentials rather than Loyc.Syntax
	/// so that <see cref="UString"/> can implement it, and because it might
	/// be useful in other contexts.
	/// <para/>
	/// This interface was created to read characters more efficiently. 
	/// Although a lexer could read characters one-at-a-time from 
	/// IReadOnlyList{char} or IListSource{char}, it requires dynamic interface 
	/// dispatch for every character. On the other hand, if lexers avoid this 
	/// overhead by requiring the entire file in the form of a string, it becomes 
	/// necessary to hold the entire file in memory at once, in a very specific 
	/// format (a string).
	/// <para/>
	/// Slice() allows the lexer to request small pieces of the file that
	/// it can read without dynamic dispatch. Typically a lexer will be
	/// derived from Loyc.Syntax.Lexing.BaseLexer, which requests somewhat
	/// small chunks; the ICharSource implementation is is free to read 
	/// larger blocks at once, since the return type, <see cref="UString"/>,
	/// can be a slice of a larger string.
	/// <para/>
	/// This interface provides good efficiency when reading from strings, 
	/// or from data structures composed of large strings (most notably, 
	/// this interface could efficiently return sections of a gap buffer),
	/// but unlike String itself, it is flexible, and does not require
	/// the entire file to be held in memory as a single contiguous block.
	/// <para/>
	/// It's unfortunate that .NET treats strings as something completely 
	/// different than arrays. Otherwise, this interface could support not
	/// just substrings, but subarrays of any element type, which would be
	/// useful any time you want to optimize your code by reducing dynamic 
	/// dispatch.
	/// <para/>
	/// Note about Count: if ICharSource represents to a file or other Stream,
	/// reading Count forces the entire stream to be scanned in order to determine
	/// the number of characters in the file (which may be different from the number 
	/// of bytes). Rather than do a test like <c>if (index >= charSource.Count)</c>
	/// it is better to use <c>if (Slice(index, 1).Count == 0)</c>, or better yet,
	/// <c>TryGet(index, out fail)</c>.
	/// </remarks>
	public interface ICharSource : IListSource<char>
	{
		/// <summary>
		/// Returns a substring from the character source. If some of the
		/// requested characters are past the end of the stream, the string
		/// is truncated to the available number of characters.
		/// </summary>
		/// <param name="startIndex">Index of first character to return. If startIndex >= Count, an empty string is returned.</param>
		/// <param name="length">Number of characters desired.</param>
		/// <exception cref="ArgumentException">Thrown if startIndex or length are negative.</exception>
		new UString Slice(int startIndex, int length);
	}
}
