//css_ref ../../ecsharp/Lib/LeMP/Loyc.Essentials.dll;
//css_ref ../../ecsharp/Lib/LeMP/Loyc.Collections.dll;
//css_ref ../../ecsharp/Lib/LeMP/Loyc.Syntax.dll;
//css_ref ../../ecsharp/Lib/LeMP/Loyc.Ecs.dll;
//css_ref ../../ecsharp/Lib/LeMP/LeMP.exe;
//css_ref ../../ecsharp/Lib/LeMP/LeMP.StdMacros.dll;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Syntax;

namespace LeMPExampleRunner
{
	class Program
	{
		static MacroProcessor MP = new LeMP.MacroProcessor(typeof(LeMP.Prelude.BuiltinMacros), MessageSink.Console);

		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine("This program scans file for code blocks to execute in LeMP ");
			Console.WriteLine("between `~~~exec`and `~~~`, and places the output side-by-side ");
			Console.WriteLine("(assuming there is a CSS class 'sbs' to provide this effect). ");
			Console.WriteLine("It also re-runs the first code block in existing pairs.");
			
			if (args.Length == 0) {
				var ofd = new OpenFileDialog
				{
					Title = "Select markdown files with code blocks to process with LeMP",
					Filter = "Markdown files (*.md)|*.md|All files|*.*",
					Multiselect = true
				};
				if (ofd.ShowDialog() != DialogResult.OK)
					return;
				args = ofd.FileNames;
			}

			Console.WriteLine("Modify files in-place? (Y/N) ");
			bool inPlace = false;
			while (true) {
				var k = Console.ReadKey(false);
				if ((inPlace = k.Key == ConsoleKey.Y) || k.Key == ConsoleKey.N)
					break;
			}

			MP.AddMacros(typeof(LeMP.StandardMacros).Assembly);
			MP.PreOpenedNamespaces.Add((Symbol) "LeMP");
			MP.PreOpenedNamespaces.Add((Symbol) "LeMP.Prelude");

			foreach (var filename in args)
			{
				try {
					var code = File.ReadAllLines(filename);
					int numProcessed;
					using (MessageSink.PushCurrent(MessageSink.Console)) {
						MessageSink.Current.Write(Severity.Note, "", "Starting {0}", filename);
						var output = TransformFile(MP, code, out numProcessed);
						if (output == null)
							Console.WriteLine("Processing failed");
						else if (numProcessed == 0)
							Console.WriteLine("No code blocks found marked ~~~exec.");
						else {
							Console.WriteLine("Processed {0} code blocks.", numProcessed);
							var outFileName = inPlace ? filename : filename + ".out";
							Console.WriteLine(" ... Writing {0} ...", outFileName);
							File.WriteAllText(outFileName, output);
						}
					}
				} catch (IOException e) {
					Console.WriteLine(e.DescriptionAndStackTrace());
				}
			}
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey(true);
		}

		const string OutputComment = "// Output of LeMP";

		private static string TransformFile(MacroProcessor MP, string[] input_, out int numProcessed)
		{
			List<string> output = new List<string>();
			numProcessed = 0;
			var input = input_.Slice(0);
			while (input.Count > 0) {
				int index = input_.Length - input.Count;
				bool expectOutputBlock = input_.TryGet(index - 1, "").StartsWith("<div class='sbs'");
				if ((input[0] == "~~~exec") || expectOutputBlock && input[0] == "~~~csharp") {
					if (!expectOutputBlock)
						output.Add("<div class='sbs' markdown='1'>");
					if (ProcessCodeBlock(ref input, output, expectOutputBlock, index + 1))
						numProcessed++;
					else
						return null;
					if (!expectOutputBlock)
						output.Add("</div>");
				} else {
					output.Add(input[0]);
					input = input.Slice(1);
				}
			}
			return output.Join("\n");
		}

		private static bool ProcessCodeBlock(ref ArraySlice<string> input, List<string> output, bool expectOutputBlock, int inputLine0)
		{
			var sink = MessageSink.Current;
			output.Add("~~~csharp");

			int numInputLines = input.Slice(1).IndexWhere(s => s == "~~~");
			if (numInputLines == -1) {
				sink.Write(Severity.Error, "Line " + inputLine0, "No matching end marker ~~~ for input code");
				return false;
			}

			var inputLines = input.Slice(1, numInputLines);
			output.AddRange(inputLines);
			output.Add(input[1 + numInputLines]); // ~~~
			input = input.Slice(1 + numInputLines + 1);
			
			if (expectOutputBlock)
			{
				int line0 = inputLine0 + numInputLines + 2;
				// Ignore existing output block by skipping past it...
				// Just make sure the block exists and has a "Output of LeMP" comment
				if (input[0] != "" || input[1] != "~~~csharp") {
					sink.Write(Severity.Error, "Line " + line0, "Output code block is missing, so cannot replace it");
					return true;
				}
				if (input[2] != OutputComment) {
					sink.Write(Severity.Note, "Line " + (line0 + 1), "Leaving customized output block unchanged");
					return true;
				}
				int numOutputLines = input.Slice(2).IndexWhere(s => s == "~~~");
				if (numOutputLines == -1) {
					sink.Write(Severity.Error, "Line " + (line0 + 1), "No matching end marker ~~~ on output code");
					return false;
				}
				input = input.Slice(numOutputLines + 3);
			}

			// Process input with LEMP!
			var outputCode = Process(MP, inputLines.Join("\n"), inputLine0);
			if (outputCode == null)
				return false;
			if (outputCode.EndsWith("\n")) // remove extra newline
				outputCode = outputCode.Substring(0, outputCode.Length - 1);

			output.Add("");
			output.Add("~~~csharp");
			output.Add(OutputComment);
			output.Add(outputCode);
			output.Add("~~~");
			return true;
		}

		private static string Process(MacroProcessor MP, string codeStr, int lineZero)
		{
			VList<LNode> output = VList<LNode>.Empty;
			var msgs = new MessageHolder();
			using (MessageSink.PushCurrent(msgs))
			{
				var code = EcsLanguageService.Value.Parse(codeStr, msgs);
				if (!msgs.List.Any(m => m.Severity >= Severity.Error))
					output = MP.ProcessSynchronously(LNode.List(code));
			}
			foreach (var m in msgs.List)
			{
				// Goal: add lineNo to line number of error message to get correct line number
				object loc = MessageSink.LocationOf(m.Context);
				SourcePos pos = default(SourcePos);
				if (loc is SourceRange) pos = ((SourceRange)loc).Start;
				else if (loc is SourcePos) pos = ((SourcePos)loc);
				else pos = new SourcePos("Unknown", 0, 0);
				pos = new SourcePos(pos.FileName, pos.Line + lineZero, pos.PosInLine);

				MessageSink.Current.Write(m.Severity, pos, m.Format, m.Args);
			}
			if (msgs.List.Any(m => m.Severity >= Severity.Error))
				return null;
			else
				return EcsLanguageService.Value.Print(output, null, null, "  ", "\n");
		}
	}
}
