using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Ecs;
using Loyc.MiniTest;
using Loyc.Syntax.Les;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestOnFinallyReturnThrowMacros : MacroTesterBase
	{
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
		public void Test_on_throw_catch()
		{
			TestEcs(@"{ bool ok = true; on_throw_catch { ok = false; } DoSomeStuff(); Etc(); }",
					@"{ bool ok = true; try { DoSomeStuff(); Etc(); } catch { ok = false; } }");
			TestEcs(@"{ _crashed = false; on_throw_catch { _crashed = true; } DoSomeStuff(); }",
					@"{ _crashed = false; try { DoSomeStuff(); } catch { _crashed = true; } }");
			TestEcs(@"{ on_throw_catch(ex) { MessageBox.Show(ex.Message); } Etc(); }",
					@"{ try { Etc(); } catch(Exception ex) { MessageBox.Show(ex.Message); } }");
			TestEcs(@"on_throw_catch(FormatException ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(FormatException ex) { MessageBox.Show(ex.Message); }");
		}

		[Test]
		public void Test_on_throw()
		{
			TestEcs(@"on_throw(ex) { MessageBox.Show(ex.Message); } Etc();",
					@"try { Etc(); } catch(Exception ex) { MessageBox.Show(ex.Message); throw; }");
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
			TestEcs(@"on_return(int r) { r++; } return 5;",
			        @"{ int r = 5; r++; return r; }");
			Test(@"// EC# parser: macro claimed 'no return statements were found in this context'
				// I think we need preprocessing on the code below on_return.
				fn TentativeExpr()::LNode {
					_deferErrors = true;
					on_return {
						_deferErrors = false;
						if ResetIgnoredErrors() { return @null; };
					};
					return Expr(ContinueExpr);
				};", Les2LanguageService.Value,
				@"
				LNode TentativeExpr() {
					_deferErrors = true;
					{
						var __result__ = Expr(ContinueExpr);
						_deferErrors = false;
						if (ResetIgnoredErrors()) {
							return null;
						}
						return __result__;
					}
				}", EcsLanguageService.Value);
			TestEcs(@"{ on_return(R) { Log(R); } Foo(); return Bar(); AfterReturn(); }",
			        @"{ Foo(); { var R = Bar(); Log(R); return R; } AfterReturn(); }");
		}

		[Test]
		public void Test_on_return_InSwitch()
		{
			TestEcs(@"
				switch (x) {
					on_return {Neat();}
					case 1: return one;
					case 2: Etc(); return two;
					default: return els;
				}", @"
				switch (x) {
					case 1: 
						{ var __result__ = one;
						  Neat();
						  return __result__; }
					case 2: 
						Etc();
						{ var __result__ = two;
						  Neat();
						  return __result__; }
					default:
						{ var __result__ = els;
						  Neat();
						  return __result__; }
				}
			");
		}

		[Test(Fails = "TODO - eventually")]
		public void Test_on_return_InSwitch2()
		{
			TestEcs(@"
				switch (x) {
					case 1: on_return {One();} return one;
					case 2: on_return(r) {Two();} Etc(); return two;
					default: on_return(var e) {Bleh();} return els;
				}", @"
				switch (x) {
					case 1: 
						{ var __result__ = one;
						  One();
						  return __result__; }
					case 2: 
						Etc();
						{ var r = two;
						  Two();
						  return r; }
					default:
						{ var e = els;
						  Bleh();
						  return e; }
				}
			");
		}

		[Test]
		public void Test_on_return_NoReturnStatement()
		{
			// First, a couple of situations where on_return will have no effect except a warning message.
			var msgs = _msgHolder;
			using (MessageSink.PushCurrent(_msgHolder)) {
				msgs.List.Clear();
				TestEcs(@"void Foo() { { on_return() { H(); } F(G()); } }",
				        @"void Foo() { {                      F(G()); } }");
				Assert.IsTrue(msgs.List.Count > 0);
				msgs.List.Clear();
				TestEcs(@"x => { if (x) { on_return() { H(); } F(); G(); } }",
				        @"x => { if (x) {                      F(); G(); } }");
				Assert.IsTrue(msgs.List.Count > 0);
				msgs.List.Clear();
				TestEcs(@"set { { on_return() { H(); } F(G()); } }",
				        @"set { {                      F(G()); } }");
				Assert.IsTrue(msgs.List.Count > 0);
			}
			// And now some situations where it SHOULD add a handler or two.
			TestEcs(@"
				public static void Main(string[] args) {
					on_return { bye(); }
					hi();
				}", 
				@"public static void Main(string[] args) {
					hi();
					bye();
				}");
			TestEcs(@"void Foo() { F(); on_return() { Console.WriteLine(); } G(); }",
			        @"void Foo() { F(); G(); Console.WriteLine(); }");
			TestEcs(@"void Foo(bool c) { on_return() { G(); H(); } if (c) return; F(); }",
			        @"void Foo(bool c) { if (c) { G(); H(); return; } F(); { G(); H(); } }");
			TestEcs(@"class Foo { public Foo() : base() { on_return() { G(); H(); } F(); } }",
			        @"class Foo { public Foo() : base() { F(); { G(); H(); } } }");
			TestEcs(@"set { on_return() { G(); H(); } F(); }",
			        @"set { F(); { G(); H(); } }");
			TestEcs(@"f = x => { on_return { G(); H(); } F(); };",
			        @"f = x => { F(); { G(); H(); } };");
			TestEcs(@"delegate(int x) { on_return { G(); H(x); } F(x); };",
			        @"delegate(int x) { F(x); { G(); H(x); } };");
		}

		[Test]
		public void Test_on_return_IgnoresLambda()
		{
			TestEcs(@"{ on_return { Returning(); } 
			            if (list.Any(x => { return x != null; })) 
			                return true; 
			            return false;
			          }",
			        @"{ if (list.Any(x => { return x != null; })) {
			                var __result__ = true;
			                Returning();
			                return __result__; 
			            }
			            {
			                var __result__ = false;
			                Returning();
			                return __result__;
			            }
			          }");
		}

	}
}
