using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;

namespace Loyc.BooStyle
{
	public class BooLexingProvider : ILexingProvider
	{
		#region ILexingProvider Members

		public IParseNext<AstNode> NewCoreLexer(ISourceFile source)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<AstNode> NewLexer(ISourceFile source)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<AstNode> NewLexer(IParseNext<AstNode> coreLexer)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<AstNode> NewPreprocessor(IEnumerable<AstNode> lexer)
		{
			throw new NotImplementedException();
		}

		public AstNode MakeTokenTree(IEnumerable<AstNode> preprocessedInput)
		{
			throw new NotImplementedException();
		}

		public AstNode MakeTokenTree(ISourceFile charSource)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
