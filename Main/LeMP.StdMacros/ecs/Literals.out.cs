// Generated from Literals.ecs by LeMP custom tool. LeMP version: 30.1.0.0
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
using S = Loyc.Ecs.EcsCodeSymbols;

namespace LeMP.ecs
{
	partial class StandardMacros
	{
		[LexicalMacro("(array literal T[])", 
		"Converts an array literal to a C# `new T[] {...}` expression", 
		Mode = MacroMode.MatchEveryLiteral | MacroMode.PriorityInternalFallback)] 
		public static LNode ArrayLiteral(LNode node, IMacroContext context)
		{
			var value = node.Value;
			if (value is Array array) {
				Type elementType = value.GetType().GetElementType();
				string elementTypeName = elementType.NameWithGenericArgs();
				LNode elementTypeN = LNode.Call(S.CsRawText, LNode.List(LNode.Literal(elementTypeName)));

				Func<object, LNode, LNode> newLiteral = (el, pnode) => LNode.Literal(el, pnode);
				// Reduce output text size by preventing the printer from using casts 
				// e.g. print `23` instead of `(byte) 23` or `(short) 23`. Also, unbox
				// ints to save memory (ideally we'd do this for all Value Types)
				if (elementType == typeof(byte))
					newLiteral = (el, pnode) => LNode.Literal((int) (byte) el, pnode);
				if (elementType == typeof(sbyte))
					newLiteral = (el, pnode) => LNode.Literal((int) (sbyte) el, pnode);
				if (elementType == typeof(short))
					newLiteral = (el, pnode) => LNode.Literal((int) (short) el, pnode);
				if (elementType == typeof(ushort))
					newLiteral = (el, pnode) => LNode.Literal((int) (ushort) el, pnode);
				if (elementType == typeof(int))
					newLiteral = (el, pnode) => LNode.Literal((int) (int) el, pnode);

				if (array.Rank == 1) {
					var initializers = new List<LNode>();
					int count = 0;
					foreach (object element in array) {
						LNode elemNode = newLiteral(element, node);
						if ((count++ & 7) == 0 && array.Length > 8)
							elemNode = elemNode.PlusAttr(LNode.Id(S.TriviaNewline));
						initializers.Add(elemNode);
					}
					return LNode.Call(CodeSymbols.New, LNode.List().Add(LNode.Call(LNode.Call(CodeSymbols.Of, LNode.List(LNode.Id(CodeSymbols.Array), elementTypeN)))).AddRange(initializers));
				} else {
					return null;	// TODO
					//Stmt("int[,] Foo = new[,] { {\n 0 }, {\n 1,\n 2, }, };", F.Call(S.Var, F.Of(S.TwoDimensionalArray, S.Int32), 
					//	F.Call(S.Assign, Foo, F.Call(S.New, F.Call(S.TwoDimensionalArray), F.Braces(zero), F.Braces(one, two)))));
				}
			}
			return null;
		}
	}
}