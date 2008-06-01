using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprNodes
{
	/// <summary>Base class for PrefixOperator and PostfixOperator</summary>
	public abstract class UnaryOperator : AbstractOperator<IToken>, IOneOperator<IToken>
	{
		public UnaryOperator(string name, Symbol type) : base(name, type) {}
		public UnaryOperator(string name, Symbol type, IOperatorPartMatcher[] tokens) : base(name, type, tokens) { }
		
		object IOneOperator<IToken>.Generate(OneOperatorMatch<IToken> match) { return Generate(match); }
		public abstract UnaryExpr Generate(OneOperatorMatch<IToken> match);
	}
	/// <summary>A reasonable implementation for prefix operators (such as -e, !e, ~e, 
	/// not e, and so forth). The implementation of Generate() creates a UnaryExpr from
	/// the matched tokens.</summary>
	/// <remarks>The PrefixOperator constructors work the same way as for 
	/// <see cref="BinaryOperator"/>, except that (1) a prefix operator is generated, 
	/// of course, (2) the auto-generated names and types have forms such as "prefix -" 
	/// and :\-e instead of "binary -" and :e\-e.</remarks>
	public class PrefixOperator : UnaryOperator
	{
		public PrefixOperator(string tokenText, int precedence)
			: this(tokenText, precedence, DefaultPrefixOpName(tokenText), DefaultPrefixOpType(tokenText), null) { }
		public PrefixOperator(string tokenText, int precedence, string name)
			: this(tokenText, precedence, name, DefaultPrefixOpType(tokenText), null) { }
		public PrefixOperator(string tokenText, int precedence, string name, Symbol exprType)
			: this(tokenText, precedence, name, exprType, null) { }
		public PrefixOperator(string tokenText, int precedence, string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(tokenType, tokenText),
				new OneOperatorPart(precedence),
			}) {}

		public override UnaryExpr Generate(OneOperatorMatch<IToken> match)
		{
			Debug.Assert(match.Operator == this);
			Debug.Assert(match.Parts.Length == 2);
			Debug.Assert(!match.Parts[0].MatchedExpr);
			Debug.Assert(match.Parts[1].MatchedExpr);
			OneOperatorMatch<IToken> subMatch = match.Parts[1].Expr;
			AstNode subExpr = (AstNode)subMatch.Operator.Generate(subMatch);

			return new UnaryExpr(Type, GetIToken(match.Parts[0].Token), subExpr);
		}
	}
	
	/// <summary>A reasonable implementation for postfix operators (such as the 
	/// increment/decrement operators e++ and e-- found in C-family languages.)
	/// The implementation of Generate() creates a UnaryExpr from the matched tokens.
	/// </summary>
	/// <remarks>The PostfixOperator constructors work the same way as for 
	/// <see cref="BinaryOperator"/>, except that (1) a postfix operator is generated, 
	/// of course, (2) the auto-generated names and types have forms such as "postfix *" 
	/// and :e\* instead of "binary -" and :e\*e.</remarks>
	public class PostfixOperator : UnaryOperator
	{
		public PostfixOperator(string tokenText, int precedence)
			: this(tokenText, precedence, DefaultPostfixOpName(tokenText), DefaultPostfixOpType(tokenText), null) { }
		public PostfixOperator(string tokenText, int precedence, string name)
			: this(tokenText, precedence, name, DefaultPostfixOpType(tokenText), null) { }
		public PostfixOperator(string tokenText, int precedence, string name, Symbol exprType)
			: this(tokenText, precedence, name, exprType, null) { }
		public PostfixOperator(string tokenText, int precedence, string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(precedence),
				new OneOperatorPart(tokenType, tokenText),
			}) {}
			
		public override UnaryExpr Generate(OneOperatorMatch<IToken> match)
		{
			Debug.Assert(match.Operator == this);
			Debug.Assert(match.Parts.Length == 2);
			Debug.Assert(match.Parts[0].MatchedExpr);
			Debug.Assert(!match.Parts[1].MatchedExpr);
			OneOperatorMatch<IToken> subMatch = match.Parts[0].Expr;
			AstNode subExpr = (AstNode)subMatch.Operator.Generate(subMatch);

			return new UnaryExpr(Type, GetIToken(match.Parts[1].Token), subExpr);
		}
	}

}
