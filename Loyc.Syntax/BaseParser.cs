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
	/// Generator). Note: this is the old (harder to use) base class design. You 
	/// should use <see cref="BaseParserForList{Token, LaType}"/> instead.
	/// </summary>
	public abstract class BaseParser<Token, MatchType> 
		where MatchType : IEquatable<MatchType>
	{
		protected static HashSet<MatchType> NewSet(params MatchType[] items) { return new HashSet<MatchType>(items); }

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

		protected MatchType EOF;
		/// <summary>Returns the value used for EOF (normally 0)</summary>
		protected abstract MatchType EofInt();
		/// <summary>Returns the token type of _lt0 (normally _lt0.TypeInt)</summary>
		protected abstract MatchType LA0Int { get; }
		/// <summary>Returns the token at lookahead i (e.g. <c>Source[InputPosition + i]</c>
		/// if the tokens come from a list called Source) </summary>
		protected abstract Token LT(int i);
		/// <summary>Returns a string representation of the specified token type.
		/// These strings are used in error messages.</summary>
		protected abstract string ToString(MatchType tokenType);

		/// <summary>Converts a lookahead token index to a character index (used 
		/// for error reporting). Returns -1 if unsuccessful.</summary>
		protected virtual int LaIndexToSourcePos(int lookaheadIndex)
		{
			var token = LT(lookaheadIndex) as ISimpleToken<MatchType>;
			if (token == null)
				return -1;
			int charIdx = token.StartIndex;
			if (token.Type.Equals(EOF)) {
				// Our EOF token may not contain the correct character position,
				// so scan backward and look for the final token...
				for (int li = lookaheadIndex; li > lookaheadIndex-100;  li--) {
					var token2 = LT(li) as ISimpleToken<MatchType>;
					if (!token2.Type.Equals(EOF)) {
						charIdx = System.Math.Max(charIdx, token2.StartIndex + 1);
						break;
					}
				}
			}
			return charIdx;
		}

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
			var charIdx = LaIndexToSourcePos(lookaheadIndex);
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
		protected Token Match(HashSet<MatchType> set, bool inverted = false)
		{
			Token lt = _lt0;
			if (set.Contains(LA0Int) == inverted)
				Error(false, set);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(MatchType a)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (!la.Equals(a))
				Error(false, a);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(MatchType a, MatchType b)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (!la.Equals(a) && !la.Equals(b))
				Error(false, a, b);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(MatchType a, MatchType b, MatchType c)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (!la.Equals(a) && !la.Equals(b) && !la.Equals(c))
				Error(false, a, b, c);
			else
				InputPosition++;
			return lt;
		}
		protected Token Match(MatchType a, MatchType b, MatchType c, MatchType d)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (!la.Equals(a) && !la.Equals(b) && !la.Equals(c) && !la.Equals(d))
				Error(false, a, b, c, d);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept()
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (la.Equals(EOF))
				Error(true);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(MatchType a)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (la.Equals(a) || la.Equals(EOF))
				Error(true, a);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(MatchType a, MatchType b)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (la.Equals(a) || la.Equals(b) || la.Equals(EOF))
				Error(true, a, b);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(MatchType a, MatchType b, MatchType c)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (la.Equals(a) || la.Equals(b) || la.Equals(c) || la.Equals(EOF))
				Error(true, a, b, c);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d)
		{
			Token lt = _lt0; MatchType la = LA0Int;
			if (la.Equals(a) || la.Equals(b) || la.Equals(c) || la.Equals(d) || la.Equals(EOF))
				Error(true, a, b, c, d);
			else
				InputPosition++;
			return lt;
		}
		protected Token MatchExcept(HashSet<MatchType> set)
		{
			return Match(set, true);
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
		protected bool TryMatch(HashSet<MatchType> set, bool inverted = false)
		{
			if (set.Contains(LA0Int) == inverted)
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(MatchType a)
		{
			if (!(LA0Int.Equals(a)))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(MatchType a, MatchType b)
		{
			MatchType la = LA0Int;
			if (!la.Equals(a) && !la.Equals(b))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(MatchType a, MatchType b, MatchType c)
		{
			MatchType la = LA0Int;
			if (!la.Equals(a) && !la.Equals(b) && !la.Equals(c))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d)
		{
			MatchType la = LA0Int;
			if (!la.Equals(a) && !la.Equals(b) && !la.Equals(c) && !la.Equals(d))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept()
		{
			if ((LA0Int.Equals(EOF)))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(MatchType a)
		{
			MatchType la = LA0Int;
			if (la.Equals(EOF) || la.Equals(a))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(MatchType a, MatchType b)
		{
			MatchType la = LA0Int;
			if (la.Equals(EOF) || la.Equals(a) || la.Equals(b))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(MatchType a, MatchType b, MatchType c)
		{
			MatchType la = LA0Int;
			if (la.Equals(EOF) || la.Equals(a) || la.Equals(b) || la.Equals(c))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d)
		{
			MatchType la = LA0Int;
			if (la.Equals(EOF) || la.Equals(a) || la.Equals(b) || la.Equals(c) || la.Equals(d))
				return false;
			else
				InputPosition++;
			return true;
		}
		protected bool TryMatchExcept(HashSet<MatchType> set)
		{
			return TryMatch(set, true);
		}

		#endregion

		protected void Error(bool inverted, params MatchType[] expected) { Error(inverted, (IEnumerable<MatchType>)expected); }
		protected virtual void Error(bool inverted, IEnumerable<MatchType> expected)
		{
			Error(0, Localize.From("'{0}': expected {1}", ToString(LA0Int), ToString(inverted, expected)));
		}
		protected virtual string ToString(bool inverted, IEnumerable<MatchType> expected)
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
	/// <summary>
	/// An base class designed for parsers that use LLLPG (Loyc LL(k) Parser 
	/// Generator). Note: this is the old (harder to use) base class. You should 
	/// use <see cref="BaseParserForList{Token, LaType}"/> instead. This class is
	/// now an alias for BaseParser{Token,int}.
	/// </summary>
	public abstract class BaseParser<Token> : BaseParser<Token, Int32>
	{
		protected BaseParser(ISourceFile file = null, int startIndex = 0) : base(file, startIndex) {} 
	}
}
