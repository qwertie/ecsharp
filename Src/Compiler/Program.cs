using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Loyc.CompilerCore;
using Loyc.Utilities;

namespace Loyc.Compiler
{
	class Program
	{
		static string _defaultFolder = System.Environment.CurrentDirectory;
		public static string DefaultFolder { get { return _defaultFolder; } }

		/// <summary>Parses command-line arguments and uses BlooPipeline to process the code.</summary>
		public static void Main(string[] args_)
		{
			// btw: args[0] is the first argument, not the exe filename.
			//      the .NET framework handles quoted arguments (the quotes are removed).
			List<string> args = new List<string>(args_);
			Dictionary<string, string> options = new Dictionary<string,string>();

			G.ProcessCommandLineArguments(args, options, DefaultFolder, 5000);

			if (options.ContainsKey("help") || args.Contains("-help") || args.Contains("/?") || args.Contains("-?"))
			{
				ShowHelpPage(args);
				return;
			}

			Run(args, options);
		}

		public static void ShowHelpPage(IList<string> args)
		{
			Output.RawLine("TODO");
		}

		public static void Run(List<string> args, Dictionary<string, string> options)
		{
			// TODO: compiler pipeline
		}
	}
}
