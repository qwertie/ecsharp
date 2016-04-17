// Generated from UseSequenceExpressions.ecs by LeMP custom tool. LeMP version: 1.7.3.0
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
		[LexicalMacro("#useSequenceExpressions; ... if (Foo.Bar()::b.Baz != null) b.Baz.Method(); ...", "Enables the use of variable-declaration and #runSequence expressions, including the quick-binding operator `::` and the `with` expression, in the code that follows." + "Technically this allows any executable code in an expression context, such as while and for-loops, " + "but its name comes from the fact that it is usually used to allow variable declarations. " + "#useVarDeclExpressions expects to be used in a declaration context, " + "e.g. at class or namespace level, not within a function.", "#useSequenceExpressions", Mode = MacroMode.NoReprocessing)]
		public static LNode useSequenceExpressions(LNode node, IMacroContext context)
		{
			var tmp_0 = context.GetArgsAndBody(true);
			var args = tmp_0.Item1;
			var body = tmp_0.Item2;
			if (args.Count > 0)
				context.Write(Severity.Error, node[1], "#useSequenceExpressions does not support arguments.");
			context.DropRemainingNodes = true;
			body = context.PreProcess(body);
			var ers = new EliminateRunSequences(context);
			return ers.EliminateBlockExprs(body, true).AsLNode(S.Splice);
		}
		class EliminateRunSequences
		{
			public IMacroContext Context;
			public EliminateRunSequences(IMacroContext context)
			{
				Context = context;
			}
			public VList<LNode> EliminateBlockExprs(VList<LNode> stmts, bool isDeclContext)
			{
				return stmts.SmartSelect(stmt => {
					return EliminateBlockExprs(stmt, isDeclContext);
				});
			}
			public LNode EliminateBlockExprs(LNode stmt, bool isDeclContext)
			{
				LNode retType, name, argList, bases, body, initValue;
				if (EcsValidators.SpaceDefinitionKind(stmt, out name, out bases, out body) != null) {
					return body == null ? stmt : stmt.WithArgChanged(2, EliminateBlockExprs(body, true));
				} else if (EcsValidators.MethodDefinitionKind(stmt, out retType, out name, out argList, out body, true) != null) {
					return body == null ? stmt : stmt.WithArgChanged(3, EliminateBlockExprs(body, false));
				} else if (EcsValidators.IsPropertyDefinition(stmt, out retType, out name, out argList, out body, out initValue)) {
					stmt = stmt.WithArgChanged(3, body.WithArgs(part => {
						if (part.ArgCount == 1 && part[0].Calls(S.Braces))
							part = part.WithArgChanged(0, EliminateBlockExprs(part[0], false));
						return part;
					}));
					if (initValue != null) {
						var initMethod = EliminateRunSeqFromInitializer(retType, name, ref initValue);
						if (initMethod != null) {
							stmt = stmt.WithArgChanged(4, initValue);
							return LNode.Call(CodeSymbols.Splice, LNode.List(stmt, initMethod));
						}
					}
					return stmt;
				} else if (!isDeclContext) {
					return EliminateBlockExprsInExecStmt(stmt);
				} else if (stmt.CallsMin(S.Var, 2)) {
					var results = new List<LNode> { 
						stmt
					};
					var vars = stmt.Args;
					var varType = vars[0];
					for (int i = 1; i < vars.Count; i++) {
						var @var = vars[i];
						if (@var.Calls(CodeSymbols.Assign, 2) && (name = @var.Args[0]) != null && (initValue = @var.Args[1]) != null) {
							var initMethod = EliminateRunSeqFromInitializer(varType, name, ref initValue);
							if (initMethod != null) {
								results.Add(initMethod);
								vars[i] = vars[i].WithArgChanged(1, initValue);
							}
						}
					}
					if (results.Count > 1) {
						results[0] = stmt.WithArgs(vars);
						return LNode.List(results).AsLNode(S.Splice);
					}
					return stmt;
				} else
					return stmt;
			}
			LNode EliminateBlockExprsInExecBlock(LNode stmt)
			{
				stmt = EliminateBlockExprsInExecStmt(stmt);
				if (stmt.Calls(S.Splice))
					return stmt.WithTarget(S.Braces);
				return stmt;
			}
			LNode EliminateBlockExprsInExecStmt(LNode stmt)
			{
				if (!stmt.IsCall)
					return stmt;
				{
					LNode cond;
					VList<LNode> blocks;
					if (stmt.Calls(CodeSymbols.Braces))
						return stmt.WithArgs(EliminateBlockExprs(stmt.Args, false));
					else if (stmt.CallsMin(CodeSymbols.If, 1) && (cond = stmt.Args[0]) != null) {
						blocks = new VList<LNode>(stmt.Args.Slice(1));
						return ProcessBlockCallStmt(stmt, 1);
					} else if (stmt.HasSpecialName && stmt.ArgCount >= 1 && stmt.Args.Last.Calls(S.Braces)) {
						return ProcessBlockCallStmt(stmt, stmt.ArgCount - 1);
					} else {
						stmt = BubbleUpBlocks(stmt, true);
						if (stmt.CallsMin(__runSequence, 1))
							return stmt.Args.AsLNode(S.Splice);
					}
				}
				return stmt;
			}
			LNode ProcessBlockCallStmt(LNode stmt, int childStmtsStartAt)
			{
				List<LNode> childStmts = stmt.Slice(childStmtsStartAt).ToList();
				LNode partialStmt = stmt.WithArgs(stmt.Args.First(childStmtsStartAt));
				VList<LNode> advanceSequence;
				if (ProcessBlockCallStmt(ref partialStmt, out advanceSequence, childStmts)) {
					stmt = partialStmt.PlusArgs(childStmts);
					if (advanceSequence.Count != 0)
						return LNode.Call(CodeSymbols.Braces, LNode.List().AddRange(advanceSequence).Add(stmt)).SetStyle(NodeStyle.Statement);
					return stmt;
				} else
					return stmt;
			}
			bool ProcessBlockCallStmt(ref LNode partialStmt, out VList<LNode> advanceSequence, List<LNode> childStmts)
			{
				bool childChanged = false;
				for (int i = 0; i < childStmts.Count; i++) {
					var oldChild = childStmts[i];
					childStmts[i] = EliminateBlockExprsInExecBlock(oldChild);
					childChanged |= (oldChild != childStmts[i]);
				}
				var BubbleUp_GeneralCall2_1 = BubbleUp_GeneralCall2(partialStmt);
				advanceSequence = BubbleUp_GeneralCall2_1.Item1;
				partialStmt = BubbleUp_GeneralCall2_1.Item2;
				return childChanged || !advanceSequence.IsEmpty;
			}
			LNode EliminateRunSeqFromInitializer(LNode retType, LNode fieldName, ref LNode expr)
			{
				expr = BubbleUpBlocks(expr);
				if (expr.CallsMin(__runSequence, 1)) {
					var statements = expr.Args.WithoutLast(1);
					var finalResult = expr.Args.Last;
					LNode methodName = F.Id(KeyNameComponentOf(fieldName).Name + "_initializer");
					expr = LNode.Call(methodName);
					return LNode.Call(LNode.List(LNode.Id(CodeSymbols.Static)), CodeSymbols.Fn, LNode.List(retType, methodName, LNode.Call(CodeSymbols.AltList), LNode.Call(CodeSymbols.Braces, LNode.List().AddRange(statements).Add(LNode.Call(CodeSymbols.Return, LNode.List(finalResult)))).SetStyle(NodeStyle.Statement)));
				} else
					return null;
			}
			LNode BubbleUpBlocks(LNode expr, bool isStmtLevel = false)
			{
				if (!expr.IsCall)
					return expr;
				{
					LNode tmp_2 = null, value, varName, varType = null;
					VList<LNode> args, attrs;
					if (expr.Calls((Symbol) "#runSequence")) {
						args = expr.Args;
						if (args.Count == 1 && args[0].Calls(S.Braces))
							expr = expr.WithArgs(args[0].Args);
						return expr;
					} else if (expr.Calls(CodeSymbols.Braces)) {
						if (!isStmtLevel)
							Context.Write(Severity.Error, expr, "A braced block is not supported directly within an expression. Did you mean to use `#runSequence {...}`?");
						return expr;
					} else if ((attrs = expr.Attrs).IsEmpty | true && attrs.NodeNamed(S.Out) != null && expr.Calls(CodeSymbols.Var, 2) && (varType = expr.Args[0]) != null && (varName = expr.Args[1]) != null && varName.IsId) {
						if (varType.IsIdNamed(S.Missing))
							Context.Write(Severity.Error, expr, "The data type of this variable declaration must be stated explicitly.");
						return LNode.Call((Symbol) "#runSequence", LNode.List(expr.WithoutAttrNamed(S.Out), varName));
					} else if ((attrs = expr.Attrs).IsEmpty | true && expr.Calls(CodeSymbols.Var, 2) && (varType = expr.Args[0]) != null && (tmp_2 = expr.Args[1]) != null && tmp_2.Calls(CodeSymbols.Assign, 2) && (varName = tmp_2.Args[0]) != null && (value = tmp_2.Args[1]) != null || (attrs = expr.Attrs).IsEmpty | true && expr.Calls(CodeSymbols.ColonColon, 2) && (value = expr.Args[0]) != null && IsQuickBindLhs(value) && (varName = expr.Args[1]) != null && varName.IsId)
						return ConvertVarDeclToRunSequence(attrs, varType ?? F.Missing, varName, value);
				}
				if (expr.IsCall)
					return BubbleUp_GeneralCall(expr);
				else
					return expr;
			}
			LNode BubbleUp_GeneralCall(LNode expr)
			{
				var BubbleUp_GeneralCall2_3 = BubbleUp_GeneralCall2(expr);
				var combinedSequence = BubbleUp_GeneralCall2_3.Item1;
				expr = BubbleUp_GeneralCall2_3.Item2;
				if (combinedSequence.Count != 0)
					return LNode.Call((Symbol) "#runSequence", LNode.List().AddRange(combinedSequence).Add(expr));
				else
					return expr;
			}
			Pair<VList<LNode>,LNode> BubbleUp_GeneralCall2(LNode expr)
			{
				var target = expr.Target;
				var args = expr.Args;
				var combinedSequence = LNode.List();
				target = BubbleUpBlocks(target);
				if (target.CallsMin(__runSequence, 1)) {
					combinedSequence = target.Args.WithoutLast(1);
					expr = expr.WithTarget(target.Args.Last);
				}
				args = args.SmartSelect(arg => BubbleUpBlocks(arg));
				int lastRunSeq = args.LastIndexWhere(a => a.CallsMin(__runSequence, 1));
				if (lastRunSeq >= 0) {
					if (lastRunSeq > 0 && (args.Count == 2 && (target.IsIdNamed(S.And) || target.IsIdNamed(S.Or)) || args.Count == 3 && target.IsIdNamed(S.QuestionMark))) {
						Context.Write(Severity.Error, expr, "#useVarDeclExpressions is not designed to support sequences or variable declarations on the right-hand side of the `&&`, `||` or `?` operators. The generated code may be incorrect.");
					}
					var argsW = args.ToList();
					for (int i = lastRunSeq - 1; i >= 0; i--) {
						if (!argsW[i].CallsMin(__runSequence, 1) && !argsW[i].IsLiteral) {
							LNode tmpVarName, tmpVarDecl = TempVarDecl(argsW[i], out tmpVarName);
							argsW[i] = LNode.Call((Symbol) "#runSequence", LNode.List(tmpVarDecl, tmpVarName));
						}
					}
					for (int i = 0; i <= lastRunSeq; i++) {
						LNode arg = argsW[i];
						if (arg.CallsMin(__runSequence, 1)) {
							combinedSequence.AddRange(arg.Args.WithoutLast(1));
							argsW[i] = arg.Args.Last;
						}
					}
					expr = expr.WithArgs(LNode.List(argsW));
				}
				return Pair.Create(combinedSequence, expr);
			}
			LNode ConvertVarDeclToRunSequence(VList<LNode> attrs, LNode varType, LNode varName, LNode initValue)
			{
				initValue = BubbleUpBlocks(initValue);
				varType = varType ?? F.Missing;
				LNode @ref;
				attrs = attrs.WithoutNodeNamed(S.Ref, out @ref);
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
