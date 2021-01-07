// Generated from AlgebraicDataType.ecs by LeMP custom tool. LeMP version: 2.9.0.1
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

namespace LeMP
{
	using S = CodeSymbols;

	public partial class StandardMacros
	{
		static readonly Symbol __alt = (Symbol) "#alt";
		static readonly Symbol _alt = (Symbol) "alt";

		[LexicalMacro(@"e.g. alt class Pair<A,B> { alt this(A Item1, B Item2); }", 
		"Expands a short description of an 'algebraic data type' into a set of classes with a common base class. " 
		+ "All data members are read-only, and for each member (e.g. Item1 and Item2 above), " 
		+ "a With() method is generated to let users create modified versions.", 
		"#class", Mode = MacroMode.Passive | MacroMode.Normal)] 
		public static LNode AlgebraicDataType(LNode classDecl, IMacroContext context)
		{
			int? i;
			{
				LNode baseName;
				LNodeList attrs, baseTypes, body;
				if ((attrs = classDecl.Attrs).IsEmpty | true && (i = attrs.FirstIndexWhere(a => a.IsIdNamed(__alt))) != null && classDecl.Calls(CodeSymbols.Class, 3) && (baseName = classDecl.Args[0]) != null && classDecl.Args[1].Calls(CodeSymbols.AltList) && classDecl.Args[2].Calls(CodeSymbols.Braces)) {
					baseTypes = classDecl.Args[1].Args;
					body = classDecl.Args[2].Args;
					attrs = attrs.RemoveAt(i.Value);
					var adt = new AltType(attrs, baseName, baseTypes, null);
					adt.ScanClassBody(body);
					var output = new LNodeList();
					adt.GenerateOutput(ref output);
					return LNode.Call(CodeSymbols.Splice, LNode.List(output));
				}
			}
			return null;
		}

		// Info about one variant of an ADT
		class AltType
		{
		// The Member variables come from...
		// EITHER [$(.._classAttrs)] alt class $TypeName : $(..BaseTypes) { ScanClassBody produces _children }
		// OR     [$(.._classAttrs)] alt $TypeName(... AddParts() is called for this stuff ...)
		// where TypeName consists of $_typeNameStem<$(.._genericArgs)>
			private LNodeList _classAttrs;
			public LNode TypeName;
			public LNodeList BaseTypes;
			public AltType ParentType;
			public AltType(LNodeList classAttrs, LNode typeName, LNodeList baseTypes, AltType parentType)
			{
				_classAttrs = classAttrs;
				TypeName = typeName;
				BaseTypes = baseTypes;
				ParentType = parentType;
				//matchCode (TypeName) {
				//	case $stem<$(..a)>, $stem: 
				//		_typeNameStem = stem;
				//		_genericArgs = a; 
				//  default:
				//		_genericArgs = new WList<LNode>();
				//}
				{	// Above matchCode expanded:
					LNode stem;
					LNodeList a = default(LNodeList);
					if (TypeName.CallsMin(CodeSymbols.Of, 1) && (stem = TypeName.Args[0]) != null && (a = new LNodeList(TypeName.Args.Slice(1))).IsEmpty | true || (stem = TypeName) != null) {
						_typeNameStem = stem;
						_genericArgs = a.ToWList();
					} else {
						_genericArgs = new WList<LNode>();
					}
				}
				if (ParentType != null) {
					BaseTypes.Insert(0, ParentType.TypeNameWithoutAttrs);

					// Search for all 'where' clauses on the ParentType and make sure OUR generic args have them too.
					bool changed = false;
					for (int i = 0; i < _genericArgs.Count; i++) {
						var arg = _genericArgs[i];
						var parentArg = ParentType._genericArgs.FirstOrDefault(a => a.IsIdNamed(arg.Name));
						if (parentArg != null) {
							var wheres = new HashSet<LNode>(WhereTypes(arg));
							int oldCount = wheres.Count;
							var parentWheres = WhereTypes(parentArg);
							foreach (var where in parentWheres)
								wheres.Add(where);
							if (wheres.Count > oldCount) {
								arg = arg.WithAttrs(arg.Attrs.SmartWhere(a => !a.Calls(S.WhereClause))
								.Add(LNode.Call(S.WhereClause, LNode.List(wheres))));
								_genericArgs[i] = arg;
								changed = true;
							}
						}
					}
					if (changed)
						TypeName = LNode.Call(CodeSymbols.Of, LNode.List().Add(_typeNameStem).AddRange(_genericArgs)).SetStyle(NodeStyle.Operator);
				}
				TypeNameWithoutAttrs = TypeName.Select(n => n.WithoutAttrs());
			}

			static IEnumerable<LNode> WhereTypes(LNode genericParameter)
			{
				return genericParameter.Attrs.Where(a => a.Calls(S.WhereClause)).SelectMany(a => a.Args);
			}

			LNode _typeNameStem;
			WList<LNode> _genericArgs;
			LNode TypeNameWithoutAttrs;	// TypeName with type-param attributes (e.g. #in #out #where) removed
			List<AltType> _children = new List<AltType>();
			internal List<AdtParam> Parts = new List<AdtParam>();
			LNodeList _constructorAttrs;
			LNodeList _extraConstrLogic;
			LNodeList _classBody = new LNodeList();

