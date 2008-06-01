using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprNodes
{
	/// <summary>A reasonable implementation for infix ternary operators such as e?e:e.
	/// The implementation of Generate() creates a TernaryExpr from the matched 
	/// tokens.</summary>
	/// <remarks>The TernaryOperator constructors work in a similar way to the 
	/// <see cref="BinaryOperator"/> constructors, except that (1) an infix ternary 
	/// operator is generated, of course, so two token strings are required between
	/// the three expressions, and (2) the auto-generated names and types have forms 
	/// such as "ternary ? :" and :e_\?_e_\:_e instead of "binary ?" and :e_\?_e.</remarks>
	public class TernaryOperator : AbstractOperator<IToken>, IOneOperator<IToken>
	{
		public TernaryOperator(string tokenText1, string tokenText2, int precedence)
			: this(tokenText1, tokenText2, precedence, DefaultTernaryOpName(tokenText1, tokenText2), DefaultTernaryOpType(tokenText1, tokenText2)) { }
		public TernaryOperator(string tokenText1, string tokenText2, int precedence, string name)
			: this(tokenText1, tokenText2, precedence, name, DefaultTernaryOpType(tokenText1, tokenText2)) { }
		public TernaryOperator(string tokenText1, string tokenText2, int precedence, string name, Symbol exprType)
			: this(tokenText1, tokenText2, precedence, name, exprType, null, null) { }
		public TernaryOperator(string tokenText1, string tokenText2, int precedence, string name, Symbol exprType, Symbol tokenType1, Symbol tokenType2)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(precedence),
				new OneOperatorPart(tokenType1, tokenText1),
				new OneOperatorPart(Math.Max(precedence, 100)),
				new OneOperatorPart(tokenType2, tokenText2),
				new OneOperatorPart(precedence),
			}) {}

		object IOneOperator<IToken>.Generate(OneOperatorMatch<IToken> match) { return Generate(match); }
		public TernaryExpr Generate(OneOperatorMatch<IToken> match)
		{
			Debug.Assert(match.Operator == this);
			Debug.Assert(match.Parts.Length == 5);
			Debug.Assert(match.Parts[0].MatchedExpr);
			Debug.Assert(!match.Parts[1].MatchedExpr);
			Debug.Assert(match.Parts[2].MatchedExpr);
			Debug.Assert(!match.Parts[3].MatchedExpr);
			Debug.Assert(match.Parts[4].MatchedExpr);
			OneOperatorMatch<IToken> match1 = match.Parts[0].Expr;
			OneOperatorMatch<IToken> match2 = match.Parts[2].Expr;
			OneOperatorMatch<IToken> match3 = match.Parts[4].Expr;
			AstNode expr1 = (AstNode)match1.Operator.Generate(match1);
			AstNode expr2 = (AstNode)match2.Operator.Generate(match2);
			AstNode expr3 = (AstNode)match3.Operator.Generate(match3);

			return new TernaryExpr(Type, GetIToken(match.Parts[1].Token), expr1, expr2, expr3);
		}
	}
}
