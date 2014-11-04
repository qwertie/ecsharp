using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LeMP
{
	using S = CodeSymbols;
	using System.Diagnostics;

	public partial class StandardMacros
	{
		static readonly Symbol _hash = GSymbol.Get("#");

		[SimpleMacro("Type Fn(Type param) ==> target;", "Forward a call to another method", "#fn", Mode = MacroMode.Passive)]
		public static LNode ForwardMethod(LNode fn, IMessageSink sink)
		{
			LNode args, fwd, body;
			if (fn.ArgCount != 4 || !(fwd = fn.Args[3]).Calls(S.Forward, 1) || !(args = fn.Args[2]).Calls(S.List))
				return null;
			
			RVList<LNode> formalArgs = args.Args;
			RVList<LNode> argList =	RVList<LNode>.Empty;
			foreach (var formalArg in formalArgs)
			{
				if (!formalArg.Calls(S.Var, 2))
					return Reject(sink, formalArg, "'==>' expected a variable declaration here");
				LNode argName = formalArg.Args[1];
				if (argName.Calls(S.Assign, 2))
					argName = argName.Args[0];
				LNode @ref = formalArg.AttrNamed(S.Ref) ?? formalArg.AttrNamed(S.Out);
				if (@ref != null)
					argName = argName.PlusAttr(@ref);
				argList.Add(argName);
			}

			LNode target = GetForwardingTarget(fwd, fn.Args[1]);
			LNode call = F.Call(target, argList);
			
			bool isVoidFn = fn.Args[0].IsIdNamed(S.Void);
			body = F.Braces(isVoidFn ? call : F.Call(S.Return, call));
			return fn.WithArgChanged(3, body);
		}

		[SimpleMacro("Type Prop ==> target; Type Prop { get ==> target; set ==> target; }", "Forward property getter and/or setter", "#property", Mode = MacroMode.Passive)]
		public static LNode ForwardProperty(LNode prop, IMessageSink sink)
		{
			LNode name, fwd, body;
			if (prop.ArgCount != 3)
				return null;
			LNode target = GetForwardingTarget(fwd = prop.Args[2], name = prop.Args[1]);
			if (target != null)
			{
				body = F.Braces(new RVList<LNode>(
					F.Call(S.get, F.Braces(F.Call(S.Return, target))),
					F.Call(S.set, F.Braces(F.Call(S.Assign, target, F.Id(S.value))))));

				return prop.WithArgChanged(2, body);
			}
			else if ((body = fwd).Calls(S.Braces))
			{
				var body2 = body.WithArgs(stmt => {
					if (stmt.Calls(S.get, 1) && (target = GetForwardingTarget(stmt.Args[0], name)) != null)
						return stmt.WithArgs(new RVList<LNode>(F.Braces(F.Call(S.Return, target))));
					if (stmt.Calls(S.set, 1) && (target = GetForwardingTarget(stmt.Args[0], name)) != null)
						return stmt.WithArgs(new RVList<LNode>(F.Braces(F.Call(S.Assign, target, F.Id(S.value)))));
					return stmt;
				});
				if (body2 != body)
					return prop.WithArgChanged(2, body2);
			}
			return null;
		}
		static LNode GetForwardingTarget(LNode fwd, LNode methodName)
		{
			if (fwd.Calls(S.Forward, 1)) {
				LNode target = fwd.Args[0];
				if (target.Calls(S.Dot, 2) && target.Args[1].IsIdNamed(_hash))
					return target.WithArgChanged(1, target.Args[1].WithName(
						Ecs.EcsNodePrinter.KeyNameComponentOf(methodName)));
				return target;
			} else
				return null;
		}
	}
}
