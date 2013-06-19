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
			_pg = new LLParserGenerator(new GeneralCodeGenHelper());
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == LNode.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// Just do whitespace-agnostic LES at first

/*
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

			LNode Stmt [
				n:=Expr ';' { return n; }
			];

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
			
			rule NormalOp::Token [
				t:=(TT.NormalOp |TT.Dot |TT.Assignment) {return t;}
			];
			
			Square (x::double)::double => x*x;
			Square.[$T] x::T => x*x;

			priv Atom(context::Precedence, [ref] attrs::RVList.[LNode])::LNode [
				{LNode e, _;}
				(	t:=TT.Id
					(	&{t.EndIndex == LT($LI).StartIndex && context.CanParse(P.Primary)}
						p:=TT.Parens
						{e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList());}
					|	{e = F.Id((Symbol)t.Value);})
				|	t:=(TT.Number|TT.String|TT.SQString|TT.Symbol|TT.OtherLit) 
					{e = F.Literal(t.Value);}
				|	t:=(TT.NormalOp |TT.Dot |TT.Assignment|TT.PreSufOp)
					e=Expr(UnaryPrecedenceOf(t), [#out] _) {e = F.Call((Symbol)t.Value, e);}
				|	t:=TT.At
					{	if (LA(0) == TT.At) Error("Nested attributes require parenthesis"); }
					attr:=Expr(P.Primary, [#out] _) {
						if (attr.Calls(S.Tuple))
							attrs.AddRange(attr.Args);
						else
							attrs.Add(attr);
					}
					e=Atom(context)
				|	t:=TT.LParen TT.RParen {e = InterpretParens(t);}
				|	t:=TT.LBrace TT.RBrace {e = InterpretBraces(t);}
				|	t:=TT.LBrack TT.RBrack {e = InterpretBracks(t);}
				|	error {
						e = F.Missing;
						Error("Expected an expression here");
					}
				)
			];
			
 */
			Pred Id = T(TT.Id), LParen = T(TT.LParen), RParen = T(TT.RParen);
			Pred LBrace = T(TT.LBrace), RBrace = T(TT.RBrace);
			Pred LBrack = T(TT.LBrack), RBrack = T(TT.RBrack);
			Pred Literal = T(TT.Number, TT.String, TT.Symbol, TT.OtherLit);
			Pred PrefixOp = T(TT.NormalOp, TT.Dot, TT.Assignment, TT.PreSufOp);
			var la = F.Call(S.Substitute, F.Id("LA"));
			var li = F.Call(S.Substitute, F.Id("LI"));
			var lt_li = F.Call("LT", li);
			
			Rule expr = Rule("Expr", null, Start);
			Rule atom = Rule("Atom", null, Private);
			atom.Pred =
				Stmt("LNode e, _") +
				SetVar("t", +Id) + 
				(	And(F.Call(S.And, F.Call(S.Eq, F.Dot("t", "EndIndex"), F.Dot(lt_li, F.Id("StartIndex"))), Expr("context.CanParse(P.Primary)"))) +
					SetVar("p", +LParen) + +RParen +
					Stmt("e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList())") +
					Stmt("e = F.Id((Symbol)t.Value)")
				|	SetVar("t", Literal) +
					Stmt("e = F.Literal(T.Value)")
				|	SetVar("t", PrefixOp) +
					Call(expr, Expr("UnaryPrecedenceOf(t)"), Expr("out _")) +
					Stmt("e = F.Call((Symbol)t.Value, e)")
				|	SetVar("t", T(TT.At)) +
					Stmt(@"if (LA(0) == TT.At) Error(""Nested attributes require parenthesis"")") +
					SetVar("attr", Call(expr, Expr("P.Primary"), Expr("out _"))) +
					Stmt(@"
					if (attr.Calls(S.Tuple))
						attrs.AddRange(attr.Args);
					else
						attrs.Add(attr);") +
					Set("e", Call(atom, F.Id("context")))
				|	SetVar("t", +LParen) + +RParen + Stmt("e = InterpretParens(t)")
				|	SetVar("t", +LBrace) + +RBrace + Stmt("e = InterpretBraces(t)")
				|	SetVar("t", +LBrack) + +RBrack + Stmt("e = InterpretBracks(t)")
				);	// TODO: custom error branch feature
			

 /* 
			_primaryExpr::LNode;
			
			pub Expr(context::Precedence, [out] primary::LNode)::LNode [
				{LNode e; Precedence prec;}
				e=Atom(context) 
				{primary = e;}
				greedy
				(	&{context.CanParse(prec=InfixPrecedenceOf(LT(\LI)))}
					t:=(TT.NormalOp |TT.Dot |TT.Assignment|TT.BQString)
					rhs:=Expr(prec, [#out] primary)
					{e = F.Call((Symbol)t.Value, e, rhs);}
					{e.BaseStyle = NodeStyle.Operator;}
					{if (!prec.CanParse(P.NullDot)) primary = e;}
				|	&{context.CanParse(UnaryPrecedenceOf(LT(\LI)))}
					t:=TT.PreSufOp
					{e = F.Call(ToSuffixOpName((Symbol)t.Value), e);}
					{e.BaseStyle = NodeStyle.Operator;}
					{primary = null;} // disallow superexpression after suffix (prefix/suffix ambiguity)
				|	&{t.EndIndex == LT(\LI).StartIndex && context.CanParse(P.Primary)}
					t:=TT.Parens
					{e = primary = F.Call(e, ExprListInside(t).ToRVList());}
					{e.BaseStyle = NodeStyle.PurePrefixNotation;}
				|	&{t.EndIndex == LT(\LI).StartIndex && context.CanParse(P.Primary)}
					t:=TT.Bracks
					{
						var args = new RWList<LNode> { e };
						AppendExprsInside(t, args);
						e = primary = F.Call(S.Bracks, args.ToRVList());
						e.BaseStyle = NodeStyle.Expression;
					}
				)*
				{return e;}
			];
			
			pub SuperExpr() [
				{LNode primary, _;}
				e:=Expr(StartStmt, [#out] primary)
				{var otherExprs = RVList<LNode>.Empty;}
				(other:=Expr(StartStmt, [#out] _)
				stopped here
			];
		
			pub Stmt()::LNode [
				SuperExpr TT.Semicolon
			];
			
			//Expr (...) :: ret { ... } => Expr( (...) :: ret, { ... })
			//Expr(...) :: ret  { ... } => (Expr(...) :: ret)({...})
			// if(c) -a else b
			// Warning: '-' is formatted like a 'prefix' operator but has been interpreted as an infix operator. (To hide this warning, add a space after '-'.)
			
			
			Literal..LNode [
				  t:=(TT.String | TT.SQString | TT.Number | TT.Symbol | TT.OtherLit) { return F.Literal(t.Value); }
				| n:=TokenLiteral { return n; }
			];
			TokenLiteral..LNode [ t:="[]" { return F.Literal(t.Children); } ];
*/
			return LNode.Missing;
		}

		LNode L(TokenType tt) { return F.Dot(F.Id("TT"), F.Id(tt.ToString())); }
		Pred T(TokenType tt) { return Pred.Set(new PGNodeSet(L(tt))); }
		Pred T(params TokenType[] tts) { return Pred.Set(new PGNodeSet(tts.Select(tt => L(tt)))); }
	}
}
