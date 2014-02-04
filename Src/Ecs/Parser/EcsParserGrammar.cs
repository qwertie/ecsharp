// Generated from EcsParserGrammar.les by LLLPG custom tool. LLLPG version: 0.9.3.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --no-out-header       Suppress this message
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
namespace Ecs.Parser
{
	using TT = TokenType;
	using S = CodeSymbols;
	using EP = EcsPrecedence;
	#pragma warning disable 162
	partial class EcsParser
	{
		static readonly Symbol _trait = GSymbol.Get("trait");
		static readonly Symbol _alias = GSymbol.Get("alias");
		static readonly Symbol _where = GSymbol.Get("where");
		static readonly Symbol _assembly = GSymbol.Get("assembly");
		static readonly Symbol _module = GSymbol.Get("module");
		static readonly Symbol _from = GSymbol.Get("from");
		static readonly Symbol _await = GSymbol.Get("await");
		Symbol _spaceName;
		LNode DataType(bool afterAsOrIs = false)
		{
			Token? brack;
			var type = DataType(afterAsOrIs, out brack);
			if ((brack != null))
				Error("A type name cannot include [array dimensions]. The square brackets should be empty.");
			return type;
		}
		LNode IdNode(Token t)
		{
			return F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
		}
		LNode AutoRemoveParens(LNode node)
		{
			int i = node.Attrs.IndexWithName(S.TriviaInParens);
			if ((i > -1))
				 return node.WithAttrs(node.Attrs.RemoveAt(i));
			return node;
		}
		int count;
		public static readonly Precedence ContinueExpr = new Precedence(-100, -100, -100);
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
			return SetBaseStyle(node, NodeStyle.Operator);
		}
		LNode SetBaseStyle(LNode node, NodeStyle s)
		{
			node.BaseStyle = s;
			return node;
		}
		LNode SetAlternateStyle(LNode node)
		{
			node.Style |= NodeStyle.Alternate;
			return node;
		}
		void NonKeywordAttrError(IList<LNode> attrs, string stmtType)
		{
			var attr = attrs.FirstOrDefault(a => a.AttrNamed(S.TriviaWordAttribute) != null);
			if ((attr != null))
				Error(attr, "'{0}' appears to be a word attribute, which is not permitted before '{1}'", attr.Range.SourceText, stmtType);
		}
		static readonly Symbol _var = GSymbol.Get("var");
		static readonly Symbol _dynamic = GSymbol.Get("dynamic");
		private void MaybeRecognizeVarAsKeyword(ref LNode type)
		{
			SourceRange rng;
			Symbol name = type.Name;
			if ((name == _var || name == _dynamic) && type.IsId && (rng = type.Range).Source.Text.TryGet(rng.StartIndex, '\0') != '@')
				type = type.WithName(name == _var ? S.Missing : S.Dynamic);
		}
		bool IsNamedArg(LNode node)
		{
			return node.Calls(S.NamedArg, 2) && node.BaseStyle == NodeStyle.Operator;
		}
		RWList<LNode> _stmtAttrs = new RWList<LNode>();
		LNode CoreName(LNode complexId)
		{
			if (complexId.IsId)
				 return complexId;
			if (complexId.CallsMin(S.Of, 1))
				 return CoreName(complexId.Args[0]);
			if (complexId.CallsMin(S.Dot, 1))
				 return complexId.Args.Last;
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
		bool IsArrayType(LNode type)
		{
			return type.Calls(S.Of, 2) && S.IsArrayKeyword(type.Args[0].Name);
		}
		LNode ArgTuple(Token lp, Token rp)
		{
			var args = AppendExprsInside(lp, new RWList<LNode>(), false, true);
			return F.Tuple(args.ToRVList(), lp.StartIndex, rp.EndIndex);
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
		LNode DataType(bool afterAsOrIs, out Token? majorDimension)
		{
			var e = ComplexId();
			TypeSuffixOpt(afterAsOrIs, out majorDimension, ref e);
			return e;
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
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						Skip();
						var e2 = IdAtom();
						e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
					}
					break;
				}
			}
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.GT:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
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
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.ContextualKeyword:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						{
							Skip();
							var rhs = IdAtom();
							e = F.Dot(e, rhs);
							la0 = LA0;
							if (la0 == TT.LT) {
								switch (LA(1)) {
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.GT:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
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
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		 stop:;
			return e;
		}
		bool Scan_ComplexId()
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!TryMatch((int) TT.ColonColon))
							return false;
						if (!Scan_IdAtom())
							return false;
					}
					break;
				}
			}
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.ContextualKeyword:
					case TT.GT:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						goto match1;
				} else if (la0 == TT.Not) {
					la1 = LA(1);
					if (la1 == TT.LParen)
						goto match1;
				}
				break;
			match1:
				{
					if (!Scan_TParams())
						return false;
				}
			} while (false);
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.ContextualKeyword:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						{
							if (!TryMatch((int) TT.Dot))
								return false;
							if (!Scan_IdAtom())
								return false;
							do {
								la0 = LA0;
								if (la0 == TT.LT) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.GT:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										goto match1b;
									}
								} else if (la0 == TT.Dot) {
									la1 = LA(1);
									if (la1 == TT.LBrack)
										goto match1b;
								} else if (la0 == TT.Not) {
									la1 = LA(1);
									if (la1 == TT.LParen)
										goto match1b;
								}
								break;
							match1b:
								{
									if (!Scan_TParams())
										return false;
								}
							} while (false);
						}
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		 stop:;
			return true;
		}
		LNode IdAtom()
		{
			TokenType la0;
			LNode r;
			la0 = LA0;
			if (la0 == TT.Substitute) {
				var t = MatchAny();
				var e = Atom();
				e = AutoRemoveParens(e);
				r = F.Call(S.Substitute, e, t.StartIndex, e.Range.EndIndex);
			} else if (la0 == TT.@operator) {
				var op = MatchAny();
				var t = AnyOperator();
				r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
			} else {
				var t = Match((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.TypeKeyword);
				r = IdNode(t);
			}
			return r;
		}
		bool Scan_IdAtom()
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.Substitute) {
				if (!TryMatch((int) TT.Substitute))
					return false;
				if (!Scan_Atom())
					return false;
			} else if (la0 == TT.@operator) {
				if (!TryMatch((int) TT.@operator))
					return false;
				if (!Scan_AnyOperator())
					return false;
			} else if (!TryMatch((int) TT.ContextualKeyword, (int) TT.Id, (int) TT.TypeKeyword))
				return false;
			return true;
		}
		void TParams(ref LNode r)
		{
			TokenType la0;
			RWList<LNode> list = new RWList<LNode> { 
				r
			};
			Token end;
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						list.Add(DataType());
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
				list = AppendExprsInside(t, list);
			} else {
				Match((int) TT.Not);
				var t = Match((int) TT.LParen);
				end = Match((int) TT.RParen);
				list = AppendExprsInside(t, list);
			}
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list.ToRVList(), start, end.EndIndex);
		}
		bool Try_Scan_TParams(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParams();
		}
		bool Scan_TParams()
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
				switch (LA0) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!Scan_DataType())
							return false;
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
			int count;
			bool result = false;
			dimensionBrack = null;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (!afterAsOrIs || !Try_TypeSuffixOpt_Test0(1) && (count = CountDims(LT(1), true)) > 0) {
						var t = MatchAny();
						if (!afterAsOrIs) {
						} else
							Check(!Try_TypeSuffixOpt_Test0(0), "!((TT.At|TT.Add|TT.LParen|TT.Mul|TT.ContextualKeyword|TT.TypeKeyword|TT.Sub|TT.Substitute|TT.String|TT.SQString|TT.AndBits|TT.Not|TT.NotBits|TT.LBrace|TT.IncDec|TT.Forward|TT.Number|TT.Symbol|TT.@new|TT.Id|TT.OtherLit))");
						e = F.Of(F.Id(S.QuestionMark), e, e.Range.StartIndex, t.EndIndex);
						result = true;
					} else
						break;
				} else if (la0 == TT.Mul) {
					var t = MatchAny();
					e = F.Of(F.Id(S._Pointer), e, e.Range.StartIndex, t.EndIndex);
					result = true;
				} else if (la0 == TT.LBrack) {
					if ((count = CountDims(LT(0), true)) > 0) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var dims = InternalList<Pair<int,int>>.Empty;
							Token rb;
							var lb = MatchAny();
							rb = MatchAny();
							dims.Add(Pair.Create(count, rb.EndIndex));
							for (;;) {
								la0 = LA0;
								if (la0 == TT.LBrack) {
									if ((count = CountDims(LT(0), false)) > 0) {
										la1 = LA(1);
										if (la1 == TT.RBrack) {
											Skip();
											rb = MatchAny();
											dims.Add(Pair.Create(count, rb.EndIndex));
										} else
											break;
									} else
										break;
								} else
									break;
							}
							if (CountDims(lb, false) <= 0)
								dimensionBrack = lb;
							for (int i = dims.Count - 1; i >= 0; i--)
								e = F.Of(F.Id(S.GetArrayKeyword(dims[i].A)), e, e.Range.StartIndex, dims[i].B);
							result = true;
						} else
							break;
					} else
						break;
				} else
					break;
			}
			return result;
		}
		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt, bool afterAsOrIs)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt(afterAsOrIs);
		}
		bool Scan_TypeSuffixOpt(bool afterAsOrIs)
		{
			TokenType la0, la1;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					if (!afterAsOrIs || !Try_TypeSuffixOpt_Test0(1) && (count = CountDims(LT(1), true)) > 0) {
						if (!TryMatch((int) TT.QuestionMark))
							return false;
						if (!afterAsOrIs) {
						} else if (Try_TypeSuffixOpt_Test0(0))
							return false;
					} else
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
			}
			return true;
		}
		LNode ComplexNameDecl()
		{
			TokenType la0, la1;
			var e = IdAtom();
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						Skip();
						var e2 = IdAtom();
						e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
					}
					break;
				}
			}
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
				case TT.ContextualKeyword:
				case TT.GT:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
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
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.ContextualKeyword:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						{
							Skip();
							var rhs = IdAtom();
							e = F.Dot(e, rhs);
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
								case TT.ContextualKeyword:
								case TT.GT:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
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
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		 stop:;
			return e;
		}
		bool Scan_ComplexNameDecl()
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				switch (LA(1)) {
				case TT.@operator:
				case TT.ContextualKeyword:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (!TryMatch((int) TT.ColonColon))
							return false;
						if (!Scan_IdAtom())
							return false;
					}
					break;
				}
			}
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
					case TT.LBrack:
						{
							if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
								goto match1;
						}
						break;
					case TT.@in:
					case TT.@operator:
					case TT.AttrKeyword:
					case TT.ContextualKeyword:
					case TT.GT:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						goto match1;
					}
				} else if (la0 == TT.Dot) {
					la1 = LA(1);
					if (la1 == TT.LBrack)
						goto match1;
				} else if (la0 == TT.Not) {
					la1 = LA(1);
					if (la1 == TT.LParen)
						goto match1;
				}
				break;
			match1:
				{
					if (!Scan_TParamsDecl())
						return false;
				}
			} while (false);
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Dot) {
					switch (LA(1)) {
					case TT.@operator:
					case TT.ContextualKeyword:
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						{
							if (!TryMatch((int) TT.Dot))
								return false;
							if (!Scan_IdAtom())
								return false;
							do {
								la0 = LA0;
								if (la0 == TT.LT) {
									switch (LA(1)) {
									case TT.LBrack:
										{
											if (!(Down(1) && Up(Try_Scan_AsmOrModLabel(0))))
												goto match1b;
										}
										break;
									case TT.@in:
									case TT.@operator:
									case TT.AttrKeyword:
									case TT.ContextualKeyword:
									case TT.GT:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										goto match1b;
									}
								} else if (la0 == TT.Dot) {
									la1 = LA(1);
									if (la1 == TT.LBrack)
										goto match1b;
								} else if (la0 == TT.Not) {
									la1 = LA(1);
									if (la1 == TT.LParen)
										goto match1b;
								}
								break;
							match1b:
								{
									if (!Scan_TParamsDecl())
										return false;
								}
							} while (false);
						}
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		 stop:;
			return true;
		}
		void TParamsDecl(ref LNode r)
		{
			TokenType la0;
			RWList<LNode> list = new RWList<LNode> { 
				r
			};
			Token end;
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
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
				list = AppendExprsInside(t, list);
			} else {
				Match((int) TT.Not);
				var t = Match((int) TT.LParen);
				end = Match((int) TT.RParen);
				list = AppendExprsInside(t, list);
			}
			int start = r.Range.StartIndex;
			r = F.Call(S.Of, list.ToRVList(), start, end.EndIndex);
		}
		bool Try_Scan_TParamsDecl(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TParamsDecl();
		}
		bool Scan_TParamsDecl()
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.LT) {
				if (!TryMatch((int) TT.LT))
					return false;
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
			RWList<LNode> attrs = null;
			int startIndex = GetTextPosition(InputPosition);
			NormalAttributes(ref attrs);
			TParamAttributeKeywords(ref attrs);
			var node = IdAtom();
			if ((attrs != null))
				node = node.WithAttrs(attrs.ToRVList());
			return node;
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
			LNode r;
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
					r = F.Attr(_triviaUseOperatorKeyword, F.Id((Symbol) t.Value, op.StartIndex, t.EndIndex));
				}
				break;
			case TT.@base:
			case TT.@this:
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					r = IdNode(t);
				}
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				{
					var t = MatchAny();
					r = F.Literal(t.Value, t.StartIndex, t.EndIndex);
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
				{
					var at = MatchAny();
					var lb = Match((int) TT.LBrack);
					var rb = Match((int) TT.RBrack);
					r = F.Literal(lb.Children, at.StartIndex, rb.EndIndex);
				}
				break;
			case TT.@checked:
			case TT.@unchecked:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					r = F.Call((Symbol) t.Value, ExprListInside(args), t.StartIndex, rp.EndIndex);
				}
				break;
			case TT.@default:
			case TT.@sizeof:
			case TT.@typeof:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					r = F.Call((Symbol) t.Value, TypeInside(args), t.StartIndex, rp.EndIndex);
				}
				break;
			case TT.@delegate:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					Match((int) TT.RParen);
					var block = Match((int) TT.LBrace);
					var rb = Match((int) TT.RBrace);
					r = F.Call(S.Lambda, F.Tuple(ExprListInside(args).ToRVList()), F.Braces(StmtListInside(block).ToRVList(), block.StartIndex, rb.EndIndex), t.StartIndex, rb.EndIndex);
				}
				break;
			default:
				{
					r = Error("Invalid expression. Expected (parentheses), {braces}, identifier, literal, or $substitution.");
					for (;;) {
						la0 = LA0;
						if (!(la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != EOF)
								Skip();
							else
								break;
						} else
							break;
					}
				}
				break;
			}
			return r;
		}
		static readonly HashSet<int> Scan_Atom_set0 = NewSet((int) TT.@base, (int) TT.@this, (int) TT.ContextualKeyword, (int) TT.Id, (int) TT.TypeKeyword);
		static readonly HashSet<int> Scan_Atom_set1 = NewSet((int) TT.Number, (int) TT.OtherLit, (int) TT.SQString, (int) TT.String, (int) TT.Symbol);
		bool Scan_Atom()
		{
			TokenType la0, la1;
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
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.TypeKeyword:
				if (!TryMatch(Scan_Atom_set0))
					return false;
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				if (!TryMatch(Scan_Atom_set1))
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
				{
					if (!TryMatch((int) TT.At))
						return false;
					if (!TryMatch((int) TT.LBrack))
						return false;
					if (!TryMatch((int) TT.RBrack))
						return false;
				}
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
			default:
				{
					for (;;) {
						la0 = LA0;
						if (!(la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon)) {
							la1 = LA(1);
							if (la1 != EOF)
								{if (!TryMatchExcept((int) EOF, (int) TT.Comma, (int) TT.Semicolon))
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
		static readonly HashSet<int> AnyOperator_set0 = NewSet((int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Set, (int) TT.Sub, (int) TT.Substitute, (int) TT.XorBits);
		Token AnyOperator()
		{
			var op = Match(AnyOperator_set0);
			return op;
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
			Token? majorDimension = null;
			int endIndex;
			var list = new RWList<LNode>();
			var op = Match((int) TT.@new);
			la0 = LA0;
			if (la0 == TT.LBrack) {
				Check((count = CountDims(LT(0), false)) > 0, "(count = CountDims(LT($LI), @false)) > 0");
				var lb = MatchAny();
				var rb = Match((int) TT.RBrack);
				var type = F.Id(S.GetArrayKeyword(count), lb.StartIndex, rb.EndIndex);
				lb = Match((int) TT.LBrace);
				rb = Match((int) TT.RBrace);
				list.Add(LNode.Call(type, type.Range));
				AppendInitializersInside(lb, list);
				endIndex = rb.EndIndex;
			} else if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				list.Add(F._Missing);
				AppendInitializersInside(lb, list);
				endIndex = rb.EndIndex;
			} else {
				var type = DataType(false, out majorDimension);
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							var lp = MatchAny();
							var rp = MatchAny();
							if ((majorDimension != null))
								Error("Syntax error: unexpected constructor argument list (...)");
							list.Add(F.Call(type, ExprListInside(lp).ToRVList(), type.Range.StartIndex, rp.EndIndex));
							endIndex = rp.EndIndex;
							la0 = LA0;
							if (la0 == TT.LBrace) {
								la1 = LA(1);
								if (la1 == TT.RBrace) {
									var lb = MatchAny();
									var rb = MatchAny();
									AppendInitializersInside(lb, list);
									endIndex = rb.EndIndex;
								}
							}
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						Token lb = op, rb = op;
						bool haveBraces = false;
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								lb = MatchAny();
								rb = MatchAny();
								haveBraces = true;
							}
						}
						if ((majorDimension != null))
							list.Add(LNode.Call(type, ExprListInside(majorDimension.Value).ToRVList(), type.Range));
						else
							list.Add(LNode.Call(type, type.Range));
						if ((haveBraces)) {
							AppendInitializersInside(lb, list);
							endIndex = rb.EndIndex;
						} else
							endIndex = type.Range.EndIndex;
						if ((!haveBraces && majorDimension == null)) {
							if (IsArrayType(type))
								Error("Syntax error: missing array size expression");
							else
								Error("Syntax error: expected constructor argument list (...) or initializers {...}");
						}
					}
				} while (false);
			}
			return F.Call(S.New, list.ToRVList(), op.StartIndex, endIndex);
		}
		bool Scan_NewExpr()
		{
			TokenType la0, la1;
			if (!TryMatch((int) TT.@new))
				return false;
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
				do {
					la0 = LA0;
					if (la0 == TT.LParen) {
						la1 = LA(1);
						if (la1 == TT.RParen) {
							if (!TryMatch((int) TT.LParen))
								return false;
							if (!TryMatch((int) TT.RParen))
								return false;
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
			if (Try_ExprInParensAuto_Test0(0)) {
				var r = ExprInParens(true);
				return r;
			} else {
				var r = ExprInParens(false);
				return r;
			}
		}
		bool Scan_ExprInParensAuto()
		{
			if (Try_ExprInParensAuto_Test0(0))
				{if (!Scan_ExprInParens(true))
					return false;}
			else if (!Scan_ExprInParens(false))
				return false;
			return true;
		}
		static readonly HashSet<int> PrimaryExpr_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.At, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LParen, (int) TT.Number, (int) TT.OtherLit, (int) TT.SQString, (int) TT.String, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		LNode PrimaryExpr()
		{
			TokenType la1;
			var e = Atom();
			for (;;) {
				switch (LA0) {
				case TT.Dot:
					{
						if (Try_PrimaryExpr_Test0(0)) {
							la1 = LA(1);
							if (PrimaryExpr_set0.Contains((int) la1))
								goto match1;
							else
								TParams(ref e);
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
							e = F.Call(e, ExprListInside(lp), e.Range.StartIndex, rp.EndIndex);
						}
					}
					break;
				case TT.At:
					{
						Skip();
						var lb = Match((int) TT.LBrace);
						var rb = Match((int) TT.RBrace);
						var stmts = StmtListInside(lb).ToRVList();
						e = SetBaseStyle(F.Call(e, stmts, e.Range.StartIndex, rb.EndIndex), NodeStyle.Statement);
					}
					break;
				case TT.LBrack:
					{
						var lb = MatchAny();
						var rb = Match((int) TT.RBrack);
						var list = new RWList<LNode> { 
							e
						};
						e = F.Call(S.Bracks, AppendExprsInside(lb, list).ToRVList(), e.Range.StartIndex, rb.EndIndex);
					}
					break;
				case TT.IncDec:
					{
						var t = MatchAny();
						e = F.Call(t.Value == S.PreInc ? S.PostInc : S.PostDec, e, e.Range.StartIndex, t.EndIndex);
					}
					break;
				case TT.LT:
					{
						if (Try_PrimaryExpr_Test0(0))
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
						var bb = BracedBlock();
						if ((!e.IsCall || e.BaseStyle == NodeStyle.Operator))
							e = F.Call(e, bb, e.Range.StartIndex, bb.Range.EndIndex);
						else
							e = e.WithArgs(e.Args.Add(bb)).WithRange(e.Range.StartIndex, bb.Range.EndIndex);
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
					e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
				}
			}
		 stop:;
			return e;
		}
		LNode PrimaryExpr_NewStyleCast(LNode e)
		{
			TokenType la0;
			var lp = MatchAny();
			var rp = Match((int) TT.RParen);
			Down(lp);
			Symbol kind;
			RWList<LNode> attrs = null;
			la0 = LA0;
			if (la0 == TT.PtrArrow) {
				Skip();
				kind = S.Cast;
			} else if (la0 == TT.@as) {
				Skip();
				kind = S.As;
			} else {
				Match((int) TT.@using);
				kind = S.UsingCast;
			}
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var type = DataType();
			Match((int) EOF);
			if (attrs != null)
				type = type.PlusAttrs(attrs.ToRVList());
			return Up(SetAlternateStyle(SetOperatorStyle(F.Call(kind, e, type, e.Range.StartIndex, rp.EndIndex))));
		}
		LNode NullDotExpr()
		{
			TokenType la0;
			var e = PrimaryExpr();
			for (;;) {
				la0 = LA0;
				if (la0 == TT.NullDot) {
					Skip();
					var rhs = PrimaryExpr();
					e = F.Call(S.NullDot, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
				} else
					break;
			}
			return e;
		}
		static readonly HashSet<int> PrefixExpr_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		LNode PrefixExpr()
		{
			TokenType la2;
			do {
				switch (LA0) {
				case TT.Add:
				case TT.AndBits:
				case TT.Forward:
				case TT.IncDec:
				case TT.Mul:
				case TT.Not:
				case TT.NotBits:
				case TT.Sub:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						return SetOperatorStyle(F.Call((Symbol) op.Value, e, op.StartIndex, e.Range.EndIndex));
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
									Down(lp);
									return SetOperatorStyle(F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex));
								} else
									goto match4;
							} else
								goto match4;
						} else
							goto match4;
					}
					break;
				case TT.Power:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						return SetOperatorStyle(F.Call(S._Dereference, SetOperatorStyle(F.Call(S._Dereference, e, op.StartIndex + 1, e.Range.EndIndex)), op.StartIndex, e.Range.EndIndex));
					}
					break;
				default:
					goto match4;
				}
				break;
			match4:
				{
					var e = NullDotExpr();
					return e;
				}
			} while (false);
		}
		LNode Expr(Precedence context)
		{
			TokenType la0, la1;
			Debug.Assert(context.CanParse(EP.Prefix));
			Precedence prec;
			var e = PrefixExpr();
			for (;;) {
				switch (LA0) {
				case TT.GT:
				case TT.LT:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
								if (context.CanParse(EP.Shift)) {
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
							} else {
								la1 = LA(1);
								if (PrefixExpr_set0.Contains((int) la1))
									goto match1;
								else
									goto stop;
							}
						} else if (LT(0).EndIndex == LT(0 + 1).StartIndex) {
							if (context.CanParse(EP.Shift)) {
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
								e = SetOperatorStyle(F.Call(S.QuestionMark, e, then, @else, e.Range.StartIndex, @else.Range.EndIndex));
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
					e = SetOperatorStyle(F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
				}
				continue;
			match3:
				{
					la0 = LA0;
					if (la0 == TT.LT) {
						Skip();
						Match((int) TT.LT);
						var rhs = Expr(EP.Shift);
						e = SetOperatorStyle(F.Call(S.Shl, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
					} else {
						Match((int) TT.GT);
						Match((int) TT.GT);
						var rhs = Expr(EP.Shift);
						e = SetOperatorStyle(F.Call(S.Shr, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex));
					}
				}
			}
		 stop:;
			return e;
		}
		public LNode ExprStart(bool allowUnassignedVarDecl)
		{
			TokenType la0, la1;
			LNode e;
			Token argName = default(Token);
			RWList<LNode> attrs = null;
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
					if (Try_Scan_DetectVarDecl(0, allowUnassignedVarDecl)) {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
					} else {
						la1 = LA(1);
						if (la1 == TT.Colon)
							goto match1;
					}
				}
				break;
			match1:
				{
					argName = MatchAny();
					Skip();
				}
			} while (false);
			NormalAttributes(ref attrs);
			AttributeKeywords(ref attrs);
			var wc = WordAttributes(ref attrs);
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
							goto match2b;
					}
					break;
				default:
					goto match2b;
				}
				break;
			match2b:
				{
					if ((wc != 0))
						NonKeywordAttrError(attrs, "expression");
					e = Expr(ContinueExpr);
				}
			} while (false);
			if ((attrs != null))
				e = e.PlusAttrs(attrs.ToRVList());
			if ((argName.Value != null))
				e = SetOperatorStyle(F.Call(S.NamedArg, IdNode(argName), e, argName.StartIndex, e.Range.EndIndex));
			return e;
		}
		void DetectVarDecl(bool allowUnassigned)
		{
			TokenType la0;
			VarDeclStart();
			la0 = LA0;
			if (la0 == TT.Set) {
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
			la0 = LA0;
			if (la0 == TT.Set) {
				if (!TryMatch((int) TT.Set))
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
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark)
					SubConditional();
				else if (!(la0 == EOF || la0 == TT.Colon || la0 == TT.Comma || la0 == TT.QuestionMark || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			Match((int) EOF, (int) TT.Comma, (int) TT.Semicolon);
		}
		static readonly HashSet<int> Scan_NoUnmatchedColon_set0 = NewSet((int) EOF, (int) TT.Colon, (int) TT.Comma, (int) TT.QuestionMark, (int) TT.Semicolon);
		bool Scan_NoUnmatchedColon()
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark)
					{if (!Scan_SubConditional())
						return false;}
				else if (!(la0 == EOF || la0 == TT.Colon || la0 == TT.Comma || la0 == TT.QuestionMark || la0 == TT.Semicolon))
					{if (!TryMatchExcept(Scan_NoUnmatchedColon_set0))
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
			for (;;) {
				la0 = LA0;
				if (la0 == EOF || la0 == TT.Colon)
					break;
				else if (la0 == TT.QuestionMark)
					SubConditional();
				else
					MatchExcept((int) EOF, (int) TT.Comma, (int) TT.Semicolon);
			}
			Match((int) TT.Colon);
		}
		bool Scan_SubConditional()
		{
			TokenType la0;
			if (!TryMatch((int) TT.QuestionMark))
				return false;
			for (;;) {
				la0 = LA0;
				if (la0 == EOF || la0 == TT.Colon)
					break;
				else if (la0 == TT.QuestionMark)
					{if (!Scan_SubConditional())
						return false;}
				else if (!TryMatchExcept((int) EOF, (int) TT.Comma, (int) TT.Semicolon))
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
			LNode type = pair.Item1, name = pair.Item2;
			la0 = LA0;
			if (la0 == TT.Set) {
				Skip();
				var init = Expr(ContinueExpr);
				return F.Call(S.Var, type, F.Call(S.Set, name, init, name.Range.StartIndex, init.Range.EndIndex), type.Range.StartIndex, init.Range.EndIndex);
			}
			return F.Call(S.Var, type, name, type.Range.StartIndex, name.Range.EndIndex);
		}
		Pair<LNode,LNode> VarDeclStart()
		{
			var e = DataType();
			var name = Match((int) TT.ContextualKeyword, (int) TT.Id);
			MaybeRecognizeVarAsKeyword(ref e);
			return Pair.Create(e, IdNode(name));
		}
		bool Scan_VarDeclStart()
		{
			if (!Scan_DataType())
				return false;
			if (!TryMatch((int) TT.ContextualKeyword, (int) TT.Id))
				return false;
			return true;
		}
		LNode ExprInParens(bool allowUnassignedVarDecl)
		{
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			if ((!Down(lp))) {
				return F.Call(S.Tuple, lp.StartIndex, rp.EndIndex);
			}
			return Up(InParens_ExprOrTuple(allowUnassignedVarDecl, lp.StartIndex, rp.EndIndex));
		}
		bool Scan_ExprInParens(bool allowUnassignedVarDecl)
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}
		static readonly HashSet<int> InParens_ExprOrTuple_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		LNode InParens_ExprOrTuple(bool allowUnassignedVarDecl, int startIndex, int endIndex)
		{
			TokenType la0, la1;
			la0 = LA0;
			if (InParens_ExprOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(allowUnassignedVarDecl);
				la0 = LA0;
				if (la0 == TT.Comma) {
					var list = new RVList<LNode> { 
						e
					};
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
					return F.Tuple(list, startIndex, endIndex);
				}
				return F.InParens(e, startIndex, endIndex);
			} else
				return F.Tuple(RVList<LNode>.Empty, startIndex, endIndex);
			Match((int) EOF);
		}
		LNode BracedBlock(Symbol spaceName = null, Symbol target = null)
		{
			var oldSpace = _spaceName;
			_spaceName = spaceName ?? oldSpace;
			var lb = Match((int) TT.LBrace);
			var rb = Match((int) TT.RBrace);
			var stmts = StmtListInside(lb).ToRVList();
			_spaceName = oldSpace;
			return SetBaseStyle(F.Call(target ?? S.Braces, stmts, lb.StartIndex, rb.EndIndex), NodeStyle.Statement);
		}
		bool Scan_BracedBlock(Symbol spaceName = null, Symbol target = null)
		{
			if (!TryMatch((int) TT.LBrace))
				return false;
			if (!TryMatch((int) TT.RBrace))
				return false;
			return true;
		}
		void NormalAttributes(ref RWList<LNode> attrs)
		{
			TokenType la0, la1;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					if (!(Down(0) && Up(Try_Scan_AsmOrModLabel(0)))) {
						la1 = LA(1);
						if (la1 == TT.RBrack) {
							var t = MatchAny();
							Skip();
							if ((Down(t))) {
								AttributeContents(ref attrs);
								Up();
							}
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
		void AttributeContents(ref RWList<LNode> attrs)
		{
			TokenType la0, la1;
			Token attrTarget = default(Token);
			la0 = LA0;
			if (la0 == TT.@return || la0 == TT.ContextualKeyword || la0 == TT.Id) {
				la1 = LA(1);
				if (la1 == TT.Colon) {
					attrTarget = MatchAny();
					Skip();
				}
			}
			ExprList(attrs = attrs ?? new RWList<LNode>(), allowTrailingComma: true, allowUnassignedVarDecl: true);
			if (attrTarget.Value != null) {
				var attrTargetNode = IdNode(attrTarget);
				for (int i = 0; i < attrs.Count; i++) {
					var attr = attrs[i];
					if ((!IsNamedArg(attr)))
						attrs[i] = SetOperatorStyle(F.Call(S.NamedArg, attrTargetNode, attr, attrTarget.StartIndex, attr.Range.EndIndex));
					else {
						attrTargetNode = attrs[i].Args[1];
						Error(attrTargetNode, "Syntax error: only one attribute target is allowed");
					}
				}
			}
		}
		void AttributeKeywords(ref RWList<LNode> attrs)
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					(attrs = attrs ?? new RWList<LNode>()).Add(IdNode(t));
				} else
					break;
			}
		}
		void TParamAttributeKeywords(ref RWList<LNode> attrs)
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@in || la0 == TT.AttrKeyword) {
					var t = MatchAny();
					(attrs = attrs ?? new RWList<LNode>()).Add(IdNode(t));
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
		int WordAttributes(ref RWList<LNode> attrs)
		{
			TokenType LA1;
			int nonKeywords = 0;
			if (LA0 == TT.Id && ((LA1 = LA(1)) == TT.Set || LA1 == TT.LParen || LA1 == TT.Dot))
				 return 0;
			for (;;) {
				switch (LA0) {
				case TT.AttrKeyword:
					{
						var t = MatchAny();
						attrs.Add(IdNode(t));
					}
					break;
				case TT.@new:
				case TT.ContextualKeyword:
				case TT.Id:
					{
						if (Try_WordAttributes_Test0(1)) {
							var t = MatchAny();
							LNode node;
							if ((t.Type() == TT.@new))
								node = IdNode(t);
							else
								node = F.Attr(_triviaWordAttribute, F.Id("#" + t.Value.ToString(), t.StartIndex, t.EndIndex));
							attrs = attrs ?? new RWList<LNode>();
							attrs.Add(node);
							nonKeywords++;
						} else
							goto stop;
					}
					break;
				default:
					goto stop;
				}
			}
		 stop:;
			return nonKeywords;
		}
		static readonly HashSet<int> Stmt_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Number, (int) TT.OrBits, (int) TT.OrXor, (int) TT.OtherLit, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set1 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Number, (int) TT.OrBits, (int) TT.OrXor, (int) TT.OtherLit, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Set, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set2 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.BQString, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set3 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set4 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.Comma, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Number, (int) TT.OrBits, (int) TT.OrXor, (int) TT.OtherLit, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.RBrace, (int) TT.RBrack, (int) TT.RParen, (int) TT.Semicolon, (int) TT.Set, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set5 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set6 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.At, (int) TT.ColonColon, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.Number, (int) TT.OtherLit, (int) TT.QuestionMark, (int) TT.SQString, (int) TT.String, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		static readonly HashSet<int> Stmt_set7 = NewSet((int) EOF, (int) TT.@as, (int) TT.@catch, (int) TT.@else, (int) TT.@finally, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.BQString, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LEGE, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set8 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.Semicolon, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		public LNode Stmt()
		{
			TokenType la1, la2;
			_stmtAttrs.Clear();
			int startIndex = LT0.StartIndex;
			NormalAttributes(ref _stmtAttrs);
			AttributeKeywords(ref _stmtAttrs);
			var wc = WordAttributes(ref _stmtAttrs);
			var attrs = _stmtAttrs.ToRVList();
			LNode r;
			do {
				switch (LA0) {
				case TT.@using:
					{
						switch (LA(1)) {
						case TT.@operator:
						case TT.ContextualKeyword:
						case TT.Id:
						case TT.Substitute:
						case TT.TypeKeyword:
							r = UsingDirective(startIndex, attrs);
							break;
						case TT.LParen:
							{
								r = UsingStmt(startIndex);
								r = r.PlusAttrs(attrs);
							}
							break;
						default:
							goto match33;
						}
					}
					break;
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
							goto match15;
						default:
							goto match33;
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
								if (Try_Stmt_Test0(0)) {
									if (_spaceName == LT(0).Value) {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											{
												la2 = LA(2);
												if (Stmt_set0.Contains((int) la2))
													r = TraitDecl(startIndex, attrs);
												else if (la2 == TT.Comma)
													r = MethodOrPropertyOrVar(startIndex, attrs);
												else
													goto match33;
											}
											break;
										case TT.ColonColon:
										case TT.Dot:
										case TT.LBrack:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.QuestionMark:
											{
												if (Try_PrimaryExpr_Test0(1)) {
													switch (LA(2)) {
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
														goto match15;
													}
												} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
													switch (LA(2)) {
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
													case TT.@this:
													case TT.@checked:
													case TT.@delegate:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.LT:
													case TT.Not:
													case TT.NotBits:
													case TT.Number:
													case TT.OtherLit:
													case TT.Power:
													case TT.SQString:
													case TT.String:
													case TT.Sub:
													case TT.Symbol:
														goto match15;
													default:
														goto match33;
													}
												} else {
													switch (LA(2)) {
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
													case TT.@this:
													case TT.@checked:
													case TT.@delegate:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Not:
													case TT.NotBits:
													case TT.Number:
													case TT.OtherLit:
													case TT.Power:
													case TT.SQString:
													case TT.String:
													case TT.Sub:
													case TT.Symbol:
														goto match15;
													default:
														goto match33;
													}
												}
											}
											break;
										case TT.LParen:
											{
												if (Try_BlockCallStmt_Test0(1)) {
													if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
														goto match11;
													else
														goto match14;
												} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto match11;
												else
													goto match15;
											}
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto match14;
												else
													goto match15;
											}
										case TT.Forward:
											goto match14;
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
										case TT.At:
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
											goto match15;
										case TT.Colon:
											goto match21;
										default:
											goto match33;
										}
									} else {
										switch (LA(1)) {
										case TT.@operator:
										case TT.ContextualKeyword:
										case TT.Id:
										case TT.Substitute:
										case TT.TypeKeyword:
											{
												la2 = LA(2);
												if (Stmt_set0.Contains((int) la2))
													r = TraitDecl(startIndex, attrs);
												else if (la2 == TT.Comma)
													r = MethodOrPropertyOrVar(startIndex, attrs);
												else
													goto match33;
											}
											break;
										case TT.ColonColon:
										case TT.Dot:
										case TT.LBrack:
										case TT.LT:
										case TT.Mul:
										case TT.Not:
										case TT.QuestionMark:
											{
												if (Try_PrimaryExpr_Test0(1)) {
													switch (LA(2)) {
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
														goto match15;
													}
												} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
													switch (LA(2)) {
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
													case TT.@this:
													case TT.@checked:
													case TT.@delegate:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.LT:
													case TT.Not:
													case TT.NotBits:
													case TT.Number:
													case TT.OtherLit:
													case TT.Power:
													case TT.SQString:
													case TT.String:
													case TT.Sub:
													case TT.Symbol:
														goto match15;
													default:
														goto match33;
													}
												} else {
													switch (LA(2)) {
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
													case TT.@this:
													case TT.@checked:
													case TT.@delegate:
													case TT.@new:
													case TT.@sizeof:
													case TT.@typeof:
													case TT.@unchecked:
													case TT.Add:
													case TT.AndBits:
													case TT.At:
													case TT.Dot:
													case TT.Forward:
													case TT.IncDec:
													case TT.LBrace:
													case TT.Not:
													case TT.NotBits:
													case TT.Number:
													case TT.OtherLit:
													case TT.Power:
													case TT.SQString:
													case TT.String:
													case TT.Sub:
													case TT.Symbol:
														goto match15;
													default:
														goto match33;
													}
												}
											}
											break;
										case TT.LParen:
											{
												if (Try_Constructor_Test2(1))
													goto match11;
												else if (Try_BlockCallStmt_Test0(1))
													goto match14;
												else
													goto match15;
											}
										case TT.LBrace:
											{
												if (Try_BlockCallStmt_Test0(1))
													goto match14;
												else
													goto match15;
											}
										case TT.Forward:
											goto match14;
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
										case TT.At:
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
											goto match15;
										case TT.Colon:
											goto match21;
										default:
											goto match33;
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
													goto match11;
												else
													goto match14;
											} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto match11;
											else
												goto match15;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.Forward:
										goto match14;
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
									case TT.At:
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
										goto match15;
									case TT.Colon:
										goto match21;
									default:
										goto match33;
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
												goto match11;
											else if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.Forward:
										goto match14;
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
									case TT.At:
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
										goto match15;
									case TT.Colon:
										goto match21;
									default:
										goto match33;
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
										{
											la2 = LA(2);
											if (Stmt_set0.Contains((int) la2))
												r = TraitDecl(startIndex, attrs);
											else if (la2 == TT.Comma)
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto match33;
										}
										break;
									case TT.ColonColon:
									case TT.Dot:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										{
											if (Try_PrimaryExpr_Test0(1)) {
												switch (LA(2)) {
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
													goto match15;
												}
											} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.LT:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											} else {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											}
										}
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto match11;
												else
													goto match14;
											} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto match11;
											else
												goto match15;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.Forward:
										goto match14;
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
									case TT.At:
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
										goto match15;
									case TT.Colon:
										goto match21;
									default:
										goto match33;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										{
											la2 = LA(2);
											if (Stmt_set0.Contains((int) la2))
												r = TraitDecl(startIndex, attrs);
											else if (la2 == TT.Comma)
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto match33;
										}
										break;
									case TT.ColonColon:
									case TT.Dot:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										{
											if (Try_PrimaryExpr_Test0(1)) {
												switch (LA(2)) {
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
													goto match15;
												}
											} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.LT:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											} else {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											}
										}
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto match11;
											else if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.Forward:
										goto match14;
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
									case TT.At:
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
										goto match15;
									case TT.Colon:
										goto match21;
									default:
										goto match33;
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
												goto match11;
											else
												goto match14;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto match11;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
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
											goto match11;
										else if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
								}
							}
						} else if (Is(0, _alias)) {
							if (Try_Stmt_Test0(0)) {
								if (_spaceName == LT(0).Value) {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										{
											la2 = LA(2);
											if (Stmt_set1.Contains((int) la2))
												r = AliasDecl(startIndex, attrs);
											else if (la2 == TT.Comma || la2 == TT.Semicolon)
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto match33;
										}
										break;
									case TT.ColonColon:
									case TT.Dot:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										{
											if (Try_PrimaryExpr_Test0(1)) {
												switch (LA(2)) {
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
													goto match15;
												}
											} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.LT:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											} else {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											}
										}
										break;
									case TT.LParen:
										{
											if (Try_BlockCallStmt_Test0(1)) {
												if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
													goto match11;
												else
													goto match14;
											} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto match11;
											else
												goto match15;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.Forward:
										goto match14;
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
									case TT.At:
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
										goto match15;
									case TT.Colon:
										goto match21;
									default:
										goto match33;
									}
								} else {
									switch (LA(1)) {
									case TT.@operator:
									case TT.ContextualKeyword:
									case TT.Id:
									case TT.Substitute:
									case TT.TypeKeyword:
										{
											la2 = LA(2);
											if (Stmt_set1.Contains((int) la2))
												r = AliasDecl(startIndex, attrs);
											else if (la2 == TT.Comma || la2 == TT.Semicolon)
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto match33;
										}
										break;
									case TT.ColonColon:
									case TT.Dot:
									case TT.LBrack:
									case TT.LT:
									case TT.Mul:
									case TT.Not:
									case TT.QuestionMark:
										{
											if (Try_PrimaryExpr_Test0(1)) {
												switch (LA(2)) {
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
													goto match15;
												}
											} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.LT:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											} else {
												switch (LA(2)) {
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
												case TT.@this:
												case TT.@checked:
												case TT.@delegate:
												case TT.@new:
												case TT.@sizeof:
												case TT.@typeof:
												case TT.@unchecked:
												case TT.Add:
												case TT.AndBits:
												case TT.At:
												case TT.Dot:
												case TT.Forward:
												case TT.IncDec:
												case TT.LBrace:
												case TT.Not:
												case TT.NotBits:
												case TT.Number:
												case TT.OtherLit:
												case TT.Power:
												case TT.SQString:
												case TT.String:
												case TT.Sub:
												case TT.Symbol:
													goto match15;
												default:
													goto match33;
												}
											}
										}
										break;
									case TT.LParen:
										{
											if (Try_Constructor_Test2(1))
												goto match11;
											else if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.LBrace:
										{
											if (Try_BlockCallStmt_Test0(1))
												goto match14;
											else
												goto match15;
										}
									case TT.Forward:
										goto match14;
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
									case TT.At:
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
										goto match15;
									case TT.Colon:
										goto match21;
									default:
										goto match33;
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
												goto match11;
											else
												goto match14;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto match11;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
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
											goto match11;
										else if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
								}
							}
						} else if (Try_Stmt_Test0(0)) {
							if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.ColonColon:
								case TT.Dot:
								case TT.LBrack:
								case TT.LT:
								case TT.Mul:
								case TT.Not:
								case TT.QuestionMark:
									{
										if (Try_PrimaryExpr_Test0(1)) {
											switch (LA(2)) {
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
												goto match15;
											}
										} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.LT:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										} else {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										}
									}
									break;
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto match11;
											else
												goto match14;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto match11;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
								}
							} else {
								switch (LA(1)) {
								case TT.ColonColon:
								case TT.Dot:
								case TT.LBrack:
								case TT.LT:
								case TT.Mul:
								case TT.Not:
								case TT.QuestionMark:
									{
										if (Try_PrimaryExpr_Test0(1)) {
											switch (LA(2)) {
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
												goto match15;
											}
										} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.LT:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										} else {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										}
									}
									break;
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto match11;
										else if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
								}
							}
						} else if (_spaceName == LT(0).Value) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_BlockCallStmt_Test0(1)) {
									if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
										goto match11;
									else
										goto match14;
								} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
									goto match11;
								else
									goto match15;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto match14;
								else
									goto match15;
							} else if (la1 == TT.Forward)
								goto match14;
							else if (Stmt_set2.Contains((int) la1))
								goto match15;
							else if (la1 == TT.Colon)
								goto match21;
							else
								goto match33;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto match11;
								else if (Try_BlockCallStmt_Test0(1))
									goto match14;
								else
									goto match15;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto match14;
								else
									goto match15;
							} else if (la1 == TT.Forward)
								goto match14;
							else if (Stmt_set2.Contains((int) la1))
								goto match15;
							else if (la1 == TT.Colon)
								goto match21;
							else
								goto match33;
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
									case TT.@base:
									case TT.@default:
									case TT.@this:
									case TT.@checked:
									case TT.@delegate:
									case TT.@new:
									case TT.@sizeof:
									case TT.@typeof:
									case TT.@unchecked:
									case TT.At:
									case TT.LBrace:
									case TT.LParen:
									case TT.Number:
									case TT.OtherLit:
									case TT.SQString:
									case TT.String:
									case TT.Symbol:
										r = OperatorCast(startIndex, attrs);
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
										goto match15;
									default:
										goto match33;
									}
								}
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
							case TT.Set:
							case TT.Sub:
							case TT.XorBits:
								{
									switch (LA(2)) {
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
									case TT.At:
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
										goto match15;
									default:
										goto match33;
									}
								}
								break;
							case TT.@operator:
							case TT.ContextualKeyword:
							case TT.Id:
							case TT.TypeKeyword:
								r = OperatorCast(startIndex, attrs);
								break;
							default:
								goto match33;
							}
						} else {
							switch (LA(1)) {
							case TT.Substitute:
								{
									la2 = LA(2);
									if (PrimaryExpr_set0.Contains((int) la2))
										r = OperatorCast(startIndex, attrs);
									else if (Stmt_set3.Contains((int) la2))
										goto match15;
									else
										goto match33;
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
							case TT.Set:
							case TT.Sub:
							case TT.XorBits:
								goto match15;
							default:
								goto match33;
							}
						}
					}
					break;
				case TT.Id:
					{
						if (Try_Stmt_Test0(0)) {
							if (_spaceName == LT(0).Value) {
								switch (LA(1)) {
								case TT.ColonColon:
								case TT.Dot:
								case TT.LBrack:
								case TT.LT:
								case TT.Mul:
								case TT.Not:
								case TT.QuestionMark:
									{
										if (Try_PrimaryExpr_Test0(1)) {
											switch (LA(2)) {
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
												goto match15;
											}
										} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.LT:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										} else {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										}
									}
									break;
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_BlockCallStmt_Test0(1)) {
											if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
												goto match11;
											else
												goto match14;
										} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
											goto match11;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
								}
							} else {
								switch (LA(1)) {
								case TT.ColonColon:
								case TT.Dot:
								case TT.LBrack:
								case TT.LT:
								case TT.Mul:
								case TT.Not:
								case TT.QuestionMark:
									{
										if (Try_PrimaryExpr_Test0(1)) {
											switch (LA(2)) {
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
												goto match15;
											}
										} else if (LT(1).EndIndex == LT(1 + 1).StartIndex) {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.LT:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										} else {
											switch (LA(2)) {
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
											case TT.@this:
											case TT.@checked:
											case TT.@delegate:
											case TT.@new:
											case TT.@sizeof:
											case TT.@typeof:
											case TT.@unchecked:
											case TT.Add:
											case TT.AndBits:
											case TT.At:
											case TT.Dot:
											case TT.Forward:
											case TT.IncDec:
											case TT.LBrace:
											case TT.Not:
											case TT.NotBits:
											case TT.Number:
											case TT.OtherLit:
											case TT.Power:
											case TT.SQString:
											case TT.String:
											case TT.Sub:
											case TT.Symbol:
												goto match15;
											default:
												goto match33;
											}
										}
									}
									break;
								case TT.@operator:
								case TT.ContextualKeyword:
								case TT.Id:
								case TT.Substitute:
								case TT.TypeKeyword:
									r = MethodOrPropertyOrVar(startIndex, attrs);
									break;
								case TT.LParen:
									{
										if (Try_Constructor_Test2(1))
											goto match11;
										else if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.LBrace:
									{
										if (Try_BlockCallStmt_Test0(1))
											goto match14;
										else
											goto match15;
									}
								case TT.Forward:
									goto match14;
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
								case TT.At:
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
									goto match15;
								case TT.Colon:
									goto match21;
								default:
									goto match33;
								}
							}
						} else if (_spaceName == LT(0).Value) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_BlockCallStmt_Test0(1)) {
									if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
										goto match11;
									else
										goto match14;
								} else if (Try_Constructor_Test0(1) || Try_Constructor_Test2(1))
									goto match11;
								else
									goto match15;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto match14;
								else
									goto match15;
							} else if (la1 == TT.Forward)
								goto match14;
							else if (Stmt_set2.Contains((int) la1))
								goto match15;
							else if (la1 == TT.Colon)
								goto match21;
							else
								goto match33;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto match11;
								else if (Try_BlockCallStmt_Test0(1))
									goto match14;
								else
									goto match15;
							} else if (la1 == TT.LBrace) {
								if (Try_BlockCallStmt_Test0(1))
									goto match14;
								else
									goto match15;
							} else if (la1 == TT.Forward)
								goto match14;
							else if (Stmt_set2.Contains((int) la1))
								goto match15;
							else if (la1 == TT.Colon)
								goto match21;
							else
								goto match33;
						}
					}
					break;
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (Try_Stmt_Test0(0)) {
							la1 = LA(1);
							if (Stmt_set6.Contains((int) la1)) {
								if (Down(1) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow)) {
									if (Try_PrimaryExpr_Test0(1)) {
										la2 = LA(2);
										if (Stmt_set4.Contains((int) la2))
											r = MethodOrPropertyOrVar(startIndex, attrs);
										else
											goto match15;
									} else {
										la2 = LA(2);
										switch (la2) {
										case EOF:
										case TT.@as:
										case TT.@catch:
										case TT.@else:
										case TT.@finally:
										case TT.@in:
										case TT.@is:
										case TT.@using:
										case TT.@while:
											goto match15;
										default:
											if (Stmt_set4.Contains((int) la2))
												r = MethodOrPropertyOrVar(startIndex, attrs);
											else
												goto match33;
											break;
										}
									}
								} else if (Try_PrimaryExpr_Test0(1)) {
									la2 = LA(2);
									if (Stmt_set4.Contains((int) la2))
										r = MethodOrPropertyOrVar(startIndex, attrs);
									else
										goto match15;
								} else {
									la2 = LA(2);
									switch (la2) {
									case EOF:
									case TT.@as:
									case TT.@catch:
									case TT.@else:
									case TT.@finally:
									case TT.@in:
									case TT.@is:
									case TT.@using:
									case TT.@while:
										goto match15;
									default:
										if (Stmt_set4.Contains((int) la2))
											r = MethodOrPropertyOrVar(startIndex, attrs);
										else
											goto match33;
										break;
									}
								}
							} else if (Stmt_set5.Contains((int) la1))
								goto match15;
							else
								goto match33;
						} else
							goto match15;
					}
					break;
				case TT.@this:
					{
						if (_spaceName != S.Def) {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test1(1) || Try_Constructor_Test2(1))
									goto match11;
								else
									goto match15;
							} else if (Stmt_set7.Contains((int) la1))
								goto match15;
							else
								goto match33;
						} else {
							la1 = LA(1);
							if (la1 == TT.LParen) {
								if (Try_Constructor_Test2(1))
									goto match11;
								else
									goto match15;
							} else if (Stmt_set7.Contains((int) la1))
								goto match15;
							else
								goto match33;
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
									if ((wc != 0))
										NonKeywordAttrError(attrs, "destructor");
								} else if (Stmt_set7.Contains((int) la2))
									goto match15;
								else
									goto match33;
							}
							break;
						case TT.@base:
						case TT.@default:
						case TT.@checked:
						case TT.@delegate:
						case TT.@new:
						case TT.@operator:
						case TT.@sizeof:
						case TT.@typeof:
						case TT.@unchecked:
						case TT.Add:
						case TT.AndBits:
						case TT.At:
						case TT.Dot:
						case TT.Forward:
						case TT.IncDec:
						case TT.LBrace:
						case TT.LParen:
						case TT.Mul:
						case TT.Not:
						case TT.NotBits:
						case TT.Number:
						case TT.OtherLit:
						case TT.Power:
						case TT.SQString:
						case TT.String:
						case TT.Sub:
						case TT.Substitute:
						case TT.Symbol:
						case TT.TypeKeyword:
							goto match15;
						default:
							goto match33;
						}
					}
					break;
				case TT.LBrace:
					{
						r = BracedBlock();
						r = r.PlusAttrs(attrs);
						if ((wc != 0))
							NonKeywordAttrError(attrs, "braced-block statement");
					}
					break;
				case TT.@checked:
				case TT.@unchecked:
					{
						la1 = LA(1);
						if (la1 == TT.LParen)
							goto match15;
						else if (la1 == TT.LBrace) {
							r = CheckedOrUncheckedStmt(startIndex);
							r = r.PlusAttrs(attrs);
						} else
							goto match33;
					}
					break;
				case TT.@default:
					{
						la1 = LA(1);
						if (la1 == TT.LParen)
							goto match15;
						else if (la1 == TT.Colon)
							goto match21;
						else
							goto match33;
					}
				case TT.@base:
				case TT.@new:
				case TT.@sizeof:
				case TT.@typeof:
				case TT.Add:
				case TT.AndBits:
				case TT.At:
				case TT.Dot:
				case TT.Forward:
				case TT.IncDec:
				case TT.LParen:
				case TT.Mul:
				case TT.Not:
				case TT.Number:
				case TT.OtherLit:
				case TT.Power:
				case TT.SQString:
				case TT.String:
				case TT.Sub:
				case TT.Symbol:
					goto match15;
				case TT.@do:
					{
						r = DoStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@case:
					{
						r = CaseStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@goto:
					{
						la1 = LA(1);
						if (Stmt_set8.Contains((int) la1)) {
							r = GotoStmt(startIndex);
							r = r.PlusAttrs(attrs);
						} else if (la1 == TT.@case) {
							r = GotoCaseStmt(startIndex);
							r = r.PlusAttrs(attrs);
						} else
							goto match33;
					}
					break;
				case TT.@break:
				case TT.@continue:
				case TT.@return:
				case TT.@throw:
					{
						r = ReturnBreakContinueThrow(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@while:
					{
						r = WhileStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@for:
					{
						r = ForStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@foreach:
					{
						r = ForEachStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@if:
					{
						r = IfStmt(startIndex);
						r = r.PlusAttrs(attrs);
						if ((wc != 0))
							NonKeywordAttrError(attrs, "if statement");
					}
					break;
				case TT.@switch:
					{
						r = SwitchStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@lock:
					{
						r = LockStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@fixed:
					{
						r = FixedStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.@try:
					{
						r = TryStmt(startIndex);
						r = r.PlusAttrs(attrs);
					}
					break;
				case TT.Semicolon:
					{
						var t = MatchAny();
						r = F.Id(S.Missing, startIndex, t.EndIndex).PlusAttrs(attrs);
						if ((wc != 0))
							NonKeywordAttrError(attrs, "empty statement");
					}
					break;
				default:
					goto match33;
				}
				break;
			match11:
				{
					r = Constructor(startIndex, attrs);
					if ((wc != 0 && !r.Args[1, F._Missing].IsIdNamed(S.This)))
						NonKeywordAttrError(attrs, "constructor");
				}
				break;
			match14:
				{
					r = BlockCallStmt();
					r = r.PlusAttrs(attrs);
					if ((wc != 0))
						NonKeywordAttrError(attrs, "block-call statement");
				}
				break;
			match15:
				{
					r = ExprStatement();
					r = r.PlusAttrs(attrs);
					if ((wc != 0))
						NonKeywordAttrError(attrs, "expression");
				}
				break;
			match21:
				{
					r = LabelStmt(startIndex);
					r = r.PlusAttrs(attrs);
				}
				break;
			match33:
				{
					r = Error("Syntax error: statement expected at '{0}'", LT(0).SourceText(SourceFile.Text));
					ScanToEndOfStmt();
				}
			} while (false);
			return r;
		}
		LNode ExprStatement()
		{
			var r = Expr(ContinueExpr);
			switch (LA0) {
			case EOF:
			case TT.@catch:
			case TT.@else:
			case TT.@finally:
			case TT.@while:
				r = F.Call(S.Result, r, r.Range.StartIndex, r.Range.EndIndex);
				break;
			default:
				Match((int) TT.Semicolon);
				break;
			}
			return r;
		}
		void ScanToEndOfStmt()
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (!(la0 == EOF || la0 == TT.LBrace || la0 == TT.Semicolon))
					Skip();
				else
					break;
			}
			la0 = LA0;
			if (la0 == TT.Semicolon)
				Skip();
			else if (la0 == TT.LBrace) {
				Skip();
				la0 = LA0;
				if (la0 == TT.RBrace)
					Skip();
			}
		}
		LNode SpaceDecl(int startIndex, RVList<LNode> attrs)
		{
			var t = MatchAny();
			var kind = (Symbol) t.Value;
			var r = RestOfSpaceDecl(startIndex, kind, attrs);
			return r;
		}
		LNode TraitDecl(int startIndex, RVList<LNode> attrs)
		{
			Check(Is(0, _trait), "Is($LI, _trait)");
			var t = Match((int) TT.ContextualKeyword);
			var r = RestOfSpaceDecl(startIndex, S.Trait, attrs);
			return r;
		}
		LNode RestOfSpaceDecl(int startIndex, Symbol kind, RVList<LNode> attrs)
		{
			TokenType la0;
			var name = ComplexNameDecl();
			var bases = BaseListOpt();
			WhereClausesOpt(ref name);
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				return F.Call(kind, name, bases, startIndex, end.EndIndex).WithAttrs(attrs);
			} else {
				var body = BracedBlock(CoreName(name).Name);
				return F.Call(kind, name, bases, body, startIndex, body.Range.EndIndex).WithAttrs(attrs);
			}
		}
		LNode AliasDecl(int startIndex, RVList<LNode> attrs)
		{
			Check(Is(0, _alias), "Is($LI, _alias)");
			var t = Match((int) TT.ContextualKeyword);
			var newName = ComplexNameDecl();
			var r = RestOfAlias(startIndex, newName);
			return r.WithAttrs(attrs);
		}
		LNode UsingDirective(int startIndex, RVList<LNode> attrs)
		{
			TokenType la0;
			Match((int) TT.@using);
			var nsName = ComplexNameDecl();
			la0 = LA0;
			if (la0 == TT.Set) {
				var r = RestOfAlias(startIndex, nsName);
				return r.WithAttrs(attrs).PlusAttr(_filePrivate);
			} else {
				Match((int) TT.Semicolon);
				return F.Call(S.Import, nsName);
			}
		}
		LNode RestOfAlias(int startIndex, LNode newName)
		{
			TokenType la0;
			Match((int) TT.Set);
			var oldName = ComplexNameDecl();
			var bases = BaseListOpt();
			WhereClausesOpt(ref newName);
			var name = F.Call(S.Set, newName, oldName, newName.Range.StartIndex, oldName.Range.EndIndex);
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				return F.Call(S.Alias, name, bases, startIndex, end.EndIndex);
			} else {
				var body = BracedBlock(CoreName(newName).Name);
				return F.Call(S.Alias, name, bases, body, startIndex, body.Range.EndIndex);
			}
		}
		LNode EnumDecl(int startIndex, RVList<LNode> attrs)
		{
			TokenType la0;
			var t = MatchAny();
			var id = Match((int) TT.Id);
			var name = IdNode(id);
			var bases = BaseListOpt();
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				return F.Call(S.Enum, name, bases, startIndex, end.EndIndex).WithAttrs(attrs);
			} else {
				var lb = Match((int) TT.LBrace);
				var rb = Match((int) TT.RBrace);
				var list = AppendExprsInside(lb, new RWList<LNode>(), true);
				var body = F.Braces(list, lb.StartIndex, rb.EndIndex);
				return F.Call(S.Enum, name, bases, body, startIndex, body.Range.EndIndex).WithAttrs(attrs);
			}
		}
		LNode BaseListOpt()
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.Colon) {
				var bases = new RVList<LNode>();
				Skip();
				bases.Add(DataType());
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						bases.Add(DataType());
					} else
						break;
				}
				return F.Tuple(bases);
			} else
				return F._Missing;
		}
		void WhereClausesOpt(ref LNode name)
		{
			TokenType la0;
			var list = new BMultiMap<Symbol,LNode>();
			for (;;) {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword)
					list.Add(WhereClause());
				else
					break;
			}
			if ((list.Count != 0)) {
				if ((!name.CallsMin(S.Of, 2)))
					Error("'{0}' is not generic and cannot use 'where' clauses.", name.ToString());
				else {
					var tparams = name.Args.ToRWList();
					for (int i = 1; i < tparams.Count; i++) {
						var wheres = list[TParamSymbol(tparams[i])];
						tparams[i] = tparams[i].PlusAttrs(wheres);
						wheres.Clear();
					}
					name = name.WithArgs(tparams.ToRVList());
					if ((list.Count > 0))
						Error(list[0].Value, "There is no type parameter named '{0}'", list[0].Key);
				}
			}
		}
		KeyValuePair<Symbol,LNode> WhereClause()
		{
			TokenType la0;
			Check(Is(0, _where), "Is($LI, _where)");
			var where = MatchAny();
			var T = Match((int) TT.ContextualKeyword, (int) TT.Id);
			Match((int) TT.Colon);
			var constraints = RVList<LNode>.Empty;
			constraints.Add(WhereConstraint());
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					constraints.Add(WhereConstraint());
				} else
					break;
			}
			return new KeyValuePair<Symbol,LNode>((Symbol) T.Value, F.Call(S.Where, constraints, where.StartIndex, constraints.Last.Range.EndIndex));
		}
		LNode WhereConstraint()
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.@class || la0 == TT.@struct) {
				var t = MatchAny();
				return IdNode(t);
			} else if (la0 == TT.@new) {
				var n = MatchAny();
				Check(LT(0).Count == 0, "LT($LI).Count == 0");
				var lp = Match((int) TT.LParen);
				var rp = Match((int) TT.RParen);
				return F.Call(S.New, n.StartIndex, rp.EndIndex);
			} else {
				var t = DataType();
				return t;
			}
		}
		Token AsmOrModLabel()
		{
			Check(LT(0).Value == _assembly || LT(0).Value == _module, "LT($LI).Value == _assembly || LT($LI).Value == _module");
			var t = Match((int) TT.ContextualKeyword);
			Match((int) TT.Colon);
			return t;
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
		LNode AssemblyOrModuleAttribute(int startIndex, RVList<LNode> attrs)
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var lb = MatchAny();
			var rb = Match((int) TT.RBrack);
			Down(lb);
			var kind = AsmOrModLabel();
			var list = new RWList<LNode>();
			ExprList(list);
			Up();
			var r = F.Call(kind.Value == _module ? S.Module : S.Assembly, list.ToRVList(), startIndex, rb.EndIndex);
			return r.WithAttrs(attrs);
		}
		LNode MethodOrPropertyOrVar(int startIndex, RVList<LNode> attrs)
		{
			TokenType la0, la1;
			LNode r;
			var type = DataType();
			do {
				la0 = LA0;
				if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
					la1 = LA(1);
					if (la1 == TT.Comma || la1 == TT.Semicolon || la1 == TT.Set) {
						MaybeRecognizeVarAsKeyword(ref type);
						var parts = new RVList<LNode> { 
							type
						};
						parts.Add(NameAndMaybeInit(IsArrayType(type)));
						for (;;) {
							la0 = LA0;
							if (la0 == TT.Comma) {
								Skip();
								parts.Add(NameAndMaybeInit(IsArrayType(type)));
							} else
								break;
						}
						var end = Match((int) TT.Semicolon);
						r = F.Call(S.Var, parts, type.Range.StartIndex, end.EndIndex).PlusAttrs(attrs);
					} else
						goto match2;
				} else
					goto match2;
				break;
			match2:
				{
					var name = ComplexNameDecl();
					switch (LA0) {
					case TT.LParen:
						r = MethodArgListAndBody(startIndex, attrs, S.Def, type, name);
						break;
					case TT.At:
					case TT.ContextualKeyword:
					case TT.Forward:
					case TT.LBrace:
						{
							WhereClausesOpt(ref name);
							var body = MethodBodyOrForward();
							r = F.Call(S.Property, type, name, body, type.Range.StartIndex, body.Range.EndIndex).PlusAttrs(attrs);
						}
						break;
					default:
						{
							ScanToEndOfStmt();
							Error("Syntax error in method, property, or variable declaration");
							r = F._Missing.PlusAttrs(attrs);
						}
						break;
					}
				}
			} while (false);
			return r;
		}
		LNode OperatorCast(int startIndex, RVList<LNode> attrs)
		{
			LNode r;
			var op = MatchAny();
			var type = DataType();
			var name = F.Attr(_triviaUseOperatorKeyword, F.Id(S.Cast, op.StartIndex, op.EndIndex));
			r = MethodArgListAndBody(startIndex, attrs, S.Def, type, name);
			return r;
		}
		LNode MethodArgListAndBody(int startIndex, RVList<LNode> attrs, Symbol kind, LNode type, LNode name)
		{
			TokenType la0;
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			WhereClausesOpt(ref name);
			LNode r, baseCall = null;
			la0 = LA0;
			if (la0 == TT.Colon) {
				Skip();
				var target = Match((int) TT.@base, (int) TT.@this);
				var baselp = Match((int) TT.LParen);
				var baserp = Match((int) TT.RParen);
				baseCall = F.Call((Symbol) target.Value, ExprListInside(baselp), target.StartIndex, baserp.EndIndex);
				if ((kind != S.Cons))
					Error(baseCall, "This is not a constructor declaration, so there should be no ':' clause.");
			}
			for (int i = 0; i < attrs.Count; i++) {
				var attr = attrs[i];
				if (IsNamedArg(attr) && attr.Args[0].IsIdNamed(S.Return)) {
					type = type.PlusAttr(attr.Args[1]);
					attrs.RemoveAt(i);
					i--;
				}
			}
			la0 = LA0;
			if (la0 == TT.At || la0 == TT.Forward || la0 == TT.LBrace) {
				var body = MethodBodyOrForward();
				if (kind == S.Delegate)
					Error("A 'delegate' is not expected to have a method body.");
				if (baseCall != null)
					body = body.WithArgs(body.Args.Insert(0, baseCall)).WithRange(baseCall.Range.StartIndex, body.Range.EndIndex);
				var parts = new RVList<LNode> { 
					type, name, ArgTuple(lp, rp), body
				};
				r = F.Call(kind, parts, startIndex, body.Range.EndIndex);
			} else {
				var end = Match((int) TT.Semicolon);
				if (kind == S.Cons && baseCall != null) {
					Error(baseCall, "A method body is required.");
					var parts = new RVList<LNode> { 
						type, name, ArgTuple(lp, rp), LNode.Call(S.Braces, new RVList<LNode>(baseCall), baseCall.Range)
					};
					return F.Call(kind, parts, startIndex, baseCall.Range.EndIndex);
				}
				r = F.Call(kind, type, name, ArgTuple(lp, rp), startIndex, end.EndIndex);
			}
			return r.PlusAttrs(attrs);
		}
		LNode MethodBodyOrForward()
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.Forward) {
				var op = MatchAny();
				var e = ExprStart(false);
				Match((int) TT.Semicolon);
				return F.Call(S.Forward, e, op.StartIndex, e.Range.EndIndex);
			} else if (la0 == TT.At) {
				var at = MatchAny();
				var lb = Match((int) TT.LBrack);
				var rb = Match((int) TT.RBrack);
				Match((int) TT.Semicolon);
				return F.Literal(lb.Children, at.StartIndex, rb.EndIndex);
			} else {
				var body = BracedBlock(S.Def);
				return body;
			}
		}
		LNode NameAndMaybeInit(bool isArray)
		{
			TokenType la0;
			var name = Match((int) TT.ContextualKeyword, (int) TT.Id);
			LNode r = F.Id((Symbol) name.Value, name.StartIndex, name.EndIndex);
			la0 = LA0;
			if (la0 == TT.Set) {
				Skip();
				do {
					la0 = LA0;
					if (la0 == TT.LBrace) {
						if (Down(0) && Up(HasNoSemicolons()) && isArray) {
							var lb = MatchAny();
							var rb = Match((int) TT.RBrace);
							var initializers = InitializerListInside(lb).ToRVList();
							var expr = F.Call(S.ArrayInit, initializers, lb.StartIndex, rb.EndIndex);
							expr = SetBaseStyle(expr, NodeStyle.OldStyle);
							r = F.Call(S.Set, r, expr, name.StartIndex, rb.EndIndex);
						} else
							goto match2;
					} else
						goto match2;
					break;
				match2:
					{
						var init = ExprStart(false);
						r = F.Call(S.Set, r, init, name.StartIndex, init.Range.EndIndex);
					}
				} while (false);
			}
			return r;
		}
		void NoSemicolons()
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (!(la0 == EOF || la0 == TT.Semicolon))
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
			for (;;) {
				la0 = LA0;
				if (!(la0 == EOF || la0 == TT.Semicolon))
					{if (!TryMatchExcept((int) EOF, (int) TT.Semicolon))
						return false;}
				else
					break;
			}
			if (!TryMatch((int) EOF))
				return false;
			return true;
		}
		LNode Constructor(int startIndex, RVList<LNode> attrs)
		{
			TokenType la0;
			LNode r;
			Token n;
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
					if (_spaceName != S.Def) {
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
			LNode name = F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex);
			r = MethodArgListAndBody(startIndex, attrs, S.Cons, F._Missing, name);
			return r;
		}
		LNode Destructor(int startIndex, RVList<LNode> attrs)
		{
			TokenType la0;
			LNode r;
			Token n;
			var tilde = MatchAny();
			la0 = LA0;
			if (la0 == TT.ContextualKeyword || la0 == TT.Id) {
				Check(LT(0).Value == _spaceName, "LT($LI).Value == _spaceName");
				n = MatchAny();
			} else
				n = Match((int) TT.@this);
			LNode name = F.Call(S.NotBits, F.Id((Symbol) n.Value, n.StartIndex, n.EndIndex), tilde.StartIndex, n.EndIndex);
			r = MethodArgListAndBody(startIndex, attrs, S.Def, F._Missing, name);
			return r;
		}
		LNode DelegateDecl(int startIndex, RVList<LNode> attrs)
		{
			Skip();
			var type = DataType();
			var name = ComplexNameDecl();
			var r = MethodArgListAndBody(startIndex, attrs, S.Delegate, type, name);
			return r.WithAttrs(attrs);
		}
		LNode EventDecl(int startIndex, RVList<LNode> attrs)
		{
			TokenType la0;
			LNode r;
			Skip();
			var type = DataType();
			var name = ComplexNameDecl();
			la0 = LA0;
			if (la0 == TT.LBrace) {
				var body = BracedBlock(S.Def);
				r = F.Call(S.Event, type, name, body, startIndex, body.Range.EndIndex);
			} else {
				var parts = new RVList<LNode>(type, name);
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						parts.Add(ComplexNameDecl());
					} else
						break;
				}
				var end = Match((int) TT.Semicolon);
				r = F.Call(S.Event, parts, startIndex, end.EndIndex);
			}
			return r.WithAttrs(attrs);
		}
		LNode LabelStmt(int startIndex)
		{
			var id = Match((int) TT.@default, (int) TT.ContextualKeyword, (int) TT.Id);
			var end = Match((int) TT.Colon);
			return F.Call(S.Label, IdNode(id), startIndex, end.EndIndex);
		}
		LNode CaseStmt(int startIndex)
		{
			TokenType la0;
			var cases = RVList<LNode>.Empty;
			var kw = Match((int) TT.@case);
			cases.Add(ExprStart(false));
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					cases.Add(ExprStart(false));
				} else
					break;
			}
			var end = Match((int) TT.Colon);
			return F.Call(S.Case, cases, startIndex, end.EndIndex);
		}
		LNode BlockCallStmt()
		{
			TokenType la0;
			var id = MatchAny();
			Check(Try_BlockCallStmt_Test0(0), "(TT.LParen TT.RParen (TT.LBrace TT.RBrace | TT.Id) | TT.LBrace TT.RBrace | TT.Forward)");
			var args = new RWList<LNode>();
			LNode block;
			la0 = LA0;
			if (la0 == TT.LParen) {
				var lp = MatchAny();
				var rp = Match((int) TT.RParen);
				AppendExprsInside(lp, args);
				la0 = LA0;
				if (la0 == TT.LBrace)
					block = BracedBlock();
				else {
					block = Stmt();
					if ((ColumnOf(block.Range.StartIndex) <= ColumnOf(id.StartIndex) || !char.IsLower(id.Value.ToString().FirstOrDefault())))
						_messages.Write(_Warning, block, "Probable missing semicolon before this statement.");
				}
			} else if (la0 == TT.Forward) {
				var fwd = MatchAny();
				var e = ExprStart(true);
				Match((int) TT.Semicolon);
				block = SetOperatorStyle(F.Call(S.Forward, e, fwd.StartIndex, e.Range.EndIndex));
			} else
				block = BracedBlock();
			args.Add(block);
			var result = F.Call((Symbol) id.Value, args.ToRVList(), id.StartIndex, block.Range.EndIndex);
			if (block.Calls(S.Forward, 1))
				result = F.Attr(_triviaForwardedProperty, result);
			return SetBaseStyle(result, NodeStyle.Special);
		}
		LNode ReturnBreakContinueThrow(int startIndex)
		{
			var kw = MatchAny();
			var e = ExprOrNull(false);
			var end = Match((int) TT.Semicolon);
			if (e != null)
				 return F.Call((Symbol) kw.Value, e, startIndex, end.EndIndex);
			else
				 return F.Call((Symbol) kw.Value, startIndex, end.EndIndex);
		}
		LNode GotoStmt(int startIndex)
		{
			Skip();
			var e = ExprOrNull(false);
			var end = Match((int) TT.Semicolon);
			if (e != null)
				 return F.Call(S.Goto, e, startIndex, end.EndIndex);
			else
				 return F.Call(S.Goto, startIndex, end.EndIndex);
		}
		LNode GotoCaseStmt(int startIndex)
		{
			TokenType la0, la1;
			LNode e = null;
			Skip();
			Skip();
			la0 = LA0;
			if (la0 == TT.@default) {
				la1 = LA(1);
				if (la1 == TT.Semicolon) {
					var @def = MatchAny();
					e = F.Id(S.Default, @def.StartIndex, @def.EndIndex);
				} else
					e = ExprOpt(false);
			} else
				e = ExprOpt(false);
			var end = Match((int) TT.Semicolon);
			return F.Call(S.GotoCase, e, startIndex, end.EndIndex);
		}
		LNode CheckedOrUncheckedStmt(int startIndex)
		{
			var kw = MatchAny();
			var bb = BracedBlock();
			return F.Call((Symbol) kw.Value, bb, startIndex, bb.Range.EndIndex);
		}
		LNode DoStmt(int startIndex)
		{
			Skip();
			var block = Stmt();
			Match((int) TT.@while);
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var end = Match((int) TT.Semicolon);
			var parts = new RWList<LNode> { 
				block
			};
			SingleExprInside(p, "while (...)", parts);
			return F.Call(S.DoWhile, parts.ToRVList(), startIndex, end.EndIndex);
		}
		LNode WhileStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			var cond = SingleExprInside(p, "while (...)");
			return F.Call(S.While, cond, block, startIndex, block.Range.EndIndex);
		}
		LNode ForStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			Down(p);
			var init = ExprOpt(true);
			Match((int) TT.Semicolon);
			var cond = ExprOpt(false);
			Match((int) TT.Semicolon);
			var inc = ExprOpt(false);
			Up();
			var parts = new RVList<LNode> { 
				init, cond, inc, block
			};
			return F.Call(S.For, parts, startIndex, block.Range.EndIndex);
		}
		LNode ForEachStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			Down(p);
			var @var = VarIn();
			var list = ExprStart(false);
			Up();
			return F.Call(S.ForEach, @var, list, block, startIndex, block.Range.EndIndex);
		}
		static readonly HashSet<int> VarIn_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@in, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.At, (int) TT.Backslash, (int) TT.BQString, (int) TT.Colon, (int) TT.ColonColon, (int) TT.CompoundSet, (int) TT.ContextualKeyword, (int) TT.DivMod, (int) TT.Dot, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.Forward, (int) TT.GT, (int) TT.Id, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LBrace, (int) TT.LEGE, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.Number, (int) TT.OrBits, (int) TT.OrXor, (int) TT.OtherLit, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuestionMark, (int) TT.QuickBind, (int) TT.Set, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword, (int) TT.XorBits);
		LNode VarIn()
		{
			TokenType la1;
			LNode @var;
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
								goto match1;
							else
								goto match2;
						} else
							goto match2;
					}
				default:
					goto match1;
				}
			match1:
				{
					Check(Try_VarIn_Test0(0), "Atom TT.@in");
					@var = Atom();
				}
				break;
			match2:
				{
					var pair = VarDeclStart();
					@var = F.Call(S.Var, pair.A, pair.B, pair.A.Range.StartIndex, pair.B.Range.EndIndex);
				}
			} while (false);
			Match((int) TT.@in);
			return @var;
		}
		static readonly HashSet<int> IfStmt_set0 = NewSet((int) TT.@base, (int) TT.@break, (int) TT.@continue, (int) TT.@default, (int) TT.@return, (int) TT.@this, (int) TT.@throw, (int) TT.@case, (int) TT.@checked, (int) TT.@class, (int) TT.@delegate, (int) TT.@do, (int) TT.@enum, (int) TT.@event, (int) TT.@fixed, (int) TT.@for, (int) TT.@foreach, (int) TT.@goto, (int) TT.@if, (int) TT.@interface, (int) TT.@lock, (int) TT.@namespace, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@struct, (int) TT.@switch, (int) TT.@try, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.@using, (int) TT.@while, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.Semicolon, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		LNode IfStmt(int startIndex)
		{
			TokenType la0, la1;
			LNode @else = null;
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var then = Stmt();
			la0 = LA0;
			if (la0 == TT.@else) {
				la1 = LA(1);
				if (IfStmt_set0.Contains((int) la1)) {
					Skip();
					@else = Stmt();
				}
			}
			var cond = SingleExprInside(p, "if (...)");
			if (@else == null)
				 return F.Call(S.If, cond, then, startIndex, then.Range.EndIndex);
			else
				 return F.Call(S.If, cond, then, @else, startIndex, then.Range.EndIndex);
		}
		LNode SwitchStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			var expr = SingleExprInside(p, "switch (...)");
			return F.Call(S.Switch, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode UsingStmt(int startIndex)
		{
			Skip();
			var p = MatchAny();
			Match((int) TT.RParen);
			var block = Stmt();
			var expr = SingleExprInside(p, "using (...)");
			return F.Call(S.UsingStmt, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode LockStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(S.Lock, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode FixedStmt(int startIndex)
		{
			Skip();
			var p = Match((int) TT.LParen);
			Match((int) TT.RParen);
			var block = Stmt();
			var expr = SingleExprInside(p, "lock (...)");
			return F.Call(S.Fixed, expr, block, startIndex, block.Range.EndIndex);
		}
		LNode TryStmt(int startIndex)
		{
			TokenType la0, la1;
			Skip();
			var header = Stmt();
			var parts = new RVList<LNode> { 
				header
			};
			LNode expr, handler;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@catch) {
					la1 = LA(1);
					if (IfStmt_set0.Contains((int) la1)) {
						var kw = MatchAny();
						la0 = LA0;
						if (la0 == TT.LParen) {
							var p = MatchAny();
							Match((int) TT.RParen);
							handler = Stmt();
							expr = SingleExprInside(p, "catch (...)", null, true);
						} else {
							handler = Stmt();
							expr = F.Id(S.Missing, kw.EndIndex, kw.EndIndex);
						}
						parts.Add(F.Call(S.Catch, expr, handler, kw.StartIndex, handler.Range.EndIndex));
					} else
						break;
				} else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == TT.@finally) {
					la1 = LA(1);
					if (IfStmt_set0.Contains((int) la1)) {
						var kw = MatchAny();
						handler = Stmt();
						parts.Add(F.Call(S.Finally, handler, kw.StartIndex, handler.Range.EndIndex));
					} else
						break;
				} else
					break;
			}
			var result = F.Call(S.Try, parts, startIndex, parts.Last.Range.EndIndex);
			if (parts.Count == 1)
				Error(result, "'try': At least one 'catch' or 'finally' clause is required");
			return result;
		}
		LNode ExprOrNull(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			la0 = LA0;
			if (InParens_ExprOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(allowUnassignedVarDecl);
				return e;
			} else
				return null;
		}
		LNode ExprOpt(bool allowUnassignedVarDecl = false)
		{
			TokenType la0;
			la0 = LA0;
			if (InParens_ExprOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(allowUnassignedVarDecl);
				return e;
			} else
				return MissingHere();
		}
		static readonly HashSet<int> ExprList_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@operator, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Comma, (int) TT.ContextualKeyword, (int) TT.Dot, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		void ExprList(RWList<LNode> list, bool allowTrailingComma = false, bool allowUnassignedVarDecl = false)
		{
			TokenType la0, la1;
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt(allowUnassignedVarDecl));
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
							goto match3;
					} else if (la0 == EOF)
						break;
					else
						goto match3;
					continue;
				match2:
					{
						Skip();
						list.Add(ExprOpt(allowUnassignedVarDecl));
					}
					continue;
				match3:
					{
						Error("Syntax error in expression list");
						for (;;) {
							la0 = LA0;
							if (!(la0 == EOF || la0 == TT.Comma))
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
			TokenType la0;
			LNode e;
			la0 = LA0;
			if (la0 == TT.LBrace) {
				var lb = MatchAny();
				var rb = Match((int) TT.RBrace);
				var exprs = InitializerListInside(lb).ToRVList();
				e = SetBaseStyle(F.Call(S.Braces, exprs, lb.StartIndex, rb.EndIndex), NodeStyle.OldStyle);
			} else
				e = ExprOpt(false);
			return e;
		}
		void InitializerList(RWList<LNode> list)
		{
			TokenType la0, la1;
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(InitializerExpr());
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
							goto match3;
					} else if (la0 == EOF)
						break;
					else
						goto match3;
					continue;
				match3:
					{
						Error("Syntax error in initializer list");
						for (;;) {
							la0 = LA0;
							if (!(la0 == EOF || la0 == TT.Comma))
								Skip();
							else
								break;
						}
					}
				}
			}
			Skip();
		}
		void StmtList(RWList<LNode> list)
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (la0 != EOF)
					list.Add(Stmt());
				else
					break;
			}
			Skip();
		}
		static readonly HashSet<int> TypeSuffixOpt_Test0_set0 = NewSet((int) TT.@new, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.ContextualKeyword, (int) TT.Forward, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		private bool Try_TypeSuffixOpt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return TypeSuffixOpt_Test0();
		}
		private bool TypeSuffixOpt_Test0()
		{
			if (!TryMatch(TypeSuffixOpt_Test0_set0))
				return false;
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
		static readonly HashSet<int> PrimaryExpr_Test0_set0 = NewSet((int) TT.ContextualKeyword, (int) TT.Id);
		private bool Try_PrimaryExpr_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return PrimaryExpr_Test0();
		}
		private bool PrimaryExpr_Test0()
		{
			if (!Scan_TParams())
				return false;
			if (!TryMatchExcept(PrimaryExpr_Test0_set0))
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
			TokenType la0;
			la0 = LA0;
			if (la0 == TT.Add || la0 == TT.Sub)
				{if (!TryMatch((int) TT.Add, (int) TT.Sub))
					return false;}
			else {
				if (!TryMatch((int) TT.IncDec))
					return false;
				if (!TryMatch((int) TT.LParen))
					return false;
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
			switch (LA0) {
			case TT.@operator:
			case TT.ContextualKeyword:
			case TT.Id:
			case TT.Substitute:
			case TT.TypeKeyword:
				{
					if (!Scan_DataType())
						return false;
					if (!TryMatch((int) TT.AttrKeyword, (int) TT.ContextualKeyword, (int) TT.Id, (int) TT.TypeKeyword))
						return false;
				}
				break;
			case TT.@new:
			case TT.AttrKeyword:
				if (!TryMatch((int) TT.@new, (int) TT.AttrKeyword))
					return false;
				break;
			case TT.@this:
				{
					if (!(_spaceName != S.Def))
						return false;
					if (!TryMatch((int) TT.@this))
						return false;
				}
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
		static readonly HashSet<int> Stmt_Test0_set0 = NewSet((int) TT.At, (int) TT.Comma, (int) TT.Forward, (int) TT.LBrace, (int) TT.LParen, (int) TT.Semicolon, (int) TT.Set);
		private bool Try_Stmt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Stmt_Test0();
		}
		private bool Stmt_Test0()
		{
			if (!Scan_DataType())
				return false;
			if (!Scan_ComplexNameDecl())
				return false;
			if (!TryMatch(Stmt_Test0_set0))
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
			la0 = LA0;
			if (la0 == TT.LParen) {
				if (!TryMatch((int) TT.LParen))
					return false;
				if (!TryMatch((int) TT.RParen))
					return false;
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
