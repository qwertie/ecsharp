using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.CompilerCore;

namespace Ecs.Parser
{
	using LS = EcsTokenTypes;

	public class EcsParserGenerator : LlpgTests
	{
		TerminalPred Sym(Symbol s) { return Pred.Sym(s); }
		TerminalPred Sym(string s) { return Pred.Sym(GSymbol.Get(s)); }
		public static readonly Symbol _id = GSymbol.Get("id");
		public Pred id { get { return Sym(_id); } }

		public void GenerateParserCode()
		{
			_pg = new LLParserGenerator(new PGCodeGenForSymbolStream());
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == Node.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Okay, so EC# code is pretty much just a bunch of statements...
			// and statements contain expressions.
			var DottedExpr = Rule("ComplexId", id + Star(Sym("#.") + id));
			var StartExpr = Rule("StartExpr", DottedExpr, Start);

			var Stmt = Rule("Stmt", Sym(""), Start); // completed later
			var UsingDirective = Rule("UsingDirective", LS.@using + DottedExpr);
			var UsingStmt = Rule("UsingStmt", Sym(LS.@using) + LS.LParen + StartExpr + LS.RParen + Stmt);
			var Stmts = Rule("Stmts", Star(Stmt), Start);
			Stmt.Pred = UsingDirective | UsingStmt;
		}
	}
}
