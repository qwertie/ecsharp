using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Ecs;

namespace LeMP.Test
{
	[ContainsMacros]
	public class TestMacros
	{
		[LexicalMacro("Identity(args...)", "Expanded args in-place (kinda pointless?) for testing")]
		public static LNode Identity(LNode node, IMessageSink sink)
		{
			return node.WithName(S.Splice);
		}
		[LexicalMacro("priorityTest(x, y)", "Change first argument to 'hi'", 
			"priorityTest", "priorityTestPCB", Mode = MacroMode.PriorityOverride)]
		public static LNode priorityTestHi(LNode node, IMessageSink sink)
		{
			if (node.ArgCount >= 1 && !node[0].IsIdNamed("hi"))
				return node.WithArgChanged(0, LNode.Id("hi"));
			return null;
		}
		[LexicalMacro("priorityTest(x, y)", "Swap arg 0 and arg 1", Mode = MacroMode.ProcessChildrenAfter)]
		public static LNode priorityTest(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithArgs(node[1], node[0]);
			return null;
		}
		[LexicalMacro("priorityTestPCB(x, y)", "Swap arg 0 and arg 1", Mode = MacroMode.ProcessChildrenBefore)]
		public static LNode priorityTestPCB(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithArgs(node[1], node[0]);
			return null;
		}
	}
}
namespace LeMP
{
	/// <summary>A simple version of Compiler that takes a single input and produces 
	/// a StringBuilder. Pre-opens LeMP.Prelude namespace.</summary>
	public class TestCompiler : Compiler
	{
		public TestCompiler(IMessageSink sink, ICharSource text, string fileName = "")
			: base(sink, typeof(LeMP.Prelude.BuiltinMacros), new[] { new InputOutput(text, fileName) }) 
		{
			Parallel = false;
			MacroProcessor.AddMacros(typeof(LeMP.Prelude.Les.Macros));
			MacroProcessor.AddMacros(typeof(LeMP.StandardMacros));
			MacroProcessor.AddMacros(typeof(LeMP.Test.TestMacros));
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
			MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
		}
			
		public StringBuilder Output;
		public VList<LNode> Results;
			
		protected override void WriteOutput(InputOutput io)
		{
			Results = io.Output;
			Output = new StringBuilder();
			foreach (LNode node in Results) {
				LNode.Printer(node, Output, Sink, null, IndentString, NewlineString);
				Output.Append(NewlineString);
			}
		}

		#region static Test(), StripExtraWhitespace() methods

		public static void Test(string input, string output, IMessageSink sink, int maxExpand = 0xFFFF)
		{
			using (LNode.PushPrinter(new EcsNodePrinter(null, null) { PreferPlainCSharp = true }.Print)) {
				var c = new TestCompiler(sink, new UString(input), "");
				c.MaxExpansions = maxExpand;
				c.MacroProcessor.AbortTimeout = TimeSpan.Zero; // never timeout (avoids spawning a new thread)
				c.Run();
				Assert.AreEqual(StripExtraWhitespace(output), StripExtraWhitespace(c.Output.ToString()));
			}
		}
		
		static readonly string[] CommentPrefix = new[] { "//" };
		/// <summary>Strips whitespace and single-line comments from a string.
		/// Helps test whether two blocks of code are "sufficiently equal".</summary>
		public static string StripExtraWhitespace(string a, string[] commentPrefixes = null)
		{
			commentPrefixes = commentPrefixes ?? CommentPrefix;
			StringBuilder sb = new StringBuilder();
			char prev_c = '\0';
			for (int i = 0; i < a.Length; i++) {
				char c = a[i];

				var slice = a.USlice(i);
				for (int cp = 0; cp < commentPrefixes.Length; cp++) {
					if (slice.StartsWith(commentPrefixes[cp])) {
						do ++i; while (i < a.Length && (c = a[i]) != '\n' && c != '\r');
						break;
					}
				}

				if (c == '\n' || c == '\r' || c == '\t')
					c = ' ';
				if (c == ' ' && (!MaybeId(prev_c) || !MaybeId(a.TryGet(i + 1, '\0'))))
					continue;

				sb.Append(c);
				prev_c = c;
			}
			return sb.ToString();
		}
		static bool MaybeId(char c) { return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'); }

		#endregion
	}

	[TestFixture]
	public class MacroProcessorTests
	{
		[Test]
		public void TrivialTest()
		{
			Test("no macros.apply here;",   // LES input
				"no(macros.apply, here);"); // C# output
			Test("while (@true) {};",       // LES input
				"while ((true)) {}");       // C# output
		}

