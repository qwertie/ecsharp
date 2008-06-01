using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore.ExprNodes
{	
	/// <summary>
	/// A base class that throws NotSuportedException on any attempt to modify the 
	/// Params list.
	/// </summary>
	/// <remarks>
	/// The derived class must override the following:
	/// <code lang="C#">
	///	protected override int Count(Symbol listId);
	///	protected override AstNode this[Symbol listId, int index] { get; set; }
	///	public override IEnumerable&lt;AstNode&gt; AllChildren { get; }
	/// </code>
	/// For efficiency, it may optionally override the following:
	/// <code>
	/// public override AstNode FirstParam { get; set; }
	///	public override AstNode SecondParam { get; set; }
	/// </code>
	/// </remarks>
	public class NodeWithFixedParamList : AstNode
	{
		public NodeWithFixedParamList(Symbol nodeType, ITokenValueAndPos positionToken) : base(nodeType, positionToken) {}
		public NodeWithFixedParamList(Symbol nodeType, SourcePos position) : base(nodeType, position) { }

		private void AutoThrow(Symbol listId)
		{
			if (listId == _Params)
				throw new NotSupportedException(Localize.From
					("{0}'s parameter list has a fixed size.", GetType().Name));
		}
		protected internal override void InsertRange(Symbol listId, int index, IEnumerable<AstNode> items)
		{
			AutoThrow(listId);
			base.InsertRange(listId, index, items);
		}
		protected internal override void Insert(Symbol listId, int index, AstNode item)
		{
			AutoThrow(listId);
			base.Insert(listId, index, item);
		}
		protected internal override void RemoveAt(Symbol listId, int index)
		{
			AutoThrow(listId);
			base.RemoveAt(listId, index);
		}
		protected internal override void Clear(Symbol listId)
		{
			AutoThrow(listId);
			base.Clear(listId);
		}
		
		// used in derived classes to avoid warning CS1911
		protected IEnumerable<AstNode> base_AllChildren() { return base.AllChildren; }
	}
}
