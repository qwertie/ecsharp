using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Collections.MutableListExtensionMethods;
using Loyc.Threading;
using Loyc.Collections.Impl;

namespace Loyc.Syntax.Lexing
{
	/// <summary>
	/// A common token type recommended for Loyc languages that want to use 
	/// features such as token literals or the <see cref="TokensToTree"/> class.
	/// </summary>
	/// <remarks>
	/// For performance reasons, a Token ought to be a structure rather than
	/// a class. But if Token is a struct, we have a conundrum: how do we support 
	/// tokens from different languages? We can't use inheritance since structs
	/// do not support it. When EC# is ready, we could use a single struct plus
	/// an alias for each language, but of course this structure predates the 
	/// implementation of EC#.
	/// <para/>
	/// Luckily, tokens in most languages are very similar. A four-word structure
	/// generally suffices:
	/// <ol>
	/// <li><see cref="TypeInt"/>: each language can use a different set of token types 
	/// represented by a different <c>enum</c>. All enums can be converted to 
	/// an integer, so <see cref="Token"/> uses Int32 as the token type. In order
	/// to support DSLs via token literals (e.g. LLLPG is a DSL inside EC#), the
	/// TypeInt should be based on <see cref="TokenKind"/>.</li>
	/// <li><see cref="Value"/>: this can be any object. For literals, this should 
	/// be the actual value of the literal, for whitespace it should be 
	/// <see cref="WhitespaceTag.Value"/>, etc. See <see cref="Value"/> for 
	/// the complete list.</li>
	/// <li><see cref="StartIndex"/>: location in the original source file where 
	/// the token starts.</li>
	/// <li><see cref="Length"/>: length of the token in the source file (32 bits).</li>
	/// </ol>
	/// Since 64-bit platforms are very common, the Value is 64 bits, and padding
	/// increases the structure size from 16 bytes to 24. Given this reality, 
	/// I decided to fill in the 4 bytes of padding with additional information:
	/// <ol>
	/// <li><see cref="Style"/>: 8 bits of style information, e.g. it can be used to
	///    mark whether integer literals use hexadecimal, binary or decimal format.</li>
	/// <li>TextValue range: Tokens are able to keep track of two values, one in 
	///    the <see cref="Value"/> property and one returned from <see cref="TextValue(ICharSource)"/>.
	///    16 bits of information enables the TextValue feature to work without 
	///    memory allocation in many cases; see the documentation of the constructor
	///    <see cref="Token(int, int, UString, NodeStyle, object, int, int)"/> for
	///    more information about the purpose and usage of this feature.
	/// </ol>
	/// To save space (and because .NET doesn't handle large structures well),
	/// tokens do not know what source file they came from and cannot convert 
	/// their location to a line number. For this reason, one should keep a
	/// reference to the <see cref="ISourceFile"/> and call <see cref="IIndexToLine.IndexToLine(int)"/> 
	/// to get the source location.
	/// <para/>
	/// A generic token also cannot convert itself to a properly-formatted 
	/// string. The <see cref="ToString"/> method does allow you to provide an optional
	/// reference to <see cref="ICharSource"/> which allows the token to get its
	/// original text, and in any case you can call <see cref="SetToStringStrategy"/>
	/// to control the method by which a token converts itself to a string.
	/// <para/>
	/// Fun fact: originally I planned to use <see cref="Symbol"/> as the common 
	/// token type, because it is extensible and could nicely represent tokens in 
	/// all languages; unfortunately, Symbol may reduce parsing performance 
	/// because it cannot be used with the switch opcode (i.e. the switch 
	/// statement in C#), so I decided to switch to integers instead and to 
	/// introduce the concept of <see cref="TokenKind"/>, which is derived from 
	/// <see cref="Type"/> using <see cref="TokenKind.KindMask"/>.
	/// Each language should have, in the namespace of that language, an
	/// extension method <c>public static TokenType Type(this Token t)</c> that 
	/// converts the TypeInt to the enum type for that language.
	/// </remarks>
	public struct Token : IListSource<Token>, IToken<int>, IEquatable<Token>
	{
		/// <summary>Initializes the Token structure.</summary>
		/// <param name="type">Value of <see cref="TypeInt"/></param>
		/// <param name="startIndex">Value of <see cref="StartIndex"/></param>
		/// <param name="length">Value of <see cref="Length"/></param>
		/// <param name="style">Value of <see cref="Style"/></param>
		/// <param name="value">Value of <see cref="Value"/></param>
		public Token(int type, int startIndex, int length, NodeStyle style = 0, object value = null)
		{
			_typeInt = type;
			_style = style;
			_substringOffset = _substringOffsetFromEnd = 0;
			_startIndex = startIndex;
			_length = length;
			_value = value;
		}

