using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class PreludeMacroTests
	{
		SeverityMessageFilter _sink = new SeverityMessageFilter(MessageSink.Console, Severity.DebugDetail);

		private void Test(string input, string output, int maxExpand = 0xFFFF)
		{
			TestCompiler.Test(input, output, _sink, maxExpand, false);
		}

		[Test]
		public void PreludeLoopStatements()
		{
			Test("for (x = 0; x < 100; x++) { };",
			     "for (x = 0; x < 100; x++) { }");
			Test("for (; x < 100; x++) { };",
			     "for (; x < 100; x++) { }");
			Test("for (x = 0; ; ;) { };",
			     "for (x = 0; ; ) { }");
			Test("for (x = 0; ; ) { };",
			     "for (x = 0; ; ) { }");
			Test(@"foreach (item `in` list) { };",
			     "foreach (var item in list) { }");
			Test("while Foo { Bar(); };",
			     "while (Foo) { Bar(); }");
			Test("do x++ while (x < 100);",
			     "do x++; while((x < 100));");
		}

		[Test]
		public void OtherPreludeExecutableStatements()
		{
			Test("if x > 10 { WriteLine(\"Too many!!!\"); };",
			     "if (x > 10) { WriteLine(\"Too many!!!\"); }");
			Test("if  x < 0  { Negative(); } else { NonNeg(); };",
				 "if (x < 0) { Negative(); } else { NonNeg(); }");
			Test("if  x < 0  { Negative(); } else if (x > 0) { Positive(); } else { Zero(); };",
				 "if (x < 0) { Negative(); } else if((x > 0)){ Positive(); } else { Zero(); }");
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
			Test(@"x = y -> int; x = y `as` string;",
			     "x = (int)y; x = y as string;");
			Test(@"var zero = default(int);",
			     "var zero = default(int);");
			Test("x = c ? a : b;",
			     "x = c ? a : b;");
			Test("a : b;",
			     "#namedArg(a, b);");
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
	}
}
