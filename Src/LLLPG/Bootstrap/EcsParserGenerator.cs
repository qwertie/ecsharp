using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.LLParserGenerator;
using Loyc.CompilerCore;
using EP = Ecs.EcsPrecedence;

namespace Ecs.Parser
{
	using LS = TokenType;
	using Loyc.Syntax;
	using Loyc.Utilities;

	/// <summary>I intended to use this class to generate an EC# parser.
	/// My plan has changed: I will use LEL code to generate an EC# parser.
	/// This code will be deleted eventually.</summary>
	public class EcsParserGenerator : LlpgHelpers
	{
		public static readonly Symbol _id = GSymbol.Get("id");
		public Pred id { get { return Lit(_id); } }

		LLParserGenerator _pg;

		public void GenerateParserCode()
		{
			_pg = new LLParserGenerator(new GeneralCodeGenHelper(), MessageSink.Console);

			// FUTURE IDEA for simple "rewrite rules":
			// Look for $(...) inside code blocks, and automatically do replacements...
			//
			// rule PrefixExpr() ==> @[
			//     op:=('\'|'.'|'-'|'+'|'!'|'~'|Inc|Dec) PrefixExpr -> { Call(op, $PrefixExpr) }
			//   | id                                               -> { (Node)id }
			// ];
			// rule DottedExpr ==> @[
			//   PrefixExpr ('.' PrefixExpr)+ 
			//   { Call($'.', $\[PrefixExpr+]) }
			// ];
			// ---means---
			// rule PrefixExpr() ==> @[
			//     op:=('\'|'.'|'-'|'+'|'!'|'~'|Inc|Dec) p0:=PrefixExpr { Call(op, p0) }
			//   | i0:=id                                               { (Node)i0 }
			// ];
			// rule DottedExpr ==> @[
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
					public X ==> @[ ... ];
					public int Y(string str) ==> @[ ... ];
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
			
			static readonly _global = GSymbol.Get("global");
			public HashSet<Symbol> ExternAliases = new HashSet<Symbol>(new[] { _global });
			
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
			bool Is(int li, Symbol value)
			{
				return LT(li).Value == value;
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
			LNode IdOrTypeKw ==> @[
				{LNode t;}
				(t=$Id | t=$TypeKeyword)
				{return F.Id((Symbol)t.Value);}
			];
			LNode Atom ==> @[
				{LNode r;}
				( t:="()"       {r = F.InParens(ExprInside(t));}
				| '\\' e:=IdOrTypeKw   {r = F.Call(S.Substitute, e);}
				| '\\' t:="()"  {r = F.Call(S.Substitute, ExprInside(t));}
				| '.' e:=Atom   {r = F.Call(S.Dot, e);}
				| r=IdOrTypeKw
				) {return r;}
			];
			
			LNode DataType ==> @[
				ComplexId
				TypeSuffixOpt(ref e)
			];
			[token] LNode ComplexId ==> @[
				e:=Atom 
				// There can be only a single "externAlias::" prefix in a complex 
				// ident. (any additional uses of "::" can be interpreted by the
				// expression parser as quick binds, but that's not our job here.)
				(	&{ExternAliases.Contains(e.Name) && e.IsId} "::" e2=Atom 
					{e = F.Call(S.ColonColon, e, e2);}
				)?
				|	RestOfId(ref e)
				{return e;}
			];
			void RestOfId(ref LNode r, bool tc) ==> @[
				// "&TParams => TParams" means "scan ahead to verify that there 
				// really is a TParams here (not just a less-than operator). If 
				// there is, match it with no prediction step." The gate "=>" 
				// suppresses prediction, which would be redundant after having
				// just verified the entire TParams (it is neccessary to suppress
				// prediction explicitly because LLLPG doesn't "understand" and-
				// predicates enough to know that it's repeating the same work.)
				(&TParams => L:=TParams {L.Insert(0, r); r=F.Call(S.Of, L.ToRVList());})?
				DotRestOfId(ref r, tc)?
			];
			void DotRestOfId(ref LNode r) ==> @[
				'.' e:=Atom {r=F.Dot(r, e)} RestOfId(ref n)
			];
			LNode TParams ==> @[
				{RWList<LNode> a;}
				( '<' a+=ComplexId (',' a+=ComplexId)* '>' 
				| '.' t:="[]" {return ExprListInside(t);}
				| '!' t:="()" {return ExprListInside(t);}
				) {return a;}
			];
			bool TypeSuffixOpt(ref LNode e) ==> @[
				{int count;}
				(	'?' {e = F.Of(S.QuestionMark, e);}
				|	'*' {e = F.Of(S._Pointer, e);}
				|	{var dims = InternalList<int>.Empty;}
					(&{(count=CountDims(LT(\LI))) > 0} "[]" {dims.Add(count);})+
					{
						for (int i = dims.Count-1; i >= 0; i--)
							e = F.Of(S.GetArrayKeyword(dims[i]), e);
					}
				|	{return false;}
				)	{return true;}
			];
		#endregion
		
		#region Attributes
		
			// NOTE TO SELF: DO NOT HANDLE "new" AS AN ATTRIBUTE YET!
			RWList<LNode> _attrs = new RWList<LNode>();
			int _wordAttrCount = 0;
			
			NormalAttributes ==> @[
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
			[token] int WordAttributes() ==> @[
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
			AsmOrModLabel ==> @[ 
				&{LT(\LI).Value==_assembly || LT(\LI).Value==_module} $Id ':'
			];
			AssemblyOrModuleAttribute ==> @[
				// not needed to bootstrap
				// new feature needed: \&Foo creates IsFoo() and calls it
				&{Down(\LI) && Up(\&AsmOrModLabel)} t:="[]" 
				(=> {Down(t);} AsmOrModLabel L:=ExprList {Up();})
				{return L;}
			];
		
		#endregion

		#region Expressions

			// Parsing EC# expressions is very tricky. Here are some of the things 
			// that make it difficult, especially in an LL(k) parser:
			//    
			// 1. Parenthesis: is it a cast or a normal expression?
			//    (A<B,C>)y      is a cast
			//    (A<B,C>)-y     is NOT a cast
			//    (A<B,C>)(-y)   is a cast
			//    (A<B,C>)(as B) is NOT a cast (well, the second part is)
			//    x(A<B,C>)(-y)  is NOT a cast
			//    -(A<B,C>)(-y)  is a cast
			//    \(A<B,C>)(-y)  is NOT a cast
			//    (A<B,C>){y}    is a cast
			//    (A<B,C>)[y]    is NOT a cast
			//    (A<B,C>)++(y)  is NOT a cast (it's post-increment and call)
			//    (A<B> C)(-y)   is NOT a cast
			//    ([] A<B,C>)(y) is NOT a cast (empty attr list defeats cast)
			//    (A+B==C)y      is nonsensical
			//    x(A<B,C>)y     is nonsensical
			// 2. In-expr var declarations: is "(A<B,C>D)" a variable declaration?
			//    (A<B,C>D) => D.Foo() // variable declaration
			//    (A<B,C>D) = Foo()    // variable declaration
			//    (f1, f2) = (A<B,C>D) // tuple
			//    Foo(A<B,C>D)         // method with two arguments
			//    void Foo(A<B,C>D)    // variable declaration at statement level
			//    Foo(A<B,C>D)         // variable declaration at statement level if 'Foo' is the space name
			// 3. Less-than: is it a generics list or an operator? Need unlimited lookahead.
			//    (A<B,C)    // less-than operator
			//    (A<B,C>)   // generics list
			//    (A<B,C>D)  // context-dependent
			// 4. Brackets. Is "Foo[]" a data type, or an indexer with no args?
			//    (Foo[]) x; // data type: #of(#[], Foo)
			//    (Foo[]).x; // indexer:   #[](Foo)
			// 5. Does "?" make a nullable type or a conditional operator?
			//    Foo<B> ? x = null;     // nullable type
			//    Foo<B> ? x = null : y; // conditional operator
			
			// The Ambiguity flags help communicate contextual nuances between the 
			// rules, to distinguish some of the above cases.
			[Flags]
			enum Ambiguity { 
				// Inputs
				AllowUnassignedVarDecl = 1
				StatementLevel = 2,
				StopAtArgumentList = 8, // helps parse method declaration
				ExpectCast = 4,         // used in (...)x
				ExpectType = 8,         // used in typeof(...)

				// Outputs
				BlankIndexed = 0x0040,
				TypeSuffix = 0x0080,
				IsExpr = 0x0100,        // implies "not a type"
				IsCall = 0x0200,        // implies "not a type"
				HasAttrs = 0x0400,    // defeats cast
				IsTuple = 0x0800,     // multiple arguments (or no arguments)
				Error = 0x1000
				NotAType = IsExpr|IsCall,
				
				Type = BlankIndexed | TypeSuffix,
			}

			static readonly int MinPrec = Precedence.MinValue.Lo;
			/// <summary>Context: beginning of statement (#namedArg not supported, allow multiple #var decl)</summary>
			public static readonly Precedence StartStmt      = new Precedence(MinPrec, MinPrec, MinPrec);
			/// <summary>Context: beginning of expression (#var must have initial value)</summary>
			public static readonly Precedence StartExpr      = new Precedence(MinPrec+1, MinPrec+1, MinPrec+1);
			/// <summary>Context: middle of expression, top level (#var and #namedArg not supported)</summary>
			public static readonly Precedence ContinueExpr   = new Precedence(MinPrec+2, MinPrec+2, MinPrec+2);
		
			// Combines the previous precedence floor with that of a prefix operator
			Precedence RP(Precedence prev, Precedence pre) {
				return new Precedence(prev.Lo, prev.Hi, prev.Left, Math.Max(prev.Right, pre.Right));
			}
			
			AtomOrPrefixOp(Precedence p, ref Ambiguity f) ==> @[
				(	t:=$Id                   {r = F.Id((Symbol)t.Value);}
				|	default t:=$TypeKeyword {r = F.Id((t.Value as Symbol) ?? F._Missing);}
					&{p.CanParse(EP.Prefix)}
					t:=('-'|'+'|'~'|'!'|"++"|"--"|'&'|'*')
					e:=Expr(RP(p, EP.Prefix), ref f)
					{
						r = F.Call((Symbol)t.Value, e);
						f |= Ambiguity.IsExpr;
					}
				|	// TODO: consider converting "==>" to a binary operator
					&{p.CanParse(EP.Forward)}
					"==>"
					e:=Expr(RP(p, EP.Forward), ref f)
					{
						r = F.Call(S.Forward, e);
						f |= Ambiguity.IsExpr;
					}
				|  '\\'
					e:=Expr(EP.Substitute, ref f)
					{r = F.Call(S.Substitute, e.IsParenthesizedExpr ? e.Args[0] : e);}
				|  t:=('.'|"::")
					e:=Expr(EP.Substitute, ref f)
					{r = F.Call((Symbol)t.Value, e);}
				|	r=NewExpr
				|	r=CastOrParens(p)
				)
			];
			
			NewExpr ==> @[ 
				{LNode r; Ambiguity f = 0;}
				"new" 
				
				typeAndCons:=Expr(EP.Primary, ref f);
				{                     
					if ((f & Ambiguity.NotAType) != 0)
						Error(typeAndCons, "Type expected after 'new'");
					return 
				}
				(	t:="{}"
					{
						var list = ExprListInside(t);
						list.Insert(0, typeAndCons);
						r = F.Call(S.New, list.ToRVList());
					}
				|	{r = F.Call(S.New, typeAndCons);}
				{return r;}
			];
			
			CastOrParens(Parecedence p) ==> @[
				// A cast requires...
				// - Precedence floor of Prefix or lower
				// - Must be followed by $Id, a literal, "{}", $Id, '\\', 
				//   or a "()" that is not one of the new cast operators
				// - That the initial parens are a data type
				// - That the initial parens are not an argument list 
				//   (if so this rule is not called in the first place)
				{LNode r; Ambiguity f;}
				(	&{p.CanParse(EP.Prefix)}
					t:="()"
					&('\\'|$Id|$SQString|$DQString|$Number|"{}" | &!{IsNewCastOp(\LI)} "()")
					{r = ExprOrTupleInside(t, ref f, false);}
					(	=>	// Block further lookahead
						(	// We still don't know if it's a cast until this part runs
							&{(f & (Ambiguity.NotAType|Ambiguity.HasAttrs|Ambiguity.Tuple)) == 0}
							{f = 0;}
							subject:=Expr(RP(p, EP.Prefix), ref f)
							{
								if ((f & Ambiguity.TypeSuffix) != 0)
									Error(subject, "Syntax error: data type not expected here");
								r = F.Call(S.Cast, subject, r);
							}
						|	{	// not a cast
								if ((f & Ambiguity.Tuple) == 0)
									r = F.InParens(r);
							}
						)
					)
				/	t:="()" &('=' | "=>")
					{f = Ambiguity.AllowUnassignedVarDecl;}
					{r = ExprOrTupleInside(t, ref f, true);}
				/	t:="()"
					{f = 0;}
					{r = ExprOrTupleInside(t, ref f, true);}
				)	{return r;}
			];

			LNode ExprOrTupleInside(Token t, ref Ambiguity f, bool saveParensIfNotTuple)
			{
				var c = t.Children;
				Debug.Assert(c != null); // "()", "[]" and "{}" always have children
				if (c.Count == 0) {
					f.Tuple = true;
					return F.@void; // "()" is the void literal
				}
				Down(t);
				f = Ambiguity.ExpectCast;
				r = Up(ExprOrTuple(StartExpr, f));
				if (saveParensIfNotTuple && (f & Ambiguity.Tuple) == 0)
					r = F.InParens(r);
				return r;
			}

			LNode ExprOrTuple(ref Ambiguity f) ==> @[
				first:=Expr(StartExpr, ref f);
				(	{Ambiguity f0 = f; RWList<LNode> list = null; LNode next;}
					(	',' 
						{f=f0;}
						(	next=Expr(StartExpr, ref f)
						|	{next=F._Missing;}
						)
						{
							list = list ?? new RWList<LNode> {first};
							list.Add(next);
							f |= Ambiguity.Tuple;
						}
					)*   {return F.Call(S.Tuple, list.ToRVList());}
				/	_ => {return r;}
				)
			];
			
			bool IsNewCastOp(int li)
			{
				var c = LT(li).Children;
				if (c != null && c.Count != 0)
					return IsNewCastOp(c[0].Type);
			}
			bool IsNewCastOp(TT tt)
			{
				return tt == TT.@is || tt == TT.@as || tt == TT.@using || tt == TT.PtrArrow;
			}

			Expr(Precedence p, ref Ambiguity f) ==> @[
				(&{p.CanParse(ContinueExpr)} NormalAttributes)?
				
				AtomOrPrefixOp(p, ref f)
			];
		
		#endregion	

		#region Statements
		
			// Ids at beginning of a Stmt
			List<Triplet<int, LNode, bool>> _startingIds = new List<Triplet<int, LNode, bool>>();
			
			// Parsing C# statements, and by extension EC#, can be a messy business.
			// But I think I've figured out how to parse it efficiently with the 
			// humble LLLPG. The challenge is that:
			// - Lots of statements can start with an arbitrary number of "word 
			//   attributes" which are identifiers that are not keywords or data types.
			// - Methods, properties and variables all start with a pair of complex
			//   identifiers, e.g. a statement that starts with "Foo<T> IList<T>.Count"
			//   could be a method or property depending on what comes next.
			// - Variable/method syntax and expression syntax can be indistinguishable, 
			//   e.g. a * b(); looks like both a multiplication and a method declaration.
			//   (for this kind of ambiguity, the code is always assumed to be a 
			//   declaration rather than an expression.)
			// Obviously, it is impossible to use a simple list of alternatives like
			// "MethodStmt | PropertyStmt | VarStmt", since distinguishing between 
			// these three things requires unlimited lookahead, even if there are no
			// word attributes.
			//
			// The following statement types can use word attributes:
			// - Methods: partial void Main()
			// - Vars:    partial int x;
			// - Spaces:  partial class Foo {}
			// - partial alias x = 5;
			Stmt ==> @[
				(	AssemblyOrModuleAttribute
				|	NormalAttributes
					
					// Gather up all the complex identifiers at the beginning of the 
					// statement into a list.
					firstComplex:=GatherStartingIds
					{var list = _startingIds;}
					{int c = list.Count, i;}
					
					// Once we're past those pesky identifiers, we can easily 
					// distinguish between the types of statements.
					{LNode r;}
					(	&(firstComplex==-1) r=EventDecl
					|	&(firstComplex==-1) r=DelegateDecl
					|	&(firstComplex==-1) r=SpaceDecl
					|	&{c>=2}             r=FinishNonKeywordSpaceDecl
					/	&{c>=2}             r=FinishVarDecl
					|	&{c>=2}             r=FinishPropertyDecl
					|	&{c>=2}             r=FinishMethodDecl
					/*|	LabelStmt // includes default:
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
					|	WhileStmt*/
					|	&{c<2}              r=FinishExprStmt()
					)
				)
			];
			
			bool GatherStartingIds ==> @[
				{_startingIds.Clear();}
				{bool firstComplex = -1;}
				(
					(	// If we see an attribute keyword (e.g. 'static') after some 
						// identifiers, those identifiers must have been attributes.
						t:=$AttrKeyword 
						{
							if (firstComplex != -1) {
								// maybe user forgot a semicolon?
								Error("Syntax error: attribute keyword '{0}' is unexpected here", 
									t.Value.ToString().Substring(1));
							} else {
								foreach (var triplet in _startingIds)
									_attrs.Add(F.Id((Symbol)triplet.Item2.Value));
							}
							_startingIds.Clear();
						}
					|	{int startPos = _inputPosition;}
						id:=ComplexId
						hasSuf:=TypeSuffixOpt(ref id)
						{
							int len = _inputPosition - startPos;
							if (len > 1 && firstComplex == -1)
								firstComplex = _startingIds.Count;
							_startingIds.Add(Pair.Create(len, id, hasSuf));
						}
					)
				)*
				{return firstComplex;}
			];
			
			int FlushIds(int leftOver)
			{
				int c = _startingIds.Count, attrs = c - leftOver;
				Debug.Assert(c >= leftOver);
				if (attrs > 0) {
					bool error = false;
					for (int i = 0; i < attrs; i++) {
						var triplet = _startingIds[i];
						if (triplet.B.IsIdent) {
							_attrs.Add(F.Id("#" + triplet.B.Name.ToString()));
						} else if (!error) {
							error = true;
							Error(triplet.A, "Syntax error: too many complex identifiers in a row.");
						}
					}
					_startingIds.RemoveRange(0, attrs);
				}
			}
			
			LNode EventDecl ==> @[
				t:="event" {FlushIds(0);}
				GatherStartingIds
				(	&{_startingIds.Count>=2} r:=FinishPropertyDecl(true) {return r;}
				|	{return Error(t, "Syntax error: 'event' is missing data type or name afterward");}
				)
			];
			LNode DelegateDecl ==> @[
				t:="delegate" {FlushIds(0);}
				GatherStartingIds
				(	&{_startingIds.Count>=2} r:=FinishMethodDecl(true) {return r;}
				|	{return Error(t, "Syntax error: 'delegate' is missing data type or name afterward");}
				)
			];
			static readonly Symbol _where = GSymbol.Get("where");
			LNode SpaceDecl ==> @[
				{FlushIds(0);}
				{Token t;}
				(t="namespace" | t="class" | t="struct" | t="interface" | t="enum")
				{Symbol kind = (Symbol)t.Value;}
				name:=ComplexId
				bases:=BaseListOpt
				WhereClausesOpt(ref name)
				(	';'               {return F.Call(kind, name, bases);}
				|	body:=BracedBlock {return F.Call(kind, name, bases, body);}
				)
				&{} $Id
			];
			LNode BaseListOpt ==> @[
				{RWList<LNode> bases = null;}
				(	':' @base:=DataType {bases ??= new RWList<LNode>(); bases.Add(@base);}
				 	(',' bases+=DataType {bases.Add(@base);})*
				)?
				{return bases == null ? F.EmptyList : F.List(bases.ToRVList());}
			];
			static readonly Symbol _trait = GSymbol.Get("trait");
			static readonly Symbol _alias = GSymbol.Get("alias");
			static readonly Symbol __trait = GSymbol.Get("#trait");
			static readonly Symbol __alias = GSymbol.Get("#alias");
			LNode FinishNonKeywordSpaceDecl ==> @[
				{int i;}
				&{(i=NonKeywordSpaceDeclKeywordIndex()) != -1}
				=> (
					{	// Backtrack if necessary, then flush word attrs
						int c = _startingIds.Count;
						if (i + 2 < c) {
							_inputPosition = _startingIds[i+2].A;
							_startingIds.Resize(i+2);
						}
						FlushIds(2);
						Symbol kind = _startingIds[0].B.Name; // _trait or _alias
						Symbol kind2 = (kind == _trait ? __trait : __alias);
					}
					{LNode name;}
					(	&{kind==_alias} =>
						name=FinishExprStmt()
					|	{Debug.Assert(_kind==_trait);}
						name=_startingIds[1].B;
					)
					bases:=BaseListOpt
					WhereClausesOpt(ref name)
					(	';'               {return F.Call(kind2, name, bases);}
					|	body:=BracedBlock {return F.Call(kind2, name, bases, body);}
					)
				)
			];
			LNode BracedBlock ==> @[
				t:="{}" (=>
					{Down(t);} 
					list:=Stmts 
					{Up(); return F.Braces(list.ToRVList());}
				)
			];
			
			void WhereClausesOpt(ref LNode name) ==> @[
				// TODO: add 'where' clauses to type name
			];
			int NonKeywordSpaceDeclKeywordIndex()
			{
				// Non-space decls like "partial trait X {...}" will normally have 
				// the word 'trait' or 'alias' as the second-to-last word in 
				// _startingIds. However, in the case of "trait X<T> where T : class",
				// the word 'trait' comes earlier. This method figures that out.
				LNode id;
				Symbol name;
				for (int i = 0, c = _startingIds.Count - 1; i < c; i++)
					if ((id = _startingIds[0].B).IsIdent && 
						((name = id.Name) == _trait || name == _alias)
						return i;
				return -1;
			}
			
			LNode FinishVarDecl ==> @[
				{FlushIds(2);}
			];
			LNode FinishPropertyDecl(bool isEvent = false) ==> @[
				{FlushIds(2);}
				
			];
			LNode FinishMethodDecl(bool isDelegate = false) ==> @[
				{FlushIds(2);}
			];
			LNode FinishExprStmt() ==> @[
				&{do something special when there's a _startingId} ...
				| Expr(StartStmt)
			];
		
		#endregion
		
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
			var Parens = Rule("Parens", Lit(LS.RParen) + T(LS.RParen));
			// id | \(id | '(' ')' )
			var IdPart = Rule("IdPart", Stmt("LNode n") +
				SetVar("id", id) + Stmt("n = F.Id(id)") 
				| Id(@"\") + (id | Parens) + Stmt(""));
			var ComplexId = Rule("ComplexId", IdPart + Star(Id("#.") + IdPart));
			
			StartExpr.Pred = ComplexId;

			var stmt = Rule("Stmt", Id(""), Start); // completed later
			var UsingDirective = Rule("UsingDirective", T(LS.@using) + ComplexId);
			var UsingStmt = Rule("UsingStmt", T(LS.@using) + T(LS.LParen) + StartExpr + T(LS.RParen) + stmt);
			var Stmts = Rule("Stmts", Star(stmt), Start);
			stmt.Pred = UsingDirective | UsingStmt;
		}

		TerminalPred T(LS tt)
		{
			LNode node = F.Dot("TT", tt.ToString());
			return new TerminalPred(node, new PGNodeSet(new[] { node }), true);
		}
	}
}
