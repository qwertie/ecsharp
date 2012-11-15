using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Loyc.Essentials;
using System.Diagnostics;
using Loyc.Utilities;
using ecs;

namespace Loyc.CompilerCore
{
	using S = CodeSymbols;
	public partial class CodeSymbols
	{
		// Plain C# operators (node names)
		public static readonly Symbol _Mul = GSymbol.Get("#*"); // or dereference
		public static readonly Symbol _Div = GSymbol.Get("#/");
		public static readonly Symbol _Add = GSymbol.Get("#+"); // or unary +
		public static readonly Symbol _Sub = GSymbol.Get("#-"); // or unary -
		public static readonly Symbol _PreInc = GSymbol.Get("#++");
		public static readonly Symbol _PreDec = GSymbol.Get("#--");
		public static readonly Symbol _PostInc = GSymbol.Get("#postIncrement");
		public static readonly Symbol _PostDec = GSymbol.Get("#postDecrement");
		public static readonly Symbol _Mod = GSymbol.Get("#%");
		public static readonly Symbol _And = GSymbol.Get("#&&");
		public static readonly Symbol _Or = GSymbol.Get("#||");
		public static readonly Symbol _Xor = GSymbol.Get("#^^");
		public static readonly Symbol _Eq = GSymbol.Get("#==");
		public static readonly Symbol _Neq = GSymbol.Get("#!=");
		public static readonly Symbol _GT = GSymbol.Get("#>");
		public static readonly Symbol _GE = GSymbol.Get("#>=");
		public static readonly Symbol _LT = GSymbol.Get("#<");
		public static readonly Symbol _LE = GSymbol.Get("#<=");
		public static readonly Symbol _Shr = GSymbol.Get("#>>");
		public static readonly Symbol _Shl = GSymbol.Get("#<<");
		public static readonly Symbol _Not = GSymbol.Get("#!");
		public static readonly Symbol _Set = GSymbol.Get("#=");
		public static readonly Symbol _OrBits = GSymbol.Get("#|");
		public static readonly Symbol _AndBits = GSymbol.Get("#&"); // also, address-of
		public static readonly Symbol _NotBits = GSymbol.Get("#~"); 
		public static readonly Symbol _XorBits = GSymbol.Get("#^");
		public static readonly Symbol _List = GSymbol.Get("#");     // Produces the last value, e.g. #(1, 2, 3) == 3.
		public static readonly Symbol _Braces = GSymbol.Get("#{}"); // Creates a scope.
		public static readonly Symbol _Bracks = GSymbol.Get("#[]"); // indexing operator (use _Attr for attributes)
		public static readonly Symbol _TypeArgs = GSymbol.Get("#<>");
		public static readonly Symbol _Dot = GSymbol.Get("#.");
		public static readonly Symbol _If = GSymbol.Get("#if");             // e.g. #if(x,y,z); doubles as x?y:z operator
		public static readonly Symbol _NamedArg = GSymbol.Get("#namedArg"); // Named argument e.g. #namedarg(x, 0) <=> x: 0
		public static readonly Symbol _Label = GSymbol.Get("#label");       // e.g. #label(success) <=> success:
		public static readonly Symbol _Goto = GSymbol.Get("#goto");         // e.g. #goto(success) <=> goto success;
		public static readonly Symbol _Case = GSymbol.Get("#case");         // e.g. #case(10, 20) <=> case 10, 20:
		
		// Enhanced C# stuff (node names)
		public static readonly Symbol _Exp = GSymbol.Get("#**");
		public static readonly Symbol _In = GSymbol.Get("#in");
		public static readonly Symbol _Substitute = GSymbol.Get("#\\");
		public static readonly Symbol _DotDot = GSymbol.Get("#..");
		public static readonly Symbol _VerbatimCode = GSymbol.Get("#@");
		public static readonly Symbol _DoubleVerbatimCode = GSymbol.Get("#@@");
		public static readonly Symbol _End = GSymbol.Get("#$");
		public static readonly Symbol _Tuple = GSymbol.Get("#tuple");
		public static readonly Symbol _Literal = GSymbol.Get("#literal");
		public static readonly Symbol _Var = GSymbol.Get("#var"); // e.g. #var(int, x(0), y(1), z)
		public static readonly Symbol _Bind = GSymbol.Get("#::"); // EC# quick binding operator. Slightly different scoping rules than #var.
		public static readonly Symbol _Def = GSymbol.Get("#def"); // e.g. #def(F, #([required] #var(#<>(List, int), list)), void, #(return)))

		// Tags
		public static readonly Symbol _Attrs = GSymbol.Get("#attrs"); // This is the default tag type e.g. [Serializable, #public] #var(int, Val)

