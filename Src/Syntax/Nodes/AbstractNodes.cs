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

		public sealed override NodeKind Kind { get { return NodeKind.Id; } }
		public abstract override Symbol Name { get; }
		public abstract override LNode WithName(Symbol name);
		
		[EditorBrowsable(EditorBrowsableState.Never)] public override object Value { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LiteralNode WithValue(object value)           { throw new InvalidOperationException("WithValue(): this is an IdNode, cannot change Value."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode Target { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override RVList<LNode> Args { get { return RVList<LNode>.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(LNode target, RVList<LNode> args)  { throw new InvalidOperationException("With(): this is an IdNode, cannot use With(target, args)."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(Symbol target, RVList<LNode> args) { throw new InvalidOperationException("With(): this is an IdNode, cannot use With(target, args)."); }
		public override CallNode WithArgs(RVList<LNode> args) { return new StdComplexCallNode(this, args, Range); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }

		public abstract override LNode Clone();
		public abstract override LNode WithAttrs(RVList<LNode> attrs);
		public override bool Equals(LNode b, bool compareStyles)
		{
			var kind = Kind;
			if (kind != b.Kind)
				return false;
			if (compareStyles && Style != b.Style)
				return false;
			Debug.Assert(ArgCount == 0 && b.ArgCount == 0);
			return Name == b.Name;
		}
		protected internal override int GetHashCode(int recurse, int styleMask)
		{
			int hash = Name.GetHashCode();
			hash += AttrCount;
			return hash += (int)Style & styleMask;
		}

		public override bool IsIdWithoutPAttrs()            { return !HasPAttrs(); }
		public override bool IsIdWithoutPAttrs(Symbol name) { return Name == name && !HasPAttrs(); }
		public override bool IsIdNamed(Symbol name)             { return Name == name; }
	}
	
	/// <summary>Base class of all nodes that represent literal values such as 123 and "foo".</summary>
	public abstract class LiteralNode : LNode
	{
		protected LiteralNode(LNode ras) : base(ras) { }
		protected LiteralNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override NodeKind Kind { get { return NodeKind.Literal; } }
		public abstract override object Value { get; }
		public abstract override LiteralNode WithValue(object value);

		[EditorBrowsable(EditorBrowsableState.Never)] public override Symbol Name { get { return GSymbol.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode WithName(Symbol name)                   { throw new InvalidOperationException("WidthName(): this is a LiteralNode, cannot change Name."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LNode Target { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override RVList<LNode> Args { get { return RVList<LNode>.Empty; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(LNode target, RVList<LNode> args)  { throw new InvalidOperationException("With(): this is a LiteralNode, cannot use With(target, args)."); }
		[EditorBrowsable(EditorBrowsableState.Never)] public override CallNode With(Symbol target, RVList<LNode> args) { throw new InvalidOperationException("With(): this is a LiteralNode, cannot use With(target, args)."); }
		public override CallNode WithArgs(RVList<LNode> args) { return new StdComplexCallNode(this, args, Range); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }

		public abstract override LNode Clone();
		public abstract override LNode WithAttrs(RVList<LNode> attrs);
		public override bool Equals(LNode b, bool compareStyles)
		{
			var kind = Kind;
			if (kind != b.Kind)
				return false;
			if (compareStyles && Style != b.Style)
				return false;
			Debug.Assert(ArgCount == 0 && b.ArgCount == 0);
			return object.Equals(Value, b.Value);
		}
		protected internal override int GetHashCode(int recurse, int styleMask)
		{
			int hash = (Value ?? "").GetHashCode() + 1;
			hash += AttrCount;
			return hash += (int)Style & styleMask;
		}
	}
	
	/// <summary>Base class of all nodes that represent calls such as <c>f(x)</c>, 
	/// operator calls such as <c>x + y</c>, braced blocks, and all other things 
	/// that are not simple symbols and literals.</summary>
	public abstract class CallNode : LNode
	{
		protected CallNode(LNode ras) : base(ras) { }
		protected CallNode(SourceRange range, NodeStyle style) : base(range, style) { }

		public sealed override NodeKind Kind { get { return NodeKind.Call; } }
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
		[EditorBrowsable(EditorBrowsableState.Never)] public override object Value { get { return null; } }
		[EditorBrowsable(EditorBrowsableState.Never)] public override LiteralNode WithValue(object value) { throw new InvalidOperationException("WithValue(): this is a CallNode, cannot change Value."); }
		public abstract override LNode Target { get; }
		public abstract override RVList<LNode> Args { get; }
		public abstract override CallNode With(LNode target, RVList<LNode> args);
		public abstract override CallNode With(Symbol target, RVList<LNode> args);
		public override CallNode WithArgs(RVList<LNode> args) { return With(Target, args); }

		public sealed override void Call(LNodeVisitor visitor)  { visitor.Visit(this); }
		public sealed override void Call(ILNodeVisitor visitor) { visitor.Visit(this); }

		public abstract override LNode Clone();
		public abstract override LNode WithAttrs(RVList<LNode> attrs);
		public override bool Equals(LNode b, bool compareStyles)
		{
			var kind = Kind;
			if (kind != b.Kind)
				return false;
			if (compareStyles && Style != b.Style)
				return false;
			if (!Equals(Args, b.Args) ||
				!Equals(Attrs, b.Attrs))
				return false;
			return Equals(Target, b.Target, compareStyles);
		}
		protected internal override int GetHashCode(int recurse, int styleMask)
		{
			RVList<LNode> args = Args, attrs = Attrs;
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
			return hash += (int)Style & styleMask;
		}

		public override bool Calls(Symbol name, int argCount)    { return Name == name && ArgCount == argCount; }
		public override bool Calls(Symbol name)                  { return Name == name; }
		public override bool CallsMin(Symbol name, int argCount) { return Name == name && ArgCount >= argCount; }
		public override bool IsParenthesizedExpr           { get { return ArgCount == 1 && Target.IsIdNamed(GSymbol.Empty); } }
		public override bool HasSimpleHead()                     { var t = Target; return !t.IsCall && !t.HasAttrs; }
		public override bool HasSimpleHeadWithoutPAttrs()        { var t = Target; return !t.IsCall && !t.HasPAttrs(); }
		public override LNode WithArgs(Func<LNode, LNode> selector) { return WithArgs(Args.SmartSelect(selector)); }
		public override LNode Unparenthesized()                  { return IsParenthesizedExpr ? Args[0] : this; }
	}
}
