using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Syntax;
using S = Loyc.Syntax.CodeSymbols;

namespace Loyc.Ecs.Tests
{
	partial class EcsParserTests
	{
		[Test]
		public void TrivialLinqExpression()
		{
			var expr = F.Call(S.Linq, F.Call(S.From, F.Call(S.In, x, Foo)), F.Call(S.Select, x));
			Expr("from x in Foo select x", expr);
		}

	}
}
