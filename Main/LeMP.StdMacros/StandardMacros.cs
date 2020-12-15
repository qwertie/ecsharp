using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Ecs;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	/// <summary>
	/// Standard macros, such as unroll() and replace() that can work in all Loyc languages.
	/// Also includes macros designed to convert EC#-only features to C# (e.g. quick::binding).
	/// </summary>
	[ContainsMacros]
	public partial class StandardMacros
	{
		public static Symbol NextTempName(IMacroContext ctx, string prefix = "tmp_")
		{
			return GSymbol.Get(prefix + ctx.IncrementTempCounter());
		}
		public static Symbol NextTempName(IMacroContext ctx, LNode value)
		{
			string prefix = value.Name.Name;
			prefix = EcsValidators.IsPlainCsIdentifier(prefix) ? prefix + "_" : "tmp_";
			return NextTempName(ctx, prefix);
		}
		static LNode TempVarDecl(IMacroContext ctx, LNode value, out LNode tmpVarName)
		{
			tmpVarName = LNode.Id(NextTempName(ctx, value), value);
			return F.Var(F.Missing, tmpVarName, value);
		}
		static LNode TempVarDecl(IMacroContext ctx, LNode value, out LNode tmpVarName, string prefix)
		{
			tmpVarName = LNode.Id(NextTempName(ctx, prefix), value);
			return F.Var(F.Missing, tmpVarName, value);
		}

		static LNodeFactory F = new LNodeFactory(new EmptySourceFile("StandardMacros.cs"));

		static LNode Reject(IMessageSink sink, LNode at, string msg)
		{
			sink.Write(Severity.Error, at, msg);
			return null;
		}

		// Used to avoid evaluating `value` more than once by creating a 
		// declaration in `output` of a temporary variable to hold the value. 
		// If `value` looks simple (according to LooksLikeSimpleValue), this 
		// fn returns value and leaves output unchanged.
		protected internal static LNode MaybeAddTempVarDecl(IMacroContext ctx, LNode value, WList<LNode> output)
		{
			if (!LooksLikeSimpleValue(value)) {
				LNode tmpId;
				output.Add(TempVarDecl(ctx, value, out tmpId));
				return tmpId;
			}
			return value;
		}
		// Decides whether a value should be placed in a temporary variable
		// if it is evaluated more than once by a macro. This returns true for 
		// non-uppercase identifiers like "x" or "_Foo", for literals like 42,
		// and for dotted expressions in which all components are non-uppercase 
		// identifiers, e.g. foo.bar.baz. Returns false for everything else.
		internal protected static bool LooksLikeSimpleValue(LNode value)
		{
			if (value.IsCall) {
				if (value.Calls(S.Dot)) {
					if (value.ArgCount == 1)
						value = value.Args[0];
					else if (value.ArgCount == 2) {
						if (!LooksLikeSimpleValue(value.Args[0]))
							return false;
						value = value.Args[1];
					} else
						return false;
				} else
					return false;
			}
			if (value.IsId)
				return !char.IsUpper(value.Name.Name.TryGet(0, '\0'));
			if (value.IsLiteral)
				return true;
			return false;
		}

		/// <summary>Given a statement, this method attempts to decide if the 
		/// immediately following statement (if any) is reachable.</summary>
		/// <returns>true if reachable/unsure, false if definitely unreachable.</returns>
		/// <remarks>
		/// The goal of this code is to avoid the dreaded compiler warning 
		/// "Unreachable code detected". This is just a heuristic since we 
		/// don't have access to proper reachability analysis. In fact, it's
		/// no doubt buggy.
		/// </remarks>
		public static bool NextStatementMayBeReachable(LNode stmt)
		{
			if (stmt.CallsMin(S.Braces, 1))
				return NextStatementMayBeReachable(stmt.Args.Last);
			if (!stmt.HasSpecialName)
				return true;

			if (stmt.Calls(S.Goto, 1))
				return false;
			else if (stmt.Calls(S.Continue) || stmt.Calls(S.Break))
				return false;
			else if (stmt.Calls(S.Return))
				return false;
			else if (stmt.Calls(S.GotoCase, 1))
				return false;

			bool isFor;
			LNode body;
			if (stmt.Calls(S.If, 2))
				return true;
			else if (stmt.Calls(S.If, 3))
			{
				var r1 = NextStatementMayBeReachable(stmt.Args[1]);
				var r2 = NextStatementMayBeReachable(stmt.Args[2]);
				return r1 || r2;
			}
			else if (stmt.CallsMin(S.SwitchStmt, 2) && (body = stmt.Args[1]).CallsMin(S.Braces, 2))
			{
				// for a switch statement, assume it exits normally if a break 
				// statement is the last statement of any of the cases, or if
				// there is no "default" case.
				bool beforeCase = true;
				bool hasDefaultCase = false;
				foreach (var substmt in body.Args.ToFVList())
				{
					if (beforeCase && substmt.Calls(S.Break))
						return true;
					if (substmt.Calls(S.Label, 1) && substmt.Args[0].IsIdNamed(S.Default))
						hasDefaultCase = beforeCase = true;
					else
						beforeCase = substmt.Calls(S.Case);
				}
				return hasDefaultCase == false;
			}
			else if ((isFor = stmt.Calls(S.For, 4)) || stmt.Calls(S.While, 2) || stmt.Calls(S.DoWhile, 2))
			{   // Infinite loop?
				var cond = stmt.Args[isFor ? 1 : 0];
				if (cond.IsIdNamed(S.Missing) || true.Equals(cond.Value))
					return true; // ok, I don't know what to do
				return true;
			}
			else if (stmt.CallsMin(S.Try, 1))
			{
				return NextStatementMayBeReachable(stmt.Args[0]);
			}
			else if (stmt.ArgCount >= 1)
			{
				Debug.Assert(stmt.HasSpecialName);
				return NextStatementMayBeReachable(stmt.Args.Last);
			}
			return true;
		}

		#region concatId (##), nameof

		[LexicalMacro(@"a `##` b; concatId(a, b)", 
			"Concatenates identifiers and/or literals to produce an identifier. For example, the output of ``a `##` b`` is `ab`.\n"
			+"\n**Note**: concatId cannot be used directly as a variable or method name unless you use `$(out concatId(...))`.", 
			"##", "concatId")]
		public static LNode concatId(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			if (args.Count == 0)
				return null;
			if (args.Slice(0, args.Count - 1).Any(n => n.IsCall))
				return Reject(sink, node, "All arguments to ##() or concat() must be identifiers or literals (except the last one)");

			LNodeList attrs = node.Attrs;
			LNode arg = null;
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < args.Count; i++)
			{
				arg = args[i];
				attrs.AddRange(arg.Attrs);

				if (arg.IsLiteral)
					sb.Append(arg.Value ?? "null");
				else if (arg.IsId)
					sb.Append(arg.Name);
				else { // call
					if (i + 1 != args.Count || !arg.HasSimpleHead())
						return Reject(sink, arg, "Expected simple identifier or literal");
					sb.Append(arg.Name);
				}
			}
			Symbol combined = GSymbol.Get(sb.ToString());
			LNode result;
			if (arg.IsCall)
				result = arg.WithTarget(combined);
			else
				result = LNode.Id(combined, node);
			return result.WithAttrs(attrs);
		}

		/// <summary>Retrieves the "key" name component for the nameof(...) macro.</summary>
		/// <remarks>
		/// The key name component of <c>global::Foo!int.Bar!T(x)</c> (in C# notation
		/// global::Foo&lt;int>.Bar&lt;T>(x)) is <c>Bar</c>. This example tree has the 
		/// structure <c>((((global::Foo)!int).Bar)!T)(x)</c>).
		/// </remarks>
		public static Symbol KeyNameComponentOf(LNode name)
		{
			return EcsValidators.KeyNameComponentOf(name);
		}

		[LexicalMacro(@"stringify(expr)", "Converts an expression to a string (note: original formatting is not preserved)")]
		public static LNode stringify(LNode node, IMacroContext context)
		{
			if (node.ArgCount != 1)
				return null; // reject
			return F.Literal(LNode.Printer.Print(node.Args[0], context.Sink, ParsingMode.Expressions));
		}
		
		#endregion

		[LexicalMacro("#ecs;", "Typically used at the top of a file, this macro enable certain EC# features before the EC# compiler is written, by implementing those features as macro. "
			+"Currently, `#ecs` expands to `#useSymbols; #useSequenceExpressions`.", "#ecs", Mode = MacroMode.MatchIdentifierOnly)]
		public static LNode ecs(LNode node, IMacroContext context)
		{
			return F.Call(S.Splice, F.Id("#useSymbols"), F.Id("#useSequenceExpressions"));
		}

		[LexicalMacro(@"x `code==` y", 
			 "Returns the literal true if two or more syntax trees are equal, or false if not. "
			+"The two arguments are macro-preprocessed, and trivia is ignored.", 
			"code==", "'code==", "codeEquals", "'codeEquals")]
		public static LNode codeEquals(LNode node, IMacroContext context)
		{
			if (node.ArgCount < 2) return null;
			node = context.PreProcessChildren();
			var args = node.Args;

			LNode left = args[0];
			for (int i = 1; i < args.Count; i++)
				if (!left.Equals(args[i], LNode.CompareMode.IgnoreTrivia))
					return F.Literal(G.BoxedFalse);
			return F.Literal(G.BoxedTrue);
		}

		[LexicalMacro(@"static if(cond) { then; } else { otherwise; }", 
			"Equivalent to `static_if(cond, then, otherwise)` but with friendly C# syntax. See documentation of `static_if` for more information.",
			"#if", Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode StaticIf(LNode @if, IMacroContext context)
		{
			LNode @static;
			if ((@static = @if.AttrNamed(S.Static)) == null || !@static.IsId)
				return null;
			return static_if(@if, context);
		}
		
		[LexicalMacro(@"static_if(cond, then, otherwise);", 
			 "The `static_if` statement is replaced with the 'then' clause or the 'otherwise' clause according "
			+"to whether the first argument - a boolean expression - evaluates to true or false. "
			+"The `otherwise` clause is optional; if it is omitted and the boolean expression evaluates to false, "
			+"the entire `static_if` statement disappears from the output."
			+"Currently, the condition supports only boolean math (e.g. `!true || false` can be evaluated but "
			+"not `5 > 4`). `static_if` is often used in conjunction with the `staticMatches` operator.",
			Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode static_if(LNode @if, IMacroContext context)
		{
			if (!@if.ArgCount.IsInRange(2, 3))
				return null;
			LNode cond = context.PreProcess(@if.Args[0]);
			cond = ReduceBooleanExpr(cond);
			object @bool;
			if ((@bool = cond.Value) is bool)
			{
				LNode output = (bool)@bool ? @if.Args[1] : @if.Args.TryGet(2, null) ?? F.Call(S.Splice);
				if (output.Calls(S.Braces))
					return output.WithTarget(S.Splice);
				else
					return output;
			}
			else
				return Reject(context, @if.Args[0], "Only boolean expressions can be evaluated.");
		}

		internal static LNode ReduceBooleanExpr(LNode node)
		{
			var n = node.Name;
			if (node.ArgCount == 2)
			{
				var lhs = ReduceBooleanExpr(node[0]);
				if (lhs.Value is bool)
				{
					if (n == S.And || n == S.AndBits)
						if ((bool)lhs.Value)
							return ReduceBooleanExpr(node[1]);
						else
							return F.False;
					if (n == S.Or || n == S.OrBits)
						if ((bool)lhs.Value)
							return F.True;
						else
							return ReduceBooleanExpr(node[1]);
					if (n == S.Eq || n == S.NotEq) {
						var rhs = ReduceBooleanExpr(node[1]);
						if (rhs.Value is bool)
							if ((((bool)lhs.Value) == ((bool)rhs.Value)) == (n == S.Eq))
								return F.True;
							else
								return F.False;
					}
				}
			}
			else if (node.ArgCount == 1 && (n == S.Not || n == S.NotBits))
			{
				var arg = ReduceBooleanExpr(node[0]);
				if (arg.Value is bool)
					return !(bool)arg.Value ? F.True : F.False;
			}
			return node;
		}

		[LexicalMacro("A ??= B", "Assign A = B only when A is null. Caution: currently, A is evaluated twice.", "'??=")]
		public static LNode NullCoalesceSet(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count != 2)
				return null;
			LNode x = a[0], y = a[1];
			// This is INCOMPLETE! But it'll suffice temporarily.
			// #??=(x, y) => x = x ?? y => #=(x, #??(x, y))
			return F.Assign(x, F.Call(S.NullCoalesce, x, y));
		}

		[LexicalMacro("A=:B; A:::B", "Declare a variable B and set it to the value A. Typically used within a larger expression, "+
			"e.g. if (int.Parse(text):::num > 0) positives += num;", "'=:", "':::")]
		public static LNode QuickBind(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F.Missing, F.Assign(B, A));
			}
			return null;
		}
		
		[LexicalMacro("A := B", "Declare a variable A and set it to the value of B. Equivalent to \"var A = B\".", "':=")]
		public static LNode ColonEquals(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F.Missing, F.Assign(A, B));
			}
			return null;
		}

		[LexicalMacro("namespace Foo;", "Surrounds the remaining code in a namespace block.", "#namespace", Mode = MacroMode.Passive)]
		public static LNode Namespace(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 2 && !node.Args.Last.Calls(S.Braces))
			{
				context.DropRemainingNodes = true;
				node = node.PlusArg(F.Braces(context.RemainingNodes).PlusAttr(F.TriviaNewline));
				// avoid artifact: `namespace Xyz` tends to be on one line, but we don't want the output to be.
				return node.SetStyle(node.Style & ~NodeStyle.OneLiner);
			}
			return null;
		}

		[LexicalMacro(@"includeFile(""Filename"")", 
			"Reads source code from the specified file, and inserts the syntax tree in place of the macro call. "
			+"The input language is determined automatically according to the file extension. "
			+"For nostalgic purposes (to resemble C/C++), `#include` is a synonym of `includeFile`. " 
			+"The single argument is macro-preprocessed.",
			"includeFile", "#include")]
		public static LNode includeFile(LNode node, IMacroContext context)
		{
			string filename;
			if (node.ArgCount == 1 && (filename = context.PreProcess(node[0]).Value as string) != null) {
				var parser = ParsingService.GetServiceForFileName(filename) ?? ParsingService.Default;
				var inputFolder = context.ScopedProperties.TryGetValue((Symbol)"#inputFolder", "").ToString();
				var path = System.IO.Path.Combine(inputFolder, filename);
				var contents = LNode.List(parser.ParseFile(path, context.Sink));
				return LNode.Call(S.Splice, contents, node);
			}
			return null;
		}

		[LexicalMacro(@"includeFileBinary(""Filename"")", 
			 "Reads bytes from a binary file, and returns them as a byte array literal. "
			+"The single argument is macro-preprocessed.")]
		public static LNode includeFileBinary(LNode node, IMacroContext context)
		{
			string filename;
			if (node.ArgCount == 1 && (filename = context.PreProcess(node[0]).Value as string) != null) {
				var inputFolder = context.ScopedProperties.TryGetValue((Symbol)"#inputFolder", "").ToString();
				var path = System.IO.Path.Combine(inputFolder, filename);
				var bytes = File.ReadAllBytes(path);
				var literal = F.Literal(bytes);
				// hex is typically more readable but decimal takes up fewer characters
				if (bytes.Length <= 1024)
					literal.SetBaseStyle(NodeStyle.HexLiteral);
				return literal;
			}
			return null;
		}

		[LexicalMacro(@"includeFileText(""Filename"")", 
			 "Reads a UTF-8 text file into a string literal. Newlines become '\\n'."
			+"The single argument is macro-preprocessed.")]
		public static LNode includeFileText(LNode node, IMacroContext context)
		{
			string filename;
			if (node.ArgCount == 1 && (filename = context.PreProcess(node[0]).Value as string) != null) {
				var inputFolder = context.ScopedProperties.TryGetValue((Symbol)"#inputFolder", "").ToString();
				var path = System.IO.Path.Combine(inputFolder, filename);
				var text = File.ReadAllText(path, Encoding.UTF8);
				return F.Literal(text).SetBaseStyle(NodeStyle.TDQStringLiteral);
			}
			return null;
		}

		[LexicalMacro(@"$(out concatId(a, b, c))", 
			"`$(out ...)` allows you to use a macro in Enhanced C# in places where macros are ordinarily not allowed, "
			+"such as in places where a data type or a method name are expected. The `out` attribute is required "
			+"to make it clear you want to run this macro and that some other meaning of `$` does not apply. Examples:\n\n"
			+"    $(out Foo) number; // variable of type Foo\n"
			+"    int $(out concatId(Sq, uare))(int x) => x*x;",
			"'$", Mode = MacroMode.Passive)]
		public static LNode DollarSignIdentity(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 1)
			{
				var expA = node.Args[0];
				var expB = expA.WithoutAttrNamed(S.Out);
				if (expA != expB)
					return expB;
			}
			return null;
		}

		[LexicalMacro("unless (Condition) {Then...}; /* LES only */ unless Condition {Then...} else {Else...}",
			"If 'Condition' is false, runs the 'Then' code; otherwise, runs the 'Else' code, if any.")]
		public static LNode @unless(LNode node, IMessageSink sink)
		{
			return LeMP.Prelude.Les.Macros.IfUnless(node, true, sink);
		}

		[LexicalMacro(@"macro_scope { code; }", 
			 "Creates a scope for local macros and local $variables to be defined. "
			+"The call itself (`macro_scope` and braces) disappear from the output.")]
		public static LNode macro_scope(LNode node, IMacroContext context)
		{
			var args = node.Args;
			LNodeList results;
			if (args.Count == 1 && args[0].Calls(S.Braces)) {
				node = context.PreProcessChildren();
				results = node.Args;
				if (results.Count == 1 && results[0].Calls(S.Braces))
					results = results[0].Args;
			} else {
				// Fabricate braces to ensure that a scope is created
				node = context.PreProcess(F.Braces(node.Args));
				results = node.Args;
			}
			return results.AsLNode(S.Splice);
		}

		static Symbol _currentNamespace = (Symbol)"#currentNamespace";

		[LexicalMacro(@"namespace Name { ... } /* C# syntax */",
			"Responds to braces inside namespace statements by changing the `#currentNamespace` property and by 'opening' the namespace as if by using `#importMacros`. The output is not directly affected.",
			"'{}", Mode = MacroMode.Passive)]
		public static LNode DetectCurrentNamespace(LNode node, IMacroContext context)
		{
			if (EcsValidators.SpaceDefinitionKind(context.Parent, out LNode name, out _, out _) == S.Namespace)
			{
				var newNamespace = name.Print(ParsingMode.Expressions);
				var currentNamespace = context.ScopedProperties.TryGetValue(_currentNamespace, null) as Symbol;
				if (currentNamespace == null)
					currentNamespace = (Symbol)newNamespace;
				else
					currentNamespace = (Symbol)(currentNamespace.Name + "." + newNamespace);
				context.OpenMacroNamespaces.Add(currentNamespace);
				context.ScopedProperties[_currentNamespace] = currentNamespace;
			}
			return null; // don't alter output
		}

		[LexicalMacro(@"reset_macros { code; }", 
			 "While processing the arguments of reset_macros,\n\n"
			+"- locally-created macros defined outside of `reset_macros` are forgotten\n"
			+"- scoped properties are reset to predefined values\n"
			+"- the list of open namespaces is reset to defaults\n")]
		public static LNode reset_macros(LNode node, IMacroContext context)
		{
			var args = node.Args;
			if (args.Count == 1 && args[0].Calls(S.Braces))
				node = args[0];

			var results = context.PreProcess(node.Args, asRoot: false, resetOpenNamespaces: true, resetProperties: true);

			return results.AsLNode(S.Splice);
		}
	}
}
