using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;

namespace Loyc.Syntax
{
	public abstract class BaseParser<Token, TType> 
	{
		protected BaseParser() { _eof = EOF; }

		public Token LT0 { get; protected set; }
		public TType LA0 { get { return ToLA(LT0); } }

		private int _inputPosition = 0;
		protected int InputPosition
		{
			get { return _inputPosition; }
			set {
				_inputPosition = value;
				LT0 = LT(0);
			}
		}

		private TType _eof;
		protected abstract TType EOF { get; }
		protected abstract TType LA(int i);
		protected abstract Token LT(int i);
		protected abstract bool LT0Equals(TType b);
		protected abstract TType ToLA(Token lt);
		protected abstract string PositionToString(int inputPosition);

		protected void Skip()
		{
			// Called when prediction already verified the input (and LA(0) is not saved, so we return void)
			Debug.Assert(!LT0Equals(_eof));
			InputPosition++;
		}

		#region Normal matching

		protected Token MatchAny()
		{
			Token lt = LT0;
			InputPosition++;
			return lt;
		}
		protected Token Match(HashSet<TType> set)
		{
			Token lt = LT0;
			if (!set.Contains(ToLA(lt)))
				Error(false, set);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(TType a)
		{
			Token lt = LT0;
			if (!LT0Equals(a))
				Error(false, a);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(TType a, TType b)
		{
			Token lt = LT0;
			if (!LT0Equals(a) && !LT0Equals(b))
				Error(false, a, b);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(TType a, TType b, TType c)
		{
			Token lt = LT0;
			if (!LT0Equals(a) && !LT0Equals(b) && !LT0Equals(c))
				Error(false, a, b, c);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept()
		{
			Token lt = LT0;
			if (LT0Equals(_eof))
				Error(true);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(TType a)
		{
			Token lt = LT0;
			if (LT0Equals(a) || LT0Equals(_eof))
				Error(true, a);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(TType a, TType b)
		{
			Token lt = LT0;
			if (LT0Equals(a) || LT0Equals(b) || LT0Equals(_eof))
				Error(true, a, b);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(TType a, TType b, TType c)
		{
			Token lt = LT0;
			if (LT0Equals(a) || LT0Equals(b) || LT0Equals(c) || LT0Equals(_eof))
				Error(true, a, b, c);
			else
				InputPosition++;
			return lt;
		}

		#endregion

		#region Try-matching

		protected struct SavedPosition : IDisposable
		{
			BaseParser<Token, TType> _parser;
			int _oldPosition;
			public SavedPosition(BaseParser<Token, TType> parser) { _parser = parser; _oldPosition = parser.InputPosition; }
			public void Dispose() { _parser.InputPosition = _oldPosition; }
		}
		protected bool TryMatch(HashSet<TType> set)
		{
			if (!set.Contains(LA0))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(TType a)
		{
			if (!LT0Equals(a))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(TType a, TType b)
		{
			if (!LT0Equals(a) && !LT0Equals(b))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(TType a, TType b, TType c)
		{
			if (!LT0Equals(a) && !LT0Equals(b) && !LT0Equals(c))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept()
		{
			if (LT0Equals(_eof))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(TType a)
		{
			if (LT0Equals(_eof) || LT0Equals(a))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(TType a, TType b)
		{
			if (LT0Equals(_eof) || LT0Equals(a) || LT0Equals(b))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(TType a, TType b, TType c)
		{
			if (LT0Equals(_eof) || LT0Equals(a) || LT0Equals(b) || LT0Equals(c))
				return false;
			else
				InputPosition++;
			return true;
		}

		#endregion

		protected void Error(bool inverted, params TType[] expected) { Error(inverted, (IEnumerable<TType>)expected); }
		protected virtual void Error(bool inverted, IEnumerable<TType> expected)
		{
			Error(InputPosition, Localize.From("Error: '{0}': expected {1}", ToString(LA0), ToString(inverted, expected)));
		}
		protected virtual string ToString(TType item)
		{
			if (item == null || item.Equals(_eof))
				return Localize.From("EOF");
			else
				return item.ToString();
		}
		protected virtual string ToString(bool inverted, IEnumerable<TType> expected)
		{
			int plural = expected.Take(2).Count();
			if (plural == 0)
				return Localize.From(inverted ? "anything" : "nothing");
			else if (inverted)
				return Localize.From("anything except {0}", ToString(false, expected));
			else if (plural == 1)
				return ToString(expected.First());
			else
				return StringExt.Join("|", expected.Select(e => ToString(e)));
		}
		protected virtual void Error(int inputPosition, string message)
		{
			string pos = PositionToString(inputPosition);
			throw new FormatException(pos + ": " + message);
		}
		protected virtual void Check(bool expectation, string expectedDescr = "")
		{
			if (!expectation)
				Error(InputPosition, Localize.From("An expected condition was false: {0}", expectedDescr));
		}
	}
}