		// Other
		public static readonly Symbol _CallKind = GSymbol.Get("#callKind"); // result of node.Kind on a call
		public static readonly Symbol _Missing = GSymbol.Get("#missing"); // A component of a list was omitted, e.g. Foo(, y) => Foo(#missing, y)
		
		// Accessibility flags: these work slightly differently than the standard C# flags.
		// #public, #public_ex, #protected and #protected_ex can all be used at once,
		// #protected_ex and #public both imply #protected, and #public_ex implies
		// the other three. Access within the same space and nested spaces is always
		// allowed.
		/// <summary>Provides general access within a library or program (implies
		/// #protected_in).</summary>
		public static readonly Symbol _Internal = GSymbol.Get("#internal");
		/// <summary>Provides general access, even outside the assembly (i.e. 
		/// dynamic-link library). Implies #internal, #protectedIn and #protected.</summary>
		public static readonly Symbol _Public = GSymbol.Get("#public");
		/// <summary>Provides access to derived classes only within the same library
		/// or program (i.e. assembly). There is no C# equivalent to this keyword,
		/// which does not provide access outside the assembly.</summary>
		public static readonly Symbol _ProtectedIn = GSymbol.Get("#protectedIn");
		/// <summary>Provides access to all derived classes. Implies #protected_in.
		/// #protected #internal corresponds to C# "protected internal"</summary>
		public static readonly Symbol _Protected = GSymbol.Get("#protected"); 
		/// <summary>Revokes access outside the same space and nested spaces. This 
		/// can be used in spaces in which the default is not private to request
		/// private as a starting point. Therefore, other flags (e.g. #protected_ex)
		/// can be added to this flag to indicate what access the user wants to
		/// provide instead.
		/// </summary><remarks>
		/// The name #private may be slightly confusing, since a symbol marked 
		/// #private is not actually private when there are other access markers
		/// included at the same time. I considered calling it #revoke instead,
		/// since its purpose is to revoke the default access modifiers of the
		/// space, but I was concerned that someone might want to reserve #revoke 
		/// for some other purpose.
		/// </remarks>
		public static readonly Symbol _Private = GSymbol.Get("#private");

		// C#/.NET standard data types
		public static readonly Symbol _String = GSymbol.Get("#string");
		public static readonly Symbol _Char   = GSymbol.Get("#char");
		public static readonly Symbol _Bool   = GSymbol.Get("#bool");
		public static readonly Symbol _Int8   = GSymbol.Get("#sbyte");
		public static readonly Symbol _Int16  = GSymbol.Get("#short");
		public static readonly Symbol _Int32  = GSymbol.Get("#int");
		public static readonly Symbol _Int64  = GSymbol.Get("#long");
		public static readonly Symbol _UInt8  = GSymbol.Get("#byte");
		public static readonly Symbol _UInt16 = GSymbol.Get("#ushort");
		public static readonly Symbol _UInt32 = GSymbol.Get("#uint");
		public static readonly Symbol _UInt64 = GSymbol.Get("#ulong");
		public static readonly Symbol _Single = GSymbol.Get("#float");
		public static readonly Symbol _Double = GSymbol.Get("#double");
		public static readonly Symbol _Decimal = GSymbol.Get("#decimal");
	}

	/// <summary>Contains static helper methods for creating <see cref="GreenNode"/>s.</summary>
	public partial class GreenFactory
	{
		// Common literals
		public static readonly GreenNode @true = Literal(true);
		public static readonly GreenNode @false = Literal(false);
		public static readonly GreenNode @null = Literal((object)null);
		public static readonly GreenNode @void = Literal(ecs.@void.Value);
		public static readonly GreenNode int_0 = Literal(0);
		public static readonly GreenNode int_1 = Literal(1);
		public static readonly GreenNode string_empty = Literal("");

		public static readonly GreenNode Missing = new GreenSymbol(S._Missing, 0);
		public static readonly GreenNode DefKeyword = new GreenSymbol(S._Def, -1);
		
		// Standard data types (marked synthetic)
		public static readonly GreenNode String = new GreenSymbol(S._String, -1);
		public static readonly GreenNode Char = new GreenSymbol(S._Char, -1);
		public static readonly GreenNode Bool = new GreenSymbol(S._Bool, -1);
		public static readonly GreenNode Int8 = new GreenSymbol(S._Int8, -1);
		public static readonly GreenNode Int16 = new GreenSymbol(S._Int16, -1);
		public static readonly GreenNode Int32 = new GreenSymbol(S._Int32, -1);
		public static readonly GreenNode Int64 = new GreenSymbol(S._Int64, -1);
		public static readonly GreenNode UInt8 = new GreenSymbol(S._UInt8, -1);
		public static readonly GreenNode UInt16 = new GreenSymbol(S._UInt16, -1);
		public static readonly GreenNode UInt32 = new GreenSymbol(S._UInt32, -1);
		public static readonly GreenNode UInt64 = new GreenSymbol(S._UInt64, -1);
		public static readonly GreenNode Single = new GreenSymbol(S._Single, -1);
		public static readonly GreenNode Double = new GreenSymbol(S._Double, -1);
		public static readonly GreenNode Decimal = new GreenSymbol(S._Decimal, -1);

