using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Utilities;

namespace Loyc.Syntax
{
	/// <summary>
	/// A list of common symbols that, by convention, have special meaning:
	/// operators, built-in data types, keywords, trivia, etc.
	/// </summary>
	/// <remarks>
	/// Code that can use symbol forms directly, such as "'!=", tends to be very compact.
	/// The symbols in this class tend to be abbreviated in order to make usages of this 
	/// class more compact (e.g. <see cref="NotEq"/> is short like its corresponding 
	/// symbol "'!="). In C# one can access these symbols more easily with 
	/// <c>using static Loyc.Syntax.CodeSymbols</c> or with
	/// <c>using S = Loyc.Syntax.CodeSymbols</c> as the Loyc codebase does.
	/// <para/>
	/// Some symbols have an alternate name that starts with an underscore. For example,
	/// <c>_Negate</c> represents the unary minus operator, but in fact it is the same
	/// symbol as the subtraction operator <c>Sub</c>, <c>'-</c>.
	/// </remarks>
	public partial class CodeSymbols
	{
		// Plain C# operators (node names)
		public static readonly Symbol Mul = GSymbol.Get("'*");     //!< "*" Multiplication (or dereference)
		public static readonly Symbol Div = GSymbol.Get("'/");     //!< "/" Division
		public static readonly Symbol Add = GSymbol.Get("'+");     //!< "+" Addition or unary +
		public static readonly Symbol Sub = GSymbol.Get("'-");     //!< "-" Subtraction or unary -
		public static readonly Symbol _Dereference = GSymbol.Get("'*"); //!< Alias for Mul
		public static readonly Symbol _Pointer = GSymbol.Get("'*");     //!< Alias for Mul
		public static readonly Symbol _UnaryPlus = GSymbol.Get("'+");   //!< Alias for Add
		public static readonly Symbol _Negate = GSymbol.Get("'-"); //!< Alias for Sub. Infix and prefix operators use same symbol
		public static readonly Symbol PreInc = GSymbol.Get("'++"); //!< "++" Unary prefix increment
		public static readonly Symbol PreDec = GSymbol.Get("'--"); //!< "--" Unary prefix decrement
		public static readonly Symbol PostInc = GSymbol.Get("'suf++"); //!< "suf++" Unary suffix increment
		public static readonly Symbol PostDec = GSymbol.Get("'suf--"); //!< "suf--" Unary suffix decrement
		public static readonly Symbol Mod = GSymbol.Get("'%");    //!< "%"  Remainder operator
		public static readonly Symbol And = GSymbol.Get("'&&");   //!< "&&" Logical short-circuit 'and' operator
		public static readonly Symbol Or = GSymbol.Get("'||");    //!< "||" Logical short-circuit 'or' operator
		public static readonly Symbol Xor = GSymbol.Get("'^^");   //!< "^^" Logical 'xor' operator (tentative--this operator is redundant, "!=" is equivalent)
		public static readonly Symbol Eq = GSymbol.Get("'==");    //!< "==" Equality test operator
		public static readonly Symbol NotEq = GSymbol.Get("'!="); //!< "!=" Inequality test operator
		[Obsolete("Use NotEq instead")]
		public static readonly Symbol Neq = GSymbol.Get("'!=");   //!< "!=" Inequality test operator
		public static readonly Symbol GT = GSymbol.Get("'>");     //!< ">"  Greater-than operator
		public static readonly Symbol GE = GSymbol.Get("'>=");    //!< ">=" Greater-than-or-equal-to operator
		public static readonly Symbol LT = GSymbol.Get("'<");     //!< "<"  Less-than operator
		public static readonly Symbol LE = GSymbol.Get("'<=");    //!< "<=" Less-than-or-equal-to operator
		public static readonly Symbol Matches = GSymbol.Get("'=~");    //!< "=~" Pattern match test operator
		public static readonly Symbol Compare = GSymbol.Get("'<=>");    //!< "<=>" Three-way comparison a.k.a. shaceship operator
		public static readonly Symbol Shr = GSymbol.Get("'>>");   //!< ">>" Right-shift operator
		public static readonly Symbol Shl = GSymbol.Get("'<<");   //!< "<<" Left-shift operator
		public static readonly Symbol Not = GSymbol.Get("'!");    //!< "!"  Logical 'not' operator
		public static readonly Symbol Assign = GSymbol.Get("'="); //!< "="  Assignment operator
		public static readonly Symbol OrBits = GSymbol.Get("'|");     //!< "|" Bitwise or operator
		public static readonly Symbol AndBits = GSymbol.Get("'&");    //!< "&" Bitwise and operator. Also, address-of (unary &)
		public static readonly Symbol _AddressOf = GSymbol.Get("'&"); //!< Alias for AndBits
		public static readonly Symbol NotBits = GSymbol.Get("'~");    //!< Unary bitwise inversion operator
		public static readonly Symbol _Concat = GSymbol.Get("'~");    //!< Alias for NotBits. Concat operator in D
		public static readonly Symbol _Destruct = GSymbol.Get("'~");  //!< Alias for NotBits.
		public static readonly Symbol XorBits = GSymbol.Get("'^");    //!< "^" Bitwise exclusive-or operator
		
