using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Loyc.MiniTest;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Loyc.Ecs.Parser;

/// <summary>Enhanced C#. Currently only the parser (<see cref="Ecs.Parser.EcsParser"/>)
/// and printer (<see cref="Ecs.EcsNodePrinter"/>) are implemented, so LeMP is used 
/// to convert supported features of EC# to C#.</summary>
namespace Loyc.Ecs
{
	/// <summary>Entry point: runs the EC# test suite and related tests.</summary>
	public class Program
	{
		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );

			RunEcsTests();
			RunTests.Run(new GTests());
			//RunTests.Run(new LNodeTests());
		}

		public static void RunEcsTests()
		{
			RunTests.Run(new EcsLexerTests());
			RunTests.Run(new EcsParserTests());
			RunTests.Run(new EcsNodePrinterTests());
		}
	}
}
