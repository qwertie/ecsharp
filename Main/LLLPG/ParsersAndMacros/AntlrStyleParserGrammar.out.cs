// Generated from AntlrStyleParserGrammar.ecs by LeMP custom tool. LeMP version: 2.8.0.0
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
	
		public new static LNodeList ParseTokenTree(TokenTree tokens, IMessageSink sink)
		{
			return Parse(tokens, tokens.File, sink);
		}
		public new static LNodeList Parse(IList<Token> tokenTree, ISourceFile file, IMessageSink messages)
		{
			if (_parser == null)
				_parser = new AntlrStyleParser(tokenTree, file, messages);
			else {
				_parser.Reset(tokenTree, default(Token), file);
				_parser.ErrorSink = messages;
			}
			return _parser.RulesAndStuff();
		}
	
		private AntlrStyleParser(IList<Token> tokens, ISourceFile file, IMessageSink messageSink)
			 : base(tokens, file, messageSink) { }
	
		LNode ParseHostReturnType(Token paren)
		{
			var list = ParseHostCode(paren, ParsingMode.FormalArguments);
			if (list.Count != 1)
				Error(-1, "LLLPG: Expected a single variable declaration (or data type) after '{0}'", ToString(paren.TypeInt));
			LNode result;
			if (list.Count > 0)
				result = list[0];
			else
				result = LNode.Missing;
			if (result.Calls(S.Var, 2)) {
				if (!result[1].IsIdNamed("result"))
					ErrorSink.Error(result[1], "LLLPG requires that the result of a rule be called 'result'");
				return result[0];
			} else
				return result;
		}
	
		static readonly Symbol _init = (Symbol) "init";
		static readonly Symbol _members = (Symbol) "members";
		static readonly Symbol _token = (Symbol) "token";
		static readonly Symbol _rule = (Symbol) "rule";
		bool Is(int li, Symbol value)
		{
			var lt = LT(li);
			return lt.Value == value;
		}
	
	
		// The output is essentially #rule($returnType, $name, $args, $grammarExpr)
		LNode Rule()
		{
			TT la0, la1;
			LNode gExpr = default(LNode);
			Token initRB = default(Token);
			Token lit_lpar = default(Token);
			Token lit_lsqb = default(Token);
			Token tok__AttrKeyword = default(Token);
			// line 85
			var attrs = LNode.List();
			var args = LNode.List();
			LNode retType = null;
			// Line 91: ((TT.At)? TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.At || la0 == TT.LBrack) {
					// Line 91: (TT.At)?
					la0 = (TT) LA0;
					if (la0 == TT.At)
						Skip();
					lit_lsqb = Match((int) TT.LBrack);
					Match((int) TT.RBrack);
					// line 92
					attrs.AddRange(ParseHostCode(lit_lsqb, ParsingMode.Expressions));
				} else
					break;
			}
			// Line 93: (TT.AttrKeyword)*
			for (;;) {
				la0 = (TT) LA0;
				if (la0 == TT.AttrKeyword) {
					tok__AttrKeyword = MatchAny();
					// line 93
					attrs.Add(F.Id(tok__AttrKeyword));
				} else
					break;
			}
			// line 95
			bool isToken = false;
			// Line 97: greedy(&{Is($LI, _token)} TT.Id | &{Is($LI, _rule)} TT.Id)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				if (Is(0, _token)) {
					la1 = (TT) LA(1);
					if (la1 == TT.Id) {
						Skip();
						// line 97
						isToken = true;
					}
				} else if (Is(0, _rule)) {
					la1 = (TT) LA(1);
					if (la1 == TT.Id)
						Skip();
				}
			}
			var ruleName = Match((int) TT.Id);
			// Line 102: (TT.LBrack TT.RBrack | TT.LParen TT.RParen)?
			la0 = (TT) LA0;
			if (la0 == TT.LBrack) {
				lit_lsqb = MatchAny();
				Match((int) TT.RBrack);
				// line 102
				args = ParseHostCode(lit_lsqb, ParsingMode.FormalArguments);
			} else if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				Match((int) TT.RParen);
				// line 103
				args = ParseHostCode(lit_lpar, ParsingMode.FormalArguments);
			}
			// Line 105: (TT.Returns TT.LBrack TT.RBrack | TT.Returns TT.LParen TT.RParen)?
			la0 = (TT) LA0;
			if (la0 == TT.Returns) {
				la1 = (TT) LA(1);
				if (la1 == TT.LBrack) {
					Skip();
					lit_lsqb = MatchAny();
					Match((int) TT.RBrack);
					// line 105
					retType = ParseHostReturnType(lit_lsqb);
				} else {
					Skip();
					lit_lpar = Match((int) TT.LParen);
					Match((int) TT.RParen);
					// line 106
					retType = ParseHostReturnType(lit_lpar);
				}
			}
			// line 109
			Token? initBrace = null;
			// Line 111: greedy(&{Is($LI, _init)} TT.Id TT.LBrace TT.RBrace)?
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
			// Line 114: (TT.Id (TT.LBrace TT.RBrace | TT.Id)?)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				var id = MatchAny();
				// line 116
				string id2 = id.Value.ToString();
				bool isAntlrThing = id2.IsOneOf("scope", "throws", "options", "after");
				Error(-1, isAntlrThing ? "LLLPG does not support ANTLR rule directives ('scope', 'throws', 'options', etc.)." : "Syntax error (expected ':' to begin the rule)");
				// Line 122: (TT.LBrace TT.RBrace | TT.Id)?
				la0 = (TT) LA0;
				if (la0 == TT.LBrace) {
					Skip();
					Match((int) TT.RBrace);
				} else if (la0 == TT.Id)
					Skip();
			}
			// Line 126: (((TT.Colon|TT.StartColon)) GrammarExpr TT.Semicolon | TT.At TT.LBrace GrammarExpr TT.RBrace (TT.Semicolon)?)
			la0 = (TT) LA0;
			if (la0 == TT.Colon || la0 == TT.StartColon) {
				// Line 126: ((TT.Colon|TT.StartColon))
				la0 = (TT) LA0;
				if (la0 == TT.Colon || la0 == TT.StartColon)
					Skip();
				else {
					// line 126
					Error(0, "Expected ':' or '::=' to begin the rule");
					// Line 126: greedy(TT.Assignment)?
					la0 = (TT) LA0;
					if (la0 == TT.Assignment)
						Skip();
				}
				gExpr = GrammarExpr();
				Match((int) TT.Semicolon);
			} else {
				Match((int) TT.At);
				Match((int) TT.LBrace);
				gExpr = GrammarExpr();
				Match((int) TT.RBrace);
				// Line 132: (TT.Semicolon)?
				la0 = (TT) LA0;
				if (la0 == TT.Semicolon)
					Skip();
			}
			// line 135
			if (initBrace != null) {
				var initAction = ParseHostBraces(initBrace.Value, initRB.EndIndex, ParsingMode.Statements);
				gExpr = LNode.Call(CodeSymbols.Tuple, LNode.List(initAction, gExpr));
			}
			var rule = isToken ? F.Id("#token") : F.Id("#rule");
			return LNode.Call((Symbol) "#noLexicalMacros", LNode.List(LNode.Call(LNode.List(attrs), rule, LNode.List(retType ?? F.Void, F.Id(ruleName), LNode.Call(CodeSymbols.AltList, LNode.List(args)), gExpr))));
		}
	
	
		// Supports alias("..." = Token) statements
		LNode HostCall()
		{
			TT la0;
			Token lit_lpar = default(Token);
			Token target = default(Token);
			// Line 157: (TT.Id)?
			la0 = (TT) LA0;
			if (la0 == TT.Id)
				target = MatchAny();
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			Match((int) TT.Semicolon);
			// line 159
			var args = ParseHostCode(lit_lpar, ParsingMode.Expressions);
			return F.Call(F.Id(target), args);
		}
	
	
		// Inserts code into output. In ANTLR you write @members {...}
		LNode HostBlock()
		{
			TT la0;
			Token lit_lcub = default(Token);
			// Line 166: (&{Is($LI, _members)} TT.Id)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				Check(Is(0, _members), "Expected Is($LI, _members)");
				Skip();
			}
			lit_lcub = Match((int) TT.LBrace);
			Match((int) TT.RBrace);
			// Line 167: (TT.Semicolon)?
			la0 = (TT) LA0;
			if (la0 == TT.Semicolon)
				Skip();
			// line 169
			var args = ParseHostCode(lit_lcub, ParsingMode.Declarations);
			return args.AsLNode(S.Splice);
		}
	
	
		public LNodeList RulesAndStuff()
		{
			LNodeList result = default(LNodeList);
			// Line 176: ( Rule | HostCall | HostBlock )
			switch ((TT) LA0) {
			case TT.At: case TT.AttrKeyword: case TT.LBrack:
				result.Add(Rule());
				break;
			case TT.Id:
				{
					switch ((TT) LA(1)) {
					case TT.Id: case TT.LBrack:
						result.Add(Rule());
						break;
					case TT.LParen:
						{
							switch ((TT) LA(3)) {
							case TT.At: case TT.Colon: case TT.Id: case TT.Returns:
							case TT.StartColon:
								result.Add(Rule());
								break;
							default:
								result.Add(HostCall());
								break;
							}
						}
						break;
					case TT.At: case TT.Colon: case TT.Returns: case TT.StartColon:
						result.Add(Rule());
						break;
					default:
						result.Add(HostBlock());
						break;
					}
				}
				break;
			case TT.LParen:
				result.Add(HostCall());
				break;
			default:
				result.Add(HostBlock());
				break;
			}
			// Line 176: ( Rule | HostCall | HostBlock )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.At: case TT.AttrKeyword: case TT.LBrack:
					result.Add(Rule());
					break;
				case TT.Id:
					{
						switch ((TT) LA(1)) {
						case TT.Id: case TT.LBrack:
							result.Add(Rule());
							break;
						case TT.LParen:
							{
								switch ((TT) LA(3)) {
								case TT.At: case TT.Colon: case TT.Id: case TT.Returns:
								case TT.StartColon:
									result.Add(Rule());
									break;
								default:
									result.Add(HostCall());
									break;
								}
							}
							break;
						case TT.At: case TT.Colon: case TT.Returns: case TT.StartColon:
							result.Add(Rule());
							break;
						default:
							result.Add(HostBlock());
							break;
						}
					}
					break;
				case TT.LParen:
					result.Add(HostCall());
					break;
				case TT.LBrace:
					result.Add(HostBlock());
					break;
				default:
					goto stop;
				}
			}
		stop:;
			Match((int) EOF);
			return result;
		}
	
	}
}