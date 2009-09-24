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
	/*/// <summary>
	/// LoycExpr serves as the base class of all Loyc expressions.
	/// </summary>
	/// <remarks>
	/// </remarks>
	public abstract class LoycExpr : LoycCodeNode
	{
		protected IOneOperator<IToken> _operator;
		public IOneOperator<IToken> Operator { get { return _operator; } }

		public LoycExpr(IAstNode prototype, IOneOperator<IToken> op)
			: base(prototype, op.NodeType)
		{
			_operator = op;
		}
		public LoycExpr(IAstNode prototype, IOneOperator<IToken> op, IAstNode originalParent)
			: base(prototype, op.NodeType, originalParent)
		{
			_operator = op;
		}

		#region Children management: default implementation

		public virtual IList<LoycExpr> ExprChildren
		{
			get { return (IList<LoycExpr>)Attr(_Attrs); }
			set { SetAttr(_Attrs, value); }
		}
		public override IList<IAstNode> Params
		{
			get { return new ListTUpcaster<LoycExpr,IAstNode>(ExprChildren); }
			set { throw new NotImplementedException(); }
		}
		public override IAstNode FirstChild 
		{
			get {
				if (ChildCount < 1) 
					return null;
				return ExprChildren[0];
			}
			set { ExprChildren[0] = (LoycExpr)value; }
		}
		public override IAstNode SecondChild 
		{ 
			get {
				if (ChildCount < 2) 
					return null;
				return ExprChildren[1];
			}
			set { ExprChildren[1] = (LoycExpr)value; }
		}
		public override IAstNode LastChild 
		{
			get {
				if (ChildCount < 1)
					return null;
				return ExprChildren[ChildCount - 1];
			}
			set { ExprChildren[ChildCount - 1] = (LoycExpr)value; }
		}
		public virtual LoycExpr FirstExpr
		{
			get {
				if (ChildCount < 1) 
					return null;
				return ExprChildren[0];
			}
			set { ExprChildren[0] = value; }
		}
		public virtual LoycExpr SecondExpr
		{
			get {
				if (ChildCount < 2) 
					return null;
				return ExprChildren[1];
			}
			set { ExprChildren[1] = value; }
		}
		public virtual LoycExpr LastExpr
		{
			get {
				if (ChildCount < 1)
					return null;
				return ExprChildren[ChildCount - 1];
			}
			set { ExprChildren[ChildCount - 1] = value; }
		}
		public override int ChildCount 
		{
			get {
				IList<LoycExpr> c = ExprChildren;
				if (c == null)
					return 0;
				else
					return c.Count;
			}
		}

		#endregion
	}
	*/
}
