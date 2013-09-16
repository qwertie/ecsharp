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
using System.Diagnostics;

namespace LEL.Prelude
{
	/// <summary>Marks a method as an LEL simple macro.</summary>
	/// <remarks>The method signature of a macro must be <see cref="SimpleMacro"/>.</remarks>
	[AttributeUsage(AttributeTargets.Method)]
	public class SimpleMacroAttribute : Attribute
	{
		public SimpleMacroAttribute(params string[] names) { Names = names; }
		public readonly string[] Names;
	}

	/// <summary>Method signature of an LEL simple macro.</summary>
	/// <param name="node">The node that caused the macro to be invoked (includes 
	/// the name of the macro itself, and any attributes applied to the macro)</param>
	/// <param name="rejectReason">If the input does not have a valid form, the
	/// macro rejects it by returning null. When returning null, the macro should
	/// explain the reason for the rejection (including a pattern that the macro 
	/// accepts) via this object.</param>
	/// <returns>A node to replace the original <c>node</c>, or null if this 
	/// macro rejects the input node. Returning null can allow a different macro 
	/// to accept the node instead.</returns>
	/// <remarks>If there are multiple macros in scope with the same name, they 
	/// are <i>all</i> called. Macro expansion succeeds if exactly one macro accepts 
	/// the input. If no macros accept the input, the error message given by each
	/// macro is printed; if multiple macros accept the input, an ambiguity error
	/// is printed.</remarks>
	public delegate LNode SimpleMacro(LNode node, out IMessageSink rejectReason);

	public class StockOverloadAttribute : Attribute { }
	public class LowPriorityOverloadAttribute : Attribute { }

	/// <summary>Defines the core, predefined constructs of LEL.</summary>
	public static partial class Macros
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		static readonly Symbol Error = MessageSink.Error;
		static readonly Symbol Warning = MessageSink.Warning;
		static LNode Reject(IMessageSink error, LNode at, string msg)
		{
			error.Write(Error, at, msg);
			return null;
		}

		#region Definition statements (classes, methods, etc.)

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
				return Reject(sink, node, "A type definition must have the form kind(Name, { Body }) or kind(Name(Bases), { Body }) (where «kind» is struct/class/enum/trait/alias)");
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
			if (!parts.Count.IsInRange(1, 2) || !sig.IsCall || (body != null && !body.Calls(S.Braces)))
				return Reject(sink, node, "A method definition must have the form def(Name(Args), { Body }), def(Name(Args) -> ReturnType, { Body }) or def(Name ==> ForwardingTarget, { Body })");
			
			LNode forwardTo = null, retVal = null;
			if (sig.Calls(S.Forward, 2)) {
				forwardTo = sig.Args[1];
				sig = sig.Args[0];
				if (body != null)
					return Reject(sink, sig.Target, "Cannot use ==> and a method body {...} at the same time.");
			}
			if (sig.Calls(S._RightArrow, 2) || sig.Calls(S.ColonColon, 2)) {
				retVal = sig.Args[1];
				sig = sig.Args[0];
			}
			if (retVal.Calls(S.Braces) && body == null) {
				body = retVal;
				retVal = F._Missing;
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

		// Syntax I'm using:
		//   prop x::int { get { _x; }; set { _x = value; } }
		//   (#property (int, x, { get { ... }; set { ... } }))
		// Syntax I'd rather use:
		//   get x::int { _x; }
		//   set x::int { _x = value; }
		
		[SimpleMacro]
		public static LNode @prop(LNode node, IMessageSink sink)
		{
			var parts = node.Args;
			LNode sig = parts.TryGet(0, null), body = parts.TryGet(1, null), name, retVal = null;
			if (parts.Count != 2 || !body.Calls(S.Braces))
				return Reject(sink, node, "A property definition must have the form prop(Name, { Body }), or prop(Name::type, { Body })");

			if (sig.Calls(S._RightArrow, 2) || sig.Calls(S.ColonColon, 2)) {
				name = sig.Args[0];
				retVal = sig.Args[1];
			} else {
				name = sig;
				retVal = F._Missing;
			}
			if (!IsComplexId(name))
				return Reject(sink, name, "Property name must be a complex identifier");

			return node.With(S.Property, retVal, name, body);
		}

		[SimpleMacro]
		public static LNode @var(LNode node, IMessageSink sink)
		{
			var parts = node.Args;
			LNode sig = parts.TryGet(0, null), body = parts.TryGet(1, null);
			if (parts.Count == 0)
				return Reject(sink, node, "A variable definition must have the form var(Name::Type), var(Name = value), or var(Name::Type = value)");

			RWList<LNode> varStmts = null;
			LNode varStmt = null;
			for (int i = 0; i < parts.Count; i++) {
				LNode part = parts[i], type = null, init = null;
				if (part.Calls(S.Set, 2)) {
					init = part.Args[1];
					part = part.Args[0];
				}
				if (part.Calls(S.ColonColon, 2)) {
					type = part.Args[1];
					part = part.Args[0];
				}
				if (init == null && part.Calls(S.Set, 2)) {
					init = part.Args[1];
					part = part.Args[0];
				}
				if (!part.IsId)
					return Reject(sink, part, "Expected a simple variable name here");
				if (!IsComplexId(type))
					return Reject(sink, type, "Expected a type name here");
				type = type ?? F._Missing;

				var nameAndInit = init == null ? part : F.Call(S.Set, part, init);
				if (varStmt != null && varStmt.Args[0].Equals(type))
					varStmt = varStmt.WithArgs(varStmt.Args.Add(nameAndInit));
				else {
					if (varStmt != null)
						varStmts = varStmts ?? new RWList<LNode> { varStmt };
					varStmt = F.Call(S.Var, type, nameAndInit);
				}
			}
			
			// Return a single statement or a list of them if necessary
			if (varStmts != null) {
				varStmts.Add(varStmt);
				return F.List(varStmts.ToRVList());
			} else {
				return varStmt;
			}
		}

		#endregion

		#region Executable statements

		[SimpleMacro]
		public static LNode @for(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.For);
		}
		