		/// <inheritdoc cref="Token(ushort, int, int, NodeStyle, object)"/>
		public Token(int type, int startIndex, int length, object value)
		{
			_typeInt = type;
			_style = 0;
			_substringOffset = _substringOffsetFromEnd = 0;
			_startIndex = startIndex;
			_length = length;
			_value = value;
		}

		/// <summary>Initializes a kind of token designed to store two parts of a 
		/// literal without allocating extra memory (see the Remarks for details).
		/// </summary>
		/// <param name="type">Value of <see cref="TypeInt"/></param>
		/// <param name="startIndex">Value of <see cref="StartIndex"/></param>
		/// <param name="tokenText">A substring of the token in the original source 
		/// file, such that <see cref="Length"/> will be <c>tokenText.Length</c> 
		/// and <c>tokenText.Substring(valueStart - startIndex, valueEnd - valueStart)</c> 
		/// will be returned from <see cref="TextValue(ICharSource)"/>. For correct
		/// results, the <see cref="ICharSource"/> passed to TextValue later needs 
		/// to represent the same string that was used to produce this parameter.</param>
		/// <param name="style">Value of <see cref="Style"/></param>
		/// <param name="value">Value of <see cref="Value"/></param>
		/// <param name="substringStart">Index where the TextValue starts in the source 
		/// code; should be equal to or greater than startIndex.</param>
		/// <param name="substringEnd">Index where the TextValue ends in the source 
		/// code; should be equal to or less than startIndex + tokenText.Length.</param>
		/// <remarks>
		/// Literals in many languages can be broken into two textual parts: their 
		/// type and their value. For example, in some languages you can write 123.5f,
		/// where "f" indicates that the floating-point value has a size of 32 bits.
		/// C++ strings have up to three parts, as in <c>u"Hello"_UD</c>: <c>u</c>
		/// indicates the character type (u = 16-bit unicode) while <c>_UD</c> 
		/// indicates that the string should be interpreted in a user-defined way.
		/// In LES3, all literals have two parts: value text and a type marker. For 
		/// example, 123.5f has a text "123.5" and type marker "_f"; greeting"Hello" 
		/// has text "Hello" and type marker "greeting"; and a simple number like 123 
		/// has text "123" and type marker "_".
		/// <para/>
		/// This constructor allows you to represent up to two "values" in a single 
		/// token without necessarily allocating memory for them, even though Tokens 
		/// only contain a single heap reference (<see cref="Value"/>). When calling
		/// this constructor, the second value, called the "TextValue", must be a 
		/// substring of the token's original source text; for example given the token 
		/// <c>"Hello"</c>, the tokenizer would use <c>Hello</c> as the TextValue. 
		/// Rather than allocating a string "Hello" and storing it in the token, you 
		/// can use this constructor to record the fact that the string <c>Hello</c> 
		/// begins one character after the beginning of the token (<c>valueStart = 1</c>)
		/// and one character before the end of the token 
		/// (<c>valueEnd = startIndex + tokenText.Length - 1</c>).
		/// <para/>
		/// Since a Token does not have a reference to its own source file (<see 
		/// cref="ISourceFile"/>), the language parser will need to use the <see 
		/// cref="TextValue(ICharSource)"/> method to retrieve the value text later.
		/// <para/>
		/// <see cref="Token"/> is a small structure that allocates only 4 bits for 
		/// each offset (8 bits total). If either offset is greater than 15, the 
		/// TextValue is combined with the value in a heap object of type 
		/// <see cref="Tuple{object, UString}"/>, but this is a hidden implementation 
		/// detail - the <see cref="Value"/> property simply returns the object.
		/// <para/>
		/// For strings that contain escape sequences, such as "Hello\n", you may
		/// prefer to store a parsed version of the string in the Token. There is
		/// another constructor for this purpose, which always allocates memory:
		/// <see cref="Token(ushort, int, int, NodeStyle, object, UString)"/>.
		/// </remarks>
		public Token(int type, int startIndex, UString tokenText, NodeStyle style, object value, int substringStart, int substringEnd)
		{
			_startIndex = startIndex;
			_length = tokenText.Length;
			_value = value; // may change
			_typeInt = type;
			_style = style;

			Debug.Assert(substringStart >= startIndex && substringEnd <= startIndex + tokenText.Length);
			int substringOffset = substringStart - startIndex;
			int substringOffsetFromEnd = startIndex + tokenText.Length - substringEnd;
			if ((uint)substringOffset < 255 && (uint)substringOffsetFromEnd <= 255)
			{
				_substringOffset = (byte)substringOffset;
				_substringOffsetFromEnd = (byte)substringOffsetFromEnd;
			}
			_substringOffset = _substringOffsetFromEnd = 0xFF;
			_value = new Tuple<object, UString>(value, tokenText.Slice(substringStart, substringEnd - substringStart));
		}

