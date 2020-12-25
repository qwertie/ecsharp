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
		public void MacroWithUsingDirective()
		{
			// TODO: add LeMP feature for disposing/end of file/end of block
			//       until then Reset manually to avoid interference from other tests
			StandardMacros.ResetRoslyn();

			TestEcs(@"
				macro digits($(str && str.Value is string strValue))
				{
					using System.Text.RegularExpressions;

					var re = new Regex(""[0-9]"");
					return LNode.Literal(re.Matches(strValue).Count);
				}
				int i = digits(""I have 25 apples an 7 bananas"");
				", @"
				int i = 3;");

			StandardMacros.ResetRoslyn();
			TestEcs(@"
				using System.Text;

				macro poop($(.._))
				{
					var sb = new StringBuilder(""POOP"");
					return node.WithTarget((Symbol) sb.ToString());
				}

				int i = poop(123);
				", @"
				using System.Text;

				int i = POOP(123);");
		}

		[Test]
		public void PriorityTest()
		{
			// TODO: add LeMP feature for disposing/end of file/end of block
			//       until then Reset manually to avoid interference from other tests
			StandardMacros.ResetRoslyn();

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
