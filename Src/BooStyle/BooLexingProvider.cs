using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;

namespace Loyc.BooStyle
{
	public class BooLexingProvider : ILexingProvider
	{
		public IParseNext<AstNode> NewCoreLexer(ICharSource source)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<AstNode> NewLexer(ICharSource source)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<AstNode> NewPreprocessor(IEnumerable<AstNode> lexer)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public IEnumerable<AstNode> NewTreeParser(IEnumerable<AstNode> preprocessedInput)
		{
			throw new Exception("The method or operation is not implemented.");
		}

		public AstNode MakeTokenTree(IEnumerable<AstNode> preprocessedInput)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}
