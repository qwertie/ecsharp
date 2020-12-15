using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdIdNodeWithAttrs(attrs, _name, this);
		}
	}
	internal class StdIdNodeWithAttrs : StdIdNode
	{
		LNodeList _attrs;
		public StdIdNodeWithAttrs(LNodeList attrs, Symbol name, LNode ras)
			: base(name, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdIdNodeWithAttrs(LNodeList attrs, Symbol name, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(name, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdIdNode cov_Clone() { return new StdIdNodeWithAttrs(_attrs, _name, this); }

		public override LNodeList Attrs { get { return _attrs; } }
		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdIdNode(_name, this);
			return new StdIdNodeWithAttrs(attrs, _name, this);
		}
		public override LNode WithAttrs(Func<LNode, Maybe<LNode>> selector)
		{
			var newAttrs = Attrs.WhereSelect(selector);
			if (newAttrs == Attrs)
				return this;
			return WithAttrs(newAttrs);
		}
		public override LNode WithAttrs(Func<LNode, IReadOnlyList<LNode>> selector)
		{
			var newAttrs = Attrs.SmartSelectMany(selector);
			if (newAttrs == Attrs)
				return this;
			return WithAttrs(newAttrs);
		}
	}
}
