using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;

namespace ecs
{
	using SourceLocation = String;

	public partial class CodeFactory
	{
		// Plain C# operators (node names)
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
		// _List (#): a list of things, e.g. method argument definitions. Auto-
		// explodes when returned from a macro. Used with one argument to 
		// represent redundant parenthesis. The result of the multiple-argument 
		// form is always void.
		public static Symbol _List = GSymbol.Get("#");     // Produces the last value, e.g. #(1, 2, 3) == 3.
		public static Symbol _Braces = GSymbol.Get("#{}"); // Creates a scope.
		public static Symbol _Bracks = GSymbol.Get("#[]"); // indexing operator (use _Attr for attributes)
		public static Symbol _TypeArgs = GSymbol.Get("#<>");
		public static Symbol _Dot = GSymbol.Get("#.");
		public static Symbol _If = GSymbol.Get("#if");             // e.g. #if(x,y,z); doubles as x?y:z operator
		public static Symbol _NamedArg = GSymbol.Get("#namedarg"); // Named argument e.g. #namedarg(x, 0) <=> x: 0
		public static Symbol _Label = GSymbol.Get("#label");       // e.g. #label(success) <=> success:
		public static Symbol _Goto = GSymbol.Get("#goto");         // e.g. #goto(success) <=> goto success;
		public static Symbol _Case = GSymbol.Get("#case");         // e.g. #case(10, 20) <=> case 10, 20:
		
		// Enhanced C# stuff (node names)
		public static Symbol _Exp = GSymbol.Get("#**");
		public static Symbol _In = GSymbol.Get("#in");
		public static Symbol _Substitute = GSymbol.Get("#\\");
		public static Symbol _DotDot = GSymbol.Get("#..");
		public static Symbol _VerbatimCode = GSymbol.Get("#@");
		public static Symbol _DoubleVerbatimCode = GSymbol.Get("#@@");
		public static Symbol _End = GSymbol.Get("#$");
		public static Symbol _Tuple = GSymbol.Get("#tuple");
		public static Symbol _Literal = GSymbol.Get("#literal");
		public static Symbol _Var = GSymbol.Get("#var"); // e.g. #var(int, x(0), y(1), z)
		public static Symbol _Bind = GSymbol.Get("#::"); // EC# quick binding operator. Slightly different scoping rules than #var.
		public static Symbol _Def = GSymbol.Get("#def"); // e.g. #def(F, #([required] #var(#<>(List, int), list)), void, #(return)))

		// Tags
		public static Symbol _Attrs = GSymbol.Get("#attrs"); // This is the default tag type e.g. [Serializable, #public] #var(int, Val)

		// Other
		public static Symbol _ListKind = GSymbol.Get("#listkind"); // result of node.Kind on a list
		public static Symbol _EmptyList = GSymbol.Get("#emptylist");

		// Common literals
		public static readonly Node @true = Literal(true);
		public static readonly Node @false = Literal(false);
		public static readonly Node @null = Literal((object)null);
		public static readonly Node @void = Literal(ecs.@void.Value);
		public static readonly Node int_0 = Literal(0);
		public static readonly Node int_1 = Literal(1);
		public static readonly Node empty_string = Literal("");

		public static Node Symbol(string name, object basis = null)
		{
			return new SymbolNode(GSymbol.Get(name), basis);
		}
		public static Node Symbol(Symbol name, object basis = null)
		{
			return new SymbolNode(name, basis);
		}
		public static Node Call(Node head, Node _1, object basis = null)
		{
			return new ListNode(head, new Node[] { _1 }, basis);
		}
		public static Node Call(Node head, Node _1, Node _2, object basis = null)
		{
			return new ListNode(head, new Node[] { _1, _2 }, basis);
		}
		public static Node Call(Node head, params Node[] list)
		{
			return new ListNode(head, list, null);
		}
		public static Node Call(Node head, Node[] list, object basis)
		{
			return new ListNode(head, list, basis);
		}
		public static Node Call(Symbol name, Node _1, object basis = null)
		{
			return new ListNode(Symbol(name, basis), new Node[] { _1 }, basis);
		}
		public static Node Call(Symbol name, Node _1, Node _2, object basis = null)
		{
			return new ListNode(Symbol(name, basis), new Node[] { _1, _2 }, basis);
		}
		public static Node Call(Symbol name, Node _1, Node _2, Node _3, object basis = null)
		{
			return new ListNode(Symbol(name, basis), new Node[] { _1, _2, _3 }, basis);
		}
		public static Node Call(Symbol name, Node _1, Node _2, Node _3, Node _4, object basis = null)
		{
			return new ListNode(Symbol(name, basis), new Node[] { _1, _2, _3, _4 }, basis);
		}
		public static Node Call(Symbol name, Node[] nodes, object basis = null)
		{
			return new ListNode(Symbol(name, basis), nodes, basis);
		}
		public static Node Literal(object value, object basis = null)
		{
			return new LiteralNode(value, basis);
		}

		public static Node Name(Symbol name)
		{
			return Symbol(name);
		}
		public static Node Braces(params Node[] contents)
		{
			return Call(_Braces, contents);
		}
		public static Node List(params Node[] contents)
		{
			return Call(_List, contents);
		}
		public static Node Tuple(params Node[] contents)
		{
			return Call(_Tuple, contents);
		}
		public static Node Def(Symbol name, Node argList, Node retVal, Node body = null)
		{
			return Def(Name(name), argList, retVal, body);
		}
		public static Node Def(Node name, Node argList, Node retVal, Node body = null)
		{
			Node def;
			if (body == null) def = Call(_Def, name, argList, retVal, body);
			else              def = Call(_Def, name, argList, retVal);
			return def;
		}
		public static Node ArgList(params Node[] vars)
		{
			foreach (var var in vars)
				G.RequireArg(var.Name == _Var && var.ArgCount >= 2, "vars", var);
			return Call(_List, vars);
		}
		public static Node Var(Node type, Symbol name, Node initValue = null)
		{
			if (initValue != null)
				return Call(_Var, type, Call(name, initValue));
			return Call(_Var, type, Symbol(name));
		}
		public static Node Var(Node type, params Symbol[] names)
		{
			var list = new List<Node>(names.Length+1) { type };
			list.AddRange(names.Select(n => Symbol(n)));
			return Call(_Var, list.ToArray());
		}
		public static Node Var(Node type, params Node[] namesWithValues)
		{
			var list = new List<Node>(namesWithValues.Length+1) { type };
			list.AddRange(namesWithValues);
			return Call(_Var, list.ToArray());
		}
		//public static node Attr(node existing, node attr)
		//{
		//    if (existing.Name == _Attr)
		//        return existing.AddArg(attr);
		//    else
		//        return node.New(_Attr, existing, attr);
		//}
	}
}
