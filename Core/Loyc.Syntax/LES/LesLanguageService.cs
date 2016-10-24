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
	/// LES overview: http://loyc.net/les
	/// </remarks>
	public class Les2LanguageService : IParsingService
	{
		public static readonly Les2LanguageService Value = new Les2LanguageService();

		public override string ToString()
		{
			return "Loyc Expression Syntax v2.0";
		}

		static readonly string[] _fileExtensions = new[] { "les", "les2" };
		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }

		public LNodePrinter Printer
		{
			get { return LesNodePrinter.Printer; }
		}
		public string Print(LNode node, IMessageSink msgs = null, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			Printer(node, sb, msgs ?? MessageSink.Current, mode, indentString, lineSeparator);
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
			return new LesLexer(text, fileName, msgs);
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, ParsingMode inputType = null, bool preserveComments = false)
		{
			var lexer = new WhitespaceFilter(Tokenize(text, fileName, msgs));
			return Parse(lexer, msgs, inputType, preserveComments);
		}
		public IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, ParsingMode inputType = null, bool preserveComments = false)
		{
			return Parse(input.Buffered(), input.SourceFile, msgs, inputType);
		}

		[ThreadStatic]
		static LesParser _parser;

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
				LesParser parser = _parser;
				if (parser == null)
					_parser = parser = new LesParser(input, file, msgs);
				else {
					parser.ErrorSink = msgs;
					parser.Reset(input.AsList(), file);
				}
				if (inputType == ParsingMode.Expressions)
					return parser.ExprList();
				else
					return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			} else {
				var parser = new LesParser(input, file, msgs);
				return parser.Start(new Holder<TokenType>(TokenType.Semicolon)).Buffered();
			}
		}
	}

	/// <summary>Alternate name for Les2LanguageService (will change to Les3LanguageService in the future)</summary>
	public class LesLanguageService : Les2LanguageService { }
}
