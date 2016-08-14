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
			: this((UString)text, fileName, BaseLexer.LogExceptionErrorSink) { }
		public Les3Lexer(ICharSource text, string fileName, IMessageSink sink, int startPosition = 0) 
			: base(text, fileName, sink, startPosition) { }
	
		// Creates a Token
		private Token T(TokenType type, object value)
		{
			return new Token((int)type, _startPosition, InputPosition - _startPosition, value);
		}
	}
	
	partial class Les3Parser : LesParser
	{
		private LNode ExpressionAndEof()
		{
			throw new NotImplementedException();
		}

		//LNodeFactory F;

		public Les3Parser(IList<Token> list, ISourceFile file, IMessageSink sink, int startIndex = 0)
			: base(list, file, sink, startIndex) { }//{ F = new LNodeFactory(file); }
		
		// Used for error reporting
		protected override string ToString(int tokenType) { 
			return ((TokenType)tokenType).ToString();
		}

		protected new Precedence PrefixPrecedenceOf(Token t)
		{
			var prec = base.PrefixPrecedenceOf(t);
			if (prec == LesPrecedence.Other)
				ErrorSink.Write(Severity.Error, new SourceRange(SourceFile, t.StartIndex, t.Length), 
					"Operator `{0}` cannot be used as a prefix operator", t.Value);
			return prec;
		}
	}
}
