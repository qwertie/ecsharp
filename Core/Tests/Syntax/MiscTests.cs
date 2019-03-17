using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loyc.Syntax
{
	using S = CodeSymbols;

	// Mostly LNode tests
	[TestFixture]
	public class MiscTests
	{
		[Test]
		public void IsTrivia()
		{
			Assert.IsTrue(S.IsTriviaSymbol(S.TriviaAppendStatement));
			Assert.IsTrue(S.IsTriviaSymbol(S.TriviaSLComment));
			Assert.IsTrue(S.IsTriviaSymbol((Symbol)"#trivia_foo"));
			Assert.IsFalse(S.IsTriviaSymbol((Symbol)"#triviaFoo"));
			Assert.IsTrue(S.IsTriviaSymbol((Symbol)"%Foo"));
			Assert.IsFalse(S.IsTriviaSymbol((Symbol)"'Foo"));
			Assert.IsTrue(S.IsTriviaSymbol((Symbol)"%"));
			Assert.IsFalse(S.IsTriviaSymbol(GSymbol.Empty));
		}
	}
}
