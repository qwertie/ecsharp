using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Utilities;
using TT = Loyc.Syntax.Les.TokenType;

namespace Loyc.Syntax.Les
{
	public class TokenTree : DList<Token>
	{
		public TokenTree(ISourceFile file, int capacity) : base(capacity) { File = file; }
		public TokenTree(ISourceFile file, IReadOnlyCollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, ICollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, IEnumerable<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file) { File = file; }
		public readonly ISourceFile File;
	}

	/// <summary><see cref="WhitespaceTag.Value"/> is used in <see cref="Token.Value"/>
	/// to represent whitespace and comments, which allows them to be quickly 
	/// filtered out.</summary>
	public class WhitespaceTag
	{
		private WhitespaceTag() { }
		public static readonly WhitespaceTag Value = new WhitespaceTag();
		public override string ToString() { return "<Whitespace>"; }
	}

	public struct Token : IListSource<Token>
	{
		public TokenType Type;
		public int StartIndex;
		int _length;
		const int LengthMask = 0x0FFFFFFF;
		const int StyleMask = unchecked((int)0xF0000000);
		const int StyleShift = 24;

		public int Length { get { return _length & LengthMask; } }
		public NodeStyle Style { get { return (NodeStyle)((_length & StyleMask) >> StyleShift); } }
		
		/// <summary>The parsed value of the token.</summary>
		/// <remarks>The value is
		/// <ul>
		/// <li>For strings: the parsed value of the string (no quotes, escape 
		/// sequences removed), i.e. a boxed char or string, or 
		/// <see cref="ApparentInterpolatedString"/> if the string contains 
		/// so-called interpolation expressions. A backquoted string (which is
		/// a kind of operator) is converted to a Symbol.</li>
		/// <li>For numbers: the parsed value of the number (e.g. 4 => int, 4L => long, 4.0f => float)</li>
		/// <li>For identifiers: the parsed name of the identifier, as a Symbol (e.g. x => \x, @for => \for, @`1+1` => \`1+1`)</li>
		/// <li>For any keyword including AttrKeyword and TypeKeyword tokens: a 
		/// Symbol containing the name of the keyword (no "#" prefix)</li>
		/// <li>For all other tokens: null</li>
		/// <li>For punctuation and operators: the text of the punctuation with "#" in front, as a symbol</li>
		/// <li>For spaces, comments, and everything else: the <see cref="Whitespace"/> object</li>
		/// <li>For openers and closers (open paren, open brace, etc.) after tree-ification: a TokenTree object.</li>
		/// </ul></remarks>
		public object Value;
		public TokenTree Children { get { return Value as TokenTree; } }
		public int EndIndex { get { return StartIndex + Length; } }
		public bool IsWhitespace { get { return Value == WhitespaceTag.Value; } }
		public bool Is(TokenType tt, object value) { return tt == Type && object.Equals(value, Value); }

		public Token(TokenType type, int startIndex, int length, NodeStyle style = 0, object value = null)
		{
			Type = type;
			StartIndex = startIndex;
			_length = length | (((int)style << StyleShift) & StyleMask);
			Value = value;
		}

		public string SourceText(ISourceFile sf)
		{
			if (StartIndex < sf.Count)
				return sf.Substring(StartIndex, Length);
			return null;
		}

		/// <summary>Reconstructs a string that represents the token, if possible.
		/// May not work for whitespace and comments, because the value of these
		/// token types is stored in the original source file and for performance 
		/// reasons not copied to the token, by default.</summary>
		/// <remarks>The returned string, in general, will not match the original
		/// token, but will be round-trippable; that is, if the returned string
		/// is parsed as by <see cref="LesLexer"/>, the lexer will produce an 
		/// equivalent token. For example, the number 12_000 wil be printed as 
		/// 12000--a different string that represents the same value.
		/// </remarks>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			switch (Type) {
				case TT.Spaces: return " ";
				case TT.Newline: return "\n";
				case TT.SLComment: return "//\n";
				case TT.MLComment: return "/**/";
				case TT.Id        : 
				case TT.Number    :
				case TT.String    :
				case TT.SQString  :
				case TT.BQString  :
				case TT.Symbol    :
				case TT.OtherLit  :
				case TT.Dot       :
				case TT.Assignment:
				case TT.NormalOp  :
				case TT.PreSufOp  :
				case TT.Colon     :
				case TT.At        :
				case TT.Comma     :
				case TT.Semicolon :
				case TT.Parens    :
				case TT.LParen    :
				case TT.RParen    :
				case TT.Bracks    :
				case TT.LBrack    :
				case TT.RBrack    :
				case TT.Braces    :
				case TT.LBrace    :
				case TT.RBrace    :
				case TT.Of        :
				case TT.OpenOf    :
				case TT.Indent    :
				case TT.Dedent    :
				case TT.Shebang   :


			default:
				Debug.Assert(Value is Symbol && Value.ToString().StartsWith("#"));
				return Value.ToString().Substring(1);
			}
		}

		#region IListSource<Token> Members

		public Token this[int index]
		{
			get { return Children[index]; }
		}
		public Token TryGet(int index, ref bool fail)
		{
			var c = Children;
			if (c != null)
				return c.TryGet(index, ref fail);
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
		public int Count
		{
			get { var c = Children; return c == null ? 0 : c.Count; }
		}
		IRange<Token> IListSource<Token>.Slice(int start, int count) { return Slice(start, count); }
		public Slice_<Token> Slice(int start, int count) { return new Slice_<Token>(this, start, count); }

		#endregion
	}
}
