using Loyc;
using Loyc.Collections;
using Loyc.Ecs;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using S = Loyc.Syntax.CodeSymbols;

namespace LeMP
{
	[ContainsMacros]
	public partial class StandardMacros
	{
		static Symbol _myNamespace = (Symbol)typeof(StandardMacros).Namespace;

		internal static LNodeFactory F = new LNodeFactory(new EmptySourceFile("LeMP.StandardMacros"));
		internal static readonly Symbol __ = (Symbol)"_";
		internal static readonly Symbol _hash = GSymbol.Get("#");

		internal static LNode Reject(IMessageSink sink, LNode at, string msg, Severity severity = Severity.Error)
		{
			sink.Write(severity, at, msg);
			return null;
		}
		public static Symbol NextTempName(IMacroContext ctx, string prefix = "tmp_")
		{
			return GSymbol.Get(prefix + ctx.IncrementTempCounter());
		}
		public static Symbol NextTempName(IMacroContext ctx, LNode value)
		{
			string prefix = value.Name.Name;
			prefix = LNode.IsSpecialName(prefix) ? "tmp_" : prefix + "_";
			return NextTempName(ctx, prefix);
		}
		internal static LNode TempVarDecl(IMacroContext ctx, LNode value, out LNode tmpVarName)
		{
			tmpVarName = LNode.Id(NextTempName(ctx, value), value);
			return F.Var(F.Missing, tmpVarName, value);
		}
		internal static LNode TempVarDecl(IMacroContext ctx, LNode value, out LNode tmpVarName, string prefix)
		{
			tmpVarName = LNode.Id(NextTempName(ctx, prefix), value);
			return F.Var(F.Missing, tmpVarName, value);
		}

		internal static LNode UnwrapBraces(LNode node)
		{
			if (node.Calls(S.Braces, 1) && !node.HasPAttrs())
				return node.Args[0];
			return node;
		}

		internal static Symbol DecodeSubstitutionExpr(LNode expr, out LNode condition, out bool isParams, out bool refExistingVar)
		{
			condition = null;
			isParams = false;
			refExistingVar = false;
			if (expr.Calls(S.Substitute, 1)) {
				LNode id = expr.Args[0];
				if (id.AttrNamed(S.Params) != null)
					isParams = true;
				else if (id.Calls(S.DotDotDot, 1) || id.Calls(S.DotDot, 1)) {
					isParams = true;
					id = id.Args[0];
				}

				if (id.AttrNamed(S.Ref) != null)
					refExistingVar = true;

				if (id.Calls(S.IndexBracks, 2)) {
					// very old style
					condition = id.Args[1];
					id = id.Args[0];
				} else
					while (id.Calls(S.And, 2) || id.Calls(S.When, 2)) {
						// old style `&&` and new style `when`
						condition = condition == null ? id.Args[1] : LNode.Call(CodeSymbols.And, LNode.List(id.Args[1], condition)).SetStyle(NodeStyle.Operator);
						id = id.Args[0];
					}

				if (condition != null)
					condition = condition.ReplaceRecursive(n => n.IsIdNamed(S._HashMark) ? id : null);
				if (!id.IsId)
					return null;
				return id.Name;
			}
			return null;
		}

		/// <summary>Given the contents of case statement like `matchCode` or 
		/// `switch`, this method gets a list of the cases.</summary>
		/// <returns>The first item in each pair is a list of the cases associated
		/// with a single handler (for `default:`, the list is empty). The second 
		/// item is the handler code.</returns>
		static internal VList<(LNodeList Cases, LNodeList Handler)> GetCases(LNodeList body, IMessageSink sink)
		{
			var pairs = VList<(LNodeList Cases, LNodeList Handler)>.Empty;
			for (int i = 0; i < body.Count; i++) {
				bool isDefault;
				if (body[i].Calls(S.Lambda, 2)) {
					var alts = body[i][0].WithoutOuterParens().AsList(S.Tuple).SmartSelect(UnwrapBraces);
					pairs.Add((alts, body[i][1].AsList(S.Braces)));
				} else if ((isDefault = IsDefaultLabel(body[i])) || body[i].CallsMin(S.Case, 1)) {
					var alts = isDefault ? LNodeList.Empty : body[i].Args.SmartSelect(UnwrapBraces);
					int bodyStart = ++i;
					for (; i < body.Count && !IsDefaultLabel(body[i]) && !body[i].CallsMin(S.Case, 1); i++) { }
					var handler = new LNodeList(body.Slice(bodyStart, i - bodyStart));
					pairs.Add((alts, handler));
					i--;    // counteract i++ when loop repeats (redo)
				} else {
					Reject(sink, body[i], "expected 'case _:' or '_ => _'");
					break;
				}
			}
			return pairs;
		}

		static bool IsDefaultLabel(LNode stmt)
		{
			return stmt.Calls(S.Label, 1) && stmt[0].IsIdNamed(S.Default);
		}

		#region concatId (##), stringify

		[LexicalMacro(@"concat(a, b)",
			"Concatenates identifiers and/or literals to produce a string. For example, " +
			"the output of `concat(abc, 123)` is `abc123`.\n", Mode = MacroMode.ProcessChildrenBefore)]
		public static LNode concat(LNode node, IMacroContext context)
		{
			var result = ConcatCore(node, out var attrs, context.Sink);
			return result == null ? null : LNode.Literal(result.ToString(), node).WithAttrs(attrs);
		}

		[LexicalMacro(@"a `##` b; concatId(a, b)",
			"Concatenates identifiers and/or literals to produce an identifier. For example, the output of ``a `##` b`` is `ab`.\n"
			+ "\n**Note**: concatId cannot be used directly as a variable or method name unless you use `$(out concatId(...))`.",
			"##", "concatId", Mode = MacroMode.ProcessChildrenBefore)]
		public static LNode concatId(LNode node, IMacroContext context)
		{
			StringBuilder sb = ConcatCore(node, out var attrs, context.Sink, allowLastToBeCall: true);
			if (sb == null)
				return null;

			Symbol combined = GSymbol.Get(sb.ToString());
			LNode result;
			if (node.Args.Last.IsCall)
				result = node.Args.Last.WithTarget(combined);
			else
				result = LNode.Id(combined, node);
			return result.WithAttrs(attrs);
		}

		private static StringBuilder ConcatCore(LNode node, out LNodeList attrs, IMessageSink sink, bool allowLastToBeCall = false)
		{
			attrs = node.Attrs;
			var args = node.Args;
			if (args.Count == 0)
				return null;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < args.Count; i++) {
				LNode arg = args[i];
				attrs.AddRange(arg.Attrs);

				if (arg.IsLiteral) {
					if (!arg.TextValue.IsNull)
						arg.TextValue.AppendTo(sb);
					else
						sb.Append(arg.Value ?? "null");
				} else if (arg.IsId) {
					sb.Append(arg.Name);
				} else { // call
					if (i + 1 != args.Count || !arg.HasSimpleHead()) {
						Reject(sink, arg, "Expected simple identifier or literal");
						return null;
					}
					sb.Append(arg.Name);
				}
			}
			return sb;
		}

		[LexicalMacro(@"stringify(expr)", "Converts an expression to a string (note: original formatting is not preserved)")]
		public static LNode stringify(LNode node, IMacroContext context)
		{
			if (node.ArgCount != 1)
				return null; // reject
			return F.Literal(LNode.Printer.Print(node.Args[0], context.Sink, ParsingMode.Expressions));
		}

		#endregion
	}
}