		public static readonly Symbol Braces = GSymbol.Get("'{}"); //!< "{}" Creates a scope.
		public static readonly Symbol IndexBracks = GSymbol.Get("'suf[]"); //!< "'suf[]" indexing operator
																		   //!< foo[1, A] <=> @`'suf[]`(foo, 1, A), but in a type context, Foo[] <=> @'of(@`[]`, Foo)
		public static readonly Symbol Array = GSymbol.Get("'[]");  //!< Used for list/array literals. Not used for attributes.
		public static readonly Symbol _Bracks = Array;            //!< Synonym for Array (`'[]`)
		public static readonly Symbol TwoDimensionalArray = GSymbol.Get("'[,]"); //!< int[,] <=> @'of(@`#[,]`, int)

		// New Symbols for C# 5 and 6 (NullDot `?.` is defined elsewhere, since EC# already supported it)
		[Obsolete("Use EcsCodeSymbols.Async instead (Loyc.Ecs package)")]
		public static readonly Symbol Async = GSymbol.Get("#async"); //!< [#async] Task Foo(); <=> async Task Foo();
		                              // async is a normal contextual attribute so it needs no special parser support.
		[Obsolete("Use EcsCodeSymbols.Await instead (Loyc.Ecs package)")]
		public static readonly Symbol Await = GSymbol.Get("await"); //!< await(x); <=> await x; (TENTATIVE: should this be changed to #await?)
		public static readonly Symbol NullIndexBracks = GSymbol.Get("'?[]"); //!< "?[]" indexing operator of C# 6
		                              //!< @`?[]`(foo, #(1, A)) <=> foo?[1, A] (TENTATIVE, may be changed later)
		[Obsolete("Use EcsCodeSymbols.InitializerAssignment instead (Loyc.Ecs package)")]
		public static readonly Symbol InitializerAssignment = GSymbol.Get("'[]="); //!< @`[]=`(0, 1, x) <=> [0, 1]=x
		                              // (TENTATIVE, and only supported in 'new' initializer blocks)
		
		/// <summary># is used for lists of things in definition constructs, e.g. 
		///     <c>#class(Derived, #(Base, IEnumerable), {...})</c>.
		/// For a time, 'tuple was used for this purpose; the problem is that a
		/// find-and-replace operation intended to find run-time tuples could 
		/// accidentally match one of these lists. So I decided to dedicate # 
		/// for use inside special constructs; its meaning depends on context.
		/// </summary>
		public static readonly Symbol AltList = GSymbol.Get("#");
		public static readonly Symbol _HashMark = GSymbol.Get("#");

		public static readonly Symbol QuestionMark = GSymbol.Get("'?");    //!< "'?" Conditional operator. (a?b:c) <=> @`?`(a,b,c) and int? <=> @'of(@`'?`, int)
		public static readonly Symbol Of = GSymbol.Get("'of");             //!< "'of" for giving generic arguments. @'of(List,int) <=> List<int>
		public static readonly Symbol Dot = GSymbol.Get("'.");             //!< "'." binary dot operator, e.g. string.Join
		public static readonly Symbol NamedArg = GSymbol.Get("'::=");      //!< "'::=" Named argument e.g. `'::=`(x, 0) <=> x: 0
		public static readonly Symbol New = GSymbol.Get("'new");           //!< "'new": new Foo(x) { a } <=> @`'new`(Foo(x), a);  new[] { ... } <=> @`'new`(@`[]`(), ...)
		public static readonly Symbol NewAttribute = GSymbol.Get("#new");  //!< "#new": public new void Foo() {} <=> [#public, #new] #fn(#void, Foo, #(), {})
		public static readonly Symbol Out = GSymbol.Get("#out");           //!< "#out": out x <=> [#out] x
		public static readonly Symbol Ref = GSymbol.Get("#ref");           //!< "#ref": ref int x <=> [#ref] #var(#int, x)
		public static readonly Symbol Sizeof = GSymbol.Get("'sizeof");     //!< "'sizeof" sizeof(int) <=> @'sizeof(int)
		public static readonly Symbol Typeof = GSymbol.Get("'typeof");     //!< "'typeof" typeof(Foo) <=> @'typeof(Foo),
		                                                                   //!<           typeof<foo> <=> @'of(@'typeof, foo)
		public static readonly Symbol As = GSymbol.Get("'as");             //!< "'as":   @'as(x,string) <=> x as string <=> x(as string)
		public static readonly Symbol Is = GSymbol.Get("'is");             //!< "'is":   @'is(x,string) <=> x is string, @'is(x,#var(Foo,v),#(y,z)) <=> x is Foo v(y, z)
		public static readonly Symbol Cast = GSymbol.Get("'cast");         //!< "'cast": @'cast(x,int) <=> (int)x <=> x(-> int)
		public static readonly Symbol NullCoalesce = GSymbol.Get("'??");   //!< "'??":    a ?? b <=> @`'??`(a, b)
		[Obsolete("This was renamed to RightArrow")]
		public static readonly Symbol PtrArrow = GSymbol.Get("'->");
		public static readonly Symbol ColonColon = GSymbol.Get("'::");     //!< "'::" Scope resolution operator in many languages
		public static readonly Symbol Lambda = GSymbol.Get("'=>");         //!< "'=>" used to define an anonymous function
		public static readonly Symbol Default = GSymbol.Get("'default");   //!< "'default" for the default(T) pseudofunction in C#
		public static readonly Symbol IS = GSymbol.Get("'IS");             //!< Backquoted suffixes in LES3 use this: x`bytes` <=> `'IS`(x, bytes)

