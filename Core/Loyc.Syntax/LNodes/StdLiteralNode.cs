using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
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
			: base(ras) { _value = value; }
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
		public override LNode WithAttrs(Func<LNode, IReadOnlyList<LNode>> selector)
		{
			var newAttrs = Attrs.SmartSelectMany(selector);
			if (newAttrs == Attrs)
				return this;
			return WithAttrs(newAttrs);
		}
	}
}
