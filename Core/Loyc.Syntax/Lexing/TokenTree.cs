using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
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

		IToken<int> ITryGet<int, IToken<int>>.TryGet(int index, out bool fail) => TryGet(index, out fail);
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
		public string ToString(Func<Token, ICharSource, string> toStringStrategy = null, ICharSource sourceCode = null)
		{
			StringBuilder sb = new StringBuilder();
			AppendTo(sb, toStringStrategy ?? Token.ToStringStrategy, sourceCode);
			return sb.ToString();
		}
		void AppendTo(StringBuilder sb, Func<Token, ICharSource, string> toStringStrategy, ICharSource sourceCode, int prevEndIndex = 0)
		{
			Token prev = new Token((ushort)0, prevEndIndex, 0);
			for (int i = 0; i < Count; i++)
			{
				Token t = this[i];
				if (t.StartIndex != prev.EndIndex || t.StartIndex <= 0)
					sb.Append(' ');
				sb.Append(toStringStrategy(t, sourceCode));
				if (t.Value is TokenTree)
				{
					var subtree = ((TokenTree)t.Value);
					subtree.AppendTo(sb, toStringStrategy, sourceCode, t.EndIndex);
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
				list.Add(TokenToLNode(item, File));
			return list;
		}

		const int TokenKindShift = 8;

		// Used by TokenToLNode:
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
			for (int kind = 0; kind < stopAt; kind += incr)
			{
				string kindStr = ((TokenKind)kind).ToString();
				table.Add((Symbol)kindStr);
			}
			return table;
		}

		/// <summary>Converts a <see cref="Token"/> to a <see cref="LNode"/>.</summary>
		/// <param name="file">This becomes the <see cref="LNode.Source"/> property.</param>
		/// <remarks>If you really need to store tokens as LNodes, use this. Only
		/// the <see cref="Token.Kind"/>, not the TypeInt, is preserved. Identifiers 
		/// (where Kind==TokenKind.Id and Value is Symbol) are translated as Id 
		/// nodes; everything else is translated as a call, using the TokenKind as
		/// the <see cref="LNode.Name"/> and the value, if any, as parameters. For
		/// example, if it has been treeified with <see cref="TokensToTree"/>, the
		/// token list for <c>"Nodes".Substring(1, 3)</c> as parsed by LES might 
		/// translate to the LNode sequence <c>String("Nodes"), Dot(@@.), 
		/// Substring, LParam(Number(1), Separator(@@,), Number(3)), RParen()</c>.
		/// The <see cref="LNode.Range"/> will match the range of the token.
		/// </remarks>
		public static LNode TokenToLNode(Token token, ISourceFile file)
		{
			var kind = token.Kind;
			Symbol kSym = GSymbol.Empty;
			Symbol id;
			if (kind != TokenKind.Id) {
				int k = (int)kind >> TokenKindShift;
				kSym = _kindAttrTable.TryGet(k, null);
			}

			var r = new SourceRange(file, token.StartIndex, token.Length);
			var c = token.Children;
			if (c != null) {
				if (c.Count != 0)
					r = new SourceRange(file, token.StartIndex, System.Math.Max(token.EndIndex, c.Last.EndIndex) - token.StartIndex);
				return LNode.Call(kSym, c.ToLNodes(), r, token.Style);
			} else if (Token.IsOpenerOrCloser(kind) || token.Value == WhitespaceTag.Value) {
				return LNode.Call(kSym, LNodeList.Empty, r, token.Style);
			} else if (kind == TokenKind.Id && (id = token.Value as Symbol) != null) {
				return LNode.Id(id, r, token.Style);
			} else {
				return LNode.Trivia(kSym, token.Value, r, token.Style);
			}
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
