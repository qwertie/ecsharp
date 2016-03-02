// Generated from EcsParserGrammar.les by LeMP custom tool. LeMP version: 1.5.0.0
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
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Loyc.Syntax.Lexing;
using Loyc.Collections;
using Loyc.Collections.Impl;
namespace Loyc.Ecs.Parser
{
	using TT = TokenType;
	using S = CodeSymbols;
	using EP = EcsPrecedence;
	
	#pragma warning disable 162, 642
	
	partial class EcsParser
	{
		static readonly Symbol _trait = GSymbol.Get("trait");
		static readonly Symbol _alias = GSymbol.Get("alias");
		static readonly Symbol _where = GSymbol.Get("where");
		static readonly Symbol _when = GSymbol.Get("when");
		static readonly Symbol _assembly = GSymbol.Get("assembly");
		static readonly Symbol _module = GSymbol.Get("module");
		static readonly Symbol _from = GSymbol.Get("from");
		static readonly Symbol _await = GSymbol.Get("await");
		Symbol _spaceName;
		bool _insideLinqExpr;
		internal static readonly HashSet<object> LinqKeywords = EcsLexer.LinqKeywords;
		LNode DataType(bool afterAsOrIs = false)
		{
			Token? brack;
			var type = DataType(afterAsOrIs, out brack);
			if ((brack != null)) {
				Error("A type name cannot include [array dimensions]. The square brackets should be empty.");
			}
			return type;
		}
		static LNode AutoRemoveParens(LNode node)
		{
			int i = node.Attrs.IndexWithName(S.TriviaInParens);
			if ((i > -1)) {
				return node.WithAttrs(node.Attrs.RemoveAt(i));
			}
			return node;
		}
		int count;
		public static readonly Precedence ContinueExpr = new Precedence(-100);
		LNode TypeInside(Token args)
		{
			if ((!Down(args)))
				 return F.Id(S.Missing, args.EndIndex, args.EndIndex);
			var type = DataType();
			Match((int) EOF);
			return Up(type);
		}
		LNode SetOperatorStyle(LNode node)
		{
			return node.SetBaseStyle(NodeStyle.Operator);
		}
		LNode SetAlternateStyle(LNode node)
		{
			node.Style |= NodeStyle.Alternate;
			return node;
		}
		void NonKeywordAttrError(IList<LNode> attrs, string stmtType)
		{
			var attr = attrs.FirstOrDefault(a => a.AttrNamed(S.TriviaWordAttribute) != null);
			if ((attr != null)) {
				Error(attr, "'{0}' appears to be a word attribute, which is not permitted before '{1}'", attr.Range.SourceText, stmtType);
			}
		}
		static readonly Symbol _var = GSymbol.Get("var");
		static readonly Symbol _dynamic = GSymbol.Get("dynamic");
		private void MaybeRecognizeVarAsKeyword(ref LNode type)
		{
			SourceRange rng;
			Symbol name = type.Name;
			if ((name == _var || name == _dynamic) && type.IsId && (rng = type.Range).Source.Text.TryGet(rng.StartIndex, '\0') != '@') {
				type = type.WithName(name == _var ? S.Missing : S.Dynamic);
			}
		}
		bool IsNamedArg(LNode node)
		{
			return node.Calls(S.NamedArg, 2) && node.BaseStyle == NodeStyle.Operator;
		}
		WList<LNode> _stmtAttrs = new WList<LNode>();
		LNode CoreName(LNode complexId)
		{
			if (complexId.IsId)
				 return complexId;
			if (complexId.CallsMin(S.Of, 1))
				 return CoreName(complexId.Args[0]);
			if (complexId.CallsMin(S.Dot, 1))
				 return complexId.Args.Last;
			if (complexId.CallsMin(S.Substitute, 1))
				 return complexId;
			Debug.Fail("Not a complex identifier");
			return complexId.Target;
		}
		Symbol TParamSymbol(LNode T)
		{
			if (T.IsId)
				 return T.Name;
			else if (T.Calls(S.Substitute, 1) && T.Args[0].IsId)
				 return T.Args[0].Name;
			else
				 return S.Missing;
		}
		bool Is(int li, Symbol value)
		{
			return LT(li).Value == value;
		}
		private LNode MethodBodyOrForward()
		{
			LNode _;
			return MethodBodyOrForward(false, out _);
		}
		bool IsArrayType(LNode type)
		{
			return type.Calls(S.Of, 2) && S.IsArrayKeyword(type.Args[0].Name);
		}
		LNode ArgList(Token lp, Token rp)
		{
			var list = new WList<LNode>();
			if ((Down(lp.Children))) {
				ArgList(list);
				Up();
			}
			return F.List(list.ToVList(), lp.StartIndex, rp.EndIndex);
		}
		int ColumnOf(int index)
		{
			return _sourceFile.IndexToLine(index).PosInLine;
		}
		LNode MissingHere()
		{
			var i = GetTextPosition(InputPosition);
			return F.Id(S.Missing, i, i);
		}
		Token UnusualId()
		{
			Check(!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value)), "!(_insideLinqExpr && LinqKeywords.Contains(LT($LI).Value))");
			var t = Match((int) TT.ContextualKeyword);
			#line 107 "EcsParserGrammar.les"
			return t;
			#line default
		}
		bool Scan_UnusualId()
		{
			if (_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword))
				return false;
			return true;
		}
		LNode DataType(bool afterAsOrIs, out Token? majorDimension)
		{
			LNode result = default(LNode);
			result = ComplexId();
			TypeSuffixOpt(afterAsOrIs, out majorDimension, ref result);
			return result;
		}
		bool Try_Scan_DataType(int lookaheadAmt, bool afterAsOrIs = false)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DataType(afterAsOrIs);
		}
		bool Scan_DataType(bool afterAsOrIs = false)
		{
			if (!Scan_ComplexId())
				return false;
			if (!Scan_TypeSuffixOpt(afterAsOrIs))
				return false;
			return true;
		}
		LNode ComplexId()
		{
			TokenType la0, la1;
			var e = IdAtom();
			// Line 151: (TT.ColonColon IdAtom)?
			do {
				la0 = LA0;
				if (la0 == TT.ColonColon) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1;
						}
						break;
					}
				}
				break;
			match1:
				{
					Skip();
					var e2 = IdAtom();
					#line 152 "EcsParserGrammar.les"
					e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
					#line default
				}
			} while (false);
			// Line 154: (TParams)?
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.@operator:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					TParams(ref e);
					break;
				case TT.ContextualKeyword:
					{
						if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
							TParams(ref e);
					}
					break;
				case TT.GT:
					TParams(ref e);
					break;
				}
			} else if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.LBrack)
					TParams(ref e);
			} else if (la0 == TT.Not) {
				la1 = LA(1);
				if (la1 == TT.LParen)
					TParams(ref e);
			}
			// Line 155: (TT.Dot IdAtom (TParams)?)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1_a;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1_a;
							else
								goto stop;
						}
					default:
						goto stop;
					}
				} else
					goto stop;
			match1_a:
				{
					Skip();
					var rhs = IdAtom();
					#line 155 "EcsParserGrammar.les"
					e = F.Dot(e, rhs);
					#line default
					// Line 156: (TParams)?
					la0 = LA0;
					if (la0 == TT.LT) {
						switch (LA(1)) {
						case TT.@operator:
						case TT.Id:
						case TT.Substitute:
						case TT.TypeKeyword:
							TParams(ref e);
							break;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									TParams(ref e);
							}
							break;
						case TT.GT:
							TParams(ref e);
							break;
						}
					} else if (la0 == TT.Dot) {
						la1 = LA(1);
						if (la1 == TT.LBrack)
							TParams(ref e);
					} else if (la0 == TT.Not) {
						la1 = LA(1);
						if (la1 == TT.LParen)
							TParams(ref e);
					}
				}
			}
		stop:;
			#line 158 "EcsParserGrammar.les"
			return e;
			#line default
		}
		bool Scan_ComplexId()
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			// Line 151: (TT.ColonColon IdAtom)?
			do {
				la0 = LA0;
				if (la0 == TT.ColonColon) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1;
						}
						break;
					}
				}
				break;
			match1:
				{
					if (!TryMatch((int) TT.ColonColon))
						return false;
					if (!Scan_IdAtom())
						return false;
				}
			} while (false);
			// Line 154: (TParams)?
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto matchTParams;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto matchTParams;
						}
						break;
					case TT.GT:
						goto matchTParams;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						goto matchTParams;
				} else if (la0 == TT.Not) {
					la1 = LA(1);
					if (la1 == TT.LParen)
						goto matchTParams;
				}
				break;
			matchTParams:
				{
					if (!Scan_TParams())
						return false;
				}
			} while (false);
			// Line 155: (TT.Dot IdAtom (TParams)?)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1_a;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1_a;
							else
								goto stop;
						}
					default:
						goto stop;
					}
				} else
					goto stop;
			match1_a:
				{
					if (!TryMatch((int) TT.Dot))
						return false;
					if (!Scan_IdAtom())
						return false;
					// Line 156: (TParams)?
					do {
						la0 = LA0;
						if (la0 == TT.LT) {
							switch (LA(1)) {
							case TT.@operator:
							case TT.Id:
							case TT.Substitute:
							case TT.TypeKeyword:
								goto matchTParams_a;
							case TT.ContextualKeyword:
								{
									if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
										goto matchTParams_a;
								}
								break;
							case TT.GT:
								goto matchTParams_a;
							}
						} else if (la0 == TT.Dot) {
							la1 = LA(1);
							if (la1 == TT.LBrack)
								goto matchTParams_a;
						} else if (la0 == TT.Not) {
							la1 = LA(1);
							if (la1 == TT.LParen)
								goto matchTParams_a;
						}
						break;
					matchTParams_a:
						{
							if (!Scan_TParams())
								return false;
						}
					} while (false);
				}
			}
		stop:;
			return true;
		}
		LNode IdAtom()
		{
			#line 163 "EcsParserGrammar.les"
			LNode r;
			#line default
			// Line 164: ( TT.Substitute Atom | TT.@operator AnyOperator | (TT.Id|TT.TypeKeyword) | UnusualId )
			switch (LA0) {
			case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					e = AutoRemoveParens(e);
					r = F.Call(S.Substitute, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.@operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					#line 167 "EcsParserGrammar.les"
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
					#line default
				}
				break;
			case TT.Id:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					#line 169 "EcsParserGrammar.les"
					r = F.Id(t);
					#line default
				}
				break;
			default:
				{
					var t = UnusualId();
					#line 171 "EcsParserGrammar.les"
					r = F.Id(t);
					#line default
				}
				break;
			}
			#line 172 "EcsParserGrammar.les"
			return r;
			#line default
		}
		bool Scan_IdAtom()
		{
			// Line 164: ( TT.Substitute Atom | TT.@operator AnyOperator | (TT.Id|TT.TypeKeyword) | UnusualId )
			switch (LA0) {
			case TT.Substitute:
				{
					if (!TryMatch((int) TT.Substitute))
						return false;
					if (!Scan_Atom())
						return false;
				}
				break;
			case TT.@operator:
				{
					if (!TryMatch((int) TT.@operator))
						return false;
					if (!Scan_AnyOperator())
						return false;
				}
				break;
			case TT.Id:
			case TT.TypeKeyword:
				if (!TryMatch((int) TT.Id, (int) TT.TypeKeyword))
					return false;
				break;
			default:
				if (!Scan_UnusualId())
					return false;
				break;
			}
			return true;
		}
		void TParams(ref LNode r)
		{
			TokenType la0;
			WList<LNode> list = new WList<LNode> { 
				r
			};
			Token end;
			// Line 189: ( TT.LT (DataType (TT.Comma DataType)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				// Line 189: (DataType (TT.Comma DataType)*)?
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						list.Add(DataType());
						// Line 189: (TT.Comma DataType)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								Skip();
								list.Add(DataType());
							} else
								break;
						}
					}
					break;
				}
				end = Match((int) TT.GT);
			} else if (la0 == TT.Dot) {
				Skip();
				var t = Match((int) TT.LBrack);
				end = Match((int) TT.RBrack);
				#line 190 "EcsParserGrammar.les"
				list = AppendExprsInside(t, list);
				#line default
			} else {
				Match((int) TT.Not);
				var t = Match((int) TT.LParen);
				end = Match((int) TT.RParen);
				#line 191 "EcsParserGrammar.les"
				list = AppendExprsInside(t, list);
				#line default
			}
			#line 194 "EcsParserGrammar.les"
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list.ToVList(), start, end.EndIndex);
			#line default
		}
		bool Try_Scan_TParams(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParams();
		}
		bool Scan_TParams()
		{
			TokenType la0;
			// Line 189: ( TT.LT (DataType (TT.Comma DataType)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
				// Line 189: (DataType (TT.Comma DataType)*)?
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!Scan_DataType())
							return false;
						// Line 189: (TT.Comma DataType)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								if (!TryMatch((int) TT.Comma))
									return false;
								if (!Scan_DataType())
									return false;
							} else
								break;
						}
					}
					break;
				}
				if (!TryMatch((int) TT.GT))
					return false;
			} else if (la0 == TT.Dot) {
				if (!TryMatch((int) TT.Dot))
					return false;
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
			} else {
				if (!TryMatch((int) TT.Not))
					return false;
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
			}
			return true;
		}
		bool TypeSuffixOpt(bool afterAsOrIs, out Token? dimensionBrack, ref LNode e)
		{
			TokenType la0, la1;
			#line 204 "EcsParserGrammar.les"
			int count;
			#line 204 "EcsParserGrammar.les"
			bool result = false;
			dimensionBrack = null;
			#line default
			// Line 237: greedy( TT.QuestionMark (&!{afterAsOrIs} | &!(((TT.@new|TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | UnusualId))) | TT.Mul | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (!afterAsOrIs)
						goto match1;
					else if ((count = CountDims(LT(1), true)) > 0) {
						if (!Try_TypeSuffixOpt_Test0(1))
							goto match1;
						else
							break;
					} else if (!Try_TypeSuffixOpt_Test0(1))
						goto match1;
					else
						break;
				} else if (la0 == TT.Mul) {
					var t = MatchAny();
					#line 243 "EcsParserGrammar.les"
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					#line 243 "EcsParserGrammar.les"
					result = true;
					#line default
				} else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var dims = InternalList<Pair<int,int>>.Empty;
							Token rb;
							var lb = MatchAny();
							rb = MatchAny();
							#line 248 "EcsParserGrammar.les"
							dims.Add(Pair.Create(count, rb.EndIndex));
							#line default
							// Line 249: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											Skip();
											rb = MatchAny();
											#line 249 "EcsParserGrammar.les"
											dims.Add(Pair.Create(count, rb.EndIndex));
											#line default
										} else
											break;
									} else
										break;
								} else
									break;
							}
							#line 251 "EcsParserGrammar.les"
							if (CountDims(lb, false) <= 0) {
								dimensionBrack = lb;
							}
							#line 254 "EcsParserGrammar.les"
							for (int i = dims.Count - 1; i >= 0; i--) {
								e = F.Of(F.Id(S.GetArrayKeyword(dims[i].A)), e, e.Range.StartIndex, dims[i].B);
							}
							#line 257 "EcsParserGrammar.les"
							result = true;
							#line default
						} else
							break;
					} else
						break;
				} else
					break;
				continue;
			match1:
				{
					var t = MatchAny();
					// Line 237: (&!{afterAsOrIs} | &!(((TT.@new|TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | UnusualId)))
					if (!afterAsOrIs) {
					} else
						Check(!Try_TypeSuffixOpt_Test0(0), "!(((TT.@new|TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | UnusualId))");
					#line 240 "EcsParserGrammar.les"
					e = F.Of(F.Id(t), e, e.Range.StartIndex, t.EndIndex);
					#line 240 "EcsParserGrammar.les"
					result = true;
					#line default
				}
			}
			#line 260 "EcsParserGrammar.les"
			return result;
			#line default
		}
		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt, bool afterAsOrIs)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt(afterAsOrIs);
		}
		bool Scan_TypeSuffixOpt(bool afterAsOrIs)
		{
			TokenType la0, la1;
			// Line 237: greedy( TT.QuestionMark (&!{afterAsOrIs} | &!(((TT.@new|TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | UnusualId))) | TT.Mul | &{(count = CountDims(LT($LI), @true)) > 0} TT.LBrack TT.RBrack greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)* )*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (!afterAsOrIs)
						goto match1;
					else if ((count = CountDims(LT(1), true)) > 0) {
						if (!Try_TypeSuffixOpt_Test0(1))
							goto match1;
						else
							break;
					} else if (!Try_TypeSuffixOpt_Test0(1))
						goto match1;
					else
						break;
				} else if (la0 == TT.Mul)
					{if (!TryMatch((int) TT.Mul))
						return false;}
				else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							if (!TryMatch((int) TT.LBrack))
								return false;
							if (!TryMatch((int) TT.RBrack))
								return false;
							// Line 249: greedy(&{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack)*
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											if (!TryMatch((int) TT.LBrack))
												return false;
											if (!TryMatch((int) TT.RBrack))
												return false;
										} else
											break;
									} else
										break;
								} else
									break;
							}
						} else
							break;
					} else
						break;
				} else
					break;
				continue;
			match1:
				{
					if (!TryMatch((int) TT.QuestionMark))
						return false;
					// Line 237: (&!{afterAsOrIs} | &!(((TT.@new|TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | UnusualId)))
					if (!afterAsOrIs) {
					} else if (Try_TypeSuffixOpt_Test0(0))
						return false;
				}
			}
			return true;
		}
		LNode ComplexNameDecl()
		{
			TokenType la0, la1;
			var e = IdAtom();
			// Line 270: (TT.ColonColon IdAtom)?
			do {
				la0 = LA0;
				if (la0 == TT.ColonColon) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1;
						}
						break;
					}
				}
				break;
			match1:
				{
					Skip();
					var e2 = IdAtom();
					#line 271 "EcsParserGrammar.les"
					e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
					#line default
				}
			} while (false);
			// Line 273: (TParamsDecl)?
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.LBrack:
					{
						if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
							TParamsDecl(ref e);
					}
					break;
				case TT.@in:
				case TT.@operator:
				case TT.AttrKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					TParamsDecl(ref e);
					break;
				case TT.ContextualKeyword:
					{
						if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
							TParamsDecl(ref e);
					}
					break;
				case TT.GT:
					TParamsDecl(ref e);
					break;
				}
			} else if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.LBrack)
					TParamsDecl(ref e);
			} else if (la0 == TT.Not) {
				la1 = LA(1);
				if (la1 == TT.LParen)
					TParamsDecl(ref e);
			}
			// Line 274: (TT.Dot IdAtom (TParamsDecl)?)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1_a;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1_a;
							else
								goto stop;
						}
					default:
						goto stop;
					}
				} else
					goto stop;
			match1_a:
				{
					Skip();
					var rhs = IdAtom();
					#line 274 "EcsParserGrammar.les"
					e = F.Dot(e, rhs);
					#line default
					// Line 275: (TParamsDecl)?
					la0 = LA0;
					if (la0 == TT.LT) {
						switch (LA(1)) {
						case TT.LBrack:
							{
								if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
									TParamsDecl(ref e);
							}
							break;
						case TT.@in:
						case TT.@operator:
						case TT.AttrKeyword:
						case TT.Id:
						case TT.Substitute:
						case TT.TypeKeyword:
							TParamsDecl(ref e);
							break;
						case TT.ContextualKeyword:
							{
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
									TParamsDecl(ref e);
							}
							break;
						case TT.GT:
							TParamsDecl(ref e);
							break;
						}
					} else if (la0 == TT.Dot) {
						la1 = LA(1);
						if (la1 == TT.LBrack)
							TParamsDecl(ref e);
					} else if (la0 == TT.Not) {
						la1 = LA(1);
						if (la1 == TT.LParen)
							TParamsDecl(ref e);
					}
				}
			}
		stop:;
			#line 277 "EcsParserGrammar.les"
			return e;
			#line default
		}
		bool Scan_ComplexNameDecl()
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			// Line 270: (TT.ColonColon IdAtom)?
			do {
				la0 = LA0;
				if (la0 == TT.ColonColon) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1;
						}
						break;
					}
				}
				break;
			match1:
				{
					if (!TryMatch((int) TT.ColonColon))
						return false;
					if (!Scan_IdAtom())
						return false;
				}
			} while (false);
			// Line 273: (TParamsDecl)?
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.LBrack:
						{
							if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
								goto matchTParamsDecl;
						}
						break;
					case TT.@in:
					case TT.@operator:
					case TT.AttrKeyword:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto matchTParamsDecl;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto matchTParamsDecl;
						}
						break;
					case TT.GT:
						goto matchTParamsDecl;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						goto matchTParamsDecl;
				} else if (la0 == TT.Not) {
					la1 = LA(1);
					if (la1 == TT.LParen)
						goto matchTParamsDecl;
				}
				break;
			matchTParamsDecl:
				{
					if (!Scan_TParamsDecl())
						return false;
				}
			} while (false);
			// Line 274: (TT.Dot IdAtom (TParamsDecl)?)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1_a;
					case TT.ContextualKeyword:
						{
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
								goto match1_a;
							else
								goto stop;
						}
					default:
						goto stop;
					}
				} else
					goto stop;
			match1_a:
				{
					if (!TryMatch((int) TT.Dot))
						return false;
					if (!Scan_IdAtom())
						return false;
					// Line 275: (TParamsDecl)?
					do {
						la0 = LA0;
						if (la0 == TT.LT) {
							switch (LA(1)) {
							case TT.LBrack:
								{
									if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
										goto matchTParamsDecl_a;
								}
								break;
							case TT.@in:
							case TT.@operator:
							case TT.AttrKeyword:
							case TT.Id:
							case TT.Substitute:
							case TT.TypeKeyword:
								goto matchTParamsDecl_a;
							case TT.ContextualKeyword:
								{
									if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value)))
										goto matchTParamsDecl_a;
								}
								break;
							case TT.GT:
								goto matchTParamsDecl_a;
							}
						} else if (la0 == TT.Dot) {
							la1 = LA(1);
							if (la1 == TT.LBrack)
								goto matchTParamsDecl_a;
						} else if (la0 == TT.Not) {
							la1 = LA(1);
							if (la1 == TT.LParen)
								goto matchTParamsDecl_a;
						}
						break;
					matchTParamsDecl_a:
						{
							if (!Scan_TParamsDecl())
								return false;
						}
					} while (false);
				}
			}
		stop:;
			return true;
		}
		LNode ComplexThisDecl()
		{
			TokenType la0;
			var t = Match((int) TT.@this);
			#line 281 "EcsParserGrammar.les"
			var e = F.Id(t);
			#line default
			// Line 282: (TParamsDecl)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.@in:
				case TT.@operator:
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.GT:
				case TT.Id:
				case TT.LBrack:
				case TT.LParen:
				case TT.Substitute:
				case TT.TypeKeyword:
					TParamsDecl(ref e);
					break;
				}
			}
			#line 283 "EcsParserGrammar.les"
			return e;
			#line default
		}
		bool Scan_ComplexThisDecl()
		{
			TokenType la0;
			if (!TryMatch((int) TT.@this))
				return false;
			// Line 282: (TParamsDecl)?
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.@in:
				case TT.@operator:
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.GT:
				case TT.Id:
				case TT.LBrack:
				case TT.LParen:
				case TT.Substitute:
				case TT.TypeKeyword:
					if (!Scan_TParamsDecl())
						return false;
					break;
				}
			}
			return true;
		}
		void TParamsDecl(ref LNode r)
		{
			TokenType la0;
			WList<LNode> list = new WList<LNode> { 
				r
			};
			Token end;
			// Line 291: ( TT.LT (TParamDecl (TT.Comma TParamDecl)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				// Line 291: (TParamDecl (TT.Comma TParamDecl)*)?
				switch (LA0) {
				case TT.@in:
				case TT.@operator:
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.LBrack:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						list.Add(TParamDecl());
						// Line 291: (TT.Comma TParamDecl)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								Skip();
								list.Add(TParamDecl());
							} else
								break;
						}
					}
					break;
				}
				end = Match((int) TT.GT);
			} else if (la0 == TT.Dot) {
				Skip();
				var t = Match((int) TT.LBrack);
				end = Match((int) TT.RBrack);
				#line 292 "EcsParserGrammar.les"
				list = AppendExprsInside(t, list);
				#line default
			} else {
				Match((int) TT.Not);
				var t = Match((int) TT.LParen);
				end = Match((int) TT.RParen);
				#line 293 "EcsParserGrammar.les"
				list = AppendExprsInside(t, list);
				#line default
			}
			#line 296 "EcsParserGrammar.les"
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list.ToVList(), start, end.EndIndex);
			#line default
		}
		bool Try_Scan_TParamsDecl(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamsDecl();
		}
		bool Scan_TParamsDecl()
		{
			TokenType la0;
			// Line 291: ( TT.LT (TParamDecl (TT.Comma TParamDecl)*)? TT.GT | TT.Dot TT.LBrack TT.RBrack | TT.Not TT.LParen TT.RParen )
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
				// Line 291: (TParamDecl (TT.Comma TParamDecl)*)?
				switch (LA0) {
				case TT.@in:
				case TT.@operator:
				case TT.AttrKeyword:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.LBrack:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!Scan_TParamDecl())
							return false;
						// Line 291: (TT.Comma TParamDecl)*
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								if (!TryMatch((int) TT.Comma))
									return false;
								if (!Scan_TParamDecl())
									return false;
							} else
								break;
						}
					}
					break;
				}
				if (!TryMatch((int) TT.GT))
					return false;
			} else if (la0 == TT.Dot) {
				if (!TryMatch((int) TT.Dot))
					return false;
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
			} else {
				if (!TryMatch((int) TT.Not))
					return false;
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
			}
			return true;
		}
		LNode TParamDecl()
		{
			#line 302 "EcsParserGrammar.les"
			WList<LNode> attrs = null;
			#line 302 "EcsParserGrammar.les"
			int startIndex = GetTextPosition(InputPosition);
			#line default
			NormalAttributes(ref attrs);
			TParamAttributeKeywords(ref attrs);
			var node = IdAtom();
			#line 307 "EcsParserGrammar.les"
			if ((attrs != null)) {
				node = node.WithAttrs(attrs.ToVList());
			}
			return node;
			#line default
		}
		bool Scan_TParamDecl()
		{
			if (!Scan_NormalAttributes())
				return false;
			if (!Scan_TParamAttributeKeywords())
				return false;
			if (!Scan_IdAtom())
				return false;
			return true;
		}
		LNode Atom()
		{
			TokenType la0, la1;
			#line 379 "EcsParserGrammar.les"
			LNode r;
			#line default
			// Line 380: ( (TT.Dot|TT.Substitute) Atom | TT.@operator AnyOperator | (@`.`(TT, noMacro(@base))|@`.`(TT, noMacro(@this))|TT.Id|TT.TypeKeyword) | UnusualId | TT.Literal | ExprInParensAuto | BracedBlock | NewExpr | TokenLiteral | (TT.@checked|TT.@unchecked) TT.LParen TT.RParen | (@`.`(TT, noMacro(@default))|TT.@sizeof|TT.@typeof) TT.LParen TT.RParen | TT.@delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.@is DataType )
			switch (LA0) {
			case TT.Dot:
			case TT.Substitute:
				{
					var t = MatchAny();
					var e = Atom();
					e = AutoRemoveParens(e);
					r = F.Call((Symbol) t.Value, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.@operator:
				{
					var op = MatchAny();
					var t = AnyOperator();
					#line 383 "EcsParserGrammar.les"
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
					#line default
				}
				break;
			case TT.@base:
			case TT.@this:
			case TT.Id:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					#line 385 "EcsParserGrammar.les"
					r = F.Id(t);
					#line default
				}
				break;
			case TT.ContextualKeyword:
				{
					var t = UnusualId();
					#line 387 "EcsParserGrammar.les"
					r = F.Id(t);
					#line default
				}
				break;
			case TT.Literal:
				{
					var t = MatchAny();
					#line 389 "EcsParserGrammar.les"
					r = F.Literal(t.Value, t.StartIndex, t.EndIndex);
					#line default
				}
				break;
			case TT.LParen:
				r = ExprInParensAuto();
				break;
			case TT.LBrace:
				r = BracedBlock();
				break;
			case TT.@new:
				r = NewExpr();
				break;
			case TT.At:
				r = TokenLiteral();
				break;
			case TT.@checked:
			case TT.@unchecked:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					#line 396 "EcsParserGrammar.les"
					r = F.Call((Symbol) t.Value, ExprListInside(args), t.StartIndex, rp.EndIndex);
					#line default
				}
				break;
			case TT.@default:
			case TT.@sizeof:
			case TT.@typeof:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					#line 399 "EcsParserGrammar.les"
					r = F.Call((Symbol) t.Value, TypeInside(args), t.StartIndex, rp.EndIndex);
					#line default
				}
				break;
			case TT.@delegate:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					Match((int) TT.RParen);
					var block = Match((int) TT.LBrace);
					var rb = Match((int) TT.RBrace);
					#line 401 "EcsParserGrammar.les"
					r = F.Call(S.Lambda, F.List(ExprListInside(args).ToVList()), F.Braces(StmtListInside(block).ToVList(), block.StartIndex, rb.EndIndex), t.StartIndex, rb.EndIndex);
					#line default
				}
				break;
			case TT.@is:
				{
					var t = MatchAny();
					var dt = DataType();
					#line 403 "EcsParserGrammar.les"
					r = F.Call(S.Is, dt, t.StartIndex, dt.Range.EndIndex);
					#line default
				}
				break;
			default:
				{
					#line 404 "EcsParserGrammar.les"
					r = Error("Invalid expression. Expected (parentheses), {braces}, identifier, literal, or $substitution.");
					#line default
					// Line 404: greedy(~(EOF|TT.Comma|TT.Semicolon))*
					for (;;) {
						la0 = LA0;
						if (!(la0 == (TokenType) EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != (TokenType) EOF)
								Skip();
							else
								break;
						} else
							break;
					}
				}
				break;
			}
			#line 406 "EcsParserGrammar.les"
			return r;
			#line default
		}
		bool Scan_Atom()
		{
			TokenType la0, la1;
			// Line 380: ( (TT.Dot|TT.Substitute) Atom | TT.@operator AnyOperator | (@`.`(TT, noMacro(@base))|@`.`(TT, noMacro(@this))|TT.Id|TT.TypeKeyword) | UnusualId | TT.Literal | ExprInParensAuto | BracedBlock | NewExpr | TokenLiteral | (TT.@checked|TT.@unchecked) TT.LParen TT.RParen | (@`.`(TT, noMacro(@default))|TT.@sizeof|TT.@typeof) TT.LParen TT.RParen | TT.@delegate TT.LParen TT.RParen TT.LBrace TT.RBrace | TT.@is DataType )
			switch (LA0) {
			case TT.Dot:
			case TT.Substitute:
				{
					if (!TryMatch((int) TT.Dot, (int) TT.Substitute))
						return false;
					if (!Scan_Atom())
						return false;
				}
				break;
			case TT.@operator:
				{
					if (!TryMatch((int) TT.@operator))
						return false;
					if (!Scan_AnyOperator())
						return false;
				}
				break;
			case TT.@base:
			case TT.@this:
			case TT.Id:
			case TT.TypeKeyword:
				if (!TryMatch((int) TT.@base, (int) TT.@this, (int) TT.Id, (int) TT.TypeKeyword))
					return false;
				break;
			case TT.ContextualKeyword:
				if (!Scan_UnusualId())
					return false;
				break;
			case TT.Literal:
				if (!TryMatch((int) TT.Literal))
					return false;
				break;
			case TT.LParen:
				if (!Scan_ExprInParensAuto())
					return false;
				break;
			case TT.LBrace:
				if (!Scan_BracedBlock())
					return false;
				break;
			case TT.@new:
				if (!Scan_NewExpr())
					return false;
				break;
			case TT.At:
				if (!Scan_TokenLiteral())
					return false;
				break;
			case TT.@checked:
			case TT.@unchecked:
				{
					if (!TryMatch((int) TT.@checked, (int) TT.@unchecked))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.@default:
			case TT.@sizeof:
			case TT.@typeof:
				{
					if (!TryMatch((int) TT.@default, (int) TT.@sizeof, (int) TT.@typeof))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			case TT.@delegate:
				{
					if (!TryMatch((int) TT.@delegate))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
					if (!TryMatch((int) TT.LBrace))
						return false;
					if (!TryMatch((int) TT.RBrace))
						return false;
				}
				break;
			case TT.@is:
				{
					if (!TryMatch((int) TT.@is))
						return false;
					if (!Scan_DataType())
						return false;
				}
				break;
			default:
				{
					// Line 404: greedy(~(EOF|TT.Comma|TT.Semicolon))*
					for (;;) {
						la0 = LA0;
						if (!(la0 == (TokenType) EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != (TokenType) EOF)
								{if (!TryMatchExcept((int) TT.Comma, (int) TT.Semicolon))
									return false;}
							else
								break;
						} else
							break;
					}
				}
				break;
			}
			return true;
		}
		static readonly HashSet<int> AnyOperator_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.XorBits);
		Token AnyOperator()
		{
			var op = Match(AnyOperator_set0);
			#line 416 "EcsParserGrammar.les"
			return op;
			#line default
		}
		bool Scan_AnyOperator()
		{
			if (!TryMatch(AnyOperator_set0))
				return false;
			return true;
		}
		LNode NewExpr()
		{
			TokenType la0, la1;
			#line 421 "EcsParserGrammar.les"
			Token? majorDimension = null;
			int endIndex;
			var list = new WList<LNode>();
			#line default
			var op = Match((int) TT.@new);
			// Line 427: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				Check((count = CountDims(LT(0), false)) > 0, "(count = CountDims(LT($LI), @false)) > 0");
				var lb = MatchAny();
				var rb = Match((int) TT.RBrack);
				#line 429 "EcsParserGrammar.les"
				var type = F.Id(S.GetArrayKeyword(count), lb.StartIndex, rb.EndIndex);
				#line default
				lb = Match((int) TT.LBrace);
				rb = Match((int) TT.RBrace);
				#line 432 "EcsParserGrammar.les"
				list.Add(LNode.Call(type, type.Range));
				AppendInitializersInside(lb, list);
				endIndex = rb.EndIndex;
				#line default
			} else if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				#line 439 "EcsParserGrammar.les"
				list.Add(F.Missing);
				AppendInitializersInside(lb, list);
				endIndex = rb.EndIndex;
				#line default
			} else {
				var type = DataType(false, out majorDimension);
				// Line 451: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var lp = MatchAny();
							var rp = MatchAny();
							#line 453 "EcsParserGrammar.les"
							if ((majorDimension != null)) {
								Error("Syntax error: unexpected constructor argument list (...)");
							}
							#line 456 "EcsParserGrammar.les"
							list.Add(F.Call(type, ExprListInside(lp).ToVList(), type.Range.StartIndex, rp.EndIndex));
							endIndex = rp.EndIndex;
							#line default
							// Line 459: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									var lb = MatchAny();
									var rb = MatchAny();
									#line 461 "EcsParserGrammar.les"
									AppendInitializersInside(lb, list);
									endIndex = rb.EndIndex;
									#line default
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						#line 468 "EcsParserGrammar.les"
						Token lb = op, rb = op;
						#line 468 "EcsParserGrammar.les"
						bool haveBraces = false;
						#line default
						// Line 469: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								lb = MatchAny();
								rb = MatchAny();
								#line 469 "EcsParserGrammar.les"
								haveBraces = true;
								#line default
							}
						}
						#line 471 "EcsParserGrammar.les"
						if ((majorDimension != null)) {
							list.Add(LNode.Call(type, ExprListInside(majorDimension.Value).ToVList(), type.Range));
						} else {
							list.Add(LNode.Call(type, type.Range));
						}
						#line 476 "EcsParserGrammar.les"
						if ((haveBraces)) {
							AppendInitializersInside(lb, list);
							endIndex = rb.EndIndex;
						} else {
							endIndex = type.Range.EndIndex;
						}
						#line 482 "EcsParserGrammar.les"
						if ((!haveBraces && majorDimension == null)) {
							if (IsArrayType(type)) {
								Error("Syntax error: missing array size expression");
							} else {
								Error("Syntax error: expected constructor argument list (...) or initializers {...}");
							}
						}
						#line default
					}
				} while (false);
			}
			#line 492 "EcsParserGrammar.les"
			return F.Call(S.New, list.ToVList(), op.StartIndex, endIndex);
			#line default
		}
		bool Scan_NewExpr()
		{
			TokenType la0, la1;
			if (!TryMatch((int) TT.@new))
				return false;
			// Line 427: ( &{(count = CountDims(LT($LI), @false)) > 0} TT.LBrack TT.RBrack TT.LBrace TT.RBrace | TT.LBrace TT.RBrace | DataType (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?) )
			la0 = LA0;
			if (la0 == TT.LBrack) {
				if (!((count = CountDims(LT(0), false)) > 0))
					return false;
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else if (la0 == TT.LBrace) {
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else {
				if (!Scan_DataType(false))
					return false;
				// Line 451: (TT.LParen TT.RParen (TT.LBrace TT.RBrace)? / (TT.LBrace TT.RBrace)?)
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							if (!TryMatch((int) TT.LParen))
								return false;
							if (!TryMatch((int) TT.RParen))
								return false;
							// Line 459: (TT.LBrace TT.RBrace)?
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									if (!TryMatch((int) TT.LBrace))
										return false;
									if (!TryMatch((int) TT.RBrace))
										return false;
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						// Line 469: (TT.LBrace TT.RBrace)?
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								if (!TryMatch((int) TT.LBrace))
									return false;
								if (!TryMatch((int) TT.RBrace))
									return false;
							}
						}
					}
				} while (false);
			}
			return true;
		}
		LNode ExprInParensAuto()
		{
			// Line 506: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0)) {
				var r = ExprInParens(true);
				#line 507 "EcsParserGrammar.les"
				return r;
				#line default
			} else {
				var r = ExprInParens(false);
				#line 508 "EcsParserGrammar.les"
				return r;
				#line default
			}
		}
		bool Scan_ExprInParensAuto()
		{
			// Line 506: (&(ExprInParens (TT.LambdaArrow|TT.Set)) ExprInParens / ExprInParens)
			if (Try_ExprInParensAuto_Test0(0))
				{if (!Scan_ExprInParens(true))
					return false;}
			else if (!Scan_ExprInParens(false))
				return false;
			return true;
		}
		LNode TokenLiteral()
		{
			TokenType la0;
			Token at = default(Token);
			Token L = default(Token);
			Token R = default(Token);
			at = MatchAny();
			// Line 513: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				L = MatchAny();
				R = Match((int) TT.RBrack);
			} else {
				L = Match((int) TT.LBrace);
				R = Match((int) TT.RBrace);
			}
			#line 514 "EcsParserGrammar.les"
			return F.Literal(L.Children, at.StartIndex, R.EndIndex);
			#line default
		}
		bool Scan_TokenLiteral()
		{
			TokenType la0;
			if (!TryMatch((int) TT.At))
				return false;
			// Line 513: (TT.LBrack TT.RBrack | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.LBrack) {
				if (!TryMatch((int) TT.LBrack))
					return false;
				if (!TryMatch((int) TT.RBrack))
					return false;
			} else {
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			}
			return true;
		}
		LNode PrimaryExpr()
		{
			TokenType la0;
			var e = Atom();
			FinishPrimaryExpr(ref e);
			// Line 521: (TT.NullDot PrimaryExpr)?
			la0 = LA0;
			if (la0 == TT.NullDot) {
				Skip();
				var rhs = PrimaryExpr();
				#line 521 "EcsParserGrammar.les"
				e = F.Call(S.NullDot, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
				#line default
			}
			#line 523 "EcsParserGrammar.les"
			return e;
			#line default
		}
		void FinishPrimaryExpr(ref LNode e)
		{
			TokenType la1;
			// Line 528: greedy( (TT.ColonColon|TT.Dot|TT.PtrArrow|TT.QuickBind) Atom / PrimaryExpr_NewStyleCast / TT.LParen TT.RParen | TT.LBrack TT.RBrack | TT.QuestionMark TT.LBrack TT.RBrack | TT.IncDec | &(TParams ~(TT.ContextualKeyword|TT.Id)) ((TT.LT|TT.Not) | TT.Dot TT.LBrack) => TParams | BracedBlock )*
			for (;;) {
				switch (LA0) {
				case TT.Dot:
					{
						if (Try_FinishPrimaryExpr_Test0(0)) {
							switch (LA(1)) {
							case TT.@base:
							case TT.@default:
							case TT.@this:
							case TT.@checked:
							case TT.@delegate:
							case TT.@is:
							case TT.@new:
							case TT.@operator:
							case TT.@sizeof:
							case TT.@typeof:
							case TT.@unchecked:
							case TT.At:
							case TT.ContextualKeyword:
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.Literal:
							case TT.LParen:
							case TT.Substitute:
							case TT.TypeKeyword:
								goto match1;
							default:
								TParams(ref e);
								break;
							}
						} else
							goto match1;
					}
					break;
				case TT.ColonColon:
				case TT.PtrArrow:
				case TT.QuickBind:
					goto match1;
				case TT.LParen:
					{
						if (Down(0) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow))
							e = PrimaryExpr_NewStyleCast(e);
						else {
							var lp = MatchAny();
							var rp = Match((int) TT.RParen);
							#line 532 "EcsParserGrammar.les"
							e = F.Call(e, ExprListInside(lp), e.Range.StartIndex, rp.EndIndex);
							#line default
						}
					}
					break;
				case TT.LBrack:
					{
						var lb = MatchAny();
						var rb = Match((int) TT.RBrack);
						var list = new WList<LNode> { 
							e
						};
						e = F.Call(S.IndexBracks, AppendExprsInside(lb, list).ToVList(), e.Range.StartIndex, rb.EndIndex);
					}
					break;
				case TT.QuestionMark:
					{
						la1 = LA(1);
						if (la1 == TT.LBrack) {
							var t = MatchAny();
							var lb = MatchAny();
							var rb = Match((int) TT.RBrack);
							#line 550 "EcsParserGrammar.les"
							e = F.Call(S.NullIndexBracks, e, F.List(ExprListInside(lb).ToVList()), e.Range.StartIndex, rb.EndIndex);
							#line default
						} else
							goto stop;
					}
					break;
				case TT.IncDec:
					{
						var t = MatchAny();
						#line 552 "EcsParserGrammar.les"
						e = F.Call(t.Value == S.PreInc ? S.PostInc : S.PostDec, e, e.Range.StartIndex, t.EndIndex);
						#line default
					}
					break;
				case TT.LT:
					{
						if (Try_FinishPrimaryExpr_Test0(0))
							TParams(ref e);
						else
							goto stop;
					}
					break;
				case TT.Not:
					TParams(ref e);
					break;
				case TT.LBrace:
					{
						la1 = LA(1);
						if (la1 == TT.RBrace) {
							var bb = BracedBlock();
							#line 556 "EcsParserGrammar.les"
							if ((!e.IsCall || e.BaseStyle == NodeStyle.Operator)) {
								e = F.Call(e, bb, e.Range.StartIndex, bb.Range.EndIndex);
							} else {
								e = e.WithArgs(e.Args.Add(bb)).WithRange(e.Range.StartIndex, bb.Range.EndIndex);
							}
							#line default
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
				continue;
			match1:
				{
					var op = MatchAny();
					var rhs = Atom();
					#line 529 "EcsParserGrammar.les"
					e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
					#line default
				}
			}
		stop:;
		}
		LNode PrimaryExpr_NewStyleCast(LNode e)
		{
			TokenType la0;
			var lp = MatchAny();
			var rp = Match((int) TT.RParen);
			Down(lp);
			Symbol kind;
			WList<LNode> attrs = null;
			// Line 572: ( TT.PtrArrow | TT.@as | TT.@using )
			la0 = LA0;
			if (la0 == TT.PtrArrow) {
				Skip();
				#line 572 "EcsParserGrammar.les"
				kind = S.Cast;
				#line default
			} else if (la0 == TT.@as) {
				Skip();
				#line 573 "EcsParserGrammar.les"
				kind = S.As;
				#line default
			} else {
				Match((int) TT.@using);
				#line 574 "EcsParserGrammar.les"
				kind = S.UsingCast;
				#line default
			}
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var type = DataType();
			Match((int) EOF);
			#line 579 "EcsParserGrammar.les"
			if (attrs != null) {
				type = type.PlusAttrs(attrs.ToVList());
			}
			#line 582 "EcsParserGrammar.les"
			return Up(SetAlternateStyle(SetOperatorStyle(F.Call(kind, e, type, e.Range.StartIndex, rp.EndIndex))));
			#line default
		}
		static readonly HashSet<int> PrefixExpr_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Power, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		LNode PrefixExpr()
		{
			TokenType la2;
			// Line 590: ( (TT.Add|TT.AndBits|TT.DotDot|TT.Forward|TT.IncDec|TT.Mul|TT.Not|TT.NotBits|TT.Sub) PrefixExpr | (&{Down($LI) && Up(Scan_DataType() && LA0 == EOF)} TT.LParen TT.RParen &!(((TT.Add|TT.BQString|TT.Dot|TT.Sub) | TT.IncDec TT.LParen)) PrefixExpr / TT.Power PrefixExpr / PrimaryExpr) )
			do {
				switch (LA0) {
				case TT.Add:
				case TT.AndBits:
				case TT.DotDot:
				case TT.Forward:
				case TT.IncDec:
				case TT.Mul:
				case TT.Not:
				case TT.NotBits:
				case TT.Sub:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						#line 591 "EcsParserGrammar.les"
						return SetOperatorStyle(F.Call((Symbol) op.Value, e, op.StartIndex, e.Range.EndIndex));
						#line default
					}
					break;
				case TT.LParen:
					{
						if (Down(0) && Up(Scan_DataType() && LA0 == EOF)) {
							la2 = LA(2);
							if (PrefixExpr_set0.Contains((int) la2)) {
								if (!Try_PrefixExpr_Test0(2)) {
									var lp = MatchAny();
									Match((int) TT.RParen);
									var e = PrefixExpr();
									#line 597 "EcsParserGrammar.les"
									Down(lp);
									#line 597 "EcsParserGrammar.les"
									return SetOperatorStyle(F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex));
									#line default
								} else
									goto matchPrimaryExpr;
							} else
								goto matchPrimaryExpr;
						} else
							goto matchPrimaryExpr;
					}
					break;
				case TT.Power:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						#line 600 "EcsParserGrammar.les"
						return SetOperatorStyle(F.Call(S._Dereference, SetOperatorStyle(F.Call(S._Dereference, e, op.StartIndex + 1, e.Range.EndIndex)), op.StartIndex, e.Range.EndIndex));
						#line default
					}
					break;
				default:
					goto matchPrimaryExpr;
				}
				break;
			matchPrimaryExpr:
				{
					var e = PrimaryExpr();
					#line 608 "EcsParserGrammar.les"
					return e;
					#line default
				}
			} while (false);
		}
		LNode Expr(Precedence context)
		{
			TokenType la0, la1;
			Debug.Assert(context.CanParse(EP.Prefix));
			Precedence prec;
			var e = PrefixExpr();
			// Line 649: greedy( &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.@in|TT.Add|TT.And|TT.AndBits|TT.BQString|TT.CompoundSet|TT.DivMod|TT.DotDot|TT.EqNeq|TT.GT|TT.LambdaArrow|TT.LEGE|TT.LT|TT.Mul|TT.NotBits|TT.NullCoalesce|TT.OrBits|TT.OrXor|TT.Power|TT.Set|TT.Sub|TT.XorBits) Expr | &{context.CanParse(prec = InfixPrecedenceOf($LA))} (TT.@as|TT.@is|TT.@using) DataType FinishPrimaryExpr | &{context.CanParse(EP.Shift)} &{LT($LI).EndIndex == LT($LI + 1).StartIndex} (TT.LT TT.LT Expr | TT.GT TT.GT Expr) | &{context.CanParse(EP.IfElse)} TT.QuestionMark Expr TT.Colon Expr )*
			for (;;) {
				switch (LA0) {
				case TT.GT:
				case TT.LT:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							if (context.CanParse(EP.Shift)) {
								if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match1;
									else if (la1 == TT.GT || la1 == TT.LT)
										goto match3;
									else
										goto stop;
								} else {
									la1 = LA(1);
									if (PrefixExpr_set0.Contains((int) la1))
										goto match1;
									else
										goto stop;
								}
							} else if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match1;
								else
									goto stop;
							} else {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match1;
								else
									goto stop;
							}
						} else if (context.CanParse(EP.Shift)) {
							if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
								la1 = LA(1);
								if (la1 == TT.GT || la1 == TT.LT)
									goto match3;
								else
									goto stop;
							} else
								goto stop;
						} else
							goto stop;
					}
				case TT.@in:
				case TT.Add:
				case TT.And:
				case TT.AndBits:
				case TT.BQString:
				case TT.CompoundSet:
				case TT.DivMod:
				case TT.DotDot:
				case TT.EqNeq:
				case TT.LambdaArrow:
				case TT.LEGE:
				case TT.Mul:
				case TT.NotBits:
				case TT.NullCoalesce:
				case TT.OrBits:
				case TT.OrXor:
				case TT.Power:
				case TT.Set:
				case TT.Sub:
				case TT.XorBits:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1))
								goto match1;
							else
								goto stop;
						} else
							goto stop;
					}
				case TT.@as:
				case TT.@is:
				case TT.@using:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							switch (LA(1)) {
							case TT.@operator:
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.Substitute:
							case TT.TypeKeyword:
								{
									var op = MatchAny();
									var rhs = DataType(true);
									var opSym = op.Type() == TT.@using ? S.UsingCast : ((Symbol) op.Value);
									e = SetOperatorStyle(F.Call(opSym, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
									FinishPrimaryExpr(ref e);
								}
								break;
							default:
								goto stop;
							}
						} else
							goto stop;
					}
					break;
				case TT.QuestionMark:
					{
						if (context.CanParse(EP.IfElse)) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1)) {
								Skip();
								var then = Expr(ContinueExpr);
								Match((int) TT.Colon);
								var @else = Expr(EP.IfElse);
								#line 672 "EcsParserGrammar.les"
								e = SetOperatorStyle(F.Call(S.QuestionMark, e, then, @else, e.Range.StartIndex, @else.Range.EndIndex));
								#line default
							} else
								goto stop;
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
				continue;
			match1:
				{
					var op = MatchAny();
					var rhs = Expr(prec);
					#line 653 "EcsParserGrammar.les"
					e = SetOperatorStyle(F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
					#line default
				}
				continue;
			match3:
				{
					// Line 664: (TT.LT TT.LT Expr | TT.GT TT.GT Expr)
					la0 = LA0;
					if (la0 == TT.LT) {
						Skip();
						Match((int) TT.LT);
						var rhs = Expr(EP.Shift);
						#line 665 "EcsParserGrammar.les"
						e = SetOperatorStyle(F.Call(S.Shl, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
						#line default
					} else {
						Match((int) TT.GT);
						Match((int) TT.GT);
						var rhs = Expr(EP.Shift);
						#line 667 "EcsParserGrammar.les"
						e = SetOperatorStyle(F.Call(S.Shr, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
						#line default
					}
				}
			}
		stop:;
			#line 674 "EcsParserGrammar.les"
			return e;
			#line default
		}
		public LNode ExprStart(bool allowUnassignedVarDecl)
		{
			TokenType la0, la1;
			LNode result = default(LNode);
			// Line 686: ((TT.Id | UnusualId) TT.Colon ExprStart2 | ExprStart2)
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
					if (Try_Scan_DetectVarDecl(0, allowUnassignedVarDecl)) {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
						else
							result = ExprStart2(allowUnassignedVarDecl);
					} else {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
						else
							result = ExprStart2(allowUnassignedVarDecl);
					}
				} else
					result = ExprStart2(allowUnassignedVarDecl);
				break;
			match1:
				{
					#line 686 "EcsParserGrammar.les"
					Token argName = default(Token);
					#line default
					// Line 687: (TT.Id | UnusualId)
					la0 = LA0;
					if (la0 == TT.Id)
						argName = MatchAny();
					else
						argName = UnusualId();
					Skip();
					result = ExprStart2(allowUnassignedVarDecl);
					#line 689 "EcsParserGrammar.les"
					result = SetOperatorStyle(F.Call(S.NamedArg, F.Id(argName), result, argName.StartIndex, result.Range.EndIndex));
					#line default
				}
			} while (false);
			return result;
		}
		public LNode ExprStart2(bool allowUnassignedVarDecl)
		{
			#line 694 "EcsParserGrammar.les"
			LNode e;
			WList<LNode> attrs = null;
			#line default
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var wc = WordAttributes(ref attrs);
			// Line 700: (&(DetectVarDecl) IdAtom => VarDeclExpr / Expr)
			do {
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (Try_Scan_DetectVarDecl(0, allowUnassignedVarDecl))
							e = VarDeclExpr();
						else
							goto matchExpr;
					}
					break;
				default:
					goto matchExpr;
				}
				break;
			matchExpr:
				{
					#line 702 "EcsParserGrammar.les"
					if ((wc != 0)) {
						NonKeywordAttrError(attrs, "expression");
					}
					#line default
					e = Expr(ContinueExpr);
				}
			} while (false);
			#line 706 "EcsParserGrammar.les"
			if ((attrs != null)) {
				e = e.PlusAttrs(attrs.ToVList());
			}
			#line 709 "EcsParserGrammar.les"
			return e;
			#line default
		}
		void DetectVarDecl(bool allowUnassigned)
		{
			TokenType la0;
			VarDeclStart();
			// Line 723: ((TT.QuickBindSet|TT.Set) NoUnmatchedColon / &{allowUnassigned} (EOF|TT.Comma))
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				Skip();
				NoUnmatchedColon();
			} else {
				Check(allowUnassigned, "allowUnassigned");
				Match((int) EOF, (int) TT.Comma);
			}
		}
		bool Try_Scan_DetectVarDecl(int lookaheadAmt, bool allowUnassigned)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DetectVarDecl(allowUnassigned);
		}
		bool Scan_DetectVarDecl(bool allowUnassigned)
		{
			TokenType la0;
			if (!Scan_VarDeclStart())
				return false;
			// Line 723: ((TT.QuickBindSet|TT.Set) NoUnmatchedColon / &{allowUnassigned} (EOF|TT.Comma))
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				if (!TryMatch((int) TT.QuickBindSet, (int) TT.Set))
					return false;
				if (!Scan_NoUnmatchedColon())
					return false;
			} else {
				if (!allowUnassigned)
					return false;
				if (!TryMatch((int) EOF, (int) TT.Comma))
					return false;
			}
			return true;
		}
		void NoUnmatchedColon()
		{
			TokenType la0;
			// Line 741: (SubConditional | ~(EOF|TT.Colon|TT.Comma|TT.QuestionMark|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark)
					SubConditional();
				else if (!(la0 == (TokenType) EOF || la0 == TT.Colon || la0 == TT.Comma || la0 == TT.QuestionMark || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			Match((int) EOF, (int) TT.Comma, (int) TT.Semicolon);
		}
		bool Scan_NoUnmatchedColon()
		{
			TokenType la0;
			// Line 741: (SubConditional | ~(EOF|TT.Colon|TT.Comma|TT.QuestionMark|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark)
					{if (!Scan_SubConditional())
						return false;}
				else if (!(la0 == (TokenType) EOF || la0 == TT.Colon || la0 == TT.Comma || la0 == TT.QuestionMark || la0 == TT.Semicolon))
					{if (!TryMatchExcept((int) TT.Colon, (int) TT.Comma, (int) TT.QuestionMark, (int) TT.Semicolon))
						return false;}
				else
					break;
			}
			if (!TryMatch((int) EOF, (int) TT.Comma, (int) TT.Semicolon))
				return false;
			return true;
		}
		void SubConditional()
		{
			TokenType la0;
			Skip();
			// Line 746: nongreedy(SubConditional / ~(EOF|TT.Comma|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (la0 == (TokenType) EOF || la0 == TT.Colon)
					break;
				else if (la0 == TT.QuestionMark)
					SubConditional();
				else
					MatchExcept((int) TT.Comma, (int) TT.Semicolon);
			}
			Match((int) TT.Colon);
		}
		bool Scan_SubConditional()
		{
			TokenType la0;
			if (!TryMatch((int) TT.QuestionMark))
				return false;
			// Line 746: nongreedy(SubConditional / ~(EOF|TT.Comma|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (la0 == (TokenType) EOF || la0 == TT.Colon)
					break;
				else if (la0 == TT.QuestionMark)
					{if (!Scan_SubConditional())
						return false;}
				else if (!TryMatchExcept((int) TT.Comma, (int) TT.Semicolon))
					return false;
			}
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
		LNode VarDeclExpr()
		{
			TokenType la0;
			var pair = VarDeclStart();
			#line 750 "EcsParserGrammar.les"
			LNode type = pair.Item1, name = pair.Item2;
			#line default
			// Line 751: ((TT.QuickBindSet|TT.Set) Expr)?
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				Skip();
				var init = Expr(ContinueExpr);
				#line 752 "EcsParserGrammar.les"
				return F.Call(S.Var, type, F.Call(S.Assign, name, init, name.Range.StartIndex, init.Range.EndIndex), type.Range.StartIndex, init.Range.EndIndex);
				#line default
			}
			#line 756 "EcsParserGrammar.les"
			return F.Call(S.Var, type, name, type.Range.StartIndex, name.Range.EndIndex);
			#line default
		}
		Pair<LNode,LNode> VarDeclStart()
		{
			var e = DataType();
			var id = IdAtom();
			MaybeRecognizeVarAsKeyword(ref e);
			return Pair.Create(e, id);
		}
		bool Scan_VarDeclStart()
		{
			if (!Scan_DataType())
				return false;
			if (!Scan_IdAtom())
				return false;
			return true;
		}
		LNode ExprInParens(bool allowUnassignedVarDecl)
		{
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			#line 786 "EcsParserGrammar.les"
			if ((!Down(lp))) {
				return F.Call(S.Tuple, lp.StartIndex, rp.EndIndex);
			}
			return Up(InParens_ExprOrTuple(allowUnassignedVarDecl, lp.StartIndex, rp.EndIndex));
			#line default
		}
		bool Scan_ExprInParens(bool allowUnassignedVarDecl)
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}
		static readonly HashSet<int> InParens_ExprOrTuple_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Power, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		LNode InParens_ExprOrTuple(bool allowUnassignedVarDecl, int startIndex, int endIndex)
		{
			TokenType la0, la1;
			// Line 792: (ExprStart (TT.Comma (~(EOF))* => (TT.Comma ExprStart | TT.Comma)*)? | )
			la0 = LA0;
			if (InParens_ExprOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(allowUnassignedVarDecl);
				// Line 793: (TT.Comma (~(EOF))* => (TT.Comma ExprStart | TT.Comma)*)?
				la0 = LA0;
				if (la0 == TT.Comma) {
					#line 794 "EcsParserGrammar.les"
					var list = new VList<LNode> { 
						e
					};
					#line default
					// Line 795: (TT.Comma ExprStart | TT.Comma)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							la1 = LA(1);
							if (InParens_ExprOrTuple_set0.Contains((int) la1)) {
								Skip();
								list.Add(ExprStart(allowUnassignedVarDecl));
							} else
								Skip();
						} else
							break;
					}
					#line 798 "EcsParserGrammar.les"
					return F.Tuple(list, startIndex, endIndex);
					#line default
				}
				#line 800 "EcsParserGrammar.les"
				return F.InParens(e, startIndex, endIndex);
				#line default
			} else {
				#line 802 "EcsParserGrammar.les"
				return F.Tuple(VList<LNode>.Empty, startIndex, endIndex);
				#line default
			}
			Match((int) EOF);
		}
		LNode BracedBlock(Symbol spaceName = null, Symbol target = null)
		{
			#line 807 "EcsParserGrammar.les"
			var oldSpace = _spaceName;
			_spaceName = spaceName ?? oldSpace;
			#line default
			var lb = Match((int) TT.LBrace);
			var rb = Match((int) TT.RBrace);
			#line 811 "EcsParserGrammar.les"
			var stmts = StmtListInside(lb).ToVList();
			_spaceName = oldSpace;
			return F.Call(target ?? S.Braces, stmts, lb.StartIndex, rb.EndIndex).SetBaseStyle(NodeStyle.Statement);
			#line default
		}
		bool Scan_BracedBlock(Symbol spaceName = null, Symbol target = null)
		{
			if (!TryMatch((int) TT.LBrace))
				return false;
			if (!TryMatch((int) TT.RBrace))
				return false;
			return true;
		}
		void NormalAttributes(ref WList<LNode> attrs)
		{
			TokenType la0, la1;
			// Line 823: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var t = MatchAny();
							Skip();
							#line 826 "EcsParserGrammar.les"
							if ((Down(t))) {
								AttributeContents(ref attrs);
								Up();
							}
							#line default
						} else
							break;
					} else
						break;
				} else
					break;
			}
		}
		bool Try_Scan_NormalAttributes(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_NormalAttributes();
		}
		bool Scan_NormalAttributes()
		{
			TokenType la0, la1;
			// Line 823: (&!{Down($LI) && Up(Try_Scan_AsmOrModLabel(0))} TT.LBrack TT.RBrack)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							if (!TryMatch((int) TT.LBrack))
								return false;
							if (!TryMatch((int) TT.RBrack))
								return false;
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return true;
		}
		void AttributeContents(ref WList<LNode> attrs)
		{
			TokenType la0, la1;
			#line 834 "EcsParserGrammar.les"
			Token attrTarget = default(Token);
			#line default
			// Line 836: greedy((@`.`(TT, noMacro(@return))|TT.ContextualKeyword|TT.Id) TT.Colon)?
			la0 = LA0;
			if (la0 == TT.@return || la0 == TT.ContextualKeyword || la0 == TT.Id) {
				la1 = LA(1);
				if (la1 == TT.Colon) {
					attrTarget = MatchAny();
					Skip();
				}
			}
			ExprList(attrs = attrs ?? new WList<LNode>(), allowTrailingComma: true, allowUnassignedVarDecl: true);
			#line 841 "EcsParserGrammar.les"
			if (attrTarget.Value != null) {
				var attrTargetNode = F.Id(attrTarget);
				for (int i = 0; i < attrs.Count; i++) {
					var attr = attrs[i];
					if ((!IsNamedArg(attr))) {
						attrs[i] = SetOperatorStyle(F.Call(S.NamedArg, attrTargetNode, attr, attrTarget.StartIndex, attr.Range.EndIndex));
					} else {
						attrTargetNode = attrs[i].Args[1];
						Error(attrTargetNode, "Syntax error: only one attribute target is allowed");
					}
				}
			}
			#line default
		}
		void AttributeKeywords(ref WList<LNode> attrs)
		{
			TokenType la0;
			// Line 860: (TT.AttrKeyword)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					#line 861 "EcsParserGrammar.les"
					(attrs = attrs ?? new WList<LNode>()).Add(F.Id(t));
					#line default
				} else
					break;
			}
		}
		void TParamAttributeKeywords(ref WList<LNode> attrs)
		{
			TokenType la0;
			// Line 866: ((TT.@in|TT.AttrKeyword))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@in || la0 == TT.AttrKeyword) {
					var t = MatchAny();
					#line 867 "EcsParserGrammar.les"
					(attrs = attrs ?? new WList<LNode>()).Add(F.Id(t));
					#line default
				} else
					break;
			}
		}
		bool Try_Scan_TParamAttributeKeywords(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamAttributeKeywords();
		}
		bool Scan_TParamAttributeKeywords()
		{
			TokenType la0;
			// Line 866: ((TT.@in|TT.AttrKeyword))*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@in || la0 == TT.AttrKeyword)
					{if (!TryMatch((int) TT.@in, (int) TT.AttrKeyword))
						return false;}
				else
					break;
			}
			return true;
		}
		int WordAttributes(ref WList<LNode> attrs)
		{
			TokenType la0;
			#line 933 "EcsParserGrammar.les"
			TokenType LA1;
			int nonKeywords = 0;
			if (LA0 == TT.Id && ((LA1 = LA(1)) == TT.Set || LA1 == TT.LParen || LA1 == TT.Dot))
				 return 0;
			#line 937 "EcsParserGrammar.les"
			Token t;
			#line default
			// Line 939: (TT.AttrKeyword | ((@`.`(TT, noMacro(@this))|TT.@new|TT.Id) | UnusualId) &(( DataType ((TT.AttrKeyword|TT.Id|TT.TypeKeyword) | UnusualId) | @`.`(TT, noMacro(@this)) TT.LParen | (TT.@new|TT.AttrKeyword) | TT.@checked TT.LBrace TT.RBrace | TT.@unchecked TT.LBrace TT.RBrace | @`.`(TT, noMacro(@default)) TT.Colon | TT.@using TT.LParen | (@`.`(TT, noMacro(@break))|@`.`(TT, noMacro(@continue))|@`.`(TT, noMacro(@return))|@`.`(TT, noMacro(@throw))|TT.@case|TT.@class|TT.@delegate|TT.@do|TT.@enum|TT.@event|TT.@fixed|TT.@for|TT.@foreach|TT.@goto|TT.@interface|TT.@lock|TT.@namespace|TT.@struct|TT.@switch|TT.@try|TT.@while) )))*
			for (;;) {
				switch (LA0) {
				case TT.AttrKeyword:
					{
						t = MatchAny();
						#line 939 "EcsParserGrammar.les"
						attrs.Add(F.Id(t));
						#line default
					}
					break;
				case TT.@this:
				case TT.@new:
				case TT.ContextualKeyword:
				case TT.Id:
					{
						if (Try_WordAttributes_Test0(1)) {
							// Line 940: ((@`.`(TT, noMacro(@this))|TT.@new|TT.Id) | UnusualId)
							la0 = LA0;
							if (la0 == TT.@this || la0 == TT.@new || la0 == TT.Id)
								t = MatchAny();
							else
								t = UnusualId();
							#line 956 "EcsParserGrammar.les"
							LNode node;
							if ((t.Type() == TT.@new || t.Type() == TT.@this)) {
								node = F.Id(t);
							} else {
								node = F.Attr(_triviaWordAttribute, F.Id("#" + t.Value.ToString(), t.StartIndex, t.EndIndex));
							}
							#line 962 "EcsParserGrammar.les"
							attrs = attrs ?? new WList<LNode>();
							attrs.Add(node);
							nonKeywords++;
							#line default
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		stop:;
			#line 967 "EcsParserGrammar.les"
			return nonKeywords;
			#line default
		}
		static readonly HashSet<int> Stmt_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set1 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set2 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set3 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set4 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set5 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.Comma, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set6 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set7 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set8 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.At, (int) TT.ColonColon, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.QuestionMark, (int) TT.Substitute, (int) TT.TypeKeyword);
		static readonly HashSet<int> Stmt_set9 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Power, (int) TT.Semicolon, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		static readonly HashSet<int> Stmt_set10 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Power, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		public LNode Stmt()
		{
			TokenType la1, la2;
			_stmtAttrs.Clear();
			int startIndex = LT0.StartIndex;
			NormalAttributes(ref _stmtAttrs);
			AttributeKeywords(ref _stmtAttrs);
			var wc = WordAttributes(ref _stmtAttrs);
			var attrs = _stmtAttrs.ToVList();
			LNode r;
			// Line 1022: ( ((AssemblyOrModuleAttribute | EventDecl | DelegateDecl | SpaceDecl | EnumDecl) | (TraitDecl / AliasDecl / OperatorCast / Constructor / Destructor / &(DataType (ComplexNameDecl (TT.At|TT.Comma|TT.Forward|TT.LambdaArrow|TT.LBrace|TT.LParen|TT.QuickBindSet|TT.Semicolon|TT.Set) | ComplexThisDecl TT.LBrack)) MethodOrPropertyOrVar / BracedBlock / BlockCallStmt / ExprStatement) | CheckedOrUncheckedStmt | DoStmt | CaseStmt | GotoStmt | GotoCaseStmt | LabelStmt | ReturnBreakContinueThrow | WhileStmt | ForStmt | ForEachStmt | IfStmt | SwitchStmt) | (UsingStmt / UsingDirective) | LockStmt | FixedStmt | TryStmt | TT.Semicolon )
			do {
				switch (LA0) {
				case TT.LBrack:
					r = AssemblyOrModuleAttribute(startIndex, attrs);
					break;
				case TT.@event:
					r = EventDecl(startIndex, attrs);
					break;
				case TT.@delegate:
					{
						switch (LA(1)) {
						case TT.@operator:
						case TT.ContextualKeyword:
						case TT.Id:
						case TT.Substitute:
						case TT.TypeKeyword:
							r = DelegateDecl(startIndex, attrs);
							break;
						case TT.LParen:
							goto matchExprStatement;
						default:
							goto error;
						}
					}
					break;
				case TT.@class:
				case TT.@interface:
				case TT.@namespace:
				case TT.@struct:
					r = SpaceDecl(startIndex, attrs);
					break;
				case TT.@enum:
					r = EnumDecl(startIndex, attrs);
					break;
				case TT.ContextualKeyword:
					{
						if (Is(0, _trait)) {
							if (Is(0, _alias)) {
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
									if (Try_Stmt_Test0(0)) {
										if (_spaceName == LT(0).Value) {
											switch (LA(1)) {
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.Id:
											case TT.Substitute:
											case TT.TypeKeyword:
												{
													if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
														if (Try_MethodOrPropertyOrVar_Test0(1)) {
															la2 = LA(2);
															if (Stmt_set0.Contains((int) la2))
																r = TraitDecl(startIndex, attrs);
															else if (la2 == TT.Comma || la2 == TT.LBrack)
																r = MethodOrPropertyOrVar(startIndex, attrs);
															else
																goto error;
														} else {
															la2 = LA(2);
															if (Stmt_set0.Contains((int) la2))
																r = TraitDecl(startIndex, attrs);
															else if (la2 == TT.LBrack)
																r = MethodOrPropertyOrVar(startIndex, attrs);
															else
																goto error;
														}
													} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.Comma || la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													} else {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													}
												}
												break;
											case TT.LParen:
												{
													if (Try_BlockCallStmt_Test0(1)) {
														if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
															goto matchConstructor;
														else
															goto matchBlockCallStmt;
													} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
														goto matchConstructor;
													else
														goto matchExprStatement;
												}
											case TT.ColonColon:
											case TT.Dot:
											case TT.LBrack:
											case TT.LT:
											case TT.Mul:
											case TT.Not:
											case TT.QuestionMark:
												{
													if (Try_FinishPrimaryExpr_Test0(1)) {
														switch (LA(2)) {
														case TT.@this:
														case TT.@operator:
														case TT.ContextualKeyword:
														case TT.GT:
														case TT.Id:
														case TT.LBrack:
														case TT.LParen:
														case TT.Mul:
														case TT.QuestionMark:
														case TT.RBrack:
														case TT.Substitute:
														case TT.TypeKeyword:
															r = MethodOrPropertyOrVar(startIndex, attrs);
															break;
														default:
															goto matchExprStatement;
														}
													} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
														switch (LA(2)) {
														case TT.@this:
														case TT.@operator:
														case TT.ContextualKeyword:
														case TT.GT:
														case TT.Id:
														case TT.LBrack:
														case TT.LParen:
														case TT.Mul:
														case TT.QuestionMark:
														case TT.RBrack:
														case TT.Substitute:
														case TT.TypeKeyword:
															r = MethodOrPropertyOrVar(startIndex, attrs);
															break;
														case TT.@base:
														case TT.@default:
														case TT.@checked:
														case TT.@delegate:
														case TT.@is:
														case TT.@new:
														case TT.@sizeof:
														case TT.@typeof:
														case TT.@unchecked:
														case TT.Add:
														case TT.AndBits:
														case TT.At:
														case TT.Dot:
														case TT.DotDot:
														case TT.Forward:
														case TT.IncDec:
														case TT.LBrace:
														case TT.Literal:
														case TT.LT:
														case TT.Not:
														case TT.NotBits:
														case TT.Power:
														case TT.Sub:
															goto matchExprStatement;
														default:
															goto error;
														}
													} else {
														switch (LA(2)) {
														case TT.@this:
														case TT.@operator:
														case TT.ContextualKeyword:
														case TT.GT:
														case TT.Id:
														case TT.LBrack:
														case TT.LParen:
														case TT.Mul:
														case TT.QuestionMark:
														case TT.RBrack:
														case TT.Substitute:
														case TT.TypeKeyword:
															r = MethodOrPropertyOrVar(startIndex, attrs);
															break;
														case TT.@base:
														case TT.@default:
														case TT.@checked:
														case TT.@delegate:
														case TT.@is:
														case TT.@new:
														case TT.@sizeof:
														case TT.@typeof:
														case TT.@unchecked:
														case TT.Add:
														case TT.AndBits:
														case TT.At:
														case TT.Dot:
														case TT.DotDot:
														case TT.Forward:
														case TT.IncDec:
														case TT.LBrace:
														case TT.Literal:
														case TT.Not:
														case TT.NotBits:
														case TT.Power:
														case TT.Sub:
															goto matchExprStatement;
														default:
															goto error;
														}
													}
												}
												break;
											case TT.@this:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											case TT.LBrace:
												{
													if (Try_BlockCallStmt_Test0(1))
														goto matchBlockCallStmt;
													else
														goto matchExprStatement;
												}
											case TT.Forward:
												goto matchBlockCallStmt;
											case EOF:
											case TT.@as:
											case TT.@catch:
											case TT.@else:
											case TT.@finally:
											case TT.@in:
											case TT.@is:
											case TT.@using:
											case TT.@while:
											case TT.Add:
											case TT.And:
											case TT.AndBits:
											case TT.BQString:
											case TT.CompoundSet:
											case TT.DivMod:
											case TT.DotDot:
											case TT.EqNeq:
											case TT.GT:
											case TT.IncDec:
											case TT.LambdaArrow:
											case TT.LEGE:
											case TT.NotBits:
											case TT.NullCoalesce:
											case TT.NullDot:
											case TT.OrBits:
											case TT.OrXor:
											case TT.Power:
											case TT.PtrArrow:
											case TT.QuickBind:
											case TT.Semicolon:
											case TT.Set:
											case TT.Sub:
											case TT.XorBits:
												goto matchExprStatement;
											case TT.Colon:
												goto matchLabelStmt;
											default:
												goto error;
											}
										} else {
											switch (LA(1)) {
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.Id:
											case TT.Substitute:
											case TT.TypeKeyword:
												{
													if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
														if (Try_MethodOrPropertyOrVar_Test0(1)) {
															la2 = LA(2);
															if (Stmt_set0.Contains((int) la2))
																r = TraitDecl(startIndex, attrs);
															else if (la2 == TT.Comma || la2 == TT.LBrack)
																r = MethodOrPropertyOrVar(startIndex, attrs);
															else
																goto error;
														} else {
															la2 = LA(2);
															if (Stmt_set0.Contains((int) la2))
																r = TraitDecl(startIndex, attrs);
															else if (la2 == TT.LBrack)
																r = MethodOrPropertyOrVar(startIndex, attrs);
															else
																goto error;
														}
													} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.Comma || la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													} else {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													}
												}
												break;
											case TT.LParen:
												{
													if (Try_Constructor_Test2(1))
														goto matchConstructor;
													else if (Try_BlockCallStmt_Test0(1))
														goto matchBlockCallStmt;
													else
														goto matchExprStatement;
												}
											case TT.ColonColon:
											case TT.Dot:
											case TT.LBrack:
											case TT.LT:
											case TT.Mul:
											case TT.Not:
											case TT.QuestionMark:
												{
													if (Try_FinishPrimaryExpr_Test0(1)) {
														switch (LA(2)) {
														case TT.@this:
														case TT.@operator:
														case TT.ContextualKeyword:
														case TT.GT:
														case TT.Id:
														case TT.LBrack:
														case TT.LParen:
														case TT.Mul:
														case TT.QuestionMark:
														case TT.RBrack:
														case TT.Substitute:
														case TT.TypeKeyword:
															r = MethodOrPropertyOrVar(startIndex, attrs);
															break;
														default:
															goto matchExprStatement;
														}
													} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
														switch (LA(2)) {
														case TT.@this:
														case TT.@operator:
														case TT.ContextualKeyword:
														case TT.GT:
														case TT.Id:
														case TT.LBrack:
														case TT.LParen:
														case TT.Mul:
														case TT.QuestionMark:
														case TT.RBrack:
														case TT.Substitute:
														case TT.TypeKeyword:
															r = MethodOrPropertyOrVar(startIndex, attrs);
															break;
														case TT.@base:
														case TT.@default:
														case TT.@checked:
														case TT.@delegate:
														case TT.@is:
														case TT.@new:
														case TT.@sizeof:
														case TT.@typeof:
														case TT.@unchecked:
														case TT.Add:
														case TT.AndBits:
														case TT.At:
														case TT.Dot:
														case TT.DotDot:
														case TT.Forward:
														case TT.IncDec:
														case TT.LBrace:
														case TT.Literal:
														case TT.LT:
														case TT.Not:
														case TT.NotBits:
														case TT.Power:
														case TT.Sub:
															goto matchExprStatement;
														default:
															goto error;
														}
													} else {
														switch (LA(2)) {
														case TT.@this:
														case TT.@operator:
														case TT.ContextualKeyword:
														case TT.GT:
														case TT.Id:
														case TT.LBrack:
														case TT.LParen:
														case TT.Mul:
														case TT.QuestionMark:
														case TT.RBrack:
														case TT.Substitute:
														case TT.TypeKeyword:
															r = MethodOrPropertyOrVar(startIndex, attrs);
															break;
														case TT.@base:
														case TT.@default:
														case TT.@checked:
														case TT.@delegate:
														case TT.@is:
														case TT.@new:
														case TT.@sizeof:
														case TT.@typeof:
														case TT.@unchecked:
														case TT.Add:
														case TT.AndBits:
														case TT.At:
														case TT.Dot:
														case TT.DotDot:
														case TT.Forward:
														case TT.IncDec:
														case TT.LBrace:
														case TT.Literal:
														case TT.Not:
														case TT.NotBits:
														case TT.Power:
														case TT.Sub:
															goto matchExprStatement;
														default:
															goto error;
														}
													}
												}
												break;
											case TT.@this:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											case TT.LBrace:
												{
													if (Try_BlockCallStmt_Test0(1))
														goto matchBlockCallStmt;
													else
														goto matchExprStatement;
												}
											case TT.Forward:
												goto matchBlockCallStmt;
											case EOF:
											case TT.@as:
											case TT.@catch:
											case TT.@else:
											case TT.@finally:
											case TT.@in:
											case TT.@is:
											case TT.@using:
											case TT.@while:
											case TT.Add:
											case TT.And:
											case TT.AndBits:
											case TT.BQString:
											case TT.CompoundSet:
											case TT.DivMod:
											case TT.DotDot:
											case TT.EqNeq:
											case TT.GT:
											case TT.IncDec:
											case TT.LambdaArrow:
											case TT.LEGE:
											case TT.NotBits:
											case TT.NullCoalesce:
											case TT.NullDot:
											case TT.OrBits:
											case TT.OrXor:
											case TT.Power:
											case TT.PtrArrow:
											case TT.QuickBind:
											case TT.Semicolon:
											case TT.Set:
											case TT.Sub:
											case TT.XorBits:
												goto matchExprStatement;
											case TT.Colon:
												goto matchLabelStmt;
											default:
												goto error;
											}
										}
									} else if (_spaceName == LT(0).Value) {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											r = TraitDecl(startIndex, attrs);
											break;
										case TT.LParen:
											{
												if (Try_BlockCallStmt_Test0(1)) {
													if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
														goto matchConstructor;
													else
														goto matchBlockCallStmt;
												} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchExprStatement;
											}
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.Forward:
											goto matchBlockCallStmt;
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
										case TT.Add:
										case TT.And:
										case TT.AndBits:
										case TT.BQString:
										case TT.ColonColon:
										case TT.CompoundSet:
										case TT.DivMod:
										case TT.Dot:
										case TT.DotDot:
										case TT.EqNeq:
										case TT.GT:
										case TT.IncDec:
										case TT.LambdaArrow:
										case TT.LBrack:
										case TT.LEGE:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.NotBits:
										case TT.NullCoalesce:
										case TT.NullDot:
										case TT.OrBits:
										case TT.OrXor:
										case TT.Power:
										case TT.PtrArrow:
										case TT.QuestionMark:
										case TT.QuickBind:
										case TT.Semicolon:
										case TT.Set:
										case TT.Sub:
										case TT.XorBits:
											goto matchExprStatement;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									} else {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											r = TraitDecl(startIndex, attrs);
											break;
										case TT.LParen:
											{
												if (Try_Constructor_Test2(1))
													goto matchConstructor;
												else if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.Forward:
											goto matchBlockCallStmt;
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
										case TT.Add:
										case TT.And:
										case TT.AndBits:
										case TT.BQString:
										case TT.ColonColon:
										case TT.CompoundSet:
										case TT.DivMod:
										case TT.Dot:
										case TT.DotDot:
										case TT.EqNeq:
										case TT.GT:
										case TT.IncDec:
										case TT.LambdaArrow:
										case TT.LBrack:
										case TT.LEGE:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.NotBits:
										case TT.NullCoalesce:
										case TT.NullDot:
										case TT.OrBits:
										case TT.OrXor:
										case TT.Power:
										case TT.PtrArrow:
										case TT.QuestionMark:
										case TT.QuickBind:
										case TT.Semicolon:
										case TT.Set:
										case TT.Sub:
										case TT.XorBits:
											goto matchExprStatement;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									}
								} else if (Try_Stmt_Test0(0)) {
									if (_spaceName == LT(0).Value) {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											r = TraitDecl(startIndex, attrs);
											break;
										case TT.LParen:
											{
												if (Try_BlockCallStmt_Test0(1)) {
													if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
														goto matchConstructor;
													else
														goto matchBlockCallStmt;
												} else
													goto matchConstructor;
											}
										case TT.Forward:
										case TT.LBrace:
											goto matchBlockCallStmt;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									} else {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											r = TraitDecl(startIndex, attrs);
											break;
										case TT.LParen:
											{
												if (Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											}
										case TT.Forward:
										case TT.LBrace:
											goto matchBlockCallStmt;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									}
								} else if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = TraitDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											} else
												goto matchConstructor;
										}
									case TT.Forward:
									case TT.LBrace:
										goto matchBlockCallStmt;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = TraitDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										}
									case TT.Forward:
									case TT.LBrace:
										goto matchBlockCallStmt;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								}
							} else if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
								if (Try_Stmt_Test0(0)) {
									if (_spaceName == LT(0).Value) {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											{
												if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
													if (Try_MethodOrPropertyOrVar_Test0(1)) {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.Comma || la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													} else {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													}
												} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
													la2 = LA(2);
													if (Stmt_set0.Contains((int) la2))
														r = TraitDecl(startIndex, attrs);
													else if (la2 == TT.Comma || la2 == TT.LBrack)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												} else {
													la2 = LA(2);
													if (Stmt_set0.Contains((int) la2))
														r = TraitDecl(startIndex, attrs);
													else if (la2 == TT.LBrack)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												}
											}
											break;
										case TT.LParen:
											{
												if (Try_BlockCallStmt_Test0(1)) {
													if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
														goto matchConstructor;
													else
														goto matchBlockCallStmt;
												} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchExprStatement;
											}
										case TT.ColonColon:
										case TT.Dot:
										case TT.LBrack:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.QuestionMark:
											{
												if (Try_FinishPrimaryExpr_Test0(1)) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													default:
														goto matchExprStatement;
													}
												} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.LT:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												} else {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												}
											}
											break;
										case TT.@this:
											r = MethodOrPropertyOrVar(startIndex, attrs);
											break;
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.Forward:
											goto matchBlockCallStmt;
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
										case TT.Add:
										case TT.And:
										case TT.AndBits:
										case TT.BQString:
										case TT.CompoundSet:
										case TT.DivMod:
										case TT.DotDot:
										case TT.EqNeq:
										case TT.GT:
										case TT.IncDec:
										case TT.LambdaArrow:
										case TT.LEGE:
										case TT.NotBits:
										case TT.NullCoalesce:
										case TT.NullDot:
										case TT.OrBits:
										case TT.OrXor:
										case TT.Power:
										case TT.PtrArrow:
										case TT.QuickBind:
										case TT.Semicolon:
										case TT.Set:
										case TT.Sub:
										case TT.XorBits:
											goto matchExprStatement;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									} else {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											{
												if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
													if (Try_MethodOrPropertyOrVar_Test0(1)) {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.Comma || la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													} else {
														la2 = LA(2);
														if (Stmt_set0.Contains((int) la2))
															r = TraitDecl(startIndex, attrs);
														else if (la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													}
												} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
													la2 = LA(2);
													if (Stmt_set0.Contains((int) la2))
														r = TraitDecl(startIndex, attrs);
													else if (la2 == TT.Comma || la2 == TT.LBrack)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												} else {
													la2 = LA(2);
													if (Stmt_set0.Contains((int) la2))
														r = TraitDecl(startIndex, attrs);
													else if (la2 == TT.LBrack)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												}
											}
											break;
										case TT.LParen:
											{
												if (Try_Constructor_Test2(1))
													goto matchConstructor;
												else if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.ColonColon:
										case TT.Dot:
										case TT.LBrack:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.QuestionMark:
											{
												if (Try_FinishPrimaryExpr_Test0(1)) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													default:
														goto matchExprStatement;
													}
												} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.LT:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												} else {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												}
											}
											break;
										case TT.@this:
											r = MethodOrPropertyOrVar(startIndex, attrs);
											break;
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.Forward:
											goto matchBlockCallStmt;
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
										case TT.Add:
										case TT.And:
										case TT.AndBits:
										case TT.BQString:
										case TT.CompoundSet:
										case TT.DivMod:
										case TT.DotDot:
										case TT.EqNeq:
										case TT.GT:
										case TT.IncDec:
										case TT.LambdaArrow:
										case TT.LEGE:
										case TT.NotBits:
										case TT.NullCoalesce:
										case TT.NullDot:
										case TT.OrBits:
										case TT.OrXor:
										case TT.Power:
										case TT.PtrArrow:
										case TT.QuickBind:
										case TT.Semicolon:
										case TT.Set:
										case TT.Sub:
										case TT.XorBits:
											goto matchExprStatement;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									}
								} else if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = TraitDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchExprStatement;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.Forward:
										goto matchBlockCallStmt;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.ColonColon:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.Dot:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LBrack:
									case TT.LEGE:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuestionMark:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = TraitDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto matchConstructor;
											else if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.Forward:
										goto matchBlockCallStmt;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.ColonColon:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.Dot:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LBrack:
									case TT.LEGE:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuestionMark:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								}
							} else if (Try_Stmt_Test0(0)) {
								if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = TraitDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											} else
												goto matchConstructor;
										}
									case TT.Forward:
									case TT.LBrace:
										goto matchBlockCallStmt;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = TraitDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										}
									case TT.Forward:
									case TT.LBrace:
										goto matchBlockCallStmt;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								}
							} else if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = TraitDecl(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else
											goto matchConstructor;
									}
								case TT.Forward:
								case TT.LBrace:
									goto matchBlockCallStmt;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							} else {
								switch (LA(1)) {
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = TraitDecl(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									}
								case TT.Forward:
								case TT.LBrace:
									goto matchBlockCallStmt;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							}
						} else if (Is(0, _alias)) {
							if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
								if (Try_Stmt_Test0(0)) {
									if (_spaceName == LT(0).Value) {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											{
												if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
													if (Try_MethodOrPropertyOrVar_Test0(1)) {
														la2 = LA(2);
														if (Stmt_set1.Contains((int) la2))
															r = AliasDecl(startIndex, attrs);
														else if (la2 == TT.Comma || la2 == TT.LBrack || la2 == TT.Semicolon)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													} else {
														la2 = LA(2);
														if (Stmt_set1.Contains((int) la2))
															r = AliasDecl(startIndex, attrs);
														else if (la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													}
												} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
													la2 = LA(2);
													if (Stmt_set1.Contains((int) la2))
														r = AliasDecl(startIndex, attrs);
													else if (la2 == TT.Comma || la2 == TT.LBrack || la2 == TT.Semicolon)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												} else {
													la2 = LA(2);
													if (Stmt_set1.Contains((int) la2))
														r = AliasDecl(startIndex, attrs);
													else if (la2 == TT.LBrack)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												}
											}
											break;
										case TT.LParen:
											{
												if (Try_BlockCallStmt_Test0(1)) {
													if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
														goto matchConstructor;
													else
														goto matchBlockCallStmt;
												} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchExprStatement;
											}
										case TT.ColonColon:
										case TT.Dot:
										case TT.LBrack:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.QuestionMark:
											{
												if (Try_FinishPrimaryExpr_Test0(1)) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													default:
														goto matchExprStatement;
													}
												} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.LT:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												} else {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												}
											}
											break;
										case TT.@this:
											r = MethodOrPropertyOrVar(startIndex, attrs);
											break;
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.Forward:
											goto matchBlockCallStmt;
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
										case TT.Add:
										case TT.And:
										case TT.AndBits:
										case TT.BQString:
										case TT.CompoundSet:
										case TT.DivMod:
										case TT.DotDot:
										case TT.EqNeq:
										case TT.GT:
										case TT.IncDec:
										case TT.LambdaArrow:
										case TT.LEGE:
										case TT.NotBits:
										case TT.NullCoalesce:
										case TT.NullDot:
										case TT.OrBits:
										case TT.OrXor:
										case TT.Power:
										case TT.PtrArrow:
										case TT.QuickBind:
										case TT.Semicolon:
										case TT.Set:
										case TT.Sub:
										case TT.XorBits:
											goto matchExprStatement;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									} else {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											{
												if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
													if (Try_MethodOrPropertyOrVar_Test0(1)) {
														la2 = LA(2);
														if (Stmt_set1.Contains((int) la2))
															r = AliasDecl(startIndex, attrs);
														else if (la2 == TT.Comma || la2 == TT.LBrack || la2 == TT.Semicolon)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													} else {
														la2 = LA(2);
														if (Stmt_set1.Contains((int) la2))
															r = AliasDecl(startIndex, attrs);
														else if (la2 == TT.LBrack)
															r = MethodOrPropertyOrVar(startIndex, attrs);
														else
															goto error;
													}
												} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
													la2 = LA(2);
													if (Stmt_set1.Contains((int) la2))
														r = AliasDecl(startIndex, attrs);
													else if (la2 == TT.Comma || la2 == TT.LBrack || la2 == TT.Semicolon)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												} else {
													la2 = LA(2);
													if (Stmt_set1.Contains((int) la2))
														r = AliasDecl(startIndex, attrs);
													else if (la2 == TT.LBrack)
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
												}
											}
											break;
										case TT.LParen:
											{
												if (Try_Constructor_Test2(1))
													goto matchConstructor;
												else if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.ColonColon:
										case TT.Dot:
										case TT.LBrack:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.QuestionMark:
											{
												if (Try_FinishPrimaryExpr_Test0(1)) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													default:
														goto matchExprStatement;
													}
												} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.LT:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												} else {
													switch (LA(2)) {
													case TT.@this:
													case TT.@operator:
													case TT.ContextualKeyword:
													case TT.GT:
													case TT.Id:
													case TT.LBrack:
													case TT.LParen:
													case TT.Mul:
													case TT.QuestionMark:
													case TT.RBrack:
													case TT.Substitute:
													case TT.TypeKeyword:
														r = MethodOrPropertyOrVar(startIndex, attrs);
														break;
													case TT.@base:
													case TT.@default:
													case TT.@checked:
													case TT.@delegate:
													case TT.@is:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.DotDot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Literal:
													case TT.Not:
													case TT.NotBits:
													case TT.Power:
													case TT.Sub:
														goto matchExprStatement;
													default:
														goto error;
													}
												}
											}
											break;
										case TT.@this:
											r = MethodOrPropertyOrVar(startIndex, attrs);
											break;
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto matchBlockCallStmt;
												else
													goto matchExprStatement;
											}
										case TT.Forward:
											goto matchBlockCallStmt;
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
										case TT.Add:
										case TT.And:
										case TT.AndBits:
										case TT.BQString:
										case TT.CompoundSet:
										case TT.DivMod:
										case TT.DotDot:
										case TT.EqNeq:
										case TT.GT:
										case TT.IncDec:
										case TT.LambdaArrow:
										case TT.LEGE:
										case TT.NotBits:
										case TT.NullCoalesce:
										case TT.NullDot:
										case TT.OrBits:
										case TT.OrXor:
										case TT.Power:
										case TT.PtrArrow:
										case TT.QuickBind:
										case TT.Semicolon:
										case TT.Set:
										case TT.Sub:
										case TT.XorBits:
											goto matchExprStatement;
										case TT.Colon:
											goto matchLabelStmt;
										default:
											goto error;
										}
									}
								} else if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = AliasDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchExprStatement;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.Forward:
										goto matchBlockCallStmt;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.ColonColon:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.Dot:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LBrack:
									case TT.LEGE:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuestionMark:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = AliasDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto matchConstructor;
											else if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.Forward:
										goto matchBlockCallStmt;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.ColonColon:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.Dot:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LBrack:
									case TT.LEGE:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuestionMark:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								}
							} else if (Try_Stmt_Test0(0)) {
								if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = AliasDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											} else
												goto matchConstructor;
										}
									case TT.Forward:
									case TT.LBrace:
										goto matchBlockCallStmt;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = AliasDecl(startIndex, attrs);
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										}
									case TT.Forward:
									case TT.LBrace:
										goto matchBlockCallStmt;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								}
							} else if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = AliasDecl(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else
											goto matchConstructor;
									}
								case TT.Forward:
								case TT.LBrace:
									goto matchBlockCallStmt;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							} else {
								switch (LA(1)) {
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = AliasDecl(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									}
								case TT.Forward:
								case TT.LBrace:
									goto matchBlockCallStmt;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							}
						} else if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
							if (Try_Stmt_Test0(0)) {
								if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto matchConstructor;
												else
													goto matchBlockCallStmt;
											} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchExprStatement;
										}
									case TT.ColonColon:
									case TT.Dot:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										{
											if (Try_FinishPrimaryExpr_Test0(1)) {
												switch (LA(2)) {
												case TT.@this:
												case TT.@operator:
												case TT.ContextualKeyword:
												case TT.GT:
												case TT.Id:
												case TT.LBrack:
												case TT.LParen:
												case TT.Mul:
												case TT.QuestionMark:
												case TT.RBrack:
												case TT.Substitute:
												case TT.TypeKeyword:
													r = MethodOrPropertyOrVar(startIndex, attrs);
													break;
												default:
													goto matchExprStatement;
												}
											} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
												switch (LA(2)) {
												case TT.@this:
												case TT.@operator:
												case TT.ContextualKeyword:
												case TT.GT:
												case TT.Id:
												case TT.LBrack:
												case TT.LParen:
												case TT.Mul:
												case TT.QuestionMark:
												case TT.RBrack:
												case TT.Substitute:
												case TT.TypeKeyword:
													r = MethodOrPropertyOrVar(startIndex, attrs);
													break;
												case TT.@base:
												case TT.@default:
												case TT.@checked:
												case TT.@delegate:
												case TT.@is:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.DotDot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Literal:
												case TT.LT:
												case TT.Not:
												case TT.NotBits:
												case TT.Power:
												case TT.Sub:
													goto matchExprStatement;
												default:
													goto error;
												}
											} else {
												switch (LA(2)) {
												case TT.@this:
												case TT.@operator:
												case TT.ContextualKeyword:
												case TT.GT:
												case TT.Id:
												case TT.LBrack:
												case TT.LParen:
												case TT.Mul:
												case TT.QuestionMark:
												case TT.RBrack:
												case TT.Substitute:
												case TT.TypeKeyword:
													r = MethodOrPropertyOrVar(startIndex, attrs);
													break;
												case TT.@base:
												case TT.@default:
												case TT.@checked:
												case TT.@delegate:
												case TT.@is:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.DotDot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Literal:
												case TT.Not:
												case TT.NotBits:
												case TT.Power:
												case TT.Sub:
													goto matchExprStatement;
												default:
													goto error;
												}
											}
										}
										break;
									case TT.@this:
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = MethodOrPropertyOrVar(startIndex, attrs);
										break;
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.Forward:
										goto matchBlockCallStmt;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LEGE:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								} else {
									switch (LA(1)) {
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto matchConstructor;
											else if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.ColonColon:
									case TT.Dot:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										{
											if (Try_FinishPrimaryExpr_Test0(1)) {
												switch (LA(2)) {
												case TT.@this:
												case TT.@operator:
												case TT.ContextualKeyword:
												case TT.GT:
												case TT.Id:
												case TT.LBrack:
												case TT.LParen:
												case TT.Mul:
												case TT.QuestionMark:
												case TT.RBrack:
												case TT.Substitute:
												case TT.TypeKeyword:
													r = MethodOrPropertyOrVar(startIndex, attrs);
													break;
												default:
													goto matchExprStatement;
												}
											} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
												switch (LA(2)) {
												case TT.@this:
												case TT.@operator:
												case TT.ContextualKeyword:
												case TT.GT:
												case TT.Id:
												case TT.LBrack:
												case TT.LParen:
												case TT.Mul:
												case TT.QuestionMark:
												case TT.RBrack:
												case TT.Substitute:
												case TT.TypeKeyword:
													r = MethodOrPropertyOrVar(startIndex, attrs);
													break;
												case TT.@base:
												case TT.@default:
												case TT.@checked:
												case TT.@delegate:
												case TT.@is:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.DotDot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Literal:
												case TT.LT:
												case TT.Not:
												case TT.NotBits:
												case TT.Power:
												case TT.Sub:
													goto matchExprStatement;
												default:
													goto error;
												}
											} else {
												switch (LA(2)) {
												case TT.@this:
												case TT.@operator:
												case TT.ContextualKeyword:
												case TT.GT:
												case TT.Id:
												case TT.LBrack:
												case TT.LParen:
												case TT.Mul:
												case TT.QuestionMark:
												case TT.RBrack:
												case TT.Substitute:
												case TT.TypeKeyword:
													r = MethodOrPropertyOrVar(startIndex, attrs);
													break;
												case TT.@base:
												case TT.@default:
												case TT.@checked:
												case TT.@delegate:
												case TT.@is:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.DotDot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Literal:
												case TT.Not:
												case TT.NotBits:
												case TT.Power:
												case TT.Sub:
													goto matchExprStatement;
												default:
													goto error;
												}
											}
										}
										break;
									case TT.@this:
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = MethodOrPropertyOrVar(startIndex, attrs);
										break;
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto matchBlockCallStmt;
											else
												goto matchExprStatement;
										}
									case TT.Forward:
										goto matchBlockCallStmt;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LEGE:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									case TT.Colon:
										goto matchLabelStmt;
									default:
										goto error;
									}
								}
							} else if (_spaceName == LT(0).Value) {
								la1 = LA(1);
								if (la1 == TT.LParen) {
									if (Try_BlockCallStmt_Test0(1)) {
										if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
										goto matchConstructor;
									else
										goto matchExprStatement;
								} else if (la1 == TT.LBrace) {
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.Forward)
									goto matchBlockCallStmt;
								else if (Stmt_set2.Contains((int) la1))
									goto matchExprStatement;
								else if (la1 == TT.Colon)
									goto matchLabelStmt;
								else
									goto error;
							} else {
								la1 = LA(1);
								if (la1 == TT.LParen) {
									if (Try_Constructor_Test2(1))
										goto matchConstructor;
									else if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.LBrace) {
									if (Try_BlockCallStmt_Test0(1))
										goto matchBlockCallStmt;
									else
										goto matchExprStatement;
								} else if (la1 == TT.Forward)
									goto matchBlockCallStmt;
								else if (Stmt_set2.Contains((int) la1))
									goto matchExprStatement;
								else if (la1 == TT.Colon)
									goto matchLabelStmt;
								else
									goto error;
							}
						} else if (Try_Stmt_Test0(0)) {
							if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else
											goto matchConstructor;
									}
								case TT.Forward:
								case TT.LBrace:
									goto matchBlockCallStmt;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							} else {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									}
								case TT.Forward:
								case TT.LBrace:
									goto matchBlockCallStmt;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							}
						} else if (_spaceName == LT(0).Value) {
							switch (LA(1)) {
							case TT.LParen:
								{
									if (Try_BlockCallStmt_Test0(1)) {
										if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchBlockCallStmt;
									} else
										goto matchConstructor;
								}
							case TT.Forward:
							case TT.LBrace:
								goto matchBlockCallStmt;
							case TT.Colon:
								goto matchLabelStmt;
							default:
								goto error;
							}
						} else {
							switch (LA(1)) {
							case TT.LParen:
								{
									if (Try_Constructor_Test2(1))
										goto matchConstructor;
									else
										goto matchBlockCallStmt;
								}
							case TT.Forward:
							case TT.LBrace:
								goto matchBlockCallStmt;
							case TT.Colon:
								goto matchLabelStmt;
							default:
								goto error;
							}
						}
					}
					break;
				case TT.@operator:
					{
						if (Try_Stmt_Test0(0)) {
							switch (LA(1)) {
							case TT.Substitute:
								{
									switch (LA(2)) {
									case TT.@base:
									case TT.@default:
									case TT.@this:
									case TT.@checked:
									case TT.@delegate:
									case TT.@is:
									case TT.@new:
									case TT.@operator:
									case TT.@sizeof:
									case TT.@typeof:
									case TT.@unchecked:
									case TT.At:
									case TT.ContextualKeyword:
									case TT.Dot:
									case TT.Id:
									case TT.LBrace:
									case TT.Literal:
									case TT.LParen:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = OperatorCast(startIndex, attrs);
										break;
									case TT.ColonColon:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										r = MethodOrPropertyOrVar(startIndex, attrs);
										break;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LEGE:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									default:
										goto error;
									}
								}
								break;
							case TT.@operator:
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.TypeKeyword:
								r = OperatorCast(startIndex, attrs);
								break;
							case TT.Add:
							case TT.And:
							case TT.AndBits:
							case TT.At:
							case TT.Backslash:
							case TT.BQString:
							case TT.Colon:
							case TT.ColonColon:
							case TT.CompoundSet:
							case TT.DivMod:
							case TT.Dot:
							case TT.DotDot:
							case TT.EqNeq:
							case TT.Forward:
							case TT.GT:
							case TT.IncDec:
							case TT.LambdaArrow:
							case TT.LEGE:
							case TT.LT:
							case TT.Mul:
							case TT.Not:
							case TT.NotBits:
							case TT.NullCoalesce:
							case TT.NullDot:
							case TT.OrBits:
							case TT.OrXor:
							case TT.Power:
							case TT.PtrArrow:
							case TT.QuestionMark:
							case TT.QuickBind:
							case TT.QuickBindSet:
							case TT.Set:
							case TT.Sub:
							case TT.XorBits:
								{
									switch (LA(2)) {
									case TT.@this:
									case TT.@operator:
									case TT.ColonColon:
									case TT.ContextualKeyword:
									case TT.Dot:
									case TT.Id:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = MethodOrPropertyOrVar(startIndex, attrs);
										break;
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
									case TT.Add:
									case TT.And:
									case TT.AndBits:
									case TT.BQString:
									case TT.CompoundSet:
									case TT.DivMod:
									case TT.DotDot:
									case TT.EqNeq:
									case TT.GT:
									case TT.IncDec:
									case TT.LambdaArrow:
									case TT.LBrace:
									case TT.LEGE:
									case TT.LParen:
									case TT.NotBits:
									case TT.NullCoalesce:
									case TT.NullDot:
									case TT.OrBits:
									case TT.OrXor:
									case TT.Power:
									case TT.PtrArrow:
									case TT.QuickBind:
									case TT.Semicolon:
									case TT.Set:
									case TT.Sub:
									case TT.XorBits:
										goto matchExprStatement;
									default:
										goto error;
									}
								}
								break;
							default:
								goto error;
							}
						} else {
							switch (LA(1)) {
							case TT.Substitute:
								{
									la2 = LA(2);
									switch (la2) {
									case TT.@base:
									case TT.@default:
									case TT.@this:
									case TT.@checked:
									case TT.@delegate:
									case TT.@is:
									case TT.@new:
									case TT.@operator:
									case TT.@sizeof:
									case TT.@typeof:
									case TT.@unchecked:
									case TT.At:
									case TT.ContextualKeyword:
									case TT.Dot:
									case TT.Id:
									case TT.LBrace:
									case TT.Literal:
									case TT.LParen:
									case TT.Substitute:
									case TT.TypeKeyword:
										r = OperatorCast(startIndex, attrs);
										break;
									default:
										if (Stmt_set3.Contains((int) la2))
											goto matchExprStatement;
										else
											goto error;
									}
								}
								break;
							case TT.@operator:
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.TypeKeyword:
								r = OperatorCast(startIndex, attrs);
								break;
							case TT.Add:
							case TT.And:
							case TT.AndBits:
							case TT.At:
							case TT.Backslash:
							case TT.BQString:
							case TT.Colon:
							case TT.ColonColon:
							case TT.CompoundSet:
							case TT.DivMod:
							case TT.Dot:
							case TT.DotDot:
							case TT.EqNeq:
							case TT.Forward:
							case TT.GT:
							case TT.IncDec:
							case TT.LambdaArrow:
							case TT.LEGE:
							case TT.LT:
							case TT.Mul:
							case TT.Not:
							case TT.NotBits:
							case TT.NullCoalesce:
							case TT.NullDot:
							case TT.OrBits:
							case TT.OrXor:
							case TT.Power:
							case TT.PtrArrow:
							case TT.QuestionMark:
							case TT.QuickBind:
							case TT.QuickBindSet:
							case TT.Set:
							case TT.Sub:
							case TT.XorBits:
								goto matchExprStatement;
							default:
								goto error;
							}
						}
					}
					break;
				case TT.Id:
					{
						if (Try_Stmt_Test0(0)) {
							if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto matchConstructor;
											else
												goto matchBlockCallStmt;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto matchConstructor;
										else
											goto matchExprStatement;
									}
								case TT.ColonColon:
								case TT.Dot:
								case TT.LBrack:
								case TT.LT:
								case TT.Mul:
								case TT.Not:
								case TT.QuestionMark:
									{
										if (Try_FinishPrimaryExpr_Test0(1)) {
											switch (LA(2)) {
											case TT.@this:
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.GT:
											case TT.Id:
											case TT.LBrack:
											case TT.LParen:
											case TT.Mul:
											case TT.QuestionMark:
											case TT.RBrack:
											case TT.Substitute:
											case TT.TypeKeyword:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											default:
												goto matchExprStatement;
											}
										} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
											switch (LA(2)) {
											case TT.@this:
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.GT:
											case TT.Id:
											case TT.LBrack:
											case TT.LParen:
											case TT.Mul:
											case TT.QuestionMark:
											case TT.RBrack:
											case TT.Substitute:
											case TT.TypeKeyword:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											case TT.@base:
											case TT.@default:
											case TT.@checked:
											case TT.@delegate:
											case TT.@is:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.DotDot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Literal:
											case TT.LT:
											case TT.Not:
											case TT.NotBits:
											case TT.Power:
											case TT.Sub:
												goto matchExprStatement;
											default:
												goto error;
											}
										} else {
											switch (LA(2)) {
											case TT.@this:
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.GT:
											case TT.Id:
											case TT.LBrack:
											case TT.LParen:
											case TT.Mul:
											case TT.QuestionMark:
											case TT.RBrack:
											case TT.Substitute:
											case TT.TypeKeyword:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											case TT.@base:
											case TT.@default:
											case TT.@checked:
											case TT.@delegate:
											case TT.@is:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.DotDot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Literal:
											case TT.Not:
											case TT.NotBits:
											case TT.Power:
											case TT.Sub:
												goto matchExprStatement;
											default:
												goto error;
											}
										}
									}
									break;
								case TT.@this:
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											goto matchExprStatement;
									}
								case TT.Forward:
									goto matchBlockCallStmt;
								case EOF:
								case TT.@as:
								case TT.@catch:
								case TT.@else:
								case TT.@finally:
								case TT.@in:
								case TT.@is:
								case TT.@using:
								case TT.@while:
								case TT.Add:
								case TT.And:
								case TT.AndBits:
								case TT.BQString:
								case TT.CompoundSet:
								case TT.DivMod:
								case TT.DotDot:
								case TT.EqNeq:
								case TT.GT:
								case TT.IncDec:
								case TT.LambdaArrow:
								case TT.LEGE:
								case TT.NotBits:
								case TT.NullCoalesce:
								case TT.NullDot:
								case TT.OrBits:
								case TT.OrXor:
								case TT.Power:
								case TT.PtrArrow:
								case TT.QuickBind:
								case TT.Semicolon:
								case TT.Set:
								case TT.Sub:
								case TT.XorBits:
									goto matchExprStatement;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							} else {
								switch (LA(1)) {
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto matchConstructor;
										else if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											goto matchExprStatement;
									}
								case TT.ColonColon:
								case TT.Dot:
								case TT.LBrack:
								case TT.LT:
								case TT.Mul:
								case TT.Not:
								case TT.QuestionMark:
									{
										if (Try_FinishPrimaryExpr_Test0(1)) {
											switch (LA(2)) {
											case TT.@this:
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.GT:
											case TT.Id:
											case TT.LBrack:
											case TT.LParen:
											case TT.Mul:
											case TT.QuestionMark:
											case TT.RBrack:
											case TT.Substitute:
											case TT.TypeKeyword:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											default:
												goto matchExprStatement;
											}
										} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
											switch (LA(2)) {
											case TT.@this:
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.GT:
											case TT.Id:
											case TT.LBrack:
											case TT.LParen:
											case TT.Mul:
											case TT.QuestionMark:
											case TT.RBrack:
											case TT.Substitute:
											case TT.TypeKeyword:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											case TT.@base:
											case TT.@default:
											case TT.@checked:
											case TT.@delegate:
											case TT.@is:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.DotDot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Literal:
											case TT.LT:
											case TT.Not:
											case TT.NotBits:
											case TT.Power:
											case TT.Sub:
												goto matchExprStatement;
											default:
												goto error;
											}
										} else {
											switch (LA(2)) {
											case TT.@this:
											case TT.@operator:
											case TT.ContextualKeyword:
											case TT.GT:
											case TT.Id:
											case TT.LBrack:
											case TT.LParen:
											case TT.Mul:
											case TT.QuestionMark:
											case TT.RBrack:
											case TT.Substitute:
											case TT.TypeKeyword:
												r = MethodOrPropertyOrVar(startIndex, attrs);
												break;
											case TT.@base:
											case TT.@default:
											case TT.@checked:
											case TT.@delegate:
											case TT.@is:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.DotDot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Literal:
											case TT.Not:
											case TT.NotBits:
											case TT.Power:
											case TT.Sub:
												goto matchExprStatement;
											default:
												goto error;
											}
										}
									}
									break;
								case TT.@this:
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto matchBlockCallStmt;
										else
											goto matchExprStatement;
									}
								case TT.Forward:
									goto matchBlockCallStmt;
								case EOF:
								case TT.@as:
								case TT.@catch:
								case TT.@else:
								case TT.@finally:
								case TT.@in:
								case TT.@is:
								case TT.@using:
								case TT.@while:
								case TT.Add:
								case TT.And:
								case TT.AndBits:
								case TT.BQString:
								case TT.CompoundSet:
								case TT.DivMod:
								case TT.DotDot:
								case TT.EqNeq:
								case TT.GT:
								case TT.IncDec:
								case TT.LambdaArrow:
								case TT.LEGE:
								case TT.NotBits:
								case TT.NullCoalesce:
								case TT.NullDot:
								case TT.OrBits:
								case TT.OrXor:
								case TT.Power:
								case TT.PtrArrow:
								case TT.QuickBind:
								case TT.Semicolon:
								case TT.Set:
								case TT.Sub:
								case TT.XorBits:
									goto matchExprStatement;
								case TT.Colon:
									goto matchLabelStmt;
								default:
									goto error;
								}
							}
						} else if (_spaceName == LT(0).Value) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_BlockCallStmt_Test0(1)) {
									if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
										goto matchConstructor;
									else
										goto matchBlockCallStmt;
								} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
									goto matchConstructor;
								else
									goto matchExprStatement;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.Forward)
								goto matchBlockCallStmt;
							else if (Stmt_set2.Contains((int) la1))
								goto matchExprStatement;
							else if (la1 == TT.Colon)
								goto matchLabelStmt;
							else
								goto error;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto matchConstructor;
								else if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto matchBlockCallStmt;
								else
									goto matchExprStatement;
							} else if (la1 == TT.Forward)
								goto matchBlockCallStmt;
							else if (Stmt_set2.Contains((int) la1))
								goto matchExprStatement;
							else if (la1 == TT.Colon)
								goto matchLabelStmt;
							else
								goto error;
						}
					}
					break;
				case TT.@this:
					{
						if (_spaceName != S.Fn || LA(0 + 3) == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test1(1) || Try_Constructor_Test2(1))
									goto matchConstructor;
								else
									goto matchExprStatement;
							} else if (Stmt_set4.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto matchConstructor;
								else
									goto matchExprStatement;
							} else if (Stmt_set4.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						}
					}
				case TT.NotBits:
					{
						switch (LA(1)) {
						case TT.@this:
						case TT.ContextualKeyword:
						case TT.Id:
							{
								la2 = LA(2);
								if (la2 == TT.LParen) {
									r = Destructor(startIndex, attrs);
									#line 1034 "EcsParserGrammar.les"
									if ((wc != 0)) {
										NonKeywordAttrError(attrs, "destructor");
									}
									#line default
								} else if (Stmt_set4.Contains((int) la2))
									goto matchExprStatement;
								else
									goto error;
							}
							break;
						case TT.@base:
						case TT.@default:
						case TT.@checked:
						case TT.@delegate:
						case TT.@is:
						case TT.@new:
						case TT.@operator:
						case TT.@sizeof:
						case TT.@typeof:
						case TT.@unchecked:
						case TT.Add:
						case TT.AndBits:
						case TT.At:
						case TT.Dot:
						case TT.DotDot:
						case TT.Forward:
						case TT.IncDec:
						case TT.LBrace:
						case TT.Literal:
						case TT.LParen:
						case TT.Mul:
						case TT.Not:
						case TT.NotBits:
						case TT.Power:
						case TT.Sub:
						case TT.Substitute:
						case TT.TypeKeyword:
							goto matchExprStatement;
						default:
							goto error;
						}
					}
					break;
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (Try_Stmt_Test0(0)) {
							la1 = LA(1);
							if (Stmt_set8.Contains((int) la1)) {
								if (!(_insideLinqExpr && LinqKeywords.Contains(LT(1).Value))) {
									if (Try_MethodOrPropertyOrVar_Test0(1)) {
										if (Down(1) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow)) {
											if (Try_FinishPrimaryExpr_Test0(1)) {
												la2 = LA(2);
												if (Stmt_set5.Contains((int) la2))
													r = MethodOrPropertyOrVar(startIndex, attrs);
												else
													goto matchExprStatement;
											} else {
												la2 = LA(2);
												switch (la2) {
												case EOF:
												case TT.@as:
												case TT.@catch:
												case TT.@else:
												case TT.@finally:
												case TT.@in:
												case TT.@using:
												case TT.@while:
													goto matchExprStatement;
												default:
													if (Stmt_set5.Contains((int) la2))
														r = MethodOrPropertyOrVar(startIndex, attrs);
													else
														goto error;
													break;
												}
											}
										} else if (Try_FinishPrimaryExpr_Test0(1)) {
											la2 = LA(2);
											if (Stmt_set5.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto matchExprStatement;
										} else {
											la2 = LA(2);
											switch (la2) {
											case EOF:
											case TT.@as:
											case TT.@catch:
											case TT.@else:
											case TT.@finally:
											case TT.@in:
											case TT.@using:
											case TT.@while:
												goto matchExprStatement;
											default:
												if (Stmt_set5.Contains((int) la2))
													r = MethodOrPropertyOrVar(startIndex, attrs);
												else
													goto error;
												break;
											}
										}
									} else if (Down(1) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow)) {
										if (Try_FinishPrimaryExpr_Test0(1)) {
											la2 = LA(2);
											if (Stmt_set6.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto matchExprStatement;
										} else {
											la2 = LA(2);
											switch (la2) {
											case EOF:
											case TT.@as:
											case TT.@catch:
											case TT.@else:
											case TT.@finally:
											case TT.@in:
											case TT.@using:
											case TT.@while:
											case TT.Semicolon:
												goto matchExprStatement;
											default:
												if (Stmt_set6.Contains((int) la2))
													r = MethodOrPropertyOrVar(startIndex, attrs);
												else
													goto error;
												break;
											}
										}
									} else if (Try_FinishPrimaryExpr_Test0(1)) {
										la2 = LA(2);
										if (Stmt_set6.Contains((int) la2))
											r = MethodOrPropertyOrVar(startIndex, attrs);
										else
											goto matchExprStatement;
									} else {
										la2 = LA(2);
										switch (la2) {
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@using:
										case TT.@while:
										case TT.Semicolon:
											goto matchExprStatement;
										default:
											if (Stmt_set6.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto error;
											break;
										}
									}
								} else if (Try_MethodOrPropertyOrVar_Test0(1)) {
									if (Down(1) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow)) {
										if (Try_FinishPrimaryExpr_Test0(1)) {
											la2 = LA(2);
											if (Stmt_set5.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto matchExprStatement;
										} else {
											la2 = LA(2);
											switch (la2) {
											case EOF:
											case TT.@as:
											case TT.@catch:
											case TT.@else:
											case TT.@finally:
											case TT.@in:
											case TT.@using:
											case TT.@while:
												goto matchExprStatement;
											default:
												if (Stmt_set5.Contains((int) la2))
													r = MethodOrPropertyOrVar(startIndex, attrs);
												else
													goto error;
												break;
											}
										}
									} else if (Try_FinishPrimaryExpr_Test0(1)) {
										la2 = LA(2);
										if (Stmt_set5.Contains((int) la2))
											r = MethodOrPropertyOrVar(startIndex, attrs);
										else
											goto matchExprStatement;
									} else {
										la2 = LA(2);
										switch (la2) {
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@using:
										case TT.@while:
											goto matchExprStatement;
										default:
											if (Stmt_set5.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto error;
											break;
										}
									}
								} else if (Down(1) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow)) {
									if (Try_FinishPrimaryExpr_Test0(1)) {
										la2 = LA(2);
										if (Stmt_set6.Contains((int) la2))
											r = MethodOrPropertyOrVar(startIndex, attrs);
										else
											goto matchExprStatement;
									} else {
										la2 = LA(2);
										switch (la2) {
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@using:
										case TT.@while:
										case TT.Semicolon:
											goto matchExprStatement;
										default:
											if (Stmt_set6.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto error;
											break;
										}
									}
								} else if (Try_FinishPrimaryExpr_Test0(1)) {
									la2 = LA(2);
									if (Stmt_set6.Contains((int) la2))
										r = MethodOrPropertyOrVar(startIndex, attrs);
									else
										goto matchExprStatement;
								} else {
									la2 = LA(2);
									switch (la2) {
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@using:
									case TT.@while:
									case TT.Semicolon:
										goto matchExprStatement;
									default:
										if (Stmt_set6.Contains((int) la2))
											r = MethodOrPropertyOrVar(startIndex, attrs);
										else
											goto error;
										break;
									}
								}
							} else if (Stmt_set7.Contains((int) la1))
								goto matchExprStatement;
							else
								goto error;
						} else
							goto matchExprStatement;
					}
					break;
				case TT.LBrace:
					{
						r = BracedBlock();
						r = r.PlusAttrs(attrs);
						if ((wc != 0)) {
							NonKeywordAttrError(attrs, "braced-block statement");
						}
					}
					break;
				case TT.@checked:
				case TT.@unchecked:
					{
						la1 = LA(1);
						if (la1 == TT.LParen)
							goto matchExprStatement;
						else if (la1 == TT.LBrace) {
							r = CheckedOrUncheckedStmt(startIndex);
							#line 1044 "EcsParserGrammar.les"
							r = r.PlusAttrs(attrs);
							#line default
						} else
							goto error;
					}
					break;
				case TT.@default:
					{
						la1 = LA(1);
						if (la1 == TT.LParen)
							goto matchExprStatement;
						else if (la1 == TT.Colon)
							goto matchLabelStmt;
						else
							goto error;
					}
				case TT.@base:
				case TT.@is:
				case TT.@new:
				case TT.@sizeof:
				case TT.@typeof:
				case TT.Add:
				case TT.AndBits:
				case TT.At:
				case TT.Dot:
				case TT.DotDot:
				case TT.Forward:
				case TT.IncDec:
				case TT.Literal:
				case TT.LParen:
				case TT.Mul:
				case TT.Not:
				case TT.Power:
				case TT.Sub:
					goto matchExprStatement;
				case TT.@do:
					{
						r = DoStmt(startIndex);
						#line 1045 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@case:
					{
						r = CaseStmt(startIndex);
						#line 1046 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@goto:
					{
						la1 = LA(1);
						if (Stmt_set9.Contains((int) la1)) {
							r = GotoStmt(startIndex);
							#line 1047 "EcsParserGrammar.les"
							r = r.PlusAttrs(attrs);
							#line default
						} else if (la1 == TT.@case) {
							r = GotoCaseStmt(startIndex);
							#line 1048 "EcsParserGrammar.les"
							r = r.PlusAttrs(attrs);
							#line default
						} else
							goto error;
					}
					break;
				case TT.@break:
				case TT.@continue:
				case TT.@return:
				case TT.@throw:
					{
						r = ReturnBreakContinueThrow(startIndex);
						#line 1050 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@while:
					{
						r = WhileStmt(startIndex);
						#line 1051 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@for:
					{
						r = ForStmt(startIndex);
						#line 1052 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@foreach:
					{
						r = ForEachStmt(startIndex);
						#line 1053 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@if:
					{
						r = IfStmt(startIndex);
						r = r.PlusAttrs(attrs);
						if ((wc != 0)) {
							NonKeywordAttrError(attrs, "if statement");
						}
					}
					break;
				case TT.@switch:
					{
						r = SwitchStmt(startIndex);
						#line 1056 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@using:
					{
						la1 = LA(1);
						if (la1 == TT.LParen) {
							r = UsingStmt(startIndex);
							#line 1057 "EcsParserGrammar.les"
							r = r.PlusAttrs(attrs);
							#line default
						} else if (Stmt_set10.Contains((int) la1))
							r = UsingDirective(startIndex, attrs);
						else
							goto error;
					}
					break;
				case TT.@lock:
					{
						r = LockStmt(startIndex);
						#line 1059 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@fixed:
					{
						r = FixedStmt(startIndex);
						#line 1060 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.@try:
					{
						r = TryStmt(startIndex);
						#line 1061 "EcsParserGrammar.les"
						r = r.PlusAttrs(attrs);
						#line default
					}
					break;
				case TT.Semicolon:
					{
						var t = MatchAny();
						r = F.Id(S.Missing, startIndex, t.EndIndex).PlusAttrs(attrs);
						if ((wc != 0)) {
							NonKeywordAttrError(attrs, "empty statement");
						}
					}
					break;
				default:
					goto error;
				}
				break;
			matchConstructor:
				{
					r = Constructor(startIndex, attrs);
					#line 1031 "EcsParserGrammar.les"
					if ((wc != 0 && !r.Args[1, F.Missing].IsIdNamed(S.This))) {
						NonKeywordAttrError(attrs, "constructor");
					}
					#line default
				}
				break;
			matchBlockCallStmt:
				{
					r = BlockCallStmt();
					r = r.PlusAttrs(attrs);
					if ((wc != 0)) {
						NonKeywordAttrError(attrs, "block-call statement");
					}
				}
				break;
			matchExprStatement:
				{
					r = ExprStatement();
					r = r.PlusAttrs(attrs);
					if ((wc != 0)) {
						NonKeywordAttrError(attrs, "expression");
					}
				}
				break;
			matchLabelStmt:
				{
					r = LabelStmt(startIndex);
					#line 1049 "EcsParserGrammar.les"
					r = r.PlusAttrs(attrs);
					#line default
				}
				break;
			error:
				{
					#line 1064 "EcsParserGrammar.les"
					r = Error("Syntax error: statement expected at '{0}'", LT(0).SourceText(SourceFile.Text));
					#line default
					ScanToEndOfStmt();
				}
			} while (false);
			#line 1067 "EcsParserGrammar.les"
			return r;
			#line default
		}
		LNode ExprStatement()
		{
			var r = Expr(ContinueExpr);
			// Line 1072: ((EOF|TT.@catch|TT.@else|TT.@finally|TT.@while) =>  | default TT.Semicolon)
			switch (LA0) {
			case EOF:
			case TT.@catch:
			case TT.@else:
			case TT.@finally:
			case TT.@while:
				{
					#line 1073 "EcsParserGrammar.les"
					r = F.Call(S.Result, r, r.Range.StartIndex, r.Range.EndIndex);
					#line default
				}
				break;
			default:
				Match((int) TT.Semicolon);
				break;
			}
			#line 1076 "EcsParserGrammar.les"
			return r;
			#line default
		}
		void ScanToEndOfStmt()
		{
			TokenType la0;
			// Line 1081: greedy(~(EOF|TT.LBrace|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.LBrace || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			// Line 1082: greedy(TT.Semicolon | TT.LBrace (TT.RBrace)?)?
			la0 = LA0;
			if (la0 == TT.Semicolon)
				Skip();
			else if (la0 == TT.LBrace) {
				Skip();
				// Line 1082: (TT.RBrace)?
				la0 = LA0;
				if (la0 == TT.RBrace)
					Skip();
			}
		}
		LNode SpaceDecl(int startIndex, VList<LNode> attrs)
		{
			var t = MatchAny();
			#line 1091 "EcsParserGrammar.les"
			var kind = (Symbol) t.Value;
			#line default
			var r = RestOfSpaceDecl(startIndex, kind, attrs);
			#line 1093 "EcsParserGrammar.les"
			return r;
			#line default
		}
		LNode TraitDecl(int startIndex, VList<LNode> attrs)
		{
			Check(Is(0, _trait), "Is($LI, _trait)");
			var t = Match((int) TT.ContextualKeyword);
			var r = RestOfSpaceDecl(startIndex, S.Trait, attrs);
			#line 1099 "EcsParserGrammar.les"
			return r;
			#line default
		}
		LNode RestOfSpaceDecl(int startIndex, Symbol kind, VList<LNode> attrs)
		{
			TokenType la0;
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			WhereClausesOpt(ref name);
			// Line 1106: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				#line 1107 "EcsParserGrammar.les"
				return F.Call(kind, name, bases, startIndex, end.EndIndex).WithAttrs(attrs);
				#line default
			} else {
				var body = BracedBlock(CoreName(name).Name);
				#line 1109 "EcsParserGrammar.les"
				return F.Call(kind, name, bases, body, startIndex, body.Range.EndIndex).WithAttrs(attrs);
				#line default
			}
		}
		LNode AliasDecl(int startIndex, VList<LNode> attrs)
		{
			Check(Is(0, _alias), "Is($LI, _alias)");
			var t = Match((int) TT.ContextualKeyword);
			var newName = ComplexNameDecl();
			Match((int) TT.QuickBindSet, (int) TT.Set);
			var oldName = ComplexNameDecl();
			var r = RestOfAlias(oldName, startIndex, newName);
			#line 1119 "EcsParserGrammar.les"
			return r.WithAttrs(attrs);
			#line default
		}
		LNode UsingDirective(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			Token end = default(Token);
			LNode nsName = default(LNode);
			Token static_ = default(Token);
			Token t = default(Token);
			t = Match((int) TT.@using);
			// Line 1125: (&{Is($LI, S.Static)} TT.AttrKeyword ExprStart TT.Semicolon / ExprStart (&{nsName.Calls(S.Assign, 2) && (aliasedType = nsName[1]) != @null} RestOfAlias | default TT.Semicolon))
			do {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					if (Is(0, S.Static)) {
						static_ = MatchAny();
						nsName = ExprStart(true);
						end = Match((int) TT.Semicolon);
						#line 1127 "EcsParserGrammar.les"
						attrs.Add(F.Id(static_));
						#line default
					} else
						goto matchExprStart;
				} else
					goto matchExprStart;
				break;
			matchExprStart:
				{
					nsName = ExprStart(true);
					#line 1129 "EcsParserGrammar.les"
					LNode aliasedType = null;
					#line default
					// Line 1130: (&{nsName.Calls(S.Assign, 2) && (aliasedType = nsName[1]) != @null} RestOfAlias | default TT.Semicolon)
					do {
						switch (LA0) {
						case TT.Semicolon:
							{
								if (nsName.Calls(S.Assign, 2) && (aliasedType = nsName[1]) != null)
									goto matchRestOfAlias;
								else
									end = Match((int) TT.Semicolon);
							}
							break;
						case TT.Colon:
						case TT.ContextualKeyword:
						case TT.LBrace:
							goto matchRestOfAlias;
						default:
							end = Match((int) TT.Semicolon);
							break;
						}
						break;
					matchRestOfAlias:
						{
							Check(nsName.Calls(S.Assign, 2) && (aliasedType = nsName[1]) != null, "nsName.Calls(S.Assign, 2) && (aliasedType = nsName[1]) != @null");
							#line 1131 "EcsParserGrammar.les"
							nsName = nsName[0];
							#line default
							var r = RestOfAlias(aliasedType, startIndex, nsName);
							#line 1133 "EcsParserGrammar.les"
							return r.WithAttrs(attrs).PlusAttr(_filePrivate);
							#line default
						}
					} while (false);
				}
			} while (false);
			#line 1137 "EcsParserGrammar.les"
			return F.Call(S.Import, nsName, t.StartIndex, end.EndIndex).WithAttrs(attrs);
			#line default
		}
		LNode RestOfAlias(LNode oldName, int startIndex, LNode newName)
		{
			TokenType la0;
			var bases = BaseListOpt();
			WhereClausesOpt(ref newName);
			#line 1143 "EcsParserGrammar.les"
			var name = F.Call(S.Assign, newName, oldName, newName.Range.StartIndex, oldName.Range.EndIndex);
			#line default
			// Line 1144: (TT.Semicolon | BracedBlock)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				#line 1145 "EcsParserGrammar.les"
				return F.Call(S.Alias, name, bases, startIndex, end.EndIndex);
				#line default
			} else {
				var body = BracedBlock(CoreName(newName).Name);
				#line 1147 "EcsParserGrammar.les"
				return F.Call(S.Alias, name, bases, body, startIndex, body.Range.EndIndex);
				#line default
			}
		}
		LNode EnumDecl(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			var t = MatchAny();
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			// Line 1155: (TT.Semicolon | TT.LBrace TT.RBrace)
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				#line 1156 "EcsParserGrammar.les"
				return F.Call(S.Enum, name, bases, startIndex, end.EndIndex).WithAttrs(attrs);
				#line default
			} else {
				var lb = Match((int) TT.LBrace);
				var rb = Match((int) TT.RBrace);
				#line 1159 "EcsParserGrammar.les"
				var list = ExprListInside(lb, true);
				var body = F.Braces(list, lb.StartIndex, rb.EndIndex);
				return F.Call(S.Enum, name, bases, body, startIndex, body.Range.EndIndex).WithAttrs(attrs);
				#line default
			}
		}
		LNode BaseListOpt()
		{
			TokenType la0;
			// Line 1167: (TT.Colon DataType (TT.Comma DataType)* | )
			la0 = LA0;
			if (la0 == TT.Colon) {
				#line 1167 "EcsParserGrammar.les"
				var bases = new VList<LNode>();
				#line default
				Skip();
				bases.Add(DataType());
				// Line 1169: (TT.Comma DataType)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						bases.Add(DataType());
					} else
						break;
				}
				#line 1170 "EcsParserGrammar.les"
				return F.List(bases);
				#line default
			} else {
				#line 1171 "EcsParserGrammar.les"
				return F.List();
				#line default
			}
		}
		void WhereClausesOpt(ref LNode name)
		{
			TokenType la0;
			#line 1193 "EcsParserGrammar.les"
			var list = new BMultiMap<Symbol,LNode>();
			#line default
			// Line 1194: (WhereClause)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword)
					list.Add(WhereClause());
				else
					break;
			}
			#line 1195 "EcsParserGrammar.les"
			if ((list.Count != 0)) {
				if ((!name.CallsMin(S.Of, 2))) {
					Error("'{0}' is not generic and cannot use 'where' clauses.", name.ToString());
				} else {
					var tparams = name.Args.ToWList();
					for (int i = 1; i < tparams.Count; i++) {
						var wheres = list[TParamSymbol(tparams[i])];
						tparams[i] = tparams[i].PlusAttrs(wheres);
						wheres.Clear();
					}
					name = name.WithArgs(tparams.ToVList());
					if ((list.Count > 0)) {
						Error(list[0].Value, "There is no type parameter named '{0}'", list[0].Key);
					}
				}
			}
			#line default
		}
		KeyValuePair<Symbol,LNode> WhereClause()
		{
			TokenType la0;
			Check(Is(0, _where), "Is($LI, _where)");
			var where = MatchAny();
			var T = Match((int) TT.ContextualKeyword, (int) TT.Id);
			Match((int) TT.Colon);
			#line 1225 "EcsParserGrammar.les"
			var constraints = VList<LNode>.Empty;
			#line default
			constraints.Add(WhereConstraint());
			// Line 1227: (TT.Comma WhereConstraint)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					constraints.Add(WhereConstraint());
				} else
					break;
			}
			#line 1228 "EcsParserGrammar.les"
			return new KeyValuePair<Symbol,LNode>((Symbol) T.Value, F.Call(S.Where, constraints, where.StartIndex, constraints.Last.Range.EndIndex));
			#line default
		}
		LNode WhereConstraint()
		{
			TokenType la0;
			// Line 1232: ( (TT.@class|TT.@struct) | TT.@new &{LT($LI).Count == 0} TT.LParen TT.RParen | DataType )
			la0 = LA0;
			if (la0 == TT.@class || la0 == TT.@struct) {
				var t = MatchAny();
				#line 1232 "EcsParserGrammar.les"
				return F.Id(t);
				#line default
			} else if (la0 == TT.@new) {
				var n = MatchAny();
				Check(LT(0).Count == 0, "LT($LI).Count == 0");
				var lp = Match((int) TT.LParen);
				var rp = Match((int) TT.RParen);
				#line 1234 "EcsParserGrammar.les"
				return F.Call(S.New, n.StartIndex, rp.EndIndex);
				#line default
			} else {
				var t = DataType();
				#line 1235 "EcsParserGrammar.les"
				return t;
				#line default
			}
		}
		Token AsmOrModLabel()
		{
			Check(LT(0).Value == _assembly || LT(0).Value == _module, "LT($LI).Value == _assembly || LT($LI).Value == _module");
			var t = Match((int) TT.ContextualKeyword);
			Match((int) TT.Colon);
			#line 1250 "EcsParserGrammar.les"
			return t;
			#line default
		}
		bool Try_Scan_AsmOrModLabel(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_AsmOrModLabel();
		}
		bool Scan_AsmOrModLabel()
		{
			if (!(LT(0).Value == _assembly || LT(0).Value == _module))
				return false;
			if (!TryMatch((int) TT.ContextualKeyword))
				return false;
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
		LNode AssemblyOrModuleAttribute(int startIndex, VList<LNode> attrs)
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var lb = MatchAny();
			var rb = Match((int) TT.RBrack);
			#line 1256 "EcsParserGrammar.les"
			Down(lb);
			#line default
			var kind = AsmOrModLabel();
			#line 1258 "EcsParserGrammar.les"
			var list = new WList<LNode>();
			#line default
			ExprList(list);
			#line 1261 "EcsParserGrammar.les"
			Up();
			var r = F.Call(kind.Value == _module ? S.Module : S.Assembly, list.ToVList(), startIndex, rb.EndIndex);
			return r.WithAttrs(attrs);
			#line default
		}
		static readonly HashSet<int> MethodOrPropertyOrVar_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.Comma, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword, (int) TT.XorBits);
		LNode MethodOrPropertyOrVar(int startIndex, VList<LNode> attrs)
		{
			TokenType la0, la1;
			Token lb = default(Token);
			LNode name = default(LNode);
			Token rb = default(Token);
			LNode result = default(LNode);
			var type = DataType();
			// Line 1276: (&(IdAtom (TT.Comma|TT.Semicolon|TT.Set)) NameAndMaybeInit (TT.Comma NameAndMaybeInit)* TT.Semicolon / (ComplexNameDecl | ComplexThisDecl) (MethodArgListAndBody | WhereClausesOpt (TT.LBrack TT.RBrack)? MethodBodyOrForward))
			do {
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (Try_MethodOrPropertyOrVar_Test0(0)) {
							la1 = LA(1);
							if (MethodOrPropertyOrVar_set0.Contains((int) la1)) {
								MaybeRecognizeVarAsKeyword(ref type);
								var parts = new VList<LNode> { 
									type
								};
								parts.Add(NameAndMaybeInit(IsArrayType(type)));
								// Line 1280: (TT.Comma NameAndMaybeInit)*
								for (;;) {
									la0 = LA0;
									if (la0 == TT.Comma) {
										Skip();
										parts.Add(NameAndMaybeInit(IsArrayType(type)));
									} else
										break;
								}
								var end = Match((int) TT.Semicolon);
								#line 1281 "EcsParserGrammar.les"
								result = F.Call(S.Var, parts, type.Range.StartIndex, end.EndIndex).PlusAttrs(attrs);
								#line default
							} else
								goto match2;
						} else
							goto match2;
					}
					break;
				default:
					goto match2;
				}
				break;
			match2:
				{
					// Line 1285: (ComplexNameDecl | ComplexThisDecl)
					switch (LA0) {
					case TT.@operator:
					case TT.ContextualKeyword:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						name = ComplexNameDecl();
						break;
					default:
						name = ComplexThisDecl();
						break;
					}
					// Line 1287: (MethodArgListAndBody | WhereClausesOpt (TT.LBrack TT.RBrack)? MethodBodyOrForward)
					switch (LA0) {
					case TT.LParen:
						result = MethodArgListAndBody(startIndex, attrs, S.Fn, type, name);
						break;
					case TT.At:
					case TT.ContextualKeyword:
					case TT.Forward:
					case TT.LambdaArrow:
					case TT.LBrace:
					case TT.LBrack:
						{
							WhereClausesOpt(ref name);
							#line 1290 "EcsParserGrammar.les"
							LNode args = F.Missing;
							#line default
							// Line 1291: (TT.LBrack TT.RBrack)?
							la0 = LA0;
							if (la0 == TT.LBrack) {
								lb = MatchAny();
								rb = Match((int) TT.RBrack);
								#line 1291 "EcsParserGrammar.les"
								args = ArgList(lb, rb);
								#line default
							}
							#line 1292 "EcsParserGrammar.les"
							LNode initializer;
							#line default
							var body = MethodBodyOrForward(true, out initializer);
							#line 1294 "EcsParserGrammar.les"
							result = (initializer != null ? F.Property(type, name, args, body, initializer, type.Range.StartIndex, initializer.Range.EndIndex) : F.Property(type, name, args, body, null, type.Range.StartIndex, body.Range.EndIndex)).PlusAttrs(attrs);
							#line default
						}
						break;
					default:
						{
							ScanToEndOfStmt();
							Error("Syntax error in method, property, or variable declaration");
							result = F.Missing.PlusAttrs(attrs);
						}
						break;
					}
				}
			} while (false);
			return result;
		}
		LNode OperatorCast(int startIndex, VList<LNode> attrs)
		{
			#line 1306 "EcsParserGrammar.les"
			LNode r;
			#line default
			var op = MatchAny();
			var type = DataType();
			#line 1308 "EcsParserGrammar.les"
			var name = F.Attr(_triviaUseOperatorKeyword, F.Id(S.Cast, op.StartIndex, op.EndIndex));
			#line default
			r = MethodArgListAndBody(startIndex, attrs, S.Fn, type, name);
			#line 1310 "EcsParserGrammar.les"
			return r;
			#line default
		}
		LNode MethodArgListAndBody(int startIndex, VList<LNode> attrs, Symbol kind, LNode type, LNode name)
		{
			TokenType la0;
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			WhereClausesOpt(ref name);
			#line 1316 "EcsParserGrammar.les"
			LNode r, baseCall = null;
			#line default
			// Line 1317: (TT.Colon (@`.`(TT, noMacro(@base))|@`.`(TT, noMacro(@this))) TT.LParen TT.RParen)?
			la0 = LA0;
			if (la0 == TT.Colon) {
				Skip();
				var target = Match((int) TT.@base, (int) TT.@this);
				var baselp = Match((int) TT.LParen);
				var baserp = Match((int) TT.RParen);
				#line 1319 "EcsParserGrammar.les"
				baseCall = F.Call((Symbol) target.Value, ExprListInside(baselp), target.StartIndex, baserp.EndIndex);
				if ((kind != S.Cons)) {
					Error(baseCall, "This is not a constructor declaration, so there should be no ':' clause.");
				}
				#line default
			}
			#line 1326 "EcsParserGrammar.les"
			for (int i = 0; i < attrs.Count; i++) {
				var attr = attrs[i];
				if (IsNamedArg(attr) && attr.Args[0].IsIdNamed(S.Return)) {
					type = type.PlusAttr(attr.Args[1]);
					attrs.RemoveAt(i);
					i--;
				}
			}
			#line default
			// Line 1335: (default TT.Semicolon | MethodBodyOrForward)
			do {
				switch (LA0) {
				case TT.Semicolon:
					goto match1;
				case TT.At:
				case TT.Forward:
				case TT.LambdaArrow:
				case TT.LBrace:
					{
						var body = MethodBodyOrForward();
						#line 1347 "EcsParserGrammar.les"
						if (kind == S.Delegate) {
							Error("A 'delegate' is not expected to have a method body.");
						}
						if (baseCall != null) {
							body = body.WithArgs(body.Args.Insert(0, baseCall)).WithRange(baseCall.Range.StartIndex, body.Range.EndIndex);
						}
						#line 1351 "EcsParserGrammar.les"
						var parts = new VList<LNode> { 
							type, name, ArgList(lp, rp), body
						};
						r = F.Call(kind, parts, startIndex, body.Range.EndIndex);
						#line default
					}
					break;
				default:
					goto match1;
				}
				break;
			match1:
				{
					var end = Match((int) TT.Semicolon);
					#line 1337 "EcsParserGrammar.les"
					if (kind == S.Cons && baseCall != null) {
						Error(baseCall, "A method body is required.");
						var parts = new VList<LNode> { 
							type, name, ArgList(lp, rp), LNode.Call(S.Braces, new VList<LNode>(baseCall), baseCall.Range)
						};
						return F.Call(kind, parts, startIndex, baseCall.Range.EndIndex);
					}
					#line 1343 "EcsParserGrammar.les"
					r = F.Call(kind, type, name, ArgList(lp, rp), startIndex, end.EndIndex);
					#line default
				}
			} while (false);
			#line 1355 "EcsParserGrammar.les"
			return r.PlusAttrs(attrs);
			#line default
		}
		LNode MethodBodyOrForward(bool isProperty, out LNode propInitializer)
		{
			TokenType la0, la1;
			#line 1360 "EcsParserGrammar.les"
			propInitializer = null;
			#line default
			// Line 1361: ( TT.Forward ExprStart TT.Semicolon | TT.LambdaArrow ExprStart TT.Semicolon | TokenLiteral TT.Semicolon | BracedBlock greedy(&{isProperty} TT.Set ExprStart TT.Semicolon)? )
			la0 = LA0;
			if (la0 == TT.Forward) {
				var op = MatchAny();
				var e = ExprStart(true);
				Match((int) TT.Semicolon);
				#line 1361 "EcsParserGrammar.les"
				return F.Call(S.Forward, e, op.StartIndex, e.Range.EndIndex);
				#line default
			} else if (la0 == TT.LambdaArrow) {
				var op = MatchAny();
				var e = ExprStart(false);
				Match((int) TT.Semicolon);
				#line 1362 "EcsParserGrammar.les"
				return e;
				#line default
			} else if (la0 == TT.At) {
				var e = TokenLiteral();
				Match((int) TT.Semicolon);
				#line 1363 "EcsParserGrammar.les"
				return e;
				#line default
			} else {
				var body = BracedBlock(S.Fn);
				// Line 1367: greedy(&{isProperty} TT.Set ExprStart TT.Semicolon)?
				la0 = LA0;
				if (la0 == TT.Set) {
					if (isProperty) {
						la1 = LA(1);
						if (InParens_ExprOrTuple_set0.Contains((int) la1)) {
							Skip();
							propInitializer = ExprStart(false);
							var end = Match((int) TT.Semicolon);
						}
					}
				}
				#line 1368 "EcsParserGrammar.les"
				return body;
				#line default
			}
		}
		LNode NameAndMaybeInit(bool isArray)
		{
			TokenType la0;
			var r = IdAtom();
			// Line 1390: ((TT.QuickBindSet|TT.Set) (&{isArray} &{Down($LI) && Up(HasNoSemicolons())} TT.LBrace TT.RBrace / ExprStart))?
			la0 = LA0;
			if (la0 == TT.QuickBindSet || la0 == TT.Set) {
				Skip();
				// Line 1395: (&{isArray} &{Down($LI) && Up(HasNoSemicolons())} TT.LBrace TT.RBrace / ExprStart)
				do {
					la0 = LA0;
					if (la0 == TT.LBrace) {
						if (isArray) {
							if (Down(0) && Up(HasNoSemicolons())) {
								var lb = MatchAny();
								var rb = Match((int) TT.RBrace);
								#line 1399 "EcsParserGrammar.les"
								var initializers = InitializerListInside(lb).ToVList();
								var expr = F.Call(S.ArrayInit, initializers, lb.StartIndex, rb.EndIndex);
								expr = expr.SetBaseStyle(NodeStyle.OldStyle);
								r = F.Call(S.Assign, r, expr, r.Range.StartIndex, rb.EndIndex);
								#line default
							} else
								goto matchExprStart;
						} else
							goto matchExprStart;
					} else
						goto matchExprStart;
					break;
				matchExprStart:
					{
						var init = ExprStart(false);
						#line 1405 "EcsParserGrammar.les"
						r = F.Call(S.Assign, r, init, r.Range.StartIndex, init.Range.EndIndex);
						#line default
					}
				} while (false);
			}
			#line 1408 "EcsParserGrammar.les"
			return r;
			#line default
		}
		void NoSemicolons()
		{
			TokenType la0;
			// Line 1411: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			Match((int) EOF);
		}
		bool Try_HasNoSemicolons(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return HasNoSemicolons();
		}
		bool HasNoSemicolons()
		{
			TokenType la0;
			// Line 1411: (~(EOF|TT.Semicolon))*
			for (;;) {
				la0 = LA0;
				if (!(la0 == (TokenType) EOF || la0 == TT.Semicolon))
					{if (!TryMatchExcept((int) TT.Semicolon))
						return false;}
				else
					break;
			}
			if (!TryMatch((int) EOF))
				return false;
			return true;
		}
		LNode Constructor(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			#line 1418 "EcsParserGrammar.les"
			LNode r;
			#line 1418 "EcsParserGrammar.les"
			Token n;
			#line default
			// Line 1419: ( &{_spaceName == LT($LI).Value} (TT.ContextualKeyword|TT.Id) &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / &{_spaceName != S.Fn || LA($LI + 3) == TT.LBrace} @`.`(TT, noMacro(@this)) &(TT.LParen TT.RParen (TT.LBrace|TT.Semicolon)) / (@`.`(TT, noMacro(@this))|TT.ContextualKeyword|TT.Id) &(TT.LParen TT.RParen TT.Colon) )
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
					if (_spaceName == LT(0).Value) {
						if (Try_Constructor_Test0(1))
							n = MatchAny();
						else
							goto match3;
					} else
						goto match3;
				} else {
					if (_spaceName != S.Fn || LA(0 + 3) == TT.LBrace) {
						if (Try_Constructor_Test1(1))
							n = Match((int) TT.@this);
						else
							goto match3;
					} else
						goto match3;
				}
				break;
			match3:
				{
					n = Match((int) TT.@this, (int) TT.ContextualKeyword, (int) TT.Id);
					Check(Try_Constructor_Test2(0), "TT.LParen TT.RParen TT.Colon");
				}
			} while (false);
			#line 1428 "EcsParserGrammar.les"
			LNode name = F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex);
			#line default
			r = MethodArgListAndBody(startIndex, attrs, S.Cons, F.Missing, name);
			#line 1430 "EcsParserGrammar.les"
			return r;
			#line default
		}
		LNode Destructor(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			#line 1434 "EcsParserGrammar.les"
			LNode r;
			#line 1434 "EcsParserGrammar.les"
			Token n;
			#line default
			var tilde = MatchAny();
			// Line 1436: ((&{LT($LI).Value == _spaceName}) (TT.ContextualKeyword|TT.Id) | @`.`(TT, noMacro(@this)))
			la0 = LA0;
			if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
				// Line 1436: (&{LT($LI).Value == _spaceName})
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id)
					Check(LT(0).Value == _spaceName, "LT($LI).Value == _spaceName");
				else {
					#line 1437 "EcsParserGrammar.les"
					Error("Unexpected destructor '{0}'", LT(0).Value);
					#line default
				}
				n = MatchAny();
			} else
				n = Match((int) TT.@this);
			#line 1441 "EcsParserGrammar.les"
			LNode name = F.Call(S.NotBits, F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex), tilde.StartIndex, n.EndIndex);
			#line default
			r = MethodArgListAndBody(startIndex, attrs, S.Fn, F.Missing, name);
			#line 1443 "EcsParserGrammar.les"
			return r;
			#line default
		}
		LNode DelegateDecl(int startIndex, VList<LNode> attrs)
		{
			Skip();
			var type = DataType();
			var name = ComplexNameDecl();
			var r = MethodArgListAndBody(startIndex, attrs, S.Delegate, type, name);
			#line 1453 "EcsParserGrammar.les"
			return r.WithAttrs(attrs);
			#line default
		}
		LNode EventDecl(int startIndex, VList<LNode> attrs)
		{
			TokenType la0;
			#line 1457 "EcsParserGrammar.les"
			LNode r;
			#line default
			Skip();
			var type = DataType();
			var name = ComplexNameDecl();
			// Line 1459: (default (TT.Comma ComplexNameDecl)* TT.Semicolon | BracedBlock)
			do {
				la0 = LA0;
				if (la0 == TT.Comma || la0 == TT.Semicolon)
					goto match1;
				else if (la0 == TT.LBrace) {
					var body = BracedBlock(S.Fn);
					#line 1465 "EcsParserGrammar.les"
					r = F.Call(S.Event, type, name, body, startIndex, body.Range.EndIndex);
					#line default
				} else
					goto match1;
				break;
			match1:
				{
					#line 1460 "EcsParserGrammar.les"
					var parts = new VList<LNode>(type, name);
					#line default
					// Line 1461: (TT.Comma ComplexNameDecl)*
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							parts.Add(ComplexNameDecl());
						} else
							break;
					}
					var end = Match((int) TT.Semicolon);
					#line 1463 "EcsParserGrammar.les"
					r = F.Call(S.Event, parts, startIndex, end.EndIndex);
					#line default
				}
			} while (false);
			#line 1467 "EcsParserGrammar.les"
			return r.WithAttrs(attrs);
			#line default
		}
		LNode LabelStmt(int startIndex)
		{
			var id = Match((int) TT.@default, (int) TT.ContextualKeyword, (int) TT.Id);
			var end = Match((int) TT.Colon);
			#line 1478 "EcsParserGrammar.les"
			return F.Call(S.Label, F.Id(id), startIndex, end.EndIndex);
			#line default
		}
		LNode CaseStmt(int startIndex)
		{
			TokenType la0;
			#line 1482 "EcsParserGrammar.les"
			var cases = VList<LNode>.Empty;
			#line default
			var kw = Match((int) TT.@case);
			cases.Add(ExprStart2(true));
			// Line 1484: (TT.Comma ExprStart2)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					cases.Add(ExprStart2(true));
				} else
					break;
			}
			var end = Match((int) TT.Colon);
			#line 1485 "EcsParserGrammar.les"
			return F.Call(S.Case, cases, startIndex, end.EndIndex);
			#line default
		}
		LNode BlockCallStmt()
		{
			TokenType la0;
			var id = MatchAny();
			Check(Try_BlockCallStmt_Test0(0), "( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )");
			var args = new WList<LNode>();
			LNode block;
			// Line 1503: ( TT.LParen TT.RParen (BracedBlock | TT.Id => Stmt) | TT.Forward ExprStart TT.Semicolon | BracedBlock )
			la0 = LA0;
			if (la0 == TT.LParen) {
				var lp = MatchAny();
				var rp = Match((int) TT.RParen);
				#line 1503 "EcsParserGrammar.les"
				AppendExprsInside(lp, args, false, true);
				#line default
				// Line 1504: (BracedBlock | TT.Id => Stmt)
				la0 = LA0;
				if (la0 == TT.LBrace)
					block = BracedBlock();
				else {
					block = Stmt();
					#line 1507 "EcsParserGrammar.les"
					if ((ColumnOf(block.Range.StartIndex) <= ColumnOf(id.StartIndex) || !char.IsLower(id.Value.ToString().FirstOrDefault()))) {
						ErrorSink.Write(_Warning, block, "Probable missing semicolon before this statement.");
					}
					#line default
				}
			} else if (la0 == TT.Forward) {
				var fwd = MatchAny();
				var e = ExprStart(true);
				Match((int) TT.Semicolon);
				#line 1514 "EcsParserGrammar.les"
				block = SetOperatorStyle(F.Call(S.Forward, e, fwd.StartIndex, e.Range.EndIndex));
				#line default
			} else
				block = BracedBlock();
			#line 1518 "EcsParserGrammar.les"
			args.Add(block);
			var result = F.Call((Symbol) id.Value, args.ToVList(), id.StartIndex, block.Range.EndIndex);
			if (block.Calls(S.Forward, 1)) {
				result = F.Attr(_triviaForwardedProperty, result);
			}
			#line 1523 "EcsParserGrammar.les"
			return result.SetBaseStyle(NodeStyle.Special);
			#line default
		}
		LNode ReturnBreakContinueThrow(int startIndex)
		{
			var kw = MatchAny();
			var e = ExprOrNull(false);
			var end = Match((int) TT.Semicolon);
			#line 1537 "EcsParserGrammar.les"
			if (e != null)
				 return F.Call((Symbol) kw.Value, e, startIndex, end.EndIndex);
			else
				 return F.Call((Symbol) kw.Value, startIndex, end.EndIndex);
			#line default
		}
		LNode GotoStmt(int startIndex)
		{
			Skip();
			var e = ExprOrNull(false);
			var end = Match((int) TT.Semicolon);
			#line 1547 "EcsParserGrammar.les"
			if (e != null)
				 return F.Call(S.Goto, e, startIndex, end.EndIndex);
			else
				 return F.Call(S.Goto, startIndex, end.EndIndex);
			#line default
		}
		LNode GotoCaseStmt(int startIndex)
		{
			TokenType la0, la1;
			#line 1553 "EcsParserGrammar.les"
			LNode e = null;
			#line default
			Skip();
			Skip();
			// Line 1555: (@`.`(TT, noMacro(@default)) | ExprOpt)
			la0 = LA0;
			if (la0 == TT.@default) {
				la1 = LA(1);
				if (la1 == TT.Semicolon) {
					var @def = MatchAny();
					#line 1556 "EcsParserGrammar.les"
					e = F.Id(S.Default, @def.StartIndex, @def.EndIndex);
					#line default
				} else
					e = ExprOpt(false);
			} else
				e = ExprOpt(false);
			var end = Match((int) TT.Semicolon);
			#line 1559 "EcsParserGrammar.les"
			return F.Call(S.GotoCase, e, startIndex, end.EndIndex);
			#line default
		}
		LNode CheckedOrUncheckedStmt(int startIndex)
		{
			var kw = MatchAny();
			var bb = BracedBlock();
			#line 1567 "EcsParserGrammar.les"
			return F.Call((Symbol) kw.Value, bb, startIndex, bb.Range.EndIndex);
			#line default
		}
		LNode DoStmt(int startIndex)
		{
			Skip();
			var block = Stmt();
			Match((int) TT.@while);
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var end = Match((int) TT.Semicolon);
			#line 1575 "EcsParserGrammar.les"
			var parts = new WList<LNode> { 
				block
			};
			SingleExprInside(p, "while (...)", parts);
			return F.Call(S.DoWhile, parts.ToVList(), startIndex, end.EndIndex);
			#line default
		}
		LNode WhileStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1584 "EcsParserGrammar.les"
			var cond = SingleExprInside(p, "while (...)");
			return F.Call(S.While, cond, block, startIndex, block.Range.EndIndex);
			#line default
		}
		LNode ForStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1604 "EcsParserGrammar.les"
			Down(p);
			#line default
			var init = ExprOpt(true);
			Match((int) TT.Semicolon);
			var cond = ExprOpt(false);
			Match((int) TT.Semicolon);
			var inc = ExprOpt(false);
			#line 1606 "EcsParserGrammar.les"
			Up();
			#line default
			#line 1608 "EcsParserGrammar.les"
			var parts = new VList<LNode> { 
				init, cond, inc, block
			};
			return F.Call(S.For, parts, startIndex, block.Range.EndIndex);
			#line default
		}
		LNode ForEachStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1616 "EcsParserGrammar.les"
			Down(p);
			#line default
			var @var = VarIn();
			var list = ExprStart(false);
			#line 1619 "EcsParserGrammar.les"
			Up();
			#line default
			#line 1620 "EcsParserGrammar.les"
			return F.Call(S.ForEach, @var, list, block, startIndex, block.Range.EndIndex);
			#line default
		}
		static readonly HashSet<int> VarIn_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@in, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.Literal, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.QuickBindSet, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword, (int) TT.XorBits);
		LNode VarIn()
		{
			TokenType la1;
			#line 1624 "EcsParserGrammar.les"
			LNode @var;
			#line default
			// Line 1625: (&(Atom TT.@in) Atom / VarDeclStart)
			do {
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (Try_VarIn_Test0(0)) {
							la1 = LA(1);
							if (VarIn_set0.Contains((int) la1))
								goto matchAtom;
							else
								goto matchVarDeclStart;
						} else
							goto matchVarDeclStart;
					}
				default:
					goto matchAtom;
				}
			matchAtom:
				{
					Check(Try_VarIn_Test0(0), "Atom TT.@in");
					@var = Atom();
				}
				break;
			matchVarDeclStart:
				{
					var pair = VarDeclStart();
					#line 1628 "EcsParserGrammar.les"
					@var = F.Call(S.Var, pair.A, pair.B, pair.A.Range.StartIndex, pair.B.Range.EndIndex);
					#line default
				}
			} while (false);
			Match((int) TT.@in);
			#line 1631 "EcsParserGrammar.les"
			return @var;
			#line default
		}
		static readonly HashSet<int> IfStmt_set0 = NewSet((int) TT.@base, (int) TT.@break, (int) TT.@continue, (int) TT.@default, (int) TT.@return, (int) TT.@this, (int) TT.@throw, (int) TT.@case, (int) TT.@checked, (int) TT.@class, (int) TT.@delegate, (int) TT.@do, (int) TT.@enum, (int) TT.@event, (int) TT.@fixed, (int) TT.@for, (int) TT.@foreach, (int) TT.@goto, (int) TT.@if, (int) TT.@interface, (int) TT.@is, (int) TT.@lock, (int) TT.@namespace, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@struct, (int) TT.@switch, (int) TT.@try, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Power, (int) TT.Semicolon, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		LNode IfStmt(int startIndex)
		{
			TokenType la0, la1;
			#line 1637 "EcsParserGrammar.les"
			LNode @else = null;
			#line default
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var then = Stmt();
			// Line 1639: greedy(TT.@else Stmt)?
			la0 = LA0;
			if (la0 == TT.@else) {
				la1 = LA(1);
				if (IfStmt_set0.Contains((int) la1)) {
					Skip();
					@else = Stmt();
				}
			}
			#line 1641 "EcsParserGrammar.les"
			var cond = SingleExprInside(p, "if (...)");
			if (@else == null)
				 return F.Call(S.If, cond, then, startIndex, then.Range.EndIndex);
			else
				 return F.Call(S.If, cond, then, @else, startIndex, then.Range.EndIndex);
			#line default
		}
		LNode SwitchStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1650 "EcsParserGrammar.les"
			var expr = SingleExprInside(p, "switch (...)");
			return F.Call(S.Switch, expr, block, startIndex, block.Range.EndIndex);
			#line default
		}
		LNode UsingStmt(int startIndex)
		{
			Skip();
			var p = MatchAny();
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1660 "EcsParserGrammar.les"
			var expr = SingleExprInside(p, "using (...)");
			return F.Call(S.UsingStmt, expr, block, startIndex, block.Range.EndIndex);
			#line default
		}
		LNode LockStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1668 "EcsParserGrammar.les"
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(S.Lock, expr, block, startIndex, block.Range.EndIndex);
			#line default
		}
		LNode FixedStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			#line 1676 "EcsParserGrammar.les"
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(S.Fixed, expr, block, startIndex, block.Range.EndIndex);
			#line default
		}
		LNode TryStmt(int startIndex)
		{
			TokenType la0, la1;
			LNode handler = default(LNode);
			Skip();
			var header = Stmt();
			#line 1685 "EcsParserGrammar.les"
			var parts = new VList<LNode> { 
				header
			};
			LNode varExpr;
			#line 1686 "EcsParserGrammar.les"
			LNode whenExpr;
			#line default
			// Line 1688: greedy(TT.@catch (TT.LParen TT.RParen / ) (&{Is($LI, _when)} TT.ContextualKeyword TT.LParen TT.RParen / ) Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@catch) {
					la1 = LA(1);
					if (IfStmt_set0.Contains((int) la1)) {
						var kw = MatchAny();
						// Line 1689: (TT.LParen TT.RParen / )
						la0 = LA0;
						if (la0 == TT.LParen) {
							var p = MatchAny();
							Match((int) TT.RParen);
							#line 1689 "EcsParserGrammar.les"
							varExpr = SingleExprInside(p, "catch (...)", null, true);
							#line default
						} else {
							#line 1690 "EcsParserGrammar.les"
							varExpr = MissingHere();
							#line default
						}
						// Line 1691: (&{Is($LI, _when)} TT.ContextualKeyword TT.LParen TT.RParen / )
						do {
							la0 = LA0;
							if (la0 == TT.ContextualKeyword) {
								if (Is(0, _when)) {
									if (!(_insideLinqExpr && LinqKeywords.Contains(LT(0).Value))) {
										la1 = LA(1);
										if (la1 == TT.LParen)
											goto match1;
										else
											goto match2;
									} else if (Is(0, _trait) || Is(0, _alias)) {
										la1 = LA(1);
										if (la1 == TT.LParen)
											goto match1;
										else
											goto match2;
									} else {
										la1 = LA(1);
										if (la1 == TT.LParen)
											goto match1;
										else
											goto match2;
									}
								} else
									goto match2;
							} else
								goto match2;
						match1:
							{
								Skip();
								var c = MatchAny();
								Match((int) TT.RParen);
								#line 1692 "EcsParserGrammar.les"
								whenExpr = SingleExprInside(c, "when (...)");
								#line default
							}
							break;
						match2:
							{
								#line 1693 "EcsParserGrammar.les"
								whenExpr = MissingHere();
								#line default
							}
						} while (false);
						handler = Stmt();
						#line 1695 "EcsParserGrammar.les"
						parts.Add(F.Call(S.Catch, varExpr, whenExpr, handler, kw.StartIndex, handler.Range.EndIndex));
						#line default
					} else
						break;
				} else
					break;
			}
			// Line 1698: greedy(TT.@finally Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@finally) {
					la1 = LA(1);
					if (IfStmt_set0.Contains((int) la1)) {
						var kw = MatchAny();
						handler = Stmt();
						#line 1699 "EcsParserGrammar.les"
						parts.Add(F.Call(S.Finally, handler, kw.StartIndex, handler.Range.EndIndex));
						#line default
					} else
						break;
				} else
					break;
			}
			#line 1702 "EcsParserGrammar.les"
			var result = F.Call(S.Try, parts, startIndex, parts.Last.Range.EndIndex);
			if (parts.Count == 1) {
				Error(result, "'try': At least one 'catch' or 'finally' clause is required");
			}
			#line 1706 "EcsParserGrammar.les"
			return result;
			#line default
		}
		LNode ExprOrNull(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			// Line 1715: (ExprStart | )
			la0 = LA0;
			if (InParens_ExprOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(allowUnassignedVarDecl);
				#line 1715 "EcsParserGrammar.les"
				return e;
				#line default
			} else {
				#line 1716 "EcsParserGrammar.les"
				return null;
				#line default
			}
		}
		LNode ExprOpt(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			// Line 1719: (ExprStart | )
			la0 = LA0;
			if (InParens_ExprOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(allowUnassignedVarDecl);
				#line 1719 "EcsParserGrammar.les"
				return e;
				#line default
			} else {
				#line 1720 "EcsParserGrammar.les"
				return MissingHere();
				#line default
			}
		}
		static readonly HashSet<int> ExprList_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@is, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Comma, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.DotDot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Power, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		void ExprList(WList<LNode> list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			TokenType la0, la1;
			// Line 1729: nongreedy(ExprOpt (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt(allowUnassignedVarDecl));
				// Line 1730: (TT.Comma &{allowTrailingComma} EOF / TT.Comma ExprOpt)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (la1 == EOF) {
							if (allowTrailingComma) {
								Skip();
								Skip();
							} else
								goto match2;
						} else if (ExprList_set0.Contains((int) la1))
							goto match2;
						else
							goto error;
					} else if (la0 == EOF)
						break;
					else
						goto error;
					continue;
				match2:
					{
						Skip();
						list.Add(ExprOpt(allowUnassignedVarDecl));
					}
					continue;
				error:
					{
						#line 1732 "EcsParserGrammar.les"
						Error("Syntax error in expression list");
						#line default
						// Line 1732: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
			Skip();
		}
		void ArgList(WList<LNode> list)
		{
			TokenType la0;
			// Line 1738: nongreedy(ExprOpt (TT.Comma ExprOpt)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt(true));
				// Line 1739: (TT.Comma ExprOpt)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						list.Add(ExprOpt(true));
					} else if (la0 == EOF)
						break;
					else {
						#line 1740 "EcsParserGrammar.les"
						Error("Syntax error in argument list");
						#line default
						// Line 1740: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
			Skip();
		}
		LNode InitializerExpr()
		{
			TokenType la0, la2;
			LNode result = default(LNode);
			// Line 1747: ( TT.LBrace TT.RBrace / TT.LBrack TT.RBrack TT.Set ExprStart / ExprOpt )
			do {
				la0 = LA0;
				if (la0 == TT.LBrace) {
					la2 = LA(2);
					if (la2 == (TokenType) EOF || la2 == TT.Comma) {
						var lb = MatchAny();
						var rb = Match((int) TT.RBrace);
						#line 1749 "EcsParserGrammar.les"
						var exprs = InitializerListInside(lb).ToVList();
						result = F.Call(S.Braces, exprs, lb.StartIndex, rb.EndIndex).SetBaseStyle(NodeStyle.OldStyle);
						#line default
					} else
						result = ExprOpt(false);
				} else if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la2 = LA(2);
						if (la2 == TT.Set)
							goto match2;
						else
							result = ExprOpt(false);
					} else
						goto match2;
				} else
					result = ExprOpt(false);
				break;
			match2:
				{
					var lb = MatchAny();
					var rb = Match((int) TT.RBrack);
					Match((int) TT.Set);
					var e = ExprStart(false);
					#line 1753 "EcsParserGrammar.les"
					result = F.Call(S.InitializerAssignment, ExprListInside(lb).ToVList().Add(e), lb.StartIndex, e.Range.EndIndex);
					#line default
				}
			} while (false);
			return result;
		}
		void InitializerList(WList<LNode> list)
		{
			TokenType la0, la1;
			// Line 1760: nongreedy(InitializerExpr (TT.Comma EOF / TT.Comma InitializerExpr)*)?
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(InitializerExpr());
				// Line 1761: (TT.Comma EOF / TT.Comma InitializerExpr)*
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						la1 = LA(1);
						if (la1 == EOF) {
							Skip();
							Skip();
						} else if (ExprList_set0.Contains((int) la1)) {
							Skip();
							list.Add(InitializerExpr());
						} else
							goto error;
					} else if (la0 == EOF)
						break;
					else
						goto error;
					continue;
				error:
					{
						#line 1763 "EcsParserGrammar.les"
						Error("Syntax error in initializer list");
						#line default
						// Line 1763: (~(EOF|TT.Comma))*
						for (;;) {
							la0 = LA0;
							if (!(la0 == (TokenType) EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
			Skip();
		}
		void StmtList(WList<LNode> list)
		{
			TokenType la0;
			// Line 1768: (~(EOF) => Stmt)*
			for (;;) {
				la0 = LA0;
				if (la0 != (TokenType) EOF)
					list.Add(Stmt());
				else
					break;
			}
			Skip();
		}
		static readonly HashSet<int> TypeSuffixOpt_Test0_set0 = NewSet((int) TT.@new, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.Literal, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Sub, (int) TT.Substitute, (int) TT.TypeKeyword);
		private bool Try_TypeSuffixOpt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return TypeSuffixOpt_Test0();
		}
		private bool TypeSuffixOpt_Test0()
		{
			// Line 237: ((TT.@new|TT.Add|TT.AndBits|TT.At|TT.Forward|TT.Id|TT.IncDec|TT.LBrace|TT.Literal|TT.LParen|TT.Mul|TT.Not|TT.NotBits|TT.Sub|TT.Substitute|TT.TypeKeyword) | UnusualId)
			switch (LA0) {
			case TT.@new:
			case TT.Add:
			case TT.AndBits:
			case TT.At:
			case TT.Forward:
			case TT.Id:
			case TT.IncDec:
			case TT.LBrace:
			case TT.Literal:
			case TT.LParen:
			case TT.Mul:
			case TT.Not:
			case TT.NotBits:
			case TT.Sub:
			case TT.Substitute:
			case TT.TypeKeyword:
				if (!TryMatch(TypeSuffixOpt_Test0_set0))
					return false;
				break;
			default:
				if (!Scan_UnusualId())
					return false;
				break;
			}
			return true;
		}
		private bool Try_ExprInParensAuto_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return ExprInParensAuto_Test0();
		}
		private bool ExprInParensAuto_Test0()
		{
			if (!Scan_ExprInParens(true))
				return false;
			if (!TryMatch((int) TT.LambdaArrow, (int) TT.Set))
				return false;
			return true;
		}
		static readonly HashSet<int> FinishPrimaryExpr_Test0_set0 = NewSet((int) TT.ContextualKeyword, (int) TT.Id);
		private bool Try_FinishPrimaryExpr_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return FinishPrimaryExpr_Test0();
		}
		private bool FinishPrimaryExpr_Test0()
		{
			if (!Scan_TParams())
				return false;
			if (!TryMatchExcept(FinishPrimaryExpr_Test0_set0))
				return false;
			return true;
		}
		private bool Try_PrefixExpr_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return PrefixExpr_Test0();
		}
		private bool PrefixExpr_Test0()
		{
			// Line 595: ((TT.Add|TT.BQString|TT.Dot|TT.Sub) | TT.IncDec TT.LParen)
			switch (LA0) {
			case TT.Add:
			case TT.BQString:
			case TT.Dot:
			case TT.Sub:
				if (!TryMatch((int) TT.Add, (int) TT.BQString, (int) TT.Dot, (int) TT.Sub))
					return false;
				break;
			default:
				{
					if (!TryMatch((int) TT.IncDec))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
				}
				break;
			}
			return true;
		}
		static readonly HashSet<int> WordAttributes_Test0_set0 = NewSet((int) TT.@break, (int) TT.@continue, (int) TT.@return, (int) TT.@throw, (int) TT.@case, (int) TT.@class, (int) TT.@delegate, (int) TT.@do, (int) TT.@enum, (int) TT.@event, (int) TT.@fixed, (int) TT.@for, (int) TT.@foreach, (int) TT.@goto, (int) TT.@interface, (int) TT.@lock, (int) TT.@namespace, (int) TT.@struct, (int) TT.@switch, (int) TT.@try, (int) TT.@while);
		private bool Try_WordAttributes_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return WordAttributes_Test0();
		}
		private bool WordAttributes_Test0()
		{
			TokenType la0;
			// Line 943: ( DataType ((TT.AttrKeyword|TT.Id|TT.TypeKeyword) | UnusualId) | @`.`(TT, noMacro(@this)) TT.LParen | (TT.@new|TT.AttrKeyword) | TT.@checked TT.LBrace TT.RBrace | TT.@unchecked TT.LBrace TT.RBrace | @`.`(TT, noMacro(@default)) TT.Colon | TT.@using TT.LParen | (@`.`(TT, noMacro(@break))|@`.`(TT, noMacro(@continue))|@`.`(TT, noMacro(@return))|@`.`(TT, noMacro(@throw))|TT.@case|TT.@class|TT.@delegate|TT.@do|TT.@enum|TT.@event|TT.@fixed|TT.@for|TT.@foreach|TT.@goto|TT.@interface|TT.@lock|TT.@namespace|TT.@struct|TT.@switch|TT.@try|TT.@while) )
			switch (LA0) {
			case TT.@operator:
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.Substitute:
			case TT.TypeKeyword:
				{
					if (!Scan_DataType())
						return false;
					// Line 943: ((TT.AttrKeyword|TT.Id|TT.TypeKeyword) | UnusualId)
					la0 = LA0;
					if (la0 == TT.AttrKeyword || la0 == TT.Id || la0 == TT.TypeKeyword)
						{if (!TryMatch((int) TT.AttrKeyword, (int) TT.Id, (int) TT.TypeKeyword))
							return false;}
					else if (!Scan_UnusualId())
						return false;
				}
				break;
			case TT.@this:
				{
					if (!TryMatch((int) TT.@this))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
				}
				break;
			case TT.@new:
			case TT.AttrKeyword:
				if (!TryMatch((int) TT.@new, (int) TT.AttrKeyword))
					return false;
				break;
			case TT.@checked:
				{
					if (!TryMatch((int) TT.@checked))
						return false;
					if (!TryMatch((int) TT.LBrace))
						return false;
					if (!TryMatch((int) TT.RBrace))
						return false;
				}
				break;
			case TT.@unchecked:
				{
					if (!TryMatch((int) TT.@unchecked))
						return false;
					if (!TryMatch((int) TT.LBrace))
						return false;
					if (!TryMatch((int) TT.RBrace))
						return false;
				}
				break;
			case TT.@default:
				{
					if (!TryMatch((int) TT.@default))
						return false;
					if (!TryMatch((int) TT.Colon))
						return false;
				}
				break;
			case TT.@using:
				{
					if (!TryMatch((int) TT.@using))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
				}
				break;
			default:
				if (!TryMatch(WordAttributes_Test0_set0))
					return false;
				break;
			}
			return true;
		}
		static readonly HashSet<int> Stmt_Test0_set0 = NewSet((int) TT.At, (int) TT.Comma, (int) TT.Forward, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LParen, (int) TT.QuickBindSet, (int) TT.Semicolon, (int) TT.Set);
		private bool Try_Stmt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Stmt_Test0();
		}
		private bool Stmt_Test0()
		{
			if (!Scan_DataType())
				return false;
			// Line 1035: (ComplexNameDecl (TT.At|TT.Comma|TT.Forward|TT.LambdaArrow|TT.LBrace|TT.LParen|TT.QuickBindSet|TT.Semicolon|TT.Set) | ComplexThisDecl TT.LBrack)
			switch (LA0) {
			case TT.@operator:
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.Substitute:
			case TT.TypeKeyword:
				{
					if (!Scan_ComplexNameDecl())
						return false;
					if (!TryMatch(Stmt_Test0_set0))
						return false;
				}
				break;
			default:
				{
					if (!Scan_ComplexThisDecl())
						return false;
					if (!TryMatch((int) TT.LBrack))
						return false;
				}
				break;
			}
			return true;
		}
		private bool Try_MethodOrPropertyOrVar_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return MethodOrPropertyOrVar_Test0();
		}
		private bool MethodOrPropertyOrVar_Test0()
		{
			if (!Scan_IdAtom())
				return false;
			if (!TryMatch((int) TT.Comma, (int) TT.Semicolon, (int) TT.Set))
				return false;
			return true;
		}
		private bool Try_Constructor_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Constructor_Test0();
		}
		private bool Constructor_Test0()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.LBrace, (int) TT.Semicolon))
				return false;
			return true;
		}
		private bool Try_Constructor_Test1(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Constructor_Test1();
		}
		private bool Constructor_Test1()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.LBrace, (int) TT.Semicolon))
				return false;
			return true;
		}
		private bool Try_Constructor_Test2(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Constructor_Test2();
		}
		private bool Constructor_Test2()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
		private bool Try_BlockCallStmt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return BlockCallStmt_Test0();
		}
		private bool BlockCallStmt_Test0()
		{
			TokenType la0;
			// Line 1500: ( TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward )
			la0 = LA0;
			if (la0 == TT.LParen) {
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
				// Line 1500: (TT.LBrace TT.RBrace | TT.Id)
				la0 = LA0;
				if (la0 == TT.LBrace) {
					if (!TryMatch((int) TT.LBrace))
						return false;
					if (!TryMatch((int) TT.RBrace))
						return false;
				} else if (!TryMatch((int) TT.Id))
					return false;
			} else if (la0 == TT.LBrace) {
				if (!TryMatch((int) TT.LBrace))
					return false;
				if (!TryMatch((int) TT.RBrace))
					return false;
			} else if (!TryMatch((int) TT.Forward))
				return false;
			return true;
		}
		private bool Try_VarIn_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return VarIn_Test0();
		}
		private bool VarIn_Test0()
		{
			if (!Scan_Atom())
				return false;
			if (!TryMatch((int) TT.@in))
				return false;
			return true;
		}
	}
}
