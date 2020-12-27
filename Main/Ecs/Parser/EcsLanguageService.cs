using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Collections;
using Loyc.Syntax.Lexing;
using Loyc.Ecs.Parser;

/// <summary>Classes related to Enhanced C# (mostly found in Loyc.Ecs.dll).</summary>
namespace Loyc.Ecs
{
	/// <summary>The <see cref="Value"/> property provides easy access to the lexer, 
	/// parser and printer for Enhanced C#.</summary>
	/// <remarks>
	/// EC# overview: https://ecsharp.net
	/// </remarks>
	public class EcsLanguageService : IParsingService, ILNodePrinter
	{
		static readonly string[] _fileExtensionsNormal = new[] { "ecs", "cs" };
		static readonly string[] _fileExtensionsPlainCs = new[] { "cs", "ecs" };
		
		public static readonly EcsLanguageService Value = new EcsLanguageService(false);
		public static readonly EcsLanguageService WithPlainCSharpPrinter = new EcsLanguageService(true);

		readonly string[] _fileExtensions = _fileExtensionsNormal;
		readonly bool _usePlainCsPrinter;
		readonly string _name = "Enhanced C#";

		private EcsLanguageService(bool usePlainCsPrinter)
		{
			if (_usePlainCsPrinter = usePlainCsPrinter) {
				_fileExtensions = _fileExtensionsPlainCs;
				_name = "Enhanced C# (configured for C# output)";
			}
		}

		public override string ToString()
		{
			return _name;
		}

		public IEnumerable<string> FileExtensions { get { return _fileExtensions; } }
		
		public void Print(LNode node, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			if (_usePlainCsPrinter)
				EcsNodePrinter.PrintPlainCSharp(node, target, sink, mode, options);
			else
				EcsNodePrinter.PrintECSharp(node, target, sink, mode, options);
		}
		public void Print(IEnumerable<LNode> nodes, StringBuilder target, IMessageSink sink = null, ParsingMode mode = null, ILNodePrinterOptions options = null)
		{
			LNodePrinter.PrintMultiple(this, nodes, target, sink, mode, options);
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
			return new EcsLexer(text, fileName, msgs) { SpacesPerTab = options.SpacesPerTab };
		}
		public IListSource<LNode> Parse(ICharSource text, string fileName, IMessageSink msgs, IParsingOptions options)
		{
			var lexer = Tokenize(text, fileName, msgs, options);
			return Parse(lexer, msgs, options);
		}
		public IListSource<LNode> Parse(ILexer<Token> input, IMessageSink msgs, IParsingOptions options)
		{
			var preprocessed = new EcsPreprocessor(input, options.PreserveComments);
			var treeified = new TokensToTree(preprocessed, false);
			var results = Parse(treeified.Buffered(), input.SourceFile, msgs, options);
			if (options.PreserveComments) {
				var injector = new EcsTriviaInjector(preprocessed.TriviaList, input.SourceFile, 
					(int)TokenType.Newline, "/*", "*/", "//", 
					!options.Mode.IsOneOf<Symbol>(ParsingMode.Expressions, ParsingMode.FormalArguments));
				return injector.Run(results.GetEnumerator()).Buffered();
			} else
				return results;
		}

		[ThreadStatic]
		static EcsParser _parser;

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
			var inputType = options.Mode;
			if (file.Text.TryGet(255).HasValue || inputType == ParsingMode.FormalArguments || 
				inputType == ParsingMode.Types || inputType == ParsingMode.Expressions)
			{
				EcsParser parser = _parser;
				if (parser == null)
					_parser = parser = new EcsParser(input, file, msgs);
				else {
					parser.ErrorSink = msgs ?? MessageSink.Default;
					parser.Reset(input, file);
				}
				if (inputType == ParsingMode.Expressions)
					return parser.ParseExprs(false, allowUnassignedVarDecl: false);
				else if (inputType == ParsingMode.FormalArguments)
					return parser.ParseExprs(false, allowUnassignedVarDecl: true);
				else if (inputType == ParsingMode.Types)
					return LNode.List(parser.DataType());
				else
					return parser.ParseStmtsGreedy();
			}
			else
			{
				var parser = new EcsParser(input, file, msgs);
				return parser.ParseStmtsLazy().Buffered();
			}
		}
	}
}

