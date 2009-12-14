using System;
using System.Collections.Generic;
using System.Text;

namespace Loyc.CompilerCore
{
	/// <summary>A wrapper around IEnumerable(of IToken) that filters out ITokens where 
	/// VisibleToParser is false.</summary>
	public class VisibleTokenFilter<Tok> : IEnumerable<Tok>
		where Tok : AstNode
	{
		protected IEnumerable<Tok> _source;
		public VisibleTokenFilter(IEnumerable<Tok> source) 
			{ _source = source; }
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
		public IEnumerator<Tok> GetEnumerator()
		{
			foreach (Tok t in _source)
				if (t.Range.Source.Language == null || !Tokens.IsOob(t.NodeType))
					yield return t;
		}
	}
}