			internal void AddParts(LNodeList parts)
			{
				foreach (var part in parts)
					Parts.Add(new AdtParam(part, this));
			}
			internal void ScanClassBody(LNodeList body)
			{
				foreach (var stmt in body) {
					int? i;
					{
						LNode altName;
						LNodeList attrs, childBody = default(LNodeList), parts, rest;
						if ((attrs = stmt.Attrs).IsEmpty | true && stmt.Calls(CodeSymbols.Fn, 3) && stmt.Args[0].IsIdNamed((Symbol) "alt") && (altName = stmt.Args[1]) != null && stmt.Args[2].Calls(CodeSymbols.AltList) && (parts = stmt.Args[2].Args).IsEmpty | true || (attrs = stmt.Attrs).IsEmpty | true && stmt.Calls(CodeSymbols.Fn, 4) && stmt.Args[0].IsIdNamed((Symbol) "alt") && (altName = stmt.Args[1]) != null && stmt.Args[2].Calls(CodeSymbols.AltList) && (parts = stmt.Args[2].Args).IsEmpty | true && stmt.Args[3].Calls(CodeSymbols.Braces) && (childBody = stmt.Args[3].Args).IsEmpty | true) {
							LNode genericAltName = altName;
							if (altName.CallsMin(CodeSymbols.Of, 1)) { } else {
								if (_genericArgs.Count > 0)
									genericAltName = LNode.Call(CodeSymbols.Of, LNode.List().Add(altName).AddRange(_genericArgs.ToVList())).SetStyle(NodeStyle.Operator);
							}
							var child = new AltType(attrs, genericAltName, LNode.List(), this);
							child.AddParts(parts);
							child.ScanClassBody(childBody);
							_children.Add(child);
						} else if ((attrs = stmt.Attrs).IsEmpty | true && (i = attrs.FirstIndexWhere(a => a.IsIdNamed(__alt))) != null && stmt.CallsMin(CodeSymbols.Constructor, 3) && stmt.Args[1].IsIdNamed((Symbol) "#this") && stmt.Args[2].Calls(CodeSymbols.AltList) && (rest = new LNodeList(stmt.Args.Slice(3))).IsEmpty | true && rest.Count <= 1) {
							parts = stmt.Args[2].Args;
							attrs.RemoveAt(i.Value);
							_constructorAttrs.AddRange(attrs);
							if (rest.Count > 0 && rest[0].Calls(S.Braces))
								_extraConstrLogic.AddRange(rest[0].Args);
							AddParts(parts);
						} else {
							_classBody.Add(stmt);
						}
					}
				}
			}
			// Generates a class declaration for the current alt and its subtypes
			internal void GenerateOutput(ref LNodeList list)
			{
				bool isAbstract = _classAttrs.Any(a => a.IsIdNamed(S.Abstract));

				var baseParts = new List<AdtParam>();
				for (var type = ParentType; type != null; type = type.ParentType)
					baseParts.InsertRange(0, type.Parts);
				var allParts = baseParts.Concat(Parts);

				var initialization = Parts.Select(p => LNode.Call(CodeSymbols.Assign, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id(CodeSymbols.This), p.NameId)).SetStyle(NodeStyle.Operator), p.NameId)).SetStyle(NodeStyle.Operator)).ToList();
				if (baseParts.Count > 0)
					initialization.Insert(0, F.Call(S.Base, baseParts.Select(p => p.NameId)));

				var args = new LNodeList(allParts.Select(p => p.OriginalDecl));
				if (!_constructorAttrs.Any(a => a.IsIdNamed(S.Public)))
					_constructorAttrs.Add(F.Id(S.Public));
				LNode constructor = LNode.Call(LNode.List(_constructorAttrs), CodeSymbols.Constructor, LNode.List(LNode.Missing, _typeNameStem, LNode.Call(CodeSymbols.AltList, LNode.List(args)), LNode.Call(CodeSymbols.Braces, LNode.List().AddRange(initialization).AddRange(_extraConstrLogic)).SetStyle(NodeStyle.StatementBlock)));

				var outBody = new LNodeList();
				outBody.Add(constructor);
				outBody.AddRange(Parts.Select(p => p.GetFieldDecl()));
				outBody.AddRange(baseParts.Select(p => GetWithFn(p, isAbstract, S.Override, allParts)));
				outBody.AddRange(Parts.Select(p => GetWithFn(p, isAbstract, _children.Count > 0 ? S.Virtual : null, allParts)));
				outBody.AddRange(Parts.WithIndexes()
				.Where(kvp => kvp.Value.NameId.Name.Name != "Item" + (baseParts.Count + kvp.Key + 1))
				.Select(kvp => kvp.Value.GetItemDecl(baseParts.Count + kvp.Key + 1)));
				outBody.AddRange(_classBody);

