using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Syntax;
using Loyc.Collections;
using S = Loyc.Syntax.CodeSymbols;
using Loyc.Math;

namespace LEL.Prelude
{
	/// <summary>Marks a method as an LEL macro.</summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class MacroAttribute : Attribute
	{
	}

	/// <summary>Defines the core, predefined constructs of LEL.</summary>
	public static partial class Prelude
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		/*


		[Macro]
		public static LNode @class(RVList<LNode> args)
		{
			if (!args.Count.IsInRange(2, 3)) return Error("A class definition needs 2-3 arguments: (class_name, { body }) or (class_name, bases, { body })");
			if (!args.Last.Calls(S.Braces)) return Error("A class definition must end with a braced block.");
			LNode name, bases, error, retVal = F._Missing;
			int i = 0;
			if ((error = DecodeSignature(args, ref i, out name, out bases, ref retVal)) != null)
				return error;
		}

		// Class decl syntaxes:
		//
		// class Derived (Base) { ... }
		// class Derived(Base) { ... }
		//
		// Method decl syntaxes:
		//
		// def Square(x::int) -> int { return x*x; }
		// def Square([ref] x::int) { x *= x; }
		// def Square (x::int) -> int { return x*x; }
		// def Square ([ref] x::int) { x *= x; }
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

		private static LNode DecodeSignature(RVList<LNode> allArgs, ref int i, out LNode name, out RVList<LNode> args, ref LNode retVal)
		{
			name = allArgs[i];
			args = RVList<LNode>.Empty;

			// Detect name(args)->ret
			bool done_i = false;
			if (name.Calls(S._RightArrow, 2)) {
				if (retVal != null) return Error("Arrow '->' not expected here");
				retVal = name.Args[1];
				name = name.Args[0];
				i++;
				done_i = true;
			}

			// Detect name(args)
			if (name.IsCall && !name.CallsMin(S.Of, 1) && !name.Calls(S.Dot, 2)) {
				args = name.Args;
				name = name.Target;
				if (!done_i) i++;
			} else {
				if (i + 1 >= allArgs.Count)
					return Error("Missing argument list");

				// Detect (args)->ret
				var argsNode = allArgs[i + 1];
				if (argsNode.Calls(S._RightArrow, 2)) {
					retVal = argsNode.Args[1];
					argsNode = argsNode.Args[0];
				}
				
				// Expect (args), in parenthesis
				if (argsNode.Calls(S.Missing)) {
					args = argsNode.Args;
				} else
					return Error("Expected argument list, but got '{0}' node", argsNode.Name);
			}
		}
		*/
		private static LNode Error(string msg)
		{
			return F.Call(S.Error, F.Literal(msg));
		}
		private static LNode Error(string msg, object arg0)
		{
			return F.Call(S.Error, F.Literal(msg), F.Literal(arg0));
		}
		private static LNode Error(string msg, object arg0, object arg1)
		{
			return F.Call(S.Error, F.Literal(msg), F.Literal(arg0), F.Literal(arg1));
		}
		private static LNode Error(string msg, object arg0, object arg1, object arg2)
		{
			return F.Call(S.Error, F.Literal(msg), F.Literal(arg0), F.Literal(arg1), F.Literal(arg2));
		}
	}
}
