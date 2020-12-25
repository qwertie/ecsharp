using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Math;
using Loyc.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

// TODO: move C#-specific macros to a more appropriate namespace
namespace LeMP
{
	using S = EcsCodeSymbols;

	partial class StandardMacros
	{
		// Note: LeMP can process multiple source files on different threads at the same time
		// TODO: this design is flawed, as we don't actually know when LeMP starts or 
		//       stops processing a file, so we don't know when to reset the session. 
		//       I think we need some kind of event system, and maybe a per-file unique ID,
		//       so sessions for different files are separate and proper cleanup can occur 
		//       at the end. In practice, this is working fine, but it's fragile.
		const string __result_of_precompute = nameof(__result_of_precompute);
		const string __macro_context = nameof(__macro_context);
		[ThreadStatic] static ScriptState<object> _roslynScriptState;
		[ThreadStatic] static StreamWriter _roslynSessionLog;
		[ThreadStatic] static string _roslynSessionLogFileName;

		// TODO: add LeMP feature for disposing/end of file/end of block
		public static void ResetRoslyn()
		{
			_roslynSessionLog.Dispose();
			_roslynScriptState = null;
			_roslynSessionLog = null;
			_roslynSessionLogFileName = null;
		}

		[LexicalMacro("compileTime { /* EC# code to run at compile time */ }",
			"Runs code at compile time using the Microsoft Roslyn interactive engine. " +
			"LeMP will preprocess the code before giving it to Roslyn, so LeMP " +
			"macros can be used inside the compileTime block. " +
			"The code block must appear at the top level of the source file, " +
			"not within other constructs (such as namespace or class). " +
			"The code inside this block will disappear from the output.", Mode = MacroMode.NoReprocessing)]
		public static LNode compileTime(LNode node, IMacroContext context) => 
			CompileTimeMacro(nameof(compileTime), node, context, false);

		[LexicalMacro("rawCompileTime { /* C# code to run at compile time */ }",
			"Like compileTime {...}, this macro runs code at compile time. This version of " +
			"the macro does not preprocess the code, so it must already be plain C# code.", Mode = MacroMode.NoReprocessing)]
		public static LNode rawCompileTime(LNode node, IMacroContext context) =>
			CompileTimeMacro(nameof(rawCompileTime), node, context, alsoRuntime: false, wantPreprocess: false);

		[LexicalMacro("compileTimeAndRuntime { /* EC# declarations available at both compile-time and runtime */ }",
			"Runs code at compile time using the Microsoft Roslyn interactive engine. " +
			"LeMP will preprocess the code before giving it to Roslyn, so LeMP " +
			"macros can be used inside the compileTimeAndRuntime block. " +
			"The code block must appear at the top level of the source file, " +
			"not within other constructs (such as namespace or class). " +
			"The code inside this block will also appear in the output so that the " +
			"declarations therein will be available at runtime.", Mode = MacroMode.NoReprocessing)]
		public static LNode compileTimeAndRuntime(LNode node, IMacroContext context) => 
			CompileTimeMacro(nameof(compileTimeAndRuntime), node, context, true);

		[LexicalMacro("rawCompileTimeAndRuntime { /* C# code to run at compile time */ }",
			"Like compileTimeAndRuntime {...}, this macro runs code at compile time and " +
			"also emits the code block to the output file. This version of the macro does not " +
			"preprocess the code, so it must already be plain C# code.", Mode = MacroMode.NoReprocessing)]
		public static LNode rawCompileTimeAndRuntime(LNode node, IMacroContext context) =>
			CompileTimeMacro(nameof(rawCompileTimeAndRuntime), node, context, alsoRuntime: true, wantPreprocess: false);

		[LexicalMacro("precompute(_expression_)",
			"Evaluates an expression at compile time using the Microsoft Roslyn interactive engine. " +
			"LeMP will preprocess the code before giving it to Roslyn, so LeMP " +
			"macros can be used inside the expression. " +
			"If the result of the expression is a Loyc.Syntax.ILNode object or a list of them, " +
			"the macro call is replaced by its output. Otherwise, the value should be a printable literal.")]
		public static LNode precompute(LNode node, IMacroContext context) =>
			PrecomputeMacro(nameof(precompute), node, context, false);

		[LexicalMacro("rawPrecompute(_expression_)",
			"Evaluates an expression at compile time using the Microsoft Roslyn interactive engine. " +
			"This is the \"raw\" version of `precompute`, which means that it does not preprocess " +
			"the code with LeMP before giving it to Roslyn. Also, if the expression returns code, that " +
			"code is not postprocessed with LeMP either. Note, however, that other macros such as "+
			"`replace` can potentially change the code before `rawPrecompute` sees it. " +
			"If the result of the expression is a Loyc.Syntax.ILNode object or a list of them, " +
			"the macro call is replaced by its output. Otherwise, the value should be a printable literal.",
			Mode = MacroMode.NoReprocessing)]
		public static LNode rawPrecompute(LNode node, IMacroContext context) =>
			PrecomputeMacro(nameof(rawPrecompute), node, context, true);