		// Compound assignment
		public static readonly Symbol NullCoalesceAssign = GSymbol.Get("'??="); //!< "'??=": `a ??= b` means `a = a ?? b`
		public static readonly Symbol MulAssign = GSymbol.Get("'*=");           //!< "'*=" multiply-and-set operator
		public static readonly Symbol DivAssign = GSymbol.Get("'/=");           //!< "'/=" divide-and-set operator
		public static readonly Symbol ModAssign = GSymbol.Get("'%=");           //!< "'%=" set-to-remainder operator
		public static readonly Symbol SubAssign = GSymbol.Get("'-=");           //!< "'-=" subtract-and-set operator
		public static readonly Symbol AddAssign = GSymbol.Get("'+=");           //!< "'+=" add-and-set operator
		public static readonly Symbol ConcatAssign = GSymbol.Get("'~=");        //!< "'~=" concatenate-and-set operator
		public static readonly Symbol ShrAssign = GSymbol.Get("'>>=");          //!< "'>>=" shift-right-by operator
		public static readonly Symbol ShlAssign = GSymbol.Get("'<<=");          //!< "'<<=" shift-left-by operator
		public static readonly Symbol ExpAssign = GSymbol.Get("'**=");          //!< "'**=" raise-to-exponent-and-set operator
		public static readonly Symbol XorBitsAssign = GSymbol.Get("'^=");       //!< "'^=" bitwise-xor-by operator
		public static readonly Symbol AndBitsAssign = GSymbol.Get("'&=");       //!< "'&=" bitwise-and-with operator
		public static readonly Symbol OrBitsAssign = GSymbol.Get("'|=");        //!< "'|=" set-bits operator

		// Executable statements
		public static readonly Symbol If = GSymbol.Get("#if");               //!< e.g. #if(c,x,y) and #if(c,x); I wanted it to be the conditional operator too, but the semantics are a bit different
		public static readonly Symbol DoWhile = GSymbol.Get("#doWhile");     //!< e.g. #doWhile(x++, condition); <=> do x++; while(condition);
		public static readonly Symbol While = GSymbol.Get("#while");         //!< e.g. #while(condition,{...}); <=> while(condition) {...}
		public static readonly Symbol UsingStmt = GSymbol.Get("#using");     //!< e.g. #using(expr, {...}); <=> using(expr) {...} (note: use #import or CodeSymbols.Import for a using directive)
		public static readonly Symbol For = GSymbol.Get("#for");             //!< e.g. #for(int i = 0, i < Count, i++, {...}); <=> for(int i = 0; i < Count; i++) {...}
		public static readonly Symbol ForEach = GSymbol.Get("#foreach");     //!< e.g. #foreach(#var(@``, n), list, {...}); <=> foreach(var n in list) {...}
		public static readonly Symbol Label = GSymbol.Get("#label");         //!< e.g. #label(success) <=> success:
		public static readonly Symbol Case = GSymbol.Get("#case");           //!< e.g. #case(10, 20) <=> case 10, 20:
		public static readonly Symbol Return = GSymbol.Get("#return");       //!< e.g. #return(x);  <=> return x;   [#yield] #return(x) <=> yield return x;
		public static readonly Symbol Continue = GSymbol.Get("#continue");   //!< e.g. #continue(); <=> continue;
		public static readonly Symbol Break = GSymbol.Get("#break");         //!< e.g. #break();    <=> break;
		public static readonly Symbol Goto = GSymbol.Get("#goto");           //!< e.g. #goto(label) <=> goto label;
		public static readonly Symbol GotoCase = GSymbol.Get("#gotoCase");   //!< e.g. #gotoCase(expr) <=> goto case expr;
		public static readonly Symbol Throw = GSymbol.Get("#throw");         //!< e.g. #throw(expr);  <=> throw expr;
		public static readonly Symbol Checked = GSymbol.Get("#checked");     //!< e.g. #checked({ stmt; }); <=> checked { stmt; }
		public static readonly Symbol Unchecked = GSymbol.Get("#unchecked"); //!< e.g. #unchecked({ stmt; }); <=> unchecked { stmt; }
		public static readonly Symbol Fixed = GSymbol.Get("#fixed");         //!< e.g. #fixed(#var(@`*`(#int32), x = &y), stmt); <=> fixed(int* x = &y) stmt;
		public static readonly Symbol Lock = GSymbol.Get("#lock");           //!< e.g. #lock(obj, stmt); <=> lock(obj) stmt;
		[Obsolete]
		public static readonly Symbol Switch = GSymbol.Get("#switch");       
		public static readonly Symbol SwitchStmt = GSymbol.Get("#switch");   //!< e.g. #switch(n, { ... }); <=> switch(n) { ... }
		public static readonly Symbol SwitchExpr = GSymbol.Get("'switch");   //!< e.g. @'switch(x, { ... }); <=> x switch { ... }
		public static readonly Symbol Try = GSymbol.Get("#try");             //!< e.g. #try({...}, #catch(@``, @``, {...})); <=> try {...} catch {...}
		public static readonly Symbol Catch = GSymbol.Get("#catch");         //!< "#catch"   catch clause of #try statement: #catch(#var(Exception,e), whenExpr, {...})
		public static readonly Symbol Finally = GSymbol.Get("#finally");     //!< "#finally" finally clause of #try statement: #finally({...})
		
