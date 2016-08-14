using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Les;

namespace Loyc.Syntax
{
	/// <summary>A special literal is a normal number or string paired with a 
	/// (typically unrecognized) type prefix or suffix.</summary>
	/// <remarks>This structure is used as the value of an LESv3 token to 
	/// represent non-standard numbers and strings such as <c>1.1unum</c> 
	/// and <c>bytes"ab cd"</c></remarks>
	public struct SpecialLiteral : IEquatable<SpecialLiteral>
	{
		public SpecialLiteral(object value, Symbol literalType)
		{
			this = default(SpecialLiteral); // only needed in VS 2010 (C# 4?)
			Value = value; LiteralType = literalType;
		}
		/// <summary>The numeric or string value of the literal.</summary>
		public object Value { get; set; }
		/// <summary>A prefix or suffix on the literal that represents some 
		/// additional meaning.</summary>
		/// <remarks>In LESv3, this field becomes the sole parameter <c>s</c> 
		/// of a <c>#trivia_literalType(s)</c> attribute attached to the <see cref="LNode"/>
		/// that represents the literal.</remarks>
		public Symbol LiteralType { get; set; }

		public bool Equals(SpecialLiteral rhs)
		{
			return LiteralType == rhs.LiteralType && (Value == rhs.Value || Value != null && Value.Equals(rhs.Value));
		}
		public override bool Equals(object obj)
		{
			if (obj is SpecialLiteral) {
				var rhs = (SpecialLiteral)obj;
				return Equals(rhs);
			}
			return false;
		}
		public override int GetHashCode()
		{
			var hc = LiteralType.Id;
			if (Value == null)
				return hc;
			return hc ^ Value.GetHashCode();
		}
		public override string ToString()
		{
			if (Value is string)
				return LiteralTypeAsLes3Identifier() + "\"" + ParseHelpers.EscapeCStyle((string)Value, EscapeC.Control | EscapeC.DoubleQuotes) + "\"";
			else if (Value is double)
				return ((double)Value).ToString("0.0") + LiteralTypeAsLes3Identifier();
			else
				return Value.ToString() + LiteralTypeAsLes3Identifier();
		}
		internal string LiteralTypeAsLes3Identifier()
		{
			if (LesNodePrinter.IsNormalIdentifier(LiteralType))
				return LiteralType.Name;
			else
				return "`" + ParseHelpers.EscapeCStyle(LiteralType.Name, EscapeC.Control, '`') + "`";
		}
	}
}
