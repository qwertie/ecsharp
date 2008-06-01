using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprNodes
{
	public class IDOperator : AbstractOperator<IToken>, IOneOperator<IToken>
	{
		public IDOperator() : this(Localize.From("identifier"), IdExpr.DefaultType, Tokens.ID) {}
		public IDOperator(string name, Symbol exprType, Symbol tokenType)
			: base(name, exprType, new OneOperatorPart[] {
				new OneOperatorPart(tokenType),
			}) {}

		object IOneOperator<IToken>.Generate(OneOperatorMatch<IToken> match) { return Generate(match); }
		public IdExpr Generate(OneOperatorMatch<IToken> match)
		{
			Debug.Assert(match.Operator == this);
			Debug.Assert(match.Parts.Length == 1);
			Debug.Assert(!match.Parts[0].MatchedExpr);

			return new IdExpr(GetIToken(match.Parts[0].Token));
		}
	}
}
