// Generated from Prelude.Les3.ecs by LeMP custom tool. LeMP version: 2.8.3.0
// Note: you can give command-line arguments to the tool via 'Custom Tool Namespace':
// --no-out-header       Suppress this message
// --verbose             Allow verbose messages (shown by VS as 'warnings')
// --timeout=X           Abort processing thread after X seconds (default: 10)
// --macros=FileName.dll Load macros from FileName.dll, path relative to this file 
// Use #importMacros to use macros in a given namespace, e.g. #importMacros(Loyc.LLPG);
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeMP.Prelude.Les3
{
	/// <summary>Defines the core, predefined constructs of LeMP for LES3 for .NET.</summary>
	[ContainsMacros] 
	public static partial class Macros {
		static Symbol _myNamespace = (Symbol) typeof(Macros).Namespace;
	
		public static IEnumerable<MacroInfo> AliasedMacros()
		{
			return MacroInfo.GetMacros(typeof(LeMP.Les3.To.CSharp.Macros))
			.Concat(LeMP.Les3.To.CSharp.Macros.AliasedMacros())
			.Select(mi => new MacroInfo(_myNamespace, mi, mi.Macro));
		}
	}
}

namespace LeMP.Les3.To.CSharp
{
	using S = Loyc.Syntax.CodeSymbols;
	using Les2 = LeMP.Prelude.Les.Macros;

	/// <summary>Defines the core, predefined constructs of LeMP for LES3 for .NET.</summary>
	[ContainsMacros] 
	public static partial class Macros {
		static readonly Symbol sy_get = (Symbol) "get", sy_set = (Symbol) "set", sy__numprop = (Symbol) "#prop";
		static LNodeFactory F = new LNodeFactory(EmptySourceFile.Synthetic);
	
		static readonly HashSet<string> _aliasedMacros = new HashSet<string>(new[] { 
			
				// Basic types
				"sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", 
				"decimal", "bool", "string", "void", "object"
				, 
				// Attribute keywords
				"out", "ref", "params", 
				"virtual", "abstract", "override", "sealed", "extern", "static", 
				"readonly", "const", "explicit", "implicit", 
				"public", "internal", "private", "protected", 
				"unsafe", "volatile", 
				"pub", "priv", "prot", "virt", 
				"partial", "async"
				, 
				"'?"	// c ? a : b
			});
	
		public static IEnumerable<MacroInfo> AliasedMacros()
		{
			var ns = (Symbol) typeof(Macros).Namespace;
			var les2macros = MacroInfo.GetMacros(typeof(Les2), null, ns);
			var list = (from m in les2macros where m.Names.Any(name => _aliasedMacros.Contains(name)) select m)
		
			.ToList();
			// These macros are not listed in _aliasedMacros to avoid including extra macros 
			// having the same name, e.g. defaultPseudoFunc and defaultCase both watch for
			// things named `default`, but we only want to include one of them here.
			list.Add(MacroInfoFor(LeMP.Prelude.Les.Macros.defaultPseudoFunc));
			list.Add(MacroInfoFor(LeMP.Prelude.Les.Macros.castOperator));
			list.Add(MacroInfoFor(LeMP.Prelude.Les.Macros.of));
			list.Add(MacroInfoFor(LeMP.Prelude.Les.Macros.quickFunction));
			list.Add(MacroInfoFor(LeMP.Prelude.Les.Macros.quickFunctionWithColon));
			return list;
		
			MacroInfo MacroInfoFor(LexicalMacro f) => 
			new MacroInfo(ns, f.Method.GetCustomAttributes(false).OfType<LexicalMacroAttribute>().Single(), f);
		}
	
