/*
 * Created by David on 7/17/2007 at 7:11 AM
 */

using System;
using System.Collections.Generic;
using Loyc.Runtime;
using System.Diagnostics;

namespace Loyc.CompilerCore.Onep
{
	/// <summary>Represents a single token or a sub-expression within an operator.
	/// If the part represents a subexpression, the precedence limit on the 
	/// subexpression is Prec. If the part represents a token, the token can match 
	/// on specific text (the Text field), on a specific token type (Type), or both.</summary>
	public class OneOperatorPart<Token>
		where Token : ITokenValue
	{
		public OneOperatorPart(int prec)
			{ Prec = prec; Type = Tokens.Null; Text = null; }
		public OneOperatorPart(Symbol type, string text) 
			{ Type = type; Text = text; Prec = -1; }
		public OneOperatorPart(Symbol type) 
			{ Type = type; Text = null; Prec = -1; }
		public OneOperatorPart(string text) 
			{ Type = Tokens.Null; Text = text; Prec = -1; }
		
		/// <summary>Content to match, e.g. "==", or null if any text is acceptable or if
		/// this token matches an expression.</summary>
		public string Text;
		/// <summary>Token type to match, e.g. Tokens.ML_COMMENT; Tokens.Null matches 
		/// any type with the correct text. The value should also be null if this 
		/// part matches a subexpression.</summary>
		public Symbol Type;
		/// <summary>Precedence value, conventionally between 0 and 100, typically from 
		/// the Precedence enumeration. Lower values (closer to zero) have higher 
		/// precedence.</summary>
		public int Prec;
		
		public bool MatchesExpr { get { return Prec != -1; } }
		
		public bool Match(ITokenValue t) 
		{
			Debug.Assert(!MatchesExpr);
			if (t == null || (Type != t.Type && !Type.IsNull))
				return false;
			return Text == null || Text == t.Text;
		}
		public override string ToString()
		{
			if (Prec != -1) {
				return "e" + Prec.ToString();
			} else if (Text != null)
				return string.Format("{0}\"{1}\"", Type, Text);
			else
				return Type.ToString();
		}
		public override bool Equals(object b) 
		{
			OneOperatorPart<Token> b_ = b as OneOperatorPart<Token>;
			return this == b_;
		}
		public static bool operator ==(OneOperatorPart<Token> a, OneOperatorPart<Token> b)
		{
			if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null))
				return object.ReferenceEquals(a, null) && object.ReferenceEquals(b, null);
			else
				return a.Prec == b.Prec && a.Text == b.Text && a.Type == b.Type;
		}
		public static bool operator !=(OneOperatorPart<Token> a, OneOperatorPart<Token> b) 
		{
			return !(a == b); 
		}
		public override int GetHashCode() 
		{
			int hc = Prec + Type.Id; 
			if (Text != null) 
				hc += Text.GetHashCode();
			return hc;
		}
	}
}
