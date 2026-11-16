// Usage (requires .NET SDK 10+; builds LeMP from the ecsharp repo next to this one):
//   dotnet run -c Release RunExamples.cs -- [--in-place] file1.md file2.md ...
// (A Debug build also works, but a failed Debug.Assert in LeMP terminates the process.)
#:project ../../ecsharp/Main/LeMP/LeMP.csproj
#:property TargetFramework=net10.0
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeMP;
using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Syntax;

class Program
{
  public static void Main(string[] args)
  {
    bool inPlace = args.Contains("--in-place");
    args = args.Where(a => a != "--in-place").ToArray();
    if (args.Length == 0) {
      Console.WriteLine("Usage: dotnet run -c Release RunExamples.cs -- [--in-place] file1.md file2.md ...");
      Console.WriteLine("Scans markdown files for code blocks to execute in LeMP between `~~~exec` and `~~~`,");
      Console.WriteLine("and places the output side-by-side (assuming there is a CSS class 'sbs' to provide");
      Console.WriteLine("this effect). It also re-runs the first code block in existing pairs.");
      Console.WriteLine("Each example is processed like an .ecs file converted to .out.cs by LeMP.exe.");
      Console.WriteLine("Without --in-place, output is written to file.md.out");
      return;
    }

    foreach (var filename in args)
    {
      try {
        var code = File.ReadAllLines(filename);
        int numProcessed;
        using (MessageSink.SetDefault(ConsoleMessageSink.Value)) {
          MessageSink.Default.Write(Severity.Note, "", "Starting {0}", filename);
          var output = TransformFile(Path.GetFileName(filename), code, out numProcessed);
          if (output == null)
            Console.WriteLine("Processing failed");
          else if (numProcessed == 0)
            Console.WriteLine("No code blocks found marked ~~~exec.");
          else {
            Console.WriteLine("Processed {0} code blocks.", numProcessed);
            var outFileName = inPlace ? filename : filename + ".out";
            Console.WriteLine(" ... Writing {0} ...", outFileName);
            File.WriteAllText(outFileName, output + "\n");
          }
        }
      } catch (IOException e) {
        Console.WriteLine(e.DescriptionAndStackTrace());
      }
    }
  }

  const string OutputComment = "// Output of LeMP";

  private static string TransformFile(string fileName, string[] input_, out int numProcessed)
  {
    List<string> output = new List<string>();
    numProcessed = 0;
    var input = new ArraySlice<string>(input_);
    while (input.Count > 0) {
      int index = input_.Length - input.Count;
      bool expectOutputBlock = input_.TryGet(index - 1, "").StartsWith("<div class='sbs'");
      if ((input[0] == "~~~exec") || expectOutputBlock && input[0] == "~~~csharp") {
        if (!expectOutputBlock)
          output.Add("<div class='sbs' markdown='1'>");
        if (ProcessCodeBlock(fileName, ref input, output, expectOutputBlock, index + 1))
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

  private static bool ProcessCodeBlock(string fileName, ref ArraySlice<string> input, List<string> output, bool expectOutputBlock, int inputLine0)
  {
    var sink = MessageSink.Default;
    output.Add("~~~csharp");

    int numInputLines = input.Slice(1).FirstIndexWhere(s => s == "~~~") ?? -1;
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
      int numOutputLines = input.Slice(2).FirstIndexWhere(s => s == "~~~") ?? -1;
      if (numOutputLines == -1) {
        sink.Write(Severity.Error, "Line " + (line0 + 1), "No matching end marker ~~~ on output code");
        return false;
      }
      input = input.Slice(numOutputLines + 3);
    }

    // Process input with LEMP! Messages refer to a virtual file named after the
    // example's first line in the markdown file, e.g. "ref-other.md line 375.ecs"
    var outputCode = Process(inputLines.Join("\n"), $"{fileName} line {inputLine0 + 1}.ecs");
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

  /// <summary>Runs one example through the same pipeline as LeMP.exe, as if the example
  /// were an .ecs file being converted to .out.cs. Returns null if an error occurred.</summary>
  private static string Process(string code, string virtualFileName)
  {
    var msgs = new MessageHolder();
    var compiler = new ExampleCompiler(msgs, code, virtualFileName);
    // LeMP.exe does this too; it affects macros such as stringify and notnull
    using (LNode.SetPrinter(EcsLanguageService.WithPlainCSharpPrinter))
      compiler.Run();
    foreach (var m in msgs.List)
      if (m.Severity > Severity.Verbose)
        m.WriteTo(ConsoleMessageSink.Value);
    return msgs.List.Any(m => m.Severity >= Severity.Error) ? null : compiler.Output;
  }

  /// <summary>A LeMP <see cref="Compiler"/> that processes a single in-memory file and
  /// captures the output as a string, indented with two spaces instead of tabs.</summary>
  class ExampleCompiler : Compiler
  {
    public string Output;

    public ExampleCompiler(IMessageSink sink, string code, string fileName)
      : base(sink, typeof(LeMP.Prelude.BuiltinMacros), new[] { new InputOutput(new UString(code), fileName) })
    {
      Parallel = false;
      OutExt = ".out.cs";
      OutOptions.IndentString = "  ";
      AddStdMacros();
    }

    protected override void WriteOutput(InputOutput io)
    {
      Output = io.OutPrinter.Print(io.Output, Sink, null, io.OutOptions);
    }
  }
}
