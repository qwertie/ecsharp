using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.CompilerCore;
using Loyc.Syntax;
using System.Diagnostics;
using TT = Ecs.Parser.TokenType;
using Loyc.Utilities;

namespace Ecs.Parser
{
	public class TokenTree : DList<Token>
	{
		public TokenTree(ISourceFile file, int capacity) : base(capacity) { File = file; }
		public TokenTree(ISourceFile file, IIterable<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, ISource<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, ICollection<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file, IEnumerable<Token> items) : base(items) { File = file; }
		public TokenTree(ISourceFile file) { File = file; }
		public readonly ISourceFile File;
	}

	/// <summary><see cref="WhitespaceTag.Value"/> is used in <see cref="Token.Value"/> to represent whitespace.</summary>
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

		public override string ToString()
		{
			//TODO
			switch (Type) {
				case TT.SQString:
					return G.EscapeCStyle(Value.ToString(), EscapeC.SingleQuotes | EscapeC.Control);
				case TT.Number:
					return Value.ToString(); // ish
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
		public Iterator<Token> GetIterator()
		{
			var c = Children;
			return c == null ? EmptyIterator<Token>.Value : c.GetIterator();
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

		#endregion
	}

}
