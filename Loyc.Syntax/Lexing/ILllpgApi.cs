using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Lexing
{
	/// <summary>For reference purposes, this interface is a list of the non-static 
	/// methods that LLLPG expects to be able to call when it is generating code. 
	/// LLLPG does not actually need lexers and parsers to implement this interface;
	/// they simply need to implement the same set of methods as this interface 
	/// contains.</summary>
	/// <typeparam name="Token">The return value of the Match methods. LLLPG does 
	/// not care and does not need to know what this type is. In lexers, these 
	/// methods should return the character that was matched, and in parsers they 
	/// should return the token that was matched.</typeparam>
	/// <typeparam name="MatchType">The data type of arguments to Match, 
	/// MatchExcept, TryMatch and TryMatchExcept. In lexers, MatchType is always 
	/// int. In parsers, by default, LLLPG generates code as though MatchType is 
	/// same as LaType, but BaseParser uses int instead for performance reasons. 
	/// Consequently, when using BaseParser you need to use the matchType(int) 
	/// option to change MatchType to int.</typeparam>
	/// <typeparam name="LaType">The data type of LA0 and LA(i). This is always int 
	/// in lexers, but in parsers you can use the laType(...) option (documented in 
	/// the previous article) to change this type.</typeparam>
	/// <seealso cref="ILllpgLexerApi{Char}"/>
	public interface ILllpgApi<Token, MatchType, LaType>
	{
		// Note: the set type is expected to contain a Contains(MatchType) method.
		//static HashSet<MatchType> NewSet(params MatchType[] items);
		//static HashSet<MatchType> NewSetOfRanges(params MatchType[] ranges);

		LaType LA0 { get; }
		LaType LA(int i);
		//static const LaType EOF;

		void Error(int lookaheadIndex, string message);

		// Normal matching methods
		void Skip();
		Token MatchAny();
		Token Match(MatchType a);
		Token Match(MatchType a, MatchType b);
		Token Match(MatchType a, MatchType b, MatchType c);
		Token Match(MatchType a, MatchType b, MatchType c, MatchType d);
		Token Match(HashSet<MatchType> set);
		Token MatchExcept();
		Token MatchExcept(MatchType a);
		Token MatchExcept(MatchType a, MatchType b);
		Token MatchExcept(MatchType a, MatchType b, MatchType c);
		Token MatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
		Token MatchExcept(HashSet<MatchType> set);

		// Used to verify and-predicates in the matching stage
		void Check(bool expectation, string expectedDescr);

		// For backtracking (used by generated Try_Xyz() methods)
		//struct SavePosition : IDisposable
		//{
		//	public SavePosition(Lexer lexer, int lookaheadAmt);
		//	public void Dispose();
		//}

		// For recognizers (used by generated Scan_Xyz() methods)
		bool TryMatch(MatchType a);
		bool TryMatch(MatchType a, MatchType b);
		bool TryMatch(MatchType a, MatchType b, MatchType c);
		bool TryMatch(MatchType a, MatchType b, MatchType c, MatchType d);
		bool TryMatch(HashSet<MatchType> set);
		bool TryMatchRange(int aLo, int aHi);
		bool TryMatchRange(int aLo, int aHi, int bLo, int bHi);
		bool TryMatchExcept();
		bool TryMatchExcept(MatchType a);
		bool TryMatchExcept(MatchType a, MatchType b);
		bool TryMatchExcept(MatchType a, MatchType b, MatchType c);
		bool TryMatchExcept(MatchType a, MatchType b, MatchType c, MatchType d);
		bool TryMatchExcept(HashSet<MatchType> set);
		bool TryMatchExceptRange(int aLo, int aHi);
		bool TryMatchExceptRange(int aLo, int aHi, int bLo, int bHi);
	}

	/// <summary>For reference purposes, this interface contains the non-static 
	/// methods that LLLPG expects lexers to implement. LLLPG does not actually 
	/// expect lexers to implement this interface; they simply need to implement 
	/// the same set of methods as this interface contains.</summary>
	/// <typeparam name="Token">The return value of the Match() methods, which is
	/// the input value (character) actually encountered in the stream. This type 
	/// is usually int.</typeparam>
	public interface ILllpgLexerApi<Token> : ILllpgApi<Token, int, int>
	{
		Token MatchRange(int aLo, int aHi);
		Token MatchRange(int aLo, int aHi, int bLo, int bHi);
		Token MatchExceptRange(int aLo, int aHi);
		Token MatchExceptRange(int aLo, int aHi, int bLo, int bHi);
	}
}
