using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using NUnit.Framework;

namespace Loyc.CompilerCore.Onep
{
	/// <summary>
	/// A reasonable default implementation of IOperatorDivider. Using the
	/// operators supplied to ProcessOperators(), Divide() splits a token the way a 
	/// standard lexer would.
	/// </summary>
	public class BasicOperatorDivider : IOperatorDivider
	{
		/// <summary>The set of punctuation strings and their lengths.</summary>
		protected Dictionary<string, int> _opStrings;
		
		/// <summary>Builds a table of operator strings from a list of operators.
		/// </summary>
		public void ProcessOperators<Expr,Token>(IEnumerable<IOneOperator<Expr,Token>> ops)
			where Token : ITokenValue
		{
			_opStrings = new Dictionary<string, int>();
			System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(@"[a-zA-Z0-9]");
			// Create operator lookup table (hashtable)
			foreach (IOneOperator<Expr,Token> op in ops) {
				foreach(OneOperatorPart<Token> part in op.Parts) {
					// Thers'e a problem here: we're not necessarily told whether the
					// operator is supposed to match PUNC or not. We don't want to
					// clutter up our look-up table with stuff that isn't punctuation 
					// (although it won't prevent the division process from working 
					// correctly even if there is unnecessary crap in the LUT), so use a
					// regex to exclude tokens that clearly have non-punctuation stuff.
					if (!string.IsNullOrEmpty(part.Text) && rx.IsMatch(part.Text) 
					    && !_opStrings.ContainsKey(part.Text))
						_opStrings.Add(part.Text, part.Text.Length);
				}
			}
		}
		
		public bool MayDivide(ITokenValue token)
		{
			string text = token.Text;
			return text.Length > 1 && !_opStrings.ContainsKey(text);
		}
		
		/// <summary>Splits a token the way a standard lexer would, calling 'func'
		/// for each relevant substring of the token.</summary>
		/// <remarks>This method only produces one interpretation.
		/// 
		/// At each character position in the input, it looks for the longest
		/// matching operator.
		/// 
		/// Normally this method is used to divide PUNC tokens, but this type
		/// is not required.</remarks>
		public IEnumerator<DividedOperatorPart> Divide(ITokenValue token)
		{
			string text = token.Text;
			if (text == null || text.Length <= 1 || _opStrings.ContainsKey(text))
				yield return new DividedOperatorPart(0, text);
			else {
				int i = 0, len = text.Length - 1;
				string substr;
				for(;;) {
					for (;; len--) {
						substr = text.Substring(i, len);
						if (len <= 1 || _opStrings.ContainsKey(substr))
							break;
					}
					yield return new DividedOperatorPart(i, substr);
					i++;
					len = text.Length - i;
					if (len <= 0)
						break;
				}
			}
		}
	}

	[TestFixture]
	class BasicOperatorDividerTests
	{
		[Test]
		public void TestWhenEmpty()
		{
			IOperatorDivider div = new BasicOperatorDivider();
			TokenValue tok = new TokenValue(Tokens.PUNC);
			CheckMatch(tok, div, "", "");
			CheckMatch(tok, div, "?", "?");
			CheckMatch(tok, div, "?&", "?", "&");
			CheckMatch(tok, div, "&&", "&", "&");
		}

		[Test]
		public void TestWithSingleCharOps()
		{
			IOperatorDivider div = new BasicOperatorDivider();
			TokenValue tok = new TokenValue(Tokens.PUNC);
			//List<IOneOperator<ICodeNode,TokenValue>
			//div.ProcessOperators(ops);
			CheckMatch(tok, div, "", "");
			CheckMatch(tok, div, "?", "?");
			CheckMatch(tok, div, "?&", "?", "&");
			CheckMatch(tok, div, "&&", "&", "&");
		}

		void CheckMatch(TokenValue tok, IOperatorDivider div, string input, params string[] parts)
		{
			tok.Text = input;
			Assert.AreEqual(parts.Length <= 1, div.MayDivide(tok));
			CheckMatch(div.Divide(tok), parts);
		}
		void CheckMatch(IEnumerator<DividedOperatorPart> e, params string[] parts)
		{
			int offs = 0;
			for (int i = 0; i < parts.Length; i++) {
				Assert.AreEqual(i == parts.Length-1, e.MoveNext());
				Assert.AreEqual(offs, e.Current.Offset);
				Assert.AreEqual(parts[i], e.Current.Substring);
				offs += parts[i].Length;
			}
		}
	}
}