		[LexicalMacro("class Name { Members; }; class Name(Bases...) { Members... }", 
		"Defines a class (a by-reference data type with data and/or methods).", 
		"#class", Mode = MacroMode.Passive)] 
		public static LNode Class(LNode node, IMacroContext context)
		{
			return Les2.TranslateSpaceDefinition(node, context, S.Class);
		}
		[LexicalMacro("struct Name { Members; }; struct Name(Bases...) { Members... }", 
		"Defines a struct (a by-value data type with data and/or methods).", 
		"#struct", Mode = MacroMode.Passive)] 
		public static LNode Struct(LNode node, IMacroContext context)
		{
			return Les2.TranslateSpaceDefinition(node, context, S.Struct);
		}
		[LexicalMacro("enum Name { Tag1 = Num1; Tag2 = Num2; ... }; enum Name(BaseInteger) { Tag1 = Num1; Tag2 = Num2; ... }", 
		"Defines an enumeration (a integer that represents one of several identifiers, or a combination of bit flags when marked with [Flags]).", 
		"#enum", Mode = MacroMode.Passive)] 
		public static LNode Enum(LNode node, IMacroContext context)
		{
			return Les2.TranslateSpaceDefinition(node, context, S.Enum);
		}
		[LexicalMacro("trait Name { Members; }; trait Name(Bases...) { Members... }", 
		"Not implemented. A set of methods that can be inserted easily into a host class or struct; just add the trait to the host's list of Bases.", 
		"#trait", Mode = MacroMode.Passive)] 
		public static LNode Trait(LNode node, IMacroContext context)
		{
			return Les2.TranslateSpaceDefinition(node, context, S.Trait);
		}
		[LexicalMacro("alias NewName = OldName; alias NewName(Bases...) = OldName; alias NewName(Bases) = OldName { FakeMembers... }", 
		"Not implemented. Defines an alternate view on a data type. If 'Bases' specifies one or more interfaces, a variable of type NewName can be implicitly converted to those interfaces.", 
		"#alias", Mode = MacroMode.Passive)] 
		public static LNode Alias(LNode node, IMacroContext context)
		{
			return Les2.TranslateSpaceDefinition(node, context, S.Alias);
		}
		[LexicalMacro(".using NewName = OldName", "Defines an alias that applies inside the current module only.", "#using")] 
		public static LNode UsingDecl(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 1)
			{
				if (Les2.IsComplexId(node.Args[0])) {
					// Looks like an import statement
					context.Warning(node.Target, "Use #import instead of #using.");
					return node.WithTarget(S.Import);
				}
				var result = Les2.TranslateSpaceDefinition(node, context, S.Alias);
				if (result != null)
					return result.PlusAttr(F.Id(S.FilePrivate));
			}
			return null;
		}
		[LexicalMacro(".fn Name(Args...) { Body... }; .fn Name(Args...): ReturnType { Body }", 
		"Defines a function (also known as a method).", 
		"#fn", Mode = MacroMode.Passive)] 
		public static LNode Fn(LNode node, IMacroContext context)
		{
			return Les2.FnOrCons(node, context, false);
		}
		[LexicalMacro(".cons ClassName(Args...) {Body...}", 
		"Defines a constructor for the enclosing type. To call the base class constructor, call base(...) as the first statement of the Body.", 
		"#cons", Mode = MacroMode.Passive)] 
		public static LNode Cons(LNode node, IMacroContext context)
		{
			return Les2.FnOrCons(node, context, true);
		}
		[LexicalMacro("Name: Type get {Body...} set {Body...}; Name: Type get _ set _", 
		"Defines a property. The getter and setter are optional, but there must be at least one of them.", 
		"'get", "'set", Mode = MacroMode.Passive)] 
		public static LNode QuickProp(LNode node, IMacroContext context)
		{
			{
				LNode getExpr = null, setExpr = null, sig, tmp_10 = null, tmp_11 = null;
				if (node.Calls((Symbol) "'get", 2) && (sig = node.Args[0]) != null && (tmp_10 = node.Args[1]) != null && tmp_10.Calls((Symbol) "'set", 2) && (getExpr = tmp_10.Args[0]) != null && (setExpr = tmp_10.Args[1]) != null || node.Calls((Symbol) "'set", 2) && (sig = node.Args[0]) != null && (tmp_11 = node.Args[1]) != null && tmp_11.Calls((Symbol) "'get", 2) && (setExpr = tmp_11.Args[0]) != null && (getExpr = tmp_11.Args[1]) != null || node.Calls((Symbol) "'get", 2) && (sig = node.Args[0]) != null && (getExpr = node.Args[1]) != null || node.Calls((Symbol) "'set", 2) && (sig = node.Args[0]) != null && (setExpr = node.Args[1]) != null)// Deconstruct the signature
				 {
					LNode name, type;
					if (sig.Calls(CodeSymbols.Colon, 2) && (name = sig.Args[0]) != null && (type = sig.Args[1]) != null) {
						// Build C# property body (the stuff in braces)
						LNode get = ToCSharpGetOrSet(getExpr, sy_get);
						LNode set = ToCSharpGetOrSet(setExpr, sy_set);
						LNode body;
						if (setExpr == null)
							body = LNode.Call(CodeSymbols.Braces, LNode.List(get)).SetStyle(NodeStyle.StatementBlock);
						else if (getExpr == null)
							body = LNode.Call(CodeSymbols.Braces, LNode.List(set)).SetStyle(NodeStyle.StatementBlock);
						else
							body = LNode.Call(CodeSymbols.Braces, LNode.List(get, set)).SetStyle(NodeStyle.StatementBlock);
					
						// Detect indexer (e.g. this[index: int])
						LNode args = LNode.Missing;
						{
							LNode name_apos;
							LNodeList args_apos;
							if (name.CallsMin(CodeSymbols.IndexBracks, 1) && (name_apos = name.Args[0]) != null) {
								args_apos = new LNodeList(name.Args.Slice(1));
								name = name_apos;
								args = LNode.Call(CodeSymbols.AltList, LNode.List(args_apos));
							}
						}
						// Detect property initializer (C# 6)
						LNode output;
						{
							LNode initializer, type_apos;
							if (type.Calls(CodeSymbols.Assign, 2) && (type_apos = type.Args[0]) != null && (initializer = type.Args[1]) != null)
								output = LNode.Call(CodeSymbols.Property, LNode.List(type_apos, name, args, body, initializer));
							else
								output = LNode.Call(CodeSymbols.Property, LNode.List(type, name, args, body));
						}
						return output.WithAttrs(node.Attrs);
					}
				}
			}
			return null;	// Assume it's not a property decl
			LNode ToCSharpGetOrSet(LNode getExpr, Symbol get_) {
				if (getExpr == null)
					return null;
				LNode get = LNode.Id(get_);
				if (getExpr.Calls(S.Braces))
					return LNode.Call(get, LNode.List(getExpr)).SetBaseStyle(NodeStyle.Special);
				else if (getExpr.IsIdNamed("_"))
					return get;
				else
					return LNode.Call(CodeSymbols.Lambda, LNode.List(get, getExpr)).SetStyle(NodeStyle.Operator);
			}
		}
	
