using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Utilities;

namespace Loyc.Syntax
{
	/// <summary>
	/// A list of common symbols that have special meaning somewhere in Loyc or EC#:
	/// operators, built-in data types, keywords, trivia, etc.
	/// </summary>
	public partial class CodeSymbols
	{
		// Plain C# operators (node names)
		public static readonly Symbol Mul = GSymbol.Get("*"); // or dereference
		public static readonly Symbol Div = GSymbol.Get("/");
		public static readonly Symbol Add = GSymbol.Get("+");     // or unary +
		public static readonly Symbol Sub = GSymbol.Get("-");     // or unary -
		public static readonly Symbol _Dereference = GSymbol.Get("*");
		public static readonly Symbol _Pointer = GSymbol.Get("*");
		public static readonly Symbol _UnaryPlus = GSymbol.Get("+");
		public static readonly Symbol _Negate = GSymbol.Get("-"); // infix and prefix operators use same symbol
		public static readonly Symbol PreInc = GSymbol.Get("++");
		public static readonly Symbol PreDec = GSymbol.Get("--");
		public static readonly Symbol PostInc = GSymbol.Get("suf++");
		public static readonly Symbol PostDec = GSymbol.Get("suf--");
		public static readonly Symbol Mod = GSymbol.Get("%");
		public static readonly Symbol And = GSymbol.Get("&&");
		public static readonly Symbol Or = GSymbol.Get("||");
		public static readonly Symbol Xor = GSymbol.Get("^^");
		public static readonly Symbol Eq = GSymbol.Get("==");
		public static readonly Symbol Neq = GSymbol.Get("!=");
		public static readonly Symbol GT = GSymbol.Get(">");
		public static readonly Symbol GE = GSymbol.Get(">=");
		public static readonly Symbol LT = GSymbol.Get("<");
		public static readonly Symbol LE = GSymbol.Get("<=");
		public static readonly Symbol Shr = GSymbol.Get(">>");
		public static readonly Symbol Shl = GSymbol.Get("<<");
		public static readonly Symbol Not = GSymbol.Get("!");
		public static readonly Symbol Set = GSymbol.Get("=");
		public static readonly Symbol OrBits = GSymbol.Get("|");
		public static readonly Symbol AndBits = GSymbol.Get("&"); // also, address-of (unary &)
		public static readonly Symbol _AddressOf = GSymbol.Get("&");
		public static readonly Symbol NotBits = GSymbol.Get("~"); 
		public static readonly Symbol _Concat = GSymbol.Get("~"); // infix, tentative
		public static readonly Symbol _Destruct = GSymbol.Get("~"); 
		public static readonly Symbol XorBits = GSymbol.Get("^");
		
		public static readonly Symbol Braces = GSymbol.Get("{}"); // Creates a scope.
		public static readonly Symbol Bracks = GSymbol.Get("[]"); // indexing operator and array type (use _Attr for attributes)
		                                                          // foo[1] <=> @`[]`(foo, 1) and int[] <=> #of(@`[]`, int)
		public static readonly Symbol _Array = GSymbol.Get("[]");
		public static readonly Symbol TwoDimensionalArray = GSymbol.Get("[,]"); // int[,] <=> #of(@`[,]`, int)

		// # is used for lists of things in definition constructs, e.g. 
		//     #class(Derived, #(Base, IEnumerable), {...})
		// For a time, #tuple was used for this purpose; the problem is that a
		// find-and-replace operation intended to find run-time tuples could 
		// accidentally match one of these lists. So I decided to dedicate # 
		// for use inside special constructs; its meaning depends on context.
		public static readonly Symbol List = GSymbol.Get("#");

		public static readonly Symbol QuestionMark = GSymbol.Get("?"); // (a?b:c) <=> @`?`(a,b,c) and int? <=> #of(@`?`, int)
		public static readonly Symbol Of = GSymbol.Get("#of");
		public static readonly Symbol Dot = GSymbol.Get(".");
		public static readonly Symbol NamedArg = GSymbol.Get("#namedArg"); // Named argument e.g. #namedarg(x, 0) <=> x: 0
		public static readonly Symbol New = GSymbol.Get("#new"); // new Foo(x) { a } <=> #new(Foo(x), a)
		public static readonly Symbol Out = GSymbol.Get("#out");
		public static readonly Symbol Ref = GSymbol.Get("#ref");
		public static readonly Symbol Sizeof = GSymbol.Get("#sizeof");       // sizeof(int) <=> #sizeof(int)
		public static readonly Symbol Typeof = GSymbol.Get("#typeof");       // typeof(Foo) <=> #typeof(Foo)
		                                                                     // typeof<foo> <=> #of(#typeof, foo)
		public static readonly Symbol As = GSymbol.Get("#as");               // #as(x,string) <=> x as string <=> x(as string)
		public static readonly Symbol Is = GSymbol.Get("#is");               // #is(x,string) <=> x is string
		public static readonly Symbol Cast = GSymbol.Get("#cast");           // #cast(x,int) <=> (int)x <=> x(-> int)
		public static readonly Symbol NullCoalesce = GSymbol.Get("??");
		public static readonly Symbol PtrArrow = GSymbol.Get("->");
		public static readonly Symbol ColonColon = GSymbol.Get("::"); // Scope resolution operator in many languages
		public static readonly Symbol Lambda = GSymbol.Get("=>");
		public static readonly Symbol Default = GSymbol.Get("#default");

