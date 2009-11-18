using System;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using NUnit.Framework;
using System.Text.RegularExpressions;

namespace Loyc.CompilerCore.ExprParsing
{
	/*public class StandardOperatorDivider
	{
		protected Dictionary<string, int> _opStrings;

		public void ProcessOperators(IEnumerable<IOneOperator<AstNode>> ops)
		{
			_opStrings = new Dictionary<string, int>();
			Regex rx = new Regex(@"[a-zA-Z0-9]");
			
			// Create operator lookup table (hashtable)
			foreach (IOneOperator<AstNode> op in ops)
			{
				foreach (IOperatorPartMatcher part in op.Parts)
				{
					// Thers'e a problem here: we're not necessarily told whether the
					// operator is supposed to match PUNC or not. We don't want to
					// clutter up our look-up table with stuff that isn't punctuation 
					// (although it won't prevent the division process from working 
					// correctly even if there is unnecessary crap in the LUT), so use a
					// regex to exclude tokens that clearly have non-punctuation stuff.
					if (!string.IsNullOrEmpty(part.Text) && !rx.IsMatch(part.Text)
						&& !_opStrings.ContainsKey(part.Text))
						_opStrings.Add(part.Text, part.Text.Length);
				}
			}
		}
		public bool MayDivide(ITokenValue token)
		{
			if (token.NodeType != Tokens.PUNC)
				return false;
			else {
				string text = token.Text;
				return text.Length > 1 && !_opStrings.ContainsKey(text);
			}
		}
	}*/
	

	/// <summary>A factory that must be supplied to BasicOperatorDivider so it can
	/// create subtokens (tokens that represent part of an existing token).
	/// </summary>
	/// <param name="t">An existing token</param>
	/// <param name="offset">Index in t.Text at which substring begins</param>
	/// <param name="substring">Substring that should become the Text of the
	/// subtoken</param>
	/// <returns>A new subtoken</returns>
	public delegate Token CreateSubToken<Token>(Token t, int offset, string substring);

	/// <summary>
	/// A reasonable default implementation of IOperatorDivider. Using the
	/// operators supplied to ProcessOperators(), Divide() splits a token the way a 
	/// standard lexer would.
	/// </summary>
	public class BasicOperatorDivider<Token> : IOperatorDivider<Token>
		where Token : ITokenValueAndPos
	{
		CreateSubToken<Token> _subTokenFactory;
		public BasicOperatorDivider(CreateSubToken<Token> tokenFactory)
			{ _subTokenFactory = tokenFactory; }

		/// <summary>The set of punctuation strings and their lengths.</summary>
		protected Dictionary<string, int> _opStrings;
		
		/// <summary>Builds a table of operator strings from a list of operators.
		/// </summary>
		public void ProcessOperators(IEnumerable<IOneOperator<Token>> ops)
		{
			_opStrings = new Dictionary<string, int>();
			Regex rx = new Regex(@"[a-zA-Z0-9]");
			// Create operator lookup table (hashtable)
			foreach (IOneOperator<Token> op in ops) {
				foreach(IOperatorPartMatcher part in op.Parts) {
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

		public bool MayDivide(Token token)
		{
			string text = token.Text;
			return text.Length > 1 && !_opStrings.ContainsKey(text);
		}
		
		/// <summary>Splits a token the way a standard lexer would, creating a
		/// subtoken for each relevant substring of the token.</summary>
		/// <remarks>This method only produces one interpretation.
		/// 
		/// At each character position in the input, it looks for the longest
		/// matching operator.
		/// 
		/// Normally this method is used to divide PUNC tokens, but this type
		/// is not required.</remarks>
		public IOperatorDividerState<Token> Divide(Token token) { return new State(token, this); }

		public class State : IOperatorDividerState<Token>
		{
			internal State(Token t, BasicOperatorDivider<Token> parent) { _token = t; _parent = parent; }
			BasicOperatorDivider<Token> _parent;
			Token _token;
			bool _done;

			public bool GetNext(List<Token> result)
			{
				if (_done)
					return false;
				result.Clear();
				string text = _token.Text;
				if (text == null || text.Length <= 1 || _parent._opStrings.ContainsKey(text))
					result.Add(_token);
				else {
					int i = 0, len = text.Length - 1;
					string substr;
					for(;;) {
						for (;; len--) {
							substr = text.Substring(i, len);
							if (len <= 1 || _parent._opStrings.ContainsKey(substr))
								break;
						}
						result.Add(_parent._subTokenFactory(_token, i, substr));
						i++;
						len = text.Length - i;
						if (len <= 0)
							break;
					}
				}
				_done = true;
				return true;
			}
		}
	}

	[TestFixture]
	class BasicOperatorDividerTests
	{
		[Test]
		public void TestWhenEmpty()
		{
			IOperatorDivider<TokenValueAndPos> div = new BasicOperatorDivider<TokenValueAndPos>(TokenValue.SubTokenFactory);
			TokenValueAndPos tok = new TokenValueAndPos(Tokens.PUNC);
			CheckMatch(tok, div, "", "");
			CheckMatch(tok, div, "?", "?");
			CheckMatch(tok, div, "?&", "?", "&");
			CheckMatch(tok, div, "&&", "&", "&");
		}

		[Test]
		public void TestWithSingleCharOps()
		{
			IOperatorDivider<TokenValueAndPos> div = new BasicOperatorDivider<TokenValueAndPos>(TokenValue.SubTokenFactory);
			TokenValueAndPos tok = new TokenValueAndPos(Tokens.PUNC);
			//List<IOneOperator<ICodeNode,TokenValue>
			//div.ProcessOperators(ops);
			CheckMatch(tok, div, "", "");
			CheckMatch(tok, div, "?", "?");
			CheckMatch(tok, div, "?&", "?", "&");
			CheckMatch(tok, div, "&&", "&", "&");
		}

		void CheckMatch(TokenValueAndPos tok, IOperatorDivider<TokenValueAndPos> div, string input, params string[] parts)
		{
			tok.Text = input;
			Assert.AreEqual(parts.Length <= 1, div.MayDivide(tok));
			List<TokenValueAndPos> subTok = new List<TokenValueAndPos>();
			div.Divide(tok).GetNext(subTok);
			CheckMatch(subTok.GetEnumerator(), parts);
		}
		void CheckMatch(IEnumerator<TokenValueAndPos> e, params string[] parts)
		{
			int offs = 0;
			for (int i = 0; i < parts.Length; i++) {
				Assert.AreEqual(i == parts.Length-1, e.MoveNext());
				Assert.AreEqual(parts[i], e.Current.Text);
				offs += parts[i].Length;
			}
		}
	}
}
