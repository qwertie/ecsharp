using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.LLParserGenerator;

namespace Loyc.Syntax.Lexing
{
	public abstract class BaseLexer<TSource> where TSource : IListSource<char>
	{
		protected BaseLexer(TSource input) { CharSource = input; }

		public int LA0 { get; private set; }

		private int _inputPosition = 0;
		public int InputPosition
		{
			get { return _inputPosition; }
			protected set {
				_inputPosition = value;
				if (_source != null)
					LA0 = LA(0);
			}
		}

		private TSource _source;
		protected TSource CharSource
		{
			get { return _source; }
			set {
				_source = value;
				if (_source != null)
					LA0 = LA(0);
			}
		}

		protected abstract void Error(int inputPosition, string message);

		protected int LA(int i)
		{
			bool fail = false;
			char result = _source.TryGet(_inputPosition + i, ref fail);
			return fail ? -1 : result;
		}

		protected void Skip()
		{
			// Called when prediction already verified the input (and LA(0) is not saved)
			Debug.Assert(_inputPosition < _source.Count);
			InputPosition++;
		}

		protected int _lineStartAt;
		protected int _lineNumber = 1;
		/// <summary>Current line number. Starts at 1 for the first line, unless derived class changes it.</summary>
		public int LineNumber { get { return _lineNumber; } }

		/// <summary>The lexer should call this method, which updates _lineStartAt 
		/// and _lineNumber, each time it encounters a newline, even inside 
		/// comments and strings.</summary>
		protected virtual void AfterNewline()
		{
			_lineStartAt = InputPosition;
			_lineNumber++;
		}

		/// <summary>Default newline parser that matches '\n' or '\r' unconditionally.</summary>
		/// <remarks>
		/// You can use this implementation in an LLLPG lexer with "extern", like so:
		/// <c>extern rule Newline @[ '\r' + '\n'? | '\n' ];</c>
		/// By using this implementation everywhere in the grammar in which a 
		/// newline is allowed (even inside comments and strings), you can ensure
		/// that <see cref="AfterNewline()"/> is called, so that the line number
		/// is updated properly.
		/// </remarks>
		protected void Newline()
		{
			int la0;
			la0 = LA0;
			if (la0 == '\r') {
				Skip();
				for (;;) {
					la0 = LA0;
					if (la0 == '\r')
						Skip();
					else
						break;
				}
				la0 = LA0;
				if (la0 == '\n')
					Skip();
			} else
				Match('\n');
			AfterNewline();
		}

		#region Normal matching

		protected int MatchAny()
		{
			int la = LA0;
			InputPosition++;
			return la;
		}
		protected int Match(IntSet set)
		{
			int la = LA0;
			if (!set.Contains(la))
				Error(set);
			else
				InputPosition++;
			return la;
		}
		protected int Match(int a)
		{
			int la = LA0;
			if (la != a)
				Error(IntSet.WithChars(a));
			else
				InputPosition++;
			return la;
		}
		protected int Match(int a, int b)
		{
			int la = LA0;
			if (la != a && la != b)
				Error(IntSet.WithChars(a, b));
			else
				InputPosition++;
			return la;
		}
		protected int Match(int a, int b, int c)
		{
			int la = LA0;
			if (la != a && la != b && la != c)
				Error(IntSet.WithChars(a, b, c));
			else
				InputPosition++;
			return la;
		}
		protected int MatchRange(int aLo, int aHi)
		{
			int la = LA0;
			if ((la < aLo || la > aHi))
				Error(IntSet.WithCharRanges(aLo, aHi));
			else
				InputPosition++;
			return la;
		}
		protected int MatchRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA0;
			if ((la < aLo || la > aHi) && (la < bLo || la > bHi))
				Error(IntSet.WithCharRanges(aLo, aHi, bLo, bHi));
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept()
		{
			int la = LA0;
			if (la == -1)
				Error(IntSet.WithoutChars());
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(int a)
		{
			int la = LA0;
			if (la == -1 || la == a)
				Error(IntSet.WithoutChars(a));
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(int a, int b)
		{
			int la = LA0;
			if (la == -1 || la == a || la == b)
				Error(IntSet.WithoutChars(a, b));
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(int a, int b, int c)
		{
			int la = LA0;
			if (la == -1 || la == a || la == b || la == c)
				Error(IntSet.WithoutChars(a, b, c));
			else
				InputPosition++;
			return la;
		}
		protected int MatchExceptRange(int aLo, int aHi)
		{
			int la = LA0;
			if (la == -1 || (la >= aLo && la <= aHi))
				Error(IntSet.WithoutCharRanges(aLo, aHi));
			else
				InputPosition++;
			return la;
		}
		protected int MatchExceptRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA0;
			if (la == -1 || (la >= aLo && la <= aHi) || (la >= bLo && la <= bHi))
				Error(IntSet.WithoutCharRanges(aLo, aHi, bLo, bHi));
			else
				InputPosition++;
			return la;
		}

		#endregion

		#region Try-matching

		protected struct SavePosition : IDisposable
		{
			BaseLexer<TSource> _lexer;
			int _oldPosition;
			public SavePosition(BaseLexer<TSource> lexer, int lookaheadAmt)
				{ _lexer = lexer; _oldPosition = lexer.InputPosition; lexer.InputPosition += lookaheadAmt; }
			public void Dispose() { _lexer.InputPosition = _oldPosition; }
		}
		protected bool TryMatch(IntSet set)
		{
			if (!set.Contains(LA0))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(int a)
		{
			if (LA0 != a)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(int a, int b)
		{
			int la = LA0;
			if (la != a && la != b)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(int a, int b, int c)
		{
			int la = LA0;
			if (la != a && la != b && la != c)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchRange(int aLo, int aHi)
		{
			int la = LA0;
			if (la < aLo || la > aHi)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA0;
			if ((la < aLo || la > aHi) && (la < bLo || la > bHi))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept()
		{
			if (LA0 == -1)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(int a)
		{
			int la = LA0;
			if (la == -1 || la == a)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(int a, int b)
		{
			int la = LA0;
			if (la == -1 || la == a || la == b)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(int a, int b, int c)
		{
			int la = LA0;
			if (la == -1 || la == a || la == b || la == c)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExceptRange(int aLo, int aHi)
		{
			int la = LA0;
			if (la == -1 || (la >= aLo && la <= aHi))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA0;
			if (la == -1 || (la >= aLo && la <= aHi) || (la >= bLo && la <= bHi))
				return false;
			else
				InputPosition++;
			return true;
		}

		#endregion

		protected virtual void Error(IntSet expected)
		{
			Error(InputPosition, Localize.From("Error: '{0}': expected {1}", IntSet.WithChars(LA(0)), expected));
		}
		protected virtual void Check(bool expectation, string expectedDescr = "")
		{
			if (!expectation)
				Error(InputPosition, Localize.From("An expected condition was false: {0}", expectedDescr));
		}
	}
}
