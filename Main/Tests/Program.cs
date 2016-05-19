using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeMP;
using LeMP.Tests;
using Loyc;
using Loyc.Collections;
using Loyc.Ecs.Tests;
using Loyc.MiniTest;

namespace Loyc.Tests
{
	public class RunMainTests
	{
        public static readonly VList<Pair<string, Func<bool>>> Menu = RunCoreTests.Menu.AddRange(
            new Pair<string, Func<bool>>[] {
                new Pair<string,Func<bool>>("Run unit tests of Enhanced C#", Test_Ecs),
                new Pair<string,Func<bool>>("Run unit tests of LeMP", Test_LeMP),
                new Pair<string,Func<bool>>("Run unit tests of LLLPG", Loyc.LLParserGenerator.Program.Test_LLLPG),
                new Pair<string,Func<bool>>("LeMP article examples", Samples.Samples.Run),
			});

		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );
            if (!RunCoreTests.RunMenu(Menu, args))
                // Let the outside world know that something
                // went wrong by setting the exit code to
                // '1'. This is particularly useful for
                // automated tests (CI).
                Environment.ExitCode = 1;
		}

		public static bool Test_Ecs()
		{
            return RunTests.RunMany(
                new EcsLexerTests(),
                new EcsParserTests(),
                new EcsNodePrinterTests(),
                new EcsValidatorTests());
		}
        public static bool Test_LeMP()
		{
            return RunTests.RunMany(
                new MacroProcessorTests(),
                new PreludeMacroTests(),
                new SmallerMacroTests(),
                new TestAlgebraicDataTypes(),
                new TestCodeContractMacros(),
                new TestCodeQuoteMacro(),
                new TestMacroCombinations(),
                new TestMatchCodeMacro(),
                new TestMatchMacro(),
                new TestOnFinallyReturnThrowMacros(),
                new TestReplaceMacro(),
                new TestSequenceExpressionMacro(),
                new TestSetOrCreateMemberMacro(),
                new TestUnrollMacro());
		}
	}
}
