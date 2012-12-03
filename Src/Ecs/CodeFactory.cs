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
		public static readonly Symbol Mul = GSymbol.Get("#*"); // or dereference
		public static readonly Symbol Div = GSymbol.Get("#/");
		public static readonly Symbol Add = GSymbol.Get("#+");     // or unary +
		public static readonly Symbol Sub = GSymbol.Get("#-");     // or unary -
		public static readonly Symbol PreInc = GSymbol.Get("#++");
		public static readonly Symbol PreDec = GSymbol.Get("#--");
		public static readonly Symbol PostInc = GSymbol.Get("#postIncrement");
		public static readonly Symbol PostDec = GSymbol.Get("#postDecrement");
		public static readonly Symbol Mod = GSymbol.Get("#%");
		public static readonly Symbol And = GSymbol.Get("#&&");
		public static readonly Symbol Or = GSymbol.Get("#||");
		public static readonly Symbol Xor = GSymbol.Get("#^^");
		public static readonly Symbol Eq = GSymbol.Get("#==");
		public static readonly Symbol Neq = GSymbol.Get("#!=");
		public static readonly Symbol GT = GSymbol.Get("#>");
		public static readonly Symbol GE = GSymbol.Get("#>=");
		public static readonly Symbol LT = GSymbol.Get("#<");
		public static readonly Symbol LE = GSymbol.Get("#<=");
		public static readonly Symbol Shr = GSymbol.Get("#>>");
		public static readonly Symbol Shl = GSymbol.Get("#<<");
		public static readonly Symbol Not = GSymbol.Get("#!");
		public static readonly Symbol Set = GSymbol.Get("#=");
		public static readonly Symbol OrBits = GSymbol.Get("#|");
		public static readonly Symbol AndBits = GSymbol.Get("#&"); // also, address-of (unary &)
		public static readonly Symbol NotBits = GSymbol.Get("#~"); 
		public static readonly Symbol XorBits = GSymbol.Get("#^");
		public static readonly Symbol List = GSymbol.Get("#");     // Produces the last value, e.g. #(1, 2, 3) == 3.
		public static readonly Symbol Braces = GSymbol.Get("#{}"); // Creates a scope.
		public static readonly Symbol Bracks = GSymbol.Get("#[]"); // indexing operator and array type (use _Attr for attributes)
		                                                           // foo[1] <=> #[](foo, 1) and int[] <=> #of(#[], int)
		public static readonly Symbol TwoDimensionalArray = GSymbol.Get("#[,]"); // int[,] <=> #of(#`[,]`, int)
		public static readonly Symbol QuestionMark = GSymbol.Get("#?"); // (a?b:c) <=> #?(a,b,c) and int? <=> #of(#?, int)
		public static readonly Symbol Of = GSymbol.Get("#of");
		public static readonly Symbol Dot = GSymbol.Get("#.");
		public static readonly Symbol NamedArg = GSymbol.Get("#namedArg"); // Named argument e.g. #namedarg(x, 0) <=> x: 0
		public static readonly Symbol CodeQuote = GSymbol.Get("#quote");                         // Code quote @(...), @{...} or @[...]
		public static readonly Symbol CodeQuoteSubstituting = GSymbol.Get("#quoteSubstituting"); // Code quote @@(...), @@{...} or @@[...]
		public static readonly Symbol New = GSymbol.Get("#new");
		public static readonly Symbol Out = GSymbol.Get("#out");
		public static readonly Symbol Typeof = GSymbol.Get("#typeof");     // typeof(Foo) or typeof<foo>
		
		// Executable statements
		public static readonly Symbol If = GSymbol.Get("#if");             // e.g. #if(x,y,z); I wanted it to be the conditional operator too, but the semantics are a bit different
		public static readonly Symbol DoWhile = GSymbol.Get("#doWhile");   // e.g. #doWhile(x++, condition); <=> do x++; while(condition);
		public static readonly Symbol While = GSymbol.Get("#while");       // e.g. #while(condition,{...}); <=> while(condition) {...}
		public static readonly Symbol Using = GSymbol.Get("#using");       // e.g. #using(expr, {...}); <=> using(expr) {...}
		public static readonly Symbol For = GSymbol.Get("#for");           // e.g. #for(int i = 0, i < Count, i++, {...}); <=> for(int i = 0; i < Count; i++) {...}
		public static readonly Symbol ForEach = GSymbol.Get("#foreach");   // e.g. #foreach(#var(#missing, n), list); <=> foreach(var n in list)
		public static readonly Symbol Label = GSymbol.Get("#label");       // e.g. #label(success) <=> success:
		public static readonly Symbol Case = GSymbol.Get("#case");         // e.g. #case(10, 20) <=> case 10, 20:
		public static readonly Symbol Return = GSymbol.Get("#return");     // e.g. #return(x);  <=> return x;
		public static readonly Symbol Continue = GSymbol.Get("#continue"); // e.g. #continue;   <=> continue;
		public static readonly Symbol Break = GSymbol.Get("#break");       // e.g. #break;      <=> break;
		public static readonly Symbol Goto = GSymbol.Get("#goto");         // e.g. #goto(label) <=> goto label;
		public static readonly Symbol GotoCase = GSymbol.Get("#gotoCase"); // e.g. #gotoCase(expr) <=> goto case expr;
		public static readonly Symbol Throw = GSymbol.Get("#throw");       // e.g. #throw(expr);  <=> throw expr;
		public static readonly Symbol Checked = GSymbol.Get("#checked");   // e.g. #checked({ stmt; }); <=> checked { stmt; }
		public static readonly Symbol Unchecked = GSymbol.Get("#unchecked"); // e.g. #unchecked({ stmt; }); <=> unchecked { stmt; }
		public static readonly Symbol Fixed = GSymbol.Get("#fixed");       // e.g. #fixed(#var(#*(#int), x(&y)), stmt); <=> fixed(int* x = &y) stmt;
		public static readonly Symbol Lock = GSymbol.Get("#lock");         // e.g. #lock(obj, stmt); <=> lock(obj) stmt;
		public static readonly Symbol Switch = GSymbol.Get("#switch");     // e.g. #switch(n, { ... }); <=> switch(n) { ... }
		public static readonly Symbol Try = GSymbol.Get("#try");           // e.g. #try({...}, #catch({...})); <=> try {...} catch {...}
		
		// Space definitions
		public static readonly Symbol Class = GSymbol.Get("#def_class");   // e.g. #def_class(Foo, #(IFoo), { });  <=> class Foo : IFoo { }
		public static readonly Symbol Struct = GSymbol.Get("#def_struct"); // e.g. #def_struct(Foo, #(IFoo), { }); <=> struct Foo : IFoo { }
		public static readonly Symbol Trait = GSymbol.Get("#def_trait");   // e.g. #def_trait(Foo, #(IFoo), { });  <=> trait Foo : IFoo { }
		public static readonly Symbol Enum = GSymbol.Get("#def_enum");     // e.g. #def_enum(Foo, #(byte), { });  <=> enum Foo : byte { }
		public static readonly Symbol Alias = GSymbol.Get("#def_alias");   // e.g. #def_alias(Int = int, #(IMath), { });  <=> alias Int = int : IMath { }
		public static readonly Symbol Interface = GSymbol.Get("#def_interface"); // e.g. #def_interface(IB, #(IA), { });  <=> interface IB : IA { }
		public static readonly Symbol Namespace = GSymbol.Get("#def_namespace"); // e.g. #def_namespace(NS, #missing, { });  <=> namespace NS { }

		// Other definitions
		public static readonly Symbol Var = GSymbol.Get("#var");           // e.g. #var(#int, x(0), y(1), z). #var(#missing, x(0)) <=> var x = 0;
		public static readonly Symbol Event = GSymbol.Get("#def_event");   // e.g. #def_event(EventHandler, Click, { }) <=> event EventHandler Click { }
		public static readonly Symbol Delegate = GSymbol.Get("#def_delegate"); // e.g. #def_delegate(Foo, #(), #int); <=> delegate int Foo();
		public static readonly Symbol Property = GSymbol.Get("#def_prop"); // e.g. #def_prop(Foo, int, { #get; }) <=> int Foo { get; }

		// Enhanced C# stuff (node names)
		public static readonly Symbol Exp = GSymbol.Get("#**");
		public static readonly Symbol In = GSymbol.Get("#in");
		public static readonly Symbol Substitute = GSymbol.Get(@"#\");
		public static readonly Symbol DotDot = GSymbol.Get("#..");
		public static readonly Symbol VerbatimCode = GSymbol.Get("#@");
		public static readonly Symbol DoubleVerbatimCode = GSymbol.Get("#@@");
		public static readonly Symbol End = GSymbol.Get("#$");
		public static readonly Symbol Tuple = GSymbol.Get("#tuple");
		public static readonly Symbol Literal = GSymbol.Get("#literal");
		public static readonly Symbol QuickBind = GSymbol.Get("#:::"); // Quick variable-creation operator.
		public static readonly Symbol ColonColon = GSymbol.Get("#::"); // Scope resolution operator in many languages; serves as a temporary representation of #::: in EC#
		public static readonly Symbol Def = GSymbol.Get("#def"); // e.g. #def(void, F, #([required] #var(#<>(List, int), list)), #(return)))

		// Other
		public static readonly Symbol CallKind = GSymbol.Get("#callKind"); // result of node.Kind on a call
		public static readonly Symbol Missing = GSymbol.Get("#missing"); // A component of a list was omitted, e.g. Foo(, y) => Foo(#missing, y)
		
		// Accessibility flags: these work slightly differently than the standard C# flags.
		// #public, #public_ex, #protected and #protected_ex can all be used at once,
		// #protected_ex and #public both imply #protected, and #public_ex implies
		// the other three. Access within the same space and nested spaces is always
		// allowed.
		/// <summary>Provides general access within a library or program (implies
		/// #protected_in).</summary>
		public static readonly Symbol Internal = GSymbol.Get("#internal");
		/// <summary>Provides general access, even outside the assembly (i.e. 
		/// dynamic-link library). Implies #internal, #protectedIn and #protected.</summary>
		public static readonly Symbol Public = GSymbol.Get("#public");
		/// <summary>Provides access to derived classes only within the same library
		/// or program (i.e. assembly). There is no C# equivalent to this keyword,
		/// which does not provide access outside the assembly.</summary>
		public static readonly Symbol ProtectedIn = GSymbol.Get("#protectedIn");
		/// <summary>Provides access to all derived classes. Implies #protected_in.
		/// #protected #internal corresponds to C# "protected internal"</summary>
		public static readonly Symbol Protected = GSymbol.Get("#protected"); 
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
		public static readonly Symbol Private = GSymbol.Get("#private");

		public static readonly Symbol Static = GSymbol.Get("#static");


		// C#/.NET standard data types
		public static readonly Symbol Void   = GSymbol.Get("#void");
		public static readonly Symbol String = GSymbol.Get("#string");
		public static readonly Symbol Char   = GSymbol.Get("#char");
		public static readonly Symbol Bool   = GSymbol.Get("#bool");
		public static readonly Symbol Int8   = GSymbol.Get("#sbyte");
		public static readonly Symbol Int16  = GSymbol.Get("#short");
		public static readonly Symbol Int32  = GSymbol.Get("#int");
		public static readonly Symbol Int64  = GSymbol.Get("#long");
		public static readonly Symbol UInt8  = GSymbol.Get("#byte");
		public static readonly Symbol UInt16 = GSymbol.Get("#ushort");
		public static readonly Symbol UInt32 = GSymbol.Get("#uint");
		public static readonly Symbol UInt64 = GSymbol.Get("#ulong");
		public static readonly Symbol Single = GSymbol.Get("#float");
		public static readonly Symbol Double = GSymbol.Get("#double");
		public static readonly Symbol Decimal = GSymbol.Get("#decimal");

		// Styles
		public static readonly Symbol StyleCommaSeparatedStmts = GSymbol.Get("#style_commaSeparated");
		public static readonly Symbol StyleMacroCall = GSymbol.Get("#style_macroCall");

		public static bool IsArrayKeyword(Symbol s) { return CountArrayDimensions(s) > 0; }
		public static int CountArrayDimensions(Symbol s)
		{
			if (s.Name.Length >= 2 && s.Name[0] == '[' && s.Name[s.Name.Length-1] == ']') {
				for (int i = 1; i < s.Name.Length-1; i++)
					if (s.Name[i] != ',')
						return 0;
				return s.Name.Length-1;
			}
			return 0;
		}
	}

	/// <summary>Contains static helper methods for creating <see cref="GreenNode"/>s.
	/// Also contains the Cache method, which deduplicates subtrees that have the
	/// same structure.
	/// </summary>
	public class GreenFactory
	{
		public static readonly GreenNode Missing = new GreenSymbol(S.Missing, EmptySourceFile.Unknown, -1);

		// Common literals
		public GreenNode @true        { get { return Literal(true); } }
		public GreenNode @false       { get { return Literal(false); } }
		public GreenNode @null        { get { return Literal((object)null); } }
		public GreenNode @void        { get { return Literal(ecs.@void.Value); } }
		public GreenNode int_0        { get { return Literal(0); } }
		public GreenNode int_1        { get { return Literal(1);  } }
		public GreenNode string_empty { get { return Literal(""); } }

		public GreenNode DefKeyword   { get { return Symbol(S.Def, -1); } }
		public GreenNode EmptyList    { get { return Symbol(S.List, -1); } }
		
		// Standard data types (marked synthetic)
		public GreenNode Void    { get { return Symbol(S.Void, -1);	} }
		public GreenNode String  { get { return Symbol(S.String, -1);	} }
		public GreenNode Char    { get { return Symbol(S.Char, -1);	} }
		public GreenNode Bool    { get { return Symbol(S.Bool, -1);	} }
		public GreenNode Int8    { get { return Symbol(S.Int8, -1);	} }
		public GreenNode Int16   { get { return Symbol(S.Int16, -1);	} }
		public GreenNode Int32   { get { return Symbol(S.Int32, -1);	} }
		public GreenNode Int64   { get { return Symbol(S.Int64, -1);	} }
		public GreenNode UInt8   { get { return Symbol(S.UInt8, -1);	} }
		public GreenNode UInt16  { get { return Symbol(S.UInt16, -1);	} }
		public GreenNode UInt32  { get { return Symbol(S.UInt32, -1);	} }
		public GreenNode UInt64  { get { return Symbol(S.UInt64, -1);	} }
		public GreenNode Single  { get { return Symbol(S.Single, -1);	} }
		public GreenNode Double  { get { return Symbol(S.Double, -1);	} }
		public GreenNode Decimal { get { return Symbol(S.Decimal, -1);} }

		// Standard access modifiers
		public GreenNode Internal    { get { return Symbol(S.Internal, -1);	} }
		public GreenNode Public      { get { return Symbol(S.Public, -1);		} }
		public GreenNode ProtectedIn { get { return Symbol(S.ProtectedIn, -1);} }
		public GreenNode Protected   { get { return Symbol(S.Protected, -1);	} }
		public GreenNode Private     { get { return Symbol(S.Private, -1);    } }

		ISourceFile _file;
		ISourceFile File { get { return _file; } set { _file = value; } }

		public GreenFactory(ISourceFile file) { _file = file; }

		/// <summary>Gets a structurally equivalent node from the thread-local 
		/// cache, or places the node in the cache if it is not already there.</summary>
		/// <remarks>
		/// If the node is mutable, it will be frozen if it was put in the cache,
		/// or left unfrozen if a different node is being returned from the cache. 
		/// <para/>
		/// The node's SourceWidth is preserved but its Style is not.
		/// </remarks>
		public static GreenNode Cache(GreenNode input)
		{
			input = input.AutoOptimize(false, false);
			var r = _cache.Cache(input);
			if (r == input) r.Freeze();
			return r;
		}
		[ThreadStatic]
		static SimpleCache<GreenNode> _cache = new SimpleCache<GreenNode>(16384, GreenNode.DeepComparer.Value);

		public static readonly GreenAtOffs[] EmptyGreenArray = new GreenAtOffs[0];

		// Atoms: symbols (including keywords) and literals
		public GreenNode Symbol(string name, int sourceWidth = -1)
		{
			return new GreenSymbol(GSymbol.Get(name), _file, sourceWidth);
		}
		public GreenNode Symbol(Symbol name, int sourceWidth = -1)
		{
			return new GreenSymbol(name, _file, sourceWidth);
		}
		public GreenNode Literal(object value, int sourceWidth = -1)
		{
			return new GreenLiteral(value, _file, sourceWidth);
		}

		// Calls
		public GreenNode Call(GreenAtOffs head, int sourceWidth = -1)
		{
			return new GreenCall0(head, _file, sourceWidth);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs _1, int sourceWidth = -1)
		{
			return new GreenCall1(head, _file, sourceWidth, _1);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs _1, GreenAtOffs _2, int sourceWidth = -1)
		{
			return new GreenCall2(head, _file, sourceWidth, _1, _2);
		}
		public GreenNode Call(GreenAtOffs head, params GreenAtOffs[] list)
		{
			return Call(head, list, -1);
		}
		public GreenNode Call(GreenAtOffs head, GreenAtOffs[] list, int sourceWidth = -1)
		{
			return AddArgs(new EditableGreenNode(head, _file, sourceWidth), list);
		}
		public GreenNode Call(Symbol name, int sourceWidth = -1)
		{
			return new GreenSimpleCall0(name, _file, sourceWidth);
		}
		public GreenNode Call(Symbol name, GreenAtOffs _1, int sourceWidth = -1)
		{
			return new GreenSimpleCall1(name, _file, sourceWidth, new GreenAtOffs(_1));
		}
		public GreenNode Call(Symbol name, GreenAtOffs _1, GreenAtOffs _2, int sourceWidth = -1)
		{
			return new GreenSimpleCall2(name, _file, sourceWidth, new GreenAtOffs(_1), new GreenAtOffs(_2));
		}
		public GreenNode Call(Symbol name, GreenAtOffs[] list, int sourceWidth = -1)
		{
			return AddArgs(new EditableGreenNode(name, _file, sourceWidth), list);
		}
		private static GreenNode AddArgs(EditableGreenNode n, GreenAtOffs[] list)
		{
			var a = n.Args;
			for (int i = 0; i < list.Length; i++)
				a.Add(new GreenAtOffs(list[i]));
			return n;
		}

		public GreenNode Dot(params GreenAtOffs[] list)
		{
			return Call(S.Dot, list, -1);
		}
		public GreenNode Dot(params Symbol[] list)
		{
			GreenAtOffs[] array = list.Select(s => (GreenAtOffs)Symbol(s, -1)).ToArray();
			return Call(S.Dot, array);
		}
		public GreenNode Of(params GreenAtOffs[] list)
		{
			return Call(S.Of, list, -1);
		}
		public GreenNode Of(params Symbol[] list)
		{
			GreenAtOffs[] array = list.Select(s => (GreenAtOffs)Symbol(s, -1)).ToArray();
			return Call(S.Of, array);
		}
		public GreenNode Name(Symbol name, int sourceWidth = -1)
		{
			return Symbol(name, sourceWidth);
		}
		public GreenNode Braces(params GreenAtOffs[] contents)
		{
			return Braces(contents);
		}
		public GreenNode Braces(GreenAtOffs[] contents, int sourceWidth = -1)
		{
			return Call(S.Braces, contents, sourceWidth);
		}
		public GreenNode List(params GreenAtOffs[] contents)
		{
			return List(contents, -1);
		}
		public GreenNode List(GreenAtOffs[] contents, int sourceWidth = -1)
		{
			return Call(S.List, contents, sourceWidth);
		}
		public GreenNode Tuple(params GreenAtOffs[] contents)
		{
			return Tuple(contents, -1);
		}
		public GreenNode Tuple(GreenAtOffs[] contents, int sourceWidth = -1)
		{
			return Call(S.Tuple, contents, sourceWidth);
		}
		public GreenNode Def(Symbol name, GreenNode argList, GreenNode retType, GreenNode body = null, int sourceWidth = -1)
		{
			var file = argList.SourceFile;
			Debug.Assert(file == retType.SourceFile);
			return Def(Name(name), argList, retType, body, sourceWidth);
		}
		public GreenNode Def(GreenNode name, GreenNode argList, GreenNode retType, GreenNode body = null, int sourceWidth = -1)
		{
			G.Require(argList.Name == S.List || argList.Name == S.Missing);
			GreenNode def;
			if (body == null) def = Call(S.Def, new GreenAtOffs[] { name, argList, retType }, sourceWidth);
			else              def = Call(S.Def, new GreenAtOffs[] { name, argList, retType, body }, sourceWidth);
			return def;
		}
		public GreenNode Def(GreenAtOffs name, GreenAtOffs argList, GreenAtOffs retType, GreenAtOffs body = default(GreenAtOffs), int sourceWidth = -1, ISourceFile file = null)
		{
			G.Require(argList.Node.Name == S.List || argList.Node.Name == S.Missing);
			GreenNode def;
			if (body.Node == null) def = Call(S.Def, new[] { name, argList, retType }, sourceWidth);
			else                   def = Call(S.Def, new[] { name, argList, retType, body }, sourceWidth);
			return def;
		}
		public GreenNode ArgList(params GreenAtOffs[] vars)
		{
			return ArgList(vars, -1);
		}
		public GreenNode ArgList(GreenAtOffs[] vars, int sourceWidth)
		{
			foreach (var var in vars)
				G.RequireArg(var.Node.Name == S.Var && var.Node.ArgCount >= 2, "vars", var);
			return Call(S.List, vars, sourceWidth);
		}
		public GreenNode Var(GreenAtOffs type, Symbol name, GreenAtOffs initValue = default(GreenAtOffs))
		{
			if (initValue.Node != null)
				return Call(S.Var, type, Call(name, initValue));
			else
				return Call(S.Var, type, Symbol(name));
		}
		public GreenNode Var(GreenAtOffs type, params Symbol[] names)
		{
			var list = new List<GreenAtOffs>(names.Length+1) { type };
			list.AddRange(names.Select(n => new GreenAtOffs(Symbol(n))));
			return Call(S.Var, list.ToArray());
		}
		public GreenNode Var(GreenAtOffs type, params GreenAtOffs[] namesWithValues)
		{
			var list = new List<GreenAtOffs>(namesWithValues.Length+1) { type };
			list.AddRange(namesWithValues);
			return Call(S.Var, list.ToArray());
		}
	}
}