		/*static bool DeconstructSignature(LNode sig, out LNode args, out bool isIndexer, out LNode output)
		{
			matchCode(sig) {
			case $name `':` $type:
				// Detect indexer (e.g. this[index: int])
				LNode args = LNode.Missing;
				matchCode(name) {
					case $name'[$(..args')]: 
						name = name';
						args = quote(#($(..args')));
				}
				// Detect property initializer (C# 6)
				LNode output;
				matchCode(type) {
					case $type' = $initializer:
						output = quote { #property($type', $name, $args, $body, $initializer); };
					default:
						output = quote { #property($type, $name, $args, $body); };
				}
				return output.WithAttrs(node.Attrs);
			}
			return false;
		}*/
	
		[LexicalMacro(".prop Name: Type { .get {Body...} .set {Body...} }; .prop Name: Type => value", 
		"Defines a property. The getter and setter are optional, but there must be at least one of them.", "#prop")] 
		public static LNode Prop(LNode node, IMacroContext context)
		{
			LNode visibility = node.Target;
			if (visibility.Name != sy__numprop)
				node = node.PlusAttr(visibility);
		
			{
				LNode getExpr, init = null, name, tmp_12 = null, tmp_13 = null, tmp_14, tmp_15, type;
				LNodeList content;
				if (node.Args.Count == 2 && (tmp_12 = node.Args[0]) != null && tmp_12.Calls(CodeSymbols.Colon, 2) && (name = tmp_12.Args[0]) != null && (type = tmp_12.Args[1]) != null && node.Args[1].Calls(CodeSymbols.Braces) && (content = node.Args[1].Args).IsEmpty | true || node.Args.Count == 3 && (tmp_13 = node.Args[0]) != null && tmp_13.Calls(CodeSymbols.Colon, 2) && (name = tmp_13.Args[0]) != null && (type = tmp_13.Args[1]) != null && node.Args[1].Calls(CodeSymbols.Braces) && (content = node.Args[1].Args).IsEmpty | true && node.Args[2].Calls((Symbol) "#initially", 1) && (init = node.Args[2].Args[0]) != null) {
					LNode args = GetArgList(ref name);
					var newBody = LNode.List();
					foreach (var part_ in content) {
						LNode part = part_;
						{
							LNode body, value;
							if (part.Calls((Symbol) "#get", 0))
								part = LNode.Id(S.get, part);
							else if (part.Calls((Symbol) "#get", 1) && (body = part.Args[0]) != null) {
								part = part.WithName(S.get);
								if (body.Calls(S.Braces))
									part = part.SetBaseStyle(NodeStyle.Special);
								else
									part = part.With(S.Lambda, part.Target, body);
							} else if (part.Calls((Symbol) "#set", 0))
								part = LNode.Id(S.set, part);
							else if (part.Calls((Symbol) "#set", 1) && (body = part.Args[0]) != null) {
								part = part.WithName(S.set);
								if (body.Calls(S.Braces))
									part = part.SetBaseStyle(NodeStyle.Special);
								else
									part = part.With(S.Lambda, part.Target, body);
							} else if (part.Calls((Symbol) "#init", 1) && (value = part.Args[0]) != null) {
								init = value;
								part = null;
							}
						}
						if (part != null)
							newBody.Add(part);
					}
					if (init != null) {
						if (newBody.IsEmpty)
							newBody.Add(F.Id(S.get));
						return node.With(S.Property, type, name, args, F.Braces(newBody), init);
					} else
						return node.With(S.Property, type, name, args, F.Braces(newBody));
				} else if (node.Args.Count == 1 && (tmp_14 = node.Args[0]) != null && tmp_14.Calls(CodeSymbols.Colon, 2) && (name = tmp_14.Args[0]) != null && (tmp_15 = tmp_14.Args[1]) != null && tmp_15.Calls(CodeSymbols.Lambda, 2) && (type = tmp_15.Args[0]) != null && (getExpr = tmp_15.Args[1]) != null) {
					var args = GetArgList(ref name);
					return node.With(S.Property, type, name, args, getExpr);
				}
			}
			return Les2.Reject(context, node, "Unrecognized property syntax");
		
			LNode GetArgList(ref LNode name_apos)
			{
				if (name_apos.Calls(S.IndexBracks)) {
					var args = name_apos.Args.RemoveAt(0);
					name_apos = name_apos[0].PlusAttrs(name_apos.Attrs);
					return F.Call(S.AltList, args);
				} else {
					return LNode.Missing;
				}
			}
		}
	
