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

/// <summary>Defines prelude macros, which are predefined macros that normally 
/// do not have to be explicitly imported before use (in LES or EC#).</summary>
namespace LeMP.Prelude
{
	/// <summary>Defines <c>noMacro(...)</c> for suppressing macro expansion and 
	/// <c>import macros your.namespace.name</c> as an alias for 
	/// <c>#importMacros(your.namespace.name)</c>.
	/// </summary>
	[ContainsMacros]
	public static partial class Macros
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);
		
		[LexicalMacro("noMacro(Code)", "Pass code through to the output language, without macro processing.", 
			Mode = MacroMode.NoReprocessing)]
		public static LNode noMacro(LNode node, IMessageSink sink)
		{
			if (!node.IsCall)
				return null;
			return node.WithTarget(S.Splice);
		}
		
		static readonly Symbol _macros = GSymbol.Get("macros");
		static readonly Symbol _importMacros = GSymbol.Get("#importMacros");

		[LexicalMacro("import macros Namespace",
			"Use macros from specified namespace. The 'macros' modifier imports macros only, deleting this statement from the output.")]
		public static LNode import(LNode node, IMessageSink sink)
		{
			if (node.Args.TryGet(0, F._Missing).IsIdNamed(_macros))
				return node.With(_importMacros, node.Args.RemoveAt(0));
			return null;
		}
	}

	/// <summary>Defines prelude macros for LES, which are predefined macros that 
	/// normally do not have to be explicitly imported before use.</summary>
	namespace Les
	{

	/// <summary>Defines the core, predefined constructs of LeMP for LES.</summary>
	[ContainsMacros]
	public static partial class Macros
	{
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Default);

		static LNode Reject(IMessageSink error, LNode at, string msg)
		{
			error.Write(Severity.Note, at, msg);
			return null;
		}

		#region Definition statements (classes, methods, etc.)

		static readonly Symbol _macros = GSymbol.Get("macros");

		[LexicalMacro("import Namespace;", "Use symbols from specified namespace ('using' in C#).")]
		public static LNode import(LNode node, IMessageSink sink)
		{
			if (!node.Args.TryGet(0, F._Missing).IsIdNamed(_macros))
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
		public static LNode @using1(LNode node, IMacroContext sink)
		{
			if (node.ArgCount == 1 && IsComplexId(node.Args[0])) {
				// Looks like an import statement
				sink.Write(Severity.Warning, node.Target, "The 'import' statement replaces the 'using' statement in LeMP.");
				return node.WithTarget(S.Import);
			}
			var result = TranslateSpaceDefinition(node, sink, S.Alias);
			if (result != null)
				return result.PlusAttr(F.Id(S.FilePrivate));
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
					return Reject(context, node, "A type definition must have the form kind(Name, { Body }) or kind(Name(Bases), { Body }) (where «kind» is struct/class/enum/trait/alias)");
			}
			if (isAlias) {
				if (!nameEtc.Calls(S.Assign, 2))
					return Reject(context, node, "An 'alias' (or 'using') definition must have the form alias(NewName = OldName, { Body }) or alias(NewName(Interfaces) = OldName, { Body })");
				oldName = nameEtc.Args[1];
				nameEtc = nameEtc.Args[0];
			}

			LNode name, bases;
			if (IsComplexId(nameEtc, true)) {
				name = nameEtc;
				bases = F.List();
			} else {
				name = nameEtc.Target ?? nameEtc;
				bases = nameEtc.WithTarget(S.List);
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
		// A complex identifier has the form Id, ComplexId.Id, or ComplexId!(ComplexId, ...)
		// where Id is a simple identifier and ComplexId is a complex identifier. Also, the
		// form X!Y!Z, i.e. #of(#of(...), ...) is not allowed. $Substitution is allowed.
		public static bool IsComplexId(LNode id, bool allowOf = true)
		{
			if (id.IsCall) {
				if (id.Name == S.Of) {
					if (allowOf)
						return (id.HasSimpleHead() || IsComplexId(id.Target, false)) && id.Args.All(a => IsComplexId(a));
					return false;
				} else if (id.Calls(S.Dot, 2)) {
					return id.Args.Last.IsId && IsComplexId(id.Args[0]);
				} else if (id.Calls(S.Substitute, 1)) {
					return true;
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

		[LexicalMacro("def Name(Args...) { Body... }; def Name(Args...)::ReturnType { Body }; def Name ==> ForwardingTarget { Body }",
			"Defines a function (also known as a method). The '==> ForwardingTarget' version is not implemented.")]
		public static LNode @def(LNode node, IMessageSink sink)
		{
			return DefOrConstructor(node, sink, false);
		}
		[LexicalMacro("fn Name(Args...) { Body... }; fn Name(Args...)::ReturnType { Body }; fn Name ==> ForwardingTarget { Body }",
			"Defines a function (also known as a method). The '==> ForwardingTarget' version is not implemented.")]
		public static LNode @fn(LNode node, IMessageSink sink)
		{
			return DefOrConstructor(node, sink, false);
		}
		[LexicalMacro("cons ClassName(Args...) {Body...}", "Defines a constructor for the enclosing type. To call the base class constructor, call base(...) as the first statement of the Body.")]
		public static LNode cons(LNode node, IMessageSink sink)
		{
			return DefOrConstructor(node, sink, true);
		}
		static LNode DefOrConstructor(LNode node, IMessageSink sink, bool isCons)
		{
			var parts = node.Args;
			LNode sig = parts.TryGet(0, null), body = parts.TryGet(1, null);
			if (!parts.Count.IsInRange(1, 2) || !sig.IsCall || (body != null && !body.Calls(S.Braces)))
				return null;
			
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
			if (retVal != null && retVal.Calls(S.Braces) && body == null) {
				body = retVal;
				retVal = F._Missing;
			}
			var name = sig.Target ?? sig;
			if (!IsTargetDefinitionId(sig, true))
				return Reject(sink, sig.Target, "Invalid method name");
			var argList = sig.ArgCount != 0 ? sig.WithTarget(S.List) : F.List();

			if (retVal == null)
				retVal = isCons ? F._Missing : F.Void;
			else if (isCons)
				return Reject(sink, retVal, "A constructor cannot have a return type");

			Symbol kind = isCons ? S.Cons : S.Fn;
			if (body != null)
				return node.With(kind, retVal, name, argList, body);
			else if (forwardTo != null)
				return node.With(kind, retVal, name, argList, F.Call(S.Forward, forwardTo));
			else
				return node.With(kind, retVal, name, argList);
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

		//static readonly LNode trivia_macroCall = F.Id(S.TriviaMacroCall);
		//// TEMP: probably we should use NodeStyle.Special instead of #trivia_macroCall.
		//// In that case this macro will be unnecessary, as get {...} will already 
		//// be marked NodeStyle.Special due to its syntax, whereas get({...}); is not.
		//[SimpleMacro("get {...}; set {...}", "Adds #trivia_macroCall attr for C# printing", "get", "set", "add", "remove",
		//    Mode = MacroMode.ProcessChildrenAfter | MacroMode.Passive)] // avoid being called a second time
		//public static LNode GetSet(LNode node, IMessageSink sink)
		//{
		//    if (node.Style == NodeStyle.Special && node.AttrNamed(S.TriviaMacroCall) == null)
		//        return node.WithAttr(trivia_macroCall);
		//    return null;
		//}
		

		[LexicalMacro("var Name::Type; var Name::Type = Value; var Name = Value",
			"Defines a variable or field in the current scope. You can define more than one at a time, e.g. 'var X::int Name::string;'")]
		public static LNode @var(LNode node, IMessageSink sink)
		{
			var parts = node.Args;
			if (parts.Count == 0)
				return Reject(sink, node, "A variable definition must have the form var(Name::Type), var(Name = value), or var(Name::Type = value)");
			if (parts[0].IsId)
				return null; // e.g. this is true for "static readonly x::Foo"

			RWList<LNode> varStmts = null;
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
				type = type ?? F._Missing;

				var nameAndInit = init == null ? part : F.Call(S.Assign, part, init);
				if (varStmt != null && varStmt.Args[0].Equals(type)) {
					// same type used again, e.g. (var x::int y::int) => (#var int x y)
					varStmt = varStmt.WithArgs(varStmt.Args.Add(nameAndInit));
				} else {
					// first item (var x::int => #var int x) or type changed (var a::A b::B => #var A a; #var B b)
					if (varStmt != null) {
						varStmts = varStmts ?? new RWList<LNode>();
						varStmts.Add(varStmt);
					}
					varStmt = node.With(S.Var, type, nameAndInit);
				}
			}
			
			// Return a single statement or a list of them if necessary
			if (varStmts != null) {
				varStmts.Add(varStmt);
				return F.Call(S.Splice, varStmts.ToRVList());
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
			if (node.ArgCount == 2 && (tuple = node.Args[0]).Calls(S.Tuple, 3))
				return node.With(S.For, tuple.Args[0], tuple.Args[1], tuple.Args[2], node.Args[1]);
			else if (node.ArgCount == 4)
				return node.WithTarget(S.For);
			return null;
		}

		static readonly Symbol _in = GSymbol.Get("in");

		[LexicalMacro(@"foreach Item \in Collection {Body...}; foreach Item::Type \in Collection {Body...}", "Represents the C# 'foreach' statement.")]
		public static LNode @foreach(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			if (args.Count == 2 && args[0].Calls(_in, 2)) {
				LNode decl = args[0].Args[0], list = args[0].Args[1], body = args[1];
				if (decl.IsId)
					decl = F.Var(F._Missing, decl);
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
				return node.With(S.DoWhile, new RVList<LNode>(node.Args[0], node.Args[1].Args[0]));
			} else if (node.ArgCount == 3 && args.TryGet(1, null).IsIdNamed(_while)) {
				return node.With(S.DoWhile, new RVList<LNode>(node.Args[0], node.Args[2]));
			}
			return null;
		}
		static readonly Symbol _while = GSymbol.Get("while");

		static readonly Symbol _else = GSymbol.Get("else");
		[LexicalMacro("if Condition {Then...}; if Condition {Then...} else {Else...}",
			"If 'Condition' is true, runs the 'Then' code; otherwise, runs the 'Else' code, if any.")]
		public static LNode @if(LNode node, IMessageSink sink)
		{
			var args = node.Args;
			LNode cond = args.TryGet(0, null), then = args.TryGet(1, null), @else = args.TryGet(3, null);
			if (node.ArgCount != 2 && (node.ArgCount != 4 || !args.TryGet(2, null).IsIdNamed(_else)))
				if (node.ArgCount > 2)
					return Reject(sink, node, "An if-statement must have the form «if(Cond, expr)» or «if(Cond, ThenClause, else, ElseClause)»");
				else
					return null;
			if (@else == null)
				return node.With(S.If, cond, then);
			else
				return node.With(S.If, cond, then, @else);
		}

		[LexicalMacro("unless Condition {Then...}; unless Condition {Then...} else {Else...}",
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

		[LexicalMacro("switch Value { case ConstExpr1; Handler1; break; case ConstExpr2; Handler2; break; default; DefaultHandler; }",
			"Chooses one of several code paths based on the specified 'Value'.")]
		public static LNode @switch(LNode node, IMessageSink sink)
		{
			return TranslateCall(node, S.Switch);
		}

		[LexicalMacro("break", "Exit the loop or switch body (the innermost loop, if more than one enclosing loop)")]
		public static LNode @break(LNode node, IMessageSink sink)
		{
			if (!node.IsId) return null;
			return node.WithTarget(S.Break);
		}

		[LexicalMacro("continue", "Jump to the end of the loop body, running the loop again if the loop condition is true.")]
		public static LNode @continue(LNode node, IMessageSink sink)
		{
			if (!node.IsId) return null;
			return node.WithTarget(S.Continue);
		}

		[LexicalMacro("case ConstExpr; case ConstExpr { Code... }", "One label in a switch statement.")]
		public static LNode @case(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 1)
				return node.WithTarget(S.Case);
			else if (node.ArgCount == 2 && node.Args[1].Calls(S.Braces))
				return F.Call(S.Splice, new RVList<LNode>(node.WithArgs(node.Args.First(1)), node.Args[1]));
			return null;
		}
		
		[LexicalMacro("default; default { Code... }", "The default label in a switch statement.", "default")]
		public static LNode @default1(LNode node, IMessageSink sink)
		{
			if (node.IsId)
				return node.With(S.Label, F.Id(S.Default));
			else if (node.ArgCount == 1 && node.Args[0].Calls(S.Braces))
				return F.Call(S.Splice, new RVList<LNode>(node.With(S.Label, new RVList<LNode>(F.Id(S.Default))), node.Args[0]));
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
			RWList<LNode> clauses = new RWList<LNode>();
			var parts = node.Args;
			
			for (int i = parts.Count-2; i >= 1; i -= 2)
			{
				var p = parts[i];
				if (p.IsIdNamed(_finally)) {
					if (clauses.Count != 0 || finallyCode != null)
						sink.Write(Severity.Error, p, "The «finally» clause must come last, there can only be one of them.");
					finallyCode = parts[i+1];
				} else if (p.Name == _catch) {
					if (p.ArgCount > 0) {
						// This is a normal catch clause
						clauses.Insert(0, F.Call(S.Catch, F.Call(S.Splice, p.Args), parts[i + 1]));
					} else {
						// This is a catch-all clause (the type argument is missing)
						if (clauses.Count != 0)
							sink.Write(Severity.Error, p, "The catch-all clause must be the last «catch» clause.");
						clauses.Add(F.Call(S.Catch, F._Missing, parts[i + 1]));
					}
				} else if (i > 1 && parts[i-1].IsIdNamed(_catch)) {
					// This is a normal catch clause
					clauses.Insert(0, F.Call(S.Catch, p, parts[i+1]));
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
			return node.With(S.Try, clauses.ToRVList());
		}

		[LexicalMacro("return; return Expr", "Returns to the caller of the current method or lambda function.")]
		public static LNode @throw(LNode node, IMessageSink sink)
		{
			if (node.ArgCount > 1) return null;
			return node.With(S.Throw, node.Args); // change throw -> #throw() and throw(x) -> #throw(x)
		}

		[LexicalMacro("return; return Expr", "Returns to the caller of the current method or lambda function.")]
		public static LNode @return(LNode node, IMessageSink sink)
		{
			if (node.ArgCount <= 1)
				return node.WithTarget(S.Return);
			return null;
		}

		[LexicalMacro("using Disposable {Body...}; using VarName := Disposable {Body...}", "The Dispose() method of the 'Disposable' expression is called when the Body finishes.", "using")]
		public static LNode @using2(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.UsingStmt);
			return null;
		}

		[LexicalMacro("this(Params...)", "Calls a constructor in the same class. Can only be used inside a constructor.")]
		public static LNode @this(LNode node, IMessageSink sink)
		{
			return node.WithName(S.This);
		}

		[LexicalMacro("base(Params...)", "Calls a constructor in the base class. Can only be used inside a constructor.")]
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
		public static LNode @default2(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 1 && !node.Args[0].Calls(S.Braces))
				return node.WithTarget(S.Default);
			return null;
		}

		[LexicalMacro(@"cast(Expr, Type); Expr \cast Type", "Converts an expression to a new data type.", "cast", "->")]
		public static LNode cast(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.Cast);
			return null;
		}

		[LexicalMacro(@"Expr \as Type", "Attempts to cast a reference down to a derived class. The result is null if the cast fails.")]
		public static LNode @as(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.As);
			return null;
		}

		// only works as long as : is allowed
		[LexicalMacro(@"condition ? (t : f)", "Attempts to cast a reference down to a derived class. The result is null if the cast fails.", "?")]
		public static LNode QuestionMark(LNode node, IMessageSink sink)
		{
			if (node.ArgCount == 2 && node.Args[1].Calls(S.Colon, 2))
				return node.With(S.QuestionMark, node.Args[0], node.Args[1].Args[0], node.Args[1].Args[1]);
			return null;
		}

		// only works as long as : is allowed
		[LexicalMacro(@"arg: value", "Represents a named argument.", ":")]
		public static LNode NamedArg(LNode node, IMessageSink sink)
		{
			if (node.Calls(S.Colon, 2) && node.Args[0].IsId)
				return node.WithName(S.NamedArg);
			return null;
		}

		static readonly Symbol _array = GSymbol.Get("array");
		static readonly Symbol _opt = GSymbol.Get("opt");
		static readonly Symbol _ptr = GSymbol.Get("ptr");

		[LexicalMacro("array!Type; opt!Type; ptr!Type", "array!Type represents an array of Type; opt!Type represents the nullable version of Type; ptr!Type represents a pointer to Type.", "#of")]
		public static LNode of(LNode node, IMessageSink sink)
		{
			LNode kind;
			if (node.ArgCount == 2 && (kind = node.Args[0]).IsId) {
				if (kind.IsIdNamed(_array)) return node.WithArgChanged(0, kind.WithName(S._Array));
				if (kind.IsIdNamed(_opt))   return node.WithArgChanged(0, kind.WithName(S.QuestionMark));
				if (kind.IsIdNamed(_ptr))   return node.WithArgChanged(0, kind.WithName(S._Pointer));
			} else if (node.ArgCount == 3 && (kind = node.Args[0]).IsIdNamed(_array) && node.Args[1].IsLiteral) {
				return node.WithArgs(kind.WithName(S.GetArrayKeyword((int)node.Args[1].Value)), node.Args[2]);
			}
			return null;
		}

		#endregion

		#region Attributes & data types

		private static LNode TranslateId(LNode node, Symbol symbol)
		{
			if (!node.IsId) return null;
			return node.WithName(symbol);
		}
		static LNode TranslateWordAttr(LNode node, IMessageSink sink, Symbol attr)
		{
			//LNode result = TranslateId(node, attr);
			//if (result == null && node.ArgCount >= 1) {
			if (node.ArgCount < 1) 
				return null;
			else {
				if (node.ArgCount > 1)
					return node.PlusAttr(F.Id(attr)).With(node.Args[0], node.Args.RemoveAt(0));
				else
					return node.Args[0].PlusAttr(F.Id(attr));
			}
		}

		static LNode TranslateVarAttr(LNode node, IMessageSink sink, Symbol kind)
		{
			// This first part is used to interpret declarations like "readonly x = 5"
			// or "static x = 5" as variable declarations (e.g. [#static] var x = 5)
			var x = @var(node, MessageSink.Null);
			if (x != null)
				return x.PlusAttr(F.Id(kind));
			
			x = TranslateWordAttr(node, sink, kind);
			return x;
		}

		[LexicalMacro("[pub]", "Used as an attribute to indicate that a type, method or field is publicly accessible.")]
		public static LNode pub(LNode node, IMessageSink sink) { return TranslateId(node, S.Public); }
		[LexicalMacro("[priv]", "Used as an attribute to indicate that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.")]
		public static LNode priv(LNode node, IMessageSink sink) { return TranslateId(node, S.Private); }
		[LexicalMacro("[prot]", "Used as an attribute to indicate that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.")]
		public static LNode prot(LNode node, IMessageSink sink) { return TranslateId(node, S.Protected); }
		[LexicalMacro("[virt]", "Indicates that a method is 'virtual', which means that calls to it can potentially go to a derived class that 'overrides' the method.")]
		public static LNode virt(LNode node, IMessageSink sink) { return TranslateId(node, S.Virtual); }
		[LexicalMacro("public <declaration>", "Indicates that a type, method or field is publicly accessible.")]
		public static LNode @public(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Public); }
		[LexicalMacro("private <declaration>", "Indicates that a method, field or inner type is private, meaning it is inaccessible outside the scope in which it is defined.")]
		public static LNode @private(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Private); }
		[LexicalMacro("protected <declaration>", "Indicates that a method, field or inner type has protected accessibility, meaning it only accessible in the current scope and in the scope of derived classes.")]
		public static LNode @protected(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Protected); }
		[LexicalMacro("internal <declaration>", "Indicates that a type, method or field is accessible only inside the same assembly. When combined with prot, it is also accessible to derived classes in different assemblies.")]
		public static LNode @internal(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Internal); }

		[LexicalMacro("virtual <declaration>", "Indicates that a method is 'virtual', which means that calls to it can potentially go to a derived class that 'overrides' the method.")]
		public static LNode @virtual(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Virtual); }
		[LexicalMacro("override <declaration>", "Indicates that a method overrides a virtual method in the base class.")]
		public static LNode @override(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Override); }
		[LexicalMacro("extern <declaration>", "Indicates that the definition is supplies elsewhere.")]
		public static LNode @extern(LNode node, IMessageSink sink) { return TranslateVarAttr(node, sink, S.Extern); }
		[LexicalMacro("static <declaration>", "Applies the #static attribute to a declaration.")]
		public static LNode @static(LNode node, IMessageSink sink) { return TranslateVarAttr(node, sink, S.Static); }
		[LexicalMacro("unsafe <declaration>", "Indicates that the definition may use 'unsafe' parts of C#, such as pointers")]
		public static LNode @unsafe(LNode node, IMessageSink sink) { return TranslateVarAttr(node, sink, S.Unsafe); }

		[LexicalMacro("partial <declaration>", "Indicates that the declared thing may be formed by combining multiple separate parts. When you see this, look for other things with the same name.")]
		public static LNode @partial(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Partial); }
		[LexicalMacro("readonly Name::Type; readonly Name::Type = Value; readonly Name = Value", "Indicates that a variable cannot be changed after it is initialized.")]
		public static LNode @readonly(LNode node, IMessageSink sink) { return TranslateVarAttr(node, sink, S.Readonly); }
		[LexicalMacro("const Name::Type; const Name::Type = Value; const Name = Value", "Indicates a compile-time constant.")]
		public static LNode @const(LNode node, IMessageSink sink) { return TranslateVarAttr(node, sink, S.Const); }

		[LexicalMacro("Name::Type", "Defines a variable or field in the current scope.", "::")]
		public static LNode ColonColon(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				var r = node.With(S.Var, a[1], a[0]);
				r.BaseStyle = NodeStyle.Operator;
				return r;
			}
			return null;
		}
		[LexicalMacro("Name::Type = Value; Name::Type := Value", "Defines a variable or field in the current scope.", "=", ":=")]
		public static LNode ColonColonInit(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode name = a[0], value = a[1];
				if (name.Calls(S.ColonColon, 2))
					return node.With(S.Var, name.Args[1], F.Call(S.Assign, name.Args[0], value));
			}
			return null;
		}
		[LexicalMacro("Name := Value", "Defines a variable or field in the current scope.", ":=")]
		public static LNode ColonEquals(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode name = a[0], value = a[1];
				return node.With(S.Var, F._Missing, F.Call(S.Assign, name, value));
			}
			return null;
		}
		[LexicalMacro("Value=:Name", "Defines a variable or field in the current scope.", "=:")]
		public static LNode QuickBind(LNode node, IMessageSink sink)
		{
			var a = node.Args;
			if (a.Count == 2)
				return node.With(S.Var, new RVList<LNode>(F._Missing, F.Call(S.Assign, a[1], a[0])));
			return null;
		}
		
		[LexicalMacro("[ref]", "Used as an attribute on a method parameter to indicate that it is passed by reference. This means the caller must pass a variable (not a value), and that the caller can see changes to the variable.")]
		public static LNode @ref(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Ref); }
		[LexicalMacro("[out]", "Used as an attribute on a method parameter to indicate that it is passed by reference. In addition, the called method must assign a value to the variable, and it cannot receive input through the variable.")]
		public static LNode @out(LNode node, IMessageSink sink) { return TranslateWordAttr(node, sink, S.Out); }

		[LexicalMacro("sbyte", "A signed 8-bit data type")]
		public static LNode @sbyte(LNode node, IMessageSink sink) { return TranslateId(node, S.Int8); }
		[LexicalMacro("byte", "An unsigned 8-bit data type")]
		public static LNode @byte(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt8); }
		[LexicalMacro("short", "A signed 16-bit data type")]
		public static LNode @short(LNode node, IMessageSink sink) { return TranslateId(node, S.Int16); }
		[LexicalMacro("ushort", "An unsigned 16-bit data type")]
		public static LNode @ushort(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt16); }
		[LexicalMacro("int", "A signed 32-bit data type")]
		public static LNode @int(LNode node, IMessageSink sink) { return TranslateId(node, S.Int32); }
		[LexicalMacro("uint", "An unsigned 32-bit data type")]
		public static LNode @uint(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt32); }
		[LexicalMacro("long", "A signed 64-bit data type")]
		public static LNode @long(LNode node, IMessageSink sink) { return TranslateId(node, S.Int64); }
		[LexicalMacro("ulong", "An unsigned 64-bit data type")]
		public static LNode @ulong(LNode node, IMessageSink sink) { return TranslateId(node, S.UInt64); }
		[LexicalMacro("char", "A 16-bit single-character data type")]
		public static LNode @char(LNode node, IMessageSink sink) { return TranslateId(node, S.Char); }
		[LexicalMacro("float", "A 32-bit floating-point data type")]
		public static LNode @float(LNode node, IMessageSink sink) { return TranslateId(node, S.Single); }
		[LexicalMacro("double", "A 64-bit floating-point data type")]
		public static LNode @double(LNode node, IMessageSink sink) { return TranslateId(node, S.Double); }
		[LexicalMacro("bool", "The boolean data type (holds one of two values, @true or @false)")]
		public static LNode @bool(LNode node, IMessageSink sink) { return TranslateId(node, S.Bool); }
		[LexicalMacro("string", "The string data type: a read-only sequence of characters.")]
		public static LNode @string(LNode node, IMessageSink sink) { return TranslateId(node, S.String); }
		[LexicalMacro("decimal", "A 128-bit floating-point BCD data type")]
		public static LNode @decimal(LNode node, IMessageSink sink) { return TranslateId(node, S.Decimal); }
		[LexicalMacro("void", "An empty data type that always has the same value, known as '@void'")]
		public static LNode @void(LNode node, IMessageSink sink) { return TranslateId(node, S.Void); }
		[LexicalMacro("object", "Common base class of all .NET data types")]
		public static LNode @object(LNode node, IMessageSink sink) { return TranslateId(node, S.Object); }

		private static LNode TranslateLiteral(LNode node, IMessageSink sink, object literal)
		{
			if (!node.IsId) return null;
			return LNode.Literal(literal, node);
		}

		[LexicalMacro("true", "")]
		public static LNode @true(LNode node, IMessageSink sink) { return TranslateLiteral(node, sink, true); }
		[LexicalMacro("false", "")]
		public static LNode @false(LNode node, IMessageSink sink) { return TranslateLiteral(node, sink, false); }
		[LexicalMacro("null", "(Nothing in Visual Basic)")]
		public static LNode @null(LNode node, IMessageSink sink) { return TranslateLiteral(node, sink, null); }

		#endregion
	}

	} // namespace Les
}
