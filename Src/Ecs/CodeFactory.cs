using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;

namespace ecs
{
	public partial class CodeFactory
	{
		// Plain C# operators
		public static Symbol _Mul = GSymbol.Get("#*"); // or dereference
		public static Symbol _Div = GSymbol.Get("#/");
		public static Symbol _Add = GSymbol.Get("#+"); // or unary +
		public static Symbol _Sub = GSymbol.Get("#-"); // or unary -
		public static Symbol _PreInc = GSymbol.Get("#++");
		public static Symbol _PreDec = GSymbol.Get("#--");
		public static Symbol _PostInc = GSymbol.Get("#postincrement");
		public static Symbol _PostDec = GSymbol.Get("#postdecrement");
		public static Symbol _Mod = GSymbol.Get("#%");
		public static Symbol _And = GSymbol.Get("#&&");
		public static Symbol _Or = GSymbol.Get("#||");
		public static Symbol _Xor = GSymbol.Get("#^^");
		public static Symbol _Eq = GSymbol.Get("#==");
		public static Symbol _Neq = GSymbol.Get("#!=");
		public static Symbol _GT = GSymbol.Get("#>");
		public static Symbol _GE = GSymbol.Get("#>=");
		public static Symbol _LT = GSymbol.Get("#<");
		public static Symbol _LE = GSymbol.Get("#<=");
		public static Symbol _Shr = GSymbol.Get("#>>");
		public static Symbol _Shl = GSymbol.Get("#<<");
		public static Symbol _Not = GSymbol.Get("#!");
		public static Symbol _Set = GSymbol.Get("#=");
		public static Symbol _OrBits = GSymbol.Get("#|");
		public static Symbol _AndBits = GSymbol.Get("#&"); // also, address-of
		public static Symbol _NotBits = GSymbol.Get("#~"); 
		public static Symbol _XorBits = GSymbol.Get("#^");
		public static Symbol _List = GSymbol.Get("#");     // e.g. result of #(1, 2) is 2; represents parenthesis if 1 arg
		public static Symbol _Braces = GSymbol.Get("#{}"); // Like #last, but requires braces
		public static Symbol _Bracks = GSymbol.Get("#[]"); // indexing operator (use _Attr for attributes)
		public static Symbol _TypeArgs = GSymbol.Get("#<>");
		public static Symbol _Dot = GSymbol.Get("#.");
		public static Symbol _If = GSymbol.Get("#if");             // e.g. #if(x,y,z); doubles as x?y:z operator
		public static Symbol _Attr = GSymbol.Get("#attr");         // e.g. #attr(Serializable, #var(int, Val))
		public static Symbol _NamedArg = GSymbol.Get("#namedarg"); // e.g. #namedarg(x, 0) <=> x: 0
		public static Symbol _Label = GSymbol.Get("#label");       // e.g. #label(success) <=> success:
		public static Symbol _Goto = GSymbol.Get("#goto");         // e.g. #goto(success) <=> goto success;
		public static Symbol _Case = GSymbol.Get("#case");         // e.g. #case(10, 20) <=> case 10, 20:
			
		// Enhanced C# stuff
		public static Symbol _Exp = GSymbol.Get("#**");
		public static Symbol _In = GSymbol.Get("#in");
		public static Symbol _Substitute = GSymbol.Get("#\\");
		public static Symbol _DotDot = GSymbol.Get("#..");
		public static Symbol _VerbatimCode = GSymbol.Get("#@");
		public static Symbol _DoubleVerbatimCode = GSymbol.Get("#@@");
		public static Symbol _End = GSymbol.Get("#$");
		public static Symbol _Tuple = GSymbol.Get("#tuple");
		public static Symbol _Literal = GSymbol.Get("#literal");
		public static Symbol _Var = GSymbol.Get("#var");
		public static Symbol _Bind = GSymbol.Get("#::");
		public static Symbol _Def = GSymbol.Get("#def"); // e.g. #def(F, #list(#var(#<>(List, int), list, #[]: required)), void, #(return)))

		// Other
		public static Symbol _ListKind = GSymbol.Get("#listkind"); // result of node.Kind on a list
		public static Symbol _EmptyList = GSymbol.Get("#emptylist");

		// Common literals
		public static readonly node @true = node.NewLiteral(true);
		public static readonly node @false = node.NewLiteral(false);
		public static readonly node @null = node.NewLiteral((object)null);
		public static readonly node @void = node.NewLiteral(ecs.@void.Value);
		public static readonly node int_0 = node.NewLiteral(0);
		public static readonly node int_1 = node.NewLiteral(1);
		public static readonly node empty_string = node.NewLiteral("");

		public static node Name(Symbol name)
		{
			return node.New(name);
		}
		public static node Braces(params node[] contents)
		{
			return node.New(_Braces, contents);
		}
		public static node List(params node[] contents)
		{
			return node.New(_List, contents);
		}
		public static node Tuple(params node[] contents)
		{
			return node.New(_Tuple, contents);
		}
		public static node Def(Symbol name, node argList, node retVal, node body = null)
		{
			return Def(Name(name), argList, retVal, body);
		}
		public static node Def(node name, node argList, node retVal, node body = null)
		{
			node def;
			if (body == null) def = node.New(_Def, name, argList, retVal, body);
			else              def = node.New(_Def, name, argList, retVal);
			return def;
		}
		public static node ArgList(params node[] vars)
		{
			foreach (var var in vars)
				G.RequireArg(var.SkipAttrs.Name == _Var && var.Count >= 3, "vars", var);
			return node.New(_List, vars);
		}
		public static node Var(node type, Symbol name, node initValue = null)
		{
			if (initValue != null)
				return node.New(_Var, type, node.New(name, initValue));
			return node.New(_Var, type, node.New(name));
		}
		public static node Var(node type, params Symbol[] names)
		{
			var list = new List<node>(names.Length+1) { type };
			list.AddRange(names.Select(n => node.New(n)));
			return node.New(_Var, list.ToArray());
		}
		public static node Var(node type, params node[] namesWithValues)
		{
			var list = new List<node>(namesWithValues.Length+1) { type };
			list.AddRange(namesWithValues);
			return node.New(_Var, list.ToArray());
		}
		public static node Attr(node existing, node attr)
		{
			if (existing.Name == _Attr)
				return existing.AddArg(attr);
			else
				return node.New(_Attr, existing, attr);
		}
	}
}