		// Standard access modifiers
		public static readonly GreenNode Internal = new GreenSymbol(S._Internal, -1);
		public static readonly GreenNode Public = new GreenSymbol(S._Public, -1);
		public static readonly GreenNode ProtectedIn = new GreenSymbol(S._ProtectedIn, -1);
		public static readonly GreenNode Protected = new GreenSymbol(S._Protected, -1);
		public static readonly GreenNode Private = new GreenSymbol(S._Private, -1);

		// The fundamentals: symbols, literals, and calls
		public static GreenNode Symbol(string name, int sourceWidth = -1)
		{
			return new GreenSymbol(GSymbol.Get(name), sourceWidth);
		}
		public static GreenNode Symbol(Symbol name, int sourceWidth = -1)
		{
			return new GreenSymbol(name, sourceWidth);
		}
		public static GreenNode Literal(object value, int sourceWidth = -1)
		{
			return new GreenLiteral(value, sourceWidth);
		}
		public static GreenNode Call(GreenNode head)
		{
			return new GreenCall0(new GreenAndOffset(head), -1);
		}
		public static GreenNode Call(GreenNode head, GreenNode _1)
		{
			return new GreenCall1(new GreenAndOffset(head), -1, new GreenAndOffset(_1));
		}
		public static GreenNode Call(GreenNode head, GreenNode _1, GreenNode _2, int sourceWidth = -1)
		{
			return new GreenCall2(new GreenAndOffset(head), -1, new GreenAndOffset(_1), new GreenAndOffset(_2));
		}
		public static GreenNode Call(GreenNode head, params GreenNode[] list)
		{
			var n = new EditableGreenNode(new GreenAndOffset(head), -1);
			for (int i = 0; i < list.Length; i++)
				n.Args.Add(new GreenAndOffset(list[i]));
			return n;
		}
		public static GreenNode Call(Symbol name)
		{
			return new GreenSimpleCall0(name, -1);
		}
		public static GreenNode Call(Symbol name, GreenNode _1)
		{
			return new GreenSimpleCall1(name, -1, new GreenAndOffset(_1));
		}
		public static GreenNode Call(Symbol name, GreenNode _1, GreenNode _2, int sourceWidth = -1)
		{
			return new GreenSimpleCall2(name, -1, new GreenAndOffset(_1), new GreenAndOffset(_2));
		}
		public static GreenNode Call(Symbol name, params GreenNode[] list)
		{
			var n = new EditableGreenNode(name, -1);
			for (int i = 0; i < list.Length; i++)
				n.Args.Add(new GreenAndOffset(list[i]));
			return n;
		}

		public static GreenNode Name(Symbol name)
		{
			return Symbol(name);
		}
		public static GreenNode Braces(params GreenNode[] contents)
		{
			return Call(S._Braces, contents);
		}
		public static GreenNode List(params GreenNode[] contents)
		{
			return Call(S._List, contents);
		}
		public static GreenNode Tuple(params GreenNode[] contents)
		{
			return Call(S._Tuple, contents);
		}
		public static GreenNode Def(Symbol name, GreenNode argList, GreenNode retType, GreenNode body = null)
		{
			return Def(Name(name), argList, retType, body);
		}
		public static GreenNode Def(GreenNode name, GreenNode argList, GreenNode retType, GreenNode body = null)
		{
			G.Require(argList.Name == S._List || argList.Name == S._Missing);
			GreenNode def;
			if (body == null) def = Call(S._Def, name, argList, retType);
			else              def = Call(S._Def, name, argList, retType, body);
			return def;
		}
		public static GreenNode ArgList(params GreenNode[] vars)
		{
			foreach (var var in vars)
				G.RequireArg(var.Name == S._Var && var.ArgCount >= 2, "vars", var);
			return Call(S._List, vars);
		}
		public static GreenNode Var(GreenNode type, Symbol name, GreenNode initValue = null)
		{
			if (initValue != null)
				return Call(S._Var, type, Call(name, initValue));
			return Call(S._Var, type, Symbol(name));
		}
		public static GreenNode Var(GreenNode type, params Symbol[] names)
		{
			var list = new List<GreenNode>(names.Length+1) { type };
			list.AddRange(names.Select(n => Symbol(n)));
			return Call(S._Var, list.ToArray());
		}
		public static GreenNode Var(GreenNode type, params GreenNode[] namesWithValues)
		{
			var list = new List<GreenNode>(namesWithValues.Length+1) { type };
			list.AddRange(namesWithValues);
			return Call(S._Var, list.ToArray());
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
