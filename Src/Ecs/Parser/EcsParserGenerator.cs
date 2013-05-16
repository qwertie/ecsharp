using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.CompilerCore;
using Node = Loyc.Syntax.LNode;

namespace Ecs.Parser
{
	using LS = TokenType;

	public class EcsParserGenerator : LlpgHelpers
	{
		public static readonly Symbol _id = GSymbol.Get("id");
		public Pred id { get { return Sym(_id); } }

		LLParserGenerator _pg;

		public void GenerateParserCode()
		{
			_pg = new LLParserGenerator(new PGCodeGenForSymbolStream());
			_pg.OutputMessage += (node, pred, type, msg) => {
				object subj = node == Node.Missing ? (object)pred : node;
				Console.WriteLine("--- at {0}:\n--- {1}: {2}", subj.ToString(), type, msg);
			};

			// FUTURE IDEA for simple "rewrite rules":
			// Look for \(...) inside code blocks, and automatically do replacements...
			//
			// rule PrefixExpr() ==> #[
			//     op:=('\'|'.'|'-'|'+'|'!'|'~'|Inc|Dec) PrefixExpr -> { Call(op, \PrefixExpr) }
			//   | id                                               -> { (Node)id }
			// ];
			// rule DottedExpr ==> #[
			//   PrefixExpr ('.' PrefixExpr)+ 
			//   { Call(\'.', \[PrefixExpr+]) }
			// ];
			// ---means---
			// rule PrefixExpr() ==> #[
			//     op:=('\'|'.'|'-'|'+'|'!'|'~'|Inc|Dec) p0:=PrefixExpr { Call(op, p0) }
			//   | i0:=id                                               { (Node)i0 }
			// ];
			// rule DottedExpr ==> #[
			//   { InternalList<int> p0 = InternalList<int>.Empty; }
			//   p0+=PrefixExpr (c0:='.' p0+=PrefixExpr)+
			//   { Call(c0, p0) }
			// ];
			
			// Dictionary.[int, string]    // Nemerle style
			// Dictionary!(int, string)    // D-style
			// Dictionary(of int, string)  // VB-style
			// Dictionary<int, string>     // C# style

#if false
			What we need is a really simple grammar to bootstrap. Needs to accept input such as:

			namespace NS {
				[[LLLPG()]]
				public partial class Foo {
					public X ==> #[ ... ];
					public int Y(string str) ==> #[ ... ];
				}
			}

			That's pretty much it? No need for expressions, except var decls.

			WORD ATTRIBUTES ALLOWED:
			- On statements that start with keywords (except 'if')
			- On variable declarations, property declarations, and method declarations
			
			alias "[]" = TT.Bracks;
			alias "()" = TT.Parens;
			alias "{}" = TT.Braces;
			alias '.' = TT.Dot;
			alias ':' = TT.Colon;
			alias $Id = TT.Id;
			
			LNode ExprInside(Token group)
			{
				...
			}
			LNode ExprListInside(Symbol listName, Token group)
			{
			}
			RWList<LNode> ExprListInside(Token group)
			{
				return AppendExprsInside(group, new RWList<LNode>());
			}
			RWList<LNode> AppendExprsInside(Token group, RWList<LNode> list)
			{
				...
				return list;
			}
			bool ValueIs(int li, object value)
			{
				return object.Equals(LT(li).Value, value);
			}
			
			Symbol _spaceName; // to resolve the constructor ambiguity

		#region Complex identifiers and type names
			// The complex identifier in EC# is a subset of the language of expressions,
			// and data types are a superset of the language of complex identifiers.
			// Complex identifiers can appear in the following contexts:
			// - Space names: namespace Foo.Bar<\T> {...}
			//   (and yes, I want to support template parameters on namespaces someday)
			// - Method/property names: bool global::System.IEquatable<T>.Equals(T other)
			// Data types can appear in the following contexts:
			// - Fields and properties: int* x { get; set; }
			// - Methods declarations: int[] f(Foo<T>[,][] x);
			// - Certain pseudo-functions: typeof(Foo<T[]>)
			// Note that complex identifiers can contain substitution expressions,
			// which, in turn, can contain expressions of any complexity, e.g. 
			// Foo<\(x*y(++z))>. Of course, complex identifiers also appear within 
			// normal expressions, but the expression grammar doesn't use the
			// main "ComplexId" rule, instead it's handled somewhat separately.
			
			// := saves the whole token ... user should be able to define separate 
			// match methods when the return value is saved. LATER.
			LNode IdOrTypeKw ==> #[
				{LNode t;}
				(t=$Id | t=$TypeKeyword)
				{return F.Id((Symbol)t.Value);}
			];
			LNode Atom ==> #[
				{LNode r;}
				( t:="()"       {r = F.InParens(ExprInside(t));}
				| '\\' e:=IdOrTypeKw   {r = F.Call(S.Substitute, e);}
				| '\\' t:="()"  {r = F.Call(S.Substitute, ExprInside(t));}
				| '.' e:=Atom   {r = F.Call(S.Dot, e);}
				| r=IdOrTypeKw
				) {return r;}
			];
			
