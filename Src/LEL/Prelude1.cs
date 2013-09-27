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
	/// <summary>Marks a class to be searched for macros.</summary>
	/// <remarks>The method signature of a macro must be <see cref="SimpleMacro"/> and
	/// it must be marked with <see cref="SimpleMacroAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ContainsMacrosAttribute : Attribute
	{
	}

	/// <summary>Marks a method as an LEL simple macro.</summary>
	/// <remarks>
	/// To be recognized as a macro, the method must be static and its signature 
	/// must be <see cref="SimpleMacro"/>. A class will not be searched for macros
	/// unless the class is marked with <see cref="ContainsMacrosAttribute"/>.</remarks>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple=false)]
	public class SimpleMacroAttribute : Attribute
	{
		public SimpleMacroAttribute(string syntax, string description, params string[] names) 
			{ Syntax = syntax; Description = description; Names = names; }
		public readonly string Syntax;
		public readonly string Description;
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
	public delegate LNode SimpleMacro(LNode node, IMessageSink rejectReason);

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

		static readonly Symbol _macros = GSymbol.Get("macros");
		static readonly Symbol _importMacros = GSymbol.Get("#importMacros");

		[SimpleMacro("import Namespace; import macros Namespace", 
			"Use symbols from specified namespace ('using' in C#). The 'macros' modifier imports macros only, deleting this statement from the output.")]
		public static LNode import(LNode node, IMessageSink sink)
		{
			if (node.Args.TryGet(0, F._Missing).IsIdNamed(_macros))
				return node.With(_importMacros, node.Args.RemoveAt(0));
			else
				return node.WithTarget(S.Import);
		}
		[SimpleMacro("class Name { Members; }; class Name(Bases...) { Members... }", 
			"Defines a class (a by-reference data type with data and/or methods).")]
		public static LNode @class(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Class);
		}
		[SimpleMacro("struct Name { Members; }; struct Name(Bases...) { Members... }", 
			"Defines a struct (a by-value data type with data and/or methods).")]
		public static LNode @struct(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Struct);
		}
		[SimpleMacro("enum Name { Tag1 = Num1; Tag2 = Num2; ... }; enum Name(BaseInteger) { Tag1 = Num1; Tag2 = Num2; ... }", 
			"Defines an enumeration (a integer that represents one of several identifiers, or a combination of bit flags when marked with [Flags]).")]
		public static LNode @enum(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Enum);
		}
		[SimpleMacro("trait Name { Members; }; trait Name(Bases...) { Members... }",
			"Not implemented. A set of methods that can be inserted easily into a host class or struct; just add the trait to the host's list of Bases.")]
		public static LNode @trait(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Trait);
		}
		[SimpleMacro("alias NewName = OldName; alias NewName(Bases...) = OldName; alias NewName(Bases) = OldName { FakeMembers... }",
			"Not implemented. Defines an alternate view on a data type. If 'Bases' specifies one or more interfaces, a variable of type NewName can be implicitly converted to those interfaces.")]
		public static LNode @alias(LNode node, IMessageSink sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Alias, true);
		}
		[SimpleMacro("namespace Name { Members... }",
			"Adds the specified members to a namespace. Namespaces are used to organize code; it is recommended that every data type and method be placed in a namespace. The 'Name' can have multiple levels (A.B.C).")]
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

		[SimpleMacro("def Name(Args...) { Body... }; def Name(Args...)::ReturnType { Body }; def Name ==> ForwardingTarget { Body }",
			"Defines a function (also known as a method). The '==> ForwardingTarget' version is not implemented.")]
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
		
		[SimpleMacro("prop Name::Type { get {Body...} set {Body...} }", 
			"Defines a property. The getter and setter are optional, but there must be at least one of them.")]
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

		[SimpleMacro("var Name::Type; var Name::Type = Value; var Name = Value",
			"Defines a variable or field in the current scope. You can define more than one at a time, e.g. 'var X::int Name::string;'")]
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

		[SimpleMacro("for Init Test Increment {Body...}",
			"Represents the standard C/C++/C#/Java 'for' statement, e.g. 'for i=0 i<10 i++ { Console.WriteLine(i); };'")]
		public static LNode @for(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.For);
		}
		
		[SimpleMacro("while Condition {Body...}",
			"Runs the Body code repeatedly, as long as 'Condition' is true. The Condition is checked before Body is run the first time.")]
		public static LNode @while(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.While);
		}

		[SimpleMacro("do {Body...} while Condition; do {Body...} while(Condition)",
			"Runs the Body code repeatedly, as long as 'Condition' is true. The Condition is checked after Body has already run.")]
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
		[SimpleMacro("if Condition {Then...}; if Condition {Then...} else {Else...}",
			"If 'Condition' is true, runs the 'Then' code; otherwise, runs the 'Else' code, if any.")]
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

		[SimpleMacro("unless Condition {Then...}; unless Condition {Then...} else {Else...}",
			"If 'Condition' is false, runs the 'Then' code; otherwise, runs the 'Else' code, if any.")]
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

		[SimpleMacro("switch Value { case ConstExpr1; Handler1; break; case ConstExpr2; Handler2; break; default; DefaultHandler; }",
			"Chooses one of several code paths based on the specified 'Value'.")]
		public static LNode @switch(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.Switch);
		}
		
		[SimpleMacro("lock Object {Body...}",
			"Acquires a multithreading lock associated with the specified object. 'lock' waits for any other thread holding the lock to release it before running the statements in 'Body'.")]
		public static LNode @lock(LNode node, IMessageSink sink)
		{
			return node.WithTarget(S.Lock);
		}

		static readonly Symbol _catch = GSymbol.Get("catch");
		static readonly Symbol _finally = GSymbol.Get("finally");
		
		[SimpleMacro("try {Code...} catch (E::Exception) {Handler...} finally {Cleanup...}",
			"Runs 'Code'. The try block must be followed by at least one catch or finally clause. A catch clause catches any exceptions that are thrown while the Code is running, and executes 'Handler'. A finally clause runs 'Cleanup' code before propagating the exception to higher-level code.")]
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

		#region Attributes & data types
		
		[SimpleMacro("pub", "Used as an attribute to indicate that a type, method or field is publicly accessible.", "pub", "public")]
		public static LNode pub(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Public); }
		[SimpleMacro("priv", "Used as an attribute to indicate that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.", "priv", "private")]
		public static LNode priv(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Private); }
		[SimpleMacro("prot", "Used as an attribute to indicate that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.", "prot", "protected")]
		public static LNode prot(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Protected); }
		[SimpleMacro("internal", "Used as an attribute to indicate that a type, method or field is accessible only inside the same assembly. When combined with prot, it is also accessible to derived classes in different assemblies.")]
		public static LNode @internal(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Internal); }
		
		[SimpleMacro("ref", "Used as an attribute on a method parameter to indicate that it is passed by reference. This means the caller must pass a variable (not a value), and that the caller can see changes to the variable.")]
		public static LNode @ref(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Ref); }
		[SimpleMacro("out", "Used as an attribute on a method parameter to indicate that it is passed by reference. In addition, the called method must assign a value to the variable, and it cannot receive input through the variable.")]
		public static LNode @out(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Out); }

		[SimpleMacro("sbyte", "A signed 8-bit data type")]
		public static LNode @sbyte(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Int8); }
		[SimpleMacro("byte", "An unsigned 8-bit data type")]
		public static LNode @byte(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.UInt8); }
		[SimpleMacro("short", "A signed 16-bit data type")]
		public static LNode @short(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Int16); }
		[SimpleMacro("ushort", "An unsigned 16-bit data type")]
		public static LNode @ushort(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.UInt16); }
		[SimpleMacro("int", "A signed 32-bit data type")]
		public static LNode @int(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Int32); }
		[SimpleMacro("uint", "An unsigned 32-bit data type")]
		public static LNode @uint(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.UInt32); }
		[SimpleMacro("long", "A signed 64-bit data type")]
		public static LNode @long(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Int64); }
		[SimpleMacro("ulong", "An unsigned 64-bit data type")]
		public static LNode @ulong(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.UInt64); }
		[SimpleMacro("char", "A 16-bit single-character data type")]
		public static LNode @char(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Char); }
		[SimpleMacro("float", "A 32-bit floating-point data type")]
		public static LNode @float(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Single); }
		[SimpleMacro("double", "A 64-bit floating-point data type")]
		public static LNode @double(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Double); }
		[SimpleMacro("bool", "The boolean data type (holds one of two values, @true or @false)")]
		public static LNode @bool(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Bool); }
		[SimpleMacro("string", "The string data type: a read-only sequence of characters.")]
		public static LNode @string(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.String); }
		[SimpleMacro("decimal", "A 128-bit floating-point BCD data type")]
		public static LNode @decimal(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Decimal); }
		[SimpleMacro("void", "An empty data type that always has the same value, known as '@void'")]
		public static LNode @void(LNode node, IMessageSink sink) { return TranslateId(node, sink, S.Void); }

		private static LNode TranslateId(LNode node, IMessageSink sink, Symbol symbol)
		{
			if (!node.IsId) return null;
			return node.WithName(symbol);
		}

		#endregion
	}
}
