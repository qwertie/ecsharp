using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Utilities;
using Loyc.Syntax.Lexing;
using Loyc.Collections;

namespace Loyc.Syntax.Les
{
	/// <summary>The <see cref="Value"/> property provides easy access to the lexer, 
	/// parser and printer for Loyc Expression Syntax (LES).</summary>
	/// <remarks>
	/// LES overview: http://sourceforge.net/apps/mediawiki/loyc/index.php?title=LES
	/// </remarks>
	public class LesLanguageService : ILanguageService
	{
		public static readonly LesLanguageService Value = new LesLanguageService();
		
		public LNodePrinter Printer
		{
			get { return LesNodePrinter.Printer; }
		}
		public string Print(LNode node, Utilities.IMessageSink msgs, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			Printer(node, sb, msgs, mode, indentString, lineSeparator);
			return sb.ToString();
		}
		public bool HasTokenizer
		{
			get { return true; }
		}
		public ILexer Tokenize(ISourceFile file, IMessageSink msgs)
		{
			// TODO support other source file types
			var lexer = new LesLexer((StringCharSourceFile)file,
				(index, msg) => { msgs.Write(MessageSink.Error, file.IndexToLine(index), msg); });
			return new TokensToTree(lexer, true);
		}
		public IEnumerator<LNode> Parse(ISourceFile file, IMessageSink msgs, Symbol inputType = null)
		{
			var lexer = Tokenize(file, msgs);
			return Parse(lexer, msgs, inputType);
		}
		public IEnumerator<LNode> Parse(ILexer input, IMessageSink msgs, Symbol inputType = null)
		{
			return Parse(input.Buffered(), input.Source, msgs, inputType);
		}

		public IEnumerator<LNode> Parse(IListSource<Token> input, ISourceFile file, IMessageSink msgs, Symbol inputType = null)
		{
			var parser = new LesParser(input, file, msgs);
			if (inputType == null || inputType == LanguageService.File || inputType == LanguageService.Stmts)
				return parser.ParseStmtsUntilEnd();
			else
				throw new NotImplementedException("'Exprs' is not implemented");
		}
	}
}
