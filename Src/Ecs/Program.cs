using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Loyc.MiniTest;
using Loyc.CompilerCore;
using Loyc;
using Loyc.Utilities;
using Loyc.Syntax;
using Ecs.Parser;

namespace Ecs
{
	public class Program
	{
		public static void Main(string[] args)
		{
			// Workaround for MS bug: Assert(false) will not fire in debugger
			Debug.Listeners.Clear();
			Debug.Listeners.Add( new DefaultTraceListener() );

			//RunTests.Run(new LNodeTests());
			RunTests.Run(new EcsLexerTests());
			RunTests.Run(new EcsParserTests());
			RunTests.Run(new GTests());
			RunTests.Run(new EcsNodePrinterTests());
		}
	}
}
