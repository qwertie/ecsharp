using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Collections;
using Loyc.Utilities;
using System.Diagnostics;

namespace Loyc.LLParserGenerator
{
	/// <summary>Provides standard functionality of LLLPG parsers.
	/// Note that there are no public members.</summary>
	/// <typeparam name="InToken">Type of input to process: typically int for a 
	/// character stream or Node for a token stream.</typeparam>
	public class BaseRecognizer<InToken>
	{
		protected BaseRecognizer(IParserSource<InToken> source = null)
		{
			_source = source;
		}

		protected int _inputPosition = 0;
		protected IParserSource<InToken> _source;
		
		protected readonly InToken EOF = 
			typeof(InToken) == typeof(char) ? (InToken)(object)'\uFFFF' : 
			typeof(InToken) == typeof(int) ? (InToken)(object)-1 : default(InToken);

		////////////////////////////////////////////////////////////////////////
		// Interface used by parser generator //////////////////////////////////

		protected InToken LA(int i)
		{
			return _source.TryGet(_inputPosition + i, EOF);
		}
		protected void Match()
		{
			_inputPosition++; 
			Debug.Assert(_inputPosition <= _source.Count);
		}
		protected void Match(InToken ch)
		{
			if (!ch.Equals(LA(0)))
				Error(TokenName(ch), LA(0));
			else
				Match();
		}
		protected void Match(InToken alt1, InToken alt2)
		{
			var la0 = LA(0);
			if (!la0.Equals(alt1) && !la0.Equals(alt2))
				Error(TokenName(alt1, alt2), LA(0));
			else
				Match();
		}
		protected void Match(InToken[] alts)
		{
			var la0 = LA(0);
			for (int i = 0; i < alts.Length; i++)
				if (la0.Equals(alts[i]))
					return;
			
			string expected;
			if (alts.Length < 4)
				expected = TokenName(alts);
			else
				expected = TokenName(alts.Slice(0, 3).ToArray()) + "...";
			Error(expected, la0);
		}
		protected virtual void Error(string expected, InToken LA)
		{
			string msg = string.Format("{0} {1}", GetErrorHeader(), GetErrorMessage(expected, LA));
			throw new Exception(msg);
		}

		////////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////

		protected virtual string GetErrorHeader()
		{
			return _source.IndexToLine(_inputPosition).ToString();
		}
		protected virtual string GetErrorMessage(string expected, InToken LA)
		{
			return string.Format(
				"Syntax error at {1}: expected {0}", expected, TokenName(LA));
		}
		protected virtual string TokenName(params InToken[] id)
		{
			var sb = new StringBuilder();
			bool @char = typeof(InToken) == typeof(int);
			for (int i = 0; i < id.Length; i++)
			{
				InToken tok = id[i];
				if (sb.Length != 0)
					sb.Append(' ');
				if (@char) {
					int ch = (int)(object)tok;
					if (ch == -1) {
						sb.Append("EOF");
						continue;
					} else if (ch >= 0 && ch <= 0xFFFF) {
						sb.AppendFormat("'{0}'", EscapedChar((char)ch));
						continue;
					}
				}
				sb.Append(tok.ToString());
			}
			return sb.ToString();
		}
		protected virtual string EscapedChar(char c)
		{
			return G.EscapeCStyle(c.ToString(), EscapeC.Control | EscapeC.ABFV | EscapeC.SingleQuotes);
		}
	}
}
