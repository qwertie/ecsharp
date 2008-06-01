using System;
using System.Collections.Generic;
using System.Text;
using Loyc.Runtime;

namespace Loyc.CompilerCore
{
	static class LoycG
	{
		/// <summary>Adds a keyword to a dictionary.</summary>
		/// <returns>true if keyword was added, false if it was already defined.</returns>
		/// <remarks>Does not check that the keyword is a valid identifier.</remarks>
		public static bool AddKeyword(IDictionary<string, Symbol> dic, string keyword)
		{
			if (dic.ContainsKey(keyword))
				return false;
			dic.Add(keyword, Symbol.Get("_" + keyword));
			return true;
		}
		/// <summary>Casts IEnumerable to ISimpleSource2, or, if that doesn't 
		/// work, creates and returns a new EnumerableSource that wraps around 
		/// the IEnumerable. 
		/// </summary>
		public static ISimpleSource2<IToken> EnumerableToSource(IEnumerable<IToken> e)
		{
			ISimpleSource2<IToken> s = e as ISimpleSource2<IToken>;
			if (s != null)
				return s;
			return new EnumerableSource<IToken>(e);
		}
	}
}
