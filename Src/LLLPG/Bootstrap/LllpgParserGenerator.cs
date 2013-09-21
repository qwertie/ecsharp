using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;

namespace Loyc.LLParserGenerator
{
	/// <summary>Slated for deletion. Had been planning to use LLLPG to generate a parser that understands LLLPG's own input language.</summary>
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


			// a.b = c.d(x)

			Atom @[
				(	t:=(TT.Id|TT.String|TT.Number|TT.OtherLit)
					
				|	&{Is(\LI,"greedy","nongreedy")} target:=TT.Id)? 
					p:=TT.LParen TT.RParen
					{Down(p); return Up(F.Call((Symbol)target.Value, Expr()));})
			];
			DottedExpr @[
				Atom (TT.Dot Atom)* (TT.LParen TT.RParen)?
			];
			TermOrNonTerm @[
				DottedExpr (TT.Assignment DottedExpr)?
			];
			AndPred @[
				(&{Is(\LI,"&","&!")} t:=TT.Operator) (&{Is(\LI,"!")} TT.Operator)?
				(	TermOrNonTerm
				|	TT.LBrace TT.RBrace
				)
			];
			Expr1 @[
				TermOrNonTerm | AndPred
			];
			Expr2 @[
				Expr1 (&{Is(\LI,"*","?","+")} TT.Operator)*
			];
			CodeBlock @[
				LBrace RBrace
			];
			Expr3 @[
				(Expr2 | CodeBlock)*
			];
			Expr4 @[
				Expr3 (&{Is(\LI,"=>")} TT.Operator Expr3)?
			];
			Expr5 @[
				(&{Is(\LI,"default")} TT.Id)? Expr4
			];
			Expr @[
				Expr5 (&{Is(\LI,"|","/")} TT.Operator Expr5)*
			];
			

#endif

			return LNode.Missing;
		}
	}
}
