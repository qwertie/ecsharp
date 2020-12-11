using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	/// <summary>Represents a serialized text form of a literal value.</summary>
	public interface ISerializedLiteral
	{
		/// <summary>Represents the serialized text of the value.</summary>
		/// <remarks>Typically this will be a parsed form of the string; for example in LES3, 
		/// if the TextValue is `C:\Users`, the raw text may be `C:\\Users` which was parsed 
		/// so that the double backslash became a single backslash.
		/// <para/>
		/// Since this property has type <see cref="UString"/> which is a struct, it cannot 
		/// be null, but <c>TextValue.IsNull</c> can be true when this interface is part of
		/// <see cref="ILiteralValue"/> or <see cref="ILNode"/>. This means either that the 
		/// original text form of the literal has been discarded, or that it never existed. 
		/// For example if you call <see cref="Loyc.Syntax.LNode.Literal(123)"/> it will 
		/// create a "synthetic" node representing the integer 123. In that case, there is no 
		/// text form, <c>TextValue.IsNull</c> will be true and <c>TypeMarker</c> will be 
		/// null.
		/// </remarks>
		UString TextValue { get; }

		/// <summary>Represents the type of the value.</summary>
		/// <remarks>The Type Marker indicates not just the type but also the syntax of the
		/// <see cref="TextValue"/>. If the syntax of a TextValue is not compatible with 
		/// the syntax used in LES, it should not use the same type marker as is used in LES.
		/// <para/>
		/// The TypeMarker can be null when this object is a <see cref="ILNode"/> and no 
		/// type marker was assigned to it, either because the node is synthetic (did not
		/// come from a source file) or the parser simple did not assign one. In this case,
		/// <c>TextValue.IsNull</c> is true. Conversely, if the TypeMarker is not null, 
		/// <c>TextValue.IsNull</c> may be true or false (for details, see the documentation 
		/// of <see cref="ILiteralValue"/>).
		/// </remarks>
		Symbol TypeMarker { get; }
	}

	/// <summary>Bundles the optional original text of a value with an optional in-memory form of it.</summary>
	/// <remarks>
	/// <see cref="ILNode"/> objects that do not represent literals will have a Value property 
	/// that returns <see cref="NoValue.Value"/>. Also, the TextValue and TypeMarker for a value 
	/// may not be known, in which case they will return an empty string and null, respectively. 
	/// It could also be that the <see cref="ISerializedLiteral.TextValue"/> is known but the 
	/// value it represents is not known, in which case the Value may be a copy of the TextValue.
	/// <para/>
	/// If this object represents a literal, Value should never be <see cref="NoValue.Value"/>, 
	/// and it should not be null unless null is the actual value of the literal.
	/// <para/>
	/// In all, a literal may have the the following valid combinations of properties:
	/// <ul>
	/// <li>TextValue.IsNull, null TypeMarker, valid Value: this combination often occurs when
	///     nodes created programmatically and have never been in text form before, but it 
	///     may also occur if the parser isn't designed to preserve input text, or if the
	///     syntax is nonstandard and there is no benefit in preserving the text. Keep in
	///     mind that the TypeMarker dictates constraints on syntax, not just type, so if a 
	///     language uses an unusual literal syntax the most reasonable thing is often to
	///     parse the text and not include it in the Loyc tree.</li>
	/// <li>TextValue.IsNull, non-null TypeMarker, valid Value: this combination can be used
	///     to disambiguate when one type corresponds to multiple TypeMarkers. For example,
	///     the literal 123 has the generic numeric type marker "_". This may be stored as 
	///     a 32-bit integer in the Loyc tree, but suppose that an attempt is made to store
	///     the integer in an 8-bit integer variable. Should it succeed? It is conceivable
	///     that in a particular programming language, a generic number 123 (type marker "_") 
	///     can be stored in a byte variable, but that a literal with the explicit type 
	///     marker "_i32" should not be convertible to a byte because is marked as 32 bits.</li>
	/// <li>Non-null TextValue, non-null TypeMarker: In this case the Value will either be
	///     a parsed form of TextValue (so that <c>!Value.Equals(TextValue)</c> or a boxed
	///     form of TextValue so that <c>Value.Equals(TextValue)</c>. The latter case 
	///     indicates that the parser did not recognize the TypeMarker, or that parsing the 
	///     TextValue failed, that literal parsing is not enabled in the parser, or that
	///     the value is a string but the parser chose not to convert it to the .NET native
	///     string type.</li>
	/// </ul>
	/// Caution: TextValue is <see cref="UString"/> and an "empty" UString is "equal" to a 
	/// "null" UString since the list of characters is the same. Do not compare UString 
	/// with null; instead, use <see cref="UString.IsNull"/>.
	/// </remarks>
	public interface ILiteralValue : ISerializedLiteral, IHasValue<object>
	{
		//bool IsParsed { get; }
	}
}
