using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax
{
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator). Note: this is the old (harder to use) base class. You should 
	/// use <see cref="BaseParserForList{Token}"/> instead.
	/// </summary>
	public abstract class BaseParser<Token>
	{
		protected static HashSet<int> NewSet(params int[] items) { return new HashSet<int>(items); }
		protected static HashSet<int> NewSet<T>(params T[] items) where T:IConvertible { return new HashSet<int>(items.Select(t => t.ToInt32(null))); }

		protected BaseParser(ISourceFile file = null, int startIndex = 0) { 
			EOF = EofInt();
			_sourceFile = file;
			_inputPosition = startIndex;
		}

		/// <summary>The <see cref="ISourceFile"/> object that was provided to the constructor, if any.</summary>
		protected ISourceFile SourceFile { get { return _sourceFile; } }
		protected ISourceFile _sourceFile;

		protected Token _lt0;
		/// <summary>Next token to parse (cached; is set to LT(0) whenever InputPosition is changed).</summary>
		public Token LT0 { [DebuggerStepThrough] get { return _lt0; } }

		protected int _inputPosition;
		/// <summary>Current position of the next token to be parsed.</summary>
		protected int InputPosition
		{
			[DebuggerStepThrough]
			get { return _inputPosition; }
			set {
				_inputPosition = value;
				_lt0 = LT(0);
			}
		}

		private Int32 EOF;
		/// <summary>Returns the value used for EOF (normally 0)</summary>
		protected abstract Int32 EofInt();
		/// <summary>Returns the token type of _lt0 (normally _lt0.TypeInt)</summary>
		protected abstract Int32 LA0Int { get; }
		/// <summary>Returns the token at lookahead i (e.g. <c>Source[InputPosition + i]</c>
		/// if the tokens come from a list called Source) </summary>
		protected abstract Token LT(int i);
		/// <summary>Returns a string representation of the specified token type.
		/// These strings are used in error messages.</summary>
		protected abstract string ToString(Int32 tokenType);

		/// <summary>Records an error or throws an exception.</summary>
		/// <param name="lookaheadIndex">Location of the error relative to the
		/// current <c>InputPosition</c>. When called by BaseParser, lookaheadIndex 
		/// is always equal to 0.</param>
		/// <remarks>
		/// The default implementation throws a <see cref="FormatException"/>.
		/// When overriding this method, you can convert the lookaheadIndex
		/// to a <see cref="SourcePos"/> using the expression
		/// <c>SourceFile.IndexToLine(LT(lookaheadIndex).StartIndex)</c>. This only
		/// works if an <c>ISourceFile</c> object was provided to the constructor of 
		/// this class, and <c>Token</c> implements <see cref="ISimpleToken"/>.
		/// </remarks>
		protected virtual void Error(int lookaheadIndex, string message)
		{
			var token = LT(lookaheadIndex) as ISimpleToken;
			if (token == null)
				throw new FormatException(message);
			int charIdx = token.StartIndex;
			if (SourceFile == null)
				throw new FormatException(string.Format("at [{0}]: {1}", charIdx, message));
			else
				throw new FormatException(SourceFile.IndexToLine(charIdx) + ": " + message);
		}
		protected virtual void Error(int lookaheadIndex, string format, params object[] args)
		{
			string msg = Localize.From(format, args);
			Error(lookaheadIndex, msg);
		}

		protected void Skip()
		{
			// Called when prediction already verified the input (and LA(0) is not saved, so we return void)
			InputPosition++;
		}

		#region Normal matching

		protected Token MatchAny()
		{
			Token lt = _lt0;
			InputPosition++;
			return lt;
		}
		protected Token Match(HashSet<Int32> set, bool inverted = false)
		{
			Token lt = _lt0;
			if (set.Contains(LA0Int) == inverted)
				Error(false, set);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(Int32 a)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if (!(la == a))
				Error(false, a);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(Int32 a, Int32 b)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if (!(la == a) && !(la == b))
				Error(false, a, b);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(Int32 a, Int32 b, Int32 c)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if (!(la == a) && !(la == b) && !(la == c))
				Error(false, a, b, c);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(Int32 a, Int32 b, Int32 c, Int32 d)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if (!(la == a) && !(la == b) && !(la == c) && !(la == d))
				Error(false, a, b, c, d);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept()
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if ((la == EOF))
				Error(true);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(Int32 a)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if ((la == a) || (la == EOF))
				Error(true, a);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(Int32 a, Int32 b)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if ((la == a) || (la == b) || (la == EOF))
				Error(true, a, b);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(Int32 a, Int32 b, Int32 c)
		{
			Token lt = _lt0; Int32 la = LA0Int;
			if ((la == a) || (la == b) || (la == c) || (la == EOF))
				Error(true, a, b, c);
			else
				InputPosition++;
			return lt;
		}

		#endregion

		#region Try-matching

		/// <summary>A helper class used by LLLPG for backtracking.</summary>
		protected struct SavePosition : IDisposable
		{
			BaseParser<Token> _parser;
			int _oldPosition;
			public SavePosition(BaseParser<Token> parser, int lookaheadAmt)
				{ _parser = parser; _oldPosition = parser.InputPosition; parser.InputPosition += lookaheadAmt; }
			public void Dispose() { _parser.InputPosition = _oldPosition; }
		}
		protected bool TryMatch(HashSet<Int32> set, bool inverted = false)
		{
			if (set.Contains(LA0Int) == inverted)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(Int32 a)
		{
			if (!(LA0Int == a))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(Int32 a, Int32 b)
		{
			Int32 la = LA0Int;
			if (!(la == a) && !(la == b))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(Int32 a, Int32 b, Int32 c)
		{
			Int32 la = LA0Int;
			if (!(la == a) && !(la == b) && !(la == c))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(Int32 a, Int32 b, Int32 c, Int32 d)
		{
			Int32 la = LA0Int;
			if (!(la == a) && !(la == b) && !(la == c) && !(la == d))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept()
		{
			if ((LA0Int == EOF))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(Int32 a)
		{
			Int32 la = LA0Int;
			if ((la == EOF) || (la == a))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(Int32 a, Int32 b)
		{
			Int32 la = LA0Int;
			if ((la == EOF) || (la == a) || (la == b))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(Int32 a, Int32 b, Int32 c)
		{
			Int32 la = LA0Int;
			if ((la == EOF) || (la == a) || (la == b) || (la == c))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(Int32 a, Int32 b, Int32 c, Int32 d)
		{
			Int32 la = LA0Int;
			if ((la == EOF) || (la == a) || (la == b) || (la == c) || (la == d))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(HashSet<Int32> set)
		{
			return TryMatch(set, true);
		}

		#endregion

		protected void Error(bool inverted, params Int32[] expected) { Error(inverted, (IEnumerable<Int32>)expected); }
		protected virtual void Error(bool inverted, IEnumerable<Int32> expected)
		{
			Error(0, Localize.From("'{0}': expected {1}", ToString(LA0Int), ToString(inverted, expected)));
		}
		protected virtual string ToString(bool inverted, IEnumerable<Int32> expected)
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
		protected virtual void Check(bool expectation, string expectedDescr = "")
		{
			if (!expectation)
				Error(0, Localize.From("An expected condition was false: {0}", expectedDescr));
		}
	}
}
