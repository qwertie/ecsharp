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
	public class NullaryExpr : NodeWithFixedParamList
	{
		public NullaryExpr(Symbol nodeType, ITokenValueAndPos positionToken)
			: base(nodeType, positionToken)
		{
		}

		protected internal override int Count(Symbol listId)
		{
			if (listId == _Params)
				return 0;
			else
				return base.Count(listId);
		}
		protected internal override AstNode this[Symbol listId, int index]
		{
			get { 
				if (listId == _Params) {
					throw new IndexOutOfRangeException(Localize.From("NullaryExpr has no parameters"));
				} else
					return base[listId, index];
			}
			set {
				if (listId == _Params) {
					throw new IndexOutOfRangeException(Localize.From("NullaryExpr has no parameters"));
				} else
					base[listId, index] = value;
			}
		}
		public override AstNode FirstParam
		{
			get { return null; }
		}
		public override AstNode SecondParam
		{
			get { return null; }
		}
	}

	public class IdExpr : NullaryExpr
	{
		public static readonly Symbol DefaultType = Symbol.Get("ID");

	    public IdExpr(IToken identifier)
	        : base(DefaultType, identifier)
	    {
	    }
		public override string Name
		{ 
			get { return _positionToken.Text; }
		}
	}
}
