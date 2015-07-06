using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Lexing
{
	/// <summary>Filters out tokens whose <c>Value</c> is <see cref="WhitespaceTag.Value"/>.</summary>
	public class WhitespaceFilter<Token> : LexerWrapper<Token>
		where Token : IHasValue<object>
	{
		public WhitespaceFilter(ILexer<Token> lexer) : base(lexer) { }

		public override Maybe<Token> NextToken()
		{
			do _current = Lexer.NextToken();
			while (_current.HasValue && _current.Value.Value == WhitespaceTag.Value);
			return _current;
		}
	}

	/// <summary>Alias for <c>WhitespaceFilter{Token}</c></summary>
	public class WhitespaceFilter : WhitespaceFilter<Token>
	{
		public WhitespaceFilter(ILexer<Token> lexer) : base(lexer) { }
	}
}
