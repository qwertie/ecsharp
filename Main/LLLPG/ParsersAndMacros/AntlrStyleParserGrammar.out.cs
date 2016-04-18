// Generated from AntlrStyleParserGrammar.ecs by LeMP custom tool. LeMP version: 1.7.3.0
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
	internal class AntlrStyleParser : StageOneParser
	{
		[ThreadStatic]
		static AntlrStyleParser _parser;
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
		static readonly Symbol _init = (Symbol) "init";
		bool Is(int li, Symbol value)
		{
			var lt = LT(li);
			return lt.Value == value;
		}
		LNode Rule()
		{
			TT la0, la1;
			Token initRB = default(Token);
			Token lit_lpar = default(Token);
			Token lit_lsqb = default(Token);
			Token tok__AttrKeyword = default(Token);
			// line 81
			var attrs = LNode.List();
			var args = LNode.List();
			LNode retType = null;
			// Line 86: (TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.LBrack) {
					lit_lsqb = MatchAny();
					Match((int) TT.RBrack);
					// line 86
					attrs = ParseHostArgList(lit_lsqb, ParsingMode.Exprs);
				} else
					break;
			}
			// Line 87: (TT.AttrKeyword)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.AttrKeyword) {
					tok__AttrKeyword = MatchAny();
					// line 87
					attrs.Add(F.Id(tok__AttrKeyword));
				} else
					break;
			}
			var ruleName = Match((int) TT.Id);
			// Line 90: (TT.LBrack TT.RBrack (TT.Returns TT.LBrack TT.RBrack)? | TT.LParen TT.RParen (TT.Returns TT.LParen TT.RParen)?)?
			la0 = (TT) LA0;
			if (la0 == TT.LBrack) {
				lit_lsqb = MatchAny();
				Match((int) TT.RBrack);
				// line 90
				args = ParseHostArgList(lit_lsqb, ParsingMode.FormalArgs);
				// Line 91: (TT.Returns TT.LBrack TT.RBrack)?
				la0 = (TT) LA0;
				if (la0 == TT.Returns) {
					Skip();
					lit_lsqb = Match((int) TT.LBrack);
					Match((int) TT.RBrack);
					// line 91
					retType = ParseHostReturnType(lit_lsqb);
				}
			} else if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				Match((int) TT.RParen);
				// line 92
				args = ParseHostArgList(lit_lpar, ParsingMode.FormalArgs);
				// Line 93: (TT.Returns TT.LParen TT.RParen)?
				la0 = (TT) LA0;
				if (la0 == TT.Returns) {
					Skip();
					lit_lpar = Match((int) TT.LParen);
					Match((int) TT.RParen);
					// line 93
					retType = ParseHostReturnType(lit_lpar);
				}
			}
			// line 96
			Token? initBrace = null;
			// Line 97: (&{Is($LI, _init)} TT.Id TT.LBrace TT.RBrace)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				if (Is(0, _init)) {
					la1 = (TT) LA(1);
					if (la1 == TT.LBrace) {
						Skip();
						initBrace = MatchAny();
						initRB = Match((int) TT.RBrace);
					}
				}
			}
			// Line 100: (TT.Id (TT.LBrace TT.RBrace | TT.Id)?)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				var id = MatchAny();
				// line 102
				string id2 = id.Value.ToString();
				bool isAntlrThing = id2.IsOneOf("scope", "throws", "options", "after");
				Error(-1, isAntlrThing ? "LLLPG does not support ANTLR rule directives ('scope', 'throws', 'options', etc.)." : "Syntax error (expected ':' to begin the rule)");
				// Line 108: (TT.LBrace TT.RBrace | TT.Id)?
				la0 = (TT) LA0;
				if (la0 == TT.LBrace) {
					Skip();
					Match((int) TT.RBrace);
				} else if (la0 == TT.Id)
					Skip();
			}
			// Line 111: ((TT.Colon|TT.StartColon))
			la0 = (TT) LA0;
			if (la0 == TT.Colon || la0 == TT.StartColon)
				Skip();
			else {
				// line 111
				Error(0, "Expected ':' or '::=' to begin the rule");
				// Line 111: greedy(TT.Assignment)?
				la0 = (TT) LA0;
				if (la0 == TT.Assignment) {
					la1 = (TT) LA(1);
					if (la1 != (TT) EOF)
						Skip();
				}
			}
			var gExpr = GrammarExpr();
			Match((int) TT.Semicolon);
			// line 115
			if (initBrace != null) {
				var initAction = ParseHostBraces(initBrace.Value, initRB.EndIndex, ParsingMode.Stmts);
				gExpr = LNode.Call(CodeSymbols.Tuple, LNode.List(initAction, gExpr));
			}
			return LNode.Call((Symbol) "#noLexicalMacros", LNode.List(LNode.Call(LNode.List(attrs), (Symbol) "#rule", LNode.List(retType ?? F.Void, F.Id(ruleName), LNode.Call(CodeSymbols.AltList, LNode.List(args)), gExpr))));
		}
		public VList<LNode> ParseRules()
		{
			TT la0;
			VList<LNode> result = default(VList<LNode>);
			result.Add(Rule());
			// Line 134: (Rule)*
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
