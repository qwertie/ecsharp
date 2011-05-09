using System;
using System.Collections.Generic;
using Loyc.Essentials;
using Loyc.Utilities;
using System.Collections;
using Loyc.CompilerCore.ExprParsing;
using System.Diagnostics;

namespace Loyc.CompilerCore.ExprNodes
{
	/// <summary>
	/// A class that holds an operator specification and supplies it to an 
	/// IONEParser.
	/// </summary>
	public abstract class AbstractOperator<Token> //: IOneOperator<Token>
		where Token : ITokenValueAndPos
	{
		public AbstractOperator(string name, Symbol type) 
		{
			_name = name;
			_type = type;
		}
		public AbstractOperator(string name, Symbol type, IOperatorPartMatcher[] tokens) 
		{ 
			_name = name;
			_type = type;
			_tokens = tokens; 
		}
		public AbstractOperator(IOneOperator<Token> original)
		{ 
			_name = original.Name; 
			_type = original.Type; 
			_tokens = ArrayExt.Clone(original.Parts);
		}
		protected string _name;
		protected Symbol _type;
		protected IOperatorPartMatcher[] _tokens = null;

		#region IOneOperator interface (Generate() is left unimplemented)

		public string Name { get { return _name; } }    // e.g. "compare equal"
		public Symbol Type { get { return _type; } }
		
		public virtual IOperatorPartMatcher[] Parts
		{
			get { return _tokens; }
			set { _tokens = value; }
		}
		public virtual int ComparePriority(IOneOperator<Token> other) 
		{
			return 0;
		}
		public virtual bool IsAcceptable(OneOperatorMatch<Token> match, ISimpleMessageSink output)
		{
			return true;
		}

		#endregion

		/// <summary>Retrieves the IToken corresponding to an ITokenValue, if the
		/// ITokenValue is either an IToken or a TokenValueWrapper(of IToken).</summary>
		/// <remarks>
		/// Unfortunately, Loyc's current design is a bit strange in regard to tokens
		/// in OneOperatorMatchPart(of TokenT). Because BasicDividerSource(of Token)
		/// is unable to create new tokens of type Token, it instead produces
		/// TokenValueWrapper objects that wrap around a Token. However, BasicOneParser
		/// sometimes uses BasicDividerSource and sometimes uses the original token
		/// source, depending on whether division is necessary. Consequently, the
		/// tokens matched are sometimes of type Token and sometimes of type 
		/// TokenValueWrapper(of Token); that's why OneOperatorMatchPart(of TokenT).Token 
		/// is declared as an ITokenValue instead of a TokenT. Anyway, this method is
		/// needed to obtain the original IToken to be used as a prototype.
		/// 
		/// I should really think of a better implementation, a better system.
		/// </remarks>
		/*public AstNode GetIToken(ITokenValue value)
		{
			IToken valueA = value as IToken;
			if (valueA != null)
				return valueA;
			TokenValueWrapper<IToken> valueB = value as TokenValueWrapper<IToken>;
			if (valueB != null)
				return valueB.Original;
			Debug.Fail("Not sure what to do :(");
			return null;
		}*/

		#region Default operator names and types
		public static string DefaultBinaryOpName(string tokenText)
		{
			string tt = tokenText;
			if (NeedQuotes(tokenText))
				tt = "'" + tt + "'";
			return Localize.From("binary {0}", tt);
		}
		public static Symbol DefaultBinaryOpType(string tokenText)
		{
			return GSymbol.Get(string.Format(NeedQuotes(tokenText) ? "e_{0}_e" : "e{0}e", tokenText));
		}
		public static string DefaultPostfixOpName(string tokenText)
		{
			string tt = tokenText;
			if (NeedQuotes(tokenText))
				tt = "'" + tt + "'";
			return Localize.From("postfix {0}", tt);
		}
		public static Symbol DefaultPostfixOpType(string tokenText)
		{
			return GSymbol.Get(string.Format(NeedQuotes(tokenText) ? "e_{0}" : "e{0}", tokenText));
		}
		public static string DefaultPrefixOpName(string tokenText)
		{
			string tt = tokenText;
			if (NeedQuotes(tokenText))
				tt = "'" + tt + "'";
			return Localize.From("prefix {0}", tt);
		}
		public static Symbol DefaultPrefixOpType(string tokenText)
		{
			return GSymbol.Get(string.Format(NeedQuotes(tokenText) ? "{0}_e" : "{0}e", tokenText));
		}
		public static string DefaultTernaryOpName(string tokenText1, string tokenText2)
		{
			string tt = tokenText1 + " " + tokenText2;
			if (NeedQuotes(tokenText1, tokenText2))
				tt = "'" + tt + "'";
			return Localize.From("ternary {0}", tt);
		}
		public static Symbol DefaultTernaryOpType(string tokenText1, string tokenText2)
		{
			return GSymbol.Get(string.Format(NeedQuotes(tokenText1, tokenText2) 
				? "e_{0}_e_{1}_e" : "e{0}e{1}e", tokenText1, tokenText2));
		}
		protected static bool NeedQuotes(string tokenText)
		{
			return string.IsNullOrEmpty(tokenText) || char.IsLetterOrDigit(tokenText[0]) || char.IsLetterOrDigit(tokenText[tokenText.Length-1]);
		}
		protected static bool NeedQuotes(string tokenText1, string tokenText2)
		{	// This overload is for ternary operators
			return NeedQuotes(tokenText1) || NeedQuotes(tokenText2);
		}
		#endregion
	}
}
