using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax
{
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator) and receive tokens from any <see cref="IEnumerator{Token}"/>.
	/// (Potentially also useful for parsers written by hand.)
	/// </summary>
	/// <remarks>
	/// The compiler will ensure that you use this base class correctly. All you 
	/// have to do is call the base class constructor and override the abstract 
	/// method <see cref="ToString(MatchType)"/>.
	/// <para/>
	/// This version of BaseParserForList has <c>List</c> (a token list) as a 
	/// generic parameter. Compared to using <c>IList{Token}</c> directly, this 
	/// can increase performance in case the <c>List</c> is a value type (e.g. 
	/// <c>InternalList&lt;Token></c>).
	/// </remarks>
	/// <typeparam name="Token">Data type of complete tokens in the token list. A 
	/// token contains the type of a "word" in the program (string, identifier, plus 
	/// sign, etc.), a value (e.g. the name of an identifier), and a range of 
	/// characters in the source file. See <see cref="ISimpleToken{MatchType}"/>.
	/// Note: Token is usually a small struct; this class assumes that it will 
	/// never be null.</typeparam>
	/// <typeparam name="MatchType">A data type, usually int, that represents a 
	/// token type (identifier, operator, etc.) and implements <see cref="IEquatable{T}"/>
	/// so it can be compared for equality with other token types; this is also the 
	/// type of the <see cref="ISimpleToken{Matchtype}.Type"/> property. <c>MatchType</c>
	/// cannot be an enum because an enum does not implement <see cref="IEquatable{T}"/>.</typeparam>
	/// <typeparam name="List">Data type of the list that contains the tokens (one 
	/// often uses IList{Token}, but one could use <see cref="Loyc.Collections.Impl.InternalList{T}"/> 
	/// for potentially higher performance.)</typeparam>
	public abstract class BaseParserForList<Token, MatchType, List> : BaseParser<Token, MatchType>
		where Token : ISimpleToken<MatchType>
		where MatchType : IEquatable<MatchType>
		where List : IList<Token>
	{
		/// <summary>Initializes this object to begin parsing the specified tokens.</summary>
		/// <param name="list">A list of tokens that the derived class will parse.</param>
		/// <param name="eofToken">A token value to return when the input position 
		/// reaches the end of the token list. Ideally <c>eofToken.StartIndex</c>
		/// would contain the position of EOF, but the base class method
		/// <see cref="BaseParser{Tok,MT}.LaIndexToCharIndex"/> does not trust this
		/// value, and will ensure that the character index returned for EOF is at 
		/// least as large as the character index of the last token in the file. 
		/// This means that it is safe to set <c>ISimpleToken{MatchType}.StartIndex</c> 
		/// to 0 in the EOF token, because when an error message involves EOF, the
		/// base class will find a more accurate EOF position.</param>
		/// <param name="file">A source file object that will be returned by the 
		/// <see cref="SourceFile"/> property. By default, this object is used to 
		/// get the file name, line number and column number shown in parser errors. 
		/// If your lexer uses <see cref="BaseLexer"/> or <see cref="LexerSource"/>, 
		/// you can get this object from the <see cref="BaseLexer{C}.SourceFile"/> 
		/// property. The <see cref="SourceFile"/> property (in this class) will 
		/// return this value. If this parameter is null, then by default, error 
		/// messages will only show the character index instead of the file, line 
		/// number and column number.</param>
		/// <param name="startIndex">The initial index from which to start reading
		/// tokens from the list (normally 0).</param>
		protected BaseParserForList(List list, Token eofToken, ISourceFile file, int startIndex = 0) : base(file, startIndex)
		{
			Reset(list, eofToken, file, startIndex);
		}

		/// <summary>Reinitializes the object. This method is called by the constructor.</summary>
		/// <remarks>See the constructor for documentation of the parameters.</remarks>
		protected virtual void Reset(List list, Token eofToken, ISourceFile file, int startIndex = 0)
		{
			CheckParam.IsNotNull<object>("list", list);
			EofToken = eofToken;
			EOF = EofToken.Type;
			_tokenList = list;
			_listCount = list.Count; // to avoid 1st-chance exceptions
			_sourceFile = file;
			InputPosition = startIndex;
		}
		protected void Reset()
		{
			Reset(TokenList, EofToken, SourceFile);
		}

		protected Token EofToken;

		/// <summary>The IList{Token} that was provided to the constructor, if any.</summary>
		/// <remarks>Note: if you are starting to parse a new source file, you should call 
		/// <see cref="Reset"/> instead of setting this property.</remarks>
		protected List TokenList { get { return _tokenList; } }
		protected List _tokenList;
		// cached list size to avoid frequently calling the virtual Count property.
		// (don't worry, it's updated automatically by LT() if the list size changes)
		private int _listCount;

		protected sealed override MatchType EofInt() { return EOF; }
		protected sealed override MatchType LA0Int { get { return _lt0.Type; } }
		protected sealed override Token LT(int i)
		{
			i += InputPosition;
			if ((uint)i < (uint)_listCount || (uint)i < (uint)(_listCount = _tokenList.Count)) {
				try {
					return _tokenList[i];
				} catch {
					_listCount = _tokenList.Count;
				}
			}
			return EofToken;
		}

		protected MatchType LA(int i) { return LT(i).Type; }
		protected MatchType LA0 { get { return _lt0.Type; } }
		
		/// <summary>Returns a string representation of the specified token type.
		/// These strings are used in error messages.</summary>
		protected override abstract string ToString(MatchType tokenType);

		protected new int InputPosition
		{
			[DebuggerStepThrough]
			get { return _inputPosition; }
			set {
				_inputPosition = value;
				_lt0 = LT(0);
			}
		}

		#region Down & Up
		// These are used to traverse into token subtrees if you are using the
		// TokensToTree preprocessor, e.g. given w=(x+y)*z, the outer token list is 
		// w=()*z, and the 3 tokens x+y are children of '('.
		// So the parser calls something like Down(lparen) to begin parsing inside 
		// the parentheses, then it calls Up() to return to the parent tree.

		private Stack<KeyValuePair<List, int>> _parents;

		/// <summary>Switches to parsing the specified token list at position zero
		/// (typically the value of <see cref="Loyc.Syntax.Lexing.Token.Children"/> 
		/// in a token tree produced by <see cref="TokensToTree"/>.) The original 
		/// token list and the original <see cref="InputPosition"/> are placed on a 
		/// stack, so you can restore the old list by calling <see cref="Up()"/>.</summary>
		/// <returns>True if successful, false if <c>children</c> is null.</returns>
		protected bool Down(List children)
		{
			if (children != null)
			{
				if (_parents == null)
					_parents = new Stack<KeyValuePair<List, int>>();
				_parents.Push(new KeyValuePair<List,int>(_tokenList, InputPosition));
				_tokenList = children;
				InputPosition = 0;
				return true;
			}
			return false;
		}
		/// <summary>Returns to the old token list saved by <see cref="Down"/>.</summary>
		protected void Up()
		{
			Debug.Assert(_parents.Count > 0);
			var pair = _parents.Pop();
			_tokenList = pair.Key;
			InputPosition = pair.Value;
		}
		/// <summary>Calls <see cref="Up()"/> and returns <c>value</c>.</summary>
		protected T Up<T>(T value)
		{
			Up();
			return value;
		}

		#endregion
	}
	
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator) and receive tokens from any <see cref="IEnumerator{Token}"/>.
	/// </summary>
	/// <remarks>
	/// This base class for LLLPG parsers reads tokens from <see cref="IList{Token}"/>,
	/// but you can also pass an <see cref="IEnumerable{Token}"/> or 
	/// <see cref="IEnumerator{Token}"/> to the constructor and it will 
	/// convert it to a list, lazily, using <see cref="BufferedSequence{T}"/>.
	/// </remarks>
	public abstract class BaseParserForList<Token, MatchType> : BaseParserForList<Token, MatchType, IList<Token>>
		where Token : ISimpleToken<MatchType>
		where MatchType : IEquatable<MatchType>
	{
		/// <inheridoc/>
		protected BaseParserForList(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0)
			: base(list, eofToken, file, startIndex) { }
		protected BaseParserForList(IListAndListSource<Token> list, Token eofToken, ISourceFile file, int startIndex = 0)
			: base(list, eofToken, file, startIndex) { }
		protected BaseParserForList(IListSource<Token> list, Token eofToken, ISourceFile file, int startIndex = 0)
			: base(list.AsList(), eofToken, file, startIndex) { }
		/// <inheridoc/>
		protected BaseParserForList(IEnumerable<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
		/// <inheridoc/>
		protected BaseParserForList(IEnumerator<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
	}
}
