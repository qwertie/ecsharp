using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.CompilerCore;
using Loyc.Utilities;

namespace Loyc.Syntax
{
	public class StdSymbolNode : SymbolNode, ISymbolNode
	{
		public StdSymbolNode(Symbol name, LNode ras) : base(ras)
		{
			if ((_name = name) == null)
				throw new ArgumentException("Cannot set SymbolNode.Name to null.");
		}
		public StdSymbolNode(Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) : base(range, style) 
		{
			if ((_name = name) == null)
				throw new ArgumentException("Cannot set SymbolNode.Name to null.");
		}
		
		protected Symbol _name;
		public override Symbol Name { get { return _name; } }
		public override LNode WithName(Symbol name) { var copy = cov_Clone(); copy._name = name; return copy; }

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdSymbolNode cov_Clone() { return new StdSymbolNode(_name, this); }

		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdSymbolNodeWithAttrs(attrs, _name, this);
		}
	}
	public class StdSymbolNodeWithAttrs : StdSymbolNode
	{
		RVList<LNode> _attrs;
		public StdSymbolNodeWithAttrs(RVList<LNode> attrs, Symbol name, LNode ras) 
			: base(name, ras) { _attrs = attrs; }
		public StdSymbolNodeWithAttrs(RVList<LNode> attrs, Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(name, range, style) { _attrs = attrs; }
		
		public override StdSymbolNode cov_Clone() { return new StdSymbolNodeWithAttrs(_attrs, _name, this); }

		public override RVList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return new StdSymbolNode(_name, this);
			return new StdSymbolNodeWithAttrs(attrs, _name, this);
		}
	}


	public class StdLiteralNode : LiteralNode, ILiteralNode
	{
		public StdLiteralNode(object value, LNode ras) 
			: base(ras) { _value = value; }
		public StdLiteralNode(object value, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(range, style) { _value = value; }

		public override Symbol Name { get { return GSymbol.Empty; } }
		public override LNode WithName(Symbol name) { throw new InvalidOperationException("WithName(): This node is a literal; its Name cannot be changed."); }
		
		protected object _value;
		public override object Value { get { return _value; } }
		public override LiteralNode WithValue(object value) { var copy = cov_Clone(); copy._value = value; return copy; }
		
		public override LNode Clone() { return cov_Clone(); }
		public virtual StdLiteralNode cov_Clone() { return new StdLiteralNode(_value, this); }

		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdLiteralNodeWithAttrs(attrs, _value, this);
		}
	}
	public class StdLiteralNodeWithAttrs : StdLiteralNode
	{
		RVList<LNode> _attrs;
		public StdLiteralNodeWithAttrs(RVList<LNode> attrs, object value, LNode ras) 
			: base(value, ras) { _attrs = attrs; }
		public StdLiteralNodeWithAttrs(RVList<LNode> attrs, object value, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(value, range, style) { _attrs = attrs; }

		public override StdLiteralNode cov_Clone() { return new StdLiteralNodeWithAttrs(_attrs, _value, this); }

		public override RVList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return new StdLiteralNode(_value, this);
			return new StdLiteralNodeWithAttrs(attrs, _value, this);
		}
	}


	public abstract class StdCallNode : CallNode, ICallNode
	{
		public StdCallNode(RVList<LNode> args, LNode ras)
			: base(ras) { _args = args; }
		public StdCallNode(RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(range, style) { _args = args; }

		protected RVList<LNode> _args;
		public override RVList<LNode> Args { get { return _args; } }

		public override CallNode With(LNode target, RVList<LNode> args)
		{
			var attrs = Attrs;
			if (attrs.Count == 0)
				return new StdComplexCallNode(target, args, this);
			else
				return new StdComplexCallNodeWithAttrs(attrs, target, args, this);
		}
	}

	public class StdSimpleCallNode : StdCallNode
	{
		public StdSimpleCallNode(Symbol name, RVList<LNode> args, LNode ras) 
			: base(args, ras) { _name = name; }
		public StdSimpleCallNode(Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(args, range, style) { _name = name; }
		
		protected Symbol _name;
		public override Symbol Name { get { return _name ?? GSymbol.Empty; } }
		public override LNode WithName(Symbol name) { var copy = cov_Clone(); copy._name = name; return copy; }

		public override LNode Target
		{
			get { return _name == null ? null : new StdSymbolNode(_name, this); }
		}
		public override CallNode With(Symbol target, RVList<LNode> args)
		{
			var copy = cov_Clone();
			copy._args = args;
			copy._name = target;
			return copy;
		}
		public override CallNode WithArgs(RVList<LNode> args)
		{
			var copy = cov_Clone();
			copy._args = args;
			return copy;
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdSimpleCallNode cov_Clone() { return new StdSimpleCallNode(_name, _args, this); }

		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdSimpleCallNodeWithAttrs(attrs, _name, _args, this);
		}
	}
	public class StdSimpleCallNodeWithAttrs : StdSimpleCallNode
	{
		protected RVList<LNode> _attrs;
		public StdSimpleCallNodeWithAttrs(RVList<LNode> attrs, Symbol name, RVList<LNode> args, LNode ras) 
			: base(name, args, ras) { _attrs = attrs; }
		public StdSimpleCallNodeWithAttrs(RVList<LNode> attrs, Symbol name, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(name, args, range, style) { _attrs = attrs; }

		public override StdSimpleCallNode cov_Clone() { return new StdSimpleCallNodeWithAttrs(_attrs, _name, _args, this); }

		public override RVList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return new StdSimpleCallNode(_name, _args, this);
			return new StdSimpleCallNodeWithAttrs(attrs, _name, _args, this);
		}
	}

	public class StdComplexCallNode : StdCallNode
	{
		public StdComplexCallNode(LNode target, RVList<LNode> args, LNode ras)
			: base(args, ras) { _target = target; }
		public StdComplexCallNode(LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(args, range, style) { _target = target; }
		protected LNode _target;
		public override LNode Target { get { return _target; } }
		public override Symbol Name {
			get {
				var target = Target;
				if (target == null || !target.IsSymbol)
					return GSymbol.Empty;
				return target.Name;
			}
		}

		public override CallNode With(Symbol target, RVList<LNode> args)
		{
			var attrs = Attrs;
			if (attrs.Count == 0)
				return new StdSimpleCallNode(target, args, this);
			else
				return new StdSimpleCallNodeWithAttrs(attrs, target, args, this);
		}
		public override CallNode WithArgs(RVList<LNode> args)
		{
			var copy = cov_Clone();
			copy._args = args;
			return copy;
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdComplexCallNode cov_Clone() { return new StdComplexCallNode(_target, _args, this); }

		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdComplexCallNodeWithAttrs(attrs, _target, _args, this);
		}
	}
	public class StdComplexCallNodeWithAttrs : StdComplexCallNode
	{
		protected RVList<LNode> _attrs;
		public StdComplexCallNodeWithAttrs(RVList<LNode> attrs, LNode target, RVList<LNode> args, LNode ras)
			: base(target, args, ras) { _attrs = attrs; }
		public StdComplexCallNodeWithAttrs(RVList<LNode> attrs, LNode target, RVList<LNode> args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(target, args, range, style) { _attrs = attrs; }

		public override StdComplexCallNode cov_Clone() { return new StdComplexCallNodeWithAttrs(_attrs, _target, _args, this); }

		public override RVList<LNode> Attrs { get { return _attrs; } }
		public override LNode WithAttrs(RVList<LNode> attrs)
		{
			if (attrs.Count == 0) return new StdComplexCallNode(_target, _args, this);
			return new StdComplexCallNodeWithAttrs(attrs, _target, _args, this);
		}
	}
}