		// Compound assignment
		public static readonly Symbol NullCoalesceSet = GSymbol.Get("??=");
		public static readonly Symbol MulSet = GSymbol.Get("*=");
		public static readonly Symbol DivSet = GSymbol.Get("/=");
		public static readonly Symbol ModSet = GSymbol.Get("%=");
		public static readonly Symbol SubSet = GSymbol.Get("-=");
		public static readonly Symbol AddSet = GSymbol.Get("+=");
		public static readonly Symbol ConcatSet = GSymbol.Get("~=");
		public static readonly Symbol ShrSet = GSymbol.Get(">>=");
		public static readonly Symbol ShlSet = GSymbol.Get("<<=");
		public static readonly Symbol ExpSet = GSymbol.Get("**=");
		public static readonly Symbol XorBitsSet = GSymbol.Get("^=");
		public static readonly Symbol AndBitsSet = GSymbol.Get("&=");
		public static readonly Symbol OrBitsSet = GSymbol.Get("|=");

		// Executable statements
		public static readonly Symbol If = GSymbol.Get("#if");               // e.g. #if(x,y,z); I wanted it to be the conditional operator too, 
		public static readonly Symbol Else = GSymbol.Get("#else");           //      but the semantics are a bit different
		public static readonly Symbol DoWhile = GSymbol.Get("#doWhile");     // e.g. #doWhile(x++, condition); <=> do x++; while(condition);
		public static readonly Symbol While = GSymbol.Get("#while");         // e.g. #while(condition,{...}); <=> while(condition) {...}
		public static readonly Symbol UsingStmt = GSymbol.Get("#using");     // e.g. #using(expr, {...}); <=> using(expr) {...}
		public static readonly Symbol For = GSymbol.Get("#for");             // e.g. #for(int i = 0, i < Count, i++, {...}); <=> for(int i = 0; i < Count; i++) {...}
		public static readonly Symbol ForEach = GSymbol.Get("#foreach");     // e.g. #foreach(#var(@``, n), list, {...}); <=> foreach(var n in list) {...}
		public static readonly Symbol Label = GSymbol.Get("#label");         // e.g. #label(success) <=> success:
		public static readonly Symbol Case = GSymbol.Get("#case");           // e.g. #case(10, 20) <=> case 10, 20:
		public static readonly Symbol Return = GSymbol.Get("#return");       // e.g. #return(x);  <=> return x;   [#yield] #return(x) <=> yield return x;
		public static readonly Symbol Continue = GSymbol.Get("#continue");   // e.g. #continue;   <=> continue;
		public static readonly Symbol Break = GSymbol.Get("#break");         // e.g. #break;      <=> break;
		public static readonly Symbol Goto = GSymbol.Get("#goto");           // e.g. #goto(label) <=> goto label;
		public static readonly Symbol GotoCase = GSymbol.Get("#gotoCase");   // e.g. #gotoCase(expr) <=> goto case expr;
		public static readonly Symbol Throw = GSymbol.Get("#throw");         // e.g. #throw(expr);  <=> throw expr;
		public static readonly Symbol Checked = GSymbol.Get("#checked");     // e.g. #checked({ stmt; }); <=> checked { stmt; }
		public static readonly Symbol Unchecked = GSymbol.Get("#unchecked"); // e.g. #unchecked({ stmt; }); <=> unchecked { stmt; }
		public static readonly Symbol Fixed = GSymbol.Get("#fixed");         // e.g. #fixed(#var(@`*`(#int32), x(&y)), stmt); <=> fixed(int* x = &y) stmt;
		public static readonly Symbol Lock = GSymbol.Get("#lock");           // e.g. #lock(obj, stmt); <=> lock(obj) stmt;
		public static readonly Symbol Switch = GSymbol.Get("#switch");       // e.g. #switch(n, { ... }); <=> switch(n) { ... }
		public static readonly Symbol Try = GSymbol.Get("#try");             // e.g. #try({...}, #catch(@``, {...})); <=> try {...} catch {...}
		public static readonly Symbol Catch = GSymbol.Get("#catch");       
		public static readonly Symbol Finally = GSymbol.Get("#finally");   
		
