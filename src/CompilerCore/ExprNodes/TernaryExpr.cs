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
	public class TernaryExpr : NodeWithFixedParamList
	{
		protected AstNode _expr1;
		protected AstNode _expr2;
		protected AstNode _expr3;

		public TernaryExpr(Symbol nodeType, ITokenValueAndPos positionToken, AstNode expr1, AstNode expr2, AstNode expr3)
			: base(nodeType, positionToken)
		{
			_expr1 = expr1;
			_expr2 = expr2;
			_expr3 = expr3;
		}

		protected internal override int Count(Symbol listId)
		{
			if (listId == _Params)
				return 3;
			else
				return base.Count(listId);
		}
		protected internal override AstNode this[Symbol listId, int index]
		{
			get { 
				if (listId == _Params) {
					if (index == 0)
						return _expr1;
					else if (index == 1)
						return _expr2;
					else if (index == 2)
						return _expr3;
					else
						throw new IndexOutOfRangeException(Localize.From("TernaryExpr: index must be 0, 1 or 2"));
				} else
					return base[listId, index];
			}
			set {
				if (listId == _Params) {
					if (index == 0)
						_expr1 = value;
					else if (index == 1)
						_expr2 = value;
					else if (index == 2)
						_expr3 = value;
					else
						throw new IndexOutOfRangeException(Localize.From("TernaryExpr: index must be 0, 1 or 2"));
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
			get { return _expr2; }
		}
		
		public override IEnumerable<AstNode> AllChildren
		{
			get {
				yield return _expr1;
				yield return _expr2;
				yield return _expr3;
				foreach (AstNode child in base_AllChildren())
					yield return child;
			}
		}
	}
}