		private static LNode CompileTimeMacro(string macroName, LNode node, IMacroContext context, bool alsoRuntime, bool wantPreprocess = true)
		{
			if (node.ArgCount != 1 || !node[0].Calls(S.Braces)) {
				context.Error(node.Target, "{0} should have a single argument: a braced block.", macroName);
				return null;
			}

			LNodeList code = node[0].Args;
			if (wantPreprocess)
			{
				if (context.Ancestors.Take(context.Ancestors.Count - 1).Any(
					n => n.Name.IsOneOf(S.Class, S.Struct, S.Enum, S.Namespace)
					  || n.Name.IsOneOf(S.Constructor, S.Fn, S.Property, S.Var)))
					context.Error(node.Target, "{0} is designed for use only at the top level of the file. It will be executed as though it is at the top level: any outer scopes will be ignored.", macroName);

				code = context.PreProcess(code);
			}

			WriteHeaderCommentInSessionLog(node, context.Sink);

			// Remove namespace blocks (not supported by Roslyn scripting)
			LNode namespaceBlock = null;
			var codeSansNamespaces = code.RecursiveReplace(RemoveNamespaces);
			LNodeList? RemoveNamespaces(LNode n)
			{
				if (EcsValidators.SpaceDefinitionKind(n, out _, out _, out LNode body) == S.Namespace)
				{
					namespaceBlock = n;
					return body.Args.RecursiveReplace(RemoveNamespaces);
				}
				return null;
			}

			if (namespaceBlock != null)
				context.Warning(namespaceBlock, "The C# scripting engine does not support namespaces. They will be ignored when running at compile time.");

			RunCSharpCodeWithRoslyn(node, codeSansNamespaces, context);

			_roslynSessionLog?.Flush();
			return alsoRuntime ? F.Splice(code) : F.Splice();
		}

		private static LNode PrecomputeMacro(string macroName, LNode node, IMacroContext context, bool rawMode)
		{
			if (node.ArgCount == 1)
			{
				LNode expr = node[0];
				if (expr.Calls(S.Braces)) {
					context.Error(node.Target, "The argument to {0} should be an expression. Blocks are not currently supported.", macroName);
					return null;
				}

				if (!rawMode)
					expr = context.PreProcess(expr);

				// To avoid creating variables, run {__result_of_precompute = $expr;} instead of 
				// $expr directly. Thus an input like `precompute(dict.TryGetValue(k, out var v))` 
				// does not create a variable v. This is desirable because `precompute` may appear 
				// anywhere. From the perspective of the C# Scripting Engine there is a single 
				// scope for all script expressions, but from the user's perspective he/she would 
				// expect a new scope for each set of curly braces. It would be weird if a variable 
				// `v` was created in a nested scope and then it still existed at an outer scope. 
				// To avoid variable scoping problems, variable creation is disabled entirely. 
				// By contrast, you can create variables in `compileTime`, but to avoid confusion,
				// `compileTime` complains if the user tries to use it in a nested scope.
				LNode exprBlock = LNode.Call(S.Braces, LNode.List(
					F.Assign(F.Id(__result_of_precompute), F.Attr(F.TriviaNewline, expr))), node);

				WriteHeaderCommentInSessionLog(node, context.Sink);
				RunCSharpCodeWithRoslyn(node, LNode.List(exprBlock), context);
				object result = _roslynScriptState.GetVariable(__result_of_precompute).Value;

				if (!(result is NoValue))
				{
					LNode output;
					if (result == null)
						output = F.Null;
					else if (result is ILNode)
						output = ((ILNode)result).ToLNode();
					else if (result is IEnumerable<LNode> list)
						output = F.Splice(list);
					else if (result is IEnumerable<ILNode> list2)
						output = F.Splice(list2.Select(n => n.ToLNode()));
					else
					{
						// TODO: this won't work for arbitrary types but... 
						//       how can we even detect if it might work?
						output = F.Literal(result);
					}
					return output.IncludingTriviaFrom(node);
				}
			}
			return null;
		}

		private static void WriteHeaderCommentInSessionLog(LNode codeBlock, IMessageSink sink)
		{
			MaybeStartRoslynSession(codeBlock, sink);

			_roslynSessionLog?.WriteLine();
			_roslynSessionLog?.WriteLine("// " + "Running {0}:".Localized(codeBlock.Range.Start.ToString()));
		}