		[LexicalMacro(".public Name: Type { .get {Body...} .set {Body...} }; .internal Name: Type => value", 
		"Defines a property. The getter and setter are optional, but there must be at least one of them.", 
		"#public", "#private", "#protected", "#internal", "#static", "#virtual", "#override", Mode = MacroMode.Passive)] 
		public static LNode Prop2(LNode node, IMacroContext context)
		{
			return Prop(node, context);
		}
	
		[LexicalMacro(".var Name: Type; .var Name: Type = Value; .var Name = Value", 
		"Defines a variable or field in the current scope. You can define more than one at a time, e.g. 'var X::int Name::string;'", 
		"#var", Mode = MacroMode.Passive)] 
		public static LNode Var(LNode node, IMacroContext context)
		{
			if (node.ArgCount > 1)
				return null;
			return Les2.var(node, context);
		}
		[LexicalMacro(".for (Init, Test, Increment) {Body...};", 
		"Represents the standard C/C++/C#/Java 'for' statement", "#for", Mode = MacroMode.Passive)] 
		public static LNode For(LNode node, IMacroContext context)
		{
			return node.ArgCount == 2 ? Les2.@for(node, context) : null;
		}
	
		[LexicalMacro(@".foreach Item in Collection {Body...}; .foreach Item: Type in Collection {Body...}", "Represents the C# 'foreach' statement.", 
		"#foreach", Mode = MacroMode.Passive)] 
		public static LNode Foreach(LNode node, IMacroContext context)
		{
			return Les2.@foreach(node, context);
		}
	
