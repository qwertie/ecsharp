using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;

namespace Loyc.Syntax.Lexing
{
	/// <summary>A lexer wrapper that saves whitespace tokens into a list (<see cref="TriviaList"/>).</summary>
	/// <remarks>
	/// This wrapper filters out all whitespace tokens (where <see cref="Token.Value"/> is 
	/// <see cref="WhitespaceTag.Value"/>) and saves them in a list. It is typically used with 
	/// <seealso cref="StandardTriviaInjector"/>.
	/// </remarks>
	public class TriviaSaver : LexerWrapper<Token>
	{
		/// <summary>Initializer.</summary>
		/// <param name="lexer">Lexer to wrap.</param>
		/// <param name="newlineTypeInt">In some languages, newlines are not considered 
		/// whitespace but they still need to be saved in the trivia list. If the 
		/// <see cref="Token.TypeInt"/> equals this value, the token is saved but NOT filtered out.</param>
		public TriviaSaver(ILexer<Token> lexer, int newlineTypeInt = int.MinValue) : base(lexer) { _newlineTypeInt = newlineTypeInt; }

		int _newlineTypeInt;
		DList<Token> _trivia = new DList<Token>();
		public IListAndListSource<Token> TriviaList { get { return _trivia; } }

		public override Maybe<Token> NextToken()
		{
			while ((_current = Lexer.NextToken()).HasValue) {
				var tok = _current.Value;
				if (tok.Value != WhitespaceTag.Value) {
					if (tok.TypeInt == _newlineTypeInt)
						TriviaList.Add(tok);
					break;
				}
				TriviaList.Add(tok);
			}
			return _current;
		}
	}
}
