// Generated from AntlrStyleParserGrammar.ecs by LeMP custom tool. LeMP version: 1.7.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;
	using TT = TokenType;
	using VList_LNode = VList<LNode>;
	internal class AntlrStyleParser : StageOneParser
	{
		[ThreadStatic] static AntlrStyleParser _parser;
		public new static VList<LNode> ParseTokenTree(TokenTree tokens, IMessageSink sink)
		{
			return Parse(tokens, tokens.File, sink);
		}
		public new static VList<LNode> Parse(IListSource<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			if (_parser == null)
				_parser = new AntlrStyleParser(tokenTree, file, messages);
			else {
				_parser.Reset(tokenTree, file);
				_parser.ErrorSink = messages;
			}
			return _parser.ParseRules();
		}
		private AntlrStyleParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink, IParsingService hostLanguage = null) : base(tokens, file, messageSink, hostLanguage)
		{
		}
		LNode ParseHostReturnType(Token paren)
		{
			var list = ParseHostArgList(paren, ParsingMode.FormalArgs);
			if (list.Count != 1)
				Error(-1, "LLLPG: Expected a single variable declaration (or data type) '{0}'", ToString(paren.TypeInt));
			LNode result;
			if (list.Count > 0)
				result = list[0];
			else
				result = LNode.Missing;
			if (result.Calls(S.Var, 2)) {
				if (!result[1].IsIdNamed("result"))
					ErrorSink.Write(Severity.Error, result[1], "LLLPG requires that the result of a rule be called 'result'");
				return result[0];
			} else
				return result;
		}
		LNode Rule()
		{
			TT la0, la1;
			LNode got_GrammarExpr = default(LNode);
			Token lit_lpar = default(Token);
			Token lit_lsqb = default(Token);
			Token tok__AttrKeyword = default(Token);
			// line 75
			var attrs = LNode.List();
			var args = LNode.List();
			LNode retType = null;
			// Line 80: (TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.LBrack) {
					lit_lsqb = MatchAny();
					Match((int) TT.RBrack);
					// line 80
					attrs = ParseHostArgList(lit_lsqb, ParsingMode.Exprs);
				} else
					break;
			}
			// Line 81: (TT.AttrKeyword)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.AttrKeyword) {
					tok__AttrKeyword = MatchAny();
					// line 81
					attrs.Add(F.Id(tok__AttrKeyword));
				} else
					break;
			}
			var ruleName = Match((int) TT.Id);
			// Line 84: (TT.LBrack TT.RBrack TT.Returns TT.LBrack TT.RBrack | TT.LParen TT.RParen TT.Returns TT.LParen TT.RParen)?
			la0 = (TT) LA0;
			if (la0 == TT.LBrack) {
				lit_lsqb = MatchAny();
				Match((int) TT.RBrack);
				// line 84
				args = ParseHostArgList(lit_lsqb, ParsingMode.FormalArgs);
				Match((int) TT.Returns);
				lit_lsqb = Match((int) TT.LBrack);
				Match((int) TT.RBrack);
				// line 85
				retType = ParseHostReturnType(lit_lsqb);
			} else if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				Match((int) TT.RParen);
				// line 86
				args = ParseHostArgList(lit_lpar, ParsingMode.FormalArgs);
				Match((int) TT.Returns);
				lit_lpar = Match((int) TT.LParen);
				Match((int) TT.RParen);
				// line 87
				retType = ParseHostReturnType(lit_lpar);
			}
			// Line 91: (TT.Id (TT.LBrace TT.RBrace | TT.Id)?)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				var id = MatchAny();
				// line 93
				string id2 = id.Value.ToString();
				bool isAntlrThing = id2.IsOneOf("scope", "throws", "options", "init", "after");
				Error(-1, isAntlrThing ? "LLLPG does not support ANTLR rule directives ('scope', 'throws', 'options', etc.)." : "Syntax error (expected ':' to begin the rule)");
				// Line 99: (TT.LBrace TT.RBrace | TT.Id)?
				la0 = (TT) LA0;
				if (la0 == TT.LBrace) {
					Skip();
					Match((int) TT.RBrace);
				} else if (la0 == TT.Id)
					Skip();
			}
			// Line 101: ((TT.Colon|TT.StartColon))
			la0 = (TT) LA0;
			if (la0 == TT.Colon || la0 == TT.StartColon)
				Skip();
			else {
				// line 101
				Error(0, "Expected ':' or '::='");
				// Line 101: (TT.Assignment)?
				la0 = (TT) LA0;
				if (la0 == TT.Assignment) {
					la1 = (TT) LA(1);
					if (la1 != (TT) EOF)
						Skip();
				}
			}
			got_GrammarExpr = GrammarExpr();
			Match((int) TT.Semicolon);
			// line 105
			return LNode.Call((Symbol) "#noLexicalMacros", LNode.List(LNode.Call(LNode.List(attrs), (Symbol) "#rule", LNode.List(retType ?? F.Void, F.Id(ruleName), LNode.Call(CodeSymbols.AltList, LNode.List(args)), got_GrammarExpr))));
		}
		public VList_LNode ParseRules()
		{
			TT la0;
			VList_LNode result = default(VList_LNode);
			result.Add(Rule());
			// Line 120: (Rule)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.AttrKeyword || la0 == TT.Id || la0 == TT.LBrack)
					result.Add(Rule());
				else
					break;
			}
			Match((int) EOF);
			return result;
		}
	}
}
