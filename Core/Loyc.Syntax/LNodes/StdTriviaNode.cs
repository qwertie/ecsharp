//
// Contains the standard immutable node types, all of which have a name that 
// starts with "Std".
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Utilities;
using Loyc;
using System.Diagnostics;
using Loyc.Syntax.Les;

namespace Loyc.Syntax
{

	/// <summary>A simple call node with a single literal argument. </summary>
	/// <remarks>
	/// Essentially, this is a special kind of node with both a name and a value.
	/// Since there is no syntax (or <see cref="LNodeKind"/>) for a node that has
	/// both a Name and a Value, the node pretends that it is has a single argument,
	/// Args[0], which allows this node to be printed as if it were a normal call
	/// node. For example, if this node has Name=(Symbol)"PI" and Value=3.1415,
	/// it will be printed as <c>PI(3.1415)</c>. The <see cref="TriviaValue"/>
	/// property returns this value (in this case, (object)3.1415). Please note
	/// that the normal <see cref="LNode.Value"/> is still <see cref="NoValue.Value"/>
	/// so that if the node is printed and reparsed, it doesn't behave differently.
	/// <para/>
	/// This node type is used to represent tokens and trivia nodes with values.
	/// </remarks>
	internal class StdTriviaNode : CallNode
	{
		public StdTriviaNode(Symbol name, object value, LNode ras)
			: base(ras)          { _name = name ?? GSymbol.Empty; _tokenValue = value; }
		public StdTriviaNode(Symbol name, object value, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(range, style) { _name = name ?? GSymbol.Empty; _tokenValue = value; }
		
		private Symbol _name;
		private object _tokenValue;

		public sealed override Symbol Name { get { return _name; } }
		public sealed override object TriviaValue { get { return _tokenValue; } }

		public override LNodeList Args
		{
			get { 
				if (_tokenValue != NoValue.Value) 
					return new LNodeList(LNode.Literal(_tokenValue, this));
				else
					return new LNodeList();
			}
		}
		
		public override int Max { get { return 0; } }

		public override LNode WithName(Symbol name) { var copy = cov_Clone(); copy._name = name; return copy; }

		public override LNode Target
		{
			get { return new StdIdNode(_name, this); }
		}
		public override CallNode WithArgs(LNodeList args)
		{
			return LNode.Call(_name, args, this);
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdTriviaNode cov_Clone() { return new StdTriviaNode(_name, _tokenValue, this); }

		public override LNode WithAttrs(LNodeList attrs)
		{
			return LNode.Call(attrs, _name, Args, this);
		}

		public override bool HasSimpleHead()                     { return true; }
		public override bool HasSimpleHeadWithoutPAttrs()        { return true; }
	}
}
