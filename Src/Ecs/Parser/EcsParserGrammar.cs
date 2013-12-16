// Generated from EcsParserGrammar.les by LLLPG custom tool. LLLPG version: 0.9.2.0
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
		RWList<LNode> _attrs = new RWList<LNode>();
		bool Is(int li, Symbol value)
		{
			return LT(li).Value == value;
		}
		LNode DataType(bool allowDimensions = false)
		{
			var e = ComplexId();
			TypeSuffixOpt(ref e, allowDimensions);
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
			if (!Scan_TypeSuffixOpt())
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
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.GT:
				case TT.LParen:
				case TT.Id:
				case TT.TypeKeyword:
				case TT.Substitute:
				case TT.LBrack:
					TParams(ref r);
					break;
				}
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
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				switch (LA(1)) {
				case TT.GT:
				case TT.LParen:
				case TT.Id:
				case TT.TypeKeyword:
				case TT.Substitute:
				case TT.LBrack:
					if (!Scan_TParams())
						return false;
					break;
				}
			}
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
		bool TypeSuffixOpt(ref LNode e, bool allowDimensions)
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
					Check((count = CountDims(LT(0))) > 0, "(count = CountDims(LT($LI))) > 0");
					Skip();
					Match((int) TT.RBrack);
					dims.Add(count);
					for (;;) {
						la0 = LA0;
						if (la0 == TT.LBrack) {
							la1 = LA(1);
							if (la1 == TT.RBrack) {
								Check((count = CountDims(LT(0))) > 0, "(count = CountDims(LT($LI))) > 0");
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
		bool Try_Scan_TypeSuffixOpt(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return Scan_TypeSuffixOpt();
		}
		bool Scan_TypeSuffixOpt()
		{
			TokenType la0, la1;
			for (;;) {
				la0 = LA0;
				if (la0 == TT.QuestionMark)
					{if (!TryMatch((int) TT.QuestionMark))
						return false;}
				else if (la0 == TT.Mul)if (!TryMatch((int) TT.Mul))
					return false;
				else if (la0 == TT.LBrack) {
					if (!((count = CountDims(LT(0))) > 0))
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
								if (!((count = CountDims(LT(0))) > 0))
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
					r = F.Call(S.Substitute, e, t.StartIndex, e.Range.EndIndex);
				}
				break;
			case TT.@this:
			case TT.Id:
			case TT.TypeKeyword:
				{
					var t = MatchAny();
					r = F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex);
				}
				break;
			case TT.LParen:
				r = ExprInParens();
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
					case TT.TypeKeyword:
					case TT.Substitute:
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
			case TT.@typeof:
			case TT.@checked:
			case TT.@unchecked:
			case TT.@sizeof:
			case TT.@default:
				{
					var t = MatchAny();
					var args = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					r = F.Call((Symbol) t.Value, ExprListInside(args), t.StartIndex, rp.EndIndex);
				}
				break;
			default:
				{
					var t = Match((int) TT.@delegate);
					var args = Match((int) TT.LParen);
					Match((int) TT.RParen);
					var block = Match((int) TT.LBrace);
					var rb = Match((int) TT.RBrace);
					r = F.Call(S.Lambda, F.Tuple(ExprListInside(args).ToRVList()), F.Braces(StmtListInside(block).ToRVList(), block.StartIndex, rb.EndIndex), t.StartIndex, rb.EndIndex);
				}
				break;
			}
			return r;
		}
		static readonly HashSet<int> Scan_Atom_set0 = NewSet(TT.@default, TT.@checked, TT.@sizeof, TT.@typeof, TT.@unchecked);
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
			case TT.@this:
			case TT.Id:
			case TT.TypeKeyword:
				if (!TryMatch((int) TT.@this, (int) TT.Id, (int) TT.TypeKeyword))
					return false;
				break;
			case TT.LParen:
				if (!Scan_ExprInParens())
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
					case TT.TypeKeyword:
					case TT.Substitute:
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
			case TT.@typeof:
			case TT.@checked:
			case TT.@unchecked:
			case TT.@sizeof:
			case TT.@default:
				{
					if (!TryMatch(Scan_Atom_set0))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
				break;
			default:
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
			}
			return true;
		}
		LNode PrimaryExpr()
		{
			var e = Atom();
			for (;;) {
				switch (LA0) {
				case TT.Dot:
				case TT.PtrArrow:
				case TT.ColonColon:
				case TT.QuickBind:
					{
						var op = MatchAny();
						var rhs = Atom();
						e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
					}
					break;
				case TT.LParen:
					{
						if (Down(0) && Up(LA0 == TT.@as || LA0 == TT.@using || LA0 == TT.PtrArrow)) {
							if (true)
								e = PrimaryExpr_NewStyleCast(e);
							else
								goto match3;
						} else
							goto match3;
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
						e = F.Call((Symbol) t.Value, e, e.Range.StartIndex, t.EndIndex);
					}
					break;
				default:
					goto stop;
				}
				continue;
			match3:
				{
					var lp = MatchAny();
					var rp = Match((int) TT.RParen);
					e = F.Call(e, ExprListInside(lp), e.Range.StartIndex, rp.EndIndex);
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
			var type = ExprStart();
			Match();
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
		LNode PrefixExpr()
		{
			do {
				switch (LA0) {
				case TT.Not:
				case TT.NotBits:
				case TT.Add:
				case TT.Sub:
				case TT.IncDec:
					{
						var op = MatchAny();
						var e = PrefixExpr();
						return F.Call((Symbol) op.Value, e, op.StartIndex, e.Range.EndIndex);
					}
					break;
				case TT.LParen:
					{
						if (Down(0) && Up(Scan_DataType() && LA0 == EOF)) {
							switch (LA(2)) {
							case TT.Dot:
							case TT.NotBits:
							case TT.LParen:
							case TT.Sub:
							case TT.Add:
							case TT.IncDec:
								{
									if (!Try_PrefixExpr_Test0(2))
										goto match2;
									else
										goto match3;
								}
							case TT.Substitute:
							case TT.Not:
							case TT.LBrace:
							case TT.@this:
							case TT.@new:
							case TT.@typeof:
							case TT.@checked:
							case TT.At:
							case TT.@unchecked:
							case TT.@sizeof:
							case TT.@delegate:
							case TT.TypeKeyword:
							case TT.Id:
							case TT.@default:
								goto match2;
							default:
								goto match3;
							}
						} else
							goto match3;
					}
				default:
					goto match3;
				}
				break;
			match2:
				{
					var lp = MatchAny();
					Match((int) TT.RParen);
					Check(!Try_PrefixExpr_Test0(0), "!(((TT.Add|TT.Sub) | TT.IncDec TT.LParen))");
					var e = PrefixExpr();
					Down(lp);
					return F.Call(S.Cast, e, Up(DataType()), lp.StartIndex, e.Range.EndIndex);
				}
				break;
			match3:
				{
					var e = NullDotExpr();
					return e;
				}
			} while (false);
		}
		static readonly HashSet<int> Expr_set0 = NewSet(TT.@default, TT.@this, TT.@as, TT.@checked, TT.@delegate, TT.@in, TT.@is, TT.@new, TT.@sizeof, TT.@typeof, TT.@unchecked, TT.@using, TT.Add, TT.AndBits, TT.AndOr, TT.At, TT.BQString, TT.Colon, TT.ColonColon, TT.CompoundSet, TT.Div, TT.Dot, TT.DotDot, TT.EqNeq, TT.GT, TT.Id, TT.IncDec, TT.LambdaArrow, TT.LBrace, TT.LBrack, TT.LEGE, TT.LParen, TT.LT, TT.Mul, TT.Not, TT.NotBits, TT.NullCoalesce, TT.NullDot, TT.OrXorBits, TT.Power, TT.PtrArrow, TT.QuestionMark, TT.QuickBind, TT.RBrace, TT.RParen, TT.Set, TT.Sub, TT.Substitute, TT.TypeKeyword);
		LNode Expr(Precedence context)
		{
			TokenType la2;
			Debug.Assert(context.CanParse(EP.Prefix));
			Precedence prec;
			var e = PrefixExpr();
			for (;;) {
				switch (LA0) {
				case TT.AndOr:
				case TT.LambdaArrow:
				case TT.AndBits:
				case TT.OrXorBits:
				case TT.NotBits:
				case TT.LEGE:
				case TT.DotDot:
				case TT.BQString:
				case TT.Div:
				case TT.Power:
				case TT.Add:
				case TT.@is:
				case TT.Set:
				case TT.EqNeq:
				case TT.@in:
				case TT.CompoundSet:
				case TT.LT:
				case TT.@as:
				case TT.GT:
				case TT.Mul:
				case TT.NullCoalesce:
				case TT.@using:
				case TT.Sub:
					{
						switch (LA(1)) {
						case TT.Substitute:
						case TT.Not:
						case TT.Dot:
						case TT.@this:
						case TT.NotBits:
						case TT.@new:
						case TT.@typeof:
						case TT.@checked:
						case TT.At:
						case TT.@unchecked:
						case TT.@sizeof:
						case TT.LParen:
						case TT.@delegate:
						case TT.TypeKeyword:
						case TT.Sub:
						case TT.Id:
						case TT.@default:
						case TT.Add:
						case TT.LBrace:
						case TT.IncDec:
							{
								Check(context.CanParse(prec = InfixPrecedenceOf(LA0)), "context.CanParse(prec = InfixPrecedenceOf($LA))");
								var op = MatchAny();
								var rhs = Expr(prec);
								e = F.Call((Symbol) op.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex);
								e.BaseStyle = NodeStyle.Operator;
							}
							break;
						default:
							goto stop;
						}
					}
					break;
				case TT.QuestionMark:
					{
						switch (LA(1)) {
						case TT.Substitute:
						case TT.Not:
						case TT.Dot:
						case TT.@this:
						case TT.NotBits:
						case TT.@new:
						case TT.@typeof:
						case TT.@checked:
						case TT.At:
						case TT.@unchecked:
						case TT.@sizeof:
						case TT.LParen:
						case TT.@delegate:
						case TT.TypeKeyword:
						case TT.Sub:
						case TT.Id:
						case TT.@default:
						case TT.Add:
						case TT.LBrace:
						case TT.IncDec:
							{
								la2 = LA(2);
								if (Expr_set0.Contains(la2)) {
									Check(context.CanParse(EP.IfElse), "context.CanParse(EP.IfElse)");
									Skip();
									var then = Expr(ContinueExpr);
									Match((int) TT.Colon);
									var @else = Expr(EP.IfElse);
									e = F.Call(S.QuestionMark, e, then, @else, e.Range.StartIndex, @else.Range.EndIndex);
									e.BaseStyle = NodeStyle.Operator;
								} else
									goto stop;
							}
							break;
						default:
							goto stop;
						}
					}
					break;
				default:
					goto stop;
				}
			}
		 stop:;
			return e;
		}
		LNode ExprStart()
		{
			var e = Expr(ContinueExpr);
			return e;
		}
		void NormalAttributes()
		{
			TokenType la0;
			_attrs.Clear();
			for (;;) {
				la0 = LA0;
				if (la0 == TT.LBrack) {
					var t = MatchAny();
					Match((int) TT.RBrack);
					AppendExprsInside(t, _attrs);
				} else
					break;
			}
			for (;;) {
				la0 = LA0;
				if (la0 == TT.AttrKeyword) {
					switch (LA(1)) {
					case TT.AttrKeyword:
					case TT.@new:
					case TT.Id:
					case EOF:
						{
							var t = MatchAny();
							_attrs.Add(F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex));
						}
						break;
					default:
						goto stop;
					}
				} else
					goto stop;
			}
		 stop:;
		}
		int WordAttributes()
		{
			TokenType la0;
			int count = 0;
			la0 = LA0;
			if (la0 == TT.AttrKeyword) {
				var t = MatchAny();
				_attrs.Add(F.Id((Symbol) t.Value, t.StartIndex, t.EndIndex));
			} else {
				Check(!!LT(0).Value.ToString().StartsWith("#"), "!(!LT($LI).Value.ToString().StartsWith(\"#\"))");
				var t = Match((int) TT.@new, (int) TT.Id);
				Check(Try_WordAttributes_Test0(0), "(DataType TT.Id | &{_spaceName != S.Def} #.(TT, noMacro(@this)) | TT.@checked TT.LBrace TT.RBrace | TT.@unchecked TT.LBrace TT.RBrace | #.(TT, noMacro(@default)) TT.Colon | (TT.@fixed|TT.@try|#.(TT, noMacro(@throw))|TT.@foreach|TT.@while|TT.@do|TT.@switch|TT.@class|#.(TT, noMacro(@break))|TT.@interface|TT.@goto|TT.@event|TT.@using|TT.@for|#.(TT, noMacro(@return))|TT.@struct|TT.@delegate|#.(TT, noMacro(@continue))|TT.@enum|TT.@case|TT.@namespace|TT.@lock))");
				count++;
				_attrs.Add(F.Id("#" + t.Value.ToString(), t.StartIndex, t.EndIndex));
			}
			return count;
		}
		public LNode Stmt()
		{
			NormalAttributes();
			WordAttributes();
			var attrs = _attrs.ToRVList();
			LNode r;
			r = SpaceDecl(attrs);
			r = ExprStart();
			r = r.WithAttrs(attrs);
			return r;
		}
		void AsmOrModLabel()
		{
			Check(LT(0).Value == _assembly || LT(0).Value == _module, "LT($LI).Value == _assembly || LT($LI).Value == _module");
			Match((int) TT.Id);
			Match((int) TT.Colon);
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
		RWList<LNode> AssemblyOrModuleAttribute()
		{
			Check(Down(0) && Up(Try_Scan_AsmOrModLabel(0)), "Down($LI) && Up(Try_Scan_AsmOrModLabel(0))");
			var t = Match((int) TT.LBrack);
			Match((int) TT.RBrack);
			Down(t);
			AsmOrModLabel();
			var L = new RWList<LNode>();
			ExprList(ref L);
			return Up(L);
		}
		static readonly HashSet<int> SpaceDecl_set0 = NewSet(TT.@class, TT.@enum, TT.@interface, TT.@namespace, TT.@struct);
		LNode SpaceDecl(RVList<LNode> attrs)
		{
			TokenType la0;
			var t = Match(SpaceDecl_set0);
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
		LNode ExprInParens()
		{
			var lp = Match((int) TT.LParen);
			var rp = Match((int) TT.RParen);
			Down(lp);
			return Up(ExprInParensOrTuple(lp.StartIndex, rp.EndIndex));
		}
		bool Scan_ExprInParens()
		{
			if (!TryMatch((int) TT.LParen))
				return false;
			if (!TryMatch((int) TT.RParen))
				return false;
			return true;
		}
		LNode ExprInParensOrTuple(int startIndex, int endIndex)
		{
			TokenType la0;
			var e = ExprStart();
			la0 = LA0;
			if (la0 == TT.Comma) {
				var list = new RVList<LNode> { 
					e
				};
				for (;;) {
					la0 = LA0;
					if (la0 == TT.Comma) {
						switch (LA(1)) {
						case TT.Substitute:
						case TT.Not:
						case TT.Dot:
						case TT.@this:
						case TT.NotBits:
						case TT.@new:
						case TT.@typeof:
						case TT.@checked:
						case TT.At:
						case TT.@unchecked:
						case TT.@sizeof:
						case TT.LParen:
						case TT.@delegate:
						case TT.TypeKeyword:
						case TT.Sub:
						case TT.Id:
						case TT.@default:
						case TT.Add:
						case TT.LBrace:
						case TT.IncDec:
							{
								Skip();
								list.Add(ExprStart());
							}
							break;
						default:
							Skip();
							break;
						}
					} else
						goto stop;
				}
			 stop:;
				return Up(F.Tuple(list, startIndex, endIndex));
			}
			Match();
			return Up(F.InParens(e, startIndex, endIndex));
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
		void WhereClausesOpt(ref LNode name)
		{
		}
		void WhereClause()
		{
			Check(Is(0, _where), "Is($LI, _where)");
			Match((int) TT.Id);
		}
		void ExprList(RWList<LNode> list)
		{
			TokenType la0;
			list.Add(ExprStart());
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Comma) {
					Skip();
					list.Add(ExprStart());
				} else if (la0 == EOF)
					break;
				else {
					Error(InputPosition, "Expected ','");
					for (;;) {
						la0 = LA0;
						if (!(la0 == EOF || la0 == TT.Comma))
							Skip();
						else
							break;
					}
				}
			}
			Match();
		}
		void StmtList(RWList<LNode> list)
		{
			TokenType la0;
			list.Add(Stmt());
			for (;;) {
				la0 = LA0;
				if (la0 == TT.Semicolon) {
					Skip();
					list.Add(Stmt());
				} else if (la0 == EOF)
					break;
				else {
					Error(InputPosition, "Expected ';'");
					for (;;) {
						la0 = LA0;
						if (!(la0 == EOF || la0 == TT.Semicolon))
							Skip();
						else
							break;
					}
				}
			}
			Match();
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
		static readonly HashSet<int> WordAttributes_Test0_set0 = NewSet(TT.@break, TT.@continue, TT.@return, TT.@throw, TT.@case, TT.@class, TT.@delegate, TT.@do, TT.@enum, TT.@event, TT.@fixed, TT.@for, TT.@foreach, TT.@goto, TT.@interface, TT.@lock, TT.@namespace, TT.@struct, TT.@switch, TT.@try, TT.@using, TT.@while);
		private bool Try_WordAttributes_Test0(int lookaheadAmt)
		{
			using (new SavePosition(this, lookaheadAmt))
				return WordAttributes_Test0();
		}
		private bool WordAttributes_Test0()
		{
			switch (LA0) {
			case TT.Id:
			case TT.TypeKeyword:
			case TT.Substitute:
				{
					if (!Scan_DataType())
						return false;
					if (!TryMatch((int) TT.Id))
						return false;
				}
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
			default:
				if (!TryMatch(WordAttributes_Test0_set0))
					return false;
				break;
			}
			return true;
		}
	}
}
