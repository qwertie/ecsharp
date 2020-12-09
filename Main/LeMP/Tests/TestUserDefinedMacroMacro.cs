using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeMP.Tests
{
	[TestFixture]
	public class TestUserDefinedMacroMacro : MacroTesterBase
	{
		[Test]
		public void BasicMacros()
		{
			TestEcs(@"
				macro poop($(.._))
				{
					return node.WithTarget((Symbol)""POOP"");
				}
				[Passive]
				macro ({ Int32 $(..v); })
				{
					return quote(int $(..v));
				}
				float f;
				Int32 i = poop(123);
				Int32 x, y = 0;",
				@"float f;
				int i = POOP(123);
				int x, y = 0;");
		}

		[Test]
		public void PriorityTest()
		{
			TestEcs(@"[""Change first argument to HELLO if it's not an identifier"", Passive]
				macro StupidDemoMacro($(arg0 && !arg0.IsId), $(..rest))
				{
					return node.WithArgChanged(0, quote(HELLO));
				}

				StupidDemoMacro(1 + 1, 2 + 2);
				StupidDemoMacro(goodbye, 2 + 2);
				", @"
				StupidDemoMacro(HELLO, 2 + 2);
				StupidDemoMacro(goodbye, 2 + 2);");
			TestEcs(@"
				[""Change first argument to 'hi'"", PriorityOverride, Passive]
				macro priorityTest($(arg0 && !arg0.IsIdNamed(""hi"")), $(.._))
				{
					return node.WithArgChanged(0, quote(hi));
				}
				[""Swap arg 0 and arg 1"", ProcessChildrenAfter, Passive]
				macro priorityTest($arg0, $arg1)
				{
					return node.WithArgs(arg1, arg0);
				}
				priorityTest(0, 1);",
				@"priorityTest(1, hi);");
		}
	}
}
