using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	internal abstract class StdCallNode : CallNode
	{
		public StdCallNode(LNodeList args, LNode ras)
			: base(ras) { _args = args; NoNulls(args, "Args"); }
		public StdCallNode(LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(range, style) { _args = args; NoNulls(args, "Args"); }

		protected LNodeList _args;
		public override LNodeList Args { get { return _args; } }

		public sealed override int Max { get { return _args.Count - 1; } }
	}

	internal class StdSimpleCallNode : StdCallNode
	{
		public StdSimpleCallNode(Symbol name, LNodeList args, LNode ras)
			: base(args, ras) { _name = name ?? GSymbol.Empty; DetectTargetRange(); }
		public StdSimpleCallNode(Symbol name, LNodeList args, StdSimpleCallNode ras)
			: base(args, ras) { _name = name ?? GSymbol.Empty; _targetOffs = ras._targetOffs; _targetLen = ras._targetLen; }
		public StdSimpleCallNode(Symbol name, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(args, range, style) { _name = name ?? GSymbol.Empty; DetectTargetRange(); }
		public StdSimpleCallNode(Symbol name, LNodeList args, SourceRange range, int targetStart, int targetEnd, NodeStyle style = NodeStyle.Default)
			: base(args, range, style)
		{
			_name = name ?? GSymbol.Empty;
			_targetOffs = ClipUShort(targetStart - RAS.StartIndex);
			_targetLen = ClipUShort(targetEnd - targetStart);
		}
		public StdSimpleCallNode(Loyc.Syntax.Lexing.Token targetToken, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
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
		// Guess range of Target if range was not provided to the constructor
		private void DetectTargetRange()
		{
			if (RAS.Length > 0)
			{
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
						int endTarget = endIndex;
						if (c > 1 && (r1 = _args[1].Range).StartIndex > r0.EndIndex)
							endTarget = r1.StartIndex;
						_targetLen = ClipUShort(endTarget - r0.EndIndex);
					}
				} else
					_targetLen = 0;
			}
		}

		static ushort ClipUShort(int x) { return (ushort)(x < 0 ? 0 : x < ushort.MaxValue ? x : ushort.MaxValue); }

		public override LNode Target
		{
			get { return new StdIdNode(_name, new SourceRange(Source, RAS.StartIndex + _targetOffs, _targetLen)); }
		}
		public override CallNode WithArgs(LNodeList args)
		{
			if (args == _args)
				return this;
			var copy = cov_Clone();
			copy._args = args;
			NoNulls(args, nameof(Args));
			return copy;
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdSimpleCallNode cov_Clone() { return new StdSimpleCallNode(_name, _args, this); }
		public override LNode WithRange(int startIndex, int endIndex)
		{
			// Bug fix 2016-10: changing the Range affected Target.Range because
			// _targetOffs is relative to RAS. Avoid that. TODO: unit tests for this.
			int targetStart = RAS.StartIndex + _targetOffs;
			int newTargetStart = targetStart - startIndex;
			if (newTargetStart != (ushort)newTargetStart)
			{
				// Switch to StdComplexCallNode because new value of _targetOffs won't fit in ushort
				return new StdComplexCallNode(Target, Args, new SourceRange(RAS.Source, startIndex, endIndex - startIndex), RAS.Style);
			}
			else
			{
				var copy = cov_Clone();
				copy.RAS = new RangeAndStyle(RAS.Source, startIndex, endIndex - startIndex, RAS.Style);
				copy._targetOffs = (ushort)newTargetStart;
				return copy;
			}
		}

		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdSimpleCallNodeWithAttrs(attrs, _name, _args, this);
		}

		public override bool HasSimpleHead() { return true; }
		public override bool HasSimpleHeadWithoutPAttrs() { return true; }
	}

	internal class StdSimpleCallNodeWithAttrs : StdSimpleCallNode
	{
		protected LNodeList _attrs;
		public StdSimpleCallNodeWithAttrs(LNodeList attrs, Symbol name, LNodeList args, LNode ras)
			: base(name, args, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdSimpleCallNodeWithAttrs(LNodeList attrs, Symbol name, LNodeList args, StdSimpleCallNode ras)
			: base(name, args, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdSimpleCallNodeWithAttrs(LNodeList attrs, Symbol name, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(name, args, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdSimpleCallNodeWithAttrs(LNodeList attrs, Loyc.Syntax.Lexing.Token targetToken, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(targetToken, args, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdSimpleCallNode cov_Clone() { return new StdSimpleCallNodeWithAttrs(_attrs, _name, _args, this); }

		public override LNodeList Attrs { get { return _attrs; } }
		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdSimpleCallNode(_name, _args, this);
			return new StdSimpleCallNodeWithAttrs(attrs, _name, _args, this);
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

	internal class StdComplexCallNode : StdCallNode
	{
		public StdComplexCallNode(LNode target, LNodeList args, LNode ras)
			: base(args, ras) { CheckParam.IsNotNull("target", target); _target = target; }
		public StdComplexCallNode(LNode target, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(args, range, style) { CheckParam.IsNotNull("target", target); _target = target; }
		protected LNode _target;
		public override LNode Target { get { return _target; } }
		public override Symbol Name
		{
			get {
				var target = Target;
				Debug.Assert(target != null);
				if (target == null || !target.IsId)
					return GSymbol.Empty;
				return target.Name;
			}
		}

		public override CallNode WithArgs(LNodeList args)
		{
			if (args == _args)
				return this;
			var copy = cov_Clone();
			copy._args = args;
			NoNulls(args, nameof(Args));
			return copy;
		}

		public override LNode Clone() { return cov_Clone(); }
		public virtual StdComplexCallNode cov_Clone() { return new StdComplexCallNode(_target, _args, this); }

		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdComplexCallNodeWithAttrs(attrs, _target, _args, this);
		}
	}

	internal class StdComplexCallNodeWithAttrs : StdComplexCallNode
	{
		protected LNodeList _attrs;
		public StdComplexCallNodeWithAttrs(LNodeList attrs, LNode target, LNodeList args, LNode ras)
			: base(target, args, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdComplexCallNodeWithAttrs(LNodeList attrs, LNode target, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(target, args, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdComplexCallNode cov_Clone() { return new StdComplexCallNodeWithAttrs(_attrs, _target, _args, this); }

		public override LNodeList Attrs { get { return _attrs; } }
		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdComplexCallNode(_target, _args, this);
			return new StdComplexCallNodeWithAttrs(attrs, _target, _args, this);
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
