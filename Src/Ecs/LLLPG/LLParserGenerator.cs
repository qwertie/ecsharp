using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc.Essentials;
using Loyc.Collections;
using Loyc.CompilerCore;
using Loyc.Collections.Impl;
using ecs;

namespace Loyc.LLParserGenerator
{
	using S = CodeSymbols;
	using Loyc.Utilities;

#if false
	TODO: use this table as a test suite for the parser

	General rules:
	- Variables and fields use #var(type, name1, name2(initial_value), name3)
	  Properties use #property(name, type, #{ body; }) instead.
	  The parser treats "var x" as #var(var, x), but #var(#missing, x) is canonical.
	- All spaces have the form #spacekind(name, #(inherited_types), #{body});
	  the third argument is omitted if the body is omitted.
	  e.g. #struct(Point<#T>, #(IPoint), #{ public int X, Y; });
	- Methods, operators and constructors use #def(retType, name, #(args), #{body});
	  the body can be omitted, or replaced with #==>(target) for forwarding.
	  "if" and "where" clauses are attached as #if and #where attributes.
	  e.g. #def(#double, Square, #(double x), #{ return x*x; });

	Standard EC# statements: Declarations       Prefix notation
	-------------------------------------       ---------------
	using System.Collections.Generic;           #import(System.Collections.Generic);
	using System { Linq, Text };                #import (System.Linq, System.Text);
	using Foo = Bar;                            [#fileLocal] #alias(Foo = Bar);
	extern alias Z;                             #extern_alias(Z);
	[assembly:Attr]                             [Attr] #assembly;
	case 123:                                   #case(123);
	default:                                    #label(#default);
	label_name:                                 #label(label_name);
	int x = 0;                                  #var(int, x(0));
	int* a, b = &x, c;                          #var(#*(int), a, b(&x), c);
	public partial class Foo<T> : IFoo {}       [#public, #partial] #class(Foo<T>, #(IFoo), {});
	struct Foo<\T> if default(T) + 0 is legal   [#if(default(T) + 0 is legal)] #struct(Foo<\T>, #missing, {});
	enum Foo : byte { A = 1, B, C, Z = 26 }     #enum(Foo, byte, #(A = 1, B, C, Z = 26));
	trait Foo<\T> : Stream { ... }              #trait(Foo<\T>, #(Stream), {...});
	interface Foo<T> : IEnumerable<T> { ... }   #interface(Foo<T>, #(IEnumerable<T>), {...});
	namespace Foo<T> { ... }                    #namespace(Foo<T>, #missing, {...});
	namespace Foo<T> { ... }                    #namespace(Foo<T>, #missing, {...});
	alias Map<K,V> = Dictionary<K,V>;           #alias(Foo<T> = Bar<T>);
	alias Foo = Bar : IFoo { ... }              #alias(Foo<T> = Bar<T>, #(IFoo), { ... });
	event EventHandler Click;                   #event(EventHandler, Click);
	event EventHandler A, B;                    #event(EventHandler, A, B));
	event EventHandler A { add { } remove { } } #event(EventHandler, A, { add({ }); remove({ }); }));
	delegate void foo<T>(T x) where T:class,X   [#where(T, #class, X)] #delegate(foo<T>, #(T x), void);
	public new partial string foo(int x);       [#public, #partial, #new] #def(#string, foo, #(int x));
	int foo(int x) => x * x;                    #def(int, foo, #(int x), { x * x; });
	int foo(int x) { return x * x; }            #def(int, foo, #(int x), { #return(x * x); });
	def foo(int x) ==> bar;                     [#def] #def(#missing, foo, #(int x), #==>(bar));
	int Foo { get; set; }                       #property(int, Foo, { get; set; })
	IEnumerator IEnumerable.GetEnumerator() { } #def(IEnumerator, IEnumerable.GetEnumerator, #(), { });
	new (int x) : this(x, 0) { y = x; }         #def(#missing, #new, #(int x), { #this(x, 0); y = x; });
	Foo (int x) : base(x) { y = x; }            #def(#missing, Foo,  #(int x), { #base(x); y = x; });
	~Foo () { ... }                             #def(#missing, #~(Foo), #(), { ... });
	static bool operator==(T a, T b) { ... }    [#static] #def(#bool, [#operator] #==, #(T a, T b), { ... });
	static implicit operator A(B b) { ... }     [#static, #implicit] #def(A, [#operator] #cast, #(B b), { ... });
	static explicit operator A<T><\T>(B<T> b);  [#static, #explicit] #def(A<T>, [#operator] #of<#cast, \T>, #(B<T> b));
	bool operator `when`(Cond cond) { ... }     #def(#bool, [#operator] when, #(Cond cond), { ... });

	Standard EC# statements: Executable         Prefix notation
	-----------------------------------         ---------------
	if (c) f();                                 #if(c, f());
	if (c) { f(); }                             #if(c, { f(); });
	if (c) a = 1, b = 2;                        #if(c, #(a = 1, b = 2));
	if (c) f(); else { g(); }                   #if(c, f(), { g() });
	for (int x = 0; x * y < 100; x++) f(x);     #for(#var(int, x(0)), x * y < 100, x++, f(x));
	foreach (var x in list) { ... }             #foreach(#var(var, x), list, { ... }) // not "#in(#var(var, x), list)" because that's unparsable
	while (x > 0) { ... }                       #while(x > 0, { ... })
	switch (c) { case '+', '-': goto default;   #switch(c, { #case('+', '-'); #goto(#default);
	             default: break; }                           #default; #break; }
	checked { ... }                             #checked({ ... })
	unchecked { ... }                           #unchecked({ ... })
	using (d = new Form()) { ... }              #using(d = new Form(), { ... })
	using (IDisposable d = new Form()) { ... }  #using(#var(IDisposable, d(new Form()), { ... })
	


	EC# expressions       Prefix notation            EC# expressions       Prefix notation 
	---------------       ---------------            ---------------       ---------------
	foo (or @foo)         foo                        a + b          
	food.pizza.cheese     #.(food, pizza, cheese)     
	.foo                  #.(foo)                     
	foo<A, B>             #of(foo, A, B)              
	operator ==           #operator(#==)              
	int                   #int                       
	int x = 0             #var(#int, x(0))           
    foo()::x              #:::(foo(), x)               
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      
                                                      

#endif

	/// <summary>A helper class for generating input nodes to the parser generator.</summary>
	/// <remarks>
	/// I was going to use this to bootstrap the EC# parser, but I'm not sure if 
	/// it's necessary now; I should probably just create predicates directly (<see cref="Pred"/>).
	/// </remarks>
	public class PGFactory : GreenFactory
	{
		public PGFactory() : base(EmptySourceFile.Unknown) { }
		public PGFactory(ISourceFile file) : base(file) { }

		public static readonly Symbol _Gate = GSymbol.Get("#=>");
		public static readonly Symbol _Star = GSymbol.Get("#*");
		public static readonly Symbol _Plus = GSymbol.Get("#+");
		public static readonly Symbol _Opt = GSymbol.Get("#?");
		public static readonly Symbol _Nongreedy = GSymbol.Get("nongreedy");
		public static readonly Symbol _Greedy = GSymbol.Get("greedy");
		public static readonly Symbol _rule = GSymbol.Get("rule");
		public static readonly Symbol _token = GSymbol.Get("token");

		public GreenNode Any { get { return Symbol(GSymbol.Get("_")); } } // represents any terminal

		public GreenNode Rule(string name, params GreenAtOffs[] sequence)
		{
			return Def(Symbol(_rule), GSymbol.Get(name), ArgList(), Braces(sequence));
		}
		public GreenNode Seq(params GreenAtOffs[] sequence) { return Call(S.Tuple, sequence); }
		public GreenNode Seq(params char[] sequence) { return Call(S.Tuple, sequence.Select(c => (GreenAtOffs)_(c)).ToArray()); }
		public GreenNode Star(params GreenAtOffs[] sequence) { return Call(_Star, sequence); }
		public GreenNode Plus(params GreenAtOffs[] sequence) { return Call(_Plus, sequence); }
		public GreenNode Opt(params GreenAtOffs[] sequence)  { return Call(_Opt,  sequence); }
		public GreenNode Nongreedy(GreenNode loop) { return Greedy(loop, false); }
		public GreenNode Greedy(GreenNode loop, bool greedy = true)
		{
			Debug.Assert(loop.Name == _Star || loop.Name == _Plus || loop.Name == _Opt);
			return Call(greedy ? _Greedy : _Nongreedy, loop);
		}
		public  GreenNode And(params GreenAtOffs[] sequence)  { return Call(S.AndBits, AutoS(sequence)); }
		public  GreenNode AndNot(params GreenAtOffs[] sequence) { return Call(S.Not, AutoS(sequence)); }
		public  GreenNode AndCode(params GreenAtOffs[] sequence) { return Call(S.AndBits, Code(sequence)); }
		public  GreenNode Code(params GreenAtOffs[] statements) { return Call(S.Braces, statements); }
		private GreenNode AutoS(GreenAtOffs[] sequence)
		{
			return sequence.Length == 1 ? sequence[0].Node : Seq(sequence);
		}
		public GreenNode _(char c)
		{
			return Literal(c);
		}
	}

	/// <summary>Encapsulates LLLPG, the Loyc Parser Generator for LL Parsers.</summary>
	/// <remarks>
	/// Note: the input to LLLPG is usually provided in the form of EC# source code:
	/// <code>
	/// [[GenerateLLParser]]
	/// class Tokenizer
	/// {
	///   token Id ==> #
	///     [ '@' { _verbatim=1; }? NormalIdStart NormalIdCont*
	///     | '@' { _verbatim=1; } SqString
	///     ];
	///   rule IdStart ==> #[ Letter | '_' ];
	///   rule IdCont  ==> #[ IdStart | ('0'..'9') ];
	///   rule Letter  ==> #[ 'a'..'z' | 'A'..'Z' | &{Char.IsLetter(LA0)} . ];
	///   bool _verbatim;
	/// }
	/// </code>
	/// In that case, there is no need to use this class directly.
	/// <para/>
	/// LLParserGenerator's job is to generate a parser (in the form of an EC# Loyc 
	/// tree) for a set of <see cref="Rule"/>s. Each rule represents a sub-parser, 
	/// whose job is to parse a single predicate (Pred object). LLParserGenerator
	/// can generate both lexers (which take characters as input) and parsers (which
	/// take tokens as input), and many low-level details of the parsing process can 
	/// be customized.</li>
	/// <para/>
	/// Parser generation has the following steps:
	/// <ol>
	/// <li><see cref="AddRules"/>(): scan the body of a class or other statement list
	///     and build a set of Rule objects, one per rule() method or property in the 
	///     source code (alternately, you can add Rule objects directly)</li>
	/// <li><see cref="DetermineLocalFollowSets"/>: scan all predicates of all rules 
	///     and notify each predicate of the predicate that follows it by setting 
	///     <see cref="Pred.Next"/>.</li>
	/// <li><see cref="DetermineRuleFollowSets"/>: scan all predicates of all rules 
	///     looking for <see cref="RuleRef"/>s. For each RuleRef found, add the value 
	///     of <see cref="Pred.Next"/> to the follow set of the rule (stored in 
	///     <see cref="Rule.EndOfRule"/>).</li>
	/// <li>The actual parser can now be generated. Generating code for sequences 
	///     like '(' 'a'..'z' ')' is trivial; the biggest difficulty is generating 
	///     prediction code. See below for details about how prediction (and 
	///     matching) works.
	/// </li>
	/// <li></li>
	/// </ol>
	/// Typically, the majority of a parser consists of prediction code. In 
	/// general, the job of prediction code is to figure out, given some input 
	/// sequence, which one of a number of "alternatives" match that sequence. If 
	/// none of the alternatives match the sequence, the input has an error (or 
	/// from a Chomskian perspective, the input cannot be generated by the 
	/// grammar). If more than one alternative matches the input sequence then the 
	/// grammar is ambiguous; in that case the parser will select the first 
	/// alternative that matches. Also, the parser generator will detect the 
	/// ambiguity as it generates the prediction code and issue a warning about it, 
	/// unless the grammar does something to suppress the warning, such as using 
	/// the '/' operator instead of '|', which means "I know it's ambiguous--just
	/// pick the first matching clause".
	/// <para/>
	/// The following kinds of grammar elements require prediction:
	/// <para/>
	/// <ul>
	/// <li><c>a | b</c> (which is equivalent to <c>a / b</c>): prediction chooses between a and b</li>
	/// <li><c>a?</c>: prediction chooses between a and whatever follows a?</li>
	/// <li><c>a*</c>: prediction chooses between a and whatever follows a*</li>
	/// <li><c>(a | b)*: </c>prediction chooses between three alternatives (a, b, and exiting the loop).</li>
	/// <li><c>(a | b)?: </c>prediction chooses between three alternatives (a, b, and nothing)</c>.</li>
	/// <li><c>a+</c>: equivalent to <c>a a*</c></li>
	/// </ul>
	/// Let's look at a simple example of the prediction code generated for a rule 
	/// called "Foo":
	/// <code>
	/// // rule a ==> #[ 'a' | 'A' ];
	/// // rule b ==> #[ 'b' | 'B' ];
	/// // public rule Foo ==> #[ a | b ];
	/// public void Foo()
	/// {
	///   var la0 = LA(0);
	///   if (la0 == 'a' || la0 == 'A')
	///     a();
	///   else
	///     b();
	/// }
	/// </code>
	/// By default, to make prediction more efficient, the last alternative is 
	/// assumed to match if the others don't. So when <c>a</c> doesn't match, <c>b</c>
	/// is called even though it has not been verified to match yet. This behavior
	/// can be changed by putting the <c>[MatchLastByDefault(false)]</c> attribute 
	/// on either a single rule or on the entire parser class (NOT IMPLEMENTED).
	/// Alternatively, you can select the default using the default(...) pseudo-
	/// function, e.g.
	/// <code>
	/// // public rule Foo ==> #[ default(a) | b ];
	/// public void Foo()
	/// {
	///   var la0 = LA(0);
	///   if (la0 == 'b' || la0 == 'B')
	///     b();
	///   else
	///     a();
	/// }
	/// </code>
	/// In general, the prediction and matching phases are physically separated:
	/// First an if-else chain does prediction, and then a second if-else chain
	/// reacts to the choice that was made. However, in simple cases like this one
	/// that only require LL(1) prediction, prediction and matching are merged 
	/// into a single if-else chain. The if-else statements are the prediction 
	/// part of the code, while the calls to a() and b() are the matching part.
	/// <para/>
	/// Here's another example:
	/// <code>
	/// // public rule Foo ==> #[ (a | b? 'c')* ];
	/// public void Foo()
	/// {
	///   for (;;) {
	///     var la0 = LA(0);
	///     if (la0 == 'a' || la0 == 'A')
	///       a();
	///     else if (la0 == 'b' || la0 == 'B' || la0 == 'c') {
	///       do {
	///         la0 = LA(0);
	///         if (la0 == 'b' || la0 == 'B')
	///           b();
	///       } while(false);
	///       Match('c');
	///     }
	///   }
	/// }
	/// </code>
	/// A kleene star (*) always produces a "for(;;)" loop, while an optional item
	/// always produces a "do ... while(false)" pseudo-loop. Here there are two
	/// separate prediction phases: one for the outer loop <c>(a | b? 'c')*</c>,
	/// and one for <c>b?</c>.
	/// <para/>
	/// In this example, the loop appears at the end of the rule. In some such 
	/// cases, the "follow set" of the rule becomes relevant. In order for the 
	/// parser to decide whether to exit the loop or not, it may need to know what 
	/// can follow the loop. For instance, if <c>('a' 'b')*</c> is followed by 
	/// <c>('a'..'z'+)</c>, it is not always possible to tell whether to stay in 
	/// the loop or exit just by looking at the first input character. If LA(0) is 
	/// 'a', it is necessary to look at the second character; only if the second 
	/// character is 'b' is it possible to conclude that 'a' 'b' should be matched.
	/// <para/>
	/// Therefore, before generating a parser one of the steps is to build the 
	/// follow set of each rule, by looking for places where a rule appears inside
	/// other rules. A rule is not aware of its current caller, so it gathers 
	/// information from all call sites. When a rule is marked "public", it is 
	/// considered to be a starting rule, which causes the follow set to include
	/// $ (which means "end of input").
	/// <para/>
	/// The worst-case follow set is <c>.* $</c> (anything), because essentially
	/// the prediction step must look ahead by the maximum number of characters or
	/// tokens (called k, as in "LL(k)") to decide whether to stay in the loop or 
	/// exit, and even after looking k characters ahead it is often impossible to
	/// disambiguate (in particular, the maximum lookahead is required when an arm
	/// contains a loop.)
	/// <para/>
	/// To mitigate this problem, there are separate "k" values for follow sets 
	/// and token follow sets. When generating a lexer, the follow set of any 
	/// token tends to be close to <c>.* $</c>, i.e. tokens can be almost anything.
	/// Specifically, in computer languages the acceptable input is almost the 
	/// entire ASCII character set, plus perhaps unicode characters that can be
	/// used as identifiers. The default <see cref="TokenFollowSetK"/> is 1, which
	/// means "only look ahead one character to decide whether to exit a loop".
	/// You can also set it to 0, which means "don't pay attention to the follow
	/// set at all, just use the minimum possible lookahead to figure out whether 
	/// the non-exit arms could possibly match."
	/// <para/>
	/// By the way, if the follow set is <c>.* $</c> and you use a nongreedy()
	/// loop, the loop will never execute since <c>.* $</c> matches anything and
	/// is always preferred.
	/// <para/>
	/// Here's an example that needs more than one character of lookahead:
	/// <code>
	/// // public rule Foo ==> #[ 'a'..'z'+ | 'x' '0'..'9' '0'..'9' ];
	/// public void Foo()
	/// {
	///   int la0, la1;
	///   la0 = LA(0);
	///   int alt = 0;
	///   if (la0 == 'x') {
	///     la1 = LA(1);
	///     if (la1 >= '0' && '9' >= la1) {
	///       Match();
	///       Match();
	///       MatchRange('0', '9');
	///     } else
	///       alt = 1;
	///   } else
	///     alt = 1;
	/// 
	///   if (alt == 1) {
	///     Match();
	///     for (;;) {
	///       la0 = LA(0);
	///       if (la0 >= 'a' && 'z' >= la0)
	///         Match();
	///       else
	///         break;
	///     }
	///   }
	/// }
	/// </code>
	/// Here, the prediction and matching phases are merged for the second 
	/// alternative, but separate for the first alternative (because it is chosen 
	/// in two different places in the prediction logic). Notice that the matching 
	/// for alt 2 starts with <c>Match()</c> twice, with no arguments, but is 
	/// followed by <c>MatchRange('a', 'z')</c>. This demonstrates communication 
	/// between prediction and matching: the matching phase can tell that LA(0) is 
	/// confirmed to be 'x', and LA(1) is confirmed to be '0'..'9', so an 
	/// unconditional match suffices. However, nothing is known about LA(2) so its 
	/// value must be checked, which is what MatchRange() is supposed to do.
	/// <para/>
	/// In some cases, LA(0) is irrelevant. Consider this example:
	/// <code>
	/// // public rule Foo ==> #[ '(' 'a'..'z'* ')' | '(' '0'..'9'+ ')' ];
	/// public void Foo()
	/// {
	///   int la0, la1;
	///   la1 = LA(1);
	///   if (la1 >= 'a' && 'z' >= la1) {
	///     Match('(');
	///     for (;;) {
	///       la0 = LA(0);
	///       if (la0 >= 'a' && 'z' >= la0)
	///         Match();
	///       else
	///         break;
	///     }
	///     Match(')');
	///   } else {
	///     Match('(');
	///     MatchRange('0', '9');
	///     for (;;) {
	///       la0 = LA(0);
	///       if (la0 >= '0' && '9' >= la0)
	///         Match();
	///       else
	///         break;
	///     }
	///     Match(')');
	///   }
	/// }
	/// </code>
	/// Here, the first character of both alternatives is always '(', so looking at
	/// LA(0) doesn't help choose which branch to take, and prediction skips ahead
	/// to LA(1).
	/// </remarks>
	public class LLParserGenerator : PGFactory
	{
		public const int EOF = PGIntSet.EOF;

		#region Tests
		void Seq()
		{
			
		}
		#endregion

		Dictionary<Symbol, Rule> _rules = new Dictionary<Symbol,Rule>();
		HashSet<Rule> _tokens = new HashSet<Rule>();
		public int DefaultK = 2;
		public int FollowSetK = 2;
		public int TokenFollowSetK = 1;
		public bool NoDefaultArm = false;

		#region Step 1: AddRules() and related

		public LLParserGenerator() { }

		public Dictionary<Symbol, Rule> AddRules(Node stmtList)
		{
			_rules = new Dictionary<Symbol, Rule>();
			foreach (var stmt in stmtList.Args)
			{
				if (stmt.Calls(S.Def, 4))
				{
					bool isToken;
					var name = stmt.Args[0].Name;
					if ((isToken = name == _token) || stmt.Args[0].Name == _rule) try
						{
							var body = stmt.Args[3];
							var expr = body.Args[body.Args.Count - 1];
							var rule = new Rule(expr, name, NodeToPred(expr), stmt.TryGetAttr(S.Public) != null) { IsToken = isToken };
							AddRule(rule);
						}
						catch (Exception ex)
						{
							Console.WriteLine("ConvertRule failed: " + ex.Message);
						}
				}
			}
			return _rules;
		}
		public void AddRules(IEnumerable<Rule> rules)
		{
			foreach (var rule in rules)
				AddRule(rule);
		}
		public void AddRule(Rule rule)
		{
			_rules.Add(rule.Name, rule);
			if (rule.IsToken)
				_tokens.Add(rule);
		}

		enum Context { Rule, GateLeft, GateRight, And };

		private Pred NodeToPred(Node expr, Context ctx = Context.Rule)
		{
			if (expr.IsCall)
			{
				bool slash = false, not, orTerminals;
				if (expr.Calls(S.DotDot, 2) && expr.Args[0].IsLiteral && expr.Args[1].IsLiteral)
				{
					object v0 = expr.Args[0].Value, v1 = expr.Args[1].Value;
					if (v0 is char && v1 is char)
						return new TerminalPred(expr, (char)v0, (char)v1);
				}
				else if (expr.CallsMin(S.Tuple, 1))
				{
					// sequence: (a, b, c)
					if (expr.Calls(S.Tuple, 1))
						return NodeToPred(expr.Args[0], ctx);
					return ArgsToSeq(expr, ctx);
				}
				else if ((orTerminals = expr.Calls(S.OrBits, 2)) || expr.Calls(S.Or, 2) || (slash = expr.Calls(S.Div, 2)))
				{
					// alternatives: a | b, a || b, a / b
					var left = NodeToPred(expr.Args[0], ctx);
					var right = NodeToPred(expr.Args[1], ctx);
					var lt = AsTerminalSet(left);
					var rt = AsTerminalSet(right);
					if (lt != null && rt != null && lt.CanMerge(rt))
						return lt.Merge(rt);
					else
					{
						//if (orTerminals)
						//	throw new ArgumentException("Cannot use '{0}' as an argument to '|' because it is not a terminal set. Use '||' instead.",
						//		expr.Args[lt != null ? 1 : 0].ToString());
						return new Alts(expr, left, right, slash);
					}
				}
				else if (expr.CallsMin(_Star, 1) || expr.CallsMin(_Plus, 1) || expr.CallsMin(_Opt, 1))
				{
					// loop (a`+`, a`*`) or optional (a`?`)
					var type = expr.Name;
					bool greedy = true;
					Pred subpred = null;
					if (expr.ArgCount == 1)
					{ // +, * or ? had only one argument (usual case)
						expr = expr.Args[0];
						if ((greedy = expr.CallsMin(_Greedy, 1)) || expr.CallsMin(_Nongreedy, 1))
						{
							slash = true; // ignore ambiguous
							if (expr.Args.Count == 1)
								subpred = NodeToPred(expr.Args[0], ctx);
							else
								subpred = ArgsToSeq(expr, ctx);
						}
						else
							subpred = NodeToPred(expr, ctx);
					}
					else
					{
						subpred = ArgsToSeq(expr, ctx);
					}

					if (type == _Opt)
						return new Alts(expr, LoopMode.Opt, subpred) { Greedy = greedy };
					if (type == _Plus)
					{
						var seq = new Seq(expr);
						seq.List.Add(subpred);
						seq.List.Add(new Alts(expr, LoopMode.Star, subpred));
						return seq;
					}
					return new Alts(expr, LoopMode.Star, subpred) { Greedy = greedy };
				}
				else if (expr.Calls(_Gate, 2))
				{
					if (ctx == Context.GateLeft || ctx == Context.GateRight)
						throw new ArgumentException(Localize.From(
							"Cannot use a gate ('{0}') inside another gate", expr));
					return new Gate(expr, NodeToPred(expr.Args[0], Context.GateLeft),
										  NodeToPred(expr.Args[1], Context.GateRight));
				}
				else if ((not = expr.Calls(S.Not, 1)) || expr.Calls(S.AndBits, 1))
				{
					expr = expr.Args[0];
					var subpred = AutoNodeToPred(expr, Context.And);
					var subpred2 = subpred as AndPred;
					if (subpred2 != null)
					{
						subpred2.Not ^= not;
						return subpred2;
					}
					else
						return new AndPred(expr, subpred, not);
				}
			}
			else
			{
				// Non-call
				while (expr.Head != null) // eliminate parenthesis
					expr = expr.Head;
				if (expr.IsLiteral && expr.Value is char)
					return new TerminalPred(expr, (char)expr.Value);
				if (expr.IsSimpleSymbol)
					return new RuleRef(expr, _rules[expr.Name]);
			}
			throw new ArgumentException("Unrecognized expression '{0}'", expr.ToString());
		}
		private Seq ArgsToSeq(Node expr, Context ctx)
		{
			var objs = expr.Args.Select(node => AutoNodeToPred(node, ctx)).ToList();
			Seq seq = new Seq(expr);
			Node action = null;
			for (int i = 0; i < objs.Count; i++)
			{
				if (objs[i] is Node)
				{
					var code = objs[i] as Node;
					if (ctx == Context.And || ctx == Context.GateLeft)
						throw new ArgumentException(Localize.From(ctx == Context.And ?
							"Cannot use an action block ('{0}') inside an '&' or '!' predicate; these predicates are for prediction only." :
							"Cannot use an action block ('{0}') on the left side of a '=>' gate; the left side is for prediction only.", objs[i].ToString()));
					action = Pred.AppendAction(action, code);
				}
				else // Pred
				{
					Pred pred = (Pred)objs[i];
					pred.PreAction = action;
					action = null;
					seq.List.Add(pred);
				}
			}
			if (action != null)
				seq.PostAction = action;
			return seq;
		}
		private object AutoNodeToPred(Node expr, Context ctx)
		{
			if (expr.CallsMin(S.Braces, 0))
				return expr; // code
			return NodeToPred(expr, ctx);
		}
		static TerminalPred AsTerminalSet(Pred pred)
		{
			if (pred is RuleRef)
				return AsTerminalSet(((RuleRef)pred).Rule.Pred);
			if (pred is TerminalPred)
				return (TerminalPred)pred;
			return null;
		}

		#endregion

		#region Step 2: DetermineFollowSets() and related

		void DetermineFollowSets()
		{
			foreach (Rule rule in _rules.Values)
				new DetermineLocalFollowSets().Run(rule);

			// Each rule's Next is always an EndOfRule object, which has a list 
			// of things that could follow the rule elsewhere in the grammar.
			// To determine the follow set of each rule, me must find all places 
			// where the rule is used...
			new DetermineRuleFollowSets(_rules).Run();
		}

		class DetermineLocalFollowSets : PredVisitor
		{
			TerminalPred AnyFollowSet = TerminalPred.AnyFollowSet();

			public void Run(Rule rule)
			{
				Visit(rule.Pred, rule.EndOfRule);
			}
			void Visit(Pred pred, Pred next)
			{
				pred.Next = next;
				pred.Call(this);
			}

			public override void Visit(Seq seq)
			{
				var next = seq.Next;
				for (int i = seq.List.Count - 1; i >= 0; i--)
				{
					Visit(seq.List[i], next);
					next = seq.List[i];
				}
			}
			public override void Visit(Alts alts)
			{
				for (int i = 0; i < alts.Arms.Count; i++)
					Visit(alts.Arms[i], alts.Next);
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Match, gate);
				Visit(gate.Predictor, AnyFollowSet);
			}
			public override void Visit(AndPred pred)
			{
				var child = pred.Pred as Pred;
				if (child != null)
					Visit(child, AnyFollowSet);
			}
		}

		class DetermineRuleFollowSets : RecursivePredVisitor
		{
			private Dictionary<Symbol, Rule> _rules;
			public DetermineRuleFollowSets(Dictionary<Symbol, Rule> rules) { _rules = rules; }

			public void Run()
			{
				foreach (Rule rule in _rules.Values)
					rule.Pred.Call(this);
			}
			public override void Visit(RuleRef rref)
			{
				if (rref.Next is EndOfRule)
					rref.Rule.EndOfRule.FollowSet.UnionWith((rref.Next as EndOfRule).FollowSet);
				else
					rref.Rule.EndOfRule.FollowSet.Add(rref.Next);
			}
		}

		#endregion

		#region Step 3: code generation

		protected GreenFactory F; // initialized by GenerateCode
		Dictionary<IPGTerminalSet, Symbol> _setDeclNames;
		protected int _setNameCounter = 0;

		public Node GenerateCode(Symbol className, ISourceFile sourceFile)
		{
			DetermineFollowSets();

			F = new GreenFactory(sourceFile);
			_setDeclNames = new Dictionary<IPGTerminalSet, Loyc.Symbol>();

			// TODO use class body provided by user
			var greenClass = F.Attr(F.Public, F.Symbol(S.Partial), F.Call(S.Class, F.Symbol(className), F.List(), F.Braces()));
			var result = Node.FromGreen(greenClass, -1);

			var generator = new GenerateCodeVisitor(this, F, result.Args[2]);
			foreach(var rule in _rules.Values)
				generator.Generate(rule);
			return result;
		}

		class GenerateCodeVisitor : PredVisitor
		{
			public LLParserGenerator LLPG;
			public GreenFactory F;
			Node _backupBasis;
			Rule _currentRule;
			Node _classBody; // Location to generate terminal sets
			Node _target; // Location where we're currently generating code
			ulong _laVarsNeeded;
			bool _usingIntAlt;
			int _k;

			public GenerateCodeVisitor(LLParserGenerator llpg, GreenFactory f, Node classBody)
			{
				LLPG = llpg;
				F = f;
				_classBody = classBody;
				_backupBasis = CompilerCore.Node.NewSynthetic(GSymbol.Get("nowhere"), new SourceRange(F.File, -1, -1));
			}

			public static readonly Symbol _alt = GSymbol.Get("alt");

			public void Generate(Rule rule)
			{
				_currentRule = rule;
				_k = rule.K > 0 ? rule.K : LLPG.DefaultK;
				//   ruleMethod = @{ public void \(F.Symbol(rule.Name))() { } }
				Node ruleMethod = Node.FromGreen(F.Attr(F.Public, F.Def(F.Void, F.Symbol(rule.Name), F.List(), F.Braces())));
				Node body = _target = ruleMethod.Args[3];
				_laVarsNeeded = 0;
				_usingIntAlt = false;
				LLPG._setNameCounter = 0;
				
				Visit(rule.Pred);

				if (_usingIntAlt)
					body.Args.Insert(0, Node.FromGreen(F.Var(F.Int32, _alt, F.Literal(0))));
				if (_laVarsNeeded != 0) {
					Node laVars = Node.FromGreen(F.Call(S.Var, LLPG.LAType()));
					for (int i = 0; _laVarsNeeded != 0; i++, _laVarsNeeded >>= 1)
						if ((_laVarsNeeded & 1) != 0)
							laVars.Args.Add(Node.NewSynthetic(GSymbol.Get("la" + i.ToString()), F.File));
					body.Args.Insert(0, laVars);
				}
				_classBody.Args.Add(ruleMethod);
			}

			new public void Visit(Pred pred)
			{
				if (pred.PreAction != null)
					_target.Args.AddSpliceClone(pred.PreAction);
				pred.Call(this);
				if (pred.PostAction != null)
					_target.Args.AddSpliceClone(pred.PostAction);
			}

			void VisitWithNewTarget(Pred toBeVisited, Node target)
			{
				Node old = _target;
				_target = target;
				Visit(toBeVisited);
				_target = old;
			}

			public override void Visit(Alts alts)
			{
				bool haveLoop = false;

				// Generate a loop body for (...)* or (...)?:
				var target = _target;
				if (alts.Mode == LoopMode.Star) {
					// (...)* => for (;;) {}
					var loop = Node.FromGreen(F.Call(S.For, new GreenAtOffs[] { F._Missing, F._Missing, F._Missing, F.Braces() }));
					_target.Args.Add(loop);
					target = loop.Args[3];
					haveLoop = true;
				} else if (alts.Mode == LoopMode.Opt) {
					// (...)? => do {} while(false); IF the exit branch is NOT the default
					// If the exit branch is the default, then no loop and no "break" is needed.
					if ((uint)alts.DefaultArm < (uint)alts.Arms.Count)
					{
						var loop = Node.FromGreen(F.Call(S.Do, F.Braces(), F.@false));
						_target.Args.Add(loop);
						target = loop.Args[0];
						haveLoop = true;
					}
				}

				// Get statements to run when prediction succeeds on each branch.
				// Initially it's just "alt = i" (or "break" for the exit branch)
				var handlers = new AltHandlers[alts.ArmCountPlusExit];
				int i;
				var alt = F.Symbol(_alt);
				for (i = 0; i < alts.Arms.Count; i++)
					handlers[i] = new AltHandlers(i, ToNode(F.Call(S.Set, alt, F.Literal(i + 1)), alts.Arms[i].Basis));
				if (alts.HasExit)
					handlers[i] = new AltHandlers(-1, ToNode(haveLoop ? F.Call(S.Break) : F.Symbol(S.Missing), alts.Basis));

				var firstSets = LLPG.ComputeFirstSets(alts, handlers);

				// *** PREDICT! ***
				Node tree = GeneratePredictionTree(alts, firstSets);

				// Generate matching code for each arm
				Node prevIf = null, ifChain = null;
				for (i = 0; i < alts.Arms.Count; i++)
				{
					var hs = handlers[i];
					Node matchingCode = Node.NewSynthetic(S.Braces, F.File);
					VisitWithNewTarget(alts.Arms[i], matchingCode);

					if (hs.Nodes.Count > 1 && (matchingCode.ArgCount != 1 || matchingCode.Args[0].Name == S.If))
					{
						_usingIntAlt = true;
						//   ifThen = @{ if (alt == \(i+1)) \matchingCode; }
						Node ifThen = Node.FromGreen(F.Call(S.If, F.Call(S.Eq, alt, F.Literal(i + 1)), matchingCode.FrozenGreen));
						if (prevIf == null)
							ifChain = ifThen;
						else
							prevIf.Args.Add(ifThen);
						prevIf = ifThen;
					}
					else
					{
						// Put handler directly in prediction tree, replacing "alt=i;" statement
						foreach (var before in hs.Nodes)
						{
							var after = matchingCode;
							if (after.ArgCount == 1)
								after = after.Args[0].Detach();
							if (before.Parent != null)
								before.Parent.SetChild(before.IndexInParent, after);
							else
							{	// This can occur if the prediction tree is trivial, i.e. 
								// always makes the same decision. Then GeneratePredictionTree()
								// simply returns the handler for the chosen alternative, which
								// has no parent. So just change 'tree' from 'before' to 'after'.
								Debug.Assert(before == tree);
								tree = after;
							}
						}
					}
				}

				if (tree.Calls(S.Braces)) {
					while (tree.ArgCount != 0)
						target.Args.Add(tree.TryGetArg(0).Detach());
				} else
					target.Args.Add(tree);

				if (ifChain != null)
					target.Args.Add(ifChain);
			}

			Node ToNode(GreenNode gnode, Node forSourceIndex)
			{
				return Node.FromGreen(gnode, (forSourceIndex ?? _backupBasis).SourceIndex);
			}

			Node GeneratePredictionTree(Alts alts, Pair<KthSet,AltHandlers>[] kthSets)
			{
				var thisBranchAlts = new List<AltHandlers>();
				var predictionTable = new List<Pair<IPGTerminalSet, Node>>();
				int lookahead = kthSets[0].A.LA;
				var laVar = F.Symbol("la" + lookahead.ToString());
				Debug.Assert(kthSets.All(p => p.A.LA == lookahead));
				int i;

				IPGTerminalSet covered = TrivialTerminalSet.Empty();
				for (;;)
				{
					thisBranchAlts.Clear();
					// e.g. given an Alts value of ('0' '0'..'7'+ | '0'..'9'+), 
					// ComputeSetForNextBranch finds the set '0' in the first 
					// iteration, '1'..'9' on the second iteration, and finally null.
					IPGTerminalSet set = ComputeSetForNextBranch(kthSets, thisBranchAlts, ref covered);
					if (set == null)
						break;
					if (thisBranchAlts.Count == 1 || lookahead + 1 >= _k)
					{
						var handlers = thisBranchAlts[0];
						handlers.Nodes.Add(handlers.DefaultNode.Clone());
						predictionTable.Add(G.Pair(set, handlers.Nodes.Last));
					}
					else
					{
						Debug.Assert(thisBranchAlts.Count > 1);
						var nextSets = LLPG.ComputeNextSets(kthSets, set);
						Node subtree = GeneratePredictionTree(alts, nextSets);
						predictionTable.Add(G.Pair(set, subtree));
					}
				}
				for (i = 0; i < predictionTable.Count; i++)//TEMP
				{
					Debug.Assert(!predictionTable[i].B.HasParent);
				}
				Debug.Assert(predictionTable.Count >= 1);
				bool needErrorBranch = LLPG.NoDefaultArm && !covered.ContainsEverything;

				// In a scenario such as LA(0) of (. 'A'..'Z' | . '0'..'9'), it is 
				// possible that there is only one entry in the prediction table. 
				// In that case, don't generate any prediction code for this amount
				// of lookahead.
				if (predictionTable.Count == 1 && !needErrorBranch)
					return predictionTable[0].B;

				// Declare laVar
				_laVarsNeeded |= 1ul << lookahead;
				// block = @{#{ \laVar = \(LA(context.Count)); }}
				Node block = Node.FromGreen(F.Braces(F.Call(S.Set, laVar, LLPG.LA(lookahead))));

				// From the prediction table, generate a chain of if-else 
				// statements in reverse, starting with the final "else" clause
				Node @else;
				i = predictionTable.Count;
				if (needErrorBranch)
					@else = LLPG.ErrorBranch(_currentRule, covered);
				else
					@else = predictionTable[--i].B;
				for (--i; i >= 0; i--)
				{
					Node test = GenerateTest(predictionTable[i].A, laVar);
					Node @if = Node.NewSynthetic(S.If, F.File);
					@if.Args.Add(test);
					@if.Args.Add(predictionTable[i].B);
					if (!@else.IsSimpleSymbolWithoutPAttrs(S.Missing))
						@if.Args.Add(@else);
					@else = @if;
				}
				block.Args.Add(@else);
				return block;
			}
			private Node GenerateTest(IPGTerminalSet set, GreenNode laVar)
			{
				// TODO: simplify tests using "dontcares"
				var laVar_ = Node.FromGreen(laVar);
				Node test = set.GenerateTest(laVar_, null);
				if (test == null)
				{
					var setName = LLPG.GenerateSetDecl(_classBody, _currentRule, set);
					test = set.GenerateTest(laVar_, setName);
				}
				return test;
			}
			private static IPGTerminalSet ComputeSetForNextBranch(Pair<KthSet,AltHandlers>[] kthSets, List<AltHandlers> thisBranchAlts, ref IPGTerminalSet covered)
			{
				int i;
				IPGTerminalSet set = null;
				for (i = 0; ; i++)
				{
					if (i == kthSets.Length)
						return null; // done!
					set = kthSets[i].A.Set.Subtract(covered) ?? covered.Intersection(kthSets[i].A.Set, false, true);
					if (!set.IsEmptySet)
						break;
				}

				thisBranchAlts.Add(kthSets[i].B);
				for (i++; i < kthSets.Length; i++)
				{
					var next = set.Intersection(kthSets[i].A.Set) ?? kthSets[i].A.Set.Intersection(set);
					if (!next.IsEmptySet)
					{
						set = next;
						thisBranchAlts.Add(kthSets[i].B);
					}
				}
				covered = covered.Union(set) ?? set.Union(covered);
				return set;
			}

			public override void Visit(Seq pred)
			{
				foreach (var p in pred.List)
					Visit(p);
			}
			public override void Visit(Gate pred)
			{
				Visit(pred.Match);
			}
			public override void Visit(AndPred pred)
			{
				// ignore, for now
			}
			public override void Visit(RuleRef rref)
			{
				_target.Args.Add(Node.FromGreen(F.Call(rref.Rule.Name)));
			}
			public override void Visit(TerminalPred term)
			{
				if (term.Set.ContainsEverything)
					_target.Args.Add(LLPG.GenerateMatch());
				else
					_target.Args.Add(LLPG.GenerateMatch(_classBody, _currentRule, term.Set));
			}
		}

		#endregion

		protected static readonly Symbol _Match = GSymbol.Get("Match");
		protected static readonly Symbol _MatchExcept = GSymbol.Get("MatchExcept");
		protected static readonly Symbol _MatchRange = GSymbol.Get("MatchRange");
		protected static readonly Symbol _MatchExceptRange = GSymbol.Get("MatchExceptRange");

		#region Generators of little code snippets

		protected virtual Symbol GenerateSetName(Rule currentRule)
		{
			return GSymbol.Get(string.Format("{0}_set{1}", currentRule.Name.Name, _setNameCounter++));
		}
		
		protected virtual Symbol GenerateSetDecl(Node classBody, Rule currentRule, IPGTerminalSet set)
		{
			Symbol setName;
			if (_setDeclNames.TryGetValue(set, out setName))
				return setName;
			
			setName = GenerateSetName(currentRule);
			classBody.Args.Add(set.GenerateSetDecl(setName));
			
			return _setDeclNames[set] = setName;
		}


		/// <summary>Generate code to match any token.</summary>
		/// <returns>Default implementation returns <c>@{ Match(); }</c>.</returns>
		protected virtual Node GenerateMatch() // match anything
		{
			return Node.FromGreen(F.Call(_Match));
		}

		/// <summary>Generate code to match a set, e.g. 
		/// <c>@{ MatchRange('a', 'z');</c> or <c>@{ MatchExcept('\n', '\r'); }</c>.
		/// If the set is too complex, a declaration for it is created in classBody.</summary>
		protected virtual Node GenerateMatch(Node classBody, Rule currentRule, IPGTerminalSet set_)
		{
			var set = set_ as PGIntSet;
			if (set != null) {
				if (set.Complexity(2, 3, !set.Inverted) <= 6) {
					Node call;
					Symbol matchMethod = set.Inverted ? _MatchExcept : _Match;
					if (set.Complexity(1, 2, true) > set.Count) {
						Debug.Assert(!set.IsSymbolSet);
						matchMethod = set.Inverted ? _MatchExceptRange : _MatchRange;
						call = Node.FromGreen(F.Call(matchMethod));
						for (int i = 0; i < set.Count; i++) {
							if (!set.Inverted || set[i].Lo != EOF || set[i].Hi != EOF) {
								call.Args.Add(Node.FromGreen(set.MakeLiteral(set[i].Lo)));
								call.Args.Add(Node.FromGreen(set.MakeLiteral(set[i].Hi)));
							}
						}
					} else {
						call = Node.FromGreen(F.Call(matchMethod));
						for (int i = 0; i < set.Count; i++) {
							var r = set[i];
							for (int c = r.Lo; c <= r.Hi; c++) {
								if (!set.Inverted || c != EOF)
									call.Args.Add(Node.FromGreen(set.MakeLiteral(c)));
							}
						}
					}
					return call;
				}
			}
			
			var tset = set_ as TrivialTerminalSet;
			if (tset != null)
				return GenerateMatch(classBody, currentRule,
					new PGIntSet(false, tset.Inverted) { ContainsEOF = tset.ContainsEOF });

			var setName = GenerateSetDecl(classBody, currentRule, set_);
			return Node.FromGreen(F.Call(_Match, F.Symbol(setName)));
		}

		/// <summary>Generates code to read LA(k).</summary>
		/// <returns>Default implementation returns @(LA(k)).</returns>
		protected virtual GreenNode LA(int k)
		{
			return F.Call(GSymbol.Get("LA"), F.Literal(k));
		}
		
		/// <summary>Generates code for the error branch of prediction.</summary>
		/// <param name="currentRule">Rule in which the code is generated.</param>
		/// <param name="covered">The permitted token set, which the input did not match.</param>
		protected virtual Node ErrorBranch(Rule currentRule, IPGTerminalSet covered)
		{
			return Node.FromGreen(F.Literal("TODO: Report error to user"));
		}

		/// <summary>Returns the data type of LA(k)</summary>
		/// <returns>Default implementation returns @(int).</returns>
		protected virtual GreenNode LAType()
		{
			return F.Int32;
		}

		#endregion

		#region Prediction analysis code
		// Helper code for GenerateCodeVisitor.GeneratePredictionTree()

		/// <summary>Keeps track of the code that runs when the prediction tree 
		/// decides that a particular branch (in Alts) should be executed.</summary>
		/// <remarks>
		/// Often, an alternative can show up multiple times in a prediction tree.
		/// The default handler for an arm of Alts is just "alt = i;", but in many
		/// cases we can replace that with the actual prediction code itself. By 
		/// keeping track of each copy of "alt = i", we can replace them after 
		/// building the prediction tree.
		/// </remarks>
		protected class AltHandlers
		{
			public AltHandlers(int index, Node handler)
			{
				Index = index;
				//handler.Value = this;
				DefaultNode = handler;
				Nodes = InternalList<Node>.Empty;
			}
			public int Index;                  /// -1 for exit, 0 for first alternative, 1 for second, etc.
			public bool IsExit { get { return Index == -1; } }
			public Node DefaultNode;           /// @{ alt = \(Index+1); } or @{ break; }
			public InternalList<Node> Nodes;   /// Copies of DefaultNode that were inserted in the prediction tree
			public bool IncludeNext;           /// Used by ComputeNextSets
			public override string ToString()  /// for debugging
			{
				return string.Format("Handler {0}: {1} * {2}", Index, DefaultNode.Print(), Nodes.Count);
			}
		}

		// The int in each pair is the alt number: 0..Arms.Count and Arms.Count for exit
		protected Pair<KthSet,AltHandlers>[] ComputeFirstSets(Alts alts, AltHandlers[] handlers)
		{
			bool hasExit = alts.Mode != LoopMode.None;
			var firstSets = new Pair<KthSet,AltHandlers>[alts.ArmCountPlusExit];

			int i;
			for (i = 0; i < alts.Arms.Count; i++)
				firstSets[i] = G.Pair(ComputeKthSet(new KthSet(alts.Arms[i]), false), handlers[i]);
			if (hasExit)
				firstSets[i] = G.Pair(ComputeKthSet(new KthSet(alts.Next), true), handlers[i]);
			if ((uint)alts.DefaultArm < (uint)alts.Arms.Count)
				InternalList.Move(firstSets, alts.DefaultArm, firstSets.Length-1);
			return firstSets;
		}
		protected Pair<KthSet,AltHandlers>[] ComputeNextSets(Pair<KthSet,AltHandlers>[] previous, IPGTerminalSet context)
		{
			int count = 0;
			for (int i = 0; i < previous.Length; i++) {
				if (previous[i].B.IncludeNext = context.Overlaps(previous[i].A.Set))
					count++;
			}
			
			var next = new Pair<KthSet,AltHandlers>[count];
			for (int i = 0, j = 0; i < previous.Length; i++)
			{
				if (previous[i].B.IncludeNext)
					next[j++] = G.Pair(ComputeKthSet(previous[i].A, previous[i].B.IsExit), previous[i].B);
			}
			return next;
		}
		//private Pair<KthSet, int>[] ComputeNextSets(Pair<KthSet, int>[] previous)
		//{
		//    var nextSets = new Pair<KthSet,int>[previous.Length];
		//    for (int i = 0; i < previous.Length; i++)
		//        nextSets[i] = G.Pair(ComputeKthSet(previous[i].A), previous[i].B);
		//    return nextSets;
		//}

		/// <summary>Represents a location in a grammar: a predicate and a 
		/// "return stack" which is a singly-linked list. This type is used 
		/// within <see cref="Transition"/>.</summary>
		protected class GrammarPos
		{
			public GrammarPos(Pred pred, GrammarPos @return = null)
			{
				Debug.Assert(pred != null); 
				Pred = pred; 
				Return = @return;
			}
			public readonly Pred Pred;
			public readonly GrammarPos Return;

			public override string ToString() // for debugging
			{
				if (Return != null)
					return string.Format("{0} (=> {1})", Pred, Return);
				return Pred.ToString();
			}
		}
			
		/// <summary>Represents a position in a grammar (<see cref="GrammarPos"/>) 
		/// plus the set of characters that leads to that position from the previous 
		/// position. This is a single case in a <see cref="KthSet"/>.</summary>
		/// <remarks>
		/// For example, suppose the grammar is
		/// <code>
		///		rule X ==> #[ 'a' Y 'z' ];
		///		rule Y ==> #[ 'a'..'y' 'b'..'z' ];
		/// </code>
		/// If the previous position is represented by the dot in <c>'a'.Y 'z'</c>,
		/// i.e. before Y, then <see cref="ComputeKthSet"/> will compute a Transition
		/// with Set=[a-y] and Position pointing to <c>.'b'..'z'</c>, with a return 
		/// stack that points to <c>'a' Y.'z'</c>
		/// </remarks>
		protected struct Transition
		{
			public Transition(IPGTerminalSet set, GrammarPos position) {
				Debug.Assert(position != null);
				Set = set; 
				Position = position;
			}
			public IPGTerminalSet Set;
			public GrammarPos Position;

			public override string ToString() // for debugging
			{
				return string.Format("{0} => {1}", Set, Position);
			}
		}

		/// <summary>Represents the possible interpretations of a single input 
		/// character, in terms of transitions in the grammar.</summary>
		/// <remarks>
		/// For example, suppose the grammar is
		/// <code>
		///     rule @for ==> #[ #for ($id #in $collection | $id $`=` range) ]
		///     rule range ==> #[ start $'..' stop ]
		/// </code>
		/// If the starting position is right after #for, then <see cref="ComputeKthSet"/>
		/// will generate two cases, one at <c>$id.#in $collection</c> and 
		/// another at <c>$id.$`=` stop</c>. In both cases, the Set is $id, so
		/// <see cref="KthSet.Set"/> will also be $id.
		/// </remarks>
		protected class KthSet
		{
			public KthSet() { }
			public KthSet(Pred start) { Cases.Add(new Transition(null, new GrammarPos(start))); }
			public int LA = -1;
			public List<Transition> Cases = new List<Transition>();
			public IPGTerminalSet Set;
				
			public void UpdateSet(bool addEOF)
			{
				if (Cases.Count == 0)
					Set = TrivialTerminalSet.Empty();
				else {
					Set = Cases[0].Set;
					for (int i = 1; i < Cases.Count; i++)
						Set = Set.Union(Cases[i].Set);
				}
				if (addEOF) {
					Set = Set.Clone(); // bug fix, avoid changing Cases[0].Set
					Set.ContainsEOF = true;
				}
			}
			public override string ToString() // for debugging
			{
				return string.Format("la{0} = {1} ({2})", LA, Set.ToString(), Cases.Select(c => c.Set).Join("|"));
			}
		}
			
		protected KthSet ComputeKthSet(KthSet previous, bool addEOF)
		{
			var next = new KthSet() { LA = previous.LA + 1 };
			for (int i = 0; i < previous.Cases.Count; i++)
				_computeNext.Do(next, previous.Cases[i].Position);
			MakeCanonical(next);
			ConsolidateDuplicatePositions(next);
			next.UpdateSet(addEOF);
			return next;
		}

		protected void MakeCanonical(KthSet next)
		{
			var cases = next.Cases;
			for (int i = 0; i < cases.Count; i++)
				cases[i] = new Transition(cases[i].Set, _getCanonical.Do(cases[i].Position));
		}

		protected ComputeNext _computeNext = new ComputeNext();
		protected GetCanonical _getCanonical = new GetCanonical();
		
		/// <summary>Computes the "canonical" interpretation of a position for
		/// prediction purposes, so that <see cref="ConsolidateDuplicatePositions"/> 
		/// can detect duplicates reliably. Call <see cref="Do"/>() to use.</summary>
		protected class GetCanonical : PredVisitor
		{
			/// <summary>Computes the "canonical" interpretation of a position.</summary>
			/// <remarks>
			/// For example, given
			/// <code>
			///		rule X ==> #[ 'a' Y 'z' ];
			///		rule Y ==> #[ 'a'..'y' 'b'..'z' ];
			/// </code>
			/// The position before the sequence <c>'a' Y 'z'</c> is equivalent to 
			/// the position before 'a', so the result points to 'a' rather than to
			/// the sequence itself.
			/// <para/>
			/// The position after 'b'..'z' is equivalent to the position before 'z',
			/// if Y was called from X. Therefore, given the position after 'b'..'z'
			/// (a pointer to <see cref="EndOfRule"/>), and return address before 'z',
			/// this method returns the position before 'z'.
			/// </remarks>
			public GrammarPos Do(GrammarPos input)
			{
				_result = input.Pred;
				_return = input.Return;
				Visit(input.Pred);
				if (_result != input.Pred)
					return new GrammarPos(_result, _return);
				else
					return input;
			}
			Pred _result;
			protected GrammarPos _return; // may be null

			public override void Visit(Seq seq)
			{
				Visit(seq.List[0]); // btw, empty sequences should not happen
			}
			public override void Visit(Gate gate)
			{
				Visit(gate.Predictor);
			}
			public override void Visit(EndOfRule end)
			{
				if (_return != null) {
					var returnTo = _return.Pred;
					_return = _return.Return; // Return!
					Visit(returnTo);
				}
			}
			public override void VisitOther(Pred pred)
			{
				_result = pred;
			}
		}

		/// <summary>Gathers a list of all one-token transitions starting from a 
		/// single position.</summary>
		/// <remarks>
		/// For example, given
		/// <code>
		///		rule X ==> #[ 'x' Y '0'..'9' 'x' ];
		///		rule Y ==> #[.('y'? | Z) ];
		///		rule Z ==> #[ ('z' | '0'..'9' '0'..'9'*) ];
		/// </code>
		/// If the dot (.) represents the current position, then this class 
		/// computes the possible <see cref="Transition"/>s, which are as follows:
		/// <code>
		///     Transition.Set   Transition.Position
		///     'y'              rule Y ==> #[ ('y'? | Z).];                 (EndOfRule)
		///     '0'..'9'         rule X ==> #[ 'x' Y '0'..'9'.'x' ];         (TerminalPred)
		///     'z'              rule Z ==> #[ ('z' | '0'..'9' '0'..'9'*).]; (EndOfRule)
		///     '0'..'9'         rule Z ==> #[ ('z' | '0'..'9'.'0'..'9'*) ]; (Alts)
		/// </code>
		/// This class is derived from GetCanonical just to inherit some code from it.
		/// </remarks>
		protected class ComputeNext : GetCanonical
		{
			public void Do(KthSet result, GrammarPos position)
			{
				_result = result;
				_return = position.Return;
				Visit(position.Pred);
			}
			KthSet _result;

			public override void Visit(TerminalPred term)
			{
				_result.Cases.Add(new Transition(term.Set, new GrammarPos(term.Next, _return)));
			}
			public override void Visit(RuleRef rref)
			{
				var old = _return;
				_return = new GrammarPos(rref.Next, _return);
				Visit(rref.Rule.Pred);
				_return = old;
			}
			public override void Visit(Alts alts)
			{
				foreach (var pred in alts.Arms)
					Visit(pred);
				if (alts.HasExit)
					Visit(alts.Next);
			}
			public override void Visit(AndPred and)
			{
				Visit(and.Next); // skip
			}
			public override void Visit(EndOfRule end)
			{
				if (_return != null) {
					var returnTo = _return.Pred;
					_return = _return.Return; // Return!
					Visit(returnTo);
				} else {
					// Nowhere to return to? Use the follow set of the rule.
					foreach (var pred in end.FollowSet)
						Visit(pred);
				}
			}
		}

		static void ConsolidateDuplicatePositions(KthSet set)
		{
			Trace.WriteLine("TODO: ConsolidateDuplicatePositions");
		}

		#endregion
	}

	public class Rule
	{
		public readonly Node Basis;
		public readonly EndOfRule EndOfRule = new EndOfRule();

		public Rule(Node basis, Symbol name, Pred pred, bool isStartingRule) 
		{
			Basis = basis; Pred = pred; Name = name;
			if (IsStartingRule = isStartingRule) {
				var eof = new TerminalPred(null, -1);
				eof.Next = eof; // everything needs a "Next"
				EndOfRule.FollowSet.Add(eof);
			}
		}
		public readonly Symbol Name;
		public readonly Pred Pred;
		public bool IsToken, IsStartingRule;
		public int K; // max lookahead; <= 0 to use default

		public static Alts operator |(Rule a, Pred b) { return (Alts)((RuleRef)a | b); }
		public static Alts operator |(Pred a, Rule b) { return (Alts)(a | (RuleRef)b); }
		public static Alts operator |(Rule a, Rule b) { return (Alts)((RuleRef)a | (RuleRef)b); }
		public static implicit operator Rule(RuleRef rref) { return rref.Rule; }
		public static implicit operator RuleRef(Rule rule) { return new RuleRef(null, rule); }
	}

	/*
	 * token Id() {
	 *   (('@', {_verbatim=1;})`?`, NormalIdStart, NormalIdCont`*`) |
	 *   ('@', {_verbatim=1;}, SqString);
	 * }
	 * rule IdStart() { Letter | '_'; }
	 * rule IdCont()  { IdStart | '0'..'9'; }
	 * rule Letter()  { ('a'..'z') | ('A'..'Z') | (&{Char.IsLetter(LA0)}, _); }
	 * 
	 * 
	 * 
	 * rule goo() => { foo 'z' }
	 * rule foo() => { nongreedy(('a' | 'b') num | 'c')* ('c' | 'd') | 'a'+; }
	 * rule num() => { ('0'..'9')+; }
	 * 
	 * static readonly InputSet num__set0 = Range('0','9');
	 * void foo()
	 * {
	 *   int alt;
	 *   char LA0, LA1;
	 *   LA0 = LA(0);
	 *   if (LA0 == 'a') {
	 *     LA1 = LA(1);
	 *     if (LA1 >= '0' && LA1 <= '9')
	 *       alt = 0;
	 *     else
	 *       alt = 1;
	 *   } else
	 *     alt = 1;
	 *   
	 *   if (alt == 0) {
	 *     for (;;) { // for `?` use do...while(false)
	 *       LA0 = LA(0);
	 *       if (LA0 >= 'a' && LA0 <= 'b')
	 *         alt = 0;
	 *       else if (LA0 == 'c') {
	 *         if (LA0 == 'z')
	 *           break;
	 *         else
	 *           alt = 1;
	 *       } else
	 *         break;
	 *       
	 *       if (alt == 0) {
	 *         Consume(); // alt == 0 already implies 'a'..'b'; no check needed
	 *         num();
	 *       }
	 *     }
	 *   } else {
	 *     Consume();
	 *   }
	 * }
	 * void num()
	 * {
	 *   int alt;
	 *   char LA0;
	 *   LA0 = LA(0);
	 *   Match(LA0 >= '0' && LA0 <= '9', "0..9");
	 *   for (;;) {
	 *     LA0 = LA(0);
	 *     if (LA0 >= '0' && LA0 <= '9')
	 *       alt = 0;
	 *     else
	 *       alt = -1;
	 *     if (alt == 0)
	 *       Consume();
	 *     else
	 *       break;
	 *   }
	 * }
	 * 
	 * 
	 * Fun time! So hey, what would this less-ambiguous C alternative look like? D?
	 * - Juxtaposition operator is not possible in general because x - y, x `@` y
	 *   would be ambiguous: could be (x) (-y), (x `@`) (y)
	 * - In boo style, can allow arbitrary macro names without parens e.g. 
	 *       boo.foo (bar) - 1: ... 
	 *   Or if braces normally start child blocks:
	 *       boo.foo (bar) - 1 { ... }
	 *   In that case, need something else like {{ }} to make a scope mid-statement.
	 *   presence of ':' indicates that 'boo.foo' must be a macro name;
	 *   "assert (x) > (y)" can't work this way, but "assert: (x) > (y)" can.
	 *   Labels would need some other syntax such as 
	 * 
	 * Distinguishing argument lists from expressions: arg list when
	 * 1. beginning of statement, multiple words before parens, and/or
	 * 2. parens followed by =>
	 * a b c d(e < f, g > h, j < k, l < m >> m) => { ... }
	 * Note: constructors and destructors may look like exprs; assume fn call
	 *       and disambiguate in postprocessing step.
	 * Note: var decl in expression requires '=' to disambiguate, either right
	 *       after the type and name (int x = ...) or outside the parenthesis 
	 *       (x, int y, a[i]) = ...
	 */

}
