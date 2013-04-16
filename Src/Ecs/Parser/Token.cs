using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.CompilerCore;

namespace Ecs.Parser
{
	public class TokenTree : DList<Token>
	{
		public TokenTree(StringCharSourceFile file, int capacity) : base(capacity) { File = file; }
		public TokenTree(StringCharSourceFile file, IIterable<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file, ISource<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file, ICollection<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file, IEnumerable<Token> items) : base(items) { File = file; }
		public TokenTree(StringCharSourceFile file) { File = file; }
		public readonly StringCharSourceFile File;
	}

	public struct Token : IListSource<Token>
	{
		public Symbol Type;
		public int StartIndex, Length;
		/// <summary>The parsed value of the token.</summary>
		/// <remarks>The value is
		/// <ul>
		/// <li>For strings: the parsed value of the string (no quotes, escape 
		/// sequences removed), i.e. a boxed char or string, or 
		/// <see cref="ApparentInterpolatedString"/> if the string contains 
		/// so-called interpolation expressions. A backquoted string (which is
		/// a kind of operator) is converted to a Symbol.</li>
		/// <li>For numbers: the parsed value of the number (e.g. 4 => int, 4L => long, 4.0f => float)</li>
		/// <li>For identifiers: the parsed name of the identifier, as a Symbol (e.g. x => $x, @for => $for, @`1+1` => $`1+1`)</li>
		/// <li>For any keyword including AttrKeyword and TypeKeyword tokens: a 
		/// Symbol containing the name of the keyword (no "#" prefix)</li>
		/// <li>For all other tokens: null</li>
		/// <li>For punctuation and operators: the text of the punctuation with "#" in front, as a symbol</li>
		/// <li>For spaces, comments, and everything else: null</li>
		/// <li>For openers and closers (open paren, open brace, etc.) after tree-ification: a TokenTree object.</li>
		/// </ul></remarks>
		public object Value;
		public TokenTree Children { get { return Value as TokenTree; } }

		public Token(Symbol type, int startIndex, int length, object value = null)
		{
			Type = type;
			StartIndex = startIndex;
			Length = length;
			Value = value;
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
