using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;

namespace Loyc.BooStyle
{
	public class BooLexingProvider : ILexingProvider
	{
		public IParseNext<IToken> NewCoreLexer(ICharSource source)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<IToken> NewLexer(ICharSource source)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<IToken> NewPreprocessor(IEnumerable<IToken> lexer)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<IToken> NewTreeParser(IEnumerable<IToken> preprocessedInput)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
