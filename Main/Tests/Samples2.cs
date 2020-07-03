//
// A file for playing around
// 
using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Lexing;
using Loyc.Ecs;
using Loyc.Syntax.Les;

namespace Samples
{
	partial class PlayPen
	{

	}
}
namespace Samples
{
	partial class Samples : Assert
	{
		[Test]
		public void Les3PrettyPrinterDemo()
		{
			// Pretty printer demo
			var code = Les3LanguageService.Value.Parse(
				@".memory 1
				  .fn _sumIntegersDemo(input: i32, length: i32): i32 {
					sum: i32
					sum = 0
					.block {
					  .br stop if input s< 1
					  sum = 0
					  .loop loop {
						// I picked := for set_local and = for tee_local;
						// feel free to vote your own preference.
						$sum := i32[$input] + $sum
						$input := $input + 4
						.br loop if $length = $length + -1
					  }
					  :stop
					}
					sum // return value
				  }", msgs: ConsoleMessageSink.Value);
			var pp = new Les3PrettyPrinter(null, new Les3PrinterOptions { IndentString = "  " });
			pp.PrintToConsole(code.Cast<ILNode>());
		}

		/*[Test]*/ public void PrintAllTheNames()
		{
			try {
				PrintAllTheNames(@"..\..\Main\LeMP.StdMacros");
			} catch {}
		}

		public static void PrintAllTheNames(string path)
		{
			using (ParsingService.SetDefault(EcsLanguageService.Value))
			using (MessageSink.SetDefault(ConsoleMessageSink.Value))
				foreach (var filename in Directory.GetFiles(path, "*.cs")) {
					Console.WriteLine(filename);
					foreach (var stmt in EcsLanguageService.Value.ParseFile(filename))
						stmt.ReplaceRecursive(node => {
							var name = PlayPen.GetName(node);
							if (name != null)
								MessageSink.Default.Write(Severity.Note, node,
									"Found {0} named {1}", node.Name, name);
							return null; // do not change anything
						});
				}
		}

		#if !DotNet3 // .NET 3 can't enumerate files recursively
		/*[Test]*/
		public void ScanForCsStrings()
		{
			var folder = AppDomain.CurrentDomain.BaseDirectory;
			int i = folder.IndexOf(@"bin\", StringComparison.OrdinalIgnoreCase);
			if (i == -1)
				i = folder.IndexOf(@"bin\", StringComparison.OrdinalIgnoreCase);
			if (i != -1)
				folder = folder.Substring(0, i);
			ScanForCsStrings(folder, ConsoleMessageSink.Value);
		}

		// Scan source code for strings and print them out!
		void ScanForCsStrings(string path, IMessageSink msgOut)
		{
			var files = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories);
			using (MessageSink.SetContextToString(ctx => {
				// Show line and column instead of character range
				if (ctx is SourceRange) return ((SourceRange)ctx).Start.ToString();
				return MessageSink.GetLocationString(ctx);
			})) {
				foreach (string file in files) {
					// Important: ignore comments because their values are strings
					// and we are only interested in actual strings.
					var code = Loyc.Ecs.EcsLanguageService.Value.ParseFile(file, msgOut, ParsingMode.File, preserveComments: false);
					var strings = from statement in code
								  from node in statement.Descendants()
								  where node.Value is string && ((string)node.Value).Any(c => char.IsLetter(c))
								  select node;
					foreach (var strNode in strings)
						msgOut.Write(Severity.Note, strNode.Range, strNode.Value as string);
				}
			}
		}
		#endif
	}
}
