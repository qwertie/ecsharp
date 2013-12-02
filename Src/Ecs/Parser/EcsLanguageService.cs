using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Ecs.Parser
{
	/// <summary>The <see cref="Value"/> property provides easy access to the lexer, 
	/// parser and printer for Enhanced C#.</summary>
	/// <remarks>
	/// LES overview: http://sourceforge.net/apps/mediawiki/loyc/index.php?title=LES
	/// </remarks>
	public class EcsLanguageService : IParsingService
	{
		public static readonly EcsLanguageService Value = new EcsLanguageService();
		
		public LNodePrinter Printer
		{
			get { return EcsNodePrinter.Printer; }
		}
		public string Print(LNode node, IMessageSink msgs, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			EcsNodePrinter.Print(node, sb, msgs, mode, indentString, lineSeparator);
			return sb.ToString();
		}
		public bool HasTokenizer
		{
			get { return true; }
		}
		public ILexer Tokenize(ISourceFile file, IMessageSink msgs)
		{
			// TODO support other source file types
			var lexer = new EcsLexer((StringCharSourceFile)file,
				(index, msg) => { msgs.Write(MessageSink.Error, file.IndexToLine(index), msg); });
			return new TokensToTree(lexer, true);
		}
		public IListSource<LNode> Parse(ISourceFile file, IMessageSink msgs, Symbol inputType = null)
		{
			var lexer = Tokenize(file, msgs);
			return Parse(lexer, msgs, inputType);
		}
		public IListSource<LNode> Parse(ILexer input, IMessageSink msgs, Symbol inputType = null)
		{
			return Parse(input.Buffered(), input.File, msgs, inputType);
		}

		[ThreadStatic]
		static EcsParser _parser;

		public IListSource<LNode> Parse(IListSource<Token> input, ISourceFile file, IMessageSink msgs, Symbol inputType = null)
		{
			// We'd prefer to re-use our _parser object for efficiency, but
			// when parsing lazily, we can't use it because another parsing 
			// operation could start before this one is finished. To force 
			// greedy parsing, we can call ParseStmtsGreedy(), but the caller may 
			// prefer lazy parsing, especially if the input is large. As a 
			// compromise I'll check if the source file is larger than a 
			// certain arbitrary size. Also, ParseExprs() is always greedy so...
			bool exprMode = inputType == ParsingService.Exprs;
			char _ = '\0';
			if (inputType == ParsingService.Exprs || file.TryGet(255, ref _)) {
				EcsParser parser = _parser;
				if (parser == null)
					_parser = parser = new EcsParser(input, file, msgs);
				else {
					parser.MessageSink = msgs;
					parser.Reset(input, file);
				}
				if (inputType == ParsingService.Exprs)
					return parser.ParseExprs();
				else
					return parser.ParseStmtsGreedy();
			} else {
				var parser = new EcsParser(input, file, msgs);
				return parser.ParseStmtsLazy().Buffered();
			}
		}
	}
}

