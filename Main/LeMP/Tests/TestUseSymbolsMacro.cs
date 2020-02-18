using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.MiniTest;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestUseSymbolsMacro : MacroTesterBase
	{
		[Test]
		public void Basics()
		{
			TestLes("@[Attr] #useSymbols; @@foo;",
				@"@[Attr, #static, #readonly] #var(Symbol, sy_foo = @'cast(""foo"", Symbol)); sy_foo;");
			TestEcs("[Attr] #useSymbols; Symbol status = @@OK;",
				@"[Attr] static readonly Symbol sy_OK = (Symbol) ""OK""; Symbol status = sy_OK;");
			TestLes("@[Attr] #useSymbols; @@`->`;",
				@"@[Attr, #static, #readonly] #var(Symbol, @sy_-> = @'cast(""->"", Symbol)); @sy_->;");
		}
		[Test]
		public void WithinTypes()
		{
			TestEcs(@"[Attr] #useSymbols; 
					public class X { 
						Symbol status = @@OK; 
						interface Y { void Foo(Symbol arg = @@foo); }
					}
					public struct Z { public static Symbol z = @@Z; }
				", @"
					public class X { 
						[Attr] static readonly Symbol sy_OK = (Symbol) ""OK"", sy_foo = (Symbol) ""foo""; 
						Symbol status = sy_OK; 
						interface Y { void Foo(Symbol arg = sy_foo); }
					}
					public struct Z { 
						[Attr] static readonly Symbol sy_Z = (Symbol) ""Z"";
						public static Symbol z = sy_Z;
					}
				");
		}

		[Test]
		public void WithOptions()
		{
			TestEcs("[Attr] #useSymbols(prefix(_), inherit(@@OK)); Symbol status = @@OK;",
				@"Symbol status = _OK;");
			TestEcs(@"public #useSymbols(prefix: S_, inherit: (@@Good, @@Bad)); 
				Symbol status = @@OK ?? @@Good;
				Symbol Err() { return @@Bad ?? @@Error; }",
				@"public static readonly Symbol S_OK = (Symbol) ""OK"", S_Error = (Symbol) ""Error"";
				Symbol status = S_OK ?? S_Good;
				Symbol Err() { return S_Bad ?? S_Error; }");
		}

		[Test]
		public void BugFix()
		{
			// Bug: `using X = Y` caused IndexOutOfRangeException
			TestEcs(@"[Attr] #useSymbols; 
				using X = Y; Symbol z = @@foo;",
				@"[Attr] static readonly Symbol sy_foo = (Symbol)""foo""; 
				using X = Y; Symbol z = sy_foo;");
		}
	}
}
