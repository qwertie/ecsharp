using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Threading;
using Loyc.Math;

namespace Loyc.Syntax.Lexing
{
	/// <summary>A list of Token structures along with the <see cref="ISourceFile"/> 
	/// object that represents the source file that the tokens came from.</summary>
	/// <remarks>This class is called <c>TokenTree</c> because certain kinds of 
	/// tokens used by some parsers are formed into trees by using <see cref="TokenTree"/> 
	/// as the type of the <see cref="Token.Value"/> of certain tokens. Specifically,
	/// the LES and EC# parsers expect open-bracket and open-brace tokens ('(', 
	/// '[' and '{') to have a child <see cref="TokenTree"/> that contains all the 
	/// tokens within a pair of brackets or braces. Typically this tree is not 
	/// created directly by the lexer, but by a helpe clas (<see cref="TokensToTree"/>).</remarks>
	public class TokenTree : DList<Token>, IListSource<IToken>, IEquatable<TokenTree>
	{
		public TokenTree(ISourceFile file, int capacity) : base(capacity) { File = file; }
		public TokenTree(ISourceFile file, ICollectionAndReadOnly<Token> items) : this(file, (IReadOnlyCollection<Token>)items) { }
		public TokenTree(ISourceFile file, IReadOnlyCollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, ICollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, IEnumerable<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file) { File = file; }
		
		public readonly ISourceFile File;

		IToken IListSource<IToken>.TryGet(int index, out bool fail)
		{
			return TryGet(index, out fail);
		}
		IRange<IToken> IListSource<IToken>.Slice(int start, int count)
		{
			return new UpCastListSource<Token, IToken>(this).Slice(start, count);
		}
		IToken IReadOnlyList<IToken>.this[int index]
		{
			get { return this[index]; }
		}
		IEnumerator<IToken> IEnumerable<IToken>.GetEnumerator()
		{
			return Enumerable.Cast<IToken>(this).GetEnumerator();
		}

		#region ToString, Equals, GetHashCode

		public override string ToString()
		{
			return ToString(Token.ToStringStrategy);
		}
		public string ToString(Func<Token, string> toStringStrategy = null)
		{
			StringBuilder sb = new StringBuilder();
			AppendTo(sb, toStringStrategy ?? Token.ToStringStrategy);
			return sb.ToString();
		}
		void AppendTo(StringBuilder sb, Func<Token, string> toStringStrategy, int prevEndIndex = 0)
		{
			Token prev = new Token(0, prevEndIndex, 0);
			for (int i = 0; i < Count; i++) {
				Token t = this[i];
				if (t.StartIndex != prev.EndIndex || t.StartIndex <= 0)
					sb.Append(' ');
				sb.Append(toStringStrategy(t));
				if (t.Value is TokenTree) {
					var subtree = ((TokenTree)t.Value);
					subtree.AppendTo(sb, toStringStrategy, t.EndIndex);
					if (subtree.Count != 0)
						t = t.WithRange(t.StartIndex, subtree.Last.EndIndex); // to avoid printing unnecessary space before closing ')' or '}'
				}
				prev = t;
			}
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TokenTree);
		}
		/// <summary>Compares the elements of the token tree for equality.</summary>
		/// <remarks>Because <see cref="LNodes"/> are compared by value and not by 
		/// reference, and LNodes can contain TokenTrees, TokenTrees should also be
		/// compared by value.</remarks>
		public bool Equals(TokenTree other)
		{
			if (other == null || Count != other.Count) return false;
			return this.SequenceEqual(other);
		}
		public override int GetHashCode()
		{
			return this.SequenceHashCode<Token>();
		}

