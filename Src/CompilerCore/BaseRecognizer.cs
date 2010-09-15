using System.IO;
using System.Collections.Generic;
using Loyc.Runtime;
using Loyc.Utilities;
using System;
using System.Diagnostics;

namespace Loyc.CompilerCore
{
	/// <summary>Provides standard functionality of LoycPG parsers. 
	/// Note that there are no public members.</summary>
	/// <typeparam name="InTokenId">Type of input to process: typically int for a 
	/// character stream or IToken/ITokenValue for a token stream.</typeparam>
	public class BaseRecognizer<InTokenId>
	{
		protected BaseRecognizer(IParserSource<InTokenId> source)
		{
			_source = source;
		}

		// _prematch is a "prepredicted symbol" matched by the caller. The 
		// callee should disregard _prealt unless _prematch is the expected
		// value for the rule.
		protected Symbol _prematch;
		// _prealt is a "prepredicted alternative number" matched by the 
		// caller. It does not indicate that the entire alternative is 
		// prematched, only enough to make a prediction. Values: -1 for loop 
		// exit, 0 for unknown, or 1,2,3 for other alternatives
		protected int _prealt;
		protected Stack<int> _savedPositions = new Stack<int>();
		protected int _inputPosition = 0;
		protected IParserSource<InTokenId> _source;
		protected readonly InTokenId EOF = typeof(InTokenId) == typeof(char) ? (InTokenId)(object)'\uFFFF' : default(InTokenId);

		////////////////////////////////////////////////////////////////////////
		// Interface used by parser generator //////////////////////////////////

		protected void ClearPreAlt()
		{
			_prealt = 0; // "Unknown"
		}
		protected void SetPrematch(Symbol prematch, int prealt)
		{
			_prematch = prematch;
			_prealt = prealt;
		}
		protected InTokenId LA(int i)
		{
			return _source[_inputPosition + i, EOF];
		}
		protected delegate bool MatchPred(InTokenId LA);
		protected void Match(MatchPred pred)
		{
			if (!pred(LA(0)))
				Throw("something else", LA(0));
			else
				Consume();
		}
		protected void Match(InTokenId ch)
		{
			if (!ch.Equals(LA(0)))
				Throw(TokenName(ch), LA(0));
			else
				Consume();
		}
		protected void Consume() {
			_inputPosition++; 
			Debug.Assert(_inputPosition <= _source.Count);
		}
		protected void Consume(int count) 
		{
			_inputPosition += count;
			Debug.Assert(_inputPosition <= _source.Count);
		}

		protected virtual void Throw(string expected, InTokenId LA)
		{
			if (_savedPositions.Count > 0) {
				// Guessing mode: return false from Match() instead of throwing
				_guessFailed = true;
			} else {
				string msg = string.Format("{0} {1}", GetErrorHeader(), GetErrorMessage(expected, LA));
				throw new RecognitionException(msg, _source, _inputPosition, TokenName(LA));
			}
		}
		protected virtual void EmitErrorMessage(string msg)
		{
			Console.Error.WriteLine(msg);
		}
		protected void BeginGuess()
		{
			_savedPositions.Push(_inputPosition);
		}
		protected void EndGuess()
		{
			_inputPosition = _savedPositions.Pop();
			_guessFailed = false;
		}
		protected bool IsGuessing { get { return _savedPositions.Count > 0; } }
		protected bool _guessFailed;
		protected bool GuessFailed { 
			get { return _guessFailed; } 
		}
		protected Symbol Prematch { get { return _prematch; } }
		protected int PreAlt      { get { return _prealt; } }

		////////////////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////////////////

		protected virtual string GetErrorHeader()
		{
			return _source.IndexToLine(_inputPosition).ToString();
		}
		protected virtual string GetErrorMessage(string expected, InTokenId LA)
		{
			return string.Format(
				"Syntax error: expected '{0}' but got '{1}'", expected, TokenName(LA));
		}
		protected virtual string TokenName(InTokenId id)
		{
			if (id is int) {
				int iid = (int)(object)id;
				if (iid == -1)
					return "EOF";
				else if (iid >= 0 && iid <= 0xFFFF)
					return NameOfChar((char)iid);
			}
			return id.ToString();
		}
		protected virtual string NameOfChar(char c)
		{
			// TODO this should be hooked up to some kind of token renderer
			// that converts content to a valid source code string
			if (c == '\\') return @"'\\'";
			if (c == '\'') return @"'\''";
			return string.Format("'{0}'", c);
		}
	}

}
