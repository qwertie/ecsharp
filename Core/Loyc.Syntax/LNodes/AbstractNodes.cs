// 
// The three kinds of Loyc nodes: IdNode, LiteralNode and CallNode
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Loyc.Collections;
using System.Diagnostics;

namespace Loyc.Syntax
{
	/// <summary>Base class of all nodes that represent simple identifiers (including special symbols such as #foo).</summary>
	public abstract class IdNode : LNode
	{
		protected IdNode(LNode ras) : base(ras) { }
		protected IdNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override LNodeKind Kind { get { return LNodeKind.Id; } }
		public abstract override Symbol Name { get; }
		public abstract override LNode WithName(Symbol name);
		
		[EditorBrowsable(EditorBrowsableState.Never)] public override object Value { get { return NoValue.Value; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LiteralNode WithValue(object value) { throw new InvalidOperationException("WithValue(): this is an IdNode, cannot change Value."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode Target { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override VList<LNode> Args { get { return VList<LNode>.Empty; } }
		public override CallNode WithArgs(VList<LNode> args) { return new StdComplexCallNode(this, args, Range); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }

		public abstract override LNode Clone();
		public abstract override LNode WithAttrs(VList<LNode> attrs);
		protected internal override int GetHashCode(int recurse, int styleMask)
		{
			int hash = Name.GetHashCode();
			hash += AttrCount;
			return hash += (int)Style & styleMask;
		}

		public override bool IsIdWithoutPAttrs()            { return !HasPAttrs(); }
		public override bool IsIdWithoutPAttrs(Symbol name) { return Name == name && !HasPAttrs(); }
		public override bool IsIdNamed(Symbol name)         { return Name == name; }
		public override bool IsIdNamed(string name)         { return Name.Name == name; }
		
		public sealed override int Max { get { return -2; } }
	}
	
	/// <summary>Base class of all nodes that represent literal values such as 123 and "foo".</summary>
	public abstract class LiteralNode : LNode
	{
		protected LiteralNode(LNode ras) : base(ras) { }
		protected LiteralNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override LNodeKind Kind { get { return LNodeKind.Literal; } }
		public abstract override object Value { get; }
		public abstract override LiteralNode WithValue(object value);

		[EditorBrowsable(EditorBrowsableState.Never)] public override Symbol Name { get { return GSymbol.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode Target { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override VList<LNode> Args { get { return VList<LNode>.Empty; } }
		public override CallNode WithArgs(VList<LNode> args) { return new StdComplexCallNode(this, args, Range); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }

		public abstract override LNode Clone();
		public abstract override LNode WithAttrs(VList<LNode> attrs);
		protected internal override int GetHashCode(int recurse, int styleMask)
		{
			int hash = (Value ?? "").GetHashCode() + 1;
			hash += AttrCount;
			return hash += (int)Style & styleMask;
		}
		
		public sealed override int Max { get { return -2; } }
	}
	
	/// <summary>Base class of all nodes that represent calls such as <c>f(x)</c>, 
	/// operator calls such as <c>x + y</c>, braced blocks, and all other things 
	/// that are not simple symbols and literals.</summary>
	public abstract class CallNode : LNode
	{
		protected CallNode(LNode ras) : base(ras) { }
		protected CallNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override LNodeKind Kind { get { return LNodeKind.Call; } }
		public override Symbol Name {
			get {
				var target = Target;
				if (target == null || !target.IsId)
					return GSymbol.Empty;
				return target.Name;
			}
		}
		public override LNode WithName(Symbol name)
		{
			return WithTarget(Target.WithName(name));
		}
		[EditorBrowsable(EditorBrowsableState.Never)] public override object Value { get { return NoValue.Value; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LiteralNode WithValue(object value) { throw new InvalidOperationException("WithValue(): this is a CallNode, cannot change Value."); }
		public abstract override LNode Target { get; }
		public abstract override VList<LNode> Args { get; }
		public override CallNode WithArgs(VList<LNode> args) { return With(Target, args); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }

		public abstract override LNode Clone();
		public abstract override LNode WithAttrs(VList<LNode> attrs);

		// Hashcode computation can be costly for call nodes, so cache the result.
		// (Equality testing can be even more expensive when two trees are equal,
		// but I see no way to optimize that part)
		protected int _hashCode = -1;
		protected internal override int GetHashCode(int recurse, int styleMask)
		{
			if (_hashCode != -1)
				return _hashCode;

			VList<LNode> args = Args, attrs = Attrs;
			int hash = (args.Count << 3) + attrs.Count;
			if (recurse > 0) {
				var target = Target;
				if (target != null)
					hash ^= target.GetHashCode(recurse - 1, styleMask);
				for (int i = 0, c = System.Math.Min(attrs.Count, recurse << 2); i < c; i++)
					hash = (hash * 4129) + attrs[i].GetHashCode(recurse - 1, styleMask);
				for (int i = 0, c = System.Math.Min(args.Count, recurse << 2); i < c; i++)
					hash = (hash * 1013) + args[i].GetHashCode(recurse - 1, styleMask);
			}
			return _hashCode = (hash += (int)Style & styleMask);
		}

		public override bool Calls(Symbol name, int argCount)    { return Name == name && ArgCount == argCount; }
		public override bool Calls(string name, int argCount)    { return Name.Name == name && ArgCount == argCount; }
		public override bool Calls(Symbol name)                  { return Name == name; }
		public override bool Calls(string name)                  { return Name.Name == name; }
		public override bool CallsMin(Symbol name, int argCount) { return Name == name && ArgCount >= argCount; }
		public override bool CallsMin(string name, int argCount) { return Name.Name == name && ArgCount >= argCount; }
		public override bool HasSimpleHead()                     { var t = Target; return !t.IsCall; }
		public override bool HasSimpleHeadWithoutPAttrs()        { var t = Target; return !t.IsCall && !t.HasPAttrs(); }

		public sealed override LNode WithArgs(Func<LNode, Maybe<LNode>> selector)
		{
			VList<LNode> args = Args, newArgs = args.WhereSelect(selector);
			if (args == newArgs)
				return this;
			return WithArgs(newArgs);
		}
		public sealed override LNode Select(Func<LNode, Maybe<LNode>> selector, ReplaceOpt options = ReplaceOpt.ProcessAttrs)
		{
			var node = (options & ReplaceOpt.ProcessAttrs) != 0 ? WithAttrs(selector) : this;
			LNode target = node.Target, newTarget = selector(node.Target).Or(EmptySplice);
			if (newTarget != null && newTarget != target)
				return node.With(newTarget, Args.WhereSelect(selector));
			else
				return node.WithArgs(selector);
		}
	}
}