		// Space definitions
		public static readonly Symbol Class = GSymbol.Get("#class");    //!< e.g. #class(Foo, #(IFoo), { });  <=> class Foo : IFoo { }
		public static readonly Symbol Struct = GSymbol.Get("#struct");  //!< e.g. #struct(Foo, #(IFoo), { }); <=> struct Foo : IFoo { }
		public static readonly Symbol Trait = GSymbol.Get("#trait");    //!< e.g. #trait(Foo, #(IFoo), { });  <=> trait Foo : IFoo { }
		public static readonly Symbol Enum = GSymbol.Get("#enum");      //!< e.g. #enum(Foo, #(byte), { });  <=> enum Foo : byte { }
		public static readonly Symbol Alias = GSymbol.Get("#alias");    //!< e.g. #alias(Int = int, #(IMath), { });  <=> alias Int = int : IMath { }
		                                                                //!< also, [#filePrivate] #alias(I = System.Int32) <=> using I = System.Int32;
		public static readonly Symbol Interface = GSymbol.Get("#interface"); //!< e.g. #interface(IB, #(IA), { });  <=> interface IB : IA { }
		public static readonly Symbol Namespace = GSymbol.Get("#namespace"); //!< e.g. #namespace(NS, @``, { });  <=> namespace NS { }

		// Other definitions
		public static readonly Symbol Var = GSymbol.Get("#var");           //!< e.g. #var(#int32, x = 0, y = 1, z); #var(@``, x = 0) <=> var x = 0;
		public static readonly Symbol Event = GSymbol.Get("#event");       //!< e.g. #event(EventHandler, Click, { }) <=> event EventHandler Click { }
		public static readonly Symbol Delegate = GSymbol.Get("#delegate"); //!< e.g. #delegate(#int32, Foo, @'tuple()); <=> delegate int Foo();
		public static readonly Symbol Property = GSymbol.Get("#property"); //!< e.g. #property(#int32, Foo, @``, { get; }) <=> int Foo { get; }

		// Misc
		public static readonly Symbol Where = GSymbol.Get("#where");       //!< "#where" e.g. class Foo<T> where T:class, Foo {} <=> #class(@'of(Foo, [#where(#class, Foo)] T), #(), {});
		public static readonly Symbol This = GSymbol.Get("#this");         //!< "#this" e.g. this.X <=> #this.X; this(arg) <=> #this(arg).
		public static readonly Symbol Base = GSymbol.Get("#base");         //!< "#base" e.g. base.X <=> #base.X; base(arg) <=> #base(arg).
		public static readonly Symbol Operator = GSymbol.Get("#operator"); //!< e.g. #fn(#bool, [#operator] @`==`, #(Foo a, Foo b))
		public static readonly Symbol Implicit = GSymbol.Get("#implicit"); //!< e.g. [#implicit] #fn(#int32, [#operator] #cast, (Foo a,))
		public static readonly Symbol Explicit = GSymbol.Get("#explicit"); //!< e.g. [#explicit] #fn(#int32, [#operator] #cast, (Foo a,))
		public static readonly Symbol Missing = GSymbol.Empty;             //!< Indicates that a syntax element was omitted, e.g. Foo(, y) => Foo(@``, y)
		public static readonly Symbol Splice = GSymbol.Get("#splice");     //!< When a macro returns #splice(a, b, c), the argument list (a, b, c) is spliced into the surrounding code.
		public static readonly Symbol Assembly = GSymbol.Get("#assembly"); //!< e.g. [assembly: Foo] <=> #assembly(Foo);
		public static readonly Symbol Module = GSymbol.Get("#module");     //!< e.g. [module: Foo] <=> [Foo] #module;
		public static readonly Symbol Import = GSymbol.Get("#import");     //!< e.g. using System; <=> #import(System);
		//!< #import is used instead of #using because the using(...) {...} statement already uses #using
		public static readonly Symbol Partial = GSymbol.Get("#partial");
		public static readonly Symbol Yield = GSymbol.Get("#yield");       //!< e.g. #return(x);  <=> return x;   [#yield] #return(x) <=> yield return x;
		public static readonly Symbol ArrayInit = GSymbol.Get("#arrayInit"); //!< C# e.g. int[] x = {1,2} <=> int[] x = #arrayInit(1, 2)

