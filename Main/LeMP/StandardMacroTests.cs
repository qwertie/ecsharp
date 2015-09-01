using Ecs.Parser;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP
{
	[TestFixture]
	public class StandardMacroTests
	{
		IMessageSink _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		[Test]
		public void TestNullDot()
		{
			TestBoth(@"a = b.c?.d;", @"a = b.c?.d;",
			          "a = b.c != null ? b.c.d : null;");
			TestEcs(@"a = b.c?.d.e;",
			         "a = b.c != null ? b.c.d.e : null;");
			TestBoth(@"a = b?.c[d];", @"a = b?.c[d];",
			          "a = b != null ? b.c[d] : null;");
			TestEcs(@"a = b?.c?.d();",
			         "a = b != null ? b.c != null ? b.c.d() : null : null;");
			TestBoth(@"return a.b?.c().d!x;", @"return a.b?.c().d<x>;",
			          "return a.b != null ? a.b.c().d<x> : null;");
		}

		[Test]
		public void TestTuples()
		{
			TestEcs("use_default_tuple_types();", "");
			TestBoth("(1; a) + (2; a; b) + (3; a; b; c);",
			         "(1, a) + (2, a, b) + (3, a, b, c);",
			         "Tuple.Create(1, a) + Tuple.Create(2, a, b) + Tuple.Create(3, a, b, c);");
			TestBoth("x::#!(String; DateTime) = (\"\"; DateTime.Now); y::#!(Y) = (new Y(););",
			         "#<String, DateTime> x = (\"\", DateTime.Now);     #<Y> y = (new Y(),);",
			         "Tuple<String, DateTime> x = Tuple.Create(\"\", DateTime.Now); Tuple<Y> y = Tuple.Create(new Y());");
			TestEcs("set_tuple_type(Sum, Sum); a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Sum(1) + Sum(1, 2) + Sum(1, 2, 3, 4, 5);");
			TestEcs("set_tuple_type(Tuple); set_tuple_type(2, Pair, Pair.Create);"+
			        "a = (1,) + (1, 2) + (1, 2, 3, 4, 5);",
			        "a = Tuple.Create(1) + Pair.Create(1, 2) + Tuple.Create(1, 2, 3, 4, 5);");
			
			TestBoth("(a; b; c) = foo;", "(a, b, c) = foo;",
			        "a = foo.Item1; b = foo.Item2; c = foo.Item3;");
			TestEcs("(var a, var b, c) = foo;",
			        "var a = foo.Item1; var b = foo.Item2; c = foo.Item3;");
			int n = StandardMacros.NextTempCounter;
			TestEcs("(a, b.c.d) = Foo;",
			        "var tmp_"+n+" = Foo; a = tmp_"+n+".Item1; b.c.d = tmp_"+n+".Item2;");
			n = StandardMacros.NextTempCounter;
			TestEcs("(a, b, c, d) = X.Y();",
			        "var tmp_"+n+" = X.Y(); a = tmp_"+n+".Item1; b = tmp_"+n+".Item2; c = tmp_"+n+".Item3; d = tmp_"+n+".Item4;");
		}

		[Test]
		public void WithTest()
		{
			int n = StandardMacros.NextTempCounter;
			TestEcs("with (foo) { .bar = .baz(); }",
			        "{ var tmp_"+n+" = foo; tmp_"+n+".bar = tmp_"+n+".baz(); }");
			n = StandardMacros.NextTempCounter;
			TestEcs(@"with (jekyll) { 
						.A = 1; 
						with(mr.hyde()) { x = .F(x); }
						with(.B + .C(.D));
					}", string.Format(@"{{
						var tmp_{0} = jekyll;
						tmp_{0}.A = 1;
						{{
							var tmp_{1} = mr.hyde();
							x = tmp_{1}.F(x);
						}}
						with(tmp_{0}.B + tmp_{0}.C(tmp_{0}.D));
					}}", n + 1, n));
		}

		[Test]
		public void TestCodeQuote()
		{
			TestEcs("quote { F(); }",
				   @"LNode.Call((Symbol)""F"");");
			TestEcs("quote(F(x, 0));",
				   @"LNode.Call((Symbol)""F"", LNode.List(LNode.Id((Symbol) ""x""), LNode.Literal(0)));");
			TestEcs("quote { x = x + 1; }",
				   @"LNode.Call(CodeSymbols.Assign, LNode.List(LNode.Id((Symbol) ""x""), LNode.Call(CodeSymbols.Add, LNode.List(LNode.Id((Symbol) ""x""), LNode.Literal(1))).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.Operator);");
			TestEcs("quote { Console.WriteLine(\"Hello\"); }",
				   @"LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) ""Console""), LNode.Id((Symbol) ""WriteLine""))), LNode.List(LNode.Literal(""Hello"")));");
			TestEcs("q = quote({ while (Foo<T>) Yay(); });",
				   @"q = LNode.Call(CodeSymbols.While, LNode.List(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Id((Symbol) ""Foo""), LNode.Id((Symbol) ""T""))), LNode.Call((Symbol) ""Yay"")));");
			TestEcs("q = quote({ if (true) { Yay(); } });",
				   @"q = LNode.Call(CodeSymbols.If, LNode.List(LNode.Literal(true), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call((Symbol) ""Yay""))).SetStyle(NodeStyle.Statement)));");
			TestEcs("q = quote({ Yay(); break; });",
				   @"q = LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call((Symbol) ""Yay""), LNode.Call(CodeSymbols.Break))).SetStyle(NodeStyle.Statement);");
			TestEcs("q = quote({ $(dict[key]) = 1; });",
				   @"q = LNode.Call(CodeSymbols.Assign, LNode.List(dict[key], LNode.Literal(1))).SetStyle(NodeStyle.Operator);");
			TestEcs("q = quote(hello + $x);",
				   @"q = LNode.Call(CodeSymbols.Add, LNode.List(LNode.Id((Symbol) ""hello""), x)).SetStyle(NodeStyle.Operator);");
			TestEcs("quote { (x); }",
				   @"LNode.Id(LNode.List(LNode.InParensTrivia), (Symbol) ""x"");");
			TestEcs("rawQuote { Func($Foo); }",
					@"LNode.Call((Symbol) ""Func"", LNode.List(LNode.Call(CodeSymbols.Substitute, LNode.List(LNode.Id((Symbol) ""Foo"")))));");
			TestEcs("quote(Foo($first, $(..rest)));",
				   @"LNode.Call((Symbol) ""Foo"", new RVList<LNode>().Add(first).AddRange(rest));");
			TestEcs("quote(Foo($(..args)));",
				   @"LNode.Call((Symbol) ""Foo"", new RVList<LNode>(args));");
			TestEcs("quote { [$(..attrs)] public X; }",
				   @"LNode.Id(new RVList<LNode>().AddRange(attrs).Add(LNode.Id(CodeSymbols.Public)), (Symbol)""X"");");
		}

		[Test]
		public void TestUseSymbols()
		{
			TestLes("@[Attr] use_symbols; @@foo;",
				@"@[Attr, #static, #readonly] #var(Symbol, sy_foo = #cast(""foo"", Symbol)); sy_foo;");
			TestEcs("[Attr] use_symbols; Symbol status = @@OK;",
				@"[Attr] static readonly Symbol sy_OK = (Symbol) ""OK""; Symbol status = sy_OK;");
			TestEcs("[Attr] use_symbols(prefix(_), inherit(@@OK)); Symbol status = @@OK;",
				@"Symbol status = _OK;");
			TestEcs(@"public use_symbols(prefix: S_, inherit: (@@Good, @@Bad)); 
				Symbol status = @@OK ?? @@Good;
				Symbol Err() { return @@Bad ?? @@Error; }",
				@"public static readonly Symbol S_OK = (Symbol) ""OK"", S_Error = (Symbol) ""Error"";
				Symbol status = S_OK ?? S_Good;
				Symbol Err() { return S_Bad ?? S_Error; }");
			TestLes("@[Attr] use_symbols; @@`->`;",
				@"@[Attr, #static, #readonly] #var(Symbol, sy__dash_gt = #cast(""->"", Symbol)); sy__dash_gt;");
		}

		[Test]
		public void TestMatchCode()
		{
			TestLes(@"matchCode var { 777 => Yay(); 666 => Boo(); $_ => @Huh?(); }",
				@"#if(777.Equals(var.Value), Yay(), 
					#if(666.Equals(var.Value), Boo(), @Huh?()));");
			TestEcs(@"matchCode(code) {
					case { '2' + 2; }: Weird();
				}", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && 2.Equals(code.Args[1].Value))
						Weird();");
			int n = StandardMacros.NextTempCounter;
			TestEcs(@"matchCode(Get.Var) {
					case Do($stuff): Do(stuff);
				}", @"{
						var tmp_"+n+@" = Get.Var;
						LNode stuff;
						if (tmp_"+n+@".Calls((Symbol) ""Do"", 1) && (stuff = tmp_5.Args[0]) != null)
							Do(stuff);
					}");
			TestEcs(@"matchCode(code) { 
					$(lit(#.IsLiteral)) => Literal(); 
					$(id[#.IsId]) => Id(); 
					$_ => Call();
				}",
				@"{
					LNode id, lit;
					if ((lit = code) != null && lit.IsLiteral)
						Literal();
					else if ((id = code) != null && id.IsId)
						Id();
					else
						Call();
				}");
			TestEcs(@"matchCode(code) { 
					case foo, FOO:  Foo(); 
					case 1, 1.0:    One(); 
					case $whatever: Whatever();
				}", @"{
					LNode whatever;
					if (code.IsIdNamed((Symbol) ""foo"") || code.IsIdNamed((Symbol) ""FOO""))
						Foo();
					else if (1.Equals(code.Value) || 1d.Equals(code.Value))
						One();
					else if ((whatever = code) != null)
						Whatever();
				}");
			TestEcs(@"matchCode(code) { 
					case 1, one: One();
				}", @"
					if (1.Equals(code.Value) || code.IsIdNamed((Symbol) ""one""))
						One();
				");
			TestEcs(@"matchCode(code) { 
					case $binOp($_, $_): BinOp(binOp);
				}", @"{
					LNode binOp;
					if (code.Args.Count == 2 && (binOp = code.Target) != null)
						BinOp(binOp);
				}");
			TestEcs(@"matchCode(code) { 
					case ($a, $b, $(..args), $c):                Three(a, b, c);
					case (null, $(..args)), ($first, $(..args)): Tuple(first, args);
					case ($(..args), $last):                     Unreachable();
					case ($(..args),):                           Tuple(args);
				}", @"{
					LNode a, b, c, first = null, last;
					RVList<LNode> args;
					if (code.CallsMin(CodeSymbols.Tuple, 3) && (a = code.Args[0]) != null && (b = code.Args[1]) != null && (c = code.Args[code.Args.Count - 1]) != null) {
						args = new RVList<LNode>(code.Args.Slice(2, code.Args.Count - 3));
						Three(a, b, c);
					} else if (code.CallsMin(CodeSymbols.Tuple, 1) && code.Args[0].Value == null && (args = new RVList<LNode>(code.Args.Slice(1))).IsEmpty | true 
						|| code.CallsMin(CodeSymbols.Tuple, 1) && (first = code.Args[0]) != null && (args = new RVList<LNode>(code.Args.Slice(1))).IsEmpty | true)
						Tuple(first, args);
					else if (code.CallsMin(CodeSymbols.Tuple, 1) && (last = code.Args[code.Args.Count - 1]) != null) {
						args = code.Args.WithoutLast(1);
						Unreachable();
					} else if (code.Calls(CodeSymbols.Tuple)) {
						args = code.Args;
						Tuple(args);
					}
				}");
			n = StandardMacros.NextTempCounter;
			TestEcs(@"matchCode(code) { 
					case $x = $y:
						Assign(x, y);
					case [$(..attrs)] $x = $(x_(#.Equals(x))) + 1, 
					     [$(..attrs)] $op($x, $y):
						Handle(attrs);
						Handle(x, y);
					default:
						Other();
				}", @"{
					LNode op          = null, tmp_"+n+@" = null, x, x_ = null, y = null;
					RVList<LNode> attrs;
					if (code.Calls(CodeSymbols.Assign, 2) && (x = code.Args[0]) != null && (y = code.Args[1]) != null)
						Assign(x, y);
					else if ( 
						(attrs = code.Attrs).IsEmpty | true && code.Calls(CodeSymbols.Assign, 2) && 
						(x = code.Args[0]) != null && (tmp_"+n+@" = code.Args[1]) != null && tmp_"+n+@".Calls(CodeSymbols.Add, 2) && 
						(x_ = tmp_"+n+@".Args[0]) != null && x_.Equals(x) && 1.Equals(tmp_"+n+ @".Args[1].Value) || 
						(attrs = code.Attrs).IsEmpty | true && code.Args.Count == 2 && (op = code.Target) != null && 
						(x = code.Args[0]) != null && (y = code.Args[1]) != null) {
						Handle(attrs);
						Handle(x, y);
					} else
						Other();
				}");
			// Ideally this generated code would use a tmp_n variable, but I'll accept the current output
			n = StandardMacros.NextTempCounter;
			TestEcs(@"
				matchCode(classDecl) {
				case {
						[$(..attrs)] 
						class $typeName : $(..baseTypes) { $(..body); }
					}:
						Handler();
				}", @"{
					LNode typeName;
					RVList<LNode> attrs, baseTypes, body;
					if ((attrs = classDecl.Attrs).IsEmpty | true && classDecl.Calls(CodeSymbols.Class, 3) && 
						(typeName = classDecl.Args[0]) != null && classDecl.Args[1].Calls(CodeSymbols.AltList) && 
						classDecl.Args[2].Calls(CodeSymbols.Braces))
					{
						baseTypes = classDecl.Args[1].Args;
						body = classDecl.Args[2].Args;
						Handler();
					}
				}");
			TestEcs(@"matchCode(code) {
					case { '2' + $(ref rhs); }: Weird();
				}", @"
					if (code.Calls(CodeSymbols.Add, 2) && '2'.Equals(code.Args[0].Value) && (rhs = code.Args[1]) != null)
						Weird();");
			TestEcs(@"matchCode (code) {
					case $_<$(.._)>:
					default: Nope();
				}", @"
					if (code.CallsMin(CodeSymbols.Of, 1)) { } 
					else Nope();
				");
		}

		[Test]
		public void TestAlgebraicDataTypeDecls()
		{
			TestEcs(@"
				public alt class SExpr {
					public alt Atom(object Value);
					public alt List(params object[] Items);
				}", @"
					public class SExpr
					{
						public SExpr() {}
					}
					public class Atom : SExpr
					{
						public Atom(object Value) { this.Value = Value; }
						
						public object Value { get; private set; }
						public Atom WithValue(object newValue) { return new Atom(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public object Item1 { get { return Value; } }
					}
					public class List : SExpr
					{
						public List(params object[] Items) { this.Items = Items; }

						public object[] Items { get; private set; }
						public List WithItems(params object[] newValue) { return new List(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public object[] Item1 { get { return Items; } }
					}");
			TestEcs(@"
				public alt class Opt<T> {
					public alt Have(T Value);
				}", @"
					public class Opt<T> {
						public Opt() { }
					}
					public class Have<T> : Opt<T>
					{
						public Have(T Value) { this.Value = Value; }
						
						public T Value { get; private set; }
						public Have<T> WithValue(T newValue) { return new Have<T>(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public T Item1 { get { return Value; } }
					}
					public static partial class Have
					{
						public static Have<T> New<T>(T Value) { return new Have<T>(Value); }
					}");
			TestEcs(@"
				[A] public abstract alt class BinaryTree<T> : BaseClass {
					[N] alt Node(BinaryTree<T> Left, BinaryTree<T> Right);
					[L] alt Leaf<T>(T Value) { stuff; }
					common_stuff;
				}
			", @"
				[A] public abstract class BinaryTree<T> : BaseClass
				{ 
					public BinaryTree() { }
					common_stuff;
				}
				[N] class Node<T> : BinaryTree<T>
				{
					public Node(BinaryTree<T> Left, BinaryTree<T> Right) { this.Left = Left; this.Right = Right; }
					
					public BinaryTree<T> Left { get; private set; }
					public BinaryTree<T> Right { get; private set; }
					public Node<T> WithLeft(BinaryTree<T> newValue) { return new Node<T>(newValue, Right); }
					public Node<T> WithRight(BinaryTree<T> newValue) { return new Node<T>(Left, newValue); }
					[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
					public BinaryTree<T> Item1 { get { return Left; } }
					[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
					public BinaryTree<T> Item2 { get { return Right; } }
				}
				[N] static partial class Node
				{
					public static Node<T> New<T>(BinaryTree<T> Left, BinaryTree<T> Right)
						{ return new Node<T>(Left, Right); }
				}
				[L] class Leaf<T> : BinaryTree<T>
				{
					public Leaf(T Value) { this.Value = Value; }
					
					public T Value { get; private set; }
					public Leaf<T> WithValue(T newValue) { return new Leaf<T>(newValue); }
					[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
					public T Item1 { get { return Value; } }
					stuff;
				}
				[L] static partial class Leaf
				{
					public static Leaf<T> New<T>(T Value) 
						{ return new Leaf<T>(Value); }
				}");
		}

		[Test]
		public void TestAlgebraicDataTypesAdvanced()
		{
			TestEcs(@"
				public abstract alt class LNode {
					public alt this(LNode[] Attributes);
					public alt LId(Symbol Name);
					public alt LLiteral(object Value);
					public alt LCall(LNode Target, params LNode[] Args);
				}", @"
					public abstract class LNode
					{
						public LNode(LNode[] Attributes) { this.Attributes = Attributes; }

						public LNode[] Attributes { get; private set; }
						public abstract LNode WithAttributes(LNode[] newValue);
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public LNode[] Item1 { get { return Attributes; } }
					}
					public class LId : LNode
					{
						public LId(LNode[] Attributes, Symbol Name) : base(Attributes) { this.Name = Name; }
						
						public Symbol Name { get; private set; }
						public override LNode WithAttributes(LNode[] newValue) { return new LId(newValue, Name); }
						// cov_With* functions (workaround for C#'s lack of covariant 
						// return types) are not currently implemented (and comments in test strings are ignored)
						//{ return cov_WithAttributes(newValue); }
						//public virtual LId cov_WithAttributes(LNode[] newValue) { return new LId(newValue, Name); }
						public LId WithName(Symbol newValue) { return new LId(Attributes, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public Symbol Item2 { get { return Name; } }
					}
					public class LLiteral : LNode
					{
						public LLiteral(LNode[] Attributes, object Value) : base(Attributes) { this.Value = Value; }

						public object Value { get; private set; }
						public override LNode WithAttributes(LNode[] newValue) { return new LLiteral(newValue, Value); }
						//{ return cov_WithAttributes(newValue); }
						//public virtual LId cov_WithAttributes(LNode[] newValue) { return new LLiteral(newValue, Value); }
						public LLiteral WithValue(object newValue) { return new LLiteral(Attributes, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public object Item2 { get { return Value; } }
					}
					public class LCall : LNode
					{
						public LCall(LNode[] Attributes, LNode Target, params LNode [] Args) : base(Attributes) { this.Target = Target; this.Args = Args; }

						public LNode Target { get; private set; }
						public LNode[] Args { get; private set; }
						public override LNode WithAttributes(LNode[] newValue) { return new LCall(newValue, Target, Args); }
						//{ return cov_WithAttributes(newValue); }
						//public virtual LCall cov_WithAttributes(LNode[] newValue) { return new LCall(newValue, Target, Args); }
						public LCall WithTarget(LNode newValue) { return new LCall(Attributes, newValue, Args); }
						public LCall WithArgs(params LNode[] newValue) { return new LCall(Attributes, Target, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public LNode Item2 { get { return Target; } }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public LNode[] Item3 { get { return Args; } }
					}");
			TestEcs(@"
				public alt class MyTuple<T1> {
					public alt this(T1 Item1);
					public alt MyTuple<T1,T2>(T2 Item2) {
						public alt MyTuple<T1,T2,T3>(T3 Item3) { }
					}
				}", @"
					public class MyTuple<T1> {
						public MyTuple(T1 Item1) { this.Item1 = Item1; }
						public T1 Item1 { get; private set; }
						public virtual MyTuple<T1> WithItem1(T1 newValue) { return new MyTuple<T1>(newValue); }
					}
					public static partial class MyTuple
					{
						public static MyTuple<T1> New<T1>(T1 Item1)
							{ return new MyTuple<T1>(Item1); }
					}
					public class MyTuple<T1, T2> : MyTuple<T1> {
						public MyTuple(T1 Item1, T2 Item2) : base(Item1) { this.Item2 = Item2; }
						public T2 Item2 { get; private set; }
						public override MyTuple<T1> WithItem1(T1 newValue) //{ return cov_WithItem1(newValue); }
						//public override MyTuple<T1, T2> cov_WithItem1(T1 newValue) 
						{ return new MyTuple<T1, T2>(newValue, Item2); }
						public virtual  MyTuple<T1, T2> WithItem2(T2 newValue) { return new MyTuple<T1, T2>(Item1, newValue); }
					}
					public static partial class MyTuple
					{
						public static MyTuple<T1, T2> New<T1, T2>(T1 Item1, T2 Item2)
							{ return new MyTuple<T1, T2>(Item1, Item2); }
					}
					public class MyTuple<T1, T2, T3> : MyTuple<T1, T2> {
						public MyTuple(T1 Item1, T2 Item2, T3 Item3) : base(Item1, Item2) { this.Item3 = Item3; }
						public T3 Item3 { get; private set; }
						public override MyTuple<T1> WithItem1(T1 newValue) //{ return cov_cov_WithItem1(newValue); }
						//public virtual  MyTuple<T1, T2, T3> cov_cov_WithItem1(T1 newValue) 
						{ return new MyTuple<T1, T2, T3>(newValue, Item2, Item3); }
						public override MyTuple<T1, T2> WithItem2(T2 newValue) //{ return cov_WithItem2(newValue); }
						//public virtual  MyTuple<T1, T2, T3> cov_WithItem2(T2 newValue) 
						{ return new MyTuple<T1, T2, T3>(Item1, newValue, Item3); }
						public MyTuple<T1, T2, T3> WithItem3(T3 newValue) { return new MyTuple<T1, T2, T3>(Item1, Item2, newValue); }
					}
					public static partial class MyTuple
					{
						public static MyTuple<T1, T2, T3> New<T1, T2, T3>(T1 Item1, T2 Item2, T3 Item3)
							{ return new MyTuple<T1, T2, T3>(Item1, Item2, Item3); }
					}
				");
			TestEcs(@"
				public alt class Base<T> {
					public alt this(T Toilet) { base_constructor_code; }
					public alt Derived<T,P>(P Paper) {
						public alt this() { constructor_code; }
					}
				}", @"
					public class Base<T> {
						public Base(T Toilet)
						{
							this.Toilet = Toilet;
							base_constructor_code;
						}

						public T Toilet { get; private set; }
						public virtual Base<T> WithToilet(T newValue) { return new Base<T>(newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
						public T Item1 { get { return Toilet; } }
					}
					public static partial class Base
					{
						public static Base<T> New<T>(T Toilet) { return new Base<T>(Toilet); }
					}
					public class Derived<T,P> : Base<T>
					{
						public Derived(T Toilet, P Paper) : base(Toilet)
						{
							this.Paper = Paper;
							constructor_code;
						}
						public P Paper { get; private set; }
						public override Base<T> WithToilet(T newValue) { return new Derived<T,P>(newValue, Paper); }
						public Derived<T,P> WithPaper(P newValue) { return new Derived<T,P>(Toilet, newValue); }
						[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)] 
						public P Item2 { get { return Paper; } }
					}
					public static partial class Derived
					{
						public static Derived<T,P> New<T,P>(T Toilet, P Paper) { return new Derived<T,P>(Toilet, Paper); }
					}");
		}

		[Test]
		public void TestStringInterpolation()
		{
			Assert.Fail("TODO");
		}

		[Test]
		public void SetOrCreateMemberTest()
		{
			var old = _sink;
			_sink = MessageSink.Trace;
			try {
				TestEcs("void Set(set int X);", "void Set(set int X);"); // Body required
			} finally { _sink = old; }
			TestEcs("void Set(set int X, set int Y) {}",
				"void Set(int x, int y) { X = x; Y = y; }");
			TestEcs("void Set(public int X, bool Y, private string Z) { if (Y) Rejoice(); }",
				@"public int X; private string Z; 
				void Set(int x, bool Y, string z) {
					X = x; Z = z; if (Y) Rejoice();
				}");
			TestEcs(
				@"void Set(
					[Spanish] set int _hola, 
					[English] static int _hello, 
					[Alzheimer's] partial long goodbye = 8, 
					[Hawaii] protected internal string Aloha = 5,
					[French] internal string _Bonjour = 7,
					[Other] readonly int _ciao = 4) { Foo(_ciao); }",
				@"
				[English] static int _hello;
				[Alzheimer's] partial long goodbye;
				[Hawaii] protected internal string Aloha;
				[French] internal string _Bonjour;
				void Set(
					[Spanish] int hola, 
					int hello, 
					long goodbye = 8, 
					string aloha = 5,
					string Bonjour = 7,
					[Other] readonly int _ciao = 4)
				{
					_hola = hola;
					_hello = hello;
					this.goodbye = goodbye;
					Aloha = aloha;
					_Bonjour = Bonjour;
					Foo(_ciao);
				}");
			TestEcs(@"class Point { 
				public Point(public int X, public int Y) {}
				public this(set int X, set int Y) {}
			}", @"class Point {
				public int X;
				public int Y;
				public Point(int x, int y) { X = x; Y = y; }
				public this(int x, int y) { X = x; Y = y; }
			}");
			TestEcs("void Set(public params string[] Strs) {}",
				"public string[] Strs; void Set(params string[] strs) { Strs = strs; }");
		}
		
		[Test]
		public void ResultTest()
		{
			TestEcs("static int Square(int x) { x*x }",
			        "static int Square(int x) { return x*x; }");
			TestEcs("static int Abs(int x) { if (x >= 0) x else -x }",
			        "static int Abs(int x) { if (x >= 0) return x; else return -x; }");
			TestEcs("static int Smallr(int x) { if (x > 100) { while(x > 100) x /= 2; x } else { x - 1 } }",
			        "static int Smallr(int x) { if (x > 100) { while(x > 100) x /= 2; return x; } else { return x - 1; } }");
			TestEcs("static bool ToBool(bool? b) { " +
					  "if (b == null) throw new InvalidCastException(); else if (b) true else false }",
					"static bool ToBool(bool? b) { " +
					  "if (b == null) throw new InvalidCastException(); else if (b) return true; else return false; }");
			TestEcs(@"static string Ordinal(int x) { switch(x) { 
						case 1: {""first""} case 2: {""second""} case 3: {""third""} 
						case 4: {""fourth""} case 5: {""fifth""} case 6: {""sixth""} 
						case 7: {""seventh""} case 8: {""eighth""} case 9: {""ninth""}
						default: {""(not supported)""} 
					} }",
				   @"static string Ordinal(int x) { switch(x) { 
						case 1: {return ""first"";} case 2: {return ""second"";} case 3: {return ""third"";} 
						case 4: {return ""fourth"";} case 5: {return ""fifth"";} case 6: {return ""sixth"";} 
						case 7: {return ""seventh"";} case 8: {return ""eighth"";} case 9: {return ""ninth"";}
						default: {return ""(not supported)"";} 
					} }");
		}
		
		[Test]
		public void ForwardedMethodTest()
		{
			TestEcs("static void Exit() ==> Application.Exit;",
			        "static void Exit() { Application.Exit(); }");
			TestEcs("static int InRange(int x, int lo, int hi) ==> MathEx.InRange;",
			        "static int InRange(int x, int lo, int hi) { return MathEx.InRange(x, lo, hi); }");
			TestEcs("void Append(string fmt, params string[] args) ==> sb.AppendFormat;",
			        "void Append(string fmt, params string[] args) { sb.AppendFormat(fmt, args); }");
			TestEcs("void AppendFormat(string fmt, params string[] args) ==> sb.#;",
					"void AppendFormat(string fmt, params string[] args) { sb.AppendFormat(fmt, args); }");
			TestEcs("internal int Count ==> _list.Count;",
					"internal int Count { get { return _list.Count; } set { _list.Count = value; } }");
			TestEcs("internal int Count ==> _list.#;",
					"internal int Count { get { return _list.Count; } set { _list.Count = value; } }");
			TestEcs("internal int Count { get ==> _list.#; set ==> _list.#; }",
					"internal int Count { get { return _list.Count; } set { _list.Count = value; } }");
		}

		[Test]
		public void BackingFieldTest()
		{
			TestEcs("[field _name] public string Name { get; }",
			        "string _name; public string Name { get { return _name; } }");
			TestEcs("[protected field _name] public string Name { get; protected set; }",
			        "protected string _name; public string Name { get { return _name; } protected set { _name = value; } }");
			TestEcs("[field] public string Name { get; }",
			        "string _name; public string Name { get { return _name; } }");
			TestEcs("[[A] field _lives = 3] [B] public int LivesLeft { internal get; set; }",
			        "[A] int _lives = 3; [B] public int LivesLeft { internal get { return _lives; } set { _lives = value; } }");
			TestEcs("[field] public string Name { get; set { _name = value; } }",
			        "string _name; public string Name { get { return _name; } set { _name = value; } }");
			TestEcs("[[A] field] [B, C] public string Name { get; }",
			        "[A] string _name; [B, C] public string Name { get { return _name; } }");
			TestEcs("public string Name { get; protected set; }",
			        "public string Name { get; protected set; }");
		}

		[Test]
		public void Test_on_finally()
		{
			TestEcs(@"{ Foo(); on_finally { Console.WriteLine(""Finally!""); } Bar(); }",
					@"{ Foo(); try { Bar(); } finally { Console.WriteLine(""Finally!""); } }");
			TestEcs(@"{ x++; on_finally { x--; Foo(); } DoSomeStuff(); Etc(); }",
					@"{ x++; try { DoSomeStuff(); Etc(); } finally { x--; Foo(); } }");
			// Alternate syntax from D
			TestEcs(@"scope(exit) { Leaving(); } while(_fun) KeepDoingIt();",
			        @"try { while(_fun) KeepDoingIt(); } finally { Leaving(); }");
		}
		[Test]
		public void Test_on_catch_on_throw()
		{
			TestEcs(@"{ bool ok = true; on_catch { ok = false; } DoSomeStuff(); Etc(); }",
					@"{ bool ok = true; try { DoSomeStuff(); Etc(); } catch { ok = false; } }");
			TestEcs(@"{ _crashed = false; on_catch { _crashed = true; } DoSomeStuff(); }",
					@"{ _crashed = false; try { DoSomeStuff(); } catch { _crashed = true; } }");
			TestEcs(@"{ on_catch(ex) { MessageBox.Show(ex.Message); } Etc(); }",
					@"{ try { Etc(); } catch(Exception ex) { MessageBox.Show(ex.Message); } }");
			TestEcs(@"on_throw(ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(Exception ex) { MessageBox.Show(ex.Message); throw; }");
			TestEcs(@"on_catch(FormatException ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(FormatException ex) { MessageBox.Show(ex.Message); }");
			TestEcs(@"on_throw(FormatException ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(FormatException ex) { MessageBox.Show(ex.Message); throw; }");
		}

		[Test]
		public void Test_on_return()
		{
			TestEcs(@"{ on_return(R) { Log(R); } Foo(); return Bar(); }",
			        @"{ Foo(); { var R = Bar(); Log(R); return R; } }");
			TestEcs(@"{ on_return(r) { r++; } Foo(); return x > 0 ? x : -x; }",
					@"{ Foo(); { var r = x > 0 ? x : -x; r++; return r; } }");
			TestEcs(@"{ on_return { Log(""return""); } if (true) return 5; else return; }",
			        @"{ if (true) { var __result__ = 5; Log(""return""); return __result__; } else { Log(""return""); return; } }");
			TestEcs(@"on_return(int r = 5) { r++; } return;",
			        @"{ int r = 5; r++; return r; }");
			TestEcs(@"on_return(int r) { r++; } return 5;",
			        @"{ int r = 5; r++; return r; }");
		}

		[Test]
		public void RequireTest()
		{
			// "[requires] is currently not supported for methods that do not have a body (e.g. interface methods)"
			// "[ensures] is currently not supported for methods that do not have a body (e.g. interface methods)"
			TestEcs("[requires(t != null)]"+
			       @"public void Wait(Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Contract.Requires<ArgumentException>(t != null, ""Wait() requires t != null""); t.Wait(); }");
			TestEcs("public void Wait([requires(# != null)] Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Contract.Requires(t != null, ""Wait() requires t != null""); t.Wait(); }");
			TestEcs("public void Wait([required] Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Contract.Requires(t != null, ""Wait() requires t != null""); t.Wait(); }");
			TestEcs("void Wait([requires(# != null)] Task<T> t) { t.Wait(); }",
			       @"void Wait(Task<T> t) { Contract.Requires(t != null, ""t != null""); t.Wait(); }");
			TestEcs("[assert(t != null)]"+
			       @"public void Wait(Task<T> t) { t.Wait(); }",
			       @"public void Wait(Task<T> t) { "+
			       @"  Debug.Assert(t != null, ""Wait() expects t != null""); t.Wait(); }");
			TestEcs(@"[assert(x >= min, ""x is too low"")]"+
			        @"[assert(x <= max, ""x is too high"")]"+
			        @"public void AssertRange(int x, int min, int max) { }",
			        @"public void AssertRange(int x, int min, int max) {"+
			        @"  Debug.Assert(x >= min, ""x is too low""); Debug.Assert(x <= max, ""x is too high""); }");
		}
		[Test]
		public void EnsuresTest()
		{
			// TODO: support ensures-on-throw
			// TODO: support async (Task Result) ensures
			TestEcs(@"[ensures(# >= 0)]
			          public static int Square(int x) { 
			            return x*x;
			          }",
			        @"public static int Square(int x) {
			            var @return = x*x; 
			            Contract.Assert(x >= 0, ""Square() failed to ensure (result >= 0)""); 
			            return @return;
			          }");
			TestEcs(@"public static Node Root { 
			            [ensures(# != null, ""Internal error"")] 
			            get { return _root; }
			          }",
			        @"public static Node Root { get {
			            var @return = _root; 
			            Contract.Assert(@return != null, ""Internal error"");
			            return @return;
			          } }");
			TestEcs(@"[ensures(File.Exists(filename), ""Gimme a friggin' break!""]
			          void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!"");
			          }",
			        @"void Save(string filename) { 
			            File.WriteAllText(filename, ""Saved!""); 
			            Contract.Assert(File.Exists(filename), ""Gimme a friggin' break!"");
			          }");
			TestEcs(@"[assert_ensures(comp(lo, hi) <= 0)]
				public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp) {
					if (comp(lo, hi) > 0) {
						Swap(ref lo, ref hi);
						return true;
					}
					return false;
				}", @"
				public static bool SortPair<T>(ref T lo, ref T hi, Comparison<T> comp) {
					if (comp(lo, hi) > 0) {
						Swap(ref lo, ref hi);
						Debug.Assert(comp(lo, hi) <= 0, ""SortPair() failed to ensure comp(lo, hi) <= 0"");
						return true;
					}
					Debug.Assert(comp(lo, hi) <= 0, ""SortPair() failed to ensure comp(lo, hi) <= 0"");
					return false;
				}");
		}

		[Test]
		public void MixingFeatures()
		{
			TestEcs("[requires(x >= 0)] static double Sqrt(double x) ==> Math.Sqrt;",
			       @"static double Sqrt(double x) { Contract.Requires(x >= 0, ""Sqrt() requires x >= 0""); return Math.Sqrt(x); }");
			TestEcs("[field _foo] public List<int> Foo { get; [required] set; }",
			       @"List<int> _foo; public List<int> Foo { "+
			       @"  get { return _foo; } "+
			       @"  set { Contract.Requires(value != null, ""Foo requires value != null""); _foo = value; }");
			TestEcs("[field _foo] public List<int> Foo { [ensures(# != null)] get; [requires(# != null)] set; }",
			       @"List<int> _foo; public List<int> Foo { "+
			       @"  get { var @return = _foo; Contract.Assert(); return @return; } "+
			       @"  set { Contract.Requires(value != null, ""Foo requires value != null""); _foo = value; }");
			string result = 
			    @"public static Node Root { get {
			        var @return = _root; 
			        Contract.Assert(@return != null, ""Internal error"");
			        return @return;
			      } }";
			TestEcs(@"
				public static Node Root { 
					[ensures(# != null, ""Internal error"")] 
					get { _root; }
				}", result);
			TestEcs(@"[field _root]
				public static Node Root { 
					[ensures(# != null, ""Internal error"")] 
					get;
				}",
				"Node _root; " + result);
		}

		[Test]
		public void TestNameOfAndStringify()
		{
			TestBoth(@"s = nameof(hello);",    @"s = nameof(hello);",     @"s = ""hello"";");
			TestBoth(@"s = stringify(hello);", @"s = stringify(hello);",  @"s = ""hello"";");
			TestBoth(@"s = nameof(A.B!C(D));", @"s = nameof(A.B<C>(D));", @"s = ""B"";");
			TestEcs (@"s = stringify(A.B<C>(D));",                        @"s = ""A.B<C>(D)"";");
		}

		[Test]
		public void TestConcat()
		{
			TestBoth(@"@1stprog = hello `##` world;", @"@1stprog = hello `##` world;", @"@1stprog = helloworld;");
			TestLes(@"##(call, ""_func_"", with(argument));", @"call_func_with(argument);");
		}

		[Test]
		public void TestTreeEqualsAndStaticIf()
		{
			TestEcs(@"bool b = a `tree==` 'A';",                        @"bool b = false;");
			TestEcs(@"bool b = f(x) `tree==` f(x /*comment*/);",        @"bool b = true;");
			TestEcs(@"a(); static if (true)  { b(); c(); } d();",       @"a(); b(); c(); d();");
			TestEcs(@"a(); static if (false) { b(); c(); } d();",       @"a(); d();");
			TestEcs(@"a(); static if (a `tree==` 'A') {{ b(); }} c();", @"a(); c();");
			TestEcs(@"a(); static if (foo() `tree==` foo(2)) b(); else {{ c(); }} d();", @"a(); { c(); } d();");
			TestEcs(@"a(); static if (foo.bar<baz> `tree==` foo.bar<baz>) b(); else c(); d();", @"a(); b(); d();");
			TestEcs(@"c = static_if(false, see, C);", @"c = C;");
		}

		[Test]
		public void TestUnroll()
		{
			TestLes(@"unroll X `in` (X; Y) { sum += X; }",
			          "sum += X; sum += Y;");
			TestBoth(@"unroll X `in` (X; Y) { sum += X; }",
			          "unroll (X in (X, Y)) { sum += X; }",
			          "sum += X; sum += Y;");
			TestEcs("unroll (X in (X, Y)) { sum += X; Console.WriteLine(sum); }",
			        "sum += X; Console.WriteLine(sum);"+
			        "sum += Y; Console.WriteLine(sum);");
			TestEcs("unroll (X in (A(), [Oof] B)) { X(X, [Foo] X); }",
			        "A()(A(), [Foo] A());"+
			        "([Oof] B)([Oof] B, [Foo, Oof] B);");
			TestEcs("unroll ((type, name) in ((int, x), (uint, y), (float, z))) { type name = 0; }",
			        "int x = 0; "+
			        "uint y = 0; "+
			        "float z = 0;");
			TestEcs("unroll (X in (int X = 41, X++, Console.WriteLine(X))) { X; }",
			        "int X = 41; X++; Console.WriteLine(X);");
		}

		[Test]
		public void TestReplace_basics()
		{
			// Simple cases
			TestLes(@"replace (nothing => nobody) {nowhere;}", "nowhere;");
			TestLes(@"replace (a => b) {a;}", "b;");
			TestLes(@"replace (7 => seven) {x = 7;}", "x = seven;");
			TestLes(@"replace (7() => ""seven"") {x = 7() + 7;}", @"x = ""seven"" + 7;");
			TestLes(@"replace (a => b) {@[Hello] a; a(a);}", "@[Hello] b; b(b);");
			TestLes("replace (MS => MessageSink; C => Current; W => Write; S => Severity; B => \"Bam!\")\n"+
			        "    { MS.C.W(S.Error, @null, B); }",
			        @"MessageSink.Current.Write(Severity.Error, @null, ""Bam!"");");
			TestLes(@"replace (Write => Store; Console.Write => Console.Write) "+
			        @"{ Write(x); Console.Write(x); }", "Store(x); Console.Write(x);");
			
			// Swap
			TestLes("replace (foo => bar; bar => foo) {foo() = bar;}", "bar() = foo;");
			TestLes("replace (a => 'a'; 'a' => A; A => a) {'a' = A - a;}", "A = a - 'a';");

			// Captures
			TestBoth("replace (input($capture) => output($capture)) { var i = 21; input(i * 2); };",
			         "replace (input($capture) => output($capture)) { var i = 21; input(i * 2); }",
			         "var i = 21; output(i * 2);");
		}

		[Test]
		public void TestReplace_params()
		{
			TestEcs("replace({ $(params before); on_exit { $(params command); } $(params after); } =>\n"+
			        "        { $before;          try  { $after; } finally { $command; }         }) \n"+
			        "{{ var foo = new Foo(); on_exit { foo.Dispose(); } Combobulate(foo); return foo; }}",
			        " { var foo = new Foo(); try { Combobulate(foo); return foo; } finally { foo.Dispose(); } }");
			
			TestLes("replace ($($format; $(@[#params] args)) => String.Format($format, $args))\n"+
			        @"   { MessageBox.Show($(""I hate {0}""; noun)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}"", noun));");
			TestLes("replace ($($format; $(@[#params] args)) => String.Format($format, $args))\n"+
			        @"   { MessageBox.Show($(""I hate {0}ing {1}s""; verb; noun), $(""FYI"";)); }",
			        @"MessageBox.Show(String.Format(""I hate {0}ing {1}s"", verb, noun), String.Format(""FYI""));");
		}

		[Test]
		public void TestReplace_match_attributes()
		{
			// [foo] a([attr] Foo) `MatchesPattern` 
			// [#trivia_, bar] a([$attr] $foo)
			
			// ([foo] F([x] X, [y] Y, [a1(...), a2(...)] Z) `MatchesPattern`
			//        F(X, $Y, $(params P), [$A, a1($(params args))] $Z)) == false cuz [x] is unmatched

			// TODO
		}

		[Test]
		public void TestReplaceInTokenTree()
		{
			TestLes("replace (foo => bar; bar => foo) { foo(@{ foo(*****) }); }", "bar(@{ bar(*****) });");
			TestLes("replace (foo => bar; bar => foo) { foo(@{ foo(%bar%) }); }", "bar(@{ bar(%foo%) });");
			TestLes("replace (foo => bar; bar => foo) { foo(@{ ***(%bar%) }); }", "bar(@{ ***(%foo%) });");
			TestLes(@"unroll ((A; B) `in` ((Eh; Bee); ('a'; ""b""); (1; 2d))) { foo(B - A, @{A + B}); }",
				@"foo(Bee - Eh,  @{Eh + Bee});
				  foo(""b"" - 'a', @{'a' + ""b""});
				  foo(2d - 1, @{1 + 2d});");
		}

		[Test]
		public void TestReplace_advanced()
		{
			// Nested replacements
			TestEcs(@"replace(X => Y, X($(params p)) => X($p)) { X = X(X, Y); }",
			        @"Y = X(Y, Y);");
			// Note: $a * $b doesn't work because it is seen as a variable decl
			TestEcs(@"replace(($a + $b + $c) => Add($a, $b, $c), ($a`*`$b) => Mul($a, $b))
			          { var y = 2*x*2 + 3*x + 4; }", 
			        @"var y = Add(Mul(Mul(2, x), 2), Mul(3, x), 4);");

			//TestEcs(@"replace(TO=>DO) {}", @"");
		}

		[Test]
		public void TestReplace_RemainingNodes()
		{
			TestEcs(@"{ replace(C => Console, WL => WriteLine); string name = ""Bob""; C.WL(""Hi ""+name); }",
			        @"{ string name = ""Bob""; Console.WriteLine(""Hi ""+name); }");
		}

		private void TestLes(string input, string outputLes, int maxExpand = 0xFFFF)
		{
			Test(input, LesLanguageService.Value, outputLes, LesLanguageService.Value, maxExpand);
		}
		private void TestEcs(string input, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(input, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		private void TestBoth(string inputLes, string inputEcs, string outputEcs, int maxExpand = 0xFFFF)
		{
			Test(inputLes, LesLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
			Test(inputEcs, EcsLanguageService.Value, outputEcs, EcsLanguageService.Value, maxExpand);
		}
		private void Test(string input, IParsingService inLang, string expected, IParsingService outLang, int maxExpand = 0xFFFF)
		{
			var lemp = NewLemp(maxExpand);
			using (ParsingService.PushCurrent(inLang))
			{
				var inputCode = new RVList<LNode>(inLang.Parse(input, _sink));
				var results = lemp.ProcessSynchronously(inputCode);
				var expectCode = outLang.Parse(expected, _sink);
				if (!results.SequenceEqual(expectCode))
				{	// TEST FAILED, print error
					string resultStr = results.Select(n => outLang.Print(n, _sink)).Join("\n");
					Assert.AreEqual(TestCompiler.StripExtraWhitespace(expected),
									TestCompiler.StripExtraWhitespace(resultStr));
				}
			}
		}
		MacroProcessor NewLemp(int maxExpand)
		{
			var lemp = new MacroProcessor(typeof(LeMP.Prelude.Macros), _sink);
			lemp.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			lemp.AddMacros(typeof(LeMP.StandardMacros));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			lemp.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
			lemp.MaxExpansions = maxExpand;
			return lemp;
		}
	}
}
