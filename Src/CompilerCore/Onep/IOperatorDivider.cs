using System;
using System.Collections.Generic;
using Loyc.Runtime;

namespace Loyc.CompilerCore.Onep
{
	/// <summary>An interface for classes that can split tokens into substrings. Objects
	/// implementing this interface are used by BasicDividerSource and IOneParser 
	/// classes.</summary>
	/// <remarks>The standard implementation is BasicOperatorDivider.</remarks>
	public interface IOperatorDivider
	{
		/// <summary>Gives the IOperatorDivider an opportunity to build a table of 
		/// operator strings from a list of operators.</summary>
		/// <param name="ops">A list of operators</param>
		void ProcessOperators<Expr, Token>(IEnumerable<IOneOperator<Expr, Token>> ops)
			where Token : ITokenValue;
			
		/// <summary>
		/// Returns true if the Divide() method might divide the token into multiple parts 
		/// if it were called.
		/// </summary>
		/// <remarks>An acceptable implementation would be to always return true. 
		/// However, splitting tokens is slightly expensive so this function should return
		/// false to avoid the overhead.</remarks>
		bool MayDivide(ITokenValue token);
		
		/// <summary>Divides a token into parts according to the known set of
		/// operators, supplying each interpretation via the returned enumerator. 
		/// A divider may (or may not) be able to supply multiple interpretations; 
		/// if it can, then the interpretations will be supplied in order of 
		/// priority.</summary>
		/// <returns>An enumerator that produces the sub-tokens.</returns>
		/// <remarks>The caller must check DividedOperatorPart.Offset to learn 
		/// when a new interpretation is starting: the first part of each 
		/// interpretation has an Offset of 0.
		/// 
		/// Typically this method is used to divide PUNC tokens, but the token
		/// type is not required to be PUNC.</remarks>
		IEnumerator<DividedOperatorPart> Divide(ITokenValue token);
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
