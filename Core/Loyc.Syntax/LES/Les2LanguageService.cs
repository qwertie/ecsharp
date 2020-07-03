using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Utilities;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc;

namespace Loyc.Syntax.Les
{
	/// <summary>The <see cref="Value"/> property provides easy access to the lexer, 
	/// parser and printer for Loyc Expression Syntax (LES).</summary>
	/// <remarks>
	/// LES overview: http://loyc.net/les
	/// </remarks>
	public class Les2LanguageService : IParsingService, ILNodePrinter
	{
		public static readonly Les2LanguageService Value = new Les2LanguageService();

		public override string ToString()
		{
			return "Loyc Expression Syntax v2.0";
		}

		static readonly string[] _fileExtensions = new[] { "les", "les2" };
		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }

		void ILNodePrinter.Print(LNode node, StringBuilder target, IMessageSink sink, ParsingMode mode, ILNodePrinterOptions options)
		{
			Print((ILNode)node, target, sink, mode, options);
		}
		public void Print(ILNode node, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			Les2Printer.Print(node, target, sink, mode, options);
		}
		public string Print(ILNode node, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			StringBuilder target = new StringBuilder();
			Print(node, target, sink, mode, options);
			return target.ToString();
		}
		public void Print(IEnumerable<LNode> nodes, StringBuilder target, IMessageSink msgs = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			LNodePrinter.PrintMultiple(this, nodes, target, msgs, mode, options);
		}
		public bool HasTokenizer
		{
			get { return true; }
		}
		public bool CanPreserveComments
		{
			get { return true; }
		}
		public ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options)
		{
			return new Les2Lexer(text, fileName, msgs) {
				SkipValueParsing = options.SurfaceScanOnly,
				SpacesPerTab = options.SpacesPerTab
			};
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options)
		{
			var lexer = Tokenize(text, fileName, msgs, options);
			return Parse(lexer, msgs, options);
		}
		public IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, IParsingOptions options)
		{
			if (options.PreserveComments) {
				var saver = new TriviaSaver(input, (int)TokenType.Newline);
				var results = Parse(saver.Buffered(), input.SourceFile, msgs, options);
				var injector = new StandardTriviaInjector(saver.TriviaList, saver.SourceFile, (int)TokenType.Newline, "/*", "*/", "//");
				return injector.Run(results.GetEnumerator()).Buffered();
			} else {
				var lexer = new WhitespaceFilter(input);
				return Parse(lexer.Buffered(), input.SourceFile, msgs, options);
			}
		}

		[ThreadStatic]
		static Les2Parser _parser;

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
			bool exprMode = options.Mode == ParsingMode.Expressions;
			if (options.Mode == ParsingMode.Expressions || file.Text.TryGet(255).HasValue) {
				Les2Parser parser = _parser;
				if (parser == null)
					_parser = parser = new Les2Parser(input, file, msgs);
				else {
					parser.ErrorSink = msgs;
					parser.Reset(input.AsList(), file);
				}
				if (options.Mode == ParsingMode.Expressions)
					return parser.ExprList();
				else
					return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			} else {
				var parser = new Les2Parser(input, file, msgs);
				return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			}
		}
	}

	/// <summary>Alternate name for Les2LanguageService (will change to Les3LanguageService in the future)</summary>
	public class LesLanguageService : Les2LanguageService { }
}
