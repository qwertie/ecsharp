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
		Symbol _spaceName;
		LNode AutoRemoveParens(LNode node)
		{
			int i = node.Attrs.IndexWithName(S.TriviaInParens);
			if ((i > -1))
				 return node.WithAttrs(node.Attrs.RemoveAt(i));
			return node;
		}
		int count;
		[Flags] enum Ambiguity
		{
			AllowUnassignedVarDecl = 1, StatementLevel = 2, StopAtArgumentList = 8, ExpectCast = 4, ExpectType = 8, BlankIndexed = 64, TypeSuffix = 128, IsExpr = 256, IsCall = 512, HasAttrs = 1024, IsTuple = 2048, Error = 4096, NotAType = IsExpr | IsCall, Type = BlankIndexed | TypeSuffix
		}
		static readonly int MinPrec = Precedence.MinValue.Lo;
		public static readonly Precedence StartStmt = new Precedence(MinPrec, MinPrec, MinPrec);
		public static readonly Precedence StartExpr = new Precedence(MinPrec + 1, MinPrec + 1, MinPrec + 1);
		public static readonly Precedence ContinueExpr = new Precedence(MinPrec + 2, MinPrec + 2, MinPrec + 2);
		static readonly Symbol _var = GSymbol.Get("var");
		private void MaybeRecognizeVarAsKeyword(ref LNode type)
		{
			SourceRange rng;
			if (type.IsIdNamed(_var) && (rng = type.Range).Source.TryGet(rng.StartIndex, 'v') == 'v') {
				type = F.Id(S.Missing, rng.StartIndex, rng.EndIndex);
			}
		}
		LNode _triviaWordAttribute;
		RWList<LNode> _stmtAttrs = new RWList<LNode>();
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
			var args = ExprListInside(lp);
			return F.Tuple(args.ToRVList(), lp.StartIndex, rp.EndIndex);
		}
		LNode DataType(bool allowDimensions = false)
		{
			var e = ComplexId();
			TypeSuffixOpt(allowDimensions, ref e);
			return e;
		}
		bool Try_Scan_DataType(int lookaheadAmt, bool allowDimensions = false)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DataType(allowDimensions);
		}
		bool Scan_DataType(bool allowDimensions = false)
		{
			if (!Scan_ComplexId())
				return false;
			if (!Scan_TypeSuffixOpt(allowDimensions))
				return false;
			return true;
		}
		LNode ComplexId()
		{
			TokenType la0, la1;
			var e = IdAtom();
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				la1 = LA(1);
				if (la1 == TT.Id || la1 == TT.Substitute || la1 == TT.TypeKeyword) {
					Skip();
					var e2 = IdAtom();
					e = F.Call(S.ColonColon, e, e2, e.Range.StartIndex, e2.Range.EndIndex);
				}
			}
			RestOfId(ref e);
			return e;
		}
		bool Scan_ComplexId()
		{
			TokenType la0, la1;
			if (!Scan_IdAtom())
				return false;
			la0 = LA0;
			if (la0 == TT.ColonColon) {
				la1 = LA(1);
				if (la1 == TT.Id || la1 == TT.Substitute || la1 == TT.TypeKeyword) {
					if (!TryMatch((int) TT.ColonColon))
						return false;
					if (!Scan_IdAtom())
						return false;
				}
			}
			if (!Scan_RestOfId())
				return false;
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
			} else {
				var t = Match((int) TT.Id, (int) TT.TypeKeyword);
				r = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
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
			} else if (!TryMatch((int) TT.Id, (int) TT.TypeKeyword))
				return false;
			return true;
		}
		void RestOfId(ref LNode r)
		{
			TokenType la0, la1;
			la0 = LA0;
			if (la0 == TT.LT) {
				switch (LA(1)) {
				case TT.GT:
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					TParams(ref r);
					break;
				}
			} else if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.LBrack)
					TParams(ref r);
			} else if (la0 == TT.Not) {
				la1 = LA(1);
				if (la1 == TT.LParen)
					TParams(ref r);
			}
			la0 = LA0;
			if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.Id || la1 == TT.Substitute || la1 == TT.TypeKeyword)
					DotRestOfId(ref r);
			}
		}
		bool Try_Scan_RestOfId(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_RestOfId();
		}
		bool Scan_RestOfId()
		{
			TokenType la0, la1;
			do {
				la0 = LA0;
				if (la0 == TT.LT) {
					switch (LA(1)) {
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
			la0 = LA0;
			if (la0 == TT.Dot) {
				la1 = LA(1);
				if (la1 == TT.Id || la1 == TT.Substitute || la1 == TT.TypeKeyword)
					if (!Scan_DotRestOfId())
						return false;
			}
			return true;
		}
		void DotRestOfId(ref LNode r)
		{
			Match((int) TT.Dot);
			var e = IdAtom();
			r = F.Dot(r, e);
			RestOfId(ref r);
		}
		bool Try_Scan_DotRestOfId(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_DotRestOfId();
		}
		bool Scan_DotRestOfId()
		{
			if (!TryMatch((int) TT.Dot))
				return false;
			if (!Scan_IdAtom())
				return false;
			if (!Scan_RestOfId())
				return false;
			return true;
		}
		void TParams(ref LNode r)
		{
			TokenType la0;
			RWList<LNode> list = new RWList<LNode>();
			Token end;
			la0 = LA0;
			if (la0 == TT.LT) {
				Skip();
				la0 = LA0;
				if (la0 == TT.Id || la0 == TT.Substitute || la0 == TT.TypeKeyword) {
					list.Add(ComplexId());
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							Skip();
							list.Add(ComplexId());
						} else
							break;
					}
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
			list.Insert(0, r);
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
				la0 = LA0;
				if (la0 == TT.Id || la0 == TT.Substitute || la0 == TT.TypeKeyword) {
					if (!Scan_ComplexId())
						return false;
					for (;;) {
						la0 = LA0;
						if (la0 == TT.Comma) {
							if (!TryMatch((int) TT.Comma))
								return false;
							if (!Scan_ComplexId())
								return false;
						} else
							break;
					}
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
		bool TypeSuffixOpt(bool allowDimensions, ref LNode e)
		{
			TokenType la0, la1;
			int count;
			bool result = false;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark) {
					Skip();
					e = F.Of(F.Id(S.QuestionMark), e);
					result = true;
				} else if (la0 == TT.Mul) {
					Skip();
					e = F.Of(F.Id(S._Pointer), e);
					result = true;
				} else if (la0 == TT.LBrack) {
					var dims = InternalList<int>.Empty;
					Check((count = CountDims(LT(0), allowDimensions)) > 0, "(count = CountDims(LT($LI), allowDimensions)) > 0");
					Skip();
					Match((int) TT.RBrack);
					dims.Add(count);
					for (;;) {
						la0 = LA0;
						if (la0 == TT.LBrack) {
							la1 = LA(1);
							if (la1 == TT.RBrack) {
								Check((count = CountDims(LT(0), allowDimensions)) > 0, "(count = CountDims(LT($LI), allowDimensions)) > 0");
								Skip();
								Skip();
								dims.Add(count);
							} else
								break;
						} else
							break;
					}
					for (int i = dims.Count - 1; i >= 0; i--)
						e = F.Of(F.Id(S.GetArrayKeyword(dims[i])), e);
					result = true;
				} else
					break;
			}
			return result;
		}
		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt, bool allowDimensions)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt(allowDimensions);
		}
		bool Scan_TypeSuffixOpt(bool allowDimensions)
		{
			TokenType la0, la1;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark)
					{if (!TryMatch((int) TT.QuestionMark))
						return false;}
				else if (la0 == TT.Mul)
					{if (!TryMatch((int) TT.Mul))
						return false;}
				else if (la0 == TT.LBrack) {
					if (!((count = CountDims(LT(0), allowDimensions)) > 0))
						return false;
					if (!TryMatch((int) TT.LBrack))
						return false;
					if (!TryMatch((int) TT.RBrack))
						return false;
					for (;;) {
						la0 = LA0;
						if (la0 == TT.LBrack) {
							la1 = LA(1);
							if (la1 == TT.RBrack) {
								if (!((count = CountDims(LT(0), allowDimensions)) > 0))
									return false;
								if (!TryMatch((int) TT.LBrack))
									return false;
								if (!TryMatch((int) TT.RBrack))
									return false;
							} else
								break;
						} else
							break;
					}
				} else
					break;
			}
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
			case TT.@base:
			case TT.@this:
			case TT.Id:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					r = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
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
			case TT.At:
				{
					var at = MatchAny();
					var t = Match((int) TT.LBrack);
					var rb = Match((int) TT.RBrack);
					r = F.Literal(t.Children, at.StartIndex, rb.EndIndex);
					r = F.Literal(t.Children, t.StartIndex, rb.EndIndex);
				}
				break;
			case TT.@new:
				{
					LNode type = F._Missing;
					Skip();
					switch (LA0) {
					case TT.LBrack:
						{
							Check(LT(0).Count == 0, "LT($LI).Count == 0");
							var lb = MatchAny();
							var rb = Match((int) TT.RBrack);
							type = F.Id(S.Bracks, lb.StartIndex, rb.EndIndex);
						}
						break;
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						type = DataType(true);
						break;
					}
					var list = new RWList<LNode>();
					la0 = LA0;
					if (la0 == TT.LParen) {
						var lp = MatchAny();
						Match((int) TT.RParen);
						list.Add(type.WithArgs(ExprListInside(lp).ToRVList()));
						la0 = LA0;
						if (la0 == TT.LBrace) {
							la1 = LA(1);
							if (la1 == TT.RBrace) {
								var lb = MatchAny();
								Skip();
								AppendExprsInside(lb, list);
							}
						}
					} else {
						list.Add(type.WithArgs());
						var lb = Match((int) TT.LBrace);
						Match((int) TT.RBrace);
						AppendStmtsInside(lb, list);
					}
					r = F.Call(S.New, list.ToRVList());
				}
				break;
			case TT.@default:
			case TT.@checked:
			case TT.@sizeof:
			case TT.@typeof:
			case TT.@unchecked:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					r = F.Call((Symbol) t.Value, ExprListInside(args), t.StartIndex, rp.EndIndex);
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
					r = Error("Invalid expression");
					for (;;) {
						la0 = LA0;
						if (!(la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon))
							Skip();
						else
							break;
					}
				}
				break;
			}
			return r;
		}
		static readonly HashSet<int> Scan_Atom_set0 = NewSet((int) TT.Number, (int) TT.OtherLit, (int) TT.SQString, (int) TT.String, (int) TT.Symbol);
		static readonly HashSet<int> Scan_Atom_set1 = NewSet((int) TT.@default, (int) TT.@checked, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked);
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
			case TT.@base:
			case TT.@this:
			case TT.Id:
			case TT.TypeKeyword:
				if (!TryMatch((int) TT.@base, (int) TT.@this, (int) TT.Id, (int) TT.TypeKeyword))
					return false;
				break;
			case TT.Number:
			case TT.OtherLit:
			case TT.SQString:
			case TT.String:
			case TT.Symbol:
				if (!TryMatch(Scan_Atom_set0))
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
			case TT.@new:
				{
					if (!TryMatch((int) TT.@new))
						return false;
					switch (LA0) {
					case TT.LBrack:
						{
							if (!(LT(0).Count == 0))
								return false;
							if (!TryMatch((int) TT.LBrack))
								return false;
							if (!TryMatch((int) TT.RBrack))
								return false;
						}
						break;
					case TT.Id:
					case TT.Substitute:
					case TT.TypeKeyword:
						if (!Scan_DataType(true))
							return false;
						break;
					}
					la0 = LA0;
					if (la0 == TT.LParen) {
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
					} else {
						if (!TryMatch((int) TT.LBrace))
							return false;
						if (!TryMatch((int) TT.RBrace))
							return false;
					}
				}
				break;
			case TT.@default:
			case TT.@checked:
			case TT.@sizeof:
			case TT.@typeof:
			case TT.@unchecked:
				{
					if (!TryMatch(Scan_Atom_set1))
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
						if (!(la0 == EOF || la0 == TT.Comma || la0 == TT.Semicolon))
							{if (!TryMatchExcept((int) EOF, (int) TT.Comma, (int) TT.Semicolon))
								return false;}
						else
							break;
					}
				}
				break;
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
		LNode PrimaryExpr()
		{
			var e = Atom();
			for (;;) {
				switch (LA0) {
				case TT.Dot:
					{
						if (Try_PrimaryExpr_Test0(0)) {
							switch (LA(1)) {
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
							case TT.Dot:
							case TT.Id:
							case TT.LBrace:
							case TT.LParen:
							case TT.Number:
							case TT.OtherLit:
							case TT.SQString:
							case TT.String:
							case TT.Substitute:
							case TT.Symbol:
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
							e = F.Call(e, ExprListInside(lp), e.Range.StartIndex, rp.EndIndex);
						}
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
			var lp = MatchAny();
			var rp = Match((int) TT.RParen);
			Down(lp);
			var kind = Match((int) TT.@as, (int) TT.@using, (int) TT.PtrArrow);
			var type = ExprStart(false);
			Match((int) EOF);
			return Up(F.Call(kind.Value == S.PtrArrow ? S.Cast : ((Symbol) kind.Value), e, type, e.Range.StartIndex, rp.EndIndex));
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
		static readonly HashSet<int> PrefixExpr_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.Dot, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		LNode PrefixExpr()
		{
			TokenType la2;
			do {
				switch (LA0) {
				case TT.Add:
				case TT.AndBits:
				case TT.IncDec:
				case TT.Mul:
				case TT.Not:
				case TT.NotBits:
				case TT.Sub:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						return F.Call((Symbol) op.Value, e, op.StartIndex, e.Range.EndIndex);
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
									return F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex);
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
						return F.Call(S._Dereference, F.Call(S._Dereference, e, op.StartIndex + 1, e.Range.EndIndex), op.StartIndex, e.Range.EndIndex);
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
				case TT.@as:
				case TT.@in:
				case TT.@is:
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
				case TT.@using:
					{
						la0 = LA0;
						if (context.CanParse(prec = InfixPrecedenceOf(la0))) {
							la1 = LA(1);
							if (PrefixExpr_set0.Contains((int) la1)) {
								var op = MatchAny();
								var rhs = Expr(prec);
								e = F.Call(S.UsingCast, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
								e.BaseStyle = NodeStyle.Operator;
							} else
								goto stop;
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
								e = F.Call(S.QuestionMark, e, then, @else, e.Range.StartIndex, @else.Range.EndIndex);
								e.BaseStyle = NodeStyle.Operator;
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
					e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
					e.BaseStyle = NodeStyle.Operator;
				}
				continue;
			match3:
				{
					la0 = LA0;
					if (la0 == TT.LT) {
						Skip();
						Match((int) TT.LT);
						var rhs = Expr(EP.Shift);
						e = F.Call(S.Shl, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
					} else {
						Match((int) TT.GT);
						Match((int) TT.GT);
						var rhs = Expr(EP.Shift);
						e = F.Call(S.Shr, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
					}
					e.BaseStyle = NodeStyle.Operator;
				}
			}
		 stop:;
			return e;
		}
		public LNode ExprStart(bool allowUnassignedVarDecl)
		{
			TokenType la0;
			LNode e;
			RWList<LNode> attrs = null;
			NormalAttributes(ref attrs);
			la0 = LA0;
			if (la0 == TT.Id || la0 == TT.Substitute || la0 == TT.TypeKeyword) {
				if (Try_Scan_DetectVarDecl(0, allowUnassignedVarDecl))
					e = VarDeclExpr();
				else
					e = Expr(ContinueExpr);
			} else
				e = Expr(ContinueExpr);
			if ((attrs != null))
				e = e.PlusAttrs(attrs.ToRVList());
			return e;
		}
		void DetectVarDecl(bool allowUnassigned)
		{
			TokenType la0;
			VarDeclStart();
			la0 = LA0;
			if (la0 == TT.Set)
				Skip();
			else
				Check(allowUnassigned, "allowUnassigned");
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
			if (la0 == TT.Set)
				{if (!TryMatch((int) TT.Set))
					return false;}
			else if (!allowUnassigned)
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
			var e = DataType(false);
			var name = Match((int) TT.Id);
			MaybeRecognizeVarAsKeyword(ref e);
			return Pair.Create(e, F.Id((Symbol) name.Value, name.StartIndex, name.EndIndex));
		}
		bool Scan_VarDeclStart()
		{
			if (!Scan_DataType(false))
				return false;
			if (!TryMatch((int) TT.Id))
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
			return Up(ExprInParensOrTuple(allowUnassignedVarDecl, lp.StartIndex, rp.EndIndex));
		}
		bool Scan_ExprInParens(bool allowUnassignedVarDecl)
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}
		static readonly HashSet<int> ExprInParensOrTuple_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Dot, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		LNode ExprInParensOrTuple(bool allowUnassignedVarDecl, int startIndex, int endIndex)
		{
			TokenType la0, la1;
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
						if (ExprInParensOrTuple_set0.Contains((int) la1)) {
							Skip();
							list.Add(ExprStart(allowUnassignedVarDecl));
						} else
							Skip();
					} else
						break;
				}
				return F.Tuple(list, startIndex, endIndex);
			}
			Match((int) EOF);
			return F.InParens(e, startIndex, endIndex);
		}
		LNode BracedBlock()
		{
			var t = Match((int) TT.LBrace);
			Match((int) TT.RBrace);
			return F.Braces(StmtListInside(t).ToRVList());
		}
		bool Scan_BracedBlock()
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
					la1 = LA(1);
					if (la1 == TT.RBrack) {
						var t = MatchAny();
						Skip();
						AppendExprsInside(t, attrs = attrs ?? new RWList<LNode>());
					} else
						break;
				} else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					attrs = attrs ?? new RWList<LNode>();
					attrs.Add(F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex));
				} else
					break;
			}
		}
		void WordAttributes(RWList<LNode> attrs)
		{
			TokenType la0;
			TokenType la1_ = LA(1);
			if (LA0 == TT.Id && (la1_ == TT.Set || la1_ == TT.LParen || la1_ == TT.Dot))
				return;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					var t = MatchAny();
					attrs.Add(F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex));
				} else if (la0 == TT.@new || la0 == TT.Id) {
					if (Try_WordAttributes_Test0(1)) {
						var t = MatchAny();
						_triviaWordAttribute = _triviaWordAttribute ?? F.Id(S.TriviaWordAttribute);
						attrs.Add(F.Attr(_triviaWordAttribute, F.Id("#" + t.Value.ToString(), t.StartIndex, t.EndIndex)));
					} else
						break;
				} else
					break;
			}
		}
		static readonly HashSet<int> Stmt_set0 = NewSet((int) EOF, (int) TT.@as, (int) TT.@in, (int) TT.@is, (int) TT.@using, (int) TT.Add, (int) TT.And, (int) TT.AndBits, (int) TT.BQString, (int) TT.CompoundSet, (int) TT.DivMod, (int) TT.DotDot, (int) TT.EqNeq, (int) TT.GT, (int) TT.IncDec, (int) TT.LambdaArrow, (int) TT.LEGE, (int) TT.NotBits, (int) TT.NullCoalesce, (int) TT.NullDot, (int) TT.OrBits, (int) TT.OrXor, (int) TT.Power, (int) TT.PtrArrow, (int) TT.QuickBind, (int) TT.Semicolon, (int) TT.Set, (int) TT.Sub, (int) TT.XorBits);
		static readonly HashSet<int> Stmt_set1 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@delegate, (int) TT.@new, (int) TT.@sizeof, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.At, (int) TT.ColonColon, (int) TT.Dot, (int) TT.Id, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.LT, (int) TT.Mul, (int) TT.Not, (int) TT.Number, (int) TT.OtherLit, (int) TT.QuestionMark, (int) TT.SQString, (int) TT.String, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		public LNode Stmt()
		{
			TokenType la0, la1;
			_stmtAttrs.Clear();
			NormalAttributes(ref _stmtAttrs);
			WordAttributes(_stmtAttrs);
			var attrs = _stmtAttrs.ToRVList();
			LNode r;
			do {
				switch (LA0) {
				case TT.@class:
				case TT.@enum:
				case TT.@interface:
				case TT.@namespace:
				case TT.@struct:
					r = SpaceDecl(attrs);
					break;
				case TT.LBrack:
					r = AssemblyOrModuleAttribute(attrs);
					break;
				case TT.@event:
					r = EventDecl(attrs);
					break;
				case TT.@delegate:
					{
						switch (LA(1)) {
						case TT.Id:
						case TT.Substitute:
						case TT.TypeKeyword:
							r = DelegateDecl(attrs);
							break;
						case TT.LParen:
							goto match6;
						default:
							goto match8;
						}
					}
					break;
				case TT.Id:
				case TT.Substitute:
				case TT.TypeKeyword:
					{
						if (Try_Stmt_Test0(0)) {
							la1 = LA(1);
							if (Stmt_set1.Contains((int) la1))
								r = MethodOrPropertyOrVar(attrs);
							else if (Stmt_set0.Contains((int) la1))
								goto match6;
							else
								goto match8;
						} else
							goto match6;
					}
					break;
				case TT.@base:
				case TT.@default:
				case TT.@this:
				case TT.@checked:
				case TT.@new:
				case TT.@sizeof:
				case TT.@typeof:
				case TT.@unchecked:
				case TT.Add:
				case TT.AndBits:
				case TT.At:
				case TT.Dot:
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
				case TT.Symbol:
					goto match6;
				case TT.Semicolon:
					{
						var t = MatchAny();
						r = F.Id(S.Missing, t.StartIndex, t.EndIndex);
					}
					break;
				default:
					goto match8;
				}
				break;
			match6:
				{
					r = Expr(ContinueExpr);
					la0 = LA0;
					if (la0 == EOF) {
						Skip();
						r = F.Call(S.Result, r, r.Range.StartIndex, r.Range.EndIndex);
					} else
						Match((int) TT.Semicolon);
					r = r.WithAttrs(attrs);
				}
				break;
			match8:
				{
					ScanToEndOfStmt();
					r = Error("Syntax error: statement expected");
				}
			} while (false);
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
		void WhereClausesOpt(ref LNode name)
		{
		}
		void WhereClause()
		{
			Check(Is(0, _where), "Is($LI, _where)");
			Match((int) TT.Id);
		}
		LNode SpaceDecl(RVList<LNode> attrs)
		{
			TokenType la0;
			var t = MatchAny();
			var kind = (Symbol) t.Value;
			var name = ComplexId();
			var bases = BaseListOpt();
			WhereClausesOpt(ref name);
			la0 = LA0;
			if (la0 == TT.Semicolon) {
				var end = MatchAny();
				return F.Call(kind, name, bases, t.StartIndex, end.EndIndex).WithAttrs(attrs);
			} else {
				var body = BracedBlock();
				return F.Call(kind, name, bases, body, t.StartIndex, body.Range.EndIndex).WithAttrs(attrs);
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
				return F.List(bases);
			} else
				return F.List();
		}
		Token AsmOrModLabel()
		{
			Check(LT(0).Value == _assembly || LT(0).Value == _module, "LT($LI).Value == _assembly || LT($LI).Value == _module");
			var t = Match((int) TT.Id);
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
			if (!TryMatch((int) TT.Id))
				return false;
			if (!TryMatch((int) TT.Colon))
				return false;
			return true;
		}
		LNode AssemblyOrModuleAttribute(RVList<LNode> attrs)
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var lb = MatchAny();
			var rb = Match((int) TT.RBrack);
			Down(lb);
			var kind = AsmOrModLabel();
			var list = new RWList<LNode>();
			ExprList(list);
			Up();
			var r = F.Call(kind.Value == _module ? S.Module : S.Assembly, list.ToRVList(), lb.StartIndex, rb.EndIndex);
			return r.WithAttrs(attrs);
		}
		LNode MethodOrPropertyOrVar(RVList<LNode> attrs)
		{
			TokenType la0, la1;
			LNode r;
			var type = DataType();
			do {
				la0 = LA0;
				if (la0 == TT.Id) {
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
						r = F.Call(S.Var, parts, type.Range.StartIndex, end.EndIndex);
					} else
						goto match2;
				} else
					goto match2;
				break;
			match2:
				{
					var name = ComplexId();
					var lp = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					la0 = LA0;
					if (la0 == TT.LBrace) {
						var body = BracedBlock();
						r = F.Def(type, name, ArgTuple(lp, rp), body, type.Range.StartIndex, body.Range.EndIndex);
					} else {
						var end = Match((int) TT.Semicolon);
						r = F.Def(type, name, ArgTuple(lp, rp), null, type.Range.StartIndex, end.EndIndex);
					}
				}
			} while (false);
			return r.WithAttrs(attrs);
		}
		LNode NameAndMaybeInit(bool isArray)
		{
			TokenType la0;
			var name = Match((int) TT.Id);
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
							var initializers = StmtListInside(lb);
							initializers.Insert(0, F.Call(S.Bracks, lb.StartIndex, lb.StartIndex));
							var init = F.Call(S.New, initializers.ToRVList(), lb.StartIndex, rb.EndIndex);
							r = F.Call(S.Set, r, init, name.StartIndex, rb.EndIndex);
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
		LNode EventDecl(RVList<LNode> attrs)
		{
			TokenType la0;
			LNode r;
			var k = MatchAny();
			var type = DataType();
			var name = ComplexId();
			la0 = LA0;
			if (la0 == TT.LBrace) {
				var body = BracedBlock();
				r = F.Call(S.Event, type, name, body, k.StartIndex, body.Range.EndIndex);
			} else {
				var end = Match((int) TT.Semicolon);
				r = F.Call(S.Event, type, name, k.StartIndex, end.EndIndex);
			}
			return r.WithAttrs(attrs);
		}
		LNode DelegateDecl(RVList<LNode> attrs)
		{
			var k = MatchAny();
			var type = DataType();
			var name = ComplexId();
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			var end = Match((int) TT.Semicolon);
			var r = F.Call(S.Delegate, type, name, ArgTuple(lp, rp), k.StartIndex, end.EndIndex);
			return r.WithAttrs(attrs);
		}
		LNode ExprOpt()
		{
			TokenType la0;
			la0 = LA0;
			if (ExprInParensOrTuple_set0.Contains((int) la0)) {
				var e = ExprStart(false);
				return e;
			} else {
				var i = GetTextPosition(InputPosition);
				return F.Id(S.Missing, i, i);
			}
		}
		void ExprList(RWList<LNode> list)
		{
			TokenType la0;
			la0 = LA0;
			if (la0 == EOF)
				;
			else {
				list.Add(ExprOpt());
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						Skip();
						list.Add(ExprOpt());
					} else if (la0 == EOF)
						break;
					else {
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
		static readonly HashSet<int> StmtList_set0 = NewSet((int) TT.@base, (int) TT.@default, (int) TT.@this, (int) TT.@checked, (int) TT.@class, (int) TT.@delegate, (int) TT.@enum, (int) TT.@event, (int) TT.@interface, (int) TT.@namespace, (int) TT.@new, (int) TT.@sizeof, (int) TT.@struct, (int) TT.@typeof, (int) TT.@unchecked, (int) TT.Add, (int) TT.AndBits, (int) TT.At, (int) TT.AttrKeyword, (int) TT.Dot, (int) TT.Id, (int) TT.IncDec, (int) TT.LBrace, (int) TT.LBrack, (int) TT.LParen, (int) TT.Mul, (int) TT.Not, (int) TT.NotBits, (int) TT.Number, (int) TT.OtherLit, (int) TT.Power, (int) TT.Semicolon, (int) TT.SQString, (int) TT.String, (int) TT.Sub, (int) TT.Substitute, (int) TT.Symbol, (int) TT.TypeKeyword);
		void StmtList(RWList<LNode> list)
		{
			TokenType la0;
			for (;;) {
				la0 = LA0;
				if (StmtList_set0.Contains((int) la0))
					list.Add(Stmt());
				else
					break;
			}
			Match((int) EOF);
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
		static readonly HashSet<int> PrimaryExpr_Test0_set0 = NewSet((int) TT.Id);
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
		static readonly HashSet<int> WordAttributes_Test0_set0 = NewSet((int) TT.@break, (int) TT.@continue, (int) TT.@return, (int) TT.@throw, (int) TT.@case, (int) TT.@class, (int) TT.@delegate, (int) TT.@do, (int) TT.@enum, (int) TT.@event, (int) TT.@fixed, (int) TT.@for, (int) TT.@foreach, (int) TT.@goto, (int) TT.@interface, (int) TT.@lock, (int) TT.@namespace, (int) TT.@struct, (int) TT.@switch, (int) TT.@try, (int) TT.@using, (int) TT.@while);
		private bool Try_WordAttributes_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return WordAttributes_Test0();
		}
		private bool WordAttributes_Test0()
		{
			TokenType la0;
			do {
				switch (LA0) {
				case TT.Id:
					{
						switch (LA(1)) {
						case TT.ColonColon:
						case TT.Dot:
						case TT.Id:
						case TT.LBrack:
						case TT.LT:
						case TT.Mul:
						case TT.Not:
						case TT.QuestionMark:
							goto match1;
						default:
							goto match2;
						}
					}
				case TT.Substitute:
				case TT.TypeKeyword:
					goto match1;
				case TT.@new:
				case TT.AttrKeyword:
					goto match2;
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
				default:
					if (!TryMatch(WordAttributes_Test0_set0))
						return false;
					break;
				}
				break;
			match1:
				{
					if (!Scan_DataType())
						return false;
					if (!TryMatch((int) TT.Id))
						return false;
				}
				break;
			match2:
				{
					la0 = LA0;
					if (la0 == TT.Id)
						if (!TryMatch((int) TT.Id))
							return false;
					if (!TryMatch((int) TT.@new, (int) TT.AttrKeyword))
						return false;
				}
			} while (false);
			return true;
		}
		private bool Try_Stmt_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Stmt_Test0();
		}
		private bool Stmt_Test0()
		{
			if (!Scan_DataType())
				return false;
			if (!Scan_ComplexId())
				return false;
			return true;
		}
	}
}
