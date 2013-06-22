using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Les
{
	using Loyc.LLParserGenerator;
	using S = Loyc.Syntax.CodeSymbols;
	using TT = Loyc.Syntax.Les.TokenType;

	class LesParserGenerator : LlpgHelpers
	{
		LLParserGenerator _pg;

		public LNode GenerateParserCode()
		{
			_pg = new LLParserGenerator(new GeneralCodeGenHelper("TT", true));
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == LNode.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Just do whitespace-agnostic LES at first

#if false
			// LES parser

			RWList<LNode> ExprListInside(Token group)
			{
				return AppendExprsInside(group, new RWList<LNode>());
			}
			RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list)
			{
				...
				return list;
			}

			int*$
			int?$
			int[]

			// UML: 
			// + public
			// - private
			// -+ protected
			// +- internal
			// -+ +- protected unioned with internal
			// -+-, +-+ protected intersected with internal
			// # protected
			// _ static
			
			rule NormalOp::Token @[
				t:=(TT.NormalOp |TT.Dot |TT.Assignment) {return t;}
			];
			
			Square (x::double)::double => x*x;
			Square.[$T] x::T => x*x;

			priv Atom(context::Precedence, [ref] attrs::RWList!LNode)::LNode @[
				{LNode e, _;}
				(	t:=TT.Id
					(	&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)}
						p:=TT.LParen TT.RParen
						{e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList());}
					/	{e = F.Id((Symbol)t.Value);})
				|	t:=(TT.Number|TT.String|TT.SQString|TT.Symbol|TT.OtherLit) 
					{e = F.Literal(t.Value);}
				|	TT.At t:=TT.LBrack TT.RBrack
					{e = F.Literal(t.Children);}
				|	t:=(TT.NormalOp |TT.Dot |TT.Assignment|TT.PreSufOp|TT.PrefixOp)
					e=Expr(PrefixPrecedenceOf(t), [#out] _) 
					{e = F.Call((Symbol)t.Value, e);}
				|	t:=TT.LBrack TT.RBrack
					{attrs = AppendExprsInside(t, attrs);}
					e=Atom(context, ref attrs)
				|	t:=TT.LParen TT.RParen {e = InterpretParens(t);}
				|	t:=TT.LBrace TT.RBrace {e = InterpretBraces(t);}
				|	error {
						e = F.Missing;
						Error("Expected an expression here");
					}
				)
				{return e;}
			];
#endif
			Pred Id = T(TT.Id), LParen = T(TT.LParen), RParen = T(TT.RParen);
			Pred LBrace = T(TT.LBrace), RBrace = T(TT.RBrace);
			Pred LBrack = T(TT.LBrack), RBrack = T(TT.RBrack);
			Pred Literal = T(TT.Number, TT.String, TT.Symbol, TT.OtherLit);
			Pred PrefixOp = T(TT.NormalOp, TT.Dot, TT.Assignment, TT.PreSufOp, TT.PrefixOp);
			Pred InfixOp = T(TT.NormalOp, TT.Dot, TT.Assignment);
			Pred SuffixOp = T(TT.PreSufOp, TT.SuffixOp);
			Pred Comma = T(TT.Comma);
			var la = F.Call(S.Substitute, F.Id("LA"));
			var li = F.Call(S.Substitute, F.Id("LI"));
			var lt_li = F.Call("LT", li);
			
			Rule expr = Rule("Expr", null, Start);
			Rule atom = Rule("Atom", null, Private);
			atom.Basis = F.Def(F.Id("LNode"), F._Missing, F.List(Expr("Precedence context"), Expr("ref RWList<LNode> attrs")));
			atom.Pred =
				Stmt("LNode e, _") +
				(	SetVar("t", +Id) + 
					((	And(F.Call(S.And, F.Call(S.Eq, F.Dot("t", "EndIndex"), F.Dot(lt_li, F.Id("StartIndex"))), Expr("context.CanParse(P.Primary)"))) +
						SetVar("p", +LParen) + +RParen +
						Stmt("e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList())"))
					/(	Expr("e = F.Id((Symbol)t.Value)") + Seq()
					))
				|	SetVar("t", Literal) +
					Stmt("e = F.Literal(t.Value)")
				|	T(TT.At) + SetVar("t", +LBrack) + +RBrack +
					Stmt(@"e = F.Literal(t.Children)")
				|	SetVar("t", PrefixOp) +
					Set("e", Call(expr, Expr("PrefixPrecedenceOf(t)"), Expr("out _"))) +
					Stmt("e = F.Call((Symbol)t.Value, e)")
				|	SetVar("t", +LBrack) + +RBrack +
					Stmt("attrs = AppendExprsInside(t, attrs)") +
					Set("e", Call(atom, F.Id("context"), Expr("ref attrs")))
				|	SetVar("t", +LParen) + +RParen + Stmt("e = InterpretParens(t)")
				|	SetVar("t", +LBrace) + +RBrace + Stmt("e = InterpretBraces(t)")
				) // TODO: custom error branch feature
				+ Stmt("return e");	
			
 #if false
			_primaryExpr::LNode;
			
			pub Expr(context::Precedence, [out] primary::LNode)::LNode @[
				{LNode e; Precedence prec; RVList<LNode> attrs;}
				e=Atom(context, out attrs) 
				{primary = e;}
				greedy
				(	&{context.CanParse(prec=InfixPrecedenceOf(LT(\LI)))}
					t:=(TT.NormalOp |TT.Dot |TT.Assignment)
					rhs:=Expr(prec, [#out] primary)
					{e = F.Call((Symbol)t.Value, e, rhs);}
					{e.BaseStyle = NodeStyle.Operator;}
					{if (!prec.CanParse(P.NullDot)) primary = e;}
				|	&{context.CanParse(SuffixPrecedenceOf(LT(\LI)))}
					t:=(TT.PreSufOp|TT.SuffixOp)
					{e = F.Call(ToSuffixOpName((Symbol)t.Value), e);}
					{e.BaseStyle = NodeStyle.Operator;}
					{primary = null;} // disallow superexpression after suffix (prefix/suffix ambiguity)
				|	&{t.EndIndex == LT(\LI).StartIndex && context.CanParse(P.Primary)}
					t:=TT.LParen TT.RParen
					{e = primary = F.Call(e, ExprListInside(t).ToRVList());}
					{e.BaseStyle = NodeStyle.PurePrefixNotation;}
				|	&{context.CanParse(P.Primary)}
					t:=TT.LBrack TT.RBrack
					{
						var args = new RWList<LNode> { e };
						AppendExprsInside(t, args);
						e = primary = F.Call(S.Bracks, args.ToRVList());
						e.BaseStyle = NodeStyle.Expression;
					}
				)*
				{return e.WithAttrs(attrs.ToRVList());}
			];
#endif
			expr.Basis = F.Def(F.Id("LNode"), F._Missing, F.List(Expr("Precedence context"), Expr("out LNode primary")));
			expr.Pred = Stmt("LNode e; Precedence prec; RWList<LNode> attrs = null") +
				Set("e", Call(atom, F.Id("context"), Expr("ref attrs"))) +
				Stmt("primary = e") +
				Star(
					And(F.Call(F.Dot("context", "CanParse"), F.Set(F.Id("prec"), F.Call(F.Id("InfixPrecedenceOf"), lt_li)))) +
					SetVar("t", +InfixOp) +
					SetVar("rhs", Call(expr, F.Id("prec"), Expr("out primary"))) +
					Stmt("e = F.Call((Symbol)t.Value, e, rhs);") +
					Stmt("e.BaseStyle = NodeStyle.Operator") +
					Stmt("if (!prec.CanParse(P.NullDot)) primary = e;")
				|	And(F.Call(F.Dot("context", "CanParse"), F.Call(F.Id("UnaryPrecedenceOf"), lt_li))) +
					SetVar("t", +SuffixOp) +
					Stmt("e = F.Call(ToSuffixOpName((Symbol)t.Value), e)") +
					Stmt("e.BaseStyle = NodeStyle.Operator") +
					Stmt("primary = null; // disallow superexpression after suffix (prefix/suffix ambiguity")
				|	//And(F.Call(S.And, F.Call(S.Eq, Expr("t.EndIndex"), F.Dot(lt_li, F.Id("StartIndex"))), Expr("context.CanParse(P.Primary)"))) +
					And(Expr("context.CanParse(P.Primary)")) +
					SetVar("t", +LBrack) + +RBrack +
					Stmt(@"
						var args = new RWList<LNode> { e };
						AppendExprsInside(t, args);
						e = primary = F.Call(S.Bracks, args.ToRVList());
						e.BaseStyle = NodeStyle.Expression;"
					.Replace("\r\n", "\n"))
				,true) +
				Stmt("return attrs == null ? e : e.WithAttrs(attrs.ToRVList())");

#if false
			pub SuperExpr()::LNode @[
				{LNode primary, p_;}
				e:=Expr(StartStmt, [#out] primary)
				{var otherExprs = RVList<LNode>.Empty; p_ = e;}
				(	
					{if (p_ == null) Error(InputPosition-2, "Suffix operator is ambiguous at superexpression boundary.");}
					otherExprs+=Expr(StartStmt, [#out] p_) 
					{primary.BaseStyle = NodeStyle.Special;}
				)*
				{return MakeSuperExpr(e, primary, otherExprs);}
			];
#endif
			LNode ReturnsLNode = F.Def(F.Id("LNode"), F._Missing, F.List());

			Rule superExpr = Rule("SuperExpr", 
				Stmt("LNode primary, p_") +
				SetVar("e", Call(expr, F.Id("StartStmt"), Expr("out primary"))) +
				Stmt("var otherExprs = RVList<LNode>.Empty; p_ = e;") +
				Star(
					Stmt(@"if (p_ == null) Error(InputPosition-2, ""Suffix operator is ambiguous at superexpression boundary."")") +
					AddSet("otherExprs", Call(expr, F.Id("StartStmt"), Expr("out p_"))) +
					Stmt("primary.BaseStyle = NodeStyle.Special")
				) +
				Stmt("return MakeSuperExpr(e, primary, otherExprs)"),
				Start);
			superExpr.Basis = ReturnsLNode;

			_pg.AddRules(atom, expr, superExpr);

#if false
			LNode MissingExpr = F.Id(S.Missing);

			pub SuperExprOpt()::LNode @[
				(e:=SuperExpr {return e;} | {return MissingExpr;})
			];
			pub ExprList([ref] RWList!LNode) @[
				{exprs = exprs ?? new RWList<LNode>();}
				(	exprs+=SuperExpr
					(TT.Comma exprs+=SuperExprOpt)*
				|	{exprs.Add(MissingExpr);}
					(TT.Comma exprs+=SuperExprOpt)+
				)?
			];
			pub StmtList([ref] RWList!LNode) @[
				{exprs = exprs ?? new RWList<LNode>();}
				next:=SuperExprOpt
				(	{exprs.Add(next);}
					{next = MissingExpr;}
					TT.Semicolon 
					(	next=SuperExpr
					/	{Error("Expected a statement to start here");} (~TT.Semicolon)+
					)?
				)*
				{if (next != (object)MissingExpr) exprs.Add(next);}
			];
#endif
			Rule superExprOpt = Rule("SuperExprOpt",
				SetVar("e", superExpr) + Stmt("return e") | Expr("return MissingExpr") + Seq(), Private);
			superExprOpt.Basis = ReturnsLNode;
			
			LNode AppendsExprList = F.Def(F.Void, F._Missing, F.List(Expr("ref RWList<LNode> exprs")));
			Rule exprList = Rule("ExprList",
				Stmt("exprs = exprs ?? new RWList<LNode>()") +
				Opt(
					AddSet("exprs", superExpr) + 
					Star(+Comma + AddSet("exprs", superExprOpt))
				|	Stmt("exprs.Add(MissingExpr)") +
					Plus(+Comma + AddSet("exprs", superExprOpt))
				),
				Start);
			Rule stmtList = Rule("StmtList",
				Stmt("exprs = exprs ?? new RWList<LNode>()") +
				SetVar("next", superExprOpt) +
				Star(
					Stmt("exprs.Add(next); next = MissingExpr") +
					T(TT.Semicolon) +
					Opt(Set("next", superExpr)
					/	(Stmt(@"Error(""Expected a statement to start here"")") + Plus(Not(TT.Semicolon, TT.LBrace)))
					)
				) +
				Stmt("if (next != (object)MissingExpr) exprs.Add(next);"),
				Start);
			exprList.Basis = AppendsExprList;
			stmtList.Basis = AppendsExprList;
			
			_pg.AddRules(superExprOpt, exprList, stmtList);
			LNode members = _pg.GenerateCode(F.File);

			return F.Attr(F.Public, F.Id(S.Partial), 
			        F.Call(S.Class, F.Id("LesParser"), F.List(), members));
		}

		LNode L(TokenType tt) { return F.Dot(F.Id("TT"), F.Id(tt.ToString())); }
		Pred T(TokenType tt) { return Pred.Set(new PGNodeSet(L(tt))); }
		Pred T(params TokenType[] tts) { return Pred.Set(new PGNodeSet(tts.Select(tt => L(tt)))); }
		Pred Not(params TokenType[] tts) { return Pred.Set(new PGNodeSet(tts.Select(tt => L(tt)), true)); }
	}
}
