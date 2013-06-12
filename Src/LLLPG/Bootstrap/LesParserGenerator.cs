using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Loyc.Syntax.Les
{
	using Loyc.LLParserGenerator;

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
				t:=(TT.NormalOp |TT.Dot |TT.Assignment|TT.BQString) {return t;}
			];
			
			Square (x::double)::double => x*x;
			Square.[$T] x::T => x*x;

			priv Atom(context::Precedence, @ref attrs::RVList.[LNode])::LNode [
				{LNode e;}
				(	t:=TT.Id
					(	&{t.EndIndex == LT(\LI).StartIndex && context.CanParse(P.Primary)}
						p:=TT.Parens
						{e = F.Call((Symbol)t.Value, ExprListInside(p).ToRVList());}
					|	{e = F.Id((Symbol)t.Value);})
				|	t:=(TT.Number|TT.String|TT.SQString|TT.Symbol|TT.OtherLit) 
					{e = F.Literal(t.Value);}
				|	t:=(TT.NormalOp |TT.Dot |TT.Assignment|TT.BQString|TT.PreSufOp)
					{LNode _;}
					e=Expr(UnaryPrecedenceOf(t), @#out _) {e = F.Call((Symbol)t.Value, e);}
				|	t:=TT.At
					{	if (LA(0) == TT.At) Error("Nested attributes require parenthesis"); }
					attr:=Expr(P.Primary) {
						if (attr.Calls(S.Tuple))
							attrs.AddRange(attr.Args);
						else
							attrs.Add(attr);
					}
					e=Atom(context)
				|	t:=TT.Parens {e = InterpretParens(t);}
				|	t:=TT.Braces {e = InterpretBraces(t);}
				|	t:=TT.Bracks {e = InterpretBracks(t);}
				|	error {
						e = F.Missing;
						Error("Expected an expression here");
					}
				)
			];
			
			_primaryExpr::LNode;
			
			pub Expr(context::Precedence, @out primary::LNode)::LNode [
				{LNode e; Precedence prec;}
				e=Atom(context) 
				{primary = e;}
				greedy
				(	&{context.CanParse(prec=InfixPrecedenceOf(LT(\LI)))}
					t:=(TT.NormalOp |TT.Dot |TT.Assignment|TT.BQString)
					rhs:=Expr(prec, @#out primary)
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
				e:=Expr(StartStmt, @#out primary)
				{var otherExprs = RVList<LNode>.Empty;}
				(other:=Expr(StartStmt, @#out _)
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
#endif
			return LNode.Missing;
		}
	}
}
