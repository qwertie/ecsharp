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
	public class BinaryExpr : NodeWithFixedParamList
	{
		protected AstNode _expr1;
		protected AstNode _expr2;

		public BinaryExpr(Symbol nodeType, SourceRange range, AstNode expr1, AstNode expr2)
			: base(nodeType, range)
		{
			_expr1 = expr1;
			_expr2 = expr2;
		}

		protected override int Count(Symbol listId)
		{
			if (listId == _Params)
				return 2;
			else
				return base.Count(listId);
		}
		protected override AstNode this[Symbol listId, int index]
		{
			get { 
				if (listId == _Params) {
					if (index == 0)
						return _expr1;
					else if (index == 1)
						return _expr2;
					else
						throw new IndexOutOfRangeException(Localize.From("BinaryExpr: index must be 0 or 1"));
				} else
					return base[listId, index];
			}
			set {
				if (listId == _Params) {
					if (index == 0)
						_expr1 = value;
					else if (index == 1)
						_expr2 = value;
					else
						throw new IndexOutOfRangeException(Localize.From("BinaryExpr: index must be 0 or 1"));
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
				foreach (AstNode child in base_AllChildren())
					yield return child;
			}
		}
	}
}
