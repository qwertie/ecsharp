namespace Loyc.Syntax
{
	/// <summary>This structure is typically created implicitly by
	/// <see cref="LNodeFactory.Literal(Token, ILiteralParser)"/>. It holds the value 
	/// of a literal while being able to retrieve the unescaped text of that literal
	/// from the source code.</summary>
	/// <remarks>This type is a bit smaller than <see cref="LiteralValue"/> because
	/// it doesn't hold a reference to the source text; instead it shares the reference 
	/// to the source file that is already stored in the <see cref="LNode"/> in which 
	/// it is embedded.</remarks>
	internal struct LiteralFromParser : ILiteralValueProvider
	{
		public LiteralFromParser(object value, int startIndex, int length, Symbol typeMarker)
		{
			_value = value;
			_startIndex = startIndex;
			_length = length;
			_typeMarker = typeMarker;
		}
		/// <summary>This constructor is meant for the case where the Value should
		/// simply be the boxed return value from <see cref="GetTextValue"/> (i.e. 
		/// <see cref="LNode.TextValue"/>). To save memory, the Value (of type
		/// <see cref="UString"/>) is not generated until it is requested.</summary>
		public LiteralFromParser(int startIndex, int length, Symbol typeMarker)
		{
			_value = NoValue.Value;
			_startIndex = startIndex;
			_length = length;
			_typeMarker = typeMarker;
		}
		object _value;
		int _startIndex, _length;
		Symbol _typeMarker;

		// ILiteralValueProvider implementation:

		public UString GetTextValue(SourceRange range) =>
			_startIndex < 0 ? default(UString) : range.Source.Text.Slice(_startIndex, _length);
		public Symbol GetTypeMarker(SourceRange range) => _typeMarker;
		public object GetValue(SourceRange range) =>
			_value == NoValue.Value ? GetTextValue(range) : _value;
	}
}
