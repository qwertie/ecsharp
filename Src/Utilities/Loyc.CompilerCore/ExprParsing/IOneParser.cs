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
		where Token : ITokenValueAndPos
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
		/// of the input source. If false, it will parse until it can parse no more
		/// (i.e. until a token is encountered that cannot be considered part of
		/// the expression.)</param>
		/// <returns>On success, an expression representing the tokens. The error
		/// protocol is not yet defined. TODO.</returns>
		Expr Parse(IListSource<Token> source, ref int position, bool untilEnd);
	}
	
	/// <summary>
	/// ONE stands for "One Nonterminal Expression". A OneParser takes a list
	/// of operators described by IOneOperator objects and uses them to parse
	/// expressions supplied to its Parse() function.
	/// </summary><remarks>
	/// A OneParser's job is to generate an expression tree from a list of tokens
	/// and operators supplied by the caller.
	/// <para/>
	/// There is only one ONEP implementation at the moment. It is called
	/// BasicOneParser, and it works best if the the tokens already have been
	/// converted to a tree using EssentialTreeParser.
	/// </remarks>
	public interface IOneParser<Token> : 
		IEnumerable<IOneOperator<Token>>,
		IExprParser<OneOperatorMatch<Token>, Token>
		where Token : ITokenValueAndPos
	{
		void Add(IOneOperator<Token> op);
		void AddRange(IEnumerable<IOneOperator<Token>> ops);
		void Clear();
		int OperatorCount { get; }
    }
}