		[Test]
		public void ExpandLimit()
		{
			Test("{ x::Foo; y::int; }",
				"{ Foo x; int y; }", 2);
			Test("{ x::Foo; y::int; }",
				"{ Foo x; @int y; }", 1);
			Test("@[static] def Main()::void { var x::int = `default` int; }",
				"[@static] @void Main() { @var(x::@int = @default(@int)); }", 1);
			Test("@[static] def Main()::void { var x::int = `default` int; }",
				"static void Main() { @int x = @default(@int); }", 2);
			Test("@[static] def Main()::void { var x::int = `default` int; }",
				"static void Main() { int x = default(@int); }", 3);
			Test("@[static] def Main()::void { var x::int = `default` int; }",
				"static void Main() { int x = default(int); }", 4);
		}

		[Test]
		public void JustSpliceTest()
		{
			Test("the #splice(macro, inserts, stuff, in-place) here;",
				"the (macro, inserts, stuff, @in-place, here);");
		}

		[Test]
		public void NoLexicalMacrosTest()
		{
			Test("#noLexicalMacros(blocks macros, e.g. break, return Foo);",
				"blocks(macros); e.g.@break; @return(Foo);");
		}

		[Test]
		public void ImportsTest()
		{
			Test("import_macros LeMP.Test; x();",
				"x();");
			Test("import x.y;",
				"using x.y;");
			Test("Identity(x); { import LeMP.Test; Identity(x); }; Identity(x);",
				"Identity(x); { using LeMP.Test; x; } Identity(x);");
			Test("{{ import LeMP.Test; Identity(x); }}; Identity(x);",
				"{{ using LeMP.Test; x; }} Identity(x);");
		}

		[Test]
		public void CorePreludeExecutableStatements()
		{
			Test("for (x = 0; x < 100; x++) { };",
			     "for (x = 0; x < 100; x++) { }");
			Test(@"foreach (item `in` list) { };",
			     "foreach (var item in list) { }");
			Test("while Foo { Bar(); };",
			     "while (Foo) { Bar(); }");
			Test("do x++ while (x < 100);",
			     "do x++; while((x < 100));");
			Test("if x > 10 { WriteLine(\"Too many!!!\"); };",
			     "if (x > 10) { WriteLine(\"Too many!!!\"); }");
			Test("if  x < 0  { Negative(); } else { NonNeg(); };",
				 "if (x < 0) { Negative(); } else { NonNeg(); }");
			Test("if  x < 0  { Negative(); } else if (x > 0) { Positive(); } else { Zero(); };",
				 "if (x < 0) { Negative(); } else if((x > 0)){ Positive(); } else { Zero(); }");
			Test("unless input.IsLowPriority { Process(input); };",
			     "if  (!input.IsLowPriority) { Process(input); }");
			Test("if  input.IsLowPriority  { WhoCares(); } else while (!input.EOF)  { input.Read(); };",
				 "if (input.IsLowPriority) { WhoCares(); } else while((!input.EOF)) { input.Read(); }");
			Test("switch  x  { case 0; break; default; break; }",
			     "switch (x) { case 0: break; default: break; }");
			Test("switch  x  { case 0 { break; }; default { continue; }; }",
			     "switch (x) { case 0: { break; } default: { continue; } }");
			Test("lock x {}",
			     "lock (x) {}");
			Test("try { Blah; Blah; Blah; } catch (ex::Exception) { throw; };",
			     "try { Blah; Blah; Blah; } catch (Exception ex) { throw; }");
			Test("try { Blah; Blah; Blah; } finally { Cleanup(); };",
			     "try { Blah; Blah; Blah; } finally { Cleanup(); }");
			Test("try { } catch  Exception  { } catch { } finally { Cleanup(); };",
			     "try { } catch (Exception) { } catch { } finally { Cleanup(); }");
			Test("readonly x::int = 5;",
			     "readonly int x = 5;");
			Test("const x::int = 5;",
			     "const int x = 5;");
			Test("using A = B;",
				 "using A = B;");
		}

		[Test]
		public void DataTypes()
		{
			Test("var(a::byte, b::sbyte, c::short, d::ushort);",
			     "byte a; sbyte b; short c; ushort d;");
			Test("var(a::int, b::uint, c::long, d::ulong);",
			     "int a; uint b; long c; ulong d;");
			Test("var(a::float, b::double, c::char, d::string);",
			     "float a; double b; char c; string d;");
			Test("var(a::object=@null, b::decimal, c::bool, d::void);",
			     "object a = null; decimal b; bool c; void d;");
			Test("x::int = default(int);",
			     "int x = default(int);");
			Test("dot::bool = false;",
			     "bool dot = false;");
			Test("nums::array!int = null;",
			     "int[] nums = null;");
			Test("point::ptr!int;",
			     "int* point;");
			Test("maybe::opt!int;",
			     "int? maybe;");
			Test("nums::array!(2, int);",
			     "int[,] nums;");
			Test("nums::List!int;",
			     "List<int> nums;");
		}