		// Space definitions
		public static readonly Symbol Class = GSymbol.Get("#class");    // e.g. #class(Foo, #(IFoo), { });  <=> class Foo : IFoo { }
		public static readonly Symbol Struct = GSymbol.Get("#struct");  // e.g. #struct(Foo, #(IFoo), { }); <=> struct Foo : IFoo { }
		public static readonly Symbol Trait = GSymbol.Get("#trait");    // e.g. #trait(Foo, #(IFoo), { });  <=> trait Foo : IFoo { }
		public static readonly Symbol Enum = GSymbol.Get("#enum");      // e.g. #enum(Foo, #(byte), { });  <=> enum Foo : byte { }
		public static readonly Symbol Alias = GSymbol.Get("#alias");    // e.g. #alias(Int = int, #(IMath), { });  <=> alias Int = int : IMath { }
		                                                                // also, [#filePrivate] #alias(I = System.Int32) <=> using I = System.Int32;
		public static readonly Symbol Interface = GSymbol.Get("#interface"); // e.g. #interface(IB, #(IA), { });  <=> interface IB : IA { }
		public static readonly Symbol Namespace = GSymbol.Get("#namespace"); // e.g. #namespace(NS, @``, { });  <=> namespace NS { }

		// Other definitions
		public static readonly Symbol Var = GSymbol.Get("#var");           // e.g. #var(#int32, x(0), y(1), z). #var(@``, x = 0) <=> var x = 0;
		public static readonly Symbol Event = GSymbol.Get("#event");       // e.g. #event(EventHandler, Click, { }) <=> event EventHandler Click { }
		public static readonly Symbol Delegate = GSymbol.Get("#delegate"); // e.g. #delegate(#int32, Foo, #tuple()); <=> delegate int Foo();
		public static readonly Symbol Property = GSymbol.Get("#property"); // e.g. #property(#int32, Foo, { get; }) <=> int Foo { get; }

		// Misc
		public static readonly Symbol Where = GSymbol.Get("#where");
		public static readonly Symbol This = GSymbol.Get("#this");
		public static readonly Symbol Base = GSymbol.Get("#base");
		public static readonly Symbol Operator = GSymbol.Get("#operator"); // e.g. #def(#bool, [#operator] @`==`, #(Foo a, Foo b))
		public static readonly Symbol Implicit = GSymbol.Get("#implicit"); // e.g. [#implicit] #def(#int32, [#operator] #cast, (Foo a,))
		public static readonly Symbol Explicit = GSymbol.Get("#explicit"); // e.g. [#explicit] #def(#int32, [#operator] #cast, (Foo a,))
		public static readonly Symbol Missing = GSymbol.Empty;             // A syntax element was omitted, e.g. Foo(, y) => Foo(@``, y)
		public static readonly Symbol Splice = GSymbol.Get("#splice");     // When a macro returns #splice(a, b, c), the argument list (a, b, c) is spliced into the surrounding code.
		public static readonly Symbol Assembly = GSymbol.Get("#assembly"); // e.g. [assembly: Foo] <=> [Foo] #assembly;
		public static readonly Symbol Module = GSymbol.Get("#module");     // e.g. [module: Foo] <=> [Foo] #module;
		public static readonly Symbol Import = GSymbol.Get("#import");     // e.g. using System; <=> #import(System);
		// #import is used instead of #using because the using(...) {...} statement already uses #using
		public static readonly Symbol Partial = GSymbol.Get("#partial");
		public static readonly Symbol ArrayInit = GSymbol.Get("#arrayInit"); // C# e.g. int[] x = {1,2} <=> int[] x = #arrayInit(1, 2)

