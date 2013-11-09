using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc.Syntax.Lexing
{
	public abstract class BaseLexer<TSource> where TSource : IListSource<char>
	{
		protected static HashSet<int> NewSet(params int[] items) { return new HashSet<int>(items); }
		protected static HashSet<int> NewSetOfRanges(params int[] ranges) 
		{
			var set = new HashSet<int>();
			for (int r = 0; r < ranges.Length; r += 2)
				for (int i = ranges[r]; i <= ranges[r+1]; i++)
					set.Add(i);
			return set;
		}

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
		protected int Match(HashSet<int> set)
		{
			int la = LA0;
			if (!set.Contains(la)) {
				Error(false, set);
			} else
				InputPosition++;
			return la;
		}
		protected int Match(int a)
		{
			int la = LA0;
			if (la != a)
				Error(false, a, a);
			else
				InputPosition++;
			return la;
		}
		protected int Match(int a, int b)
		{
			int la = LA0;
			if (la != a && la != b)
				Error(false, a, a, b, b);
			else
				InputPosition++;
			return la;
		}
		protected int Match(int a, int b, int c)
		{
			int la = LA0;
			if (la != a && la != b && la != c)
				Error(false, a, a, b, b, c, c);
			else
				InputPosition++;
			return la;
		}
		protected int MatchRange(int aLo, int aHi)
		{
			int la = LA0;
			if ((la < aLo || la > aHi))
				Error(false, aLo, aHi);
			else
				InputPosition++;
			return la;
		}
		protected int MatchRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA0;
			if ((la < aLo || la > aHi) && (la < bLo || la > bHi))
				Error(false, aLo, aHi, bLo, bHi);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept()
		{
			int la = LA0;
			if (la == -1)
				Error(true, -1, -1);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(HashSet<int> set)
		{
			int la = LA0;
			if (set.Contains(la))
				Error(true, set);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(int a)
		{
			int la = LA0;
			if (la == -1 || la == a)
				Error(true, a, a);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(int a, int b)
		{
			int la = LA0;
			if (la == -1 || la == a || la == b)
				Error(true, a, a, b, b);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExcept(int a, int b, int c)
		{
			int la = LA0;
			if (la == -1 || la == a || la == b || la == c)
				Error(true, a, a, b, b, c, c);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExceptRange(int aLo, int aHi)
		{
			int la = LA0;
			if (la == -1 || (la >= aLo && la <= aHi))
				Error(true, aLo, aHi);
			else
				InputPosition++;
			return la;
		}
		protected int MatchExceptRange(int aLo, int aHi, int bLo, int bHi)
		{
			int la = LA0;
			if (la == -1 || (la >= aLo && la <= aHi) || (la >= bLo && la <= bHi))
				Error(true, aLo, aHi, bLo, bHi);
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
		protected bool TryMatch(HashSet<int> set)
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
		protected bool TryMatchExcept(HashSet<int> set)
		{
			if (set.Contains(LA0))
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

		protected virtual void Error(bool inverted, params int[] ranges) { Error(inverted, (IList<int>)ranges); }
		protected virtual void Error(bool inverted, IList<int> ranges)
		{
			string rangesDescr = RangesToString(ranges);
			var input = new StringBuilder();
			PrintChar(LA0, input);
			if (inverted)
				Error(InputPosition, Localize.From("{0}: expected a character other than {1}", input, rangesDescr));
			else if (ranges.Count > 2)
				Error(InputPosition, Localize.From("{0}: expected one of {1}", input, rangesDescr));
			else
				Error(InputPosition, Localize.From("{0}: expected {1}", input, rangesDescr));
		}
		protected virtual void Error(bool inverted, HashSet<int> set)
		{
			var array = set.ToArray();
			array.Sort();
			var list = new List<int>();
			int i, j;
			for (i = 0; i < array.Length; i++)
			{
				for (j = i + 1; j < array.Length && array[j] == array[i] + 1; j++) { }
				list.Add(i);
				list.Add(j - 1);
			}
			Error(inverted, list);
		}

		string RangesToString(IList<int> ranges)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < ranges.Count; i += 2)
			{
				if (i != 0)
					sb.Append(' ');
				int lo = ranges[i], hi = ranges[i + 1];
				PrintChar(lo, sb);
				if (hi > lo)
				{
					sb.Append(hi > lo + 1 ? '-' : ' ');
					PrintChar(hi, sb);
				}
			}
			return sb.ToString();
		}
		void PrintChar(int c, StringBuilder sb)
		{
			if (c == -1)
				sb.Append("EOF");
			else if (c >= 0 && c < 0xFFFC) {
				sb.Append('\'');
				G.EscapeCStyle((char)c, sb, EscapeC.Default | EscapeC.SingleQuotes);
				sb.Append('\'');
			} else
				sb.Append(c);
		}

		protected virtual void Check(bool expectation, string expectedDescr = "")
		{
			if (!expectation)
				Error(InputPosition, Localize.From("An expected condition was false: {0}", expectedDescr));
		}
	}
}
