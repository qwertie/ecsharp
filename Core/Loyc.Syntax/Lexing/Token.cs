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
using Loyc.Collections.Impl;

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
	/// created directly by the lexer, but by a helper class (<see cref="TokensToTree"/>).
	/// <para/>
	/// Caution: this class is mutable, even though TokenTrees are sometimes stored
	/// in <see cref="LNode"/>s which are supposed to be immutable. Please do not
	/// modify token trees that are stored inside LNodes.
	/// </remarks>
	public class TokenTree : DList<Token>, IListSource<Token>, IListSource<IToken<int>>, IEquatable<TokenTree>, ICloneable<TokenTree>
	{
		public TokenTree(ISourceFile file, int capacity) : base(capacity) { File = file; }
		public TokenTree(ISourceFile file, ICollectionAndReadOnly<Token> items) : this(file, (IReadOnlyCollection<Token>)items) { }
		public TokenTree(ISourceFile file, IReadOnlyCollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, ICollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, IEnumerable<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, Token[] items) : base(items) { File = file; }
		public TokenTree(ISourceFile file) { File = file; }
		
		public readonly ISourceFile File;

		IToken<int> IListSource<IToken<int>>.TryGet(int index, out bool fail)
		{
			return TryGet(index, out fail);
		}
		IRange<IToken<int>> IListSource<IToken<int>>.Slice(int start, int count)
		{
			return new UpCastListSource<Token, IToken<int>>(this).Slice(start, count);
		}
		IToken<int> IReadOnlyList<IToken<int>>.this[int index]
		{
			get { return this[index]; }
		}
		IEnumerator<IToken<int>> IEnumerable<IToken<int>>.GetEnumerator()
		{
			return Enumerable.Cast<IToken<int>>(this).GetEnumerator();
		}
		/// <summary>Gets a deep (recursive) clone of the token tree.</summary>
		public new TokenTree Clone() { return Clone(true); }
		public     TokenTree Clone(bool deep)
		{
			return new TokenTree(File, ((DList<Token>)this).Select(t => { 
				var c = t.Children; 
				return c != null ? t.WithValue(c.Clone(true)) : t;
			}));
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
		/// <remarks>Because <see cref="LNode"/>s are compared by value and not by 
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

		/// <summary>Converts this list of <see cref="Token"/> to a list of <see cref="LNode"/>.</summary>
		/// <remarks>See <see cref="Token.ToLNode(ISourceFile)"/> for more information.</remarks>
		public VList<LNode> ToLNodes()
		{
			VList<LNode> list = VList<LNode>.Empty;
			foreach (var item in (DList<Token>)this)
				list.Add(item.ToLNode(File));
			return list;
		}

		/// <summary>Converts a token tree back to a plain list.</summary>
		public DList<Token> Flatten()
		{
			var output = new DList<Token>();
			bool hasChildren = false;
			Flatten(this, output, ref hasChildren);
			return hasChildren ? output : this;
		}
		internal static void Flatten(DList<Token> input, DList<Token> output, ref bool hasChildren)
		{
			foreach (var token in input)
			{
				output.Add(token);
				var c = token.Children;
				if (c != null && c.Count != 0)
				{
					hasChildren = true;
					Flatten(c, output, ref hasChildren);
				}
			}
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
	public struct Token : IListSource<Token>, IToken<int>, IEquatable<Token>
	{
		public Token(int type, int startIndex, int length, NodeStyle style = 0, object value = null)
		{
			TypeInt = type;
			StartIndex = startIndex;
			_length = length | (((int)style << StyleShift) & StyleMask);
			Value = value;
		}
		public Token(int type, int startIndex, int length, object value)
		{
			TypeInt = type;
			StartIndex = startIndex;
			_length = length;
			Value = value;
		}

		/// <summary>Token type.</summary>
		public readonly int TypeInt;

		/// <summary>Token kind.</summary>
		public TokenKind Kind { get { return ((TokenKind)TypeInt & TokenKind.KindMask); } }

		/// <summary>Location in the orginal source file where the token starts, or
		/// -1 for a synthetic token.</summary>
		public readonly int StartIndex;
		int ISimpleToken<int>.StartIndex { get { return StartIndex; } }

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
		public object Value;
		
		/// <summary>Returns Value as TokenTree (null if not a TokenTree).</summary>
		public TokenTree Children { get { return Value as TokenTree; } }

		/// <summary>Returns StartIndex + Length.</summary>
		public int EndIndex { get { return StartIndex + Length; } }

		/// <summary>Returns true if Value == <see cref="WhitespaceTag.Value"/>.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)] public bool IsWhitespace { get { return Value == WhitespaceTag.Value; } }
		
		/// <summary>Returns true if the specified type and value match this token.</summary>
		public bool Is(int type, object value) { return type == TypeInt && object.Equals(value, Value); }

		static readonly ThreadLocalVariable<Func<Token, string>> ToStringStrategyTLV = new ThreadLocalVariable<Func<Token,string>>(Loyc.Syntax.Les.TokenExt.ToString);
		public static SavedValue<Func<Token, string>> SetToStringStrategy(Func<Token, string> newValue)
		{
			return new SavedValue<Func<Token, string>>(ToStringStrategyTLV, newValue);
		}

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
		public UString SourceText(ILexer<Token> l) { return SourceText(l.SourceFile.Text); }

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
		int ISimpleToken<int>.Type { get { return TypeInt; } }
		IToken<int> IToken<int>.WithType(int type) { return WithType(type); }
		public Token WithType(int type) { return new Token(type, StartIndex, _length, Value); }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object IHasValue<object>.Value { get { return Value; } }
		IToken<int> IToken<int>.WithValue(object value) { return WithValue(value); }
		public Token WithValue(object value) { return new Token(TypeInt, StartIndex, _length, value); }

		public Token WithRange(int startIndex, int endIndex) { return new Token(TypeInt, startIndex, endIndex - startIndex, Value); }
		public Token WithStartIndex(int startIndex)          { return new Token(TypeInt, startIndex, _length, Value); }

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
			return tt >= TokenKind.LParen && tt < (TokenKind)((int)TokenKind.ROther + (1 << TokenKindShift));
		}

		#region ToLNode()

		/// <summary>Converts a <see cref="Token"/> to a <see cref="LNode"/>.</summary>
		/// <param name="file">This becomes the <see cref="LNode.Source"/> property.</param>
		/// <remarks>If you really need to store tokens as LNodes, use this. Only
		/// the <see cref="Kind"/>, not the TypeInt, is preserved. Identifiers 
		/// (where Kind==TokenKind.Id and Value is Symbol) are translated as Id 
		/// nodes; everything else is translated as a call, using the TokenKind as
		/// the <see cref="LNode.Name"/> and the value, if any, as parameters. For
		/// example, if it has been treeified with <see cref="TokensToTree"/>, the
		/// token list for <c>"Nodes".Substring(1, 3)</c> as parsed by LES might 
		/// translate to the LNode sequence <c>String("Nodes"), Dot(@@.), 
		/// Substring, LParam(Number(1), Separator(@@,), Number(3)), RParen()</c>.
		/// The <see cref="LNode.Range"/> will match the range of the token.
		/// </remarks>
		public LNode ToLNode(ISourceFile file)
		{
			var kind = Kind;
			Symbol kSym = GSymbol.Empty;
			Symbol id;
			if (kind != TokenKind.Id) {
				int k = (int)kind >> TokenKindShift;
				kSym = _kindAttrTable.TryGet(k, null);
			}

			var r = new SourceRange(file, StartIndex, Length);
			var c = Children;
			if (c != null) {
				if (c.Count != 0)
					r = new SourceRange(file, StartIndex, System.Math.Max(EndIndex, c.Last.EndIndex) - StartIndex);
				return LNode.Call(kSym, c.ToLNodes(), r, Style);
			} else if (IsOpenerOrCloser(kind) || Value == WhitespaceTag.Value) {
				return LNode.Call(kSym, VList<LNode>.Empty, r, Style);
			} else if (kind == TokenKind.Id && (id = this.Value as Symbol) != null) {
				return LNode.Id(id, r, Style);
			} else {
				return LNode.Trivia(kSym, this.Value, r, Style);
			}
		}

		private Symbol GetPunctuationSymbol(TokenKind Kind)
		{
			int i = (Kind - TokenKind.LParen) >> TokenKindShift;
			if ((uint)i < (uint)TokenKindPunctuationSymbols.Length)
				return TokenKindPunctuationSymbols[i];
			return null;
		}

		internal Symbol GetParenPairSymbol(Token followingToken)
		{
			Symbol opener = Value as Symbol;
			Symbol closer = followingToken.Value as Symbol;
			if (opener != null && closer != null)
				return GSymbol.Get(opener.Name + closer.Name);
			else
				return GetParenPairSymbol(Kind, followingToken.Kind);
		}
		public static Symbol GetParenPairSymbol(TokenKind k, TokenKind k2)
		{
			Symbol both;
			if (k == TokenKind.LParen && k2 == TokenKind.RParen)
				both = Parens;
			else if (k == TokenKind.LBrack && k2 == TokenKind.RBrack)
				both = CodeSymbols._Bracks;
			else if (k == TokenKind.LBrace && k2 == TokenKind.RBrace)
				both = CodeSymbols.Braces;
			else if (k == TokenKind.Indent && k2 == TokenKind.Dedent)
				both = IndentDedent;
			else if (k == TokenKind.LOther && k2 == TokenKind.ROther)
				both = LOtherROther;
			else
				return null;
			return both;
		}

		static readonly Symbol Parens = GSymbol.Get("()");
		static readonly Symbol IndentDedent = GSymbol.Get("IndentDedent");
		static readonly Symbol LOtherROther = GSymbol.Get("LOtherROther");
		const int TokenKindShift = 8;
		const int NumPuncSymbols = ((TokenKind.RBrace - TokenKind.LParen) >> TokenKindShift) + 1;
		static readonly Symbol[] TokenKindPunctuationSymbols = new Symbol[NumPuncSymbols] {
			(Symbol)"(", (Symbol)")", 
			(Symbol)"[", (Symbol)"]",
			(Symbol)"{", (Symbol)"}"
		};
		// Each list contains a single item, the attribute to be associated with
		// the node returned from ToLNode. Why a list for only one item? This is
		// an optimization to ensure we only allocate the list once. Example:
		// _kindAttrTable[(int)TokenKind.Operator >> TokenKindShift][0].Name.Name == "Operator"
		static readonly InternalList<Symbol> _kindAttrTable = KindAttrTable();
		private static InternalList<Symbol> KindAttrTable()
		{
			Debug.Assert(((int)TokenKind.KindMask & ((2 << TokenKindShift) - 1)) == (1 << TokenKindShift));
			int incr = (1 << TokenKindShift), stopAt = (int)TokenKind.KindMask;
			var table = new InternalList<Symbol>(stopAt / incr);
			for (int kind = 0; kind < stopAt; kind += incr) {
				string kindStr = ((TokenKind)kind).ToString();
				table.Add((Symbol)kindStr);
			}
			return table;
		}
		
		#endregion
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
