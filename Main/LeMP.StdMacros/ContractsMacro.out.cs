// Generated from ContractsMacro.ecs by LeMP custom tool. LeMP version: 1.7.0.0
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
namespace LeMP
{
	partial class StandardMacros
	{
		static readonly Symbol sy_notnull = (Symbol) "notnull", sy_ensuresOnThrow = (Symbol) "ensuresOnThrow", sy_requires = (Symbol) "requires", sy_assert = (Symbol) "assert", sy_ensures = (Symbol) "ensures", sy_ensuresAssert = (Symbol) "ensuresAssert", sy_ensuresFinally = (Symbol) "ensuresFinally", sy__numassertMethodForRequires = (Symbol) "#assertMethodForRequires", sy__numassertMethodForEnsures = (Symbol) "#assertMethodForEnsures", sy__numassertMethodForEnsuresFinally = (Symbol) "#assertMethodForEnsuresFinally", sy__numexceptionTypeForEnsuresOnThrow = (Symbol) "#exceptionTypeForEnsuresOnThrow";
		static readonly Symbol _haveContractRewriter = (Symbol) "#haveContractRewriter";
		static bool SetScopedProperty<T>(LNode literal, IMacroContext context, object key)
		{
			if (literal != null) {
				var value = literal.Value;
				if (value is T) {
					context.ScopedProperties[key] = (T) value;
					return true;
				}
			}
			Reject(context, literal, "Expected a literal of type {0}.".Localized(typeof(T).Name));
			return false;
		}
		[LexicalMacro("notnull T method(notnull T arg) {...}; T method([requires(expr)] T arg) {...}; " + "[requires(expr)] T method(...) {...}; [ensures(expr)] T method(...) {...}; " + "[ensuresOnThrow(expr)] T method(...) {...}; [ensuresOnThrow<Exception>(expr)] T method(...) {...}", "Generates Contract checks in a method.\n\n" + "- [requires(expr)] and [assert(expr)] specify an expression that must be true at the beginning of the method; assert conditions are checked in debug builds only, while \"requires\" conditions are checked in all builds. The condition can include an underscore `_` that refers to the argument that the attribute is attached to, if any.\n" + "- [ensures(expr)] and [ensuresAssert(expr)] specify an expression that must be true if-and-when the method returns normally. assert conditions are checked in debug builds only. The condition can include an underscore `_` that refers to the return value of the method.\n" + "- [ensuresFinally(expr)] specifies an expression that must be true when the method exits, whether by exception or by a normal return. This is implemented by wrapping the method in a try-finally block.\n" + "- [ensuresOnThrow(expr)] and [ensuresOnThrow<ExceptionType>(expr)] specify a condition that must be true if the method throws an exception. When #haveContractRewriter is false, underscore `_` refers to the thrown exception object; this is not available in the MS Code Contracts Rewriter.\n" + "- notnull is equivalent to [requires(_ != null)] if applied to an argument, and [ensures(_ != null)] if applied to the method as a whole.\n" + "\nAll contract attributes (except notnull) can specify multiple expressions separated by commas, to produce multiple checks, each with its own error message.", "#fn", "#cons", Mode = MacroMode.Passive | MacroMode.PriorityInternalFallback)] public static LNode ContractsOnMethod(LNode fn, IMacroContext context)
		{
			LNode fnArgs, oldFn = fn;
			if (fn.ArgCount >= 4) {
				var rw = new CodeContractRewriter(fn.Args[0], fn.Args[1], context);
				fn = ProcessArgContractAttributes(fn, 2, rw);
				if (fn.Args[0].HasAttrs)
					fn = fn.WithArgChanged(0, fn.Args[0].WithAttrs(rw.Process(fn.Args[0].Attrs, null)));
				if (fn.HasAttrs)
					fn = fn.WithAttrs(rw.Process(fn.Attrs, null));
				if (rw.PrependStmts.IsEmpty) {
					return null;
				} else {
					var body = fn.Args[3];
					if (!body.Calls(S.Braces))
						body = LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(body)))).SetStyle(NodeStyle.Statement);
					body = body.WithArgs(body.Args.InsertRange(0, rw.PrependStmts));
					fn = fn.WithArgChanged(3, body);
					return fn;
				}
			}
			return null;
		}
		static readonly LNode Id_lambda_function = LNode.Id((Symbol) "lambda_function");
		[LexicalMacro("([notnull] (x => ...)); ([notnull] x) => ...; ([requires(expr)] x) => ...; " + "([ensures(expr)] (x => ...)); ([ensuresOnThrow(expr)] (x => ...)); ", "Generates Contract checks in a lambda function. See the documentation of " + "ContractsOnMethod for more information about the contract attributes.", "=>", Mode = MacroMode.Passive | MacroMode.PriorityInternalFallback)] public static LNode ContractsOnLambda(LNode fn, IMacroContext context)
		{
			LNode oldFn = fn;
			if (fn.ArgCount == 2) {
				var rw = new CodeContractRewriter(LNode.Missing, Id_lambda_function, context);
				fn = ProcessArgContractAttributes(fn, 0, rw, isLambda: true);
				if (fn.HasAttrs)
					fn = fn.WithAttrs(rw.Process(fn.Attrs, null));
				if (rw.PrependStmts.IsEmpty) {
					return null;
				} else {
					var body = fn.Args[1];
					if (!body.Calls(S.Braces))
						body = LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(body)))).SetStyle(NodeStyle.Statement);
					body = body.WithArgs(body.Args.InsertRange(0, rw.PrependStmts));
					fn = fn.WithArgChanged(1, body);
					return fn;
				}
			}
			return null;
		}
		static readonly LNode Id_value = LNode.Id(CodeSymbols.value);
		[LexicalMacro("notnull T Prop {...}; T this[[requires(expr)] T arg] {...}; " + "T Prop { [requires(expr)] set; }; [ensures(expr)] T Prop {...}; " + "[ensuresOnThrow(expr)] T Prop {...}; [ensuresOnThrow<Exception>(expr)] T Prop {...}", "Generates contract checks in a property. You can apply contract attributes to " + "the property itself, to the getter, to the setter, or all three. When the [requires] " + "or [assert] attributes are applied to the property itself, they are treated as if " + "they were applied to the getter; but when the [ensures], [ensuresAssert], notnull, " + "and [ensuresOnThrow] attributes are applied to the property itself, they are treated " + "as if they were applied to both the getter and the setter separately.", "#property", Mode = MacroMode.Passive | MacroMode.PriorityInternalFallback)] public static LNode ContractsOnProperty(LNode prop, IMacroContext context)
		{
			LNode propArgs, oldProp = prop;
			if (prop.ArgCount == 4) {
				LNode braces = prop[3];
				var oldBraces = braces;
				var rw = new CodeContractRewriter(prop.Args[0], prop.Args[1], context);
				prop = ProcessArgContractAttributes(prop, 2, rw);
				VList<LNode> cAttrs = LNode.List();
				prop = prop.WithArgChanged(0, GrabContractAttrs(prop.Args[0], ref cAttrs, ContractAppliesTo.Getter));
				prop = GrabContractAttrs(prop, ref cAttrs);
				LNode getter = null, setter = null;
				int getterIndex = -1, setterIndex = -1;
				VList<LNode> getterAttrs = LNode.List(), setterAttrs = LNode.List();
				bool isLambdaProperty = !braces.Calls(S.Braces);
				if (isLambdaProperty) {
					if (cAttrs.Count == 0)
						return null;
					getterAttrs = cAttrs;
					getter = LNode.Call(CodeSymbols.get, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(braces)))).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special);
					braces = LNode.Call(CodeSymbols.Braces, LNode.List(getter)).SetStyle(NodeStyle.Statement);
					getterIndex = 0;
				} else {
					for (int i = 0; i < braces.Args.Count; i++) {
						var part = braces.Args[i];
						if (part.Calls(S.get)) {
							getter = part;
							getterIndex = i;
						}
						if (part.Calls(S.set)) {
							setter = part;
							setterIndex = i;
						}
					}
					if (cAttrs.Count != 0) {
						getterAttrs = cAttrs.Where(a => (PropertyContractInterpretation(a) & ContractAppliesTo.Getter) != 0);
						setterAttrs = cAttrs.Where(a => (PropertyContractInterpretation(a) & ContractAppliesTo.Setter) != 0);
					}
				}
				var sharedPrependStmts = rw.PrependStmts;
				if (getter != null) {
					getter = GrabContractAttrs(getter, ref getterAttrs);
					rw.Process(getterAttrs, null);
					rw.PrependStmtsToGetterOrSetter(ref braces, getterIndex, getter);
				}
				if (setter != null) {
					rw.PrependStmts = sharedPrependStmts;
					setter = GrabContractAttrs(setter, ref setterAttrs);
					rw.Process(setterAttrs, LNode.Id(CodeSymbols.value), true);
					rw.PrependStmtsToGetterOrSetter(ref braces, setterIndex, setter);
				}
				if (braces == oldBraces)
					return null;
				else
					return prop.WithArgChanged(3, braces);
			}
			return null;
		}
		static LNode ProcessArgContractAttributes(LNode fn, int argsIndex, CodeContractRewriter rw, bool isLambda = false)
		{
			LNode fnArgs = fn.Args[argsIndex];
			if (fnArgs.CallsMin(isLambda ? S.Tuple : S.AltList, 1)) {
				return fn.WithArgChanged(argsIndex, fnArgs.WithArgs(arg => {
					if (arg.HasAttrs)
						return arg.WithAttrs(rw.Process(arg.Attrs, GetVarName(arg)));
					return arg;
				}));
			} else if (isLambda) {
				var arg = fnArgs;
				if (arg.HasAttrs)
					fn = fn.WithArgChanged(argsIndex, arg.WithAttrs(rw.Process(arg.Attrs, GetVarName(arg))));
			}
			return fn;
		}
		static LNode GrabContractAttrs(LNode node, ref VList<LNode> cAttrs, ContractAppliesTo kinds = ContractAppliesTo.Both)
		{
			if (node.HasAttrs) {
				VList<LNode> cAttrs2 = cAttrs;
				var r = node.WithAttrs(attr => {
					if ((PropertyContractInterpretation(attr) & kinds) != 0) {
						cAttrs2.Add(attr);
						return NoValue.Value;
					}
					return attr;
				});
				cAttrs = cAttrs2;
				return r;
			}
			return node;
		}
		static bool ReplaceContractUnderscore(ref LNode condition, LNode variableName)
		{
			bool hasUnderscore = false;
			LNode old = condition;
			condition = condition.ReplaceRecursive(n => {
				if (n.IsIdNamed(__) || n.IsIdNamed(_hash)) {
					hasUnderscore = true;
					return variableName;
				}
				return null;
			});
			return hasUnderscore;
		}
		static LNode GetVarName(LNode arg)
		{
			{
				LNode tmp_0 = null, variableName;
				if (arg.Calls(CodeSymbols.Var, 2) && (tmp_0 = arg.Args[1]) != null && tmp_0.Calls(CodeSymbols.Assign, 2) && (variableName = tmp_0.Args[0]) != null || arg.Calls(CodeSymbols.Var, 2) && (variableName = arg.Args[1]) != null)
					return variableName;
				else
					return arg.WithoutAttrs();
			}
		}
		static readonly Symbol __notnull = (Symbol) "#notnull";
		static readonly LNode Id_return_value = LNode.Id((Symbol) "return_value");
		static readonly LNode Id__exception__ = LNode.Id((Symbol) "__exception__");
		[Flags] enum ContractAppliesTo
		{
			Getter = 1, Setter = 2, Both = 3, Neither = 0
		}
		;
		static ContractAppliesTo PropertyContractInterpretation(LNode attribute)
		{
			LNode _;
			var mode = GetContractAttrMode(attribute, out _);
			if (mode == null)
				return ContractAppliesTo.Neither;
			if (mode == sy_notnull || mode == sy_ensuresOnThrow)
				return ContractAppliesTo.Both;
			else if (mode == sy_requires || mode == sy_assert)
				return ContractAppliesTo.Setter;
			else if (attribute.Args.Any(cond => ReplaceContractUnderscore(ref cond, null)))
				return ContractAppliesTo.Getter;
			else
				return ContractAppliesTo.Both;
		}
		static Symbol GetContractAttrMode(LNode attr, out LNode exceptionType)
		{
			var mode = attr.Name;
			exceptionType = null;
			if (!attr.HasSimpleHead()) {
				var target = attr.Target;
				if (target.Calls(S.Of, 2) && target.Args[0].IsIdNamed(sy_ensuresOnThrow)) {
					exceptionType = target.Args[1];
					mode = sy_ensuresOnThrow;
				}
			}
			if (mode == __notnull)
				mode = sy_notnull;
			if (mode == sy_ensuresOnThrow || mode == sy_requires || mode == sy_notnull || mode == sy_assert || mode == sy_ensures || mode == sy_ensuresAssert || mode == sy_ensuresFinally)
				return mode;
			return null;
		}
		private class CodeContractRewriter
		{
			internal VList<LNode> PrependStmts;
			private LNode ReturnType;
			private LNode FullMethodName;
			private IMacroContext Context;
			internal CodeContractRewriter(LNode returnType, LNode fullMethodName, IMacroContext context)
			{
				ReturnType = returnType;
				FullMethodName = fullMethodName;
				Context = context;
				PrependStmts = new VList<LNode>();
			}
			internal VList<LNode> Process(VList<LNode> attributes, LNode variableName, bool isPropSetter = false)
			{
				return attributes.Where(attr => {
					LNode exceptionType = null;
					var mode = GetContractAttrMode(attr, out exceptionType);
					if (mode != null) {
						ProcessAttribute(attr, mode, exceptionType, variableName, isPropSetter);
						return false;
					}
					return true;
				});
			}
			bool _haveCCRewriter;
			void ProcessAttribute(LNode attr, Symbol mode, LNode exceptionType, LNode variableName, bool isPropSetter)
			{
				var conditions = attr.Args;
				object haveCCRewriter = Context.ScopedProperties.TryGetValue(_haveContractRewriter, null);
				_haveCCRewriter = haveCCRewriter is bool ? (bool) haveCCRewriter : false;
				if (mode == sy_notnull) {
					if (attr.Args.Count != 0)
						Context.Write(Severity.Warning, attr, "'#notnull' does not expect arguments.");
					if (variableName != null)
						mode = sy_requires;
					else
						mode = sy_ensures;
					conditions.Add(LNode.Call(CodeSymbols.Neq, LNode.List(LNode.Id((Symbol) "_"), LNode.Literal(null))).SetStyle(NodeStyle.Operator));
				} else if (!attr.IsCall) {
					Context.Write(Severity.Warning, attr, "'{0}' expects a list of conditions.", attr.Name);
				}
				if (mode == sy_requires || mode == sy_assert)
					ProcessRequiresAttribute(conditions, mode, variableName);
				else {
					if (variableName != null && !isPropSetter)
						Context.Write(Severity.Error, attr, "The '{0}' attribute does not apply to method arguments.", mode);
					else
						ProcessEnsuresAttribute(conditions, mode, exceptionType, variableName);
				}
			}
			void ProcessRequiresAttribute(VList<LNode> conditions, Symbol mode, LNode variableName)
			{
				foreach (var condition_ in conditions) {
					LNode condition = condition_;
					LNode conditionStr;
					if (ReplaceContractUnderscore(ref condition, variableName))
						if (variableName == null)
							Context.Write(Severity.Error, condition, "`{0}`: underscore has no meaning in this location.", mode);
					if (mode == sy_assert) {
						PrependStmts.Add(LNode.Call((Symbol) "assert", LNode.List(condition)));
					} else if (_haveCCRewriter) {
						PrependStmts.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Requires"))), LNode.List(condition)));
					} else {
						conditionStr = ConditionToStringLit(condition, "Precondition failed: {1}");
						PrependStmts.Add(LNode.Call(GetAssertMethodForRequires(), LNode.List(condition, conditionStr)));
					}
				}
			}
			void ProcessEnsuresAttribute(VList<LNode> conditions, Symbol mode, LNode exceptionType, LNode variableName)
			{
				bool haveCCRewriter = _haveCCRewriter && mode != sy_ensuresAssert && mode != sy_ensuresFinally;
				var checks = LNode.List();
				foreach (var condition_ in conditions) {
					LNode condition = condition_;
					LNode conditionStr;
					LNode contractResult = null;
					string underscoreError = null;
					if (mode == sy_ensuresOnThrow) {
						contractResult = Id__exception__;
						if (haveCCRewriter)
							underscoreError = "`ensuresOnThrow` does not support `_` in MS Code Contracts mode.";
					} else {
						contractResult = haveCCRewriter ? LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Result"))), ReturnType))) : Id_return_value;
						if (mode == sy_ensuresFinally)
							underscoreError = "The macro for `{0}` does not support `_` because the return value is not available in `finally`";
						else if (haveCCRewriter && ReturnType.IsIdNamed(S.Missing))
							underscoreError = "The macro for `{0}` does not support `_` in this context when MS Code Contracts are enabled, because the return type is unknown.";
						bool changed = ReplaceContractUnderscore(ref condition, contractResult);
					}
					if (ReplaceContractUnderscore(ref condition, contractResult) && underscoreError != null)
						Context.Write(Severity.Error, condition, underscoreError, mode);
					if (haveCCRewriter) {
						if (mode == sy_ensuresOnThrow)
							checks.Add(exceptionType != null ? LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "EnsuresOnThrow"))), exceptionType)), LNode.List(condition)) : LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "EnsuresOnThrow"))), LNode.List(condition)));
						else
							checks.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Ensures"))), LNode.List(condition)));
					} else {
						conditionStr = ConditionToStringLit(condition, mode == sy_ensuresOnThrow ? "Postcondition failed after throwing an exception: {1}" : "Postcondition failed: {1}");
						if (mode == sy_ensuresOnThrow) {
							var excType = GetExceptionTypeForEnsuresOnThrow();
							checks.Add(LNode.Call(CodeSymbols.If, LNode.List(LNode.Call(CodeSymbols.Not, LNode.List(condition)).SetStyle(NodeStyle.Operator), LNode.Call(CodeSymbols.Throw, LNode.List(LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(excType, LNode.List(conditionStr, Id__exception__)))))))));
						} else {
							LNode assertMethod;
							if (mode == sy_ensuresAssert)
								assertMethod = GetAssertMethod(Context);
							else if (mode == sy_ensuresFinally)
								assertMethod = GetAssertMethodForEnsuresFinally();
							else
								assertMethod = GetAssertMethodForEnsures();
							checks.Add(LNode.Call(assertMethod, LNode.List(condition, conditionStr)));
						}
					}
				}
				if (checks.Count > 0) {
					if (_haveCCRewriter) {
						PrependStmts.AddRange(checks);
					} else if (mode == sy_ensuresOnThrow) {
						LNode excSpec = exceptionType == null ? Id__exception__ : LNode.Call(CodeSymbols.Var, LNode.List(exceptionType, Id__exception__));
						PrependStmts.Add(LNode.Call((Symbol) "on_throw", LNode.List(excSpec, LNode.Call(CodeSymbols.Braces, LNode.List(checks)).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special));
					} else if (mode == sy_ensuresFinally) {
						PrependStmts.Add(LNode.Call((Symbol) "on_finally", LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(checks)).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special));
					} else {
						PrependStmts.Add(LNode.Call((Symbol) "on_return", LNode.List(Id_return_value, LNode.Call(CodeSymbols.Braces, LNode.List(checks)).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special));
					}
				}
			}
			static readonly LNode defaultContractAssert = LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Assert")));
			LNode GetAssertMethodForRequires()
			{
				return (Context.ScopedProperties.TryGetValue(sy__numassertMethodForRequires, null)as LNode) ?? defaultContractAssert;
			}
			LNode GetAssertMethodForEnsures()
			{
				return (Context.ScopedProperties.TryGetValue(sy__numassertMethodForEnsures, null)as LNode) ?? defaultContractAssert;
			}
			LNode GetAssertMethodForEnsuresFinally()
			{
				return (Context.ScopedProperties.TryGetValue(sy__numassertMethodForEnsuresFinally, null)as LNode) ?? defaultContractAssert;
			}
			LNode GetExceptionTypeForEnsuresOnThrow()
			{
				return (Context.ScopedProperties.TryGetValue(sy__numexceptionTypeForEnsuresOnThrow, null)as LNode) ?? LNode.Id((Symbol) "InvalidOperationException");
			}
			LNode ConditionToStringLit(LNode condition, string formatStr)
			{
				var methodName = FullMethodName;
				while (methodName.CallsMin(S.Dot, 2))
					methodName = methodName.Args.Last;
				var ps = ParsingService.Current;
				return F.Literal(string.Format(formatStr, ps.Print(methodName, Context.Sink, ParsingService.Exprs), ps.Print(condition, Context.Sink, ParsingService.Exprs)));
			}
			public void PrependStmtsToGetterOrSetter(ref LNode braces, int getterIndex, LNode getter)
			{
				if (!PrependStmts.IsEmpty) {
					if (getter.ArgCount == 0) {
						Context.Write(Severity.Error, getter, "`{0}`: contracts cannot be applied to autoproperties. " + "A body is required, but you can use [field] on the property to add a body to `get` and `set` automatically.", getter);
						return;
					} else if (getter.ArgCount == 1) {
						var body = getter.Args[0];
						if (!body.Calls(S.Braces))
							body = LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(body)))).SetStyle(NodeStyle.Statement);
						body = body.WithArgs(body.Args.InsertRange(0, PrependStmts));
						getter = getter.WithArgs(body);
						braces = braces.WithArgChanged(getterIndex, getter);
					}
				}
			}
		}
	}
}
