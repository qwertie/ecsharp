using System;
using System.Collections.Generic;
using System.Text;
//using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using Loyc.Utilities;
using Loyc.Runtime;
using Loyc.CompilerCore.ExprParsing;

namespace Loyc.CompilerCore.ExprNodes
{
	public class UnaryExpr : NodeWithFixedParamList
	{
		protected AstNode _expr1;

		public UnaryExpr(Symbol nodeType, SourceRange range, AstNode expr)
			: base(nodeType, range)
		{
			_expr1 = expr;
		}

		protected override int Count(Symbol listId)
		{
			if (listId == _Params)
				return 1;
			else
				return base.Count(listId);
		}
		protected override AstNode this[Symbol listId, int index]
		{
			get { 
				if (listId == _Params) {
					if (index == 0)
						return _expr1;
					else
						throw new IndexOutOfRangeException(Localize.From("UnaryExpr: index must be 0"));
				} else
					return base[listId, index];
			}
			set {
				if (listId == _Params) {
					if (index == 0)
						_expr1 = value;
					else
						throw new IndexOutOfRangeException(Localize.From("UnaryExpr: index must be 0"));
				} else
					base[listId, index] = value;
			}
		}
		public override AstNode FirstParam
		{
			get { return _expr1; }
		}
		public override AstNode SecondParam
		{
			get { return null; }
		}
		public override IEnumerable<AstNode> AllChildren
		{
			get {
				yield return _expr1;
				foreach (AstNode child in base_AllChildren())
					yield return child;
			}
		}
	}
}
