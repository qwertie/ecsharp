// Generated from UseBlockExpressions.ecs by LeMP custom tool. LeMP version: 1.7.1.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Ecs;
namespace LeMP
{
	partial class StandardMacros
	{
		static readonly Symbol __runSequence = (Symbol) "#runSequence";
		[LexicalMacro("#useBlockExpressions; ... if (Foo.Bar()::b.Baz != null) b.Baz.Method(); ...", "Enables the use of \"block expressions\" and the quick-binding operator `::` in the code that follows. " + "#useBlockExpressions expects to be used in a declaration context, " + "e.g. at class or namespace level, not within a function.", "#useBlockExpressions", Mode = MacroMode.NoReprocessing)]
		public static LNode useBlockExpressions(LNode node, IMacroContext context)
		{
			var tmp_0 = context.GetArgsAndBody(true);
			var args = tmp_0.Item1;
			var body = tmp_0.Item2;
			if (args.Count > 0)
				context.Write(Severity.Error, node[1], "#useSpecialExpressions does not support arguments.");
			context.DropRemainingNodes = true;
			body = context.PreProcess(body);
			bool hasBlockExprs;
			return F.Call(S.Splice, body.Select(stmt => {
				return EliminateBlockExprs(stmt, true);
			}));
		}
		struct BlockExprEliminator
		{
			public IMacroContext Context;
			public BlockExprEliminator(IMacroContext context)
			{
				Context = context;
			}
			private static LNode EliminateBlockExprs(LNode stmt, bool isDeclContext)
			{
				LNode retType, name, argList, bases, body, initValue;
				if (EcsValidators.SpaceDefinitionKind(stmt, out name, out bases, out body) != null) {
					return body == null ? stmt : stmt.WithArgChanged(2, EliminateBlockExprs(body, true));
				} else if (EcsValidators.MethodDefinitionKind(stmt, out retType, out name, out argList, out body, true) != null) {
					return body == null ? stmt : stmt.WithArgChanged(3, EliminateBlockExprs(body, false));
				} else if (EcsValidators.IsPropertyDefinition(stmt, out retType, out name, out argList, out body, out initValue)) {
					if (initValue != null)
						stmt = stmt.WithArgChanged(4, EliminateBlockExprsInExecStmt(initValue, true));
					stmt = stmt.WithArgChanged(3, EliminateBlockExprs(body, false));
					return stmt;
				} else if (stmt.CallsMin(S.Var, 2)) {
					var vars = stmt.Args;
					for (int i = 1; i < vars.Count; i++) {
						stmt = stmt.WithArgChanged(i, @var => {
							if (@var.Calls(S.Assign, 2))
								return @var.WithArgChanged(1, expr => EliminateBlockExprsInExecStmt(expr, true));
							return @var;
						});
					}
					return stmt;
				} else if (!stmt.Calls(S.Braces) && !isDeclContext) {
					return EliminateBlockExprsInExecStmt(stmt, false);
				} else
					return stmt;
			}
			static LNode EliminateBlockExprsInExecStmt(LNode stmt, bool isFieldInitializer)
			{
				if (!stmt.IsCall)
					return stmt;
				{
					LNode block, cond, inc, init, target;
					VList<LNode> args, blocks;
					if (stmt.Calls(CodeSymbols.Braces))
						return stmt.WithArgs(substmt => EliminateBlockExprs(substmt, false));
					else if (stmt.Calls(CodeSymbols.While, 2) && (cond = stmt.Args[0]) != null && (block = stmt.Args[1]) != null) {
					} else if (stmt.Calls(CodeSymbols.DoWhile, 2) && (block = stmt.Args[0]) != null && (cond = stmt.Args[1]) != null) {
					} else if (stmt.Calls(CodeSymbols.For, 4) && (init = stmt.Args[0]) != null && (cond = stmt.Args[1]) != null && (inc = stmt.Args[2]) != null && (block = stmt.Args[3]) != null) {
					} else if (stmt.CallsMin(CodeSymbols.If, 1) && (cond = stmt.Args[0]) != null) {
						blocks = new VList<LNode>(stmt.Args.Slice(1));
					} else if (stmt.IsCall && (target = stmt.Target) != null) {
						args = stmt.Args;
						if (target.IsId && args.Count > 0 && args.Last.Calls(S.Braces) && (stmt.Name.Name.StartsWith("#") || stmt.BaseStyle == NodeStyle.Special)) {
							return EliminateBlockExprsIn(stmt, target, -1, args.WithoutLast(1), args.Last);
						default:
							return EliminateBlockExprsIn(stmt, stmt.Target, stmt.Args);
						}
					}
				}
				return stmt;
			}
			static LNode EliminateBlocksExprsIn(LNode stmt, LNode target, VList<LNode> args, LNode loopCondition = null, LNode innerBlock = null)
			{
			}
			static LNode BubbleUpBlocks(LNode expr)
			{
				if (!expr.IsCall)
					return expr;
				{
					LNode tmp_1, tmp_2, tmp_3, tmp_4, tmp_5, tmp_6, value, varName;
					VList<LNode> attrs;
					if (expr.CallsMin(CodeSymbols.Assembly, 1) && expr.Args[0].Calls(CodeSymbols.Substitute, 1) && expr.Args[0].Args[0].Calls(CodeSymbols.DotDotDot, 1) && (tmp_1 = expr.Args[0].Args[0].Args[0]) != null && tmp_1.Calls(CodeSymbols.Neq, 2) && (tmp_2 = tmp_1.Args[0]) != null && tmp_2.Calls(CodeSymbols.IndexBracks, 2) && tmp_2.Args[0].IsIdNamed((Symbol) "attrs") && (tmp_3 = tmp_2.Args[1]) != null && tmp_3.Args.Count == 1 && (tmp_4 = tmp_3.Target) != null && tmp_4.Calls(CodeSymbols.Dot, 2) && tmp_4.Args[0].IsIdNamed((Symbol) "#") && tmp_4.Args[1].IsIdNamed((Symbol) "NodeNamed") && (tmp_5 = tmp_3.Args[0]) != null && tmp_5.Calls(CodeSymbols.Dot, 2) && tmp_5.Args[0].IsIdNamed((Symbol) "S") && tmp_5.Args[1].IsIdNamed((Symbol) "Out") && tmp_1.Args[1].Value == null && (tmp_6 = expr.Args[1]) != null && (attrs = tmp_6.Attrs).IsEmpty | true && tmp_6.Calls(CodeSymbols.ColonColon, 2) && (value = tmp_6.Args[0]) != null && IsQuickBindLhs(value) && (varName = tmp_6.Args[1]) != null && varName.IsId)
						if (expr.Calls(S.In, 2))
							@#error("Statement expected at ''");
				}
				static LNode ConvertVarDeclToRunSequence(LNode varType, LNode varName, LNode initValue)
				{
					initValue = BubbleUpBlocks(initValue);
					varType = varType ?? F.Missing;
					LNode @ref, node = attrs.WithoutNodeNamed(S.Ref, out @ref);
					{
						LNode resultValue;
						VList<LNode> stmts;
						if (initValue.CallsMin((Symbol) "#runSequence", 1) && (resultValue = initValue.Args[initValue.Args.Count - 1]) != null) {
							stmts = initValue.Args.WithoutLast(1);
							var newVarDecl = LNode.Call(LNode.List(attrs), CodeSymbols.Var, LNode.List(varType, LNode.Call(CodeSymbols.Assign, LNode.List(varName, resultValue))));
							return initValue.WithArgs(stmts.Add(newVarDecl).Add(varName));
						} else {
							var newVarDecl = LNode.Call(LNode.List(attrs), CodeSymbols.Var, LNode.List(varType, LNode.Call(CodeSymbols.Assign, LNode.List(varName, initValue))));
							return LNode.Call((Symbol) "#runSequence", LNode.List(newVarDecl, varName));
						}
					}
				}
				static bool IsQuickBindLhs(LNode value)
				{
					if (!value.IsId)
						return true;
					return char.IsUpper(value.Name.Name.TryGet(0, '\0'));
				}
			}
		}
	}
}
