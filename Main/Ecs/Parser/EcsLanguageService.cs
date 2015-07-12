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
		static readonly string[] _fileExtensionsNormal = new[] { "ecs", "cs" };
		static readonly string[] _fileExtensionsPlainCs = new[] { "cs", "ecs" };
		
		public static readonly EcsLanguageService Value = new EcsLanguageService(false);
		public static readonly EcsLanguageService WithPlainCSharpPrinter = new EcsLanguageService(true);

		readonly string[] _fileExtensions = _fileExtensionsNormal;
		readonly LNodePrinter _printer = EcsNodePrinter.Printer;
		readonly string _name = "Enhanced C# (alpha)";

		private EcsLanguageService(bool printPlainCSharp)
		{
			if (printPlainCSharp) {
				_printer = EcsNodePrinter.PrintPlainCSharp;
				_fileExtensions = _fileExtensionsPlainCs;
				_name = "Enhanced C# (configured for C# output)";
			}
		}

		public override string ToString()
		{
			return _name;
		}

		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }
		
		public LNodePrinter Printer
		{
			get { return _printer; }
		}
		public string Print(LNode node, IMessageSink msgs = null, object mode = null, string indentString = "\t", string lineSeparator = "\n")
		{
			var sb = new StringBuilder();
			EcsNodePrinter.Printer(node, sb, msgs ?? MessageSink.Current, mode, indentString, lineSeparator);
			return sb.ToString();
		}
		public bool HasTokenizer
		{
			get { return true; }
		}
		public ILexer<Token> Tokenize(ICharSource text, string fileName, IMessageSink msgs)
		{
			return new EcsLexer(text, fileName, msgs);
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, Symbol inputType = null)
		{
			var lexer = Tokenize(text, fileName, msgs);
			return Parse(lexer, msgs, inputType);
		}
		public IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, Symbol inputType = null)
		{
			var preprocessed = new EcsPreprocessor(input);
			var treeified = new TokensToTree(preprocessed, false);
			return Parse(treeified.Buffered(), input.SourceFile, msgs, inputType);
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
					parser.ErrorSink = msgs ?? MessageSink.Current;
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

