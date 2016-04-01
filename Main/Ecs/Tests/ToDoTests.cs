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

		[Test(Fails = "Left broken because LLLPG is too slow to analyze the grammar")]
		public void Await()
		{
			Expr("await x ** 2", F.Call(S.Exp, F.Call(await, x), F.Literal(2)));
			Expr("await Foo.x", F.Call(await, F.Dot(Foo, x)));
			Expr("a * await Foo.x", F.Call(S.Mul, a, F.Call(await, F.Dot(Foo, x))));

			Expr("await ++x", F.Call(await, F.Call(S.PreInc, x)));
			Expr("await++ + x", F.Call(S.Add, F.Call(S.PostInc, await), x));
			// Uh-oh, it looks like the parsing of this should depend on whether the
			// enclosing function has the `async` keyword or not. If it does, it 
			// should parse as `await((a).b)`, otherwise it should be `(await(a)).b`.
			// But EC# doesn't change modes in this way, so it's always parsed as
			// `await((a).b)`
			Expr("await(a).b", F.Call(await, F.Dot(F.InParens(a), b)));
			// @await is treated slightly differently, but currently the node Name is 
			// "await" either way. Should it be #await when the @ sign is absent?
			Expr("@await(a).b", F.Dot(F.Call(await, a), b));
		}
		
		[Test(Fails = "Stuff that is intentionally left broken for the time being")]
		public void TODO()
		{
			Stmt("var a = (Foo ? b = c as Foo? : 0);", 
				F.Var(F.Missing, F.Call(S.Assign, a, F.InParens(
					F.Call(S.QuestionMark, Foo,
						F.Call(S.Assign, b, F.Call(S.As, c, F.Of(S.QuestionMark, Foo.Name))), zero)))));
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
