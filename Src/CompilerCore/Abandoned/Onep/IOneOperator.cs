/*
 * Created by David on 7/17/2007 at 7:11 AM
 */

using System;
using System.Collections.Generic;
using Loyc.Runtime;
using System.Diagnostics;
using Loyc.Utilities;
using System.Text;

namespace Loyc.CompilerCore.Onep
{
	/// <summary>
	/// Interface for a class that supplies an operator specification to an IOneParser.
	/// </summary>
	public interface IOneOperator<Expr,Token> 
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
		
		/// <summary>A list of parts of the operator. For performance reasons,
		/// this property is a simple array.</summary>
		OneOperatorPart<Token>[] Parts { get; }
		
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
		int ComparePriority(IOneOperator<Expr,Token> other);
		
		/// <summary>An IOneParser uses this method in case of ambiguity to query
		/// whether an interpretation is valid.</summary>
		/// <param name="match">Description of the match.</param>
		/// <param name="output">Errors and warnings about a failure should be sent to this object.</param>
		/// <returns></returns>
		/// <remarks>Do not keep the match or its Parts array for future reference. To 
		/// maximize efficiency, the caller may re-use the same array for different matches. 
		/// Make a copy if you need one.
		///
		/// IOneParser need not call this method if there is no Ambiguity , although it is 
		/// allowed to.
		/// </remarks>
		bool IsAcceptable(OneOperatorMatch<Expr,Token> match, IOneMessageSink output);
		
		/// <summary>Generates an expression from a list of match parts (a tree of 
		/// tokens that makes up this operator). This method can return null to 
		/// reject the arguments to the expression.
		/// </summary>
		/// <param name="parts">A list of the parts of the operator.</param>
		/// <remarks>The IOneParser only calls Generate() on the outermost expression;
		/// it is the respoisibility of each operator to call Generate() on its own 
		/// arguments to generate the subexpressions, if applicable.</remarks>
		Expr Generate(OneOperatorMatch<Expr,Token> match);
	}

	/// <summary>This is an object to which errors, warnings and notices should be 
	/// sent by <see cref="IOneOperator.Generate"/>().</summary>
	/// <remarks>
	/// </remarks>
	public interface IOneMessageSink
	{
		/// <summary>
		/// Writes a message to the screen or an internal queue for later output. 
		/// The IOneParser will add the filename and position to the message if it is 
		/// output.
		/// </summary>
		/// <param name="category">Type of message. See <see cref="Output"/> for a list of standard categories.</param>
		/// <param name="lang">Language to use in the call to <see cref="Translate"/>.
		/// Use "EN" if the source language is English.</param>
		/// <param name="msg">Untranslated format string to display</param>
		/// <param name="args">Arguments to string.Format()</param>
		/// <remarks>When the message is displayed, lang, msg and args[] are 
		/// passed to <see cref="Translate.Do"/>().
		/// 
		/// The string may never be put on the screen. When an ambiguity is detected,
		/// IOneParser calls <see cref="IOneOperator.Generate"/>() for all possible
		/// interpretations. If a generator fails, it should output an error message via
		/// this method describing why it failed (although it is not required to). The
		/// message is stored in a queue that is discarded if the ambiguity is resolved,
		/// and displayed otherwise.
		/// 
		/// You pass the components of the error message to Write() rather than the
		/// complete message for three reasons: (1) the string may not be output, so
		/// there is no reason to waste CPU cycles on translating and formatting it. 
		/// (2) If the 'msg' values from different sources are the same, IOneParser may 
		/// want to merge them somehow (but this is not currently done).
		/// (3) Potentially, the variable parts of the message can be highlighted by
		/// the output code.
		/// </remarks>
		void Write(Symbol category, string lang, string msg, params object[] args);
		
		/// <summary>
		/// Writes a message to the screen or an internal queue for later output. This
		/// is like the other overload except that a resource name is specified.
		/// </summary>
		/// <param name="category">Type of message. See <see cref="Output"/> for a list of standard categories.</param>
		/// <param name="resource">Name to give to the first argument of
		/// <see cref="Translate.Do"/>()</param>
		/// <param name="args">Arguments to string.Format()</param>
		void Write(Symbol category, Symbol resource, params object[] args);
	}

	public class OneOperatorMatch<Expr, Token> : ITokenValue
		where Token : ITokenValue
	{
		public OneOperatorMatch() { }
		public OneOperatorMatch(IOneOperator<Expr, Token> op, OneOperatorMatchPart<Expr, Token>[] parts)
		{
			Operator = op;
			Parts = parts;
		}
		public OneOperatorMatch(OneOperatorMatch<Expr,Token> other)
		{
			this.Operator = other.Operator;
			this.Parts = other.Parts;
		}
		/// <summary>This flag is set by IOneParser when IOneOperator.IsAcceptable
		/// returns true. This value is informational, intended to let 
		/// IOneOperator.Generate() know whether IsAcceptable has been called.</summary>
		public bool IsAcceptable;
		/// <summary></summary>
		public IOneOperator<Expr,Token> Operator;
		public OneOperatorMatchPart<Expr,Token>[] Parts;

		public override string ToString() { return ToString(false); }
		public string ToString(bool addParens)
		{
			if (Parts == null || Parts.Length == 0)
				return string.Empty;
			StringBuilder sb = new StringBuilder(Parts[0].ToString(addParens));
			for (int i = 1; i < Parts.Length; i++) {
				sb.Append(' ');
				sb.Append(Parts[i].ToString(addParens));
			}
			return sb.ToString();
		}

		#region ITokenValue Members
		public Symbol Type
		{
			get { return Operator.Type; }
		}
		public string Text
		{
			get { return ToString(false); }
		}
		#endregion
	}
		
	/// <summary>Describes the match made to one OneOperatorPart. Either 'Expr'
	/// or 'Token' are non-null but not both. 'Expr' is non-null if this part matched a 
	/// sub-expression, otherwise Token is non-null.</summary>
	public struct OneOperatorMatchPart<ExprT, TokenT> : ITokenValue
		where TokenT : ITokenValue
	{
		public OneOperatorMatchPart(OneOperatorMatch<ExprT,TokenT> expr)
			{ Expr = expr; Token = default(TokenT); }
		public OneOperatorMatchPart(TokenT token)
			{ Expr = null; Token = token; }
		
		public OneOperatorMatch<ExprT, TokenT> Expr;
		public ITokenValue Token;

		public bool MatchedExpr
		{
			get { return Expr != null; }
		}
		public override string ToString() { return ToString(false); }
		public string ToString(bool addParens) 
		{
			if (Expr != null)
				return string.Format("({0})", Expr.ToString(addParens));
			else
				return Token.Text;
		}

		#region ITokenValue Members
		public Symbol Type
		{
			get
			{
				if (Expr != null)
					return Expr.Type;
				else
					return Token.Type;
			}
		}
		public string Text
		{
			get { return ToString(false); }
		}
		#endregion
	}

	/// <summary>This wrapper class is needed in case IOneOperator wants to
	/// return the match itself from its Generate() method. It's not possible 
	/// to declare a OneOperatorMatch with itself as a template argument, i.e.
	/// you can't say OneOperatorMatch<OneOperatorMatch, Token>. In other words, 
	/// it's impossible for Generate() to return a OneOperatorMatch<Expr,Token> 
	/// if Expr is OneOperatorMatch<Expr,Token> itself, because it would be a 
	/// circular reference and impossible to type. Weird eh? You can, however, 
	/// create this wrapper with the OneOperatorMatch inside it.
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
	}
}
