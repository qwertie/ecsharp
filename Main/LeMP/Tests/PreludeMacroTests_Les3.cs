using System;
using System.Collections.Generic;
using Loyc;
using Loyc.Ecs;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;

namespace LeMP.Tests
{
	[TestFixture]
	public class PreludeMacroTests_Les3
	{
		SeverityMessageFilter _sink = new SeverityMessageFilter(ConsoleMessageSink.Value, Severity.DebugDetail);

		public void Test(string input, string output, int maxExpand = 0xFFFF)
		{
			using (LNode.SetPrinter(EcsLanguageService.Value))
			using (ParsingService.SetDefault(Les3LanguageService.Value))
				TestCompiler.Test(input, output, _sink, maxExpand, "LeMP.Prelude.Les3");
		}

		[Test]
		public void PreludeLoopStatements()
		{
			Test(".for (x = 0; x < 100; x++) { };",
				  "for (x = 0; x < 100; x++) { }");
			Test(".for (; x < 100; x++) { };",
				  "for (; x < 100; x++) { }");
			Test(".for (x = 0; ; ;) { };",
				  "for (x = 0; ; ) { }");
			Test(".for (x = 0; ; ) { };",
				  "for (x = 0; ; ) { }");
			Test(".foreach item in list { };",
				  "foreach (var item in list) { }");
			Test(".foreach (item in list) { };",
				  "foreach (var item in list) { }");
			Test(".while Foo { Bar(); };",
				  "while (Foo) { Bar(); }");
			Test(".do x++ while x < 100;",
				  "do x++; while(x < 100);");
			Test(".do x++ while (x < 100);",
				  "do x++; while((x < 100));");
		}

		[Test]
		public void IfStatements()
		{
			Test(".if x > 10 { WriteLine(\"Too many!!!\"); };",
				  "if (x > 10) { WriteLine(\"Too many!!!\"); }");
			Test(".if  x < 0  { Negative(); } else { NonNeg(); };",
				  "if (x < 0) { Negative(); } else { NonNeg(); }");
			Test(".if  x < 0  { Negative(); } elsif  x > 0  { Positive(); } else { Zero(); };",
				  "if (x < 0) { Negative(); } else if(x > 0) { Positive(); } else { Zero(); }");
			Test(".if  x < 1  { NonPositive(); } elseif (x > 1) { Positive(); } else { One(); };",
				  "if (x < 1) { NonPositive(); } else if((x > 1)){ Positive(); } else { One(); }");
			Test(".if  input.IsLowPriority  { WhoCares(); } else .while (!input.EOF)  { input.Read(); };",
				  "if (input.IsLowPriority) { WhoCares(); } else while ((!input.EOF)) { input.Read(); }");
		}

		[Test]
		public void ElseIfWithWarning()
		{
			Test(".if  x > 0  { Positive(); } else if (x < 0) { Negative(); } else { Zero(); };",
				  "if (x > 0) { Positive(); } else if (x < 0) { Negative(); } else { Zero(); }");
		}

		[Test]
		public void SwitchStatements()
		{
			Test(".switch  x  { .case 0; .break; .default; .break; }",
				  "switch (x) {  case 0: break;   default: break; }");
			Test(".switch  x  { .case 1 { .break; }; .default { .continue; }; }",
				  "switch (x) { case 1: {  break; }  default: {  continue; } }");
			Test(".switch  x  { .case 0 or 1 or -1       { .break; }; .default { .continue; }; }",
				  "switch (x) { case 0: case 1: case -1: {  break; }  default: {  continue; } }");
			Test(".switch  x  { .case 2: .break; .default: { .continue; } }",
				  "switch (x) {  case 2:  break;  default: {  continue; } }");
			Test(".switch  x  { .case 0 or 1:   .break; .default: .continue; }",
				  "switch (x) { case 0: case 1:  break;  default:  continue; }");
		}

		[Test]
		public void OtherPreludeExecutableStatements()
		{
			Test(".lock x {}",
				  "lock (x) {}");
			Test(".try { Abracadabra; } finally { Cleanup(); };",
				  "try { Abracadabra; } finally { Cleanup(); }");
			Test(".try { Blah; Blah; Blah; } catch ex: Exception { .throw; };",
				  "try { Blah; Blah; Blah; } catch (Exception ex) { throw; }");
			Test(".try { } catch  Exception  { } catch { } finally { Cleanup(); };",
				  "try { } catch (Exception) { } catch { } finally { Cleanup(); }");
			Test(".using A = B;",
				  "using A = B;");
		}

		[Test]
		public void VarDeclarations()
		{
			Test("x: Foo",
				 "Foo x;");
			// A good syntax for multi-var-declarations in LES3 is hard to find...
			//Test("x and y: Foo",
			//	 "Foo x, y;");
			//Test("x = 1 and y = 2: Foo",
			//	 "Foo x = 1, y = 2;");
			Test("x: int = default(int);",
				 "int x = default(int);");
			Test("dot: bool = false;",
				 "bool dot = false;");
			Test("nums: List!int;",
				 "List<int> nums;");
			Test("@readonly x: int = 5;",
				  "readonly int x = 5;");
			Test("@const x: int = 5;",
				  "const int x = 5;");
		}