				list.Add(LNode.Call(LNode.List(_classAttrs), CodeSymbols.Class, LNode.List(TypeName, LNode.Call(CodeSymbols.AltList, LNode.List(BaseTypes)), LNode.Call(CodeSymbols.Braces, LNode.List(outBody)).SetStyle(NodeStyle.StatementBlock))));
				if (_genericArgs.Count > 0 && Parts.Count > 0) {
					var argNames = allParts.Select(p => p.NameId);
					list.Add(LNode.Call(LNode.List().AddRange(_classAttrs).Add(LNode.Id(CodeSymbols.Static)).Add(LNode.Id(CodeSymbols.Partial)), CodeSymbols.Class, LNode.List(_typeNameStem, LNode.Call(CodeSymbols.AltList), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(LNode.List(LNode.Id(CodeSymbols.Public), LNode.Id(CodeSymbols.Static)), CodeSymbols.Fn, LNode.List(TypeNameWithoutAttrs, LNode.Call(CodeSymbols.Of, LNode.List().Add(LNode.Id((Symbol) "New")).AddRange(_genericArgs)).SetStyle(NodeStyle.Operator), LNode.Call(CodeSymbols.AltList, LNode.List(args)), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(TypeNameWithoutAttrs, LNode.List(argNames)))))))).SetStyle(NodeStyle.StatementBlock))))).SetStyle(NodeStyle.StatementBlock))));
				}
				foreach (var child in _children)
					child.GenerateOutput(ref list);
			}
			internal LNode GetWithFn(AdtParam part, bool isAbstract, Symbol virtualOverride, IEnumerable<AdtParam> allParts)
			{
				int totalParts = allParts.Count();
				var withField = F.Id("With" + part.NameId.Name);

				var args = LNode.List();
				foreach (AdtParam otherPart in allParts) {
					if (part == otherPart)
						args.Add(F.Id("newValue"));
					else
						args.Add(otherPart.NameId);
				}

				var attrs = new LNodeList(F.Id(S.Public));
				if (isAbstract)
					attrs.Add(F.Id(S.Abstract));
				if (virtualOverride != null && (!isAbstract || virtualOverride == S.Override))
					attrs.Add(F.Id(virtualOverride));

				LNode method;
				LNode type = part.Type;
				LNode retType = part.ContainingType.TypeNameWithoutAttrs;
				if (isAbstract) {
					method = LNode.Call(LNode.List(attrs), CodeSymbols.Fn, LNode.List(retType, withField, LNode.Call(CodeSymbols.AltList, LNode.List(LNode.Call(LNode.List(part.OriginalDecl.Attrs), CodeSymbols.Var, LNode.List(type, LNode.Id((Symbol) "newValue")))))));
				} else {
					method = LNode.Call(LNode.List(attrs), CodeSymbols.Fn, LNode.List(retType, withField, LNode.Call(CodeSymbols.AltList, LNode.List(LNode.Call(LNode.List(part.OriginalDecl.Attrs), CodeSymbols.Var, LNode.List(type, LNode.Id((Symbol) "newValue"))))), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(TypeNameWithoutAttrs, LNode.List(args)))))))).SetStyle(NodeStyle.StatementBlock)));
				}
				return method;
			}
		}

		// Info about one parameter of one ADT
		class AdtParam
		{
			internal LNode OriginalDecl;
			internal AltType ContainingType;
			internal AdtParam(LNode originalDecl, AltType containingType)
			{
				OriginalDecl = originalDecl;
				ContainingType = containingType;
				if (!OriginalDecl.Calls(S.Var, 2))
					throw new LogException(OriginalDecl, "alt: Expected a variable declaration");
				Type = OriginalDecl.Args[0];
				NameId = OriginalDecl.Args[1];
				if (NameId.Calls(S.Assign, 2))
					NameId = NameId.Args[0];
				if (!NameId.IsId)
					throw new LogException(NameId, "alt: Expected a simple variable name");
			}

			internal readonly LNode Type;
			internal readonly LNode NameId;

			internal LNode GetFieldDecl() {
				return LNode.Call(LNode.List(LNode.Id(CodeSymbols.Public)), CodeSymbols.Property, LNode.List(Type, NameId, LNode.Missing, LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Id(CodeSymbols.get), LNode.Id(LNode.List(LNode.Id(CodeSymbols.Private)), CodeSymbols.set))).SetStyle(NodeStyle.StatementBlock)));
			}
			internal LNode GetItemDecl(int itemNum) {
				LNode ItemN = F.Id("Item" + itemNum);
				// ItemN properties are used by the code generated for pattern matching
				return LNode.Call(LNode.List(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "System"), LNode.Id((Symbol) "ComponentModel"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "EditorBrowsable"))).SetStyle(NodeStyle.Operator), LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "System"), LNode.Id((Symbol) "ComponentModel"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "EditorBrowsableState"))).SetStyle(NodeStyle.Operator), LNode.Id((Symbol) "Never"))).SetStyle(NodeStyle.Operator))), LNode.Id(CodeSymbols.Public)), CodeSymbols.Property, LNode.List(Type, ItemN, LNode.Missing, LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.get, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(NameId)))).SetStyle(NodeStyle.StatementBlock))).SetStyle(NodeStyle.Special))).SetStyle(NodeStyle.StatementBlock)));
			}
		}
	}
}