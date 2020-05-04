using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;

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

		IToken<int> IListSource<IToken<int>>.TryGet(int index, out bool fail) => TryGet(index, out fail);
		IRange<IToken<int>> IListSource<IToken<int>>.Slice(int start, int count)
		{
			return new UpCastListSource<Token, IToken<int>>(this).Slice(start, count);
		}
		IToken<int> IReadOnlyList<IToken<int>>.this[int index] => this[index];
		IToken<int> IIndexed<int, IToken<int>>.this[int index] => this[index];
		IEnumerator<IToken<int>> IEnumerable<IToken<int>>.GetEnumerator()
		{
			return Enumerable.Cast<IToken<int>>(this).GetEnumerator();
		}
		/// <summary>Gets a deep (recursive) clone of the token tree.</summary>
		public new TokenTree Clone() => Clone(true);
		public TokenTree Clone(bool deep)
		{
			return new TokenTree(File, ((DList<Token>)this).Select(t => {
				var c = t.Children;
				return c != null ? t.WithValue(c.Clone(true)) : t;
			}));
		}

		#region ToString, Equals, GetHashCode

		public override string ToString() => ToString(Token.ToStringStrategy);
		public string ToString(Func<Token, string> toStringStrategy = null)
		{
			StringBuilder sb = new StringBuilder();
			AppendTo(sb, toStringStrategy ?? Token.ToStringStrategy);
			return sb.ToString();
		}
		void AppendTo(StringBuilder sb, Func<Token, string> toStringStrategy, int prevEndIndex = 0)
		{
			Token prev = new Token(0, prevEndIndex, 0);
			for (int i = 0; i < Count; i++)
			{
				Token t = this[i];
				if (t.StartIndex != prev.EndIndex || t.StartIndex <= 0)
					sb.Append(' ');
				sb.Append(toStringStrategy(t));
				if (t.Value is TokenTree)
				{
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
			if (other == null) return false;
			return LinqToLists.SequenceEqual<Token>(this, other);
		}
		public override int GetHashCode()
		{
			return this.SequenceHashCode<Token>();
		}

		#endregion

		/// <summary>Converts this list of <see cref="Token"/> to a list of <see cref="LNode"/>.</summary>
		/// <remarks>See <see cref="Token.ToLNode(ISourceFile)"/> for more information.</remarks>
		public LNodeList ToLNodes()
		{
			var list = LNodeList.Empty;
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
}
