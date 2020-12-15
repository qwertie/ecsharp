namespace Loyc.Syntax
{
	/// <summary>A simple implementation of <see cref="IUninterpretedLiteral"/> which
	/// can also be turned into an LNode by calling 
	/// <see cref="LNode.Literal{UninterpretedLiteral}(SourceRange, UninterpretedLiteral, NodeStyle)"/>.
	/// </summary>
	public struct UninterpretedLiteral : IUninterpretedLiteral, ILiteralValueProvider
	{
		public UninterpretedLiteral(UString textValue, Symbol typeMarker)
		{
			TextValue = textValue;
			TypeMarker = typeMarker;
		}
		public UString TextValue { get; }
		public Symbol TypeMarker { get; }

		UString ILiteralValueProvider.GetTextValue(SourceRange range) => TextValue;
		Symbol ILiteralValueProvider.GetTypeMarker(SourceRange range) => TypeMarker;
		object ILiteralValueProvider.GetValue(SourceRange range) => TextValue;
	}
}
