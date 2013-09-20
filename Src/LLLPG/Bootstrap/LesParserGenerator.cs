using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Les
{
	using Loyc.LLParserGenerator;
	using S = Loyc.Syntax.CodeSymbols;
	using TT = Loyc.Syntax.Les.TokenType;

	/// <summary>Generates source code for the LES parser.</summary>
	class LesParserGenerator : LlpgHelpers
	{
		LLParserGenerator _pg;

		public LNode GenerateParserCode()
		{
			_pg = new LLParserGenerator(new GeneralCodeGenHelper("TT", true) { MatchType = F.Int32 });
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == LNode.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Just do whitespace-agnostic LES at first

#if false
			int*$
			int?$
			int[]

			// An Atom is:
			// - a literal or simple identifier
			//   - simple calls are also handled here, as a space optimization
			// - a token literal @[ ... ]
			// - a prefix operator followed by an Expr
			// - [Attributes] followed by an Atom
			// - an (expression) in parenthesis
			// - a { block } in braces
			priv Atom(context::Precedence, [ref] attrs::RWList!LNode)::LNode @[
				{LNode e, _;}
				(	t:=TT.Id
					(	&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)}
						p:=TT.LParen rp:=TT.RParen
						{e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList(), t.StartIndex, rp.EndIndex - t.StartIndex);}
					/	{e = F.Id((Symbol)t.Value, t.StartIndex, t.Length);})
				|	t:=(TT.Number|TT.String|TT.SQString|TT.Symbol|TT.OtherLit) 
					{e = F.Literal(t.Value, t.StartIndex, t.Length);}
				|	TT.At t:=TT.LBrack rb:=TT.RBrack
					{e = F.Literal(t.Children, t.StartIndex, rb.EndIndex - t.StartIndex);}
				|	t:=(TT.NormalOp|TT.BQString|TT.Dot|TT.Assignment|TT.PreSufOp|TT.PrefixOp)
					e=Expr(PrefixPrecedenceOf(t), [#out] _) 
					{e = F.Call((Symbol)t.Value, e, t.StartIndex, e.Range.EndIndex - t.StartIndex);}
				|	t:=TT.LBrack TT.RBrack
					{attrs = AppendExprsInside(t, attrs);}
					e=Atom(context, ref attrs)
				|	t:=TT.LParen rp:=TT.RParen {e = InterpretParens(t, rp.EndIndex);}
				|	t:=TT.LBrace rb:=TT.RBrace {e = InterpretBraces(t, rb.EndIndex);}
				|	error {
						e = F.Id(S.Missing, LT0.BeginIndex, 0);
						Error("Expected an expression here");
					}
				)
				{return e;}
			];
#endif
			Pred Id = T(TT.Id), LParen = T(TT.LParen), RParen = T(TT.RParen);
			Pred LBrace = T(TT.LBrace), RBrace = T(TT.RBrace);
			Pred LBrack = T(TT.LBrack), RBrack = T(TT.RBrack);
			Pred Literal = T(TT.Number, TT.String, TT.SQString, TT.Symbol, TT.OtherLit);
			Pred PrefixOp = T(TT.PrefixOp, TT.Not, TT.NormalOp, TT.BQString, TT.Dot, TT.Assignment, TT.Colon, TT.PreSufOp);
			Pred InfixOp = T(TT.NormalOp, TT.BQString, TT.Dot, TT.Assignment, TT.Colon);
			Pred SuffixOp = T(TT.PreSufOp, TT.SuffixOp);
			Pred Comma = T(TT.Comma);
			var la = F.Call(S.Substitute, F.Id("LA"));
			var li = F.Call(S.Substitute, F.Id("LI"));
			var lt_li = F.Call("LT", li);
			
			Rule expr = Rule("Expr", null, Token);
			Rule atom = Rule("Atom", null, Private);
			atom.Basis = F.Def(F.Id("LNode"), F._Missing, F.List(Expr("Precedence context"), Expr("ref RWList<LNode> attrs")));
			atom.Pred =
				Stmt("LNode e = F._Missing, _") +
				(	SetVar("t", +Id) + 
					((	And(F.Call(S.And, F.Call(S.Eq, F.Dot("t", "EndIndex"), F.Dot(lt_li, F.Id("StartIndex"))), Expr("context.CanParse(P.Primary)"))) +
						SetVar("p", +LParen) + SetVar("rp", +RParen) +
						Stmt("e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList(), t.StartIndex, rp.EndIndex - t.StartIndex)"))
					/(	Expr("e = F.Id((Symbol)t.Value, t.StartIndex, t.Length)") + Seq()
					))
				|	SetVar("t", Literal) +
					Stmt("e = F.Literal(t.Value, t.StartIndex, t.Length)")
				|	T(TT.At) + SetVar("t", +LBrack) + SetVar("rb", +RBrack) +
					Stmt(@"e = F.Literal(t.Children, t.StartIndex, rb.EndIndex - t.StartIndex)")
				|	SetVar("t", PrefixOp) +
					Set("e", Call(expr, Expr("PrefixPrecedenceOf(t)"), Expr("out _"))) +
					Stmt("e = F.Call((Symbol)t.Value, e, t.StartIndex, e.Range.EndIndex - t.StartIndex)")
				|	SetVar("t", +LBrack) + +RBrack +
					Stmt("attrs = AppendExprsInside(t, attrs)") +
					Set("e", Call(atom, F.Id("context"), Expr("ref attrs")))
				|	SetVar("t", +LParen) + SetVar("rp", +RParen) + Stmt("e = InterpretParens(t, rp.EndIndex)")
				|	SetVar("t", +LBrace) + SetVar("rb", +RBrace) + Stmt("e = InterpretBraces(t, rb.EndIndex)")
				) // TODO: custom error branch feature
				+ Stmt("return e");
			((Alts)atom.Pred).DefaultArm = -1;
			
 #if false
			// PrimaryExprPFloor = P.NullDot

			_primaryExpr::LNode;
			
			// Types of expressions:
			// - Atoms (includes attributes and prefix operators)
			// - infix + operators
			// - generic!arguments
			// - suffix_operators++
			// - method_calls(with arguments)
			// - indexers[with indexes]
			pub Expr(context::Precedence, [out] primary::LNode)::LNode @[
				{LNode e; Precedence prec; RVList<LNode> attrs;}
				e=Atom(context, out attrs) 
				{primary = e;}
				greedy
				(	// Infix operator
					&{context.CanParse(prec=InfixPrecedenceOf(LT(\LI)))}
					t:=(TT.NormalOp|TT.BQString|TT.Dot|TT.Assignment)
					rhs:=Expr(prec, [#out] primary)
					{e = F.Call((Symbol)t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);}
					{e.BaseStyle = NodeStyle.Operator;}
					{if (!prec.CanParse(P.NullDot)) primary = e;}
				|	// ! operator (generics)
					&{context.CanParse(P.Primary)}
					TT.Not
					rhs:=Expr(P.Primary, [#out] primary)
					{
						RVList<LNode> args;
						if (rhs.Calls(S.Missing))
							args = new RVList<LNode>(e).AddRange(rhs.Args);
						else
							args = new RVList<LNode>(e, rhs);
						e = F.Call(S.Of, args, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);
						e.BaseStyle = NodeStyle.Operator;
					}
				|	// Suffix operator
					&{context.CanParse(SuffixPrecedenceOf(LT(\LI)))}
					t:=(TT.PreSufOp|TT.SuffixOp)
					{e = F.Call(ToSuffixOpName((Symbol)t.Value), e, e.Range.StartIndex, t.EndIndex - e.Range.StartIndex);}
					{e.BaseStyle = NodeStyle.Operator;}
					{if (t.Type() == TT.PreSufOp) primary = null;} // disallow superexpression after suffix (prefix/suffix ambiguity)
				|	// Method call
					&{e.Range.EndIndex == LT(\LI).StartIndex && context.CanParse(P.Primary)}
					t:=TT.LParen rp:=TT.RParen
					{e = primary = F.Call(e, ExprListInside(t).ToRVList(), e.Range.StartIndex, rp.EndIndex - e.Range.StartIndex);}
					{e.BaseStyle = NodeStyle.PurePrefixNotation;}
				|	// Indexer / square brackets
					&{context.CanParse(P.Primary)}
					t:=TT.LBrack rb:=TT.RBrack
					{
						var args = new RWList<LNode> { e };
						AppendExprsInside(t, args);
						e = primary = F.Call(S.Bracks, args.ToRVList(), e.Range.StartIndex, rb.EndIndex - e.Range.StartIndex);
						e.BaseStyle = NodeStyle.Expression;
					}
				|	// Juxtaposition
					&{context.CanParse(P_SuperExpr)}
					WORK IN PROGRESS
				)*
				{return attrs == null ? e : e.WithAttrs(attrs.ToRVList());}
			];
#endif
			expr.Basis = F.Def(F.Id("LNode"), F._Missing, F.List(Expr("Precedence context"), Expr("out LNode primary")));
			Alts alts;
			expr.Pred = Stmt("LNode e, _; Precedence prec; " +
				"RWList<LNode> attrs = null; ") +
				Set("e", Call(atom, F.Id("context"), Expr("ref attrs"))) +
				Stmt("primary = e") +
				(alts = Star(
					// Infix operator
					And(F.Call(F.Dot("context", "CanParse"), F.Set(F.Id("prec"), F.Call(F.Id("InfixPrecedenceOf"), lt_li)))) +
					SetVar("t", +InfixOp) +
					SetVar("rhs", Call(expr, F.Id("prec"), Expr("out primary"))) +
					Stmt("e = F.Call((Symbol)t.Value, e, rhs, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);") +
					Stmt("e.BaseStyle = NodeStyle.Operator") +
					Stmt("if (!prec.CanParse(P.NullDot)) primary = e;")
				|	// ! operator (generics)
					And(Expr("context.CanParse(P.Primary)")) +
					T(TT.Not) +
					SetVar("rhs", Call(expr, Expr("P.Primary"), Expr("out primary"))) +
					Stmt(@"
						RVList<LNode> args;
						if (rhs.Calls(S.Tuple))
							args = new RVList<LNode>(e).AddRange(rhs.Args);
						else
							args = new RVList<LNode>(e, rhs);
						e = F.Call(S.Of, args, e.Range.StartIndex, rhs.Range.EndIndex - e.Range.StartIndex);
						e.BaseStyle = NodeStyle.Operator;"
					.Replace("\r\n", "\n"))
				|	// Suffix operator
					And(F.Call(F.Dot("context", "CanParse"), F.Call(F.Id("SuffixPrecedenceOf"), lt_li))) +
					SetVar("t", +SuffixOp) +
					Stmt("e = F.Call(ToSuffixOpName((Symbol)t.Value), e, e.Range.StartIndex, t.EndIndex - e.Range.StartIndex)") +
					Stmt("e.BaseStyle = NodeStyle.Operator") +
					Stmt("if (t.Type() == TT.PreSufOp) primary = null; // disallow superexpression after suffix (prefix/suffix ambiguity")
				|	// Method call
					And(F.Call(S.And, F.Call(S.Eq, Expr("e.Range.EndIndex"), F.Dot(lt_li, F.Id("StartIndex"))), Expr("context.CanParse(P.Primary)"))) +
					SetVar("t", +LParen) + SetVar("rp", +RParen) +
					Stmt("e = primary = F.Call(e, ExprListInside(t).ToRVList(), e.Range.StartIndex, rp.EndIndex - e.Range.StartIndex)") +
					Stmt("e.BaseStyle = NodeStyle.PurePrefixNotation")
				|	// Indexer / square brackets
					And(Expr("context.CanParse(P.Primary)")) +
					SetVar("t", +LBrack) + SetVar("rb", +RBrack) +
					Stmt(@"
						var args = new RWList<LNode> { e };
						AppendExprsInside(t, args);
						e = primary = F.Call(S.Bracks, args.ToRVList(), e.Range.StartIndex, rb.EndIndex - e.Range.StartIndex);
						e.BaseStyle = NodeStyle.Expression;"
					.Replace("\r\n", "\n"))
				|	// Juxtaposition / superexpression
					// A loop is not strictly needed here; we could add each expr
					// one at a time, but that would be less efficient in the 
					// majority of cases.
					And(Expr("context.CanParse(P_SuperExpr)")) +
					Stmt("var rhs = RVList<LNode>.Empty") +
					Plus(AddSet("rhs", Call(expr, Expr("P_SuperExpr"), Expr("out _"))), greedy:true) +
					Stmt("e = MakeSuperExpr(e, ref primary, rhs)")
				,true)) +
				Stmt("return attrs == null ? e : e.WithAttrs(attrs.ToRVList())");
			
			// The Juxtaposition operator is ambiguous with several other branches.
			// I'd use the "/" operator but its precedence is annoyingly higher than +. Instead:
			alts.NoAmbigWarningFlags = 0x20;
#if false
			// A superexpression is a sequence of expressions with no separator 
			// between them. The first expression is treated specially; e.g.
			// the super expression a+b c*d e=f, which consists of three
			// expressions a+b, c*d and e=f, is parsed (a + b(c * d, e = f)).
			pub SuperExpr()::LNode @[
				{LNode primary, p_;}
				e:=Expr(StartStmt, [#out] primary)
				{var otherExprs = RVList<LNode>.Empty; p_ = e;}
				(	
					{if (p_ == null) Error(InputPosition-2, "Suffix operator is ambiguous at superexpression boundary.");}
					otherExprs+=Expr(StartStmt, [#out] p_) 
					{primary.BaseStyle = NodeStyle.Special;}
				)*
				~(TT.Semicolon | TT.Comma) {  } ~(TT.Semicolon | TT.Comma)*
				{return MakeSuperExpr(e, primary, otherExprs);}
			];
#endif
			LNode ReturnsLNode = F.Def(F.Id("LNode"), F._Missing, F.List());

			//Rule superExpr = Rule("SuperExpr", 
			//    Stmt("LNode primary, p_") +
			//    SetVar("e", Call(expr, F.Id("StartStmt"), Expr("out primary"))) +
			//    Stmt("var otherExprs = RVList<LNode>.Empty; p_ = e;") +
			//    Star(
			//        Stmt(@"if (p_ == null) Error(InputPosition-2, ""Suffix operator is ambiguous at superexpression boundary."")") +
			//        AddSet("otherExprs", Call(expr, F.Id("StartStmt"), Expr("out p_"))) +
			//        Stmt("primary.BaseStyle = NodeStyle.Special")
			//    ) +
			//    Stmt("return MakeSuperExpr(e, primary, otherExprs)"),
			//    Start);
			Rule superExpr = Rule("SuperExpr",
				Stmt("LNode _") +
				SetVar("e", Call(expr, Expr("StartStmt"), Expr("out _"))) +
				Stmt("return e"), Start);

			superExpr.Basis = ReturnsLNode;

			_pg.AddRules(atom, expr, superExpr);
			
#if false
			LNode MissingExpr = F.Id(S.Missing);

			pub SuperExprOpt()::LNode @[
				(e:=SuperExpr {return e;} | {return MissingExpr;})
			];
			// A sequence of expressions separated by commas
			pub ExprList([ref] exprs::RWList!LNode) @[
				{exprs = exprs ?? new RWList<LNode>();}
				(	exprs+=SuperExpr
					(TT.Comma exprs+=SuperExprOpt)*
				|	{exprs.Add(MissingExpr);}
					(TT.Comma exprs+=SuperExprOpt)+
				)?
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

#if false
			pub SuperExprOptUntil(terminator::TokenType)::LNode @[
				{LNode e = MissingExpr;}
				e=SuperExpr?
				{bool error = false;}
				(	&{\LA!=terminator} 
					{	if (!error) {
							error = true;
							Error(InputPosition, "Expected " + terminator.ToString());
						}
					}
					_ 
				)*
				{return e;}
			];
			pub StmtList([ref] exprs::RWList!LNode) @[
				{exprs = exprs ?? new RWList<LNode>();}
				next:=SuperExprOptUntil(TT.Semicolon)
				(	{exprs.Add(next);}
					TT.Semicolon 
					next=SuperExprOptUntil(TT.Semicolon)
				)*
				{if (next != (object)MissingExpr) exprs.Add(next);}
			];
#endif
			Rule superExprOptUntil = Rule("SuperExprOptUntil",
				Stmt("LNode e = MissingExpr") +
				Opt(Set("e", superExpr)) +
				Stmt("bool error = false") +
				Gate(T(TT.Semicolon, TT.Comma), 
					Star(
						And(F.Call(S.Neq, la, F.Id("terminator"))) +
						Stmt(@"
							if (!error) {
								error = true;
								Error(InputPosition, ""Expected "" + terminator.ToString());
							}
						".Replace("\r\n", "\n")) +
						AnyNode
					,true)) +
				Stmt("return e"), Private
			);
			superExprOptUntil.Basis = F.Def(F.Id("LNode"), F._Missing, F.List(Expr("TokenType terminator")));

			Rule stmtList = Rule("StmtList",
				Stmt("exprs = exprs ?? new RWList<LNode>()") +
				SetVar("next", Call(superExprOptUntil, Expr("TT.Semicolon"))) +
				Star(
					Stmt("exprs.Add(next)") +
					T(TT.Semicolon) +
					Set("next", Call(superExprOptUntil, Expr("TT.Semicolon")))
				) +
				Stmt("if (next != (object)MissingExpr) exprs.Add(next);"),
				Start);
			exprList.Basis = AppendsExprList;
			stmtList.Basis = AppendsExprList;
			
			_pg.AddRules(superExprOpt, superExprOptUntil, exprList, stmtList);
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
