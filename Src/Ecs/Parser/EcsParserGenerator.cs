using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.CompilerCore;
using Node = Loyc.Syntax.LNode;

namespace Ecs.Parser
{
	using LS = TokenType;

	public class EcsParserGenerator : LlpgHelpers
	{
		public static readonly Symbol _id = GSymbol.Get("id");
		public Pred id { get { return Sym(_id); } }

		LLParserGenerator _pg;

		public void GenerateParserCode()
		{
			_pg = new LLParserGenerator(new PGCodeGenForSymbolStream());
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == Node.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// FUTURE IDEA for simple "rewrite rules":
			// Look for \(...) inside code blocks, and automatically do replacements...
			//
			// rule PrefixExpr() ==> #[
			//     op:=('\'|'.'|'-'|'+'|'!'|'~'|Inc|Dec) PrefixExpr -> { Call(op, \PrefixExpr) }
			//   | id                                               -> { (Node)id }
			// ];
			// rule DottedExpr ==> #[
			//   PrefixExpr ('.' PrefixExpr)+ 
			//   { Call(\'.', \[PrefixExpr+]) }
			// ];
			// ---means---
			// rule PrefixExpr() ==> #[
			//     op:=('\'|'.'|'-'|'+'|'!'|'~'|Inc|Dec) p0:=PrefixExpr { Call(op, p0) }
			//   | i0:=id                                               { (Node)i0 }
			// ];
			// rule DottedExpr ==> #[
			//   { InternalList<int> p0 = InternalList<int>.Empty; }
			//   p0+=PrefixExpr (c0:='.' p0+=PrefixExpr)+
			//   { Call(c0, p0) }
			// ];
			
			// Dictionary.[int, string]    // Nemerle style
			// Dictionary!(int, string)    // D-style
			// Dictionary(of int, string)  // VB-style
			// Dictionary<int, string>     // C# style


			// Okay, so EC# code is pretty much just a bunch of statements...
			// and statements contain expressions.
			var StartExpr = Rule("StartExpr", null, Start);
			var Parens = Rule("Parens", Sym(LS.RParen) + LS.RParen);
			// id | \(id | '(' ')' )
			var IdPart = Rule("IdPart", Stmt("LNode n") +
				SetVar("id", id) + Stmt("n = F.Symbol(id)") 
				| Sym(@"\") + (id | Parens) + Stmt(""));
			var ComplexId = Rule("ComplexId", IdPart + Star(Sym("#.") + IdPart));
			
			StartExpr.Pred = ComplexId;

			var stmt = Rule("Stmt", Sym(""), Start); // completed later
			var UsingDirective = Rule("UsingDirective", LS.@using + ComplexId);
			var UsingStmt = Rule("UsingStmt", Sym(LS.@using) + LS.LParen + StartExpr + LS.RParen + stmt);
			var Stmts = Rule("Stmts", Star(stmt), Start);
			stmt.Pred = UsingDirective | UsingStmt;
		}
	}
}