		/// <summary>Initializes a kind of token designed to store two parts of a 
		/// literal (see the Remarks for details).</summary>
		/// <param name="type">Value of <see cref="TypeInt"/></param>
		/// <param name="startIndex">Value of <see cref="StartIndex"/></param>
		/// <param name="length">Value of <see cref="Length"/></param>
		/// <param name="style">Value of <see cref="Style"/></param>
		/// <param name="value">Value of <see cref="Value"/></param>
		/// <param name="textValue">Value returned from <see cref="TextValue(ICharSource)"/>.</param>
		/// <remarks>
		/// As explained in the documentation of the other constructor 
		/// (<see cref="Token(ushort, int, UString, NodeStyle, object, int, int)"/>,
		/// some literals have two parts which we call the Value and the TextValue.
		/// Since the Token structure only contains a single heap reference, 
		/// this contructor combines Value with TextValue in a heap object of type 
		/// <see cref="Tuple{object, UString}"/>, but this is a hidden implementation 
		/// detail; the <see cref="Value"/> property simply returns the object.
		/// </remarks>
		public Token(int type, int startIndex, int length, NodeStyle style, object value, UString textValue)
		{
			_typeInt = type;
			_style = style;
			_substringOffset = _substringOffsetFromEnd = 0xFF;
			_startIndex = startIndex;
			_length = length;
			_value = new Tuple<object, UString>(value, textValue);
		}

		private Token(int typeInt, int startIndex, int length, object value, NodeStyle style, byte substringOffset, byte substringOffsetFromEnd)
		{
			_typeInt = typeInt;
			_startIndex = startIndex;
			_length = length;
			_style = style;
			_substringOffset = substringOffset;
			_substringOffsetFromEnd = substringOffsetFromEnd;
			_value = value;
		}

		private readonly int _typeInt;
		private readonly int _startIndex;
		private readonly int _length;
		private readonly NodeStyle _style;
		private readonly byte _substringOffset, _substringOffsetFromEnd;
		private object _value;

		/// <summary>Token type.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int TypeInt => _typeInt;

		/// <summary>Token category. This value is only meaningful if the token type
		/// integers are based on <see cref="TokenKind"/>s. Token types for LES and 
		/// Enhanced C# are, indeed, based on <see cref="TokenKind"/>.</summary>
		public TokenKind Kind { get { return ((TokenKind)TypeInt & TokenKind.KindMask); } }

		/// <summary>Location in the orginal source file where the token starts, or
		/// -1 for a synthetic token.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int StartIndex => _startIndex;

