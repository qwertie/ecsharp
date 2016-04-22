// Generated from AntlrStyleParserGrammar.ecs by LeMP custom tool. LeMP version: 1.7.5.0
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
			return _parser.RulesAndStuff();
		}
		private AntlrStyleParser(IListSource<Token> tokens, ISourceFile file, IMessageSink messageSink, IParsingService hostLanguage = null) : base(tokens, file, messageSink, hostLanguage)
		{
		}
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
					ErrorSink.Write(Severity.Error, result[1], "LLLPG requires that the result of a rule be called 'result'");
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
			// Line 96: (&{Is($LI, _token)} TT.Id | &{Is($LI, _rule)} TT.Id)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				if (Is(0, _token)) {
					la1 = (TT) LA(1);
					if (la1 == TT.Id) {
						Skip();
						// line 96
						isToken = true;
					}
				} else if (Is(0, _rule)) {
					la1 = (TT) LA(1);
					if (la1 == TT.Id)
						Skip();
				}
			}
			var ruleName = Match((int) TT.Id);
			// Line 101: (TT.LBrack TT.RBrack | TT.LParen TT.RParen)?
			la0 = (TT) LA0;
			if (la0 == TT.LBrack) {
				lit_lsqb = MatchAny();
				Match((int) TT.RBrack);
				// line 101
				args = ParseHostCode(lit_lsqb, ParsingMode.FormalArguments);
			} else if (la0 == TT.LParen) {
				lit_lpar = MatchAny();
				Match((int) TT.RParen);
				// line 102
				args = ParseHostCode(lit_lpar, ParsingMode.FormalArguments);
			}
			// Line 104: ((TT.Returns TT.LBrack TT.RBrack)? | (TT.Returns TT.LParen TT.RParen)?)?
			do {
				switch ((TT) LA0) {
				case TT.Returns:
					{
						la1 = (TT) LA(1);
						if (la1 == TT.LBrack)
							goto match1;
						else {
							// Line 105: (TT.Returns TT.LParen TT.RParen)?
							la0 = (TT) LA0;
							if (la0 == TT.Returns) {
								Skip();
								lit_lpar = Match((int) TT.LParen);
								Match((int) TT.RParen);
								// line 105
								retType = ParseHostReturnType(lit_lpar);
							}
						}
					}
					break;
				case TT.Id:
					{
						switch ((TT) LA(1)) {
						case TT.At:
						case TT.Colon:
						case TT.Id:
						case TT.LBrace:
						case TT.StartColon:
							goto match1;
						}
					}
					break;
				case TT.Colon:
				case TT.StartColon:
					{
						la1 = (TT) LA(1);
						if (la1 != (TT) EOF)
							goto match1;
					}
					break;
				case TT.At:
					{
						la1 = (TT) LA(1);
						if (la1 == TT.LBrace)
							goto match1;
					}
					break;
				}
				break;
			match1:
				{
					// Line 104: (TT.Returns TT.LBrack TT.RBrack)?
					la0 = (TT) LA0;
					if (la0 == TT.Returns) {
						Skip();
						lit_lsqb = Match((int) TT.LBrack);
						Match((int) TT.RBrack);
						// line 104
						retType = ParseHostReturnType(lit_lsqb);
					}
				}
			} while (false);
			// line 108
			Token? initBrace = null;
			// Line 109: (&{Is($LI, _init)} TT.Id TT.LBrace TT.RBrace)?
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
			// Line 112: (TT.Id (TT.LBrace TT.RBrace | TT.Id)?)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				var id = MatchAny();
				// line 114
				string id2 = id.Value.ToString();
				bool isAntlrThing = id2.IsOneOf("scope", "throws", "options", "after");
				Error(-1, isAntlrThing ? "LLLPG does not support ANTLR rule directives ('scope', 'throws', 'options', etc.)." : "Syntax error (expected ':' to begin the rule)");
				// Line 120: (TT.LBrace TT.RBrace | TT.Id)?
				la0 = (TT) LA0;
				if (la0 == TT.LBrace) {
					Skip();
					Match((int) TT.RBrace);
				} else if (la0 == TT.Id)
					Skip();
			}
			// Line 124: (((TT.Colon|TT.StartColon)) GrammarExpr TT.Semicolon | TT.At TT.LBrace GrammarExpr TT.RBrace (TT.Semicolon)?)
			la0 = (TT) LA0;
			if (la0 == TT.Colon || la0 == TT.StartColon) {
				// Line 124: ((TT.Colon|TT.StartColon))
				la0 = (TT) LA0;
				if (la0 == TT.Colon || la0 == TT.StartColon)
					Skip();
				else {
					// line 124
					Error(0, "Expected ':' or '::=' to begin the rule");
					// Line 124: greedy(TT.Assignment)?
					la0 = (TT) LA0;
					if (la0 == TT.Assignment) {
						la1 = (TT) LA(1);
						if (la1 != (TT) EOF)
							Skip();
					}
				}
				gExpr = GrammarExpr();
				Match((int) TT.Semicolon);
			} else {
				Match((int) TT.At);
				Match((int) TT.LBrace);
				gExpr = GrammarExpr();
				Match((int) TT.RBrace);
				// Line 130: (TT.Semicolon)?
				la0 = (TT) LA0;
				if (la0 == TT.Semicolon)
					Skip();
			}
			// line 133
			if (initBrace != null) {
				var initAction = ParseHostBraces(initBrace.Value, initRB.EndIndex, ParsingMode.Statements);
				gExpr = LNode.Call(CodeSymbols.Tuple, LNode.List(initAction, gExpr));
			}
			var rule = isToken ? F.Id("#token") : F.Id("#rule");
			return LNode.Call((Symbol) "#noLexicalMacros", LNode.List(LNode.Call(LNode.List(attrs), rule, LNode.List(retType ?? F.Void, F.Id(ruleName), LNode.Call(CodeSymbols.AltList, LNode.List(args)), gExpr))));
		}
		LNode HostCall()
		{
			TT la0;
			Token lit_lpar = default(Token);
			Token target = default(Token);
			// Line 155: (TT.Id)?
			la0 = (TT) LA0;
			if (la0 == TT.Id)
				target = MatchAny();
			lit_lpar = Match((int) TT.LParen);
			Match((int) TT.RParen);
			Match((int) TT.Semicolon);
			// line 157
			var args = ParseHostCode(lit_lpar, ParsingMode.Expressions);
			return F.Call(F.Id(target), args);
		}
		LNode HostBlock()
		{
			TT la0;
			Token lit_lcub = default(Token);
			// Line 164: (&{Is($LI, _members)} TT.Id)?
			la0 = (TT) LA0;
			if (la0 == TT.Id) {
				Check(Is(0, _members), "Is($LI, _members)");
				Skip();
			}
			lit_lcub = Match((int) TT.LBrace);
			Match((int) TT.RBrace);
			// Line 165: (TT.Semicolon)?
			la0 = (TT) LA0;
			if (la0 == TT.Semicolon)
				Skip();
			// line 167
			var args = ParseHostCode(lit_lcub, ParsingMode.Declarations);
			return args.AsLNode(S.Splice);
		}
		public VList<LNode> RulesAndStuff()
		{
			TT la1;
			VList<LNode> result = default(VList<LNode>);
			// Line 174: ( Rule | HostCall | HostBlock )
			switch ((TT) LA0) {
			case TT.At:
			case TT.AttrKeyword:
			case TT.LBrack:
				result.Add(Rule());
				break;
			case TT.Id:
				{
					if (Is(0, _members)) {
						if (Is(0, _token) || Is(0, _rule)) {
							switch ((TT) LA(1)) {
							case TT.Id:
							case TT.LBrack:
								result.Add(Rule());
								break;
							case TT.LParen:
								{
									switch ((TT) LA(3)) {
									case TT.At:
									case TT.Colon:
									case TT.Id:
									case TT.Returns:
									case TT.StartColon:
										result.Add(Rule());
										break;
									default:
										result.Add(HostCall());
										break;
									}
								}
								break;
							case TT.At:
							case TT.Colon:
							case TT.Returns:
							case TT.StartColon:
								result.Add(Rule());
								break;
							default:
								result.Add(HostBlock());
								break;
							}
						} else {
							switch ((TT) LA(1)) {
							case TT.LBrack:
								result.Add(Rule());
								break;
							case TT.LParen:
								{
									switch ((TT) LA(3)) {
									case TT.At:
									case TT.Colon:
									case TT.Id:
									case TT.Returns:
									case TT.StartColon:
										result.Add(Rule());
										break;
									default:
										result.Add(HostCall());
										break;
									}
								}
								break;
							case TT.At:
							case TT.Colon:
							case TT.Id:
							case TT.Returns:
							case TT.StartColon:
								result.Add(Rule());
								break;
							default:
								result.Add(HostBlock());
								break;
							}
						}
					} else if (Is(0, _token) || Is(0, _rule)) {
						la1 = (TT) LA(1);
						if (la1 == TT.Id || la1 == TT.LBrack)
							result.Add(Rule());
						else if (la1 == TT.LParen) {
							switch ((TT) LA(3)) {
							case TT.At:
							case TT.Colon:
							case TT.Id:
							case TT.Returns:
							case TT.StartColon:
								result.Add(Rule());
								break;
							default:
								result.Add(HostCall());
								break;
							}
						} else
							result.Add(Rule());
					} else {
						la1 = (TT) LA(1);
						if (la1 == TT.LBrack)
							result.Add(Rule());
						else if (la1 == TT.LParen) {
							switch ((TT) LA(3)) {
							case TT.At:
							case TT.Colon:
							case TT.Id:
							case TT.Returns:
							case TT.StartColon:
								result.Add(Rule());
								break;
							default:
								result.Add(HostCall());
								break;
							}
						} else
							result.Add(Rule());
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
			// Line 174: ( Rule | HostCall | HostBlock )*
			for (;;) {
				switch ((TT) LA0) {
				case TT.At:
				case TT.AttrKeyword:
				case TT.LBrack:
					result.Add(Rule());
					break;
				case TT.Id:
					{
						if (Is(0, _members)) {
							if (Is(0, _token) || Is(0, _rule)) {
								switch ((TT) LA(1)) {
								case TT.Id:
								case TT.LBrack:
									result.Add(Rule());
									break;
								case TT.LParen:
									{
										switch ((TT) LA(3)) {
										case TT.At:
										case TT.Colon:
										case TT.Id:
										case TT.Returns:
										case TT.StartColon:
											result.Add(Rule());
											break;
										default:
											result.Add(HostCall());
											break;
										}
									}
									break;
								case TT.At:
								case TT.Colon:
								case TT.Returns:
								case TT.StartColon:
									result.Add(Rule());
									break;
								default:
									result.Add(HostBlock());
									break;
								}
							} else {
								switch ((TT) LA(1)) {
								case TT.LBrack:
									result.Add(Rule());
									break;
								case TT.LParen:
									{
										switch ((TT) LA(3)) {
										case TT.At:
										case TT.Colon:
										case TT.Id:
										case TT.Returns:
										case TT.StartColon:
											result.Add(Rule());
											break;
										default:
											result.Add(HostCall());
											break;
										}
									}
									break;
								case TT.At:
								case TT.Colon:
								case TT.Id:
								case TT.Returns:
								case TT.StartColon:
									result.Add(Rule());
									break;
								default:
									result.Add(HostBlock());
									break;
								}
							}
						} else if (Is(0, _token) || Is(0, _rule)) {
							la1 = (TT) LA(1);
							if (la1 == TT.Id || la1 == TT.LBrack)
								result.Add(Rule());
							else if (la1 == TT.LParen) {
								switch ((TT) LA(3)) {
								case TT.At:
								case TT.Colon:
								case TT.Id:
								case TT.Returns:
								case TT.StartColon:
									result.Add(Rule());
									break;
								default:
									result.Add(HostCall());
									break;
								}
							} else
								result.Add(Rule());
						} else {
							la1 = (TT) LA(1);
							if (la1 == TT.LBrack)
								result.Add(Rule());
							else if (la1 == TT.LParen) {
								switch ((TT) LA(3)) {
								case TT.At:
								case TT.Colon:
								case TT.Id:
								case TT.Returns:
								case TT.StartColon:
									result.Add(Rule());
									break;
								default:
									result.Add(HostCall());
									break;
								}
							} else
								result.Add(Rule());
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
