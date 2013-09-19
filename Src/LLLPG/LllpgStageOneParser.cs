using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax.Les;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.LLParserGenerator
{
	using TT = TokenType;
	using S = CodeSymbols;
	using P = LesPrecedence;

	public class LllpgStageOneParser : LesParser
	{
		/// <summary>Parses a token tree into a sequence of LNodes, one per top-
		/// level statement in the input.</summary>
		public static new IEnumerator<LNode> Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			var parser = new LllpgStageOneParser(tokenTree, file, messages);
			return parser.StmtsUntilEnd();
		}
		public LllpgStageOneParser(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages) : base(tokenTree, file, messages)
		{
		}
	}
}
