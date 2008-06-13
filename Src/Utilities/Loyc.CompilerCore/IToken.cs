using System;
using System.Collections.Generic;
using System.Text;
//using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	/// <summary>
	/// This is Loyc's standard token interface. In addition to the methods in ICodeNode, 
	/// IAttributes(of object), and ITokenContent, ITokens form a linked list, have 
	/// properties describing their location in the source code text, and have a 
	/// VisibleToParser property which distinguishes code from whitespace and comment
	/// tokens.
	/// </summary>
	public interface IToken : IBaseNode
	{
		/// <summary>Returns the parsed value of the token, such as a string, an
		/// integer, a code node, or something else depending on the token type. If
		/// the token does not need to be parsed, or is not able to parse itself, it may
		/// return null. The implementor can decide whether the setter should work; it 
		/// may throw an exception instead.</summary>
		object Content { get; set; }

		/// <summary>Gets or sets the list of tokens nested inside this one.</summary>
		/// <remarks>
		/// Conventionally, token lists are not hierarchical, but in 
		/// Loyc, the Essential Tree Parser makes a token tree. This is explained
		/// in the Loyc design overview under "Essential tree parsing (ETP)".
		/// 
		/// Block should be null by default. The ETP sets this to a list of children 
		/// only for certain token types that can have children (e.g. :PARENS,
		/// :BRACES).
		/// </remarks>
		IList<IToken> Block { get; set; }

		/// <summary>The source from which the token text can be obtained. If
		/// the token is synthetic, the CharSource can be null.</summary>
		ICharSource CharSource { get; }

		/// <summary>CharSource.Substring(StartIndex, Length) should equal Text, 
		/// unless the token is synthetic.</summary>
		int StartIndex { get; }
		/// <summary>CharSource.Substring(StartIndex, Length) should equal Text,
		/// unless the token is synthetic.</summary>
		int Length { get; }

		/// <summary>Returns the number of spaces following this token, or 0 if
		/// a :WS token is used to represent those spaces.</summary>
		/// <remarks>Tokens can use up a lot of memory, and if spaces are
		/// represented by WS tokens, they may make up a substantial portion of all
		/// tokens due to the convention of writing expressions like X + Y * Z that
		/// contain many spaces. To save memory, SpacesAfter specifies a count of
		/// spaces following the token in lieu of a WS token. These spaces are not
		/// included in the token's Text or Length properties.</remarks>
		int SpacesAfter { get; }

		/// <summary>Returns whether this token should be processed by the parser.
		/// </summary><remarks>
		/// Invisible tokens usually include whitespace, comments, and 
		/// LINE_CONTINUATION tokens. BTW, typically, the filter that removes 
		/// "invisible" tokens is separate from the parser. There is no setter 
		/// because visibility is normally determined internally by the lexer.
		/// </remarks>
		bool VisibleToParser { get; }

		/*/// <summary>Gets or sets the token adjacent to this one in the source 
		/// text. This method returns hidden tokens as well as visible ones.
		/// </summary><remarks>
		/// The setters are only to be used by LexicallyInsertAfter() and 
		/// LexicallyUnlink(). The setters may verify that this is the case with 
		/// code like the following:
		/// <code>
		/// public IToken LexicalNext { 
		/// 	get { return _lexNext; }
		/// 	set { 
		/// 		Debug.Assert((value != null && value.LexicalPrev == this) ||
		/// 		             (_lexNext != null && _lexNext.LexicalPrev == null));
		/// 		_lexNext = value;
		/// 	}
		/// }
		/// public IToken LexicalPrev {
		/// 	get { return _lexPrev; }
		/// 	set { 
		/// 		Debug.Assert((value != null && value.LexicalNext == this) ||
		/// 		             (_lexPrev != null && _lexPrev.LexicalNext == null));
		/// 		_lexPrev = value;
		/// 	}
		/// }
		/// </code>
		/// </remarks>
		IToken LexicalNext { get; set; }
		IToken LexicalPrev { get; set; }
		
        /// <summary>Inserts this token after another in the lexical linked
        /// list.</summary>
        /// <remarks>This method must call Unlink() before performing the insertion
        /// if it is already in a list. It must also set its "LexicalPrev" and
        /// "LexicalNext" properties before setting the corresponding properties on
        /// the tokens to which it is linking, because those setters may check that
        /// this has been done. As an optimization, this method may do nothing if is
        /// is already after the specified token.
        /// </remarks>
		void LexicallyInsertAfter(IToken other);
		
		/// <summary>Removes this token from a linked list</summary>
		/// <remarks>The token must set its own links to null before setting 
		/// the properties of the tokens to which it was linked, because the
		/// setters may verify that their former links have null backlinks.</remarks>
		void LexicallyUnlink();*/
	}

}
