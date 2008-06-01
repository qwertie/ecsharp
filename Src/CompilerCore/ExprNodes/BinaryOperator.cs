using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprNodes
{
	/// <summary>A reasonable implementation for infix binary operators (such as e+e, 
	/// e and e, e >> e, and so forth). The implementation of Generate() creates a 
	/// BinaryExpr from the matched tokens.</summary>
	/// <remarks>If you create a BinaryOperator without giving the constructor a name
	/// or expression type, then the name and type are automatically generated from
	/// the tokenText, as follows.
	/// 
	/// - For operators whose tokenText starts with a letter or number, the expression 
	///   type symbol is e_tokenText_e; for example an "and" operator would have the
	///   symbol :e_and_e. Otherwise, the underscores are left out; for example, the
	///   addition operator is given the type symbol :e\+e.
	/// - For operators whose tokenText starts with a letter or number, the expression 
	///   name is "binary 'tokenText'"; for example an "and" operator would have the
	///   name "binary 'and'". Otherwise, the quotes are left out; for example, the
	///   addition operator is given the name "binary +".
	/// </remarks>
	public class BinaryOperator : AbstractOperator<IToken>, IOneOperator<IToken>
	{
		public BinaryOperator(string tokenText, int precedence)
			: this(tokenText, precedence, DefaultBinaryOpName(tokenText), DefaultBinaryOpType(tokenText), null) { }
		public BinaryOperator(string tokenText, int precedence, string name)
			: this(tokenText, precedence, name, DefaultBinaryOpType(tokenText), null) { }
		public BinaryOperator(string tokenText, int precedence, string name, Symbol exprType)
			: this(tokenText, precedence, name, exprType, null) { }
		public BinaryOperator(string tokenText, int precedence, string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(precedence),
				new OneOperatorPart(tokenType, tokenText),
				new OneOperatorPart(precedence),
			}) {}

		object IOneOperator<IToken>.Generate(OneOperatorMatch<IToken> match) { return Generate(match); }
		public BinaryExpr Generate(OneOperatorMatch<IToken> match)
		{
			Debug.Assert(match.Operator == this);
			Debug.Assert(match.Parts.Length == 3);
			Debug.Assert(match.Parts[0].MatchedExpr);
			Debug.Assert(!match.Parts[1].MatchedExpr);
			Debug.Assert(match.Parts[2].MatchedExpr);
			OneOperatorMatch<IToken> matchL = match.Parts[0].Expr;
			OneOperatorMatch<IToken> matchR = match.Parts[2].Expr;
			AstNode exprL = (AstNode)matchL.Operator.Generate(matchL);
			AstNode exprR = (AstNode)matchR.Operator.Generate(matchR);

			return new BinaryExpr(Type, GetIToken(match.Parts[1].Token), exprL, exprR);
		}
	}
}
