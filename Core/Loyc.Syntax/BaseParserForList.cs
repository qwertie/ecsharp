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
	/// </summary>
	/// <remarks>
	/// This version of BaseParserForList has List as a generic parameter. Compared 
	/// to using IList{Token} directly, this can increase performance in case 
	/// the list is a value type (e.g. <c>InternalList&lt;Token></c>).
	/// </remarks>
	public abstract class BaseParserForList<Token, TokenList> : BaseParser<Token>
		where Token : ISimpleToken
		where TokenList : IList<Token>
	{
		/// <summary>Initializes this object to begin parsing the specified tokens.</summary>
		/// <param name="list">A list of tokens that the derived class will parse.</param>
		/// <param name="eofToken">A token value to return when the input position 
		/// reaches the end of the token list.</param>
		/// <param name="file">A source file object that will be returned by the <see cref="SourceFile"/>
		/// property. By default, this object is used to get the file name, line 
		/// number and column number shown in parser errors. If you are using 
		/// <see cref="BaseLexer"/>, you can get this object from the
		/// <see cref="BaseLexer{C}.SourceFile"/> property. The <see cref="SourceFile"/>
		/// property (in this class) will return this value. It can be null, which
		/// means that default error messages will show the character index instead
		/// of the file, line number and column number.</param>
		/// <param name="startIndex">The initial index from which to start reading
		/// tokens from the list (normally 0).</param>
		protected BaseParserForList(TokenList list, Token eofToken, ISourceFile file, int startIndex = 0) : base(file, startIndex)
		{
			Reset(list, eofToken, file);
		}
		protected void Reset(TokenList list, Token eofToken, ISourceFile file, int startIndex = 0)
		{
			EofToken = eofToken;
			EOF = EofToken.TypeInt;
			_list = list;
			_sourceFile = file;
			_inputPosition = startIndex;
		}

		protected Token EofToken;
		protected Int32 EOF; // EofToken.TypeInt

		/// <summary>The IList{Token} that was provided to the constructor, if any.</summary>
		protected TokenList List { get { return _list; } }
		private   TokenList _list;
		// cached list size to avoid frequently calling the virtual Count property.
		// (don't worry, it's updated automatically by LT() if the list size changes)
		private int _listCount;

		protected sealed override Int32 EofInt() { return EOF; }
		protected sealed override Int32 LA0Int { get { return _lt0.TypeInt; } }
		protected sealed override Token LT(int i)
		{
			i += InputPosition;
			if ((uint)i < (uint)_listCount || (uint)i < (uint)(_listCount = _list.Count)) {
				try {
					return _list[i];
				} catch {
					_listCount = _list.Count;
				}
			}
			return EofToken;
		}
		
		/// <summary>Returns a string representation of the specified token type.
		/// These strings are used in error messages.</summary>
		protected override abstract string ToString(Int32 tokenType);

		protected new int InputPosition
		{
			[DebuggerStepThrough]
			get { return _inputPosition; }
			set {
				_inputPosition = value;
				_lt0 = LT(0);
			}
		}
	}
	
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator) and receive tokens from any <see cref="IEnumerator{Token}"/>.
	/// </summary>
	/// <remarks>
	/// This base class for LLLPG parsers reads tokens from <see cref="IList{Token}"/>,
	/// but you can also pass an <see cref="IEnumerable{Token}"/> or 
	/// <see cref="IEnumerator{Token}"/> to the constructor and it will use
	/// <see cref="BufferedSequence{T}"/> to convert the sequence to a list.
	/// </remarks>
	public abstract class BaseParserForList<Token> : BaseParserForList<Token, IList<Token>>
		where Token : ISimpleToken
	{
		/// <inheridoc/>
		protected BaseParserForList(IList<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: base(list, eofToken, file, startIndex) { }
		/// <inheridoc/>
		protected BaseParserForList(IEnumerable<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
		/// <inheridoc/>
		protected BaseParserForList(IEnumerator<Token> list, Token eofToken, ISourceFile file, int startIndex = 0) 
			: this(list.Buffered(), eofToken, file, startIndex) { }
	}
}