		public static readonly Symbol StackAlloc = GSymbol.Get("#stackalloc");
		public static readonly Symbol Backslash = GSymbol.Get(@"\");
		public static readonly Symbol DoubleBang = GSymbol.Get(@"!!");
		public static readonly Symbol _RightArrow = GSymbol.Get(@"->");
		public static readonly Symbol LeftArrow = GSymbol.Get(@"<-");

		public static readonly Symbol Readonly = GSymbol.Get("#readonly");
		public static readonly Symbol Const = GSymbol.Get("#const");

		public static readonly Symbol Static = GSymbol.Get("#static");
		public static readonly Symbol Virtual = GSymbol.Get("#virtual");
		public static readonly Symbol Override = GSymbol.Get("#override");
		public static readonly Symbol Abstract = GSymbol.Get("#abstract");
		public static readonly Symbol Sealed = GSymbol.Get("#sealed");
		public static readonly Symbol Extern = GSymbol.Get("#extern");
		public static readonly Symbol Unsafe = GSymbol.Get("#unsafe");
		public static readonly Symbol Params = GSymbol.Get("#params");
		public static readonly Symbol Volatile = GSymbol.Get("#volatile");
		
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
		public static readonly Symbol NullDot = GSymbol.Get("?.");
		public static readonly Symbol Exp = GSymbol.Get("**");
		public static readonly Symbol In = GSymbol.Get("#in");
		public static readonly Symbol Substitute = GSymbol.Get(@"$");
		public static readonly Symbol _TemplateArg = GSymbol.Get(@"$");
		public static readonly Symbol DotDot = GSymbol.Get("..");
		public static readonly Symbol CodeQuote = GSymbol.Get("@");              // Code quote @(...), @{...} or @[...]
		public static readonly Symbol CodeQuoteSubstituting = GSymbol.Get("@@"); // Code quote @@(...), @@{...} or @@[...]
		public static readonly Symbol Tuple = GSymbol.Get("#tuple");
		public static readonly Symbol Literal = GSymbol.Get("#literal");
		public static readonly Symbol QuickBind = GSymbol.Get("=:");        // Quick variable-creation operator.
		public static readonly Symbol QuickBindSet = GSymbol.Get(":=");     // Quick variable-creation operator.
		public static readonly Symbol Def = GSymbol.Get("#def");             // e.g. #def(F, #([required] #var(#of(List, int), list)), #void, {return;})
		public static readonly Symbol Cons = GSymbol.Get("#cons");           // e.g. #cons(@``, Foo, #(), {return;}) <=> this() {return;)
		public static readonly Symbol Forward = GSymbol.Get("==>");
		public static readonly Symbol UsingCast = GSymbol.Get("#usingCast"); // #usingCast(x,int) <=> x using int <=> x(using int)
		                                                                     // #using is reserved for the using statement: using(expr) {...}
		public static readonly Symbol IsLegal = GSymbol.Get("#isLegal");
		public static readonly Symbol Result = GSymbol.Get("#result");       // #result(expr) indicates that expr was missing a semicolon, which
		                                                                     // indicates that "expr" will be the value of the containing block.
		
		// EC# directives (not to be confused with preprocessor directives)
		public static readonly Symbol Error = GSymbol.Get("#error");         // e.g. #error("This feature is not supported in Windows CE")
		public static readonly Symbol Warning = GSymbol.Get("#warning");     // e.g. #warning("Possibly mistaken empty statement")
		public static readonly Symbol Note = GSymbol.Get("#note");           // e.g. #note("I love bunnies")

		// Preprocessor directives
		public static readonly Symbol PPIf = GSymbol.Get("##if");     // e.g. #if(x,y,z); I wanted it to be the conditional operator too, but the semantics are a bit different
		public static readonly Symbol PPElse = GSymbol.Get("##else"); // e.g. #if(x,y,z); I wanted it to be the conditional operator too, but the semantics are a bit different
		public static readonly Symbol PPElIf = GSymbol.Get("##elif");
		public static readonly Symbol PPEndIf = GSymbol.Get("##endif");
		public static readonly Symbol PPDefine = GSymbol.Get("##define");
		public static readonly Symbol PPUndef = GSymbol.Get("##undef");
		public static readonly Symbol PPRegion = GSymbol.Get("##region");
		public static readonly Symbol PPEndRegion = GSymbol.Get("##endregion");
		public static readonly Symbol PPPragma = GSymbol.Get("##pragma");
		public static readonly Symbol PPError = GSymbol.Get("##error");
		public static readonly Symbol PPWarning = GSymbol.Get("##warning");
		public static readonly Symbol PPNote = GSymbol.Get("##note");
		public static readonly Symbol PPLine = GSymbol.Get("##line");

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
		public static readonly Symbol Void   = GSymbol.Get("#void");
		public static readonly Symbol String = GSymbol.Get("#string");
		public static readonly Symbol Char   = GSymbol.Get("#char");
		public static readonly Symbol Bool   = GSymbol.Get("#bool");
		public static readonly Symbol Int8   = GSymbol.Get("#int8");
		public static readonly Symbol Int16  = GSymbol.Get("#int16");
		public static readonly Symbol Int32  = GSymbol.Get("#int32");
		public static readonly Symbol Int64  = GSymbol.Get("#int64");
		public static readonly Symbol UInt8  = GSymbol.Get("#uint8");
		public static readonly Symbol UInt16 = GSymbol.Get("#uint16");
		public static readonly Symbol UInt32 = GSymbol.Get("#uint32");
		public static readonly Symbol UInt64 = GSymbol.Get("#uint64");
		public static readonly Symbol Single = GSymbol.Get("#single");
		public static readonly Symbol Double = GSymbol.Get("#double");
		public static readonly Symbol Decimal = GSymbol.Get("#decimal");
		public static readonly Symbol Object = GSymbol.Get("#object");
		public static readonly Symbol Dynamic = GSymbol.Get("#dynamic");

		// Tokens
		public static readonly Symbol Colon = GSymbol.Get(":");
		public static readonly Symbol Semicolon = GSymbol.Get(";");
		public static readonly Symbol Do = GSymbol.Get("#do");
		// Represents the C comma operator. All arguments are evaluated and
		// the result of the expression is the value of the last argument.
		public static readonly Symbol Comma = GSymbol.Get(",");

		// Trivia
		//public static readonly Symbol TriviaCommaSeparatedStmts = GSymbol.Get("#trivia_commaSeparated");
		public static readonly Symbol TriviaInParens = GSymbol.Get("#trivia_inParens");
		public static readonly Symbol TriviaMacroAttribute = GSymbol.Get("#trivia_macroAttribute");
		public static readonly Symbol TriviaDoubleVerbatim = GSymbol.Get("#trivia_doubleVerbatim");
		public static readonly Symbol TriviaUseOperatorKeyword = GSymbol.Get("#trivia_useOperatorKeyword");
		public static readonly Symbol TriviaForwardedProperty = GSymbol.Get("#trivia_forwardedProperty");
		// if #trivia_rawText has the string "eat my shorts!" attached to it, then
		// [#trivia_rawText] x; is printed "eat my shorts!x;". Similarly, the
		// other trivia nodes must be attached as attributes with a string Value.
		public static readonly Symbol TriviaRawTextBefore = GSymbol.Get("#trivia_rawTextBefore");
		public static readonly Symbol TriviaRawTextAfter = GSymbol.Get("#trivia_rawTextAfter");
		public static readonly Symbol TriviaSLCommentBefore = GSymbol.Get("#trivia_SLCommentBefore");
		public static readonly Symbol TriviaMLCommentBefore = GSymbol.Get("#trivia_MLCommentBefore");
		public static readonly Symbol TriviaSLCommentAfter = GSymbol.Get("#trivia_SLCommentAfter");
		public static readonly Symbol TriviaMLCommentAfter = GSymbol.Get("#trivia_MLCommentAfter");
		public static readonly Symbol TriviaSpaceBefore = GSymbol.Get("#trivia_SpaceBefore");
		public static readonly Symbol TriviaSpaceAfter = GSymbol.Get("#trivia_SpaceAfter");
		// In EC#, this trivia is placed on an identifier treated as an attribute (e.g. partial, async).
		public static readonly Symbol TriviaWordAttribute = GSymbol.Get("#trivia_WordAttribute");

		// #rawText must have a Value. The Value is converted to a string and
		// printed out by EcsNodePrinter without any filtering.
		public static readonly Symbol RawText = GSymbol.Get("#rawText");

		// NodeStyle.Alternate is used for: @"verbatim strings", 0xhex numbers, 
		// new-style casts x(->int), delegate(old-style lambdas) {...}

		public static bool IsArrayKeyword(Symbol s) { return CountArrayDimensions(s) > 0; }
		public static int CountArrayDimensions(Symbol s)
		{
			if (s.Name.Length >= 2 && s.Name.StartsWith("[") && s.Name[s.Name.Length-1] == ']') {
				for (int i = 1; i < s.Name.Length-1; i++)
					if (s.Name[i] != ',')
						return 0;
				return s.Name.Length-1;
			}
			return 0;
		}
		public static Symbol GetArrayKeyword(int dims)
		{
			if (dims <= 0) throw new ArgumentException("GetArrayKeyword(dims <= 0)");
			if (dims == 1) return Bracks;
			if (dims == 2) return TwoDimensionalArray;
			return GSymbol.Get("[" + new string(',', dims-1) + "]");
		}
	}
}
