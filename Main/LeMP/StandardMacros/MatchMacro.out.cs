// Generated from MatchMacro.ecs by LeMP custom tool. LLLPG version: 1.4.0.0
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
using System.Diagnostics;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using S = Loyc.Syntax.CodeSymbols;
namespace LeMP
{
	partial class StandardMacros
	{
		[LexicalMacro("match (var) { case ...: ... }; // In LES, use a => b instead of case a: b", "Attempts to match and deconstruct an object against a \"pattern\", such as a tuple or an algebraic data type. Example:\n" + "match (obj) {  \n" + "   case is Shape(ShapeType.Circle, $size, Location: $p is Point<int>($x, $y)): \n" + "      Circle(size, x, y); \n" + "}\n\n" + "This is translated to the following C# code: \n" + "do { \n" + "   Point<int> p; \n" + "   Shape tmp1; \n" + "   if (obj is Shape) { \n" + "      var tmp1 = (Shape)obj; \n" + "      if (tmp1.Item1 == ShapeType.Circle) { \n" + "         var size = tmp1.Item2; \n" + "         var tmp2 = tmp1.Location; \n" + "         if (tmp2 is Point<int>) { \n" + "            var p = (Point<int>)tmp2; \n" + "            var x = p.Item1; \n" + "            var y = p.Item2; \n" + "            Circle(size, x, y); \n" + "            break; \n" + "         } \n" + "      }\n" + "   }\n" + "} while(false); \n" + "`break` is not expected at the end of each handler (`case` code block), but it can " + "be used to exit early from a `case`. You can associate multiple patterns with the same " + "handler using `case pattern1, pattern2:` in EC#, but please note that (due to a " + "limitation of plain C#) this causes code duplication since the handler will be repeated " + "for each pattern.")] public static LNode match(LNode node, IMacroContext context)
		{
			{
				LNode input;
				RVList<LNode> contents;
				if (node.Args.Count == 2 && (input = node.Args[0]) != null && node.Args[1].Calls(CodeSymbols.Braces)) {
					contents = node.Args[1].Args;
					var outputs = new RWList<LNode>();
					input = MaybeAddTempVarDecl(input, outputs);
					int next_i = 0;
					for (int case_i = 0; case_i < contents.Count; case_i = next_i) {
						var @case = contents[case_i];
						if (!IsCaseLabel(@case))
							return Reject(context, contents[0], "In 'match': expected 'case' statement");
						for (next_i = case_i + 1; next_i < contents.Count; next_i++) {
							var stmt = contents[next_i];
							if (IsCaseLabel(stmt))
								break;
							if (stmt.Calls(S.Break, 0)) {
								next_i++;
								break;
							}
						}
						var handler = new RVList<LNode>(contents.Slice(case_i + 1, next_i - (case_i + 1)));
						if (@case.Calls(S.Case) && @case.Args.Count > 0) {
							var codeGen = new CodeGeneratorForMatchCase(context, input, handler);
							foreach (var pattern in @case.Args)
								outputs.Add(codeGen.GenCodeForPattern(pattern));
						} else {
							outputs.Add(LNode.Call(CodeSymbols.Braces, new RVList<LNode>(handler)).SetStyle(NodeStyle.Statement));
							if (next_i < contents.Count)
								context.Write(Severity.Error, contents[next_i], "The default branch must be the final branch in a 'match' statement.");
						}
					}
					return LNode.Call(CodeSymbols.DoWhile, LNode.List(outputs.ToRVList().AsLNode(S.Braces), LNode.Literal(false)));
				}
			}
			return null;
		}
		static bool IsCaseLabel(LNode @case)
		{
			if (@case.Calls(CodeSymbols.Case) || @case.Calls(CodeSymbols.Label, 1) && @case.Args[0].IsIdNamed((Symbol) "#default"))
				return true;
			return false;
		}
		class CodeGeneratorForMatchCase
		{
			protected IMacroContext _context;
			protected LNode _input;
			protected RVList<LNode> _handler;
			internal CodeGeneratorForMatchCase(IMacroContext context, LNode input, RVList<LNode> handler)
			{
				_context = context;
				_input = input;
				_handler = handler;
				var @break = LNode.Call(CodeSymbols.Break);
				if (_handler.IsEmpty || !_handler.Last.Equals(@break))
					_handler.Add(@break);
			}
			internal LNode GenCodeForPattern(LNode pattern)
			{
				_output = new List<Pair<Mode,LNode>>();
				GenCodeForPattern(_input, pattern);
				return GetOutputAsLNode();
			}
			enum Mode
			{
				Statement, Condition
			}
			List<Pair<Mode,LNode>> _output;
			void PutStmt(LNode stmt)
			{
				_output.Add(Pair.Create(Mode.Statement, stmt));
			}
			void PutCond(LNode cond)
			{
				_output.Add(Pair.Create(Mode.Condition, cond));
			}
			void GenCodeForPattern(LNode input, LNode pattern)
			{
				bool refExistingVar;
				LNode varBinding, cmpExpr, isType, inRange;
				RVList<LNode> subPatterns, conditions;
				GetPatternComponents(pattern, out varBinding, out refExistingVar, out cmpExpr, out isType, out inRange, out subPatterns, out conditions);
				if (isType != null) {
					if ((cmpExpr ?? inRange ?? varBinding) != null) {
						if (!LooksLikeSimpleValue(input))
							PutStmt(TempVarDecl(input, out input));
					}
					PutCond(LNode.Call(CodeSymbols.Is, LNode.List(input, isType)).SetStyle(NodeStyle.Operator));
					if (varBinding == null && ((cmpExpr ?? inRange) != null || subPatterns.Count > 0))
						varBinding = LNode.Id(NextTempName(), isType);
				}
				if (varBinding != null) {
					if (isType != null) {
						if (refExistingVar)
							PutStmt(LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, LNode.Call(CodeSymbols.Cast, LNode.List(input, isType)).SetStyle(NodeStyle.Operator))).SetStyle(NodeStyle.Operator));
						else
							PutStmt(LNode.Call(CodeSymbols.Var, LNode.List(isType, LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, LNode.Call(CodeSymbols.Cast, LNode.List(input, isType)).SetStyle(NodeStyle.Operator))))));
					} else {
						if (refExistingVar)
							PutStmt(LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, input)).SetStyle(NodeStyle.Operator));
						else
							PutStmt(LNode.Call(CodeSymbols.Var, LNode.List(LNode.Missing, LNode.Call(CodeSymbols.Assign, LNode.List(varBinding, input)))));
					}
					input = varBinding;
				}
				if (cmpExpr != null) {
					if (cmpExpr.Value == null)
						PutCond(LNode.Call(CodeSymbols.Eq, LNode.List(input, LNode.Literal(null))).SetStyle(NodeStyle.Operator));
					else
						PutCond(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(cmpExpr, LNode.Id((Symbol) "Equals"))), LNode.List(input)));
				}
				if (inRange != null) {
					bool exclRange = false;
					{
						LNode rangeHi, rangeLo;
						if (inRange.Calls(CodeSymbols.DotDot, 2) && (rangeLo = inRange.Args[0]) != null && (rangeHi = inRange.Args[1]) != null && (exclRange = true)) {
							if (!LooksLikeSimpleValue(input)) {
								Debug.Assert(isType == null);
								PutStmt(TempVarDecl(input, out input));
							}
							PutCond(LNode.Call(CodeSymbols.GE, LNode.List(input, rangeLo)).SetStyle(NodeStyle.Operator));
							PutCond(exclRange ? LNode.Call(CodeSymbols.LT, LNode.List(input, rangeHi)).SetStyle(NodeStyle.Operator) : LNode.Call(CodeSymbols.LE, LNode.List(input, rangeHi)).SetStyle(NodeStyle.Operator));
						} else if (inRange.Calls(CodeSymbols.DotDot, 1) && (rangeHi = inRange.Args[0]) != null && (exclRange = true))
							PutCond(exclRange ? LNode.Call(CodeSymbols.LT, LNode.List(input, rangeHi)).SetStyle(NodeStyle.Operator) : LNode.Call(CodeSymbols.LE, LNode.List(input, rangeHi)).SetStyle(NodeStyle.Operator));
						else
							PutCond(LNode.Call(CodeSymbols.In, LNode.List(input, inRange)).SetStyle(NodeStyle.Operator));
					}
				}
				for (int itemIndex = 0; itemIndex < subPatterns.Count; itemIndex++) {
					var subPattern = subPatterns[itemIndex];
					LNode propName;
					if (subPattern.Calls(S.NamedArg, 2) || subPattern.Calls(S.Colon, 2)) {
						propName = subPattern[0];
						subPattern = subPattern[1];
					} else
						propName = LNode.Id("Item" + (itemIndex + 1), subPattern);
					GenCodeForPattern(LNode.Call(CodeSymbols.Dot, LNode.List(input, propName)), subPattern);
				}
				foreach (var cond in conditions)
					PutCond(cond);
			}
			void GetPatternComponents(LNode pattern, out LNode varBinding, out bool refExistingVar, out LNode cmpExpr, out LNode isType, out LNode inRange, out RVList<LNode> subPatterns, out RVList<LNode> conditions)
			{
				bool haveSubPatterns = false;
				subPatterns = RVList<LNode>.Empty;
				refExistingVar = pattern.AttrNamed(S.Ref) != null;
				conditions = RVList<LNode>.Empty;
				while (pattern.Calls(S.And, 2)) {
					conditions.Add(pattern.Args.Last);
					pattern = pattern.Args[0];
				}
				LNode cmpExprOrBinding = null;
				varBinding = cmpExpr = isType = inRange = null;
				for (int pass = 1; pass <= 3; pass++) {
					LNode inRange2 = inRange, isType2 = isType;
					if (pattern.Calls(CodeSymbols.In, 2) && (cmpExprOrBinding = pattern.Args[0]) != null && (inRange = pattern.Args[1]) != null || pattern.Calls((Symbol) "in", 2) && (cmpExprOrBinding = pattern.Args[0]) != null && (inRange = pattern.Args[1]) != null) {
						pattern = cmpExprOrBinding;
						if (inRange2 != null)
							_context.Write(Severity.Error, inRange2, "match-case does not support multiple 'in' operators");
					} else if (pattern.Calls(CodeSymbols.Is, 2) && (cmpExprOrBinding = pattern.Args[0]) != null && (isType = pattern.Args[1]) != null || pattern.Calls((Symbol) "is", 2) && (cmpExprOrBinding = pattern.Args[0]) != null && (isType = pattern.Args[1]) != null) {
						pattern = cmpExprOrBinding;
						if (isType2 != null)
							_context.Write(Severity.Error, isType2, "match-case does not support multiple 'is' operators");
					} else if (pattern.Calls(CodeSymbols.Is, 1) && (isType = pattern.Args[0]) != null || pattern.Calls((Symbol) "is", 1) && (isType = pattern.Args[0]) != null) {
						if (isType2 != null)
							_context.Write(Severity.Error, isType2, "match-case does not support multiple 'is' operators");
						goto doneAnalysis;
					} else if (pattern.Calls(CodeSymbols.DotDot, 2) && pattern.Args[1].Calls(CodeSymbols.Dot, 1) || pattern.Calls(CodeSymbols.DotDot, 2) || pattern.Calls(CodeSymbols.DotDot, 1) && pattern.Args[0].Calls(CodeSymbols.Dot, 1) || pattern.Calls(CodeSymbols.DotDot, 1)) {
						inRange = pattern;
						goto doneAnalysis;
					} else if (pattern.Calls(CodeSymbols.Tuple)) {
						subPatterns = pattern.Args;
						cmpExprOrBinding = null;
					} else if (pattern.Calls(S.Substitute, 1)) {
						cmpExprOrBinding = pattern;
						varBinding = pattern[0];
						break;
					} else if (!haveSubPatterns && pattern.IsCall && (pattern.Name == GSymbol.Empty || (!pattern.HasSpecialName && LesNodePrinter.IsNormalIdentifier(pattern.Name)))) {
						haveSubPatterns = true;
						subPatterns = pattern.Args;
						pattern = pattern.Target;
					} else {
						cmpExprOrBinding = pattern;
					}
				}
			doneAnalysis:
				if (cmpExprOrBinding != null) {
					if (cmpExprOrBinding.IsId && cmpExprOrBinding.AttrNamed(S.TriviaInParens) == null)
						varBinding = cmpExprOrBinding;
					if (varBinding != null) {
						if (varBinding.AttrNamed(S.Ref) != null) {
							refExistingVar = true;
							varBinding = varBinding.WithoutAttrs();
						}
						if (varBinding.IsIdNamed(__))
							varBinding = cmpExprOrBinding = null;
						else if (!varBinding.IsId) {
							_context.Write(Severity.Error, varBinding, "Invalid variable name in match-case: {0}", varBinding);
							varBinding = null;
						}
					}
					if (varBinding == null)
						cmpExpr = cmpExprOrBinding;
				}
				if (refExistingVar && varBinding == null) {
					refExistingVar = false;
					var got = cmpExprOrBinding ?? pattern;
					_context.Write(Severity.Warning, got, "'ref' expected a variable name (got `{0}`)", got);
				}
			}
			LNode GetOutputAsLNode()
			{
				RWList<LNode> finalOutput = _handler.ToRWList();
				for (int end = _output.Count - 1; end >= 0; end--) {
					Mode mode = _output[end].A;
					LNode code = _output[end].B;
					if (mode == Mode.Condition) {
						int start = end;
						for (; start > 0 && _output[start - 1].A == mode; start--) {
						}
						LNode cond = _output[start].B;
						for (int i = start + 1; i <= end; i++)
							cond = LNode.Call(CodeSymbols.And, LNode.List(cond, _output[i].B)).SetStyle(NodeStyle.Operator);
						end = start;
						finalOutput = new RWList<LNode> { 
							LNode.Call(CodeSymbols.If, LNode.List(cond, finalOutput.ToRVList().AsLNode(S.Braces)))
						};
					} else
						finalOutput.Insert(0, code);
				}
				return finalOutput.ToRVList().AsLNode(S.Braces);
			}
		}
	}
}
