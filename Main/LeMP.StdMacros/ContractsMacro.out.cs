// Generated from ContractsMacro.ecs by LeMP custom tool. LeMP version: 1.6.1.0
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
		static readonly Symbol sy_haveContractRewriter = (Symbol) "haveContractRewriter", sy_notnull = (Symbol) "notnull", sy_ensuresOnThrow = (Symbol) "ensuresOnThrow", sy_requires = (Symbol) "requires", sy_assert = (Symbol) "assert", sy_ensures = (Symbol) "ensures", sy_assertEnsures = (Symbol) "assertEnsures", sy_HaveCodeContractsRewriter = (Symbol) "HaveCodeContractsRewriter";
		[LexicalMacro("#haveContractRewriter(true)", "Sets a flag to indicate that your build process includes the Microsoft Code Contracts binary rewriter, so\n\n" + "- [requires(condition)] will be rewritten as `Contract.Requires(condition)` instead of Contract.Requires(condition, s) where s is a string that includes the method name and condition.", "- [ensures(condition)] will be rewritten as `Contract.Ensures(condition)` instead of `on_return(return_value) { Contract.Assert(condition, s); }`.", "- [ensuresOnThrow(condition)] will be rewritten as Contract.EnsuresOnThrow(condition) instead of `on_throw(__exception__) { Contract.Assert(condition, s); }`.", "#haveContractRewriter")] public static LNode haveContractRewriter(LNode node, IMacroContext context)
		{
			if (node.ArgCount <= 1) {
				if (SetScopedProperty<bool>(node.Args[0, F.@true], context, sy_haveContractRewriter))
					return LNode.Call(CodeSymbols.Splice);
			}
			return null;
		}
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
		[LexicalMacro("notnull T method(notnull T arg) {...}; T method([requires(expr)] T arg) {...}; " + "[requires(expr)] T method(...) {...}; [ensures(expr)] T method(...) {...}; " + "[ensuresOnThrow(expr)] T method(...) {...}; [ensuresOnThrow<Exception>(expr)] T method(...) {...}", "Generates Contract checks in a method.\n\n" + "- [requires(expr)] and [assert(expr)] specifies an expression that must be true at the beginning of the method; assert conditions are checked in debug builds only. The condition can include an underscore `_` that refers to the argument that the attribute is attached to, if any.\n", "- [ensures(expr)] and [assertEnsures(expr)] specifies an expression that must be true if-and-when the method returns normally. assert conditions are checked in debug builds only. The condition can include an underscore `_` that refers to the return value of the method.\n", "- [ensuresOnThrow(expr)] and [ensuresOnThrow<ExceptionType>(expr)] specify a condition that must be true if the method throws an exception. When #haveContractRewriter is false, underscore `_` refers to the thrown exception object; this is not available in the MS Code Contracts Rewriter.\n", "- notnull is equivalent to [requires(_ != null)] if applied to an argument, and [ensures(_ != null)] if applied to the method as a whole.\n", "\nAll contract attributes (except notnull) can specify multiple expressions separated by commas, to produce multiple checks, each with its own error message.", "#fn", "#cons", Mode = MacroMode.Passive | MacroMode.PriorityInternalFallback)] public static LNode ContractsOnMethod(LNode fn, IMacroContext context)
		{
			LNode fnArgs, oldFn = fn;
			if (fn.ArgCount >= 4) {
				var rw = new CodeContractRewriter(fn.Args[0], fn.Args[1], context);
				if ((fnArgs = fn.Args[2]).Calls(S.AltList)) {
					fn = fn.WithArgChanged(2, fnArgs.WithArgs(arg => {
						if (arg.HasAttrs)
							return arg.WithAttrs(rw.Process(arg.Attrs, GetVarName(arg)));
						return arg;
					}));
				}
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
		static readonly LNode Id_value = LNode.Id(CodeSymbols.value);
		[LexicalMacro("notnull T Prop {...}; T this[[requires(expr)] T arg] {...}; " + "T Prop { [requires(expr)] set; }; [ensures(expr)] T Prop {...}; " + "[ensuresOnThrow(expr)] T Prop {...}; [ensuresOnThrow<Exception>(expr)] T Prop {...}", "Generates contract checks in a property. You can apply contract attributes to " + "the property itself, to the getter, to the setter, or all three. When the [requires] " + "or [assert] attributes are applied to the property itself, they are treated as if " + "they were applied to the getter; but when the [ensures], [assertEnsures], notnull, " + "and [ensuresOnThrow] attributes are applied to the property itself, they are treated " + "as if they were applied to both the getter and the setter separately.", "#property", Mode = MacroMode.Passive | MacroMode.PriorityInternalFallback)] public static LNode ContractsOnProperty(LNode prop, IMacroContext context)
		{
			LNode propArgs, oldProp = prop;
			if (prop.ArgCount == 4) {
				LNode braces = prop[3];
				var oldBraces = braces;
				var rw = new CodeContractRewriter(prop.Args[0], prop.Args[1], context);
				if ((propArgs = prop.Args[2]).Calls(S.AltList) && propArgs.ArgCount > 0) {
					prop = prop.WithArgChanged(2, propArgs.WithArgs(arg => {
						if (arg.HasAttrs)
							return arg.WithAttrs(rw.Process(arg.Attrs, GetVarName(arg)));
						return arg;
					}));
				}
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
					return arg;
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
			if (attr.Calls(S.Of, 2) && attr.Args[0].IsIdNamed(sy_ensuresOnThrow)) {
				exceptionType = attr.Args[1];
				mode = sy_ensuresOnThrow;
			}
			if (mode == __notnull)
				mode = sy_notnull;
			if (mode == sy_ensuresOnThrow || mode == sy_requires || mode == sy_notnull || mode == sy_ensures || mode == sy_assert || mode == sy_assertEnsures)
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
				object haveCCRewriter = Context.ScopedProperties.TryGetValue(sy_HaveCodeContractsRewriter, null);
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
						conditionStr = ConditionToStringLit(condition, "`{0}` requires `{1}`");
						PrependStmts.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Requires"))), LNode.List(condition, conditionStr)));
					}
				}
			}
			void ProcessEnsuresAttribute(VList<LNode> conditions, Symbol mode, LNode exceptionType, LNode variableName)
			{
				bool haveCCRewriter = _haveCCRewriter && mode != sy_assertEnsures;
				var checks = LNode.List();
				foreach (var condition_ in conditions) {
					LNode condition = condition_;
					LNode conditionStr;
					LNode contractResult = null;
					if (mode == sy_ensuresOnThrow) {
						if (!haveCCRewriter) {
							contractResult = Id__exception__;
						} else if (ReplaceContractUnderscore(ref condition, null))
							Context.Write(Severity.Error, condition, "`ensuresOnThrow` does not support `_` in MS Code Contracts mode.");
					} else {
						contractResult = haveCCRewriter ? LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Result"))), ReturnType))) : Id_return_value;
						ReplaceContractUnderscore(ref condition, contractResult);
					}
					if (haveCCRewriter) {
						if (mode == sy_ensuresOnThrow)
							checks.Add(exceptionType != null ? LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "EnsuresOnThrow"))), exceptionType)), LNode.List(condition)) : LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "EnsuresOnThrow"))), LNode.List(condition)));
						else
							checks.Add(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Ensures"))), LNode.List(condition)));
					} else {
						conditionStr = ConditionToStringLit(condition, mode == sy_ensuresOnThrow ? "`{0}` did not ensure-on-throw `{1}`" : "`{0}` did not ensure `{1}`");
						var assertMethod = mode == sy_assertEnsures ? GetAssertMethod(Context) : LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "Contract"), LNode.Id((Symbol) "Assert")));
						checks.Add(LNode.Call(assertMethod, LNode.List(condition, conditionStr)));
					}
				}
				if (checks.Count > 0) {
					if (_haveCCRewriter) {
						PrependStmts.AddRange(checks);
					} else if (mode == sy_ensuresOnThrow) {
						LNode excSpec = exceptionType == null ? Id__exception__ : LNode.Call(CodeSymbols.Var, LNode.List(exceptionType, Id__exception__));
						PrependStmts.Add(LNode.Call((Symbol) "on_throw", LNode.List(excSpec, LNode.Call(CodeSymbols.Braces, LNode.List(checks)).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special));
					} else {
						PrependStmts.Add(LNode.Call((Symbol) "on_return", LNode.List(Id_return_value, LNode.Call(CodeSymbols.Braces, LNode.List(checks)).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special));
					}
				}
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