		private static void MaybeStartRoslynSession(LNode codeBlock, IMessageSink sink)
		{
			if (_roslynSessionLogFileName == null)
			{
				try
				{
					_roslynSessionLogFileName = Path.GetTempFileName();
					FileInfo fileInfo = new FileInfo(_roslynSessionLogFileName);
					fileInfo.Attributes |= FileAttributes.Temporary;
					_roslynSessionLog = File.AppendText(_roslynSessionLogFileName);
					_roslynSessionLog.WriteLine("Roslyn code execution log - {0}".Localized(DateTime.Now.ToString()));
				}
				catch (IOException e)
				{
					sink.Error(codeBlock, "Error opening Roslyn session log {0}: {1}".Localized(
						_roslynSessionLogFileName, e.Message));
				}
			}

			if (_roslynScriptState == null)
			{
				var scriptOptions =
					ScriptOptions.Default.WithReferences(
						typeof(IIsEmpty).Assembly, // Loyc.Interfaces.dll
						typeof(MessageHolder).Assembly, // Loyc.Essentials.dll
						typeof(VList<>).Assembly, // Loyc.Collections.dll
						typeof(LNode).Assembly, // Loyc.Syntax.dll
						typeof(EcsLanguageService).Assembly, // Loyc.Ecs.dll
						typeof(StandardMacros).Assembly // LeMP.StdMacros.dll
					)
					.WithImports("Loyc", "Loyc.Syntax", "LeMP", "System")
					.WithLanguageVersion(LanguageVersion.CSharp8)
					.WithEmitDebugInformation(true)
					.WithCheckOverflow(false);

				_roslynScriptState = CSharpScript.RunAsync(
					$"dynamic {__result_of_precompute};\n" +
					// Using dynamic for this variable type gave me an error when calling .RegisterMacro on it:
					// Error CS0656 Missing compiler required member 'Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create'
					$"global::LeMP.IMacroContext {__macro_context};", scriptOptions).Result;
			}
		}

		private static object RunCSharpCodeWithRoslyn(LNode parent, LNodeList code, IMacroContext context, ParsingMode printMode = null)
		{
			// Note: when using compileTimeAndRuntime, the transforms here affect the version
			//       sent to Roslyn, but not the runtime version placed in the output file.
			code = code.SmartSelectMany(stmt =>
			{
				// Ensure #r gets an absolute path; I don't know what Roslyn does with a 
				// relative path (maybe WithMetadataResolver would let me control this,
				// but it's easier not to)
				if ((stmt.Calls(S.CsiReference, 1) || stmt.Calls(S.CsiLoad, 1)) && stmt[0].Value is string fileName)
				{
					fileName = fileName.Trim().WithoutPrefix("\"").WithoutSuffix("\"");
					var inputFolder = context.ScopedProperties.TryGetValue((Symbol)"#inputFolder", "").ToString();
					var fullPath = Path.Combine(inputFolder, fileName);
					return LNode.List(stmt.WithArgChanged(0, stmt[0].WithValue("\"" + fullPath + "\"")));
				}

				// For each (top-level) LexicalMacro method, call __macro_context.RegisterMacro().
				LNode attribute = null;
				if ((attribute = stmt.Attrs.FirstOrDefault(
						attr => AppearsToCall(attr, "LeMP", nameof(LexicalMacroAttribute).WithoutSuffix("Attribute"))
						     || AppearsToCall(attr, "LeMP", nameof(LexicalMacroAttribute)))) != null
					&& EcsValidators.MethodDefinitionKind(stmt, out _, out var macroName, out _, out _) == S.Fn)
				{
					var setters = SeparateAttributeSetters(ref attribute);
					attribute = attribute.WithTarget((Symbol) nameof(LexicalMacroAttribute));
					setters.Insert(0, attribute);
					var newAttribute = F.Call(S.New, setters);
					var registrationCommand = 
						F.Call(F.Dot(__macro_context, nameof(IMacroContext.RegisterMacro)),
							F.Call(S.New, F.Call(nameof(MacroInfo), F.Null, newAttribute, macroName)));
					return LNode.List(stmt, registrationCommand);
				}
				return LNode.List(stmt);
			});

			var outputLocationMapper = new LNodeRangeMapper();
			var options = new LNodePrinterOptions { IndentString = "  ", SaveRange = outputLocationMapper.SaveRange };
			string codeText = EcsLanguageService.WithPlainCSharpPrinter.Print(code, context.Sink, printMode, options);
			_roslynSessionLog?.WriteLine(codeText);
			_roslynSessionLog?.Flush();

