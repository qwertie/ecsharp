using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Collections;
using Loyc.Syntax.Lexing;

namespace Loyc.Syntax.Les
{
	public class Les3LanguageService : IParsingService, ILNodePrinter
	{
		public static readonly Les3LanguageService Value = new Les3LanguageService();

		public override string ToString()
		{
			return "Loyc Expression Syntax v3.0";
		}

		static readonly string[] _fileExtensions = new[] { "les3" };
		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }

		void ILNodePrinter.Print(LNode node, StringBuilder target, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options)
		{
			Print((ILNode)node, target, sink, mode, options);
		}
		void ILNodePrinter.Print(IEnumerable<LNode> nodes, StringBuilder target, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options)
		{
			Print(nodes, target, sink, mode, options);
		}
		public void Print(ILNode node, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			CheckParam.IsNotNull("target", target);
			var p = new Les3Printer(target, sink, options);
			p.Print(node);
		}
		public string Print(ILNode node, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			StringBuilder target = new StringBuilder();
			Print(node, target, sink, mode, options);
			return target.ToString();
		}
		public void Print(IEnumerable<ILNode> nodes, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			CheckParam.IsNotNull("target", target);
			var p = new Les3Printer(target, sink, options);
			p.Print(nodes);
		}

		public bool HasTokenizer
		{
			get { return true; }
		}
		public bool CanPreserveComments
		{
			get { return false; }
		}
		public ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options)
		{
			return new Les3Lexer(text, fileName, msgs) { SpacesPerTab = options.SpacesPerTab };
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options)
		{
			var lexer = Tokenize(text, fileName, msgs, options);
			return Parse(lexer, msgs, options);
		}
		public IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, IParsingOptions options)
		{
			if (options.PreserveComments) {
				// Filter out whitespace, including some newlines (those directly inside square brackets or parentheses)
				var saver = new TriviaSaver(input, (int)TokenType.Newline);
				var results = Parse(saver.Buffered(), input.SourceFile, msgs, options);
				var injector = new StandardTriviaInjector(saver.TriviaList, input.SourceFile, (int)TokenType.Newline, "/*", "*/", "//", options.Mode != ParsingMode.Expressions);
				injector.SLCommentSuffix = @"\\";
				return injector.Run(results.GetEnumerator()).Buffered();
			} else {
				return Parse(new WhitespaceFilter(input).Buffered(), input.SourceFile, msgs, options);
			}
		}

		[ThreadStatic]
		static Les3Parser _parser;

		public IListSource<LNode> Parse(IListSource<Token> input, ISourceFile file, IMessageSink msgs, IParsingOptions options)
		{
			// For efficiency we'd prefer to re-use our _parser object, but
			// when parsing lazily, we can't re-use it because another parsing 
			// operation could start before this one is finished. To force 
			// greedy parsing, we can call ParseStmtsGreedy(), but the caller may 
			// prefer lazy parsing, especially if the input is large. As a 
			// compromise I'll check if the source file is larger than a 
			// certain arbitrary size. Also, ParseExprs() is always greedy 
			// so we can always re-use _parser in that case.
			if (options.Mode == ParsingMode.Expressions || file.Text.TryGet(255).HasValue) {
				Les3Parser parser = _parser;
				if (parser == null)
					_parser = parser = new Les3Parser(input.AsList(), file, msgs, options);
				else {
					parser.ErrorSink = msgs;
					parser.Reset(input.AsList(), file, options);
				}
				if (options.Mode == ParsingMode.Expressions)
					return parser.Start(new Holder<TokenType>(default(TokenType))).Buffered();
				else
					return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			} else {
				var parser = new Les3Parser(input.AsList(), file, msgs, options);
				return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			}
		}
	}
}