			bool _typeContext;
			LNode DataType ==> #[
				{_typeContext=true;} ComplexId {_typeContext=false;}
			];
			LNode ComplexId ==> #[ 
				e:=Atom RestOfId(ref e)
				(&{_typeContext} TypeSuffixOpt(ref e))
				{return e;}
			];
			void RestOfId(ref LNode r, bool tc) ==> #[
				(L:=TParams {L.Insert(0, r); r=F.Call(S.Of, L.ToRVList();})?
				DotRestOfId(ref r, tc)?
			];
			void DotRestOfId(ref LNode r) ==> #[
				'.' e:=Atom {r=F.Dot(r, e)}
				RestOfId(ref n)
			];
			LNode TParams ==> #[
				{RWList<LNode> a;}
				( '<' a+=ComplexId (',' a+=ComplexId)* '>' 
				| '.' t:="[]" {return ExprListInside(t);}
				| '!' t:="()" {return ExprListInside(t);}
				) {return a;}
			];
			TypeSuffixOpt(ref LNode e) ==> #[
				{int count;}
				(	'?' {e = F.Of(S.QuestionMark, e);}
				|	'*' {e = F.Of(S._Pointer, e);}
				|	{var dims = InternalList<int>.Empty;}
					(&{(count=CountDims(LT(\LI))) > 0} "[]" {dims.Add(count);})+
					{
						for (int i = dims.Count-1; i >= 0; i--)
							e = F.Of(S.GetArrayKeyword(dims[i]), e);
					}
				)?
			];
		#endregion
		
		#region Attributes
		
			// NOTE TO SELF: DO NOT HANDLE "new" AS AN ATTRIBUTE YET!
			RWList<LNode> _attrs = new RWList<LNode>();
			int _wordAttrCount = 0;
			
			NormalAttributes ==> #[
				{_attrs.Clear();}
				(	t:="[]" {AppendExprsInside(t, _attrs);} )*
				greedy({_attrs.Add(F.Id((Symbol)LT(0).Value));} $AttrKeyword)*
			];
			
			// "word attributes" are a messy feature of EC#. C# has a few non-
			// keyword attributes that appear at the beginning of a statement, such
			// as "partial", "yield" and "async". EC# generalizes this concept to 
			// "word attributes", which can be any normal identifier.
			//
			// BTW: "word attributes" are not really "contextual keywords" in the C# 
			// sense, because the identifiers are not recognized by the parser. The
			// only true contextual keyword in EC# is "var" ("var" is not a contextual
			// keyword in C# in the same way; in EC#, "var" is identified as a keyword
			// by the parser, but in C# it is identified during semantic analysis.)
			//
			// Since they're not keywords, word attributes are difficult because:
			// 1. Lots of statements start with words that are not word attributes, 
			//    such as "foo=0", "foo* x = null", "foo x { ... }", and "foo x();"
			//    Somehow we have to figure out which words are attributes.
			// 2. Not all statements accept word attributes. But the attributes 
			//    appear before the statement, so how can we tell if we should
			//    be expecting word attributes or not?
			// Using an LL(k) parser generator is quite limiting in this situation;
			// normal lookahead is strictly limited and we can't backtrack except 
			// via "and" predicates or custom code. On the other hand, I still think 
			// LL(k) is the best way to parse, precisely because it minimizes 
			// backtracking. So I've used and-predicates here to explicitly look 
			// ahead, and I only look ahead far enough to figure out "is this word 
			// an attribute?", not "what kind of statement is this?"
			// 
			// There are lots of "keyword-based" statements that accept word
			// attributes (think "partial class" and "yield return"), which are easy
			// to handle. Our main difficulty is the non-keyword statements:
			// 1. statements that can begin with an identifier, but also accept
			//    word attributes:
			//    - word type* name ... // field, property or method
			//    - word type? name ... // field, property or method
			//    - word type[,][] name ... // field, property or method
			//    - word type<x> name ... // field, property or method
			//    - word type name(); // method decl
			//    - word type name<x>(); // method decl
			//    - word type<x> name(); // method decl
			//    - word type<x> name<x>(); // method decl
			//    - word this();        // except inside methods
			//    - word this<x>();  // except inside methods
			//    - word label:
			// 2. statements that can start with an identifier, and do not accept 
			//    word attributes:
			//    - foo(0);
			//    - foo<x>();
			//    - foo = 0;
			//    - foo - bar;
			// Notice that if a word is followed by an operator of any kind, it 
			// can't be an attribute; but if it is followed by another word, it's
			// unclear. In particular, notice that for "A B<x>...", "A" is sometimes
			// an attribute and sometimes not. In "A B<x> C", A is an attribute,
			// but in "A B<x>()" it is a return type. The same difficulty exists
			// in case of alternate generics specifiers such as "A B.[x]".
			[token] int WordAttributes() ==> #[
				{int words = 0;}
				(	{_attrs.Add(F.Id((Symbol)LT(0).Value));}
				   $AttrKeyword 
				|	{_attrs.Add(F.Id("#" + LT(0).Value.ToString()));}
					&!{((Symbol)LT(\LI).Value).Name.StartsWith("#")} $Id {words++;}
					&(	TypeName $Id
					|	&{_spaceName!=S.Def} "this"
					|	"checked" "{}" | "unchecked" "{}" | "default" ':'
					|	("namespace" | "class" | "struct" | "interface" | "enum" | "delegate"
						| "event" | "case" | "break" | "continue" | "do" | "fixed" | "for"
						| "foreach" | "goto" | "lock" | "return" | "switch" | "throw" | "try" | "using" | "while")
					)
				)
				{return words;}
			];
			
			public static readonly Symbol _assembly = GSymbol.Get("assembly");
			public static readonly Symbol _module = GSymbol.Get("module");
			AsmOrModLabel ==> #[ 
				&{LT(\LI).Value==_assembly || LT(\LI).Value==_module} $Id ':'
			];
			AssemblyOrModuleAttribute ==> #[
				// not needed to bootstrap
				// new feature needed: \&Foo creates IsFoo() and calls it
				&{Down(\LI) && Up(\&AsmOrModLabel)} t:="[]" 
				(=> {Down(t);} AsmOrModLabel L:=ExprList {Up();})
				{return L;}
			];
		
		#endregion

			Expr ==> #[
				r:=ComplexId
				{return r;}
			];

			Stmt ==> #[
				(	AssemblyOrModuleAttribute
				|	NormalAttributes
					nWords:=WordAttributes
					(	SpaceStmt // struct, class, namespace, interface, enum, trait, alias
					|	VarStmt
					|	Property
					|	Event
					|	MethodStmt
					|	DelegateStmt
					|	LabelStmt // includes default:
					|	BreakStmt
					|	CaseStmt
					|	CheckedStmt
					|	ContinueStmt
					|	DoStmt
					|	EventStmt
					|	FixedStmt
					|	ForStmt
					|	ForEachStmt
					|	GotoStmt
					|	&{nWords==0} IfStmt
					|	LockStmt
					|	ReturnStmt
					|	SwitchStmt
					|	ThrowStmt
					|	TryStmt
					|	UncheckedStmt
					|	UsingStmt
					|	WhileStmt
					|	&{nWords==0}
					|	default &{nWords==0} ExprStmt)
				)
			];
			RWList<LNode> _words;
			bool _hasWords;


			
			static foo bar public gah blah < 5;


			Rule ==> #[ a? &b c* {foo;} ];
		
			// [[LLLPG]] macro produces...
			class Parser {
				[[LllpgCodeGen]]
				void Rule()
				{
//					#?(a) + #&(b) + #*(c) + {foo;};
				}
			}


			

#endif







			// Okay, so EC# code is pretty much just a bunch of statements...
			// and statements contain expressions.
			var StartExpr = Rule("StartExpr", null, Start);
			var Parens = Rule("Parens", Sym(LS.RParen) + LS.RParen);
			// id | \(id | '(' ')' )
			var IdPart = Rule("IdPart", Stmt("LNode n") +
				SetVar("id", id) + Stmt("n = F.Id(id)") 
				| Sym(@"\") + (id | Parens) + Stmt(""));
			var ComplexId = Rule("ComplexId", IdPart + Star(Sym("#.") + IdPart));
			
			StartExpr.Pred = ComplexId;

			var stmt = Rule("Stmt", Sym(""), Start); // completed later
			var UsingDirective = Rule("UsingDirective", LS.@using + ComplexId);
			var UsingStmt = Rule("UsingStmt", Sym(LS.@using) + LS.LParen + StartExpr + LS.RParen + stmt);
			var Stmts = Rule("Stmts", Star(stmt), Start);
			stmt.Pred = UsingDirective | UsingStmt;
		}
	}
}
