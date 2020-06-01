using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Loyc.Syntax
{
	/// <summary>A simple implementation of <see cref="ISerializedLiteral"/> that also 
	/// implements <see cref="ILiteralValueProvider"/> so that it can be used to construct
	/// literals (e.g. by calling <see cref="LNode.Literal{P}(SourceRange, P, NodeStyle)"/>).</summary>
	public struct SerializedLiteral : ISerializedLiteral, ILiteralValueProvider
	{
		public SerializedLiteral(UString textValue, Symbol typeMarker)
		{
			TextValue = textValue;
			TypeMarker = typeMarker;
		}

		public UString TextValue { get; }
		public Symbol TypeMarker { get; }

		UString ILiteralValueProvider.GetTextValue(SourceRange range) => TextValue;
		Symbol ILiteralValueProvider.GetTypeMarker(SourceRange range) => TypeMarker;
		object ILiteralValueProvider.GetValue(SourceRange range) => (object)TextValue;
	}

	/// <summary>Bundles the optional original text of a value with an optional in-memory 
	/// form of it; see remarks at <see cref="ILiteralValue"/>.</summary>
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

	/// <summary>This structure is intended to be stored in a <see cref="LiteralNode"/>,
	/// where it holds the value of a literal while being able to retrieve the 
	/// original text of that value from the source code. This type is slightly
	/// smaller than <see cref="LiteralValue"/> because it doesn't hold a 
	/// reference to the source text (it relies on <see cref="LiteralNode"/> to
	/// provide access to that.)</summary>
	public struct ParsedLiteral : ILiteralValueProvider
	{
		public ParsedLiteral(object value, int startIndex, int length, Symbol typeMarker)
		{
			_value = value;
			_startIndex = startIndex;
			_length = length;
			_typeMarker = typeMarker;
		}
		object _value;
		int _startIndex, _length;
		Symbol _typeMarker;

		public UString GetTextValue(SourceRange range) => 
			_startIndex < 0 ? default(UString) : range.Source.Text.Slice(_startIndex, _length);
		public Symbol GetTypeMarker(SourceRange range) => _typeMarker;
		public object GetValue(SourceRange range) => _value;
	}
}
