using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Loyc;
using Loyc.Collections;
using Loyc.LLParserGenerator;
using Loyc.Syntax;
using Loyc.Utilities;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using LeMP;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Windows.Forms;

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
	[CodeGeneratorRegistration(typeof(LeMP), "Lexical Macro Processor (C# output)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP), "Lexical Macro Processor (C# output)", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP), "Lexical Macro Processor (C# output)", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	//[ProvideObject(typeof(LeMP))]
	public class LeMP : LeMPCustomTool
	{
		protected override string DefaultExtension()
		{
			return ".cs";
		}
		protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
		{
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.PrintPlainCSharp))
				return base.Generate(inputFilePath, inputFileContents, defaultNamespace, progressCallback);
		}
	}

	// Note: the class name is used as the name of the Custom Tool from the end-user's perspective.
	[ComVisible(true)]
	[Guid("35860B1B-43E7-49F5-FC2C-DE18F30F2598")]
	[CodeGeneratorRegistration(typeof(LeMP_Ecs), "Lexical Macro Processor (EC# output)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP_Ecs), "Lexical Macro Processor (EC# output)", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP_Ecs), "Lexical Macro Processor (EC# output)", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	//[ProvideObject(typeof(LeMP_Ecs))]
	public class LeMP_Ecs : LeMPCustomTool
	{
		protected override string DefaultExtension()
		{
			return ".ecs";
		}
		protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
		{
			using (LNode.PushPrinter(Ecs.EcsNodePrinter.Printer))
				return base.Generate(inputFilePath, inputFileContents, defaultNamespace, progressCallback);
		}
	}

	[ComVisible(true)]
	[Guid("A246E3E1-BA36-40BD-804E-144A422FEF0D")]
	[CodeGeneratorRegistration(typeof(LeMP_Les), "Lexical Macro Processor (LES output)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP_Les), "Lexical Macro Processor (LES output)", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LeMP_Les), "Lexical Macro Processor (LES output)", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	//[ProvideObject(typeof(LeMP_Les))]
	public class LeMP_Les : LeMPCustomTool
	{
		protected override string DefaultExtension()
		{
			return ".les";
		}
		protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
		{
			using (LNode.PushPrinter(Loyc.Syntax.Les.LesNodePrinter.Printer))
				return base.Generate(inputFilePath, inputFileContents, defaultNamespace, progressCallback);
		}
	}

	// Note: the class name is used as the name of the Custom Tool from the end-user's perspective.
	[ComVisible(true)]
	[Guid("91585B26-E0B4-4BEE-B4A5-56FD529B9157")]
	[CodeGeneratorRegistration(typeof(LLLPG), "LL(k) Parser Generator (C# output)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LLLPG), "LL(k) Parser Generator (C# output)", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LLLPG), "LL(k) Parser Generator (C# output)", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	//[ProvideObject(typeof(LLLPG))]
	public class LLLPG : LeMP
	{
		public override void Configure(global::LeMP.Compiler c)
		{
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLPG"));
			base.Configure(c);
		}
	}

	[ComVisible(true)]
	[Guid("01D3BAE6-ED5F-4FDB-AA7A-9D37ED878E02")]
	[CodeGeneratorRegistration(typeof(LLLPG_Les), "LL(k) Parser Generator (LES output)", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LLLPG_Les), "LL(k) Parser Generator (LES output)", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
	[CodeGeneratorRegistration(typeof(LLLPG_Les), "LL(k) Parser Generator (LES output)", vsContextGuids.vsContextGuidVJSProject, GeneratesDesignTimeSource = true)]
	//[ProvideObject(typeof(LLLPG_Les))]
	public class LLLPG_Les : LeMP_Les
	{
		public override void Configure(global::LeMP.Compiler c)
		{
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("Loyc.LLPG"));
			base.Configure(c);
		}
	}

	public abstract class LeMPCustomTool : CustomToolBase
	{
		public LeMPCustomTool() { } // exists for a breakpoint

		protected override abstract string DefaultExtension();

		class Compiler : global::LeMP.Compiler
		{
			public Compiler(IMessageSink sink, InputOutput file)
				: base(sink, typeof(global::LeMP.Prelude.Macros), new [] { file }) { }

			public StringBuilder Output = new StringBuilder();
			public bool NoOutHeader;

			protected override void WriteOutput(InputOutput io)
			{
				RVList<LNode> results = io.Output;
				var printer = LNode.Printer;
				if (!NoOutHeader)
					Output.AppendFormat(
						"// Generated from {1} by LeMP custom tool. LLLPG version: {2}{0}"
						+ "// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':{0}"
						+ "// --macros=FileName.dll Load macros from FileName.dll, path relative to this file {0}"
						+ "// --verbose             Allow verbose messages (shown as 'warnings'){0}"
						+ "// --no-out-header       Suppress this message{0}", NewlineString, 
						Path.GetFileName(io.FileName), typeof(Rule).Assembly.GetName().Version.ToString());
				foreach (LNode node in results)
				{
					printer(node, Output, Sink, null, IndentString, NewlineString);
					Output.Append(NewlineString);
				}
			}
		}

		public virtual void Configure(global::LeMP.Compiler c)
		{
			c.AddMacros(typeof(Loyc.LLPG.Macros).Assembly);
			c.AddMacros(typeof(global::LeMP.Prelude.Macros).Assembly);
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP"));
			c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude"));
		}

		protected override byte[] Generate(string inputFilePath, string inputFileContents, string defaultNamespace, IVsGeneratorProgress progressCallback)
		{
			string oldCurDir = Environment.CurrentDirectory;
			try {
				string inputFolder = Path.GetDirectoryName(inputFilePath);
 				Environment.CurrentDirectory = inputFolder; // --macros should be relative to file being processed

				var options = new BMultiMap<string, string>();
				var argList = G.SplitCommandLineArguments(defaultNamespace);
				UG.ProcessCommandLineArguments(argList, options, "", LeMP.Compiler.ShortOptions, LeMP.Compiler.TwoArgOptions);

				string _;
				var KnownOptions = LeMP.Compiler.KnownOptions;
				if (options.TryGetValue("help", out _) || options.TryGetValue("?", out _))
					LeMP.Compiler.ShowHelp(KnownOptions);
				
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
				MessageHolder innerSink = new MessageHolder();
				
				// Block verbose messages when --verbose is not specified
				Severity sev = Severity.Note;
				string value;
				if (options.TryGetValue("verbose", out value) && (value != "false")) {
					if (!Enum.TryParse(value, out sev))
						sev = Severity.Verbose;
				}
				var sink = new SeverityMessageFilter(innerSink, sev);

				var sourceFile = new InputOutput((StringSlice)inputFileContents, inputFilePath);

				var c = new Compiler(sink, sourceFile);
				c.Parallel = false; // only one file, parallel doesn't help

				if (LeMP.Compiler.ProcessArguments(c, options)) {
					if (options.ContainsKey("no-out-header")) {
						options.Remove("no-out-header", 1);
						c.NoOutHeader = true;
					}
					LeMP.Compiler.WarnAboutUnknownOptions(options, sink, KnownOptions);
					if (c != null)
					{
						if (inputFilePath.EndsWith(".les", StringComparison.OrdinalIgnoreCase))
							c.MacroProcessor.PreOpenedNamespaces.Add(GSymbol.Get("LeMP.Prelude.Les"));
						Configure(c);
						c.Run();

						// Report errors
						foreach (var msg in innerSink.List)
							ReportErrorToVS(progressCallback, msg.Severity, msg.Context, msg.Format, msg.Args);

						return Encoding.UTF8.GetBytes(c.Output.ToString());
					}
				}
				return null;
			} finally {
				Environment.CurrentDirectory = oldCurDir;
			}
		}

		void ReportErrorToVS(IVsGeneratorProgress generatorProgress, Severity severity, object context, string message, object[] args)
		{
			int line = 0, col = 1;
			string message2 = Localize.From(message, args);
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
			} else
				message2 = MessageSink.LocationString(context) + ": " + message2;

			bool subwarning = severity < Severity.Warning;
			int n = subwarning ? 2 : severity == Severity.Warning ? 1 : 0;
			generatorProgress.GeneratorError(n, 0u, message2, (uint)line - 1u, (uint)col - 1u);
		}
	}
}

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
				var parsed = Ecs.Parser.EcsLanguageService.Value.Parse(testCode);
				string printed = Ecs.Parser.EcsLanguageService.Value.Print(parsed.First());
				Loyc.MiniTest.Assert.AreEqual(testCode, printed);
			} catch (Exception ex) {
				MessageBox.Show(Localize.From(
					"Self-test failed with the following exception:\n{0}\n{1}", 
					ex.ExceptionMessageAndType(), ex.StackTrace));
			}
		}
	}
}