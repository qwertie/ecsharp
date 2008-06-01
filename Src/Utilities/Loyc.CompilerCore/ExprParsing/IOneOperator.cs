/*
 * Created by David on 7/17/2007 at 7:11 AM
 */

using System;
using System.Collections.Generic;
using Loyc.Runtime;
using System.Diagnostics;
using Loyc.Utilities;
using System.Text;

namespace Loyc.CompilerCore.ExprParsing
{
	/// <summary>
	/// Interface for a class that supplies an operator specification to an IOneParser.
	/// </summary>
	public interface IOneOperator<Token> 
		where Token : ITokenValue
	{
		/// <summary>A user-visible name for the operator, e.g. "binary plus". This 
		/// member is informational only.</summary>
		string Name { get; }
		
		/// <summary>The operator's type identifier.</summary>
		/// <remarks>This member is informational. It should match the Type of 
		/// expressions produced by this operator, unless the operator may resolve 
		/// to expressions of more than one type.</remarks>
		Symbol Type { get; }
		
		/// <summary>A list of parts of the operator. For best performance, this 
		/// property is a simple array.</summary>
		IOperatorPartMatcher[] Parts { get; }
		
		/// <summary>Determines whether this operator should take priority over the 
		/// 'other' operator.</summary>
		/// <returns>1 if this operator part takes priority over parts of the other operator,
		/// -1 if the other operator has higher priority, and 0 otherwise.</returns>
		/// <remarks>ComparePriority() has the opportunity to disambiguate this 
		/// operators with others, but the other operator is given the same opportunity.
		/// If both ComparePriority methods return the same value, the conflict remains
		/// unresolved, but if the two methods are in agreement, or if one is neutral 
		/// (returning 0) while the other is not, then ambiguity between the two 
		/// operators is resolved.
		/// 
		/// The parser may attempt to determine priorities in advance or it may call this
		/// method only when an ambiguity arises in an actual expression. 
		/// BasicOneParser uses the latter approach.
		/// 
		/// The precise effect of specifying a priority between two operators is left 
		/// undefined. BasicOneParser, I'm afraid, has a pretty simple disambiguation 
		/// mechanism that is actually pretty dumb in some cases.
		/// </remarks>
		int ComparePriority(IOneOperator<Token> other);
		
		/// <summary>An IOneParser uses this method in case of ambiguity to query
		/// whether an interpretation is valid.</summary>
		/// <param name="match">Description of the match.</param>
		/// <param name="output">Errors and warnings about a failure should be sent 
		/// to this object. The messages will not be output unless disambiguation 
		/// fails.</param>
		/// <returns></returns>
		/// <remarks>
		/// IOneParser need not call this method if there is no ambiguity, although it is 
		/// allowed to.
		/// </remarks>
		bool IsAcceptable(OneOperatorMatch<Token> match, ISimpleMessageSink output);

        /// <summary>Generates an expression from a match (a tree of tokens that
        /// match this operator). Can return null to reject the match.
        /// </summary>
        /// <param name="match">A match to this operator.</param>
        /// <remarks>The IOneParser only calls Generate() on the outermost
        /// expression; it is the respoisibility of each operator to call Generate()
        /// on its own arguments to generate the subexpressions, if applicable.
        /// 
        /// Originally, Generate() returned Expr, which was a generic parameter of
        /// this interface. However, for this to work, OneOperatorMatch and
        /// OneOperatorMatchPart also had to have an Expr argument (i.e.
        /// OneOperatorMatch(of Expr, Token)) so that OneOperatorMatch could contain a
        /// references to its operator. However, this made it impossible for
        /// Generate() to return match itself, because there is no way to specify
        /// OneOperatorMatch as a type argument to itself (i.e. OneOperatorMatch
        /// (of OneOperatorMatch, Token) is not allowed.) Therefore, I changed it to
        /// return object.</remarks>
		object Generate(OneOperatorMatch<Token> match);
	}

