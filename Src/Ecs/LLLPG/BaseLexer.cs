using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.CompilerCore;
using System.Diagnostics;

namespace Loyc.LLParserGenerator
{
	public class BaseLexer<Source> where Source : IParserSource<char>
	{
		protected int _inputPosition = 0;
		protected Source _source;

		protected BaseLexer(Source input) { _source = input; }

		protected int LA(int i)
		{
			bool fail = false;
			char result = _source.TryGet(_inputPosition + i, ref fail);
			return fail ? -1 : result;
		}

		protected void Consume()
		{
			// Called when prediction already verified the input (and LA(0) is not saved)
			Debug.Assert(_inputPosition < _source.Count);
			_inputPosition++;
		}
		protected int Match(IntSet set)
		{
			int la = LA(0);
			if (!set.Contains(la))
				Error(set);
			else
				_inputPosition++;
			return la;
		}
		protected int Match(int a)
		{
			int la = LA(0);
			if (la != a)
				Error(IntSet.WithChars(a));
			else
				_inputPosition++;
			return la;
		}
		protected int Match(int a, int b)
		{
			int la = LA(0);
			if (la != a && la != b)
				Error(IntSet.WithChars(a, b));
			else
				_inputPosition++;
			return la;
		}
		protected int Match(int a, int b, int c)
		{
			int la = LA(0);
			if (la != a && la != b && la != c)
				Error(IntSet.WithChars(a, b, c));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchRange(int aLo, int aHi)
		{
			int la = LA(0);
			if ((la < aLo || la > aHi))
				Error(IntSet.WithCharRanges(aLo, aHi));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA(0);
			if ((la < aLo || la > aHi) && (la < bLo || la > bHi))
				Error(IntSet.WithCharRanges(aLo, aHi, bLo, bHi));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchExcept()
		{
			int la = LA(0);
			if (la == -1)
				Error(IntSet.WithoutChars());
			else
				_inputPosition++;
			return la;
		}
		protected int MatchExcept(int a)
		{
			int la = LA(0);
			if (la == -1 || la == a)
				Error(IntSet.WithoutChars(a));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchExcept(int a, int b)
		{
			int la = LA(0);
			if (la == -1 || la == a || la == b)
				Error(IntSet.WithoutChars(a, b));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchExcept(int a, int b, int c)
		{
			int la = LA(0);
			if (la == -1 || la == a || la == b || la == c)
				Error(IntSet.WithoutChars(a, b, c));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchExceptRange(int aLo, int aHi)
		{
			int la = LA(0);
			if (la == -1 || (la >= aLo && la <= aHi))
				Error(IntSet.WithoutCharRanges(aLo, aHi));
			else
				_inputPosition++;
			return la;
		}
		protected int MatchExceptRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA(0);
			if (la == -1 || (la >= aLo && la <= aHi) || (la >= bLo && la <= bHi))
				Error(IntSet.WithoutCharRanges(aLo, aHi, bLo, bHi));
			else
				_inputPosition++;
			return la;
		}
		
		protected virtual void Error(IntSet expected)
		{
			var pos = _source.IndexToLine(_inputPosition);
			Error(string.Format("{0}: Error: '{1}': expected {2}", pos, IntSet.WithChars(LA(0)), expected));
		}
		protected virtual void Error(string message)
		{
			throw new FormatException(message);
		}
		protected virtual void Check(bool expectation)
		{
			if (!expectation)
				throw new Exception("An expected condition was false");
		}
	}
}
