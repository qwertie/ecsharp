using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;
using Loyc.Utilities;

namespace Loyc.CompilerCore
{
	static class LoycG
	{
		/// <summary>Adds a keyword to a dictionary.</summary>
		/// <returns>true if keyword was added, false if it was already defined.</returns>
		/// <remarks>Does not check that the keyword is a valid identifier.</remarks>
		public static bool AddKeyword(IDictionary<string, Symbol> dic, string keyword)
		{
			if (dic.ContainsKey(keyword))
				return false;
			dic.Add(keyword, Symbol.Get("_" + keyword));
			return true;
		}
		/// <summary>Casts IEnumerable to ISimpleSource2, or, if that doesn't 
		/// work, creates and returns a new EnumerableSource that wraps around 
		/// the IEnumerable. 
		/// </summary>
		public static ISimpleSource2<AstNode> EnumerableToSource(IEnumerable<AstNode> e)
		{
			ISimpleSource2<AstNode> s = e as ISimpleSource2<AstNode>;
			if (s != null)
				return s;
			return new EnumerableSource<AstNode>(e);
		}

		/// <summary>Converts a parsed identifier to one that can be emitted in 
		/// source code or displayed in error messages.</summary>
		/// <param name="parsedId"></param>
		/// <param name="keywords">A set of keywords. This is required in order to 
		/// know when a normal identifier should be escaped; for example "int" 
		/// should be escaped in C#, but "foo" should not. This method assumes
		/// keywords are valid identifiers--don't put "keywords" in this list 
		/// that include special characters, or the identifier might not be 
		/// escaped properly.</param>
		/// <param name="csharpMode">Whether to use the C# "@" notation where 
		/// applicable. If this is false, all identifiers are escaped as 
		/// \"ident"; if this is true, @notation is used for identifiers that do 
		/// not contain special characters. Note: @notation will be used for 
		/// identifiers that start with a digit, such as @3D, even though these
		/// identifiers are not accepted in the (ECMA) C# standard.</param>
		/// <remarks>
		/// Normal identifiers like this_one are returned unchanged; identifiers
		/// with special characters such as this-one are escaped as \"this-one".
		/// </remarks>
		public static string UnparseID(string parsedId, ICollection<string> keywords, bool csharpMode)
		{
			if (parsedId.Length == 0)
				return "\\\"\"";
			
			// Look for characters that make this not a valid identifier
			int escapeMode = 0;
			if (parsedId[0] >= '0' && parsedId[0] <= '9')
				escapeMode = 1;
			if (keywords.Contains(parsedId))
				escapeMode = 1;
			else {
				for (int i = 0; i < parsedId.Length; i++)
					if (!IsIdentifierChar(parsedId[i])) {
						escapeMode = 2;
						break;
					}
			}
			if (escapeMode == 0)
				return parsedId;
			else if (escapeMode == 1) {
				if (csharpMode)
					return "@" + parsedId;
				else
					return "\\\"" + parsedId + "\"";
			} else {
				return "\\\"" + G.EscapeCStyle(
						parsedId, EscapeC.Control | EscapeC.DoubleQuotes
					) + "\"";
			}
		}

		public static bool IsIdentifierChar(char c)
		{
			return (c >= 'a' && c <= 'z')
				|| (c >= 'A' && c <= 'Z')
				|| (c >= '0' && c <= '9')
				|| (c == '_');
		}
	}
}
