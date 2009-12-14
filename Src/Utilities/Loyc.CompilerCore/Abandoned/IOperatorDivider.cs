using System;
using System.Collections.Generic;
using Loyc.Runtime;

namespace Loyc.CompilerCore.ExprParsing
{
	/// <summary>An interface for classes that can split tokens into substrings. Objects
	/// implementing this interface are used by BasicDividerSource and IOneParser 
	/// classes.</summary>
	/// <remarks>The standard implementation is BasicOperatorDivider.</remarks>
	public interface IOperatorDivider<Token> 
		where Token : ITokenValueAndPos
	{
		/// <summary>Gives the IOperatorDivider an opportunity to build a table of 
		/// operator strings from a list of operators.</summary>
		/// <param name="ops">A list of operators</param>
		void ProcessOperators(IEnumerable<IOneOperator<Token>> ops);
			
		/// <summary>
		/// Returns true if the Divide() method might divide the token into multiple parts 
		/// if it were called, false if it definitely won't.
		/// </summary>
		/// <remarks>An acceptable implementation would be to always return true. 
		/// However, splitting tokens is slightly expensive so this function should 
		/// return false to avoid the overhead.</remarks>
		bool MayDivide(Token token);

		/// <summary>Returns an object that essentially iterates over the possible
		/// interpretations of the token.</summary>
		IOperatorDividerState<Token> Divide(Token token);
	}
	public interface IOperatorDividerState<Token>
		where Token : ITokenValue
	{
		/// <summary>Divides a token into parts according to the known set of
		/// operators. Each time Divide() is called, it either produces a new
		/// interpretation of the token passed to Consider() and returns true,
		/// or it returns false to indicate that no more interpretations are
		/// available.</summary>
		/// <returns>True if result is valid, or false if there are no more
		/// interpretations.</returns>
		/// <remarks>The caller must allocate the list. Divide() calls
		/// result.Clear() before adding a valid interpretation.
		/// 
		/// Typically this method is used to divide :PUNC tokens, but the token
		/// type is not required to be :PUNC.</remarks>
		bool GetNext(List<Token> result);
	}
	
	/// <summary>This is a helper structure for IOperatorDivider.Divide()</summary>
	public struct DividedOperatorPart
	{
		public int Offset;
		public string Substring;
		public DividedOperatorPart(int offset, string substring)
		{
			Offset = offset;
			Substring = substring;
		}
	}
}
