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
	/// EC# overview: https://sourceforge.net/apps/mediawiki/loyc/index.php?title=Ecs
	/// </remarks>
	public class EcsLanguageService : IParsingService
	{
		public static readonly EcsLanguageService Value = new EcsLanguageService();

		public override string ToString()
		{
			return "Enhanced C# (alpha)";
		}

		static readonly string[] _fileExtensions = new[] { "ecs", "cs" };
		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }
		
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
		public ILexer Tokenize(ICharSource text, string fileName, IMessageSink msgs)
		{
			var lexer = new EcsLexer(text, fileName, msgs);
			return new TokensToTree(lexer, true);
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, Symbol inputType = null)
		{
			var lexer = Tokenize(text, fileName, msgs);
			return Parse(lexer, msgs, inputType);
		}
		public IListSource<LNode> Parse(ILexer input, IMessageSink msgs, Symbol inputType = null)
		{
			return Parse(input.Buffered(), input.SourceFile, msgs, inputType);
		}

		[ThreadStatic]
		static EcsParser _parser;

		public IListSource<LNode> Parse(IListSource<Token> input, ISourceFile file, IMessageSink msgs, Symbol inputType = null)
		{
			// For efficiency we'd prefer to re-use our _parser object, but
			// when parsing lazily, we can't re-use it because another parsing 
			// operation could start before this one is finished. To force 
			// greedy parsing, we can call ParseStmtsGreedy(), but the caller may 
			// prefer lazy parsing, especially if the input is large. As a 
			// compromise I'll check if the source file is larger than a 
			// certain arbitrary size. Also, ParseExprs() is always greedy 
			// so we can always re-use _parser in that case.
			bool exprMode = inputType == ParsingService.Exprs;
			char _ = '\0';
			if (inputType == ParsingService.Exprs || file.Text.TryGet(255, ref _)) {
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

