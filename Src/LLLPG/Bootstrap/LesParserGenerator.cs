using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Les
{
	using Loyc.LLParserGenerator;

	class LesParserGenerator : LlpgHelpers
	{
		LLParserGenerator _pg;

		public LNode GenerateParserCode()
		{
			_pg = new LLParserGenerator(new GeneralCodeGenHelper());
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == LNode.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Just do whitespace-agnostic LES at first

#if false
			// LES parser

			// Combines the previous precedence floor with that of a prefix operator
			Precedence RP(Precedence prev, Precedence pre) {
				return new Precedence(prev.Lo, prev.Hi, prev.Left, Math.Max(prev.Right, pre.Right));
			}

			LNode Stmt [
				n:=Expr ';' { return n; }
			];

			int*$
			int?$
			int[]

			Expr(context::Precedence)::LNode [
				x::$ = 6;
				x::$ = 
				
			];

			Literal..LNode [
				  t:=(TT.String | TT.SQString | TT.Number | TT.Symbol | TT.OtherLit) { return F.Literal(t.Value); }
				| n:=TokenLiteral { return n; }
			];
			TokenLiteral..LNode [ t:="[]" { return F.Literal(t.Children); } ];
#endif
			return LNode.Missing;
		}
	}
}
