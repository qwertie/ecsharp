using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;

namespace Loyc.LLParserGenerator
{
	/// <summary>Uses LLLPG to generate a parser that understands LLLPG's own input language.</summary>
	class LllpgParserGenerator : LlpgHelpers
	{
		LLParserGenerator _pg;

		public LNode GenerateParserCode()
		{
			_pg = new LLParserGenerator(new GeneralCodeGenHelper("TK", true) { MatchType = F.Int32 });
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == LNode.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

#if false

			Atom @[
				TT.Id|TT.String|TT.Number|TT.OtherLit
			];
			Term @[
				//(TT.Id TT.Assignment)? 
				Atom (TT.Dot Atom)* (TT.LParen TT.RParen)?
			];
			Expr0 @[
				Term ((&{Is(\LI,'|')||Is(\LI,'/')} TT.Operator)?
				Term 
			];

#endif

			return LNode.Missing;
		}
	}
}
