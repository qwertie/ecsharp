using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Loyc;
using Loyc.Syntax;
using Loyc.Collections;
using Loyc.Math;
using Loyc.Utilities;
using S = Loyc.Syntax.CodeSymbols;

/// <summary>Defines prelude macros for LES, which are predefined macros that 
/// normally do not have to be explicitly imported before use. Their job is to
/// convert LES to C# constructs.</summary>
namespace LeMP.Prelude.Les
{
	/// <summary>Defines the core, predefined constructs of LeMP for LES for .NET.</summary>
	[ContainsMacros]
	public static partial class Macros
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Synthetic);

		internal static LNode Reject(IMessageSink error, LNode at, string msg)
		{
			error.Write(Severity.Note, at, msg);
			return null;
		}
		internal static LNode Reject(IMessageSink error, LNode at, string msg, params object[] args)
		{
			error.Write(Severity.Note, at, msg, args);
			return null;
		}
		/// Some functions here are called by LESv3 macros. In LESv3, a syntax like 
		/// .class is used which becomes #class in the LNode. Do not print an error 
		/// in that case, since the tree might be valid, unbeknownst to the macro.
		static LNode RejectIfNormal(IMessageSink error, LNode at, string msg)
		{
			return at.HasSpecialName ? null : Reject(error, at, msg);
		}

		#region Definition statements (classes, methods, etc.)

		static readonly Symbol _macros = GSymbol.Get("macros");

		[LexicalMacro("import Namespace;", "Use symbols from specified namespace ('using' in C#).")]
		public static LNode import(LNode node, IMessageSink sink)
		{
			if (!node.Args.TryGet(0, F.Missing).IsIdNamed(_macros))
				return node.WithTarget(S.Import);
			return null;
		}
		[LexicalMacro("class Name { Members; }; class Name(Bases...) { Members... }", 
			"Defines a class (a by-reference data type with data and/or methods).")]
		public static LNode @class(LNode node, IMacroContext sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Class);
		}
		[LexicalMacro("struct Name { Members; }; struct Name(Bases...) { Members... }", 
			"Defines a struct (a by-value data type with data and/or methods).")]
		public static LNode @struct(LNode node, IMacroContext sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Struct);
		}
		[LexicalMacro("enum Name { Tag1 = Num1; Tag2 = Num2; ... }; enum Name(BaseInteger) { Tag1 = Num1; Tag2 = Num2; ... }", 
			"Defines an enumeration (a integer that represents one of several identifiers, or a combination of bit flags when marked with [Flags]).")]
		public static LNode @enum(LNode node, IMacroContext sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Enum);
		}
		[LexicalMacro("trait Name { Members; }; trait Name(Bases...) { Members... }",
			"Not implemented. A set of methods that can be inserted easily into a host class or struct; just add the trait to the host's list of Bases.")]
		public static LNode @trait(LNode node, IMacroContext sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Trait);
		}
		[LexicalMacro("alias NewName = OldName; alias NewName(Bases...) = OldName; alias NewName(Bases) = OldName { FakeMembers... }",
			"Not implemented. Defines an alternate view on a data type. If 'Bases' specifies one or more interfaces, a variable of type NewName can be implicitly converted to those interfaces.")]
		public static LNode @alias(LNode node, IMacroContext sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Alias);
		}
		[LexicalMacro("using NewName = OldName", "Defines an alias that applies inside the current module only.", "using")]
		public static LNode @usingDecl(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 1) {
				if (IsComplexId(node.Args[0])) {
					// Looks like an import statement
					context.Sink.Warning(node.Target, "The 'import' statement replaces the 'using' statement in LeMP for LES.");
					return node.WithTarget(S.Import);
				}
				var result = TranslateSpaceDefinition(node, context, S.Alias);
				if (result != null)
					return result.PlusAttr(F.Id(S.FilePrivate));
			}
			return null;
		}
		[LexicalMacro("namespace Name { Members... }",
			"Adds the specified members to a namespace. Namespaces are used to organize code; it is recommended that every data type and method be placed in a namespace. The 'Name' can have multiple levels (A.B.C).")]
		public static LNode @namespace(LNode node, IMacroContext sink)
		{
			return TranslateSpaceDefinition(node, sink, S.Namespace);
		}

		public static LNode TranslateSpaceDefinition(LNode node, IMacroContext context, Symbol newTarget)
		{
			if (!node.IsCall)
				return null;

			bool isAlias = newTarget == S.Alias, isNamespace = newTarget == S.Namespace;
			var args = node.Args;
			LNode nameEtc = args.TryGet(0, null), body = args.TryGet(1, null), oldName = null;

			if (args.Count == 1 ? !isAlias : (args.Count != 2 || !body.Calls(S.Braces))) {
				if (isNamespace && args.Count == 1) {
					// Special case: a namespace can auto-wrap whatever statements follow.
					body = F.Braces(context.RemainingNodes);
					context.DropRemainingNodes = true;
				} else
					return RejectIfNormal(context, node, "A type definition must have the form kind(Name, { Body }) or kind(Name(Bases), { Body }) (where «kind» is struct/class/enum/trait/alias)");
			}
			if (isAlias) {
				if (!nameEtc.Calls(S.Assign, 2))
					return RejectIfNormal(context, node, "An 'alias' (or 'using') definition must have the form alias(NewName = OldName, { Body }) or alias(NewName(Interfaces) = OldName, { Body })");
				oldName = nameEtc.Args[1];
				nameEtc = nameEtc.Args[0];
			}

			LNode name, bases;
			if (IsComplexId(nameEtc, true)) {
				name = nameEtc;
				bases = F.List();
			} else {
				name = nameEtc.Target ?? nameEtc;
				bases = nameEtc.WithTarget(S.AltList);
			}

			if (isNamespace) {
				if (!IsComplexId(name, true))
					return Reject(context, name, "Invalid namespace name (expected a complex identifier)");
			} else {
				if (!IsDefinitionId(name, false))
					return Reject(context, name, "Invalid type name (expected a simple name or Name!(T1,T2,...))");
			}

			if (isAlias) {
				if (body == null)
					return node.With(newTarget, F.Call(S.Assign, name, oldName), bases);
				else
					return node.With(newTarget, F.Call(S.Assign, name, oldName), bases, body);
			} else {
				Debug.Assert(body != null);
				return node.With(newTarget, name, bases, body);
			}
		}

		// A definition identifier has the form Name or Name!(Id,$Id,...)
		// where Id is a simple identifier and Name is either a simple identifier 
		// or (if dots are allowed) a complex identifier with allowOf=false.
		public static bool IsDefinitionId(LNode id, bool allowDots)
		{
			var args = id.Args;
			if (id.CallsMin(S.Of, 1)) {
				if (!(allowDots ? IsComplexId(args[0], false) : args[0].IsId))
					return false;
				for (int i = 1; i < args.Count; i++)
					if (!(args[i].IsId || args[i].Calls(S.Substitute, 1) && args[i].Args[0].IsId))
						return false;
				return true;
			} else
				return allowDots ? IsComplexId(id, false) : id.IsId;
		}
		public static bool IsTargetDefinitionId(LNode id, bool allowDots)
		{
			return id.HasSimpleHead() || IsDefinitionId(id.Target, allowDots);
		}
		public static bool IsComplexId(LNode id, bool allowOf = true)
		{
			return Loyc.Ecs.EcsValidators.IsComplexIdentifier(id);
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

		[LexicalMacro("NameOfFunction(Args...) -> Type => Body; NameOfFunction(Args...) ==> ForwardingTarget",
			"Defines a function (also known as a method).",
			"'=>", "'==>", Mode = MacroMode.Passive)]
		public static LNode quickFunction(LNode node, IMessageSink sink)
		{
			// Recognizes Name(Args...) => Body
			// and        Name(Args...) ==> Body
			if (node.ArgCount == 2)
			{
				bool isForwarding = node.Calls("'==>");
				LNode sig = node[0], body = node[1];
				if (!sig.HasSpecialName)
					return NewFunctionNode(node, sig, null, body, sink, isForwarding);
			}
			return null;
		}

		[LexicalMacro("NameOfFunction(Args...): Type => Body; NameOfFunction(Args...): Type ==> ForwardingTarget",
			"Defines a function (also known as a method).", "':")]
		public static LNode quickFunctionWithColon(LNode node, IMessageSink sink)
		{
			// Recognizes Name(Args...) : (Type =>  Body)
			// and        Name(Args...) : (Type ==> Body)
			if (node.ArgCount == 2)
			{
				LNode sig = node[0], rhs = node[1];
				bool isForwarding = rhs.Calls(S.Forward, 2);
				if (isForwarding || rhs.Calls(S.Lambda, 2))
				{
					LNode type = rhs[0];
					LNode body = rhs[1];
					if (!sig.HasSpecialName)
						return NewFunctionNode(node, sig, type, body, sink, isForwarding);
				}
			}
			return null;
		}

		[LexicalMacro("fn Name(Args...) { Body... }; fn Name(Args...)::ReturnType { Body }; fn Name(Args...): ReturnType { Body }; fn Name ==> ForwardingTarget",
			"Defines a function (also known as a method).",
			"fn", "def")]
		public static LNode @fn(LNode node, IMessageSink sink)
		{
			return FnOrCons(node, sink, false);
		}
		[LexicalMacro("cons ClassName(Args...) {Body...}", "Defines a constructor for the enclosing type. To call the base class constructor, call base(...) as the first statement of the Body.")]
		public static LNode cons(LNode node, IMessageSink sink)
		{
			return FnOrCons(node, sink, true);
		}
		public static LNode FnOrCons(LNode node, IMessageSink sink, bool isCons)
		{
			var parts = node.Args;
			LNode rest = parts.TryGet(0, null), body = parts.TryGet(1, null);
			if (!parts.Count.IsInRange(1, 2) || !rest.IsCall || (body != null && !body.Calls(S.Braces)))
				return null;

			// Recognize Name(Args...) : (Type =>  Body)
			// and       Name(Args...) : (Type ==> Body)
			// and       Name(Args...) : Type
			LNode type = null, sig = null;
			if (rest.Calls(S.Colon, 2))
			{
				sig = rest[0];
				rest = rest[1];
			}
			// Recognize TypeOrSig => Body
			// and       TypeOrSig ==> Body
			bool isForwarding = rest.Calls(S.Forward, 2);
			if (isForwarding || rest.Calls(S.Lambda, 2))
			{
				if (body != null)
					return Reject(sink, rest.Target, "Cannot use '{0}' and a body {...} at the same time.", type.Name.Name.Substring(1));
				body = rest[1];
				rest = rest[0];
			}
			if (sig == null)
				sig = rest;
			else
				type = rest;
			// Recognize Sig::Type
			if (sig.Calls(S.ColonColon, 2) && type == null)
			{
				type = sig[1];
				sig = sig[0];
			}
			return NewFunctionNode(node, sig, type, body, sink, isForwarding, isCons);
		}
		public static LNode NewFunctionNode(LNode range, LNode sig, LNode retVal, LNode body, IMessageSink sink, bool isForwarding, bool isCons = false)
		{
			if (!sig.IsCall)
				return null;

			var name = sig.Target;
			if (!IsTargetDefinitionId(sig, true))
				return Reject(sink, sig.Target, "Invalid method name");
			var argList = sig.WithTarget(S.AltList);

			if (retVal == null)
				retVal = isCons ? F.Missing : F.Void;
			else if (isCons)
				return Reject(sink, retVal, "A constructor cannot have a return type");

			Symbol kind = isCons ? S.Constructor : S.Fn;
			if (body != null) {
				if (isForwarding)
					return range.With(kind, retVal, name, argList, F.Call(S.Forward, body));
				else
					return range.With(kind, retVal, name, argList, body);
			} else
				return range.With(kind, retVal, name, argList);
		}

		// Syntax I'm using:
		//   prop x::int { get { _x; }; set { _x = value; } }
		//   (#property (int, x, { get { ... }; set { ... } }))
		// Syntax I'd rather use:
		//   get x::int { _x; }
		//   set x::int { _x = value; }
		
		[LexicalMacro("prop Name::Type { get {Body...} set {Body...} }", 
			"Defines a property. The getter and setter are optional, but there must be at least one of them.")]
		public static LNode @prop(LNode node, IMessageSink sink)
		{
			var parts = node.Args;
			LNode sig = parts.TryGet(0, null), body = parts.TryGet(1, null), name, retVal = null;
			if (parts.Count != 2 || !body.Calls(S.Braces))
				return RejectIfNormal(sink, node, "A property definition must have the form prop(Name, { Body }), or prop(Name::type, { Body })");

			if (sig.Calls(S.RightArrow, 2) || sig.Calls(S.ColonColon, 2) || sig.Calls(S.Colon, 2)) {
				name = sig.Args[0];
				retVal = sig.Args[1];
			} else {
				name = sig;
				retVal = F.Missing;
			}
			if (!IsComplexId(name))
				return RejectIfNormal(sink, name, "Property name must be a complex identifier");

			return node.With(S.Property, retVal, name, F.Missing, body);
		}

		[LexicalMacro("var Name::Type; var Name::Type = Value; var Name = Value",
			"Defines a variable or field in the current scope. You can define more than one at a time, e.g. 'var X::int Name::string;'")]
		public static LNode @var(LNode node, IMessageSink sink)
		{
			var parts = node.Args;
			if (parts.Count == 0)
				return Reject(sink, node, "A variable definition must have the form var(Name::Type), var(Name = value), or var(Name::Type = value)");
			if (parts[0].IsId)
				return null; // e.g. this is true for "static readonly x::Foo"

			WList<LNode> varStmts = null;
			LNode varStmt = null;
			for (int i = 0; i < parts.Count; i++) {
				LNode part = parts[i], type = null, init = null;
				if (part.Calls(S.Assign, 2)) {
					init = part.Args[1];
					part = part.Args[0];
				}
				if (part.Calls(S.ColonColon, 2)) {
					type = part.Args[1];
					part = part.Args[0];
				}
				if (init == null && part.Calls(S.Assign, 2)) {
					init = part.Args[1];
					part = part.Args[0];
				}
				if (!part.IsId)
					return Reject(sink, part, "Expected a simple variable name here");
				if (type != null && !IsComplexId(type))
					return Reject(sink, type, "Expected a type name here");
				type = type ?? F.Missing;

				var nameAndInit = init == null ? part : F.Call(S.Assign, part, init);
				if (varStmt != null && varStmt.Args[0].Equals(type)) {
					// same type used again, e.g. var(x::int, y::int) => #var(int, x, y)
					varStmt = varStmt.WithArgs(varStmt.Args.Add(nameAndInit));
				} else {
					// first item var(x::int) => #var(int, x) or type changed (var(a::A, b::B) => #var(A, a); #var(B, b))
					if (varStmt != null) {
						varStmts = varStmts ?? new WList<LNode>();
						varStmts.Add(varStmt);
					}
					varStmt = node.With(S.Var, type, nameAndInit).SetBaseStyle(NodeStyle.Default);
				}
			}
			
			// Return a single statement or a list of them if necessary
			if (varStmts != null) {
				varStmts.Add(varStmt);
				return F.Call(S.Splice, varStmts.ToVList());
			} else {
				return varStmt;
			}
		}

		#endregion

		#region Executable statements

		private static LNode TranslateCall(LNode node, Symbol symbol)
		{
			if (!node.IsCall) return null;
			return node.WithTarget(symbol);
		}

		[LexicalMacro("for Init Test Increment {Body...}; for (Init, Test, Increment) {Body...};",
			"Represents the standard C/C++/C#/Java 'for' statement, e.g. 'for i=0 i<10 i++ { Console.WriteLine(i); };'")]
		public static LNode @for(LNode node, IMessageSink sink)
		{
			LNode tuple;
			if (node.ArgCount == 2 && ((tuple = node.Args[0]).Calls(S.Tuple, 3) || tuple.Calls(S.Tuple, 2)))
				return node.With(S.For, 
					asAltList(tuple.Args[0]), 
					tuple.Args[1], 
					asAltList(tuple.Args[2, LNode.Missing]),
					node.Args[1]);
			else if (node.ArgCount == 4)
				return node.With(S.For,
					asAltList(node.Args[0]), 
					node.Args[1], 
					asAltList(node.Args[2]),
					node.Args[3]);
			return null;
		}

		static LNode asAltList(LNode node) {
			return node.Calls(S.AltList) ? node
			     : node.IsIdNamed(GSymbol.Empty) ? LNode.Call(S.AltList, node)
			     : LNode.Call(S.AltList, LNode.List(node), node).SetBaseStyle(NodeStyle.Default);
		}

		static readonly Symbol _in = GSymbol.Get("in"), __in = GSymbol.Get("'in");

		[LexicalMacro(@"foreach Item \in Collection {Body...}; foreach Item::Type \in Collection {Body...}", "Represents the C# 'foreach' statement.")]
		public static LNode @foreach(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			LNode inExpr;
			if (args.Count == 2 && ((inExpr = args[0]).Calls(_in, 2) || inExpr.Calls(__in, 2))) {
				LNode decl = inExpr.Args[0], list = inExpr.Args[1], body = args[1];
				if (decl.IsId)
					decl = F.Var(F.Missing, decl);
				return node.With(S.ForEach, decl, list, body);
			}
			return null;
		}
		
		[LexicalMacro("while Condition {Body...}",
			"Runs the Body code repeatedly, as long as 'Condition' is true. The Condition is checked before Body is run the first time.")]
		public static LNode @while(LNode node, IMessageSink sink)
		{
			return TranslateCall(node, S.While);
		}

		[LexicalMacro("do {Body...} while Condition; do {Body...} while(Condition)",
			"Runs the Body code repeatedly, as long as 'Condition' is true. The Condition is checked after Body has already run.")]
		public static LNode @do(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			if (node.ArgCount == 2 && args.Last.Calls(_while, 1)) {
				return node.With(S.DoWhile, new LNodeList(node.Args[0], node.Args[1].Args[0]));
			} else if (node.ArgCount == 3 && args.TryGet(1, null).IsIdNamed(_while)) {
				return node.With(S.DoWhile, new LNodeList(node.Args[0], node.Args[2]));
			}
			return null;
		}

		static readonly Symbol _while = GSymbol.Get("while");
		static readonly Symbol _else = GSymbol.Get("else");
		static readonly Symbol _unless = GSymbol.Get("unless");
		static readonly Symbol _if = GSymbol.Get("if");

		[LexicalMacro("if Condition {Then...}; if Condition {Then...} else {Else...}",
			"If 'Condition' is true, runs the 'Then' code; otherwise, runs the 'Else' code, if any.")]
		public static LNode @if(LNode node, IMessageSink sink)
		{
			return IfUnless(node, node.Calls(_unless), sink);
		}
		public static LNode IfUnless(LNode node, bool isUnless, IMessageSink sink)
		{
			var args = node.Args;
			LNode cond = args.TryGet(0, null), then = args.TryGet(1, null),
				elseKW = args.TryGet(2, null), @else = args.TryGet(3, null);
			if (cond == null)
				return null;
			if (then == null)
				return Reject(sink, cond, "'{0}' statement ended early", isUnless ? "unless" : "if");
			if (isUnless)
				cond = F.Call(S.Not, cond);
			if (elseKW == null)
				return node.With(S.If, cond, then);
			if (!elseKW.IsIdNamed(_else))
				return Reject(sink, elseKW, "'{0}': expected else clause or end-of-statement marker", isUnless ? "unless" : "if");
			if (@else.IsId && args.Count > 4)
				@else = LNode.Call(@else.Name, new LNodeList(args.Slice(4)), node);
			return node.With(S.If, cond, then, @else);
		}

		[LexicalMacro("switch Value { case ConstExpr1; Handler1; break; case ConstExpr2; Handler2; break; default; DefaultHandler; }",
			"Chooses one of several code paths based on the specified 'Value'.")]
		public static LNode @switch(LNode node, IMessageSink sink)
		{
			return TranslateCall(node, S.SwitchStmt);
		}

		[LexicalMacro("break", "Exit the loop or switch body (the innermost loop, if more than one enclosing loop)", Mode = MacroMode.MatchIdentifier)]
		public static LNode @break(LNode node, IMessageSink sink)
		{
			if (!node.IsId) return null;
			return node.WithTarget(S.Break);
		}

		[LexicalMacro("continue", "Jump to the end of the loop body, running the loop again if the loop condition is true.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @continue(LNode node, IMessageSink sink)
		{
			if (!node.IsId) return null;
			return node.WithTarget(S.Continue);
		}

		[LexicalMacro("case ConstExpr; case ConstExpr { Code... }", "One label in a switch statement.")]
		public static LNode @case(LNode node, IMessageSink sink)
		{
			if (node.ArgCount >= 2 && node.Args.Last.Calls(S.Braces))
				return F.Call(S.Splice, new LNodeList(node.WithArgs(node.Args.WithoutLast(1)), node.Args.Last));
			if (node.ArgCount >= 1)
				return node.WithTarget(S.Case);
			return null;
		}
		
		[LexicalMacro("default; default { Code... }", "The default label in a switch statement.", "default", Mode = MacroMode.MatchIdentifier)]
		public static LNode @defaultCase(LNode node, IMessageSink sink)
		{
			if (node.IsId)
				return node.With(S.Label, F.Id(S.Default)).SetBaseStyle(NodeStyle.Default);
			else if (node.ArgCount == 1 && node.Args[0].Calls(S.Braces))
				return F.Call(S.Splice, new LNodeList(node.With(S.Label, new LNodeList(F.Id(S.Default))).SetBaseStyle(NodeStyle.Default), node.Args[0]));
			return null;
		}

		[LexicalMacro("goto LabelName", "Run code starting at the specified label in the same method.")]
		public static LNode @goto(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 1)
				return node.WithTarget(S.Goto);
			return null;
		}
		
		static readonly Symbol _case = GSymbol.Get("case");
		[LexicalMacro("goto case ConstExpr", "Jump to the specified case in the body of the same switch statement.", "goto")]
		public static LNode GotoCase(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2 && node.Args[0].IsIdNamed(_case))
				return node.With(S.GotoCase, node.Args[1]);
			return null;
		}

		[LexicalMacro("label LabelName", "Define a label here that 'goto' can jump to.")]
		public static LNode label(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 1)
				return node.WithTarget(S.Label);
			return null;
		}
		
		[LexicalMacro("lock Object {Body...}",
			"Acquires a multithreading lock associated with the specified object. 'lock' waits for any other thread holding the lock to release it before running the statements in 'Body'.")]
		public static LNode @lock(LNode node, IMessageSink sink)
		{
			return TranslateCall(node, S.Lock);
		}

		static readonly Symbol _catch = GSymbol.Get("catch");
		static readonly Symbol _finally = GSymbol.Get("finally");
		
		[LexicalMacro("try {Code...} catch (E::Exception) {Handler...} finally {Cleanup...}",
			"Runs 'Code'. The try block must be followed by at least one catch or finally clause. A catch clause catches any exceptions that are thrown while the Code is running, and executes 'Handler'. A finally clause runs 'Cleanup' code before propagating the exception to higher-level code.")]
		public static LNode @try(LNode node, IMessageSink sink)
		{
			if (!node.IsCall)
				return null;

			// try(code, catch, Exception::e, handler, catch, ..., finally, handler)
			// ...becomes...
			// #try(#{ stmt1; stmt2; ... }, #catch(#var(Exception, e), handler), #finally(handler))
			LNode finallyCode = null;
			WList<LNode> clauses = new WList<LNode>();
			var parts = node.Args;
			
			for (int i = parts.Count-2; i >= 1; i -= 2)
			{
				var p = parts[i];
				if (p.IsIdNamed(_finally)) {
					if (clauses.Count != 0 || finallyCode != null)
						sink.Error(p, "The «finally» clause must come last, there can only be one of them.");
					finallyCode = parts[i+1];
				} else if (p.Name == _catch) {
					if (p.ArgCount > 0) {
						if (p.ArgCount > 1)
							sink.Error(p, "Expected catch() to take one argument.");
						// This is a normal catch clause
						clauses.Insert(0, F.Call(S.Catch, p.Args[0], F.Missing, parts[i + 1]));
					} else {
						// This is a catch-all clause (the type argument is missing)
						if (clauses.Count != 0)
							sink.Error(p, "The catch-all clause must be the last «catch» clause.");
						clauses.Add(F.Call(S.Catch, F.Missing, F.Missing, parts[i + 1]));
					}
				} else if (i > 1 && parts[i-1].IsIdNamed(_catch)) {
					// This is a normal catch clause
					clauses.Insert(0, F.Call(S.Catch, AutoRemoveParens(p), F.Missing, parts[i+1]));
					i--;
				} else {
					return Reject(sink, p, "Expected «catch» or «finally» clause here. Clause is missing or malformed.");
				}
				if (i == 2)
					return Reject(sink, parts[1], "Expected «catch» or «finally» clause here. Clause is missing or malformed.");
			}
			if (clauses.Count == 0 && finallyCode == null) {
				Debug.Assert(node.ArgCount <= 1);
				return Reject(sink, node, "Missing «catch, Type, Code» or «finally, Code» clause");
			}
			if (finallyCode != null)
				clauses.Add(F.Call(S.Finally, finallyCode));
			clauses.Insert(0, node.Args[0]);
			return node.With(S.Try, clauses.ToLNodeList());
		}
		
		public static LNode AutoRemoveParens(LNode node)
		{
			int i = node.Attrs.IndexWithName(S.TriviaInParens);
			if (i > -1)
				return node.WithAttrs(node.Attrs.RemoveAt(i));
			return node;
		}

		[LexicalMacro("throw; throw Expr", "Returns to the caller of the current method or lambda function.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @throw(LNode node, IMessageSink sink)
		{
			if (node.ArgCount > 1) return null;
			return node.With(S.Throw, node.Args); // change throw -> #throw() and throw(x) -> #throw(x)
		}

		[LexicalMacro("return; return Expr", "Returns to the caller of the current method or lambda function.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @return(LNode node, IMessageSink sink)
		{
			if (node.ArgCount <= 1)
				return node.WithTarget(S.Return);
			return null;
		}

		[LexicalMacro("using Disposable {Body...}; using VarName := Disposable {Body...}", "The Dispose() method of the 'Disposable' expression is called when the Body finishes.", "using")]
		public static LNode @using(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.UsingStmt);
			return null;
		}

		[LexicalMacro("this; this(Params...)", "Refers to current object, or calls a constructor in the same class.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @this(LNode node, IMessageSink sink)
		{
			return node.WithName(S.This);
		}

		[LexicalMacro("base; base(Params...)", "Refers to base class, or calls a constructor in the base class.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @base(LNode node, IMessageSink sink)
		{
			return node.WithName(S.Base);
		}

		#endregion

		#region Operators

		[LexicalMacro("(new Type); (new Type(Args...))", "Initializes a new instance of the specified type.")]
		public static LNode @new(LNode node, IMessageSink sink)
		{
			LNode consExpr = node.Args.TryGet(0, null), csharpInitializer = node.Args.TryGet(1, null);
			if (consExpr == null || node.ArgCount > 2 || (csharpInitializer != null && !csharpInitializer.Calls(S.Braces)))
				return null;
			
			if (IsComplexId(consExpr))
				consExpr = F.Call(consExpr);
			if (csharpInitializer != null)
				return F.Call(S.New, consExpr, csharpInitializer.WithTarget(S.Splice));
			else
				return F.Call(S.New, consExpr);
		}

		[LexicalMacro("default(Type)", "The default value for the specified type (@null or an empty structure).", "default")]
		public static LNode defaultPseudoFunc(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 1 && !node.Args[0].Calls(S.Braces))
				return node.WithTarget(S.Default).SetBaseStyle(NodeStyle.Default);
			return null;
		}

		[LexicalMacro(@"cast(Expr, Type); Expr \cast Type", "Converts an expression to a new data type.", "cast", "'->")]
		public static LNode castOperator(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.Cast);
			return null;
		}

		[LexicalMacro(@"Expr `as` Type", "Attempts to cast a reference down to a derived class. The result is null if the cast fails.", "as")]
		public static LNode @as(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.As);
			return null;
		}

		[LexicalMacro(@"Expr `is` Type", "Determines whether a value is an instance of a specified type (@false or @true).", "is")]
		public static LNode @is(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.Is);
			return null;
		}

		// only works as long as : is allowed
		[LexicalMacro(@"condition ? (t : f)", 
			"Attempts to cast a reference down to a derived class. The result is null if the cast fails.", 
			"'?", Mode = MacroMode.Normal | MacroMode.Passive)]
		public static LNode QuestionMark(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2 && node.Args[1].Calls(S.Colon, 2))
				return node.With(S.QuestionMark, node.Args[0], node.Args[1].Args[0], node.Args[1].Args[1]);
			return null;
		}

		[LexicalMacro(@"arg<: value", "Represents a named argument.", "'<:")]
		public static LNode NamedArg(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2 && node.Args[0].IsId) {
				if (node.Name == S.Colon)
					sink.Write(Severity.Warning, node, "Use of `:` for named arguments is deprecated. Use `<:` instead.");
				return node.WithName(S.NamedArg);
			}
			return null;
		}

		static readonly Symbol _array = GSymbol.Get("array");
		static readonly Symbol _opt = GSymbol.Get("opt");
		static readonly Symbol _ptr = GSymbol.Get("ptr");

		[LexicalMacro("array!Type; opt!Type; ptr!Type", 
			"array!Type represents an array of Type; opt!Type represents the nullable version of Type; ptr!Type represents a pointer to Type.",
			"'of", Mode = MacroMode.Normal | MacroMode.Passive)]
		public static LNode of(LNode node, IMessageSink sink)
		{
			LNode kind;
			if (node.ArgCount == 2 && (kind = node.Args[0]).IsId) {
				if (kind.IsIdNamed(_array)) return node.WithArgChanged(0, kind.WithName(S.Array));
				if (kind.IsIdNamed(_opt))   return node.WithArgChanged(0, kind.WithName(S.QuestionMark));
				if (kind.IsIdNamed(_ptr))   return node.WithArgChanged(0, kind.WithName(S._Pointer));
			} else if (node.ArgCount == 3 && (kind = node.Args[0]).IsIdNamed(_array) && node.Args[1].IsLiteral) {
				return node.WithArgs(kind.WithName(S.GetArrayKeyword((int)node.Args[1].Value)), node.Args[2]);
			}
			return null;
		}

		#endregion

		#region Attributes & data types

		private static LNode TranslateId(LNode node, Symbol symbol, IMacroContext checkIsAttr = null)
		{
			if (!node.IsId) return null;
			if (checkIsAttr != null && !checkIsAttr.IsAttribute) return null;
			return node.WithName(symbol);
		}
		static LNode TranslateWordAttr(LNode node, IMacroContext ctx, Symbol attr)
		{
			if (node.ArgCount == 0) {
				if (node.IsId && ctx.IsAttribute)
					return node.WithName(attr);
				return null;
			} else {
				if (node.ArgCount == 1)
					return node.Args[0].PlusAttrs(node.Attrs.Add(F.Id(attr)));
				else // designed for LESv1; this branch is not very useful in LESv2
					return node.PlusAttr(F.Id(attr)).With(node.Args[0], node.Args.RemoveAt(0));
			}
		}

		static LNode TranslateVarAttr(LNode node, IMacroContext sink, Symbol kind)
		{
			// This first part is used to interpret declarations like "readonly x = 5"
			// or "static x = 5" as variable declarations (e.g. [#static] var x = 5)
			var x = @var(node, MessageSink.Null);
			if (x != null)
				return x.PlusAttr(F.Id(kind));
			return TranslateWordAttr(node, sink, kind);
		}

		[LexicalMacro("[pub]", "Used as an attribute to indicate that a type, method or field is publicly accessible.", Mode = MacroMode.MatchIdentifier)]
		public static LNode pub(LNode node, IMacroContext ctx) { return TranslateId(node, S.Public, ctx); }
		[LexicalMacro("[priv]", "Used as an attribute to indicate that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.", Mode = MacroMode.MatchIdentifier)]
		public static LNode priv(LNode node, IMacroContext ctx) { return TranslateId(node, S.Private, ctx); }
		[LexicalMacro("[prot]", "Used as an attribute to indicate that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.", Mode = MacroMode.MatchIdentifier)]
		public static LNode prot(LNode node, IMacroContext ctx) { return TranslateId(node, S.Protected, ctx); }
		[LexicalMacro("[virt]", "Indicates that a method is 'virtual', which means that calls to it can potentially go to a derived class that 'overrides' the method.", Mode = MacroMode.MatchIdentifier)]
		public static LNode virt(LNode node, IMacroContext ctx) { return TranslateId(node, S.Virtual, ctx); }
		[LexicalMacro("public <declaration>", "Indicates that a type, method or field is publicly accessible.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @public(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Public); }
		[LexicalMacro("private <declaration>", "Indicates that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @private(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Private); }
		[LexicalMacro("protected <declaration>", "Indicates that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @protected(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Protected); }
		[LexicalMacro("internal <declaration>", "Indicates that a type, method or field is accessible only inside the same assembly. When combined with prot, it is also accessible to derived classes in different assemblies.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @internal(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Internal); }

		[LexicalMacro("virtual <declaration>", "Indicates that a method is 'virtual', which means that calls to it can potentially go to a derived class that 'overrides' the method.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @virtual(LNode node, IMacroContext context) { return TranslateWordAttr(node, context, S.Virtual); }
		[LexicalMacro("override <declaration>", "Indicates that a method overrides a virtual method in the base class.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @override(LNode node, IMacroContext context) { return TranslateWordAttr(node, context, S.Override); }
		[LexicalMacro("extern <declaration>", "Indicates that the definition is supplies elsewhere.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @extern(LNode node, IMacroContext context) { return TranslateVarAttr(node, context, S.Extern); }
		[LexicalMacro("static <declaration>", "Applies the #static attribute to a declaration.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @static(LNode node, IMacroContext context) { return TranslateVarAttr(node, context, S.Static); }
		[LexicalMacro("unsafe <declaration>", "Indicates that the definition may use 'unsafe' parts of C#, such as pointers", Mode = MacroMode.MatchIdentifier)]
		public static LNode @unsafe(LNode node, IMacroContext context) { return TranslateVarAttr(node, context, S.Unsafe); }

		[LexicalMacro("partial <declaration>", "Indicates that the declared thing may be formed by combining multiple separate parts. When you see this, look for other things with the same name.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @partial(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Partial); }
		[LexicalMacro("readonly Name::Type; readonly Name::Type = Value; readonly Name = Value", "Indicates that a variable cannot be changed after it is initialized.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @readonly(LNode node, IMacroContext context) { return TranslateVarAttr(node, context, S.Readonly); }
		[LexicalMacro("const Name::Type; const Name::Type = Value; const Name = Value", "Indicates a compile-time constant.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @const(LNode node, IMacroContext context) { return TranslateVarAttr(node, context, S.Const); }

		[LexicalMacro("Name::Type", "Defines a variable or field in the current scope.",
			"'::", Mode = MacroMode.Passive)]
		public static LNode VarDecl(LNode node, IMessageSink context)
		{
			var a = node.Args;
			if (a.Count == 2) {
				if (a[0].IsId) {
					return node.With(S.Var, a[1], a[0]).SetBaseStyle(NodeStyle.Default);
				} else if (a[0].CallsMin(S.Tuple, 1)) {
					return node.With(S.Var, LNode.List(a[1]).AddRange(a[0].Args)).SetBaseStyle(NodeStyle.Default);
				} else
					return Reject(context, node, "Expected a variable name or tuple to the left of `{0}`", node.Name);
			}
			return null;
		}
		[LexicalMacro("Name::Type = Value; Name::Type := Value", "Defines a variable or field in the current scope.", "'=", "':=", Mode = MacroMode.Normal | MacroMode.Passive)]
		public static LNode VarDecl2(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode name = a[0], value = a[1];
				if (name.Calls(S.ColonColon, 2))
					return node.With(S.Var, name.Args[1], F.Call(S.Assign, name.Args[0], value));
			}
			return null;
		}
		[LexicalMacro("Name: Type = Value", "Defines a variable or field in the current scope.", "':", Mode = MacroMode.Normal | MacroMode.Passive)]
		public static LNode VarDecl3(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2)
			{
				LNode name = a[0], rest = a[1];
				if (name.IsId)
					if (rest.Calls(S.Assign, 2))
						return node.With(S.Var, rest[0], F.Call(S.Assign, name, rest[1]));
					else if (IsComplexId(rest))
						return node.With(S.Var, rest, name);
			}
			return null;
		}

		[LexicalMacro("[ref]", "Used as an attribute on a method parameter to indicate that it is passed by reference. This means the caller must pass a variable (not a value), and that the caller can see changes to the variable.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @ref(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Ref); }
		[LexicalMacro("[out]", "Used as an attribute on a method parameter to indicate that it is passed by reference. In addition, the called method must assign a value to the variable, and it cannot receive input through the variable.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @out(LNode node, IMacroContext sink) { return TranslateWordAttr(node, sink, S.Out); }

		[LexicalMacro("sbyte", "A signed 8-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @sbyte(LNode node, IMessageSink sink) { return TranslateId(node, S.Int8); }
		[LexicalMacro("byte", "An unsigned 8-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @byte(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt8); }
		[LexicalMacro("short", "A signed 16-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @short(LNode node, IMessageSink sink) { return TranslateId(node, S.Int16); }
		[LexicalMacro("ushort", "An unsigned 16-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @ushort(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt16); }
		[LexicalMacro("int", "A signed 32-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @int(LNode node, IMessageSink sink) { return TranslateId(node, S.Int32); }
		[LexicalMacro("uint", "An unsigned 32-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @uint(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt32); }
		[LexicalMacro("long", "A signed 64-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @long(LNode node, IMessageSink sink) { return TranslateId(node, S.Int64); }
		[LexicalMacro("ulong", "An unsigned 64-bit data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @ulong(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt64); }
		[LexicalMacro("char", "A 16-bit single-character data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @char(LNode node, IMessageSink sink) { return TranslateId(node, S.Char); }
		[LexicalMacro("float", "A 32-bit floating-point data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @float(LNode node, IMessageSink sink) { return TranslateId(node, S.Single); }
		[LexicalMacro("double", "A 64-bit floating-point data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @double(LNode node, IMessageSink sink) { return TranslateId(node, S.Double); }
		[LexicalMacro("bool", "The boolean data type (holds one of two values, @true or @false)", Mode = MacroMode.MatchIdentifier)]
		public static LNode @bool(LNode node, IMessageSink sink) { return TranslateId(node, S.Bool); }
		[LexicalMacro("string", "The string data type: a read-only sequence of characters.", Mode = MacroMode.MatchIdentifier)]
		public static LNode @string(LNode node, IMessageSink sink) { return TranslateId(node, S.String); }
		[LexicalMacro("decimal", "A 128-bit floating-point BCD data type", Mode = MacroMode.MatchIdentifier)]
		public static LNode @decimal(LNode node, IMessageSink sink) { return TranslateId(node, S.Decimal); }
		[LexicalMacro("void", "An empty data type that always has the same value, known as '@void'", Mode = MacroMode.MatchIdentifier)]
		public static LNode @void(LNode node, IMessageSink sink) { return TranslateId(node, S.Void); }
		[LexicalMacro("object", "Common base class of all .NET data types", Mode = MacroMode.MatchIdentifier)]
		public static LNode @object(LNode node, IMessageSink sink) { return TranslateId(node, S.Object); }

		private static LNode TranslateLiteral(LNode node, IMessageSink sink, object literal)
		{
			if (!node.IsId) return null;
			return LNode.Literal(literal, node);
		}

		[LexicalMacro("true", "", Mode = MacroMode.MatchIdentifier)]
		public static LNode @true(LNode node, IMessageSink sink) { return TranslateLiteral(node, sink, true); }
		[LexicalMacro("false", "", Mode = MacroMode.MatchIdentifier)]
		public static LNode @false(LNode node, IMessageSink sink) { return TranslateLiteral(node, sink, false); }
		[LexicalMacro("null", "(Nothing in Visual Basic)", Mode = MacroMode.MatchIdentifier)]
		public static LNode @null(LNode node, IMessageSink sink) { return TranslateLiteral(node, sink, null); }

		#endregion
	}
}
