using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
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
	/// reference to the source text, because it is embedded in a 
	/// <see cref="LiteralNode"/> which has a reference to that.</summary>
	/// <remarks>In addition, this struct provides the ability to parse lazily.</remarks>
	public struct ParsedValue : ILiteralValueProvider
	{
		public ParsedValue(object value, int startIndex, int length, Symbol typeMarker)
		{
			_value = value;
			_startIndex = startIndex;
			_length = length;
			_typeMarker = typeMarker;
		}
		object _value;
		int _startIndex, _length;
		Symbol _typeMarker;

		UString ILiteralValueProvider.GetTextValue(SourceRange range) => 
			_startIndex < 0 ? default(UString) : range.Source.Text.Slice(_startIndex, _length);
		Symbol ILiteralValueProvider.GetTypeMarker(SourceRange range) => _typeMarker;
		object ILiteralValueProvider.GetValue(SourceRange range) => _value;
	}

	/// <summary>The intention of this interface is that a <c>struct</c> implementing 
	/// it can be embedded inside a <see cref="LiteralNode"/> in order to lazily obtain
	/// the text of a literal, or even parse it lazily.</summary>
	/// <remarks>Each method here is given the value of <see cref="LNode.Range"/> so that 
	/// the provider has access to the original source code text.</remarks>
	public interface ILiteralValueProvider
	{
		/// <summary><see cref="LNode.Value"/> returns whatever this returns.</param>
		object GetValue(SourceRange range);
		/// <summary><see cref="LNode.TextValue"/> returns whatever this returns.</param>
		UString GetTextValue(SourceRange range);
		/// <summary><see cref="LNode.TypeMarker"/> returns whatever this returns.</param>
		Symbol GetTypeMarker(SourceRange range);
	}
}