		/// <summary>Length of the token in the source file, or 0 for a synthetic 
		/// or implied token.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Length => _length;
		/// <summary>8 bits of nonsemantic information about the token. The style 
		/// is used to distinguish hex literals from decimal literals, or triple-
		/// quoted strings from double-quoted strings.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public NodeStyle Style => _style;

		/// <summary>The parsed value of the token.</summary>
		/// <remarks>Recommended ways to use this field:
		/// <ul>
		/// <li>For strings: the parsed value of the string (no quotes, escape 
		/// sequences removed), i.e. a boxed char or a string. A backquoted 
		/// string in EC#/LES is converted to a <see cref="Symbol"/> because it 
		/// is a kind of operator.</li>
		/// <li>For numbers: the parsed value of the number (e.g. 4 => int, 4L => long, 4.0f => float)</li>
		/// <li>For identifiers: the parsed name of the identifier, as a Symbol 
		/// (e.g. x => x, @for => for, @`1+1` => <c>1+1</c>)</li>
		/// <li>For any keyword including AttrKeyword and TypeKeyword tokens: a 
		/// Symbol containing the name of the keyword, with "#" prefix</li>
		/// <li>For punctuation and operators: the text of the punctuation as a 
		/// Symbol.</li>
		/// <li>For openers (open paren, open brace, etc.): null for normal linear
		/// parsers. If the tokens have been processed by <see cref="TokensToTree"/>,
		/// this will be a <see cref="TokenTree"/>.</li>
		/// <li>For spaces and comments: for performance reasons, it is not 
		/// recommended to extract the text of whitespace from the source file; 
		/// instead, use <see cref="WhitespaceTag.Value"/></li>
		/// <li>When no value is needed (because the Type() is enough): null</li>
		/// </ul>
		/// </remarks>
		public object Value {
			get {
				if (_substringOffset != 0xFF)
					return _value;
				else
					return((Tuple<object, UString>)_value).Item1;
			}
			set {
				if (_substringOffset != 0xFF)
					_value = value;
				else
					_value = new Tuple<object, UString>(value, ((Tuple<object, UString>)_value).Item2);
			}
		}
		
		/// <summary>Returns Value as TokenTree (null if not a TokenTree).</summary>
		public TokenTree Children { get { return Value as TokenTree; } }

		/// <summary>Returns StartIndex + Length.</summary>
		public int EndIndex { get { return StartIndex + Length; } }

		/// <summary>Returns true if Value == <see cref="WhitespaceTag.Value"/>.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsWhitespace { get { return Value == WhitespaceTag.Value; } }
		
		/// <summary>Returns true if the specified type and value match this token.</summary>
		public bool Is(int type, object value) { return type == TypeInt && object.Equals(value, Value); }

		static readonly ThreadLocalVariable<Func<Token, ICharSource, string>> ToStringStrategyTLV = new ThreadLocalVariable<Func<Token,ICharSource,string>>(Loyc.Syntax.Les.TokenExt.ToString);
		public static SavedValue<Func<Token, ICharSource, string>> SetToStringStrategy(Func<Token, ICharSource, string> newValue)
		{
			return new SavedValue<Func<Token, ICharSource, string>>(ToStringStrategyTLV, newValue);
		}

		/// <summary>Gets or sets the strategy used by <see cref="ToString"/>.</summary>
		public static Func<Token, ICharSource, string> ToStringStrategy
		{
			get { return ToStringStrategyTLV.Value; }
			set { ToStringStrategyTLV.Value = value ?? Loyc.Syntax.Les.TokenExt.ToString; }
		}

		/// <summary>Gets the <see cref="SourceRange"/> of a token, under the 
		/// assumption that the token came from the specified source file.</summary>
		public SourceRange Range(ISourceFile sf)
		{
			return new SourceRange(sf, StartIndex, Length);
		}
		public SourceRange Range(ILexer<Token> l) { return Range(l.SourceFile); }

