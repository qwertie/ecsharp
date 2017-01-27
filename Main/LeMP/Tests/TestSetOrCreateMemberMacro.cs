using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestSetOrCreateMemberMacro : MacroTesterBase
	{
		[Test]
		public void TestOnMethods()
		{
			TestEcs("void Set(set int X, set int Y) {}",
				"void Set(int x, int y) { X = x; Y = y; }");
			TestEcs("void Set(public int X, bool Y, private string Z) { if (Y) Rejoice(); }",
				@"public int X; private string Z; 
				void Set(int x, bool Y, string z) {
					X = x; Z = z; if (Y) Rejoice();
				}");
		}

		[Test]
		public void TestCreateProperties()
		{
			TestEcs("void Prop(public Foo Foo {get; private set;}) {}", @"
				public Foo Foo {get; private set;} 
				void Prop(Foo foo) { Foo = foo; }");
			TestEcs("void Prop(public Foo Foo {get; private set;}, int _bar {get;} = 0) {}", @"
				public Foo Foo {get; private set;} 
				int _bar {get;}
				void Prop(Foo foo, int bar = 0) { Foo = foo; _bar = bar; }");
		}

		[Test]
		public void TestOnConstructor()
		{
			TestEcs(@"class Point { 
				public Point(public int X, public int Y) {}
				public this(set int X, set int Y) {}
			}", @"
			class Point {
				public int X;
				public int Y;
				public Point(int x, int y) { X = x; Y = y; }
				public Point(int x, int y) { X = x; Y = y; }
			}");

			TestEcs(@"class Point { 
				public Point(public int X, public int Y) : base(X + 1, Y + 1) {}
			}", @"
			class Point {
				public int X;
				public int Y;
				public Point(int x, int y) : base(x + 1, y + 1) { X = x; Y = y; }
			}");
		}

		[Test]
		public void TestNoBodyError()
		{
			using (MessageSink.SetDefault(_msgHolder)) {
				TestEcs("void Set(set int X);", "void Set(set int X);"); // Body required
				Assert.IsTrue(_msgHolder.List.Count > 0, "warning expected");
			}
		}

		[Test]
		public void TestAttributeTransfer()
		{
			TestEcs(
				@"void Set(
					// Hello
					[Spanish] set int _hola, 
					[field: English] static int _hello, 
					[field: Alzheimer's] partial long goodbye = 8, 
					[Hawaii] protected internal string Aloha = 5,
					// Comment
					[French] internal string _Bonjour = 7,
					[Other] readonly int _ciao = 4
				) { Foo(_ciao); }",
				@"
				[English] static int _hello;
				
				[Alzheimer's] partial long goodbye;
				
				protected internal string Aloha;
				
				// Comment
				internal string _Bonjour;

				void Set(
					// Hello
					[Spanish] int hola, int hello, long goodbye = 8, [Hawaii] string aloha = 5, [French] string Bonjour = 7, [Other] readonly int _ciao = 4)
				{
					_hola = hola;
					_hello = hello;
					this.goodbye = goodbye;
					Aloha = aloha;
					_Bonjour = Bonjour;
					Foo(_ciao);
				}");
			TestEcs("void Set(public params string[] Strs) {}",
				"public string[] Strs; void Set(params string[] strs) { Strs = strs; }");
			TestEcs("void Set([A] [property: B] static string[] Strs { get; private set; }) {}",
				@"	[B] static string[] Strs { get; private set; }
					void Set([A] string[] strs) { Strs = strs; }");
		}
	}
}
