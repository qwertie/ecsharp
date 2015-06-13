using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.MiniTest;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// A preprocessor usually inserted between the lexer and parser that inserts
	/// "indent", "dedent", and "end-of-line" tokens at appropriate places in a
	/// token stream.
	/// </summary>
	/// <remarks>This class will not work correctly if the lexer does not implement 
	/// <see cref="ILexer{T}.IndentLevel"/> properly.
	/// <para/>
	/// This class is abstract because it doesn't know how to create new tokens.
	/// The derived class must implement <see cref="MakeEndOfLineToken"/>,
	/// <see cref="MakeIndentToken"/> and <see cref="MakeDedentToken"/>. 
	/// <see cref="IndentTokenGenerator"/> is non-abstract version of this class 
	/// that creates <see cref="Loyc.Syntax.Lexing.Token"/> structures.
	/// <para/>
	/// Creation of indent, dedent, and end-of-line tokens can be suppressed inside 
	/// brackets, i.e. () [] {}. To accomplish this, set the <see cref="OpenBrackets"/> 
	/// and <see cref="CloseBrackets"/> properties.
	/// <para/>
	/// Note: EOL tokens are not generated for empty lines.
	/// <para/>
	/// Partial dedents ad unexpected indents, as in
	/// <code>
	///   if Condition:
	///       print("Hello")
	///     print("Hello again")
	///   else:
	///       print("Goodbye")
	///         print("Goodbye again")
	/// </code>
	/// will cause an error message to be written to the original lexer's 
	/// <see cref="ErrorSink"/>.
	/// </remarks>
	public abstract class IndentTokenGenerator<Token, TokenType> : LexerWrapper<Token>
		where Token : ISimpleToken<TokenType>
		where TokenType : IEquatable<TokenType>
	{
		/// <summary>Initializes the indent detector.</summary>
		/// <param name="lexer">Original lexer (either a raw lexer or an instance of another preprocessor such as <see cref="TokensToTree"/>.)</param>
		/// <param name="indentTriggers">A list of token types that trigger the insertion of an indentation token.</param>
		public IndentTokenGenerator(ILexer<Token> lexer, params TokenType[] indentTriggers) : base(lexer)
		{
			IndentTriggers = indentTriggers;
		}

		private TokenType[] _indentTrigger;
		public TokenType[] IndentTriggers
		{
			get { return _indentTrigger; }
			set { _indentTrigger = value ?? EmptyArray<TokenType>.Value; }
		}
		private TokenType[] _openBrackets;
		public TokenType[] OpenBrackets
		{
			get { return _openBrackets; }
			set { _openBrackets = value ?? EmptyArray<TokenType>.Value; }
		}
		private TokenType[] _closeBrackets;
		public TokenType[] CloseBrackets
		{
			get { return _closeBrackets; }
			set { _closeBrackets = value ?? EmptyArray<TokenType>.Value; }
		}
		/// <summary>If this option is true, then by default, a trigger token in
		/// the IndentTrigger list is suppressed and replaced by the token returned
		/// by <see cref="MakeIndentToken"/>. If this option is false, the trigger
		/// token is kept and followed by an indent token.</summary>
		public bool SuppressTriggerByDefault { get; set; }


		/// <summary>Returns a token to represent indentation, or null to suppress 
		/// generating an indent-dedent pair at this point.</summary>
		/// <param name="indentTrigger">The token that triggered this function call.</param>
		/// <param name="suppressTrigger">Whether to omit the trigger token from the
		/// output. This parameter is initialized to the value of the <see 
		/// cref="SuppressTriggerByDefault"/> property.</param>
		public abstract Maybe<Token> MakeIndentToken(Token indentTrigger, ref bool suppressTrigger);
		/// <summary>Returns a token to represent un-indentation.</summary>
		/// <param name="tokenAfterDedent">The first un-indented token, or the last
		/// token in the file if <c>atEof</c>.</param>
		/// <param name="atEof">If true, the end of the file has been reached.</param>
		public abstract Token MakeDedentToken(Token tokenAfterDedent, bool atEof);
		/// <summary>Returns a token to represent the end of a line, or null to
		/// avoid generating such a token.</summary>
		/// <param name="finalTokenInLine">Final token before the newline was 
		/// encountered.</param>
		/// <remarks>This function is also called at end-of-file, unless there are 
		/// no tokens in the file.</remarks>
		public abstract Maybe<Token> MakeEndOfLineToken(Token finalTokenInLine);
		
		public override Maybe<Token> NextToken()
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// A preprocessor usually inserted between the lexer and parser that inserts
	/// "indent", "dedent", and "end-of-line" tokens at appropriate places in a
	/// token stream.
	/// </summary>
	/// <remarks>
	/// Suppose you use an <see cref="IndentType"/> and <see cref="DedentType"/> 
	/// that are equal to the token types you've chosen for <c>{ braces }</c> (e.g.  
	/// (<see cref="TokenKind.LBrace"/> and <see cref="TokenKind.RBrace"/>), the 
	/// only indent trigger is a colon (:), and you set <see cref="EolType"/> to 
	/// the token type you're using for semicolons. Then the token stream from 
	/// input such as
	/// <code>
	///	def Sqrt(value):
	///		if value == 0: return 0
	///		g = 0; bshft = Log2Floor(value) >> 1;
	///		b = 1 &lt;&lt; bshft
	///		do:
	///			temp = (g + g + b) &lt;&lt; bshft
	///			if value >= temp: g += b
	///				value -= temp
	///			b >>= 1
	///		while (bshft-- > 0)
	///		return g
	/// </code>
	/// will be converted to a token stream equivalent to
	/// <code>
	///	def Sqrt(value): {
	///		if value == 0: { return 0;
	///		} g = 0; bshft = Log2Floor(value) >> 1;
	///		b = 1 &lt;&lt; bshft;
	///		do: {
	///			temp = (g + g + b) &lt;&lt; bshft
	///			if value >= temp: { g += b;
	///				value -= temp;
	///			} b >>= 1;
	///		} while (bshft-- > 0);
	///		return g;
	///	}</code>
	///	That is, a semicolon is added to lines that don't already have one, open 
	///	braces are inserted right after colons, and semicolons are <i>not</i> added 
	///	right after opening braces.
	///	<para/>
	/// If multiple indents occur on a single line, as in
	/// <code>
	/// if x: if y:
	///     Foo(x, y)
	/// </code>
	/// The output will be like this:
	/// <code>
	/// if x: { if y: {
	///     Foo(x, y);
	/// }}
	/// </code>
	/// </remarks>
	/// <seealso cref="IndentTokenGenerator{Token,TokenType}"/>
	public class IndentTokenGenerator : IndentTokenGenerator<Token, int>
	{
		int? _eolType;
		int _indentType, _dedentType;
		
		/// <summary>Gets or sets the token type of end-statement (a.k.a. end-of-line) 
		/// markers, cast to an integer as required by <see cref="Token"/>. Use 
		/// <c>null</c> to avoid generating such markers</summary>
		/// <remarks>Note: if the last token on a line has this same type, this 
		/// class will not generate an extra newline token.</remarks>
		public int? EolType
		{
			get { return _eolType; }
			set { _eolType = value; }
		}
		/// <summary>Gets or sets the token type of indentation markers, cast to an 
		/// integer as required by <see cref="Token"/>.</summary>
		public int IndentType
		{
			get { return _indentType; }
			set { _indentType = value; }
		}
		/// <summary>Gets or sets the token type of "unindent" markers, cast to an 
		/// integer as required by <see cref="Token"/>.</summary>
		public int DedentType
		{
			get { return _dedentType; }
			set { _dedentType = value; }
		}
		
		/// <summary>Initializes the indent detector.</summary>
		/// <param name="lexer">Original lexer (either a raw lexer or an instance of <see cref="TokensToTree"/>.)</param>
		/// <param name="indentTriggers">A list of token types that trigger the insertion of an indentation token.</param>
		/// <param name="eolType">Token type of end-statement markers inserted when
		/// newlines are encountered, or null to avoid generating such markers.</param>
		/// <param name="indentType">Token type of indentation markers</param>
		/// <param name="dedentType">Token type of un-indent markers</param>
		public IndentTokenGenerator(ILexer<Token> lexer, int[] indentTriggers, int? eolType, int indentType = (int)TokenKind.Indent, int dedentType = (int)TokenKind.Dedent) 
			: base(lexer, indentTriggers) {
			_indentType = indentType;
			_dedentType = dedentType;
			_eolType = eolType;
		}

		public override Maybe<Token> MakeIndentToken(Token indentTrigger, ref bool suppressTrigger)
		{
			return new Token(_indentType, indentTrigger.EndIndex, 0, null);
		}
		public override Token MakeDedentToken(Token tokenAfterDedent, bool atEof)
		{
			return new Token(_dedentType, tokenAfterDedent.StartIndex, 0, null);
		}
		public override Maybe<Token> MakeEndOfLineToken(Token finalTokenInLine)
		{
			if (_eolType == null || finalTokenInLine.TypeInt == _eolType.Value)
				return NoValue.Value;
			return new Token(_eolType.Value, finalTokenInLine.EndIndex, 0, null);
		}
	}

	[TestFixture]
	public class IndentTokenGeneratorTests
	{
		
	}
}