    /// <summary>
    /// A tree of tokens built by an IOneParser-based parser (typically BasicOneParser)
    /// to represent an expression.
    /// </summary>
    /// <typeparam name="Token">Any token that implements ITokenValue</typeparam>
	public class OneOperatorMatch<Token> : ITokenValue
		where Token : ITokenValue
	{
		public OneOperatorMatch() { }
		public OneOperatorMatch(IOneOperator<Token> op, OneOperatorMatchPart<Token>[] parts)
		{
			Operator = op;
			Parts = parts;
		}
		public OneOperatorMatch(OneOperatorMatch<Token> other)
		{
			this.Operator = other.Operator;
			this.Parts = other.Parts;
		}
		/// <summary>This flag is set by IOneParser when IOneOperator.IsAcceptable
		/// returns true. This value is informational, intended to let 
		/// IOneOperator.Generate() know whether IsAcceptable has been called.</summary>
		public bool IsAcceptable;
		/// <summary></summary>
		public IOneOperator<Token> Operator;
		public OneOperatorMatchPart<Token>[] Parts;

		public override string ToString()
		{
			if (Parts == null || Parts.Length == 0)
				return string.Empty;
			StringBuilder sb = new StringBuilder(Parts[0].ToString());
			for (int i = 1; i < Parts.Length; i++) {
				sb.Append(' ');
				sb.Append(Parts[i].ToString());
			}
			return sb.ToString();
		}

		#region ITokenValue Members
		public Symbol NodeType
		{
			get { return Operator.Type; }
		}
		public string Text
		{
			get { return ToString(); }
		}
		#endregion
	}
		
	/// <summary>Describes the match made to one OneOperatorPart. Either 'Expr'
	/// or 'Token' are non-null but not both. 'Expr' is non-null if this part matched a 
	/// sub-expression, otherwise Token is non-null.</summary>
	public struct OneOperatorMatchPart<TokenT> : ITokenValue
		where TokenT : ITokenValue
	{
		public OneOperatorMatchPart(OneOperatorMatch<TokenT> expr)
			{ Expr = expr; Token = default(TokenT); }
		public OneOperatorMatchPart(TokenT token)
			{ Expr = null; Token = token; }
		
		public OneOperatorMatch<TokenT> Expr;
		public ITokenValue Token;

		public bool MatchedExpr
		{
			get { return Expr != null; }
		}
		public override string ToString() 
		{
			if (Expr != null)
				return string.Format("({0})", Expr.ToString());
			else
				return Token.Text;
		}

		#region ITokenValue Members
		public Symbol NodeType
		{
			get
			{
				if (Expr != null)
					return Expr.NodeType;
				else
					return Token.NodeType;
			}
		}
		public string Text
		{
			get { return ToString(); }
		}
		#endregion
	}

    /*
	/// <summary>This wrapper class is needed in case IOneOperator wants to
	/// return the match itself from its Generate() method. It's not possible 
	/// to declare a OneOperatorMatch with itself as a template argument, i.e.
	/// you can't say OneOperatorMatch<OneOperatorMatch, Token>. In other words, 
	/// it's impossible for Generate() to return a OneOperatorMatch<Expr,Token> 
	/// if Expr is OneOperatorMatch<Expr,Token> itself, because it would be a 
	/// circular reference and impossible to type in. Weird eh? You can, 
	/// however, create this wrapper with the OneOperatorMatch inside it.
	/// </summary>
	public struct MatchWrapper<Token>
		where Token : ITokenValue
	{
		public OneOperatorMatch<MatchWrapper<Token>, Token> Match;
		public MatchWrapper(OneOperatorMatch<MatchWrapper<Token>, Token> match)
			{ Match = match; }
		public static implicit operator 
			OneOperatorMatch<MatchWrapper<Token>, Token>(MatchWrapper<Token> m) 
			{ return m.Match; }
		public override string ToString()
			{ return Match.ToString(); }
	}*/
}