		public static readonly Symbol StackAlloc = GSymbol.Get("#stackalloc"); //!< #stackalloc for C# stackalloc (TODO)
		public static readonly Symbol Backslash = GSymbol.Get(@"'\");      //!< "'\" operator

		[Obsolete("Use PreBangBang or SufBangBang")]
		public static readonly Symbol DoubleBang = GSymbol.Get(@"'!!");    //!< "'!!" operator
		public static readonly Symbol PreBangBang = GSymbol.Get(@"'!!");   //!< "'!!" operator
		public static readonly Symbol SufBangBang = GSymbol.Get(@"'suf!!"); //!< "'suf!!" operator
		public static readonly Symbol BangBangDot = GSymbol.Get(@"'!!.");  //!< "'!!." operator
		public static readonly Symbol RightArrow = GSymbol.Get(@"'->");    //!< "'->" operator: a->b
		[Obsolete("This was renamed to RightArrow")]
		public static readonly Symbol _RightArrow = GSymbol.Get(@"'->");   //!< "'->" operator: a->b
		public static readonly Symbol LeftArrow = GSymbol.Get(@"'<-");     //!< "'<-" operator
		public static readonly Symbol Parens = GSymbol.Get("'()");         //!< Produced by ' in LESv3, which switches parser to prefix expression mode (similar to s-expressions)

		public static readonly Symbol Readonly = GSymbol.Get("#readonly"); //!< "#readonly" e.g. readonly int X; <=> [#readonly] #var(#int, X);
		public static readonly Symbol Const = GSymbol.Get("#const");       //!< "#const"    e.g. const int X = 1; <=> [#const] #var(#int, X = 1);

		public static readonly Symbol Static = GSymbol.Get("#static");     //!< "#static" attribute
		public static readonly Symbol Virtual = GSymbol.Get("#virtual");   //!< "#virtual" attribute
		public static readonly Symbol Override = GSymbol.Get("#override"); //!< "#override" attribute
		public static readonly Symbol Abstract = GSymbol.Get("#abstract"); //!< "#abstract" attribute
		public static readonly Symbol Sealed = GSymbol.Get("#sealed");     //!< "#sealed" attribute
		public static readonly Symbol Extern = GSymbol.Get("#extern");     //!< "#extern" attribute
		public static readonly Symbol Unsafe = GSymbol.Get("#unsafe");     //!< "#unsafe" attribute
		public static readonly Symbol Params = GSymbol.Get("#params");     //!< "#params" attribute
		public static readonly Symbol Volatile = GSymbol.Get("#volatile"); //!< "#volatile" attribute
		
		/// <summary>An identifier or call with this Name indicates that parsing or 
		/// analysis failed earlier and that an error message has already been 
		/// printed.</summary>
		/// <remarks>When code in a compiler sees this symbol it should be seen as
		/// a signal to avoid printing further error messages that involve the same
		/// node. Typically, a node named #badCode should replace the bad code, and 
		/// it may have an argument that describes the error, which could be printed 
		/// at runtime if compilation continues to completion.</remarks>
		/// <example>#badCode("Argument 2: Cannot convert 'string' to 'int'.")</example>
		public static readonly Symbol BadCode = GSymbol.Get("#badCode");

		// Enhanced C# stuff (node names)
		public static readonly Symbol NullDot = GSymbol.Get("'?.");       //!< "?."  safe navigation ("null dot") operator
		public static readonly Symbol Exp = GSymbol.Get("'**");           //!< "**"  exponent operator
		public static readonly Symbol In = GSymbol.Get("'in");            //!< "'in" membership test operator
		public static readonly Symbol Substitute = GSymbol.Get(@"'$");    //!< "$"   substitution operator
		public static readonly Symbol _TemplateArg = GSymbol.Get(@"'$");  //!< Alias for Substitude
		public static readonly Symbol DotDot = GSymbol.Get("'..");        //!< ".." Binary range operator (exclusive)
		public static readonly Symbol DotDotDot = GSymbol.Get("'...");    //!< "..." Binary range operator (inclusive)
		public static readonly Symbol DotDotLT = GSymbol.Get("'..<");     //!< "..<" Swift uses this instead of ".."
		public static readonly Symbol Tuple = GSymbol.Get("'tuple");      //!< "'tuple": (1, "a") <=> @'tuple(1, "a")
		public static readonly Symbol QuickBind = GSymbol.Get("'=:");     //!< "=:" Quick variable-creation operator (variable name on right). In consideration: may be changed to ":::"
		public static readonly Symbol QuickBindAssign = GSymbol.Get("':="); //!< ":=" Quick variable-creation operator (variable name on left)
		public static readonly Symbol Fn = GSymbol.Get("#fn");            //!< e.g. #fn(#void, Foo, #(#var(List<int>, list)), {return;}) <=> void Foo(List<int> list) {return;}
		public static readonly Symbol Constructor = GSymbol.Get("#cons"); //!< e.g. #cons(@``, Foo, #(), {this.x = 0;}) <=> Foo() {this.x = 0;)
		public static readonly Symbol Forward = GSymbol.Get("'==>");      //!< "==>" forwarding operator e.g. int X ==> _x; <=> #property(#int32, X, @`==>`(_x));
		public static readonly Symbol UsingCast = GSymbol.Get("'using");  //!< @`'using`(x,int) <=> x using int <=> x(using int)
		                                                                     //!< #using is reserved for the using statement: using(expr) {...}
		public static readonly Symbol IsLegal = GSymbol.Get("'isLegal");     //!< TODO
		public static readonly Symbol Result = GSymbol.Get("#result");       //!< #result(expr) indicates that expr was missing a semicolon, which
		                                                                     //!< indicates that "expr" will be the value of the containing block.

		// Names of property getters & setters, and event adders & removers
		public static readonly Symbol get = GSymbol.Get("get");
		public static readonly Symbol set = GSymbol.Get("set");
		public static readonly Symbol add = GSymbol.Get("add");
		public static readonly Symbol remove = GSymbol.Get("remove");
		public static readonly Symbol value = GSymbol.Get("value");

		// EC# directives (not to be confused with preprocessor directives)
		public static readonly Symbol Error = GSymbol.Get("#error");         // e.g. #error("This feature is not supported in Windows CE")
		public static readonly Symbol Warning = GSymbol.Get("#warning");     // e.g. #warning("Possibly mistaken empty statement")
		public static readonly Symbol Note = GSymbol.Get("#note");           // e.g. #note("I love bunnies")

		// C# LINQ clauses
		public static readonly Symbol Linq = GSymbol.Get("#linq");           // e.g. #linq(#from(x in list), #where(x > 0), #select(x))
		public static readonly Symbol From = GSymbol.Get("#from");           // e.g. #from(x in list) // LHS of `in` can be id or var decl
		public static readonly Symbol Let = GSymbol.Get("#let");             // e.g. #let(x = y.Foo) // can have any expression inside
		public static readonly Symbol Join = GSymbol.Get("#join");           // e.g. #join(p in products, #equals(c.ID, p.CID), #into(pGroup))
		public static readonly Symbol OrderBy = GSymbol.Get("#orderby");     // e.g. #orderby(#ascending(p.Name), #descending(p.Date))
		public static readonly Symbol Ascending = GSymbol.Get("#ascending");
		public static readonly Symbol Descending = GSymbol.Get("#descending");
		public static readonly Symbol Select = GSymbol.Get("#select");       // e.g. #select(p.Name)
		public static readonly Symbol GroupBy = GSymbol.Get("#groupBy");     // e.g. #groupBy(p.Name, p.Year) - similar to #select, but creates groups
		public static readonly Symbol Into = GSymbol.Get("#into");           // e.g. #linq(..., #into(id, ...)) - use output of outer query as input to inner query
		//Where is defined elsewhere in this class                           // e.g. #where(x > 0)

		// Preprocessor directives
		public static readonly Symbol PPIf = GSymbol.Get("##if");           //!< "##if"      represents the #if preprocessor token (does not reach the parser)
		public static readonly Symbol PPElse = GSymbol.Get("##else");       //!< "##else"    represents the #else preprocessor token (does not reach the parser) 
		public static readonly Symbol PPElIf = GSymbol.Get("##elif");       //!< "##elif"    represents the #elif preprocessor token (does not reach the parser) 
		public static readonly Symbol PPEndIf = GSymbol.Get("##endif");     //!< "##endif"   represents the #endif preprocessor token (does not reach the parser)
		public static readonly Symbol PPDefine = GSymbol.Get("##define");   //!< "##define"  represents the #define preprocessor token (does not reach the parser)
		public static readonly Symbol PPUndef = GSymbol.Get("##undef");     //!< "##undef"   represents the #undef preprocessor token (does not reach the parser)
		public static readonly Symbol PPRegion = GSymbol.Get("##region");   //!< "##region"  represents the #region preprocessor token (does not reach the parser)
		public static readonly Symbol PPEndRegion = GSymbol.Get("##endregion"); //!< "##endregion" represents the #endregion preprocessor token (does not reach the parser)
		public static readonly Symbol PPPragma = GSymbol.Get("##pragma");   //!< "##pragma"  represents the #pragma preprocessor token (does not reach the parser)
		public static readonly Symbol PPError = GSymbol.Get("##error");     //!< "##error"   represents the #error preprocessor token (does not reach the parser)
		public static readonly Symbol PPWarning = GSymbol.Get("##warning"); //!< "##warning" represents the #warning preprocessor token (does not reach the parser)
		public static readonly Symbol PPNote = GSymbol.Get("##note");       //!< "##note"    represents the #note preprocessor token (does not reach the parser)
		public static readonly Symbol PPLine = GSymbol.Get("##line");       //!< "##line"    represents the #line preprocessor token (does not reach the parser)

		// Accessibility flags
		/// <summary>Provides general access within a library or program (implies
		/// #protected_in).</summary>
		public static readonly Symbol Internal = GSymbol.Get("#internal");
		/// <summary>Provides general access, even outside the assembly (i.e. 
		/// dynamic-link library). Implies #internal, #protectedIn and #protected.</summary>
		public static readonly Symbol Public = GSymbol.Get("#public");
		/// <summary>Provides access to derived classes only within the same library
		/// or program (i.e. assembly). There is no C# equivalent to this keyword,
		/// which does not provide access outside the assembly.</summary>
		public static readonly Symbol ProtectedIn = GSymbol.Get("#protectedIn");
		/// <summary>Provides access to all derived classes. Implies #protected_in.
		/// #protected #internal corresponds to C# "protected internal"</summary>
		public static readonly Symbol Protected = GSymbol.Get("#protected"); 
		/// <summary>Revokes access outside the same space and nested spaces. This 
		/// can be used in spaces in which the default is not private to request
		/// private as a starting point. Therefore, other flags (e.g. #protected_ex)
		/// can be added to this flag to indicate what access the user wants to
		/// provide instead.
		/// </summary><remarks>
		/// The name #private may be slightly confusing, since a symbol marked 
		/// #private is not actually private when there are other access markers
		/// included at the same time. I considered calling it #revoke instead,
		/// since its purpose is to revoke the default access modifiers of the
		/// space, but I was concerned that someone might want to reserve #revoke 
		/// for some other purpose.
		/// </remarks>
		public static readonly Symbol Private = GSymbol.Get("#private");
		/// <summary>Used with #alias to indicate that an alias is local to the
		/// current source file. <c>[#filePrivate] #alias(X = Y, #())</c> is the long
		/// form of <c>using X = Y</c> in EC#.</summary>
		public static readonly Symbol FilePrivate = GSymbol.Get("#filePrivate");

		// C#/.NET standard data types
		public static readonly Symbol Void   = GSymbol.Get("#void");     //!< "#void" data type
		public static readonly Symbol String = GSymbol.Get("#string");   //!< "#string" standard data type
		public static readonly Symbol Char   = GSymbol.Get("#char");     //!< "#char" character data type
		public static readonly Symbol Bool   = GSymbol.Get("#bool");     //!< "#bool" boolean data type
		public static readonly Symbol Int8   = GSymbol.Get("#int8");     //!< "#int8" 8-bit signed integer data type
		public static readonly Symbol Int16  = GSymbol.Get("#int16");    //!< "#int16" 16-bit signed integer data type
		public static readonly Symbol Int32  = GSymbol.Get("#int32");    //!< "#int32" 32-bit signed integer data type
		public static readonly Symbol Int64  = GSymbol.Get("#int64");    //!< "#int64" 64-bit signed integer data type
		public static readonly Symbol UInt8  = GSymbol.Get("#uint8");    //!< "#uint8"  8-bit unsigned integer data type
		public static readonly Symbol UInt16 = GSymbol.Get("#uint16");   //!< "#uint16" 16-bit unsigned integer data type
		public static readonly Symbol UInt32 = GSymbol.Get("#uint32");   //!< "#uint32" 32-bit unsigned integer data type
		public static readonly Symbol UInt64 = GSymbol.Get("#uint64");   //!< "#uint64" 64-bit unsigned integer data type
		public static readonly Symbol Single = GSymbol.Get("#single");   //!< "#single" 32-bit float data type
		public static readonly Symbol Double = GSymbol.Get("#double");   //!< "#double" 64-bit float data type
		public static readonly Symbol Decimal = GSymbol.Get("#decimal"); //!< "#decimal" .NET decimal data type
		public static readonly Symbol Object = GSymbol.Get("#object");   //!< "#object" .NET object data type
		public static readonly Symbol Dynamic = GSymbol.Get("#dynamic"); //!< "#dynamic" dynamically-typed variable type

		// Tokens
		public static readonly Symbol Colon = GSymbol.Get("':");         //!< ":" token value for colon
		public static readonly Symbol Semicolon = GSymbol.Get("';");     //!< ";" token value for semicolon
		public static readonly Symbol AtSign = GSymbol.Get("'@");        //!< "@" token value for at sign
		public static readonly Symbol Do = GSymbol.Get("#do");           //!< "#do" token value for do keyword, not to be confused with #doWhile
		public static readonly Symbol Else = GSymbol.Get("#else");       //!< "#else" token value for else keyword
		public static readonly Symbol Comma = GSymbol.Get("',");         //!< "," token value for comma
			// (C comma operator: all arguments are evaluated and the result of the expression is the value of the last argument. This is equivalent to the way we tentatively define #)

		// Trivia
		//public static readonly Symbol TriviaCommaSeparatedStmts = GSymbol.Get("%commaSeparated");
		public static readonly Symbol TriviaInParens = GSymbol.Get("%inParens");                     //!< "%inParens" an attribute attached to an expression that has parenthesis around it.
		public static readonly Symbol TriviaMacroAttribute = GSymbol.Get("%macroAttribute");         //!< "%macroAttribute" an attribute attached to a EC# statement that uses a macro-style call, e.g. foo {...} <=> [%macroAttribute] foo({...});
		public static readonly Symbol TriviaDoubleVerbatim = GSymbol.Get("%doubleVerbatim");         //!< obsolete
		public static readonly Symbol TriviaUseOperatorKeyword = GSymbol.Get("%useOperatorKeyword"); //!< "%useOperatorKeyword" e.g. Foo.operator+(a, b) <=> Foo.([`%useOperatorKeyword`]`'+`)(a, b)
		public static readonly Symbol TriviaForwardedProperty = GSymbol.Get("%forwardedProperty");   //!< "%forwardedProperty" e.g. get ==> _x; <=> [`%forwardedProperty`] get(`'==>`(_x));
		public static readonly Symbol TriviaRawText = GSymbol.Get("%rawText");                 //!< "%rawText" - Arbitrary text to be emitted unchanged, e.g. `[`%rawText`("cue!")] q;` is printed as `cue!q;`.
		[Obsolete("Use EcsCodeSymbols.TriviaCsRawText instead (Loyc.Ecs package)")]
		public static readonly Symbol TriviaCsRawText = GSymbol.Get("%C#RawText");             //!< "%C#RawText" - `%C#RawText`("stuff") - Raw text that is only printed by the C# printer (not printers for other languages)
		[Obsolete("Use EcsCodeSymbols.TriviaCsPPRawText instead (Loyc.Ecs package)")]
		public static readonly Symbol TriviaCsPPRawText = GSymbol.Get("%C#PPRawText");         //!< "%C#PPRawText" - `%C#PPRawText`("#stuff") - Raw text that is guaranteed to be preceded by a newline and is only printed by the C# printer
		
		/// "%wordAttribute": in EC#, this trivia is placed on an identifier treated as an attribute (e.g. partial, async).
		public static readonly Symbol TriviaWordAttribute = GSymbol.Get("%wordAttribute");
		public static readonly Symbol TriviaDummyNode = GSymbol.Get("%dummyNode"); //!< Attribute attached to a dummy node that was created so that trivia could be attached to it
		public static readonly Symbol TriviaSLComment = GSymbol.Get("%SLComment"); //!< "%SLComment", e.g. @`%SLComment`(" Text")
		public static readonly Symbol TriviaMLComment = GSymbol.Get("%MLComment"); //!< "%MLComment", e.g. @`%MLComment`(" Text")
		public static readonly Symbol TriviaNewline = GSymbol.Get("%newline");
		public static readonly Symbol TriviaAppendStatement = GSymbol.Get("%appendStatement"); //!< Suppresses the newline that ordinarily appears before each statement in a braced block
		public static readonly Symbol TriviaSpaces = GSymbol.Get("%spaces");
		public static readonly Symbol TriviaTrailing = GSymbol.Get("%trailing");
		public static readonly Symbol TriviaRegion = GSymbol.Get("%region");       //!< "%region" - Region begin marker: #region Title <=> `%region`(" Title");
		public static readonly Symbol TriviaEndRegion = GSymbol.Get("%endRegion"); //!< "%endRegion" - Region end marker: #endregion End <=> `%endregion`(" End");

		/// #rawText must be a call with a single literal argument. The Value of
		/// the argument is converted to a string and printed out by EcsNodePrinter 
		/// without any filtering, e.g. `#rawText("Hello")` is printed `Hello`.
		public static readonly Symbol RawText = GSymbol.Get("#rawText");
		[Obsolete("Use EcsCodeSymbols.CsRawText instead (Loyc.Ecs package)")]
		public static readonly Symbol CsRawText = GSymbol.Get("#C#RawText");
		[Obsolete("Use EcsCodeSymbols.CsPPRawText instead (Loyc.Ecs package)")]
		public static readonly Symbol CsPPRawText = GSymbol.Get("#C#PPRawText"); //!< "#C#PPRawText" - Preprocessor raw text: always printed on separate line

		// NodeStyle.Alternate is used for: @"verbatim strings", 0xhex numbers, 
		// new-style casts x(->int), delegate(old-style lambdas) {...}

		/// <summary>Returns true if the symbol is a pair of square brackets with 
		/// zero or more commas inside, e.g. "[,]", which in EC# represents an array 
		/// type of a specific number of dimensions.</summary>
		public static bool IsArrayKeyword(Symbol s) { return CountArrayDimensions(s) > 0; }
		/// <summary>Returns the rank of an array symbol when <see cref="IsArrayKeyword"/> 
		/// is true, or 0 if the symbol does not represent an array type.</summary>
		public static int CountArrayDimensions(Symbol s)
		{
			if (s.Name.Length >= 3 && s.Name.StartsWith("'[") && s.Name[s.Name.Length-1] == ']') {
				for (int i = 2; i < s.Name.Length-1; i++)
					if (s.Name[i] != ',')
						return 0;
				return s.Name.Length-2;
			}
			return 0;
		}
		/// <summary>Gets the Symbol for an array with the specified number of 
		/// dimensions, e.g. <c>GetArrayKeyword(3)</c> returns <c>[,,]</c>.</summary>
		public static Symbol GetArrayKeyword(int dims)
		{
			if (dims <= 0) throw new ArgumentException("GetArrayKeyword(dims <= 0)");
			if (dims == 1) return Array;
			if (dims == 2) return TwoDimensionalArray;
			return GSymbol.Get("'[" + new string(',', dims-1) + "]");
		}
		public static bool IsTriviaSymbol(Symbol name) {
			return name != null && (name.Name.StartsWith("%") || name.Name.StartsWith("#trivia_"));
		}
	}
}
