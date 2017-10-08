using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Forms;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Utilities;
using Loyc.Ecs;
using Loyc.LLParserGenerator;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using LeMP;
using Loyc.Syntax.Les;

namespace Loyc.VisualStudio
{
	public static class vsContextGuids
	{
		public const string vsContextGuidVCSProject = "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}";
		public const string vsContextGuidVCSEditor = "{694DD9B6-B865-4C5B-AD85-86356E9C88DC}";
		public const string vsContextGuidVBProject = "{164B10B9-B200-11D0-8C61-00A0C91E29D5}";
		public const string vsContextGuidVBEditor = "{E34ACDC0-BAAE-11D0-88BF-00A0C9110049}";
		public const string vsContextGuidVJSProject = "{E6FDF8B0-F3D1-11D4-8576-0002A516ECE8}";
		public const string vsContextGuidVJSEditor = "{E6FDF88A-F3D1-11D4-8576-0002A516ECE8}";
	}

	// Note: the class name is used as the name of the Custom Tool from the end-user's perspective.
	[ComVisible(true)]
	[Guid("3583EDC5-48F7-49F5-8502-DE18F30F9825")]
	[CodeGeneratorRegistration(typeof(LeMP), "Lexical Macro Processor", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP), "Lexical Macro Processor", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP), "Lexical Macro Processor", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	[ProvideObject(typeof(LeMP), RegisterUsing = RegistrationMethod.CodeBase)]
	public class LeMP : LeMPCustomTool
	{
		protected override string DefaultExtension()
		{
			return _requestedExtension ?? ".out.cs";
		}
		protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
		{
			using (LNode.SetPrinter(EcsLanguageService.WithPlainCSharpPrinter))
				return base.Generate(inputFilePath, inputFileContents, defaultNamespace, progressCallback);
		}
	}

