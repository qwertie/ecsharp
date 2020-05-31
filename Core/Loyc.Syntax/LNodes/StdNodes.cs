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
	}

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

	internal struct SimpleValue<V> : ILiteralValueProvider
	{
		V _value;
		public SimpleValue(V value) => _value = value;
		public UString GetTextValue(SourceRange range) => default(UString);
		public Symbol GetTypeMarker(SourceRange range) => null;
		public object GetValue(SourceRange range) => _value;
	}
	
	internal class StdLiteralNode<TValue> : LiteralNode where TValue : ILiteralValueProvider
	{
		public StdLiteralNode(TValue value, LNode ras) 
			: base(ras) { _value = value;  }
		public StdLiteralNode(TValue value, SourceRange range, NodeStyle style = NodeStyle.Default) 
			: base(range, style) { _value = value; }

		public override Symbol Name { get { return GSymbol.Empty; } }
		
		protected TValue _value;
		public override object Value => _value.GetValue(Range);
		public override UString TextValue => _value.GetTextValue(Range);
		public override Symbol TypeMarker => _value.GetTypeMarker(Range);

		public override LiteralNode WithValue(object value) => cov_Clone(new SimpleValue<object>(value));
		
		public override LNode Clone() => cov_Clone(_value);
		public virtual StdLiteralNode<Value2> cov_Clone<Value2>(Value2 value) where Value2 : ILiteralValueProvider => new StdLiteralNode<Value2>(value, this);

		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs.Count == 0) return this;
			return new StdLiteralNodeWithAttrs<TValue>(attrs, _value, this);
		}
	}

	internal class StdLiteralNodeWithAttrs<Value> : StdLiteralNode<Value> where Value : ILiteralValueProvider
	{
		LNodeList _attrs;
		public StdLiteralNodeWithAttrs(LNodeList attrs, Value value, LNode ras)
			: base(value, ras) { _attrs = attrs; NoNulls(attrs, "Attrs"); }
		public StdLiteralNodeWithAttrs(LNodeList attrs, Value value, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(value, range, style) { _attrs = attrs; NoNulls(attrs, "Attrs"); }

		public override StdLiteralNode<Value2> cov_Clone<Value2>(Value2 value) => new StdLiteralNodeWithAttrs<Value2>(_attrs, value, this);

		public override LNodeList Attrs { get { return _attrs; } }
		public override LNode WithAttrs(LNodeList attrs)
		{
			if (attrs == Attrs) return this;
			if (attrs.Count == 0) return new StdLiteralNode<Value>(_value, this);
			return new StdLiteralNodeWithAttrs<Value>(attrs, _value, this);
		}
		public override LNode WithAttrs(Func<LNode, Maybe<LNode>> selector)
		{
			var newAttrs = Attrs.WhereSelect(selector);
			if (newAttrs == Attrs)
				return this;
			return WithAttrs(newAttrs);
		}
	}


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
			if (newTargetStart != (ushort)newTargetStart) {
				// Switch to StdComplexCallNode because new value of _targetOffs won't fit in ushort
				return new StdComplexCallNode(Target, Args, new SourceRange(RAS.Source, startIndex, endIndex - startIndex), RAS.Style);
			} else {
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

		public override bool HasSimpleHead()                     { return true; }
		public override bool HasSimpleHeadWithoutPAttrs()        { return true; }
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
	}

	internal class StdComplexCallNode : StdCallNode
	{
		public StdComplexCallNode(LNode target, LNodeList args, LNode ras)
			: base(args, ras) { CheckParam.IsNotNull("target", target); _target = target; }
		public StdComplexCallNode(LNode target, LNodeList args, SourceRange range, NodeStyle style = NodeStyle.Default)
			: base(args, range, style) { CheckParam.IsNotNull("target", target); _target = target; }
		protected LNode _target;
		public override LNode Target { get { return _target; } }
		public override Symbol Name {
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
	}
}
