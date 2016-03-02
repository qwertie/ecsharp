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
using Loyc.Math;

namespace Loyc.Syntax
{
	internal class StdIdNode : IdNode
	{
		public StdIdNode(Symbol name, LNode ras) : base(ras)
		{
			if ((_name = name) == null)
				throw new ArgumentException("Cannot set IdNode.Name to null.");
		}
		public StdIdNode(Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) : base(range, style) 
		{
			if ((_name = name) == null)
				throw new ArgumentException("Cannot set IdNode.Name to null.");
		}
		
		protected Symbol _name;
		public override Symbol Name { get { return _name; } }
		public override LNode WithName(Symbol name) { var copy = cov_Clone(); copy._name = name; return copy; }

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdIdNode cov_Clone() { return new StdIdNode(_name, this); }

		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdIdNodeWithAttrs(attrs, _name, this);
		}
	}
	internal class StdIdNodeWithAttrs : StdIdNode
	{
		VList<LNode> _attrs;
		public StdIdNodeWithAttrs(VList<LNode> attrs, Symbol name, LNode ras)
			: base(name, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdIdNodeWithAttrs(VList<LNode> attrs, Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(name, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		
		public override StdIdNode cov_Clone() { return new StdIdNodeWithAttrs(_attrs, _name, this); }

		public override VList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdIdNode(_name, this);
			return new StdIdNodeWithAttrs(attrs, _name, this);
		}
	}

	/// <summary>A node that has both a Name and a Value. </summary>
	/// <remarks>
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

		public override VList<LNode> Args
		{
			get { 
				if (_tokenValue != NoValue.Value) 
					return new VList<LNode>(LNode.Literal(_tokenValue, this));
				else
					return new VList<LNode>();
			}
		}
		
		public override int Max { get { return 0; } }

		public override LNode WithName(Symbol name) { var copy = cov_Clone(); copy._name = name; return copy; }

		public override LNode Target
		{
			get { return new StdIdNode(_name, this); }
		}
		public override CallNode WithArgs(VList<LNode> args)
		{
			return LNode.Call(_name, args, this);
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdTriviaNode cov_Clone() { return new StdTriviaNode(_name, _tokenValue, this); }

		public override LNode WithAttrs(VList<LNode> attrs)
		{
			return LNode.Call(attrs, _name, Args, this);
		}

		public override bool HasSimpleHead()                     { return true; }
		public override bool HasSimpleHeadWithoutPAttrs()        { return true; }
	}

	internal class StdLiteralNode : LiteralNode
	{
		public StdLiteralNode(object value, LNode ras) 
			: base(ras) { _value = value; }
		public StdLiteralNode(object value, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(range, style) { _value = value; }

		public override Symbol Name { get { return GSymbol.Empty; } }
		
		protected object _value;
		public override object Value { get { return _value; } }
		public override LiteralNode WithValue(object value) { var copy = cov_Clone(); copy._value = value; return copy; }
		
		public override LNode Clone() { return cov_Clone(); }
		public virtual StdLiteralNode cov_Clone() { return new StdLiteralNode(_value, this); }

		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdLiteralNodeWithAttrs(attrs, _value, this);
		}
	}
	internal class StdLiteralNodeWithAttrs : StdLiteralNode
	{
		VList<LNode> _attrs;
		public StdLiteralNodeWithAttrs(VList<LNode> attrs, object value, LNode ras) 
			: base(value, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdLiteralNodeWithAttrs(VList<LNode> attrs, object value, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(value, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdLiteralNode cov_Clone() { return new StdLiteralNodeWithAttrs(_attrs, _value, this); }

		public override VList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdLiteralNode(_value, this);
			return new StdLiteralNodeWithAttrs(attrs, _value, this);
		}
	}


	internal abstract class StdCallNode : CallNode
	{
		public StdCallNode(VList<LNode> args, LNode ras)
			: base(ras) { _args = args; NoNulls(args, "Args"); }
		public StdCallNode(VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(range, style) { _args = args; NoNulls(args, "Args"); }

		protected VList<LNode> _args;
		public override VList<LNode> Args { get { return _args; } }
		
		public sealed override int Max { get { return _args.Count - 1; } }
	}

	internal class StdSimpleCallNode : StdCallNode
	{
		public StdSimpleCallNode(Symbol name, VList<LNode> args, LNode ras)
			: base(args, ras) { _name = name ?? GSymbol.Empty; DetectTargetRange(); }
		public StdSimpleCallNode(Symbol name, VList<LNode> args, StdSimpleCallNode ras)
			: base(args, ras) { _name = name ?? GSymbol.Empty; _targetOffs = ras._targetOffs; _targetLen = ras._targetLen; }
		public StdSimpleCallNode(Symbol name, VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(args, range, style) { _name = name ?? GSymbol.Empty; DetectTargetRange(); }
		public StdSimpleCallNode(Loyc.Syntax.Lexing.Token targetToken, VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(args, range, style)
		{
			_name = (Symbol)(targetToken.Value ?? GSymbol.Empty); 
			_targetOffs = ClipUShort(targetToken.StartIndex - RAS.StartIndex);
			_targetLen = ClipUShort(targetToken.Length);
		}
		
		protected Symbol _name;
		public override Symbol Name { get { return _name; } }
		public override LNode WithName(Symbol name) { var copy = cov_Clone(); copy._name = name; return copy; }

		// Offset and range of Target within its parent (yes, I'm a little obsessed 
		// with saving memory on rarely-used members, so they are ushort)
		public ushort _targetOffs, _targetLen;
		// TODO: the parser should be allowed to choose this range manually
		private void DetectTargetRange()
		{
			if (RAS.Length > 0) {
				int c = _args.Count;
				SourceRange r0, r1;
				_targetOffs = 0;
				if (c == 0) {
					// assume this node is a simple call: MethodName()
					_targetLen = (ushort)System.Math.Max(0, Range.Length - 2);
				} else if ((r0 = _args[0].Range).Source == RAS.Source && r0.StartIndex >= RAS.StartIndex) {
					int endIndex = RAS.EndIndex;
					if (RAS.StartIndex < r0.StartIndex || c > 1 && r0.EndIndex >= (r1 = _args[1].Range).StartIndex) {
						// assume this is a normal call, Target is at beginning
						_targetLen = ClipUShort(r0.StartIndex - RAS.StartIndex);
					} else {
						// assume this is an operator, e.g. for x + y, use _targetOffs=1, _targetLen=3
						_targetOffs = ClipUShort(r0.EndIndex);
						_targetLen = ClipUShort(endIndex - r0.EndIndex);
					}
				} else
					_targetLen = 0;
			}
		}
		ushort ClipUShort(int x) { return (ushort)MathEx.InRange(x, 0, ushort.MaxValue); }

		public override LNode Target
		{
			get { return new StdIdNode(_name, new SourceRange(Source, RAS.StartIndex + _targetOffs, _targetLen)); }
		}
		public override CallNode WithArgs(VList<LNode> args)
		{
		    var copy = cov_Clone();
		    copy._args = args;
		    return copy;
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdSimpleCallNode cov_Clone() { return new StdSimpleCallNode(_name, _args, this); }

		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdSimpleCallNodeWithAttrs(attrs, _name, _args, this);
		}

		public override bool HasSimpleHead()                     { return true; }
		public override bool HasSimpleHeadWithoutPAttrs()        { return true; }
	}
	internal class StdSimpleCallNodeWithAttrs : StdSimpleCallNode
	{
		protected VList<LNode> _attrs;
		public StdSimpleCallNodeWithAttrs(VList<LNode> attrs, Symbol name, VList<LNode> args, LNode ras) 
			: base(name, args, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdSimpleCallNodeWithAttrs(VList<LNode> attrs, Symbol name, VList<LNode> args, StdSimpleCallNode ras)
			: base(name, args, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdSimpleCallNodeWithAttrs(VList<LNode> attrs, Symbol name, VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(name, args, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdSimpleCallNodeWithAttrs(VList<LNode> attrs, Loyc.Syntax.Lexing.Token targetToken, VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(targetToken, args, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdSimpleCallNode cov_Clone() { return new StdSimpleCallNodeWithAttrs(_attrs, _name, _args, this); }

		public override VList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdSimpleCallNode(_name, _args, this);
			return new StdSimpleCallNodeWithAttrs(attrs, _name, _args, this);
		}
		
		// Hashcode computation can be costly for call nodes, so cache the result.
		// (Equality testing can be even more expensive when two trees are equal,
		// but I see no way to optimize that part)
		protected int _hashCode;
		public override int GetHashCode()
		{
			if (_hashCode == 0)
				return _hashCode = base.GetHashCode();
			return _hashCode;
		}
	}

	internal class StdComplexCallNode : StdCallNode
	{
		public StdComplexCallNode(LNode target, VList<LNode> args, LNode ras)
			: base(args, ras) { CheckParam.IsNotNull("target", target); _target = target; }
		public StdComplexCallNode(LNode target, VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(args, range, style) { CheckParam.IsNotNull("target", target); _target = target; }
		protected LNode _target;
		public override LNode Target { get { return _target; } }
		public override Symbol Name {
			get {
				var target = Target;
				if (target == null || !target.IsId)
					return GSymbol.Empty;
				return target.Name;
			}
		}

		public override CallNode WithArgs(VList<LNode> args)
		{
			var copy = cov_Clone();
			copy._args = args;
			return copy;
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdComplexCallNode cov_Clone() { return new StdComplexCallNode(_target, _args, this); }

		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdComplexCallNodeWithAttrs(attrs, _target, _args, this);
		}
	}
	internal class StdComplexCallNodeWithAttrs : StdComplexCallNode
	{
		protected VList<LNode> _attrs;
		public StdComplexCallNodeWithAttrs(VList<LNode> attrs, LNode target, VList<LNode> args, LNode ras)
			: base(target, args, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdComplexCallNodeWithAttrs(VList<LNode> attrs, LNode target, VList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(target, args, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdComplexCallNode cov_Clone() { return new StdComplexCallNodeWithAttrs(_attrs, _target, _args, this); }

		public override VList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(VList<LNode> attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdComplexCallNode(_target, _args, this);
			return new StdComplexCallNodeWithAttrs(attrs, _target, _args, this);
		}
	}
}
