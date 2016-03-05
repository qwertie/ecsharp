using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Ecs.Parser;
using Loyc.MiniTest;

namespace Loyc.Tests
{
	public class RunMainTests
	{
		public static readonly VList<Pair<string, Action>> Menu = RunCoreTests.Menu.AddRange(
			new Pair<string, Action>[] {
				new Pair<string,Action>("Run unit tests of Enhanced C#", Test_Ecs),
				new Pair<string,Action>("Run unit tests of LeMP", Test_LeMP),
				new Pair<string,Action>("Run unit tests of LLLPG",Loyc.LLParserGenerator.Program.Test_LLLPG),
			});

		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );
			RunCoreTests.RunMenu(Menu);
		}

		public static void Test_Ecs()
		{
			RunTests.Run(new EcsLexerTests());
			RunTests.Run(new EcsParserTests());
			RunTests.Run(new EcsNodePrinterTests());
		}
		public static void Test_LeMP()
		{
			RunTests.Run(new MacroProcessorTests());
			RunTests.Run(new StandardMacroTests());
		}
	}
}
