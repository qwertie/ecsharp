using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Ecs;
using System.Diagnostics;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	/// <summary>
	/// Standard macros, such as unroll() and replace() that can work in all Loyc languages.
	/// Also includes macros designed to convert EC# feature to C# (e.g. tuples).
	/// </summary>
	[ContainsMacros]
	public partial class StandardMacros
	{
		// Start counting temporary vars at 10 to avoid name collisions with 
		// things that were manually named, for example "tmp_2"
		[ThreadStatic]
		internal static int _nextTempCounter = 10;
		public static int NextTempCounter { get { return _nextTempCounter; } }

		public static Symbol NextTempName(string prefix = "tmp_")
		{
			return GSymbol.Get(prefix + _nextTempCounter++);
		}
		static LNode TempVarDecl(LNode value, out LNode tmpId, string prefix)
		{
			tmpId = LNode.Id(NextTempName(prefix), value);
			return F.Var(F.Missing, tmpId, value);
		}
		static LNode TempVarDecl(LNode value, out LNode tmpId)
		{
			string prefix = value.Name.Name;
			if (!EcsValidators.IsPlainCsIdentifier(prefix))
				prefix = "tmp_";
			else
				prefix += "_";
			return TempVarDecl(value, out tmpId, prefix);
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
		protected static LNode MaybeAddTempVarDecl(LNode value, WList<LNode> output)
		{
			if (!LooksLikeSimpleValue(value)) {
				LNode tmpId;
				output.Add(TempVarDecl(value, out tmpId));
				return tmpId;
			}
			return value;
		}
		// Decides whether a value should be placed in a temporary variable
		// if it is evaluated more than once by a macro. This returns true for 
		// non-uppercase identifiers like "x" or "_Foo", for literals like 42,
		// and for dotted expressions in which all components are non-uppercase 
		// identifiers, e.g. foo.bar.baz. Returns false for everything else.
		protected static bool LooksLikeSimpleValue(LNode value)
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

		// quote @{ Hope.For(777, $cash) } => F.Call(F.Dot(F.Id("Hope"), F.Id("For")), F.Literal(777), cash)
		// declare_symbols @@Foo @@Bar {...} (braces optional, invoke replace_code)
		// replace_code (IntSet.Parse($s) \with ParseSet($s), IntSet \with HashSet!int) {...}
		// replace_code_after ($X($Y, -1) \with $X($Y, EOF)) {...}
		// with Foo.Bar {...} => replace_code (.($x) \with Foo.Bar.$x) {...}
		// include_here "../Util/Util.les"
		// specialize $T \in (int, float, double) { def Square!$T(T x) { x*x; }; class Foo!($T,U) {}; }
		// run_macros (specialize $T \in (int, float)) (replace Xyz \with Zyx) { ... }
		// Bob.Hair?.Brush()
		// cons Point(public field X::int, public field Y::int) { }
		// def SetX(public X::int) { }
		// prop X::int (public field _x) { get; set; };
		// def DoSomething(required X::string) { };
		// public override def ToString()::string ==> _obj;
		// forward_to _obj { def ToString()::string; def GetHashCode()::int; };
		// foo ??= Foo();
		// x - 0.5;
		// x in (1, 2, 3);
		// $"The value is {Value,-10:C}." => string.Format("The value is {0,-10:C}", Value)
		// save_and_restore _foo { Foo(_foo = true); } => var tmp17 = _foo; try { Foo(_foo = true); } finally { _foo = tmp17; }

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
			else if (stmt.CallsMin(S.Switch, 2) && (body = stmt.Args[1]).CallsMin(S.Braces, 2))
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

			VList<LNode> attrs = node.Attrs;
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

		[LexicalMacro(@"nameof(id_or_expr)", @"Converts the 'key' name component of an expression to a string (e.g. nameof(A.B<C>(D)) == ""B"")")]
		public static LNode @nameof(LNode nameof, IMacroContext context)
		{
			if (nameof.ArgCount != 1)
				return null;
			Symbol expr = KeyNameComponentOf(nameof.Args[0]);
			return F.Literal(expr.Name);
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
			return F.Literal(ParsingService.Current.Print(node.Args[0], context.Sink, ParsingMode.Exprs));
		}
		
		#endregion

		[LexicalMacro(@"x `tree==` y", 
			"Returns the literal true if two or more syntax trees are equal, or false if not. The two arguments are preprocessed.", 
			"tree==")]
		public static LNode TreeEqual(LNode node, IMacroContext context)
		{
			if (node.ArgCount < 2) return null;
			node = context.PreProcessChildren();
			var args = node.Args;

			LNode left = args[0];
			for (int i = 1; i < args.Count; i++)
				if (!left.Equals(args[i]))
					return F.Literal(G.BoxedFalse);
			return F.Literal(G.BoxedTrue);
		}

		[LexicalMacro(@"static if() {...} else {...}", "TODO. Only boolean true/false implemented now", "#if", 
			Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode StaticIf(LNode @if, IMacroContext context)
		{
			LNode @static;
			if ((@static = @if.AttrNamed(S.Static)) == null || !@static.IsId)
				return null;
			return static_if(@if, context);
		}
		
		[LexicalMacro(@"static_if(cond, then, otherwise)", "TODO. Only boolean true/false implemented now",
			Mode = MacroMode.Passive | MacroMode.Normal)]
		public static LNode static_if(LNode @if, IMacroContext context)
		{
			if (!Range.IsInRange(@if.ArgCount, 2, 3))
				return null;
			LNode cond = context.PreProcess(@if.Args[0]);
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
				return Reject(context, @if.Args[0], "'static if' is incredibly limited right now. Currently it only supports a literal boolean or (x `tree==` y)");
		}

		[LexicalMacro("A ??= B", "Assign A = B only when A is null. Caution: currently, A is evaluated twice.", "??=")]
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
			"e.g. if (int.Parse(text):::num > 0) positives += num;", "=:", ":::")]
		public static LNode QuickBind(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode A = a[0], B = a[1];
				return F.Vars(F.Missing, F.Assign(B, A));
			}
			return null;
		}
		
		[LexicalMacro("A := B", "Deprecated. Declare a variable A and set it to the value of B. Equivalent to \"var A = B\".", ":=")]
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
				return node.WithArgs(node.Args.Add(F.Braces(context.RemainingNodes)));
			}
			return null;
		}

		[LexicalMacro(@"includeFile(""Filename"")", 
			"Reads source code from the specified file, and inserts the syntax tree in place of the macro call. "
			+"The input language is determined automatically according to the file extension. "
			+"For nostalgic purposes (to resemble C/C++), `#include` is a synonym of `includeFile`.", 
			"includeFile", "#include")]
		public static LNode includeFile(LNode node, IMacroContext context)
		{
			string filename;
			if (node.ArgCount == 1 && (filename = context.PreProcess(node[0]).Value as string) != null) {
				var parser = ParsingService.GetServiceForFileName(filename) ?? ParsingService.Current;
				var inputFolder = context.ScopedProperties.TryGetValue((Symbol)"#inputFolder", "").ToString();
				var path = System.IO.Path.Combine(inputFolder, filename);
				var contents = LNode.List(parser.ParseFile(path, context.Sink));
				return LNode.Call(S.Splice, contents, node);
			}
			return null;
		}

		[LexicalMacro(@"$(out concatId(a, b, c))", 
			"`$(out ...)` allows you to use a macro in Enhanced C# in places where macros are ordinarily not allowed, "
			+"such as in places where a data type or a method name are expected. The `out` attribute is required "
			+"to make it clear you want to run this macro and that some other meaning of `$` does not apply. Examples:\n\n"
			+"    $(out Foo) number; // variable of type Foo\n"
			+"    int $(out concatId(Sq, uare))(int x) => x*x;",
			"$")]
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

		[LexicalMacro("#runSequence { Stmts; };",
			"Allows #runSequence at brace-scope without the use of #useVarDeclExpressions",
			"#runSequence")]
		public static LNode runSequence(LNode node, IMacroContext context)
		{
			if (context.Parent.Calls(S.Braces)) { 
				if (node.ArgCount == 1 && node.Args[0].Calls(S.Braces))
					return node.WithArgs(node.Args[0].Args);
				return node.WithTarget(S.Splice);
			}
			return Reject(context, node, "#useVarDeclExpressions is required to make #runSequence work");
		}
	}
}