			_roslynScriptState.GetVariable(__macro_context).Value = context;
			try
			{
				// Allow users to write messages via MessageSink.Default
				using (MessageSink.SetDefault(new MessageSinkFromDelegate((sev, ctx, msg, args) => {
					_roslynSessionLog?.Write("{0} from user ({1}): ", sev, MessageSink.GetLocationString(ctx));
					_roslynSessionLog?.WriteLine(msg, args);
					context.Sink.Write(sev, ctx, msg, args);
				})))
				{
					_roslynScriptState = _roslynScriptState.ContinueWithAsync(codeText).Result;
				}
				return _roslynScriptState.ReturnValue;
			}
			catch (CompilationErrorException e) when (e.Diagnostics.Length > 0 && e.Diagnostics[0].Location.IsInSource)
			{
				// Determine the best location in the source code at which to report the error.
				// Keep in mind that the error may have occurred in a synthetic location not 
				// found in the original code, and we cannot report such a location.
				Microsoft.CodeAnalysis.Text.TextSpan range = e.Diagnostics[0].Location.SourceSpan;
				var errorLocation = new IndexRange(range.Start, range.Length);
				var mappedErrorLocation = outputLocationMapper.FindRelatedNodes(errorLocation, 10)
					.FirstOrDefault(p => !p.A.Range.Source.Text.IsEmpty);
				string locationCaveat = "";
				if (mappedErrorLocation.A != null)
				{
					bool mappedIsEarly = mappedErrorLocation.B.EndIndex <= errorLocation.StartIndex;
					if (mappedIsEarly || mappedErrorLocation.B.StartIndex >= errorLocation.EndIndex)
						locationCaveat = "; " +
							"The error occurred at a location ({0}) that doesn't seem to exist in the original code.".Localized(
							mappedIsEarly ? "after the location indicated".Localized()
							              : "before the location indicated".Localized());
				}

				// Extract the line where the error occurred, for inclusion in the error message
				int column = e.Diagnostics[0].Location.GetLineSpan().StartLinePosition.Character;
				int lineStart = range.Start - column;
				int lineEnd = codeText.IndexOf('\n', lineStart);
				if (lineEnd < lineStart)
					lineEnd = codeText.Length;
				string line = codeText.Substring(lineStart, lineEnd - lineStart);
				
				string errorMsg = e.Message + " - in «{0}»{1}".Localized(line, locationCaveat);
				context.Sink.Error(mappedErrorLocation.A ?? parent, errorMsg);
				LogRoslynError(e, context.Sink, parent, compiling: true);
			}
			catch (Exception e)
			{
				while (e is AggregateException ae && ae.InnerExceptions.Count == 1)
					e = ae.InnerExceptions[0];
				context.Sink.Error(parent, "An exception was thrown from your code:".Localized() + 
				                   " " + e.ExceptionMessageAndType());
				LogRoslynError(e, context.Sink, parent, compiling: false);
			}
			return NoValue.Value;
		}

		private static bool AppearsToCall(LNode attr, string optionalNamespace, string name)
		{
			// Detecting this syntactically is very clumsy, and we're assuming the namespace is imported.
			LNode target = attr.Target;
			return target != null && (
				target.IsIdNamed(name) ||
				target.Calls(S.Dot, 2) && target.Args[1].IsIdNamed(name) && (
					target.Args[0].IsIdNamed(optionalNamespace) ||
					target.Args[0].Calls(S.ColonColon, 2) && target.Args[0].Args[0].IsIdNamed("global") && target.Args[0].Args[1].IsIdNamed(optionalNamespace)
				));
		}

		// Given Foo(x, y, a = 1, c = 2), extracts { a = 1, c = 2 } to a separate list
		private static LNodeList SeparateAttributeSetters(ref LNode attribute)
		{
			LNodeList setters = LNode.List();
			while (attribute.Args.LastOrDefault()?.Calls(S.Assign) == true)
			{
				setters.Insert(0, attribute.Args.Last);
				attribute = attribute.WithArgs(attribute.Args.WithoutLast(1));
			}
			return setters;
		}

		static void LogRoslynError(Exception error, IMessageSink sink, object context, bool compiling)
		{
			if (_roslynSessionLog != null)
			{
				string action = compiling ? "compiling".Localized() : "running".Localized();
				_roslynSessionLog.WriteLine("// *** " + "An error occurred while {0} the code above.".Localized(action));
				_roslynSessionLog.WriteLine("// *** " + "Note: due to the error, any variables/types you attempted to define above won't be visible in subsequent code below.".Localized());
				_roslynSessionLog.WriteLine(error.DescriptionAndStackTrace());
				_roslynSessionLog.Flush();
				string moreInfo = "Session log at {0} contains emitted code.".Localized(_roslynSessionLogFileName);
				if (!compiling)
					moreInfo = "Stack trace: {0}".Localized(error.StackTrace) + "\n" + moreInfo;
				sink.Write(Severity.ErrorDetail, context, moreInfo);
			}
		}

	}
}