		#endregion
	}

	/// <summary><see cref="WhitespaceTag.Value"/> is used in <see cref="Token.Value"/>
	/// to represent whitespace and comments, which allows them to be quickly 
	/// filtered out.</summary>
	public class WhitespaceTag
	{
		protected WhitespaceTag() { }
		public static readonly WhitespaceTag Value = new WhitespaceTag();
		public override string ToString() { return "(Whitespace)"; }
	}

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
	/// an integer, so <see cref="Token"/> uses Int32 as the token type.</li>
	/// <li><see cref="Value"/>: this can be any object. For literals, this should 
	/// be the actual value of the literal, for whitespace it should be 
	/// <see cref="WhitespaceTag.Value"/>, etc. See <see cref="Value"/> for 
	/// the complete list.</li>
	/// <li><see cref="StartIndex"/>: location in the original source file where 
	/// the token starts.</li>
	/// <li><see cref="Length"/>: length of the token in the source file (24 bits).</li>
	/// <li><see cref="Style"/>: 8 bits for other information.</li>
	/// </ol>
	/// Originally I planned to use <see cref="Symbol"/> as the common token 
	/// type, because it is extensible and could nicely represent tokens in all
	/// languages; unfortunately, Symbol may reduce parsing performance because 
	/// it cannot be used with the switch opcode (i.e. the switch statement in 
	/// C#), so I decided to switch to integers instead and to introduce the 
	/// concept of <see cref="TokenKind"/>, which is derived from 
	/// <see cref="Type"/> using <see cref="TokenKind.KindMask"/>.
	/// Each language should have, in the namespace of that language, an
	/// extension method <c>public static TokenType Type(this Token t)</c> that 
	/// converts the TypeInt to the enum type for that language.
	/// <para/>
	/// To save space (and because .NET doesn't handle large structures well),
	/// tokens do not know what source file they came from and cannot convert 
	/// their location to a line number. For this reason, one should keep a
	/// reference to the <see cref="ISourceFile"/> and call <see cref="IIndexToLine.IndexToLine(int)"/> 
	/// to get the source location.
	/// <para/>
	/// A generic token also cannot convert itself to a properly-formatted 
	/// string. The <see cref="ToString"/> method does allow 
	/// </remarks>
	public struct Token : IListSource<Token>, IToken, IEquatable<Token>
	{
		public Token(int type, int startIndex, int length, NodeStyle style = 0, object value = null)
		{
			TypeInt = type;
			StartIndex = startIndex;
			_length = length | (((int)style << StyleShift) & StyleMask);
			Value = value;
		}
		private Token(int type, int startIndex, int lengthAndStyle, object value)
		{
			TypeInt = type;
			StartIndex = startIndex;
			_length = lengthAndStyle;
			Value = value;
		}

		/// <summary>Token type.</summary>
		public readonly int TypeInt;

		/// <summary>Token kind.</summary>
		public TokenKind Kind { get { return ((TokenKind)TypeInt & TokenKind.KindMask); } }

		/// <summary>Location in the orginal source file where the token starts, or
		/// -1 for a synthetic token.</summary>
		public readonly int StartIndex;
		int ISimpleToken.StartIndex { get { return StartIndex; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)] int _length;
		const int LengthMask = 0x00FFFFFF;
		const int StyleMask = unchecked((int)0xFF000000);
		const int StyleShift = 24;

		/// <summary>Length of the token in the source file, or 0 for a synthetic 
		/// or implied token.</summary>
		public int Length { get { return _length & LengthMask; } }
		/// <summary>8 bits of nonsemantic information about the token. The style 
		/// is used to distinguish hex literals from decimal literals, or triple-
		/// quoted strings from double-quoted strings.</summary>
		public NodeStyle Style { get { return (NodeStyle)((_length & StyleMask) >> StyleShift); } }
		
		/// <summary>The parsed value of the token.</summary>
		/// <remarks>The value is
		/// <ul>
		/// <li>For strings: the parsed value of the string (no quotes, escape 
		/// sequences removed), i.e. a boxed char or string. A backquoted string 
		/// is converted to a Symbol because it is a kind of operator.</li>
		/// <li>For numbers: the parsed value of the number (e.g. 4 => int, 4L => long, 4.0f => float)</li>
		/// <li>For identifiers: the parsed name of the identifier, as a Symbol 
		/// (e.g. x => x, @for => for, @`1+1` => <c>1+1</c>)</li>
		/// <li>For any keyword including AttrKeyword and TypeKeyword tokens: a 
		/// Symbol containing the name of the keyword, with "#" prefix</li>
		/// <li>For punctuation and operators: the text of the punctuation as a 
		/// symbol (with '#' in front, if the language conventionally uses this 
		/// prefix)</li>
		/// <li>For openers (open paren, open brace, etc.) after the tokens have
		/// been processed by <see cref="TokensToTree"/>: a TokenTree object.</li>
		/// <li>For spaces and comments: <see cref="WhitespaceTag.Value"/></li>
		/// <li>When no value is needed (because the Type() is enough): null</li>
		/// </ul>
		/// For performance reasons, the text of whitespace is not extracted from
		/// the source file; <see cref="Value"/> is WhitespaceTag.Value for 
		/// whitespace. Value must be assigned for other types such as 
		/// identifiers and literals.
		/// <para/>
		/// Since the same identifiers and literals are often used more than once 
		/// in a given source file, an optimized lexer could use a data structure 
		/// such as a trie or hashtable to cache boxed literals and identifier 
		/// symbols, and re-use the same values when the same identifiers and 
		/// literals are encountered multiple times. Done carefully, this avoids 
		/// the overhead of repeatedly extracting string objects from the source 
		/// file. If strings must be extracted for some reason (e.g. <c>
		/// double.TryParse</c> requires an extracted string), at least memory can 
		/// be saved.
		/// </remarks>
		public object Value;
		
		/// <summary>Returns Value as TokenTree (null if not a TokenTree).</summary>
		public TokenTree Children { get { return Value as TokenTree; } }

		/// <summary>Returns StartIndex + Length.</summary>
		public int EndIndex { get { return StartIndex + Length; } }

		/// <summary>Returns true if Value == <see cref="WhitespaceTag.Value"/>.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsWhitespace { get { return Value == WhitespaceTag.Value; } }
		
		/// <summary>Returns true if the specified type and value match this token.</summary>
		public bool Is(int type, object value) { return type == TypeInt && object.Equals(value, Value); }

		public static readonly ThreadLocalVariable<Func<Token, string>> ToStringStrategyTLV = new ThreadLocalVariable<Func<Token,string>>(Loyc.Syntax.Les.TokenExt.ToString);
		/// <summary>Gets or sets the strategy used by <see cref="ToString"/>.</summary>
		public static Func<Token, string> ToStringStrategy
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
		public SourceRange Range(ILexer l) { return Range(l.SourceFile); }

		/// <summary>Gets the original source text for a token if available, under the 
		/// assumption that the specified source file correctly specifies where the
		/// token came from. If the token is synthetic, returns <see cref="UString.Null"/>.</summary>
		public UString SourceText(ICharSource file)
		{
			if ((uint)StartIndex <= (uint)file.Count)
				return file.Slice(StartIndex, Length);
			return UString.Null;
		}
		public UString SourceText(ILexer l) { return SourceText(l.SourceFile.Text); }

		#region ToString, Equals, GetHashCode

		/// <summary>Reconstructs a string that represents the token, if possible.
		/// Does not work for whitespace and comments, because the value of these
		/// token types is stored in the original source file and for performance 
		/// reasons is not copied to the token.</summary>
		/// <remarks>
		/// This does <i>not</i> return the original source text; it uses a language-
		/// specific stringizer (<see cref="ToStringStrategy"/>).
		/// <para/>
		/// The returned string, in general, will not match the original
		/// token, since the <see cref="ToStringStrategy"/> does not have access to
		/// the original source file.
		/// </remarks>
		public override string ToString()
		{
			return ToStringStrategy(this);
		}

		public override bool Equals(object obj)
		{
			return obj is Token && Equals((Token)obj);
		}
		/// <summary>Equality depends on TypeInt and Value, but not StartIndex and 
		/// Length (which matches the equality condition of <see cref="LNode"/>).</summary>
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
			return c == null ? EmptyEnumerator<Token>.Value : c.GetEnumerator();
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
		int ISimpleToken.TypeInt { get { return TypeInt; } }
		IToken IToken.WithType(int type) { return WithType(type); }
		public Token WithType(int type) { return new Token(type, StartIndex, _length, Value); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object ISimpleToken.Value { get { return Value; } }
		IToken IToken.WithValue(object value) { return WithValue(value); }
		public Token WithValue(object value) { return new Token(TypeInt, StartIndex, _length, value); }

		public Token WithRange(int startIndex, int endIndex) { return new Token(TypeInt, startIndex, endIndex - startIndex, Value); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IListSource<IToken> IToken.Children
		{
			get { return new UpCastListSource<Token, IToken>(Children); }
		}
		IToken ICloneable<IToken>.Clone()
		{
			return this;
		}

		public object ToSourceRange(ISourceFile sourceFile)
		{
			return new SourceRange(sourceFile, StartIndex, Length);
		}
	}

	/// <summary>Basic information about a token as expected by <see cref="BaseParser"/>:
	/// type (as an integer), index where the token starts in the source file, 
	/// and a value.</summary>
	public interface ISimpleToken
	{
		/// <summary>Token type (cast to an integer if it is an enum).</summary>
		int TypeInt { get; }
		/// <summary>Character index where the token starts in the source file.</summary>
		int StartIndex { get; }
		/// <summary>Value of the token. The meaning of this property is defined
		/// by the particular implementation of this interface, but typically this 
		/// property contains a parsed form of the token (e.g. if the token came 
		/// from the text "3.14", its value might be <c>(double)3.14</c>.</summary>
		object Value { get; }
	}

	/// <summary>The methods of <see cref="Token"/> in the form of an interface.</summary>
	public interface IToken : ISimpleToken, ICloneable<IToken>
	{
		int Length { get; }

		IToken WithType(int type);

		TokenKind Kind { get; }

		IToken WithValue(object value);

		IListSource<IToken> Children { get; }
	}
}
