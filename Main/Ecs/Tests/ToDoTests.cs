using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsPrinterAndParserTests
	{
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
