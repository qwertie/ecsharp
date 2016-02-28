// Generated from AlgebraicDataType.ecs by LeMP custom tool. LeMP version: 1.4.1.0
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
		[LexicalMacro("e.g. alt class Tree<T> { alt Node(Tree<T> Left, Tree<T> Right); alt Leaf(T Value); }", "Expands a short description of an 'algebraic data type' into a set of classes with a common base class.", "#class", Mode = MacroMode.Passive | MacroMode.Normal)] public static LNode AlgebraicDataType(LNode classDecl, IMacroContext context)
		{
			int i;
			{
				LNode baseName;
				RVList<LNode> attrs, baseTypes, body;
				if ((attrs = classDecl.Attrs).IsEmpty | true && (i = attrs.IndexWhere(a => a.IsIdNamed(__alt))) > -1 && classDecl.Calls(CodeSymbols.Class, 3) && (baseName = classDecl.Args[0]) != null && classDecl.Args[1].Calls(CodeSymbols.AltList) && classDecl.Args[2].Calls(CodeSymbols.Braces)) {
					baseTypes = classDecl.Args[1].Args;
					body = classDecl.Args[2].Args;
					attrs = attrs.RemoveAt(i);
					var adt = new AltType(attrs, baseName, baseTypes, null);
					adt.ScanClassBody(body);
					var output = new RVList<LNode>();
					adt.GenerateOutput(ref output);
					return LNode.Call(CodeSymbols.Splice, new RVList<LNode>(output));
				}
			}
			return null;
		}
		class AltType
		{
			private RVList<LNode> _typeAttrs;
			public LNode TypeName;
			public RVList<LNode> BaseTypes;
			public AltType ParentType;
			public AltType(RVList<LNode> typeAttrs, LNode typeName, RVList<LNode> baseTypes, AltType parentType)
			{
				_typeAttrs = typeAttrs;
				TypeName = typeName;
				BaseTypes = baseTypes;
				ParentType = parentType;
				if (ParentType != null)
					BaseTypes.Add(ParentType.TypeName);
				{
					LNode stem;
					RVList<LNode> a = default(RVList<LNode>);
					if (TypeName.CallsMin(CodeSymbols.Of, 1) && (stem = TypeName.Args[0]) != null && (a = new RVList<LNode>(TypeName.Args.Slice(1))).IsEmpty | true || (stem = TypeName) != null) {
						_typeNameStem = stem;
						_genericArgs = a;
					}
				}
			}
			LNode _typeNameStem;
			RVList<LNode> _genericArgs = new RVList<LNode>();
			List<AltType> _children = new List<AltType>();
			internal List<AdtParam> Parts = new List<AdtParam>();
			RVList<LNode> _constructorAttrs;
			RVList<LNode> _extraConstrLogic;
			RVList<LNode> _classBody = new RVList<LNode>();
			public void AddParts(RVList<LNode> parts)
			{
				foreach (var part in parts)
					Parts.Add(new AdtParam(part, this));
			}
			public void ScanClassBody(RVList<LNode> body)
			{
				foreach (var stmt in body) {
					int i;
					{
						LNode altName;
						RVList<LNode> attrs, childBody = default(RVList<LNode>), parts, rest;
						if ((attrs = stmt.Attrs).IsEmpty | true && stmt.Calls(CodeSymbols.Fn, 3) && stmt.Args[0].IsIdNamed((Symbol) "alt") && (altName = stmt.Args[1]) != null && stmt.Args[2].Calls(CodeSymbols.AltList) && (parts = stmt.Args[2].Args).IsEmpty | true || (attrs = stmt.Attrs).IsEmpty | true && stmt.Calls(CodeSymbols.Fn, 4) && stmt.Args[0].IsIdNamed((Symbol) "alt") && (altName = stmt.Args[1]) != null && stmt.Args[2].Calls(CodeSymbols.AltList) && (parts = stmt.Args[2].Args).IsEmpty | true && stmt.Args[3].Calls(CodeSymbols.Braces) && (childBody = stmt.Args[3].Args).IsEmpty | true) {
							LNode genericAltName = altName;
							if (altName.CallsMin(CodeSymbols.Of, 1)) {
							} else if (_genericArgs.Count > 0)
								genericAltName = LNode.Call(CodeSymbols.Of, new RVList<LNode>().Add(altName).AddRange(_genericArgs));
							var child = new AltType(attrs, genericAltName, LNode.List(), this);
							child.AddParts(parts);
							child.ScanClassBody(childBody);
							_children.Add(child);
						} else if ((attrs = stmt.Attrs).IsEmpty | true && (i = attrs.IndexWhere(a => a.IsIdNamed(__alt))) > -1 && stmt.CallsMin(CodeSymbols.Cons, 3) && stmt.Args[1].IsIdNamed((Symbol) "#this") && stmt.Args[2].Calls(CodeSymbols.AltList) && (rest = new RVList<LNode>(stmt.Args.Slice(3))).IsEmpty | true && rest.Count <= 1) {
							parts = stmt.Args[2].Args;
							attrs.RemoveAt(i);
							_constructorAttrs.AddRange(attrs);
							if (rest.Count > 0 && rest[0].Calls(S.Braces))
								_extraConstrLogic.AddRange(rest[0].Args);
							AddParts(parts);
						} else
							_classBody.Add(stmt);
					}
				}
			}
			public void GenerateOutput(ref RVList<LNode> list)
			{
				bool isAbstract = _typeAttrs.Any(a => a.IsIdNamed(S.Abstract));
				var baseParts = new List<AdtParam>();
				for (var type = ParentType; type != null; type = type.ParentType)
					baseParts.InsertRange(0, type.Parts);
				var allParts = baseParts.Concat(Parts);
				var initialization = Parts.Select(p => LNode.Call(CodeSymbols.Assign, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id(CodeSymbols.This), p.NameId)), p.NameId)).SetStyle(NodeStyle.Operator)).ToList();
				if (baseParts.Count > 0)
					initialization.Insert(0, F.Call(S.Base, baseParts.Select(p => p.NameId)));
				var args = new RVList<LNode>(allParts.Select(p => p.OriginalDecl));
				if (!_constructorAttrs.Any(a => a.IsIdNamed(S.Public)))
					_constructorAttrs.Add(F.Id(S.Public));
				LNode constructor = LNode.Call(new RVList<LNode>(_constructorAttrs), CodeSymbols.Cons, LNode.List(LNode.Missing, _typeNameStem, LNode.Call(CodeSymbols.AltList, new RVList<LNode>(args)), LNode.Call(CodeSymbols.Braces, new RVList<LNode>().AddRange(initialization).AddRange(_extraConstrLogic)).SetStyle(NodeStyle.Statement)));
				var outBody = new RVList<LNode>();
				outBody.Add(constructor);
				outBody.AddRange(Parts.Select(p => p.GetFieldDecl()));
				outBody.AddRange(baseParts.Select(p => GetWithFn(p, isAbstract, S.Override, allParts)));
				outBody.AddRange(Parts.Select(p => GetWithFn(p, isAbstract, _children.Count > 0 ? S.Virtual : null, allParts)));
				outBody.AddRange(Parts.WithIndexes().Where(kvp => kvp.Value.NameId.Name.Name != "Item" + (baseParts.Count + kvp.Key + 1)).Select(kvp => kvp.Value.GetItemDecl(baseParts.Count + kvp.Key + 1)));
				outBody.AddRange(_classBody);
				list.Add(LNode.Call(new RVList<LNode>(_typeAttrs), CodeSymbols.Class, LNode.List(TypeName, LNode.Call(CodeSymbols.AltList, new RVList<LNode>(BaseTypes)), LNode.Call(CodeSymbols.Braces, new RVList<LNode>(outBody)).SetStyle(NodeStyle.Statement))));
				if (_genericArgs.Count > 0 && Parts.Count > 0) {
					var argNames = allParts.Select(p => p.NameId);
					list.Add(LNode.Call(new RVList<LNode>().AddRange(_typeAttrs).Add(LNode.Id(CodeSymbols.Static)).Add(LNode.Id(LNode.List(LNode.Id(CodeSymbols.TriviaWordAttribute)), CodeSymbols.Partial)), CodeSymbols.Class, LNode.List(_typeNameStem, LNode.Call(CodeSymbols.AltList), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(LNode.List(LNode.Id(CodeSymbols.Public), LNode.Id(CodeSymbols.Static)), CodeSymbols.Fn, LNode.List(TypeName, LNode.Call(CodeSymbols.Of, new RVList<LNode>().Add(LNode.Id((Symbol) "New")).AddRange(_genericArgs)), LNode.Call(CodeSymbols.AltList, new RVList<LNode>(args)), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(TypeName, new RVList<LNode>(argNames)))))))).SetStyle(NodeStyle.Statement))))).SetStyle(NodeStyle.Statement))));
				}
				foreach (var child in _children)
					child.GenerateOutput(ref list);
			}
			public LNode GetWithFn(AdtParam part, bool isAbstract, Symbol virtualOverride, IEnumerable<AdtParam> allParts)
			{
				LNode genericClassName = this.TypeName;
				int totalParts = allParts.Count();
				var withField = F.Id("With" + part.NameId.Name);
				var args = LNode.List();
				foreach (AdtParam otherPart in allParts) {
					if (part == otherPart)
						args.Add(F.Id("newValue"));
					else
						args.Add(otherPart.NameId);
				}
				var attrs = new RVList<LNode>(F.Id(S.Public));
				if (isAbstract)
					attrs.Add(F.Id(S.Abstract));
				if (virtualOverride != null && (!isAbstract || virtualOverride == S.Override))
					attrs.Add(F.Id(virtualOverride));
				LNode method;
				LNode type = part.Type;
				LNode retType = part.ContainingType.TypeName;
				if (isAbstract) {
					method = LNode.Call(new RVList<LNode>(attrs), CodeSymbols.Fn, LNode.List(retType, withField, LNode.Call(CodeSymbols.AltList, LNode.List(LNode.Call(new RVList<LNode>(part.OriginalDecl.Attrs), CodeSymbols.Var, LNode.List(type, LNode.Id((Symbol) "newValue")))))));
				} else {
					method = LNode.Call(new RVList<LNode>(attrs), CodeSymbols.Fn, LNode.List(retType, withField, LNode.Call(CodeSymbols.AltList, LNode.List(LNode.Call(new RVList<LNode>(part.OriginalDecl.Attrs), CodeSymbols.Var, LNode.List(type, LNode.Id((Symbol) "newValue"))))), LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(LNode.Call(CodeSymbols.New, LNode.List(LNode.Call(genericClassName, new RVList<LNode>(args)))))))).SetStyle(NodeStyle.Statement)));
				}
				return method;
			}
		}
		class AdtParam
		{
			public LNode OriginalDecl;
			public AltType ContainingType;
			public AdtParam(LNode originalDecl, AltType containingType)
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
			public readonly LNode Type;
			public readonly LNode NameId;
			public LNode GetFieldDecl()
			{
				return LNode.Call(LNode.List(LNode.Id(CodeSymbols.Public)), CodeSymbols.Property, LNode.List(Type, NameId, LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Id(CodeSymbols.get), LNode.Id(LNode.List(LNode.Id(CodeSymbols.Private)), CodeSymbols.set))).SetStyle(NodeStyle.Statement)));
			}
			public LNode GetItemDecl(int itemNum)
			{
				LNode ItemN = F.Id("Item" + itemNum);
				return LNode.Call(LNode.List(LNode.Call(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "System"), LNode.Id((Symbol) "ComponentModel"))), LNode.Id((Symbol) "EditorBrowsable"))), LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Call(CodeSymbols.Dot, LNode.List(LNode.Id((Symbol) "System"), LNode.Id((Symbol) "ComponentModel"))), LNode.Id((Symbol) "EditorBrowsableState"))), LNode.Id((Symbol) "Never"))))), LNode.Id(CodeSymbols.Public)), CodeSymbols.Property, LNode.List(Type, ItemN, LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.get, LNode.List(LNode.Call(CodeSymbols.Braces, LNode.List(LNode.Call(CodeSymbols.Return, LNode.List(NameId)))).SetStyle(NodeStyle.Statement))).SetStyle(NodeStyle.Special))).SetStyle(NodeStyle.Statement)));
			}
		}
	}
}
