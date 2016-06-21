// Currently, no IntelliSense (code completion) is available in .ecs files,
// so it can be useful to split your Lexer and Parser classes between two 
// files. In this file (the .cs file) IntelliSense will be available and 
// the other file (the .ecs file) contains your grammar code.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;               // optional (for IMessageSink, Symbol, etc.)
using Loyc.Collections;   // optional (many handy interfaces & classes)
using Loyc.Syntax.Lexing; // For BaseLexer
using Loyc.Syntax;        // For BaseParser<Token> and LNode

namespace Loyc.Syntax.Les
{
	partial class Les3Lexer : LesLexer
	{
		// When using the Loyc libraries, `BaseLexer` and `BaseILexer` read character 
		// data from an `ICharSource`, which the string wrapper `UString` implements.
		public Les3Lexer(string text, string fileName = "") 
			: this((UString)text, fileName, BaseLexer.FormatExceptionErrorSink) { }
		public Les3Lexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) 
			: base(text, fileName, null) { }
	
		// Creates a Token
		private Token T(TokenType type, object value)
		{
			return new Token((int)type, _startPosition, InputPosition - _startPosition, value);
		}
	}
	
	partial class Les3Parser : LesParser
	{
		public static LNode Parse(string text, IMessageSink errorSink, string fileName = "")
		{
			var lexer = new Les3Lexer(text, fileName);
			// Lexer is derived from BaseILexer, which implements IEnumerator<Token>.
			// Buffered() is an extension method that gathers the output of the 
			// enumerator into a list so that the parser can consume it.
			var parser = new Les3Parser(lexer.Buffered(), lexer.SourceFile, errorSink);
			return parser.ExpressionAndEof();
		}

		private LNode ExpressionAndEof()
		{
			throw new NotImplementedException();
		}

		//LNodeFactory F;

		protected Les3Parser(IList<Token> list, ISourceFile file, IMessageSink sink, int startIndex = 0)
			: base(list, file, sink, startIndex) { }//{ F = new LNodeFactory(file); }
		
		// Used for error reporting
		protected override string ToString(int tokenType) { 
			return ((TokenType)tokenType).ToString();
		}
	}
}