		[Test]
		public void CoreGenericTypes()
		{
			Test("nums: array!int = null;",
				 "int[] nums = null;");
			Test("point: ptr!int;",
				 "int* point;");
			Test("maybe: opt!int;",
				 "int? maybe;");
			Test("nums: array!(2, int);",
				 "int[,] nums;");
		}

		[Test]
		public void CorePreludeOperators()
		{
			Test(@"x = y -> int; x = y AS string;",
				 "x = (int)y; x = y as string;");
			Test(@".var zero = default(int);",
				 "var zero = default(int);");
			Test("x = c ? a : b;",
				 "x = c ? a : b;");
			Test("a <: b;",
				 "@`'::=`(a, b);");
		}

		[Test]
		public void SpaceDeclarations()
		{
			Test("@partial .class Foo(System.Object) {};",
				 "partial class Foo : System.Object {}");
			Test(".struct Foo(IEnumerable, ICloneable) {};",
				 "struct Foo : IEnumerable, ICloneable {}");
			Test("@public .struct Point!T { @public X: T; @public Y: T; };",
				 " public  struct Point<T> { public T X; public T Y; }");
			Test("@private .enum Letters(byte) { A='a', B='b' };",
				 " private  enum Letters : byte { A='a', B='b' }");
			Test("@private .trait Foo { };",
				 " private  trait Foo { }");
			Test("@protected .alias A = B.C;",
				 " protected  alias A = B.C;");
			Test("@protected .alias A(IA) = B.C { };",
				 " protected  alias A = B.C : IA { }");
			Test("@internal .namespace Foo.Etc { Bar: string; };",
				 " internal  namespace Foo.Etc { string Bar; }");
		}

		[Test]
		public void QuickPropertyDeclarations()
		{
			Test("@protected X: int get _ set _;",
				" protected  int X { get; set; }");
			Test("@protected X: int get _x;",
				" protected int X { get => _x; }");
			Test("@protected X: int get { .return _x } set _x = value;",
				" protected int X { get { return _x; } set => _x = value; }");
			Test("@protected X: int set { _x = value } get _x;",
				" protected int X { get => _x; set { _x = value; } }");
		}

		[Test]
		public void PropertyDeclarations()
		{
			Test(".prop P: T { .get { } };",
			     "T P { get { } }");
			Test(".static P: T { .get _p };",
			      "static T P { get => _p; }");
			Test(".public P: T { .set { } };",
			      "public T P { set { } }");
			Test(".private P: T { .set _p = value };",
			      "private T P { set => _p = value; }");
			Test(".protected P: T { .get _p; .set { _p = value } };",
			      "protected T P { get => _p; set { _p = value; } }");
			Test(".internal P: T { set => _p = value; get => _p };",
			      "internal T P { set => _p = value; get => _p; }");
			Test(".virtual P: T { .get };",
			      "virtual T P { get; }");
			Test(".override P: T { .get; .set; };",
			      "override T P { get; set; }");
		}

		[Test]
		public void PropertyDeclarationWithInitializer()
		{
			Test(".public P: T { .get } initially 0;",
				  "public T P { get; } = 0;");
			Test(".public P: T { .get; .set; .init 7 }",
				  "public T P { get; set; } = 7;");
			Test(".public P: T { .init 7 }",
				  "public T P { get; } = 7;");
		}

		[Test]
		public void OtherPreludeDeclarations()
		{
			Test("@pub Foo: int = 0;",
				 "public int Foo = 0;");
			Test("@public Foo: int = 0;",
				 "public int Foo = 0;");
			Test("@extern .fn NoOp(x: int);",
				 " extern  void NoOp(int x);");
			Test("@static .fn Name(Arg1, Arg2): RetType {};",
				 " static RetType Name(Arg1, Arg2) {}");
			Test(".fn Spit(times: int = 1) {};",
				 "void Spit(int times = 1) {}");
			// TODO: support multiple args in keyword-expressions
			//Test("@protected .var X=0, Y=0",
			//	 "protected var X = 0, Y = 0;");
			Test("@protected @internal X: Int32 = 0;",
				 "protected internal Int32 X = 0;");
			// In EC#, this(...) prints as #this(...) if not inside a method
			Test(".fn Foo() { this(false); base(true); }",
				 "void Foo() { this(false); base(true); }");
			Test("@internal .cons Foo() { base(17); .return; }",
				 " internal Foo() : base(17) { return; }");
			Test("@public .cons Foo() { this(null); .return; }",
				 " public Foo() : this(null) { return; }");
		}

		[Test]
		public void TestChangeRemainingNodes()
		{
			// Use namespace macro to test the DropRemainingNodes feature
			Test(".namespace Foo { .import System; .namespace Etc; Bar: string; };",
				 "namespace Foo { using  System; namespace Etc { string Bar; } }");
			Test("{ .import System; .namespace Etc; Bar: string;   }; Baz();",
				 "{ using  System; namespace Etc { string Bar; } }  Baz();");
			Test("@internal .namespace Foo.Etc; Bar: string; @public .fn Baz() {};",
				 " internal  namespace Foo.Etc { string Bar; public void Baz() {} }");
		}
	}
}
