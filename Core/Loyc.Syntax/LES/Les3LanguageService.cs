using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	public class Les3LanguageService : IParsingService
	{
		public static readonly Les3LanguageService Value = new Les3LanguageService();

		public override string ToString()
		{
			return "Loyc Expression Syntax v3.0";
		}

		static readonly string[] _fileExtensions = new[] { "les3", "was" };
		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }

		public LNodePrinter Printer
		{
			get { return Les3Printer.Printer; }
		}
		public string Print(LNode node, IMessageSink msgs = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			var sb = new StringBuilder();
			Les3Printer.Print(node, sb, msgs, mode, options);
			return sb.ToString();
		}
		public string Print(IEnumerable<LNode> nodes, IMessageSink msgs = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			var sb = new StringBuilder();
			Les3Printer.Print(nodes.Upcast<ILNode, LNode>(), sb, msgs, mode, options);
			return sb.ToString();
		}
		public bool HasTokenizer
		{
			get { return true; }
		}
		public bool CanPreserveComments
		{
			get { return false; }
		}
		public ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs)
		{
			return new Les3Lexer(text, fileName, msgs);
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, ParsingMode inputType = null, bool preserveComments = true)
		{
			var lexer = Tokenize(text, fileName, msgs);
			return Parse(lexer, msgs, inputType, preserveComments);
		}
		public IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, ParsingMode inputType = null, bool preserveComments = true)
		{
			if (preserveComments) {
				var saver = new TriviaSaver(input, (int)TokenType.Newline);
				var results = Parse(saver.Buffered(), input.SourceFile, msgs, inputType);
				var injector = new StandardTriviaInjector(saver.TriviaList, input.SourceFile, (int)TokenType.Newline, "/*", "*/", "//");
				injector.SLCommentSuffix = @"\\";
				return injector.Run(results.GetEnumerator()).Buffered();
			} else {
				return Parse(new WhitespaceFilter(input).Buffered(), input.SourceFile, msgs, inputType);
			}
		}

		[ThreadStatic]
		static Les3Parser _parser;

		public IListSource<LNode> Parse(IListSource<Token> input, ISourceFile file, IMessageSink msgs, ParsingMode inputType = null)
		{
			// For efficiency we'd prefer to re-use our _parser object, but
			// when parsing lazily, we can't re-use it because another parsing 
			// operation could start before this one is finished. To force 
			// greedy parsing, we can call ParseStmtsGreedy(), but the caller may 
			// prefer lazy parsing, especially if the input is large. As a 
			// compromise I'll check if the source file is larger than a 
			// certain arbitrary size. Also, ParseExprs() is always greedy 
			// so we can always re-use _parser in that case.
			bool exprMode = inputType == ParsingMode.Expressions;
			char _ = '\0';
			if (inputType == ParsingMode.Expressions || file.Text.TryGet(255, ref _)) {
				Les3Parser parser = _parser;
				if (parser == null)
					_parser = parser = new Les3Parser(input.AsList(), file, msgs);
				else {
					parser.ErrorSink = msgs;
					parser.Reset(input.AsList(), file);
				}
				if (inputType == ParsingMode.Expressions)
					return parser.ExprList();
				else
					return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			} else {
				var parser = new Les3Parser(input.AsList(), file, msgs);
				return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			}
		}
	}
}
