using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsPrinterAndParserTests
	{
		[Test(Fails = "Parser does not yet preserve comments")]
		public void CommentTrivia()
		{
			var stmt = Attr(F.Trivia(S.TriviaMLCommentBefore, "bx"), F.Trivia(S.TriviaMLCommentAfter, "ax"), x);
			Stmt("/*bx*/x; /*ax*/",   stmt);
			Expr("/*bx*/x /*ax*/",    stmt);
			Stmt("x;",               stmt, p => p.OmitComments = true);
			stmt = Attr(F.Trivia(S.TriviaSLCommentBefore, "bx"), F.Trivia(S.TriviaSpaceAfter, "\t\t"), F.Trivia(S.TriviaSLCommentAfter, "ax"), x);
			Stmt("//bx\nx;\t\t//ax", stmt);
			Expr("//bx\nx //ax",     stmt, p => p.OmitSpaceTrivia = true);
			Expr("//bx\nx\t\t//ax",  stmt);
			Stmt("//bx\nx; //ax",    stmt, p => p.OmitSpaceTrivia = true);
			Stmt("x;\t\t",           stmt, p => p.OmitComments = true);
			stmt = 
				Attr(F.Trivia(S.TriviaSLCommentBefore, " a block"), 
					F.Trivia(S.TriviaSLCommentAfter, " end of block"), F.Braces(
					Attr(F.Trivia(S.TriviaSLCommentBefore, " set x to zero"),
						F.Trivia(S.TriviaSpaceAfter, "  "),
						F.Trivia(S.TriviaSLCommentAfter, " x was set to zero"),
						F.Assign(Attr(F.Trivia(S.TriviaMLCommentAfter, "the variable"), x),
									  Attr(F.Trivia(S.TriviaMLCommentAfter, "its new value"), zero)
					))));
			Stmt("// a block\n{\n  // set x to zero\n  x /*the variable*/= 0 /*its new value*/;  // x was set to zero\n} // end of block", stmt);
			stmt = Attr(F.Trivia(S.TriviaSLCommentAfter, " leave loop"), F.Call(S.Break));
			Stmt("break; // leave loop", stmt);
		}

		/* Not planning to implement 'if' clauses, for now
		[Test]
		public void BraceInIfClause()
		{
			// A braced block is not allowed inside an "if" clause. However we 
			// don't have a good way to prevent it.
			var stmt = Attr(F.Call(S.If, F.Call(S.Eq, a, F.Braces(b))),
			                F.Call(S.Namespace, Foo, F._Missing));
			Stmt("namespace Foo if a == @`{}`(b);", stmt);
			stmt = Attr(F.Call(S.If, F.Call(S.Eq, a, StmtStyle(F.List(b)))),
			            F.Call(S.Class, Foo));
			Stmt("class Foo if a == #(b);", stmt);
		}
		 */
	}
}