	// Note: the class name is used as the name of the Custom Tool from the end-user's perspective.
	[ComVisible(true)]
	[Guid("91585B26-E0B4-4BEE-B4A5-56FD529B9157")]
	[CodeGeneratorRegistration(typeof(LLLPG), "LL(k) Parser Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LLLPG), "LL(k) Parser Generator", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LLLPG), "LL(k) Parser Generator", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	[ProvideObject(typeof(LLLPG), RegisterUsing = RegistrationMethod.CodeBase)]
	public class LLLPG : LeMP
	{
		public override void Configure(global::LeMP.Compiler c)
		{
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLPG"));
			base.Configure(c);
		}
	}

	[ComVisible(true)]
	public abstract class LeMPCustomTool : CustomToolBase
	{
		public LeMPCustomTool() { } // exists for a breakpoint

		protected string _requestedExtension;
		protected override abstract string DefaultExtension();

		class Compiler : global::LeMP.Compiler
		{
			public Compiler(IMessageSink sink, InputOutput file)
				: base(sink, typeof(global::LeMP.Prelude.BuiltinMacros), new [] { file }) { }

			public StringBuilder Output = new StringBuilder();
			public bool NoOutHeader;

			protected override void WriteOutput(InputOutput io)
			{
				VList<LNode> results = io.Output;
				if (!NoOutHeader)
					Output.AppendFormat(
						"// Generated from {1} by LeMP custom tool. LeMP version: {2}{0}"
						+ "// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':{0}"
						+ "// --no-out-header       Suppress this message{0}"
						+ "// --verbose             Allow verbose messages (shown by VS as 'warnings'){0}"
						+ "// --timeout=X           Abort processing thread after X seconds (default: 10){0}"
						+ "// --macros=FileName.dll Load macros from FileName.dll, path relative to this file {0}"
						+ "// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);{0}", NewlineString, 
						Path.GetFileName(io.FileName), typeof(MacroProcessor).Assembly.GetName().Version.ToString());
				var options = new LNodePrinterOptions {
					IndentString = IndentString, NewlineString = NewlineString
				};
				LNode.Printer.Print(results, Output, Sink, ParsingMode.File, options);
			}
		}

		public virtual void Configure(global::LeMP.Compiler c)
		{
			c.AddMacros(typeof(Loyc.LLPG.Macros).Assembly); // LLLPG.exe
		}

		protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
		{
			string oldCurDir = Environment.CurrentDirectory;
			try {
				string inputFolder = Path.GetDirectoryName(inputFilePath);
 				Environment.CurrentDirectory = inputFolder; // --macros should be relative to file being processed

				// Originally I wrote a conversion from IVsGeneratorProgress to
 				// IMessageSink so that errors could be reported immediately and
				// directly to Visual Studio. This broke in a bizarre way when I
				// added processing on a separate thread (in order to be able to
				// abort the thread if it runs too long); I got the following
				// InvalidCastException: "Unable to cast COM object of type 'System.__ComObject' 
				// to interface type 'Microsoft.VisualStudio.Shell.Interop.IVsGeneratorProgress'.
				// This operation failed because the QueryInterface call on the COM component for 
				// the interface with IID '{BED89B98-6EC9-43CB-B0A8-41D6E2D6669D}' failed due to 
				// the following error: No such interface supported (Exception from HRESULT: 
				// 0x80004002 (E_NOINTERFACE))."
				// 
				// A simple solution is to store the messages rather than reporting
				// them immediately. I'll report the errors at the very end.
				MessageHolder sink = new MessageHolder();
				
				var sourceFile = new InputOutput((UString)inputFileContents, inputFilePath);

				Compiler.KnownOptions["no-out-header"] = Pair.Create("", "Remove explanatory comment from output file");
				Compiler.KnownOptions.Remove("parallel");   // not applicable to single file
				Compiler.KnownOptions.Remove("noparallel"); // not applicable to single file

				var c = new Compiler(sink, sourceFile) { 
					AbortTimeout = TimeSpan.FromSeconds(10),
					Parallel = false // only one file, parallel doesn't help
				};

				var argList = G.SplitCommandLineArguments(defaultNamespace);
				var options = c.ProcessArguments(argList, true, false);
				// Note: if default namespace is left blank, VS uses the namespace 
				// from project settings. Don't show an error in that case.
				if (argList.Count > 1 || (argList.Count == 1 && options.Count > 0))
					sink.Write(Severity.Error, "Command line", "'{0}': expected options only (try --help).", argList[0]);

				string _;
				if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _))
				{
					var ms = new MemoryStream();
					LeMP.Compiler.ShowHelp(LeMP.Compiler.KnownOptions, new StreamWriter(ms), false);
					return ms.GetBuffer();
				}

				LeMP.Compiler.WarnAboutUnknownOptions(options, sink, LeMP.Compiler.KnownOptions);
				
				if (options.ContainsKey("no-out-header"))
					c.NoOutHeader = true;
				
				if (c.InLang == LesLanguageService.Value || inputFilePath.EndsWith(".les", StringComparison.OrdinalIgnoreCase))
					c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
						
				Configure(c);
				_requestedExtension = c.OutExt;
				c.Run();

				// Report errors
				foreach (var msg in sink.List)
					ReportErrorToVS(progressCallback, msg.Severity, msg.Context, msg.Format, msg.Args);

				return Encoding.UTF8.GetBytes(c.Output.ToString());
			} finally {
				Environment.CurrentDirectory = oldCurDir;
			}
		}

		void ReportErrorToVS(IVsGeneratorProgress generatorProgress, Severity severity, object context, string message, object[] args)
		{
			int line = 0, col = 1;
			string message2 = message.Localized(args);
			if (context is LNode) {
				var range = ((LNode)context).Range;
				line = range.Start.Line;
				col = range.Start.PosInLine;
			} else if (context is SourcePos) {
				line = ((SourcePos)context).Line;
				col = ((SourcePos)context).PosInLine;
			} else if (context is SourceRange) {
				line = ((SourceRange)context).Start.Line;
				col = ((SourceRange)context).Start.PosInLine;
			} else if (context is Pred) {
				line = ((Pred)context).Basis.Range.Start.Line;
				col = ((Pred)context).Basis.Range.Start.PosInLine;
			} else
				message2 = MessageSink.ContextToString(context) + ": " + message2;

			bool subwarning = severity < Severity.Warning;
			int n = subwarning ? 2 : severity == Severity.Warning ? 1 : 0;
			generatorProgress.GeneratorError(n, 0u, message2, (uint)line - 1u, (uint)col - 1u);
		}
	}
}

#if false
namespace SingleFileGenerator
{
	public partial class InstallForm
	{
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			
			// This is a simple self test to make sure there's no DLLs version mismatches.
			try {
				var testCode = @"Hello(""World!"");";
				var parsed = EcsLanguageService.Value.Parse(testCode);
				string printed = EcsLanguageService.Value.Print(parsed.First());
				Loyc.MiniTest.Assert.AreEqual(testCode, printed);
			} catch (Exception ex) {
				MessageBox.Show(
					string.Format("Self-test failed with the following exception:\n{0}\n{1}",
						ex.ExceptionMessageAndType(), ex.StackTrace));
			}
		}
	}
}
#endif