		[SimpleMacro]
		public static LNode @while(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.While);
		}

		[SimpleMacro]
		public static LNode @do(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			if (node.ArgCount == 2 && args.Last.Calls(S.While, 1)) {
				return node.With(S.DoWhile, new RVList<LNode>(node.Args[0], node.Args[1].Args[0]));
			} else if (node.ArgCount == 3 && args.TryGet(1, null).IsIdNamed(_while)) {
				return node.With(S.DoWhile, new RVList<LNode>(node.Args[0], node.Args[2]));
			}
			return Reject(sink, node, "A do-while statement must have the form «do(expr, while(expr))» or «do(expr, while, expr)»");
		}
		static readonly Symbol _while = GSymbol.Get("while");

		static readonly Symbol _else = GSymbol.Get("else");
		[SimpleMacro]
		public static LNode @if(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			LNode cond = args.TryGet(0, null), then = args.TryGet(1, null), @else = args.TryGet(3, null);
			if (node.ArgCount != 2 && (node.ArgCount != 4 || !args.TryGet(2, null).IsIdNamed(_else)))
				return Reject(sink, node, "An if-statement must have the form «if(Cond, expr)» or «if(Cond, ThenClause, else, ElseClause)»");
			if (@else == null)
				return node.With(S.If, cond, then);
			else
				return node.With(S.If, cond, then, @else);
		}

		[SimpleMacro]
		public static LNode @unless(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			LNode cond = args.TryGet(0, null), then = args.TryGet(1, null), @else = args.TryGet(3, null);
			if (node.ArgCount != 2 && (node.ArgCount != 4 || !args.TryGet(2, null).IsIdNamed(_else)))
				return Reject(sink, node, "An unless-statement must have the form «unless(Cond, expr)» or «unless(Cond, ThenClause, else, ElseClause)»");
			if (@else == null)
				return node.With(S.If, F.Call(S.Not, cond), then);
			else
				return node.With(S.If, F.Call(S.Not, cond), then, @else);
		}

		[SimpleMacro]
		public static LNode @switch(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.Switch);
		}
		
		[SimpleMacro]
		public static LNode @lock(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.Lock);
		}

		static readonly Symbol _catch = GSymbol.Get("catch");
		static readonly Symbol _finally = GSymbol.Get("finally");
		[SimpleMacro]
		public static LNode @try(LNode node, IMessageSink sink)
		{
			// try(code, catch, Exception::e, handler, catch, ..., finally, handler)
			// ...becomes...
			// #try(#{ stmt1; stmt2; ... }, #catch(#var(Exception, e), handler), #finally(handler))
			LNode finallyCode = null;
			RWList<LNode> clauses = new RWList<LNode>();
			var parts = node.Args;
			
			for (int i = parts.Count-2; i >= 1; i -= 2)
			{
				var p = parts[i];
				if (p.IsCall && (p.Name == _finally || p.Name == _catch))
					return Reject(sink, p, "The «catch» and «finally» clauses do not take arguments (for catch, use «catch (...)», not «catch(...)»).");
				if (p.IsIdNamed(_finally)) {
					if (clauses.Count != 0 || finallyCode != null)
						return Reject(sink, p, "The «finally» clause must come last and must not be a call.");
					finallyCode = parts[i+1];
				} else if (p.IsIdNamed(_catch)) {
					// This is a catch-all clause (the type argument is missing)
					if (clauses.Count != 0)
						return Reject(sink, p, "The catch-all clause must be the last «catch» clause.");
					clauses.Add(F.Call(S.Catch, F._Missing, parts[i+1]));
				} else if (i > 1 && parts[i-1].IsIdNamed(_catch)) {
					// This is a normal catch clause
					if (p.Calls(S.ColonColon, 2)) // e::Exception => #var(Exception, e)
						p = F.Call(S.Var, p.Args[1], p.Args[0]);
					clauses.Add(F.Call(S.Catch, p, parts[i+1]));
				} else {
					return Reject(sink, p, "Expected «catch» or «finally» clause here");
				}
				if (i == 2)
					return Reject(sink, parts[1], "Expected «catch» or «finally» clause here");
			}
			if (clauses.Count == 0 && finallyCode == null) {
				Debug.Assert(node.ArgCount <= 1);
				return Reject(sink, node, "Missing «catch, Type, Code» or «finally, Code» clause");
			}
			if (finallyCode != null)
				clauses.Add(F.Call(S.Finally, finallyCode));
			clauses.Insert(0, node.Args[0]);
			return node.With(S.Try, clauses.ToRVList());
		}

		#endregion
	}
}
