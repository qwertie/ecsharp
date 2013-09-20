using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// A standard interface for lexers.
	/// </summary>
	public interface ILexer : IEnumerator<Token>, IIndexToLine
	{
		/// <summary>The file being lexed.</summary>
		ISourceFile Source { get; }
		/// <summary>Scans the next token and returns information about it.</summary>
		/// <returns>The next token, or null at the end of the source file.</returns>
		Token? NextToken();
		/// <summary>Event handler for errors.</summary>
		Action<int, string> OnError { get; set; }
		/// <summary>Indentation level of the current line. This is updated after 
		/// scanning the first whitespaces on a new line, and may be reset to zero 
		/// when <see cref="NextToken()"/> returns a newline.</summary>
		int IndentLevel { get; }
		/// <summary>Current line number (1 for the first line).</summary>
		int LineNumber { get; }
	}
}
