/*
 * Created by David on 7/17/2007 at 7:11 AM
 */
using System;
using System.Collections.Generic;
using Loyc.Runtime;
//using Loyc.CompilerCore.Expressions;

namespace Loyc.CompilerCore.ExprParsing
{
	/// <summary>
	/// This interface is the expression-parsing part of IOneParser.
	/// </summary>
	public interface IExprParser<Expr, Token>
		where Token : ITokenValue
	{
		/// <summary>Parses an expression from a token list.</summary>
		/// <param name="source">A list of input tokens.</param>
		/// <param name="position">Upon entry, position is the first token index to 
		/// examine in the source; All prior tokens are ignored. On exit, position is the 
		/// position of the end of the accepted expression. If parsing fails, the value of 
		/// position should be between the start position and source.Count, but is 
		/// otherwise undefined.
		/// </param>
		/// <param name="untilEnd">If true, the parser will try to parse until the end 
		/// of the input source. If false, it will parse until it can parse no more.</param>
		/// <param name="divider">Optional divider to use to divide PUNC tokens. 
		/// If null, no division is performed. The parser will not call ProcessOperators()
		/// on the divider; it should be assumed that the divider has already been 
		/// prepared.</param>
		/// <returns>On success, an expression representing the tokens. The error
		/// protocol is not yet defined. TODO.</returns>
		/// <remarks>Not every parser has to support division, although it is required 
		/// in many situations for a successful parse.</remarks>
		Expr Parse(ISourceFile<Token> source, ref int position, bool untilEnd, IOperatorDivider<Token> divider);
	}
	
	/// <summary>
	/// ONE stands for "One Nonterminal Expression". A OneParser takes a list
	/// of operators described by IOneOperator objects and uses them to parse
	/// expressions supplied to its Parse() function.
	/// </summary><remarks>
	/// A OneParser's job is to generate an expression tree from a list of tokens
	/// and operators supplied by the caller. The tree is composed of 'Expr' nodes, where Expr is a 
	/// generic parameter derived from ITokenValue, from a list of ITokens that have 
	/// already been through a tree parser.
	/// 
	/// Before parsing with a divider, prepare the divider with 
	/// IOperatorDivider.ProcessOperators() (You can use the IOneParser as the
	/// argument to that method).
	/// <code>
	/// IOperatorDivider.ProcessOperators(IOneParser.GetEnumerator())
	/// </code>
	/// </remarks>
	public interface IOneParser<Token> : 
		IEnumerable<IOneOperator<Token>>
		where Token : ITokenValue
	{
		void Add(IOneOperator<Token> op);
		void AddRange(IEnumerable<IOneOperator<Token>> ops);
		void Clear();
		int OperatorCount { get; }
		OneOperatorMatch<Token> Parse(ISourceFile<Token> source, ref int position, bool untilEnd, IOperatorDivider<Token> divider);
    }
}
