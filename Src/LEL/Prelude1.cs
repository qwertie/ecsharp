using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Math;
using Loyc.Utilities;
using Loyc;

namespace LEL.Prelude
{
	/// <summary>Marks a method as an LEL macro.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class SimpleMacroAttribute : Attribute
	{
	}

	/// <summary>Defines the core, predefined constructs of LEL.</summary>
	public static partial class Prelude
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		static readonly Symbol Error = MessageSink.Error;
		static readonly Symbol Warning = MessageSink.Warning;
		static LNode Reject(IMessageSink error, LNode at, string msg)
		{
			error.Write(Error, at, msg);
			return null;
		}

		[SimpleMacro]
		public static LNode @class(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Class);
		}
		[SimpleMacro]
		public static LNode @struct(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Struct);
		}
		[SimpleMacro]
		public static LNode @enum(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Enum);
		}
		[SimpleMacro]
		public static LNode @trait(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Trait);
		}
		[SimpleMacro]
		public static LNode @alias(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Alias, true);
		}
		[SimpleMacro]
		public static LNode @namespace(LNode node, IMessageSink sink)
		{
			// We can allow whoever processes #namespace to do the validation instead
			//var args = node.Args;
			//LNode name = args.TryGet(0, null), body = args.TryGet(1, null);
			//if (args.Count != 2 || !body.Calls(S.Braces))
			//    return Reject(sink, node, "A namespace definition must have the form namespace(Name, { Body })");
			//if (!IsComplexId(name))
			//    return Reject(sink, node, "Invalid namespace name (expected a complex identifier)");
			return node.WithTarget(S.Namespace);
		}

		public static LNode TranslateSpaceDefinition(LNode node, IMessageSink sink, Symbol newTarget, bool isAlias = false)
		{
			var args = node.Args;
			LNode name = args.TryGet(0, null), body = args.TryGet(1, null), oldName = null, bases;

			if (args.Count != 2 || !body.Calls(S.Braces))
				return Reject(sink, node, "A type definition must have the form kind(Name, { Body }) or kind(Name(Bases), { Body }) (where kind is struct/class/enum/trait/alias)");
			if (isAlias) {
				if (!name.Calls(S.Set, 2) || !IsComplexId(oldName = name.Args[1]))
					return Reject(sink, node, "An alias definition must have the form alias(NewName = OldName, { Body }) or alias(NewName(Interfaces) = OldName, { Body })");
				name = name.Args[0];
			}
			if (IsDefinitionId(name, false))
				bases = F.EmptyList;
			else {
				if (!IsTargetDefinitionId(name, false))
					return Reject(sink, name, "Invalid class name (expected a simple name or Name!(T1,T2,...))");
				foreach (var arg in name.Args)
					if (!IsComplexId(arg))
						return Reject(sink, arg, "Invalid base class name (expected a complex identifier)");
				bases = name.WithTarget(S.List);
				name = name.Target ?? name;
			}
			if (isAlias)
				return node.With(newTarget, name, F.EmptyList, body);
			else
				return node.With(newTarget, F.Call(S.Set, name, oldName), bases, body);
		}

		// A definition identifier has the form Name or Name!(Id,$Id,...)
		// where Id is a simple identifier and Name is either a simple identifier 
		// or (if dots are allowed) a complex identifier with allowOf=false.
		public static bool IsDefinitionId(LNode id, bool allowDots)
		{
			var args = id.Args;
			if (id.CallsMin(S.Of, 1)) {
				if (allowDots ? !IsComplexId(args[0], false) : !args[0].IsId)
					return false;
				for (int i = 1; i < args.Count; i++)
					if (!(args[i].IsId || args[i].Calls(S.Substitute, 1) && args[i].Args[0].IsId))
						return false;
				return true;
			} else
				return allowDots ? IsComplexId(id, false) : args[0].IsId;
		}
		public static bool IsTargetDefinitionId(LNode id, bool allowDots)
		{
			return id.HasSimpleHead() || IsDefinitionId(id.Target, allowDots);
		}
		// A complex identifier has the form Id, ComplexId.Id, or ComplexId!(ComplexId, ...)
		// where Id is a simple identifier and ComplexId is a complex identifier. Also, the
		// form X!Y!Z, i.e. #of(#of(...), ...) is not allowed.
		public static bool IsComplexId(LNode id, bool allowOf = true)
		{
			if (id.IsCall) {
				if (id.Name == S.Of) {
					if (allowOf)
						return (id.HasSimpleHead() || IsComplexId(id.Target, false)) && id.Args.All(a => IsComplexId(a));
					return false;
				} else if (id.Calls(S.Dot, 2)) {
					return id.Args.Last.IsId && IsComplexId(id.Args[0]);
				} else
					return false;
			} else
				return id.IsId;
		}

		// Method decl syntaxes:
		//
		// def Square([ref] x::int) -> int { x *= x; }
		// def Square(x::int) -> int { return x*x; }
		// def Square ==> X.Y;
		// prop int Foo { _foo }
		// prop int Foo { _foo }
		// 
		// body =~ $foo + $bar
		//
		// match node.Args {
		//   ($name, ($..args) -> $ret, { $body }) `where` !name.IsCall || name.CallsMin(S.Of, 1) || name.Calls(S.Dot, 2);
		//   ($name, ($args), { $body });
		//   ($name($args), $(body!{ $.._ });
		//   => {
		//     return sub(
		//   }
		//    => action
		//   (def $name($args)) `where` (name 
		// };

		[SimpleMacro]
		public static LNode @def(LNode node, IMessageSink sink)
		{
			var parts = node.Args;
			LNode sig = parts.TryGet(0, null), body = parts.TryGet(1, null);
			if (!parts.Count.IsInRange(1, 2) || !sig.IsCall || (body != null && !body.Calls(S.Braces))) {
				return Reject(sink, node, "A method definition must have the form def(Name(Args), { Body }), def(Name(Args) -> ReturnType, { Body }) or def(Name ==> ForwardingTarget, { Body })");
			}
			LNode forwardTo = null, retVal = null;
			if (sig.Calls(S.Forward, 2)) {
				forwardTo = sig.Args[1];
				sig = sig.Args[0];
				if (body != null)
					return Reject(sink, sig.Target, "Cannot use ==> and a method body {...} at the same time.");
			}
			if (sig.Calls(S._RightArrow, 2)) {
				retVal = sig.Args[1];
				sig = sig.Args[0];
			}
			if (retVal.Calls(S.Braces) && body == null) {
				body = retVal;
				retVal = F.Id(S.Auto);
			}
			var name = sig.Target;
			if (!IsDefinitionId(sig, true))
				return Reject(sink, sig.Target, "Invalid method name");
			var argList = sig.WithTarget(S.List);

			if (body != null)
				return node.With(S.Def, retVal ?? F.Void, name, argList, body);
			else if (forwardTo != null)
				return node.With(S.Def, retVal ?? F.Void, name, argList, F.Call(S.Forward, forwardTo));
			else
				return node.With(S.Def, retVal ?? F.Void, name, argList);
		}

		
	}
}
