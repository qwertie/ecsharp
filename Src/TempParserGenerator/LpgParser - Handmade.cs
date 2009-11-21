using System;
using System.Collections.Generic;
using System.Text;
using Loyc.CompilerCore;
using Loyc.Runtime;

namespace TempParserGenerator
{
	class LpgParser : BaseRecognizer<AstNode>
	{
		static public readonly Symbol _ID = Symbol.Get("ID");
		static public readonly Symbol _INT = Symbol.Get("INT");
		static public readonly Symbol _LPAREN = Symbol.Get("LPAREN");
		static public readonly Symbol _RPAREN = Symbol.Get("RPAREN");
		static public readonly Symbol _LBRACE = Symbol.Get("LBRACE");
		static public readonly Symbol _RBRACE = Symbol.Get("RBRACE");
		static public readonly Symbol _LBRACK = Symbol.Get("LBRACK");
		static public readonly Symbol _RBRACK = Symbol.Get("RBRACK");
		static public readonly Symbol _INDENT = Symbol.Get("INDENT");
		static public readonly Symbol _DEDENT = Symbol.Get("DEDENT");
		static public readonly Symbol _SQ_STRING = Symbol.Get("SQ_STRING");
		static public readonly Symbol _DQ_STRING = Symbol.Get("DQ_STRING");
		static public readonly Symbol _RE_STRING = Symbol.Get("RE_STRING");
		static public readonly Symbol _PUNC = Symbol.Get("PUNC");
		static public readonly Symbol _EOS = Symbol.Get("EOS");
		static public readonly Symbol _COLON = Symbol.Get("COLON");

		List<AstNode> _parsers = new List<AstNode>();

		////////////////////////////////////////////////////////////////////////
		// Planned parser generator code ///////////////////////////////////////
		 
		// start rule Code()
		void Code()
		{
			AstNode LA0 = LA(0);
			int alt = -1;
			if (IsMatch(LA0, "tree"))
				alt = 1;
			else if (IsMatch(LA0, "parser"))
				alt = 1;
			else if (IsMatch(LA0, "namespace"))
				alt = 2;
			else if (!IsEof(LA0))
				alt = 3;

			if (alt == 1)
				Parser();
			else if (alt == 2)
			{
				Match("namespace");
				Match(_ID);
				Match(":");
				BeginChild(Match(_INDENT));
				try {
					Code();
				}
				finally {
					EndChild();
				}
			}
		}

		void Parser()
		{
			// "tree"? "parser" ID ":" ^(INDENT ...)
			int alt; 
			
			AstNode LA0 = LA(0);
			if (IsMatch(LA0, "tree"))
				Consume();

			Match("parser");
			Match(_ID);
			Match(":");
			BeginChild(Match(_INDENT));
			try {
				do {
					// option LL=1 (Rule | Options | .)*
					LA0 = LA(0);
					alt = -1;
					if (IsMatch(LA0, "rule"))
						alt = 1;
					else if (IsMatch(LA0, "option"))
						alt = 2;
					else if (IsMatch(LA0, "options"))
						alt = 2;
					else if (!IsEof(LA0))
						alt = 3;

					if (alt == 1)
						Rule();
					else if (alt == 2)
						Options();
					else if (alt == 3)
						Consume();
				} while (alt != -1);
			}
			finally {
				EndChild();
			}
		}

		static SymbolSet OneOf_LPAREN_LBRACK = TerminalSet(_LPAREN, _LBRACK);

		private void Rule()
		{
			// "rule" ID (args=(LPAREN | LBRACK))? ":" ^(INDENT ...)
			Match("rule");
			Match(_ID);
			Match(OneOf_LPAREN_LBRACK);
			Match(":");
			BeginChild(Match(INDENT));
			try {
				// Expr DEDENT
				Expr();
				Match(_DEDENT);
			}
			finally {
				EndChild();
			}
		}

		private void Expr()
		{
			// Alternative (("|" | "/") Alternative)* ("->" RewriteExpr)?
			throw new NotImplementedException();
		}

		private static SymbolSet TerminalSet(Symbol one, Symbol two)
		{
			var ss = new SymbolSet();
			ss.Add(one);
			ss.Add(two);
		}

		private void Options()
		{
			// ("option" | "options") Option ("," Option)* "in"?
			int alt = -1;
			AstNode LA0 = LA(0);
			if (IsMatch(LA0, "option"))
				alt = 1;
			else
				alt = 2;

			if (alt != 1 && alt != 2)
				Throw("'option' or 'options'", LA0);

			Option();
		}

		private void Option()
		{
			// ID ("=" .)?
			Match(_ID);
			
			AstNode LA0 = LA(0);
			if (IsMatch(LA0, "="))
			{
				Consume();
				Consume();
			}
		}

		private void BeginChild(AstNode astNode)
		{
			throw new NotImplementedException();
		}
		private void EndChild()
		{
			throw new NotImplementedException();
		}

		private AstNode Match(string text)
		{
			AstNode LA0 = LA(0);
			if (!IsMatch(LA0, text))
				Throw(text, LA0);
			else
				Consume();
			return LA0;
		}
		private AstNode Match(Symbol symbol)
		{
			AstNode LA0 = LA(0);
			if (!IsMatch(LA0, symbol))
				Throw(symbol.Name, LA0);
			else
				Consume();
			return LA0;
		}
		private AstNode Match(SymbolSet set)
		{
			AstNode LA0 = LA(0);
			if (!IsMatch(LA0, set))
				Throw(text, set.ToString());
			else
				Consume();
			return LA0;
		}

		private bool IsEof(AstNode LA0)
		{
			return LA0 == null;
		}

		private bool IsMatch(AstNode n, string text)
		{
			if (n.Range.Length != text.Length)
				return false;
			if (text.Length <= 0)
				return true;
			if (n.Range[0] != text[0])
				return false;
			if (text.Length == 1)
				return true;
			return n.SourceText == text;
		}
		private bool IsMatch(AstNode n, Symbol symbol)
		{
			return n.NodeType == symbol;
		}
		private bool IsMatch(AstNode n, SymbolSet set)
		{
			return set.Contains(n.NodeType);
		}
	}
}