		[Test]
		public void CorePreludeOperators()
		{
			Test("static x := (new List!int(100));",
			     "static var x = new List<int>(100);");
			Test(@"x = y `cast` int; x = y `as` string;",
			     "x = (int)y; x = y as string;");
			Test(@"var zero = default(int);",
			     "var zero = default(int);");
			Test("x = c ? a : b;",
			     "x = c ? a : b;");
			Test("a : b;",
			     "a`#namedArg`b;");
		}

		[Test]
		public void CorePreludeDeclarations()
		{
			Test("@[partial] class Foo(System.Object) {};",
				 "partial class Foo : System.Object {}");
			Test("struct Foo(IEnumerable, ICloneable) {};",
				 "struct Foo : IEnumerable, ICloneable {}");
			Test("@[pub] Foo::int = 0;",
				 "public int Foo = 0;");
			Test("public Foo::int = 0;",
				 "public int Foo = 0;");
			Test("@[public] struct Point!T { public X::T; public Y::T; };",
				 " public  struct Point<T> { public T X; public T Y; }");
			Test("@[private] enum Letters(byte) { A='a'; B='b'; };",
				 " private  enum Letters : byte { A='a', B='b' }");
			Test("@[private] trait Foo { };",
				 " private  trait Foo { }");
			Test("@[protected] alias A = B.C;",
				 " protected  alias A = B.C;");
			Test("@[protected] alias A(IA) = B.C { };",
				 " protected  alias A = B.C : IA { }");
			Test("@[internal] namespace Foo.Etc { Bar::string; };",
				 " internal  namespace Foo.Etc { string Bar; }");
			Test("@[extern] fn NoOp(x::int);",
				 " extern  void NoOp(int x);");
			Test("@[static] fn Name(Arg1, Arg2)::RetType {};",
				 " static RetType Name(Arg1, Arg2) {}");
			Test("fn Spit(times::int = 1) {};",
				 "void Spit(int times = 1) {}");
			Test("@[protected] prop X::int { get; set; };",
				 " protected  int X { get; set; }");
			Test("prop X::int { get { return _x; }; };",
				 "int X { get { return _x; } }");
			Test("@[protected] var(X=0, Y=0);",
				 "protected var X = 0, Y = 0;");
			Test("@[protected] internal X::int = 0;",
				 "protected internal int X = 0;");
			// In EC#, this(...) prints as #this(...) if not inside a method
			Test("fn Foo() { this(@false); base(@true); }",
				 "void Foo() { this(false); base(true); }");
			Test("@[internal] cons Foo() { base(17); return; }",
			     " internal Foo() : base(17) { return; }");
			Test("@[public] cons Foo() { this(@null); return; }",
			     " public Foo() : this(null) { return; }");
		}
		
		[Test]
		public void TestChangeRemainingNodes()
		{
			// Use namespace macro to test the DropRemainingNodes feature
			Test("namespace Foo { import System; namespace Etc; Bar::string; };",
				 "namespace Foo { using  System; namespace Etc { string Bar; } }");
			Test("{ import System; namespace Etc; Bar::string;   }; Baz();",
				 "{ using  System; namespace Etc { string Bar; } }  Baz();");
			Test("@[internal] namespace Foo.Etc; Bar::string; @[public] fn Baz() {};",
				 " internal  namespace Foo.Etc { string Bar; public void Baz() {} }");
		}

		[Test]
		public void Regressions()
		{
			Test("prop x::int { get { return 0; } set; };",
				"int x { get ({ return 0; }, set); }");
		}

		[Test]
		public void PriorityTest()
		{
			Test("import_macros LeMP.Test; priorityTest(0, 1);",
			                              "priorityTest(1, hi);");
			Test("{ import_macros LeMP.Test; foo0(); priorityTest(0, 2); foo(); }",
				 "{                          foo0(); priorityTest(2, hi); foo(); }");
			Test("{ import_macros LeMP.Test; priorityTest(0, x::int = 3); foo(); }",
				 "{                          priorityTest(int x = 3, hi); foo(); }");
			Test("{ import_macros LeMP.Test; priorityTestPCB(0, x::int = 4); foo2(); }",
				 "{                          priorityTestPCB(int x = 4, hi); foo2(); }");
		}

		SeverityMessageFilter _sink = new SeverityMessageFilter(MessageSink.Console, Severity.Debug);

		private void Test(string input, string output, int maxExpand = 0xFFFF)
		{
			TestCompiler.Test(input, output, _sink, maxExpand);
		}
	}
}
