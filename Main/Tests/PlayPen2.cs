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

namespace Samples
{
	partial class PlayPen 
	{
		public static void PrintAllTheNames(string path)
		{
			using (ParsingService.PushCurrent(EcsLanguageService.Value))
			using (MessageSink.PushCurrent(MessageSink.Console))
				foreach (var filename in Directory.GetFiles(path, "*.cs")) {
					Console.WriteLine(filename);
					foreach (var stmt in EcsLanguageService.Value.ParseFile(filename))
						stmt.ReplaceRecursive(node => {
							var name = GetName(node);
							if (name != null)
								MessageSink.Current.Write(Severity.Note, node, 
									"Found {0} named {1}", node.Name, name);
							return null; // do not change anything
						});
				}
		}
	}
}
