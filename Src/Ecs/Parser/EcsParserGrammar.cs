// Generated from EcsParserGrammar.les by LLLPG custom tool. LLLPG version: 0.9.2.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// --no-out-header       Suppress this message
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
	partial class EcsParser
	{
		LNode CallBinary(Symbol op, LNode lhs, LNode rhs)
		{
			int start = lhs.Range.StartIndex;
			return F.Call(op, lhs, rhs, start, rhs.Range.EndIndex - start);
		}
		static readonly Symbol _alias = GSymbol.Get("alias");
		static readonly Symbol _where = GSymbol.Get("where");
		bool Is(int li, Symbol value)
		{
			return LT(li).Value == value;
		}
		Symbol _spaceName;
		int count;
		void WhereClause()
		{
			Check(Is(0, _where), "Is($LI, _where)");
			Match((int) TT.Id);
		}
		LNode DataType()
		{
			var e = ComplexId();
			TypeSuffixOpt(ref e);
			return e;
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
					e = CallBinary(S.ColonColon, e, e2);
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
			TokenType la0, la1;
			LNode r;
			la0 = LA0;
			if (la0 == TT.Substitute) {
				la1 = LA(1);
				if (la1 == TT.Id || la1 == TT.Substitute || la1 == TT.TypeKeyword) {
					var t = MatchAny();
					var e = Atom();
					r = F.Call(S.Substitute, e, t.StartIndex, e.Range.EndIndex - t.StartIndex);
				} else {
					var t = MatchAny();
					var p = Match((int) TT.LParen);
					var rp = Match((int) TT.RParen);
					r = F.Call(S.Substitute, ExprInside(t), rp.EndIndex - t.StartIndex);
				}
			} else {
				var t = Match((int) TT.Id, (int) TT.TypeKeyword);
				r = F.Id((Symbol) t.Value, t.StartIndex, t.Length);
			}
			return r;
		}
		bool Scan_IdAtom()
		{
			TokenType la0, la1;
			la0 = LA0;
			if (la0 == TT.Substitute) {
				la1 = LA(1);
				if (la1 == TT.Id || la1 == TT.Substitute || la1 == TT.TypeKeyword) {
					if (!TryMatch((int) TT.Substitute))
						return false;
					if (!Scan_Atom())
						return false;
				} else {
					if (!TryMatch((int) TT.Substitute))
						return false;
					if (!TryMatch((int) TT.LParen))
						return false;
					if (!TryMatch((int) TT.RParen))
						return false;
				}
			} else if (!TryMatch((int) TT.Id, (int) TT.TypeKeyword))
				return false;
			return true;
		}
		void RestOfId(ref LNode r)
		{
			TokenType la0, la1;
			la0 = LA0;
			if (la0 == TT.Dot || la0 == TT.LT || la0 == TT.Not) {
				if (Try_Scan_TParams(0)) {
					switch (LA(1)) {
					case TT.GT:
					case TT.LParen:
					case TT.LBrack:
					case TT.Substitute:
					case TT.Id:
					case TT.TypeKeyword:
						TParams(ref r);
						break;
					}
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
				if (Try_Scan_TParams(0)) {
					switch (LA(1)) {
					case TT.GT:
					case TT.LParen:
					case TT.LBrack:
					case TT.Substitute:
					case TT.Id:
					case TT.TypeKeyword:
						if (!Scan_TParams())
							return false;
						break;
					}
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
			r = F.Call(S.Of, list.ToRVList(), start, end.EndIndex - start);
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
		bool TypeSuffixOpt(ref LNode e)
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
			var e = IdAtom();
			return e;
		}
		bool Scan_Atom()
		{
			if (!Scan_IdAtom())
				return false;
			return true;
		}
	}
}
