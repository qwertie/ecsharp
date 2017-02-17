using System;
using System.Collections.Generic;
using System.Linq;
using Loyc.MiniTest;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsPrinterAndParserTests
	{
		[Test]
		public void TrivialLinqExpressions()
		{
			var expr = F.Call(S.Linq, F.Call(S.From, F.Call(S.In, x, Foo)), F.Call(S.Select, x));
			Expr("from x in Foo select x", expr);

			expr = F.Call(S.Linq, F.Call(S.From, F.Call(S.In, x, Foo)), OnNewLine(F.Call(S.Select, x)));
			Expr("from x in Foo \nselect x", expr);
		}

		[Test]
		public void CommonLinqClauses()
		{
			var expr = F.Call(S.Linq, 
				F.Call(S.From, F.Call(S.In, F.Var(Foo, x), Foo)),
				F.Call(S.Let, F.Call(S.Assign, a, F.Call(Foo, x))),
				F.Call(S.OrderBy, F.Call(S.Descending, a)),
				F.Call(S.Select, F.Call(S.IndexBracks, x, a)));
			Expr("from Foo x in Foo let a = Foo(x) orderby a descending select x[a]", 
				expr);

			// Ensure LINQ keywords treated as identifiers outside query expressions
			// Notably, there is a cast ambiguity inside query expressions
			expr = F.Call(S.Add, F.Call(S.Cast, F.Id("orderby"), Foo),
			                     F.Call(S.Cast, F.Id("descending"), Foo));
			Stmt("(Foo) orderby + (Foo) descending;", expr);

			expr = F.Call(Foo, F.Call(S.Linq, 
				F.Call(S.From, F.Call(S.In, x, F.InParens(Foo))),
				F.Call(S.OrderBy, F.Call(S.Descending, F.InParens(F.Dot(x, a))), 
				                  F.Call(S.Ascending, F.InParens(F.Dot(x, b)))),
				F.Call(S.Select, F.InParens(x))));
			Stmt("Foo(from x in (Foo) orderby (x.a) descending, (x.b) ascending select (x));", 
				expr);

			expr = F.Var(F.Missing, c, F.Call(S.Linq, 
				F.Call(S.From, F.Call(S.In, F.Var(F.Missing, x), Foo)),
				F.Call(S.OrderBy, F.Call(S.Mul, x, x)),
				F.Call(S.Where, F.Call(S.GT, F.Dot(x, Foo), zero)),
				F.Call(S.Let, F.Call(S.Assign, a, x)),
				F.Call(S.GroupBy, F.Call(S.Assign, x, x), F.Call(S.NullCoalesce, x, x))));
			Stmt("var c = from var x in Foo orderby x * x where x.Foo > 0 let a = x group x = x by x ?? x;", 
				expr);
		}

		[Test]
		public void LinqJoins()
		{
			var expr = F.Call(S.Linq, 
				F.Call(S.From, F.Call(S.In, x, Foo)),
				F.Call(S.Join, F.Call(S.In, F.Var(F.Missing, a), F.Call(Foo, x)), F.Call("#equals", x, F.Call(S.NullCoalesce, a, b))),
				F.Call(S.Select, F.Call(Foo, x, a)));
			Expr("from x in Foo join var a in Foo(x) on x equals a ?? b select Foo(x, a)", expr);

			expr = F.Call(S.Linq, 
				F.Call(S.From, F.Call(S.In, x, Foo)),
				F.Call(S.Join, F.Call(S.In, a, F.InParens(F.Call(Foo, x))), F.Call("#equals", 
				               F.InParens(x), F.Call(S.NullCoalesce, a, b)), F.Call(S.Into, F.Id("g"))),
				F.Call(S.GroupBy, F.Id("g"), F.Dot(x, Foo)));
			Expr("from x in Foo join a in (Foo(x)) on (x) equals a ?? b into g group g by x.Foo", expr);
		}

		[Test]
		public void LinqContinuations()
		{
			var expr = F.Call(S.Linq, 
				F.Call(S.From, F.Call(S.In, x, Foo)),
				F.Call(S.Select, F.Call(Foo, x)),
				F.Call(S.Into, a, 
					F.Call(S.Where, F.Call(S.Eq, a, x)),
					F.Call(S.Select, a)));
			Expr("from x in Foo select Foo(x) into a where a == x select a", expr);

			expr = F.Call(S.Linq,
				F.Call(S.From, F.Call(S.In, x, Foo)),
				F.Call(S.Select, F.Call(Foo, x)),
				F.Call(S.Into, a,
					F.Call(S.Where, F.Call(S.Eq, a, x)),
					F.Call(S.Select, a),
					F.Call(S.Into, b,
						F.Call(S.GroupBy, b, F.Call(Foo, b)))));
			Expr("from x in Foo select Foo(x) into a where a == x select a into b group b by Foo(b)", expr);
		}
	}
}