		/// <summary>Gets the original source text for a token if available, under the 
		/// assumption that the specified source file correctly specifies where the
		/// token came from. If the token is synthetic, returns <see cref="UString.Null"/>.</summary>
		public UString SourceText(ICharSource chars)
		{
			if ((uint)StartIndex <= (uint)chars.Count)
				return chars.Slice(StartIndex, Length);
			return UString.Null;
		}
		public UString SourceText(ILexer<Token> l) => SourceText(l.SourceFile.Text);

		/// <summary>Helps get the "text value" from tokens that used one of the 
		/// constructors designed to support this use case, e.g.
		/// <see cref="Token(int type, int startIndex, UString tokenText, NodeStyle style, object value, int valueStart, int valueEnd)"/>.
		/// If one of the other constructors was used, this function returns the same
		/// value as <see cref="SourceText(ICharSource)"/>.</summary>
		/// <param name="chars">Original source code or lexer from which this token was derived.</param>
		public UString TextValue(ICharSource source)
		{
			if (_substringOffset == 0xFF)
				return ((Tuple<object, UString>)Value).Item2;
			return source.Slice(StartIndex + _substringOffset, Length - _substringOffset - _substringOffsetFromEnd);
		}
		/// <inheritdoc cref="TextValue(ICharSource)"/>
		public UString TextValue(ILexer<Token> source) => TextValue(source.SourceFile.Text);

		#region ToString, Equals, GetHashCode

		/// <summary>Reconstructs a string that represents the token, if possible.
		/// Does not work for whitespace and comments, because the value of these
		/// token types is stored in the original source file and for performance 
		/// reasons is not copied to the token.</summary>
		/// <remarks>
		/// This does <i>not</i> return the original source text; it uses the stringizer 
		/// in <see cref="ToStringStrategy"/>, which can be overridden with language-
		/// specific behavior by calling <see cref="SetToStringStrategy"/>.
		/// <para/>
		/// The returned string, in general, will not match the original
		/// token, since the <see cref="ToStringStrategy"/> does not have access to
		/// the original source file.
		/// </remarks>
		public override string ToString() => ToStringStrategy(this, null);
		
		/// <summary>Gets the original text of the token, if you provide a reference
		/// to the original source code text. Note: the method used to convert the
		/// token to a string can be overridden with <see cref="SetToStringStrategy"/>.</summary>
		public string ToString(ICharSource sourceText) => ToStringStrategy(this, sourceText);

		public override bool Equals(object obj)
		{
			return obj is Token && Equals((Token)obj);
		}
		/// <summary>Equality depends on TypeInt and Value, but not StartIndex and 
		/// Length (this is the same equality condition as <see cref="LNode"/>).</summary>
		public bool Equals(Token other)
		{
			return TypeInt == other.TypeInt && object.Equals(Value, other.Value);
		}
		public override int GetHashCode()
		{
			int hc = TypeInt;
			if (Value != null)
				hc ^= Value.GetHashCode();
			return hc;
		}

		#endregion

		#region IListSource<Token> Members

