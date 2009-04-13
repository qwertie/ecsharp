using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	/// <summary>Interface for a predicate that can be tested against tokens.</summary>
	public interface ITokenMatcher
	{
		/// <summary>Token type that is required to match, e.g. Tokens.ML_COMMENT; 
		/// Tokens.Null may match tokens of more than one type.</summary>
		Symbol Type { get; }

		/// <summary>Token text to match, e.g. "==", or null if more than one 
		/// string (or any string) can match.</summary>
		string Text { get; }

		/// <summary>Examines a token and decides whether it matches</summary>
		/// <returns>True if token 't' was a match.</returns>
		/// <remarks>If nether NodeType nor Text are null, the match criteria are fully
		/// specified so Match() must return
		/// <c>t.NodeType == NodeType &amp;&amp; t.Text == Text</c>.
		/// </remarks>
		bool Match(ITokenValue t);
	}

	/// <summary>Standard token matcher: matches a specific token type, specific token text,
	/// or both. Additionally, if NodeType.IsNull and Text == null then any token matches.</summary>
	public struct TokenMatcher : ITokenMatcher
	{
		public TokenMatcher(Symbol type, string text)
		{ _type = type; _text = text; }
		public TokenMatcher(Symbol type)
		{ _type = type; _text = null; }
		public TokenMatcher(string text)
		{ _type = null; _text = text; }

		private Symbol _type;
		private string _text;

		/// <summary>Token type to match, e.g. Tokens.ML_COMMENT; Tokens.Null matches 
		/// any type of token with the correct text.</summary>
		public Symbol Type { get { return _type; } set { _type = value; } }

		/// <summary>Content to match, e.g. "==", or null if any text is acceptable.</summary>
		public string Text { get { return _text; } set { _text = value; } }

		public bool Match(ITokenValue t)
		{
			if (t == null || (Type != t.NodeType && Type != null))
				return false;
			return Text == null || Text == t.Text;
		}
		public override string ToString()
		{
			if (Text != null)
				return string.Format("{0}\"{1}\"", Type, Text);
			else
				return Type.ToString();
		}
		public override bool Equals(object b_)
		{
			ITokenMatcher b = b_ as ITokenMatcher;
			return b != null && b.Text == Text && b.Type == Type;
		}
		public static bool operator ==(TokenMatcher a, TokenMatcher b)
		{
			return a.Text == b.Text && a.Type == b.Type;
		}
		public static bool operator !=(TokenMatcher a, TokenMatcher b)
		{
			return !(a == b);
		}
		public override int GetHashCode()
		{
			int hc = Type.Id;
			if (Text != null)
				hc += Text.GetHashCode();
			return hc;
		}
	}
}