		[LexicalMacro(".do {...} while Condition", "", "#do", Mode = MacroMode.Passive)] 
		public static LNode Do(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 2 && node.Args[1].Calls(S.While, 1))
				return node.With(S.DoWhile, node[0], node[1].Args[0]);
			return null;
		}
	
		static Symbol __except = GSymbol.Get("#except");
		static Symbol __when = GSymbol.Get("'when");
	
		[LexicalMacro(".try {...} catch e: Exception {...} except {...} finally {...}", "", "#try", Mode = MacroMode.Passive)] 
		public static LNode Try(LNode node, IMacroContext context)
		{
			var a = node.Args.ToWList();
			for (int i = 1; i < a.Count; i++)
			{
				var clause = a[i];
				if (clause.Calls(S.Catch) || clause.Calls(__except))
				{
					if (clause.ArgCount == 1)
					{
						a[i] = clause.WithArgs(F.Missing, F.Missing, a[i].Args[0]);
					} else
					if (clause.ArgCount == 2)
					{
						if (clause.Args[0].Calls(__when, 2))
							a[i] = clause.WithArgs(clause[0][0], clause[0][1], clause[1]);
						else
							a[i] = clause.WithArgs(clause[0], F.Missing, clause[1]);
					}
				}
			}
			if (a.ToLNodeList() != node.Args)
				return node.WithArgs(a.ToVList());
			return null;
		}
	
		static readonly Symbol __elsif = GSymbol.Get("#elsif"), __elseif = GSymbol.Get("#elseif");
		static readonly Symbol __unless = GSymbol.Get("#unless");
	
		[LexicalMacro(".if Condition {...} elsif Condition {...} else {...}", "", 
		"#if", "#unless", Mode = MacroMode.Passive)] 
		public static LNode IfUnless(LNode node, IMacroContext context)
		{
			// #if(cond1, {...}, #elsif(cond2, {...}), #else({...}));
			// #unless(cond1, {...}, #else({...}));
			var args = node.Args;
			bool isUnless = node.Calls(__unless) && args.Count >= 2;
			if (isUnless)
				node = node.WithArgChanged(0, F.Call(S.Not, args[0]));
		
			if (args.Count >= 3)
			{
				LNode clause = args[2];
				bool isElseIf = clause.Calls(__elsif) || clause.Calls(__elseif);
				if (clause.Calls(S.Else, 2) && clause[0].Calls("if", 1)) {
					// Although it's possible to accept "else if (foo)", "else if foo {...}" 
					// would be parsed quite wrongly as "else (if foo {...})"! So issue a 
					// warning to discourage the bad habit of writing "else if".
					context.Warning(clause[0].Target, "'else if' should be one word (elsif or elseif)");
					isElseIf = true;
					clause = clause.WithArgChanged(0, clause[0][0]);
				}
				if (isElseIf)
				{
					var first3 = args.WithoutLast(args.Count - 3);
					// node: #if(cond1, {...}, #elsif(cond2, {...}), #elsif(cond3, {...}), #else({...}))
					//                        ^^^^^^^clause^^^^^^^
					//          ^^^^^^^^^^^^first3^^^^^^^^^^^^^^^^
					// returns: #if(cond1, {...}, #if(cond2, {...}, #elsif(cond3, {...}), #else({...})))
					LNode @else = clause.WithTarget(S.If);
					if (args.Count > 3)
						@else = @else .WithArgs(@else .Args.AddRange(args.Slice(3)));
					return node.WithArgs(first3.WithoutLast(1).Add(@else));
				}
				if (clause.Calls(S.Else, 1) && args.Count == 3)
					return node.WithArgChanged(2, clause[0]);
			}
		
			return isUnless ? node : null;
		}
	
		[LexicalMacro("Name: Type; Name: Type = Value; (Name1, Name2): Type; (Name1 = Val1, Name2 = Val2): Type", 
		"Defines one or more variables or fields in the current scope.", 
		"':", Mode = MacroMode.Passive)] 
		public static LNode VarDecl(LNode node, IMacroContext context)
		{
			var a = node.Args;
			if (a.Count == 2) {
				LNode name = a[0], type = a[1], nameAssignment = name;
				if (type.Calls(S.Assign, 2)) {
					nameAssignment = type.WithArgs(name, type[1]);
					type = type[0];
				}
				if (name.IsId) {
					return node.With(S.Var, type, nameAssignment).SetBaseStyle(NodeStyle.Default);
				} else
					return Les2.Reject(context, node, "Unrecognized variable declaration syntax", node.Name);
			}
			return null;
		}
	
		[LexicalMacro(@"arg <: value", "Represents a named argument.", "'<:")] 
		public static LNode NamedArg(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 2 && node.Args[0].IsId)
				return node.WithName(S.NamedArg);
			return null;
		}
	
		[LexicalMacro(".case Expr: { Code... }; .case Expr1 or Expr2: { Code... }", "A handler in a switch statement. Colon is optional.", 
		"#case", Mode = MacroMode.Passive)] 
		public static LNode @case(LNode node, IMacroContext context)
		{
			LNode expr, braces = null;
			if (node.ArgCount == 2 && (braces = node.Args.Last).Calls(S.Braces) || 
			node.ArgCount == 1 && (node[0].Calls(S.Colon, 2) || node[0].Calls("'or", 2)))
			{
				expr = node[0];
			
				var results = LNode.List();
				while (expr.Calls("'or", 2)) {
					results.Add(LNode.Call(S.Case, LNode.List(expr[0]), expr[0]));
					expr = expr[1];
				}
				if (braces != null) {
					results.Add(LNode.Call(S.Case, LNode.List(expr), expr));
					return F.Call(S.Splice, results.Add(braces));
				} else if (expr.Calls(S.Colon, 2)) {
					results.Add(LNode.Call(S.Case, LNode.List(expr[0]), expr[0]));
					return F.Call(S.Splice, results.Add(expr[1]));
				} else
					return Les2.Reject(context, node, "Unrecognized syntax in case statement");
			}
			return null;
		}
	
		[LexicalMacro(".default; .default { Code... }", "The default label in a switch statement.", 
		"#default", Mode = MacroMode.Passive)] 
		public static LNode @defaultCase(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 0)
				return node.With(S.Label, LNode.Id(S.Default, node)).SetBaseStyle(NodeStyle.Default);
			else if (node.ArgCount == 1)
			{
				var arg = node.Args[0];
				if (arg.Calls(S.Colon, 1)	// .default: {...}
				)
					arg = arg.Args[0];
				else if (!arg.Calls(S.Braces)	// expecting .default {...}
				)
					return null;
				return F.Call(S.Splice, 
				node.With(S.Label, LNode.Id(S.Default, node)).SetBaseStyle(NodeStyle.Default), 
				arg);
			}
			return null;
		}
	
		[LexicalMacro(@"Expr AS Type", "Attempts to cast a reference down to a derived class. The result is null if the cast fails.", "'AS")] 
		public static LNode @as(LNode node, IMacroContext context)
		{
			if (node.ArgCount == 2)
				return node.WithTarget(S.As);
			return null;
		}
	
		[LexicalMacro(".namespace Name { Members... }", 
		"Adds the specified members to a namespace. Namespaces are used to organize code; it is recommended that every data type and method be placed in a namespace. The 'Name' can have multiple levels (A.B.C).", 
		"#namespace")] 
		public static LNode @namespace(LNode node, IMacroContext context)
		{
			return Les2.TranslateSpaceDefinition(node, context, S.Namespace);
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
	}
}