		public Token this[int index]
		{
			get { return Children[index]; }
		}
		public Token TryGet(int index, out bool fail)
		{
			var c = Children;
			if (c != null)
				return c.TryGet(index, out fail);
			fail = true;
			return default(Token);
		}
		public IEnumerator<Token> GetEnumerator()
		{
			var c = Children;
			return c == null ? EmptyEnumerator<Token>.Value : (IEnumerator<Token>)c.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Count
		{
			get { var c = Children; return c == null ? 0 : c.Count; }
		}
		IRange<Token> IListSource<Token>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<Token> Slice(int start, int count) { return new Slice_<Token>(this, start, count); }

		#endregion

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		int ISimpleToken<int>.Type => TypeInt;
		IToken<int> IToken<int>.WithType(int type) => WithType(checked((ushort)type));
		public Token WithType(int type) => new Token(type, _startIndex, _length, _value, _style, _substringOffset, _substringOffset);

		IToken<int> IToken<int>.WithValue(object value) => WithValue(value);
		public Token WithValue(object value)
		{
			Token newT = this;
			newT.Value = value;
			return newT;
		}

		public Token WithRange(int startIndex, int endIndex) => new Token(_typeInt, startIndex, endIndex - startIndex, _value, _style, _substringOffset, _substringOffset);
		public Token WithStartIndex(int startIndex) => new Token(_typeInt, startIndex, _length, _value, _style, _substringOffset, _substringOffset);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IListSource<IToken<int>> IToken<int>.Children
		{
			get { return new UpCastListSource<Token, IToken<int>>(Children); }
		}
		IToken<int> ICloneable<IToken<int>>.Clone()
		{
			return this;
		}

		public static bool IsOpener(TokenKind tt)
		{
			return IsOpenerOrCloser(tt) && ((int)tt & 0x0100) == 0;
		}
		public static bool IsCloser(TokenKind tt)
		{
			return IsOpenerOrCloser(tt) && ((int)tt & 0x0100) != 0;
		}
		public static bool IsOpenerOrCloser(TokenKind tt)
		{
			return tt >= TokenKind.LParen && tt < (TokenKind)((int)TokenKind.ROther + 0x100);
		}

		[Obsolete("Please call TokenTree.TokenToLNode(Token, ISourceFile) instead.")]
		public LNode ToLNode(ISourceFile file) => TokenTree.TokenToLNode(this, file);

		[Obsolete("This doesn't appear to be used will be removed if no one complains")]
		public static Symbol GetParenPairSymbol(TokenKind k, TokenKind k2)
		{
			Symbol both;
			if (k == TokenKind.LParen && k2 == TokenKind.RParen)
				both = GSymbol.Get("'()");
			else if (k == TokenKind.LBrack && k2 == TokenKind.RBrack)
				both = CodeSymbols._Bracks;
			else if (k == TokenKind.LBrace && k2 == TokenKind.RBrace)
				both = CodeSymbols.Braces;
			else if (k == TokenKind.Indent && k2 == TokenKind.Dedent)
				both = GSymbol.Get("'IndentDedent");
			else if (k == TokenKind.LOther && k2 == TokenKind.ROther)
				both = GSymbol.Get("'LOtherROther");
			else
				return null;
			return both;
		}
	}

	/// <summary><see cref="WhitespaceTag.Value"/> can be used as the
	/// <see cref="Token.Value"/> of whitespace tokens, to make whitespace
	/// easy to filter out.</summary>
	public class WhitespaceTag
	{
		protected WhitespaceTag() { }
		public static readonly WhitespaceTag Value = new WhitespaceTag();
		public override string ToString() { return "(Whitespace)"; }
	}

	/// <summary>Basic information about a token as expected by <see cref="BaseParser{Token}"/>:
	/// a token <see cref="Type"/>, which is the type of a "word" in the program 
	/// (string, identifier, plus sign, etc.), a value (e.g. the name of an 
	/// identifier), and an index where the token starts in the source file.</summary>
	public interface ISimpleToken<TokenType> : IHasValue<object>
	{
		/// <summary>The category of the token (integer, keyword, etc.) used as
		/// the primary value for identifying the token in a parser.</summary>
		TokenType Type { get; }
		/// <summary>Character index where the token starts in the source file.</summary>
		int StartIndex { get; }
		#if false // inherited
		/// <summary>Value of the token. The meaning of this property is defined
		/// by the particular implementation of this interface, but typically this 
		/// property contains a parsed form of the token (e.g. if the token came 
		/// from the text "3.14", its value might be <c>(double)3.14</c>.</summary>
		object Value { get; }
		#endif
	}
	
	/// <summary>Alias for ISimpleToken{int}.</summary>
	public interface ISimpleToken : ISimpleToken<Int32> { }

	/// <summary>The methods of <see cref="Token"/> in the form of an interface.</summary>
	/// <typeparam name="TT">Token Type: the data type of the Type property of 
	/// <see cref="ISimpleToken{LaType}"/> (one often uses int).</typeparam>
	public interface IToken<TT> : ISimpleToken<TT>, ICloneable<IToken<TT>>
	{
		int Length { get; }

		IToken<TT> WithType(int type);

		TokenKind Kind { get; }

		IToken<TT> WithValue(object value);

		IListSource<IToken<TT>> Children { get; }
	}
}
