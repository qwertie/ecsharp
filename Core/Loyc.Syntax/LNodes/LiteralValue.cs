namespace Loyc.Syntax
{
	/// <summary>Bundles the optional original text of a value with an optional in-memory 
	/// form of it; see remarks at <see cref="ILiteralValue"/>.
	/// This struct can also be turned into an LNode by calling 
	/// <see cref="LNode.Literal{LiteralValue}(SourceRange, LiteralValue, NodeStyle)"/>.
	/// </summary>
	public struct LiteralValue : ILiteralValue, ILiteralValueProvider
	{
		public LiteralValue(object value, UString textValue, Symbol typeMarker)
		{
			Value = value;
			TextValue = textValue;
			TypeMarker = typeMarker;
		}
		public object Value { get; }
		public UString TextValue { get; }
		public Symbol TypeMarker { get; }

		UString ILiteralValueProvider.GetTextValue(SourceRange range) => TextValue;
		Symbol ILiteralValueProvider.GetTypeMarker(SourceRange range) => TypeMarker;
		object ILiteralValueProvider.GetValue(SourceRange range) => Value;
	}
}
